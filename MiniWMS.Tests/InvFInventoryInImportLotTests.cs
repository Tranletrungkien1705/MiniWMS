using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ClosedXML.Excel;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #96 InvF_InventoryInController/ImportLot — nguồn InvF_InventoryInController.cs:3664-4097.
/// Server THẬT (FreePort.Get) + DB SQLite tạm (Seeder hạ tầng nạp Mst_ProductSeeder Order 14:
/// PT-LOC-DAU/SP01@ORG01, SP01@ORG02, SP01-L2A, SP01-L2B; Mst_InventorySeeder Order 12:
/// KHO01 (InvBUPattern="HN%"), KHO02 ("HCM%"), KHO03 ("HN%")).
/// HTTP POST multipart /api/invf_inventoryin/importlot.
/// File excel 7 cột: ProductCodeUser, ProductLotNo, Qty, ProductionDate, ExpiredDate, InvCodeInActual, Remark.
/// </summary>
public class InvFInventoryInImportLotTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_imp_lot_{Guid.NewGuid():N}.db");
        var dll = Path.Combine(AppContext.BaseDirectory, "MiniWMS.dll");
        Assert.True(File.Exists(dll), $"Không thấy {dll}");

        var psi = new ProcessStartInfo("dotnet", $"\"{dll}\"")
        {
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.Environment["PORT"] = port.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={dbPath}";
        psi.Environment["SSO_AUTHORITY"] = "http://127.0.0.1:1";

        var p = Process.Start(psi)!;
        var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        var up = false;
        for (var i = 0; i < 60 && !up; i++)
        {
            await Task.Delay(500);
            try { up = (await http.GetAsync("/healthz")).IsSuccessStatusCode; } catch { }
        }
        Assert.True(up, "Server không lên trong 30s");
        return (p, dbPath, http);
    }

    private static void Stop(Process p, string dbPath, HttpClient http)
    {
        try { p.Kill(true); } catch { }
        http.Dispose();
        foreach (var f in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
            try { if (File.Exists(f)) File.Delete(f); } catch { }
    }

    private static readonly string[] Headers =
        ["Mã hàng hóa *", "Số lô *", "Số lượng *", "Ngày sản xuất", "Ngày hết hạn", "Vị trí *", "Ghi chú"];

    private static byte[] Xlsx(params string[][] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");
        for (var c = 0; c < Headers.Length; c++) ws.Cell(1, c + 1).Value = Headers[c];
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length && c < Headers.Length; c++)
                ws.Cell(r + 2, c + 1).Value = rows[r][c];
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static MultipartFormDataContent Form(byte[] bytes, string fileName = "test.xlsx", string? invBUPattern = null)
    {
        var content = new MultipartFormDataContent();
        var filePart = new ByteArrayContent(bytes);
        filePart.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(filePart, "file", fileName);
        if (invBUPattern != null) content.Add(new StringContent(invBUPattern), "invBUPattern");
        return content;
    }

    private static async Task<JsonElement> PostAsync(HttpClient http, MultipartFormDataContent form)
    {
        var res = await http.PostAsync("/api/invf_inventoryin/importlot", form);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private static string? FirstMessage(JsonElement root)
    {
        if (root.TryGetProperty("Messages", out var m) && m.ValueKind == JsonValueKind.Array && m.GetArrayLength() > 0)
            return m[0].GetString();
        if (root.TryGetProperty("Detail", out var d) && d.ValueKind == JsonValueKind.String)
            return d.GetString();
        return null;
    }

    [Fact]
    public async Task ImportLot_Success_RoundTrip()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // KHÔNG truyền invBUPattern: guard vị trí cần cả KHO01 (HN%) và KHO02 (HCM%) cùng hợp lệ.
            var bytes = Xlsx(
                ["PT-LOC-DAU", "LOT-A", "100", "", "", "KHO01", ""],
                ["PT-LOC-DAU", "LOT-A", "50", "", "", "KHO02", ""]);
            var root = await PostAsync(http, Form(bytes));
            Assert.True(root.GetProperty("Success").GetBoolean(), FirstMessage(root) ?? "Success=false");
            var list = root.GetProperty("ListInvFInventoryInDtlUI");
            Assert.Equal(1, list.GetArrayLength());
            var item = list[0];
            Assert.Equal("SP01", item.GetProperty("ProductCode").GetString());
            Assert.Equal("PT-LOC-DAU", item.GetProperty("ProductCodeUser").GetString());
            Assert.Equal("KHO01, KHO02", item.GetProperty("InvCodeInActual").GetString());
            var qtyElem = item.GetProperty("Qty");
            var qtyVal = qtyElem.ValueKind == JsonValueKind.Number ? qtyElem.GetDouble() : double.Parse(qtyElem.GetString()!);
            Assert.Equal(150.0, qtyVal);
            Assert.Equal(2, item.GetProperty("Lst_Product_InvLot").GetArrayLength());
            var subs = item.GetProperty("Lst_InvF_InventoryInDtlUI");
            Assert.Equal(2, subs.GetArrayLength());
            var subCodes = Enumerable.Range(0, subs.GetArrayLength()).Select(i => subs[i].GetProperty("ProductCode").GetString()).ToList();
            Assert.Contains("SP01-L2A", subCodes);
            Assert.Contains("SP01-L2B", subCodes);
            Assert.Equal("", item.GetProperty("VATRateCode").GetString());
        }
        finally { Stop(p, dbPath, http); }
    }

    [Fact]
    public async Task ImportLot_InvBUPattern_Excludes_Position()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // invBUPattern="HCM%" ⇒ danh sách vị trí hợp lệ chỉ còn KHO02; KHO01 không qua ⇒ guard (nguồn 3760-3765).
            var bytes = Xlsx(
                ["PT-LOC-DAU", "LOT-A", "100", "", "", "KHO01", ""],
                ["PT-LOC-DAU", "LOT-A", "50", "", "", "KHO02", ""]);
            var r = await PostAsync(http, Form(bytes, invBUPattern: "HCM%"));
            Assert.False(r.GetProperty("Success").GetBoolean());
            Assert.Equal("Vị trí 'KHO01' không có trong hệ thống!", FirstMessage(r));
        }
        finally { Stop(p, dbPath, http); }
    }

    [Fact]
    public async Task ImportLot_EmptyFile_And_BadExtension()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var r1 = await PostAsync(http, Form(Array.Empty<byte>(), fileName: "x.xlsx"));
            Assert.False(r1.GetProperty("Success").GetBoolean());
            Assert.Equal("File import không hợp lệ!", r1.GetProperty("Detail").GetString());

            var r2 = await PostAsync(http, Form(Xlsx(["PT-LOC-DAU", "LOT-A", "10", "", "", "KHO01", ""]), fileName: "test.txt"));
            Assert.False(r2.GetProperty("Success").GetBoolean());
            Assert.Equal("File import không hợp lệ!", r2.GetProperty("Detail").GetString());
        }
        finally { Stop(p, dbPath, http); }
    }

    [Fact]
    public async Task ImportLot_BadColumnCount()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("S");
            ws.Cell(1, 1).Value = "Mã hàng hóa *"; ws.Cell(1, 2).Value = "Số lô *";
            ws.Cell(2, 1).Value = "PT-LOC-DAU"; ws.Cell(2, 2).Value = "LOT-A";
            using var ms = new MemoryStream(); wb.SaveAs(ms);
            var r = await PostAsync(http, Form(ms.ToArray()));
            Assert.False(r.GetProperty("Success").GetBoolean());
            Assert.Equal("File excel import không hợp lệ!", FirstMessage(r));
        }
        finally { Stop(p, dbPath, http); }
    }

    [Fact]
    public async Task ImportLot_RowGuards()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var cases = new (string[] row, string expected)[]
            {
                (["", "LOT-A", "10", "", "", "KHO01", ""], "Mã hàng hóa không được để trống!"),
                (["PT-LOC-DAU", "", "10", "", "", "KHO01", ""], "Số lô không được để trống!"),
                (["PT-LOC-DAU", "LOT-A", "", "", "", "KHO01", ""], "Số lượng không được để trống!"),
                (["PT-LOC-DAU", "LOT-A", "abc", "", "", "KHO01", ""], "Số lượng không phải định dạng số!"),
                (["PT-LOC-DAU", "LOT-A", "-1", "", "", "KHO01", ""], "Số lượng không được < 0!"),
                (["PT-LOC-DAU", "LOT-A", "10", "", "", "", ""], "Vị trí không được để trống!"),
                (["PT-LOC-DAU", "LOT-A", "10", "", "", "KHO99", ""], "Vị trí 'KHO99' không có trong hệ thống!"),
            };
            for (var k = 0; k < cases.Length; k++)
            {
                var r = await PostAsync(http, Form(Xlsx(cases[k].row)));
                Assert.False(r.GetProperty("Success").GetBoolean(), $"ca {k}");
                Assert.Equal(cases[k].expected, FirstMessage(r));
            }
        }
        finally { Stop(p, dbPath, http); }
    }

    [Fact]
    public async Task ImportLot_DuplicateTriple_InFile()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var r = await PostAsync(http, Form(Xlsx(
                ["PT-LOC-DAU", "LOT-A", "10", "", "", "KHO01", ""],
                ["PT-LOC-DAU", "LOT-A", "20", "", "", "KHO01", ""])));
            Assert.False(r.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã hàng hóa 'PT-LOC-DAU' thuộc lô 'LOT-A' có vị trí 'KHO01' trong file excel lặp!", FirstMessage(r));
        }
        finally { Stop(p, dbPath, http); }
    }

    [Fact]
    public async Task ImportLot_UnknownProduct()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var r = await PostAsync(http, Form(Xlsx(["PT-NONEXIST", "LOT-A", "10", "", "", "KHO01", ""])));
            Assert.False(r.GetProperty("Success").GetBoolean());
            Assert.Equal("Mã hàng hóa 'PT-NONEXIST' không có trong hệ thống!", FirstMessage(r));
        }
        finally { Stop(p, dbPath, http); }
    }
}

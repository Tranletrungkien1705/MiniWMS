using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #30 Inv_CostPriceHistController/ExportExcel — nguồn Inv_CostPriceHistController.cs:721-777
/// (+ GetExportDicColumsTemplate :829-843, BaseController.GenExcelExportFilePath :750-768)
/// → Inv_CostPriceHistService.Instance.WA_Inv_CostPriceHist_Get (dòng 755) → API InvCostPriceHistController.cs:163-232
/// → biz Form.cs:200118-200203 → Form.cs:200205-200351 → Form.cs:200498-200669 (Inv_CostPriceHist_GetX_New20240916).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/inv_costpricehist/exportexcel.
///
/// Số kỳ vọng tính tay từ nguồn + seed:
///   Mst_Product (Order 14): SP01/SP02/SP03 @ ORG01, SP01 @ ORG02 (ProductCodeUser PT-LOC-DAU/PT-LOC-GIO/PT-BUGI).
///   Mst_Unit (Order 15): U01 "Cái", U02 "Bộ" @ ORG01; U01 @ ORG02.
///   Mst_Inventory (Order 12): KHO01, KHO02, KHO03.
///   Inv_CostPriceHist (Order 200): CPH01..CPH05 — SP01@ORG01@KHO01 có 2 mốc (2026-01-01, 2026-06-01),
///     SP02@ORG01@KHO01 (2026-03-01), SP03@ORG01@KHO02 (2026-04-01), SP01@ORG02@KHO01 (2026-05-01).
///   ⇒ body rỗng (FlagIsCurrent "" → StdFlag = "0" → nhánh select distinct): 5 dòng dữ liệu (6 dòng tính cả tiêu đề).
///     FlagIsCurrent = "1" (nhánh group by + Max): 4 dòng dữ liệu (5 dòng tính cả tiêu đề).
///     InvCode = "KHO02": 1 dòng dữ liệu (2 dòng tính cả tiêu đề).
///     InvCode = "KHONG-CO": 0 dòng ⇒ nhánh "Không có dữ liệu xuất!" (dòng 765-768).
/// </summary>
public class InvCostPriceHistExportExcelTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"inv_costpricehist_exportexcel_{Guid.NewGuid():N}.db");
        var dll = Path.Combine(AppContext.BaseDirectory, "MiniWMS.dll");
        Assert.True(File.Exists(dll), $"Không thấy {dll}");

        var psi = new ProcessStartInfo("dotnet", $"\"{dll}\"")
        {
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.Environment["PORT"] = port.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={dbPath}";
        psi.Environment["SSO_AUTHORITY"] = "http://127.0.0.1:1";   // không chạm mạng thật

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

    private static async Task<int> CountAsync(string dbPath, string table)
    {
        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"select count(*) from {table}";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    /// <summary>Mở file Excel theo `strUrl` trả về (vật lý nằm dưới wwwroot của thư mục chạy app).</summary>
    private static XLWorkbook OpenExport(string strUrl)
    {
        var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        var physical = Path.Combine(webRoot, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(physical), $"Không thấy file {physical}");
        return new XLWorkbook(physical);
    }

    [Fact]
    public async Task ExportExcel_Guards_Filters_And_FileContent()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — body rỗng: SysAccessCheckDeny(userState null) = false (BaseController.cs:3245-3269)
            //          → RedirectToAction("Index", "AccessDeny") (Inv_CostPriceHistController.cs:723-727).
            var r1 = await http.PostAsync("/api/inv_costpricehist/exportexcel", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal("/AccessDeny/Index", root.GetProperty("RedirectUrl").GetString());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
            }

            // Ca 2 — có quyền nhưng ListSysAccess không chứa MNU_RPT_INV_INVENTORYBALANCE_STORAGETIME → vẫn bị chặn.
            var r2 = await http.PostAsJsonAsync("/api/inv_costpricehist/exportexcel",
                new { ListSysAccess = new[] { "MNU_INV_COSTPRICEHIST" } });
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
                Assert.Equal("/AccessDeny/Index", d2.RootElement.GetProperty("RedirectUrl").GetString());

            // Ca 3 — có quyền (không phân biệt hoa/thường), body rỗng: FlagIsCurrent "" → StdFlag = "0" → nhánh select
            //          distinct (Form.cs:200561-200583) → 5 dòng dữ liệu; xuất Excel 9 cột (dòng 829-843).
            var r3 = await http.PostAsJsonAsync("/api/inv_costpricehist/exportexcel",
                new { ListSysAccess = new[] { "mnu_rpt_inv_inventorybalance_storagetime" } });
            Assert.Equal(HttpStatusCode.OK, r3.StatusCode);
            string url3;
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                var root = d3.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
                Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
                url3 = root.GetProperty("strUrl").GetString()!;
            }
            // Nguồn 761 + BaseController.cs:752-759: /TempFiles/yyyy-MM-dd/Inv_CostPriceHist_yyMMddHHmmss.xlsx
            Assert.StartsWith("/TempFiles/", url3);
            Assert.EndsWith(".xlsx", url3);
            Assert.Contains("Inv_CostPriceHist_", url3);

            using (var wb = OpenExport(url3))
            {
                var ws = wb.Worksheets.First();
                // Nguồn 762: sheet name = string.Format("Inv_CostPriceHist").
                Assert.Equal("Inv_CostPriceHist", ws.Name);
                // Nguồn 833-841: 9 cột ĐÚNG thứ tự từ điển nguồn.
                Assert.Equal("Thời gian hiệu lực", ws.Cell(1, 1).GetString());
                Assert.Equal("Mã hàng hóa", ws.Cell(1, 2).GetString());
                Assert.Equal("Tên hàng hóa", ws.Cell(1, 3).GetString());
                Assert.Equal("Đơn vị tính", ws.Cell(1, 4).GetString());
                Assert.Equal("Giá vốn", ws.Cell(1, 5).GetString());
                Assert.Equal("Số phiếu nhập", ws.Cell(1, 6).GetString());
                Assert.Equal("Người cập nhật", ws.Cell(1, 7).GetString());
                Assert.Equal("Ngày cập nhật", ws.Cell(1, 8).GetString());
                Assert.Equal("Mã kho", ws.Cell(1, 9).GetString());
                // 5 dòng dữ liệu + 1 dòng tiêu đề.
                Assert.Equal(6, ws.LastRowUsed()!.RowNumber());

                // Dòng SP01@ORG01@KHO01 mốc 2026-06-01: cột join Mst_Product + 4 cột bảng.
                var row = ws.RowsUsed().Skip(1).Single(r =>
                    r.Cell(2).GetString() == "PT-LOC-DAU" && r.Cell(9).GetString() == "KHO01"
                    && r.Cell(1).GetDateTime().Month == 6);
                Assert.Equal("Lọc dầu động cơ", row.Cell(3).GetString());
                Assert.Equal("U01", row.Cell(4).GetString());
                Assert.Equal(92000d, row.Cell(5).GetDouble());
                Assert.Equal("PN260801-001", row.Cell(6).GetString());
                Assert.Equal("seed", row.Cell(7).GetString());
                Assert.Equal(new DateTime(2026, 6, 1), row.Cell(8).GetDateTime());
            }

            // Ca 4 — bộ lọc khác: FlagIsCurrent = "1" → nhánh group by + Max(EffectDTimeUTC) (Form.cs:200588-200610)
            //          → 4 dòng dữ liệu (5 dòng tính cả tiêu đề).
            var r4 = await http.PostAsJsonAsync("/api/inv_costpricehist/exportexcel",
                new { IsSysAdmin = true, FlagIsCurrent = "1" });
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
            {
                Assert.True(d4.RootElement.GetProperty("Success").GetBoolean());
                using var wb = OpenExport(d4.RootElement.GetProperty("strUrl").GetString()!);
                Assert.Equal(5, wb.Worksheets.First().LastRowUsed()!.RowNumber());
            }

            // Ca 5 — bộ lọc khác: InvCode = "kho02" (toán tử `=`, StdParam viết hoa) → 1 dòng dữ liệu (2 dòng).
            var r5 = await http.PostAsJsonAsync("/api/inv_costpricehist/exportexcel",
                new { IsSysAdmin = true, InvCode = "kho02" });
            using (var d5 = JsonDocument.Parse(await r5.Content.ReadAsStringAsync()))
            {
                Assert.True(d5.RootElement.GetProperty("Success").GetBoolean());
                using var wb = OpenExport(d5.RootElement.GetProperty("strUrl").GetString()!);
                var ws = wb.Worksheets.First();
                Assert.Equal(2, ws.LastRowUsed()!.RowNumber());
                Assert.Equal("PT-BUGI", ws.Cell(2, 2).GetString());
                Assert.Equal("KHO02", ws.Cell(2, 9).GetString());
            }

            // Ca 6 — không có dữ liệu: InvCode = "KHONG-CO" → { Success = false, Title = "Không có dữ liệu xuất!",
            //          CheckSuccess = "0" } (dòng 765-768), KHÔNG sinh file.
            var r6 = await http.PostAsJsonAsync("/api/inv_costpricehist/exportexcel",
                new { IsSysAdmin = true, InvCode = "KHONG-CO" });
            Assert.Equal(HttpStatusCode.OK, r6.StatusCode);
            using (var d6 = JsonDocument.Parse(await r6.Content.ReadAsStringAsync()))
            {
                var root = d6.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Không có dữ liệu xuất!", root.GetProperty("Title").GetString());
                Assert.Equal("0", root.GetProperty("CheckSuccess").GetString());
                Assert.False(root.TryGetProperty("strUrl", out _));
            }

            // Action ĐỌC: không ghi gì — vẫn 5 dòng Inv_CostPriceHist.
            Assert.Equal(5, await CountAsync(dbPath, "Inv_CostPriceHist"));
        }
        finally
        {
            try { p.Kill(true); } catch { }
            http.Dispose();
            SqliteConnection.ClearAllPools();
            foreach (var f in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
                try { if (File.Exists(f)) File.Delete(f); } catch { }
        }
    }
}

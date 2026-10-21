using System.Diagnostics;
using System.Net;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #28 Inv_CostPriceHistController/ExportTemplate — nguồn Inv_CostPriceHistController.cs:408-418
/// (+ GetImportDicColumsTemplate :818-827, BaseController.GenExcelExportFilePath :750-768).
/// Action KHÔNG có tham số, KHÔNG đọc/ghi bảng, KHÔNG gọi service: luôn xuất file mẫu RỖNG (chỉ dòng tiêu đề 4 cột)
/// rồi trả { Success = true, Title = "Đã xuất excel thành công!", CheckSuccess = "1", strUrl = url }.
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/inv_costpricehist/exporttemplate.
/// </summary>
public class InvCostPriceHistExportTemplateTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"inv_costpricehist_exporttemplate_{Guid.NewGuid():N}.db");
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

    [Fact]
    public async Task ExportTemplate_EmptyBody_ReturnsSuccessAndHeaderOnlyFile()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — body rỗng: nguồn 408-418 không có tham số, không guard ⇒ vẫn xuất file mẫu + Success = true.
            var r1 = await http.PostAsync("/api/inv_costpricehist/exporttemplate", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            string strUrl;
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Đã xuất excel thành công!", root.GetProperty("Title").GetString());
                Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
                strUrl = root.GetProperty("strUrl").GetString()!;
            }

            // Nguồn 414 + BaseController.cs:752-759: /TempFiles/yyyy-MM-dd/Inv_CostPriceHist_yyMMddHHmmss.xlsx
            Assert.StartsWith("/TempFiles/", strUrl);
            Assert.EndsWith(".xlsx", strUrl);
            Assert.Contains("Inv_CostPriceHist_", strUrl);

            // File vật lý nằm dưới wwwroot của thư mục chạy app (dll ở bin/Release/net8.0).
            var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var physical = Path.Combine(webRoot, strUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(physical), $"Không thấy file {physical}");

            // Nguồn 410 + 818-827: list RỖNG ⇒ chỉ có dòng tiêu đề, 4 cột đúng thứ tự từ điển nguồn.
            using (var wb = new XLWorkbook(physical))
            {
                var ws = wb.Worksheets.First();
                Assert.Equal("Mst_Product", ws.Name);   // nguồn 415: sheet name viết cứng "Mst_Product"
                Assert.Equal("Thời gian hiệu lực(*)", ws.Cell(1, 1).GetString());
                Assert.Equal("Mã hàng hóa(*)", ws.Cell(1, 2).GetString());
                Assert.Equal("Giá vốn(*)", ws.Cell(1, 3).GetString());
                Assert.Equal("Mã kho", ws.Cell(1, 4).GetString());
                Assert.Equal(1, ws.LastRowUsed()!.RowNumber());   // không có dòng dữ liệu
            }

            // Ca 2 — body có dữ liệu lạ: nguồn KHÔNG đọc body ⇒ kết quả y hệt (vẫn Success, vẫn file mới).
            var r2 = await http.PostAsync("/api/inv_costpricehist/exporttemplate",
                new StringContent("{\"ProductCodeUser\":\"X\"}", System.Text.Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
            {
                Assert.True(d2.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("1", d2.RootElement.GetProperty("CheckSuccess").GetString());
                Assert.EndsWith(".xlsx", d2.RootElement.GetProperty("strUrl").GetString());
            }

            // Action KHÔNG ghi bảng: Inv_CostPriceHist vẫn đúng 5 dòng seed (Inv_CostPriceHistSeeder Order 200).
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

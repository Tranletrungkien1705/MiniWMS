using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #305 Mst_CustomerGroupController/Export — nguồn Mst_CustomerGroupController.cs:408-473
/// (→ WA_Mst_CustomerGroup_Get → Mst_CustomerGroup_Get → Mst_CustomerGroup_GetX, Customer.cs:11219-12320).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/mst_customergroup/export.
/// Số kỳ vọng tính tay từ nguồn + seeder (Mst_CustomerGroupSeeder: 3 dòng KH-LE/KH-DL/KH-XN, OrgID "1001";
/// thứ tự chèn KH-LE → KH-DL → KH-XN = thứ tự MyIdxSeq của nguồn).
/// </summary>
public class Mst_CustomerGroupExportTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"mst_customergroup_export_{Guid.NewGuid():N}.db");
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

    private static async Task<int> CountAsync(string dbPath)
    {
        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "select count(*) from Mst_CustomerGroup";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Export_EmptyBody_ReturnsNoData_AndDoesNotWrite()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — body rỗng: nguồn 419-425 dựng model Name/Desc = null → strWhereClause rỗng (590-597)
            // → where (1=1) → GetX phân trang theo MyIdxSeq với Ft_RecordStart/Ft_RecordCount = 0/0
            // (giá trị khởi tạo của BaseController) → nFilterRecordEnd = -1 → 0 dòng → nhánh "Không có dữ liệu!" (dòng 462-465).
            var r1 = await http.PostAsync("/api/mst_customergroup/export", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Không có dữ liệu!", root.GetProperty("Title").GetString());
                Assert.Equal("1", root.GetProperty("CheckSuccess").GetString());
                Assert.False(root.TryGetProperty("strUrl", out _));   // nguồn 464: nhánh rỗng KHÔNG có strUrl
            }

            // Ca 2 — body có bộ lọc nhưng vẫn 0 dòng (cùng lý do phân trang 0/0) → vẫn "Không có dữ liệu!".
            var r2 = await http.PostAsJsonAsync("/api/mst_customergroup/export", new { CustomerGrpName = "Khách" });
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
            {
                Assert.True(d2.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Không có dữ liệu!", d2.RootElement.GetProperty("Title").GetString());
            }

            // Action ĐỌC: không ghi gì — vẫn 3 dòng danh mục.
            Assert.Equal(3, await CountAsync(dbPath));
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

    /// <summary>
    /// Nguồn 590-597: `CustomerGrpName like '%x%'` và `CustomerGrpDesc like '%x%'` (Trim, không phân biệt hoa/thường).
    /// Kiểm bộ lọc + nhánh xuất Excel bằng controller trực tiếp trên DB in-memory (Ft_RecordStart/Count của gate = 0/0
    /// nên nhánh xuất file không chạy được qua HTTP — xem GAP trong TOM TAT).
    /// </summary>
    [Fact]
    public async Task Export_Filters_And_ExcelColumns()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        await db.Database.EnsureCreatedAsync();
        await PortSchema.EnsureAsync(db);

        var webRoot = Path.Combine(Path.GetTempPath(), $"mst_customergroup_wwwroot_{Guid.NewGuid():N}");
        Directory.CreateDirectory(webRoot);
        try
        {
            var ctrl = new MiniWMS.Controllers.Port.Mst_CustomerGroupControllerPort(db, new TestWebHostEnvironment(webRoot));

            // Bộ lọc Name "Đại" → chỉ KH-DL ("Đại lý") — 1 dòng.
            var r1 = await ctrl.Export(new() { CustomerGrpName = "Đại" });
            var ok1 = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(r1);
            var title1 = (string)ok1.Value!.GetType().GetProperty("Title")!.GetValue(ok1.Value)!;
            Assert.Equal("Không có dữ liệu!", title1);   // Ft_RecordCount = 0 → 0 dòng (nguồn 12153-12154)

            // Bộ lọc Desc "phân phối" → chỉ KH-DL — vẫn 0 dòng vì phân trang 0/0.
            var r2 = await ctrl.Export(new() { CustomerGrpDesc = "phân phối" });
            var ok2 = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(r2);
            var title2 = (string)ok2.Value!.GetType().GetProperty("Title")!.GetValue(ok2.Value)!;
            Assert.Equal("Không có dữ liệu!", title2);

            // Bộ lọc không khớp → 0 dòng.
            var r3 = await ctrl.Export(new() { CustomerGrpName = "KHONG-CO" });
            var ok3 = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(r3);
            var title3 = (string)ok3.Value!.GetType().GetProperty("Title")!.GetValue(ok3.Value)!;
            Assert.Equal("Không có dữ liệu!", title3);

            // Đối chiếu bộ lọc trực tiếp trên DB (cùng biểu thức Like của action): Name "Đại" → 1 dòng KH-DL;
            // Desc "phân phối" → 1 dòng KH-DL; Name "khách" (chữ thường, có dấu) → 1 dòng KH-LE.
            // LƯU Ý: `lower()` Unicode chỉ được đăng ký ở hạ tầng khi chạy app (Data/SqliteUnicodeLower.cs) —
            // DbContext in-memory của test KHÔNG có nó, nên ở đây so bằng `ToLowerInvariant()` phía C#.
            var all = await db.Set<Mst_CustomerGroup>().ToListAsync();
            static bool Like(string? col, string value) =>
                (col ?? "").ToLowerInvariant().Contains(value.Trim().ToLowerInvariant(), StringComparison.Ordinal);
            Assert.Equal(1, all.Count(x => Like(x.CustomerGrpName, "Đại")));
            Assert.Equal(1, all.Count(x => Like(x.CustomerGrpDesc, "phân phối")));
            Assert.Equal(1, all.Count(x => Like(x.CustomerGrpName, "khách")));
            Assert.Equal(0, all.Count(x => Like(x.CustomerGrpName, "KHONG-CO")));

            // Nhánh xuất Excel (nguồn 453-460): gọi thẳng PortExcel với đúng từ điển cột của gate (569-576)
            // để xác nhận tiêu đề 2 cột + dữ liệu, rồi đọc lại file.
            var dic = new Dictionary<string, string>
            {
                { "CustomerGrpName", "Mã nhóm khách hàng" },
                { "CustomerGrpDesc", "Tên nhóm khách hàng" },
            };
            var rows = await db.Set<Mst_CustomerGroup>().OrderBy(x => x.AutoId).ToListAsync();
            var (filePath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, nameof(Mst_CustomerGroup));
            PortExcel.ExportToExcelFromList(rows, dic, filePath, nameof(Mst_CustomerGroup));
            Assert.True(File.Exists(filePath));
            Assert.StartsWith("/TempFiles/", virtualPath);
            Assert.EndsWith(".xlsx", virtualPath);

            using (var wb = new XLWorkbook(filePath))
            {
                var ws = wb.Worksheets.First();
                Assert.Equal("Mã nhóm khách hàng", ws.Cell(1, 1).GetString());
                Assert.Equal("Tên nhóm khách hàng", ws.Cell(1, 2).GetString());
                Assert.Equal("Khách lẻ", ws.Cell(2, 1).GetString());
                Assert.Equal("Khách mua lẻ", ws.Cell(2, 2).GetString());
                Assert.Equal("Đại lý", ws.Cell(3, 1).GetString());
                Assert.Equal("Xưởng dịch vụ", ws.Cell(4, 1).GetString());
            }
        }
        finally
        {
            try { Directory.Delete(webRoot, true); } catch { }
        }
    }

    private class TestWebHostEnvironment(string webRoot) : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ApplicationName { get; set; } = "MiniWMS.Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = webRoot;
        public string EnvironmentName { get; set; } = "Development";
    }
}

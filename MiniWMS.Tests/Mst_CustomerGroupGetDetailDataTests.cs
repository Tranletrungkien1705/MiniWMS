using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #304 Mst_CustomerGroupController/GetDetailData — nguồn Mst_CustomerGroupController.cs:350-402
/// (→ WA_Mst_CustomerGroup_Get → Mst_CustomerGroup_Get → Mst_CustomerGroup_GetX, Customer.cs:11219-12320).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/mst_customergroup/getdetaildata.
/// Số kỳ vọng tính tay từ nguồn + seeder (Mst_CustomerGroupSeeder: 3 dòng KH-LE/KH-DL/KH-XN, OrgID "1001";
/// thứ tự chèn KH-LE → KH-DL → KH-XN = thứ tự MyIdxSeq của nguồn).
/// </summary>
public class Mst_CustomerGroupGetDetailDataTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"mst_customergroup_detail_{Guid.NewGuid():N}.db");
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
    public async Task GetDetailData_EmptyBody_UnknownCode_Found_AndCaseInsensitive()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — body rỗng: nguồn 362-366 dựng model với CustomerGrpCode = null → strWhereClause rỗng (586-589)
            // → where (1=1) → GetX trả dòng ĐẦU theo MyIdxSeq asc = KH-LE (dòng chèn đầu tiên của seeder).
            var r1 = await http.PostAsync("/api/mst_customergroup/getdetaildata", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.Equal("KH-LE", root.GetProperty("CustomerGrpCode").GetString());
                Assert.Equal("Khách lẻ", root.GetProperty("CustomerGrpName").GetString());
                Assert.Equal("Khách mua lẻ", root.GetProperty("CustomerGrpDesc").GetString());
                Assert.Equal("1", root.GetProperty("FlagActive").GetString());
                Assert.Equal("1001", root.GetProperty("OrgID").GetString());
            }

            // Ca 2 — CustomerGrpCode rỗng chuỗi: CUtils.IsNullOrEmpty → KHÔNG thêm điều kiện → vẫn dòng đầu (KH-LE).
            var r2 = await http.PostAsJsonAsync("/api/mst_customergroup/getdetaildata", new { CustomerGrpCode = "" });
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
                Assert.Equal("KH-LE", d2.RootElement.GetProperty("CustomerGrpCode").GetString());

            // Ca 3 — mã không tồn tại: nguồn 390-394 không thay model → trả đúng đối tượng dựng ban đầu
            // (CustomerGrpCode = mã gửi lên, mọi cột khác null, AutoId = 0).
            var r3 = await http.PostAsJsonAsync("/api/mst_customergroup/getdetaildata", new { CustomerGrpCode = "KH-KHONG-CO" });
            Assert.Equal(HttpStatusCode.OK, r3.StatusCode);
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                var root = d3.RootElement;
                Assert.Equal("KH-KHONG-CO", root.GetProperty("CustomerGrpCode").GetString());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("CustomerGrpName").ValueKind);
                Assert.Equal(JsonValueKind.Null, root.GetProperty("CustomerGrpDesc").ValueKind);
                Assert.Equal(JsonValueKind.Null, root.GetProperty("FlagActive").ValueKind);
                Assert.Equal(JsonValueKind.Null, root.GetProperty("OrgID").ValueKind);
                Assert.Equal(0, root.GetProperty("AutoId").GetInt64());
            }

            // Ca 4 — tìm thấy: KH-DL (dòng thứ 2 của seeder) → trả đúng dòng đó.
            var r4 = await http.PostAsJsonAsync("/api/mst_customergroup/getdetaildata", new { CustomerGrpCode = "KH-DL" });
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
            {
                var root = d4.RootElement;
                Assert.Equal("KH-DL", root.GetProperty("CustomerGrpCode").GetString());
                Assert.Equal("Đại lý", root.GetProperty("CustomerGrpName").GetString());
                Assert.Equal("Đại lý phân phối", root.GetProperty("CustomerGrpDesc").GetString());
                Assert.Equal("1", root.GetProperty("FlagActive").GetString());
            }

            // Ca 5 — mã gửi chữ thường vẫn khớp (nguồn SQL Server collation không phân biệt hoa/thường, toán tử "=").
            var r5 = await http.PostAsJsonAsync("/api/mst_customergroup/getdetaildata", new { CustomerGrpCode = "kh-xn" });
            using (var d5 = JsonDocument.Parse(await r5.Content.ReadAsStringAsync()))
            {
                var root = d5.RootElement;
                Assert.Equal("KH-XN", root.GetProperty("CustomerGrpCode").GetString());
                Assert.Equal("Xưởng dịch vụ", root.GetProperty("CustomerGrpName").GetString());
                Assert.Equal("0", root.GetProperty("FlagActive").GetString());
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
    /// Nguồn 12174-12187: `select distinct OrgID, CustomerGrpCode ... order by MyIdxSeq asc` — cùng một mã ở 2 OrgID
    /// cho 2 dòng; lấy dòng đầu theo thứ tự chèn (MyIdxSeq). Kiểm bằng controller trực tiếp trên DB in-memory.
    /// </summary>
    [Fact]
    public async Task GetDetailData_SameCodeTwoOrgs_ReturnsFirstInserted()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        await db.Database.EnsureCreatedAsync();
        await PortSchema.EnsureAsync(db);

        db.Set<Mst_CustomerGroup>().AddRange(
            new Mst_CustomerGroup { OrgID = "ORG02", CustomerGrpCode = "KH-DUP", CustomerGrpName = "Bản ORG02", FlagActive = "1" },
            new Mst_CustomerGroup { OrgID = "ORG01", CustomerGrpCode = "KH-DUP", CustomerGrpName = "Bản ORG01", FlagActive = "1" });
        await db.SaveChangesAsync();

        var ctrl = new MiniWMS.Controllers.Port.Mst_CustomerGroupControllerPort(db);
        var r = await ctrl.GetDetailData(new() { CustomerGrpCode = "KH-DUP" });
        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(r);
        var entity = Assert.IsType<Mst_CustomerGroup>(ok.Value);
        Assert.Equal("ORG02", entity.OrgID);          // dòng chèn trước = MyIdxSeq nhỏ hơn
        Assert.Equal("Bản ORG02", entity.CustomerGrpName);
    }
}

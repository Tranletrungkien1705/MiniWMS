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
/// #301 Mst_CustomerGroupController/Create — action MỞ FORM của nguồn (Mst_CustomerGroupController.cs:106-113):
/// không tham số, không ghi DB, trả `new Mst_CustomerGroup()` RỖNG. Test khởi động server THẬT trên cổng trống
/// (FreePort.Get) + DB SQLite tạm, gọi POST /api/mst_customergroup/create.
/// </summary>
public class Mst_CustomerGroupCreateTests
{
    [Fact]
    public async Task Create_ReturnsEmptyModel_AndWritesNothing()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"mst_customergroup_{Guid.NewGuid():N}.db");
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

        using var p = Process.Start(psi)!;
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            var up = false;
            for (var i = 0; i < 60 && !up; i++)
            {
                await Task.Delay(500);
                try { up = (await http.GetAsync("/healthz")).IsSuccessStatusCode; } catch { }
            }
            Assert.True(up, "Server không lên trong 30s");

            // Đếm số dòng trước khi gọi (action nguồn KHÔNG ghi bảng).
            int before;
            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "select count(*) from Mst_CustomerGroup";
                before = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
            Assert.Equal(3, before);   // seeder danh mục: 3 dòng

            // Body rỗng (nguồn không có tham số) -> 200 + model rỗng.
            var res = await http.PostAsync("/api/mst_customergroup/create", null);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var body = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            Assert.Equal(0, root.GetProperty("AutoId").GetInt64());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("CustomerGrpCode").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("CustomerGrpName").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("CustomerGrpDesc").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("FlagActive").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("OrgID").ValueKind);

            // Gửi kèm body lạ cũng bị bỏ qua (nguồn không đọc tham số) -> vẫn model rỗng.
            var res2 = await http.PostAsJsonAsync("/api/mst_customergroup/create",
                new { CustomerGrpCode = "KH-TEST", CustomerGrpName = "Bị bỏ qua" });
            Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
            using var doc2 = JsonDocument.Parse(await res2.Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.Null, doc2.RootElement.GetProperty("CustomerGrpCode").ValueKind);

            // Không ghi DB: số dòng không đổi.
            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "select count(*) from Mst_CustomerGroup";
                Assert.Equal(before, Convert.ToInt32(await cmd.ExecuteScalarAsync()));
            }
        }
        finally
        {
            try { p.Kill(true); } catch { }
            SqliteConnection.ClearAllPools();
            foreach (var f in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
                try { if (File.Exists(f)) File.Delete(f); } catch { }
        }
    }

    [Fact]
    public async Task Seeder_IsIdempotent()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        await db.Database.EnsureCreatedAsync();
        await PortSchema.EnsureAsync(db);
        await PortSchema.EnsureAsync(db);
        Assert.Equal(3, await db.Set<Mst_CustomerGroup>().CountAsync());
    }
}

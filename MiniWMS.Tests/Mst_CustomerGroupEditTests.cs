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
/// #303 Mst_CustomerGroupController/Edit — nguồn Mst_CustomerGroupController.cs:253-344.
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/mst_customergroup/edit.
/// Số kỳ vọng tính tay từ nguồn + seeder (Mst_CustomerGroupSeeder: 3 dòng KH-LE/KH-DL/KH-XN, OrgID "1001").
/// </summary>
public class Mst_CustomerGroupEditTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"mst_customergroup_edit_{Guid.NewGuid():N}.db");
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

    private static async Task<(string? Name, string? Desc, string? Flag, string? OrgID)> ReadRowAsync(string dbPath, string code)
    {
        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "select CustomerGrpName, CustomerGrpDesc, FlagActive, OrgID from Mst_CustomerGroup where CustomerGrpCode = $c";
        cmd.Parameters.AddWithValue("$c", code);
        await using var rd = await cmd.ExecuteReaderAsync();
        Assert.True(await rd.ReadAsync(), $"Không thấy dòng {code}");
        return (rd.IsDBNull(0) ? null : rd.GetString(0), rd.IsDBNull(1) ? null : rd.GetString(1),
                rd.IsDBNull(2) ? null : rd.GetString(2), rd.IsDBNull(3) ? null : rd.GetString(3));
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
    public async Task Edit_EmptyBody_And_UnknownCode_And_RoundTrip()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — body rỗng: nguồn 263 sai điều kiện → else 337-341 → Success=true "Mã nhóm khách hàng trống!".
            var r1 = await http.PostAsync("/api/mst_customergroup/edit", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                Assert.True(d1.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Mã nhóm khách hàng trống!", d1.RootElement.GetProperty("Messages")[0].GetString());
            }

            // Ca 2 — CustomerGrpCode rỗng chuỗi: cùng nhánh else.
            var r1b = await http.PostAsJsonAsync("/api/mst_customergroup/edit", new { CustomerGrpCode = "", CustomerGrpName = "X" });
            using (var d1b = JsonDocument.Parse(await r1b.Content.ReadAsStringAsync()))
                Assert.Equal("Mã nhóm khách hàng trống!", d1b.RootElement.GetProperty("Messages")[0].GetString());

            // Ca 3 — mã không tồn tại: nguồn 331-335 → Success=true, in CustomerGrpName (LỖI NGUỒN GIỮ NGUYÊN).
            var r2 = await http.PostAsJsonAsync("/api/mst_customergroup/edit",
                new { CustomerGrpCode = "KH-KHONG-CO", CustomerGrpName = "Nhóm lạ" });
            Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
            {
                Assert.True(d2.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Mã nhóm khách hàng 'Nhóm lạ' không có trong hệ thống!", d2.RootElement.GetProperty("Messages")[0].GetString());
            }
            Assert.Equal(3, await CountAsync(dbPath));   // không ghi gì

            // Ca 4 — thành công round-trip: KH-LE (tên "Khách lẻ", FlagActive "1") → đổi tên/mô tả/trạng thái.
            var r3 = await http.PostAsJsonAsync("/api/mst_customergroup/edit", new
            {
                CustomerGrpCode = "KH-LE",
                CustomerGrpName = "Khách lẻ VIP",
                CustomerGrpDesc = "Khách mua lẻ ưu tiên",
                FlagActive = "0",
                OrgID = "1001",
            });
            Assert.Equal(HttpStatusCode.OK, r3.StatusCode);
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                Assert.True(d3.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Sửa nhóm khách hàng thành công!", d3.RootElement.GetProperty("Messages")[0].GetString());
            }

            var row = await ReadRowAsync(dbPath, "KH-LE");
            Assert.Equal("Khách lẻ VIP", row.Name);
            Assert.Equal("Khách mua lẻ ưu tiên", row.Desc);
            Assert.Equal("0", row.Flag);
            Assert.Equal("1001", row.OrgID);

            // Không thêm/xoá dòng: vẫn 3 dòng danh mục.
            Assert.Equal(3, await CountAsync(dbPath));

            // Ca 5 — mã gửi chữ thường vẫn khớp (nguồn SQL Server collation không phân biệt hoa/thường).
            var r4 = await http.PostAsJsonAsync("/api/mst_customergroup/edit",
                new { CustomerGrpCode = "kh-dl", CustomerGrpName = "Đại lý cấp 1", CustomerGrpDesc = "Đại lý", FlagActive = "1", OrgID = "1001" });
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
                Assert.Equal("Sửa nhóm khách hàng thành công!", d4.RootElement.GetProperty("Messages")[0].GetString());
            var row2 = await ReadRowAsync(dbPath, "KH-DL");
            Assert.Equal("Đại lý cấp 1", row2.Name);
            Assert.Equal("1", row2.Flag);
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

    [Fact]
    public async Task ImagesSeeder_IsIdempotent()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        await db.Database.EnsureCreatedAsync();
        await PortSchema.EnsureAsync(db);
        await PortSchema.EnsureAsync(db);
        Assert.Equal(3, await db.Set<Mst_CustomerGroupImages>().CountAsync());
    }
}

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #306 Mst_CustomerGroupController/Delete — nguồn Mst_CustomerGroupController.cs:480-565
/// (→ WA_Mst_CustomerGroup_Get → Mst_CustomerGroup_Get → Mst_CustomerGroup_GetX, Customer.cs:11219-12320;
///  → WA_Mst_CustomerGroup_Delete = dịch vụ NGOÀI, bỏ qua; → WA_Mst_CustomerGroup_Save = GAP, xem conflicts\306_conflict.md).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/mst_customergroup/delete.
/// Số kỳ vọng tính tay từ nguồn + seeder (Mst_CustomerGroupSeeder: 3 dòng KH-LE/KH-DL/KH-XN, OrgID "1001";
/// Mst_CustomerGroupImagesSeeder: 3 dòng ảnh cùng mã nhóm).
/// </summary>
public class Mst_CustomerGroupDeleteTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"mst_customergroup_delete_{Guid.NewGuid():N}.db");
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
    public async Task Delete_EmptyBody_UnknownCode_And_Found()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — body rỗng: nguồn 491 sai điều kiện → else 552-556 → Success=false "Mã nhóm khách hàng trống!".
            var r1 = await http.PostAsync("/api/mst_customergroup/delete", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                Assert.False(d1.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Mã nhóm khách hàng trống!", d1.RootElement.GetProperty("Messages")[0].GetString());
            }

            // Ca 2 — CustomerGrpCode rỗng chuỗi: CUtils.IsNullOrEmpty → cùng nhánh else.
            var r1b = await http.PostAsJsonAsync("/api/mst_customergroup/delete", new { CustomerGrpCode = "" });
            using (var d1b = JsonDocument.Parse(await r1b.Content.ReadAsStringAsync()))
            {
                Assert.False(d1b.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Mã nhóm khách hàng trống!", d1b.RootElement.GetProperty("Messages")[0].GetString());
            }

            // Ca 3 — mã không tồn tại: nguồn 546-550 → Success giữ false, in đúng MÃ gửi lên.
            var r2 = await http.PostAsJsonAsync("/api/mst_customergroup/delete", new { CustomerGrpCode = "KH-KHONG-CO" });
            Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
            {
                Assert.False(d2.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Mã nhóm khách hàng 'KH-KHONG-CO' không có trong hệ thống!", d2.RootElement.GetProperty("Messages")[0].GetString());
            }

            // Ca 4 — tìm thấy: nguồn 540-544 → Success=true, "Xóa nhóm khách hàng 'KH-DL' thành công!".
            var r3 = await http.PostAsJsonAsync("/api/mst_customergroup/delete", new { CustomerGrpCode = "KH-DL" });
            Assert.Equal(HttpStatusCode.OK, r3.StatusCode);
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                Assert.True(d3.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Xóa nhóm khách hàng 'KH-DL' thành công!", d3.RootElement.GetProperty("Messages")[0].GetString());
            }

            // Ca 5 — mã gửi chữ thường vẫn khớp (nguồn SQL Server collation không phân biệt hoa/thường, toán tử "=").
            var r4 = await http.PostAsJsonAsync("/api/mst_customergroup/delete", new { CustomerGrpCode = "kh-xn" });
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
            {
                Assert.True(d4.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Xóa nhóm khách hàng 'kh-xn' thành công!", d4.RootElement.GetProperty("Messages")[0].GetString());
            }

            // GAP (conflicts\306_conflict.md): bước Save (FlagIsDelete = "1") chưa port vì excerpt thiếu tầng API
            // `WA_Mst_CustomerGroup_Save` → dữ liệu KHÔNG bị xoá: vẫn 3 dòng nhóm + 3 dòng ảnh.
            Assert.Equal(3, await CountAsync(dbPath, "Mst_CustomerGroup"));
            Assert.Equal(3, await CountAsync(dbPath, "Mst_CustomerGroupImages"));
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

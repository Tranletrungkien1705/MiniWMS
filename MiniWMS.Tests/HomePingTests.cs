using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #21 HomeController/Ping — nguồn HomeController.cs:388-391: `return Json(true, JsonRequestBehavior.AllowGet);`
/// (không tham số, không guard, không mã lỗi, không bảng, không dịch vụ ngoài). Test khởi động server THẬT
/// trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/home/ping.
/// Số kỳ vọng tính tay từ nguồn: body JSON đúng `true`, không ghi bảng nào.
/// </summary>
public class HomePingTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"home_ping_{Guid.NewGuid():N}.db");
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

    [Fact]
    public async Task Ping_ReturnsTrue_AndWritesNothing()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — body rỗng: nguồn không có tham số → vẫn trả `true` (dòng 390).
            var r1 = await http.PostAsync("/api/home/ping", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                Assert.Equal(JsonValueKind.True, d1.RootElement.ValueKind);
                Assert.True(d1.RootElement.GetBoolean());
            }

            // Ca 2 — body có dữ liệu thừa: nguồn bỏ qua mọi tham số → kết quả y hệt.
            var r2 = await http.PostAsJsonAsync("/api/home/ping", new { Anything = "x" });
            Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
                Assert.True(d2.RootElement.GetBoolean());

            // Action ĐỌC: không ghi bảng nào — Mst_Sys_Config vẫn đúng 3 dòng seed.
            await using var conn = new SqliteConnection($"Data Source={dbPath}");
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "select count(*) from Mst_Sys_Config";
            Assert.Equal(3, Convert.ToInt32(await cmd.ExecuteScalarAsync()));
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

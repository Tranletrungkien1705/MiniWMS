using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #22 HomeController/Notification — nguồn HomeController.cs:394-504: dựng `objInosNotificationResult` rỗng (dòng 399-407),
/// nếu có userState thì InitSSO (dòng 422) + `NotificationService.GetNotifications(...)` (dòng 430-431, DỊCH VỤ NGOÀI),
/// lấy phần tử UNREAD đầu tiên (dòng 436-440), rồi trả `Json(objNotificationResult, AllowGet)` (dòng 502).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/home/notification.
/// Số kỳ vọng tính tay từ nguồn: mọi số đếm = 0, List rỗng, Success = true, không ghi bảng nào.
/// </summary>
public class HomeNotificationTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"home_notification_{Guid.NewGuid():N}.db");
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
    public async Task Notification_EmptyResult_FromExternalService_AndWritesNothing()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — body rỗng: nguồn 419 `userState == null` → bỏ qua khối lấy thông báo, Success = true (dòng 446),
            // objInosNotificationResult giữ giá trị khởi tạo rỗng (dòng 399-407).
            var r1 = await http.PostAsync("/api/home/notification", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal("", root.GetProperty("ErrorCode").GetString());
                Assert.Equal("", root.GetProperty("ErrorDetail").GetString());
                Assert.Equal("", root.GetProperty("UrlLogin").GetString());
                var inos = root.GetProperty("InosNotificationResult");
                Assert.Equal(0, inos.GetProperty("PageIndex").GetInt32());
                Assert.Equal(0, inos.GetProperty("PageSize").GetInt32());
                Assert.Equal(0, inos.GetProperty("UnreadCount").GetInt32());
                Assert.Equal(0, inos.GetProperty("PageCount").GetInt32());
                Assert.Equal(0, inos.GetProperty("ItemCount").GetInt32());
                Assert.Equal(0, inos.GetProperty("List").GetArrayLength());
                Assert.Equal(0, root.GetProperty("ListInosNotification_UnRead").GetArrayLength());
                // Nguồn 500: DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") — 23 ký tự.
                Assert.Equal(23, root.GetProperty("DateTime").GetString()!.Length);
            }

            // Ca 2 — có userState + NetworkID: nguồn 430-431 gọi dịch vụ ngoài NotificationService (GAP) →
            // KHÔNG bịa thông báo: kết quả vẫn rỗng, Success = true, có ghi GAP.
            var r2 = await http.PostAsJsonAsync("/api/home/notification", new { HasUserState = true, NetworkID = "1001" });
            Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
            {
                var root = d2.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                var inos = root.GetProperty("InosNotificationResult");
                Assert.Equal(0, inos.GetProperty("List").GetArrayLength());
                Assert.Equal(0, inos.GetProperty("UnreadCount").GetInt32());
                Assert.Equal(0, inos.GetProperty("ItemCount").GetInt32());
                Assert.Equal(0, root.GetProperty("ListInosNotification_UnRead").GetArrayLength());
                Assert.Contains("NotificationService.GetNotifications", root.GetProperty("Gap")[0].GetString()!);
            }

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

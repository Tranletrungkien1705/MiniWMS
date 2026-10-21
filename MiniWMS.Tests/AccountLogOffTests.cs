using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #7 AccountController/LogOff — nguồn AccountController.cs:953-974.
/// Phần port được: xoá cookie "accesstoken" (chỉ khi request có) + luôn xoá "ASP.NET_SessionId" (dòng 960-966).
/// Phần KHÔNG port được (Session/InitSSO/FormsAuth/redirect InosBaseUrl) → GAP + .parity-run\conflicts\7_conflict.md.
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm.
/// </summary>
public class AccountLogOffTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"account_logoff_{Guid.NewGuid():N}.db");
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

    private static string[] SetCookies(HttpResponseMessage r)
        => r.Headers.TryGetValues("Set-Cookie", out var v) ? v.ToArray() : Array.Empty<string>();

    [Fact]
    public async Task LogOff_NoCookie_SetsOnlySessionIdExpired_AndSuccess()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Nguồn 960: Request.Cookies["accesstoken"] == null → KHÔNG ghi cookie accesstoken.
            // Nguồn 965-966: LUÔN ghi ASP.NET_SessionId với Expires quá khứ.
            var res = await http.PostAsync("/api/account/logoff", null);
            Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);

            var cookies = SetCookies(res);
            Assert.DoesNotContain(cookies, c => c.StartsWith("accesstoken=", StringComparison.OrdinalIgnoreCase));
            var session = Assert.Single(cookies, c => c.StartsWith("ASP.NET_SessionId=", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("expires=", session, StringComparison.OrdinalIgnoreCase);

            // Nguồn không trả JSON (redirect); đích trả Success + RedirectUrl null (GAP, conflict #7).
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("RedirectUrl").ValueKind);
        }
        finally
        {
            p.Kill(true);
            try { File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public async Task LogOff_WithAccessTokenCookie_ExpiresBothCookies()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Nguồn 960-964: có cookie accesstoken → ghi đè cùng tên với Expires = Now-1 ngày.
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/account/logoff");
            req.Headers.Add("Cookie", "accesstoken=ABC123");
            var res = await http.SendAsync(req);
            Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);

            var cookies = SetCookies(res);
            var at = Assert.Single(cookies, c => c.StartsWith("accesstoken=", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("expires=", at, StringComparison.OrdinalIgnoreCase);
            var session = Assert.Single(cookies, c => c.StartsWith("ASP.NET_SessionId=", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("expires=", session, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            p.Kill(true);
            try { File.Delete(dbPath); } catch { }
        }
    }
}

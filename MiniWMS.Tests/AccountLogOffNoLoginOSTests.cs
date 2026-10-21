using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #9 AccountController/LogOffNoLoginOS — nguồn AccountController.cs:988-998.
/// Thao tác:
/// - Xoá cookies, luôn ghi đè cookie ASP.NET_SessionId hết hạn.
/// - RedirectToAction("Login", "Account") tương ứng RedirectUrl = "/Account/Login".
/// </summary>
public class AccountLogOffNoLoginOSTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"account_logoffnologinos_{Guid.NewGuid():N}.db");
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
        psi.Environment["SSO_AUTHORITY"] = "http://127.0.0.1:1";

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
    public async Task LogOffNoLoginOS_NoCookie_SetsSessionIdExpired_AndReturnsLoginRedirect()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var res = await http.PostAsync("/api/account/logoffnologinos", null);
            Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);

            var cookies = SetCookies(res);
            var session = Assert.Single(cookies, c => c.StartsWith("ASP.NET_SessionId=", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("expires=", session, StringComparison.OrdinalIgnoreCase);

            var body = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("/Account/Login", doc.RootElement.GetProperty("RedirectUrl").GetString());
        }
        finally
        {
            try { p.Kill(true); } catch { }
            try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public async Task LogOffNoLoginOS_WithClientCookies_ClearsCookiesAndReturnsSuccess()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/account/logoffnologinos");
            req.Headers.Add("Cookie", "custom_cookie=abc123xyz; token=test_token");

            var res = await http.SendAsync(req);
            Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);

            var cookies = SetCookies(res);
            var custom = Assert.Single(cookies, c => c.StartsWith("custom_cookie=", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("expires=", custom, StringComparison.OrdinalIgnoreCase);

            var token = Assert.Single(cookies, c => c.StartsWith("token=", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("expires=", token, StringComparison.OrdinalIgnoreCase);

            var session = Assert.Single(cookies, c => c.StartsWith("ASP.NET_SessionId=", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("expires=", session, StringComparison.OrdinalIgnoreCase);

            var body = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
            Assert.Equal("/Account/Login", doc.RootElement.GetProperty("RedirectUrl").GetString());
        }
        finally
        {
            try { p.Kill(true); } catch { }
            try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
        }
    }
}

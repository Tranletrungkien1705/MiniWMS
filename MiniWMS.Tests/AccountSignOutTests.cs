using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #8 AccountController/SignOut — nguồn AccountController.cs:977-983.
/// Nguồn: Session.Clear + FormsAuthentication.SignOut + return null (200 rỗng).
/// Đích: cả 2 thao tác hạ tầng KHÔNG có (GAP); action trả 200 + { Success = true }.
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm.
/// </summary>
public class AccountSignOutTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"account_signout_{Guid.NewGuid():N}.db");
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

    [Fact]
    public async Task SignOut_NoBody_ReturnsSuccess()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Nguồn 977-983: không tham số, không body; chỉ Session.Clear + FormsAuth.SignOut + return null.
            var res = await http.PostAsync("/api/account/signout", null);
            Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        }
        finally
        {
            p.Kill(true);
            try { File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public async Task SignOut_EmptyBody_ReturnsSuccess()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Body rỗng (action không nhận body) — vẫn 200 + Success=true.
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/account/signout")
            {
                Content = new StringContent("", System.Text.Encoding.UTF8, "application/json"),
            };
            var res = await http.SendAsync(req);
            Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.True(doc.RootElement.GetProperty("Success").GetBoolean());
        }
        finally
        {
            p.Kill(true);
            try { File.Delete(dbPath); } catch { }
        }
    }
}

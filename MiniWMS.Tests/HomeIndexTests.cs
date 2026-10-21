using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #18 HomeController/Index — nguồn HomeController.cs:20-330 (→ WA_OS_Inos_Org_GetMyOrgList,
/// WA_MstSv_Sys_User_Login, WA_Sys_User_GetForCurrentUser). Test khởi động server THẬT trên cổng trống
/// (FreePort.Get) + DB SQLite tạm, gọi POST /api/home/index.
/// Số kỳ vọng tính tay từ nguồn + seeder Mst_Sys_ConfigSeeder (3 dòng: 2 dòng NetworkID "1001", 1 dòng "1002").
/// </summary>
public class HomeIndexTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"home_index_{Guid.NewGuid():N}.db");
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
    public async Task Index_Guards_And_ConfigFilter()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — body rỗng: nguồn 22 `User.Identity.IsAuthenticated` = false → SignOut + Redirect(login) (dòng 324-329).
            var r1 = await http.PostAsync("/api/home/index", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.True(root.GetProperty("SignOut").GetBoolean());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("RedirectUrl").ValueKind);
                Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
            }

            // Ca 2 — đã đăng nhập nhưng KHÔNG có AccessToken: nguồn 51-56 gọi SSO (dịch vụ ngoài) → GAP, không có dữ liệu.
            var r2 = await http.PostAsJsonAsync("/api/home/index", new { IsAuthenticated = true });
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
            {
                var root = d2.RootElement;
                Assert.True(root.GetProperty("SignOut").GetBoolean());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
                Assert.Contains("GetCurrentUser", root.GetProperty("Gap")[0].GetString());
            }

            // Ca 3 — có token nhưng chưa có NetworkID/OrgID (WA_OS_Inos_Org_GetMyOrgList là dịch vụ ngoài) → GAP.
            var r3 = await http.PostAsJsonAsync("/api/home/index", new { IsAuthenticated = true, AccessToken = "tok" });
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                var root = d3.RootElement;
                Assert.False(root.GetProperty("SignOut").GetBoolean());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
                Assert.Contains("WA_OS_Inos_Org_GetMyOrgList", root.GetProperty("Gap")[0].GetString());
            }

            // Ca 4 — có NetworkID/OrgID nhưng thiếu UserCode (WA_MstSv_Sys_User_Login là dịch vụ ngoài) → GAP.
            var r4 = await http.PostAsJsonAsync("/api/home/index", new { IsAuthenticated = true, AccessToken = "tok", NetworkID = "1001", OrgID = "ORG01" });
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
            {
                var root = d4.RootElement;
                Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
                Assert.Contains("WA_MstSv_Sys_User_Login", root.GetProperty("Gap")[0].GetString());
            }

            // Ca 5 — đủ dữ liệu: nguồn 180-198 lọc Mst_Sys_Config.NetworkID = "1001" → 2 dòng (CFG01, CFG02).
            var r5 = await http.PostAsJsonAsync("/api/home/index", new { IsAuthenticated = true, AccessToken = "tok", UserCode = "u@x", NetworkID = "1001", OrgID = "ORG01" });
            Assert.Equal(HttpStatusCode.OK, r5.StatusCode);
            using (var d5 = JsonDocument.Parse(await r5.Content.ReadAsStringAsync()))
            {
                var root = d5.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal("/Dashboard/Index", root.GetProperty("RedirectUrl").GetString());
                var data = root.GetProperty("Data");
                Assert.Equal("1001", data.GetProperty("NetworkID").GetString());
                Assert.Equal("ORG01", data.GetProperty("OrgID").GetString());
                var cfgs = data.GetProperty("Mst_Sys_Config");
                Assert.Equal(2, cfgs.GetArrayLength());
                Assert.Equal("CFG01", cfgs[0].GetProperty("SysConfigID").GetString());
                Assert.Equal("CFG02", cfgs[1].GetProperty("SysConfigID").GetString());
            }

            // Ca 6 — bộ lọc khác: NetworkID = "1002" → 1 dòng (CFG03); mã mạng không tồn tại → 0 dòng.
            var r6 = await http.PostAsJsonAsync("/api/home/index", new { IsAuthenticated = true, AccessToken = "tok", UserCode = "u@x", NetworkID = "1002", OrgID = "ORG03" });
            using (var d6 = JsonDocument.Parse(await r6.Content.ReadAsStringAsync()))
                Assert.Equal(1, d6.RootElement.GetProperty("Data").GetProperty("Mst_Sys_Config").GetArrayLength());

            var r7 = await http.PostAsJsonAsync("/api/home/index", new { IsAuthenticated = true, AccessToken = "tok", UserCode = "u@x", NetworkID = "9999", OrgID = "ORG01" });
            using (var d7 = JsonDocument.Parse(await r7.Content.ReadAsStringAsync()))
                Assert.Equal(0, d7.RootElement.GetProperty("Data").GetProperty("Mst_Sys_Config").GetArrayLength());

            // Action ĐỌC: không ghi gì — vẫn 3 dòng cấu hình.
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

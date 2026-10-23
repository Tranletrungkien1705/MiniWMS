using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #87 InvF_InventoryInController/Approve — nguồn InvF_InventoryInController.cs:1677-1730
/// → WA_InvF_InventoryIn_Appr (API InvFInventoryInController.cs:164-233)
/// → WAS_InvF_InventoryIn_Appr_New20230609 (Form.cs:181176-181253)
/// → InvF_InventoryIn_Appr_New20230609 (Form.cs:181255-181424)
/// → InvF_InventoryIn_ApprX_New20230609 (Form.cs:181429-182307).
/// </summary>
public class InvF_InventoryInApproveTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_appr_{Guid.NewGuid():N}.db");
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
    public async Task Approve_Guards_And_RoundTrip()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — Phân quyền: IsSysAdmin = false, thiếu quyền BTN_INVF_INVENTORYIN_APPROVE
            var r1 = await http.PostAsJsonAsync("/api/invf_inventoryin/approve",
                new { IsSysAdmin = false, ListSysAccess = new List<string>() });
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("/AccessDeny/Index", root.GetProperty("RedirectUrl").GetString());
            }

            // Ca 2 — Danh sách phiếu rỗng
            var r2 = await http.PostAsJsonAsync("/api/invf_inventoryin/approve",
                new { IsSysAdmin = true, ListInvF_InventoryIn = new List<object>() });
            Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
            {
                var root = d2.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_Appr", root.GetProperty("ErrorCode").GetString());
            }

            // Ca 3 — Phiếu không tồn tại trong DB
            var r3 = await http.PostAsJsonAsync("/api/invf_inventoryin/approve",
                new
                {
                    IsSysAdmin = true,
                    ListInvF_InventoryIn = new[]
                    {
                        new { IF_InvInNo = "PNK_NOT_EXIST" }
                    }
                });
            Assert.Equal(HttpStatusCode.BadRequest, r3.StatusCode);
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                var root = d3.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_CheckDB_InvInNoNotFound", root.GetProperty("ErrorCode").GetString());
            }

            // Ca 4 — Phiếu trạng thái không phải PENDING (PNK002 có trạng thái APPROVE)
            var r4 = await http.PostAsJsonAsync("/api/invf_inventoryin/approve",
                new
                {
                    IsSysAdmin = true,
                    ListInvF_InventoryIn = new[]
                    {
                        new { IF_InvInNo = "PNK002" }
                    }
                });
            Assert.Equal(HttpStatusCode.BadRequest, r4.StatusCode);
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
            {
                var root = d4.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_CheckDB_StatusNotMatched", root.GetProperty("ErrorCode").GetString());
            }

            // Ca 5 — Duyệt thành công (PNK001 trạng thái PENDING ban đầu)
            var r5 = await http.PostAsJsonAsync("/api/invf_inventoryin/approve",
                new
                {
                    IsSysAdmin = true,
                    WaUserCode = "U01",
                    ListInvF_InventoryIn = new[]
                    {
                        new { IF_InvInNo = "PNK001", Remark = "Duyệt phiếu test tự động" }
                    }
                });
            Assert.Equal(HttpStatusCode.OK, r5.StatusCode);
            using (var d5 = JsonDocument.Parse(await r5.Content.ReadAsStringAsync()))
            {
                var root = d5.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal("/InvF_InventoryIn/Index", root.GetProperty("RedirectUrl").GetString());
            }

            // Ca 6 — Duyệt lại chính PNK001 sẽ bị chặn do trạng thái đã đổi thành APPROVE
            var r6 = await http.PostAsJsonAsync("/api/invf_inventoryin/approve",
                new
                {
                    IsSysAdmin = true,
                    ListInvF_InventoryIn = new[]
                    {
                        new { IF_InvInNo = "PNK001" }
                    }
                });
            Assert.Equal(HttpStatusCode.BadRequest, r6.StatusCode);
            using (var d6 = JsonDocument.Parse(await r6.Content.ReadAsStringAsync()))
            {
                var root = d6.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_CheckDB_StatusNotMatched", root.GetProperty("ErrorCode").GetString());
            }
        }
        finally
        {
            try { p.Kill(entireProcessTree: true); } catch { }
            http.Dispose();
            try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
        }
    }
}

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #85 InvF_InventoryInController/UpdAfterAppr — nguồn InvF_InventoryInController.cs:1330-1383
/// → WA_InvF_InventoryIn_UpdAfterAppr (API InvFInventoryInController.cs:236-305)
/// → WAS_InvF_InventoryIn_UpdAfterAppr (Form.cs:157400-157479)
/// → InvF_InventoryIn_UpdAfterAppr (Form.cs:157236-157398)
/// → InvF_InventoryIn_UpdAfterApprX (Form.cs:157083-157234).
/// </summary>
public class InvF_InventoryInUpdAfterApprTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_updafterappr_{Guid.NewGuid():N}.db");
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
    public async Task UpdAfterAppr_Guards_And_RoundTrip()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — Access Deny: IsSysAdmin = false, không có quyền
            var r1 = await http.PostAsJsonAsync("/api/invf_inventoryin/updafterappr",
                new { IsSysAdmin = false, ListSysAccess = new List<string>() });
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("/AccessDeny/Index", root.GetProperty("RedirectUrl").GetString());
            }

            // Ca 2 — Guard body/model rỗng: trả 400 Erridn.Skycic.Inventory.InvF_InventoryIn_UpdAfterAppr
            var r2 = await http.PostAsJsonAsync("/api/invf_inventoryin/updafterappr",
                new { IsSysAdmin = true });
            Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
            {
                var root = d2.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_UpdAfterAppr", root.GetProperty("ErrorCode").GetString());
            }

            // Ca 3 — Guard phiếu không tồn tại: trả 400 Erridn.Skycic.Inventory.InvF_InventoryIn_CheckDB_InvInNoNotFound
            var r3 = await http.PostAsJsonAsync("/api/invf_inventoryin/updafterappr",
                new
                {
                    IsSysAdmin = true,
                    Model = new
                    {
                        Obj_InvF_InventoryIn = new { IF_InvInNo = "PNK-NOT-EXIST" }
                    }
                });
            Assert.Equal(HttpStatusCode.BadRequest, r3.StatusCode);
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                var root = d3.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_CheckDB_InvInNoNotFound", root.GetProperty("ErrorCode").GetString());
            }

            // Ca 4 — Guard trạng thái không phải APPROVE: PNK001 đang là PENDING
            var r4 = await http.PostAsJsonAsync("/api/invf_inventoryin/updafterappr",
                new
                {
                    IsSysAdmin = true,
                    Model = new
                    {
                        Obj_InvF_InventoryIn = new { IF_InvInNo = "PNK001" }
                    }
                });
            Assert.Equal(HttpStatusCode.BadRequest, r4.StatusCode);
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
            {
                var root = d4.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_CheckDB_StatusNotMatched", root.GetProperty("ErrorCode").GetString());
            }

            // Ca 5 — Thành công round-trip & FlagRedirect = "1"
            var r5 = await http.PostAsJsonAsync("/api/invf_inventoryin/updafterappr",
                new
                {
                    IsSysAdmin = true,
                    WaUserCode = "U_TEST_01",
                    Model = new
                    {
                        FlagRedirect = "1",
                        Obj_InvF_InventoryIn = new
                        {
                            IF_InvInNo = "PNK002",
                            InvoiceNo = "HD002-NEW",
                            UserDeliver = "NGUYEN VAN A",
                            Remark = "Ghi chu da sua"
                        }
                    }
                });
            Assert.Equal(HttpStatusCode.OK, r5.StatusCode);
            using (var d5 = JsonDocument.Parse(await r5.Content.ReadAsStringAsync()))
            {
                var root = d5.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal("/InvF_InventoryIn/Detail", root.GetProperty("RedirectUrl").GetString());
            }

            // Kiểm tra DB thật đã cập nhật đúng giá trị
            await using var conn = new SqliteConnection($"Data Source={dbPath}");
            await conn.OpenAsync();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT InvoiceNo, UserDeliver, Remark, LogLUBy FROM InvF_InventoryIn WHERE IF_InvInNo = 'PNK002'";
                await using var reader = await cmd.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.Equal("HD002-NEW", reader.GetString(0));
                Assert.Equal("NGUYEN VAN A", reader.GetString(1));
                Assert.Equal("Ghi chu da sua", reader.GetString(2));
                Assert.Equal("U_TEST_01", reader.GetString(3));
            }
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

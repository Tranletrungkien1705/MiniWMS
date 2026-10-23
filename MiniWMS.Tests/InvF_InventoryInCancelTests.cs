using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #88 InvF_InventoryInController/Cancel — nguồn InvF_InventoryInController.cs:1735-1788
/// → WA_InvF_InventoryIn_Cancel (API InvFInventoryInController.cs:308-377)
/// → WAS_InvF_InventoryIn_Cancel (Form.cs:157869-157947)
/// → InvF_InventoryIn_Cancel (Form.cs:157710-157866)
/// → InvF_InventoryIn_CancelX (Form.cs:157480-157708).
/// </summary>
public class InvF_InventoryInCancelTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_cancel_{Guid.NewGuid():N}.db");
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
    public async Task Cancel_Guards_And_RoundTrip()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — Phân quyền: IsSysAdmin = false, thiếu quyền BTN_INVF_INVENTORYIN_CANCEL.
            var r1 = await http.PostAsJsonAsync("/api/invf_inventoryin/cancel",
                new { IsSysAdmin = false, ListSysAccess = new List<string>() });
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("/AccessDeny/Index", root.GetProperty("RedirectUrl").GetString());
            }

            // Ca 2 — Body không có danh sách: 400 Erridn.Skycic.Inventory.InvF_InventoryIn_Cancel.
            var r2 = await http.PostAsJsonAsync("/api/invf_inventoryin/cancel",
                new { IsSysAdmin = true });
            Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
            {
                var root = d2.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_Cancel", root.GetProperty("ErrorCode").GetString());
            }

            // Ca 3 — Phiếu không tồn tại.
            var r3 = await http.PostAsJsonAsync("/api/invf_inventoryin/cancel",
                new
                {
                    IsSysAdmin = true,
                    WaUserCode = "U_TEST",
                    ListInvF_InventoryIn = new[] { new { IF_InvInNo = "PNK-NOT-EXIST" } }
                });
            Assert.Equal(HttpStatusCode.BadRequest, r3.StatusCode);
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                var root = d3.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_CheckDB_InvInNoNotFound", root.GetProperty("ErrorCode").GetString());
            }

            // Ca 4 — Phiếu PNK002 đang APPROVE (không phải PENDING).
            var r4 = await http.PostAsJsonAsync("/api/invf_inventoryin/cancel",
                new
                {
                    IsSysAdmin = true,
                    WaUserCode = "U_TEST",
                    ListInvF_InventoryIn = new[] { new { IF_InvInNo = "PNK002" } }
                });
            Assert.Equal(HttpStatusCode.BadRequest, r4.StatusCode);
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
            {
                var root = d4.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_CheckDB_StatusNotMatched", root.GetProperty("ErrorCode").GetString());
            }

            // Ca 5 — Mảng rỗng: gate trả Success = true (không lỗi, không xoá gì).
            var r5 = await http.PostAsJsonAsync("/api/invf_inventoryin/cancel",
                new { IsSysAdmin = true, ListInvF_InventoryIn = new List<object>() });
            Assert.Equal(HttpStatusCode.OK, r5.StatusCode);
            using (var d5 = JsonDocument.Parse(await r5.Content.ReadAsStringAsync()))
            {
                Assert.True(d5.RootElement.GetProperty("Success").GetBoolean());
            }

            // Ca 6 — Huỷ thành công PNK001 (đang PENDING) + round-trip.
            var r6 = await http.PostAsJsonAsync("/api/invf_inventoryin/cancel",
                new
                {
                    IsSysAdmin = true,
                    WaUserCode = "U_TEST_01",
                    ListInvF_InventoryIn = new[] { new { IF_InvInNo = "pnk001", Remark = "Huỷ do sai số" } }
                });
            Assert.Equal(HttpStatusCode.OK, r6.StatusCode);
            using (var d6 = JsonDocument.Parse(await r6.Content.ReadAsStringAsync()))
            {
                var root = d6.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal("/api/invf_inventoryin/index", root.GetProperty("RedirectUrl").GetString());
            }

            // Kiểm tra DB: header đã CANCEL + CancelBy + CancelDTimeUTC + Remark + LogLUBy.
            await using var conn = new SqliteConnection($"Data Source={dbPath}");
            await conn.OpenAsync();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT IF_InvInStatus, CancelBy, CancelDTimeUTC, Remark, LogLUBy FROM InvF_InventoryIn WHERE IF_InvInNo = 'PNK001'";
                await using var reader = await cmd.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.Equal("CANCEL", reader.GetString(0));
                Assert.Equal("U_TEST_01", reader.GetString(1));
                Assert.False(reader.IsDBNull(2));
                Assert.Equal("Huỷ do sai số", reader.GetString(3));
                Assert.Equal("U_TEST_01", reader.GetString(4));
            }

            // Dtl PNK001 đã chuyển CANCEL.
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT IF_InvInStatusDtl FROM InvF_InventoryInDtl WHERE IF_InvInNo = 'PNK001'";
                await using var reader = await cmd.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.Equal("CANCEL", reader.GetString(0));
            }

            // BalanceFormBlock PNK001/KHO01 đã bị xoá.
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Inv_InventoryBalanceFormBlock WHERE RefNo = 'PNK001' AND InvCode = 'KHO01'";
                var cnt = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                Assert.Equal(0L, cnt);
            }
            // BalanceFormBlock PNK001/KHO02 (khác InvCode) vẫn còn.
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Inv_InventoryBalanceFormBlock WHERE RefNo = 'PNK001' AND InvCode = 'KHO02'";
                var cnt = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                Assert.Equal(1L, cnt);
            }
            // BalanceFormBlock PNK002 (khác RefNo) vẫn còn.
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Inv_InventoryBalanceFormBlock WHERE RefNo = 'PNK002'";
                var cnt = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                Assert.Equal(1L, cnt);
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

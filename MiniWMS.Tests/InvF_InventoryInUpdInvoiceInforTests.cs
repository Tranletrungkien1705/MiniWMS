using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #94 InvF_InventoryInController/UpdInvoiceInfor — nguồn InvF_InventoryInController.cs:3219-3257
/// → WA_InvF_InventoryIn_UpdInvoiceInfor (API InvFInventoryInController.cs:1102-1171)
/// → WAS_InvF_InventoryIn_UpdInvoiceInfor (Form.cs:179667-179747)
/// → InvF_InventoryIn_UpdInvoiceInfor_New20241109 (Form.cs:180070-180235)
/// → InvF_InventoryIn_UpdInvoiceInforX_New20241109 (Form.cs:180237-180397).
/// Số kỳ vọng tính tay từ nguồn + seeder `InvF_InventoryInMasterSeeder` (PNK001/PNK002/PNK003):
///   - model null → 400 "Erridn.Skycic.Inventory.InvF_InventoryIn_UpdInvoiceInfor"
///   - IF_InvInNo không tồn tại → "Erridn.Skycic.Inventory.InvF_InventoryIn_CheckDB_InvInNoNotFound"
///   - IF_InvInNo rỗng (StdParam → null) → cũng NotFound (SQL `= NULL` không trả dòng)
///   - PNK001 (PENDING), CheckDB với "APPROVE" ⇒ "APPROVE".Contains("PENDING") = false → StatusNotMatched
///   - PNK002 (APPROVE) cập nhật: InvoiceNo="HD002-NEW94", InvoiceSign="KY SO 94",
///     BusinessGroupCode="bg02" → StdParam viết HOA = "BG02"; LUBy = LogLUBy = "U_TEST_94".
/// </summary>
public class InvF_InventoryInUpdInvoiceInforTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_updinv_{Guid.NewGuid():N}.db");
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
    public async Task UpdInvoiceInfor_Guards_And_RoundTrip()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — Model null → mã mặc định
            var r1 = await http.PostAsJsonAsync("/api/invf_inventoryin/updinvoiceinfor", new { });
            Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                Assert.False(d1.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_UpdInvoiceInfor",
                    d1.RootElement.GetProperty("ErrorCode").GetString());
            }

            // Ca 2 — IF_InvInNo không tồn tại
            var r2 = await http.PostAsJsonAsync("/api/invf_inventoryin/updinvoiceinfor",
                new { Model = new { IF_InvInNo = "PNK-NOT-EXIST" } });
            Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
            {
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_CheckDB_InvInNoNotFound",
                    d2.RootElement.GetProperty("ErrorCode").GetString());
            }

            // Ca 3 — IF_InvInNo rỗng → StdParam → null → NotFound
            var r3 = await http.PostAsJsonAsync("/api/invf_inventoryin/updinvoiceinfor",
                new { Model = new { IF_InvInNo = "" } });
            Assert.Equal(HttpStatusCode.BadRequest, r3.StatusCode);
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_CheckDB_InvInNoNotFound",
                    d3.RootElement.GetProperty("ErrorCode").GetString());
            }

            // Ca 4 — PNK001 PENDING không phải APPROVE
            var r4 = await http.PostAsJsonAsync("/api/invf_inventoryin/updinvoiceinfor",
                new { Model = new { IF_InvInNo = "PNK001" } });
            Assert.Equal(HttpStatusCode.BadRequest, r4.StatusCode);
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
            {
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_CheckDB_StatusNotMatched",
                    d4.RootElement.GetProperty("ErrorCode").GetString());
            }

            // Ca 5 — PNK002 APPROVE round-trip
            var r5 = await http.PostAsJsonAsync("/api/invf_inventoryin/updinvoiceinfor", new
            {
                WaUserCode = "U_TEST_94",
                Model = new
                {
                    IF_InvInNo = "PNK002",
                    InvoiceNo = "HD002-NEW94",
                    InvoiceSign = "KY SO 94",
                    BusinessGroupCode = "bg02"
                }
            });
            Assert.Equal(HttpStatusCode.OK, r5.StatusCode);
            using (var d5 = JsonDocument.Parse(await r5.Content.ReadAsStringAsync()))
            {
                var root = d5.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal("/InvF_InventoryIn/Index", root.GetProperty("RedirectUrl").GetString());
                var msgs = root.GetProperty("Messages").EnumerateArray().Select(x => x.GetString()).ToList();
                Assert.Contains("Sửa thông tin hóa đơn thành công!", msgs);
            }

            // DB thật đã cập nhật đúng giá trị
            await using var conn = new SqliteConnection($"Data Source={dbPath}");
            await conn.OpenAsync();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT InvoiceNo, InvoiceSign, BusinessGroupCode, LUBy, LogLUBy " +
                                  "FROM InvF_InventoryIn WHERE IF_InvInNo = 'PNK002'";
                await using var reader = await cmd.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.Equal("HD002-NEW94", reader.GetString(0));
                Assert.Equal("KY SO 94", reader.GetString(1));
                Assert.Equal("BG02", reader.GetString(2));   // StdParam viết hoa
                Assert.Equal("U_TEST_94", reader.GetString(3));
                Assert.Equal("U_TEST_94", reader.GetString(4));
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

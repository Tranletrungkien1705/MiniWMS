using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #26 Inv_CostPriceHistController/Inv_CostPriceHist_Calc — nguồn Inv_CostPriceHistController.cs:191-265
/// (→ Mst_InventoryService.Instance.WA_Mst_Inventory_GetInvCodeLeaf dòng 242 → API MstInventoryController.cs:465-536
///  → biz MasterData.cs:11805-11902 → MasterData.cs:11846-11863).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/inv_costpricehist/inv_costpricehist_calc.
///
/// Số kỳ vọng tính tay từ nguồn + seeder Mst_InventorySeeder (Order 12): KHO01, KHO02, KHO03 — cả 3 FlagActive = "1".
/// </summary>
public class InvCostPriceHistCalcTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"inv_costpricehist_calc_{Guid.NewGuid():N}.db");
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
    public async Task Calc_Guards_And_InventoryList()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — body rỗng: SysAccessCheckDeny(userState null) = false (BaseController.cs:3245-3269)
            //          → RedirectToAction("Index", "AccessDeny") (Inv_CostPriceHistController.cs:193-197).
            var r1 = await http.PostAsync("/api/inv_costpricehist/inv_costpricehist_calc", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal("/AccessDeny/Index", root.GetProperty("RedirectUrl").GetString());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("ListMst_Inventory").ValueKind);
                Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
            }

            // Ca 2 — có quyền nhưng ListSysAccess không chứa MNU_INV_COSTPRICEHIST_CALC → vẫn bị chặn.
            var r2 = await http.PostAsJsonAsync("/api/inv_costpricehist/inv_costpricehist_calc",
                new { ListSysAccess = new[] { "MNU_INV_COSTPRICEHIST" } });
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
                Assert.Equal("/AccessDeny/Index", d2.RootElement.GetProperty("RedirectUrl").GetString());

            // Ca 3 — có quyền (không phân biệt hoa/thường) nhưng KHÔNG có cấu hình phiên (dòng 209) → danh sách kho RỖNG.
            var r3 = await http.PostAsJsonAsync("/api/inv_costpricehist/inv_costpricehist_calc",
                new { ListSysAccess = new[] { "mnu_inv_costpricehist_calc" } });
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                var root = d3.RootElement;
                Assert.Equal(JsonValueKind.Null, root.GetProperty("RedirectUrl").ValueKind);
                Assert.Equal(0, root.GetProperty("ListMst_Inventory").GetArrayLength());
                Assert.Equal(0, root.GetProperty("Data").GetArrayLength());
            }

            // Ca 4 — cấu hình có nhưng KHÔNG dòng nào chứa "CALCUPINVBYINVCODE" (dòng 211) → danh sách kho RỖNG.
            var r4 = await http.PostAsJsonAsync("/api/inv_costpricehist/inv_costpricehist_calc", new
            {
                IsSysAdmin = true,
                Lst_Mst_Sys_Config = new[] { new { SysConfigID = "CFG01", FlagActive = "1" } }
            });
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
                Assert.Equal(0, d4.RootElement.GetProperty("ListMst_Inventory").GetArrayLength());

            // Ca 5 — cấu hình tính giá vốn FlagActive != "1" (dòng 217) → kho mặc định { InvCode = "I", InvName = "I" } (dòng 251-256).
            var r5 = await http.PostAsJsonAsync("/api/inv_costpricehist/inv_costpricehist_calc", new
            {
                IsSysAdmin = true,
                Lst_Mst_Sys_Config = new[] { new { SysConfigID = "CALCUPINVBYINVCODE", FlagActive = "0" } }
            });
            using (var d5 = JsonDocument.Parse(await r5.Content.ReadAsStringAsync()))
            {
                var list = d5.RootElement.GetProperty("ListMst_Inventory");
                Assert.Equal(1, list.GetArrayLength());
                Assert.Equal("I", list[0].GetProperty("InvCode").GetString());
                Assert.Equal("I", list[0].GetProperty("InvName").GetString());
            }

            // Ca 6 — cấu hình tính giá vốn FlagActive = "1" → where Mst_Inventory.FlagActive='1' (dòng 236)
            //          → 3 dòng seed (KHO01, KHO02, KHO03).
            var r6 = await http.PostAsJsonAsync("/api/inv_costpricehist/inv_costpricehist_calc", new
            {
                IsSysAdmin = true,
                Lst_Mst_Sys_Config = new[] { new { SysConfigID = "CALCUPINVBYINVCODE", FlagActive = "1" } }
            });
            Assert.Equal(HttpStatusCode.OK, r6.StatusCode);
            using (var d6 = JsonDocument.Parse(await r6.Content.ReadAsStringAsync()))
            {
                var root = d6.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                var list = root.GetProperty("ListMst_Inventory");
                Assert.Equal(3, list.GetArrayLength());
                Assert.Equal("KHO01", list[0].GetProperty("InvCode").GetString());
                Assert.Equal("KHO02", list[1].GetProperty("InvCode").GetString());
                Assert.Equal("KHO03", list[2].GetProperty("InvCode").GetString());
                // Nguồn 264: View(new List<Inv_CostPriceHist>()) — luôn rỗng.
                Assert.Equal(0, root.GetProperty("Data").GetArrayLength());
            }

            // Ca 7 — bộ lọc khác: thêm 1 kho FlagActive = "0" → vẫn 3 dòng (chỉ kho đang hoạt động).
            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "insert into Mst_Inventory (InvCode, InvName, FlagIn_Out, FlagActive, LogLUBy) values ('KHO99', 'Kho ngừng', '1', '0', 'test')";
                Assert.Equal(1, await cmd.ExecuteNonQueryAsync());
            }
            var r7 = await http.PostAsJsonAsync("/api/inv_costpricehist/inv_costpricehist_calc", new
            {
                IsSysAdmin = true,
                Lst_Mst_Sys_Config = new[] { new { SysConfigID = "CALCUPINVBYINVCODE", FlagActive = "1" } }
            });
            using (var d7 = JsonDocument.Parse(await r7.Content.ReadAsStringAsync()))
                Assert.Equal(3, d7.RootElement.GetProperty("ListMst_Inventory").GetArrayLength());

            // Action ĐỌC: không ghi gì — vẫn 4 dòng Mst_Inventory.
            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "select count(*) from Mst_Inventory";
                Assert.Equal(4, Convert.ToInt32(await cmd.ExecuteScalarAsync()));
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

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #14 DashboardRptController/WG_Rpt_Summary_Out_Pivot — nguồn DashboardRptController.cs:353-474
/// (→ Mst_InventoryService.Instance.WA_Mst_Inventory_Get dòng 387 → ReportsService.Instance.WA_Rpt_Summary_In_Out_Pivot dòng 436,
/// InventoryAction = "OUT" dòng 433).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/dashboardrpt/wg_rpt_summary_out_pivot.
/// Số kỳ vọng tính tay từ nguồn + seeder Mst_InventorySeeder (3 dòng: KHO01/KHO02 FlagIn_Out="1", KHO03 FlagIn_Out="0").
/// GAP: tầng API/biz của WA_Rpt_Summary_In_Out_Pivot không có trong nguồn được trích ⇒ endpoint trả 400 GAP.NoBackend
/// (xem .parity-run\conflicts\13_conflict.md) — test khẳng định đúng hành vi đó, KHÔNG khẳng định số liệu báo cáo tự chế.
/// </summary>
public class DashboardRptSummaryOutPivotTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"dashboardrpt_outpivot_{Guid.NewGuid():N}.db");
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

    private static async Task<int> CountInOutAsync(string dbPath)
    {
        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "select count(*) from Mst_Inventory where FlagIn_Out = '1'";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    [Fact]
    public async Task WG_Rpt_Summary_Out_Pivot_EmptyBody_ReturnsGapNoBackend()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Seeder: 3 kho, 2 kho FlagIn_Out = "1" (KHO01, KHO02) — nguồn 365 lọc `FlagIn_Out = '1'`.
            Assert.Equal(2, await CountInOutAsync(dbPath));

            // Ca 1 — body rỗng: init mặc định "init" (nguồn 353) ⇒ dùng toàn bộ danh sách kho; sau đó gọi
            // WA_Rpt_Summary_In_Out_Pivot (nguồn 436) — tầng này KHÔNG có trong nguồn được trích ⇒ 400 GAP.NoBackend.
            var r1 = await http.PostAsync("/api/dashboardrpt/wg_rpt_summary_out_pivot", null);
            Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.DashboardRpt_WG_Rpt_Summary_Out_Pivot", root.GetProperty("ErrorCode").GetString());
                // PortResult.Fail nhận các cặp (tên, giá trị): "GAP.NoBackend" → "WA_Rpt_Summary_In_Out_Pivot".
                Assert.Equal("WA_Rpt_Summary_In_Out_Pivot", root.GetProperty("Params").GetProperty("GAP.NoBackend").GetString());
            }

            // Ca 2 — init != "init" + lstinvcode JSON hợp lệ: nguồn 397-408 dựng danh sách kho theo mã gửi lên
            // (không lọc FlagIn_Out) rồi vẫn gọi WA_Rpt_Summary_In_Out_Pivot ⇒ cùng 400 GAP.NoBackend.
            var r2 = await http.PostAsJsonAsync("/api/dashboardrpt/wg_rpt_summary_out_pivot",
                new { NetworkID = "1001", LstInvCode = "[\"KHO03\"]", Init = "search" });
            Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
                Assert.Equal("WA_Rpt_Summary_In_Out_Pivot", d2.RootElement.GetProperty("Params").GetProperty("GAP.NoBackend").GetString());

            // Ca 3 — init != "init" + lstinvcode rỗng: nguồn 397 JsonConvert.DeserializeObject<List<string>>("") ném ngoại lệ
            // (gate không bắt) ⇒ PortExceptionFilter trả 400 mã DashboardRpt_WG_Rpt_Summary_Out_Pivot.
            var r3 = await http.PostAsJsonAsync("/api/dashboardrpt/wg_rpt_summary_out_pivot",
                new { Init = "search", LstInvCode = "" });
            Assert.Equal(HttpStatusCode.BadRequest, r3.StatusCode);
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                var root = d3.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.DashboardRpt_WG_Rpt_Summary_Out_Pivot", root.GetProperty("ErrorCode").GetString());
                Assert.True(root.GetProperty("Params").TryGetProperty("Exception", out _));
            }
        }
        finally
        {
            http.Dispose();
            try { p.Kill(true); } catch { }
            SqliteConnection.ClearAllPools();
            foreach (var f in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
                try { if (File.Exists(f)) File.Delete(f); } catch { }
        }
    }
}

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #27 Inv_CostPriceHistController/LoadData — nguồn Inv_CostPriceHistController.cs:321-366
/// (→ GetPrdBase dòng 368-403 → WA_Mst_Product_Get → ProductCenter.MasterData.cs:5714-5872).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/inv_costpricehist/loaddata.
///
/// Số kỳ vọng tính tay từ nguồn + seeder Mst_ProductSeeder (6 dòng):
///   ORG01/SP01, ORG01/SP02, ORG01/SP03, ORG02/SP01 (ProductLevelSys null),
///   ORG01/SP01-L2A, ORG01/SP01-L2B (ProductLevelSys = "L2PRD", ProductCodeRoot = "SP01").
///   - Get_Base (dòng 867/871): ProductCode = x AND ProductLevelSys != "L2PRD" ⇒ SP01 → 2 dòng (ORG01, ORG02).
///   - Get_Level2 (dòng 885/889): ProductCodeRoot = x AND ProductLevelSys = "L2PRD" ⇒ SP01 → 2 dòng.
///   - LstPrdLeve2.AddRange(LstPrdBase) (dòng 350) ⇒ LstPrdLeve2 = 2 + 2 = 4.
/// </summary>
public class InvCostPriceHistLoadDataTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"inv_costpricehist_loaddata_{Guid.NewGuid():N}.db");
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
    public async Task LoadData_Guards_And_ProductLists()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — body rỗng: `CUtils.IsNullOrEmpty(productcode)` (dòng 326) ⇒ rơi xuống
            //          JsonViewError("Create", ...) với resultEntry.Success = false (dòng 323, 365).
            var r1 = await http.PostAsync("/api/inv_costpricehist/loaddata", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.False(root.GetProperty("Success").GetBoolean());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("Html").ValueKind);
            }

            // Ca 2 — productcode = "" (chuỗi rỗng) ⇒ cùng nhánh guard như body rỗng.
            var r2 = await http.PostAsJsonAsync("/api/inv_costpricehist/loaddata", new { ProductCode = "" });
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
                Assert.False(d2.RootElement.GetProperty("Success").GetBoolean());

            // Ca 3 — productcode = "SP01": Get_Base 2 dòng, Get_Level2 2 dòng, LstPrdLeve2 = 4 (dòng 350).
            var r3 = await http.PostAsJsonAsync("/api/inv_costpricehist/loaddata", new { ProductCode = "SP01" });
            Assert.Equal(HttpStatusCode.OK, r3.StatusCode);
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                var root = d3.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                var list = root.GetProperty("data");
                Assert.Equal(1, list.GetArrayLength());
                var item = list[0];
                Assert.Equal(2, item.GetProperty("LstPrdBase").GetArrayLength());
                Assert.Equal(4, item.GetProperty("LstPrdLeve2").GetArrayLength());
                // Get_Base: ProductCode = "SP01" ⇒ ORG01/SP01 và ORG02/SP01 (không có dòng L2PRD).
                var baseCodes = item.GetProperty("LstPrdBase").EnumerateArray()
                    .Select(x => x.GetProperty("OrgID").GetString() + "/" + x.GetProperty("ProductCode").GetString())
                    .OrderBy(x => x).ToList();
                Assert.Equal(new[] { "ORG01/SP01", "ORG02/SP01" }, baseCodes);
                // Get_Level2: ProductCodeRoot = "SP01" AND ProductLevelSys = "L2PRD" ⇒ SP01-L2A, SP01-L2B.
                var l2Codes = item.GetProperty("LstPrdLeve2").EnumerateArray()
                    .Where(x => x.GetProperty("ProductLevelSys").GetString() == "L2PRD")
                    .Select(x => x.GetProperty("ProductCode").GetString())
                    .OrderBy(x => x).ToList();
                Assert.Equal(new[] { "SP01-L2A", "SP01-L2B" }, l2Codes);
            }

            // Ca 4 — bộ lọc khác: productcode = "SP02" ⇒ Get_Base 1 dòng, Get_Level2 0 dòng, LstPrdLeve2 = 1.
            var r4 = await http.PostAsJsonAsync("/api/inv_costpricehist/loaddata", new { ProductCode = "SP02" });
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
            {
                var item = d4.RootElement.GetProperty("data")[0];
                Assert.Equal(1, item.GetProperty("LstPrdBase").GetArrayLength());
                Assert.Equal(1, item.GetProperty("LstPrdLeve2").GetArrayLength());
            }

            // Ca 5 — productcode không tồn tại: Get_Base rỗng ⇒ `Lst_Mst_Product[0]` (dòng 336) ném ngoại lệ
            //          ⇒ PortExceptionFilter trả 400 (nguồn: catch → Success = false).
            var r5 = await http.PostAsJsonAsync("/api/inv_costpricehist/loaddata", new { ProductCode = "KHONGCO" });
            Assert.Equal(HttpStatusCode.BadRequest, r5.StatusCode);
            using (var d5 = JsonDocument.Parse(await r5.Content.ReadAsStringAsync()))
            {
                Assert.False(d5.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.Inv_CostPriceHist_LoadData",
                    d5.RootElement.GetProperty("ErrorCode").GetString());
            }

            // Action ĐỌC: không ghi gì — vẫn 6 dòng Mst_Product.
            await using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "select count(*) from Mst_Product";
                Assert.Equal(6, Convert.ToInt32(await cmd.ExecuteScalarAsync()));
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

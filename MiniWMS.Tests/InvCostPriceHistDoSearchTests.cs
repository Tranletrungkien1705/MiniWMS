using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #24 Inv_CostPriceHistController/DoSearch_Inv_CostPriceHist — nguồn Inv_CostPriceHistController.cs:99-148
/// (→ Inv_CostPriceHistService.Instance.WA_Inv_CostPriceHist_Get dòng 134 → API InvCostPriceHistController.cs:163-232
///  → biz Form.cs:200118-200203 → Form.cs:200205-200351 → Form.cs:200498-200669 Inv_CostPriceHist_GetX_New20240916).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/inv_costpricehist/dosearch_inv_costpricehist.
///
/// Số kỳ vọng tính tay từ nguồn + seed:
///   Mst_Product (Order 14): SP01/SP02/SP03 @ ORG01, SP01 @ ORG02 (ProductCodeUser PT-LOC-DAU/PT-LOC-GIO/PT-BUGI).
///   Mst_Unit    (Order 15): U01 "Cái", U02 "Bộ" @ ORG01; U01 @ ORG02.
///   Mst_Inventory (Order 12): KHO01, KHO02, KHO03 (đều FlagActive = "1").
///   Inv_CostPriceHist (Order 200): CPH01..CPH05 — SP01@ORG01@KHO01 có 2 mốc (2026-01-01, 2026-06-01),
///     SP02@ORG01@KHO01 (2026-03-01), SP03@ORG01@KHO02 (2026-04-01), SP01@ORG02@KHO01 (2026-05-01).
///   ⇒ FlagIsCurrent != "0" (mặc định StdFlag("") = "0" ⇒ nhánh select distinct): 5 dòng.
///     FlagIsCurrent = "1" (nhánh group by + Max): 4 dòng (SP01@ORG01@KHO01 chỉ còn mốc 2026-06-01).
///     InvCode = "KHO02": 1 dòng. ProductCodeUser chứa "loc-dau": 3 dòng (2 mốc SP01@ORG01 + 1 dòng SP01@ORG02).
/// </summary>
public class InvCostPriceHistDoSearchTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"inv_costpricehist_search_{Guid.NewGuid():N}.db");
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

    private static async Task<int> CountAsync(string dbPath, string table)
    {
        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"select count(*) from {table}";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    [Fact]
    public async Task DoSearch_Guards_And_Filters()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1 — body rỗng: SysAccessCheckDeny(userState null) = false (BaseController.cs:3245-3269)
            //          → RedirectToAction("Index", "AccessDeny") (Inv_CostPriceHistController.cs:101-105).
            var r1 = await http.PostAsync("/api/inv_costpricehist/dosearch_inv_costpricehist", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal("/AccessDeny/Index", root.GetProperty("RedirectUrl").GetString());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("Data").ValueKind);
            }

            // Ca 2 — có quyền nhưng ListSysAccess không chứa MNU_RPT_INV_INVENTORYBALANCE_STORAGETIME → vẫn bị chặn.
            var r2 = await http.PostAsJsonAsync("/api/inv_costpricehist/dosearch_inv_costpricehist",
                new { ListSysAccess = new[] { "MNU_INV_COSTPRICEHIST" } });
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
                Assert.Equal("/AccessDeny/Index", d2.RootElement.GetProperty("RedirectUrl").GetString());

            // Ca 3 — có quyền (không phân biệt hoa/thường), body rỗng: FlagIsCurrent "" → StdFlag = "0" → nhánh select distinct
            //          (Form.cs:200561-200583) → 5 dòng seed.
            var r3 = await http.PostAsJsonAsync("/api/inv_costpricehist/dosearch_inv_costpricehist",
                new { ListSysAccess = new[] { "mnu_rpt_inv_inventorybalance_storagetime" } });
            Assert.Equal(HttpStatusCode.OK, r3.StatusCode);
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                var root = d3.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal(JsonValueKind.Null, root.GetProperty("RedirectUrl").ValueKind);
                Assert.Equal(5, root.GetProperty("Data").GetArrayLength());
            }

            // Ca 4 — FlagIsCurrent = "1" → nhánh group by + Max(EffectDTimeUTC) (Form.cs:200588-200610) → 4 dòng;
            //          SP01@ORG01@KHO01 chỉ còn mốc 2026-06-01.
            var r4 = await http.PostAsJsonAsync("/api/inv_costpricehist/dosearch_inv_costpricehist",
                new { IsSysAdmin = true, FlagIsCurrent = "1" });
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
            {
                var data = d4.RootElement.GetProperty("Data");
                Assert.Equal(4, data.GetArrayLength());
                var sp01 = data.EnumerateArray().Single(x =>
                    x.GetProperty("ProductCode").GetString() == "SP01" && x.GetProperty("OrgID").GetString() == "ORG01");
                Assert.StartsWith("2026-06-01", sp01.GetProperty("EffectDTimeUTC").GetString());
                // Cột join: mp_ProductCodeUser / mp_ProductName / mp_UnitCode / mu_UnitName / mi_InvCode / mi_InvName.
                Assert.Equal("PT-LOC-DAU", sp01.GetProperty("mp_ProductCodeUser").GetString());
                Assert.Equal("Lọc dầu động cơ", sp01.GetProperty("mp_ProductName").GetString());
                Assert.Equal("U01", sp01.GetProperty("mp_UnitCode").GetString());
                Assert.Equal("Cái", sp01.GetProperty("mu_UnitName").GetString());
                Assert.Equal("KHO01", sp01.GetProperty("mi_InvCode").GetString());
                Assert.Equal("Kho trung tâm Hà Nội", sp01.GetProperty("mi_InvName").GetString());
            }

            // Ca 5 — bộ lọc khác: InvCode = "KHO02" (toán tử `=`, StdParam viết hoa) → 1 dòng (SP03@ORG01@KHO02).
            var r5 = await http.PostAsJsonAsync("/api/inv_costpricehist/dosearch_inv_costpricehist",
                new { IsSysAdmin = true, InvCode = "kho02" });
            using (var d5 = JsonDocument.Parse(await r5.Content.ReadAsStringAsync()))
            {
                var data = d5.RootElement.GetProperty("Data");
                Assert.Equal(1, data.GetArrayLength());
                Assert.Equal("SP03", data[0].GetProperty("ProductCode").GetString());
                Assert.Equal("KHO02", data[0].GetProperty("InvCode").GetString());
            }

            // Ca 6 — ProductCodeUser chứa "loc-dau" (like '%x%', StdParam viết hoa) → 3 dòng (2 mốc SP01@ORG01 + SP01@ORG02).
            var r6 = await http.PostAsJsonAsync("/api/inv_costpricehist/dosearch_inv_costpricehist",
                new { IsSysAdmin = true, ProductCodeUser = "loc-dau" });
            using (var d6 = JsonDocument.Parse(await r6.Content.ReadAsStringAsync()))
                Assert.Equal(3, d6.RootElement.GetProperty("Data").GetArrayLength());

            // Ca 7 — ProductName chứa "gió" (StdStr, không viết hoa) → 1 dòng (SP02 "Lọc gió động cơ").
            var r7 = await http.PostAsJsonAsync("/api/inv_costpricehist/dosearch_inv_costpricehist",
                new { IsSysAdmin = true, ProductName = "gió" });
            using (var d7 = JsonDocument.Parse(await r7.Content.ReadAsStringAsync()))
            {
                var data = d7.RootElement.GetProperty("Data");
                Assert.Equal(1, data.GetArrayLength());
                Assert.Equal("SP02", data[0].GetProperty("ProductCode").GetString());
            }

            // Ca 8 — không khớp gì: InvCode = "KHONG-CO" → danh sách rỗng (nguồn 135-138 giữ list rỗng).
            var r8 = await http.PostAsJsonAsync("/api/inv_costpricehist/dosearch_inv_costpricehist",
                new { IsSysAdmin = true, InvCode = "KHONG-CO" });
            using (var d8 = JsonDocument.Parse(await r8.Content.ReadAsStringAsync()))
                Assert.Equal(0, d8.RootElement.GetProperty("Data").GetArrayLength());

            // Action ĐỌC: không ghi gì — vẫn 5 dòng Inv_CostPriceHist.
            Assert.Equal(5, await CountAsync(dbPath, "Inv_CostPriceHist"));
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

    /// <summary>
    /// Nguồn Form.cs:200637-200639: `left join Mst_Unit mu on t.OrgID = mu.OrgID and mp.UnitCode = mu.UnitCode` —
    /// không có đơn vị thì dòng VẪN trả về với mu_UnitName = null (không bị inner join loại).
    /// Kiểm bằng controller trực tiếp trên DB SQLite in-memory.
    /// </summary>
    [Fact]
    public async Task DoSearch_LeftJoinUnit_MissingUnit_ReturnsNullUnitName()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        await db.Database.EnsureCreatedAsync();
        await PortSchema.EnsureAsync(db);

        db.Set<Mst_Product>().Add(new Mst_Product { OrgID = "ORG01", ProductCode = "SPX", ProductCodeUser = "PT-X", ProductName = "Hàng X", UnitCode = "U99" });
        db.Set<Inv_CostPriceHist>().Add(new Inv_CostPriceHist { Id = "CPHX", OrgID = "ORG01", ProductCode = "SPX", InvCode = "KHO01", EffectDTimeUTC = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) });
        await db.SaveChangesAsync();

        var ctrl = new MiniWMS.Controllers.Port.Inv_CostPriceHistControllerPort(db);
        var r = await ctrl.DoSearch_Inv_CostPriceHist(new() { IsSysAdmin = true, InvCode = "KHO01" });
        var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(r);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);
        var row = doc.RootElement.GetProperty("Data").EnumerateArray()
            .Single(x => x.GetProperty("ProductCode").GetString() == "SPX");
        Assert.Equal("U99", row.GetProperty("mp_UnitCode").GetString());
        Assert.Equal(JsonValueKind.Null, row.GetProperty("mu_UnitName").ValueKind);
    }
}

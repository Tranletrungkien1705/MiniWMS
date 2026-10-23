using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Controllers.Port;
using MiniWMS.Data;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #32 InvF_InventoryCusReturnController/Index — nguồn InvF_InventoryCusReturnController.cs:28-110.
/// Phủ 5 ca P2. Ca 1-4: server THẬT + DB SQLite tạm (đầy đủ seeder khác chạy trước).
/// Ca 5: DbContext SQLite in-memory KHÔNG seed, không boot server ⇒ [] chứ không null.
/// Số dòng kỳ vọng: đếm bằng SQL trên CÙNG DB tạm (bảng dùng chung với hạng mục khác đã seed sẵn),
/// đồng thời chứng minh bộ lọc có tác dụng (thỏa &lt; tổng) và 2 bộ lọc ra 2 số khác nhau.
/// </summary>
public class InvFInventoryCusReturnIndexTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_cusreturn_{Guid.NewGuid():N}.db");
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

    private static async Task<int> CountAsync(string dbPath, string sql)
    {
        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Index_PageRecordcount_MatchSourceShape()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var expInv = await CountAsync(dbPath,
                "select count(*) from Mst_Inventory where lower(FlagActive)='1' and lower(FlagIn_Out)='1'");
            var expCus = await CountAsync(dbPath,
                "select count(*) from Mst_Customer where lower(FlagActive)='1'");
            var totInv = await CountAsync(dbPath, "select count(*) from Mst_Inventory");
            var totCus = await CountAsync(dbPath, "select count(*) from Mst_Customer");

            Assert.True(expInv < totInv, $"where Mst_Inventory phải loại ít nhất 1 dòng: {expInv}/{totInv}");
            Assert.True(expCus < totCus, $"where Mst_Customer phải loại ít nhất 1 dòng: {expCus}/{totCus}");
            Assert.NotEqual(expInv, expCus);

            var r1 = await http.PostAsync("/api/invf_inventorycusreturn/index", null);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using (var d1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync()))
            {
                var root = d1.RootElement;
                Assert.True(root.GetProperty("Success").GetBoolean());
                Assert.Equal(0, root.GetProperty("PageCur").GetInt32());
                Assert.Equal(10, root.GetProperty("Recordcount").GetInt32());
                Assert.Equal("0", root.GetProperty("StartCount").GetString());
                var pi = root.GetProperty("PageInfo");
                Assert.Equal(0, pi.GetProperty("PageIndex").GetInt32());
                Assert.Equal(10, pi.GetProperty("PageSize").GetInt32());
                Assert.Equal(0, pi.GetProperty("ItemCount").GetInt32());
                Assert.Equal(0, pi.GetProperty("PageCount").GetInt32());
                Assert.Equal(0, pi.GetProperty("DataList").GetArrayLength());
                Assert.Equal(expInv, root.GetProperty("Lst_Mst_Inventory").GetArrayLength());
                Assert.Equal(expCus, root.GetProperty("Lst_MstCustomer").GetArrayLength());
            }

            var r2 = await http.PostAsJsonAsync("/api/invf_inventorycusreturn/index", new { Page = 2, Recordcount = 5 });
            using (var d2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync()))
            {
                var root = d2.RootElement;
                Assert.Equal(2, root.GetProperty("PageCur").GetInt32());
                Assert.Equal(5, root.GetProperty("Recordcount").GetInt32());
                Assert.Equal("10", root.GetProperty("StartCount").GetString());
                var pi = root.GetProperty("PageInfo");
                Assert.Equal(2, pi.GetProperty("PageIndex").GetInt32());
                Assert.Equal(5, pi.GetProperty("PageSize").GetInt32());
                Assert.Equal(0, pi.GetProperty("DataList").GetArrayLength());
                Assert.Equal(expInv, root.GetProperty("Lst_Mst_Inventory").GetArrayLength());
                Assert.Equal(expCus, root.GetProperty("Lst_MstCustomer").GetArrayLength());
            }

            var r3 = await http.PostAsJsonAsync("/api/invf_inventorycusreturn/index", new { Page = -1, Recordcount = 7 });
            using (var d3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync()))
            {
                var root = d3.RootElement;
                Assert.Equal(-1, root.GetProperty("PageCur").GetInt32());
                Assert.Equal(7, root.GetProperty("Recordcount").GetInt32());
                Assert.Equal("-7", root.GetProperty("StartCount").GetString());
                var pi = root.GetProperty("PageInfo");
                Assert.Equal(-1, pi.GetProperty("PageIndex").GetInt32());
                Assert.Equal(7, pi.GetProperty("PageSize").GetInt32());
                Assert.Equal(0, pi.GetProperty("DataList").GetArrayLength());
                Assert.Equal(expInv, root.GetProperty("Lst_Mst_Inventory").GetArrayLength());
                Assert.Equal(expCus, root.GetProperty("Lst_MstCustomer").GetArrayLength());
            }

            var r4 = await http.PostAsJsonAsync("/api/invf_inventorycusreturn/index", new { Recordcount = 0 });
            using (var d4 = JsonDocument.Parse(await r4.Content.ReadAsStringAsync()))
            {
                var root = d4.RootElement;
                Assert.Equal(0, root.GetProperty("PageCur").GetInt32());
                Assert.Equal(0, root.GetProperty("Recordcount").GetInt32());
                Assert.Equal("0", root.GetProperty("StartCount").GetString());
                var pi = root.GetProperty("PageInfo");
                Assert.Equal(0, pi.GetProperty("PageIndex").GetInt32());
                Assert.Equal(0, pi.GetProperty("PageSize").GetInt32());
                Assert.Equal(expInv, root.GetProperty("Lst_Mst_Inventory").GetArrayLength());
                Assert.Equal(expCus, root.GetProperty("Lst_MstCustomer").GetArrayLength());
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

    [Fact]
    public async Task Index_EmptyDb_ReturnsEmptyListsNotNull()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        using var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();

        var ctrl = new InvF_InventoryCusReturnControllerPort(db);
        var res = await ctrl.Index(new InvF_InventoryCusReturnControllerPort.InvF_InventoryCusReturn_IndexReq());
        var ok = Assert.IsType<OkObjectResult>(res);
        var data = Assert.IsType<InvF_InventoryCusReturnControllerPort.InvF_InventoryCusReturn_IndexData>(ok.Value);
        Assert.True(data.Success);
        Assert.Equal(0, data.PageCur);
        Assert.Equal(10, data.Recordcount);
        Assert.Equal("0", data.StartCount);
        Assert.NotNull(data.PageInfo);
        Assert.Empty(data.PageInfo!.DataList);
        Assert.Equal(0, data.PageInfo.ItemCount);
        Assert.Equal(0, data.PageInfo.PageCount);
        Assert.Equal(0, data.PageInfo.PageIndex);
        Assert.Equal(10, data.PageInfo.PageSize);
        Assert.NotNull(data.Lst_Mst_Inventory);
        Assert.Empty(data.Lst_Mst_Inventory!);
        Assert.NotNull(data.Lst_MstCustomer);
        Assert.Empty(data.Lst_MstCustomer!);
    }
}

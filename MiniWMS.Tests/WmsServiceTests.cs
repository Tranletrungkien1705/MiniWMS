using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models;
using MiniWMS.Services;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// Test nghiệp vụ kho trên SQLite in-memory: tồn tính từ phiếu đã ghi sổ (In+/Out−/Transfer),
/// guard xuất/chuyển quá tồn, phiếu nháp không tính tồn, và định giá tồn theo giá vốn.
/// </summary>
public class WmsServiceTests
{
    private static (AppDbContext db, IWmsService svc, SqliteConnection conn) NewSvc()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        return (db, new WmsService(db), conn);
    }

    private static async Task<(int wh1, int wh2, int p1, int p2)> Seed(IWmsService svc)
    {
        var w1 = await svc.CreateWarehouseAsync(new Warehouse { Name = "Kho A", Code = "A" });
        var w2 = await svc.CreateWarehouseAsync(new Warehouse { Name = "Kho B", Code = "B" });
        var p1 = await svc.CreateProductAsync(new Product { Name = "Lọc dầu", Code = "P1", CostPrice = 100_000, MinStock = 10 });
        var p2 = await svc.CreateProductAsync(new Product { Name = "Bugi", Code = "P2", CostPrice = 90_000 });
        return (w1, w2, p1, p2);
    }

    private static async Task<int> Doc(IWmsService svc, DocType type, int? from, int? to, params (int pid, int qty)[] lines)
    {
        var d = new StockDoc { Type = type, FromWarehouseId = from, ToWarehouseId = to };
        return await svc.CreateDocAsync(d, lines.Select(l => (l.pid, l.qty, 0m, (string?)null)).ToList());
    }

    [Fact]
    public async Task PostIn_IncreasesBalance()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var (w1, _, p1, _) = await Seed(svc);
            var id = await Doc(svc, DocType.In, null, w1, (p1, 100));
            var (ok, _) = await svc.PostDocAsync(id);
            Assert.True(ok);
            var bal = await svc.BalancesAsync(w1);
            Assert.Equal(100, bal.Single(b => b.ProductId == p1).Qty);
        }
    }

    [Fact]
    public async Task DraftDoc_NotCountedInBalance()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var (w1, _, p1, _) = await Seed(svc);
            await Doc(svc, DocType.In, null, w1, (p1, 100));  // để Draft, không post
            var bal = await svc.BalancesAsync(w1);
            Assert.Empty(bal);
        }
    }

    [Fact]
    public async Task PostOut_DecreasesBalance()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var (w1, _, p1, _) = await Seed(svc);
            await svc.PostDocAsync(await Doc(svc, DocType.In, null, w1, (p1, 100)));
            await svc.PostDocAsync(await Doc(svc, DocType.Out, w1, null, (p1, 30)));
            var bal = await svc.BalancesAsync(w1);
            Assert.Equal(70, bal.Single(b => b.ProductId == p1).Qty);
        }
    }

    [Fact]
    public async Task OverIssue_Blocked()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var (w1, _, p1, _) = await Seed(svc);
            await svc.PostDocAsync(await Doc(svc, DocType.In, null, w1, (p1, 50)));
            var (ok, msg) = await svc.PostDocAsync(await Doc(svc, DocType.Out, w1, null, (p1, 80)));
            Assert.False(ok);
            Assert.Contains("Không đủ tồn", msg);
        }
    }

    [Fact]
    public async Task Transfer_MovesStockBetweenWarehouses()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var (w1, w2, p1, _) = await Seed(svc);
            await svc.PostDocAsync(await Doc(svc, DocType.In, null, w1, (p1, 100)));
            await svc.PostDocAsync(await Doc(svc, DocType.Transfer, w1, w2, (p1, 40)));
            Assert.Equal(60, (await svc.BalancesAsync(w1)).Single(b => b.ProductId == p1).Qty);
            Assert.Equal(40, (await svc.BalancesAsync(w2)).Single(b => b.ProductId == p1).Qty);
        }
    }

    [Fact]
    public async Task InventoryValue_UsesCostPrice()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var (w1, _, p1, p2) = await Seed(svc);
            await svc.PostDocAsync(await Doc(svc, DocType.In, null, w1, (p1, 10), (p2, 20)));
            // 10×100.000 + 20×90.000 = 1.000.000 + 1.800.000 = 2.800.000
            var dash = await svc.DashboardAsync();
            Assert.Equal(2_800_000, dash.InventoryValue);
            Assert.Equal(30, dash.TotalOnHand);
        }
    }

    [Fact]
    public async Task LowStock_FlaggedWhenBelowMin()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var (w1, _, p1, _) = await Seed(svc);       // p1 MinStock=10
            await svc.PostDocAsync(await Doc(svc, DocType.In, null, w1, (p1, 5)));
            var dash = await svc.DashboardAsync();
            Assert.Equal(1, dash.LowStock);
        }
    }

    [Fact]
    public async Task Post_Twice_Blocked()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var (w1, _, p1, _) = await Seed(svc);
            var id = await Doc(svc, DocType.In, null, w1, (p1, 10));
            await svc.PostDocAsync(id);
            var (ok, _) = await svc.PostDocAsync(id);
            Assert.False(ok);
        }
    }

    [Fact]
    public async Task Cancel_ExcludesFromBalance()
    {
        var (db, svc, conn) = NewSvc(); using (conn)
        {
            var (w1, _, p1, _) = await Seed(svc);
            var id = await Doc(svc, DocType.In, null, w1, (p1, 100));
            await svc.CancelDocAsync(id);
            Assert.Empty(await svc.BalancesAsync(w1));   // cancelled không tính tồn
            var (ok, _) = await svc.PostDocAsync(id);     // hủy rồi không ghi sổ được
            Assert.False(ok);
        }
    }
}

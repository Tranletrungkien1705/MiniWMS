using Microsoft.EntityFrameworkCore;
using MiniWMS.Models;

namespace MiniWMS.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await MigratePostgresAsync(db);

        if (!await db.Orgs.AnyAsync(o => o.Id == TenantContext.DefaultOrgId))
        {
            db.Orgs.Add(new Org { Id = TenantContext.DefaultOrgId, Name = "Demo WMS", ApiKey = TenantContext.DefaultApiKey });
            await db.SaveChangesAsync();
        }
        if (!await db.Warehouses.AnyAsync())
        {
            db.Warehouses.AddRange(
                new Warehouse { Code = "KHO-HN", Name = "Kho Hà Nội", Address = "KCN Bắc Thăng Long" },
                new Warehouse { Code = "KHO-HCM", Name = "Kho TP.HCM", Address = "KCN Tân Bình" });
            await db.SaveChangesAsync();
        }
        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                new Product { Code = "AO-001", Name = "Áo sơ mi trắng", Uom = "cái", MinStock = 20 },
                new Product { Code = "QUAN-001", Name = "Quần jeans slim", Uom = "cái", MinStock = 15 },
                new Product { Code = "PK-001", Name = "Thắt lưng da", Uom = "cái", MinStock = 10 });
            await db.SaveChangesAsync();
        }
        if (!await db.Docs.AnyAsync())
        {
            var whs = await db.Warehouses.ToListAsync();
            var prods = await db.Products.ToListAsync();
            var hn = whs.First(w => w.Code == "KHO-HN").Id;
            // 1 phiếu nhập đầu kỳ đã ghi sổ vào kho HN
            var pn = new StockDoc { Type = DocType.In, ToWarehouseId = hn, Code = "PNSEED-001", Status = DocStatus.Posted, Note = "Tồn đầu kỳ", CreatedBy = "seed" };
            foreach (var p in prods) pn.Lines.Add(new StockDocLine { ProductId = p.Id, Quantity = 100 });
            db.Docs.Add(pn);
            await db.SaveChangesAsync();
        }
        if (!await db.Docs.AnyAsync(d => d.Type == DocType.Transfer))
        {
            var whs = await db.Warehouses.ToListAsync();
            var prods = await db.Products.ToListAsync();
            var hn = whs.FirstOrDefault(w => w.Code == "KHO-HN")?.Id;
            var hcm = whs.FirstOrDefault(w => w.Code == "KHO-HCM")?.Id;
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001")?.Id;
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001")?.Id;
            var pk = prods.FirstOrDefault(p => p.Code == "PK-001")?.Id;

            if (hn.HasValue && hcm.HasValue && ao.HasValue && quan.HasValue)
            {
                var pc = new StockDoc
                {
                    Type = DocType.Transfer,
                    FromWarehouseId = hn.Value,
                    ToWarehouseId = hcm.Value,
                    Code = "PCSEED-001",
                    Status = DocStatus.Posted,
                    Date = DateTime.Now.AddDays(-2),
                    Note = "Điều chuyển nội bộ tiếp tế chi nhánh HCM",
                    CreatedBy = "seed"
                };
                pc.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 20 });
                pc.Lines.Add(new StockDocLine { ProductId = quan.Value, Quantity = 10 });
                db.Docs.Add(pc);

                if (pk.HasValue)
                {
                    var px = new StockDoc
                    {
                        Type = DocType.Out,
                        FromWarehouseId = hn.Value,
                        Code = "PXSEED-001",
                        Status = DocStatus.Posted,
                        Date = DateTime.Now.AddDays(-1),
                        Note = "Xuất bán đơn hàng shop online",
                        CreatedBy = "seed"
                    };
                    px.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 15 });
                    px.Lines.Add(new StockDocLine { ProductId = pk.Value, Quantity = 10 });
                    db.Docs.Add(px);
                }
                await db.SaveChangesAsync();
            }
        }
        if (!await db.Audits.AnyAsync())
        {
            var whs = await db.Warehouses.ToListAsync();
            var prods = await db.Products.ToListAsync();
            var hn = whs.First(w => w.Code == "KHO-HN").Id;
            var kk = new StockAudit
            {
                WarehouseId = hn,
                Code = "KKSEED-001",
                Status = StockAuditStatus.Draft,
                Note = "Kiểm kê định kỳ tháng",
                CreatedBy = "seed"
            };
            foreach (var p in prods)
            {
                var act = p.Code switch { "AO-001" => 98, "QUAN-001" => 100, "PK-001" => 102, _ => 100 };
                var note = p.Code switch { "AO-001" => "Hao hụt 2 cái", "QUAN-001" => "Khớp tồn", "PK-001" => "Thừa 2 cái kiểm đếm thêm", _ => "" };
                kk.Lines.Add(new StockAuditLine { ProductId = p.Id, QtyInit = 100, QtyActual = act, Note = note });
            }
            db.Audits.Add(kk);
            await db.SaveChangesAsync();
        }
    }

    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        var tables = new[] { "Warehouses", "Products", "Docs", "DocLines", "Audits", "AuditLines" };
        var sql = new List<string>
        {
            "CREATE TABLE IF NOT EXISTS miniwms.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON miniwms.\"Orgs\" (\"ApiKey\")",
        };
        foreach (var t in tables) sql.Add($"ALTER TABLE miniwms.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        foreach (var s in sql) try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}

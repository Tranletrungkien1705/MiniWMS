using Microsoft.EntityFrameworkCore;
using MiniWMS.Models;

namespace MiniWMS.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await MigratePostgresAsync(db);
        await MigrateSqliteAsync(db);

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
        if (!await db.MoveOrders.AnyAsync())
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
                var pcSeed = await db.Docs.FirstOrDefaultAsync(d => d.Code == "PCSEED-001");

                // Lệnh 1: Đã hoàn tất, gắn với PCSEED-001
                var moDone = new MoveOrder
                {
                    Code = "MOSEED-001",
                    FromWarehouseId = hn.Value,
                    ToWarehouseId = hcm.Value,
                    Status = MoveOrderStatus.Finished,
                    Date = DateTime.Now.AddDays(-2),
                    CreatedAt = DateTime.Now.AddDays(-2),
                    ApprovedAt = DateTime.Now.AddDays(-2),
                    FinishedAt = DateTime.Now.AddDays(-2),
                    StockDocId = pcSeed?.Id,
                    Note = "Lệnh điều chuyển tiếp tế chi nhánh HCM",
                    CreatedBy = "seed"
                };
                moDone.Lines.Add(new MoveOrderLine { ProductId = ao.Value, Quantity = 20, Note = "Chuyển size M, L" });
                moDone.Lines.Add(new MoveOrderLine { ProductId = quan.Value, Quantity = 10, Note = "Chuyển theo đơn đặt hàng" });
                db.MoveOrders.Add(moDone);

                // Lệnh 2: Đang chờ duyệt (Pending)
                if (pk.HasValue)
                {
                    var moPending = new MoveOrder
                    {
                        Code = "MOSEED-002",
                        FromWarehouseId = hn.Value,
                        ToWarehouseId = hcm.Value,
                        Status = MoveOrderStatus.Pending,
                        Date = DateTime.Now,
                        CreatedAt = DateTime.Now,
                        Note = "Yêu cầu chuyển gấp phụ kiện thắt lưng cho showroom HCM",
                        CreatedBy = "seed"
                    };
                    moPending.Lines.Add(new MoveOrderLine { ProductId = pk.Value, Quantity = 5, Note = "Thắt lưng da cao cấp" });
                    db.MoveOrders.Add(moPending);
                }
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        var tables = new[] { "Warehouses", "Products", "Docs", "DocLines", "Audits", "AuditLines", "MoveOrders", "MoveOrderLines" };
        var sql = new List<string>
        {
            "CREATE TABLE IF NOT EXISTS miniwms.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON miniwms.\"Orgs\" (\"ApiKey\")",
        };
        foreach (var t in tables) sql.Add($"ALTER TABLE miniwms.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        foreach (var s in sql) try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }

    private static async Task MigrateSqliteAsync(AppDbContext db)
    {
        if (db.Database.IsNpgsql()) return;
        var sql = new[]
        {
            @"CREATE TABLE IF NOT EXISTS ""MoveOrders"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""FromWarehouseId"" INTEGER NOT NULL,
                ""ToWarehouseId"" INTEGER NOT NULL,
                ""Date"" TEXT NOT NULL,
                ""Note"" TEXT NULL,
                ""CreatedBy"" TEXT NOT NULL,
                ""Status"" INTEGER NOT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""ApprovedAt"" TEXT NULL,
                ""FinishedAt"" TEXT NULL,
                ""StockDocId"" INTEGER NULL,
                CONSTRAINT ""FK_MoveOrders_Warehouses_FromWarehouseId"" FOREIGN KEY (""FromWarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_MoveOrders_Warehouses_ToWarehouseId"" FOREIGN KEY (""ToWarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_MoveOrders_Docs_StockDocId"" FOREIGN KEY (""StockDocId"") REFERENCES ""Docs"" (""Id"") ON DELETE SET NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MoveOrders_OrgId_Code"" ON ""MoveOrders"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""MoveOrderLines"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""MoveOrderId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""Quantity"" INTEGER NOT NULL,
                ""Note"" TEXT NULL,
                CONSTRAINT ""FK_MoveOrderLines_MoveOrders_MoveOrderId"" FOREIGN KEY (""MoveOrderId"") REFERENCES ""MoveOrders"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_MoveOrderLines_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE
            );"
        };
        foreach (var s in sql)
        {
            try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
        }
    }
}

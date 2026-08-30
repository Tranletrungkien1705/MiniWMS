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
                new Warehouse { Code = "KHO-HN", Name = "Kho Phụ tùng Hà Nội", Address = "KCN Bắc Thăng Long", Keeper = "Trần Văn Kho" },
                new Warehouse { Code = "KHO-HCM", Name = "Kho Phụ tùng TP.HCM", Address = "KCN Tân Bình", Keeper = "Lê Thị Thủ" });
            await db.SaveChangesAsync();
        }
        if (!await db.Products.AnyAsync())
        {
            // Danh mục phụ tùng ô tô (đồng bộ hệ sinh thái Hyundai: MiniShowroom bán xe → MiniService sửa → MiniWMS cấp phụ tùng).
            var cat = new (string code, string name, string uom, string category, decimal cost, decimal sale, int min, int max)[]
            {
                ("PT-LOC-DAU", "Lọc dầu động cơ", "cái", "Bảo dưỡng", 85_000, 150_000, 30, 300),
                ("PT-LOC-GIO", "Lọc gió động cơ", "cái", "Bảo dưỡng", 120_000, 220_000, 20, 200),
                ("PT-LOC-DHOA", "Lọc gió điều hòa", "cái", "Bảo dưỡng", 95_000, 180_000, 20, 200),
                ("PT-BUGI", "Bugi đánh lửa", "cái", "Đánh lửa", 90_000, 160_000, 40, 400),
                ("PT-DAU-NHOT", "Dầu nhớt động cơ 5W-30", "lít", "Dầu mỡ", 130_000, 210_000, 100, 1000),
                ("PT-MA-PHANH-TR", "Má phanh trước", "bộ", "Phanh", 480_000, 780_000, 15, 120),
                ("PT-MA-PHANH-SAU", "Má phanh sau", "bộ", "Phanh", 420_000, 700_000, 15, 120),
                ("PT-DIA-PHANH", "Đĩa phanh trước", "cái", "Phanh", 950_000, 1_450_000, 8, 60),
                ("PT-ACQUY", "Ắc quy 12V-60Ah", "cái", "Điện", 1_350_000, 1_950_000, 10, 80),
                ("PT-LOP", "Lốp 215/60R17", "cái", "Lốp", 1_850_000, 2_650_000, 12, 100),
                ("PT-GAT-MUA", "Cần gạt mưa", "cặp", "Ngoại thất", 180_000, 320_000, 20, 150),
                ("PT-BONG-DEN", "Bóng đèn pha H4", "cái", "Điện", 110_000, 200_000, 30, 200),
                ("PT-NUOC-LAM-MAT", "Nước làm mát", "lít", "Dầu mỡ", 65_000, 120_000, 50, 400),
                ("PT-DAY-CUROA", "Dây curoa tổng", "cái", "Động cơ", 380_000, 620_000, 10, 80),
                ("PT-CANH-QUAT", "Cánh quạt két nước", "cái", "Làm mát", 550_000, 880_000, 6, 40),
            };
            db.Products.AddRange(cat.Select(c => new Product
            {
                Code = c.code, Name = c.name, Uom = c.uom, Category = c.category,
                Barcode = "893" + Math.Abs(c.code.GetHashCode()).ToString().PadLeft(10, '0')[..10],
                CostPrice = c.cost, SalePrice = c.sale, MinStock = c.min, MaxStock = c.max
            }));
            await db.SaveChangesAsync();
        }
        if (!await db.Docs.AnyAsync())
        {
            var whs = await db.Warehouses.ToListAsync();
            var prods = await db.Products.ToListAsync();
            var hn = whs.First(w => w.Code == "KHO-HN").Id;
            var hcm = whs.First(w => w.Code == "KHO-HCM").Id;

            // Nhập đầu kỳ kho HN (đơn giá = giá vốn) — tạo giá trị tồn thật.
            var pn = new StockDoc { Type = DocType.In, ToWarehouseId = hn, Code = "PN260801-001", Status = DocStatus.Posted,
                Note = "Nhập tồn đầu kỳ", RefNo = "HD-MUA-8801", PartnerName = "Hyundai Thành Công", CreatedBy = "seed", Date = DateTime.Now.AddDays(-20) };
            foreach (var p in prods) pn.Lines.Add(new StockDocLine { ProductId = p.Id, Quantity = 200, UnitPrice = p.CostPrice });
            db.Docs.Add(pn);

            // Nhập kho HCM ít hơn
            var pn2 = new StockDoc { Type = DocType.In, ToWarehouseId = hcm, Code = "PN260801-002", Status = DocStatus.Posted,
                Note = "Nhập kho HCM", RefNo = "HD-MUA-8802", PartnerName = "Hyundai Thành Công", CreatedBy = "seed", Date = DateTime.Now.AddDays(-18) };
            foreach (var p in prods.Take(8)) pn2.Lines.Add(new StockDocLine { ProductId = p.Id, Quantity = 80, UnitPrice = p.CostPrice });
            db.Docs.Add(pn2);
            await db.SaveChangesAsync();

            // Xuất phục vụ sửa chữa (kho HN) — giảm tồn.
            var px = new StockDoc { Type = DocType.Out, FromWarehouseId = hn, Code = "PX260810-001", Status = DocStatus.Posted,
                Note = "Xuất cho xưởng dịch vụ", RefNo = "RO-1205", PartnerName = "Xưởng DV Hà Nội", CreatedBy = "seed", Date = DateTime.Now.AddDays(-5) };
            px.Lines.Add(new StockDocLine { ProductId = prods[0].Id, Quantity = 25, UnitPrice = prods[0].SalePrice });
            px.Lines.Add(new StockDocLine { ProductId = prods[4].Id, Quantity = 60, UnitPrice = prods[4].SalePrice });
            px.Lines.Add(new StockDocLine { ProductId = prods[5].Id, Quantity = 8, UnitPrice = prods[5].SalePrice });
            db.Docs.Add(px);

            // 1 phiếu nháp (chưa ghi sổ) để thấy luồng
            var draft = new StockDoc { Type = DocType.Transfer, FromWarehouseId = hn, ToWarehouseId = hcm, Code = "PC260812-001",
                Status = DocStatus.Draft, Note = "Điều chuyển bổ sung HCM", CreatedBy = "seed", Date = DateTime.Now.AddDays(-1) };
            draft.Lines.Add(new StockDocLine { ProductId = prods[9].Id, Quantity = 10, UnitPrice = prods[9].CostPrice });
            db.Docs.Add(draft);
            await db.SaveChangesAsync();
        }
    }

    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        const string S = "miniwms";
        var sql = new List<string>
        {
            $"CREATE TABLE IF NOT EXISTS {S}.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            $"CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON {S}.\"Orgs\" (\"ApiKey\")",
        };
        foreach (var t in new[] { "Warehouses", "Products", "Docs", "DocLines" })
            sql.Add($"ALTER TABLE {S}.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        void Add(string t, string col, string type) => sql.Add($"ALTER TABLE {S}.\"{t}\" ADD COLUMN IF NOT EXISTS \"{col}\" {type}");
        Add("Warehouses", "Keeper", "text");
        Add("Products", "Category", "text"); Add("Products", "Barcode", "text");
        Add("Products", "CostPrice", "numeric(18,2) NOT NULL DEFAULT 0"); Add("Products", "SalePrice", "numeric(18,2) NOT NULL DEFAULT 0");
        Add("Products", "MaxStock", "integer NOT NULL DEFAULT 0");
        Add("Docs", "PartnerName", "text"); Add("Docs", "TraceCode", "text");   // tích hợp MiniTrace
        Add("Docs", "StampBatchCode", "text"); Add("Docs", "StampSampleQr", "text");   // tích hợp MiniStamp
        Add("DocLines", "UnitPrice", "numeric(18,2) NOT NULL DEFAULT 0"); Add("DocLines", "LotNo", "text");
        foreach (var s in sql) try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }
}

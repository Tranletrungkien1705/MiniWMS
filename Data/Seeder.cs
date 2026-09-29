using Microsoft.EntityFrameworkCore;
using MiniWMS.Models;

namespace MiniWMS.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        // 🔴 BẪY #28: EnsureCreated là NO-OP khi .db đã tồn tại ⇒ bảng/cột thêm
        // sau đó KHÔNG được tạo, build xanh mà chạy lên chết "no such table".
        await MigrateSqliteAuto.RunAsync(db);
        await MigratePostgresAsync(db);
        await MigrateSqliteAsync(db);   // bảng/cột port 1:1 mới (2020.3.Skycic.Inventory)

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
                Note = "Nhập tồn đầu kỳ", RefNo = "HD-MUA-8801", PartnerName = "Hyundai Thành Công", CreatedBy = "seed", Date = DateTime.Now.AddDays(-20),
                // Thẻ kho: InvAction = 'IN' ⇒ diễn giải "Nhập kho" (Form.cs:135520); CfMixCode01..03 = Số hợp đồng / Số Container / Biển số xe
                InvAction = "IN", CfMixCode01 = "HD-MUA-8801", CfMixCode02 = "CONT-HN-0091", CfMixCode03 = "29H-123.45" };
            foreach (var p in prods) pn.Lines.Add(new StockDocLine { ProductId = p.Id, Quantity = 200, UnitPrice = p.CostPrice });
            db.Docs.Add(pn);

            // Nhập kho HCM ít hơn
            var pn2 = new StockDoc { Type = DocType.In, ToWarehouseId = hcm, Code = "PN260801-002", Status = DocStatus.Posted,
                Note = "Nhập kho HCM", RefNo = "HD-MUA-8802", PartnerName = "Hyundai Thành Công", CreatedBy = "seed", Date = DateTime.Now.AddDays(-18),
                InvAction = "IN", CfMixCode01 = "HD-MUA-8802", CfMixCode02 = "CONT-SG-0117", CfMixCode03 = "51C-678.90" };
            foreach (var p in prods.Take(8)) pn2.Lines.Add(new StockDocLine { ProductId = p.Id, Quantity = 80, UnitPrice = p.CostPrice });
            db.Docs.Add(pn2);
            await db.SaveChangesAsync();

            // Xuất phục vụ sửa chữa (kho HN) — giảm tồn.
            var px = new StockDoc { Type = DocType.Out, FromWarehouseId = hn, Code = "PX260810-001", Status = DocStatus.Posted,
                Note = "Xuất cho xưởng dịch vụ", RefNo = "RO-1205", PartnerName = "Xưởng DV Hà Nội", CreatedBy = "seed", Date = DateTime.Now.AddDays(-5),
                // InvAction = 'OUT' ⇒ diễn giải "Xuất kho" (Form.cs:135521); khi 'OUT' thì CfMixCode hiện ở 3 cột InvFCFOutCode01..03 (Form.cs:135749-135766)
                InvAction = "OUT", CfMixCode01 = "XK-2026-0007", CfMixCode02 = "CONT-HN-0455", CfMixCode03 = "29K-555.11" };
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
        // ── PORT 1:1 (2020.3.Skycic.Inventory) — thêm cột cho báo cáo Thẻ kho ──
        Add("Docs", "InvAction", "text");        // nguồn: InvF_WarehouseCard.InventoryAction
        Add("Docs", "CfMixCode01", "text");      // nguồn: InvF_WarehouseCard.InvFCFMixCode01 — Form.cs:135692
        Add("Docs", "CfMixCode02", "text");      // nguồn: InvFCFMixCode02 — Form.cs:135698
        Add("Docs", "CfMixCode03", "text");      // nguồn: InvFCFMixCode03 — Form.cs:135704
        foreach (var s in sql) try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }

    /// <summary>
    /// SQLite: <c>EnsureCreated</c> KHÔNG tạo bảng mới trên file DB đã tồn tại, nên các bảng
    /// port 1:1 mới (InvAudits/InvAuditLines/MoveOrds/MoveOrdLines) và 4 cột thêm vào Docs
    /// phải tự tạo bằng DDL. Kiểu cột viết đúng theo cách EF Core 8 map cho SQLite:
    /// decimal/DateTime/Guid → TEXT, int/bool/enum → INTEGER.
    /// </summary>
    private static async Task MigrateSqliteAsync(AppDbContext db)
    {
        if (!db.Database.IsSqlite()) return;
        var sql = new List<string>
        {
            // Phiếu kiểm kê — nguồn: bảng InvF_InvAudit (Models/InvF_InvAudit.cs:9-50)
            """
            CREATE TABLE IF NOT EXISTS "InvAudits" (
              "Id" INTEGER NOT NULL CONSTRAINT "PK_InvAudits" PRIMARY KEY AUTOINCREMENT,
              "OrgId" TEXT NOT NULL, "Code" TEXT NOT NULL, "WarehouseId" INTEGER NOT NULL,
              "Status" INTEGER NOT NULL, "Remark" TEXT NULL,
              "CreatedAt" TEXT NOT NULL, "CreatedBy" TEXT NOT NULL,
              "UpdatedAt" TEXT NULL, "UpdatedBy" TEXT NULL,
              "ApprovedAt" TEXT NULL, "ApprovedBy" TEXT NULL,
              "FinishedAt" TEXT NULL, "FinishedBy" TEXT NULL,
              "CancelledAt" TEXT NULL, "CancelledBy" TEXT NULL,
              "LogLuAt" TEXT NOT NULL, "LogLuBy" TEXT NULL, "AdjustDocCodes" TEXT NULL,
              CONSTRAINT "FK_InvAudits_Warehouses_WarehouseId" FOREIGN KEY ("WarehouseId") REFERENCES "Warehouses" ("Id") ON DELETE RESTRICT)
            """,
            """CREATE UNIQUE INDEX IF NOT EXISTS "IX_InvAudits_OrgId_Code" ON "InvAudits" ("OrgId", "Code")""",
            """CREATE INDEX IF NOT EXISTS "IX_InvAudits_WarehouseId" ON "InvAudits" ("WarehouseId")""",
            // Dòng phiếu kiểm kê — nguồn: InvF_InvAuditDtl (Models/InvF_InvAuditDtl.cs:10-61)
            """
            CREATE TABLE IF NOT EXISTS "InvAuditLines" (
              "Id" INTEGER NOT NULL CONSTRAINT "PK_InvAuditLines" PRIMARY KEY AUTOINCREMENT,
              "OrgId" TEXT NOT NULL, "AuditId" INTEGER NOT NULL, "ProductId" INTEGER NOT NULL,
              "LocationInit" TEXT NULL, "LocationActual" TEXT NULL,
              "QtyInit" TEXT NOT NULL, "QtyActual" TEXT NOT NULL,
              "UpAudit" TEXT NOT NULL, "ValAudit" TEXT NOT NULL, "Vat" TEXT NOT NULL,
              "UnitCode" TEXT NULL, "InventoryAction" INTEGER NOT NULL,
              "FlagExist" INTEGER NOT NULL, "FlagAudit" INTEGER NOT NULL, "StatusDtl" INTEGER NOT NULL,
              "Remark" TEXT NULL, "LogLuAt" TEXT NOT NULL, "LogLuBy" TEXT NULL,
              CONSTRAINT "FK_InvAuditLines_InvAudits_AuditId" FOREIGN KEY ("AuditId") REFERENCES "InvAudits" ("Id") ON DELETE CASCADE,
              CONSTRAINT "FK_InvAuditLines_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE)
            """,
            """CREATE INDEX IF NOT EXISTS "IX_InvAuditLines_AuditId" ON "InvAuditLines" ("AuditId")""",
            """CREATE INDEX IF NOT EXISTS "IX_InvAuditLines_ProductId" ON "InvAuditLines" ("ProductId")""",
            // Phiếu điều chuyển — nguồn: InvF_MoveOrd (Models/InvF_MoveOrd.cs:9-51)
            """
            CREATE TABLE IF NOT EXISTS "MoveOrds" (
              "Id" INTEGER NOT NULL CONSTRAINT "PK_MoveOrds" PRIMARY KEY AUTOINCREMENT,
              "OrgId" TEXT NOT NULL, "Code" TEXT NOT NULL, "MoveOrdType" TEXT NULL,
              "OutWarehouseId" INTEGER NOT NULL, "InWarehouseId" INTEGER NOT NULL, "AuditCode" TEXT NULL,
              "CreatedAt" TEXT NOT NULL, "CreatedBy" TEXT NOT NULL,
              "UpdatedAt" TEXT NULL, "UpdatedBy" TEXT NULL,
              "ApprovedAt" TEXT NULL, "ApprovedBy" TEXT NULL,
              "CancelledAt" TEXT NULL, "CancelledBy" TEXT NULL,
              "Status" INTEGER NOT NULL, "Remark" TEXT NULL,
              "LogLuAt" TEXT NOT NULL, "LogLuBy" TEXT NULL, "TransferDocCode" TEXT NULL,
              CONSTRAINT "FK_MoveOrds_Warehouses_OutWarehouseId" FOREIGN KEY ("OutWarehouseId") REFERENCES "Warehouses" ("Id") ON DELETE RESTRICT,
              CONSTRAINT "FK_MoveOrds_Warehouses_InWarehouseId" FOREIGN KEY ("InWarehouseId") REFERENCES "Warehouses" ("Id") ON DELETE RESTRICT)
            """,
            """CREATE UNIQUE INDEX IF NOT EXISTS "IX_MoveOrds_OrgId_Code" ON "MoveOrds" ("OrgId", "Code")""",
            """CREATE INDEX IF NOT EXISTS "IX_MoveOrds_OutWarehouseId" ON "MoveOrds" ("OutWarehouseId")""",
            """CREATE INDEX IF NOT EXISTS "IX_MoveOrds_InWarehouseId" ON "MoveOrds" ("InWarehouseId")""",
            // Dòng phiếu điều chuyển — nguồn: InvF_MoveOrdDtl (Models/InvF_MoveOrdDtl.cs:61-113)
            """
            CREATE TABLE IF NOT EXISTS "MoveOrdLines" (
              "Id" INTEGER NOT NULL CONSTRAINT "PK_MoveOrdLines" PRIMARY KEY AUTOINCREMENT,
              "OrgId" TEXT NOT NULL, "MoveOrdId" INTEGER NOT NULL, "ProductId" INTEGER NOT NULL,
              "OutLocation" TEXT NULL, "InLocation" TEXT NULL,
              "Qty" TEXT NOT NULL, "UpMove" TEXT NOT NULL, "ValMove" TEXT NOT NULL,
              "Vat" TEXT NOT NULL, "ValVat" TEXT NOT NULL, "ValAfterVat" TEXT NOT NULL,
              "UnitCode" TEXT NULL, "StatusDtl" INTEGER NOT NULL, "Remark" TEXT NULL,
              "LogLuAt" TEXT NOT NULL, "LogLuBy" TEXT NULL,
              CONSTRAINT "FK_MoveOrdLines_MoveOrds_MoveOrdId" FOREIGN KEY ("MoveOrdId") REFERENCES "MoveOrds" ("Id") ON DELETE CASCADE,
              CONSTRAINT "FK_MoveOrdLines_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE CASCADE)
            """,
            """CREATE INDEX IF NOT EXISTS "IX_MoveOrdLines_MoveOrdId" ON "MoveOrdLines" ("MoveOrdId")""",
            """CREATE INDEX IF NOT EXISTS "IX_MoveOrdLines_ProductId" ON "MoveOrdLines" ("ProductId")""",
        };
        foreach (var s in sql) try { await db.Database.ExecuteSqlRawAsync(s); } catch { }

        // SQLite không có "ADD COLUMN IF NOT EXISTS" ⇒ hỏi PRAGMA trước rồi mới ALTER,
        // để log không đầy dòng ERR "duplicate column name" ở mỗi lần khởi động.
        var daCo = new List<string>();
        try { daCo = await db.Database.SqlQueryRaw<string>("SELECT name AS \"Value\" FROM pragma_table_info('Docs')").ToListAsync(); }
        catch { /* không đọc được PRAGMA thì cứ thử ALTER như cũ */ }
        // 3 cột đầu (TraceCode/StampBatchCode/StampSampleQr) là cột CŨ của tích hợp MiniTrace/MiniStamp:
        // trước đây chỉ có ALTER cho Postgres nên file miniwms.db trong repo BỊ LỆCH SCHEMA và
        // /Doc, /Inventory trả 500 ("no such column: d.StampBatchCode") — đo thật 29/09.
        // 4 cột sau là cột port 1:1 cho Thẻ kho (2020.3.Skycic.Inventory).
        foreach (var col in new[] { "TraceCode", "StampBatchCode", "StampSampleQr",
                                    "InvAction", "CfMixCode01", "CfMixCode02", "CfMixCode03" })
        {
            if (daCo.Contains(col)) continue;
            var ddl = "ALTER TABLE \"Docs\" ADD COLUMN \"" + col + "\" TEXT NULL";   // tên cột là hằng trong mã, không từ input
            try { await db.Database.ExecuteSqlRawAsync(ddl); } catch { }
        }
    }
}

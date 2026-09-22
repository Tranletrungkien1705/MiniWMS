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
                new Product { Code = "AO-001", Name = "Áo sơ mi trắng", Uom = "cái", MinStock = 20, MaxStock = 200, CostPrice = 150000m },
                new Product { Code = "QUAN-001", Name = "Quần jeans slim", Uom = "cái", MinStock = 15, MaxStock = 150, CostPrice = 280000m },
                new Product { Code = "PK-001", Name = "Thắt lưng da", Uom = "cái", MinStock = 10, MaxStock = 80, CostPrice = 120000m },
                new Product { Code = "VAY-001", Name = "Váy đầm công sở", Uom = "cái", MinStock = 12, MaxStock = 100, CostPrice = 320000m });
            await db.SaveChangesAsync();
        }
        else
        {
            // Cập nhật giá vốn cho dữ liệu cũ nếu chưa có
            var existingProds = await db.Products.Where(p => p.CostPrice == 0).ToListAsync();
            if (existingProds.Any())
            {
                foreach (var p in existingProds)
                {
                    p.CostPrice = p.Code switch
                    {
                        "AO-001" => 150000m,
                        "QUAN-001" => 280000m,
                        "PK-001" => 120000m,
                        "VAY-001" => 320000m,
                        _ => 100000m
                    };
                }
                await db.SaveChangesAsync();
            }
        }
        if (!await db.Docs.AnyAsync())
        {
            var whs = await db.Warehouses.ToListAsync();
            var prods = await db.Products.ToListAsync();
            var hn = whs.First(w => w.Code == "KHO-HN").Id;
            // 1 phiếu nhập đầu kỳ đã ghi sổ vào kho HN (cách đây 110 ngày để tạo nhóm tuổi tồn kho > 90 ngày)
            var pn = new StockDoc { Type = DocType.In, ToWarehouseId = hn, Code = "PNSEED-001", Status = DocStatus.Posted, Date = DateTime.Now.AddDays(-110), Note = "Tồn đầu kỳ", CreatedBy = "seed" };
            foreach (var p in prods) pn.Lines.Add(new StockDocLine { ProductId = p.Id, Quantity = 100 });
            db.Docs.Add(pn);
            await db.SaveChangesAsync();
        }
        else
        {
            var pn1 = await db.Docs.FirstOrDefaultAsync(d => d.Code == "PNSEED-001");
            if (pn1 != null && pn1.Date.Date >= DateTime.Today.AddDays(-5))
            {
                pn1.Date = DateTime.Now.AddDays(-110);
                await db.SaveChangesAsync();
            }
        }

        if (!await db.Docs.AnyAsync(d => d.Code == "PNSEED-004"))
        {
            var whs = await db.Warehouses.ToListAsync();
            var hn = whs.FirstOrDefault(w => w.Code == "KHO-HN")?.Id;
            var prods = await db.Products.ToListAsync();
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001")?.Id;
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001")?.Id;
            var vay = prods.FirstOrDefault(p => p.Code == "VAY-001")?.Id;

            if (hn.HasValue && ao.HasValue && quan.HasValue && vay.HasValue)
            {
                // Phiếu nhập cách đây 15 ngày cho Áo sơ mi trắng (< 30 ngày)
                var pn4 = new StockDoc
                {
                    Type = DocType.In,
                    ToWarehouseId = hn.Value,
                    Code = "PNSEED-004",
                    Status = DocStatus.Posted,
                    Date = DateTime.Now.AddDays(-15),
                    Note = "Nhập bổ sung hàng mới Áo sơ mi trắng",
                    CreatedBy = "seed"
                };
                pn4.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 20 });
                db.Docs.Add(pn4);

                // Phiếu nhập cách đây 45 ngày cho Quần jeans (31 - 60 ngày)
                var pn5 = new StockDoc
                {
                    Type = DocType.In,
                    ToWarehouseId = hn.Value,
                    Code = "PNSEED-005",
                    Status = DocStatus.Posted,
                    Date = DateTime.Now.AddDays(-45),
                    Note = "Nhập bổ sung Quần jeans slim",
                    CreatedBy = "seed"
                };
                pn5.Lines.Add(new StockDocLine { ProductId = quan.Value, Quantity = 15 });
                db.Docs.Add(pn5);

                // Phiếu nhập cách đây 75 ngày cho Váy đầm công sở (61 - 90 ngày)
                var pn6 = new StockDoc
                {
                    Type = DocType.In,
                    ToWarehouseId = hn.Value,
                    Code = "PNSEED-006",
                    Status = DocStatus.Posted,
                    Date = DateTime.Now.AddDays(-75),
                    Note = "Nhập bộ sưu tập Váy đầm giữa mùa",
                    CreatedBy = "seed"
                };
                pn6.Lines.Add(new StockDocLine { ProductId = vay.Value, Quantity = 10 });
                db.Docs.Add(pn6);

                await db.SaveChangesAsync();
            }
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
        if (!await db.ReturnToSuppliers.AnyAsync())
        {
            var whs = await db.Warehouses.ToListAsync();
            var prods = await db.Products.ToListAsync();
            var hn = whs.FirstOrDefault(w => w.Code == "KHO-HN")?.Id;
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001")?.Id;
            var pk = prods.FirstOrDefault(p => p.Code == "PK-001")?.Id;

            if (hn.HasValue && ao.HasValue)
            {
                // Phiếu xuất kho cho hàng xuất trả SEED01
                var pxReturn = new StockDoc
                {
                    Type = DocType.Out,
                    FromWarehouseId = hn.Value,
                    Code = "PXSEED-002",
                    Status = DocStatus.Posted,
                    Date = DateTime.Now.AddDays(-1),
                    RefNo = "THNCC-SEED01",
                    Note = "Xuất trả hàng NCC Tổng Công ty May 10 theo phiếu THNCC-SEED01: Lỗi đường may bung chỉ",
                    CreatedBy = "seed"
                };
                pxReturn.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 5 });
                db.Docs.Add(pxReturn);
                await db.SaveChangesAsync();

                // Phiếu trả hàng NCC 1: Đã duyệt & xuất kho
                var retDone = new ReturnToSupplier
                {
                    Code = "THNCC-SEED01",
                    WarehouseId = hn.Value,
                    SupplierName = "Tổng Công ty May 10",
                    SupplierCode = "NCC-MAY10",
                    RefDocNo = "PNSEED-001",
                    Reason = "Lỗi đường may bung chỉ ở cổ áo",
                    Status = ReturnSupStatus.Finished,
                    Date = DateTime.Now.AddDays(-1),
                    CreatedAt = DateTime.Now.AddDays(-1),
                    FinishedAt = DateTime.Now.AddDays(-1),
                    StockDocId = pxReturn.Id,
                    CreatedBy = "seed"
                };
                retDone.Lines.Add(new ReturnToSupplierLine { ProductId = ao.Value, Quantity = 5, UnitPrice = 180000m, Note = "5 áo size L bung đường chỉ vai" });
                db.ReturnToSuppliers.Add(retDone);

                // Phiếu trả hàng NCC 2: Chờ duyệt (Draft)
                if (pk.HasValue)
                {
                    var retDraft = new ReturnToSupplier
                    {
                        Code = "THNCC-SEED02",
                        WarehouseId = hn.Value,
                        SupplierName = "Xưởng Da Thật Hà Nội",
                        SupplierCode = "NCC-DAHN",
                        RefDocNo = "HD-9921",
                        Reason = "Trầy xước mặt khóa kim loại khi kiểm nhận",
                        Status = ReturnSupStatus.Draft,
                        Date = DateTime.Now,
                        CreatedAt = DateTime.Now,
                        CreatedBy = "seed"
                    };
                    retDraft.Lines.Add(new ReturnToSupplierLine { ProductId = pk.Value, Quantity = 3, UnitPrice = 250000m, Note = "Mặt khóa trầy xước không đạt chuẩn xuất bán" });
                    db.ReturnToSuppliers.Add(retDraft);
                }

                await db.SaveChangesAsync();
            }
        }
        if (!await db.CustomerReturns.AnyAsync())
        {
            var whs = await db.Warehouses.ToListAsync();
            var prods = await db.Products.ToListAsync();
            var hn = whs.FirstOrDefault(w => w.Code == "KHO-HN")?.Id;
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001")?.Id;
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001")?.Id;

            if (hn.HasValue && ao.HasValue)
            {
                // Phiếu nhập kho cho hàng khách trả đã duyệt SEED01
                var pnReturn = new StockDoc
                {
                    Type = DocType.In,
                    ToWarehouseId = hn.Value,
                    Code = "PNSEED-003",
                    Status = DocStatus.Posted,
                    Date = DateTime.Now.AddDays(-1),
                    RefNo = "THKH-SEED01",
                    Note = "Nhập hàng khách trả lại: Đại lý Thời trang An Phát theo phiếu THKH-SEED01: Đổi size L sang XL",
                    CreatedBy = "seed"
                };
                pnReturn.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 3 });
                db.Docs.Add(pnReturn);
                await db.SaveChangesAsync();

                // Phiếu khách trả 1: Đã duyệt & nhập kho (Finished)
                var cusDone = new CustomerReturn
                {
                    Code = "THKH-SEED01",
                    WarehouseId = hn.Value,
                    CustomerName = "Đại lý Thời trang An Phát",
                    CustomerCode = "KH-ANPHAT",
                    InvoiceNo = "HD-2026-0312",
                    RefOrderNo = "PXSEED-001",
                    Reason = "Khách đổi hàng size L sang XL do mặc chật",
                    Status = CusReturnStatus.Finished,
                    Date = DateTime.Now.AddDays(-1),
                    CreatedAt = DateTime.Now.AddDays(-1),
                    FinishedAt = DateTime.Now.AddDays(-1),
                    StockDocId = pnReturn.Id,
                    CreatedBy = "seed"
                };
                cusDone.Lines.Add(new CustomerReturnLine { ProductId = ao.Value, Quantity = 3, UnitPrice = 220000m, Note = "3 áo sơ mi trắng nguyên tem mác" });
                db.CustomerReturns.Add(cusDone);

                // Phiếu khách trả 2: Chờ nhận hàng (Draft)
                if (quan.HasValue)
                {
                    var cusDraft = new CustomerReturn
                    {
                        Code = "THKH-SEED02",
                        WarehouseId = hn.Value,
                        CustomerName = "Cửa hàng Thời trang Hải Đăng",
                        CustomerCode = "KH-HAIDANG",
                        InvoiceNo = "HD-2026-0318",
                        RefOrderNo = "ORD-8821",
                        Reason = "Khách phản hồi màu đậm hơn ảnh mẫu, yêu cầu hoàn trả",
                        Status = CusReturnStatus.Draft,
                        Date = DateTime.Now,
                        CreatedAt = DateTime.Now,
                        CreatedBy = "seed"
                    };
                    cusDraft.Lines.Add(new CustomerReturnLine { ProductId = quan.Value, Quantity = 2, UnitPrice = 350000m, Note = "2 quần jeans slim nguyên bao bì" });
                    db.CustomerReturns.Add(cusDraft);
                }

                await db.SaveChangesAsync();
            }
        }
        if (!await db.StockLots.AnyAsync())
        {
            var whs = await db.Warehouses.ToListAsync();
            var prods = await db.Products.ToListAsync();
            var hn = whs.FirstOrDefault(w => w.Code == "KHO-HN")?.Id;
            var hcm = whs.FirstOrDefault(w => w.Code == "KHO-HCM")?.Id;
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001")?.Id;
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001")?.Id;
            var pk = prods.FirstOrDefault(p => p.Code == "PK-001")?.Id;
            var vay = prods.FirstOrDefault(p => p.Code == "VAY-001")?.Id;

            var today = DateTime.Today;

            if (hn.HasValue && ao.HasValue && quan.HasValue && pk.HasValue && vay.HasValue)
            {
                var lots = new List<StockLot>
                {
                    // 1. Áo sơ mi trắng - Kho HN: Lô an toàn (> 90 ngày)
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = ao.Value,
                        LotNo = "LOT-AO26-01",
                        ProductionDate = today.AddDays(-60),
                        ExpiredDate = today.AddDays(365),
                        InDate = today.AddDays(-55),
                        Quantity = 50,
                        Note = "Lô hàng sản xuất đầu năm 2026, chất vải cotton cao cấp"
                    },
                    // 2. Áo sơ mi trắng - Kho HN: Lô cận hạn nguy cấp (<= 30 ngày)
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = ao.Value,
                        LotNo = "LOT-AO25-04",
                        ProductionDate = today.AddDays(-340),
                        ExpiredDate = today.AddDays(15),
                        InDate = today.AddDays(-330),
                        Quantity = 20,
                        Note = "Lô hàng cuối năm 2025 còn lại, cần ưu tiên xuất bán sớm (FEFO)"
                    },
                    // 3. Quần jeans slim - Kho HN: Lô an toàn
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = quan.Value,
                        LotNo = "LOT-QJ26-01",
                        ProductionDate = today.AddDays(-45),
                        ExpiredDate = today.AddDays(700),
                        InDate = today.AddDays(-40),
                        Quantity = 70,
                        Note = "Lô quần jeans co giãn 4 chiều"
                    },
                    // 4. Thắt lưng da - Kho HN: Lô ĐÃ QUÁ HẠN (Expired < 0 ngày) để kiểm tra cảnh báo
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = pk.Value,
                        LotNo = "LOT-TL25-01",
                        ProductionDate = today.AddDays(-400),
                        ExpiredDate = today.AddDays(-15),
                        InDate = today.AddDays(-380),
                        Quantity = 12,
                        Note = "Lô phụ kiện lưu kho quá hạn kiểm định, cần lập biên bản thanh lý/trả lại"
                    },
                    // 5. Thắt lưng da - Kho HN: Lô cận hạn cảnh báo (31 - 90 ngày)
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = pk.Value,
                        LotNo = "LOT-TL25-03",
                        ProductionDate = today.AddDays(-300),
                        ExpiredDate = today.AddDays(45),
                        InDate = today.AddDays(-280),
                        Quantity = 28,
                        Note = "Mặt khóa hợp kim, kiểm định lớp mạ còn 45 ngày"
                    },
                    // 6. Thắt lưng da - Kho HN: Lô an toàn
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = pk.Value,
                        LotNo = "LOT-TL26-01",
                        ProductionDate = today.AddDays(-30),
                        ExpiredDate = today.AddDays(600),
                        InDate = today.AddDays(-25),
                        Quantity = 40,
                        Note = "Hàng mới nhập đầu quý"
                    },
                    // 7. Váy đầm công sở - Kho HN: Lô cận hạn nguy cấp (<= 30 ngày)
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = vay.Value,
                        LotNo = "LOT-VD25-02",
                        ProductionDate = today.AddDays(-335),
                        ExpiredDate = today.AddDays(25),
                        InDate = today.AddDays(-320),
                        Quantity = 25,
                        Note = "Mẫu thiết kế mùa trước, cận hạn lưu kho theo cam kết đại lý"
                    },
                    // 8. Váy đầm công sở - Kho HN: Lô an toàn
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = vay.Value,
                        LotNo = "LOT-VD26-01",
                        ProductionDate = today.AddDays(-20),
                        ExpiredDate = today.AddDays(500),
                        InDate = today.AddDays(-15),
                        Quantity = 75,
                        Note = "Bộ sưu tập xuân hè mới nhất"
                    }
                };

                // Lô tại Kho TP.HCM
                if (hcm.HasValue)
                {
                    lots.Add(new StockLot
                    {
                        WarehouseId = hcm.Value,
                        ProductId = ao.Value,
                        LotNo = "LOT-AO26-HCM1",
                        ProductionDate = today.AddDays(-30),
                        ExpiredDate = today.AddDays(400),
                        InDate = today.AddDays(-2),
                        Quantity = 20,
                        Note = "Hàng nhận điều chuyển từ Kho Hà Nội"
                    });
                    lots.Add(new StockLot
                    {
                        WarehouseId = hcm.Value,
                        ProductId = quan.Value,
                        LotNo = "LOT-QJ26-HCM1",
                        ProductionDate = today.AddDays(-35),
                        ExpiredDate = today.AddDays(650),
                        InDate = today.AddDays(-2),
                        Quantity = 10,
                        Note = "Hàng nhận điều chuyển từ Kho Hà Nội"
                    });
                }

                db.StockLots.AddRange(lots);
                await db.SaveChangesAsync();
            }
        }
        if (!await db.StockSerials.AnyAsync())
        {
            var whs = await db.Warehouses.ToListAsync();
            var prods = await db.Products.ToListAsync();
            var hn = whs.FirstOrDefault(w => w.Code == "KHO-HN")?.Id;
            var hcm = whs.FirstOrDefault(w => w.Code == "KHO-HCM")?.Id;
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001")?.Id;
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001")?.Id;
            var pk = prods.FirstOrDefault(p => p.Code == "PK-001")?.Id;
            var vay = prods.FirstOrDefault(p => p.Code == "VAY-001")?.Id;

            var today = DateTime.Today;

            if (hn.HasValue && ao.HasValue && quan.HasValue && pk.HasValue && vay.HasValue)
            {
                var serials = new List<StockSerial>
                {
                    // 1. Áo sơ mi trắng - Kho HN: Khả dụng (Available)
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = ao.Value,
                        SerialNo = "SN-AO26-0001",
                        LotNo = "LOT-AO26-01",
                        Status = StockSerialStatus.Available,
                        InDate = today.AddDays(-55),
                        RefNo = "PNSEED-001",
                        Note = "Tem bảo hành nguyên vẹn, size L"
                    },
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = ao.Value,
                        SerialNo = "SN-AO26-0002",
                        LotNo = "LOT-AO26-01",
                        Status = StockSerialStatus.Available,
                        InDate = today.AddDays(-55),
                        RefNo = "PNSEED-001",
                        Note = "Tem bảo hành nguyên vẹn, size M"
                    },
                    // 2. Áo sơ mi trắng - Kho HN: Tạm khóa / Giữ hàng theo đơn (Locked)
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = ao.Value,
                        SerialNo = "SN-AO26-0003",
                        LotNo = "LOT-AO26-01",
                        Status = StockSerialStatus.Locked,
                        InDate = today.AddDays(-55),
                        RefNo = "ORD-VIP-991",
                        Note = "Tạm giữ cho khách hàng VIP đại lý An Phát"
                    },
                    // 3. Quần jeans slim - Kho HN: Khả dụng (Available)
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = quan.Value,
                        SerialNo = "SN-QJ26-0101",
                        LotNo = "LOT-QJ26-01",
                        Status = StockSerialStatus.Available,
                        InDate = today.AddDays(-40),
                        RefNo = "PNSEED-001",
                        Note = "Size 31, kiểm tra chất lượng đạt loại A"
                    },
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = quan.Value,
                        SerialNo = "SN-QJ26-0102",
                        LotNo = "LOT-QJ26-01",
                        Status = StockSerialStatus.Available,
                        InDate = today.AddDays(-40),
                        RefNo = "PNSEED-001",
                        Note = "Size 32, tem chống hàng giả đầy đủ"
                    },
                    // 4. Quần jeans slim - Kho HN: Đã xuất kho (Exported)
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = quan.Value,
                        SerialNo = "SN-QJ26-0099",
                        LotNo = "LOT-QJ26-01",
                        Status = StockSerialStatus.Exported,
                        InDate = today.AddDays(-60),
                        OutDate = today.AddDays(-2),
                        RefNo = "PXSEED-001",
                        Note = "Xuất bán đơn hàng shop online"
                    },
                    // 5. Thắt lưng da - Kho HN: Hỏng / Lỗi kiểm định (DamagedNG)
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = pk.Value,
                        SerialNo = "SN-TL25-0012",
                        LotNo = "LOT-TL25-01",
                        Status = StockSerialStatus.DamagedNG,
                        InDate = today.AddDays(-380),
                        RefNo = "PNSEED-001",
                        Note = "Lỗi mặt khóa hợp kim bị oxy hóa, chờ gửi trả bảo hành NCC May 10"
                    },
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = pk.Value,
                        SerialNo = "SN-TL26-0045",
                        LotNo = "LOT-TL26-01",
                        Status = StockSerialStatus.Available,
                        InDate = today.AddDays(-25),
                        RefNo = "PNSEED-001",
                        Note = "Da bò thật nguyên miếng, phụ kiện xuất sắc"
                    },
                    // 6. Váy đầm công sở - Kho HN: Khả dụng
                    new()
                    {
                        WarehouseId = hn.Value,
                        ProductId = vay.Value,
                        SerialNo = "SN-VD26-0008",
                        LotNo = "LOT-VD26-01",
                        Status = StockSerialStatus.Available,
                        InDate = today.AddDays(-15),
                        RefNo = "PNSEED-006",
                        Note = "Hàng thiết kế cao cấp, mã RFID gắn thẻ"
                    }
                };

                // Lô serial tại kho TP.HCM
                if (hcm.HasValue)
                {
                    serials.Add(new StockSerial
                    {
                        WarehouseId = hcm.Value,
                        ProductId = ao.Value,
                        SerialNo = "SN-AO26-HCM01",
                        LotNo = "LOT-AO26-HCM1",
                        Status = StockSerialStatus.Available,
                        InDate = today.AddDays(-2),
                        RefNo = "PCSEED-001",
                        Note = "Nhận điều chuyển từ Kho Hà Nội"
                    });
                    serials.Add(new StockSerial
                    {
                        WarehouseId = hcm.Value,
                        ProductId = quan.Value,
                        SerialNo = "SN-QJ26-HCM01",
                        LotNo = "LOT-QJ26-HCM1",
                        Status = StockSerialStatus.Available,
                        InDate = today.AddDays(-2),
                        RefNo = "PCSEED-001",
                        Note = "Nhận điều chuyển từ Kho Hà Nội"
                    });
                }

                db.StockSerials.AddRange(serials);
                await db.SaveChangesAsync();
            }
        }
        if (!await db.InventoryBlocks.AnyAsync())
        {
            var whs = await db.Warehouses.ToListAsync();
            var hn = whs.FirstOrDefault(w => w.Code == "KHO-HN")?.Id;
            var hcm = whs.FirstOrDefault(w => w.Code == "KHO-HCM")?.Id;

            if (hn.HasValue)
            {
                var blocks = new List<InventoryBlock>
                {
                    // Kệ SHELF-A (Kho Hà Nội)
                    new()
                    {
                        WarehouseId = hn.Value,
                        InvBlockCode = "A-01-01",
                        ShelfCode = "SHELF-A",
                        InvBlockDesc = "Dãy A - Tầng 1 - Khoang 01",
                        Length = 120, Width = 80, Height = 60,
                        MaxCapacity = 150,
                        FlagActive = true,
                        Remark = "Khu vực lưu trữ Áo sơ mi & Quần jeans gấp gọn"
                    },
                    new()
                    {
                        WarehouseId = hn.Value,
                        InvBlockCode = "A-01-02",
                        ShelfCode = "SHELF-A",
                        InvBlockDesc = "Dãy A - Tầng 1 - Khoang 02",
                        Length = 120, Width = 80, Height = 60,
                        MaxCapacity = 150,
                        FlagActive = true,
                        Remark = "Khu vực lưu trữ Áo sơ mi xuất khẩu"
                    },
                    new()
                    {
                        WarehouseId = hn.Value,
                        InvBlockCode = "A-02-01",
                        ShelfCode = "SHELF-A",
                        InvBlockDesc = "Dãy A - Tầng 2 - Khoang 01",
                        Length = 120, Width = 80, Height = 50,
                        MaxCapacity = 100,
                        FlagActive = true,
                        Remark = "Ngăn hàng thời trang cao cấp"
                    },
                    // Kệ SHELF-B (Kho Hà Nội)
                    new()
                    {
                        WarehouseId = hn.Value,
                        InvBlockCode = "B-01-01",
                        ShelfCode = "SHELF-B",
                        InvBlockDesc = "Dãy B - Tầng 1 - Khoang 01",
                        Length = 100, Width = 60, Height = 40,
                        MaxCapacity = 200,
                        FlagActive = true,
                        Remark = "Kệ chứa phụ kiện thắt lưng da và ví"
                    },
                    new()
                    {
                        WarehouseId = hn.Value,
                        InvBlockCode = "B-01-02",
                        ShelfCode = "SHELF-B",
                        InvBlockDesc = "Dãy B - Tầng 1 - Khoang 02 (Bảo trì)",
                        Length = 100, Width = 60, Height = 40,
                        MaxCapacity = 80,
                        FlagActive = false,
                        Remark = "Đang bảo trì ray đỡ kệ chịu lực"
                    },
                    // Kệ SHELF-C (Kho Hà Nội)
                    new()
                    {
                        WarehouseId = hn.Value,
                        InvBlockCode = "C-01-01",
                        ShelfCode = "SHELF-C",
                        InvBlockDesc = "Dãy C - Tầng 1 - Khoang 01",
                        Length = 150, Width = 100, Height = 80,
                        MaxCapacity = 120,
                        FlagActive = true,
                        Remark = "Khu bảo quản váy đầm dạ hội có móc treo"
                    }
                };

                if (hcm.HasValue)
                {
                    blocks.Add(new InventoryBlock
                    {
                        WarehouseId = hcm.Value,
                        InvBlockCode = "HCM-01-01",
                        ShelfCode = "SHELF-S1",
                        InvBlockDesc = "Dãy Nam - Tầng 1 - Khoang 01",
                        Length = 120, Width = 80, Height = 60,
                        MaxCapacity = 120,
                        FlagActive = true,
                        Remark = "Khu vực nhận hàng luân chuyển từ chi nhánh phía Bắc"
                    });
                    blocks.Add(new InventoryBlock
                    {
                        WarehouseId = hcm.Value,
                        InvBlockCode = "HCM-01-02",
                        ShelfCode = "SHELF-S1",
                        InvBlockDesc = "Dãy Nam - Tầng 1 - Khoang 02",
                        Length = 120, Width = 80, Height = 60,
                        MaxCapacity = 120,
                        FlagActive = true,
                        Remark = "Khu vực soạn hàng xuất bán online giao nhanh"
                    });
                }

                db.InventoryBlocks.AddRange(blocks);
                await db.SaveChangesAsync();
            }
        }

        // Seed dữ liệu Quản lý & Lịch sử Giá vốn kho (CostPriceHist - port từ Inv_CostPriceHist Skycic)
        if (!await db.CostPriceHists.AnyAsync())
        {
            var prods = await db.Products.ToListAsync();
            var whMain = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HN" || w.Name.Contains("Hà Nội") || w.Name.Contains("Chính"));
            var whSub = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HCM" || (whMain != null && w.Id != whMain.Id));
            var costPrices = new List<CostPriceHist>();

            foreach (var p in prods)
            {
                decimal baseCost = p.CostPrice > 0 ? p.CostPrice : (p.Code switch
                {
                    "AO-001" => 150000m,
                    "QUAN-001" => 280000m,
                    "PK-001" => 120000m,
                    "VAY-001" => 320000m,
                    _ => 100000m
                });

                // 1. Bản ghi kỳ trước (1 tháng trước) - Lịch sử
                costPrices.Add(new CostPriceHist
                {
                    WarehouseId = whMain?.Id,
                    ProductId = p.Id,
                    EffectDate = DateTime.Today.AddMonths(-1).AddDays(-5),
                    CostPrice = Math.Round(baseCost * 0.96m, 0),
                    RefDocNo = $"KYTINH-{DateTime.Today.AddMonths(-1):yyyyMM}",
                    CalcPeriodName = $"Kỳ tính giá vốn tháng {DateTime.Today.AddMonths(-1):MM/yyyy}",
                    IsCurrent = false,
                    SourceType = CostPriceSourceType.AutoCalc,
                    Remark = "Tính giá vốn bình quân gia quyền kỳ trước theo phiếu nhập kho",
                    CreatedBy = "hethong",
                    CreatedAt = DateTime.Today.AddMonths(-1).AddDays(-5)
                });

                // 2. Bản ghi hiện hành (áp dụng từ đầu tháng này)
                costPrices.Add(new CostPriceHist
                {
                    WarehouseId = whMain?.Id,
                    ProductId = p.Id,
                    EffectDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                    CostPrice = baseCost,
                    RefDocNo = $"KYTINH-{DateTime.Today:yyyyMM}",
                    CalcPeriodName = $"Kỳ tính giá vốn tháng {DateTime.Today:MM/yyyy}",
                    IsCurrent = true,
                    SourceType = CostPriceSourceType.AutoCalc,
                    Remark = "Chốt giá vốn bình quân gia quyền kỳ hiện hành",
                    CreatedBy = "admin",
                    CreatedAt = DateTime.Today.AddDays(-10)
                });

                // 3. Nếu có kho phụ, thêm giá vốn kho phụ (có thể chênh lệch chi phí vận chuyển lưu kho)
                if (whSub != null)
                {
                    costPrices.Add(new CostPriceHist
                    {
                        WarehouseId = whSub.Id,
                        ProductId = p.Id,
                        EffectDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                        CostPrice = Math.Round(baseCost * 1.02m, 0), // Kho phụ cộng thêm 2% chi phí luân chuyển
                        RefDocNo = $"KYTINH-{DateTime.Today:yyyyMM}-HCM",
                        CalcPeriodName = $"Kỳ tính giá vốn tháng {DateTime.Today:MM/yyyy} - {whSub.Name}",
                        IsCurrent = true,
                        SourceType = CostPriceSourceType.AutoCalc,
                        Remark = $"Giá vốn kho {whSub.Name} bao gồm chi phí điều chuyển luân kho",
                        CreatedBy = "admin",
                        CreatedAt = DateTime.Today.AddDays(-10)
                    });
                }
            }

            db.CostPriceHists.AddRange(costPrices);
            await db.SaveChangesAsync();
        }

        // Seed dữ liệu Kỳ chốt tồn kho & Snapshot số dư (PeriodClosing - port từ Rpt_In_Out_Inv Skycic)
        if (!await db.PeriodClosings.AnyAsync())
        {
            var prods = await db.Products.ToListAsync();
            var whHn = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HN");
            var whHcm = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HCM");

            if (whHn != null && prods.Any())
            {
                // Kỳ 1: Chốt sổ Tháng 02/2026 (Đã chốt sổ thành công)
                var closingFeb = new PeriodClosing
                {
                    Code = "CK2602-001",
                    PeriodMonth = new DateTime(2026, 2, 1),
                    PeriodName = "Kỳ chốt kho Tháng 02/2026 (Toàn hệ thống)",
                    WarehouseId = null,
                    Status = PeriodClosingStatus.Closed,
                    ClosedAt = new DateTime(2026, 2, 28, 17, 30, 0),
                    ClosedBy = "ketoankho",
                    Note = "Chốt sổ tồn kho tháng 02/2026, số liệu đã đối soát khớp với Thẻ kho và Biên bản kiểm kê định kỳ",
                    CreatedAt = new DateTime(2026, 2, 28, 17, 30, 0)
                };

                foreach (var p in prods)
                {
                    decimal cost = p.CostPrice > 0 ? p.CostPrice : 150000m;

                    // Dòng tại Kho Hà Nội
                    closingFeb.Lines.Add(new PeriodClosingLine
                    {
                        WarehouseId = whHn.Id,
                        ProductId = p.Id,
                        OpeningQty = 100,
                        InQty = 20,
                        LastInPrice = cost,
                        InAmount = 20 * cost,
                        OutQty = 15,
                        LastOutPrice = cost,
                        OutAmount = 15 * cost,
                        ClosingQty = 105,
                        CostPrice = cost,
                        ClosingValue = 105 * cost,
                        Note = "Chốt số dư tháng 02/2026"
                    });

                    // Dòng tại Kho TP.HCM (nếu có)
                    if (whHcm != null)
                    {
                        closingFeb.Lines.Add(new PeriodClosingLine
                        {
                            WarehouseId = whHcm.Id,
                            ProductId = p.Id,
                            OpeningQty = 20,
                            InQty = 10,
                            LastInPrice = cost,
                            InAmount = 10 * cost,
                            OutQty = 5,
                            LastOutPrice = cost,
                            OutAmount = 5 * cost,
                            ClosingQty = 25,
                            CostPrice = cost,
                            ClosingValue = 25 * cost,
                            Note = "Chốt số dư tháng 02/2026"
                        });
                    }
                }

                db.PeriodClosings.Add(closingFeb);

                // Kỳ 2: Chốt sổ Tháng 01/2026 (Kỳ đầu năm)
                var closingJan = new PeriodClosing
                {
                    Code = "CK2601-001",
                    PeriodMonth = new DateTime(2026, 1, 1),
                    PeriodName = "Kỳ chốt kho Tháng 01/2026 (Toàn hệ thống)",
                    WarehouseId = null,
                    Status = PeriodClosingStatus.Closed,
                    ClosedAt = new DateTime(2026, 1, 31, 18, 0, 0),
                    ClosedBy = "ketoankho",
                    Note = "Chốt sổ kỳ đầu năm 2026, bàn giao số dư năm tài chính mới",
                    CreatedAt = new DateTime(2026, 1, 31, 18, 0, 0)
                };

                foreach (var p in prods)
                {
                    decimal cost = p.CostPrice > 0 ? p.CostPrice : 150000m;
                    closingJan.Lines.Add(new PeriodClosingLine
                    {
                        WarehouseId = whHn.Id,
                        ProductId = p.Id,
                        OpeningQty = 90,
                        InQty = 30,
                        LastInPrice = cost,
                        InAmount = 30 * cost,
                        OutQty = 20,
                        LastOutPrice = cost,
                        OutAmount = 20 * cost,
                        ClosingQty = 100,
                        CostPrice = cost,
                        ClosingValue = 100 * cost,
                        Note = "Chốt số dư tháng 01/2026"
                    });
                }

                db.PeriodClosings.Add(closingJan);
                await db.SaveChangesAsync();
            }
        }

        // Seed dữ liệu Quản lý Thùng Carton & Đóng kiện hàng hoá (InventoryCarton - port từ Inv_InventoryCarton Skycic)
        if (!await db.InventoryCartons.AnyAsync())
        {
            var whHn = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HN");
            var whHcm = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HCM");
            var prods = await db.Products.ToListAsync();
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001");
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001");
            var pk = prods.FirstOrDefault(p => p.Code == "PK-001");
            var vay = prods.FirstOrDefault(p => p.Code == "VAY-001");

            var today = DateTime.Today;

            if (whHn != null && ao != null && quan != null && pk != null && vay != null)
            {
                var cartons = new List<InventoryCarton>
                {
                    // 1. Thùng đã niêm phong - Kho Hà Nội (Áo sơ mi trắng)
                    new()
                    {
                        WarehouseId = whHn.Id,
                        CartonCode = "CTN2603-HN01",
                        QrCode = "CTN2603-HN01",
                        CartonType = "Thùng carton 5 lớp sóng BC (60x40x40)",
                        ProductId = ao.Id,
                        LotNo = "LOT-AO26-01",
                        Quantity = 30,
                        Capacity = 50,
                        LengthCm = 60, WidthCm = 40, HeightCm = 40,
                        GrossWeightKg = 12.5,
                        Status = CartonStatus.Sealed,
                        ShelfLocation = "A-01-01",
                        PackerName = "Nguyễn Văn Đóng",
                        PackedAt = today.AddDays(-5),
                        SealedAt = today.AddDays(-5).AddHours(2),
                        Remark = "Kiện hàng áo sơ mi trắng đóng thùng đạt chuẩn xuất khẩu"
                    },
                    // 2. Thùng đã niêm phong - Kho Hà Nội (Quần jeans slim)
                    new()
                    {
                        WarehouseId = whHn.Id,
                        CartonCode = "CTN2603-HN02",
                        QrCode = "CTN2603-HN02",
                        CartonType = "Thùng carton xuất khẩu chịu lực (60x40x40)",
                        ProductId = quan.Id,
                        LotNo = "LOT-QJ26-01",
                        Quantity = 20,
                        Capacity = 30,
                        LengthCm = 60, WidthCm = 40, HeightCm = 40,
                        GrossWeightKg = 16.0,
                        Status = CartonStatus.Sealed,
                        ShelfLocation = "A-01-02",
                        PackerName = "Trần Thị Kiện",
                        PackedAt = today.AddDays(-4),
                        SealedAt = today.AddDays(-4).AddHours(1),
                        Remark = "Đã dán tem kiểm định QC Pass và niêm phong kẹp chì"
                    },
                    // 3. Thùng đang đóng dở dang - Kho Hà Nội (Váy đầm công sở)
                    new()
                    {
                        WarehouseId = whHn.Id,
                        CartonCode = "CTN2603-HN03",
                        QrCode = "CTN2603-HN03",
                        CartonType = "Thùng carton chống ẩm 3 lớp (50x40x30)",
                        ProductId = vay.Id,
                        LotNo = "LOT-VD26-01",
                        Quantity = 15,
                        Capacity = 25,
                        LengthCm = 50, WidthCm = 40, HeightCm = 30,
                        GrossWeightKg = 9.8,
                        Status = CartonStatus.Packing,
                        ShelfLocation = "A-02-01",
                        PackerName = "Lê Văn Hộp",
                        PackedAt = today.AddDays(-1),
                        Remark = "Đang chờ kiểm đếm thêm 10 váy đầm để đủ kiện 25 sp"
                    },
                    // 4. Thùng đã xuất kho giao hàng - Kho Hà Nội (Thắt lưng da)
                    new()
                    {
                        WarehouseId = whHn.Id,
                        CartonCode = "CTN2603-HN04",
                        QrCode = "CTN2603-HN04",
                        CartonType = "Thùng carton nhỏ phụ kiện (40x30x20)",
                        ProductId = pk.Id,
                        LotNo = "LOT-TL26-01",
                        Quantity = 25,
                        Capacity = 40,
                        LengthCm = 40, WidthCm = 30, HeightCm = 20,
                        GrossWeightKg = 8.5,
                        Status = CartonStatus.Shipped,
                        ShelfLocation = "B-01-01",
                        PackerName = "Nguyễn Văn Đóng",
                        PackedAt = today.AddDays(-3),
                        SealedAt = today.AddDays(-3).AddHours(2),
                        ShippedAt = today.AddDays(-1),
                        RefDocNo = "PXSEED-001",
                        Remark = "Đã xuất kho bàn giao đơn vị vận chuyển Viettel Post"
                    },
                    // 5. Thùng rỗng sẵn sàng đóng hàng - Kho Hà Nội
                    new()
                    {
                        WarehouseId = whHn.Id,
                        CartonCode = "CTN2603-HN05",
                        QrCode = "CTN2603-HN05",
                        CartonType = "Thùng carton tiêu chuẩn (40x30x30)",
                        Quantity = 0,
                        Capacity = 50,
                        LengthCm = 40, WidthCm = 30, HeightCm = 30,
                        GrossWeightKg = 0.5,
                        Status = CartonStatus.Empty,
                        ShelfLocation = "B-01-02",
                        Remark = "Thùng rỗng sẵn sàng đóng gói cho ca sản xuất chiều"
                    }
                };

                // 6. Thùng tại Kho TP.HCM
                if (whHcm != null)
                {
                    cartons.Add(new InventoryCarton
                    {
                        WarehouseId = whHcm.Id,
                        CartonCode = "CTN2603-HCM01",
                        QrCode = "CTN2603-HCM01",
                        CartonType = "Thùng carton 5 lớp sóng BC (60x40x40)",
                        ProductId = ao.Id,
                        LotNo = "LOT-AO26-HCM1",
                        Quantity = 20,
                        Capacity = 50,
                        LengthCm = 60, WidthCm = 40, HeightCm = 40,
                        GrossWeightKg = 8.4,
                        Status = CartonStatus.Sealed,
                        ShelfLocation = "KHO-HCM-A",
                        PackerName = "Trần Nam",
                        PackedAt = today.AddDays(-2),
                        SealedAt = today.AddDays(-2).AddHours(1),
                        RefDocNo = "PCSEED-001",
                        Remark = "Hàng nhận từ lệnh điều chuyển nội bộ Kho Hà Nội"
                    });
                }

                db.InventoryCartons.AddRange(cartons);
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        var tables = new[] { "Warehouses", "Products", "Docs", "DocLines", "Audits", "AuditLines", "MoveOrders", "MoveOrderLines", "ReturnToSuppliers", "ReturnToSupplierLines", "CustomerReturns", "CustomerReturnLines", "StockLots", "StockSerials", "InventoryBlocks", "CostPriceHists", "PeriodClosings", "PeriodClosingLines", "InventoryCartons" };
        var sql = new List<string>
        {
            "CREATE TABLE IF NOT EXISTS miniwms.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON miniwms.\"Orgs\" (\"ApiKey\")",
        };
        foreach (var t in tables) sql.Add($"ALTER TABLE miniwms.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        sql.Add("ALTER TABLE miniwms.\"Products\" ADD COLUMN IF NOT EXISTS \"MaxStock\" integer NOT NULL DEFAULT 0");
        sql.Add("ALTER TABLE miniwms.\"Products\" ADD COLUMN IF NOT EXISTS \"CostPrice\" numeric NOT NULL DEFAULT 0");
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
            );",
            @"CREATE TABLE IF NOT EXISTS ""ReturnToSuppliers"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""WarehouseId"" INTEGER NOT NULL,
                ""SupplierName"" TEXT NOT NULL,
                ""SupplierCode"" TEXT NULL,
                ""RefDocNo"" TEXT NULL,
                ""Date"" TEXT NOT NULL,
                ""Reason"" TEXT NULL,
                ""CreatedBy"" TEXT NOT NULL,
                ""Status"" INTEGER NOT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""FinishedAt"" TEXT NULL,
                ""StockDocId"" INTEGER NULL,
                CONSTRAINT ""FK_ReturnToSuppliers_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_ReturnToSuppliers_Docs_StockDocId"" FOREIGN KEY (""StockDocId"") REFERENCES ""Docs"" (""Id"") ON DELETE SET NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ReturnToSuppliers_OrgId_Code"" ON ""ReturnToSuppliers"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""ReturnToSupplierLines"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""ReturnToSupplierId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""Quantity"" INTEGER NOT NULL,
                ""UnitPrice"" TEXT NOT NULL DEFAULT '0',
                ""Note"" TEXT NULL,
                CONSTRAINT ""FK_ReturnToSupplierLines_ReturnToSuppliers_ReturnToSupplierId"" FOREIGN KEY (""ReturnToSupplierId"") REFERENCES ""ReturnToSuppliers"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_ReturnToSupplierLines_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE
            );",
            @"CREATE TABLE IF NOT EXISTS ""CustomerReturns"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""WarehouseId"" INTEGER NOT NULL,
                ""CustomerName"" TEXT NOT NULL,
                ""CustomerCode"" TEXT NULL,
                ""InvoiceNo"" TEXT NULL,
                ""RefOrderNo"" TEXT NULL,
                ""Date"" TEXT NOT NULL,
                ""Reason"" TEXT NULL,
                ""CreatedBy"" TEXT NOT NULL,
                ""Status"" INTEGER NOT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""FinishedAt"" TEXT NULL,
                ""StockDocId"" INTEGER NULL,
                CONSTRAINT ""FK_CustomerReturns_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_CustomerReturns_Docs_StockDocId"" FOREIGN KEY (""StockDocId"") REFERENCES ""Docs"" (""Id"") ON DELETE SET NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CustomerReturns_OrgId_Code"" ON ""CustomerReturns"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""CustomerReturnLines"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""CustomerReturnId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""Quantity"" INTEGER NOT NULL,
                ""UnitPrice"" TEXT NOT NULL DEFAULT '0',
                ""Note"" TEXT NULL,
                CONSTRAINT ""FK_CustomerReturnLines_CustomerReturns_CustomerReturnId"" FOREIGN KEY (""CustomerReturnId"") REFERENCES ""CustomerReturns"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_CustomerReturnLines_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE
            );",
            @"CREATE TABLE IF NOT EXISTS ""StockLots"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""WarehouseId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""LotNo"" TEXT NOT NULL,
                ""ProductionDate"" TEXT NULL,
                ""ExpiredDate"" TEXT NOT NULL,
                ""InDate"" TEXT NOT NULL,
                ""Quantity"" INTEGER NOT NULL,
                ""Note"" TEXT NULL,
                CONSTRAINT ""FK_StockLots_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_StockLots_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_StockLots_OrgId_WarehouseId_ProductId_LotNo"" ON ""StockLots"" (""OrgId"", ""WarehouseId"", ""ProductId"", ""LotNo"");",
            @"CREATE TABLE IF NOT EXISTS ""StockSerials"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""WarehouseId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""SerialNo"" TEXT NOT NULL,
                ""LotNo"" TEXT NULL,
                ""Status"" INTEGER NOT NULL,
                ""InDate"" TEXT NOT NULL,
                ""OutDate"" TEXT NULL,
                ""RefNo"" TEXT NULL,
                ""Note"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                CONSTRAINT ""FK_StockSerials_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_StockSerials_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_StockSerials_OrgId_WarehouseId_ProductId_SerialNo"" ON ""StockSerials"" (""OrgId"", ""WarehouseId"", ""ProductId"", ""SerialNo"");",
            @"CREATE TABLE IF NOT EXISTS ""InventoryBlocks"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""WarehouseId"" INTEGER NOT NULL,
                ""InvBlockCode"" TEXT NOT NULL,
                ""ShelfCode"" TEXT NOT NULL,
                ""InvBlockDesc"" TEXT NULL,
                ""Length"" REAL NOT NULL DEFAULT 0,
                ""Width"" REAL NOT NULL DEFAULT 0,
                ""Height"" REAL NOT NULL DEFAULT 0,
                ""MaxCapacity"" INTEGER NOT NULL DEFAULT 100,
                ""FlagActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                CONSTRAINT ""FK_InventoryBlocks_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_InventoryBlocks_OrgId_WarehouseId_InvBlockCode"" ON ""InventoryBlocks"" (""OrgId"", ""WarehouseId"", ""InvBlockCode"");",
            @"CREATE TABLE IF NOT EXISTS ""CostPriceHists"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""WarehouseId"" INTEGER NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""EffectDate"" TEXT NOT NULL,
                ""CostPrice"" NUMERIC NOT NULL DEFAULT 0,
                ""RefDocNo"" TEXT NULL,
                ""IsCurrent"" INTEGER NOT NULL DEFAULT 1,
                ""CalcPeriodName"" TEXT NULL,
                ""SourceType"" INTEGER NOT NULL DEFAULT 0,
                ""Remark"" TEXT NULL,
                ""CreatedBy"" TEXT NOT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedBy"" TEXT NULL,
                ""UpdatedAt"" TEXT NULL,
                CONSTRAINT ""FK_CostPriceHists_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_CostPriceHists_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_CostPriceHists_OrgId_WarehouseId_ProductId_EffectDate"" ON ""CostPriceHists"" (""OrgId"", ""WarehouseId"", ""ProductId"", ""EffectDate"");",
            @"CREATE TABLE IF NOT EXISTS ""PeriodClosings"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""PeriodMonth"" TEXT NOT NULL,
                ""PeriodName"" TEXT NOT NULL,
                ""WarehouseId"" INTEGER NULL,
                ""Status"" INTEGER NOT NULL DEFAULT 1,
                ""ClosedAt"" TEXT NULL,
                ""ClosedBy"" TEXT NOT NULL,
                ""Note"" TEXT NULL,
                ""ReopenReason"" TEXT NULL,
                ""ReopenedAt"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                CONSTRAINT ""FK_PeriodClosings_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PeriodClosings_OrgId_Code"" ON ""PeriodClosings"" (""OrgId"", ""Code"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_PeriodClosings_OrgId_PeriodMonth_WarehouseId"" ON ""PeriodClosings"" (""OrgId"", ""PeriodMonth"", ""WarehouseId"");",
            @"CREATE TABLE IF NOT EXISTS ""PeriodClosingLines"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""PeriodClosingId"" INTEGER NOT NULL,
                ""WarehouseId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""OpeningQty"" INTEGER NOT NULL,
                ""InQty"" INTEGER NOT NULL,
                ""LastInPrice"" NUMERIC NOT NULL DEFAULT 0,
                ""InAmount"" NUMERIC NOT NULL DEFAULT 0,
                ""OutQty"" INTEGER NOT NULL,
                ""LastOutPrice"" NUMERIC NOT NULL DEFAULT 0,
                ""OutAmount"" NUMERIC NOT NULL DEFAULT 0,
                ""ClosingQty"" INTEGER NOT NULL,
                ""CostPrice"" NUMERIC NOT NULL DEFAULT 0,
                ""ClosingValue"" NUMERIC NOT NULL DEFAULT 0,
                ""Note"" TEXT NULL,
                CONSTRAINT ""FK_PeriodClosingLines_PeriodClosings_PeriodClosingId"" FOREIGN KEY (""PeriodClosingId"") REFERENCES ""PeriodClosings"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_PeriodClosingLines_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_PeriodClosingLines_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE RESTRICT
            );",
            @"ALTER TABLE ""Products"" ADD COLUMN ""MaxStock"" INTEGER NOT NULL DEFAULT 0;",
            @"ALTER TABLE ""Products"" ADD COLUMN ""CostPrice"" NUMERIC NOT NULL DEFAULT 0;",
            @"CREATE TABLE IF NOT EXISTS ""InventoryCartons"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""CartonCode"" TEXT NOT NULL,
                ""QrCode"" TEXT NULL,
                ""WarehouseId"" INTEGER NOT NULL,
                ""CartonType"" TEXT NOT NULL DEFAULT 'Thùng carton tiêu chuẩn',
                ""ProductId"" INTEGER NULL,
                ""LotNo"" TEXT NULL,
                ""Quantity"" INTEGER NOT NULL DEFAULT 0,
                ""Capacity"" INTEGER NOT NULL DEFAULT 50,
                ""LengthCm"" REAL NOT NULL DEFAULT 40,
                ""WidthCm"" REAL NOT NULL DEFAULT 30,
                ""HeightCm"" REAL NOT NULL DEFAULT 30,
                ""GrossWeightKg"" REAL NOT NULL DEFAULT 0,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""ShelfLocation"" TEXT NULL,
                ""PackerName"" TEXT NULL,
                ""PackedAt"" TEXT NULL,
                ""SealedAt"" TEXT NULL,
                ""ShippedAt"" TEXT NULL,
                ""RefDocNo"" TEXT NULL,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                CONSTRAINT ""FK_InventoryCartons_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_InventoryCartons_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE SET NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_InventoryCartons_OrgId_CartonCode"" ON ""InventoryCartons"" (""OrgId"", ""CartonCode"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_InventoryCartons_OrgId_WarehouseId_Status"" ON ""InventoryCartons"" (""OrgId"", ""WarehouseId"", ""Status"");"
        };
        foreach (var s in sql)
        {
            try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
        }
    }
}

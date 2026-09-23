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
                new Warehouse { Code = "KHO-HN", Name = "Kho Hà Nội", Address = "KCN Bắc Thăng Long", InvTypeCode = "KHO_TONG", InvLevelTypeCode = "CAP_1", AreaCode = "AREA_HN", Remark = "Tổng kho trung tâm miền Bắc điều phối hàng hóa toàn quốc" },
                new Warehouse { Code = "KHO-HCM", Name = "Kho TP.HCM", Address = "KCN Tân Bình", InvTypeCode = "KHO_TC", InvLevelTypeCode = "CAP_2", AreaCode = "AREA_HCM", Remark = "Kho trung chuyển và cung ứng khu vực miền Nam" });
            await db.SaveChangesAsync();
        }
        else
        {
            var existingWhs = await db.Warehouses.ToListAsync();
            bool whChanged = false;
            foreach (var w in existingWhs)
            {
                if (string.IsNullOrWhiteSpace(w.InvTypeCode))
                {
                    w.InvTypeCode = w.Code switch
                    {
                        "KHO-HN" => "KHO_TONG",
                        "KHO-HCM" => "KHO_TC",
                        _ => "KHO_TONG"
                    };
                    whChanged = true;
                }
                if (string.IsNullOrWhiteSpace(w.InvLevelTypeCode))
                {
                    w.InvLevelTypeCode = w.Code switch
                    {
                        "KHO-HN" => "CAP_1",
                        "KHO-HCM" => "CAP_2",
                        _ => "CAP_3"
                    };
                    whChanged = true;
                }
                if (string.IsNullOrWhiteSpace(w.AreaCode))
                {
                    w.AreaCode = w.Code switch
                    {
                        "KHO-HN" => "AREA_HN",
                        "KHO-HCM" => "AREA_HCM",
                        _ => "AREA_HN"
                    };
                    whChanged = true;
                }
            }
            if (whChanged) await db.SaveChangesAsync();
        }
        if (!await db.InventoryTypes.AnyAsync())
        {
            db.InventoryTypes.AddRange(
                new InventoryType { Code = "KHO_TONG", Name = "Kho tổng phân phối", IsActive = true, Remark = "Tổng kho trung tâm lưu trữ và điều phối hàng hóa cho toàn bộ hệ thống chi nhánh" },
                new InventoryType { Code = "KHO_NVL", Name = "Kho nguyên vật liệu", IsActive = true, Remark = "Bảo quản nguyên liệu, vải tấm, phụ liệu may mặc cấp phát cho xưởng sản xuất" },
                new InventoryType { Code = "KHO_TP", Name = "Kho thành phẩm", IsActive = true, Remark = "Tiếp nhận sản phẩm hoàn chỉnh đạt KCS, lưu kho chờ xuất bán hoặc giao đại lý" },
                new InventoryType { Code = "KHO_TC", Name = "Kho trung chuyển / Hub", IsActive = true, Remark = "Trạm trung chuyển kết nối logistics giữa các vùng miền và kho chi nhánh" },
                new InventoryType { Code = "KHO_BH", Name = "Kho bảo hành & Linh kiện", IsActive = true, Remark = "Lưu trữ hàng lỗi bảo hành, phụ tùng linh kiện thay thế và xử lý tân trang" },
                new InventoryType { Code = "KHO_DL", Name = "Kho ký gửi đại lý", IsActive = true, Remark = "Kho đặt tại các showroom đại lý ủy quyền và điểm bán phân phối ngoài" }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.InventoryLevelTypes.AnyAsync())
        {
            db.InventoryLevelTypes.AddRange(
                new InventoryLevelType { Code = "CAP_1", Name = "Kho Cấp 1 - Tổng kho trung ương", IsActive = true, Remark = "Tổng kho quy mô quốc gia, trung tâm dự trữ chiến lược và điều phối toàn hệ thống" },
                new InventoryLevelType { Code = "CAP_2", Name = "Kho Cấp 2 - Kho vùng & khu vực", IsActive = true, Remark = "Kho trung tâm khu vực Bắc / Trung / Nam, tiếp nhận điều phối cho các kho chi nhánh cấp 3" },
                new InventoryLevelType { Code = "CAP_3", Name = "Kho Cấp 3 - Kho chi nhánh & tỉnh", IsActive = true, Remark = "Kho phục vụ phân phối địa phương, cung ứng hàng lẻ, showroom và khách hàng trực tiếp" },
                new InventoryLevelType { Code = "HUB", Name = "Kho Hub phân loại & trung chuyển", IsActive = true, Remark = "Điểm trung chuyển hàng hóa tốc độ cao, xử lý chia chọn cross-docking giao nhanh" },
                new InventoryLevelType { Code = "KHO_DAILY", Name = "Kho đại lý ủy quyền & ký gửi", IsActive = true, Remark = "Điểm lưu kho tại đại lý phân phối ủy quyền, điểm nhượng quyền hoặc chuỗi đối tác" }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.InventoryInTypes.AnyAsync())
        {
            db.InventoryInTypes.AddRange(
                new InventoryInType { Code = "IN_BUY", Name = "Nhập mua hàng Nhà cung cấp", FlagStatistic = true, IsActive = true, Remark = "Tiếp nhận hàng mua mới từ NCC theo đơn đặt hàng PO và hóa đơn mua hàng" },
                new InventoryInType { Code = "IN_PROD", Name = "Nhập thành phẩm sản xuất KCS", FlagStatistic = true, IsActive = true, Remark = "Tiếp nhận thành phẩm từ xưởng sản xuất hoàn thành kiểm định KCS nhập kho" },
                new InventoryInType { Code = "IN_RETURN", Name = "Nhập hàng khách trả lại", FlagStatistic = false, IsActive = true, Remark = "Tiếp nhận hàng đổi trả, bảo hành hoặc khách hàng hoàn đơn bán" },
                new InventoryInType { Code = "IN_TRANSFER", Name = "Nhập điều chuyển kho đến", FlagStatistic = false, IsActive = true, Remark = "Tiếp nhận hàng luân chuyển từ kho nội bộ khác theo lệnh điều chuyển" },
                new InventoryInType { Code = "IN_AUDIT", Name = "Nhập cân bằng kiểm kê thừa", FlagStatistic = false, IsActive = true, Remark = "Phiếu nhập điều chỉnh tăng số dư khi thực tế kiểm đếm lớn hơn tồn sổ sách" },
                new InventoryInType { Code = "IN_SAMPLE", Name = "Nhập hàng mẫu / Triển lãm", FlagStatistic = false, IsActive = true, Remark = "Nhập hàng mẫu trưng bày, thử nghiệm chất lượng hoặc hàng tài trợ không tính giá" },
                new InventoryInType { Code = "IN_OTHER", Name = "Nhập kho điều chỉnh khác", FlagStatistic = false, IsActive = true, Remark = "Các giao dịch nhập kho phát sinh ngoài danh mục tiêu chuẩn" }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.InventoryOutTypes.AnyAsync())
        {
            db.InventoryOutTypes.AddRange(
                new InventoryOutType { Code = "OUT_SALE", Name = "Xuất bán hàng / Phân phối đại lý", FlagStatistic = true, IsActive = true, Remark = "Xuất hàng thương mại cho khách hàng hoặc đại lý theo hợp đồng bán buôn, đơn hàng phân phối" },
                new InventoryOutType { Code = "OUT_PROD", Name = "Xuất cấp phát nguyên vật liệu sản xuất", FlagStatistic = true, IsActive = true, Remark = "Cấp phát nguyên liệu, phụ tùng và vật tư cho các phân xưởng sản xuất theo lệnh sản xuất" },
                new InventoryOutType { Code = "OUT_TRANSFER", Name = "Xuất điều chuyển kho đi", FlagStatistic = false, IsActive = true, Remark = "Xuất luân chuyển hàng hóa sang kho chi nhánh hoặc trung tâm phân phối khác theo lệnh điều chuyển" },
                new InventoryOutType { Code = "OUT_RETURN_SUP", Name = "Xuất trả hàng Nhà cung cấp", FlagStatistic = false, IsActive = true, Remark = "Xuất trả hàng lỗi hỏng, sai quy cách hoặc dư thừa cho nhà cung cấp theo phiếu trả NCC" },
                new InventoryOutType { Code = "OUT_AUDIT", Name = "Xuất cân bằng kiểm kê thiếu", FlagStatistic = false, IsActive = true, Remark = "Phiếu xuất điều chỉnh giảm số dư khi thực tế kiểm đếm nhỏ hơn tồn sổ sách sau kiểm kê" },
                new InventoryOutType { Code = "OUT_DISPOSAL", Name = "Xuất hủy / Thanh lý hàng hỏng hóc", FlagStatistic = false, IsActive = true, Remark = "Tiêu hủy hàng hóa quá hạn sử dụng, biến chất, lỗi kỹ thuật hoặc thanh lý thu hồi phế liệu" },
                new InventoryOutType { Code = "OUT_SAMPLE", Name = "Xuất hàng mẫu / Quảng bá / Khuyến mại", FlagStatistic = false, IsActive = true, Remark = "Xuất hàng phục vụ chào hàng đối tác, trưng bày hội chợ triển lãm hoặc tặng kèm khuyến mại" },
                new InventoryOutType { Code = "OUT_OTHER", Name = "Xuất kho điều chỉnh khác", FlagStatistic = false, IsActive = true, Remark = "Các giao dịch xuất kho phát sinh ngoài danh mục tiêu chuẩn" }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.MoveOrdTypes.AnyAsync())
        {
            db.MoveOrdTypes.AddRange(
                new MoveOrdType { Code = "MOVE_BRANCH", Name = "Điều chuyển chi nhánh & Cửa hàng", Description = "Phân phối hàng định kỳ từ tổng kho phân phối đến các kho chi nhánh, showroom hoặc điểm bán", IsUrgent = false, IsActive = true },
                new MoveOrdType { Code = "MOVE_REPLENISH", Name = "Điều chuyển bổ sung định mức an toàn", Description = "Bổ sung hàng tồn kho khẩn cấp hoặc định kỳ khi kho nhận chạm ngưỡng tồn tối thiểu MinStock", IsUrgent = false, IsActive = true },
                new MoveOrdType { Code = "MOVE_TRANSIT", Name = "Điều chuyển qua Hub trung chuyển", Description = "Luân chuyển hàng qua các trạm trung chuyển trung gian (Hub logistics) trước khi về kho đích", IsUrgent = false, IsActive = true },
                new MoveOrdType { Code = "MOVE_WARRANTY", Name = "Điều chuyển bảo hành & Kiểm định", Description = "Chuyển hàng hóa lỗi kỹ thuật, nghi ngờ chất lượng về kho thẩm định kỹ thuật hoặc trung tâm bảo hành", IsUrgent = true, IsActive = true },
                new MoveOrdType { Code = "MOVE_REORG", Name = "Điều chuyển quy hoạch & Sắp xếp lại kho", Description = "Chuyển hàng sắp xếp gom kho, tối ưu thể tích lưu trữ khay kệ hoặc giải tỏa kho bảo trì", IsUrgent = false, IsActive = true },
                new MoveOrdType { Code = "MOVE_DISPOSAL", Name = "Điều chuyển tập kết xử lý hàng hủy", Description = "Chuyển gom hàng hỏng nặng, hết hạn sử dụng về kho cách ly chờ làm thủ tục tiêu hủy", IsUrgent = false, IsActive = true }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.Dealers.AnyAsync())
        {
            var whHn = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HN");
            var whHcm = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HCM");
            var whDn = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-DN");

            db.Dealers.AddRange(
                // Cấp 1: Tổng Đại lý Phân phối Vùng (Level 1)
                new Dealer
                {
                    Code = "DL_MB01",
                    Name = "Tổng Đại lý Phân phối Miền Bắc - Sao Mai",
                    ParentCode = null,
                    Level = 1,
                    DealerType = "Đại lý độc quyền",
                    BUCode = "BU_NORTH",
                    ProvinceCode = "Hà Nội",
                    Address = "Số 188 Nguyễn Trãi, Thanh Xuân, Hà Nội",
                    PresentBy = "Trần Đình Khang",
                    GovIdNumber = "0108876543",
                    Phone = "024-38665544",
                    Email = "saomai.mb@dailywms.vn",
                    WarehouseId = whHn?.Id,
                    IsActive = true,
                    Remark = "Tổng đại lý bao tiêu thị trường phía Bắc, định mức công nợ 60 ngày",
                    CreatedAt = DateTime.Now.AddDays(-120)
                },
                new Dealer
                {
                    Code = "DL_MN01",
                    Name = "Tổng Đại lý Phân phối Miền Nam - Phương Nam",
                    ParentCode = null,
                    Level = 1,
                    DealerType = "Đại lý độc quyền",
                    BUCode = "BU_SOUTH",
                    ProvinceCode = "TP. Hồ Chí Minh",
                    Address = "Số 450 Hai Bà Trưng, Phường Tân Định, Quận 1, TP.HCM",
                    PresentBy = "Võ Văn Hậu",
                    GovIdNumber = "0309988776",
                    Phone = "028-38221100",
                    Email = "phuongnam.mn@dailywms.vn",
                    WarehouseId = whHcm?.Id,
                    IsActive = true,
                    Remark = "Tổng đại lý phân phối toàn bộ thị trường Đông & Tây Nam Bộ",
                    CreatedAt = DateTime.Now.AddDays(-120)
                },
                new Dealer
                {
                    Code = "DL_MT01",
                    Name = "Tổng Đại lý Phân phối Miền Trung - Sông Hàn",
                    ParentCode = null,
                    Level = 1,
                    DealerType = "Đại lý độc quyền",
                    BUCode = "BU_CENTRAL",
                    ProvinceCode = "Đà Nẵng",
                    Address = "Số 92 Điện Biên Phủ, Thanh Khê, TP. Đà Nẵng",
                    PresentBy = "Nguyễn Hữu Cảnh",
                    GovIdNumber = "0407766554",
                    Phone = "0236-3755443",
                    Email = "songhan.mt@dailywms.vn",
                    WarehouseId = whDn?.Id,
                    IsActive = true,
                    Remark = "Tổng đại lý trung tâm miền Trung & khu vực Tây Nguyên",
                    CreatedAt = DateTime.Now.AddDays(-120)
                },

                // Cấp 2: Đại lý khu vực tỉnh / thành (Level 2)
                new Dealer
                {
                    Code = "DL_HP01",
                    Name = "Đại lý Khu vực Hải Phòng - Cảng Xanh",
                    ParentCode = "DL_MB01",
                    Level = 2,
                    DealerType = "Đại lý phổ thông",
                    BUCode = "BU_NORTH",
                    ProvinceCode = "Hải Phòng",
                    Address = "Số 55 Lạch Tray, Ngô Quyền, Hải Phòng",
                    PresentBy = "Lê Hải Triều",
                    GovIdNumber = "0316655443",
                    Phone = "0225-3844332",
                    Email = "cangxanh.hp@dailywms.vn",
                    WarehouseId = whHn?.Id,
                    IsActive = true,
                    Remark = "Đại lý cấp 2 phân phối khu vực Hải Phòng, Quảng Ninh",
                    CreatedAt = DateTime.Now.AddDays(-100)
                },
                new Dealer
                {
                    Code = "DL_CT01",
                    Name = "Đại lý Khu vực Tây Nam Bộ - Cần Thơ",
                    ParentCode = "DL_MN01",
                    Level = 2,
                    DealerType = "Đại lý phổ thông",
                    BUCode = "BU_SOUTH",
                    ProvinceCode = "Cần Thơ",
                    Address = "Số 120 30 Tháng 4, Ninh Kiều, Cần Thơ",
                    PresentBy = "Phạm Thanh Phong",
                    GovIdNumber = "0925544332",
                    Phone = "0292-3833221",
                    Email = "taynam.ct@dailywms.vn",
                    WarehouseId = whHcm?.Id,
                    IsActive = true,
                    Remark = "Đại lý cấp 2 phân phối khu vực Đồng bằng Sông Cửu Long",
                    CreatedAt = DateTime.Now.AddDays(-90)
                },
                new Dealer
                {
                    Code = "DL_NA01",
                    Name = "Đại lý Khu vực Nghệ An - Lam Giang",
                    ParentCode = "DL_MT01",
                    Level = 2,
                    DealerType = "Đại lý phổ thông",
                    BUCode = "BU_CENTRAL",
                    ProvinceCode = "Nghệ An",
                    Address = "Số 78 Quang Trung, TP. Vinh, Nghệ An",
                    PresentBy = "Hồ Xuân Hương",
                    GovIdNumber = "0384433221",
                    Phone = "0238-3844112",
                    Email = "lamgiang.na@dailywms.vn",
                    WarehouseId = whDn?.Id,
                    IsActive = true,
                    Remark = "Đại lý cấp 2 khu vực Bắc Trung Bộ",
                    CreatedAt = DateTime.Now.AddDays(-80)
                },

                // Cấp 3: Showroom & Điểm bán ủy quyền (Level 3)
                new Dealer
                {
                    Code = "DL_SR_HN",
                    Name = "Showroom Ủy quyền Tràng Thi - Hoàn Kiếm",
                    ParentCode = "DL_MB01",
                    Level = 3,
                    DealerType = "Showroom bán lẻ",
                    BUCode = "BU_NORTH",
                    ProvinceCode = "Hà Nội",
                    Address = "Số 12 Tràng Thi, Hoàn Kiếm, Hà Nội",
                    PresentBy = "Nguyễn Minh Thu",
                    GovIdNumber = "0011928374",
                    Phone = "024-39366688",
                    Email = "trangthi.sr@dailywms.vn",
                    WarehouseId = whHn?.Id,
                    IsActive = true,
                    Remark = "Điểm trưng bày và bán lẻ flagship tại trung tâm Hà Nội",
                    CreatedAt = DateTime.Now.AddDays(-60)
                },
                new Dealer
                {
                    Code = "DL_SR_SG",
                    Name = "Showroom Ủy quyền Nguyễn Huệ - Quận 1",
                    ParentCode = "DL_MN01",
                    Level = 3,
                    DealerType = "Showroom bán lẻ",
                    BUCode = "BU_SOUTH",
                    ProvinceCode = "TP. Hồ Chí Minh",
                    Address = "Số 68 Nguyễn Huệ, Bến Nghé, Quận 1, TP.HCM",
                    PresentBy = "Đoàn Gia Bảo",
                    GovIdNumber = "0791987654",
                    Phone = "028-38299988",
                    Email = "nguyenhue.sr@dailywms.vn",
                    WarehouseId = whHcm?.Id,
                    IsActive = true,
                    Remark = "Điểm trưng bày và bán lẻ cao cấp tại phố đi bộ TP.HCM",
                    CreatedAt = DateTime.Now.AddDays(-60)
                }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.UserMapInventories.AnyAsync())
        {
            var whHn = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HN");
            var whHcm = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HCM");
            var whDn = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-DN");
            var today = DateTime.Today;

            var maps = new List<UserMapInventory>();

            if (whHn != null)
            {
                maps.Add(new UserMapInventory
                {
                    WarehouseId = whHn.Id,
                    UserCode = "admin",
                    UserName = "Hệ thống Quản trị (Admin)",
                    UserRole = "Trưởng kho / Quản lý",
                    Email = "admin@miniwms.vn",
                    Phone = "0901.888.999",
                    IsActive = true,
                    Remark = "Phụ trách điều phối tổng thể Kho Hà Nội và phê duyệt xuất nhập",
                    AssignedBy = "system",
                    AssignedAt = today.AddDays(-90)
                });
                maps.Add(new UserMapInventory
                {
                    WarehouseId = whHn.Id,
                    UserCode = "thukho_hn01",
                    UserName = "Nguyễn Văn Hưng",
                    UserRole = "Thủ kho chính",
                    Email = "hung.nv@miniwms.vn",
                    Phone = "0912.345.678",
                    IsActive = true,
                    Remark = "Quản lý bảo quản hàng hóa, chốt sổ tồn và giám sát thủ tục nhập xuất",
                    AssignedBy = "admin",
                    AssignedAt = today.AddDays(-60)
                });
                maps.Add(new UserMapInventory
                {
                    WarehouseId = whHn.Id,
                    UserCode = "nv_xuatkho_hn",
                    UserName = "Trần Thị Mai Lan",
                    UserRole = "Nhân viên xuất nhập",
                    Email = "lan.ttm@miniwms.vn",
                    Phone = "0988.112.233",
                    IsActive = true,
                    Remark = "Phụ trách tiếp nhận hàng NCC và soạn đơn xuất bán sỉ",
                    AssignedBy = "thukho_hn01",
                    AssignedAt = today.AddDays(-45)
                });
                maps.Add(new UserMapInventory
                {
                    WarehouseId = whHn.Id,
                    UserCode = "kiemke_hn",
                    UserName = "Lê Hoàng Quân",
                    UserRole = "Kiểm kê viên",
                    Email = "quan.lh@miniwms.vn",
                    Phone = "0977.556.677",
                    IsActive = true,
                    Remark = "Định kỳ đối soát tồn thực tế, kiểm tra số lô và serial",
                    AssignedBy = "admin",
                    AssignedAt = today.AddDays(-30)
                });
            }

            if (whHcm != null)
            {
                maps.Add(new UserMapInventory
                {
                    WarehouseId = whHcm.Id,
                    UserCode = "thukho_sg01",
                    UserName = "Võ Minh Trí",
                    UserRole = "Trưởng kho / Quản lý",
                    Email = "tri.vm@miniwms.vn",
                    Phone = "0933.224.466",
                    IsActive = true,
                    Remark = "Tổng chỉ huy kho miền Nam, điều chuyển phân phối đại lý",
                    AssignedBy = "admin",
                    AssignedAt = today.AddDays(-75)
                });
                maps.Add(new UserMapInventory
                {
                    WarehouseId = whHcm.Id,
                    UserCode = "nv_kho_sg",
                    UserName = "Phạm Hồng Ngọc",
                    UserRole = "Thủ kho chính",
                    Email = "ngoc.ph@miniwms.vn",
                    Phone = "0908.776.543",
                    IsActive = true,
                    Remark = "Trực ca vận hành xuất nhập và quét mã vạch",
                    AssignedBy = "thukho_sg01",
                    AssignedAt = today.AddDays(-40)
                });
            }

            if (whDn != null)
            {
                maps.Add(new UserMapInventory
                {
                    WarehouseId = whDn.Id,
                    UserCode = "thukho_dn",
                    UserName = "Đặng Hải Đăng",
                    UserRole = "Thủ kho chính",
                    Email = "dang.dh@miniwms.vn",
                    Phone = "0966.331.122",
                    IsActive = true,
                    Remark = "Quản lý Hub trung chuyển miền Trung",
                    AssignedBy = "admin",
                    AssignedAt = today.AddDays(-50)
                });
            }

            if (maps.Count > 0)
            {
                foreach (var m in maps)
                {
                    m.DepartmentCode = m.UserRole.Contains("Kiểm kê") ? "PB_KCS" : "PB_QLKHO";
                }
                db.UserMapInventories.AddRange(maps);
                await db.SaveChangesAsync();
            }
        }
        else
        {
            var existingMaps = await db.UserMapInventories.ToListAsync();
            bool mapChanged = false;
            foreach (var m in existingMaps)
            {
                if (string.IsNullOrEmpty(m.DepartmentCode))
                {
                    m.DepartmentCode = m.UserRole.Contains("Kiểm kê") ? "PB_KCS" : "PB_QLKHO";
                    mapChanged = true;
                }
            }
            if (mapChanged) await db.SaveChangesAsync();
        }

        var existingOutDocs = await db.Docs.Where(d => d.Type == DocType.Out).ToListAsync();
        bool docChanged = false;
        foreach (var d in existingOutDocs)
        {
            if (string.IsNullOrEmpty(d.DepartmentCode))
            {
                d.DepartmentCode = "PX_LAPRAP";
                d.DepartmentName = "Phân xưởng Lắp ráp & Hoàn thiện";
                docChanged = true;
            }
        }
        if (docChanged) await db.SaveChangesAsync();
        if (!await db.PartTypes.AnyAsync())
        {
            db.PartTypes.AddRange(
                new PartType { Code = "TP", Name = "Thành phẩm", IsActive = true, Remark = "Hàng hóa hoàn chỉnh hoàn tất quy trình sản xuất KCS hoặc đóng gói sẵn sàng xuất bán" },
                new PartType { Code = "BTP", Name = "Bán thành phẩm", IsActive = true, Remark = "Sản phẩm dở dang, chi tiết cụm lắp ráp trung gian phục vụ hoàn thiện tiếp theo" },
                new PartType { Code = "NVL", Name = "Nguyên vật liệu", IsActive = true, Remark = "Nguyên liệu và vật tư chính đưa vào quy trình sản xuất (vải dệt, da thật, sợi)" },
                new PartType { Code = "PTLK", Name = "Phụ tùng & Linh kiện", IsActive = true, Remark = "Phụ kiện phần cứng, linh kiện kim khí, phụ tùng bảo dưỡng thay thế" },
                new PartType { Code = "BBDG", Name = "Bao bì đóng gói", IsActive = true, Remark = "Thùng carton, hộp duplex, tem nhãn, pallet, vật liệu đóng gói kẹp chì" },
                new PartType { Code = "CCDC", Name = "Công cụ & Dụng cụ kho", IsActive = true, Remark = "Thiết bị đo lường, kéo may, vật tư tiêu hao hỗ trợ vận hành kho" },
                new PartType { Code = "HHTM", Name = "Hàng hóa thương mại", IsActive = true, Remark = "Hàng mua về bán lại nguyên trạng, không qua gia công chế tạo" }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.Brands.AnyAsync())
        {
            db.Brands.AddRange(
                new Brand { Code = "MAY10", Name = "May 10", Origin = "Việt Nam", IsActive = true, Remark = "Tổng công ty May 10 - Thương hiệu thời trang công sở và sơ mi hàng đầu Việt Nam" },
                new Brand { Code = "VIETTIEN", Name = "Việt Tiến", Origin = "Việt Nam", IsActive = true, Remark = "Tổng công ty CP May Việt Tiến - Thương hiệu trang phục nam công sở lịch lãm" },
                new Brand { Code = "ANPHUOC", Name = "An Phước - Pierre Cardin", Origin = "Việt Nam / Pháp", IsActive = true, Remark = "Thương hiệu thời trang & phụ kiện da thủ công cao cấp" },
                new Brand { Code = "LEVI", Name = "Levi's", Origin = "Mỹ", IsActive = true, Remark = "Thương hiệu thời trang jeans denim và phong cách hiện đại quốc tế" },
                new Brand { Code = "CANIFA", Name = "CANIFA", Origin = "Việt Nam", IsActive = true, Remark = "Thời trang ứng dụng thường ngày và trang phục gia đình chất liệu len sợi" },
                new Brand { Code = "NEM", Name = "NEM Fashion", Origin = "Việt Nam", IsActive = true, Remark = "Thương hiệu thời trang thiết kế váy đầm nữ công sở thanh lịch" }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.PartColors.AnyAsync())
        {
            db.PartColors.AddRange(
                new PartColor { Code = "DEN", Name = "Black", NameVN = "Đen", IsActive = true, Remark = "Màu đen cơ bản, dễ phối đồ, phù hợp trang phục công sở và dạo phố" },
                new PartColor { Code = "TRANG", Name = "White", NameVN = "Trắng", IsActive = true, Remark = "Màu trắng tinh khôi, sáng sủa cho sơ mi và áo thun" },
                new PartColor { Code = "XANH-NAVY", Name = "Navy Blue", NameVN = "Xanh navy", IsActive = true, Remark = "Xanh navy đậm lịch lãm, phổ biến cho âu phục và jeans" },
                new PartColor { Code = "XANH-DUONG", Name = "Blue", NameVN = "Xanh dương", IsActive = true, Remark = "Xanh dương denim đặc trưng cho quần jeans và sơ mi casual" },
                new PartColor { Code = "DO-DAM", Name = "Dark Red", NameVN = "Đỏ đậm", IsActive = true, Remark = "Đỏ đậm sang trọng cho váy đầm dạ hội và phụ kiện" },
                new PartColor { Code = "NAU-DA", Name = "Leather Brown", NameVN = "Nâu da", IsActive = true, Remark = "Nâu da bò tự nhiên cho thắt lưng, ví da thủ công" },
                new PartColor { Code = "XAM", Name = "Gray", NameVN = "Xám", IsActive = false, Remark = "Xám ghi trung tính, tạm ngừng áp dụng để chuẩn hóa bảng màu" }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.PartUnits.AnyAsync())
        {
            db.PartUnits.AddRange(
                new PartUnit { Code = "CAI", Name = "Cái", IsStandard = true, IsActive = true, Remark = "Đơn vị tính cơ bản cho sản phẩm đơn chiếc, may mặc, phụ kiện" },
                new PartUnit { Code = "HOP", Name = "Hộp", IsStandard = true, IsActive = true, Remark = "Quy cách đóng gói hộp duplex / hộp carton nhỏ" },
                new PartUnit { Code = "THUNG", Name = "Thùng", IsStandard = false, IsActive = true, Remark = "Đơn vị bao bì đóng gói master carton vận chuyển" },
                new PartUnit { Code = "KG", Name = "Kilogram", IsStandard = true, IsActive = true, Remark = "Đơn vị đo khối lượng chuẩn hệ SI cho nguyên vật liệu sợi bông, vải tấm" },
                new PartUnit { Code = "MET", Name = "Mét", IsStandard = true, IsActive = true, Remark = "Đơn vị đo chiều dài cho cuộn vải, ruy băng may mặc" },
                new PartUnit { Code = "CUON", Name = "Cuộn", IsStandard = false, IsActive = true, Remark = "Đơn vị bao gói dạng cuộn tròn dây kéo, vải lót, màng co" },
                new PartUnit { Code = "BO", Name = "Bộ", IsStandard = true, IsActive = true, Remark = "Đơn vị theo bộ sản phẩm hoàn chỉnh gồm nhiều chi tiết đi kèm" },
                new PartUnit { Code = "CHIEC", Name = "Chiếc", IsStandard = true, IsActive = true, Remark = "Đơn vị đếm hàng hóa cá thể hóa đơn lẻ" },
                new PartUnit { Code = "PALLET", Name = "Pallet", IsStandard = false, IsActive = true, Remark = "Đơn vị quy đổi bốc xếp lưu kho theo kiện nâng pallet tiêu chuẩn" },
                new PartUnit { Code = "VI", Name = "Vỉ", IsStandard = false, IsActive = true, Remark = "Đơn vị đóng vỉ phụ liệu cúc, khóa, tem nhãn" }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.PartMaterialTypes.AnyAsync())
        {
            db.PartMaterialTypes.AddRange(
                new PartMaterialType { Code = "COTTON", Name = "Vải sợi Cotton 100%", IsActive = true, Remark = "Sợi bông tự nhiên, thoáng mát, thấm hút mồ hôi, bảo quản nơi khô ráo thoáng khí" },
                new PartMaterialType { Code = "KAKI", Name = "Vải Kaki dệt thoi", IsActive = true, Remark = "Sợi dệt thoi mật độ cao, độ bền cơ học tốt, chống nhăn xước, dùng cho âu phục và đồng phục" },
                new PartMaterialType { Code = "LEATHER", Name = "Da bò thuộc cao cấp", IsActive = true, Remark = "Da tự nhiên xử lý thủ công, độ đàn hồi cao, tránh ẩm mốc và nhiệt độ quá cao" },
                new PartMaterialType { Code = "DENIM", Name = "Vải Denim sợi chéo", IsActive = true, Remark = "Vải jean cotton dệt chéo chàm bền chắc, phong cách thời trang trẻ trung năng động" },
                new PartMaterialType { Code = "SILK", Name = "Lụa tơ tằm tự nhiên", IsActive = true, Remark = "Chất liệu cao cấp mềm mịn, chống tích điện, bảo quản trong túi vải chuyên dụng" },
                new PartMaterialType { Code = "POLYESTER", Name = "Sợi tổng hợp Polyester", IsActive = true, Remark = "Chống nước nhẹ, định hình form dáng tốt, chống co rút khi giặt" },
                new PartMaterialType { Code = "STEEL", Name = "Hợp kim thép không gỉ Inox", IsActive = true, Remark = "Vật liệu kim khí chế tạo phụ kiện khóa, móc, chốt chống ăn mòn oxi hóa" },
                new PartMaterialType { Code = "PLASTIC", Name = "Nhựa kỹ thuật ABS/PP", IsActive = true, Remark = "Vật liệu polymer chịu va đập, chế tạo khuy cúc áo, thẻ bài, móc treo" }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.ProductAttributes.AnyAsync())
        {
            db.ProductAttributes.AddRange(
                new ProductAttribute { Code = "COLOR", Name = "Màu sắc", NetworkId = "WH01", IsActive = true },
                new ProductAttribute { Code = "SIZE", Name = "Kích cỡ / Size", NetworkId = "WH01", IsActive = true },
                new ProductAttribute { Code = "MATERIAL", Name = "Chất liệu cấu thành", NetworkId = "WH01", IsActive = true },
                new ProductAttribute { Code = "WEIGHT", Name = "Khối lượng (gram)", NetworkId = "WH01", IsActive = true },
                new ProductAttribute { Code = "ORIGIN", Name = "Xuất xứ / Nguồn gốc", NetworkId = "WH01", IsActive = true },
                new ProductAttribute { Code = "WARRANTY", Name = "Thời hạn bảo hành (tháng)", NetworkId = "WH01", IsActive = true },
                new ProductAttribute { Code = "PACKING", Name = "Quy cách đóng gói", NetworkId = "WH01", IsActive = true },
                new ProductAttribute { Code = "BRAND", Name = "Thương hiệu sản phẩm", NetworkId = "WH01", IsActive = false }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.ProductModels.AnyAsync())
        {
            db.ProductModels.AddRange(
                new ProductModel { Code = "MD-M10-SLIM", Name = "Sơ mi Nam Slimfit Oxford", BrandCode = "MAY10", OrgModelCode = "M10-SL26", IsActive = true, Remark = "Dòng sơ mi dáng ôm vừa trẻ trung, chất liệu cotton thoáng mát cao cấp" },
                new ProductModel { Code = "MD-M10-CLASSIC", Name = "Sơ mi Nam Classic Công sở", BrandCode = "MAY10", OrgModelCode = "M10-CL26", IsActive = true, Remark = "Dòng sơ mi dáng suông cổ điển, phong cách lịch lãm quý phái" },
                new ProductModel { Code = "MD-LV-501", Name = "Quần Jeans 501 Original Fit", BrandCode = "LEVI", OrgModelCode = "LV-501-STD", IsActive = true, Remark = "Dòng quần jeans kinh điển phong cách Mỹ, độ bền vượt trội" },
                new ProductModel { Code = "MD-AP-LEATHER", Name = "Phụ kiện Thắt lưng Da Mill Grain", BrandCode = "ANPHUOC", OrgModelCode = "AP-LG26", IsActive = true, Remark = "Dòng phụ kiện thắt lưng da thủ công cao cấp nguyên miếng" },
                new ProductModel { Code = "MD-NEM-LUX", Name = "Váy đầm dạ hội & Công sở Luxury", BrandCode = "NEM", OrgModelCode = "NEM-LX08", IsActive = true, Remark = "Dòng thời trang dạ tiệc lụa sang trọng thanh lịch" },
                new ProductModel { Code = "MD-VT-SMART", Name = "Sơ mi & Âu phục Smart Casual", BrandCode = "VIETTIEN", OrgModelCode = "VT-SC26", IsActive = true, Remark = "Dòng sản phẩm văn phòng công sở hiện đại, co giãn thoải mái" },
                new ProductModel { Code = "MD-CNF-DAILY", Name = "Trang phục dạo phố Everyday Active", BrandCode = "CANIFA", OrgModelCode = "CNF-EA01", IsActive = true, Remark = "Dòng thời trang gia đình chất liệu len sợi tự nhiên ứng dụng hằng ngày" }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.ProductGroups.AnyAsync())
        {
            db.ProductGroups.AddRange(
                // Nhóm gốc Cấp 1 (Root Categories)
                new ProductGroup { Code = "GRP_THOI_TRANG", Name = "Thời trang may mặc", Description = "Trang phục quần áo may mặc nam nữ, sơ mi, âu phục và váy đầm", ParentCode = null, BrandCode = null, IsActive = true },
                new ProductGroup { Code = "GRP_PHU_KIEN", Name = "Phụ kiện thời trang & Đồ da", Description = "Phụ kiện thời trang như thắt lưng da, ví da, cà vạt và phụ kiện hoàn thiện", ParentCode = null, BrandCode = null, IsActive = true },
                new ProductGroup { Code = "GRP_NVL_DET", Name = "Nguyên phụ liệu may mặc", Description = "Vải dệt thoi, da tấm, sợi dệt tự nhiên và phụ liệu may", ParentCode = null, BrandCode = null, IsActive = true },
                // Phân nhóm con Cấp 2 (Sub-groups)
                new ProductGroup { Code = "GRP_AO_SM", Name = "Áo sơ mi & Áo Polo công sở", Description = "Các dòng sơ mi slimfit, classic và áo polo chất liệu cotton", ParentCode = "GRP_THOI_TRANG", BrandCode = "MAY10", IsActive = true },
                new ProductGroup { Code = "GRP_QUAN_JEAN", Name = "Quần Jeans & Kaki denim", Description = "Dòng quần jeans denim bền chắc và quần kaki dệt thoi", ParentCode = "GRP_THOI_TRANG", BrandCode = "LEVI", IsActive = true },
                new ProductGroup { Code = "GRP_VAY_DAM", Name = "Váy đầm dạ hội & Công sở", Description = "Thời trang thiết kế váy đầm nữ lụa tơ tằm thanh lịch", ParentCode = "GRP_THOI_TRANG", BrandCode = "NEM", IsActive = true },
                new ProductGroup { Code = "GRP_THAT_LUNG", Name = "Thắt lưng da & Ví da thủ công", Description = "Phụ kiện thắt lưng da bò cao cấp và ví da mill grain", ParentCode = "GRP_PHU_KIEN", BrandCode = "ANPHUOC", IsActive = true },
                new ProductGroup { Code = "GRP_VAI_TAM", Name = "Vải dệt cuộn & Vải tấm", Description = "Vải cotton, kaki, denim dạng cây cuộn phục vụ cắt may", ParentCode = "GRP_NVL_DET", BrandCode = null, IsActive = true }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.Areas.AnyAsync())
        {
            db.Areas.AddRange(
                // Vùng gốc Cấp 1 (Root Market Regions)
                new Area { Code = "AREA_MB", Name = "Vùng Miền Bắc", Description = "Vùng thị trường & mạng lưới logistics kho Miền Bắc (Hà Nội, Hải Phòng, Bắc Ninh...)", ParentCode = null, IsActive = true },
                new Area { Code = "AREA_MT", Name = "Vùng Miền Trung & Tây Nguyên", Description = "Vùng thị trường & mạng lưới logistics kho Miền Trung (Đà Nẵng, Quảng Nam, Huế...)", ParentCode = null, IsActive = true },
                new Area { Code = "AREA_MN", Name = "Vùng Miền Nam", Description = "Vùng thị trường & mạng lưới logistics kho Miền Nam (TP.HCM, Cần Thơ, Bình Dương...)", ParentCode = null, IsActive = true },
                // Khu vực nhánh Cấp 2 (Sub-areas / Branch territories)
                new Area { Code = "AREA_HN", Name = "Khu vực Hà Nội & Vùng phụ cận", Description = "Đầu mối phân phối trung tâm thủ đô Hà Nội, Bắc Ninh, Hưng Yên", ParentCode = "AREA_MB", IsActive = true },
                new Area { Code = "AREA_HP", Name = "Khu vực Duyên hải Hải Phòng - Quảng Ninh", Description = "Khu vực cảng biển và chuỗi phân phối duyên hải phía Bắc", ParentCode = "AREA_MB", IsActive = true },
                new Area { Code = "AREA_DN", Name = "Khu vực Đà Nẵng & Trung Trung Bộ", Description = "Trung tâm phân phối logistics và đại lý khu vực Đà Nẵng, Quảng Nam", ParentCode = "AREA_MT", IsActive = true },
                new Area { Code = "AREA_HCM", Name = "Khu vực TP. Hồ Chí Minh & Đông Nam Bộ", Description = "Trung tâm tiêu thụ lớn nhất, phân phối TP.HCM, Bình Dương, Đồng Nai", ParentCode = "AREA_MN", IsActive = true },
                new Area { Code = "AREA_CT", Name = "Khu vực Cần Thơ & Tây Nam Bộ", Description = "Đầu mối giao nhận kho và phân phối khu vực Đồng bằng Sông Cửu Long", ParentCode = "AREA_MN", IsActive = true }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.CustomerGroups.AnyAsync())
        {
            db.CustomerGroups.AddRange(
                // Cấp 1: Kênh / Nhóm gốc (Root Customer Groups)
                new CustomerGroup { Code = "GRP_DAILY", Name = "Hệ thống Đại lý phân phối", Description = "Mạng lưới đại lý ủy quyền và kênh phân phối chính thức, chính sách chiết khấu cấp đại lý", ParentCode = null, IsActive = true },
                new CustomerGroup { Code = "GRP_B2B", Name = "Khách hàng Doanh nghiệp & Công trình", Description = "Khách hàng dự án thầu, công trình, cung ứng số lượng lớn theo hợp đồng dài hạn", ParentCode = null, IsActive = true },
                new CustomerGroup { Code = "GRP_RETAIL", Name = "Khách hàng Tiêu dùng & Bán lẻ", Description = "Kênh bán lẻ trực tiếp tại kho hoặc quầy dịch vụ, giao nhận ngay", ParentCode = null, IsActive = true },
                new CustomerGroup { Code = "GRP_OEM", Name = "Đối tác Sản xuất & Gia công OEM", Description = "Đối tác gia công liên kết thương hiệu, nguyên vật liệu & thành phẩm hoàn thiện", ParentCode = null, IsActive = true },
                // Cấp 2: Nhóm nhánh (Sub-groups)
                new CustomerGroup { Code = "DAILY_CAP1", Name = "Đại lý Cấp 1 — Tổng kho vùng", Description = "Đại lý cấp 1 quy mô lớn, bao tiêu sản lượng theo khu vực miền, hạn mức nợ 45 ngày", ParentCode = "GRP_DAILY", IsActive = true },
                new CustomerGroup { Code = "DAILY_CAP2", Name = "Đại lý Cấp 2 — Cửa hàng ủy quyền", Description = "Đại lý cấp 2, điểm bán lẻ liên kết và chuỗi showroom theo tỉnh/thành", ParentCode = "GRP_DAILY", IsActive = true },
                new CustomerGroup { Code = "B2B_DUAN", Name = "Dự án & Nhà thầu trọng điểm", Description = "Các hợp đồng cung ứng gói thầu lớn, yêu cầu hồ sơ KCS và tiến độ giao hàng", ParentCode = "GRP_B2B", IsActive = true },
                new CustomerGroup { Code = "B2B_SI", Name = "Khách buôn sỉ quy mô lớn", Description = "Đối tác thương mại nhập hàng khối lượng lớn định kỳ hàng tháng", ParentCode = "GRP_B2B", IsActive = true }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.Departments.AnyAsync())
        {
            db.Departments.AddRange(
                // Cấp 1: Khối / Ban điều hành (Root Departments)
                new Department { Code = "KHOI_LOG", Name = "Khối Chuỗi Cung ứng & Logistics", ParentCode = null, BUCode = "BU_SCM", Level = 1, MST = "0101234567-001", Description = "Quản lý hệ thống tổng kho, kho vùng trung chuyển, điều độ logistics và cấp phát vật tư", IsActive = true },
                new Department { Code = "KHOI_SX", Name = "Khối Quản trị Sản xuất & Chế tạo", ParentCode = null, BUCode = "BU_MFG", Level = 1, MST = "0101234567-002", Description = "Quản trị toàn bộ chuỗi chế tạo, phân xưởng gia công, lắp ráp và kiểm soát chất lượng KCS", IsActive = true },
                new Department { Code = "KHOI_KD", Name = "Khối Kinh doanh & Tiếp thị", ParentCode = null, BUCode = "BU_SALES", Level = 1, MST = "0101234567-003", Description = "Quản lý mạng lưới bán hàng B2B, hệ thống đại lý phân phối và bảo hành sau bán hàng", IsActive = true },
                new Department { Code = "KHOI_TAICHINH", Name = "Khối Tài chính & Kế toán Doanh nghiệp", ParentCode = null, BUCode = "BU_FIN", Level = 1, MST = "0101234567-004", Description = "Kiểm soát hạch toán tài sản kho, giá vốn bình quân, đối soát tồn kho và chốt kỳ sổ sách", IsActive = true },

                // Cấp 2: Phòng ban & Phân xưởng trực thuộc (Sub-departments & Workshops)
                new Department { Code = "PB_QLKHO", Name = "Phòng Quản lý Kho vận & Vật tư", ParentCode = "KHOI_LOG", BUCode = "BU_SCM", Level = 2, MST = "0101234567-001", Description = "Tổ chức vận hành, bảo quản, đóng gói carton/hộp và kiểm kê định kỳ kho hàng", IsActive = true },
                new Department { Code = "PX_LAPRAP", Name = "Phân xưởng Lắp ráp & Hoàn thiện", ParentCode = "KHOI_SX", BUCode = "BU_MFG", Level = 2, MST = "0101234567-002", Description = "Tiếp nhận linh kiện NVL, hoàn thiện thành phẩm và đóng số Serial/IMEI", IsActive = true },
                new Department { Code = "PX_GIACONG", Name = "Phân xưởng Chế tạo & Cơ khí chính xác", ParentCode = "KHOI_SX", BUCode = "BU_MFG", Level = 2, MST = "0101234567-002", Description = "Gia công thô chi tiết, đúc khuôn, cắt gọt và xuất dùng vật tư nguyên liệu", IsActive = true },
                new Department { Code = "PB_KCS", Name = "Phòng Kiểm soát Chất lượng (QA/QC - KCS)", ParentCode = "KHOI_SX", BUCode = "BU_MFG", Level = 2, MST = "0101234567-002", Description = "Giám định chất lượng hàng hóa nhập mua, nghiệm thu thành phẩm sản xuất trước nhập kho", IsActive = true },
                new Department { Code = "PB_DIEUDO", Name = "Phòng Kế hoạch & Điều độ Sản xuất", ParentCode = "KHOI_SX", BUCode = "BU_MFG", Level = 2, MST = "0101234567-002", Description = "Lập kế hoạch nhu cầu vật tư (MRP), cấp phát xuất dùng và tiến độ đơn hàng", IsActive = true },
                new Department { Code = "PB_BANHANG", Name = "Phòng Kinh doanh & Kênh Đại lý", ParentCode = "KHOI_KD", BUCode = "BU_SALES", Level = 2, MST = "0101234567-003", Description = "Tiếp nhận đơn đặt hàng, điều phối lệnh xuất kho giao cho đại lý và dự án", IsActive = true },
                new Department { Code = "PB_BAOHANH", Name = "Trung tâm Kỹ thuật & Bảo hành", ParentCode = "KHOI_KD", BUCode = "BU_SALES", Level = 2, MST = "0101234567-003", Description = "Xử lý hàng khách trả, đổi trả linh kiện bảo hành và thẩm định sản phẩm NG", IsActive = true },
                new Department { Code = "PB_KETOANKHO", Name = "Bộ phận Kế toán Kho & Thẻ kho", ParentCode = "KHOI_TAICHINH", BUCode = "BU_FIN", Level = 2, MST = "0101234567-004", Description = "Hạch toán nghiệp vụ nhập-xuất-tồn, tính giá vốn xuất kho và kiểm soát hao hụt", IsActive = true }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.CustomerSources.AnyAsync())
        {
            db.CustomerSources.AddRange(
                // Cấp 1: Kênh / Nguồn gốc (Root Channels)
                new CustomerSource { Code = "SRC_DIRECT", Name = "Kênh Trực tiếp tại Tổng kho", ParentCode = null, BUCode = "BU_SCM", Description = "Bán lẻ và giao nhận trực tiếp tại quầy tiếp nhận tổng kho hoặc chi nhánh", IsActive = true },
                new CustomerSource { Code = "SRC_DEALER", Name = "Kênh Mạng lưới Đại lý & NPP", ParentCode = null, BUCode = "BU_SALES", Description = "Hệ thống nhà phân phối, đại lý ủy quyền cấp 1 & cấp 2 trên toàn quốc", IsActive = true },
                new CustomerSource { Code = "SRC_ECOMMERCE", Name = "Kênh Sàn Thương mại Điện tử", ParentCode = null, BUCode = "BU_SALES", Description = "Kênh bán lẻ qua sàn TMĐT (Shopee, Lazada, TikTok Shop, Tiki) xuất kho theo kiện", IsActive = true },
                new CustomerSource { Code = "SRC_PROJECT", Name = "Kênh Dự án & Đấu thầu B2B", ParentCode = null, BUCode = "BU_SALES", Description = "Hợp đồng dự án thầu, cung ứng vật tư, đồng phục doanh nghiệp lớn và cơ quan nhà nước", IsActive = true },
                new CustomerSource { Code = "SRC_SHOWROOM", Name = "Kênh Chuỗi Showroom & Cửa hàng", ParentCode = null, BUCode = "BU_SALES", Description = "Mạng lưới cửa hàng thời trang bán lẻ, showroom trưng bày và giới thiệu sản phẩm", IsActive = true },
                new CustomerSource { Code = "SRC_EXPORT", Name = "Kênh Xuất khẩu & Quốc tế", ParentCode = null, BUCode = "BU_SCM", Description = "Ủy thác xuất khẩu và đối tác thương mại thị trường quốc tế, quy chuẩn đóng thùng WMS", IsActive = true },
                new CustomerSource { Code = "SRC_ONLINE", Name = "Kênh Hotline & Đặt hàng Online", ParentCode = null, BUCode = "BU_SALES", Description = "Đơn đặt hàng từ website thương mại, tổng đài hotline tư vấn và bán buôn từ xa", IsActive = true },
                new CustomerSource { Code = "SRC_OEM", Name = "Kênh Đối tác Sản xuất & OEM", ParentCode = null, BUCode = "BU_MFG", Description = "Khách hàng liên kết hợp tác sản xuất nhượng quyền, cung ứng NVL và gia công thành phẩm", IsActive = true },

                // Cấp 2: Kênh nhánh (Sub-channels)
                new CustomerSource { Code = "SRC_DEALER_MB", Name = "Đại lý Kênh Miền Bắc", ParentCode = "SRC_DEALER", BUCode = "BU_SALES", Description = "Mạng lưới đại lý và kho trung chuyển khu vực các tỉnh phía Bắc", IsActive = true },
                new CustomerSource { Code = "SRC_DEALER_MN", Name = "Đại lý Kênh Miền Nam", ParentCode = "SRC_DEALER", BUCode = "BU_SALES", Description = "Mạng lưới đại lý và NPP các tỉnh Đông Nam Bộ & Tây Nam Bộ", IsActive = true },
                new CustomerSource { Code = "SRC_ECOM_TIKI", Name = "Gian hàng TMĐT Tiki & Lazada", ParentCode = "SRC_ECOMMERCE", BUCode = "BU_SALES", Description = "Gian hàng chính hãng LazMall & TikiNow kho phân phối nhanh", IsActive = true },
                new CustomerSource { Code = "SRC_ECOM_SHOPEE", Name = "Gian hàng TMĐT Shopee & TikTok", ParentCode = "SRC_ECOMMERCE", BUCode = "BU_SALES", Description = "Kênh livestream và đơn hàng Shopee Mall giao hỏa tốc", IsActive = true }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.GovTaxOffices.AnyAsync())
        {
            db.GovTaxOffices.AddRange(
                // Cấp 0: Cục Thuế (Root Tax Departments)
                new GovTaxOffice { Code = "0100231226", Name = "Cục Thuế TP Hà Nội", ParentCode = null, BUCode = "0100231226", BUPattern = "0100231226%", Level = 0, ProvinceCode = "01", DistrictCode = null, Address = "Số 187 Giảng Võ, Q. Đống Đa, TP Hà Nội", ContactEmail = "cucthue.hanoi@gdt.gov.vn", ContactPhone = "024.3851.5000", IsActive = true },
                new GovTaxOffice { Code = "0301234567", Name = "Cục Thuế TP Hồ Chí Minh", ParentCode = null, BUCode = "0301234567", BUPattern = "0301234567%", Level = 0, ProvinceCode = "79", DistrictCode = null, Address = "Số 138 Nguyễn Thị Minh Khai, Q.3, TP Hồ Chí Minh", ContactEmail = "cucthue.hcm@gdt.gov.vn", ContactPhone = "028.3930.5000", IsActive = true },
                // Cấp 1: Chi cục Thuế (Sub Tax Offices)
                new GovTaxOffice { Code = "0100231226-001", Name = "Chi cục Thuế Quận Hoàn Kiếm", ParentCode = "0100231226", BUCode = "0100231226.0100231226-001", BUPattern = "0100231226.0100231226-001%", Level = 1, ProvinceCode = "01", DistrictCode = "001", Address = "Số 8 Lê Thái Tổ, Q. Hoàn Kiếm, TP Hà Nội", ContactEmail = "cct.hoankiem@gdt.gov.vn", ContactPhone = "024.3825.3000", IsActive = true },
                new GovTaxOffice { Code = "0100231226-002", Name = "Chi cục Thuế Quận Đống Đa", ParentCode = "0100231226", BUCode = "0100231226.0100231226-002", BUPattern = "0100231226.0100231226-002%", Level = 1, ProvinceCode = "01", DistrictCode = "006", Address = "Số 187 Giảng Võ, Q. Đống Đa, TP Hà Nội", ContactEmail = "cct.dongda@gdt.gov.vn", ContactPhone = "024.3851.5001", IsActive = true },
                new GovTaxOffice { Code = "0301234567-001", Name = "Chi cục Thuế Quận 1", ParentCode = "0301234567", BUCode = "0301234567.0301234567-001", BUPattern = "0301234567.0301234567-001%", Level = 1, ProvinceCode = "79", DistrictCode = "760", Address = "Số 138 Nguyễn Thị Minh Khai, Q.1, TP Hồ Chí Minh", ContactEmail = "cct.quan1@gdt.gov.vn", ContactPhone = "028.3930.5001", IsActive = true },
                // Cấp 2: Đội Thuế (Tax Teams)
                new GovTaxOffice { Code = "0100231226-001-01", Name = "Đội Thuế số 1 - Q. Hoàn Kiếm", ParentCode = "0100231226-001", BUCode = "0100231226.0100231226-001.0100231226-001-01", BUPattern = "0100231226.0100231226-001.0100231226-001-01%", Level = 2, ProvinceCode = "01", DistrictCode = "001", Address = "Số 8 Lê Thái Tổ, Q. Hoàn Kiếm, TP Hà Nội", ContactEmail = "doi1.hoankiem@gdt.gov.vn", ContactPhone = "024.3825.3001", IsActive = true },
                new GovTaxOffice { Code = "0100231226-002-01", Name = "Đội Thuế số 1 - Q. Đống Đa", ParentCode = "0100231226-002", BUCode = "0100231226.0100231226-002.0100231226-002-01", BUPattern = "0100231226.0100231226-002.0100231226-002-01%", Level = 2, ProvinceCode = "01", DistrictCode = "006", Address = "Số 187 Giảng Võ, Q. Đống Đa, TP Hà Nội", ContactEmail = "doi1.dongda@gdt.gov.vn", ContactPhone = "024.3851.5002", IsActive = true },
                new GovTaxOffice { Code = "0301234567-001-01", Name = "Đội Thuế số 1 - Q.1 (ngừng dùng)", ParentCode = "0301234567-001", BUCode = "0301234567.0301234567-001.0301234567-001-01", BUPattern = "0301234567.0301234567-001.0301234567-001-01%", Level = 2, ProvinceCode = "79", DistrictCode = "760", Address = "Số 138 Nguyễn Thị Minh Khai, Q.1, TP Hồ Chí Minh", ContactEmail = "doi1.quan1@gdt.gov.vn", ContactPhone = "028.3930.5002", IsActive = false }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                new Product { Code = "AO-001", Name = "Áo sơ mi trắng", PartTypeCode = "TP", BrandCode = "MAY10", ModelCode = "MD-M10-SLIM", PMType = "COTTON", ProductGrpCode = "GRP_AO_SM", Uom = "cái", MinStock = 20, MaxStock = 200, CostPrice = 150000m },
                new Product { Code = "QUAN-001", Name = "Quần jeans slim", PartTypeCode = "TP", BrandCode = "LEVI", ModelCode = "MD-LV-501", PMType = "DENIM", ProductGrpCode = "GRP_QUAN_JEAN", Uom = "cái", MinStock = 15, MaxStock = 150, CostPrice = 280000m },
                new Product { Code = "PK-001", Name = "Thắt lưng da", PartTypeCode = "PTLK", BrandCode = "ANPHUOC", ModelCode = "MD-AP-LEATHER", PMType = "LEATHER", ProductGrpCode = "GRP_THAT_LUNG", Uom = "cái", MinStock = 10, MaxStock = 80, CostPrice = 120000m },
                new Product { Code = "VAY-001", Name = "Váy đầm công sở", PartTypeCode = "TP", BrandCode = "NEM", ModelCode = "MD-NEM-LUX", PMType = "SILK", ProductGrpCode = "GRP_VAY_DAM", Uom = "cái", MinStock = 12, MaxStock = 100, CostPrice = 320000m });
            await db.SaveChangesAsync();
        }
        if (!await db.PartColorMaps.AnyAsync())
        {
            var seedProds = await db.Products.ToListAsync();
            int Pid(string code) => seedProds.FirstOrDefault(p => p.Code == code)?.Id ?? 0;
            var colorMaps = new List<PartColorMap>();
            if (Pid("AO-001") > 0)
            {
                colorMaps.Add(new PartColorMap { ProductId = Pid("AO-001"), PartColorCode = "TRANG", IsDefault = true, IsActive = true });
                colorMaps.Add(new PartColorMap { ProductId = Pid("AO-001"), PartColorCode = "XANH-NAVY", IsDefault = false, IsActive = true });
            }
            if (Pid("QUAN-001") > 0)
            {
                colorMaps.Add(new PartColorMap { ProductId = Pid("QUAN-001"), PartColorCode = "XANH-DUONG", IsDefault = true, IsActive = true });
                colorMaps.Add(new PartColorMap { ProductId = Pid("QUAN-001"), PartColorCode = "DEN", IsDefault = false, IsActive = true });
            }
            if (Pid("PK-001") > 0)
            {
                colorMaps.Add(new PartColorMap { ProductId = Pid("PK-001"), PartColorCode = "NAU-DA", IsDefault = true, IsActive = true });
                colorMaps.Add(new PartColorMap { ProductId = Pid("PK-001"), PartColorCode = "DEN", IsDefault = false, IsActive = true });
            }
            if (Pid("VAY-001") > 0)
            {
                colorMaps.Add(new PartColorMap { ProductId = Pid("VAY-001"), PartColorCode = "DO-DAM", IsDefault = true, IsActive = true });
                colorMaps.Add(new PartColorMap { ProductId = Pid("VAY-001"), PartColorCode = "DEN", IsDefault = false, IsActive = true });
            }
            if (colorMaps.Count > 0)
            {
                db.PartColorMaps.AddRange(colorMaps);
                await db.SaveChangesAsync();
            }
        }
        if (!await db.SecretLicenses.AnyAsync())
        {
            db.SecretLicenses.Add(new SecretLicense
            {
                Mst = "0101234567",
                TotalQty = 5000,
                TotalQtyIssued = 0,
                TotalQtyUsed = 0,
                IsActive = true,
                Remark = "Hạn mức số in tem mua từ cơ quan thuế (đơn vị demo)"
            });
            await db.SaveChangesAsync();
        }
        if (!await db.InventorySecrets.AnyAsync())
        {
            var license = await db.SecretLicenses.FirstOrDefaultAsync();
            var genTimesNo = $"GEN-{DateTime.Now:yyyyMMdd}-001";
            var secrets = new List<InventorySecret>();
            for (int i = 1; i <= 20; i++)
            {
                var serialNo = $"{genTimesNo}-{i:D6}";
                secrets.Add(new InventorySecret
                {
                    SerialNo = serialNo,
                    QrSerialNo = serialNo,
                    GenTimesNo = genTimesNo,
                    FlagMap = i <= 5,
                    FlagUsed = i <= 3,
                    LogLUBy = "seed",
                    Remark = i <= 3 ? "Đã in tem & xuất kho" : (i <= 5 ? "Đã gán vào kiện hàng" : "Sẵn sàng cấp phát"),
                    CreatedAt = DateTime.Now.AddDays(-7)
                });
            }
            db.InventorySecrets.AddRange(secrets);
            if (license != null)
            {
                license.TotalQtyIssued = secrets.Count;
                license.TotalQtyUsed = secrets.Count(s => s.FlagUsed);
                license.UpdatedAt = DateTime.Now;
            }
            await db.SaveChangesAsync();
        }
        if (!await db.Provinces.AnyAsync())
        {
            db.Provinces.AddRange(
                new Province { Code = "HN", Name = "Thành phố Hà Nội", IsActive = true },
                new Province { Code = "HCM", Name = "Thành phố Hồ Chí Minh", IsActive = true },
                new Province { Code = "DN", Name = "Thành phố Đà Nẵng", IsActive = true },
                new Province { Code = "HP", Name = "Thành phố Hải Phòng", IsActive = true },
                new Province { Code = "CT", Name = "Thành phố Cần Thơ", IsActive = true }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.Districts.AnyAsync())
        {
            db.Districts.AddRange(
                new District { Code = "HBT", ProvinceCode = "HN", Name = "Quận Hai Bà Trưng", IsActive = true },
                new District { Code = "CG", ProvinceCode = "HN", Name = "Quận Cầu Giấy", IsActive = true },
                new District { Code = "Q1", ProvinceCode = "HCM", Name = "Quận 1", IsActive = true },
                new District { Code = "TB", ProvinceCode = "HCM", Name = "Quận Tân Bình", IsActive = true },
                new District { Code = "HC", ProvinceCode = "DN", Name = "Quận Hải Châu", IsActive = true },
                new District { Code = "LC", ProvinceCode = "HP", Name = "Quận Lê Chân", IsActive = true },
                new District { Code = "NK", ProvinceCode = "CT", Name = "Quận Ninh Kiều", IsActive = true }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.Agents.AnyAsync())
        {
            db.Agents.AddRange(
                new Agent { Code = "AG-HN-001", Name = "Đại lý Hà Nội - Hai Bà Trưng", ProvinceCode = "HN", DistrictCode = "HBT", Address = "Số 12 Bà Triệu, P. Nguyễn Du", IsActive = true, Remark = "Đại lý cấp 1 khu vực nội thành Hà Nội" },
                new Agent { Code = "AG-HN-002", Name = "Đại lý Hà Nội - Cầu Giấy", ProvinceCode = "HN", DistrictCode = "CG", Address = "Số 45 Xuân Thủy, P. Dịch Vọng Hậu", IsActive = true, Remark = "Đại lý phân phối khu vực phía Tây Hà Nội" },
                new Agent { Code = "AG-HCM-001", Name = "Đại lý Sài Gòn - Quận 1", ProvinceCode = "HCM", DistrictCode = "Q1", Address = "Số 88 Lê Lợi, P. Bến Thành", IsActive = true, Remark = "Đại lý trung tâm thương mại Quận 1" },
                new Agent { Code = "AG-HCM-002", Name = "Đại lý Sài Gòn - Tân Bình", ProvinceCode = "HCM", DistrictCode = "TB", Address = "Số 210 Cộng Hòa, P. 12", IsActive = true, Remark = "Đại lý khu vực sân bay Tân Sơn Nhất" },
                new Agent { Code = "AG-DN-001", Name = "Đại lý Đà Nẵng - Hải Châu", ProvinceCode = "DN", DistrictCode = "HC", Address = "Số 30 Bạch Đằng, P. Thạch Thang", IsActive = true, Remark = "Đại lý miền Trung" },
                new Agent { Code = "AG-HP-001", Name = "Đại lý Hải Phòng - Lê Chân", ProvinceCode = "HP", DistrictCode = "LC", Address = "Số 15 Tô Hiệu, P. Trại Cau", IsActive = false, Remark = "Tạm dừng hoạt động để tái cơ cấu" }
            );
            await db.SaveChangesAsync();
        }
        else
        {
            // Cập nhật giá vốn, loại mặt hàng và thương hiệu cho dữ liệu cũ nếu chưa có
            var existingProds = await db.Products.ToListAsync();
            bool hasChanged = false;
            foreach (var p in existingProds)
            {
                if (p.CostPrice == 0)
                {
                    p.CostPrice = p.Code switch
                    {
                        "AO-001" => 150000m,
                        "QUAN-001" => 280000m,
                        "PK-001" => 120000m,
                        "VAY-001" => 320000m,
                        _ => 100000m
                    };
                    hasChanged = true;
                }
                if (string.IsNullOrWhiteSpace(p.PartTypeCode))
                {
                    p.PartTypeCode = p.Code switch
                    {
                        "AO-001" => "TP",
                        "QUAN-001" => "TP",
                        "PK-001" => "PTLK",
                        "VAY-001" => "TP",
                        _ => "TP"
                    };
                    hasChanged = true;
                }
                if (string.IsNullOrWhiteSpace(p.BrandCode))
                {
                    p.BrandCode = p.Code switch
                    {
                        "AO-001" => "MAY10",
                        "QUAN-001" => "LEVI",
                        "PK-001" => "ANPHUOC",
                        "VAY-001" => "NEM",
                        _ => null
                    };
                    hasChanged = true;
                }
                if (string.IsNullOrWhiteSpace(p.PMType))
                {
                    p.PMType = p.Code switch
                    {
                        "AO-001" => "COTTON",
                        "QUAN-001" => "DENIM",
                        "PK-001" => "LEATHER",
                        "VAY-001" => "SILK",
                        _ => "COTTON"
                    };
                    hasChanged = true;
                }
                if (string.IsNullOrWhiteSpace(p.ModelCode))
                {
                    p.ModelCode = p.Code switch
                    {
                        "AO-001" => "MD-M10-SLIM",
                        "QUAN-001" => "MD-LV-501",
                        "PK-001" => "MD-AP-LEATHER",
                        "VAY-001" => "MD-NEM-LUX",
                        _ => null
                    };
                    hasChanged = true;
                }
                if (string.IsNullOrWhiteSpace(p.ProductGrpCode))
                {
                    p.ProductGrpCode = p.Code switch
                    {
                        "AO-001" => "GRP_AO_SM",
                        "QUAN-001" => "GRP_QUAN_JEAN",
                        "PK-001" => "GRP_THAT_LUNG",
                        "VAY-001" => "GRP_VAY_DAM",
                        _ => null
                    };
                    hasChanged = true;
                }
            }
            if (hasChanged) await db.SaveChangesAsync();
        }

        if (!await db.Suppliers.AnyAsync())
        {
            db.Suppliers.AddRange(
                new Supplier
                {
                    Code = "NCC-MAY10",
                    Name = "Tổng Công ty May 10 - CTCP",
                    ContactName = "Nguyễn Văn Hưng",
                    Phone = "024-38276923",
                    Email = "kinhdoanh@may10.vn",
                    Address = "765 Nguyễn Văn Linh, Sài Đồng, Long Biên, Hà Nội",
                    TaxCode = "0100100101",
                    IsActive = true,
                    Note = "Nhà cung cấp áo sơ mi và thời trang công sở cao cấp",
                    CreatedAt = DateTime.Now.AddDays(-120)
                },
                new Supplier
                {
                    Code = "NCC-DAHN",
                    Name = "Xưởng Sản Xuất Da Thật Hà Nội",
                    ContactName = "Trần Thị Lan",
                    Phone = "0912-345-678",
                    Email = "dathathanoi@gmail.com",
                    Address = "Làng nghề đồ da Phú Xuyên, Hà Nội",
                    TaxCode = "0108928371",
                    IsActive = true,
                    Note = "Cung cấp phụ kiện thắt lưng, ví da, đồ da bò thật",
                    CreatedAt = DateTime.Now.AddDays(-120)
                },
                new Supplier
                {
                    Code = "NCC-PHONGPHU",
                    Name = "Tổng Công ty CP Dệt May Phong Phú",
                    ContactName = "Lê Hoàng Nam",
                    Phone = "028-38963533",
                    Email = "sales@phongphucorp.com",
                    Address = "48 Tăng Nhơn Phú, P. Tăng Nhơn Phú B, TP. Thủ Đức, TP.HCM",
                    TaxCode = "0301445722",
                    IsActive = true,
                    Note = "Cung ứng vải jeans, đầm thời trang và trang phục dệt may",
                    CreatedAt = DateTime.Now.AddDays(-120)
                },
                new Supplier
                {
                    Code = "NCC-VIETTIEN",
                    Name = "Tổng Công ty CP May Việt Tiến",
                    ContactName = "Phạm Quang Minh",
                    Phone = "028-38640800",
                    Email = "viettien@viettien.com.vn",
                    Address = "07 Lê Minh Xuân, Phường 7, Tân Bình, TP.HCM",
                    TaxCode = "0300401524",
                    IsActive = true,
                    Note = "Đơn vị cung ứng váy đầm, âu phục và trang phục cao cấp",
                    CreatedAt = DateTime.Now.AddDays(-120)
                },
                new Supplier
                {
                    Code = "NCC-BAOBI",
                    Name = "Công ty CP Bao bì & Phụ liệu Toàn Cầu",
                    ContactName = "Hoàng Tuấn Anh",
                    Phone = "0222-3899123",
                    Email = "contact@toancaupack.vn",
                    Address = "KCN Quế Võ, Bắc Ninh",
                    TaxCode = "2300987654",
                    IsActive = true,
                    Note = "Cung cấp thùng carton, hộp đóng gói, tem nhãn WMS",
                    CreatedAt = DateTime.Now.AddDays(-120)
                }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Customers.AnyAsync())
        {
            db.Customers.AddRange(
                new Customer
                {
                    Code = "KH-FPT",
                    Name = "Công ty Cổ phần Bán lẻ Kỹ thuật số FPT",
                    CustomerType = "Đại lý phân phối",
                    ContactName = "Nguyễn Tuấn Anh",
                    ContactPhone = "0982-111-222",
                    Phone = "024-73006666",
                    Email = "khohang@fptshop.com.vn",
                    Address = "261 Khánh Hội, Phường 2, Quận 4, TP.HCM",
                    Province = "TP.HCM",
                    AreaCode = "AREA_HCM",
                    CustomerGrpCode = "DAILY_CAP1",
                    CustomerSourceCode = "SRC_DEALER_MN",
                    TaxCode = "0105777650",
                    IsActive = true,
                    Note = "Hệ thống đại lý phân phối thiết bị & thời trang cao cấp",
                    CreatedAt = DateTime.Now.AddDays(-120)
                },
                new Customer
                {
                    Code = "KH-MWG",
                    Name = "Công ty Cổ phần Thế Giới Di Động",
                    CustomerType = "Đại lý phân phối",
                    ContactName = "Trần Minh Trí",
                    ContactPhone = "0903-555-888",
                    Phone = "028-38125960",
                    Email = "giaonhan@thegioididong.com",
                    Address = "Lô T2-1.2, Đường D1, Khu Công nghệ cao, TP. Thủ Đức, TP.HCM",
                    Province = "TP.HCM",
                    AreaCode = "AREA_HCM",
                    CustomerGrpCode = "DAILY_CAP1",
                    CustomerSourceCode = "SRC_DEALER_MN",
                    TaxCode = "0303217354",
                    IsActive = true,
                    Note = "Chuỗi siêu thị phân phối bán lẻ quy mô toàn quốc",
                    CreatedAt = DateTime.Now.AddDays(-120)
                },
                new Customer
                {
                    Code = "KH-VNPT",
                    Name = "Tổng Công ty Dịch vụ Viễn thông VNPT",
                    CustomerType = "Dự án / Công trình",
                    ContactName = "Lê Văn Đức",
                    ContactPhone = "0915-888-999",
                    Phone = "024-37735555",
                    Email = "vattu@vnpt.vn",
                    Address = "57 Huỳnh Thúc Kháng, Đống Đa, Hà Nội",
                    Province = "Hà Nội",
                    AreaCode = "AREA_HN",
                    CustomerGrpCode = "B2B_DUAN",
                    CustomerSourceCode = "SRC_PROJECT",
                    TaxCode = "0106869738",
                    IsActive = true,
                    Note = "Hợp đồng dự án cấp phát đồng phục & vật tư định kỳ",
                    CreatedAt = DateTime.Now.AddDays(-120)
                },
                new Customer
                {
                    Code = "KH-BACHHOA",
                    Name = "Chuỗi Cửa hàng Bách Hóa Miền Bắc",
                    CustomerType = "Bán buôn B2B",
                    ContactName = "Hoàng Mai Hương",
                    ContactPhone = "0934-222-333",
                    Phone = "024-39876543",
                    Email = "kinhdoanh@bachhoamienbac.vn",
                    Address = "18 Tam Trinh, Hoàng Mai, Hà Nội",
                    Province = "Hà Nội",
                    AreaCode = "AREA_HN",
                    CustomerGrpCode = "DAILY_CAP2",
                    CustomerSourceCode = "SRC_DEALER_MB",
                    TaxCode = "0107896541",
                    IsActive = true,
                    Note = "Đối tác lấy buôn phân phối cho mạng lưới cửa hàng bán buôn",
                    CreatedAt = DateTime.Now.AddDays(-120)
                },
                new Customer
                {
                    Code = "KH-ANPHU",
                    Name = "Công ty TNHH Thương mại Dịch vụ An Phú",
                    CustomerType = "Đại lý phân phối",
                    ContactName = "Đỗ Hải Đăng",
                    ContactPhone = "0977-666-777",
                    Phone = "0236-3688999",
                    Email = "anphucorp@gmail.com",
                    Address = "220 Nguyễn Văn Linh, Q. Hải Châu, TP. Đà Nẵng",
                    Province = "Đà Nẵng",
                    AreaCode = "AREA_DN",
                    CustomerGrpCode = "DAILY_CAP2",
                    CustomerSourceCode = "SRC_SHOWROOM",
                    TaxCode = "0401889922",
                    IsActive = true,
                    Note = "Đại lý ủy quyền độc quyền khu vực Miền Trung",
                    CreatedAt = DateTime.Now.AddDays(-120)
                },
                new Customer
                {
                    Code = "KH-RETAIL",
                    Name = "Khách hàng mua lẻ tại kho",
                    CustomerType = "Khách lẻ",
                    ContactName = "Khách lẻ trực tiếp",
                    ContactPhone = "0909-000-000",
                    Phone = "0909-000-000",
                    Email = "khachle@miniwms.vn",
                    Address = "Tại quầy nhận hàng kho trung tâm",
                    Province = "Hà Nội",
                    AreaCode = "AREA_HN",
                    CustomerGrpCode = "GRP_RETAIL",
                    CustomerSourceCode = "SRC_DIRECT",
                    TaxCode = "",
                    IsActive = true,
                    Note = "Khách mua lẻ trực tiếp thanh toán ngay",
                    CreatedAt = DateTime.Now.AddDays(-120)
                }
            );
            await db.SaveChangesAsync();
        }
        else
        {
            var existingCusts = await db.Customers.ToListAsync();
            bool custChanged = false;
            foreach (var c in existingCusts)
            {
                if (string.IsNullOrWhiteSpace(c.AreaCode))
                {
                    c.AreaCode = c.Code switch
                    {
                        "KH-FPT" => "AREA_HCM",
                        "KH-MWG" => "AREA_HCM",
                        "KH-VNPT" => "AREA_HN",
                        "KH-BACHHOA" => "AREA_HN",
                        "KH-ANPHU" => "AREA_DN",
                        _ => "AREA_HN"
                    };
                    custChanged = true;
                }
                if (string.IsNullOrWhiteSpace(c.CustomerGrpCode))
                {
                    c.CustomerGrpCode = c.Code switch
                    {
                        "KH-FPT" => "DAILY_CAP1",
                        "KH-MWG" => "DAILY_CAP1",
                        "KH-VNPT" => "B2B_DUAN",
                        "KH-BACHHOA" => "DAILY_CAP2",
                        "KH-ANPHU" => "DAILY_CAP2",
                        "KH-RETAIL" => "GRP_RETAIL",
                        _ => "GRP_RETAIL"
                    };
                    custChanged = true;
                }
                if (string.IsNullOrWhiteSpace(c.CustomerSourceCode))
                {
                    c.CustomerSourceCode = c.Code switch
                    {
                        "KH-FPT" => "SRC_DEALER_MN",
                        "KH-MWG" => "SRC_DEALER_MN",
                        "KH-VNPT" => "SRC_PROJECT",
                        "KH-BACHHOA" => "SRC_DEALER_MB",
                        "KH-ANPHU" => "SRC_SHOWROOM",
                        "KH-RETAIL" => "SRC_DIRECT",
                        _ => "SRC_DIRECT"
                    };
                    custChanged = true;
                }
            }
            if (custChanged) await db.SaveChangesAsync();
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
                    SupplierCode = "NCC-VIETTIEN",
                    SupplierName = "Tổng Công ty CP May Việt Tiến",
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

        if (!await db.Docs.AnyAsync(d => d.Code == "PNSEED-007"))
        {
            var whs = await db.Warehouses.ToListAsync();
            var hn = whs.FirstOrDefault(w => w.Code == "KHO-HN")?.Id;
            var prods = await db.Products.ToListAsync();
            var pk = prods.FirstOrDefault(p => p.Code == "PK-001")?.Id;

            if (hn.HasValue && pk.HasValue)
            {
                var pn7 = new StockDoc
                {
                    Type = DocType.In,
                    ToWarehouseId = hn.Value,
                    Code = "PNSEED-007",
                    SupplierCode = "NCC-DAHN",
                    SupplierName = "Xưởng Sản Xuất Da Thật Hà Nội",
                    Status = DocStatus.Posted,
                    Date = DateTime.Now.AddDays(-5),
                    Note = "Nhập lô thắt lưng da cao cấp từ Xưởng Da Thật Hà Nội",
                    CreatedBy = "seed"
                };
                pn7.Lines.Add(new StockDocLine { ProductId = pk.Value, Quantity = 50 });
                db.Docs.Add(pn7);
                await db.SaveChangesAsync();
            }
        }

        if (!await db.Docs.AnyAsync(d => d.Code == "PNSEED-008"))
        {
            var whs = await db.Warehouses.ToListAsync();
            var hn = whs.FirstOrDefault(w => w.Code == "KHO-HN")?.Id;
            var prods = await db.Products.ToListAsync();
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001")?.Id;
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001")?.Id;

            if (hn.HasValue && ao.HasValue && quan.HasValue)
            {
                var pn8 = new StockDoc
                {
                    Type = DocType.In,
                    ToWarehouseId = hn.Value,
                    Code = "PNSEED-008",
                    SupplierCode = "NCC-MAY10",
                    SupplierName = "Tổng Công ty May 10 - CTCP",
                    RefNo = "PO-2026-0388",
                    Status = DocStatus.Posted,
                    Date = DateTime.Now.AddDays(-1),
                    Note = "Nhập bổ sung đơn hàng thời trang hè 2026",
                    CreatedBy = "thu_kho_hn"
                };
                pn8.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 40 });
                pn8.Lines.Add(new StockDocLine { ProductId = quan.Value, Quantity = 25 });
                db.Docs.Add(pn8);
                await db.SaveChangesAsync();
            }
        }

        // Cập nhật thông tin NCC cho các phiếu nhập mẫu cũ nếu chưa có
        var existingDocsToUpdate = await db.Docs.Where(d => d.Type == DocType.In && string.IsNullOrEmpty(d.SupplierCode)).ToListAsync();
        if (existingDocsToUpdate.Any())
        {
            foreach (var d in existingDocsToUpdate)
            {
                switch (d.Code)
                {
                    case "PNSEED-001":
                    case "PNSEED-004":
                        d.SupplierCode = "NCC-MAY10";
                        d.SupplierName = "Tổng Công ty May 10 - CTCP";
                        break;
                    case "PNSEED-005":
                        d.SupplierCode = "NCC-PHONGPHU";
                        d.SupplierName = "Tổng Công ty CP Dệt May Phong Phú";
                        break;
                    case "PNSEED-006":
                        d.SupplierCode = "NCC-VIETTIEN";
                        d.SupplierName = "Tổng Công ty CP May Việt Tiến";
                        break;
                    default:
                        d.SupplierCode = "NCC-MAY10";
                        d.SupplierName = "Tổng Công ty May 10 - CTCP";
                        break;
                }
            }
            await db.SaveChangesAsync();
        }

        // Cập nhật thông tin Khách hàng cho các phiếu xuất mẫu cũ nếu chưa có
        var existingOutDocsToUpdate = await db.Docs.Where(d => d.Type == DocType.Out && string.IsNullOrEmpty(d.CustomerCode)).ToListAsync();
        if (existingOutDocsToUpdate.Any())
        {
            foreach (var d in existingOutDocsToUpdate)
            {
                switch (d.Code)
                {
                    case "PXSEED-001":
                        d.CustomerCode = "KH-FPT";
                        d.CustomerName = "Công ty Cổ phần Bán lẻ Kỹ thuật số FPT";
                        break;
                    case "PXSEED-002":
                        d.CustomerCode = "KH-MWG";
                        d.CustomerName = "Công ty Cổ phần Thế Giới Di Động";
                        break;
                    case "PXSEED-003":
                        d.CustomerCode = "KH-BACHHOA";
                        d.CustomerName = "Chuỗi Cửa hàng Bách Hóa Miền Bắc";
                        break;
                    default:
                        d.CustomerCode = "KH-FPT";
                        d.CustomerName = "Công ty Cổ phần Bán lẻ Kỹ thuật số FPT";
                        break;
                }
            }
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

        // Bổ sung các phiếu kho trải dài qua các tháng T1, T2, T3/2026 để kiểm tra ma trận 12 tháng
        if (!await db.Docs.AnyAsync(d => d.Code == "PNSEED-M01"))
        {
            var whs = await db.Warehouses.ToListAsync();
            var prods = await db.Products.ToListAsync();
            var hn = whs.FirstOrDefault(w => w.Code == "KHO-HN")?.Id;
            var hcm = whs.FirstOrDefault(w => w.Code == "KHO-HCM")?.Id;
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001")?.Id;
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001")?.Id;
            var pk = prods.FirstOrDefault(p => p.Code == "PK-001")?.Id;
            var vay = prods.FirstOrDefault(p => p.Code == "VAY-001")?.Id;

            int currentYear = DateTime.Today.Year;

            if (hn.HasValue && ao.HasValue && quan.HasValue && pk.HasValue && vay.HasValue)
            {
                // Tháng 1: Nhập đợt Tết & Xuất phân phối đầu năm
                var pnM01 = new StockDoc
                {
                    Type = DocType.In,
                    ToWarehouseId = hn.Value,
                    Code = "PNSEED-M01",
                    SupplierCode = "NCC-MAY10",
                    SupplierName = "Tổng Công ty May 10 - CTCP",
                    Date = new DateTime(currentYear, 1, 12, 9, 30, 0),
                    Status = DocStatus.Posted,
                    Note = "Nhập hàng phục vụ chiến dịch Tết Nguyên Đán",
                    CreatedBy = "seed"
                };
                pnM01.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 60 });
                pnM01.Lines.Add(new StockDocLine { ProductId = quan.Value, Quantity = 45 });
                pnM01.Lines.Add(new StockDocLine { ProductId = vay.Value, Quantity = 35 });
                db.Docs.Add(pnM01);

                var pxM01 = new StockDoc
                {
                    Type = DocType.Out,
                    FromWarehouseId = hn.Value,
                    Code = "PXSEED-M01",
                    Date = new DateTime(currentYear, 1, 24, 14, 0, 0),
                    Status = DocStatus.Posted,
                    Note = "Xuất hàng đợt 1 phục vụ chuỗi cửa hàng thời trang",
                    CreatedBy = "seed"
                };
                pxM01.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 30 });
                pxM01.Lines.Add(new StockDocLine { ProductId = quan.Value, Quantity = 20 });
                pxM01.Lines.Add(new StockDocLine { ProductId = pk.Value, Quantity = 15 });
                db.Docs.Add(pxM01);

                // Tháng 2: Nhập bổ sung sau Tết & Xuất bán tháng 2
                var pnM02 = new StockDoc
                {
                    Type = DocType.In,
                    ToWarehouseId = hn.Value,
                    Code = "PNSEED-M02",
                    SupplierCode = "NCC-PHONGPHU",
                    SupplierName = "Tổng Công ty CP Dệt May Phong Phú",
                    Date = new DateTime(currentYear, 2, 10, 10, 15, 0),
                    Status = DocStatus.Posted,
                    Note = "Nhập phục hồi cơ cấu tồn kho sau kỳ nghỉ Tết",
                    CreatedBy = "seed"
                };
                pnM02.Lines.Add(new StockDocLine { ProductId = quan.Value, Quantity = 50 });
                pnM02.Lines.Add(new StockDocLine { ProductId = pk.Value, Quantity = 40 });
                db.Docs.Add(pnM02);

                var pxM02 = new StockDoc
                {
                    Type = DocType.Out,
                    FromWarehouseId = hn.Value,
                    Code = "PXSEED-M02",
                    Date = new DateTime(currentYear, 2, 22, 16, 30, 0),
                    Status = DocStatus.Posted,
                    Note = "Xuất hàng chiến dịch ngày lễ Valentine và phụ kiện",
                    CreatedBy = "seed"
                };
                pxM02.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 25 });
                pxM02.Lines.Add(new StockDocLine { ProductId = pk.Value, Quantity = 20 });
                pxM02.Lines.Add(new StockDocLine { ProductId = vay.Value, Quantity = 15 });
                db.Docs.Add(pxM02);

                // Tháng 3: Nhập bộ sưu tập xuân hè & Xuất đại lý
                var pnM03 = new StockDoc
                {
                    Type = DocType.In,
                    ToWarehouseId = hn.Value,
                    Code = "PNSEED-M03",
                    SupplierCode = "NCC-VIETTIEN",
                    SupplierName = "Tổng Công ty CP May Việt Tiến",
                    Date = new DateTime(currentYear, 3, 5, 8, 45, 0),
                    Status = DocStatus.Posted,
                    Note = "Nhập ra mắt bộ sưu tập thời trang công sở mới",
                    CreatedBy = "seed"
                };
                pnM03.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 80 });
                pnM03.Lines.Add(new StockDocLine { ProductId = vay.Value, Quantity = 55 });
                db.Docs.Add(pnM03);

                var pxM03 = new StockDoc
                {
                    Type = DocType.Out,
                    FromWarehouseId = hn.Value,
                    Code = "PXSEED-M03",
                    Date = new DateTime(currentYear, 3, 20, 11, 0, 0),
                    Status = DocStatus.Posted,
                    Note = "Xuất đại lý phân phối miền Bắc tháng 3",
                    CreatedBy = "seed"
                };
                pxM03.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 40 });
                pxM03.Lines.Add(new StockDocLine { ProductId = quan.Value, Quantity = 30 });
                pxM03.Lines.Add(new StockDocLine { ProductId = vay.Value, Quantity = 25 });
                db.Docs.Add(pxM03);

                await db.SaveChangesAsync();
            }
        }

        if (!await db.Audits.IgnoreQueryFilters().AnyAsync(a => a.Code == "KKSEED-001"))
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

        if (!await db.Docs.AnyAsync(d => d.Code == "PNDRAFT-001"))
        {
            var whs = await db.Warehouses.ToListAsync();
            var hn = whs.FirstOrDefault(w => w.Code == "KHO-HN")?.Id;
            var prods = await db.Products.ToListAsync();
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001")?.Id;
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001")?.Id;

            if (hn.HasValue && ao.HasValue && quan.HasValue)
            {
                // Phiếu nhập mua NCC đang trên đường về (Back-order)
                var pnDraft = new StockDoc
                {
                    Type = DocType.In,
                    ToWarehouseId = hn.Value,
                    Code = "PNDRAFT-001",
                    SupplierCode = "NCC-MAY10",
                    SupplierName = "Tổng Công ty May 10 - CTCP",
                    Status = DocStatus.Draft,
                    Date = DateTime.Now,
                    RefNo = "PO-2026-0330",
                    Note = "Đơn đặt mua bổ sung hàng dự kiến giao trong tuần (Back-order)",
                    CreatedBy = "purchaser"
                };
                pnDraft.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 35 });
                pnDraft.Lines.Add(new StockDocLine { ProductId = quan.Value, Quantity = 25 });
                db.Docs.Add(pnDraft);

                // Phiếu xuất kho đang soạn hàng (Blocked / Reserved)
                var pxDraft = new StockDoc
                {
                    Type = DocType.Out,
                    FromWarehouseId = hn.Value,
                    Code = "PXDRAFT-001",
                    Status = DocStatus.Draft,
                    Date = DateTime.Now,
                    RefNo = "SO-2026-889",
                    Note = "Đơn xuất bán sỉ đại lý miền Bắc - đang giữ chỗ đóng gói (Blocked)",
                    CreatedBy = "saleman"
                };
                pxDraft.Lines.Add(new StockDocLine { ProductId = ao.Value, Quantity = 10 });
                pxDraft.Lines.Add(new StockDocLine { ProductId = quan.Value, Quantity = 8 });
                db.Docs.Add(pxDraft);

                await db.SaveChangesAsync();
            }
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
                    MoveOrdTypeCode = "MOVE_BRANCH",
                    MoveOrdTypeName = "Điều chuyển chi nhánh & Cửa hàng",
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
                        MoveOrdTypeCode = "MOVE_REPLENISH",
                        MoveOrdTypeName = "Điều chuyển bổ sung định mức an toàn",
                        Status = MoveOrderStatus.Pending,
                        Date = DateTime.Now,
                        CreatedAt = DateTime.Now,
                        Note = "Yêu cầu chuyển gấp phụ kiện thắt lưng cho showroom HCM",
                        CreatedBy = "seed"
                    };
                    moPending.Lines.Add(new MoveOrderLine { ProductId = pk.Value, Quantity = 5, Note = "Thắt lưng da cao cấp" });
                    db.MoveOrders.Add(moPending);
                }

                // Lệnh 3: Đã duyệt - Chuyển bảo hành kiểm định kỹ thuật (MOVE_WARRANTY)
                if (pk.HasValue)
                {
                    var moWarranty = new MoveOrder
                    {
                        Code = "MOSEED-003",
                        FromWarehouseId = hcm.Value,
                        ToWarehouseId = hn.Value,
                        MoveOrdTypeCode = "MOVE_WARRANTY",
                        MoveOrdTypeName = "Điều chuyển bảo hành & Kiểm định",
                        Status = MoveOrderStatus.Approved,
                        Date = DateTime.Now.AddDays(-1),
                        CreatedAt = DateTime.Now.AddDays(-1),
                        ApprovedAt = DateTime.Now.AddHours(-12),
                        Note = "Chuyển sản phẩm bảo hành về trung tâm kiểm định kỹ thuật Hà Nội",
                        CreatedBy = "seed"
                    };
                    moWarranty.Lines.Add(new MoveOrderLine { ProductId = pk.Value, Quantity = 2, Note = "Kiểm định khóa kim loại" });
                    db.MoveOrders.Add(moWarranty);
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
        if (!await db.InventoryTransactions.AnyAsync())
        {
            var whs = await db.Warehouses.ToListAsync();
            var prods = await db.Products.ToListAsync();
            var hn = whs.FirstOrDefault(w => w.Code == "KHO-HN")?.Id;
            var hcm = whs.FirstOrDefault(w => w.Code == "KHO-HCM")?.Id;
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001")?.Id;
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001")?.Id;
            var pk = prods.FirstOrDefault(p => p.Code == "PK-001")?.Id;

            var today = DateTime.Today;

            if (hn.HasValue && hcm.HasValue && ao.HasValue && quan.HasValue && pk.HasValue)
            {
                var txns = new List<InventoryTransaction>
                {
                    // 1. Nhập kho mua hàng (InvF_InventoryIn) - Kho HN
                    new()
                    {
                        WarehouseId = hn.Value, ProductId = ao.Value,
                        TxnType = InventoryTxnType.In, Quality = InventoryTxnQuality.OK,
                        FunctionName = "InvF_InventoryIn_APPRX",
                        QtyChTotalOK = 200, QtyChBlockOK = 0, QtyChTotalNG = 0, QtyChBlockNG = 0,
                        RefType = "StockDoc", RefCode00 = "PNSEED-001",
                        Remark = "Nhập mua áo sơ mi trắng từ NCC An Phát",
                        CreatedBy = "seed", CreatedAt = today.AddDays(-55)
                    },
                    new()
                    {
                        WarehouseId = hn.Value, ProductId = quan.Value,
                        TxnType = InventoryTxnType.In, Quality = InventoryTxnQuality.OK,
                        FunctionName = "InvF_InventoryIn_APPRX",
                        QtyChTotalOK = 150, QtyChBlockOK = 0, QtyChTotalNG = 0, QtyChBlockNG = 0,
                        RefType = "StockDoc", RefCode00 = "PNSEED-001",
                        Remark = "Nhập mua quần jeans slim từ NCC Việt Tiến",
                        CreatedBy = "seed", CreatedAt = today.AddDays(-55)
                    },
                    // 2. Xuất kho bán hàng (InvF_InventoryOut) - Kho HN
                    new()
                    {
                        WarehouseId = hn.Value, ProductId = ao.Value,
                        TxnType = InventoryTxnType.Out, Quality = InventoryTxnQuality.OK,
                        FunctionName = "InvF_InventoryOut_APPRX",
                        QtyChTotalOK = -60, QtyChBlockOK = 0, QtyChTotalNG = 0, QtyChBlockNG = 0,
                        RefType = "StockDoc", RefCode00 = "PXSEED-001",
                        Remark = "Xuất bán áo sơ mi cho đại lý An Phát",
                        CreatedBy = "seed", CreatedAt = today.AddDays(-40)
                    },
                    // 3. Điều chuyển kho (InvF_MoveOrd) - HN -> HCM
                    new()
                    {
                        WarehouseId = hn.Value, ProductId = quan.Value,
                        TxnType = InventoryTxnType.Move, Quality = InventoryTxnQuality.OK,
                        FunctionName = "InvF_MoveOrd_Out",
                        QtyChTotalOK = -30, QtyChBlockOK = 0, QtyChTotalNG = 0, QtyChBlockNG = 0,
                        RefType = "MoveOrder", RefCode00 = "DCSEED-001",
                        Remark = "Xuất điều chuyển quần jeans sang Kho TP.HCM",
                        CreatedBy = "seed", CreatedAt = today.AddDays(-2)
                    },
                    new()
                    {
                        WarehouseId = hcm.Value, ProductId = quan.Value,
                        TxnType = InventoryTxnType.Move, Quality = InventoryTxnQuality.OK,
                        FunctionName = "InvF_MoveOrd_In",
                        QtyChTotalOK = 30, QtyChBlockOK = 0, QtyChTotalNG = 0, QtyChBlockNG = 0,
                        RefType = "MoveOrder", RefCode00 = "DCSEED-001",
                        Remark = "Nhận điều chuyển quần jeans từ Kho Hà Nội",
                        CreatedBy = "seed", CreatedAt = today.AddDays(-2)
                    },
                    // 4. Cân bằng kiểm kê (InvF_InvAudit) - Kho HN
                    new()
                    {
                        WarehouseId = hn.Value, ProductId = pk.Value,
                        TxnType = InventoryTxnType.Audit, Quality = InventoryTxnQuality.OK,
                        FunctionName = "InvF_InvAudit_Finish",
                        QtyChTotalOK = -5, QtyChBlockOK = 0, QtyChTotalNG = 0, QtyChBlockNG = 0,
                        RefType = "StockAudit", RefCode00 = "KKSEED-001",
                        Remark = "Cân bằng thiếu 5 phụ kiện sau kiểm kê định kỳ",
                        CreatedBy = "seed", CreatedAt = today.AddDays(-10)
                    },
                    // 5. Khách trả hàng (InvF_InventoryCusReturn) - Kho HN
                    new()
                    {
                        WarehouseId = hn.Value, ProductId = ao.Value,
                        TxnType = InventoryTxnType.CusReturn, Quality = InventoryTxnQuality.OK,
                        FunctionName = "InvF_InventoryCusReturn_APPRX",
                        QtyChTotalOK = 8, QtyChBlockOK = 0, QtyChTotalNG = 0, QtyChBlockNG = 0,
                        RefType = "CustomerReturn", RefCode00 = "KTHSEED-001",
                        Remark = "Nhận lại 8 áo sơ mi khách đổi size",
                        CreatedBy = "seed", CreatedAt = today.AddDays(-5)
                    },
                    // 6. Trả hàng NCC (InvF_InventoryReturnSup) - Kho HN
                    new()
                    {
                        WarehouseId = hn.Value, ProductId = quan.Value,
                        TxnType = InventoryTxnType.ReturnSup, Quality = InventoryTxnQuality.NG,
                        FunctionName = "InvF_InventoryReturnSup_APPRX",
                        QtyChTotalOK = -12, QtyChBlockOK = 0, QtyChTotalNG = 0, QtyChBlockNG = 0,
                        RefType = "ReturnToSupplier", RefCode00 = "TNHSEED-001",
                        Remark = "Xuất trả 12 quần jeans lỗi đường may cho NCC",
                        CreatedBy = "seed", CreatedAt = today.AddDays(-3)
                    },
                    // 7. Nhập thành phẩm SX (InvF_InventoryInFG) - Kho HN
                    new()
                    {
                        WarehouseId = hn.Value, ProductId = ao.Value,
                        TxnType = InventoryTxnType.InFG, Quality = InventoryTxnQuality.OK,
                        FunctionName = "InvF_InventoryInFG_APPRX",
                        QtyChTotalOK = 100, QtyChBlockOK = 0, QtyChTotalNG = 0, QtyChBlockNG = 0,
                        RefType = "InventoryInFG", RefCode00 = "IFFGSEED-001",
                        Remark = "Nhập kho 100 áo sơ mi thành phẩm từ xưởng may",
                        CreatedBy = "seed", CreatedAt = today.AddDays(-1)
                    }
                };

                db.InventoryTransactions.AddRange(txns);
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

        // Seed dữ liệu Quản lý Nhập kho thành phẩm sản xuất (InventoryInFG - port từ InvF_InventoryInFG Skycic)
        if (!await db.InventoryInFGs.AnyAsync())
        {
            var whHn = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HN");
            var prods = await db.Products.ToListAsync();
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001");
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001");
            var vay = prods.FirstOrDefault(p => p.Code == "VAY-001");
            var today = DateTime.Today;

            if (whHn != null && ao != null && quan != null && vay != null)
            {
                // Phiếu nhập kho StockDoc cho phiếu nhập TP IFFG2603-001 đã duyệt
                var pnFG = new StockDoc
                {
                    Type = DocType.In,
                    ToWarehouseId = whHn.Id,
                    Code = "PNSEED-FG01",
                    Status = DocStatus.Posted,
                    Date = today.AddDays(-3),
                    RefNo = "IFFG2603-001",
                    Note = "Nhập kho thành phẩm theo phiếu IFFG2603-001 - Lệnh SX: LSX-2026-0312 từ Phân xưởng May Xuất Khẩu 1",
                    CreatedBy = "seed"
                };
                pnFG.Lines.Add(new StockDocLine { ProductId = ao.Id, Quantity = 48 });
                pnFG.Lines.Add(new StockDocLine { ProductId = quan.Id, Quantity = 30 });
                db.Docs.Add(pnFG);
                await db.SaveChangesAsync();

                // 1. Phiếu ĐÃ DUYỆT & NHẬP KHO (Approved)
                var fg1 = new InventoryInFG
                {
                    Code = "IFFG2603-001",
                    WarehouseId = whHn.Id,
                    FormType = InvInFGFormType.InternalProduction,
                    WorkshopName = "Phân xưởng May Xuất Khẩu 1",
                    WorkOrderNo = "LSX-2026-0312",
                    ShiftLeader = "Nguyễn Văn Thắng (Quản đốc)",
                    Date = today.AddDays(-3),
                    Status = InvInFGStatus.Approved,
                    StockDocId = pnFG.Id,
                    CreatedAt = today.AddDays(-3),
                    ApprovedAt = today.AddDays(-3).AddHours(2),
                    ApprovedBy = "admin",
                    Remark = "Nhập kho thành phẩm hoàn thành theo kế hoạch đơn hàng xuất khẩu quý 1",
                    CreatedBy = "seed"
                };

                fg1.Lines.Add(new InventoryInFGLine
                {
                    ProductId = ao.Id,
                    PlanQty = 50,
                    ActualQty = 48,
                    DefectQty = 2,
                    UnitCost = 145000m,
                    ProductionDate = today.AddDays(-4),
                    Note = "48 áo sơ mi đạt chuẩn KCS xuất khẩu loại 1; 2 áo lệch khuy chuyển sửa lại xưởng"
                });

                fg1.Lines.Add(new InventoryInFGLine
                {
                    ProductId = quan.Id,
                    PlanQty = 30,
                    ActualQty = 30,
                    DefectQty = 0,
                    UnitCost = 270000m,
                    ProductionDate = today.AddDays(-4),
                    Note = "100% đạt chuẩn thông số kỹ thuật xuất xưởng"
                });

                fg1.Serials.Add(new InventoryInFGSerial { ProductId = ao.Id, SerialNo = "AO2603-001", Note = "Áo sơ mi size M" });
                fg1.Serials.Add(new InventoryInFGSerial { ProductId = ao.Id, SerialNo = "AO2603-002", Note = "Áo sơ mi size L" });
                fg1.Serials.Add(new InventoryInFGSerial { ProductId = ao.Id, SerialNo = "AO2603-003", Note = "Áo sơ mi size XL" });
                fg1.Serials.Add(new InventoryInFGSerial { ProductId = quan.Id, SerialNo = "QJ2603-001", Note = "Quần jeans size 31" });
                fg1.Serials.Add(new InventoryInFGSerial { ProductId = quan.Id, SerialNo = "QJ2603-002", Note = "Quần jeans size 32" });

                db.InventoryInFGs.Add(fg1);

                // 2. Phiếu ĐANG CHỜ DUYỆT KCS (Pending)
                var fg2 = new InventoryInFG
                {
                    Code = "IFFG2603-002",
                    WarehouseId = whHn.Id,
                    FormType = InvInFGFormType.Outsourced,
                    WorkshopName = "Xưởng Gia Công May Đo Tân Tiến",
                    WorkOrderNo = "LSX-2026-0318",
                    ShiftLeader = "Phạm Hồng Quân (Giám sát OEM)",
                    Date = today,
                    Status = InvInFGStatus.Pending,
                    CreatedAt = today,
                    Remark = "Nhập kho thành phẩm váy đầm công sở từ đối tác OEM Tân Tiến, đang chờ kiểm tra nghiệm thu KCS",
                    CreatedBy = "seed"
                };

                fg2.Lines.Add(new InventoryInFGLine
                {
                    ProductId = vay.Id,
                    PlanQty = 40,
                    ActualQty = 38,
                    DefectQty = 2,
                    UnitCost = 310000m,
                    ProductionDate = today.AddDays(-1),
                    Note = "38 váy đầm dạ hội đạt chuẩn đóng gói, 2 váy sờn mép chỉ trả đối tác dệt lại"
                });

                fg2.Serials.Add(new InventoryInFGSerial { ProductId = vay.Id, SerialNo = "VD2603-001", Note = "Váy đầm dạ hội size S" });
                fg2.Serials.Add(new InventoryInFGSerial { ProductId = vay.Id, SerialNo = "VD2603-002", Note = "Váy đầm dạ hội size M" });

                db.InventoryInFGs.Add(fg2);
                await db.SaveChangesAsync();
            }
        }

        // Seed dữ liệu Quản lý Xuất kho thành phẩm & Vận chuyển phân phối (InventoryOutFG - port từ InvF_InventoryOutFG Skycic)
        if (!await db.InventoryOutFGs.AnyAsync())
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO01") ?? await db.Warehouses.FirstAsync();
            var ao = await db.Products.FirstOrDefaultAsync(p => p.Code == "SP01");
            var quan = await db.Products.FirstOrDefaultAsync(p => p.Code == "SP02");

            if (ao != null && quan != null)
            {
                // 1. Phiếu ĐÃ DUYỆT XUẤT KHO (Approved)
                var outFg1 = new InventoryOutFG
                {
                    Code = "IFOFG260328-001",
                    WarehouseId = wh.Id,
                    OutType = InvOutFGType.Commercial,
                    FormType = InvOutFGFormType.BarcodeSerial,
                    CustomerName = "Công ty Cổ phần Thương mại & Thời trang Hà Nội",
                    AgentCode = "DL-HN-001",
                    DeliveryAddress = "Kho Tổng Đông Anh, Km 12 Quốc lộ 3, Hà Nội",
                    DriverName = "Trần Văn Vận Chuyển",
                    DriverPhone = "0912.888.999",
                    PlateNo = "29C-888.66",
                    MoocNo = "29R-012.34",
                    OrderNo = "DH-2026-0328-01",
                    Date = DateTime.Today.AddDays(-2),
                    CreatedBy = "admin",
                    Status = InvOutFGStatus.Approved,
                    CreatedAt = DateTime.Now.AddDays(-2),
                    ApprovedAt = DateTime.Now.AddDays(-2).AddHours(2),
                    ApprovedBy = "admin",
                    Remark = "Xuất giao hàng đợt 1 theo hợp đồng phân phối đại lý miền Bắc"
                };

                outFg1.Lines.Add(new InventoryOutFGLine
                {
                    ProductId = ao.Id,
                    Qty = 20,
                    UnitPrice = 250_000m,
                    UnitCost = ao.CostPrice > 0 ? ao.CostPrice : 150_000m,
                    Note = "Áo sơ mi nam cao cấp"
                });

                outFg1.Lines.Add(new InventoryOutFGLine
                {
                    ProductId = quan.Id,
                    Qty = 15,
                    UnitPrice = 380_000m,
                    UnitCost = quan.CostPrice > 0 ? quan.CostPrice : 220_000m,
                    Note = "Quần jeans nam slimfit"
                });

                outFg1.Serials.Add(new InventoryOutFGSerial { ProductId = ao.Id, SerialNo = "AO2603-001", Note = "Đã xuất kho cho DL-HN-001" });
                outFg1.Serials.Add(new InventoryOutFGSerial { ProductId = ao.Id, SerialNo = "AO2603-002", Note = "Đã xuất kho cho DL-HN-001" });
                outFg1.Serials.Add(new InventoryOutFGSerial { ProductId = quan.Id, SerialNo = "QJ2603-001", Note = "Đã xuất kho cho DL-HN-001" });

                db.InventoryOutFGs.Add(outFg1);

                // 2. Phiếu ĐANG CHỜ DUYỆT XUẤT (Pending)
                var outFg2 = new InventoryOutFG
                {
                    Code = "IFOFG260330-002",
                    WarehouseId = wh.Id,
                    OutType = InvOutFGType.EndCustomer,
                    FormType = InvOutFGFormType.QuantityOnly,
                    CustomerName = "Dự án Đồng phục Công sở Tập đoàn Viễn thông VNPT",
                    AgentCode = "DA-VNPT-02",
                    DeliveryAddress = "Tòa nhà VNPT, 57 Huỳnh Thúc Kháng, Đống Đa, Hà Nội",
                    DriverName = "Nguyễn Văn Lái Xe",
                    DriverPhone = "0988.765.432",
                    PlateNo = "30E-678.90",
                    MoocNo = null,
                    OrderNo = "HDBD-2026-0330",
                    Date = DateTime.Today,
                    CreatedBy = "admin",
                    Status = InvOutFGStatus.Pending,
                    CreatedAt = DateTime.Now,
                    Remark = "Giao hàng trực tiếp tại kho dự án, kiểm đếm tại chân công trình"
                };

                outFg2.Lines.Add(new InventoryOutFGLine
                {
                    ProductId = ao.Id,
                    Qty = 10,
                    UnitPrice = 240_000m,
                    UnitCost = ao.CostPrice > 0 ? ao.CostPrice : 150_000m,
                    Note = "Áo sơ mi đồng phục size chuẩn"
                });

                db.InventoryOutFGs.Add(outFg2);
                await db.SaveChangesAsync();
            }
        }

        // Seed dữ liệu Phiếu nhập kho mua hàng (PurchaseReceipt - port từ InvF_InventoryIn Skycic)
        if (!await db.PurchaseReceipts.AnyAsync())
        {
            var whHn = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HN");
            var whHcm = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HCM");
            var prods = await db.Products.ToListAsync();
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001");
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001");
            var vay = prods.FirstOrDefault(p => p.Code == "VAY-001");
            var today = DateTime.Today;

            if (whHn != null && ao != null && quan != null && vay != null)
            {
                // Phiếu kho nhập mua đã ghi sổ cho phiếu PN2603-001 đã duyệt
                var pnBuy = new StockDoc
                {
                    Type = DocType.In,
                    ToWarehouseId = whHn.Id,
                    Code = "PNSEED-BUY01",
                    Status = DocStatus.Posted,
                    Date = today.AddDays(-5),
                    SupplierCode = "NCC-001",
                    SupplierName = "Công ty TNHH Vải Sợi Việt",
                    RefNo = "PN2603-001",
                    Note = "Nhập kho mua hàng theo phiếu PN2603-001 - NCC: Công ty TNHH Vải Sợi Việt - HĐ: HD-2026-0312",
                    CreatedBy = "seed"
                };
                pnBuy.Lines.Add(new StockDocLine { ProductId = ao.Id, Quantity = 200 });
                pnBuy.Lines.Add(new StockDocLine { ProductId = quan.Id, Quantity = 150 });
                db.Docs.Add(pnBuy);
                await db.SaveChangesAsync();

                // 1. Phiếu ĐÃ DUYỆT & NHẬP KHO (Approved)
                var pr1 = new PurchaseReceipt
                {
                    Code = "PN2603-001",
                    WarehouseId = whHn.Id,
                    InvInTypeCode = "IN_BUY",
                    InvInTypeName = "Nhập mua hàng Nhà cung cấp",
                    SupplierName = "Công ty TNHH Vải Sợi Việt",
                    SupplierCode = "NCC-001",
                    InvoiceNo = "HD-2026-0312",
                    InvoiceDate = today.AddDays(-5),
                    OrderNo = "PO-2026-0312",
                    UserDeliver = "Trần Văn Giao",
                    VehicleNo = "29C-123.45",
                    ContractNo = "HĐMB-2026-01",
                    Date = today.AddDays(-5),
                    Status = PurchaseReceiptStatus.Approved,
                    StockDocId = pnBuy.Id,
                    CreatedAt = today.AddDays(-5),
                    ApprovedAt = today.AddDays(-5).AddHours(3),
                    ApprovedBy = "admin",
                    Remark = "Nhập kho lô vải và phụ liệu may mặc theo hợp đồng mua quý 1",
                    CreatedBy = "seed"
                };
                pr1.Lines.Add(new PurchaseReceiptLine { ProductId = ao.Id, Quantity = 200, UnitPrice = 120000m, VATRate = 10, UnitCode = "cái", Note = "Áo sơ mi trắng size M" });
                pr1.Lines.Add(new PurchaseReceiptLine { ProductId = quan.Id, Quantity = 150, UnitPrice = 180000m, VATRate = 10, UnitCode = "cái", Note = "Quần jeans xanh size 32" });
                db.PurchaseReceipts.Add(pr1);

                // 2. Phiếu ĐANG CHỜ DUYỆT (Pending)
                var pr2 = new PurchaseReceipt
                {
                    Code = "PN2603-002",
                    WarehouseId = whHn.Id,
                    InvInTypeCode = "IN_BUY",
                    InvInTypeName = "Nhập mua hàng Nhà cung cấp",
                    SupplierName = "Công ty CP Phụ Liệu May Mặc Hà Nội",
                    SupplierCode = "NCC-002",
                    InvoiceNo = "HD-2026-0325",
                    InvoiceDate = today,
                    OrderNo = "PO-2026-0325",
                    UserDeliver = "Lê Thị Vận",
                    VehicleNo = "30F-678.90",
                    Date = today,
                    Status = PurchaseReceiptStatus.Pending,
                    CreatedAt = today,
                    Remark = "Chờ kiểm đếm và phê duyệt nhập kho lô váy đầm dạ hội",
                    CreatedBy = "seed"
                };
                pr2.Lines.Add(new PurchaseReceiptLine { ProductId = vay.Id, Quantity = 80, UnitPrice = 350000m, VATRate = 8, UnitCode = "cái", Note = "Váy đầm dạ hội size S-M" });
                db.PurchaseReceipts.Add(pr2);

                await db.SaveChangesAsync();
            }
        }

        // Seed dữ liệu Quản lý Hộp đóng gói & Phân cấp bao bì kho (InventoryBox - port từ Inv_InventoryBox Skycic)
        if (!await db.InventoryBoxes.AnyAsync())
        {
            var whHn = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HN");
            var prods = await db.Products.ToListAsync();
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001");
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001");
            var pk = prods.FirstOrDefault(p => p.Code == "PK-001");
            var vay = prods.FirstOrDefault(p => p.Code == "VAY-001");

            var ctn1 = await db.InventoryCartons.FirstOrDefaultAsync(c => c.CartonCode == "CTN2603-HN01");
            var ctn2 = await db.InventoryCartons.FirstOrDefaultAsync(c => c.CartonCode == "CTN2603-HN02");

            var today = DateTime.Today;

            if (whHn != null && ao != null && quan != null && pk != null && vay != null)
            {
                var boxes = new List<InventoryBox>
                {
                    // 1. Hộp đã niêm phong & đã đóng vào Thùng carton CTN2603-HN01 (Áo sơ mi trắng - Hộp 1)
                    new()
                    {
                        WarehouseId = whHn.Id,
                        CartonId = ctn1?.Id,
                        BoxCode = "BOX2603-HN01",
                        QrCode = "BOX2603-HN01",
                        GenTimesBoxNo = "GTB2603251000",
                        SecretNo = "SEC-8839-A1",
                        BoxType = "Hộp duplex nắp gài (20x15x10)",
                        ProductId = ao.Id,
                        LotNo = "LOT-AO26-01",
                        Quantity = 10,
                        Capacity = 10,
                        LengthCm = 20, WidthCm = 15, HeightCm = 10,
                        GrossWeightKg = 2.1,
                        Status = BoxStatus.InCarton,
                        FlagMap = true,
                        FlagUsed = true,
                        ShelfLocation = "A-01-01",
                        PackerName = "Nguyễn Văn Đóng",
                        PackedAt = today.AddDays(-5),
                        SealedAt = today.AddDays(-5).AddHours(1),
                        Remark = "Hộp 1/3 đóng vào thùng carton CTN2603-HN01"
                    },
                    // 2. Hộp đã niêm phong & đã đóng vào Thùng carton CTN2603-HN01 (Áo sơ mi trắng - Hộp 2)
                    new()
                    {
                        WarehouseId = whHn.Id,
                        CartonId = ctn1?.Id,
                        BoxCode = "BOX2603-HN02",
                        QrCode = "BOX2603-HN02",
                        GenTimesBoxNo = "GTB2603251000",
                        SecretNo = "SEC-8839-A2",
                        BoxType = "Hộp duplex nắp gài (20x15x10)",
                        ProductId = ao.Id,
                        LotNo = "LOT-AO26-01",
                        Quantity = 10,
                        Capacity = 10,
                        LengthCm = 20, WidthCm = 15, HeightCm = 10,
                        GrossWeightKg = 2.1,
                        Status = BoxStatus.InCarton,
                        FlagMap = true,
                        FlagUsed = true,
                        ShelfLocation = "A-01-01",
                        PackerName = "Nguyễn Văn Đóng",
                        PackedAt = today.AddDays(-5),
                        SealedAt = today.AddDays(-5).AddHours(1),
                        Remark = "Hộp 2/3 đóng vào thùng carton CTN2603-HN01"
                    },
                    // 3. Hộp đã niêm phong & đã đóng vào Thùng carton CTN2603-HN02 (Quần jeans slim)
                    new()
                    {
                        WarehouseId = whHn.Id,
                        CartonId = ctn2?.Id,
                        BoxCode = "BOX2603-HN03",
                        QrCode = "BOX2603-HN03",
                        GenTimesBoxNo = "GTB2603260930",
                        SecretNo = "SEC-9921-B1",
                        BoxType = "Hộp carton bồi sóng E (30x20x12)",
                        ProductId = quan.Id,
                        LotNo = "LOT-QJ26-01",
                        Quantity = 10,
                        Capacity = 10,
                        LengthCm = 30, WidthCm = 20, HeightCm = 12,
                        GrossWeightKg = 4.5,
                        Status = BoxStatus.InCarton,
                        FlagMap = true,
                        FlagUsed = true,
                        ShelfLocation = "A-01-02",
                        PackerName = "Trần Thị Kiện",
                        PackedAt = today.AddDays(-4),
                        SealedAt = today.AddDays(-4).AddHours(1),
                        Remark = "Hộp quần jeans đóng trong thùng CTN2603-HN02"
                    },
                    // 4. Hộp đã niêm phong ĐỘC LẬP / CHỜ GÁN THÙNG (Thắt lưng da cao cấp)
                    new()
                    {
                        WarehouseId = whHn.Id,
                        CartonId = null,
                        BoxCode = "BOX2603-HN04",
                        QrCode = "BOX2603-HN04",
                        GenTimesBoxNo = "GTB2603281400",
                        SecretNo = "SEC-7712-PK",
                        BoxType = "Hộp quà tặng bọc nhung (15x12x8)",
                        ProductId = pk.Id,
                        LotNo = "LOT-TL26-01",
                        Quantity = 5,
                        Capacity = 5,
                        LengthCm = 15, WidthCm = 12, HeightCm = 8,
                        GrossWeightKg = 1.2,
                        Status = BoxStatus.Sealed,
                        FlagMap = false,
                        FlagUsed = true,
                        ShelfLocation = "B-01-01",
                        PackerName = "Lê Văn Hộp",
                        PackedAt = today.AddDays(-2),
                        SealedAt = today.AddDays(-2).AddHours(2),
                        Remark = "Hộp quà tặng thắt lưng da cao cấp, dán tem cào bảo mật, chờ gán thùng"
                    },
                    // 5. Hộp đang đóng dở dang (Váy đầm công sở)
                    new()
                    {
                        WarehouseId = whHn.Id,
                        CartonId = null,
                        BoxCode = "BOX2603-HN05",
                        QrCode = "BOX2603-HN05",
                        GenTimesBoxNo = "GTB2603290800",
                        SecretNo = null,
                        BoxType = "Hộp duplex nắp gài (25x20x10)",
                        ProductId = vay.Id,
                        LotNo = "LOT-VD26-01",
                        Quantity = 3,
                        Capacity = 5,
                        LengthCm = 25, WidthCm = 20, HeightCm = 10,
                        GrossWeightKg = 1.0,
                        Status = BoxStatus.Packing,
                        FlagMap = false,
                        FlagUsed = true,
                        ShelfLocation = "A-02-01",
                        PackerName = "Lê Văn Hộp",
                        PackedAt = today.AddDays(-1),
                        Remark = "Đang đóng dở 3/5 váy đầm công sở chờ KCS hoàn thiện"
                    },
                    // 6. Hộp rỗng mới khởi tạo theo đợt
                    new()
                    {
                        WarehouseId = whHn.Id,
                        CartonId = null,
                        BoxCode = "BOX2603-HN06",
                        QrCode = "BOX2603-HN06",
                        GenTimesBoxNo = "GTB2603300800",
                        SecretNo = null,
                        BoxType = "Hộp duplex tiêu chuẩn (20x15x10)",
                        Quantity = 0,
                        Capacity = 10,
                        LengthCm = 20, WidthCm = 15, HeightCm = 10,
                        GrossWeightKg = 0.2,
                        Status = BoxStatus.Empty,
                        FlagMap = false,
                        FlagUsed = false,
                        ShelfLocation = "B-01-02",
                        Remark = "Hộp rỗng mới sinh mã sẵn sàng sử dụng"
                    },
                    // 7. Hộp đã xuất kho giao lẻ
                    new()
                    {
                        WarehouseId = whHn.Id,
                        CartonId = null,
                        BoxCode = "BOX2603-HN07",
                        QrCode = "BOX2603-HN07",
                        GenTimesBoxNo = "GTB2603271100",
                        SecretNo = "SEC-6623-EX",
                        BoxType = "Hộp quà tặng bọc nhung (15x12x8)",
                        ProductId = pk.Id,
                        LotNo = "LOT-TL26-01",
                        Quantity = 5,
                        Capacity = 5,
                        LengthCm = 15, WidthCm = 12, HeightCm = 8,
                        GrossWeightKg = 1.2,
                        Status = BoxStatus.Shipped,
                        FlagMap = false,
                        FlagUsed = true,
                        ShelfLocation = "B-01-01",
                        PackerName = "Nguyễn Văn Đóng",
                        PackedAt = today.AddDays(-3),
                        SealedAt = today.AddDays(-3).AddHours(1),
                        ShippedAt = today.AddDays(-1),
                        RefDocNo = "PXSEED-001",
                        Remark = "Đã xuất bán lẻ kèm phiếu xuất kho PXSEED-001"
                    }
                };

                db.InventoryBoxes.AddRange(boxes);
                await db.SaveChangesAsync();
            }
        }

        // Seed dữ liệu Lệnh giao hàng / Phiếu xuất điều phối bản đồ (Rpt_MapDeliveryOrder_ByInvFIOut)
        if (!await db.Docs.AnyAsync(d => d.Code == "PX-MAP-001"))
        {
            var whHn = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HN");
            var whHcm = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HCM");
            var prods = await db.Products.ToListAsync();
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001");
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001");
            var pk = prods.FirstOrDefault(p => p.Code == "PK-001");
            var vay = prods.FirstOrDefault(p => p.Code == "VAY-001");

            var today = DateTime.Today;

            if (whHn != null && whHcm != null && ao != null && quan != null && pk != null && vay != null)
            {
                // 1. Phiếu xuất đã giao hoàn tất (3 ngày trước)
                var doc1 = new StockDoc
                {
                    Code = "PX-MAP-001",
                    Type = DocType.Out,
                    FromWarehouseId = whHn.Id,
                    CustomerCode = "KH001",
                    CustomerName = "Công ty TNHH Thời trang An Phú",
                    Date = today.AddDays(-3),
                    CreatedAt = today.AddDays(-3),
                    Status = DocStatus.Posted,
                    Note = "Xuất bán buôn theo đơn đặt hàng ĐH-AP-01",
                    CreatedBy = "dispatcher"
                };
                doc1.Lines.Add(new StockDocLine { ProductId = ao.Id, Quantity = 25 });
                doc1.Lines.Add(new StockDocLine { ProductId = pk.Id, Quantity = 10 });
                db.Docs.Add(doc1);

                // 2. Phiếu xuất chờ giao nhưng đã QUÁ HẠN hôm nay => CẢNH BÁO GIAO CHẬM (high-line-delay)
                var doc2 = new StockDoc
                {
                    Code = "PX-MAP-002",
                    Type = DocType.Out,
                    FromWarehouseId = whHn.Id,
                    CustomerCode = "KH002",
                    CustomerName = "Chuỗi Cửa hàng Thời trang Tràng Thi",
                    Date = today.AddDays(-1),
                    CreatedAt = today.AddDays(-1),
                    Status = DocStatus.Draft, // Chờ giao
                    Note = "Cần xe tải nhỏ giao gấp điểm bán phố đi bộ",
                    CreatedBy = "sales-admin"
                };
                doc2.Lines.Add(new StockDocLine { ProductId = quan.Id, Quantity = 20 });
                db.Docs.Add(doc2);

                // 3. Phiếu xuất giao HÔM NAY (Today)
                var doc3 = new StockDoc
                {
                    Code = "PX-MAP-003",
                    Type = DocType.Out,
                    FromWarehouseId = whHcm.Id,
                    CustomerCode = "KH003",
                    CustomerName = "Đại lý Thời trang Phương Nam",
                    Date = today,
                    CreatedAt = today,
                    Status = DocStatus.Draft,
                    Note = "Lệnh xuất hàng giao trưa nay tại kho trung chuyển Tân Bình",
                    CreatedBy = "dispatcher"
                };
                doc3.Lines.Add(new StockDocLine { ProductId = vay.Id, Quantity = 30 });
                doc3.Lines.Add(new StockDocLine { ProductId = ao.Id, Quantity = 15 });
                db.Docs.Add(doc3);

                // 4. Phiếu xuất kế hoạch giao trong 2 ngày tới
                var doc4 = new StockDoc
                {
                    Code = "PX-MAP-004",
                    Type = DocType.Out,
                    FromWarehouseId = whHn.Id,
                    CustomerCode = "KH004",
                    CustomerName = "Công ty CP Bán lẻ Thời trang Việt",
                    Date = today.AddDays(2),
                    CreatedAt = today,
                    Status = DocStatus.Draft,
                    Note = "Đơn giao trung tâm thương mại Aeon Mall",
                    CreatedBy = "sales-admin"
                };
                doc4.Lines.Add(new StockDocLine { ProductId = pk.Id, Quantity = 40 });
                db.Docs.Add(doc4);

                // 5. Phiếu xuất kế hoạch giao trong 4 ngày tới
                var doc5 = new StockDoc
                {
                    Code = "PX-MAP-005",
                    Type = DocType.Out,
                    FromWarehouseId = whHcm.Id,
                    CustomerCode = "KH001",
                    CustomerName = "Công ty TNHH Thời trang An Phú",
                    Date = today.AddDays(4),
                    CreatedAt = today,
                    Status = DocStatus.Draft,
                    Note = "Cung ứng đợt 2 cho thị trường miền Nam",
                    CreatedBy = "dispatcher"
                };
                doc5.Lines.Add(new StockDocLine { ProductId = ao.Id, Quantity = 35 });
                db.Docs.Add(doc5);

                await db.SaveChangesAsync();
            }
        }

        // Bổ sung phiếu xuất thành phẩm trải quanh hôm nay
        if (!await db.InventoryOutFGs.AnyAsync(f => f.Code == "IFOFG-MAP-001"))
        {
            var whHn = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HN");
            var whHcm = await db.Warehouses.FirstOrDefaultAsync(w => w.Code == "KHO-HCM");
            var prods = await db.Products.ToListAsync();
            var ao = prods.FirstOrDefault(p => p.Code == "AO-001");
            var quan = prods.FirstOrDefault(p => p.Code == "QUAN-001");
            var today = DateTime.Today;

            if (whHn != null && whHcm != null && ao != null && quan != null)
            {
                var fg1 = new InventoryOutFG
                {
                    Code = "IFOFG-MAP-001",
                    WarehouseId = whHn.Id,
                    OutType = InvOutFGType.Commercial,
                    FormType = InvOutFGFormType.QuantityOnly,
                    CustomerName = "Đại lý Phân phối Cảng Xanh Hải Phòng",
                    AgentCode = "DL_HP01",
                    DeliveryAddress = "Số 55 Lạch Tray, Ngô Quyền, Hải Phòng",
                    DriverName = "Trần Đình Trọng",
                    DriverPhone = "0912.334.556",
                    PlateNo = "15C-456.78",
                    OrderNo = "PO-HP-2026",
                    Date = today.AddDays(-2),
                    CreatedBy = "admin",
                    Status = InvOutFGStatus.Approved,
                    ApprovedAt = today.AddDays(-2),
                    ApprovedBy = "admin",
                    Remark = "Đã xuất giao nguyên xe container Hải Phòng"
                };
                fg1.Lines.Add(new InventoryOutFGLine
                {
                    ProductId = ao.Id,
                    Qty = 50,
                    UnitPrice = 240_000m,
                    UnitCost = ao.CostPrice > 0 ? ao.CostPrice : 150_000m,
                    Note = "Áo sơ mi trắng xuất lô đạt chuẩn"
                });
                db.InventoryOutFGs.Add(fg1);

                // Lệnh xuất thành phẩm PENDING nhưng quá hạn (high-line-delay)
                var fg2 = new InventoryOutFG
                {
                    Code = "IFOFG-MAP-002",
                    WarehouseId = whHcm.Id,
                    OutType = InvOutFGType.Commercial,
                    FormType = InvOutFGFormType.QuantityOnly,
                    CustomerName = "Tổng Đại lý Phân phối Miền Nam - Phương Nam",
                    AgentCode = "DL_MN01",
                    DeliveryAddress = "Số 450 Hai Bà Trưng, Quận 1, TP.HCM",
                    DriverName = "Võ Văn Lái",
                    DriverPhone = "0909.112.233",
                    PlateNo = "51D-889.90",
                    OrderNo = "PO-MN-2026-03",
                    Date = today.AddDays(-1),
                    CreatedBy = "admin",
                    Status = InvOutFGStatus.Pending, // Chờ duyệt xuất giao
                    Remark = "Chờ xe tải giao hàng đến kho đại lý"
                };
                fg2.Lines.Add(new InventoryOutFGLine
                {
                    ProductId = quan.Id,
                    Qty = 30,
                    UnitPrice = 360_000m,
                    UnitCost = quan.CostPrice > 0 ? quan.CostPrice : 220_000m,
                    Note = "Quần jeans nam xuất đợt khuyến mãi"
                });
                db.InventoryOutFGs.Add(fg2);

                await db.SaveChangesAsync();
            }

            // ==================== SEED BIỂU MẪU IN KHO & TEM NHÃN (InvF_TempPrint & Mst_TempPrintType Skycic) ====================
            if (!await db.TempPrintTypes.AnyAsync())
            {
                db.TempPrintTypes.AddRange(
                    new TempPrintType { Code = "IN", Name = "Phiếu nhập kho (TT200 / TT133)", GroupCode = "DOC", Description = "Biểu mẫu chứng từ ghi nhận nhập mua, hoàn nhập hàng hóa kho", IsActive = true },
                    new TempPrintType { Code = "OUT", Name = "Phiếu xuất kho kiêm giao hàng", GroupCode = "DOC", Description = "Biểu mẫu xuất bán buôn, đại lý, xuất phục vụ phân phối & bán lẻ", IsActive = true },
                    new TempPrintType { Code = "MOVE", Name = "Lệnh & Phiếu điều chuyển kho", GroupCode = "DOC", Description = "Biểu mẫu vận chuyển lưu chuyển hàng nội bộ giữa các kho", IsActive = true },
                    new TempPrintType { Code = "AUDIT", Name = "Biên bản kiểm kê & đối soát", GroupCode = "DOC", Description = "Biên bản hội đồng kiểm kê kiểm đếm và xử lý chênh lệch kho", IsActive = true },
                    new TempPrintType { Code = "CARTON", Name = "Tem nhãn vận chuyển thùng Carton", GroupCode = "LABEL", Description = "Nhãn dán tiêu chuẩn kiện hàng Carton Barcode & QR Code", IsActive = true },
                    new TempPrintType { Code = "BOX", Name = "Tem nhãn định danh hộp bao bì", GroupCode = "LABEL", Description = "Nhãn dán hộp inner box quản lý quy cách sản phẩm", IsActive = true },
                    new TempPrintType { Code = "K80", Name = "Phiếu xuất giao nhanh nhiệt K80", GroupCode = "DOC", Description = "Biểu mẫu in máy in bill nhiệt khổ 80mm giao nhanh", IsActive = true }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.TempPrints.AnyAsync())
            {
                var bodyInA4 = @"<div style=""font-family:'Segoe UI',Arial,sans-serif; line-height:1.4; color:#111;"">
    <div style=""display:flex; justify-content:space-between; align-items:flex-start; border-bottom:2px solid #222; padding-bottom:8px; margin-bottom:15px;"">
        <div style=""font-size:12px;"">
            <div style=""font-weight:bold; font-size:14px; text-transform:uppercase;"">{{UnitName}}</div>
            <div>Địa chỉ: {{UnitAddress}}</div>
            <div>Điện thoại: {{UnitPhone}} | Email: {{UnitEmail}}</div>
        </div>
        <div style=""text-align:right; font-size:11px;"">
            <div style=""font-weight:bold;"">Mẫu số: 01 - VT</div>
            <div style=""font-style:italic;"">(Ban hành theo TT số 200/2014/TT-BTC)</div>
            <div style=""margin-top:4px;"">{{Barcode}}</div>
        </div>
    </div>
    <div style=""text-align:center; margin-bottom:15px;"">
        <div style=""font-size:20px; font-weight:bold; text-transform:uppercase; letter-spacing:1px;"">{{HeaderTitle}}</div>
        <div style=""font-style:italic; font-size:12px; margin-top:3px;"">{{DocDateFull}} — Số phiếu: <strong style=""font-size:14px; color:#b02a37;"">{{DocNo}}</strong></div>
        <div style=""font-size:11px; color:#555;"">{{SubTitle}}</div>
    </div>
    <div style=""margin-bottom:12px; font-size:13px; line-height:1.7;"">
        <div>- Người giao hàng: <strong>{{Deliverer}}</strong></div>
        <div>- Đơn vị / NCC: <strong>{{PartnerName}}</strong> (Địa chỉ: {{PartnerAddress}})</div>
        <div>- Lý do nhập kho: <em>{{Reason}}</em></div>
        <div>- Nhập tại kho: <strong>{{WarehouseName}}</strong> (Địa chỉ: {{WarehouseAddress}})</div>
    </div>
    {{ItemsTable}}
    <div style=""font-size:12px; margin-top:8px;"">
        <div>- Ghi chú: <em>{{Note}}</em></div>
        <div style=""font-style:italic; color:#666; margin-top:4px;"">{{NoteFooter}}</div>
    </div>
    {{Signatures}}
</div>";

                var bodyOutA4 = @"<div style=""font-family:'Segoe UI',Arial,sans-serif; line-height:1.4; color:#111;"">
    <div style=""display:flex; justify-content:space-between; align-items:flex-start; border-bottom:2px solid #222; padding-bottom:8px; margin-bottom:15px;"">
        <div style=""font-size:12px;"">
            <div style=""font-weight:bold; font-size:14px; text-transform:uppercase;"">{{UnitName}}</div>
            <div>Địa chỉ: {{UnitAddress}}</div>
            <div>Điện thoại: {{UnitPhone}} | Email: {{UnitEmail}}</div>
        </div>
        <div style=""text-align:right; font-size:11px;"">
            <div style=""font-weight:bold;"">Mẫu số: 02 - VT</div>
            <div style=""font-style:italic;"">(Ban hành theo TT số 200/2014/TT-BTC)</div>
            <div style=""margin-top:4px;"">{{Barcode}}</div>
        </div>
    </div>
    <div style=""text-align:center; margin-bottom:15px;"">
        <div style=""font-size:20px; font-weight:bold; text-transform:uppercase; letter-spacing:1px;"">{{HeaderTitle}}</div>
        <div style=""font-style:italic; font-size:12px; margin-top:3px;"">{{DocDateFull}} — Số phiếu: <strong style=""font-size:14px; color:#0d6efd;"">{{DocNo}}</strong></div>
        <div style=""font-size:11px; color:#555;"">{{SubTitle}}</div>
    </div>
    <div style=""margin-bottom:12px; font-size:13px; line-height:1.7;"">
        <div>- Người nhận hàng: <strong>{{Receiver}}</strong></div>
        <div>- Đơn vị nhận / Khách hàng: <strong>{{PartnerName}}</strong></div>
        <div>- Địa chỉ nhận hàng: <em>{{PartnerAddress}}</em></div>
        <div>- Lý do xuất kho: <em>{{Reason}}</em></div>
        <div>- Xuất tại kho: <strong>{{WarehouseName}}</strong> (Địa chỉ: {{WarehouseAddress}})</div>
    </div>
    {{ItemsTable}}
    <div style=""font-size:12px; margin-top:8px;"">
        <div>- Ghi chú: <em>{{Note}}</em></div>
        <div style=""font-style:italic; color:#666; margin-top:4px;"">{{NoteFooter}}</div>
    </div>
    {{Signatures}}
</div>";

                var bodyMoveA4 = @"<div style=""font-family:'Segoe UI',Arial,sans-serif; line-height:1.4; color:#111;"">
    <div style=""display:flex; justify-content:space-between; align-items:flex-start; border-bottom:2px solid #222; padding-bottom:8px; margin-bottom:15px;"">
        <div style=""font-size:12px;"">
            <div style=""font-weight:bold; font-size:14px; text-transform:uppercase;"">{{UnitName}}</div>
            <div>Địa chỉ: {{UnitAddress}}</div>
            <div>Hotline điều phối: {{UnitPhone}}</div>
        </div>
        <div style=""text-align:right; font-size:11px;"">
            <div style=""font-weight:bold;"">LỆNH VẬN CHUYỂN NỘI BỘ</div>
            <div style=""margin-top:4px;"">{{Barcode}}</div>
        </div>
    </div>
    <div style=""text-align:center; margin-bottom:15px;"">
        <div style=""font-size:20px; font-weight:bold; text-transform:uppercase; letter-spacing:1px; color:#198754;"">{{HeaderTitle}}</div>
        <div style=""font-style:italic; font-size:12px; margin-top:3px;"">{{DocDateFull}} — Lệnh số: <strong style=""font-size:14px;"">{{DocNo}}</strong></div>
        <div style=""font-size:11px; color:#555;"">{{SubTitle}}</div>
    </div>
    <div style=""display:flex; justify-content:space-between; background:#f8f9fa; border:1px solid #dee2e6; border-radius:4px; padding:10px; margin-bottom:12px; font-size:13px;"">
        <div style=""width:48%;"">
            <div style=""font-weight:bold; color:#0d6efd;"">KHO XUẤT ĐIỀU CHUYỂN:</div>
            <div>{{WarehouseName}}</div>
            <div style=""font-size:12px; color:#555;"">Địa chỉ: {{WarehouseAddress}}</div>
            <div style=""margin-top:4px;"">Thủ kho xuất: <strong>{{Deliverer}}</strong></div>
        </div>
        <div style=""width:48%; border-left:1px dashed #ccc; padding-left:15px;"">
            <div style=""font-weight:bold; color:#198754;"">KHO TIẾP NHẬN ĐẾN:</div>
            <div>{{PartnerName}}</div>
            <div style=""font-size:12px; color:#555;"">Địa chỉ: {{PartnerAddress}}</div>
            <div style=""margin-top:4px;"">Thủ kho nhận: <strong>{{Receiver}}</strong></div>
        </div>
    </div>
    <div style=""font-size:13px; margin-bottom:10px;"">- Mục đích điều chuyển: <em>{{Reason}}</em></div>
    {{ItemsTable}}
    <div style=""font-size:12px; margin-top:8px;"">
        <div>- Ghi chú vận chuyển: <em>{{Note}}</em></div>
        <div style=""font-style:italic; color:#666; margin-top:4px;"">{{NoteFooter}}</div>
    </div>
    {{Signatures}}
</div>";

                var bodyAuditA4 = @"<div style=""font-family:'Segoe UI',Arial,sans-serif; line-height:1.4; color:#111;"">
    <div style=""display:flex; justify-content:space-between; align-items:flex-start; border-bottom:2px solid #222; padding-bottom:8px; margin-bottom:15px;"">
        <div style=""font-size:12px;"">
            <div style=""font-weight:bold; font-size:14px; text-transform:uppercase;"">{{UnitName}}</div>
            <div>Địa điểm kho kiểm kê: {{WarehouseAddress}}</div>
        </div>
        <div style=""text-align:right; font-size:11px;"">
            <div style=""font-weight:bold;"">Mẫu số: 05 - VT (Kiểm kê)</div>
            <div>{{Barcode}}</div>
        </div>
    </div>
    <div style=""text-align:center; margin-bottom:15px;"">
        <div style=""font-size:20px; font-weight:bold; text-transform:uppercase; letter-spacing:1px; color:#6f42c1;"">{{HeaderTitle}}</div>
        <div style=""font-style:italic; font-size:12px; margin-top:3px;"">{{DocDateFull}} — Mã đợt kiểm kê: <strong style=""font-size:14px;"">{{DocNo}}</strong></div>
        <div style=""font-size:11px; color:#555;"">{{SubTitle}}</div>
    </div>
    <div style=""margin-bottom:12px; font-size:13px; line-height:1.7;"">
        <div>- Kho được kiểm kê: <strong>{{WarehouseName}}</strong></div>
        <div>- Hội đồng kiểm kê: <strong>1. Ông Lê Hoàng Long (Trưởng ban) | 2. Ông Trần Đình Trọng (Kế toán kho) | 3. Ông Nguyễn Văn Hùng (Thủ kho)</strong></div>
        <div>- Nội dung kiểm kê: <em>{{Reason}}</em></div>
    </div>
    {{ItemsTable}}
    <div style=""font-size:12px; margin-top:8px;"">
        <div>- Kết luận của Hội đồng: <em>{{Note}}</em></div>
        <div style=""font-style:italic; color:#666; margin-top:4px;"">{{NoteFooter}}</div>
    </div>
    {{Signatures}}
</div>";

                var bodyCarton = @"<div style=""font-family:'Segoe UI',Arial,sans-serif; color:#000; font-size:12px; line-height:1.3;"">
    <div style=""border:2px solid #000; padding:10px; height:100%;"">
        <div style=""display:flex; justify-content:space-between; align-items:center; border-bottom:2px solid #000; padding-bottom:6px; margin-bottom:8px;"">
            <div style=""font-weight:bold; font-size:14px;"">MINIWMS LOGISTICS</div>
            <div style=""border:1px solid #000; padding:2px 6px; font-weight:bold; font-size:11px;"">STANDARD CARTON</div>
        </div>
        <div style=""text-align:center; margin:8px 0;"">
            <div style=""font-size:11px; color:#444;"">MÃ THÙNG CARTON (CAN NO)</div>
            <div style=""font-size:18px; font-weight:bold; letter-spacing:2px;"">{{DocNo}}</div>
            <div style=""margin:5px 0;"">{{Barcode}}</div>
        </div>
        <div style=""border-top:1px solid #000; border-bottom:1px solid #000; padding:6px 0; margin:8px 0; font-size:11px;"">
            <div style=""display:flex; justify-content:space-between;""><span>Kho gửi:</span><strong>{{WarehouseName}}</strong></div>
            <div style=""display:flex; justify-content:space-between; margin-top:3px;""><span>Nơi nhận:</span><strong>{{PartnerName}}</strong></div>
            <div style=""margin-top:3px;""><span>Địa chỉ:</span> <em>{{PartnerAddress}}</em></div>
        </div>
        <div style=""background:#f0f0f0; border:1px solid #ccc; padding:6px; font-size:11px; margin-bottom:8px;"">
            <div style=""display:flex; justify-content:space-between;""><span>Mặt hàng:</span><strong>IP15-128 / MAC-M3-16</strong></div>
            <div style=""display:flex; justify-content:space-between; margin-top:2px;""><span>Số lượng kiện:</span><strong>{{TotalQty}} sản phẩm</strong></div>
            <div style=""display:flex; justify-content:space-between; margin-top:2px;""><span>Trọng lượng Gross:</span><strong>18.5 kg</strong></div>
            <div style=""display:flex; justify-content:space-between; margin-top:2px;""><span>Quy cách kích thước:</span><strong>60 x 40 x 40 cm</strong></div>
        </div>
        <div style=""text-align:center; font-size:10px; font-weight:bold; border-top:1px dashed #000; padding-top:6px;"">
            {{NoteFooter}}
        </div>
    </div>
</div>";

                var bodyBox = @"<div style=""font-family:'Segoe UI',Arial,sans-serif; color:#000; font-size:11px; line-height:1.25;"">
    <div style=""border:2px solid #000; padding:8px; height:100%;"">
        <div style=""display:flex; justify-content:space-between; border-bottom:1px solid #000; padding-bottom:4px; margin-bottom:6px;"">
            <strong style=""font-size:12px;"">WMS INNER BOX LABEL</strong>
            <span>Hộp số: <strong>{{DocNo}}</strong></span>
        </div>
        <div style=""text-align:center; margin:4px 0;"">
            {{Barcode}}
        </div>
        <div style=""font-size:10px; margin-top:4px;"">
            <div>Sản phẩm: <strong>Điện thoại iPhone 15 128GB (IP15-128)</strong></div>
            <div style=""display:flex; justify-content:space-between; margin-top:2px;"">
                <span>Số lượng đóng hộp: <strong>10 Chiếc</strong></span>
                <span>Số lô: <strong>LOT-202603</strong></span>
            </div>
            <div style=""display:flex; justify-content:space-between; margin-top:2px;"">
                <span>Đóng vào thùng: <strong>CTN-2026-00452</strong></span>
                <span>Vị trí: <strong>K-A1-02</strong></span>
            </div>
        </div>
        <div style=""text-align:center; font-size:9px; font-style:italic; margin-top:6px; border-top:1px dashed #666; padding-top:3px;"">
            {{NoteFooter}}
        </div>
    </div>
</div>";

                var bodyK80 = @"<div style=""font-family:'Courier New',Courier,monospace; width:100%; font-size:12px; line-height:1.3; color:#000;"">
    <div style=""text-align:center; margin-bottom:8px;"">
        <div style=""font-weight:bold; font-size:14px;"">{{UnitName}}</div>
        <div style=""font-size:10px;"">{{UnitAddress}}</div>
        <div style=""font-size:10px;"">Hotline: {{UnitPhone}}</div>
        <div style=""border-bottom:1px dashed #000; margin:6px 0;""></div>
        <div style=""font-weight:bold; font-size:15px;"">{{HeaderTitle}}</div>
        <div style=""font-size:11px;"">Số: {{DocNo}} | {{DocDate}}</div>
    </div>
    <div style=""font-size:11px; margin-bottom:6px;"">
        <div>KH: {{PartnerName}}</div>
        <div>Đ/C: {{PartnerAddress}}</div>
        <div>Kho xuất: {{WarehouseName}}</div>
    </div>
    <div style=""border-bottom:1px dashed #000; margin:4px 0;""></div>
    <div style=""font-size:11px;"">
        <div style=""display:flex; justify-content:space-between; font-weight:bold;"">
            <span>TÊN HÀNG</span><span>SL x GIÁ = TIỀN</span>
        </div>
        <div style=""margin-top:4px;"">
            <div>1. IP15-128 (iPhone 15)</div>
            <div style=""display:flex; justify-content:space-between; padding-left:10px;"">
                <span>50 x 19,500,000</span><strong>975,000,000</strong>
            </div>
        </div>
        <div style=""margin-top:4px;"">
            <div>2. MAC-M3-16 (MacBook Air)</div>
            <div style=""display:flex; justify-content:space-between; padding-left:10px;"">
                <span>15 x 27,000,000</span><strong>405,000,000</strong>
            </div>
        </div>
        <div style=""margin-top:4px;"">
            <div>3. WATCH-S9-41 (Apple Watch)</div>
            <div style=""display:flex; justify-content:space-between; padding-left:10px;"">
                <span>10 x 7,000,000</span><strong>70,000,000</strong>
            </div>
        </div>
    </div>
    <div style=""border-bottom:1px dashed #000; margin:6px 0;""></div>
    <div style=""display:flex; justify-content:space-between; font-weight:bold; font-size:13px;"">
        <span>TỔNG CỘNG:</span><span>{{TotalAmount}}</span>
    </div>
    <div style=""display:flex; justify-content:space-between; font-size:11px; margin-top:2px;"">
        <span>Tổng sản lượng:</span><strong>{{TotalQty}} sp</strong>
    </div>
    <div style=""border-bottom:1px dashed #000; margin:6px 0;""></div>
    <div style=""text-align:center; margin:8px 0;"">
        {{Barcode}}
    </div>
    <div style=""text-align:center; font-size:10px; margin-top:6px;"">
        {{NoteFooter}}
    </div>
</div>";

                db.TempPrints.AddRange(
                    new TempPrint
                    {
                        Code = "PN_TT200_A4",
                        Name = "Phiếu nhập kho tiêu chuẩn Bộ Tài chính (A4)",
                        TypeCode = "IN",
                        PaperSize = "A4_Portrait",
                        UnitName = "CÔNG TY CỔ PHẦN LOGISTICS MINIWMS",
                        UnitAddress = "Lô CN-08, KCN Bắc Thăng Long, Đông Anh, TP. Hà Nội",
                        UnitPhone = "024-3795-8888",
                        UnitEmail = "kho.tong@miniwms.vn",
                        HeaderTitle = "PHIẾU NHẬP KHO",
                        SubTitle = "Mẫu số: 01 - VT (Ban hành theo TT số 200/2014/TT-BTC ngày 22/12/2014 của BTC)",
                        BodyTemplateHtml = bodyInA4,
                        NoteFooter = "Phiếu nhập kho lập 3 liên: Liên 1 lưu phòng KT, Liên 2 Thủ kho giữ ghi thẻ kho, Liên 3 giao người giao hàng.",
                        IsDefault = true,
                        IsActive = true,
                        Remark = "Mẫu in chuẩn Thông tư 200 áp dụng cho toàn bộ giao dịch nhập mua và nhập thành phẩm SX"
                    },
                    new TempPrint
                    {
                        Code = "PX_TT200_A4",
                        Name = "Phiếu xuất kho kiêm biên bản giao nhận (A4)",
                        TypeCode = "OUT",
                        PaperSize = "A4_Portrait",
                        UnitName = "CÔNG TY CỔ PHẦN LOGISTICS MINIWMS",
                        UnitAddress = "Lô CN-08, KCN Bắc Thăng Long, Đông Anh, TP. Hà Nội",
                        UnitPhone = "024-3795-8888",
                        UnitEmail = "kho.tong@miniwms.vn",
                        HeaderTitle = "PHIẾU XUẤT KHO",
                        SubTitle = "Mẫu số: 02 - VT (Ban hành theo TT số 200/2014/TT-BTC ngày 22/12/2014 của BTC)",
                        BodyTemplateHtml = bodyOutA4,
                        NoteFooter = "Hàng hóa đã được kiểm tra đủ số lượng, nguyên niêm phong, đúng quy cách xuất kho.",
                        IsDefault = true,
                        IsActive = true,
                        Remark = "Mẫu in xuất kho kiêm biên bản bàn giao vận chuyển cho khách hàng & đại lý"
                    },
                    new TempPrint
                    {
                        Code = "PC_NOIBO_A4",
                        Name = "Phiếu xuất điều chuyển kho nội bộ (A4)",
                        TypeCode = "MOVE",
                        PaperSize = "A4_Portrait",
                        UnitName = "CÔNG TY CỔ PHẦN LOGISTICS MINIWMS",
                        UnitAddress = "Lô CN-08, KCN Bắc Thăng Long, Đông Anh, TP. Hà Nội",
                        UnitPhone = "024-3795-8888",
                        HeaderTitle = "PHIẾU ĐIỀU CHUYỂN KHO NỘI BỘ",
                        SubTitle = "Kiêm lệnh điều động & vận chuyển hàng hóa giữa các kho / chi nhánh",
                        BodyTemplateHtml = bodyMoveA4,
                        NoteFooter = "Lệnh điều chuyển có giá trị trong vòng 48 giờ kể từ thời điểm phát hành.",
                        IsDefault = true,
                        IsActive = true,
                        Remark = "Mẫu in luân chuyển kho nội bộ, đối soát kho xuất và kho tiếp nhận"
                    },
                    new TempPrint
                    {
                        Code = "BB_KIEMKE_A4",
                        Name = "Biên bản kiểm kê vật tư, hàng hóa định kỳ (A4)",
                        TypeCode = "AUDIT",
                        PaperSize = "A4_Landscape",
                        UnitName = "CÔNG TY CỔ PHẦN LOGISTICS MINIWMS",
                        UnitAddress = "Lô CN-08, KCN Bắc Thăng Long, Đông Anh, TP. Hà Nội",
                        HeaderTitle = "BIÊN BẢN KIỂM KÊ VẬT TƯ, CÔNG CỤ, HÀNG HÓA",
                        SubTitle = "Thời điểm kiểm kê: 24h00 cuối kỳ đối soát kho",
                        BodyTemplateHtml = bodyAuditA4,
                        NoteFooter = "Hội đồng kiểm kê chịu trách nhiệm trước Ban Giám đốc về tính chính xác của số liệu kiểm kê thực tế.",
                        IsDefault = true,
                        IsActive = true,
                        Remark = "Mẫu in biên bản hội đồng kiểm kê kho theo Thông tư 200/2014/TT-BTC"
                    },
                    new TempPrint
                    {
                        Code = "TEM_CARTON_100X150",
                        Name = "Tem nhãn vận chuyển kiện thùng WMS (100x150 mm)",
                        TypeCode = "CARTON",
                        PaperSize = "Label_100x150",
                        UnitName = "MINIWMS SMART LOGISTICS",
                        HeaderTitle = "CARTON SHIPPING LABEL",
                        SubTitle = "WMS Master Shipping Unit (Standard TT-100x150)",
                        BodyTemplateHtml = bodyCarton,
                        NoteFooter = "FRAGILE - HANDLE WITH CARE - HÀNG DỄ VỠ XIN NHẸ TAY",
                        IsDefault = true,
                        IsActive = true,
                        Remark = "Tem dán ngoài thùng carton vận chuyển liên tỉnh và lưu kho pallet"
                    },
                    new TempPrint
                    {
                        Code = "TEM_BOX_100X75",
                        Name = "Tem nhãn định danh hộp bao bì Inner Box (100x75 mm)",
                        TypeCode = "BOX",
                        PaperSize = "Label_100x75",
                        UnitName = "MINIWMS SMART LOGISTICS",
                        HeaderTitle = "INNER BOX PACKAGING LABEL",
                        SubTitle = "Định danh hộp đóng gói & Quản lý vị trí lưu trữ",
                        BodyTemplateHtml = bodyBox,
                        NoteFooter = "CHECKED BY QC / WMS INVENTORY CONTROL",
                        IsDefault = true,
                        IsActive = true,
                        Remark = "Tem dán hộp đóng gói sản phẩm bên trong thùng master"
                    },
                    new TempPrint
                    {
                        Code = "PX_NHIET_K80",
                        Name = "Phiếu xuất giao hàng in nhiệt POS K80 (80mm)",
                        TypeCode = "K80",
                        PaperSize = "Thermal_K80",
                        UnitName = "HỆ THỐNG KHO VẬN MINIWMS",
                        UnitAddress = "Kho Tổng Hà Nội - KCN Bắc Thăng Long",
                        UnitPhone = "024-3795-8888",
                        HeaderTitle = "PHIẾU XUẤT GIAO HÀNG",
                        SubTitle = "Dành cho máy in bill / in nhiệt khổ 80mm",
                        BodyTemplateHtml = bodyK80,
                        NoteFooter = "Cảm ơn Quý khách! Vui lòng kiểm tra kỹ hàng trước khi nhận.",
                        IsDefault = true,
                        IsActive = true,
                        Remark = "Mẫu in nhiệt cuộn 80mm cho các lệnh xuất kho giao nhanh bằng xe tải/xe máy"
                    }
                );
                await db.SaveChangesAsync();
            }
        }

        // Seed Danh mục Loại tiền & Tỷ giá quy đổi ngoại tệ kho (port từ OS_PrdCenter_Mst_CurrencyEx Skycic)
        if (!await db.CurrencyExchanges.AnyAsync())
        {
            var now = DateTime.Now;
            db.CurrencyExchanges.AddRange(
                new CurrencyExchange
                {
                    Code = "VND",
                    Name = "Đồng Việt Nam",
                    BaseCurrencyCode = "VND",
                    BuyRate = 1.0000m,
                    SellRate = 1.0000m,
                    InterExRate = 1.0000m,
                    InterExSource = "Ngân hàng Nhà nước Việt Nam",
                    Symbol = "₫",
                    IsBase = true,
                    IsActive = true,
                    Remark = "Đồng tiền kế toán & định giá tồn kho cơ sở của hệ thống MiniWMS",
                    UpdatedTime = now
                },
                new CurrencyExchange
                {
                    Code = "USD",
                    Name = "Đô la Mỹ",
                    BaseCurrencyCode = "VND",
                    BuyRate = 25420.0000m,
                    SellRate = 25480.0000m,
                    InterExRate = 25450.0000m,
                    InterExSource = "Vietcombank Hội sở",
                    Symbol = "$",
                    IsBase = false,
                    IsActive = true,
                    Remark = "Ngoại tệ thanh toán chính cho các lô nhập khẩu phụ tùng & nguyên phụ liệu may",
                    UpdatedTime = now
                },
                new CurrencyExchange
                {
                    Code = "EUR",
                    Name = "Đồng tiền chung Châu Âu",
                    BaseCurrencyCode = "VND",
                    BuyRate = 27530.0000m,
                    SellRate = 28980.0000m,
                    InterExRate = 28250.0000m,
                    InterExSource = "Vietcombank Hội sở",
                    Symbol = "€",
                    IsBase = false,
                    IsActive = true,
                    Remark = "Ngoại tệ nhập khẩu máy may công nghiệp và thiết bị tự động hóa từ Đức/Ý",
                    UpdatedTime = now.AddHours(-1)
                },
                new CurrencyExchange
                {
                    Code = "JPY",
                    Name = "Yên Nhật",
                    BaseCurrencyCode = "VND",
                    BuyRate = 167.2000m,
                    SellRate = 177.1000m,
                    InterExRate = 172.1500m,
                    InterExSource = "Vietcombank Hội sở",
                    Symbol = "¥",
                    IsBase = false,
                    IsActive = true,
                    Remark = "Ngoại tệ đối soát nhập linh kiện chính xác và khóa kéo kỹ thuật cao Nhật Bản",
                    UpdatedTime = now.AddHours(-2)
                },
                new CurrencyExchange
                {
                    Code = "CNY",
                    Name = "Nhân dân tệ",
                    BaseCurrencyCode = "VND",
                    BuyRate = 3490.0000m,
                    SellRate = 3640.0000m,
                    InterExRate = 3565.0000m,
                    InterExSource = "Vietcombank Hội sở",
                    Symbol = "¥",
                    IsBase = false,
                    IsActive = true,
                    Remark = "Ngoại tệ nhập khẩu vải tấm cotton cuộn, chỉ may công nghiệp và phụ liệu bao bì",
                    UpdatedTime = now.AddHours(-3)
                },
                new CurrencyExchange
                {
                    Code = "GBP",
                    Name = "Bảng Anh",
                    BaseCurrencyCode = "VND",
                    BuyRate = 32650.0000m,
                    SellRate = 34040.0000m,
                    InterExRate = 33340.0000m,
                    InterExSource = "Vietcombank Hội sở",
                    Symbol = "£",
                    IsBase = false,
                    IsActive = true,
                    Remark = "Hợp đồng gia công OEM thời trang xuất khẩu sang Vương quốc Anh",
                    UpdatedTime = now.AddHours(-4)
                },
                new CurrencyExchange
                {
                    Code = "KRW",
                    Name = "Won Hàn Quốc",
                    BaseCurrencyCode = "VND",
                    BuyRate = 18.2500m,
                    SellRate = 19.9500m,
                    InterExRate = 19.1000m,
                    InterExSource = "Vietcombank Hội sở",
                    Symbol = "₩",
                    IsBase = false,
                    IsActive = true,
                    Remark = "Ngoại tệ nhập khẩu phụ kiện da may mặc thời trang Hàn Quốc",
                    UpdatedTime = now.AddHours(-5)
                },
                new CurrencyExchange
                {
                    Code = "SGD",
                    Name = "Đô la Singapore",
                    BaseCurrencyCode = "VND",
                    BuyRate = 18850.0000m,
                    SellRate = 19650.0000m,
                    InterExRate = 19250.0000m,
                    InterExSource = "Vietcombank Hội sở",
                    Symbol = "S$",
                    IsBase = false,
                    IsActive = true,
                    Remark = "Thanh toán cước vận tải biển trung chuyển logistics khu vực Đông Nam Á",
                    UpdatedTime = now.AddHours(-6)
                },
                new CurrencyExchange
                {
                    Code = "THB",
                    Name = "Baht Thái Lan",
                    BaseCurrencyCode = "VND",
                    BuyRate = 710.0000m,
                    SellRate = 790.0000m,
                    InterExRate = 750.0000m,
                    InterExSource = "Vietcombank Hội sở",
                    Symbol = "฿",
                    IsBase = false,
                    IsActive = true,
                    Remark = "Ngoại tệ nhập hạt nhựa PE và bao bì thùng carton từ Thái Lan",
                    UpdatedTime = now.AddHours(-8)
                }
            );
            await db.SaveChangesAsync();
        }

        // ==================== SEED DANH MỤC QUY CÁCH SẢN PHẨM (OS_PrdCenter_Mst_Spec / Mst_Spec Skycic) ====================
        if (!await db.ProductSpecs.AnyAsync())
        {
            var now = DateTime.Now;
            db.ProductSpecs.AddRange(
                new ProductSpec
                {
                    Code = "SPC-AO-SM-TRANG-L",
                    Name = "Áo sơ mi Slimfit Trắng - Size L",
                    SpecDesc = "Dáng ôm vừa Slimfit, Cổ áo bẻ cứng cáp 3.8cm, Tay dài măng sét kép, Vòng ngực 104cm, Vòng eo 96cm, Dài áo 74cm",
                    ModelCode = "MD-M10-SLIM",
                    SpecType1 = "Tiêu chuẩn",
                    Color = "Trắng Tinh (White)",
                    StandardUnitCode = "cái",
                    FlagHasSerial = false,
                    FlagHasLOT = true,
                    IsActive = true,
                    Remark = "Sản phẩm chủ lực bán buôn & hệ thống showroom, bảo quản nơi khô ráo, nhiệt độ < 30°C",
                    CreatedAt = now.AddDays(-60)
                },
                new ProductSpec
                {
                    Code = "SPC-AO-SM-TRANG-M",
                    Name = "Áo sơ mi Slimfit Trắng - Size M",
                    SpecDesc = "Dáng ôm vừa Slimfit, Cổ áo bẻ 3.8cm, Tay dài, Vòng ngực 100cm, Vòng eo 92cm, Dài áo 72cm",
                    ModelCode = "MD-M10-SLIM",
                    SpecType1 = "Tiêu chuẩn",
                    Color = "Trắng Tinh (White)",
                    StandardUnitCode = "cái",
                    FlagHasSerial = false,
                    FlagHasLOT = true,
                    IsActive = true,
                    Remark = "Quy cách phổ biến nhất thị trường miền Bắc & Trung",
                    CreatedAt = now.AddDays(-55)
                },
                new ProductSpec
                {
                    Code = "SPC-AO-SM-XANH-L",
                    Name = "Áo sơ mi Slimfit Xanh Pastel - Size L",
                    SpecDesc = "Dáng ôm vừa Slimfit, Cổ áo bẻ, Vải Cotton dệt vân chìm chống nhăn, Vòng ngực 104cm, Vòng eo 96cm",
                    ModelCode = "MD-M10-SLIM",
                    SpecType1 = "Cao cấp",
                    Color = "Xanh Pastel (Light Blue)",
                    StandardUnitCode = "cái",
                    FlagHasSerial = false,
                    FlagHasLOT = true,
                    IsActive = true,
                    Remark = "Dòng công sở cao cấp mùa hè",
                    CreatedAt = now.AddDays(-50)
                },
                new ProductSpec
                {
                    Code = "SPC-QUAN-JN-DEN-32",
                    Name = "Quần Jeans Nam Co Giãn Đen - Size 32",
                    SpecDesc = "Form ống đứng Regular Straight, Lưng vừa, Vòng bụng 82cm, Dài quần 102cm, Ống rộng 18.5cm, Chỉ may đúp chịu lực",
                    ModelCode = "MD-LV-501",
                    SpecType1 = "Cao cấp",
                    Color = "Đen Nhám (Matt Black)",
                    StandardUnitCode = "chiếc",
                    FlagHasSerial = true,
                    FlagHasLOT = true,
                    IsActive = true,
                    Remark = "Mặt hàng kiểm soát nghiêm ngặt theo mã Serial/Barcode cá thể hóa chống giả mạo",
                    CreatedAt = now.AddDays(-45)
                },
                new ProductSpec
                {
                    Code = "SPC-QUAN-JN-XANH-31",
                    Name = "Quần Jeans Nam Cổ Điển Xanh Denim - Size 31",
                    SpecDesc = "Form suông ống thẳng cổ điển, Lưng vừa, Vòng bụng 80cm, Dài quần 100cm, Xử lý wash sờn phong cách",
                    ModelCode = "MD-LV-501",
                    SpecType1 = "Tiêu chuẩn",
                    Color = "Xanh Denim (Classic Blue)",
                    StandardUnitCode = "chiếc",
                    FlagHasSerial = true,
                    FlagHasLOT = false,
                    IsActive = true,
                    Remark = "Dán tem bảo mật WMS và nhãn QR code theo dõi xuất xưởng",
                    CreatedAt = now.AddDays(-40)
                },
                new ProductSpec
                {
                    Code = "SPC-AO-POLO-TRANG-L",
                    Name = "Áo thun Polo Nam Thể Thao Trắng - Size L",
                    SpecDesc = "Vải Pique gai cá sấu thoáng khí, Kháng khuẩn khử mùi, Bo cổ dệt sọc thể thao, Dài áo 71cm, Ngực 102cm",
                    ModelCode = "MD-VT-POLO",
                    SpecType1 = "Tiêu chuẩn",
                    Color = "Trắng Phối Đỏ (White/Red)",
                    StandardUnitCode = "cái",
                    FlagHasSerial = false,
                    FlagHasLOT = false,
                    IsActive = true,
                    Remark = "Sản phẩm sự kiện & quà tặng công đoàn đại lý",
                    CreatedAt = now.AddDays(-35)
                },
                new ProductSpec
                {
                    Code = "SPC-GIAT-XA-CAN-3L",
                    Name = "Nước Giặt Xả Đậm Đặc Can 3.5 Lít",
                    SpecDesc = "Dung tích thực 3500ml, Can nhựa HDPE chịu va đập kèm nắp định lượng 60ml, Hương hoa thiên nhiên ngát hương 7 ngày",
                    ModelCode = "MD-AP-VEST",
                    SpecType1 = "Công nghiệp",
                    Color = "Tím Oải Hương (Lavender)",
                    StandardUnitCode = "can",
                    FlagHasSerial = false,
                    FlagHasLOT = true,
                    IsActive = true,
                    Remark = "Bắt buộc theo dõi Date sản xuất & hạn dùng 36 tháng theo chuẩn FEFO kho hóa chất tiêu dùng",
                    CreatedAt = now.AddDays(-30)
                },
                new ProductSpec
                {
                    Code = "SPC-AO-KHOAC-DA-XL",
                    Name = "Áo Khoác Da Bò Nam Bomber - Size XL",
                    SpecDesc = "Da bò thuộc 100% nhập khẩu Ý, Khóa kéo đồng YKK cao cấp, Lớp lót lụa habutai giữ ấm, Ngực 112cm, Dài 68cm",
                    ModelCode = "MD-LV-501",
                    SpecType1 = "Xuất khẩu",
                    Color = "Nâu Bò Cổ Điển (Vintage Brown)",
                    StandardUnitCode = "cái",
                    FlagHasSerial = true,
                    FlagHasLOT = true,
                    IsActive = true,
                    Remark = "Sản phẩm giá trị cao, bảo quản phòng lạnh độ ẩm < 55%, quét serial từng chiếc",
                    CreatedAt = now.AddDays(-25)
                }
            );
            await db.SaveChangesAsync();

            // Cập nhật SpecCode cho các sản phẩm hiện có
            var p1 = await db.Products.FirstOrDefaultAsync(p => p.Code == "SP001");
            if (p1 != null) { p1.SpecCode = "SPC-AO-SM-TRANG-L"; p1.ModelCode = "MD-M10-SLIM"; }
            var p2 = await db.Products.FirstOrDefaultAsync(p => p.Code == "SP002");
            if (p2 != null) { p2.SpecCode = "SPC-QUAN-JN-DEN-32"; p2.ModelCode = "MD-LV-501"; }
            var p3 = await db.Products.FirstOrDefaultAsync(p => p.Code == "SP003");
            if (p3 != null) { p3.SpecCode = "SPC-AO-POLO-TRANG-L"; p3.ModelCode = "MD-VT-POLO"; }
            await db.SaveChangesAsync();
        }

        // ==================== SEED QUY CÁCH ĐÓNG GÓI THEO ĐƠN VỊ TÍNH (OS_PrdCenter_Mst_SpecUnit / Mst_SpecUnit Skycic) ====================
        if (!await db.SpecUnits.AnyAsync())
        {
            var now = DateTime.Now;
            db.SpecUnits.AddRange(
                new SpecUnit
                {
                    SpecCode = "SPC-AO-SM-TRANG-L",
                    UnitCode = "cái",
                    StandardUnitCode = "cái",
                    SpecUnitDesc = "Đơn vị bán lẻ 1 áo sơ mi Slimfit Trắng L, gấp gói túi PE trong suốt",
                    Qty = 1m,
                    Length = 35m, Width = 25m, Height = 4m,
                    Volume = 0.0035m, Weight = 0.35m,
                    IsActive = true,
                    Remark = "Đơn vị cơ sở để quy đổi lên hộp/thùng",
                    CreatedAt = now.AddDays(-60)
                },
                new SpecUnit
                {
                    SpecCode = "SPC-AO-SM-TRANG-L",
                    UnitCode = "hộp",
                    StandardUnitCode = "cái",
                    SpecUnitDesc = "Hộp combo 5 áo sơ mi, hộp carton cứng in logo, đóng đai nhựa",
                    Qty = 5m,
                    Length = 40m, Width = 30m, Height = 12m,
                    Volume = 0.0144m, Weight = 1.9m,
                    IsActive = true,
                    Remark = "Quy cách đóng hộp quà tặng doanh nghiệp",
                    CreatedAt = now.AddDays(-58)
                },
                new SpecUnit
                {
                    SpecCode = "SPC-AO-SM-TRANG-L",
                    UnitCode = "thùng",
                    StandardUnitCode = "cái",
                    SpecUnitDesc = "Thùng 12 hộp xếp 2 lớp, đóng đai nhựa, dán tem QR WMS",
                    Qty = 60m,
                    Length = 62m, Width = 42m, Height = 30m,
                    Volume = 0.0781m, Weight = 24.5m,
                    IsActive = true,
                    Remark = "Quy cách xuất kho theo thùng cho đại lý cấp 1",
                    CreatedAt = now.AddDays(-55)
                },
                new SpecUnit
                {
                    SpecCode = "SPC-QUAN-JN-DEN-32",
                    UnitCode = "cái",
                    StandardUnitCode = "chiếc",
                    SpecUnitDesc = "Đơn vị bán lẻ 1 quần Jeans Nam Đen size 32, gấp gói túi PE",
                    Qty = 1m,
                    Length = 38m, Width = 28m, Height = 5m,
                    Volume = 0.0053m, Weight = 0.65m,
                    IsActive = true,
                    Remark = "Mặt hàng quản lý Serial từng chiếc",
                    CreatedAt = now.AddDays(-45)
                },
                new SpecUnit
                {
                    SpecCode = "SPC-QUAN-JN-DEN-32",
                    UnitCode = "thùng",
                    StandardUnitCode = "chiếc",
                    SpecUnitDesc = "Thùng 20 quần Jeans xếp phẳng, lót giấy chống ẩm, đóng đai",
                    Qty = 20m,
                    Length = 60m, Width = 40m, Height = 25m,
                    Volume = 0.06m, Weight = 13.5m,
                    IsActive = true,
                    Remark = "Quy cách xuất kho theo thùng",
                    CreatedAt = now.AddDays(-42)
                },
                new SpecUnit
                {
                    SpecCode = "SPC-GIAT-XA-CAN-3L",
                    UnitCode = "can",
                    StandardUnitCode = "can",
                    SpecUnitDesc = "Can nhựa HDPE 3.5 lít kèm nắp định lượng 60ml",
                    Qty = 1m,
                    Length = 18m, Width = 12m, Height = 28m,
                    Volume = 0.006m, Weight = 3.7m,
                    IsActive = true,
                    Remark = "Đơn vị cơ sở, theo dõi Date sản xuất & HSD FEFO",
                    CreatedAt = now.AddDays(-30)
                },
                new SpecUnit
                {
                    SpecCode = "SPC-GIAT-XA-CAN-3L",
                    UnitCode = "thùng",
                    StandardUnitCode = "can",
                    SpecUnitDesc = "Thùng 6 can xếp 2 lớp, lót xốp chống va đập, đóng đai nhựa",
                    Qty = 6m,
                    Length = 40m, Width = 28m, Height = 30m,
                    Volume = 0.0336m, Weight = 22.8m,
                    IsActive = true,
                    Remark = "Quy cách xuất kho theo thùng cho kênh bán buôn",
                    CreatedAt = now.AddDays(-28)
                },
                new SpecUnit
                {
                    SpecCode = "SPC-AO-KHOAC-DA-XL",
                    UnitCode = "cái",
                    StandardUnitCode = "cái",
                    SpecUnitDesc = "Đơn vị bán lẻ 1 áo khoác da bò, bọc túi vải không dệt + hộp cứng",
                    Qty = 1m,
                    Length = 45m, Width = 35m, Height = 8m,
                    Volume = 0.0126m, Weight = 1.8m,
                    IsActive = true,
                    Remark = "Sản phẩm giá trị cao, quét serial từng chiếc",
                    CreatedAt = now.AddDays(-25)
                }
            );
            await db.SaveChangesAsync();
        }

        // ==================== SEED BẢNG GIÁ QUY CÁCH SẢN PHẨM (OS_PrdCenter_Mst_SpecPrice / Mst_SpecPrice Skycic) ====================
        if (!await db.SpecPrices.AnyAsync())
        {
            var now = DateTime.Now;
            db.SpecPrices.AddRange(
                new SpecPrice
                {
                    SpecCode = "SPC-AO-SM-TRANG-L",
                    UnitCode = "cái",
                    BuyPrice = 180000m,
                    SellPrice = 280000m,
                    CurrencyCode = "VND",
                    VATRateCode = "VAT10",
                    DiscountVND = 15000m,
                    EffectDTimeStart = now.AddMonths(-3),
                    IsActive = true,
                    Remark = "Giá bán buôn đại lý cấp 1 toàn quốc - Dáng Slimfit Trắng L",
                    CreatedAt = now.AddMonths(-3)
                },
                new SpecPrice
                {
                    SpecCode = "SPC-AO-SM-TRANG-L",
                    UnitCode = "hộp",
                    BuyPrice = 880000m,
                    SellPrice = 1350000m,
                    CurrencyCode = "VND",
                    VATRateCode = "VAT10",
                    DiscountVND = 80000m,
                    EffectDTimeStart = now.AddMonths(-3),
                    IsActive = true,
                    Remark = "Quy cách đóng hộp combo 5 áo công sở cao cấp - Quà tặng doanh nghiệp",
                    CreatedAt = now.AddMonths(-3)
                },
                new SpecPrice
                {
                    SpecCode = "SPC-AO-SM-TRANG-M",
                    UnitCode = "cái",
                    BuyPrice = 175000m,
                    SellPrice = 275000m,
                    CurrencyCode = "VND",
                    VATRateCode = "VAT10",
                    DiscountVND = 15000m,
                    EffectDTimeStart = now.AddMonths(-2),
                    IsActive = true,
                    Remark = "Size M bán chạy kênh showroom và sàn TMĐT",
                    CreatedAt = now.AddMonths(-2)
                },
                new SpecPrice
                {
                    SpecCode = "SPC-AO-SM-XANH-L",
                    UnitCode = "cái",
                    BuyPrice = 220000m,
                    SellPrice = 340000m,
                    CurrencyCode = "VND",
                    VATRateCode = "VAT10",
                    DiscountVND = 20000m,
                    EffectDTimeStart = now.AddMonths(-2),
                    IsActive = true,
                    Remark = "Dòng cao cấp vải Bamboo chống nhăn sợi tre tự nhiên",
                    CreatedAt = now.AddMonths(-2)
                },
                new SpecPrice
                {
                    SpecCode = "SPC-QUAN-JN-DEN-32",
                    UnitCode = "cái",
                    BuyPrice = 350000m,
                    SellPrice = 550000m,
                    CurrencyCode = "VND",
                    VATRateCode = "VAT8",
                    DiscountVND = 30000m,
                    EffectDTimeStart = now.AddMonths(-3),
                    IsActive = true,
                    Remark = "Jeans nam cao cấp dáng chuẩn Levi xuất khẩu",
                    CreatedAt = now.AddMonths(-3)
                },
                new SpecPrice
                {
                    SpecCode = "SPC-QUAN-JN-DEN-32",
                    UnitCode = "thùng",
                    BuyPrice = 6800000m,
                    SellPrice = 10500000m,
                    CurrencyCode = "VND",
                    VATRateCode = "VAT8",
                    DiscountVND = 600000m,
                    EffectDTimeStart = now.AddMonths(-3),
                    IsActive = true,
                    Remark = "Thùng carton Master đóng 20 chiếc giao đại lý tỉnh",
                    CreatedAt = now.AddMonths(-3)
                },
                new SpecPrice
                {
                    SpecCode = "SPC-AO-POLO-TRANG-L",
                    UnitCode = "cái",
                    BuyPrice = 150000m,
                    SellPrice = 240000m,
                    CurrencyCode = "VND",
                    VATRateCode = "VAT10",
                    DiscountVND = 10000m,
                    EffectDTimeStart = now.AddMonths(-1),
                    IsActive = true,
                    Remark = "Áo Polo thể thao dệt tổ ong co giãn 4 chiều",
                    CreatedAt = now.AddMonths(-1)
                },
                new SpecPrice
                {
                    SpecCode = "SPC-AO-KHOAC-GIO-XL",
                    UnitCode = "cái",
                    BuyPrice = 420000m,
                    SellPrice = 690000m,
                    CurrencyCode = "VND",
                    VATRateCode = "VAT10",
                    DiscountVND = 40000m,
                    EffectDTimeStart = now.AddMonths(-2),
                    IsActive = true,
                    Remark = "Áo khoác gió 2 lớp trượt nước chống gió lạnh mùa đông",
                    CreatedAt = now.AddMonths(-2)
                },
                new SpecPrice
                {
                    SpecCode = "SPC-AO-KHOAC-GIO-XL",
                    UnitCode = "cái (USD)",
                    BuyPrice = 18m,
                    SellPrice = 30m,
                    CurrencyCode = "USD",
                    VATRateCode = "VAT0",
                    DiscountVND = 1.5m,
                    EffectDTimeStart = now.AddMonths(-2),
                    IsActive = true,
                    Remark = "Bảng giá FOB xuất khẩu đối tác thị trường Bắc Mỹ (USD)",
                    CreatedAt = now.AddMonths(-2)
                },
                new SpecPrice
                {
                    SpecCode = "SPC-VAY-DA-HOI-DEN-S",
                    UnitCode = "bộ",
                    BuyPrice = 750000m,
                    SellPrice = 1280000m,
                    CurrencyCode = "VND",
                    VATRateCode = "VAT10",
                    DiscountVND = 80000m,
                    EffectDTimeStart = now.AddMonths(-1),
                    IsActive = true,
                    Remark = "Đầm dạ hội đính pha lê cao cấp, quản lý serial từng bộ",
                    CreatedAt = now.AddMonths(-1)
                },
                new SpecPrice
                {
                    SpecCode = "SPC-VAY-DA-HOI-DEN-S",
                    UnitCode = "bộ (USD)",
                    BuyPrice = 32m,
                    SellPrice = 55m,
                    CurrencyCode = "USD",
                    VATRateCode = "VAT0",
                    DiscountVND = 2.5m,
                    EffectDTimeStart = now.AddMonths(-1),
                    IsActive = true,
                    Remark = "Bảng giá xuất khẩu đối tác chuỗi thời trang quốc tế (USD)",
                    CreatedAt = now.AddMonths(-1)
                }
            );
            await db.SaveChangesAsync();
        }

        // ==================== SEED DANH MỤC THUẾ SUẤT VAT HÀNG HÓA (OS_PrdCenter_Mst_VATRate / Mst_VATRate Skycic) ====================
        if (!await db.VATRates.AnyAsync())
        {
            var now = DateTime.Now;
            db.VATRates.AddRange(
                new VATRate
                {
                    VATRateCode = "VAT0",
                    Rate = 0m,
                    VATDesc = "Thuế suất 0% (Hàng xuất khẩu, vận tải quốc tế, cung cấp dịch vụ cho khu phi thuế quan)",
                    IsActive = true,
                    Remark = "Căn cứ Điều 9 Thông tư 219/2013/TT-BTC về thuế GTGT xuất khẩu",
                    CreatedAt = now.AddMonths(-6)
                },
                new VATRate
                {
                    VATRateCode = "VAT5",
                    Rate = 5m,
                    VATDesc = "Thuế suất 5% (Nông lâm thủy hải sản chưa chế biến, thiết bị y tế, thuốc tân dược, đồ dùng dạy học)",
                    IsActive = true,
                    Remark = "Căn cứ Điều 10 Thông tư 219/2013/TT-BTC về nhóm mặt hàng thiết yếu",
                    CreatedAt = now.AddMonths(-6)
                },
                new VATRate
                {
                    VATRateCode = "VAT8",
                    Rate = 8m,
                    VATDesc = "Thuế suất ưu đãi 8% (Chính sách giảm thuế GTGT 2% hỗ trợ phục hồi sản xuất kinh doanh)",
                    IsActive = true,
                    Remark = "Căn cứ Nghị quyết 110/2023/QH15 & Nghị định 94/2023/NĐ-CP (áp dụng giảm 2% từ 10% xuống 8%)",
                    CreatedAt = now.AddMonths(-4)
                },
                new VATRate
                {
                    VATRateCode = "VAT10",
                    Rate = 10m,
                    VATDesc = "Thuế suất chuẩn 10% (Hàng hóa tiêu chuẩn, thời trang may mặc, thiết bị điện tử, phụ tùng linh kiện)",
                    IsActive = true,
                    Remark = "Thuế suất phổ thông tiêu chuẩn theo Điều 11 Thông tư 219/2013/TT-BTC",
                    CreatedAt = now.AddMonths(-6)
                },
                new VATRate
                {
                    VATRateCode = "KCT",
                    Rate = 0m,
                    VATDesc = "Không chịu thuế GTGT (Sản phẩm giống cây trồng vật nuôi, máy móc thiết bị chuyên dùng phục vụ nông nghiệp)",
                    IsActive = true,
                    Remark = "Đối tượng không thuộc diện chịu thuế GTGT theo Điều 4 Thông tư 219/2013/TT-BTC",
                    CreatedAt = now.AddMonths(-6)
                }
            );
            await db.SaveChangesAsync();
        }
    }

    private static async Task MigratePostgresAsync(AppDbContext db)
    {
        if (!db.Database.IsNpgsql()) return;
        var def = TenantContext.DefaultOrgId;
        var tables = new[] { "Warehouses", "Products", "Docs", "DocLines", "Audits", "AuditLines", "MoveOrders", "MoveOrderLines", "ReturnToSuppliers", "ReturnToSupplierLines", "CustomerReturns", "CustomerReturnLines", "StockLots", "StockSerials", "InventoryBlocks", "CostPriceHists", "PeriodClosings", "PeriodClosingLines", "InventoryCartons", "InventoryBoxes", "InventoryInFGs", "InventoryInFGLines", "InventoryInFGSerials", "InventoryOutFGs", "InventoryOutFGLines", "InventoryOutFGSerials", "Suppliers", "Customers", "PartTypes", "Brands", "PartUnits", "PartMaterialTypes", "ProductModels", "InventoryTypes", "InventoryLevelTypes", "InventoryInTypes", "InventoryOutTypes", "UserMapInventories", "ProductGroups", "Areas", "CustomerGroups", "Departments", "CustomerSources", "MoveOrdTypes", "Dealers", "TempPrintTypes", "TempPrints", "CurrencyExchanges", "ProductSpecs", "SpecUnits", "SpecPrices", "VATRates", "PartColors", "PartColorMaps", "InventorySecrets", "SecretLicenses", "Provinces", "Districts", "Agents", "PurchaseReceipts", "PurchaseReceiptLines" };
        var sql = new List<string>
        {
            "CREATE TABLE IF NOT EXISTS miniwms.\"Orgs\" (\"Id\" uuid PRIMARY KEY, \"Name\" text NOT NULL DEFAULT '', \"ApiKey\" text NOT NULL DEFAULT '', \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Orgs_ApiKey\" ON miniwms.\"Orgs\" (\"ApiKey\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"Suppliers\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"ContactName\" text NULL, \"Phone\" text NULL, \"Email\" text NULL, \"Address\" text NULL, \"TaxCode\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Note\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Suppliers_OrgId_Code\" ON miniwms.\"Suppliers\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"Customers\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"CustomerType\" text NOT NULL DEFAULT 'Đại lý phân phối', \"ContactName\" text NULL, \"ContactPhone\" text NULL, \"Phone\" text NULL, \"Email\" text NULL, \"Address\" text NULL, \"Province\" text NULL, \"TaxCode\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Note\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Customers_OrgId_Code\" ON miniwms.\"Customers\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"PartTypes\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_PartTypes_OrgId_Code\" ON miniwms.\"PartTypes\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"Brands\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"Origin\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Brands_OrgId_Code\" ON miniwms.\"Brands\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"PartUnits\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"IsStandard\" boolean NOT NULL DEFAULT true, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_PartUnits_OrgId_Code\" ON miniwms.\"PartUnits\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"PartMaterialTypes\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_PartMaterialTypes_OrgId_Code\" ON miniwms.\"PartMaterialTypes\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"ProductModels\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"BrandCode\" text NULL, \"OrgModelCode\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_ProductModels_OrgId_Code\" ON miniwms.\"ProductModels\" (\"OrgId\", \"Code\")",
            "CREATE INDEX IF NOT EXISTS \"IX_ProductModels_OrgId_BrandCode\" ON miniwms.\"ProductModels\" (\"OrgId\", \"BrandCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"InventoryTypes\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_InventoryTypes_OrgId_Code\" ON miniwms.\"InventoryTypes\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"InventoryLevelTypes\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_InventoryLevelTypes_OrgId_Code\" ON miniwms.\"InventoryLevelTypes\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"InventoryInTypes\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"FlagStatistic\" boolean NOT NULL DEFAULT true, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_InventoryInTypes_OrgId_Code\" ON miniwms.\"InventoryInTypes\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"InventoryOutTypes\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"FlagStatistic\" boolean NOT NULL DEFAULT true, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_InventoryOutTypes_OrgId_Code\" ON miniwms.\"InventoryOutTypes\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"UserMapInventories\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"WarehouseId\" integer NOT NULL, \"UserCode\" text NOT NULL, \"UserName\" text NOT NULL, \"UserRole\" text NOT NULL DEFAULT 'Thủ kho chính', \"Email\" text NULL, \"Phone\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"AssignedBy\" text NOT NULL DEFAULT 'admin', \"AssignedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_UserMapInventories_OrgId_WarehouseId_UserCode\" ON miniwms.\"UserMapInventories\" (\"OrgId\", \"WarehouseId\", \"UserCode\")",
            "CREATE INDEX IF NOT EXISTS \"IX_UserMapInventories_OrgId_UserCode\" ON miniwms.\"UserMapInventories\" (\"OrgId\", \"UserCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"ProductGroups\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"Description\" text NULL, \"ParentCode\" text NULL, \"BrandCode\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_ProductGroups_OrgId_Code\" ON miniwms.\"ProductGroups\" (\"OrgId\", \"Code\")",
            "CREATE INDEX IF NOT EXISTS \"IX_ProductGroups_OrgId_ParentCode\" ON miniwms.\"ProductGroups\" (\"OrgId\", \"ParentCode\")",
            "CREATE INDEX IF NOT EXISTS \"IX_ProductGroups_OrgId_BrandCode\" ON miniwms.\"ProductGroups\" (\"OrgId\", \"BrandCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"Areas\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"Description\" text NULL, \"ParentCode\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Areas_OrgId_Code\" ON miniwms.\"Areas\" (\"OrgId\", \"Code\")",
            "CREATE INDEX IF NOT EXISTS \"IX_Areas_OrgId_ParentCode\" ON miniwms.\"Areas\" (\"OrgId\", \"ParentCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"CustomerGroups\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"Description\" text NULL, \"ParentCode\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_CustomerGroups_OrgId_Code\" ON miniwms.\"CustomerGroups\" (\"OrgId\", \"Code\")",
            "CREATE INDEX IF NOT EXISTS \"IX_CustomerGroups_OrgId_ParentCode\" ON miniwms.\"CustomerGroups\" (\"OrgId\", \"ParentCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"Departments\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"ParentCode\" text NULL, \"BUCode\" text NULL, \"Level\" integer NOT NULL DEFAULT 1, \"MST\" text NULL, \"Description\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Departments_OrgId_Code\" ON miniwms.\"Departments\" (\"OrgId\", \"Code\")",
            "CREATE INDEX IF NOT EXISTS \"IX_Departments_OrgId_ParentCode\" ON miniwms.\"Departments\" (\"OrgId\", \"ParentCode\")",
            "CREATE INDEX IF NOT EXISTS \"IX_Departments_OrgId_Level\" ON miniwms.\"Departments\" (\"OrgId\", \"Level\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"CustomerSources\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"ParentCode\" text NULL, \"BUCode\" text NULL, \"Description\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_CustomerSources_OrgId_Code\" ON miniwms.\"CustomerSources\" (\"OrgId\", \"Code\")",
            "CREATE INDEX IF NOT EXISTS \"IX_CustomerSources_OrgId_ParentCode\" ON miniwms.\"CustomerSources\" (\"OrgId\", \"ParentCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"MoveOrdTypes\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"Description\" text NULL, \"IsUrgent\" boolean NOT NULL DEFAULT false, \"IsActive\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_MoveOrdTypes_OrgId_Code\" ON miniwms.\"MoveOrdTypes\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"Dealers\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"ParentCode\" text NULL, \"Level\" integer NOT NULL DEFAULT 1, \"DealerType\" text NOT NULL DEFAULT 'Đại lý phân phối', \"BUCode\" text NULL, \"ProvinceCode\" text NULL, \"Address\" text NULL, \"PresentBy\" text NULL, \"GovIdNumber\" text NULL, \"Email\" text NULL, \"Phone\" text NULL, \"WarehouseId\" integer NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Dealers_OrgId_Code\" ON miniwms.\"Dealers\" (\"OrgId\", \"Code\")",
            "CREATE INDEX IF NOT EXISTS \"IX_Dealers_OrgId_ParentCode\" ON miniwms.\"Dealers\" (\"OrgId\", \"ParentCode\")",
            "CREATE INDEX IF NOT EXISTS \"IX_Dealers_OrgId_Level\" ON miniwms.\"Dealers\" (\"OrgId\", \"Level\")",
            "CREATE INDEX IF NOT EXISTS \"IX_Dealers_OrgId_ProvinceCode\" ON miniwms.\"Dealers\" (\"OrgId\", \"ProvinceCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"TempPrintTypes\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"GroupCode\" text NOT NULL DEFAULT 'DOC', \"Description\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_TempPrintTypes_OrgId_Code\" ON miniwms.\"TempPrintTypes\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"TempPrints\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"TypeCode\" text NOT NULL DEFAULT 'IN', \"PaperSize\" text NOT NULL DEFAULT 'A4_Portrait', \"UnitName\" text NOT NULL DEFAULT '', \"UnitAddress\" text NULL, \"UnitPhone\" text NULL, \"UnitEmail\" text NULL, \"HeaderTitle\" text NOT NULL DEFAULT '', \"SubTitle\" text NULL, \"BodyTemplateHtml\" text NOT NULL DEFAULT '', \"NoteFooter\" text NULL, \"IsDefault\" boolean NOT NULL DEFAULT false, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"UpdatedAt\" timestamp NULL)",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_TempPrints_OrgId_Code\" ON miniwms.\"TempPrints\" (\"OrgId\", \"Code\")",
            "CREATE INDEX IF NOT EXISTS \"IX_TempPrints_OrgId_TypeCode\" ON miniwms.\"TempPrints\" (\"OrgId\", \"TypeCode\")",
            "CREATE INDEX IF NOT EXISTS \"IX_TempPrints_OrgId_IsDefault\" ON miniwms.\"TempPrints\" (\"OrgId\", \"IsDefault\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"CurrencyExchanges\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"BaseCurrencyCode\" text NOT NULL DEFAULT 'VND', \"BuyRate\" numeric NOT NULL DEFAULT 1, \"SellRate\" numeric NOT NULL DEFAULT 1, \"InterExRate\" numeric NOT NULL DEFAULT 1, \"InterExSource\" text NULL, \"Symbol\" text NULL, \"IsBase\" boolean NOT NULL DEFAULT false, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"UpdatedTime\" timestamp NOT NULL DEFAULT now(), \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_CurrencyExchanges_OrgId_Code\" ON miniwms.\"CurrencyExchanges\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"ProductSpecs\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL, \"SpecDesc\" text NULL, \"ModelCode\" text NULL, \"SpecType1\" text NULL, \"Color\" text NULL, \"StandardUnitCode\" text NULL, \"FlagHasSerial\" boolean NOT NULL DEFAULT false, \"FlagHasLOT\" boolean NOT NULL DEFAULT false, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"UpdatedAt\" timestamp NULL)",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_ProductSpecs_OrgId_Code\" ON miniwms.\"ProductSpecs\" (\"OrgId\", \"Code\")",
            "CREATE INDEX IF NOT EXISTS \"IX_ProductSpecs_OrgId_ModelCode\" ON miniwms.\"ProductSpecs\" (\"OrgId\", \"ModelCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"SpecPrices\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"SpecCode\" text NOT NULL, \"UnitCode\" text NOT NULL, \"BuyPrice\" numeric NOT NULL DEFAULT 0, \"SellPrice\" numeric NOT NULL DEFAULT 0, \"CurrencyCode\" text NOT NULL DEFAULT 'VND', \"VATRateCode\" text NULL, \"DiscountVND\" numeric NOT NULL DEFAULT 0, \"EffectDTimeStart\" timestamp NOT NULL DEFAULT now(), \"EffectDTimeEnd\" timestamp NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"UpdatedAt\" timestamp NULL)",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_SpecPrices_OrgId_SpecCode_UnitCode\" ON miniwms.\"SpecPrices\" (\"OrgId\", \"SpecCode\", \"UnitCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"SpecUnits\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"SpecCode\" text NOT NULL, \"UnitCode\" text NOT NULL, \"StandardUnitCode\" text NULL, \"SpecUnitDesc\" text NULL, \"Qty\" numeric NOT NULL DEFAULT 1, \"Length\" numeric NOT NULL DEFAULT 0, \"Width\" numeric NOT NULL DEFAULT 0, \"Height\" numeric NOT NULL DEFAULT 0, \"Volume\" numeric NOT NULL DEFAULT 0, \"Weight\" numeric NOT NULL DEFAULT 0, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"UpdatedAt\" timestamp NULL)",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_SpecUnits_OrgId_SpecCode_UnitCode\" ON miniwms.\"SpecUnits\" (\"OrgId\", \"SpecCode\", \"UnitCode\")",
            "CREATE INDEX IF NOT EXISTS \"IX_SpecUnits_OrgId_SpecCode\" ON miniwms.\"SpecUnits\" (\"OrgId\", \"SpecCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"VATRates\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"VATRateCode\" text NOT NULL, \"Rate\" numeric NOT NULL DEFAULT 0, \"VATDesc\" text NOT NULL DEFAULT '', \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"UpdatedAt\" timestamp NULL)",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_VATRates_OrgId_VATRateCode\" ON miniwms.\"VATRates\" (\"OrgId\", \"VATRateCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"PartColors\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL DEFAULT '', \"NameVN\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_PartColors_OrgId_Code\" ON miniwms.\"PartColors\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"PartColorMaps\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"ProductId\" integer NOT NULL, \"PartColorCode\" text NOT NULL, \"IsDefault\" boolean NOT NULL DEFAULT false, \"IsActive\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_PartColorMaps_OrgId_ProductId_PartColorCode\" ON miniwms.\"PartColorMaps\" (\"OrgId\", \"ProductId\", \"PartColorCode\")",
            "CREATE INDEX IF NOT EXISTS \"IX_PartColorMaps_OrgId_PartColorCode\" ON miniwms.\"PartColorMaps\" (\"OrgId\", \"PartColorCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"InventorySecrets\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"SerialNo\" text NOT NULL, \"QrSerialNo\" text NOT NULL DEFAULT '', \"GenTimesNo\" text NULL, \"FlagMap\" boolean NOT NULL DEFAULT false, \"FlagUsed\" boolean NOT NULL DEFAULT false, \"Remark\" text NULL, \"LogLUBy\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"UpdatedAt\" timestamp NULL)",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_InventorySecrets_OrgId_SerialNo\" ON miniwms.\"InventorySecrets\" (\"OrgId\", \"SerialNo\")",
            "CREATE INDEX IF NOT EXISTS \"IX_InventorySecrets_OrgId_GenTimesNo\" ON miniwms.\"InventorySecrets\" (\"OrgId\", \"GenTimesNo\")",
            "CREATE INDEX IF NOT EXISTS \"IX_InventorySecrets_OrgId_FlagUsed\" ON miniwms.\"InventorySecrets\" (\"OrgId\", \"FlagUsed\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"SecretLicenses\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Mst\" text NOT NULL DEFAULT '', \"TotalQty\" integer NOT NULL DEFAULT 0, \"TotalQtyIssued\" integer NOT NULL DEFAULT 0, \"TotalQtyUsed\" integer NOT NULL DEFAULT 0, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"UpdatedAt\" timestamp NULL)",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_SecretLicenses_OrgId_Mst\" ON miniwms.\"SecretLicenses\" (\"OrgId\", \"Mst\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"Provinces\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL DEFAULT '', \"IsActive\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Provinces_OrgId_Code\" ON miniwms.\"Provinces\" (\"OrgId\", \"Code\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"Districts\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"ProvinceCode\" text NOT NULL DEFAULT '', \"Name\" text NOT NULL DEFAULT '', \"IsActive\" boolean NOT NULL DEFAULT true, \"CreatedAt\" timestamp NOT NULL DEFAULT now())",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Districts_OrgId_Code\" ON miniwms.\"Districts\" (\"OrgId\", \"Code\")",
            "CREATE INDEX IF NOT EXISTS \"IX_Districts_OrgId_ProvinceCode\" ON miniwms.\"Districts\" (\"OrgId\", \"ProvinceCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"Agents\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"Name\" text NOT NULL DEFAULT '', \"ProvinceCode\" text NULL, \"DistrictCode\" text NULL, \"Address\" text NULL, \"IsActive\" boolean NOT NULL DEFAULT true, \"Remark\" text NULL, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"UpdatedAt\" timestamp NULL)",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Agents_OrgId_Code\" ON miniwms.\"Agents\" (\"OrgId\", \"Code\")",
            "CREATE INDEX IF NOT EXISTS \"IX_Agents_OrgId_ProvinceCode\" ON miniwms.\"Agents\" (\"OrgId\", \"ProvinceCode\")",
            "CREATE INDEX IF NOT EXISTS \"IX_Agents_OrgId_DistrictCode\" ON miniwms.\"Agents\" (\"OrgId\", \"DistrictCode\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"PurchaseReceipts\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"Code\" text NOT NULL, \"WarehouseId\" integer NOT NULL, \"InvInTypeCode\" text NULL, \"InvInTypeName\" text NULL, \"SupplierName\" text NOT NULL DEFAULT '', \"SupplierCode\" text NULL, \"InvoiceNo\" text NULL, \"InvoiceDate\" timestamp NULL, \"OrderNo\" text NULL, \"UserDeliver\" text NULL, \"VehicleNo\" text NULL, \"ContainerNo\" text NULL, \"ContractNo\" text NULL, \"Date\" timestamp NOT NULL DEFAULT now(), \"CreatedBy\" text NOT NULL DEFAULT '', \"Status\" integer NOT NULL DEFAULT 0, \"CreatedAt\" timestamp NOT NULL DEFAULT now(), \"ApprovedAt\" timestamp NULL, \"ApprovedBy\" text NULL, \"Remark\" text NULL, \"StockDocId\" integer NULL)",
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_PurchaseReceipts_OrgId_Code\" ON miniwms.\"PurchaseReceipts\" (\"OrgId\", \"Code\")",
            "CREATE INDEX IF NOT EXISTS \"IX_PurchaseReceipts_OrgId_WarehouseId_Status\" ON miniwms.\"PurchaseReceipts\" (\"OrgId\", \"WarehouseId\", \"Status\")",
            "CREATE TABLE IF NOT EXISTS miniwms.\"PurchaseReceiptLines\" (\"Id\" serial PRIMARY KEY, \"OrgId\" uuid NOT NULL DEFAULT '" + def + "', \"PurchaseReceiptId\" integer NOT NULL, \"ProductId\" integer NOT NULL, \"Quantity\" integer NOT NULL DEFAULT 0, \"UnitPrice\" numeric NOT NULL DEFAULT 0, \"VATRate\" double precision NOT NULL DEFAULT 0, \"UnitCode\" text NULL, \"Note\" text NULL)",
            "CREATE INDEX IF NOT EXISTS \"IX_PurchaseReceiptLines_OrgId_PurchaseReceiptId\" ON miniwms.\"PurchaseReceiptLines\" (\"OrgId\", \"PurchaseReceiptId\")",
        };
        foreach (var t in tables) sql.Add($"ALTER TABLE miniwms.\"{t}\" ADD COLUMN IF NOT EXISTS \"OrgId\" uuid NOT NULL DEFAULT '{def}'");
        sql.Add("ALTER TABLE miniwms.\"Products\" ADD COLUMN IF NOT EXISTS \"SpecCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"MoveOrders\" ADD COLUMN IF NOT EXISTS \"MoveOrdTypeCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"MoveOrders\" ADD COLUMN IF NOT EXISTS \"MoveOrdTypeName\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Customers\" ADD COLUMN IF NOT EXISTS \"CustomerSourceCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Docs\" ADD COLUMN IF NOT EXISTS \"DepartmentCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Docs\" ADD COLUMN IF NOT EXISTS \"DepartmentName\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"UserMapInventories\" ADD COLUMN IF NOT EXISTS \"DepartmentCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Warehouses\" ADD COLUMN IF NOT EXISTS \"InvTypeCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Warehouses\" ADD COLUMN IF NOT EXISTS \"InvLevelTypeCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Warehouses\" ADD COLUMN IF NOT EXISTS \"AreaCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Warehouses\" ADD COLUMN IF NOT EXISTS \"Remark\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Customers\" ADD COLUMN IF NOT EXISTS \"AreaCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Customers\" ADD COLUMN IF NOT EXISTS \"CustomerGrpCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Products\" ADD COLUMN IF NOT EXISTS \"MaxStock\" integer NOT NULL DEFAULT 0");
        sql.Add("ALTER TABLE miniwms.\"Products\" ADD COLUMN IF NOT EXISTS \"CostPrice\" numeric NOT NULL DEFAULT 0");
        sql.Add("ALTER TABLE miniwms.\"Products\" ADD COLUMN IF NOT EXISTS \"PartTypeCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Products\" ADD COLUMN IF NOT EXISTS \"BrandCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Products\" ADD COLUMN IF NOT EXISTS \"PMType\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Products\" ADD COLUMN IF NOT EXISTS \"ModelCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Products\" ADD COLUMN IF NOT EXISTS \"ProductGrpCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Docs\" ADD COLUMN IF NOT EXISTS \"SupplierCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Docs\" ADD COLUMN IF NOT EXISTS \"SupplierName\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Docs\" ADD COLUMN IF NOT EXISTS \"CustomerCode\" text NULL");
        sql.Add("ALTER TABLE miniwms.\"Docs\" ADD COLUMN IF NOT EXISTS \"CustomerName\" text NULL");
        foreach (var s in sql) try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
    }

    private static async Task MigrateSqliteAsync(AppDbContext db)
    {
        if (db.Database.IsNpgsql()) return;
        var sql = new List<string>
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
            @"CREATE INDEX IF NOT EXISTS ""IX_InventoryCartons_OrgId_WarehouseId_Status"" ON ""InventoryCartons"" (""OrgId"", ""WarehouseId"", ""Status"");",
            @"CREATE TABLE IF NOT EXISTS ""InventoryInFGs"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""WarehouseId"" INTEGER NOT NULL,
                ""FormType"" INTEGER NOT NULL DEFAULT 0,
                ""WorkshopName"" TEXT NOT NULL,
                ""WorkOrderNo"" TEXT NULL,
                ""ShiftLeader"" TEXT NULL,
                ""Date"" TEXT NOT NULL,
                ""CreatedBy"" TEXT NOT NULL,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""CreatedAt"" TEXT NOT NULL,
                ""ApprovedAt"" TEXT NULL,
                ""ApprovedBy"" TEXT NULL,
                ""Remark"" TEXT NULL,
                ""StockDocId"" INTEGER NULL,
                CONSTRAINT ""FK_InventoryInFGs_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_InventoryInFGs_Docs_StockDocId"" FOREIGN KEY (""StockDocId"") REFERENCES ""Docs"" (""Id"") ON DELETE SET NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_InventoryInFGs_OrgId_Code"" ON ""InventoryInFGs"" (""OrgId"", ""Code"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_InventoryInFGs_OrgId_WarehouseId_Status"" ON ""InventoryInFGs"" (""OrgId"", ""WarehouseId"", ""Status"");",
            @"CREATE TABLE IF NOT EXISTS ""InventoryInFGLines"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""InventoryInFGId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""PlanQty"" INTEGER NOT NULL,
                ""ActualQty"" INTEGER NOT NULL,
                ""DefectQty"" INTEGER NOT NULL DEFAULT 0,
                ""UnitCost"" NUMERIC NOT NULL DEFAULT 0,
                ""ProductionDate"" TEXT NULL,
                ""Note"" TEXT NULL,
                CONSTRAINT ""FK_InventoryInFGLines_InventoryInFGs_InventoryInFGId"" FOREIGN KEY (""InventoryInFGId"") REFERENCES ""InventoryInFGs"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_InventoryInFGLines_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE RESTRICT
            );",
            @"CREATE TABLE IF NOT EXISTS ""InventoryInFGSerials"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""InventoryInFGId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""SerialNo"" TEXT NOT NULL,
                ""Note"" TEXT NULL,
                CONSTRAINT ""FK_InventoryInFGSerials_InventoryInFGs_InventoryInFGId"" FOREIGN KEY (""InventoryInFGId"") REFERENCES ""InventoryInFGs"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_InventoryInFGSerials_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE RESTRICT
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_InventoryInFGSerials_OrgId_InventoryInFGId_ProductId_SerialNo"" ON ""InventoryInFGSerials"" (""OrgId"", ""InventoryInFGId"", ""ProductId"", ""SerialNo"");",
            @"CREATE TABLE IF NOT EXISTS ""InventoryOutFGs"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""WarehouseId"" INTEGER NOT NULL,
                ""OutType"" INTEGER NOT NULL DEFAULT 0,
                ""FormType"" INTEGER NOT NULL DEFAULT 0,
                ""CustomerName"" TEXT NOT NULL,
                ""AgentCode"" TEXT NULL,
                ""DeliveryAddress"" TEXT NULL,
                ""DriverName"" TEXT NULL,
                ""DriverPhone"" TEXT NULL,
                ""PlateNo"" TEXT NULL,
                ""MoocNo"" TEXT NULL,
                ""OrderNo"" TEXT NULL,
                ""Date"" TEXT NOT NULL,
                ""CreatedBy"" TEXT NOT NULL,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""CreatedAt"" TEXT NOT NULL,
                ""ApprovedAt"" TEXT NULL,
                ""ApprovedBy"" TEXT NULL,
                ""Remark"" TEXT NULL,
                ""StockDocId"" INTEGER NULL,
                CONSTRAINT ""FK_InventoryOutFGs_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_InventoryOutFGs_Docs_StockDocId"" FOREIGN KEY (""StockDocId"") REFERENCES ""Docs"" (""Id"") ON DELETE SET NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_InventoryOutFGs_OrgId_Code"" ON ""InventoryOutFGs"" (""OrgId"", ""Code"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_InventoryOutFGs_OrgId_WarehouseId_Status"" ON ""InventoryOutFGs"" (""OrgId"", ""WarehouseId"", ""Status"");",
            @"CREATE TABLE IF NOT EXISTS ""InventoryOutFGLines"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""InventoryOutFGId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""Qty"" INTEGER NOT NULL,
                ""UnitCost"" NUMERIC NOT NULL DEFAULT 0,
                ""UnitPrice"" NUMERIC NOT NULL DEFAULT 0,
                ""Note"" TEXT NULL,
                CONSTRAINT ""FK_InventoryOutFGLines_InventoryOutFGs_InventoryOutFGId"" FOREIGN KEY (""InventoryOutFGId"") REFERENCES ""InventoryOutFGs"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_InventoryOutFGLines_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE RESTRICT
            );",
            @"CREATE TABLE IF NOT EXISTS ""InventoryOutFGSerials"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""InventoryOutFGId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""SerialNo"" TEXT NOT NULL,
                ""Note"" TEXT NULL,
                CONSTRAINT ""FK_InventoryOutFGSerials_InventoryOutFGs_InventoryOutFGId"" FOREIGN KEY (""InventoryOutFGId"") REFERENCES ""InventoryOutFGs"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_InventoryOutFGSerials_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE RESTRICT
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_InventoryOutFGSerials_OrgId_InventoryOutFGId_ProductId_SerialNo"" ON ""InventoryOutFGSerials"" (""OrgId"", ""InventoryOutFGId"", ""ProductId"", ""SerialNo"");",
            @"CREATE TABLE IF NOT EXISTS ""InventoryBoxes"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""BoxCode"" TEXT NOT NULL,
                ""QrCode"" TEXT NULL,
                ""GenTimesBoxNo"" TEXT NULL,
                ""SecretNo"" TEXT NULL,
                ""WarehouseId"" INTEGER NOT NULL,
                ""CartonId"" INTEGER NULL,
                ""BoxType"" TEXT NOT NULL DEFAULT 'Hộp duplex tiêu chuẩn',
                ""ProductId"" INTEGER NULL,
                ""LotNo"" TEXT NULL,
                ""Quantity"" INTEGER NOT NULL DEFAULT 0,
                ""Capacity"" INTEGER NOT NULL DEFAULT 10,
                ""LengthCm"" REAL NOT NULL DEFAULT 20,
                ""WidthCm"" REAL NOT NULL DEFAULT 15,
                ""HeightCm"" REAL NOT NULL DEFAULT 10,
                ""GrossWeightKg"" REAL NOT NULL DEFAULT 0,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""FlagMap"" INTEGER NOT NULL DEFAULT 0,
                ""FlagUsed"" INTEGER NOT NULL DEFAULT 0,
                ""ShelfLocation"" TEXT NULL,
                ""PackerName"" TEXT NULL,
                ""PackedAt"" TEXT NULL,
                ""SealedAt"" TEXT NULL,
                ""ShippedAt"" TEXT NULL,
                ""RefDocNo"" TEXT NULL,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                CONSTRAINT ""FK_InventoryBoxes_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_InventoryBoxes_InventoryCartons_CartonId"" FOREIGN KEY (""CartonId"") REFERENCES ""InventoryCartons"" (""Id"") ON DELETE SET NULL,
                CONSTRAINT ""FK_InventoryBoxes_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE SET NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_InventoryBoxes_OrgId_BoxCode"" ON ""InventoryBoxes"" (""OrgId"", ""BoxCode"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_InventoryBoxes_OrgId_WarehouseId_Status"" ON ""InventoryBoxes"" (""OrgId"", ""WarehouseId"", ""Status"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_InventoryBoxes_OrgId_CartonId"" ON ""InventoryBoxes"" (""OrgId"", ""CartonId"");",
            @"ALTER TABLE ""Docs"" ADD COLUMN ""SupplierCode"" TEXT NULL;",
            @"ALTER TABLE ""Docs"" ADD COLUMN ""SupplierName"" TEXT NULL;",
            @"ALTER TABLE ""Docs"" ADD COLUMN ""CustomerCode"" TEXT NULL;",
            @"ALTER TABLE ""Docs"" ADD COLUMN ""CustomerName"" TEXT NULL;",
            @"CREATE TABLE IF NOT EXISTS ""Suppliers"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""ContactName"" TEXT NULL,
                ""Phone"" TEXT NULL,
                ""Email"" TEXT NULL,
                ""Address"" TEXT NULL,
                ""TaxCode"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Note"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Suppliers_OrgId_Code"" ON ""Suppliers"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""Customers"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""CustomerType"" TEXT NOT NULL DEFAULT 'Đại lý phân phối',
                ""ContactName"" TEXT NULL,
                ""ContactPhone"" TEXT NULL,
                ""Phone"" TEXT NULL,
                ""Email"" TEXT NULL,
                ""Address"" TEXT NULL,
                ""Province"" TEXT NULL,
                ""TaxCode"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Note"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Customers_OrgId_Code"" ON ""Customers"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""PartTypes"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PartTypes_OrgId_Code"" ON ""PartTypes"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""Brands"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""Origin"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Brands_OrgId_Code"" ON ""Brands"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""PartUnits"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""IsStandard"" INTEGER NOT NULL DEFAULT 1,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PartUnits_OrgId_Code"" ON ""PartUnits"" (""OrgId"", ""Code"");",
            @"ALTER TABLE ""Products"" ADD COLUMN ""PartTypeCode"" TEXT NULL;",
            @"ALTER TABLE ""Products"" ADD COLUMN ""BrandCode"" TEXT NULL;",
            @"ALTER TABLE ""Products"" ADD COLUMN ""PMType"" TEXT NULL;",
            @"ALTER TABLE ""Products"" ADD COLUMN ""ModelCode"" TEXT NULL;",
            @"CREATE TABLE IF NOT EXISTS ""PartMaterialTypes"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PartMaterialTypes_OrgId_Code"" ON ""PartMaterialTypes"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""ProductModels"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""BrandCode"" TEXT NULL,
                ""OrgModelCode"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ProductModels_OrgId_Code"" ON ""ProductModels"" (""OrgId"", ""Code"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_ProductModels_OrgId_BrandCode"" ON ""ProductModels"" (""OrgId"", ""BrandCode"");",
            @"ALTER TABLE ""Warehouses"" ADD COLUMN ""InvTypeCode"" TEXT NULL;",
            @"ALTER TABLE ""Warehouses"" ADD COLUMN ""InvLevelTypeCode"" TEXT NULL;",
            @"ALTER TABLE ""Warehouses"" ADD COLUMN ""Remark"" TEXT NULL;",
            @"CREATE TABLE IF NOT EXISTS ""InventoryTypes"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_InventoryTypes_OrgId_Code"" ON ""InventoryTypes"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""InventoryLevelTypes"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_InventoryLevelTypes_OrgId_Code"" ON ""InventoryLevelTypes"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""InventoryInTypes"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""FlagStatistic"" INTEGER NOT NULL DEFAULT 1,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_InventoryInTypes_OrgId_Code"" ON ""InventoryInTypes"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""InventoryOutTypes"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""FlagStatistic"" INTEGER NOT NULL DEFAULT 1,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_InventoryOutTypes_OrgId_Code"" ON ""InventoryOutTypes"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""UserMapInventories"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""WarehouseId"" INTEGER NOT NULL,
                ""UserCode"" TEXT NOT NULL,
                ""UserName"" TEXT NOT NULL,
                ""UserRole"" TEXT NOT NULL DEFAULT 'Thủ kho chính',
                ""Email"" TEXT NULL,
                ""Phone"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""AssignedBy"" TEXT NOT NULL DEFAULT 'admin',
                ""AssignedAt"" TEXT NOT NULL,
                CONSTRAINT ""FK_UserMapInventories_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE CASCADE
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserMapInventories_OrgId_WarehouseId_UserCode"" ON ""UserMapInventories"" (""OrgId"", ""WarehouseId"", ""UserCode"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_UserMapInventories_OrgId_UserCode"" ON ""UserMapInventories"" (""OrgId"", ""UserCode"");",
            @"ALTER TABLE ""Products"" ADD COLUMN ""ProductGrpCode"" TEXT NULL;",
            @"CREATE TABLE IF NOT EXISTS ""ProductGroups"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""Description"" TEXT NULL,
                ""ParentCode"" TEXT NULL,
                ""BrandCode"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ProductGroups_OrgId_Code"" ON ""ProductGroups"" (""OrgId"", ""Code"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_ProductGroups_OrgId_ParentCode"" ON ""ProductGroups"" (""OrgId"", ""ParentCode"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_ProductGroups_OrgId_BrandCode"" ON ""ProductGroups"" (""OrgId"", ""BrandCode"");",
            @"ALTER TABLE ""Warehouses"" ADD COLUMN ""AreaCode"" TEXT NULL;",
            @"ALTER TABLE ""Customers"" ADD COLUMN ""AreaCode"" TEXT NULL;",
            @"CREATE TABLE IF NOT EXISTS ""Areas"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""Description"" TEXT NULL,
                ""ParentCode"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Areas_OrgId_Code"" ON ""Areas"" (""OrgId"", ""Code"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Areas_OrgId_ParentCode"" ON ""Areas"" (""OrgId"", ""ParentCode"");",
            @"ALTER TABLE ""Customers"" ADD COLUMN ""CustomerGrpCode"" TEXT NULL;",
            @"CREATE TABLE IF NOT EXISTS ""CustomerGroups"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""Description"" TEXT NULL,
                ""ParentCode"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CustomerGroups_OrgId_Code"" ON ""CustomerGroups"" (""OrgId"", ""Code"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_CustomerGroups_OrgId_ParentCode"" ON ""CustomerGroups"" (""OrgId"", ""ParentCode"");",
            @"ALTER TABLE ""Docs"" ADD COLUMN ""DepartmentCode"" TEXT NULL;",
            @"ALTER TABLE ""Docs"" ADD COLUMN ""DepartmentName"" TEXT NULL;",
            @"ALTER TABLE ""UserMapInventories"" ADD COLUMN ""DepartmentCode"" TEXT NULL;",
            @"CREATE TABLE IF NOT EXISTS ""Departments"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""ParentCode"" TEXT NULL,
                ""BUCode"" TEXT NULL,
                ""Level"" INTEGER NOT NULL DEFAULT 1,
                ""MST"" TEXT NULL,
                ""Description"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Departments_OrgId_Code"" ON ""Departments"" (""OrgId"", ""Code"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Departments_OrgId_ParentCode"" ON ""Departments"" (""OrgId"", ""ParentCode"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Departments_OrgId_Level"" ON ""Departments"" (""OrgId"", ""Level"");",
            @"ALTER TABLE ""Customers"" ADD COLUMN ""CustomerSourceCode"" TEXT NULL;",
            @"CREATE TABLE IF NOT EXISTS ""CustomerSources"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""ParentCode"" TEXT NULL,
                ""BUCode"" TEXT NULL,
                ""Description"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CustomerSources_OrgId_Code"" ON ""CustomerSources"" (""OrgId"", ""Code"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_CustomerSources_OrgId_ParentCode"" ON ""CustomerSources"" (""OrgId"", ""ParentCode"");",
            @"ALTER TABLE ""MoveOrders"" ADD COLUMN ""MoveOrdTypeCode"" TEXT NULL;",
            @"ALTER TABLE ""MoveOrders"" ADD COLUMN ""MoveOrdTypeName"" TEXT NULL;",
            @"CREATE TABLE IF NOT EXISTS ""MoveOrdTypes"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""Description"" TEXT NULL,
                ""IsUrgent"" INTEGER NOT NULL DEFAULT 0,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MoveOrdTypes_OrgId_Code"" ON ""MoveOrdTypes"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""Dealers"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""ParentCode"" TEXT NULL,
                ""Level"" INTEGER NOT NULL DEFAULT 1,
                ""DealerType"" TEXT NOT NULL DEFAULT 'Đại lý phân phối',
                ""BUCode"" TEXT NULL,
                ""ProvinceCode"" TEXT NULL,
                ""Address"" TEXT NULL,
                ""PresentBy"" TEXT NULL,
                ""GovIdNumber"" TEXT NULL,
                ""Email"" TEXT NULL,
                ""Phone"" TEXT NULL,
                ""WarehouseId"" INTEGER NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                CONSTRAINT ""FK_Dealers_Warehouses_WarehouseId"" FOREIGN KEY (""WarehouseId"") REFERENCES ""Warehouses"" (""Id"") ON DELETE SET NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Dealers_OrgId_Code"" ON ""Dealers"" (""OrgId"", ""Code"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Dealers_OrgId_ParentCode"" ON ""Dealers"" (""OrgId"", ""ParentCode"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Dealers_OrgId_Level"" ON ""Dealers"" (""OrgId"", ""Level"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Dealers_OrgId_ProvinceCode"" ON ""Dealers"" (""OrgId"", ""ProvinceCode"");",
            @"CREATE TABLE IF NOT EXISTS ""TempPrintTypes"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""GroupCode"" TEXT NOT NULL DEFAULT 'DOC',
                ""Description"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_TempPrintTypes_OrgId_Code"" ON ""TempPrintTypes"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""TempPrints"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""TypeCode"" TEXT NOT NULL DEFAULT 'IN',
                ""PaperSize"" TEXT NOT NULL DEFAULT 'A4_Portrait',
                ""UnitName"" TEXT NOT NULL DEFAULT '',
                ""UnitAddress"" TEXT NULL,
                ""UnitPhone"" TEXT NULL,
                ""UnitEmail"" TEXT NULL,
                ""HeaderTitle"" TEXT NOT NULL DEFAULT '',
                ""SubTitle"" TEXT NULL,
                ""BodyTemplateHtml"" TEXT NOT NULL DEFAULT '',
                ""NoteFooter"" TEXT NULL,
                ""IsDefault"" INTEGER NOT NULL DEFAULT 0,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_TempPrints_OrgId_Code"" ON ""TempPrints"" (""OrgId"", ""Code"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_TempPrints_OrgId_TypeCode"" ON ""TempPrints"" (""OrgId"", ""TypeCode"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_TempPrints_OrgId_IsDefault"" ON ""TempPrints"" (""OrgId"", ""IsDefault"");",
            @"CREATE TABLE IF NOT EXISTS ""CurrencyExchanges"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""BaseCurrencyCode"" TEXT NOT NULL DEFAULT 'VND',
                ""BuyRate"" NUMERIC NOT NULL DEFAULT 1,
                ""SellRate"" NUMERIC NOT NULL DEFAULT 1,
                ""InterExRate"" NUMERIC NOT NULL DEFAULT 1,
                ""InterExSource"" TEXT NULL,
                ""Symbol"" TEXT NULL,
                ""IsBase"" INTEGER NOT NULL DEFAULT 0,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""UpdatedTime"" TEXT NOT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CurrencyExchanges_OrgId_Code"" ON ""CurrencyExchanges"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""ProductSpecs"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""SpecDesc"" TEXT NULL,
                ""ModelCode"" TEXT NULL,
                ""SpecType1"" TEXT NULL,
                ""Color"" TEXT NULL,
                ""StandardUnitCode"" TEXT NULL,
                ""FlagHasSerial"" INTEGER NOT NULL DEFAULT 0,
                ""FlagHasLOT"" INTEGER NOT NULL DEFAULT 0,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ProductSpecs_OrgId_Code"" ON ""ProductSpecs"" (""OrgId"", ""Code"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_ProductSpecs_OrgId_ModelCode"" ON ""ProductSpecs"" (""OrgId"", ""ModelCode"");",
            @"ALTER TABLE ""Products"" ADD COLUMN ""SpecCode"" TEXT NULL;",
            @"CREATE TABLE IF NOT EXISTS ""SpecPrices"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""SpecCode"" TEXT NOT NULL,
                ""UnitCode"" TEXT NOT NULL,
                ""BuyPrice"" NUMERIC NOT NULL DEFAULT 0,
                ""SellPrice"" NUMERIC NOT NULL DEFAULT 0,
                ""CurrencyCode"" TEXT NOT NULL DEFAULT 'VND',
                ""VATRateCode"" TEXT NULL,
                ""DiscountVND"" NUMERIC NOT NULL DEFAULT 0,
                ""EffectDTimeStart"" TEXT NOT NULL,
                ""EffectDTimeEnd"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_SpecPrices_OrgId_SpecCode_UnitCode"" ON ""SpecPrices"" (""OrgId"", ""SpecCode"", ""UnitCode"");",
            @"CREATE TABLE IF NOT EXISTS ""SpecUnits"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""SpecCode"" TEXT NOT NULL,
                ""UnitCode"" TEXT NOT NULL,
                ""StandardUnitCode"" TEXT NULL,
                ""SpecUnitDesc"" TEXT NULL,
                ""Qty"" NUMERIC NOT NULL DEFAULT 1,
                ""Length"" NUMERIC NOT NULL DEFAULT 0,
                ""Width"" NUMERIC NOT NULL DEFAULT 0,
                ""Height"" NUMERIC NOT NULL DEFAULT 0,
                ""Volume"" NUMERIC NOT NULL DEFAULT 0,
                ""Weight"" NUMERIC NOT NULL DEFAULT 0,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_SpecUnits_OrgId_SpecCode_UnitCode"" ON ""SpecUnits"" (""OrgId"", ""SpecCode"", ""UnitCode"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_SpecUnits_OrgId_SpecCode"" ON ""SpecUnits"" (""OrgId"", ""SpecCode"");",
            @"CREATE TABLE IF NOT EXISTS ""VATRates"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""VATRateCode"" TEXT NOT NULL,
                ""Rate"" NUMERIC NOT NULL DEFAULT 0,
                ""VATDesc"" TEXT NOT NULL DEFAULT '',
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_VATRates_OrgId_VATRateCode"" ON ""VATRates"" (""OrgId"", ""VATRateCode"");",
            @"CREATE TABLE IF NOT EXISTS ""InventoryTransactions"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""WarehouseId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""TxnType"" INTEGER NOT NULL DEFAULT 0,
                ""FunctionName"" TEXT NOT NULL DEFAULT '',
                ""Quality"" INTEGER NOT NULL DEFAULT 0,
                ""QtyChTotalOK"" INTEGER NOT NULL DEFAULT 0,
                ""QtyChBlockOK"" INTEGER NOT NULL DEFAULT 0,
                ""QtyChTotalNG"" INTEGER NOT NULL DEFAULT 0,
                ""QtyChBlockNG"" INTEGER NOT NULL DEFAULT 0,
                ""RefType"" TEXT NULL,
                ""RefCode00"" TEXT NULL,
                ""RefCode01"" TEXT NULL,
                ""RefCode02"" TEXT NULL,
                ""RefCode03"" TEXT NULL,
                ""RefCode04"" TEXT NULL,
                ""RefCode05"" TEXT NULL,
                ""Remark"" TEXT NULL,
                ""CreatedBy"" TEXT NOT NULL DEFAULT '',
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE INDEX IF NOT EXISTS ""IX_InventoryTransactions_OrgId_WarehouseId_ProductId_CreatedAt"" ON ""InventoryTransactions"" (""OrgId"", ""WarehouseId"", ""ProductId"", ""CreatedAt"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_InventoryTransactions_OrgId_TxnType"" ON ""InventoryTransactions"" (""OrgId"", ""TxnType"");",
            @"CREATE TABLE IF NOT EXISTS ""PartColors"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""Code"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL DEFAULT '',
                ""NameVN"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""Remark"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PartColors_OrgId_Code"" ON ""PartColors"" (""OrgId"", ""Code"");",
            @"CREATE TABLE IF NOT EXISTS ""PartColorMaps"" (
                ""Id"" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ""OrgId"" TEXT NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""PartColorCode"" TEXT NOT NULL,
                ""IsDefault"" INTEGER NOT NULL DEFAULT 0,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL,
                CONSTRAINT ""FK_PartColorMaps_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE
            );",
            @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PartColorMaps_OrgId_ProductId_PartColorCode"" ON ""PartColorMaps"" (""OrgId"", ""ProductId"", ""PartColorCode"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_PartColorMaps_OrgId_PartColorCode"" ON ""PartColorMaps"" (""OrgId"", ""PartColorCode"");"
        };
        sql.Add("CREATE TABLE IF NOT EXISTS \"Provinces\" (\"Id\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"OrgId\" TEXT NOT NULL, \"Code\" TEXT NOT NULL, \"Name\" TEXT NOT NULL DEFAULT '', \"IsActive\" INTEGER NOT NULL DEFAULT 1, \"CreatedAt\" TEXT NOT NULL);");
        sql.Add("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Provinces_OrgId_Code\" ON \"Provinces\" (\"OrgId\", \"Code\");");
        sql.Add("CREATE TABLE IF NOT EXISTS \"Districts\" (\"Id\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"OrgId\" TEXT NOT NULL, \"Code\" TEXT NOT NULL, \"ProvinceCode\" TEXT NOT NULL DEFAULT '', \"Name\" TEXT NOT NULL DEFAULT '', \"IsActive\" INTEGER NOT NULL DEFAULT 1, \"CreatedAt\" TEXT NOT NULL);");
        sql.Add("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Districts_OrgId_Code\" ON \"Districts\" (\"OrgId\", \"Code\");");
        sql.Add("CREATE INDEX IF NOT EXISTS \"IX_Districts_OrgId_ProvinceCode\" ON \"Districts\" (\"OrgId\", \"ProvinceCode\");");
        sql.Add("CREATE TABLE IF NOT EXISTS \"Agents\" (\"Id\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"OrgId\" TEXT NOT NULL, \"Code\" TEXT NOT NULL, \"Name\" TEXT NOT NULL DEFAULT '', \"ProvinceCode\" TEXT NULL, \"DistrictCode\" TEXT NULL, \"Address\" TEXT NULL, \"IsActive\" INTEGER NOT NULL DEFAULT 1, \"Remark\" TEXT NULL, \"CreatedAt\" TEXT NOT NULL, \"UpdatedAt\" TEXT NULL);");
        sql.Add("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Agents_OrgId_Code\" ON \"Agents\" (\"OrgId\", \"Code\");");
        sql.Add("CREATE INDEX IF NOT EXISTS \"IX_Agents_OrgId_ProvinceCode\" ON \"Agents\" (\"OrgId\", \"ProvinceCode\");");
        sql.Add("CREATE INDEX IF NOT EXISTS \"IX_Agents_OrgId_DistrictCode\" ON \"Agents\" (\"OrgId\", \"DistrictCode\");");
        sql.Add("CREATE TABLE IF NOT EXISTS \"PurchaseReceipts\" (\"Id\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"OrgId\" TEXT NOT NULL, \"Code\" TEXT NOT NULL, \"WarehouseId\" INTEGER NOT NULL, \"InvInTypeCode\" TEXT NULL, \"InvInTypeName\" TEXT NULL, \"SupplierName\" TEXT NOT NULL DEFAULT '', \"SupplierCode\" TEXT NULL, \"InvoiceNo\" TEXT NULL, \"InvoiceDate\" TEXT NULL, \"OrderNo\" TEXT NULL, \"UserDeliver\" TEXT NULL, \"VehicleNo\" TEXT NULL, \"ContainerNo\" TEXT NULL, \"ContractNo\" TEXT NULL, \"Date\" TEXT NOT NULL, \"CreatedBy\" TEXT NOT NULL DEFAULT '', \"Status\" INTEGER NOT NULL DEFAULT 0, \"CreatedAt\" TEXT NOT NULL, \"ApprovedAt\" TEXT NULL, \"ApprovedBy\" TEXT NULL, \"Remark\" TEXT NULL, \"StockDocId\" INTEGER NULL, CONSTRAINT \"FK_PurchaseReceipts_Warehouses_WarehouseId\" FOREIGN KEY (\"WarehouseId\") REFERENCES \"Warehouses\" (\"Id\") ON DELETE RESTRICT, CONSTRAINT \"FK_PurchaseReceipts_Docs_StockDocId\" FOREIGN KEY (\"StockDocId\") REFERENCES \"Docs\" (\"Id\") ON DELETE SET NULL);");
        sql.Add("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_PurchaseReceipts_OrgId_Code\" ON \"PurchaseReceipts\" (\"OrgId\", \"Code\");");
        sql.Add("CREATE INDEX IF NOT EXISTS \"IX_PurchaseReceipts_OrgId_WarehouseId_Status\" ON \"PurchaseReceipts\" (\"OrgId\", \"WarehouseId\", \"Status\");");
        sql.Add("CREATE TABLE IF NOT EXISTS \"PurchaseReceiptLines\" (\"Id\" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, \"OrgId\" TEXT NOT NULL, \"PurchaseReceiptId\" INTEGER NOT NULL, \"ProductId\" INTEGER NOT NULL, \"Quantity\" INTEGER NOT NULL DEFAULT 0, \"UnitPrice\" NUMERIC NOT NULL DEFAULT 0, \"VATRate\" REAL NOT NULL DEFAULT 0, \"UnitCode\" TEXT NULL, \"Note\" TEXT NULL, CONSTRAINT \"FK_PurchaseReceiptLines_PurchaseReceipts_PurchaseReceiptId\" FOREIGN KEY (\"PurchaseReceiptId\") REFERENCES \"PurchaseReceipts\" (\"Id\") ON DELETE CASCADE, CONSTRAINT \"FK_PurchaseReceiptLines_Products_ProductId\" FOREIGN KEY (\"ProductId\") REFERENCES \"Products\" (\"Id\") ON DELETE RESTRICT);");
        sql.Add("CREATE INDEX IF NOT EXISTS \"IX_PurchaseReceiptLines_OrgId_PurchaseReceiptId\" ON \"PurchaseReceiptLines\" (\"OrgId\", \"PurchaseReceiptId\");");
        foreach (var s in sql)
        {
            try { await db.Database.ExecuteSqlRawAsync(s); } catch { }
        }
    }
}
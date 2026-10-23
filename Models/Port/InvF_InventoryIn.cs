using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — InvF_InventoryIn (phiếu nhập kho).
/// Model đại diện cho danh sách trả về trong PageInfo của action Index / DoSearch.
/// Cột tối thiểu lấy theo các trường chuẩn của chứng từ nhập kho hệ thống Skycic.Inventory.
/// </summary>
public class InvF_InventoryIn
{
    public string IF_InvInNo { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? InvInType { get; set; }
    public string? InvCodeIn { get; set; }
    public string? IF_InvAudNo { get; set; }
    public string? CustomerCode { get; set; }
    public string? InvoiceNo { get; set; }
    public string? OrderNoSys { get; set; }
    public string? OrderNo { get; set; }
    public string? OrderType { get; set; }
    public string? RefNoSys { get; set; }
    public string? RefNo { get; set; }
    public string? RefType { get; set; }
    public string? UserDeliver { get; set; }
    public double? TotalValIn { get; set; }
    public double? TotalValInDesc { get; set; }
    public double? TotalValInAfterDesc { get; set; }
    public DateTime? CreateDTimeUTC { get; set; }
    public string? CreateBy { get; set; }
    public DateTime? LUDTimeUTC { get; set; }
    public string? LUBy { get; set; }
    public DateTime? ApprDTimeUTC { get; set; }
    public string? ApprBy { get; set; }
    public string? FlagQR { get; set; }
    public string? IF_InvInStatus { get; set; }
    // #88 Cancel — 2 cột ghi khi hủy phiếu (InvF_InventoryIn_CancelX Form.cs:157546-157579)
    public DateTime? CancelDTimeUTC { get; set; }
    public string? CancelBy { get; set; }
    public string? Remark { get; set; }
    public string? InvInDesc { get; set; }
    public string? FlagActive { get; set; } = "1";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    public string? InvFCFInCode01 { get; set; }
    public string? InvFCFInCode02 { get; set; }
    public string? InvFCFInCode03 { get; set; }
    public string? InvFCFInCode04 { get; set; }
    public string? InvFCFInCode05 { get; set; }
    public string? InvFCFInCode06 { get; set; }
    public string? InvFCFInCode07 { get; set; }
    public string? InvFCFInCode08 { get; set; }
    public string? InvFCFInCode09 { get; set; }
    public string? InvFCFInCode10 { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? InvoiceSign { get; set; }
    public string? FlagSynced { get; set; }
    public string? CodeGuid { get; set; }
    public double? TotalValVAT { get; set; }
    public string? BusinessGroupCode { get; set; }

    // Các cột join bổ sung khi Get (Form.cs:146908-146920)
    public string? mct_CustomerCode { get; set; }
    public string? mct_CustomerName { get; set; }
    public string? miit_InvInType { get; set; }
    public string? miit_InvInTypeName { get; set; }
    public string? mii_InvCode { get; set; }
    public string? mii_InvName { get; set; }
    public string? su_UserCode_Create { get; set; }
    public string? su_UserName_Create { get; set; }
}

public class InvF_InventoryInConfiguration : IEntityTypeConfiguration<InvF_InventoryIn>
{
    public void Configure(EntityTypeBuilder<InvF_InventoryIn> b)
    {
        b.ToTable("InvF_InventoryIn");
        b.HasKey(x => x.IF_InvInNo);
        b.Property(x => x.IF_InvInNo).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.InvInType).HasMaxLength(50);
        b.Property(x => x.InvCodeIn).HasMaxLength(50);
        b.Property(x => x.CustomerCode).HasMaxLength(50);
        b.Property(x => x.InvoiceNo).HasMaxLength(100);
        b.Property(x => x.OrderNo).HasMaxLength(100);
        b.Property(x => x.RefNo).HasMaxLength(100);
        b.Property(x => x.IF_InvInStatus).HasMaxLength(50);
        b.Property(x => x.CancelBy).HasMaxLength(50);
        b.Property(x => x.BusinessGroupCode).HasMaxLength(50);
        b.Property(x => x.InvInDesc).HasMaxLength(500);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.CreateBy).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);

        // Các trường join không lưu vào bảng chính
        b.Ignore(x => x.mct_CustomerCode);
        b.Ignore(x => x.mct_CustomerName);
        b.Ignore(x => x.miit_InvInType);
        b.Ignore(x => x.miit_InvInTypeName);
        b.Ignore(x => x.mii_InvCode);
        b.Ignore(x => x.mii_InvName);
        b.Ignore(x => x.su_UserCode_Create);
        b.Ignore(x => x.su_UserName_Create);
    }
}

/// <summary>
/// port 1:1 InvF_InventoryInDtl.cs — chi tiết phiếu nhập kho.
/// </summary>
public class InvF_InventoryInDtl
{
    public string IF_InvInNo { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public string? InvCodeInActual { get; set; }
    public string? NetworkID { get; set; }
    public double? Qty { get; set; }
    public double? QtyReturn { get; set; }
    public double? UPIn { get; set; }
    public double? UPInDesc { get; set; }
    public double? ValInvIn { get; set; }
    public double? ValInDesc { get; set; }
    public double? ValInAfterDesc { get; set; }
    public string? UnitCode { get; set; }
    public string? IF_InvInStatusDtl { get; set; }
    public string? Remark { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public double? VAT { get; set; }
    public double? ValVAT { get; set; }
}

public class InvF_InventoryInDtlConfiguration : IEntityTypeConfiguration<InvF_InventoryInDtl>
{
    public void Configure(EntityTypeBuilder<InvF_InventoryInDtl> b)
    {
        b.ToTable("InvF_InventoryInDtl");
        b.HasKey(x => new { x.IF_InvInNo, x.ProductCode });
        b.Property(x => x.IF_InvInNo).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.InvCodeInActual).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.UnitCode).HasMaxLength(50);
        b.Property(x => x.IF_InvInStatusDtl).HasMaxLength(50);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// DDL nguồn trích trong source_excerpt.txt:328-353
/// </summary>
public class Mst_InvInType
{
    public string InvInType { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? InvInTypeName { get; set; }
    public string? FlagActive { get; set; } = "1";
    public string? FlagStatistic { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_InvInTypeConfiguration : IEntityTypeConfiguration<Mst_InvInType>
{
    public void Configure(EntityTypeBuilder<Mst_InvInType> b)
    {
        b.ToTable("Mst_InvInType");
        b.HasKey(x => x.InvInType);
        b.Property(x => x.InvInType).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.InvInTypeName).HasMaxLength(100);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.FlagStatistic).HasMaxLength(10);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// port 1:1 Mst_BusinessGroup (nhóm kinh doanh / chi nhánh).
/// </summary>
public class Mst_BusinessGroup
{
    public string BusinessGroupCode { get; set; } = "";
    public string? BusinessGroupName { get; set; }
    public string? BusinessType { get; set; }
    public string? FlagActive { get; set; } = "1";
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_BusinessGroupConfiguration : IEntityTypeConfiguration<Mst_BusinessGroup>
{
    public void Configure(EntityTypeBuilder<Mst_BusinessGroup> b)
    {
        b.ToTable("Mst_BusinessGroup");
        b.HasKey(x => x.BusinessGroupCode);
        b.Property(x => x.BusinessGroupCode).HasMaxLength(50);
        b.Property(x => x.BusinessGroupName).HasMaxLength(255);
        b.Property(x => x.BusinessType).HasMaxLength(50);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// port 1:1 Mst_Customer (khách hàng / nhà cung cấp).
/// </summary>
public class Mst_Customer
{
    public string CustomerCode { get; set; } = "";
    public string? CustomerName { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; } = "1";
    public string? FlagSupplier { get; set; } = "0";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_CustomerConfiguration : IEntityTypeConfiguration<Mst_Customer>
{
    public void Configure(EntityTypeBuilder<Mst_Customer> b)
    {
        b.ToTable("Mst_Customer");
        b.HasKey(x => x.CustomerCode);
        b.Property(x => x.CustomerCode).HasMaxLength(50);
        b.Property(x => x.CustomerName).HasMaxLength(255);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.FlagSupplier).HasMaxLength(10);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// port 1:1 Mst_NNT (thông tin người nộp thuế / tổ chức chi nhánh).
/// </summary>
public class Mst_NNT
{
    public string MST { get; set; } = "";
    public string? MSTBUPattern { get; set; }
    public string? NNTFullName { get; set; }
    public string? OrgID { get; set; }
    public string? NetworkID { get; set; }
    public string? FlagActive { get; set; } = "1";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_NNTConfiguration : IEntityTypeConfiguration<Mst_NNT>
{
    public void Configure(EntityTypeBuilder<Mst_NNT> b)
    {
        b.ToTable("Mst_NNT");
        b.HasKey(x => x.MST);
        b.Property(x => x.MST).HasMaxLength(50);
        b.Property(x => x.MSTBUPattern).HasMaxLength(50);
        b.Property(x => x.NNTFullName).HasMaxLength(255);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seed demo cho danh mục phụ vụ màn hình Index của InvF_InventoryIn (Order 35).
/// Idempotent, kiểm tra trước khi insert.
/// </summary>
public class InvF_InventoryInMasterSeeder : IPortSeeder
{
    public int Order => 35;

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Mst_InvInType>().AnyAsync())
        {
            db.Set<Mst_InvInType>().AddRange(
                new Mst_InvInType { InvInType = "IN01", NetworkID = "1001", InvInTypeName = "Nhập mua nhà cung cấp", FlagActive = "1", FlagStatistic = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
                new Mst_InvInType { InvInType = "IN02", NetworkID = "1001", InvInTypeName = "Nhập chuyển kho", FlagActive = "1", FlagStatistic = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
                new Mst_InvInType { InvInType = "IN03", NetworkID = "1001", InvInTypeName = "Nhập khác (ngừng)", FlagActive = "0", FlagStatistic = "0", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow }
            );
        }

        if (!await db.Set<Mst_BusinessGroup>().AnyAsync())
        {
            db.Set<Mst_BusinessGroup>().AddRange(
                new Mst_BusinessGroup { BusinessGroupCode = "BG01", BusinessGroupName = "Nhóm DV sửa chữa", BusinessType = "DV", FlagActive = "1", OrgID = "ORG01", NetworkID = "1001", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
                new Mst_BusinessGroup { BusinessGroupCode = "BG02", BusinessGroupName = "Nhóm TM bán lẻ", BusinessType = "TM", FlagActive = "1", OrgID = "ORG01", NetworkID = "1001", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
                new Mst_BusinessGroup { BusinessGroupCode = "BG03", BusinessGroupName = "Nhóm DV ngưng", BusinessType = "DV", FlagActive = "0", OrgID = "ORG01", NetworkID = "1001", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow }
            );
        }

        if (!await db.Set<Mst_Customer>().AnyAsync())
        {
            db.Set<Mst_Customer>().AddRange(
                new Mst_Customer { CustomerCode = "NCC01", CustomerName = "Nhà cung cấp Phụ tùng Á Châu", OrgID = "ORG01", NetworkID = "1001", FlagActive = "1", FlagSupplier = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
                new Mst_Customer { CustomerCode = "NCC02", CustomerName = "Nhà cung cấp Dầu nhớt Total", OrgID = "ORG01", NetworkID = "1001", FlagActive = "1", FlagSupplier = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
                new Mst_Customer { CustomerCode = "KH01", CustomerName = "Khách hàng mua buôn", OrgID = "ORG01", NetworkID = "1001", FlagActive = "1", FlagSupplier = "0", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
                new Mst_Customer { CustomerCode = "NCC03", CustomerName = "Nhà cung cấp cũ (ngừng)", OrgID = "ORG01", NetworkID = "1001", FlagActive = "0", FlagSupplier = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow }
            );
        }

        if (!await db.Set<Mst_NNT>().AnyAsync())
        {
            db.Set<Mst_NNT>().AddRange(
                new Mst_NNT { MST = "0101234567", MSTBUPattern = "0101234567%", NNTFullName = "Công ty TNHH Phụ Tùng Trực Tuyến", OrgID = "ORG01", NetworkID = "1001", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
                new Mst_NNT { MST = "0101234567-001", MSTBUPattern = "0101234567%", NNTFullName = "Chi nhánh Hà Nội 1", OrgID = "ORG01", NetworkID = "1001", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
                new Mst_NNT { MST = "0309999999", MSTBUPattern = "0309999999%", NNTFullName = "Chi nhánh Độc lập", OrgID = "ORG02", NetworkID = "1001", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
                new Mst_NNT { MST = "0101234567-999", MSTBUPattern = "0101234567%", NNTFullName = "Chi nhánh Đã đóng", OrgID = "ORG01", NetworkID = "1001", FlagActive = "0", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow }
            );
        }

        // Seed nghiệp vụ phiếu nhập kho demo (InvF_InventoryIn & InvF_InventoryInDtl)
        if (!await db.Set<InvF_InventoryIn>().AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.Set<InvF_InventoryIn>().AddRange(
                new InvF_InventoryIn
                {
                    IF_InvInNo = "PNK001",
                    OrgID = "ORG01",
                    NetworkID = "1001",
                    InvInType = "IN01",
                    InvCodeIn = "KHO01",
                    CustomerCode = "NCC01",
                    InvoiceNo = "HD001",
                    RefNo = "REF001",
                    BusinessGroupCode = "BG01",
                    IF_InvInStatus = "PENDING",
                    TotalValIn = 1000000,
                    TotalValInAfterDesc = 1100000,
                    TotalValVAT = 100000,
                    CreateDTimeUTC = now.AddDays(-2),
                    CreateBy = "U01",
                    InvoiceDate = now.AddDays(-2),
                    FlagActive = "1",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                },
                new InvF_InventoryIn
                {
                    IF_InvInNo = "PNK002",
                    OrgID = "ORG01",
                    NetworkID = "1001",
                    InvInType = "IN01",
                    InvCodeIn = "KHO01",
                    CustomerCode = "NCC02",
                    InvoiceNo = "HD002",
                    RefNo = "REF002",
                    BusinessGroupCode = "BG01",
                    IF_InvInStatus = "APPROVE",
                    TotalValIn = 2000000,
                    TotalValInAfterDesc = 2200000,
                    TotalValVAT = 200000,
                    CreateDTimeUTC = now.AddDays(-1),
                    CreateBy = "U01",
                    ApprDTimeUTC = now.AddDays(-1),
                    ApprBy = "U01",
                    InvoiceDate = now.AddDays(-1),
                    FlagActive = "1",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                },
                new InvF_InventoryIn
                {
                    IF_InvInNo = "PNK003",
                    OrgID = "ORG01",
                    NetworkID = "1001",
                    InvInType = "IN02",
                    InvCodeIn = "KHO02",
                    CustomerCode = "NCC01",
                    InvoiceNo = "HD003",
                    RefNo = "REF003",
                    BusinessGroupCode = "BG02",
                    IF_InvInStatus = "CANCEL",
                    TotalValIn = 500000,
                    TotalValInAfterDesc = 550000,
                    TotalValVAT = 50000,
                    CreateDTimeUTC = now,
                    CreateBy = "U01",
                    InvoiceDate = now,
                    FlagActive = "1",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                }
            );

            db.Set<InvF_InventoryInDtl>().AddRange(
                new InvF_InventoryInDtl
                {
                    IF_InvInNo = "PNK001",
                    ProductCode = "SP01",
                    InvCodeInActual = "KHO01",
                    NetworkID = "1001",
                    Qty = 10,
                    UPIn = 100000,
                    ValInvIn = 1000000,
                    ValInAfterDesc = 1100000,
                    UnitCode = "U01",
                    IF_InvInStatusDtl = "PENDING",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                },
                new InvF_InventoryInDtl
                {
                    IF_InvInNo = "PNK002",
                    ProductCode = "SP02",
                    InvCodeInActual = "KHO01",
                    NetworkID = "1001",
                    Qty = 20,
                    UPIn = 100000,
                    ValInvIn = 2000000,
                    ValInAfterDesc = 2200000,
                    UnitCode = "U01",
                    IF_InvInStatusDtl = "APPROVE",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                },
                new InvF_InventoryInDtl
                {
                    IF_InvInNo = "PNK003",
                    ProductCode = "SP01",
                    InvCodeInActual = "KHO02",
                    NetworkID = "1001",
                    Qty = 5,
                    UPIn = 100000,
                    ValInvIn = 500000,
                    ValInAfterDesc = 550000,
                    UnitCode = "U01",
                    IF_InvInStatusDtl = "CANCEL",
                    LogLUBy = "seed",
                    LogLUDTimeUTC = now
                }
            );
        }

        await db.SaveChangesAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — InvF_InventoryIn (phiếu nhập kho).
/// Model đại diện cho danh sách trả về trong PageInfo của action Index (dòng 25-28, 47-50).
/// Cột tối thiểu lấy theo các trường chuẩn của chứng từ nhập kho hệ thống Skycic.Inventory.
/// </summary>
public class InvF_InventoryIn
{
    public string IF_InvInNo { get; set; } = "";
    public string? OrgID { get; set; }
    public string? InvInType { get; set; }
    public string? InvCodeIn { get; set; }
    public string? CustomerCode { get; set; }
    public string? InvInDesc { get; set; }
    public string? FlagActive { get; set; } = "1";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class InvF_InventoryInConfiguration : IEntityTypeConfiguration<InvF_InventoryIn>
{
    public void Configure(EntityTypeBuilder<InvF_InventoryIn> b)
    {
        b.ToTable("InvF_InventoryIn");
        b.HasKey(x => x.IF_InvInNo);
        b.Property(x => x.IF_InvInNo).HasMaxLength(50);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.InvInType).HasMaxLength(50);
        b.Property(x => x.InvCodeIn).HasMaxLength(50);
        b.Property(x => x.CustomerCode).HasMaxLength(50);
        b.Property(x => x.InvInDesc).HasMaxLength(500);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// DDL nguồn trích trong source_excerpt.txt:328-353:
/// CREATE TABLE Mst_InvInType (
///        InvInType            Varchar50 NOT NULL,
///        NetworkID            Varchar50 NOT NULL,
///        InvInTypeName        Varchar100 NOT NULL,
///        FlagActive           MyStatus,
///        FlagStatistic        MyStatus,
///        LogLUDTimeUTC        MyDateTime,
///        LogLUBy              Varchar50
/// )
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
/// Cột dùng ở action Index: BusinessType, FlagActive (dòng 41-42, 223-228), OrgID/NetworkID.
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
/// Cột dùng ở action Index: CustomerCode, CustomerName, FlagActive, FlagSupplier (dòng 108).
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
/// Cột dùng ở action Index: MST, MSTBUPattern, NNTFullName, FlagActive (dòng 119).
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

        await db.SaveChangesAsync();
    }
}

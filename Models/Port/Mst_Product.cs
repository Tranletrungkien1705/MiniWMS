using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Mst_Product.cs — Mst_Product (danh mục hàng hoá).
/// Cột lấy theo SQL nguồn của #24 `Inv_CostPriceHist_GetX_New20240916` (Form.cs:200572-200639)
/// cộng với cột join dùng ở #86 `InvF_InventoryIn_GetX` (Form.cs:146951-146957, 147027-147033, 147059-147061).
/// Khoá: (OrgID, ProductCode) — theo đúng cặp cột dùng để join ở nguồn.
/// </summary>
public class Mst_Product
{
    public string OrgID { get; set; } = "";
    public string ProductCode { get; set; } = "";
    /// <summary>Nguồn Form.cs:200576 — `f.ProductCodeUser like '%@strProductCodeUser%'`.</summary>
    public string? ProductCodeUser { get; set; }
    /// <summary>Nguồn Form.cs:200577 — `f.ProductName like '%@strProductName%'`.</summary>
    public string? ProductName { get; set; }
    /// <summary>Nguồn Form.cs:200624 — `mp.UnitCode` (join Mst_Unit theo OrgID + UnitCode).</summary>
    public string? UnitCode { get; set; }
    /// <summary>
    /// Nguồn #27 Inv_CostPriceHistController.cs:332/340 — `ProductLevelSys` (lọc `!= "L2PRD"` ở
    /// strWhereClause_Mst_Product_Get_Base dòng 871, `= "L2PRD"` ở strWhereClause_Mst_Product_Get_Level2 dòng 889).
    /// </summary>
    public string? ProductLevelSys { get; set; }
    /// <summary>
    /// Nguồn #27 Inv_CostPriceHistController.cs:336 — `ProductCodeRoot` (lấy từ dòng đầu của Get_Base, dùng làm
    /// điều kiện `ProductCodeRoot = productcode` ở strWhereClause_Mst_Product_Get_Level2 dòng 885).
    /// </summary>
    public string? ProductCodeRoot { get; set; }
    /// <summary>Nguồn #86 Form.cs:146954 — `mp.ProductCodeBase` (join `iiif.OrgID = mp.OrgID and iiifdt.ProductCode = mp.ProductCode`; dùng cho gom UI).</summary>
    public string? ProductCodeBase { get; set; }
    /// <summary>Nguồn #86 Form.cs:146955 — `mp.FlagLot`.</summary>
    public string? FlagLot { get; set; }
    /// <summary>Nguồn #86 Form.cs:146956 — `mp.FlagSerial`.</summary>
    public string? FlagSerial { get; set; }
    /// <summary>Nguồn #86 Form.cs:146957 — `mp.ValConvert` (double vì SQLite SUM/đọc số).</summary>
    public double? ValConvert { get; set; }
}

public class Mst_ProductConfiguration : IEntityTypeConfiguration<Mst_Product>
{
    public void Configure(EntityTypeBuilder<Mst_Product> b)
    {
        b.ToTable("Mst_Product");
        b.HasKey(x => new { x.OrgID, x.ProductCode });
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.ProductCodeUser).HasMaxLength(50);
        b.Property(x => x.ProductName).HasMaxLength(255);
        b.Property(x => x.UnitCode).HasMaxLength(50);
        b.Property(x => x.ProductLevelSys).HasMaxLength(50);
        b.Property(x => x.ProductCodeRoot).HasMaxLength(50);
        b.Property(x => x.ProductCodeBase).HasMaxLength(50);
        b.Property(x => x.FlagLot).HasMaxLength(10);
        b.Property(x => x.FlagSerial).HasMaxLength(10);
    }
}

/// <summary>
/// Seed demo Mst_Product (Order 14 — danh mục hàng hoá, sau Mst_Sys_Config Order 13).
/// Dữ liệu khớp với seed của #24: Mst_Inventory (KHO01/KHO02/KHO03), Mst_Unit (U01/U02), Inv_CostPriceHist (Order 200).
/// #27 LoadData: thêm 2 dòng cấp 2 (ProductLevelSys = "L2PRD", ProductCodeRoot = "SP01") để có ca lọc ra số dòng khác nhau.
/// #86 bổ sung `ProductCodeBase`/`FlagLot`/`FlagSerial`/`ValConvert` để port Detail gom UI đúng như nguồn.
/// </summary>
public class Mst_ProductSeeder : IPortSeeder
{
    public int Order => 14;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Product>().AnyAsync()) return;
        db.Set<Mst_Product>().AddRange(
            new Mst_Product { OrgID = "ORG01", ProductCode = "SP01", ProductCodeUser = "PT-LOC-DAU", ProductName = "Lọc dầu động cơ", UnitCode = "U01", ProductCodeBase = "SP01", FlagLot = "1", FlagSerial = "0", ValConvert = 1 },
            new Mst_Product { OrgID = "ORG01", ProductCode = "SP02", ProductCodeUser = "PT-LOC-GIO", ProductName = "Lọc gió động cơ", UnitCode = "U01", ProductCodeBase = "SP02", FlagLot = "1", FlagSerial = "0", ValConvert = 1 },
            new Mst_Product { OrgID = "ORG01", ProductCode = "SP03", ProductCodeUser = "PT-BUGI", ProductName = "Bugi đánh lửa", UnitCode = "U02", ProductCodeBase = "SP03", FlagLot = "0", FlagSerial = "1", ValConvert = 1 },
            new Mst_Product { OrgID = "ORG02", ProductCode = "SP01", ProductCodeUser = "PT-LOC-DAU", ProductName = "Lọc dầu động cơ", UnitCode = "U01", ProductCodeBase = "SP01", FlagLot = "1", FlagSerial = "0", ValConvert = 1 },
            new Mst_Product { OrgID = "ORG01", ProductCode = "SP01-L2A", ProductCodeUser = "PT-LOC-DAU-L2A", ProductName = "Lọc dầu động cơ (cấp 2)", UnitCode = "U01", ProductLevelSys = "L2PRD", ProductCodeRoot = "SP01", ProductCodeBase = "SP01", FlagLot = "1", FlagSerial = "0", ValConvert = 1 },
            new Mst_Product { OrgID = "ORG01", ProductCode = "SP01-L2B", ProductCodeUser = "PT-LOC-DAU-L2B", ProductName = "Lọc dầu động cơ (cấp 2)", UnitCode = "U01", ProductLevelSys = "L2PRD", ProductCodeRoot = "SP01", ProductCodeBase = "SP01", FlagLot = "1", FlagSerial = "0", ValConvert = 1 }
        );
        await db.SaveChangesAsync();
    }
}

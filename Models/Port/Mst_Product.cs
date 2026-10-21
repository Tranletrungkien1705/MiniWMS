using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Mst_Product.cs — Mst_Product (danh mục hàng hoá).
/// Cột lấy theo SQL nguồn của #24 `Inv_CostPriceHist_GetX_New20240916` (Form.cs:200572-200639):
///   - join `Mst_Product f on t.ProductCode = f.ProductCode and t.OrgID = f.OrgID` (dòng 200572-200574);
///   - lọc `f.ProductCodeUser like '%x%'` (dòng 200576), `f.ProductName like '%x%'` (dòng 200577);
///   - chiếu `mp.ProductCodeUser`, `mp.ProductName`, `mp.UnitCode` (dòng 200622-200624).
/// Khoá: (OrgID, ProductCode) — theo đúng cặp cột dùng để join ở nguồn.
/// GAP: model/DDL đầy đủ của Mst_Product KHÔNG có trong excerpt #24 ⇒ chỉ khai các cột nguồn dùng tới.
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
    }
}

/// <summary>
/// Seed demo Mst_Product (Order 14 — danh mục hàng hoá, sau Mst_Sys_Config Order 13).
/// Dữ liệu khớp với seed của #24: Mst_Inventory (KHO01/KHO02/KHO03), Mst_Unit (U01/U02), Inv_CostPriceHist (Order 200).
/// </summary>
public class Mst_ProductSeeder : IPortSeeder
{
    public int Order => 14;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Product>().AnyAsync()) return;
        db.Set<Mst_Product>().AddRange(
            new Mst_Product { OrgID = "ORG01", ProductCode = "SP01", ProductCodeUser = "PT-LOC-DAU", ProductName = "Lọc dầu động cơ", UnitCode = "U01" },
            new Mst_Product { OrgID = "ORG01", ProductCode = "SP02", ProductCodeUser = "PT-LOC-GIO", ProductName = "Lọc gió động cơ", UnitCode = "U01" },
            new Mst_Product { OrgID = "ORG01", ProductCode = "SP03", ProductCodeUser = "PT-BUGI", ProductName = "Bugi đánh lửa", UnitCode = "U02" },
            new Mst_Product { OrgID = "ORG02", ProductCode = "SP01", ProductCodeUser = "PT-LOC-DAU", ProductName = "Lọc dầu động cơ", UnitCode = "U01" }
        );
        await db.SaveChangesAsync();
    }
}

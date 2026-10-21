using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Mst_Unit.cs — Mst_Unit (đơn vị tính).
/// Cột lấy theo SQL nguồn của #24 `Inv_CostPriceHist_GetX_New20240916` (Form.cs:200637-200639):
///   `left join Mst_Unit mu on t.OrgID = mu.OrgID and mp.UnitCode = mu.UnitCode`, chiếu `mu.UnitName` (dòng 200625).
/// Khoá: (OrgID, UnitCode) — theo đúng cặp cột dùng để join ở nguồn.
/// GAP: model/DDL đầy đủ của Mst_Unit KHÔNG có trong excerpt #24 ⇒ chỉ khai các cột nguồn dùng tới.
/// </summary>
public class Mst_Unit
{
    public string OrgID { get; set; } = "";
    public string UnitCode { get; set; } = "";
    /// <summary>Nguồn Form.cs:200625 — `mu.UnitName`.</summary>
    public string? UnitName { get; set; }
}

public class Mst_UnitConfiguration : IEntityTypeConfiguration<Mst_Unit>
{
    public void Configure(EntityTypeBuilder<Mst_Unit> b)
    {
        b.ToTable("Mst_Unit");
        b.HasKey(x => new { x.OrgID, x.UnitCode });
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.UnitCode).HasMaxLength(50);
        b.Property(x => x.UnitName).HasMaxLength(255);
    }
}

/// <summary>Seed demo Mst_Unit (Order 15 — danh mục đơn vị tính, sau Mst_Product Order 14).</summary>
public class Mst_UnitSeeder : IPortSeeder
{
    public int Order => 15;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Unit>().AnyAsync()) return;
        db.Set<Mst_Unit>().AddRange(
            new Mst_Unit { OrgID = "ORG01", UnitCode = "U01", UnitName = "Cái" },
            new Mst_Unit { OrgID = "ORG01", UnitCode = "U02", UnitName = "Bộ" },
            new Mst_Unit { OrgID = "ORG02", UnitCode = "U01", UnitName = "Cái" }
        );
        await db.SaveChangesAsync();
    }
}

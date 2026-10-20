using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Mst_InventoryLevelType.cs — Mst_InventoryLevelType.
/// Cột lấy theo model nguồn D:\idocNet\...\Common\Models\Mst_InventoryLevelType.cs:8-21.
/// PK nguồn: (OrgID, InvLevelType).
/// </summary>
public class Mst_InventoryLevelType
{
    public string OrgID { get; set; } = "";
    public string InvLevelType { get; set; } = "";
    public string? InvLevelTypeName { get; set; }
    public string? FlagActive { get; set; } = "1";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_InventoryLevelTypeConfiguration : IEntityTypeConfiguration<Mst_InventoryLevelType>
{
    public void Configure(EntityTypeBuilder<Mst_InventoryLevelType> b)
    {
        b.ToTable("Mst_InventoryLevelType");
        b.HasKey(x => new { x.OrgID, x.InvLevelType });
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.InvLevelType).HasMaxLength(50);
        b.Property(x => x.InvLevelTypeName).HasMaxLength(255);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>Seed demo Mst_InventoryLevelType (Order 11 — danh mục cấp kho, sau Mst_Org Order 10).</summary>
public class Mst_InventoryLevelTypeSeeder : IPortSeeder
{
    public int Order => 11;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_InventoryLevelType>().AnyAsync()) return;
        db.Set<Mst_InventoryLevelType>().AddRange(
            new Mst_InventoryLevelType { OrgID = "ORG01", InvLevelType = "LEVEL01", InvLevelTypeName = "Cấp 1 - Khu A", FlagActive = "1", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
            new Mst_InventoryLevelType { OrgID = "ORG01", InvLevelType = "LEVEL02", InvLevelTypeName = "Cấp 2 - Khu B", FlagActive = "1", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
            new Mst_InventoryLevelType { OrgID = "ORG02", InvLevelType = "LEVEL01", InvLevelTypeName = "Cấp 1 - Kho HCM", FlagActive = "0", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
            new Mst_InventoryLevelType { OrgID = "ORG03", InvLevelType = "LEVEL03", InvLevelTypeName = "Cấp 3 - Chi nhánh ngừng", FlagActive = "1", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}

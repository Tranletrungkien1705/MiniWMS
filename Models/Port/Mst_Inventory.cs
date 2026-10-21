using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Mst_Inventory.cs — Mst_Inventory (danh sách kho).
/// Cột lấy theo model nguồn (excerpt #13: "MODEL: khong thay Mst_Inventory.cs ... dung MODEL lam tu dien cot";
/// FK nguồn `Mst_Dealer.Mst_PartMaterialType.sql:69 REFERENCES Mst_Inventory (InvCode)` ⇒ PK = InvCode).
/// Gate DashboardRptController.cs:242 lọc `Mst_Inventory.FlagIn_Out = '1'` và chỉ lấy cột `Mst_Inventory.InvCode` (dòng 262).
/// </summary>
public class Mst_Inventory
{
    public string InvCode { get; set; } = "";
    public string? InvName { get; set; }
    /// <summary>Nguồn DashboardRptController.cs:242 — cờ kho nhập/xuất, lọc `= '1'`.</summary>
    public string? FlagIn_Out { get; set; }
    public string? FlagActive { get; set; } = "1";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_InventoryConfiguration : IEntityTypeConfiguration<Mst_Inventory>
{
    public void Configure(EntityTypeBuilder<Mst_Inventory> b)
    {
        b.ToTable("Mst_Inventory");
        b.HasKey(x => x.InvCode);
        b.Property(x => x.InvCode).HasMaxLength(50);
        b.Property(x => x.InvName).HasMaxLength(255);
        b.Property(x => x.FlagIn_Out).HasMaxLength(10);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>Seed demo Mst_Inventory (Order 12 — danh mục kho, sau Mst_InventoryLevelType Order 11).</summary>
public class Mst_InventorySeeder : IPortSeeder
{
    public int Order => 12;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Inventory>().AnyAsync()) return;
        db.Set<Mst_Inventory>().AddRange(
            new Mst_Inventory { InvCode = "KHO01", InvName = "Kho trung tâm Hà Nội", FlagIn_Out = "1", FlagActive = "1", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
            new Mst_Inventory { InvCode = "KHO02", InvName = "Kho chi nhánh TP.HCM", FlagIn_Out = "1", FlagActive = "1", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
            new Mst_Inventory { InvCode = "KHO03", InvName = "Kho chỉ xuất (không nhập)", FlagIn_Out = "0", FlagActive = "1", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}

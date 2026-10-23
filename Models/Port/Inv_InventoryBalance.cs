using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Inv_InventoryBalance.cs — Inv_InventoryBalance (tồn kho tổng hợp).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Common\Models\Inv_InventoryBalance.cs:9-37
/// Cột: OrgID, InvCode, ProductCode, NetworkID, QtyTotalOK, QtyBlockOK, QtyAvailOK, QtyTotalNG, QtyBlockNG, QtyAvailNG, LogLUDTimeUTC, LogLUBy, UPInv, TotalValInv.
/// Dùng trong InvF_InventoryOutController.GetTonKho (dòng 3616-3659).
/// </summary>
public class Inv_InventoryBalance
{
    public long AutoID { get; set; }
    public string? OrgID { get; set; }
    public string? InvCode { get; set; }
    public string? ProductCode { get; set; }
    public string? NetworkID { get; set; }
    public double? QtyTotalOK { get; set; }
    public double? QtyBlockOK { get; set; }
    public double? QtyAvailOK { get; set; }
    public double? QtyTotalNG { get; set; }
    public double? QtyBlockNG { get; set; }
    public double? QtyAvailNG { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public double? UPInv { get; set; }
    public double? TotalValInv { get; set; }
}

public class Inv_InventoryBalanceConfiguration : IEntityTypeConfiguration<Inv_InventoryBalance>
{
    public void Configure(EntityTypeBuilder<Inv_InventoryBalance> b)
    {
        b.ToTable("Inv_InventoryBalance");
        b.HasKey(x => x.AutoID);
        b.Property(x => x.AutoID).ValueGeneratedOnAdd();
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.InvCode).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seed demo Inv_InventoryBalance (Order 43 — sau InvF_InventoryOutInstLot Order 42).
/// Phục vụ kiểm thử GetTonKho: tính tồn quy đổi, filter ProductCode, InvBUPattern, QtyTotalOK > 0.
/// </summary>
public class Inv_InventoryBalanceSeeder : IPortSeeder
{
    public int Order => 43;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Inv_InventoryBalance>().AnyAsync()) return;
        db.Set<Inv_InventoryBalance>().AddRange(
            new Inv_InventoryBalance
            {
                OrgID = "ORG01",
                InvCode = "KHO01", // InvBUPattern = HN%
                ProductCode = "BASE01",
                NetworkID = "1001",
                QtyTotalOK = 100,
                QtyAvailOK = 100,
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            },
            new Inv_InventoryBalance
            {
                OrgID = "ORG01",
                InvCode = "KHO02", // InvBUPattern = HCM%
                ProductCode = "BASE01",
                NetworkID = "1001",
                QtyTotalOK = 50,
                QtyAvailOK = 50,
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            },
            new Inv_InventoryBalance
            {
                OrgID = "ORG01",
                InvCode = "KHO01", // InvBUPattern = HN%
                ProductCode = "BASE02",
                NetworkID = "1001",
                QtyTotalOK = 80,
                QtyAvailOK = 80,
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            },
            new Inv_InventoryBalance
            {
                OrgID = "ORG01",
                InvCode = "KHO01",
                ProductCode = "BASE01",
                NetworkID = "1001",
                QtyTotalOK = 0, // Bị lọc bởi QtyTotalOK > 0
                QtyAvailOK = 0,
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            }
        );
        await db.SaveChangesAsync();
    }
}

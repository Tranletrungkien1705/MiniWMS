using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Inv_InventoryBalanceLot.cs — Inv_InventoryBalanceLot (tồn kho theo lô).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Common\Models\Inv_InventoryBalanceLot.cs:9-42
/// Cột: OrgID, InvCode, ProductCode, ProductLotNo, NetworkID, QtyTotalOK, QtyBlockOK, QtyAvailOK,
///      QtyTotalNG, QtyBlockNG, QtyAvailNG, ProductionDate, ExpiredDate, ValDateExpired, LogLUDTimeUTC, LogLUBy.
/// Dùng trong InvF_InventoryOutController.ShowLoDetail (dòng 2140-2208).
/// </summary>
public class Inv_InventoryBalanceLot
{
    public long AutoID { get; set; }
    public string? OrgID { get; set; }
    public string? InvCode { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductLotNo { get; set; }
    public string? NetworkID { get; set; }
    public double? QtyTotalOK { get; set; }
    public double? QtyBlockOK { get; set; }
    public double? QtyAvailOK { get; set; }
    public double? QtyTotalNG { get; set; }
    public double? QtyBlockNG { get; set; }
    public double? QtyAvailNG { get; set; }
    public string? ProductionDate { get; set; }
    public string? ExpiredDate { get; set; }
    public string? ValDateExpired { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Inv_InventoryBalanceLotConfiguration : IEntityTypeConfiguration<Inv_InventoryBalanceLot>
{
    public void Configure(EntityTypeBuilder<Inv_InventoryBalanceLot> b)
    {
        b.ToTable("Inv_InventoryBalanceLot");
        b.HasKey(x => x.AutoID);
        b.Property(x => x.AutoID).ValueGeneratedOnAdd();
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.InvCode).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.ProductLotNo).HasMaxLength(100);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.ProductionDate).HasMaxLength(50);
        b.Property(x => x.ExpiredDate).HasMaxLength(50);
        b.Property(x => x.ValDateExpired).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seed demo Inv_InventoryBalanceLot (Order 41).
/// </summary>
public class Inv_InventoryBalanceLotSeeder : IPortSeeder
{
    public int Order => 41;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Inv_InventoryBalanceLot>().AnyAsync()) return;
        db.Set<Inv_InventoryBalanceLot>().AddRange(
            new Inv_InventoryBalanceLot
            {
                OrgID = "ORG01",
                InvCode = "KHO01",
                ProductCode = "BASE_P01",
                ProductLotNo = "LOT001",
                NetworkID = "1001",
                QtyTotalOK = 100,
                QtyBlockOK = 0,
                QtyAvailOK = 100,
                ProductionDate = "2026-01-01",
                ExpiredDate = "2027-01-01",
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            },
            new Inv_InventoryBalanceLot
            {
                OrgID = "ORG01",
                InvCode = "KHO01",
                ProductCode = "BASE_P01",
                ProductLotNo = "LOT002",
                NetworkID = "1001",
                QtyTotalOK = 50,
                QtyBlockOK = 0,
                QtyAvailOK = 50,
                ProductionDate = "2026-02-01",
                ExpiredDate = "2027-02-01",
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            },
            new Inv_InventoryBalanceLot
            {
                OrgID = "ORG01",
                InvCode = "KHO02",
                ProductCode = "BASE_P01",
                ProductLotNo = "LOT003",
                NetworkID = "1001",
                QtyTotalOK = 80,
                QtyBlockOK = 0,
                QtyAvailOK = 80,
                ProductionDate = "2026-03-01",
                ExpiredDate = "2027-03-01",
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            },
            new Inv_InventoryBalanceLot
            {
                OrgID = "ORG01",
                InvCode = "KHO01",
                ProductCode = "BASE_P02",
                ProductLotNo = "LOT004",
                NetworkID = "1001",
                QtyTotalOK = 0, // Tồn kho = 0 để kiểm tra guard QtyTotalOK > 0
                QtyBlockOK = 0,
                QtyAvailOK = 0,
                ProductionDate = "2026-01-01",
                ExpiredDate = "2027-01-01",
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            }
        );
        await db.SaveChangesAsync();
    }
}

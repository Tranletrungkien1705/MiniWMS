using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 InvF_InventoryInQR.cs — InvF_InventoryInQR.
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Common\Models\InvF_InventoryInQR.cs:9-39
/// </summary>
public class InvF_InventoryInQR
{
    public string IF_InvInNo { get; set; } = "";
    public string QRCode { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? ProductCode { get; set; }
    public string? BoxNo { get; set; }
    public string? CanNo { get; set; }
    public string? ProductLotNo { get; set; }
    public string? ShiftInCode { get; set; }
    public string? UserKCS { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }

    public string? mp_ProductCode { get; set; }
    public string? mp_Productname { get; set; }
    public string? mp_ProductCodeUser { get; set; }
}

public class InvF_InventoryInQRConfiguration : IEntityTypeConfiguration<InvF_InventoryInQR>
{
    public void Configure(EntityTypeBuilder<InvF_InventoryInQR> b)
    {
        b.ToTable("InvF_InventoryInQR");
        b.HasKey(x => new { x.IF_InvInNo, x.QRCode });
        b.Property(x => x.IF_InvInNo).HasMaxLength(50);
        b.Property(x => x.QRCode).HasMaxLength(100);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.BoxNo).HasMaxLength(50);
        b.Property(x => x.CanNo).HasMaxLength(50);
        b.Property(x => x.ProductLotNo).HasMaxLength(100);
        b.Property(x => x.ShiftInCode).HasMaxLength(50);
        b.Property(x => x.UserKCS).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
        b.Property(x => x.mp_ProductCode).HasMaxLength(50);
        b.Property(x => x.mp_Productname).HasMaxLength(255);
        b.Property(x => x.mp_ProductCodeUser).HasMaxLength(50);
    }
}

/// <summary>
/// Seed demo cho InvF_InventoryInQR (Order 39).
/// </summary>
public class InvF_InventoryInQRSeeder : IPortSeeder
{
    public int Order => 39;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<InvF_InventoryInQR>().AnyAsync()) return;

        db.Set<InvF_InventoryInQR>().AddRange(
            new InvF_InventoryInQR
            {
                IF_InvInNo = "PNK001",
                QRCode = "QR001",
                NetworkID = "1001",
                ProductCode = "SP01",
                ProductLotNo = "LOT001",
                BoxNo = "BOX01",
                CanNo = "CAN01",
                LogLUBy = "seed",
                LogLUDTimeUTC = DateTime.UtcNow,
                mp_ProductCode = "SP01",
                mp_Productname = "Lọc dầu động cơ",
                mp_ProductCodeUser = "PT-LOC-DAU"
            }
        );
        await db.SaveChangesAsync();
    }
}

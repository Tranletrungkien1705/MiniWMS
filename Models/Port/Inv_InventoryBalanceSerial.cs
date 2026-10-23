using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Inv_InventoryBalanceSerial.cs — Inv_InventoryBalanceSerial (serial tồn kho).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Common\Models\Inv_InventoryBalanceSerial.cs:9-35
/// Cột: OrgID, InvCode, ProductCode, SerialNo, NetworkID, ProductLotNo, RefNo_Type, RefNo_PK, BlockStatus, FlagNG, LogLUDTimeUTC, LogLUBy.
/// Không có DDL trong nguồn → dùng AutoID surrogate (mẫu InvF_InventoryOutAttachFile.cs:13).
/// </summary>
public class Inv_InventoryBalanceSerial
{
    public long AutoID { get; set; }
    /// <summary>Nguồn dòng 11 — OrgID.</summary>
    public string? OrgID { get; set; }
    /// <summary>Nguồn dòng 13 — Mã kho; join với Mst_Inventory.InvCode (Inventory.cs:6351-6353).</summary>
    public string? InvCode { get; set; }
    /// <summary>Nguồn dòng 15 — Mã sản phẩm (gate 2030-2032 lọc `= '<ProductCode>'`).</summary>
    public string? ProductCode { get; set; }
    /// <summary>Nguồn dòng 17 — SerialNo.</summary>
    public string? SerialNo { get; set; }
    public string? NetworkID { get; set; }
    public string? ProductLotNo { get; set; }
    public string? RefNo_Type { get; set; }
    public string? RefNo_PK { get; set; }
    public string? BlockStatus { get; set; }
    public string? FlagNG { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Inv_InventoryBalanceSerialConfiguration : IEntityTypeConfiguration<Inv_InventoryBalanceSerial>
{
    public void Configure(EntityTypeBuilder<Inv_InventoryBalanceSerial> b)
    {
        b.ToTable("Inv_InventoryBalanceSerial");
        b.HasKey(x => x.AutoID);
        b.Property(x => x.AutoID).ValueGeneratedOnAdd();
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.InvCode).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.SerialNo).HasMaxLength(100);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.ProductLotNo).HasMaxLength(50);
        b.Property(x => x.RefNo_Type).HasMaxLength(50);
        b.Property(x => x.RefNo_PK).HasMaxLength(100);
        b.Property(x => x.BlockStatus).HasMaxLength(50);
        b.Property(x => x.FlagNG).HasMaxLength(10);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seed demo Inv_InventoryBalanceSerial (Order 18).
/// Cố ý seed: PC1 có ở KHO01 (HN%) và KHO02 (HCM%); PC2 chỉ ở KHO01 → phân biệt được ca `invBUPattern` + `ProductCode`.
/// </summary>
public class Inv_InventoryBalanceSerialSeeder : IPortSeeder
{
    public int Order => 18;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Inv_InventoryBalanceSerial>().AnyAsync()) return;
        db.Set<Inv_InventoryBalanceSerial>().AddRange(
            new Inv_InventoryBalanceSerial { OrgID = "ORG01", InvCode = "KHO01", ProductCode = "PC1", SerialNo = "SN1", NetworkID = "1001", FlagNG = "0", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
            new Inv_InventoryBalanceSerial { OrgID = "ORG01", InvCode = "KHO01", ProductCode = "PC1", SerialNo = "SN2", NetworkID = "1001", FlagNG = "0", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
            new Inv_InventoryBalanceSerial { OrgID = "ORG01", InvCode = "KHO01", ProductCode = "PC2", SerialNo = "SN3", NetworkID = "1001", FlagNG = "0", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
            new Inv_InventoryBalanceSerial { OrgID = "ORG01", InvCode = "KHO02", ProductCode = "PC1", SerialNo = "SN4", NetworkID = "1001", FlagNG = "0", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}

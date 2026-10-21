using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 InvF_InventoryInInstLot.cs — InvF_InventoryInInstLot.
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Common\Models\InvF_InventoryInInstLot.cs:9-52
/// Dùng trong InvF_InventoryInController.ShowLo (dòng 445-450) để nhận và giải tuần tự hóa danh sách LOT.
/// </summary>
public class InvF_InventoryInInstLot
{
    public string? IF_InvInNo { get; set; }
    public string? InvCodeInActual { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductLotNo { get; set; }
    public string? NetworkID { get; set; }
    public double? Qty { get; set; }
    public string? ProductionDate { get; set; }
    public string? ExpiredDate { get; set; }
    public string? ValDateExpired { get; set; }
    public string? Remark { get; set; }
    public string? IF_InvInILStatus { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? mp_ProductCode { get; set; }
    public string? mp_ProductName { get; set; }
    public string? mp_ProductCodeUser { get; set; }
    public string? mp_ProductCodeBase { get; set; }
    public string? mp_FlagLot { get; set; }
    public string? mp_FlagSerial { get; set; }
    public double? mp_ValConvert { get; set; }
}

public class InvF_InventoryInInstLotConfiguration : IEntityTypeConfiguration<InvF_InventoryInInstLot>
{
    public void Configure(EntityTypeBuilder<InvF_InventoryInInstLot> b)
    {
        b.ToTable("InvF_InventoryInInstLot");
        b.HasKey(x => new { x.IF_InvInNo, x.ProductCode, x.ProductLotNo });
        b.Property(x => x.IF_InvInNo).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.ProductLotNo).HasMaxLength(100);
        b.Property(x => x.InvCodeInActual).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.ProductionDate).HasMaxLength(50);
        b.Property(x => x.ExpiredDate).HasMaxLength(50);
        b.Property(x => x.ValDateExpired).HasMaxLength(50);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.Property(x => x.IF_InvInILStatus).HasMaxLength(50);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
        b.Property(x => x.mp_ProductCode).HasMaxLength(50);
        b.Property(x => x.mp_ProductName).HasMaxLength(255);
        b.Property(x => x.mp_ProductCodeUser).HasMaxLength(50);
        b.Property(x => x.mp_ProductCodeBase).HasMaxLength(50);
        b.Property(x => x.mp_FlagLot).HasMaxLength(10);
        b.Property(x => x.mp_FlagSerial).HasMaxLength(10);
    }
}

/// <summary>
/// Seed demo cho InvF_InventoryInInstLot (Order 36 - sau InvF_InventoryIn Order 35).
/// </summary>
public class InvF_InventoryInInstLotSeeder : IPortSeeder
{
    public int Order => 36;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<InvF_InventoryInInstLot>().AnyAsync()) return;

        db.Set<InvF_InventoryInInstLot>().AddRange(
            new InvF_InventoryInInstLot
            {
                IF_InvInNo = "IN20261021-0001",
                InvCodeInActual = "KHO01",
                ProductCode = "PRD001",
                ProductLotNo = "LOT202610-01",
                NetworkID = "1001",
                Qty = 100,
                ProductionDate = "2026-10-01",
                ExpiredDate = "2027-10-01",
                ValDateExpired = "365",
                Remark = "Lô hàng mẫu 01",
                IF_InvInILStatus = "PENDING",
                LogLUBy = "seed",
                LogLUDTimeUTC = DateTime.UtcNow,
                mp_ProductCode = "PRD001",
                mp_ProductName = "Sản phẩm A",
                mp_ProductCodeUser = "SP01",
                mp_ProductCodeBase = "PRD001",
                mp_FlagLot = "1",
                mp_FlagSerial = "0",
                mp_ValConvert = 1
            },
            new InvF_InventoryInInstLot
            {
                IF_InvInNo = "IN20261021-0001",
                InvCodeInActual = "KHO01",
                ProductCode = "PRD001",
                ProductLotNo = "LOT202610-02",
                NetworkID = "1001",
                Qty = 50,
                ProductionDate = "2026-10-05",
                ExpiredDate = "2027-10-05",
                ValDateExpired = "365",
                Remark = "Lô hàng mẫu 02",
                IF_InvInILStatus = "PENDING",
                LogLUBy = "seed",
                LogLUDTimeUTC = DateTime.UtcNow,
                mp_ProductCode = "PRD001",
                mp_ProductName = "Sản phẩm A",
                mp_ProductCodeUser = "SP01",
                mp_ProductCodeBase = "PRD001",
                mp_FlagLot = "1",
                mp_FlagSerial = "0",
                mp_ValConvert = 1
            }
        );
        await db.SaveChangesAsync();
    }
}

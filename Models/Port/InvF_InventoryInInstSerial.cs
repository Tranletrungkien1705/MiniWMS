using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 InvF_InventoryInInstSerial.cs — InvF_InventoryInInstSerial.
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Common\Models\InvF_InventoryInInstSerial.cs:9-42
/// Dùng trong InvF_InventoryInController.ShowSerial (dòng 495-500) để nhận và giải tuần tự hóa danh sách Serial.
/// </summary>
public class InvF_InventoryInInstSerial
{
    public string? IF_InvInNo { get; set; }
    public string? InvCodeInActual { get; set; }
    public string? ProductCode { get; set; }
    public string? SerialNo { get; set; }
    public string? NetworkID { get; set; }
    public string? IF_InvInISStatus { get; set; }
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

public class InvF_InventoryInInstSerialConfiguration : IEntityTypeConfiguration<InvF_InventoryInInstSerial>
{
    public void Configure(EntityTypeBuilder<InvF_InventoryInInstSerial> b)
    {
        b.ToTable("InvF_InventoryInInstSerial");
        b.HasKey(x => new { x.IF_InvInNo, x.ProductCode, x.SerialNo });
        b.Property(x => x.IF_InvInNo).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.SerialNo).HasMaxLength(100);
        b.Property(x => x.InvCodeInActual).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.IF_InvInISStatus).HasMaxLength(50);
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
/// Seed demo cho InvF_InventoryInInstSerial (Order 37 - sau InvF_InventoryInInstLot Order 36).
/// </summary>
public class InvF_InventoryInInstSerialSeeder : IPortSeeder
{
    public int Order => 37;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<InvF_InventoryInInstSerial>().AnyAsync()) return;

        db.Set<InvF_InventoryInInstSerial>().AddRange(
            new InvF_InventoryInInstSerial
            {
                IF_InvInNo = "IN20261021-0001",
                InvCodeInActual = "KHO01",
                ProductCode = "PRD002",
                SerialNo = "SN202610-001",
                NetworkID = "1001",
                IF_InvInISStatus = "PENDING",
                LogLUBy = "seed",
                LogLUDTimeUTC = DateTime.UtcNow,
                mp_ProductCode = "PRD002",
                mp_ProductName = "Sản phẩm B",
                mp_ProductCodeUser = "SP02",
                mp_ProductCodeBase = "PRD002",
                mp_FlagLot = "0",
                mp_FlagSerial = "1",
                mp_ValConvert = 1
            },
            new InvF_InventoryInInstSerial
            {
                IF_InvInNo = "IN20261021-0001",
                InvCodeInActual = "KHO01",
                ProductCode = "PRD002",
                SerialNo = "SN202610-002",
                NetworkID = "1001",
                IF_InvInISStatus = "PENDING",
                LogLUBy = "seed",
                LogLUDTimeUTC = DateTime.UtcNow,
                mp_ProductCode = "PRD002",
                mp_ProductName = "Sản phẩm B",
                mp_ProductCodeUser = "SP02",
                mp_ProductCodeBase = "PRD002",
                mp_FlagLot = "0",
                mp_FlagSerial = "1",
                mp_ValConvert = 1
            }
        );
        await db.SaveChangesAsync();
    }
}

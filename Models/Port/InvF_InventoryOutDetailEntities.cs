using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port
{
    public class InvF_InventoryOutCover
    {
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? mp_root_ProductCodeBase { get; set; }
    }

    public class InvF_InventoryOutCoverConfiguration : IEntityTypeConfiguration<InvF_InventoryOutCover>
    {
        public void Configure(EntityTypeBuilder<InvF_InventoryOutCover> builder)
        {
            builder.ToTable("InvF_InventoryOutCover");
            builder.HasKey(x => new { x.IF_InvOutNo, x.ProductCode });
        }
    }

    /// <summary>
    /// port 1:1 InvF_InventoryOutInstLot.cs — InvF_InventoryOutInstLot (lô phiếu xuất).
    /// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Common\Models\InvF_InventoryOutInstLot.cs:9-41.
    /// Dùng trong InvF_InventoryOutController.ShowLoDetail (dòng 2134-2273).
    /// </summary>
    public class InvF_InventoryOutInstLot
    {
        public string? IF_InvOutNo { get; set; }
        public string? InvCodeOutActual { get; set; }
        public string? ProductCodeRoot { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductLotNo { get; set; }
        public string? LotNo { get; set; }
        public string? NetworkID { get; set; }
        public double? Qty { get; set; }
        public string? ExpiredDate { get; set; }
        public string? ProductionDate { get; set; }
        public string? IF_InvOutILStatus { get; set; }
        public DateTime? LogLUDTimeUTC { get; set; }
        public string? LogLUBy { get; set; }
        public double? QtyInv { get; set; }
        public double? ValConvert { get; set; }
    }

    public class InvF_InventoryOutInstLotConfiguration : IEntityTypeConfiguration<InvF_InventoryOutInstLot>
    {
        public void Configure(EntityTypeBuilder<InvF_InventoryOutInstLot> builder)
        {
            builder.ToTable("InvF_InventoryOutInstLot");
            builder.HasKey(x => new { x.IF_InvOutNo, x.ProductCode, x.ProductLotNo });
            builder.Property(x => x.IF_InvOutNo).HasMaxLength(50);
            builder.Property(x => x.ProductCode).HasMaxLength(50);
            builder.Property(x => x.ProductLotNo).HasMaxLength(100);
            builder.Property(x => x.LotNo).HasMaxLength(100);
            builder.Property(x => x.ProductCodeRoot).HasMaxLength(50);
            builder.Property(x => x.InvCodeOutActual).HasMaxLength(50);
            builder.Property(x => x.NetworkID).HasMaxLength(50);
            builder.Property(x => x.ExpiredDate).HasMaxLength(50);
            builder.Property(x => x.ProductionDate).HasMaxLength(50);
            builder.Property(x => x.IF_InvOutILStatus).HasMaxLength(50);
            builder.Property(x => x.LogLUBy).HasMaxLength(50);
        }
    }

    /// <summary>
    /// Seed demo InvF_InventoryOutInstLot (Order 42).
    /// Gắn với InvF_InventoryOutSeeder (Order 17, IF_InvOutNo = OUT001).
    /// </summary>
    public class InvF_InventoryOutInstLotSeeder : IPortSeeder
    {
        public int Order => 42;

        public async Task SeedAsync(AppDbContext db)
        {
            if (await db.Set<InvF_InventoryOutInstLot>().AnyAsync()) return;
            db.Set<InvF_InventoryOutInstLot>().AddRange(
                new InvF_InventoryOutInstLot
                {
                    IF_InvOutNo = "OUT001",
                    ProductCodeRoot = "PROD01",
                    ProductCode = "PROD01",
                    ProductLotNo = "LOT001",
                    LotNo = "LOT001",
                    InvCodeOutActual = "KHO01",
                    NetworkID = "1001",
                    Qty = 15,
                    ProductionDate = "2026-01-01",
                    ExpiredDate = "2027-01-01",
                    IF_InvOutILStatus = "PENDING",
                    LogLUDTimeUTC = DateTime.UtcNow,
                    LogLUBy = "seed"
                },
                new InvF_InventoryOutInstLot
                {
                    IF_InvOutNo = "OUT001",
                    ProductCodeRoot = "PROD01",
                    ProductCode = "PROD01",
                    ProductLotNo = "LOT002",
                    LotNo = "LOT002",
                    InvCodeOutActual = "KHO01",
                    NetworkID = "1001",
                    Qty = 25,
                    ProductionDate = "2026-02-01",
                    ExpiredDate = "2027-02-01",
                    IF_InvOutILStatus = "PENDING",
                    LogLUDTimeUTC = DateTime.UtcNow,
                    LogLUBy = "seed"
                },
                new InvF_InventoryOutInstLot
                {
                    IF_InvOutNo = "OUT001",
                    ProductCodeRoot = "PROD02",
                    ProductCode = "PROD02",
                    ProductLotNo = "LOT003",
                    LotNo = "LOT003",
                    InvCodeOutActual = "KHO02",
                    NetworkID = "1001",
                    Qty = 10,
                    ProductionDate = "2026-03-01",
                    ExpiredDate = "2027-03-01",
                    IF_InvOutILStatus = "PENDING",
                    LogLUDTimeUTC = DateTime.UtcNow,
                    LogLUBy = "seed"
                }
            );
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// port 1:1 InvF_InventoryOutInstSerial.cs — InvF_InventoryOutInstSerial (serial phiếu xuất).
    /// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Common\Models\InvF_InventoryOutInstSerial.cs:9-31.
    /// Bổ sung cột ProductCodeRoot cho filter 1:1 của InvF_InventoryOutController.cs:2092 (x.ProductCodeRoot.Equals(ProductCode)).
    /// </summary>
    public class InvF_InventoryOutInstSerial
    {
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? SerialNo { get; set; }
        /// <summary>Nguồn dòng 15 — mã hàng hóa gốc (filter gate 2092).</summary>
        public string? ProductCodeRoot { get; set; }
        /// <summary>Nguồn dòng 13 — Mã kho xuất thực tế.</summary>
        public string? InvCodeOutActual { get; set; }
        public string? NetworkID { get; set; }
        public string? ProductLotNo { get; set; }
        /// <summary>Nguồn dòng 25 — trạng thái phiếu xuất (serial).</summary>
        public string? IF_InvOutISStatus { get; set; }
        public DateTime? LogLUDTimeUTC { get; set; }
        public string? LogLUBy { get; set; }
    }

    public class InvF_InventoryOutInstSerialConfiguration : IEntityTypeConfiguration<InvF_InventoryOutInstSerial>
    {
        public void Configure(EntityTypeBuilder<InvF_InventoryOutInstSerial> builder)
        {
            builder.ToTable("InvF_InventoryOutInstSerial");
            builder.HasKey(x => new { x.IF_InvOutNo, x.ProductCode, x.SerialNo });
            builder.Property(x => x.IF_InvOutNo).HasMaxLength(50);
            builder.Property(x => x.ProductCode).HasMaxLength(50);
            builder.Property(x => x.SerialNo).HasMaxLength(100);
            builder.Property(x => x.ProductCodeRoot).HasMaxLength(50);
            builder.Property(x => x.InvCodeOutActual).HasMaxLength(50);
            builder.Property(x => x.NetworkID).HasMaxLength(50);
            builder.Property(x => x.ProductLotNo).HasMaxLength(50);
            builder.Property(x => x.IF_InvOutISStatus).HasMaxLength(50);
            builder.Property(x => x.LogLUBy).HasMaxLength(50);
        }
    }

    /// <summary>
    /// Seed demo InvF_InventoryOutInstSerial (Order 19 — nghiệp vụ phiếu xuất).
    /// Gắn với InvF_InventoryOutSeeder (Order 17, IF_InvOutNo = OUT001).
    /// </summary>
    public class InvF_InventoryOutInstSerialSeeder : IPortSeeder
    {
        public int Order => 19;

        public async Task SeedAsync(AppDbContext db)
        {
            if (await db.Set<InvF_InventoryOutInstSerial>().AnyAsync()) return;
            db.Set<InvF_InventoryOutInstSerial>().AddRange(
                new InvF_InventoryOutInstSerial { IF_InvOutNo = "OUT001", ProductCode = "PC1", SerialNo = "SN1", ProductCodeRoot = "PC1", InvCodeOutActual = "KHO01", NetworkID = "1001", IF_InvOutISStatus = "PENDING", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
                new InvF_InventoryOutInstSerial { IF_InvOutNo = "OUT001", ProductCode = "PC1", SerialNo = "SN2", ProductCodeRoot = "PC1", InvCodeOutActual = "KHO01", NetworkID = "1001", IF_InvOutISStatus = "PENDING", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
                new InvF_InventoryOutInstSerial { IF_InvOutNo = "OUT001", ProductCode = "PC2", SerialNo = "SN3", ProductCodeRoot = "PC2", InvCodeOutActual = "KHO01", NetworkID = "1001", IF_InvOutISStatus = "PENDING", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" }
            );
            await db.SaveChangesAsync();
        }
    }

    public class InvF_InventoryOutDtl
    {
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
    }

    public class InvF_InventoryOutDtlConfiguration : IEntityTypeConfiguration<InvF_InventoryOutDtl>
    {
        public void Configure(EntityTypeBuilder<InvF_InventoryOutDtl> builder)
        {
            builder.ToTable("InvF_InventoryOutDtl");
            builder.HasKey(x => new { x.IF_InvOutNo, x.ProductCode });
        }
    }

    public class InvF_InventoryOutQR
    {
        public string? IF_InvOutNo { get; set; }
        public string? QRId { get; set; }
    }

    public class InvF_InventoryOutQRConfiguration : IEntityTypeConfiguration<InvF_InventoryOutQR>
    {
        public void Configure(EntityTypeBuilder<InvF_InventoryOutQR> builder)
        {
            builder.ToTable("InvF_InventoryOutQR");
            builder.HasKey(x => new { x.IF_InvOutNo, x.QRId });
        }
    }
}

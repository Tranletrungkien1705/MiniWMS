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

    public class InvF_InventoryOutInstLot
    {
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? LotNo { get; set; }
    }

    public class InvF_InventoryOutInstLotConfiguration : IEntityTypeConfiguration<InvF_InventoryOutInstLot>
    {
        public void Configure(EntityTypeBuilder<InvF_InventoryOutInstLot> builder)
        {
            builder.ToTable("InvF_InventoryOutInstLot");
            builder.HasKey(x => new { x.IF_InvOutNo, x.ProductCode, x.LotNo });
        }
    }

    public class InvF_InventoryOutInstSerial
    {
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? SerialNo { get; set; }
    }

    public class InvF_InventoryOutInstSerialConfiguration : IEntityTypeConfiguration<InvF_InventoryOutInstSerial>
    {
        public void Configure(EntityTypeBuilder<InvF_InventoryOutInstSerial> builder)
        {
            builder.ToTable("InvF_InventoryOutInstSerial");
            builder.HasKey(x => new { x.IF_InvOutNo, x.ProductCode, x.SerialNo });
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

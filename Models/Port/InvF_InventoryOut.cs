using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

[Table("InvF_InventoryOut")]
public class InvF_InventoryOut
{
    [Key]
    [StringLength(50)]
    public string IF_InvOutNo { get; set; } = null!;

    [StringLength(50)]
    public string? OrgID { get; set; }

    [StringLength(50)]
    public string? InvOutType { get; set; }

    [StringLength(50)]
    public string? CustomerCode { get; set; }

    [StringLength(50)]
    public string? InvCodeOut { get; set; }

    [StringLength(50)]
    public string? RefNo { get; set; }

    [StringLength(50)]
    public string? RefType { get; set; }

    [StringLength(50)]
    public string? BusinessGroupCode { get; set; }

    [StringLength(50)]
    public string? ProfileStatus { get; set; }

    [StringLength(50)]
    public string? IF_InvOutStatus { get; set; }

    public DateTime? CreateDTimeUTC { get; set; }

    [StringLength(50)]
    public string? CreateBy { get; set; }

    public DateTime? ApprDTimeUTC { get; set; }

    [StringLength(10)]
    public string? FlagIsDelete { get; set; }

    public DateTime? LogLUDTimeUTC { get; set; }
}

public class InvF_InventoryOutConfiguration : IEntityTypeConfiguration<InvF_InventoryOut>
{
    public void Configure(EntityTypeBuilder<InvF_InventoryOut> builder)
    {
        builder.HasKey(x => x.IF_InvOutNo);
    }
}

public class InvF_InventoryOutSeeder : IPortSeeder
{
    public int Order => 17;

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<InvF_InventoryOut>().AnyAsync(x => x.IF_InvOutNo == "OUT001"))
        {
            db.Set<InvF_InventoryOut>().Add(new InvF_InventoryOut
            {
                IF_InvOutNo = "OUT001",
                OrgID = "ORG01",
                InvOutType = "OUT_TYPE_1",
                CustomerCode = "CUST01",
                InvCodeOut = "INV01",
                RefNo = "001",
                RefType = "RO",
                BusinessGroupCode = "BG01",
                ProfileStatus = "NEW",
                IF_InvOutStatus = "PENDING",
                CreateDTimeUTC = DateTime.UtcNow,
                CreateBy = "admin",
                FlagIsDelete = "0"
            });
            await db.SaveChangesAsync();
        }
    }
}

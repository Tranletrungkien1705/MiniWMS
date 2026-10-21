using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

[Table("InvF_InventoryOutAttachFile")]
public class InvF_InventoryOutAttachFile
{
    [Key]
    public long AutoID { get; set; }
    public int? Idx { get; set; }
    public string? IF_InvOutNo { get; set; }
    public string NetworkID { get; set; } = string.Empty;
    public string AttachFilePath { get; set; } = string.Empty;
    public string? AttachFileName { get; set; }
    public string? AttachFileDesc { get; set; }
    public string? FlagActive { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    [NotMapped]
    public string? FlagIsFilePath { get; set; }
    [NotMapped]
    public string? AttachFileSpec { get; set; }
}

public class InvF_InventoryOutAttachFileConfiguration : IEntityTypeConfiguration<InvF_InventoryOutAttachFile>
{
    public void Configure(EntityTypeBuilder<InvF_InventoryOutAttachFile> builder)
    {
        builder.ToTable("InvF_InventoryOutAttachFile");
        builder.HasKey(x => x.AutoID);
    }
}

public class InvF_InventoryOutAttachFileSeeder : IPortSeeder
{
    public int Order => 220;
    public void Seed(AppDbContext db)
    {
        if (!db.Set<InvF_InventoryOutAttachFile>().Any())
        {
            db.Set<InvF_InventoryOutAttachFile>().Add(new InvF_InventoryOutAttachFile
            {
                IF_InvOutNo = "INVOUT001",
                NetworkID = "1001",
                AttachFilePath = "Uploads/2026-10-22/test.pdf",
                AttachFileName = "test.pdf",
                FlagActive = "1"
            });
            db.SaveChanges();
        }
    }

    public Task SeedAsync(AppDbContext db)
    {
        Seed(db);
        return Task.CompletedTask;
    }
}

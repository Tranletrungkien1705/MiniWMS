using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

public class Mst_Org
{
    public string OrgID { get; set; } = "";
    public string? OrgParent { get; set; }
    public string? OrgBusinessType { get; set; }
    public string? OrgType { get; set; }
    public string? Remark { get; set; }
    public string? FlagActive { get; set; } = "1";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgName { get; set; }
}

public class Mst_OrgConfiguration : IEntityTypeConfiguration<Mst_Org>
{
    public void Configure(EntityTypeBuilder<Mst_Org> b)
    {
        b.ToTable("Mst_Org");
        b.HasKey(x => x.OrgID);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.OrgParent).HasMaxLength(50);
        b.Property(x => x.OrgBusinessType).HasMaxLength(50);
        b.Property(x => x.OrgType).HasMaxLength(50);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.OrgName).HasMaxLength(255);
    }
}

public class Mst_OrgSeeder : IPortSeeder
{
    public int Order => 10;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_Org>().AnyAsync()) return;
        db.Set<Mst_Org>().AddRange(
            new Mst_Org { OrgID = "ORG01", OrgName = "Chi nhánh Hà Nội", FlagActive = "1", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
            new Mst_Org { OrgID = "ORG02", OrgName = "Chi nhánh TP.HCM", FlagActive = "1", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
            new Mst_Org { OrgID = "ORG03", OrgName = "Chi nhánh Đà Nẵng (ngừng)", FlagActive = "0", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Mst_VATRate.cs — Mst_VATRate (thuế suất VAT).
/// Model nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Common\Models\Mst_VATRate.cs:9-26
/// (VATRateCode, NetworkID, VATRate, VATDesc, Remark, FlagActive, LogLUDTimeUTC, LogLUBy).
/// DDL: không có 'create table Mst_VATRate' trong .sql nguồn → dùng MODEL làm từ điển cột
/// (nguồn khai object cho mọi cột vì ORM động; port gán kiểu theo tên/ngữ nghĩa — ghi GAP).
/// Gate InvF_InventoryInController.cs:777-798 lọc `Mst_VATRate.FlagActive = '1'` (dòng 792).
/// </summary>
public class Mst_VATRate
{
    public string VATRateCode { get; set; } = "";
    public string? NetworkID { get; set; }
    public double? VATRate { get; set; }
    public string? VATDesc { get; set; }
    public string? Remark { get; set; }
    public string? FlagActive { get; set; } = "1";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_VATRateConfiguration : IEntityTypeConfiguration<Mst_VATRate>
{
    public void Configure(EntityTypeBuilder<Mst_VATRate> b)
    {
        b.ToTable("Mst_VATRate");
        b.HasKey(x => x.VATRateCode);
        b.Property(x => x.VATRateCode).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.VATDesc).HasMaxLength(255);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>Seed demo Mst_VATRate (Order 34 — danh mục thuế suất VAT).</summary>
public class Mst_VATRateSeeder : IPortSeeder
{
    public int Order => 34;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Mst_VATRate>().AnyAsync()) return;
        db.Set<Mst_VATRate>().AddRange(
            new Mst_VATRate { VATRateCode = "VAT10", NetworkID = "1001", VATRate = 10, VATDesc = "Thuế GTGT 10%", FlagActive = "1", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
            new Mst_VATRate { VATRateCode = "VAT8",  NetworkID = "1001", VATRate = 8,  VATDesc = "Thuế GTGT 8%",  FlagActive = "1", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" },
            new Mst_VATRate { VATRateCode = "VAT0",  NetworkID = "1001", VATRate = 0,  VATDesc = "Không chịu thuế (ngừng)", FlagActive = "0", LogLUDTimeUTC = DateTime.UtcNow, LogLUBy = "seed" }
        );
        await db.SaveChangesAsync();
    }
}

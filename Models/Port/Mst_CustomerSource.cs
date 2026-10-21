using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

// port 1:1 Mst_CustomerSourceController.cs — Mst_CustomerSource
// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\Mst_CustomerSourceController.cs
// Cột lấy theo màn hình nguồn tương ứng (CustomerSourceCode, CustomerSourceName, CustomerSourceDesc, FlagActive, OrgID, LogLUBy, LogLUDTimeUTC).
public class Mst_CustomerSource
{
    public long AutoId { get; set; }
    public string? OrgID { get; set; }
    public string? CustomerSourceCode { get; set; }
    public string? CustomerSourceName { get; set; }
    public string? CustomerSourceDesc { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUBy { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
}

public class Mst_CustomerSourceConfiguration : IEntityTypeConfiguration<Mst_CustomerSource>
{
    public void Configure(EntityTypeBuilder<Mst_CustomerSource> builder)
    {
        builder.ToTable("Mst_CustomerSource");
        builder.HasKey(x => x.AutoId);
        builder.Property(x => x.AutoId).ValueGeneratedOnAdd();
    }
}

/// <summary>Dữ liệu mẫu danh mục nguồn khách hàng (idempotent, chỉ THÊM dòng mới).</summary>
public class Mst_CustomerSourceSeeder : IPortSeeder
{
    public int Order => 33;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_CustomerSource>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_CustomerSource { OrgID = "1001", CustomerSourceCode = "CS-WEB", CustomerSourceName = "Website", CustomerSourceDesc = "Khách hàng từ website", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
            new Mst_CustomerSource { OrgID = "1001", CustomerSourceCode = "CS-FB", CustomerSourceName = "Facebook", CustomerSourceDesc = "Khách hàng từ Facebook", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
            new Mst_CustomerSource { OrgID = "1001", CustomerSourceCode = "CS-REF", CustomerSourceName = "Giới thiệu", CustomerSourceDesc = "Khách hàng giới thiệu", FlagActive = "0", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }
}

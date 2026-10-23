using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port
{
    public class Mst_District
    {
        public string? ProvinceCode { get; set; }
        public string? DistrictCode { get; set; }
        public string? FlagActive { get; set; }
    }

    public class Mst_DistrictConfiguration : IEntityTypeConfiguration<Mst_District>
    {
        public void Configure(EntityTypeBuilder<Mst_District> builder)
        {
            builder.ToTable("Mst_District");
            builder.HasKey(x => new { x.ProvinceCode, x.DistrictCode });
            // PK chưa xác minh đầy đủ; theo FK REFERENCES Mst_District (ProvinceCode, ...) từ 20191203.z11.CreateTable.Mst_Agent.sql:27
        }
    }

    public class Mst_DistrictSeeder : IPortSeeder
    {
        public int Order => 84;

        public async Task SeedAsync(AppDbContext db)
        {
            var list = new List<Mst_District>
            {
                new() { ProvinceCode = "01", DistrictCode = "001", FlagActive = "1" },
                new() { ProvinceCode = "01", DistrictCode = "002", FlagActive = "1" },
                new() { ProvinceCode = "01", DistrictCode = "003", FlagActive = "0" },
                new() { ProvinceCode = "79", DistrictCode = "001", FlagActive = "1" },
                new() { ProvinceCode = "79", DistrictCode = "002", FlagActive = "0" },
                new() { ProvinceCode = "48", DistrictCode = "001", FlagActive = "1" },
                new() { ProvinceCode = "48", DistrictCode = "002", FlagActive = "1" },
            };

            foreach (var item in list)
            {
                var existing = await db.Set<Mst_District>()
                    .FirstOrDefaultAsync(x => x.ProvinceCode == item.ProvinceCode && x.DistrictCode == item.DistrictCode);
                if (existing == null)
                {
                    db.Set<Mst_District>().Add(item);
                }
            }

            await db.SaveChangesAsync();
        }
    }
}

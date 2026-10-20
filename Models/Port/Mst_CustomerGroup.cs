using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

// port 1:1 Mst_CustomerGroupController.cs — Mst_CustomerGroup
// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\Mst_CustomerGroupController.cs:111
//        (`new Mst_CustomerGroup()` — action Create chỉ dựng đối tượng RỖNG để mở form, không đọc/ghi bảng).
// Cột lấy theo màn hình nguồn `Views/Mst_CustomerGroup/Mst_CustomerGroup_Info.cshtml`
// (labs-audit/NGUON_VIEW_BANG_COT.tsv:432: CustomerGrpCode, CustomerGrpDesc, CustomerGrpName, FlagActive)
// + các cột hệ thống mà mọi bảng Mst_* của nguồn đều có (AutoId, OrgID, LogLUBy, LogLUDTimeUTC).
// LƯU Ý: DDL/model 2020.3 KHÔNG nằm trong source_excerpt.txt — xem .parity-run\conflicts\301_conflict.md.
public class Mst_CustomerGroup
{
    public long AutoId { get; set; }
    public string? OrgID { get; set; }
    public string? CustomerGrpCode { get; set; }
    public string? CustomerGrpName { get; set; }
    public string? CustomerGrpDesc { get; set; }
    public string? FlagActive { get; set; }
    public string? LogLUBy { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
}

public class Mst_CustomerGroupConfiguration : IEntityTypeConfiguration<Mst_CustomerGroup>
{
    public void Configure(EntityTypeBuilder<Mst_CustomerGroup> builder)
    {
        builder.ToTable("Mst_CustomerGroup");
        builder.HasKey(x => x.AutoId);
        builder.Property(x => x.AutoId).ValueGeneratedOnAdd();
    }
}

/// <summary>Dữ liệu mẫu danh mục nhóm khách hàng (idempotent, chỉ THÊM dòng mới).</summary>
public class Mst_CustomerGroupSeeder : IPortSeeder
{
    public int Order => 31;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_CustomerGroup>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_CustomerGroup { OrgID = "1001", CustomerGrpCode = "KH-LE", CustomerGrpName = "Khách lẻ", CustomerGrpDesc = "Khách mua lẻ", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
            new Mst_CustomerGroup { OrgID = "1001", CustomerGrpCode = "KH-DL", CustomerGrpName = "Đại lý", CustomerGrpDesc = "Đại lý phân phối", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
            new Mst_CustomerGroup { OrgID = "1001", CustomerGrpCode = "KH-XN", CustomerGrpName = "Xưởng dịch vụ", CustomerGrpDesc = "Xưởng sửa chữa", FlagActive = "0", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

// port 1:1 Mst_CustomerGroupController.cs — Mst_CustomerGroupImages
// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Biz\CustomerCentrer\Customer.cs:13407-13424
//        (danh sách cột của bảng tạm `#input_Mst_CustomerGroupImages` — chính là các cột được insert vào bảng thật
//         ở Customer.cs:13485-13515) + cột khoá `Idx` (int, Customer.cs:13411).
// LƯU Ý: DDL/model 2020.3 KHÔNG nằm trong source_excerpt.txt — xem .parity-run\conflicts\303_conflict.md.
public class Mst_CustomerGroupImages
{
    public int Idx { get; set; }
    public string? OrgID { get; set; }
    public string? CustomerGrpCode { get; set; }
    public string? NetworkID { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageName { get; set; }
    public string? ImageDesc { get; set; }
    public string? FlagPrimaryImage { get; set; }
    public string? FlagActive { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_CustomerGroupImagesConfiguration : IEntityTypeConfiguration<Mst_CustomerGroupImages>
{
    public void Configure(EntityTypeBuilder<Mst_CustomerGroupImages> builder)
    {
        builder.ToTable("Mst_CustomerGroupImages");
        // Khoá: (OrgID, CustomerGrpCode, Idx) — nguồn join ảnh theo OrgID + CustomerGrpCode (Customer.cs:12180-12182)
        // và `Idx` là số thứ tự ảnh trong nhóm (Customer.cs:13411).
        builder.HasKey(x => new { x.OrgID, x.CustomerGrpCode, x.Idx });
    }
}

/// <summary>Dữ liệu mẫu ảnh nhóm khách hàng (idempotent, chỉ THÊM dòng mới).</summary>
public class Mst_CustomerGroupImagesSeeder : IPortSeeder
{
    public int Order => 32;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_CustomerGroupImages>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_CustomerGroupImages { Idx = 1, OrgID = "1001", CustomerGrpCode = "KH-LE", ImagePath = "/images/kh-le.png", ImageName = "kh-le.png", ImageDesc = "Ảnh nhóm khách lẻ", FlagPrimaryImage = "1", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
            new Mst_CustomerGroupImages { Idx = 1, OrgID = "1001", CustomerGrpCode = "KH-DL", ImagePath = "/images/kh-dl.png", ImageName = "kh-dl.png", ImageDesc = "Ảnh nhóm đại lý", FlagPrimaryImage = "1", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
            // Nhóm KH-XN (FlagActive = "0") cũng có ảnh: Edit ghi đè FlagActive của nhóm nhưng KHÔNG đụng bảng ảnh
            // (gate chỉ đặt Ft_Cols_Upd cho Mst_CustomerGroup, Mst_CustomerGroupController.cs:301-305).
            new Mst_CustomerGroupImages { Idx = 1, OrgID = "1001", CustomerGrpCode = "KH-XN", ImagePath = "/images/kh-xn.png", ImageName = "kh-xn.png", ImageDesc = "Ảnh xưởng dịch vụ", FlagPrimaryImage = "1", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }
}

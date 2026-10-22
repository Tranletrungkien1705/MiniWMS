using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 InvF_MoveOrd.cs — InvF_MoveOrd (lệnh chuyển kho).
/// Nguồn: excerpt #142/#143 — "MODEL: khong thay InvF_MoveOrd.cs ... DDL: khong co 'create table InvF_MoveOrd'"
/// ⇒ KHÔNG có từ điển cột. Gate InvF_MoveOrdController.cs:43-46 chỉ dựng `new PageInfo&lt;InvF_MoveOrd&gt;(0, recordcount)`
/// với `DataList` RỖNG và KHÔNG truy vấn bảng này ⇒ entity chỉ cần tồn tại để giữ kiểu PageInfo.
///
/// #143 `DoSearch` (InvF_MoveOrdController.cs:89-151 → Form.cs:183334-183660) ĐỌC bảng này; cột dưới đây lấy ĐÚNG
/// các cột SQL nguồn dùng tới:
///   - `ifmo.IF_MONo` (Form.cs:183403, 183408, 183410, 183472, 183503, 183537, 183569);
///   - `ifmo.CreateDTimeUTC` (dòng 183404, 183419 `order by ... desc`);
///   - `ifmo.OrgID` (dòng 183408, 183412, 183415, 183507, 183510, 183541, 183573);
///   - `ifmo.InvCodeIn` (dòng 183416);
///   - `ifmo.CreateBy` (dòng 183474 `left join Sys_User su on ifmo.CreateBy = su.UserCode`).
/// Các cột còn lại của `ifmo.*` (dòng 183466) KHÔNG có trong excerpt ⇒ GAP, không tự đặt.
/// </summary>
public class InvF_MoveOrd
{
    /// <summary>Nguồn Form.cs:183403 — `ifmo.IF_MONo` (khoá join của mọi bảng con).</summary>
    public string IF_MONo { get; set; } = "";
    /// <summary>Nguồn Form.cs:183404 — `ifmo.CreateDTimeUTC` (lọc khoảng From/To, sắp xếp giảm dần).</summary>
    public DateTime? CreateDTimeUTC { get; set; }
    /// <summary>Nguồn Form.cs:183408 — `ifmo.OrgID` (join Mst_NNT/Mst_Product/Mst_Inventory/Mst_Unit).</summary>
    public string? OrgID { get; set; }
    /// <summary>Nguồn Form.cs:183416 — `ifmo.InvCodeIn` (join kho nhập).</summary>
    public string? InvCodeIn { get; set; }
    /// <summary>Nguồn Form.cs:183474 — `ifmo.CreateBy` (left join Sys_User).</summary>
    public string? CreateBy { get; set; }
    /// <summary>Nguồn Form.cs:183465 — `t.MyIdxSeq` (cột tay cầm thứ tự của bảng tạm, không thuộc bảng nguồn).</summary>
    public long MyIdxSeq { get; set; }
    /// <summary>Nguồn Form.cs:183403 — `ifmo.IF_MONo` của bảng tạm `#tbl_InvF_MoveOrd_Filter_Draft` (dòng 183403).</summary>
    public string? Filter_IF_MONo { get; set; }
    /// <summary>Nguồn Form.cs:183468 — `su.UserCode su_UserCode_Create` (left join Sys_User).</summary>
    public string? su_UserCode_Create { get; set; }
    /// <summary>Nguồn Form.cs:183469 — `su.UserName su_UserName_Create` (left join Sys_User).</summary>
    public string? su_UserName_Create { get; set; }
}

public class InvF_MoveOrdConfiguration : IEntityTypeConfiguration<InvF_MoveOrd>
{
    public void Configure(EntityTypeBuilder<InvF_MoveOrd> b)
    {
        b.ToTable("InvF_MoveOrd");
        // Nguồn không cho biết DDL/khoá thật; IF_MONo là cột nguồn dùng để join mọi bảng con (Form.cs:183410).
        b.HasKey(x => x.IF_MONo);
        b.Property(x => x.IF_MONo).HasMaxLength(50);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.InvCodeIn).HasMaxLength(50);
        b.Property(x => x.CreateBy).HasMaxLength(50);
        // Cột join/tay cầm không lưu vào bảng chính.
        b.Ignore(x => x.MyIdxSeq);
        b.Ignore(x => x.Filter_IF_MONo);
        b.Ignore(x => x.su_UserCode_Create);
        b.Ignore(x => x.su_UserName_Create);
    }
}

/// <summary>
/// Seed demo nghiệp vụ cho #143 InvF_MoveOrdController/DoSearch (Order 38 — sau InvF_InventoryInInstSerial Order 37).
/// Idempotent. Dữ liệu khớp seed danh mục sẵn có: Mst_Org (ORG01/ORG02), Mst_NNT (ORG01/ORG02 FlagActive = "1"),
/// Mst_Inventory (KHO01/KHO02 FlagActive = "1"), Mst_Product (SP01/SP02/SP03 @ ORG01), Mst_Unit (U01/U02 @ ORG01),
/// Sys_User (ADMIN/USER01).
/// 4 lệnh chuyển kho (đều OrgID = ORG01, InvCodeIn = KHO01 để qua được inner join #tbl_Mst_Inventory_ViewAbility):
///   PC001 PENDING, PC002 APPROVE, PC003 CANCEL, PC004 PENDING — mỗi lệnh 1 dòng chi tiết (SP01/SP02/SP03/SP01).
/// ⇒ body rỗng: 4 dòng; chkPending = "1": 2 dòng; chkApproved = "1": 1 dòng; chkCanceled = "1": 1 dòng;
///   chkPending + chkApproved: 3 dòng; IF_MONo = "PC001,PC002": 2 dòng.
/// </summary>
public class InvF_MoveOrdSeeder : IPortSeeder
{
    public int Order => 38;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<InvF_MoveOrd>().AnyAsync()) return;

        var now = DateTime.UtcNow;
        db.Set<InvF_MoveOrd>().AddRange(
            new InvF_MoveOrd { IF_MONo = "PC001", OrgID = "ORG01", InvCodeIn = "KHO01", CreateDTimeUTC = now.AddDays(-4), CreateBy = "ADMIN" },
            new InvF_MoveOrd { IF_MONo = "PC002", OrgID = "ORG01", InvCodeIn = "KHO01", CreateDTimeUTC = now.AddDays(-3), CreateBy = "USER01" },
            new InvF_MoveOrd { IF_MONo = "PC003", OrgID = "ORG01", InvCodeIn = "KHO01", CreateDTimeUTC = now.AddDays(-2), CreateBy = "ADMIN" },
            new InvF_MoveOrd { IF_MONo = "PC004", OrgID = "ORG01", InvCodeIn = "KHO01", CreateDTimeUTC = now.AddDays(-1), CreateBy = "USER01" }
        );

        db.Set<InvF_MoveOrdDtl>().AddRange(
            new InvF_MoveOrdDtl { IF_MONo = "PC001", ProductCode = "SP01", UnitCode = "U01" },
            new InvF_MoveOrdDtl { IF_MONo = "PC002", ProductCode = "SP02", UnitCode = "U01" },
            new InvF_MoveOrdDtl { IF_MONo = "PC003", ProductCode = "SP03", UnitCode = "U02" },
            new InvF_MoveOrdDtl { IF_MONo = "PC004", ProductCode = "SP01", UnitCode = "U01" }
        );

        await db.SaveChangesAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Inv_CostPriceHist.cs — Inv_CostPriceHist (lịch sử giá vốn).
/// Nguồn: Inv_CostPriceHistController.cs:96 `return View(new List&lt;Inv_CostPriceHist&gt;())` — action Index KHÔNG đọc/ghi bảng này,
/// chỉ trả danh sách RỖNG. Excerpt #23 dòng 156-158: "MODEL: khong thay Inv_CostPriceHist.cs ... dung MODEL lam tu dien cot"
/// ⇒ nguồn KHÔNG cho biết cột nào. Entity dưới đây chỉ là khung tối thiểu để dựng kiểu trả về (List rỗng), KHÔNG dùng để
/// đọc/ghi dữ liệu và KHÔNG tự đặt cột nghiệp vụ (xem .parity-run\conflicts\23_conflict.md).
///
/// #24 `DoSearch_Inv_CostPriceHist` (Form.cs:200498-200669) đọc bảng này với các cột: ProductCode, EffectDTimeUTC, OrgID,
/// InvCode (dòng 200566-200569, 200591-200594, 200621 `t.*`) ⇒ bổ sung đúng 4 cột đó. Khoá vẫn giữ `Id` của khung #23
/// (nguồn không cho biết DDL/khoá thật); bộ cột nguồn dùng để join `#tbl_Inv_CostPriceHist_Filter` là
/// (ProductCode, EffectDTimeUTC, OrgID, InvCode) — dòng 200630-200633.
/// Các cột còn lại của `t.*` nguồn KHÔNG có trong excerpt ⇒ GAP, không tự đặt.
///
/// #30 `ExportExcel` (Inv_CostPriceHistController.cs:721-777) xuất Excel theo `GetExportDicColumsTemplate()`
/// (dòng 829-843) — 9 cột, trong đó 5 cột thuộc bảng này: `EffectDTimeUTC`, `UPInv`, `FormNo`, `UpdBy`, `UpdDTimeUTC`
/// (3 cột còn lại `mp_ProductCodeUser`/`mp_ProductName`/`mp_UnitCode` là cột join của Mst_Product, xem
/// `ExportExcel_Inv_CostPriceHistItem`). Tên cột lấy NGUYÊN VĂN từ hằng `TblInv_CostPriceHist.*` của nguồn.
/// </summary>
public class Inv_CostPriceHist
{
    /// <summary>Khoá kỹ thuật của khung — nguồn không có thông tin cột (GAP).</summary>
    public string Id { get; set; } = "";
    /// <summary>Nguồn Form.cs:200566 — `t.ProductCode` (join Mst_Product theo ProductCode + OrgID).</summary>
    public string? ProductCode { get; set; }
    /// <summary>Nguồn Form.cs:200567 — `t.EffectDTimeUTC` (lọc khoảng From/To, dòng 200579-200580).</summary>
    public DateTime? EffectDTimeUTC { get; set; }
    /// <summary>Nguồn Form.cs:200568 — `t.OrgID`.</summary>
    public string? OrgID { get; set; }
    /// <summary>Nguồn Form.cs:200569 — `t.InvCode` (lọc `= '@strInvCode'`, dòng 200578).</summary>
    public string? InvCode { get; set; }
    /// <summary>Nguồn Inv_CostPriceHistController.cs:837 — `TblInv_CostPriceHist.UPInv` (Giá vốn). Kiểu số nguồn không có trong excerpt ⇒ `double?` (luật mục 3).</summary>
    public double? UPInv { get; set; }
    /// <summary>Nguồn Inv_CostPriceHistController.cs:838 — `TblInv_CostPriceHist.FormNo` (Số phiếu nhập).</summary>
    public string? FormNo { get; set; }
    /// <summary>Nguồn Inv_CostPriceHistController.cs:839 — `TblInv_CostPriceHist.UpdBy` (Người cập nhật).</summary>
    public string? UpdBy { get; set; }
    /// <summary>Nguồn Inv_CostPriceHistController.cs:840 — `TblInv_CostPriceHist.UpdDTimeUTC` (Ngày cập nhật).</summary>
    public DateTime? UpdDTimeUTC { get; set; }
}

public class Inv_CostPriceHistConfiguration : IEntityTypeConfiguration<Inv_CostPriceHist>
{
    public void Configure(EntityTypeBuilder<Inv_CostPriceHist> b)
    {
        b.ToTable("Inv_CostPriceHist");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.InvCode).HasMaxLength(50);
        // #30 ExportExcel — 4 cột nguồn dùng để xuất Excel (Inv_CostPriceHistController.cs:837-840).
        b.Property(x => x.FormNo).HasMaxLength(50);
        b.Property(x => x.UpdBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seed demo Inv_CostPriceHist (Order 200 — nghiệp vụ). Nguồn #24 chỉ cho biết 4 cột (ProductCode, EffectDTimeUTC, OrgID,
/// InvCode) nên seed CHỈ dùng 4 cột đó; dữ liệu khớp Mst_Product (SP01/SP02/SP03 @ ORG01, SP01 @ ORG02) và
/// Mst_Inventory (KHO01/KHO02/KHO03). Mỗi (ProductCode, OrgID, InvCode) có 2 mốc EffectDTimeUTC để phân biệt
/// FlagIsCurrent = "0" (lấy tất cả) và FlagIsCurrent != "0" (chỉ Max(EffectDTimeUTC)).
/// #30 ExportExcel: điền thêm 4 cột nguồn dùng để xuất Excel (UPInv, FormNo, UpdBy, UpdDTimeUTC) — giá trị demo,
/// nguồn không cho biết quy tắc sinh (chỉ là dữ liệu mẫu, không phải giá trị nghiệp vụ suy diễn).
/// </summary>
public class Inv_CostPriceHistSeeder : IPortSeeder
{
    public int Order => 200;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Inv_CostPriceHist>().AnyAsync()) return;
        db.Set<Inv_CostPriceHist>().AddRange(
            // SP01 @ ORG01 @ KHO01 — 2 mốc giá vốn.
            new Inv_CostPriceHist { Id = "CPH01", OrgID = "ORG01", ProductCode = "SP01", InvCode = "KHO01", EffectDTimeUTC = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UPInv = 85000, FormNo = "PN260801-001", UpdBy = "seed", UpdDTimeUTC = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Inv_CostPriceHist { Id = "CPH02", OrgID = "ORG01", ProductCode = "SP01", InvCode = "KHO01", EffectDTimeUTC = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), UPInv = 92000, FormNo = "PN260801-001", UpdBy = "seed", UpdDTimeUTC = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
            // SP02 @ ORG01 @ KHO01 — 1 mốc.
            new Inv_CostPriceHist { Id = "CPH03", OrgID = "ORG01", ProductCode = "SP02", InvCode = "KHO01", EffectDTimeUTC = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), UPInv = 120000, FormNo = "PN260801-001", UpdBy = "seed", UpdDTimeUTC = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            // SP03 @ ORG01 @ KHO02 — 1 mốc.
            new Inv_CostPriceHist { Id = "CPH04", OrgID = "ORG01", ProductCode = "SP03", InvCode = "KHO02", EffectDTimeUTC = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), UPInv = 90000, FormNo = "PN260801-001", UpdBy = "seed", UpdDTimeUTC = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc) },
            // SP01 @ ORG02 @ KHO01 — 1 mốc (cùng mã hàng, khác OrgID).
            new Inv_CostPriceHist { Id = "CPH05", OrgID = "ORG02", ProductCode = "SP01", InvCode = "KHO01", EffectDTimeUTC = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), UPInv = 87000, FormNo = "PN260801-002", UpdBy = "seed", UpdDTimeUTC = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
        await db.SaveChangesAsync();
    }
}

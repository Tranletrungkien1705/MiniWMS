using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Rpt_Summary_In_Out_Pivot.cs — Rpt_Summary_In_Out_Pivot (kết quả báo cáo tổng hợp nhập/xuất dạng pivot).
/// Cột lấy theo model nguồn (excerpt #13: "MODEL: khong thay Rpt_Summary_In_Out_Pivot.cs ... dung MODEL lam tu dien cot").
/// Gate DashboardRptController.cs:316-335 chỉ đọc 5 cột: ProductCode, ProductCodeUser, ProductName, UnitCode, Qty.
/// Bảng này KHÔNG phải bảng lưu trữ: nguồn sinh dữ liệu bằng WA_Rpt_Summary_In_Out_Pivot (ReportsService) — xem GAP ở controller port.
/// </summary>
public class Rpt_Summary_In_Out_Pivot
{
    public string? ProductCode { get; set; }
    public string? ProductCodeUser { get; set; }
    public string? ProductName { get; set; }
    public string? UnitCode { get; set; }
    /// <summary>Nguồn `Convert.ToDouble(itQty.Qty)` (DashboardRptController.cs:327) ⇒ double? (luật: MyValue/float ⇒ double?).</summary>
    public double? Qty { get; set; }
}

public class Rpt_Summary_In_Out_PivotConfiguration : IEntityTypeConfiguration<Rpt_Summary_In_Out_Pivot>
{
    public void Configure(EntityTypeBuilder<Rpt_Summary_In_Out_Pivot> b)
    {
        b.ToTable("Rpt_Summary_In_Out_Pivot");
        // Model nguồn không có khoá (kết quả báo cáo) — dùng ProductCode làm khoá để EF dựng được bảng.
        b.HasKey(x => x.ProductCode);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.ProductCodeUser).HasMaxLength(50);
        b.Property(x => x.ProductName).HasMaxLength(255);
        b.Property(x => x.UnitCode).HasMaxLength(50);
    }
}

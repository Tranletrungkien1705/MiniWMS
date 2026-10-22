using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 InvF_MoveOrdDtl.cs — InvF_MoveOrdDtl (chi tiết lệnh chuyển kho).
/// Nguồn: excerpt #143 — "MODEL: khong thay InvF_MoveOrd.cs ... DDL: khong co 'create table InvF_MoveOrd'"
/// ⇒ KHÔNG có từ điển cột. Cột dưới đây lấy ĐÚNG các cột mà SQL nguồn dùng tới
/// (Form.cs:183409-183413, 183504-183511, 183491 `ifmodt.*`):
///   - join `ifmo.IF_MONo = ifmodt.IF_MONo` (dòng 183410, 183505);
///   - join `ifmodt.ProductCode = mp.ProductCode` (dòng 183413, 183508);
///   - join `ifmodt.UnitCode = mu.UnitCode` (dòng 183511).
/// Các cột còn lại của `ifmodt.*` KHÔNG có trong excerpt ⇒ GAP, không tự đặt.
/// </summary>
public class InvF_MoveOrdDtl
{
    /// <summary>Nguồn Form.cs:183410 — `ifmo.IF_MONo = ifmodt.IF_MONo`.</summary>
    public string IF_MONo { get; set; } = "";
    /// <summary>Nguồn Form.cs:183413 — `ifmodt.ProductCode = mp.ProductCode`.</summary>
    public string ProductCode { get; set; } = "";
    /// <summary>Nguồn Form.cs:183511 — `ifmodt.UnitCode = mu.UnitCode`.</summary>
    public string? UnitCode { get; set; }
}

public class InvF_MoveOrdDtlConfiguration : IEntityTypeConfiguration<InvF_MoveOrdDtl>
{
    public void Configure(EntityTypeBuilder<InvF_MoveOrdDtl> b)
    {
        b.ToTable("InvF_MoveOrdDtl");
        // Khoá theo cặp cột nguồn dùng để join (IF_MONo + ProductCode); nguồn không cho biết DDL thật.
        b.HasKey(x => new { x.IF_MONo, x.ProductCode });
        b.Property(x => x.IF_MONo).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.UnitCode).HasMaxLength(50);
    }
}

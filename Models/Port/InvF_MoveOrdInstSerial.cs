using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 InvF_MoveOrdInstSerial.cs — InvF_MoveOrdInstSerial (serial của lệnh chuyển kho).
/// Nguồn: excerpt #143 — "MODEL: khong thay InvF_MoveOrd.cs ... DDL: khong co 'create table InvF_MoveOrd'"
/// ⇒ KHÔNG có từ điển cột. Cột dưới đây lấy ĐÚNG các cột mà SQL nguồn dùng tới
/// (Form.cs:183570-183574, 183560 `ifmos.*`):
///   - join `t.IF_MONo = ifmos.IF_MONo` (dòng 183571);
///   - join `ifmos.ProductCode = mp.ProductCode` (dòng 183574).
/// Các cột còn lại của `ifmos.*` KHÔNG có trong excerpt ⇒ GAP, không tự đặt.
/// </summary>
public class InvF_MoveOrdInstSerial
{
    /// <summary>Nguồn Form.cs:183571 — `t.IF_MONo = ifmos.IF_MONo`.</summary>
    public string IF_MONo { get; set; } = "";
    /// <summary>Nguồn Form.cs:183574 — `ifmos.ProductCode = mp.ProductCode`.</summary>
    public string ProductCode { get; set; } = "";
}

public class InvF_MoveOrdInstSerialConfiguration : IEntityTypeConfiguration<InvF_MoveOrdInstSerial>
{
    public void Configure(EntityTypeBuilder<InvF_MoveOrdInstSerial> b)
    {
        b.ToTable("InvF_MoveOrdInstSerial");
        // Khoá theo cặp cột nguồn dùng để join (IF_MONo + ProductCode); nguồn không cho biết DDL thật.
        b.HasKey(x => new { x.IF_MONo, x.ProductCode });
        b.Property(x => x.IF_MONo).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
    }
}

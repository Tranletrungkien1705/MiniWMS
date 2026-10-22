using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 InvF_MoveOrdInstLot.cs — InvF_MoveOrdInstLot (lô của lệnh chuyển kho).
/// Nguồn: excerpt #143 — "MODEL: khong thay InvF_MoveOrd.cs ... DDL: khong co 'create table InvF_MoveOrd'"
/// ⇒ KHÔNG có từ điển cột. Cột dưới đây lấy ĐÚNG các cột mà SQL nguồn dùng tới
/// (Form.cs:183538-183542, 183528 `ifmol.*`):
///   - join `t.IF_MONo = ifmol.IF_MONo` (dòng 183539);
///   - join `ifmol.ProductCode = mp.ProductCode` (dòng 183542).
/// Các cột còn lại của `ifmol.*` KHÔNG có trong excerpt ⇒ GAP, không tự đặt.
/// </summary>
public class InvF_MoveOrdInstLot
{
    /// <summary>Nguồn Form.cs:183539 — `t.IF_MONo = ifmol.IF_MONo`.</summary>
    public string IF_MONo { get; set; } = "";
    /// <summary>Nguồn Form.cs:183542 — `ifmol.ProductCode = mp.ProductCode`.</summary>
    public string ProductCode { get; set; } = "";
}

public class InvF_MoveOrdInstLotConfiguration : IEntityTypeConfiguration<InvF_MoveOrdInstLot>
{
    public void Configure(EntityTypeBuilder<InvF_MoveOrdInstLot> b)
    {
        b.ToTable("InvF_MoveOrdInstLot");
        // Khoá theo cặp cột nguồn dùng để join (IF_MONo + ProductCode); nguồn không cho biết DDL thật.
        b.HasKey(x => new { x.IF_MONo, x.ProductCode });
        b.Property(x => x.IF_MONo).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
    }
}

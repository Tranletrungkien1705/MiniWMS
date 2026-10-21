using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 InvF_MoveOrd.cs — InvF_MoveOrd (lệnh chuyển kho).
/// Nguồn: excerpt #142 — "MODEL: khong thay InvF_MoveOrd.cs ... DDL: khong co 'create table InvF_MoveOrd'"
/// ⇒ KHÔNG có từ điển cột. Gate InvF_MoveOrdController.cs:43-46 chỉ dựng `new PageInfo&lt;InvF_MoveOrd&gt;(0, recordcount)`
/// với `DataList` RỖNG và KHÔNG truy vấn bảng này ⇒ entity chỉ cần tồn tại để giữ kiểu PageInfo.
/// Không có DDL nguồn để suy cột/khoá ⇒ KHÔNG khai cột nào; EF cần 1 thuộc tính để dựng bảng SQLite hợp lệ
/// nên khai `MyIdxSeq` (cột tay cầm thứ tự, không đọc/ghi bởi action Index — xem LESSONS A.5).
/// </summary>
public class InvF_MoveOrd
{
    public int MyIdxSeq { get; set; }
}

public class InvF_MoveOrdConfiguration : IEntityTypeConfiguration<InvF_MoveOrd>
{
    public void Configure(EntityTypeBuilder<InvF_MoveOrd> b)
    {
        b.ToTable("InvF_MoveOrd");
        // Không có DDL/model nguồn để suy khoá ⇒ không khai khoá nghiệp vụ (bảng không được đọc/ghi bởi action Index).
        b.HasNoKey();
    }
}

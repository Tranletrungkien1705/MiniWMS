using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Mst_ProductGroup.cs — Mst_ProductGroup (nhóm hàng hoá).
/// Nguồn: excerpt #143 — "MODEL: khong thay Mst_ProductGroup.cs ... DDL: khong co 'create table Mst_ProductGroup'"
/// ⇒ KHÔNG có từ điển cột. Bảng này CHỈ xuất hiện trong danh sách bảng/hàm họ Mst_ của excerpt (dòng 901);
/// SQL của action DoSearch (Form.cs:183399-183456) KHÔNG join/đọc bảng này ⇒ KHÔNG khai cột nào.
/// EF cần 1 thuộc tính để dựng bảng SQLite hợp lệ nên khai `MyIdxSeq` (cột tay cầm thứ tự, không đọc/ghi).
/// </summary>
public class Mst_ProductGroup
{
    public int MyIdxSeq { get; set; }
}

public class Mst_ProductGroupConfiguration : IEntityTypeConfiguration<Mst_ProductGroup>
{
    public void Configure(EntityTypeBuilder<Mst_ProductGroup> b)
    {
        b.ToTable("Mst_ProductGroup");
        // Không có DDL/model nguồn để suy khoá ⇒ không khai khoá nghiệp vụ (bảng không được đọc/ghi bởi action DoSearch).
        b.HasNoKey();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MiniWMS.Models.Port;

/// <summary>
/// Bảng sequence Seq_IF_InvInNo — sinh mã IF_InvInNo (Seq.cs:28-41, 78).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Biz\Seq.cs:28-41 Seq_Common_Raw
/// (`insert into {tbl} values (null); delete {tbl} where AutoID = @@Identity; select @@Identity Tid;`).
/// htMap IFInvInNo: "Seq_IF_InvInNo", format "{0}PN.{3}.{1:00000}{2}", maxSeq 100000 (Seq.cs:78).
/// PortSeq.NextAsync&lt;Seq_IF_InvInNo&gt; sinh số tương đương (Data/PortSeq.cs:44-61).
/// KHÔNG seeder: bảng sequence phải rỗng; NextAsync tự insert/delete mỗi lần gọi.
/// </summary>
public class Seq_IF_InvInNo
{
    public int AutoID { get; set; }
}

public class Seq_IF_InvInNoConfiguration : IEntityTypeConfiguration<Seq_IF_InvInNo>
{
    public void Configure(EntityTypeBuilder<Seq_IF_InvInNo> b)
    {
        b.ToTable("Seq_IF_InvInNo");
        b.HasKey(x => x.AutoID);
        b.Property(x => x.AutoID).ValueGeneratedOnAdd();
    }
}

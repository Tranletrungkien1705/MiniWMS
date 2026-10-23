using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 — `Inv_InventoryBalanceFormBlock` (bảng chặn tồn kho, bị XOÁ khi huỷ phiếu nhập kho).
/// Nguồn dùng trong `InvF_InventoryIn_CancelX` (Form.cs:157650-157688) qua
/// `Inv_InventoryBalanceFormBlock_Delete` (Form.cs:92762-92823).
/// Cột lấy từ temp table `#input_Inv_InventoryBalanceFormBlock` (Form.cs:92776-92785) — đó cũng là
/// các cột SELECT trong câu `strSqlCheck` (Form.cs:157654-157674).
/// Không có DDL chính thức trong Common → dùng surrogate `AutoID` để làm khoá trên SQLite (giống `Inv_InventoryBalance`).
/// </summary>
public class Inv_InventoryBalanceFormBlock
{
    public long AutoID { get; set; }
    public string? OrgID { get; set; }
    public string? InvCode { get; set; }
    public string? ProductCode { get; set; }
    public string? RefNo { get; set; }
    public string? NetworkID { get; set; }
    public string? RefType { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Inv_InventoryBalanceFormBlockConfiguration : IEntityTypeConfiguration<Inv_InventoryBalanceFormBlock>
{
    public void Configure(EntityTypeBuilder<Inv_InventoryBalanceFormBlock> b)
    {
        b.ToTable("Inv_InventoryBalanceFormBlock");
        b.HasKey(x => x.AutoID);
        b.Property(x => x.AutoID).ValueGeneratedOnAdd();
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.InvCode).HasMaxLength(50);
        b.Property(x => x.ProductCode).HasMaxLength(50);
        b.Property(x => x.RefNo).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.RefType).HasMaxLength(10);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>
/// Seed demo cho Inv_InventoryBalanceFormBlock (Order 44 — sau Inv_InventoryBalance Order 43).
/// 3 dòng:
///  - (PNK001, KHO01): trùng dòng Dtl PNK001 → SẼ bị xoá khi Cancel PNK001.
///  - (PNK001, KHO02): cùng RefNo nhưng KHÁC InvCode (không nằm trong Dtl PNK001) → giữ nguyên.
///  - (PNK002, KHO01): khác RefNo → giữ nguyên.
/// </summary>
public class Inv_InventoryBalanceFormBlockSeeder : IPortSeeder
{
    public int Order => 44;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Inv_InventoryBalanceFormBlock>().AnyAsync()) return;
        var now = DateTime.UtcNow;
        db.Set<Inv_InventoryBalanceFormBlock>().AddRange(
            new Inv_InventoryBalanceFormBlock { OrgID = "ORG01", InvCode = "KHO01", ProductCode = "SP01", RefNo = "PNK001", NetworkID = "1001", RefType = "IN", LogLUBy = "seed", LogLUDTimeUTC = now },
            new Inv_InventoryBalanceFormBlock { OrgID = "ORG01", InvCode = "KHO02", ProductCode = "SP02", RefNo = "PNK001", NetworkID = "1001", RefType = "IN", LogLUBy = "seed", LogLUDTimeUTC = now },
            new Inv_InventoryBalanceFormBlock { OrgID = "ORG01", InvCode = "KHO01", ProductCode = "SP02", RefNo = "PNK002", NetworkID = "1001", RefType = "IN", LogLUBy = "seed", LogLUDTimeUTC = now }
        );
        await db.SaveChangesAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// Seed demo cho #122 InvF_InventoryOutController/ComboAppr (Order 240 — nghiệp vụ phiếu xuất chi tiết).
/// Nguồn: InvF_InventoryOutController.cs:2290-2344 — Lst_InvF_InventoryOutDtl của phiếu OUT001/OUT002.
/// Nguồn biz Form.cs:34161-34165 (inner join InvF_InventoryOutDtl) ⇒ DTL chỉ trả khi header tồn tại.
/// Thêm header OUT002 (ngoài OUT001 của InvF_InventoryOutSeeder Order 17) để kiểm tra 2 bộ lọc IF_InvOutNo
/// trả về số dòng khác nhau (3 vs 2). KHÔNG sửa seeder khác (LESSONS A.24).
/// </summary>
public class InvF_InventoryOutDtlSeeder : IPortSeeder
{
    public int Order => 240;

    public async Task SeedAsync(AppDbContext db)
    {
        // Header OUT002 — không đụng OUT001 (InvF_InventoryOutSeeder Order 17).
        if (!await db.Set<InvF_InventoryOut>().AnyAsync(x => x.IF_InvOutNo == "OUT002"))
        {
            db.Set<InvF_InventoryOut>().Add(new InvF_InventoryOut
            {
                IF_InvOutNo = "OUT002",
                OrgID = "ORG01",
                InvOutType = "OUT_TYPE_1",
                CustomerCode = "CUST02",
                InvCodeOut = "INV01",
                RefNo = "002",
                RefType = "RO",
                BusinessGroupCode = "BG01",
                ProfileStatus = "NEW",
                IF_InvOutStatus = "APPROVE",
                FlagIsDelete = "0",
                CreateDTimeUTC = DateTime.UtcNow,
                CreateBy = "admin"
            });
            await db.SaveChangesAsync();
        }

        if (await db.Set<InvF_InventoryOutDtl>().AnyAsync()) return;
        db.Set<InvF_InventoryOutDtl>().AddRange(
            new InvF_InventoryOutDtl { IF_InvOutNo = "OUT001", ProductCode = "PROD01" },
            new InvF_InventoryOutDtl { IF_InvOutNo = "OUT001", ProductCode = "PROD02" },
            new InvF_InventoryOutDtl { IF_InvOutNo = "OUT001", ProductCode = "PROD03" },
            new InvF_InventoryOutDtl { IF_InvOutNo = "OUT002", ProductCode = "PROD01" },
            new InvF_InventoryOutDtl { IF_InvOutNo = "OUT002", ProductCode = "PROD04" }
        );
        await db.SaveChangesAsync();
    }
}

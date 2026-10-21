using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;
using System.Text.Json;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryOutController.DeleteMulti
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\InvF_InventoryOutController.cs:794-857
/// </summary>
[ApiController]
public class InvF_InventoryOutControllerPort(AppDbContext db) : ControllerBase
{
    public class DeleteMultiReq
    {
        public List<InvF_InventoryOut>? Lst_InvF_InventoryOut { get; set; }
        public string? LstJson { get; set; }
        public string? lstInvF_InventoryOut { get; set; }
    }

    [HttpPost("/api/invf_inventoryout/deletemulti")]
    public async Task<ActionResult> DeleteMulti([FromBody] DeleteMultiReq? req)
    {
        List<InvF_InventoryOut>? listInvF_InventoryOut = req?.Lst_InvF_InventoryOut;
        if (listInvF_InventoryOut == null && !string.IsNullOrEmpty(req?.LstJson))
        {
            try
            {
                listInvF_InventoryOut = JsonSerializer.Deserialize<List<InvF_InventoryOut>>(req.LstJson);
            }
            catch
            {
            }
        }
        if (listInvF_InventoryOut == null && !string.IsNullOrEmpty(req?.lstInvF_InventoryOut))
        {
            try
            {
                listInvF_InventoryOut = JsonSerializer.Deserialize<List<InvF_InventoryOut>>(req.lstInvF_InventoryOut);
            }
            catch
            {
            }
        }

        if (listInvF_InventoryOut == null || listInvF_InventoryOut.Count == 0)
        {
            return BadRequest(PortResult.Fail("InvF_InventoryOut_DeleteMulti", "Check.strJson", "Không tồn tại danh sách cần xóa !"));
        }

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in listInvF_InventoryOut)
            {
                if (item == null || string.IsNullOrEmpty(item.IF_InvOutNo)) continue;

                var ifInvOutNo = item.IF_InvOutNo.Trim().ToUpper();
                var entity = await db.Set<InvF_InventoryOut>().FirstOrDefaultAsync(x => x.IF_InvOutNo != null && x.IF_InvOutNo.ToUpper() == ifInvOutNo);

                if (entity != null)
                {
                    entity.FlagIsDelete = "1";
                    entity.LogLUDTimeUTC = DateTime.UtcNow;
                }
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                Success = true,
                Messages = "Xóa phiếu xuất kho thành công!"
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return BadRequest(PortResult.Fail("InvF_InventoryOut_DeleteMulti", "Exception", ex.Message));
        }
    }
}

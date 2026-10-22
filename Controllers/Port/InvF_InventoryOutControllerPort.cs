using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port
{
    [ApiController]
    [Route("api/invf_inventoryout")]
    public class InvF_InventoryOutControllerPort(AppDbContext db) : ControllerBase
    {
        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] InvF_InventoryOutUpdateRequest? req)
        {
            if (req == null || string.IsNullOrEmpty(req.IF_InvOutNo))
            {
                return PortResult.Fail("InvF_InventoryOut_Update", "Check.IF_InvOutNo", null);
            }

            using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                var item = await db.Set<InvF_InventoryOut>().FirstOrDefaultAsync(x => x.IF_InvOutNo == req.IF_InvOutNo);
                if (item == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("InvF_InventoryOut_Update", "Check.IF_InvOutNoNotFound", req.IF_InvOutNo);
                }

                item.CustomerCode = !string.IsNullOrEmpty(req.CustomerCode) ? req.CustomerCode.Trim() : item.CustomerCode;
                item.InvCodeOut = !string.IsNullOrEmpty(req.InvCodeOut) ? req.InvCodeOut.Trim() : item.InvCodeOut;
                item.RefNo = !string.IsNullOrEmpty(req.RefNo) ? req.RefNo.Trim() : item.RefNo;
                item.RefType = !string.IsNullOrEmpty(req.RefType) ? req.RefType.Trim() : item.RefType;
                item.BusinessGroupCode = !string.IsNullOrEmpty(req.BusinessGroupCode) ? req.BusinessGroupCode.Trim() : item.BusinessGroupCode;
                item.LUDTimeUTC = DateTime.UtcNow;

                db.Set<InvF_InventoryOut>().Update(item);
                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { Success = true, Data = item });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("InvF_InventoryOut_Update", "Exception", ex.Message);
            }
        }
    }

    public class InvF_InventoryOutUpdateRequest
    {
        public string? IF_InvOutNo { get; set; }
        public string? CustomerCode { get; set; }
        public string? InvCodeOut { get; set; }
        public string? RefNo { get; set; }
        public string? RefType { get; set; }
        public string? BusinessGroupCode { get; set; }
    }
}

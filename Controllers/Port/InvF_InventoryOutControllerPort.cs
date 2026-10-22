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

        [HttpPost("detail")]
        public async Task<IActionResult> Detail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] InvF_InventoryOutDetailRequest? req)
        {
            if (req == null || string.IsNullOrEmpty(req.IF_InvOutNo))
            {
                return PortResult.Fail("InvF_InventoryOut_Get", "Check.IF_InvOutNo", null);
            }

            try
            {
                var item = await db.Set<InvF_InventoryOut>().FirstOrDefaultAsync(x => x.IF_InvOutNo == req.IF_InvOutNo);
                if (item == null)
                {
                    return PortResult.Fail("InvF_InventoryOut_Get", "Check.IF_InvOutNoNotFound", req.IF_InvOutNo);
                }

                if (CUtilsStrValue(item.RefType) == "RO")
                {
                    item.RefNo = "RO-" + CUtilsStrValue(item.RefNo);
                }

                var covers = await db.Set<InvF_InventoryOutCover>().Where(x => x.IF_InvOutNo == req.IF_InvOutNo).ToListAsync();
                var lots = await db.Set<InvF_InventoryOutInstLot>().Where(x => x.IF_InvOutNo == req.IF_InvOutNo).ToListAsync();
                var serials = await db.Set<InvF_InventoryOutInstSerial>().Where(x => x.IF_InvOutNo == req.IF_InvOutNo).ToListAsync();
                var dtls = await db.Set<InvF_InventoryOutDtl>().Where(x => x.IF_InvOutNo == req.IF_InvOutNo).ToListAsync();
                var qrs = await db.Set<InvF_InventoryOutQR>().Where(x => x.IF_InvOutNo == req.IF_InvOutNo).ToListAsync();
                var attachFiles = await db.Set<InvF_InventoryOutAttachFile>().Where(x => x.IF_InvOutNo == req.IF_InvOutNo).ToListAsync();

                return Ok(new
                {
                    Success = true,
                    Data = item,
                    Lst_InvF_InventoryOutCover = covers,
                    Lst_InvF_InventoryOutInstLot = lots,
                    Lst_InvF_InventoryOutInstSerial = serials,
                    Lst_InvF_InventoryOutDtl = dtls,
                    Lst_InvF_InventoryOutQR = qrs,
                    Lst_InvF_InventoryOutAttachFile = attachFiles
                });
            }
            catch (Exception ex)
            {
                return PortResult.Fail("InvF_InventoryOut_Get", "Exception", ex.Message);
            }
        }

        private static string CUtilsStrValue(object? val)
        {
            return val?.ToString() ?? string.Empty;
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

    public class InvF_InventoryOutDetailRequest
    {
        public string? IF_InvOutNo { get; set; }
    }
}

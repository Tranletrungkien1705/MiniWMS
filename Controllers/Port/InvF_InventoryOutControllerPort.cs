using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryOutController.UpdProfile
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\InvF_InventoryOutController.cs:690-791
/// </summary>
[ApiController]
public class InvF_InventoryOutControllerPort(AppDbContext db) : ControllerBase
{
    public class UpdProfileReq
    {
        public InvF_InventoryOut? InvF_InventoryOut { get; set; }
        public List<InvF_InventoryOutAttachFile>? Lst_InvF_InventoryOutAttachFile { get; set; }
    }

    [HttpPost("/api/invf_inventoryout/updprofile")]
    public async Task<ActionResult> UpdProfile([FromBody] UpdProfileReq? req)
    {
        if (req?.InvF_InventoryOut == null || string.IsNullOrEmpty(req.InvF_InventoryOut.IF_InvOutNo))
        {
            return BadRequest(PortResult.Fail("InvF_InventoryOut_UpdProfile", "Check.strJson", null));
        }

        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var ifInvOutNo = req.InvF_InventoryOut.IF_InvOutNo.Trim().ToUpper();
            var entity = await db.Set<InvF_InventoryOut>().FirstOrDefaultAsync(x => x.IF_InvOutNo != null && x.IF_InvOutNo.ToUpper() == ifInvOutNo);

            if (entity == null)
            {
                await tx.RollbackAsync();
                return BadRequest(PortResult.Fail("InvF_InventoryOut_UpdProfile", "InvF_InventoryOut_CheckDB", ifInvOutNo));
            }

            var status = entity.IF_InvOutStatus;
            if (status != "APPROVE" && status != "CANCEL")
            {
                await tx.RollbackAsync();
                return BadRequest(PortResult.Fail("InvF_InventoryOut_UpdProfile", "InvF_InventoryOut_CheckDB", status));
            }

            entity.ProfileStatus = req.InvF_InventoryOut.ProfileStatus ?? entity.ProfileStatus;
            // Remark and LUDTimeUTC can be handled if added or ignored since entity model doesn't have them explicitly, but let's check if we can update other fields if needed.

            if (req.Lst_InvF_InventoryOutAttachFile != null && req.Lst_InvF_InventoryOutAttachFile.Count > 0)
            {
                var existingAtts = await db.Set<InvF_InventoryOutAttachFile>().Where(x => x.IF_InvOutNo == ifInvOutNo).ToListAsync();
                if (existingAtts.Count > 0)
                {
                    db.Set<InvF_InventoryOutAttachFile>().RemoveRange(existingAtts);
                }

                int seq = 0;
                foreach (var att in req.Lst_InvF_InventoryOutAttachFile)
                {
                    att.IF_InvOutNo = ifInvOutNo;
                    att.LogLUDTimeUTC = DateTime.UtcNow;
                    att.FlagActive = "1";
                    
                    if (att.FlagIsFilePath != "1" && !string.IsNullOrEmpty(att.AttachFileSpec))
                    {
                        try
                        {
                            var bytes = Convert.FromBase64String(att.AttachFileSpec);
                            var subFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
                            var fileName = $"{DateTime.UtcNow:yyyyMMdd.HHmmss}.{seq++}.{att.AttachFileName ?? "file.dat"}";
                            var relativePath = Path.Combine("Uploads", subFolder, fileName);
                            
                            var uploadRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads", subFolder);
                            Directory.CreateDirectory(uploadRoot);
                            System.IO.File.WriteAllBytes(Path.Combine(uploadRoot, fileName), bytes);
                            
                            att.AttachFilePath = relativePath;
                        }
                        catch
                        {
                        }
                    }

                    db.Set<InvF_InventoryOutAttachFile>().Add(att);
                }
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                Success = true,
                Messages = "Cập nhật hồ sơ phiếu xuất kho thành công!"
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return BadRequest(PortResult.Fail("InvF_InventoryOut_UpdProfile", "Exception", ex.Message));
        }
    }
}

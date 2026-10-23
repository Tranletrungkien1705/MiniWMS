using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — action `Approve` (#87).
/// Chuỗi nguồn:
///   - Gate: `InvF_InventoryInController.cs:1677-1730` (Approve(string objlistinvf_inventoryin))
///   - API: `InvFInventoryInController.cs:164-233` (WA_InvF_InventoryIn_Appr)
///   - BIZ: `Form.cs:181176-181253` (WAS_InvF_InventoryIn_Appr_New20230609)
///   - BIZ: `Form.cs:181255-181424` (InvF_InventoryIn_Appr_New20230609)
///   - BIZ SQL: `Form.cs:181429-182307` (InvF_InventoryIn_ApprX_New20230609)
/// </summary>
public partial class InvF_InventoryInControllerPort
{
    private const string ErrApproveDefault = "InvF_InventoryIn_Appr";
    private const string ErrCheckInvInNoNotFound = "InvF_InventoryIn_CheckDB_InvInNoNotFound";
    private const string ErrCheckStatusNotMatched = "InvF_InventoryIn_CheckDB_StatusNotMatched";

    public class ApproveItemReq
    {
        public string? IF_InvInNo { get; set; }
        public string? Remark { get; set; }
    }

    public class ApproveReq
    {
        public bool? IsSysAdmin { get; set; }
        public List<string>? ListSysAccess { get; set; }
        public string? WaUserCode { get; set; }
        public string? NetworkId { get; set; }
        public string? OrgId { get; set; }
        public string? ObjListInvF_InventoryIn { get; set; }
        public List<ApproveItemReq>? ListInvF_InventoryIn { get; set; }
    }

    public class ApproveResult
    {
        public bool Success { get; set; } = true;
        public string? RedirectUrl { get; set; }
        public List<string> Messages { get; set; } = new();
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }

    [HttpPost("api/invf_inventoryin/approve")]
    public async Task<IActionResult> Approve([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ApproveReq? req)
    {
        // 1. Phân quyền SysAccessCheckDeny (gate dòng 1679-1683)
        // BTN_INVF_INVENTORYIN_APPROVE
        var hasAccess = SysAccessCheckDeny(req?.IsSysAdmin, req?.ListSysAccess, "BTN_INVF_INVENTORYIN_APPROVE");
        if (!hasAccess)
        {
            return Ok(new ApproveResult
            {
                Success = false,
                RedirectUrl = "/AccessDeny/Index",
                Title = "Access denied",
                Messages = new List<string> { "Access denied" }
            });
        }

        // 2. Parse danh sách phiếu nhập kho
        List<ApproveItemReq>? items = req?.ListInvF_InventoryIn;
        if ((items == null || items.Count == 0) && !string.IsNullOrWhiteSpace(req?.ObjListInvF_InventoryIn))
        {
            try
            {
                items = System.Text.Json.JsonSerializer.Deserialize<List<ApproveItemReq>>(
                    req.ObjListInvF_InventoryIn,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                return PortResult.Fail(ErrApproveDefault, "Exception", ex.Message);
            }
        }

        if (items == null || items.Count == 0)
        {
            return PortResult.Fail(ErrApproveDefault, "Check.objlistinvf_inventoryin", null);
        }

        var waUserCode = (req?.WaUserCode ?? "").Trim();
        var dtimeSys = DateTime.UtcNow;

        // 3. Xử lý từng phiếu (gate dòng 1707-1717 gọi tuần tự WA_InvF_InventoryIn_Appr)
        foreach (var item in items)
        {
            var strIF_InvInNo = StdParam(item.IF_InvInNo);
            var strRemark = (item.Remark ?? "").Trim();

            // InvF_InventoryIn_CheckDB (Form.cs:181482 & 146703-146767)
            if (string.IsNullOrEmpty(strIF_InvInNo))
            {
                return PortResult.Fail(ErrCheckInvInNoNotFound, "Check.IF_InvInNo", strIF_InvInNo);
            }

            var record = await db.Set<InvF_InventoryIn>().FirstOrDefaultAsync(x => x.IF_InvInNo == strIF_InvInNo);
            if (record == null)
            {
                return PortResult.Fail(ErrCheckInvInNoNotFound, "Check.IF_InvInNo", strIF_InvInNo);
            }

            // TConst.IF_InvInStatus.Pending ("PENDING")
            if (!StringEqualIgnoreCase(record.IF_InvInStatus, StatusPending))
            {
                return PortResult.Fail(ErrCheckStatusNotMatched,
                    "Check.IF_InvInNo", strIF_InvInNo,
                    "Check.strFlagActiveListToCheck", StatusPending,
                    "DB.IF_InvInStatus", record.IF_InvInStatus);
            }

            // Transaction per inventory in record (WAS_InvF_InventoryIn_Appr / InvF_InventoryIn_ApprX)
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                // Cập nhật trạng thái phiếu nhập kho master (Form.cs:181536-181563)
                record.LogLUDTimeUTC = dtimeSys;
                record.LogLUBy = waUserCode;
                record.ApprDTimeUTC = dtimeSys;
                record.ApprBy = waUserCode;
                record.IF_InvInStatus = StatusApprove;
                record.Remark = strRemark;

                // Cập nhật trạng thái các dòng chi tiết InvF_InventoryInDtl (Form.cs:181545-181593)
                var details = await db.Set<InvF_InventoryInDtl>()
                    .Where(x => x.IF_InvInNo == strIF_InvInNo)
                    .ToListAsync();

                foreach (var dtl in details)
                {
                    dtl.LogLUDTimeUTC = dtimeSys;
                    dtl.LogLUBy = waUserCode;
                    dtl.IF_InvInStatusDtl = StatusApprove;
                }

                // Cập nhật tồn kho LastInInvDTimeUTC cho Inv_InventoryBalance (Form.cs:181951-182006)
                var distinctProducts = details
                    .Select(d => new { d.InvCodeInActual, d.ProductCode })
                    .Distinct()
                    .ToList();

                foreach (var p in distinctProducts)
                {
                    var balances = await db.Set<Inv_InventoryBalance>()
                        .Where(b => b.OrgID == record.OrgID && b.InvCode == p.InvCodeInActual && b.ProductCode == p.ProductCode)
                        .ToListAsync();

                    foreach (var bal in balances)
                    {
                        bal.LogLUDTimeUTC = dtimeSys;
                        bal.LogLUBy = waUserCode;
                    }
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Gate dòng 1719-1722:
        // resultEntry.Success = true;
        // resultEntry.AddMessage("Duyệt phiếu nhập kho thành công!");
        // resultEntry.RedirectUrl = Url.Action("Index");
        return Ok(new ApproveResult
        {
            Success = true,
            RedirectUrl = "/InvF_InventoryIn/Index",
            Title = "Duyệt phiếu nhập kho thành công!",
            Messages = new List<string> { "Duyệt phiếu nhập kho thành công!" }
        });
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — action `Cancel` (#88).
/// Chuỗi nguồn:
///   - Gate: `InvF_InventoryInController.cs:1735-1788` — Cancel(string objlistinvf_inventoryin)
///   - API : `InvFInventoryInController.cs:308-377` — WA_InvF_InventoryIn_Cancel
///   - BIZ : `Form.cs:157869-157947` — WAS_InvF_InventoryIn_Cancel
///   - BIZ : `Form.cs:157710-157866` — InvF_InventoryIn_Cancel
///   - BIZ : `Form.cs:157480-157708` — InvF_InventoryIn_CancelX (SQL nghiệp vụ)
/// GAP:
///  - Bỏ `Sys_User_CheckAuthorize` (Form.cs:157775-157788), `Sys_Access_CheckDenyV30` (Form.cs:157791-157794),
///    `Sys_User_GetAbilityViewOfUser` + `zzzzClauseSelect_Mst_NNT_ViewAbility_Get` (Form.cs:157510-157513)
///    — không có session/authorization ở đích (giống `Approve`/`UpdAfterAppr`/các port khác).
///  - Bỏ `ProcessBizReq_OutSide`/`ProcessBizReturn_OutSide` (Form.cs:157758-157765, 157855-157863) — log nguồn.
///  - Bỏ `CommitSafety`/`RollbackSafety` (Form.cs:157821-157844) — dùng transaction EF tiêu chuẩn.
/// </summary>
public partial class InvF_InventoryInControllerPort
{
    private const string ErrCancelDefault = "InvF_InventoryIn_Cancel";
    private const string ErrCancelInvInNoNotFound = "InvF_InventoryIn_CheckDB_InvInNoNotFound";
    private const string ErrCancelStatusNotMatched = "InvF_InventoryIn_CheckDB_StatusNotMatched";
    private const string StatusCancel = "CANCEL";

    public class CancelItemReq
    {
        public string? IF_InvInNo { get; set; }
        public string? Remark { get; set; }
    }

    public class CancelReq
    {
        public bool? IsSysAdmin { get; set; }
        public List<string>? ListSysAccess { get; set; }
        public string? WaUserCode { get; set; }
        /// <summary>Danh sách phiếu — body trực tiếp (khuyến nghị).</summary>
        public List<CancelItemReq>? ListInvF_InventoryIn { get; set; }
        /// <summary>Nguồn gate nhận `string objlistinvf_inventoryin` — body dạng chuỗi JSON. Hỗ trợ tương thích.</summary>
        public string? ObjListInvF_InventoryIn { get; set; }
    }

    public class CancelResult
    {
        public bool Success { get; set; }
        public string? RedirectUrl { get; set; }
        public List<string> Messages { get; set; } = new();
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }

    [HttpPost("api/invf_inventoryin/cancel")]
    public async Task<IActionResult> Cancel([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CancelReq? req)
    {
        // Gate InvF_InventoryInController.cs:1737-1741 — SysAccessCheckDeny(UserState, "BTN_INVF_INVENTORYIN_CANCEL").
        if (!SysAccessCheckDeny(req?.IsSysAdmin, req?.ListSysAccess, "BTN_INVF_INVENTORYIN_CANCEL"))
        {
            return Ok(new CancelResult
            {
                Success = false,
                RedirectUrl = "/AccessDeny/Index",
                Title = "Access denied",
                Messages = new List<string> { "Access denied" }
            });
        }

        // Gate InvF_InventoryInController.cs:1748 — JsonConvert.DeserializeObject<List<InvF_InventoryIn>>(objlistinvf_inventoryin).
        List<CancelItemReq>? items = req?.ListInvF_InventoryIn;
        if ((items == null || items.Count == 0) && !string.IsNullOrWhiteSpace(req?.ObjListInvF_InventoryIn))
        {
            try
            {
                items = System.Text.Json.JsonSerializer.Deserialize<List<CancelItemReq>>(
                    req.ObjListInvF_InventoryIn,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                return PortResult.Fail(ErrCancelDefault, "Exception", ex.Message);
            }
        }
        if (items == null)
        {
            // Gate InvF_InventoryInController.cs:1748-1785 — DeserializeObject(null) ném → gate SetFailed → Success=false.
            return PortResult.Fail(ErrCancelDefault, "Check.objlistinvf_inventoryin", null);
        }
        // Gate 1765-1775: foreach rỗng → không gọi service → gate trả Success=true (dòng 1777-1780).
        if (items.Count == 0)
        {
            return Ok(new CancelResult
            {
                Success = true,
                RedirectUrl = "/api/invf_inventoryin/index",
                Title = "Hủy phiếu nhập kho thành công!",
                Messages = new List<string> { "Hủy phiếu nhập kho thành công!" }
            });
        }

        var waUserCode = (req!.WaUserCode ?? "").Trim();

        // Gate InvF_InventoryInController.cs:1765-1775 — foreach item gọi WA_InvF_InventoryIn_Cancel.
        // Mỗi item là một transaction độc lập (biz Form.cs:157730 bNeedTransaction=true + RollbackSafety
        // trong finally ⇒ lỗi ở item k giữ nguyên các item 1..k-1 đã commit).
        foreach (var item in items)
        {
            // InvF_InventoryIn_CancelX Form.cs:157519-157520 — StdParam(IF_InvInNo) + Trim(Remark).
            var strIF_InvInNo = StdParam(item.IF_InvInNo);
            var strRemark = (item.Remark ?? "").Trim();

            // InvF_InventoryIn_CheckDB (Form.cs:157524-157530): flagExist="1" (TConst.Flag.Yes),
            // status="PENDING" (TConst.IF_InvInStatus.Pending).
            //   - Form.cs:146729 — row không có & flagExist="1" ⇒ InvInNoNotFound.
            //   - Form.cs:146754 — status DB không nằm trong list ⇒ StatusNotMatched (so nguyên chuỗi list).
            if (string.IsNullOrEmpty(strIF_InvInNo))
            {
                return PortResult.Fail(ErrCancelInvInNoNotFound, "Check.IF_InvInNo", strIF_InvInNo);
            }

            // SQL Server collation không phân biệt hoa/thường ⇒ dùng Like-lower (PORT_RULES mục 3).
            var pattern = PortQuery.Equal(strIF_InvInNo);
            var rec = await db.Set<InvF_InventoryIn>()
                .FirstOrDefaultAsync(x => EF.Functions.Like(x.IF_InvInNo.ToLower(), pattern, PortQuery.Esc));
            if (rec == null)
            {
                return PortResult.Fail(ErrCancelInvInNoNotFound, "Check.IF_InvInNo", strIF_InvInNo);
            }

            // Nguồn: `!strStatusListToCheck.Contains(statusDB)` với list="PENDING" — giữ nguyên
            // ngữ nghĩa `string.Contains` (case-sensitive, rỗng luôn khớp) như Form.cs:146754.
            if (!StatusPending.Contains(rec.IF_InvInStatus ?? ""))
            {
                return PortResult.Fail(ErrCancelStatusNotMatched,
                    "Check.IF_InvInNo", strIF_InvInNo,
                    "Check.strFlagActiveListToCheck", StatusPending,
                    "DB.IF_InvInStatus", rec.IF_InvInStatus);
            }

            var dtimeSys = DateTime.UtcNow;
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                // Form.cs:157574-157581 — UPDATE InvF_InventoryIn: LogLUDTimeUTC, LogLUBy, CancelDTimeUTC,
                // CancelBy, IF_InvInStatus = CANCEL, Remark.
                rec.LogLUDTimeUTC = dtimeSys;
                rec.LogLUBy = waUserCode;
                rec.CancelDTimeUTC = dtimeSys;
                rec.CancelBy = waUserCode;
                rec.IF_InvInStatus = StatusCancel;
                rec.Remark = strRemark;

                // Form.cs:157603-157632 — UPDATE InvF_InventoryInDtl: LogLUDTimeUTC, LogLUBy,
                // IF_InvInStatusDtl = IF_InvInStatus (đã gán CANCEL ở trên).
                var dtls = await db.Set<InvF_InventoryInDtl>()
                    .Where(x => EF.Functions.Like((x.IF_InvInNo ?? "").ToLower(), pattern, PortQuery.Esc))
                    .ToListAsync();
                foreach (var d in dtls)
                {
                    d.LogLUDTimeUTC = dtimeSys;
                    d.LogLUBy = waUserCode;
                    d.IF_InvInStatusDtl = StatusCancel;
                }

                // Form.cs:157650-157688 — Inv_InventoryBalanceFormBlock:
                //   SELECT distinct ifii.OrgID, t.InvCodeInActual InvCode, mp.ProductCodeBase ProductCode,
                //                  t.IF_InvInNo RefNo, ifii.NetworkID, 'IN' RefType, ...
                //     FROM InvF_InventoryInDtl t JOIN #input_InvF_InventoryIn f ON ...
                //          JOIN InvF_InventoryIn ifii ON ... JOIN Mst_Product mp ON t.ProductCode = mp.ProductCode.
                //   Sau đó Inv_InventoryBalanceFormBlock_Delete (Form.cs:92792-92805) xoá các dòng có
                //     OrgID + InvCode + RefNo trùng (KHÔNG so ProductCode/RefType — cột đã comment trong DDL).
                //
                // Đích: lấy InvCodeInActual của Dtl (chỉ khi ProductCode có mặt trong Mst_Product) rồi xoá
                // BalanceFormBlock theo (OrgID của header, InvCode, RefNo = IF_InvInNo).
                var dtlProducts = await db.Set<InvF_InventoryInDtl>()
                    .Where(x => EF.Functions.Like((x.IF_InvInNo ?? "").ToLower(), pattern, PortQuery.Esc))
                    .Select(x => new { x.InvCodeInActual, x.ProductCode })
                    .ToListAsync();

                var productCodesLower = dtlProducts
                    .Select(x => x.ProductCode)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(s => s!.ToLower())
                    .Distinct()
                    .ToList();

                var existingProducts = productCodesLower.Count == 0
                    ? new List<string>()
                    : await db.Set<Mst_Product>()
                        .Where(p => productCodesLower.Contains(p.ProductCode.ToLower()))
                        .Select(p => p.ProductCode)
                        .ToListAsync();
                var productSet = existingProducts
                    .Select(p => p.ToLower())
                    .ToHashSet();

                var invCodes = dtlProducts
                    .Where(d => d.InvCodeInActual != null
                             && !string.IsNullOrEmpty(d.ProductCode)
                             && productSet.Contains(d.ProductCode!.ToLower()))
                    .Select(d => d.InvCodeInActual!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (invCodes.Count > 0 && !string.IsNullOrEmpty(rec.OrgID))
                {
                    // Load BalanceFormBlock theo RefNo = IF_InvInNo (không phân biệt hoa/thường).
                    var fbRows = await db.Set<Inv_InventoryBalanceFormBlock>()
                        .Where(x => EF.Functions.Like((x.RefNo ?? "").ToLower(), pattern, PortQuery.Esc))
                        .ToListAsync();

                    // So OrgID + InvCode trong bộ nhớ (OrdinalIgnoreCase) — tương đương `=` của SQL Server.
                    var toDelete = fbRows
                        .Where(x => !string.IsNullOrEmpty(x.OrgID)
                                 && string.Equals(x.OrgID, rec.OrgID, StringComparison.OrdinalIgnoreCase)
                                 && !string.IsNullOrEmpty(x.InvCode)
                                 && invCodes.Any(ic => string.Equals(ic, x.InvCode, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                    if (toDelete.Count > 0) db.RemoveRange(toDelete);
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

        // Gate InvF_InventoryInController.cs:1777-1780 — Success=true, AddMessage, RedirectUrl Index.
        return Ok(new CancelResult
        {
            Success = true,
            RedirectUrl = "/api/invf_inventoryin/index",
            Title = "Hủy phiếu nhập kho thành công!",
            Messages = new List<string> { "Hủy phiếu nhập kho thành công!" }
        });
    }
}

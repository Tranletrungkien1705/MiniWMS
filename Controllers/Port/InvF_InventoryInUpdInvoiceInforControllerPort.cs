using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — action `UpdInvoiceInfor` (#94).
/// Chuỗi nguồn:
///   - Gate: `InvF_InventoryInController.cs:3219-3257` (UpdInvoiceInfor(string model)):
///       body là chuỗi JSON `model` → `JsonConvert.DeserializeObject&lt;InvF_InventoryIn&gt;(model)` (Gate:3229)
///       ⇒ dựng `RQ_InvF_InventoryIn` rồi gọi `WA_InvF_InventoryIn_UpdInvoiceInfor` (Gate:3245)
///       ⇒ thành công: `resultEntry.Success = true`, message "Sửa thông tin hóa đơn thành công!",
///         `RedirectUrl = Url.Action("Index")` (Gate:3246-3248)
///       ⇒ thất bại: `SetFailed().AddException(this, ex)` (Gate:3253) → JsonViewError trả `Success = false`.
///   - API : `InvFInventoryInController.cs:1102-1171` — WA_InvF_InventoryIn_UpdInvoiceInfor
///       (CheckGatewayAuthentication; gọi WAS; trả `ServiceResult&lt;RT_InvF_InventoryIn&gt;`).
///   - BIZ : `Form.cs:179667-179747` — WAS_InvF_InventoryIn_UpdInvoiceInfor
///       (chỉ forward + bọc lỗi mặc định `ErridnInventory.WAS_InvF_InventoryIn_UpdInvoiceInfor`).
///   - BIZ : `Form.cs:180070-180235` — InvF_InventoryIn_UpdInvoiceInfor_New20241109
///       (BeginTransaction, log yêu cầu, Sys_User_CheckAuthorize, Sys_Access_CheckDenyV30,
///        gọi `InvF_InventoryIn_UpdInvoiceInforX_New20241109`, CommitSafety — luôn rollback).
///   - BIZ : `Form.cs:180237-180397` — InvF_InventoryIn_UpdInvoiceInforX_New20241109 (SQL thật):
///       CheckDB + update các cột LUDTimeUTC / LUBy / LogLUDTimeUTC / LogLUBy /
///       InvoiceNo / InvoiceDate / InvoiceSign / BusinessGroupCode.
/// GAP:
///  - Bỏ `Sys_User_CheckAuthorize` (Form.cs:180141-180154), `Sys_Access_CheckDenyV30`
///    (Form.cs:180157-180161) — đích không có session/Sys_Access. GATE `UpdInvoiceInfor` KHÔNG gọi
///    SysAccessCheckDeny (khác Approve/Cancel/UpdAfterAppr), nên endpoint này KHÔNG kiểm tra quyền —
///    đúng hành vi gate.
///  - Bỏ `Sys_User_GetAbilityViewOfUser` + `zzzzClauseSelect_Mst_NNT_ViewAbility_Get`
///    (Form.cs:180274-180278) — nguồn tạo bảng tạm nhưng câu UPDATE (Form.cs:180355-180364)
///    KHÔNG join bảng tạm ⇒ bỏ không đổi kết quả.
///  - Bỏ `ProcessBizReq_OutSide`/`ProcessBizReturn_OutSide` (Form.cs:180124-180131, 180224-180231) — log nguồn.
///  - Bỏ `MyBuildDBDT_Common` (bảng tạm `#input_InvF_InventoryIn` của SQL Server) —
///    thay bằng transaction EF cập nhật trực tiếp cùng các cột cùng giá trị
///    (Form.cs:180305-180335 và 180341-180350).
/// </summary>
public partial class InvF_InventoryInControllerPort
{
    private const string ErrUpdInvoiceInforDefault = "InvF_InventoryIn_UpdInvoiceInfor"; // Error.idN.Inventory.cs:508
    private const string ErrUpdInvoiceInforInvInNoNotFound = "InvF_InventoryIn_CheckDB_InvInNoNotFound"; // Error.idN.Inventory.cs:365
    private const string ErrUpdInvoiceInforStatusNotMatched = "InvF_InventoryIn_CheckDB_StatusNotMatched"; // Error.idN.Inventory.cs:367
    private const string StatusApprove2 = "APPROVE"; // TConst.IF_InvInStatus.Approve (Const.Main.BE.cs:403)

    /// <summary>
    /// Body endpoint.
    /// Gate `UpdInvoiceInfor(string model)` nhận TRỰC TIẾP chuỗi JSON InvF_InventoryIn (Gate:3229)
    /// ⇒ ở đích `Model` LÀ đối tượng đó (đúng luật A.21).
    /// `WaUserCode` thay cho `UserState.SysUser.UserCode` không có ở đích (Gate:3238);
    /// chỉ dùng để ghi LUBy/LogLUBy (Form.cs:180328, 180330) — không dùng để phân quyền.
    /// </summary>
    public class UpdInvoiceInforReq
    {
        public string? WaUserCode { get; set; }
        public InvF_InventoryIn? Model { get; set; }
    }

    public class UpdInvoiceInforResult
    {
        public bool Success { get; set; } = true;
        public string? RedirectUrl { get; set; }
        public string? Title { get; set; }
        public List<string> Messages { get; set; } = new();
    }

    [HttpPost("api/invf_inventoryin/updinvoiceinfor")]
    public async Task<IActionResult> UpdInvoiceInfor(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] UpdInvoiceInforReq? req)
    {
        try
        {
            // Gate:3229 — deserialize `model`. null → InvF_InventoryIn null → NPE tại
            // Form.cs:179713 (`InvF_InventoryIn.IF_InvInNo`) ⇒ biz trả mã mặc định
            // `ErridnInventory.InvF_InventoryIn_UpdInvoiceInfor` (Form.cs:180095, 180204).
            if (req?.Model == null)
                return PortResult.Fail(ErrUpdInvoiceInforDefault, "Check.model", null);

            var header = req.Model;
            var strWAUserCode = req.WaUserCode ?? "";

            // Form.cs:180283-180287 — chuẩn hoá tham số đúng hàm nguồn.
            //   StdParam = Trim().ToUpper(), rỗng → null (MÃ)
            //   StdStr   = Trim(), rỗng → null (TÊN/ghi chú)
            //   StdDTime = null nếu rỗng; DateTime đã đúng kiểu ở đích (Utils.cs:97-104).
            var strIF_InvInNo = StdParam(header.IF_InvInNo);
            var strInvoiceNo = StdParam(header.InvoiceNo);
            var dtInvoiceDate = header.InvoiceDate;
            var strInvoiceSign = StdStr(header.InvoiceSign);
            var strBusinessGroupCode = StdParam(header.BusinessGroupCode);

            // InvF_InventoryIn_CheckDB (Form.cs:146703-146767):
            //   select top 1 t.* from InvF_InventoryIn where IF_InvInNo = @objIF_InvInNo
            //   - strFlagExistToCheck = "1" (TConst.Flag.Yes = "1"): rowcount < 1 → InvInNoNotFound
            //   - strStatusListToCheck = "APPROVE": !"APPROVE".Contains(status) → StatusNotMatched
            //     (form.cs:146754 dùng String.Contains case-sensitive — port bằng `string.Contains`).
            var record = await db.Set<InvF_InventoryIn>()
                .FirstOrDefaultAsync(x => x.IF_InvInNo == strIF_InvInNo);
            if (record == null)
            {
                return PortResult.Fail(ErrUpdInvoiceInforInvInNoNotFound,
                    "Check.IF_InvInNo", strIF_InvInNo);
            }
            if (!StatusApprove2.Contains(record.IF_InvInStatus ?? ""))
            {
                return PortResult.Fail(ErrUpdInvoiceInforStatusNotMatched,
                    "Check.IF_InvInNo", strIF_InvInNo,
                    "Check.strFlagActiveListToCheck", StatusApprove2,
                    "DB.IF_InvInStatus", record.IF_InvInStatus);
            }

            // Form.cs:180341-180350 — Update các cột (đủ theo nguồn):
            //   t.LogLUDTimeUTC / t.LogLUBy / t.LUDTimeUTC / t.LUBy /
            //   t.InvoiceNo / t.InvoiceDate / t.InvoiceSign / t.BusinessGroupCode
            // LUDTimeUTC = LogLUDTimeUTC = dtimeSys (Form.cs:180327, 180329);
            // LUBy = LogLUBy = strWAUserCode (Form.cs:180328, 180330).
            var dtimeSys = DateTime.UtcNow;
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                record.LUDTimeUTC = dtimeSys;
                record.LUBy = strWAUserCode;
                record.LogLUDTimeUTC = dtimeSys;
                record.LogLUBy = strWAUserCode;
                record.InvoiceNo = strInvoiceNo;
                record.InvoiceDate = dtInvoiceDate;
                record.InvoiceSign = strInvoiceSign;
                record.BusinessGroupCode = strBusinessGroupCode;

                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }

            // Gate:3246-3248 — Success = true, message + RedirectUrl = Url.Action("Index").
            return Ok(new UpdInvoiceInforResult
            {
                Success = true,
                RedirectUrl = "/InvF_InventoryIn/Index",
                Title = "Sửa thông tin hóa đơn thành công!",
                Messages = new() { "Sửa thông tin hóa đơn thành công!" }
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail(ErrUpdInvoiceInforDefault, "Exception", ex.Message);
        }
    }
}

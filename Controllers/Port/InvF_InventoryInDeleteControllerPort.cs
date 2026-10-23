using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — action `Delete` (#89).
/// Chuỗi nguồn:
///   - Gate: `InvF_InventoryInController.cs:1793-1875` (Delete(string objlistinvf_inventoryin))
///   - API: `InvFInventoryInController.cs:20-89` (WA_InvF_InventoryIn_Get) & `92-161` (WA_InvF_InventoryIn_Save)
///   - BIZ: `Form.cs:147339-147485` (WAS_InvF_InventoryIn_Get)
///   - BIZ: `Form.cs:147161-147337` (InvF_InventoryIn_Get)
///   - BIZ SQL: `Form.cs:146769-147160` (InvF_InventoryIn_GetX)
///   - BIZ: `Form.cs:169677-169823` (WAS_InvF_InventoryIn_Save_New20240606)
///   - BIZ: `Form.cs:169829-170089` (InvF_InventoryIn_Save_New20241112)
///   - BIZ SQL: `Form.cs:170091-172363` (InvF_InventoryIn_SaveX_New20241112)
/// </summary>
public partial class InvF_InventoryInControllerPort
{
    private const string ErrDeleteDefault = "InvF_InventoryIn_Save";
    private const string ErrDeleteCheckIF_InvInNo = "InvF_InventoryIn_SaveX_InvalidIF_InvInNo";
    private const string ErrDeleteCheckStatus = "InvF_InventoryIn_SaveX_InvalidStatus";

    public class DeleteItemReq
    {
        public string? IF_InvInNo { get; set; }
    }

    public class DeleteReq
    {
        public bool? IsSysAdmin { get; set; }
        public List<string>? ListSysAccess { get; set; }
        public string? WaUserCode { get; set; }
        public string? NetworkId { get; set; }
        public string? OrgId { get; set; }
        /// <summary>Danh sách phiếu truyền trực tiếp dạng mảng đối tượng.</summary>
        public List<DeleteItemReq>? ListInvF_InventoryIn { get; set; }
        /// <summary>Tham số chuỗi JSON theo đúng gate nguồn `string objlistinvf_inventoryin`.</summary>
        public string? ObjListInvF_InventoryIn { get; set; }
    }

    public class DeleteResult
    {
        public bool Success { get; set; } = true;
        public string? RedirectUrl { get; set; }
        public List<string> Messages { get; set; } = new();
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }

    [HttpPost("api/invf_inventoryin/delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DeleteReq? req)
    {
        // 1. Phân quyền SysAccessCheckDeny (gate dòng 1795-1799)
        // BTN_INVF_INVENTORYIN_DELETE
        var hasAccess = SysAccessCheckDeny(req?.IsSysAdmin, req?.ListSysAccess, "BTN_INVF_INVENTORYIN_DELETE");
        if (!hasAccess)
        {
            return Ok(new DeleteResult
            {
                Success = false,
                RedirectUrl = "/AccessDeny/Index",
                Title = "Access denied",
                Messages = new List<string> { "Access denied" }
            });
        }

        // 2. Parse danh sách phiếu nhập kho từ JSON string hoặc list object
        List<DeleteItemReq>? items = req?.ListInvF_InventoryIn;
        if ((items == null || items.Count == 0) && !string.IsNullOrWhiteSpace(req?.ObjListInvF_InventoryIn))
        {
            try
            {
                items = System.Text.Json.JsonSerializer.Deserialize<List<DeleteItemReq>>(
                    req.ObjListInvF_InventoryIn,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                return PortResult.Fail(ErrDeleteDefault, "Exception", ex.Message);
            }
        }

        if (items == null)
        {
            return PortResult.Fail(ErrDeleteDefault, "Check.objlistinvf_inventoryin", null);
        }

        // Nếu danh sách rỗng, không gọi service, trả Success = true như gate 1836-1867
        if (items.Count == 0)
        {
            return Ok(new DeleteResult
            {
                Success = true,
                RedirectUrl = "/api/invf_inventoryin/index",
                Title = "Xoá phiếu nhập kho thành công!",
                Messages = new List<string> { "Xoá phiếu nhập kho thành công!" }
            });
        }

        var listIF_InvInNo = items
            .Select(m => StdParam(m.IF_InvInNo))
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .ToList();

        // 3. Nguồn gate dòng 1810-1835: Gọi WA_InvF_InventoryIn_Get lấy danh sách phiếu và các bảng chi tiết
        // Sau đó foreach từng objInvF_InventoryIn gọi WA_InvF_InventoryIn_Save với FlagIsDelete = "1"
        // Trong InvF_InventoryIn_SaveX_New20241112 (Form.cs:1673-1678):
        //   - Nếu phiếu không tồn tại trong DB: khi bIsDelete = true -> goto MyCodeLabel_Done (coi như thành công).
        //   - Nếu phiếu đã tồn tại trong DB:
        //       + IF_InvInStatus khác "PENDING" -> ném lỗi InvF_InventoryIn_SaveX_InvalidStatus
        //       + IF_InvInStatus == "PENDING" -> thực hiện xóa trong DB (bảng InvF_InventoryIn và các chi tiết Dtl, Lot, Serial, QR)
        foreach (var invNo in listIF_InvInNo)
        {
            var strIF_InvInNo = invNo!;
            var rec = await db.Set<InvF_InventoryIn>()
                .FirstOrDefaultAsync(x => x.IF_InvInNo == strIF_InvInNo);

            if (rec != null)
            {
                if (!StringEqualIgnoreCase(rec.IF_InvInStatus, StatusPending))
                {
                    return PortResult.Fail(ErrDeleteCheckStatus,
                        "Check.DB.IF_InvInStatus", rec.IF_InvInStatus,
                        "Check.IF_InvInStatus.Expected", StatusPending);
                }

                await using var tx = await db.Database.BeginTransactionAsync();
                try
                {
                    await db.Set<InvF_InventoryInDtl>().Where(x => x.IF_InvInNo == strIF_InvInNo).ExecuteDeleteAsync();
                    await db.Set<InvF_InventoryInInstSerial>().Where(x => x.IF_InvInNo == strIF_InvInNo).ExecuteDeleteAsync();
                    await db.Set<InvF_InventoryInInstLot>().Where(x => x.IF_InvInNo == strIF_InvInNo).ExecuteDeleteAsync();
                    await db.Set<InvF_InventoryInQR>().Where(x => x.IF_InvInNo == strIF_InvInNo).ExecuteDeleteAsync();
                    await db.Set<InvF_InventoryIn>().Where(x => x.IF_InvInNo == strIF_InvInNo).ExecuteDeleteAsync();

                    await tx.CommitAsync();
                }
                catch (Exception)
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }
        }

        return Ok(new DeleteResult
        {
            Success = true,
            RedirectUrl = "/api/invf_inventoryin/index",
            Title = "Xoá phiếu nhập kho thành công!",
            Messages = new List<string> { "Xoá phiếu nhập kho thành công!" }
        });
    }
}

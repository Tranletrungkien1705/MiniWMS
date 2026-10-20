using Microsoft.AspNetCore.Mvc;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 Mst_CustomerGroupController.cs — Mst_CustomerGroup
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\Mst_CustomerGroupController.cs:106-113
///   public ActionResult Create()
///   {
///       var userState = this.UserState;
///       Session["UserState"] = userState;
///       ViewBag.ViewType = "create";
///       var objMst_CustomerGroup = new Mst_CustomerGroup();
///       return JsonView("Mst_CustomerGroup_Info", objMst_CustomerGroup);
///   }
/// Action KHÔNG có tham số, KHÔNG gọi service/biz, KHÔNG đọc/ghi bảng, KHÔNG mã lỗi (excerpt mục 4: 0 chỗ ném lỗi).
/// Đây là action MỞ FORM: trả một đối tượng Mst_CustomerGroup RỖNG (mọi cột null, AutoId = 0) — port đúng nguồn,
/// KHÔNG tự chế logic ghi. Xem .parity-run\conflicts\301_conflict.md.
/// </summary>
[ApiController]
[Route("api/mst_customergroup")]
public class Mst_CustomerGroupControllerPort : ControllerBase
{
    /// <summary>Mở form tạo nhóm khách hàng — trả model rỗng như `new Mst_CustomerGroup()` (nguồn dòng 111).</summary>
    [HttpPost("create")]
    public ActionResult<Mst_CustomerGroup> Create()
    {
        // Nguồn: ViewBag.ViewType = "create" (dòng 110) + new Mst_CustomerGroup() (dòng 111).
        // Session["UserState"] (dòng 108-109) là trạng thái phiên của Website nguồn — đích không có phiên, bỏ qua (GAP).
        return Ok(new Mst_CustomerGroup());
    }
}

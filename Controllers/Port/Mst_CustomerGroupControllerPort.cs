using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 Mst_CustomerGroupController.cs — Mst_CustomerGroup (action Create, Edit)
/// Nguồn Create: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\Mst_CustomerGroupController.cs:106-113
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
/// Nguồn Edit: cùng file :253-344 (xem chú thích ở action Edit).
/// </summary>
[ApiController]
[Route("api/mst_customergroup")]
public class Mst_CustomerGroupControllerPort(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Body của Edit — nguồn nhận `string model` rồi `JsonConvert.DeserializeObject&lt;Mst_CustomerGroup&gt;(model)`
    /// (Mst_CustomerGroupController.cs:261), tức body chính LÀ đối tượng Mst_CustomerGroup (bài học A.21).
    /// </summary>
    public class EditReq
    {
        public string? CustomerGrpCode { get; set; }
        public string? CustomerGrpName { get; set; }
        public string? CustomerGrpDesc { get; set; }
        public string? FlagActive { get; set; }
        /// <summary>GAP: nguồn lấy `UserState.Mst_NNT.OrgID` (dòng 299) — đích không có phiên nên nhận trong body.</summary>
        public string? OrgID { get; set; }
    }

    /// <summary>
    /// port 1:1 Mst_CustomerGroupController.cs:253-344 — Edit(string model).
    /// Luồng: rỗng → Success "Mã nhóm khách hàng trống!"; Get (where CustomerGrpCode = X, lấy dòng đầu)
    ///        → không có → Success "Mã nhóm khách hàng 'X' không có trong hệ thống!";
    ///        có → ghi đè Name/Desc/FlagActive/OrgID lên dòng DB → Update (Ft_Cols_Upd 3 cột) → Save (ghi đè lại).
    /// GAP: WA_Mst_CustomerGroup_Update KHÔNG có trong nguồn (excerpt mục 2: "khong thay WA_Mst_CustomerGroup_Update") —
    ///      xem .parity-run\conflicts\303_conflict.md.
    /// </summary>
    [HttpPost("edit")]
    public async Task<IActionResult> Edit([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] EditReq? req)
    {
        // Nguồn 263: `model != null && !CUtils.IsNullOrEmpty(objMst_CustomerGroup.CustomerGrpCode)` — sai → else (dòng 337-341).
        if (req == null || string.IsNullOrEmpty(req.CustomerGrpCode))
            return Ok(new { Success = true, Messages = new[] { "Mã nhóm khách hàng trống!" }, RedirectUrl = (string?)null });

        // Nguồn 266-270 + strWhereClause 586-589: where Mst_CustomerGroup.CustomerGrpCode = X ("=", không wildcard).
        // GetX 12174-12187: select distinct OrgID, CustomerGrpCode ... order by MyIdxSeq asc → lấy dòng đầu (dòng 296).
        var code = req.CustomerGrpCode.Trim();
        var cur = await db.Set<Mst_CustomerGroup>()
            .Where(x => EF.Functions.Like((x.CustomerGrpCode ?? "").ToLower(), PortQuery.Equal(code), PortQuery.Esc))
            .OrderBy(x => x.AutoId)
            .FirstOrDefaultAsync();

        // Nguồn 331-335: không có dòng → Success=true, "Mã nhóm khách hàng '<CustomerGrpName>' không có trong hệ thống!"
        // (nguồn in CustomerGrpName, KHÔNG phải mã — LỖI NGUỒN GIỮ NGUYÊN, dòng 334).
        if (cur == null)
            return Ok(new { Success = true, Messages = new[] { $"Mã nhóm khách hàng '{req.CustomerGrpName}' không có trong hệ thống!" }, RedirectUrl = (string?)null });

        // Nguồn 296-299: ghi đè 3 cột từ client + OrgID = UserState.Mst_NNT.OrgID (GAP: nhận từ body).
        cur.CustomerGrpName = req.CustomerGrpName;
        cur.CustomerGrpDesc = req.CustomerGrpDesc;
        cur.FlagActive = req.FlagActive;
        cur.OrgID = req.OrgID;

        // Nguồn 301-305: Ft_Cols_Upd = "Mst_CustomerGroup.CustomerGrpName,Mst_CustomerGroup.CustomerGrpDesc,Mst_CustomerGroup.FlagActive,"
        // → UpdateX chỉ ghi 3 cột này (OrgID KHÔNG nằm trong danh sách).
        // Nguồn 320-325: Save ghi đè lại Lst_Mst_CustomerGroup bằng dòng DB (đã sửa) + FlagIsDelete = "0"
        // → SaveX xoá theo CustomerGrpCode rồi insert lại đúng các cột của dòng đó (Customer.cs:13429-13515).
        // GAP: WA_Mst_CustomerGroup_Update không có trong nguồn (excerpt mục 2: "khong thay WA_Mst_CustomerGroup_Update")
        //      → KHÔNG port được bước Update; xem .parity-run\conflicts\303_conflict.md.
        // GAP: SaveX ghi LogLUDTimeUTC/LogLUBy từ dòng DB đọc lại (Customer.cs:13467-13468, 13478-13479), KHÔNG lấy
        //      từ WAUserCode của request → không tự đặt LogLUBy = WAUserCode (không có trong nguồn).
        // GAP: SaveX xoá Mst_CustomerGroupImages theo CustomerGrpCode rồi insert lại từ #input_Mst_CustomerGroupImages
        //      (Customer.cs:13431-13447, 13485-13515); gate Edit KHÔNG nạp Lst_Mst_CustomerGroupImages (dòng 291) nên
        //      bảng tạm rỗng → nguồn XOÁ ảnh của nhóm. Chưa port vì cần chốt cùng xung đột Update (xem conflict).
        await db.SaveChangesAsync();

        // Nguồn 326-329: Success=true, "Sửa nhóm khách hàng thành công!", RedirectUrl = Url.Action("Index") (đích không có trang Index → null).
        return Ok(new { Success = true, Messages = new[] { "Sửa nhóm khách hàng thành công!" }, RedirectUrl = (string?)null });
    }

    /// <summary>Mở form tạo nhóm khách hàng — trả model rỗng như `new Mst_CustomerGroup()` (nguồn dòng 111).</summary>
    [HttpPost("create")]
    public ActionResult<Mst_CustomerGroup> Create()
    {
        // Nguồn: ViewBag.ViewType = "create" (dòng 110) + new Mst_CustomerGroup() (dòng 111).
        // Session["UserState"] (dòng 108-109) là trạng thái phiên của Website nguồn — đích không có phiên, bỏ qua (GAP).
        return Ok(new Mst_CustomerGroup());
    }
}

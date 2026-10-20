using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 Mst_CustomerGroupController.cs — Mst_CustomerGroup (action Create, Edit, GetDetailData, Export)
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
public class Mst_CustomerGroupControllerPort(AppDbContext db, IWebHostEnvironment? env = null) : ControllerBase
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

    /// <summary>
    /// Body của GetDetailData — nguồn nhận `string customerGrpCode` (Mst_CustomerGroupController.cs:350).
    /// </summary>
    public class GetDetailDataReq
    {
        public string? CustomerGrpCode { get; set; }
    }

    /// <summary>
    /// port 1:1 Mst_CustomerGroupController.cs:350-402 — GetDetailData(string customerGrpCode).
    /// Luồng: dựng Mst_CustomerGroup{CustomerGrpCode} → strWhereClause (chỉ cột CustomerGrpCode, "=", dòng 586-589)
    ///        → WA_Mst_CustomerGroup_Get (Rt_Cols_Mst_CustomerGroup = "*", Ft_RecordStart/Ft_RecordCount của gate)
    ///        → biz Mst_CustomerGroup_Get → Mst_CustomerGroup_GetX (SQL: distinct OrgID+CustomerGrpCode, order MyIdxSeq asc,
    ///        phân trang theo MyIdxSeq, rồi select mo.* join lại) → lấy dòng ĐẦU (dòng 390-393).
    /// Không có dòng → trả chính đối tượng dựng ban đầu (chỉ có CustomerGrpCode, các cột khác null — dòng 362-365, 394).
    /// Không có mã lỗi nghiệp vụ: chỉ catch chung của gate (dòng 396-399) → PortExceptionFilter.
    /// GAP: nguồn lấy NetworkID/OrgID/UtcOffset từ UserState (dòng 376-378) — đích không có phiên; các tham số này chỉ dùng cho
    ///      Sys_User_CheckAuthorize (dòng 11389-11402), KHÔNG tham gia SQL lọc dữ liệu → bỏ qua.
    /// GAP: Ft_RecordStart/Ft_RecordCount của gate (dòng 380-381) là tham số phân trang của BaseController, không nằm trong
    ///      chữ ký action → đích dùng mặc định 0/0 như giá trị khởi tạo của nguồn (xem .parity-run\conflicts\304_conflict.md).
    /// </summary>
    [HttpPost("getdetaildata")]
    public async Task<IActionResult> GetDetailData([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GetDetailDataReq? req)
    {
        // Nguồn 362-365: objMst_CustomerGroup = new Mst_CustomerGroup() { CustomerGrpCode = customerGrpCode }.
        var objMst_CustomerGroup = new Mst_CustomerGroup { CustomerGrpCode = req?.CustomerGrpCode };

        // Nguồn 366 + 586-589: chỉ thêm điều kiện khi CustomerGrpCode không rỗng, toán tử "=" (không wildcard).
        // Rỗng → where (1=1) → GetX trả dòng đầu tiên của bảng (order MyIdxSeq asc).
        IQueryable<Mst_CustomerGroup> q = db.Set<Mst_CustomerGroup>();
        if (!string.IsNullOrEmpty(objMst_CustomerGroup.CustomerGrpCode))
        {
            var code = objMst_CustomerGroup.CustomerGrpCode;
            q = q.Where(x => EF.Functions.Like((x.CustomerGrpCode ?? "").ToLower(), PortQuery.Equal(code), PortQuery.Esc));
        }

        // Nguồn 12174-12187: select distinct OrgID, CustomerGrpCode ... order by MyIdxSeq asc (MyIdxSeq = thứ tự dòng nguồn).
        // Nguồn 12194-12231: phân trang theo MyIdxSeq rồi inner join Mst_CustomerGroup lấy mo.* theo đúng thứ tự đó.
        // Đích: sắp theo AutoId (khoá tự tăng = thứ tự chèn) rồi lấy dòng đầu — tương đương MyIdxSeq asc.
        var first = await q.OrderBy(x => x.AutoId).FirstOrDefaultAsync();

        // Nguồn 390-393: chỉ thay objMst_CustomerGroup khi danh sách có dòng; 394: JsonView(model).
        if (first != null) objMst_CustomerGroup = first;
        return Ok(objMst_CustomerGroup);
    }

    /// <summary>
    /// Body của Export — nguồn nhận 2 tham số chuỗi `customerGrpName, customerGrpDesc`
    /// (Mst_CustomerGroupController.cs:408); đích nhận qua JSON body (bài học A.14).
    /// </summary>
    public class ExportReq
    {
        public string? CustomerGrpName { get; set; }
        public string? CustomerGrpDesc { get; set; }
    }

    /// <summary>
    /// Cột Excel của Export — chép nguyên văn `GetImportDicColums()` của gate
    /// (Mst_CustomerGroupController.cs:569-576): khoá = tên thuộc tính, giá trị = nhãn cột.
    /// </summary>
    private static readonly Dictionary<string, string> ExportDicColums = new()
    {
        { "CustomerGrpName", "Mã nhóm khách hàng" },
        { "CustomerGrpDesc", "Tên nhóm khách hàng" },
    };

    /// <summary>
    /// port 1:1 Mst_CustomerGroupController.cs:408-473 — Export(string customerGrpName, string customerGrpDesc).
    /// Luồng: dựng Mst_CustomerGroup{Name, Desc} → strWhereClause (chỉ 2 cột Name/Desc, `like '%x%'`, dòng 590-597)
    ///        → WA_Mst_CustomerGroup_Get (Rt_Cols_Mst_CustomerGroup = "*", Ft_RecordStart/Ft_RecordCount của gate)
    ///        → biz Mst_CustomerGroup_Get → Mst_CustomerGroup_GetX (SQL: distinct OrgID+CustomerGrpCode, order MyIdxSeq asc,
    ///        phân trang theo MyIdxSeq, rồi select mo.* join lại) → có dòng: ghi Excel + trả strUrl; không có: "Không có dữ liệu!".
    /// Không có mã lỗi nghiệp vụ: chỉ catch chung của gate (dòng 467-470) → PortExceptionFilter.
    /// GAP: nguồn lấy NetworkID/OrgID/UtcOffset từ UserState (dòng 435-437) — đích không có phiên; các tham số này chỉ dùng cho
    ///      Sys_User_CheckAuthorize (dòng 11389-11402), KHÔNG tham gia SQL lọc dữ liệu → bỏ qua.
    /// GAP: Ft_RecordStart/Ft_RecordCount của gate (dòng 439-440) là tham số phân trang của BaseController, không nằm trong
    ///      chữ ký action → đích dùng mặc định 0/0 như giá trị khởi tạo của nguồn (xem .parity-run\conflicts\304_conflict.md).
    /// GAP: nguồn trả `strUrl` = đường dẫn ảo `/TempFiles/...` (dòng 460); đích trả đường dẫn ảo tương đối như PortExcel sinh ra
    ///      (không ghép GetRequestBaseAddress vì nguồn KHÔNG ghép ở action này).
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ExportReq? req)
    {
        // Nguồn 419-423: objMst_CustomerGroup = new Mst_CustomerGroup() { CustomerGrpName, CustomerGrpDesc }.
        var objMst_CustomerGroup = new Mst_CustomerGroup
        {
            CustomerGrpName = req?.CustomerGrpName,
            CustomerGrpDesc = req?.CustomerGrpDesc,
        };

        // Nguồn 425 + 590-597: chỉ thêm điều kiện khi giá trị không rỗng; Name/Desc dùng `like '%<Trim>%'`.
        IQueryable<Mst_CustomerGroup> q = db.Set<Mst_CustomerGroup>();
        if (!string.IsNullOrEmpty(objMst_CustomerGroup.CustomerGrpName))
        {
            var name = objMst_CustomerGroup.CustomerGrpName;
            q = q.Where(x => EF.Functions.Like((x.CustomerGrpName ?? "").ToLower(), PortQuery.Contains(name), PortQuery.Esc));
        }
        if (!string.IsNullOrEmpty(objMst_CustomerGroup.CustomerGrpDesc))
        {
            var desc = objMst_CustomerGroup.CustomerGrpDesc;
            q = q.Where(x => EF.Functions.Like((x.CustomerGrpDesc ?? "").ToLower(), PortQuery.Contains(desc), PortQuery.Esc));
        }

        // Nguồn 12174-12187: select distinct OrgID, CustomerGrpCode ... order by MyIdxSeq asc (MyIdxSeq = thứ tự dòng nguồn).
        // Nguồn 12194-12231: phân trang theo MyIdxSeq rồi inner join Mst_CustomerGroup lấy mo.* theo đúng thứ tự đó.
        // Đích: sắp theo AutoId (khoá tự tăng = thứ tự chèn) — tương đương MyIdxSeq asc.
        var ordered = q.OrderBy(x => x.AutoId);

        // Nguồn 12153-12154: nFilterRecordStart = Ft_RecordStart, nFilterRecordEnd = Start + Count - 1 (phân trang theo MyIdxSeq).
        // Ft_RecordStart/Ft_RecordCount của gate mặc định 0/0 (xem GAP ở summary) → nFilterRecordEnd = -1 → 0 dòng.
        // Giữ đúng mẫu phân trang của các port hiện có (bài học A.12).
        const long recordStart = 0;
        const long recordCount = 0;
        var list = (recordCount <= 0 || recordStart < 0)
            ? new List<Mst_CustomerGroup>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                           .Take((int)Math.Min(recordCount, int.MaxValue))
                           .ToListAsync();

        // Nguồn 449-465: có dòng → xuất Excel + trả strUrl; không có → "Không có dữ liệu!".
        if (list.Count == 0)
            return Ok(new { Success = true, Title = "Không có dữ liệu!", CheckSuccess = "1" });

        // Nguồn 453-458: GetImportDicColums() + GenExcelExportFilePath(typeof(Mst_CustomerGroup).Name) + ExportToExcelFromList.
        // Phần ghi file bọc try/catch riêng (bài học A.2).
        try
        {
            var webRoot = env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (filePath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, nameof(Mst_CustomerGroup));
            PortExcel.ExportToExcelFromList(list, ExportDicColums, filePath, nameof(Mst_CustomerGroup));

            // Nguồn 460: Success = true, Title = "Đã xuất excel thành công!", CheckSuccess = "1", strUrl = url.
            return Ok(new { Success = true, Title = "Đã xuất excel thành công!", CheckSuccess = "1", strUrl = virtualPath });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Mst_CustomerGroup_Export", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Body của Delete — nguồn nhận `string customerGrpCode = ""` (Mst_CustomerGroupController.cs:480).
    /// </summary>
    public class DeleteReq
    {
        public string? CustomerGrpCode { get; set; }
    }

    /// <summary>
    /// port 1:1 Mst_CustomerGroupController.cs:480-565 — Delete(string customerGrpCode = "").
    /// Luồng: rỗng → Success "Mã nhóm khách hàng trống!"; Get (where CustomerGrpCode = X, lấy dòng đầu)
    ///        → không có → Success "Mã nhóm khách hàng 'X' không có trong hệ thống!";
    ///        có → WA_Mst_CustomerGroup_Delete (dịch vụ Product Center, dòng 532) → Save với FlagIsDelete = "1" (dòng 537-539)
    ///        → Success "Xóa nhóm khách hàng 'X' thành công!".
    /// GAP: bước WA_Mst_CustomerGroup_Delete gọi DỊCH VỤ NGOÀI (AddressAPIs_Product_Customer_Centrer, dòng 532) — đích không có,
    ///      bỏ qua và ghi GAP (luật §1).
    /// GAP: WA_Mst_CustomerGroup_Save (dòng 539) — excerpt mục 2 KHÔNG có API `WA_Mst_CustomerGroup_Save` của tầng Biz.Web
    ///      (chỉ có WAS_/Mst_CustomerGroup_SaveX ở biz); xem .parity-run\conflicts\306_conflict.md.
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DeleteReq? req)
    {
        // Nguồn 491: `!CUtils.IsNullOrEmpty(customerGrpCode)` — rỗng/null → else (dòng 552-556).
        if (string.IsNullOrEmpty(req?.CustomerGrpCode))
            return Ok(new { Success = false, Messages = new[] { "Mã nhóm khách hàng trống!" }, RedirectUrl = (string?)null });

        // Nguồn 494-498 + strWhereClause 586-589: where Mst_CustomerGroup.CustomerGrpCode = X ("=", không wildcard).
        // GetX 12174-12187: select distinct OrgID, CustomerGrpCode ... order by MyIdxSeq asc → lấy dòng đầu (dòng 528).
        var code = req.CustomerGrpCode;
        var cur = await db.Set<Mst_CustomerGroup>()
            .Where(x => EF.Functions.Like((x.CustomerGrpCode ?? "").ToLower(), PortQuery.Equal(code), PortQuery.Esc))
            .OrderBy(x => x.AutoId)
            .FirstOrDefaultAsync();

        // Nguồn 546-550: không có dòng → Success giữ false, "Mã nhóm khách hàng '<customerGrpCode>' không có trong hệ thống!".
        if (cur == null)
            return Ok(new { Success = false, Messages = new[] { $"Mã nhóm khách hàng '{code}' không có trong hệ thống!" }, RedirectUrl = (string?)null });

        // Nguồn 532: WA_Mst_CustomerGroup_Delete(objRQ, UserState.AddressAPIs_Product_Customer_Centrer) — dịch vụ NGOÀI, bỏ qua (GAP).
        // Nguồn 536-539: Lst_Mst_CustomerGroup = [dòng DB] + FlagIsDelete = "1" → WA_Mst_CustomerGroup_Save.
        //   SaveX (Customer.cs:13316, 13429-13447) với FlagIsDelete = "1": xoá Mst_CustomerGroupImages + Mst_CustomerGroup
        //   theo CustomerGrpCode, KHÔNG insert lại (bIsDelete = true).
        // GAP: tầng API `WA_Mst_CustomerGroup_Save` không có trong excerpt (mục 2) → chưa port bước Save;
        //      xem .parity-run\conflicts\306_conflict.md.

        // Nguồn 540-544: Success = true, "Xóa nhóm khách hàng '<customerGrpCode>' thành công!", RedirectUrl = Url.Action("Index").
        return Ok(new { Success = true, Messages = new[] { $"Xóa nhóm khách hàng '{code}' thành công!" }, RedirectUrl = (string?)null });
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

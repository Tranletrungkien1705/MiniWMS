using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 Inv_CostPriceHistController.cs — Inv_CostPriceHist (action Index, Inv_CostPriceHist_Calc).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\Inv_CostPriceHistController.cs:24-97
///   → Mst_InventoryService.Instance.WA_Mst_Inventory_GetInvCodeLeaf (dòng 75)
///   → API MstInventoryController.cs:465-536 → biz MasterData.cs:11805-11902 (WAS_Mst_Inventory_GetInvCodeLeaf)
///   → Mst_Inventory_GetInvCodeLeaf (MasterData.cs:11846-11863).
///
/// Luồng nguồn (dòng 24-97):
///   1. SysAccessCheckDeny(UserState, "MNU_INV_COSTPRICEHIST") — false ⇒ RedirectToAction("Index", "AccessDeny") (dòng 26-30).
///   2. Đọc UserState (UserCode/UserPassword/NetworkID/OrgID/AddressAPIs) (dòng 32-37) — GAP: đích không có phiên.
///   3. Nếu `RT_Mst_Sys_Config.Lst_Mst_Sys_Config` có dòng (dòng 42):
///        - lọc các dòng có `SysConfigID` CHỨA "CALCUPINVBYINVCODE" (dòng 44);
///        - có dòng ⇒ lấy dòng ĐẦU, `FlagActive` == FlagActive ("1") ⇒ gọi WA_Mst_Inventory_GetInvCodeLeaf
///          với where `Mst_Inventory.FlagActive='1'` (dòng 49-80);
///          ngược lại ⇒ danh sách kho = 1 dòng cố định { InvCode = "I", InvName = "I" } (dòng 84-90);
///        - KHÔNG có dòng cấu hình ⇒ danh sách kho RỖNG (không vào nhánh nào).
///   4. ViewBag.ListMst_Inventory = listMst_Inventory (dòng 94); return View(new List&lt;Inv_CostPriceHist&gt;()) (dòng 96).
///
/// GAP: `RT_Mst_Sys_Config` của nguồn là dữ liệu PHIÊN (UserState) — đích không có phiên nên nhận danh sách cấu hình
///      qua body (SysConfigID/FlagActive), KHÔNG tự đọc bảng Mst_Sys_Config thay thế (nguồn không đọc bảng ở action này).
/// GAP: `SysAccessCheckDeny` cần UserState.IsSysAdmin + ListSysAccess — đích không có phiên; nhận `IsSysAdmin`/`ListSysAccess`
///      tuỳ chọn trong body, thiếu ⇒ coi như KHÔNG có quyền (nguồn trả false khi userState null, BaseController.cs:3245-3269).
/// GAP: `Mst_Inventory_GetInvCodeLeaf` (thân hàm SQL) KHÔNG có trong excerpt — chỉ biết where `Mst_Inventory.FlagActive='1'`
///      do gate dựng (dòng 69) và biz trả bảng "Mst_Inventory" (MasterData.cs:11869-11871). Đích lọc đúng điều kiện đó trên
///      bảng Mst_Inventory nội bộ; phần "leaf" (kho lá) của SQL nguồn chưa xác minh được ⇒ ghi GAP, không tự thêm điều kiện.
/// </summary>
[ApiController]
[Route("api/inv_costpricehist")]
public class Inv_CostPriceHistControllerPort(AppDbContext db, IWebHostEnvironment? env = null) : ControllerBase
{
    /// <summary>
    /// port 1:1 Inv_CostPriceHistController.cs:408-418 — ExportTemplate().
    /// Nguồn KHÔNG có tham số action, KHÔNG gọi service/biz, KHÔNG đọc/ghi bảng, KHÔNG mã lỗi (excerpt mục 4: 0 chỗ ném lỗi).
    /// Luồng: list = new List&lt;Inv_CostPriceHist&gt;() (dòng 410) → dicColNames = GetImportDicColumsTemplate() (dòng 412, 4 cột)
    ///        → GenExcelExportFilePath(typeof(Inv_CostPriceHist).Name, ref url) (dòng 414, BaseController.cs:750-768)
    ///        → ExcelExport.ExportToExcelFromList(list, dicColNames, filePath, "Mst_Product") (dòng 415 — tên sheet nguồn
    ///          viết cứng "Mst_Product", KHÔNG phải tên bảng Inv_CostPriceHist; giữ nguyên)
    ///        → Json(new { Success = true, Title = "Đã xuất excel thành công!", CheckSuccess = "1", strUrl = url }) (dòng 417).
    /// GAP: `ExcelExport` của nguồn nằm trong DLL không có mã ⇒ định dạng ô (độ rộng, kiểu số/ngày) dùng PortExcel
    ///      (cùng giả định đã ghi ở Data/PortExcel.cs:12-13).
    /// </summary>
    [HttpPost("exporttemplate")]
    public IActionResult ExportTemplate([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] object? req = null)
    {
        // Nguồn 410: `var list = new List<Inv_CostPriceHist>();` — danh sách RỖNG (file mẫu chỉ có dòng tiêu đề).
        var list = new List<Inv_CostPriceHist>();

        // Nguồn 412 + 818-827: GetImportDicColumsTemplate() — 4 cột, ĐÚNG thứ tự khai báo của từ điển nguồn.
        var dicColNames = new Dictionary<string, string>
        {
            { "EffectDTimeUTC", "Thời gian hiệu lực(*)" },
            { "ProductCodeUser", "Mã hàng hóa(*)" },
            { "UPInv", "Giá vốn(*)" },
            { "InvCode", "Mã kho" },
        };

        // Nguồn 414: GenExcelExportFilePath(string.Format(typeof(Inv_CostPriceHist).Name), ref url).
        // Nguồn 415: ExcelExport.ExportToExcelFromList(list, dicColNames, filePath, string.Format("Mst_Product")).
        // Phần ghi file bọc try/catch riêng (bài học A.2).
        try
        {
            var webRoot = env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var (filePath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, nameof(Inv_CostPriceHist));
            PortExcel.ExportToExcelFromList(list, dicColNames, filePath, "Mst_Product");

            // Nguồn 417: Json(new { Success = true, Title = "Đã xuất excel thành công!", CheckSuccess = "1", strUrl = url }).
            return Ok(new { Success = true, Title = "Đã xuất excel thành công!", CheckSuccess = "1", strUrl = virtualPath });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("Inv_CostPriceHist_ExportTemplate", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// Body của ExportExcel. Nguồn (Inv_CostPriceHistController.cs:721) nhận 4 tham số chuỗi
    /// `productcodeuser, productname, invcode, flagiscurrent`; các trường còn lại là dữ liệu nguồn lấy từ phiên
    /// (UserState) — thiếu thì để null, KHÔNG tự đặt mặc định.
    /// </summary>
    public class ExportExcelReq : ISysAccessState
    {
        /// <summary>Nguồn dòng 750 — `CUtils.StrValue(productcodeuser)`.</summary>
        public string? ProductCodeUser { get; set; }
        /// <summary>Nguồn dòng 751 — `CUtils.StrValue(productname)`.</summary>
        public string? ProductName { get; set; }
        /// <summary>Nguồn dòng 753 — `CUtils.StrValue(invcode)`.</summary>
        public string? InvCode { get; set; }
        /// <summary>Nguồn dòng 752 — `CUtils.StrValue(flagiscurrent)`.</summary>
        public string? FlagIsCurrent { get; set; }
        /// <summary>Nguồn dòng 745 — `CUtils.StrValue(UserState.Mst_NNT.OrgID)` (dùng cho Sys_User_CheckAuthorize).</summary>
        public string? OrgID { get; set; }
        /// <summary>Nguồn dòng 744 — `CUtils.StrValue(UserState.Mst_NNT.NetworkID)`.</summary>
        public string? NetworkID { get; set; }
        /// <summary>Nguồn dòng 742 — `CUtils.StrValue(UserState.SysUser.UserCode)`.</summary>
        public string? WAUserCode { get; set; }
        /// <summary>Nguồn dòng 741 — `CUtils.StrValue(UserState.AccessToken)`.</summary>
        public string? AccessToken { get; set; }
        /// <summary>Nguồn dòng 723 — `SysAccessCheckDeny(UserState, ...)` cần `userState.IsSysAdmin` (BaseController.cs:3250).</summary>
        public bool? IsSysAdmin { get; set; }
        /// <summary>Nguồn dòng 723 — `userState.ListSysAccess[].ObjectCode` (BaseController.cs:3258-3260).</summary>
        public List<string>? ListSysAccess { get; set; }
    }

    /// <summary>
    /// port 1:1 Inv_CostPriceHistController.cs:721-777 — ExportExcel(productcodeuser, productname, invcode, flagiscurrent).
    /// Chuỗi: gate → Inv_CostPriceHistService.Instance.WA_Inv_CostPriceHist_Get (dòng 755)
    ///        → API InvCostPriceHistController.cs:163-232 → biz Form.cs:200118-200203 (WAS_Inv_CostPriceHist_Get)
    ///        → Form.cs:200205-200351 (Inv_CostPriceHist_Get) → Form.cs:200498-200669 (Inv_CostPriceHist_GetX_New20240916).
    /// Luồng nguồn:
    ///   1. SysAccessCheckDeny(UserState, "MNU_RPT_INV_INVENTORYBALANCE_STORAGETIME") — false ⇒ RedirectToAction("Index", "AccessDeny") (dòng 723-727).
    ///   2. Dựng RQ_Inv_CostPriceHist (dòng 735-754): EffectDTimeUTCFrom/To KHÔNG được gate gán (null).
    ///   3. Gọi WA_Inv_CostPriceHist_Get (dòng 755).
    ///   4. Lst_Inv_CostPriceHist != null && Count > 0 (dòng 756) ⇒ xuất Excel 9 cột (GetExportDicColumsTemplate dòng 829-843)
    ///      rồi trả { Success = true, Title = "Đã xuất excel thành công!", CheckSuccess = "1", strUrl = url } (dòng 764).
    ///      Ngược lại ⇒ { Success = false, Title = "Không có dữ liệu xuất!", CheckSuccess = "0" } (dòng 767).
    ///   5. Ngoại lệ ⇒ { Success = false, Detail = resultEntry.Detail } (dòng 771-776).
    /// </summary>
    [HttpPost("exportexcel")]
    public async Task<IActionResult> ExportExcel(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ExportExcelReq? req)
    {
        // Nguồn 723-727: SysAccessCheckDeny(UserState, "MNU_RPT_INV_INVENTORYBALANCE_STORAGETIME") — false ⇒
        // RedirectToAction("Index", "AccessDeny"). BaseController.cs:3245-3269: userState null ⇒ false; IsSysAdmin ⇒ true;
        // ngược lại phải có ObjectCode khớp (InvariantCultureIgnoreCase).
        if (!SysAccessCheckDeny(req, "MNU_RPT_INV_INVENTORYBALANCE_STORAGETIME"))
            return Ok(new
            {
                Success = true,
                RedirectUrl = "/AccessDeny/Index",
                Data = (object?)null,
            });

        // Nguồn 735-754: dựng RQ_Inv_CostPriceHist. Nguồn 750-753: CUtils.StrValue(x) = chuỗi rỗng khi null
        // (KHÔNG Trim/ToUpper ở gate). EffectDTimeUTCFrom/To KHÔNG được gate gán (null) — dòng 160-161 biz.
        var productCodeUser = req?.ProductCodeUser ?? "";
        var productName = req?.ProductName ?? "";
        var flagIsCurrent = req?.FlagIsCurrent ?? "";
        var invCode = req?.InvCode ?? "";

        // Nguồn 755: Inv_CostPriceHistService.Instance.WA_Inv_CostPriceHist_Get(objRQ, addressAPIs).
        // GAP: Sys_User_CheckAuthorize / Sys_Access_CheckDenyV30 / Sys_User_GetAbilityViewOfUser / zzzzClauseSelect_Mst_NNT_ViewAbility_Get
        //      (Form.cs:200264-200284, 200554-200558) là tầng xác thực + phân quyền theo phiên — đích không có phiên
        //      ⇒ bỏ qua, KHÔNG tự chế (cùng cách #24, xem .parity-run\conflicts\24_conflict.md).
        var listInv_CostPriceHist = await GetInv_CostPriceHistAsync<ExportExcel_Inv_CostPriceHistItem>(productCodeUser, productName,
            flagIsCurrent, null, null, invCode);

        // Nguồn 756: `objRT != null && objRT.Lst_Inv_CostPriceHist != null && Count > 0`.
        if (listInv_CostPriceHist.Count > 0)
        {
            // Nguồn 759 + 829-843: GetExportDicColumsTemplate() — 9 cột, ĐÚNG thứ tự khai báo của từ điển nguồn.
            // 3 cột mp_* là cột join của Mst_Product (Form.cs:200622-200624) ⇒ lấy từ ExportExcel_Inv_CostPriceHistItem.
            var dicColNames = new Dictionary<string, string>
            {
                { "EffectDTimeUTC", "Thời gian hiệu lực" },
                { "mp_ProductCodeUser", "Mã hàng hóa" },
                { "mp_ProductName", "Tên hàng hóa" },
                { "mp_UnitCode", "Đơn vị tính" },
                { "UPInv", "Giá vốn" },
                { "FormNo", "Số phiếu nhập" },
                { "UpdBy", "Người cập nhật" },
                { "UpdDTimeUTC", "Ngày cập nhật" },
                { "InvCode", "Mã kho" },
            };

            // Nguồn 760-762: GenExcelExportFilePath(typeof(Inv_CostPriceHist).Name, ref url) +
            // ExcelExport.ExportToExcelFromList(list, dicColNames, filePath, "Inv_CostPriceHist").
            // Phần ghi file bọc try/catch riêng (bài học A.2).
            try
            {
                var webRoot = env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var (filePath, virtualPath) = PortExcel.GenExcelExportFilePath(webRoot, nameof(Inv_CostPriceHist));
                PortExcel.ExportToExcelFromList(listInv_CostPriceHist, dicColNames, filePath, "Inv_CostPriceHist");

                // Nguồn 764: Json(new { Success = true, Title = "Đã xuất excel thành công!", CheckSuccess = "1", strUrl = url }).
                return Ok(new { Success = true, Title = "Đã xuất excel thành công!", CheckSuccess = "1", strUrl = virtualPath });
            }
            catch (Exception ex)
            {
                // Nguồn 771-776: catch ⇒ resultEntry.SetFailed().AddException; Json(new { Success = false, Detail = resultEntry.Detail }).
                return Ok(new { Success = false, Detail = ex.Message });
            }
        }

        // Nguồn 765-768: không có dữ liệu ⇒ Json(new { Success = false, Title = "Không có dữ liệu xuất!", CheckSuccess = "0" }).
        return Ok(new { Success = false, Title = "Không có dữ liệu xuất!", CheckSuccess = "0" });
    }

    /// <summary>
    /// Body của Index. Nguồn KHÔNG có tham số action; các trường dưới đây là dữ liệu nguồn lấy từ phiên
    /// (UserState / RT_Mst_Sys_Config) — thiếu thì để null, KHÔNG tự đặt mặc định.
    /// </summary>
    public class IndexReq : ISysAccessState
    {
        /// <summary>Nguồn: `userState.IsSysAdmin` (BaseController.cs:3250).</summary>
        public bool? IsSysAdmin { get; set; }
        /// <summary>Nguồn: `userState.ListSysAccess[].ObjectCode` (BaseController.cs:3258-3260).</summary>
        public List<string>? ListSysAccess { get; set; }
        /// <summary>Nguồn: `userState.RT_Mst_Sys_Config.Lst_Mst_Sys_Config` (Inv_CostPriceHistController.cs:42-44).</summary>
        public List<IndexSysConfig>? Lst_Mst_Sys_Config { get; set; }
    }

    /// <summary>Một dòng `Mst_Sys_Config` trong phiên (chỉ 2 cột action dùng: SysConfigID, FlagActive).</summary>
    public class IndexSysConfig
    {
        public string? SysConfigID { get; set; }
        public string? FlagActive { get; set; }
    }

    /// <summary>
    /// port 1:1 Inv_CostPriceHistController.cs:24-97 — Index().
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] IndexReq? req)
    {
        // Nguồn 26-30: SysAccessCheckDeny(UserState, "MNU_INV_COSTPRICEHIST") — false ⇒ RedirectToAction("Index", "AccessDeny").
        // BaseController.cs:3245-3269: userState null ⇒ false; IsSysAdmin ⇒ true; ngược lại phải có ObjectCode khớp
        // (StringComparison.InvariantCultureIgnoreCase) với "MNU_INV_COSTPRICEHIST".
        if (!SysAccessCheckDeny(req, "MNU_INV_COSTPRICEHIST"))
            return Ok(new
            {
                Success = true,
                RedirectUrl = "/AccessDeny/Index",
                ListMst_Inventory = (object?)null,
                Data = (object?)null,
            });

        // Nguồn 41: `var listMst_Inventory = new List<Mst_Inventory>();`
        var listMst_Inventory = new List<Mst_Inventory>();

        // Nguồn 42: `userState.RT_Mst_Sys_Config.Lst_Mst_Sys_Config != null && Count > 0`.
        var lstSysConfig = req?.Lst_Mst_Sys_Config;
        if (lstSysConfig != null && lstSysConfig.Count > 0)
        {
            // Nguồn 44: Where(it => CUtils.StrValue(it.SysConfigID).Contains("CALCUPINVBYINVCODE")) — CHỨA, phân biệt hoa/thường.
            var listMst_Sys_Config_TinhGiaVon = lstSysConfig
                .Where(it => (it.SysConfigID ?? "").Contains("CALCUPINVBYINVCODE"))
                .ToList();

            // Nguồn 46: có dòng cấu hình tính giá vốn.
            if (listMst_Sys_Config_TinhGiaVon.Count > 0)
            {
                // Nguồn 49-50: strFlagActive = CUtils.StrValue(dòng đầu .FlagActive); so `Equals(FlagActive)` ("1").
                var strFlagActive = listMst_Sys_Config_TinhGiaVon[0].FlagActive ?? "";
                if (strFlagActive.Equals("1"))
                {
                    // Nguồn 52-80: theo từng kho → WA_Mst_Inventory_GetInvCodeLeaf với
                    // Ft_WhereClause = "Mst_Inventory.FlagActive='1'" (dòng 69), Rt_Cols_Mst_Inventory = "*" (dòng 72).
                    // Biz MasterData.cs:11846-11871: trả bảng "Mst_Inventory" → Lst_Mst_Inventory.
                    // GAP: thân SQL Mst_Inventory_GetInvCodeLeaf không có trong excerpt (chỉ biết điều kiện FlagActive='1').
                    var lst = await db.Set<Mst_Inventory>()
                        .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                        .ToListAsync();

                    // Nguồn 76-79: chỉ gán khi Lst_Mst_Inventory != null && Count > 0; rỗng ⇒ giữ danh sách rỗng.
                    if (lst.Count > 0)
                        listMst_Inventory = lst;
                }
                else
                {
                    // Nguồn 84-90: không theo kho ⇒ kho mặc định (I).
                    listMst_Inventory.Add(new Mst_Inventory
                    {
                        InvCode = "I",
                        InvName = "I",
                    });
                }
            }
        }

        // Nguồn 94-96: ViewBag.ListMst_Inventory = listMst_Inventory; return View(new List<Inv_CostPriceHist>()).
        return Ok(new
        {
            Success = true,
            RedirectUrl = (string?)null,
            ListMst_Inventory = listMst_Inventory,
            Data = new List<Inv_CostPriceHist>(),
        });
    }

    /// <summary>
    /// Body của LoadData. Nguồn (Inv_CostPriceHistController.cs:321) nhận 1 tham số `productcode`.
    /// </summary>
    public class LoadDataReq
    {
        /// <summary>Nguồn dòng 321 — `string productcode` (dùng nguyên văn, KHÔNG chuẩn hoá ở gate).</summary>
        public string? ProductCode { get; set; }
    }

    /// <summary>
    /// port 1:1 Inv_CostPriceHistController.cs:321-366 — LoadData(productcode).
    /// Chuỗi: gate → GetPrdBase (dòng 335/343) → Mst_ProductService.Instance.WA_Mst_Product_Get (dòng 401)
    ///        → API MstProductController.cs:20-88 → biz ProductCenter.MasterData.cs:5714-5872 (WAS_Mst_Product_Get)
    ///        → Mst_Product_Get (MasterData.cs:5769).
    /// </summary>
    [HttpPost("loaddata")]
    public async Task<IActionResult> LoadData(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LoadDataReq? req)
    {
        // Nguồn 326: `if (!CUtils.IsNullOrEmpty(productcode))` — rỗng/null ⇒ rơi xuống JsonViewError("Create", ...)
        // với resultEntry.Success = false (dòng 323, 365).
        var productcode = req?.ProductCode;
        if (!string.IsNullOrEmpty(productcode))
        {
            // Nguồn 329-335: Mst_Product { ProductCode = productcode, ProductLevelSys = "L2PRD" } →
            // strWhereClause_Mst_Product_Get_Base (dòng 859-876): ProductCode = x AND ProductLevelSys != "L2PRD".
            var lstPrdBase = await GetPrdBaseAsync(productcode, level2: false);

            // Nguồn 336: `objRT_Mst_ProductBase.Lst_Mst_Product[0].ProductCodeRoot` — ném nếu danh sách rỗng
            // (giữ nguyên hành vi nguồn: ngoại lệ → PortExceptionFilter trả 400).
            // LỖI NGUỒN GIỮ NGUYÊN (Inv_CostPriceHistController.cs:336-343): biến `_productcodeRoot` được gán nhưng
            // KHÔNG dùng — where clause Level2 (dòng 885) vẫn lọc `ProductCodeRoot = productcode` (tham số gốc).
            var _productcodeRoot = lstPrdBase[0].ProductCodeRoot;

            // Nguồn 337-343: strWhereClause_Mst_Product_Get_Level2 (dòng 877-894):
            // ProductCodeRoot = productcode AND ProductLevelSys = "L2PRD".
            var lstPrdLevel2 = await GetPrdBaseAsync(productcode, level2: true);

            // Nguồn 344-351: Mst_ProductUI { LstPrdBase, LstPrdLeve2 }; LstPrdLeve2.AddRange(LstPrdBase).
            var productbase = new LoadData_ProductUI
            {
                LstPrdBase = lstPrdBase,
                LstPrdLeve2 = lstPrdLevel2,
            };
            productbase.LstPrdLeve2.AddRange(lstPrdBase);
            var listproductbase = new List<LoadData_ProductUI> { productbase };

            // Nguồn 354-357: `if (listproductbase != null) return Json(new { Success = true, data = listproductbase });`
            // (luôn khác null ⇒ luôn vào nhánh này).
            return Ok(new
            {
                Success = true,
                data = listproductbase,
            });
        }

        // Nguồn 360-365: catch → SetFailed().AddException; JsonViewError("Create", null, resultEntry) với Success = false.
        // Đích: PortExceptionFilter đã bắt ngoại lệ; nhánh này chỉ còn trường hợp productcode rỗng.
        // GAP: `Html` của nguồn là kết quả RenderRazorViewToString("Create") (BaseController.cs:649-691) — đích không có
        //      Razor view ⇒ null; `Title`/`Detail` là Messages/Detail của JsonResultEntry (rỗng khi không có lỗi).
        return Ok(new
        {
            Success = false,
            Html = (string?)null,
            Title = (object?)null,
            Detail = (object?)null,
        });
    }

    /// <summary>
    /// port 1:1 gate GetPrdBase (Inv_CostPriceHistController.cs:368-403) + 2 hàm strWhereClause (dòng 859-894)
    /// → biz WAS_Mst_Product_Get (ProductCenter.MasterData.cs:5714-5872) → Mst_Product_Get (dòng 5769).
    /// `level2 = false` ⇒ where `ProductCode = x AND ProductLevelSys != "L2PRD"` (dòng 867/871);
    /// `level2 = true`  ⇒ where `ProductCodeRoot = x AND ProductLevelSys = "L2PRD"` (dòng 885/889).
    /// GAP: thân SQL `Mst_Product_Get` KHÔNG có trong excerpt — chỉ biết where clause do gate dựng và bảng trả về
    ///      "Mst_Product" (MasterData.cs:5804-5806); đích lọc đúng điều kiện đó trên bảng Mst_Product nội bộ.
    /// GAP: `Rt_Cols_Prd_Attribute = "*"` (dòng 394) — bảng Prd_Attribute chưa có ở đích, gate không dùng kết quả
    ///      (dòng 348-349 bị comment) ⇒ bỏ qua.
    /// </summary>
    private async Task<List<Mst_Product>> GetPrdBaseAsync(string productCode, bool level2)
    {
        // Nguồn 867/885: `ProductCode = @x` / `ProductCodeRoot = @x` — toán tử `=`, KHÔNG wildcard.
        var pCode = PortQuery.Equal(productCode);
        var q = db.Set<Mst_Product>().AsQueryable();
        if (level2)
        {
            q = q.Where(x => EF.Functions.Like((x.ProductCodeRoot ?? "").ToLower(), pCode, PortQuery.Esc));
            // Nguồn 889: `ProductLevelSys = 'L2PRD'`.
            q = q.Where(x => EF.Functions.Like((x.ProductLevelSys ?? "").ToLower(), PortQuery.Equal("L2PRD"), PortQuery.Esc));
        }
        else
        {
            q = q.Where(x => EF.Functions.Like((x.ProductCode ?? "").ToLower(), pCode, PortQuery.Esc));
            // Nguồn 871: `ProductLevelSys != 'L2PRD'`.
            q = q.Where(x => !EF.Functions.Like((x.ProductLevelSys ?? "").ToLower(), PortQuery.Equal("L2PRD"), PortQuery.Esc));
        }

        // Nguồn 385-386: Ft_RecordStart / Ft_RecordCount của gate (mặc định 0) — biz trả toàn bộ khi không phân trang.
        return await q.ToListAsync();
    }

    /// <summary>
    /// port 1:1 BaseController.cs:3245-3269 — SysAccessCheckDeny(userState, functionNames).
    /// Nhận `IsSysAdmin` + `ListSysAccess` (dữ liệu phiên) qua interface để dùng chung cho mọi body của controller này.
    /// </summary>
    private static bool SysAccessCheckDeny(ISysAccessState? userState, string functionNames)
    {
        if (userState != null)
        {
            if (userState.IsSysAdmin == true)
                return true;

            if (functionNames != null && functionNames.Trim().Length > 0)
            {
                if (userState.ListSysAccess != null)
                    return userState.ListSysAccess.Any(a =>
                        string.Equals(a, functionNames.Trim(), StringComparison.InvariantCultureIgnoreCase));
            }
        }

        return false;
    }

    /// <summary>Dữ liệu phiên mà `SysAccessCheckDeny` cần (BaseController.cs:3245-3269).</summary>
    public interface ISysAccessState
    {
        bool? IsSysAdmin { get; }
        List<string>? ListSysAccess { get; }
    }

    /// <summary>
    /// Body của Inv_CostPriceHist_Calc. Nguồn (Inv_CostPriceHistController.cs:191) KHÔNG có tham số action; các trường dưới đây
    /// là dữ liệu nguồn lấy từ phiên (UserState / RT_Mst_Sys_Config) — thiếu thì để null, KHÔNG tự đặt mặc định.
    /// </summary>
    public class Inv_CostPriceHist_CalcReq : ISysAccessState
    {
        /// <summary>Nguồn: `userState.IsSysAdmin` (BaseController.cs:3250).</summary>
        public bool? IsSysAdmin { get; set; }
        /// <summary>Nguồn: `userState.ListSysAccess[].ObjectCode` (BaseController.cs:3258-3260).</summary>
        public List<string>? ListSysAccess { get; set; }
        /// <summary>Nguồn: `userState.RT_Mst_Sys_Config.Lst_Mst_Sys_Config` (Inv_CostPriceHistController.cs:209-211).</summary>
        public List<IndexSysConfig>? Lst_Mst_Sys_Config { get; set; }
    }

    /// <summary>
    /// port 1:1 Inv_CostPriceHistController.cs:191-265 — Inv_CostPriceHist_Calc().
    /// Chuỗi: gate → Mst_InventoryService.Instance.WA_Mst_Inventory_GetInvCodeLeaf (dòng 242)
    ///        → API MstInventoryController.cs:465-536 → biz MasterData.cs:11805-11902 (WAS_Mst_Inventory_GetInvCodeLeaf)
    ///        → MasterData.cs:11846-11863 (Mst_Inventory_GetInvCodeLeaf).
    /// </summary>
    [HttpPost("inv_costpricehist_calc")]
    public async Task<IActionResult> Inv_CostPriceHist_Calc(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] Inv_CostPriceHist_CalcReq? req)
    {
        // Nguồn 193-197: SysAccessCheckDeny(UserState, "MNU_INV_COSTPRICEHIST_CALC") — false ⇒
        // RedirectToAction("Index", "AccessDeny"). BaseController.cs:3245-3269: userState null ⇒ false; IsSysAdmin ⇒ true;
        // ngược lại phải có ObjectCode khớp (InvariantCultureIgnoreCase).
        if (!SysAccessCheckDeny(req, "MNU_INV_COSTPRICEHIST_CALC"))
            return Ok(new
            {
                Success = true,
                RedirectUrl = "/AccessDeny/Index",
                ListMst_Inventory = (object?)null,
                Data = (object?)null,
            });

        // Nguồn 208: `var listMst_Inventory = new List<Mst_Inventory>();`
        var listMst_Inventory = new List<Mst_Inventory>();

        // Nguồn 209: `userState.RT_Mst_Sys_Config.Lst_Mst_Sys_Config != null && Count > 0`.
        var lstSysConfig = req?.Lst_Mst_Sys_Config;
        if (lstSysConfig != null && lstSysConfig.Count > 0)
        {
            // Nguồn 211: Where(it => CUtils.StrValue(it.SysConfigID).Contains("CALCUPINVBYINVCODE")) — CHỨA, phân biệt hoa/thường.
            var listMst_Sys_Config_TinhGiaVon = lstSysConfig
                .Where(it => (it.SysConfigID ?? "").Contains("CALCUPINVBYINVCODE"))
                .ToList();

            // Nguồn 213: có dòng cấu hình tính giá vốn.
            if (listMst_Sys_Config_TinhGiaVon.Count > 0)
            {
                // Nguồn 216-217: strFlagActive = CUtils.StrValue(dòng đầu .FlagActive); so `Equals(FlagActive)` ("1").
                var strFlagActive = listMst_Sys_Config_TinhGiaVon[0].FlagActive ?? "";
                if (strFlagActive.Equals("1"))
                {
                    // Nguồn 221-242: theo từng kho → WA_Mst_Inventory_GetInvCodeLeaf với
                    // Ft_WhereClause = "Mst_Inventory.FlagActive='1'" (dòng 236), Rt_Cols_Mst_Inventory = "*" (dòng 239).
                    // Biz MasterData.cs:11846-11871: trả bảng "Mst_Inventory" → Lst_Mst_Inventory.
                    // GAP: thân SQL Mst_Inventory_GetInvCodeLeaf không có trong excerpt (chỉ biết điều kiện FlagActive='1').
                    var lst = await db.Set<Mst_Inventory>()
                        .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                        .ToListAsync();

                    // Nguồn 243-246: chỉ gán khi Lst_Mst_Inventory != null && Count > 0; rỗng ⇒ giữ danh sách rỗng.
                    if (lst.Count > 0)
                        listMst_Inventory = lst;
                }
                else
                {
                    // Nguồn 251-256: không theo kho ⇒ kho mặc định (I).
                    listMst_Inventory.Add(new Mst_Inventory
                    {
                        InvCode = "I",
                        InvName = "I",
                    });
                }
            }
        }

        // Nguồn 261-264: ViewBag.ListMst_Inventory = listMst_Inventory; ViewBag.UtcOffset = ...;
        // return View(new List<Inv_CostPriceHist>()).
        return Ok(new
        {
            Success = true,
            RedirectUrl = (string?)null,
            ListMst_Inventory = listMst_Inventory,
            Data = new List<Inv_CostPriceHist>(),
        });
    }

    /// <summary>
    /// Body của DoSearch_Inv_CostPriceHist. Nguồn (Inv_CostPriceHistController.cs:99) nhận 4 tham số chuỗi
    /// `productcodeuser, productname, invcode, flagiscurrent`; các trường còn lại là dữ liệu nguồn lấy từ phiên
    /// (UserState) — thiếu thì để null, KHÔNG tự đặt mặc định.
    /// </summary>
    public class DoSearch_Inv_CostPriceHistReq : ISysAccessState
    {
        /// <summary>Nguồn dòng 128 — `CUtils.StrValue(productcodeuser)`.</summary>
        public string? ProductCodeUser { get; set; }
        /// <summary>Nguồn dòng 129 — `CUtils.StrValue(productname)`.</summary>
        public string? ProductName { get; set; }
        /// <summary>Nguồn dòng 131 — `InvCode = invcode` (KHÔNG bọc StrValue).</summary>
        public string? InvCode { get; set; }
        /// <summary>Nguồn dòng 130 — `CUtils.StrValue(flagiscurrent)`.</summary>
        public string? FlagIsCurrent { get; set; }
        /// <summary>Nguồn dòng 123 — `CUtils.StrValue(UserState.Mst_NNT.OrgID)` (dùng cho Sys_User_CheckAuthorize).</summary>
        public string? OrgID { get; set; }
        /// <summary>Nguồn dòng 122 — `CUtils.StrValue(UserState.Mst_NNT.NetworkID)`.</summary>
        public string? NetworkID { get; set; }
        /// <summary>Nguồn dòng 120 — `CUtils.StrValue(UserState.SysUser.UserCode)`.</summary>
        public string? WAUserCode { get; set; }
        /// <summary>Nguồn dòng 119 — `CUtils.StrValue(UserState.AccessToken)`.</summary>
        public string? AccessToken { get; set; }
        /// <summary>Nguồn dòng 101 — `SysAccessCheckDeny(UserState, ...)` cần `userState.IsSysAdmin` (BaseController.cs:3250).</summary>
        public bool? IsSysAdmin { get; set; }
        /// <summary>Nguồn dòng 101 — `userState.ListSysAccess[].ObjectCode` (BaseController.cs:3258-3260).</summary>
        public List<string>? ListSysAccess { get; set; }
    }

    /// <summary>
    /// port 1:1 Inv_CostPriceHistController.cs:99-148 — DoSearch_Inv_CostPriceHist().
    /// Chuỗi: gate → Inv_CostPriceHistService.Instance.WA_Inv_CostPriceHist_Get (dòng 134)
    ///        → API InvCostPriceHistController.cs:163-232 → biz Form.cs:200118-200203 (WAS_Inv_CostPriceHist_Get)
    ///        → Form.cs:200205-200351 (Inv_CostPriceHist_Get) → Form.cs:200498-200669 (Inv_CostPriceHist_GetX_New20240916).
    /// </summary>
    [HttpPost("dosearch_inv_costpricehist")]
    public async Task<IActionResult> DoSearch_Inv_CostPriceHist(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DoSearch_Inv_CostPriceHistReq? req)
    {
        // Nguồn 101-105: SysAccessCheckDeny(UserState, "MNU_RPT_INV_INVENTORYBALANCE_STORAGETIME") — false ⇒
        // RedirectToAction("Index", "AccessDeny"). BaseController.cs:3245-3269: userState null ⇒ false; IsSysAdmin ⇒ true;
        // ngược lại phải có ObjectCode khớp (InvariantCultureIgnoreCase).
        if (!SysAccessCheckDeny(req, "MNU_RPT_INV_INVENTORYBALANCE_STORAGETIME"))
            return Ok(new
            {
                Success = true,
                RedirectUrl = "/AccessDeny/Index",
                Data = (object?)null,
            });

        // Nguồn 111-132: dựng RQ_Inv_CostPriceHist. EffectDTimeUTCFrom/To KHÔNG được gate gán (null) — dòng 160-161 biz.
        // Nguồn 128-131: CUtils.StrValue(x) = chuỗi rỗng khi null (KHÔNG Trim/ToUpper ở gate).
        var rq = new
        {
            ProductCodeUser = req?.ProductCodeUser ?? "",
            ProductName = req?.ProductName ?? "",
            FlagIsCurrent = req?.FlagIsCurrent ?? "",
            InvCode = req?.InvCode ?? "",
            EffectDTimeUTCFrom = (string?)null,
            EffectDTimeUTCTo = (string?)null,
        };

        // Nguồn 134: Inv_CostPriceHistService.Instance.WA_Inv_CostPriceHist_Get(objRQ, addressAPIs).
        // GAP: Sys_User_CheckAuthorize / Sys_Access_CheckDenyV30 / Sys_User_GetAbilityViewOfUser / zzzzClauseSelect_Mst_NNT_ViewAbility_Get
        //      (Form.cs:200264-200284, 200554-200558) là tầng xác thực + phân quyền theo phiên — đích không có phiên
        //      (UserState/AccessToken/ListSysAccess của biz) ⇒ bỏ qua, KHÔNG tự chế. Xem .parity-run\conflicts\24_conflict.md.
        var listInv_CostPriceHist = await GetInv_CostPriceHistAsync<DoSearch_Inv_CostPriceHistItem>(rq.ProductCodeUser, rq.ProductName,
            rq.FlagIsCurrent, rq.EffectDTimeUTCFrom, rq.EffectDTimeUTCTo, rq.InvCode);

        // Nguồn 135-140: chỉ gán khi Lst_Inv_CostPriceHist != null && Count > 0; rỗng ⇒ danh sách rỗng.
        // Nguồn 140: JsonView("BindDataInv_CostPriceHist", listInv_CostPriceHist) — trả danh sách cho view.
        return Ok(new
        {
            Success = true,
            RedirectUrl = (string?)null,
            Data = listInv_CostPriceHist,
        });
    }

    /// <summary>
    /// port 1:1 Form.cs:200498-200669 — Inv_CostPriceHist_GetX_New20240916 (bản LIVE được Inv_CostPriceHist_Get gọi, dòng 200290).
    /// Trả danh sách dòng `Inv_CostPriceHist` + 6 cột join (mp_ProductCodeUser, mp_ProductName, mp_UnitCode, mu_UnitName,
    /// mi_InvCode, mi_InvName — dòng 200620-200643).
    /// Dùng chung cho #24 `DoSearch_Inv_CostPriceHist` và #30 `ExportExcel` (cùng gọi `WA_Inv_CostPriceHist_Get`).
    /// <typeparam name="TItem">Kiểu dòng trả về — phải kế thừa `DoSearch_Inv_CostPriceHistItem` (dùng chung cho #24 và #30).</typeparam>
    /// </summary>
    private async Task<List<TItem>> GetInv_CostPriceHistAsync<TItem>(
        string? productCodeUser, string? productName, string? flagIsCurrent,
        string? effectDTimeUTCFrom, string? effectDTimeUTCTo, string? invCode)
        where TItem : DoSearch_Inv_CostPriceHistItem, new()
    {
        // Nguồn 200538-200543: Refine — StdParam (Trim+ToUpper, rỗng ⇒ null), StdStr (Trim, rỗng ⇒ null),
        // StdFlag ("1" nếu đúng "1", mọi giá trị khác ⇒ "0"), StdDTime.
        var pProductCodeUser = StdParam(productCodeUser);
        var pProductName = StdStr(productName);
        var pFlagIsCurrent = StdFlag(flagIsCurrent);
        var pEffectDTimeUTCFrom = StdDTime(effectDTimeUTCFrom);
        var pEffectDTimeUTCTo = StdDTime(effectDTimeUTCTo);
        var pInvCode = StdParam(invCode);

        // Nguồn 200571-200610: #tbl_Inv_CostPriceHist_Filter — inner join Mst_Product f (ProductCode + OrgID),
        // where (1=1) + 5 điều kiện; FlagIsCurrent = "0" ⇒ select distinct (ProductCode, EffectDTimeUTC, OrgID, InvCode),
        // ngược lại ⇒ group by (ProductCode, OrgID, InvCode) lấy Max(EffectDTimeUTC).
        var q = from t in db.Set<Inv_CostPriceHist>()
                join f in db.Set<Mst_Product>()
                    on new { t.ProductCode, t.OrgID } equals new { f.ProductCode, f.OrgID }
                select new { t, f };

        // Nguồn 200576/200601: `('@strProductCodeUser' = '' or f.ProductCodeUser like '%@strProductCodeUser%')`.
        if (pProductCodeUser != null)
            q = q.Where(x => EF.Functions.Like((x.f.ProductCodeUser ?? "").ToLower(), PortQuery.Contains(pProductCodeUser), PortQuery.Esc));
        // Nguồn 200577/200602: `('@strProductName' = '' or f.ProductName like '%@strProductName%')`.
        if (pProductName != null)
            q = q.Where(x => EF.Functions.Like((x.f.ProductName ?? "").ToLower(), PortQuery.Contains(pProductName), PortQuery.Esc));
        // Nguồn 200578/200603: `('@strInvCode' = '' or t.InvCode = '@strInvCode')` — toán tử `=`, KHÔNG wildcard.
        if (pInvCode != null)
            q = q.Where(x => EF.Functions.Like((x.t.InvCode ?? "").ToLower(), PortQuery.Equal(pInvCode), PortQuery.Esc));
        // Nguồn 200579/200604: `('@strEffectDTimeUTCFrom' = '' or t.EffectDTimeUTC >= '@strEffectDTimeUTCFrom')`.
        if (pEffectDTimeUTCFrom != null)
            q = q.Where(x => x.t.EffectDTimeUTC >= pEffectDTimeUTCFrom);
        // Nguồn 200580/200605: `('@strEffectDTimeUTCTo' = '' or t.EffectDTimeUTC <= '@strEffectDTimeUTCTo')`.
        if (pEffectDTimeUTCTo != null)
            q = q.Where(x => x.t.EffectDTimeUTC <= pEffectDTimeUTCTo);

        // Nguồn 200561: `StringEqual(strFlagIsCurrent, TConst.Flag.Inactive)` — "0" ⇒ nhánh select distinct (KHÔNG group by).
        List<Inv_CostPriceHist> filterRows;
        if (string.Equals(pFlagIsCurrent, "0", StringComparison.Ordinal))
        {
            filterRows = await q.Select(x => x.t).Distinct().ToListAsync();
        }
        else
        {
            // Nguồn 200588-200610: group by (ProductCode, OrgID, InvCode) → Max(EffectDTimeUTC).
            var maxRows = await q.GroupBy(x => new { x.t.ProductCode, x.t.OrgID, x.t.InvCode })
                .Select(g => new { g.Key.ProductCode, g.Key.OrgID, g.Key.InvCode, EffectDTimeUTC = g.Max(y => y.t.EffectDTimeUTC) })
                .ToListAsync();
            filterRows = maxRows.Select(m => new Inv_CostPriceHist
            {
                ProductCode = m.ProductCode,
                OrgID = m.OrgID,
                InvCode = m.InvCode,
                EffectDTimeUTC = m.EffectDTimeUTC,
            }).ToList();
        }

        // Nguồn 200620-200643: inner join #tbl_Inv_CostPriceHist_Filter f (ProductCode + EffectDTimeUTC + OrgID + InvCode),
        // inner join Mst_Product mp (ProductCode + OrgID), left join Mst_Unit mu (OrgID + UnitCode),
        // inner join Mst_Inventory mi (InvCode + OrgID); where (1=1).
        // GAP: Mst_Inventory ở đích chỉ có khoá InvCode (Models/Port/Mst_Inventory.cs:29) — nguồn join thêm `t.OrgID = mi.OrgID`
        //      (dòng 200642) nhưng cột OrgID của Mst_Inventory KHÔNG có trong nguồn được trích ⇒ bỏ điều kiện OrgID, ghi GAP.
        var result = new List<TItem>();
        foreach (var f in filterRows)
        {
            var rows = await (from t in db.Set<Inv_CostPriceHist>()
                              join mp in db.Set<Mst_Product>()
                                  on new { t.ProductCode, t.OrgID } equals new { mp.ProductCode, mp.OrgID }
                              join mi in db.Set<Mst_Inventory>()
                                  on t.InvCode equals mi.InvCode
                              join mu in db.Set<Mst_Unit>()
                                  on new { t.OrgID, mp.UnitCode } equals new { mu.OrgID, mu.UnitCode } into muJoin
                              from mu in muJoin.DefaultIfEmpty()
                              where t.ProductCode == f.ProductCode
                                    && t.EffectDTimeUTC == f.EffectDTimeUTC
                                    && t.OrgID == f.OrgID
                                    && t.InvCode == f.InvCode
                              select new TItem
                              {
                                  Id = t.Id,
                                  ProductCode = t.ProductCode,
                                  EffectDTimeUTC = t.EffectDTimeUTC,
                                  OrgID = t.OrgID,
                                  InvCode = t.InvCode,
                                  mp_ProductCodeUser = mp.ProductCodeUser,
                                  mp_ProductName = mp.ProductName,
                                  mp_UnitCode = mp.UnitCode,
                                  mu_UnitName = mu == null ? null : mu.UnitName,
                                  mi_InvCode = mi.InvCode,
                                  mi_InvName = mi.InvName,
                              }).ToListAsync();
            // #30 ExportExcel: 4 cột `t.*` của bảng (Inv_CostPriceHistController.cs:837-840) — gán sau khi truy vấn
            // (TItem chỉ ràng buộc lớp cơ sở nên không chiếu trực tiếp trong biểu thức LINQ).
            if (typeof(TItem) == typeof(ExportExcel_Inv_CostPriceHistItem))
            {
                var ids = rows.Select(r => r.Id).ToList();
                var extra = await db.Set<Inv_CostPriceHist>().AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .Select(x => new { x.Id, x.UPInv, x.FormNo, x.UpdBy, x.UpdDTimeUTC })
                    .ToDictionaryAsync(x => x.Id);
                foreach (var r in rows)
                {
                    if (!extra.TryGetValue(r.Id, out var e)) continue;
                    var item = (ExportExcel_Inv_CostPriceHistItem)(object)r;
                    item.UPInv = e.UPInv;
                    item.FormNo = e.FormNo;
                    item.UpdBy = e.UpdBy;
                    item.UpdDTimeUTC = e.UpdDTimeUTC;
                }
            }
            result.AddRange(rows);
        }

        return result;
    }

    /// <summary>Nguồn TUtils.CUtils.StdParam — `Trim().ToUpper()`, rỗng ⇒ null (luật PORT_RULES mục 3).</summary>
    private static string? StdParam(string? x)
    {
        var v = x?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(v) ? null : v;
    }

    /// <summary>Nguồn TUtils.CUtils.StdStr — `Trim().Normalize()`, rỗng ⇒ null (luật PORT_RULES mục 3).</summary>
    private static string? StdStr(string? x)
    {
        var v = x?.Trim().Normalize();
        return string.IsNullOrEmpty(v) ? null : v;
    }

    /// <summary>Nguồn TUtils.CUtils.StdFlag — `"1"` nếu đúng `"1"`, MỌI giá trị khác kể cả null ⇒ `"0"` (luật PORT_RULES mục 3).</summary>
    private static string StdFlag(string? x) => x == "1" ? "1" : "0";

    /// <summary>
    /// Nguồn TUtils.CUtils.StdDTime — chuẩn hoá chuỗi thời gian; rỗng ⇒ null. Định dạng chấp nhận KHÔNG có trong tài liệu
    /// (DLL không có mã) ⇒ chỉ nhận chuỗi ISO/`yyyy-MM-dd HH:mm:ss`; chuỗi khác coi như null (GAP, xem conflicts\24_conflict.md).
    /// </summary>
    private static DateTime? StdDTime(string? x)
    {
        var v = x?.Trim();
        if (string.IsNullOrEmpty(v)) return null;
        if (DateTime.TryParse(v, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt)) return dt;
        return null;
    }
}

/// <summary>
/// port 1:1 model nguồn `Mst_ProductUI` (Inv_CostPriceHistController.cs:344-351) — 2 danh sách sản phẩm.
/// Nguồn KHÔNG có file model Mst_ProductUI trong 12.Dev.Common (excerpt dòng 213) ⇒ chỉ khai 2 thuộc tính gate dùng.
/// </summary>
public class LoadData_ProductUI
{
    /// <summary>Nguồn 346 — `productbase.LstPrdBase = objRT_Mst_ProductBase.Lst_Mst_Product`.</summary>
    public List<Mst_Product> LstPrdBase { get; set; } = new();
    /// <summary>Nguồn 347-350 — `LstPrdLeve2` (giữ nguyên lỗi chính tả của nguồn) + AddRange(LstPrdBase).</summary>
    public List<Mst_Product> LstPrdLeve2 { get; set; } = new();
}

/// <summary>
/// Một dòng kết quả của DoSearch_Inv_CostPriceHist — `t.*` (Form.cs:200621) + 6 cột join (dòng 200622-200627).
/// Cột `t.*` ngoài 4 cột đã biết KHÔNG có trong excerpt ⇒ GAP, không tự đặt.
/// </summary>
public class DoSearch_Inv_CostPriceHistItem
{
    public string Id { get; set; } = "";
    public string? ProductCode { get; set; }
    public DateTime? EffectDTimeUTC { get; set; }
    public string? OrgID { get; set; }
    public string? InvCode { get; set; }
    /// <summary>Nguồn 200622 — `mp.ProductCodeUser mp_ProductCodeUser`.</summary>
    public string? mp_ProductCodeUser { get; set; }
    /// <summary>Nguồn 200623 — `mp.ProductName mp_ProductName`.</summary>
    public string? mp_ProductName { get; set; }
    /// <summary>Nguồn 200624 — `mp.UnitCode mp_UnitCode`.</summary>
    public string? mp_UnitCode { get; set; }
    /// <summary>Nguồn 200625 — `mu.UnitName mu_UnitName` (left join ⇒ null khi không có đơn vị).</summary>
    public string? mu_UnitName { get; set; }
    /// <summary>Nguồn 200626 — `mi.InvCode mi_InvCode`.</summary>
    public string? mi_InvCode { get; set; }
    /// <summary>Nguồn 200627 — `mi.InvName mi_InvName`.</summary>
    public string? mi_InvName { get; set; }
}

/// <summary>
/// Một dòng kết quả của ExportExcel — `t.*` (Form.cs:200621) + 3 cột join Mst_Product (dòng 200622-200624).
/// Nguồn 759-762 xuất Excel từ `Lst_Inv_CostPriceHist` (kiểu `Inv_CostPriceHist`) với từ điển cột 829-843, trong đó
/// `mp_ProductCodeUser`/`mp_ProductName`/`mp_UnitCode` là cột join ⇒ dùng lại item của #24 (đã có đủ 3 cột đó).
/// </summary>
public class ExportExcel_Inv_CostPriceHistItem : DoSearch_Inv_CostPriceHistItem
{
    /// <summary>Nguồn Inv_CostPriceHistController.cs:837 — `TblInv_CostPriceHist.UPInv` (Giá vốn).</summary>
    public double? UPInv { get; set; }
    /// <summary>Nguồn Inv_CostPriceHistController.cs:838 — `TblInv_CostPriceHist.FormNo` (Số phiếu nhập).</summary>
    public string? FormNo { get; set; }
    /// <summary>Nguồn Inv_CostPriceHistController.cs:839 — `TblInv_CostPriceHist.UpdBy` (Người cập nhật).</summary>
    public string? UpdBy { get; set; }
    /// <summary>Nguồn Inv_CostPriceHistController.cs:840 — `TblInv_CostPriceHist.UpdDTimeUTC` (Ngày cập nhật).</summary>
    public DateTime? UpdDTimeUTC { get; set; }
}

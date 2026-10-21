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
public class Inv_CostPriceHistControllerPort(AppDbContext db) : ControllerBase
{
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
        var listInv_CostPriceHist = await GetInv_CostPriceHistAsync(rq.ProductCodeUser, rq.ProductName,
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
    /// </summary>
    private async Task<List<DoSearch_Inv_CostPriceHistItem>> GetInv_CostPriceHistAsync(
        string? productCodeUser, string? productName, string? flagIsCurrent,
        string? effectDTimeUTCFrom, string? effectDTimeUTCTo, string? invCode)
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
        var result = new List<DoSearch_Inv_CostPriceHistItem>();
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
                              select new DoSearch_Inv_CostPriceHistItem
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

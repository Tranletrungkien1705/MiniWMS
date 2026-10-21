using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 Inv_CostPriceHistController.cs — Inv_CostPriceHist (action Index).
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
    public class IndexReq
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
    /// </summary>
    private static bool SysAccessCheckDeny(IndexReq? userState, string functionNames)
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
}

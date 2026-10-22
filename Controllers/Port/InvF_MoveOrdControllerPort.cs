using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_MoveOrdController.cs — InvF_MoveOrd (action Index, DoSearch).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\InvF_MoveOrdController.cs:23-85.
///   -> BaseController.cs:3245-3269 (SysAccessCheckDeny)
///   -> Mst_InventoryService.Instance.WA_Mst_Inventory_Get (dòng 76)
///   -> API WA_Mst_Inventory_Get (MstInventoryController.cs:18-89)
///   -> BIZ WAS_Mst_Inventory_Get (MasterData.cs:9254-9368) -> Mst_Inventory_Get.
/// Gate KHÔNG đọc InvF_MoveOrd: chỉ dựng PageInfo rỗng (dòng 43-46) rồi trả View(pageInfo) (dòng 84).
///
/// #143 DoSearch: InvF_MoveOrdController.cs:89-151 (+ strWhereClause_InvF_MoveOrd dòng 3696-3780)
///   -> InvF_MoveOrdService.Instance.WA_InvF_MoveOrd_Get (dòng 137)
///   -> API InvFMoveOrdController.cs:20-89 (WA_InvF_MoveOrd_Get)
///   -> BIZ Form.cs:183838-183975 (WAS_InvF_MoveOrd_Get) -> Form.cs:183663-183836 (InvF_MoveOrd_Get)
///   -> Form.cs:183334-183660 (InvF_MoveOrd_GetX).
/// </summary>
[ApiController]
public class InvF_MoveOrdControllerPort(AppDbContext db) : ControllerBase
{
    public class IndexReq
    {
        /// <summary>Nguồn dòng 23 — tham số `int page = 0`.</summary>
        public int Page { get; set; } = 0;
        /// <summary>Nguồn dòng 23 — tham số `int recordcount = 10`.</summary>
        public int RecordCount { get; set; } = 10;

        /// <summary>Nguồn BaseController.cs:3250 — userState.IsSysAdmin.</summary>
        public bool? IsSysAdmin { get; set; }
        /// <summary>Nguồn BaseController.cs:3258-3260 — userState.ListSysAccess[].ObjectCode.</summary>
        public List<string>? ListSysAccess { get; set; }
    }

    public class IndexResult
    {
        public bool Success { get; set; } = true;
        /// <summary>Nguồn dòng 28 — RedirectToAction("Index", "AccessDeny") khi bị chặn quyền.</summary>
        public string? RedirectUrl { get; set; }
        /// <summary>Nguồn dòng 43-46, 84 — PageInfo rỗng (DataList rỗng, ItemCount/PageCount = 0).</summary>
        public PageInfo<InvF_MoveOrd>? PageInfo { get; set; }
        /// <summary>Nguồn dòng 48-81 — ViewBag.List_Mst_InventoryInOut (WA_Mst_Inventory_Get, lọc FlagActive = '1' and FlagIn_Out = '1').</summary>
        public List<Mst_Inventory>? List_Mst_InventoryInOut { get; set; }
    }

    /// <summary>
    /// port 1:1 InvF_MoveOrdController.cs:23-85 — Index(int page = 0, int recordcount = 10).
    /// 1) SysAccessCheckDeny(UserState, "MNU_INVF_MOVEORD") — sai thì RedirectToAction("Index", "AccessDeny") (dòng 25-29).
    /// 2) Dựng PageInfo&lt;InvF_MoveOrd&gt;(0, recordcount) với DataList rỗng (dòng 43-46).
    /// 3) Lấy danh sách kho nhập/xuất: Mst_Inventory.FlagActive = '1' and Mst_Inventory.FlagIn_Out = '1' (dòng 52-53),
    ///    Rt_Cols_Mst_Inventory = "*" (dòng 74) ⇒ trả đủ cột; danh sách rỗng thì ViewBag là list rỗng (dòng 77-81).
    /// 4) return View(pageInfo) (dòng 84).
    /// </summary>
    [HttpPost("api/invf_moveord/index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] IndexReq? req)
    {
        var page = req?.Page ?? 0;
        var recordcount = req?.RecordCount ?? 10;

        // Nguồn dòng 25-29: chặn quyền MNU_INVF_MOVEORD -> RedirectToAction("Index", "AccessDeny")
        if (!SysAccessCheckDeny(req, "MNU_INVF_MOVEORD"))
        {
            return Ok(new IndexResult
            {
                Success = true,
                RedirectUrl = "/AccessDeny/Index"
            });
        }

        // Nguồn dòng 43-46: PageInfo<InvF_MoveOrd>(0, recordcount) { DataList = new List<InvF_MoveOrd>() }
        var pageInfo = new PageInfo<InvF_MoveOrd>(
            DataList: new List<InvF_MoveOrd>(),
            ItemCount: 0,
            PageCount: 0,
            PageIndex: page,
            PageSize: recordcount
        );

        // Nguồn dòng 52-53: Mst_Inventory.FlagActive = '1' and Mst_Inventory.FlagIn_Out = '1'
        var listMstInventoryInOut = await db.Set<Mst_Inventory>()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagIn_Out ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .OrderBy(x => x.InvCode)
            .ToListAsync();

        // Nguồn dòng 77-81: chỉ AddRange khi Lst_Mst_Inventory.Count > 0; ViewBag luôn là list (rỗng nếu không có)
        return Ok(new IndexResult
        {
            Success = true,
            PageInfo = pageInfo,
            List_Mst_InventoryInOut = listMstInventoryInOut.Count > 0 ? listMstInventoryInOut : new List<Mst_Inventory>()
        });
    }

    /// <summary>Nguồn BaseController.cs:3245-3269 — SysAccessCheckDeny.</summary>
    private static bool SysAccessCheckDeny(IndexReq? req, string functionNames)
    {
        if (req != null)
        {
            if (req.IsSysAdmin == true) return true;
            if (!string.IsNullOrWhiteSpace(functionNames) && req.ListSysAccess != null)
            {
                return req.ListSysAccess.Any(a => string.Equals(a, functionNames.Trim(), StringComparison.InvariantCultureIgnoreCase));
            }
        }
        return false;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 DashboardRptController.cs — Rpt_Summary_In_Out_Pivot (action WG_Rpt_Summary_In_Pivot, WG_Rpt_Summary_Out_Pivot).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\DashboardRptController.cs:230-351 (IN), 353-474 (OUT).
/// Chuỗi: gate WG_Rpt_Summary_In_Pivot → Mst_InventoryService.Instance.WA_Mst_Inventory_Get (dòng 264)
///        → ReportsService.Instance.WA_Rpt_Summary_In_Out_Pivot (dòng 313) → gộp theo ProductCode (dòng 316-337).
/// Chuỗi: gate WG_Rpt_Summary_Out_Pivot → Mst_InventoryService.Instance.WA_Mst_Inventory_Get (dòng 387)
///        → ReportsService.Instance.WA_Rpt_Summary_In_Out_Pivot (dòng 436, InventoryAction = "OUT") → gộp theo ProductCode (dòng 437-461).
/// GAP: excerpt #13/#14 KHÔNG có tầng API/biz của WA_Rpt_Summary_In_Out_Pivot (dòng 178: "API: khong thay controller DashboardRptController.cs
///      trong ...Biz.Web\Controllers"; dòng 180-183: 0 chỗ ném lỗi, không mã lỗi) ⇒ KHÔNG port được phép tính báo cáo.
///      Endpoint trả PortResult.Fail("DashboardRpt_WG_Rpt_Summary_In_Pivot", "GAP.NoBackend", ...) — xem .parity-run\conflicts\13_conflict.md.
/// GAP: gate chặn `User.Identity.IsAuthenticated && UserReportState != null` (dòng 232/355) — đích không có phiên đăng nhập báo cáo
///      (UserReportState/AccessToken/UtcOffset/AddressAPIs) nên bỏ qua guard này, không tự chế giá trị thay thế.
/// </summary>
[ApiController]
public class DashboardRptControllerPort(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Tham số của gate (DashboardRptController.cs:230): `networkid = ""`, `lstinvcode = ""`, `init = "init"`.
    /// `lstinvcode` là chuỗi JSON danh sách mã kho (dòng 274) — nhận nguyên chuỗi như nguồn.
    /// </summary>
    public class WG_Rpt_Summary_In_PivotReq
    {
        public string? NetworkID { get; set; }
        public string? LstInvCode { get; set; }
        public string? Init { get; set; }
    }

    // Nguồn 230-351: POST /api/dashboardrpt/wg_rpt_summary_in_pivot
    [HttpPost("api/dashboardrpt/wg_rpt_summary_in_pivot")]
    public async Task<IActionResult> WG_Rpt_Summary_In_Pivot(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WG_Rpt_Summary_In_PivotReq? req)
    {
        // Nguồn 230: tham số mặc định của action — `init = "init"` (dòng 272/339 so `!init.Equals("init")`).
        var init = req?.Init ?? "init";

        // Nguồn 241-269: danh sách kho — where `Mst_Inventory.FlagIn_Out = '1'` (dòng 242), chỉ lấy cột InvCode (dòng 262).
        // Nguồn 265: `objRT_Mst_Inventory != null && Lst_Mst_Inventory.Count > 0` mới add; rỗng ⇒ danh sách rỗng.
        var listMstInventoryInOut = await db.Set<Mst_Inventory>()
            .Where(x => EF.Functions.Like((x.FlagIn_Out ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .Select(x => new Mst_Inventory { InvCode = x.InvCode })
            .ToListAsync();

        // Nguồn 271-290: init != "init" ⇒ deserialize lstinvcode (JSON List<string>) và dựng danh sách kho theo mã;
        // ngược lại ⇒ dùng toàn bộ danh sách kho ở trên.
        var lstMstInventoryRQ = new List<Mst_Inventory>();
        if (!init.Equals("init"))
        {
            // Nguồn 274: JsonConvert.DeserializeObject<List<string>>(lstinvcode) — chuỗi rỗng/null ⇒ ném (gate không bắt).
            var lstInvCodeSearch = System.Text.Json.JsonSerializer.Deserialize<List<string>>(req?.LstInvCode ?? "");
            if (lstInvCodeSearch != null && lstInvCodeSearch.Count > 0)
            {
                foreach (var invCode in lstInvCodeSearch)
                    lstMstInventoryRQ.Add(new Mst_Inventory { InvCode = invCode });
            }
        }
        else
        {
            lstMstInventoryRQ.AddRange(listMstInventoryInOut);
        }

        // Nguồn 293-313: RQ_Rpt_Summary_In_Out_Pivot (ApprDTimeUTCFrom/To = đầu/cuối tháng hiện tại, InventoryAction = "IN",
        // Lst_Mst_Inventory = danh sách kho) → ReportsService.Instance.WA_Rpt_Summary_In_Out_Pivot.
        // GAP: tầng API/biz của WA_Rpt_Summary_In_Out_Pivot KHÔNG có trong nguồn được trích (excerpt dòng 178-183) ⇒ không port được
        // phép tính báo cáo; KHÔNG tự chế bảng/kết quả. Xem .parity-run\conflicts\13_conflict.md.
        _ = lstMstInventoryRQ;
        return PortResult.Fail("DashboardRpt_WG_Rpt_Summary_In_Pivot", "GAP.NoBackend",
            "WA_Rpt_Summary_In_Out_Pivot", "ReportsService.Instance.WA_Rpt_Summary_In_Out_Pivot (DashboardRptController.cs:313)");
    }

    /// <summary>
    /// Tham số của gate (DashboardRptController.cs:353): `networkid = ""`, `lstinvcode = ""`, `init = "init"`.
    /// `lstinvcode` là chuỗi JSON danh sách mã kho (dòng 397) — nhận nguyên chuỗi như nguồn.
    /// </summary>
    public class WG_Rpt_Summary_Out_PivotReq
    {
        public string? NetworkID { get; set; }
        public string? LstInvCode { get; set; }
        public string? Init { get; set; }
    }

    // Nguồn 353-474: POST /api/dashboardrpt/wg_rpt_summary_out_pivot
    [HttpPost("api/dashboardrpt/wg_rpt_summary_out_pivot")]
    public async Task<IActionResult> WG_Rpt_Summary_Out_Pivot(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] WG_Rpt_Summary_Out_PivotReq? req)
    {
        // Nguồn 353: tham số mặc định của action — `init = "init"` (dòng 395/462 so `!init.Equals("init")`).
        var init = req?.Init ?? "init";

        // Nguồn 364-392: danh sách kho — where `Mst_Inventory.FlagIn_Out = '1'` (dòng 365), chỉ lấy cột InvCode (dòng 385).
        // Nguồn 388: `objRT_Mst_Inventory != null && Lst_Mst_Inventory.Count > 0` mới add; rỗng ⇒ danh sách rỗng.
        var listMstInventoryInOut = await db.Set<Mst_Inventory>()
            .Where(x => EF.Functions.Like((x.FlagIn_Out ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .Select(x => new Mst_Inventory { InvCode = x.InvCode })
            .ToListAsync();

        // Nguồn 394-413: init != "init" ⇒ deserialize lstinvcode (JSON List<string>) và dựng danh sách kho theo mã;
        // ngược lại ⇒ dùng toàn bộ danh sách kho ở trên.
        var lstMstInventoryRQ = new List<Mst_Inventory>();
        if (!init.Equals("init"))
        {
            // Nguồn 397: JsonConvert.DeserializeObject<List<string>>(lstinvcode) — chuỗi rỗng/null ⇒ ném (gate không bắt).
            var lstInvCodeSearch = System.Text.Json.JsonSerializer.Deserialize<List<string>>(req?.LstInvCode ?? "");
            if (lstInvCodeSearch != null && lstInvCodeSearch.Count > 0)
            {
                foreach (var invCode in lstInvCodeSearch)
                    lstMstInventoryRQ.Add(new Mst_Inventory { InvCode = invCode });
            }
        }
        else
        {
            lstMstInventoryRQ.AddRange(listMstInventoryInOut);
        }

        // Nguồn 416-436: RQ_Rpt_Summary_In_Out_Pivot (ApprDTimeUTCFrom/To = đầu/cuối tháng hiện tại, InventoryAction = "OUT",
        // Lst_Mst_Inventory = danh sách kho) → ReportsService.Instance.WA_Rpt_Summary_In_Out_Pivot.
        // GAP: tầng API/biz của WA_Rpt_Summary_In_Out_Pivot KHÔNG có trong nguồn được trích (excerpt dòng 178-183) ⇒ không port được
        // phép tính báo cáo; KHÔNG tự chế bảng/kết quả. Xem .parity-run\conflicts\13_conflict.md.
        _ = lstMstInventoryRQ;
        return PortResult.Fail("DashboardRpt_WG_Rpt_Summary_Out_Pivot", "GAP.NoBackend",
            "WA_Rpt_Summary_In_Out_Pivot", "ReportsService.Instance.WA_Rpt_Summary_In_Out_Pivot (DashboardRptController.cs:436)");
    }
}

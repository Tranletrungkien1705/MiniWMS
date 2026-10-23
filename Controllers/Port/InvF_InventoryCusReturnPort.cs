using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryCusReturnController.cs — action Index (#32).
/// Nguồn: InvF_InventoryCusReturnController.cs:28-110 — action MVC GET Index(int page=0, int recordcount=10).
/// Đích: POST /api/invf_inventorycusreturn/index body JSON { page, recordcount } (hai trường tuỳ chọn,
/// thiếu/null body ⇒ mặc định 0/10 đúng như chữ ký nguồn — A.8, A.14).
/// Action chỉ ĐỌC: 2 truy vấn danh mục Mst_Inventory (FlagActive='1' and FlagIn_Out='1', gate :53)
/// và Mst_Customer (FlagActive='1', gate :86); PageInfo luôn rỗng (gate :47-50/:96-99).
/// KHÔNG truy vấn bảng InvF_InventoryCusReturn ⇒ không tạo entity EF cho bảng này.
/// </summary>
[ApiController]
[Route("api/invf_inventorycusreturn")]
public class InvF_InventoryCusReturnControllerPort(AppDbContext db) : ControllerBase
{
    /// <summary>Body JSON của action Index — hai trường tuỳ chọn, mặc định 0/10.</summary>
    public class InvF_InventoryCusReturn_IndexReq
    {
        public int Page { get; set; } = 0;
        public int Recordcount { get; set; } = 10;
    }

    /// <summary>
    /// Hình dạng kết quả (P1 "Hinh dang ket qua"): Success/PageCur/Recordcount/StartCount/PageInfo
    /// + 2 danh sách tra cứu. Đây KHÔNG phải entity của bảng InvF_InventoryCusReturn (P3).
    /// </summary>
    public class InvF_InventoryCusReturn_IndexData
    {
        public bool Success { get; set; } = true;
        public int PageCur { get; set; }
        public int Recordcount { get; set; }
        public string? StartCount { get; set; }
        public PageInfo<object>? PageInfo { get; set; }
        public List<Mst_Inventory>? Lst_Mst_Inventory { get; set; }
        public List<Mst_Customer>? Lst_MstCustomer { get; set; }
    }

    /// <summary>
    /// Action Index — nguồn gate :28-110.
    /// GAP: guard quyền `SysAccessCheckDeny(UserState, "MNU_INVF_INVENTORYCUSRETURN")` (gate :30-34) KHÔNG port
    /// được — đích không có session/quyền (P1.a "BO QUA, ghi GAP").
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] InvF_InventoryCusReturn_IndexReq? req)
    {
        // Nguồn gate :28 — mặc định page = 0, recordcount = 10. Không clamp (P1 "Moi tham so dau vao", A.12).
        var page = req?.Page ?? 0;
        var recordcount = req?.Recordcount ?? 10;

        // Nguồn gate :43-44: itemCount = 0; pageCount = 0.
        var itemCount = 0;
        var pageCount = 0;

        // Nguồn gate :47-50 khởi tạo `new PageInfo<InvF_InventoryCusReturn>(0, recordcount) { DataList = new List<>() }`
        // rồi :96-99 gán PageSize/PageIndex/ItemCount/PageCount. Đích dùng PageInfo<object> — không tạo entity EF.
        var pageInfo = new PageInfo<object>(
            DataList: new List<object>(),
            ItemCount: itemCount,
            PageCount: pageCount,
            PageIndex: page,
            PageSize: recordcount);

        // Nguồn gate :53 + BaseController.Task_Mst_Inventory_Get :2717-2750 — where
        // "Mst_Inventory.FlagActive = '1' and Mst_Inventory.FlagIn_Out = '1'".
        // Nguồn dùng "=" (không wildcard) ⇒ PortQuery.Equal, hạ chữ + escape (luật §3, A.14).
        var listMstInventory = await db.Set<Mst_Inventory>()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagIn_Out ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            // Sắp xếp: thân Mst_Inventory_Get không có trong excerpt ⇒ ORDER BY CHƯA XÁC MINH.
            // Chọn InvCode cho ổn định — khai S + GAP trong bảng đối chiếu.
            .OrderBy(x => x.InvCode)
            .ToListAsync();

        // Nguồn gate :86 — where "Mst_Customer.FlagActive = '1'" (điều kiện CustomerType='NCC' bị comment ⇒ KHÔNG lọc).
        // Đích dùng "=" ⇒ PortQuery.Equal như trên.
        var listMstCustomer = await db.Set<Mst_Customer>()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .OrderBy(x => x.CustomerCode) // Sắp xếp chưa xác minh ở nguồn — chọn CustomerCode cho ổn định, ghi S+GAP.
            .ToListAsync();

        // Nguồn gate :100 — ViewBag.StartCount = (page * recordcount).ToString() giữ nguyên phép nhân int (không đổi long, không clamp).
        return Ok(new InvF_InventoryCusReturn_IndexData
        {
            Success = true,
            PageCur = page,
            Recordcount = recordcount,
            StartCount = (page * recordcount).ToString(),
            PageInfo = pageInfo,
            Lst_Mst_Inventory = listMstInventory,
            Lst_MstCustomer = listMstCustomer
        });
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — InvF_InventoryIn (action Index).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\InvF_InventoryInController.cs:28-186.
///
/// Luồng nghiệp vụ của Index:
/// 1. SysAccessCheckDeny(UserState, "MNU_INVF_INVENTORYIN") — false ⇒ RedirectToAction("Index", "AccessDeny") (dòng 30-34).
/// 2. Khởi tạo PageInfo rỗng: pageInfo.PageSize = recordcount; pageInfo.PageIndex = page;
///    itemCount = 0; pageCount = 0; DataList = new List<InvF_InventoryIn>() (dòng 43-50, 149-154).
/// 3. Lấy song song (dòng 52-91):
///    - Mst_Inventory: FlagActive = '1' và FlagIn_Out = '1' (dòng 53);
///    - Mst_InvInType: FlagActive = '1' (dòng 54);
///    - Mst_BusinessGroup: BusinessType = "DV" và FlagActive = '1' (dòng 61-65, 222-230).
/// 4. Thông tin khách hàng / nhà cung cấp (dòng 108):
///    - Mst_Customer: FlagActive = '1' và FlagSupplier = '1'.
/// 5. Thông tin chi nhánh (dòng 119):
///    - Mst_NNT: MSTBUPattern like UserState.Mst_NNT.MSTBUPattern và FlagActive = '1'.
/// 6. Thông tin hàng hoá (dòng 139):
///    - Mst_Product: FlagActive = '1' (lọc theo các sản phẩm có thể nhập).
///
/// GAP:
/// - UserState / AddressAPIs / Gateway token của nguồn là phiên Web. Ở đích không có phiên nên nhận
///   các trường phiên tùy chọn qua body (`IsSysAdmin`, `ListSysAccess`, `MSTBUPattern`).
/// - Dịch vụ ngoài Mst_CustomerService, Mst_ProductService, Mst_NNTService gọi WCF/HTTP sang ProductCenter/Master
///   ở nguồn được thay thế bằng đọc dữ liệu từ DB cục bộ chuẩn hóa của MiniWMS theo đúng bộ lọc SQL nguồn.
/// </summary>
[ApiController]
public class InvF_InventoryInControllerPort(AppDbContext db) : ControllerBase
{
    public class IndexReq
    {
        public int Page { get; set; } = 0;
        public int RecordCount { get; set; } = 10;

        /// <summary>Nguồn BaseController.cs:3250 — userState.IsSysAdmin.</summary>
        public bool? IsSysAdmin { get; set; }
        /// <summary>Nguồn BaseController.cs:3258-3260 — userState.ListSysAccess[].ObjectCode.</summary>
        public List<string>? ListSysAccess { get; set; }
        /// <summary>Nguồn dòng 119 — UserState.Mst_NNT.MSTBUPattern.</summary>
        public string? MSTBUPattern { get; set; }
    }

    public class IndexResult
    {
        public bool Success { get; set; } = true;
        public string? RedirectUrl { get; set; }
        public PageInfo<InvF_InventoryIn>? PageInfo { get; set; }
        public List<Mst_Inventory>? Lst_Mst_Inventory { get; set; }
        public List<Mst_InvInType>? Lst_Mst_InvInType { get; set; }
        public List<Mst_BusinessGroup>? Lst_Mst_BusinessGroup { get; set; }
        public List<Mst_Customer>? Lst_MstCustomer { get; set; }
        public List<Mst_NNT>? ListMst_NNT { get; set; }
        public List<Mst_Product>? Lst_Mst_Product { get; set; }
        public string? StartCount { get; set; }
    }

    [HttpPost("api/invf_inventoryin/index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] IndexReq? req)
    {
        var page = req?.Page ?? 0;
        var recordcount = req?.RecordCount ?? 10;

        // Nguồn dòng 30-34: SysAccessCheckDeny(UserState, "MNU_INVF_INVENTORYIN")
        if (!SysAccessCheckDeny(req, "MNU_INVF_INVENTORYIN"))
        {
            return Ok(new IndexResult
            {
                Success = true,
                RedirectUrl = "/AccessDeny/Index"
            });
        }

        // Nguồn dòng 47-50, 149-154: PageInfo rỗng
        var pageInfo = new PageInfo<InvF_InventoryIn>(
            DataList: new List<InvF_InventoryIn>(),
            ItemCount: 0,
            PageCount: 0,
            PageIndex: page,
            PageSize: recordcount
        );

        // Nguồn dòng 53: Mst_Inventory.FlagActive = '1' and Mst_Inventory.FlagIn_Out = '1'
        var list_Mst_Inventory = await db.Set<Mst_Inventory>()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagIn_Out ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .ToListAsync();

        // Nguồn dòng 54: Mst_InvInType.FlagActive = '1'
        var list_Mst_InvInType = await db.Set<Mst_InvInType>()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .ToListAsync();

        // Nguồn dòng 61-65, 223-228: BusinessType = "DV" and FlagActive = '1'
        var list_Mst_BusinessGroup = await db.Set<Mst_BusinessGroup>()
            .Where(x => EF.Functions.Like((x.BusinessType ?? "").ToLower(), PortQuery.Equal("DV"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .ToListAsync();

        // Nguồn dòng 108: Mst_Customer.FlagActive = '1' and Mst_Customer.FlagSupplier = '1'
        var list_Mst_Customer = await db.Set<Mst_Customer>()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagSupplier ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .ToListAsync();

        // Nguồn dòng 119: Mst_NNT.MSTBUPattern like ... and Mst_NNT.FlagActive = '1'
        var pattern = req?.MSTBUPattern;
        var queryNNT = db.Set<Mst_NNT>()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

        if (!string.IsNullOrEmpty(pattern))
        {
            var p = pattern.Trim().ToLowerInvariant();
            queryNNT = queryNNT.Where(x => EF.Functions.Like((x.MSTBUPattern ?? "").ToLower(), p, PortQuery.Esc));
        }
        var list_Mst_NNT = await queryNNT.ToListAsync();

        // Nguồn dòng 139: Mst_Product.FlagActive = '1'
        var list_Mst_Product = await db.Set<Mst_Product>().ToListAsync();

        var startCount = (page * recordcount).ToString();

        return Ok(new IndexResult
        {
            Success = true,
            RedirectUrl = null,
            PageInfo = pageInfo,
            Lst_Mst_Inventory = list_Mst_Inventory,
            Lst_Mst_InvInType = list_Mst_InvInType,
            Lst_Mst_BusinessGroup = list_Mst_BusinessGroup,
            Lst_MstCustomer = list_Mst_Customer,
            ListMst_NNT = list_Mst_NNT,
            Lst_Mst_Product = list_Mst_Product,
            StartCount = startCount
        });
    }

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

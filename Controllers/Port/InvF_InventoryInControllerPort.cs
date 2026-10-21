using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — InvF_InventoryIn (actions Index, DoSearch, SearchInvInType, SearchInventoryIn).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\InvF_InventoryInController.cs:190-275, 282-320, 323-361, 2846-2964.
/// Action SearchInvInType: dòng 282-320.
///   -> BaseController.cs:2917-2922 (Mst_InvInType_Get_Async)
///   -> BaseController.cs:2883-2915 (Task_Mst_InvInType_Get)
///   -> API WA_Mst_InvInType_Get (MstInvInTypeController.cs:18-89)
///   -> BIZ WAS_Mst_InvInType_Get (MasterData.cs:5632-5728)
///   -> BIZ Mst_InvInType_Get: lọc Mst_InvInType theo FlagActive = '1' và like InvInType / InvInTypeName.
/// Action SearchInventoryIn: dòng 323-361 (#74).
///   -> BaseController.cs:2752-2757 (Mst_Inventory_Get_Async)
///   -> BaseController.cs:2717-2750 (Task_Mst_Inventory_Get)
///   -> API WA_Mst_Inventory_Get (MstInventoryController.cs:18-89)
///   -> BIZ WAS_Mst_Inventory_Get (MasterData.cs:9254-9368)
///   -> BIZ Mst_Inventory_Get: lọc Mst_Inventory theo FlagActive = '1' and FlagIn_Out = '1' và like InvCode / InvName.
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

    public class DoSearchReq
    {
        public string? IF_InvInNo { get; set; }
        public string? CreatedTimeFrom { get; set; }
        public string? CreatedTimeTo { get; set; }
        public string? ApprdTimeFrom { get; set; }
        public string? ApprdTimeTo { get; set; }
        public string? InvoiceDateFrom { get; set; }
        public string? InvoiceDateTo { get; set; }
        public string? InvoiceNo { get; set; }
        public string? InvInType { get; set; }
        public string? CustomerCode { get; set; }
        public string? InvCodeIn { get; set; }
        public string? ProductCode { get; set; }
        public string? OrgId { get; set; }
        public string? RefNo { get; set; }
        public string? ChkPending { get; set; }
        public string? ChkApproved { get; set; }
        public string? ChkCanceled { get; set; }
        public string? BusinessGroupCode { get; set; }
        public int RecordCount { get; set; } = 50;
        public int Page { get; set; } = 0;
        public string? UtcOffset { get; set; }
    }

    public class SearchInvInTypeReq
    {
        /// <summary>Nguồn InvF_InventoryInController.cs:282 — txtsearch.</summary>
        public string? TxtSearch { get; set; }
    }

    public class SearchInvInTypeResult
    {
        public bool Success { get; set; } = true;
        public List<Mst_InvInType> List_Mst_InvInType { get; set; } = new();
    }

    public class SearchInventoryInReq
    {
        /// <summary>Nguồn InvF_InventoryInController.cs:323 — txtsearch.</summary>
        public string? TxtSearch { get; set; }
    }

    public class SearchInventoryInResult
    {
        public bool Success { get; set; } = true;
        public List<Mst_Inventory> List_Mst_Inventory { get; set; } = new();
    }

    /// <summary>
    /// port 1:1 InvF_InventoryInController.cs:282-320 — SearchInvInType(string txtsearch).
    /// Chuỗi gọi: SearchInvInType -> Mst_InvInType_Get_Async -> Task_Mst_InvInType_Get -> WA_Mst_InvInType_Get -> WAS_Mst_InvInType_Get.
    /// Lọc: FlagActive = '1'; nếu txtsearch không rỗng thì AND (InvInType LIKE '%txt%' OR InvInTypeName LIKE '%txt%').
    /// </summary>
    [HttpPost("api/invf_inventoryin/searchinvintype")]
    public async Task<IActionResult> SearchInvInType([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchInvInTypeReq? req)
    {
        try
        {
            // Nguồn dòng 290: var strWhere_Mst_InvInType = "Mst_InvInType.FlagActive = '1'";
            var query = db.Set<Mst_InvInType>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

            var txtsearch = req?.TxtSearch;
            // Nguồn dòng 291-296: if (!string.IsNullOrEmpty(txtsearch)) ... InvInType like '%...%' or InvInTypeName like '%...%'
            if (!string.IsNullOrEmpty(txtsearch))
            {
                var pattern = PortQuery.Contains(txtsearch.Trim().ToLowerInvariant());
                query = query.Where(x => EF.Functions.Like((x.InvInType ?? "").ToLower(), pattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.InvInTypeName ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            // OrderBy theo InvInType để kết quả ổn định và khớp truy vấn danh mục
            var list = await query.OrderBy(x => x.InvInType).ToListAsync();

            return Ok(new SearchInvInTypeResult
            {
                Success = true,
                List_Mst_InvInType = list
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("WAS_Mst_InvInType_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// port 1:1 InvF_InventoryInController.cs:323-361 — SearchInventoryIn(string txtsearch).
    /// Chuỗi gọi: SearchInventoryIn -> Mst_Inventory_Get_Async -> Task_Mst_Inventory_Get -> WA_Mst_Inventory_Get -> WAS_Mst_Inventory_Get.
    /// Lọc: FlagActive = '1' and FlagIn_Out = '1' (nguồn dòng 330: "Mst_Inventory.FlagActive = '1' and Mst_Inventory.FlagIn_Out = '1'");
    /// nếu txtsearch không rỗng thì AND (InvCode LIKE '%txtsearch%' OR InvName LIKE '%txtsearch%') (nguồn dòng 332-337).
    /// Trả về: List_Mst_Inventory (nguồn dòng 350: ViewBag.List_Mst_Inventory = list_Mst_Inventory).
    /// </summary>
    [HttpPost("api/invf_inventoryin/searchinventoryin")]
    public async Task<IActionResult> SearchInventoryIn([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchInventoryInReq? req)
    {
        try
        {
            // Nguồn dòng 330: var strWhere_Mst_Inventory = "Mst_Inventory.FlagActive = '1' and Mst_Inventory.FlagIn_Out = '1'";
            var query = db.Set<Mst_Inventory>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.FlagIn_Out ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

            var txtsearch = req?.TxtSearch;
            // Nguồn dòng 332-337: if (!string.IsNullOrEmpty(txtsearch)) ... InvCode like '%...%' or InvName like '%...%'
            if (!string.IsNullOrEmpty(txtsearch))
            {
                var pattern = PortQuery.Contains(txtsearch.Trim().ToLowerInvariant());
                query = query.Where(x => EF.Functions.Like((x.InvCode ?? "").ToLower(), pattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.InvName ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            // OrderBy theo InvCode để kết quả trả về ổn định
            var list = await query.OrderBy(x => x.InvCode).ToListAsync();

            return Ok(new SearchInventoryInResult
            {
                Success = true,
                List_Mst_Inventory = list
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("WAS_Mst_Inventory_Get", "Exception", ex.Message);
        }
    }

    [HttpPost("api/invf_inventoryin/index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] IndexReq? req)
    {
        var page = req?.Page ?? 0;
        var recordcount = req?.RecordCount ?? 10;

        if (!SysAccessCheckDeny(req, "MNU_INVF_INVENTORYIN"))
        {
            return Ok(new IndexResult
            {
                Success = true,
                RedirectUrl = "/AccessDeny/Index"
            });
        }

        var pageInfo = new PageInfo<InvF_InventoryIn>(
            DataList: new List<InvF_InventoryIn>(),
            ItemCount: 0,
            PageCount: 0,
            PageIndex: page,
            PageSize: recordcount
        );

        return Ok(new IndexResult
        {
            Success = true,
            PageInfo = pageInfo,
            Lst_Mst_Inventory = await db.Set<Mst_Inventory>().Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)).ToListAsync(),
            Lst_Mst_InvInType = await db.Set<Mst_InvInType>().Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)).ToListAsync(),
            Lst_Mst_BusinessGroup = await db.Set<Mst_BusinessGroup>().Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)).ToListAsync(),
            Lst_MstCustomer = await db.Set<Mst_Customer>().Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)).ToListAsync(),
            ListMst_NNT = await db.Set<Mst_NNT>().Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)).ToListAsync(),
            Lst_Mst_Product = await db.Set<Mst_Product>().ToListAsync()
        });
    }

    [HttpPost("api/invf_inventoryin/dosearch")]
    public async Task<IActionResult> DoSearch([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DoSearchReq? req)
    {
        var recordcount = req?.RecordCount ?? 50;
        var page = req?.Page ?? 0;

        var query = from iiif in db.Set<InvF_InventoryIn>()
                    join iiid in db.Set<InvF_InventoryInDtl>() on iiif.IF_InvInNo equals iiid.IF_InvInNo into dtls
                    from iiid in dtls.DefaultIfEmpty()
                    select new { iiif, iiid };

        // 1. Lọc theo IF_InvInNo (dòng 2854-2857)
        if (!string.IsNullOrWhiteSpace(req?.IF_InvInNo))
        {
            var pattern = PortQuery.Contains(req.IF_InvInNo);
            query = query.Where(x => EF.Functions.Like((x.iiif.IF_InvInNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 2. Lọc theo CreatedTimeFrom (dòng 2858-2861)
        if (!string.IsNullOrWhiteSpace(req?.CreatedTimeFrom) && DateTime.TryParse(req.CreatedTimeFrom, out var dtCreatedFrom))
        {
            query = query.Where(x => x.iiif.CreateDTimeUTC >= dtCreatedFrom);
        }

        // 3. Lọc theo CreatedTimeTo (dòng 2862-2865)
        if (!string.IsNullOrWhiteSpace(req?.CreatedTimeTo) && DateTime.TryParse(req.CreatedTimeTo, out var dtCreatedTo))
        {
            query = query.Where(x => x.iiif.CreateDTimeUTC <= dtCreatedTo);
        }

        // 4. Lọc theo ApprdTimeFrom (dòng 2866-2869)
        if (!string.IsNullOrWhiteSpace(req?.ApprdTimeFrom) && DateTime.TryParse(req.ApprdTimeFrom, out var dtApprdFrom))
        {
            query = query.Where(x => x.iiif.ApprDTimeUTC >= dtApprdFrom);
        }

        // 5. Lọc theo ApprdTimeTo (dòng 2870-2873)
        if (!string.IsNullOrWhiteSpace(req?.ApprdTimeTo) && DateTime.TryParse(req.ApprdTimeTo, out var dtApprdTo))
        {
            query = query.Where(x => x.iiif.ApprDTimeUTC <= dtApprdTo);
        }

        // 6. Lọc theo InvoiceDateFrom (dòng 2874-2877)
        if (!string.IsNullOrWhiteSpace(req?.InvoiceDateFrom) && DateTime.TryParse(req.InvoiceDateFrom, out var dtInvoiceFrom))
        {
            query = query.Where(x => x.iiif.InvoiceDate >= dtInvoiceFrom);
        }

        // 7. Lọc theo InvoiceDateTo (dòng 2878-2881)
        if (!string.IsNullOrWhiteSpace(req?.InvoiceDateTo) && DateTime.TryParse(req.InvoiceDateTo, out var dtInvoiceTo))
        {
            query = query.Where(x => x.iiif.InvoiceDate <= dtInvoiceTo);
        }

        // 8. Lọc theo InvoiceNo (dòng 2882-2885)
        if (!string.IsNullOrWhiteSpace(req?.InvoiceNo))
        {
            var pattern = PortQuery.Contains(req.InvoiceNo);
            query = query.Where(x => EF.Functions.Like((x.iiif.InvCodeIn ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 9. Lọc theo InvInType (dòng 2886-2889)
        if (!string.IsNullOrWhiteSpace(req?.InvInType))
        {
            var pattern = PortQuery.Equal(req.InvInType);
            query = query.Where(x => EF.Functions.Like((x.iiif.InvInType ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 10. Lọc theo CustomerCode (dòng 2890-2893)
        if (!string.IsNullOrWhiteSpace(req?.CustomerCode))
        {
            var pattern = PortQuery.Equal(req.CustomerCode);
            query = query.Where(x => EF.Functions.Like((x.iiif.CustomerCode ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 11. Lọc theo InvCodeIn (dòng 2894-2897)
        if (!string.IsNullOrWhiteSpace(req?.InvCodeIn))
        {
            var pattern = PortQuery.Equal(req.InvCodeIn);
            query = query.Where(x => EF.Functions.Like((x.iiif.InvCodeIn ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 12. Lọc theo ProductCode (dòng 2898-2901)
        if (!string.IsNullOrWhiteSpace(req?.ProductCode))
        {
            var pattern = PortQuery.Contains(req.ProductCode);
            query = query.Where(x => x.iiid != null && EF.Functions.Like((x.iiid.ProductCode ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 13. Lọc theo OrgId (dòng 2902-2905)
        if (!string.IsNullOrWhiteSpace(req?.OrgId))
        {
            var pattern = PortQuery.Equal(req.OrgId);
            query = query.Where(x => EF.Functions.Like((x.iiif.OrgID ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 14. Lọc theo RefNo (dòng 2906-2909)
        if (!string.IsNullOrWhiteSpace(req?.RefNo))
        {
            var pattern = PortQuery.Contains(req.RefNo);
            query = query.Where(x => EF.Functions.Like((x.iiif.RefNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 15. Lọc theo BusinessGroupCode (dòng 2910-2925)
        if (!string.IsNullOrWhiteSpace(req?.BusinessGroupCode))
        {
            var bgCodes = req.BusinessGroupCode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                               .Select(s => s.ToLower())
                                               .ToList();
            if (bgCodes.Count > 0)
            {
                query = query.Where(x => bgCodes.Contains((x.iiif.BusinessGroupCode ?? "").ToLower()));
            }
        }

        // 16. Lọc trạng thái chkpending / chkapproved / chkcanceled (dòng 2926-2960)
        var statusList = new List<string>();
        if (req?.ChkPending == "1") statusList.Add("PENDING");
        if (req?.ChkApproved == "1") statusList.Add("APPROVE");
        if (req?.ChkCanceled == "1") statusList.Add("CANCEL");
        if (statusList.Count > 0)
        {
            query = query.Where(x => statusList.Contains(x.iiif.IF_InvInStatus ?? ""));
        }

        // distinct theo phiếu nhập (Form.cs:146840)
        var distinctQuery = query.Select(x => x.iiif).Distinct();

        var itemCount = await distinctQuery.CountAsync();
        var pageCount = recordcount <= 0 ? 0 : (itemCount % recordcount == 0 ? itemCount / recordcount : (itemCount / recordcount + 1));

        var ordered = distinctQuery.OrderByDescending(x => x.CreateDTimeUTC);

        var recordStart = (long)page * recordcount;
        var pagedInvs = (recordcount <= 0 || recordStart < 0)
            ? new List<InvF_InventoryIn>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue))
                           .Take((int)Math.Min(recordcount, int.MaxValue))
                           .ToListAsync();

        // Join các trường hiển thị (Form.cs:146908-146934): Mst_Customer, Mst_InvInType, Mst_Inventory, Sys_User
        var resultList = new List<InvF_InventoryIn>();
        if (pagedInvs.Count > 0)
        {
            var customers = await db.Set<Mst_Customer>().ToListAsync();
            var invTypes = await db.Set<Mst_InvInType>().ToListAsync();
            var inventories = await db.Set<Mst_Inventory>().ToListAsync();
            var users = await db.Set<Sys_User>().ToListAsync();

            foreach (var inv in pagedInvs)
            {
                var cust = customers.FirstOrDefault(c => string.Equals(c.OrgID, inv.OrgID, StringComparison.OrdinalIgnoreCase)
                                                     && string.Equals(c.CustomerCode, inv.CustomerCode, StringComparison.OrdinalIgnoreCase));
                var it = invTypes.FirstOrDefault(t => string.Equals(t.InvInType, inv.InvInType, StringComparison.OrdinalIgnoreCase));
                var iv = inventories.FirstOrDefault(i => string.Equals(i.InvCode, inv.InvCodeIn, StringComparison.OrdinalIgnoreCase));
                var u = users.FirstOrDefault(usr => string.Equals(usr.UserCode, inv.CreateBy, StringComparison.OrdinalIgnoreCase));

                inv.mct_CustomerCode = cust?.CustomerCode;
                inv.mct_CustomerName = cust?.CustomerName;
                inv.miit_InvInType = it?.InvInType;
                inv.miit_InvInTypeName = it?.InvInTypeName;
                inv.mii_InvCode = iv?.InvCode;
                inv.mii_InvName = iv?.InvName;
                inv.su_UserCode_Create = u?.UserCode;
                inv.su_UserName_Create = u?.UserName;
                resultList.Add(inv);
            }
        }

        var pageInfo = new PageInfo<InvF_InventoryIn>(
            DataList: resultList,
            ItemCount: itemCount,
            PageCount: pageCount,
            PageIndex: page,
            PageSize: recordcount
        );

        return Ok(new
        {
            Success = true,
            Data = pageInfo
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

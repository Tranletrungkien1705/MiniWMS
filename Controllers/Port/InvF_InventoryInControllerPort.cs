using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — InvF_InventoryIn (actions Index, DoSearch, SearchInvInType).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\InvF_InventoryInController.cs:190-275, 282-320, 2846-2964.
/// Action SearchInvInType: dòng 282-320.
///   -> BaseController.cs:2917-2922 (Mst_InvInType_Get_Async)
///   -> BaseController.cs:2883-2915 (Task_Mst_InvInType_Get)
///   -> API WA_Mst_InvInType_Get (MstInvInTypeController.cs:18-89)
///   -> BIZ WAS_Mst_InvInType_Get (MasterData.cs:5632-5728)
///   -> BIZ Mst_InvInType_Get: lọc Mst_InvInType theo FlagActive = '1' và like InvInType / InvInTypeName.
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

        var list_Mst_Inventory = await db.Set<Mst_Inventory>()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagIn_Out ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .ToListAsync();

        var list_Mst_InvInType = await db.Set<Mst_InvInType>()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .ToListAsync();

        var list_Mst_BusinessGroup = await db.Set<Mst_BusinessGroup>()
            .Where(x => EF.Functions.Like((x.BusinessType ?? "").ToLower(), PortQuery.Equal("DV"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .ToListAsync();

        var list_Mst_Customer = await db.Set<Mst_Customer>()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagSupplier ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .ToListAsync();

        var pattern = req?.MSTBUPattern;
        var queryNNT = db.Set<Mst_NNT>()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

        if (!string.IsNullOrEmpty(pattern))
        {
            var p = pattern.Trim().ToLowerInvariant();
            queryNNT = queryNNT.Where(x => EF.Functions.Like((x.MSTBUPattern ?? "").ToLower(), p, PortQuery.Esc));
        }
        var list_Mst_NNT = await queryNNT.ToListAsync();

        var list_Mst_Product = await db.Set<Mst_Product>().ToListAsync();

        var startCount = "0";

        return Ok(new IndexResult
        {
            Success = true,
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

    [HttpPost("api/invf_inventoryin/dosearch")]
    public async Task<IActionResult> DoSearch([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DoSearchReq? req)
    {
        var page = req?.Page ?? 0;
        var recordcount = req?.RecordCount ?? 50;

        // Base query theo Form.cs:146816-146900
        var query = from iiif in db.Set<InvF_InventoryIn>()
                    join iiid in db.Set<InvF_InventoryInDtl>() on iiif.IF_InvInNo equals iiid.IF_InvInNo into dtls
                    from dtl in dtls.DefaultIfEmpty()
                    select new { iiif, dtl };

        // 1. Lọc IF_InvInNo (dòng 2855)
        if (!string.IsNullOrWhiteSpace(req?.IF_InvInNo))
        {
            var p = PortQuery.Contains(req.IF_InvInNo.Trim().ToLowerInvariant());
            query = query.Where(x => EF.Functions.Like((x.iiif.IF_InvInNo ?? "").ToLower(), p, PortQuery.Esc));
        }

        // 2. Lọc CreatedTimeFrom / CreatedTimeTo (dòng 2860-2866)
        if (!string.IsNullOrWhiteSpace(req?.CreatedTimeFrom) && DateTime.TryParse(req.CreatedTimeFrom, out var dtCreatedFrom))
        {
            query = query.Where(x => x.iiif.CreateDTimeUTC >= dtCreatedFrom);
        }
        if (!string.IsNullOrWhiteSpace(req?.CreatedTimeTo) && DateTime.TryParse(req.CreatedTimeTo, out var dtCreatedTo))
        {
            query = query.Where(x => x.iiif.CreateDTimeUTC <= dtCreatedTo);
        }

        // 3. Lọc ApprdTimeFrom / ApprdTimeTo (dòng 2871-2877)
        if (!string.IsNullOrWhiteSpace(req?.ApprdTimeFrom) && DateTime.TryParse(req.ApprdTimeFrom, out var dtApprdFrom))
        {
            query = query.Where(x => x.iiif.ApprDTimeUTC >= dtApprdFrom);
        }
        if (!string.IsNullOrWhiteSpace(req?.ApprdTimeTo) && DateTime.TryParse(req.ApprdTimeTo, out var dtApprdTo))
        {
            query = query.Where(x => x.iiif.ApprDTimeUTC <= dtApprdTo);
        }

        // 4. Lọc InvoiceDateFrom / InvoiceDateTo (dòng 2882-2888)
        if (!string.IsNullOrWhiteSpace(req?.InvoiceDateFrom) && DateTime.TryParse(req.InvoiceDateFrom, out var dtInvDateFrom))
        {
            query = query.Where(x => x.iiif.InvoiceDate >= dtInvDateFrom);
        }
        if (!string.IsNullOrWhiteSpace(req?.InvoiceDateTo) && DateTime.TryParse(req.InvoiceDateTo, out var dtInvDateTo))
        {
            query = query.Where(x => x.iiif.InvoiceDate <= dtInvDateTo);
        }

        // 5. Lọc InvoiceNo (dòng 2893)
        if (!string.IsNullOrWhiteSpace(req?.InvoiceNo))
        {
            var p = PortQuery.Contains(req.InvoiceNo.Trim().ToLowerInvariant());
            query = query.Where(x => EF.Functions.Like((x.iiif.InvoiceNo ?? "").ToLower(), p, PortQuery.Esc));
        }

        // 6. Lọc InvInType (dòng 2898)
        if (!string.IsNullOrWhiteSpace(req?.InvInType))
        {
            var p = PortQuery.Equal(req.InvInType.Trim().ToLowerInvariant());
            query = query.Where(x => EF.Functions.Like((x.iiif.InvInType ?? "").ToLower(), p, PortQuery.Esc));
        }

        // 7. Lọc CustomerCode (dòng 2903)
        if (!string.IsNullOrWhiteSpace(req?.CustomerCode))
        {
            var p = PortQuery.Equal(req.CustomerCode.Trim().ToLowerInvariant());
            query = query.Where(x => EF.Functions.Like((x.iiif.CustomerCode ?? "").ToLower(), p, PortQuery.Esc));
        }

        // 8. Lọc InvCodeIn (dòng 2908)
        if (!string.IsNullOrWhiteSpace(req?.InvCodeIn))
        {
            var p = PortQuery.Equal(req.InvCodeIn.Trim().ToLowerInvariant());
            query = query.Where(x => EF.Functions.Like((x.iiif.InvCodeIn ?? "").ToLower(), p, PortQuery.Esc));
        }

        // 9. Lọc ProductCode (dòng 2913) - lọc trên chi tiết
        if (!string.IsNullOrWhiteSpace(req?.ProductCode))
        {
            var p = PortQuery.Contains(req.ProductCode.Trim().ToLowerInvariant());
            query = query.Where(x => x.dtl != null && EF.Functions.Like((x.dtl.ProductCode ?? "").ToLower(), p, PortQuery.Esc));
        }

        // 10. Lọc OrgId (dòng 2918)
        if (!string.IsNullOrWhiteSpace(req?.OrgId))
        {
            var p = PortQuery.Equal(req.OrgId.Trim().ToLowerInvariant());
            query = query.Where(x => EF.Functions.Like((x.iiif.OrgID ?? "").ToLower(), p, PortQuery.Esc));
        }

        // 11. Lọc RefNo (dòng 2923)
        if (!string.IsNullOrWhiteSpace(req?.RefNo))
        {
            var p = PortQuery.Contains(req.RefNo.Trim().ToLowerInvariant());
            query = query.Where(x => EF.Functions.Like((x.iiif.RefNo ?? "").ToLower(), p, PortQuery.Esc));
        }

        // 12. Lọc BusinessGroupCode (dòng 2963)
        if (!string.IsNullOrWhiteSpace(req?.BusinessGroupCode))
        {
            var bgCodes = req.BusinessGroupCode.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(s => s.Trim().ToLowerInvariant())
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — InvF_InventoryIn (actions Index, DoSearch).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\InvF_InventoryInController.cs:190-275, 2846-2964.
/// API & BIZ: InvFInventoryInController.cs:20-89 -> Form.cs:146769-147485.
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

    [HttpPost("api/invf_inventoryin/dosearch")]
    public async Task<IActionResult> DoSearch([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DoSearchReq? req)
    {
        var recordcount = req?.RecordCount ?? 50;
        var page = req?.Page ?? 0;

        // Tính offset giờ địa phương nếu có
        var utcOffsetHours = 0.0;
        if (!string.IsNullOrWhiteSpace(req?.UtcOffset))
        {
            double.TryParse(req.UtcOffset.Trim(), out utcOffsetHours);
        }

        // Khởi tạo truy vấn chính join theo Form.cs:146844-146855:
        // InvF_InventoryIn iiif
        // inner join InvF_InventoryInDtl iiifdt on iiif.IF_InvInNo = iiifdt.IF_InvInNo
        // left join Mst_Product mp on iiif.OrgID = mp.OrgID and iiifdt.ProductCode = mp.ProductCode
        var query = from iiif in db.Set<InvF_InventoryIn>()
                    join iiifdt in db.Set<InvF_InventoryInDtl>() on iiif.IF_InvInNo equals iiifdt.IF_InvInNo
                    join mp in db.Set<Mst_Product>() on new { OrgID = iiif.OrgID ?? "", iiifdt.ProductCode } equals new { mp.OrgID, mp.ProductCode } into g_mp
                    from mp in g_mp.DefaultIfEmpty()
                    select new { iiif, iiifdt, mp };

        // 1. Lọc IF_InvInNo: like '%x%' (InvF_InventoryInController.cs:2856)
        if (!string.IsNullOrWhiteSpace(req?.IF_InvInNo))
        {
            query = query.Where(x => EF.Functions.Like((x.iiif.IF_InvInNo ?? "").ToLower(), PortQuery.Contains(req.IF_InvInNo), PortQuery.Esc));
        }

        // 2. Lọc CreatedTimeFrom: >= date 00:00:00 chuyển sang UTC (dòng 2862)
        if (!string.IsNullOrWhiteSpace(req?.CreatedTimeFrom))
        {
            if (DateTime.TryParse(req.CreatedTimeFrom.Trim(), out var dtFrom))
            {
                var dtUtc = dtFrom.Date.AddHours(-utcOffsetHours);
                query = query.Where(x => x.iiif.CreateDTimeUTC >= dtUtc);
            }
        }

        // 3. Lọc CreatedTimeTo: <= date 23:59:59 chuyển sang UTC (dòng 2868)
        if (!string.IsNullOrWhiteSpace(req?.CreatedTimeTo))
        {
            if (DateTime.TryParse(req.CreatedTimeTo.Trim(), out var dtTo))
            {
                var dtUtc = dtTo.Date.AddDays(1).AddTicks(-1).AddHours(-utcOffsetHours);
                query = query.Where(x => x.iiif.CreateDTimeUTC <= dtUtc);
            }
        }

        // 4. Lọc ApprdTimeFrom: >= date 00:00:00 (dòng 2874)
        if (!string.IsNullOrWhiteSpace(req?.ApprdTimeFrom))
        {
            if (DateTime.TryParse(req.ApprdTimeFrom.Trim(), out var dtApprFrom))
            {
                var dtUtc = dtApprFrom.Date.AddHours(-utcOffsetHours);
                query = query.Where(x => x.iiif.ApprDTimeUTC >= dtUtc);
            }
        }

        // 5. Lọc ApprdTimeTo: <= date 23:59:59 (dòng 2880)
        if (!string.IsNullOrWhiteSpace(req?.ApprdTimeTo))
        {
            if (DateTime.TryParse(req.ApprdTimeTo.Trim(), out var dtApprTo))
            {
                var dtUtc = dtApprTo.Date.AddDays(1).AddTicks(-1).AddHours(-utcOffsetHours);
                query = query.Where(x => x.iiif.ApprDTimeUTC <= dtUtc);
            }
        }

        // 6. Lọc InvoiceDateFrom (dòng 2886)
        if (!string.IsNullOrWhiteSpace(req?.InvoiceDateFrom))
        {
            if (DateTime.TryParse(req.InvoiceDateFrom.Trim(), out var dtInvFrom))
            {
                var dtUtc = dtInvFrom.Date.AddHours(-utcOffsetHours);
                query = query.Where(x => x.iiif.InvoiceDate >= dtUtc);
            }
        }

        // 7. Lọc InvoiceDateTo (dòng 2892)
        if (!string.IsNullOrWhiteSpace(req?.InvoiceDateTo))
        {
            if (DateTime.TryParse(req.InvoiceDateTo.Trim(), out var dtInvTo))
            {
                var dtUtc = dtInvTo.Date.AddDays(1).AddTicks(-1).AddHours(-utcOffsetHours);
                query = query.Where(x => x.iiif.InvoiceDate <= dtUtc);
            }
        }

        // 8. Lọc InvoiceNo: = (dòng 2896)
        if (!string.IsNullOrWhiteSpace(req?.InvoiceNo))
        {
            query = query.Where(x => EF.Functions.Like((x.iiif.InvoiceNo ?? "").ToLower(), PortQuery.Equal(req.InvoiceNo), PortQuery.Esc));
        }

        // 9. Lọc InvInType: = (dòng 2900)
        if (!string.IsNullOrWhiteSpace(req?.InvInType))
        {
            query = query.Where(x => EF.Functions.Like((x.iiif.InvInType ?? "").ToLower(), PortQuery.Equal(req.InvInType), PortQuery.Esc));
        }

        // 10. Lọc InvCodeIn: = (dòng 2904)
        if (!string.IsNullOrWhiteSpace(req?.InvCodeIn))
        {
            query = query.Where(x => EF.Functions.Like((x.iiif.InvCodeIn ?? "").ToLower(), PortQuery.Equal(req.InvCodeIn), PortQuery.Esc));
        }

        // 11. Lọc CustomerCode: = (dòng 2908)
        if (!string.IsNullOrWhiteSpace(req?.CustomerCode))
        {
            query = query.Where(x => EF.Functions.Like((x.iiif.CustomerCode ?? "").ToLower(), PortQuery.Equal(req.CustomerCode), PortQuery.Esc));
        }

        // 12. Lọc ProductCode: like '%x%' trên Mst_Product (dòng 2912)
        if (!string.IsNullOrWhiteSpace(req?.ProductCode))
        {
            query = query.Where(x => EF.Functions.Like((x.mp.ProductCode ?? x.iiifdt.ProductCode ?? "").ToLower(), PortQuery.Contains(req.ProductCode), PortQuery.Esc));
        }

        // 13. Lọc OrgId: = (dòng 2916)
        if (!string.IsNullOrWhiteSpace(req?.OrgId))
        {
            query = query.Where(x => EF.Functions.Like((x.iiif.OrgID ?? "").ToLower(), PortQuery.Equal(req.OrgId), PortQuery.Esc));
        }

        // 14. Lọc RefNo: like '%x%' (dòng 2920)
        if (!string.IsNullOrWhiteSpace(req?.RefNo))
        {
            query = query.Where(x => EF.Functions.Like((x.iiif.RefNo ?? "").ToLower(), PortQuery.Contains(req.RefNo), PortQuery.Esc));
        }

        // 15. Lọc BusinessGroupCode: in (dòng 2924)
        if (!string.IsNullOrWhiteSpace(req?.BusinessGroupCode))
        {
            var bgCodes = req.BusinessGroupCode.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().ToLowerInvariant())
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
            var invNos = pagedInvs.Select(x => x.IF_InvInNo).ToList();
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

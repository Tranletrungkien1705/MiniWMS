using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — InvF_InventoryIn (actions Index, DoSearch, SearchInvInType, SearchInventoryIn, ShowInvCodeInActual, ShowLo).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\InvF_InventoryInController.cs:190-275, 282-320, 323-361, 365-411, 415-461, 2846-2964.
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
/// Action ShowInvCodeInActual: dòng 365-411 (#75).
///   -> BaseController.cs:2752-2757 (Mst_Inventory_Get_Async)
///   -> BaseController.cs:2717-2750 (Task_Mst_Inventory_Get)
///   -> API WA_Mst_Inventory_Get (MstInventoryController.cs:18-89)
///   -> BIZ WAS_Mst_Inventory_Get (MasterData.cs:9254-9368)
///   -> BIZ Mst_Inventory_Get: lọc Mst_Inventory theo FlagActive = '1' (dòng 371) và like InvBUPattern (dòng 373-377).
/// Action ShowLo: dòng 415-461 (#76).
///   -> BaseController.cs:2752-2757 (Mst_Inventory_Get_Async)
///   -> BaseController.cs:2717-2750 (Task_Mst_Inventory_Get)
///   -> API WA_Mst_Inventory_Get (MstInventoryController.cs:18-89)
///   -> BIZ WAS_Mst_Inventory_Get (MasterData.cs:9254-9368)
///   -> BIZ Mst_Inventory_Get: lọc Mst_Inventory theo FlagActive = '1' (dòng 422) và like InvBUPattern (dòng 424-428).
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

    public class ShowInvCodeInActualReq
    {
        /// <summary>Nguồn InvF_InventoryInController.cs:365 — productcode.</summary>
        public string? ProductCode { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:365 — productcodeuser.</summary>
        public string? ProductCodeUser { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:365 — productcodebase.</summary>
        public string? ProductCodeBase { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:365 — productname.</summary>
        public string? ProductName { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:365 — invbupattern.</summary>
        public string? InvBUPattern { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:365 — listdetail (JSON mảng InvF_InventoryInDtl hoặc truyền đối tượng).</summary>
        public string? ListDetail { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:365 — ifinvinstatus.</summary>
        public string? IF_InvInStatus { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:365 — viewtype.</summary>
        public string? ViewType { get; set; }
    }

    public class ShowInvCodeInActualResult
    {
        public bool Success { get; set; } = true;
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public List<Mst_Inventory> ListMst_Inventory { get; set; } = new();
        public List<InvF_InventoryInDtl> ListInvF_InventoryInDtl { get; set; } = new();
        public string? IF_InvInStatus { get; set; }
        public string? ViewType { get; set; }
    }

    public class ShowLoReq
    {
        /// <summary>Nguồn InvF_InventoryInController.cs:415 — productcode.</summary>
        public string? ProductCode { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:415 — productcodeuser.</summary>
        public string? ProductCodeUser { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:415 — productname.</summary>
        public string? ProductName { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:415 — invbupattern.</summary>
        public string? InvBUPattern { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:415 — listlot (JSON mảng InvF_InventoryInInstLot).</summary>
        public string? ListLot { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:415 — ifinvinstatus.</summary>
        public string? IF_InvInStatus { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:415 — viewtype.</summary>
        public string? ViewType { get; set; }
    }

    public class ShowLoResult
    {
        public bool Success { get; set; } = true;
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public List<Mst_Inventory> ListMst_Inventory { get; set; } = new();
        public List<InvF_InventoryInInstLot> ListInvF_InventoryInInstLot { get; set; } = new();
        public string? IF_InvInStatus { get; set; }
        public string? ViewType { get; set; }
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
    /// Lọc: FlagActive = '1' and FlagIn_Out = '1'; nếu txtsearch không rỗng thì AND (InvCode LIKE '%txt%' OR InvName LIKE '%txt%').
    /// </summary>
    [HttpPost("api/invf_inventoryin/searchinventoryin")]
    public async Task<IActionResult> SearchInventoryIn([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchInventoryInReq? req)
    {
        try
        {
            // Nguồn dòng 331: var strWhere_Mst_Inventory = "Mst_Inventory.FlagActive = '1' and Mst_Inventory.FlagIn_Out = '1'";
            var query = db.Set<Mst_Inventory>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.FlagIn_Out ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

            var txtsearch = req?.TxtSearch;
            // Nguồn dòng 333-338: if (!string.IsNullOrEmpty(txtsearch)) ... InvCode like '%...%' or InvName like '%...%'
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

    /// <summary>
    /// port 1:1 InvF_InventoryInController.cs:365-411 — ShowInvCodeInActual.
    /// Chuỗi gọi: ShowInvCodeInActual -> Mst_Inventory_Get_Async -> Task_Mst_Inventory_Get -> WA_Mst_Inventory_Get -> WAS_Mst_Inventory_Get.
    /// Lọc Mst_Inventory: FlagActive = '1' (dòng 371); nếu invbupattern không rỗng thì AND Mst_Inventory.InvBUPattern like 'invbupattern' (dòng 373-377).
    /// ViewBag.ProductName = productcodeuser + " - " + productname (dòng 391).
    /// ViewBag.ListInvF_InventoryInDtl = Deserialize<List<InvF_InventoryInDtl>>(listdetail) (dòng 395-399).
    /// </summary>
    [HttpPost("api/invf_inventoryin/showinvcodeinactual")]
    public async Task<IActionResult> ShowInvCodeInActual([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ShowInvCodeInActualReq? req)
    {
        try
        {
            // Nguồn dòng 371: var strWhere_Mst_Inventory = "Mst_Inventory.FlagActive = '1'";
            var query = db.Set<Mst_Inventory>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

            var invbupattern = req?.InvBUPattern;
            // Nguồn dòng 373-377: if (!string.IsNullOrEmpty(invbupattern)) strWhere += " Mst_Inventory.InvBUPattern like '" + invbupattern + "'";
            if (!string.IsNullOrEmpty(invbupattern))
            {
                // Nguồn dùng like với chuỗi invbupattern nguyên vẹn (có thể chứa wildcard % hoặc không)
                var pattern = invbupattern.Trim().ToLowerInvariant().Replace(@"\", @"\\");
                query = query.Where(x => EF.Functions.Like((x.InvBUPattern ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            var listMstInventory = await query.OrderBy(x => x.InvCode).ToListAsync();

            // Nguồn dòng 391: ViewBag.ProductName = productcodeuser + " - " + productname;
            var productCodeUser = req?.ProductCodeUser ?? "";
            var productNameRaw = req?.ProductName ?? "";
            var productDisplayName = string.IsNullOrEmpty(productCodeUser) && string.IsNullOrEmpty(productNameRaw)
                ? null
                : $"{productCodeUser} - {productNameRaw}";

            // Nguồn dòng 394-399: parse listdetail JSON nếu có
            var lstDetail = new List<InvF_InventoryInDtl>();
            if (!string.IsNullOrEmpty(req?.ListDetail))
            {
                try
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<InvF_InventoryInDtl>>(
                        req.ListDetail,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    if (parsed != null)
                    {
                        lstDetail.AddRange(parsed);
                    }
                }
                catch
                {
                    // Nguồn bắt lỗi JsonParseException trong khối chung, trả kết quả lỗi
                    throw;
                }
            }

            return Ok(new ShowInvCodeInActualResult
            {
                Success = true,
                ProductCode = req?.ProductCode,
                ProductName = productDisplayName,
                ListMst_Inventory = listMstInventory,
                ListInvF_InventoryInDtl = lstDetail,
                IF_InvInStatus = req?.IF_InvInStatus,
                ViewType = req?.ViewType
            });
        }
        catch (Exception ex)
        {
            // Nguồn dòng 405-410: resultEntry.SetFailed().AddException(this, ex)
            return PortResult.Fail("WAS_Mst_Inventory_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// port 1:1 InvF_InventoryInController.cs:415-461 — ShowLo.
    /// Chuỗi gọi: ShowLo -> Mst_Inventory_Get_Async -> Task_Mst_Inventory_Get -> WA_Mst_Inventory_Get -> WAS_Mst_Inventory_Get.
    /// Lọc Mst_Inventory: FlagActive = '1' (dòng 422); nếu invBUPattern không rỗng thì AND Mst_Inventory.InvBUPattern like 'invBUPattern' (dòng 424-428).
    /// ViewBag.ProductCode = productcode (dòng 441).
    /// ViewBag.ProductName = productcodeuser + " - " + productname (dòng 442).
    /// ViewBag.ListMst_Inventory = list_Mst_Inventory (dòng 443).
    /// ViewBag.ListInvF_InventoryInInstLot = Deserialize<List<InvF_InventoryInInstLot>>(listlot) (dòng 445-450).
    /// ViewBag.IF_InvInStatus = ifinvinstatus (dòng 451).
    /// ViewBag.ViewType = viewtype (dòng 452).
    /// </summary>
    [HttpPost("api/invf_inventoryin/showlo")]
    public async Task<IActionResult> ShowLo([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ShowLoReq? req)
    {
        try
        {
            // Nguồn dòng 422: var strWhere_Mst_Inventory = "Mst_Inventory.FlagActive = '1'";
            var query = db.Set<Mst_Inventory>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

            var invbupattern = req?.InvBUPattern;
            // Nguồn dòng 424-428: if (!string.IsNullOrEmpty(invBUPattern)) strWhere += " Mst_Inventory.InvBUPattern like '" + invBUPattern + "'";
            if (!string.IsNullOrEmpty(invbupattern))
            {
                // Nguồn dùng like với chuỗi invBUPattern nguyên vẹn (có thể chứa wildcard % hoặc không)
                var pattern = invbupattern.Trim().ToLowerInvariant().Replace(@"\", @"\\");
                query = query.Where(x => EF.Functions.Like((x.InvBUPattern ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            var listMstInventory = await query.OrderBy(x => x.InvCode).ToListAsync();

            // Nguồn dòng 442: ViewBag.ProductName = productcodeuser + " - " + productname;
            var productCodeUser = req?.ProductCodeUser ?? "";
            var productNameRaw = req?.ProductName ?? "";
            var productDisplayName = string.IsNullOrEmpty(productCodeUser) && string.IsNullOrEmpty(productNameRaw)
                ? null
                : $"{productCodeUser} - {productNameRaw}";

            // Nguồn dòng 445-450: parse listlot JSON nếu có
            var lstLot = new List<InvF_InventoryInInstLot>();
            if (!string.IsNullOrEmpty(req?.ListLot))
            {
                try
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<InvF_InventoryInInstLot>>(
                        req.ListLot,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    if (parsed != null)
                    {
                        lstLot.AddRange(parsed);
                    }
                }
                catch
                {
                    // Nguồn bắt lỗi JsonParseException trong khối chung, trả kết quả lỗi
                    throw;
                }
            }

            return Ok(new ShowLoResult
            {
                Success = true,
                ProductCode = req?.ProductCode,
                ProductName = productDisplayName,
                ListMst_Inventory = listMstInventory,
                ListInvF_InventoryInInstLot = lstLot,
                IF_InvInStatus = req?.IF_InvInStatus,
                ViewType = req?.ViewType
            });
        }
        catch (Exception ex)
        {
            // Nguồn dòng 455-458: resultEntry.SetFailed().AddException(this, ex)
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
        var page = req?.Page ?? 0;
        var recordcount = req?.RecordCount ?? 50;

        var query = db.Set<InvF_InventoryIn>().AsQueryable();

        // 1. IF_InvInNo
        if (!string.IsNullOrEmpty(req?.IF_InvInNo))
        {
            var pattern = PortQuery.Contains(req.IF_InvInNo.Trim().ToLowerInvariant());
            query = query.Where(x => EF.Functions.Like((x.IF_InvInNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 2. InvInType
        if (!string.IsNullOrEmpty(req?.InvInType))
        {
            var v = req.InvInType.Trim().ToLowerInvariant();
            query = query.Where(x => EF.Functions.Like((x.InvInType ?? "").ToLower(), PortQuery.Equal(v), PortQuery.Esc));
        }

        // 3. CustomerCode
        if (!string.IsNullOrEmpty(req?.CustomerCode))
        {
            var v = req.CustomerCode.Trim().ToLowerInvariant();
            query = query.Where(x => EF.Functions.Like((x.CustomerCode ?? "").ToLower(), PortQuery.Equal(v), PortQuery.Esc));
        }

        // 4. InvCodeIn
        if (!string.IsNullOrEmpty(req?.InvCodeIn))
        {
            var v = req.InvCodeIn.Trim().ToLowerInvariant();
            query = query.Where(x => EF.Functions.Like((x.InvCodeIn ?? "").ToLower(), PortQuery.Equal(v), PortQuery.Esc));
        }

        // 5. InvoiceNo
        if (!string.IsNullOrEmpty(req?.InvoiceNo))
        {
            var pattern = PortQuery.Contains(req.InvoiceNo.Trim().ToLowerInvariant());
            query = query.Where(x => EF.Functions.Like((x.InvoiceNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 6. RefNo
        if (!string.IsNullOrEmpty(req?.RefNo))
        {
            var pattern = PortQuery.Contains(req.RefNo.Trim().ToLowerInvariant());
            query = query.Where(x => EF.Functions.Like((x.RefNo ?? "").ToLower(), pattern, PortQuery.Esc));
        }

        // 7. BusinessGroupCode
        if (!string.IsNullOrEmpty(req?.BusinessGroupCode))
        {
            var v = req.BusinessGroupCode.Trim().ToLowerInvariant();
            query = query.Where(x => EF.Functions.Like((x.BusinessGroupCode ?? "").ToLower(), PortQuery.Equal(v), PortQuery.Esc));
        }

        // 8. Trạng thái (ChkPending, ChkApproved, ChkCanceled)
        var statusList = new List<string>();
        if (string.Equals(req?.ChkPending, "1", StringComparison.OrdinalIgnoreCase)) statusList.Add("PENDING");
        if (string.Equals(req?.ChkApproved, "1", StringComparison.OrdinalIgnoreCase)) statusList.Add("APPROVE");
        if (string.Equals(req?.ChkCanceled, "1", StringComparison.OrdinalIgnoreCase)) statusList.Add("CANCEL");

        if (statusList.Count > 0)
        {
            query = query.Where(x => statusList.Contains(x.IF_InvInStatus ?? ""));
        }

        // 9. Khoảng thời gian tạo CreatedTimeFrom / CreatedTimeTo
        if (DateTime.TryParse(req?.CreatedTimeFrom, out var dtFrom))
        {
            query = query.Where(x => x.CreateDTimeUTC >= dtFrom);
        }
        if (DateTime.TryParse(req?.CreatedTimeTo, out var dtTo))
        {
            query = query.Where(x => x.CreateDTimeUTC <= dtTo);
        }

        // 10. Khoảng thời gian duyệt ApprdTimeFrom / ApprdTimeTo
        if (DateTime.TryParse(req?.ApprdTimeFrom, out var dtApprFrom))
        {
            query = query.Where(x => x.ApprDTimeUTC >= dtApprFrom);
        }
        if (DateTime.TryParse(req?.ApprdTimeTo, out var dtApprTo))
        {
            query = query.Where(x => x.ApprDTimeUTC <= dtApprTo);
        }

        // 11. Khoảng ngày hóa đơn InvoiceDateFrom / InvoiceDateTo
        if (DateTime.TryParse(req?.InvoiceDateFrom, out var dtInvFrom))
        {
            query = query.Where(x => x.InvoiceDate >= dtInvFrom);
        }
        if (DateTime.TryParse(req?.InvoiceDateTo, out var dtInvTo))
        {
            query = query.Where(x => x.InvoiceDate <= dtInvTo);
        }

        var itemCount = await query.CountAsync();
        var pageCount = recordcount > 0 ? (int)Math.Ceiling((double)itemCount / recordcount) : 0;
        var recordStart = page * recordcount;

        var ordered = query.OrderByDescending(x => x.CreateDTimeUTC).ThenByDescending(x => x.IF_InvInNo);
        var pagedItems = (recordcount <= 0 || recordStart < 0)
            ? new List<InvF_InventoryIn>()
            : await ordered.Skip((int)Math.Min(recordStart, int.MaxValue)).Take((int)Math.Min(recordcount, int.MaxValue)).ToListAsync();

        // Join thêm thông tin bổ sung: Khách hàng, Loại nhập, Kho, Người tạo
        var resultList = new List<InvF_InventoryIn>();
        if (pagedItems.Count > 0)
        {
            var customerCodes = pagedItems.Select(x => x.CustomerCode).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            var invTypesCodes = pagedItems.Select(x => x.InvInType).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            var invCodes = pagedItems.Select(x => x.InvCodeIn).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            var userCodes = pagedItems.Select(x => x.CreateBy).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

            var customers = await db.Set<Mst_Customer>()
                .Where(x => customerCodes.Contains(x.CustomerCode))
                .ToListAsync();

            var invTypes = await db.Set<Mst_InvInType>()
                .Where(x => invTypesCodes.Contains(x.InvInType))
                .ToListAsync();

            var inventories = await db.Set<Mst_Inventory>()
                .Where(x => invCodes.Contains(x.InvCode))
                .ToListAsync();

            var users = await db.Set<Sys_User>()
                .Where(x => userCodes.Contains(x.UserCode))
                .ToListAsync();

            foreach (var inv in pagedItems)
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

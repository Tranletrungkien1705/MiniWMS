using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — InvF_InventoryIn (actions Index, DoSearch, SearchInvInType, SearchInventoryIn, ShowInvCodeInActual, ShowLo, ShowSerial, SearchCustomer, Create).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\InvF_InventoryInController.cs:190-275, 282-320, 323-361, 365-411, 415-461, 465-512, 566-623, 726-888, 2846-2964.
/// Action SearchInvInType: dòng 282-320 -> BaseController.cs:2917-2922 -> API WA_Mst_InvInType_Get -> BIZ WAS_Mst_InvInType_Get.
/// Action SearchInventoryIn: dòng 323-361 (#74) -> BaseController.cs:2752-2757 -> API WA_Mst_Inventory_Get -> BIZ WAS_Mst_Inventory_Get.
/// Action ShowInvCodeInActual: dòng 365-411 (#75) -> Mst_Inventory_Get (FlagActive='1', like InvBUPattern).
/// Action ShowLo: dòng 415-461 (#76) -> Mst_Inventory_Get (FlagActive='1', like InvBUPattern).
/// Action ShowSerial: dòng 465-512 (#77) -> Mst_Inventory_Get (FlagActive='1', like InvBUPattern).
/// Action SearchCustomer: dòng 566-623 (#79) -> List_Mst_Customer_Async (FlagActive='1' AND FlagSupplier='1').
/// Action Create: dòng 726-888 (#82).
///   -> SysAccessCheckDeny(UserState, "MNU_INVF_INVENTORYIN_CREATE") — false ⇒ RedirectToAction("Index","AccessDeny") (dòng 728-732).
///   -> SeqNo(new Seq_Common { SequenceType = SeqType.IFInvInNo, Param_Prefix="", Param_Postfix="" }) (dòng 738-743):
///        BaseController.cs:2214-2237 → Seq_CommonService.WA_Seq_Common_Get (API SeqController.cs:164-233)
///        → biz WAS_Seq_Common_Get (Seq.cs:2642-2719) → Seq_Common_Get (Seq.cs:126-220)
///        → Seq_Common_MyGet (Seq.cs:42-124, htMap IFInvInNo: "Seq_IF_InvInNo", "{0}PN.{3}.{1:00000}{2}", 100000)
///        → Seq_Common_Raw (Seq.cs:28-41): insert/delete trên Seq_IF_InvInNo để lấy AutoId (commit riêng).
///   -> 5 truy vấn song song (Task.WhenAll dòng 832):
///        Mst_InvInType: FlagActive='1' (dòng 748);
///        Mst_Inventory: FlagActive='1' AND FlagIn_Out='1' (dòng 752);
///        Mst_Customer: FlagActive='1' AND FlagSupplier='1' (dòng 771);
///        Mst_VATRate: FlagActive='1' (dòng 792);
///        Mst_BusinessGroup: BusinessType='DV' AND FlagActive='1' (dòng 802-807 + strWhereClause 3197-3213).
///   -> return View(new RT_InvF_InventoryIn { skeleton PENDING, 4 list con RỖNG }) (dòng 871-886).
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

    public class SearchCustomerReq
    {
        /// <summary>Nguồn InvF_InventoryInController.cs:566 — txtsearch.</summary>
        public string? TxtSearch { get; set; }
    }

    public class SearchCustomerResult
    {
        public bool Success { get; set; } = true;
        public List<Mst_Customer> List_Mst_Customer { get; set; } = new();
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

    public class ShowSerialReq
    {
        /// <summary>Nguồn InvF_InventoryInController.cs:465 — productcode.</summary>
        public string? ProductCode { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:465 — productcodeuser.</summary>
        public string? ProductCodeUser { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:465 — productname.</summary>
        public string? ProductName { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:465 — invbupattern.</summary>
        public string? InvBUPattern { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:465 — listserial (JSON mảng InvF_InventoryInInstSerial).</summary>
        public string? ListSerial { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:465 — ifinvinstatus.</summary>
        public string? IF_InvInStatus { get; set; }
        /// <summary>Nguồn InvF_InventoryInController.cs:465 — viewtype.</summary>
        public string? ViewType { get; set; }
    }

    public class ShowSerialResult
    {
        public bool Success { get; set; } = true;
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public List<Mst_Inventory> ListMst_Inventory { get; set; } = new();
        public List<InvF_InventoryInInstSerial> ListInvF_InventoryInInstSerial { get; set; } = new();
        public string? IF_InvInStatus { get; set; }
        public string? ViewType { get; set; }
    }

    /// <summary>
    /// port 1:1 InvF_InventoryInController.cs:726-888 — Create().
    /// GAP: đích không có phiên ⇒ nhận IsSysAdmin + ListSysAccess (cho SysAccessCheckDeny), OrgID + NetworkID
    /// (cho skeleton) qua body tuỳ chọn. Nguồn đọc từ UserState (dòng 728, 735-736).
    /// </summary>
    public class CreateReq
    {
        /// <summary>Nguồn BaseController.cs:3250 — userState.IsSysAdmin.</summary>
        public bool? IsSysAdmin { get; set; }
        /// <summary>Nguồn BaseController.cs:3258-3260 — userState.ListSysAccess[].ObjectCode.</summary>
        public List<string>? ListSysAccess { get; set; }
        /// <summary>Nguồn dòng 735 — UserState.Mst_NNT.OrgID.</summary>
        public string? OrgId { get; set; }
        /// <summary>Nguồn dòng 736 — UserState.Mst_NNT.NetworkID.</summary>
        public string? NetworkId { get; set; }
    }

    public class CreateResult
    {
        public bool Success { get; set; } = true;
        public string? RedirectUrl { get; set; }
        public List<InvF_InventoryIn>? Lst_InvF_InventoryIn { get; set; }
        public List<InvF_InventoryInDtl>? Lst_InvF_InventoryInDtl { get; set; }
        public List<InvF_InventoryInInstLot>? Lst_InvF_InventoryInInstLot { get; set; }
        public List<InvF_InventoryInInstSerial>? Lst_InvF_InventoryInInstSerial { get; set; }
        public List<Mst_Inventory>? Lst_Mst_Inventory { get; set; }
        public List<Mst_InvInType>? Lst_Mst_InvInType { get; set; }
        public List<Mst_Customer>? Lst_Mst_Customer { get; set; }
        public List<Mst_VATRate>? List_Mst_VATRate { get; set; }
        public List<Mst_BusinessGroup>? Lst_Mst_BusinessGroup { get; set; }
    }

    /// <summary>
    /// port 1:1 InvF_InventoryInController.cs:282-320 — SearchInvInType(string txtsearch).
    /// Lọc: FlagActive = '1'; nếu txtsearch không rỗng thì AND (InvInType LIKE '%txt%' OR InvInTypeName LIKE '%txt%').
    /// </summary>
    [HttpPost("api/invf_inventoryin/searchinvintype")]
    public async Task<IActionResult> SearchInvInType([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchInvInTypeReq? req)
    {
        try
        {
            var query = db.Set<Mst_InvInType>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

            var txtsearch = req?.TxtSearch;
            if (!string.IsNullOrEmpty(txtsearch))
            {
                var pattern = PortQuery.Contains(txtsearch.Trim().ToLowerInvariant());
                query = query.Where(x => EF.Functions.Like((x.InvInType ?? "").ToLower(), pattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.InvInTypeName ?? "").ToLower(), pattern, PortQuery.Esc));
            }

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
    /// Lọc: FlagActive = '1' and FlagIn_Out = '1'; nếu txtsearch không rỗng thì AND (InvCode LIKE '%txt%' OR InvName LIKE '%txt%').
    /// </summary>
    [HttpPost("api/invf_inventoryin/searchinventoryin")]
    public async Task<IActionResult> SearchInventoryIn([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchInventoryInReq? req)
    {
        try
        {
            var query = db.Set<Mst_Inventory>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.FlagIn_Out ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

            var txtsearch = req?.TxtSearch;
            if (!string.IsNullOrEmpty(txtsearch))
            {
                var pattern = PortQuery.Contains(txtsearch.Trim().ToLowerInvariant());
                query = query.Where(x => EF.Functions.Like((x.InvCode ?? "").ToLower(), pattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.InvName ?? "").ToLower(), pattern, PortQuery.Esc));
            }

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
    /// port 1:1 InvF_InventoryInController.cs:566-623 — SearchCustomer(string txtsearch).
    /// Lọc: FlagActive = '1' and FlagSupplier = '1'; nếu txtsearch không rỗng thì AND (CustomerCode LIKE '%txt%' OR CustomerName LIKE '%txt%').
    /// </summary>
    [HttpPost("api/invf_inventoryin/searchcustomer")]
    public async Task<IActionResult> SearchCustomer([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchCustomerReq? req)
    {
        try
        {
            var query = db.Set<Mst_Customer>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.FlagSupplier ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

            var txtsearch = req?.TxtSearch;
            if (!string.IsNullOrEmpty(txtsearch))
            {
                var pattern = PortQuery.Contains(txtsearch.Trim().ToLowerInvariant());
                var normSearch = txtsearch.Trim();
                query = query.Where(x => EF.Functions.Like((x.CustomerCode ?? "").ToLower(), pattern, PortQuery.Esc)
                                      || EF.Functions.Like((x.CustomerName ?? "").ToLower(), pattern, PortQuery.Esc)
                                      || (x.CustomerName != null && EF.Functions.Like(x.CustomerName, "%" + normSearch + "%")));
            }

            var list = await query.OrderBy(x => x.CustomerCode).ToListAsync();

            return Ok(new SearchCustomerResult
            {
                Success = true,
                List_Mst_Customer = list
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail("WAS_Mst_Customer_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// port 1:1 InvF_InventoryInController.cs:365-411 — ShowInvCodeInActual.
    /// Lọc Mst_Inventory: FlagActive = '1' (dòng 371); nếu invbupattern không rỗng thì AND Mst_Inventory.InvBUPattern like 'invbupattern' (dòng 373-377).
    /// </summary>
    [HttpPost("api/invf_inventoryin/showinvcodeinactual")]
    public async Task<IActionResult> ShowInvCodeInActual([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ShowInvCodeInActualReq? req)
    {
        try
        {
            var query = db.Set<Mst_Inventory>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

            var invbupattern = req?.InvBUPattern;
            if (!string.IsNullOrEmpty(invbupattern))
            {
                var pattern = invbupattern.Trim().ToLowerInvariant().Replace(@"\", @"\\");
                query = query.Where(x => EF.Functions.Like((x.InvBUPattern ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            var listMstInventory = await query.OrderBy(x => x.InvCode).ToListAsync();

            var productCodeUser = req?.ProductCodeUser ?? "";
            var productNameRaw = req?.ProductName ?? "";
            var productDisplayName = string.IsNullOrEmpty(productCodeUser) && string.IsNullOrEmpty(productNameRaw)
                ? null
                : $"{productCodeUser} - {productNameRaw}";

            var lstDetail = new List<InvF_InventoryInDtl>();
            if (!string.IsNullOrEmpty(req?.ListDetail))
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
            return PortResult.Fail("WAS_Mst_Inventory_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// port 1:1 InvF_InventoryInController.cs:415-461 — ShowLo.
    /// </summary>
    [HttpPost("api/invf_inventoryin/showlo")]
    public async Task<IActionResult> ShowLo([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ShowLoReq? req)
    {
        try
        {
            var query = db.Set<Mst_Inventory>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

            var invbupattern = req?.InvBUPattern;
            if (!string.IsNullOrEmpty(invbupattern))
            {
                var pattern = invbupattern.Trim().ToLowerInvariant().Replace(@"\", @"\\");
                query = query.Where(x => EF.Functions.Like((x.InvBUPattern ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            var listMstInventory = await query.OrderBy(x => x.InvCode).ToListAsync();

            var productCodeUser = req?.ProductCodeUser ?? "";
            var productNameRaw = req?.ProductName ?? "";
            var productDisplayName = string.IsNullOrEmpty(productCodeUser) && string.IsNullOrEmpty(productNameRaw)
                ? null
                : $"{productCodeUser} - {productNameRaw}";

            var lstLot = new List<InvF_InventoryInInstLot>();
            if (!string.IsNullOrEmpty(req?.ListLot))
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
            return PortResult.Fail("WAS_Mst_Inventory_Get", "Exception", ex.Message);
        }
    }

    /// <summary>
    /// port 1:1 InvF_InventoryInController.cs:465-512 — ShowSerial.
    /// </summary>
    [HttpPost("api/invf_inventoryin/showserial")]
    public async Task<IActionResult> ShowSerial([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ShowSerialReq? req)
    {
        try
        {
            var query = db.Set<Mst_Inventory>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

            var invbupattern = req?.InvBUPattern;
            if (!string.IsNullOrEmpty(invbupattern))
            {
                var pattern = invbupattern.Trim().ToLowerInvariant().Replace(@"\", @"\\");
                query = query.Where(x => EF.Functions.Like((x.InvBUPattern ?? "").ToLower(), pattern, PortQuery.Esc));
            }

            var listMstInventory = await query.OrderBy(x => x.InvCode).ToListAsync();

            var productCodeUser = req?.ProductCodeUser ?? "";
            var productNameRaw = req?.ProductName ?? "";
            var productDisplayName = string.IsNullOrEmpty(productCodeUser) && string.IsNullOrEmpty(productNameRaw)
                ? null
                : $"{productCodeUser} - {productNameRaw}";

            var lstSerial = new List<InvF_InventoryInInstSerial>();
            if (!string.IsNullOrEmpty(req?.ListSerial))
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<InvF_InventoryInInstSerial>>(
                    req.ListSerial,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                if (parsed != null)
                {
                    lstSerial.AddRange(parsed);
                }
            }

            return Ok(new ShowSerialResult
            {
                Success = true,
                ProductCode = req?.ProductCode,
                ProductName = productDisplayName,
                ListMst_Inventory = listMstInventory,
                ListInvF_InventoryInInstSerial = lstSerial,
                IF_InvInStatus = req?.IF_InvInStatus,
                ViewType = req?.ViewType
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
        var page = req?.Page ?? 0;
        var recordcount = req?.RecordCount ?? 50;

        var query = db.Set<InvF_InventoryIn>().AsQueryable();

        if (req != null)
        {
            if (!string.IsNullOrEmpty(req.IF_InvInNo))
            {
                var pat = PortQuery.Contains(req.IF_InvInNo.Trim().ToLowerInvariant());
                query = query.Where(x => EF.Functions.Like(x.IF_InvInNo.ToLower(), pat, PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(req.InvInType))
            {
                var pat = PortQuery.Equal(req.InvInType.Trim().ToLowerInvariant());
                query = query.Where(x => EF.Functions.Like((x.InvInType ?? "").ToLower(), pat, PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(req.CustomerCode))
            {
                var pat = PortQuery.Equal(req.CustomerCode.Trim().ToLowerInvariant());
                query = query.Where(x => EF.Functions.Like((x.CustomerCode ?? "").ToLower(), pat, PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(req.InvCodeIn))
            {
                var pat = PortQuery.Equal(req.InvCodeIn.Trim().ToLowerInvariant());
                query = query.Where(x => EF.Functions.Like((x.InvCodeIn ?? "").ToLower(), pat, PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(req.BusinessGroupCode))
            {
                var pat = PortQuery.Equal(req.BusinessGroupCode.Trim().ToLowerInvariant());
                query = query.Where(x => EF.Functions.Like((x.BusinessGroupCode ?? "").ToLower(), pat, PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(req.RefNo))
            {
                var pat = PortQuery.Contains(req.RefNo.Trim().ToLowerInvariant());
                query = query.Where(x => EF.Functions.Like((x.RefNo ?? "").ToLower(), pat, PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(req.InvoiceNo))
            {
                var pat = PortQuery.Contains(req.InvoiceNo.Trim().ToLowerInvariant());
                query = query.Where(x => EF.Functions.Like((x.InvoiceNo ?? "").ToLower(), pat, PortQuery.Esc));
            }
            if (!string.IsNullOrEmpty(req.CreatedTimeFrom) && DateTime.TryParse(req.CreatedTimeFrom, out var dFrom))
            {
                query = query.Where(x => x.CreateDTimeUTC >= dFrom);
            }
            if (!string.IsNullOrEmpty(req.CreatedTimeTo) && DateTime.TryParse(req.CreatedTimeTo, out var dTo))
            {
                query = query.Where(x => x.CreateDTimeUTC <= dTo);
            }
            if (!string.IsNullOrEmpty(req.ApprdTimeFrom) && DateTime.TryParse(req.ApprdTimeFrom, out var aFrom))
            {
                query = query.Where(x => x.ApprDTimeUTC >= aFrom);
            }
            if (!string.IsNullOrEmpty(req.ApprdTimeTo) && DateTime.TryParse(req.ApprdTimeTo, out var aTo))
            {
                query = query.Where(x => x.ApprDTimeUTC <= aTo);
            }

            var statusList = new List<string>();
            if (req.ChkPending == "1") statusList.Add("PENDING");
            if (req.ChkApproved == "1") statusList.Add("APPROVED");
            if (req.ChkCanceled == "1") statusList.Add("CANCELED");
            if (statusList.Count > 0)
            {
                query = query.Where(x => statusList.Contains(x.IF_InvInStatus ?? ""));
            }
        }

        var total = await query.CountAsync();
        var itemCount = total;
        var pageCount = recordcount > 0 ? (int)Math.Ceiling((double)total / recordcount) : 0;

        var list = new List<InvF_InventoryIn>();
        if (recordcount > 0 && page >= 0)
        {
            var skip = (int)Math.Min((long)page * recordcount, int.MaxValue);
            var take = (int)Math.Min(recordcount, int.MaxValue);
            list = await query.OrderByDescending(x => x.CreateDTimeUTC)
                              .ThenBy(x => x.IF_InvInNo)
                              .Skip(skip)
                              .Take(take)
                              .ToListAsync();
        }

        var resultList = new List<InvF_InventoryIn>();
        if (list.Count > 0)
        {
            var custCodes = list.Select(x => x.CustomerCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
            var invCodes = list.Select(x => x.InvCodeIn).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
            var userCodes = list.Select(x => x.LogLUBy).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();

            var custMap = await db.Set<Mst_Customer>().Where(c => custCodes.Contains(c.CustomerCode)).ToDictionaryAsync(c => c.CustomerCode);
            var invMap = await db.Set<Mst_Inventory>().Where(i => invCodes.Contains(i.InvCode)).ToDictionaryAsync(i => i.InvCode);
            var userMap = await db.Set<Sys_User>().Where(u => userCodes.Contains(u.UserCode)).ToDictionaryAsync(u => u.UserCode);

            foreach (var inv in list)
            {
                custMap.TryGetValue(inv.CustomerCode ?? "", out var c);
                invMap.TryGetValue(inv.InvCodeIn ?? "", out var iv);
                userMap.TryGetValue(inv.LogLUBy ?? "", out var u);

                inv.mct_CustomerCode = c?.CustomerCode;
                inv.mct_CustomerName = c?.CustomerName;
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

    /// <summary>
    /// port 1:1 InvF_InventoryInController.cs:726-888 — Create() (form nạp dữ liệu phiếu nhập kho).
    /// GAP: đích không có phiên ⇒ nhận IsSysAdmin + ListSysAccess (SysAccessCheckDeny), OrgID + NetworkID (skeleton)
    ///      qua body tuỳ chọn. Nguồn đọc từ UserState (dòng 728, 735-736).
    /// </summary>
    [HttpPost("api/invf_inventoryin/create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateReq? req)
    {
        try
        {
            // Nguồn dòng 728-732: SysAccessCheckDeny(UserState, "MNU_INVF_INVENTORYIN_CREATE")
            if (!SysAccessCheckDeny(req?.IsSysAdmin, req?.ListSysAccess, "MNU_INVF_INVENTORYIN_CREATE"))
            {
                return Ok(new CreateResult { Success = true, RedirectUrl = "/AccessDeny/Index" });
            }

            // Nguồn dòng 735-736: orgID / networkID từ UserState.Mst_NNT (GAP: đích nhận qua body).
            var orgID = req?.OrgId ?? "";
            var networkID = req?.NetworkId ?? "";

            // Nguồn dòng 738-743: SeqNo(Seq_Common { SequenceType = SeqType.IFInvInNo, Param_Prefix="", Param_Postfix="" })
            // → BaseController.cs:2214-2237 → WA_Seq_Common_Get → Seq_Common_Get (Seq.cs:126-220)
            // → Seq_Common_MyGet (Seq.cs:42-124): htMap IFInvInNo = { "Seq_IF_InvInNo", "{0}PN.{3}.{1:00000}{2}", "100000" }
            // → Seq_Common_Raw (Seq.cs:28-41): insert/delete trên Seq_IF_InvInNo → lấy AutoId tăng dần (transaction riêng).
            var seq = await PortSeq.NextAsync<Seq_IF_InvInNo>(db);
            var iFInvInNo = PortSeq.Build("{0}PN.{3}.{1:00000}{2}", null, seq, 100000, null, DateTime.UtcNow);

            // Nguồn dòng 748-832: Task.WhenAll(5 truy vấn danh mục).
            var tInvInType = db.Set<Mst_InvInType>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .ToListAsync();
            var tInventory = db.Set<Mst_Inventory>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.FlagIn_Out ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .ToListAsync();
            var tCustomer = db.Set<Mst_Customer>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.FlagSupplier ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .ToListAsync();
            var tVATRate = db.Set<Mst_VATRate>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .ToListAsync();
            var tBusinessGroup = db.Set<Mst_BusinessGroup>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.BusinessType ?? "").ToLower(), PortQuery.Equal("DV"), PortQuery.Esc))
                .ToListAsync();
            await Task.WhenAll(tInvInType, tInventory, tCustomer, tVATRate, tBusinessGroup);

            // Nguồn dòng 871-886: return View(new RT_InvF_InventoryIn {...}); skeleton PENDING, 4 list con RỖNG.
            return Ok(new CreateResult
            {
                Success = true,
                Lst_InvF_InventoryIn = new List<InvF_InventoryIn>
                {
                    new InvF_InventoryIn
                    {
                        NetworkID = networkID,
                        OrgID = orgID,
                        IF_InvInNo = iFInvInNo,
                        IF_InvInStatus = "PENDING",
                    }
                },
                Lst_InvF_InventoryInDtl = new List<InvF_InventoryInDtl>(),
                Lst_InvF_InventoryInInstLot = new List<InvF_InventoryInInstLot>(),
                Lst_InvF_InventoryInInstSerial = new List<InvF_InventoryInInstSerial>(),
                Lst_Mst_Inventory = tInventory.Result,
                Lst_Mst_InvInType = tInvInType.Result,
                Lst_Mst_Customer = tCustomer.Result,
                List_Mst_VATRate = tVATRate.Result,
                Lst_Mst_BusinessGroup = tBusinessGroup.Result,
            });
        }
        catch (Exception ex)
        {
            // Mã mặc định nguồn của chuỗi Seq (biz WAS_Seq_Common_Get, Seq.cs:2659) — bao trọn Create khi bước Seq lỗi.
            return PortResult.Fail("WAS_Seq_Common_Get", "Exception", ex.Message);
        }
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

    /// <summary>Overload dùng cho Create (CreateReq mang IsSysAdmin + ListSysAccess). Logic y hệt Index.</summary>
    private static bool SysAccessCheckDeny(bool? isSysAdmin, List<string>? listSysAccess, string functionNames)
    {
        if (isSysAdmin == true) return true;
        if (!string.IsNullOrWhiteSpace(functionNames) && listSysAccess != null)
        {
            return listSysAccess.Any(a => string.Equals(a, functionNames.Trim(), StringComparison.InvariantCultureIgnoreCase));
        }
        return false;
    }
}

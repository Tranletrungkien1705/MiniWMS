using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — actions Index, DoSearch, SearchInvInType, SearchInventoryIn,
/// ShowInvCodeInActual, ShowLo, ShowSerial, SearchCustomer, Create, UpdateData.
/// Action UpdateData (#84): dòng 1267-1326 → WA_InvF_InventoryIn_Save (API InvFInventoryInController.cs:92-161)
///   → WAS_InvF_InventoryIn_Save_New20240606 (Form.cs:169677-169823)
///   → InvF_InventoryIn_Save_New20241112 (Form.cs:169830-170089)
///   → InvF_InventoryIn_SaveX_New20241112 (Form.cs:170091-172363).
/// </summary>
[ApiController]
public partial class InvF_InventoryInControllerPort(AppDbContext db) : ControllerBase
{
    public class IndexReq
    {
        public int Page { get; set; } = 0;
        public int RecordCount { get; set; } = 10;
        public bool? IsSysAdmin { get; set; }
        public List<string>? ListSysAccess { get; set; }
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

    public class SearchInvInTypeReq { public string? TxtSearch { get; set; } }
    public class SearchInvInTypeResult { public bool Success { get; set; } = true; public List<Mst_InvInType> List_Mst_InvInType { get; set; } = new(); }
    public class SearchInventoryInReq { public string? TxtSearch { get; set; } }
    public class SearchInventoryInResult { public bool Success { get; set; } = true; public List<Mst_Inventory> List_Mst_Inventory { get; set; } = new(); }
    public class SearchCustomerReq { public string? TxtSearch { get; set; } }
    public class SearchCustomerResult { public bool Success { get; set; } = true; public List<Mst_Customer> List_Mst_Customer { get; set; } = new(); }

    public class ShowInvCodeInActualReq
    {
        public string? ProductCode { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductCodeBase { get; set; }
        public string? ProductName { get; set; }
        public string? InvBUPattern { get; set; }
        public string? ListDetail { get; set; }
        public string? IF_InvInStatus { get; set; }
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
        public string? ProductCode { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
        public string? InvBUPattern { get; set; }
        public string? ListLot { get; set; }
        public string? IF_InvInStatus { get; set; }
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
        public string? ProductCode { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
        public string? InvBUPattern { get; set; }
        public string? ListSerial { get; set; }
        public string? IF_InvInStatus { get; set; }
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

    public class CreateReq
    {
        public bool? IsSysAdmin { get; set; }
        public List<string>? ListSysAccess { get; set; }
        public string? OrgId { get; set; }
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

    public class UpdateDataReq
    {
        public bool? IsSysAdmin { get; set; }
        public List<string>? ListSysAccess { get; set; }
        public string? OrgId { get; set; }
        public string? NetworkId { get; set; }
        public string? WaUserCode { get; set; }
        public InvF_InventoryInSaveReq? Model { get; set; }
    }

    public class InvF_InventoryInSaveReq
    {
        public InvF_InventoryIn? Obj_InvF_InventoryIn { get; set; }
        public List<InvF_InventoryInDtl>? Lst_InvF_InventoryInDtl { get; set; }
        public List<InvF_InventoryInInstLot>? Lst_InvF_InventoryInInstLot { get; set; }
        public List<InvF_InventoryInInstSerial>? Lst_InvF_InventoryInInstSerial { get; set; }
        public List<InvF_InventoryInQR>? Lst_InvF_InventoryInQR { get; set; }
        public string? FlagRedirect { get; set; }
    }

    public class UpdateDataResult
    {
        public bool Success { get; set; }
        public string? RedirectUrl { get; set; }
        public List<string>? Messages { get; set; }
        public string? ErrorCode { get; set; }
        public string? Detail { get; set; }
        public List<InvF_InventoryIn>? Lst_InvF_InventoryIn { get; set; }
    }

    private const string ErrDefault = "InvF_InventoryIn_Save";
    private const string ErrInvalidIF_InvInNo = "InvF_InventoryIn_SaveX_InvalidIF_InvInNo";
    private const string ErrInvalidStatus = "InvF_InventoryIn_SaveX_InvalidStatus";
    private const string ErrInvalidCustomerCode = "InvF_InventoryIn_SaveX_InvalidCustomerCode";
    private const string ErrInvalidRefNoSys = "InvF_InventoryIn_SaveX_InvalidRefNoSys";
    private const string ErrInvalidRefType = "InvF_InventoryIn_SaveX_InvalidRefType";
    private const string ErrInvalidValues = "InvF_InventoryIn_SaveX_InvalidValues";
    private const string FlagYes = "1";
    private const string StatusPending = "PENDING";

    [HttpPost("api/invf_inventoryin/updatedata")]
    public async Task<IActionResult> UpdateData([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] UpdateDataReq? req)
    {
        try
        {
            if (!SysAccessCheckDenyUpdate(req?.IsSysAdmin, req?.ListSysAccess))
                return Ok(new UpdateDataResult { Success = false, RedirectUrl = "/AccessDeny/Index", Messages = new() { "Access denied" } });

            if (req?.Model == null || req.Model.Obj_InvF_InventoryIn == null)
                return PortResult.Fail(ErrDefault, "Check.model", null);

            var save = req.Model;
            var header = save.Obj_InvF_InventoryIn;

            header.OrgID = req.OrgId;
            header.NetworkID = req.NetworkId;

            var waUserCode = req.WaUserCode ?? "";

            var strIF_InvInNo = StdParam(header.IF_InvInNo);
            var strOrgID = StdParam(header.OrgID);
            var strInvInType = StdParam(header.InvInType);
            var strInvCodeIn = StdParam(header.InvCodeIn);
            var strCustomerCode = StdParam(header.CustomerCode);
            var strInvoiceNo = StdParam(header.InvoiceNo);
            var strOrderNoSys = StdParam(header.OrderNoSys);
            var strOrderNo = StdParam(header.OrderNo);
            var strOrderType = StdParam(header.OrderType);
            var strRefNoSys = StdParam(header.RefNoSys);
            var strRefNo = StdParam(header.RefNo);
            var strRefType = StdParam(header.RefType);
            var strUserDeliver = (header.UserDeliver ?? "").Trim();
            var strRemark = (header.Remark ?? "").Trim();
            var strBusinessGroupCode = StdParam(header.BusinessGroupCode);
            var strInvoiceSign = StdStr(header.InvoiceSign);

            if (string.IsNullOrEmpty(strIF_InvInNo))
                return PortResult.Fail(ErrInvalidIF_InvInNo, "Check.strIF_InvInNo", strIF_InvInNo);

            var dtDB = await db.Set<InvF_InventoryIn>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.IF_InvInNo == strIF_InvInNo);
            var hasOld = dtDB != null;

            string strCreateBy;
            string strCodeGuid;
            if (hasOld)
            {
                if (!StringEqualIgnoreCase(dtDB!.IF_InvInStatus, StatusPending))
                    return PortResult.Fail(ErrInvalidStatus, "Check.DB.IF_InvInStatus",
                        dtDB.IF_InvInStatus, "Check.IF_InvInStatus.Expected", StatusPending);
                strCreateBy = StdParam(dtDB.CreateBy) ?? "";
                var guidDB = StdStr(dtDB.CodeGuid);
                strCodeGuid = string.IsNullOrEmpty(guidDB) ? Guid.NewGuid().ToString() : guidDB!;
            }
            else
            {
                strCreateBy = waUserCode;
                strCodeGuid = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrEmpty(strOrgID))
                return PortResult.Fail("Mst_Org_CheckDB_OrgNotFound", "Check.strOrgID", strOrgID);
            var orgDb = await db.Set<Mst_Org>().AsNoTracking().FirstOrDefaultAsync(x => x.OrgID == strOrgID);
            if (orgDb == null)
                return PortResult.Fail("Mst_Org_CheckDB_OrgNotFound", "Check.strOrgID", strOrgID);
            if (!StringEqual(orgDb.FlagActive, FlagYes))
                return PortResult.Fail("Mst_Org_CheckDB_FlagActiveNotMatched", "Check.FlagActive", orgDb.FlagActive, "Check.strFlagActiveListToCheck", FlagYes);

            if (string.IsNullOrEmpty(strCustomerCode))
                return PortResult.Fail(ErrInvalidCustomerCode, "Check.strIF_InvInNo", strIF_InvInNo, "Check.strCustomerCode", strCustomerCode);

            var dblTotalValIn = header.TotalValIn ?? 0d;
            if (dblTotalValIn < 0) return PortResult.Fail(ErrInvalidValues, "Check.dblTotalValIn", dblTotalValIn);
            var dblTotalValInDesc = header.TotalValInDesc ?? 0d;
            if (dblTotalValInDesc < 0) return PortResult.Fail(ErrInvalidValues, "Check.dblTotalValInDesc", dblTotalValInDesc);
            var dblTotalValInAfterDesc = header.TotalValInAfterDesc ?? 0d;
            if (dblTotalValInAfterDesc < 0) return PortResult.Fail(ErrInvalidValues, "Check.dblTotalValInAfterDesc", dblTotalValInAfterDesc);
            var dblTotalValVAT = header.TotalValVAT ?? 0d;
            if (dblTotalValVAT < 0) return PortResult.Fail(ErrInvalidValues, "Check.TotalValVAT", dblTotalValVAT);

            if (!string.IsNullOrEmpty(strRefNo))
            {
                if (string.IsNullOrEmpty(strRefNoSys))
                    return PortResult.Fail(ErrInvalidRefNoSys, "Check.strIF_InvInNo", strIF_InvInNo, "Check.strRefNo", strRefNo, "Check.strRefNoSys", strRefNoSys);
                if (string.IsNullOrEmpty(strRefType))
                    return PortResult.Fail(ErrInvalidRefType, "Check.strIF_InvInNo", strIF_InvInNo, "Check.strRefNo", strRefNo, "Check.stRefType", strRefType);
            }

            var lstDtl = save.Lst_InvF_InventoryInDtl ?? new List<InvF_InventoryInDtl>();
            var lstLot = save.Lst_InvF_InventoryInInstLot ?? new List<InvF_InventoryInInstLot>();
            var lstSerial = save.Lst_InvF_InventoryInInstSerial ?? new List<InvF_InventoryInInstSerial>();
            var lstQR = save.Lst_InvF_InventoryInQR ?? new List<InvF_InventoryInQR>();

            var dtimeSys = DateTime.UtcNow;

            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                if (hasOld)
                {
                    await db.Set<InvF_InventoryInDtl>().Where(x => x.IF_InvInNo == strIF_InvInNo).ExecuteDeleteAsync();
                    await db.Set<InvF_InventoryInInstSerial>().Where(x => x.IF_InvInNo == strIF_InvInNo).ExecuteDeleteAsync();
                    await db.Set<InvF_InventoryInInstLot>().Where(x => x.IF_InvInNo == strIF_InvInNo).ExecuteDeleteAsync();
                    await db.Set<InvF_InventoryInQR>().Where(x => x.IF_InvInNo == strIF_InvInNo).ExecuteDeleteAsync();
                    await db.Set<InvF_InventoryIn>().Where(x => x.IF_InvInNo == strIF_InvInNo).ExecuteDeleteAsync();
                }

                var headerRow = new InvF_InventoryIn
                {
                    IF_InvInNo = strIF_InvInNo!,
                    NetworkID = header.NetworkID,
                    OrgID = strOrgID,
                    InvInType = strInvInType,
                    InvCodeIn = strInvCodeIn,
                    IF_InvAudNo = StdParam(header.IF_InvAudNo),
                    CustomerCode = strCustomerCode,
                    InvoiceNo = strInvoiceNo,
                    OrderNoSys = strOrderNoSys,
                    OrderNo = strOrderNo,
                    OrderType = strOrderType,
                    RefNoSys = strRefNoSys,
                    RefNo = strRefNo,
                    RefType = strRefType,
                    UserDeliver = strUserDeliver,
                    TotalValIn = header.TotalValIn,
                    TotalValInDesc = header.TotalValInDesc,
                    TotalValInAfterDesc = header.TotalValInAfterDesc,
                    CreateDTimeUTC = hasOld ? dtDB!.CreateDTimeUTC : dtimeSys,
                    CreateBy = strCreateBy,
                    LUDTimeUTC = null,
                    LUBy = null,
                    ApprDTimeUTC = null,
                    ApprBy = null,
                    FlagQR = "0",
                    IF_InvInStatus = StatusPending,
                    Remark = strRemark,
                    InvFCFInCode01 = (header.InvFCFInCode01 ?? "").Trim(),
                    InvFCFInCode02 = (header.InvFCFInCode02 ?? "").Trim(),
                    InvFCFInCode03 = (header.InvFCFInCode03 ?? "").Trim(),
                    InvFCFInCode04 = (header.InvFCFInCode04 ?? "").Trim(),
                    InvFCFInCode05 = (header.InvFCFInCode05 ?? "").Trim(),
                    InvFCFInCode06 = (header.InvFCFInCode06 ?? "").Trim(),
                    InvFCFInCode07 = (header.InvFCFInCode07 ?? "").Trim(),
                    InvFCFInCode08 = (header.InvFCFInCode08 ?? "").Trim(),
                    InvFCFInCode09 = (header.InvFCFInCode09 ?? "").Trim(),
                    InvFCFInCode10 = (header.InvFCFInCode10 ?? "").Trim(),
                    InvoiceDate = header.InvoiceDate,
                    InvoiceSign = strInvoiceSign,
                    FlagSynced = "0",
                    LogLUDTimeUTC = dtimeSys,
                    LogLUBy = waUserCode,
                    CodeGuid = strCodeGuid,
                    TotalValVAT = header.TotalValVAT,
                    BusinessGroupCode = strBusinessGroupCode,
                    FlagActive = "1"
                };
                db.Set<InvF_InventoryIn>().Add(headerRow);

                foreach (var d in lstDtl)
                {
                    d.IF_InvInNo = strIF_InvInNo!;
                    d.NetworkID = header.NetworkID;
                    d.InvCodeInActual = StdParam(d.InvCodeInActual);
                    d.ProductCode = StdParam(d.ProductCode) ?? "";
                    d.IF_InvInStatusDtl = StatusPending;
                    d.LogLUDTimeUTC = dtimeSys;
                    d.LogLUBy = waUserCode;
                    db.Set<InvF_InventoryInDtl>().Add(d);
                }

                foreach (var l in lstLot)
                {
                    l.IF_InvInNo = strIF_InvInNo;
                    l.NetworkID = header.NetworkID;
                    l.InvCodeInActual = StdParam(l.InvCodeInActual);
                    l.ProductCode = StdParam(l.ProductCode);
                    l.ProductLotNo = StdParam(l.ProductLotNo);
                    l.IF_InvInILStatus = StatusPending;
                    l.LogLUDTimeUTC = dtimeSys;
                    l.LogLUBy = waUserCode;
                    db.Set<InvF_InventoryInInstLot>().Add(l);
                }

                foreach (var s in lstSerial)
                {
                    s.IF_InvInNo = strIF_InvInNo;
                    s.NetworkID = header.NetworkID;
                    s.InvCodeInActual = StdParam(s.InvCodeInActual);
                    s.ProductCode = StdParam(s.ProductCode);
                    s.SerialNo = StdParam(s.SerialNo);
                    s.IF_InvInISStatus = StatusPending;
                    s.LogLUDTimeUTC = dtimeSys;
                    s.LogLUBy = waUserCode;
                    db.Set<InvF_InventoryInInstSerial>().Add(s);
                }

                foreach (var q in lstQR)
                {
                    q.IF_InvInNo = strIF_InvInNo;
                    q.NetworkID = header.NetworkID;
                    q.QRCode = StdParam(q.QRCode);
                    q.ProductCode = StdParam(q.ProductCode);
                    q.BoxNo = StdParam(q.BoxNo);
                    q.CanNo = StdParam(q.CanNo);
                    q.ProductLotNo = StdParam(q.ProductLotNo);
                    q.ShiftInCode = StdParam(q.ShiftInCode);
                    q.LogLUDTimeUTC = dtimeSys;
                    q.LogLUBy = waUserCode;
                    db.Set<InvF_InventoryInQR>().Add(q);
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }

            var redirect = (save.FlagRedirect ?? "").Equals("1", StringComparison.Ordinal)
                ? "/api/invf_inventoryin/create"
                : "/api/invf_inventoryin/index";

            var savedList = await db.Set<InvF_InventoryIn>().AsNoTracking()
                .Where(x => x.IF_InvInNo == strIF_InvInNo).ToListAsync();

            return Ok(new UpdateDataResult
            {
                Success = true,
                RedirectUrl = redirect,
                Messages = new() { "Sửa phiếu nhập kho thành công!" },
                Lst_InvF_InventoryIn = savedList
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail(ErrDefault, "Exception", ex.Message);
        }
    }

    private static string? StdParam(string? s)
    {
        if (s == null) return null;
        var t = s.Trim();
        return t.Length == 0 ? null : t.ToUpperInvariant();
    }

    private static string? StdStr(string? s)
    {
        if (s == null) return null;
        var t = s.Trim();
        return t.Length == 0 ? null : t;
    }

    private static bool StringEqual(string? a, string? b)
        => string.Equals(a ?? "", b ?? "", StringComparison.Ordinal);

    private static bool StringEqualIgnoreCase(object? a, object? b)
        => string.Equals(Convert.ToString(a) ?? "", Convert.ToString(b) ?? "", StringComparison.OrdinalIgnoreCase);

    private static bool SysAccessCheckDenyUpdate(bool? isSysAdmin, List<string>? listSysAccess)
    {
        if (isSysAdmin == true) return true;
        const string fn = "MNU_INVF_INVENTORYIN_UPDATE";
        if (listSysAccess != null && listSysAccess.Any(a => string.Equals(a, fn, StringComparison.InvariantCultureIgnoreCase)))
            return true;
        return false;
    }

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
            return Ok(new SearchInvInTypeResult { Success = true, List_Mst_InvInType = list });
        }
        catch (Exception ex) { return PortResult.Fail("WAS_Mst_InvInType_Get", "Exception", ex.Message); }
    }

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
            return Ok(new SearchInventoryInResult { Success = true, List_Mst_Inventory = list });
        }
        catch (Exception ex) { return PortResult.Fail("WAS_Mst_Inventory_Get", "Exception", ex.Message); }
    }

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
            return Ok(new SearchCustomerResult { Success = true, List_Mst_Customer = list });
        }
        catch (Exception ex) { return PortResult.Fail("WAS_Mst_Customer_Get", "Exception", ex.Message); }
    }

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
                ? null : $"{productCodeUser} - {productNameRaw}";
            var lstDetail = new List<InvF_InventoryInDtl>();
            if (!string.IsNullOrEmpty(req?.ListDetail))
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<InvF_InventoryInDtl>>(req.ListDetail,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null) lstDetail.AddRange(parsed);
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
        catch (Exception ex) { return PortResult.Fail("WAS_Mst_Inventory_Get", "Exception", ex.Message); }
    }

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
                ? null : $"{productCodeUser} - {productNameRaw}";
            var lstLot = new List<InvF_InventoryInInstLot>();
            if (!string.IsNullOrEmpty(req?.ListLot))
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<InvF_InventoryInInstLot>>(req.ListLot,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null) lstLot.AddRange(parsed);
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
        catch (Exception ex) { return PortResult.Fail("WAS_Mst_Inventory_Get", "Exception", ex.Message); }
    }

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
                ? null : $"{productCodeUser} - {productNameRaw}";
            var lstSerial = new List<InvF_InventoryInInstSerial>();
            if (!string.IsNullOrEmpty(req?.ListSerial))
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<InvF_InventoryInInstSerial>>(req.ListSerial,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null) lstSerial.AddRange(parsed);
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
        catch (Exception ex) { return PortResult.Fail("WAS_Mst_Inventory_Get", "Exception", ex.Message); }
    }

    [HttpPost("api/invf_inventoryin/index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] IndexReq? req)
    {
        var page = req?.Page ?? 0;
        var recordcount = req?.RecordCount ?? 10;
        if (!SysAccessCheckDeny(req, "MNU_INVF_INVENTORYIN"))
            return Ok(new IndexResult { Success = true, RedirectUrl = "/AccessDeny/Index" });
        var pageInfo = new PageInfo<InvF_InventoryIn>(
            DataList: new List<InvF_InventoryIn>(), ItemCount: 0, PageCount: 0, PageIndex: page, PageSize: recordcount);
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
                query = query.Where(x => x.CreateDTimeUTC >= dFrom);
            if (!string.IsNullOrEmpty(req.CreatedTimeTo) && DateTime.TryParse(req.CreatedTimeTo, out var dTo))
                query = query.Where(x => x.CreateDTimeUTC <= dTo);
            if (!string.IsNullOrEmpty(req.ApprdTimeFrom) && DateTime.TryParse(req.ApprdTimeFrom, out var aFrom))
                query = query.Where(x => x.ApprDTimeUTC >= aFrom);
            if (!string.IsNullOrEmpty(req.ApprdTimeTo) && DateTime.TryParse(req.ApprdTimeTo, out var aTo))
                query = query.Where(x => x.ApprDTimeUTC <= aTo);
            var statusList = new List<string>();
            if (req.ChkPending == "1") statusList.Add("PENDING");
            if (req.ChkApproved == "1") statusList.Add("APPROVED");
            if (req.ChkCanceled == "1") statusList.Add("CANCELED");
            if (statusList.Count > 0)
                query = query.Where(x => statusList.Contains(x.IF_InvInStatus ?? ""));
        }
        var total = await query.CountAsync();
        var pageCount = recordcount > 0 ? (int)Math.Ceiling((double)total / recordcount) : 0;
        var list = new List<InvF_InventoryIn>();
        if (recordcount > 0 && page >= 0)
        {
            var skip = (int)Math.Min((long)page * recordcount, int.MaxValue);
            var take = (int)Math.Min(recordcount, int.MaxValue);
            list = await query.OrderByDescending(x => x.CreateDTimeUTC).ThenBy(x => x.IF_InvInNo).Skip(skip).Take(take).ToListAsync();
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
            DataList: resultList, ItemCount: total, PageCount: pageCount, PageIndex: page, PageSize: recordcount);
        return Ok(new { Success = true, Data = pageInfo });
    }

    [HttpPost("api/invf_inventoryin/create")]
    public async Task<IActionResult> Create([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateReq? req)
    {
        try
        {
            if (!SysAccessCheckDeny(req?.IsSysAdmin, req?.ListSysAccess, "MNU_INVF_INVENTORYIN_CREATE"))
                return Ok(new CreateResult { Success = true, RedirectUrl = "/AccessDeny/Index" });
            var orgID = req?.OrgId ?? "";
            var networkID = req?.NetworkId ?? "";
            var seq = await PortSeq.NextAsync<Seq_IF_InvInNo>(db);
            var iFInvInNo = PortSeq.Build("{0}PN.{3}.{1:00000}{2}", null, seq, 100000, null, DateTime.UtcNow);
            var tInvInType = db.Set<Mst_InvInType>().Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)).ToListAsync();
            var tInventory = db.Set<Mst_Inventory>().Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                          && EF.Functions.Like((x.FlagIn_Out ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)).ToListAsync();
            var tCustomer = db.Set<Mst_Customer>().Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                          && EF.Functions.Like((x.FlagSupplier ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)).ToListAsync();
            var tVATRate = db.Set<Mst_VATRate>().Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)).ToListAsync();
            var tBusinessGroup = db.Set<Mst_BusinessGroup>().Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                          && EF.Functions.Like((x.BusinessType ?? "").ToLower(), PortQuery.Equal("DV"), PortQuery.Esc)).ToListAsync();
            await Task.WhenAll(tInvInType, tInventory, tCustomer, tVATRate, tBusinessGroup);
            return Ok(new CreateResult
            {
                Success = true,
                Lst_InvF_InventoryIn = new List<InvF_InventoryIn>
                {
                    new InvF_InventoryIn { NetworkID = networkID, OrgID = orgID, IF_InvInNo = iFInvInNo, IF_InvInStatus = "PENDING" }
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
        catch (Exception ex) { return PortResult.Fail("WAS_Seq_Common_Get", "Exception", ex.Message); }
    }

    private static bool SysAccessCheckDeny(IndexReq? req, string functionNames)
    {
        if (req != null)
        {
            if (req.IsSysAdmin == true) return true;
            if (!string.IsNullOrWhiteSpace(functionNames) && req.ListSysAccess != null)
                return req.ListSysAccess.Any(a => string.Equals(a, functionNames.Trim(), StringComparison.InvariantCultureIgnoreCase));
        }
        return false;
    }

    private static bool SysAccessCheckDeny(bool? isSysAdmin, List<string>? listSysAccess, string functionNames)
    {
        if (isSysAdmin == true) return true;
        if (!string.IsNullOrWhiteSpace(functionNames) && listSysAccess != null)
            return listSysAccess.Any(a => string.Equals(a, functionNames.Trim(), StringComparison.InvariantCultureIgnoreCase));
        return false;
    }
}

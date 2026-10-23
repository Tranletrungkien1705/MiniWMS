using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.cs — action `Detail` (#86).
/// Chuỗi nguồn: gate `InvF_InventoryInController.cs:1385-1673`
///  → `WA_InvF_InventoryIn_Get` (API InvFInventoryInController.cs:20-89)
///  → `WAS_InvF_InventoryIn_Get` (Form.cs:147386-147405)
///  → `InvF_InventoryIn_Get` (Form.cs:147161-147337)
///  → `InvF_InventoryIn_GetX` (Form.cs:146769-147160 — SQL).
/// Ngoài ra gate còn gọi song song `WA_Mst_Customer_Get`, `WA_Mst_Product_Get`, `Mst_Inventory_Get`,
/// `Mst_InvInType_Get`, `List_Mst_VATRate`; port gộp lại thành query trực tiếp trên các bảng danh mục đã có.
/// ViewAbility (#tbl_Mst_NNT_ViewAbility, #tbl_Mst_Inventory_ViewAbility) — xem GAP.
/// (Partial class — `[ApiController]` đã khai ở InvF_InventoryInControllerPort.cs.)
/// </summary>
public partial class InvF_InventoryInControllerPort
{
    private const string ErrDetail = "InvF_InventoryIn_Get";

    public class DetailReq
    {
        public bool? IsSysAdmin { get; set; }
        public List<string>? ListSysAccess { get; set; }
        /// <summary>Tham số `if_invinno` của gate (InvF_InventoryInController.cs:1385).</summary>
        public string? IF_InvInNo { get; set; }
    }

    public class DetailResult
    {
        public bool Success { get; set; } = true;
        public string? RedirectUrl { get; set; }
        public List<Mst_Inventory>? Lst_Mst_Inventory { get; set; }
        public List<Mst_InvInType>? Lst_Mst_InvInType { get; set; }
        public List<Mst_VATRate>? List_Mst_VATRate { get; set; }
        public List<Mst_BusinessGroup>? Lst_Mst_BusinessGroup { get; set; }
        public List<Mst_Customer>? Lst_MstCustomer { get; set; }
        public InvF_InventoryIn? Obj_InvF_InventoryIn { get; set; }
        public List<InvF_InventoryInDtlExt>? Lst_InvF_InventoryInDtl { get; set; }
        public List<InvF_InventoryInInstLot>? Lst_InvF_InventoryInInstLot { get; set; }
        public List<InvF_InventoryInInstSerial>? Lst_InvF_InventoryInInstSerial { get; set; }
        public List<InvF_InventoryInQR>? Lst_InvF_InventoryInQR { get; set; }
        public List<InvF_InventoryInDtlUI>? Lst_InvF_InventoryInDtlUI { get; set; }
    }

    /// <summary>
    /// Bản chiếu đầy đủ của 1 dòng InvF_InventoryInDtl bao gồm các cột join mà SQL nguồn lấy thêm
    /// (Form.cs:146948-146963 — `mp_*`, `mii_*`, `mu_UnitName`).
    /// </summary>
    public class InvF_InventoryInDtlExt
    {
        public string IF_InvInNo { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public string? InvCodeInActual { get; set; }
        public string? NetworkID { get; set; }
        public double? Qty { get; set; }
        public double? QtyReturn { get; set; }
        public double? UPIn { get; set; }
        public double? UPInDesc { get; set; }
        public double? ValInvIn { get; set; }
        public double? ValInDesc { get; set; }
        public double? ValInAfterDesc { get; set; }
        public string? UnitCode { get; set; }
        public string? IF_InvInStatusDtl { get; set; }
        public string? Remark { get; set; }
        public DateTime? LogLUDTimeUTC { get; set; }
        public string? LogLUBy { get; set; }
        public double? VAT { get; set; }
        public double? ValVAT { get; set; }

        // Cột join (Form.cs:146951-146957, 146959-146962)
        public string? mp_ProductCode { get; set; }
        public string? mp_ProductName { get; set; }
        public string? mp_ProductCodeUser { get; set; }
        public string? mp_ProductCodeBase { get; set; }
        public string? mp_FlagLot { get; set; }
        public string? mp_FlagSerial { get; set; }
        public double? mp_ValConvert { get; set; }
        public string? mii_InvCode { get; set; }
        public string? mii_InvName { get; set; }
        public string? mu_UnitName { get; set; }
    }

    /// <summary>
    /// Khuôn UI gom nhóm theo ProductCode của nguồn (gate InvF_InventoryInController.cs:1616-1638).
    /// </summary>
    public class InvF_InventoryInDtlUI
    {
        public string? IF_InvInNo { get; set; }
        public string? InvCodeInActual { get; set; }
        public string? ProductCode { get; set; }
        public string? NetworkID { get; set; }
        public double? Qty { get; set; }
        public double? UPIn { get; set; }
        public double? UPInDesc { get; set; }
        public double? ValInvIn { get; set; }
        public double? ValInDesc { get; set; }
        public double? ValInAfterDesc { get; set; }
        public string? UnitCode { get; set; }
        public string? IF_InvInStatusDtl { get; set; }
        public string? Remark { get; set; }
        public string? mp_ProductName { get; set; }
        public string? mp_ProductCodeUser { get; set; }
        public string? mp_ProductCodeBase { get; set; }
        public string? mp_FlagLot { get; set; }
        public string? mp_FlagSerial { get; set; }
        public double? ValVAT { get; set; }
        public double? VAT { get; set; }
        public List<Mst_Product>? Lst_Mst_ProductBase { get; set; }
    }

    /// <summary>
    /// POST /api/invf_inventoryin/detail — route khớp `InvF_InventoryInController/Detail`.
    /// Gate nguồn nhận tham số `if_invinno` từ query; port nhận `IF_InvInNo` trong body (đúng luật port 1:1 dùng POST).
    /// </summary>
    [HttpPost("api/invf_inventoryin/detail")]
    public async Task<IActionResult> Detail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DetailReq? req)
    {
        try
        {
            // Gate dòng 1387-1391: SysAccessCheckDeny(UserState, "MNU_INVF_INVENTORYIN_DETAIL")
            if (!SysAccessCheckDeny(req?.IsSysAdmin, req?.ListSysAccess, "MNU_INVF_INVENTORYIN_DETAIL"))
                return Ok(new DetailResult { Success = false, RedirectUrl = "/AccessDeny/Index" });

            // === 1. Danh mục songsong (gate dòng 1400-1487) ===
            var lstInventory = await db.Set<Mst_Inventory>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.FlagIn_Out ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .ToListAsync();

            var lstInvInType = await db.Set<Mst_InvInType>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .ToListAsync();

            var lstVATRate = await db.Set<Mst_VATRate>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .ToListAsync();

            // Mst_BusinessGroup: BusinessType = "DV" AND FlagActive = "1" (gate dòng 1433-1434 + strWhereClause_Mst_BusinessGroup)
            var lstBusinessGroup = await db.Set<Mst_BusinessGroup>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.BusinessType ?? "").ToLower(), PortQuery.Equal("DV"), PortQuery.Esc))
                .ToListAsync();

            // Mst_Customer: FlagActive = '1' AND FlagSupplier = '1' (gate dòng 1509)
            var lstCustomer = await db.Set<Mst_Customer>()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.FlagSupplier ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
                .ToListAsync();

            // === 2. Truy vấn phiếu nhập InvF_InventoryIn (strWhereClause_InvF_InventoryInByID — Form.cs:2967-2977) ===
            // Nguồn: `if (!CUtils.IsNullOrEmpty(if_invinno)) sbSql.AddWhereClause(InvF_InventoryIn.IF_InvInNo, ..., "=")`
            var strIfInvinno = StdParam(req?.IF_InvInNo);
            var invQuery = db.Set<InvF_InventoryIn>().AsQueryable();
            if (!string.IsNullOrEmpty(strIfInvinno))
                invQuery = invQuery.Where(x => EF.Functions.Like(x.IF_InvInNo.ToLower(), PortQuery.Equal(strIfInvinno!), PortQuery.Esc));

            // Nguồn SQL sắp xếp `order by iiif.CreateDTimeUTC desc` (Form.cs:146857)
            var lstHeader = await invQuery.OrderByDescending(x => x.CreateDTimeUTC).ToListAsync();

            var header = lstHeader.Count > 0 ? lstHeader[0] : null;
            var lstDetail = new List<InvF_InventoryInDtlExt>();
            var lstLot = new List<InvF_InventoryInInstLot>();
            var lstSerial = new List<InvF_InventoryInInstSerial>();
            var lstQR = new List<InvF_InventoryInQR>();
            var lstUI = new List<InvF_InventoryInDtlUI>();

            if (header != null)
            {
                // === 3. Chi tiết + LOT + Serial + QR cùng IF_InvInNo (WAS_InvF_InventoryIn_Get dòng 147408-147455) ===
                var rawDetail = await db.Set<InvF_InventoryInDtl>()
                    .Where(x => x.IF_InvInNo == header.IF_InvInNo).ToListAsync();
                lstLot = await db.Set<InvF_InventoryInInstLot>()
                    .Where(x => x.IF_InvInNo == header.IF_InvInNo).ToListAsync();
                lstSerial = await db.Set<InvF_InventoryInInstSerial>()
                    .Where(x => x.IF_InvInNo == header.IF_InvInNo).ToListAsync();
                lstQR = await db.Set<InvF_InventoryInQR>()
                    .Where(x => x.IF_InvInNo == header.IF_InvInNo).ToListAsync();

                var orgID = header.OrgID ?? "";

                // Nạp Mst_Product theo (OrgID, ProductCode) — SQL Form.cs:146968-146970
                var detailPrdCodes = rawDetail.Select(x => x.ProductCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                var prdDict = (await db.Set<Mst_Product>()
                        .Where(x => x.OrgID == orgID && detailPrdCodes.Contains(x.ProductCode))
                        .ToListAsync())
                    .ToDictionary(x => x.ProductCode, x => x);

                // Nạp Mst_Inventory cho InvCodeInActual (SQL Form.cs:146971-146973)
                var invCodes = rawDetail.Select(x => x.InvCodeInActual).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                var invDict = (await db.Set<Mst_Inventory>()
                        .Where(x => invCodes.Contains(x.InvCode))
                        .ToListAsync())
                    .ToDictionary(x => x.InvCode, x => x);

                // Nạp Mst_Unit (SQL Form.cs:146974-146976 — join theo mp.UnitCode = mu.UnitCode AND mp.OrgID = mu.OrgID)
                var unitCodes = prdDict.Values.Select(p => p.UnitCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                var unitDict = (await db.Set<Mst_Unit>()
                        .Where(x => x.OrgID == orgID && unitCodes.Contains(x.UnitCode))
                        .ToListAsync())
                    .ToDictionary(x => x.UnitCode, x => x);

                // Sinh DtlExt cho khớp ViewBag.lstInvF_InventoryInDtl của nguồn
                foreach (var d in rawDetail)
                {
                    prdDict.TryGetValue(d.ProductCode, out var p);
                    invDict.TryGetValue(d.InvCodeInActual ?? "", out var inv);
                    Mst_Unit? unit = null;
                    if (p?.UnitCode != null) unitDict.TryGetValue(p.UnitCode, out unit);

                    lstDetail.Add(new InvF_InventoryInDtlExt
                    {
                        IF_InvInNo = d.IF_InvInNo,
                        ProductCode = d.ProductCode,
                        InvCodeInActual = d.InvCodeInActual,
                        NetworkID = d.NetworkID,
                        Qty = d.Qty,
                        QtyReturn = d.QtyReturn,
                        UPIn = d.UPIn,
                        UPInDesc = d.UPInDesc,
                        ValInvIn = d.ValInvIn,
                        ValInDesc = d.ValInDesc,
                        ValInAfterDesc = d.ValInAfterDesc,
                        UnitCode = d.UnitCode,
                        IF_InvInStatusDtl = d.IF_InvInStatusDtl,
                        Remark = d.Remark,
                        LogLUDTimeUTC = d.LogLUDTimeUTC,
                        LogLUBy = d.LogLUBy,
                        VAT = d.VAT,
                        ValVAT = d.ValVAT,
                        mp_ProductCode = p?.ProductCode,
                        mp_ProductName = p?.ProductName,
                        mp_ProductCodeUser = p?.ProductCodeUser,
                        mp_ProductCodeBase = p?.ProductCodeBase,
                        mp_FlagLot = p?.FlagLot,
                        mp_FlagSerial = p?.FlagSerial,
                        mp_ValConvert = p?.ValConvert,
                        mii_InvCode = inv?.InvCode,
                        mii_InvName = inv?.InvName,
                        mu_UnitName = unit?.UnitName
                    });
                }

                // === 4. Gom UI theo ProductCode (gate dòng 1558-1652) ===
                // Nguồn: `lstPrdCodeBase = Lst_InvF_InventoryInDtl.Select(m => m.mp_ProductCodeBase.ToString()).ToList()`
                // rồi `WA_Mst_Product_Get` với `strWhereSearchProductImport(null, lstPrdCodeBase)`.
                // GAP: nội dung `strWhereSearchProductImport` không có trong excerpt — port theo đúng mục đích:
                //   lấy Mst_Product theo OrgID của header + ProductCodeBase trong danh sách trên (WHERE ProductCodeBase IN (...)).
                var lstPrdCodeBase = lstDetail
                    .Select(x => x.mp_ProductCodeBase)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .ToList()!;
                var listProductBase = lstPrdCodeBase.Count == 0
                    ? new List<Mst_Product>()
                    : await db.Set<Mst_Product>()
                        .Where(x => x.OrgID == orgID && lstPrdCodeBase.Contains(x.ProductCodeBase ?? ""))
                        .ToListAsync();

                var lstPrdDistinct = new List<string>();
                foreach (var item in lstDetail)
                {
                    var prodKey = item.ProductCode;
                    if (lstPrdDistinct.Contains(prodKey)) continue;

                    var listInvInDtlCur = lstDetail.Where(m => m.ProductCode == item.ProductCode).ToList();
                    var qty = 0.0;
                    var valInAfterDesc = 0.0;
                    var invCodeInActual = "";
                    var listViTri = new List<string>();
                    foreach (var it in listInvInDtlCur)
                    {
                        qty += it.Qty ?? 0;
                        valInAfterDesc += it.ValInAfterDesc ?? 0;
                        var viTri = it.InvCodeInActual ?? "";
                        if (!listViTri.Contains(viTri))
                        {
                            if (!string.IsNullOrEmpty(invCodeInActual))
                                invCodeInActual += ", " + viTri;
                            else
                                invCodeInActual = viTri;
                            listViTri.Add(viTri);
                        }
                    }

                    var itemUI = new InvF_InventoryInDtlUI
                    {
                        IF_InvInNo = item.IF_InvInNo,
                        InvCodeInActual = invCodeInActual,
                        ProductCode = item.ProductCode,
                        NetworkID = item.NetworkID,
                        Qty = qty,
                        UPIn = item.UPIn,
                        UPInDesc = item.UPInDesc,
                        ValInvIn = item.ValInvIn,
                        ValInDesc = item.ValInDesc,
                        ValInAfterDesc = valInAfterDesc,
                        UnitCode = item.UnitCode,
                        IF_InvInStatusDtl = item.IF_InvInStatusDtl,
                        Remark = item.Remark,
                        mp_ProductName = item.mp_ProductName,
                        mp_ProductCodeUser = item.mp_ProductCodeUser,
                        mp_ProductCodeBase = item.mp_ProductCodeBase,
                        mp_FlagLot = item.mp_FlagLot,
                        mp_FlagSerial = item.mp_FlagSerial,
                        ValVAT = item.ValVAT,
                        VAT = item.VAT
                    };

                    // Nguồn gate dòng 1640-1647: lọc productBase khớp ProductCodeBase của dòng hiện tại
                    if (listProductBase.Count > 0)
                    {
                        var listPrdCheckBase = listProductBase
                            .Where(x => (x.ProductCodeBase ?? "") == (item.mp_ProductCodeBase ?? ""))
                            .ToList();
                        if (listPrdCheckBase.Count > 0)
                            itemUI.Lst_Mst_ProductBase = listPrdCheckBase;
                    }

                    lstUI.Add(itemUI);
                    lstPrdDistinct.Add(prodKey);
                }
            }

            return Ok(new DetailResult
            {
                Success = true,
                Lst_Mst_Inventory = lstInventory,
                Lst_Mst_InvInType = lstInvInType,
                List_Mst_VATRate = lstVATRate,
                Lst_Mst_BusinessGroup = lstBusinessGroup,
                Lst_MstCustomer = lstCustomer,
                Obj_InvF_InventoryIn = header,
                Lst_InvF_InventoryInDtl = lstDetail,
                Lst_InvF_InventoryInInstLot = lstLot,
                Lst_InvF_InventoryInInstSerial = lstSerial,
                Lst_InvF_InventoryInQR = lstQR,
                Lst_InvF_InventoryInDtlUI = lstUI
            });
        }
        catch (Exception ex)
        {
            return PortResult.Fail(ErrDetail, "Exception", ex.Message);
        }
    }
}

// EOF — hết file InvF_InventoryInDetailControllerPort.cs

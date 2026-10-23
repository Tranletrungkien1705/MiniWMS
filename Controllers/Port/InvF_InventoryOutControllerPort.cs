using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port
{
    [ApiController]
    [Route("api/invf_inventoryout")]
    public class InvF_InventoryOutControllerPort(AppDbContext db) : ControllerBase
    {
        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] InvF_InventoryOutUpdateRequest? req)
        {
            if (req == null || string.IsNullOrEmpty(req.IF_InvOutNo))
            {
                return PortResult.Fail("InvF_InventoryOut_Update", "Check.IF_InvOutNo", null);
            }

            using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                var item = await db.Set<InvF_InventoryOut>().FirstOrDefaultAsync(x => x.IF_InvOutNo == req.IF_InvOutNo);
                if (item == null)
                {
                    await tx.RollbackAsync();
                    return PortResult.Fail("InvF_InventoryOut_Update", "Check.IF_InvOutNoNotFound", req.IF_InvOutNo);
                }

                item.CustomerCode = !string.IsNullOrEmpty(req.CustomerCode) ? req.CustomerCode.Trim() : item.CustomerCode;
                item.InvCodeOut = !string.IsNullOrEmpty(req.InvCodeOut) ? req.InvCodeOut.Trim() : item.InvCodeOut;
                item.RefNo = !string.IsNullOrEmpty(req.RefNo) ? req.RefNo.Trim() : item.RefNo;
                item.RefType = !string.IsNullOrEmpty(req.RefType) ? req.RefType.Trim() : item.RefType;
                item.BusinessGroupCode = !string.IsNullOrEmpty(req.BusinessGroupCode) ? req.BusinessGroupCode.Trim() : item.BusinessGroupCode;
                item.LUDTimeUTC = DateTime.UtcNow;

                db.Set<InvF_InventoryOut>().Update(item);
                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { Success = true, Data = item });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return PortResult.Fail("InvF_InventoryOut_Update", "Exception", ex.Message);
            }
        }

        [HttpPost("detail")]
        public async Task<IActionResult> Detail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] InvF_InventoryOutDetailRequest? req)
        {
            if (req == null || string.IsNullOrEmpty(req.IF_InvOutNo))
            {
                return PortResult.Fail("InvF_InventoryOut_Get", "Check.IF_InvOutNo", null);
            }

            try
            {
                var item = await db.Set<InvF_InventoryOut>().FirstOrDefaultAsync(x => x.IF_InvOutNo == req.IF_InvOutNo);
                if (item == null)
                {
                    return PortResult.Fail("InvF_InventoryOut_Get", "Check.IF_InvOutNoNotFound", req.IF_InvOutNo);
                }

                if (CUtilsStrValue(item.RefType) == "RO")
                {
                    item.RefNo = "RO-" + CUtilsStrValue(item.RefNo);
                }

                var covers = await db.Set<InvF_InventoryOutCover>().Where(x => x.IF_InvOutNo == req.IF_InvOutNo).ToListAsync();
                var lots = await db.Set<InvF_InventoryOutInstLot>().Where(x => x.IF_InvOutNo == req.IF_InvOutNo).ToListAsync();
                var serials = await db.Set<InvF_InventoryOutInstSerial>().Where(x => x.IF_InvOutNo == req.IF_InvOutNo).ToListAsync();
                var dtls = await db.Set<InvF_InventoryOutDtl>().Where(x => x.IF_InvOutNo == req.IF_InvOutNo).ToListAsync();
                var qrs = await db.Set<InvF_InventoryOutQR>().Where(x => x.IF_InvOutNo == req.IF_InvOutNo).ToListAsync();
                var attachFiles = await db.Set<InvF_InventoryOutAttachFile>().Where(x => x.IF_InvOutNo == req.IF_InvOutNo).ToListAsync();

                return Ok(new
                {
                    Success = true,
                    Data = item,
                    Lst_InvF_InventoryOutCover = covers,
                    Lst_InvF_InventoryOutInstLot = lots,
                    Lst_InvF_InventoryOutInstSerial = serials,
                    Lst_InvF_InventoryOutDtl = dtls,
                    Lst_InvF_InventoryOutQR = qrs,
                    Lst_InvF_InventoryOutAttachFile = attachFiles
                });
            }
            catch (Exception ex)
            {
                return PortResult.Fail("InvF_InventoryOut_Get", "Exception", ex.Message);
            }
        }

        /// <summary>
        /// port 1:1 InvF_InventoryOutController.cs:2011-2119 — Serial.
        /// Chuỗi: gate → WA_Inv_InventoryBalanceSerial_Get (Inventory.cs:6117) + WA_InvF_InventoryOut_Get (Form.cs:33724).
        /// </summary>
        [HttpPost("serial")]
        public async Task<IActionResult> Serial([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SerialReq? req)
        {
            try
            {
                var productCode = req?.ProductCode;
                var invBUPattern = req?.InvBUPattern;
                var ifInvOutNo = req?.IF_InvOutNo;

                // #1 Balance serial (gate 2026-2062 -> WA_Inv_InventoryBalanceSerial_Get -> Inventory.cs:6283)
                var qBal = from bal in db.Set<Inv_InventoryBalanceSerial>()
                           join mi in db.Set<Mst_Inventory>() on bal.InvCode equals mi.InvCode
                           select new { bal, mi };

                // Luôn áp Mst_Inventory.FlagActive = '1' (gate 2038).
                qBal = qBal.Where(x => EF.Functions.Like((x.mi.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

                // Inv_InventoryBalanceSerial.ProductCode = '<ProductCode>' (gate 2030-2032, SQL Server `=` case-insensitive).
                if (!string.IsNullOrEmpty(productCode))
                {
                    var p = PortQuery.Equal(productCode.Trim().ToLowerInvariant());
                    qBal = qBal.Where(x => EF.Functions.Like((x.bal.ProductCode ?? "").ToLower(), p, PortQuery.Esc));
                }

                if (!string.IsNullOrEmpty(invBUPattern))
                {
                    var pat = invBUPattern.Trim().ToLowerInvariant()
                        .Replace("\\", "\\\\")
                        .Replace("_", "\\_");
                    qBal = qBal.Where(x => EF.Functions.Like((x.mi.InvBUPattern ?? "").ToLower(), pat, "\\"));
                }

                var lstBal = await qBal
                    .OrderBy(x => x.bal.ProductCode).ThenBy(x => x.bal.SerialNo)
                    .Select(x => x.bal)
                    .ToListAsync();

                // #2 InvF_InventoryOut (gate 2065-2088, WhereClauseInvF_InventoryOut 2375-2378).
                IQueryable<InvF_InventoryOut> qOut = db.Set<InvF_InventoryOut>();
                if (!string.IsNullOrEmpty(ifInvOutNo))
                {
                    var p = PortQuery.Equal(ifInvOutNo.Trim().ToLowerInvariant());
                    qOut = qOut.Where(x => EF.Functions.Like((x.IF_InvOutNo ?? "").ToLower(), p, PortQuery.Esc));
                }
                var lstOut = await qOut.OrderBy(x => x.IF_InvOutNo).ToListAsync();
                if (lstOut.Count == 0)
                {
                    return PortResult.Fail("InvF_InventoryOut_Get", "Check.IF_InvOutNoNotFound", ifInvOutNo);
                }
                var iF_InvOutStatus = lstOut[0].IF_InvOutStatus ?? "";

                // #3 InvF_InventoryOutInstSerial + filter ProductCodeRoot == ProductCode (gate 2089-2093 — Ordinal, case-sensitive).
                IQueryable<InvF_InventoryOutInstSerial> qSer = db.Set<InvF_InventoryOutInstSerial>();
                if (!string.IsNullOrEmpty(ifInvOutNo))
                {
                    var p = PortQuery.Equal(ifInvOutNo.Trim().ToLowerInvariant());
                    qSer = qSer.Where(x => EF.Functions.Like((x.IF_InvOutNo ?? "").ToLower(), p, PortQuery.Esc));
                }
                var lstSerAll = await qSer.OrderBy(x => x.SerialNo).ToListAsync();
                var lstSer = string.IsNullOrEmpty(productCode)
                    ? new List<InvF_InventoryOutInstSerial>()
                    : lstSerAll.Where(x => string.Equals(x.ProductCodeRoot, productCode, StringComparison.Ordinal)).ToList();

                return Ok(new SerialResult
                {
                    Success = true,
                    ViewName = "Serial",
                    IF_InvOutNo = ifInvOutNo,
                    ProductCode = productCode,
                    ProductCodeUser = req?.ProductCodeUser,
                    ProductName = req?.ProductName,
                    IF_InvOutStatus = iF_InvOutStatus,
                    Lst_Inv_InventoryBalanceSerial = lstBal,
                    Lst_InvF_InventoryOutInstSerial = lstSer
                });
            }
            catch (Exception ex)
            {
                return PortResult.Fail("InvF_InventoryOut_Get", "Exception", ex.Message);
            }
        }

        /// <summary>
        /// port 1:1 InvF_InventoryOutController.cs:2122-2286 — ShowLoDetail.
        /// Chuỗi gọi: gate ShowLoDetail → WA_Inv_InventoryBalanceLot_Get (Inventory.cs:5552) + WA_InvF_InventoryOut_Get (Form.cs:33724).
        /// Tham số gate: IF_InvOutNo, ProductCode, ProductCodeBase, invBUPattern, valconvert, listLot, ViewType, viewonly = "0".
        /// Quy đổi tồn: QtyTotalOK = round(QtyTotalOK / curValConvert, 2).
        /// Khớp cache: nếu listLot có dữ liệu, duyệt gán QtyAvailOK theo lotCur.Qty và LogLUBy = "1".
        /// </summary>
        [HttpPost("showlodetail")]
        public async Task<IActionResult> ShowLoDetail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ShowLoDetailReq? req)
        {
            try
            {
                var ifInvOutNo = req?.IF_InvOutNo;
                var productCode = req?.ProductCode;
                var productCodeBase = req?.ProductCodeBase;
                var invBUPattern = req?.invBUPattern;
                var valconvert = req?.valconvert;
                var listLot = req?.listLot;
                var viewType = req?.ViewType;
                var viewonly = string.IsNullOrEmpty(req?.viewonly) ? "0" : req.viewonly;

                List<InvF_InventoryOutInstLot> lst_InvF_InventoryOutInstLotCur = new();
                if (req?.Lst_InvF_InventoryOutInstLotCur != null && req.Lst_InvF_InventoryOutInstLotCur.Count > 0)
                {
                    lst_InvF_InventoryOutInstLotCur = req.Lst_InvF_InventoryOutInstLotCur;
                }
                else if (!string.IsNullOrEmpty(listLot))
                {
                    try
                    {
                        var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<InvF_InventoryOutInstLot>>(listLot, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (deserialized != null) lst_InvF_InventoryOutInstLotCur = deserialized;
                    }
                    catch
                    {
                        // Giữ danh sách rỗng nếu json không hợp lệ
                    }
                }

                // #1 Balance lot (gate 2140-2178 -> WA_Inv_InventoryBalanceLot_Get -> Inventory.cs:5824)
                var qBal = from bal in db.Set<Inv_InventoryBalanceLot>()
                           join mi in db.Set<Mst_Inventory>() on bal.InvCode equals mi.InvCode
                           select new { bal, mi };

                // Mst_Inventory.FlagActive = '1' (gate 2153)
                qBal = qBal.Where(x => EF.Functions.Like((x.mi.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

                // Inv_InventoryBalanceLot.QtyTotalOK > 0 (gate 2154)
                qBal = qBal.Where(x => (x.bal.QtyTotalOK ?? 0) > 0);

                // Inv_InventoryBalanceLot.ProductCode = ProductCodeBase (gate 2145-2148)
                if (!string.IsNullOrEmpty(productCode))
                {
                    var pBase = PortQuery.Equal((productCodeBase ?? "").Trim().ToLowerInvariant());
                    qBal = qBal.Where(x => EF.Functions.Like((x.bal.ProductCode ?? "").ToLower(), pBase, PortQuery.Esc));
                }

                // Mst_Inventory.InvBUPattern like invBUPattern (gate 2149-2152)
                if (!string.IsNullOrEmpty(invBUPattern))
                {
                    var pat = invBUPattern.Trim().ToLowerInvariant()
                        .Replace("\\", "\\\\")
                        .Replace("_", "\\_");
                    qBal = qBal.Where(x => EF.Functions.Like((x.mi.InvBUPattern ?? "").ToLower(), pat, "\\"));
                }

                var lstInv_InventoryBalanceLot = await qBal
                    .OrderBy(x => x.bal.ProductCode).ThenBy(x => x.bal.ProductLotNo)
                    .Select(x => x.bal)
                    .ToListAsync();

                // Tính lại tồn kho theo giá trị quy đổi (gate 2180-2189)
                double curValConvert = 1;
                if (!string.IsNullOrEmpty(valconvert))
                {
                    if (double.TryParse(valconvert, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedVal))
                    {
                        curValConvert = parsedVal != 0 ? parsedVal : 1;
                    }
                }

                foreach (var lot in lstInv_InventoryBalanceLot)
                {
                    lot.QtyTotalOK = (lot.QtyTotalOK == null) ? 0 : Math.Round((lot.QtyTotalOK ?? 0) / curValConvert, 2);
                }

                // Gán QtyAvailOK theo Qty bảng cache hiện tại (gate 2191-2208)
                if (lst_InvF_InventoryOutInstLotCur.Count > 0)
                {
                    foreach (var lot in lstInv_InventoryBalanceLot)
                    {
                        var lotCur = lst_InvF_InventoryOutInstLotCur.FirstOrDefault(m =>
                            CUtilsStrValue(m.ProductCode).Equals(CUtilsStrValue(lot.ProductCode), StringComparison.Ordinal)
                            && CUtilsStrValue(m.ProductLotNo).Equals(CUtilsStrValue(lot.ProductLotNo), StringComparison.Ordinal)
                            && CUtilsStrValue(m.InvCodeOutActual).Equals(CUtilsStrValue(lot.InvCode), StringComparison.Ordinal));

                        if (lotCur != null)
                        {
                            lot.QtyAvailOK = lotCur.Qty;
                            lot.LogLUBy = "1"; // Mượn tạm dùng cho view SL (nguồn dòng 2201)
                        }
                        else
                        {
                            lot.QtyAvailOK = 0;
                        }
                    }
                }

                // #2 InvF_InventoryOut (gate 2212-2262, WhereClauseInvF_InventoryOut 2375-2378).
                IQueryable<InvF_InventoryOut> qOut = db.Set<InvF_InventoryOut>();
                if (!string.IsNullOrEmpty(ifInvOutNo))
                {
                    var p = PortQuery.Equal(ifInvOutNo.Trim().ToLowerInvariant());
                    qOut = qOut.Where(x => EF.Functions.Like((x.IF_InvOutNo ?? "").ToLower(), p, PortQuery.Esc));
                }
                var lstOut = await qOut.OrderBy(x => x.IF_InvOutNo).ToListAsync();
                if (lstOut.Count == 0)
                {
                    return PortResult.Fail("InvF_InventoryOut_Get", "Check.IF_InvOutNoNotFound", ifInvOutNo);
                }
                var iF_InvOutStatus = lstOut[0].IF_InvOutStatus ?? "";

                // #3 InvF_InventoryOutInstLot + filter ProductCodeRoot == ProductCode (gate 2236-2255 — Ordinal, case-sensitive).
                IQueryable<InvF_InventoryOutInstLot> qLot = db.Set<InvF_InventoryOutInstLot>();
                if (!string.IsNullOrEmpty(ifInvOutNo))
                {
                    var p = PortQuery.Equal(ifInvOutNo.Trim().ToLowerInvariant());
                    qLot = qLot.Where(x => EF.Functions.Like((x.IF_InvOutNo ?? "").ToLower(), p, PortQuery.Esc));
                }
                var lstLotAll = await qLot.OrderBy(x => x.ProductLotNo).ToListAsync();
                var lst_InvF_InventoryOutInstLot = string.IsNullOrEmpty(productCode)
                    ? new List<InvF_InventoryOutInstLot>()
                    : lstLotAll.Where(x => string.Equals(x.ProductCodeRoot, productCode, StringComparison.Ordinal)).ToList();

                if (lst_InvF_InventoryOutInstLotCur.Count > 0)
                {
                    foreach (var lot in lst_InvF_InventoryOutInstLot)
                    {
                        var lotCur = lst_InvF_InventoryOutInstLotCur.FirstOrDefault(m =>
                            CUtilsStrValue(m.ProductCode).Equals(CUtilsStrValue(lot.ProductCode), StringComparison.Ordinal)
                            && CUtilsStrValue(m.ProductLotNo).Equals(CUtilsStrValue(lot.ProductLotNo), StringComparison.Ordinal)
                            && CUtilsStrValue(m.InvCodeOutActual).Equals(CUtilsStrValue(lot.InvCodeOutActual), StringComparison.Ordinal));

                        if (lotCur != null)
                        {
                            lot.Qty = lotCur.Qty;
                        }
                        else
                        {
                            lot.Qty = 0;
                        }
                    }
                }

                return Ok(new ShowLoDetailResult
                {
                    Success = true,
                    ViewName = "Lo",
                    IF_InvOutNo = ifInvOutNo,
                    ProductCode = productCode,
                    ProductCodeBase = productCodeBase,
                    IF_InvOutStatus = iF_InvOutStatus,
                    ViewOnly = viewonly,
                    ViewType = viewType,
                    Lst_Inv_InventoryBalanceLot = lstInv_InventoryBalanceLot,
                    Lst_InvF_InventoryOutInstLot = lst_InvF_InventoryOutInstLot
                });
            }
            catch (Exception ex)
            {
                return PortResult.Fail("InvF_InventoryOut_Get", "Exception", ex.Message);
            }
        }

        private static string CUtilsStrValue(object? val)
        {
            return val?.ToString() ?? string.Empty;
        }
    }

    public class InvF_InventoryOutUpdateRequest
    {
        public string? IF_InvOutNo { get; set; }
        public string? CustomerCode { get; set; }
        public string? InvCodeOut { get; set; }
        public string? RefNo { get; set; }
        public string? RefType { get; set; }
        public string? BusinessGroupCode { get; set; }
    }

    public class InvF_InventoryOutDetailRequest
    {
        public string? IF_InvOutNo { get; set; }
    }

    /// <summary>Body cho action Serial (nguồn InvF_InventoryOutController.cs:2011 — 5 tham số).</summary>
    public class SerialReq
    {
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? InvBUPattern { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
    }

    /// <summary>Kết quả Serial — thay ViewBag + JsonView("Serial", Lst_InvF_InventoryOutInstSerial) của nguồn.</summary>
    public class SerialResult
    {
        public bool Success { get; set; } = true;
        public string? ViewName { get; set; } = "Serial";
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
        public string? IF_InvOutStatus { get; set; }
        public List<Inv_InventoryBalanceSerial> Lst_Inv_InventoryBalanceSerial { get; set; } = new();
        public List<InvF_InventoryOutInstSerial> Lst_InvF_InventoryOutInstSerial { get; set; } = new();
    }

    /// <summary>Body cho action ShowLoDetail (nguồn InvF_InventoryOutController.cs:2122 — 8 tham số).</summary>
    public class ShowLoDetailReq
    {
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductCodeBase { get; set; }
        public string? invBUPattern { get; set; }
        public string? valconvert { get; set; }
        public string? listLot { get; set; }
        public List<InvF_InventoryOutInstLot>? Lst_InvF_InventoryOutInstLotCur { get; set; }
        public string? ViewType { get; set; }
        public string? viewonly { get; set; }
    }

    /// <summary>Kết quả ShowLoDetail — thay ViewBag + JsonView("Lo", lst_InvF_InventoryOutInstLot) của nguồn.</summary>
    public class ShowLoDetailResult
    {
        public bool Success { get; set; } = true;
        public string? ViewName { get; set; } = "Lo";
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductCodeBase { get; set; }
        public string? IF_InvOutStatus { get; set; }
        public string? ViewOnly { get; set; }
        public string? ViewType { get; set; }
        public List<Inv_InventoryBalanceLot> Lst_Inv_InventoryBalanceLot { get; set; } = new();
        public List<InvF_InventoryOutInstLot> Lst_InvF_InventoryOutInstLot { get; set; } = new();
    }
}

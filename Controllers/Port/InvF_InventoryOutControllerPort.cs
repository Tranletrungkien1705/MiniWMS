using System.Globalization;
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
        /// Join Inv_InventoryBalanceSerial với Mst_Inventory (để lọc theo Mst_Inventory.InvBUPattern).
        /// Nguồn:
        ///  - Lọc Lst_InvF_InventoryOutInstSerial bằng C# Ordinal .Equals(ProductCode) (dòng 2092).
        ///  - Nếu có IF_InvOutNo nhưng không tìm thấy phiếu => ném IndexOutOfRangeException (dòng 2097) => HTTP 400.
        ///  - Khi body rỗng, ifInvOutNo rỗng: lấy IF_InvOutStatus từ phiếu đầu tiên (OUT001).
        /// </summary>
        [HttpPost("serial")]
        public async Task<IActionResult> Serial([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SerialReq? req)
        {
            var ifInvOutNo = req?.IF_InvOutNo;
            var productCode = req?.ProductCode;
            var invBUPattern = req?.InvBUPattern;
            var productCodeUser = req?.ProductCodeUser;
            var productName = req?.ProductName;

            try
            {
                var qBal = from b in db.Set<Inv_InventoryBalanceSerial>()
                           join inv in db.Set<Mst_Inventory>() on b.InvCode equals inv.InvCode into invGroup
                           from inv in invGroup.DefaultIfEmpty()
                           select new { b, inv };

                if (!string.IsNullOrEmpty(productCode))
                {
                    qBal = qBal.Where(x => EF.Functions.Like((x.b.ProductCode ?? "").ToLower(), PortQuery.Equal(productCode), PortQuery.Esc));
                }

                if (!string.IsNullOrEmpty(invBUPattern))
                {
                    var cleanPat = invBUPattern.TrimEnd('%');
                    qBal = qBal.Where(x => x.inv != null && EF.Functions.Like((x.inv.InvBUPattern ?? "").ToLower(), PortQuery.Contains(cleanPat), PortQuery.Esc));
                }

                var lstBalance = await qBal.OrderBy(x => x.b.SerialNo).Select(x => x.b).ToListAsync();

                var ifInvOutStatus = "";
                var lstInstSerial = new List<InvF_InventoryOutInstSerial>();

                if (!string.IsNullOrEmpty(ifInvOutNo))
                {
                    var header = await db.Set<InvF_InventoryOut>()
                        .FirstOrDefaultAsync(x => EF.Functions.Like((x.IF_InvOutNo ?? "").ToLower(), PortQuery.Equal(ifInvOutNo), PortQuery.Esc));
                    if (header == null)
                    {
                        // Nguồn dòng 2097: objRT_InvF_InventoryOut.Lst_InvF_InventoryOut[0] => IndexOutOfRangeException
                        throw new IndexOutOfRangeException($"Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')");
                    }

                    ifInvOutStatus = header.IF_InvOutStatus ?? "";

                    var qInstList = await db.Set<InvF_InventoryOutInstSerial>()
                        .Where(x => EF.Functions.Like((x.IF_InvOutNo ?? "").ToLower(), PortQuery.Equal(ifInvOutNo), PortQuery.Esc))
                        .OrderBy(x => x.SerialNo)
                        .ToListAsync();

                    if (!string.IsNullOrEmpty(productCode))
                    {
                        // Nguồn dòng 2092: x.ProductCodeRoot.Equals(ProductCode) (Ordinal phân biệt hoa thường trên C#)
                        lstInstSerial = qInstList.Where(x => string.Equals(x.ProductCodeRoot, productCode, StringComparison.Ordinal)).ToList();
                    }
                    else
                    {
                        lstInstSerial = qInstList;
                    }
                }
                else
                {
                    var firstHeader = await db.Set<InvF_InventoryOut>().OrderBy(x => x.IF_InvOutNo).FirstOrDefaultAsync();
                    if (firstHeader != null)
                    {
                        ifInvOutStatus = firstHeader.IF_InvOutStatus ?? "";
                    }
                }

                return Ok(new SerialResult
                {
                    Success = true,
                    ViewName = "Serial",
                    IF_InvOutNo = ifInvOutNo,
                    ProductCode = productCode,
                    ProductCodeUser = productCodeUser,
                    ProductName = productName,
                    IF_InvOutStatus = ifInvOutStatus,
                    Lst_Inv_InventoryBalanceSerial = lstBalance,
                    Lst_InvF_InventoryOutInstSerial = lstInstSerial
                });
            }
            catch (Exception ex)
            {
                return PortResult.Fail("InvF_InventoryOut_Get", "Exception", ex.Message);
            }
        }

        /// <summary>
        /// port 1:1 InvF_InventoryOutController.cs:2122-2286 — ShowLoDetail.
        /// Chuỗi: gate → WA_Inv_InventoryBalanceLot_Get (Inventory.cs:5378) + WA_InvF_InventoryOut_Get (Form.cs:33724).
        /// Join Inv_InventoryBalanceLot với Mst_Inventory (để lọc theo Mst_Inventory.InvBUPattern).
        /// </summary>
        [HttpPost("showlodetail")]
        public async Task<IActionResult> ShowLoDetail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ShowLoDetailReq? req)
        {
            var ifInvOutNo = req?.IF_InvOutNo;
            var productCode = req?.ProductCode;
            var productCodeBase = req?.ProductCodeBase;
            var invBUPattern = req?.invBUPattern;
            var valconvertStr = req?.valconvert;
            var listLotStr = req?.listLot;
            var curLots = req?.Lst_InvF_InventoryOutInstLotCur;
            var viewType = req?.ViewType;
            var viewonly = req?.viewonly;

            try
            {
                var qBal = from b in db.Set<Inv_InventoryBalanceLot>()
                           join inv in db.Set<Mst_Inventory>() on b.InvCode equals inv.InvCode into invGroup
                           from inv in invGroup.DefaultIfEmpty()
                           select new { b, inv };

                if (!string.IsNullOrEmpty(productCodeBase))
                {
                    qBal = qBal.Where(x => EF.Functions.Like((x.b.ProductCode ?? "").ToLower(), PortQuery.Equal(productCodeBase), PortQuery.Esc));
                }

                if (!string.IsNullOrEmpty(invBUPattern))
                {
                    var cleanPat = invBUPattern.TrimEnd('%');
                    qBal = qBal.Where(x => x.inv != null && EF.Functions.Like((x.inv.InvBUPattern ?? "").ToLower(), PortQuery.Contains(cleanPat), PortQuery.Esc));
                }

                var listBalance = await qBal.OrderBy(x => x.b.ProductLotNo).Select(x => x.b).ToListAsync();

                double valconvert = 0;
                if (!string.IsNullOrEmpty(valconvertStr))
                {
                    double.TryParse(valconvertStr, NumberStyles.Any, CultureInfo.InvariantCulture, out valconvert);
                }

                if (valconvert > 0)
                {
                    foreach (var it in listBalance)
                    {
                        var qty = it.QtyTotalOK ?? 0;
                        it.QtyTotalOK = Math.Round(qty / valconvert, 2);
                    }
                }

                var ifInvOutStatus = "";
                var listInstLot = new List<InvF_InventoryOutInstLot>();

                if (!string.IsNullOrEmpty(ifInvOutNo))
                {
                    var header = await db.Set<InvF_InventoryOut>()
                        .FirstOrDefaultAsync(x => EF.Functions.Like((x.IF_InvOutNo ?? "").ToLower(), PortQuery.Equal(ifInvOutNo), PortQuery.Esc));
                    if (header != null)
                    {
                        ifInvOutStatus = header.IF_InvOutStatus ?? "";
                    }

                    var qInst = db.Set<InvF_InventoryOutInstLot>()
                        .Where(x => EF.Functions.Like((x.IF_InvOutNo ?? "").ToLower(), PortQuery.Equal(ifInvOutNo), PortQuery.Esc));

                    if (!string.IsNullOrEmpty(productCode))
                    {
                        qInst = qInst.Where(x => EF.Functions.Like(
                            (x.ProductCodeRoot ?? "").ToLower(),
                            PortQuery.Equal(productCode),
                            PortQuery.Esc));
                    }

                    listInstLot = await qInst.OrderBy(x => x.ProductLotNo).ToListAsync();

                    if (valconvert > 0)
                    {
                        foreach (var it in listInstLot)
                        {
                            var qty = it.Qty ?? 0;
                            it.Qty = Math.Round(qty / valconvert, 2);
                        }
                    }
                }

                var resultList = new List<InvF_InventoryOutInstLot>();

                if (curLots != null && curLots.Count > 0)
                {
                    resultList.AddRange(curLots);
                }
                else if (listInstLot.Count > 0)
                {
                    resultList.AddRange(listInstLot);
                }
                else if (!string.IsNullOrEmpty(listLotStr))
                {
                    var parts = listLotStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                    {
                        var lotTrim = p.Trim();
                        if (string.IsNullOrEmpty(lotTrim)) continue;

                        var bal = listBalance.FirstOrDefault(x =>
                            string.Equals(x.ProductLotNo, lotTrim, StringComparison.OrdinalIgnoreCase));
                        resultList.Add(new InvF_InventoryOutInstLot
                        {
                            IF_InvOutNo = ifInvOutNo,
                            ProductCode = productCode,
                            ProductCodeRoot = productCode,
                            ProductLotNo = lotTrim,
                            Qty = bal?.QtyTotalOK ?? 0
                        });
                    }
                }

                return Ok(new ShowLoDetailResult
                {
                    Success = true,
                    ViewName = "Lo",
                    IF_InvOutNo = ifInvOutNo,
                    ProductCode = productCode,
                    ProductCodeBase = productCodeBase,
                    IF_InvOutStatus = ifInvOutStatus,
                    ViewOnly = viewonly,
                    ViewType = viewType,
                    Lst_Inv_InventoryBalanceLot = listBalance,
                    Lst_InvF_InventoryOutInstLot = resultList
                });
            }
            catch (Exception ex)
            {
                return PortResult.Fail("InvF_InventoryOut_Get", "Exception", ex.Message);
            }
        }

        /// <summary>
        /// port 1:1 InvF_InventoryOutController.cs:2290-2344 — ComboAppr.
        /// Chuỗi: gate → WA_InvF_InventoryOut_Get (Form.cs:33724 -> InvF_InventoryOut_Get_New20210707:33895)
        /// Khi ifInvOutNo rỗng: trả về tất cả DTL (5 dòng theo seed OUT001/OUT002).
        /// Khi có ifInvOutNo: kiểm tra header tồn tại (case-insensitive), trả DTL của header đó.
        /// </summary>
        [HttpPost("comboappr")]
        public async Task<IActionResult> ComboAppr([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ComboApprReq? req)
        {
            var ifInvOutNo = req?.IF_InvOutNo;
            var productCode = req?.ProductCode;
            var productCodeUser = req?.ProductCodeUser;
            var productName = req?.ProductName;

            try
            {
                var lstDtl = new List<InvF_InventoryOutDtl>();

                if (string.IsNullOrEmpty(ifInvOutNo))
                {
                    lstDtl = await db.Set<InvF_InventoryOutDtl>()
                        .OrderBy(x => x.IF_InvOutNo)
                        .ThenBy(x => x.ProductCode)
                        .ToListAsync();
                }
                else
                {
                    var headerExists = await db.Set<InvF_InventoryOut>()
                        .AnyAsync(x => EF.Functions.Like((x.IF_InvOutNo ?? "").ToLower(), PortQuery.Equal(ifInvOutNo), PortQuery.Esc));

                    if (headerExists)
                    {
                        lstDtl = await db.Set<InvF_InventoryOutDtl>()
                            .Where(x => EF.Functions.Like((x.IF_InvOutNo ?? "").ToLower(), PortQuery.Equal(ifInvOutNo), PortQuery.Esc))
                            .OrderBy(x => x.ProductCode)
                            .ToListAsync();
                    }
                }

                return Ok(new ComboApprResult
                {
                    Success = true,
                    ViewName = "ComboAppr",
                    IF_InvOutNo = ifInvOutNo,
                    ProductCode = productCode,
                    ProductCodeUser = productCodeUser,
                    ProductName = productName,
                    Lst_InvF_InventoryOutDtl = lstDtl
                });
            }
            catch (Exception ex)
            {
                return PortResult.Fail("InvF_InventoryOut_Get", "Exception", ex.Message);
            }
        }

        /// <summary>
        /// port 1:1 InvF_InventoryOutController.cs:3602-3711 — GetTonKho.
        /// Chuỗi gọi: gate GetTonKho
        ///   -> #1 WA_Inv_InventoryBalance_Get (InvInventoryBalanceController.cs:20 -> Inventory.cs:4776 -> Inv_InventoryBalance_Get:4902)
        ///        Filter: Inv_InventoryBalance.ProductCode = productCodeBase (nếu có), Mst_Inventory.InvBUPattern like InvBUPattern (nếu có),
        ///                Inv_InventoryBalance.QtyTotalOK > 0.
        ///        Quy đổi: it.QtyTotalOK = Math.Round(Convert.ToDouble(it.QtyTotalOK) / valConvert, 2) (khi valConvert > 0).
        ///   -> #2 WA_InvF_InventoryOut_Get (InvFInventoryOutController.cs:20 -> Form.cs:33724 -> InvF_InventoryOut_Get_New20210707:33895)
        ///        WhereClauseInvF_InventoryOut với IF_InvOutNo = IF_InvOutNo.
        /// </summary>
        [HttpPost("gettonkho")]
        public async Task<IActionResult> GetTonKho([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GetTonKhoReq? req)
        {
            try
            {
                var ifInvOutNo = req?.IF_InvOutNo;
                var productCode = req?.ProductCode;
                var productCodeBase = req?.ProductCodeBase;
                var invBUPattern = req?.InvBUPattern;
                var valConvertStr = req?.ValConvert;
                var productCodeUser = req?.ProductCodeUser;
                var productName = req?.ProductName;
                var viewType = req?.ViewType;
                var onlyView = req?.onlyView ?? "0";

                var lstInv_InventoryBalance = new List<Inv_InventoryBalance>();
                var lst_InvF_InventoryOutDtl = new List<InvF_InventoryOutDtl>();

                var qBal = from b in db.Set<Inv_InventoryBalance>()
                           join inv in db.Set<Mst_Inventory>() on b.InvCode equals inv.InvCode into invGroup
                           from inv in invGroup.DefaultIfEmpty()
                           select new { b, inv };

                if (!string.IsNullOrEmpty(productCodeBase))
                {
                    qBal = qBal.Where(x => EF.Functions.Like((x.b.ProductCode ?? "").ToLower(), PortQuery.Equal(productCodeBase), PortQuery.Esc));
                }

                if (!string.IsNullOrEmpty(invBUPattern))
                {
                    var cleanPat = invBUPattern.TrimEnd('%');
                    qBal = qBal.Where(x => x.inv != null && EF.Functions.Like((x.inv.InvBUPattern ?? "").ToLower(), PortQuery.Contains(cleanPat), PortQuery.Esc));
                }

                qBal = qBal.Where(x => (x.b.QtyTotalOK ?? 0) > 0);

                lstInv_InventoryBalance = await qBal.OrderBy(x => x.b.InvCode).Select(x => x.b).ToListAsync();

                double valConvert = 0;
                if (!string.IsNullOrEmpty(valConvertStr))
                {
                    double.TryParse(valConvertStr, NumberStyles.Any, CultureInfo.InvariantCulture, out valConvert);
                }

                if (valConvert > 0)
                {
                    foreach (var it in lstInv_InventoryBalance)
                    {
                        var qty = it.QtyTotalOK ?? 0;
                        it.QtyTotalOK = Math.Round(qty / valConvert, 2);
                    }
                }

                if (!string.IsNullOrEmpty(ifInvOutNo))
                {
                    var headerExists = await db.Set<InvF_InventoryOut>()
                        .AnyAsync(x => EF.Functions.Like((x.IF_InvOutNo ?? "").ToLower(), PortQuery.Equal(ifInvOutNo), PortQuery.Esc));

                    if (headerExists)
                    {
                        var qDtl = db.Set<InvF_InventoryOutDtl>()
                            .Where(x => EF.Functions.Like((x.IF_InvOutNo ?? "").ToLower(), PortQuery.Equal(ifInvOutNo), PortQuery.Esc));

                        if (!string.IsNullOrEmpty(productCode))
                        {
                            qDtl = qDtl.Where(x => EF.Functions.Like(
                                (x.ProductCode ?? "").ToLower(),
                                PortQuery.Equal(productCode),
                                PortQuery.Esc));
                        }

                        lst_InvF_InventoryOutDtl = await qDtl.OrderBy(x => x.ProductCode).ToListAsync();
                    }
                }

                return Ok(new GetTonKhoResult
                {
                    Success = true,
                    ViewName = "ShowTonKho",
                    IF_InvOutNo = ifInvOutNo,
                    ProductCode = productCode,
                    ProductCodeBase = productCodeBase,
                    ProductCodeUser = productCodeUser,
                    ProductName = productName,
                    ViewType = viewType,
                    OnlyView = onlyView,
                    Lst_Inv_InventoryBalance = lstInv_InventoryBalance,
                    Lst_InvF_InventoryOutDtl = lst_InvF_InventoryOutDtl
                });
            }
            catch (Exception ex)
            {
                return PortResult.Fail("InvF_InventoryOut_Get", "Exception", ex.Message);
            }
        }

        /// <summary>
        /// port 1:1 InvF_InventoryOutController.cs:3792-3873 — SearchOrd_OrderSO.
        /// Chuỗi: gate SearchOrd_OrderSO -> strWhereClause_Ord_OrderSOSearch (InvF_InventoryOutController.cs:2575-2615)
        ///        -> Ord_OrderSOService.Instance.WA_Ord_OrderSO_Get(objRQ_Ord_OrderSO) (dịch vụ ngoài DMS).
        /// Nghiệp vụ lọc trên bảng Ord_OrderSO:
        ///   - OrderStatus = 'APPROVE' (bắt buộc)
        ///   - NetworkID = NetworkID (nếu có)
        ///   - OrgID = OrgID (nếu có)
        ///   - OrderNo like '%OrderNo%' (nếu có)
        ///   - CustomerName like '%CustomerName%' (nếu có)
        ///   - OrderCF03 like '%vin%' (nếu có)
        ///   - CreateDTimeUTC >= ApprDTimeUTCFrom (00:00:00) (nếu có)
        ///   - CreateDTimeUTC <= ApprDTimeUTCTo (23:59:59) (nếu có)
        /// Phân trang: Page (mặc định 0), RecordCount (mặc định 10). Trả PageInfo&lt;Ord_OrderSO&gt;.
        /// </summary>
        [HttpPost("searchord_orderso")]
        public async Task<IActionResult> SearchOrd_OrderSO([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchOrd_OrderSOReq? req)
        {
            var orderno = req?.OrderNo ?? "";
            var apprdtimefrom = req?.ApprDTimeFrom ?? "";
            var apprdtimeto = req?.ApprDTimeTo ?? "";
            var customername = req?.CustomerName ?? "";
            var vin = req?.Vin ?? "";
            var page = req?.Page ?? 0;
            var recordcount = req?.RecordCount ?? 10;
            var networkId = req?.NetworkID;
            var orgId = req?.OrgID;
            var utcOffset = req?.UtcOffset ?? 7.0;

            try
            {
                var allList = await db.Set<Ord_OrderSO>().ToListAsync();
                var q = allList.AsEnumerable();

                // Nguồn dòng 2612: sbSql.AddWhereClause(Tbl_Ord_OrderSO + TblOrd_OrderSO.OrderStatus, "APPROVE", "=");
                q = q.Where(x => string.Equals((x.OrderStatus ?? "").Trim(), "APPROVE", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(networkId))
                {
                    q = q.Where(x => string.Equals((x.NetworkID ?? "").Trim(), networkId.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(orgId))
                {
                    q = q.Where(x => string.Equals((x.OrgID ?? "").Trim(), orgId.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(orderno))
                {
                    q = q.Where(x => (x.OrderNo ?? "").Contains(orderno.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(customername))
                {
                    q = q.Where(x => (x.CustomerName ?? "").Contains(customername.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(vin))
                {
                    q = q.Where(x => (x.OrderCF03 ?? "").Contains(vin.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(apprdtimefrom) &&
                    DateTime.TryParse(apprdtimefrom.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtFrom))
                {
                    var dtUtcFrom = dtFrom.Date.AddHours(-utcOffset);
                    q = q.Where(x => x.CreateDTimeUTC >= dtUtcFrom);
                }

                if (!string.IsNullOrWhiteSpace(apprdtimeto) &&
                    DateTime.TryParse(apprdtimeto.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtTo))
                {
                    var dtUtcTo = dtTo.Date.AddDays(1).AddTicks(-1).AddHours(-utcOffset);
                    q = q.Where(x => x.CreateDTimeUTC <= dtUtcTo);
                }

                var filteredList = q.ToList();
                var itemCount = filteredList.Count;
                var pageCount = 0;
                if (recordcount > 0)
                {
                    pageCount = (itemCount % recordcount == 0) ? (itemCount / recordcount) : (itemCount / recordcount + 1);
                }

                var recordStart = page * recordcount;
                var listOrd_OrderSO = new List<Ord_OrderSO>();

                if (recordcount > 0 && recordStart >= 0 && itemCount > 0)
                {
                    listOrd_OrderSO = filteredList
                        .OrderByDescending(x => x.CreateDTimeUTC)
                        .ThenBy(x => x.OrderNo)
                        .Skip((int)Math.Min(recordStart, int.MaxValue))
                        .Take((int)Math.Min(recordcount, int.MaxValue))
                        .ToList();
                }

                var pageInfo = new PageInfo<Ord_OrderSO>(
                    DataList: listOrd_OrderSO,
                    ItemCount: itemCount,
                    PageCount: pageCount,
                    PageIndex: page,
                    PageSize: recordcount
                );

                return Ok(new SearchOrd_OrderSOResult
                {
                    Success = true,
                    ViewName = "BindData_Ord_OrderSO",
                    Data = pageInfo,
                    ListOrd_OrderSO = listOrd_OrderSO,
                    StartCount = (page * recordcount).ToString()
                });
            }
            catch (Exception ex)
            {
                return PortResult.Fail("InvF_InventoryOut_SearchOrd_OrderSO", "Exception", ex.Message);
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

    /// <summary>
    /// Body cho action ComboAppr (nguồn InvF_InventoryOutController.cs:2290 — 4 tham số).
    /// `ProductCode` và `ProductCodeUser/ProductName` khớp chữ ký gate nhưng nguồn KHÔNG dùng để lọc
    /// (dòng 2419-2422 comment) — chỉ để ViewBag / hiển thị.
    /// </summary>
    public class ComboApprReq
    {
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
    }

    /// <summary>
    /// Kết quả ComboAppr — thay `JsonView("ComboAppr", Lst_InvF_InventoryOutDtl)` của nguồn.
    /// Nguồn: Lst_InvF_InventoryOutDtl rỗng khi không có header (dòng 2331 — Success, KHÔNG Fail).
    /// </summary>
    public class ComboApprResult
    {
        public bool Success { get; set; } = true;
        public string? ViewName { get; set; } = "ComboAppr";
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
        public List<InvF_InventoryOutDtl> Lst_InvF_InventoryOutDtl { get; set; } = new();
    }

    /// <summary>Body cho action GetTonKho (nguồn InvF_InventoryOutController.cs:3602 — 9 tham số).</summary>
    public class GetTonKhoReq
    {
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductCodeBase { get; set; }
        public string? InvBUPattern { get; set; }
        public string? ValConvert { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
        public string? ViewType { get; set; }
        public string? onlyView { get; set; }
    }

    /// <summary>Kết quả GetTonKho — thay ViewBag + JsonView("ShowTonKho", lstInv_InventoryBalance) của nguồn.</summary>
    public class GetTonKhoResult
    {
        public bool Success { get; set; } = true;
        public string? ViewName { get; set; } = "ShowTonKho";
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductCodeBase { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
        public string? ViewType { get; set; }
        public string? OnlyView { get; set; } = "0";
        public List<Inv_InventoryBalance> Lst_Inv_InventoryBalance { get; set; } = new();
        public List<InvF_InventoryOutDtl> Lst_InvF_InventoryOutDtl { get; set; } = new();
    }

    /// <summary>
    /// Body cho action SearchOrd_OrderSO (nguồn InvF_InventoryOutController.cs:3792-3800).
    /// </summary>
    public class SearchOrd_OrderSOReq
    {
        public string? OrderNo { get; set; } = "";
        public string? ApprDTimeFrom { get; set; } = "";
        public string? ApprDTimeTo { get; set; } = "";
        public string? CustomerName { get; set; } = "";
        public string? Vin { get; set; } = "";
        public string? Init { get; set; } = "init";
        public int Page { get; set; } = 0;
        public int RecordCount { get; set; } = 10;
        public string? NetworkID { get; set; }
        public string? OrgID { get; set; }
        public double? UtcOffset { get; set; } = 7.0;
    }

    /// <summary>
    /// Kết quả SearchOrd_OrderSO — thay JsonView("BindData_Ord_OrderSO", pageInfo) của nguồn.
    /// </summary>
    public class SearchOrd_OrderSOResult
    {
        public bool Success { get; set; } = true;
        public string? ViewName { get; set; } = "BindData_Ord_OrderSO";
        public PageInfo<Ord_OrderSO>? Data { get; set; }
        public List<Ord_OrderSO> ListOrd_OrderSO { get; set; } = new();
        public string? StartCount { get; set; }
    }
}

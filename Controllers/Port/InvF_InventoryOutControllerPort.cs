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
        ///
        /// Gate dựng where cho balance-serial (2026-2039):
        ///   Inv_InventoryBalanceSerial.ProductCode = '&lt;ProductCode&gt;' (nếu có)
        ///   AND Mst_Inventory.InvBUPattern like '&lt;invBUPattern&gt;' (nếu có)
        ///   AND Mst_Inventory.FlagActive = '1'
        /// Sau đó gọi Inv_InventoryBalanceSerial_GetX (Inventory.cs:6283):
        ///   #tbl_Inv_InventoryBalanceSerial_Filter_Draft (6343-6348) join Mst_Inventory mi (6351-6353) on OrgID+InvCode,
        ///   htSpCols (6417-6431) đăng ký cả Inv_InventoryBalanceSerial.(iiifl.) LẪN Mst_Inventory.(mi.) nên các filter áp được.
        ///   (Bản đọc trước của tôi bị cắt ở dòng 6381 và hiểu nhầm join là broken — đã sửa, xem GAP dòng 6400-6403.)
        /// Filter phụ của WA_InvF_InventoryOut_Get (Form.cs:33724): where InvF_InventoryOut.IF_InvOutNo = '&lt;IF_InvOutNo&gt;' (WhereClauseInvF_InventoryOut 2375-2378).
        /// Sau đó gate lọc Lst_InvF_InventoryOutInstSerial với x.ProductCodeRoot.Equals(ProductCode) (2092 — Ordinal, case-sensitive).
        /// IF_InvOutStatus = Lst_InvF_InventoryOut[0].IF_InvOutStatus (2097 — IndexOutOfRange nếu không có dòng).
        ///
        /// GAP:
        ///  - View server-side (JsonView("Serial")) ⇒ trả JSON phẳng tương đương ViewBag.
        ///  - `#tbl_Mst_NNT_ViewAbility` + `#tbl_Mst_Inventory_ViewAbility` (permission view) — không có phiên ở đích ⇒ bỏ.
        ///  - Mst_Inventory ở đích (Models/Port/Mst_Inventory.cs) KHÔNG có cột OrgID ⇒ join Inv_InventoryBalanceSerial↔Mst_Inventory theo InvCode (nguồn: OrgID + InvCode, Inventory.cs:6351-6353).
        ///  - Ft_RecordStart/Ft_RecordCount nguồn lấy từ BaseController (paged); port trả đủ danh sách (không paging) vì Serial của 1 phiếu luôn nhỏ.
        ///  - `Lst_InvF_InventoryOut[0]` (2097): nguồn ném IndexOutOfRange khi không có dòng; port trả Fail(InvF_InventoryOut_Get, Check.IF_InvOutNoNotFound).
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

                // Inv_InventoryBalanceSerial.ProductCode = '&lt;ProductCode&gt;' (gate 2030-2032, SQL Server `=` case-insensitive).
                if (!string.IsNullOrEmpty(productCode))
                {
                    var p = PortQuery.Equal(productCode.Trim().ToLowerInvariant());
                    qBal = qBal.Where(x => EF.Functions.Like((x.bal.ProductCode ?? "").ToLower(), p, PortQuery.Esc));
                }

                // Mst_Inventory.InvBUPattern like '&lt;invBUPattern&gt;' (gate 2034-2036). Người dùng có thể truyền wildcard `%`;
                // escape `\` và `_` nhưng giữ `%` để khớp `like` của SQL Server.
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
                    // Nguồn dòng 2097 truy cập [0] sẽ IndexOutOfRange ⇒ đổi thành mã lỗi (xem GAP).
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
}

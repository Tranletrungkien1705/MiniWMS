using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.SearchProduct — nguồn `InvF_InventoryInController.cs:2439-2614` (#93).
/// Chuỗi nguồn:
///   - Gate: `InvF_InventoryInController.cs:2439-2614`
///   - Dịch vụ ngoài: `OS_Ord_OrderPDService.Instance.WA_Ord_OrderPD_Get(objRQ_Ord_OrderPD, addressAPIs)`
///     (gọi API LQDMS ngoài để lấy thông tin đơn hàng sản xuất `Ord_OrderPD` và chi tiết `Ord_OrderPDDtl`).
///     Bảng `Ord_OrderPD` và `Ord_OrderPDDtl` là bảng nghiệp vụ của hệ thống ngoài LQDMS, không có bảng nội bộ trực tiếp.
///     Target MiniWMS hỗ trợ tìm kiếm sản phẩm theo đơn hàng bằng entity mô phỏng `Ord_OrderPD` / `Ord_OrderPDDtl` (hoặc mock LQDMS)
///     và join/lookup sang `Mst_Product` nội bộ theo `strWhereGetProductId` (FlagActive='1', FlagBuy='1', ProductType='PRODUCT', ProductCode).
///   - Hàm phụ:
///       `strWhereClause_Ord_OrderPD_Search` (gate 2830-2843): `Ord_OrderPD.OrderPDNo = refno`
///       `strWhereGetProductId` (gate 2980-3001): `Mst_Product.FlagActive = '1' AND FlagBuy = '1' AND ProductType = 'PRODUCT' AND ProductCode = item.ProductCode`
///   - Kết quả trả về khi thành công:
///       `{ Success = true, LstRpt_OrderSummary_TotalProductForInv = listOS_OrderPDUI }`
///   - Kết quả khi refno không tìm thấy / rỗng:
///       `{ Success = false, Messages = ["Số RefNo " + refno + " không tồn tại trên hệ thống"] }`
/// </summary>
public partial class InvF_InventoryInControllerPort
{
    public class SearchProductReq
    {
        /// <summary>Nguồn dòng 2439: tham số `refno`.</summary>
        public string? RefNo { get; set; }
    }

    /// <summary>
    /// Nguồn `Ord_OrderPDUI.cs` và gate dòng 2557-2570: các thuộc tính của sản phẩm theo đơn hàng.
    /// </summary>
    public class SearchProductItemUI
    {
        public string? ProductCode { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
        public string? PlanQty { get; set; }
        public string? OrderPDNo { get; set; }
        public string? OrderPDNoSys { get; set; }
        public string? UPIn { get; set; }
        public string? UnitCode { get; set; }
        public string? CustomerCodeSys { get; set; }
        public string? FlagLot { get; set; }
        public string? FlagSerial { get; set; }
    }

    public class SearchProductResult
    {
        public bool Success { get; set; }
        public List<SearchProductItemUI>? LstRpt_OrderSummary_TotalProductForInv { get; set; }
        public List<string>? Messages { get; set; }
    }

    [HttpPost("api/invf_inventoryin/searchproduct")]
    public async Task<IActionResult> SearchProduct([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchProductReq? req)
    {
        var refno = req?.RefNo?.Trim() ?? "";

        // Nguồn dòng 2461-2498: Tìm kiếm đơn hàng sản xuất theo refno
        // Nếu refno rỗng hoặc không tìm thấy trong Ord_OrderPD:
        if (string.IsNullOrEmpty(refno))
        {
            return Ok(new SearchProductResult
            {
                Success = false,
                Messages = new List<string> { $"Số RefNo {refno} không tồn tại trên hệ thống" }
            });
        }

        // Truy vấn Ord_OrderPD theo OrderPDNo (so sánh chuỗi chuẩn không phân biệt hoa thường)
        var order = await db.Set<Ord_OrderPD>().AsNoTracking()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrderPDNo ?? "").ToLower(), PortQuery.Equal(refno), PortQuery.Esc));

        if (order == null)
        {
            return Ok(new SearchProductResult
            {
                Success = false,
                Messages = new List<string> { $"Số RefNo {refno} không tồn tại trên hệ thống" }
            });
        }

        // Lấy chi tiết Ord_OrderPDDtl theo OrderPDNoSys (hoặc OrderPDNo)
        var orderNoSys = order.OrderPDNoSys ?? "";
        var listDtl = await db.Set<Ord_OrderPDDtl>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.OrderPDNoSys ?? "").ToLower(), PortQuery.Equal(orderNoSys), PortQuery.Esc))
            .ToListAsync();

        if (listDtl.Count == 0)
        {
            return Ok(new SearchProductResult
            {
                Success = false,
                Messages = new List<string> { $"Số RefNo {refno} không tồn tại trên hệ thống" }
            });
        }

        var listOS_OrderPDUI = new List<SearchProductItemUI>();

        // Nguồn gate dòng 2514-2594: Duyệt từng chi tiết và tìm Mst_Product theo strWhereGetProductId
        // FlagActive = "1", FlagBuy = "1", ProductType = "PRODUCT", ProductCode = item.ProductCode
        foreach (var item in listDtl)
        {
            var prdCode = item.ProductCode ?? "";
            var prd = await db.Set<Mst_Product>().AsNoTracking()
                .Where(p => EF.Functions.Like((p.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((p.FlagBuy ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((p.ProductType ?? "").ToLower(), PortQuery.Equal("product"), PortQuery.Esc)
                         && EF.Functions.Like((p.ProductCode ?? "").ToLower(), PortQuery.Equal(prdCode), PortQuery.Esc))
                .FirstOrDefaultAsync();

            if (prd != null)
            {
                listOS_OrderPDUI.Add(new SearchProductItemUI
                {
                    ProductCode = prd.ProductCode ?? "",
                    ProductCodeUser = prd.ProductCodeUser ?? "",
                    ProductName = prd.ProductName ?? "",
                    // Nguồn dòng 2562: CUtils.StrValue(item.PGInvInRemainQty)
                    PlanQty = item.PGInvInRemainQty?.ToString() ?? "",
                    OrderPDNo = order.OrderPDNo ?? "",
                    OrderPDNoSys = order.OrderPDNoSys ?? "",
                    UPIn = prd.UPBuy.HasValue ? prd.UPBuy.Value.ToString() : "",
                    UnitCode = prd.UnitCode ?? "",
                    CustomerCodeSys = order.CustomerCodeSys ?? "",
                    FlagLot = prd.FlagLot ?? "",
                    FlagSerial = prd.FlagSerial ?? ""
                });
            }
        }

        return Ok(new SearchProductResult
        {
            Success = true,
            LstRpt_OrderSummary_TotalProductForInv = listOS_OrderPDUI
        });
    }
}

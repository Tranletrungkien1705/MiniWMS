using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.GetProductExactly — nguồn InvF_InventoryInController.cs:1880-2016.
/// Chuỗi nguồn: gate 1880-2016 → WA_Mst_Product_Get (BaseController.cs:2926-2936) → WAS_Mst_Product_Get
///   (ProductCenter.MasterData.cs:5714-5872) → Mst_Product_Get (ProductCenter.MasterData.cs:5276-...).
/// Bổ sung: strWhereGetProductId (gate 2980-3001), strWhereClause_Mst_Product_Get_Base (gate 3108-3133),
/// GetPrdBase (gate 2620-2654).
/// GAP: ReportsService.Instance.WA_Rpt_Inv_InvBalance_LastUpdInvByProduct (gate dòng 1970) — dịch vụ BÁO CÁO NGOÀI
///      (qua `addressAPIs`), biz WAS_Rpt_Inv_InvBalance_LastUpdInvByProduct tồn tại nhưng dùng Mst_Inventory.OrgID /
///      InvBUCode / InvBUPattern (target Mst_Inventory entity thiếu OrgID + InvBUCode) và Mst_Product.ValConvert,
///      ProductCodeBase đầy đủ. Port bỏ qua bước này; InvCodeSuggest = "" (đúng hành vi khi báo cáo trả rỗng).
/// GAP: Ft_RecordStart/Ft_RecordCount của BaseController không có ở đích — port trả TẤT CẢ dòng khớp filter
///      (thực tế prdid thường chỉ khớp 1 sản phẩm nên không khác biệt).
/// </summary>
public partial class InvF_InventoryInControllerPort
{
    public class GetProductExactlyReq
    {
        /// <summary>Nguồn dòng 1880: tham số `invcode` (dùng để gọi ReportsService — port bỏ qua, xem GAP).</summary>
        public string? InvCode { get; set; }
        /// <summary>Nguồn dòng 1880: tham số `prdid` (mặc định "").</summary>
        public string? PrdId { get; set; }
    }

    /// <summary>Nguồn dòng 1993-2008: các trường của Mst_ProductUI được gán trong gate.</summary>
    public class GetProductExactlyItem
    {
        public string? ProductCode { get; set; }
        public string? ProductCodeBase { get; set; }
        public string? ProductCodeRoot { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
        public string? ProductType { get; set; }
        public string? UnitCode { get; set; }
        public double? UPBuy { get; set; }
        public double? UPSell { get; set; }
        public string? FlagActive { get; set; }
        public string? FlagLot { get; set; }
        public string? FlagSerial { get; set; }
        public string? InvCodeSuggest { get; set; }
    }

    [HttpPost("api/invf_inventoryin/getproductexactly")]
    public async Task<IActionResult> GetProductExactly([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GetProductExactlyReq? req)
    {
        // Nguồn dòng 1890: objMst_Product.ProductCode = prdid
        var prdid = req?.PrdId ?? "";

        // strWhereGetProductId (gate 2980-3001): FlagActive='1' AND FlagBuy='1' AND ProductType='PRODUCT'
        //   + if (ProductCode != null) ProductCode = prdid
        var q1 = db.Set<Mst_Product>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagBuy ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.ProductType ?? "").ToLower(), PortQuery.Equal("PRODUCT"), PortQuery.Esc));
        if (!string.IsNullOrEmpty(prdid))
        {
            var pat = PortQuery.Equal(prdid);
            q1 = q1.Where(x => EF.Functions.Like((x.ProductCode ?? "").ToLower(), pat, PortQuery.Esc));
        }
        // Nguồn `Mst_Product_Get` bảng tạm `#tbl_Mst_Product_Filter_Draft`
        // order by `mp.OrgID, mp.ProductCode` (ProductCenter.MasterData.cs:5414-5416) — port giữ thứ tự ấy.
        var products1 = await q1.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode).ToListAsync();

        // Nguồn dòng 1924: objMst_ProductBase = objRT_Mst_Product[0] — rỗng thì IndexOutOfRange
        // (PortExceptionFilter trả 400 với mã `InvF_InventoryIn_GetProductExactly`).
        var productBase = products1[0];

        // strWhereClause_Mst_Product_Get_Base (gate 3108-3133): FlagActive='1' AND FlagBuy='1' AND ProductType='PRODUCT'
        //   + if (ProductCodeBase != null) ProductCodeBase = value
        //   + if (ProductCodeRoot != null) ProductCodeRoot = value
        //   + if (ProductCode    != null) ProductCode    != value
        var q2 = db.Set<Mst_Product>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagBuy ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.ProductType ?? "").ToLower(), PortQuery.Equal("PRODUCT"), PortQuery.Esc));
        if (!string.IsNullOrEmpty(productBase.ProductCodeBase))
        {
            var pat = PortQuery.Equal(productBase.ProductCodeBase);
            q2 = q2.Where(x => EF.Functions.Like((x.ProductCodeBase ?? "").ToLower(), pat, PortQuery.Esc));
        }
        if (!string.IsNullOrEmpty(productBase.ProductCodeRoot))
        {
            var pat = PortQuery.Equal(productBase.ProductCodeRoot);
            q2 = q2.Where(x => EF.Functions.Like((x.ProductCodeRoot ?? "").ToLower(), pat, PortQuery.Esc));
        }
        if (!string.IsNullOrEmpty(productBase.ProductCode))
        {
            var pat = PortQuery.Equal(productBase.ProductCode);
            q2 = q2.Where(x => !EF.Functions.Like((x.ProductCode ?? "").ToLower(), pat, PortQuery.Esc));
        }
        var baseList = await q2.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode).ToListAsync();

        // Nguồn dòng 1930-1935: listProduct = [objMst_ProductBase] + lstPrdBase
        var listProduct = new List<Mst_Product> { productBase };
        listProduct.AddRange(baseList);

        // GAP: bỏ qua ReportsService.Instance.WA_Rpt_Inv_InvBalance_LastUpdInvByProduct (gate 1970) —
        //      InvCodeSuggest luôn "" (đúng hành vi khi báo cáo trả rỗng).
        var listProductUI = new List<GetProductExactlyItem>();
        foreach (var itPrd in listProduct)
        {
            listProductUI.Add(new GetProductExactlyItem
            {
                ProductCode = itPrd.ProductCode,
                ProductCodeBase = itPrd.ProductCodeBase,
                ProductCodeRoot = itPrd.ProductCodeRoot,
                ProductCodeUser = itPrd.ProductCodeUser,
                ProductName = itPrd.ProductName,
                ProductType = itPrd.ProductType,
                UnitCode = itPrd.UnitCode,
                UPBuy = itPrd.UPBuy,
                UPSell = itPrd.UPSell,
                FlagActive = itPrd.FlagActive,
                FlagLot = itPrd.FlagLot,
                FlagSerial = itPrd.FlagSerial,
                InvCodeSuggest = ""
            });
        }

        // Nguồn dòng 2015: return Json(new { Data = listProductUI });
        return Ok(new { Data = listProductUI });
    }
}

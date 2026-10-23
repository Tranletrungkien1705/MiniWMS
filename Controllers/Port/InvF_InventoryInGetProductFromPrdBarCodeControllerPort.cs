using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.GetProductFromPrdBarCode — nguồn InvF_InventoryInController.cs:2020-2160.
/// Chuỗi nguồn: gate 2020-2160 → WA_Mst_Product_Get (BaseController.cs:2926-2936) → WAS_Mst_Product_Get
///   (ProductCenter.MasterData.cs:5714-5872) → Mst_Product_Get (ProductCenter.MasterData.cs:5276-...).
/// Bổ sung: strWhereGetProductByPrdBarcode (gate 3163-3180), strWhereClause_Mst_Product_Get_Base (gate 3108-3133),
/// GetPrdBase (gate 2620-2654).
/// GAP: ReportsService.Instance.WA_Rpt_Inv_InvBalance_LastUpdInvByProduct (gate dòng 2114) — dịch vụ BÁO CÁO NGOÀI
///      (qua `addressAPIs`), biz WAS_Rpt_Inv_InvBalance_LastUpdInvByProduct tồn tại nhưng dùng Mst_Inventory.OrgID /
///      InvBUCode / InvBUPattern (target Mst_Inventory entity thiếu OrgID + InvBUCode) và Mst_Product.ValConvert,
///      ProductCodeBase đầy đủ. Port bỏ qua bước này; InvCodeSuggest = "" (đúng hành vi khi báo cáo trả rỗng).
/// GAP: Ft_RecordStart/Ft_RecordCount của BaseController không có ở đích — port trả TẤT CẢ dòng khớp filter.
/// Guard: Nguồn dòng 2064-2067: lstRT_Mst_Product rỗng => Json(new { Success = false, Messages = "Hàng hóa không có trong hệ thống!" }).
/// </summary>
public partial class InvF_InventoryInControllerPort
{
    public class GetProductFromPrdBarCodeReq
    {
        /// <summary>Nguồn dòng 2020: tham số `invcode` (dùng để gọi ReportsService — port bỏ qua, xem GAP).</summary>
        public string? InvCode { get; set; }
        /// <summary>Nguồn dòng 2020: tham số `prdbarcode` (mặc định "").</summary>
        public string? PrdBarCode { get; set; }
    }

    /// <summary>Nguồn dòng 2137-2153: các trường của Mst_ProductUI được gán trong gate.</summary>
    public class GetProductFromPrdBarCodeItem
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

    [HttpPost("api/invf_inventoryin/getproductfromprdbarcode")]
    public async Task<IActionResult> GetProductFromPrdBarCode([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GetProductFromPrdBarCodeReq? req)
    {
        // Nguồn dòng 2028-2031: objMst_Product.ProductBarCode = prdbarcode (mặc định "")
        var prdbarcode = req?.PrdBarCode ?? "";

        // strWhereGetProductByPrdBarcode (gate 3163-3180): FlagActive='1' AND FlagBuy='1' AND ProductType='PRODUCT'
        //   + if (!CUtils.IsNullOrEmpty(data.ProductBarCode)) ProductBarCode = prdbarcode
        var q1 = db.Set<Mst_Product>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagBuy ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.ProductType ?? "").ToLower(), PortQuery.Equal("PRODUCT"), PortQuery.Esc));
        if (!string.IsNullOrEmpty(prdbarcode))
        {
            var pat = PortQuery.Equal(prdbarcode);
            q1 = q1.Where(x => EF.Functions.Like((x.ProductBarCode ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // Nguồn `Mst_Product_Get` bảng tạm `#tbl_Mst_Product_Filter_Draft`
        // order by `mp.OrgID, mp.ProductCode` (ProductCenter.MasterData.cs:5414-5416).
        var products1 = await q1.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode).ToListAsync();

        // Nguồn dòng 2064-2067:
        // if (lstRT_Mst_Product == null || lstRT_Mst_Product.Count == 0)
        // {
        //     return Json(new { Success = false, Messages = "Hàng hóa không có trong hệ thống!" });
        // }
        if (products1 == null || products1.Count == 0)
        {
            return Ok(new { Success = false, Messages = "Hàng hóa không có trong hệ thống!" });
        }

        // Nguồn dòng 2069: objMst_ProductBase = lstRT_Mst_Product[0];
        var productBase = products1[0];

        // strWhereClause_Mst_Product_Get_Base (gate 3108-3133): FlagActive='1' AND FlagBuy='1' AND ProductType='PRODUCT'
        //   + if (!CUtils.IsNullOrEmpty(model.ProductCodeBase)) ProductCodeBase = value
        //   + if (!CUtils.IsNullOrEmpty(model.ProductCodeRoot)) ProductCodeRoot = value
        //   + if (!CUtils.IsNullOrEmpty(model.ProductCode))     ProductCode    != value
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

        // Nguồn dòng 2075-2080: listProduct = [objMst_ProductBase] + lstPrdBase
        var listProduct = new List<Mst_Product> { productBase };
        if (baseList != null && baseList.Count > 0)
        {
            listProduct.AddRange(baseList);
        }

        // GAP: bỏ qua ReportsService.Instance.WA_Rpt_Inv_InvBalance_LastUpdInvByProduct (gate 2114) —
        //      InvCodeSuggest luôn "" (đúng hành vi khi báo cáo trả rỗng).
        var listProductUI = new List<GetProductFromPrdBarCodeItem>();
        foreach (var itPrd in listProduct)
        {
            listProductUI.Add(new GetProductFromPrdBarCodeItem
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

        // Nguồn dòng 2159: return Json(new { Success = true, ListProduct = listProductUI });
        return Ok(new { Success = true, ListProduct = listProductUI });
    }
}

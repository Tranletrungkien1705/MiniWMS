using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.SearchMstProduct — nguồn InvF_InventoryInController.cs:2162-2315 (#92).
/// Chuỗi nguồn: gate 2162-2315 → WA_Mst_Product_Get (BaseController.cs:2926-2936) → WAS_Mst_Product_Get
///   (ProductCenter.MasterData.cs:5714-5872) → Mst_Product_Get (ProductCenter.MasterData.cs:5276-...).
/// Bổ sung: strWhereClause_Mst_Product (gate 3135-3161), strWhereSearchProductImport (gate 3026-3048).
/// GAP: ReportsService.Instance.WA_Rpt_Inv_InvBalance_LastUpdInvByProduct (gate dòng 2248) — dịch vụ BÁO CÁO NGOÀI
///      (qua `addressAPIs`), biz WAS_Rpt_Inv_InvBalance_LastUpdInvByProduct tồn tại nhưng dùng Mst_Inventory.OrgID /
///      InvBUCode / InvBUPattern (target Mst_Inventory entity thiếu OrgID + InvBUCode) và Mst_Product.ValConvert,
///      ProductCodeBase đầy đủ. Port bỏ qua bước này; InvCodeSuggest = "" (đúng hành vi khi báo cáo trả rỗng).
/// Phân trang: Ft_RecordStart = page * recordcount, Ft_RecordCount = recordcount (gate 2195-2196).
/// Kết quả: PageInfo&lt;SearchMstProductDtlUI&gt; bọc JsonView (SearchProduct).
/// </summary>
public partial class InvF_InventoryInControllerPort
{
    public class SearchMstProductReq
    {
        /// <summary>Nguồn dòng 2162: tham số `productcode` (mặc định null/empty).</summary>
        public string? ProductCode { get; set; }
        /// <summary>Nguồn dòng 2162: tham số `invcode` (dùng để gọi ReportsService — port bỏ qua, xem GAP).</summary>
        public string? InvCode { get; set; }
        /// <summary>Nguồn dòng 2162: tham số `recordcount = 10`.</summary>
        public int RecordCount { get; set; } = 10;
        /// <summary>Nguồn dòng 2162: tham số `page = 0`.</summary>
        public int Page { get; set; } = 0;
    }

    /// <summary>
    /// Nguồn InvF_InventoryInController.cs:2277-2290: các trường của InvF_InventoryInDtlUI được gán trong SearchMstProduct.
    /// </summary>
    public class SearchMstProductDtlUI
    {
        public string? ProductCode { get; set; }
        public string? mp_ProductCodeBase { get; set; }
        public string? mp_ProductCodeUser { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? mp_ProductName { get; set; }
        public string? UnitCode { get; set; }
        public string? mu_UnitName { get; set; }
        public double? UPIn { get; set; }
        public string? mp_FlagLot { get; set; }
        public string? mp_FlagSerial { get; set; }
        public string? InvCodeSuggest { get; set; }
        public List<Mst_Product>? Lst_Mst_ProductBase { get; set; }
    }

    [HttpPost("api/invf_inventoryin/searchmstproduct")]
    public async Task<IActionResult> SearchMstProduct([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SearchMstProductReq? req)
    {
        var productcode = req?.ProductCode ?? "";
        var recordcount = req?.RecordCount ?? 10;
        var page = req?.Page ?? 0;

        if (recordcount <= 0)
        {
            recordcount = 10;
        }

        // strWhereClause_Mst_Product(productcode, "1", "PRODUCT", "1") (gate 3135-3161):
        // FlagActive = '1'
        // ProductType IN ('PRODUCT')
        // FlagBuy = '1'
        // if (!string.IsNullOrEmpty(productcode)) (ProductCodeUser like %productcode% OR ProductName like %productcode%)
        var q1 = db.Set<Mst_Product>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagBuy ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.ProductType ?? "").ToLower(), PortQuery.Equal("PRODUCT"), PortQuery.Esc));

        if (!string.IsNullOrEmpty(productcode))
        {
            var pat = PortQuery.Contains(productcode);
            q1 = q1.Where(x => EF.Functions.Like((x.ProductCodeUser ?? "").ToLower(), pat, PortQuery.Esc)
                            || EF.Functions.Like((x.ProductName ?? "").ToLower(), pat, PortQuery.Esc));
        }

        // Nguồn Mst_Product_Get (ProductCenter.MasterData.cs:5414-5416): order by OrgID, ProductCode
        var orderedQ1 = q1.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode);

        var itemCount = await orderedQ1.CountAsync();
        var pageCount = recordcount > 0
            ? (itemCount % recordcount == 0 ? itemCount / recordcount : (itemCount / recordcount + 1))
            : 0;

        var recordStart = page * recordcount;
        var listMstProduct = (itemCount <= 0 || recordStart < 0 || recordStart >= itemCount)
            ? new List<Mst_Product>()
            : await orderedQ1.Skip(recordStart).Take(recordcount).ToListAsync();

        var listDtlUI = new List<SearchMstProductDtlUI>();

        if (listMstProduct.Count > 0)
        {
            // Lấy SP cùng base: strWhereSearchProductImport(null, lstPrdCodeBase) (gate 3026-3048)
            // FlagActive='1', FlagBuy='1', ProductType='PRODUCT', ProductCodeBase IN (lstPrdCodeBase)
            var lstPrdCodeBase = listMstProduct
                .Where(m => !string.IsNullOrEmpty(m.ProductCodeBase))
                .Select(m => m.ProductCodeBase!)
                .Distinct()
                .ToList();

            var listProductBase = new List<Mst_Product>();
            if (lstPrdCodeBase.Count > 0)
            {
                var qBase = db.Set<Mst_Product>().AsNoTracking()
                    .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                             && EF.Functions.Like((x.FlagBuy ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                             && EF.Functions.Like((x.ProductType ?? "").ToLower(), PortQuery.Equal("PRODUCT"), PortQuery.Esc));

                // Lọc theo danh sách ProductCodeBase
                var basePredicates = lstPrdCodeBase.Select(b => PortQuery.Equal(b)).ToList();
                qBase = qBase.Where(x => basePredicates.Any(p => EF.Functions.Like((x.ProductCodeBase ?? "").ToLower(), p, PortQuery.Esc)));

                listProductBase = await qBase.OrderBy(x => x.OrgID).ThenBy(x => x.ProductCode).ToListAsync();
            }

            // Join Mst_Unit để lấy mu_UnitName
            var unitCodes = listMstProduct.Where(x => !string.IsNullOrEmpty(x.UnitCode)).Select(x => x.UnitCode!).Distinct().ToList();
            var units = await db.Set<Mst_Unit>().AsNoTracking()
                .Where(u => unitCodes.Contains(u.UnitCode))
                .ToListAsync();

            // GAP: ReportsService.Instance.WA_Rpt_Inv_InvBalance_LastUpdInvByProduct (gate 2248) bỏ qua -> invCodeSuggest = ""
            foreach (var itPrd in listMstProduct)
            {
                var unit = units.FirstOrDefault(u => string.Equals(u.OrgID, itPrd.OrgID, StringComparison.OrdinalIgnoreCase)
                                                  && string.Equals(u.UnitCode, itPrd.UnitCode, StringComparison.OrdinalIgnoreCase))
                           ?? units.FirstOrDefault(u => string.Equals(u.UnitCode, itPrd.UnitCode, StringComparison.OrdinalIgnoreCase));

                var itemUI = new SearchMstProductDtlUI
                {
                    ProductCode = itPrd.ProductCode,
                    mp_ProductCodeBase = itPrd.ProductCodeBase,
                    mp_ProductCodeUser = itPrd.ProductCodeUser,
                    ProductCodeUser = itPrd.ProductCodeUser,
                    mp_ProductName = itPrd.ProductName,
                    UnitCode = itPrd.UnitCode,
                    mu_UnitName = unit?.UnitName,
                    UPIn = itPrd.UPBuy,
                    mp_FlagLot = itPrd.FlagLot,
                    mp_FlagSerial = itPrd.FlagSerial,
                    InvCodeSuggest = ""
                };

                if (!string.IsNullOrEmpty(itPrd.ProductCodeBase) && listProductBase.Count > 0)
                {
                    var listPrdCheckBase = listProductBase
                        .Where(x => string.Equals(x.ProductCodeBase, itPrd.ProductCodeBase, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (listPrdCheckBase.Count > 0)
                    {
                        itemUI.Lst_Mst_ProductBase = listPrdCheckBase;
                    }
                }

                listDtlUI.Add(itemUI);
            }
        }

        var pageInfo = new PageInfo<SearchMstProductDtlUI>(
            listDtlUI,
            itemCount,
            pageCount,
            page,
            recordcount
        );

        return Ok(pageInfo);
    }
}

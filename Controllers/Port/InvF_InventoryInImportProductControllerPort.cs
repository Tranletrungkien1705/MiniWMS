using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.ImportProduct — nguồn `InvF_InventoryInController.cs:3263-3660` (#95).
/// Chuỗi nguồn:
///   - Gate: `InvF_InventoryInController.cs:3263-3660` (ImportProduct(HttpPostedFileWrapper file, string invBUPattern))
///   - Service calls từ gate:
///       + `List_Mst_Inventory_Get(strWhere_Mst_Inventory)` (BaseController.cs:2759-2791):
///           Mst_Inventory.FlagActive = '1' [AND Mst_Inventory.InvBUPattern like invBUPattern]
///       + `WA_Mst_VATRate_Get(objRQ_Mst_VATRate)` (BaseController.cs:4457-4467):
///           Mst_VATRate.FlagActive = '1'
///       + `WA_Mst_Product_Get(objRQ_Mst_Product)` (BaseController.cs:2926-2936) với 2 lần query:
///           Query 1: `strWhereSearchProductImport(lstProductCodeUser, null)` (InvF_InventoryInController.cs:3026-3048):
///             Mst_Product.FlagActive = '1' AND FlagBuy = '1' AND ProductType = 'PRODUCT'
///             AND ProductCodeUser in (lstProductCodeUser)
///           Query 2: `strWhereSearchProductImport(null, lstPrdCodeBase)`:
///             Mst_Product.FlagActive = '1' AND FlagBuy = '1' AND ProductType = 'PRODUCT'
///             AND ProductCodeBase in (lstPrdCodeBase)
///   - Cột file template: `GetImportDicColumsTemplateProduct` (InvF_InventoryInController.cs:4860-4869):
///       1. ProductCodeUser (Mã hàng hóa *)
///       2. Qty (Số lượng *)
///       3. InvCodeInActual (Vị trí *)
///       4. UPIn (Giá nhập *)
///   - Kiểm tra và guard (theo thứ tự nguồn):
///       1. File rỗng / đuôi không hợp lệ (.xlsx, .xls) -> Nonsense.MESS_CHECK_FILEIMPORT ("File import không hợp lệ!")
///       2. Số cột khác 4 -> "File excel import không hợp lệ!"
///       3. Duyệt từng dòng:
///           - ProductCodeUser rỗng -> "Mã hàng hóa không được để trống!"
///           - Qty rỗng -> "Số lượng không được để trống!"
///           - Qty không phải số -> "Số lượng không phải định dạng số!"
///           - Qty < 0 -> "Số lượng không được < 0!"
///           - InvCodeInActual rỗng -> "Vị trí không được để trống!"
///           - InvCodeInActual không tồn tại trong Mst_Inventory (theo filter invBUPattern) -> "Vị trí '{0}' không có trong hệ thống!"
///           - UPIn nếu có mà không phải số -> "Giá nhập không phải định dạng số!"
///           - UPIn nếu có mà < 0 -> "Giá nhập không được < 0!"
///       4. Kiểm tra trùng (ProductCodeUser, InvCodeInActual) trong file -> "Mã hàng hóa '{0}' có vị trí '{1}' trong file excel lặp!"
///       5. Duyệt distinct ProductCodeUser:
///           - ProductCodeUser không có trong danh sách Mst_Product hợp lệ -> "Mã hàng hóa '{0}' không có trong hệ thống!"
///           - Build danh sách `InvF_InventoryInDtlUI` chính kèm `Lst_Product_InvCodeInActual`, tính tổng Qty, join vị trí cách nhau bởi ", "
///           - Nếu file excel có UPIn thì lấy UPIn từ file, ngược lại lấy UPBuy từ Mst_Product
///       6. Lấy SP cùng ProductCodeBase (khác ProductCode) add vào `item.Lst_InvF_InventoryInDtlUI`
///   - Kết quả:
///       Thành công: `{ Success = true, ListInvFInventoryInDtlUI = [...], List_Mst_VATRate = [...] }`
///       Lỗi: `{ Success = false, Messages = [...] }`
/// </summary>
public partial class InvF_InventoryInControllerPort
{
    private static readonly string[] ImportProductColumns = ["ProductCodeUser", "Qty", "InvCodeInActual", "UPIn"];

    public class ImportProductItemUI
    {
        public string? ProductCodeRoot { get; set; }
        public string? ProductCodeBase { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
        public string? ProductType { get; set; }
        public string? UnitCode { get; set; }
        public string? mu_UnitName { get; set; }
        public double? VAT { get; set; }
        public double? Qty { get; set; }
        public string? UPIn { get; set; }
        public string? FlagLot { get; set; }
        public string? FlagSerial { get; set; }
        public object? UPInDesc { get; set; } = 0;
        public string? InvCodeInActual { get; set; }
        public string? Remark { get; set; } = "";
        public List<ImportProductItemUI>? Lst_Product_InvCodeInActual { get; set; }
        public List<ImportProductItemUI>? Lst_InvF_InventoryInDtlUI { get; set; }
    }

    public class ImportProductResult
    {
        public bool Success { get; set; }
        public List<ImportProductItemUI>? ListInvFInventoryInDtlUI { get; set; }
        public List<Mst_VATRate>? List_Mst_VATRate { get; set; }
        public string? Detail { get; set; }
        public List<string>? Messages { get; set; }
    }

    [HttpPost("api/invf_inventoryin/importproduct")]
    public async Task<IActionResult> ImportProduct(IFormFile? file, [FromForm] string? invBUPattern)
    {
        var result = new ImportProductResult { Success = false };

        if (file == null || file.Length == 0)
        {
            result.Detail = "File import không hợp lệ!";
            return Ok(result);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xls")
        {
            result.Detail = "File import không hợp lệ!";
            return Ok(result);
        }

        int colCount;
        List<Dictionary<string, string?>> rows;
        try
        {
            using var stream = file.OpenReadStream();
            (colCount, rows) = PortExcel.ReadSheet(stream, ImportProductColumns, firstDataRow: 2);
        }
        catch (Exception ex)
        {
            result.Messages = [ex.Message];
            return Ok(result);
        }

        if (rows.Count == 0)
        {
            result.Messages = ["File không có dữ liệu!"];
            return Ok(result);
        }

        if (colCount != ImportProductColumns.Length)
        {
            result.Messages = ["File excel import không hợp lệ!"];
            return Ok(result);
        }

        // #region ["Mst_Inventory"] (BaseController.cs:2759)
        // Mst_Inventory.FlagActive = '1' [and Mst_Inventory.InvBUPattern like invBUPattern]
        var qInv = db.Set<Mst_Inventory>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

        if (!string.IsNullOrEmpty(invBUPattern))
        {
            var pat = invBUPattern.Trim().ToLowerInvariant();
            qInv = qInv.Where(x => EF.Functions.Like((x.InvBUPattern ?? "").ToLower(), pat, PortQuery.Esc));
        }

        var list_Mst_Inventory = await qInv.ToListAsync();

        // #region ["Mst_VATRate"] (BaseController.cs:4457)
        // Mst_VATRate.FlagActive = '1'
        var list_Mst_VATRate = await db.Set<Mst_VATRate>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc))
            .OrderBy(x => x.VATRateCode)
            .ToListAsync();

        // #region ["Check null"] (InvF_InventoryInController.cs:3336-3411)
        foreach (var dr in rows)
        {
            var productCodeUser = dr.GetValueOrDefault("ProductCodeUser");
            if (string.IsNullOrWhiteSpace(productCodeUser))
            {
                result.Messages = ["Mã hàng hóa không được để trống!"];
                return Ok(result);
            }

            var qtyStr = dr.GetValueOrDefault("Qty");
            if (string.IsNullOrWhiteSpace(qtyStr))
            {
                result.Messages = ["Số lượng không được để trống!"];
                return Ok(result);
            }

            if (!double.TryParse(qtyStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fQty)
                && !double.TryParse(qtyStr, out fQty))
            {
                result.Messages = ["Số lượng không phải định dạng số!"];
                return Ok(result);
            }

            if (fQty < 0)
            {
                result.Messages = ["Số lượng không được < 0!"];
                return Ok(result);
            }

            var invCodeInActual = dr.GetValueOrDefault("InvCodeInActual");
            if (string.IsNullOrWhiteSpace(invCodeInActual))
            {
                result.Messages = ["Vị trí không được để trống!"];
                return Ok(result);
            }

            var objMst_Inventory = list_Mst_Inventory
                .FirstOrDefault(it => string.Equals(it.InvCode, invCodeInActual, StringComparison.OrdinalIgnoreCase));
            if (objMst_Inventory == null)
            {
                result.Messages = [$"Vị trí '{invCodeInActual}' không có trong hệ thống!"];
                return Ok(result);
            }

            var upInStr = dr.GetValueOrDefault("UPIn");
            if (!string.IsNullOrWhiteSpace(upInStr))
            {
                if (!double.TryParse(upInStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fUPIn)
                    && !double.TryParse(upInStr, out fUPIn))
                {
                    result.Messages = ["Giá nhập không phải định dạng số!"];
                    return Ok(result);
                }

                if (fUPIn < 0)
                {
                    result.Messages = ["Giá nhập không được < 0!"];
                    return Ok(result);
                }
            }
        }

        // #region ["Check mã hàng hóa trùng"] (InvF_InventoryInController.cs:3413-3434)
        for (var i = 0; i < rows.Count; i++)
        {
            var p1 = rows[i].GetValueOrDefault("ProductCodeUser")?.Trim() ?? "";
            var v1 = rows[i].GetValueOrDefault("InvCodeInActual")?.Trim() ?? "";
            for (var j = 0; j < rows.Count; j++)
            {
                if (i != j)
                {
                    var p2 = rows[j].GetValueOrDefault("ProductCodeUser")?.Trim() ?? "";
                    var v2 = rows[j].GetValueOrDefault("InvCodeInActual")?.Trim() ?? "";
                    if (string.Equals(p1, p2, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(v1, v2, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Messages = [$"Mã hàng hóa '{p1}' có vị trí '{v1}' trong file excel lặp!"];
                        return Ok(result);
                    }
                }
            }
        }

        // #region ["Check mã hàng hóa tồn tại trong hệ thống và add vào list"] (InvF_InventoryInController.cs:3436-3574)
        var lstProductCodeUser_Distinct = rows
            .Select(r => r.GetValueOrDefault("ProductCodeUser")?.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Query 1: lấy thông tin sản phẩm từ Mst_Product theo ProductCodeUser
        // strWhereSearchProductImport: FlagActive='1' AND FlagBuy='1' AND ProductType='PRODUCT' AND ProductCodeUser in (...)
        var qPrd1 = db.Set<Mst_Product>().AsNoTracking()
            .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.FlagBuy ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                     && EF.Functions.Like((x.ProductType ?? "").ToLower(), PortQuery.Equal("PRODUCT"), PortQuery.Esc));

        var listProductAll = await qPrd1.ToListAsync();
        var listProduct = listProductAll
            .Where(x => lstProductCodeUser_Distinct.Any(code => string.Equals(code, x.ProductCodeUser, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var listInvFInventoryInDtlUI = new List<ImportProductItemUI>();
        var lstPrdCodeBase = new List<string>();

        foreach (var productCodeUser in lstProductCodeUser_Distinct)
        {
            var objMst_Product = listProduct
                .FirstOrDefault(it => string.Equals(it.ProductCodeUser, productCodeUser, StringComparison.OrdinalIgnoreCase));

            if (objMst_Product == null)
            {
                result.Messages = [$"Mã hàng hóa '{productCodeUser}' không có trong hệ thống!"];
                return Ok(result);
            }

            var productCodeBase = objMst_Product.ProductCodeBase ?? "";
            var objInvF_InventoryInDtlUI = new ImportProductItemUI
            {
                ProductCodeRoot = objMst_Product.ProductCodeRoot ?? "",
                ProductCodeBase = productCodeBase,
                ProductCode = objMst_Product.ProductCode ?? "",
                ProductCodeUser = objMst_Product.ProductCodeUser ?? "",
                ProductName = objMst_Product.ProductName ?? "",
                ProductType = objMst_Product.ProductType ?? "",
                UnitCode = objMst_Product.UnitCode ?? "",
                mu_UnitName = "",
                VAT = null,
                Qty = 0.0,
                UPIn = objMst_Product.UPBuy?.ToString() ?? "",
                FlagLot = objMst_Product.FlagLot ?? "",
                FlagSerial = objMst_Product.FlagSerial ?? "",
                UPInDesc = 0,
                InvCodeInActual = "",
                Remark = "",
                Lst_Product_InvCodeInActual = new List<ImportProductItemUI>(),
                Lst_InvF_InventoryInDtlUI = new List<ImportProductItemUI>()
            };

            // Nếu file excel có nhập giá => lấy giá từ file excel (dòng đầu tiên có giá)
            var objProduct_Excel = rows
                .FirstOrDefault(it => string.Equals(it.GetValueOrDefault("ProductCodeUser"), objMst_Product.ProductCodeUser, StringComparison.OrdinalIgnoreCase));
            if (objProduct_Excel != null)
            {
                var upInExcel = objProduct_Excel.GetValueOrDefault("UPIn");
                if (!string.IsNullOrWhiteSpace(upInExcel))
                {
                    objInvF_InventoryInDtlUI.UPIn = upInExcel;
                }
            }

            var listDtlImportCur = rows
                .Where(it => string.Equals(it.GetValueOrDefault("ProductCodeUser"), productCodeUser, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var totalQty = 0.0;
            var actualInvs = new List<string>();

            foreach (var _it in listDtlImportCur)
            {
                var invLoc = _it.GetValueOrDefault("InvCodeInActual") ?? "";
                var qStr = _it.GetValueOrDefault("Qty") ?? "0";
                double.TryParse(qStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dQty);

                var objProduct_InvCodeInActual = new ImportProductItemUI
                {
                    ProductCode = objMst_Product.ProductCode ?? "",
                    InvCodeInActual = invLoc,
                    Qty = dQty
                };
                objInvF_InventoryInDtlUI.Lst_Product_InvCodeInActual.Add(objProduct_InvCodeInActual);

                totalQty += dQty;
                actualInvs.Add(invLoc);
            }

            objInvF_InventoryInDtlUI.Qty = totalQty;
            objInvF_InventoryInDtlUI.InvCodeInActual = string.Join(", ", actualInvs);

            listInvFInventoryInDtlUI.Add(objInvF_InventoryInDtlUI);
            if (!string.IsNullOrEmpty(productCodeBase) && !lstPrdCodeBase.Contains(productCodeBase, StringComparer.OrdinalIgnoreCase))
            {
                lstPrdCodeBase.Add(productCodeBase);
            }
        }

        // #region Lấy SP cùng base và Add vào list return (InvF_InventoryInController.cs:3575-3641)
        if (lstPrdCodeBase.Count > 0)
        {
            var qPrdBase = db.Set<Mst_Product>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.FlagBuy ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.ProductType ?? "").ToLower(), PortQuery.Equal("PRODUCT"), PortQuery.Esc));

            var listBaseAll = await qPrdBase.ToListAsync();
            var listProductBase = listBaseAll
                .Where(x => lstPrdCodeBase.Any(b => string.Equals(b, x.ProductCodeBase, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (listProductBase.Count > 0)
            {
                foreach (var item in listInvFInventoryInDtlUI)
                {
                    var listPrdCheckBase = listProductBase
                        .Where(it => !string.IsNullOrEmpty(it.ProductCodeBase)
                                  && !string.IsNullOrEmpty(it.ProductCode)
                                  && string.Equals(it.ProductCodeBase, item.ProductCodeBase, StringComparison.OrdinalIgnoreCase)
                                  && !string.Equals(it.ProductCode, item.ProductCode, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var _it in listPrdCheckBase)
                    {
                        var qtySub = "0";
                        var uPInSub = _it.UPBuy?.ToString() ?? "";
                        var invCodeInActualSub = "";

                        var objExcelRow = rows
                            .FirstOrDefault(it => string.Equals(it.GetValueOrDefault("ProductCodeUser"), _it.ProductCodeUser, StringComparison.OrdinalIgnoreCase));
                        if (objExcelRow != null)
                        {
                            qtySub = objExcelRow.GetValueOrDefault("Qty") ?? "0";
                            if (!string.IsNullOrWhiteSpace(objExcelRow.GetValueOrDefault("UPIn")))
                            {
                                uPInSub = objExcelRow.GetValueOrDefault("UPIn")!;
                            }
                            invCodeInActualSub = objExcelRow.GetValueOrDefault("InvCodeInActual") ?? "";
                        }

                        double.TryParse(qtySub, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedQtySub);

                        var objInvFInventoryInDtlUI = new ImportProductItemUI
                        {
                            ProductCodeRoot = _it.ProductCodeRoot ?? "",
                            ProductCodeBase = _it.ProductCodeBase ?? "",
                            ProductCode = _it.ProductCode ?? "",
                            ProductCodeUser = _it.ProductCodeUser ?? "",
                            ProductName = _it.ProductName ?? "",
                            ProductType = _it.ProductType ?? "",
                            UnitCode = _it.UnitCode ?? "",
                            mu_UnitName = "",
                            VAT = null,
                            Qty = parsedQtySub,
                            UPIn = uPInSub,
                            FlagLot = _it.FlagLot ?? "",
                            FlagSerial = _it.FlagSerial ?? "",
                            UPInDesc = "0",
                            InvCodeInActual = invCodeInActualSub,
                            Remark = ""
                        };

                        item.Lst_InvF_InventoryInDtlUI ??= new List<ImportProductItemUI>();
                        item.Lst_InvF_InventoryInDtlUI.Add(objInvFInventoryInDtlUI);
                    }
                }
            }
        }

        return Ok(new
        {
            Success = true,
            ListInvFInventoryInDtlUI = listInvFInventoryInDtlUI,
            List_Mst_VATRate = list_Mst_VATRate
        });
    }
}

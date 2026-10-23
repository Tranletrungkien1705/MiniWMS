using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryInController.ImportLot — nguồn `InvF_InventoryInController.cs:3664-4097` (#96).
/// Chuỗi nguồn:
///   - Gate: `InvF_InventoryInController.cs:3664-4097` (ImportLot(HttpPostedFileWrapper file, string invBUPattern))
///   - Service từ gate:
///       + `List_Mst_Inventory_Get(strWhere)` (BaseController.cs:2759-2791):
///           Mst_Inventory.FlagActive = '1' [AND InvBUPattern like invBUPattern]
///       + `WA_Mst_Product_Get(objRQ)` (BaseController.cs:2926-2936) — 2 lần query:
///           Query 1: strWhereSearchProductImport(lstProductCodeUser, null) (InvF_InventoryInController.cs:3026-3048):
///             FlagActive='1' AND FlagBuy='1' AND ProductType='PRODUCT' AND ProductCodeUser IN (...)
///           Query 2: strWhereSearchProductImport(null, lstPrdCodeBase):
///             FlagActive='1' AND FlagBuy='1' AND ProductType='PRODUCT' AND ProductCodeBase IN (...)
///   - Cột file template (GetImportDicColumsTemplateLot, InvF_InventoryInController.cs:4871-4883):
///       ProductCodeUser, ProductLotNo, Qty, ProductionDate, ExpiredDate, InvCodeInActual, Remark
///   - Kiểm tra/guard theo thứ tự nguồn: đuôi file, số cột, rỗng Mã hàng hóa/Số lô/Số lượng, số lượng hợp lệ &gt;= 0,
///     Vị trí rỗng/không có trong Mst_Inventory (theo filter invBUPattern), trùng bộ 3 (ProductCodeUser, InvCodeInActual, ProductLotNo)
///     trong file, ProductCodeUser không có trong Mst_Product ⇒ thông điệp tiếng Việt (JsonResultEntry.Messages),
///     KHÔNG dùng PortResult.Fail (nguồn không ném TError — chỉ AddMessage).
/// </summary>
public partial class InvF_InventoryInControllerPort
{
    // Nguồn InvF_InventoryInController.cs:4873-4882 — thứ tự khoá trong GetImportDicColumsTemplateLot
    private static readonly string[] ImportLotColumns =
        ["ProductCodeUser", "ProductLotNo", "Qty", "ProductionDate", "ExpiredDate", "InvCodeInActual", "Remark"];

    /// <summary>
    /// Bản port của `InvF_InventoryInDtlUI` (model UI nguồn không có trong Common — xem excerpt mục MODEL).
    /// Trường `Qty` là `object?` vì nguồn gán cả double (3866, 3923, 3977) LẪN string (3951 `CUtils.StrValue(qty1)`) và
    /// giữ nguyên giá trị đọc được từ Excel (3919 `Qty = _item.Qty`). `UPInDesc` cũng gán int 0 (3889) / string "0" (4040).
    /// </summary>
    public class ImportLotItemUI
    {
        public string? ProductCodeRoot { get; set; }
        public string? ProductCodeBase { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
        public string? ProductType { get; set; }
        public string? UnitCode { get; set; }
        public string? mu_UnitName { get; set; }
        public string? VATRateCode { get; set; }
        public string? VAT { get; set; }
        public object? Qty { get; set; }
        public string? UPIn { get; set; }
        public string? FlagLot { get; set; }
        public string? FlagSerial { get; set; }
        public object? UPInDesc { get; set; }
        public string? InvCodeInActual { get; set; }
        public string? ProductLotNo { get; set; }
        public string? ProductionDate { get; set; }
        public string? ExpiredDate { get; set; }
        public string? Remark { get; set; }
        public List<ImportLotItemUI>? Lst_Product_InvCodeInActual { get; set; }
        public List<ImportLotItemUI>? Lst_Product_InvLot { get; set; }
        public List<ImportLotItemUI>? Lst_InvF_InventoryInDtlUI { get; set; }
    }

    /// <summary>Khuôn trả về: lỗi đầu vào = `{ Success=false, Detail|Messages }`; thành công = `{ Success=true, ListInvFInventoryInDtlUI }` (nguồn 4082).</summary>
    public class ImportLotResult
    {
        public bool Success { get; set; }
        public List<ImportLotItemUI>? ListInvFInventoryInDtlUI { get; set; }
        public string? Detail { get; set; }
        public List<string>? Messages { get; set; }
    }

    // Ký nguyên text nguồn Nonsense.MESS_CHECK_FILEIMPORT / MESS_CHECK_FILE_NULL — mã hằng không có trong repo nguồn ⇒
    // dùng đúng chuỗi đã dùng ở port #95 (InvF_InventoryInImportProductControllerPort.cs:94, 101, 120).
    private const string MsgInvalidFile = "File import không hợp lệ!";
    private const string MsgEmptyFile = "File không có dữ liệu!";

    [HttpPost("api/invf_inventoryin/importlot")]
    public async Task<IActionResult> ImportLot(IFormFile? file, [FromForm] string? invBUPattern)
    {
        var result = new ImportLotResult { Success = false };
        try
        {
            if (file == null || file.Length == 0)
            {
                // Nguồn 3676: `file.FileName` ⇒ NRE nếu file null, bắt ở catch (4091-4093). Đích xử lý thống nhất cùng thông điệp như port #95.
                result.Detail = MsgInvalidFile;
                return Ok(result);
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
            {
                // Nguồn 3677-3682: `resultEntry.Detail = Nonsense.MESS_CHECK_FILEIMPORT`.
                result.Detail = MsgInvalidFile;
                return Ok(result);
            }

            int colCount;
            List<Dictionary<string, string?>> rows;
            try
            {
                using var s = file.OpenReadStream();
                // Nguồn 3689: ExcelImportNew.Query(filePath, "A2") — dữ liệu bắt đầu dòng 2, cột theo THỨ TỰ template.
                (colCount, rows) = PortExcel.ReadSheet(s, ImportLotColumns, firstDataRow: 2);
            }
            catch (Exception ex)
            {
                result.Messages = [ex.Message];
                return Ok(result);
            }

            if (rows.Count == 0)
            {
                // Nguồn 4084-4088: Nonsense.MESS_CHECK_FILE_NULL.
                result.Messages = [MsgEmptyFile];
                return Ok(result);
            }

            if (colCount != ImportLotColumns.Length)
            {
                // Nguồn 3692-3697: "File excel import không hợp lệ!"
                result.Messages = ["File excel import không hợp lệ!"];
                return Ok(result);
            }

            // #region ["Mst_Inventory"] (gate 3698-3708) → BaseController.cs:2759-2791
            // Nguồn 3699: Mst_Inventory.FlagActive = '1'; 3704: InvBUPattern like 'invBUPattern'
            var qInv = db.Set<Mst_Inventory>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc));

            if (!string.IsNullOrEmpty(invBUPattern))
            {
                // Nguồn 3704: like nguyên chuỗi invBUPattern (không bọc %)
                var pat = invBUPattern.Trim().ToLowerInvariant();
                qInv = qInv.Where(x => EF.Functions.Like((x.InvBUPattern ?? "").ToLower(), pat, PortQuery.Esc));
            }
            var listMstInventory = await qInv.ToListAsync();

            // #region ["Check null"] (gate 3709-3796)
            foreach (var dr in rows)
            {
                var productCodeUser = dr.GetValueOrDefault("ProductCodeUser");
                if (string.IsNullOrWhiteSpace(productCodeUser))
                {
                    result.Messages = ["Mã hàng hóa không được để trống!"]; // 3712-3717
                    return Ok(result);
                }

                var productLotNo = dr.GetValueOrDefault("ProductLotNo");
                if (string.IsNullOrWhiteSpace(productLotNo))
                {
                    result.Messages = ["Số lô không được để trống!"]; // 3718-3723
                    return Ok(result);
                }

                var qtyStr = dr.GetValueOrDefault("Qty");
                if (string.IsNullOrWhiteSpace(qtyStr))
                {
                    result.Messages = ["Số lượng không được để trống!"]; // 3724-3729
                    return Ok(result);
                }
                if (!TryParseDouble(qtyStr, out var fQty))
                {
                    result.Messages = ["Số lượng không phải định dạng số!"]; // 3733-3738
                    return Ok(result);
                }
                if (fQty < 0)
                {
                    result.Messages = ["Số lượng không được < 0!"]; // 3742-3747
                    return Ok(result);
                }

                var invCodeInActual = dr.GetValueOrDefault("InvCodeInActual");
                if (string.IsNullOrWhiteSpace(invCodeInActual))
                {
                    result.Messages = ["Vị trí không được để trống!"]; // 3751-3756
                    return Ok(result);
                }
                var objMstInv = listMstInventory.FirstOrDefault(it =>
                    !string.IsNullOrEmpty(it.InvCode)
                    && string.Equals(it.InvCode, invCodeInActual.Trim(), StringComparison.OrdinalIgnoreCase)); // 3760
                if (objMstInv == null)
                {
                    result.Messages = [$"Vị trí '{invCodeInActual}' không có trong hệ thống!"]; // 3763
                    return Ok(result);
                }
                // Nguồn 3769-3795: chuẩn hoá ProductionDate/ExpiredDate bằng Nonsense.DATE_TIME_FORMAT (không có mã hằng) — GAP, giữ nguyên chuỗi.
            }

            // #region ["Check mã hàng hóa trùng"] (gate 3797-3820) — trùng bộ 3 (ProductCodeUser, InvCodeInActual, ProductLotNo)
            for (var i = 0; i < rows.Count; i++)
            {
                var p1 = (rows[i].GetValueOrDefault("ProductCodeUser") ?? "").Trim();
                var v1 = (rows[i].GetValueOrDefault("InvCodeInActual") ?? "").Trim();
                var l1 = (rows[i].GetValueOrDefault("ProductLotNo") ?? "").Trim();
                for (var j = 0; j < rows.Count; j++)
                {
                    if (i == j) continue;
                    var p2 = (rows[j].GetValueOrDefault("ProductCodeUser") ?? "").Trim();
                    var v2 = (rows[j].GetValueOrDefault("InvCodeInActual") ?? "").Trim();
                    var l2 = (rows[j].GetValueOrDefault("ProductLotNo") ?? "").Trim();
                    // Nguồn 3811 dùng .Equals ⇒ Ordinal (phân biệt hoa/thường)
                    if (string.Equals(p1, p2, StringComparison.Ordinal)
                        && string.Equals(v1, v2, StringComparison.Ordinal)
                        && string.Equals(l1, l2, StringComparison.Ordinal))
                    {
                        result.Messages = [$"Mã hàng hóa '{p1}' thuộc lô '{l1}' có vị trí '{v1}' trong file excel lặp!"]; // 3813
                        return Ok(result);
                    }
                }
            }

            // #region ["Check mã hàng hóa tồn tại trong hệ thống và add vào list"] (gate 3821-4011)
            var lstProductCodeUserDistinct = rows
                .Select(r => (r.GetValueOrDefault("ProductCodeUser") ?? "").Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(); // nguồn 3823

            // Query 1: strWhereSearchProductImport(lstProductCodeUser, null) — FlagActive='1' AND FlagBuy='1' AND ProductType='PRODUCT' AND ProductCodeUser IN (...)
            var listProductAll = await db.Set<Mst_Product>().AsNoTracking()
                .Where(x => EF.Functions.Like((x.FlagActive ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.FlagBuy ?? "").ToLower(), PortQuery.Equal("1"), PortQuery.Esc)
                         && EF.Functions.Like((x.ProductType ?? "").ToLower(), PortQuery.Equal("PRODUCT"), PortQuery.Esc))
                .ToListAsync();
            var listProduct = listProductAll
                .Where(x => !string.IsNullOrEmpty(x.ProductCodeUser)
                         && lstProductCodeUserDistinct.Any(code => string.Equals(code, x.ProductCodeUser, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var listInvFInventoryInDtlUI = new List<ImportLotItemUI>();
            var lstPrdCodeBase = new List<string>();

            foreach (var productCodeUser in lstProductCodeUserDistinct)
            {
                var objMst_Product = listProduct
                    .FirstOrDefault(it => !string.IsNullOrEmpty(it.ProductCodeUser)
                                       && string.Equals(it.ProductCodeUser, productCodeUser, StringComparison.OrdinalIgnoreCase));
                if (objMst_Product == null)
                {
                    result.Messages = [$"Mã hàng hóa '{productCodeUser}' không có trong hệ thống!"]; // 3865/4007
                    return Ok(result);
                }

                var productCodeBase = objMst_Product.ProductCodeBase ?? "";

                // Nguồn 3875-3896: khởi tạo obj chính (giá trị ban đầu — Qty/fQty sẽ gán lại trong vòng lặp)
                var objUI = new ImportLotItemUI
                {
                    ProductCodeRoot = objMst_Product.ProductCodeRoot ?? "",
                    ProductCodeBase = productCodeBase,
                    ProductCode = objMst_Product.ProductCode ?? "",
                    ProductCodeUser = objMst_Product.ProductCodeUser ?? "",
                    ProductName = objMst_Product.ProductName ?? "",
                    ProductType = objMst_Product.ProductType ?? "",
                    UnitCode = objMst_Product.UnitCode ?? "",
                    mu_UnitName = "", // nguồn 3884 `mu_UnitName` là cột join Mst_Unit — Mst_Product đích không có cột ⇒ "" (GAP)
                    Qty = 0.0,
                    UPIn = objMst_Product.UPBuy?.ToString() ?? "",
                    FlagLot = objMst_Product.FlagLot ?? "",
                    FlagSerial = objMst_Product.FlagSerial ?? "",
                    UPInDesc = 0,
                    InvCodeInActual = "",
                    Remark = "",
                    Lst_Product_InvCodeInActual = new List<ImportLotItemUI>(),
                    Lst_Product_InvLot = new List<ImportLotItemUI>(),
                    VATRateCode = "", // GAP: Mst_Product đích không có cột VATRateCode
                    VAT = ""           // GAP: Mst_Product đích không có cột join mvat_VATRate
                };

                var listDtlImportCur = rows
                    .Where(it => !string.IsNullOrEmpty(it.GetValueOrDefault("ProductCodeUser"))
                              && string.Equals((it.GetValueOrDefault("ProductCodeUser") ?? "").Trim(), productCodeUser, StringComparison.OrdinalIgnoreCase))
                    .ToList(); // nguồn 3897
                var listInvCodeIn_Distinct = listDtlImportCur
                    .Where(it => !string.IsNullOrEmpty(it.GetValueOrDefault("InvCodeInActual")))
                    .Select(item => (item.GetValueOrDefault("InvCodeInActual") ?? "").Trim())
                    .Distinct()
                    .ToList(); // nguồn 3898

                var fQty = 0.0; // nguồn 3871
                if (listDtlImportCur.Count > 0)
                {
                    var i = 0;
                    if (listInvCodeIn_Distinct.Count > 0)
                    {
                        var strInvCodeInActual = ""; // nguồn 3904
                        foreach (var invCode in listInvCodeIn_Distinct)
                        {
                            var listInvCodeIn = listDtlImportCur
                                .Where(it => !string.IsNullOrEmpty(it.GetValueOrDefault("InvCodeInActual"))
                                          && string.Equals((it.GetValueOrDefault("InvCodeInActual") ?? "").Trim(), invCode, StringComparison.OrdinalIgnoreCase))
                                .ToList(); // nguồn 3908
                            if (listInvCodeIn.Count > 1)
                            {
                                // nguồn 3910-3938
                                foreach (var _item in listInvCodeIn)
                                {
                                    var objProduct = new ImportLotItemUI
                                    {
                                        ProductCode = objMst_Product.ProductCode ?? "",
                                        ProductLotNo = (_item.GetValueOrDefault("ProductLotNo") ?? "").Trim(),
                                        InvCodeInActual = (_item.GetValueOrDefault("InvCodeInActual") ?? "").Trim(),
                                        Qty = ParseDoubleOr0(_item.GetValueOrDefault("Qty"))
                                    };
                                    objUI.Lst_Product_InvCodeInActual!.Add(objProduct);
                                    fQty += ParseDoubleOr0(_item.GetValueOrDefault("Qty"));
                                    objUI.Qty = fQty;
                                    objUI.InvCodeInActual = objProduct.InvCodeInActual;
                                    objUI.ProductLotNo = objProduct.ProductLotNo;

                                    // nguồn 3927-3935 — Contains PHÂN BIỆT hoa/thường (string.Contains mặc định Ordinal)
                                    var rawInv = _item.GetValueOrDefault("InvCodeInActual") ?? "";
                                    if (!strInvCodeInActual.Contains(rawInv))
                                    {
                                        if (!string.IsNullOrEmpty(strInvCodeInActual)) strInvCodeInActual += ", ";
                                        strInvCodeInActual += rawInv;
                                    }
                                }
                            }
                            else
                            {
                                // nguồn 3939-3953
                                if (i != 0) strInvCodeInActual += ", ";
                                strInvCodeInActual += listInvCodeIn[0].GetValueOrDefault("InvCodeInActual") ?? "";
                                var qty2 = ParseDoubleOr0(listInvCodeIn[0].GetValueOrDefault("Qty"));
                                var qty1 = Convert.ToDouble(objUI.Qty ?? 0.0);
                                qty1 += qty2;
                                objUI.Qty = qty1.ToString(); // nguồn 3951: `CUtils.StrValue(qty1)` ⇒ string
                                i++;
                            }
                        }
                        objUI.InvCodeInActual = strInvCodeInActual; // nguồn 3955
                    }
                    else
                    {
                        // Nhánh chết (mọi dòng đã qua guard "InvCodeInActual không được rỗng") — port nguyên để khớp 1-1.
                        var strInvCodeInActual = "";
                        foreach (var _it in listDtlImportCur)
                        {
                            var objProduct_InvCodeInActual = new ImportLotItemUI
                            {
                                ProductCode = objMst_Product.ProductCode ?? "",
                                InvCodeInActual = (_it.GetValueOrDefault("InvCodeInActual") ?? "").Trim(),
                                Qty = ParseDoubleOr0(_it.GetValueOrDefault("Qty"))
                            };
                            objUI.Lst_Product_InvCodeInActual!.Add(objProduct_InvCodeInActual);
                            fQty += ParseDoubleOr0(_it.GetValueOrDefault("Qty"));
                            if (i != 0) strInvCodeInActual += ", ";
                            strInvCodeInActual += _it.GetValueOrDefault("InvCodeInActual") ?? "";
                            objUI.Qty = fQty;
                            objUI.InvCodeInActual = strInvCodeInActual;
                            i++;
                        }
                    }
                }

                // nguồn 3984-3996: Lst_Product_InvLot — mỗi dòng Excel
                foreach (var _is in listDtlImportCur)
                {
                    var objLot = new ImportLotItemUI
                    {
                        ProductCode = objMst_Product.ProductCode ?? "",
                        InvCodeInActual = (_is.GetValueOrDefault("InvCodeInActual") ?? "").Trim(),
                        Qty = _is.GetValueOrDefault("Qty"), // nguồn 3990 giữ NGUYÊN giá trị đọc được từ Excel
                        ProductLotNo = (_is.GetValueOrDefault("ProductLotNo") ?? "").Trim(),
                        ProductionDate = _is.GetValueOrDefault("ProductionDate"),
                        ExpiredDate = _is.GetValueOrDefault("ExpiredDate")
                    };
                    objUI.Lst_Product_InvLot!.Add(objLot);
                }

                listInvFInventoryInDtlUI.Add(objUI); // nguồn 3998
                if (!lstPrdCodeBase.Contains(productCodeBase)) lstPrdCodeBase.Add(productCodeBase); // nguồn 3999-4002
            }

            // #region ["Lấy SP cùng base và Add vào list return"] (gate 4012-4080)
            if (lstPrdCodeBase.Count > 0)
            {
                // Query 2: strWhereSearchProductImport(null, lstPrdCodeBase) — cùng filter FlagActive/FlagBuy/ProductType + ProductCodeBase IN (...)
                var listProductBase = listProductAll
                    .Where(x => !string.IsNullOrEmpty(x.ProductCodeBase)
                             && lstPrdCodeBase.Any(b => string.Equals(b, x.ProductCodeBase, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (listProductBase.Count > 0)
                {
                    foreach (var item in listInvFInventoryInDtlUI)
                    {
                        // nguồn 4019-4022: cùng ProductCodeBase, KHÁC ProductCode
                        var listPrdCheckBase = listProductBase
                            .Where(it => !string.IsNullOrEmpty(it.ProductCodeBase)
                                      && !string.IsNullOrEmpty(it.ProductCode)
                                      && string.Equals(it.ProductCodeBase, item.ProductCodeBase, StringComparison.OrdinalIgnoreCase)
                                      && !string.Equals(it.ProductCode, item.ProductCode, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        foreach (var _it in listPrdCheckBase)
                        {
                            // nguồn 4038-4051 — mặc định qty="0", invCodeInActual="".
                            // Nguồn tìm `objMst_Product` từ `listDtlImport` theo cột `ProductCode` — template KHÔNG có cột này
                            // ⇒ luôn null ⇒ qty="0", invCodeInActual="" (LỖI NGUỒN GIỮ NGUYÊN). Chỉ khi Excel có cột ProductCode (ngoài template)
                            // mới có ca qty="1". Port kiểm để khớp đúng nguồn.
                            var qty = "0";
                            var invCodeInActual = "";
                            var matchedRow = rows.FirstOrDefault(r =>
                                !string.IsNullOrEmpty(r.GetValueOrDefault("ProductCode"))
                                && string.Equals(r.GetValueOrDefault("ProductCode"), _it.ProductCode, StringComparison.Ordinal));
                            if (matchedRow != null)
                            {
                                qty = "1";
                                invCodeInActual = matchedRow.GetValueOrDefault("InvCodeInActual") ?? "";
                            }

                            var objSub = new ImportLotItemUI
                            {
                                ProductCodeRoot = _it.ProductCodeRoot ?? "",
                                ProductCodeBase = _it.ProductCodeBase ?? "",
                                ProductCode = _it.ProductCode ?? "",
                                ProductCodeUser = _it.ProductCodeUser ?? "",
                                ProductName = _it.ProductName ?? "",
                                ProductType = _it.ProductType ?? "",
                                UnitCode = _it.UnitCode ?? "",
                                mu_UnitName = "",
                                VATRateCode = "", // GAP (xem trên)
                                VAT = "",          // GAP (xem trên)
                                Qty = qty,
                                UPIn = _it.UPBuy?.ToString() ?? "",
                                FlagLot = _it.FlagLot ?? "",
                                FlagSerial = _it.FlagSerial ?? "",
                                UPInDesc = "0",
                                InvCodeInActual = invCodeInActual,
                                Remark = ""
                            };
                            item.Lst_InvF_InventoryInDtlUI ??= new List<ImportLotItemUI>();
                            item.Lst_InvF_InventoryInDtlUI.Add(objSub);
                        }
                    }
                }
            }

            return Ok(new { Success = true, ListInvFInventoryInDtlUI = listInvFInventoryInDtlUI }); // nguồn 4082
        }
        catch (Exception ex)
        {
            // nguồn 4091-4094: SetFailed().AddException(ex) — trả nguyên ex.Message như port khác
            result.Messages = [ex.Message];
            return Ok(result);
        }
    }

    private static bool TryParseDouble(string? s, out double v)
    {
        v = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        return double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v)
            || double.TryParse(s, out v);
    }

    private static double ParseDoubleOr0(string? s) => TryParseDouble(s, out var v) ? v : 0;
}

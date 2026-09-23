using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models;
using MiniWMS.Services;

namespace MiniWMS.Controllers;

public class HomeController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index() { ViewBag.Dash = await svc.DashboardAsync(); return View(); }
}

public class WarehouseController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.InventoryTypes = await svc.InventoryTypesAsync(activeOnly: true);
        ViewBag.InventoryLevelTypes = await svc.InventoryLevelTypesAsync(activeOnly: true);
        ViewBag.Areas = await svc.AreasAsync(activeOnly: true);
        return View(await svc.WarehousesAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? code, string? address, string? invTypeCode, string? invLevelTypeCode, string? areaCode, string? remark)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Cần tên kho."; return RedirectToAction(nameof(Index)); }
        await svc.CreateWarehouseAsync(new Warehouse
        {
            Name = name.Trim(),
            Code = code ?? "",
            Address = address?.Trim(),
            InvTypeCode = string.IsNullOrWhiteSpace(invTypeCode) ? null : invTypeCode.Trim().ToUpperInvariant(),
            InvLevelTypeCode = string.IsNullOrWhiteSpace(invLevelTypeCode) ? null : invLevelTypeCode.Trim().ToUpperInvariant(),
            AreaCode = string.IsNullOrWhiteSpace(areaCode) ? null : areaCode.Trim().ToUpperInvariant(),
            Remark = remark?.Trim()
        });
        TempData["Success"] = "Đã tạo kho.";
        return RedirectToAction(nameof(Index));
    }
}

public class ProductController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.PartTypes = await svc.PartTypesAsync(activeOnly: true);
        ViewBag.Brands = await svc.BrandsAsync(activeOnly: true);
        ViewBag.ProductModels = await svc.ProductModelsAsync(activeOnly: true);
        ViewBag.PartUnits = await svc.PartUnitsAsync(activeOnly: true);
        ViewBag.MaterialTypes = await svc.PartMaterialTypesAsync(activeOnly: true);
        ViewBag.ProductGroups = await svc.ProductGroupsAsync(activeOnly: true);
        return View(await svc.ProductsAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? code, string? partTypeCode, string? brandCode, string? modelCode, string? pmType, string? productGrpCode, string uom, int minStock, int maxStock = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Cần tên hàng."; return RedirectToAction(nameof(Index)); }
        await svc.CreateProductAsync(new Product
        {
            Name = name.Trim(),
            Code = code ?? "",
            PartTypeCode = string.IsNullOrWhiteSpace(partTypeCode) ? null : partTypeCode.Trim().ToUpperInvariant(),
            BrandCode = string.IsNullOrWhiteSpace(brandCode) ? null : brandCode.Trim().ToUpperInvariant(),
            ModelCode = string.IsNullOrWhiteSpace(modelCode) ? null : modelCode.Trim().ToUpperInvariant(),
            PMType = string.IsNullOrWhiteSpace(pmType) ? null : pmType.Trim().ToUpperInvariant(),
            ProductGrpCode = string.IsNullOrWhiteSpace(productGrpCode) ? null : productGrpCode.Trim().ToUpperInvariant(),
            Uom = string.IsNullOrWhiteSpace(uom) ? "cái" : uom,
            MinStock = minStock,
            MaxStock = maxStock
        });
        TempData["Success"] = "Đã tạo mặt hàng.";
        return RedirectToAction(nameof(Index));
    }
}

public class DocController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(DocType? type, DocStatus? status)
    {
        ViewBag.Type = type; ViewBag.Status = status;
        return View(await svc.DocsAsync(type, status));
    }

    public async Task<IActionResult> Create(DocType type = DocType.In)
    {
        ViewBag.Type = type;
        ViewBag.Warehouses = await svc.WarehousesAsync();
        ViewBag.Products = await svc.ProductsAsync();
        ViewBag.Suppliers = await svc.SuppliersAsync(activeOnly: true);
        ViewBag.Customers = await svc.CustomersAsync(activeOnly: true);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocType type, int? fromWarehouseId, int? toWarehouseId, string? note, string? refNo,
        string? supplierCode, string? supplierName,
        string? customerCode, string? customerName,
        int[]? productId, int[]? qty)
    {
        var doc = new StockDoc
        {
            Type = type,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            SupplierCode = supplierCode?.Trim(),
            SupplierName = supplierName?.Trim(),
            CustomerCode = customerCode?.Trim(),
            CustomerName = customerName?.Trim(),
            Note = note,
            RefNo = refNo,
            CreatedBy = "web"
        };
        var lines = new List<(int, int)>();
        for (int i = 0; productId != null && i < productId.Length; i++)
            lines.Add((productId[i], i < (qty?.Length ?? 0) ? qty![i] : 0));
        if (!lines.Any(l => l.Item1 > 0 && l.Item2 != 0)) { TempData["Error"] = "Cần ít nhất 1 dòng hàng."; return RedirectToAction(nameof(Create), new { type }); }
        var id = await svc.CreateDocAsync(doc, lines);
        TempData["Success"] = "Đã tạo phiếu (Nháp). Bấm Ghi sổ để cập nhật tồn.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var d = await svc.GetDocAsync(id);
        if (d == null) return NotFound();
        return View(d);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Post(int id)
    {
        var (ok, msg) = await svc.PostDocAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try { await svc.CancelDocAsync(id); TempData["Success"] = "Đã hủy phiếu."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }
}

public class AuditController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, StockAuditStatus? status)
    {
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Status = status;
        ViewBag.Warehouses = await svc.WarehousesAsync();
        return View(await svc.AuditsAsync(warehouseId, status));
    }

    public async Task<IActionResult> Create(int? warehouseId)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;
        var selectedWhId = warehouseId ?? whs.FirstOrDefault()?.Id ?? 0;
        ViewBag.SelectedWarehouseId = selectedWhId;

        var prods = await svc.ProductsAsync();
        var balances = selectedWhId > 0 ? await svc.BalancesAsync(selectedWhId) : new List<BalanceRow>();
        var balDict = balances.ToDictionary(b => b.ProductId, b => b.Qty);

        var items = prods.Select(p => new
        {
            Product = p,
            QtyInit = balDict.TryGetValue(p.Id, out var q) ? q : 0
        }).ToList();

        ViewBag.Items = items;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int warehouseId, string? note, int[]? productId, int[]? qtyInit, int[]? qtyActual, string[]? lineNote)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho kiểm kê.";
            return RedirectToAction(nameof(Create));
        }

        var lines = new List<(int productId, int qtyInit, int qtyActual, string? note)>();
        for (int i = 0; productId != null && i < productId.Length; i++)
        {
            var pid = productId[i];
            var qInit = (qtyInit != null && i < qtyInit.Length) ? qtyInit[i] : 0;
            var qAct = (qtyActual != null && i < qtyActual.Length) ? qtyActual[i] : 0;
            var lNote = (lineNote != null && i < lineNote.Length) ? lineNote[i] : null;
            lines.Add((pid, qInit, qAct, lNote));
        }

        if (!lines.Any(l => l.productId > 0))
        {
            TempData["Error"] = "Cần ít nhất 1 mặt hàng để kiểm kê.";
            return RedirectToAction(nameof(Create), new { warehouseId });
        }

        var audit = new StockAudit
        {
            WarehouseId = warehouseId,
            Note = note,
            CreatedBy = "web"
        };
        var id = await svc.CreateAuditAsync(audit, lines);
        TempData["Success"] = "Đã tạo phiếu kiểm kê (Nháp). Bấm Cân bằng kho để tự động điều chỉnh tồn.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var audit = await svc.GetAuditAsync(id);
        if (audit == null) return NotFound();
        return View(audit);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Balance(int id)
    {
        var (ok, msg) = await svc.BalanceAuditAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            await svc.CancelAuditAsync(id);
            TempData["Success"] = "Đã hủy phiếu kiểm kê.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Detail), new { id });
    }
}

public class MoveOrderController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? fromWarehouseId, int? toWarehouseId, MoveOrderStatus? status)
    {
        ViewBag.FromWarehouseId = fromWarehouseId;
        ViewBag.ToWarehouseId = toWarehouseId;
        ViewBag.Status = status;
        ViewBag.Warehouses = await svc.WarehousesAsync();
        return View(await svc.MoveOrdersAsync(fromWarehouseId, toWarehouseId, status));
    }

    public async Task<IActionResult> Create(int? fromWarehouseId, int? toWarehouseId)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;
        var fWh = fromWarehouseId ?? whs.FirstOrDefault()?.Id ?? 0;
        var tWh = toWarehouseId ?? whs.Skip(1).FirstOrDefault()?.Id ?? 0;
        ViewBag.FromWarehouseId = fWh;
        ViewBag.ToWarehouseId = tWh;
        ViewBag.Products = await svc.ProductsAsync();

        var balances = fWh > 0 ? await svc.BalancesAsync(fWh) : new List<BalanceRow>();
        ViewBag.Balances = balances.ToDictionary(b => b.ProductId, b => b.Qty);

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int fromWarehouseId, int toWarehouseId, string? note, int[]? productId, int[]? qty, string[]? lineNote)
    {
        if (fromWarehouseId <= 0 || toWarehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn đầy đủ kho xuất và kho nhận.";
            return RedirectToAction(nameof(Create));
        }
        if (fromWarehouseId == toWarehouseId)
        {
            TempData["Error"] = "Kho xuất chuyển và kho nhận chuyển phải khác nhau.";
            return RedirectToAction(nameof(Create), new { fromWarehouseId, toWarehouseId });
        }

        var lines = new List<(int productId, int qty, string? note)>();
        for (int i = 0; productId != null && i < productId.Length; i++)
        {
            var pid = productId[i];
            var q = (qty != null && i < qty.Length) ? qty[i] : 0;
            var lNote = (lineNote != null && i < lineNote.Length) ? lineNote[i] : null;
            if (pid > 0 && q > 0) lines.Add((pid, q, lNote));
        }

        if (lines.Count == 0)
        {
            TempData["Error"] = "Cần ít nhất 1 mặt hàng với số lượng > 0.";
            return RedirectToAction(nameof(Create), new { fromWarehouseId, toWarehouseId });
        }

        var order = new MoveOrder
        {
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            Note = note,
            CreatedBy = "web"
        };
        var id = await svc.CreateMoveOrderAsync(order, lines);
        TempData["Success"] = "Đã lập Lệnh điều chuyển kho (Chờ duyệt). Bấm Duyệt lệnh để tiếp tục.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var order = await svc.GetMoveOrderAsync(id);
        if (order == null) return NotFound();

        var balances = await svc.BalancesAsync(order.FromWarehouseId);
        ViewBag.Balances = balances.ToDictionary(b => b.ProductId, b => b.Qty);

        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var (ok, msg) = await svc.ApproveMoveOrderAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(int id)
    {
        var (ok, msg) = await svc.ExecuteMoveOrderAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            await svc.CancelMoveOrderAsync(id);
            TempData["Success"] = "Đã hủy lệnh điều chuyển.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Detail), new { id });
    }
}

public class InventoryController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId)
    {
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Warehouses = await svc.WarehousesAsync();
        return View(await svc.BalancesAsync(warehouseId));
    }
}

public class WarehouseCardController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? productId, int? warehouseId, DateTime? fromDate, DateTime? toDate)
    {
        var prods = await svc.ProductsAsync();
        var whs = await svc.WarehousesAsync();
        ViewBag.Products = prods;
        ViewBag.Warehouses = whs;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        var targetPid = productId ?? prods.FirstOrDefault()?.Id ?? 0;
        ViewBag.ProductId = targetPid;

        if (targetPid <= 0)
        {
            return View((WarehouseCardReport?)null);
        }

        try
        {
            var report = await svc.WarehouseCardAsync(targetPid, warehouseId, fromDate, toDate);
            return View(report);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View((WarehouseCardReport?)null);
        }
    }
}

public class InventoryInOutController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? q)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;
        ViewBag.WarehouseId = warehouseId;

        var defFrom = fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var defTo = toDate ?? DateTime.Today;
        ViewBag.FromDate = defFrom.ToString("yyyy-MM-dd");
        ViewBag.ToDate = defTo.ToString("yyyy-MM-dd");
        ViewBag.Keyword = q ?? "";

        var report = await svc.InventoryInOutReportAsync(warehouseId, defFrom, defTo, q);
        return View(report);
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? q)
    {
        var defFrom = fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var defTo = toDate ?? DateTime.Today;
        var report = await svc.InventoryInOutReportAsync(warehouseId, defFrom, defTo, q);

        var sb = new System.Text.StringBuilder();
        // UTF-8 BOM để Excel hiển thị đúng tiếng Việt
        sb.Append('\uFEFF');
        sb.AppendLine("BÁO CÁO NHẬP - XUẤT - TỒN KHO");
        sb.AppendLine($"Kho:;{report.WarehouseName}");
        sb.AppendLine($"Từ ngày:;{report.FromDate:dd/MM/yyyy};Đến ngày:;{report.ToDate:dd/MM/yyyy}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã hàng;Tên hàng hoá;ĐVT;Kho hàng;Tồn đầu kỳ;Nhập trong kỳ;Xuất trong kỳ;Tồn cuối kỳ");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            sb.AppendLine($"{stt++};\"{r.ProductCode}\";\"{r.ProductName.Replace("\"", "\"\"")}\";\"{r.Uom}\";\"{r.WarehouseName}\";{r.OpeningQty};{r.InQty};{r.OutQty};{r.ClosingQty}");
        }

        sb.AppendLine($";;;;TỔNG CỘNG;{report.TotalOpeningQty};{report.TotalInQty};{report.TotalOutQty};{report.TotalClosingQty}");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BaoCao_NhapXuatTon_{report.FromDate:yyyyMMdd}_{report.ToDate:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class OrgController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var orgs = await db.Orgs.IgnoreQueryFilters().OrderBy(o => o.CreatedAt).ToListAsync();
        Request.Cookies.TryGetValue(TenantContext.CookieName, out var curKey);
        ViewBag.CurrentKey = curKey ?? TenantContext.DefaultApiKey;
        return View(orgs);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Cần tên tổ chức."; return RedirectToAction(nameof(Index)); }
        var org = new Org { Name = name.Trim(), ApiKey = "wms_" + Guid.NewGuid().ToString("N") };
        db.Orgs.Add(org); await db.SaveChangesAsync();
        SetCookies(org.ApiKey, org.Name);
        TempData["Success"] = $"Đã tạo & chuyển sang \"{org.Name}\".";
        return RedirectToAction("Index", "Home");
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Switch(string apiKey)
    {
        var org = await db.Orgs.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.ApiKey == apiKey);
        if (org == null) { TempData["Error"] = "Không tìm thấy."; return RedirectToAction(nameof(Index)); }
        SetCookies(org.ApiKey, org.Name);
        return RedirectToAction("Index", "Home");
    }
    public IActionResult Reset()
    {
        Response.Cookies.Delete(TenantContext.CookieName); Response.Cookies.Delete("org_name");
        return RedirectToAction("Index", "Home");
    }
    private void SetCookies(string k, string n)
    {
        var o = new CookieOptions { IsEssential = true, Expires = DateTimeOffset.UtcNow.AddDays(30) };
        Response.Cookies.Append(TenantContext.CookieName, k, o); Response.Cookies.Append("org_name", n, o);
    }
}

public class ReturnSupController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, ReturnSupStatus? status)
    {
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Status = status;
        ViewBag.Warehouses = await svc.WarehousesAsync();
        return View(await svc.ReturnToSuppliersAsync(warehouseId, status));
    }

    public async Task<IActionResult> Create(int? warehouseId)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;
        var selectedWhId = warehouseId ?? whs.FirstOrDefault()?.Id ?? 0;
        ViewBag.SelectedWarehouseId = selectedWhId;
        ViewBag.Products = await svc.ProductsAsync();

        var balances = selectedWhId > 0 ? await svc.BalancesAsync(selectedWhId) : new List<BalanceRow>();
        ViewBag.Balances = balances.ToDictionary(b => b.ProductId, b => b.Qty);

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int warehouseId, string supplierName, string? supplierCode, string? refDocNo, string? reason,
        int[]? productId, int[]? qty, decimal[]? unitPrice, string[]? lineNote)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho xuất trả hàng.";
            return RedirectToAction(nameof(Create));
        }
        if (string.IsNullOrWhiteSpace(supplierName))
        {
            TempData["Error"] = "Vui lòng nhập tên nhà cung cấp.";
            return RedirectToAction(nameof(Create), new { warehouseId });
        }

        var lines = new List<(int productId, int qty, decimal unitPrice, string? note)>();
        for (int i = 0; productId != null && i < productId.Length; i++)
        {
            var pid = productId[i];
            var q = (qty != null && i < qty.Length) ? qty[i] : 0;
            var price = (unitPrice != null && i < unitPrice.Length) ? unitPrice[i] : 0m;
            var lNote = (lineNote != null && i < lineNote.Length) ? lineNote[i] : null;
            if (pid > 0 && q > 0) lines.Add((pid, q, price, lNote));
        }

        if (lines.Count == 0)
        {
            TempData["Error"] = "Cần ít nhất 1 mặt hàng với số lượng > 0.";
            return RedirectToAction(nameof(Create), new { warehouseId });
        }

        var returnDoc = new ReturnToSupplier
        {
            WarehouseId = warehouseId,
            SupplierName = supplierName.Trim(),
            SupplierCode = supplierCode?.Trim(),
            RefDocNo = refDocNo?.Trim(),
            Reason = reason?.Trim(),
            CreatedBy = "web"
        };

        var id = await svc.CreateReturnToSupplierAsync(returnDoc, lines);
        TempData["Success"] = "Đã lập phiếu xuất trả hàng NCC (Chờ duyệt). Bấm 'Duyệt & Xuất kho' để trừ tồn.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var doc = await svc.GetReturnToSupplierAsync(id);
        if (doc == null) return NotFound();

        var balances = await svc.BalancesAsync(doc.WarehouseId);
        ViewBag.Balances = balances.ToDictionary(b => b.ProductId, b => b.Qty);

        return View(doc);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var (ok, msg) = await svc.ApproveReturnToSupplierAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            await svc.CancelReturnToSupplierAsync(id);
            TempData["Success"] = "Đã hủy phiếu trả hàng nhà cung cấp.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Detail), new { id });
    }
}

public class CustomerReturnController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, CusReturnStatus? status)
    {
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Status = status;
        ViewBag.Warehouses = await svc.WarehousesAsync();
        return View(await svc.CustomerReturnsAsync(warehouseId, status));
    }

    public async Task<IActionResult> Create(int? warehouseId)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;
        var selectedWhId = warehouseId ?? whs.FirstOrDefault()?.Id ?? 0;
        ViewBag.SelectedWarehouseId = selectedWhId;
        ViewBag.Products = await svc.ProductsAsync();
        ViewBag.Customers = await svc.CustomersAsync(activeOnly: true);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int warehouseId, string customerName, string? customerCode, string? invoiceNo, string? refOrderNo, string? reason,
        int[]? productId, int[]? qty, decimal[]? unitPrice, string[]? lineNote)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho nhận hàng trả lại.";
            return RedirectToAction(nameof(Create));
        }
        if (string.IsNullOrWhiteSpace(customerName))
        {
            TempData["Error"] = "Vui lòng nhập tên khách hàng trả lại.";
            return RedirectToAction(nameof(Create), new { warehouseId });
        }

        var lines = new List<(int productId, int qty, decimal unitPrice, string? note)>();
        for (int i = 0; productId != null && i < productId.Length; i++)
        {
            var pid = productId[i];
            var q = (qty != null && i < qty.Length) ? qty[i] : 0;
            var price = (unitPrice != null && i < unitPrice.Length) ? unitPrice[i] : 0m;
            var lNote = (lineNote != null && i < lineNote.Length) ? lineNote[i] : null;
            if (pid > 0 && q > 0) lines.Add((pid, q, price, lNote));
        }

        if (lines.Count == 0)
        {
            TempData["Error"] = "Cần ít nhất 1 mặt hàng nhận trả với số lượng > 0.";
            return RedirectToAction(nameof(Create), new { warehouseId });
        }

        var returnDoc = new CustomerReturn
        {
            WarehouseId = warehouseId,
            CustomerName = customerName.Trim(),
            CustomerCode = customerCode?.Trim(),
            InvoiceNo = invoiceNo?.Trim(),
            RefOrderNo = refOrderNo?.Trim(),
            Reason = reason?.Trim(),
            CreatedBy = "web"
        };

        var id = await svc.CreateCustomerReturnAsync(returnDoc, lines);
        TempData["Success"] = "Đã lập phiếu nhận hàng khách trả lại (Chờ nhận). Bấm 'Duyệt & Nhập kho' để cộng tồn.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> Detail(int id)
    {
        var doc = await svc.GetCustomerReturnAsync(id);
        if (doc == null) return NotFound();
        return View(doc);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var (ok, msg) = await svc.ApproveCustomerReturnAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            await svc.CancelCustomerReturnAsync(id);
            TempData["Success"] = "Đã hủy phiếu khách hàng trả lại.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Detail), new { id });
    }
}

public class StockMinimumController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, bool onlyBelowMin = true, string? q = null)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.OnlyBelowMin = onlyBelowMin;
        ViewBag.Keyword = q ?? "";

        var report = await svc.StockMinimumReportAsync(warehouseId, onlyBelowMin, q);
        return View(report);
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, bool onlyBelowMin = true, string? q = null)
    {
        var report = await svc.StockMinimumReportAsync(warehouseId, onlyBelowMin, q);

        var sb = new System.Text.StringBuilder();
        // UTF-8 BOM cho Excel
        sb.Append('\uFEFF');
        sb.AppendLine("BÁO CÁO CHẠM TỒN KHO TỐI THIỂU & CẢNH BÁO AN TOÀN KHO");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName}");
        sb.AppendLine($"Ngày xuất:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Chế độ lọc:;{(onlyBelowMin ? "Chỉ mặt hàng chạm/dưới định mức" : "Xem tất cả mặt hàng")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã hàng;Tên hàng hoá;ĐVT;Kho lưu trữ;Tồn thực tế;Định mức tối thiểu;Định mức tối đa;Lượng thiếu hụt;Tỷ lệ an toàn (%);Trạng thái");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            sb.AppendLine($"{stt++};\"{r.ProductCode}\";\"{r.ProductName.Replace("\"", "\"\"")}\";\"{r.Uom}\";\"{r.WarehouseName}\";{r.CurrentQty};{r.MinStock};{r.MaxStock};{r.ShortageQty};{r.SafetyRatio:F1}%;\"{r.AlertLabel}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;TỔNG CỘNG THIẾU HỤT:;;;;{report.TotalShortageQty};;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BaoCao_ChamTonToiThieu_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class LotExpiryController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, LotExpiryStatus? status, string? q)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Status = status;
        ViewBag.Keyword = q ?? "";

        var report = await svc.StockLotExpiryReportAsync(warehouseId, status, q);
        return View(report);
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, LotExpiryStatus? status, string? q)
    {
        var report = await svc.StockLotExpiryReportAsync(warehouseId, status, q);

        var sb = new System.Text.StringBuilder();
        // UTF-8 BOM cho Excel
        sb.Append('\uFEFF');
        sb.AppendLine("BÁO CÁO THEO DÕI HẠN SỬ DỤNG HÀNG HÓA & TỒN KHO THEO LÔ");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName}");
        sb.AppendLine($"Ngày xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Trạng thái lọc:;{(status.HasValue ? status.Value.ToString() : "Tất cả lô")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã hàng;Tên hàng hoá;ĐVT;Kho lưu trữ;Số lô sản xuất;Ngày sản xuất;Hạn sử dụng;Ngày nhập kho;Số ngày lưu kho;Số ngày còn lại;Số lượng tồn;Trạng thái hạn dùng");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var nsx = r.ProductionDate.HasValue ? r.ProductionDate.Value.ToString("dd/MM/yyyy") : "—";
            var hsd = r.ExpiredDate.ToString("dd/MM/yyyy");
            var nnk = r.InDate.ToString("dd/MM/yyyy");
            sb.AppendLine($"{stt++};\"{r.ProductCode}\";\"{r.ProductName.Replace("\"", "\"\"")}\";\"{r.Uom}\";\"{r.WarehouseName}\";\"{r.LotNo}\";{nsx};{hsd};{nnk};{r.DaysInStock};{r.DaysToExpiry};{r.Quantity};\"{r.StatusLabel}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;;;;;;;TỔNG CỘNG LƯỢNG TỒN:;{report.TotalQuantity};");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BaoCao_HanSuDung_TheoLo_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class StorageTimeController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, StorageTimeAgingBracket? bracket, string? q, DateTime? asOfDate)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Bracket = bracket;
        ViewBag.Keyword = q ?? "";
        ViewBag.AsOfDate = (asOfDate ?? DateTime.Today).ToString("yyyy-MM-dd");

        var report = await svc.StorageTimeReportAsync(warehouseId, bracket, q, asOfDate);
        return View(report);
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, StorageTimeAgingBracket? bracket, string? q, DateTime? asOfDate)
    {
        var report = await svc.StorageTimeReportAsync(warehouseId, bracket, q, asOfDate);

        var sb = new System.Text.StringBuilder();
        // UTF-8 BOM cho Excel
        sb.Append('\uFEFF');
        sb.AppendLine("BÁO CÁO TUỔI KHO & THỜI GIAN LƯU KHO HÀNG HÓA");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName}");
        sb.AppendLine($"Ngày chốt số liệu:;{report.AsOfDate:dd/MM/yyyy}");
        sb.AppendLine($"Bộ lọc nhóm tuổi:;{(bracket.HasValue ? bracket.Value.ToString() : "Tất cả các nhóm")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã hàng;Tên hàng hoá;ĐVT;Kho lưu trữ;Số lượng tồn;Đơn giá vốn (VNĐ);Tổng giá trị tồn (VNĐ);Ngày nhập gần nhất;Tuổi kho (ngày);Phân nhóm tuổi kho;Khuyến nghị xử lý");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var lastIn = r.LastInDate.HasValue ? r.LastInDate.Value.ToString("dd/MM/yyyy") : "—";
            sb.AppendLine($"{stt++};\"{r.ProductCode}\";\"{r.ProductName.Replace("\"", "\"\"")}\";\"{r.Uom}\";\"{r.WarehouseName}\";{r.CurrentQty};{r.CostPrice:F0};{r.TotalValue:F0};{lastIn};{r.StorageDays};\"{r.BracketLabel}\";\"{r.Recommendation}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;TỔNG CỘNG:;{report.TotalQty};;{report.TotalInventoryValue:F0};;;;");
        sb.AppendLine($";;;;TỒN ĐỌNG VỐN (> 90 NGÀY):;;;{report.StagnantValue:F0};;{report.StagnantItemsCount} mặt hàng;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BaoCao_TuoiKho_ThoiGianLuuKho_{report.AsOfDate:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class StockSerialController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, int? productId, StockSerialStatus? status, string? q)
    {
        var whs = await svc.WarehousesAsync();
        var prods = await svc.ProductsAsync();
        ViewBag.Warehouses = whs;
        ViewBag.Products = prods;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.ProductId = productId;
        ViewBag.Status = status;
        ViewBag.Keyword = q ?? "";

        var report = await svc.StockSerialReportAsync(warehouseId, productId, status, q);
        return View(report);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int warehouseId, int productId, string serialNo, string? lotNo, string? refNo, string? note)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho lưu trữ.";
            return RedirectToAction(nameof(Index), new { warehouseId, productId });
        }
        if (productId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn mặt hàng.";
            return RedirectToAction(nameof(Index), new { warehouseId, productId });
        }
        if (string.IsNullOrWhiteSpace(serialNo))
        {
            TempData["Error"] = "Vui lòng nhập số Serial / Barcode.";
            return RedirectToAction(nameof(Index), new { warehouseId, productId });
        }

        try
        {
            var serial = new StockSerial
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                SerialNo = serialNo.Trim(),
                LotNo = lotNo?.Trim(),
                RefNo = refNo?.Trim(),
                Note = note?.Trim(),
                Status = StockSerialStatus.Available
            };
            await svc.CreateStockSerialAsync(serial);
            TempData["Success"] = $"Đã đăng ký Serial/IMEI '{serial.SerialNo}' thành công.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { warehouseId, productId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, StockSerialStatus status, string? note, int? warehouseId, int? productId, StockSerialStatus? filterStatus, string? q)
    {
        var (ok, msg) = await svc.ChangeStockSerialStatusAsync(id, status, note);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index), new { warehouseId, productId, status = filterStatus, q });
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, int? productId, StockSerialStatus? status, string? q)
    {
        var report = await svc.StockSerialReportAsync(warehouseId, productId, status, q);

        var sb = new System.Text.StringBuilder();
        // UTF-8 BOM cho Excel
        sb.Append('\uFEFF');
        sb.AppendLine("BÁO CÁO QUẢN LÝ & TRA CỨU SERIAL / IMEI HÀNG TỒN KHO");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName};Mặt hàng:;{report.ProductName}");
        sb.AppendLine($"Ngày xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc trạng thái:;{(status.HasValue ? status.Value.ToString() : "Tất cả các trạng thái")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã Serial/IMEI;Mã hàng;Tên hàng hoá;ĐVT;Kho lưu trữ;Số lô (Lot);Ngày nhập;Ngày xuất;Số chứng từ;Trạng thái;Ghi chú");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var inDate = r.InDate.ToString("dd/MM/yyyy");
            var outDate = r.OutDate.HasValue ? r.OutDate.Value.ToString("dd/MM/yyyy") : "—";
            sb.AppendLine($"{stt++};\"{r.SerialNo}\";\"{r.ProductCode}\";\"{r.ProductName.Replace("\"", "\"\"")}\";\"{r.Uom}\";\"{r.WarehouseName}\";\"{r.LotNo ?? "—"}\";{inDate};{outDate};\"{r.RefNo ?? "—"}\";\"{r.StatusLabel}\";\"{r.Note?.Replace("\"", "\"\"") ?? ""}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;TỔNG CỘNG:;{report.TotalSerials} serial;;Khả dụng: {report.AvailableCount};Đang khóa: {report.LockedCount};Lỗi NG: {report.DamagedNGCount};Đã xuất: {report.ExportedCount};");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BaoCao_Serial_IMEI_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class InventoryBlockController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, string? shelfCode, bool? activeOnly, string? q)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.ShelfCode = shelfCode ?? "";
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.Keyword = q ?? "";
        ViewBag.Shelves = await svc.GetShelvesAsync(warehouseId);

        var report = await svc.InventoryBlockReportAsync(warehouseId, shelfCode, activeOnly, q);
        return View(report);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int warehouseId, string invBlockCode, string shelfCode, string? invBlockDesc,
        double length, double width, double height, int maxCapacity, string? remark, int? filterWhId, string? filterShelf, bool? filterActive, string? q)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho lưu trữ.";
            return RedirectToAction(nameof(Index), new { warehouseId = filterWhId, shelfCode = filterShelf, activeOnly = filterActive, q });
        }
        if (string.IsNullOrWhiteSpace(invBlockCode))
        {
            TempData["Error"] = "Vui lòng nhập mã vị trí ô kho.";
            return RedirectToAction(nameof(Index), new { warehouseId = filterWhId, shelfCode = filterShelf, activeOnly = filterActive, q });
        }
        if (string.IsNullOrWhiteSpace(shelfCode))
        {
            TempData["Error"] = "Vui lòng nhập mã dãy kệ.";
            return RedirectToAction(nameof(Index), new { warehouseId = filterWhId, shelfCode = filterShelf, activeOnly = filterActive, q });
        }

        try
        {
            var block = new InventoryBlock
            {
                WarehouseId = warehouseId,
                InvBlockCode = invBlockCode.Trim(),
                ShelfCode = shelfCode.Trim(),
                InvBlockDesc = invBlockDesc?.Trim(),
                Length = length,
                Width = width,
                Height = height,
                MaxCapacity = maxCapacity > 0 ? maxCapacity : 100,
                Remark = remark?.Trim(),
                FlagActive = true
            };
            await svc.CreateInventoryBlockAsync(block);
            TempData["Success"] = $"Đã thêm vị trí kho '{block.InvBlockCode}' (Kệ {block.ShelfCode}) thành công.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { warehouseId = filterWhId ?? warehouseId, shelfCode = filterShelf, activeOnly = filterActive, q });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, int? warehouseId, string? shelfCode, bool? activeOnly, string? q)
    {
        var (ok, msg) = await svc.ToggleInventoryBlockStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index), new { warehouseId, shelfCode, activeOnly, q });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? warehouseId, string? shelfCode, bool? activeOnly, string? q)
    {
        var (ok, msg) = await svc.DeleteInventoryBlockAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index), new { warehouseId, shelfCode, activeOnly, q });
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, string? shelfCode, bool? activeOnly, string? q)
    {
        var report = await svc.InventoryBlockReportAsync(warehouseId, shelfCode, activeOnly, q);

        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("DANH MỤC VỊ TRÍ KHO - SƠ ĐỒ KHAY KỆ & Ô LƯU TRỮ");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName};Dãy kệ:;{(string.IsNullOrWhiteSpace(report.ShelfCode) ? "Tất cả dãy kệ" : report.ShelfCode)}");
        sb.AppendLine($"Ngày xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Trạng thái lọc:;{(activeOnly.HasValue ? (activeOnly.Value ? "Chỉ vị trí hoạt động" : "Chỉ vị trí bảo trì/khóa") : "Tất cả trạng thái")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã vị trí (Block);Dãy kệ (Shelf);Mô tả vị trí;Kho lưu trữ;Dài (cm);Rộng (cm);Cao (cm);Thể tích (m3);Sức chứa tối đa;Trạng thái;Ghi chú;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var created = r.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            sb.AppendLine($"{stt++};\"{r.InvBlockCode}\";\"{r.ShelfCode}\";\"{r.InvBlockDesc?.Replace("\"", "\"\"") ?? ""}\";\"{r.WarehouseName}\";{r.Length};{r.Width};{r.Height};{r.VolumeM3:F3};{r.MaxCapacity};\"{r.StatusLabel}\";\"{r.Remark?.Replace("\"", "\"\"") ?? ""}\";{created}");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;TỔNG CỘNG:;{report.TotalBlocks} vị trí;;;{report.TotalVolumeM3:F3} m3;{report.TotalCapacity} sp;Hoạt động: {report.ActiveCount};Bảo trì: {report.MaintenanceCount};Dãy kệ: {report.TotalShelves}");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"SoDo_ViTriKho_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class CostPriceController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, int? productId, bool? currentOnly, DateTime? fromDate, DateTime? toDate, string? q)
    {
        var whs = await svc.WarehousesAsync();
        var prods = await svc.ProductsAsync();
        ViewBag.Warehouses = whs;
        ViewBag.Products = prods;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.ProductId = productId;
        ViewBag.CurrentOnly = currentOnly;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.Keyword = q ?? "";

        var report = await svc.CostPriceHistReportAsync(warehouseId, productId, currentOnly, fromDate, toDate, q);
        return View(report);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? warehouseId, int productId, decimal costPrice, DateTime? effectDate, string? refDocNo, string? remark)
    {
        if (productId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn mặt hàng.";
            return RedirectToAction(nameof(Index), new { warehouseId, productId });
        }
        if (costPrice < 0)
        {
            TempData["Error"] = "Đơn giá vốn không thể là số âm.";
            return RedirectToAction(nameof(Index), new { warehouseId, productId });
        }

        try
        {
            var item = new CostPriceHist
            {
                WarehouseId = warehouseId > 0 ? warehouseId : null,
                ProductId = productId,
                CostPrice = costPrice,
                EffectDate = effectDate ?? DateTime.Today,
                RefDocNo = string.IsNullOrWhiteSpace(refDocNo) ? $"DC-{DateTime.Now:yyyyMMdd}" : refDocNo.Trim(),
                CalcPeriodName = $"Điều chỉnh giá vốn thủ công {DateTime.Today:dd/MM/yyyy}",
                IsCurrent = true,
                SourceType = CostPriceSourceType.Manual,
                Remark = remark?.Trim(),
                CreatedBy = "admin"
            };

            await svc.CreateCostPriceHistAsync(item);
            TempData["Success"] = $"Đã thiết lập giá vốn {costPrice:N0} đ thành công.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { warehouseId, productId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, decimal costPrice, string? remark, int? filterWhId, int? filterProdId, bool? filterCurrent, string? q)
    {
        var (ok, msg) = await svc.UpdateCostPriceHistAsync(id, costPrice, remark);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index), new { warehouseId = filterWhId, productId = filterProdId, currentOnly = filterCurrent, q });
    }

    public async Task<IActionResult> Calculate(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? calcPeriodName)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;

        var defFrom = fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        var defTo = toDate ?? DateTime.Today;
        var periodName = string.IsNullOrWhiteSpace(calcPeriodName) ? $"Kỳ tính giá vốn tháng {DateTime.Today:MM/yyyy}" : calcPeriodName.Trim();

        ViewBag.WarehouseId = warehouseId;
        ViewBag.FromDate = defFrom.ToString("yyyy-MM-dd");
        ViewBag.ToDate = defTo.ToString("yyyy-MM-dd");
        ViewBag.CalcPeriodName = periodName;

        var preview = await svc.PreviewCalculateCostPriceAsync(warehouseId, defFrom, defTo, periodName, null);
        return View(preview);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCalculation(int? warehouseId, DateTime? effectDate, string? calcPeriodName, int[]? productIds, decimal[]? newCostPrices, string[]? notes)
    {
        if (productIds == null || productIds.Length == 0)
        {
            TempData["Error"] = "Không có mặt hàng nào để áp dụng.";
            return RedirectToAction(nameof(Calculate), new { warehouseId });
        }

        var periodName = string.IsNullOrWhiteSpace(calcPeriodName) ? $"Kỳ tính giá vốn {DateTime.Today:MM/yyyy}" : calcPeriodName.Trim();
        var effDate = effectDate ?? DateTime.Today;

        var items = new List<(int ProductId, decimal NewCostPrice, string Note)>();
        for (int i = 0; i < productIds.Length; i++)
        {
            var pid = productIds[i];
            var price = (newCostPrices != null && i < newCostPrices.Length) ? newCostPrices[i] : 0m;
            var note = (notes != null && i < notes.Length) ? notes[i] : "";
            items.Add((pid, price, note));
        }

        var (ok, msg, count) = await svc.ApplyCalculateCostPriceAsync(warehouseId, effDate, periodName, items);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index), new { warehouseId });
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, int? productId, bool? currentOnly, DateTime? fromDate, DateTime? toDate, string? q)
    {
        var report = await svc.CostPriceHistReportAsync(warehouseId, productId, currentOnly, fromDate, toDate, q);

        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("BẢNG KÊ & LỊCH SỬ GIÁ VỐN KHO HÀNG HÓA");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName};Mặt hàng:;{report.ProductName}");
        sb.AppendLine($"Ngày xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc hiện hành:;{(currentOnly.HasValue ? (currentOnly.Value ? "Chỉ giá vốn hiện hành" : "Chỉ lịch sử cũ") : "Tất cả các kỳ")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã hàng;Tên hàng hoá;ĐVT;Kho lưu trữ;Đơn giá vốn (VNĐ);Trạng thái hiện hành;Thời điểm hiệu lực;Số chứng từ / Kỳ tính;Nguồn tính;Người cập nhật;Ngày cập nhật;Ghi chú");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var eff = r.EffectDate.ToString("dd/MM/yyyy");
            var upd = (r.UpdatedAt ?? r.CreatedAt).ToString("dd/MM/yyyy HH:mm");
            var cur = r.IsCurrent ? "Hiện hành" : "Lịch sử";
            sb.AppendLine($"{stt++};\"{r.ProductCode}\";\"{r.ProductName.Replace("\"", "\"\"")}\";\"{r.Uom}\";\"{r.WarehouseName}\";{r.CostPrice:F0};\"{cur}\";{eff};\"{r.RefDocNo ?? "—"}\";\"{r.SourceTypeLabel}\";\"{r.UpdatedBy ?? r.CreatedBy}\";{upd};\"{r.Remark?.Replace("\"", "\"\"") ?? ""}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;TỔNG CỘNG:;{report.TotalRecords} bản ghi;;;{report.CurrentItemsCount} sp có giá vốn;Đơn giá bình quân:;{report.AvgCostPrice:F0};;Cao nhất: {report.MaxCostPrice:F0};Thấp nhất: {report.MinCostPrice:F0}");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BangKe_GiaVonKho_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class PeriodClosingController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, PeriodClosingStatus? status, int? year)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Status = status;
        ViewBag.Year = year;

        var list = await svc.PeriodClosingsAsync(warehouseId, status, year);

        // Thống kê KPI
        ViewBag.TotalClosings = list.Count;
        ViewBag.ClosedCount = list.Count(c => c.Status == PeriodClosingStatus.Closed);
        ViewBag.ReopenedCount = list.Count(c => c.Status == PeriodClosingStatus.Reopened);

        var latestClosed = list.FirstOrDefault(c => c.Status == PeriodClosingStatus.Closed);
        ViewBag.LatestClosingPeriod = latestClosed?.PeriodName ?? "Chưa có kỳ chốt";
        ViewBag.LatestClosingValue = latestClosed?.TotalClosingValue ?? 0m;
        ViewBag.LatestClosingQty = latestClosed?.TotalClosingQty ?? 0;

        return View(list);
    }

    public async Task<IActionResult> Create(int? warehouseId, int? year, int? month)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;

        var targetYear = year ?? (DateTime.Today.Month == 1 ? DateTime.Today.Year - 1 : DateTime.Today.Year);
        var targetMonth = month ?? (DateTime.Today.Month == 1 ? 12 : DateTime.Today.Month - 1);

        ViewBag.WarehouseId = warehouseId;
        ViewBag.Year = targetYear;
        ViewBag.Month = targetMonth;

        var preview = await svc.PreviewPeriodClosingAsync(warehouseId, targetYear, targetMonth);
        return View(preview);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? warehouseId, int year, int month, string? note)
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
        {
            TempData["Error"] = "Tháng hoặc năm chốt sổ không hợp lệ.";
            return RedirectToAction(nameof(Create), new { warehouseId, year, month });
        }

        var (ok, msg, id) = await svc.CreateAndClosePeriodAsync(warehouseId, year, month, note, "admin");
        TempData[ok ? "Success" : "Error"] = msg;

        if (ok)
        {
            return RedirectToAction(nameof(Detail), new { id });
        }

        return RedirectToAction(nameof(Create), new { warehouseId, year, month });
    }

    public async Task<IActionResult> Detail(int id, string? q)
    {
        var closing = await svc.GetPeriodClosingAsync(id);
        if (closing == null) return NotFound();

        ViewBag.Keyword = q ?? "";
        return View(closing);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(int id, string? reason)
    {
        var (ok, msg) = await svc.ReopenPeriodClosingAsync(id, reason ?? "Mở lại kỳ để kiểm tra");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var (ok, msg) = await svc.CancelPeriodClosingAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> ExportCsv(int id)
    {
        var closing = await svc.GetPeriodClosingAsync(id);
        if (closing == null) return NotFound();

        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine($"BẢNG KÊ CHI TIẾT CHỐT SỔ TỒN KHO THÁNG - {closing.PeriodName.ToUpper()}");
        sb.AppendLine($"Mã kỳ chốt:;{closing.Code};Kho áp dụng:;{(closing.Warehouse != null ? closing.Warehouse.Name : "Toàn hệ thống")}");
        sb.AppendLine($"Thời điểm chốt:;{closing.ClosedAt:dd/MM/yyyy HH:mm};Người chốt:;{closing.ClosedBy};Trạng thái:;{closing.Status}");
        sb.AppendLine($"Ghi chú:;\"{closing.Note?.Replace("\"", "\"\"") ?? ""}\"");
        sb.AppendLine();
        sb.AppendLine("STT;Mã kho;Tên kho;Mã SP;Tên mặt hàng;ĐVT;Tồn đầu kỳ;Nhập trong kỳ;Đơn giá nhập;Tổng giá trị nhập;Xuất trong kỳ;Đơn giá xuất;Tổng giá trị xuất;Tồn cuối kỳ;Đơn giá vốn chốt;Giá trị tồn cuối kỳ (VNĐ);Ghi chú");

        int stt = 1;
        foreach (var l in closing.Lines.OrderBy(x => x.Warehouse.Code).ThenBy(x => x.Product.Code))
        {
            sb.AppendLine($"{stt++};\"{l.Warehouse.Code}\";\"{l.Warehouse.Name}\";\"{l.Product.Code}\";\"{l.Product.Name.Replace("\"", "\"\"")}\";\"{l.Product.Uom}\";{l.OpeningQty};{l.InQty};{l.LastInPrice:F0};{l.InAmount:F0};{l.OutQty};{l.LastOutPrice:F0};{l.OutAmount:F0};{l.ClosingQty};{l.CostPrice:F0};{l.ClosingValue:F0};\"{l.Note?.Replace("\"", "\"\"") ?? ""}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;TỔNG CỘNG:;;{closing.TotalOpeningQty};{closing.TotalInQty};;{closing.Lines.Sum(x => x.InAmount):F0};{closing.TotalOutQty};;{closing.Lines.Sum(x => x.OutAmount):F0};{closing.TotalClosingQty};;{closing.TotalClosingValue:F0};");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BangKe_ChotTonKho_{closing.Code}_{DateTime.Now:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class CartonController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, int? productId, CartonStatus? status, string? q)
    {
        var whs = await svc.WarehousesAsync();
        var prods = await svc.ProductsAsync();
        ViewBag.Warehouses = whs;
        ViewBag.Products = prods;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.ProductId = productId;
        ViewBag.Status = status;
        ViewBag.Keyword = q ?? "";

        var report = await svc.CartonsAsync(warehouseId, productId, status, q);
        return View(report);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var carton = await svc.GetCartonAsync(id);
        if (carton == null) return NotFound();

        ViewBag.Products = await svc.ProductsAsync();
        return View(carton);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int warehouseId, string? cartonCode, string? cartonType, double lengthCm, double widthCm, double heightCm, int capacity, string? shelfLocation, string? remark)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho lưu trữ.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var carton = new InventoryCarton
            {
                WarehouseId = warehouseId,
                CartonCode = cartonCode?.Trim() ?? "",
                CartonType = string.IsNullOrWhiteSpace(cartonType) ? "Thùng carton tiêu chuẩn" : cartonType.Trim(),
                LengthCm = lengthCm > 0 ? lengthCm : 40,
                WidthCm = widthCm > 0 ? widthCm : 30,
                HeightCm = heightCm > 0 ? heightCm : 30,
                Capacity = capacity > 0 ? capacity : 50,
                ShelfLocation = shelfLocation?.Trim(),
                Remark = remark?.Trim(),
                Status = CartonStatus.Empty
            };

            var id = await svc.CreateCartonAsync(carton);
            TempData["Success"] = $"Đã tạo thùng carton {carton.CartonCode} thành công.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { warehouseId });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateBatch(int warehouseId, string? cartonType, int count, double lengthCm, double widthCm, double heightCm, int capacity, string? shelfLocation, string? prefix)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho lưu trữ.";
            return RedirectToAction(nameof(Index));
        }

        var (ok, msg, _) = await svc.GenerateCartonsBatchAsync(
            warehouseId,
            cartonType ?? "Thùng carton tiêu chuẩn",
            count,
            lengthCm,
            widthCm,
            heightCm,
            capacity,
            shelfLocation,
            prefix);

        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index), new { warehouseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pack(int id, int productId, int quantity, string? lotNo, double grossWeightKg, string? packerName, string? note)
    {
        var (ok, msg) = await svc.PackCartonAsync(id, productId, quantity, lotNo, grossWeightKg, packerName, note);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Seal(int id)
    {
        var (ok, msg) = await svc.SealCartonAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpack(int id, string? reason)
    {
        var (ok, msg) = await svc.UnpackCartonAsync(id, reason);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ship(int id, string refDocNo)
    {
        if (string.IsNullOrWhiteSpace(refDocNo))
        {
            TempData["Error"] = "Vui lòng nhập số chứng từ xuất kho.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var (ok, msg) = await svc.ShipCartonAsync(id, refDocNo);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeleteCartonAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, int? productId, CartonStatus? status, string? q)
    {
        var report = await svc.CartonsAsync(warehouseId, productId, status, q);

        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("BẢNG KÊ QUẢN LÝ THÙNG CARTON & ĐÓNG KIỆN HÀNG HÓA");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName};Mặt hàng:;{(report.ProductName ?? "Tất cả")}");
        sb.AppendLine($"Ngày xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Trạng thái lọc:;{(status.HasValue ? status.Value.ToString() : "Tất cả trạng thái")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã thùng (Carton);Mã QR;Kho lưu trữ;Loại thùng;Mã hàng;Tên mặt hàng;ĐVT;Số lô;Số lượng đóng;Sức chứa;Dài (cm);Rộng (cm);Cao (cm);Thể tích (m3);Trọng lượng cả bì (kg);Trạng thái;Vị trí lưu kho;Người đóng thùng;Ngày đóng;Ngày niêm phong;Chứng từ liên quan;Ghi chú");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var packed = r.PackedAt.HasValue ? r.PackedAt.Value.ToString("dd/MM/yyyy HH:mm") : "—";
            var sealedDate = r.SealedAt.HasValue ? r.SealedAt.Value.ToString("dd/MM/yyyy HH:mm") : "—";
            sb.AppendLine($"{stt++};\"{r.CartonCode}\";\"{r.QrCode ?? ""}\";\"{r.WarehouseName}\";\"{r.CartonType}\";\"{r.ProductCode ?? ""}\";\"{r.ProductName?.Replace("\"", "\"\"") ?? ""}\";\"{r.Uom ?? ""}\";\"{r.LotNo ?? ""}\";{r.Quantity};{r.Capacity};{r.LengthCm};{r.WidthCm};{r.HeightCm};{r.VolumeM3:F3};{r.GrossWeightKg:F2};\"{r.StatusLabel}\";\"{r.ShelfLocation ?? ""}\";\"{r.PackerName ?? ""}\";{packed};{sealedDate};\"{r.RefDocNo ?? ""}\";\"{r.Remark?.Replace("\"", "\"\"") ?? ""}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;TỔNG CỘNG:;{report.TotalCartons} thùng;;;;{report.TotalItemsPacked} sản phẩm;;;;;{report.TotalVolumeM3:F3} m3;{report.TotalWeightKg:F2} kg;Niêm phong: {report.SealedCount};Đang đóng: {report.PackingCount};Thùng rỗng: {report.EmptyCount};Đã xuất: {report.ShippedCount}");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BangKe_ThungCarton_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class BoxController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, int? productId, int? cartonId, BoxStatus? status, bool? flagMap, string? q)
    {
        var whs = await svc.WarehousesAsync();
        var prods = await svc.ProductsAsync();
        ViewBag.Warehouses = whs;
        ViewBag.Products = prods;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.ProductId = productId;
        ViewBag.CartonId = cartonId;
        ViewBag.Status = status;
        ViewBag.FlagMap = flagMap;
        ViewBag.Keyword = q ?? "";

        if (warehouseId.HasValue)
        {
            ViewBag.AvailableCartons = await svc.AvailableCartonsAsync(warehouseId.Value);
        }
        else
        {
            var allCartonsReport = await svc.CartonsAsync(null, null, null, null);
            ViewBag.AvailableCartons = allCartonsReport.Rows.Select(r => new { r.Id, r.CartonCode }).ToList();
        }

        var report = await svc.BoxesAsync(warehouseId, productId, cartonId, status, flagMap, q);
        return View(report);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var box = await svc.GetBoxAsync(id);
        if (box == null) return NotFound();

        ViewBag.Products = await svc.ProductsAsync();
        ViewBag.AvailableCartons = await svc.AvailableCartonsAsync(box.WarehouseId);
        return View(box);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int warehouseId, string? boxCode, string? boxType, double lengthCm, double widthCm, double heightCm, int capacity, int? cartonId, string? shelfLocation, string? remark)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho lưu trữ.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var box = new InventoryBox
            {
                WarehouseId = warehouseId,
                BoxCode = boxCode?.Trim() ?? "",
                BoxType = string.IsNullOrWhiteSpace(boxType) ? "Hộp duplex tiêu chuẩn" : boxType.Trim(),
                LengthCm = lengthCm > 0 ? lengthCm : 20,
                WidthCm = widthCm > 0 ? widthCm : 15,
                HeightCm = heightCm > 0 ? heightCm : 10,
                Capacity = capacity > 0 ? capacity : 10,
                CartonId = (cartonId.HasValue && cartonId.Value > 0) ? cartonId : null,
                FlagMap = (cartonId.HasValue && cartonId.Value > 0),
                ShelfLocation = shelfLocation?.Trim(),
                Remark = remark?.Trim(),
                Status = (cartonId.HasValue && cartonId.Value > 0) ? BoxStatus.InCarton : BoxStatus.Empty
            };

            var id = await svc.CreateBoxAsync(box);
            TempData["Success"] = $"Đã tạo mã hộp {box.BoxCode} thành công.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { warehouseId });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateBatch(int warehouseId, string? boxType, int count, double lengthCm, double widthCm, double heightCm, int capacity, int? cartonId, string? shelfLocation, string? prefix)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho lưu trữ.";
            return RedirectToAction(nameof(Index));
        }

        var (ok, msg, _) = await svc.GenerateBoxesBatchAsync(
            warehouseId,
            boxType ?? "Hộp duplex tiêu chuẩn",
            count,
            lengthCm,
            widthCm,
            heightCm,
            capacity,
            (cartonId.HasValue && cartonId.Value > 0) ? cartonId : null,
            shelfLocation,
            prefix);

        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index), new { warehouseId, cartonId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pack(int id, int productId, int quantity, string? lotNo, double grossWeightKg, string? packerName, string? secretNo, string? note)
    {
        var (ok, msg) = await svc.PackBoxAsync(id, productId, quantity, lotNo, grossWeightKg, packerName, secretNo, note);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Seal(int id, string? secretNo)
    {
        var (ok, msg) = await svc.SealBoxAsync(id, secretNo);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MapCarton(int id, int cartonId)
    {
        if (cartonId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn thùng carton muốn gán hộp vào.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var (ok, msg) = await svc.MapBoxToCartonAsync(id, cartonId);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UnmapCarton(int id)
    {
        var (ok, msg) = await svc.UnmapBoxFromCartonAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpack(int id, string? reason)
    {
        var (ok, msg) = await svc.UnpackBoxAsync(id, reason);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ship(int id, string refDocNo)
    {
        if (string.IsNullOrWhiteSpace(refDocNo))
        {
            TempData["Error"] = "Vui lòng nhập số chứng từ / phiếu xuất kho.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var (ok, msg) = await svc.ShipBoxAsync(id, refDocNo);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? warehouseId)
    {
        var (ok, msg) = await svc.DeleteBoxAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index), new { warehouseId });
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, int? productId, int? cartonId, BoxStatus? status, bool? flagMap, string? q)
    {
        var report = await svc.BoxesAsync(warehouseId, productId, cartonId, status, flagMap, q);

        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("BẢNG KÊ QUẢN LÝ HỘP ĐÓNG GÓI & PHÂN CẤP BAO BÌ KHO");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName};Mặt hàng:;{(report.ProductName ?? "Tất cả")};Thùng Carton:;{(report.CartonCode ?? "Tất cả")}");
        sb.AppendLine($"Ngày xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Trạng thái lọc:;{(status.HasValue ? status.Value.ToString() : "Tất cả trạng thái")};Gán thùng:;{(flagMap.HasValue ? (flagMap.Value ? "Đã gán thùng" : "Chưa gán thùng") : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã hộp (BoxNo);Mã QR;Đợt sinh mã;Mã niêm phong/Secret;Kho lưu trữ;Thùng Carton chứa;Quy cách hộp;Mã hàng;Tên mặt hàng;ĐVT;Số lô;Số lượng đóng;Sức chứa;Dài (cm);Rộng (cm);Cao (cm);Thể tích (m3);Trọng lượng cả bì (kg);Trạng thái;Trạng thái gán thùng;Vị trí kệ;Người đóng;Ngày đóng;Ngày niêm phong;Chứng từ liên quan;Ghi chú");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var packed = r.PackedAt.HasValue ? r.PackedAt.Value.ToString("dd/MM/yyyy HH:mm") : "—";
            var sealedDate = r.SealedAt.HasValue ? r.SealedAt.Value.ToString("dd/MM/yyyy HH:mm") : "—";
            sb.AppendLine($"{stt++};\"{r.BoxCode}\";\"{r.QrCode ?? ""}\";\"{r.GenTimesBoxNo ?? ""}\";\"{r.SecretNo ?? ""}\";\"{r.WarehouseName}\";\"{r.CartonCode ?? "Chưa gán"}\";\"{r.BoxType}\";\"{r.ProductCode ?? ""}\";\"{r.ProductName?.Replace("\"", "\"\"") ?? ""}\";\"{r.Uom ?? ""}\";\"{r.LotNo ?? ""}\";{r.Quantity};{r.Capacity};{r.LengthCm};{r.WidthCm};{r.HeightCm};{r.VolumeM3:F4};{r.GrossWeightKg:F2};\"{r.StatusLabel}\";\"{r.MapLabel}\";\"{r.ShelfLocation ?? ""}\";\"{r.PackerName ?? ""}\";{packed};{sealedDate};\"{r.RefDocNo ?? ""}\";\"{r.Remark?.Replace("\"", "\"\"") ?? ""}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;TỔNG CỘNG:;{report.TotalBoxes} hộp;;;;{report.TotalItemsPacked} sản phẩm;;;;;{report.TotalVolumeM3:F4} m3;{report.TotalWeightKg:F2} kg;Niêm phong: {report.SealedCount};Trong thùng: {report.InCartonCount};Đang đóng: {report.PackingCount};Hộp rỗng: {report.EmptyCount};Đã xuất: {report.ShippedCount}");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BangKe_HopDongGoi_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class InventoryInFGController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, InvInFGStatus? status, InvInFGFormType? formType, DateTime? fromDate, DateTime? toDate, string? q)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Status = status;
        ViewBag.FormType = formType;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.Keyword = q ?? "";

        var report = await svc.InventoryInFGsAsync(warehouseId, status, formType, fromDate, toDate, q);
        return View(report);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Warehouses = await svc.WarehousesAsync();
        ViewBag.Products = await svc.ProductsAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int warehouseId, InvInFGFormType formType, string workshopName, string? workOrderNo, string? shiftLeader,
        DateTime? date, string? remark, int[]? productIds, int[]? planQtys, int[]? actualQtys, int[]? defectQtys, decimal[]? unitCosts, string[]? notes, string? serialsInput)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho nhập thành phẩm.";
            return RedirectToAction(nameof(Create));
        }
        if (string.IsNullOrWhiteSpace(workshopName))
        {
            TempData["Error"] = "Vui lòng nhập tên phân xưởng / nhà máy sản xuất.";
            return RedirectToAction(nameof(Create));
        }

        var lines = new List<(int productId, int planQty, int actualQty, int defectQty, decimal unitCost, DateTime? prodDate, string? note)>();
        if (productIds != null)
        {
            for (int i = 0; i < productIds.Length; i++)
            {
                var pid = productIds[i];
                var plan = (planQtys != null && i < planQtys.Length) ? planQtys[i] : 0;
                var act = (actualQtys != null && i < actualQtys.Length) ? actualQtys[i] : 0;
                var def = (defectQtys != null && i < defectQtys.Length) ? defectQtys[i] : 0;
                var cost = (unitCosts != null && i < unitCosts.Length) ? unitCosts[i] : 0m;
                var note = (notes != null && i < notes.Length) ? notes[i] : null;
                if (pid > 0 && act > 0)
                {
                    lines.Add((pid, plan, act, def, cost, date, note));
                }
            }
        }

        if (lines.Count == 0)
        {
            TempData["Error"] = "Cần ít nhất 1 dòng mặt hàng thành phẩm có số lượng nhập > 0.";
            return RedirectToAction(nameof(Create));
        }

        // Tách danh sách Serial / IMEI nếu có nhập
        var serials = new List<(int productId, string serialNo, string? note)>();
        if (!string.IsNullOrWhiteSpace(serialsInput))
        {
            var linesArr = serialsInput.Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var firstPid = lines.First().productId;
            foreach (var item in linesArr)
            {
                var trimmed = item.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                if (trimmed.Contains(':'))
                {
                    var parts = trimmed.Split(':', 2);
                    var prodCode = parts[0].Trim();
                    var serNo = parts[1].Trim();
                    var matchedProd = (await svc.ProductsAsync()).FirstOrDefault(p => p.Code.Equals(prodCode, StringComparison.OrdinalIgnoreCase));
                    var targetPid = matchedProd?.Id ?? firstPid;
                    serials.Add((targetPid, serNo, null));
                }
                else
                {
                    serials.Add((firstPid, trimmed, null));
                }
            }
        }

        try
        {
            var doc = new InventoryInFG
            {
                WarehouseId = warehouseId,
                FormType = formType,
                WorkshopName = workshopName.Trim(),
                WorkOrderNo = workOrderNo?.Trim(),
                ShiftLeader = shiftLeader?.Trim(),
                Date = date ?? DateTime.Today,
                Remark = remark?.Trim(),
                CreatedBy = "admin"
            };

            var id = await svc.CreateInventoryInFGAsync(doc, lines, serials);
            TempData["Success"] = $"Đã lập phiếu nhập thành phẩm {doc.Code} (Chờ duyệt KCS). Bấm 'Phê duyệt & Nhập kho' để ghi sổ tồn kho.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Create));
        }
    }

    public async Task<IActionResult> Detail(int id)
    {
        var doc = await svc.GetInventoryInFGAsync(id);
        if (doc == null) return NotFound();
        return View(doc);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var (ok, msg) = await svc.ApproveInventoryInFGAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var (ok, msg) = await svc.CancelInventoryInFGAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, InvInFGStatus? status, InvInFGFormType? formType, DateTime? fromDate, DateTime? toDate, string? q)
    {
        var report = await svc.InventoryInFGsAsync(warehouseId, status, formType, fromDate, toDate, q);

        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("BẢNG KÊ QUẢN LÝ PHIẾU NHẬP KHO THÀNH PHẨM SẢN XUẤT");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName};Từ ngày:;{(report.FromDate.HasValue ? report.FromDate.Value.ToString("dd/MM/yyyy") : "Tất cả")};Đến ngày:;{(report.ToDate.HasValue ? report.ToDate.Value.ToString("dd/MM/yyyy") : "Tất cả")}");
        sb.AppendLine($"Trạng thái lọc:;{(status.HasValue ? status.Value.ToString() : "Tất cả")};Hình thức:;{(formType.HasValue ? formType.Value.ToString() : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã phiếu;Ngày nhập;Kho thành phẩm;Hình thức nhập;Phân xưởng / Đối tác SX;Lệnh SX / Mẻ;Quản đốc ca;SL Kế hoạch;SL Thực nhập;SL Phế phẩm;Tỷ lệ KCS (%);Tổng giá trị (VNĐ);Số lượng Serial;Trạng thái;Phiếu kho liên kết;Ghi chú");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var dateStr = r.Date.ToString("dd/MM/yyyy");
            sb.AppendLine($"{stt++};\"{r.Code}\";{dateStr};\"{r.WarehouseName}\";\"{r.FormTypeLabel}\";\"{r.WorkshopName}\";\"{r.WorkOrderNo ?? "—"}\";\"{r.ShiftLeader ?? "—"}\";{r.TotalPlanQty};{r.TotalActualQty};{r.TotalDefectQty};{r.PassRatePercent:F1}%;{r.TotalAmount:F0};{r.TotalSerialsCount};\"{r.StatusLabel}\";\"{r.StockDocCode ?? "—"}\";\"{r.Remark?.Replace("\"", "\"\"") ?? ""}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;;;;TỔNG CỘNG:;{report.TotalPlanQty};{report.TotalActualQty};{report.TotalDefectQty};;{report.TotalAmount:F0};;Chờ duyệt: {report.PendingCount};Đã nhập kho: {report.ApprovedCount};Đã hủy: {report.CancelledCount}");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BangKe_NhapKhoThanhPham_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class InventoryOutFGController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, InvOutFGStatus? status, InvOutFGType? outType, InvOutFGFormType? formType, DateTime? fromDate, DateTime? toDate, string? q)
    {
        var whs = await svc.WarehousesAsync();
        ViewBag.Warehouses = whs;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Status = status;
        ViewBag.OutType = outType;
        ViewBag.FormType = formType;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.Keyword = q ?? "";

        var report = await svc.InventoryOutFGsAsync(warehouseId, status, outType, formType, fromDate, toDate, q);
        return View(report);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Warehouses = await svc.WarehousesAsync();
        ViewBag.Products = await svc.ProductsAsync();
        ViewBag.Customers = await svc.CustomersAsync(activeOnly: true);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int warehouseId, InvOutFGType outType, InvOutFGFormType formType,
        string customerName, string? agentCode, string? deliveryAddress, string? driverName, string? driverPhone,
        string? plateNo, string? moocNo, string? orderNo, DateTime? date, string? remark,
        int[]? productIds, int[]? qtys, decimal[]? unitPrices, decimal[]? unitCosts, string[]? notes, string? serialsInput)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho xuất thành phẩm.";
            return RedirectToAction(nameof(Create));
        }
        if (string.IsNullOrWhiteSpace(customerName))
        {
            TempData["Error"] = "Vui lòng nhập tên khách hàng / đại lý nhận hàng.";
            return RedirectToAction(nameof(Create));
        }

        var lines = new List<(int productId, int qty, decimal unitPrice, decimal unitCost, string? note)>();
        if (productIds != null)
        {
            for (int i = 0; i < productIds.Length; i++)
            {
                var pid = productIds[i];
                var q = (qtys != null && i < qtys.Length) ? qtys[i] : 0;
                var price = (unitPrices != null && i < unitPrices.Length) ? unitPrices[i] : 0m;
                var cost = (unitCosts != null && i < unitCosts.Length) ? unitCosts[i] : 0m;
                var note = (notes != null && i < notes.Length) ? notes[i] : null;
                if (pid > 0 && q > 0)
                {
                    lines.Add((pid, q, price, cost, note));
                }
            }
        }

        if (lines.Count == 0)
        {
            TempData["Error"] = "Cần ít nhất 1 dòng mặt hàng thành phẩm có số lượng xuất > 0.";
            return RedirectToAction(nameof(Create));
        }

        // Tách danh sách Serial / IMEI nếu có nhập
        var serials = new List<(int productId, string serialNo, string? note)>();
        if (!string.IsNullOrWhiteSpace(serialsInput))
        {
            var linesArr = serialsInput.Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var firstPid = lines.First().productId;
            foreach (var item in linesArr)
            {
                var trimmed = item.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                if (trimmed.Contains(':'))
                {
                    var parts = trimmed.Split(':', 2);
                    var prodCode = parts[0].Trim();
                    var serNo = parts[1].Trim();
                    var matchedProd = (await svc.ProductsAsync()).FirstOrDefault(p => p.Code.Equals(prodCode, StringComparison.OrdinalIgnoreCase));
                    var targetPid = matchedProd?.Id ?? firstPid;
                    serials.Add((targetPid, serNo, null));
                }
                else
                {
                    serials.Add((firstPid, trimmed, null));
                }
            }
        }

        try
        {
            var doc = new InventoryOutFG
            {
                WarehouseId = warehouseId,
                OutType = outType,
                FormType = formType,
                CustomerName = customerName.Trim(),
                AgentCode = agentCode?.Trim(),
                DeliveryAddress = deliveryAddress?.Trim(),
                DriverName = driverName?.Trim(),
                DriverPhone = driverPhone?.Trim(),
                PlateNo = plateNo?.Trim(),
                MoocNo = moocNo?.Trim(),
                OrderNo = orderNo?.Trim(),
                Date = date ?? DateTime.Today,
                Remark = remark?.Trim(),
                CreatedBy = "admin"
            };

            var id = await svc.CreateInventoryOutFGAsync(doc, lines, serials);
            TempData["Success"] = $"Đã lập phiếu xuất thành phẩm {doc.Code} (Chờ duyệt xuất). Bấm 'Phê duyệt & Xuất kho' để trừ tồn kho.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Create));
        }
    }

    public async Task<IActionResult> Detail(int id)
    {
        var doc = await svc.GetInventoryOutFGAsync(id);
        if (doc == null) return NotFound();
        return View(doc);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var (ok, msg) = await svc.ApproveInventoryOutFGAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var (ok, msg) = await svc.CancelInventoryOutFGAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, InvOutFGStatus? status, InvOutFGType? outType, InvOutFGFormType? formType, DateTime? fromDate, DateTime? toDate, string? q)
    {
        var report = await svc.InventoryOutFGsAsync(warehouseId, status, outType, formType, fromDate, toDate, q);

        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("BẢNG KÊ QUẢN LÝ PHIẾU XUẤT KHO THÀNH PHẨM & VẬN CHUYỂN PHÂN PHỐI");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName};Từ ngày:;{(report.FromDate.HasValue ? report.FromDate.Value.ToString("dd/MM/yyyy") : "Tất cả")};Đến ngày:;{(report.ToDate.HasValue ? report.ToDate.Value.ToString("dd/MM/yyyy") : "Tất cả")}");
        sb.AppendLine($"Trạng thái lọc:;{(status.HasValue ? status.Value.ToString() : "Tất cả")};Nghiệp vụ xuất:;{(outType.HasValue ? outType.Value.ToString() : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã phiếu;Ngày xuất;Kho thành phẩm;Nghiệp vụ xuất;Hình thức xuất;Khách hàng / Đại lý;Mã đại lý;Địa chỉ nhận;Lái xe;SĐT lái xe;Biển số xe;Số moóc;Số đơn hàng;Tổng SL xuất;Tổng giá trị xuất (VNĐ);Số lượng Serial;Trạng thái;Phiếu kho liên kết;Ghi chú");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var dateStr = r.Date.ToString("dd/MM/yyyy");
            sb.AppendLine($"{stt++};\"{r.Code}\";{dateStr};\"{r.WarehouseName}\";\"{r.OutTypeLabel}\";\"{r.FormTypeLabel}\";\"{r.CustomerName}\";\"{r.AgentCode ?? "—"}\";\"{r.DeliveryAddress ?? "—"}\";\"{r.DriverName ?? "—"}\";\"{r.DriverPhone ?? "—"}\";\"{r.PlateNo ?? "—"}\";\"{r.MoocNo ?? "—"}\";\"{r.OrderNo ?? "—"}\";{r.TotalQty};{r.TotalAmount:F0};{r.TotalSerialsCount};\"{r.StatusLabel}\";\"{r.StockDocCode ?? "—"}\";\"{r.Remark?.Replace("\"", "\"\"") ?? ""}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;;;;;;;;;;TỔNG CỘNG:;{report.TotalQty};{report.TotalAmount:F0};;Chờ duyệt: {report.PendingCount};Đã xuất kho: {report.ApprovedCount};Đã hủy: {report.CancelledCount}");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BangKe_XuatKhoThanhPham_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class SummaryInReturnSupController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, string? supplierCode, DateTime? fromDate, DateTime? toDate, string? q)
    {
        var defFrom = fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var defTo = toDate ?? DateTime.Today;

        ViewBag.WarehouseId = warehouseId;
        ViewBag.SupplierCode = supplierCode;
        ViewBag.FromDate = defFrom.ToString("yyyy-MM-dd");
        ViewBag.ToDate = defTo.ToString("yyyy-MM-dd");
        ViewBag.Keyword = q;
        ViewBag.Warehouses = await svc.WarehousesAsync();
        ViewBag.Suppliers = await svc.SuppliersAsync(activeOnly: true);

        var report = await svc.SummaryInReturnSupReportAsync(warehouseId, supplierCode, defFrom, defTo, q);
        return View(report);
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, string? supplierCode, DateTime? fromDate, DateTime? toDate, string? q)
    {
        var defFrom = fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var defTo = toDate ?? DateTime.Today;
        var report = await svc.SummaryInReturnSupReportAsync(warehouseId, supplierCode, defFrom, defTo, q);

        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF');
        sb.AppendLine("BÁO CÁO TỔNG HỢP NHẬP MUA & TRẢ HÀNG NHÀ CUNG CẤP");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName};Nhà cung cấp:;{(string.IsNullOrWhiteSpace(report.SupplierCode) ? "Tất cả" : report.SupplierCode)}");
        sb.AppendLine($"Từ ngày:;{report.FromDate:dd/MM/yyyy};Đến ngày:;{report.ToDate:dd/MM/yyyy}");
        sb.AppendLine($"Ngày xuất:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã NCC;Tên Nhà Cung Cấp;Mã hàng;Tên mặt hàng;ĐVT;Tổng SL Nhập;Giá trị Nhập (VNĐ);Tổng SL Trả;Giá trị Trả (VNĐ);Thực nhận (Net);Giá trị Thực nhận (VNĐ);Tỷ lệ trả (%);Tỷ trọng (%/Tổng nhận);Đánh giá chất lượng");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            sb.AppendLine($"{stt++};\"{r.SupplierCode}\";\"{r.SupplierName.Replace("\"", "\"\"")}\";\"{r.ProductCode}\";\"{r.ProductName.Replace("\"", "\"\"")}\";\"{r.Uom}\";{r.InQty};{r.InAmount:F0};{r.ReturnQty};{r.ReturnAmount:F0};{r.NetQty};{r.NetAmount:F0};{r.ReturnRate:F2}%;{r.SharePercent:F2}%;\"{r.QualityGrade}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;;TỔNG CỘNG:;{report.TotalInQty};{report.TotalInAmount:F0};{report.TotalReturnQty};{report.TotalReturnAmount:F0};{report.TotalNetQty};{report.TotalNetAmount:F0};{report.AvgReturnRate:F2}%;100%;Số NCC: {report.SuppliersCount}");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BC_TongHop_NhapTraNCC_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class SupplierController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? q, bool? activeOnly)
    {
        ViewBag.Keyword = q ?? "";
        ViewBag.ActiveOnly = activeOnly;
        var list = await svc.SuppliersAsync(q, activeOnly);
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? code, string? contactName, string? phone, string? email, string? address, string? taxCode, string? note)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên nhà cung cấp.";
            return RedirectToAction(nameof(Index));
        }

        var sup = new Supplier
        {
            Code = code?.Trim() ?? "",
            Name = name.Trim(),
            ContactName = contactName?.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            TaxCode = taxCode?.Trim(),
            Note = note?.Trim(),
            IsActive = true
        };

        await svc.CreateSupplierAsync(sup);
        TempData["Success"] = $"Đã tạo nhà cung cấp '{sup.Name}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.ToggleSupplierStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }
}

public class CustomerController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? q, string? customerType, bool? activeOnly, string? customerGrpCode)
    {
        ViewBag.Keyword = q ?? "";
        ViewBag.CustomerType = customerType ?? "";
        ViewBag.CustomerGrpCode = customerGrpCode ?? "";
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.Areas = await svc.AreasAsync(activeOnly: true);
        ViewBag.CustomerGroups = await svc.CustomerGroupsAsync(activeOnly: true);
        var list = await svc.CustomersAsync(q, customerType, activeOnly, customerGrpCode);
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? code, string? customerType, string? contactName, string? contactPhone, string? phone, string? email, string? address, string? province, string? areaCode, string? customerGrpCode, string? taxCode, string? note)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên khách hàng / đại lý.";
            return RedirectToAction(nameof(Index));
        }

        var cust = new Customer
        {
            Code = code?.Trim() ?? "",
            Name = name.Trim(),
            CustomerType = string.IsNullOrWhiteSpace(customerType) ? "Đại lý phân phối" : customerType.Trim(),
            ContactName = contactName?.Trim(),
            ContactPhone = contactPhone?.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            Province = province?.Trim(),
            AreaCode = string.IsNullOrWhiteSpace(areaCode) ? null : areaCode.Trim().ToUpperInvariant(),
            CustomerGrpCode = string.IsNullOrWhiteSpace(customerGrpCode) ? null : customerGrpCode.Trim().ToUpperInvariant(),
            TaxCode = taxCode?.Trim(),
            Note = note?.Trim(),
            IsActive = true
        };

        await svc.CreateCustomerAsync(cust);
        TempData["Success"] = $"Đã tạo khách hàng '{cust.Name}'.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? customerType, string? contactName, string? contactPhone, string? phone, string? email, string? address, string? province, string? areaCode, string? customerGrpCode, string? taxCode, string? note, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên khách hàng / đại lý.";
            return RedirectToAction(nameof(Index));
        }

        var cust = new Customer
        {
            Name = name.Trim(),
            CustomerType = string.IsNullOrWhiteSpace(customerType) ? "Đại lý phân phối" : customerType.Trim(),
            ContactName = contactName?.Trim(),
            ContactPhone = contactPhone?.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            Province = province?.Trim(),
            AreaCode = string.IsNullOrWhiteSpace(areaCode) ? null : areaCode.Trim().ToUpperInvariant(),
            CustomerGrpCode = string.IsNullOrWhiteSpace(customerGrpCode) ? null : customerGrpCode.Trim().ToUpperInvariant(),
            TaxCode = taxCode?.Trim(),
            Note = note?.Trim(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdateCustomerAsync(id, cust);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.ToggleCustomerStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeleteCustomerAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetCustomerDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy khách hàng." });
        return Json(new
        {
            customer = new
            {
                detail.Customer.Id,
                detail.Customer.Code,
                detail.Customer.Name,
                detail.Customer.CustomerType,
                detail.Customer.ContactName,
                detail.Customer.ContactPhone,
                detail.Customer.Phone,
                detail.Customer.Email,
                detail.Customer.Address,
                detail.Customer.Province,
                detail.Customer.TaxCode,
                detail.Customer.Note,
                detail.Customer.IsActive,
                CreatedAt = detail.Customer.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            },
            totalOutQty = detail.TotalOutQty,
            totalReturnQty = detail.TotalReturnQty,
            outDocs = detail.OutDocs.Select(d => new
            {
                d.Id,
                d.Code,
                Warehouse = d.FromWarehouse?.Name ?? "—",
                Date = d.Date.ToString("dd/MM/yyyy"),
                d.TotalQty,
                Status = d.Status.ToString(),
                d.RefNo,
                d.Note
            }),
            outFGDocs = detail.OutFGDocs.Select(f => new
            {
                f.Id,
                f.Code,
                Warehouse = f.Warehouse?.Name ?? "—",
                Date = f.Date.ToString("dd/MM/yyyy"),
                f.TotalQty,
                Status = f.Status.ToString(),
                f.DeliveryAddress,
                f.PlateNo
            }),
            returns = detail.Returns.Select(r => new
            {
                r.Id,
                r.Code,
                Warehouse = r.Warehouse?.Name ?? "—",
                Date = r.Date.ToString("dd/MM/yyyy"),
                r.TotalQty,
                Status = r.Status.ToString(),
                r.InvoiceNo,
                r.Reason
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? q, string? customerType, bool? activeOnly)
    {
        var list = await svc.CustomersAsync(q, customerType, activeOnly);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("STT,MaKH,TenKhachHang,LoaiKhachHang,NguoiLienHe,SDTLienHe,DienThoai,Email,DiaChi,TinhThanh,MaSoThue,TrangThai,GhiChu,NgayTao");

        int stt = 1;
        foreach (var c in list)
        {
            sb.AppendLine(string.Join(",",
                stt++,
                EscapeCsv(c.Code),
                EscapeCsv(c.Name),
                EscapeCsv(c.CustomerType),
                EscapeCsv(c.ContactName ?? ""),
                EscapeCsv(c.ContactPhone ?? ""),
                EscapeCsv(c.Phone ?? ""),
                EscapeCsv(c.Email ?? ""),
                EscapeCsv(c.Address ?? ""),
                EscapeCsv(c.Province ?? ""),
                EscapeCsv(c.TaxCode ?? ""),
                c.IsActive ? "DangHoatDong" : "TamDung",
                EscapeCsv(c.Note ?? ""),
                c.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            ));
        }

        var preamble = System.Text.Encoding.UTF8.GetPreamble();
        var bytes = preamble.Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"KhachHang_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }

    private static string EscapeCsv(string val)
    {
        if (string.IsNullOrEmpty(val)) return "\"\"";
        return "\"" + val.Replace("\"", "\"\"") + "\"";
    }
}

public class InventoryOutDtlController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? outType, string? q)
    {
        ViewBag.Warehouses = await svc.WarehousesAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.FromDate = (fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).ToString("yyyy-MM-dd");
        ViewBag.ToDate = (toDate ?? DateTime.Today).ToString("yyyy-MM-dd");
        ViewBag.OutType = outType ?? "";
        ViewBag.Keyword = q ?? "";

        var report = await svc.InventoryOutDtlReportAsync(warehouseId, fromDate, toDate, outType, q);
        return View(report);
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? outType, string? q)
    {
        var report = await svc.InventoryOutDtlReportAsync(warehouseId, fromDate, toDate, outType, q);

        var sb = new System.Text.StringBuilder();
        // Thêm UTF-8 BOM để Excel hiển thị tiếng Việt không bị lỗi font
        sb.Append('\uFEFF');
        sb.AppendLine("STT;Số phiếu xuất;Ngày xuất;Loại xuất;Mã chứng từ gốc;Kho xuất;Bên nhận / Khách hàng;Mã vật tư;Tên vật tư;ĐVT;Số lượng xuất;Đơn giá vốn (đ);Tổng giá vốn (đ);Người xuất;Ghi chú");

        int stt = 1;
        foreach (var r in report.Items)
        {
            var dateStr = r.DocDate.ToString("dd/MM/yyyy HH:mm");
            sb.AppendLine($"{stt++};\"{r.DocNo}\";{dateStr};\"{r.OutTypeName}\";\"{r.RefNo ?? "—"}\";\"{r.WarehouseName}\";\"{r.CustomerName}\";\"{r.ProductCode}\";\"{r.ProductName}\";\"{r.UnitName}\";{r.Quantity};{r.UnitPrice:F0};{r.TotalAmount:F0};\"{r.CreatedBy}\";\"{r.Note?.Replace("\"", "\"\"") ?? ""}\"");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BaoCao_ChiTietXuatKho_{report.FromDate:yyyyMMdd}_{report.ToDate:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class InventoryInDtlController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? inType, string? q)
    {
        ViewBag.Warehouses = await svc.WarehousesAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.FromDate = (fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).ToString("yyyy-MM-dd");
        ViewBag.ToDate = (toDate ?? DateTime.Today).ToString("yyyy-MM-dd");
        ViewBag.InType = inType ?? "";
        ViewBag.Keyword = q ?? "";

        var report = await svc.InventoryInDtlReportAsync(warehouseId, fromDate, toDate, inType, q);
        return View(report);
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? inType, string? q)
    {
        var report = await svc.InventoryInDtlReportAsync(warehouseId, fromDate, toDate, inType, q);

        var sb = new System.Text.StringBuilder();
        // Thêm UTF-8 BOM để Excel hiển thị tiếng Việt không bị lỗi font
        sb.Append('\uFEFF');
        sb.AppendLine("STT;Số phiếu nhập;Ngày nhập;Loại nhập;Số hóa đơn / Chứng từ;Mã tham chiếu / Lệnh gốc;Kho nhập;Vị trí / Ô kệ;Nhà cung cấp / Đối tác giao;Mã vật tư;Tên vật tư;ĐVT;Số lượng nhập;Đơn giá nhập (đ);Thuế VAT (%);Thành tiền trước thuế (đ);Tiền thuế (đ);Tổng thanh toán (đ);Người nhập;Ghi chú");

        int stt = 1;
        foreach (var r in report.Items)
        {
            var dateStr = r.DocDate.ToString("dd/MM/yyyy HH:mm");
            sb.AppendLine($"{stt++};\"{r.DocNo}\";{dateStr};\"{r.InTypeName}\";\"{r.InvoiceNo ?? "—"}\";\"{r.RefNo ?? "—"}\";\"{r.WarehouseName}\";\"{r.LocationCode ?? "—"}\";\"{r.SupplierName}\";\"{r.ProductCode}\";\"{r.ProductName}\";\"{r.UnitName}\";{r.Quantity};{r.UnitPrice:F0};{r.VatPercent:F0};{r.ValBeforeTax:F0};{r.ValTax:F0};{r.TotalAmount:F0};\"{r.CreatedBy}\";\"{r.Note?.Replace("\"", "\"\"") ?? ""}\"");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BaoCao_ChiTietNhapKho_{report.FromDate:yyyyMMdd}_{report.ToDate:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class SummaryMonthlyController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? year, int? warehouseId, string? viewMode, string? q)
    {
        int targetYear = year ?? DateTime.Today.Year;
        string mode = string.IsNullOrWhiteSpace(viewMode) ? "ALL" : viewMode.ToUpperInvariant();

        ViewBag.Year = targetYear;
        ViewBag.WarehouseId = warehouseId;
        ViewBag.ViewMode = mode;
        ViewBag.Keyword = q ?? "";
        ViewBag.Warehouses = await svc.WarehousesAsync();

        var report = await svc.MonthlyMatrixReportAsync(targetYear, warehouseId, mode, q);
        return View(report);
    }

    public async Task<IActionResult> ExportCsv(int? year, int? warehouseId, string? viewMode, string? q)
    {
        int targetYear = year ?? DateTime.Today.Year;
        string mode = string.IsNullOrWhiteSpace(viewMode) ? "ALL" : viewMode.ToUpperInvariant();
        var report = await svc.MonthlyMatrixReportAsync(targetYear, warehouseId, mode, q);

        var sb = new System.Text.StringBuilder();
        // Thêm UTF-8 BOM để Excel hiển thị tiếng Việt không bị lỗi font
        sb.Append('\uFEFF');
        sb.AppendLine("BÁO CÁO MA TRẬN TỔNG HỢP NHẬP - XUẤT & TỒN KHO 12 THÁNG");
        sb.AppendLine($"Năm báo cáo:;{report.Year};Kho áp dụng:;{report.WarehouseName}");
        sb.AppendLine($"Chế độ xem:;{report.ViewMode};Ngày xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Tổng lượng nhập năm:;{report.TotalInYear};Tổng lượng xuất năm:;{report.TotalOutYear};Lưu chuyển ròng năm:;{report.NetMovementYear};Tháng cao điểm:;{report.PeakMonthName} ({report.PeakMonthVolume} lượt/sp)");
        sb.AppendLine();

        sb.AppendLine("Mã hàng;Tên mặt hàng;ĐVT;Tồn đầu năm;Chỉ tiêu;T1;T2;T3;T4;T5;T6;T7;T8;T9;T10;T11;T12;Tổng năm / Tồn cuối;Trung bình tháng;Tháng cao điểm");

        foreach (var item in report.Items)
        {
            var pCode = item.ProductCode;
            var pName = item.ProductName.Replace("\"", "\"\"");
            var uom = item.Uom;
            var opYear = item.OpeningYearQty;

            bool showIn = mode == "ALL" || mode == "IN_ONLY";
            bool showOut = mode == "ALL" || mode == "OUT_ONLY";
            bool showNet = mode == "ALL" || mode == "NET_ONLY";
            bool showBal = mode == "ALL" || mode == "BALANCE_ONLY";

            if (showIn && item.InRow != null)
            {
                var r = item.InRow;
                sb.AppendLine($"\"{pCode}\";\"{pName}\";\"{uom}\";{opYear};\"{r.ActionLabel}\";{r.M1};{r.M2};{r.M3};{r.M4};{r.M5};{r.M6};{r.M7};{r.M8};{r.M9};{r.M10};{r.M11};{r.M12};{r.TotalYear};{r.AvgMonth:F1};Tháng {r.PeakMonth:D2}");
            }
            if (showOut && item.OutRow != null)
            {
                var r = item.OutRow;
                sb.AppendLine($"\"{pCode}\";\"{pName}\";\"{uom}\";{opYear};\"{r.ActionLabel}\";{r.M1};{r.M2};{r.M3};{r.M4};{r.M5};{r.M6};{r.M7};{r.M8};{r.M9};{r.M10};{r.M11};{r.M12};{r.TotalYear};{r.AvgMonth:F1};Tháng {r.PeakMonth:D2}");
            }
            if (showNet && item.NetRow != null)
            {
                var r = item.NetRow;
                sb.AppendLine($"\"{pCode}\";\"{pName}\";\"{uom}\";{opYear};\"{r.ActionLabel}\";{r.M1};{r.M2};{r.M3};{r.M4};{r.M5};{r.M6};{r.M7};{r.M8};{r.M9};{r.M10};{r.M11};{r.M12};{r.TotalYear};{r.AvgMonth:F1};Tháng {r.PeakMonth:D2}");
            }
            if (showBal && item.BalanceRow != null)
            {
                var r = item.BalanceRow;
                sb.AppendLine($"\"{pCode}\";\"{pName}\";\"{uom}\";{opYear};\"{r.ActionLabel}\";{r.M1};{r.M2};{r.M3};{r.M4};{r.M5};{r.M6};{r.M7};{r.M8};{r.M9};{r.M10};{r.M11};{r.M12};{r.TotalYear};{r.AvgMonth:F1};Tháng {r.PeakMonth:D2}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"TỔNG CỘNG TOÀN KHO;;;;Nhập kho;{report.MonthlyTotalIn[0]};{report.MonthlyTotalIn[1]};{report.MonthlyTotalIn[2]};{report.MonthlyTotalIn[3]};{report.MonthlyTotalIn[4]};{report.MonthlyTotalIn[5]};{report.MonthlyTotalIn[6]};{report.MonthlyTotalIn[7]};{report.MonthlyTotalIn[8]};{report.MonthlyTotalIn[9]};{report.MonthlyTotalIn[10]};{report.MonthlyTotalIn[11]};{report.TotalInYear};{report.TotalInYear / 12.0:F1};-");
        sb.AppendLine($"TỔNG CỘNG TOÀN KHO;;;;Xuất kho;{report.MonthlyTotalOut[0]};{report.MonthlyTotalOut[1]};{report.MonthlyTotalOut[2]};{report.MonthlyTotalOut[3]};{report.MonthlyTotalOut[4]};{report.MonthlyTotalOut[5]};{report.MonthlyTotalOut[6]};{report.MonthlyTotalOut[7]};{report.MonthlyTotalOut[8]};{report.MonthlyTotalOut[9]};{report.MonthlyTotalOut[10]};{report.MonthlyTotalOut[11]};{report.TotalOutYear};{report.TotalOutYear / 12.0:F1};-");
        sb.AppendLine($"TỔNG CỘNG TOÀN KHO;;;;Tồn cuối kỳ;{report.MonthlyTotalBalance[0]};{report.MonthlyTotalBalance[1]};{report.MonthlyTotalBalance[2]};{report.MonthlyTotalBalance[3]};{report.MonthlyTotalBalance[4]};{report.MonthlyTotalBalance[5]};{report.MonthlyTotalBalance[6]};{report.MonthlyTotalBalance[7]};{report.MonthlyTotalBalance[8]};{report.MonthlyTotalBalance[9]};{report.MonthlyTotalBalance[10]};{report.MonthlyTotalBalance[11]};{report.MonthlyTotalBalance[11]};{report.MonthlyTotalBalance.Average():F1};-");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BaoCao_MaTran_NhapXuatTon12T_{report.Year}_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class StockExtendController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, StockExtendStatus? status, string? q)
    {
        ViewBag.Warehouses = await svc.WarehousesAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Status = status;
        ViewBag.Keyword = q ?? "";

        var report = await svc.StockExtendReportAsync(warehouseId, status, q);
        return View(report);
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, StockExtendStatus? status, string? q)
    {
        var report = await svc.StockExtendReportAsync(warehouseId, status, q);

        var sb = new System.Text.StringBuilder();
        // UTF-8 BOM để Excel hiển thị tiếng Việt chuẩn không bị vỡ font
        sb.Append('\uFEFF');
        sb.AppendLine("BÁO CÁO TỒN KHO MỞ RỘNG & DỰ PHÓNG KHẢ DỤNG (PORT TỪ RPT_INV_INVENTORYBALANCE_EXTEND)");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName};Ngày xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc trạng thái:;{(status.HasValue ? status.Value.ToString() : "Tất cả")};Từ khóa tìm kiếm:;{(string.IsNullOrWhiteSpace(q) ? "Tất cả" : q)}");
        sb.AppendLine($"Tổng tồn vật lý:;{report.TotalQtyTotalOK};Tổng tạm khóa/giữ chỗ:;{report.TotalQtyBlockOK};Tổng tồn khả dụng:;{report.TotalQtyAvailOK};Tổng hàng sắp về:;{report.TotalQtyBackOrder};Tổng tồn mở rộng:;{report.TotalQtyStockExt};Tổng giá trị tồn kho:;{report.TotalInventoryValue:F0} đ");
        sb.AppendLine();
        sb.AppendLine("STT;Mã hàng hoá;Tên hàng hoá;ĐVT;Kho lưu trữ;Tổng tồn vật lý (On-hand);Tạm khóa / Giữ chỗ (Blocked);Tồn khả dụng (Available);Tỷ lệ khả dụng (%);Hàng sắp về (Back-order);Tồn mở rộng dự phóng (Extended Stock);Định mức tối thiểu (MinStock);Định mức tối đa (MaxStock);Đơn giá vốn (đ);Giá trị tồn (đ);Trạng thái định mức;Lượng cần nhập thêm;Quản lý Lô;Quản lý Serial");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            sb.AppendLine($"{stt++};\"{r.ProductCode}\";\"{r.ProductName.Replace("\"", "\"\"")}\";\"{r.Uom}\";\"{r.WarehouseName}\";{r.QtyTotalOK};{r.QtyBlockOK};{r.QtyAvailOK};{r.AvailRate:F1}%;{r.QtyBackOrder};{r.QtyStockExt};{r.MinStock};{r.MaxStock};{r.CostPrice:F0};{r.TotalValue:F0};\"{r.StatusLabel}\";{r.ReplenishNeeded};\"{(r.HasLot ? "Có" : "Không")}\";\"{(r.HasSerial ? "Có" : "Không")}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;TỔNG CỘNG:;{report.TotalQtyTotalOK};{report.TotalQtyBlockOK};{report.TotalQtyAvailOK};{report.AvgAvailRate:F1}%;{report.TotalQtyBackOrder};{report.TotalQtyStockExt};;;;{report.TotalInventoryValue:F0};Cháy hàng: {report.OutOfStockCount};Dưới định mức: {report.UnderMinCount};Đạt chuẩn: {report.OptimalCount};Vượt định mức: {report.OverMaxCount};Cần nhập thêm: {report.UrgentReplenishCount} sp");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BaoCao_TonKhoMoRong_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class InventoryValuationController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, InventoryValuationAbcClass? abcClass, bool onlyHasStock = true, string? q = null, DateTime? asOfDate = null)
    {
        ViewBag.Warehouses = await svc.WarehousesAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.AbcClass = abcClass;
        ViewBag.OnlyHasStock = onlyHasStock;
        ViewBag.Keyword = q ?? "";
        ViewBag.AsOfDate = (asOfDate ?? DateTime.Today).ToString("yyyy-MM-dd");

        var report = await svc.InventoryValuationReportAsync(warehouseId, abcClass, onlyHasStock, q, asOfDate);
        return View(report);
    }

    public async Task<IActionResult> ExportCsv(int? warehouseId, InventoryValuationAbcClass? abcClass, bool onlyHasStock = true, string? q = null, DateTime? asOfDate = null)
    {
        var report = await svc.InventoryValuationReportAsync(warehouseId, abcClass, onlyHasStock, q, asOfDate);

        var sb = new System.Text.StringBuilder();
        // UTF-8 BOM để Excel hiển thị tiếng Việt chuẩn
        sb.Append('\uFEFF');
        sb.AppendLine("BÁO CÁO ĐÁNH GIÁ GIÁ TRỊ TỒN KHO & CƠ CẤU TÀI SẢN KHO (PORT TỪ RPT_INV_INVENTORYBALANCE_BYVALUE)");
        sb.AppendLine($"Kho hàng:;{report.WarehouseName};Ngày chốt số liệu:;{report.AsOfDate:dd/MM/yyyy};Ngày xuất file:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Phân hạng ABC:;{(abcClass.HasValue ? abcClass.Value.ToString() : "Tất cả")};Chỉ hiện có tồn:;{(onlyHasStock ? "Có" : "Không")};Từ khóa:;{(string.IsNullOrWhiteSpace(q) ? "Tất cả" : q)}");
        sb.AppendLine($"Tổng số mặt hàng:;{report.TotalItems};Tổng tồn vật lý:;{report.TotalPhysicalQty};Tổng tạm khóa:;{report.TotalBlockedQty};Tổng tồn khả dụng:;{report.TotalAvailableQty};Tổng giá trị tồn kho:;{report.GrandTotalValMixBase:N0} đ;Giá trị khả dụng:;{report.GrandTotalValAvail:N0} đ;Giá trị tạm khóa:;{report.GrandTotalValBlock:N0} đ;Tỷ lệ giá trị khả dụng:;{report.AvailValueRatio:F1}%");
        sb.AppendLine($"Cơ cấu ABC:;Nhóm A: {report.ClassACount} sp ({report.ClassAValue:N0} đ);Nhóm B: {report.ClassBCount} sp ({report.ClassBValue:N0} đ);Nhóm C: {report.ClassCCount} sp ({report.ClassCValue:N0} đ)");
        sb.AppendLine();
        sb.AppendLine("STT;Mã hàng hoá;Tên hàng hoá;ĐVT;Kho lưu trữ;Tồn vật lý (On-hand);Tạm khóa (Blocked);Tồn khả dụng (Avail);Tỷ lệ khả dụng (%);Đơn giá vốn kho (đ);Tổng giá trị tồn vật lý (đ);Giá trị hàng khả dụng (đ);Giá trị hàng tạm khóa (đ);Tỷ trọng tài sản (%);Phân hạng ABC;Cảnh báo rủi ro vốn;Quản lý Lô;Quản lý Serial");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            sb.AppendLine($"{stt++};\"{r.ProductCode}\";\"{r.ProductName.Replace("\"", "\"\"")}\";\"{r.Uom}\";\"{r.WarehouseName}\";{r.QtyTotalOK};{r.QtyBlockOK};{r.QtyAvailOK};{r.AvailRate:F1}%;{r.CostPrice:F0};{r.TotalValMixBase:F0};{r.TotalValAvail:F0};{r.TotalValBlock:F0};{r.SharePercent:F2}%;\"{r.AbcClassLabel}\";\"{r.CapitalRiskStatus}\";\"{(r.HasLot ? "Có" : "Không")}\";\"{(r.HasSerial ? "Có" : "Không")}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;;;TỔNG CỘNG:;{report.TotalPhysicalQty};{report.TotalBlockedQty};{report.TotalAvailableQty};;{report.GrandTotalValMixBase:F0};{report.GrandTotalValAvail:F0};{report.GrandTotalValBlock:F0};100.0%;;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"BaoCao_DinhGiaTonKho_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}

public class PartTypeController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? q, bool? activeOnly)
    {
        ViewBag.Keyword = q ?? "";
        ViewBag.ActiveOnly = activeOnly;
        var report = await svc.PartTypesReportAsync(q, activeOnly);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetPartTypeDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy loại mặt hàng." });
        return Json(new
        {
            item = new
            {
                detail.Item.Id,
                detail.Item.Code,
                detail.Item.Name,
                detail.Item.Remark,
                detail.Item.IsActive,
                CreatedAt = detail.Item.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            },
            totalProducts = detail.TotalProducts,
            totalStockQty = detail.TotalStockQty,
            products = detail.Products.Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.Uom,
                p.MinStock,
                p.MaxStock,
                p.CostPrice
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string code, string name, string? remark, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên loại mặt hàng.";
            return RedirectToAction(nameof(Index));
        }

        var item = new PartType
        {
            Code = code?.Trim().ToUpperInvariant() ?? "",
            Name = name.Trim(),
            Remark = remark?.Trim(),
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };

        try
        {
            await svc.CreatePartTypeAsync(item);
            TempData["Success"] = $"Đã tạo loại mặt hàng '{item.Name}' ({item.Code}).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? remark, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên loại mặt hàng.";
            return RedirectToAction(nameof(Index));
        }

        var item = new PartType
        {
            Name = name.Trim(),
            Remark = remark?.Trim(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdatePartTypeAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.TogglePartTypeStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeletePartTypeAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? q, bool? activeOnly)
    {
        var report = await svc.PartTypesReportAsync(q, activeOnly);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH MỤC PHÂN LOẠI LOẠI MẶT HÀNG KHO (MST_PARTTYPE)");
        sb.AppendLine($"Ngày xuất:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc:;{(activeOnly == true ? "Đang áp dụng" : activeOnly == false ? "Ngừng áp dụng" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã loại;Tên loại mặt hàng;Trạng thái;Số lượng SP;Tổng tồn kho;Ghi chú / Mô tả;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var statusStr = r.IsActive ? "Đang áp dụng" : "Ngừng áp dụng";
            sb.AppendLine($"{stt++};\"{r.Code}\";\"{r.Name.Replace("\"", "\"\"")}\";\"{statusStr}\";{r.ProductCount};{r.TotalStockQty};\"{r.Remark?.Replace("\"", "\"\"")}\";{r.CreatedAt:dd/MM/yyyy}");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG SỐ LOẠI MẶT HÀNG:;{report.TotalTypes};;;;");
        sb.AppendLine($";;TỔNG MẶT HÀNG ĐÃ PHÂN LOẠI:;{report.TotalProductsMapped};;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"LoaiMatHang_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}

public class BrandController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? q, bool? activeOnly)
    {
        ViewBag.Keyword = q ?? "";
        ViewBag.ActiveOnly = activeOnly;
        var report = await svc.BrandsReportAsync(q, activeOnly);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetBrandDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy thương hiệu." });
        return Json(new
        {
            id = detail.Item.Id,
            code = detail.Item.Code,
            name = detail.Item.Name,
            origin = detail.Item.Origin,
            remark = detail.Item.Remark,
            isActive = detail.Item.IsActive,
            totalProducts = detail.TotalProducts,
            totalStockQty = detail.TotalStockQty,
            products = detail.Products.Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.Uom,
                p.PartTypeCode,
                p.MinStock,
                p.MaxStock,
                p.CostPrice
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? code, string name, string? origin, string? remark, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên thương hiệu.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var item = new Brand
            {
                Code = code?.Trim().ToUpperInvariant() ?? "",
                Name = name.Trim(),
                Origin = origin?.Trim(),
                Remark = remark?.Trim(),
                IsActive = isActive
            };
            await svc.CreateBrandAsync(item);
            TempData["Success"] = $"Đã tạo mới thương hiệu '{item.Name}' ({item.Code}).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? origin, string? remark, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên thương hiệu.";
            return RedirectToAction(nameof(Index));
        }

        var item = new Brand
        {
            Name = name.Trim(),
            Origin = origin?.Trim(),
            Remark = remark?.Trim(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdateBrandAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.ToggleBrandStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeleteBrandAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? q, bool? activeOnly)
    {
        var report = await svc.BrandsReportAsync(q, activeOnly);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH MỤC THƯƠNG HIỆU / NHÃN HIỆU HÀNG HÓA KHO (MST_BRAND)");
        sb.AppendLine($"Ngày xuất:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc:;{(activeOnly == true ? "Đang áp dụng" : activeOnly == false ? "Ngừng áp dụng" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã thương hiệu;Tên thương hiệu;Xuất xứ / Quốc gia;Trạng thái;Số lượng SP;Tổng tồn kho;Ghi chú / Phân khúc;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var statusStr = r.IsActive ? "Đang áp dụng" : "Ngừng áp dụng";
            sb.AppendLine($"{stt++};\"{r.Code}\";\"{r.Name.Replace("\"", "\"\"")}\";\"{r.Origin?.Replace("\"", "\"\"") ?? "—"}\";\"{statusStr}\";{r.ProductCount};{r.TotalStockQty};\"{r.Remark?.Replace("\"", "\"\"")}\";{r.CreatedAt:dd/MM/yyyy}");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG SỐ THƯƠNG HIỆU:;{report.TotalBrands};;;;");
        sb.AppendLine($";;TỔNG MẶT HÀNG ĐÃ GÁN THƯƠNG HIỆU:;{report.TotalProductsMapped};;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"ThuongHieu_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}

public class PartUnitController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? q, bool? activeOnly, bool? standardOnly)
    {
        ViewBag.Keyword = q ?? "";
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.StandardOnly = standardOnly;
        var report = await svc.PartUnitsReportAsync(q, activeOnly, standardOnly);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetPartUnitDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy đơn vị tính." });
        return Json(new
        {
            id = detail.Item.Id,
            code = detail.Item.Code,
            name = detail.Item.Name,
            isStandard = detail.Item.IsStandard,
            remark = detail.Item.Remark,
            isActive = detail.Item.IsActive,
            totalProducts = detail.TotalProducts,
            totalStockQty = detail.TotalStockQty,
            products = detail.Products.Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.Uom,
                p.PartTypeCode,
                p.BrandCode,
                p.MinStock,
                p.MaxStock,
                p.CostPrice
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? code, string name, bool isStandard = true, string? remark = null, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên đơn vị tính.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var item = new PartUnit
            {
                Code = code?.Trim().ToUpperInvariant() ?? "",
                Name = name.Trim(),
                IsStandard = isStandard,
                Remark = remark?.Trim(),
                IsActive = isActive
            };
            await svc.CreatePartUnitAsync(item);
            TempData["Success"] = $"Đã tạo mới đơn vị tính '{item.Name}' ({item.Code}).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, bool isStandard = true, string? remark = null, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên đơn vị tính.";
            return RedirectToAction(nameof(Index));
        }

        var item = new PartUnit
        {
            Name = name.Trim(),
            IsStandard = isStandard,
            Remark = remark?.Trim(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdatePartUnitAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.TogglePartUnitStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeletePartUnitAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? q, bool? activeOnly, bool? standardOnly)
    {
        var report = await svc.PartUnitsReportAsync(q, activeOnly, standardOnly);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH MỤC ĐƠN VỊ TÍNH HÀNG HÓA / VẬT TƯ KHO (MST_PARTUNIT)");
        sb.AppendLine($"Ngày xuất:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc trạng thái:;{(activeOnly == true ? "Đang áp dụng" : activeOnly == false ? "Ngừng áp dụng" : "Tất cả")}");
        sb.AppendLine($"Bộ lọc loại chuẩn:;{(standardOnly == true ? "Đơn vị cơ bản/chuẩn" : standardOnly == false ? "Đơn vị quy đổi" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã đơn vị tính;Tên đơn vị tính;Phân loại;Trạng thái;Số lượng SP;Tổng tồn kho;Ghi chú / Quy cách;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var typeStr = r.IsStandard ? "Đơn vị cơ bản / Chuẩn" : "Đơn vị quy đổi / Thứ cấp";
            var statusStr = r.IsActive ? "Đang áp dụng" : "Ngừng áp dụng";
            sb.AppendLine($"{stt++};\"{r.Code}\";\"{r.Name.Replace("\"", "\"\"")}\";\"{typeStr}\";\"{statusStr}\";{r.ProductCount};{r.TotalStockQty};\"{r.Remark?.Replace("\"", "\"\"")}\";{r.CreatedAt:dd/MM/yyyy}");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG SỐ ĐƠN VỊ TÍNH:;{report.TotalUnits};;;;");
        sb.AppendLine($";;ĐƠN VỊ CƠ BẢN / CHUẨN:;{report.StandardUnitsCount};;;;");
        sb.AppendLine($";;TỔNG MẶT HÀNG SỬ DỤNG:;{report.TotalProductsMapped};;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"DonViTinh_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}

public class PartMaterialTypeController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? q, bool? activeOnly)
    {
        ViewBag.Keyword = q ?? "";
        ViewBag.ActiveOnly = activeOnly;
        var report = await svc.PartMaterialTypesReportAsync(q, activeOnly);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetPartMaterialTypeDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy nhóm chất liệu." });
        return Json(new
        {
            id = detail.Item.Id,
            code = detail.Item.Code,
            name = detail.Item.Name,
            remark = detail.Item.Remark,
            isActive = detail.Item.IsActive,
            totalProducts = detail.TotalProducts,
            totalStockQty = detail.TotalStockQty,
            products = detail.Products.Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.Uom,
                p.PartTypeCode,
                p.BrandCode,
                p.PMType,
                p.MinStock,
                p.MaxStock,
                p.CostPrice
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? code, string name, string? remark = null, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên nhóm chất liệu / vật liệu.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var item = new PartMaterialType
            {
                Code = code?.Trim().ToUpperInvariant() ?? "",
                Name = name.Trim(),
                Remark = remark?.Trim(),
                IsActive = isActive
            };
            await svc.CreatePartMaterialTypeAsync(item);
            TempData["Success"] = $"Đã tạo mới nhóm chất liệu '{item.Name}' ({item.Code}).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? remark = null, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên nhóm chất liệu / vật liệu.";
            return RedirectToAction(nameof(Index));
        }

        var item = new PartMaterialType
        {
            Name = name.Trim(),
            Remark = remark?.Trim(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdatePartMaterialTypeAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.TogglePartMaterialTypeStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeletePartMaterialTypeAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? q, bool? activeOnly)
    {
        var report = await svc.PartMaterialTypesReportAsync(q, activeOnly);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH MỤC NHÓM CHẤT LIỆU / VẬT LIỆU HÀNG HÓA KHO (MST_PARTMATERIALTYPE)");
        sb.AppendLine($"Ngày xuất:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc trạng thái:;{(activeOnly == true ? "Đang áp dụng" : activeOnly == false ? "Ngừng áp dụng" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã nhóm chất liệu;Tên nhóm chất liệu / vật liệu;Trạng thái;Số lượng SP;Tổng tồn kho;Ghi chú / Quy cách bảo quản;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var statusStr = r.IsActive ? "Đang áp dụng" : "Ngừng áp dụng";
            sb.AppendLine($"{stt++};\"{r.Code}\";\"{r.Name.Replace("\"", "\"\"")}\";\"{statusStr}\";{r.ProductCount};{r.TotalStockQty};\"{r.Remark?.Replace("\"", "\"\"")}\";{r.CreatedAt:dd/MM/yyyy}");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG SỐ NHÓM CHẤT LIỆU:;{report.TotalMaterialTypes};;;;");
        sb.AppendLine($";;ĐANG ÁP DỤNG:;{report.ActiveCount};;;;");
        sb.AppendLine($";;NGỪNG ÁP DỤNG:;{report.InactiveCount};;;;");
        sb.AppendLine($";;TỔNG MẶT HÀNG ĐÃ GÁN:;{report.TotalProductsMapped};;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"ChatLieuVatLieu_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}

public class ProductModelController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? q, string? brandCode, bool? activeOnly)
    {
        ViewBag.Keyword = q ?? "";
        ViewBag.BrandCode = brandCode ?? "";
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.Brands = await svc.BrandsAsync(activeOnly: true);

        var report = await svc.ProductModelsReportAsync(q, brandCode, activeOnly);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetProductModelDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy dòng sản phẩm / model." });
        return Json(new
        {
            id = detail.Item.Id,
            code = detail.Item.Code,
            name = detail.Item.Name,
            brandCode = detail.Item.BrandCode,
            brandName = detail.Brand?.Name ?? detail.Item.BrandCode,
            orgModelCode = detail.Item.OrgModelCode,
            remark = detail.Item.Remark,
            isActive = detail.Item.IsActive,
            totalProducts = detail.TotalProducts,
            totalStockQty = detail.TotalStockQty,
            products = detail.Products.Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.Uom,
                p.PartTypeCode,
                p.BrandCode,
                p.ModelCode,
                p.PMType,
                p.MinStock,
                p.MaxStock,
                p.CostPrice
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? code, string name, string? brandCode = null, string? orgModelCode = null, string? remark = null, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên dòng sản phẩm / model.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var item = new ProductModel
            {
                Code = code?.Trim().ToUpperInvariant() ?? "",
                Name = name.Trim(),
                BrandCode = string.IsNullOrWhiteSpace(brandCode) ? null : brandCode.Trim().ToUpperInvariant(),
                OrgModelCode = string.IsNullOrWhiteSpace(orgModelCode) ? null : orgModelCode.Trim(),
                Remark = remark?.Trim(),
                IsActive = isActive
            };
            await svc.CreateProductModelAsync(item);
            TempData["Success"] = $"Đã tạo mới dòng sản phẩm / model '{item.Name}' ({item.Code}).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? brandCode = null, string? orgModelCode = null, string? remark = null, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên dòng sản phẩm / model.";
            return RedirectToAction(nameof(Index));
        }

        var item = new ProductModel
        {
            Name = name.Trim(),
            BrandCode = string.IsNullOrWhiteSpace(brandCode) ? null : brandCode.Trim().ToUpperInvariant(),
            OrgModelCode = string.IsNullOrWhiteSpace(orgModelCode) ? null : orgModelCode.Trim(),
            Remark = remark?.Trim(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdateProductModelAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.ToggleProductModelStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeleteProductModelAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? q, string? brandCode, bool? activeOnly)
    {
        var report = await svc.ProductModelsReportAsync(q, brandCode, activeOnly);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH MỤC DÒNG SẢN PHẨM / MODEL HÀNG HÓA KHO (MST_MODEL)");
        sb.AppendLine($"Ngày xuất:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc thương hiệu:;{(string.IsNullOrEmpty(brandCode) ? "Tất cả" : brandCode)}");
        sb.AppendLine($"Bộ lọc trạng thái:;{(activeOnly == true ? "Đang áp dụng" : activeOnly == false ? "Ngừng áp dụng" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã Model;Tên dòng sản phẩm / Model;Thương hiệu;Mã OrgModel (Hãng);Trạng thái;Số lượng SP;Tổng tồn kho;Ghi chú / Đặc tính dòng;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var statusStr = r.IsActive ? "Đang áp dụng" : "Ngừng áp dụng";
            var brandStr = !string.IsNullOrEmpty(r.BrandName) ? $"{r.BrandName} ({r.BrandCode})" : (r.BrandCode ?? "—");
            sb.AppendLine($"{stt++};\"{r.Code}\";\"{r.Name.Replace("\"", "\"\"")}\";\"{brandStr.Replace("\"", "\"\"")}\";\"{r.OrgModelCode?.Replace("\"", "\"\"")}\";\"{statusStr}\";{r.ProductCount};{r.TotalStockQty};\"{r.Remark?.Replace("\"", "\"\"")}\";{r.CreatedAt:dd/MM/yyyy}");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG SỐ MODEL:;{report.TotalModels};;;;;");
        sb.AppendLine($";;ĐANG ÁP DỤNG:;{report.ActiveCount};;;;;");
        sb.AppendLine($";;NGỪNG ÁP DỤNG:;{report.InactiveCount};;;;;");
        sb.AppendLine($";;TỔNG MẶT HÀNG ĐÃ GÁN:;{report.TotalProductsMapped};;;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"DongSanPham_Model_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}

public class InventoryTypeController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? q, bool? activeOnly)
    {
        ViewBag.Keyword = q ?? "";
        ViewBag.ActiveOnly = activeOnly;
        var report = await svc.InventoryTypesReportAsync(q, activeOnly);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetInventoryTypeDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy loại kho." });
        return Json(new
        {
            item = new
            {
                detail.Item.Id,
                detail.Item.Code,
                detail.Item.Name,
                detail.Item.Remark,
                detail.Item.IsActive,
                CreatedAt = detail.Item.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            },
            totalWarehouses = detail.TotalWarehouses,
            totalStockQty = detail.TotalStockQty,
            warehouses = detail.Warehouses.Select(w => new
            {
                w.Id,
                w.Code,
                w.Name,
                w.Address,
                w.Remark
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string code, string name, string? remark, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên loại kho.";
            return RedirectToAction(nameof(Index));
        }

        var item = new InventoryType
        {
            Code = code?.Trim().ToUpperInvariant() ?? "",
            Name = name.Trim(),
            Remark = remark?.Trim(),
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };

        try
        {
            await svc.CreateInventoryTypeAsync(item);
            TempData["Success"] = $"Đã tạo loại kho '{item.Name}' ({item.Code}).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? remark, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên loại kho.";
            return RedirectToAction(nameof(Index));
        }

        var item = new InventoryType
        {
            Name = name.Trim(),
            Remark = remark?.Trim(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdateInventoryTypeAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.ToggleInventoryTypeStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeleteInventoryTypeAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? q, bool? activeOnly)
    {
        var report = await svc.InventoryTypesReportAsync(q, activeOnly);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH MỤC PHÂN LOẠI LOẠI KHO (MST_INVENTORYTYPE)");
        sb.AppendLine($"Ngày xuất:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc:;{(activeOnly == true ? "Đang áp dụng" : activeOnly == false ? "Ngừng áp dụng" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã loại kho;Tên loại kho;Trạng thái;Số lượng kho;Tổng tồn kho thực tế;Ghi chú / Mục đích sử dụng;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var statusStr = r.IsActive ? "Đang áp dụng" : "Ngừng áp dụng";
            sb.AppendLine($"{stt++};\"{r.Code}\";\"{r.Name.Replace("\"", "\"\"")}\";\"{statusStr}\";{r.WarehouseCount};{r.TotalStockQty};\"{r.Remark?.Replace("\"", "\"\"")}\";{r.CreatedAt:dd/MM/yyyy}");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG SỐ LOẠI KHO:;{report.TotalTypes};;;;");
        sb.AppendLine($";;TỔNG KHO ĐÃ PHÂN LOẠI:;{report.TotalWarehousesMapped};;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"LoaiKho_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}

public class InventoryLevelTypeController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? q, bool? activeOnly)
    {
        ViewBag.Keyword = q ?? "";
        ViewBag.ActiveOnly = activeOnly;
        var report = await svc.InventoryLevelTypesReportAsync(q, activeOnly);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetInventoryLevelTypeDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy cấp kho." });
        return Json(new
        {
            item = new
            {
                detail.Item.Id,
                detail.Item.Code,
                detail.Item.Name,
                detail.Item.Remark,
                detail.Item.IsActive,
                CreatedAt = detail.Item.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            },
            totalWarehouses = detail.TotalWarehouses,
            totalStockQty = detail.TotalStockQty,
            warehouses = detail.Warehouses.Select(w => new
            {
                w.Id,
                w.Code,
                w.Name,
                w.Address,
                w.Remark
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string code, string name, string? remark, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên cấp kho.";
            return RedirectToAction(nameof(Index));
        }

        var item = new InventoryLevelType
        {
            Code = code?.Trim().ToUpperInvariant() ?? "",
            Name = name.Trim(),
            Remark = remark?.Trim(),
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };

        try
        {
            await svc.CreateInventoryLevelTypeAsync(item);
            TempData["Success"] = $"Đã tạo cấp kho '{item.Name}' ({item.Code}).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? remark, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên cấp kho.";
            return RedirectToAction(nameof(Index));
        }

        var item = new InventoryLevelType
        {
            Name = name.Trim(),
            Remark = remark?.Trim(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdateInventoryLevelTypeAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.ToggleInventoryLevelTypeStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeleteInventoryLevelTypeAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? q, bool? activeOnly)
    {
        var report = await svc.InventoryLevelTypesReportAsync(q, activeOnly);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH MỤC PHÂN CẤP CẤP KHO (MST_INVENTORYLEVELTYPE)");
        sb.AppendLine($"Ngày xuất:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc:;{(activeOnly == true ? "Đang áp dụng" : activeOnly == false ? "Ngừng áp dụng" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã cấp kho;Tên cấp kho;Trạng thái;Số lượng kho trực thuộc;Tổng tồn kho thực tế;Ghi chú / Quy mô thẩm quyền;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var statusStr = r.IsActive ? "Đang áp dụng" : "Ngừng áp dụng";
            sb.AppendLine($"{stt++};\"{r.Code}\";\"{r.Name.Replace("\"", "\"\"")}\";\"{statusStr}\";{r.WarehouseCount};{r.TotalStockQty};\"{r.Remark?.Replace("\"", "\"\"")}\";{r.CreatedAt:dd/MM/yyyy}");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG SỐ CẤP KHO:;{report.TotalLevels};;;;");
        sb.AppendLine($";;TỔNG KHO ĐÃ PHÂN CẤP:;{report.TotalWarehousesMapped};;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"CapKho_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}

public class InventoryInTypeController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? q, bool? activeOnly, bool? statisticOnly)
    {
        ViewBag.Keyword = q ?? "";
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.StatisticOnly = statisticOnly;
        var report = await svc.InventoryInTypesReportAsync(q, activeOnly, statisticOnly);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetInventoryInTypeDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy loại nhập kho." });
        return Json(new
        {
            item = new
            {
                detail.Item.Id,
                detail.Item.Code,
                detail.Item.Name,
                detail.Item.FlagStatistic,
                detail.Item.Remark,
                detail.Item.IsActive,
                CreatedAt = detail.Item.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            },
            totalDocs = detail.TotalDocs,
            totalQtyIn = detail.TotalQtyIn,
            docs = detail.Docs.Take(15).Select(d => new
            {
                d.Id,
                d.Code,
                Date = d.Date.ToString("dd/MM/yyyy"),
                WarehouseName = d.ToWarehouse?.Name ?? "",
                d.TotalQty,
                d.Note,
                d.RefNo
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string code, string name, bool flagStatistic = true, string? remark = null, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên loại nhập kho.";
            return RedirectToAction(nameof(Index));
        }

        var item = new InventoryInType
        {
            Code = code?.Trim().ToUpperInvariant() ?? "",
            Name = name.Trim(),
            FlagStatistic = flagStatistic,
            Remark = remark?.Trim(),
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };

        try
        {
            await svc.CreateInventoryInTypeAsync(item);
            TempData["Success"] = $"Đã tạo loại nhập kho '{item.Name}' ({item.Code}).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, bool flagStatistic = true, string? remark = null, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên loại nhập kho.";
            return RedirectToAction(nameof(Index));
        }

        var item = new InventoryInType
        {
            Name = name.Trim(),
            FlagStatistic = flagStatistic,
            Remark = remark?.Trim(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdateInventoryInTypeAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.ToggleInventoryInTypeStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatistic(int id)
    {
        var (ok, msg) = await svc.ToggleInventoryInTypeStatisticAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeleteInventoryInTypeAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? q, bool? activeOnly, bool? statisticOnly)
    {
        var report = await svc.InventoryInTypesReportAsync(q, activeOnly, statisticOnly);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH MỤC LOẠI HÌNH & LÝ DO NHẬP KHO (MST_INVIN_TYPE)");
        sb.AppendLine($"Ngày xuất:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc trạng thái:;{(activeOnly == true ? "Đang áp dụng" : activeOnly == false ? "Ngừng áp dụng" : "Tất cả")}");
        sb.AppendLine($"Bộ lọc thống kê:;{(statisticOnly == true ? "Có tính thống kê" : statisticOnly == false ? "Không tính thống kê" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã loại nhập kho;Tên loại nhập kho;Tính thống kê mua/SL;Trạng thái;Số phiếu nhập;Tổng SL nhập;Ghi chú / Quy trình;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var statStr = r.FlagStatistic ? "Có tính thống kê" : "Không tính";
            var statusStr = r.IsActive ? "Đang áp dụng" : "Ngừng áp dụng";
            sb.AppendLine($"{stt++};\"{r.Code}\";\"{r.Name.Replace("\"", "\"\"")}\";\"{statStr}\";\"{statusStr}\";{r.TotalDocsCount};{r.TotalQtyIn};\"{r.Remark?.Replace("\"", "\"\"")}\";{r.CreatedAt:dd/MM/yyyy}");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG SỐ LOẠI NHẬP:;{report.TotalTypes};;;;;");
        sb.AppendLine($";;TỔNG PHIẾU NHẬP LIÊN QUAN:;{report.TotalInDocsCount};;;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"LoaiNhapKho_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}

public class InventoryOutTypeController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? q, bool? activeOnly, bool? statisticOnly)
    {
        ViewBag.Keyword = q ?? "";
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.StatisticOnly = statisticOnly;
        var report = await svc.InventoryOutTypesReportAsync(q, activeOnly, statisticOnly);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetInventoryOutTypeDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy loại xuất kho." });
        return Json(new
        {
            item = new
            {
                detail.Item.Id,
                detail.Item.Code,
                detail.Item.Name,
                detail.Item.FlagStatistic,
                detail.Item.Remark,
                detail.Item.IsActive,
                CreatedAt = detail.Item.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            },
            totalDocs = detail.TotalDocs,
            totalQtyOut = detail.TotalQtyOut,
            docs = detail.Docs.Take(15).Select(d => new
            {
                d.Id,
                d.Code,
                Date = d.Date.ToString("dd/MM/yyyy"),
                WarehouseName = d.FromWarehouse?.Name ?? "",
                d.TotalQty,
                CustomerName = d.CustomerName ?? "",
                d.Note,
                d.RefNo
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string code, string name, bool flagStatistic = true, string? remark = null, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên loại xuất kho.";
            return RedirectToAction(nameof(Index));
        }

        var item = new InventoryOutType
        {
            Code = code?.Trim().ToUpperInvariant() ?? "",
            Name = name.Trim(),
            FlagStatistic = flagStatistic,
            Remark = remark?.Trim(),
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };

        try
        {
            await svc.CreateInventoryOutTypeAsync(item);
            TempData["Success"] = $"Đã tạo loại xuất kho '{item.Name}' ({item.Code}).";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, bool flagStatistic = true, string? remark = null, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên loại xuất kho.";
            return RedirectToAction(nameof(Index));
        }

        var item = new InventoryOutType
        {
            Name = name.Trim(),
            FlagStatistic = flagStatistic,
            Remark = remark?.Trim(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdateInventoryOutTypeAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.ToggleInventoryOutTypeStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatistic(int id)
    {
        var (ok, msg) = await svc.ToggleInventoryOutTypeStatisticAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeleteInventoryOutTypeAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? q, bool? activeOnly, bool? statisticOnly)
    {
        var report = await svc.InventoryOutTypesReportAsync(q, activeOnly, statisticOnly);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH MỤC LOẠI HÌNH & LÝ DO XUẤT KHO (MST_INVOUT_TYPE)");
        sb.AppendLine($"Ngày xuất:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc trạng thái:;{(activeOnly == true ? "Đang áp dụng" : activeOnly == false ? "Ngừng áp dụng" : "Tất cả")}");
        sb.AppendLine($"Bộ lọc thống kê:;{(statisticOnly == true ? "Có tính thống kê" : statisticOnly == false ? "Không tính thống kê" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã loại xuất kho;Tên loại xuất kho;Tính thống kê xuất/DT;Trạng thái;Số phiếu xuất;Tổng SL xuất;Ghi chú / Quy trình;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var statStr = r.FlagStatistic ? "Có tính thống kê" : "Không tính";
            var statusStr = r.IsActive ? "Đang áp dụng" : "Ngừng áp dụng";
            sb.AppendLine($"{stt++};\"{r.Code}\";\"{r.Name.Replace("\"", "\"\"")}\";\"{statStr}\";\"{statusStr}\";{r.TotalDocsCount};{r.TotalQtyOut};\"{r.Remark?.Replace("\"", "\"\"")}\";{r.CreatedAt:dd/MM/yyyy}");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG SỐ LOẠI XUẤT:;{report.TotalTypes};;;;;");
        sb.AppendLine($";;TỔNG PHIẾU XUẤT LIÊN QUAN:;{report.TotalOutDocsCount};;;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"LoaiXuatKho_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}

public class UserMapInventoryController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId, string? userRole, bool? activeOnly, string? q)
    {
        ViewBag.Warehouses = await svc.WarehousesAsync();
        ViewBag.WarehouseId = warehouseId;
        ViewBag.UserRole = userRole ?? "";
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.Keyword = q ?? "";
        ViewBag.Summaries = await svc.GetWarehouseAssignmentSummariesAsync();

        var report = await svc.UserMapInventoriesReportAsync(warehouseId, userRole, activeOnly, q);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var item = await svc.GetUserMapInventoryAsync(id);
        if (item == null) return NotFound(new { error = "Không tìm thấy bản ghi phân quyền kho." });
        return Json(new
        {
            id = item.Id,
            warehouseId = item.WarehouseId,
            warehouseName = item.Warehouse.Name,
            warehouseCode = item.Warehouse.Code,
            userCode = item.UserCode,
            userName = item.UserName,
            userRole = item.UserRole,
            email = item.Email,
            phone = item.Phone,
            remark = item.Remark,
            isActive = item.IsActive,
            assignedBy = item.AssignedBy,
            assignedAt = item.AssignedAt.ToString("dd/MM/yyyy HH:mm")
        });
    }

    [HttpGet]
    public async Task<IActionResult> ByWarehouse(int warehouseId)
    {
        var list = await svc.GetUserMapsByWarehouseAsync(warehouseId);
        return Json(list.Select(m => new
        {
            m.Id,
            m.UserCode,
            m.UserName,
            m.UserRole,
            m.Email,
            m.Phone,
            m.IsActive,
            m.Remark,
            AssignedAt = m.AssignedAt.ToString("dd/MM/yyyy HH:mm")
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int warehouseId, string userCode, string userName, string? userRole, string? email, string? phone, string? remark, bool isActive = true)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho hàng muốn phân quyền.";
            return RedirectToAction(nameof(Index));
        }
        if (string.IsNullOrWhiteSpace(userCode))
        {
            TempData["Error"] = "Cần mã tài khoản / mã nhân viên.";
            return RedirectToAction(nameof(Index));
        }
        if (string.IsNullOrWhiteSpace(userName))
        {
            TempData["Error"] = "Cần họ và tên nhân sự phụ trách.";
            return RedirectToAction(nameof(Index));
        }

        var item = new UserMapInventory
        {
            WarehouseId = warehouseId,
            UserCode = userCode.Trim().ToLowerInvariant(),
            UserName = userName.Trim(),
            UserRole = string.IsNullOrWhiteSpace(userRole) ? "Thủ kho chính" : userRole.Trim(),
            Email = email?.Trim(),
            Phone = phone?.Trim(),
            Remark = remark?.Trim(),
            IsActive = isActive,
            AssignedBy = "admin",
            AssignedAt = DateTime.Now
        };

        try
        {
            await svc.CreateUserMapInventoryAsync(item);
            TempData["Success"] = $"Đã phân quyền nhân sự '{item.UserName}' ({item.UserCode}) phụ trách kho thành công.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { warehouseId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string userName, string? userRole, string? email, string? phone, string? remark, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            TempData["Error"] = "Cần họ và tên nhân sự.";
            return RedirectToAction(nameof(Index));
        }

        var item = new UserMapInventory
        {
            UserName = userName.Trim(),
            UserRole = string.IsNullOrWhiteSpace(userRole) ? "Thủ kho chính" : userRole.Trim(),
            Email = email?.Trim(),
            Phone = phone?.Trim(),
            Remark = remark?.Trim(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdateUserMapInventoryAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.ToggleUserMapInventoryStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeleteUserMapInventoryAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchMap(int warehouseId, string[]? userCodes, string[]? userNames, string[]? userRoles, string[]? emails, string[]? phones, string? remark, string? assignedBy)
    {
        if (warehouseId <= 0)
        {
            TempData["Error"] = "Vui lòng chọn kho hàng cần gán nhân sự.";
            return RedirectToAction(nameof(Index));
        }

        if (userCodes == null || userCodes.Length == 0)
        {
            TempData["Error"] = "Không có nhân sự nào được chọn để gán vào kho.";
            return RedirectToAction(nameof(Index));
        }

        var users = new List<BatchMapUserItemDto>();
        for (int i = 0; i < userCodes.Length; i++)
        {
            var code = userCodes[i]?.Trim();
            if (string.IsNullOrWhiteSpace(code)) continue;

            var name = (userNames != null && i < userNames.Length) ? userNames[i] : code;
            var role = (userRoles != null && i < userRoles.Length) ? userRoles[i] : "Thủ kho chính";
            var email = (emails != null && i < emails.Length) ? emails[i] : null;
            var phone = (phones != null && i < phones.Length) ? phones[i] : null;

            users.Add(new BatchMapUserItemDto(code, name, role, email, phone, remark));
        }

        var (ok, msg, _) = await svc.BatchMapUsersToWarehouseAsync(warehouseId, users, assignedBy ?? "admin");
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index), new { warehouseId });
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(int? warehouseId, string? userRole, bool? activeOnly, string? q)
    {
        var report = await svc.UserMapInventoriesReportAsync(warehouseId, userRole, activeOnly, q);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH SÁCH PHÂN QUYỀN THỦ KHO & GÁN NGƯỜI DÙNG QUẢN LÝ KHO (MST_USERMAPINVENTORY)");
        sb.AppendLine($"Ngày xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc kho:;{(warehouseId.HasValue ? report.Rows.FirstOrDefault()?.WarehouseName ?? warehouseId.ToString() : "Tất cả kho")}");
        sb.AppendLine($"Bộ lọc vai trò:;{(string.IsNullOrWhiteSpace(userRole) ? "Tất cả vai trò" : userRole)}");
        sb.AppendLine($"Bộ lọc trạng thái:;{(activeOnly == true ? "Đang hiệu lực" : activeOnly == false ? "Tạm dừng" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã nhân viên;Họ và tên nhân sự;Vai trò phụ trách;Mã kho;Tên kho;Loại kho;Cấp kho;Email;Số điện thoại;Trạng thái;Người phân công;Ngày phân công;Ghi chú");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var statusStr = r.IsActive ? "Đang hiệu lực" : "Tạm dừng";
            sb.AppendLine($"{stt++};\"{r.UserCode}\";\"{r.UserName.Replace("\"", "\"\"")}\";\"{r.UserRole}\";\"{r.WarehouseCode}\";\"{r.WarehouseName.Replace("\"", "\"\"")}\";\"{r.InvTypeCode ?? ""}\";\"{r.InvLevelTypeCode ?? ""}\";\"{r.Email ?? ""}\";\"{r.Phone ?? ""}\";\"{statusStr}\";\"{r.AssignedBy}\";{r.AssignedAt:dd/MM/yyyy HH:mm};\"{r.Remark?.Replace("\"", "\"\"")}\"");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG LƯỢT PHÂN CÔNG:;{report.TotalAssignments};;;;;;;;;;");
        sb.AppendLine($";;ĐANG HIỆU LỰC:;{report.ActiveAssignments};;;;;;;;;;");
        sb.AppendLine($";;SỐ NHÂN SỰ VẬN HÀNH KHO:;{report.TotalUsersAssigned};;;;;;;;;;");
        sb.AppendLine($";;KHO CHƯA CÓ NGƯỜI PHỤ TRÁCH:;{report.UnassignedWarehousesCount};;;;;;;;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"PhanQuyenKho_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}

public class ProductGroupController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? parentCode, string? brandCode, bool? activeOnly, string? q)
    {
        var allGroups = await svc.ProductGroupsAsync();
        ViewBag.RootGroups = allGroups.Where(g => string.IsNullOrWhiteSpace(g.ParentCode)).ToList();
        ViewBag.Brands = await svc.BrandsAsync(activeOnly: true);
        ViewBag.ParentCode = parentCode ?? "";
        ViewBag.BrandCode = brandCode ?? "";
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.Keyword = q ?? "";

        var report = await svc.ProductGroupsReportAsync(q, parentCode, brandCode, activeOnly);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetProductGroupDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy nhóm hàng." });

        return Json(new
        {
            id = detail.Group.Id,
            code = detail.Group.Code,
            name = detail.Group.Name,
            description = detail.Group.Description,
            parentCode = detail.Group.ParentCode,
            parentName = detail.ParentGroup?.Name,
            brandCode = detail.Group.BrandCode,
            isActive = detail.Group.IsActive,
            createdAt = detail.Group.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            totalProducts = detail.TotalProducts,
            totalStockQty = detail.TotalStockQty,
            subGroups = detail.SubGroups.Select(s => new { s.Id, s.Code, s.Name, s.IsActive }),
            products = detail.Products.Select(p => new { p.Id, p.Code, p.Name, p.Uom, p.CostPrice })
        });
    }

    [HttpGet]
    public async Task<IActionResult> Products(int id)
    {
        var detail = await svc.GetProductGroupDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy nhóm hàng." });

        return Json(new
        {
            groupCode = detail.Group.Code,
            groupName = detail.Group.Name,
            totalProducts = detail.TotalProducts,
            totalStockQty = detail.TotalStockQty,
            products = detail.Products.Select(p => new { p.Id, p.Code, p.Name, p.Uom, p.CostPrice })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? code, string name, string? description, string? parentCode, string? brandCode, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên nhóm hàng.";
            return RedirectToAction(nameof(Index));
        }

        var item = new ProductGroup
        {
            Code = code?.Trim().ToUpperInvariant() ?? "",
            Name = name.Trim(),
            Description = description?.Trim(),
            ParentCode = string.IsNullOrWhiteSpace(parentCode) ? null : parentCode.Trim().ToUpperInvariant(),
            BrandCode = string.IsNullOrWhiteSpace(brandCode) ? null : brandCode.Trim().ToUpperInvariant(),
            IsActive = isActive
        };

        try
        {
            await svc.CreateProductGroupAsync(item);
            TempData["Success"] = $"Đã tạo mới nhóm hàng '{item.Name}' ({item.Code}) thành công.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? description, string? parentCode, string? brandCode, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên nhóm hàng.";
            return RedirectToAction(nameof(Index));
        }

        var item = new ProductGroup
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            ParentCode = string.IsNullOrWhiteSpace(parentCode) ? null : parentCode.Trim().ToUpperInvariant(),
            BrandCode = string.IsNullOrWhiteSpace(brandCode) ? null : brandCode.Trim().ToUpperInvariant(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdateProductGroupAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.ToggleProductGroupStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeleteProductGroupAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? parentCode, string? brandCode, bool? activeOnly, string? q)
    {
        var report = await svc.ProductGroupsReportAsync(q, parentCode, brandCode, activeOnly);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH MỤC PHÂN NHÓM HÀNG HÓA KHO (MST_PRODUCTGROUP & MST_PRODUCTGROUPSUB)");
        sb.AppendLine($"Ngày xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc nhóm cha:;{(string.IsNullOrWhiteSpace(parentCode) ? "Tất cả" : parentCode)}");
        sb.AppendLine($"Bộ lọc thương hiệu:;{(string.IsNullOrWhiteSpace(brandCode) ? "Tất cả" : brandCode)}");
        sb.AppendLine($"Bộ lọc trạng thái:;{(activeOnly == true ? "Đang áp dụng" : activeOnly == false ? "Tạm dừng" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã nhóm;Tên nhóm hàng;Cấp bậc;Nhóm cha;Thương hiệu;Mô tả đặc tính;Số mặt hàng;Tổng tồn kho thực tế;Trạng thái;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var levelStr = r.Level == 1 ? "Cấp 1 (Gốc)" : "Cấp 2 (Con)";
            var statusStr = r.IsActive ? "Đang áp dụng" : "Tạm dừng";
            sb.AppendLine($"{stt++};\"{r.Code}\";\"{r.Name.Replace("\"", "\"\"")}\";\"{levelStr}\";\"{r.ParentName ?? r.ParentCode ?? ""}\";\"{r.BrandName ?? r.BrandCode ?? ""}\";\"{r.Description?.Replace("\"", "\"\"")}\";{r.ProductCount};{r.TotalStockQty};\"{statusStr}\";{r.CreatedAt:dd/MM/yyyy HH:mm}");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG SỐ NHÓM HÀNG:;{report.TotalGroups};;;;;;;");
        sb.AppendLine($";;NHÓM HÀNG GỐC (ROOT):;{report.RootGroupsCount};;;;;;;");
        sb.AppendLine($";;PHÂN NHÓM CON (SUB):;{report.SubGroupsCount};;;;;;;");
        sb.AppendLine($";;TỔNG MẶT HÀNG PHÂN LOẠI:;{report.TotalProductsAssigned};;;;;;;");
        sb.AppendLine($";;TỔNG TỒN KHO THEO NHÓM:;{report.TotalStockQty};;;;;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"NhomHangHoa_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}

public class AreaController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? parentCode, bool? activeOnly, string? q)
    {
        var allAreas = await svc.AreasAsync();
        ViewBag.RootAreas = allAreas.Where(a => string.IsNullOrWhiteSpace(a.ParentCode)).ToList();
        ViewBag.ParentCode = parentCode ?? "";
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.Keyword = q ?? "";

        var report = await svc.AreasReportAsync(q, parentCode, activeOnly);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetAreaDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy vùng / khu vực." });

        return Json(new
        {
            id = detail.Area.Id,
            code = detail.Area.Code,
            name = detail.Area.Name,
            description = detail.Area.Description,
            parentCode = detail.Area.ParentCode,
            parentName = detail.ParentArea?.Name,
            isActive = detail.Area.IsActive,
            createdAt = detail.Area.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            totalWarehouses = detail.TotalWarehouses,
            totalCustomers = detail.TotalCustomers,
            totalStockQty = detail.TotalStockQty,
            subAreas = detail.SubAreas.Select(s => new { s.Id, s.Code, s.Name, s.IsActive }),
            warehouses = detail.Warehouses.Select(w => new { w.Id, w.Code, w.Name, w.Address }),
            customers = detail.Customers.Select(c => new { c.Id, c.Code, c.Name, c.CustomerType, c.Province })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? code, string name, string? description, string? parentCode, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên vùng / khu vực.";
            return RedirectToAction(nameof(Index));
        }

        var item = new Area
        {
            Code = code?.Trim().ToUpperInvariant() ?? "",
            Name = name.Trim(),
            Description = description?.Trim(),
            ParentCode = string.IsNullOrWhiteSpace(parentCode) ? null : parentCode.Trim().ToUpperInvariant(),
            IsActive = isActive
        };

        try
        {
            await svc.CreateAreaAsync(item);
            TempData["Success"] = $"Đã tạo mới khu vực '{item.Name}' ({item.Code}) thành công.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? description, string? parentCode, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên vùng / khu vực.";
            return RedirectToAction(nameof(Index));
        }

        var item = new Area
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            ParentCode = string.IsNullOrWhiteSpace(parentCode) ? null : parentCode.Trim().ToUpperInvariant(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdateAreaAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.ToggleAreaStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeleteAreaAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? parentCode, bool? activeOnly, string? q)
    {
        var report = await svc.AreasReportAsync(q, parentCode, activeOnly);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH MỤC VÙNG THỊ TRƯỜNG & ĐỊA BÀN KHO (MST_AREA)");
        sb.AppendLine($"Ngày xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc vùng cha:;{(string.IsNullOrWhiteSpace(parentCode) ? "Tất cả" : parentCode)}");
        sb.AppendLine($"Bộ lọc trạng thái:;{(activeOnly == true ? "Đang áp dụng" : activeOnly == false ? "Tạm dừng" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã khu vực;Tên vùng - khu vực;Cấp bậc;Vùng trực thuộc cha;Mô tả phạm vi logistics;Số kho trực thuộc;Số khách hàng - đại lý;Tổng tồn kho thực tế;Trạng thái;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var levelStr = r.Level == 1 ? "Cấp 1 (Gốc)" : "Cấp 2 (Nhánh)";
            var statusStr = r.IsActive ? "Đang áp dụng" : "Tạm dừng";
            sb.AppendLine($"{stt++};\"{r.Code}\";\"{r.Name.Replace("\"", "\"\"")}\";\"{levelStr}\";\"{r.ParentName ?? r.ParentCode ?? ""}\";\"{r.Description?.Replace("\"", "\"\"")}\";{r.WarehouseCount};{r.CustomerCount};{r.TotalStockQty};\"{statusStr}\";{r.CreatedAt:dd/MM/yyyy HH:mm}");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG SỐ KHU VỰC:;{report.TotalAreas};;;;;;;;");
        sb.AppendLine($";;VÙNG GỐC CẤP 1:;{report.RootAreasCount};;;;;;;;");
        sb.AppendLine($";;KHU VỰC NHÁNH CẤP 2:;{report.SubAreasCount};;;;;;;;");
        sb.AppendLine($";;TỔNG KHO PHÂN BỔ:;{report.TotalWarehousesAssigned};;;;;;;;");
        sb.AppendLine($";;TỔNG KHÁCH HÀNG - ĐẠI LÝ:;{report.TotalCustomersAssigned};;;;;;;;");
        sb.AppendLine($";;TỔNG TỒN KHO THỰC TẾ:;{report.TotalStockQty};;;;;;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"VungKhuVucKho_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}

public class CustomerGroupController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(string? parentCode, bool? activeOnly, string? q)
    {
        var allGroups = await svc.CustomerGroupsAsync();
        ViewBag.RootGroups = allGroups.Where(g => string.IsNullOrWhiteSpace(g.ParentCode)).ToList();
        ViewBag.ParentCode = parentCode ?? "";
        ViewBag.ActiveOnly = activeOnly;
        ViewBag.Keyword = q ?? "";

        var report = await svc.CustomerGroupsReportAsync(q, parentCode, activeOnly);
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await svc.GetCustomerGroupDetailAsync(id);
        if (detail == null) return NotFound(new { error = "Không tìm thấy nhóm khách hàng." });

        return Json(new
        {
            id = detail.Group.Id,
            code = detail.Group.Code,
            name = detail.Group.Name,
            description = detail.Group.Description,
            parentCode = detail.Group.ParentCode,
            parentName = detail.ParentGroup?.Name,
            isActive = detail.Group.IsActive,
            createdAt = detail.Group.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            totalCustomers = detail.TotalCustomers,
            totalDispatchedQty = detail.TotalDispatchedQty,
            subGroups = detail.SubGroups.Select(s => new { s.Id, s.Code, s.Name, s.IsActive }),
            customers = detail.Customers.Select(c => new { c.Id, c.Code, c.Name, c.CustomerType, c.Province }),
            recentDispatches = detail.RecentDispatches.Select(d => new
            {
                d.Id,
                d.Code,
                date = d.Date.ToString("dd/MM/yyyy"),
                d.CustomerName,
                totalQty = d.Lines.Sum(l => l.Quantity),
                status = d.Status.ToString()
            })
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string? code, string name, string? description, string? parentCode, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên nhóm khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        var item = new CustomerGroup
        {
            Code = code?.Trim().ToUpperInvariant() ?? "",
            Name = name.Trim(),
            Description = description?.Trim(),
            ParentCode = string.IsNullOrWhiteSpace(parentCode) ? null : parentCode.Trim().ToUpperInvariant(),
            IsActive = isActive
        };

        try
        {
            await svc.CreateCustomerGroupAsync(item);
            TempData["Success"] = $"Đã tạo mới nhóm khách hàng '{item.Name}' ({item.Code}) thành công.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, string? description, string? parentCode, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Cần tên nhóm khách hàng.";
            return RedirectToAction(nameof(Index));
        }

        var item = new CustomerGroup
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            ParentCode = string.IsNullOrWhiteSpace(parentCode) ? null : parentCode.Trim().ToUpperInvariant(),
            IsActive = isActive
        };

        var (ok, msg) = await svc.UpdateCustomerGroupAsync(id, item);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var (ok, msg) = await svc.ToggleCustomerGroupStatusAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (ok, msg) = await svc.DeleteCustomerGroupAsync(id);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? parentCode, bool? activeOnly, string? q)
    {
        var report = await svc.CustomerGroupsReportAsync(q, parentCode, activeOnly);
        var sb = new System.Text.StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("DANH MỤC NHÓM KHÁCH HÀNG & ĐẠI LÝ PHÂN PHỐI (MST_CUSTOMERGROUP)");
        sb.AppendLine($"Ngày xuất báo cáo:;{DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Bộ lọc nhóm cha:;{(string.IsNullOrWhiteSpace(parentCode) ? "Tất cả" : parentCode)}");
        sb.AppendLine($"Bộ lọc trạng thái:;{(activeOnly == true ? "Đang áp dụng" : activeOnly == false ? "Tạm dừng" : "Tất cả")}");
        sb.AppendLine();
        sb.AppendLine("STT;Mã nhóm;Tên nhóm khách hàng;Cấp bậc;Nhóm trực thuộc cha;Mô tả chính sách & công nợ;Số khách hàng - đại lý;Tổng xuất kho phân phối;Trạng thái;Ngày tạo");

        int stt = 1;
        foreach (var r in report.Rows)
        {
            var levelStr = r.Level == 1 ? "Cấp 1 (Gốc)" : "Cấp 2 (Nhánh)";
            var statusStr = r.IsActive ? "Đang áp dụng" : "Tạm dừng";
            sb.AppendLine($"{stt++};\"{r.Code}\";\"{r.Name.Replace("\"", "\"\"")}\";\"{levelStr}\";\"{r.ParentName ?? r.ParentCode ?? ""}\";\"{r.Description?.Replace("\"", "\"\"")}\";{r.CustomerCount};{r.TotalDispatchedQty};\"{statusStr}\";{r.CreatedAt:dd/MM/yyyy HH:mm}");
        }

        sb.AppendLine();
        sb.AppendLine($";;TỔNG SỐ NHÓM KHÁCH HÀNG:;{report.TotalGroups};;;;;;");
        sb.AppendLine($";;NHÓM KÊNH GỐC CẤP 1:;{report.RootGroupsCount};;;;;;");
        sb.AppendLine($";;PHÂN NHÓM CON CẤP 2:;{report.SubGroupsCount};;;;;;");
        sb.AppendLine($";;TỔNG KHÁCH HÀNG ĐƯỢC PHÂN NHÓM:;{report.TotalCustomersAssigned};;;;;;");
        sb.AppendLine($";;TỔNG LƯỢNG HÀNG XUẤT KHO:;{report.TotalDispatchedQty};;;;;;");

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"NhomKhachHang_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }
}












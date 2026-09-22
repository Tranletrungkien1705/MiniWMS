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
    public async Task<IActionResult> Index() => View(await svc.WarehousesAsync());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? code, string? address)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Cần tên kho."; return RedirectToAction(nameof(Index)); }
        await svc.CreateWarehouseAsync(new Warehouse { Name = name.Trim(), Code = code ?? "", Address = address });
        TempData["Success"] = "Đã tạo kho.";
        return RedirectToAction(nameof(Index));
    }
}

public class ProductController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index() => View(await svc.ProductsAsync());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? code, string uom, int minStock, int maxStock = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Cần tên hàng."; return RedirectToAction(nameof(Index)); }
        await svc.CreateProductAsync(new Product { Name = name.Trim(), Code = code ?? "", Uom = string.IsNullOrWhiteSpace(uom) ? "cái" : uom, MinStock = minStock, MaxStock = maxStock });
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
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocType type, int? fromWarehouseId, int? toWarehouseId, string? note, string? refNo,
        int[]? productId, int[]? qty)
    {
        var doc = new StockDoc { Type = type, FromWarehouseId = fromWarehouseId, ToWarehouseId = toWarehouseId, Note = note, RefNo = refNo, CreatedBy = "web" };
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




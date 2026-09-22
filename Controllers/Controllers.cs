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
    public async Task<IActionResult> Create(string name, string? code, string uom, int minStock)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Cần tên hàng."; return RedirectToAction(nameof(Index)); }
        await svc.CreateProductAsync(new Product { Name = name.Trim(), Code = code ?? "", Uom = string.IsNullOrWhiteSpace(uom) ? "cái" : uom, MinStock = minStock });
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


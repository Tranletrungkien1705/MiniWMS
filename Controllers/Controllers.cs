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

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

public class InventoryController(IWmsService svc) : Controller
{
    public async Task<IActionResult> Index(int? warehouseId)
    {
        ViewBag.WarehouseId = warehouseId;
        ViewBag.Warehouses = await svc.WarehousesAsync();
        return View(await svc.BalancesAsync(warehouseId));
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

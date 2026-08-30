using Microsoft.AspNetCore.Mvc;
using MiniWMS.Data;
using MiniWMS.Models;
using MiniWMS.Services;

namespace MiniWMS.Controllers;

/// <summary>
/// API JSON cho SPA React. DTO phẳng (tránh vòng lặp navigation). Dashboard cache Redis 30s theo tenant (X-Cache).
/// Enum trả kèm text tiếng Việt. Tồn kho tính từ phiếu đã ghi sổ (không bảng balance riêng).
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class ApiV1Controller(IWmsService svc, ICache cache, ITenantContext tenant) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var key = $"wms:dash:{tenant.OrgId}";
        var hit = await cache.GetAsync<WmsDash>(key);
        if (hit != null) { Response.Headers["X-Cache"] = "HIT"; return Ok(hit); }
        var d = await svc.DashboardAsync();
        await cache.SetAsync(key, d, TimeSpan.FromSeconds(30));
        Response.Headers["X-Cache"] = "MISS";
        return Ok(d);
    }

    // ── Kho ──
    [HttpGet("warehouses")]
    public async Task<IActionResult> Warehouses()
        => Ok((await svc.WarehousesAsync()).Select(w => new { w.Id, w.Code, w.Name, w.Address, w.Keeper }));

    [HttpPost("warehouses")]
    public async Task<IActionResult> CreateWarehouse([FromBody] WarehouseReq r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) return BadRequest(new { error = "Cần tên kho." });
        var id = await svc.CreateWarehouseAsync(new Warehouse { Name = r.Name.Trim(), Code = r.Code ?? "", Address = r.Address, Keeper = r.Keeper });
        return Ok(new { id });
    }

    // ── Sản phẩm ──
    [HttpGet("products")]
    public async Task<IActionResult> Products()
        => Ok((await svc.ProductsAsync()).Select(ToProductDto));

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] ProductReq r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) return BadRequest(new { error = "Cần tên hàng." });
        var id = await svc.CreateProductAsync(new Product
        {
            Name = r.Name.Trim(), Code = r.Code ?? "", Uom = string.IsNullOrWhiteSpace(r.Uom) ? "cái" : r.Uom!,
            Category = r.Category, Barcode = r.Barcode, CostPrice = r.CostPrice, SalePrice = r.SalePrice,
            MinStock = r.MinStock, MaxStock = r.MaxStock
        });
        return Ok(new { id });
    }

    // ── Phiếu kho ──
    [HttpGet("docs")]
    public async Task<IActionResult> Docs([FromQuery] DocType? type, [FromQuery] DocStatus? status)
        => Ok((await svc.DocsAsync(type, status)).Select(ToDocDto));

    [HttpGet("docs/{id:int}")]
    public async Task<IActionResult> Doc(int id)
    {
        var d = await svc.GetDocAsync(id);
        if (d == null) return NotFound(new { error = "Không tìm thấy phiếu." });
        return Ok(new DocDetailDto(ToDocDto(d), d.Lines.Select(l => new DocLineDto(
            l.ProductId, l.Product?.Code ?? "", l.Product?.Name ?? "", l.Product?.Uom ?? "", l.Quantity, l.UnitPrice, l.LineValue, l.LotNo)).ToList()));
    }

    [HttpPost("docs")]
    public async Task<IActionResult> CreateDoc([FromBody] DocReq r)
    {
        var lines = (r.Lines ?? new()).Where(l => l.ProductId > 0 && l.Quantity != 0)
            .Select(l => (l.ProductId, l.Quantity, l.UnitPrice, l.LotNo)).ToList();
        if (lines.Count == 0) return BadRequest(new { error = "Cần ít nhất 1 dòng hàng." });
        var doc = new StockDoc
        {
            Type = (DocType)r.Type, FromWarehouseId = r.FromWarehouseId, ToWarehouseId = r.ToWarehouseId,
            Note = r.Note, RefNo = r.RefNo, PartnerName = r.PartnerName, CreatedBy = r.CreatedBy ?? "api",
            Date = r.Date == default ? DateTime.Now : r.Date
        };
        var id = await svc.CreateDocAsync(doc, lines);
        return Ok(new { id });
    }

    [HttpPost("docs/{id:int}/post")]
    public async Task<IActionResult> Post(int id)
    {
        var (ok, msg) = await svc.PostDocAsync(id);
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpPost("docs/{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        await svc.CancelDocAsync(id);
        return Ok(new { ok = true });
    }

    // ── Tồn kho ──
    [HttpGet("balances")]
    public async Task<IActionResult> Balances([FromQuery] int? warehouseId)
        => Ok((await svc.BalancesAsync(warehouseId)).Select(b => new
        {
            b.WarehouseId, b.Warehouse, b.ProductId, b.ProductCode, b.ProductName, b.Uom, b.Qty, b.MinStock,
            b.CostPrice, b.Value, low = b.MinStock > 0 && b.Qty <= b.MinStock
        }));

    // ── Mappers ──
    private static object ToProductDto(Product p) => new
    {
        p.Id, p.Code, p.Name, p.Uom, p.Category, p.Barcode, p.CostPrice, p.SalePrice, p.MinStock, p.MaxStock
    };
    private static DocDto ToDocDto(StockDoc d) => new(
        d.Id, d.Code, (int)d.Type, Ui.DocTypeBadge(d.Type).text, Ui.DocTypeBadge(d.Type).css,
        d.FromWarehouseId, d.FromWarehouse?.Name, d.ToWarehouseId, d.ToWarehouse?.Name,
        d.Date, d.RefNo, d.PartnerName, d.Note, d.CreatedBy,
        (int)d.Status, Ui.StatusBadge(d.Status).text, Ui.StatusBadge(d.Status).css,
        d.Lines.Count, d.TotalQty, d.TotalValue, d.CreatedAt, d.TraceCode, d.StampBatchCode, d.StampSampleQr);
}

// ── DTOs ──
public record DocDto(int Id, string Code, int Type, string TypeText, string TypeCss,
    int? FromWarehouseId, string? FromWarehouse, int? ToWarehouseId, string? ToWarehouse,
    DateTime Date, string? RefNo, string? PartnerName, string? Note, string CreatedBy,
    int Status, string StatusText, string StatusCss, int LineCount, int TotalQty, decimal TotalValue, DateTime CreatedAt, string? TraceCode, string? StampBatchCode, string? StampSampleQr);
public record DocLineDto(int ProductId, string ProductCode, string ProductName, string Uom, int Quantity, decimal UnitPrice, decimal LineValue, string? LotNo);
public record DocDetailDto(DocDto Doc, List<DocLineDto> Lines);

// ── Request classes (STJ bind ổn định) ──
public class WarehouseReq { public string Name { get; set; } = ""; public string? Code { get; set; } public string? Address { get; set; } public string? Keeper { get; set; } }
public class ProductReq
{
    public string Name { get; set; } = ""; public string? Code { get; set; } public string? Uom { get; set; }
    public string? Category { get; set; } public string? Barcode { get; set; }
    public decimal CostPrice { get; set; } public decimal SalePrice { get; set; } public int MinStock { get; set; } public int MaxStock { get; set; }
}
public class DocLineReq { public int ProductId { get; set; } public int Quantity { get; set; } public decimal UnitPrice { get; set; } public string? LotNo { get; set; } }
public class DocReq
{
    public int Type { get; set; } public int? FromWarehouseId { get; set; } public int? ToWarehouseId { get; set; }
    public DateTime Date { get; set; } public string? RefNo { get; set; } public string? PartnerName { get; set; }
    public string? Note { get; set; } public string? CreatedBy { get; set; }
    public List<DocLineReq>? Lines { get; set; }
}

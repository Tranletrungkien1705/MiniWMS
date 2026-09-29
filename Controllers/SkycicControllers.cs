using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models;
using MiniWMS.Services;

namespace MiniWMS.Controllers;

/// <summary>
/// Phiếu kiểm kê kho — port 1:1 <c>InvFInvAuditController</c> của 2020.3.Skycic.Inventory
/// (idn.Skycic.Inventory.Website/Controllers/InvFInvAuditController.cs).
/// Ánh xạ action: Index→Index, Create→Create, Save→Create[POST], Audit/ActionIF_InvAudNo→Count,
/// Approve→Approve, Finish→Finish, Cancel→Cancel, Detail→Detail.
/// </summary>
public class InvAuditController(ISkycicService svc, IWmsService wms) : Controller
{
    private string CurUser => User.Identity?.Name ?? "web";

    // nguồn: InvFInvAuditController.cs:22 Index(..., InvCodeAudit, page, recordcount, init)
    public async Task<IActionResult> Index(FormStatus? status)
    {
        ViewBag.Status = status;
        return View(await svc.AuditsAsync(status));
    }

    // nguồn: InvFInvAuditController.cs:1127 Create() — nạp Lst_Mst_Inventory cho dropdown "Kho kiểm kê"
    public async Task<IActionResult> Create()
    {
        ViewBag.Warehouses = await wms.WarehousesAsync();
        return View();
    }

    // nguồn: InvFInvAuditController.cs:639 Save(model, savetype, invBUPattern, lst_ProductCodeUser)
    // savetype == "saveandcreate" ⇒ quay lại Create (Controller:994-1001).
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int warehouseId, string? remark, string? savetype)
    {
        var (ok, msg, id) = await svc.CreateAuditAsync(warehouseId, remark, CurUser);
        TempData[ok ? "Success" : "Error"] = msg;
        if (!ok) return RedirectToAction(nameof(Create));
        return savetype == "saveandcreate" ? RedirectToAction(nameof(Create)) : RedirectToAction(nameof(Detail), new { id });
    }

    // nguồn: InvFInvAuditController.cs:1805 Detail(IF_InvAudNo)
    public async Task<IActionResult> Detail(int id)
    {
        var a = await svc.GetAuditAsync(id);
        if (a == null) return NotFound();
        return View(a);
    }

    // nguồn: InvFInvAuditController.cs:2372 ActionIF_InvAudNo(model) — ghi số lượng thực tế của 1 dòng
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Count(int id, int lineId, decimal qtyActual, string? remark)
    {
        var (ok, msg) = await svc.CountAuditLineAsync(lineId, qtyActual, remark, CurUser);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]   // nguồn: InvFInvAuditController.cs:450 Approve(lstIF_InvAudNo)
    public async Task<IActionResult> Approve(int id, string? remark)
    {
        var (ok, msg) = await svc.ApproveAuditAsync(id, remark, CurUser);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]   // nguồn: InvFInvAuditController.cs:513 Finish(lstIF_InvAudNo)
    public async Task<IActionResult> Finish(int id, string? remark)
    {
        var (ok, msg) = await svc.FinishAuditAsync(id, remark, CurUser);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]   // nguồn: InvFInvAuditController.cs:576 Cancel(lstIF_InvAudNo)
    public async Task<IActionResult> Cancel(int id, string? remark)
    {
        var (ok, msg) = await svc.CancelAuditAsync(id, remark, CurUser);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }
}

/// <summary>
/// Phiếu điều chuyển kho — port 1:1 <c>InvF_MoveOrdController</c> của 2020.3.Skycic.Inventory
/// (idn.Skycic.Inventory.Website/Controllers/InvF_MoveOrdController.cs + Views/InvF_MoveOrd/*).
/// </summary>
public class MoveOrdController(ISkycicService svc, IWmsService wms) : Controller
{
    private string CurUser => User.Identity?.Name ?? "web";

    public async Task<IActionResult> Index(FormStatus? status)
    {
        ViewBag.Status = status;
        return View(await svc.MoveOrdsAsync(status));
    }

    // nguồn: Views/InvF_MoveOrd/Create.cshtml:3-4 nạp List_Mst_InventoryInOut cho 2 dropdown kho
    public async Task<IActionResult> Create()
    {
        ViewBag.Warehouses = await wms.WarehousesAsync();
        ViewBag.Products = await wms.ProductsAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int outWarehouseId, int inWarehouseId, string? moveOrdType, string? auditCode,
        string? remark, int[]? productId, decimal[]? qty, decimal[]? upMove, decimal[]? vat, string[]? lineRemark)
    {
        var head = new MoveOrd
        {
            OutWarehouseId = outWarehouseId, InWarehouseId = inWarehouseId,
            MoveOrdType = moveOrdType, AuditCode = auditCode, Remark = remark
        };
        var lines = new List<MoveOrdLine>();
        for (int i = 0; productId != null && i < productId.Length; i++)
            lines.Add(new MoveOrdLine
            {
                ProductId = productId[i],
                Qty = i < (qty?.Length ?? 0) ? qty![i] : 0,
                UpMove = i < (upMove?.Length ?? 0) ? upMove![i] : 0,
                Vat = i < (vat?.Length ?? 0) ? vat![i] : 0,
                Remark = i < (lineRemark?.Length ?? 0) ? lineRemark![i] : null
            });
        var (ok, msg, id) = await svc.CreateMoveOrdAsync(head, lines, CurUser);
        TempData[ok ? "Success" : "Error"] = msg;
        return ok ? RedirectToAction(nameof(Detail), new { id }) : RedirectToAction(nameof(Create));
    }

    public async Task<IActionResult> Detail(int id)
    {
        var m = await svc.GetMoveOrdAsync(id);
        if (m == null) return NotFound();
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var (ok, msg) = await svc.ApproveMoveOrdAsync(id, CurUser);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var (ok, msg) = await svc.CancelMoveOrdAsync(id, CurUser);
        TempData[ok ? "Success" : "Error"] = msg;
        return RedirectToAction(nameof(Detail), new { id });
    }
}

/// <summary>
/// Báo cáo Thẻ kho — port 1:1 <c>ReportController</c> action Rpt_InvF_WarehouseCard
/// (Views/Report/Rpt_InvF_WarehouseCard.cshtml + BindDataRpt_InvF_WarehouseCard.cshtml).
/// Bộ lọc nguồn: ProductCode + ApprDTimeUTCFrom/To (Biz/Form.cs:138452-138454).
/// </summary>
public class WarehouseCardController(ISkycicService svc, IWmsService wms) : Controller
{
    public async Task<IActionResult> Index(int? productId, DateTime? from, DateTime? to)
    {
        ViewBag.ProductId = productId; ViewBag.From = from; ViewBag.To = to;
        ViewBag.Products = await wms.ProductsAsync();
        return View(await svc.WarehouseCardAsync(productId, from, to));
    }
}

/// <summary>
/// API JSON cho 3 màn vừa port (cùng mẫu ApiV1Controller: DTO phẳng, tên cột giữ theo nguồn
/// để đối chiếu 1:1 được bằng máy).
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class SkycicApiController(ISkycicService svc, AppDbContext db) : ControllerBase
{
    // ── Phiếu kiểm kê (InvF_InvAudit) ──
    [HttpGet("invaudits")]
    public async Task<IActionResult> Audits([FromQuery] FormStatus? status)
        => Ok((await svc.AuditsAsync(status)).Select(a => new
        {
            a.Id, IF_InvAudNo = a.Code, InvCodeAudit = a.Warehouse?.Code, InvName = a.Warehouse?.Name,
            IF_InvAuditStatus = a.Status.ToString().ToUpperInvariant(), a.Remark,
            CreateDTimeUTC = a.CreatedAt, CreateBy = a.CreatedBy,
            ApprDTimeUTC = a.ApprovedAt, ApprBy = a.ApprovedBy,
            FinishDTimeUTC = a.FinishedAt, FinishBy = a.FinishedBy,
            CancelDTimeUTC = a.CancelledAt, CancelBy = a.CancelledBy,
            LineCount = a.Lines.Count, a.TotalQtyInit, a.TotalQtyActual, a.TotalQtyDiff, a.AdjustDocCodes
        }));

    [HttpGet("invaudits/{id:int}")]
    public async Task<IActionResult> Audit(int id)
    {
        var a = await svc.GetAuditAsync(id);
        if (a == null) return NotFound();
        return Ok(new
        {
            a.Id, IF_InvAudNo = a.Code, InvCodeAudit = a.Warehouse?.Code,
            IF_InvAuditStatus = a.Status.ToString().ToUpperInvariant(), a.Remark,
            Lst_InvF_InvAuditDtl = a.Lines.Select(l => new
            {
                l.Id, ProductCode = l.Product.Code, mp_ProductName = l.Product.Name,
                l.LocationInit, l.LocationActual, l.QtyInit, l.QtyActual, QtyRemain = l.QtyDiff,
                l.UpAudit, l.ValAudit, l.Vat, l.ValVat, l.UnitCode,
                InventoryAction = l.InventoryAction.ToString().ToUpperInvariant(),
                FlagExist = l.FlagExist ? "1" : "0", FlagAudit = l.FlagAudit ? "1" : "0",
                IF_InvAuditStatusDtl = l.StatusDtl.ToString().ToUpperInvariant(), l.Remark
            })
        });
    }

    [HttpPost("invaudits")]
    public async Task<IActionResult> CreateAudit([FromBody] CreateAuditReq r)
    {
        var (ok, msg, id) = await svc.CreateAuditAsync(r.WarehouseId, r.Remark, "api");
        return ok ? Ok(new { ok, id, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpPost("invaudits/{id:int}/approve")]
    public async Task<IActionResult> ApproveAudit(int id, [FromBody] RemarkReq? r)
    {
        var (ok, msg) = await svc.ApproveAuditAsync(id, r?.Remark, "api");
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpPost("invaudits/{id:int}/finish")]
    public async Task<IActionResult> FinishAudit(int id, [FromBody] RemarkReq? r)
    {
        var (ok, msg) = await svc.FinishAuditAsync(id, r?.Remark, "api");
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    [HttpPost("invaudits/{id:int}/cancel")]
    public async Task<IActionResult> CancelAudit(int id, [FromBody] RemarkReq? r)
    {
        var (ok, msg) = await svc.CancelAuditAsync(id, r?.Remark, "api");
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ── Phiếu điều chuyển (InvF_MoveOrd) ──
    [HttpGet("moveords")]
    public async Task<IActionResult> MoveOrds([FromQuery] FormStatus? status)
        => Ok((await svc.MoveOrdsAsync(status)).Select(m => new
        {
            m.Id, IF_MONo = m.Code, m.MoveOrdType, InvCodeOut = m.OutWarehouse?.Code, InvCodeIn = m.InWarehouse?.Code,
            IF_InvAudNo = m.AuditCode, IF_MOStatus = m.Status.ToString().ToUpperInvariant(), m.Remark,
            CreateDTimeUTC = m.CreatedAt, CreateBy = m.CreatedBy, ApprDTimeUTC = m.ApprovedAt, ApprBy = m.ApprovedBy,
            CancelDTimeUTC = m.CancelledAt, CancelBy = m.CancelledBy,
            m.TotalQty, m.TotalValAfterVat, m.TransferDocCode
        }));

    [HttpGet("moveords/{id:int}")]
    public async Task<IActionResult> MoveOrd(int id)
    {
        var m = await svc.GetMoveOrdAsync(id);
        if (m == null) return NotFound();
        return Ok(new
        {
            m.Id, IF_MONo = m.Code, InvCodeOut = m.OutWarehouse?.Code, InvCodeIn = m.InWarehouse?.Code,
            IF_MOStatus = m.Status.ToString().ToUpperInvariant(), m.Remark,
            Lst_InvF_MoveOrdDtl = m.Lines.Select(l => new
            {
                l.Id, ProductCode = l.Product.Code, mp_ProductName = l.Product.Name,
                InvCodeOut = l.OutLocation, InvCodeIn = l.InLocation,
                l.Qty, l.UpMove, l.ValMove, l.Vat, l.ValVat, l.ValAfterVat, l.UnitCode,
                IF_MOStatusDtl = l.StatusDtl.ToString().ToUpperInvariant(), l.Remark
            })
        });
    }

    [HttpPost("moveords/{id:int}/approve")]
    public async Task<IActionResult> ApproveMoveOrd(int id)
    {
        var (ok, msg) = await svc.ApproveMoveOrdAsync(id, "api");
        return ok ? Ok(new { ok, msg }) : BadRequest(new { ok, error = msg });
    }

    // ── Thẻ kho (Rpt_InvF_WarehouseCard) ──
    [HttpGet("reports/warehouse-card")]
    public async Task<IActionResult> WarehouseCard([FromQuery] string? productCode, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        int? pid = null;
        if (!string.IsNullOrWhiteSpace(productCode))
            pid = (await db.Products.FirstOrDefaultAsync(p => p.Code == productCode))?.Id;
        return Ok(await svc.WarehouseCardAsync(pid, from, to));
    }

    public record CreateAuditReq(int WarehouseId, string? Remark);
    public record RemarkReq(string? Remark);
}

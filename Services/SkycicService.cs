using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models;

namespace MiniWMS.Services;

/// <summary>
/// Nghiệp vụ port 1:1 từ dự án nguồn <c>2020.3.Skycic.Inventory</c> (Dev/V10):
/// phiếu kiểm kê (InvF_InvAudit), phiếu điều chuyển (InvF_MoveOrd), thẻ kho (Rpt_InvF_WarehouseCard).
/// Mọi guard đều dẫn nguồn <c>file:dòng</c>. Chỗ nào nguồn KHÔNG có guard thì KHÔNG tự thêm
/// (đã khai ở _labs/_audit/AGENT_MiniWMS.md mục 5).
/// </summary>
public interface ISkycicService
{
    Task<List<InvAudit>> AuditsAsync(FormStatus? status);
    Task<InvAudit?> GetAuditAsync(int id);
    Task<(bool ok, string msg, int id)> CreateAuditAsync(int warehouseId, string? remark, string user);
    Task<(bool ok, string msg)> CountAuditLineAsync(int lineId, decimal qtyActual, string? remark, string user);
    Task<(bool ok, string msg)> ApproveAuditAsync(int id, string? remark, string user);
    Task<(bool ok, string msg)> FinishAuditAsync(int id, string? remark, string user);
    Task<(bool ok, string msg)> CancelAuditAsync(int id, string? remark, string user);

    Task<List<MoveOrd>> MoveOrdsAsync(FormStatus? status);
    Task<MoveOrd?> GetMoveOrdAsync(int id);
    Task<(bool ok, string msg, int id)> CreateMoveOrdAsync(MoveOrd head, List<MoveOrdLine> lines, string user);
    Task<(bool ok, string msg)> ApproveMoveOrdAsync(int id, string user);
    Task<(bool ok, string msg)> CancelMoveOrdAsync(int id, string user);

    Task<List<WarehouseCardRow>> WarehouseCardAsync(int? productId, DateTime? from, DateTime? to);
}

public class SkycicService(AppDbContext db, IWmsService wms) : ISkycicService
{
    // ═════════════════════════════ PHIẾU KIỂM KÊ ═════════════════════════════
    // Port 1:1 InvFInvAuditController.cs + Biz/Form.cs (InvF_InvAudit_SaveX / _ApprX / _FinishX / _CancelX)

    public async Task<List<InvAudit>> AuditsAsync(FormStatus? status)
    {
        var q = db.InvAudits.Include(a => a.Warehouse).Include(a => a.Lines).AsQueryable();
        if (status.HasValue) q = q.Where(a => a.Status == status.Value);
        var list = await q.ToListAsync();
        // Nguồn sắp xếp mới nhất trước (InvFInvAuditController.cs:22 Index nhận createdtimefrom/to rồi phân trang).
        return list.OrderByDescending(a => a.CreatedAt).ToList();
    }

    public Task<InvAudit?> GetAuditAsync(int id) =>
        db.InvAudits.Include(a => a.Warehouse).Include(a => a.Lines).ThenInclude(l => l.Product)
          .FirstOrDefaultAsync(a => a.Id == id);

    /// <summary>
    /// Tạo phiếu kiểm kê: dòng hàng hoá KHÔNG do người dùng gõ tay mà sinh từ TỒN KHO hiện có.
    /// Port 1:1 nhánh "không Lot/Serial" của InvFInvAuditController.cs:874-935:
    ///   - lấy Inv_InventoryBalance theo kho, điều kiện <c>QtyTotalOK &gt; 0</c> (dòng 884)
    ///   - QtyInit = QtyActual = Round(QtyTotalOK / ValConvert, 2) (dòng 901, 929-931)
    ///   - gộp theo (InvCodeInit, InvCodeActual, ProductCode); trùng thì CỘNG DỒN (dòng 917-935)
    ///   - InvCodeInit = InvCodeActual = InvCode của dòng tồn (dòng 923-925)
    /// Trạng thái khởi tạo PENDING — Biz/Form.cs:72760.
    /// </summary>
    public async Task<(bool ok, string msg, int id)> CreateAuditAsync(int warehouseId, string? remark, string user)
    {
        // Guard: kho kiểm kê bắt buộc — nguồn Create.cshtml:99 (dấu * đỏ) và
        // Biz/Form.cs:72851-72857 Mst_Inventory_CheckDB đòi kho tồn tại + FlagActive + FlagIn_Out.
        var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId);
        if (wh == null) return (false, "Chưa chọn kho kiểm kê!", 0);

        var bal = await wms.BalancesAsync(warehouseId);
        // Nguồn chỉ lấy dòng tồn > 0 (InvFInvAuditController.cs:884: QtyTotalOK, 0, ">").
        var rows = bal.Where(b => b.Qty > 0).ToList();
        if (rows.Count == 0)
            return (false, $"Kho {wh.Name} không có tồn > 0 nên không tạo được phiếu kiểm kê.", 0);

        var audit = new InvAudit
        {
            Code = $"KK{DateTime.Now:yyMMdd}-{await db.InvAudits.CountAsync() + 1:D3}",  // nguồn: Seq IFInvAudNo, ViewBag.IFInvAudNo (Create.cshtml:5)
            WarehouseId = warehouseId,
            Status = FormStatus.Pending,
            Remark = remark,
            CreatedAt = DateTime.Now, CreatedBy = user,
            LogLuAt = DateTime.Now, LogLuBy = user
        };
        foreach (var r in rows)
        {
            var p = await db.Products.FirstAsync(x => x.Id == r.ProductId);
            audit.Lines.Add(new InvAuditLine
            {
                ProductId = r.ProductId,
                LocationInit = wh.Code, LocationActual = wh.Code,   // nguồn: InvCodeInit = InvCodeActual = it.InvCode (Controller:923-925)
                QtyInit = r.Qty, QtyActual = r.Qty,                 // nguồn: QtyInit = QtyActual = qty (Controller:926-927)
                UnitCode = p.Uom,                                   // nguồn: UnitCode = product.UnitCode (Controller:928)
                UpAudit = p.CostPrice,                              // nguồn: UPAudit "Đơn giá" (InvF_InvAuditDtl.cs:24) — đích lấy giá vốn
                Remark = "",                                        // nguồn: Remark = "" (Controller:929)
                FlagExist = true, FlagAudit = false,                // nguồn: chưa kiểm ⇒ FlagAudit '0' (LstProduct.cshtml:211)
                InventoryAction = AuditAction.In,
                StatusDtl = FormStatus.Pending,
                LogLuAt = DateTime.Now, LogLuBy = user
            });
        }
        foreach (var l in audit.Lines) l.ValAudit = l.UpAudit * l.QtyActual;  // ValAudit "Thành tiền" (InvF_InvAuditDtl.cs:26)
        db.InvAudits.Add(audit);
        await db.SaveChangesAsync();
        return (true, $"Lưu phiếu kiểm kê thành công! ({audit.Code} — {audit.Lines.Count} mặt hàng)", audit.Id);
    }

    /// <summary>
    /// Nhập số lượng thực tế cho 1 dòng ⇒ đánh cờ đã kiểm (FlagAudit = '1').
    /// Guard sửa phiếu: chỉ PENDING hoặc APPROVE — port 1:1 Biz/Form.cs:72797-72812
    /// ("Check.IF_InvAuditStatus.Expected" = "PENDING Or APPROVE").
    /// Chiều tác động kho suy từ dấu lệch: thừa ⇒ IN, thiếu ⇒ OUT (Form.cs:78059 vs :78139).
    /// </summary>
    public async Task<(bool ok, string msg)> CountAuditLineAsync(int lineId, decimal qtyActual, string? remark, string user)
    {
        var line = await db.InvAuditLines.Include(l => l.Audit).FirstOrDefaultAsync(l => l.Id == lineId);
        if (line == null) return (false, "Không tìm thấy dòng kiểm kê.");
        if (line.Audit.Status is not (FormStatus.Pending or FormStatus.Approve))
            return (false, "Phiếu kiểm kê phải ở trạng thái Chờ duyệt hoặc Đã duyệt mới sửa được.");
        if (qtyActual < 0) return (false, "Số lượng thực tế không được âm.");

        line.QtyActual = qtyActual;
        line.ValAudit = line.UpAudit * qtyActual;
        line.Remark = remark;
        line.FlagAudit = true;                                  // nguồn: FlagAudit '1' ⇒ "Đã kiểm" (LstProduct.cshtml:211-213)
        line.InventoryAction = qtyActual >= line.QtyInit ? AuditAction.In : AuditAction.Out;
        line.LogLuAt = DateTime.Now; line.LogLuBy = user;
        line.Audit.UpdatedAt = DateTime.Now; line.Audit.UpdatedBy = user;
        await db.SaveChangesAsync();
        return (true, $"Đã ghi số lượng thực tế (lệch {line.QtyDiff:N2}).");
    }

    /// <summary>Duyệt phiếu: PENDING → APPROVE. Port 1:1 InvFInvAuditController.cs:450-510
    /// (gọi InvF_InvAudit_Appr) + Biz/Form.cs:77068 InvF_InvAudit_ApprX.</summary>
    public async Task<(bool ok, string msg)> ApproveAuditAsync(int id, string? remark, string user)
    {
        var a = await db.InvAudits.FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return (false, "Số phiếu kiểm kê không tồn tại");   // nguồn: Controller:497
        if (a.Status != FormStatus.Pending) return (false, "Phiếu không ở trạng thái Chờ duyệt.");
        a.Status = FormStatus.Approve;
        a.ApprovedAt = DateTime.Now; a.ApprovedBy = user;
        if (!string.IsNullOrWhiteSpace(remark)) a.Remark = remark;          // nguồn: Controller:471 truyền Remark khi duyệt
        a.LogLuAt = DateTime.Now; a.LogLuBy = user;
        await db.SaveChangesAsync();
        return (true, "Duyệt phiếu kiểm kê thành công!");                  // nguồn: Controller:489
    }

    /// <summary>
    /// Chốt phiếu kiểm kê (FINISH) — bước thật sự làm ĐỔI TỒN KHO.
    /// Port 1:1 Biz/Form.cs:77649 InvF_InvAudit_FinishX:
    ///   1. chỉ chạy khi phiếu đang APPROVE (Form.cs:77699-77703 CheckDB với IF_InvAuditStatus.Approve);
    ///   2. KHÔNG được còn dòng nào FlagAudit = '0' (Form.cs:77745-77790, SQL "and t.FlagAudit = '0'");
    ///   3. sinh phiếu NHẬP cho phần thừa (Qty = QtyActual − QtyInit, chiều IN — Form.cs:78059)
    ///      và phiếu XUẤT cho phần thiếu (Qty = QtyInit − QtyActual, chiều OUT — Form.cs:78139);
    ///   4. set FinishDTimeUTC/FinishBy + IF_InvAuditStatus = FINISH và lan trạng thái xuống Dtl
    ///      (Form.cs:77800-77812 zzB_Update_InvF_InvAuditDtl_ClauseSet: IF_InvAuditStatusDtl = f.IF_InvAuditStatus).
    /// </summary>
    public async Task<(bool ok, string msg)> FinishAuditAsync(int id, string? remark, string user)
    {
        var a = await db.InvAudits.Include(x => x.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return (false, "Số phiếu kiểm kê không tồn tại");
        if (a.Status != FormStatus.Approve)
            return (false, "Phiếu kiểm kê phải Đã duyệt (APPROVE) mới chốt được.");
        var chuaKiem = a.Lines.Count(l => !l.FlagAudit);
        if (chuaKiem > 0)
            return (false, $"Còn {chuaKiem} dòng chưa kiểm (FlagAudit = 0) — không chốt được phiếu.");

        var codes = new List<string>();
        // Phần THỪA ⇒ phiếu nhập điều chỉnh vào chính kho kiểm kê.
        var thua = a.Lines.Where(l => l.QtyDiff > 0).ToList();
        if (thua.Count > 0)
        {
            var doc = new StockDoc
            {
                Type = DocType.In, ToWarehouseId = a.WarehouseId,
                RefNo = a.Code, Note = "Nhập Kiểm Kê", CreatedBy = user,
                InvAction = "AUDITIN"      // nguồn: InventoryAction 'AUDITIN' — Form.cs:139478
            };
            var lines = thua.Select(l => ((int)l.ProductId, (int)Math.Round(l.QtyDiff), l.UpAudit, (string?)null)).ToList();
            var docId = await wms.CreateDocAsync(doc, lines);
            var (ok, msg) = await wms.PostDocAsync(docId);
            if (!ok) return (false, "Không ghi sổ được phiếu nhập kiểm kê: " + msg);
            codes.Add((await wms.GetDocAsync(docId))!.Code);
        }
        // Phần THIẾU ⇒ phiếu xuất điều chỉnh khỏi kho kiểm kê.
        var thieu = a.Lines.Where(l => l.QtyDiff < 0).ToList();
        if (thieu.Count > 0)
        {
            var doc = new StockDoc
            {
                Type = DocType.Out, FromWarehouseId = a.WarehouseId,
                RefNo = a.Code, Note = "Xuất Kiểm Kê", CreatedBy = user,
                InvAction = "AUDITOUT"     // nguồn: InventoryAction 'AUDITOUT' — Form.cs:139494
            };
            // Nguồn chiều OUT lấy Qty = QtyInit − QtyActual (Form.cs:78139) ⇒ số dương.
            var lines = thieu.Select(l => ((int)l.ProductId, (int)Math.Round(-l.QtyDiff), l.UpAudit, (string?)null)).ToList();
            var docId = await wms.CreateDocAsync(doc, lines);
            var (ok, msg) = await wms.PostDocAsync(docId);
            if (!ok) return (false, "Không ghi sổ được phiếu xuất kiểm kê: " + msg);
            codes.Add((await wms.GetDocAsync(docId))!.Code);
        }

        a.Status = FormStatus.Finish;
        a.FinishedAt = DateTime.Now; a.FinishedBy = user;
        if (!string.IsNullOrWhiteSpace(remark)) a.Remark = remark;
        a.LogLuAt = DateTime.Now; a.LogLuBy = user;
        a.AdjustDocCodes = codes.Count > 0 ? string.Join(", ", codes) : null;
        foreach (var l in a.Lines) { l.StatusDtl = FormStatus.Finish; l.LogLuAt = DateTime.Now; l.LogLuBy = user; }
        await db.SaveChangesAsync();
        var them = codes.Count > 0 ? $" Phiếu điều chỉnh: {a.AdjustDocCodes}." : " Không có lệch nên không sinh phiếu điều chỉnh.";
        return (true, "Chốt phiếu kiểm kê thành công!" + them);
    }

    /// <summary>Hủy phiếu. Port 1:1 InvFInvAuditController.cs:576-637 (InvF_InvAudit_Cancel)
    /// + Biz/Form.cs:79161 InvF_InvAudit_CancelX. Phiếu đã FINISH thì không hủy nữa.</summary>
    public async Task<(bool ok, string msg)> CancelAuditAsync(int id, string? remark, string user)
    {
        var a = await db.InvAudits.FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return (false, "Số phiếu kiểm kê không tồn tại");
        if (a.Status == FormStatus.Finish) return (false, "Phiếu đã chốt (FINISH) — không hủy được.");
        if (a.Status == FormStatus.Cancel) return (false, "Phiếu đã hủy.");
        a.Status = FormStatus.Cancel;
        a.CancelledAt = DateTime.Now; a.CancelledBy = user;
        if (!string.IsNullOrWhiteSpace(remark)) a.Remark = remark;
        a.LogLuAt = DateTime.Now; a.LogLuBy = user;
        foreach (var l in db.InvAuditLines.Where(l => l.AuditId == id)) l.StatusDtl = FormStatus.Cancel;
        await db.SaveChangesAsync();
        return (true, "Hủy phiếu kiểm kê thành công!");                    // nguồn: Controller:616
    }

    // ═════════════════════════════ PHIẾU ĐIỀU CHUYỂN ═════════════════════════
    // Port 1:1 InvF_MoveOrd/Create.cshtml + Scripts/mine/InvF_MoveOrd.js

    public async Task<List<MoveOrd>> MoveOrdsAsync(FormStatus? status)
    {
        var q = db.MoveOrds.Include(m => m.OutWarehouse).Include(m => m.InWarehouse).Include(m => m.Lines).AsQueryable();
        if (status.HasValue) q = q.Where(m => m.Status == status.Value);
        var list = await q.ToListAsync();
        return list.OrderByDescending(m => m.CreatedAt).ToList();
    }

    public Task<MoveOrd?> GetMoveOrdAsync(int id) =>
        db.MoveOrds.Include(m => m.OutWarehouse).Include(m => m.InWarehouse)
          .Include(m => m.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(m => m.Id == id);

    /// <summary>
    /// Tạo phiếu điều chuyển. Guard port 1:1 Scripts/mine/InvF_MoveOrd.js:30-48:
    ///   - "Chưa chọn kho xuất!"  (js:30)
    ///   - "Chưa chọn kho nhập!"  (js:31)
    ///   - "Không có hàng hoá điều chuyển!" khi lưới trống (js:43-48)
    /// Tiền tệ theo _CommonJs.cshtml:2891-2894: ValMove = Qty*UPMove; ValVAT = ValMove*VAT/100;
    /// ValAfterVAT = ValMove + ValVAT.
    /// </summary>
    public async Task<(bool ok, string msg, int id)> CreateMoveOrdAsync(MoveOrd head, List<MoveOrdLine> lines, string user)
    {
        if (head.OutWarehouseId <= 0) return (false, "Chưa chọn kho xuất!", 0);
        if (head.InWarehouseId <= 0) return (false, "Chưa chọn kho nhập!", 0);
        var real = lines.Where(l => l.ProductId > 0 && l.Qty > 0).ToList();
        if (real.Count == 0) return (false, "Không có hàng hoá điều chuyển!", 0);

        head.Code = $"DC{DateTime.Now:yyMMdd}-{await db.MoveOrds.CountAsync() + 1:D3}";   // nguồn: ViewBag.InvFMoveOrdNo (Create.cshtml:5)
        head.Status = FormStatus.Pending;
        head.CreatedAt = DateTime.Now; head.CreatedBy = user;
        head.LogLuAt = DateTime.Now; head.LogLuBy = user;
        var outWh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == head.OutWarehouseId);
        var inWh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == head.InWarehouseId);
        foreach (var l in real)
        {
            var p = await db.Products.FirstOrDefaultAsync(x => x.Id == l.ProductId);
            l.UnitCode ??= p?.Uom;
            l.OutLocation ??= outWh?.Code;      // nguồn cột "Vị trí xuất" (Create.cshtml:226)
            l.InLocation ??= inWh?.Code;        // nguồn cột "Vị trí nhập" (Create.cshtml:227)
            l.ValMove = l.Qty * l.UpMove;                       // D = A * B (_CommonJs.cshtml:2891)
            l.ValVat = l.ValMove * l.Vat / 100m;                // E = D * (C/100) (_CommonJs.cshtml:2892)
            l.ValAfterVat = l.ValMove + l.ValVat;               // G = D + E (_CommonJs.cshtml:2894)
            l.StatusDtl = FormStatus.Pending;
            l.LogLuAt = DateTime.Now; l.LogLuBy = user;
            head.Lines.Add(l);
        }
        db.MoveOrds.Add(head);
        await db.SaveChangesAsync();
        return (true, $"Lưu phiếu điều chuyển thành công! ({head.Code})", head.Id);
    }

    /// <summary>
    /// Duyệt phiếu điều chuyển ⇒ sinh phiếu Chuyển kho (StockDoc Transfer) và ghi sổ để tồn đổi thật.
    /// Nguồn ghi thẳng Inv_InventoryBalance với InventoryAction 'MOVEORDOUT'/'MOVEORDIN'
    /// (Biz/Form.cs:139478, :139494); đích đi qua StockDoc để dùng lại phép tính tồn có sẵn,
    /// nhờ đó guard "không đủ tồn" của WmsService.PostDocAsync:148-156 được áp dụng.
    /// </summary>
    public async Task<(bool ok, string msg)> ApproveMoveOrdAsync(int id, string user)
    {
        var m = await db.MoveOrds.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return (false, "Số phiếu điều chuyển không tồn tại.");
        if (m.Status != FormStatus.Pending) return (false, "Phiếu không ở trạng thái Chờ duyệt.");

        var doc = new StockDoc
        {
            Type = DocType.Transfer, FromWarehouseId = m.OutWarehouseId, ToWarehouseId = m.InWarehouseId,
            RefNo = m.Code, Note = "Điều chuyển", CreatedBy = user,
            InvAction = "MOVEORD"      // Thẻ kho nở thành MOVEORDOUT / MOVEORDIN (xem WarehouseCardAsync)
        };
        var lines = m.Lines.Select(l => ((int)l.ProductId, (int)Math.Round(l.Qty), l.UpMove, (string?)null)).ToList();
        var docId = await wms.CreateDocAsync(doc, lines);
        var (ok, msg) = await wms.PostDocAsync(docId);
        if (!ok)
        {
            // Không ghi sổ được (thiếu tồn) ⇒ giữ phiếu ở Chờ duyệt, xoá phiếu nháp vừa tạo.
            var draft = await db.Docs.Include(d => d.Lines).FirstOrDefaultAsync(d => d.Id == docId);
            if (draft != null) { db.DocLines.RemoveRange(draft.Lines); db.Docs.Remove(draft); await db.SaveChangesAsync(); }
            return (false, msg);
        }
        m.Status = FormStatus.Approve;
        m.ApprovedAt = DateTime.Now; m.ApprovedBy = user;
        m.LogLuAt = DateTime.Now; m.LogLuBy = user;
        m.TransferDocCode = (await wms.GetDocAsync(docId))!.Code;
        foreach (var l in m.Lines) l.StatusDtl = FormStatus.Approve;
        await db.SaveChangesAsync();
        return (true, $"Duyệt phiếu điều chuyển thành công! Đã ghi sổ {m.TransferDocCode}.");
    }

    public async Task<(bool ok, string msg)> CancelMoveOrdAsync(int id, string user)
    {
        var m = await db.MoveOrds.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return (false, "Số phiếu điều chuyển không tồn tại.");
        if (m.Status == FormStatus.Approve) return (false, "Phiếu đã duyệt và ghi sổ — không hủy được.");
        if (m.Status == FormStatus.Cancel) return (false, "Phiếu đã hủy.");
        m.Status = FormStatus.Cancel;
        m.CancelledAt = DateTime.Now; m.CancelledBy = user;
        m.LogLuAt = DateTime.Now; m.LogLuBy = user;
        foreach (var l in m.Lines) l.StatusDtl = FormStatus.Cancel;
        await db.SaveChangesAsync();
        return (true, "Hủy phiếu điều chuyển thành công!");
    }

    // ═════════════════════════════════ THẺ KHO ═══════════════════════════════
    // Port 1:1 Rpt_InvF_WarehouseCardX_New20200602 (Biz/Form.cs:134971-136083).

    /// <summary>Diễn giải theo InventoryAction. Port 1:1 CASE ở Biz/Form.cs:135518-135530.</summary>
    private static string ActionDesc(string action) => action switch
    {
        "IN" => "Nhập kho",
        "CUSRETURN" => "Nhập KH trả",
        "OUT" => "Xuất kho",
        "MOVEORDIN" => "Điều chuyển",
        "MOVEORDOUT" => "Điều chuyển",
        "MOVEAUDITIN" => "Điều chuyển Kiểm Kê",
        "MOVEAUDITOUT" => "Điều chuyển Kiểm kê",
        "AUDITIN" => "Nhập Kiểm Kê",
        "AUDITOUT" => "Xuất Kiểm Kê",
        "RETURNSUP" => "Xuất trả NCC",
        _ => action
    };

    /// <summary>
    /// Thẻ kho: mỗi dòng = 1 lần hàng hoá vào/ra 1 kho, kèm tồn LUỸ KẾ sau lần đó.
    /// Port 1:1 Biz/Form.cs:134971 Rpt_InvF_WarehouseCardX_New20200602:
    ///   - chỉ tính phiếu đã duyệt, lọc theo ApprDTimeUTC From/To (Form.cs:139462 dùng ApprDTimeUTC);
    ///   - QtyTrans lấy theo InventoryAction, riêng MOVEORDOUT / MOVEAUDITOUT NHÂN (-1)
    ///     (Form.cs:135675-135688);
    ///   - phiếu chuyển kho xuất hiện CẢ 2 chiều: dòng lọc bỏ chiều OUT ở Form.cs:135508
    ///     ĐANG BỊ COMMENT trong bản LIVE (bản có bật lọc là Rpt_InvF_WarehouseCardX — mã chết,
    ///     Form.cs:134220 / :134658);
    ///   - InvFCFInCode01..03 chỉ có giá trị khi action = 'IN', InvFCFOutCode01..03 khi 'OUT',
    ///     cùng đọc từ 1 cột nguồn InvFCFMixCode01..03 (Form.cs:135689-135706 và :135749-135766).
    /// Tồn luỹ kế: đích tính lại bằng cách cộng dồn theo thời gian (nguồn dùng bảng luỹ kế
    /// InvF_WarehouseCard.TotalQtyInv + số đầu kỳ Form.cs:139465-139500).
    /// </summary>
    public async Task<List<WarehouseCardRow>> WarehouseCardAsync(int? productId, DateTime? from, DateTime? to)
    {
        var docs = await db.Docs.Where(d => d.Status == DocStatus.Posted)
            .Include(d => d.FromWarehouse).Include(d => d.ToWarehouse)
            .Include(d => d.Lines).ThenInclude(l => l.Product).ToListAsync();

        // Bước 1: nở mỗi dòng phiếu thành 1..2 "lần chuyển kho" (kho, chiều, số lượng).
        var moves = new List<(DateTime when, StockDoc doc, StockDocLine line, Warehouse wh, string action, decimal qty)>();
        foreach (var d in docs.OrderBy(d => d.Date).ThenBy(d => d.Id))
            foreach (var l in d.Lines)
            {
                if (productId.HasValue && l.ProductId != productId.Value) continue;
                switch (d.Type)
                {
                    case DocType.In when d.ToWarehouse != null:
                        moves.Add((d.Date, d, l, d.ToWarehouse, d.InvAction ?? "IN", l.Quantity));
                        break;
                    case DocType.Out when d.FromWarehouse != null:
                        moves.Add((d.Date, d, l, d.FromWarehouse, d.InvAction ?? "OUT", l.Quantity));
                        break;
                    case DocType.Transfer:
                        // 2 dòng: MOVEORDOUT ở kho xuất (QtyTrans âm) và MOVEORDIN ở kho nhập.
                        if (d.FromWarehouse != null) moves.Add((d.Date, d, l, d.FromWarehouse, "MOVEORDOUT", l.Quantity));
                        if (d.ToWarehouse != null) moves.Add((d.Date, d, l, d.ToWarehouse, "MOVEORDIN", l.Quantity));
                        break;
                }
            }

        // Bước 2: tồn luỹ kế theo (kho, hàng) — cộng dồn theo đúng thứ tự thời gian, tính CẢ
        // phần trước mốc From để số đầu kỳ đúng (nguồn: #tbl_Inv_In/Out_QtyBase_BeginPeriod, Form.cs:139465-139500).
        var running = new Dictionary<(int wh, int pid), decimal>();
        var rows = new List<WarehouseCardRow>();
        foreach (var m in moves.OrderBy(x => x.when).ThenBy(x => x.doc.Id))
        {
            var nhap = m.action is "IN" or "AUDITIN" or "MOVEORDIN" or "MOVEAUDITIN" or "CUSRETURN";  // Form.cs:139478
            var qtyTrans = nhap ? m.qty : -m.qty;                                                     // Form.cs:135679-135688
            var key = (m.wh.Id, m.line.ProductId);
            running.TryGetValue(key, out var cur);
            cur += qtyTrans;
            running[key] = cur;

            if (from.HasValue && m.when.Date < from.Value.Date) continue;
            if (to.HasValue && m.when.Date > to.Value.Date) continue;

            var isIn = m.action == "IN";
            var isOut = m.action == "OUT";
            rows.Add(new WarehouseCardRow(
                DocNo: m.doc.Code,
                DateDoc: m.when.ToString("yyyy-MM-dd"),
                ApprDTime: m.when,
                InventoryAction: m.action,
                InventoryActionDesc: ActionDesc(m.action),
                InvCode: m.wh.Code,
                InvName: m.wh.Name,
                ProductCode: m.line.Product.Code,
                ProductCodeUser: m.line.Product.Code,
                ProductName: m.line.Product.Name,
                ListInvCode: m.line.LotNo ?? m.wh.Code,
                QtyTrans: qtyTrans,
                TotalQtyInv: cur,
                InvFCFInCode01: isIn ? m.doc.CfMixCode01 : null,
                InvFCFInCode02: isIn ? m.doc.CfMixCode02 : null,
                InvFCFInCode03: isIn ? m.doc.CfMixCode03 : null,
                InvFCFOutCode01: isOut ? m.doc.CfMixCode01 : null,
                InvFCFOutCode02: isOut ? m.doc.CfMixCode02 : null,
                InvFCFOutCode03: isOut ? m.doc.CfMixCode03 : null));
        }
        // Nguồn sắp theo AutoId (thứ tự phát sinh); đích sắp mới nhất trước cho dễ đọc rồi giữ nguyên nhóm.
        return rows.OrderByDescending(r => r.ApprDTime).ThenBy(r => r.DocNo).ToList();
    }
}

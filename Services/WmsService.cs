using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models;

namespace MiniWMS.Services;

public record BalanceRow(int WarehouseId, string Warehouse, int ProductId, string ProductCode, string ProductName, string Uom, int Qty, int MinStock);
public record WmsDash(int Warehouses, int Products, int PostedDocs, int DraftDocs, int TotalOnHand, int LowStock, int PendingAudits, int PendingMoveOrders, int PendingReturns, int PendingCustomerReturns, int ExpiringLots = 0, int StagnantItems = 0, int DamagedSerials = 0);

public interface IWmsService
{
    Task<List<Warehouse>> WarehousesAsync();
    Task<List<Product>> ProductsAsync();
    Task<int> CreateWarehouseAsync(Warehouse w);
    Task<int> CreateProductAsync(Product p);
    Task<List<StockDoc>> DocsAsync(DocType? type, DocStatus? status);
    Task<StockDoc?> GetDocAsync(int id);
    Task<int> CreateDocAsync(StockDoc doc, List<(int productId, int qty)> lines);
    Task<(bool ok, string msg)> PostDocAsync(int id);
    Task CancelDocAsync(int id);
    Task<List<StockAudit>> AuditsAsync(int? warehouseId, StockAuditStatus? status);
    Task<StockAudit?> GetAuditAsync(int id);
    Task<int> CreateAuditAsync(StockAudit audit, List<(int productId, int qtyInit, int qtyActual, string? note)> lines);
    Task<(bool ok, string msg)> BalanceAuditAsync(int id);
    Task CancelAuditAsync(int id);
    Task<List<MoveOrder>> MoveOrdersAsync(int? fromWhId, int? toWhId, MoveOrderStatus? status);
    Task<MoveOrder?> GetMoveOrderAsync(int id);
    Task<int> CreateMoveOrderAsync(MoveOrder order, List<(int productId, int qty, string? note)> lines);
    Task<(bool ok, string msg)> ApproveMoveOrderAsync(int id);
    Task<(bool ok, string msg)> ExecuteMoveOrderAsync(int id);
    Task CancelMoveOrderAsync(int id);
    Task<List<ReturnToSupplier>> ReturnToSuppliersAsync(int? warehouseId, ReturnSupStatus? status);
    Task<ReturnToSupplier?> GetReturnToSupplierAsync(int id);
    Task<int> CreateReturnToSupplierAsync(ReturnToSupplier returnDoc, List<(int productId, int qty, decimal unitPrice, string? note)> lines);
    Task<(bool ok, string msg)> ApproveReturnToSupplierAsync(int id);
    Task CancelReturnToSupplierAsync(int id);
    Task<List<CustomerReturn>> CustomerReturnsAsync(int? warehouseId, CusReturnStatus? status);
    Task<CustomerReturn?> GetCustomerReturnAsync(int id);
    Task<int> CreateCustomerReturnAsync(CustomerReturn returnDoc, List<(int productId, int qty, decimal unitPrice, string? note)> lines);
    Task<(bool ok, string msg)> ApproveCustomerReturnAsync(int id);
    Task CancelCustomerReturnAsync(int id);
    Task<List<BalanceRow>> BalancesAsync(int? warehouseId);
    Task<WarehouseCardReport> WarehouseCardAsync(int productId, int? warehouseId, DateTime? fromDate, DateTime? toDate);
    Task<InventoryInOutReport> InventoryInOutReportAsync(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? keyword);
    Task<StockMinimumReport> StockMinimumReportAsync(int? warehouseId, bool onlyBelowMin = true, string? keyword = null);
    Task<StockLotExpiryReport> StockLotExpiryReportAsync(int? warehouseId, LotExpiryStatus? status, string? keyword);
    Task<StorageTimeReport> StorageTimeReportAsync(int? warehouseId, StorageTimeAgingBracket? bracket, string? keyword, DateTime? asOfDate = null);
    Task<List<StockLot>> StockLotsAsync(int? warehouseId, int? productId);
    Task<StockSerialReport> StockSerialReportAsync(int? warehouseId, int? productId, StockSerialStatus? status, string? keyword);
    Task<List<StockSerial>> StockSerialsAsync(int? warehouseId, int? productId, StockSerialStatus? status);
    Task<StockSerial?> GetStockSerialAsync(int id);
    Task<int> CreateStockSerialAsync(StockSerial serial);
    Task<(bool ok, string msg)> ChangeStockSerialStatusAsync(int id, StockSerialStatus newStatus, string? note);
    Task<WmsDash> DashboardAsync();
}

public class WmsService(AppDbContext db) : IWmsService
{
    public Task<List<Warehouse>> WarehousesAsync() => db.Warehouses.OrderBy(w => w.Code).ToListAsync();
    public Task<List<Product>> ProductsAsync() => db.Products.OrderBy(p => p.Code).ToListAsync();

    public async Task<int> CreateWarehouseAsync(Warehouse w)
    {
        if (string.IsNullOrWhiteSpace(w.Code)) w.Code = $"KHO{await db.Warehouses.CountAsync() + 1:D2}";
        db.Warehouses.Add(w); await db.SaveChangesAsync(); return w.Id;
    }
    public async Task<int> CreateProductAsync(Product p)
    {
        if (string.IsNullOrWhiteSpace(p.Code)) p.Code = $"SP{await db.Products.CountAsync() + 1:D4}";
        db.Products.Add(p); await db.SaveChangesAsync(); return p.Id;
    }

    public async Task<List<StockDoc>> DocsAsync(DocType? type, DocStatus? status)
    {
        var q = db.Docs.Include(d => d.FromWarehouse).Include(d => d.ToWarehouse).Include(d => d.Lines).AsQueryable();
        if (type.HasValue) q = q.Where(d => d.Type == type.Value);
        if (status.HasValue) q = q.Where(d => d.Status == status.Value);
        var list = await q.ToListAsync();
        return list.OrderByDescending(d => d.CreatedAt).ToList();
    }

    public Task<StockDoc?> GetDocAsync(int id) =>
        db.Docs.Include(d => d.FromWarehouse).Include(d => d.ToWarehouse).Include(d => d.Lines).ThenInclude(l => l.Product)
          .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<int> CreateDocAsync(StockDoc doc, List<(int productId, int qty)> lines)
    {
        doc.Code = $"{Prefix(doc.Type)}{DateTime.Now:yyMMdd}-{await db.Docs.CountAsync() + 1:D3}";
        doc.Status = DocStatus.Draft;
        foreach (var (pid, qty) in lines.Where(l => l.productId > 0 && l.qty != 0))
            doc.Lines.Add(new StockDocLine { ProductId = pid, Quantity = qty });
        db.Docs.Add(doc);
        await db.SaveChangesAsync();
        return doc.Id;
    }

    public async Task<(bool ok, string msg)> PostDocAsync(int id)
    {
        var doc = await db.Docs.Include(d => d.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return (false, "Không tìm thấy phiếu.");
        if (doc.Status != DocStatus.Draft) return (false, "Phiếu không ở trạng thái Nháp.");
        if (doc.Lines.Count == 0) return (false, "Phiếu chưa có dòng hàng.");

        // Kiểm tra tồn đủ khi Xuất/Chuyển
        if (doc.Type is DocType.Out or DocType.Transfer && doc.FromWarehouseId is { } fromWh)
        {
            var bal = await BalancesAsync(fromWh);
            foreach (var l in doc.Lines)
            {
                var have = bal.FirstOrDefault(x => x.ProductId == l.ProductId)?.Qty ?? 0;
                if (l.Quantity > have) return (false, $"Không đủ tồn: {l.Product.Name} cần {l.Quantity}, còn {have}.");
            }
        }
        doc.Status = DocStatus.Posted;
        await db.SaveChangesAsync();
        return (true, $"Đã ghi sổ phiếu {doc.Code}.");
    }

    public async Task CancelDocAsync(int id)
    {
        var doc = await db.Docs.FirstOrDefaultAsync(d => d.Id == id) ?? throw new KeyNotFoundException();
        doc.Status = DocStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    public async Task<List<StockAudit>> AuditsAsync(int? warehouseId, StockAuditStatus? status)
    {
        var q = db.Audits.Include(a => a.Warehouse).Include(a => a.Lines).ThenInclude(l => l.Product).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(a => a.WarehouseId == warehouseId.Value);
        if (status.HasValue) q = q.Where(a => a.Status == status.Value);
        var list = await q.ToListAsync();
        return list.OrderByDescending(a => a.CreatedAt).ToList();
    }

    public Task<StockAudit?> GetAuditAsync(int id) =>
        db.Audits.Include(a => a.Warehouse)
                 .Include(a => a.InDoc)
                 .Include(a => a.OutDoc)
                 .Include(a => a.Lines).ThenInclude(l => l.Product)
                 .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<int> CreateAuditAsync(StockAudit audit, List<(int productId, int qtyInit, int qtyActual, string? note)> lines)
    {
        audit.Code = $"KK{DateTime.Now:yyMMdd}-{await db.Audits.CountAsync() + 1:D3}";
        audit.Status = StockAuditStatus.Draft;
        foreach (var (pid, qInit, qAct, note) in lines.Where(l => l.productId > 0))
            audit.Lines.Add(new StockAuditLine { ProductId = pid, QtyInit = qInit, QtyActual = qAct, Note = note });
        db.Audits.Add(audit);
        await db.SaveChangesAsync();
        return audit.Id;
    }

    public async Task<(bool ok, string msg)> BalanceAuditAsync(int id)
    {
        var audit = await db.Audits.Include(a => a.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(a => a.Id == id);
        if (audit == null) return (false, "Không tìm thấy phiếu kiểm kê.");
        if (audit.Status != StockAuditStatus.Draft) return (false, "Phiếu kiểm kê không ở trạng thái Đang kiểm kê.");
        if (audit.Lines.Count == 0) return (false, "Phiếu kiểm kê chưa có mặt hàng.");

        var excessLines = audit.Lines.Where(l => l.QtyActual > l.QtyInit).ToList();
        var shortageLines = audit.Lines.Where(l => l.QtyActual < l.QtyInit).ToList();

        if (excessLines.Count == 0 && shortageLines.Count == 0)
        {
            audit.Status = StockAuditStatus.Finished;
            audit.FinishedAt = DateTime.Now;
            await db.SaveChangesAsync();
            return (true, $"Phiếu {audit.Code} khớp tồn sổ sách. Đã đánh dấu hoàn tất kiểm kê.");
        }

        // Tự động sinh phiếu nhập kho nếu có hàng thừa sau kiểm kê
        if (excessLines.Count > 0)
        {
            var inLines = excessLines.Select(l => (l.ProductId, l.QtyActual - l.QtyInit)).ToList();
            var inDoc = new StockDoc
            {
                Type = DocType.In,
                ToWarehouseId = audit.WarehouseId,
                RefNo = audit.Code,
                Note = $"Tự động điều chỉnh thừa sau kiểm kê {audit.Code}",
                CreatedBy = string.IsNullOrWhiteSpace(audit.CreatedBy) ? "audit-balance" : audit.CreatedBy
            };
            var inDocId = await CreateDocAsync(inDoc, inLines);
            var (postOk, postMsg) = await PostDocAsync(inDocId);
            if (!postOk) return (false, $"Lỗi ghi sổ phiếu nhập thừa: {postMsg}");
            audit.InDocId = inDocId;
        }

        // Tự động sinh phiếu xuất kho nếu có hàng thiếu sau kiểm kê
        if (shortageLines.Count > 0)
        {
            var outLines = shortageLines.Select(l => (l.ProductId, l.QtyInit - l.QtyActual)).ToList();
            var outDoc = new StockDoc
            {
                Type = DocType.Out,
                FromWarehouseId = audit.WarehouseId,
                RefNo = audit.Code,
                Note = $"Tự động điều chỉnh thiếu sau kiểm kê {audit.Code}",
                CreatedBy = string.IsNullOrWhiteSpace(audit.CreatedBy) ? "audit-balance" : audit.CreatedBy
            };
            var outDocId = await CreateDocAsync(outDoc, outLines);
            var (postOk, postMsg) = await PostDocAsync(outDocId);
            if (!postOk) return (false, $"Lỗi ghi sổ phiếu xuất thiếu: {postMsg}");
            audit.OutDocId = outDocId;
        }

        audit.Status = StockAuditStatus.Finished;
        audit.FinishedAt = DateTime.Now;
        await db.SaveChangesAsync();
        return (true, $"Đã cân bằng kho cho phiếu {audit.Code}. Tồn kho đã khớp số lượng thực tế kiểm kê.");
    }

    public async Task CancelAuditAsync(int id)
    {
        var audit = await db.Audits.FirstOrDefaultAsync(a => a.Id == id) ?? throw new KeyNotFoundException();
        if (audit.Status == StockAuditStatus.Finished)
            throw new InvalidOperationException("Không thể hủy phiếu kiểm kê đã cân bằng.");
        audit.Status = StockAuditStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    /// <summary>Tồn kho tính từ các phiếu ĐÃ GHI SỔ (In:+To, Out:-From, Transfer:-From+To).</summary>
    public async Task<List<BalanceRow>> BalancesAsync(int? warehouseId)
    {
        var docs = await db.Docs.Where(d => d.Status == DocStatus.Posted).Include(d => d.Lines).ThenInclude(l => l.Product).ToListAsync();
        var whs = await db.Warehouses.ToDictionaryAsync(w => w.Id, w => w);
        var map = new Dictionary<(int wh, int pid), int>();
        void Add(int wh, int pid, int q) { map.TryGetValue((wh, pid), out var cur); map[(wh, pid)] = cur + q; }
        foreach (var d in docs)
            foreach (var l in d.Lines)
            {
                if (d.Type == DocType.In && d.ToWarehouseId is { } to) Add(to, l.ProductId, l.Quantity);
                else if (d.Type == DocType.Out && d.FromWarehouseId is { } fr) Add(fr, l.ProductId, -l.Quantity);
                else if (d.Type == DocType.Transfer)
                {
                    if (d.FromWarehouseId is { } f) Add(f, l.ProductId, -l.Quantity);
                    if (d.ToWarehouseId is { } t) Add(t, l.ProductId, l.Quantity);
                }
            }
        var products = await db.Products.ToDictionaryAsync(p => p.Id, p => p);
        var rows = new List<BalanceRow>();
        foreach (var ((wh, pid), qty) in map)
        {
            if (warehouseId.HasValue && wh != warehouseId.Value) continue;
            if (qty == 0) continue;
            if (!whs.TryGetValue(wh, out var w) || !products.TryGetValue(pid, out var p)) continue;
            rows.Add(new BalanceRow(wh, w.Name, pid, p.Code, p.Name, p.Uom, qty, p.MinStock));
        }
        return rows.OrderBy(r => r.Warehouse).ThenBy(r => r.ProductCode).ToList();
    }

    public async Task<WarehouseCardReport> WarehouseCardAsync(int productId, int? warehouseId, DateTime? fromDate, DateTime? toDate)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new KeyNotFoundException($"Không tìm thấy mặt hàng với ID {productId}.");

        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var docs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted && d.Lines.Any(l => l.ProductId == productId))
            .Include(d => d.FromWarehouse)
            .Include(d => d.ToWarehouse)
            .Include(d => d.Lines)
            .OrderBy(d => d.Date)
            .ThenBy(d => d.Id)
            .ToListAsync();

        // Tách các biến động phát sinh theo kho
        var allTrans = new List<(DateTime Date, string DocCode, int DocId, DocType DocType, string ActionDesc, int WhId, string WhName, string? OffsetWhName, int QtyIn, int QtyOut, string? Note, string? RefNo)>();

        foreach (var d in docs)
        {
            var line = d.Lines.FirstOrDefault(l => l.ProductId == productId);
            if (line == null || line.Quantity == 0) continue;

            bool isAudit = !string.IsNullOrWhiteSpace(d.RefNo) && d.RefNo.StartsWith("KK", StringComparison.OrdinalIgnoreCase)
                           || (!string.IsNullOrWhiteSpace(d.Note) && d.Note.Contains("kiểm kê", StringComparison.OrdinalIgnoreCase));

            if (d.Type == DocType.In)
            {
                if (warehouseId.HasValue && d.ToWarehouseId != warehouseId.Value) continue;
                bool isCusReturn = !string.IsNullOrWhiteSpace(d.RefNo) && d.RefNo.StartsWith("THKH", StringComparison.OrdinalIgnoreCase)
                                   || (!string.IsNullOrWhiteSpace(d.Note) && d.Note.Contains("khách trả", StringComparison.OrdinalIgnoreCase));
                var action = isAudit ? "Kiểm kê - Điều chỉnh thừa (AuditIn)" : (isCusReturn ? "Khách trả lại (CusReturn)" : "Nhập kho (In)");
                allTrans.Add((d.Date, d.Code, d.Id, d.Type, action, d.ToWarehouseId ?? 0, d.ToWarehouse?.Name ?? "", null, line.Quantity, 0, d.Note, d.RefNo));
            }
            else if (d.Type == DocType.Out)
            {
                if (warehouseId.HasValue && d.FromWarehouseId != warehouseId.Value) continue;
                bool isReturnSup = !string.IsNullOrWhiteSpace(d.RefNo) && d.RefNo.StartsWith("THNCC", StringComparison.OrdinalIgnoreCase)
                                   || (!string.IsNullOrWhiteSpace(d.Note) && d.Note.Contains("trả hàng NCC", StringComparison.OrdinalIgnoreCase));
                var action = isAudit ? "Kiểm kê - Điều chỉnh thiếu (AuditOut)" : (isReturnSup ? "Xuất trả NCC (ReturnSup)" : "Xuất kho (Out)");
                allTrans.Add((d.Date, d.Code, d.Id, d.Type, action, d.FromWarehouseId ?? 0, d.FromWarehouse?.Name ?? "", null, 0, line.Quantity, d.Note, d.RefNo));
            }
            else if (d.Type == DocType.Transfer)
            {
                if (warehouseId.HasValue)
                {
                    if (d.FromWarehouseId == warehouseId.Value)
                    {
                        var action = $"Chuyển kho đi (tới {d.ToWarehouse?.Name ?? "kho khác"})";
                        allTrans.Add((d.Date, d.Code, d.Id, d.Type, action, d.FromWarehouseId.Value, d.FromWarehouse?.Name ?? "", d.ToWarehouse?.Name, 0, line.Quantity, d.Note, d.RefNo));
                    }
                    else if (d.ToWarehouseId == warehouseId.Value)
                    {
                        var action = $"Chuyển kho đến (từ {d.FromWarehouse?.Name ?? "kho khác"})";
                        allTrans.Add((d.Date, d.Code, d.Id, d.Type, action, d.ToWarehouseId.Value, d.ToWarehouse?.Name ?? "", d.FromWarehouse?.Name, line.Quantity, 0, d.Note, d.RefNo));
                    }
                }
                else
                {
                    // Trường hợp xem tất cả kho: ghi nhận 2 giao dịch chuyển đi và nhận đến
                    allTrans.Add((d.Date, d.Code, d.Id, d.Type, $"Chuyển đi: {d.FromWarehouse?.Name} -> {d.ToWarehouse?.Name}", d.FromWarehouseId ?? 0, d.FromWarehouse?.Name ?? "", d.ToWarehouse?.Name, 0, line.Quantity, d.Note, d.RefNo));
                    allTrans.Add((d.Date, d.Code, d.Id, d.Type, $"Nhận chuyển: {d.FromWarehouse?.Name} -> {d.ToWarehouse?.Name}", d.ToWarehouseId ?? 0, d.ToWarehouse?.Name ?? "", d.FromWarehouse?.Name, line.Quantity, 0, d.Note, d.RefNo));
                }
            }
        }

        // Tách kỳ báo cáo và tính tồn đầu kỳ
        int openingBalance = 0;
        var startFilterDate = fromDate?.Date;
        var endFilterDate = toDate?.Date.AddDays(1).AddTicks(-1);

        var periodTrans = new List<(DateTime Date, string DocCode, int DocId, DocType DocType, string ActionDesc, int WhId, string WhName, string? OffsetWhName, int QtyIn, int QtyOut, string? Note, string? RefNo)>();

        foreach (var t in allTrans.OrderBy(x => x.Date).ThenBy(x => x.DocId))
        {
            if (startFilterDate.HasValue && t.Date < startFilterDate.Value)
            {
                openingBalance += (t.QtyIn - t.QtyOut);
            }
            else if (!endFilterDate.HasValue || t.Date <= endFilterDate.Value)
            {
                periodTrans.Add(t);
            }
        }

        int running = openingBalance;
        var rows = new List<WarehouseCardRow>();
        foreach (var t in periodTrans)
        {
            running += (t.QtyIn - t.QtyOut);
            rows.Add(new WarehouseCardRow(
                t.Date,
                t.DocCode,
                t.DocId,
                t.DocType,
                t.ActionDesc,
                t.WhId,
                t.WhName,
                t.OffsetWhName,
                t.QtyIn,
                t.QtyOut,
                running,
                t.Note,
                t.RefNo
            ));
        }

        int totalIn = rows.Sum(r => r.QtyIn);
        int totalOut = rows.Sum(r => r.QtyOut);
        int closingBalance = running;

        return new WarehouseCardReport(
            product.Id,
            product.Code,
            product.Name,
            product.Uom,
            warehouseId,
            whName,
            fromDate,
            toDate,
            openingBalance,
            totalIn,
            totalOut,
            closingBalance,
            rows
        );
    }

    public async Task<InventoryInOutReport> InventoryInOutReportAsync(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? keyword)
    {
        var start = (fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var end = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var allWarehouses = await db.Warehouses.ToListAsync();
        var whDict = allWarehouses.ToDictionary(w => w.Id, w => w.Name);

        var prodQuery = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            prodQuery = prodQuery.Where(p => p.Code.ToLower().Contains(kw) || p.Name.ToLower().Contains(kw));
        }
        var products = await prodQuery.ToListAsync();
        var prodDict = products.ToDictionary(p => p.Id, p => p);
        var productIds = prodDict.Keys.ToHashSet();

        // Lấy tất cả các phiếu đã ghi sổ có liên quan đến các sản phẩm cần báo cáo
        var docs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted && d.Date <= end && d.Lines.Any(l => productIds.Contains(l.ProductId)))
            .Include(d => d.Lines)
            .OrderBy(d => d.Date)
            .ThenBy(d => d.Id)
            .ToListAsync();

        // Bảng tổng hợp theo (WarehouseId, ProductId): (OpeningQty, InQty, OutQty)
        var statMap = new Dictionary<(int whId, int prodId), (int opening, int inQty, int outQty)>();

        void AddOpening(int wId, int pId, int delta)
        {
            if (!whDict.ContainsKey(wId) || !productIds.Contains(pId)) return;
            statMap.TryGetValue((wId, pId), out var cur);
            statMap[(wId, pId)] = (cur.opening + delta, cur.inQty, cur.outQty);
        }

        void AddIn(int wId, int pId, int qty)
        {
            if (!whDict.ContainsKey(wId) || !productIds.Contains(pId)) return;
            statMap.TryGetValue((wId, pId), out var cur);
            statMap[(wId, pId)] = (cur.opening, cur.inQty + qty, cur.outQty);
        }

        void AddOut(int wId, int pId, int qty)
        {
            if (!whDict.ContainsKey(wId) || !productIds.Contains(pId)) return;
            statMap.TryGetValue((wId, pId), out var cur);
            statMap[(wId, pId)] = (cur.opening, cur.inQty, cur.outQty + qty);
        }

        foreach (var d in docs)
        {
            bool isBefore = d.Date < start;
            bool isInPeriod = d.Date >= start && d.Date <= end;
            if (!isBefore && !isInPeriod) continue;

            foreach (var line in d.Lines)
            {
                if (!productIds.Contains(line.ProductId) || line.Quantity == 0) continue;

                if (d.Type == DocType.In && d.ToWarehouseId is { } toWh)
                {
                    if (warehouseId.HasValue && toWh != warehouseId.Value) continue;
                    if (isBefore) AddOpening(toWh, line.ProductId, line.Quantity);
                    else if (isInPeriod) AddIn(toWh, line.ProductId, line.Quantity);
                }
                else if (d.Type == DocType.Out && d.FromWarehouseId is { } fromWh)
                {
                    if (warehouseId.HasValue && fromWh != warehouseId.Value) continue;
                    if (isBefore) AddOpening(fromWh, line.ProductId, -line.Quantity);
                    else if (isInPeriod) AddOut(fromWh, line.ProductId, line.Quantity);
                }
                else if (d.Type == DocType.Transfer)
                {
                    if (d.FromWarehouseId is { } fr)
                    {
                        if (!warehouseId.HasValue || fr == warehouseId.Value)
                        {
                            if (isBefore) AddOpening(fr, line.ProductId, -line.Quantity);
                            else if (isInPeriod) AddOut(fr, line.ProductId, line.Quantity);
                        }
                    }
                    if (d.ToWarehouseId is { } to)
                    {
                        if (!warehouseId.HasValue || to == warehouseId.Value)
                        {
                            if (isBefore) AddOpening(to, line.ProductId, line.Quantity);
                            else if (isInPeriod) AddIn(to, line.ProductId, line.Quantity);
                        }
                    }
                }
            }
        }

        var rows = new List<InventoryInOutRow>();
        var whTargetList = warehouseId.HasValue
            ? allWarehouses.Where(w => w.Id == warehouseId.Value).ToList()
            : allWarehouses;

        foreach (var w in whTargetList)
        {
            foreach (var p in products)
            {
                statMap.TryGetValue((w.Id, p.Id), out var stat);
                int closing = stat.opening + stat.inQty - stat.outQty;

                // Nếu có phát sinh hoặc tồn khác 0, hoặc có tìm kiếm theo từ khóa
                if (stat.opening != 0 || stat.inQty != 0 || stat.outQty != 0 || closing != 0 || !string.IsNullOrWhiteSpace(keyword))
                {
                    rows.Add(new InventoryInOutRow(
                        p.Id,
                        p.Code,
                        p.Name,
                        p.Uom,
                        w.Id,
                        w.Name,
                        stat.opening,
                        stat.inQty,
                        stat.outQty,
                        closing
                    ));
                }
            }
        }

        rows = rows.OrderBy(r => r.WarehouseName).ThenBy(r => r.ProductCode).ToList();

        return new InventoryInOutReport(
            warehouseId,
            whName,
            start,
            end.Date,
            keyword,
            rows.Sum(r => r.OpeningQty),
            rows.Sum(r => r.InQty),
            rows.Sum(r => r.OutQty),
            rows.Sum(r => r.ClosingQty),
            rows
        );
    }

    public async Task<List<MoveOrder>> MoveOrdersAsync(int? fromWhId, int? toWhId, MoveOrderStatus? status)
    {
        var q = db.MoveOrders
            .Include(m => m.FromWarehouse)
            .Include(m => m.ToWarehouse)
            .Include(m => m.StockDoc)
            .Include(m => m.Lines).ThenInclude(l => l.Product)
            .AsQueryable();

        if (fromWhId.HasValue) q = q.Where(m => m.FromWarehouseId == fromWhId.Value);
        if (toWhId.HasValue) q = q.Where(m => m.ToWarehouseId == toWhId.Value);
        if (status.HasValue) q = q.Where(m => m.Status == status.Value);

        var list = await q.ToListAsync();
        return list.OrderByDescending(m => m.CreatedAt).ToList();
    }

    public Task<MoveOrder?> GetMoveOrderAsync(int id) =>
        db.MoveOrders
            .Include(m => m.FromWarehouse)
            .Include(m => m.ToWarehouse)
            .Include(m => m.StockDoc)
            .Include(m => m.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<int> CreateMoveOrderAsync(MoveOrder order, List<(int productId, int qty, string? note)> lines)
    {
        if (order.FromWarehouseId == order.ToWarehouseId)
            throw new InvalidOperationException("Kho xuất chuyển và kho nhận chuyển phải khác nhau.");

        order.Code = $"MO{DateTime.Now:yyMMdd}-{await db.MoveOrders.CountAsync() + 1:D3}";
        order.Status = MoveOrderStatus.Pending;
        foreach (var (pid, qty, note) in lines.Where(l => l.productId > 0 && l.qty > 0))
            order.Lines.Add(new MoveOrderLine { ProductId = pid, Quantity = qty, Note = note });

        db.MoveOrders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    public async Task<(bool ok, string msg)> ApproveMoveOrderAsync(int id)
    {
        var order = await db.MoveOrders
            .Include(m => m.FromWarehouse)
            .Include(m => m.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null) return (false, "Không tìm thấy lệnh điều chuyển.");
        if (order.Status != MoveOrderStatus.Pending) return (false, "Lệnh không ở trạng thái Chờ duyệt.");
        if (order.Lines.Count == 0) return (false, "Lệnh chưa có danh sách mặt hàng.");

        // Kiểm tra tồn kho tại kho xuất
        var balances = await BalancesAsync(order.FromWarehouseId);
        var balDict = balances.ToDictionary(b => b.ProductId, b => b.Qty);

        foreach (var line in order.Lines)
        {
            balDict.TryGetValue(line.ProductId, out var available);
            if (line.Quantity > available)
            {
                return (false, $"Kho xuất '{order.FromWarehouse.Name}' không đủ tồn cho '{line.Product.Name}': yêu cầu {line.Quantity}, hiện có {available}.");
            }
        }

        order.Status = MoveOrderStatus.Approved;
        order.ApprovedAt = DateTime.Now;
        await db.SaveChangesAsync();
        return (true, $"Đã phê duyệt lệnh điều chuyển {order.Code}. Sẵn sàng thực hiện chuyển hàng.");
    }

    public async Task<(bool ok, string msg)> ExecuteMoveOrderAsync(int id)
    {
        var order = await db.MoveOrders
            .Include(m => m.FromWarehouse)
            .Include(m => m.ToWarehouse)
            .Include(m => m.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null) return (false, "Không tìm thấy lệnh điều chuyển.");
        if (order.Status == MoveOrderStatus.Finished) return (false, "Lệnh điều chuyển đã được thực hiện trước đó.");
        if (order.Status == MoveOrderStatus.Cancelled) return (false, "Lệnh điều chuyển đã bị hủy.");
        if (order.Lines.Count == 0) return (false, "Lệnh chưa có danh sách mặt hàng.");

        // Tự động sinh phiếu chuyển kho StockDoc (DocType.Transfer)
        var transferDoc = new StockDoc
        {
            Type = DocType.Transfer,
            FromWarehouseId = order.FromWarehouseId,
            ToWarehouseId = order.ToWarehouseId,
            RefNo = order.Code,
            Note = $"Thực hiện theo Lệnh điều chuyển {order.Code}" + (string.IsNullOrWhiteSpace(order.Note) ? "" : $": {order.Note}"),
            CreatedBy = string.IsNullOrWhiteSpace(order.CreatedBy) ? "move-order" : order.CreatedBy
        };

        var docLines = order.Lines.Select(l => (l.ProductId, l.Quantity)).ToList();
        var docId = await CreateDocAsync(transferDoc, docLines);

        // Ghi sổ phiếu chuyển kho để trừ tồn kho xuất và tăng tồn kho nhập
        var (postOk, postMsg) = await PostDocAsync(docId);
        if (!postOk) return (false, $"Lỗi ghi sổ phiếu chuyển kho: {postMsg}");

        order.StockDocId = docId;
        order.Status = MoveOrderStatus.Finished;
        order.FinishedAt = DateTime.Now;
        await db.SaveChangesAsync();

        return (true, $"Đã thực hiện thành công Lệnh điều chuyển {order.Code}. Đã ghi sổ phiếu chuyển kho {transferDoc.Code}.");
    }

    public async Task CancelMoveOrderAsync(int id)
    {
        var order = await db.MoveOrders.FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy lệnh điều chuyển.");
        if (order.Status == MoveOrderStatus.Finished)
            throw new InvalidOperationException("Không thể hủy lệnh điều chuyển đã hoàn thành.");

        order.Status = MoveOrderStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    public async Task<List<ReturnToSupplier>> ReturnToSuppliersAsync(int? warehouseId, ReturnSupStatus? status)
    {
        var q = db.ReturnToSuppliers
            .Include(r => r.Warehouse)
            .Include(r => r.StockDoc)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .AsQueryable();

        if (warehouseId.HasValue) q = q.Where(r => r.WarehouseId == warehouseId.Value);
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);

        var list = await q.ToListAsync();
        return list.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public Task<ReturnToSupplier?> GetReturnToSupplierAsync(int id) =>
        db.ReturnToSuppliers
            .Include(r => r.Warehouse)
            .Include(r => r.StockDoc)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<int> CreateReturnToSupplierAsync(ReturnToSupplier returnDoc, List<(int productId, int qty, decimal unitPrice, string? note)> lines)
    {
        returnDoc.Code = $"THNCC{DateTime.Now:yyMMdd}-{await db.ReturnToSuppliers.CountAsync() + 1:D3}";
        returnDoc.Status = ReturnSupStatus.Draft;
        foreach (var (pid, qty, unitPrice, note) in lines.Where(l => l.productId > 0 && l.qty > 0))
            returnDoc.Lines.Add(new ReturnToSupplierLine { ProductId = pid, Quantity = qty, UnitPrice = unitPrice, Note = note });

        db.ReturnToSuppliers.Add(returnDoc);
        await db.SaveChangesAsync();
        return returnDoc.Id;
    }

    public async Task<(bool ok, string msg)> ApproveReturnToSupplierAsync(int id)
    {
        var returnDoc = await db.ReturnToSuppliers
            .Include(r => r.Warehouse)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (returnDoc == null) return (false, "Không tìm thấy phiếu trả hàng nhà cung cấp.");
        if (returnDoc.Status == ReturnSupStatus.Finished) return (false, "Phiếu trả hàng đã được duyệt và xuất kho trước đó.");
        if (returnDoc.Status == ReturnSupStatus.Cancelled) return (false, "Phiếu trả hàng đã bị hủy.");
        if (returnDoc.Lines.Count == 0) return (false, "Phiếu chưa có mặt hàng xuất trả.");

        // Kiểm tra tồn kho khả dụng tại kho xuất
        var balances = await BalancesAsync(returnDoc.WarehouseId);
        var balDict = balances.ToDictionary(b => b.ProductId, b => b.Qty);

        foreach (var line in returnDoc.Lines)
        {
            balDict.TryGetValue(line.ProductId, out var available);
            if (line.Quantity > available)
            {
                return (false, $"Kho '{returnDoc.Warehouse.Name}' không đủ tồn cho '{line.Product.Name}': yêu cầu trả {line.Quantity}, hiện có {available}.");
            }
        }

        // Tự động sinh phiếu xuất kho StockDoc (DocType.Out)
        var stockDoc = new StockDoc
        {
            Type = DocType.Out,
            FromWarehouseId = returnDoc.WarehouseId,
            RefNo = returnDoc.Code,
            Note = $"Xuất trả hàng NCC {returnDoc.SupplierName} theo phiếu {returnDoc.Code}" + (string.IsNullOrWhiteSpace(returnDoc.Reason) ? "" : $": {returnDoc.Reason}"),
            CreatedBy = string.IsNullOrWhiteSpace(returnDoc.CreatedBy) ? "return-sup" : returnDoc.CreatedBy
        };

        var docLines = returnDoc.Lines.Select(l => (l.ProductId, l.Quantity)).ToList();
        var docId = await CreateDocAsync(stockDoc, docLines);

        // Ghi sổ phiếu xuất để trừ tồn kho ngay
        var (postOk, postMsg) = await PostDocAsync(docId);
        if (!postOk) return (false, $"Lỗi ghi sổ phiếu xuất kho: {postMsg}");

        returnDoc.StockDocId = docId;
        returnDoc.Status = ReturnSupStatus.Finished;
        returnDoc.FinishedAt = DateTime.Now;
        await db.SaveChangesAsync();

        return (true, $"Đã duyệt và thực hiện xuất kho trả hàng NCC {returnDoc.Code}. Đã ghi sổ phiếu xuất kho {stockDoc.Code}.");
    }

    public async Task CancelReturnToSupplierAsync(int id)
    {
        var returnDoc = await db.ReturnToSuppliers.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu trả hàng nhà cung cấp.");
        if (returnDoc.Status == ReturnSupStatus.Finished)
            throw new InvalidOperationException("Không thể hủy phiếu trả hàng đã duyệt xuất kho.");

        returnDoc.Status = ReturnSupStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    public async Task<List<CustomerReturn>> CustomerReturnsAsync(int? warehouseId, CusReturnStatus? status)
    {
        var q = db.CustomerReturns
            .Include(c => c.Warehouse)
            .Include(c => c.StockDoc)
            .Include(c => c.Lines).ThenInclude(l => l.Product)
            .AsQueryable();

        if (warehouseId.HasValue) q = q.Where(c => c.WarehouseId == warehouseId.Value);
        if (status.HasValue) q = q.Where(c => c.Status == status.Value);

        var list = await q.ToListAsync();
        return list.OrderByDescending(c => c.CreatedAt).ToList();
    }

    public Task<CustomerReturn?> GetCustomerReturnAsync(int id) =>
        db.CustomerReturns
            .Include(c => c.Warehouse)
            .Include(c => c.StockDoc)
            .Include(c => c.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<int> CreateCustomerReturnAsync(CustomerReturn returnDoc, List<(int productId, int qty, decimal unitPrice, string? note)> lines)
    {
        returnDoc.Code = $"THKH{DateTime.Now:yyMMdd}-{await db.CustomerReturns.CountAsync() + 1:D3}";
        returnDoc.Status = CusReturnStatus.Draft;
        foreach (var (pid, qty, unitPrice, note) in lines.Where(l => l.productId > 0 && l.qty > 0))
            returnDoc.Lines.Add(new CustomerReturnLine { ProductId = pid, Quantity = qty, UnitPrice = unitPrice, Note = note });

        db.CustomerReturns.Add(returnDoc);
        await db.SaveChangesAsync();
        return returnDoc.Id;
    }

    public async Task<(bool ok, string msg)> ApproveCustomerReturnAsync(int id)
    {
        var returnDoc = await db.CustomerReturns
            .Include(c => c.Warehouse)
            .Include(c => c.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (returnDoc == null) return (false, "Không tìm thấy phiếu khách hàng trả lại.");
        if (returnDoc.Status == CusReturnStatus.Finished) return (false, "Phiếu trả hàng đã được duyệt và nhập kho trước đó.");
        if (returnDoc.Status == CusReturnStatus.Cancelled) return (false, "Phiếu trả hàng đã bị hủy.");
        if (returnDoc.Lines.Count == 0) return (false, "Phiếu chưa có mặt hàng nhận lại.");

        // Tự động sinh phiếu nhập kho StockDoc (DocType.In)
        var stockDoc = new StockDoc
        {
            Type = DocType.In,
            ToWarehouseId = returnDoc.WarehouseId,
            RefNo = returnDoc.Code,
            Note = $"Nhập hàng khách trả lại: {returnDoc.CustomerName} theo phiếu {returnDoc.Code}" + (string.IsNullOrWhiteSpace(returnDoc.Reason) ? "" : $": {returnDoc.Reason}"),
            CreatedBy = string.IsNullOrWhiteSpace(returnDoc.CreatedBy) ? "cus-return" : returnDoc.CreatedBy
        };

        var docLines = returnDoc.Lines.Select(l => (l.ProductId, l.Quantity)).ToList();
        var docId = await CreateDocAsync(stockDoc, docLines);

        // Ghi sổ phiếu nhập để cộng tồn kho ngay
        var (postOk, postMsg) = await PostDocAsync(docId);
        if (!postOk) return (false, $"Lỗi ghi sổ phiếu nhập kho: {postMsg}");

        returnDoc.StockDocId = docId;
        returnDoc.Status = CusReturnStatus.Finished;
        returnDoc.FinishedAt = DateTime.Now;
        await db.SaveChangesAsync();

        return (true, $"Đã nhận và nhập kho thành công hàng khách trả {returnDoc.Code}. Đã ghi sổ phiếu nhập kho {stockDoc.Code}.");
    }

    public async Task CancelCustomerReturnAsync(int id)
    {
        var returnDoc = await db.CustomerReturns.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu khách trả lại.");
        if (returnDoc.Status == CusReturnStatus.Finished)
            throw new InvalidOperationException("Không thể hủy phiếu trả hàng đã duyệt nhập kho.");

        returnDoc.Status = CusReturnStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    /// <summary>Báo cáo Chạm tồn kho tối thiểu & Cảnh báo an toàn kho (port từ Rpt_Inv_InventoryBalance_Minimum Skycic).</summary>
    public async Task<StockMinimumReport> StockMinimumReportAsync(int? warehouseId, bool onlyBelowMin = true, string? keyword = null)
    {
        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var allWarehouses = await db.Warehouses.ToListAsync();
        var targetWarehouses = warehouseId.HasValue
            ? allWarehouses.Where(w => w.Id == warehouseId.Value).ToList()
            : allWarehouses;

        var prodQuery = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            prodQuery = prodQuery.Where(p => p.Code.ToLower().Contains(kw) || p.Name.ToLower().Contains(kw));
        }
        var products = await prodQuery.OrderBy(p => p.Code).ToListAsync();

        // Lấy toàn bộ phiếu kho đã ghi sổ để tính tồn thực tế cho từng (kho, hàng)
        var docs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted)
            .Include(d => d.Lines)
            .ToListAsync();

        var balMap = new Dictionary<(int whId, int prodId), int>();
        void AddBal(int wId, int pId, int qty)
        {
            balMap.TryGetValue((wId, pId), out var cur);
            balMap[(wId, pId)] = cur + qty;
        }

        foreach (var d in docs)
        {
            foreach (var l in d.Lines)
            {
                if (d.Type == DocType.In && d.ToWarehouseId is { } to) AddBal(to, l.ProductId, l.Quantity);
                else if (d.Type == DocType.Out && d.FromWarehouseId is { } fr) AddBal(fr, l.ProductId, -l.Quantity);
                else if (d.Type == DocType.Transfer)
                {
                    if (d.FromWarehouseId is { } frWh) AddBal(frWh, l.ProductId, -l.Quantity);
                    if (d.ToWarehouseId is { } toWh) AddBal(toWh, l.ProductId, l.Quantity);
                }
            }
        }

        var rows = new List<StockMinimumRow>();

        foreach (var w in targetWarehouses)
        {
            foreach (var p in products)
            {
                balMap.TryGetValue((w.Id, p.Id), out var curQty);
                // Nếu sản phẩm không cấu hình định mức tối thiểu và tồn cũng = 0 thì không theo dõi
                if (p.MinStock <= 0 && curQty == 0) continue;

                int shortage = Math.Max(0, p.MinStock - curQty);
                double ratio = p.MinStock > 0 ? Math.Round((double)curQty * 100.0 / p.MinStock, 1) : 100.0;

                StockAlertLevel level;
                string label;
                string badge;

                if (curQty <= 0 && p.MinStock > 0)
                {
                    level = StockAlertLevel.OutOfStock;
                    label = "Hết hàng / Cháy kho";
                    badge = "bg-danger";
                }
                else if (curQty < p.MinStock)
                {
                    level = StockAlertLevel.Danger;
                    label = "Dưới định mức";
                    badge = "bg-warning text-dark";
                }
                else if (curQty <= Math.Ceiling(p.MinStock * 1.25))
                {
                    level = StockAlertLevel.Warning;
                    label = "Cận định mức";
                    badge = "bg-info text-dark";
                }
                else
                {
                    level = StockAlertLevel.Safe;
                    label = "Đạt an toàn";
                    badge = "bg-success";
                }

                if (onlyBelowMin && level == StockAlertLevel.Safe) continue;

                rows.Add(new StockMinimumRow(
                    p.Id,
                    p.Code,
                    p.Name,
                    p.Uom,
                    w.Id,
                    w.Name,
                    p.MinStock,
                    p.MaxStock,
                    curQty,
                    shortage,
                    ratio,
                    level,
                    label,
                    badge
                ));
            }
        }

        // Sắp xếp: OutOfStock lên đầu, sau đó Danger theo shortage giảm dần, rồi Warning, Safe
        rows = rows
            .OrderBy(r => r.AlertLevel)
            .ThenByDescending(r => r.ShortageQty)
            .ThenBy(r => r.WarehouseName)
            .ThenBy(r => r.ProductCode)
            .ToList();

        int totalMonitored = rows.Count;
        int outOfStock = rows.Count(r => r.AlertLevel == StockAlertLevel.OutOfStock);
        int danger = rows.Count(r => r.AlertLevel == StockAlertLevel.Danger);
        int warning = rows.Count(r => r.AlertLevel == StockAlertLevel.Warning);
        int safe = rows.Count(r => r.AlertLevel == StockAlertLevel.Safe);
        int totalShortage = rows.Sum(r => r.ShortageQty);

        return new StockMinimumReport(
            warehouseId,
            whName,
            onlyBelowMin,
            keyword,
            totalMonitored,
            outOfStock,
            danger,
            warning,
            safe,
            totalShortage,
            rows
        );
    }

    public Task<List<StockLot>> StockLotsAsync(int? warehouseId, int? productId)
    {
        var q = db.StockLots.Include(l => l.Warehouse).Include(l => l.Product).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(l => l.WarehouseId == warehouseId.Value);
        if (productId.HasValue) q = q.Where(l => l.ProductId == productId.Value);
        return q.OrderBy(l => l.ExpiredDate).ToListAsync();
    }

    /// <summary>Báo cáo Quản lý Lô & Hạn sử dụng hàng hóa (port từ Inv_InventoryBalanceLot & Rpt_InvBalLot_MaxExpiredDateByInv Skycic).</summary>
    public async Task<StockLotExpiryReport> StockLotExpiryReportAsync(int? warehouseId, LotExpiryStatus? status, string? keyword)
    {
        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var q = db.StockLots.Include(l => l.Warehouse).Include(l => l.Product).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(l => l.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            q = q.Where(l => l.LotNo.ToLower().Contains(kw) ||
                             l.Product.Code.ToLower().Contains(kw) ||
                             l.Product.Name.ToLower().Contains(kw));
        }

        var lots = await q.ToListAsync();
        var today = DateTime.Today;

        var rows = new List<StockLotReportRow>();
        foreach (var l in lots)
        {
            int daysInStock = Math.Max(0, (today - l.InDate.Date).Days);
            int daysToExpiry = (l.ExpiredDate.Date - today).Days;

            LotExpiryStatus st;
            string label;
            string badge;

            if (daysToExpiry < 0)
            {
                st = LotExpiryStatus.Expired;
                label = $"Đã hết hạn ({Math.Abs(daysToExpiry)} ngày trước)";
                badge = "bg-danger";
            }
            else if (daysToExpiry <= 30)
            {
                st = LotExpiryStatus.Critical;
                label = $"Cận hạn nguy cấp (còn {daysToExpiry} ngày)";
                badge = "bg-warning text-dark";
            }
            else if (daysToExpiry <= 90)
            {
                st = LotExpiryStatus.Warning;
                label = $"Cảnh báo cận hạn (còn {daysToExpiry} ngày)";
                badge = "bg-info text-dark";
            }
            else
            {
                st = LotExpiryStatus.Good;
                label = $"Đạt an toàn (còn {daysToExpiry} ngày)";
                badge = "bg-success";
            }

            if (status.HasValue && st != status.Value) continue;

            rows.Add(new StockLotReportRow(
                l.Id,
                l.WarehouseId,
                l.Warehouse.Name,
                l.ProductId,
                l.Product.Code,
                l.Product.Name,
                l.Product.Uom,
                l.LotNo,
                l.ProductionDate,
                l.ExpiredDate,
                l.InDate,
                daysInStock,
                daysToExpiry,
                l.Quantity,
                st,
                label,
                badge
            ));
        }

        // Sắp xếp theo ưu tiên xuất hàng FEFO (First Expired First Out): hạn sớm nhất lên đầu
        rows = rows
            .OrderBy(r => r.Status)
            .ThenBy(r => r.ExpiredDate)
            .ThenBy(r => r.ProductCode)
            .ToList();

        int totalLots = rows.Count;
        int expiredCount = rows.Count(r => r.Status == LotExpiryStatus.Expired);
        int criticalCount = rows.Count(r => r.Status == LotExpiryStatus.Critical);
        int warningCount = rows.Count(r => r.Status == LotExpiryStatus.Warning);
        int goodCount = rows.Count(r => r.Status == LotExpiryStatus.Good);
        int totalQty = rows.Sum(r => r.Quantity);

        return new StockLotExpiryReport(
            warehouseId,
            whName,
            status,
            keyword,
            totalLots,
            expiredCount,
            criticalCount,
            warningCount,
            goodCount,
            totalQty,
            rows
        );
    }

    /// <summary>Báo cáo Tuổi kho & Thời gian lưu kho hàng hoá (port từ Rpt_Inv_InventoryBalance_StorageTime Skycic).</summary>
    public async Task<StorageTimeReport> StorageTimeReportAsync(int? warehouseId, StorageTimeAgingBracket? bracket, string? keyword, DateTime? asOfDate = null)
    {
        var asOf = (asOfDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var allWarehouses = await db.Warehouses.ToListAsync();
        var targetWarehouses = warehouseId.HasValue
            ? allWarehouses.Where(w => w.Id == warehouseId.Value).ToList()
            : allWarehouses;

        var prodQuery = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            prodQuery = prodQuery.Where(p => p.Code.ToLower().Contains(kw) || p.Name.ToLower().Contains(kw));
        }
        var products = await prodQuery.OrderBy(p => p.Code).ToListAsync();

        // Lấy tất cả các phiếu kho đã ghi sổ tính đến ngày chốt báo cáo
        var docs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted && d.Date <= asOf)
            .Include(d => d.Lines)
            .OrderBy(d => d.Date)
            .ThenBy(d => d.Id)
            .ToListAsync();

        var balMap = new Dictionary<(int whId, int prodId), int>();
        var lastInMap = new Dictionary<(int whId, int prodId), DateTime>();

        foreach (var d in docs)
        {
            foreach (var l in d.Lines)
            {
                if (l.Quantity <= 0) continue;

                if (d.Type == DocType.In && d.ToWarehouseId is { } to)
                {
                    balMap.TryGetValue((to, l.ProductId), out var cur);
                    balMap[(to, l.ProductId)] = cur + l.Quantity;

                    if (!lastInMap.TryGetValue((to, l.ProductId), out var prevDate) || d.Date > prevDate)
                        lastInMap[(to, l.ProductId)] = d.Date;
                }
                else if (d.Type == DocType.Out && d.FromWarehouseId is { } fr)
                {
                    balMap.TryGetValue((fr, l.ProductId), out var cur);
                    balMap[(fr, l.ProductId)] = cur - l.Quantity;
                }
                else if (d.Type == DocType.Transfer)
                {
                    if (d.FromWarehouseId is { } frWh)
                    {
                        balMap.TryGetValue((frWh, l.ProductId), out var cur);
                        balMap[(frWh, l.ProductId)] = cur - l.Quantity;
                    }
                    if (d.ToWarehouseId is { } toWh)
                    {
                        balMap.TryGetValue((toWh, l.ProductId), out var cur);
                        balMap[(toWh, l.ProductId)] = cur + l.Quantity;

                        if (!lastInMap.TryGetValue((toWh, l.ProductId), out var prevDate) || d.Date > prevDate)
                            lastInMap[(toWh, l.ProductId)] = d.Date;
                    }
                }
            }
        }

        var rows = new List<StorageTimeRow>();
        var today = DateTime.Today;

        foreach (var w in targetWarehouses)
        {
            foreach (var p in products)
            {
                balMap.TryGetValue((w.Id, p.Id), out var curQty);
                if (curQty <= 0) continue; // Chỉ đưa vào báo cáo các mặt hàng đang có tồn thực tế > 0

                lastInMap.TryGetValue((w.Id, p.Id), out var lastInDate);
                DateTime? validLastIn = lastInDate != default ? lastInDate : null;

                // Tuổi kho tính theo số ngày kể từ ngày nhập kho gần nhất
                int storageDays = validLastIn.HasValue ? Math.Max(0, (today - validLastIn.Value.Date).Days) : 0;

                StorageTimeAgingBracket b;
                string bLabel;
                string badge;
                string rec;

                if (storageDays <= 30)
                {
                    b = StorageTimeAgingBracket.Tier1_Under30;
                    bLabel = "≤ 30 ngày (Mới nhập)";
                    badge = "bg-success";
                    rec = "Lưu kho an toàn, luân chuyển tốt";
                }
                else if (storageDays <= 60)
                {
                    b = StorageTimeAgingBracket.Tier2_31To60;
                    bLabel = "31 - 60 ngày (Bình thường)";
                    badge = "bg-info text-dark";
                    rec = "Duy trì kế hoạch bán hàng thường lệ";
                }
                else if (storageDays <= 90)
                {
                    b = StorageTimeAgingBracket.Tier3_61To90;
                    bLabel = "61 - 90 ngày (Chậm tiêu thụ)";
                    badge = "bg-warning text-dark";
                    rec = "Theo dõi sức mua, tăng cường kích cầu";
                }
                else
                {
                    b = StorageTimeAgingBracket.Tier4_Over90;
                    bLabel = "> 90 ngày (Tồn đọng vốn)";
                    badge = "bg-danger";
                    rec = "Ưu tiên xả hàng, khuyến mãi hoặc luân chuyển chi nhánh";
                }

                if (bracket.HasValue && b != bracket.Value) continue;

                decimal cost = p.CostPrice > 0 ? p.CostPrice : 100000m;
                decimal totalVal = curQty * cost;

                rows.Add(new StorageTimeRow(
                    p.Id,
                    p.Code,
                    p.Name,
                    p.Uom,
                    w.Id,
                    w.Name,
                    curQty,
                    cost,
                    totalVal,
                    validLastIn,
                    storageDays,
                    b,
                    bLabel,
                    badge,
                    rec
                ));
            }
        }

        // Sắp xếp: Ưu tiên tuổi kho lâu ngày nhất lên đầu, tiếp theo là giá trị tồn giảm dần
        rows = rows
            .OrderByDescending(r => r.StorageDays)
            .ThenByDescending(r => r.TotalValue)
            .ThenBy(r => r.WarehouseName)
            .ThenBy(r => r.ProductCode)
            .ToList();

        int totalItems = rows.Count;
        int totalQtySum = rows.Sum(r => r.CurrentQty);
        decimal totalInvVal = rows.Sum(r => r.TotalValue);
        int stagnantCount = rows.Count(r => r.Bracket == StorageTimeAgingBracket.Tier4_Over90);
        decimal stagnantVal = rows.Where(r => r.Bracket == StorageTimeAgingBracket.Tier4_Over90).Sum(r => r.TotalValue);
        double avgDays = rows.Count > 0 ? Math.Round(rows.Average(r => r.StorageDays), 1) : 0.0;

        int under30 = rows.Count(r => r.Bracket == StorageTimeAgingBracket.Tier1_Under30);
        int from31To60 = rows.Count(r => r.Bracket == StorageTimeAgingBracket.Tier2_31To60);
        int from61To90 = rows.Count(r => r.Bracket == StorageTimeAgingBracket.Tier3_61To90);
        int over90 = rows.Count(r => r.Bracket == StorageTimeAgingBracket.Tier4_Over90);

        return new StorageTimeReport(
            warehouseId,
            whName,
            asOf.Date,
            bracket,
            keyword,
            totalItems,
            totalQtySum,
            totalInvVal,
            stagnantCount,
            stagnantVal,
            avgDays,
            under30,
            from31To60,
            from61To90,
            over90,
            rows
        );
    }

    public async Task<WmsDash> DashboardAsync()
    {
        var balances = await BalancesAsync(null);
        var today = DateTime.Today;
        var expiringLots = await db.StockLots.CountAsync(l => l.ExpiredDate <= today.AddDays(30));

        // Tính số mặt hàng đọng vốn > 90 ngày
        var storageReport = await StorageTimeReportAsync(null, StorageTimeAgingBracket.Tier4_Over90, null);
        var stagnantItems = storageReport.StagnantItemsCount;
        var damagedSerials = await db.StockSerials.CountAsync(s => s.Status == StockSerialStatus.DamagedNG);

        return new WmsDash(
            await db.Warehouses.CountAsync(),
            await db.Products.CountAsync(),
            await db.Docs.CountAsync(d => d.Status == DocStatus.Posted),
            await db.Docs.CountAsync(d => d.Status == DocStatus.Draft),
            balances.Sum(b => b.Qty),
            balances.Count(b => b.MinStock > 0 && b.Qty <= b.MinStock),
            await db.Audits.CountAsync(a => a.Status == StockAuditStatus.Draft),
            await db.MoveOrders.CountAsync(m => m.Status == MoveOrderStatus.Pending || m.Status == MoveOrderStatus.Approved),
            await db.ReturnToSuppliers.CountAsync(r => r.Status == ReturnSupStatus.Draft),
            await db.CustomerReturns.CountAsync(c => c.Status == CusReturnStatus.Draft),
            expiringLots,
            stagnantItems,
            damagedSerials);
    }

    public Task<List<StockSerial>> StockSerialsAsync(int? warehouseId, int? productId, StockSerialStatus? status)
    {
        var q = db.StockSerials.Include(s => s.Warehouse).Include(s => s.Product).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(s => s.WarehouseId == warehouseId.Value);
        if (productId.HasValue) q = q.Where(s => s.ProductId == productId.Value);
        if (status.HasValue) q = q.Where(s => s.Status == status.Value);
        return q.OrderByDescending(s => s.CreatedAt).ToListAsync();
    }

    public Task<StockSerial?> GetStockSerialAsync(int id) =>
        db.StockSerials.Include(s => s.Warehouse).Include(s => s.Product).FirstOrDefaultAsync(s => s.Id == id);

    public async Task<int> CreateStockSerialAsync(StockSerial serial)
    {
        if (string.IsNullOrWhiteSpace(serial.SerialNo))
            throw new ArgumentException("Số Serial/IMEI không được để trống.");

        serial.SerialNo = serial.SerialNo.Trim().ToUpperInvariant();
        var exists = await db.StockSerials.AnyAsync(s => s.WarehouseId == serial.WarehouseId &&
                                                         s.ProductId == serial.ProductId &&
                                                         s.SerialNo == serial.SerialNo);
        if (exists)
            throw new InvalidOperationException($"Số Serial/IMEI '{serial.SerialNo}' đã tồn tại trong kho cho mặt hàng này.");

        serial.CreatedAt = DateTime.Now;
        db.StockSerials.Add(serial);
        await db.SaveChangesAsync();
        return serial.Id;
    }

    public async Task<(bool ok, string msg)> ChangeStockSerialStatusAsync(int id, StockSerialStatus newStatus, string? note)
    {
        var serial = await db.StockSerials.FirstOrDefaultAsync(s => s.Id == id);
        if (serial == null) return (false, "Không tìm thấy Serial/IMEI.");

        var oldStatus = serial.Status;
        serial.Status = newStatus;
        serial.UpdatedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(note))
        {
            serial.Note = string.IsNullOrWhiteSpace(serial.Note) ? note.Trim() : $"{serial.Note}; {note.Trim()}";
        }

        if (newStatus == StockSerialStatus.Exported && oldStatus != StockSerialStatus.Exported)
        {
            serial.OutDate = DateTime.Now;
        }
        else if (newStatus != StockSerialStatus.Exported && oldStatus == StockSerialStatus.Exported)
        {
            serial.OutDate = null;
        }

        await db.SaveChangesAsync();
        var statusLabel = newStatus switch
        {
            StockSerialStatus.Available => "Khả dụng / Sẵn sàng",
            StockSerialStatus.Locked => "Tạm khóa / Giữ hàng",
            StockSerialStatus.DamagedNG => "Báo hỏng / Thẩm định NG",
            StockSerialStatus.Exported => "Đã xuất kho",
            _ => newStatus.ToString()
        };
        return (true, $"Đã cập nhật trạng thái Serial '{serial.SerialNo}' thành: {statusLabel}.");
    }

    /// <summary>Báo cáo Quản lý & Tra cứu Serial / IMEI hàng tồn kho (port từ Inv_InventoryBalanceSerial Skycic).</summary>
    public async Task<StockSerialReport> StockSerialReportAsync(int? warehouseId, int? productId, StockSerialStatus? status, string? keyword)
    {
        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        string prodName = "Tất cả mặt hàng";
        if (productId.HasValue)
        {
            var p = await db.Products.FirstOrDefaultAsync(x => x.Id == productId.Value);
            if (p != null) prodName = $"{p.Code} - {p.Name}";
        }

        var q = db.StockSerials.Include(s => s.Warehouse).Include(s => s.Product).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(s => s.WarehouseId == warehouseId.Value);
        if (productId.HasValue) q = q.Where(s => s.ProductId == productId.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            q = q.Where(s => s.SerialNo.ToLower().Contains(kw) ||
                             (s.LotNo != null && s.LotNo.ToLower().Contains(kw)) ||
                             (s.RefNo != null && s.RefNo.ToLower().Contains(kw)) ||
                             s.Product.Code.ToLower().Contains(kw) ||
                             s.Product.Name.ToLower().Contains(kw));
        }

        var allMatchingSerials = await q.ToListAsync();

        int totalSerials = allMatchingSerials.Count;
        int availableCount = allMatchingSerials.Count(s => s.Status == StockSerialStatus.Available);
        int lockedCount = allMatchingSerials.Count(s => s.Status == StockSerialStatus.Locked);
        int damagedNGCount = allMatchingSerials.Count(s => s.Status == StockSerialStatus.DamagedNG);
        int exportedCount = allMatchingSerials.Count(s => s.Status == StockSerialStatus.Exported);

        var filtered = status.HasValue
            ? allMatchingSerials.Where(s => s.Status == status.Value).ToList()
            : allMatchingSerials;

        var rows = filtered
            .OrderBy(s => s.Status)
            .ThenByDescending(s => s.InDate)
            .ThenBy(s => s.SerialNo)
            .Select(s =>
            {
                var (label, badge) = s.Status switch
                {
                    StockSerialStatus.Available => ("Khả dụng", "bg-success"),
                    StockSerialStatus.Locked => ("Tạm khóa", "bg-warning text-dark"),
                    StockSerialStatus.DamagedNG => ("Lỗi / NG", "bg-danger"),
                    StockSerialStatus.Exported => ("Đã xuất", "bg-secondary"),
                    _ => (s.Status.ToString(), "bg-secondary")
                };

                return new StockSerialRow(
                    s.Id,
                    s.WarehouseId,
                    s.Warehouse.Name,
                    s.ProductId,
                    s.Product.Code,
                    s.Product.Name,
                    s.Product.Uom,
                    s.SerialNo,
                    s.LotNo,
                    s.InDate,
                    s.OutDate,
                    s.RefNo,
                    s.Status,
                    label,
                    badge,
                    s.Note
                );
            })
            .ToList();

        return new StockSerialReport(
            warehouseId,
            whName,
            productId,
            prodName,
            status,
            keyword,
            totalSerials,
            availableCount,
            lockedCount,
            damagedNGCount,
            exportedCount,
            rows
        );
    }

    private static string Prefix(DocType t) => t switch { DocType.In => "PN", DocType.Out => "PX", DocType.Transfer => "PC", _ => "PK" };
}

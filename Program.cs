using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models;
using MiniWMS.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");

var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=miniwms.db";
builder.Services.AddDbContext<AppDbContext>(o =>
{
    if (DbUtil.IsPostgres(conn)) o.UseNpgsql(DbUtil.ToNpgsql(conn));
    else o.UseSqlite(conn);
});
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IWmsService, WmsService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await Seeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

app.Use(async (ctx, next) =>
{
    var key = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(key)) ctx.Request.Cookies.TryGetValue(TenantContext.CookieName, out key);
    if (!string.IsNullOrWhiteSpace(key))
    {
        using var lookup = app.Services.CreateScope();
        var ldb = lookup.ServiceProvider.GetRequiredService<AppDbContext>();
        var org = await ldb.Orgs.FirstOrDefaultAsync(o => o.ApiKey == key);
        if (org != null) ctx.RequestServices.GetRequiredService<ITenantContext>().OrgId = org.Id;
    }
    await next();
});

app.UseStaticFiles();
app.MapGet("/healthz", () => "ok");

// API tồn kho (MiniDMS kiểm tra tồn trước khi bán)
app.MapGet("/api/balance", async (int? warehouseId, IWmsService svc) =>
    Results.Ok((await svc.BalancesAsync(warehouseId)).Select(b => new { b.Warehouse, b.ProductCode, b.ProductName, b.Uom, b.Qty })));

// API kiểm kê kho
app.MapGet("/api/audits", async (int? warehouseId, StockAuditStatus? status, IWmsService svc) =>
    Results.Ok((await svc.AuditsAsync(warehouseId, status)).Select(a => new
    {
        a.Id,
        a.Code,
        Warehouse = a.Warehouse.Name,
        a.WarehouseId,
        a.Date,
        Status = a.Status.ToString(),
        TotalInit = a.TotalInitQty,
        TotalActual = a.TotalActualQty,
        TotalDiff = a.TotalDiffQty,
        Lines = a.Lines.Select(l => new { l.Product.Code, l.Product.Name, l.QtyInit, l.QtyActual, l.DiffQty })
    })));

// API Thẻ kho (Warehouse Card - port từ Rpt_InvF_WarehouseCard Skycic)
app.MapGet("/api/warehouse-card", async (int productId, int? warehouseId, DateTime? fromDate, DateTime? toDate, IWmsService svc) =>
{
    if (productId <= 0) return Results.BadRequest(new { error = "Cần productId." });
    try
    {
        var card = await svc.WarehouseCardAsync(productId, warehouseId, fromDate, toDate);
        return Results.Ok(card);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

// API Báo cáo Nhập Xuất Tồn (Inventory In-Out-Balance - port từ Rpt_Inventory_In_Out_Inv Skycic)
app.MapGet("/api/reports/in-out-inventory", async (int? warehouseId, DateTime? fromDate, DateTime? toDate, string? q, IWmsService svc) =>
{
    var defFrom = fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    var defTo = toDate ?? DateTime.Today;
    var report = await svc.InventoryInOutReportAsync(warehouseId, defFrom, defTo, q);
    return Results.Ok(report);
});

// API Báo cáo Chạm tồn kho tối thiểu & Cảnh báo an toàn kho (Stock Minimum Alert - port từ Rpt_Inv_InventoryBalance_Minimum Skycic)
app.MapGet("/api/reports/min-stock-alert", async (int? warehouseId, bool? onlyBelowMin, string? q, IWmsService svc) =>
{
    var report = await svc.StockMinimumReportAsync(warehouseId, onlyBelowMin ?? true, q);
    return Results.Ok(report);
});

// API Báo cáo Quản lý Lô & Hạn sử dụng hàng hóa (Lot & Expiry Date - port từ Inv_InventoryBalanceLot & Rpt_InvBalLot_MaxExpiredDateByInv Skycic)
app.MapGet("/api/reports/lot-expiry", async (int? warehouseId, LotExpiryStatus? status, string? q, IWmsService svc) =>
{
    var report = await svc.StockLotExpiryReportAsync(warehouseId, status, q);
    return Results.Ok(report);
});

// API Báo cáo Tuổi kho & Thời gian lưu kho hàng hoá (Storage Time / Inventory Aging - port từ Rpt_Inv_InventoryBalance_StorageTime Skycic)
app.MapGet("/api/reports/storage-time", async (int? warehouseId, StorageTimeAgingBracket? bracket, string? q, DateTime? asOfDate, IWmsService svc) =>
{
    var report = await svc.StorageTimeReportAsync(warehouseId, bracket, q, asOfDate);
    return Results.Ok(report);
});

// API Tra cứu tồn theo Lô hàng (Stock Lots)
app.MapGet("/api/stock-lots", async (int? warehouseId, int? productId, IWmsService svc) =>
{
    var lots = await svc.StockLotsAsync(warehouseId, productId);
    return Results.Ok(lots.Select(l => new
    {
        l.Id,
        Warehouse = l.Warehouse.Name,
        l.WarehouseId,
        ProductCode = l.Product.Code,
        ProductName = l.Product.Name,
        l.ProductId,
        l.LotNo,
        ProductionDate = l.ProductionDate?.ToString("yyyy-MM-dd"),
        ExpiredDate = l.ExpiredDate.ToString("yyyy-MM-dd"),
        InDate = l.InDate.ToString("yyyy-MM-dd"),
        l.Quantity,
        l.Note
    }));
});

// API Quản lý & Tra cứu Serial / IMEI hàng tồn kho (port từ Inv_InventoryBalanceSerial Skycic)
app.MapGet("/api/stock-serials", async (int? warehouseId, int? productId, StockSerialStatus? status, string? q, IWmsService svc) =>
{
    var report = await svc.StockSerialReportAsync(warehouseId, productId, status, q);
    return Results.Ok(report);
});

app.MapGet("/api/stock-serials/{id:int}", async (int id, IWmsService svc) =>
{
    var s = await svc.GetStockSerialAsync(id);
    if (s == null) return Results.NotFound(new { error = "Không tìm thấy Serial/IMEI." });
    return Results.Ok(new
    {
        s.Id,
        Warehouse = s.Warehouse.Name,
        s.WarehouseId,
        ProductCode = s.Product.Code,
        ProductName = s.Product.Name,
        s.ProductId,
        s.SerialNo,
        s.LotNo,
        Status = s.Status.ToString(),
        InDate = s.InDate.ToString("yyyy-MM-dd"),
        OutDate = s.OutDate?.ToString("yyyy-MM-dd"),
        s.RefNo,
        s.Note,
        s.CreatedAt,
        s.UpdatedAt
    });
});

app.MapPost("/api/stock-serials", async (CreateStockSerialDto dto, IWmsService svc) =>
{
    if (dto.WarehouseId <= 0) return Results.BadRequest(new { error = "Cần WarehouseId." });
    if (dto.ProductId <= 0) return Results.BadRequest(new { error = "Cần ProductId." });
    if (string.IsNullOrWhiteSpace(dto.SerialNo)) return Results.BadRequest(new { error = "Cần SerialNo." });

    try
    {
        var serial = new StockSerial
        {
            WarehouseId = dto.WarehouseId,
            ProductId = dto.ProductId,
            SerialNo = dto.SerialNo.Trim(),
            LotNo = dto.LotNo?.Trim(),
            RefNo = dto.RefNo?.Trim(),
            Note = dto.Note?.Trim(),
            Status = dto.Status ?? StockSerialStatus.Available
        };
        var id = await svc.CreateStockSerialAsync(serial);
        return Results.Ok(new { id, serialNo = serial.SerialNo, status = serial.Status.ToString() });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/stock-serials/{id:int}/change-status", async (int id, ChangeSerialStatusDto dto, IWmsService svc) =>
{
    var (ok, msg) = await svc.ChangeStockSerialStatusAsync(id, dto.Status, dto.Note);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Vị trí kho & Khay kệ (Warehouse Location / Block / Shelf - port từ Mst_InventoryBlock Skycic)
app.MapGet("/api/inventory-blocks", async (int? warehouseId, string? shelfCode, bool? activeOnly, string? q, IWmsService svc) =>
{
    var report = await svc.InventoryBlockReportAsync(warehouseId, shelfCode, activeOnly, q);
    return Results.Ok(report);
});

app.MapGet("/api/inventory-blocks/{id:int}", async (int id, IWmsService svc) =>
{
    var b = await svc.GetInventoryBlockAsync(id);
    if (b == null) return Results.NotFound(new { error = "Không tìm thấy vị trí ô kệ." });
    return Results.Ok(new
    {
        b.Id,
        Warehouse = b.Warehouse.Name,
        b.WarehouseId,
        b.InvBlockCode,
        b.ShelfCode,
        b.InvBlockDesc,
        b.Length,
        b.Width,
        b.Height,
        b.VolumeM3,
        b.MaxCapacity,
        b.FlagActive,
        b.Remark,
        b.CreatedAt,
        b.UpdatedAt
    });
});

app.MapPost("/api/inventory-blocks", async (CreateInventoryBlockDto dto, IWmsService svc) =>
{
    if (dto.WarehouseId <= 0) return Results.BadRequest(new { error = "Cần WarehouseId." });
    if (string.IsNullOrWhiteSpace(dto.InvBlockCode)) return Results.BadRequest(new { error = "Cần InvBlockCode." });
    if (string.IsNullOrWhiteSpace(dto.ShelfCode)) return Results.BadRequest(new { error = "Cần ShelfCode." });

    try
    {
        var block = new InventoryBlock
        {
            WarehouseId = dto.WarehouseId,
            InvBlockCode = dto.InvBlockCode.Trim(),
            ShelfCode = dto.ShelfCode.Trim(),
            InvBlockDesc = dto.InvBlockDesc?.Trim(),
            Length = dto.Length,
            Width = dto.Width,
            Height = dto.Height,
            MaxCapacity = dto.MaxCapacity > 0 ? dto.MaxCapacity : 100,
            Remark = dto.Remark?.Trim(),
            FlagActive = dto.FlagActive ?? true
        };
        var id = await svc.CreateInventoryBlockAsync(block);
        return Results.Ok(new { id, invBlockCode = block.InvBlockCode, shelfCode = block.ShelfCode });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/inventory-blocks/{id:int}", async (int id, UpdateInventoryBlockDto dto, IWmsService svc) =>
{
    var block = new InventoryBlock
    {
        ShelfCode = dto.ShelfCode ?? "",
        InvBlockDesc = dto.InvBlockDesc,
        Length = dto.Length,
        Width = dto.Width,
        Height = dto.Height,
        MaxCapacity = dto.MaxCapacity,
        Remark = dto.Remark,
        FlagActive = dto.FlagActive
    };
    var (ok, msg) = await svc.UpdateInventoryBlockAsync(id, block);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-blocks/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleInventoryBlockStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/inventory-blocks/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteInventoryBlockAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapGet("/api/inventory-blocks/shelves", async (int? warehouseId, IWmsService svc) =>
    Results.Ok(await svc.GetShelvesAsync(warehouseId)));

// API Lịch sử & Tính giá vốn kho hàng hoá (Cost Price Management & Calculation - port từ Inv_CostPriceHist Skycic)
app.MapGet("/api/cost-prices", async (int? warehouseId, int? productId, bool? currentOnly, DateTime? fromDate, DateTime? toDate, string? q, IWmsService svc) =>
{
    var report = await svc.CostPriceHistReportAsync(warehouseId, productId, currentOnly, fromDate, toDate, q);
    return Results.Ok(report);
});

app.MapGet("/api/cost-prices/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetCostPriceHistAsync(id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy bản ghi giá vốn." });
    return Results.Ok(new
    {
        item.Id,
        Warehouse = item.Warehouse?.Name ?? "Toàn hệ thống",
        item.WarehouseId,
        ProductCode = item.Product.Code,
        ProductName = item.Product.Name,
        item.ProductId,
        item.CostPrice,
        EffectDate = item.EffectDate.ToString("yyyy-MM-dd"),
        item.RefDocNo,
        item.IsCurrent,
        item.CalcPeriodName,
        SourceType = item.SourceType.ToString(),
        item.Remark,
        item.CreatedBy,
        item.CreatedAt,
        item.UpdatedBy,
        item.UpdatedAt
    });
});

app.MapPost("/api/cost-prices", async (CreateCostPriceDto dto, IWmsService svc) =>
{
    if (dto.ProductId <= 0) return Results.BadRequest(new { error = "Cần ProductId." });
    if (dto.CostPrice < 0) return Results.BadRequest(new { error = "Giá vốn không thể âm." });

    try
    {
        var item = new CostPriceHist
        {
            WarehouseId = dto.WarehouseId > 0 ? dto.WarehouseId : null,
            ProductId = dto.ProductId,
            CostPrice = dto.CostPrice,
            EffectDate = dto.EffectDate ?? DateTime.Today,
            RefDocNo = dto.RefDocNo?.Trim() ?? $"DC-{DateTime.Now:yyyyMMdd}",
            CalcPeriodName = dto.CalcPeriodName?.Trim() ?? "Thiết lập giá vốn thủ công",
            IsCurrent = dto.IsCurrent ?? true,
            SourceType = CostPriceSourceType.Manual,
            Remark = dto.Remark?.Trim(),
            CreatedBy = "api"
        };
        var id = await svc.CreateCostPriceHistAsync(item);
        return Results.Ok(new { id, costPrice = item.CostPrice, isCurrent = item.IsCurrent });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/cost-prices/{id:int}", async (int id, UpdateCostPriceDto dto, IWmsService svc) =>
{
    var (ok, msg) = await svc.UpdateCostPriceHistAsync(id, dto.CostPrice, dto.Remark);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/cost-prices/calculate", async (CalcCostPricePreviewDto dto, IWmsService svc) =>
{
    var from = dto.FromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
    var to = dto.ToDate ?? DateTime.Today;
    var name = string.IsNullOrWhiteSpace(dto.CalcPeriodName) ? $"Kỳ tính giá vốn {DateTime.Today:MM/yyyy}" : dto.CalcPeriodName.Trim();
    var preview = await svc.PreviewCalculateCostPriceAsync(dto.WarehouseId, from, to, name, dto.ProductIds);
    return Results.Ok(preview);
});

app.MapPost("/api/cost-prices/apply-calculation", async (ApplyCostPriceCalcDto dto, IWmsService svc) =>
{
    if (dto.Items == null || dto.Items.Count == 0)
        return Results.BadRequest(new { error = "Không có mặt hàng nào để áp dụng." });

    var effDate = dto.EffectDate ?? DateTime.Today;
    var name = string.IsNullOrWhiteSpace(dto.CalcPeriodName) ? $"Kỳ tính giá vốn {DateTime.Today:MM/yyyy}" : dto.CalcPeriodName.Trim();
    var items = dto.Items.Select(i => (i.ProductId, i.NewCostPrice, i.Note ?? "")).ToList();

    var (ok, msg, count) = await svc.ApplyCalculateCostPriceAsync(dto.WarehouseId, effDate, name, items);
    return ok ? Results.Ok(new { success = true, message = msg, count }) : Results.BadRequest(new { success = false, message = msg });
});


// API Lệnh điều chuyển kho (Move Order - port từ InvF_MoveOrd Skycic)
app.MapGet("/api/move-orders", async (int? fromWhId, int? toWhId, MoveOrderStatus? status, IWmsService svc) =>
    Results.Ok((await svc.MoveOrdersAsync(fromWhId, toWhId, status)).Select(m => new
    {
        m.Id,
        m.Code,
        FromWarehouse = m.FromWarehouse.Name,
        m.FromWarehouseId,
        ToWarehouse = m.ToWarehouse.Name,
        m.ToWarehouseId,
        m.Date,
        Status = m.Status.ToString(),
        m.TotalQty,
        m.MoveOrdTypeCode,
        m.MoveOrdTypeName,
        m.StockDocId,
        StockDocCode = m.StockDoc?.Code,
        m.Note,
        m.CreatedBy,
        m.CreatedAt,
        m.ApprovedAt,
        m.FinishedAt,
        Lines = m.Lines.Select(l => new { l.ProductId, l.Product.Code, l.Product.Name, l.Product.Uom, l.Quantity, l.Note })
    })));

app.MapGet("/api/move-orders/{id:int}", async (int id, IWmsService svc) =>
{
    var m = await svc.GetMoveOrderAsync(id);
    if (m == null) return Results.NotFound(new { error = "Không tìm thấy lệnh điều chuyển." });
    return Results.Ok(new
    {
        m.Id,
        m.Code,
        FromWarehouse = m.FromWarehouse.Name,
        m.FromWarehouseId,
        ToWarehouse = m.ToWarehouse.Name,
        m.ToWarehouseId,
        m.Date,
        Status = m.Status.ToString(),
        m.TotalQty,
        m.MoveOrdTypeCode,
        m.MoveOrdTypeName,
        m.StockDocId,
        StockDocCode = m.StockDoc?.Code,
        m.Note,
        m.CreatedBy,
        m.CreatedAt,
        m.ApprovedAt,
        m.FinishedAt,
        Lines = m.Lines.Select(l => new { l.ProductId, l.Product.Code, l.Product.Name, l.Product.Uom, l.Quantity, l.Note })
    });
});

app.MapPost("/api/move-orders", async (CreateMoveOrderDto dto, IWmsService svc) =>
{
    if (dto.FromWarehouseId <= 0 || dto.ToWarehouseId <= 0)
        return Results.BadRequest(new { error = "Cần FromWarehouseId và ToWarehouseId." });
    if (dto.FromWarehouseId == dto.ToWarehouseId)
        return Results.BadRequest(new { error = "Kho xuất và kho nhập phải khác nhau." });
    if (dto.Lines == null || dto.Lines.Count == 0)
        return Results.BadRequest(new { error = "Cần ít nhất 1 dòng hàng." });

    var order = new MoveOrder
    {
        FromWarehouseId = dto.FromWarehouseId,
        ToWarehouseId = dto.ToWarehouseId,
        MoveOrdTypeCode = dto.MoveOrdTypeCode,
        Note = dto.Note,
        CreatedBy = "api"
    };
    var lines = dto.Lines.Select(l => (l.ProductId, l.Quantity, l.Note)).ToList();
    var id = await svc.CreateMoveOrderAsync(order, lines);
    return Results.Ok(new { id, code = order.Code, status = order.Status.ToString() });
});

app.MapPost("/api/move-orders/{id:int}/approve", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ApproveMoveOrderAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/move-orders/{id:int}/execute", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ExecuteMoveOrderAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/move-orders/{id:int}/cancel", async (int id, IWmsService svc) =>
{
    try
    {
        await svc.CancelMoveOrderAsync(id);
        return Results.Ok(new { success = true, message = "Đã hủy lệnh điều chuyển." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

// API Xuất trả hàng nhà cung cấp (Return to Supplier - port từ InvF_InventoryReturnSup Skycic)
app.MapGet("/api/returns-to-supplier", async (int? warehouseId, ReturnSupStatus? status, IWmsService svc) =>
    Results.Ok((await svc.ReturnToSuppliersAsync(warehouseId, status)).Select(r => new
    {
        r.Id,
        r.Code,
        Warehouse = r.Warehouse.Name,
        r.WarehouseId,
        r.SupplierName,
        r.SupplierCode,
        r.RefDocNo,
        r.Date,
        Status = r.Status.ToString(),
        r.TotalQty,
        r.TotalAmount,
        r.StockDocId,
        StockDocCode = r.StockDoc?.Code,
        r.Reason,
        r.CreatedBy,
        r.CreatedAt,
        r.FinishedAt,
        Lines = r.Lines.Select(l => new { l.ProductId, l.Product.Code, l.Product.Name, l.Product.Uom, l.Quantity, l.UnitPrice, l.Amount, l.Note })
    })));

app.MapGet("/api/returns-to-supplier/{id:int}", async (int id, IWmsService svc) =>
{
    var r = await svc.GetReturnToSupplierAsync(id);
    if (r == null) return Results.NotFound(new { error = "Không tìm thấy phiếu trả hàng nhà cung cấp." });
    return Results.Ok(new
    {
        r.Id,
        r.Code,
        Warehouse = r.Warehouse.Name,
        r.WarehouseId,
        r.SupplierName,
        r.SupplierCode,
        r.RefDocNo,
        r.Date,
        Status = r.Status.ToString(),
        r.TotalQty,
        r.TotalAmount,
        r.StockDocId,
        StockDocCode = r.StockDoc?.Code,
        r.Reason,
        r.CreatedBy,
        r.CreatedAt,
        r.FinishedAt,
        Lines = r.Lines.Select(l => new { l.ProductId, l.Product.Code, l.Product.Name, l.Product.Uom, l.Quantity, l.UnitPrice, l.Amount, l.Note })
    });
});

app.MapPost("/api/returns-to-supplier", async (CreateReturnSupDto dto, IWmsService svc) =>
{
    if (dto.WarehouseId <= 0)
        return Results.BadRequest(new { error = "Cần WarehouseId." });
    if (string.IsNullOrWhiteSpace(dto.SupplierName))
        return Results.BadRequest(new { error = "Cần SupplierName." });
    if (dto.Lines == null || dto.Lines.Count == 0)
        return Results.BadRequest(new { error = "Cần ít nhất 1 mặt hàng xuất trả." });

    var returnDoc = new ReturnToSupplier
    {
        WarehouseId = dto.WarehouseId,
        SupplierName = dto.SupplierName.Trim(),
        SupplierCode = dto.SupplierCode?.Trim(),
        RefDocNo = dto.RefDocNo?.Trim(),
        Reason = dto.Reason?.Trim(),
        CreatedBy = "api"
    };
    var lines = dto.Lines.Select(l => (l.ProductId, l.Quantity, l.UnitPrice, l.Note)).ToList();
    var id = await svc.CreateReturnToSupplierAsync(returnDoc, lines);
    return Results.Ok(new { id, code = returnDoc.Code, status = returnDoc.Status.ToString() });
});

app.MapPost("/api/returns-to-supplier/{id:int}/approve", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ApproveReturnToSupplierAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/returns-to-supplier/{id:int}/cancel", async (int id, IWmsService svc) =>
{
    try
    {
        await svc.CancelReturnToSupplierAsync(id);
        return Results.Ok(new { success = true, message = "Đã hủy phiếu trả hàng NCC." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

// API Nhập hàng khách trả lại (Customer Return - port từ InvF_InventoryCusReturn Skycic)
app.MapGet("/api/customer-returns", async (int? warehouseId, CusReturnStatus? status, IWmsService svc) =>
    Results.Ok((await svc.CustomerReturnsAsync(warehouseId, status)).Select(c => new
    {
        c.Id,
        c.Code,
        Warehouse = c.Warehouse.Name,
        c.WarehouseId,
        c.CustomerName,
        c.CustomerCode,
        c.InvoiceNo,
        c.RefOrderNo,
        c.Date,
        Status = c.Status.ToString(),
        c.TotalQty,
        c.TotalAmount,
        c.StockDocId,
        StockDocCode = c.StockDoc?.Code,
        c.Reason,
        c.CreatedBy,
        c.CreatedAt,
        c.FinishedAt,
        Lines = c.Lines.Select(l => new { l.ProductId, l.Product.Code, l.Product.Name, l.Product.Uom, l.Quantity, l.UnitPrice, l.Amount, l.Note })
    })));

app.MapGet("/api/customer-returns/{id:int}", async (int id, IWmsService svc) =>
{
    var c = await svc.GetCustomerReturnAsync(id);
    if (c == null) return Results.NotFound(new { error = "Không tìm thấy phiếu khách hàng trả lại." });
    return Results.Ok(new
    {
        c.Id,
        c.Code,
        Warehouse = c.Warehouse.Name,
        c.WarehouseId,
        c.CustomerName,
        c.CustomerCode,
        c.InvoiceNo,
        c.RefOrderNo,
        c.Date,
        Status = c.Status.ToString(),
        c.TotalQty,
        c.TotalAmount,
        c.StockDocId,
        StockDocCode = c.StockDoc?.Code,
        c.Reason,
        c.CreatedBy,
        c.CreatedAt,
        c.FinishedAt,
        Lines = c.Lines.Select(l => new { l.ProductId, l.Product.Code, l.Product.Name, l.Product.Uom, l.Quantity, l.UnitPrice, l.Amount, l.Note })
    });
});

app.MapPost("/api/customer-returns", async (CreateCustomerReturnDto dto, IWmsService svc) =>
{
    if (dto.WarehouseId <= 0)
        return Results.BadRequest(new { error = "Cần WarehouseId." });
    if (string.IsNullOrWhiteSpace(dto.CustomerName))
        return Results.BadRequest(new { error = "Cần CustomerName." });
    if (dto.Lines == null || dto.Lines.Count == 0)
        return Results.BadRequest(new { error = "Cần ít nhất 1 mặt hàng nhận trả." });

    var returnDoc = new CustomerReturn
    {
        WarehouseId = dto.WarehouseId,
        CustomerName = dto.CustomerName.Trim(),
        CustomerCode = dto.CustomerCode?.Trim(),
        InvoiceNo = dto.InvoiceNo?.Trim(),
        RefOrderNo = dto.RefOrderNo?.Trim(),
        Reason = dto.Reason?.Trim(),
        CreatedBy = "api"
    };
    var lines = dto.Lines.Select(l => (l.ProductId, l.Quantity, l.UnitPrice, l.Note)).ToList();
    var id = await svc.CreateCustomerReturnAsync(returnDoc, lines);
    return Results.Ok(new { id, code = returnDoc.Code, status = returnDoc.Status.ToString() });
});

app.MapPost("/api/customer-returns/{id:int}/approve", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ApproveCustomerReturnAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/customer-returns/{id:int}/cancel", async (int id, IWmsService svc) =>
{
    try
    {
        await svc.CancelCustomerReturnAsync(id);
        return Results.Ok(new { success = true, message = "Đã hủy phiếu khách hàng trả lại." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

// API Kỳ chốt sổ tồn kho & Lưu vết Snapshot số dư (port từ Rpt_In_Out_Inv & 20200407.ChotTonKho.sql Skycic)
app.MapGet("/api/period-closings", async (int? warehouseId, PeriodClosingStatus? status, int? year, IWmsService svc) =>
{
    var list = await svc.PeriodClosingsAsync(warehouseId, status, year);
    return Results.Ok(list.Select(p => new
    {
        p.Id,
        p.Code,
        p.PeriodName,
        PeriodMonth = p.PeriodMonth.ToString("yyyy-MM"),
        Warehouse = p.Warehouse?.Name ?? "Toàn hệ thống",
        p.WarehouseId,
        Status = p.Status.ToString(),
        TotalItems = p.TotalItems,
        TotalOpeningQty = p.TotalOpeningQty,
        TotalInQty = p.TotalInQty,
        TotalOutQty = p.TotalOutQty,
        TotalClosingQty = p.TotalClosingQty,
        TotalClosingValue = p.TotalClosingValue,
        ClosedAt = p.ClosedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
        p.ClosedBy,
        p.Note,
        p.ReopenReason,
        p.ReopenedAt,
        p.CreatedAt
    }));
});

app.MapGet("/api/period-closings/{id:int}", async (int id, IWmsService svc) =>
{
    var p = await svc.GetPeriodClosingAsync(id);
    if (p == null) return Results.NotFound(new { error = "Không tìm thấy kỳ chốt kho." });
    return Results.Ok(new
    {
        p.Id,
        p.Code,
        p.PeriodName,
        PeriodMonth = p.PeriodMonth.ToString("yyyy-MM"),
        Warehouse = p.Warehouse?.Name ?? "Toàn hệ thống",
        p.WarehouseId,
        Status = p.Status.ToString(),
        TotalItems = p.TotalItems,
        TotalOpeningQty = p.TotalOpeningQty,
        TotalInQty = p.TotalInQty,
        TotalOutQty = p.TotalOutQty,
        TotalClosingQty = p.TotalClosingQty,
        TotalClosingValue = p.TotalClosingValue,
        ClosedAt = p.ClosedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
        p.ClosedBy,
        p.Note,
        p.ReopenReason,
        p.ReopenedAt,
        p.CreatedAt,
        Lines = p.Lines.Select(l => new
        {
            l.Id,
            Warehouse = l.Warehouse.Name,
            l.WarehouseId,
            ProductCode = l.Product.Code,
            ProductName = l.Product.Name,
            l.Product.Uom,
            l.ProductId,
            l.OpeningQty,
            l.InQty,
            l.LastInPrice,
            l.InAmount,
            l.OutQty,
            l.LastOutPrice,
            l.OutAmount,
            l.ClosingQty,
            l.CostPrice,
            l.ClosingValue,
            l.Note
        })
    });
});

app.MapPost("/api/period-closings/preview", async (PreviewPeriodClosingDto dto, IWmsService svc) =>
{
    if (dto.Year < 2000 || dto.Year > 2100 || dto.Month < 1 || dto.Month > 12)
        return Results.BadRequest(new { error = "Tháng hoặc năm không hợp lệ." });

    var preview = await svc.PreviewPeriodClosingAsync(dto.WarehouseId, dto.Year, dto.Month);
    return Results.Ok(preview);
});

app.MapPost("/api/period-closings", async (CreatePeriodClosingDto dto, IWmsService svc) =>
{
    if (dto.Year < 2000 || dto.Year > 2100 || dto.Month < 1 || dto.Month > 12)
        return Results.BadRequest(new { error = "Tháng hoặc năm không hợp lệ." });

    var (ok, msg, id) = await svc.CreateAndClosePeriodAsync(dto.WarehouseId, dto.Year, dto.Month, dto.Note, dto.ClosedBy ?? "api");
    return ok ? Results.Ok(new { success = true, id, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/period-closings/{id:int}/reopen", async (int id, ReopenPeriodClosingDto dto, IWmsService svc) =>
{
    var (ok, msg) = await svc.ReopenPeriodClosingAsync(id, dto.Reason ?? "Mở lại kỳ");
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/period-closings/{id:int}/cancel", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.CancelPeriodClosingAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Thùng Carton & Đóng kiện hàng hoá (Warehouse Carton & Packaging - port từ Inv_InventoryCarton Skycic)
app.MapGet("/api/inventory-cartons", async (int? warehouseId, int? productId, CartonStatus? status, string? q, IWmsService svc) =>
{
    var report = await svc.CartonsAsync(warehouseId, productId, status, q);
    return Results.Ok(report);
});

app.MapGet("/api/inventory-cartons/{id:int}", async (int id, IWmsService svc) =>
{
    var c = await svc.GetCartonAsync(id);
    if (c == null) return Results.NotFound(new { error = "Không tìm thấy thùng carton." });
    return Results.Ok(new
    {
        c.Id,
        c.CartonCode,
        c.QrCode,
        Warehouse = c.Warehouse.Name,
        c.WarehouseId,
        c.CartonType,
        ProductCode = c.Product?.Code,
        ProductName = c.Product?.Name,
        c.ProductId,
        c.LotNo,
        c.Quantity,
        c.Capacity,
        c.LengthCm,
        c.WidthCm,
        c.HeightCm,
        c.VolumeM3,
        c.GrossWeightKg,
        Status = c.Status.ToString(),
        c.ShelfLocation,
        c.PackerName,
        PackedAt = c.PackedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
        SealedAt = c.SealedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
        ShippedAt = c.ShippedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
        c.RefDocNo,
        c.Remark,
        c.CreatedAt,
        c.UpdatedAt
    });
});

app.MapPost("/api/inventory-cartons", async (CreateCartonDto dto, IWmsService svc) =>
{
    if (dto.WarehouseId <= 0) return Results.BadRequest(new { error = "Cần WarehouseId." });
    try
    {
        var carton = new InventoryCarton
        {
            WarehouseId = dto.WarehouseId,
            CartonCode = dto.CartonCode?.Trim() ?? "",
            CartonType = string.IsNullOrWhiteSpace(dto.CartonType) ? "Thùng carton tiêu chuẩn" : dto.CartonType.Trim(),
            LengthCm = dto.LengthCm > 0 ? dto.LengthCm : 40,
            WidthCm = dto.WidthCm > 0 ? dto.WidthCm : 30,
            HeightCm = dto.HeightCm > 0 ? dto.HeightCm : 30,
            Capacity = dto.Capacity > 0 ? dto.Capacity : 50,
            ShelfLocation = dto.ShelfLocation?.Trim(),
            Remark = dto.Remark?.Trim(),
            Status = CartonStatus.Empty
        };
        var id = await svc.CreateCartonAsync(carton);
        return Results.Ok(new { id, cartonCode = carton.CartonCode, status = carton.Status.ToString() });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/inventory-cartons/generate-batch", async (GenerateCartonBatchDto dto, IWmsService svc) =>
{
    if (dto.WarehouseId <= 0) return Results.BadRequest(new { error = "Cần WarehouseId." });
    if (dto.Count <= 0 || dto.Count > 500) return Results.BadRequest(new { error = "Số lượng sinh phải từ 1 đến 500." });

    var (ok, msg, ids) = await svc.GenerateCartonsBatchAsync(
        dto.WarehouseId,
        dto.CartonType ?? "Thùng carton tiêu chuẩn",
        dto.Count,
        dto.LengthCm,
        dto.WidthCm,
        dto.HeightCm,
        dto.Capacity,
        dto.ShelfLocation,
        dto.Prefix);

    return ok ? Results.Ok(new { success = true, message = msg, ids, count = ids.Count }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-cartons/{id:int}/pack", async (int id, PackCartonDto dto, IWmsService svc) =>
{
    if (dto.ProductId <= 0) return Results.BadRequest(new { error = "Cần ProductId." });
    if (dto.Quantity <= 0) return Results.BadRequest(new { error = "Số lượng đóng phải > 0." });

    var (ok, msg) = await svc.PackCartonAsync(id, dto.ProductId, dto.Quantity, dto.LotNo, dto.GrossWeightKg, dto.PackerName, dto.Note);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-cartons/{id:int}/seal", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.SealCartonAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-cartons/{id:int}/unpack", async (int id, UnpackCartonDto? dto, IWmsService svc) =>
{
    var (ok, msg) = await svc.UnpackCartonAsync(id, dto?.Reason);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-cartons/{id:int}/ship", async (int id, ShipCartonDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.RefDocNo)) return Results.BadRequest(new { error = "Cần RefDocNo (Mã chứng từ xuất kho)." });
    var (ok, msg) = await svc.ShipCartonAsync(id, dto.RefDocNo);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/inventory-cartons/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteCartonAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Hộp đóng gói & Phân cấp bao bì kho (Warehouse Box Packaging - port từ Inv_InventoryBox Skycic)
app.MapGet("/api/inventory-boxes", async (int? warehouseId, int? productId, int? cartonId, BoxStatus? status, bool? flagMap, string? q, IWmsService svc) =>
{
    var report = await svc.BoxesAsync(warehouseId, productId, cartonId, status, flagMap, q);
    return Results.Ok(report);
});

app.MapGet("/api/inventory-boxes/{id:int}", async (int id, IWmsService svc) =>
{
    var b = await svc.GetBoxAsync(id);
    if (b == null) return Results.NotFound(new { error = "Không tìm thấy hộp đóng gói." });
    return Results.Ok(new
    {
        b.Id,
        b.BoxCode,
        b.QrCode,
        b.GenTimesBoxNo,
        b.SecretNo,
        Warehouse = b.Warehouse.Name,
        b.WarehouseId,
        CartonCode = b.Carton?.CartonCode,
        b.CartonId,
        b.BoxType,
        ProductCode = b.Product?.Code,
        ProductName = b.Product?.Name,
        b.ProductId,
        b.LotNo,
        b.Quantity,
        b.Capacity,
        b.LengthCm,
        b.WidthCm,
        b.HeightCm,
        b.VolumeM3,
        b.GrossWeightKg,
        Status = b.Status.ToString(),
        b.FlagMap,
        b.FlagUsed,
        b.ShelfLocation,
        b.PackerName,
        PackedAt = b.PackedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
        SealedAt = b.SealedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
        ShippedAt = b.ShippedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
        b.RefDocNo,
        b.Remark,
        b.CreatedAt,
        b.UpdatedAt
    });
});

app.MapPost("/api/inventory-boxes", async (CreateBoxDto dto, IWmsService svc) =>
{
    if (dto.WarehouseId <= 0) return Results.BadRequest(new { error = "Cần WarehouseId." });
    try
    {
        var box = new InventoryBox
        {
            WarehouseId = dto.WarehouseId,
            BoxCode = dto.BoxCode?.Trim() ?? "",
            BoxType = string.IsNullOrWhiteSpace(dto.BoxType) ? "Hộp duplex tiêu chuẩn" : dto.BoxType.Trim(),
            LengthCm = dto.LengthCm > 0 ? dto.LengthCm : 20,
            WidthCm = dto.WidthCm > 0 ? dto.WidthCm : 15,
            HeightCm = dto.HeightCm > 0 ? dto.HeightCm : 10,
            Capacity = dto.Capacity > 0 ? dto.Capacity : 10,
            CartonId = (dto.CartonId.HasValue && dto.CartonId.Value > 0) ? dto.CartonId : null,
            FlagMap = (dto.CartonId.HasValue && dto.CartonId.Value > 0),
            ShelfLocation = dto.ShelfLocation?.Trim(),
            Remark = dto.Remark?.Trim(),
            Status = (dto.CartonId.HasValue && dto.CartonId.Value > 0) ? BoxStatus.InCarton : BoxStatus.Empty
        };
        var id = await svc.CreateBoxAsync(box);
        return Results.Ok(new { id, boxCode = box.BoxCode, status = box.Status.ToString() });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/inventory-boxes/generate-batch", async (GenerateBoxBatchDto dto, IWmsService svc) =>
{
    if (dto.WarehouseId <= 0) return Results.BadRequest(new { error = "Cần WarehouseId." });
    if (dto.Count <= 0 || dto.Count > 1000) return Results.BadRequest(new { error = "Số lượng sinh phải từ 1 đến 1000." });

    var (ok, msg, ids) = await svc.GenerateBoxesBatchAsync(
        dto.WarehouseId,
        dto.BoxType ?? "Hộp duplex tiêu chuẩn",
        dto.Count,
        dto.LengthCm,
        dto.WidthCm,
        dto.HeightCm,
        dto.Capacity,
        (dto.CartonId.HasValue && dto.CartonId.Value > 0) ? dto.CartonId : null,
        dto.ShelfLocation,
        dto.Prefix);

    return ok ? Results.Ok(new { success = true, message = msg, ids, count = ids.Count }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-boxes/{id:int}/pack", async (int id, PackBoxDto dto, IWmsService svc) =>
{
    if (dto.ProductId <= 0) return Results.BadRequest(new { error = "Cần ProductId." });
    if (dto.Quantity <= 0) return Results.BadRequest(new { error = "Số lượng đóng phải > 0." });

    var (ok, msg) = await svc.PackBoxAsync(id, dto.ProductId, dto.Quantity, dto.LotNo, dto.GrossWeightKg, dto.PackerName, dto.SecretNo, dto.Note);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-boxes/{id:int}/seal", async (int id, SealBoxDto? dto, IWmsService svc) =>
{
    var (ok, msg) = await svc.SealBoxAsync(id, dto?.SecretNo);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-boxes/{id:int}/map-carton", async (int id, MapBoxCartonDto dto, IWmsService svc) =>
{
    if (dto.CartonId <= 0) return Results.BadRequest(new { error = "Cần CartonId." });
    var (ok, msg) = await svc.MapBoxToCartonAsync(id, dto.CartonId);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-boxes/{id:int}/unmap-carton", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.UnmapBoxFromCartonAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-boxes/{id:int}/unpack", async (int id, UnpackBoxDto? dto, IWmsService svc) =>
{
    var (ok, msg) = await svc.UnpackBoxAsync(id, dto?.Reason);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-boxes/{id:int}/ship", async (int id, ShipBoxDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.RefDocNo)) return Results.BadRequest(new { error = "Cần RefDocNo (Mã chứng từ xuất kho)." });
    var (ok, msg) = await svc.ShipBoxAsync(id, dto.RefDocNo);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/inventory-boxes/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteBoxAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Nhập kho thành phẩm sản xuất (port từ InvF_InventoryInFG Skycic)
app.MapGet("/api/inventory-in-fg", async (int? warehouseId, InvInFGStatus? status, InvInFGFormType? formType, DateTime? fromDate, DateTime? toDate, string? q, IWmsService svc) =>
{
    var report = await svc.InventoryInFGsAsync(warehouseId, status, formType, fromDate, toDate, q);
    return Results.Ok(report);
});

app.MapGet("/api/inventory-in-fg/{id:int}", async (int id, IWmsService svc) =>
{
    var doc = await svc.GetInventoryInFGAsync(id);
    if (doc == null) return Results.NotFound(new { error = "Không tìm thấy phiếu nhập kho thành phẩm." });
    return Results.Ok(new
    {
        doc.Id,
        doc.Code,
        Warehouse = doc.Warehouse.Name,
        doc.WarehouseId,
        FormType = doc.FormType.ToString(),
        doc.WorkshopName,
        doc.WorkOrderNo,
        doc.ShiftLeader,
        Date = doc.Date.ToString("yyyy-MM-dd"),
        Status = doc.Status.ToString(),
        doc.TotalPlanQty,
        doc.TotalActualQty,
        doc.TotalDefectQty,
        doc.TotalAmount,
        doc.PassRatePercent,
        doc.TotalSerialsCount,
        doc.StockDocId,
        StockDocCode = doc.StockDoc?.Code,
        doc.Remark,
        doc.CreatedBy,
        doc.CreatedAt,
        ApprovedAt = doc.ApprovedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
        doc.ApprovedBy,
        Lines = doc.Lines.Select(l => new
        {
            l.Id,
            ProductCode = l.Product.Code,
            ProductName = l.Product.Name,
            l.Product.Uom,
            l.ProductId,
            l.PlanQty,
            l.ActualQty,
            l.DefectQty,
            l.UnitCost,
            l.Amount,
            l.PassRate,
            ProductionDate = l.ProductionDate?.ToString("yyyy-MM-dd"),
            l.Note
        }),
        Serials = doc.Serials.Select(s => new
        {
            s.Id,
            ProductCode = s.Product.Code,
            ProductName = s.Product.Name,
            s.ProductId,
            s.SerialNo,
            s.Note
        })
    });
});

app.MapPost("/api/inventory-in-fg", async (CreateInventoryInFGDto dto, IWmsService svc) =>
{
    if (dto.WarehouseId <= 0) return Results.BadRequest(new { error = "Cần WarehouseId." });
    if (string.IsNullOrWhiteSpace(dto.WorkshopName)) return Results.BadRequest(new { error = "Cần WorkshopName." });
    if (dto.Lines == null || dto.Lines.Count == 0) return Results.BadRequest(new { error = "Cần ít nhất 1 dòng thành phẩm." });

    try
    {
        var doc = new InventoryInFG
        {
            WarehouseId = dto.WarehouseId,
            FormType = dto.FormType,
            WorkshopName = dto.WorkshopName.Trim(),
            WorkOrderNo = dto.WorkOrderNo?.Trim(),
            ShiftLeader = dto.ShiftLeader?.Trim(),
            Date = dto.Date ?? DateTime.Today,
            Remark = dto.Remark?.Trim(),
            CreatedBy = "api"
        };

        var lines = dto.Lines.Select(l => (l.ProductId, l.PlanQty, l.ActualQty, l.DefectQty, l.UnitCost, (DateTime?)null, l.Note)).ToList();
        var serials = dto.Serials != null
            ? dto.Serials.Select(s => (s.ProductId, s.SerialNo, s.Note)).ToList()
            : new List<(int, string, string?)>();

        var id = await svc.CreateInventoryInFGAsync(doc, lines, serials);
        return Results.Ok(new { id, code = doc.Code, status = doc.Status.ToString() });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/inventory-in-fg/{id:int}/approve", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ApproveInventoryInFGAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-in-fg/{id:int}/cancel", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.CancelInventoryInFGAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Xuất kho thành phẩm & Vận chuyển phân phối (port từ InvF_InventoryOutFG Skycic)
app.MapGet("/api/inventory-out-fg", async (int? warehouseId, InvOutFGStatus? status, InvOutFGType? outType, InvOutFGFormType? formType, DateTime? fromDate, DateTime? toDate, string? q, IWmsService svc) =>
{
    var report = await svc.InventoryOutFGsAsync(warehouseId, status, outType, formType, fromDate, toDate, q);
    return Results.Ok(report);
});

app.MapGet("/api/inventory-out-fg/{id:int}", async (int id, IWmsService svc) =>
{
    var doc = await svc.GetInventoryOutFGAsync(id);
    if (doc == null) return Results.NotFound(new { error = "Không tìm thấy phiếu xuất kho thành phẩm." });
    return Results.Ok(new
    {
        doc.Id,
        doc.Code,
        Warehouse = doc.Warehouse.Name,
        doc.WarehouseId,
        OutType = doc.OutType.ToString(),
        FormType = doc.FormType.ToString(),
        doc.CustomerName,
        doc.AgentCode,
        doc.DeliveryAddress,
        doc.DriverName,
        doc.DriverPhone,
        doc.PlateNo,
        doc.MoocNo,
        doc.OrderNo,
        Date = doc.Date.ToString("yyyy-MM-dd"),
        Status = doc.Status.ToString(),
        doc.TotalQty,
        doc.TotalAmount,
        doc.TotalSerialsCount,
        doc.StockDocId,
        StockDocCode = doc.StockDoc?.Code,
        doc.Remark,
        doc.CreatedBy,
        doc.CreatedAt,
        ApprovedAt = doc.ApprovedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
        doc.ApprovedBy,
        Lines = doc.Lines.Select(l => new
        {
            l.Id,
            ProductCode = l.Product.Code,
            ProductName = l.Product.Name,
            l.Product.Uom,
            l.ProductId,
            l.Qty,
            l.UnitPrice,
            l.UnitCost,
            l.Amount,
            l.Note
        }),
        Serials = doc.Serials.Select(s => new
        {
            s.Id,
            ProductCode = s.Product.Code,
            ProductName = s.Product.Name,
            s.ProductId,
            s.SerialNo,
            s.Note
        })
    });
});

app.MapPost("/api/inventory-out-fg", async (CreateInventoryOutFGDto dto, IWmsService svc) =>
{
    if (dto.WarehouseId <= 0) return Results.BadRequest(new { error = "Cần WarehouseId." });
    if (string.IsNullOrWhiteSpace(dto.CustomerName)) return Results.BadRequest(new { error = "Cần CustomerName." });
    if (dto.Lines == null || dto.Lines.Count == 0) return Results.BadRequest(new { error = "Cần ít nhất 1 dòng thành phẩm." });

    try
    {
        var doc = new InventoryOutFG
        {
            WarehouseId = dto.WarehouseId,
            OutType = dto.OutType,
            FormType = dto.FormType,
            CustomerName = dto.CustomerName.Trim(),
            AgentCode = dto.AgentCode?.Trim(),
            DeliveryAddress = dto.DeliveryAddress?.Trim(),
            DriverName = dto.DriverName?.Trim(),
            DriverPhone = dto.DriverPhone?.Trim(),
            PlateNo = dto.PlateNo?.Trim(),
            MoocNo = dto.MoocNo?.Trim(),
            OrderNo = dto.OrderNo?.Trim(),
            Date = dto.Date ?? DateTime.Today,
            Remark = dto.Remark?.Trim(),
            CreatedBy = "api"
        };

        var lines = dto.Lines.Select(l => (l.ProductId, l.Qty, l.UnitPrice, l.UnitCost, l.Note)).ToList();
        var serials = dto.Serials != null
            ? dto.Serials.Select(s => (s.ProductId, s.SerialNo, s.Note)).ToList()
            : new List<(int, string, string?)>();

        var id = await svc.CreateInventoryOutFGAsync(doc, lines, serials);
        return Results.Ok(new { id, code = doc.Code, status = doc.Status.ToString() });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/api/inventory-out-fg/{id:int}/approve", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ApproveInventoryOutFGAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-out-fg/{id:int}/cancel", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.CancelInventoryOutFGAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Báo cáo Tổng hợp Nhập mua & Trả hàng NCC (port từ Rpt_Summary_InAndReturnSup Skycic)
app.MapGet("/api/reports/in-return-sup", async (int? warehouseId, string? supplierCode, DateTime? fromDate, DateTime? toDate, string? q, IWmsService svc) =>
{
    var report = await svc.SummaryInReturnSupReportAsync(warehouseId, supplierCode, fromDate, toDate, q);
    return Results.Ok(report);
});

// API Danh mục Nhà cung cấp (port từ Mst_Supplier Skycic)
app.MapGet("/api/suppliers", async (string? q, bool? activeOnly, IWmsService svc) =>
{
    var list = await svc.SuppliersAsync(q, activeOnly);
    return Results.Ok(list.Select(s => new
    {
        s.Id,
        s.Code,
        s.Name,
        s.ContactName,
        s.Phone,
        s.Email,
        s.Address,
        s.TaxCode,
        s.IsActive,
        s.Note,
        s.CreatedAt
    }));
});

app.MapGet("/api/suppliers/{id:int}", async (int id, IWmsService svc) =>
{
    var s = await svc.GetSupplierAsync(id);
    if (s == null) return Results.NotFound(new { error = "Không tìm thấy nhà cung cấp." });
    return Results.Ok(s);
});

app.MapPost("/api/suppliers", async (CreateSupplierDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên nhà cung cấp." });
    try
    {
        var sup = new Supplier
        {
            Code = dto.Code?.Trim() ?? "",
            Name = dto.Name.Trim(),
            ContactName = dto.ContactName?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            TaxCode = dto.TaxCode?.Trim(),
            Note = dto.Note?.Trim(),
            IsActive = dto.IsActive ?? true
        };
        var id = await svc.CreateSupplierAsync(sup);
        return Results.Ok(new { id, code = sup.Code, name = sup.Name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/suppliers/{id:int}", async (int id, UpdateSupplierDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên nhà cung cấp." });
    var sup = new Supplier
    {
        Name = dto.Name.Trim(),
        ContactName = dto.ContactName?.Trim(),
        Phone = dto.Phone?.Trim(),
        Email = dto.Email?.Trim(),
        Address = dto.Address?.Trim(),
        TaxCode = dto.TaxCode?.Trim(),
        Note = dto.Note?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateSupplierAsync(id, sup);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/suppliers/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleSupplierStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Danh mục Khách hàng & Đại lý phân phối (port từ Mst_Customer Skycic)
app.MapGet("/api/customers", async (string? q, string? customerType, bool? activeOnly, string? customerGrpCode, string? customerSourceCode, IWmsService svc) =>
{
    var list = await svc.CustomersAsync(q, customerType, activeOnly, customerGrpCode, customerSourceCode);
    return Results.Ok(list.Select(c => new
    {
        c.Id,
        c.Code,
        c.Name,
        c.CustomerType,
        c.CustomerSourceCode,
        c.CustomerGrpCode,
        c.AreaCode,
        c.ContactName,
        c.ContactPhone,
        c.Phone,
        c.Email,
        c.Address,
        c.Province,
        c.TaxCode,
        c.IsActive,
        c.Note,
        c.CreatedAt
    }));
});

app.MapGet("/api/customers/{id:int}", async (int id, IWmsService svc) =>
{
    var c = await svc.GetCustomerAsync(id);
    if (c == null) return Results.NotFound(new { error = "Không tìm thấy khách hàng." });
    return Results.Ok(c);
});

app.MapGet("/api/customers/{id:int}/history", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetCustomerDetailAsync(id);
    if (detail == null) return Results.NotFound(new { error = "Không tìm thấy khách hàng." });
    return Results.Ok(detail);
});

app.MapPost("/api/customers", async (CreateCustomerDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên khách hàng." });
    try
    {
        var cust = new Customer
        {
            Code = dto.Code?.Trim() ?? "",
            Name = dto.Name.Trim(),
            CustomerType = string.IsNullOrWhiteSpace(dto.CustomerType) ? "Đại lý phân phối" : dto.CustomerType.Trim(),
            ContactName = dto.ContactName?.Trim(),
            ContactPhone = dto.ContactPhone?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            Province = dto.Province?.Trim(),
            AreaCode = string.IsNullOrWhiteSpace(dto.AreaCode) ? null : dto.AreaCode.Trim().ToUpperInvariant(),
            CustomerGrpCode = string.IsNullOrWhiteSpace(dto.CustomerGrpCode) ? null : dto.CustomerGrpCode.Trim().ToUpperInvariant(),
            CustomerSourceCode = string.IsNullOrWhiteSpace(dto.CustomerSourceCode) ? null : dto.CustomerSourceCode.Trim().ToUpperInvariant(),
            TaxCode = dto.TaxCode?.Trim(),
            Note = dto.Note?.Trim(),
            IsActive = dto.IsActive ?? true
        };
        var id = await svc.CreateCustomerAsync(cust);
        return Results.Ok(new { id, code = cust.Code, name = cust.Name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/customers/{id:int}", async (int id, UpdateCustomerDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên khách hàng." });
    var cust = new Customer
    {
        Name = dto.Name.Trim(),
        CustomerType = string.IsNullOrWhiteSpace(dto.CustomerType) ? "Đại lý phân phối" : dto.CustomerType.Trim(),
        ContactName = dto.ContactName?.Trim(),
        ContactPhone = dto.ContactPhone?.Trim(),
        Phone = dto.Phone?.Trim(),
        Email = dto.Email?.Trim(),
        Address = dto.Address?.Trim(),
        Province = dto.Province?.Trim(),
        AreaCode = string.IsNullOrWhiteSpace(dto.AreaCode) ? null : dto.AreaCode.Trim().ToUpperInvariant(),
        CustomerGrpCode = string.IsNullOrWhiteSpace(dto.CustomerGrpCode) ? null : dto.CustomerGrpCode.Trim().ToUpperInvariant(),
        CustomerSourceCode = string.IsNullOrWhiteSpace(dto.CustomerSourceCode) ? null : dto.CustomerSourceCode.Trim().ToUpperInvariant(),
        TaxCode = dto.TaxCode?.Trim(),
        Note = dto.Note?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateCustomerAsync(id, cust);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/customers/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleCustomerStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/customers/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteCustomerAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/orgs/register", async (RegisterOrgDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần Name." });
    var org = new Org { Name = dto.Name.Trim(), ApiKey = "wms_" + Guid.NewGuid().ToString("N") };
    db.Orgs.Add(org); await db.SaveChangesAsync();
    return Results.Ok(new { orgId = org.Id, apiKey = org.ApiKey });
});

// API Báo cáo Tổng hợp Xuất kho Chi tiết (port từ Rpt_InvF_InventoryOutDtl Skycic)
app.MapGet("/api/reports/inventory-out-detail", async (int? warehouseId, DateTime? fromDate, DateTime? toDate, string? outType, string? q, IWmsService svc) =>
{
    var report = await svc.InventoryOutDtlReportAsync(warehouseId, fromDate, toDate, outType, q);
    return Results.Ok(report);
});

// API Báo cáo Tổng hợp Nhập kho Chi tiết (port từ Rpt_InventoryInDtl Skycic)
app.MapGet("/api/reports/inventory-in-detail", async (int? warehouseId, DateTime? fromDate, DateTime? toDate, string? inType, string? q, IWmsService svc) =>
{
    var report = await svc.InventoryInDtlReportAsync(warehouseId, fromDate, toDate, inType, q);
    return Results.Ok(report);
});

// API Báo cáo Ma trận Tổng hợp Nhập - Xuất 12 Tháng (port từ Rpt_Summary_In_Out Skycic)
app.MapGet("/api/reports/summary-in-out", async (int? year, int? warehouseId, string? viewMode, string? q, IWmsService svc) =>
{
    int targetYear = year ?? DateTime.Today.Year;
    var report = await svc.MonthlyMatrixReportAsync(targetYear, warehouseId, viewMode, q);
    return Results.Ok(report);
});

// API Báo cáo Tổng hợp Số lượng Tồn kho theo Kỳ 12 Tháng (port từ Rpt_Summary_QtyInvByPeriod Skycic)
app.MapGet("/api/reports/summary-qty-period", async (int? year, int? warehouseId, string? q, IWmsService svc) =>
{
    int targetYear = year ?? DateTime.Today.Year;
    var report = await svc.MonthlyMatrixReportAsync(targetYear, warehouseId, "BALANCE_ONLY", q);
    return Results.Ok(new
    {
        year = report.Year,
        warehouse = report.WarehouseName,
        warehouseId = report.WarehouseId,
        totalProducts = report.QtyPeriodRows.Count,
        monthlyTotalBalance = report.MonthlyTotalBalance,
        rows = report.QtyPeriodRows
    });
});

// API Báo cáo Tồn kho mở rộng & Dự phóng khả dụng (port từ Rpt_Inv_InventoryBalance_Extend Skycic)
app.MapGet("/api/reports/inventory-balance-extend", async (int? warehouseId, StockExtendStatus? status, string? q, IWmsService svc) =>
{
    var report = await svc.StockExtendReportAsync(warehouseId, status, q);
    return Results.Ok(report);
});

// API Báo cáo Đánh giá giá trị tồn kho & Cơ cấu tài sản kho (port từ Rpt_Inv_InventoryBalance_ByValue & Rpt_Inv_InventoryBalance Skycic)
app.MapGet("/api/reports/inventory-balance-by-value", async (int? warehouseId, InventoryValuationAbcClass? abcClass, bool? onlyHasStock, string? q, DateTime? asOfDate, IWmsService svc) =>
{
    var report = await svc.InventoryValuationReportAsync(warehouseId, abcClass, onlyHasStock ?? true, q, asOfDate);
    return Results.Ok(report);
});

app.MapGet("/api/reports/inventory-valuation", async (int? warehouseId, InventoryValuationAbcClass? abcClass, bool? onlyHasStock, string? q, DateTime? asOfDate, IWmsService svc) =>
{
    var report = await svc.InventoryValuationReportAsync(warehouseId, abcClass, onlyHasStock ?? true, q, asOfDate);
    return Results.Ok(new
    {
        warehouse = report.WarehouseName,
        warehouseId = report.WarehouseId,
        asOfDate = report.AsOfDate.ToString("yyyy-MM-dd"),
        summary = new
        {
            totalItems = report.TotalItems,
            totalPhysicalQty = report.TotalPhysicalQty,
            totalBlockedQty = report.TotalBlockedQty,
            totalAvailableQty = report.TotalAvailableQty,
            grandTotalValMixBase = report.GrandTotalValMixBase,
            grandTotalValAvail = report.GrandTotalValAvail,
            grandTotalValBlock = report.GrandTotalValBlock,
            availValueRatio = report.AvailValueRatio,
            classA = new { count = report.ClassACount, value = report.ClassAValue },
            classB = new { count = report.ClassBCount, value = report.ClassBValue },
            classC = new { count = report.ClassCCount, value = report.ClassCValue }
        },
        items = report.Rows
    });
});

// API Quản lý Loại mặt hàng kho (port từ Mst_PartType Skycic)
app.MapGet("/api/part-types", async (string? q, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.PartTypesReportAsync(q, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/part-types/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetPartTypeAsync(id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy loại mặt hàng." });
    return Results.Ok(item);
});

app.MapGet("/api/part-types/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetPartTypeByCodeAsync(code);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy loại mặt hàng." });
    return Results.Ok(item);
});

app.MapGet("/api/part-types/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetPartTypeDetailAsync(id);
    if (detail == null) return Results.NotFound(new { error = "Không tìm thấy loại mặt hàng." });
    return Results.Ok(detail);
});

app.MapPost("/api/part-types", async (CreatePartTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên loại mặt hàng." });
    try
    {
        var item = new PartType
        {
            Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
            Name = dto.Name.Trim(),
            Remark = dto.Remark?.Trim(),
            IsActive = dto.IsActive ?? true
        };
        var id = await svc.CreatePartTypeAsync(item);
        return Results.Ok(new { id, code = item.Code, name = item.Name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/part-types/{id:int}", async (int id, UpdatePartTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên loại mặt hàng." });
    var item = new PartType
    {
        Name = dto.Name.Trim(),
        Remark = dto.Remark?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdatePartTypeAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/part-types/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.TogglePartTypeStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/part-types/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeletePartTypeAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Thương hiệu / Nhãn hiệu hàng hóa kho (port từ Mst_Brand Skycic)
app.MapGet("/api/brands", async (string? q, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.BrandsReportAsync(q, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/brands/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetBrandAsync(id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy thương hiệu." });
    return Results.Ok(item);
});

app.MapGet("/api/brands/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetBrandByCodeAsync(code);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy thương hiệu." });
    return Results.Ok(item);
});

app.MapGet("/api/brands/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetBrandDetailAsync(id);
    if (detail == null) return Results.NotFound(new { error = "Không tìm thấy thương hiệu." });
    return Results.Ok(detail);
});

app.MapPost("/api/brands", async (CreateBrandDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên thương hiệu." });
    try
    {
        var item = new Brand
        {
            Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
            Name = dto.Name.Trim(),
            Origin = dto.Origin?.Trim(),
            Remark = dto.Remark?.Trim(),
            IsActive = dto.IsActive ?? true
        };
        var id = await svc.CreateBrandAsync(item);
        return Results.Ok(new { id, code = item.Code, name = item.Name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/brands/{id:int}", async (int id, UpdateBrandDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên thương hiệu." });
    var item = new Brand
    {
        Name = dto.Name.Trim(),
        Origin = dto.Origin?.Trim(),
        Remark = dto.Remark?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateBrandAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/brands/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleBrandStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/brands/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteBrandAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Đơn vị tính hàng hóa / vật tư kho (port từ Mst_PartUnit Skycic)
app.MapGet("/api/part-units", async (string? q, bool? activeOnly, bool? standardOnly, IWmsService svc) =>
{
    var report = await svc.PartUnitsReportAsync(q, activeOnly, standardOnly);
    return Results.Ok(report);
});

app.MapGet("/api/part-units/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetPartUnitAsync(id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy đơn vị tính." });
    return Results.Ok(item);
});

app.MapGet("/api/part-units/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetPartUnitByCodeAsync(code);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy đơn vị tính." });
    return Results.Ok(item);
});

app.MapGet("/api/part-units/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetPartUnitDetailAsync(id);
    if (detail == null) return Results.NotFound(new { error = "Không tìm thấy đơn vị tính." });
    return Results.Ok(detail);
});

app.MapPost("/api/part-units", async (CreatePartUnitDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên đơn vị tính." });
    try
    {
        var item = new PartUnit
        {
            Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
            Name = dto.Name.Trim(),
            IsStandard = dto.IsStandard ?? true,
            Remark = dto.Remark?.Trim(),
            IsActive = dto.IsActive ?? true
        };
        var id = await svc.CreatePartUnitAsync(item);
        return Results.Ok(new { id, code = item.Code, name = item.Name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/part-units/{id:int}", async (int id, UpdatePartUnitDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên đơn vị tính." });
    var item = new PartUnit
    {
        Name = dto.Name.Trim(),
        IsStandard = dto.IsStandard ?? true,
        Remark = dto.Remark?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdatePartUnitAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/part-units/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.TogglePartUnitStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/part-units/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeletePartUnitAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Nhóm chất liệu / Loại vật liệu hàng hóa kho (port từ Mst_PartMaterialType Skycic)
app.MapGet("/api/part-material-types", async (string? q, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.PartMaterialTypesReportAsync(q, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/part-material-types/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetPartMaterialTypeAsync(id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy nhóm chất liệu." });
    return Results.Ok(item);
});

app.MapGet("/api/part-material-types/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetPartMaterialTypeByCodeAsync(code);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy nhóm chất liệu." });
    return Results.Ok(item);
});

app.MapGet("/api/part-material-types/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetPartMaterialTypeDetailAsync(id);
    if (detail == null) return Results.NotFound(new { error = "Không tìm thấy nhóm chất liệu." });
    return Results.Ok(detail);
});

app.MapPost("/api/part-material-types", async (CreatePartMaterialTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên nhóm chất liệu." });
    try
    {
        var item = new PartMaterialType
        {
            Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
            Name = dto.Name.Trim(),
            Remark = dto.Remark?.Trim(),
            IsActive = dto.IsActive ?? true
        };
        var id = await svc.CreatePartMaterialTypeAsync(item);
        return Results.Ok(new { id, code = item.Code, name = item.Name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/part-material-types/{id:int}", async (int id, UpdatePartMaterialTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên nhóm chất liệu." });
    var item = new PartMaterialType
    {
        Name = dto.Name.Trim(),
        Remark = dto.Remark?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdatePartMaterialTypeAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/part-material-types/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.TogglePartMaterialTypeStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/part-material-types/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeletePartMaterialTypeAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Dòng sản phẩm / Model hàng hóa kho (port từ Mst_Model / OS_PrdCenter_Mst_Model Skycic)
app.MapGet("/api/product-models", async (string? q, string? brandCode, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.ProductModelsReportAsync(q, brandCode, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/product-models/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetProductModelAsync(id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy dòng sản phẩm / model." });
    return Results.Ok(item);
});

app.MapGet("/api/product-models/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetProductModelByCodeAsync(code);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy dòng sản phẩm / model." });
    return Results.Ok(item);
});

app.MapGet("/api/product-models/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetProductModelDetailAsync(id);
    if (detail == null) return Results.NotFound(new { error = "Không tìm thấy dòng sản phẩm / model." });
    return Results.Ok(detail);
});

app.MapPost("/api/product-models", async (CreateProductModelDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên dòng sản phẩm / model." });
    try
    {
        var item = new ProductModel
        {
            Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
            Name = dto.Name.Trim(),
            BrandCode = string.IsNullOrWhiteSpace(dto.BrandCode) ? null : dto.BrandCode.Trim().ToUpperInvariant(),
            OrgModelCode = string.IsNullOrWhiteSpace(dto.OrgModelCode) ? null : dto.OrgModelCode.Trim(),
            Remark = dto.Remark?.Trim(),
            IsActive = dto.IsActive ?? true
        };
        var id = await svc.CreateProductModelAsync(item);
        return Results.Ok(new { id, code = item.Code, name = item.Name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/product-models/{id:int}", async (int id, UpdateProductModelDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên dòng sản phẩm / model." });
    var item = new ProductModel
    {
        Name = dto.Name.Trim(),
        BrandCode = string.IsNullOrWhiteSpace(dto.BrandCode) ? null : dto.BrandCode.Trim().ToUpperInvariant(),
        OrgModelCode = string.IsNullOrWhiteSpace(dto.OrgModelCode) ? null : dto.OrgModelCode.Trim(),
        Remark = dto.Remark?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateProductModelAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/product-models/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleProductModelStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/product-models/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteProductModelAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Loại kho / Phân loại kho hàng (port từ Mst_InventoryType Skycic)
app.MapGet("/api/inventory-types", async (string? q, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.InventoryTypesReportAsync(q, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/inventory-types/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetInventoryTypeAsync(id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy loại kho." });
    return Results.Ok(item);
});

app.MapGet("/api/inventory-types/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetInventoryTypeByCodeAsync(code);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy loại kho." });
    return Results.Ok(item);
});

app.MapGet("/api/inventory-types/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetInventoryTypeDetailAsync(id);
    if (detail == null) return Results.NotFound(new { error = "Không tìm thấy loại kho." });
    return Results.Ok(detail);
});

app.MapPost("/api/inventory-types", async (CreateInventoryTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên loại kho." });
    try
    {
        var item = new InventoryType
        {
            Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
            Name = dto.Name.Trim(),
            Remark = dto.Remark?.Trim(),
            IsActive = dto.IsActive ?? true
        };
        var id = await svc.CreateInventoryTypeAsync(item);
        return Results.Ok(new { id, code = item.Code, name = item.Name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/inventory-types/{id:int}", async (int id, UpdateInventoryTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên loại kho." });
    var item = new InventoryType
    {
        Name = dto.Name.Trim(),
        Remark = dto.Remark?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateInventoryTypeAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-types/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleInventoryTypeStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/inventory-types/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteInventoryTypeAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Cấp kho / Phân cấp quản lý kho hàng (port từ Mst_InventoryLevelType Skycic)
app.MapGet("/api/inventory-level-types", async (string? q, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.InventoryLevelTypesReportAsync(q, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/inventory-level-types/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetInventoryLevelTypeAsync(id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy cấp kho." });
    return Results.Ok(item);
});

app.MapGet("/api/inventory-level-types/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetInventoryLevelTypeByCodeAsync(code);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy cấp kho." });
    return Results.Ok(item);
});

app.MapGet("/api/inventory-level-types/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetInventoryLevelTypeDetailAsync(id);
    if (detail == null) return Results.NotFound(new { error = "Không tìm thấy cấp kho." });
    return Results.Ok(detail);
});

app.MapPost("/api/inventory-level-types", async (CreateInventoryLevelTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên cấp kho." });
    try
    {
        var item = new InventoryLevelType
        {
            Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
            Name = dto.Name.Trim(),
            Remark = dto.Remark?.Trim(),
            IsActive = dto.IsActive ?? true
        };
        var id = await svc.CreateInventoryLevelTypeAsync(item);
        return Results.Ok(new { id, code = item.Code, name = item.Name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/inventory-level-types/{id:int}", async (int id, UpdateInventoryLevelTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên cấp kho." });
    var item = new InventoryLevelType
    {
        Name = dto.Name.Trim(),
        Remark = dto.Remark?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateInventoryLevelTypeAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-level-types/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleInventoryLevelTypeStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/inventory-level-types/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteInventoryLevelTypeAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Loại hình & Lý do Nhập kho (port từ Mst_InvInType Skycic)
app.MapGet("/api/inventory-in-types", async (string? q, bool? activeOnly, bool? statisticOnly, IWmsService svc) =>
{
    var report = await svc.InventoryInTypesReportAsync(q, activeOnly, statisticOnly);
    return Results.Ok(report);
});

app.MapGet("/api/inventory-in-types/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetInventoryInTypeAsync(id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy loại nhập kho." });
    return Results.Ok(item);
});

app.MapGet("/api/inventory-in-types/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetInventoryInTypeByCodeAsync(code);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy loại nhập kho." });
    return Results.Ok(item);
});

app.MapGet("/api/inventory-in-types/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetInventoryInTypeDetailAsync(id);
    if (detail == null) return Results.NotFound(new { error = "Không tìm thấy loại nhập kho." });
    return Results.Ok(detail);
});

app.MapPost("/api/inventory-in-types", async (CreateInventoryInTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên loại nhập kho." });
    try
    {
        var item = new InventoryInType
        {
            Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
            Name = dto.Name.Trim(),
            FlagStatistic = dto.FlagStatistic ?? true,
            Remark = dto.Remark?.Trim(),
            IsActive = dto.IsActive ?? true
        };
        var id = await svc.CreateInventoryInTypeAsync(item);
        return Results.Ok(new { id, code = item.Code, name = item.Name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/inventory-in-types/{id:int}", async (int id, UpdateInventoryInTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên loại nhập kho." });
    var item = new InventoryInType
    {
        Name = dto.Name.Trim(),
        FlagStatistic = dto.FlagStatistic ?? true,
        Remark = dto.Remark?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateInventoryInTypeAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-in-types/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleInventoryInTypeStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-in-types/{id:int}/toggle-statistic", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleInventoryInTypeStatisticAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/inventory-in-types/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteInventoryInTypeAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Loại hình & Lý do Xuất kho (port từ Mst_InvOutType Skycic)
app.MapGet("/api/inventory-out-types", async (string? q, bool? activeOnly, bool? statisticOnly, IWmsService svc) =>
{
    var report = await svc.InventoryOutTypesReportAsync(q, activeOnly, statisticOnly);
    return Results.Ok(report);
});

app.MapGet("/api/inventory-out-types/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetInventoryOutTypeAsync(id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy loại xuất kho." });
    return Results.Ok(item);
});

app.MapGet("/api/inventory-out-types/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetInventoryOutTypeByCodeAsync(code);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy loại xuất kho." });
    return Results.Ok(item);
});

app.MapGet("/api/inventory-out-types/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetInventoryOutTypeDetailAsync(id);
    if (detail == null) return Results.NotFound(new { error = "Không tìm thấy loại xuất kho." });
    return Results.Ok(detail);
});

app.MapPost("/api/inventory-out-types", async (CreateInventoryOutTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên loại xuất kho." });
    try
    {
        var item = new InventoryOutType
        {
            Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
            Name = dto.Name.Trim(),
            FlagStatistic = dto.FlagStatistic ?? true,
            Remark = dto.Remark?.Trim(),
            IsActive = dto.IsActive ?? true
        };
        var id = await svc.CreateInventoryOutTypeAsync(item);
        return Results.Ok(new { id, code = item.Code, name = item.Name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/inventory-out-types/{id:int}", async (int id, UpdateInventoryOutTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên loại xuất kho." });
    var item = new InventoryOutType
    {
        Name = dto.Name.Trim(),
        FlagStatistic = dto.FlagStatistic ?? true,
        Remark = dto.Remark?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateInventoryOutTypeAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-out-types/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleInventoryOutTypeStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/inventory-out-types/{id:int}/toggle-statistic", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleInventoryOutTypeStatisticAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/inventory-out-types/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteInventoryOutTypeAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// API Phân quyền người dùng quản lý kho / Gán thủ kho phụ trách (port từ Mst_UserMapInventory Skycic)
app.MapGet("/api/user-map-inventories", async (int? warehouseId, string? userRole, bool? activeOnly, string? q, IWmsService svc) =>
{
    var report = await svc.UserMapInventoriesReportAsync(warehouseId, userRole, activeOnly, q);
    return Results.Ok(report);
});

app.MapGet("/api/user-map-inventories/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetUserMapInventoryAsync(id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy bản ghi phân quyền kho." });
    return Results.Ok(new
    {
        item.Id,
        Warehouse = item.Warehouse.Name,
        WarehouseCode = item.Warehouse.Code,
        item.WarehouseId,
        item.UserCode,
        item.UserName,
        item.UserRole,
        item.Email,
        item.Phone,
        item.IsActive,
        item.Remark,
        item.AssignedBy,
        item.AssignedAt
    });
});

app.MapGet("/api/user-map-inventories/by-warehouse/{warehouseId:int}", async (int warehouseId, IWmsService svc) =>
{
    var list = await svc.GetUserMapsByWarehouseAsync(warehouseId);
    return Results.Ok(list.Select(m => new
    {
        m.Id,
        m.WarehouseId,
        WarehouseCode = m.Warehouse.Code,
        WarehouseName = m.Warehouse.Name,
        m.UserCode,
        m.UserName,
        m.UserRole,
        m.Email,
        m.Phone,
        m.IsActive,
        m.Remark,
        m.AssignedBy,
        m.AssignedAt
    }));
});

app.MapGet("/api/user-map-inventories/by-user/{userCode}", async (string userCode, IWmsService svc) =>
{
    var list = await svc.GetUserMapsByUserCodeAsync(userCode);
    return Results.Ok(list.Select(m => new
    {
        m.Id,
        m.WarehouseId,
        WarehouseCode = m.Warehouse.Code,
        WarehouseName = m.Warehouse.Name,
        m.UserCode,
        m.UserName,
        m.UserRole,
        m.Email,
        m.Phone,
        m.IsActive,
        m.Remark,
        m.AssignedBy,
        m.AssignedAt
    }));
});

app.MapGet("/api/user-map-inventories/warehouse-summaries", async (IWmsService svc) =>
{
    var summaries = await svc.GetWarehouseAssignmentSummariesAsync();
    return Results.Ok(summaries);
});

app.MapPost("/api/user-map-inventories", async (CreateUserMapInventoryDto dto, IWmsService svc) =>
{
    if (dto.WarehouseId <= 0) return Results.BadRequest(new { error = "Cần chọn kho hợp lệ (WarehouseId)." });
    if (string.IsNullOrWhiteSpace(dto.UserCode)) return Results.BadRequest(new { error = "Cần mã tài khoản / mã nhân viên." });
    if (string.IsNullOrWhiteSpace(dto.UserName)) return Results.BadRequest(new { error = "Cần họ và tên nhân sự." });

    try
    {
        var item = new UserMapInventory
        {
            WarehouseId = dto.WarehouseId,
            UserCode = dto.UserCode.Trim().ToLowerInvariant(),
            UserName = dto.UserName.Trim(),
            UserRole = string.IsNullOrWhiteSpace(dto.UserRole) ? "Thủ kho chính" : dto.UserRole.Trim(),
            Email = dto.Email?.Trim(),
            Phone = dto.Phone?.Trim(),
            Remark = dto.Remark?.Trim(),
            IsActive = dto.IsActive ?? true,
            AssignedBy = string.IsNullOrWhiteSpace(dto.AssignedBy) ? "admin" : dto.AssignedBy.Trim(),
            AssignedAt = DateTime.Now
        };
        var id = await svc.CreateUserMapInventoryAsync(item);
        return Results.Ok(new { id, userCode = item.UserCode, userName = item.UserName, warehouseId = item.WarehouseId });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/user-map-inventories/{id:int}", async (int id, UpdateUserMapInventoryDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.UserName)) return Results.BadRequest(new { error = "Cần họ và tên nhân sự." });
    var item = new UserMapInventory
    {
        UserName = dto.UserName.Trim(),
        UserRole = string.IsNullOrWhiteSpace(dto.UserRole) ? "Thủ kho chính" : dto.UserRole.Trim(),
        Email = dto.Email?.Trim(),
        Phone = dto.Phone?.Trim(),
        Remark = dto.Remark?.Trim(),
        IsActive = dto.IsActive
    };
    var (ok, msg) = await svc.UpdateUserMapInventoryAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/user-map-inventories/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleUserMapInventoryStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/user-map-inventories/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteUserMapInventoryAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/user-map-inventories/batch-map", async (BatchMapUserDto dto, IWmsService svc) =>
{
    if (dto.WarehouseId <= 0) return Results.BadRequest(new { error = "Cần chọn kho hợp lệ (WarehouseId)." });
    if (dto.Users == null || dto.Users.Count == 0) return Results.BadRequest(new { error = "Danh sách nhân viên không được rỗng." });

    var (ok, msg, count) = await svc.BatchMapUsersToWarehouseAsync(dto.WarehouseId, dto.Users, dto.AssignedBy ?? "admin");
    return ok ? Results.Ok(new { success = true, message = msg, count }) : Results.BadRequest(new { success = false, message = msg });
});

// API Quản lý Nhóm hàng hóa / Phân nhóm sản phẩm kho (port từ Mst_ProductGroup & Mst_ProductGroupSub Skycic)
app.MapGet("/api/product-groups", async (string? q, string? parentCode, string? brandCode, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.ProductGroupsReportAsync(q, parentCode, brandCode, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/product-groups/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetProductGroupAsync(id);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy nhóm hàng." });
    return Results.Ok(item);
});

app.MapGet("/api/product-groups/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetProductGroupByCodeAsync(code);
    if (item == null) return Results.NotFound(new { error = "Không tìm thấy nhóm hàng." });
    return Results.Ok(item);
});

app.MapGet("/api/product-groups/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetProductGroupDetailAsync(id);
    if (detail == null) return Results.NotFound(new { error = "Không tìm thấy nhóm hàng." });
    return Results.Ok(detail);
});

app.MapGet("/api/product-groups/{id:int}/products", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetProductGroupDetailAsync(id);
    if (detail == null) return Results.NotFound(new { error = "Không tìm thấy nhóm hàng." });
    return Results.Ok(new
    {
        groupCode = detail.Group.Code,
        groupName = detail.Group.Name,
        totalProducts = detail.TotalProducts,
        totalStockQty = detail.TotalStockQty,
        products = detail.Products.Select(p => new { p.Id, p.Code, p.Name, p.Uom, p.CostPrice })
    });
});

app.MapPost("/api/product-groups", async (CreateProductGroupDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên nhóm hàng." });
    try
    {
        var item = new ProductGroup
        {
            Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim().ToUpperInvariant(),
            BrandCode = string.IsNullOrWhiteSpace(dto.BrandCode) ? null : dto.BrandCode.Trim().ToUpperInvariant(),
            IsActive = dto.IsActive ?? true
        };
        var id = await svc.CreateProductGroupAsync(item);
        return Results.Ok(new { id, code = item.Code, name = item.Name });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/product-groups/{id:int}", async (int id, UpdateProductGroupDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần tên nhóm hàng." });
    var item = new ProductGroup
    {
        Name = dto.Name.Trim(),
        Description = dto.Description?.Trim(),
        ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim().ToUpperInvariant(),
        BrandCode = string.IsNullOrWhiteSpace(dto.BrandCode) ? null : dto.BrandCode.Trim().ToUpperInvariant(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateProductGroupAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/product-groups/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleProductGroupStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/product-groups/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteProductGroupAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapGet("/api/areas", async (string? q, string? parentCode, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.AreasReportAsync(q, parentCode, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/areas/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetAreaAsync(id);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy vùng / khu vực." });
});

app.MapGet("/api/areas/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetAreaByCodeAsync(code);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy vùng / khu vực." });
});

app.MapGet("/api/areas/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetAreaDetailAsync(id);
    return detail != null ? Results.Ok(detail) : Results.NotFound(new { error = "Không tìm thấy vùng / khu vực." });
});

app.MapPost("/api/areas", async (CreateAreaDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Cần tên vùng / khu vực." });

    var item = new Area
    {
        Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
        Name = dto.Name.Trim(),
        Description = dto.Description?.Trim(),
        ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim().ToUpperInvariant(),
        IsActive = dto.IsActive ?? true
    };
    try
    {
        var id = await svc.CreateAreaAsync(item);
        return Results.Ok(new { success = true, id, code = item.Code });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/areas/{id:int}", async (int id, UpdateAreaDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Cần tên vùng / khu vực." });

    var item = new Area
    {
        Name = dto.Name.Trim(),
        Description = dto.Description?.Trim(),
        ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim().ToUpperInvariant(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateAreaAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/areas/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleAreaStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/areas/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteAreaAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapGet("/api/customer-groups", async (string? q, string? parentCode, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.CustomerGroupsReportAsync(q, parentCode, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/customer-groups/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetCustomerGroupAsync(id);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy nhóm khách hàng." });
});

app.MapGet("/api/customer-groups/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetCustomerGroupByCodeAsync(code);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy nhóm khách hàng." });
});

app.MapGet("/api/customer-groups/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetCustomerGroupDetailAsync(id);
    return detail != null ? Results.Ok(detail) : Results.NotFound(new { error = "Không tìm thấy nhóm khách hàng." });
});

app.MapPost("/api/customer-groups", async (CreateCustomerGroupDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { success = false, message = "Cần tên nhóm khách hàng." });

    var item = new CustomerGroup
    {
        Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
        Name = dto.Name.Trim(),
        Description = dto.Description?.Trim(),
        ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim().ToUpperInvariant(),
        IsActive = dto.IsActive ?? true
    };
    try
    {
        var id = await svc.CreateCustomerGroupAsync(item);
        return Results.Ok(new { success = true, id, code = item.Code, message = $"Đã tạo mới nhóm khách hàng '{item.Name}' ({item.Code}) thành công." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
});

app.MapPut("/api/customer-groups/{id:int}", async (int id, UpdateCustomerGroupDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { success = false, message = "Cần tên nhóm khách hàng." });

    var item = new CustomerGroup
    {
        Name = dto.Name.Trim(),
        Description = dto.Description?.Trim(),
        ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim().ToUpperInvariant(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateCustomerGroupAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/customer-groups/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleCustomerGroupStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/customer-groups/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteCustomerGroupAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// ==================== BỘ PHẬN / PHÒNG BAN QUẢN LÝ KHO (Mst_Department Skycic) ====================
app.MapGet("/api/departments", async (string? q, string? parentCode, int? level, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.DepartmentsReportAsync(q, parentCode, level, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/departments/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetDepartmentAsync(id);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy bộ phận / phòng ban." });
});

app.MapGet("/api/departments/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetDepartmentByCodeAsync(code);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy bộ phận / phòng ban." });
});

app.MapGet("/api/departments/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetDepartmentDetailAsync(id);
    return detail != null ? Results.Ok(detail) : Results.NotFound(new { error = "Không tìm thấy bộ phận / phòng ban." });
});

app.MapPost("/api/departments", async (CreateDepartmentDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Vui lòng nhập tên bộ phận / phòng ban." });

    var item = new Department
    {
        Code = dto.Code?.Trim() ?? "",
        Name = dto.Name.Trim(),
        ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim().ToUpperInvariant(),
        BUCode = string.IsNullOrWhiteSpace(dto.BUCode) ? null : dto.BUCode.Trim().ToUpperInvariant(),
        Level = dto.Level ?? 1,
        MST = string.IsNullOrWhiteSpace(dto.MST) ? null : dto.MST.Trim(),
        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
        IsActive = dto.IsActive ?? true
    };
    try
    {
        var id = await svc.CreateDepartmentAsync(item);
        return Results.Created($"/api/departments/{id}", item);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/departments/{id:int}", async (int id, UpdateDepartmentDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Vui lòng nhập tên bộ phận / phòng ban." });

    var item = new Department
    {
        Name = dto.Name.Trim(),
        ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim().ToUpperInvariant(),
        BUCode = string.IsNullOrWhiteSpace(dto.BUCode) ? null : dto.BUCode.Trim().ToUpperInvariant(),
        Level = dto.Level ?? 1,
        MST = string.IsNullOrWhiteSpace(dto.MST) ? null : dto.MST.Trim(),
        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateDepartmentAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/departments/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleDepartmentStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/departments/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteDepartmentAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// ==================== NGUỒN KHÁCH HÀNG & KÊNH TIẾP NHẬN KHO (Mst_CustomerSource Skycic) ====================
app.MapGet("/api/customer-sources", async (string? q, string? parentCode, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.CustomerSourcesReportAsync(q, parentCode, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/customer-sources/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetCustomerSourceAsync(id);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy nguồn khách hàng." });
});

app.MapGet("/api/customer-sources/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetCustomerSourceByCodeAsync(code);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy nguồn khách hàng." });
});

app.MapGet("/api/customer-sources/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetCustomerSourceDetailAsync(id);
    return detail != null ? Results.Ok(detail) : Results.NotFound(new { error = "Không tìm thấy nguồn khách hàng." });
});

app.MapPost("/api/customer-sources", async (CreateCustomerSourceDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Cần tên nguồn khách hàng." });

    var item = new CustomerSource
    {
        Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
        Name = dto.Name.Trim(),
        ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim().ToUpperInvariant(),
        BUCode = string.IsNullOrWhiteSpace(dto.BUCode) ? null : dto.BUCode.Trim().ToUpperInvariant(),
        Description = dto.Description?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    try
    {
        var id = await svc.CreateCustomerSourceAsync(item);
        return Results.Created($"/api/customer-sources/{id}", item);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/customer-sources/{id:int}", async (int id, UpdateCustomerSourceDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Vui lòng nhập tên nguồn khách hàng." });

    var item = new CustomerSource
    {
        Name = dto.Name.Trim(),
        ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim().ToUpperInvariant(),
        BUCode = string.IsNullOrWhiteSpace(dto.BUCode) ? null : dto.BUCode.Trim().ToUpperInvariant(),
        Description = dto.Description?.Trim(),
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateCustomerSourceAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/customer-sources/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleCustomerSourceStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/customer-sources/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteCustomerSourceAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// ==================== LOẠI HÌNH & MỤC ĐÍCH ĐIỀU CHUYỂN KHO (Mst_MoveOrdType Skycic) ====================
app.MapGet("/api/move-ord-types", async (string? q, bool? activeOnly, bool? urgentOnly, IWmsService svc) =>
{
    var report = await svc.MoveOrdTypesReportAsync(q, activeOnly, urgentOnly);
    return Results.Ok(report);
});

app.MapGet("/api/move-ord-types/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetMoveOrdTypeAsync(id);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy loại điều chuyển." });
});

app.MapGet("/api/move-ord-types/by-code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetMoveOrdTypeByCodeAsync(code);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy loại điều chuyển." });
});

app.MapGet("/api/move-ord-types/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetMoveOrdTypeDetailAsync(id);
    return detail != null ? Results.Ok(detail) : Results.NotFound(new { error = "Không tìm thấy loại điều chuyển." });
});

app.MapPost("/api/move-ord-types", async (CreateMoveOrdTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Cần tên loại hình điều chuyển." });

    var item = new MoveOrdType
    {
        Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
        Name = dto.Name.Trim(),
        Description = dto.Description?.Trim(),
        IsUrgent = dto.IsUrgent ?? false,
        IsActive = dto.IsActive ?? true
    };
    try
    {
        var id = await svc.CreateMoveOrdTypeAsync(item);
        return Results.Created($"/api/move-ord-types/{id}", item);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/move-ord-types/{id:int}", async (int id, UpdateMoveOrdTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Vui lòng nhập tên loại điều chuyển." });

    var item = new MoveOrdType
    {
        Name = dto.Name.Trim(),
        Description = dto.Description?.Trim(),
        IsUrgent = dto.IsUrgent ?? false,
        IsActive = dto.IsActive ?? true
    };
    var (ok, msg) = await svc.UpdateMoveOrdTypeAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/move-ord-types/{id:int}/toggle", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleMoveOrdTypeStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/move-ord-types/{id:int}/toggle-urgent", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleMoveOrdTypeUrgentAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/move-ord-types/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteMoveOrdTypeAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// ==================== QUẢN LÝ ĐẠI LÝ PHÂN PHỐI & MẠNG LƯỚI ĐIỂM BÁN KHO (Mst_Dealer Skycic) ====================
app.MapGet("/api/dealers", async (string? q, int? level, string? province, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.DealersReportAsync(q, level, province, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/dealers/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetDealerAsync(id);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy đại lý phân phối." });
});

app.MapGet("/api/dealers/code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetDealerByCodeAsync(code);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy đại lý phân phối." });
});

app.MapGet("/api/dealers/{id:int}/detail", async (int id, IWmsService svc) =>
{
    var detail = await svc.GetDealerDetailAsync(id);
    return detail != null ? Results.Ok(detail) : Results.NotFound(new { error = "Không tìm thấy đại lý phân phối." });
});

app.MapPost("/api/dealers", async (CreateDealerDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Tên đại lý phân phối không được để trống." });

    var item = new Dealer
    {
        Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
        Name = dto.Name.Trim(),
        ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim().ToUpperInvariant(),
        Level = dto.Level > 0 ? dto.Level : 1,
        DealerType = string.IsNullOrWhiteSpace(dto.DealerType) ? "Đại lý phân phối" : dto.DealerType.Trim(),
        BUCode = string.IsNullOrWhiteSpace(dto.BUCode) ? null : dto.BUCode.Trim().ToUpperInvariant(),
        ProvinceCode = string.IsNullOrWhiteSpace(dto.ProvinceCode) ? null : dto.ProvinceCode.Trim(),
        Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
        PresentBy = string.IsNullOrWhiteSpace(dto.PresentBy) ? null : dto.PresentBy.Trim(),
        GovIdNumber = string.IsNullOrWhiteSpace(dto.GovIdNumber) ? null : dto.GovIdNumber.Trim(),
        Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
        Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
        WarehouseId = dto.WarehouseId,
        IsActive = dto.IsActive ?? true,
        Remark = string.IsNullOrWhiteSpace(dto.Remark) ? null : dto.Remark.Trim()
    };

    try
    {
        var id = await svc.CreateDealerAsync(item);
        return Results.Created($"/api/dealers/{id}", item);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/dealers/{id:int}", async (int id, UpdateDealerDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Tên đại lý phân phối không được để trống." });

    var item = new Dealer
    {
        Name = dto.Name.Trim(),
        ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim().ToUpperInvariant(),
        Level = dto.Level > 0 ? dto.Level : 1,
        DealerType = string.IsNullOrWhiteSpace(dto.DealerType) ? "Đại lý phân phối" : dto.DealerType.Trim(),
        BUCode = string.IsNullOrWhiteSpace(dto.BUCode) ? null : dto.BUCode.Trim().ToUpperInvariant(),
        ProvinceCode = string.IsNullOrWhiteSpace(dto.ProvinceCode) ? null : dto.ProvinceCode.Trim(),
        Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
        PresentBy = string.IsNullOrWhiteSpace(dto.PresentBy) ? null : dto.PresentBy.Trim(),
        GovIdNumber = string.IsNullOrWhiteSpace(dto.GovIdNumber) ? null : dto.GovIdNumber.Trim(),
        Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
        Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
        WarehouseId = dto.WarehouseId,
        IsActive = dto.IsActive ?? true,
        Remark = string.IsNullOrWhiteSpace(dto.Remark) ? null : dto.Remark.Trim()
    };
    var (ok, msg) = await svc.UpdateDealerAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/dealers/{id:int}/toggle-status", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleDealerStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/dealers/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteDealerAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

// ==================== BẢN ĐỒ TIẾN ĐỘ LỆNH GIAO HÀNG THEO PHIẾU XUẤT KHO (Rpt_MapDeliveryOrder_ByInvFIOut Skycic) ====================
app.MapGet("/api/reports/map-delivery-order", async (
    int? warehouseId,
    string? areaCode,
    string? customerCode,
    string? status,
    DateTime? fromDate,
    DateTime? toDate,
    string? q,
    IWmsService svc) =>
{
    var report = await svc.MapDeliveryOrderReportAsync(warehouseId, areaCode, customerCode, status, fromDate, toDate, q);
    return Results.Ok(report);
});

app.MapGet("/api/reports/map-delivery-order/kpis", async (
    int? warehouseId,
    string? areaCode,
    string? customerCode,
    string? status,
    DateTime? fromDate,
    DateTime? toDate,
    IWmsService svc) =>
{
    var report = await svc.MapDeliveryOrderReportAsync(warehouseId, areaCode, customerCode, status, fromDate, toDate, null);
    return Results.Ok(new
    {
        report.TotalDeliveryOrders,
        report.CompletedOrders,
        report.PendingOrders,
        report.DelayedOrders,
        report.OnTimeRatePercent,
        report.TotalDispatchedQty,
        DateFrom = report.DateFrom.ToString("yyyy-MM-dd"),
        DateTo = report.DateTo.ToString("yyyy-MM-dd"),
        report.TodayStr
    });
});

app.MapGet("/api/reports/map-delivery-order/area-summary", async (
    int? warehouseId,
    DateTime? fromDate,
    DateTime? toDate,
    IWmsService svc) =>
{
    var report = await svc.MapDeliveryOrderReportAsync(warehouseId, null, null, null, fromDate, toDate, null);
    return Results.Ok(report.AreaSummaries);
});

app.MapGet("/api/reports/map-delivery-order/export-csv", async (
    int? warehouseId,
    string? areaCode,
    string? customerCode,
    string? status,
    DateTime? fromDate,
    DateTime? toDate,
    string? q,
    IWmsService svc) =>
{
    var report = await svc.MapDeliveryOrderReportAsync(warehouseId, areaCode, customerCode, status, fromDate, toDate, q);
    var sb = new System.Text.StringBuilder();
    sb.Append('\uFEFF');

    sb.AppendLine("BẢN ĐỒ TIẾN ĐỘ LỆNH GIAO HÀNG THEO PHIẾU XUẤT KHO (RPT_MAPDELIVERYORDER_BYINVFIOUT)");
    sb.AppendLine($"Kho xuất:;{report.WarehouseName};Dải ngày:;{report.DateFrom:dd/MM/yyyy} - {report.DateTo:dd/MM/yyyy};Hôm nay:;{DateTime.Today:dd/MM/yyyy}");
    sb.AppendLine($"Tổng số lệnh:;{report.TotalDeliveryOrders};Đã giao:;{report.CompletedOrders};Chờ giao:;{report.PendingOrders};Giao chậm:;{report.DelayedOrders};Tỷ lệ đúng hạn:;{report.OnTimeRatePercent}%;Tổng SL:;{report.TotalDispatchedQty}");
    sb.AppendLine();

    var headerCols = new List<string> { "STT", "Khu vực", "Mã KH", "Tên khách hàng", "Số phiếu xuất", "Loại", "Ngày xuất", "Mã hàng", "Tên hàng", "ĐVT", "Tổng SL", "Trạng thái", "Cảnh báo" };
    headerCols.AddRange(report.ListDates);
    sb.AppendLine(string.Join(";", headerCols));

    int stt = 1;
    foreach (var r in report.Rows)
    {
        var rowCols = new List<string>
        {
            (stt++).ToString(),
            $"\"{r.AreaName}\"",
            $"\"{r.CustomerCode}\"",
            $"\"{r.CustomerName.Replace("\"", "\"\"")}\"",
            $"\"{r.DeliveryOrderNo}\"",
            $"\"{r.DocTypeLabel}\"",
            r.OrderDateDisplay,
            $"\"{r.ProductCode}\"",
            $"\"{r.ProductName.Replace("\"", "\"\"")}\"",
            $"\"{r.Uom}\"",
            r.TotalQty.ToString(),
            $"\"{r.StatusLabel}\"",
            r.IsDelayed ? "CHẬM TIẾN ĐỘ" : "Đúng hạn"
        };
        foreach (var d in report.ListDates)
        {
            r.DailyQuantities.TryGetValue(d, out var qVal);
            rowCols.Add(qVal > 0 ? qVal.ToString() : "");
        }
        sb.AppendLine(string.Join(";", rowCols));
    }

    var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    return Results.File(bytes, "text/csv; charset=utf-8", $"BanDoGiaoHang_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
});

// ==================== QUẢN LÝ BIỂU MẪU IN KHO & THIẾT KẾ TEM NHÃN (InvF_TempPrint & Mst_TempPrintType Skycic) ====================
app.MapGet("/api/temp-prints", async (string? q, string? typeCode, bool? activeOnly, IWmsService svc) =>
{
    var report = await svc.TempPrintsReportAsync(q, typeCode, activeOnly);
    return Results.Ok(report);
});

app.MapGet("/api/temp-prints/{id:int}", async (int id, IWmsService svc) =>
{
    var item = await svc.GetTempPrintAsync(id);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy mẫu in." });
});

app.MapGet("/api/temp-prints/code/{code}", async (string code, IWmsService svc) =>
{
    var item = await svc.GetTempPrintByCodeAsync(code);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = "Không tìm thấy mẫu in." });
});

app.MapGet("/api/temp-prints/default/{typeCode}", async (string typeCode, IWmsService svc) =>
{
    var item = await svc.GetDefaultTempPrintByTypeAsync(typeCode);
    return item != null ? Results.Ok(item) : Results.NotFound(new { error = $"Không tìm thấy mẫu in mặc định cho loại {typeCode}." });
});

app.MapGet("/api/temp-prints/{id:int}/preview", async (int id, IWmsService svc) =>
{
    var preview = await svc.PreviewTempPrintAsync(id);
    return preview != null ? Results.Ok(preview) : Results.NotFound(new { error = "Không tìm thấy mẫu in." });
});

app.MapPost("/api/temp-prints", async (CreateTempPrintDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.HeaderTitle))
        return Results.BadRequest(new { error = "Tên mẫu in và tiêu đề biểu mẫu không được để trống." });

    var item = new TempPrint
    {
        Code = dto.Code?.Trim().ToUpperInvariant() ?? "",
        Name = dto.Name.Trim(),
        TypeCode = string.IsNullOrWhiteSpace(dto.TypeCode) ? "IN" : dto.TypeCode.Trim().ToUpperInvariant(),
        PaperSize = string.IsNullOrWhiteSpace(dto.PaperSize) ? "A4_Portrait" : dto.PaperSize.Trim(),
        UnitName = string.IsNullOrWhiteSpace(dto.UnitName) ? "CÔNG TY CỔ PHẦN LOGISTICS MINIWMS" : dto.UnitName.Trim(),
        UnitAddress = dto.UnitAddress?.Trim(),
        UnitPhone = dto.UnitPhone?.Trim(),
        UnitEmail = dto.UnitEmail?.Trim(),
        HeaderTitle = dto.HeaderTitle.Trim(),
        SubTitle = dto.SubTitle?.Trim(),
        BodyTemplateHtml = dto.BodyTemplateHtml ?? "",
        NoteFooter = dto.NoteFooter?.Trim(),
        IsDefault = dto.IsDefault ?? false,
        IsActive = dto.IsActive ?? true,
        Remark = dto.Remark?.Trim()
    };

    try
    {
        var id = await svc.CreateTempPrintAsync(item);
        return Results.Created($"/api/temp-prints/{id}", item);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/temp-prints/{id:int}", async (int id, UpdateTempPrintDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.HeaderTitle))
        return Results.BadRequest(new { error = "Tên mẫu in và tiêu đề biểu mẫu không được để trống." });

    var item = new TempPrint
    {
        Name = dto.Name.Trim(),
        TypeCode = string.IsNullOrWhiteSpace(dto.TypeCode) ? "IN" : dto.TypeCode.Trim().ToUpperInvariant(),
        PaperSize = string.IsNullOrWhiteSpace(dto.PaperSize) ? "A4_Portrait" : dto.PaperSize.Trim(),
        UnitName = string.IsNullOrWhiteSpace(dto.UnitName) ? "CÔNG TY CỔ PHẦN LOGISTICS MINIWMS" : dto.UnitName.Trim(),
        UnitAddress = dto.UnitAddress?.Trim(),
        UnitPhone = dto.UnitPhone?.Trim(),
        UnitEmail = dto.UnitEmail?.Trim(),
        HeaderTitle = dto.HeaderTitle.Trim(),
        SubTitle = dto.SubTitle?.Trim(),
        BodyTemplateHtml = dto.BodyTemplateHtml ?? "",
        NoteFooter = dto.NoteFooter?.Trim(),
        IsDefault = dto.IsDefault ?? false,
        IsActive = dto.IsActive ?? true,
        Remark = dto.Remark?.Trim()
    };

    var (ok, msg) = await svc.UpdateTempPrintAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/temp-prints/{id:int}/toggle-status", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.ToggleTempPrintStatusAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapPost("/api/temp-prints/{id:int}/set-default", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.SetDefaultTempPrintAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/temp-prints/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteTempPrintAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapGet("/api/temp-print-types", async (bool? activeOnly, IWmsService svc) =>
{
    var list = await svc.TempPrintTypesAsync(activeOnly);
    return Results.Ok(list);
});

app.MapPost("/api/temp-print-types", async (CreateTempPrintTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Mã và tên loại mẫu in không được để trống." });

    var item = new TempPrintType
    {
        Code = dto.Code.Trim().ToUpperInvariant(),
        Name = dto.Name.Trim(),
        GroupCode = string.IsNullOrWhiteSpace(dto.GroupCode) ? "DOC" : dto.GroupCode.Trim().ToUpperInvariant(),
        Description = dto.Description?.Trim(),
        IsActive = dto.IsActive ?? true
    };

    try
    {
        var id = await svc.CreateTempPrintTypeAsync(item);
        return Results.Created($"/api/temp-print-types/{id}", item);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/api/temp-print-types/{id:int}", async (int id, UpdateTempPrintTypeDto dto, IWmsService svc) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Tên loại mẫu in không được để trống." });

    var item = new TempPrintType
    {
        Name = dto.Name.Trim(),
        GroupCode = string.IsNullOrWhiteSpace(dto.GroupCode) ? "DOC" : dto.GroupCode.Trim().ToUpperInvariant(),
        Description = dto.Description?.Trim(),
        IsActive = dto.IsActive ?? true
    };

    var (ok, msg) = await svc.UpdateTempPrintTypeAsync(id, item);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapDelete("/api/temp-print-types/{id:int}", async (int id, IWmsService svc) =>
{
    var (ok, msg) = await svc.DeleteTempPrintTypeAsync(id);
    return ok ? Results.Ok(new { success = true, message = msg }) : Results.BadRequest(new { success = false, message = msg });
});

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

record RegisterOrgDto(string Name);
record CreateMoveOrderDto(int FromWarehouseId, int ToWarehouseId, string? Note, List<MoveOrderItemDto> Lines, string? MoveOrdTypeCode = null);
record MoveOrderItemDto(int ProductId, int Quantity, string? Note);
record CreateReturnSupDto(int WarehouseId, string SupplierName, string? SupplierCode, string? RefDocNo, string? Reason, List<ReturnSupItemDto> Lines);
record ReturnSupItemDto(int ProductId, int Quantity, decimal UnitPrice, string? Note);
record CreateCustomerReturnDto(int WarehouseId, string CustomerName, string? CustomerCode, string? InvoiceNo, string? RefOrderNo, string? Reason, List<CustomerReturnItemDto> Lines);
record CustomerReturnItemDto(int ProductId, int Quantity, decimal UnitPrice, string? Note);
record CreateStockSerialDto(int WarehouseId, int ProductId, string SerialNo, string? LotNo, string? RefNo, string? Note, StockSerialStatus? Status);
record ChangeSerialStatusDto(StockSerialStatus Status, string? Note);
record CreateInventoryBlockDto(int WarehouseId, string InvBlockCode, string ShelfCode, string? InvBlockDesc, double Length, double Width, double Height, int MaxCapacity, string? Remark, bool? FlagActive);
record UpdateInventoryBlockDto(string? ShelfCode, string? InvBlockDesc, double Length, double Width, double Height, int MaxCapacity, string? Remark, bool FlagActive);
record CreateCostPriceDto(int? WarehouseId, int ProductId, decimal CostPrice, DateTime? EffectDate, string? RefDocNo, string? CalcPeriodName, bool? IsCurrent, string? Remark);
record UpdateCostPriceDto(decimal CostPrice, string? Remark);
record CalcCostPricePreviewDto(int? WarehouseId, DateTime? FromDate, DateTime? ToDate, string? CalcPeriodName, int[]? ProductIds);
record ApplyCostPriceCalcDto(int? WarehouseId, DateTime? EffectDate, string? CalcPeriodName, List<ApplyCostPriceItemDto> Items);
record ApplyCostPriceItemDto(int ProductId, decimal NewCostPrice, string? Note);
record CreatePeriodClosingDto(int? WarehouseId, int Year, int Month, string? Note, string? ClosedBy);
record PreviewPeriodClosingDto(int? WarehouseId, int Year, int Month);
record ReopenPeriodClosingDto(string? Reason);
record CreateCartonDto(int WarehouseId, string? CartonCode, string? CartonType, double LengthCm, double WidthCm, double HeightCm, int Capacity, string? ShelfLocation, string? Remark);
record GenerateCartonBatchDto(int WarehouseId, string? CartonType, int Count, double LengthCm, double WidthCm, double HeightCm, int Capacity, string? ShelfLocation, string? Prefix);
record PackCartonDto(int ProductId, int Quantity, string? LotNo, double GrossWeightKg, string? PackerName, string? Note);
record ShipCartonDto(string RefDocNo);
record UnpackCartonDto(string? Reason);
record CreateBoxDto(int WarehouseId, string? BoxCode, string? BoxType, double LengthCm, double WidthCm, double HeightCm, int Capacity, int? CartonId, string? ShelfLocation, string? Remark);
record GenerateBoxBatchDto(int WarehouseId, string? BoxType, int Count, double LengthCm, double WidthCm, double HeightCm, int Capacity, int? CartonId, string? ShelfLocation, string? Prefix);
record PackBoxDto(int ProductId, int Quantity, string? LotNo, double GrossWeightKg, string? PackerName, string? SecretNo, string? Note);
record SealBoxDto(string? SecretNo);
record MapBoxCartonDto(int CartonId);
record ShipBoxDto(string RefDocNo);
record UnpackBoxDto(string? Reason);
record CreateInventoryInFGDto(int WarehouseId, InvInFGFormType FormType, string WorkshopName, string? WorkOrderNo, string? ShiftLeader, DateTime? Date, string? Remark, List<InventoryInFGLineDto> Lines, List<InventoryInFGSerialDto>? Serials);
record InventoryInFGLineDto(int ProductId, int PlanQty, int ActualQty, int DefectQty, decimal UnitCost, string? Note);
record InventoryInFGSerialDto(int ProductId, string SerialNo, string? Note);
record CreateInventoryOutFGDto(int WarehouseId, InvOutFGType OutType, InvOutFGFormType FormType, string CustomerName, string? AgentCode, string? DeliveryAddress, string? DriverName, string? DriverPhone, string? PlateNo, string? MoocNo, string? OrderNo, DateTime? Date, string? Remark, List<InventoryOutFGLineDto> Lines, List<InventoryOutFGSerialDto>? Serials);
record InventoryOutFGLineDto(int ProductId, int Qty, decimal UnitPrice, decimal UnitCost, string? Note);
record InventoryOutFGSerialDto(int ProductId, string SerialNo, string? Note);
record CreateSupplierDto(string? Code, string Name, string? ContactName, string? Phone, string? Email, string? Address, string? TaxCode, string? Note, bool? IsActive);
record UpdateSupplierDto(string Name, string? ContactName, string? Phone, string? Email, string? Address, string? TaxCode, string? Note, bool? IsActive);
record CreateCustomerDto(string? Code, string Name, string? CustomerType, string? ContactName, string? ContactPhone, string? Phone, string? Email, string? Address, string? Province, string? AreaCode, string? CustomerGrpCode, string? CustomerSourceCode, string? TaxCode, string? Note, bool? IsActive);
record UpdateCustomerDto(string Name, string? CustomerType, string? ContactName, string? ContactPhone, string? Phone, string? Email, string? Address, string? Province, string? AreaCode, string? CustomerGrpCode, string? CustomerSourceCode, string? TaxCode, string? Note, bool? IsActive);
record CreatePartTypeDto(string? Code, string Name, string? Remark, bool? IsActive);
record UpdatePartTypeDto(string Name, string? Remark, bool? IsActive);
record CreateBrandDto(string? Code, string Name, string? Origin, string? Remark, bool? IsActive);
record UpdateBrandDto(string Name, string? Origin, string? Remark, bool? IsActive);
record CreatePartUnitDto(string? Code, string Name, bool? IsStandard, string? Remark, bool? IsActive);
record UpdatePartUnitDto(string Name, bool? IsStandard, string? Remark, bool? IsActive);
record CreatePartMaterialTypeDto(string? Code, string Name, string? Remark, bool? IsActive);
record UpdatePartMaterialTypeDto(string Name, string? Remark, bool? IsActive);
record CreateProductModelDto(string? Code, string Name, string? BrandCode, string? OrgModelCode, string? Remark, bool? IsActive);
record UpdateProductModelDto(string Name, string? BrandCode, string? OrgModelCode, string? Remark, bool? IsActive);
record CreateInventoryTypeDto(string? Code, string Name, string? Remark, bool? IsActive);
record UpdateInventoryTypeDto(string Name, string? Remark, bool? IsActive);
record CreateInventoryLevelTypeDto(string? Code, string Name, string? Remark, bool? IsActive);
record UpdateInventoryLevelTypeDto(string Name, string? Remark, bool? IsActive);
record CreateInventoryInTypeDto(string? Code, string Name, bool? FlagStatistic, string? Remark, bool? IsActive);
record UpdateInventoryInTypeDto(string Name, bool? FlagStatistic, string? Remark, bool? IsActive);
record CreateInventoryOutTypeDto(string? Code, string Name, bool? FlagStatistic, string? Remark, bool? IsActive);
record UpdateInventoryOutTypeDto(string Name, bool? FlagStatistic, string? Remark, bool? IsActive);
record CreateUserMapInventoryDto(int WarehouseId, string UserCode, string UserName, string? UserRole, string? Email, string? Phone, string? Remark, bool? IsActive, string? AssignedBy);
record UpdateUserMapInventoryDto(string UserName, string? UserRole, string? Email, string? Phone, string? Remark, bool IsActive);
record BatchMapUserDto(int WarehouseId, List<BatchMapUserItemDto> Users, string? AssignedBy);
record CreateProductGroupDto(string? Code, string Name, string? Description, string? ParentCode, string? BrandCode, bool? IsActive);
record UpdateProductGroupDto(string Name, string? Description, string? ParentCode, string? BrandCode, bool? IsActive);
record CreateAreaDto(string? Code, string Name, string? Description, string? ParentCode, bool? IsActive);
record UpdateAreaDto(string Name, string? Description, string? ParentCode, bool? IsActive);
record CreateCustomerGroupDto(string? Code, string Name, string? Description, string? ParentCode, bool? IsActive);
record UpdateCustomerGroupDto(string Name, string? Description, string? ParentCode, bool? IsActive);
record CreateDepartmentDto(string? Code, string Name, string? ParentCode, string? BUCode, int? Level, string? MST, string? Description, bool? IsActive);
record UpdateDepartmentDto(string Name, string? ParentCode, string? BUCode, int? Level, string? MST, string? Description, bool? IsActive);
record CreateCustomerSourceDto(string? Code, string Name, string? ParentCode, string? BUCode, string? Description, bool? IsActive);
record UpdateCustomerSourceDto(string Name, string? ParentCode, string? BUCode, string? Description, bool? IsActive);
record CreateMoveOrdTypeDto(string? Code, string Name, string? Description, bool? IsUrgent, bool? IsActive);
record UpdateMoveOrdTypeDto(string Name, string? Description, bool? IsUrgent, bool? IsActive);
record CreateDealerDto(string? Code, string Name, string? ParentCode, int Level, string? DealerType, string? BUCode, string? ProvinceCode, string? Address, string? PresentBy, string? GovIdNumber, string? Email, string? Phone, int? WarehouseId, bool? IsActive, string? Remark);
record UpdateDealerDto(string Name, string? ParentCode, int Level, string? DealerType, string? BUCode, string? ProvinceCode, string? Address, string? PresentBy, string? GovIdNumber, string? Email, string? Phone, int? WarehouseId, bool? IsActive, string? Remark);
record CreateTempPrintDto(string? Code, string Name, string TypeCode, string? PaperSize, string? UnitName, string? UnitAddress, string? UnitPhone, string? UnitEmail, string HeaderTitle, string? SubTitle, string BodyTemplateHtml, string? NoteFooter, bool? IsDefault, bool? IsActive, string? Remark);
record UpdateTempPrintDto(string Name, string TypeCode, string? PaperSize, string? UnitName, string? UnitAddress, string? UnitPhone, string? UnitEmail, string HeaderTitle, string? SubTitle, string BodyTemplateHtml, string? NoteFooter, bool? IsDefault, bool? IsActive, string? Remark);
record CreateTempPrintTypeDto(string? Code, string Name, string? GroupCode, string? Description, bool? IsActive);
record UpdateTempPrintTypeDto(string Name, string? GroupCode, string? Description, bool? IsActive);


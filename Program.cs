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

app.MapPost("/api/orgs/register", async (RegisterOrgDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Name)) return Results.BadRequest(new { error = "Cần Name." });
    var org = new Org { Name = dto.Name.Trim(), ApiKey = "wms_" + Guid.NewGuid().ToString("N") };
    db.Orgs.Add(org); await db.SaveChangesAsync();
    return Results.Ok(new { orgId = org.Id, apiKey = org.ApiKey });
});

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

record RegisterOrgDto(string Name);
record CreateMoveOrderDto(int FromWarehouseId, int ToWarehouseId, string? Note, List<MoveOrderItemDto> Lines);
record MoveOrderItemDto(int ProductId, int Quantity, string? Note);
record CreateReturnSupDto(int WarehouseId, string SupplierName, string? SupplierCode, string? RefDocNo, string? Reason, List<ReturnSupItemDto> Lines);
record ReturnSupItemDto(int ProductId, int Quantity, decimal UnitPrice, string? Note);
record CreateCustomerReturnDto(int WarehouseId, string CustomerName, string? CustomerCode, string? InvoiceNo, string? RefOrderNo, string? Reason, List<CustomerReturnItemDto> Lines);
record CustomerReturnItemDto(int ProductId, int Quantity, decimal UnitPrice, string? Note);

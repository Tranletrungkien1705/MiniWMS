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

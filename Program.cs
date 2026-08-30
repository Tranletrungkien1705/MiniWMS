using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models;
using MiniWMS.Services;
using Serilog;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
FleetObs.ConfigureLogger("miniwms");

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
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
builder.Services.AddHttpClient();   // gọi MiniTrace khi ghi sổ phiếu
builder.Services.AddFleetObs();
builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await Seeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

app.UseFleetObs();

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

// API tích hợp: MiniDMS giao đơn → xuất kho theo mã SP (chung mã danh mục PIM). Best-effort.
app.MapPost("/api/ext/issue", async (IssueDto dto, IWmsService svc, MiniWMS.Data.AppDbContext db) =>
{
    var wh = await db.Warehouses.OrderBy(w => w.Id).FirstOrDefaultAsync();
    if (wh == null) return Results.BadRequest(new { ok = false, error = "Chưa có kho." });
    var lines = new List<(int, int, decimal, string?)>();
    var missing = new List<string>();
    foreach (var l in dto.Lines ?? [])
    {
        var p = await db.Products.FirstOrDefaultAsync(x => x.Code == l.Code);
        if (p != null && l.Qty > 0) lines.Add((p.Id, l.Qty, p.SalePrice, dto.RefNo));
        else if (p == null) missing.Add(l.Code);
    }
    if (lines.Count == 0) return Results.Ok(new { ok = false, msg = "Không mặt hàng nào khớp mã trong WMS.", missing });
    var doc = new MiniWMS.Models.StockDoc { Type = MiniWMS.Models.DocType.Out, FromWarehouseId = wh.Id, PartnerName = dto.PartnerName, RefNo = dto.RefNo, Note = "Xuất bán (MiniDMS)" };
    var id = await svc.CreateDocAsync(doc, lines);
    var (posted, msg) = await svc.PostDocAsync(id);
    var d = await svc.GetDocAsync(id);
    return Results.Ok(new { ok = posted, docCode = d!.Code, posted, msg, traceCode = d.TraceCode, warehouse = wh.Name, missing });
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
record IssueDto(string? RefNo, string? PartnerName, List<IssueLine>? Lines);
record IssueLine(string Code, int Qty);

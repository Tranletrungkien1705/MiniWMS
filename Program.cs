using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MiniWMS.Data;
using MiniWMS.Models;
using MiniWMS.Services;
using Serilog;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;   // giữ claim gốc (role/name/tenant) từ MiniSSO
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
// SSO chung: tin token do MiniSSO cấp (OIDC RS256). Authority tự nạp discovery + JWKS.
var ssoAuthority = Environment.GetEnvironmentVariable("SSO_AUTHORITY") ?? "https://minisso.onrender.com";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.Authority = ssoAuthority;
    o.RequireHttpsMetadata = ssoAuthority.StartsWith("https");
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = ssoAuthority,
        ValidateAudience = false, ValidateLifetime = true, NameClaimType = "name", RoleClaimType = "role"
    };
});
builder.Services.AddAuthorization();
builder.Services.AddFleetObs();
builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await Seeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

app.UseFleetObs();
FleetObs.ReportLicense(ssoAuthority, "miniwms");
app.UseAuthentication();
app.UseAuthorization();

// SSO chung: endpoint xác thực bằng token MiniSSO (đăng nhập 1 lần dùng chung fleet).
app.MapGet("/api/whoami", (ClaimsPrincipal u) => Results.Ok(new
{
    app = "miniwms",
    sub = u.FindFirst("sub")?.Value, name = u.Identity?.Name ?? u.FindFirst("name")?.Value,
    email = u.FindFirst("email")?.Value, tenant = u.FindFirst("tenant")?.Value,
    roles = u.FindAll("role").Select(c => c.Value)
})).RequireAuthorization();

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

// Import kho thật từ DB nguồn (dedupe theo Code)
app.MapPost("/api/import/warehouses", async (List<ImportWhDto> rows, AppDbContext db, ITenantContext tc) =>
{
    if (rows == null || rows.Count == 0) return Results.BadRequest(new { error = "Không có dữ liệu." });
    int added = 0, skipped = 0;
    var orgId = tc.OrgId;
    var existCodes = db.Warehouses.Where(w => w.OrgId == orgId).Select(w => w.Code).ToHashSet();
    foreach (var row in rows)
    {
        if (string.IsNullOrWhiteSpace(row.Code)) { skipped++; continue; }
        if (existCodes.Contains(row.Code.Trim())) { skipped++; continue; }
        db.Warehouses.Add(new Warehouse { OrgId = orgId, Code = row.Code.Trim(), Name = row.Name?.Trim() ?? row.Code.Trim(), Address = row.Address, Keeper = row.Keeper });
        existCodes.Add(row.Code.Trim()); added++;
    }
    await db.SaveChangesAsync();
    return Results.Ok(new { added, skipped, total = added + skipped });
});

// Import vật tư/phụ tùng thật từ DB nguồn (dedupe theo Code)
app.MapPost("/api/import/products", async (List<ImportProductDto> rows, AppDbContext db, ITenantContext tc) =>
{
    if (rows == null || rows.Count == 0) return Results.BadRequest(new { error = "Không có dữ liệu." });
    int added = 0, skipped = 0;
    var orgId = tc.OrgId;
    var existCodes = db.Products.Where(p => p.OrgId == orgId).Select(p => p.Code).ToHashSet();
    foreach (var row in rows)
    {
        if (string.IsNullOrWhiteSpace(row.Code)) { skipped++; continue; }
        var code = row.Code.Trim();
        if (existCodes.Contains(code)) { skipped++; continue; }
        db.Products.Add(new Product
        {
            OrgId = orgId, Code = code, Name = row.Name?.Trim() ?? code,
            Uom = row.Uom?.Trim() ?? "cái", Category = row.Category,
            CostPrice = row.CostPrice, SalePrice = row.SalePrice,
            MinStock = row.MinStock, MaxStock = row.MaxStock
        });
        existCodes.Add(code); added++;
    }
    await db.SaveChangesAsync();
    return Results.Ok(new { added, skipped, total = added + skipped });
});

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

record RegisterOrgDto(string Name);
record IssueDto(string? RefNo, string? PartnerName, List<IssueLine>? Lines);
record IssueLine(string Code, int Qty);
record ImportWhDto(string? Code, string? Name, string? Address, string? Keeper);
record ImportProductDto(string? Code, string? Name, string? Uom, string? Category, decimal CostPrice, decimal SalePrice, int MinStock, int MaxStock);

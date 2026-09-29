using Microsoft.EntityFrameworkCore;
using MiniWMS.Models;

namespace MiniWMS.Data;

public class AppDbContext : DbContext
{
    private readonly Guid _orgId;
    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : base(options) => _orgId = tenant.OrgId;

    public DbSet<Org> Orgs => Set<Org>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockDoc> Docs => Set<StockDoc>();
    public DbSet<StockDocLine> DocLines => Set<StockDocLine>();
    // PORT 1:1 (2020.3.Skycic.Inventory): InvF_InvAudit(+Dtl), InvF_MoveOrd(+Dtl)
    public DbSet<InvAudit> InvAudits => Set<InvAudit>();
    public DbSet<InvAuditLine> InvAuditLines => Set<InvAuditLine>();
    public DbSet<MoveOrd> MoveOrds => Set<MoveOrd>();
    public DbSet<MoveOrdLine> MoveOrdLines => Set<MoveOrdLine>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("miniwms");
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();
        b.Entity<Warehouse>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<Product>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Property(x => x.CostPrice).HasPrecision(18, 2); e.Property(x => x.SalePrice).HasPrecision(18, 2);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<StockDoc>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Ignore(x => x.TotalQty); e.Ignore(x => x.TotalValue);
            e.HasOne(x => x.FromWarehouse).WithMany().HasForeignKey(x => x.FromWarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToWarehouse).WithMany().HasForeignKey(x => x.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<StockDocLine>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Ignore(x => x.LineValue);
            e.HasOne(x => x.Doc).WithMany(x => x.Lines).HasForeignKey(x => x.DocId);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });

        // ── PORT 1:1 (2020.3.Skycic.Inventory) ──────────────────────────────
        // Khoá chính nguồn là IF_InvAudNo (bảng InvF_InvAudit) và IF_MONo (InvF_MoveOrd);
        // ở đích giữ Id kỹ thuật theo mẫu nhà, Code là duy nhất trong 1 tổ chức.
        b.Entity<InvAudit>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Ignore(x => x.TotalQtyInit); e.Ignore(x => x.TotalQtyActual); e.Ignore(x => x.TotalQtyDiff);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InvAuditLine>(e =>
        {
            e.Property(x => x.QtyInit).HasPrecision(18, 2); e.Property(x => x.QtyActual).HasPrecision(18, 2);
            e.Property(x => x.UpAudit).HasPrecision(18, 2); e.Property(x => x.ValAudit).HasPrecision(18, 2);
            e.Property(x => x.Vat).HasPrecision(18, 2);
            e.Ignore(x => x.QtyDiff); e.Ignore(x => x.ValVat);
            e.HasOne(x => x.Audit).WithMany(x => x.Lines).HasForeignKey(x => x.AuditId);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<MoveOrd>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Ignore(x => x.TotalQty); e.Ignore(x => x.TotalValAfterVat);
            e.HasOne(x => x.OutWarehouse).WithMany().HasForeignKey(x => x.OutWarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InWarehouse).WithMany().HasForeignKey(x => x.InWarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<MoveOrdLine>(e =>
        {
            e.Property(x => x.Qty).HasPrecision(18, 2); e.Property(x => x.UpMove).HasPrecision(18, 2);
            e.Property(x => x.ValMove).HasPrecision(18, 2); e.Property(x => x.Vat).HasPrecision(18, 2);
            e.Property(x => x.ValVat).HasPrecision(18, 2); e.Property(x => x.ValAfterVat).HasPrecision(18, 2);
            e.HasOne(x => x.MoveOrd).WithMany(x => x.Lines).HasForeignKey(x => x.MoveOrdId);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
    }

    public override int SaveChanges() { StampOrg(); return base.SaveChanges(); }
    public override Task<int> SaveChangesAsync(CancellationToken ct = default) { StampOrg(); return base.SaveChangesAsync(ct); }
    private void StampOrg()
    {
        foreach (var e in ChangeTracker.Entries<IOrgOwned>())
            if (e.State == EntityState.Added && e.Entity.OrgId == Guid.Empty) e.Entity.OrgId = _orgId;
    }
}

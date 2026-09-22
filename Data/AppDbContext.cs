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
    public DbSet<StockAudit> Audits => Set<StockAudit>();
    public DbSet<StockAuditLine> AuditLines => Set<StockAuditLine>();
    public DbSet<MoveOrder> MoveOrders => Set<MoveOrder>();
    public DbSet<MoveOrderLine> MoveOrderLines => Set<MoveOrderLine>();
    public DbSet<ReturnToSupplier> ReturnToSuppliers => Set<ReturnToSupplier>();
    public DbSet<ReturnToSupplierLine> ReturnToSupplierLines => Set<ReturnToSupplierLine>();
    public DbSet<CustomerReturn> CustomerReturns => Set<CustomerReturn>();
    public DbSet<CustomerReturnLine> CustomerReturnLines => Set<CustomerReturnLine>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("miniwms");
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();
        b.Entity<Warehouse>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<Product>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<StockDoc>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Ignore(x => x.TotalQty);
            e.HasOne(x => x.FromWarehouse).WithMany().HasForeignKey(x => x.FromWarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToWarehouse).WithMany().HasForeignKey(x => x.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<StockDocLine>(e =>
        {
            e.HasOne(x => x.Doc).WithMany(x => x.Lines).HasForeignKey(x => x.DocId);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<StockAudit>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Ignore(x => x.TotalInitQty);
            e.Ignore(x => x.TotalActualQty);
            e.Ignore(x => x.TotalDiffQty);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InDoc).WithMany().HasForeignKey(x => x.InDocId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.OutDoc).WithMany().HasForeignKey(x => x.OutDocId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<StockAuditLine>(e =>
        {
            e.Ignore(x => x.DiffQty);
            e.HasOne(x => x.Audit).WithMany(x => x.Lines).HasForeignKey(x => x.AuditId);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<MoveOrder>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Ignore(x => x.TotalQty);
            e.HasOne(x => x.FromWarehouse).WithMany().HasForeignKey(x => x.FromWarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToWarehouse).WithMany().HasForeignKey(x => x.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockDoc).WithMany().HasForeignKey(x => x.StockDocId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<MoveOrderLine>(e =>
        {
            e.HasOne(x => x.MoveOrder).WithMany(x => x.Lines).HasForeignKey(x => x.MoveOrderId);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ReturnToSupplier>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Ignore(x => x.TotalQty);
            e.Ignore(x => x.TotalAmount);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockDoc).WithMany().HasForeignKey(x => x.StockDocId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ReturnToSupplierLine>(e =>
        {
            e.Ignore(x => x.Amount);
            e.HasOne(x => x.ReturnToSupplier).WithMany(x => x.Lines).HasForeignKey(x => x.ReturnToSupplierId);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<CustomerReturn>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Ignore(x => x.TotalQty);
            e.Ignore(x => x.TotalAmount);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockDoc).WithMany().HasForeignKey(x => x.StockDocId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<CustomerReturnLine>(e =>
        {
            e.Ignore(x => x.Amount);
            e.HasOne(x => x.CustomerReturn).WithMany(x => x.Lines).HasForeignKey(x => x.CustomerReturnId);
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

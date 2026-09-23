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
    public DbSet<StockLot> StockLots => Set<StockLot>();
    public DbSet<StockSerial> StockSerials => Set<StockSerial>();
    public DbSet<InventoryBlock> InventoryBlocks => Set<InventoryBlock>();
    public DbSet<CostPriceHist> CostPriceHists => Set<CostPriceHist>();
    public DbSet<PeriodClosing> PeriodClosings => Set<PeriodClosing>();
    public DbSet<PeriodClosingLine> PeriodClosingLines => Set<PeriodClosingLine>();
    public DbSet<InventoryCarton> InventoryCartons => Set<InventoryCarton>();
    public DbSet<InventoryBox> InventoryBoxes => Set<InventoryBox>();
    public DbSet<InventoryInFG> InventoryInFGs => Set<InventoryInFG>();
    public DbSet<InventoryInFGLine> InventoryInFGLines => Set<InventoryInFGLine>();
    public DbSet<InventoryInFGSerial> InventoryInFGSerials => Set<InventoryInFGSerial>();
    public DbSet<InventoryOutFG> InventoryOutFGs => Set<InventoryOutFG>();
    public DbSet<InventoryOutFGLine> InventoryOutFGLines => Set<InventoryOutFGLine>();
    public DbSet<InventoryOutFGSerial> InventoryOutFGSerials => Set<InventoryOutFGSerial>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PartType> PartTypes => Set<PartType>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<PartColor> PartColors => Set<PartColor>();
    public DbSet<PartColorMap> PartColorMaps => Set<PartColorMap>();
    public DbSet<PartUnit> PartUnits => Set<PartUnit>();
    public DbSet<PartMaterialType> PartMaterialTypes => Set<PartMaterialType>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductModel> ProductModels => Set<ProductModel>();
    public DbSet<InventoryType> InventoryTypes => Set<InventoryType>();
    public DbSet<InventoryLevelType> InventoryLevelTypes => Set<InventoryLevelType>();
    public DbSet<InventoryInType> InventoryInTypes => Set<InventoryInType>();
    public DbSet<InventoryOutType> InventoryOutTypes => Set<InventoryOutType>();
    public DbSet<UserMapInventory> UserMapInventories => Set<UserMapInventory>();
    public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<CustomerSource> CustomerSources => Set<CustomerSource>();
    public DbSet<GovTaxOffice> GovTaxOffices => Set<GovTaxOffice>();
    public DbSet<MoveOrdType> MoveOrdTypes => Set<MoveOrdType>();
    public DbSet<Dealer> Dealers => Set<Dealer>();
    public DbSet<TempPrintType> TempPrintTypes => Set<TempPrintType>();
    public DbSet<TempPrint> TempPrints => Set<TempPrint>();
    public DbSet<CurrencyExchange> CurrencyExchanges => Set<CurrencyExchange>();
    public DbSet<ProductSpec> ProductSpecs => Set<ProductSpec>();
    public DbSet<SpecUnit> SpecUnits => Set<SpecUnit>();
    public DbSet<SpecPrice> SpecPrices => Set<SpecPrice>();
    public DbSet<VATRate> VATRates => Set<VATRate>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<InventorySecret> InventorySecrets => Set<InventorySecret>();
    public DbSet<SecretLicense> SecretLicenses => Set<SecretLicense>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseReceiptLine> PurchaseReceiptLines => Set<PurchaseReceiptLine>();
    public DbSet<InventoryOutHist> InventoryOutHists => Set<InventoryOutHist>();
    public DbSet<InventoryOutHistLine> InventoryOutHistLines => Set<InventoryOutHistLine>();
    public DbSet<InventoryOutHistSerial> InventoryOutHistSerials => Set<InventoryOutHistSerial>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        if (Database.IsNpgsql()) b.HasDefaultSchema("miniwms");
        b.Entity<Org>().HasIndex(x => x.ApiKey).IsUnique();
        b.Entity<CustomerGroup>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.ParentCode });
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Department>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.ParentCode });
            e.HasIndex(x => new { x.OrgId, x.Level });
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<CustomerSource>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.ParentCode });
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<GovTaxOffice>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.ParentCode });
            e.HasIndex(x => new { x.OrgId, x.Level });
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ProductGroup>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.ParentCode });
            e.HasIndex(x => new { x.OrgId, x.BrandCode });
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Area>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.ParentCode });
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<UserMapInventory>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.WarehouseId, x.UserCode }).IsUnique();
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryOutType>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<MoveOrdType>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<Dealer>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.ParentCode });
            e.HasIndex(x => new { x.OrgId, x.Level });
            e.HasIndex(x => new { x.OrgId, x.ProvinceCode });
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryInType>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<InventoryLevelType>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<InventoryType>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<PartMaterialType>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<ProductAttribute>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<ProductModel>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasIndex(x => new { x.OrgId, x.BrandCode }); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<PartUnit>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<Brand>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<PartColor>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<PartColorMap>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.ProductId, x.PartColorCode }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.PartColorCode });
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<PartType>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<Supplier>(e => { e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique(); e.HasQueryFilter(x => x.OrgId == _orgId); });
        b.Entity<Customer>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.CustomerGrpCode });
            e.HasIndex(x => new { x.OrgId, x.CustomerSourceCode });
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
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
        b.Entity<StockLot>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.WarehouseId, x.ProductId, x.LotNo }).IsUnique();
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<StockSerial>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.WarehouseId, x.ProductId, x.SerialNo }).IsUnique();
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryBlock>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.WarehouseId, x.InvBlockCode }).IsUnique();
            e.Ignore(x => x.VolumeM3);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<CostPriceHist>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.WarehouseId, x.ProductId, x.EffectDate });
            e.Property(x => x.CostPrice).HasPrecision(18, 2);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<PeriodClosing>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.PeriodMonth, x.WarehouseId });
            e.Ignore(x => x.TotalItems);
            e.Ignore(x => x.TotalOpeningQty);
            e.Ignore(x => x.TotalInQty);
            e.Ignore(x => x.TotalOutQty);
            e.Ignore(x => x.TotalClosingQty);
            e.Ignore(x => x.TotalClosingValue);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<PeriodClosingLine>(e =>
        {
            e.Property(x => x.LastInPrice).HasPrecision(18, 2);
            e.Property(x => x.InAmount).HasPrecision(18, 2);
            e.Property(x => x.LastOutPrice).HasPrecision(18, 2);
            e.Property(x => x.OutAmount).HasPrecision(18, 2);
            e.Property(x => x.CostPrice).HasPrecision(18, 2);
            e.Property(x => x.ClosingValue).HasPrecision(18, 2);
            e.HasOne(x => x.PeriodClosing).WithMany(x => x.Lines).HasForeignKey(x => x.PeriodClosingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryCarton>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.CartonCode }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.WarehouseId, x.Status });
            e.Ignore(x => x.VolumeM3);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryBox>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.BoxCode }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.WarehouseId, x.Status });
            e.HasIndex(x => new { x.OrgId, x.CartonId });
            e.Ignore(x => x.VolumeM3);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Carton).WithMany().HasForeignKey(x => x.CartonId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryInFG>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.WarehouseId, x.Status });
            e.Ignore(x => x.TotalPlanQty);
            e.Ignore(x => x.TotalActualQty);
            e.Ignore(x => x.TotalDefectQty);
            e.Ignore(x => x.TotalAmount);
            e.Ignore(x => x.TotalSerialsCount);
            e.Ignore(x => x.PassRatePercent);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockDoc).WithMany().HasForeignKey(x => x.StockDocId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryInFGLine>(e =>
        {
            e.Property(x => x.UnitCost).HasPrecision(18, 2);
            e.Ignore(x => x.Amount);
            e.Ignore(x => x.PassRate);
            e.HasOne(x => x.InventoryInFG).WithMany(x => x.Lines).HasForeignKey(x => x.InventoryInFGId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryInFGSerial>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.InventoryInFGId, x.ProductId, x.SerialNo });
            e.HasOne(x => x.InventoryInFG).WithMany(x => x.Serials).HasForeignKey(x => x.InventoryInFGId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryOutFG>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.WarehouseId, x.Status });
            e.Ignore(x => x.TotalQty);
            e.Ignore(x => x.TotalAmount);
            e.Ignore(x => x.TotalSerialsCount);
            e.Ignore(x => x.TotalItemsCount);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockDoc).WithMany().HasForeignKey(x => x.StockDocId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryOutFGLine>(e =>
        {
            e.Property(x => x.UnitCost).HasPrecision(18, 2);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Ignore(x => x.Amount);
            e.Ignore(x => x.CostAmount);
            e.HasOne(x => x.InventoryOutFG).WithMany(x => x.Lines).HasForeignKey(x => x.InventoryOutFGId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryOutFGSerial>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.InventoryOutFGId, x.ProductId, x.SerialNo });
            e.HasOne(x => x.InventoryOutFG).WithMany(x => x.Serials).HasForeignKey(x => x.InventoryOutFGId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<TempPrintType>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<TempPrint>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.TypeCode });
            e.HasIndex(x => new { x.OrgId, x.IsDefault });
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<CurrencyExchange>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.Property(x => x.BuyRate).HasPrecision(18, 4);
            e.Property(x => x.SellRate).HasPrecision(18, 4);
            e.Property(x => x.InterExRate).HasPrecision(18, 4);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<ProductSpec>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.ModelCode });
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<SpecUnit>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.SpecCode, x.UnitCode }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.SpecCode });
            e.Property(x => x.Qty).HasPrecision(18, 4);
            e.Property(x => x.Length).HasPrecision(18, 4);
            e.Property(x => x.Width).HasPrecision(18, 4);
            e.Property(x => x.Height).HasPrecision(18, 4);
            e.Property(x => x.Volume).HasPrecision(18, 4);
            e.Property(x => x.Weight).HasPrecision(18, 4);
            e.Ignore(x => x.ComputedVolumeM3);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<SpecPrice>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.SpecCode, x.UnitCode }).IsUnique();
            e.Property(x => x.BuyPrice).HasPrecision(18, 2);
            e.Property(x => x.SellPrice).HasPrecision(18, 2);
            e.Property(x => x.DiscountVND).HasPrecision(18, 2);
            e.Ignore(x => x.NetSellPrice);
            e.Ignore(x => x.GrossProfit);
            e.Ignore(x => x.GrossMarginPercent);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<VATRate>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.VATRateCode }).IsUnique();
            e.Property(x => x.Rate).HasPrecision(18, 2);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryTransaction>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.WarehouseId, x.ProductId, x.CreatedAt });
            e.HasIndex(x => new { x.OrgId, x.TxnType });
            e.Ignore(x => x.QtyChangeTotal);
            e.Ignore(x => x.QtyChangeAvail);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventorySecret>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.SerialNo }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.GenTimesNo });
            e.HasIndex(x => new { x.OrgId, x.FlagUsed });
            e.Ignore(x => x.Status);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<SecretLicense>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Mst }).IsUnique();
            e.Ignore(x => x.RemainingQty);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Province>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<District>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.ProvinceCode });
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<Agent>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.ProvinceCode });
            e.HasIndex(x => new { x.OrgId, x.DistrictCode });
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<PurchaseReceipt>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.WarehouseId, x.Status });
            e.Ignore(x => x.TotalQty);
            e.Ignore(x => x.TotalAmount);
            e.Ignore(x => x.TotalVATAmount);
            e.Ignore(x => x.TotalAmountAfterVAT);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockDoc).WithMany().HasForeignKey(x => x.StockDocId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<PurchaseReceiptLine>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Ignore(x => x.Amount);
            e.Ignore(x => x.VATAmount);
            e.Ignore(x => x.AmountAfterVAT);
            e.HasOne(x => x.PurchaseReceipt).WithMany(x => x.Lines).HasForeignKey(x => x.PurchaseReceiptId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryOutHist>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.OrgId, x.WarehouseId, x.Status });
            e.Ignore(x => x.TotalQty);
            e.Ignore(x => x.TotalSerialsCount);
            e.Ignore(x => x.TotalItemsCount);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.StockDoc).WithMany().HasForeignKey(x => x.StockDocId).OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryOutHistLine>(e =>
        {
            e.HasOne(x => x.InventoryOutHist).WithMany(x => x.Lines).HasForeignKey(x => x.InventoryOutHistId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => x.OrgId == _orgId);
        });
        b.Entity<InventoryOutHistSerial>(e =>
        {
            e.HasIndex(x => new { x.OrgId, x.InventoryOutHistId, x.ProductId, x.SerialNo });
            e.HasOne(x => x.InventoryOutHist).WithMany(x => x.Serials).HasForeignKey(x => x.InventoryOutHistId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
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

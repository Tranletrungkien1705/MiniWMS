using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Ord_OrderSO.cs — Đơn hàng bán (DMS).
/// Nguồn model: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Common\Models\DMS\Ord_OrderSO.cs:9-101.
/// Bảng DDL trong hệ thống DMS/Inventory: Ord_OrderSO.
/// </summary>
public class Ord_OrderSO
{
    public string OrderNo { get; set; } = "";
    public string? OrderSONoSys { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? UserCodeOwner { get; set; }
    public string? CustomerCodeSysUser { get; set; }
    public string? CustomerCodeUser { get; set; }
    public string? CustomerNameUser { get; set; }
    public string? OrderType { get; set; }
    public string? EServiceID { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhoneNo { get; set; }
    public string? CustomerMobilePhone { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerContactName { get; set; }
    public string? CustomerContactPhone { get; set; }
    public string? CustomerAreaCode { get; set; }
    public string? CustomerAreaName { get; set; }
    public string? PrdPriceCode { get; set; }
    public string? RemarkDL { get; set; }
    public string? RemarkOrg { get; set; }
    public string? FlagInvoice { get; set; }
    public double? TotalValOrderAppr { get; set; }
    public double? TotalValOrderApprAfterDiffer { get; set; }
    public double? TotalValOrderDc { get; set; }
    public double? TotalValReceivables { get; set; }
    public double? TotalValDifferReturn { get; set; }
    public double? TotalValPmt { get; set; }
    public double? TotalValRemain { get; set; }
    public double? TotalValOrderBeforeVAT { get; set; }
    public double? TotalVATVal { get; set; }
    public double? TotalValOrderAfterVAT { get; set; }
    public string? ExpectedDeliverDate { get; set; }
    public string? EstimatedDeliverDate { get; set; }
    public string? ActualDeliverDate { get; set; }
    public string? InvoiceCode { get; set; }
    public string? InvoiceNo { get; set; }
    public string? InvoiceFormNo { get; set; }
    public string? InvoiceSign { get; set; }
    public string? PaymentMethodCode { get; set; }
    public string? ShipperAddress { get; set; }
    public string? CustomerCodeSysShipper { get; set; }
    public string? CustomerCodeShipper { get; set; }
    public string? CustomerNameShipper { get; set; }
    public double? ValShipper { get; set; }
    public string? CurrencyCode { get; set; }
    public double? ExchangeRate { get; set; }
    public string? CustomField1 { get; set; }
    public DateTime? CreateDTimeUTC { get; set; }
    public string? CreateBy { get; set; }
    public string? CreateByCustomerCodeSys { get; set; }
    public string? CreateByCustomerCode { get; set; }
    public string? CreateByCustomerName { get; set; }
    public DateTime? LUDTimeUTC { get; set; }
    public string? LUBy { get; set; }
    public string? LUByCustomerCodeSys { get; set; }
    public string? LUByCustomerCode { get; set; }
    public string? LUByCustomerName { get; set; }
    public DateTime? ApprDTimeUTC { get; set; }
    public string? ApprBy { get; set; }
    public string? ApprByCustomerCodeSys { get; set; }
    public string? ApprByCustomerCode { get; set; }
    public string? ApprByCustomerName { get; set; }
    public DateTime? CancelDTimeUTC { get; set; }
    public string? CancelBy { get; set; }
    public string? CancelByCustomerCodeSys { get; set; }
    public string? CancelByCustomerCode { get; set; }
    public string? CancelByCustomerName { get; set; }
    public string? OrderStatus { get; set; }
    public string? DeliverStatus { get; set; }
    public string? InvoiceStatus { get; set; }
    public string? Remark { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? suow_UserCode { get; set; }
    public string? suow_UserName { get; set; }
    public string? OrderCF01 { get; set; }
    public string? OrderCF02 { get; set; }
    public string? OrderCF03 { get; set; }
    public string? OrderCF04 { get; set; }
    public string? OrderCF05 { get; set; }
    public string? DebitAccount { get; set; }
    public string? CreditAccount { get; set; }
    public string? OrderTypeName { get; set; }
    public string? FlagSynced { get; set; }
    public DateTime? SyncDTimeUTC { get; set; }
    public string? BusinessGroupCode { get; set; }
}

public class Ord_OrderSOConfiguration : IEntityTypeConfiguration<Ord_OrderSO>
{
    public void Configure(EntityTypeBuilder<Ord_OrderSO> b)
    {
        b.ToTable("Ord_OrderSO");
        b.HasKey(x => x.OrderNo);
        b.Property(x => x.OrderNo).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.CustomerCode).HasMaxLength(50);
        b.Property(x => x.CustomerName).HasMaxLength(250);
        b.Property(x => x.OrderStatus).HasMaxLength(50);
        b.Property(x => x.OrderCF03).HasMaxLength(100);
    }
}

/// <summary>
/// Seed dữ liệu demo cho Ord_OrderSO (Order = 245, nghiệp vụ).
/// </summary>
public class Ord_OrderSOSeeder : IPortSeeder
{
    public int Order => 245;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Ord_OrderSO>().AnyAsync()) return;

        var now = DateTime.UtcNow;
        db.Set<Ord_OrderSO>().AddRange(
            new Ord_OrderSO
            {
                OrderNo = "SO001",
                NetworkID = "1001",
                OrgID = "ORG01",
                CustomerCode = "CUST01",
                CustomerName = "Công ty TNHH Ánh Dương",
                OrderStatus = "APPROVE",
                OrderCF03 = "VIN001",
                CreateDTimeUTC = new DateTime(2026, 1, 10, 8, 30, 0, DateTimeKind.Utc),
                ApprDTimeUTC = new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            },
            new Ord_OrderSO
            {
                OrderNo = "SO002",
                NetworkID = "1001",
                OrgID = "ORG01",
                CustomerCode = "CUST02",
                CustomerName = "Công ty Bình Minh",
                OrderStatus = "APPROVE",
                OrderCF03 = "VIN002",
                CreateDTimeUTC = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc),
                ApprDTimeUTC = new DateTime(2026, 2, 15, 11, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            },
            new Ord_OrderSO
            {
                OrderNo = "SO003",
                NetworkID = "1001",
                OrgID = "ORG01",
                CustomerCode = "CUST03",
                CustomerName = "Công ty Cơ khí Toàn Cầu",
                OrderStatus = "PENDING", // Không phải APPROVE -> bị loại bởi bộ lọc mặc định
                OrderCF03 = "VIN003",
                CreateDTimeUTC = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            },
            new Ord_OrderSO
            {
                OrderNo = "SO004",
                NetworkID = "1002", // Khác NetworkID
                OrgID = "ORG02",
                CustomerCode = "CUST04",
                CustomerName = "Doanh nghiệp Tư nhân Sao Mai",
                OrderStatus = "APPROVE",
                OrderCF03 = "VIN004",
                CreateDTimeUTC = new DateTime(2026, 3, 5, 14, 0, 0, DateTimeKind.Utc),
                ApprDTimeUTC = new DateTime(2026, 3, 5, 15, 0, 0, DateTimeKind.Utc),
                LogLUBy = "seed",
                LogLUDTimeUTC = now
            }
        );

        await db.SaveChangesAsync();
    }
}

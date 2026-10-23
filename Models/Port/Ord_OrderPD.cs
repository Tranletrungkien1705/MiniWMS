using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Ord_OrderPD (đơn hàng sản xuất từ hệ thống LQDMS).
/// Nguồn gate `InvF_InventoryInController.cs:2439-2614` (#93) và model `OS_Ord_OrderPD.cs:9-35`.
/// </summary>
public class Ord_OrderPD
{
    public string OrderPDNoSys { get; set; } = "";
    public string? OrderPDNo { get; set; }
    public string? NetworkID { get; set; }
    public string? OrgID { get; set; }
    public string? OrderType { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? OrderStatus { get; set; }
    public string? Remark { get; set; }
}

public class Ord_OrderPDConfiguration : IEntityTypeConfiguration<Ord_OrderPD>
{
    public void Configure(EntityTypeBuilder<Ord_OrderPD> builder)
    {
        builder.ToTable("Ord_OrderPD");
        builder.HasKey(x => x.OrderPDNoSys);
        builder.Property(x => x.OrderPDNoSys).HasMaxLength(100);
        builder.Property(x => x.OrderPDNo).HasMaxLength(100);
    }
}

/// <summary>
/// port 1:1 Ord_OrderPDDtl (chi tiết đơn hàng sản xuất từ hệ thống LQDMS).
/// Nguồn gate `InvF_InventoryInController.cs:2439-2614` (#93) và model `OS_Ord_OrderPDDtl.cs:9-37`.
/// </summary>
public class Ord_OrderPDDtl
{
    public string OrderPDNoSys { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public string? OrgID { get; set; }
    public string? CustomerCodeSys { get; set; }
    public double? PlanQty { get; set; }
    public double? PGInvInRemainQty { get; set; }
    public double? PGInvInQty { get; set; }
    public string? Remark { get; set; }
}

public class Ord_OrderPDDtlConfiguration : IEntityTypeConfiguration<Ord_OrderPDDtl>
{
    public void Configure(EntityTypeBuilder<Ord_OrderPDDtl> builder)
    {
        builder.ToTable("Ord_OrderPDDtl");
        builder.HasKey(x => new { x.OrderPDNoSys, x.ProductCode });
        builder.Property(x => x.OrderPDNoSys).HasMaxLength(100);
        builder.Property(x => x.ProductCode).HasMaxLength(100);
    }
}

public class Ord_OrderPDSeeder : IPortSeeder
{
    // Grep Order =>: 14, 17, 19, 20, 35, 36, 37, 38, 39, 41, 42, 43, 44, 200, 220, 240
    // Chọn Order = 45 (danh mục 10-99)
    public int Order => 45;

    public async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Set<Ord_OrderPD>().AnyAsync())
        {
            await db.Set<Ord_OrderPD>().AddRangeAsync(
                new Ord_OrderPD
                {
                    OrderPDNoSys = "ORDPD_SYS_01",
                    OrderPDNo = "REFNO01",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    OrderType = "PD",
                    CustomerCodeSys = "CUST_SYS_01",
                    CustomerCode = "CUST01",
                    CustomerName = "Khách hàng thử nghiệm 01",
                    OrderStatus = "APPROVE",
                    Remark = "Đơn hàng test 01"
                },
                new Ord_OrderPD
                {
                    OrderPDNoSys = "ORDPD_SYS_02",
                    OrderPDNo = "REFNO02",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    OrderType = "PD",
                    CustomerCodeSys = "CUST_SYS_02",
                    CustomerCode = "CUST02",
                    CustomerName = "Khách hàng thử nghiệm 02",
                    OrderStatus = "APPROVE",
                    Remark = "Đơn hàng test 02"
                },
                new Ord_OrderPD
                {
                    OrderPDNoSys = "ORDPD_SYS_03",
                    OrderPDNo = "REFNO03",
                    NetworkID = "1001",
                    OrgID = "ORG01",
                    OrderType = "PD",
                    CustomerCodeSys = "CUST_SYS_01",
                    CustomerCode = "CUST01",
                    CustomerName = "Khách hàng thử nghiệm 01",
                    OrderStatus = "APPROVE",
                    Remark = "Đơn hàng test 03 có 2 sản phẩm"
                }
            );
        }

        if (!await db.Set<Ord_OrderPDDtl>().AnyAsync())
        {
            await db.Set<Ord_OrderPDDtl>().AddRangeAsync(
                new Ord_OrderPDDtl
                {
                    OrderPDNoSys = "ORDPD_SYS_01",
                    ProductCode = "SP01",
                    OrgID = "ORG01",
                    CustomerCodeSys = "CUST_SYS_01",
                    PlanQty = 100,
                    PGInvInRemainQty = 60,
                    PGInvInQty = 40,
                    Remark = "SP01 theo đơn 01"
                },
                new Ord_OrderPDDtl
                {
                    OrderPDNoSys = "ORDPD_SYS_01",
                    ProductCode = "SP02",
                    OrgID = "ORG01",
                    CustomerCodeSys = "CUST_SYS_01",
                    PlanQty = 50,
                    PGInvInRemainQty = 50,
                    PGInvInQty = 0,
                    Remark = "SP02 theo đơn 01 (không khớp FlagActive của Mst_Product)"
                },
                new Ord_OrderPDDtl
                {
                    OrderPDNoSys = "ORDPD_SYS_02",
                    ProductCode = "SP03",
                    OrgID = "ORG01",
                    CustomerCodeSys = "CUST_SYS_02",
                    PlanQty = 80,
                    PGInvInRemainQty = 80,
                    PGInvInQty = 0,
                    Remark = "SP03 theo đơn 02 (không khớp FlagActive của Mst_Product)"
                },
                new Ord_OrderPDDtl
                {
                    OrderPDNoSys = "ORDPD_SYS_03",
                    ProductCode = "SP01",
                    OrgID = "ORG01",
                    CustomerCodeSys = "CUST_SYS_01",
                    PlanQty = 100,
                    PGInvInRemainQty = 75,
                    PGInvInQty = 25,
                    Remark = "SP01 theo đơn 03"
                },
                new Ord_OrderPDDtl
                {
                    OrderPDNoSys = "ORDPD_SYS_03",
                    ProductCode = "SP01-L2A",
                    OrgID = "ORG01",
                    CustomerCodeSys = "CUST_SYS_01",
                    PlanQty = 30,
                    PGInvInRemainQty = 30,
                    PGInvInQty = 0,
                    Remark = "SP01-L2A theo đơn 03"
                }
            );
        }

        await db.SaveChangesAsync();
    }
}

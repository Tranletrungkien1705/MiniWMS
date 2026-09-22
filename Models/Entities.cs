namespace MiniWMS.Models;

public class Org
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
public interface IOrgOwned { Guid OrgId { get; set; } }

public enum DocType { In = 0, Out = 1, Transfer = 2 }      // Nhập / Xuất / Chuyển kho
public enum DocStatus { Draft = 0, Posted = 1, Cancelled = 2 }
public enum StockAuditStatus { Draft = 0, Finished = 1, Cancelled = 2 } // Đang kiểm kê / Đã cân bằng / Đã hủy
public enum MoveOrderStatus { Pending = 0, Approved = 1, Finished = 2, Cancelled = 3 } // Chờ duyệt / Đã duyệt / Đã chuyển kho / Đã hủy
public enum ReturnSupStatus { Draft = 0, Finished = 1, Cancelled = 2 } // Chờ duyệt / Đã xuất trả / Đã hủy
public enum CusReturnStatus { Draft = 0, Finished = 1, Cancelled = 2 } // Chờ nhận hàng / Đã nhập kho / Đã hủy

public class Warehouse : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Address { get; set; }
}

public class Product : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Uom { get; set; } = "cái";
    public int MinStock { get; set; }
    public int MaxStock { get; set; }
    public decimal CostPrice { get; set; } = 0m;
}

/// <summary>Phiếu kho (nhập/xuất/chuyển). Post → cập nhật tồn.</summary>
public class StockDoc : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public DocType Type { get; set; }
    public int? FromWarehouseId { get; set; }   // Out/Transfer
    public int? ToWarehouseId { get; set; }     // In/Transfer
    public string? SupplierCode { get; set; }   // Mã nhà cung cấp (khi nhập kho mua hàng)
    public string? SupplierName { get; set; }   // Tên nhà cung cấp (khi nhập kho mua hàng)
    public DateTime Date { get; set; } = DateTime.Now;
    public string? Note { get; set; }
    public string? RefNo { get; set; }
    public string CreatedBy { get; set; } = "";
    public DocStatus Status { get; set; } = DocStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Warehouse? FromWarehouse { get; set; }
    public Warehouse? ToWarehouse { get; set; }
    public List<StockDocLine> Lines { get; set; } = [];

    public int TotalQty => Lines.Sum(l => l.Quantity);
}

public class StockDocLine : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int DocId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public StockDoc Doc { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>Phiếu kiểm kê kho (Stock Audit). Khi hoàn thành / cân bằng kho sẽ tự động sinh phiếu xuất/nhập chênh lệch.</summary>
public class StockAudit : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public int WarehouseId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string? Note { get; set; }
    public string CreatedBy { get; set; } = "";
    public StockAuditStatus Status { get; set; } = StockAuditStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? FinishedAt { get; set; }

    public int? InDocId { get; set; }     // Phiếu nhập điều chỉnh thừa (nếu có)
    public int? OutDocId { get; set; }    // Phiếu xuất điều chỉnh thiếu (nếu có)

    public Warehouse Warehouse { get; set; } = null!;
    public StockDoc? InDoc { get; set; }
    public StockDoc? OutDoc { get; set; }
    public List<StockAuditLine> Lines { get; set; } = [];

    public int TotalInitQty => Lines.Sum(l => l.QtyInit);
    public int TotalActualQty => Lines.Sum(l => l.QtyActual);
    public int TotalDiffQty => Lines.Sum(l => l.DiffQty);
}

public class StockAuditLine : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int AuditId { get; set; }
    public int ProductId { get; set; }
    public int QtyInit { get; set; }      // Tồn sổ sách lý thuyết tại thời điểm lập kiểm kê
    public int QtyActual { get; set; }    // Tồn thực tế kiểm đếm
    public string? Note { get; set; }

    public StockAudit Audit { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public int DiffQty => QtyActual - QtyInit; // > 0: Thừa, < 0: Thiếu, = 0: Khớp
}

/// <summary>Dòng chi tiết Thẻ kho (Warehouse Card - port từ Rpt_InvF_WarehouseCard Skycic).</summary>
public record WarehouseCardRow(
    DateTime Date,
    string DocCode,
    int DocId,
    DocType DocType,
    string ActionDesc,
    int WarehouseId,
    string WarehouseName,
    string? OffsetWarehouseName,
    int QtyIn,
    int QtyOut,
    int Balance,
    string? Note,
    string? RefNo
);

/// <summary>Báo cáo Thẻ kho tổng hợp của một mặt hàng theo thời gian và kho.</summary>
public record WarehouseCardReport(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    int? WarehouseId,
    string WarehouseName,
    DateTime? FromDate,
    DateTime? ToDate,
    int OpeningBalance,
    int TotalIn,
    int TotalOut,
    int ClosingBalance,
    List<WarehouseCardRow> Rows
);

/// <summary>Lệnh điều chuyển kho (Move Order - port từ InvF_MoveOrd Skycic). Quản lý quy trình yêu cầu, phê duyệt và thực hiện chuyển kho.</summary>
public class MoveOrder : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public int FromWarehouseId { get; set; }   // Kho xuất chuyển
    public int ToWarehouseId { get; set; }     // Kho nhận chuyển
    public DateTime Date { get; set; } = DateTime.Now;
    public string? Note { get; set; }
    public string CreatedBy { get; set; } = "";
    public MoveOrderStatus Status { get; set; } = MoveOrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public int? StockDocId { get; set; }       // Phiếu kho chuyển hàng được sinh khi thực hiện lệnh

    public Warehouse FromWarehouse { get; set; } = null!;
    public Warehouse ToWarehouse { get; set; } = null!;
    public StockDoc? StockDoc { get; set; }
    public List<MoveOrderLine> Lines { get; set; } = [];

    public int TotalQty => Lines.Sum(l => l.Quantity);
}

public class MoveOrderLine : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int MoveOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }

    public MoveOrder MoveOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>Danh mục Nhà cung cấp hàng hóa (port từ Mst_Supplier Skycic).</summary>
public class Supplier : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Phiếu xuất trả hàng nhà cung cấp (Return to Supplier - port từ InvF_InventoryReturnSup Skycic). Quản lý xuất trả hàng lỗi, hỏng, cận hạn hoặc đổi trả cho NCC.</summary>
public class ReturnToSupplier : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public int WarehouseId { get; set; }         // Kho xuất trả hàng
    public string SupplierName { get; set; } = ""; // Tên nhà cung cấp
    public string? SupplierCode { get; set; }      // Mã NCC
    public string? RefDocNo { get; set; }          // Số phiếu nhập kho hoặc hóa đơn mua gốc (IF_InvInNo)
    public DateTime Date { get; set; } = DateTime.Now;
    public string? Reason { get; set; }            // Lý do trả hàng (lỗi hàng, sai mẫu, hư hỏng, cận date...)
    public string CreatedBy { get; set; } = "";
    public ReturnSupStatus Status { get; set; } = ReturnSupStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? FinishedAt { get; set; }
    public int? StockDocId { get; set; }          // Phiếu xuất kho sinh ra khi hoàn tất xuất trả hàng

    public Warehouse Warehouse { get; set; } = null!;
    public StockDoc? StockDoc { get; set; }
    public List<ReturnToSupplierLine> Lines { get; set; } = [];

    public int TotalQty => Lines.Sum(l => l.Quantity);
    public decimal TotalAmount => Lines.Sum(l => l.Quantity * l.UnitPrice);
}

public class ReturnToSupplierLine : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ReturnToSupplierId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }        // Đơn giá trả lại / giá mua
    public string? Note { get; set; }             // Chi tiết tình trạng lỗi mặt hàng

    public ReturnToSupplier ReturnToSupplier { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal Amount => Quantity * UnitPrice;
}

/// <summary>Phiếu nhập hàng khách trả lại (Customer Return - port từ InvF_InventoryCusReturn Skycic). Quản lý nhận hàng hoàn, đổi size, đổi trả từ khách hàng.</summary>
public class CustomerReturn : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public int WarehouseId { get; set; }         // Kho tiếp nhận hàng trả
    public string CustomerName { get; set; } = ""; // Tên khách hàng
    public string? CustomerCode { get; set; }      // Mã khách hàng
    public string? InvoiceNo { get; set; }         // Số hóa đơn bán hàng gốc
    public string? RefOrderNo { get; set; }        // Số đơn hàng / phiếu xuất gốc
    public DateTime Date { get; set; } = DateTime.Now;
    public string? Reason { get; set; }            // Lý do trả lại (đổi size, lỗi vải, khách hoàn đơn...)
    public string CreatedBy { get; set; } = "";
    public CusReturnStatus Status { get; set; } = CusReturnStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? FinishedAt { get; set; }
    public int? StockDocId { get; set; }          // Phiếu nhập kho tự động sinh khi nhận hàng & hoàn tất

    public Warehouse Warehouse { get; set; } = null!;
    public StockDoc? StockDoc { get; set; }
    public List<CustomerReturnLine> Lines { get; set; } = [];

    public int TotalQty => Lines.Sum(l => l.Quantity);
    public decimal TotalAmount => Lines.Sum(l => l.Quantity * l.UnitPrice);
}

public class CustomerReturnLine : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int CustomerReturnId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }        // Đơn giá bán / giá nhận hoàn trả
    public string? Note { get; set; }             // Chi tiết lý do đổi trả mặt hàng

    public CustomerReturn CustomerReturn { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal Amount => Quantity * UnitPrice;
}

/// <summary>Dòng chi tiết Báo cáo Nhập Xuất Tồn theo kỳ (port từ Rpt_Inventory_In_Out_Inv Skycic).</summary>
public record InventoryInOutRow(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    int WarehouseId,
    string WarehouseName,
    int OpeningQty,     // Tồn đầu kỳ (BeginPeriod_Inv_QtyBase)
    int InQty,          // Nhập trong kỳ (InPeriod_In_QtyBase)
    int OutQty,         // Xuất trong kỳ (InPeriod_Out_QtyBase)
    int ClosingQty      // Tồn cuối kỳ (EndPeriod_Inv_QtyBase)
);

/// <summary>Báo cáo Nhập Xuất Tồn tổng hợp theo kỳ (port từ Rpt_Inventory_In_Out_Inv Skycic).</summary>
public record InventoryInOutReport(
    int? WarehouseId,
    string WarehouseName,
    DateTime FromDate,
    DateTime ToDate,
    string? Keyword,
    int TotalOpeningQty,
    int TotalInQty,
    int TotalOutQty,
    int TotalClosingQty,
    List<InventoryInOutRow> Rows
);

/// <summary>Dòng chi tiết Báo cáo Tổng hợp Nhập mua & Trả hàng nhà cung cấp (port từ Rpt_Summary_InAndReturnSup Skycic).</summary>
public record SummaryInReturnSupRow(
    string SupplierCode,
    string SupplierName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    int InQty,              // TotalQtyIn
    decimal InAmount,       // TotalValIn
    int ReturnQty,          // TotalQtyReturn
    decimal ReturnAmount,   // TotalValReturn
    int NetQty,             // TotalQtyRemain = InQty - ReturnQty
    decimal NetAmount,      // TotalValRemain = InAmount - ReturnAmount
    double ReturnRate,      // Tỷ lệ trả hàng % = ReturnQty / InQty * 100
    double SharePercent,    // Tỷ trọng thực nhận % = NetQty / TotalAllNetQty * 100
    string QualityGrade,    // Đánh giá chất lượng NCC: Tốt (<=2%), Cảnh báo (2%-5%), Kém (>5%)
    string QualityBadgeClass
);

/// <summary>Báo cáo Tổng hợp Nhập mua & Trả hàng nhà cung cấp (port từ Rpt_Summary_InAndReturnSup Skycic).</summary>
public record SummaryInReturnSupReport(
    int? WarehouseId,
    string WarehouseName,
    string? SupplierCode,
    DateTime FromDate,
    DateTime ToDate,
    string? Keyword,
    int TotalInQty,
    decimal TotalInAmount,
    int TotalReturnQty,
    decimal TotalReturnAmount,
    int TotalNetQty,
    decimal TotalNetAmount,
    double AvgReturnRate,
    int SuppliersCount,
    int ProductsCount,
    List<SummaryInReturnSupRow> Rows
);

/// <summary>Mức cảnh báo tồn kho an toàn (port từ Rpt_Inv_InventoryBalance_Minimum Skycic).</summary>
public enum StockAlertLevel
{
    OutOfStock = 0, // Hết hàng / Cháy kho (Tồn = 0)
    Danger = 1,      // Dưới định mức tối thiểu (0 < Tồn < MinStock)
    Warning = 2,     // Cận định mức an toàn (MinStock <= Tồn <= MinStock * 1.25)
    Safe = 3         // Đạt chuẩn an toàn (Tồn > MinStock * 1.25)
}

/// <summary>Dòng chi tiết Báo cáo Chạm tồn kho tối thiểu & Cảnh báo an toàn kho (port từ Rpt_Inv_InventoryBalance_Minimum Skycic).</summary>
public record StockMinimumRow(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    int WarehouseId,
    string WarehouseName,
    int MinStock,
    int MaxStock,
    int CurrentQty,
    int ShortageQty,
    double SafetyRatio,
    StockAlertLevel AlertLevel,
    string AlertLabel,
    string BadgeClass
);

/// <summary>Báo cáo Chạm tồn kho tối thiểu tổng hợp (port từ Rpt_Inv_InventoryBalance_Minimum Skycic).</summary>
public record StockMinimumReport(
    int? WarehouseId,
    string WarehouseName,
    bool OnlyBelowMin,
    string? Keyword,
    int TotalMonitored,
    int OutOfStockCount,
    int DangerCount,
    int WarningCount,
    int SafeCount,
    int TotalShortageQty,
    List<StockMinimumRow> Rows
);

/// <summary>Trạng thái hạn sử dụng của Lô hàng (port từ Rpt_InvBalLot_MaxExpiredDateByInv Skycic).</summary>
public enum LotExpiryStatus
{
    Expired = 0,    // Đã hết hạn (DaysToExpiry < 0)
    Critical = 1,   // Cận hạn nguy cấp (0 <= DaysToExpiry <= 30 ngày)
    Warning = 2,    // Cận hạn cảnh báo (31 <= DaysToExpiry <= 90 ngày)
    Good = 3        // Còn hạn an toàn (> 90 ngày)
}

/// <summary>Thông tin tồn kho theo Lô & Hạn sử dụng (port từ Inv_InventoryBalanceLot Skycic).</summary>
public class StockLot : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public string LotNo { get; set; } = "";             // Số lô sản xuất (ProductLotNo)
    public DateTime? ProductionDate { get; set; }        // Ngày sản xuất
    public DateTime ExpiredDate { get; set; }            // Ngày hết hạn (MaxExpiredDate)
    public DateTime InDate { get; set; } = DateTime.Now; // Ngày nhập kho (LastInInvDate)
    public int Quantity { get; set; }                    // Số lượng tồn theo lô (QtyTotalOK)
    public string? Note { get; set; }                    // Ghi chú lô hàng

    public Warehouse Warehouse { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>Dòng chi tiết Báo cáo Theo dõi Lô & Hạn sử dụng (port từ Rpt_InvBalLot_MaxExpiredDateByInv Skycic).</summary>
public record StockLotReportRow(
    int LotId,
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    string LotNo,
    DateTime? ProductionDate,
    DateTime ExpiredDate,
    DateTime InDate,
    int DaysInStock,        // Số ngày tồn kho (QtyDayInv = DATEDIFF(day, InDate, Today))
    int DaysToExpiry,       // Số ngày còn lại đến hạn (DATEDIFF(day, Today, ExpiredDate))
    int Quantity,           // Số lượng tồn theo lô
    LotExpiryStatus Status, // Trạng thái hạn dùng
    string StatusLabel,     // Nhãn trạng thái tiếng Việt
    string BadgeClass       // Bootstrap badge class
);

/// <summary>Báo cáo Quản lý Lô & Hạn sử dụng tổng hợp (port từ Rpt_InvBalLot_MaxExpiredDateByInv Skycic).</summary>
public record StockLotExpiryReport(
    int? WarehouseId,
    string WarehouseName,
    LotExpiryStatus? StatusFilter,
    string? Keyword,
    int TotalLots,
    int ExpiredLotsCount,
    int CriticalLotsCount,
    int WarningLotsCount,
    int GoodLotsCount,
    int TotalQuantity,
    List<StockLotReportRow> Rows
);

/// <summary>Phân nhóm tuổi kho / Thời gian lưu kho hàng hóa (port từ Rpt_Inv_InventoryBalance_StorageTime Skycic).</summary>
public enum StorageTimeAgingBracket
{
    Tier1_Under30 = 0,   // Dưới 30 ngày: Hàng mới nhập, luân chuyển tốt
    Tier2_31To60 = 1,    // 31 - 60 ngày: Lưu kho bình thường
    Tier3_61To90 = 2,    // 61 - 90 ngày: Cần lưu ý / Tốc độ tiêu thụ chậm
    Tier4_Over90 = 3     // Trên 90 ngày: Tồn lâu / Nguy cơ đọng vốn / Cần xả hàng
}

/// <summary>Dòng chi tiết Báo cáo Tuổi kho & Thời gian lưu kho hàng hoá (port từ Rpt_Inv_InventoryBalance_StorageTime Skycic).</summary>
public record StorageTimeRow(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    int WarehouseId,
    string WarehouseName,
    int CurrentQty,
    decimal CostPrice,
    decimal TotalValue,
    DateTime? LastInDate,
    int StorageDays,
    StorageTimeAgingBracket Bracket,
    string BracketLabel,
    string BadgeClass,
    string Recommendation
);

/// <summary>Báo cáo Tuổi kho & Thời gian lưu kho hàng hoá tổng hợp (port từ Rpt_Inv_InventoryBalance_StorageTime Skycic).</summary>
public record StorageTimeReport(
    int? WarehouseId,
    string WarehouseName,
    DateTime AsOfDate,
    StorageTimeAgingBracket? BracketFilter,
    string? Keyword,
    int TotalItems,
    int TotalQty,
    decimal TotalInventoryValue,
    int StagnantItemsCount,
    decimal StagnantValue,
    double AverageStorageDays,
    int Under30Count,
    int From31To60Count,
    int From61To90Count,
    int Over90Count,
    List<StorageTimeRow> Rows
);

/// <summary>Trạng thái tồn kho theo Serial / IMEI (port từ Inv_InventoryBalanceSerial Skycic).</summary>
public enum StockSerialStatus
{
    Available = 0, // Khả dụng / Sẵn sàng xuất bán (BlockStatus = '0')
    Locked = 1,    // Tạm khóa / Giữ hàng theo đơn (BlockStatus = '1')
    DamagedNG = 2, // Lỗi hỏng / Chờ thẩm định / Xử lý bảo hành (FlagNG = '1')
    Exported = 3   // Đã xuất kho (Đã xuất theo phiếu xuất / đơn hàng)
}

/// <summary>Thông tin tồn kho chi tiết theo Serial / IMEI (port từ Inv_InventoryBalanceSerial Skycic).</summary>
public class StockSerial : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public string SerialNo { get; set; } = "";             // Số Serial / IMEI / Barcode cá thể hóa (SerialNo)
    public string? LotNo { get; set; }                     // Mã lô hàng gắn liền (ProductLotNo)
    public StockSerialStatus Status { get; set; } = StockSerialStatus.Available; // Trạng thái Serial
    public DateTime InDate { get; set; } = DateTime.Now;   // Ngày nhập kho
    public DateTime? OutDate { get; set; }                  // Ngày xuất kho (nếu có)
    public string? RefNo { get; set; }                     // Số chứng từ nhập/xuất liên quan (RefNo_PK)
    public string? Note { get; set; }                      // Ghi chú kiểm định / vị trí khay kệ
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>Dòng chi tiết Báo cáo & Tra cứu Serial / IMEI hàng tồn kho (port từ Inv_InventoryBalanceSerial Skycic).</summary>
public record StockSerialRow(
    int Id,
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    string SerialNo,
    string? LotNo,
    DateTime InDate,
    DateTime? OutDate,
    string? RefNo,
    StockSerialStatus Status,
    string StatusLabel,
    string BadgeClass,
    string? Note
);

/// <summary>Báo cáo Quản lý & Tra cứu Serial / IMEI tổng hợp (port từ Inv_InventoryBalanceSerial Skycic).</summary>
public record StockSerialReport(
    int? WarehouseId,
    string WarehouseName,
    int? ProductId,
    string ProductName,
    StockSerialStatus? StatusFilter,
    string? Keyword,
    int TotalSerials,
    int AvailableCount,
    int LockedCount,
    int DamagedNGCount,
    int ExportedCount,
    List<StockSerialRow> Rows
);

/// <summary>Vị trí lưu kho / Khay kệ / Ô lưu trữ trong kho (Warehouse Location / Block / Shelf - port từ Mst_InventoryBlock Skycic).</summary>
public class InventoryBlock : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int WarehouseId { get; set; }                     // Thuộc kho nào
    public string InvBlockCode { get; set; } = "";           // Mã vị trí ô kho (vd: A-01-01, B-02-04)
    public string ShelfCode { get; set; } = "";              // Mã dãy kệ (vd: SHELF-A, SHELF-B)
    public string? InvBlockDesc { get; set; }                // Mô tả vị trí (vd: Dãy A - Tầng 1 - Khoang 1)
    public double Length { get; set; } = 0;                  // Chiều dài (cm)
    public double Width { get; set; } = 0;                   // Chiều rộng (cm)
    public double Height { get; set; } = 0;                  // Chiều cao (cm)
    public int MaxCapacity { get; set; } = 100;              // Sức chứa tối đa (đơn vị hàng)
    public bool FlagActive { get; set; } = true;             // 1: Hoạt động / Sẵn sàng, 0: Tạm ngừng / Bảo trì
    public string? Remark { get; set; }                      // Ghi chú (điều kiện bảo quản, khu vực mát...)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Thể tích khối tính bằng m3 = (Length * Width * Height) / 1,000,000</summary>
    public double VolumeM3 => Math.Round((Length * Width * Height) / 1000000.0, 3);
}

/// <summary>Dòng hiển thị Vị trí kho kèm thống kê.</summary>
public record InventoryBlockRow(
    int Id,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    string InvBlockCode,
    string ShelfCode,
    string? InvBlockDesc,
    double Length,
    double Width,
    double Height,
    double VolumeM3,
    int MaxCapacity,
    bool FlagActive,
    string StatusLabel,
    string BadgeClass,
    string? Remark,
    DateTime CreatedAt
);

/// <summary>Báo cáo & Danh sách Quản lý Vị trí kho tổng hợp (port từ Mst_InventoryBlock Skycic).</summary>
public record InventoryBlockReport(
    int? WarehouseId,
    string WarehouseName,
    string? ShelfCode,
    bool? ActiveFilter,
    string? Keyword,
    int TotalBlocks,
    int ActiveCount,
    int MaintenanceCount,
    int TotalShelves,
    double TotalVolumeM3,
    int TotalCapacity,
    List<InventoryBlockRow> Rows
);

/// <summary>Nguồn gốc thiết lập / phát sinh giá vốn (port từ Inv_CostPriceHist Skycic).</summary>
public enum CostPriceSourceType
{
    AutoCalc = 0, // Tính tự động từ kỳ tính giá vốn kho (theo phiếu nhập kho)
    Manual = 1    // Thiết lập / điều chỉnh thủ công
}

/// <summary>Lịch sử & Tính giá vốn kho hàng hoá (Cost Price History & Calculation - port từ Inv_CostPriceHist Skycic).</summary>
public class CostPriceHist : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int? WarehouseId { get; set; }                    // Áp dụng cho kho cụ thể (null = toàn hệ thống)
    public int ProductId { get; set; }                       // Mặt hàng
    public DateTime EffectDate { get; set; } = DateTime.Now; // Thời điểm hiệu lực (EffectDTimeUTC)
    public decimal CostPrice { get; set; } = 0m;             // Giá vốn đơn vị kho (UPInv)
    public string? RefDocNo { get; set; }                    // Mã chứng từ / Số phiếu / Kỳ tính giá vốn (FormNo)
    public bool IsCurrent { get; set; } = true;              // Cờ giá vốn hiện hành đang áp dụng (FlagIsCurrent)
    public string? CalcPeriodName { get; set; }              // Tên kỳ tính giá vốn (nếu tính theo kỳ)
    public CostPriceSourceType SourceType { get; set; } = CostPriceSourceType.AutoCalc; // Nguồn tính giá vốn
    public string? Remark { get; set; }                      // Ghi chú lý do cập nhật/điều chỉnh
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Warehouse? Warehouse { get; set; }
    public Product Product { get; set; } = null!;
}

/// <summary>Dòng hiển thị Lịch sử giá vốn kho (port từ Inv_CostPriceHist Skycic).</summary>
public record CostPriceHistRow(
    int Id,
    int? WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    DateTime EffectDate,
    decimal CostPrice,
    string? RefDocNo,
    bool IsCurrent,
    string? CalcPeriodName,
    CostPriceSourceType SourceType,
    string SourceTypeLabel,
    string BadgeClass,
    string? Remark,
    string CreatedBy,
    DateTime CreatedAt,
    string? UpdatedBy,
    DateTime? UpdatedAt
);

/// <summary>Báo cáo & Danh sách Lịch sử giá vốn kho tổng hợp (port từ Inv_CostPriceHist Skycic).</summary>
public record CostPriceHistReport(
    int? WarehouseId,
    string WarehouseName,
    int? ProductId,
    string ProductName,
    bool? CurrentOnly,
    string? Keyword,
    DateTime? FromDate,
    DateTime? ToDate,
    int TotalRecords,
    int CurrentItemsCount,
    decimal AvgCostPrice,
    decimal MaxCostPrice,
    decimal MinCostPrice,
    List<CostPriceHistRow> Rows
);

/// <summary>Dòng xem trước tính toán giá vốn bình quân theo kỳ (port từ Inv_CostPriceHist_Calc Skycic).</summary>
public record CostPriceCalcItem(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    int? WarehouseId,
    string WarehouseName,
    decimal OldCostPrice,
    int InQty,
    decimal InTotalAmount,
    decimal NewCostPrice,
    decimal DiffAmount,
    double DiffPercent,
    string Note
);

/// <summary>Báo cáo xem trước kết quả tính giá vốn kho theo kỳ (port từ Inv_CostPriceHist_Calc Skycic).</summary>
public record CostPriceCalcPreviewReport(
    int? WarehouseId,
    string WarehouseName,
    DateTime FromDate,
    DateTime ToDate,
    string CalcPeriodName,
    int TotalProducts,
    int CalculatedProducts,
    int ChangedProducts,
    List<CostPriceCalcItem> Items
);

/// <summary>Trạng thái kỳ chốt tồn kho (port từ 20200407.ChotTonKho.sql & Rpt_In_Out_Inv Skycic).</summary>
public enum PeriodClosingStatus
{
    Draft = 0,     // Đang lập kỳ / Đang kiểm tra
    Closed = 1,    // Đã chốt sổ & Khóa kỳ (khóa dữ liệu quá khứ)
    Reopened = 2,  // Đã mở lại để điều chỉnh số liệu
    Cancelled = 3  // Đã hủy bỏ
}

/// <summary>Kỳ chốt sổ tồn kho tháng & Lưu vết Snapshot số dư (port từ Rpt_In_Out_Inv & 20200407.ChotTonKho.sql Skycic).</summary>
public class PeriodClosing : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";                                 // Mã kỳ chốt (vd: CK2603-001)
    public DateTime PeriodMonth { get; set; } = DateTime.Today;            // Tháng chốt sổ (ngày đầu tháng, vd 2026-03-01)
    public string PeriodName { get; set; } = "";                           // Tên kỳ chốt (vd: Kỳ chốt kho Tháng 03/2026)
    public int? WarehouseId { get; set; }                                  // Kho áp dụng (null = Toàn bộ kho)
    public PeriodClosingStatus Status { get; set; } = PeriodClosingStatus.Closed; // Trạng thái kỳ chốt
    public DateTime? ClosedAt { get; set; } = DateTime.Now;                // Thời điểm chốt sổ
    public string ClosedBy { get; set; } = "";                             // Người thực hiện chốt
    public string? Note { get; set; }                                      // Ghi chú chốt sổ
    public string? ReopenReason { get; set; }                              // Lý do mở lại kỳ
    public DateTime? ReopenedAt { get; set; }                              // Thời điểm mở lại
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public Warehouse? Warehouse { get; set; }
    public List<PeriodClosingLine> Lines { get; set; } = [];

    public int TotalItems => Lines.Count;
    public int TotalOpeningQty => Lines.Sum(l => l.OpeningQty);
    public int TotalInQty => Lines.Sum(l => l.InQty);
    public int TotalOutQty => Lines.Sum(l => l.OutQty);
    public int TotalClosingQty => Lines.Sum(l => l.ClosingQty);
    public decimal TotalClosingValue => Lines.Sum(l => l.ClosingValue);
}

/// <summary>Dòng chi tiết lưu vết Snapshot số dư kho theo mặt hàng (port từ Rpt_In_Out_Inv Skycic).</summary>
public class PeriodClosingLine : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int PeriodClosingId { get; set; }
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }

    public int OpeningQty { get; set; }                                    // Tồn đầu kỳ
    public int InQty { get; set; }                                         // Nhập trong kỳ (TotalQtyIn)
    public decimal LastInPrice { get; set; }                               // Đơn giá nhập cuối (ValLastIn)
    public decimal InAmount { get; set; }                                  // Tổng giá trị nhập (TotalValIn)
    public int OutQty { get; set; }                                        // Xuất trong kỳ (TotalQtyOut)
    public decimal LastOutPrice { get; set; }                              // Đơn giá xuất cuối (ValLastOut)
    public decimal OutAmount { get; set; }                                 // Tổng giá trị xuất (TotalValOut)
    public int ClosingQty { get; set; }                                    // Tồn cuối kỳ chốt sổ
    public decimal CostPrice { get; set; }                                 // Đơn giá vốn chốt kỳ
    public decimal ClosingValue { get; set; }                              // Tổng giá trị tồn chốt sổ (= ClosingQty * CostPrice)
    public string? Note { get; set; }                                      // Ghi chú

    public PeriodClosing PeriodClosing { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>Dòng hiển thị chi tiết Snapshot chốt tồn kho của một mặt hàng.</summary>
public record PeriodClosingRow(
    int Id,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    int OpeningQty,
    int InQty,
    decimal LastInPrice,
    decimal InAmount,
    int OutQty,
    decimal LastOutPrice,
    decimal OutAmount,
    int ClosingQty,
    decimal CostPrice,
    decimal ClosingValue,
    string? Note
);

/// <summary>Dòng xem trước tính toán dữ liệu chốt kỳ kho.</summary>
public record PeriodClosingPreviewItem(
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    int OpeningQty,
    int InQty,
    decimal InAmount,
    int OutQty,
    decimal OutAmount,
    int ClosingQty,
    decimal CostPrice,
    decimal ClosingValue
);

/// <summary>Báo cáo xem trước tính toán chốt kỳ tồn kho.</summary>
public record PeriodClosingPreviewReport(
    int? WarehouseId,
    string WarehouseName,
    int Year,
    int Month,
    DateTime PeriodMonth,
    DateTime FromDate,
    DateTime ToDate,
    string PeriodName,
    int TotalProducts,
    int TotalOpeningQty,
    int TotalInQty,
    int TotalOutQty,
    int TotalClosingQty,
    decimal TotalClosingValue,
    List<PeriodClosingPreviewItem> Items
);

/// <summary>Trạng thái vòng đời của Thùng Carton / Kiện hàng (port từ Inv_InventoryCarton Skycic).</summary>
public enum CartonStatus
{
    Empty = 0,    // Thùng rỗng / Mới khởi tạo mã thùng
    Packing = 1,  // Đang đóng kiện / Chưa niêm phong
    Sealed = 2,   // Đã niêm phong / Hoàn tất đóng gói, sẵn sàng xuất
    Shipped = 3,  // Đã xuất kho giao hàng
    Unpacked = 4  // Đã mở kiện / Tháo dỡ hoàn kho
}

/// <summary>Quản lý Thùng Carton & Đóng kiện hàng hóa trong kho (port từ Inv_InventoryCarton & Inv_GenTimesCarton Skycic).</summary>
public class InventoryCarton : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string CartonCode { get; set; } = "";                     // Mã thùng carton (CanNo, vd: CTN-2026-0001)
    public string? QrCode { get; set; }                              // Mã QR định danh dán nhãn thùng (QR_CanNo)
    public int WarehouseId { get; set; }                             // Kho lưu trữ thùng
    public string CartonType { get; set; } = "Thùng carton tiêu chuẩn"; // Loại thùng / Quy cách (CartonType)
    public int? ProductId { get; set; }                              // Mặt hàng đóng trong thùng (null nếu thùng rỗng)
    public string? LotNo { get; set; }                               // Số lô hàng (ProductLotNo)
    public int Quantity { get; set; } = 0;                           // Số lượng hàng trong thùng (Qty)
    public int Capacity { get; set; } = 50;                          // Sức chứa định mức tối đa của thùng
    public double LengthCm { get; set; } = 40;                       // Dài (cm)
    public double WidthCm { get; set; } = 30;                        // Rộng (cm)
    public double HeightCm { get; set; } = 30;                       // Cao (cm)
    public double GrossWeightKg { get; set; } = 0;                   // Trọng lượng cả bì (kg)
    public CartonStatus Status { get; set; } = CartonStatus.Empty;   // Trạng thái thùng
    public string? ShelfLocation { get; set; }                       // Vị trí lưu kho (kệ/ô)
    public string? PackerName { get; set; }                          // Người thực hiện đóng thùng
    public DateTime? PackedAt { get; set; }                          // Thời điểm đóng thùng
    public DateTime? SealedAt { get; set; }                          // Thời điểm niêm phong
    public DateTime? ShippedAt { get; set; }                         // Thời điểm xuất kho
    public string? RefDocNo { get; set; }                            // Số chứng từ xuất/nhập/lệnh liên quan
    public string? Remark { get; set; }                              // Ghi chú thùng carton
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public Product? Product { get; set; }

    /// <summary>Thể tích khối tính bằng m3 = (LengthCm * WidthCm * HeightCm) / 1,000,000</summary>
    public double VolumeM3 => Math.Round((LengthCm * WidthCm * HeightCm) / 1000000.0, 3);
}

/// <summary>Dòng hiển thị Thùng carton kèm trạng thái nhãn.</summary>
public record CartonRow(
    int Id,
    string CartonCode,
    string? QrCode,
    int WarehouseId,
    string WarehouseName,
    string CartonType,
    int? ProductId,
    string? ProductCode,
    string? ProductName,
    string? Uom,
    string? LotNo,
    int Quantity,
    int Capacity,
    double LengthCm,
    double WidthCm,
    double HeightCm,
    double VolumeM3,
    double GrossWeightKg,
    CartonStatus Status,
    string StatusLabel,
    string BadgeClass,
    string? ShelfLocation,
    string? PackerName,
    DateTime? PackedAt,
    DateTime? SealedAt,
    DateTime? ShippedAt,
    string? RefDocNo,
    string? Remark,
    DateTime CreatedAt
);

/// <summary>Báo cáo & Danh sách Quản lý Thùng Carton tổng hợp (port từ Inv_InventoryCarton Skycic).</summary>
public record CartonReport(
    int? WarehouseId,
    string WarehouseName,
    int? ProductId,
    string? ProductName,
    CartonStatus? StatusFilter,
    string? Keyword,
    int TotalCartons,
    int EmptyCount,
    int PackingCount,
    int SealedCount,
    int ShippedCount,
    int TotalItemsPacked,
    double TotalVolumeM3,
    double TotalWeightKg,
    List<CartonRow> Rows
);

/// <summary>Hình thức nhập kho thành phẩm (port từ InvF_InventoryInFG - FormInType Skycic).</summary>
public enum InvInFGFormType
{
    InternalProduction = 0, // Sản xuất hoàn thành nhập kho nội bộ (SX_NOIBO)
    Outsourced = 1,         // Nhập từ đơn vị gia công / OEM (GIA_CONG)
    AssemblyPack = 2,       // Nhập từ hoàn thiện đóng gói / Kẹp chì (DONG_GOI)
    WarrantyRefurbish = 3   // Nhập thu hồi tân trang sau bảo hành (BAO_HANH)
}

/// <summary>Trạng thái phiếu nhập kho thành phẩm (port từ InvF_InventoryInFG - IF_InvInFGStatus Skycic).</summary>
public enum InvInFGStatus
{
    Pending = 0,   // Mới tạo / Chờ KCS & Quản đốc duyệt nhập kho
    Approved = 1,  // Đã duyệt & Nhập kho (tự động tăng tồn kho, ghi thẻ kho & kích hoạt Serial)
    Cancelled = 2  // Đã hủy phiếu
}

/// <summary>Phiếu nhập kho thành phẩm sản xuất (port từ InvF_InventoryInFG Skycic). Quản lý tiếp nhận thành phẩm hoàn thành từ xưởng sản xuất / gia công vào kho thành phẩm.</summary>
public class InventoryInFG : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";                                 // Mã phiếu nhập TP (IF_InvInFGNo, vd: IFFG2603-001)
    public int WarehouseId { get; set; }                                   // Kho nhập thành phẩm (InvCode)
    public InvInFGFormType FormType { get; set; } = InvInFGFormType.InternalProduction; // Hình thức nhập (FormInType)
    public string WorkshopName { get; set; } = "";                         // Phân xưởng / Nhà máy sản xuất (DLCode / MST)
    public string? WorkOrderNo { get; set; }                               // Số lệnh sản xuất / Lô sản xuất (WorkOrderNo / BatchNo)
    public string? ShiftLeader { get; set; }                               // Quản đốc / Trưởng ca sản xuất phụ trách
    public DateTime Date { get; set; } = DateTime.Now;                     // Ngày nhập kho
    public string CreatedBy { get; set; } = "";                            // Người lập phiếu
    public InvInFGStatus Status { get; set; } = InvInFGStatus.Pending;     // Trạng thái phiếu
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ApprovedAt { get; set; }                              // Thời điểm phê duyệt
    public string? ApprovedBy { get; set; }                                // Người phê duyệt KCS / Thủ kho
    public string? Remark { get; set; }                                    // Diễn giải / Ghi chú
    public int? StockDocId { get; set; }                                   // Phiếu nhập kho tự động sinh ra khi duyệt (StockDoc.Type = In)

    public Warehouse Warehouse { get; set; } = null!;
    public StockDoc? StockDoc { get; set; }
    public List<InventoryInFGLine> Lines { get; set; } = [];
    public List<InventoryInFGSerial> Serials { get; set; } = [];

    public int TotalPlanQty => Lines.Sum(l => l.PlanQty);
    public int TotalActualQty => Lines.Sum(l => l.ActualQty);
    public int TotalDefectQty => Lines.Sum(l => l.DefectQty);
    public decimal TotalAmount => Lines.Sum(l => l.Amount);
    public int TotalSerialsCount => Serials.Count;
    public double PassRatePercent => TotalPlanQty > 0 ? Math.Round((double)TotalActualQty / TotalPlanQty * 100, 1) : 100.0;
}

/// <summary>Dòng chi tiết mặt hàng thành phẩm trong phiếu nhập kho (port từ InvF_InventoryInFGDtl Skycic).</summary>
public class InventoryInFGLine : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int InventoryInFGId { get; set; }
    public int ProductId { get; set; }
    public int PlanQty { get; set; }                                       // Số lượng theo lệnh sản xuất
    public int ActualQty { get; set; }                                     // Số lượng thực nhập đạt chuẩn KCS
    public int DefectQty { get; set; } = 0;                                // Số lượng lỗi / phế phẩm loại ra
    public decimal UnitCost { get; set; }                                  // Đơn giá thành phẩm / Chi phí sản xuất đơn vị
    public DateTime? ProductionDate { get; set; }                          // Ngày sản xuất
    public string? Note { get; set; }                                      // Ghi chú chi tiết

    public InventoryInFG InventoryInFG { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal Amount => ActualQty * UnitCost;
    public double PassRate => PlanQty > 0 ? Math.Round((double)ActualQty / PlanQty * 100, 1) : 100.0;
}

/// <summary>Danh sách Barcode / Serial / IMEI cá thể hóa gắn với phiếu nhập kho thành phẩm (port từ InvF_InventoryInFGInstSerial Skycic).</summary>
public class InventoryInFGSerial : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int InventoryInFGId { get; set; }
    public int ProductId { get; set; }
    public string SerialNo { get; set; } = "";                             // Số Barcode / Serial / IMEI (SerialNo)
    public string? Note { get; set; }                                      // Ghi chú

    public InventoryInFG InventoryInFG { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>Dòng hiển thị danh sách phiếu nhập kho thành phẩm.</summary>
public record InventoryInFGRow(
    int Id,
    string Code,
    int WarehouseId,
    string WarehouseName,
    InvInFGFormType FormType,
    string FormTypeLabel,
    string WorkshopName,
    string? WorkOrderNo,
    string? ShiftLeader,
    DateTime Date,
    InvInFGStatus Status,
    string StatusLabel,
    string BadgeClass,
    int TotalPlanQty,
    int TotalActualQty,
    int TotalDefectQty,
    double PassRatePercent,
    decimal TotalAmount,
    int TotalSerialsCount,
    int? StockDocId,
    string? StockDocCode,
    string? Remark,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    string? ApprovedBy
);

/// <summary>Báo cáo & Tổng hợp danh sách Phiếu nhập kho thành phẩm.</summary>
public record InventoryInFGReport(
    int? WarehouseId,
    string WarehouseName,
    InvInFGStatus? StatusFilter,
    InvInFGFormType? FormTypeFilter,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Keyword,
    int TotalReceipts,
    int PendingCount,
    int ApprovedCount,
    int CancelledCount,
    int TotalPlanQty,
    int TotalActualQty,
    int TotalDefectQty,
    decimal TotalAmount,
    List<InventoryInFGRow> Rows
);

/// <summary>Hình thức xuất kho thành phẩm (port từ InvF_InventoryOutFG - FormOutType Skycic).</summary>
public enum InvOutFGFormType
{
    QuantityOnly = 0,  // Không mã vạch / Xuất theo số lượng thông thường (KHONGMAVACH)
    BarcodeSerial = 1  // Quét mã vạch / Serial cá thể hóa (MAVACH)
}

/// <summary>Loại nghiệp vụ xuất kho thành phẩm (port từ InvF_InventoryOutFG - InvFOutType Skycic).</summary>
public enum InvOutFGType
{
    Commercial = 0,    // Xuất thương mại / Đại lý phân phối bán buôn (OUTTHUONGMAI)
    EndCustomer = 1,   // Xuất khách hàng lẻ / Công trình dự án (OUTENDCUS)
    BranchTransfer = 2,// Xuất điều chuyển chi nhánh / Showroom trưng bày (TRANSFER)
    WarrantyScrap = 3  // Xuất bảo hành / Đổi trả / Hủy mẫu thử lỗi (WARRANTY)
}

/// <summary>Trạng thái phiếu xuất kho thành phẩm (port từ InvF_InventoryOutFG - IF_InvOutFGStatus Skycic).</summary>
public enum InvOutFGStatus
{
    Pending = 0,   // Mới lập / Chờ thủ kho & bảo vệ duyệt xuất hàng
    Approved = 1,  // Đã duyệt xuất kho (tự động trừ tồn kho, ghi thẻ kho & xuất Serial)
    Cancelled = 2  // Đã hủy phiếu
}

/// <summary>Phiếu xuất kho thành phẩm sản xuất & phân phối (port từ InvF_InventoryOutFG Skycic). Quản lý xuất hàng giao đại lý, vận chuyển xe tải, container & xuất serial.</summary>
public class InventoryOutFG : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";                                 // Mã phiếu xuất TP (IF_InvOutFGNo, vd: IFOFG2603-001)
    public int WarehouseId { get; set; }                                   // Kho xuất thành phẩm (InvCode)
    public InvOutFGType OutType { get; set; } = InvOutFGType.Commercial;   // Loại hình xuất (InvFOutType)
    public InvOutFGFormType FormType { get; set; } = InvOutFGFormType.QuantityOnly; // Hình thức xuất (FormOutType)
    public string CustomerName { get; set; } = "";                         // Tên khách hàng / Đại lý nhận hàng (CustomerName)
    public string? AgentCode { get; set; }                                 // Mã đại lý / Khách hàng (AgentCode / MST)
    public string? DeliveryAddress { get; set; }                           // Địa chỉ nhận hàng / Công trình
    public string? DriverName { get; set; }                                // Tên lái xe giao nhận (DriverName)
    public string? DriverPhone { get; set; }                               // SĐT lái xe (DriverPhoneNo)
    public string? PlateNo { get; set; }                                   // Biển số xe vận chuyển (PlateNo)
    public string? MoocNo { get; set; }                                    // Biển số rơ-moóc / Container (MoocNo)
    public string? OrderNo { get; set; }                                   // Số đơn hàng / Hợp đồng mua bán
    public DateTime Date { get; set; } = DateTime.Now;                     // Ngày xuất kho
    public string CreatedBy { get; set; } = "";                            // Người lập phiếu
    public InvOutFGStatus Status { get; set; } = InvOutFGStatus.Pending;   // Trạng thái phiếu
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ApprovedAt { get; set; }                              // Thời điểm phê duyệt xuất kho
    public string? ApprovedBy { get; set; }                                // Người phê duyệt xuất kho
    public string? Remark { get; set; }                                    // Diễn giải / Ghi chú
    public int? StockDocId { get; set; }                                   // Phiếu xuất kho tự động sinh ra khi duyệt (StockDoc.Type = Out)

    public Warehouse Warehouse { get; set; } = null!;
    public StockDoc? StockDoc { get; set; }
    public List<InventoryOutFGLine> Lines { get; set; } = [];
    public List<InventoryOutFGSerial> Serials { get; set; } = [];

    public int TotalQty => Lines.Sum(l => l.Qty);
    public decimal TotalAmount => Lines.Sum(l => l.Amount);
    public int TotalSerialsCount => Serials.Count;
    public int TotalItemsCount => Lines.Count;
}

/// <summary>Dòng chi tiết mặt hàng thành phẩm trong phiếu xuất kho (port từ InvF_InventoryOutFGDtl Skycic).</summary>
public class InventoryOutFGLine : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int InventoryOutFGId { get; set; }
    public int ProductId { get; set; }
    public int Qty { get; set; }                                           // Số lượng xuất kho
    public decimal UnitCost { get; set; }                                  // Giá vốn đơn vị (UPInv)
    public decimal UnitPrice { get; set; }                                 // Đơn giá xuất / Giá bán phân phối
    public string? Note { get; set; }                                      // Ghi chú chi tiết

    public InventoryOutFG InventoryOutFG { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal Amount => Qty * UnitPrice;
    public decimal CostAmount => Qty * UnitCost;
}

/// <summary>Danh sách Barcode / Serial / IMEI gắn với phiếu xuất kho thành phẩm (port từ InvF_InventoryOutFGInstSerial Skycic).</summary>
public class InventoryOutFGSerial : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int InventoryOutFGId { get; set; }
    public int ProductId { get; set; }
    public string SerialNo { get; set; } = "";                             // Số Barcode / Serial / IMEI xuất kho (SerialNo)
    public string? Note { get; set; }                                      // Ghi chú

    public InventoryOutFG InventoryOutFG { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>Dòng hiển thị danh sách phiếu xuất kho thành phẩm.</summary>
public record InventoryOutFGRow(
    int Id,
    string Code,
    int WarehouseId,
    string WarehouseName,
    InvOutFGType OutType,
    string OutTypeLabel,
    InvOutFGFormType FormType,
    string FormTypeLabel,
    string CustomerName,
    string? AgentCode,
    string? DeliveryAddress,
    string? DriverName,
    string? DriverPhone,
    string? PlateNo,
    string? MoocNo,
    string? OrderNo,
    DateTime Date,
    InvOutFGStatus Status,
    string StatusLabel,
    string BadgeClass,
    int TotalQty,
    decimal TotalAmount,
    int TotalSerialsCount,
    int? StockDocId,
    string? StockDocCode,
    string? Remark,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    string? ApprovedBy
);

/// <summary>Báo cáo & Tổng hợp danh sách Phiếu xuất kho thành phẩm.</summary>
public record InventoryOutFGReport(
    int? WarehouseId,
    string WarehouseName,
    InvOutFGStatus? StatusFilter,
    InvOutFGType? OutTypeFilter,
    InvOutFGFormType? FormTypeFilter,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Keyword,
    int TotalOrders,
    int PendingCount,
    int ApprovedCount,
    int CancelledCount,
    int TotalQty,
    decimal TotalAmount,
    int TotalSerialsCount,
    List<InventoryOutFGRow> Rows
);

/// <summary>Trạng thái vòng đời của Hộp đóng gói / Inner Box (port từ Inv_InventoryBox Skycic).</summary>
public enum BoxStatus
{
    Empty = 0,    // Hộp rỗng / Mới khởi tạo mã hộp (FlagUsed = 0)
    Packing = 1,  // Đang đóng hàng dở dang / Chưa niêm phong
    Sealed = 2,   // Đã niêm phong / Hoàn tất đóng gói, sẵn sàng gán thùng hoặc xuất lẻ
    InCarton = 3, // Đã đóng vào thùng Carton Master (FlagMap = 1)
    Shipped = 4,  // Đã xuất kho giao hàng
    Unpacked = 5  // Đã mở hộp / Tháo dỡ hoàn kho
}

/// <summary>Quản lý Hộp đóng gói & Phân cấp bao bì kho (Warehouse Box Packaging - port từ Inv_InventoryBox & Inv_GenTimesBox Skycic).</summary>
public class InventoryBox : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string BoxCode { get; set; } = "";                         // Mã hộp (BoxNo, vd: BOX2603-HN01)
    public string? QrCode { get; set; }                              // Mã QR định danh dán nhãn nắp hộp (QR_BoxNo)
    public string? GenTimesBoxNo { get; set; }                       // Đợt sinh mã hộp (GenTimesBoxNo)
    public string? SecretNo { get; set; }                            // Số niêm phong / Mã cào bảo mật tem chống giả (SecretNo)
    public int WarehouseId { get; set; }                             // Kho lưu trữ hộp
    public int? CartonId { get; set; }                               // Thùng Carton chứa hộp này (nếu đã đóng vào thùng)
    public string BoxType { get; set; } = "Hộp duplex tiêu chuẩn";   // Quy cách loại hộp
    public int? ProductId { get; set; }                              // Mặt hàng đóng trong hộp
    public string? LotNo { get; set; }                               // Số lô hàng (ProductLotNo)
    public int Quantity { get; set; } = 0;                           // Số lượng hàng trong hộp (Qty)
    public int Capacity { get; set; } = 10;                          // Sức chứa định mức tối đa của hộp
    public double LengthCm { get; set; } = 20;                       // Dài (cm)
    public double WidthCm { get; set; } = 15;                        // Rộng (cm)
    public double HeightCm { get; set; } = 10;                       // Cao (cm)
    public double GrossWeightKg { get; set; } = 0;                   // Trọng lượng cả bì (kg)
    public BoxStatus Status { get; set; } = BoxStatus.Empty;         // Trạng thái vòng đời hộp
    public bool FlagMap { get; set; } = false;                       // Trạng thái gán vào thùng Carton (0: Chưa gán, 1: Đã gán)
    public bool FlagUsed { get; set; } = false;                      // Cờ đã đóng hàng / in tem (0: Chưa dùng, 1: Đã dùng)
    public string? ShelfLocation { get; set; }                       // Vị trí lưu kho (kệ/ô) nếu để riêng ngoài thùng
    public string? PackerName { get; set; }                          // Người thực hiện đóng hộp
    public DateTime? PackedAt { get; set; }                          // Thời điểm đóng hộp
    public DateTime? SealedAt { get; set; }                          // Thời điểm niêm phong
    public DateTime? ShippedAt { get; set; }                         // Thời điểm xuất kho
    public string? RefDocNo { get; set; }                            // Số chứng từ xuất/nhập/lệnh liên quan
    public string? Remark { get; set; }                              // Ghi chú hộp hàng
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public InventoryCarton? Carton { get; set; }
    public Product? Product { get; set; }

    /// <summary>Thể tích khối tính bằng m3 = (LengthCm * WidthCm * HeightCm) / 1,000,000</summary>
    public double VolumeM3 => Math.Round((LengthCm * WidthCm * HeightCm) / 1000000.0, 4);
}

/// <summary>Dòng hiển thị Hộp đóng gói kèm trạng thái bao bì và phân cấp thùng.</summary>
public record BoxRow(
    int Id,
    string BoxCode,
    string? QrCode,
    string? GenTimesBoxNo,
    string? SecretNo,
    int WarehouseId,
    string WarehouseName,
    int? CartonId,
    string? CartonCode,
    string BoxType,
    int? ProductId,
    string? ProductCode,
    string? ProductName,
    string? Uom,
    string? LotNo,
    int Quantity,
    int Capacity,
    double LengthCm,
    double WidthCm,
    double HeightCm,
    double VolumeM3,
    double GrossWeightKg,
    BoxStatus Status,
    string StatusLabel,
    string BadgeClass,
    bool FlagMap,
    string MapLabel,
    string MapBadgeClass,
    bool FlagUsed,
    string? ShelfLocation,
    string? PackerName,
    DateTime? PackedAt,
    DateTime? SealedAt,
    DateTime? ShippedAt,
    string? RefDocNo,
    string? Remark,
    DateTime CreatedAt
);

/// <summary>Báo cáo & Tổng hợp Danh sách Quản lý Hộp đóng gói (port từ Inv_InventoryBox Skycic).</summary>
public record BoxReport(
    int? WarehouseId,
    string WarehouseName,
    int? ProductId,
    string? ProductName,
    int? CartonId,
    string? CartonCode,
    BoxStatus? StatusFilter,
    bool? FlagMapFilter,
    string? Keyword,
    int TotalBoxes,
    int EmptyCount,
    int PackingCount,
    int SealedCount,
    int InCartonCount,
    int ShippedCount,
    int TotalItemsPacked,
    double TotalVolumeM3,
    double TotalWeightKg,
    List<BoxRow> Rows
);

/// <summary>Dòng báo cáo chi tiết xuất kho (port từ Rpt_InvF_InventoryOutDtl Skycic).</summary>
public record InventoryOutDtlItem(
    int Id,
    string DocNo,
    DateTime DocDate,
    string OutType,
    string OutTypeName,
    string? RefNo,
    string? RefType,
    int WarehouseId,
    string WarehouseName,
    string? CustomerCode,
    string CustomerName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string UnitName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    string CreatedBy,
    string? Note,
    string? DocUrl
);

/// <summary>Báo cáo tổng hợp xuất kho chi tiết (port từ Rpt_InvF_InventoryOutDtl Skycic).</summary>
public record InventoryOutDtlReport(
    DateTime FromDate,
    DateTime ToDate,
    int? WarehouseId,
    string WarehouseName,
    string? OutTypeFilter,
    string? Keyword,
    List<InventoryOutDtlItem> Items,
    int TotalDocsCount,
    int TotalQty,
    decimal TotalCostAmount,
    int DistinctProductsCount
);

/// <summary>Dòng báo cáo chi tiết nhập kho (port từ Rpt_InventoryInDtl Skycic).</summary>
public record InventoryInDtlItem(
    int Id,
    string DocNo,
    DateTime DocDate,
    string InType,
    string InTypeName,
    string? RefNo,
    string? RefType,
    int WarehouseId,
    string WarehouseName,
    string? LocationCode,
    string? SupplierCode,
    string SupplierName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string UnitName,
    int Quantity,
    decimal UnitPrice,
    decimal VatPercent,
    decimal ValBeforeTax,
    decimal ValTax,
    decimal TotalAmount,
    string? InvoiceNo,
    DateTime? InvoiceDate,
    string CreatedBy,
    string? Note,
    string? DocUrl
);

/// <summary>Báo cáo tổng hợp nhập kho chi tiết (port từ Rpt_InventoryInDtl Skycic).</summary>
public record InventoryInDtlReport(
    DateTime FromDate,
    DateTime ToDate,
    int? WarehouseId,
    string WarehouseName,
    string? InTypeFilter,
    string? Keyword,
    List<InventoryInDtlItem> Items,
    int TotalDocsCount,
    int TotalQty,
    decimal TotalBeforeTax,
    decimal TotalTaxAmount,
    decimal TotalAmount,
    int DistinctProductsCount,
    int DistinctSuppliersCount
);








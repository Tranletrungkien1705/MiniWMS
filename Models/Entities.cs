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



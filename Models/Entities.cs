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
    public string? InvTypeCode { get; set; } // Phân loại Loại kho (port từ Mst_InventoryType Skycic: KHO_TONG, KHO_NVL, KHO_TP, KHO_TC, KHO_BH, KHO_DL)
    public string? InvLevelTypeCode { get; set; } // Phân loại Cấp kho (port từ Mst_InventoryLevelType Skycic: CAP_1, CAP_2, CAP_3, HUB, KHO_DAILY)
    public string? AreaCode { get; set; }    // Phân loại Vùng / Khu vực kho (port từ Mst_Area Skycic: AREA_MB, AREA_MN, AREA_MT...)
    public string? Remark { get; set; }      // Ghi chú / Mục đích sử dụng kho
}

public class Product : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? PartTypeCode { get; set; } // Phân loại loại mặt hàng (port từ Mst_PartType: TP, BTP, NVL, PTLK, BBDG, CCDC, HHTM)
    public string? BrandCode { get; set; }    // Thương hiệu / Nhãn hiệu hàng hóa (port từ Mst_Brand: MAY10, VIETTIEN, ANPHUOC, LEVI...)
    public string? ModelCode { get; set; }    // Dòng sản phẩm / Model hàng hóa (port từ Mst_Model / OS_PrdCenter_Mst_Model: MD-M10-SLIM, MD-LV-501...)
    public string? PMType { get; set; }       // Nhóm chất liệu / Loại vật liệu hàng hóa (port từ Mst_PartMaterialType Skycic: COTTON, KAKI, LEATHER...)
    public string? ProductGrpCode { get; set; } // Phân nhóm hàng hóa / Nhóm sản phẩm (port từ Mst_ProductGroup Skycic: GRP_THOI_TRANG, GRP_AO_SM, GRP_QUAN_JEAN...)
    public string? SpecCode { get; set; }     // Quy cách / Cấu hình kỹ thuật sản phẩm (port từ Mst_Spec / OS_PrdCenter_Mst_Spec Skycic: SPC-AO-SM-TRANG-L, SPC-QUAN-JN-DEN-32...)
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
    public string? CustomerCode { get; set; }   // Mã khách hàng (khi xuất kho bán hàng / giao đại lý)
    public string? CustomerName { get; set; }   // Tên khách hàng (khi xuất kho bán hàng / giao đại lý)
    public string? DepartmentCode { get; set; } // Mã bộ phận / phòng ban nhận cấp phát / xuất dùng nội bộ (port từ Mst_Department Skycic)
    public string? DepartmentName { get; set; } // Tên bộ phận / phòng ban nhận hàng
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
    public string? MoveOrdTypeCode { get; set; } // Mã loại hình điều chuyển (port từ Mst_MoveOrdType Skycic: MOVE_BRANCH, MOVE_REPLENISH, MOVE_TRANSIT, MOVE_WARRANTY, MOVE_REORG, MOVE_DISPOSAL)
    public string? MoveOrdTypeName { get; set; } // Tên loại hình điều chuyển kho
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

/// <summary>Danh mục Khách hàng, Đại lý phân phối & Điểm nhận hàng xuất kho (port từ Mst_Customer Skycic).</summary>
public class Customer : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // Mã khách hàng (CustomerCode: KH-001, DL-MB01...)
    public string Name { get; set; } = "";             // Tên khách hàng / Đại lý / Tổ chức (CustomerName)
    public string CustomerType { get; set; } = "Đại lý phân phối"; // Loại khách hàng (CustomerType: Đại lý phân phối, Bán buôn B2B, Dự án, Khách lẻ)
    public string? Phone { get; set; }                 // Điện thoại (CustomerPhoneNo / CustomerMobilePhone)
    public string? Email { get; set; }                 // Email (CustomerEmail)
    public string? Address { get; set; }               // Địa chỉ nhận hàng / giao hàng (CustomerAddress)
    public string? Province { get; set; }              // Tỉnh / Thành phố (ProvinceCode)
    public string? AreaCode { get; set; }              // Vùng / Khu vực thị trường (port từ Mst_Area / Mst_CustomerInArea Skycic)
    public string? CustomerGrpCode { get; set; }       // Nhóm khách hàng / đại lý (port từ Mst_CustomerGroup Skycic: CustomerGrpCode)
    public string? CustomerSourceCode { get; set; }    // Nguồn khách hàng / Kênh tiếp nhận (port từ Mst_CustomerSource Skycic: CustomerSourceCode)
    public string? ContactName { get; set; }           // Người đại diện / liên hệ (ContactName)
    public string? ContactPhone { get; set; }          // Điện thoại người liên hệ (ContactPhone)
    public string? TaxCode { get; set; }               // Mã số thuế (TaxCode)
    public bool IsActive { get; set; } = true;         // Trạng thái hoạt động (FlagActive)
    public string? Note { get; set; }                  // Ghi chú / Hạn mức công nợ (Remark)
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

/// <summary>Phân nhóm tuổi tồn theo THÁNG lưu kho (port từ Rpt_Inv_InventoryBalance_ByStorageMonth Skycic).</summary>
public enum StorageMonthBracket
{
    Under3Months = 0,   // < 3 tháng: Hàng mới nhập, luân chuyển tốt
    From3To6Months = 1, // 3 - 6 tháng: Lưu kho bình thường
    From6To12Months = 2,// 6 tháng - 1 năm: Cần lưu ý
    From12To24Months = 3,// 1 - 2 năm: Tồn lâu
    Over24Months = 4    // > 2 năm: Tồn đọng vốn nghiêm trọng
}

/// <summary>Dòng chi tiết Báo cáo Tồn kho theo Tuổi tồn (tháng) & Nhóm hàng (port từ Rpt_Inv_InventoryBalance_ByStorageMonth Skycic).</summary>
public record StorageMonthRow(
    string ProductGrpCode,
    string ProductGrpName,
    string? ProductGrpDesc,
    StorageMonthBracket Bracket,
    string BracketLabel,
    string BadgeClass,
    decimal TotalValue,
    double InvPercent
);

/// <summary>Báo cáo Tồn kho theo Tuổi tồn (tháng) & Nhóm hàng tổng hợp (port từ Rpt_Inv_InventoryBalance_ByStorageMonth Skycic).</summary>
public record StorageMonthReport(
    int? WarehouseId,
    string WarehouseName,
    DateTime AsOfDate,
    StorageMonthBracket? BracketFilter,
    string? Keyword,
    decimal GrandTotalValue,
    int TotalGroups,
    decimal Under3MonthsValue,
    decimal From3To6MonthsValue,
    decimal From6To12MonthsValue,
    decimal From12To24MonthsValue,
    decimal Over24MonthsValue,
    decimal StagnantValue,
    double StagnantPercent,
    List<StorageMonthRow> Rows
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

/// <summary>Dòng chỉ tiêu theo 12 tháng của 1 mặt hàng (port từ Rpt_Summary_In_Out Skycic).</summary>
public record MonthlyMatrixRow(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    string ActionType,    // IN, OUT, NET, BALANCE
    string ActionLabel,   // Nhập kho, Xuất kho, Biến động ròng, Tồn cuối kỳ
    string BadgeClass,
    int M1,
    int M2,
    int M3,
    int M4,
    int M5,
    int M6,
    int M7,
    int M8,
    int M9,
    int M10,
    int M11,
    int M12,
    int TotalYear,
    double AvgMonth,
    int PeakMonth
);

/// <summary>Khối ma trận đầy đủ của một mặt hàng gồm các chỉ tiêu Nhập, Xuất, Ròng, Tồn cuối tháng.</summary>
public class ProductMonthlyMatrixItem
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Uom { get; set; } = "";
    public int OpeningYearQty { get; set; } // Tồn đầu năm
    public MonthlyMatrixRow? InRow { get; set; }
    public MonthlyMatrixRow? OutRow { get; set; }
    public MonthlyMatrixRow? NetRow { get; set; }
    public MonthlyMatrixRow? BalanceRow { get; set; }
    public int TotalInYear => InRow?.TotalYear ?? 0;
    public int TotalOutYear => OutRow?.TotalYear ?? 0;
    public int NetYear => TotalInYear - TotalOutYear;
    public int ClosingYearQty => BalanceRow?.M12 ?? 0;
}

/// <summary>Dòng tổng hợp tồn kho 12 tháng theo kỳ (port từ Rpt_Summary_QtyInvByPeriod Skycic).</summary>
public record SummaryQtyPeriodRow(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    int OpeningYearQty,
    int M1,
    int M2,
    int M3,
    int M4,
    int M5,
    int M6,
    int M7,
    int M8,
    int M9,
    int M10,
    int M11,
    int M12,
    int ClosingYearQty,
    int MinQty,
    int MaxQty,
    double AvgQty
);

/// <summary>Báo cáo Ma trận Tổng hợp Nhập - Xuất & Tồn kho 12 Tháng (port từ Rpt_Summary_In_Out & Rpt_Summary_QtyInvByPeriod Skycic).</summary>
public record MonthlyMatrixReport(
    int Year,
    int? WarehouseId,
    string WarehouseName,
    string ViewMode,      // ALL, IN_ONLY, OUT_ONLY, BALANCE_ONLY, NET_ONLY
    string? Keyword,
    int TotalInYear,
    int TotalOutYear,
    int NetMovementYear,
    int PeakMonth,
    string PeakMonthName,
    int PeakMonthVolume,
    int[] MonthlyTotalIn,
    int[] MonthlyTotalOut,
    int[] MonthlyTotalNet,
    int[] MonthlyTotalBalance,
    List<ProductMonthlyMatrixItem> Items,
    List<SummaryQtyPeriodRow> QtyPeriodRows
);

/// <summary>Trạng thái định mức tồn kho mở rộng (port từ Rpt_Inv_InventoryBalance_Extend Skycic).</summary>
public enum StockExtendStatus
{
    All = 0,
    OutOfStock = 1,  // Hết hàng khả dụng (QtyAvailOK <= 0)
    UnderMin = 2,    // Dưới định mức an toàn tối thiểu (QtyAvailOK < MinStock)
    Optimal = 3,     // Đạt chuẩn định mức an toàn (MinStock <= QtyAvailOK <= MaxStock)
    OverMax = 4      // Vượt định mức tối đa (QtyAvailOK > MaxStock)
}

/// <summary>Dòng báo cáo tồn kho mở rộng & dự phóng khả dụng (port từ Rpt_Inv_InventoryBalance_Extend Skycic).</summary>
public record StockExtendRow(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    int WarehouseId,
    string WarehouseName,
    int QtyTotalOK,       // Tổng tồn vật lý thực tế trên sổ sách (Physical On-hand)
    int QtyBlockOK,       // Số lượng bị khóa / giữ chỗ / phong tỏa (Blocked / Reserved)
    int QtyAvailOK,       // Số lượng khả dụng sẵn sàng xuất bán (Available to Promise = QtyTotalOK - QtyBlockOK)
    double AvailRate,     // Tỷ lệ khả dụng % = QtyAvailOK / QtyTotalOK * 100
    int QtyBackOrder,     // Hàng sắp về / Đang trên đường nhập (Back-order / On order)
    int QtyStockExt,      // Tồn kho mở rộng dự phóng = QtyAvailOK + QtyBackOrder
    int MinStock,         // Tồn an toàn tối thiểu (QtyMinSt)
    int MaxStock,         // Tồn định mức tối đa (QtyMaxSt)
    decimal CostPrice,    // Đơn giá vốn kho
    decimal TotalValue,   // Giá trị tồn kho thực tế = QtyTotalOK * CostPrice
    StockExtendStatus Status,
    string StatusLabel,
    string BadgeClass,
    int ReplenishNeeded,  // Lượng đề xuất nhập thêm = Math.Max(0, MinStock - QtyStockExt)
    bool HasLot,          // Mặt hàng quản lý theo Lô
    bool HasSerial        // Mặt hàng quản lý theo Serial / IMEI
);

/// <summary>Báo cáo Tồn kho mở rộng & Dự phóng khả dụng tổng hợp (port từ Rpt_Inv_InventoryBalance_Extend Skycic).</summary>
public record StockExtendReport(
    int? WarehouseId,
    string WarehouseName,
    StockExtendStatus? StatusFilter,
    string? Keyword,
    int TotalItems,             // Tổng số mặt hàng
    int TotalQtyTotalOK,        // Tổng tồn vật lý
    int TotalQtyBlockOK,        // Tổng số lượng phong tỏa
    int TotalQtyAvailOK,        // Tổng tồn khả dụng
    int TotalQtyBackOrder,      // Tổng hàng sắp về
    int TotalQtyStockExt,       // Tổng tồn mở rộng
    decimal TotalInventoryValue,// Tổng giá trị tồn (VNĐ)
    int OutOfStockCount,        // Số mặt hàng hết hàng khả dụng
    int UnderMinCount,          // Số mặt hàng dưới định mức
    int OptimalCount,           // Số mặt hàng đạt chuẩn
    int OverMaxCount,           // Số mặt hàng vượt định mức
    int UrgentReplenishCount,   // Số mặt hàng cần nhập thêm gấp
    double AvgAvailRate,        // Tỷ lệ khả dụng trung bình %
    List<StockExtendRow> Rows
);

/// <summary>Phân loại nhóm giá trị ABC trong đánh giá giá trị tồn kho (port từ Rpt_Inv_InventoryBalance_ByValue Skycic).</summary>
public enum InventoryValuationAbcClass
{
    All = 0,
    ClassA = 1, // Nhóm A - Giá trị cao (chiếm ~70% giá trị tồn kho, cần quản lý kiểm soát nghiêm ngặt rủi ro vốn)
    ClassB = 2, // Nhóm B - Giá trị trung bình (chiếm ~20% giá trị tồn kho)
    ClassC = 3  // Nhóm C - Giá trị thấp (chiếm ~10% giá trị tồn kho)
}

/// <summary>Dòng báo cáo Đánh giá giá trị tồn kho & Cơ cấu tài sản kho (port từ Rpt_Inv_InventoryBalance_ByValue & Rpt_Inv_InventoryBalance Skycic).</summary>
public record InventoryValuationRow(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    int WarehouseId,
    string WarehouseName,
    int QtyTotalOK,          // Số lượng tồn vật lý (Physical On-hand)
    int QtyBlockOK,          // Số lượng tạm khóa / phong tỏa (Blocked / Reserved)
    int QtyAvailOK,          // Số lượng khả dụng sẵn sàng xuất bán (Available to Promise = QtyTotalOK - QtyBlockOK)
    double AvailRate,        // Tỷ lệ khả dụng % = QtyAvailOK / QtyTotalOK * 100
    decimal CostPrice,       // Đơn giá vốn kho / định giá (ValMixBase / UPInv)
    decimal TotalValMixBase, // Tổng giá trị tồn vật lý = QtyTotalOK * CostPrice (TotalValMixBase Skycic)
    decimal TotalValAvail,   // Tổng giá trị hàng khả dụng = QtyAvailOK * CostPrice
    decimal TotalValBlock,   // Tổng giá trị hàng bị tạm khóa / chôn vốn = QtyBlockOK * CostPrice
    double SharePercent,     // Tỷ trọng % giá trị so với tổng tài sản kho (InvPercent Skycic)
    InventoryValuationAbcClass AbcClass, // Phân hạng ABC
    string AbcClassLabel,    // Hạng A / Hạng B / Hạng C
    string AbcBadgeClass,    // badge color
    string CapitalRiskStatus,// Trạng thái rủi ro vốn (Bình thường / Chôn vốn tạm khóa / Giá trị cao cần giải phóng)
    string RiskBadgeClass,   // badge color
    bool HasLot,             // Quản lý lô
    bool HasSerial           // Quản lý Serial
);

/// <summary>Báo cáo Đánh giá giá trị tồn kho & Cơ cấu tài sản kho tổng hợp (port từ Rpt_Inv_InventoryBalance_ByValue Skycic).</summary>
public record InventoryValuationReport(
    int? WarehouseId,
    string WarehouseName,
    DateTime AsOfDate,
    InventoryValuationAbcClass? AbcFilter,
    bool OnlyHasStock,
    string? Keyword,
    int TotalItems,              // Tổng số mặt hàng
    int TotalPhysicalQty,        // Tổng số lượng tồn vật lý
    int TotalBlockedQty,         // Tổng số lượng tạm khóa
    int TotalAvailableQty,       // Tổng số lượng khả dụng
    decimal GrandTotalValMixBase,// Tổng giá trị tồn kho thực tế (VNĐ)
    decimal GrandTotalValAvail,  // Tổng giá trị tồn kho khả dụng (VNĐ)
    decimal GrandTotalValBlock,  // Tổng giá trị hàng tạm khóa (VNĐ)
    double AvailValueRatio,      // Tỷ lệ giá trị khả dụng % = GrandTotalValAvail / GrandTotalValMixBase * 100
    int ClassACount,             // Số mặt hàng nhóm A
    decimal ClassAValue,         // Giá trị tồn nhóm A
    int ClassBCount,             // Số mặt hàng nhóm B
    decimal ClassBValue,         // Giá trị tồn nhóm B
    int ClassCCount,             // Số mặt hàng nhóm C
    decimal ClassCValue,         // Giá trị tồn nhóm C
    List<InventoryValuationRow> Rows
);

/// <summary>Chi tiết hồ sơ khách hàng & Lịch sử giao dịch kho (port từ Mst_Customer Skycic).</summary>
public record CustomerDetailDto(
    Customer Customer,
    List<StockDoc> OutDocs,
    List<InventoryOutFG> OutFGDocs,
    List<CustomerReturn> Returns,
    int TotalOutQty,
    int TotalReturnQty
);

/// <summary>Danh mục Loại mặt hàng / Phân loại hàng hóa kho (port từ Mst_PartType Skycic: PartType, PartTypeName, FlagActive, Remark).</summary>
public class PartType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // Mã loại mặt hàng (PartType, vd: TP, BTP, NVL, PTLK, BBDG, CCDC, HHTM)
    public string Name { get; set; } = "";          // Tên loại mặt hàng (PartTypeName)
    public bool IsActive { get; set; } = true;      // Trạng thái áp dụng (FlagActive: 1 - Áp dụng, 0 - Ngừng áp dụng)
    public string? Remark { get; set; }             // Ghi chú / Mô tả đặc tính hàng hóa (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị loại mặt hàng kèm số lượng sản phẩm liên kết và tổng tồn.</summary>
public record PartTypeRow(
    int Id,
    string Code,
    string Name,
    string? Remark,
    bool IsActive,
    DateTime CreatedAt,
    int ProductCount,
    int TotalStockQty
);

/// <summary>Báo cáo / Danh sách loại mặt hàng tổng hợp kèm 4 thẻ KPI.</summary>
public record PartTypeReport(
    string? Keyword,
    bool? ActiveFilter,
    int TotalTypes,
    int ActiveCount,
    int InactiveCount,
    int TotalProductsMapped,
    List<PartTypeRow> Rows
);

/// <summary>Chi tiết Loại mặt hàng kèm danh sách sản phẩm thuộc loại.</summary>
public record PartTypeDetailDto(
    PartType Item,
    List<Product> Products,
    int TotalProducts,
    int TotalStockQty
);

/// <summary>Danh mục Thương hiệu / Nhãn hiệu hàng hóa kho (port từ Mst_Brand Skycic: BrandCode, BrandName, FlagActive, Remark).</summary>
public class Brand : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // Mã thương hiệu / nhãn hiệu (BrandCode, vd: MAY10, VIETTIEN, ANPHUOC, LEVI, CANIFA, NEM...)
    public string Name { get; set; } = "";          // Tên thương hiệu / nhãn hiệu (BrandName)
    public string? Origin { get; set; }             // Xuất xứ / Quốc gia (vd: Việt Nam, Mỹ, Pháp, Ý, Nhật Bản...)
    public bool IsActive { get; set; } = true;      // Trạng thái áp dụng (FlagActive: 1 - Áp dụng, 0 - Ngừng áp dụng)
    public string? Remark { get; set; }             // Ghi chú / Mô tả đặc trưng phân khúc thương hiệu (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị thương hiệu kèm số lượng sản phẩm liên kết và tổng tồn.</summary>
public record BrandRow(
    int Id,
    string Code,
    string Name,
    string? Origin,
    string? Remark,
    bool IsActive,
    DateTime CreatedAt,
    int ProductCount,
    int TotalStockQty
);

/// <summary>Báo cáo / Danh sách thương hiệu tổng hợp kèm 4 thẻ KPI.</summary>
public record BrandReport(
    string? Keyword,
    bool? ActiveFilter,
    int TotalBrands,
    int ActiveCount,
    int InactiveCount,
    int TotalProductsMapped,
    List<BrandRow> Rows
);

/// <summary>Chi tiết Thương hiệu kèm danh sách sản phẩm mang thương hiệu.</summary>
public record BrandDetailDto(
    Brand Item,
    List<Product> Products,
    int TotalProducts,
    int TotalStockQty
);

/// <summary>Danh mục Màu sắc hàng hóa kho (Part Color - port từ Mst_PartColor Skycic: PartColorCode, PartColorName, PartColorNameVN, FlagActive).</summary>
public class PartColor : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // Mã màu (PartColorCode, vd: DEN, TRANG, XANH-NAVY, DO-DAM)
    public string Name { get; set; } = "";          // Tên màu (PartColorName, vd: Black, White, Navy Blue)
    public string? NameVN { get; set; }             // Tên màu tiếng Việt (PartColorNameVN, vd: Đen, Trắng, Xanh navy)
    public bool IsActive { get; set; } = true;      // Trạng thái áp dụng (FlagActive: 1 - Áp dụng, 0 - Ngừng áp dụng)
    public string? Remark { get; set; }             // Ghi chú / Mô tả sắc độ màu (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Gán màu sắc cho mặt hàng kho (Map Part Color - port từ Mst_MapPartColor Skycic: PartCode, PartColorCode, FlagDefault, FlagActive).</summary>
public class PartColorMap : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int ProductId { get; set; }              // Mặt hàng được gán màu (PartCode)
    public string PartColorCode { get; set; } = ""; // Mã màu gán cho mặt hàng (PartColorCode)
    public bool IsDefault { get; set; } = false;    // Màu mặc định của mặt hàng (FlagDefault: 1 - Mặc định, 0 - Phụ)
    public bool IsActive { get; set; } = true;      // Trạng thái áp dụng (FlagActive)
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Product Product { get; set; } = null!;
}

/// <summary>Dòng thông tin hiển thị màu sắc kèm số lượng mặt hàng gán màu và tổng tồn.</summary>
public record PartColorRow(
    int Id,
    string Code,
    string Name,
    string? NameVN,
    string? Remark,
    bool IsActive,
    DateTime CreatedAt,
    int ProductCount,
    int TotalStockQty
);

/// <summary>Báo cáo / Danh sách màu sắc hàng hóa tổng hợp kèm 4 thẻ KPI.</summary>
public record PartColorReport(
    string? Keyword,
    bool? ActiveFilter,
    int TotalColors,
    int ActiveCount,
    int InactiveCount,
    int TotalProductsMapped,
    List<PartColorRow> Rows
);

/// <summary>Dòng chi tiết mặt hàng gán một màu sắc (kèm cờ mặc định).</summary>
public record PartColorProductRow(
    int MapId,
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    bool IsDefault,
    bool IsActive,
    int StockQty
);

/// <summary>Chi tiết Màu sắc kèm danh sách mặt hàng được gán màu.</summary>
public record PartColorDetailDto(
    PartColor Item,
    List<PartColorProductRow> Products,
    int TotalProducts,
    int TotalStockQty
);

/// <summary>Danh mục Đơn vị tính hàng hóa / vật tư kho (Unit of Measure - UOM - port từ Mst_PartUnit Skycic: PartUnitCode, PartUnitName, FlagUnitStd, FlagActive, Remark).</summary>
public class PartUnit : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // Mã đơn vị tính (PartUnitCode, vd: CAI, HOP, THUNG, KG, MET, CUON, BO, CHIEC, VI, LIT, M2)
    public string Name { get; set; } = "";          // Tên đơn vị tính (PartUnitName, vd: Cái, Hộp, Thùng, Kilogram, Mét, Cuộn, Bộ, Chiếc, Vỉ, Lít, Mét vuông)
    public bool IsStandard { get; set; } = true;    // Đơn vị chuẩn / cơ bản (FlagUnitStd: 1 - Đơn vị cơ bản, 0 - Đơn vị quy đổi/thứ cấp)
    public bool IsActive { get; set; } = true;      // Trạng thái áp dụng (FlagActive: 1 - Áp dụng, 0 - Ngừng áp dụng)
    public string? Remark { get; set; }             // Ghi chú / Quy cách quy đổi hoặc định mức bao bì (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị đơn vị tính kèm số lượng sản phẩm liên kết và tổng tồn.</summary>
public record PartUnitRow(
    int Id,
    string Code,
    string Name,
    bool IsStandard,
    bool IsActive,
    string? Remark,
    DateTime CreatedAt,
    int ProductCount,
    int TotalStockQty
);

/// <summary>Báo cáo / Danh sách đơn vị tính tổng hợp kèm 4 thẻ KPI.</summary>
public record PartUnitReport(
    string? Keyword,
    bool? ActiveFilter,
    bool? StandardOnly,
    int TotalUnits,
    int StandardUnitsCount,
    int ActiveCount,
    int InactiveCount,
    int TotalProductsMapped,
    List<PartUnitRow> Rows
);

/// <summary>Chi tiết Đơn vị tính kèm danh sách sản phẩm sử dụng đơn vị.</summary>
public record PartUnitDetailDto(
    PartUnit Item,
    List<Product> Products,
    int TotalProducts,
    int TotalStockQty
);

/// <summary>Danh mục Nhóm chất liệu / Loại vật liệu hàng hóa kho (Material Type - port từ Mst_PartMaterialType Skycic: PMType, PMTypeName, FlagActive, Remark).</summary>
public class PartMaterialType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // Mã nhóm chất liệu / vật liệu (PMType, vd: COTTON, KAKI, LEATHER, SILK, DENIM, INOX, PLASTIC, STEEL, WOOD...)
    public string Name { get; set; } = "";          // Tên nhóm chất liệu / vật liệu (PMTypeName, vd: Vải sợi Cotton 100%, Vải Kaki dệt thoi, Da bò thuộc tự nhiên...)
    public bool IsActive { get; set; } = true;      // Trạng thái áp dụng (FlagActive: 1 - Áp dụng, 0 - Ngừng áp dụng)
    public string? Remark { get; set; }             // Ghi chú / Tiêu chuẩn kỹ thuật & đặc tính bảo quản chất liệu (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị nhóm chất liệu kèm số lượng sản phẩm liên kết và tổng tồn.</summary>
public record PartMaterialTypeRow(
    int Id,
    string Code,
    string Name,
    string? Remark,
    bool IsActive,
    DateTime CreatedAt,
    int ProductCount,
    int TotalStockQty
);

/// <summary>Báo cáo / Danh sách nhóm chất liệu tổng hợp kèm 4 thẻ KPI.</summary>
public record PartMaterialTypeReport(
    string? Keyword,
    bool? ActiveFilter,
    int TotalMaterialTypes,
    int ActiveCount,
    int InactiveCount,
    int TotalProductsMapped,
    List<PartMaterialTypeRow> Rows
);

/// <summary>Chi tiết Nhóm chất liệu kèm danh sách sản phẩm thuộc nhóm chất liệu này.</summary>
public record PartMaterialTypeDetailDto(
    PartMaterialType Item,
    List<Product> Products,
    int TotalProducts,
    int TotalStockQty
);

/// <summary>Danh mục Dòng sản phẩm / Model hàng hóa kho (port từ Mst_Model / OS_PrdCenter_Mst_Model Skycic: ModelCode, ModelName, BrandCode, OrgModelCode, FlagActive, Remark).</summary>
public class ProductModel : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // Mã model / dòng sản phẩm (ModelCode, vd: MD-M10-SLIM, MD-LV-501, MD-AP-LEATHER...)
    public string Name { get; set; } = "";          // Tên model / dòng sản phẩm (ModelName, vd: Sơ mi Slimfit Oxford, Quần Jeans 501 Iconic...)
    public string? BrandCode { get; set; }          // Mã thương hiệu sở hữu dòng sản phẩm (BrandCode: MAY10, VIETTIEN, ANPHUOC, LEVI, NEM...)
    public string? OrgModelCode { get; set; }       // Mã model gốc của nhà sản xuất / OEM (OrgModelCode)
    public bool IsActive { get; set; } = true;      // Trạng thái áp dụng (FlagActive: 1 - Áp dụng, 0 - Ngừng áp dụng)
    public string? Remark { get; set; }             // Ghi chú / Mô tả đặc trưng dòng sản phẩm (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị dòng sản phẩm kèm thương hiệu, số lượng sản phẩm liên kết và tổng tồn.</summary>
public record ProductModelRow(
    int Id,
    string Code,
    string Name,
    string? BrandCode,
    string? BrandName,
    string? OrgModelCode,
    string? Remark,
    bool IsActive,
    DateTime CreatedAt,
    int ProductCount,
    int TotalStockQty
);

/// <summary>Báo cáo / Danh sách dòng sản phẩm tổng hợp kèm 4 thẻ KPI.</summary>
public record ProductModelReport(
    string? Keyword,
    string? BrandCodeFilter,
    bool? ActiveFilter,
    int TotalModels,
    int ActiveCount,
    int InactiveCount,
    int TotalProductsMapped,
    List<ProductModelRow> Rows
);

/// <summary>Chi tiết Dòng sản phẩm kèm thương hiệu và danh sách sản phẩm thuộc dòng này.</summary>
public record ProductModelDetailDto(
    ProductModel Item,
    Brand? Brand,
    List<Product> Products,
    int TotalProducts,
    int TotalStockQty
);

/// <summary>Danh mục Loại kho / Phân loại kho hàng (port từ Mst_InventoryType Skycic: InvType, InvTypeName, FlagActive, Remark).</summary>
public class InventoryType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // Mã loại kho (InvType, vd: KHO_TONG, KHO_NVL, KHO_TP, KHO_TC, KHO_BH, KHO_DL)
    public string Name { get; set; } = "";          // Tên loại kho (InvTypeName, vd: Kho tổng phân phối, Kho nguyên vật liệu, Kho thành phẩm...)
    public bool IsActive { get; set; } = true;      // Trạng thái áp dụng (FlagActive: 1 - Áp dụng, 0 - Ngừng áp dụng)
    public string? Remark { get; set; }             // Ghi chú / Mục đích sử dụng & đặc tính lưu trữ (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị loại kho kèm số lượng kho trực thuộc và tổng tồn kho thực tế.</summary>
public record InventoryTypeRow(
    int Id,
    string Code,
    string Name,
    string? Remark,
    bool IsActive,
    DateTime CreatedAt,
    int WarehouseCount,
    int TotalStockQty
);

/// <summary>Báo cáo / Danh sách loại kho tổng hợp kèm 4 thẻ KPI.</summary>
public record InventoryTypeReport(
    string? Keyword,
    bool? ActiveFilter,
    int TotalTypes,
    int ActiveCount,
    int InactiveCount,
    int TotalWarehousesMapped,
    List<InventoryTypeRow> Rows
);

/// <summary>Chi tiết Loại kho kèm danh sách các kho trực thuộc loại này.</summary>
public record InventoryTypeDetailDto(
    InventoryType Item,
    List<Warehouse> Warehouses,
    int TotalWarehouses,
    int TotalStockQty
);

/// <summary>Danh mục Loại hình / Lý do Nhập kho (Inbound Type - port từ Mst_InvInType Skycic: InvInType, InvInTypeName, FlagActive, FlagStatistic, Remark).</summary>
public class InventoryInType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // Mã loại nhập kho (InvInType, vd: IN_BUY, IN_PROD, IN_RETURN, IN_TRANSFER, IN_AUDIT, IN_SAMPLE, IN_OTHER)
    public string Name { get; set; } = "";          // Tên loại nhập kho (InvInTypeName, vd: Nhập mua NCC, Nhập thành phẩm SX...)
    public bool FlagStatistic { get; set; } = true; // Cờ tính vào thống kê phân tích mua hàng / sản lượng (FlagStatistic: 1 - Tính, 0 - Không tính)
    public bool IsActive { get; set; } = true;      // Trạng thái áp dụng (FlagActive: 1 - Áp dụng, 0 - Ngừng áp dụng)
    public string? Remark { get; set; }             // Ghi chú / Quy trình chứng từ nhập kho (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị loại nhập kho kèm số lượng chứng từ phát sinh và tổng số lượng nhập.</summary>
public record InventoryInTypeRow(
    int Id,
    string Code,
    string Name,
    bool FlagStatistic,
    bool IsActive,
    string? Remark,
    DateTime CreatedAt,
    int TotalDocsCount,
    int TotalQtyIn
);

/// <summary>Báo cáo / Danh sách loại nhập kho tổng hợp kèm 4 thẻ KPI.</summary>
public record InventoryInTypeReport(
    string? Keyword,
    bool? ActiveFilter,
    bool? StatisticFilter,
    int TotalTypes,
    int ActiveCount,
    int StatisticCount,
    int InactiveCount,
    int TotalInDocsCount,
    List<InventoryInTypeRow> Rows
);

/// <summary>Chi tiết Loại nhập kho kèm thông tin và chứng từ nhập kho liên quan.</summary>
public record InventoryInTypeDetailDto(
    InventoryInType Item,
    List<StockDoc> Docs,
    int TotalDocs,
    int TotalQtyIn
);

/// <summary>Danh mục Loại hình / Lý do Xuất kho (Outbound Type - port từ Mst_InvOutType Skycic: InvOutType, InvOutTypeName, FlagActive, FlagStatistic, Remark).</summary>
public class InventoryOutType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // Mã loại xuất kho (InvOutType, vd: OUT_SALE, OUT_PROD, OUT_TRANSFER, OUT_RETURN_SUP, OUT_AUDIT, OUT_DISPOSAL, OUT_SAMPLE, OUT_OTHER)
    public string Name { get; set; } = "";          // Tên loại xuất kho (InvOutTypeName, vd: Xuất bán hàng đại lý, Xuất NVL sản xuất...)
    public bool FlagStatistic { get; set; } = true; // Cờ tính vào thống kê sản lượng xuất / doanh thu (FlagStatistic: 1 - Tính, 0 - Không tính)
    public bool IsActive { get; set; } = true;      // Trạng thái áp dụng (FlagActive: 1 - Áp dụng, 0 - Ngừng áp dụng)
    public string? Remark { get; set; }             // Ghi chú / Quy trình chứng từ xuất kho (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị loại xuất kho kèm số lượng chứng từ phát sinh và tổng số lượng xuất.</summary>
public record InventoryOutTypeRow(
    int Id,
    string Code,
    string Name,
    bool FlagStatistic,
    bool IsActive,
    string? Remark,
    DateTime CreatedAt,
    int TotalDocsCount,
    int TotalQtyOut
);

/// <summary>Báo cáo / Danh sách loại xuất kho tổng hợp kèm 4 thẻ KPI.</summary>
public record InventoryOutTypeReport(
    string? Keyword,
    bool? ActiveFilter,
    bool? StatisticFilter,
    int TotalTypes,
    int ActiveCount,
    int StatisticCount,
    int InactiveCount,
    int TotalOutDocsCount,
    List<InventoryOutTypeRow> Rows
);

/// <summary>Chi tiết Loại xuất kho kèm thông tin và chứng từ xuất kho liên quan.</summary>
public record InventoryOutTypeDetailDto(
    InventoryOutType Item,
    List<StockDoc> Docs,
    int TotalDocs,
    int TotalQtyOut
);

/// <summary>Danh mục Cấp kho / Phân cấp quản lý kho hàng (port từ Mst_InventoryLevelType Skycic: InvLevelType, InvLevelTypeName, FlagActive, Remark).</summary>
public class InventoryLevelType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";          // Mã cấp kho (InvLevelType, vd: CAP_1, CAP_2, CAP_3, HUB, KHO_DAILY)
    public string Name { get; set; } = "";          // Tên cấp kho (InvLevelTypeName, vd: Kho Cấp 1 - Tổng kho trung ương, Kho Cấp 2...)
    public bool IsActive { get; set; } = true;      // Trạng thái áp dụng (FlagActive: 1 - Áp dụng, 0 - Ngừng áp dụng)
    public string? Remark { get; set; }             // Ghi chú / Phạm vi & thẩm quyền điều phối kho (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị cấp kho kèm số lượng kho trực thuộc và tổng tồn kho thực tế.</summary>
public record InventoryLevelTypeRow(
    int Id,
    string Code,
    string Name,
    string? Remark,
    bool IsActive,
    DateTime CreatedAt,
    int WarehouseCount,
    int TotalStockQty
);

/// <summary>Báo cáo / Danh sách cấp kho tổng hợp kèm 4 thẻ KPI.</summary>
public record InventoryLevelTypeReport(
    string? Keyword,
    bool? ActiveFilter,
    int TotalLevels,
    int ActiveCount,
    int InactiveCount,
    int TotalWarehousesMapped,
    List<InventoryLevelTypeRow> Rows
);

/// <summary>Chi tiết Cấp kho kèm danh sách các kho trực thuộc cấp này.</summary>
public record InventoryLevelTypeDetailDto(
    InventoryLevelType Item,
    List<Warehouse> Warehouses,
    int TotalWarehouses,
    int TotalStockQty
);

/// <summary>Phân quyền người dùng quản lý kho / Gán thủ kho phụ trách kho (port từ Mst_UserMapInventory Skycic: OrgID, UserCode, InvCode, Remark, LogLUBy, LogLUDTimeUTC).</summary>
public class UserMapInventory : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int WarehouseId { get; set; }                                  // Kho được phân quyền quản lý (InvCode)
    public string UserCode { get; set; } = "";                             // Mã người dùng / Mã nhân viên (UserCode, vd: admin, thukho_hn01, nv_xuatkho...)
    public string UserName { get; set; } = "";                             // Họ tên nhân viên phụ trách kho
    public string UserRole { get; set; } = "Thủ kho chính";               // Vai trò phụ trách: Trưởng kho, Thủ kho chính, Nhân viên xuất nhập, Kiểm kê viên, Giám sát an toàn
    public string? Email { get; set; }                                    // Email liên lạc
    public string? Phone { get; set; }                                    // Số điện thoại liên hệ
    public string? DepartmentCode { get; set; }                            // Mã bộ phận / phòng ban trực thuộc (port từ Sys_User.DepartmentCode & Mst_Department Skycic)
    public bool IsActive { get; set; } = true;                             // Trạng thái hiệu lực phân quyền (FlagActive: 1 - Hiệu lực, 0 - Tạm dừng)
    public string? Remark { get; set; }                                    // Ghi chú / Quyết định phân công nhiệm vụ
    public string AssignedBy { get; set; } = "admin";                     // Người phân công (LogLUBy)
    public DateTime AssignedAt { get; set; } = DateTime.Now;               // Thời điểm phân quyền (LogLUDTimeUTC)

    public Warehouse Warehouse { get; set; } = null!;
}

/// <summary>Dòng thông tin hiển thị phân quyền thủ kho kèm chi tiết kho và nhân viên.</summary>
public record UserMapInventoryRow(
    int Id,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    string? InvTypeCode,
    string? InvLevelTypeCode,
    string UserCode,
    string UserName,
    string UserRole,
    string? Email,
    string? Phone,
    bool IsActive,
    string? Remark,
    string AssignedBy,
    DateTime AssignedAt
);

/// <summary>Báo cáo / Danh sách phân quyền người dùng quản lý kho tổng hợp kèm 4 thẻ KPI.</summary>
public record UserMapInventoryReport(
    int? WarehouseId,
    string? UserRole,
    bool? ActiveFilter,
    string? Keyword,
    int TotalAssignments,
    int ActiveAssignments,
    int TotalUsersAssigned,
    int UnassignedWarehousesCount,
    List<UserMapInventoryRow> Rows
);

/// <summary>Tổng hợp thông tin phân công nhân sự theo từng kho.</summary>
public record WarehouseAssignmentSummary(
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int AssignedUsersCount,
    List<string> AssignedUserNames
);

/// <summary>Dữ liệu gán hàng loạt nhân viên vào kho.</summary>
public record BatchMapUserItemDto(
    string UserCode,
    string UserName,
    string UserRole,
    string? Email,
    string? Phone,
    string? Remark
);

/// <summary>Danh mục Nhóm hàng hóa / Phân nhóm sản phẩm kho (port từ Mst_ProductGroup & Mst_ProductGroupSub Skycic: ProductGrpCode, ProductGrpName, ProductGrpDesc, ProductGrpCodeParent, BrandCode, FlagActive).</summary>
public class ProductGroup : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // Mã nhóm hàng (ProductGrpCode, vd: GRP_THOI_TRANG, GRP_AO_SM, GRP_QUAN_JEAN...)
    public string Name { get; set; } = "";             // Tên nhóm hàng (ProductGrpName, vd: Áo sơ mi & Polo, Quần Jeans & Kaki...)
    public string? Description { get; set; }          // Mô tả đặc tính nhóm hàng (ProductGrpDesc)
    public string? ParentCode { get; set; }           // Mã nhóm cha (ProductGrpCodeParent) - phân cấp cây danh mục Cấp 1 / Cấp 2
    public string? BrandCode { get; set; }            // Thương hiệu liên kết (BrandCode, vd: MAY10, LEVI, NEM...)
    public bool IsActive { get; set; } = true;         // Trạng thái hoạt động (FlagActive: 1 - Đang áp dụng, 0 - Tạm dừng)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị nhóm hàng hóa kèm thông tin nhóm cha, thương hiệu, số lượng mặt hàng và tổng tồn kho thực tế.</summary>
public record ProductGroupRow(
    int Id,
    string Code,
    string Name,
    string? Description,
    string? ParentCode,
    string? ParentName,
    string? BrandCode,
    string? BrandName,
    bool IsActive,
    DateTime CreatedAt,
    int Level, // 1 = Nhóm gốc (Root), 2 = Phân nhóm con (Sub-group)
    int ProductCount,
    int TotalStockQty
);

/// <summary>Báo cáo / Danh sách nhóm hàng hóa tổng hợp kèm 4 thẻ KPI.</summary>
public record ProductGroupReport(
    string? Keyword,
    string? ParentFilter,
    string? BrandFilter,
    bool? ActiveFilter,
    int TotalGroups,
    int RootGroupsCount,
    int SubGroupsCount,
    int TotalProductsAssigned,
    int TotalStockQty,
    List<ProductGroupRow> Rows
);

/// <summary>Chi tiết Nhóm hàng kèm danh sách các phân nhóm con và danh sách mặt hàng trực thuộc.</summary>
public record ProductGroupDetailDto(
    ProductGroup Group,
    ProductGroup? ParentGroup,
    List<ProductGroup> SubGroups,
    List<Product> Products,
    int TotalProducts,
    int TotalStockQty
);

/// <summary>Danh mục Vùng thị trường & Khu vực địa bàn kho (port từ Mst_Area Skycic: AreaCode, AreaName, AreaDesc, AreaCodeParent, FlagActive).</summary>
public class Area : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // Mã khu vực (AreaCode, vd: AREA_MB, AREA_MT, AREA_MN, AREA_HN, AREA_HCM...)
    public string Name { get; set; } = "";             // Tên khu vực (AreaName, vd: Vùng Miền Bắc, Khu vực Hà Nội & Vùng phụ cận...)
    public string? Description { get; set; }          // Mô tả địa bàn / phạm vi logistics (AreaDesc)
    public string? ParentCode { get; set; }           // Mã khu vực cha (AreaCodeParent) - phân cấp cây vùng Cấp 1 / Cấp 2
    public bool IsActive { get; set; } = true;         // Trạng thái hoạt động (FlagActive: 1 - Đang áp dụng, 0 - Tạm dừng)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị khu vực kèm thông tin khu vực cha, số lượng kho, số khách hàng/đại lý và tổng tồn kho thực tế.</summary>
public record AreaRow(
    int Id,
    string Code,
    string Name,
    string? Description,
    string? ParentCode,
    string? ParentName,
    bool IsActive,
    DateTime CreatedAt,
    int Level, // 1 = Vùng gốc (Root), 2 = Khu vực con / Chi nhánh địa bàn (Sub-area)
    int WarehouseCount,
    int CustomerCount,
    int TotalStockQty
);

/// <summary>Báo cáo / Danh sách vùng & khu vực thị trường tổng hợp kèm 4 thẻ KPI.</summary>
public record AreaReport(
    string? Keyword,
    string? ParentFilter,
    bool? ActiveFilter,
    int TotalAreas,
    int RootAreasCount,
    int SubAreasCount,
    int TotalWarehousesAssigned,
    int TotalCustomersAssigned,
    int TotalStockQty,
    List<AreaRow> Rows
);

/// <summary>Chi tiết Vùng / Khu vực kèm danh sách các khu vực con, kho hàng và khách hàng trực thuộc.</summary>
public record AreaDetailDto(
    Area Area,
    Area? ParentArea,
    List<Area> SubAreas,
    List<Warehouse> Warehouses,
    List<Customer> Customers,
    int TotalWarehouses,
    int TotalCustomers,
    int TotalStockQty
);

/// <summary>Danh mục Nhóm khách hàng & Đại lý phân phối kho (port từ Mst_CustomerGroup Skycic: CustomerGrpCode, CustomerGrpName, CustomerGrpDesc, CustomerGrpCodeParent, FlagActive).</summary>
public class CustomerGroup : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // Mã nhóm khách hàng (CustomerGrpCode, vd: GRP_DAILY, DAILY_CAP1, DAILY_CAP2, GRP_B2B, B2B_DUAN, GRP_RETAIL...)
    public string Name { get; set; } = "";             // Tên nhóm khách hàng (CustomerGrpName, vd: Hệ thống Đại lý phân phối, Đại lý Cấp 1...)
    public string? Description { get; set; }          // Mô tả chính sách chiết khấu, hạn mức nợ & giao hàng (CustomerGrpDesc)
    public string? ParentCode { get; set; }           // Mã nhóm cha (CustomerGrpCodeParent) - phân cấp cây nhóm Cấp 1 / Cấp 2
    public bool IsActive { get; set; } = true;         // Trạng thái hoạt động (FlagActive: 1 - Đang áp dụng, 0 - Tạm dừng)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị nhóm khách hàng kèm thông tin nhóm cha, số khách hàng/đại lý trực thuộc và tổng lượng hàng đã xuất kho phân phối.</summary>
public record CustomerGroupRow(
    int Id,
    string Code,
    string Name,
    string? Description,
    string? ParentCode,
    string? ParentName,
    bool IsActive,
    DateTime CreatedAt,
    int Level, // 1 = Nhóm gốc (Root / Channel), 2 = Phân nhóm con (Sub-group)
    int CustomerCount,
    int TotalDispatchedQty
);

/// <summary>Báo cáo / Danh sách nhóm khách hàng tổng hợp kèm 4 thẻ KPI.</summary>
public record CustomerGroupReport(
    string? Keyword,
    string? ParentFilter,
    bool? ActiveFilter,
    int TotalGroups,
    int RootGroupsCount,
    int SubGroupsCount,
    int TotalCustomersAssigned,
    int TotalDispatchedQty,
    List<CustomerGroupRow> Rows
);

/// <summary>Chi tiết Nhóm khách hàng kèm danh sách các phân nhóm con, danh sách khách hàng trực thuộc và các giao dịch xuất kho gần nhất.</summary>
public record     CustomerGroupDetailDto(
    CustomerGroup Group,
    CustomerGroup? ParentGroup,
    List<CustomerGroup> SubGroups,
    List<Customer> Customers,
    int TotalCustomers,
    int TotalDispatchedQty,
    List<StockDoc> RecentDispatches
);

/// <summary>Danh mục Bộ phận & Phòng ban quản lý kho, nhận hàng & cấp phát vật tư (port từ Mst_Department & Mst_DepartmentExt Skycic: DepartmentCode, DepartmentName, DepartmentCodeParent, DepartmentBUCode, DepartmentLevel, MST, FlagActive, Remark).</summary>
public class Department : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // Mã bộ phận/phòng ban (DepartmentCode, vd: KHOI_SX, KHOI_LOG, PB_QLKHO, PX_LAPRAP...)
    public string Name { get; set; } = "";             // Tên bộ phận/phòng ban (DepartmentName, vd: Phòng Quản lý Kho vận, Phân xưởng Lắp ráp...)
    public string? ParentCode { get; set; }           // Mã bộ phận cấp trên (DepartmentCodeParent) - phân cấp cây Khối/Ban -> Phòng ban -> Phân xưởng/Tổ
    public string? BUCode { get; set; }               // Đơn vị kinh doanh / Chi nhánh trực thuộc (DepartmentBUCode)
    public int Level { get; set; } = 1;               // Cấp bậc phòng ban (DepartmentLevel: 1 = Khối/Ban, 2 = Phòng ban, 3 = Phân xưởng/Tổ/Đội)
    public string? MST { get; set; }                  // Mã số thuế / Mã chi nhánh hạch toán (MST)
    public string? Description { get; set; }          // Ghi chú chức năng nhiệm vụ, địa điểm làm việc (Remark)
    public bool IsActive { get; set; } = true;         // Trạng thái hoạt động (FlagActive: 1 - Đang áp dụng, 0 - Tạm dừng)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị bộ phận/phòng ban kèm thông tin cấp bậc, bộ phận cấp trên, số nhân sự kho và số lượng phiếu/hàng cấp phát.</summary>
public record DepartmentRow(
    int Id,
    string Code,
    string Name,
    string? ParentCode,
    string? ParentName,
    string? BUCode,
    int Level,
    string LevelName,
    string? MST,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    int SubDepartmentCount,
    int AssignedUserCount,
    int DispatchedDocCount,
    int TotalDispatchedQty
);

/// <summary>Báo cáo / Danh sách bộ phận phòng ban tổng hợp kèm 4 thẻ KPI.</summary>
public record DepartmentReport(
    string? Keyword,
    string? ParentFilter,
    int? LevelFilter,
    bool? ActiveFilter,
    int TotalDepartments,
    int RootBlocksCount,       // Số Khối/Ban cấp 1
    int SubDepartmentsCount,   // Số Phòng ban & Phân xưởng (Cấp 2 & 3)
    int TotalAssignedUsers,    // Tổng nhân sự kho được phân công
    int TotalDispatchedQty,    // Tổng lượng vật tư/hàng hóa xuất dùng nội bộ / cấp phát cho các bộ phận
    List<DepartmentRow> Rows
);

/// <summary>Chi tiết Bộ phận/Phòng ban kèm danh sách bộ phận trực thuộc, danh sách nhân sự phụ trách kho và các phiếu xuất cấp phát gần nhất.</summary>
public record DepartmentDetailDto(
    Department Department,
    Department? ParentDepartment,
    List<Department> SubDepartments,
    List<UserMapInventory> AssignedUsers,
    int TotalSubDepartments,
    int TotalAssignedUsers,
    int TotalDispatchedQty,
    List<StockDoc> RecentDispatches
);

/// <summary>Danh mục Nguồn khách hàng & Kênh tiếp nhận đối tác kho (port từ Mst_CustomerSource Skycic: CustomerSourceCode, CustomerSourceName, CustomerSourceDesc, CustomerSourceCodeParent, CustomerSourceBUCode, FlagActive).</summary>
public class CustomerSource : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // Mã nguồn / Kênh khách hàng (CustomerSourceCode, vd: SRC_DIRECT, SRC_DEALER, SRC_ECOMMERCE, SRC_PROJECT...)
    public string Name { get; set; } = "";             // Tên nguồn / Kênh khách hàng (CustomerSourceName, vd: Kênh Đại lý & NPP, Kênh Sàn TMĐT...)
    public string? ParentCode { get; set; }           // Mã nguồn kênh cha (CustomerSourceCodeParent) - phân cấp kênh gốc / kênh nhánh
    public string? BUCode { get; set; }               // Khối kinh doanh / Đơn vị phụ trách kênh (CustomerSourceBUCode)
    public string? Description { get; set; }          // Mô tả đặc điểm kênh, chính sách phân phối, giao nhận (CustomerSourceDesc)
    public bool IsActive { get; set; } = true;         // Trạng thái áp dụng (FlagActive: 1 - Đang áp dụng, 0 - Tạm dừng)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị nguồn khách hàng kèm thông tin nguồn cha, số khách hàng trực thuộc và sản lượng/doanh số xuất kho theo kênh.</summary>
public record CustomerSourceRow(
    int Id,
    string Code,
    string Name,
    string? Description,
    string? ParentCode,
    string? ParentName,
    string? BUCode,
    bool IsActive,
    DateTime CreatedAt,
    int Level, // 1 = Kênh gốc (Root Channel), 2 = Kênh nhánh (Sub-channel)
    int CustomerCount,
    int TotalShippedDocsCount,
    int TotalShippedQty,
    decimal TotalShippedAmount
);

/// <summary>Báo cáo / Danh sách nguồn khách hàng tổng hợp kèm 4 thẻ KPI.</summary>
public record CustomerSourceReport(
    string? Keyword,
    string? ParentFilter,
    bool? ActiveFilter,
    int TotalSources,
    int RootSourcesCount,
    int SubSourcesCount,
    int TotalCustomersAssigned,
    string TopSourceByVolume,
    int TopVolumeQty,
    decimal TotalAllShippedAmount,
    List<CustomerSourceRow> Rows
);

/// <summary>Chi tiết Nguồn khách hàng kèm danh sách các kênh nhánh, danh sách khách hàng trực thuộc và các phiếu xuất kho giao dịch gần nhất.</summary>
public record CustomerSourceDetailDto(
    CustomerSource Source,
    CustomerSource? ParentSource,
    List<CustomerSource> SubSources,
    List<Customer> Customers,
    int TotalCustomers,
    int TotalShippedDocsCount,
    int TotalShippedQty,
    decimal TotalShippedAmount,
    List<StockDoc> RecentDispatches
);

/// <summary>Danh mục Loại hình & Mục đích Điều chuyển kho (Move Order Type - port từ Mst_MoveOrdType Skycic: MoveOrdType, MoveOrdTypeName, FlagActive, LogLUDTimeUTC, LogLUBy).</summary>
public class MoveOrdType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // Mã loại điều chuyển (MoveOrdType, vd: MOVE_BRANCH, MOVE_REPLENISH, MOVE_TRANSIT, MOVE_WARRANTY, MOVE_REORG, MOVE_DISPOSAL)
    public string Name { get; set; } = "";             // Tên loại điều chuyển (MoveOrdTypeName, vd: Điều chuyển phân phối chi nhánh, Điều chuyển bổ sung dự phòng an toàn...)
    public string? Description { get; set; }          // Mô tả quy trình & mục đích điều chuyển
    public bool IsUrgent { get; set; } = false;        // Cờ điều chuyển khẩn cấp / Mức độ ưu tiên cao
    public bool IsActive { get; set; } = true;         // Trạng thái hoạt động (FlagActive: 1 - Đang áp dụng, 0 - Tạm dừng)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng thông tin hiển thị loại điều chuyển kho kèm số lượng lệnh phát sinh và tổng sản lượng chuyển.</summary>
public record MoveOrdTypeRow(
    int Id,
    string Code,
    string Name,
    string? Description,
    bool IsUrgent,
    bool IsActive,
    DateTime CreatedAt,
    int TotalOrdersCount,
    int TotalQtyMoved
);

/// <summary>Báo cáo / Danh sách loại hình điều chuyển kho tổng hợp kèm 4 thẻ KPI.</summary>
public record MoveOrdTypeReport(
    string? Keyword,
    bool? ActiveFilter,
    bool? UrgentFilter,
    int TotalTypes,
    int ActiveCount,
    int UrgentCount,
    int InactiveCount,
    int TotalMoveOrdersCount,
    int TotalQtyMoved,
    List<MoveOrdTypeRow> Rows
);

/// <summary>Chi tiết Loại điều chuyển kèm danh sách các lệnh điều chuyển phát sinh liên quan.</summary>
public record MoveOrdTypeDetailDto(
    MoveOrdType Item,
    List<MoveOrder> Orders,
    int TotalOrders,
    int TotalQtyMoved
);

/// <summary>Danh mục Đại lý phân phối, Showroom & Cửa hàng uỷ quyền thuộc mạng lưới phân phối kho (port từ Mst_Dealer Skycic: DLCode, DLName, DLCodeParent, DLBUCode, DLLevel, ProvinceCode, DLAddress, DLPresentBy, DLGovIDNumber, DLEmail, DLPhoneNo, FlagActive, Remark).</summary>
public class Dealer : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // Mã đơn vị / đại lý (DLCode, vd: DL_MB01, DL_MN01, DL_HP01...)
    public string Name { get; set; } = "";             // Tên đơn vị / đại lý phân phối (DLName)
    public string? ParentCode { get; set; }           // Mã đại lý cấp cha (DLCodeParent) - phân cấp cây đại lý
    public int Level { get; set; } = 1;                // Cấp bậc đại lý (DLLevel: 1 = Cấp 1 / Tổng đại lý, 2 = Cấp 2 / Đại lý khu vực, 3 = Điểm bán / Showroom ủy quyền)
    public string DealerType { get; set; } = "Đại lý phân phối"; // Loại hình đơn vị (DLType: Đại lý độc quyền, Đại lý phổ thông, Cửa hàng uỷ quyền, Showroom bán lẻ)
    public string? BUCode { get; set; }               // Khối kinh doanh quản lý (DLBUCode, vd: BU_NORTH, BU_CENTRAL, BU_SOUTH)
    public string? ProvinceCode { get; set; }         // Tỉnh / Thành phố đại lý đặt trụ sở (ProvinceCode)
    public string? Address { get; set; }              // Địa chỉ chi tiết (DLAddress)
    public string? PresentBy { get; set; }            // Người đại diện pháp luật / Giám đốc đại lý (DLPresentBy)
    public string? GovIdNumber { get; set; }          // Số CCCD / Mã số thuế đại lý (DLGovIDNumber)
    public string? Email { get; set; }                // Email liên hệ (DLEmail)
    public string? Phone { get; set; }                // Điện thoại liên hệ (DLPhoneNo)
    public int? WarehouseId { get; set; }             // Kho phụ trách cung ứng / phục vụ xuất kho cho đại lý
    public bool IsActive { get; set; } = true;         // Trạng thái hoạt động (FlagActive: 1 - Đang hoạt động, 0 - Tạm dừng)
    public string? Remark { get; set; }               // Ghi chú chính sách chiết khấu, hạn mức công nợ, quy định giao nhận
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Warehouse? Warehouse { get; set; }
}

/// <summary>Dòng thông tin hiển thị đại lý kèm thông tin cấp bậc, đại lý cha, kho phụ trách và thống kê sản lượng xuất hàng.</summary>
public record DealerRow(
    int Id,
    string Code,
    string Name,
    string? ParentCode,
    string? ParentName,
    int Level,
    string LevelLabel,
    string LevelBadgeClass,
    string DealerType,
    string? BUCode,
    string? ProvinceCode,
    string? Address,
    string? PresentBy,
    string? GovIdNumber,
    string? Phone,
    string? Email,
    int? WarehouseId,
    string? WarehouseName,
    bool IsActive,
    string? Remark,
    DateTime CreatedAt,
    int SubDealersCount,
    int TotalShippedDocsCount,
    int TotalShippedQty,
    decimal TotalShippedAmount
);

/// <summary>Báo cáo / Danh sách đại lý phân phối tổng hợp kèm 4 thẻ KPI.</summary>
public record DealerReport(
    string? Keyword,
    int? LevelFilter,
    string? ProvinceFilter,
    bool? ActiveFilter,
    int TotalDealers,
    int Level1Count,
    int Level2Count,
    int Level3Count,
    int ActiveCount,
    int InactiveCount,
    int TotalShippedDocsCount,
    int TotalShippedQty,
    decimal TotalShippedAmount,
    List<DealerRow> Rows
);

/// <summary>Chi tiết Đại lý kèm danh sách các đại lý cấp dưới trực thuộc, kho phụ trách và các phiếu xuất kho giao hàng gần nhất.</summary>
public record DealerDetailDto(
    Dealer Dealer,
    Dealer? ParentDealer,
    List<Dealer> SubDealers,
    Warehouse? Warehouse,
    int TotalSubDealers,
    int TotalShippedDocsCount,
    int TotalShippedQty,
    decimal TotalShippedAmount,
    List<StockDoc> RecentDispatches
);

/// <summary>Ô dữ liệu hiển thị sản lượng giao hàng theo từng ngày trên ma trận lịch biểu (port từ Rpt_MapDeliveryOrder_ByInvFIOut Skycic).</summary>
public record MapDeliveryOrderDayCell(
    string DateStr,         // Ngày định dạng yyyy-MM-dd
    int Qty,                // Số lượng xuất giao trong ngày
    bool IsToday,           // Có phải cột ngày hôm nay không (high-line-today)
    bool IsDelayed          // Có bị cảnh báo giao chậm / quá hạn không (high-line-delay)
);

/// <summary>Dòng thông tin hiển thị tiến độ lệnh giao hàng theo phiếu xuất trên ma trận lịch biểu (port từ Rpt_MapDeliveryOrder_ByInvFIOut Skycic: AreaCode, CustomerCode, IF_InvOutNo, ProductCode, Qty, IF_InvOutStatus, CreateDTimeUTC, Rtp_Date).</summary>
public class MapDeliveryOrderRow
{
    public int Stt { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string AreaCode { get; set; } = "";              // Mã khu vực địa bàn (AreaCode)
    public string AreaName { get; set; } = "";              // Tên khu vực địa bàn (AreaName)
    public string CustomerCode { get; set; } = "";          // Mã khách hàng (CustomerCode / CustomerCodeSys)
    public string CustomerName { get; set; } = "";          // Tên khách hàng (CustomerName)
    public string DeliveryOrderNo { get; set; } = "";       // Số phiếu xuất / Lệnh giao hàng (IF_InvOutNo)
    public string DocTypeLabel { get; set; } = "";          // Loại nghiệp vụ xuất (Xuất bán, Xuất thành phẩm, Xuất điều chuyển...)
    public DateTime OrderDate { get; set; }                 // Ngày tạo / Ngày hẹn giao (CreateDTimeUTC / Date)
    public string OrderDateStr => OrderDate.ToString("yyyy-MM-dd");
    public string OrderDateDisplay => OrderDate.ToString("dd/MM/yyyy");
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";           // Mã mặt hàng (ProductCode / ProductCodeUser)
    public string ProductName { get; set; } = "";           // Tên mặt hàng (ProductName)
    public string Uom { get; set; } = "";                   // Đơn vị tính (UnitCode)
    public int TotalQty { get; set; }                       // Tổng số lượng xuất giao (Qty)
    public string Status { get; set; } = "PENDING";         // Trạng thái phiếu (PENDING, APPROVED, POSTED, FINISHED, CANCEL)
    public string StatusLabel { get; set; } = "Chờ giao";
    public string BadgeClass { get; set; } = "bg-warning text-dark";
    public bool IsDelayed { get; set; }                     // Cờ cảnh báo giao chậm: PENDING/DRAFT mà ngày <= Hôm nay
    public string? DeliveryAddress { get; set; }            // Địa chỉ nhận hàng
    public string? DriverInfo { get; set; }                 // Thông tin lái xe, biển số
    public string? Note { get; set; }                       // Ghi chú
    public Dictionary<string, int> DailyQuantities { get; set; } = new(); // Key: yyyy-MM-dd, Value: Qty
    public List<MapDeliveryOrderDayCell> Cells { get; set; } = new();
}

/// <summary>Thống kê tóm tắt tiến độ giao hàng theo khu vực địa bàn.</summary>
public record AreaDeliverySummary(
    string AreaCode,
    string AreaName,
    int TotalOrders,
    int CompletedOrders,
    int PendingOrders,
    int DelayedOrders,
    int TotalDispatchedQty,
    double OnTimeRatePercent
);

/// <summary>Báo cáo Bản đồ lệnh giao hàng theo Phiếu xuất kho tổng hợp kèm KPI và ma trận lịch biểu (port từ Rpt_MapDeliveryOrder_ByInvFIOut Skycic: DateFrom, DateTo, HighLineToday, HighLineDelay).</summary>
public record MapDeliveryOrderReport(
    int? WarehouseId,
    string WarehouseName,
    string? AreaCodeFilter,
    string? CustomerCodeFilter,
    string? StatusFilter,
    string? Keyword,
    DateTime DateFrom,
    DateTime DateTo,
    string TodayStr,
    List<string> ListDates,                 // Danh sách các ngày trong dải ngày yyyy-MM-dd
    int TotalDeliveryOrders,                // Tổng số lệnh / phiếu xuất giao hàng
    int CompletedOrders,                    // Số đơn đã hoàn tất / đã giao hàng
    int PendingOrders,                      // Số đơn đang xử lý / chờ xuất giao
    int DelayedOrders,                      // Số đơn cảnh báo giao chậm / quá hạn (high-line-delay)
    double OnTimeRatePercent,               // Tỷ lệ giao hàng đúng hạn %
    int TotalDispatchedQty,                 // Tổng sản lượng hàng hóa xuất giao
    List<MapDeliveryOrderRow> Rows,         // Dòng ma trận tiến độ
    List<AreaDeliverySummary> AreaSummaries  // Bảng phân bổ theo khu vực
);

/// <summary>Danh mục Loại mẫu in kho (port từ Mst_TempPrintType Skycic: TempPrintType, TempPrintName, Remark, FlagActive).</summary>
public class TempPrintType : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // Mã loại mẫu in (IN, OUT, MOVE, AUDIT, CARTON, BOX, K80)
    public string Name { get; set; } = "";             // Tên loại mẫu in (Phiếu nhập kho, Phiếu xuất kho, Phiếu chuyển kho, Biên bản kiểm kê, Tem thùng carton, Tem đóng hộp, Phiếu in nhiệt K80)
    public string GroupCode { get; set; } = "DOC";     // Nhóm mẫu: DOC (Chứng từ kho) hoặc LABEL (Tem nhãn mã vạch)
    public string? Description { get; set; }          // Mô tả loại biểu mẫu
    public bool IsActive { get; set; } = true;         // Trạng thái hiệu lực (FlagActive)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Biểu mẫu in kho & Thiết kế tem nhãn WMS (port từ InvF_TempPrint Skycic: IF_TempPrintNo, TempPrintType, IF_TempPrintName, NNTName, NNTAddress, NNTPhone, NNTEmail, TempPrintBody, FlagActive, Remark).</summary>
public class TempPrint : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // Mã mẫu in (IF_TempPrintNo, vd: PN_A4_STD, PX_A4_STD, PC_A4_STD, PKK_A4_STD, LABEL_CARTON, LABEL_BOX, PX_K80)
    public string Name { get; set; } = "";             // Tên mẫu in (IF_TempPrintName, vd: Phiếu nhập kho tiêu chuẩn TT200 (A4))
    public string TypeCode { get; set; } = "IN";       // Loại mẫu in liên kết với TempPrintType (IN, OUT, MOVE, AUDIT, CARTON, BOX, K80)
    public string PaperSize { get; set; } = "A4_Portrait"; // Khổ giấy: A4_Portrait, A4_Landscape, A5_Landscape, Label_100x150, Label_100x75, Thermal_K80
    public string UnitName { get; set; } = "CÔNG TY CỔ PHẦN LOGISTICS MINIWMS"; // Tên đơn vị / doanh nghiệp (NNTName)
    public string? UnitAddress { get; set; }           // Địa chỉ công ty (NNTAddress)
    public string? UnitPhone { get; set; }             // Số điện thoại (NNTPhone)
    public string? UnitEmail { get; set; }             // Email liên hệ (NNTEmail)
    public string HeaderTitle { get; set; } = "";      // Tiêu đề biểu mẫu (vd: PHIẾU NHẬP KHO, PHIẾU XUẤT KHO KIÊM BÀN GIAO)
    public string? SubTitle { get; set; }              // Tiêu đề phụ (vd: Mẫu số 01 - VT, Ban hành theo Thông tư số 200/2014/TT-BTC)
    public string BodyTemplateHtml { get; set; } = ""; // Nội dung mã HTML template của biểu mẫu (TempPrintBody) với token thay thế
    public string? NoteFooter { get; set; }            // Lời dặn / Điều khoản chân trang (vd: Đã nhận đủ hàng hóa nguyên đai nguyên kiện...)
    public bool IsDefault { get; set; } = false;       // Là mẫu in mặc định cho loại nghiệp vụ này
    public bool IsActive { get; set; } = true;         // Cờ hoạt động (FlagActive)
    public string? Remark { get; set; }                // Ghi chú
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Dòng thông tin hiển thị mẫu in kho trên danh sách tổng hợp.</summary>
public record TempPrintRow(
    int Id,
    string Code,
    string Name,
    string TypeCode,
    string TypeName,
    string GroupCode,
    string PaperSize,
    string PaperSizeLabel,
    string UnitName,
    string HeaderTitle,
    bool IsDefault,
    bool IsActive,
    string? Remark,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Báo cáo / Danh sách biểu mẫu in kho tổng hợp kèm 4 thẻ KPI.</summary>
public record TempPrintReport(
    string? TypeCodeFilter,
    bool? ActiveFilter,
    string? Keyword,
    int TotalTemplates,
    int ActiveCount,
    int InactiveCount,
    int SupportedTypesCount,
    int DefaultTemplatesCount,
    List<TempPrintRow> Rows
);

/// <summary>Kết quả xem trước nội dung mẫu in được render dữ liệu mẫu thực tế (Live Preview).</summary>
public record TempPrintPreviewResult(
    int Id,
    string Code,
    string Name,
    string TypeCode,
    string TypeName,
    string PaperSize,
    string PaperSizeCss,
    string RenderedHtml
);

// ==================== BÁO CÁO LỊCH SỬ GIAO DỊCH NHẬP XUẤT THEO ĐỐI TÁC (Rpt_Summary_In_Out_Sup_Pivot Skycic) ====================

/// <summary>Dòng chi tiết phát sinh một giao dịch nhập hoặc xuất theo đối tác (port từ Rpt_Summary_In_Out_Sup_Pivot Skycic: DocNo, ApprDateUTC, InvCode, InvName, CustomerCodeSys, CustomerCode, CustomerName, AreaCode, AreaName, ProvinceCode, ProvinceName, ProductCode, ProductCodeUser, ProductName, InventoryAction, InventoryActionDesc, Inv_In_Out_Type, Inv_In_Out_TypeDesc, ProductGrpCode, ProductGrpName, Qty, UnitCode, mu_UnitName, InvInType, InvOutType).</summary>
public record SummaryInOutPartnerPivotItem(
    int Stt,
    string DocNo,
    DateTime DocDate,
    int WarehouseId,
    string WarehouseName,
    string PartnerCode,
    string PartnerName,
    string PartnerType,       // "Nhà cung cấp", "Khách hàng / Đại lý", "Xưởng sản xuất", "Nội bộ"
    string? AreaName,
    string? ProvinceName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string? ProductGrpCode,
    string? ProductGrpName,
    string Uom,
    string ActionType,        // "IN" hoặc "OUT"
    string ActionDesc,        // "Nhập kho" hoặc "Xuất kho"
    string InOutTypeName,     // "Nhập mua NCC", "Xuất bán khách hàng", "Nhập khách trả", "Xuất trả NCC", "Nhập thành phẩm SX", "Xuất thành phẩm", v.v.
    int Quantity,
    decimal UnitPrice,
    decimal Amount,
    string? RefNo,
    string? CreatedBy,
    string? Note,
    string? DocDetailUrl
);

/// <summary>Dòng tổng hợp số lượng & giá trị nhập xuất của một mặt hàng theo từng đối tác (Pivot Row: Partner x Product).</summary>
public record SummaryInOutPartnerPivotRow(
    string PartnerCode,
    string PartnerName,
    string PartnerType,
    string? AreaName,
    string? ProvinceName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string? ProductGrpCode,
    string? ProductGrpName,
    string Uom,
    int TotalInQty,
    decimal TotalInAmount,
    int TotalOutQty,
    decimal TotalOutAmount,
    int NetQty,             // = TotalInQty - TotalOutQty
    decimal NetAmount,      // = TotalInAmount - TotalOutAmount
    int TxCount,            // Số lượt giao dịch chứng từ
    DateTime LastDate
);

/// <summary>Nhóm thông tin phân tích cấp Đối tác trong ma trận Pivot (Pivot Partner Group Header).</summary>
public record SummaryInOutPartnerGroupRow(
    string PartnerCode,
    string PartnerName,
    string PartnerType,
    string? AreaName,
    string? ProvinceName,
    int TotalInQty,
    decimal TotalInAmount,
    int TotalOutQty,
    decimal TotalOutAmount,
    int NetQty,             // = TotalInQty - TotalOutQty
    decimal NetAmount,      // = TotalInAmount - TotalOutAmount
    int ItemsCount,         // Số lượng mặt hàng khác nhau phát sinh giao dịch
    int TxCount,            // Tổng số lượt giao dịch chứng từ
    double SharePercent,    // Tỷ trọng % tổng luân chuyển (In + Out) so với toàn hệ thống
    List<SummaryInOutPartnerPivotRow> ProductRows
);

/// <summary>Báo cáo toàn diện Lịch sử giao dịch Nhập - Xuất theo Đối tác dạng ma trận Pivot (port từ Rpt_Summary_In_Out_Sup_Pivot Skycic).</summary>
public record SummaryInOutPartnerPivotReport(
    int? WarehouseId,
    string WarehouseName,
    string? PartnerCode,
    string? ProductGrpCode,
    int? ProductId,
    string? ActionType,       // "ALL", "IN", "OUT"
    DateTime FromDate,
    DateTime ToDate,
    string? Keyword,
    int TotalPartners,        // Tổng số đối tác phát sinh giao dịch
    int TotalInQty,           // Tổng lượng hàng nhập từ các đối tác
    decimal TotalInAmount,    // Tổng giá trị nhập
    int TotalOutQty,          // Tổng lượng hàng xuất cho các đối tác
    decimal TotalOutAmount,   // Tổng giá trị xuất
    int TotalNetQty,          // Chênh lệch ròng sản lượng (= In - Out)
    decimal TotalNetAmount,   // Chênh lệch ròng giá trị (= In - Out)
    int TotalTxCount,         // Tổng số lượt chứng từ giao dịch
    List<SummaryInOutPartnerGroupRow> PartnerGroups,
    List<SummaryInOutPartnerPivotRow> PivotRows,
    List<SummaryInOutPartnerPivotItem> DetailItems
);





















// ==================== QUẢN LÝ LOẠI TIỀN & TỶ GIÁ NGOẠI TỆ KHO (OS_PrdCenter_Mst_CurrencyEx Skycic) ====================

/// <summary>Danh mục Loại tiền & Tỷ giá quy đổi ngoại tệ kho (port từ OS_PrdCenter_Mst_CurrencyEx Skycic: CurrencyCode, CurrencyName, BaseCurrencyCode, BuyRate, SellRate, InterEx, UpdatedTime, FlagActive, Remark).</summary>
public class CurrencyExchange : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // Mã ngoại tệ (CurrencyCode, vd: USD, EUR, JPY, CNY, GBP, KRW, SGD, THB, VND)
    public string Name { get; set; } = "";             // Tên loại tiền (CurrencyName, vd: Đô la Mỹ, Euro, Yên Nhật, Nhân dân tệ...)
    public string BaseCurrencyCode { get; set; } = "VND"; // Loại tiền cơ sở đối chiếu (BaseCurrencyCode)
    public decimal BuyRate { get; set; } = 1m;         // Tỷ giá mua vào quy đổi sang tiền cơ sở (BuyRate)
    public decimal SellRate { get; set; } = 1m;        // Tỷ giá bán ra quy đổi sang tiền cơ sở (SellRate)
    public decimal InterExRate { get; set; } = 1m;     // Tỷ giá liên ngân hàng / tỷ giá trung tâm (InterEx)
    public string? InterExSource { get; set; }         // Nguồn tham chiếu tỷ giá (Vietcombank, Ngân hàng Nhà nước, VietinBank...)
    public string? Symbol { get; set; }                // Ký hiệu loại tiền ($, €, ¥, £, ₩, ₫)
    public bool IsBase { get; set; } = false;          // Là đồng tiền hạch toán cơ sở (VND)
    public bool IsActive { get; set; } = true;         // Cờ hoạt động / Cho phép giao dịch quy đổi kho (FlagActive)
    public string? Remark { get; set; }                // Ghi chú / Quy ước quy đổi giá vốn ngoại thương
    public DateTime UpdatedTime { get; set; } = DateTime.Now; // Thời điểm cập nhật tỷ giá
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>Dòng hiển thị tỷ giá loại tiền trên bảng danh mục.</summary>
public record CurrencyExchangeRow(
    int Id,
    string Code,
    string Name,
    string BaseCurrencyCode,
    decimal BuyRate,
    decimal SellRate,
    decimal InterExRate,
    string? InterExSource,
    string? Symbol,
    bool IsBase,
    bool IsActive,
    string? Remark,
    DateTime UpdatedTime,
    string UpdatedTimeDisplay,
    DateTime CreatedAt
);

/// <summary>Báo cáo danh mục tỷ giá ngoại tệ tổng hợp kèm 4 thẻ KPI.</summary>
public record CurrencyExchangeReport(
    string? Keyword,
    bool? ActiveFilter,
    int TotalCurrencies,
    int ActiveCount,
    int InactiveCount,
    decimal UsdBuyRate,
    decimal UsdSellRate,
    DateTime LastUpdated,
    List<CurrencyExchangeRow> Rows
);

/// <summary>Kết quả quy đổi tiền tệ theo tỷ giá kho.</summary>
public record CurrencyConvertResultDto(
    decimal SourceAmount,
    string SourceCurrency,
    string TargetCurrency,
    decimal ConvertedAmount,
    decimal AppliedRate,
    string RateType,     // "buy", "sell", "inter"
    string Formula
);

// ==================== QUẢN LÝ QUY CÁCH & THUỘC TÍNH KỸ THUẬT SẢN PHẨM KHO (OS_PrdCenter_Mst_Spec / Mst_Spec Skycic) ====================

/// <summary>Danh mục Quy cách & Thuộc tính / Đặc tính kỹ thuật hàng hóa kho (port từ Mst_Spec & OS_PrdCenter_Mst_Spec Skycic: SpecCode, SpecName, SpecDesc, ModelCode, SpecType1, SpecType2, Color, FlagHasSerial, FlagHasLOT, StandardUnitCode, DefaultUnitCode, FlagActive, Remark).</summary>
public class ProductSpec : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";             // Mã quy cách / Mã cấu hình kỹ thuật sản phẩm (SpecCode, vd: SPC-AO-SM-TRANG-L, SPC-QUAN-JN-DEN-32...)
    public string Name { get; set; } = "";             // Tên quy cách / Tên phiên bản hàng hóa (SpecName, vd: Áo sơ mi Slimfit Trắng - Size L, Quần Jeans Nam Đen - W32/L32...)
    public string? SpecDesc { get; set; }             // Mô tả thông số kỹ thuật chi tiết (SpecDesc)
    public string? ModelCode { get; set; }            // Thuộc Dòng sản phẩm / Model nào (ModelCode, vd: MD-M10-SLIM, MD-LV-501...)
    public string? SpecType1 { get; set; }            // Phân loại cấp 1 (SpecType1, vd: Tiêu chuẩn Standard, Cao cấp Premium, Công nghiệp Industrial, Xuất khẩu Export)
    public string? Color { get; set; }                // Phiên bản màu sắc (Color, vd: Trắng White, Đen Matt Black, Xanh Navy, Bạc Silver)
    public string? StandardUnitCode { get; set; }     // Đơn vị tính tiêu chuẩn (StandardUnitCode / DefaultUnitCode, vd: cái, chiếc, mét, bộ)
    public bool FlagHasSerial { get; set; } = false;   // Cờ quản lý theo Serial / Barcode cá thể hóa (FlagHasSerial: bắt buộc quét mã vạch cá thể khi xuất nhập kho)
    public bool FlagHasLOT { get; set; } = false;      // Cờ quản lý theo Lô sản xuất & Hạn sử dụng (FlagHasLOT: bắt buộc nhập số lô và HSD theo dõi FEFO/FIFO)
    public bool IsActive { get; set; } = true;         // Trạng thái áp dụng (FlagActive: 1 - Đang áp dụng, 0 - Tạm dừng)
    public string? Remark { get; set; }               // Ghi chú kỹ thuật, tiêu chuẩn đóng gói hoặc điều kiện bảo quản kho (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Dòng thông tin hiển thị Quy cách sản phẩm kèm dòng sản phẩm, thương hiệu, cờ Serial/Lô, số lượng mặt hàng và tổng tồn kho thực tế.</summary>
public record ProductSpecRow(
    int Id,
    string Code,
    string Name,
    string? SpecDesc,
    string? ModelCode,
    string? ModelName,
    string? BrandName,
    string? SpecType1,
    string? Color,
    string? StandardUnitCode,
    bool FlagHasSerial,
    bool FlagHasLOT,
    bool IsActive,
    string? Remark,
    DateTime CreatedAt,
    int ProductCount,
    int TotalStockQty
);

/// <summary>Báo cáo / Danh sách quy cách sản phẩm tổng hợp kèm 4 thẻ KPI.</summary>
public record ProductSpecReport(
    string? Keyword,
    string? ModelCodeFilter,
    string? SpecType1Filter,
    bool? HasSerialFilter,
    bool? HasLotFilter,
    bool? ActiveFilter,
    int TotalSpecs,
    int ActiveCount,
    int HasSerialCount,
    int HasLotCount,
    int TotalProductsMapped,
    int TotalStockQty,
    List<ProductSpecRow> Rows
);

/// <summary>Chi tiết Quy cách sản phẩm kèm thông tin dòng model và danh sách mặt hàng thực tế áp dụng quy cách này.</summary>
public record ProductSpecDetailDto(
    ProductSpec Spec,
    ProductModel? Model,
    Brand? Brand,
    List<Product> Products,
    int TotalProducts,
    int TotalStockQty
);


// ==================== QUẢN LÝ BẢNG GIÁ QUY CÁCH SẢN PHẨM KHO (OS_PrdCenter_Mst_SpecPrice / Mst_SpecPrice Skycic) ====================

/// <summary>Bảng giá quy cách sản phẩm kho (port từ OS_PrdCenter_Mst_SpecPrice & Mst_SpecPrice Skycic: SpecCode, UnitCode, BuyPrice, SellPrice, CurrencyCode, VATRateCode, DiscountVND, EffectDTimeStart, EffectDTimeEnd, FlagActive, Remark).</summary>
public class SpecPrice : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string SpecCode { get; set; } = "";             // Mã quy cách sản phẩm (SpecCode, vd: SPC-AO-SM-TRANG-L, SPC-QUAN-JN-DEN-32...)
    public string UnitCode { get; set; } = "cái";          // Đơn vị tính (UnitCode, vd: cái, chiếc, hộp, thùng...)
    public decimal BuyPrice { get; set; } = 0m;            // Giá mua / nhập kho dự kiến (BuyPrice)
    public decimal SellPrice { get; set; } = 0m;           // Giá bán niêm yết / xuất kho (SellPrice)
    public string CurrencyCode { get; set; } = "VND";      // Mã loại tiền tệ (CurrencyCode: VND, USD, EUR... FK liên kết CurrencyExchange)
    public string? VATRateCode { get; set; } = "VAT10";    // Mã thuế suất VAT (VATRateCode: VAT0, VAT5, VAT8, VAT10, KCT)
    public decimal DiscountVND { get; set; } = 0m;         // Mức chiết khấu định mức (DiscountVND)
    public DateTime EffectDTimeStart { get; set; } = DateTime.Now; // Thời điểm bắt đầu hiệu lực giá
    public DateTime? EffectDTimeEnd { get; set; }          // Thời điểm kết thúc hiệu lực giá (nếu có)
    public bool IsActive { get; set; } = true;             // Trạng thái áp dụng (FlagActive: 1 - Đang áp dụng, 0 - Tạm dừng)
    public string? Remark { get; set; }                    // Ghi chú chính sách giá / phân khúc áp dụng (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Giá bán ròng sau chiết khấu (Net Price) = SellPrice - DiscountVND</summary>
    public decimal NetSellPrice => Math.Max(0m, SellPrice - DiscountVND);

    /// <summary>Mức chênh lệch lợi nhuận gộp định mức = NetSellPrice - BuyPrice</summary>
    public decimal GrossProfit => NetSellPrice - BuyPrice;

    /// <summary>Tỷ suất biên lợi nhuận % định mức = (GrossProfit / NetSellPrice) * 100</summary>
    public double GrossMarginPercent => NetSellPrice > 0 ? Math.Round((double)(GrossProfit / NetSellPrice * 100m), 1) : 0.0;
}

/// <summary>Dòng thông tin hiển thị bảng giá quy cách sản phẩm kèm thông số quy cách và đơn giá sau chiết khấu.</summary>
public record SpecPriceRow(
    int Id,
    string SpecCode,
    string? SpecName,
    string? ModelCode,
    string? ModelName,
    string? Color,
    string UnitCode,
    decimal BuyPrice,
    decimal SellPrice,
    decimal DiscountVND,
    decimal NetSellPrice,
    decimal GrossProfit,
    double GrossMarginPercent,
    string CurrencyCode,
    string? CurrencySymbol,
    string? VATRateCode,
    DateTime EffectDTimeStart,
    DateTime? EffectDTimeEnd,
    bool IsActive,
    string? Remark,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Báo cáo / Danh sách bảng giá quy cách sản phẩm tổng hợp kèm 4 thẻ KPI.</summary>
public record SpecPriceReport(
    string? Keyword,
    string? SpecCodeFilter,
    string? UnitCodeFilter,
    string? CurrencyCodeFilter,
    bool? ActiveFilter,
    int TotalPrices,
    int ActiveCount,
    int ForeignCurrencyCount,
    decimal AverageSellPrice,
    List<SpecPriceRow> Rows
);

/// <summary>Chi tiết Bảng giá quy cách sản phẩm kèm thông tin quy cách và định giá quy đổi ngoại tệ.</summary>
public record SpecPriceDetailDto(
    SpecPrice Item,
    ProductSpec? Spec,
    CurrencyExchange? Currency,
    List<CurrencyValuationRow> Valuations
);

/// <summary>Dòng định giá quy đổi theo loại tiền tệ kho.</summary>
public record CurrencyValuationRow(
    string CurrencyCode,
    string CurrencyName,
    string Symbol,
    decimal BuyPriceInCurrency,
    decimal SellPriceInCurrency,
    decimal NetPriceInCurrency,
    decimal AppliedRate
);

// ==================== QUẢN LÝ DANH MỤC THUẾ SUẤT VAT HÀNG HÓA KHO (OS_PrdCenter_Mst_VATRate / Mst_VATRate Skycic) ====================

/// <summary>Danh mục thuế suất VAT hàng hóa kho (port từ OS_PrdCenter_Mst_VATRate & Mst_VATRate Skycic: VATRateCode, VATRate, VATDesc, FlagActive, Remark).</summary>
public class VATRate : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string VATRateCode { get; set; } = "";          // Mã thuế suất VAT (VATRateCode, vd: VAT0, VAT5, VAT8, VAT10, KCT)
    public decimal Rate { get; set; } = 0m;                // Tỷ lệ % thuế suất (VATRate: 0, 5, 8, 10...)
    public string VATDesc { get; set; } = "";              // Mô tả / Tên thuế suất (VATDesc, vd: Thuế suất GTGT chuẩn 10%)
    public bool IsActive { get; set; } = true;             // Trạng thái áp dụng (FlagActive: 1 - Áp dụng, 0 - Tạm dừng)
    public string? Remark { get; set; }                    // Căn cứ pháp lý / Ghi chú chính sách thuế (Remark)
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Dòng thông tin hiển thị danh mục thuế suất VAT kèm số lượng bảng giá quy cách và chứng từ liên kết.</summary>
public record VATRateRow(
    int Id,
    string VATRateCode,
    decimal Rate,
    string VATDesc,
    bool IsActive,
    string? Remark,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int MappedSpecPriceCount
);

/// <summary>Báo cáo / Danh sách thuế suất VAT tổng hợp kèm 4 thẻ KPI.</summary>
public record VATRateReport(
    string? Keyword,
    bool? ActiveFilter,
    int TotalRates,
    int ActiveCount,
    int InactiveCount,
    int TotalMappedSpecs,
    List<VATRateRow> Rows
);

/// <summary>Chi tiết Thuế suất VAT kèm danh sách bảng giá quy cách đang áp dụng mức thuế này.</summary>
public record VATRateDetailDto(
    VATRate Item,
    List<SpecPrice> MappedPrices,
    int TotalMappedPrices
);

/// <summary>Kết quả tính toán nhanh thuế VAT và tổng thanh toán sau thuế.</summary>
public record VatCalculationResult(
    decimal NetAmount,
    string VATRateCode,
    decimal Rate,
    decimal VatAmount,
    decimal TotalAmount
);

// ==================== BÁO CÁO TỒN KHO TẠI THỜI ĐIỂM (Rpt_Inv_InventoryBalance_ByPeriod Skycic) ====================

/// <summary>Dòng báo cáo Tồn kho tại thời điểm (Point-in-time Inventory Balance - port từ Rpt_Inv_InventoryBalance_ByPeriod Skycic).
/// Tồn tại mốc thời gian = Tổng nhập đã duyệt (Qty - QtyReturn) - Tổng xuất đã duyệt, tính đến mốc chốt.</summary>
public record PointInTimeBalanceRow(
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    int WarehouseId,
    string WarehouseName,
    int QtyIn,               // Tổng nhập lũy kế đến mốc (Qty_InvfIn)
    int QtyOut,              // Tổng xuất lũy kế đến mốc (Qty_InvfOut)
    int QtyAtDate,           // Tồn tại thời điểm = QtyIn - QtyOut (QtyTotalOK Skycic)
    int QtyCurrent,          // Tồn hiện tại (để so sánh biến động)
    int QtyDelta,            // Chênh lệch tồn hiện tại - tồn tại mốc (QtyCurrent - QtyAtDate)
    decimal CostPrice,       // Đơn giá vốn kho hiện hành
    decimal ValueAtDate,     // Giá trị tồn tại mốc = QtyAtDate * CostPrice
    string MovementStatus,   // Trạng thái biến động (Tăng tồn / Giảm tồn / Không đổi)
    string MovementBadgeClass
);

/// <summary>Báo cáo Tồn kho tại thời điểm tổng hợp (port từ Rpt_Inv_InventoryBalance_ByPeriod Skycic).</summary>
public record PointInTimeBalanceReport(
    int? WarehouseId,
    string WarehouseName,
    DateTime AsOfDate,           // Mốc thời điểm chốt tồn (ReportDTimeUTC Skycic)
    string? Keyword,
    int TotalItems,              // Số mặt hàng có tồn tại mốc
    int TotalQtyAtDate,          // Tổng tồn tại mốc
    int TotalQtyCurrent,         // Tổng tồn hiện tại
    int TotalQtyDelta,           // Tổng chênh lệch
    decimal TotalValueAtDate,    // Tổng giá trị tồn tại mốc
    int IncreasedCount,          // Số mặt hàng tăng tồn
    int DecreasedCount,          // Số mặt hàng giảm tồn
    int UnchangedCount,          // Số mặt hàng không đổi
    List<PointInTimeBalanceRow> Rows
);


// ==================== SỔ GIAO DỊCH KHO / NHẬT KÝ BIẾN ĐỘNG TỒN KHO (Inv_InventoryTransaction Skycic) ====================

/// <summary>Loại nghiệp vụ phát sinh giao dịch kho (port từ FunctionName / RefType của Inv_InventoryTransaction Skycic).</summary>
public enum InventoryTxnType
{
    In = 0,          // Nhập kho mua hàng / thương mại (InvF_InventoryIn)
    Out = 1,         // Xuất kho bán hàng / thương mại (InvF_InventoryOut)
    Move = 2,        // Điều chuyển kho (InvF_MoveOrd)
    Audit = 3,       // Cân bằng kiểm kê (InvF_InvAudit)
    ReturnSup = 4,   // Xuất trả hàng nhà cung cấp (InvF_InventoryReturnSup)
    CusReturn = 5,   // Nhập hàng khách trả lại (InvF_InventoryCusReturn)
    InFG = 6,        // Nhập kho thành phẩm sản xuất (InvF_InventoryInFG)
    OutFG = 7,       // Xuất kho thành phẩm (InvF_InventoryOutFG)
    Adjust = 8       // Điều chỉnh tồn thủ công / khác (Manual Adjust)
}

/// <summary>Phân loại chất lượng hàng hóa trong giao dịch (OK = hàng tốt, NG = hàng lỗi/hỏng).</summary>
public enum InventoryTxnQuality
{
    OK = 0,   // Hàng đạt chuẩn (QtyChTotalOK / QtyChBlockOK)
    NG = 1    // Hàng lỗi / hỏng / chờ xử lý (QtyChTotalNG / QtyChBlockNG)
}

/// <summary>Sổ giao dịch kho / Nhật ký biến động tồn kho (port từ Inv_InventoryTransaction Skycic).
/// Mỗi bút toán ghi nhận phần thay đổi (delta) số lượng tồn kho theo kho + mặt hàng, kèm nghiệp vụ phát sinh
/// và chứng từ tham chiếu. Đây là sổ cái bất biến (immutable ledger) của mọi biến động tồn kho.</summary>
public class InventoryTransaction : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int WarehouseId { get; set; }                     // Kho phát sinh giao dịch (InvCode)
    public int ProductId { get; set; }                       // Mặt hàng (ProductCode)
    public InventoryTxnType TxnType { get; set; }            // Loại nghiệp vụ phát sinh (FunctionName)
    public string FunctionName { get; set; } = "";           // Tên hàm nghiệp vụ gốc (vd: InvF_InventoryCusReturn_APPRX)
    public InventoryTxnQuality Quality { get; set; } = InventoryTxnQuality.OK; // Chất lượng hàng (OK / NG)

    public int QtyChTotalOK { get; set; }                    // Thay đổi tổng tồn OK (QtyChTotalOK)
    public int QtyChBlockOK { get; set; }                    // Thay đổi tồn bị khóa OK (QtyChBlockOK)
    public int QtyChTotalNG { get; set; }                    // Thay đổi tổng tồn NG (QtyChTotalNG)
    public int QtyChBlockNG { get; set; }                    // Thay đổi tồn bị khóa NG (QtyChBlockNG)

    public string? RefType { get; set; }                     // Loại chứng từ tham chiếu (RefType)
    public string? RefCode00 { get; set; }                   // Mã chứng từ gốc (RefCode00 - vd: số phiếu kho)
    public string? RefCode01 { get; set; }                   // Mã tham chiếu phụ 1 (RefCode01)
    public string? RefCode02 { get; set; }                   // Mã tham chiếu phụ 2 (RefCode02)
    public string? RefCode03 { get; set; }                   // Mã tham chiếu phụ 3 (RefCode03)
    public string? RefCode04 { get; set; }                   // Mã tham chiếu phụ 4 (RefCode04)
    public string? RefCode05 { get; set; }                   // Mã tham chiếu phụ 5 (RefCode05)

    public string? Remark { get; set; }                      // Diễn giải giao dịch
    public string CreatedBy { get; set; } = "";              // Người tạo bút toán (CreateBy)
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // Thời điểm ghi sổ (CreateDTimeUTC)

    public Warehouse Warehouse { get; set; } = null!;
    public Product Product { get; set; } = null!;

    /// <summary>Tổng thay đổi tồn vật lý (OK + NG) = QtyChTotalOK + QtyChTotalNG.</summary>
    public int QtyChangeTotal => QtyChTotalOK + QtyChTotalNG;

    /// <summary>Thay đổi tồn khả dụng = (QtyChTotalOK - QtyChBlockOK) + (QtyChTotalNG - QtyChBlockNG).</summary>
    public int QtyChangeAvail => (QtyChTotalOK - QtyChBlockOK) + (QtyChTotalNG - QtyChBlockNG);
}

/// <summary>Dòng hiển thị Sổ giao dịch kho (port từ Inv_InventoryTransaction Skycic).</summary>
public record InventoryTransactionRow(
    int Id,
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string Uom,
    InventoryTxnType TxnType,
    string TxnTypeLabel,
    string BadgeClass,
    string FunctionName,
    InventoryTxnQuality Quality,
    string QualityLabel,
    int QtyChTotalOK,
    int QtyChBlockOK,
    int QtyChTotalNG,
    int QtyChBlockNG,
    int QtyChangeTotal,
    int QtyChangeAvail,
    string? RefType,
    string? RefCode00,
    string? RefCode01,
    string? Remark,
    string CreatedBy,
    DateTime CreatedAt
);

/// <summary>Báo cáo Sổ giao dịch kho / Nhật ký biến động tồn kho tổng hợp (port từ Inv_InventoryTransaction Skycic).</summary>
public record InventoryTransactionReport(
    int? WarehouseId,
    string WarehouseName,
    int? ProductId,
    string ProductName,
    InventoryTxnType? TxnTypeFilter,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Keyword,
    int TotalTransactions,
    int TotalQtyIn,          // Tổng nhập (delta dương)
    int TotalQtyOut,         // Tổng xuất (delta âm, giá trị tuyệt đối)
    int NetQtyChange,        // Biến động ròng = TotalQtyIn - TotalQtyOut
    int InCount,             // Số bút toán nhập
    int OutCount,            // Số bút toán xuất
    int NgCount,             // Số bút toán liên quan hàng NG
    List<InventoryTransactionRow> Rows
);

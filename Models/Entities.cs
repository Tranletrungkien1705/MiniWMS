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



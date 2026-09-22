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

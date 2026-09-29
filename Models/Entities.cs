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

public class Warehouse : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Keeper { get; set; }        // thủ kho
}

/// <summary>Mặt hàng/vật tư. Cột lấy theo danh mục vật tư kho (Skycic.Inventory): nhóm, barcode, giá vốn/bán, tồn min/max.</summary>
public class Product : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Uom { get; set; } = "cái";
    public string? Category { get; set; }      // nhóm hàng
    public string? Barcode { get; set; }
    public decimal CostPrice { get; set; }     // giá vốn (để định giá tồn)
    public decimal SalePrice { get; set; }     // giá bán
    public int MinStock { get; set; }          // tồn tối thiểu (cảnh báo)
    public int MaxStock { get; set; }          // tồn tối đa
}

/// <summary>Phiếu kho (nhập/xuất/chuyển). Post → cập nhật tồn. Đối tác = NCC (nhập) hoặc khách (xuất).</summary>
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
    public string? RefNo { get; set; }          // số chứng từ gốc (HĐ mua/bán)
    public string? PartnerName { get; set; }    // nhà cung cấp / khách hàng
    public string CreatedBy { get; set; } = "";
    public DocStatus Status { get; set; } = DocStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? TraceCode { get; set; }      // mã truy xuất lô hàng (tích hợp MiniTrace) khi ghi sổ
    public string? StampBatchCode { get; set; } // mã lô tem chính hãng (tích hợp MiniStamp) khi nhập kho
    public string? StampSampleQr { get; set; }  // 1 mã QR mẫu để quét thử

    // ── PORT 1:1 (2020.3.Skycic.Inventory) — phục vụ báo cáo THẺ KHO ──────────
    /// <summary>
    /// Nghiệp vụ sinh ra phiếu, để Thẻ kho dịch ra cột "Diễn giải".
    /// Port 1:1 cột <c>InvF_WarehouseCard.InventoryAction</c>; tập giá trị lấy từ
    /// idn.Skycic.Inventory.Biz/Form.cs:139478 và Form.cs:139494
    /// ('IN','AUDITIN','MOVEORDIN','MOVEAUDITIN','CUSRETURN' | 'OUT','AUDITOUT','MOVEORDOUT','MOVEAUDITOUT','RETURNSUP').
    /// Phiếu Chuyển kho lưu gốc "MOVEORD" rồi Thẻ kho nở thành 2 dòng OUT/IN — vì nguồn
    /// KHÔNG lọc bỏ chiều OUT: Form.cs:135508 dòng lọc
    /// "and t.InventoryAction not in ('MOVEORDOUT','MOVEAUDITOUT')" ĐANG BỊ COMMENT.
    /// </summary>
    public string? InvAction { get; set; }
    public string? CfMixCode01 { get; set; }    // nguồn: InvF_WarehouseCard.InvFCFMixCode01 — Form.cs:135692 (lưới "Số hợp đồng")
    public string? CfMixCode02 { get; set; }    // nguồn: InvFCFMixCode02 — Form.cs:135698 (lưới "Số Container")
    public string? CfMixCode03 { get; set; }    // nguồn: InvFCFMixCode03 — Form.cs:135704 (lưới "Biển số xe")

    public Warehouse? FromWarehouse { get; set; }
    public Warehouse? ToWarehouse { get; set; }
    public List<StockDocLine> Lines { get; set; } = [];

    public int TotalQty => Lines.Sum(l => l.Quantity);
    /// <summary>Giá trị phiếu = Σ(SL × đơn giá).</summary>
    public decimal TotalValue => Lines.Sum(l => l.Quantity * l.UnitPrice);
}

public class StockDocLine : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int DocId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }      // đơn giá dòng (định giá nhập/xuất)
    public string? LotNo { get; set; }          // số lô
    public StockDoc Doc { get; set; } = null!;
    public Product Product { get; set; } = null!;

    public decimal LineValue => Quantity * UnitPrice;
}

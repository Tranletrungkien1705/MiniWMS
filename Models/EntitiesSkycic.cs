namespace MiniWMS.Models;

// ─────────────────────────────────────────────────────────────────────────────
// PORT 1:1 — dự án nguồn 2020.3.Skycic.Inventory (Dev/V10)
// Mọi cột đều dẫn nguồn theo dạng <file>:<dòng>. Không suy diễn: cột nào không
// tra được thì ghi rõ trong _labs/_audit/AGENT_MiniWMS.md mục 5.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Trạng thái phiếu kiểm kê / phiếu điều chuyển.
/// Port 1:1 hằng số nguồn: idn.Skycic.Inventory.Constants/Const.Main.BE.cs:418-424
/// (class IF_InvAuditStatus) — PENDING / APPROVE / CANCEL / FINISH / ERROR.
/// Dùng chung cho IF_MOStatus vì nguồn khai cùng bộ 5 giá trị (Const.Main.BE.cs:409-416).
/// </summary>
public enum FormStatus { Pending = 0, Approve = 1, Cancel = 2, Finish = 3, Error = 4 }

/// <summary>
/// Chiều tác động kho của 1 dòng kiểm kê.
/// Port 1:1 cột InvF_InvAuditDtl.InventoryAction — nguồn lọc 'IN' ở
/// idn.Skycic.Inventory.Biz/Form.cs:78068 và vùng 'OUT' ở Form.cs:78139-78141
/// (chiều OUT lấy Qty = QtyInit - QtyActual).
/// </summary>
public enum AuditAction { In = 0, Out = 1 }

/// <summary>
/// Phiếu kiểm kê kho.
/// Port 1:1 bảng <c>InvF_InvAudit</c>. Nguồn:
/// idn.Skycic.Inventory.Common/Models/InvF_InvAudit.cs:9-50 (định nghĩa cột),
/// idn.Skycic.Inventory.Website/Views/InvFInvAudit/Create.cshtml:78-132 (trường trên form),
/// idn.Skycic.Inventory.Biz/Form.cs:72869-72886 (danh sách cột ghi vào #input_InvF_InvAudit).
/// </summary>
public class InvAudit : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }                     // nguồn: InvF_InvAudit.OrgID — InvF_InvAudit.cs:15
    public string Code { get; set; } = "";              // nguồn: IF_InvAudNo, Số phiếu kiểm kê — InvF_InvAudit.cs:11
    public int WarehouseId { get; set; }                // nguồn: InvCodeAudit, Kho kiểm kê — InvF_InvAudit.cs:17
    public FormStatus Status { get; set; } = FormStatus.Pending;   // nguồn: IF_InvAuditStatus — InvF_InvAudit.cs:41
    public string? Remark { get; set; }                 // nguồn: Remark — InvF_InvAudit.cs:39
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // nguồn: CreateDTimeUTC, Ngày kiểm kê — InvF_InvAudit.cs:19
    public string CreatedBy { get; set; } = "";         // nguồn: CreateBy, người kiểm kê — InvF_InvAudit.cs:21
    public DateTime? UpdatedAt { get; set; }            // nguồn: LUDTimeUTC — InvF_InvAudit.cs:23
    public string? UpdatedBy { get; set; }              // nguồn: LUBy — InvF_InvAudit.cs:25
    public DateTime? ApprovedAt { get; set; }           // nguồn: ApprDTimeUTC — InvF_InvAudit.cs:27
    public string? ApprovedBy { get; set; }             // nguồn: ApprBy — InvF_InvAudit.cs:29
    public DateTime? FinishedAt { get; set; }           // nguồn: FinishDTimeUTC — InvF_InvAudit.cs:31
    public string? FinishedBy { get; set; }             // nguồn: FinishBy — InvF_InvAudit.cs:33
    public DateTime? CancelledAt { get; set; }          // nguồn: CancelDTimeUTC — InvF_InvAudit.cs:35
    public string? CancelledBy { get; set; }            // nguồn: CancelBy — InvF_InvAudit.cs:37
    public DateTime LogLuAt { get; set; } = DateTime.Now;   // nguồn: LogLUDTimeUTC — InvF_InvAudit.cs:43
    public string? LogLuBy { get; set; }                // nguồn: LogLUBy — InvF_InvAudit.cs:45

    /// <summary>
    /// Mã các phiếu Nhập/Xuất điều chỉnh do phiếu kiểm kê này sinh ra khi Chốt (FINISH).
    /// Nguồn: Form.cs:78031-78038 lấy số phiếu qua Seq IFInvInNo rồi gọi InvF_InventoryIn_SaveX
    /// (và nhánh IFInvOutNo / InvF_InventoryOut_SaveX cho chiều xuất).
    /// Ở nguồn liên kết nằm ở phía BÊN KIA: cột <c>InvF_InventoryIn.IF_InvAudNo</c>
    /// (idn.Skycic.Inventory.Constants/Const.Main.cs:2217) — đích cũng làm đúng thế
    /// (StockDoc.RefNo = mã phiếu kiểm kê). Cột này chỉ là CHỈ MỤC NGƯỢC cho tiện tra,
    /// không có ở nguồn ⇒ đã khai ở mục 5 của báo cáo.
    /// </summary>
    public string? AdjustDocCodes { get; set; }

    public Warehouse? Warehouse { get; set; }
    public List<InvAuditLine> Lines { get; set; } = [];

    /// <summary>Tổng số lượng lý thuyết. Nguồn: LstProduct.cshtml:247-249 (cộng QtyInit khi phiếu chưa FINISH).</summary>
    public decimal TotalQtyInit => Lines.Sum(l => l.QtyInit);
    /// <summary>Tổng số lượng thực tế. Nguồn: LstProduct.cshtml:241-245 (khi status == "FINISH" thì cộng QtyActual).</summary>
    public decimal TotalQtyActual => Lines.Sum(l => l.QtyActual);
    /// <summary>Tổng lệch. Nguồn: LstProduct.cshtml:305 — QtyRemain = SumQtyActual - SumQty (chốt HuongNV 2023-01-06,
    /// dòng 304 là công thức CŨ đã bị comment: SumQty - SumQtyActual ⇒ không dùng).</summary>
    public decimal TotalQtyDiff => TotalQtyActual - TotalQtyInit;
}

/// <summary>
/// Dòng hàng hoá của phiếu kiểm kê.
/// Port 1:1 bảng <c>InvF_InvAuditDtl</c>. Nguồn:
/// idn.Skycic.Inventory.Common/Models/InvF_InvAuditDtl.cs:10-61,
/// Views/InvFInvAudit/LstProduct.cshtml:56-86 (cột lưới),
/// Biz/Form.cs:72950-72960 (chuẩn hoá tham số vào: InvCodeInit/InvCodeActual/ProductCode/QtyInit/UnitCode/Remark).
/// </summary>
public class InvAuditLine : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }
    public int AuditId { get; set; }                    // nguồn: IF_InvAudNo (khoá phiếu) — InvF_InvAuditDtl.cs:12
    public int ProductId { get; set; }                  // nguồn: ProductCode, Mã hàng hóa — InvF_InvAuditDtl.cs:18
    public string? LocationInit { get; set; }           // nguồn: InvCodeInit, Vị trí lý thuyết — InvF_InvAuditDtl.cs:14
    public string? LocationActual { get; set; }         // nguồn: InvCodeActual, Vị trí thực tế — InvF_InvAuditDtl.cs:16
    public decimal QtyInit { get; set; }                // nguồn: QtyInit, Số lượng lý thuyết — InvF_InvAuditDtl.cs:20
    public decimal QtyActual { get; set; }              // nguồn: QtyActual, Số lượng thực tế — InvF_InvAuditDtl.cs:22
    public decimal UpAudit { get; set; }                // nguồn: UPAudit, Đơn giá — InvF_InvAuditDtl.cs:24
    public decimal ValAudit { get; set; }               // nguồn: ValAudit, Thành tiền — InvF_InvAuditDtl.cs:26
    public decimal Vat { get; set; }                    // nguồn: VAT, Thuế suất — InvF_InvAuditDtl.cs:28
    public string? UnitCode { get; set; }               // nguồn: UnitCode, Đơn vị — InvF_InvAuditDtl.cs:30
    public AuditAction InventoryAction { get; set; }    // nguồn: InventoryAction — InvF_InvAuditDtl.cs:32

    /// <summary>Cờ hàng còn tồn tại trong kho. Nguồn: FlagExist — InvF_InvAuditDtl.cs:34;
    /// LstProduct.cshtml:216-219 nếu FlagExist == "0" thì tô đỏ dòng.</summary>
    public bool FlagExist { get; set; } = true;
    /// <summary>Cờ đã kiểm. Nguồn: FlagAudit — InvF_InvAuditDtl.cs:36;
    /// LstProduct.cshtml:211-213: "1" ⇒ "Đã kiểm", ngược lại "Chưa kiểm".</summary>
    public bool FlagAudit { get; set; }
    public FormStatus StatusDtl { get; set; } = FormStatus.Pending;  // nguồn: IF_InvAuditStatusDtl — InvF_InvAuditDtl.cs:38
    public string? Remark { get; set; }                 // nguồn: Remark — InvF_InvAuditDtl.cs:40
    public DateTime LogLuAt { get; set; } = DateTime.Now;   // nguồn: LogLUDTimeUTC — InvF_InvAuditDtl.cs:42
    public string? LogLuBy { get; set; }                // nguồn: LogLUBy — InvF_InvAuditDtl.cs:44

    public InvAudit Audit { get; set; } = null!;
    public Product Product { get; set; } = null!;

    /// <summary>Lệch = thực tế − lý thuyết. Nguồn: Form.cs:78059
    /// (IsNull(t.QtyActual, 0.0) - IsNull(t.QtyInit, 0.0)) cho chiều IN.</summary>
    public decimal QtyDiff => QtyActual - QtyInit;

    /// <summary>
    /// Tiền VAT. Port công thức THẬT của nguồn (tra CHIMUC_CONGTHUC.tsv →
    /// idn.Skycic.Inventory.Biz/Form.cs:88122):
    /// ValVAT = IsNull(UPAudit, 0.0) * Qty * IsNull(VAT, 0.0) / 100.
    /// </summary>
    public decimal ValVat => UpAudit * QtyActual * Vat / 100m;
}

/// <summary>
/// Phiếu điều chuyển kho (lệnh điều chuyển, có luồng duyệt riêng — khác phiếu Chuyển kho StockDoc).
/// Port 1:1 bảng <c>InvF_MoveOrd</c>. Nguồn:
/// idn.Skycic.Inventory.Common/Models/InvF_MoveOrd.cs:9-51,
/// Views/InvF_MoveOrd/Create.cshtml:79-133 (trường header trên form).
/// </summary>
public class MoveOrd : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }                     // nguồn: OrgID — InvF_MoveOrd.cs:15
    public string Code { get; set; } = "";              // nguồn: IF_MONo, Số phiếu điều chuyển — InvF_MoveOrd.cs:11
    public string? MoveOrdType { get; set; }            // nguồn: MoveOrdType, Loại điều chuyển — InvF_MoveOrd.cs:17
    public int OutWarehouseId { get; set; }             // nguồn: InvCodeOut, Kho xuất — InvF_MoveOrd.cs:19
    public int InWarehouseId { get; set; }              // nguồn: InvCodeIn, Kho nhập — InvF_MoveOrd.cs:21
    public string? AuditCode { get; set; }              // nguồn: IF_InvAudNo, Số phiếu kiểm kê — InvF_MoveOrd.cs:23
    public DateTime CreatedAt { get; set; } = DateTime.Now;  // nguồn: CreateDTimeUTC — InvF_MoveOrd.cs:25
    public string CreatedBy { get; set; } = "";         // nguồn: CreateBy — InvF_MoveOrd.cs:27
    public DateTime? UpdatedAt { get; set; }            // nguồn: LUDTimeUTC — InvF_MoveOrd.cs:29
    public string? UpdatedBy { get; set; }              // nguồn: LUBy — InvF_MoveOrd.cs:31
    public DateTime? ApprovedAt { get; set; }           // nguồn: ApprDTimeUTC — InvF_MoveOrd.cs:33
    public string? ApprovedBy { get; set; }             // nguồn: ApprBy — InvF_MoveOrd.cs:35
    public DateTime? CancelledAt { get; set; }          // nguồn: CancelDTimeUTC — InvF_MoveOrd.cs:37
    public string? CancelledBy { get; set; }            // nguồn: CancelBy — InvF_MoveOrd.cs:39
    public FormStatus Status { get; set; } = FormStatus.Pending;  // nguồn: IF_MOStatus — InvF_MoveOrd.cs:41
    public string? Remark { get; set; }                 // nguồn: Remark (label trên form là "Nội dung xuất",
                                                        // Create.cshtml:127) — InvF_MoveOrd.cs:43
    public DateTime LogLuAt { get; set; } = DateTime.Now;   // nguồn: LogLUDTimeUTC — InvF_MoveOrd.cs:45
    public string? LogLuBy { get; set; }                // nguồn: LogLUBy — InvF_MoveOrd.cs:47

    /// <summary>Mã phiếu Chuyển kho (StockDoc) sinh khi duyệt, để tồn kho thật đổi theo.
    /// Cột KHÔNG có ở nguồn (nguồn ghi thẳng Inv_InventoryBalance) — khai báo ở mục 5.</summary>
    public string? TransferDocCode { get; set; }

    public Warehouse? OutWarehouse { get; set; }
    public Warehouse? InWarehouse { get; set; }
    public List<MoveOrdLine> Lines { get; set; } = [];

    /// <summary>Tổng số lượng điều chuyển. Nguồn: Create.cshtml:210 (span "Tổng:" id=spanTotalPrd).</summary>
    public decimal TotalQty => Lines.Sum(l => l.Qty);
    /// <summary>Tổng thành tiền sau thuế. Nguồn: cột lưới "Thành tiền sau thuế" — Create.cshtml:233.</summary>
    public decimal TotalValAfterVat => Lines.Sum(l => l.ValAfterVat);
}

/// <summary>
/// Dòng hàng hoá của phiếu điều chuyển.
/// Port 1:1 bảng <c>InvF_MoveOrdDtl</c>. Nguồn:
/// idn.Skycic.Inventory.Common/Models/InvF_MoveOrdDtl.cs:61-113,
/// Views/InvF_MoveOrd/Create.cshtml:219-234 (14 cột lưới),
/// Scripts/mine/InvF_MoveOrd.js:80-98 (các trường gửi lên: UPMove/VAT/ValMove/ValVAT/ValAfterVAT/Remark/UnitCode).
/// </summary>
public class MoveOrdLine : IOrgOwned
{
    public int Id { get; set; }
    public Guid OrgId { get; set; }                     // nguồn: OrgID — InvF_MoveOrdDtl.cs:73
    public int MoveOrdId { get; set; }                  // nguồn: IF_MONo — InvF_MoveOrdDtl.cs:63
    public int ProductId { get; set; }                  // nguồn: ProductCode — InvF_MoveOrdDtl.cs:69
    public string? OutLocation { get; set; }            // nguồn: InvCodeOut, Vị trí xuất — InvF_MoveOrdDtl.cs:65
    public string? InLocation { get; set; }             // nguồn: InvCodeIn, Vị trí nhập — InvF_MoveOrdDtl.cs:67
    public decimal Qty { get; set; }                    // nguồn: Qty — InvF_MoveOrdDtl.cs:75
    public decimal UpMove { get; set; }                 // nguồn: UPMove, Đơn giá (lưới: "Giá nhập trước VAT") — InvF_MoveOrdDtl.cs:77
    public decimal ValMove { get; set; }                // nguồn: ValMove, Thành tiền trước VAT — InvF_MoveOrdDtl.cs:79
    public decimal Vat { get; set; }                    // nguồn: VAT, Thuế suất (%) — InvF_MoveOrdDtl.cs:81
    public decimal ValVat { get; set; }                 // nguồn: ValVAT, Tiền VAT — InvF_MoveOrdDtl.cs:83
    public decimal ValAfterVat { get; set; }            // nguồn: ValAfterVAT, Thành tiền sau VAT — InvF_MoveOrdDtl.cs:85
    public string? UnitCode { get; set; }               // nguồn: UnitCode — InvF_MoveOrdDtl.cs:87
    public FormStatus StatusDtl { get; set; } = FormStatus.Pending;  // nguồn: IF_MOStatusDtl — InvF_MoveOrdDtl.cs:89
    public string? Remark { get; set; }                 // nguồn: Remark, Ghi chú — InvF_MoveOrdDtl.cs:91
    public DateTime LogLuAt { get; set; } = DateTime.Now;   // nguồn: LogLUDTimeUTC — InvF_MoveOrdDtl.cs:93
    public string? LogLuBy { get; set; }                // nguồn: LogLUBy — InvF_MoveOrdDtl.cs:95

    public MoveOrd MoveOrd { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>
/// Một dòng THẺ KHO (báo cáo, không phải bảng lưu).
/// Port 1:1 <c>Rpt_InvF_WarehouseCard</c>. Nguồn:
/// idn.Skycic.Inventory.Common/Models/Rpt_InvF_WarehouseCard.cs:9-101 (cột),
/// Views/Report/BindDataRpt_InvF_WarehouseCard.cshtml:33-69 (13 cột lưới),
/// SQL LIVE: idn.Skycic.Inventory.Biz/Form.cs:134971 Rpt_InvF_WarehouseCardX_New20200602
/// (bản _New20230106 / _New20230113 là MÃ CHẾT — chỗ gọi ở Form.cs:138525 bị comment).
/// </summary>
public record WarehouseCardRow(
    string DocNo,                // nguồn: DocNo, Số chứng từ — Rpt_InvF_WarehouseCard.cs:11
    string DateDoc,              // nguồn: DateDoc = Convert(char(10), ApprDTimeUTC) — Form.cs:135501
    DateTime? ApprDTime,         // nguồn: ApprDTimeUTC, Thời gian — Rpt_InvF_WarehouseCard.cs:57
    string InventoryAction,      // nguồn: InventoryAction — Rpt_InvF_WarehouseCard.cs:19
    string InventoryActionDesc,  // nguồn: CASE dịch nghĩa — Form.cs:135518-135530
    string InvCode,              // nguồn: InvCode, Kho tồn — Rpt_InvF_WarehouseCard.cs:13
    string InvName,              // nguồn: InvName, Tên kho tồn — Rpt_InvF_WarehouseCard.cs:15
    string ProductCode,          // nguồn: ProductCode — Rpt_InvF_WarehouseCard.cs:31
    string ProductCodeUser,      // nguồn: mp_ProductCodeUser — Rpt_InvF_WarehouseCard.cs:33
    string ProductName,          // nguồn: mp_ProductName — Rpt_InvF_WarehouseCard.cs:35
    string ListInvCode,          // nguồn: ListInvCode, List vị trí — Rpt_InvF_WarehouseCard.cs:21
    decimal QtyTrans,            // nguồn: CASE theo InventoryAction — Form.cs:135675-135688
    decimal TotalQtyInv,         // nguồn: TotalQtyInv, tồn luỹ kế — Rpt_InvF_WarehouseCard.cs:55
    string? InvFCFInCode01,      // nguồn: CASE khi action='IN' lấy InvFCFMixCode01 — Form.cs:135689-135694
    string? InvFCFInCode02,      // nguồn: InvFCFMixCode02 khi 'IN' — Form.cs:135695-135700
    string? InvFCFInCode03,      // nguồn: InvFCFMixCode03 khi 'IN' — Form.cs:135701-135706
    string? InvFCFOutCode01,     // nguồn: InvFCFMixCode01 khi 'OUT' — Form.cs:135749-135754
    string? InvFCFOutCode02,     // nguồn: InvFCFMixCode02 khi 'OUT' — Form.cs:135755-135760
    string? InvFCFOutCode03      // nguồn: InvFCFMixCode03 khi 'OUT' — Form.cs:135761-135766
);

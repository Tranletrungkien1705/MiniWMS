using MiniWMS.Models;

namespace MiniWMS.Services;

public static class Ui
{
    public static (string text, string css) DocTypeBadge(DocType t) => t switch
    {
        DocType.In => ("Nhập kho", "success"),
        DocType.Out => ("Xuất kho", "danger"),
        DocType.Transfer => ("Chuyển kho", "info"),
        _ => (t.ToString(), "secondary")
    };
    /// <summary>
    /// Nhãn trạng thái phiếu kiểm kê / phiếu điều chuyển.
    /// Port 1:1 hằng nguồn idn.Skycic.Inventory.Constants/Const.Main.BE.cs:418-424
    /// (PENDING / APPROVE / CANCEL / FINISH / ERROR). Chữ tiếng Việt theo thông điệp nguồn:
    /// "Duyệt phiếu kiểm kê thành công!" (InvFInvAuditController.cs:489),
    /// "Hủy phiếu kiểm kê thành công!" (InvFInvAuditController.cs:616).
    /// </summary>
    public static (string text, string css) FormStatusBadge(FormStatus s) => s switch
    {
        FormStatus.Pending => ("Chờ duyệt", "secondary"),
        FormStatus.Approve => ("Đã duyệt", "primary"),
        FormStatus.Finish => ("Đã chốt", "success"),
        FormStatus.Cancel => ("Đã hủy", "dark"),
        FormStatus.Error => ("Lỗi", "danger"),
        _ => (s.ToString(), "secondary")
    };

    public static (string text, string css) StatusBadge(DocStatus s) => s switch
    {
        DocStatus.Draft => ("Nháp", "secondary"),
        DocStatus.Posted => ("Đã ghi sổ", "success"),
        DocStatus.Cancelled => ("Đã hủy", "dark"),
        _ => (s.ToString(), "secondary")
    };
}

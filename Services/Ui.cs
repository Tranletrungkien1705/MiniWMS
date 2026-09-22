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
    public static (string text, string css) StatusBadge(DocStatus s) => s switch
    {
        DocStatus.Draft => ("Nháp", "secondary"),
        DocStatus.Posted => ("Đã ghi sổ", "success"),
        DocStatus.Cancelled => ("Đã hủy", "dark"),
        _ => (s.ToString(), "secondary")
    };
    public static (string text, string css) AuditStatusBadge(StockAuditStatus s) => s switch
    {
        StockAuditStatus.Draft => ("Đang kiểm kê", "warning text-dark"),
        StockAuditStatus.Finished => ("Đã cân bằng", "success"),
        StockAuditStatus.Cancelled => ("Đã hủy", "dark"),
        _ => (s.ToString(), "secondary")
    };
    public static (string text, string css) CardActionBadge(DocType t, string? refNo)
    {
        if (!string.IsNullOrWhiteSpace(refNo) && refNo.StartsWith("KK", StringComparison.OrdinalIgnoreCase))
        {
            return t == DocType.In ? ("Kiểm kê (Thừa)", "primary") : ("Kiểm kê (Thiếu)", "warning text-dark");
        }
        return t switch
        {
            DocType.In => ("Nhập kho", "success"),
            DocType.Out => ("Xuất kho", "danger"),
            DocType.Transfer => ("Chuyển kho", "info"),
            _ => (t.ToString(), "secondary")
        };
    }
}

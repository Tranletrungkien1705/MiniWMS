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
}

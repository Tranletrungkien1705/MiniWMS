using Microsoft.AspNetCore.Mvc;
using MiniWMS.Data;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 AccountController.cs — action LogOff (phiên/SSO).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\AccountController.cs:953-974.
/// Chuỗi: gate LogOff → KHÔNG gọi service/biz nào (excerpt dòng 29-30: "GATE goi service:" rỗng);
///        chỉ thao tác phiên ASP.NET + gọi hạ tầng SSO ngoài (InitSSO, BaseController.cs:883-908)
///        rồi redirect sang {InosBaseUrl}/account/signout/?returnUrl={ClientDomain} (dòng 968-971).
///
/// Phần port được (code thật của gate, không phụ thuộc hạ tầng nguồn):
///   - dòng 960-964: nếu có cookie "accesstoken" → ghi đè cookie cùng tên với Expires = Now-1 ngày (xoá);
///   - dòng 965-966: luôn ghi đè cookie "ASP.NET_SessionId" với Expires = Now-1 ngày (xoá).
///
/// GAP (không port được — hạ tầng đích không có, xem .parity-run\conflicts\7_conflict.md):
///   - dòng 955/967 Session.Clear(): đích không dùng Session (JWT Bearer + MiniSSO, Program.cs:31-40);
///   - dòng 956 InitSSO(): khởi tạo inos.common.Service.InosClientServiceBase với InosBaseUrl/SSOSecret/SolutionCode
///     từ appSettings nguồn — không tồn tại ở đích (grep "InosBaseUrl|ClientDomain" toàn repo = 0 dòng);
///   - dòng 959 FormsAuthentication.SignOut(): đích không dùng FormsAuthentication;
///   - dòng 968 GetLogoffUrl(): hàm của inos.common, không có mã nguồn, KHÔNG port (và nguồn tính rồi không dùng);
///   - dòng 969-972 Response.Redirect(urlSignOut, true): urlSignOut dựng từ appSettings["InosBaseUrl"] +
///     appSettings["ClientDomain"] — cả hai không có ở đích; KHÔNG tự chế giá trị thay thế (P3).
///
/// LỖI NGUỒN GIỮ NGUYÊN: dòng 968 tính `url = inos.common.Paths.GetLogoffUrl()` nhưng KHÔNG dùng (chỉ urlSignOut
/// được redirect ở dòng 971) — port không dựng url, đúng như nguồn không dùng.
///
/// Hình dạng phản hồi: nguồn KHÔNG trả JSON (redirect 302 + Response.End). Đích trả 200 + RedirectUrl = null
/// (cùng mẫu "không có trang đích → RedirectUrl null" của Mst_InventoryLevelTypeControllerPort.cs:177/243)
/// để client gọi được mà không cần appSettings nguồn — đây là phần CẦN CLAUDE CHỐT (conflict #7).
/// </summary>
[ApiController]
public class AccountControllerPort : ControllerBase
{
    /// <summary>
    /// Nguồn AccountController.cs:953-974 — LogOff() không có tham số đầu vào, không body.
    /// POST để đồng nhất khuôn port (luật §2.5b) và tránh cache của GET.
    /// </summary>
    [HttpPost("api/account/logoff")]
    public IActionResult LogOff()
    {
        // Nguồn 960-964: chỉ xoá cookie "accesstoken" KHI request có cookie đó (Request.Cookies["accesstoken"] != null).
        if (Request.Cookies.ContainsKey("accesstoken"))
        {
            // Nguồn 962: new HttpCookie("accesstoken") { Expires = DateTime.Now.AddDays(-1) } — không set giá trị.
            Response.Cookies.Append("accesstoken", "", new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(-1),
                Path = "/",
            });
        }

        // Nguồn 965-966: LUÔN ghi cookie "ASP.NET_SessionId" hết hạn (không phụ thuộc request có cookie hay không).
        Response.Cookies.Append("ASP.NET_SessionId", "", new CookieOptions
        {
            Expires = DateTimeOffset.Now.AddDays(-1),
            Path = "/",
        });

        // GAP: Session.Clear / InitSSO / FormsAuthentication.SignOut / redirect urlSignOut — xem chú thích đầu file.
        // Nguồn 971 redirect sang urlSignOut dựng từ appSettings không có ở đích → trả null, ghi GAP + conflict #7.
        return Ok(new { Success = true, RedirectUrl = (string?)null });
    }

    /// <summary>
    /// port 1:1 AccountController.cs:977-983 — action SignOut (phiên/FormsAuth, không body, không tham số).
    /// Nguồn (nguyên văn):
    ///   977| public ActionResult SignOut()
    ///   978| {
    ///   979|     Session.Clear();
    ///   980|
    ///   981|     FormsAuthentication.SignOut();
    ///   982|     return null;
    ///   983| }
    ///
    /// Toàn bộ thân hàm là thao tác hạ tầng phiên:
    ///   - dòng 979 Session.Clear(): đích KHÔNG dùng Session (JWT Bearer + MiniSSO) → GAP;
    ///   - dòng 981 FormsAuthentication.SignOut(): đích KHÔNG dùng FormsAuthentication → GAP;
    ///   - dòng 982 `return null`: ActionResult null = phản hồi 200 rỗng, không JSON, không mã lỗi.
    ///
    /// Không có service/biz, không bảng, không transaction, không mã lỗi, không gọi dịch vụ ngoài
    /// (excerpt dòng 14-16: "GATE goi service:" rỗng). Đây là hạng mục GAP thuần hạ tầng, cùng dạng LogOff (#7).
    ///
    /// Hình dạng phản hồi: để client dùng được, trả 200 + { Success = true } — KHÁC nguồn (nguồn 200 rỗng).
    /// Đây là phần CẦN CLAUDE CHỐT (đề xuất: giữ 200 rỗng 1:1 với `return null` của nguồn, hoặc 200 + Success=true
    /// như LogOff để đồng nhất controller). Ghi GAP + CONFLICT trong báo cáo hạng mục #8.
    /// </summary>
    [HttpPost("api/account/signout")]
    public IActionResult SignOut()
    {
        // GAP: Session.Clear (dòng 979) + FormsAuthentication.SignOut (dòng 981) — hạ tầng nguồn không tồn tại ở đích.
        // Nguồn dòng 982 `return null` → 200 rỗng; đích trả Success=true cho client dùng được (xem CONFLICT ở báo cáo).
        return Ok(new { Success = true });
    }
}

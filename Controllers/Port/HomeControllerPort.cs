using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 HomeController.cs — HomeController/Index (trang chủ sau đăng nhập).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\HomeController.cs:20-330
///   → WA_OS_Inos_Org_GetMyOrgList (OSInosController.cs:168-239 → OS.Inos.cs:2635-2725)
///   → WA_MstSv_Sys_User_Login (MasterServerController.cs:20-91 → MasterServer.cs:765-856)
///   → WA_Sys_User_GetForCurrentUser (SysUserController.cs:273-344 → System.cs:7659-7750).
///
/// port 1:1 HomeController.cs:332-337 — About() (hạng mục #19): action KHÔNG có tham số, KHÔNG gọi service,
/// KHÔNG đọc/ghi bảng nào; chỉ gán `ViewBag.Message = "Your application description page."` rồi `return View()`.
/// Đích: POST /api/home/about trả đúng chuỗi đó (PortResult không dùng vì nguồn không có nhánh lỗi).
///
/// GAP (xem .parity-run\conflicts\18_conflict.md): toàn bộ dữ liệu của action đến từ DỊCH VỤ NGOÀI
/// (inos SSO `AccountService.GetCurrentUser`, `OS_Inos_OrgService.Instance.WA_OS_Inos_Org_GetMyOrgList`,
/// `MasterServerService.Instance.WA_MstSv_Sys_User_Login`, `Sys_UserService.Instance.WA_Sys_User_GetForCurrentUser`,
/// `ReturnAPIMasterServer*`). Đích KHÔNG có các dịch vụ này ⇒ KHÔNG mô phỏng, KHÔNG bịa dữ liệu:
/// chỉ port phần đọc DB nội bộ mà nguồn thật sự làm (Mst_Sys_Config theo NetworkID, HomeController.cs:180-198)
/// và trả GAP cho các nhánh còn lại.
/// </summary>
[ApiController]
[Route("api/home")]
public class HomeControllerPort(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Body của Index. Nguồn KHÔNG có tham số action; các giá trị dưới đây là dữ liệu nguồn lấy từ
    /// phiên/SSO (GAP: đích không có phiên) — thiếu thì để null, KHÔNG tự đặt mặc định.
    /// </summary>
    public class IndexReq
    {
        /// <summary>Nguồn: `User.Identity.IsAuthenticated` (HomeController.cs:22).</summary>
        public bool? IsAuthenticated { get; set; }
        /// <summary>Nguồn: `Session["AccessToken"]` / cookie "accesstoken" (HomeController.cs:28-49).</summary>
        public string? AccessToken { get; set; }
        /// <summary>Nguồn: `inosuser.Email` (HomeController.cs:128,135).</summary>
        public string? UserCode { get; set; }
        /// <summary>Nguồn: `networkID` suy ra từ danh sách OS_Inos_Org (HomeController.cs:98).</summary>
        public string? NetworkID { get; set; }
        /// <summary>Nguồn: `orgID` suy ra từ danh sách OS_Inos_Org (HomeController.cs:99).</summary>
        public string? OrgID { get; set; }
    }

    /// <summary>
    /// port 1:1 HomeController.cs:20-330 — Index().
    /// Luồng nguồn: chưa đăng nhập → SignOut + Redirect(login) (dòng 324-329);
    ///   có token → InitSSO + GetCurrentUser (dòng 51-56) → WA_OS_Inos_Org_GetMyOrgList (dòng 78);
    ///   danh sách org rỗng → `return View(listOS_Inos_Org)` (dòng 315);
    ///   đúng 1 org → WA_MstSv_Sys_User_Login (dòng 117) → WA_Sys_User_GetForCurrentUser (dòng 148)
    ///   → Mst_NNT + Mst_Sys_Config (dòng 159-198) + 3 API master server (dòng 200-262)
    ///   → Session["networkid"] + Redirect(Dashboard/Index) (dòng 308-310).
    /// </summary>
    [HttpPost("index")]
    public async Task<IActionResult> Index([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] IndexReq? req)
    {
        // Nguồn 22-23 + 324-329: chưa đăng nhập → FormsAuthentication.SignOut() + RedirectActionToLogin().
        if (req?.IsAuthenticated != true)
            return Ok(new
            {
                Success = true,
                RedirectUrl = (string?)null,
                SignOut = true,
                Data = (object?)null,
                Gap = new[] { "RedirectActionToLogin (HomeController.cs:320,327) — GAP: đích không có trang đăng nhập nguồn" }
            });

        // Nguồn 51-56: InitSSO() + `new inos.common.Service.AccountService(Session).GetCurrentUser()`.
        // GAP: dịch vụ SSO inos không có ở đích → không mô phỏng.
        if (string.IsNullOrEmpty(req.AccessToken))
            return Ok(new
            {
                Success = true,
                RedirectUrl = (string?)null,
                SignOut = true,
                Data = (object?)null,
                Gap = new[] { "InitSSO/AccountService.GetCurrentUser (HomeController.cs:53-56) — GAP: dịch vụ ngoài" }
            });

        // Nguồn 78-99: WA_OS_Inos_Org_GetMyOrgList → danh sách org; rỗng → View(listOS_Inos_Org) (dòng 315).
        // GAP: OS_Inos_OrgService.Instance (dịch vụ ngoài) không có ở đích.
        if (string.IsNullOrEmpty(req.NetworkID) || string.IsNullOrEmpty(req.OrgID))
            return Ok(new
            {
                Success = true,
                RedirectUrl = (string?)null,
                SignOut = false,
                Data = (object?)null,
                Gap = new[]
                {
                    "WA_OS_Inos_Org_GetMyOrgList (HomeController.cs:78) — GAP: dịch vụ ngoài; danh sách org rỗng → View(listOS_Inos_Org) (dòng 315)"
                }
            });

        // Nguồn 117-119: WA_MstSv_Sys_User_Login → c_K_DT_Sys.Lst_c_K_DT_SysInfo[0].Remark = baseServiceAddress.
        // GAP: MasterServerService.Instance (dịch vụ ngoài) không có ở đích.
        if (string.IsNullOrEmpty(req.UserCode))
            return Ok(new
            {
                Success = true,
                RedirectUrl = (string?)null,
                SignOut = false,
                Data = (object?)null,
                Gap = new[]
                {
                    "WA_MstSv_Sys_User_Login (HomeController.cs:117) — GAP: dịch vụ ngoài; không có c_K_DT_Sys → không vào nhánh dòng 118-307"
                }
            });

        // Nguồn 180-198: strWhereClause_Mst_Sys_Config (dòng 373-384) = where Mst_Sys_Config.NetworkID = networkID ("=")
        // → WA_Mst_Sys_Config_Get (Rt_Cols_Mst_Sys_Config = "*", Ft_RecordCount = Ft_RecordCount).
        // Đích: đọc bảng nội bộ Mst_Sys_Config (entity Models/Port/Mst_Sys_Config.cs).
        var networkId = req.NetworkID.Trim();
        var configs = await db.Set<Mst_Sys_Config>()
            .Where(x => EF.Functions.Like((x.NetworkID ?? "").ToLower(), PortQuery.Equal(networkId), PortQuery.Esc))
            .OrderBy(x => x.OrgID).ThenBy(x => x.SysConfigID)
            .ToListAsync();

        // Nguồn 308-310: Session["networkid"] = networkID; Redirect(Url.ActionLocalized("Index", "Dashboard")).
        return Ok(new
        {
            Success = true,
            RedirectUrl = "/Dashboard/Index",
            SignOut = false,
            Data = new
            {
                NetworkID = networkId,
                OrgID = req.OrgID.Trim(),
                Mst_Sys_Config = configs
            },
            Gap = new[]
            {
                "WA_Sys_User_GetForCurrentUser (HomeController.cs:148) — GAP: dịch vụ ngoài",
                "Mst_NNT theo MST (HomeController.cs:159-177) — GAP: dịch vụ ngoài",
                "ReturnAPIMasterServer* Veloca/DMS/Product_Customer_Center (HomeController.cs:200-262) — GAP: dịch vụ ngoài",
                "UserState/Session (HomeController.cs:293-308) — GAP: đích không có phiên"
            }
        });
    }

    /// <summary>
    /// port 1:1 HomeController.cs:332-337 — About().
    /// Nguồn: `ViewBag.Message = "Your application description page."; return View();` — không tham số,
    /// không guard, không mã lỗi, không bảng, không dịch vụ ngoài.
    /// </summary>
    [HttpPost("about")]
    public IActionResult About()
    {
        // Nguồn dòng 334: chuỗi chép nguyên văn.
        return Ok(new { Success = true, Message = "Your application description page." });
    }

    /// <summary>
    /// port 1:1 HomeController.cs:339-344 — Contact() (hạng mục #20).
    /// Nguồn: `ViewBag.Message = "Your contact page."; return View();` — KHÔNG tham số, KHÔNG guard,
    /// KHÔNG mã lỗi, KHÔNG đọc/ghi bảng, KHÔNG gọi dịch vụ ngoài (excerpt dòng 13-15: danh sách service rỗng).
    /// </summary>
    [HttpPost("contact")]
    public IActionResult Contact()
    {
        // Nguồn dòng 341: chuỗi chép nguyên văn.
        return Ok(new { Success = true, Message = "Your contact page." });
    }

    /// <summary>
    /// port 1:1 HomeController.cs:388-391 — Ping() (hạng mục #21).
    /// Nguồn: `return Json(true, JsonRequestBehavior.AllowGet);` — KHÔNG tham số, KHÔNG guard,
    /// KHÔNG mã lỗi, KHÔNG đọc/ghi bảng, KHÔNG gọi dịch vụ ngoài (excerpt dòng 11-18: danh sách service rỗng).
    /// Đích: POST /api/home/ping trả đúng giá trị boolean `true` của nguồn.
    /// </summary>
    [HttpPost("ping")]
    public IActionResult Ping()
    {
        // Nguồn dòng 390: Json(true) — trả nguyên giá trị true, không bọc thêm trường nào.
        return Ok(true);
    }

    /// <summary>
    /// Body của Notification. Nguồn KHÔNG có tham số action; các giá trị dưới đây là dữ liệu nguồn lấy từ
    /// phiên (`UserState.Mst_NNT.NetworkID`, HomeController.cs:426-429) — thiếu thì để null, KHÔNG tự đặt mặc định.
    /// </summary>
    public class NotificationReq
    {
        /// <summary>Nguồn: `userState != null` (HomeController.cs:419).</summary>
        public bool? HasUserState { get; set; }
        /// <summary>Nguồn: `userState.Mst_NNT.NetworkID` (HomeController.cs:426-429).</summary>
        public string? NetworkID { get; set; }
    }

    /// <summary>
    /// port 1:1 HomeController.cs:394-504 — Notification().
    /// Luồng nguồn: userState null → bỏ qua khối lấy thông báo (dòng 419-445); ngược lại InitSSO (dòng 422)
    ///   → `new inos.common.Service.NotificationService(Session).GetNotifications(iNetworkId, "",
    ///   NotificationStatuses.ALL, 0, PageSizeConfig)` (dòng 430-431) → lấy phần tử UNREAD ĐẦU TIÊN
    ///   (dòng 436-440) → dựng `objNotificationResult` (dòng 465-501) → `Json(..., AllowGet)` (dòng 502).
    /// GAP (xem .parity-run\conflicts\22_conflict.md): toàn bộ danh sách thông báo đến từ DỊCH VỤ NGOÀI
    /// `inos.common.Service.NotificationService` (SSO/iNOS) — đích KHÔNG có ⇒ KHÔNG mô phỏng, KHÔNG bịa dữ liệu.
    /// Nguồn KHÔNG đọc/ghi bảng nội bộ nào (Mst_NNT chỉ đọc qua UserState của phiên).
    /// </summary>
    [HttpPost("notification")]
    public IActionResult Notification([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] NotificationReq? req)
    {
        // Nguồn 412-416: giá trị khởi tạo trước try.
        var success = false;
        var errorCode = "";
        var errorDetail = "";
        var urlLogin = "";
        var serverBaseAddress = "";

        // Nguồn 399-407: objInosNotificationResult khởi tạo rỗng (List rỗng, mọi số = 0).
        var pageIndex = 0;
        var pageSize = 0;
        var unreadCount = 0;
        var pageCount = 0;
        var itemCount = 0;
        var list = new List<object>();
        // Nguồn 408: listInosNotification_UnRead khởi tạo rỗng.
        var listInosNotification_UnRead = new List<object>();

        try
        {
            // Nguồn 419-445: chỉ chạy khi có userState.
            if (req?.HasUserState == true)
            {
                // Nguồn 422: InitSSO() — cấu hình client SSO (BaseController.cs:883-908).
                // GAP: dịch vụ SSO inos không có ở đích → không mô phỏng.

                // Nguồn 425-429: iNetworkId = Convert.ToInt64(userState.Mst_NNT.NetworkID) khi khác rỗng.
                // GAP: UserState/Mst_NNT của phiên không có ở đích; NetworkID nhận qua body nhưng chỉ dùng làm
                // tham số của dịch vụ ngoài (dòng 431) nên ở đích không có tác dụng lọc.

                // Nguồn 430-431: notificationService.GetNotifications(iNetworkId, "", NotificationStatuses.ALL, 0, PageSizeConfig).
                // GAP: inos.common.Service.NotificationService (dịch vụ ngoài) không có ở đích ⇒ objInosNotificationResult
                // giữ nguyên giá trị khởi tạo rỗng; KHÔNG tự dựng danh sách thông báo.
            }
            // Nguồn 446: Success = true (đặt sau khối if, kể cả khi userState null).
            success = true;
        }
        catch (Exception ex)
        {
            // Nguồn 448-462: ServiceException → ErrorCode = cex.ErrorCode.ToLower() + UrlLogin = <ClientDomain>/Account/Login;
            // ngoại lệ khác → ErrorDetail = "ServerBaseAddress: " + serverBaseAddress + "<br />" + ex.Message + ex.StackTrace.
            // GAP: mbiz.core ServiceException + AppSettings ClientDomain không có ở đích ⇒ nhánh ngoại lệ khác.
            errorDetail = "ServerBaseAddress: " + serverBaseAddress + "<br />" + ex.Message + ex.StackTrace;
        }

        // Nguồn 465-501: hình dạng objNotificationResult (Json PascalCase, AllowGet).
        return Ok(new
        {
            Success = success,
            ErrorDetail = errorDetail,
            ErrorCode = errorCode,
            UrlLogin = urlLogin,
            InosNotificationResult = new
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                UnreadCount = unreadCount,
                PageCount = pageCount,
                ItemCount = itemCount,
                List = list,
            },
            ListInosNotification_UnRead = listInosNotification_UnRead,
            // Nguồn 500: DateTime.Now.ToString(Nonsense.DATE_TIME_FULL_DB_FORMAT) = "yyyy-MM-dd HH:mm:ss.fff".
            DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            // GAP: danh sách thông báo lấy từ dịch vụ ngoài inos NotificationService (HomeController.cs:430-431).
            Gap = new[] { "NotificationService.GetNotifications (HomeController.cs:430-431) — GAP: dịch vụ ngoài" }
        });
    }
}

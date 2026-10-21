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
}

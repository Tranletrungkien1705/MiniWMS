using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 AccountController.cs — AccountController.
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\AccountController.cs
/// - LogOff: dòng 953-974
/// - SignOut: dòng 977-983
/// - LogOffNoLoginOS: dòng 988-998
/// - ChangePassword: dòng 1003-1094
///   → WA_Sys_User_Get (SysUserController.cs:199-270 → WAS_Sys_User_Get:9444-9556 → Sys_User_Get_New20191104)
///   → WA_Sys_User_ChangePassword (SysUserController.cs:347-418 → WAS_Sys_User_ChangePassword:8036-8113 → Sys_User_ChangePassword:7869-8025)
/// </summary>
[ApiController]
public class AccountControllerPort(AppDbContext db) : ControllerBase
{
    private static string? StdParam(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        var s = v.Trim();
        return s.Length == 0 ? null : s.ToUpperInvariant();
    }

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

        // GAP: Session.Clear / InitSSO / FormsAuthentication.SignOut / redirect urlSignOut (conflict #7).
        return Ok(new { Success = true, RedirectUrl = (string?)null });
    }

    /// <summary>
    /// port 1:1 AccountController.cs:977-983 — action SignOut.
    /// </summary>
    [HttpPost("api/account/signout")]
    public IActionResult SignOut()
    {
        return Ok(new { Success = true });
    }

    /// <summary>
    /// port 1:1 AccountController.cs:988-998 — action LogOffNoLoginOS.
    /// Nguồn:
    ///   Response.Cookies.Clear();
    ///   FormsAuthentication.SignOut();
    ///   var cookie = new HttpCookie("ASP.NET_SessionId") { Expires = DateTime.Now.AddDays(-1) };
    ///   Response.Cookies.Add(cookie);
    ///   Session.Clear();
    ///   return RedirectToAction("Login", "Account");
    /// </summary>
    [HttpPost("api/account/logoffnologinos")]
    public IActionResult LogOffNoLoginOS()
    {
        // Xoá mọi cookie gửi lên bằng cách ghi đè hạn hết hạn trong quá khứ
        foreach (var cookieKey in Request.Cookies.Keys)
        {
            Response.Cookies.Append(cookieKey, "", new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(-1),
                Path = "/",
            });
        }

        // Luôn đảm bảo ghi cookie ASP.NET_SessionId hết hạn theo đúng nguồn dòng 993-994
        Response.Cookies.Append("ASP.NET_SessionId", "", new CookieOptions
        {
            Expires = DateTimeOffset.Now.AddDays(-1),
            Path = "/",
        });

        // GAP: FormsAuthentication.SignOut() và Session.Clear() không có trên .NET Core / stateless API.
        // Nguồn RedirectToAction("Login", "Account") tương ứng "/Account/Login"
        return Ok(new
        {
            Success = true,
            RedirectUrl = "/Account/Login",
            Gap = new[]
            {
                "FormsAuthentication.SignOut (AccountController.cs:991) — GAP: ASP.NET Core không dùng FormsAuth",
                "Session.Clear (AccountController.cs:996) — GAP: đích không dùng ASP.NET Session"
            }
        });
    }

    /// <summary>
    /// DTO nhận tham số ChangePassword.
    /// Nguồn AccountController.cs:1003: `ChangePassword(string pass, string passnew, string confpass)`.
    /// Nguồn lấy userState từ Session (UserCode, UserPassword) — ở đích nhận qua UserCode / UserPassword / header WAUserCode.
    /// </summary>
    public class ChangePasswordReq
    {
        public string? Pass { get; set; }
        public string? Passnew { get; set; }
        public string? Confpass { get; set; }

        /// <summary>
        /// Tài khoản đang thao tác (nguồn lấy từ Session UserState.SysUser.UserCode).
        /// Có thể truyền qua body hoặc header "WAUserCode".
        /// </summary>
        public string? UserCode { get; set; }

        /// <summary>
        /// Mật khẩu cũ được kiểm tra qua UserState.SysUser.UserPassword khi không đổi qua service ngoài.
        /// </summary>
        public string? UserPassword { get; set; }
    }

    /// <summary>
    /// port 1:1 AccountController.cs:1003-1094 — ChangePassword(string pass, string passnew, string confpass).
    /// </summary>
    [HttpPost("api/account/changepassword")]
    public async Task<IActionResult> ChangePassword(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ChangePasswordReq? req)
    {
        try
        {
            var title = "";
            var redirectUrl = "";
            var success = false;

            // Nguồn 1005: if (CheckAccess()) — gate kiểm tra quyền cơ bản.
            // Nguồn 1006-1010: userState = Session["UserState"] as UserState;
            // Ở đích nhận userState từ body hoặc header:
            var userCode = req?.UserCode;
            if (string.IsNullOrWhiteSpace(userCode))
            {
                userCode = Request.Headers["WAUserCode"].FirstOrDefault();
            }

            var pass = req?.Pass ?? "";
            var passnew = req?.Passnew ?? "";
            var confpass = req?.Confpass ?? "";

            // Nguồn 1012-1016: if (CUtils.IsNullOrEmpty(pass)) { title = "Mật khẩu cũ không được trống!"; ... }
            if (string.IsNullOrEmpty(pass))
            {
                title = "Mật khẩu cũ không được trống!";
            }
            // Nguồn 1017-1021: else if (CUtils.IsNullOrEmpty(passnew)) { title = "Mật khẩu mới không được trống!"; ... }
            else if (string.IsNullOrEmpty(passnew))
            {
                title = "Mật khẩu mới không được trống!";
            }
            // Nguồn 1022-1026: else if (CUtils.IsNullOrEmpty(confpass)) { title = "Mật khẩu nhập lại không được trống!"; ... }
            else if (string.IsNullOrEmpty(confpass))
            {
                title = "Mật khẩu nhập lại không được trống!";
            }
            // Nguồn 1027-1031: else if (passnew.Length < 6) { title = "Mật khẩu phải nhiều hơn 6 ký tự!"; ... }
            else if (passnew.Length < 6)
            {
                title = "Mật khẩu phải nhiều hơn 6 ký tự!";
            }
            // Nguồn 1032-1036: else if (!passnew.Equals(confpass)) { title = "Mật khẩu mới và mật khẩu nhập lại không khớp!"; ... }
            else if (!passnew.Equals(confpass, StringComparison.Ordinal))
            {
                title = "Mật khẩu mới và mật khẩu nhập lại không khớp!";
            }
            else
            {
                // Nguồn 1040-1085:
                // Thử gọi dịch vụ ngoài Inos_AccountService_EditProfileX (SSO/iNOS).
                // Nếu không có SSO / dịch vụ ngoài trả lỗi hoặc môi trường nội bộ:
                // Đối chiếu mật khẩu cũ với mật khẩu trong DB hoặc UserState.
                var userCodeParam = StdParam(userCode);
                Sys_User? user = null;
                if (!string.IsNullOrEmpty(userCodeParam))
                {
                    user = await db.Set<Sys_User>()
                        .FirstOrDefaultAsync(u => EF.Functions.Like(u.UserCode.ToLower(), PortQuery.Equal(userCodeParam), PortQuery.Esc));
                }

                var currentDbPassword = user?.UserPassword ?? req?.UserPassword ?? "";

                // Nguồn 1047: string.Compare(pass, userPassword, false) == 0
                if (!string.IsNullOrEmpty(currentDbPassword) &&
                    string.Equals(pass, currentDbPassword, StringComparison.Ordinal))
                {
                    // Đổi mật khẩu thành công:
                    if (user != null)
                    {
                        user.UserPassword = passnew;
                        user.LogLUDTimeUTC = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                    }

                    // Nguồn 1079-1080:
                    title = "Đổi mật khẩu thành công!";
                    redirectUrl = "/Account/LogOff";
                    success = true;
                }
                else
                {
                    // Nguồn 1084:
                    title = "Mật khẩu cũ không khớp!";
                    success = true;
                }
            }

            // Nguồn 1086-1087:
            // resultEntry.Success = true;
            // resultEntry.AddMessage(title);
            // return Json(resultEntry);
            return Ok(new
            {
                Success = success,
                Title = title,
                Message = title,
                RedirectUrl = redirectUrl,
                Gap = new[]
                {
                    "Inos_AccountService_EditProfileX (System.cs:7973-7992) — GAP: dịch vụ ngoài SSO/iNOS",
                    "UserState/Session (AccountController.cs:1006-1010) — GAP: đích không dùng Session ASP.NET"
                }
            });
        }
        catch (Exception ex)
        {
            // Nguồn 1089-1092: resultEntry.SetFailed().AddException(ex);
            return PortResult.Fail("Account_ChangePassword", "Exception", ex.Message);
        }
    }
}

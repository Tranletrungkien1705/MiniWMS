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
/// - ChangePassword: dòng 1003-1094
///   → WA_Sys_User_Get (SysUserController.cs:199-270 → WAS_Sys_User_Get:9444-9556 → Sys_User_Get_New20191104)
///   → WA_Sys_User_ChangePassword (SysUserController.cs:347-418 → WAS_Sys_User_ChangePassword:8036-8113 → Sys_User_ChangePassword:7869-8025)
/// </summary>
[ApiController]
public class AccountControllerPort(AppDbContext db) : ControllerBase
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
        /// Mật khẩu trong phiên hiện tại (nguồn: UserState.SysUser.UserPassword).
        /// Nếu không truyền, hệ thống sẽ đọc từ DB theo UserCode để so sánh mật khẩu cũ.
        /// </summary>
        public string? UserPassword { get; set; }
    }

    /// <summary>
    /// port 1:1 AccountController.cs:1003-1094 — ChangePassword(string pass, string passnew, string confpass).
    /// </summary>
    [HttpPost("api/account/changepassword")]
    public async Task<IActionResult> ChangePassword([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ChangePasswordReq? req)
    {
        // Lấy UserCode từ header WAUserCode hoặc body
        var userCode = Request.Headers["WAUserCode"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userCode))
        {
            userCode = req?.UserCode;
        }
        userCode = string.IsNullOrWhiteSpace(userCode) ? null : userCode.Trim();

        var pass = string.IsNullOrWhiteSpace(req?.Pass) ? null : req.Pass.Trim();
        var passnew = string.IsNullOrWhiteSpace(req?.Passnew) ? null : req.Passnew.Trim();
        var confpass = string.IsNullOrWhiteSpace(req?.Confpass) ? null : req.Confpass.Trim();

        var title = "";
        var success = false;
        string? redirectUrl = null;

        try
        {
            // Nguồn 1009-1010: waUserCode = userState.SysUser.UserCode; waUserPassword = userState.SysUser.UserPassword;
            // Ở đích: nếu không có session, đọc user từ DB
            Sys_User? user = null;
            if (!string.IsNullOrEmpty(userCode))
            {
                user = await db.Set<Sys_User>()
                    .FirstOrDefaultAsync(u => EF.Functions.Like(u.UserCode.ToLower(), PortQuery.Equal(userCode.ToLower()), PortQuery.Esc));
            }

            var waUserPassword = req?.UserPassword ?? user?.UserPassword;

            // Nguồn 1014-1015: pass = CUtils.StrValueOrNull(pass); pass = CUtils.GetEncodedHash(pass);
            var passHash = Sys_User.GetEncodedHash(pass);

            // Nguồn 1016: if (pass.Equals(waUserPassword))
            if (!string.IsNullOrEmpty(passHash) && passHash.Equals(waUserPassword))
            {
                // Nguồn 1018: if (!CUtils.IsNullOrEmpty(passnew))
                if (!string.IsNullOrEmpty(passnew))
                {
                    // Nguồn 1021: if (!CUtils.IsNullOrEmpty(confpass))
                    if (!string.IsNullOrEmpty(confpass))
                    {
                        // Nguồn 1024: if (passnew.Equals(confpass))
                        if (passnew.Equals(confpass))
                        {
                            // Nguồn 1050: WA_Sys_User_Get check user tồn tại
                            if (user != null)
                            {
                                // Nguồn 1052: objRT_Sys_UserCur.Lst_Sys_User[0].UserPasswordNew = CUtils.GetEncodedHash(passnew);
                                var passNewHash = Sys_User.GetEncodedHash(passnew);

                                // Nguồn biz WAS_Sys_User_ChangePassword:7964-7965:
                                // dt_Sys_User.Rows[0]["UserPassword"] = TUtils.CUtils.GetEncodedHash(strUserPasswordNew);
                                // SaveData("Sys_User", dt_Sys_User, alColumnEffective.ToArray());
                                user.UserPassword = passNewHash;
                                user.LogLUDTimeUTC = DateTime.UtcNow;
                                user.LogLUBy = userCode ?? "port";
                                await db.SaveChangesAsync();

                                // Nguồn 1059-1060:
                                title = "Thay đổi mật khẩu thành công!";
                                redirectUrl = "/Account/LogOff";
                                success = true;
                            }
                            else
                            {
                                // Nguồn 1064: title = "Mã người dùng '" + waUserCode + "' không có trong hệ thống!";
                                title = "Mã người dùng '" + (userCode ?? "") + "' không có trong hệ thống!";
                                success = true;
                            }
                        }
                        else
                        {
                            // Nguồn 1069:
                            title = "Nhập lại mật khẩu mới không đúng, Vui lòng nhập lại!";
                            success = true;
                        }
                    }
                    else
                    {
                        // Nguồn 1074:
                        title = "Nhập lại khẩu mới trống!";
                        success = true;
                    }
                }
                else
                {
                    // Nguồn 1079:
                    title = "Mật khẩu mới trống!";
                    success = true;
                }
            }
            else
            {
                // Nguồn 1084:
                title = "Mật khẩu cũ không khớp!";
                success = true;
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

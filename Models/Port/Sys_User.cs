using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Sys_User.cs — Sys_User (người dùng hệ thống).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Common\Models\Sys_User.cs:9-84
/// FK tham chiếu: REFERENCES Sys_User (UserCode).
/// </summary>
public class Sys_User
{
    public string UserCode { get; set; } = "";
    public string? NetworkID { get; set; }
    public string? UserName { get; set; }
    public string? UserPassword { get; set; }
    public string? PhoneNo { get; set; }
    public string? EMail { get; set; }
    public string? OrganCode { get; set; }
    public string? DepartmentCode { get; set; }
    public string? Position { get; set; }
    public string? VerificationCode { get; set; }
    public string? Avatar { get; set; }
    public string? UUID { get; set; }
    public string? FlagDLAdmin { get; set; }
    public string? FlagSysAdmin { get; set; }
    public string? FlagNNTAdmin { get; set; }
    public string? OrgID { get; set; }
    public string? CustomerCodeSys { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public string? FlagActive { get; set; } = "1";
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
    public string? ACId { get; set; }
    public string? ACAvatar { get; set; }
    public string? ACEmail { get; set; }

    /// <summary>
    /// Hàm băm mật khẩu 1:1 của nguồn idn.Skycic.Inventory.Utils\Utils.cs:1672-1678
    /// Base64(MD5(password + "idocNet")) bỏ 2 ký tự cuối (==).
    /// </summary>
    public static string GetEncodedHash(string? password)
    {
        if (password == null) return "";
        using var md5 = MD5.Create();
        var digest = md5.ComputeHash(Encoding.UTF8.GetBytes(password + "idocNet"));
        var base64digest = Convert.ToBase64String(digest, 0, digest.Length);
        return base64digest.Length >= 2 ? base64digest[..^2] : base64digest;
    }
}

public class Sys_UserConfiguration : IEntityTypeConfiguration<Sys_User>
{
    public void Configure(EntityTypeBuilder<Sys_User> b)
    {
        b.ToTable("Sys_User");
        b.HasKey(x => x.UserCode);
        b.Property(x => x.UserCode).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.UserName).HasMaxLength(255);
        b.Property(x => x.UserPassword).HasMaxLength(255);
        b.Property(x => x.PhoneNo).HasMaxLength(50);
        b.Property(x => x.EMail).HasMaxLength(255);
        b.Property(x => x.OrganCode).HasMaxLength(50);
        b.Property(x => x.DepartmentCode).HasMaxLength(50);
        b.Property(x => x.Position).HasMaxLength(50);
        b.Property(x => x.VerificationCode).HasMaxLength(50);
        b.Property(x => x.Avatar).HasMaxLength(500);
        b.Property(x => x.UUID).HasMaxLength(100);
        b.Property(x => x.FlagDLAdmin).HasMaxLength(10);
        b.Property(x => x.FlagSysAdmin).HasMaxLength(10);
        b.Property(x => x.FlagNNTAdmin).HasMaxLength(10);
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.CustomerCodeSys).HasMaxLength(50);
        b.Property(x => x.CustomerCode).HasMaxLength(50);
        b.Property(x => x.CustomerName).HasMaxLength(255);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
        b.Property(x => x.ACId).HasMaxLength(100);
        b.Property(x => x.ACAvatar).HasMaxLength(500);
        b.Property(x => x.ACEmail).HasMaxLength(255);
    }
}

/// <summary>Seed demo Sys_User (Order 16 — danh mục người dùng, sau Mst_Unit 15).</summary>
public class Sys_UserSeeder : IPortSeeder
{
    public int Order => 16;

    public async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<Sys_User>().AnyAsync()) return;
        db.Set<Sys_User>().AddRange(
            new Sys_User
            {
                UserCode = "ADMIN",
                UserName = "Administrator",
                UserPassword = Sys_User.GetEncodedHash("123456"),
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            },
            new Sys_User
            {
                UserCode = "USER01",
                UserName = "Nguyen Van A",
                UserPassword = Sys_User.GetEncodedHash("Password123!"),
                FlagActive = "1",
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            },
            new Sys_User
            {
                UserCode = "USER_INACTIVE",
                UserName = "Nguoi dung dung hoat dong",
                UserPassword = Sys_User.GetEncodedHash("PassInactive"),
                FlagActive = "0",
                LogLUDTimeUTC = DateTime.UtcNow,
                LogLUBy = "seed"
            }
        );
        await db.SaveChangesAsync();
    }
}

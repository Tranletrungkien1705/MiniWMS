using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

// port 1:1 HomeController.cs — Mst_Sys_Config
// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\HomeController.cs:180-198
//        (gate dựng `new Mst_Sys_Config() { NetworkID = networkID }` → strWhereClause_Mst_Sys_Config :373-384
//         → WA_Mst_Sys_Config_Get → RT_Mst_Sys_Config_Login_Async).
// Cột: DDL nguồn 20210701.NCCauHinh_QRox_eTEM.sql:178-189 (OrgID, SysConfigID, NetworkID, SysConfigName,
//       SysConfigDesc, Remark, FlagActive, LogLUDTimeUTC, LogLUBy).
public class Mst_Sys_Config
{
    public string OrgID { get; set; } = "";
    public string SysConfigID { get; set; } = "";
    public string NetworkID { get; set; } = "";
    public string SysConfigName { get; set; } = "";
    public string? SysConfigDesc { get; set; }
    public string? Remark { get; set; }
    public string? FlagActive { get; set; }
    public DateTime? LogLUDTimeUTC { get; set; }
    public string? LogLUBy { get; set; }
}

public class Mst_Sys_ConfigConfiguration : IEntityTypeConfiguration<Mst_Sys_Config>
{
    public void Configure(EntityTypeBuilder<Mst_Sys_Config> b)
    {
        b.ToTable("Mst_Sys_Config");
        b.HasKey(x => new { x.OrgID, x.SysConfigID });
        b.Property(x => x.OrgID).HasMaxLength(50);
        b.Property(x => x.SysConfigID).HasMaxLength(50);
        b.Property(x => x.NetworkID).HasMaxLength(50);
        b.Property(x => x.SysConfigName).HasMaxLength(200);
        b.Property(x => x.SysConfigDesc).HasMaxLength(200);
        b.Property(x => x.Remark).HasMaxLength(200);
        b.Property(x => x.FlagActive).HasMaxLength(10);
        b.Property(x => x.LogLUBy).HasMaxLength(50);
    }
}

/// <summary>Dữ liệu mẫu cấu hình hệ thống (idempotent, chỉ THÊM dòng mới).</summary>
public class Mst_Sys_ConfigSeeder : IPortSeeder
{
    public int Order => 13;

    public async Task SeedAsync(AppDbContext db)
    {
        var set = db.Set<Mst_Sys_Config>();
        if (await set.AnyAsync()) return;

        set.AddRange(
            new Mst_Sys_Config { OrgID = "ORG01", SysConfigID = "CFG01", NetworkID = "1001", SysConfigName = "Cấu hình chung", SysConfigDesc = "Cấu hình mặc định mạng 1001", Remark = "seed", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
            new Mst_Sys_Config { OrgID = "ORG02", SysConfigID = "CFG02", NetworkID = "1001", SysConfigName = "Cấu hình chi nhánh", SysConfigDesc = "Cấu hình mạng 1001 chi nhánh 2", Remark = "seed", FlagActive = "1", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow },
            new Mst_Sys_Config { OrgID = "ORG03", SysConfigID = "CFG03", NetworkID = "1002", SysConfigName = "Cấu hình mạng khác", SysConfigDesc = "Cấu hình mạng 1002", Remark = "seed", FlagActive = "0", LogLUBy = "seed", LogLUDTimeUTC = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }
}

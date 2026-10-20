using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Controllers.Port;
using MiniWMS.Data;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// Test port 1:1 #326 — Mst_InventoryLevelTypeController/Edit.
/// Phủ: body rỗng, không tìm thấy mã, thành công round-trip, guard tên rỗng, guard Org không tồn tại, guard Org ngừng hoạt động.
/// </summary>
public class MstInventoryLevelTypeTests
{
    private static readonly JsonSerializerOptions JsonOpts = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static string Json(object? o) => JsonSerializer.Serialize(o, JsonOpts);

    private static (AppDbContext db, SqliteConnection conn) NewDb()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(conn).Options;
        var db = new AppDbContext(opt, new TenantContext { OrgId = TenantContext.DefaultOrgId });
        db.Database.EnsureCreated();
        return (db, conn);
    }

    private static async Task SeedAsync(AppDbContext db)
    {
        db.Set<Mst_Org>().AddRange(
            new Mst_Org { OrgID = "ORG01", OrgName = "Chi nhánh 1", FlagActive = "1" },
            new Mst_Org { OrgID = "ORG03", OrgName = "Chi nhánh 3", FlagActive = "0" });
        db.Set<Mst_InventoryLevelType>().AddRange(
            new Mst_InventoryLevelType { OrgID = "ORG01", InvLevelType = "L1", InvLevelTypeName = "Level 1", FlagActive = "1" },
            new Mst_InventoryLevelType { OrgID = "ORG01", InvLevelType = "L2", InvLevelTypeName = "Level 2", FlagActive = "1" },
            new Mst_InventoryLevelType { OrgID = "ORG03", InvLevelType = "L3", InvLevelTypeName = "Level 3", FlagActive = "1" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Edit_EmptyBody_ReturnsEmptyMessage()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Edit(null);
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("Mã cấp kho trống", Json(ok.Value));
        }
    }

    [Fact]
    public async Task Edit_NotFound_ReturnsNotFoundMessage()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Edit(new() { InvLevelType = "ZZZ", InvLevelTypeName = "X", FlagActive = "1", OrgID = "ORG01" });
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("không có trong hệ thống", Json(ok.Value));
        }
    }

    [Fact]
    public async Task Edit_Success_UpdatesNameAndFlag_RoundTrip()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Edit(new() { InvLevelType = "L1", InvLevelTypeName = "  Updated Name  ", FlagActive = "0", OrgID = "ORG01", WAUserCode = "tester" });
            Assert.IsType<OkObjectResult>(r);

            db.ChangeTracker.Clear();
            var row = await db.Set<Mst_InventoryLevelType>().FirstAsync(x => x.OrgID == "ORG01" && x.InvLevelType == "L1");
            Assert.Equal("Updated Name", row.InvLevelTypeName);
            Assert.Equal("0", row.FlagActive);
            Assert.Equal("tester", row.LogLUBy);
            Assert.NotNull(row.LogLUDTimeUTC);
        }
    }

    [Fact]
    public async Task Edit_FlagActiveArbitrary_CoercedTo0()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Edit(new() { InvLevelType = "L2", InvLevelTypeName = "N2", FlagActive = "yes", OrgID = "ORG01" });
            Assert.IsType<OkObjectResult>(r);

            db.ChangeTracker.Clear();
            var row = await db.Set<Mst_InventoryLevelType>().FirstAsync(x => x.OrgID == "ORG01" && x.InvLevelType == "L2");
            Assert.Equal("0", row.FlagActive);
        }
    }

    [Fact]
    public async Task Edit_EmptyName_FailsWithInvalidName()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Edit(new() { InvLevelType = "L1", InvLevelTypeName = "", FlagActive = "1", OrgID = "ORG01" });
            var bad = Assert.IsType<BadRequestObjectResult>(r);
            Assert.Contains("Mst_InventoryLevelType_UpdateX_InvalidInvLevelTypeName", Json(bad.Value));
        }
    }

    [Fact]
    public async Task Edit_OrgNotFound_FailsWithOrgNotFound()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            db.Set<Mst_InventoryLevelType>().Add(new Mst_InventoryLevelType { OrgID = "ORG99", InvLevelType = "LX", InvLevelTypeName = "Orphan", FlagActive = "1" });
            await db.SaveChangesAsync();
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Edit(new() { InvLevelType = "LX", InvLevelTypeName = "New", FlagActive = "1", OrgID = "ORG99" });
            var bad = Assert.IsType<BadRequestObjectResult>(r);
            Assert.Contains("Mst_Org_CheckDB_OrgNotFound", Json(bad.Value));
        }
    }

    [Fact]
    public async Task Edit_OrgInactive_FailsWithFlagActiveNotMatched()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Edit(new() { InvLevelType = "L3", InvLevelTypeName = "New", FlagActive = "1", OrgID = "ORG03" });
            var bad = Assert.IsType<BadRequestObjectResult>(r);
            Assert.Contains("Mst_Org_CheckDB_FlagActiveNotMatched", Json(bad.Value));
        }
    }
}

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
/// Test port 1:1 #328 — Mst_InventoryLevelTypeController/Delete.
/// Phủ: body rỗng, InvLevelType rỗng, không tìm thấy, thành công round-trip (xoá row thật), ca hoa/thường,
/// guard Org không tồn tại (Mst_Org_CheckDB_OrgNotFound), guard Org ngừng (Mst_Org_CheckDB_FlagActiveNotMatched).
/// </summary>
public class MstInventoryLevelTypeDeleteTests
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
            new Mst_Org { OrgID = "ORG02", OrgName = "Chi nhánh 2", FlagActive = "1" },
            new Mst_Org { OrgID = "ORG03", OrgName = "Chi nhánh 3 (ngừng)", FlagActive = "0" });
        db.Set<Mst_InventoryLevelType>().AddRange(
            new Mst_InventoryLevelType { OrgID = "ORG01", InvLevelType = "L1", InvLevelTypeName = "Level 1", FlagActive = "1" },
            new Mst_InventoryLevelType { OrgID = "ORG02", InvLevelType = "L1", InvLevelTypeName = "Level 1 ORG02", FlagActive = "1" },
            new Mst_InventoryLevelType { OrgID = "ORG01", InvLevelType = "L2", InvLevelTypeName = "Level 2", FlagActive = "1" },
            new Mst_InventoryLevelType { OrgID = "ORG03", InvLevelType = "L3", InvLevelTypeName = "Level 3 (org ngừng)", FlagActive = "1" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Delete_NullBody_ReturnsEmptyMessage()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Delete(null);
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("Mã cấp kho!", Json(ok.Value));
        }
    }

    [Fact]
    public async Task Delete_EmptyInvLevelType_ReturnsEmptyMessage()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Delete(new() { InvLevelType = "   " });
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("Mã cấp kho!", Json(ok.Value));
            Assert.Equal(4, await db.Set<Mst_InventoryLevelType>().CountAsync());
        }
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsNotFoundMessage_NoRowDeleted()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Delete(new() { InvLevelType = "ZZZ" });
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("không có trong hệ thống", Json(ok.Value));
            Assert.Equal(4, await db.Set<Mst_InventoryLevelType>().CountAsync());
        }
    }

    [Fact]
    public async Task Delete_Success_DeletesFirstOrderedRow_AndRoundTrip()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            Assert.Equal(2, await db.Set<Mst_InventoryLevelType>().CountAsync(x => x.InvLevelType == "L1"));

            var r = await ctrl.Delete(new() { InvLevelType = "L1" });
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("Xóa danh mục cấp kho thành công", Json(ok.Value));

            db.ChangeTracker.Clear();
            var left = await db.Set<Mst_InventoryLevelType>()
                .Where(x => x.InvLevelType == "L1").ToListAsync();
            Assert.Single(left);
            Assert.Equal("ORG02", left[0].OrgID);
        }
    }

    [Fact]
    public async Task Delete_CaseInsensitive_FindsAndDeletes()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Delete(new() { InvLevelType = "l2" });
            Assert.IsType<OkObjectResult>(r);
            db.ChangeTracker.Clear();
            Assert.False(await db.Set<Mst_InventoryLevelType>().AnyAsync(x => x.InvLevelType == "L2"));
        }
    }

    [Fact]
    public async Task Delete_WithOrgIDFilter_DeletesRowInThatOrg()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Delete(new() { InvLevelType = "L1", OrgID = "ORG02" });
            Assert.IsType<OkObjectResult>(r);
            db.ChangeTracker.Clear();
            Assert.True(await db.Set<Mst_InventoryLevelType>().AnyAsync(x => x.OrgID == "ORG01" && x.InvLevelType == "L1"));
            Assert.False(await db.Set<Mst_InventoryLevelType>().AnyAsync(x => x.OrgID == "ORG02" && x.InvLevelType == "L1"));
        }
    }

    [Fact]
    public async Task Delete_OrgInactive_FailsWithFlagActiveNotMatched()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Delete(new() { InvLevelType = "L3" });
            var bad = Assert.IsType<BadRequestObjectResult>(r);
            Assert.Contains("Mst_Org_CheckDB_FlagActiveNotMatched", Json(bad.Value));
            Assert.True(await db.Set<Mst_InventoryLevelType>().AnyAsync(x => x.OrgID == "ORG03" && x.InvLevelType == "L3"));
        }
    }

    [Fact]
    public async Task Delete_OrgNotFound_FailsWithOrgNotFound()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            db.Set<Mst_InventoryLevelType>().Add(new Mst_InventoryLevelType
            {
                OrgID = "ORG99", InvLevelType = "LX", InvLevelTypeName = "Orphan", FlagActive = "1"
            });
            await db.SaveChangesAsync();

            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Delete(new() { InvLevelType = "LX" });
            var bad = Assert.IsType<BadRequestObjectResult>(r);
            Assert.Contains("Mst_Org_CheckDB_OrgNotFound", Json(bad.Value));
            Assert.True(await db.Set<Mst_InventoryLevelType>().AnyAsync(x => x.OrgID == "ORG99" && x.InvLevelType == "LX"));
        }
    }
}

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
/// Test port 1:1 #327 — Mst_InventoryLevelTypeController/Detail.
/// Phủ: body rỗng, InvLevelType rỗng, không tìm thấy, thành công, khớp hoa/thường, filter OrgID tuỳ chọn, thứ tự OrgID asc.
/// </summary>
public class MstInventoryLevelTypeDetailTests
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
            new Mst_Org { OrgID = "ORG02", OrgName = "Chi nhánh 2", FlagActive = "1" });
        db.Set<Mst_InventoryLevelType>().AddRange(
            new Mst_InventoryLevelType { OrgID = "ORG01", InvLevelType = "L1", InvLevelTypeName = "Level 1", FlagActive = "1" },
            new Mst_InventoryLevelType { OrgID = "ORG01", InvLevelType = "L2", InvLevelTypeName = "Level 2", FlagActive = "1" },
            new Mst_InventoryLevelType { OrgID = "ORG02", InvLevelType = "L1", InvLevelTypeName = "Level 1 ORG02", FlagActive = "0" });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Detail_NullBody_ReturnsEmptyObject()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Detail(null);
            var ok = Assert.IsType<OkObjectResult>(r);
            var entity = Assert.IsType<Mst_InventoryLevelType>(ok.Value);
            Assert.Equal("", entity.OrgID);
            Assert.Equal("", entity.InvLevelType);
            Assert.Null(entity.InvLevelTypeName);
            Assert.Null(entity.FlagActive);
        }
    }

    [Fact]
    public async Task Detail_EmptyInvLevelType_ReturnsEmptyObject()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Detail(new() { InvLevelType = "  " });
            var ok = Assert.IsType<OkObjectResult>(r);
            var entity = Assert.IsType<Mst_InventoryLevelType>(ok.Value);
            Assert.Equal("", entity.InvLevelType);
            Assert.Null(entity.FlagActive);
        }
    }

    [Fact]
    public async Task Detail_NotFound_ReturnsEmptyObject()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Detail(new() { InvLevelType = "ZZZ" });
            var ok = Assert.IsType<OkObjectResult>(r);
            var entity = Assert.IsType<Mst_InventoryLevelType>(ok.Value);
            Assert.Equal("", entity.InvLevelType);
            Assert.Null(entity.FlagActive);
        }
    }

    [Fact]
    public async Task Detail_Found_ReturnsRow()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Detail(new() { InvLevelType = "L2" });
            var ok = Assert.IsType<OkObjectResult>(r);
            var entity = Assert.IsType<Mst_InventoryLevelType>(ok.Value);
            Assert.Equal("ORG01", entity.OrgID);
            Assert.Equal("L2", entity.InvLevelType);
            Assert.Equal("Level 2", entity.InvLevelTypeName);
        }
    }

    [Fact]
    public async Task Detail_CaseInsensitive_FindsRow()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Detail(new() { InvLevelType = "l2" });
            var ok = Assert.IsType<OkObjectResult>(r);
            var entity = Assert.IsType<Mst_InventoryLevelType>(ok.Value);
            Assert.Equal("L2", entity.InvLevelType);
        }
    }

    [Fact]
    public async Task Detail_WithOrgID_FiltersByOrg()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Detail(new() { InvLevelType = "L1", OrgID = "ORG02" });
            var ok = Assert.IsType<OkObjectResult>(r);
            var entity = Assert.IsType<Mst_InventoryLevelType>(ok.Value);
            Assert.Equal("ORG02", entity.OrgID);
            Assert.Equal("Level 1 ORG02", entity.InvLevelTypeName);
        }
    }

    [Fact]
    public async Task Detail_MultipleOrgs_OrderByOrgIDAsc()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var r = await ctrl.Detail(new() { InvLevelType = "L1" });
            var ok = Assert.IsType<OkObjectResult>(r);
            var entity = Assert.IsType<Mst_InventoryLevelType>(ok.Value);
            Assert.Equal("ORG01", entity.OrgID);
        }
    }
}

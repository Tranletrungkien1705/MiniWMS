using System.Text.Encodings.Web;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Controllers.Port;
using MiniWMS.Data;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// Test port 1:1 #329 — Mst_InventoryLevelTypeController/Import.
/// Phủ: file rỗng, sai phần mở rộng, sai số cột, dòng rỗng Mã/Tên, trùng mã trong file,
/// import thành công round-trip, dòng đã tồn tại bị bỏ qua im lặng (lỗi nguồn giữ nguyên), OrgID rỗng → bỏ qua tất cả.
/// </summary>
public class MstInventoryLevelTypeImportTests
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
        await db.SaveChangesAsync();
    }

    /// <summary>Sinh file .xlsx với các headers + rows (dữ liệu bắt đầu dòng 2, giống nguồn đọc từ A2).</summary>
    private static byte[] Xlsx(string[] headers, params string[][] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");
        for (var c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
        for (var r = 0; r < rows.Length; r++)
            for (var c = 0; c < rows[r].Length && c < headers.Length; c++)
                ws.Cell(r + 2, c + 1).Value = rows[r][c];
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static readonly string[] Headers = { "Mã cấp kho", "Tên cấp kho", "Trạng thái" };

    [Fact]
    public async Task Import_InvalidExtension_ReturnsInvalid()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var ff = new MockFormFile(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "test.txt");
            var r = await ctrl.Import(new Mst_InventoryLevelTypeControllerPort.ImportReq { File = ff, OrgID = "ORG01" });
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("File excel import không hợp lệ", Json(ok.Value));
        }
    }

    [Fact]
    public async Task Import_EmptyFile_ReturnsNoDataMessage()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var bytes = Xlsx(Headers);
            var r = await ctrl.ImportCoreAsync(bytes, "ORG01", "tester");
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("File excel import không có dữ liệu", Json(ok.Value));
        }
    }

    [Fact]
    public async Task Import_BadColumnCount_ReturnsInvalid()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var bytes = Xlsx(new[] { "Mã cấp kho", "Tên cấp kho" }, new[] { "L1", "Cấp 1" });
            var r = await ctrl.ImportCoreAsync(bytes, "ORG01", "tester");
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("File excel import không hợp lệ", Json(ok.Value));
        }
    }

    [Fact]
    public async Task Import_RowEmptyInvLevelType_ReturnsMessage()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var bytes = Xlsx(Headers, new[] { "L1", "Cấp 1", "1" }, new[] { "", "Cấp 2", "1" });
            var r = await ctrl.ImportCoreAsync(bytes, "ORG01", "tester");
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("Mã cấp kho không được trống! - Dòng 4", Json(ok.Value));
        }
    }

    [Fact]
    public async Task Import_RowEmptyName_ReturnsMessage()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var bytes = Xlsx(Headers, new[] { "L1", "", "1" });
            var r = await ctrl.ImportCoreAsync(bytes, "ORG01", "tester");
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("Tên cấp kho không được trống! - Dòng 3", Json(ok.Value));
        }
    }

    [Fact]
    public async Task Import_DuplicateInFile_ReturnsMessage()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var bytes = Xlsx(Headers,
                new[] { "L1", "Cấp 1", "1" },
                new[] { "L1", "Cấp 1 lặp", "1" });
            var r = await ctrl.ImportCoreAsync(bytes, "ORG01", "tester");
            var ok = Assert.IsType<OkObjectResult>(r);
            // Nguồn 519: "Mã cấp kho 'L1' bị lặp trong file excel!" + strRows(" - Dòng 3") + " - 4".
            Assert.Contains("Mã cấp kho 'L1' bị lặp trong file excel! - Dòng 3 - 4", Json(ok.Value));
        }
    }

    [Fact]
    public async Task Import_Success_InsertsRows_RoundTrip()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var bytes = Xlsx(Headers,
                new[] { "L1", "  Cấp Một  ", "0" },
                new[] { "l2", "Cấp hai", "1" });
            var r = await ctrl.ImportCoreAsync(bytes, "ORG01", "tester");
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("Đã nhập dữ liệu excel thành công", Json(ok.Value));
            Assert.Contains("\"Success\":true", Json(ok.Value));

            db.ChangeTracker.Clear();
            var all = await db.Set<Mst_InventoryLevelType>().Where(x => x.OrgID == "ORG01").ToListAsync();
            Assert.Equal(2, all.Count);
            var l1 = all.Single(x => x.InvLevelType == "L1");
            Assert.Equal("Cấp Một", l1.InvLevelTypeName);
            Assert.Equal("1", l1.FlagActive);
            Assert.Equal("tester", l1.LogLUBy);
            Assert.Equal("1001", l1.NetworkID);
            Assert.NotNull(l1.LogLUDTimeUTC);
            var l2 = all.Single(x => x.InvLevelType == "L2");
            Assert.Equal("Cấp hai", l2.InvLevelTypeName);
        }
    }

    [Fact]
    public async Task Import_ExistingCode_SilentlySkipped_SuccessTrue()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            db.Set<Mst_InventoryLevelType>().Add(new Mst_InventoryLevelType { OrgID = "ORG01", InvLevelType = "L1", InvLevelTypeName = "Đã có", FlagActive = "1" });
            await db.SaveChangesAsync();

            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var bytes = Xlsx(Headers,
                new[] { "L1", "Trùng DB", "1" },
                new[] { "L2", "Mới", "1" });
            var r = await ctrl.ImportCoreAsync(bytes, "ORG01", "tester");
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("\"Success\":true", Json(ok.Value));
            Assert.Contains("Đã nhập dữ liệu excel thành công", Json(ok.Value));

            db.ChangeTracker.Clear();
            var l1 = await db.Set<Mst_InventoryLevelType>().SingleAsync(x => x.OrgID == "ORG01" && x.InvLevelType == "L1");
            Assert.Equal("Đã có", l1.InvLevelTypeName);
            var l2 = await db.Set<Mst_InventoryLevelType>().SingleAsync(x => x.OrgID == "ORG01" && x.InvLevelType == "L2");
            Assert.Equal("Mới", l2.InvLevelTypeName);
        }
    }

    [Fact]
    public async Task Import_EmptyOrgId_SilentlySkipsAll_StillSuccess()
    {
        var (db, conn) = NewDb(); await using (conn)
        {
            await SeedAsync(db);
            var ctrl = new Mst_InventoryLevelTypeControllerPort(db);
            var bytes = Xlsx(Headers, new[] { "L1", "Cấp Một", "1" });
            var r = await ctrl.ImportCoreAsync(bytes, null, "tester");
            var ok = Assert.IsType<OkObjectResult>(r);
            Assert.Contains("\"Success\":true", Json(ok.Value));
            db.ChangeTracker.Clear();
            Assert.Equal(0, await db.Set<Mst_InventoryLevelType>().CountAsync());
        }
    }

    /// <summary>Mock IFormFile để test hành vi Import ở tầng action (extension, null-file) không cần chạy server thật.</summary>
    private sealed class MockFormFile(byte[] content, string fileName) : IFormFile
    {
        public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public string ContentDisposition => "";
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public long Length => content.Length;
        public string Name => "file";
        public string FileName { get; } = fileName;
        public void CopyTo(Stream target) => target.Write(content);
        public Task CopyToAsync(Stream target, CancellationToken ct = default) => CopyCore(target, ct);
        private async Task CopyCore(Stream target, CancellationToken ct) => await target.WriteAsync(content, ct);
        public Stream OpenReadStream() => new MemoryStream(content);
    }
}

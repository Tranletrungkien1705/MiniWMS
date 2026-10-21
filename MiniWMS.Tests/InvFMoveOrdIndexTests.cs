using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #142 InvF_MoveOrdController/Index — nguồn InvF_MoveOrdController.cs:23-85
/// (→ SysAccessCheckDeny BaseController.cs:3245-3269 → WA_Mst_Inventory_Get → WAS_Mst_Inventory_Get).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/invf_moveord/index.
/// Số kỳ vọng tính tay từ nguồn + seeder Mst_InventorySeeder (3 dòng: KHO01/KHO02 FlagIn_Out="1" FlagActive="1",
/// KHO03 FlagIn_Out="0" ⇒ bị loại bởi điều kiện dòng 53).
/// </summary>
public class InvFMoveOrdIndexTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_moveord_{Guid.NewGuid():N}.db");
        var dll = Path.Combine(AppContext.BaseDirectory, "MiniWMS.dll");
        Assert.True(File.Exists(dll), $"Không thấy {dll}");

        var psi = new ProcessStartInfo("dotnet", $"\"{dll}\"")
        {
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.Environment["PORT"] = port.ToString();
        psi.Environment["CONNECTION_STRING"] = $"Data Source={dbPath}";
        psi.Environment["SSO_AUTHORITY"] = "http://127.0.0.1:1";

        var p = Process.Start(psi)!;
        var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
        var up = false;
        for (var i = 0; i < 60 && !up; i++)
        {
            await Task.Delay(500);
            try { up = (await http.GetAsync("/healthz")).IsSuccessStatusCode; } catch { }
        }
        Assert.True(up, "Server không lên trong 30s");
        return (p, dbPath, http);
    }

    private class IndexResult
    {
        public bool Success { get; set; }
        public string? RedirectUrl { get; set; }
        public PageInfo<InvF_MoveOrd>? PageInfo { get; set; }
        public List<Mst_Inventory>? List_Mst_InventoryInOut { get; set; }
    }

    [Fact]
    public async Task Index_GuardAndInventoryInOut_ReturnExpected()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // 1. Body rỗng: không có quyền MNU_INVF_MOVEORD ⇒ RedirectToAction("Index", "AccessDeny") (nguồn dòng 25-29)
            var resEmpty = await http.PostAsJsonAsync("/api/invf_moveord/index", new { });
            Assert.Equal(HttpStatusCode.OK, resEmpty.StatusCode);
            var dataEmpty = await resEmpty.Content.ReadFromJsonAsync<IndexResult>();
            Assert.NotNull(dataEmpty);
            Assert.True(dataEmpty.Success);
            Assert.Equal("/AccessDeny/Index", dataEmpty.RedirectUrl);
            Assert.Null(dataEmpty.PageInfo);
            Assert.Null(dataEmpty.List_Mst_InventoryInOut);

            // 2. Guard: ListSysAccess có mã KHÁC ⇒ vẫn bị chặn
            var resOther = await http.PostAsJsonAsync("/api/invf_moveord/index", new { ListSysAccess = new[] { "MNU_INVF_INVENTORYIN" } });
            var dataOther = await resOther.Content.ReadFromJsonAsync<IndexResult>();
            Assert.NotNull(dataOther);
            Assert.Equal("/AccessDeny/Index", dataOther.RedirectUrl);

            // 3. Guard: ListSysAccess chứa MNU_INVF_MOVEORD (khác hoa/thường) ⇒ được vào
            var resOk = await http.PostAsJsonAsync("/api/invf_moveord/index", new { Page = 2, RecordCount = 5, ListSysAccess = new[] { "mnu_invf_moveord" } });
            Assert.Equal(HttpStatusCode.OK, resOk.StatusCode);
            var dataOk = await resOk.Content.ReadFromJsonAsync<IndexResult>();
            Assert.NotNull(dataOk);
            Assert.True(dataOk.Success);
            Assert.Null(dataOk.RedirectUrl);
            // PageInfo rỗng theo nguồn dòng 43-46 (DataList rỗng, ItemCount/PageCount = 0, giữ Page/RecordCount)
            Assert.NotNull(dataOk.PageInfo);
            Assert.Empty(dataOk.PageInfo!.DataList);
            Assert.Equal(0, dataOk.PageInfo.ItemCount);
            Assert.Equal(0, dataOk.PageInfo.PageCount);
            Assert.Equal(2, dataOk.PageInfo.PageIndex);
            Assert.Equal(5, dataOk.PageInfo.PageSize);
            // Kho nhập/xuất: FlagActive = '1' and FlagIn_Out = '1' ⇒ KHO01, KHO02 (loại KHO03)
            Assert.NotNull(dataOk.List_Mst_InventoryInOut);
            Assert.Equal(2, dataOk.List_Mst_InventoryInOut!.Count);
            Assert.Contains(dataOk.List_Mst_InventoryInOut, x => x.InvCode == "KHO01");
            Assert.Contains(dataOk.List_Mst_InventoryInOut, x => x.InvCode == "KHO02");
            Assert.DoesNotContain(dataOk.List_Mst_InventoryInOut, x => x.InvCode == "KHO03");

            // 4. Guard: IsSysAdmin = true ⇒ được vào, mặc định page = 0, recordcount = 10
            var resAdmin = await http.PostAsJsonAsync("/api/invf_moveord/index", new { IsSysAdmin = true });
            var dataAdmin = await resAdmin.Content.ReadFromJsonAsync<IndexResult>();
            Assert.NotNull(dataAdmin);
            Assert.Null(dataAdmin.RedirectUrl);
            Assert.NotNull(dataAdmin.PageInfo);
            Assert.Equal(0, dataAdmin.PageInfo!.PageIndex);
            Assert.Equal(10, dataAdmin.PageInfo.PageSize);
            Assert.Equal(2, dataAdmin.List_Mst_InventoryInOut!.Count);
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { File.Delete(dbPath); } catch { }
        }
    }
}

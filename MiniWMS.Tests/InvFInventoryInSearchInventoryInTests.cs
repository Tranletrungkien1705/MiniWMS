using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #74 InvF_InventoryInController/SearchInventoryIn — nguồn InvF_InventoryInController.cs:323-361
/// (→ Mst_Inventory_Get_Async → Task_Mst_Inventory_Get → WA_Mst_Inventory_Get → WAS_Mst_Inventory_Get).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/invf_inventoryin/searchinventoryin.
/// Số kỳ vọng tính tay từ nguồn + seeder Mst_InventorySeeder (3 dòng: KHO01/KHO02 FlagIn_Out="1" FlagActive="1", KHO03 FlagIn_Out="0").
/// </summary>
public class InvFInventoryInSearchInventoryInTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_search_{Guid.NewGuid():N}.db");
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

    private class SearchResult
    {
        public bool Success { get; set; }
        public List<Mst_Inventory>? List_Mst_Inventory { get; set; }
    }

    [Fact]
    public async Task SearchInventoryIn_EmptyAndFilters_ReturnExpectedCounts()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // 1. Body rỗng / null: trả tất cả kho có FlagActive = '1' và FlagIn_Out = '1' (2 kho: KHO01 và KHO02; loại KHO03 vì FlagIn_Out = '0')
            var resAll = await http.PostAsJsonAsync("/api/invf_inventoryin/searchinventoryin", new { });
            Assert.Equal(HttpStatusCode.OK, resAll.StatusCode);
            var dataAll = await resAll.Content.ReadFromJsonAsync<SearchResult>();
            Assert.NotNull(dataAll);
            Assert.True(dataAll.Success);
            Assert.NotNull(dataAll.List_Mst_Inventory);
            Assert.Equal(2, dataAll.List_Mst_Inventory.Count);
            Assert.Contains(dataAll.List_Mst_Inventory, x => x.InvCode == "KHO01");
            Assert.Contains(dataAll.List_Mst_Inventory, x => x.InvCode == "KHO02");
            Assert.DoesNotContain(dataAll.List_Mst_Inventory, x => x.InvCode == "KHO03");

            // 2. Lọc theo tên "Hà Nội" -> đúng 1 kho KHO01
            var resHaNoi = await http.PostAsJsonAsync("/api/invf_inventoryin/searchinventoryin", new { TxtSearch = "Hà Nội" });
            Assert.Equal(HttpStatusCode.OK, resHaNoi.StatusCode);
            var dataHaNoi = await resHaNoi.Content.ReadFromJsonAsync<SearchResult>();
            Assert.NotNull(dataHaNoi);
            Assert.True(dataHaNoi.Success);
            Assert.Single(dataHaNoi.List_Mst_Inventory!);
            Assert.Equal("KHO01", dataHaNoi.List_Mst_Inventory![0].InvCode);

            // 3. Lọc theo mã "kho02" (không phân biệt hoa/thường) -> đúng 1 kho KHO02
            var resKho02 = await http.PostAsJsonAsync("/api/invf_inventoryin/searchinventoryin", new { TxtSearch = "kho02" });
            Assert.Equal(HttpStatusCode.OK, resKho02.StatusCode);
            var dataKho02 = await resKho02.Content.ReadFromJsonAsync<SearchResult>();
            Assert.NotNull(dataKho02);
            Assert.True(dataKho02.Success);
            Assert.Single(dataKho02.List_Mst_Inventory!);
            Assert.Equal("KHO02", dataKho02.List_Mst_Inventory![0].InvCode);

            // 4. Lọc với từ khoá không khớp -> 0 dòng
            var resNone = await http.PostAsJsonAsync("/api/invf_inventoryin/searchinventoryin", new { TxtSearch = "kho không tồn tại" });
            Assert.Equal(HttpStatusCode.OK, resNone.StatusCode);
            var dataNone = await resNone.Content.ReadFromJsonAsync<SearchResult>();
            Assert.NotNull(dataNone);
            Assert.True(dataNone.Success);
            Assert.Empty(dataNone.List_Mst_Inventory!);
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { File.Delete(dbPath); } catch { }
        }
    }
}

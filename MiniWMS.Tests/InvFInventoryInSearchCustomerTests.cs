using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #79 InvF_InventoryInController/SearchCustomer — nguồn InvF_InventoryInController.cs:566-623
/// (-> List_Mst_Customer_Async -> List_Mst_Customer_Task -> RT_Mst_Customer_Get / Mst_Customer_Get).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/invf_inventoryin/searchcustomer.
/// Số kỳ vọng tính tay từ nguồn + seeder Mst_Customer (trong InvF_InventoryIn.cs):
///   - NCC01: CustomerName = "Nhà cung cấp Phụ tùng Á Châu", FlagActive = "1", FlagSupplier = "1" -> KHỚP
///   - NCC02: CustomerName = "Nhà cung cấp Dầu nhớt Total", FlagActive = "1", FlagSupplier = "1" -> KHỚP
///   - KH01: CustomerName = "Khách hàng mua buôn", FlagActive = "1", FlagSupplier = "0" -> LOẠI (không phải supplier)
///   - NCC03: CustomerName = "Nhà cung cấp cũ (ngừng)", FlagActive = "0", FlagSupplier = "1" -> LOẠI (không active)
/// </summary>
public class InvFInventoryInSearchCustomerTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_searchcust_{Guid.NewGuid():N}.db");
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

    private class SearchCustomerResult
    {
        public bool Success { get; set; }
        public List<Mst_Customer>? List_Mst_Customer { get; set; }
    }

    [Fact]
    public async Task SearchCustomer_EmptyAndFilters_ReturnExpectedCounts()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // 1. Body rỗng / null: trả tất cả NCC có FlagActive = '1' và FlagSupplier = '1' (2 NCC: NCC01 và NCC02; loại KH01 vì FlagSupplier = '0', loại NCC03 vì FlagActive = '0')
            var resAll = await http.PostAsJsonAsync("/api/invf_inventoryin/searchcustomer", new { });
            Assert.Equal(HttpStatusCode.OK, resAll.StatusCode);
            var dataAll = await resAll.Content.ReadFromJsonAsync<SearchCustomerResult>();
            Assert.NotNull(dataAll);
            Assert.True(dataAll.Success);
            Assert.NotNull(dataAll.List_Mst_Customer);
            Assert.Equal(2, dataAll.List_Mst_Customer.Count);
            Assert.Contains(dataAll.List_Mst_Customer, x => x.CustomerCode == "NCC01");
            Assert.Contains(dataAll.List_Mst_Customer, x => x.CustomerCode == "NCC02");
            Assert.DoesNotContain(dataAll.List_Mst_Customer, x => x.CustomerCode == "KH01");
            Assert.DoesNotContain(dataAll.List_Mst_Customer, x => x.CustomerCode == "NCC03");

            // 2. Lọc theo tên "Á Châu" -> đúng 1 NCC01
            var resAChau = await http.PostAsJsonAsync("/api/invf_inventoryin/searchcustomer", new { TxtSearch = "Á Châu" });
            Assert.Equal(HttpStatusCode.OK, resAChau.StatusCode);
            var dataAChau = await resAChau.Content.ReadFromJsonAsync<SearchCustomerResult>();
            Assert.NotNull(dataAChau);
            Assert.True(dataAChau.Success);
            Assert.Single(dataAChau.List_Mst_Customer!);
            Assert.Equal("NCC01", dataAChau.List_Mst_Customer![0].CustomerCode);

            // 3. Lọc theo mã "ncc02" (không phân biệt hoa/thường) -> đúng 1 NCC02
            var resNcc02 = await http.PostAsJsonAsync("/api/invf_inventoryin/searchcustomer", new { TxtSearch = "ncc02" });
            Assert.Equal(HttpStatusCode.OK, resNcc02.StatusCode);
            var dataNcc02 = await resNcc02.Content.ReadFromJsonAsync<SearchCustomerResult>();
            Assert.NotNull(dataNcc02);
            Assert.True(dataNcc02.Success);
            Assert.Single(dataNcc02.List_Mst_Customer!);
            Assert.Equal("NCC02", dataNcc02.List_Mst_Customer![0].CustomerCode);

            // 4. Lọc với từ khoá không khớp -> 0 dòng
            var resNone = await http.PostAsJsonAsync("/api/invf_inventoryin/searchcustomer", new { TxtSearch = "nhà cung cấp không tồn tại" });
            Assert.Equal(HttpStatusCode.OK, resNone.StatusCode);
            var dataNone = await resNone.Content.ReadFromJsonAsync<SearchCustomerResult>();
            Assert.NotNull(dataNone);
            Assert.True(dataNone.Success);
            Assert.Empty(dataNone.List_Mst_Customer!);
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { File.Delete(dbPath); } catch { }
        }
    }
}

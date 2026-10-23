using System.Diagnostics;
using System.Net.Http.Json;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #91 InvF_InventoryInController/GetProductFromPrdBarCode — nguồn InvF_InventoryInController.cs:2020-2160
/// (→ WA_Mst_Product_Get → WAS_Mst_Product_Get → Mst_Product_Get; + strWhereGetProductByPrdBarcode, strWhereClause_Mst_Product_Get_Base).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/invf_inventoryin/getproductfromprdbarcode.
/// KHÔNG redirect stdout/stderr (không drain → buffer 4KB đầy làm process kẹt — bài học A.4).
///
/// Seed Mst_ProductSeeder (Order 14, 6 dòng):
///   - ORG01/SP01: FlagActive='1', FlagBuy='1', ProductType='PRODUCT', ProductCodeBase='SP01', ProductBarCode='893000000001'
///   - ORG02/SP01: FlagActive='1', FlagBuy='1', ProductType='PRODUCT', ProductCodeBase='SP01', ProductBarCode='893000000001'
///   - ORG01/SP01-L2A: FlagActive='1', FlagBuy='1', ProductType='PRODUCT', ProductCodeBase='SP01', ProductCodeRoot='SP01', ProductBarCode='893000000002'
///   - ORG01/SP01-L2B: FlagActive='1', FlagBuy='1', ProductType='PRODUCT', ProductCodeBase='SP01', ProductCodeRoot='SP01', ProductBarCode=null
///   - ORG01/SP02, ORG01/SP03: không gán flag → LOẠI.
///
/// Kỳ vọng:
///   - PrdBarCode="893000000001": products1 = [ORG01/SP01, ORG02/SP01] → base=ORG01/SP01; query 2
///     (ProductCodeBase="SP01", ProductCode != "SP01") = [SP01-L2A, SP01-L2B] → ListProduct có 3 phần tử (Success = true).
///   - PrdBarCode="893000000002": products1 = [ORG01/SP01-L2A] → base=SP01-L2A; query 2
///     (ProductCodeBase="SP01", ProductCodeRoot="SP01", ProductCode != "SP01-L2A") = [SP01-L2B] → ListProduct có 2 phần tử (Success = true).
///   - PrdBarCode="BARCODE_NOT_FOUND": products1 rỗng → trả 200 Json { Success = false, Messages = "Hàng hóa không có trong hệ thống!" }
///   - PrdBarCode rỗng / null: query 1 tìm tất cả 4 dòng flagged → [0] = ORG01/SP01 → tổng ListProduct có 3 phần tử.
/// </summary>
public class InvFInventoryInGetProductFromPrdBarCodeTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_gpbc_{Guid.NewGuid():N}.db");
        var dll = Path.Combine(AppContext.BaseDirectory, "MiniWMS.dll");
        Assert.True(File.Exists(dll), $"Không thấy {dll}");

        var psi = new ProcessStartInfo("dotnet", $"\"{dll}\"")
        {
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
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

    private class BarCodeResponse
    {
        public bool Success { get; set; }
        public string? Messages { get; set; }
        public List<BarCodeItem>? ListProduct { get; set; }
    }

    private class BarCodeItem
    {
        public string? ProductCode { get; set; }
        public string? ProductCodeBase { get; set; }
        public string? ProductCodeRoot { get; set; }
        public string? ProductName { get; set; }
        public string? ProductType { get; set; }
        public string? FlagActive { get; set; }
        public string? FlagBuy { get; set; }
        public string? InvCodeSuggest { get; set; }
    }

    [Fact]
    public async Task GetProductFromPrdBarCode_AllCases()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1: BarCode "893000000001" -> tìm thấy SP01, ra 3 sản phẩm (SP01, SP01-L2A, SP01-L2B)
            var res1 = await http.PostAsJsonAsync("/api/invf_inventoryin/getproductfromprdbarcode",
                new { PrdBarCode = "893000000001", InvCode = "KHO01" });
            Assert.True(res1.IsSuccessStatusCode);
            var body1 = await res1.Content.ReadFromJsonAsync<BarCodeResponse>();
            Assert.NotNull(body1);
            Assert.True(body1.Success);
            Assert.NotNull(body1.ListProduct);
            Assert.Equal(3, body1.ListProduct.Count);
            Assert.Equal("SP01", body1.ListProduct[0].ProductCode);
            Assert.Equal("", body1.ListProduct[0].InvCodeSuggest);

            // Ca 2: BarCode "893000000002" -> tìm thấy SP01-L2A, query 2 tìm thêm SP01-L2B -> 2 sản phẩm
            var res2 = await http.PostAsJsonAsync("/api/invf_inventoryin/getproductfromprdbarcode",
                new { PrdBarCode = "893000000002" });
            Assert.True(res2.IsSuccessStatusCode);
            var body2 = await res2.Content.ReadFromJsonAsync<BarCodeResponse>();
            Assert.NotNull(body2);
            Assert.True(body2.Success);
            Assert.NotNull(body2.ListProduct);
            Assert.Equal(2, body2.ListProduct.Count);
            Assert.Equal("SP01-L2A", body2.ListProduct[0].ProductCode);
            Assert.Equal("SP01-L2B", body2.ListProduct[1].ProductCode);

            // Ca 3: BarCode không tồn tại trong hệ thống -> Success = false, Messages = "Hàng hóa không có trong hệ thống!"
            var res3 = await http.PostAsJsonAsync("/api/invf_inventoryin/getproductfromprdbarcode",
                new { PrdBarCode = "999999999999" });
            Assert.True(res3.IsSuccessStatusCode);
            var body3 = await res3.Content.ReadFromJsonAsync<BarCodeResponse>();
            Assert.NotNull(body3);
            Assert.False(body3.Success);
            Assert.Equal("Hàng hóa không có trong hệ thống!", body3.Messages);

            // Ca 4: Body rỗng (hoặc PrdBarCode rỗng) -> lọc không theo barcode, lấy dòng đầu SP01 -> ra 3 sản phẩm
            var res4 = await http.PostAsJsonAsync("/api/invf_inventoryin/getproductfromprdbarcode", new { });
            Assert.True(res4.IsSuccessStatusCode);
            var body4 = await res4.Content.ReadFromJsonAsync<BarCodeResponse>();
            Assert.NotNull(body4);
            Assert.True(body4.Success);
            Assert.NotNull(body4.ListProduct);
            Assert.Equal(3, body4.ListProduct.Count);
        }
        finally
        {
            try { p.Kill(true); } catch { }
            http.Dispose();
            try { File.Delete(dbPath); } catch { }
        }
    }
}

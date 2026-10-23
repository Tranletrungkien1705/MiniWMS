using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #90 InvF_InventoryInController/GetProductExactly — nguồn InvF_InventoryInController.cs:1880-2016
/// (→ WA_Mst_Product_Get → WAS_Mst_Product_Get → Mst_Product_Get; + strWhereGetProductId, strWhereClause_Mst_Product_Get_Base).
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/invf_inventoryin/getproductexactly.
/// KHÔNG redirect stdout/stderr (không drain → buffer 4KB đầy làm process kẹt — bài học A.4).
///
/// Seed Mst_ProductSeeder (Order 14, 6 dòng — KHÔNG đổi để không phá #27/#30):
///   - ORG01/SP01, ORG02/SP01, ORG01/SP01-L2A, ORG01/SP01-L2B: FlagActive='1' FlagBuy='1' ProductType='PRODUCT'
///     ProductCodeBase='SP01'.
///   - ORG01/SP02, ORG01/SP03: không gán flag → LOẠI.
///
/// Kỳ vọng:
///   - PrdId="SP01": products1 = [ORG01/SP01, ORG02/SP01] → base=ORG01/SP01; query 2
///     (ProductCodeBase="SP01", ProductCode != "SP01") = [SP01-L2A, SP01-L2B] → tổng 3 dòng, [0]=SP01.
///   - Body rỗng: products1 gồm 4 dòng flagged (order by OrgID/ProductCode) → [0]=ORG01/SP01 → tổng 3.
///   - PrdId="SP02"/"SP03"/"NONEXIST": products1 = 0 → products1[0] ném IndexOutOfRange
///     → PortExceptionFilter trả 400 mã `InvF_InventoryIn_GetProductExactly`.
/// GAP: ReportsService bị bỏ qua nên InvCodeSuggest luôn "".
/// </summary>
public class InvFInventoryInGetProductExactlyTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_gpe_{Guid.NewGuid():N}.db");
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

    private class GpeResponse
    {
        public List<GpeItem>? Data { get; set; }
    }

    private class GpeItem
    {
        public string? ProductCode { get; set; }
        public string? ProductCodeBase { get; set; }
        public string? ProductType { get; set; }
        public string? FlagActive { get; set; }
        public string? FlagBuy { get; set; }
        public string? InvCodeSuggest { get; set; }
    }

    [Fact]
    public async Task GetProductExactly_WithPrdId_EmptyBody_AndGuards()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var res1 = await http.PostAsJsonAsync("/api/invf_inventoryin/getproductexactly",
                new { InvCode = "KHO01", PrdId = "SP01" });
            Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
            var data1 = await res1.Content.ReadFromJsonAsync<GpeResponse>();
            Assert.NotNull(data1);
            Assert.NotNull(data1.Data);
            Assert.Equal(3, data1.Data.Count);
            Assert.Equal("SP01", data1.Data[0].ProductCode);
            Assert.Contains(data1.Data, x => x.ProductCode == "SP01-L2A");
            Assert.Contains(data1.Data, x => x.ProductCode == "SP01-L2B");
            Assert.All(data1.Data, x => Assert.Equal("", x.InvCodeSuggest));
            Assert.All(data1.Data, x => Assert.Equal("PRODUCT", x.ProductType));

            var res2 = await http.PostAsJsonAsync("/api/invf_inventoryin/getproductexactly", new { });
            Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
            var data2 = await res2.Content.ReadFromJsonAsync<GpeResponse>();
            Assert.NotNull(data2);
            Assert.NotNull(data2.Data);
            Assert.Equal(3, data2.Data.Count);
            Assert.Equal("SP01", data2.Data[0].ProductCode);

            var res3 = await http.PostAsJsonAsync("/api/invf_inventoryin/getproductexactly",
                new { InvCode = "KHO01", PrdId = "SP02" });
            Assert.Equal(HttpStatusCode.BadRequest, res3.StatusCode);
            using (var d3 = System.Text.Json.JsonDocument.Parse(await res3.Content.ReadAsStringAsync()))
            {
                Assert.False(d3.RootElement.GetProperty("Success").GetBoolean());
                Assert.Equal("Erridn.Skycic.Inventory.InvF_InventoryIn_GetProductExactly",
                    d3.RootElement.GetProperty("ErrorCode").GetString());
            }

            var res4 = await http.PostAsJsonAsync("/api/invf_inventoryin/getproductexactly",
                new { InvCode = "KHO01", PrdId = "SP03" });
            Assert.Equal(HttpStatusCode.BadRequest, res4.StatusCode);

            var res5 = await http.PostAsJsonAsync("/api/invf_inventoryin/getproductexactly",
                new { InvCode = "KHO01", PrdId = "NONEXIST-XXX" });
            Assert.Equal(HttpStatusCode.BadRequest, res5.StatusCode);
        }
        finally
        {
            try { p.Kill(true); } catch { }
            http.Dispose();
            foreach (var f in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
                try { if (File.Exists(f)) File.Delete(f); } catch { }
        }
    }
}

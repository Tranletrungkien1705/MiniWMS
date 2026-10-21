using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #77 InvF_InventoryInController/ShowSerial — nguồn InvF_InventoryInController.cs:465-512
/// Chuỗi gọi: ShowSerial -> Mst_Inventory_Get_Async -> Task_Mst_Inventory_Get -> WA_Mst_Inventory_Get -> WAS_Mst_Inventory_Get.
/// Test chạy server thật trên cổng trống (FreePort.Get) + DB SQLite tạm.
/// </summary>
public class InvFInventoryInShowSerialTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_serial_{Guid.NewGuid():N}.db");
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

    private class ShowSerialResponse
    {
        public bool Success { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public List<Mst_Inventory>? ListMst_Inventory { get; set; }
        public List<InvF_InventoryInInstSerial>? ListInvF_InventoryInInstSerial { get; set; }
        public string? IF_InvInStatus { get; set; }
        public string? ViewType { get; set; }
    }

    [Fact]
    public async Task ShowSerial_EmptyAndFilters_ReturnExpectedResults()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1: Body rỗng -> Lấy tất cả Mst_Inventory FlagActive = '1' (3 bản ghi từ Mst_InventorySeeder: KHO01, KHO02, KHO03)
            var resEmpty = await http.PostAsJsonAsync("/api/invf_inventoryin/showserial", new { });
            Assert.Equal(HttpStatusCode.OK, resEmpty.StatusCode);
            var dataEmpty = await resEmpty.Content.ReadFromJsonAsync<ShowSerialResponse>();
            Assert.NotNull(dataEmpty);
            Assert.True(dataEmpty.Success);
            Assert.Equal(3, dataEmpty.ListMst_Inventory?.Count);
            Assert.Empty(dataEmpty.ListInvF_InventoryInInstSerial!);

            // Ca 2: Lọc theo InvBUPattern = "HN%" -> KHO01 và KHO03 khớp (2 bản ghi)
            var resHn = await http.PostAsJsonAsync("/api/invf_inventoryin/showserial", new
            {
                InvBUPattern = "HN%"
            });
            Assert.Equal(HttpStatusCode.OK, resHn.StatusCode);
            var dataHn = await resHn.Content.ReadFromJsonAsync<ShowSerialResponse>();
            Assert.NotNull(dataHn);
            Assert.True(dataHn.Success);
            Assert.Equal(2, dataHn.ListMst_Inventory?.Count);
            Assert.All(dataHn.ListMst_Inventory!, item => Assert.StartsWith("HN", item.InvBUPattern));

            // Ca 3: Lọc theo InvBUPattern = "HCM%" -> chỉ KHO02 khớp (1 bản ghi)
            var resHcm = await http.PostAsJsonAsync("/api/invf_inventoryin/showserial", new
            {
                InvBUPattern = "HCM%"
            });
            Assert.Equal(HttpStatusCode.OK, resHcm.StatusCode);
            var dataHcm = await resHcm.Content.ReadFromJsonAsync<ShowSerialResponse>();
            Assert.NotNull(dataHcm);
            Assert.True(dataHcm.Success);
            Assert.Single(dataHcm.ListMst_Inventory!);
            Assert.Equal("KHO02", dataHcm.ListMst_Inventory![0].InvCode);

            // Ca 4: Round-trip truyền đầy đủ ProductCode, ProductCodeUser, ProductName, ListSerial JSON
            var sampleSerials = new List<InvF_InventoryInInstSerial>
            {
                new()
                {
                    IF_InvInNo = "IN001",
                    ProductCode = "PRD002",
                    SerialNo = "SN001",
                    InvCodeInActual = "KHO01"
                },
                new()
                {
                    IF_InvInNo = "IN001",
                    ProductCode = "PRD002",
                    SerialNo = "SN002",
                    InvCodeInActual = "KHO01"
                }
            };
            var listSerialJson = JsonSerializer.Serialize(sampleSerials);

            var resFull = await http.PostAsJsonAsync("/api/invf_inventoryin/showserial", new
            {
                ProductCode = "PRD002",
                ProductCodeUser = "SP02",
                ProductName = "Sản phẩm B",
                InvBUPattern = "HN%",
                ListSerial = listSerialJson,
                IF_InvInStatus = "PENDING",
                ViewType = "detail"
            });
            Assert.Equal(HttpStatusCode.OK, resFull.StatusCode);
            var dataFull = await resFull.Content.ReadFromJsonAsync<ShowSerialResponse>();
            Assert.NotNull(dataFull);
            Assert.True(dataFull.Success);
            Assert.Equal("PRD002", dataFull.ProductCode);
            Assert.Equal("SP02 - Sản phẩm B", dataFull.ProductName);
            Assert.Equal("PENDING", dataFull.IF_InvInStatus);
            Assert.Equal("detail", dataFull.ViewType);
            Assert.Equal(2, dataFull.ListMst_Inventory?.Count);
            Assert.Equal(2, dataFull.ListInvF_InventoryInInstSerial?.Count);
            Assert.Equal("SN001", dataFull.ListInvF_InventoryInInstSerial![0].SerialNo);

            // Ca 5: ListSerial sai định dạng JSON -> ném exception -> trả PortResult.Fail mã WAS_Mst_Inventory_Get (HTTP 400)
            var resInvalidJson = await http.PostAsJsonAsync("/api/invf_inventoryin/showserial", new
            {
                ListSerial = "invalid-json-string-not-array"
            });
            Assert.Equal(HttpStatusCode.BadRequest, resInvalidJson.StatusCode);
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
        }
    }
}

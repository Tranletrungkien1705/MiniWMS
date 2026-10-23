using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using MiniWMS.Controllers.Port;
using MiniWMS.Data;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #127 InvF_InventoryOutController/SearchOrd_OrderSO — nguồn InvF_InventoryOutController.cs:3792-3873.
/// Chuỗi: SearchOrd_OrderSO → strWhereClause_Ord_OrderSOSearch (2575) + WA_Ord_OrderSO_Get (DMS service).
/// </summary>
public class InvFInventoryOutSearchOrdOrderSOTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_out_searchord_{Guid.NewGuid():N}.db");
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

        var ok = false;
        for (var i = 0; i < 40; i++)
        {
            await Task.Delay(500);
            try
            {
                var r = await http.GetAsync("/healthz");
                if (r.StatusCode == HttpStatusCode.OK) { ok = true; break; }
            }
            catch { }
        }
        if (!ok)
        {
            try { p.Kill(); } catch { }
            throw new Exception("Server không lên sau 20s");
        }
        return (p, dbPath, http);
    }

    private static void Stop(Process p, string dbPath, HttpClient http)
    {
        try { http.Dispose(); } catch { }
        try { if (!p.HasExited) p.Kill(); } catch { }
        try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
    }

    [Fact]
    public async Task SearchOrd_OrderSO_EmptyBody_ReturnsApprovedOrdersOnly()
    {
        // Guard mặc định: OrderStatus = 'APPROVE'
        // Seed có 4 dòng:
        //  - SO001: APPROVE, Network 1001, Org ORG01
        //  - SO002: APPROVE, Network 1001, Org ORG01
        //  - SO003: PENDING (bị loại)
        //  - SO004: APPROVE, Network 1002, Org ORG02
        // Khi không lọc NetworkID/OrgID -> ra 3 đơn APPROVE (SO001, SO002, SO004).
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var res = await http.PostAsJsonAsync("/api/invf_inventoryout/searchord_orderso", new SearchOrd_OrderSOReq());
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var body = await res.Content.ReadFromJsonAsync<SearchOrd_OrderSOResult>();
            Assert.NotNull(body);
            Assert.True(body.Success);
            Assert.Equal("BindData_Ord_OrderSO", body.ViewName);
            Assert.NotNull(body.Data);
            Assert.Equal(3, body.Data.ItemCount);
            Assert.Equal(3, body.ListOrd_OrderSO.Count);
            Assert.DoesNotContain(body.ListOrd_OrderSO, x => x.OrderNo == "SO003");
        }
        finally
        {
            Stop(p, dbPath, http);
        }
    }

    [Fact]
    public async Task SearchOrd_OrderSO_FilterNetworkAndOrderNo_MatchesExact()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var req = new SearchOrd_OrderSOReq
            {
                NetworkID = "1001",
                OrderNo = "SO001"
            };
            var res = await http.PostAsJsonAsync("/api/invf_inventoryout/searchord_orderso", req);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var body = await res.Content.ReadFromJsonAsync<SearchOrd_OrderSOResult>();
            Assert.NotNull(body);
            Assert.True(body.Success);
            Assert.NotNull(body.Data);
            Assert.Equal(1, body.Data.ItemCount);
            Assert.Single(body.ListOrd_OrderSO);
            Assert.Equal("SO001", body.ListOrd_OrderSO[0].OrderNo);
        }
        finally
        {
            Stop(p, dbPath, http);
        }
    }

    [Fact]
    public async Task SearchOrd_OrderSO_FilterCustomerNameAndVin_ReturnsExpected()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var req = new SearchOrd_OrderSOReq
            {
                CustomerName = "Ánh Dương",
                Vin = "VIN001"
            };
            var res = await http.PostAsJsonAsync("/api/invf_inventoryout/searchord_orderso", req);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var body = await res.Content.ReadFromJsonAsync<SearchOrd_OrderSOResult>();
            Assert.NotNull(body);
            Assert.True(body.Success);
            Assert.NotNull(body.Data);
            Assert.Equal(1, body.Data.ItemCount);
            Assert.Equal("SO001", body.ListOrd_OrderSO[0].OrderNo);
            Assert.Equal("VIN001", body.ListOrd_OrderSO[0].OrderCF03);
        }
        finally
        {
            Stop(p, dbPath, http);
        }
    }

    [Fact]
    public async Task SearchOrd_OrderSO_Paging_WorksCorrectly()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var req = new SearchOrd_OrderSOReq
            {
                NetworkID = "1001",
                Page = 0,
                RecordCount = 1
            };
            var res = await http.PostAsJsonAsync("/api/invf_inventoryout/searchord_orderso", req);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var body = await res.Content.ReadFromJsonAsync<SearchOrd_OrderSOResult>();
            Assert.NotNull(body);
            Assert.True(body.Success);
            Assert.NotNull(body.Data);
            Assert.Equal(2, body.Data.ItemCount); // Network 1001 có SO001 và SO002 là APPROVE
            Assert.Equal(2, body.Data.PageCount);
            Assert.Single(body.ListOrd_OrderSO);
            Assert.Equal("0", body.StartCount);
        }
        finally
        {
            Stop(p, dbPath, http);
        }
    }
}

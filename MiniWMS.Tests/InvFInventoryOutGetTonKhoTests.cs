using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using MiniWMS.Controllers.Port;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #125 InvF_InventoryOutController/GetTonKho — nguồn InvF_InventoryOutController.cs:3602-3711.
/// Chuỗi: GetTonKho → WA_Inv_InventoryBalance_Get (Inventory.cs:4776) + WA_InvF_InventoryOut_Get (Form.cs:33724).
/// </summary>
public class InvFInventoryOutGetTonKhoTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_out_tonkho_{Guid.NewGuid():N}.db");
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
        try { p.Dispose(); } catch { }
        try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
    }

    [Fact]
    public async Task GetTonKho_EmptyBody_ReturnsAllActivePositiveStock()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var resp = await http.PostAsJsonAsync("/api/invf_inventoryout/gettonkho", new { });
            resp.EnsureSuccessStatusCode();

            var res = await resp.Content.ReadFromJsonAsync<GetTonKhoResult>();
            Assert.NotNull(res);
            Assert.True(res.Success);
            Assert.Equal("ShowTonKho", res.ViewName);
            Assert.Equal("0", res.OnlyView);
            // Seed có 4 dòng balance: 3 dòng QtyTotalOK > 0 (100, 50, 80) và 1 dòng QtyTotalOK = 0
            Assert.Equal(3, res.Lst_Inv_InventoryBalance.Count);
            Assert.All(res.Lst_Inv_InventoryBalance, b => Assert.True((b.QtyTotalOK ?? 0) > 0));
        }
        finally
        {
            Stop(p, dbPath, http);
        }
    }

    [Fact]
    public async Task GetTonKho_FilterProductCodeBase_ReturnsFilteredStock()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var resp = await http.PostAsJsonAsync("/api/invf_inventoryout/gettonkho", new
            {
                ProductCodeBase = "BASE01"
            });
            resp.EnsureSuccessStatusCode();

            var res = await resp.Content.ReadFromJsonAsync<GetTonKhoResult>();
            Assert.NotNull(res);
            Assert.True(res.Success);
            // Chỉ có 2 dòng của BASE01
            Assert.Equal(2, res.Lst_Inv_InventoryBalance.Count);
            Assert.All(res.Lst_Inv_InventoryBalance, b => Assert.Equal("BASE01", b.ProductCode));
        }
        finally
        {
            Stop(p, dbPath, http);
        }
    }

    [Fact]
    public async Task GetTonKho_FilterInvBUPattern_ReturnsFilteredStock()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var resp = await http.PostAsJsonAsync("/api/invf_inventoryout/gettonkho", new
            {
                ProductCodeBase = "BASE01",
                InvBUPattern = "HN%"
            });
            resp.EnsureSuccessStatusCode();

            var res = await resp.Content.ReadFromJsonAsync<GetTonKhoResult>();
            Assert.NotNull(res);
            Assert.True(res.Success);
            // BASE01 tại KHO01 (HN%) -> 1 dòng
            Assert.Single(res.Lst_Inv_InventoryBalance);
            Assert.Equal("KHO01", res.Lst_Inv_InventoryBalance[0].InvCode);
        }
        finally
        {
            Stop(p, dbPath, http);
        }
    }

    [Fact]
    public async Task GetTonKho_ValConvert_DividesStockQuantity()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var resp = await http.PostAsJsonAsync("/api/invf_inventoryout/gettonkho", new
            {
                ProductCodeBase = "BASE01",
                ValConvert = "2"
            });
            resp.EnsureSuccessStatusCode();

            var res = await resp.Content.ReadFromJsonAsync<GetTonKhoResult>();
            Assert.NotNull(res);
            Assert.True(res.Success);
            // Ban đầu 100 và 50 -> chia 2 còn 50 và 25
            var k01 = res.Lst_Inv_InventoryBalance.FirstOrDefault(x => x.InvCode == "KHO01");
            var k02 = res.Lst_Inv_InventoryBalance.FirstOrDefault(x => x.InvCode == "KHO02");
            Assert.NotNull(k01);
            Assert.NotNull(k02);
            Assert.Equal(50, k01.QtyTotalOK);
            Assert.Equal(25, k02.QtyTotalOK);
        }
        finally
        {
            Stop(p, dbPath, http);
        }
    }

    [Fact]
    public async Task GetTonKho_IF_InvOutNo_ReturnsMatchingDetails()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var resp = await http.PostAsJsonAsync("/api/invf_inventoryout/gettonkho", new
            {
                IF_InvOutNo = "OUT001",
                ProductCode = "PROD01"
            });
            resp.EnsureSuccessStatusCode();

            var res = await resp.Content.ReadFromJsonAsync<GetTonKhoResult>();
            Assert.NotNull(res);
            Assert.True(res.Success);
            Assert.Single(res.Lst_InvF_InventoryOutDtl);
            Assert.Equal("OUT001", res.Lst_InvF_InventoryOutDtl[0].IF_InvOutNo);
            Assert.Equal("PROD01", res.Lst_InvF_InventoryOutDtl[0].ProductCode);
        }
        finally
        {
            Stop(p, dbPath, http);
        }
    }
}

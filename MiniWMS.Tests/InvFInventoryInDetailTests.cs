using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #86 InvF_InventoryInController/Detail — nguồn InvF_InventoryInController.cs:1385-1673
/// → WA_InvF_InventoryIn_Get → WAS_InvF_InventoryIn_Get → InvF_InventoryIn_Get → InvF_InventoryIn_GetX.
/// Test khởi động server THẬT trên cổng trống (FreePort.Get) + DB SQLite tạm, gọi POST /api/invf_inventoryin/detail.
/// Số kỳ vọng tính tay từ nguồn + seeder:
///  - Body rỗng → SysAccessCheckDeny false → RedirectUrl="/AccessDeny/Index".
///  - IsSysAdmin=true, IF_InvInNo="PNK001":
///     · Lst_Mst_Inventory = 2 (KHO01, KHO02 — KHO03 có FlagIn_Out='0' loại).
///     · Lst_Mst_InvInType = 2 (IN01, IN02 — IN03 có FlagActive='0' loại).
///     · List_Mst_VATRate = 2 (VAT10, VAT8 — VAT0 có FlagActive='0' loại).
///     · Lst_Mst_BusinessGroup = 1 (BG01 DV & active).
///     · Lst_MstCustomer = 2 (NCC01, NCC02).
///     · Obj_InvF_InventoryIn.IF_InvInNo = "PNK001".
///     · Lst_InvF_InventoryInDtl = 1 dòng SP01 KHO01 Qty=10.
///     · Lst_InvF_InventoryInDtlUI = 1 item ProductCode="SP01", Qty=10, InvCodeInActual="KHO01",
///       mp_ProductCodeBase="SP01", Lst_Mst_ProductBase có SP01/SP01-L2A/SP01-L2B (ProductCodeBase="SP01").
///  - IF_InvInNo không tồn tại → Success=true, Obj_InvF_InventoryIn=null, các Lst_* chi tiết rỗng.
/// </summary>
public class InvFInventoryInDetailTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_detail_{Guid.NewGuid():N}.db");
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

    private class DetailResultDto
    {
        public bool Success { get; set; }
        public string? RedirectUrl { get; set; }
        public List<Mst_Inventory>? Lst_Mst_Inventory { get; set; }
        public List<Mst_InvInType>? Lst_Mst_InvInType { get; set; }
        public List<Mst_VATRate>? List_Mst_VATRate { get; set; }
        public List<Mst_BusinessGroup>? Lst_Mst_BusinessGroup { get; set; }
        public List<Mst_Customer>? Lst_MstCustomer { get; set; }
        public InvF_InventoryIn? Obj_InvF_InventoryIn { get; set; }
        public List<DetailDtlDto>? Lst_InvF_InventoryInDtl { get; set; }
        public List<DtlUIDto>? Lst_InvF_InventoryInDtlUI { get; set; }
    }

    private class DetailDtlDto
    {
        public string IF_InvInNo { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public string? InvCodeInActual { get; set; }
        public double? Qty { get; set; }
        public string? mp_ProductName { get; set; }
        public string? mp_ProductCodeBase { get; set; }
    }

    private class DtlUIDto
    {
        public string? ProductCode { get; set; }
        public string? InvCodeInActual { get; set; }
        public double? Qty { get; set; }
        public string? mp_ProductCodeBase { get; set; }
        public List<Mst_Product>? Lst_Mst_ProductBase { get; set; }
    }

    [Fact]
    public async Task Detail_WithoutPermission_ReturnsRedirect()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var res = await http.PostAsJsonAsync("/api/invf_inventoryin/detail", new { });
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var data = await res.Content.ReadFromJsonAsync<DetailResultDto>();
            Assert.NotNull(data);
            Assert.False(data!.Success);
            Assert.Equal("/AccessDeny/Index", data.RedirectUrl);
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public async Task Detail_PNK001_ReturnsHeaderDetailsAndDanhMuc()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var body = new { IsSysAdmin = true, IF_InvInNo = "PNK001" };
            var res = await http.PostAsJsonAsync("/api/invf_inventoryin/detail", body);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var data = await res.Content.ReadFromJsonAsync<DetailResultDto>();
            Assert.NotNull(data);
            Assert.True(data!.Success);

            Assert.NotNull(data.Lst_Mst_Inventory);
            Assert.Equal(2, data.Lst_Mst_Inventory!.Count);
            Assert.Contains(data.Lst_Mst_Inventory, x => x.InvCode == "KHO01");
            Assert.Contains(data.Lst_Mst_Inventory, x => x.InvCode == "KHO02");
            Assert.DoesNotContain(data.Lst_Mst_Inventory, x => x.InvCode == "KHO03");

            Assert.NotNull(data.Lst_Mst_InvInType);
            Assert.Equal(2, data.Lst_Mst_InvInType!.Count);

            Assert.NotNull(data.List_Mst_VATRate);
            Assert.Equal(2, data.List_Mst_VATRate!.Count);
            Assert.Contains(data.List_Mst_VATRate, x => x.VATRateCode == "VAT10");
            Assert.Contains(data.List_Mst_VATRate, x => x.VATRateCode == "VAT8");

            Assert.NotNull(data.Lst_Mst_BusinessGroup);
            Assert.Single(data.Lst_Mst_BusinessGroup!);
            Assert.Equal("BG01", data.Lst_Mst_BusinessGroup![0].BusinessGroupCode);

            Assert.NotNull(data.Lst_MstCustomer);
            Assert.Equal(2, data.Lst_MstCustomer!.Count);
            Assert.Contains(data.Lst_MstCustomer, x => x.CustomerCode == "NCC01");
            Assert.Contains(data.Lst_MstCustomer, x => x.CustomerCode == "NCC02");

            Assert.NotNull(data.Obj_InvF_InventoryIn);
            Assert.Equal("PNK001", data.Obj_InvF_InventoryIn!.IF_InvInNo);
            Assert.NotNull(data.Lst_InvF_InventoryInDtl);
            Assert.Single(data.Lst_InvF_InventoryInDtl!);
            var d0 = data.Lst_InvF_InventoryInDtl![0];
            Assert.Equal("SP01", d0.ProductCode);
            Assert.Equal("KHO01", d0.InvCodeInActual);
            Assert.Equal(10, d0.Qty);
            Assert.Equal("Lọc dầu động cơ", d0.mp_ProductName);
            Assert.Equal("SP01", d0.mp_ProductCodeBase);

            Assert.NotNull(data.Lst_InvF_InventoryInDtlUI);
            Assert.Single(data.Lst_InvF_InventoryInDtlUI!);
            var u0 = data.Lst_InvF_InventoryInDtlUI![0];
            Assert.Equal("SP01", u0.ProductCode);
            Assert.Equal(10, u0.Qty);
            Assert.Equal("KHO01", u0.InvCodeInActual);
            Assert.Equal("SP01", u0.mp_ProductCodeBase);
            Assert.NotNull(u0.Lst_Mst_ProductBase);
            Assert.Contains(u0.Lst_Mst_ProductBase!, x => x.ProductCode == "SP01");
            Assert.Contains(u0.Lst_Mst_ProductBase!, x => x.ProductCode == "SP01-L2A");
            Assert.Contains(u0.Lst_Mst_ProductBase!, x => x.ProductCode == "SP01-L2B");
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public async Task Detail_UnknownIfInvInNo_ReturnsEmptyLists()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var body = new { IsSysAdmin = true, IF_InvInNo = "KHONG-TON-TAI" };
            var res = await http.PostAsJsonAsync("/api/invf_inventoryin/detail", body);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var data = await res.Content.ReadFromJsonAsync<DetailResultDto>();
            Assert.NotNull(data);
            Assert.True(data!.Success);
            Assert.Null(data.Obj_InvF_InventoryIn);
            Assert.Empty(data.Lst_InvF_InventoryInDtl!);
            Assert.Empty(data.Lst_InvF_InventoryInDtlUI!);
            Assert.Equal(2, data.Lst_Mst_Inventory!.Count);
            Assert.Equal(2, data.Lst_Mst_InvInType!.Count);
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { File.Delete(dbPath); } catch { }
        }
    }
}

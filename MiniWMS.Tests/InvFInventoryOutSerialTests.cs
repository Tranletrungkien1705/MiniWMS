using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #120 InvF_InventoryOutController/Serial — nguồn InvF_InventoryOutController.cs:2011-2119.
/// Chuỗi: Serial → WA_Inv_InventoryBalanceSerial_Get (Inventory.cs:6117) + WA_InvF_InventoryOut_Get (Form.cs:33724).
/// </summary>
public class InvFInventoryOutSerialTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_out_serial_{Guid.NewGuid():N}.db");
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

    private class SerialResponse
    {
        public bool Success { get; set; }
        public string? ViewName { get; set; }
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
        public string? IF_InvOutStatus { get; set; }
        public List<Inv_InventoryBalanceSerial>? Lst_Inv_InventoryBalanceSerial { get; set; }
        public List<InvF_InventoryOutInstSerial>? Lst_InvF_InventoryOutInstSerial { get; set; }
    }

    [Fact]
    public async Task Serial_EmptyAndFilters_ReturnExpectedCounts()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Seed: Mst_Inventory KHO01(HN%), KHO02(HCM%), KHO03(HN%, FlagIn_Out=0); tất cả FlagActive='1'.
            // Inv_InventoryBalanceSerial: PC1@KHO01 = SN1,SN2; PC2@KHO01 = SN3; PC1@KHO02 = SN4.
            // InvF_InventoryOutInstSerial (OUT001): PC1/SN1, PC1/SN2, PC2/SN3.

            // Ca 1: Body rỗng — không filter ⇒ 4 balance-serial; ProductCode=null ⇒ Lst_InvF_InventoryOutInstSerial = [];
            // IF_InvOutStatus lấy từ dòng đầu của InvF_InventoryOut (OUT001 — PENDING).
            var r1 = await http.PostAsJsonAsync("/api/invf_inventoryout/serial", new { });
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            var d1 = await r1.Content.ReadFromJsonAsync<SerialResponse>();
            Assert.NotNull(d1);
            Assert.True(d1.Success);
            Assert.Equal("Serial", d1.ViewName);
            Assert.Equal("PENDING", d1.IF_InvOutStatus);
            Assert.Equal(4, d1.Lst_Inv_InventoryBalanceSerial?.Count);
            Assert.Empty(d1.Lst_InvF_InventoryOutInstSerial!);

            // Ca 2: ProductCode=PC1 (không invBUPattern) ⇒ balance = 3 (SN1,SN2,SN4); serial theo phiếu OUT001 = 2 (SN1,SN2).
            var r2 = await http.PostAsJsonAsync("/api/invf_inventoryout/serial", new { IF_InvOutNo = "OUT001", ProductCode = "PC1" });
            var d2 = await r2.Content.ReadFromJsonAsync<SerialResponse>();
            Assert.Equal(3, d2!.Lst_Inv_InventoryBalanceSerial?.Count);
            Assert.Equal(2, d2.Lst_InvF_InventoryOutInstSerial?.Count);
            Assert.All(d2.Lst_InvF_InventoryOutInstSerial!, s => Assert.Equal("PC1", s.ProductCodeRoot));

            // Ca 3 (Round-trip có lọc invBUPattern): ProductCode=PC1 + InvBUPattern="HN%" ⇒ balance = 2 (KHO01: SN1,SN2).
            var r3 = await http.PostAsJsonAsync("/api/invf_inventoryout/serial", new { IF_InvOutNo = "OUT001", ProductCode = "PC1", InvBUPattern = "HN%" });
            var d3 = await r3.Content.ReadFromJsonAsync<SerialResponse>();
            Assert.Equal(2, d3!.Lst_Inv_InventoryBalanceSerial?.Count);
            Assert.All(d3.Lst_Inv_InventoryBalanceSerial!, b => Assert.Equal("KHO01", b.InvCode));

            // Ca 4 (Bộ lọc khác ra số dòng khác): ProductCode=PC2 + InvBUPattern="HCM%" ⇒ balance = 0 (PC2 chỉ ở KHO01);
            // serial OUT001 theo PC2 = 1 (SN3).
            var r4 = await http.PostAsJsonAsync("/api/invf_inventoryout/serial", new { IF_InvOutNo = "OUT001", ProductCode = "PC2", InvBUPattern = "HCM%" });
            var d4 = await r4.Content.ReadFromJsonAsync<SerialResponse>();
            Assert.Empty(d4!.Lst_Inv_InventoryBalanceSerial!);
            Assert.Single(d4.Lst_InvF_InventoryOutInstSerial!);

            // Ca 5 (Ordinal trên C# vs LIKE không phân biệt hoa/thường trên SQL):
            // ProductCode="pc1" ⇒ balance vẫn 3 (SQL Server `=` case-insensitive) nhưng Lst_InvF_InventoryOutInstSerial = 0 (Ordinal).
            var r5 = await http.PostAsJsonAsync("/api/invf_inventoryout/serial", new { IF_InvOutNo = "OUT001", ProductCode = "pc1" });
            var d5 = await r5.Content.ReadFromJsonAsync<SerialResponse>();
            Assert.Equal(3, d5!.Lst_Inv_InventoryBalanceSerial?.Count);
            Assert.Empty(d5.Lst_InvF_InventoryOutInstSerial!);

            // Ca 6 (Guard): IF_InvOutNo=NONEXIST ⇒ nguồn IndexOutOfRange (dòng 2097) ⇒ Fail HTTP 400.
            var r6 = await http.PostAsJsonAsync("/api/invf_inventoryout/serial", new { IF_InvOutNo = "NONEXIST", ProductCode = "PC1" });
            Assert.Equal(HttpStatusCode.BadRequest, r6.StatusCode);
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
        }
    }
}

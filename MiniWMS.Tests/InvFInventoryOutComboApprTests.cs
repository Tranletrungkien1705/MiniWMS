using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #122 InvF_InventoryOutController/ComboAppr — nguồn InvF_InventoryOutController.cs:2290-2344.
/// Chuỗi: ComboAppr → WhereClauseInvF_InventoryOut (chỉ IF_InvOutNo) → WA_InvF_InventoryOut_Get (Form.cs:33724)
///       → InvF_InventoryOut_Get_New20210707 (Form.cs:33895) → InvF_InventoryOut_GetX_New20210707 (Form.cs:34079).
/// Seed: OUT001 (InvF_InventoryOutSeeder Order 17) + OUT002 (InvF_InventoryOutDtlSeeder Order 240);
///       DTL OUT001 = {PROD01,PROD02,PROD03}, OUT002 = {PROD01,PROD04}.
/// </summary>
public class InvFInventoryOutComboApprTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_out_comboappr_{Guid.NewGuid():N}.db");
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

    private class ComboApprResponse
    {
        public bool Success { get; set; }
        public string? ViewName { get; set; }
        public string? IF_InvOutNo { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductCodeUser { get; set; }
        public string? ProductName { get; set; }
        public List<InvF_InventoryOutDtl>? Lst_InvF_InventoryOutDtl { get; set; }
    }

    [Fact]
    public async Task ComboAppr_EmptyAndFilters_ReturnExpectedCounts()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Ca 1: body rỗng — không filter ⇒ tất cả DTL (OUT001: 3 + OUT002: 2) = 5.
            var r1 = await http.PostAsJsonAsync("/api/invf_inventoryout/comboappr", new { });
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            var d1 = await r1.Content.ReadFromJsonAsync<ComboApprResponse>();
            Assert.NotNull(d1);
            Assert.True(d1.Success);
            Assert.Equal("ComboAppr", d1.ViewName);
            Assert.Equal(5, d1.Lst_InvF_InventoryOutDtl?.Count);

            // Ca 2: IF_InvOutNo=OUT001 ⇒ 3 DTL (đều của OUT001).
            var r2 = await http.PostAsJsonAsync("/api/invf_inventoryout/comboappr", new { IF_InvOutNo = "OUT001" });
            Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
            var d2 = await r2.Content.ReadFromJsonAsync<ComboApprResponse>();
            Assert.NotNull(d2);
            Assert.Equal(3, d2.Lst_InvF_InventoryOutDtl?.Count);
            Assert.All(d2.Lst_InvF_InventoryOutDtl!, x => Assert.Equal("OUT001", x.IF_InvOutNo));

            // Ca 3: IF_InvOutNo=OUT002 ⇒ 2 DTL (bộ lọc khác → số dòng khác ca 2).
            var r3 = await http.PostAsJsonAsync("/api/invf_inventoryout/comboappr", new { IF_InvOutNo = "OUT002" });
            var d3 = await r3.Content.ReadFromJsonAsync<ComboApprResponse>();
            Assert.NotNull(d3);
            Assert.Equal(2, d3.Lst_InvF_InventoryOutDtl?.Count);
            Assert.All(d3.Lst_InvF_InventoryOutDtl!, x => Assert.Equal("OUT002", x.IF_InvOutNo));

            // Ca 4 (guard của nguồn): IF_InvOutNo=NONEXIST — header không tồn tại.
            // Nguồn dòng 2331 KHÔNG Fail — trả Success với Lst_InvF_InventoryOutDtl rỗng.
            var r4 = await http.PostAsJsonAsync("/api/invf_inventoryout/comboappr", new { IF_InvOutNo = "NONEXIST" });
            Assert.Equal(HttpStatusCode.OK, r4.StatusCode);
            var d4 = await r4.Content.ReadFromJsonAsync<ComboApprResponse>();
            Assert.NotNull(d4);
            Assert.True(d4.Success);
            Assert.Empty(d4.Lst_InvF_InventoryOutDtl!);

            // Ca 5: IF_InvOutNo="out001" (chữ thường) — SQL Server `=` case-insensitive ⇒ vẫn 3 DTL.
            // Port dùng Like+ToLower nên vẫn khớp (Lessons A.14).
            var r5 = await http.PostAsJsonAsync("/api/invf_inventoryout/comboappr", new { IF_InvOutNo = "out001" });
            Assert.Equal(HttpStatusCode.OK, r5.StatusCode);
            var d5 = await r5.Content.ReadFromJsonAsync<ComboApprResponse>();
            Assert.NotNull(d5);
            Assert.Equal(3, d5.Lst_InvF_InventoryOutDtl?.Count);

            // Ca 6: kèm ProductCodeUser / ProductName — chỉ để ViewBag (nguồn không dùng để lọc).
            // `productCode` trong chữ ký gate KHÔNG lọc (nguồn comment dòng 2419-2422) — DTL vẫn 5 khi body rỗng.
            var r6 = await http.PostAsJsonAsync("/api/invf_inventoryout/comboappr",
                new { ProductCode = "PROD01", ProductCodeUser = "PT-A", ProductName = "Sản phẩm A" });
            var d6 = await r6.Content.ReadFromJsonAsync<ComboApprResponse>();
            Assert.NotNull(d6);
            Assert.Equal(5, d6.Lst_InvF_InventoryOutDtl?.Count);
            Assert.Equal("PROD01", d6.ProductCode);
            Assert.Equal("PT-A", d6.ProductCodeUser);
            Assert.Equal("Sản phẩm A", d6.ProductName);
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
        }
    }
}

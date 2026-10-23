using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #82 InvF_InventoryInController/Create — nguồn InvF_InventoryInController.cs:726-888.
/// Chuỗi: Create → BaseController.cs:2214-2237 (SeqNo) → Seq_CommonService.WA_Seq_Common_Get
/// → API SeqController.cs:164-233 → biz WAS_Seq_Common_Get (Seq.cs:2642-2719) → Seq_Common_Get (Seq.cs:126-220)
/// → Seq_Common_MyGet (Seq.cs:42-124, htMap IFInvInNo: "Seq_IF_InvInNo", "{0}PN.{3}.{1:00000}{2}", 100000)
/// → song song: Mst_InvInType_Get_Async / Mst_Inventory_Get_Async / RT_Mst_Customer_Async /
///   List_Mst_VATRate_Async / List_Mst_BusinessGroup_Async.
/// Server thật trên cổng trống (FreePort.Get) + DB SQLite tạm; POST /api/invf_inventoryin/create.
/// Số kỳ vọng tính tay từ nguồn + seeder (Mst_Inventory Order 12, Mst_VATRate Order 34, InvF_InventoryInMasterSeeder Order 35):
///  - Mst_InvInType FlagActive='1': IN01, IN02 (loại IN03 FlagActive='0')
///  - Mst_Inventory FlagActive='1' AND FlagIn_Out='1': KHO01, KHO02 (loại KHO03 FlagIn_Out='0')
///  - Mst_Customer FlagActive='1' AND FlagSupplier='1': NCC01, NCC02 (loại KH01 FlagSupplier='0', NCC03 FlagActive='0')
///  - Mst_BusinessGroup BusinessType='DV' AND FlagActive='1': BG01 (loại BG02 BusinessType='TM', BG03 FlagActive='0')
///  - Mst_VATRate FlagActive='1': VAT10, VAT8 (loại VAT0 FlagActive='0')
///  - IF_InvInNo: PortSeq.Build("PN.{3}.{1:00000}", null, seq, 100000, null, UtcNow) = "PN.<ymd>.<seq5>"
/// </summary>
public class InvFInventoryInCreateTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_create_{Guid.NewGuid():N}.db");
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

    private class CreateResultDto
    {
        public bool Success { get; set; }
        public string? RedirectUrl { get; set; }
        public List<InvF_InventoryIn>? Lst_InvF_InventoryIn { get; set; }
        public List<InvF_InventoryInDtl>? Lst_InvF_InventoryInDtl { get; set; }
        public List<InvF_InventoryInInstLot>? Lst_InvF_InventoryInInstLot { get; set; }
        public List<InvF_InventoryInInstSerial>? Lst_InvF_InventoryInInstSerial { get; set; }
        public List<Mst_Inventory>? Lst_Mst_Inventory { get; set; }
        public List<Mst_InvInType>? Lst_Mst_InvInType { get; set; }
        public List<Mst_Customer>? Lst_Mst_Customer { get; set; }
        public List<Mst_VATRate>? List_Mst_VATRate { get; set; }
        public List<Mst_BusinessGroup>? Lst_Mst_BusinessGroup { get; set; }
    }

    [Fact]
    public async Task Create_WithoutPermission_ReturnsRedirect()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // 1. Body rỗng: SysAccessCheckDeny false (nguồn BaseController.cs:3245-3269) → RedirectUrl = /AccessDeny/Index
            var res = await http.PostAsJsonAsync("/api/invf_inventoryin/create", new { });
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var data = await res.Content.ReadFromJsonAsync<CreateResultDto>();
            Assert.NotNull(data);
            Assert.True(data.Success);
            Assert.Equal("/AccessDeny/Index", data.RedirectUrl);
            Assert.Null(data.Lst_InvF_InventoryIn);

            // 2. IsSysAdmin=false, ListSysAccess rỗng: vẫn Redirect
            var res2 = await http.PostAsJsonAsync("/api/invf_inventoryin/create", new { IsSysAdmin = false });
            var data2 = await res2.Content.ReadFromJsonAsync<CreateResultDto>();
            Assert.Equal("/AccessDeny/Index", data2!.RedirectUrl);

            // 3. ListSysAccess không chứa MNU_INVF_INVENTORYIN_CREATE: vẫn Redirect
            var res3 = await http.PostAsJsonAsync("/api/invf_inventoryin/create",
                new { ListSysAccess = new[] { "MNU_INVF_INVENTORYIN" } });
            var data3 = await res3.Content.ReadFromJsonAsync<CreateResultDto>();
            Assert.Equal("/AccessDeny/Index", data3!.RedirectUrl);
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public async Task Create_WithPermission_ReturnsSkeletonAndDropdowns()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // 1. IsSysAdmin=true: full result (nguồn dòng 871-886)
            var res = await http.PostAsJsonAsync("/api/invf_inventoryin/create",
                new { IsSysAdmin = true, OrgId = "ORG01", NetworkId = "1001" });
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var data = await res.Content.ReadFromJsonAsync<CreateResultDto>();
            Assert.NotNull(data);
            Assert.True(data.Success);
            Assert.Null(data.RedirectUrl);

            // Skeleton: 1 dòng có IF_InvInStatus = "PENDING", OrgID/NetworkID như gửi, IF_InvInNo sinh từ Seq_IF_InvInNo
            Assert.NotNull(data.Lst_InvF_InventoryIn);
            Assert.Single(data.Lst_InvF_InventoryIn!);
            var skeleton = data.Lst_InvF_InventoryIn![0];
            Assert.Equal("ORG01", skeleton.OrgID);
            Assert.Equal("1001", skeleton.NetworkID);
            Assert.Equal("PENDING", skeleton.IF_InvInStatus);
            Assert.NotNull(skeleton.IF_InvInNo);
            // Format nguồn Seq.cs:78 "{0}PN.{3}.{1:00000}{2}" → "PN.<ymd3>.<seq5>"; lần gọi đầu = .00001
            Assert.Matches(@"^PN\.[A-Z0-9]{3}\.00001$", skeleton.IF_InvInNo);

            // Các danh sách con khởi tạo RỖNG (nguồn dòng 882-885)
            Assert.Empty(data.Lst_InvF_InventoryInDtl!);
            Assert.Empty(data.Lst_InvF_InventoryInInstLot!);
            Assert.Empty(data.Lst_InvF_InventoryInInstSerial!);

            // Dropdown Mst_InvInType: FlagActive='1' → IN01, IN02 (loại IN03)
            Assert.Equal(2, data.Lst_Mst_InvInType!.Count);
            Assert.Contains(data.Lst_Mst_InvInType!, x => x.InvInType == "IN01");
            Assert.Contains(data.Lst_Mst_InvInType!, x => x.InvInType == "IN02");
            Assert.DoesNotContain(data.Lst_Mst_InvInType!, x => x.InvInType == "IN03");

            // Dropdown Mst_Inventory: FlagActive='1' AND FlagIn_Out='1' → KHO01, KHO02 (loại KHO03)
            Assert.Equal(2, data.Lst_Mst_Inventory!.Count);
            Assert.Contains(data.Lst_Mst_Inventory!, x => x.InvCode == "KHO01");
            Assert.Contains(data.Lst_Mst_Inventory!, x => x.InvCode == "KHO02");
            Assert.DoesNotContain(data.Lst_Mst_Inventory!, x => x.InvCode == "KHO03");

            // Dropdown Mst_Customer: FlagActive='1' AND FlagSupplier='1' → NCC01, NCC02
            Assert.Equal(2, data.Lst_Mst_Customer!.Count);
            Assert.Contains(data.Lst_Mst_Customer!, x => x.CustomerCode == "NCC01");
            Assert.Contains(data.Lst_Mst_Customer!, x => x.CustomerCode == "NCC02");
            Assert.DoesNotContain(data.Lst_Mst_Customer!, x => x.CustomerCode == "KH01");
            Assert.DoesNotContain(data.Lst_Mst_Customer!, x => x.CustomerCode == "NCC03");

            // Dropdown Mst_BusinessGroup: BusinessType='DV' AND FlagActive='1' → BG01
            Assert.Single(data.Lst_Mst_BusinessGroup!);
            Assert.Equal("BG01", data.Lst_Mst_BusinessGroup![0].BusinessGroupCode);

            // Dropdown Mst_VATRate: FlagActive='1' → VAT10, VAT8 (loại VAT0)
            Assert.Equal(2, data.List_Mst_VATRate!.Count);
            Assert.Contains(data.List_Mst_VATRate!, x => x.VATRateCode == "VAT10");
            Assert.Contains(data.List_Mst_VATRate!, x => x.VATRateCode == "VAT8");
            Assert.DoesNotContain(data.List_Mst_VATRate!, x => x.VATRateCode == "VAT0");

            // 2. Round-trip sequence: gọi lần 2 → IF_InvInNo .00002, cùng prefix ngày
            var res2 = await http.PostAsJsonAsync("/api/invf_inventoryin/create", new { IsSysAdmin = true });
            var data2 = await res2.Content.ReadFromJsonAsync<CreateResultDto>();
            var ifInvInNo2 = data2!.Lst_InvF_InventoryIn![0].IF_InvInNo;
            Assert.NotNull(ifInvInNo2);
            Assert.Matches(@"^PN\.[A-Z0-9]{3}\.00002$", ifInvInNo2!);
            // Cùng prefix ngày (ymd): 6 ký tự đầu = "PN.<ymd>"
            Assert.Equal(skeleton.IF_InvInNo!.Substring(0, 6), ifInvInNo2!.Substring(0, 6));
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public async Task Create_WithListSysAccess_ReturnsFullResult()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // ListSysAccess chứa MNU_INVF_INVENTORYIN_CREATE (không phân biệt hoa/thường) → full result
            var res = await http.PostAsJsonAsync("/api/invf_inventoryin/create",
                new { ListSysAccess = new[] { "mnu_invf_inventoryin_create" } });
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var data = await res.Content.ReadFromJsonAsync<CreateResultDto>();
            Assert.NotNull(data);
            Assert.True(data.Success);
            Assert.Null(data.RedirectUrl);
            Assert.Single(data.Lst_InvF_InventoryIn!);
            // IF_InvInNo vẫn sinh ra dù không có IsSysAdmin (dựa trên ListSysAccess)
            Assert.NotNull(data.Lst_InvF_InventoryIn![0].IF_InvInNo);
            Assert.Equal("PENDING", data.Lst_InvF_InventoryIn![0].IF_InvInStatus);
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { File.Delete(dbPath); } catch { }
        }
    }
}

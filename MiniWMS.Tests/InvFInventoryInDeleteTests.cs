using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using MiniWMS.Controllers.Port;
using MiniWMS.Models.Port;
using Xunit;

namespace MiniWMS.Tests;

/// <summary>
/// #89 InvF_InventoryInController/Delete — nguồn InvF_InventoryInController.cs:1793-1875.
/// Chuỗi: Delete -> WA_InvF_InventoryIn_Get -> WA_InvF_InventoryIn_Save (FlagIsDelete = "1").
/// Server thật trên cổng trống (FreePort.Get) + DB SQLite tạm; POST /api/invf_inventoryin/delete.
/// </summary>
public class InvFInventoryInDeleteTests
{
    private static async Task<(Process p, string dbPath, HttpClient http)> StartAsync()
    {
        var port = FreePort.Get();
        var dbPath = Path.Combine(Path.GetTempPath(), $"invf_in_del_{Guid.NewGuid():N}.db");
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

    private class DeleteResultDto
    {
        public bool Success { get; set; }
        public string? RedirectUrl { get; set; }
        public List<string>? Messages { get; set; }
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }

    private class FailResultDto
    {
        public bool Success { get; set; }
        public string? ErrorCode { get; set; }
    }

    [Fact]
    public async Task Delete_AccessDeny_WhenNoPermission()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var res = await http.PostAsJsonAsync("/api/invf_inventoryin/delete", new
            {
                IsSysAdmin = false,
                ListSysAccess = new List<string> { "SOME_OTHER_PERMISSION" },
                ListInvF_InventoryIn = new[] { new { IF_InvInNo = "PNK001" } }
            });
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var data = await res.Content.ReadFromJsonAsync<DeleteResultDto>();
            Assert.NotNull(data);
            Assert.False(data.Success);
            Assert.Equal("/AccessDeny/Index", data.RedirectUrl);
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public async Task Delete_Guard_NullList_ReturnsFail()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            var res = await http.PostAsJsonAsync("/api/invf_inventoryin/delete", new
            {
                IsSysAdmin = true
            });
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            var data = await res.Content.ReadFromJsonAsync<FailResultDto>();
            Assert.NotNull(data);
            Assert.False(data.Success);
            Assert.Contains("InvF_InventoryIn_Save", data.ErrorCode ?? "");
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public async Task Delete_Guard_InvalidStatus_ReturnsFail()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // Seed demo có PNK002 với status = "APPROVE"
            var res = await http.PostAsJsonAsync("/api/invf_inventoryin/delete", new
            {
                IsSysAdmin = true,
                ListInvF_InventoryIn = new[] { new { IF_InvInNo = "PNK002" } }
            });
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            var data = await res.Content.ReadFromJsonAsync<FailResultDto>();
            Assert.NotNull(data);
            Assert.False(data.Success);
            Assert.Contains("InvF_InventoryIn_SaveX_InvalidStatus", data.ErrorCode ?? "");
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public async Task Delete_Success_RoundTrip()
    {
        var (p, dbPath, http) = await StartAsync();
        try
        {
            // 1. PNK001 ban đầu có trạng thái PENDING
            // Gọi Delete thành công
            var res = await http.PostAsJsonAsync("/api/invf_inventoryin/delete", new
            {
                IsSysAdmin = true,
                ListInvF_InventoryIn = new[] { new { IF_InvInNo = "PNK001" } }
            });
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var data = await res.Content.ReadFromJsonAsync<DeleteResultDto>();
            Assert.NotNull(data);
            Assert.True(data.Success);
            Assert.Equal("Xoá phiếu nhập kho thành công!", data.Title);

            // 2. Kiểm tra Detail không còn tìm thấy PNK001 nữa
            var resDetail = await http.PostAsJsonAsync("/api/invf_inventoryin/detail", new
            {
                IsSysAdmin = true,
                IF_InvInNo = "PNK001"
            });
            Assert.Equal(HttpStatusCode.OK, resDetail.StatusCode);
            var detailData = await resDetail.Content.ReadFromJsonAsync<InvF_InventoryInControllerPort.DetailResult>();
            Assert.NotNull(detailData);
            Assert.Null(detailData.Obj_InvF_InventoryIn);
        }
        finally
        {
            try { p.Kill(); } catch { }
            try { if (File.Exists(dbPath)) File.Delete(dbPath); } catch { }
        }
    }
}

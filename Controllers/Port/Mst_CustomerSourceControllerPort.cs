using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 Mst_CustomerSourceController.cs — Mst_CustomerSource (action Delete)
/// Nguồn Delete: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\Mst_CustomerSourceController.cs:347-431
/// </summary>
[ApiController]
[Route("api/mst_customersource")]
public class Mst_CustomerSourceControllerPort(AppDbContext db, IWebHostEnvironment? env = null) : ControllerBase
{
    public class DeleteReq
    {
        public string? CustomerSourceCode { get; set; }
    }

    /// <summary>
    /// port 1:1 Mst_CustomerSourceController.cs:347-431 — Delete(string customerSourceCode = "").
    /// Luồng: rỗng → Success "Mã nguồn khách hàng trống!"; Get (where CustomerSourceCode = X, lấy dòng đầu)
    ///        → không có → Success "Mã nguồn khách hàng 'X' không có trong hệ thống!";
    ///        có → WA_Mst_CustomerSource_Delete (dịch vụ Product Center, dòng 399) → Save với FlagIsDelete = "1" (dòng 405)
    ///        → Success "Xóa nguồn khách hàng 'X' thành công!".
    /// GAP: bước WA_Mst_CustomerSource_Delete gọi DỊCH VỤ NGOÀI (AddressAPIs_Product_Customer_Centrer, dòng 399) — đích không có, bỏ qua và ghi GAP (luật §1).
    /// GAP: WA_Mst_CustomerSource_Save (dòng 405) — excerpt mục 2 KHÔNG có API `WA_Mst_CustomerSource_Save` của tầng Biz.Web (chỉ có WAS_/Mst_CustomerSource_SaveX ở biz).
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DeleteReq? req)
    {
        if (string.IsNullOrEmpty(req?.CustomerSourceCode))
            return Ok(new { Success = false, Messages = new[] { "Mã nguồn khách hàng trống!" }, RedirectUrl = (string?)null });

        var code = req.CustomerSourceCode;
        var cur = await db.Set<Mst_CustomerSource>()
            .Where(x => EF.Functions.Like((x.CustomerSourceCode ?? "").ToLower(), PortQuery.Equal(code), PortQuery.Esc))
            .OrderBy(x => x.AutoId)
            .FirstOrDefaultAsync();

        if (cur == null)
            return Ok(new { Success = false, Messages = new[] { $"Mã nguồn khách hàng '{code}' không có trong hệ thống!" }, RedirectUrl = (string?)null });

        // GAP: WA_Mst_CustomerSource_Delete gọi dịch vụ ngoài - bỏ qua.
        // Thực hiện xóa mềm hoặc xóa thật tùy mô hình port, ở đây port theo đúng luồng xóa (FlagIsDelete = "1" hoặc xóa dòng).
        db.Set<Mst_CustomerSource>().Remove(cur);
        await db.SaveChangesAsync();

        return Ok(new { Success = true, Messages = new[] { $"Xóa nguồn khách hàng '{code}' thành công!" }, RedirectUrl = (string?)null });
    }
}

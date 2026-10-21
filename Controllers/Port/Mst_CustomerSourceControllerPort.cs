using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 Mst_CustomerSourceController.cs — Mst_CustomerSource (action Delete, GetDetailData)
/// Nguồn Delete: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\Mst_CustomerSourceController.cs:347-431
/// Nguồn GetDetailData: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\Mst_CustomerSourceController.cs:437-489
/// </summary>
[ApiController]
[Route("api/mst_customersource")]
public class Mst_CustomerSourceControllerPort(AppDbContext db, IWebHostEnvironment? env = null) : ControllerBase
{
    public class DeleteReq
    {
        public string? CustomerSourceCode { get; set; }
    }

    public class GetDetailDataReq
    {
        public string? CustomerSourceCode { get; set; }
    }

    /// <summary>
    /// port 1:1 Mst_CustomerSourceController.cs:347-431 — Delete(string customerSourceCode = "").
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

        db.Set<Mst_CustomerSource>().Remove(cur);
        await db.SaveChangesAsync();

        return Ok(new { Success = true, Messages = new[] { $"Xóa nguồn khách hàng '{code}' thành công!" }, RedirectUrl = (string?)null });
    }

    /// <summary>
    /// port 1:1 Mst_CustomerSourceController.cs:437-489 — GetDetailData(string customerSourceCode).
    /// Luồng: nhận body GetDetailDataReq (CustomerSourceCode nullable), không chuẩn hóa.
    /// Gọi bộ lọc strWhereClause_Mst_CustomerSource (chỉ lọc theo CustomerSourceCode nếu có).
    /// Lấy dòng đầu theo OrderBy(x => x.OrgID).ThenBy(x => x.AutoId).
    /// Nếu không tìm thấy, trả về object stub chỉ có CustomerSourceCode.
    /// Trả về object đơn (không phải mảng, không null) trong Data.
    /// </summary>
    [HttpPost("getdetaildata")]
    public async Task<IActionResult> GetDetailData([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] GetDetailDataReq? req)
    {
        var code = req?.CustomerSourceCode;
        Mst_CustomerSource? item = null;

        var query = db.Set<Mst_CustomerSource>().AsQueryable();
        if (!string.IsNullOrEmpty(code))
        {
            query = query.Where(x => EF.Functions.Like((x.CustomerSourceCode ?? "").ToLower(), PortQuery.Equal(code), PortQuery.Esc));
        }

        item = await query
            .OrderBy(x => x.OrgID)
            .ThenBy(x => x.AutoId)
            .FirstOrDefaultAsync();

        if (item == null)
        {
            item = new Mst_CustomerSource
            {
                CustomerSourceCode = code
            };
        }

        return Ok(new
        {
            Success = true,
            Data = item,
            Messages = (string[]?)null,
            RedirectUrl = (string?)null
        });
    }
}

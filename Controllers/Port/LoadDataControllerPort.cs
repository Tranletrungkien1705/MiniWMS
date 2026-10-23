using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 LoadDataController.cs — LoadMst_District (dòng 25-76).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\LoadDataController.cs:25-76 (gate)
/// → Mst_DistrictService.Instance.WA_Mst_District_Get (không có trong excerpt; gọi từ gate).
/// Lọc: ProvinceCode = (dòng 809), FlagActive = Client_Flag.Active (dòng 813).
/// Rỗng/null ProvinceCode → trả mảng rỗng (dòng 44).
/// Không lọc OrgID/NetworkID (dòng 809, 813 chỉ đó).
/// </summary>
[ApiController]
public class LoadDataControllerPort(AppDbContext db) : ControllerBase
{
    [HttpPost("api/loaddata/loadmst_district")]
    public async Task<IActionResult> LoadMst_District(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LoadMst_DistrictReq? req)
    {
        // Nguồn 32: !CUtils.IsNullOrEmpty(provincecode) — null/rỗng → return JsonView với listMst_District rỗng (dòng 44).
        if (string.IsNullOrEmpty(req?.ProvinceCode))
        {
            return Ok(new { Success = true, Data = new List<Mst_District>() });
        }

        var provinceCode = req.ProvinceCode;
        const string clientFlagActive = "1"; // Nguồn 35: Client_Flag.Active (hằng số, không nhận từ client).

        // Nguồn dòng 809, 813: lọc ProvinceCode = và FlagActive = (không IN, không tách dấu phẩy).
        // Port dùng Like + ToLower để mô phỏng collation SQLite không phân biệt hoa/thường (quy tắc A.14).
        var result = await db.Set<Mst_District>()
            .Where(x => EF.Functions.Like(x.ProvinceCode!.ToLower(), PortQuery.Equal(provinceCode), PortQuery.Esc) &&
                        EF.Functions.Like(x.FlagActive!.ToLower(), PortQuery.Equal(clientFlagActive), PortQuery.Esc))
            .ToListAsync();

        return Ok(new { Success = true, Data = result });
    }
}

public class LoadMst_DistrictReq
{
    public string? ProvinceCode { get; set; }
}

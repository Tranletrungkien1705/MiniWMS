using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 Mst_InventoryLevelTypeController.cs — Mst_InventoryLevelType (action Detail, Edit).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\Mst_InventoryLevelTypeController.cs:302-351 (Detail), 222-297 (Edit).
/// Chuỗi Detail: gate Detail → WA_Mst_InventoryLevelType_Get (WAS_Mst_InventoryLevelType_Get → Mst_InventoryLevelType_Get → Mst_InventoryLevelType_GetX).
/// Chuỗi Edit: gate Edit → WA_Mst_InventoryLevelType_Get → ... → WA_Mst_InventoryLevelType_Update (WAS → Update → UpdateX).
/// GAP: không có phiên đăng nhập nên OrgID nhận từ body (nguồn: UserState.Mst_NNT.OrgID — Detail dùng cho ability view; Edit cho Mst_Org_CheckDB).
///      WAUserCode nhận từ body (nguồn: UserState.SysUser.UserCode), mặc định "port".
/// GAP: nguồn dùng user ability view để lọc OrgID trong SQL GetX; đích filter trực tiếp theo OrgID trong body (nếu truyền).
/// </summary>
[ApiController]
public class Mst_InventoryLevelTypeControllerPort(AppDbContext db) : ControllerBase
{
    public class DetailReq
    {
        public string? InvLevelType { get; set; }
        public string? OrgID { get; set; }
    }

    public class EditReq
    {
        public string? InvLevelType { get; set; }
        public string? InvLevelTypeName { get; set; }
        public string? FlagActive { get; set; }
        public string? OrgID { get; set; }
        public string? WAUserCode { get; set; }
    }

    // Nguồn 302-351: Detail(invleveltype) → trả 1 dòng Mst_InventoryLevelType hoặc object rỗng.
    [HttpPost("api/mst_inventoryleveltype/detail")]
    public async Task<IActionResult> Detail([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DetailReq? req)
    {
        // Nguồn 311: !CUtils.IsNullOrEmpty(invleveltype) — rỗng/null → trả Mst_InventoryLevelType() rỗng (dòng 350).
        if (req == null || string.IsNullOrWhiteSpace(req.InvLevelType))
            return Ok(EmptyMst_InventoryLevelType());

        // Nguồn 318 + 717-724: where InvLevelType = value (không wildcard; SQL Server collation không phân biệt hoa/thường).
        var invLevelType = req.InvLevelType.Trim().ToUpper();

        // Nguồn 3875-3904: order miltp.OrgID asc, miltp.InvLevelType asc.
        IQueryable<Mst_InventoryLevelType> q = db.Set<Mst_InventoryLevelType>();
        // GAP: nguồn join #tbl_Mst_NNT_ViewAbility (ability view của user). Đích: OrgID tuỳ chọn trong body (không truyền thì không lọc).
        if (!string.IsNullOrWhiteSpace(req.OrgID))
        {
            var orgID = req.OrgID.Trim().ToUpper();
            q = q.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgID), PortQuery.Esc));
        }
        q = q.Where(x => EF.Functions.Like((x.InvLevelType ?? "").ToLower(), PortQuery.Equal(invLevelType), PortQuery.Esc));

        var first = await q.OrderBy(x => x.OrgID).ThenBy(x => x.InvLevelType).FirstOrDefaultAsync();

        // Nguồn 344-350: không có dòng → trả object rỗng; có → trả dòng đầu.
        return Ok(first ?? EmptyMst_InventoryLevelType());
    }

    // Nguồn 350: `new Mst_InventoryLevelType()` — model nguồn không có initializer nên FlagActive/Name/Log* đều null.
    // Port entity có default FlagActive = "1", nên cần set null để khớp nguồn cho path "không có dữ liệu".
    private static Mst_InventoryLevelType EmptyMst_InventoryLevelType() => new()
    {
        OrgID = "",
        InvLevelType = "",
        InvLevelTypeName = null,
        FlagActive = null,
        LogLUDTimeUTC = null,
        LogLUBy = null,
    };

    [HttpPost("api/mst_inventoryleveltype/edit")]
    public async Task<IActionResult> Edit([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] EditReq? req)
    {
        var waUserCode = string.IsNullOrWhiteSpace(req?.WAUserCode) ? "port" : req!.WAUserCode!;
        var orgID = string.IsNullOrWhiteSpace(req?.OrgID) ? "ORG01" : req!.OrgID!.Trim();

        // Nguồn 231-295: model null/rỗng → Success=true, "Mã cấp kho trống!".
        if (req == null)
            return Ok(new { Success = true, Messages = new[] { "Mã cấp kho trống!" }, RedirectUrl = (string?)null });

        // Nguồn 236-239 + 717-736: chỉ filter theo InvLevelType nếu không rỗng; "=" (không wildcard).
        string? filterInvLevelType = string.IsNullOrWhiteSpace(req.InvLevelType) ? null : req.InvLevelType!.Trim().ToUpper();

        // Nguồn 263 + GetX 3890-3904: order OrgID asc, InvLevelType asc; lấy dòng đầu.
        IQueryable<Mst_InventoryLevelType> q = db.Set<Mst_InventoryLevelType>().Where(x => x.OrgID == orgID);
        if (filterInvLevelType != null)
            q = q.Where(x => EF.Functions.Like((x.InvLevelType ?? "").ToLower(), PortQuery.Equal(filterInvLevelType), PortQuery.Esc));
        var first = await q.OrderBy(x => x.OrgID).ThenBy(x => x.InvLevelType).FirstOrDefaultAsync();

        // Nguồn 285-289: không tìm thấy → Success=true, "Mã cấp kho 'X' không có trong hệ thống!".
        if (first == null)
            return Ok(new { Success = true, Messages = new[] { $"Mã cấp kho '{req.InvLevelType ?? ""}' không có trong hệ thống!" }, RedirectUrl = (string?)null });

        // Nguồn 266-267: ghi đè Name và FlagActive từ client vào row DB (chưa chuẩn hoá — UpdateX sẽ Trim/StdFlag).
        first.InvLevelTypeName = req.InvLevelTypeName;
        first.FlagActive = req.FlagActive;

        // Nguồn 269-272: Ft_Cols_Upd = "Mst_InventoryLevelType.InvLevelTypeName,Mst_InventoryLevelType.FlagActive,"
        var strFt_Cols_Upd = "Mst_InventoryLevelType.InvLevelTypeName,Mst_InventoryLevelType.FlagActive,".Trim().ToUpper();

        // UpdateX 4863-4866: StdParam(OrgID), StdParam(InvLevelType), Trim(Format("{0}", Name)), StdFlag(FlagActive).
        var strOrgID = (first.OrgID ?? "").Trim().ToUpper();
        var strInvLevelType = (first.InvLevelType ?? "").Trim().ToUpper();
        var strInvLevelTypeName = string.Format("{0}", first.InvLevelTypeName).Trim();
        var strFlagActive = first.FlagActive == "1" ? "1" : "0";

        // UpdateX 4868-4869
        var bUpd_InvLevelTypeName = strFt_Cols_Upd.Contains("MST_INVENTORYLEVELTYPE.INVLEVELTYPENAME");
        var bUpd_FlagActive = strFt_Cols_Upd.Contains("MST_INVENTORYLEVELTYPE.FLAGACTIVE");

        // UpdateX 4877-4883: Mst_Org_CheckDB(strOrgID, "1", "1") — tồn tại + FlagActive ∈ "1".
        var org = await db.Set<Mst_Org>().FirstOrDefaultAsync(x => x.OrgID == strOrgID);
        if (org == null)
            return PortResult.Fail("Mst_Org_CheckDB_OrgNotFound", "Check.OrgID", strOrgID);
        var orgFlag = org.FlagActive ?? "";
        if (!"1".Contains(orgFlag))
            return PortResult.Fail("Mst_Org_CheckDB_FlagActiveNotMatched",
                "Check.OrgID", strOrgID,
                "Check.strFlagActiveListToCheck", "1",
                "DB.FlagActive", org.FlagActive);

        // UpdateX 4885-4892: Mst_InventoryLevelType_CheckDB(strOrgID, strInvLevelType, "1", "") — tồn tại.
        var existsInDb = await db.Set<Mst_InventoryLevelType>()
            .AnyAsync(x => x.OrgID == strOrgID && EF.Functions.Like((x.InvLevelType ?? "").ToLower(), PortQuery.Equal(strInvLevelType), PortQuery.Esc));
        if (!existsInDb)
            return PortResult.Fail("Mst_InventoryLevelType_CheckDB_InvLevelTypeNotFound",
                "Check.OrgID", strOrgID, "Check.InvLevelType", strInvLevelType);

        // UpdateX 4894-4904: tên rỗng → InvalidInvLevelTypeName
        if (strInvLevelTypeName.Length < 1)
            return PortResult.Fail("Mst_InventoryLevelType_UpdateX_InvalidInvLevelTypeName",
                "Check.strInvLevelTypeName", strInvLevelTypeName);

        // UpdateX 4910-4927: gán cột theo Ft_Cols_Upd, luôn ghi LogLUDTimeUTC + LogLUBy.
        if (bUpd_InvLevelTypeName) first.InvLevelTypeName = strInvLevelTypeName;
        if (bUpd_FlagActive) first.FlagActive = strFlagActive;
        first.LogLUDTimeUTC = DateTime.UtcNow;
        first.LogLUBy = waUserCode;

        await db.SaveChangesAsync();

        // Nguồn 281-283: Success=true, message, RedirectUrl = Url.Action("Index") (đích không có trang Index → null).
        return Ok(new { Success = true, Messages = new[] { "Sửa danh mục cấp kho thành công!" }, RedirectUrl = (string?)null });
    }
}

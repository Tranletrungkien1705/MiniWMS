using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 Mst_InventoryLevelTypeController.cs — Mst_InventoryLevelType (action Detail, Edit, Delete, Import).
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\Mst_InventoryLevelTypeController.cs:302-351 (Detail), 222-297 (Edit), 355-421 (Delete), 427-577 (Import).
/// Chuỗi Detail: gate Detail → WA_Mst_InventoryLevelType_Get (WAS_Mst_InventoryLevelType_Get → Mst_InventoryLevelType_Get → Mst_InventoryLevelType_GetX).
/// Chuỗi Edit: gate Edit → WA_Mst_InventoryLevelType_Get → ... → WA_Mst_InventoryLevelType_Update (WAS → Update → UpdateX).
/// Chuỗi Delete: gate Delete → WA_Mst_InventoryLevelType_Get (lấy dòng đầu theo where InvLevelType) → WA_Mst_InventoryLevelType_Delete
///              (WAS → Delete → DeleteX: Mst_Org_CheckDB + Mst_InventoryLevelType_CheckDB + xoá).
/// Chuỗi Import (mục tiêu #329): gate Import → đọc file Excel → validate từng dòng → với mỗi dòng gọi WA_Mst_InventoryLevelType_Create
///              (WAS → Create → CreateX: Mst_Org_CheckDB + Mst_InventoryLevelType_CheckDB + SaveDB).
/// GAP: không có phiên đăng nhập nên OrgID nhận từ body/form (nguồn: UserState.Mst_NNT.OrgID — Detail dùng cho ability view; Edit/Import cho Mst_Org_CheckDB).
///      WAUserCode nhận từ body/form (nguồn: UserState.SysUser.UserCode), mặc định "port".
/// GAP: nguồn dùng user ability view để lọc OrgID trong SQL GetX; đích filter trực tiếp theo OrgID trong body (nếu truyền).
/// GAP (Import): ClosedXML chỉ đọc .xlsx; nguồn hỗ trợ cả .xls qua ExcelImportNew → đích trả "không hợp lệ!" cho .xls.
/// GAP (Import): nguồn đánh số dòng theo DataRow gốc; PortExcel.ReadSheet bỏ dòng trống → số dòng trong thông báo có thể lệch
///      nếu tệp có dòng trống xen kẽ.
/// LỖI NGUỒN GIỮ NGUYÊN (Import): gate không đọc return của WA_Mst_InventoryLevelType_Create → lỗi từng dòng bị bỏ qua im lặng,
///      cuối cùng vẫn trả Success=true "Đã nhập dữ liệu excel thành công!" (Mst_InventoryLevelTypeController.cs:557).
/// </summary>
[ApiController]
public class Mst_InventoryLevelTypeControllerPort(AppDbContext db, IConfiguration? config = null) : ControllerBase
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

    public class DeleteReq
    {
        /// <summary>Nguồn 355: tham số `invleveltype` của action Delete.</summary>
        public string? InvLevelType { get; set; }
        /// <summary>GAP: nguồn lọc qua ability view của user; đích nhận OrgID tuỳ chọn (không truyền = không lọc) — cùng mẫu Detail.</summary>
        public string? OrgID { get; set; }
    }

    /// <summary>Form của Import (mục tiêu #329). File là tệp Excel; OrgID/WAUserCode tuỳ chọn (GAP so với session nguồn).</summary>
    public class ImportReq
    {
        public IFormFile? File { get; set; }
        /// <summary>GAP: nguồn lấy OrgID từ `UserState.Mst_NNT.OrgID` (Mst_InventoryLevelTypeController.cs:435); đích nhận qua form.
        /// Không truyền = null → mọi dòng vấp InvalidOrgID và bị bỏ qua im lặng như nguồn.</summary>
        public string? OrgID { get; set; }
        /// <summary>GAP: nguồn lấy WAUserCode từ `UserState.SysUser.UserCode`; đích nhận qua form (mặc định "port").</summary>
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

    // Nguồn 355-421: Delete(invleveltype).
    // Luồng: rỗng → Success "Mã cấp kho!"; Get (where InvLevelType = X, order OrgID/InvLevelType asc, lấy dòng đầu)
    //        → không có → Success "…không có trong hệ thống!"; có → WA_Delete với row đó
    //        (DeleteX: Mst_Org_CheckDB(OrgID,"1","1") + Mst_InventoryLevelType_CheckDB(OrgID,InvLevelType,"1","") + xoá) → Success "Xóa…".
    [HttpPost("api/mst_inventoryleveltype/delete")]
    public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DeleteReq? req)
    {
        // Nguồn 365 + 414-418: `!CUtils.IsNullOrEmpty(invleveltype)` — rỗng → else → Success=true, "Mã cấp kho!"
        // (dùng IsNullOrWhiteSpace nhất quán Detail/Edit trong cùng controller).
        var invLevelTypeRQ = req?.InvLevelType?.Trim().ToUpper();
        if (string.IsNullOrEmpty(invLevelTypeRQ))
            return Ok(new { Success = true, Messages = new[] { "Mã cấp kho!" }, RedirectUrl = (string?)null });

        // Nguồn 372 + strWhereClause 717-724 + GetX 3890-3904: where InvLevelType = X (không wildcard), order OrgID/InvLevelType asc.
        IQueryable<Mst_InventoryLevelType> q = db.Set<Mst_InventoryLevelType>()
            .Where(x => EF.Functions.Like((x.InvLevelType ?? "").ToLower(), PortQuery.Equal(invLevelTypeRQ), PortQuery.Esc));

        // GAP: nguồn lọc qua ability view của user; đích nhận OrgID tuỳ chọn từ body (không truyền → không lọc, như Detail).
        if (!string.IsNullOrWhiteSpace(req?.OrgID))
        {
            var orgFilter = req!.OrgID!.Trim().ToUpper();
            q = q.Where(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(orgFilter), PortQuery.Esc));
        }

        var found = await q.OrderBy(x => x.OrgID).ThenBy(x => x.InvLevelType).FirstOrDefaultAsync();

        // Nguồn 408-412: không có dòng → Success=true, "Mã cấp kho 'X' không có trong hệ thống!"
        if (found == null)
            return Ok(new
            {
                Success = true,
                Messages = new[] { $"Mã cấp kho '{req!.InvLevelType}' không có trong hệ thống!" },
                RedirectUrl = (string?)null
            });

        // Nguồn 400 + DeleteX 5174-5175: lấy row tìm được, StdParam(OrgID) + StdParam(InvLevelType).
        var strOrgID = (found.OrgID ?? "").Trim().ToUpper();
        var strInvLevelType = (found.InvLevelType ?? "").Trim().ToUpper();

        // DeleteX 5183-5189: Mst_Org_CheckDB(strOrgID, TConst.Flag.Yes, TConst.Flag.Active) — tồn tại + FlagActive ∈ "1".
        var org = await db.Set<Mst_Org>().FirstOrDefaultAsync(x => x.OrgID == strOrgID);
        if (org == null)
            return PortResult.Fail("Mst_Org_CheckDB_OrgNotFound", "Check.OrgID", strOrgID);
        var orgFlag = org.FlagActive ?? "";
        if (!"1".Contains(orgFlag))
            return PortResult.Fail("Mst_Org_CheckDB_FlagActiveNotMatched",
                "Check.OrgID", strOrgID,
                "Check.strFlagActiveListToCheck", "1",
                "DB.FlagActive", org.FlagActive);

        // DeleteX 5191-5198: Mst_InventoryLevelType_CheckDB(strOrgID, strInvLevelType, Yes, "") — tồn tại (không kiểm FlagActive).
        var inDb = await db.Set<Mst_InventoryLevelType>()
            .FirstOrDefaultAsync(x => EF.Functions.Like((x.OrgID ?? "").ToLower(), PortQuery.Equal(strOrgID), PortQuery.Esc)
                                   && EF.Functions.Like((x.InvLevelType ?? "").ToLower(), PortQuery.Equal(strInvLevelType), PortQuery.Esc));
        if (inDb == null)
            return PortResult.Fail("Mst_InventoryLevelType_CheckDB_InvLevelTypeNotFound",
                "Check.OrgID", strOrgID, "Check.InvLevelType", strInvLevelType);

        // DeleteX 5203-5213: xoá dòng, SaveData.
        db.Set<Mst_InventoryLevelType>().Remove(inDb);
        await db.SaveChangesAsync();

        // Nguồn 404-406: Success=true, "Xóa danh mục cấp kho thành công!", RedirectUrl = Index (đích null).
        return Ok(new { Success = true, Messages = new[] { "Xóa danh mục cấp kho thành công!" }, RedirectUrl = (string?)null });
    }

    // ======== Nguồn 427-577: Import(HttpPostedFileWrapper file) ========
    // Đích dùng IFormFile qua [FromForm]. Phần lõi tách riêng để test HTTP/controller dùng chung không phụ thuộc IFormFile.
    [HttpPost("api/mst_inventoryleveltype/import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import([FromForm] ImportReq? req)
    {
        // Nguồn: `var oldFileName = file.FileName;` — file null → NullRef → catch SetFailed(). Đích trả message hợp lệ.
        if (req?.File == null)
            return Ok(new { Success = false, Messages = new[] { "File excel import không hợp lệ!" } });

        // Nguồn 441-448: ext != .xlsx && ext != .xls → "File excel import không hợp lệ!".
        var oldFileName = req.File.FileName ?? "";
        var ext = System.IO.Path.GetExtension(oldFileName).ToLower();
        if (ext != ".xlsx" && ext != ".xls")
            return Ok(new { Success = false, Messages = new[] { "File excel import không hợp lệ!" } });

        // GAP: ClosedXML chỉ đọc .xlsx; .xls (BIFF nhị phân) không đọc được. Nguồn dùng ExcelImportNew → có thể đọc .xls.
        if (ext == ".xls")
            return Ok(new { Success = false, Messages = new[] { "File excel import không hợp lệ!" } });

        byte[] bytes;
        await using (var ms = new MemoryStream())
        {
            await req.File.CopyToAsync(ms);
            bytes = ms.ToArray();
        }
        return await ImportCoreAsync(bytes, req.OrgID, req.WAUserCode);
    }

    /// <summary>
    /// Lõi Import — tách ra để test gọi trực tiếp. Nguồn Mst_InventoryLevelTypeController.cs:458-564.
    /// Trả về JsonResultEntry (200 OK) với Success/Messages như nguồn (`return Json(resultEntry)`), KHÔNG dùng PortResult.Fail.
    /// </summary>
    [NonAction]
    public async Task<IActionResult> ImportCoreAsync(byte[] fileContent, string? orgId, string? waUserCode)
    {
        // Nguồn 740-748: GetImportDicColums — thứ tự cột bắt buộc.
        var keys = new[] { "InvLevelType", "InvLevelTypeName", "FlagActive" };
        int columnCount;
        List<Dictionary<string, string?>> rows;
        try
        {
            using var ms = new MemoryStream(fileContent);
            (columnCount, rows) = PortExcel.ReadSheet(ms, keys, firstDataRow: 2);
        }
        catch
        {
            // Nguồn 571-574: catch SetFailed().AddException → Success=false.
            return Ok(new { Success = false, Messages = new[] { "File excel import không hợp lệ!" } });
        }

        // Nguồn 460-569: table null hoặc Rows.Count == 0 → "File excel import không có dữ liệu!".
        if (rows.Count == 0)
            return Ok(new { Success = false, Messages = new[] { "File excel import không có dữ liệu!" } });

        // Nguồn 462-467: table.Columns.Count != GetImportDicColums().Count (=3) → "File excel import không hợp lệ!".
        if (columnCount != keys.Length)
            return Ok(new { Success = false, Messages = new[] { "File excel import không hợp lệ!" } });

        // Nguồn 470-496: check null — thứ tự InvLevelType → InvLevelTypeName.
        // Số dòng giữ công thức nguồn `iRows = 2 + idx + 1` (idx 0-based) → Dòng 3, 4, ...
        for (var i = 0; i < rows.Count; i++)
        {
            var rowNo = 2 + i + 1;
            if (string.IsNullOrEmpty((rows[i].GetValueOrDefault("InvLevelType") ?? "").Trim()))
                return Ok(new { Success = false, Messages = new[] { $"Mã cấp kho không được trống! - Dòng {rowNo}" } });
            if (string.IsNullOrEmpty((rows[i].GetValueOrDefault("InvLevelTypeName") ?? "").Trim()))
                return Ok(new { Success = false, Messages = new[] { $"Tên cấp kho không được trống! - Dòng {rowNo}" } });
        }

        // Nguồn 498-526: check duplicate (String.Equals sau Trim — ordinal, phân biệt hoa/thường).
        for (var i = 0; i < rows.Count; i++)
        {
            var iRows = 2 + i + 1;
            var cur = (rows[i].GetValueOrDefault("InvLevelType") ?? "").Trim();
            for (var j = 0; j < rows.Count; j++)
            {
                if (i == j) continue;
                var jRows = 2 + j + 1;
                var other = (rows[j].GetValueOrDefault("InvLevelType") ?? "").Trim();
                if (string.Equals(cur, other, StringComparison.Ordinal))
                    return Ok(new { Success = false, Messages = new[] { $"Mã cấp kho '{cur}' bị lặp trong file excel! - Dòng {iRows} - {jRows}" } });
            }
        }

        // Nguồn 528-563: mỗi dòng gọi WA_Mst_InventoryLevelType_Create — KẾT QUẢ BỊ BỎ QUA (LỖI NGUỒN GIỮ NGUYÊN).
        // Nguồn 532-535: item.FlagActive = "1" (GHI ĐÈ cột "Trạng thái" trong Excel); item.OrgID = session OrgID.
        var networkID = config?["Biz_NetworkID"] ?? "1001";  // nguồn: biz dùng `nNetworkID` (cấu hình)
        var userCode = string.IsNullOrWhiteSpace(waUserCode) ? "port" : waUserCode!;

        foreach (var row in rows)
        {
            var nameRaw = row.GetValueOrDefault("InvLevelTypeName");
            var invLevelTypeRaw = row.GetValueOrDefault("InvLevelType");

            // CreateX 4501-4504: StdParam(OrgID), StdParam(InvLevelType), Trim(Name), StdFlag(FlagActive="1" do gate ép).
            var strOrgID = StdParam(orgId);
            var strInvLevelType = StdParam(invLevelTypeRaw);
            var strInvLevelTypeName = (nameRaw ?? "").Trim();
            var strFlagActive = "1";  // gate 534 ép = "1"; StdFlag("1") = "1"

            // CreateX 4512-4534: InvalidOrgID / InvalidInvLevelType → nguồn ném, gate bỏ qua im lặng.
            if (strOrgID == null) continue;
            if (strInvLevelType == null) continue;

            // CreateX 4538-4544: Mst_Org_CheckDB(strOrgID, Yes, Active) — tồn tại + FlagActive ∈ "1".
            var org = await db.Set<Mst_Org>().FirstOrDefaultAsync(x => x.OrgID == strOrgID);
            if (org == null) continue;
            if (!"1".Contains(org.FlagActive ?? "")) continue;

            // CreateX 4546-4553: Mst_InventoryLevelType_CheckDB(strOrgID, strInvLevelType, No, "")
            //   flagExist = "0" (Inactive) → nếu đã có dòng → ném _InvLevelTypeExist, gate bỏ qua im lặng.
            var exists = await db.Set<Mst_InventoryLevelType>()
                .AnyAsync(x => x.OrgID == strOrgID
                            && EF.Functions.Like((x.InvLevelType ?? "").ToLower(), PortQuery.Equal(strInvLevelType), PortQuery.Esc));
            if (exists) continue;

            // CreateX 4555-4565: InvalidInvLevelTypeName.
            if (strInvLevelTypeName.Length < 1) continue;

            // CreateX 4569-4590: SaveDB Mst_InventoryLevelType (gồm NetworkID — nguồn 4577 `drDB["NetworkID"] = nNetworkID`).
            try
            {
                db.Set<Mst_InventoryLevelType>().Add(new Mst_InventoryLevelType
                {
                    OrgID = strOrgID,
                    InvLevelType = strInvLevelType,
                    NetworkID = networkID,
                    InvLevelTypeName = strInvLevelTypeName,
                    FlagActive = strFlagActive,
                    LogLUDTimeUTC = DateTime.UtcNow,
                    LogLUBy = userCode,
                });
                await db.SaveChangesAsync();
            }
            catch
            {
                // Nguồn: WA_Create trả Error (không ném), gate không đọc → lỗi từng dòng bị bỏ qua; xoá change tracker để không sót entity.
                db.ChangeTracker.Clear();
            }
        }

        // Nguồn 561-563: Success=true "Đã nhập dữ liệu excel thành công!" (kể cả khi một số dòng bị bỏ qua — lỗi nguồn).
        return Ok(new { Success = true, Messages = new[] { "Đã nhập dữ liệu excel thành công!" } });
    }

    /// <summary>Port `TUtils.CUtils.StdParam` (nguồn — DLL không có mã): Trim + ToUpper, rỗng → null.</summary>
    private static string? StdParam(string? v)
    {
        var s = (v ?? "").Trim().ToUpperInvariant();
        return s.Length == 0 ? null : s;
    }
}

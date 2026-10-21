using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models.Port;

namespace MiniWMS.Controllers.Port;

/// <summary>
/// port 1:1 InvF_InventoryOutController.cs — DoSearch
/// Nguồn: D:\idocNet\2020.3.Skycic.Inventory\Dev\V10\idn.Skycic.Inventory.Website\Controllers\InvF_InventoryOutController.cs:317-426
/// </summary>
[ApiController]
public class InvF_InventoryOutControllerPort(AppDbContext db) : ControllerBase
{
    public class DoSearchReq
    {
        public string? IF_InvOutNo { get; set; }
        public string? InvOutType { get; set; }
        public string? CustomerCode { get; set; }
        public string? CreatedTimeFrom { get; set; }
        public string? CreatedTimeTo { get; set; }
        public string? ApprovedTimeFrom { get; set; }
        public string? ApprovedTimeTo { get; set; }
        public string? InvCodeOut { get; set; }
        public string? ProfileStatus { get; set; }
        public string? RefNo { get; set; }
        public string? Pending { get; set; }
        public string? Approved { get; set; }
        public string? Canceled { get; set; }
        public string? BusinessGroupCode { get; set; }
        public int Page { get; set; } = 0;
        public int RecordCount { get; set; } = 10;
        public string? UtcOffset { get; set; }
    }

    public class DoSearchRes
    {
        public bool Success { get; set; } = true;
        public PageInfo<InvF_InventoryOut> PageInfo { get; set; } = new(new List<InvF_InventoryOut>(), 0, 0, 0, 10);
    }

    [HttpPost("/api/invf_inventoryout/dosearch")]
    public async Task<ActionResult<DoSearchRes>> DoSearch([FromBody] DoSearchReq? req)
    {
        req ??= new DoSearchReq();
        var page = req.Page;
        var recordCount = req.RecordCount <= 0 ? 10 : req.RecordCount;

        var query = db.Set<InvF_InventoryOut>().AsQueryable();

        if (!string.IsNullOrEmpty(req.IF_InvOutNo))
        {
            var p = PortQuery.Equal(req.IF_InvOutNo);
            query = query.Where(x => EF.Functions.Like(x.IF_InvOutNo.ToLower(), p, PortQuery.Esc));
        }
        if (!string.IsNullOrEmpty(req.RefNo))
        {
            var p = PortQuery.Equal(req.RefNo);
            query = query.Where(x => EF.Functions.Like(x.RefNo!.ToLower(), p, PortQuery.Esc));
        }
        if (!string.IsNullOrEmpty(req.InvOutType))
        {
            var p = PortQuery.Equal(req.InvOutType);
            query = query.Where(x => EF.Functions.Like(x.InvOutType!.ToLower(), p, PortQuery.Esc));
        }
        if (!string.IsNullOrEmpty(req.CustomerCode))
        {
            var p = PortQuery.Equal(req.CustomerCode);
            query = query.Where(x => EF.Functions.Like(x.CustomerCode!.ToLower(), p, PortQuery.Esc));
        }
        if (!string.IsNullOrEmpty(req.InvCodeOut))
        {
            var p = PortQuery.Equal(req.InvCodeOut);
            query = query.Where(x => EF.Functions.Like(x.InvCodeOut!.ToLower(), p, PortQuery.Esc));
        }
        if (!string.IsNullOrEmpty(req.ProfileStatus))
        {
            var p = PortQuery.Equal(req.ProfileStatus);
            query = query.Where(x => EF.Functions.Like(x.ProfileStatus!.ToLower(), p, PortQuery.Esc));
        }

        var statuses = new List<string>();
        if (req.Pending == "1") statuses.Add("PENDING");
        if (req.Approved == "1") statuses.Add("APPROVE");
        if (req.Canceled == "1") statuses.Add("CANCEL");
        if (statuses.Count > 0)
        {
            query = query.Where(x => statuses.Contains(x.IF_InvOutStatus!));
        }

        var itemCount = await query.CountAsync();
        var pageCount = itemCount % recordCount == 0 ? itemCount / recordCount : itemCount / recordCount + 1;

        var skip = page * recordCount;
        var list = (itemCount <= 0 || skip < 0)
            ? new List<InvF_InventoryOut>()
            : await query.OrderByDescending(x => x.CreateDTimeUTC)
                         .Skip((int)Math.Min(skip, int.MaxValue))
                         .Take((int)Math.Min(recordCount, int.MaxValue))
                         .ToListAsync();

        foreach (var item in list)
        {
            if (item.RefType == "RO")
            {
                item.RefNo = "RO-" + item.RefNo;
            }
        }

        var pageInfo = new PageInfo<InvF_InventoryOut>(
            DataList: list,
            ItemCount: itemCount,
            PageCount: pageCount,
            PageIndex: page,
            PageSize: recordCount
        );

        return Ok(new DoSearchRes
        {
            Success = true,
            PageInfo = pageInfo
        });
    }
}

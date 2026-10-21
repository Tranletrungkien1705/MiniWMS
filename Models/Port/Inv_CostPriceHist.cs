using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// port 1:1 Inv_CostPriceHist.cs — Inv_CostPriceHist (lịch sử giá vốn).
/// Nguồn: Inv_CostPriceHistController.cs:96 `return View(new List&lt;Inv_CostPriceHist&gt;())` — action Index KHÔNG đọc/ghi bảng này,
/// chỉ trả danh sách RỖNG. Excerpt #23 dòng 156-158: "MODEL: khong thay Inv_CostPriceHist.cs ... dung MODEL lam tu dien cot"
/// ⇒ nguồn KHÔNG cho biết cột nào. Entity dưới đây chỉ là khung tối thiểu để dựng kiểu trả về (List rỗng), KHÔNG dùng để
/// đọc/ghi dữ liệu và KHÔNG tự đặt cột nghiệp vụ (xem .parity-run\conflicts\23_conflict.md).
/// </summary>
public class Inv_CostPriceHist
{
    /// <summary>Khoá kỹ thuật của khung — nguồn không có thông tin cột (GAP).</summary>
    public string Id { get; set; } = "";
}

public class Inv_CostPriceHistConfiguration : IEntityTypeConfiguration<Inv_CostPriceHist>
{
    public void Configure(EntityTypeBuilder<Inv_CostPriceHist> b)
    {
        b.ToTable("Inv_CostPriceHist");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(50);
    }
}

/// <summary>
/// Seed demo Inv_CostPriceHist (Order 200 — nghiệp vụ). Nguồn KHÔNG cho biết cột nào nên KHÔNG seed dòng dữ liệu giả;
/// seeder chỉ tồn tại để hạ tầng tạo bảng (PortInfra: bảng/cột thiếu được Seeder tự tạo).
/// </summary>
public class Inv_CostPriceHistSeeder : IPortSeeder
{
    public int Order => 200;

    public Task SeedAsync(AppDbContext db) => Task.CompletedTask;
}

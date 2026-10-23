using MiniWMS.Data;

namespace MiniWMS.Models.Port;

/// <summary>
/// #86 InvF_InventoryInController/Detail — KHÔNG cần thêm dữ liệu seed riêng.
/// Phiếu PNK001 và sản phẩm SP01/SP01-L2A/SP01-L2B (ProductCodeBase="SP01") đã có sẵn từ
/// `InvF_InventoryInMasterSeeder` (Order 35) và `Mst_ProductSeeder` (Order 14) của #24.
/// Thêm dòng mới ở đây sẽ phá các test khác đang đếm số dòng Mst_Product (vd #27 InvCostPriceHistLoadDataTests
/// `count(*) from Mst_Product = 6`) ⇒ giữ class rỗng chỉ để tuân khuôn IPortSeeder nếu cần mở rộng sau.
/// </summary>
public class InvF_InventoryInDetailSeeder : IPortSeeder
{
    public int Order => 20;

    public Task SeedAsync(AppDbContext db) => Task.CompletedTask;
}

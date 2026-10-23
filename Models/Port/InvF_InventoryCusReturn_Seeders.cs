// port 1:1 #32 InvF_InventoryCusReturnController/Index — KHÔNG có seeder toàn cục.
//
// Lý do (ghi trong TOM TAT mục GAP):
//  - Bảng Mst_Inventory và Mst_Customer là bảng DÙNG CHUNG với nhiều hạng mục khác đã seed sẵn
//    (Mst_InventorySeeder Order=12 → KHO01..KHO03; InvF_InventoryInMasterSeeder Order=35 → NCC01..NCC03, KH01).
//  - Thêm dòng vào 2 bảng này trong seeder toàn cục làm sai kỳ vọng số đếm của các test đã có
//    (InvFMoveOrdIndexTests, InvCostPriceHistIndexTests, InvFInventoryIn*Tests, DashboardRpt*PivotTests).
//    Đây là hồi quy thực tế đã gặp khi chạy `dotnet test` lần đầu.
//  - Vì vậy #32 KHÔNG đăng ký IPortSeeder toàn cục. Test riêng của hạng mục tự đếm bằng SQL số dòng
//    thỏa where rồi so với response (đề bài P2 cho phép: "agent PHAI dem bang SQL va ghi ro cong thuc +
//    con so thuc trong VERIFY"). Bộ lọc vẫn được chứng minh có tác dụng bằng cách so với tổng số dòng
//    của bảng (thỏa < tổng) và so 2 bộ lọc với nhau.

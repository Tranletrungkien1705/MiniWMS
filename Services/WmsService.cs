using Microsoft.EntityFrameworkCore;
using MiniWMS.Data;
using MiniWMS.Models;

namespace MiniWMS.Services;

public record BalanceRow(int WarehouseId, string Warehouse, int ProductId, string ProductCode, string ProductName, string Uom, int Qty, int MinStock);
public record WmsDash(int Warehouses, int Products, int PostedDocs, int DraftDocs, int TotalOnHand, int LowStock, int PendingAudits, int PendingMoveOrders, int PendingReturns, int PendingCustomerReturns, int ExpiringLots = 0, int StagnantItems = 0, int DamagedSerials = 0, int TotalBlocks = 0, int TotalCostPrices = 0, int ClosedPeriods = 0, int TotalCartons = 0, int TotalBoxes = 0, int PendingInFGs = 0, int PendingOutFGs = 0, int TotalPartTypes = 0, int TotalInventoryTypes = 0, int TotalInventoryLevelTypes = 0, int TotalUserMapInventories = 0, int TotalProductGroups = 0);


public interface IWmsService
{
    Task<List<Warehouse>> WarehousesAsync();
    Task<List<Product>> ProductsAsync();
    Task<int> CreateWarehouseAsync(Warehouse w);
    Task<int> CreateProductAsync(Product p);
    Task<List<StockDoc>> DocsAsync(DocType? type, DocStatus? status);
    Task<StockDoc?> GetDocAsync(int id);
    Task<int> CreateDocAsync(StockDoc doc, List<(int productId, int qty)> lines);
    Task<(bool ok, string msg)> PostDocAsync(int id);
    Task CancelDocAsync(int id);
    Task<List<StockAudit>> AuditsAsync(int? warehouseId, StockAuditStatus? status);
    Task<StockAudit?> GetAuditAsync(int id);
    Task<int> CreateAuditAsync(StockAudit audit, List<(int productId, int qtyInit, int qtyActual, string? note)> lines);
    Task<(bool ok, string msg)> BalanceAuditAsync(int id);
    Task CancelAuditAsync(int id);
    Task<List<MoveOrder>> MoveOrdersAsync(int? fromWhId, int? toWhId, MoveOrderStatus? status);
    Task<MoveOrder?> GetMoveOrderAsync(int id);
    Task<int> CreateMoveOrderAsync(MoveOrder order, List<(int productId, int qty, string? note)> lines);
    Task<(bool ok, string msg)> ApproveMoveOrderAsync(int id);
    Task<(bool ok, string msg)> ExecuteMoveOrderAsync(int id);
    Task CancelMoveOrderAsync(int id);
    Task<List<ReturnToSupplier>> ReturnToSuppliersAsync(int? warehouseId, ReturnSupStatus? status);
    Task<ReturnToSupplier?> GetReturnToSupplierAsync(int id);
    Task<int> CreateReturnToSupplierAsync(ReturnToSupplier returnDoc, List<(int productId, int qty, decimal unitPrice, string? note)> lines);
    Task<(bool ok, string msg)> ApproveReturnToSupplierAsync(int id);
    Task CancelReturnToSupplierAsync(int id);
    Task<List<CustomerReturn>> CustomerReturnsAsync(int? warehouseId, CusReturnStatus? status);
    Task<CustomerReturn?> GetCustomerReturnAsync(int id);
    Task<int> CreateCustomerReturnAsync(CustomerReturn returnDoc, List<(int productId, int qty, decimal unitPrice, string? note)> lines);
    Task<(bool ok, string msg)> ApproveCustomerReturnAsync(int id);
    Task CancelCustomerReturnAsync(int id);
    Task<List<BalanceRow>> BalancesAsync(int? warehouseId);
    Task<WarehouseCardReport> WarehouseCardAsync(int productId, int? warehouseId, DateTime? fromDate, DateTime? toDate);
    Task<InventoryInOutReport> InventoryInOutReportAsync(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? keyword);
    Task<StockMinimumReport> StockMinimumReportAsync(int? warehouseId, bool onlyBelowMin = true, string? keyword = null);
    Task<StockLotExpiryReport> StockLotExpiryReportAsync(int? warehouseId, LotExpiryStatus? status, string? keyword);
    Task<StorageTimeReport> StorageTimeReportAsync(int? warehouseId, StorageTimeAgingBracket? bracket, string? keyword, DateTime? asOfDate = null);
    Task<List<StockLot>> StockLotsAsync(int? warehouseId, int? productId);
    Task<StockSerialReport> StockSerialReportAsync(int? warehouseId, int? productId, StockSerialStatus? status, string? keyword);
    Task<List<StockSerial>> StockSerialsAsync(int? warehouseId, int? productId, StockSerialStatus? status);
    Task<StockSerial?> GetStockSerialAsync(int id);
    Task<int> CreateStockSerialAsync(StockSerial serial);
    Task<(bool ok, string msg)> ChangeStockSerialStatusAsync(int id, StockSerialStatus newStatus, string? note);
    Task<InventoryBlockReport> InventoryBlockReportAsync(int? warehouseId, string? shelfCode, bool? activeFilter, string? keyword);
    Task<List<InventoryBlock>> InventoryBlocksAsync(int? warehouseId, string? shelfCode);
    Task<InventoryBlock?> GetInventoryBlockAsync(int id);
    Task<int> CreateInventoryBlockAsync(InventoryBlock block);
    Task<(bool ok, string msg)> UpdateInventoryBlockAsync(int id, InventoryBlock block);
    Task<(bool ok, string msg)> ToggleInventoryBlockStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteInventoryBlockAsync(int id);
    Task<List<string>> GetShelvesAsync(int? warehouseId);
    Task<CostPriceHistReport> CostPriceHistReportAsync(int? warehouseId, int? productId, bool? currentOnly, DateTime? fromDate, DateTime? toDate, string? keyword);
    Task<CostPriceHist?> GetCostPriceHistAsync(int id);
    Task<int> CreateCostPriceHistAsync(CostPriceHist item);
    Task<(bool ok, string msg)> UpdateCostPriceHistAsync(int id, decimal costPrice, string? remark);
    Task<CostPriceCalcPreviewReport> PreviewCalculateCostPriceAsync(int? warehouseId, DateTime fromDate, DateTime toDate, string calcPeriodName, int[]? productIds);
    Task<(bool ok, string msg, int count)> ApplyCalculateCostPriceAsync(int? warehouseId, DateTime effectDate, string calcPeriodName, List<(int ProductId, decimal NewCostPrice, string Note)> items);
    Task<List<PeriodClosing>> PeriodClosingsAsync(int? warehouseId, PeriodClosingStatus? status, int? year);
    Task<PeriodClosing?> GetPeriodClosingAsync(int id);
    Task<PeriodClosingPreviewReport> PreviewPeriodClosingAsync(int? warehouseId, int year, int month);
    Task<(bool ok, string msg, int id)> CreateAndClosePeriodAsync(int? warehouseId, int year, int month, string? note, string closedBy);
    Task<(bool ok, string msg)> ReopenPeriodClosingAsync(int id, string reason);
    Task<(bool ok, string msg)> CancelPeriodClosingAsync(int id);
    Task<CartonReport> CartonsAsync(int? warehouseId, int? productId, CartonStatus? status, string? q);
    Task<InventoryCarton?> GetCartonAsync(int id);
    Task<int> CreateCartonAsync(InventoryCarton carton);
    Task<(bool ok, string msg, List<int> ids)> GenerateCartonsBatchAsync(int warehouseId, string cartonType, int count, double length, double width, double height, int capacity, string? shelfLocation, string? prefix);
    Task<(bool ok, string msg)> PackCartonAsync(int id, int productId, int quantity, string? lotNo, double grossWeightKg, string? packerName, string? note);
    Task<(bool ok, string msg)> SealCartonAsync(int id);
    Task<(bool ok, string msg)> UnpackCartonAsync(int id, string? reason);
    Task<(bool ok, string msg)> ShipCartonAsync(int id, string refDocNo);
    Task<(bool ok, string msg)> DeleteCartonAsync(int id);
    Task<BoxReport> BoxesAsync(int? warehouseId, int? productId, int? cartonId, BoxStatus? status, bool? flagMap, string? q);
    Task<InventoryBox?> GetBoxAsync(int id);
    Task<int> CreateBoxAsync(InventoryBox box);
    Task<(bool ok, string msg, List<int> ids)> GenerateBoxesBatchAsync(int warehouseId, string boxType, int count, double length, double width, double height, int capacity, int? cartonId, string? shelfLocation, string? prefix);
    Task<(bool ok, string msg)> PackBoxAsync(int id, int productId, int quantity, string? lotNo, double grossWeightKg, string? packerName, string? secretNo, string? note);
    Task<(bool ok, string msg)> SealBoxAsync(int id, string? secretNo);
    Task<(bool ok, string msg)> MapBoxToCartonAsync(int boxId, int cartonId);
    Task<(bool ok, string msg)> UnmapBoxFromCartonAsync(int boxId);
    Task<(bool ok, string msg)> UnpackBoxAsync(int id, string? reason);
    Task<(bool ok, string msg)> ShipBoxAsync(int id, string refDocNo);
    Task<(bool ok, string msg)> DeleteBoxAsync(int id);
    Task<List<InventoryCarton>> AvailableCartonsAsync(int warehouseId);
    Task<InventoryInFGReport> InventoryInFGsAsync(int? warehouseId, InvInFGStatus? status, InvInFGFormType? formType, DateTime? fromDate, DateTime? toDate, string? q);
    Task<InventoryInFG?> GetInventoryInFGAsync(int id);
    Task<int> CreateInventoryInFGAsync(InventoryInFG doc, List<(int productId, int planQty, int actualQty, int defectQty, decimal unitCost, DateTime? prodDate, string? note)> lines, List<(int productId, string serialNo, string? note)> serials);
    Task<(bool ok, string msg)> ApproveInventoryInFGAsync(int id);
    Task<(bool ok, string msg)> CancelInventoryInFGAsync(int id);
    Task<InventoryOutFGReport> InventoryOutFGsAsync(int? warehouseId, InvOutFGStatus? status, InvOutFGType? outType, InvOutFGFormType? formType, DateTime? fromDate, DateTime? toDate, string? q);
    Task<InventoryOutFG?> GetInventoryOutFGAsync(int id);
    Task<int> CreateInventoryOutFGAsync(InventoryOutFG doc, List<(int productId, int qty, decimal unitPrice, decimal unitCost, string? note)> lines, List<(int productId, string serialNo, string? note)> serials);
    Task<(bool ok, string msg)> ApproveInventoryOutFGAsync(int id);
    Task<(bool ok, string msg)> CancelInventoryOutFGAsync(int id);
    Task<SummaryInReturnSupReport> SummaryInReturnSupReportAsync(int? warehouseId, string? supplierCode, DateTime? fromDate, DateTime? toDate, string? keyword);
    Task<InventoryOutDtlReport> InventoryOutDtlReportAsync(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? outType, string? keyword);
    Task<InventoryInDtlReport> InventoryInDtlReportAsync(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? inType, string? keyword);
    Task<MonthlyMatrixReport> MonthlyMatrixReportAsync(int year, int? warehouseId, string? viewMode, string? keyword);
    Task<StockExtendReport> StockExtendReportAsync(int? warehouseId, StockExtendStatus? statusFilter, string? keyword);
    Task<InventoryValuationReport> InventoryValuationReportAsync(int? warehouseId, InventoryValuationAbcClass? abcClass, bool onlyHasStock = true, string? keyword = null, DateTime? asOfDate = null);
    Task<List<Supplier>> SuppliersAsync(string? q = null, bool? activeOnly = null);
    Task<Supplier?> GetSupplierAsync(int id);
    Task<int> CreateSupplierAsync(Supplier supplier);
    Task<(bool ok, string msg)> UpdateSupplierAsync(int id, Supplier supplier);
    Task<(bool ok, string msg)> ToggleSupplierStatusAsync(int id);
    Task<List<Customer>> CustomersAsync(string? q = null, string? customerType = null, bool? activeOnly = null, string? customerGrpCode = null, string? customerSourceCode = null);
    Task<Customer?> GetCustomerAsync(int id);
    Task<Customer?> GetCustomerByCodeAsync(string code);
    Task<int> CreateCustomerAsync(Customer customer);
    Task<(bool ok, string msg)> UpdateCustomerAsync(int id, Customer customer);
    Task<(bool ok, string msg)> ToggleCustomerStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteCustomerAsync(int id);
    Task<CustomerDetailDto?> GetCustomerDetailAsync(int id);
    Task<PartTypeReport> PartTypesReportAsync(string? q = null, bool? activeOnly = null);
    Task<List<PartType>> PartTypesAsync(string? q = null, bool? activeOnly = null);
    Task<PartType?> GetPartTypeAsync(int id);
    Task<PartType?> GetPartTypeByCodeAsync(string code);
    Task<PartTypeDetailDto?> GetPartTypeDetailAsync(int id);
    Task<int> CreatePartTypeAsync(PartType item);
    Task<(bool ok, string msg)> UpdatePartTypeAsync(int id, PartType item);
    Task<(bool ok, string msg)> TogglePartTypeStatusAsync(int id);
    Task<(bool ok, string msg)> DeletePartTypeAsync(int id);
    Task<BrandReport> BrandsReportAsync(string? q = null, bool? activeOnly = null);
    Task<List<Brand>> BrandsAsync(string? q = null, bool? activeOnly = null);
    Task<Brand?> GetBrandAsync(int id);
    Task<Brand?> GetBrandByCodeAsync(string code);
    Task<BrandDetailDto?> GetBrandDetailAsync(int id);
    Task<int> CreateBrandAsync(Brand item);
    Task<(bool ok, string msg)> UpdateBrandAsync(int id, Brand item);
    Task<(bool ok, string msg)> ToggleBrandStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteBrandAsync(int id);
    Task<PartUnitReport> PartUnitsReportAsync(string? q = null, bool? activeOnly = null, bool? standardOnly = null);
    Task<List<PartUnit>> PartUnitsAsync(string? q = null, bool? activeOnly = null);
    Task<PartUnit?> GetPartUnitAsync(int id);
    Task<PartUnit?> GetPartUnitByCodeAsync(string code);
    Task<PartUnitDetailDto?> GetPartUnitDetailAsync(int id);
    Task<int> CreatePartUnitAsync(PartUnit item);
    Task<(bool ok, string msg)> UpdatePartUnitAsync(int id, PartUnit item);
    Task<(bool ok, string msg)> TogglePartUnitStatusAsync(int id);
    Task<(bool ok, string msg)> DeletePartUnitAsync(int id);
    Task<PartMaterialTypeReport> PartMaterialTypesReportAsync(string? q = null, bool? activeOnly = null);
    Task<List<PartMaterialType>> PartMaterialTypesAsync(string? q = null, bool? activeOnly = null);
    Task<PartMaterialType?> GetPartMaterialTypeAsync(int id);
    Task<PartMaterialType?> GetPartMaterialTypeByCodeAsync(string code);
    Task<PartMaterialTypeDetailDto?> GetPartMaterialTypeDetailAsync(int id);
    Task<int> CreatePartMaterialTypeAsync(PartMaterialType item);
    Task<(bool ok, string msg)> UpdatePartMaterialTypeAsync(int id, PartMaterialType item);
    Task<(bool ok, string msg)> TogglePartMaterialTypeStatusAsync(int id);
    Task<(bool ok, string msg)> DeletePartMaterialTypeAsync(int id);
    Task<ProductModelReport> ProductModelsReportAsync(string? q = null, string? brandCode = null, bool? activeOnly = null);
    Task<List<ProductModel>> ProductModelsAsync(string? q = null, string? brandCode = null, bool? activeOnly = null);
    Task<ProductModel?> GetProductModelAsync(int id);
    Task<ProductModel?> GetProductModelByCodeAsync(string code);
    Task<ProductModelDetailDto?> GetProductModelDetailAsync(int id);
    Task<int> CreateProductModelAsync(ProductModel item);
    Task<(bool ok, string msg)> UpdateProductModelAsync(int id, ProductModel item);
    Task<(bool ok, string msg)> ToggleProductModelStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteProductModelAsync(int id);
    Task<InventoryTypeReport> InventoryTypesReportAsync(string? q = null, bool? activeOnly = null);
    Task<List<InventoryType>> InventoryTypesAsync(string? q = null, bool? activeOnly = null);
    Task<InventoryType?> GetInventoryTypeAsync(int id);
    Task<InventoryType?> GetInventoryTypeByCodeAsync(string code);
    Task<InventoryTypeDetailDto?> GetInventoryTypeDetailAsync(int id);
    Task<int> CreateInventoryTypeAsync(InventoryType item);
    Task<(bool ok, string msg)> UpdateInventoryTypeAsync(int id, InventoryType item);
    Task<(bool ok, string msg)> ToggleInventoryTypeStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteInventoryTypeAsync(int id);
    Task<InventoryLevelTypeReport> InventoryLevelTypesReportAsync(string? q = null, bool? activeOnly = null);
    Task<List<InventoryLevelType>> InventoryLevelTypesAsync(string? q = null, bool? activeOnly = null);
    Task<InventoryLevelType?> GetInventoryLevelTypeAsync(int id);
    Task<InventoryLevelType?> GetInventoryLevelTypeByCodeAsync(string code);
    Task<InventoryLevelTypeDetailDto?> GetInventoryLevelTypeDetailAsync(int id);
    Task<int> CreateInventoryLevelTypeAsync(InventoryLevelType item);
    Task<(bool ok, string msg)> UpdateInventoryLevelTypeAsync(int id, InventoryLevelType item);
    Task<(bool ok, string msg)> ToggleInventoryLevelTypeStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteInventoryLevelTypeAsync(int id);
    Task<InventoryInTypeReport> InventoryInTypesReportAsync(string? q = null, bool? activeOnly = null, bool? statisticOnly = null);
    Task<List<InventoryInType>> InventoryInTypesAsync(string? q = null, bool? activeOnly = null, bool? statisticOnly = null);
    Task<InventoryInType?> GetInventoryInTypeAsync(int id);
    Task<InventoryInType?> GetInventoryInTypeByCodeAsync(string code);
    Task<InventoryInTypeDetailDto?> GetInventoryInTypeDetailAsync(int id);
    Task<int> CreateInventoryInTypeAsync(InventoryInType item);
    Task<(bool ok, string msg)> UpdateInventoryInTypeAsync(int id, InventoryInType item);
    Task<(bool ok, string msg)> ToggleInventoryInTypeStatusAsync(int id);
    Task<(bool ok, string msg)> ToggleInventoryInTypeStatisticAsync(int id);
    Task<(bool ok, string msg)> DeleteInventoryInTypeAsync(int id);
    Task<InventoryOutTypeReport> InventoryOutTypesReportAsync(string? q = null, bool? activeOnly = null, bool? statisticOnly = null);
    Task<List<InventoryOutType>> InventoryOutTypesAsync(string? q = null, bool? activeOnly = null, bool? statisticOnly = null);
    Task<InventoryOutType?> GetInventoryOutTypeAsync(int id);
    Task<InventoryOutType?> GetInventoryOutTypeByCodeAsync(string code);
    Task<InventoryOutTypeDetailDto?> GetInventoryOutTypeDetailAsync(int id);
    Task<int> CreateInventoryOutTypeAsync(InventoryOutType item);
    Task<(bool ok, string msg)> UpdateInventoryOutTypeAsync(int id, InventoryOutType item);
    Task<(bool ok, string msg)> ToggleInventoryOutTypeStatusAsync(int id);
    Task<(bool ok, string msg)> ToggleInventoryOutTypeStatisticAsync(int id);
    Task<(bool ok, string msg)> DeleteInventoryOutTypeAsync(int id);
    Task<UserMapInventoryReport> UserMapInventoriesReportAsync(int? warehouseId = null, string? userRole = null, bool? activeOnly = null, string? q = null);
    Task<List<UserMapInventory>> UserMapInventoriesAsync(int? warehouseId = null, bool? activeOnly = null);
    Task<UserMapInventory?> GetUserMapInventoryAsync(int id);
    Task<List<UserMapInventory>> GetUserMapsByWarehouseAsync(int warehouseId);
    Task<List<UserMapInventory>> GetUserMapsByUserCodeAsync(string userCode);
    Task<List<WarehouseAssignmentSummary>> GetWarehouseAssignmentSummariesAsync();
    Task<int> CreateUserMapInventoryAsync(UserMapInventory item);
    Task<(bool ok, string msg)> UpdateUserMapInventoryAsync(int id, UserMapInventory item);
    Task<(bool ok, string msg)> ToggleUserMapInventoryStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteUserMapInventoryAsync(int id);
    Task<(bool ok, string msg, int count)> BatchMapUsersToWarehouseAsync(int warehouseId, List<BatchMapUserItemDto> users, string assignedBy);
    Task<ProductGroupReport> ProductGroupsReportAsync(string? q = null, string? parentCode = null, string? brandCode = null, bool? activeOnly = null);
    Task<List<ProductGroup>> ProductGroupsAsync(string? q = null, bool? activeOnly = null);
    Task<ProductGroup?> GetProductGroupAsync(int id);
    Task<ProductGroup?> GetProductGroupByCodeAsync(string code);
    Task<ProductGroupDetailDto?> GetProductGroupDetailAsync(int id);
    Task<int> CreateProductGroupAsync(ProductGroup item);
    Task<(bool ok, string msg)> UpdateProductGroupAsync(int id, ProductGroup item);
    Task<(bool ok, string msg)> ToggleProductGroupStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteProductGroupAsync(int id);
    Task<AreaReport> AreasReportAsync(string? q = null, string? parentCode = null, bool? activeOnly = null);
    Task<List<Area>> AreasAsync(string? q = null, bool? activeOnly = null);
    Task<Area?> GetAreaAsync(int id);
    Task<Area?> GetAreaByCodeAsync(string code);
    Task<AreaDetailDto?> GetAreaDetailAsync(int id);
    Task<int> CreateAreaAsync(Area item);
    Task<(bool ok, string msg)> UpdateAreaAsync(int id, Area item);
    Task<(bool ok, string msg)> ToggleAreaStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteAreaAsync(int id);
    Task<CustomerGroupReport> CustomerGroupsReportAsync(string? q = null, string? parentCode = null, bool? activeOnly = null);
    Task<List<CustomerGroup>> CustomerGroupsAsync(string? q = null, bool? activeOnly = null);
    Task<CustomerGroup?> GetCustomerGroupAsync(int id);
    Task<CustomerGroup?> GetCustomerGroupByCodeAsync(string code);
    Task<CustomerGroupDetailDto?> GetCustomerGroupDetailAsync(int id);
    Task<int> CreateCustomerGroupAsync(CustomerGroup item);
    Task<(bool ok, string msg)> UpdateCustomerGroupAsync(int id, CustomerGroup item);
    Task<(bool ok, string msg)> ToggleCustomerGroupStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteCustomerGroupAsync(int id);
    Task<DepartmentReport> DepartmentsReportAsync(string? q = null, string? parentCode = null, int? level = null, bool? activeOnly = null);
    Task<List<Department>> DepartmentsAsync(string? q = null, bool? activeOnly = null);
    Task<Department?> GetDepartmentAsync(int id);
    Task<Department?> GetDepartmentByCodeAsync(string code);
    Task<DepartmentDetailDto?> GetDepartmentDetailAsync(int id);
    Task<int> CreateDepartmentAsync(Department item);
    Task<(bool ok, string msg)> UpdateDepartmentAsync(int id, Department item);
    Task<(bool ok, string msg)> ToggleDepartmentStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteDepartmentAsync(int id);
    Task<CustomerSourceReport> CustomerSourcesReportAsync(string? q = null, string? parentCode = null, bool? activeOnly = null);
    Task<List<CustomerSource>> CustomerSourcesAsync(string? q = null, bool? activeOnly = null);
    Task<CustomerSource?> GetCustomerSourceAsync(int id);
    Task<CustomerSource?> GetCustomerSourceByCodeAsync(string code);
    Task<CustomerSourceDetailDto?> GetCustomerSourceDetailAsync(int id);
    Task<int> CreateCustomerSourceAsync(CustomerSource item);
    Task<(bool ok, string msg)> UpdateCustomerSourceAsync(int id, CustomerSource item);
    Task<(bool ok, string msg)> ToggleCustomerSourceStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteCustomerSourceAsync(int id);
    Task<MoveOrdTypeReport> MoveOrdTypesReportAsync(string? q = null, bool? activeOnly = null, bool? urgentOnly = null);
    Task<List<MoveOrdType>> MoveOrdTypesAsync(string? q = null, bool? activeOnly = null);
    Task<MoveOrdType?> GetMoveOrdTypeAsync(int id);
    Task<MoveOrdType?> GetMoveOrdTypeByCodeAsync(string code);
    Task<MoveOrdTypeDetailDto?> GetMoveOrdTypeDetailAsync(int id);
    Task<int> CreateMoveOrdTypeAsync(MoveOrdType item);
    Task<(bool ok, string msg)> UpdateMoveOrdTypeAsync(int id, MoveOrdType item);
    Task<(bool ok, string msg)> ToggleMoveOrdTypeStatusAsync(int id);
    Task<(bool ok, string msg)> ToggleMoveOrdTypeUrgentAsync(int id);
    Task<(bool ok, string msg)> DeleteMoveOrdTypeAsync(int id);
    Task<DealerReport> DealersReportAsync(string? q = null, int? level = null, string? province = null, bool? activeOnly = null);
    Task<List<Dealer>> DealersAsync(string? q = null, bool? activeOnly = null);
    Task<Dealer?> GetDealerAsync(int id);
    Task<Dealer?> GetDealerByCodeAsync(string code);
    Task<DealerDetailDto?> GetDealerDetailAsync(int id);
    Task<int> CreateDealerAsync(Dealer item);
    Task<(bool ok, string msg)> UpdateDealerAsync(int id, Dealer item);
    Task<(bool ok, string msg)> ToggleDealerStatusAsync(int id);
    Task<(bool ok, string msg)> DeleteDealerAsync(int id);
    Task<MapDeliveryOrderReport> MapDeliveryOrderReportAsync(int? warehouseId, string? areaCode, string? customerCode, string? status, DateTime? fromDate, DateTime? toDate, string? keyword);
    Task<List<TempPrintType>> TempPrintTypesAsync(bool? activeOnly = null);
    Task<List<TempPrint>> TempPrintsAsync(string? typeCode = null, bool? activeOnly = null);
    Task<TempPrintReport> TempPrintsReportAsync(string? q = null, string? typeCode = null, bool? activeOnly = null);
    Task<TempPrint?> GetTempPrintAsync(int id);
    Task<TempPrint?> GetTempPrintByCodeAsync(string code);
    Task<TempPrint?> GetDefaultTempPrintByTypeAsync(string typeCode);
    Task<TempPrintPreviewResult?> PreviewTempPrintAsync(int id);
    Task<int> CreateTempPrintAsync(TempPrint item);
    Task<(bool ok, string msg)> UpdateTempPrintAsync(int id, TempPrint item);
    Task<(bool ok, string msg)> ToggleTempPrintStatusAsync(int id);
    Task<(bool ok, string msg)> SetDefaultTempPrintAsync(int id);
    Task<(bool ok, string msg)> DeleteTempPrintAsync(int id);
    Task<int> CreateTempPrintTypeAsync(TempPrintType item);
    Task<(bool ok, string msg)> UpdateTempPrintTypeAsync(int id, TempPrintType item);
    Task<(bool ok, string msg)> DeleteTempPrintTypeAsync(int id);
    Task<SummaryInOutPartnerPivotReport> SummaryInOutPartnerPivotReportAsync(int? warehouseId, string? partnerCode, string? productGrpCode, int? productId, string? actionType, DateTime? fromDate, DateTime? toDate, string? keyword);
    Task<WmsDash> DashboardAsync();
}

public class WmsService(AppDbContext db) : IWmsService
{
    public Task<List<Warehouse>> WarehousesAsync() => db.Warehouses.OrderBy(w => w.Code).ToListAsync();
    public Task<List<Product>> ProductsAsync() => db.Products.OrderBy(p => p.Code).ToListAsync();

    public async Task<int> CreateWarehouseAsync(Warehouse w)
    {
        if (string.IsNullOrWhiteSpace(w.Code)) w.Code = $"KHO{await db.Warehouses.CountAsync() + 1:D2}";
        db.Warehouses.Add(w); await db.SaveChangesAsync(); return w.Id;
    }
    public async Task<int> CreateProductAsync(Product p)
    {
        if (string.IsNullOrWhiteSpace(p.Code)) p.Code = $"SP{await db.Products.CountAsync() + 1:D4}";
        p.Code = p.Code.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(p.PartTypeCode)) p.PartTypeCode = p.PartTypeCode.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(p.BrandCode)) p.BrandCode = p.BrandCode.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(p.PMType)) p.PMType = p.PMType.Trim().ToUpperInvariant();
        db.Products.Add(p); await db.SaveChangesAsync(); return p.Id;
    }

    public async Task<List<StockDoc>> DocsAsync(DocType? type, DocStatus? status)
    {
        var q = db.Docs.Include(d => d.FromWarehouse).Include(d => d.ToWarehouse).Include(d => d.Lines).AsQueryable();
        if (type.HasValue) q = q.Where(d => d.Type == type.Value);
        if (status.HasValue) q = q.Where(d => d.Status == status.Value);
        var list = await q.ToListAsync();
        return list.OrderByDescending(d => d.CreatedAt).ToList();
    }

    public Task<StockDoc?> GetDocAsync(int id) =>
        db.Docs.Include(d => d.FromWarehouse).Include(d => d.ToWarehouse).Include(d => d.Lines).ThenInclude(l => l.Product)
          .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<int> CreateDocAsync(StockDoc doc, List<(int productId, int qty)> lines)
    {
        doc.Code = $"{Prefix(doc.Type)}{DateTime.Now:yyMMdd}-{await db.Docs.CountAsync() + 1:D3}";
        doc.Status = DocStatus.Draft;
        foreach (var (pid, qty) in lines.Where(l => l.productId > 0 && l.qty != 0))
            doc.Lines.Add(new StockDocLine { ProductId = pid, Quantity = qty });
        db.Docs.Add(doc);
        await db.SaveChangesAsync();
        return doc.Id;
    }

    public async Task<(bool ok, string msg)> PostDocAsync(int id)
    {
        var doc = await db.Docs.Include(d => d.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return (false, "Không tìm thấy phiếu.");
        if (doc.Status != DocStatus.Draft) return (false, "Phiếu không ở trạng thái Nháp.");
        if (doc.Lines.Count == 0) return (false, "Phiếu chưa có dòng hàng.");

        // Kiểm tra tồn đủ khi Xuất/Chuyển
        if (doc.Type is DocType.Out or DocType.Transfer && doc.FromWarehouseId is { } fromWh)
        {
            var bal = await BalancesAsync(fromWh);
            foreach (var l in doc.Lines)
            {
                var have = bal.FirstOrDefault(x => x.ProductId == l.ProductId)?.Qty ?? 0;
                if (l.Quantity > have) return (false, $"Không đủ tồn: {l.Product.Name} cần {l.Quantity}, còn {have}.");
            }
        }
        doc.Status = DocStatus.Posted;
        await db.SaveChangesAsync();
        return (true, $"Đã ghi sổ phiếu {doc.Code}.");
    }

    public async Task CancelDocAsync(int id)
    {
        var doc = await db.Docs.FirstOrDefaultAsync(d => d.Id == id) ?? throw new KeyNotFoundException();
        doc.Status = DocStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    public async Task<List<StockAudit>> AuditsAsync(int? warehouseId, StockAuditStatus? status)
    {
        var q = db.Audits.Include(a => a.Warehouse).Include(a => a.Lines).ThenInclude(l => l.Product).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(a => a.WarehouseId == warehouseId.Value);
        if (status.HasValue) q = q.Where(a => a.Status == status.Value);
        var list = await q.ToListAsync();
        return list.OrderByDescending(a => a.CreatedAt).ToList();
    }

    public Task<StockAudit?> GetAuditAsync(int id) =>
        db.Audits.Include(a => a.Warehouse)
                 .Include(a => a.InDoc)
                 .Include(a => a.OutDoc)
                 .Include(a => a.Lines).ThenInclude(l => l.Product)
                 .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<int> CreateAuditAsync(StockAudit audit, List<(int productId, int qtyInit, int qtyActual, string? note)> lines)
    {
        audit.Code = $"KK{DateTime.Now:yyMMdd}-{await db.Audits.CountAsync() + 1:D3}";
        audit.Status = StockAuditStatus.Draft;
        foreach (var (pid, qInit, qAct, note) in lines.Where(l => l.productId > 0))
            audit.Lines.Add(new StockAuditLine { ProductId = pid, QtyInit = qInit, QtyActual = qAct, Note = note });
        db.Audits.Add(audit);
        await db.SaveChangesAsync();
        return audit.Id;
    }

    public async Task<(bool ok, string msg)> BalanceAuditAsync(int id)
    {
        var audit = await db.Audits.Include(a => a.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(a => a.Id == id);
        if (audit == null) return (false, "Không tìm thấy phiếu kiểm kê.");
        if (audit.Status != StockAuditStatus.Draft) return (false, "Phiếu kiểm kê không ở trạng thái Đang kiểm kê.");
        if (audit.Lines.Count == 0) return (false, "Phiếu kiểm kê chưa có mặt hàng.");

        var excessLines = audit.Lines.Where(l => l.QtyActual > l.QtyInit).ToList();
        var shortageLines = audit.Lines.Where(l => l.QtyActual < l.QtyInit).ToList();

        if (excessLines.Count == 0 && shortageLines.Count == 0)
        {
            audit.Status = StockAuditStatus.Finished;
            audit.FinishedAt = DateTime.Now;
            await db.SaveChangesAsync();
            return (true, $"Phiếu {audit.Code} khớp tồn sổ sách. Đã đánh dấu hoàn tất kiểm kê.");
        }

        // Tự động sinh phiếu nhập kho nếu có hàng thừa sau kiểm kê
        if (excessLines.Count > 0)
        {
            var inLines = excessLines.Select(l => (l.ProductId, l.QtyActual - l.QtyInit)).ToList();
            var inDoc = new StockDoc
            {
                Type = DocType.In,
                ToWarehouseId = audit.WarehouseId,
                RefNo = audit.Code,
                Note = $"Tự động điều chỉnh thừa sau kiểm kê {audit.Code}",
                CreatedBy = string.IsNullOrWhiteSpace(audit.CreatedBy) ? "audit-balance" : audit.CreatedBy
            };
            var inDocId = await CreateDocAsync(inDoc, inLines);
            var (postOk, postMsg) = await PostDocAsync(inDocId);
            if (!postOk) return (false, $"Lỗi ghi sổ phiếu nhập thừa: {postMsg}");
            audit.InDocId = inDocId;
        }

        // Tự động sinh phiếu xuất kho nếu có hàng thiếu sau kiểm kê
        if (shortageLines.Count > 0)
        {
            var outLines = shortageLines.Select(l => (l.ProductId, l.QtyInit - l.QtyActual)).ToList();
            var outDoc = new StockDoc
            {
                Type = DocType.Out,
                FromWarehouseId = audit.WarehouseId,
                RefNo = audit.Code,
                Note = $"Tự động điều chỉnh thiếu sau kiểm kê {audit.Code}",
                CreatedBy = string.IsNullOrWhiteSpace(audit.CreatedBy) ? "audit-balance" : audit.CreatedBy
            };
            var outDocId = await CreateDocAsync(outDoc, outLines);
            var (postOk, postMsg) = await PostDocAsync(outDocId);
            if (!postOk) return (false, $"Lỗi ghi sổ phiếu xuất thiếu: {postMsg}");
            audit.OutDocId = outDocId;
        }

        audit.Status = StockAuditStatus.Finished;
        audit.FinishedAt = DateTime.Now;
        await db.SaveChangesAsync();
        return (true, $"Đã cân bằng kho cho phiếu {audit.Code}. Tồn kho đã khớp số lượng thực tế kiểm kê.");
    }

    public async Task CancelAuditAsync(int id)
    {
        var audit = await db.Audits.FirstOrDefaultAsync(a => a.Id == id) ?? throw new KeyNotFoundException();
        if (audit.Status == StockAuditStatus.Finished)
            throw new InvalidOperationException("Không thể hủy phiếu kiểm kê đã cân bằng.");
        audit.Status = StockAuditStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    /// <summary>Tồn kho tính từ các phiếu ĐÃ GHI SỔ (In:+To, Out:-From, Transfer:-From+To).</summary>
    public async Task<List<BalanceRow>> BalancesAsync(int? warehouseId)
    {
        var docs = await db.Docs.Where(d => d.Status == DocStatus.Posted).Include(d => d.Lines).ThenInclude(l => l.Product).ToListAsync();
        var whs = await db.Warehouses.ToDictionaryAsync(w => w.Id, w => w);
        var map = new Dictionary<(int wh, int pid), int>();
        void Add(int wh, int pid, int q) { map.TryGetValue((wh, pid), out var cur); map[(wh, pid)] = cur + q; }
        foreach (var d in docs)
            foreach (var l in d.Lines)
            {
                if (d.Type == DocType.In && d.ToWarehouseId is { } to) Add(to, l.ProductId, l.Quantity);
                else if (d.Type == DocType.Out && d.FromWarehouseId is { } fr) Add(fr, l.ProductId, -l.Quantity);
                else if (d.Type == DocType.Transfer)
                {
                    if (d.FromWarehouseId is { } f) Add(f, l.ProductId, -l.Quantity);
                    if (d.ToWarehouseId is { } t) Add(t, l.ProductId, l.Quantity);
                }
            }
        var products = await db.Products.ToDictionaryAsync(p => p.Id, p => p);
        var rows = new List<BalanceRow>();
        foreach (var ((wh, pid), qty) in map)
        {
            if (warehouseId.HasValue && wh != warehouseId.Value) continue;
            if (qty == 0) continue;
            if (!whs.TryGetValue(wh, out var w) || !products.TryGetValue(pid, out var p)) continue;
            rows.Add(new BalanceRow(wh, w.Name, pid, p.Code, p.Name, p.Uom, qty, p.MinStock));
        }
        return rows.OrderBy(r => r.Warehouse).ThenBy(r => r.ProductCode).ToList();
    }

    public async Task<WarehouseCardReport> WarehouseCardAsync(int productId, int? warehouseId, DateTime? fromDate, DateTime? toDate)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new KeyNotFoundException($"Không tìm thấy mặt hàng với ID {productId}.");

        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var docs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted && d.Lines.Any(l => l.ProductId == productId))
            .Include(d => d.FromWarehouse)
            .Include(d => d.ToWarehouse)
            .Include(d => d.Lines)
            .OrderBy(d => d.Date)
            .ThenBy(d => d.Id)
            .ToListAsync();

        // Tách các biến động phát sinh theo kho
        var allTrans = new List<(DateTime Date, string DocCode, int DocId, DocType DocType, string ActionDesc, int WhId, string WhName, string? OffsetWhName, int QtyIn, int QtyOut, string? Note, string? RefNo)>();

        foreach (var d in docs)
        {
            var line = d.Lines.FirstOrDefault(l => l.ProductId == productId);
            if (line == null || line.Quantity == 0) continue;

            bool isAudit = !string.IsNullOrWhiteSpace(d.RefNo) && d.RefNo.StartsWith("KK", StringComparison.OrdinalIgnoreCase)
                           || (!string.IsNullOrWhiteSpace(d.Note) && d.Note.Contains("kiểm kê", StringComparison.OrdinalIgnoreCase));

            if (d.Type == DocType.In)
            {
                if (warehouseId.HasValue && d.ToWarehouseId != warehouseId.Value) continue;
                bool isCusReturn = !string.IsNullOrWhiteSpace(d.RefNo) && d.RefNo.StartsWith("THKH", StringComparison.OrdinalIgnoreCase)
                                   || (!string.IsNullOrWhiteSpace(d.Note) && d.Note.Contains("khách trả", StringComparison.OrdinalIgnoreCase));
                bool isInFG = !string.IsNullOrWhiteSpace(d.RefNo) && d.RefNo.StartsWith("IFFG", StringComparison.OrdinalIgnoreCase)
                              || (!string.IsNullOrWhiteSpace(d.Note) && d.Note.Contains("thành phẩm", StringComparison.OrdinalIgnoreCase));
                var action = isAudit ? "Kiểm kê - Điều chỉnh thừa (AuditIn)" : (isCusReturn ? "Khách trả lại (CusReturn)" : (isInFG ? "Nhập thành phẩm SX (InFG)" : "Nhập kho (In)"));
                allTrans.Add((d.Date, d.Code, d.Id, d.Type, action, d.ToWarehouseId ?? 0, d.ToWarehouse?.Name ?? "", null, line.Quantity, 0, d.Note, d.RefNo));
            }
            else if (d.Type == DocType.Out)
            {
                if (warehouseId.HasValue && d.FromWarehouseId != warehouseId.Value) continue;
                bool isReturnSup = !string.IsNullOrWhiteSpace(d.RefNo) && d.RefNo.StartsWith("THNCC", StringComparison.OrdinalIgnoreCase)
                                   || (!string.IsNullOrWhiteSpace(d.Note) && d.Note.Contains("trả hàng NCC", StringComparison.OrdinalIgnoreCase));
                var action = isAudit ? "Kiểm kê - Điều chỉnh thiếu (AuditOut)" : (isReturnSup ? "Xuất trả NCC (ReturnSup)" : "Xuất kho (Out)");
                allTrans.Add((d.Date, d.Code, d.Id, d.Type, action, d.FromWarehouseId ?? 0, d.FromWarehouse?.Name ?? "", null, 0, line.Quantity, d.Note, d.RefNo));
            }
            else if (d.Type == DocType.Transfer)
            {
                if (warehouseId.HasValue)
                {
                    if (d.FromWarehouseId == warehouseId.Value)
                    {
                        var action = $"Chuyển kho đi (tới {d.ToWarehouse?.Name ?? "kho khác"})";
                        allTrans.Add((d.Date, d.Code, d.Id, d.Type, action, d.FromWarehouseId.Value, d.FromWarehouse?.Name ?? "", d.ToWarehouse?.Name, 0, line.Quantity, d.Note, d.RefNo));
                    }
                    else if (d.ToWarehouseId == warehouseId.Value)
                    {
                        var action = $"Chuyển kho đến (từ {d.FromWarehouse?.Name ?? "kho khác"})";
                        allTrans.Add((d.Date, d.Code, d.Id, d.Type, action, d.ToWarehouseId.Value, d.ToWarehouse?.Name ?? "", d.FromWarehouse?.Name, line.Quantity, 0, d.Note, d.RefNo));
                    }
                }
                else
                {
                    // Trường hợp xem tất cả kho: ghi nhận 2 giao dịch chuyển đi và nhận đến
                    allTrans.Add((d.Date, d.Code, d.Id, d.Type, $"Chuyển đi: {d.FromWarehouse?.Name} -> {d.ToWarehouse?.Name}", d.FromWarehouseId ?? 0, d.FromWarehouse?.Name ?? "", d.ToWarehouse?.Name, 0, line.Quantity, d.Note, d.RefNo));
                    allTrans.Add((d.Date, d.Code, d.Id, d.Type, $"Nhận chuyển: {d.FromWarehouse?.Name} -> {d.ToWarehouse?.Name}", d.ToWarehouseId ?? 0, d.ToWarehouse?.Name ?? "", d.FromWarehouse?.Name, line.Quantity, 0, d.Note, d.RefNo));
                }
            }
        }

        // Tách kỳ báo cáo và tính tồn đầu kỳ
        int openingBalance = 0;
        var startFilterDate = fromDate?.Date;
        var endFilterDate = toDate?.Date.AddDays(1).AddTicks(-1);

        var periodTrans = new List<(DateTime Date, string DocCode, int DocId, DocType DocType, string ActionDesc, int WhId, string WhName, string? OffsetWhName, int QtyIn, int QtyOut, string? Note, string? RefNo)>();

        foreach (var t in allTrans.OrderBy(x => x.Date).ThenBy(x => x.DocId))
        {
            if (startFilterDate.HasValue && t.Date < startFilterDate.Value)
            {
                openingBalance += (t.QtyIn - t.QtyOut);
            }
            else if (!endFilterDate.HasValue || t.Date <= endFilterDate.Value)
            {
                periodTrans.Add(t);
            }
        }

        int running = openingBalance;
        var rows = new List<WarehouseCardRow>();
        foreach (var t in periodTrans)
        {
            running += (t.QtyIn - t.QtyOut);
            rows.Add(new WarehouseCardRow(
                t.Date,
                t.DocCode,
                t.DocId,
                t.DocType,
                t.ActionDesc,
                t.WhId,
                t.WhName,
                t.OffsetWhName,
                t.QtyIn,
                t.QtyOut,
                running,
                t.Note,
                t.RefNo
            ));
        }

        int totalIn = rows.Sum(r => r.QtyIn);
        int totalOut = rows.Sum(r => r.QtyOut);
        int closingBalance = running;

        return new WarehouseCardReport(
            product.Id,
            product.Code,
            product.Name,
            product.Uom,
            warehouseId,
            whName,
            fromDate,
            toDate,
            openingBalance,
            totalIn,
            totalOut,
            closingBalance,
            rows
        );
    }

    public async Task<InventoryInOutReport> InventoryInOutReportAsync(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? keyword)
    {
        var start = (fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var end = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var allWarehouses = await db.Warehouses.ToListAsync();
        var whDict = allWarehouses.ToDictionary(w => w.Id, w => w.Name);

        var prodQuery = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            prodQuery = prodQuery.Where(p => p.Code.ToLower().Contains(kw) || p.Name.ToLower().Contains(kw));
        }
        var products = await prodQuery.ToListAsync();
        var prodDict = products.ToDictionary(p => p.Id, p => p);
        var productIds = prodDict.Keys.ToHashSet();

        // Lấy tất cả các phiếu đã ghi sổ có liên quan đến các sản phẩm cần báo cáo
        var docs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted && d.Date <= end && d.Lines.Any(l => productIds.Contains(l.ProductId)))
            .Include(d => d.Lines)
            .OrderBy(d => d.Date)
            .ThenBy(d => d.Id)
            .ToListAsync();

        // Bảng tổng hợp theo (WarehouseId, ProductId): (OpeningQty, InQty, OutQty)
        var statMap = new Dictionary<(int whId, int prodId), (int opening, int inQty, int outQty)>();

        void AddOpening(int wId, int pId, int delta)
        {
            if (!whDict.ContainsKey(wId) || !productIds.Contains(pId)) return;
            statMap.TryGetValue((wId, pId), out var cur);
            statMap[(wId, pId)] = (cur.opening + delta, cur.inQty, cur.outQty);
        }

        void AddIn(int wId, int pId, int qty)
        {
            if (!whDict.ContainsKey(wId) || !productIds.Contains(pId)) return;
            statMap.TryGetValue((wId, pId), out var cur);
            statMap[(wId, pId)] = (cur.opening, cur.inQty + qty, cur.outQty);
        }

        void AddOut(int wId, int pId, int qty)
        {
            if (!whDict.ContainsKey(wId) || !productIds.Contains(pId)) return;
            statMap.TryGetValue((wId, pId), out var cur);
            statMap[(wId, pId)] = (cur.opening, cur.inQty, cur.outQty + qty);
        }

        foreach (var d in docs)
        {
            bool isBefore = d.Date < start;
            bool isInPeriod = d.Date >= start && d.Date <= end;
            if (!isBefore && !isInPeriod) continue;

            foreach (var line in d.Lines)
            {
                if (!productIds.Contains(line.ProductId) || line.Quantity == 0) continue;

                if (d.Type == DocType.In && d.ToWarehouseId is { } toWh)
                {
                    if (warehouseId.HasValue && toWh != warehouseId.Value) continue;
                    if (isBefore) AddOpening(toWh, line.ProductId, line.Quantity);
                    else if (isInPeriod) AddIn(toWh, line.ProductId, line.Quantity);
                }
                else if (d.Type == DocType.Out && d.FromWarehouseId is { } fromWh)
                {
                    if (warehouseId.HasValue && fromWh != warehouseId.Value) continue;
                    if (isBefore) AddOpening(fromWh, line.ProductId, -line.Quantity);
                    else if (isInPeriod) AddOut(fromWh, line.ProductId, line.Quantity);
                }
                else if (d.Type == DocType.Transfer)
                {
                    if (d.FromWarehouseId is { } fr)
                    {
                        if (!warehouseId.HasValue || fr == warehouseId.Value)
                        {
                            if (isBefore) AddOpening(fr, line.ProductId, -line.Quantity);
                            else if (isInPeriod) AddOut(fr, line.ProductId, line.Quantity);
                        }
                    }
                    if (d.ToWarehouseId is { } to)
                    {
                        if (!warehouseId.HasValue || to == warehouseId.Value)
                        {
                            if (isBefore) AddOpening(to, line.ProductId, line.Quantity);
                            else if (isInPeriod) AddIn(to, line.ProductId, line.Quantity);
                        }
                    }
                }
            }
        }

        var rows = new List<InventoryInOutRow>();
        var whTargetList = warehouseId.HasValue
            ? allWarehouses.Where(w => w.Id == warehouseId.Value).ToList()
            : allWarehouses;

        foreach (var w in whTargetList)
        {
            foreach (var p in products)
            {
                statMap.TryGetValue((w.Id, p.Id), out var stat);
                int closing = stat.opening + stat.inQty - stat.outQty;

                // Nếu có phát sinh hoặc tồn khác 0, hoặc có tìm kiếm theo từ khóa
                if (stat.opening != 0 || stat.inQty != 0 || stat.outQty != 0 || closing != 0 || !string.IsNullOrWhiteSpace(keyword))
                {
                    rows.Add(new InventoryInOutRow(
                        p.Id,
                        p.Code,
                        p.Name,
                        p.Uom,
                        w.Id,
                        w.Name,
                        stat.opening,
                        stat.inQty,
                        stat.outQty,
                        closing
                    ));
                }
            }
        }

        rows = rows.OrderBy(r => r.WarehouseName).ThenBy(r => r.ProductCode).ToList();

        return new InventoryInOutReport(
            warehouseId,
            whName,
            start,
            end.Date,
            keyword,
            rows.Sum(r => r.OpeningQty),
            rows.Sum(r => r.InQty),
            rows.Sum(r => r.OutQty),
            rows.Sum(r => r.ClosingQty),
            rows
        );
    }

    public async Task<List<MoveOrder>> MoveOrdersAsync(int? fromWhId, int? toWhId, MoveOrderStatus? status)
    {
        var q = db.MoveOrders
            .Include(m => m.FromWarehouse)
            .Include(m => m.ToWarehouse)
            .Include(m => m.StockDoc)
            .Include(m => m.Lines).ThenInclude(l => l.Product)
            .AsQueryable();

        if (fromWhId.HasValue) q = q.Where(m => m.FromWarehouseId == fromWhId.Value);
        if (toWhId.HasValue) q = q.Where(m => m.ToWarehouseId == toWhId.Value);
        if (status.HasValue) q = q.Where(m => m.Status == status.Value);

        var list = await q.ToListAsync();
        return list.OrderByDescending(m => m.CreatedAt).ToList();
    }

    public Task<MoveOrder?> GetMoveOrderAsync(int id) =>
        db.MoveOrders
            .Include(m => m.FromWarehouse)
            .Include(m => m.ToWarehouse)
            .Include(m => m.StockDoc)
            .Include(m => m.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<int> CreateMoveOrderAsync(MoveOrder order, List<(int productId, int qty, string? note)> lines)
    {
        if (order.FromWarehouseId == order.ToWarehouseId)
            throw new InvalidOperationException("Kho xuất chuyển và kho nhận chuyển phải khác nhau.");

        order.Code = $"MO{DateTime.Now:yyMMdd}-{await db.MoveOrders.CountAsync() + 1:D3}";
        order.Status = MoveOrderStatus.Pending;
        if (!string.IsNullOrWhiteSpace(order.MoveOrdTypeCode))
        {
            var moveType = await db.MoveOrdTypes.FirstOrDefaultAsync(t => t.Code == order.MoveOrdTypeCode.Trim().ToUpperInvariant());
            if (moveType != null)
            {
                order.MoveOrdTypeCode = moveType.Code;
                order.MoveOrdTypeName = moveType.Name;
            }
        }
        foreach (var (pid, qty, note) in lines.Where(l => l.productId > 0 && l.qty > 0))
            order.Lines.Add(new MoveOrderLine { ProductId = pid, Quantity = qty, Note = note });

        db.MoveOrders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    public async Task<(bool ok, string msg)> ApproveMoveOrderAsync(int id)
    {
        var order = await db.MoveOrders
            .Include(m => m.FromWarehouse)
            .Include(m => m.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null) return (false, "Không tìm thấy lệnh điều chuyển.");
        if (order.Status != MoveOrderStatus.Pending) return (false, "Lệnh không ở trạng thái Chờ duyệt.");
        if (order.Lines.Count == 0) return (false, "Lệnh chưa có danh sách mặt hàng.");

        // Kiểm tra tồn kho tại kho xuất
        var balances = await BalancesAsync(order.FromWarehouseId);
        var balDict = balances.ToDictionary(b => b.ProductId, b => b.Qty);

        foreach (var line in order.Lines)
        {
            balDict.TryGetValue(line.ProductId, out var available);
            if (line.Quantity > available)
            {
                return (false, $"Kho xuất '{order.FromWarehouse.Name}' không đủ tồn cho '{line.Product.Name}': yêu cầu {line.Quantity}, hiện có {available}.");
            }
        }

        order.Status = MoveOrderStatus.Approved;
        order.ApprovedAt = DateTime.Now;
        await db.SaveChangesAsync();
        return (true, $"Đã phê duyệt lệnh điều chuyển {order.Code}. Sẵn sàng thực hiện chuyển hàng.");
    }

    public async Task<(bool ok, string msg)> ExecuteMoveOrderAsync(int id)
    {
        var order = await db.MoveOrders
            .Include(m => m.FromWarehouse)
            .Include(m => m.ToWarehouse)
            .Include(m => m.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null) return (false, "Không tìm thấy lệnh điều chuyển.");
        if (order.Status == MoveOrderStatus.Finished) return (false, "Lệnh điều chuyển đã được thực hiện trước đó.");
        if (order.Status == MoveOrderStatus.Cancelled) return (false, "Lệnh điều chuyển đã bị hủy.");
        if (order.Lines.Count == 0) return (false, "Lệnh chưa có danh sách mặt hàng.");

        // Tự động sinh phiếu chuyển kho StockDoc (DocType.Transfer)
        var transferDoc = new StockDoc
        {
            Type = DocType.Transfer,
            FromWarehouseId = order.FromWarehouseId,
            ToWarehouseId = order.ToWarehouseId,
            RefNo = order.Code,
            Note = $"Thực hiện theo Lệnh điều chuyển {order.Code}" + (string.IsNullOrWhiteSpace(order.Note) ? "" : $": {order.Note}"),
            CreatedBy = string.IsNullOrWhiteSpace(order.CreatedBy) ? "move-order" : order.CreatedBy
        };

        var docLines = order.Lines.Select(l => (l.ProductId, l.Quantity)).ToList();
        var docId = await CreateDocAsync(transferDoc, docLines);

        // Ghi sổ phiếu chuyển kho để trừ tồn kho xuất và tăng tồn kho nhập
        var (postOk, postMsg) = await PostDocAsync(docId);
        if (!postOk) return (false, $"Lỗi ghi sổ phiếu chuyển kho: {postMsg}");

        order.StockDocId = docId;
        order.Status = MoveOrderStatus.Finished;
        order.FinishedAt = DateTime.Now;
        await db.SaveChangesAsync();

        return (true, $"Đã thực hiện thành công Lệnh điều chuyển {order.Code}. Đã ghi sổ phiếu chuyển kho {transferDoc.Code}.");
    }

    public async Task CancelMoveOrderAsync(int id)
    {
        var order = await db.MoveOrders.FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy lệnh điều chuyển.");
        if (order.Status == MoveOrderStatus.Finished)
            throw new InvalidOperationException("Không thể hủy lệnh điều chuyển đã hoàn thành.");

        order.Status = MoveOrderStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    public async Task<List<ReturnToSupplier>> ReturnToSuppliersAsync(int? warehouseId, ReturnSupStatus? status)
    {
        var q = db.ReturnToSuppliers
            .Include(r => r.Warehouse)
            .Include(r => r.StockDoc)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .AsQueryable();

        if (warehouseId.HasValue) q = q.Where(r => r.WarehouseId == warehouseId.Value);
        if (status.HasValue) q = q.Where(r => r.Status == status.Value);

        var list = await q.ToListAsync();
        return list.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public Task<ReturnToSupplier?> GetReturnToSupplierAsync(int id) =>
        db.ReturnToSuppliers
            .Include(r => r.Warehouse)
            .Include(r => r.StockDoc)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<int> CreateReturnToSupplierAsync(ReturnToSupplier returnDoc, List<(int productId, int qty, decimal unitPrice, string? note)> lines)
    {
        returnDoc.Code = $"THNCC{DateTime.Now:yyMMdd}-{await db.ReturnToSuppliers.CountAsync() + 1:D3}";
        returnDoc.Status = ReturnSupStatus.Draft;
        foreach (var (pid, qty, unitPrice, note) in lines.Where(l => l.productId > 0 && l.qty > 0))
            returnDoc.Lines.Add(new ReturnToSupplierLine { ProductId = pid, Quantity = qty, UnitPrice = unitPrice, Note = note });

        db.ReturnToSuppliers.Add(returnDoc);
        await db.SaveChangesAsync();
        return returnDoc.Id;
    }

    public async Task<(bool ok, string msg)> ApproveReturnToSupplierAsync(int id)
    {
        var returnDoc = await db.ReturnToSuppliers
            .Include(r => r.Warehouse)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (returnDoc == null) return (false, "Không tìm thấy phiếu trả hàng nhà cung cấp.");
        if (returnDoc.Status == ReturnSupStatus.Finished) return (false, "Phiếu trả hàng đã được duyệt và xuất kho trước đó.");
        if (returnDoc.Status == ReturnSupStatus.Cancelled) return (false, "Phiếu trả hàng đã bị hủy.");
        if (returnDoc.Lines.Count == 0) return (false, "Phiếu chưa có mặt hàng xuất trả.");

        // Kiểm tra tồn kho khả dụng tại kho xuất
        var balances = await BalancesAsync(returnDoc.WarehouseId);
        var balDict = balances.ToDictionary(b => b.ProductId, b => b.Qty);

        foreach (var line in returnDoc.Lines)
        {
            balDict.TryGetValue(line.ProductId, out var available);
            if (line.Quantity > available)
            {
                return (false, $"Kho '{returnDoc.Warehouse.Name}' không đủ tồn cho '{line.Product.Name}': yêu cầu trả {line.Quantity}, hiện có {available}.");
            }
        }

        // Tự động sinh phiếu xuất kho StockDoc (DocType.Out)
        var stockDoc = new StockDoc
        {
            Type = DocType.Out,
            FromWarehouseId = returnDoc.WarehouseId,
            RefNo = returnDoc.Code,
            Note = $"Xuất trả hàng NCC {returnDoc.SupplierName} theo phiếu {returnDoc.Code}" + (string.IsNullOrWhiteSpace(returnDoc.Reason) ? "" : $": {returnDoc.Reason}"),
            CreatedBy = string.IsNullOrWhiteSpace(returnDoc.CreatedBy) ? "return-sup" : returnDoc.CreatedBy
        };

        var docLines = returnDoc.Lines.Select(l => (l.ProductId, l.Quantity)).ToList();
        var docId = await CreateDocAsync(stockDoc, docLines);

        // Ghi sổ phiếu xuất để trừ tồn kho ngay
        var (postOk, postMsg) = await PostDocAsync(docId);
        if (!postOk) return (false, $"Lỗi ghi sổ phiếu xuất kho: {postMsg}");

        returnDoc.StockDocId = docId;
        returnDoc.Status = ReturnSupStatus.Finished;
        returnDoc.FinishedAt = DateTime.Now;
        await db.SaveChangesAsync();

        return (true, $"Đã duyệt và thực hiện xuất kho trả hàng NCC {returnDoc.Code}. Đã ghi sổ phiếu xuất kho {stockDoc.Code}.");
    }

    public async Task CancelReturnToSupplierAsync(int id)
    {
        var returnDoc = await db.ReturnToSuppliers.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu trả hàng nhà cung cấp.");
        if (returnDoc.Status == ReturnSupStatus.Finished)
            throw new InvalidOperationException("Không thể hủy phiếu trả hàng đã duyệt xuất kho.");

        returnDoc.Status = ReturnSupStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    public async Task<List<CustomerReturn>> CustomerReturnsAsync(int? warehouseId, CusReturnStatus? status)
    {
        var q = db.CustomerReturns
            .Include(c => c.Warehouse)
            .Include(c => c.StockDoc)
            .Include(c => c.Lines).ThenInclude(l => l.Product)
            .AsQueryable();

        if (warehouseId.HasValue) q = q.Where(c => c.WarehouseId == warehouseId.Value);
        if (status.HasValue) q = q.Where(c => c.Status == status.Value);

        var list = await q.ToListAsync();
        return list.OrderByDescending(c => c.CreatedAt).ToList();
    }

    public Task<CustomerReturn?> GetCustomerReturnAsync(int id) =>
        db.CustomerReturns
            .Include(c => c.Warehouse)
            .Include(c => c.StockDoc)
            .Include(c => c.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<int> CreateCustomerReturnAsync(CustomerReturn returnDoc, List<(int productId, int qty, decimal unitPrice, string? note)> lines)
    {
        returnDoc.Code = $"THKH{DateTime.Now:yyMMdd}-{await db.CustomerReturns.CountAsync() + 1:D3}";
        returnDoc.Status = CusReturnStatus.Draft;
        foreach (var (pid, qty, unitPrice, note) in lines.Where(l => l.productId > 0 && l.qty > 0))
            returnDoc.Lines.Add(new CustomerReturnLine { ProductId = pid, Quantity = qty, UnitPrice = unitPrice, Note = note });

        db.CustomerReturns.Add(returnDoc);
        await db.SaveChangesAsync();
        return returnDoc.Id;
    }

    public async Task<(bool ok, string msg)> ApproveCustomerReturnAsync(int id)
    {
        var returnDoc = await db.CustomerReturns
            .Include(c => c.Warehouse)
            .Include(c => c.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (returnDoc == null) return (false, "Không tìm thấy phiếu khách hàng trả lại.");
        if (returnDoc.Status == CusReturnStatus.Finished) return (false, "Phiếu trả hàng đã được duyệt và nhập kho trước đó.");
        if (returnDoc.Status == CusReturnStatus.Cancelled) return (false, "Phiếu trả hàng đã bị hủy.");
        if (returnDoc.Lines.Count == 0) return (false, "Phiếu chưa có mặt hàng nhận lại.");

        // Tự động sinh phiếu nhập kho StockDoc (DocType.In)
        var stockDoc = new StockDoc
        {
            Type = DocType.In,
            ToWarehouseId = returnDoc.WarehouseId,
            RefNo = returnDoc.Code,
            Note = $"Nhập hàng khách trả lại: {returnDoc.CustomerName} theo phiếu {returnDoc.Code}" + (string.IsNullOrWhiteSpace(returnDoc.Reason) ? "" : $": {returnDoc.Reason}"),
            CreatedBy = string.IsNullOrWhiteSpace(returnDoc.CreatedBy) ? "cus-return" : returnDoc.CreatedBy
        };

        var docLines = returnDoc.Lines.Select(l => (l.ProductId, l.Quantity)).ToList();
        var docId = await CreateDocAsync(stockDoc, docLines);

        // Ghi sổ phiếu nhập để cộng tồn kho ngay
        var (postOk, postMsg) = await PostDocAsync(docId);
        if (!postOk) return (false, $"Lỗi ghi sổ phiếu nhập kho: {postMsg}");

        returnDoc.StockDocId = docId;
        returnDoc.Status = CusReturnStatus.Finished;
        returnDoc.FinishedAt = DateTime.Now;
        await db.SaveChangesAsync();

        return (true, $"Đã nhận và nhập kho thành công hàng khách trả {returnDoc.Code}. Đã ghi sổ phiếu nhập kho {stockDoc.Code}.");
    }

    public async Task CancelCustomerReturnAsync(int id)
    {
        var returnDoc = await db.CustomerReturns.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Không tìm thấy phiếu khách trả lại.");
        if (returnDoc.Status == CusReturnStatus.Finished)
            throw new InvalidOperationException("Không thể hủy phiếu trả hàng đã duyệt nhập kho.");

        returnDoc.Status = CusReturnStatus.Cancelled;
        await db.SaveChangesAsync();
    }

    /// <summary>Báo cáo Chạm tồn kho tối thiểu & Cảnh báo an toàn kho (port từ Rpt_Inv_InventoryBalance_Minimum Skycic).</summary>
    public async Task<StockMinimumReport> StockMinimumReportAsync(int? warehouseId, bool onlyBelowMin = true, string? keyword = null)
    {
        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var allWarehouses = await db.Warehouses.ToListAsync();
        var targetWarehouses = warehouseId.HasValue
            ? allWarehouses.Where(w => w.Id == warehouseId.Value).ToList()
            : allWarehouses;

        var prodQuery = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            prodQuery = prodQuery.Where(p => p.Code.ToLower().Contains(kw) || p.Name.ToLower().Contains(kw));
        }
        var products = await prodQuery.OrderBy(p => p.Code).ToListAsync();

        // Lấy toàn bộ phiếu kho đã ghi sổ để tính tồn thực tế cho từng (kho, hàng)
        var docs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted)
            .Include(d => d.Lines)
            .ToListAsync();

        var balMap = new Dictionary<(int whId, int prodId), int>();
        void AddBal(int wId, int pId, int qty)
        {
            balMap.TryGetValue((wId, pId), out var cur);
            balMap[(wId, pId)] = cur + qty;
        }

        foreach (var d in docs)
        {
            foreach (var l in d.Lines)
            {
                if (d.Type == DocType.In && d.ToWarehouseId is { } to) AddBal(to, l.ProductId, l.Quantity);
                else if (d.Type == DocType.Out && d.FromWarehouseId is { } fr) AddBal(fr, l.ProductId, -l.Quantity);
                else if (d.Type == DocType.Transfer)
                {
                    if (d.FromWarehouseId is { } frWh) AddBal(frWh, l.ProductId, -l.Quantity);
                    if (d.ToWarehouseId is { } toWh) AddBal(toWh, l.ProductId, l.Quantity);
                }
            }
        }

        var rows = new List<StockMinimumRow>();

        foreach (var w in targetWarehouses)
        {
            foreach (var p in products)
            {
                balMap.TryGetValue((w.Id, p.Id), out var curQty);
                // Nếu sản phẩm không cấu hình định mức tối thiểu và tồn cũng = 0 thì không theo dõi
                if (p.MinStock <= 0 && curQty == 0) continue;

                int shortage = Math.Max(0, p.MinStock - curQty);
                double ratio = p.MinStock > 0 ? Math.Round((double)curQty * 100.0 / p.MinStock, 1) : 100.0;

                StockAlertLevel level;
                string label;
                string badge;

                if (curQty <= 0 && p.MinStock > 0)
                {
                    level = StockAlertLevel.OutOfStock;
                    label = "Hết hàng / Cháy kho";
                    badge = "bg-danger";
                }
                else if (curQty < p.MinStock)
                {
                    level = StockAlertLevel.Danger;
                    label = "Dưới định mức";
                    badge = "bg-warning text-dark";
                }
                else if (curQty <= Math.Ceiling(p.MinStock * 1.25))
                {
                    level = StockAlertLevel.Warning;
                    label = "Cận định mức";
                    badge = "bg-info text-dark";
                }
                else
                {
                    level = StockAlertLevel.Safe;
                    label = "Đạt an toàn";
                    badge = "bg-success";
                }

                if (onlyBelowMin && level == StockAlertLevel.Safe) continue;

                rows.Add(new StockMinimumRow(
                    p.Id,
                    p.Code,
                    p.Name,
                    p.Uom,
                    w.Id,
                    w.Name,
                    p.MinStock,
                    p.MaxStock,
                    curQty,
                    shortage,
                    ratio,
                    level,
                    label,
                    badge
                ));
            }
        }

        // Sắp xếp: OutOfStock lên đầu, sau đó Danger theo shortage giảm dần, rồi Warning, Safe
        rows = rows
            .OrderBy(r => r.AlertLevel)
            .ThenByDescending(r => r.ShortageQty)
            .ThenBy(r => r.WarehouseName)
            .ThenBy(r => r.ProductCode)
            .ToList();

        int totalMonitored = rows.Count;
        int outOfStock = rows.Count(r => r.AlertLevel == StockAlertLevel.OutOfStock);
        int danger = rows.Count(r => r.AlertLevel == StockAlertLevel.Danger);
        int warning = rows.Count(r => r.AlertLevel == StockAlertLevel.Warning);
        int safe = rows.Count(r => r.AlertLevel == StockAlertLevel.Safe);
        int totalShortage = rows.Sum(r => r.ShortageQty);

        return new StockMinimumReport(
            warehouseId,
            whName,
            onlyBelowMin,
            keyword,
            totalMonitored,
            outOfStock,
            danger,
            warning,
            safe,
            totalShortage,
            rows
        );
    }

    public Task<List<StockLot>> StockLotsAsync(int? warehouseId, int? productId)
    {
        var q = db.StockLots.Include(l => l.Warehouse).Include(l => l.Product).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(l => l.WarehouseId == warehouseId.Value);
        if (productId.HasValue) q = q.Where(l => l.ProductId == productId.Value);
        return q.OrderBy(l => l.ExpiredDate).ToListAsync();
    }

    /// <summary>Báo cáo Quản lý Lô & Hạn sử dụng hàng hóa (port từ Inv_InventoryBalanceLot & Rpt_InvBalLot_MaxExpiredDateByInv Skycic).</summary>
    public async Task<StockLotExpiryReport> StockLotExpiryReportAsync(int? warehouseId, LotExpiryStatus? status, string? keyword)
    {
        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var q = db.StockLots.Include(l => l.Warehouse).Include(l => l.Product).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(l => l.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            q = q.Where(l => l.LotNo.ToLower().Contains(kw) ||
                             l.Product.Code.ToLower().Contains(kw) ||
                             l.Product.Name.ToLower().Contains(kw));
        }

        var lots = await q.ToListAsync();
        var today = DateTime.Today;

        var rows = new List<StockLotReportRow>();
        foreach (var l in lots)
        {
            int daysInStock = Math.Max(0, (today - l.InDate.Date).Days);
            int daysToExpiry = (l.ExpiredDate.Date - today).Days;

            LotExpiryStatus st;
            string label;
            string badge;

            if (daysToExpiry < 0)
            {
                st = LotExpiryStatus.Expired;
                label = $"Đã hết hạn ({Math.Abs(daysToExpiry)} ngày trước)";
                badge = "bg-danger";
            }
            else if (daysToExpiry <= 30)
            {
                st = LotExpiryStatus.Critical;
                label = $"Cận hạn nguy cấp (còn {daysToExpiry} ngày)";
                badge = "bg-warning text-dark";
            }
            else if (daysToExpiry <= 90)
            {
                st = LotExpiryStatus.Warning;
                label = $"Cảnh báo cận hạn (còn {daysToExpiry} ngày)";
                badge = "bg-info text-dark";
            }
            else
            {
                st = LotExpiryStatus.Good;
                label = $"Đạt an toàn (còn {daysToExpiry} ngày)";
                badge = "bg-success";
            }

            if (status.HasValue && st != status.Value) continue;

            rows.Add(new StockLotReportRow(
                l.Id,
                l.WarehouseId,
                l.Warehouse.Name,
                l.ProductId,
                l.Product.Code,
                l.Product.Name,
                l.Product.Uom,
                l.LotNo,
                l.ProductionDate,
                l.ExpiredDate,
                l.InDate,
                daysInStock,
                daysToExpiry,
                l.Quantity,
                st,
                label,
                badge
            ));
        }

        // Sắp xếp theo ưu tiên xuất hàng FEFO (First Expired First Out): hạn sớm nhất lên đầu
        rows = rows
            .OrderBy(r => r.Status)
            .ThenBy(r => r.ExpiredDate)
            .ThenBy(r => r.ProductCode)
            .ToList();

        int totalLots = rows.Count;
        int expiredCount = rows.Count(r => r.Status == LotExpiryStatus.Expired);
        int criticalCount = rows.Count(r => r.Status == LotExpiryStatus.Critical);
        int warningCount = rows.Count(r => r.Status == LotExpiryStatus.Warning);
        int goodCount = rows.Count(r => r.Status == LotExpiryStatus.Good);
        int totalQty = rows.Sum(r => r.Quantity);

        return new StockLotExpiryReport(
            warehouseId,
            whName,
            status,
            keyword,
            totalLots,
            expiredCount,
            criticalCount,
            warningCount,
            goodCount,
            totalQty,
            rows
        );
    }

    /// <summary>Báo cáo Tuổi kho & Thời gian lưu kho hàng hoá (port từ Rpt_Inv_InventoryBalance_StorageTime Skycic).</summary>
    public async Task<StorageTimeReport> StorageTimeReportAsync(int? warehouseId, StorageTimeAgingBracket? bracket, string? keyword, DateTime? asOfDate = null)
    {
        var asOf = (asOfDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var allWarehouses = await db.Warehouses.ToListAsync();
        var targetWarehouses = warehouseId.HasValue
            ? allWarehouses.Where(w => w.Id == warehouseId.Value).ToList()
            : allWarehouses;

        var prodQuery = db.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            prodQuery = prodQuery.Where(p => p.Code.ToLower().Contains(kw) || p.Name.ToLower().Contains(kw));
        }
        var products = await prodQuery.OrderBy(p => p.Code).ToListAsync();

        // Lấy tất cả các phiếu kho đã ghi sổ tính đến ngày chốt báo cáo
        var docs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted && d.Date <= asOf)
            .Include(d => d.Lines)
            .OrderBy(d => d.Date)
            .ThenBy(d => d.Id)
            .ToListAsync();

        var balMap = new Dictionary<(int whId, int prodId), int>();
        var lastInMap = new Dictionary<(int whId, int prodId), DateTime>();

        foreach (var d in docs)
        {
            foreach (var l in d.Lines)
            {
                if (l.Quantity <= 0) continue;

                if (d.Type == DocType.In && d.ToWarehouseId is { } to)
                {
                    balMap.TryGetValue((to, l.ProductId), out var cur);
                    balMap[(to, l.ProductId)] = cur + l.Quantity;

                    if (!lastInMap.TryGetValue((to, l.ProductId), out var prevDate) || d.Date > prevDate)
                        lastInMap[(to, l.ProductId)] = d.Date;
                }
                else if (d.Type == DocType.Out && d.FromWarehouseId is { } fr)
                {
                    balMap.TryGetValue((fr, l.ProductId), out var cur);
                    balMap[(fr, l.ProductId)] = cur - l.Quantity;
                }
                else if (d.Type == DocType.Transfer)
                {
                    if (d.FromWarehouseId is { } frWh)
                    {
                        balMap.TryGetValue((frWh, l.ProductId), out var cur);
                        balMap[(frWh, l.ProductId)] = cur - l.Quantity;
                    }
                    if (d.ToWarehouseId is { } toWh)
                    {
                        balMap.TryGetValue((toWh, l.ProductId), out var cur);
                        balMap[(toWh, l.ProductId)] = cur + l.Quantity;

                        if (!lastInMap.TryGetValue((toWh, l.ProductId), out var prevDate) || d.Date > prevDate)
                            lastInMap[(toWh, l.ProductId)] = d.Date;
                    }
                }
            }
        }

        var rows = new List<StorageTimeRow>();
        var today = DateTime.Today;

        foreach (var w in targetWarehouses)
        {
            foreach (var p in products)
            {
                balMap.TryGetValue((w.Id, p.Id), out var curQty);
                if (curQty <= 0) continue; // Chỉ đưa vào báo cáo các mặt hàng đang có tồn thực tế > 0

                lastInMap.TryGetValue((w.Id, p.Id), out var lastInDate);
                DateTime? validLastIn = lastInDate != default ? lastInDate : null;

                // Tuổi kho tính theo số ngày kể từ ngày nhập kho gần nhất
                int storageDays = validLastIn.HasValue ? Math.Max(0, (today - validLastIn.Value.Date).Days) : 0;

                StorageTimeAgingBracket b;
                string bLabel;
                string badge;
                string rec;

                if (storageDays <= 30)
                {
                    b = StorageTimeAgingBracket.Tier1_Under30;
                    bLabel = "≤ 30 ngày (Mới nhập)";
                    badge = "bg-success";
                    rec = "Lưu kho an toàn, luân chuyển tốt";
                }
                else if (storageDays <= 60)
                {
                    b = StorageTimeAgingBracket.Tier2_31To60;
                    bLabel = "31 - 60 ngày (Bình thường)";
                    badge = "bg-info text-dark";
                    rec = "Duy trì kế hoạch bán hàng thường lệ";
                }
                else if (storageDays <= 90)
                {
                    b = StorageTimeAgingBracket.Tier3_61To90;
                    bLabel = "61 - 90 ngày (Chậm tiêu thụ)";
                    badge = "bg-warning text-dark";
                    rec = "Theo dõi sức mua, tăng cường kích cầu";
                }
                else
                {
                    b = StorageTimeAgingBracket.Tier4_Over90;
                    bLabel = "> 90 ngày (Tồn đọng vốn)";
                    badge = "bg-danger";
                    rec = "Ưu tiên xả hàng, khuyến mãi hoặc luân chuyển chi nhánh";
                }

                if (bracket.HasValue && b != bracket.Value) continue;

                decimal cost = p.CostPrice > 0 ? p.CostPrice : 100000m;
                decimal totalVal = curQty * cost;

                rows.Add(new StorageTimeRow(
                    p.Id,
                    p.Code,
                    p.Name,
                    p.Uom,
                    w.Id,
                    w.Name,
                    curQty,
                    cost,
                    totalVal,
                    validLastIn,
                    storageDays,
                    b,
                    bLabel,
                    badge,
                    rec
                ));
            }
        }

        // Sắp xếp: Ưu tiên tuổi kho lâu ngày nhất lên đầu, tiếp theo là giá trị tồn giảm dần
        rows = rows
            .OrderByDescending(r => r.StorageDays)
            .ThenByDescending(r => r.TotalValue)
            .ThenBy(r => r.WarehouseName)
            .ThenBy(r => r.ProductCode)
            .ToList();

        int totalItems = rows.Count;
        int totalQtySum = rows.Sum(r => r.CurrentQty);
        decimal totalInvVal = rows.Sum(r => r.TotalValue);
        int stagnantCount = rows.Count(r => r.Bracket == StorageTimeAgingBracket.Tier4_Over90);
        decimal stagnantVal = rows.Where(r => r.Bracket == StorageTimeAgingBracket.Tier4_Over90).Sum(r => r.TotalValue);
        double avgDays = rows.Count > 0 ? Math.Round(rows.Average(r => r.StorageDays), 1) : 0.0;

        int under30 = rows.Count(r => r.Bracket == StorageTimeAgingBracket.Tier1_Under30);
        int from31To60 = rows.Count(r => r.Bracket == StorageTimeAgingBracket.Tier2_31To60);
        int from61To90 = rows.Count(r => r.Bracket == StorageTimeAgingBracket.Tier3_61To90);
        int over90 = rows.Count(r => r.Bracket == StorageTimeAgingBracket.Tier4_Over90);

        return new StorageTimeReport(
            warehouseId,
            whName,
            asOf.Date,
            bracket,
            keyword,
            totalItems,
            totalQtySum,
            totalInvVal,
            stagnantCount,
            stagnantVal,
            avgDays,
            under30,
            from31To60,
            from61To90,
            over90,
            rows
        );
    }

    public async Task<WmsDash> DashboardAsync()
    {
        var balances = await BalancesAsync(null);
        var today = DateTime.Today;
        var expiringLots = await db.StockLots.CountAsync(l => l.ExpiredDate <= today.AddDays(30));

        // Tính số mặt hàng đọng vốn > 90 ngày
        var storageReport = await StorageTimeReportAsync(null, StorageTimeAgingBracket.Tier4_Over90, null);
        var stagnantItems = storageReport.StagnantItemsCount;
        var damagedSerials = await db.StockSerials.CountAsync(s => s.Status == StockSerialStatus.DamagedNG);
        var totalBlocks = await db.InventoryBlocks.CountAsync();
        var totalCostPrices = await db.CostPriceHists.CountAsync(c => c.IsCurrent);
        var totalClosedPeriods = await db.PeriodClosings.CountAsync(p => p.Status == PeriodClosingStatus.Closed);
        var totalCartons = await db.InventoryCartons.CountAsync();
        var totalBoxes = await db.InventoryBoxes.CountAsync();
        var pendingInFGs = await db.InventoryInFGs.CountAsync(f => f.Status == InvInFGStatus.Pending);
        var pendingOutFGs = await db.InventoryOutFGs.CountAsync(f => f.Status == InvOutFGStatus.Pending);
        var totalPartTypes = await db.PartTypes.CountAsync(p => p.IsActive);
        var totalInventoryTypes = await db.InventoryTypes.CountAsync(t => t.IsActive);
        var totalInventoryLevelTypes = await db.InventoryLevelTypes.CountAsync(t => t.IsActive);
        var totalUserMapInventories = await db.UserMapInventories.CountAsync(m => m.IsActive);
        var totalProductGroups = await db.ProductGroups.CountAsync(g => g.IsActive);

        return new WmsDash(
            await db.Warehouses.CountAsync(),
            await db.Products.CountAsync(),
            await db.Docs.CountAsync(d => d.Status == DocStatus.Posted),
            await db.Docs.CountAsync(d => d.Status == DocStatus.Draft),
            balances.Sum(b => b.Qty),
            balances.Count(b => b.MinStock > 0 && b.Qty <= b.MinStock),
            await db.Audits.CountAsync(a => a.Status == StockAuditStatus.Draft),
            await db.MoveOrders.CountAsync(m => m.Status == MoveOrderStatus.Pending || m.Status == MoveOrderStatus.Approved),
            await db.ReturnToSuppliers.CountAsync(r => r.Status == ReturnSupStatus.Draft),
            await db.CustomerReturns.CountAsync(c => c.Status == CusReturnStatus.Draft),
            expiringLots,
            stagnantItems,
            damagedSerials,
            totalBlocks,
            totalCostPrices,
            totalClosedPeriods,
            totalCartons,
            totalBoxes,
            pendingInFGs,
            pendingOutFGs,
            totalPartTypes,
            totalInventoryTypes,
            totalInventoryLevelTypes,
            totalUserMapInventories,
            totalProductGroups);
    }

    public Task<List<StockSerial>> StockSerialsAsync(int? warehouseId, int? productId, StockSerialStatus? status)
    {
        var q = db.StockSerials.Include(s => s.Warehouse).Include(s => s.Product).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(s => s.WarehouseId == warehouseId.Value);
        if (productId.HasValue) q = q.Where(s => s.ProductId == productId.Value);
        if (status.HasValue) q = q.Where(s => s.Status == status.Value);
        return q.OrderByDescending(s => s.CreatedAt).ToListAsync();
    }

    public Task<StockSerial?> GetStockSerialAsync(int id) =>
        db.StockSerials.Include(s => s.Warehouse).Include(s => s.Product).FirstOrDefaultAsync(s => s.Id == id);

    public async Task<int> CreateStockSerialAsync(StockSerial serial)
    {
        if (string.IsNullOrWhiteSpace(serial.SerialNo))
            throw new ArgumentException("Số Serial/IMEI không được để trống.");

        serial.SerialNo = serial.SerialNo.Trim().ToUpperInvariant();
        var exists = await db.StockSerials.AnyAsync(s => s.WarehouseId == serial.WarehouseId &&
                                                         s.ProductId == serial.ProductId &&
                                                         s.SerialNo == serial.SerialNo);
        if (exists)
            throw new InvalidOperationException($"Số Serial/IMEI '{serial.SerialNo}' đã tồn tại trong kho cho mặt hàng này.");

        serial.CreatedAt = DateTime.Now;
        db.StockSerials.Add(serial);
        await db.SaveChangesAsync();
        return serial.Id;
    }

    public async Task<(bool ok, string msg)> ChangeStockSerialStatusAsync(int id, StockSerialStatus newStatus, string? note)
    {
        var serial = await db.StockSerials.FirstOrDefaultAsync(s => s.Id == id);
        if (serial == null) return (false, "Không tìm thấy Serial/IMEI.");

        var oldStatus = serial.Status;
        serial.Status = newStatus;
        serial.UpdatedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(note))
        {
            serial.Note = string.IsNullOrWhiteSpace(serial.Note) ? note.Trim() : $"{serial.Note}; {note.Trim()}";
        }

        if (newStatus == StockSerialStatus.Exported && oldStatus != StockSerialStatus.Exported)
        {
            serial.OutDate = DateTime.Now;
        }
        else if (newStatus != StockSerialStatus.Exported && oldStatus == StockSerialStatus.Exported)
        {
            serial.OutDate = null;
        }

        await db.SaveChangesAsync();
        var statusLabel = newStatus switch
        {
            StockSerialStatus.Available => "Khả dụng / Sẵn sàng",
            StockSerialStatus.Locked => "Tạm khóa / Giữ hàng",
            StockSerialStatus.DamagedNG => "Báo hỏng / Thẩm định NG",
            StockSerialStatus.Exported => "Đã xuất kho",
            _ => newStatus.ToString()
        };
        return (true, $"Đã cập nhật trạng thái Serial '{serial.SerialNo}' thành: {statusLabel}.");
    }

    /// <summary>Báo cáo Quản lý & Tra cứu Serial / IMEI hàng tồn kho (port từ Inv_InventoryBalanceSerial Skycic).</summary>
    public async Task<StockSerialReport> StockSerialReportAsync(int? warehouseId, int? productId, StockSerialStatus? status, string? keyword)
    {
        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        string prodName = "Tất cả mặt hàng";
        if (productId.HasValue)
        {
            var p = await db.Products.FirstOrDefaultAsync(x => x.Id == productId.Value);
            if (p != null) prodName = $"{p.Code} - {p.Name}";
        }

        var q = db.StockSerials.Include(s => s.Warehouse).Include(s => s.Product).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(s => s.WarehouseId == warehouseId.Value);
        if (productId.HasValue) q = q.Where(s => s.ProductId == productId.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            q = q.Where(s => s.SerialNo.ToLower().Contains(kw) ||
                             (s.LotNo != null && s.LotNo.ToLower().Contains(kw)) ||
                             (s.RefNo != null && s.RefNo.ToLower().Contains(kw)) ||
                             s.Product.Code.ToLower().Contains(kw) ||
                             s.Product.Name.ToLower().Contains(kw));
        }

        var allMatchingSerials = await q.ToListAsync();

        int totalSerials = allMatchingSerials.Count;
        int availableCount = allMatchingSerials.Count(s => s.Status == StockSerialStatus.Available);
        int lockedCount = allMatchingSerials.Count(s => s.Status == StockSerialStatus.Locked);
        int damagedNGCount = allMatchingSerials.Count(s => s.Status == StockSerialStatus.DamagedNG);
        int exportedCount = allMatchingSerials.Count(s => s.Status == StockSerialStatus.Exported);

        var filtered = status.HasValue
            ? allMatchingSerials.Where(s => s.Status == status.Value).ToList()
            : allMatchingSerials;

        var rows = filtered
            .OrderBy(s => s.Status)
            .ThenByDescending(s => s.InDate)
            .ThenBy(s => s.SerialNo)
            .Select(s =>
            {
                var (label, badge) = s.Status switch
                {
                    StockSerialStatus.Available => ("Khả dụng", "bg-success"),
                    StockSerialStatus.Locked => ("Tạm khóa", "bg-warning text-dark"),
                    StockSerialStatus.DamagedNG => ("Lỗi / NG", "bg-danger"),
                    StockSerialStatus.Exported => ("Đã xuất", "bg-secondary"),
                    _ => (s.Status.ToString(), "bg-secondary")
                };

                return new StockSerialRow(
                    s.Id,
                    s.WarehouseId,
                    s.Warehouse.Name,
                    s.ProductId,
                    s.Product.Code,
                    s.Product.Name,
                    s.Product.Uom,
                    s.SerialNo,
                    s.LotNo,
                    s.InDate,
                    s.OutDate,
                    s.RefNo,
                    s.Status,
                    label,
                    badge,
                    s.Note
                );
            })
            .ToList();

        return new StockSerialReport(
            warehouseId,
            whName,
            productId,
            prodName,
            status,
            keyword,
            totalSerials,
            availableCount,
            lockedCount,
            damagedNGCount,
            exportedCount,
            rows
        );
    }

    public Task<List<InventoryBlock>> InventoryBlocksAsync(int? warehouseId, string? shelfCode)
    {
        var q = db.InventoryBlocks.Include(b => b.Warehouse).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(b => b.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(shelfCode)) q = q.Where(b => b.ShelfCode == shelfCode.Trim().ToUpperInvariant());
        return q.OrderBy(b => b.Warehouse.Code).ThenBy(b => b.ShelfCode).ThenBy(b => b.InvBlockCode).ToListAsync();
    }

    public Task<InventoryBlock?> GetInventoryBlockAsync(int id) =>
        db.InventoryBlocks.Include(b => b.Warehouse).FirstOrDefaultAsync(b => b.Id == id);

    public async Task<int> CreateInventoryBlockAsync(InventoryBlock block)
    {
        if (block.WarehouseId <= 0) throw new ArgumentException("Cần chọn Kho lưu trữ.");
        if (string.IsNullOrWhiteSpace(block.InvBlockCode)) throw new ArgumentException("Mã vị trí ô kho không được để trống.");
        if (string.IsNullOrWhiteSpace(block.ShelfCode)) throw new ArgumentException("Mã dãy kệ không được để trống.");

        block.InvBlockCode = block.InvBlockCode.Trim().ToUpperInvariant();
        block.ShelfCode = block.ShelfCode.Trim().ToUpperInvariant();
        block.InvBlockDesc = block.InvBlockDesc?.Trim();
        block.Remark = block.Remark?.Trim();
        block.Length = Math.Max(0, block.Length);
        block.Width = Math.Max(0, block.Width);
        block.Height = Math.Max(0, block.Height);
        block.MaxCapacity = Math.Max(1, block.MaxCapacity);

        var exists = await db.InventoryBlocks.AnyAsync(b => b.WarehouseId == block.WarehouseId && b.InvBlockCode == block.InvBlockCode);
        if (exists)
            throw new InvalidOperationException($"Mã vị trí '{block.InvBlockCode}' đã tồn tại trong kho này.");

        block.CreatedAt = DateTime.Now;
        db.InventoryBlocks.Add(block);
        await db.SaveChangesAsync();
        return block.Id;
    }

    public async Task<(bool ok, string msg)> UpdateInventoryBlockAsync(int id, InventoryBlock block)
    {
        var existing = await db.InventoryBlocks.FirstOrDefaultAsync(b => b.Id == id);
        if (existing == null) return (false, "Không tìm thấy vị trí ô kệ cần cập nhật.");

        if (!string.IsNullOrWhiteSpace(block.ShelfCode))
            existing.ShelfCode = block.ShelfCode.Trim().ToUpperInvariant();

        existing.InvBlockDesc = block.InvBlockDesc?.Trim();
        existing.Length = Math.Max(0, block.Length);
        existing.Width = Math.Max(0, block.Width);
        existing.Height = Math.Max(0, block.Height);
        existing.MaxCapacity = Math.Max(1, block.MaxCapacity);
        existing.Remark = block.Remark?.Trim();
        existing.FlagActive = block.FlagActive;
        existing.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật vị trí kho '{existing.InvBlockCode}'.");
    }

    public async Task<(bool ok, string msg)> ToggleInventoryBlockStatusAsync(int id)
    {
        var block = await db.InventoryBlocks.FirstOrDefaultAsync(b => b.Id == id);
        if (block == null) return (false, "Không tìm thấy vị trí ô kệ.");

        block.FlagActive = !block.FlagActive;
        block.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();

        var st = block.FlagActive ? "Đang hoạt động" : "Tạm ngừng / Bảo trì";
        return (true, $"Đã chuyển trạng thái vị trí '{block.InvBlockCode}' sang: {st}.");
    }

    public async Task<(bool ok, string msg)> DeleteInventoryBlockAsync(int id)
    {
        var block = await db.InventoryBlocks.FirstOrDefaultAsync(b => b.Id == id);
        if (block == null) return (false, "Không tìm thấy vị trí ô kệ.");

        db.InventoryBlocks.Remove(block);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa vị trí '{block.InvBlockCode}'.");
    }

    public async Task<List<string>> GetShelvesAsync(int? warehouseId)
    {
        var q = db.InventoryBlocks.AsQueryable();
        if (warehouseId.HasValue) q = q.Where(b => b.WarehouseId == warehouseId.Value);
        return await q.Select(b => b.ShelfCode).Distinct().OrderBy(s => s).ToListAsync();
    }

    /// <summary>Báo cáo & Danh sách Quản lý Vị trí kho tổng hợp (port từ Mst_InventoryBlock Skycic).</summary>
    public async Task<InventoryBlockReport> InventoryBlockReportAsync(int? warehouseId, string? shelfCode, bool? activeFilter, string? keyword)
    {
        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var q = db.InventoryBlocks.Include(b => b.Warehouse).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(b => b.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(shelfCode))
        {
            var shelfUpper = shelfCode.Trim().ToUpperInvariant();
            q = q.Where(b => b.ShelfCode == shelfUpper);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            q = q.Where(b => b.InvBlockCode.ToLower().Contains(kw) ||
                             b.ShelfCode.ToLower().Contains(kw) ||
                             (b.InvBlockDesc != null && b.InvBlockDesc.ToLower().Contains(kw)) ||
                             (b.Remark != null && b.Remark.ToLower().Contains(kw)) ||
                             b.Warehouse.Name.ToLower().Contains(kw));
        }

        var allMatching = await q.ToListAsync();

        int totalBlocks = allMatching.Count;
        int activeCount = allMatching.Count(b => b.FlagActive);
        int maintenanceCount = allMatching.Count(b => !b.FlagActive);
        int totalShelves = allMatching.Select(b => b.ShelfCode).Distinct().Count();
        double totalVolumeM3 = Math.Round(allMatching.Sum(b => b.VolumeM3), 3);
        int totalCapacity = allMatching.Sum(b => b.MaxCapacity);

        var filtered = activeFilter.HasValue
            ? allMatching.Where(b => b.FlagActive == activeFilter.Value).ToList()
            : allMatching;

        var rows = filtered
            .OrderBy(b => b.Warehouse.Name)
            .ThenBy(b => b.ShelfCode)
            .ThenBy(b => b.InvBlockCode)
            .Select(b => new InventoryBlockRow(
                b.Id,
                b.WarehouseId,
                b.Warehouse.Code,
                b.Warehouse.Name,
                b.InvBlockCode,
                b.ShelfCode,
                b.InvBlockDesc,
                b.Length,
                b.Width,
                b.Height,
                b.VolumeM3,
                b.MaxCapacity,
                b.FlagActive,
                b.FlagActive ? "Hoạt động" : "Bảo trì / Khóa",
                b.FlagActive ? "bg-success" : "bg-warning text-dark",
                b.Remark,
                b.CreatedAt
            ))
            .ToList();

        return new InventoryBlockReport(
            warehouseId,
            whName,
            shelfCode,
            activeFilter,
            keyword,
            totalBlocks,
            activeCount,
            maintenanceCount,
            totalShelves,
            totalVolumeM3,
            totalCapacity,
            rows
        );
    }

    /// <summary>Báo cáo & Danh sách Lịch sử giá vốn kho tổng hợp (port từ Inv_CostPriceHist Skycic).</summary>
    public async Task<CostPriceHistReport> CostPriceHistReportAsync(int? warehouseId, int? productId, bool? currentOnly, DateTime? fromDate, DateTime? toDate, string? keyword)
    {
        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        string prodName = "Tất cả mặt hàng";
        if (productId.HasValue)
        {
            var prod = await db.Products.FirstOrDefaultAsync(p => p.Id == productId.Value);
            if (prod != null) prodName = prod.Name;
        }

        var q = db.CostPriceHists.Include(c => c.Warehouse).Include(c => c.Product).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(c => c.WarehouseId == warehouseId.Value);
        if (productId.HasValue) q = q.Where(c => c.ProductId == productId.Value);
        if (currentOnly.HasValue && currentOnly.Value) q = q.Where(c => c.IsCurrent);
        if (fromDate.HasValue) q = q.Where(c => c.EffectDate >= fromDate.Value.Date);
        if (toDate.HasValue) q = q.Where(c => c.EffectDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            q = q.Where(c => c.Product.Code.ToLower().Contains(kw) ||
                             c.Product.Name.ToLower().Contains(kw) ||
                             (c.RefDocNo != null && c.RefDocNo.ToLower().Contains(kw)) ||
                             (c.CalcPeriodName != null && c.CalcPeriodName.ToLower().Contains(kw)) ||
                             (c.Remark != null && c.Remark.ToLower().Contains(kw)) ||
                             (c.Warehouse != null && c.Warehouse.Name.ToLower().Contains(kw)));
        }

        var list = await q.OrderByDescending(c => c.EffectDate).ThenBy(c => c.Product.Code).ToListAsync();

        int totalRecords = list.Count;
        int currentItemsCount = list.Where(c => c.IsCurrent).Select(c => c.ProductId).Distinct().Count();
        decimal avgCostPrice = list.Count > 0 ? Math.Round(list.Average(c => c.CostPrice), 0) : 0m;
        decimal maxCostPrice = list.Count > 0 ? list.Max(c => c.CostPrice) : 0m;
        decimal minCostPrice = list.Count > 0 ? list.Min(c => c.CostPrice) : 0m;

        var rows = list.Select(c =>
        {
            var (sourceLabel, badge) = c.SourceType switch
            {
                CostPriceSourceType.AutoCalc => ("Kỳ tính tự động", "bg-primary"),
                CostPriceSourceType.Manual => ("Điều chỉnh tay", "bg-warning text-dark"),
                _ => ("Khác", "bg-secondary")
            };

            return new CostPriceHistRow(
                c.Id,
                c.WarehouseId,
                c.Warehouse != null ? c.Warehouse.Name : "Toàn hệ thống",
                c.ProductId,
                c.Product.Code,
                c.Product.Name,
                c.Product.Uom,
                c.EffectDate,
                c.CostPrice,
                c.RefDocNo,
                c.IsCurrent,
                c.CalcPeriodName,
                c.SourceType,
                sourceLabel,
                badge,
                c.Remark,
                c.CreatedBy,
                c.CreatedAt,
                c.UpdatedBy,
                c.UpdatedAt
            );
        }).ToList();

        return new CostPriceHistReport(
            warehouseId,
            whName,
            productId,
            prodName,
            currentOnly,
            keyword,
            fromDate,
            toDate,
            totalRecords,
            currentItemsCount,
            avgCostPrice,
            maxCostPrice,
            minCostPrice,
            rows
        );
    }

    public Task<CostPriceHist?> GetCostPriceHistAsync(int id) =>
        db.CostPriceHists.Include(c => c.Warehouse).Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<int> CreateCostPriceHistAsync(CostPriceHist item)
    {
        if (item.ProductId <= 0) throw new InvalidOperationException("Vui lòng chọn mặt hàng.");
        if (item.CostPrice < 0) throw new InvalidOperationException("Giá vốn không thể âm.");

        if (item.IsCurrent)
        {
            // Cập nhật các bản ghi cũ của sản phẩm này tại kho này thành không hiện hành
            var oldRecords = await db.CostPriceHists
                .Where(c => c.ProductId == item.ProductId && c.WarehouseId == item.WarehouseId && c.IsCurrent)
                .ToListAsync();
            foreach (var old in oldRecords)
            {
                old.IsCurrent = false;
                old.UpdatedAt = DateTime.Now;
            }

            // Cập nhật giá vốn trên bảng Product
            var prod = await db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
            if (prod != null)
            {
                prod.CostPrice = item.CostPrice;
            }
        }

        item.CreatedAt = DateTime.Now;
        db.CostPriceHists.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateCostPriceHistAsync(int id, decimal costPrice, string? remark)
    {
        var item = await db.CostPriceHists.Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == id);
        if (item == null) return (false, "Không tìm thấy bản ghi giá vốn.");
        if (costPrice < 0) return (false, "Giá vốn không thể âm.");

        item.CostPrice = costPrice;
        item.Remark = remark?.Trim();
        item.UpdatedAt = DateTime.Now;
        item.UpdatedBy = "user";

        if (item.IsCurrent && item.Product != null)
        {
            item.Product.CostPrice = costPrice;
        }

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật giá vốn của '{item.Product?.Name}' thành {costPrice:N0} đ.");
    }

    public async Task<CostPriceCalcPreviewReport> PreviewCalculateCostPriceAsync(int? warehouseId, DateTime fromDate, DateTime toDate, string calcPeriodName, int[]? productIds)
    {
        string whName = "Toàn bộ kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var prodQuery = db.Products.AsQueryable();
        if (productIds != null && productIds.Length > 0)
        {
            prodQuery = prodQuery.Where(p => productIds.Contains(p.Id));
        }
        var products = await prodQuery.OrderBy(p => p.Code).ToListAsync();

        // Lấy các phiếu nhập kho đã ghi sổ trong kỳ
        var inDocsQuery = db.Docs
            .Include(d => d.Lines)
            .Where(d => d.Type == DocType.In && d.Status == DocStatus.Posted && d.Date >= fromDate.Date && d.Date <= toDate.Date.AddDays(1).AddTicks(-1));
        if (warehouseId.HasValue)
        {
            inDocsQuery = inDocsQuery.Where(d => d.ToWarehouseId == warehouseId.Value);
        }
        var inDocs = await inDocsQuery.ToListAsync();

        var items = new List<CostPriceCalcItem>();
        int calculatedCount = 0;
        int changedCount = 0;

        foreach (var p in products)
        {
            decimal oldCost = p.CostPrice;
            var docLines = inDocs.SelectMany(d => d.Lines).Where(l => l.ProductId == p.Id).ToList();
            int inQty = docLines.Sum(l => l.Quantity);

            decimal inAmount = 0m;
            decimal newCost = oldCost;
            string note = "";

            if (inQty > 0)
            {
                calculatedCount++;
                inAmount = inQty * (oldCost > 0 ? oldCost : 100000m);

                // Công thức tính giá vốn bình quân nhập kho kỳ này
                newCost = Math.Round(oldCost > 0 ? (oldCost * 0.98m + (inAmount / inQty) * 0.02m) : (inAmount / inQty), 0);
                if (newCost <= 0) newCost = oldCost;

                if (newCost != oldCost)
                {
                    changedCount++;
                    note = $"Phát sinh {inQty} {p.Uom} nhập kho trong kỳ. Giá vốn được tính lại bình quân.";
                }
                else
                {
                    note = $"Phát sinh {inQty} {p.Uom} nhập kho. Đơn giá không biến động.";
                }
            }
            else
            {
                note = "Không phát sinh nhập kho trong kỳ. Giữ nguyên giá vốn.";
            }

            decimal diffAmount = newCost - oldCost;
            double diffPercent = oldCost > 0 ? Math.Round((double)(diffAmount / oldCost) * 100.0, 2) : 0;

            items.Add(new CostPriceCalcItem(
                p.Id,
                p.Code,
                p.Name,
                p.Uom,
                warehouseId,
                whName,
                oldCost,
                inQty,
                inAmount,
                newCost,
                diffAmount,
                diffPercent,
                note
            ));
        }

        return new CostPriceCalcPreviewReport(
            warehouseId,
            whName,
            fromDate,
            toDate,
            calcPeriodName,
            products.Count,
            calculatedCount,
            changedCount,
            items
        );
    }

    public async Task<(bool ok, string msg, int count)> ApplyCalculateCostPriceAsync(int? warehouseId, DateTime effectDate, string calcPeriodName, List<(int ProductId, decimal NewCostPrice, string Note)> items)
    {
        if (items == null || items.Count == 0) return (false, "Không có mặt hàng nào để áp dụng giá vốn.", 0);

        int appliedCount = 0;
        foreach (var it in items)
        {
            var prod = await db.Products.FirstOrDefaultAsync(p => p.Id == it.ProductId);
            if (prod == null) continue;

            // Đặt các bản ghi cũ của sản phẩm này tại kho này thành không hiện hành
            var oldRecords = await db.CostPriceHists
                .Where(c => c.ProductId == it.ProductId && c.WarehouseId == warehouseId && c.IsCurrent)
                .ToListAsync();
            foreach (var old in oldRecords)
            {
                old.IsCurrent = false;
                old.UpdatedAt = DateTime.Now;
            }

            // Thêm bản ghi lịch sử mới
            var hist = new CostPriceHist
            {
                WarehouseId = warehouseId,
                ProductId = it.ProductId,
                EffectDate = effectDate,
                CostPrice = it.NewCostPrice,
                RefDocNo = calcPeriodName,
                CalcPeriodName = calcPeriodName,
                IsCurrent = true,
                SourceType = CostPriceSourceType.AutoCalc,
                Remark = string.IsNullOrWhiteSpace(it.Note) ? $"Chốt tính giá vốn {calcPeriodName}" : it.Note,
                CreatedBy = "hethong",
                CreatedAt = DateTime.Now
            };
            db.CostPriceHists.Add(hist);

            // Cập nhật giá vốn trên bảng Product
            prod.CostPrice = it.NewCostPrice;
            appliedCount++;
        }

        await db.SaveChangesAsync();
        return (true, $"Đã áp dụng và cập nhật giá vốn mới cho {appliedCount} mặt hàng thành công.", appliedCount);
    }

    /// <summary>Danh sách các kỳ chốt tồn kho (port từ Rpt_In_Out_Inv Skycic).</summary>
    public async Task<List<PeriodClosing>> PeriodClosingsAsync(int? warehouseId, PeriodClosingStatus? status, int? year)
    {
        var q = db.PeriodClosings
            .Include(p => p.Warehouse)
            .Include(p => p.Lines).ThenInclude(l => l.Product)
            .Include(p => p.Lines).ThenInclude(l => l.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue) q = q.Where(p => p.WarehouseId == warehouseId.Value);
        if (status.HasValue) q = q.Where(p => p.Status == status.Value);
        if (year.HasValue) q = q.Where(p => p.PeriodMonth.Year == year.Value);

        return await q.OrderByDescending(p => p.PeriodMonth).ThenByDescending(p => p.CreatedAt).ToListAsync();
    }

    /// <summary>Chi tiết kỳ chốt tồn kho (port từ Rpt_In_Out_Inv Skycic).</summary>
    public Task<PeriodClosing?> GetPeriodClosingAsync(int id) =>
        db.PeriodClosings
            .Include(p => p.Warehouse)
            .Include(p => p.Lines).ThenInclude(l => l.Product)
            .Include(p => p.Lines).ThenInclude(l => l.Warehouse)
            .FirstOrDefaultAsync(p => p.Id == id);

    /// <summary>Tính toán và xem trước số liệu chốt kỳ tồn kho (port từ 20200407.ChotTonKho.sql Skycic).</summary>
    public async Task<PeriodClosingPreviewReport> PreviewPeriodClosingAsync(int? warehouseId, int year, int month)
    {
        string whName = "Toàn bộ kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var periodMonth = new DateTime(year, month, 1);
        var fromDate = periodMonth;
        var toDate = fromDate.AddMonths(1).AddTicks(-1);
        string periodName = $"Kỳ chốt kho Tháng {month:D2}/{year}" + (warehouseId.HasValue ? $" ({whName})" : " (Toàn hệ thống)");

        var whList = warehouseId.HasValue
            ? await db.Warehouses.Where(w => w.Id == warehouseId.Value).ToListAsync()
            : await db.Warehouses.OrderBy(w => w.Code).ToListAsync();

        var products = await db.Products.OrderBy(p => p.Code).ToListAsync();

        // Lấy tất cả các phiếu kho đã Post phát sinh đến hết kỳ này
        var allDocs = await db.Docs
            .Include(d => d.Lines)
            .Where(d => d.Status == DocStatus.Posted && d.Date <= toDate)
            .ToListAsync();

        var previewItems = new List<PeriodClosingPreviewItem>();

        foreach (var wh in whList)
        {
            foreach (var prod in products)
            {
                // Tồn đầu kỳ: biến động trước ngày fromDate
                int inBefore = allDocs
                    .Where(d => d.Date < fromDate && (d.ToWarehouseId == wh.Id))
                    .SelectMany(d => d.Lines)
                    .Where(l => l.ProductId == prod.Id)
                    .Sum(l => l.Quantity);

                int outBefore = allDocs
                    .Where(d => d.Date < fromDate && (d.FromWarehouseId == wh.Id))
                    .SelectMany(d => d.Lines)
                    .Where(l => l.ProductId == prod.Id)
                    .Sum(l => l.Quantity);

                int openingQty = inBefore - outBefore;

                // Phát sinh trong kỳ
                int inQty = allDocs
                    .Where(d => d.Date >= fromDate && d.Date <= toDate && (d.ToWarehouseId == wh.Id))
                    .SelectMany(d => d.Lines)
                    .Where(l => l.ProductId == prod.Id)
                    .Sum(l => l.Quantity);

                int outQty = allDocs
                    .Where(d => d.Date >= fromDate && d.Date <= toDate && (d.FromWarehouseId == wh.Id))
                    .SelectMany(d => d.Lines)
                    .Where(l => l.ProductId == prod.Id)
                    .Sum(l => l.Quantity);

                int closingQty = openingQty + inQty - outQty;

                // Chỉ đưa vào danh sách nếu có phát sinh hoặc có tồn kho
                if (openingQty != 0 || inQty != 0 || outQty != 0 || closingQty != 0)
                {
                    decimal costPrice = prod.CostPrice > 0 ? prod.CostPrice : 100000m;
                    decimal inAmount = inQty * costPrice;
                    decimal outAmount = outQty * costPrice;
                    decimal closingVal = closingQty * costPrice;

                    previewItems.Add(new PeriodClosingPreviewItem(
                        wh.Id,
                        wh.Name,
                        prod.Id,
                        prod.Code,
                        prod.Name,
                        prod.Uom,
                        openingQty,
                        inQty,
                        inAmount,
                        outQty,
                        outAmount,
                        closingQty,
                        costPrice,
                        closingVal
                    ));
                }
            }
        }

        int totalOpening = previewItems.Sum(i => i.OpeningQty);
        int totalIn = previewItems.Sum(i => i.InQty);
        int totalOut = previewItems.Sum(i => i.OutQty);
        int totalClosing = previewItems.Sum(i => i.ClosingQty);
        decimal totalClosingVal = previewItems.Sum(i => i.ClosingValue);

        return new PeriodClosingPreviewReport(
            warehouseId,
            whName,
            year,
            month,
            periodMonth,
            fromDate,
            toDate,
            periodName,
            previewItems.Count,
            totalOpening,
            totalIn,
            totalOut,
            totalClosing,
            totalClosingVal,
            previewItems
        );
    }

    /// <summary>Thực hiện chốt sổ kỳ tồn kho & Lưu vết snapshot (port từ 20200407.ChotTonKho.sql Skycic).</summary>
    public async Task<(bool ok, string msg, int id)> CreateAndClosePeriodAsync(int? warehouseId, int year, int month, string? note, string closedBy)
    {
        var periodMonth = new DateTime(year, month, 1);

        // Kiểm tra xem kỳ này đã được chốt trước đó chưa
        var exists = await db.PeriodClosings.AnyAsync(p =>
            p.PeriodMonth.Year == year &&
            p.PeriodMonth.Month == month &&
            p.WarehouseId == warehouseId &&
            p.Status == PeriodClosingStatus.Closed);

        if (exists)
        {
            return (false, $"Kỳ tồn kho Tháng {month:D2}/{year} cho kho này đã được chốt sổ trước đó. Vui lòng mở lại kỳ nếu muốn chốt lại.", 0);
        }

        var preview = await PreviewPeriodClosingAsync(warehouseId, year, month);

        var closing = new PeriodClosing
        {
            Code = $"CK{year % 100:D2}{month:D2}-{await db.PeriodClosings.CountAsync() + 1:D3}",
            PeriodMonth = periodMonth,
            PeriodName = preview.PeriodName,
            WarehouseId = warehouseId,
            Status = PeriodClosingStatus.Closed,
            ClosedAt = DateTime.Now,
            ClosedBy = string.IsNullOrWhiteSpace(closedBy) ? "admin" : closedBy.Trim(),
            Note = note?.Trim(),
            CreatedAt = DateTime.Now
        };

        foreach (var item in preview.Items)
        {
            closing.Lines.Add(new PeriodClosingLine
            {
                WarehouseId = item.WarehouseId,
                ProductId = item.ProductId,
                OpeningQty = item.OpeningQty,
                InQty = item.InQty,
                LastInPrice = item.CostPrice,
                InAmount = item.InAmount,
                OutQty = item.OutQty,
                LastOutPrice = item.CostPrice,
                OutAmount = item.OutAmount,
                ClosingQty = item.ClosingQty,
                CostPrice = item.CostPrice,
                ClosingValue = item.ClosingValue,
                Note = $"Chốt kỳ {month:D2}/{year}"
            });
        }

        db.PeriodClosings.Add(closing);
        await db.SaveChangesAsync();

        return (true, $"Đã chốt sổ thành công '{closing.PeriodName}' (Mã: {closing.Code}) với {closing.Lines.Count} mặt hàng.", closing.Id);
    }

    /// <summary>Mở lại kỳ chốt tồn kho để điều chỉnh số liệu (port từ Rpt_In_Out_Inv Skycic).</summary>
    public async Task<(bool ok, string msg)> ReopenPeriodClosingAsync(int id, string reason)
    {
        var closing = await db.PeriodClosings.FirstOrDefaultAsync(p => p.Id == id);
        if (closing == null) return (false, "Không tìm thấy kỳ chốt kho.");
        if (closing.Status == PeriodClosingStatus.Cancelled) return (false, "Kỳ chốt kho này đã bị hủy bỏ.");

        closing.Status = PeriodClosingStatus.Reopened;
        closing.ReopenedAt = DateTime.Now;
        closing.ReopenReason = string.IsNullOrWhiteSpace(reason) ? "Mở lại để kiểm tra và đối soát bổ sung" : reason.Trim();
        closing.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã mở lại '{closing.PeriodName}'. Trạng thái hiện tại: Đã mở lại.");
    }

    /// <summary>Hủy kỳ chốt kho.</summary>
    public async Task<(bool ok, string msg)> CancelPeriodClosingAsync(int id)
    {
        var closing = await db.PeriodClosings.FirstOrDefaultAsync(p => p.Id == id);
        if (closing == null) return (false, "Không tìm thấy kỳ chốt kho.");

        closing.Status = PeriodClosingStatus.Cancelled;
        closing.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã hủy bỏ kỳ chốt kho '{closing.PeriodName}'.");
    }

    /// <summary>Báo cáo & Danh sách Quản lý Thùng Carton (port từ Inv_InventoryCarton Skycic).</summary>
    public async Task<CartonReport> CartonsAsync(int? warehouseId, int? productId, CartonStatus? status, string? q)
    {
        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        string? prodName = null;
        if (productId.HasValue)
        {
            var p = await db.Products.FirstOrDefaultAsync(x => x.Id == productId.Value);
            if (p != null) prodName = p.Name;
        }

        var query = db.InventoryCartons.Include(c => c.Warehouse).Include(c => c.Product).AsQueryable();
        if (warehouseId.HasValue) query = query.Where(c => c.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(c => c.ProductId == productId.Value);
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(c => c.CartonCode.ToLower().Contains(kw) ||
                                     (c.QrCode != null && c.QrCode.ToLower().Contains(kw)) ||
                                     (c.LotNo != null && c.LotNo.ToLower().Contains(kw)) ||
                                     (c.ShelfLocation != null && c.ShelfLocation.ToLower().Contains(kw)) ||
                                     (c.PackerName != null && c.PackerName.ToLower().Contains(kw)) ||
                                     (c.RefDocNo != null && c.RefDocNo.ToLower().Contains(kw)) ||
                                     (c.Remark != null && c.Remark.ToLower().Contains(kw)) ||
                                     (c.Product != null && (c.Product.Code.ToLower().Contains(kw) || c.Product.Name.ToLower().Contains(kw))));
        }

        var list = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

        int totalCartons = list.Count;
        int emptyCount = list.Count(c => c.Status == CartonStatus.Empty);
        int packingCount = list.Count(c => c.Status == CartonStatus.Packing);
        int sealedCount = list.Count(c => c.Status == CartonStatus.Sealed);
        int shippedCount = list.Count(c => c.Status == CartonStatus.Shipped);
        int totalItemsPacked = list.Sum(c => c.Quantity);
        double totalVolumeM3 = Math.Round(list.Sum(c => c.VolumeM3), 3);
        double totalWeightKg = Math.Round(list.Sum(c => c.GrossWeightKg), 2);

        var rows = list.Select(c =>
        {
            var (statusLabel, badgeClass) = c.Status switch
            {
                CartonStatus.Empty => ("Thùng rỗng", "bg-secondary"),
                CartonStatus.Packing => ("Đang đóng kiện", "bg-warning text-dark"),
                CartonStatus.Sealed => ("Đã niêm phong", "bg-success"),
                CartonStatus.Shipped => ("Đã xuất kho", "bg-primary"),
                CartonStatus.Unpacked => ("Đã tháo dỡ", "bg-dark"),
                _ => ("Khác", "bg-secondary")
            };

            return new CartonRow(
                c.Id,
                c.CartonCode,
                c.QrCode,
                c.WarehouseId,
                c.Warehouse.Name,
                c.CartonType,
                c.ProductId,
                c.Product?.Code,
                c.Product?.Name,
                c.Product?.Uom,
                c.LotNo,
                c.Quantity,
                c.Capacity,
                c.LengthCm,
                c.WidthCm,
                c.HeightCm,
                c.VolumeM3,
                c.GrossWeightKg,
                c.Status,
                statusLabel,
                badgeClass,
                c.ShelfLocation,
                c.PackerName,
                c.PackedAt,
                c.SealedAt,
                c.ShippedAt,
                c.RefDocNo,
                c.Remark,
                c.CreatedAt
            );
        }).ToList();

        return new CartonReport(
            warehouseId,
            whName,
            productId,
            prodName,
            status,
            q,
            totalCartons,
            emptyCount,
            packingCount,
            sealedCount,
            shippedCount,
            totalItemsPacked,
            totalVolumeM3,
            totalWeightKg,
            rows
        );
    }

    public Task<InventoryCarton?> GetCartonAsync(int id) =>
        db.InventoryCartons
            .Include(c => c.Warehouse)
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<int> CreateCartonAsync(InventoryCarton carton)
    {
        if (string.IsNullOrWhiteSpace(carton.CartonCode))
        {
            carton.CartonCode = $"CTN{DateTime.Now:yyMMdd}-{await db.InventoryCartons.CountAsync() + 1:D3}";
        }
        if (string.IsNullOrWhiteSpace(carton.QrCode))
        {
            carton.QrCode = carton.CartonCode;
        }

        if (carton.Quantity > 0 && carton.Status == CartonStatus.Empty)
        {
            carton.Status = CartonStatus.Packing;
            carton.PackedAt ??= DateTime.Now;
        }

        carton.CreatedAt = DateTime.Now;
        db.InventoryCartons.Add(carton);
        await db.SaveChangesAsync();
        return carton.Id;
    }

    public async Task<(bool ok, string msg, List<int> ids)> GenerateCartonsBatchAsync(
        int warehouseId, string cartonType, int count, double length, double width, double height, int capacity, string? shelfLocation, string? prefix)
    {
        if (count <= 0 || count > 500) return (false, "Số lượng sinh mã thùng phải từ 1 đến 500.", []);

        var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId);
        if (wh == null) return (false, "Không tìm thấy kho lưu trữ.", []);

        var currentTotal = await db.InventoryCartons.CountAsync();
        var pre = string.IsNullOrWhiteSpace(prefix) ? $"CTN{DateTime.Now:yyMMdd}-" : prefix.Trim();
        var type = string.IsNullOrWhiteSpace(cartonType) ? "Thùng carton tiêu chuẩn" : cartonType.Trim();

        var list = new List<InventoryCarton>();
        for (int i = 1; i <= count; i++)
        {
            var code = $"{pre}{currentTotal + i:D3}";
            list.Add(new InventoryCarton
            {
                WarehouseId = warehouseId,
                CartonCode = code,
                QrCode = code,
                CartonType = type,
                LengthCm = length > 0 ? length : 40,
                WidthCm = width > 0 ? width : 30,
                HeightCm = height > 0 ? height : 30,
                Capacity = capacity > 0 ? capacity : 50,
                ShelfLocation = shelfLocation?.Trim(),
                Status = CartonStatus.Empty,
                CreatedAt = DateTime.Now
            });
        }

        db.InventoryCartons.AddRange(list);
        await db.SaveChangesAsync();

        return (true, $"Đã sinh thành công {count} mã thùng carton mới ({list.First().CartonCode} &rarr; {list.Last().CartonCode}).", list.Select(c => c.Id).ToList());
    }

    public async Task<(bool ok, string msg)> PackCartonAsync(int id, int productId, int quantity, string? lotNo, double grossWeightKg, string? packerName, string? note)
    {
        var carton = await db.InventoryCartons.FirstOrDefaultAsync(c => c.Id == id);
        if (carton == null) return (false, "Không tìm thấy thùng carton.");
        if (carton.Status == CartonStatus.Sealed) return (false, "Thùng đã được niêm phong, không thể đóng thêm hàng. Vui lòng mở kiện trước.");
        if (carton.Status == CartonStatus.Shipped) return (false, "Thùng hàng đã xuất kho, không thể thao tác đóng gói.");

        var prod = await db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (prod == null) return (false, "Không tìm thấy mặt hàng.");
        if (quantity <= 0) return (false, "Số lượng đóng thùng phải lớn hơn 0.");

        carton.ProductId = productId;
        carton.Quantity = quantity;
        carton.LotNo = lotNo?.Trim();
        if (grossWeightKg > 0) carton.GrossWeightKg = grossWeightKg;
        carton.PackerName = string.IsNullOrWhiteSpace(packerName) ? "thukho" : packerName.Trim();
        carton.PackedAt = DateTime.Now;
        carton.Status = CartonStatus.Packing;
        if (!string.IsNullOrWhiteSpace(note)) carton.Remark = note.Trim();
        carton.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã đóng {quantity} {prod.Uom} '{prod.Name}' vào thùng {carton.CartonCode}.");
    }

    public async Task<(bool ok, string msg)> SealCartonAsync(int id)
    {
        var carton = await db.InventoryCartons.Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == id);
        if (carton == null) return (false, "Không tìm thấy thùng carton.");
        if (carton.Status == CartonStatus.Sealed) return (false, "Thùng carton đã được niêm phong trước đó.");
        if (carton.Status == CartonStatus.Shipped) return (false, "Thùng hàng đã xuất kho.");

        carton.Status = CartonStatus.Sealed;
        carton.SealedAt = DateTime.Now;
        carton.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã niêm phong thành công thùng carton {carton.CartonCode}. Sẵn sàng xuất kho / vận chuyển.");
    }

    public async Task<(bool ok, string msg)> UnpackCartonAsync(int id, string? reason)
    {
        var carton = await db.InventoryCartons.FirstOrDefaultAsync(c => c.Id == id);
        if (carton == null) return (false, "Không tìm thấy thùng carton.");
        if (carton.Status == CartonStatus.Shipped) return (false, "Không thể tháo dỡ thùng hàng đã xuất kho giao cho khách.");

        carton.ProductId = null;
        carton.Quantity = 0;
        carton.LotNo = null;
        carton.GrossWeightKg = 0;
        carton.Status = CartonStatus.Empty;
        carton.SealedAt = null;
        carton.PackedAt = null;
        var r = string.IsNullOrWhiteSpace(reason) ? "Đã dỡ hàng về thùng trống" : reason.Trim();
        carton.Remark = string.IsNullOrWhiteSpace(carton.Remark) ? r : $"{carton.Remark} | {r}";
        carton.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã tháo dỡ hàng khỏi thùng {carton.CartonCode}. Thùng đã đưa về trạng thái trống.");
    }

    public async Task<(bool ok, string msg)> ShipCartonAsync(int id, string refDocNo)
    {
        var carton = await db.InventoryCartons.FirstOrDefaultAsync(c => c.Id == id);
        if (carton == null) return (false, "Không tìm thấy thùng carton.");
        if (carton.Status == CartonStatus.Empty) return (false, "Thùng rỗng không thể thực hiện xuất kho giao hàng.");

        carton.Status = CartonStatus.Shipped;
        carton.ShippedAt = DateTime.Now;
        carton.RefDocNo = refDocNo.Trim();
        carton.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã ghi nhận xuất kho cho thùng {carton.CartonCode} theo chứng từ {carton.RefDocNo}.");
    }

    public async Task<(bool ok, string msg)> DeleteCartonAsync(int id)
    {
        var carton = await db.InventoryCartons.FirstOrDefaultAsync(c => c.Id == id);
        if (carton == null) return (false, "Không tìm thấy thùng carton.");
        if (carton.Status == CartonStatus.Sealed || carton.Status == CartonStatus.Shipped)
            return (false, "Không thể xóa thùng carton đã niêm phong hoặc đã xuất kho. Hãy dỡ thùng trước khi xóa.");

        db.InventoryCartons.Remove(carton);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa thùng carton {carton.CartonCode}.");
    }

    /// <summary>Báo cáo & Danh sách Quản lý Hộp đóng gói (Warehouse Box Packaging - port từ Inv_InventoryBox Skycic).</summary>
    public async Task<BoxReport> BoxesAsync(int? warehouseId, int? productId, int? cartonId, BoxStatus? status, bool? flagMap, string? q)
    {
        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        string? prodName = null;
        if (productId.HasValue)
        {
            var p = await db.Products.FirstOrDefaultAsync(x => x.Id == productId.Value);
            if (p != null) prodName = p.Name;
        }

        string? ctnCode = null;
        if (cartonId.HasValue)
        {
            var c = await db.InventoryCartons.FirstOrDefaultAsync(x => x.Id == cartonId.Value);
            if (c != null) ctnCode = c.CartonCode;
        }

        var query = db.InventoryBoxes.Include(b => b.Warehouse).Include(b => b.Carton).Include(b => b.Product).AsQueryable();
        if (warehouseId.HasValue) query = query.Where(b => b.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(b => b.ProductId == productId.Value);
        if (cartonId.HasValue) query = query.Where(b => b.CartonId == cartonId.Value);
        if (status.HasValue) query = query.Where(b => b.Status == status.Value);
        if (flagMap.HasValue) query = query.Where(b => b.FlagMap == flagMap.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(b => b.BoxCode.ToLower().Contains(kw) ||
                                     (b.QrCode != null && b.QrCode.ToLower().Contains(kw)) ||
                                     (b.GenTimesBoxNo != null && b.GenTimesBoxNo.ToLower().Contains(kw)) ||
                                     (b.SecretNo != null && b.SecretNo.ToLower().Contains(kw)) ||
                                     (b.LotNo != null && b.LotNo.ToLower().Contains(kw)) ||
                                     (b.ShelfLocation != null && b.ShelfLocation.ToLower().Contains(kw)) ||
                                     (b.PackerName != null && b.PackerName.ToLower().Contains(kw)) ||
                                     (b.RefDocNo != null && b.RefDocNo.ToLower().Contains(kw)) ||
                                     (b.Remark != null && b.Remark.ToLower().Contains(kw)) ||
                                     (b.Product != null && (b.Product.Code.ToLower().Contains(kw) || b.Product.Name.ToLower().Contains(kw))) ||
                                     (b.Carton != null && b.Carton.CartonCode.ToLower().Contains(kw)));
        }

        var list = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

        int totalBoxes = list.Count;
        int emptyCount = list.Count(b => b.Status == BoxStatus.Empty);
        int packingCount = list.Count(b => b.Status == BoxStatus.Packing);
        int sealedCount = list.Count(b => b.Status == BoxStatus.Sealed);
        int inCartonCount = list.Count(b => b.Status == BoxStatus.InCarton || b.FlagMap);
        int shippedCount = list.Count(b => b.Status == BoxStatus.Shipped);
        int totalItemsPacked = list.Sum(b => b.Quantity);
        double totalVolumeM3 = Math.Round(list.Sum(b => b.VolumeM3), 4);
        double totalWeightKg = Math.Round(list.Sum(b => b.GrossWeightKg), 2);

        var rows = list.Select(b =>
        {
            var (statusLabel, badgeClass) = b.Status switch
            {
                BoxStatus.Empty => ("Hộp rỗng", "bg-secondary"),
                BoxStatus.Packing => ("Đang đóng hàng", "bg-warning text-dark"),
                BoxStatus.Sealed => ("Đã niêm phong", "bg-info text-dark"),
                BoxStatus.InCarton => ("Đã đóng vào thùng", "bg-success"),
                BoxStatus.Shipped => ("Đã xuất kho", "bg-primary"),
                BoxStatus.Unpacked => ("Đã tháo dỡ", "bg-dark"),
                _ => ("Khác", "bg-secondary")
            };

            var (mapLabel, mapBadgeClass) = b.FlagMap
                ? ("Đã gán thùng", "bg-success")
                : ("Chưa gán thùng", "bg-light text-muted border");

            return new BoxRow(
                b.Id,
                b.BoxCode,
                b.QrCode,
                b.GenTimesBoxNo,
                b.SecretNo,
                b.WarehouseId,
                b.Warehouse.Name,
                b.CartonId,
                b.Carton?.CartonCode,
                b.BoxType,
                b.ProductId,
                b.Product?.Code,
                b.Product?.Name,
                b.Product?.Uom,
                b.LotNo,
                b.Quantity,
                b.Capacity,
                b.LengthCm,
                b.WidthCm,
                b.HeightCm,
                b.VolumeM3,
                b.GrossWeightKg,
                b.Status,
                statusLabel,
                badgeClass,
                b.FlagMap,
                mapLabel,
                mapBadgeClass,
                b.FlagUsed,
                b.ShelfLocation,
                b.PackerName,
                b.PackedAt,
                b.SealedAt,
                b.ShippedAt,
                b.RefDocNo,
                b.Remark,
                b.CreatedAt
            );
        }).ToList();

        return new BoxReport(
            warehouseId,
            whName,
            productId,
            prodName,
            cartonId,
            ctnCode,
            status,
            flagMap,
            q,
            totalBoxes,
            emptyCount,
            packingCount,
            sealedCount,
            inCartonCount,
            shippedCount,
            totalItemsPacked,
            totalVolumeM3,
            totalWeightKg,
            rows
        );
    }

    public Task<InventoryBox?> GetBoxAsync(int id) =>
        db.InventoryBoxes
          .Include(b => b.Warehouse)
          .Include(b => b.Carton)
          .Include(b => b.Product)
          .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<int> CreateBoxAsync(InventoryBox box)
    {
        if (string.IsNullOrWhiteSpace(box.BoxCode))
        {
            var seq = await db.InventoryBoxes.CountAsync() + 1;
            box.BoxCode = $"BOX{DateTime.Now:yyMM}-{seq:D4}";
        }
        if (string.IsNullOrWhiteSpace(box.QrCode))
        {
            box.QrCode = box.BoxCode;
        }

        if (box.CartonId.HasValue && box.CartonId.Value > 0)
        {
            box.FlagMap = true;
            if (box.Status == BoxStatus.Empty) box.Status = BoxStatus.InCarton;
        }

        db.InventoryBoxes.Add(box);
        await db.SaveChangesAsync();
        return box.Id;
    }

    /// <summary>Sinh dải mã hộp hàng loạt (port từ Inv_GenTimesBox Skycic).</summary>
    public async Task<(bool ok, string msg, List<int> ids)> GenerateBoxesBatchAsync(
        int warehouseId,
        string boxType,
        int count,
        double length,
        double width,
        double height,
        int capacity,
        int? cartonId,
        string? shelfLocation,
        string? prefix)
    {
        var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId);
        if (wh == null) return (false, "Không tìm thấy kho lưu trữ.", []);

        InventoryCarton? carton = null;
        if (cartonId.HasValue && cartonId.Value > 0)
        {
            carton = await db.InventoryCartons.FirstOrDefaultAsync(c => c.Id == cartonId.Value);
            if (carton == null) return (false, "Không tìm thấy thùng carton chỉ định.", []);
        }

        var pref = string.IsNullOrWhiteSpace(prefix) ? "BOX" : prefix.Trim().ToUpper();
        var nextSeq = await db.InventoryBoxes.CountAsync() + 1;
        var genTimesNo = $"GTB{DateTime.Now:yyMMddHHmm}";

        var list = new List<InventoryBox>();
        for (int i = 0; i < count; i++)
        {
            var code = $"{pref}{DateTime.Now:yyMM}-{(nextSeq + i):D4}";
            list.Add(new InventoryBox
            {
                WarehouseId = warehouseId,
                BoxCode = code,
                QrCode = code,
                GenTimesBoxNo = genTimesNo,
                BoxType = string.IsNullOrWhiteSpace(boxType) ? "Hộp duplex tiêu chuẩn" : boxType.Trim(),
                CartonId = carton?.Id,
                FlagMap = carton != null,
                LengthCm = length > 0 ? length : 20,
                WidthCm = width > 0 ? width : 15,
                HeightCm = height > 0 ? height : 10,
                Capacity = capacity > 0 ? capacity : 10,
                ShelfLocation = shelfLocation?.Trim(),
                Status = carton != null ? BoxStatus.InCarton : BoxStatus.Empty,
                CreatedAt = DateTime.Now
            });
        }

        db.InventoryBoxes.AddRange(list);
        await db.SaveChangesAsync();

        return (true, $"Đã sinh thành công {count} mã hộp mới theo đợt '{genTimesNo}' ({list.First().BoxCode} &rarr; {list.Last().BoxCode}).", list.Select(b => b.Id).ToList());
    }

    public async Task<(bool ok, string msg)> PackBoxAsync(int id, int productId, int quantity, string? lotNo, double grossWeightKg, string? packerName, string? secretNo, string? note)
    {
        var box = await db.InventoryBoxes.FirstOrDefaultAsync(b => b.Id == id);
        if (box == null) return (false, "Không tìm thấy hộp đóng gói.");
        if (box.Status == BoxStatus.Sealed) return (false, "Hộp đã được niêm phong, vui lòng mở hộp trước khi đóng thêm hàng.");
        if (box.Status == BoxStatus.Shipped) return (false, "Hộp hàng đã xuất kho, không thể thao tác đóng hàng.");

        var prod = await db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (prod == null) return (false, "Không tìm thấy mặt hàng.");
        if (quantity <= 0) return (false, "Số lượng đóng hộp phải lớn hơn 0.");

        box.ProductId = productId;
        box.Quantity = quantity;
        box.LotNo = lotNo?.Trim();
        if (grossWeightKg > 0) box.GrossWeightKg = grossWeightKg;
        if (!string.IsNullOrWhiteSpace(secretNo)) box.SecretNo = secretNo.Trim();
        box.PackerName = string.IsNullOrWhiteSpace(packerName) ? "thukho" : packerName.Trim();
        box.PackedAt = DateTime.Now;
        box.FlagUsed = true;
        box.Status = BoxStatus.Packing;
        if (!string.IsNullOrWhiteSpace(note)) box.Remark = note.Trim();
        box.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã đóng {quantity} {prod.Uom} '{prod.Name}' vào hộp {box.BoxCode}.");
    }

    public async Task<(bool ok, string msg)> SealBoxAsync(int id, string? secretNo)
    {
        var box = await db.InventoryBoxes.Include(b => b.Product).FirstOrDefaultAsync(b => b.Id == id);
        if (box == null) return (false, "Không tìm thấy hộp đóng gói.");
        if (box.Status == BoxStatus.Sealed) return (false, "Hộp đã được niêm phong trước đó.");
        if (box.Status == BoxStatus.Shipped) return (false, "Hộp hàng đã xuất kho.");

        box.Status = BoxStatus.Sealed;
        box.SealedAt = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(secretNo)) box.SecretNo = secretNo.Trim();
        box.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã niêm phong thành công hộp {box.BoxCode}. Sẵn sàng gán vào thùng carton hoặc xuất kho.");
    }

    /// <summary>Gán hộp vào thùng carton (port từ Inv_InventoryBalanceSerial_UpdCanFromBox Skycic).</summary>
    public async Task<(bool ok, string msg)> MapBoxToCartonAsync(int boxId, int cartonId)
    {
        var box = await db.InventoryBoxes.Include(b => b.Product).FirstOrDefaultAsync(b => b.Id == boxId);
        if (box == null) return (false, "Không tìm thấy hộp.");
        if (box.Status == BoxStatus.Shipped) return (false, "Hộp đã xuất kho, không thể gán vào thùng.");

        var carton = await db.InventoryCartons.FirstOrDefaultAsync(c => c.Id == cartonId);
        if (carton == null) return (false, "Không tìm thấy thùng carton.");
        if (carton.Status == CartonStatus.Shipped) return (false, "Thùng carton đã xuất kho, không thể gán thêm hộp.");
        if (carton.WarehouseId != box.WarehouseId) return (false, "Hộp và Thùng carton phải ở cùng một kho lưu trữ.");

        // Gán hộp vào thùng
        box.CartonId = carton.Id;
        box.FlagMap = true;
        box.Status = BoxStatus.InCarton;
        box.UpdatedAt = DateTime.Now;

        // Đồng bộ thông tin mặt hàng và cập nhật số lượng thùng carton
        if (!carton.ProductId.HasValue && box.ProductId.HasValue)
        {
            carton.ProductId = box.ProductId;
            carton.LotNo = box.LotNo;
        }

        if (box.Quantity > 0)
        {
            carton.Quantity += box.Quantity;
        }
        if (box.GrossWeightKg > 0)
        {
            carton.GrossWeightKg = Math.Round(carton.GrossWeightKg + box.GrossWeightKg, 2);
        }
        if (carton.Status == CartonStatus.Empty)
        {
            carton.Status = CartonStatus.Packing;
        }
        carton.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã gán thành công hộp '{box.BoxCode}' vào thùng carton '{carton.CartonCode}'.");
    }

    /// <summary>Gỡ hộp khỏi thùng carton.</summary>
    public async Task<(bool ok, string msg)> UnmapBoxFromCartonAsync(int boxId)
    {
        var box = await db.InventoryBoxes.FirstOrDefaultAsync(b => b.Id == boxId);
        if (box == null) return (false, "Không tìm thấy hộp.");
        if (box.Status == BoxStatus.Shipped) return (false, "Hộp đã xuất kho, không thể gỡ.");
        if (!box.CartonId.HasValue) return (false, "Hộp này chưa được gán vào thùng nào.");

        var carton = await db.InventoryCartons.FirstOrDefaultAsync(c => c.Id == box.CartonId.Value);
        if (carton != null)
        {
            if (box.Quantity > 0)
            {
                carton.Quantity = Math.Max(0, carton.Quantity - box.Quantity);
            }
            if (box.GrossWeightKg > 0)
            {
                carton.GrossWeightKg = Math.Max(0, Math.Round(carton.GrossWeightKg - box.GrossWeightKg, 2));
            }
            if (carton.Quantity == 0)
            {
                carton.Status = CartonStatus.Empty;
            }
            carton.UpdatedAt = DateTime.Now;
        }

        var oldCartonCode = carton?.CartonCode ?? "thùng carton";
        box.CartonId = null;
        box.FlagMap = false;
        box.Status = box.Quantity > 0 ? BoxStatus.Sealed : BoxStatus.Empty;
        box.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã tách hộp '{box.BoxCode}' ra khỏi {oldCartonCode}.");
    }

    public async Task<(bool ok, string msg)> UnpackBoxAsync(int id, string? reason)
    {
        var box = await db.InventoryBoxes.FirstOrDefaultAsync(b => b.Id == id);
        if (box == null) return (false, "Không tìm thấy hộp.");
        if (box.Status == BoxStatus.Shipped) return (false, "Không thể tháo dỡ hộp hàng đã xuất kho.");

        // Nếu hộp đang nằm trong thùng carton thì cần gỡ khỏi thùng trước
        if (box.CartonId.HasValue)
        {
            var carton = await db.InventoryCartons.FirstOrDefaultAsync(c => c.Id == box.CartonId.Value);
            if (carton != null)
            {
                carton.Quantity = Math.Max(0, carton.Quantity - box.Quantity);
                carton.GrossWeightKg = Math.Max(0, Math.Round(carton.GrossWeightKg - box.GrossWeightKg, 2));
                if (carton.Quantity == 0) carton.Status = CartonStatus.Empty;
            }
            box.CartonId = null;
            box.FlagMap = false;
        }

        box.ProductId = null;
        box.Quantity = 0;
        box.LotNo = null;
        box.GrossWeightKg = 0;
        box.Status = BoxStatus.Empty;
        box.FlagUsed = false;
        box.SealedAt = null;
        box.PackedAt = null;
        var r = string.IsNullOrWhiteSpace(reason) ? "Đã dỡ hàng về hộp trống" : reason.Trim();
        box.Remark = string.IsNullOrWhiteSpace(box.Remark) ? r : $"{box.Remark} | {r}";
        box.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã tháo dỡ hàng khỏi hộp {box.BoxCode}. Hộp đã đưa về trạng thái trống.");
    }

    public async Task<(bool ok, string msg)> ShipBoxAsync(int id, string refDocNo)
    {
        var box = await db.InventoryBoxes.FirstOrDefaultAsync(b => b.Id == id);
        if (box == null) return (false, "Không tìm thấy hộp.");
        if (box.Status == BoxStatus.Empty) return (false, "Hộp rỗng không thể thực hiện xuất kho giao hàng.");

        box.Status = BoxStatus.Shipped;
        box.ShippedAt = DateTime.Now;
        box.RefDocNo = refDocNo.Trim();
        box.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return (true, $"Đã ghi nhận xuất kho cho hộp {box.BoxCode} theo chứng từ {box.RefDocNo}.");
    }

    public async Task<(bool ok, string msg)> DeleteBoxAsync(int id)
    {
        var box = await db.InventoryBoxes.FirstOrDefaultAsync(b => b.Id == id);
        if (box == null) return (false, "Không tìm thấy hộp.");
        if (box.Status != BoxStatus.Empty)
            return (false, "Chỉ có thể xóa hộp rỗng. Vui lòng tháo dỡ hoặc gỡ hộp khỏi thùng trước khi xóa.");

        db.InventoryBoxes.Remove(box);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa hộp {box.BoxCode}.");
    }

    public Task<List<InventoryCarton>> AvailableCartonsAsync(int warehouseId) =>
        db.InventoryCartons
          .Where(c => c.WarehouseId == warehouseId && c.Status != CartonStatus.Shipped)
          .OrderBy(c => c.CartonCode)
          .ToListAsync();

    public async Task<InventoryInFGReport> InventoryInFGsAsync(int? warehouseId, InvInFGStatus? status, InvInFGFormType? formType, DateTime? fromDate, DateTime? toDate, string? q)
    {
        var query = db.InventoryInFGs
            .Include(f => f.Warehouse)
            .Include(f => f.StockDoc)
            .Include(f => f.Lines).ThenInclude(l => l.Product)
            .Include(f => f.Serials).ThenInclude(s => s.Product)
            .AsQueryable();

        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            query = query.Where(f => f.WarehouseId == warehouseId.Value);
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        if (status.HasValue) query = query.Where(f => f.Status == status.Value);
        if (formType.HasValue) query = query.Where(f => f.FormType == formType.Value);

        if (fromDate.HasValue)
        {
            var f = fromDate.Value.Date;
            query = query.Where(x => x.Date >= f);
        }
        if (toDate.HasValue)
        {
            var t = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.Date <= t);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(f => f.Code.ToLower().Contains(kw) ||
                                     f.WorkshopName.ToLower().Contains(kw) ||
                                     (f.WorkOrderNo != null && f.WorkOrderNo.ToLower().Contains(kw)) ||
                                     (f.ShiftLeader != null && f.ShiftLeader.ToLower().Contains(kw)) ||
                                     (f.Remark != null && f.Remark.ToLower().Contains(kw)) ||
                                     f.Lines.Any(l => l.Product.Code.ToLower().Contains(kw) || l.Product.Name.ToLower().Contains(kw)));
        }

        var list = await query.OrderByDescending(f => f.Date).ThenByDescending(f => f.Id).ToListAsync();

        int totalReceipts = list.Count;
        int pendingCount = list.Count(f => f.Status == InvInFGStatus.Pending);
        int approvedCount = list.Count(f => f.Status == InvInFGStatus.Approved);
        int cancelledCount = list.Count(f => f.Status == InvInFGStatus.Cancelled);
        int totalPlanQty = list.Sum(f => f.TotalPlanQty);
        int totalActualQty = list.Sum(f => f.TotalActualQty);
        int totalDefectQty = list.Sum(f => f.TotalDefectQty);
        decimal totalAmount = list.Sum(f => f.TotalAmount);

        var rows = list.Select(f =>
        {
            var (formLabel, _) = f.FormType switch
            {
                InvInFGFormType.InternalProduction => ("Sản xuất nội bộ", "bg-primary"),
                InvInFGFormType.Outsourced => ("Gia công ngoài", "bg-info text-dark"),
                InvInFGFormType.AssemblyPack => ("Lắp ráp đóng gói", "bg-secondary"),
                InvInFGFormType.WarrantyRefurbish => ("Tân trang bảo hành", "bg-warning text-dark"),
                _ => ("Khác", "bg-light text-dark")
            };

            var (statusLabel, badgeClass) = f.Status switch
            {
                InvInFGStatus.Pending => ("Chờ duyệt KCS", "bg-warning text-dark"),
                InvInFGStatus.Approved => ("Đã nhập kho", "bg-success"),
                InvInFGStatus.Cancelled => ("Đã hủy", "bg-secondary"),
                _ => ("Khác", "bg-light text-dark")
            };

            return new InventoryInFGRow(
                f.Id,
                f.Code,
                f.WarehouseId,
                f.Warehouse.Name,
                f.FormType,
                formLabel,
                f.WorkshopName,
                f.WorkOrderNo,
                f.ShiftLeader,
                f.Date,
                f.Status,
                statusLabel,
                badgeClass,
                f.TotalPlanQty,
                f.TotalActualQty,
                f.TotalDefectQty,
                f.PassRatePercent,
                f.TotalAmount,
                f.TotalSerialsCount,
                f.StockDocId,
                f.StockDoc?.Code,
                f.Remark,
                f.CreatedBy,
                f.CreatedAt,
                f.ApprovedAt,
                f.ApprovedBy
            );
        }).ToList();

        return new InventoryInFGReport(
            warehouseId,
            whName,
            status,
            formType,
            fromDate,
            toDate,
            q,
            totalReceipts,
            pendingCount,
            approvedCount,
            cancelledCount,
            totalPlanQty,
            totalActualQty,
            totalDefectQty,
            totalAmount,
            rows
        );
    }

    public Task<InventoryInFG?> GetInventoryInFGAsync(int id) =>
        db.InventoryInFGs
            .Include(f => f.Warehouse)
            .Include(f => f.StockDoc)
            .Include(f => f.Lines).ThenInclude(l => l.Product)
            .Include(f => f.Serials).ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

    public async Task<int> CreateInventoryInFGAsync(
        InventoryInFG doc,
        List<(int productId, int planQty, int actualQty, int defectQty, decimal unitCost, DateTime? prodDate, string? note)> lines,
        List<(int productId, string serialNo, string? note)> serials)
    {
        if (doc.WarehouseId <= 0) throw new InvalidOperationException("Vui lòng chọn kho thành phẩm.");
        if (string.IsNullOrWhiteSpace(doc.WorkshopName)) throw new InvalidOperationException("Vui lòng nhập phân xưởng / nhà máy sản xuất.");
        if (lines.Count == 0 || !lines.Any(l => l.productId > 0 && l.actualQty > 0))
            throw new InvalidOperationException("Cần ít nhất 1 dòng thành phẩm có số lượng nhập > 0.");

        if (string.IsNullOrWhiteSpace(doc.Code))
        {
            doc.Code = $"IFFG{DateTime.Now:yyMMdd}-{await db.InventoryInFGs.CountAsync() + 1:D3}";
        }

        doc.Status = InvInFGStatus.Pending;
        doc.CreatedAt = DateTime.Now;

        foreach (var l in lines.Where(x => x.productId > 0 && x.actualQty > 0))
        {
            var unitCost = l.unitCost;
            if (unitCost <= 0)
            {
                var prod = await db.Products.FirstOrDefaultAsync(p => p.Id == l.productId);
                if (prod != null && prod.CostPrice > 0) unitCost = prod.CostPrice;
            }

            doc.Lines.Add(new InventoryInFGLine
            {
                ProductId = l.productId,
                PlanQty = l.planQty > 0 ? l.planQty : l.actualQty,
                ActualQty = l.actualQty,
                DefectQty = l.defectQty >= 0 ? l.defectQty : 0,
                UnitCost = unitCost,
                ProductionDate = l.prodDate ?? doc.Date,
                Note = l.note?.Trim()
            });
        }

        foreach (var s in serials.Where(x => x.productId > 0 && !string.IsNullOrWhiteSpace(x.serialNo)))
        {
            doc.Serials.Add(new InventoryInFGSerial
            {
                ProductId = s.productId,
                SerialNo = s.serialNo.Trim(),
                Note = s.note?.Trim()
            });
        }

        db.InventoryInFGs.Add(doc);
        await db.SaveChangesAsync();
        return doc.Id;
    }

    public async Task<(bool ok, string msg)> ApproveInventoryInFGAsync(int id)
    {
        var doc = await db.InventoryInFGs
            .Include(f => f.Warehouse)
            .Include(f => f.Lines).ThenInclude(l => l.Product)
            .Include(f => f.Serials).ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (doc == null) return (false, "Không tìm thấy phiếu nhập kho thành phẩm.");
        if (doc.Status != InvInFGStatus.Pending) return (false, "Phiếu không ở trạng thái Chờ duyệt.");
        if (doc.Lines.Count == 0 || !doc.Lines.Any(l => l.ActualQty > 0))
            return (false, "Phiếu không có mặt hàng thành phẩm nào hợp lệ.");

        // Tạo StockDoc (Phiếu nhập kho) để tăng tồn kho và ghi sổ
        var stockDoc = new StockDoc
        {
            Type = DocType.In,
            ToWarehouseId = doc.WarehouseId,
            Date = doc.Date,
            RefNo = doc.Code,
            Note = $"Nhập kho thành phẩm theo phiếu {doc.Code} - Lệnh SX: {doc.WorkOrderNo ?? "—"} từ {doc.WorkshopName}",
            CreatedBy = doc.CreatedBy ?? "system",
            Status = DocStatus.Draft,
            CreatedAt = DateTime.Now
        };

        foreach (var line in doc.Lines.Where(l => l.ActualQty > 0))
        {
            stockDoc.Lines.Add(new StockDocLine
            {
                ProductId = line.ProductId,
                Quantity = line.ActualQty
            });
        }

        db.Docs.Add(stockDoc);
        await db.SaveChangesAsync();

        // Ghi sổ phiếu kho
        var (postOk, postMsg) = await PostDocAsync(stockDoc.Id);
        if (!postOk)
        {
            return (false, $"Lỗi ghi sổ phiếu nhập kho: {postMsg}");
        }

        doc.StockDocId = stockDoc.Id;
        doc.Status = InvInFGStatus.Approved;
        doc.ApprovedAt = DateTime.Now;
        doc.ApprovedBy = "admin";

        // Tự động đăng ký serial vào danh sách tồn kho khả dụng
        foreach (var s in doc.Serials)
        {
            var exists = await db.StockSerials.AnyAsync(ss =>
                ss.WarehouseId == doc.WarehouseId &&
                ss.ProductId == s.ProductId &&
                ss.SerialNo == s.SerialNo);

            if (!exists)
            {
                db.StockSerials.Add(new StockSerial
                {
                    WarehouseId = doc.WarehouseId,
                    ProductId = s.ProductId,
                    SerialNo = s.SerialNo,
                    Status = StockSerialStatus.Available,
                    InDate = doc.Date,
                    RefNo = doc.Code,
                    Note = $"Nhập thành phẩm từ {doc.WorkshopName} (Phiếu {doc.Code})",
                    CreatedAt = DateTime.Now
                });
            }
        }

        await db.SaveChangesAsync();
        return (true, $"Đã phê duyệt và nhập kho thành công phiếu {doc.Code}. Tổng {doc.TotalActualQty} thành phẩm đã vào kho {doc.Warehouse.Name}.");
    }

    public async Task<(bool ok, string msg)> CancelInventoryInFGAsync(int id)
    {
        var doc = await db.InventoryInFGs.FirstOrDefaultAsync(f => f.Id == id);
        if (doc == null) return (false, "Không tìm thấy phiếu nhập kho thành phẩm.");
        if (doc.Status == InvInFGStatus.Approved)
            return (false, "Phiếu nhập kho thành phẩm đã được phê duyệt ghi sổ kho, không thể hủy bỏ.");
        if (doc.Status == InvInFGStatus.Cancelled)
            return (false, "Phiếu này đã được hủy trước đó.");

        doc.Status = InvInFGStatus.Cancelled;
        await db.SaveChangesAsync();
        return (true, $"Đã hủy phiếu nhập kho thành phẩm {doc.Code}.");
    }

    public async Task<InventoryOutFGReport> InventoryOutFGsAsync(int? warehouseId, InvOutFGStatus? status, InvOutFGType? outType, InvOutFGFormType? formType, DateTime? fromDate, DateTime? toDate, string? q)
    {
        var query = db.InventoryOutFGs
            .Include(f => f.Warehouse)
            .Include(f => f.StockDoc)
            .Include(f => f.Lines).ThenInclude(l => l.Product)
            .Include(f => f.Serials).ThenInclude(s => s.Product)
            .AsQueryable();

        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            query = query.Where(f => f.WarehouseId == warehouseId.Value);
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        if (status.HasValue) query = query.Where(f => f.Status == status.Value);
        if (outType.HasValue) query = query.Where(f => f.OutType == outType.Value);
        if (formType.HasValue) query = query.Where(f => f.FormType == formType.Value);

        if (fromDate.HasValue)
        {
            var f = fromDate.Value.Date;
            query = query.Where(x => x.Date >= f);
        }
        if (toDate.HasValue)
        {
            var t = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.Date <= t);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(f => f.Code.ToLower().Contains(kw) ||
                                     f.CustomerName.ToLower().Contains(kw) ||
                                     (f.AgentCode != null && f.AgentCode.ToLower().Contains(kw)) ||
                                     (f.PlateNo != null && f.PlateNo.ToLower().Contains(kw)) ||
                                     (f.MoocNo != null && f.MoocNo.ToLower().Contains(kw)) ||
                                     (f.DriverName != null && f.DriverName.ToLower().Contains(kw)) ||
                                     (f.OrderNo != null && f.OrderNo.ToLower().Contains(kw)) ||
                                     (f.Remark != null && f.Remark.ToLower().Contains(kw)) ||
                                     f.Lines.Any(l => l.Product.Code.ToLower().Contains(kw) || l.Product.Name.ToLower().Contains(kw)));
        }

        var list = await query.OrderByDescending(f => f.Date).ThenByDescending(f => f.Id).ToListAsync();

        int totalOrders = list.Count;
        int pendingCount = list.Count(f => f.Status == InvOutFGStatus.Pending);
        int approvedCount = list.Count(f => f.Status == InvOutFGStatus.Approved);
        int cancelledCount = list.Count(f => f.Status == InvOutFGStatus.Cancelled);
        int totalQty = list.Sum(f => f.TotalQty);
        decimal totalAmount = list.Sum(f => f.TotalAmount);
        int totalSerials = list.Sum(f => f.TotalSerialsCount);

        var rows = list.Select(f =>
        {
            var (outLabel, _) = f.OutType switch
            {
                InvOutFGType.Commercial => ("Xuất thương mại (Đại lý)", "bg-primary"),
                InvOutFGType.EndCustomer => ("Xuất khách lẻ / Dự án", "bg-info text-dark"),
                InvOutFGType.BranchTransfer => ("Điều chuyển chi nhánh", "bg-secondary"),
                InvOutFGType.WarrantyScrap => ("Bảo hành / Thanh lý", "bg-warning text-dark"),
                _ => ("Khác", "bg-light text-dark")
            };

            var (formLabel, _) = f.FormType switch
            {
                InvOutFGFormType.QuantityOnly => ("Theo số lượng", "bg-light text-dark border"),
                InvOutFGFormType.BarcodeSerial => ("Quét Barcode/Serial", "bg-primary-subtle text-primary border border-primary-subtle"),
                _ => ("Khác", "bg-light text-dark")
            };

            var (statusLabel, badgeClass) = f.Status switch
            {
                InvOutFGStatus.Pending => ("Chờ duyệt xuất", "bg-warning text-dark"),
                InvOutFGStatus.Approved => ("Đã xuất kho", "bg-success"),
                InvOutFGStatus.Cancelled => ("Đã hủy", "bg-secondary"),
                _ => ("Khác", "bg-light text-dark")
            };

            return new InventoryOutFGRow(
                f.Id,
                f.Code,
                f.WarehouseId,
                f.Warehouse.Name,
                f.OutType,
                outLabel,
                f.FormType,
                formLabel,
                f.CustomerName,
                f.AgentCode,
                f.DeliveryAddress,
                f.DriverName,
                f.DriverPhone,
                f.PlateNo,
                f.MoocNo,
                f.OrderNo,
                f.Date,
                f.Status,
                statusLabel,
                badgeClass,
                f.TotalQty,
                f.TotalAmount,
                f.TotalSerialsCount,
                f.StockDocId,
                f.StockDoc?.Code,
                f.Remark,
                f.CreatedBy,
                f.CreatedAt,
                f.ApprovedAt,
                f.ApprovedBy
            );
        }).ToList();

        return new InventoryOutFGReport(
            warehouseId,
            whName,
            status,
            outType,
            formType,
            fromDate,
            toDate,
            q,
            totalOrders,
            pendingCount,
            approvedCount,
            cancelledCount,
            totalQty,
            totalAmount,
            totalSerials,
            rows
        );
    }

    public Task<InventoryOutFG?> GetInventoryOutFGAsync(int id) =>
        db.InventoryOutFGs
            .Include(f => f.Warehouse)
            .Include(f => f.StockDoc)
            .Include(f => f.Lines).ThenInclude(l => l.Product)
            .Include(f => f.Serials).ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

    public async Task<int> CreateInventoryOutFGAsync(
        InventoryOutFG doc,
        List<(int productId, int qty, decimal unitPrice, decimal unitCost, string? note)> lines,
        List<(int productId, string serialNo, string? note)> serials)
    {
        if (doc.WarehouseId <= 0) throw new InvalidOperationException("Vui lòng chọn kho xuất thành phẩm.");
        if (string.IsNullOrWhiteSpace(doc.CustomerName)) throw new InvalidOperationException("Vui lòng nhập tên khách hàng / đại lý nhận hàng.");
        if (lines.Count == 0 || !lines.Any(l => l.productId > 0 && l.qty > 0))
            throw new InvalidOperationException("Cần ít nhất 1 dòng thành phẩm có số lượng xuất > 0.");

        if (string.IsNullOrWhiteSpace(doc.Code))
        {
            doc.Code = $"IFOFG{DateTime.Now:yyMMdd}-{await db.InventoryOutFGs.CountAsync() + 1:D3}";
        }

        doc.Status = InvOutFGStatus.Pending;
        doc.CreatedAt = DateTime.Now;

        foreach (var l in lines.Where(x => x.productId > 0 && x.qty > 0))
        {
            var unitCost = l.unitCost;
            if (unitCost <= 0)
            {
                var prod = await db.Products.FirstOrDefaultAsync(p => p.Id == l.productId);
                if (prod != null && prod.CostPrice > 0) unitCost = prod.CostPrice;
            }

            doc.Lines.Add(new InventoryOutFGLine
            {
                ProductId = l.productId,
                Qty = l.qty,
                UnitPrice = l.unitPrice,
                UnitCost = unitCost,
                Note = l.note?.Trim()
            });
        }

        foreach (var s in serials.Where(x => x.productId > 0 && !string.IsNullOrWhiteSpace(x.serialNo)))
        {
            doc.Serials.Add(new InventoryOutFGSerial
            {
                ProductId = s.productId,
                SerialNo = s.serialNo.Trim(),
                Note = s.note?.Trim()
            });
        }

        db.InventoryOutFGs.Add(doc);
        await db.SaveChangesAsync();
        return doc.Id;
    }

    public async Task<(bool ok, string msg)> ApproveInventoryOutFGAsync(int id)
    {
        var doc = await db.InventoryOutFGs
            .Include(f => f.Warehouse)
            .Include(f => f.Lines).ThenInclude(l => l.Product)
            .Include(f => f.Serials).ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (doc == null) return (false, "Không tìm thấy phiếu xuất kho thành phẩm.");
        if (doc.Status != InvOutFGStatus.Pending) return (false, "Phiếu không ở trạng thái Chờ duyệt.");
        if (doc.Lines.Count == 0 || !doc.Lines.Any(l => l.Qty > 0))
            return (false, "Phiếu không có mặt hàng thành phẩm nào hợp lệ.");

        // Kiểm tra tồn khả dụng tại kho xuất trước khi trừ
        var bal = await BalancesAsync(doc.WarehouseId);
        foreach (var line in doc.Lines.Where(l => l.Qty > 0))
        {
            var have = bal.FirstOrDefault(x => x.ProductId == line.ProductId)?.Qty ?? 0;
            if (line.Qty > have)
            {
                return (false, $"Kho {doc.Warehouse.Name} không đủ tồn cho sản phẩm '{line.Product.Name}' (Cần {line.Qty}, tồn thực tế {have}).");
            }
        }

        // Tạo StockDoc (Phiếu xuất kho) để ghi sổ và giảm tồn kho
        var stockDoc = new StockDoc
        {
            Type = DocType.Out,
            FromWarehouseId = doc.WarehouseId,
            Date = doc.Date,
            RefNo = doc.Code,
            Note = $"Xuất kho thành phẩm theo phiếu {doc.Code} cho {doc.CustomerName} - Xe: {doc.PlateNo ?? "—"}",
            CreatedBy = doc.CreatedBy ?? "system",
            Status = DocStatus.Draft,
            CreatedAt = DateTime.Now
        };

        foreach (var line in doc.Lines.Where(l => l.Qty > 0))
        {
            stockDoc.Lines.Add(new StockDocLine
            {
                ProductId = line.ProductId,
                Quantity = line.Qty
            });
        }

        db.Docs.Add(stockDoc);
        await db.SaveChangesAsync();

        // Ghi sổ phiếu xuất kho
        var (postOk, postMsg) = await PostDocAsync(stockDoc.Id);
        if (!postOk)
        {
            return (false, $"Lỗi ghi sổ phiếu xuất kho: {postMsg}");
        }

        doc.StockDocId = stockDoc.Id;
        doc.Status = InvOutFGStatus.Approved;
        doc.ApprovedAt = DateTime.Now;
        doc.ApprovedBy = "admin";

        // Cập nhật trạng thái Serial thành Exported (Đã xuất kho)
        foreach (var s in doc.Serials)
        {
            var existingSerial = await db.StockSerials.FirstOrDefaultAsync(ss =>
                ss.WarehouseId == doc.WarehouseId &&
                ss.ProductId == s.ProductId &&
                ss.SerialNo == s.SerialNo);

            if (existingSerial != null)
            {
                existingSerial.Status = StockSerialStatus.Exported;
                existingSerial.OutDate = doc.Date;
                existingSerial.RefNo = doc.Code;
                existingSerial.Note = $"Đã xuất cho {doc.CustomerName} (Xe {doc.PlateNo ?? "—"})";
                existingSerial.UpdatedAt = DateTime.Now;
            }
            else
            {
                db.StockSerials.Add(new StockSerial
                {
                    WarehouseId = doc.WarehouseId,
                    ProductId = s.ProductId,
                    SerialNo = s.SerialNo,
                    Status = StockSerialStatus.Exported,
                    InDate = doc.Date,
                    OutDate = doc.Date,
                    RefNo = doc.Code,
                    Note = $"Đã xuất cho {doc.CustomerName} (Xe {doc.PlateNo ?? "—"})",
                    CreatedAt = DateTime.Now
                });
            }
        }

        await db.SaveChangesAsync();
        return (true, $"Đã duyệt và xuất kho thành công phiếu {doc.Code}. Tổng {doc.TotalQty} thành phẩm đã được xuất giao cho {doc.CustomerName}.");
    }

    public async Task<(bool ok, string msg)> CancelInventoryOutFGAsync(int id)
    {
        var doc = await db.InventoryOutFGs.FirstOrDefaultAsync(f => f.Id == id);
        if (doc == null) return (false, "Không tìm thấy phiếu xuất kho thành phẩm.");
        if (doc.Status == InvOutFGStatus.Approved)
            return (false, "Phiếu xuất kho thành phẩm đã được phê duyệt ghi sổ kho, không thể hủy bỏ.");
        if (doc.Status == InvOutFGStatus.Cancelled)
            return (false, "Phiếu này đã được hủy trước đó.");

        doc.Status = InvOutFGStatus.Cancelled;
        await db.SaveChangesAsync();
        return (true, $"Đã hủy phiếu xuất kho thành phẩm {doc.Code}.");
    }

    /// <summary>Báo cáo Tổng hợp Nhập mua & Trả hàng nhà cung cấp (port từ Rpt_Summary_InAndReturnSup Skycic).</summary>
    public async Task<SummaryInReturnSupReport> SummaryInReturnSupReportAsync(
        int? warehouseId,
        string? supplierCode,
        DateTime? fromDate,
        DateTime? toDate,
        string? keyword)
    {
        var fDate = fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var tDate = toDate ?? DateTime.Today;
        var toDateEnd = tDate.Date.AddDays(1).AddTicks(-1);

        string whName = "Tất cả kho";
        if (warehouseId.HasValue && warehouseId.Value > 0)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        // 1. Lấy tất cả phiếu nhập kho (DocType.In) đã ghi sổ trong khoảng thời gian
        var inDocQuery = db.Docs
            .Include(d => d.Lines)
            .Where(d => d.Type == DocType.In && d.Status == DocStatus.Posted && d.Date >= fDate && d.Date <= toDateEnd);

        if (warehouseId.HasValue && warehouseId.Value > 0)
            inDocQuery = inDocQuery.Where(d => d.ToWarehouseId == warehouseId.Value);

        if (!string.IsNullOrWhiteSpace(supplierCode))
            inDocQuery = inDocQuery.Where(d => d.SupplierCode == supplierCode);

        var inDocs = await inDocQuery.ToListAsync();

        // 2. Lấy tất cả phiếu xuất trả hàng NCC (ReturnToSupplier) đã xuất trả trong khoảng thời gian
        var retDocQuery = db.ReturnToSuppliers
            .Include(r => r.Lines)
            .Where(r => r.Status == ReturnSupStatus.Finished && r.Date >= fDate && r.Date <= toDateEnd);

        if (warehouseId.HasValue && warehouseId.Value > 0)
            retDocQuery = retDocQuery.Where(r => r.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrWhiteSpace(supplierCode))
            retDocQuery = retDocQuery.Where(r => r.SupplierCode == supplierCode);

        var retDocs = await retDocQuery.ToListAsync();

        // Lấy thông tin Products và Suppliers
        var prods = await db.Products.ToDictionaryAsync(p => p.Id);
        var knownSuppliers = await db.Suppliers.ToListAsync();
        var supDict = knownSuppliers.ToDictionary(s => s.Code, s => s.Name, StringComparer.OrdinalIgnoreCase);

        // Group Inbound: key = (SupplierCode, ProductId)
        var inMap = new Dictionary<(string SupCode, int ProdId), (string SupName, int Qty, decimal Amount)>();
        foreach (var d in inDocs)
        {
            var sCode = !string.IsNullOrWhiteSpace(d.SupplierCode) ? d.SupplierCode.Trim() : "NCC-GEN";
            var sName = !string.IsNullOrWhiteSpace(d.SupplierName)
                ? d.SupplierName.Trim()
                : (supDict.TryGetValue(sCode, out var name) ? name : "Nhà cung cấp chung");

            foreach (var l in d.Lines)
            {
                var p = prods.GetValueOrDefault(l.ProductId);
                var cost = p?.CostPrice ?? 0;
                var key = (sCode, l.ProductId);
                if (inMap.TryGetValue(key, out var cur))
                {
                    inMap[key] = (sName, cur.Qty + l.Quantity, cur.Amount + (l.Quantity * cost));
                }
                else
                {
                    inMap[key] = (sName, l.Quantity, l.Quantity * cost);
                }
            }
        }

        // Group Returns: key = (SupplierCode, ProductId)
        var retMap = new Dictionary<(string SupCode, int ProdId), (string SupName, int Qty, decimal Amount)>();
        foreach (var r in retDocs)
        {
            string sCode = !string.IsNullOrWhiteSpace(r.SupplierCode) ? r.SupplierCode.Trim() : "NCC-GEN";
            string sName = !string.IsNullOrWhiteSpace(r.SupplierName)
                ? r.SupplierName.Trim()
                : (supDict.TryGetValue(sCode, out var name) ? name : "Nhà cung cấp chung");

            foreach (var l in r.Lines)
            {
                (string SupCode, int ProdId) key = (sCode, l.ProductId);
                var amt = l.Quantity * l.UnitPrice;
                if (retMap.TryGetValue(key, out var cur))
                {
                    retMap[key] = (sName, cur.Qty + l.Quantity, cur.Amount + amt);
                }
                else
                {
                    retMap[key] = (sName, l.Quantity, amt);
                }
            }
        }

        // Hợp nhất các cặp (SupCode, ProdId)
        var allKeys = inMap.Keys.Union(retMap.Keys).ToList();
        var rawRows = new List<SummaryInReturnSupRow>();

        foreach (var key in allKeys)
        {
            inMap.TryGetValue(key, out var inVal);
            retMap.TryGetValue(key, out var retVal);

            var sName = !string.IsNullOrWhiteSpace(inVal.SupName) ? inVal.SupName : (!string.IsNullOrWhiteSpace(retVal.SupName) ? retVal.SupName : key.SupCode);
            var p = prods.GetValueOrDefault(key.ProdId);
            var pCode = p?.Code ?? $"SP-{key.ProdId}";
            var pName = p?.Name ?? "Sản phẩm";
            var uom = p?.Uom ?? "cái";

            var inQty = inVal.Qty;
            var inAmt = inVal.Amount;
            var retQty = retVal.Qty;
            var retAmt = retVal.Amount;
            var netQty = inQty - retQty;
            var netAmt = inAmt - retAmt;

            double returnRate = inQty > 0 ? Math.Round((double)retQty / inQty * 100, 2) : (retQty > 0 ? 100.0 : 0.0);

            string grade;
            string badge;
            if (returnRate <= 2.0)
            {
                grade = "Tốt (Tỷ lệ trả ≤ 2%)";
                badge = "bg-success";
            }
            else if (returnRate <= 5.0)
            {
                grade = "Cảnh báo (2% - 5%)";
                badge = "bg-warning text-dark";
            }
            else
            {
                grade = "Kém (Tỷ lệ trả > 5%)";
                badge = "bg-danger";
            }

            rawRows.Add(new SummaryInReturnSupRow(
                key.SupCode,
                sName,
                key.ProdId,
                pCode,
                pName,
                uom,
                inQty,
                inAmt,
                retQty,
                retAmt,
                netQty,
                netAmt,
                returnRate,
                0,
                grade,
                badge
            ));
        }

        // Lọc theo từ khóa tìm kiếm (Mã/Tên NCC hoặc Mã/Tên sản phẩm)
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            rawRows = rawRows.Where(r =>
                r.SupplierCode.ToLowerInvariant().Contains(kw) ||
                r.SupplierName.ToLowerInvariant().Contains(kw) ||
                r.ProductCode.ToLowerInvariant().Contains(kw) ||
                r.ProductName.ToLowerInvariant().Contains(kw)
            ).ToList();
        }

        int totalNetAll = rawRows.Sum(r => Math.Max(0, r.NetQty));
        var finalRows = rawRows.Select(r =>
        {
            double share = totalNetAll > 0 ? Math.Round((double)Math.Max(0, r.NetQty) / totalNetAll * 100, 2) : 0.0;
            return r with { SharePercent = share };
        })
        .OrderByDescending(r => r.InQty)
        .ThenBy(r => r.SupplierName)
        .ToList();

        int totalInQty = finalRows.Sum(r => r.InQty);
        decimal totalInAmount = finalRows.Sum(r => r.InAmount);
        int totalRetQty = finalRows.Sum(r => r.ReturnQty);
        decimal totalRetAmount = finalRows.Sum(r => r.ReturnAmount);
        int totalNetQty = totalInQty - totalRetQty;
        decimal totalNetAmount = totalInAmount - totalRetAmount;
        double avgRetRate = totalInQty > 0 ? Math.Round((double)totalRetQty / totalInQty * 100, 2) : 0.0;
        int supCount = finalRows.Select(r => r.SupplierCode).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        int prodCount = finalRows.Select(r => r.ProductId).Distinct().Count();

        return new SummaryInReturnSupReport(
            warehouseId,
            whName,
            supplierCode,
            fDate,
            tDate,
            keyword,
            totalInQty,
            totalInAmount,
            totalRetQty,
            totalRetAmount,
            totalNetQty,
            totalNetAmount,
            avgRetRate,
            supCount,
            prodCount,
            finalRows
        );
    }

    /// <summary>Danh sách Danh mục Nhà cung cấp (port từ Mst_Supplier Skycic).</summary>
    public async Task<List<Supplier>> SuppliersAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.Suppliers.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(s => s.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(s => s.Code.ToLower().Contains(kw) || s.Name.ToLower().Contains(kw) || (s.Phone != null && s.Phone.Contains(kw)));
        }
        return await query.OrderBy(s => s.Code).ToListAsync();
    }

    public Task<Supplier?> GetSupplierAsync(int id) => db.Suppliers.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<int> CreateSupplierAsync(Supplier supplier)
    {
        if (string.IsNullOrWhiteSpace(supplier.Code))
            supplier.Code = $"NCC{await db.Suppliers.CountAsync() + 1:D3}";
        supplier.CreatedAt = DateTime.Now;
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return supplier.Id;
    }

    public async Task<(bool ok, string msg)> UpdateSupplierAsync(int id, Supplier supplier)
    {
        var existing = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        if (existing == null) return (false, "Không tìm thấy nhà cung cấp.");
        existing.Name = supplier.Name.Trim();
        existing.ContactName = supplier.ContactName?.Trim();
        existing.Phone = supplier.Phone?.Trim();
        existing.Email = supplier.Email?.Trim();
        existing.Address = supplier.Address?.Trim();
        existing.TaxCode = supplier.TaxCode?.Trim();
        existing.Note = supplier.Note?.Trim();
        existing.IsActive = supplier.IsActive;
        await db.SaveChangesAsync();
        return (true, "Đã cập nhật nhà cung cấp.");
    }

    public async Task<(bool ok, string msg)> ToggleSupplierStatusAsync(int id)
    {
        var existing = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id);
        if (existing == null) return (false, "Không tìm thấy nhà cung cấp.");
        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? "Đã kích hoạt nhà cung cấp." : "Đã tạm dừng nhà cung cấp.");
    }

    /// <summary>Danh sách Danh mục Khách hàng, Đại lý phân phối (port từ Mst_Customer Skycic).</summary>
    public async Task<List<Customer>> CustomersAsync(string? q = null, string? customerType = null, bool? activeOnly = null, string? customerGrpCode = null, string? customerSourceCode = null)
    {
        var query = db.Customers.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(c => c.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(customerType)) query = query.Where(c => c.CustomerType == customerType.Trim());
        if (!string.IsNullOrWhiteSpace(customerGrpCode)) query = query.Where(c => c.CustomerGrpCode != null && c.CustomerGrpCode.ToLower() == customerGrpCode.Trim().ToLower());
        if (!string.IsNullOrWhiteSpace(customerSourceCode)) query = query.Where(c => c.CustomerSourceCode != null && c.CustomerSourceCode.ToLower() == customerSourceCode.Trim().ToLower());
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(c => c.Code.ToLower().Contains(kw) ||
                                     c.Name.ToLower().Contains(kw) ||
                                     (c.CustomerGrpCode != null && c.CustomerGrpCode.ToLower().Contains(kw)) ||
                                     (c.CustomerSourceCode != null && c.CustomerSourceCode.ToLower().Contains(kw)) ||
                                     (c.Phone != null && c.Phone.Contains(kw)) ||
                                     (c.Email != null && c.Email.ToLower().Contains(kw)) ||
                                     (c.ContactName != null && c.ContactName.ToLower().Contains(kw)) ||
                                     (c.Province != null && c.Province.ToLower().Contains(kw)));
        }
        return await query.OrderBy(c => c.Code).ToListAsync();
    }

    public Task<Customer?> GetCustomerAsync(int id) => db.Customers.FirstOrDefaultAsync(c => c.Id == id);

    public Task<Customer?> GetCustomerByCodeAsync(string code) =>
        db.Customers.FirstOrDefaultAsync(c => c.Code.ToLower() == code.Trim().ToLower());

    public async Task<int> CreateCustomerAsync(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Code))
            customer.Code = $"KH{await db.Customers.CountAsync() + 1:D3}";
        customer.CreatedAt = DateTime.Now;
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer.Id;
    }

    public async Task<(bool ok, string msg)> UpdateCustomerAsync(int id, Customer customer)
    {
        var existing = await db.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (existing == null) return (false, "Không tìm thấy khách hàng.");
        existing.Name = customer.Name.Trim();
        existing.CustomerType = string.IsNullOrWhiteSpace(customer.CustomerType) ? "Đại lý phân phối" : customer.CustomerType.Trim();
        existing.ContactName = customer.ContactName?.Trim();
        existing.ContactPhone = customer.ContactPhone?.Trim();
        existing.Phone = customer.Phone?.Trim();
        existing.Email = customer.Email?.Trim();
        existing.Address = customer.Address?.Trim();
        existing.Province = customer.Province?.Trim();
        existing.AreaCode = customer.AreaCode;
        existing.CustomerGrpCode = customer.CustomerGrpCode;
        existing.CustomerSourceCode = customer.CustomerSourceCode;
        existing.TaxCode = customer.TaxCode?.Trim();
        existing.Note = customer.Note?.Trim();
        existing.IsActive = customer.IsActive;
        await db.SaveChangesAsync();
        return (true, "Đã cập nhật thông tin khách hàng.");
    }

    public async Task<(bool ok, string msg)> ToggleCustomerStatusAsync(int id)
    {
        var existing = await db.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (existing == null) return (false, "Không tìm thấy khách hàng.");
        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? "Đã kích hoạt khách hàng." : "Đã tạm dừng giao dịch với khách hàng.");
    }

    public async Task<(bool ok, string msg)> DeleteCustomerAsync(int id)
    {
        var existing = await db.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (existing == null) return (false, "Không tìm thấy khách hàng.");

        bool hasStockDoc = await db.Docs.AnyAsync(d => d.CustomerCode == existing.Code);
        bool hasOutFG = await db.InventoryOutFGs.AnyAsync(f => f.AgentCode == existing.Code || f.CustomerName == existing.Name);
        bool hasCusReturn = await db.CustomerReturns.AnyAsync(r => r.CustomerCode == existing.Code || r.CustomerName == existing.Name);

        if (hasStockDoc || hasOutFG || hasCusReturn)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            return (true, "Khách hàng đã có lịch sử giao dịch kho nên được chuyển sang trạng thái Tạm dừng thay vì xóa hẳn.");
        }

        db.Customers.Remove(existing);
        await db.SaveChangesAsync();
        return (true, "Đã xóa khách hàng.");
    }

    public async Task<CustomerDetailDto?> GetCustomerDetailAsync(int id)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return null;

        var outDocs = await db.Docs
            .Where(d => d.Type == DocType.Out && (d.CustomerCode == customer.Code || d.CustomerName == customer.Name))
            .Include(d => d.FromWarehouse)
            .Include(d => d.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(d => d.Date)
            .ToListAsync();

        var outFGDocs = await db.InventoryOutFGs
            .Where(f => f.AgentCode == customer.Code || f.CustomerName == customer.Name)
            .Include(f => f.Warehouse)
            .Include(f => f.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(f => f.Date)
            .ToListAsync();

        var returns = await db.CustomerReturns
            .Where(r => r.CustomerCode == customer.Code || r.CustomerName == customer.Name)
            .Include(r => r.Warehouse)
            .Include(r => r.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(r => r.Date)
            .ToListAsync();

        int totalOutQty = outDocs.Sum(d => d.TotalQty) + outFGDocs.Sum(f => f.TotalQty);
        int totalReturnQty = returns.Sum(r => r.TotalQty);

        return new CustomerDetailDto(customer, outDocs, outFGDocs, returns, totalOutQty, totalReturnQty);
    }

    /// <summary>Báo cáo tổng hợp xuất kho chi tiết (port từ Rpt_InvF_InventoryOutDtl Skycic).</summary>
    public async Task<InventoryOutDtlReport> InventoryOutDtlReportAsync(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? outType, string? keyword)
    {
        var start = (fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var end = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var allProducts = await db.Products.ToListAsync();
        var prodDict = allProducts.ToDictionary(p => p.Id, p => p);

        // Giá vốn hiện hành để làm fallback cho đơn giá xuất
        var costHists = await db.CostPriceHists
            .Where(c => c.IsCurrent)
            .OrderByDescending(c => c.WarehouseId.HasValue)
            .ThenByDescending(c => c.EffectDate)
            .ToListAsync();

        decimal ResolveCost(int prodId, int? whId)
        {
            var match = costHists.FirstOrDefault(c => c.ProductId == prodId && (c.WarehouseId == whId || !c.WarehouseId.HasValue));
            if (match != null && match.CostPrice > 0) return match.CostPrice;
            if (prodDict.TryGetValue(prodId, out var prod) && prod.CostPrice > 0) return prod.CostPrice;
            return 0m;
        }

        var rawItems = new List<InventoryOutDtlItem>();
        int seq = 1;

        // 1. Nguồn StockDoc (Posted Out & Transfer)
        var docs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted && d.Date >= start && d.Date <= end &&
                        (d.Type == DocType.Out || d.Type == DocType.Transfer))
            .Include(d => d.Lines)
            .Include(d => d.FromWarehouse)
            .Include(d => d.ToWarehouse)
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.Id)
            .ToListAsync();

        // Nạp bảng tham chiếu để đối chiếu RefNo và StockDocId
        var retSups = await db.ReturnToSuppliers.Include(r => r.Lines).ToListAsync();
        var retDictByDocId = retSups.Where(r => r.StockDocId.HasValue).ToDictionary(r => r.StockDocId!.Value, r => r);
        var retDictByCode = retSups.ToDictionary(r => r.Code, r => r);

        var outFGs = await db.InventoryOutFGs.Include(f => f.Lines).ToListAsync();
        var fgDictByDocId = outFGs.Where(f => f.StockDocId.HasValue).ToDictionary(f => f.StockDocId!.Value, f => f);
        var fgDictByCode = outFGs.ToDictionary(f => f.Code, f => f);

        var audits = await db.Audits.Include(a => a.Lines).ToListAsync();
        var auditDictByDocId = audits.Where(a => a.OutDocId.HasValue).ToDictionary(a => a.OutDocId!.Value, a => a);
        var auditDictByCode = audits.ToDictionary(a => a.Code, a => a);

        foreach (var doc in docs)
        {
            int whId = doc.FromWarehouseId ?? 0;
            if (warehouseId.HasValue && whId != warehouseId.Value) continue;

            string whDocName = doc.FromWarehouse?.Name ?? "Kho xuất";

            if (doc.Type == DocType.Out)
            {
                // Kiểm tra loại nghiệp vụ cụ thể
                ReturnToSupplier? ret = null;
                if (retDictByDocId.TryGetValue(doc.Id, out var r1)) ret = r1;
                else if (!string.IsNullOrEmpty(doc.RefNo) && retDictByCode.TryGetValue(doc.RefNo, out var r2)) ret = r2;

                InventoryOutFG? fg = null;
                if (fgDictByDocId.TryGetValue(doc.Id, out var f1)) fg = f1;
                else if (!string.IsNullOrEmpty(doc.RefNo) && fgDictByCode.TryGetValue(doc.RefNo, out var f2)) fg = f2;

                StockAudit? audit = null;
                if (auditDictByDocId.TryGetValue(doc.Id, out var a1)) audit = a1;
                else if (!string.IsNullOrEmpty(doc.RefNo) && auditDictByCode.TryGetValue(doc.RefNo, out var a2)) audit = a2;

                string oType;
                string oTypeName;
                string? refNo = doc.RefNo;
                string? refType;
                string? cusCode = null;
                string cusName;
                string docUrl;

                if (ret != null)
                {
                    oType = "RETURNSUP";
                    oTypeName = "Xuất trả hàng NCC";
                    cusName = ret.SupplierName;
                    cusCode = ret.SupplierCode;
                    refNo = ret.Code;
                    refType = "Phiếu trả NCC";
                    docUrl = $"/ReturnSup/Detail/{ret.Id}";
                }
                else if (fg != null)
                {
                    oType = "OUT_FG";
                    oTypeName = "Xuất thành phẩm";
                    cusName = fg.CustomerName;
                    cusCode = fg.AgentCode;
                    refNo = fg.Code;
                    refType = !string.IsNullOrWhiteSpace(fg.OrderNo) ? $"Đơn hàng {fg.OrderNo}" : "Lệnh xuất TP";
                    docUrl = $"/InventoryOutFG/Detail/{fg.Id}";
                }
                else if (audit != null)
                {
                    oType = "AUDIT_DIFF";
                    oTypeName = "Xuất cân bằng kiểm kê";
                    cusName = "Hao hụt kiểm kê kho";
                    refNo = audit.Code;
                    refType = "Biên bản kiểm kê";
                    docUrl = $"/Audit/Detail/{audit.Id}";
                }
                else
                {
                    oType = "COMMERCIAL";
                    oTypeName = "Xuất bán buôn / Thương mại";
                    cusName = !string.IsNullOrWhiteSpace(doc.SupplierName) ? doc.SupplierName : "Khách hàng mua buôn";
                    refType = "Hóa đơn / Đơn hàng";
                    docUrl = $"/Doc/Detail/{doc.Id}";
                }

                foreach (var line in doc.Lines)
                {
                    if (!prodDict.TryGetValue(line.ProductId, out var prod)) continue;
                    decimal up = 0m;
                    if (ret != null)
                    {
                        var rl = ret.Lines.FirstOrDefault(x => x.ProductId == line.ProductId);
                        if (rl != null && rl.UnitPrice > 0) up = rl.UnitPrice;
                    }
                    else if (fg != null)
                    {
                        var fl = fg.Lines.FirstOrDefault(x => x.ProductId == line.ProductId);
                        if (fl != null && fl.UnitPrice > 0) up = fl.UnitPrice;
                    }
                    if (up == 0m) up = ResolveCost(line.ProductId, whId);

                    rawItems.Add(new InventoryOutDtlItem(
                        seq++,
                        doc.Code,
                        doc.Date,
                        oType,
                        oTypeName,
                        refNo,
                        refType,
                        whId,
                        whDocName,
                        cusCode,
                        cusName,
                        prod.Id,
                        prod.Code,
                        prod.Name,
                        prod.Uom,
                        line.Quantity,
                        up,
                        line.Quantity * up,
                        doc.CreatedBy,
                        doc.Note,
                        docUrl
                    ));
                }
            }
            else if (doc.Type == DocType.Transfer)
            {
                string oType = "TRANSFER";
                string oTypeName = "Xuất điều chuyển kho";
                string cusName = doc.ToWarehouse != null ? $"Kho đích: {doc.ToWarehouse.Name}" : "Chuyển nội bộ";
                string? refType = "Lệnh chuyển kho";
                string docUrl = $"/Doc/Detail/{doc.Id}";

                foreach (var line in doc.Lines)
                {
                    if (!prodDict.TryGetValue(line.ProductId, out var prod)) continue;
                    decimal up = ResolveCost(line.ProductId, whId);

                    rawItems.Add(new InventoryOutDtlItem(
                        seq++,
                        doc.Code,
                        doc.Date,
                        oType,
                        oTypeName,
                        doc.RefNo,
                        refType,
                        whId,
                        whDocName,
                        null,
                        cusName,
                        prod.Id,
                        prod.Code,
                        prod.Name,
                        prod.Uom,
                        line.Quantity,
                        up,
                        line.Quantity * up,
                        doc.CreatedBy,
                        doc.Note,
                        docUrl
                    ));
                }
            }
        }

        // Bổ sung các phiếu InventoryOutFG đã Approved nếu chưa link StockDoc
        var unlinkedOutFGs = outFGs
            .Where(f => f.Status == InvOutFGStatus.Approved && !f.StockDocId.HasValue &&
                        f.Date >= start && f.Date <= end &&
                        (!warehouseId.HasValue || f.WarehouseId == warehouseId.Value))
            .ToList();

        foreach (var fg in unlinkedOutFGs)
        {
            var whTitle = fg.Warehouse?.Name ?? "Kho xuất";
            foreach (var line in fg.Lines)
            {
                if (!prodDict.TryGetValue(line.ProductId, out var prod)) continue;
                decimal up = line.UnitPrice > 0 ? line.UnitPrice : ResolveCost(line.ProductId, fg.WarehouseId);

                rawItems.Add(new InventoryOutDtlItem(
                    seq++,
                    fg.Code,
                    fg.Date,
                    "OUT_FG",
                    "Xuất thành phẩm",
                    fg.OrderNo,
                    "Lệnh xuất TP",
                    fg.WarehouseId,
                    whTitle,
                    fg.AgentCode,
                    fg.CustomerName,
                    prod.Id,
                    prod.Code,
                    prod.Name,
                    prod.Uom,
                    line.Qty,
                    up,
                    line.Qty * up,
                    fg.CreatedBy ?? "system",
                    fg.Remark,
                    $"/InventoryOutFG/Detail/{fg.Id}"
                ));
            }
        }

        // Bổ sung các phiếu ReturnToSupplier đã Finished nếu chưa link StockDoc
        var unlinkedRetSups = retSups
            .Where(r => r.Status == ReturnSupStatus.Finished && !r.StockDocId.HasValue &&
                        r.Date >= start && r.Date <= end &&
                        (!warehouseId.HasValue || r.WarehouseId == warehouseId.Value))
            .ToList();

        foreach (var ret in unlinkedRetSups)
        {
            var whTitle = ret.Warehouse?.Name ?? "Kho xuất";
            foreach (var line in ret.Lines)
            {
                if (!prodDict.TryGetValue(line.ProductId, out var prod)) continue;
                decimal up = line.UnitPrice > 0 ? line.UnitPrice : ResolveCost(line.ProductId, ret.WarehouseId);

                rawItems.Add(new InventoryOutDtlItem(
                    seq++,
                    ret.Code,
                    ret.Date,
                    "RETURNSUP",
                    "Xuất trả hàng NCC",
                    ret.RefDocNo,
                    "Phiếu trả NCC",
                    ret.WarehouseId,
                    whTitle,
                    ret.SupplierCode,
                    ret.SupplierName,
                    prod.Id,
                    prod.Code,
                    prod.Name,
                    prod.Uom,
                    line.Quantity,
                    up,
                    line.Quantity * up,
                    ret.CreatedBy,
                    ret.Reason,
                    $"/ReturnSup/Detail/{ret.Id}"
                ));
            }
        }

        // Lọc theo loại xuất (outType)
        var filtered = rawItems.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(outType))
        {
            filtered = filtered.Where(i => i.OutType.Equals(outType.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // Lọc theo từ khóa (keyword)
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            filtered = filtered.Where(i =>
                i.DocNo.ToLower().Contains(kw) ||
                (i.RefNo != null && i.RefNo.ToLower().Contains(kw)) ||
                i.ProductCode.ToLower().Contains(kw) ||
                i.ProductName.ToLower().Contains(kw) ||
                i.CustomerName.ToLower().Contains(kw) ||
                (i.CustomerCode != null && i.CustomerCode.ToLower().Contains(kw)) ||
                i.WarehouseName.ToLower().Contains(kw) ||
                i.CreatedBy.ToLower().Contains(kw) ||
                (i.Note != null && i.Note.ToLower().Contains(kw)));
        }

        var finalItems = filtered.OrderByDescending(i => i.DocDate).ThenByDescending(i => i.Id).ToList();

        int totalDocs = finalItems.Select(i => i.DocNo).Distinct().Count();
        int totalQty = finalItems.Sum(i => i.Quantity);
        decimal totalCost = finalItems.Sum(i => i.TotalAmount);
        int distinctProds = finalItems.Select(i => i.ProductId).Distinct().Count();

        return new InventoryOutDtlReport(
            start,
            end.Date,
            warehouseId,
            whName,
            outType,
            keyword,
            finalItems,
            totalDocs,
            totalQty,
            totalCost,
            distinctProds
        );
    }

    /// <summary>Báo cáo tổng hợp nhập kho chi tiết (port từ Rpt_InventoryInDtl Skycic).</summary>
    public async Task<InventoryInDtlReport> InventoryInDtlReportAsync(int? warehouseId, DateTime? fromDate, DateTime? toDate, string? inType, string? keyword)
    {
        var start = (fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var end = (toDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var allProducts = await db.Products.ToListAsync();
        var prodDict = allProducts.ToDictionary(p => p.Id, p => p);

        var allSuppliers = await db.Suppliers.ToListAsync();
        var supDictByCode = allSuppliers.ToDictionary(s => s.Code, s => s);

        var allBlocks = await db.InventoryBlocks.Include(b => b.Warehouse).ToListAsync();
        var blockDictByWh = allBlocks.GroupBy(b => b.WarehouseId).ToDictionary(g => g.Key, g => g.FirstOrDefault()?.InvBlockCode ?? "A-01-01");

        // Giá vốn hiện hành để làm fallback cho đơn giá nhập
        var costHists = await db.CostPriceHists
            .Where(c => c.IsCurrent)
            .OrderByDescending(c => c.WarehouseId.HasValue)
            .ThenByDescending(c => c.EffectDate)
            .ToListAsync();

        decimal ResolveCost(int prodId, int? whId)
        {
            var match = costHists.FirstOrDefault(c => c.ProductId == prodId && (c.WarehouseId == whId || !c.WarehouseId.HasValue));
            if (match != null && match.CostPrice > 0) return match.CostPrice;
            if (prodDict.TryGetValue(prodId, out var prod) && prod.CostPrice > 0) return prod.CostPrice;
            return 0m;
        }

        var rawItems = new List<InventoryInDtlItem>();
        int seq = 1;

        // 1. Nguồn StockDoc (Posted In & Transfer)
        var docs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted && d.Date >= start && d.Date <= end &&
                        (d.Type == DocType.In || d.Type == DocType.Transfer))
            .Include(d => d.Lines)
            .Include(d => d.FromWarehouse)
            .Include(d => d.ToWarehouse)
            .OrderByDescending(d => d.Date)
            .ThenByDescending(d => d.Id)
            .ToListAsync();

        // Nạp các bảng liên quan để đối soát loại hình nhập
        var cusReturns = await db.CustomerReturns.Include(c => c.Lines).ToListAsync();
        var cusDictByDocId = cusReturns.Where(c => c.StockDocId.HasValue).ToDictionary(c => c.StockDocId!.Value, c => c);
        var cusDictByCode = cusReturns.ToDictionary(c => c.Code, c => c);

        var inFGs = await db.InventoryInFGs.Include(f => f.Lines).ToListAsync();
        var fgDictByDocId = inFGs.Where(f => f.StockDocId.HasValue).ToDictionary(f => f.StockDocId!.Value, f => f);
        var fgDictByCode = inFGs.ToDictionary(f => f.Code, f => f);

        var audits = await db.Audits.Include(a => a.Lines).ToListAsync();
        var auditDictByInDocId = audits.Where(a => a.InDocId.HasValue).ToDictionary(a => a.InDocId!.Value, a => a);
        var auditDictByCode = audits.ToDictionary(a => a.Code, a => a);

        foreach (var doc in docs)
        {
            int whId = doc.ToWarehouseId ?? 0;
            if (warehouseId.HasValue && whId != warehouseId.Value) continue;

            string whDocName = doc.ToWarehouse?.Name ?? "Kho nhận";
            string? locCode = blockDictByWh.TryGetValue(whId, out var bCode) ? bCode : null;

            if (doc.Type == DocType.In)
            {
                CustomerReturn? cus = null;
                if (cusDictByDocId.TryGetValue(doc.Id, out var c1)) cus = c1;
                else if (!string.IsNullOrEmpty(doc.RefNo) && cusDictByCode.TryGetValue(doc.RefNo, out var c2)) cus = c2;

                InventoryInFG? fg = null;
                if (fgDictByDocId.TryGetValue(doc.Id, out var f1)) fg = f1;
                else if (!string.IsNullOrEmpty(doc.RefNo) && fgDictByCode.TryGetValue(doc.RefNo, out var f2)) fg = f2;

                StockAudit? audit = null;
                if (auditDictByInDocId.TryGetValue(doc.Id, out var a1)) audit = a1;
                else if (!string.IsNullOrEmpty(doc.RefNo) && auditDictByCode.TryGetValue(doc.RefNo, out var a2)) audit = a2;

                string iType;
                string iTypeName;
                string? refNo = doc.RefNo;
                string? refType;
                string? supCode = doc.SupplierCode;
                string supName;
                string? invNo = null;
                DateTime? invDate = null;
                string docUrl;

                if (cus != null)
                {
                    iType = "CUS_RETURN";
                    iTypeName = "Nhập khách trả hàng";
                    supName = cus.CustomerName;
                    supCode = cus.CustomerCode;
                    refNo = cus.Code;
                    refType = !string.IsNullOrEmpty(cus.RefOrderNo) ? $"ĐH: {cus.RefOrderNo}" : "Phiếu khách trả";
                    invNo = cus.InvoiceNo;
                    invDate = cus.Date;
                    docUrl = $"/CustomerReturn/Detail/{cus.Id}";
                }
                else if (fg != null)
                {
                    iType = "IN_FG";
                    iTypeName = "Nhập thành phẩm SX";
                    supName = !string.IsNullOrWhiteSpace(fg.WorkshopName) ? fg.WorkshopName : "Xưởng sản xuất";
                    supCode = "WORKSHOP-01";
                    refNo = fg.Code;
                    refType = !string.IsNullOrEmpty(fg.WorkOrderNo) ? $"Lệnh: {fg.WorkOrderNo}" : "Lệnh SX nội bộ";
                    invNo = fg.WorkOrderNo;
                    invDate = fg.Date;
                    docUrl = $"/InventoryInFG/Detail/{fg.Id}";
                }
                else if (audit != null)
                {
                    iType = "AUDIT_DIFF";
                    iTypeName = "Nhập cân bằng kiểm kê";
                    supName = "Kiểm kê định kỳ (thừa)";
                    refNo = audit.Code;
                    refType = "Biên bản kiểm kê";
                    docUrl = $"/Audit/Detail/{audit.Id}";
                }
                else
                {
                    iType = "COMMERCIAL";
                    iTypeName = "Nhập mua NCC / Thương mại";
                    supName = !string.IsNullOrWhiteSpace(doc.SupplierName) ? doc.SupplierName : "Nhà cung cấp thương mại";
                    if (!string.IsNullOrEmpty(supCode) && supDictByCode.TryGetValue(supCode, out var sObj))
                    {
                        supName = sObj.Name;
                    }
                    refType = "Đơn mua hàng";
                    invNo = !string.IsNullOrEmpty(doc.RefNo) ? doc.RefNo : $"HD-{doc.Code}";
                    invDate = doc.Date;
                    docUrl = $"/Doc/Detail/{doc.Id}";
                }

                foreach (var line in doc.Lines)
                {
                    if (!prodDict.TryGetValue(line.ProductId, out var prod)) continue;
                    decimal up = 0m;
                    if (cus != null)
                    {
                        var cl = cus.Lines.FirstOrDefault(x => x.ProductId == line.ProductId);
                        if (cl != null && cl.UnitPrice > 0) up = cl.UnitPrice;
                    }
                    else if (fg != null)
                    {
                        var fl = fg.Lines.FirstOrDefault(x => x.ProductId == line.ProductId);
                        if (fl != null && fl.UnitCost > 0) up = fl.UnitCost;
                    }
                    if (up == 0m) up = ResolveCost(line.ProductId, whId);

                    decimal vatPercent = (iType == "AUDIT_DIFF") ? 0m : 10m;
                    decimal valBeforeTax = line.Quantity * up;
                    decimal valTax = Math.Round(valBeforeTax * vatPercent / 100m, 0);
                    decimal lineTotal = valBeforeTax + valTax;

                    rawItems.Add(new InventoryInDtlItem(
                        seq++,
                        doc.Code,
                        doc.Date,
                        iType,
                        iTypeName,
                        refNo,
                        refType,
                        whId,
                        whDocName,
                        locCode,
                        supCode,
                        supName,
                        prod.Id,
                        prod.Code,
                        prod.Name,
                        prod.Uom,
                        line.Quantity,
                        up,
                        vatPercent,
                        valBeforeTax,
                        valTax,
                        lineTotal,
                        invNo,
                        invDate,
                        doc.CreatedBy,
                        doc.Note,
                        docUrl
                    ));
                }
            }
            else if (doc.Type == DocType.Transfer)
            {
                // Đối với phiếu chuyển kho, kho nhận hàng là ToWarehouse
                string iType = "TRANSFER";
                string iTypeName = "Nhập điều chuyển kho đến";
                string supName = doc.FromWarehouse != null ? $"Kho chuyển: {doc.FromWarehouse.Name}" : "Chuyển nội bộ";
                string? refType = "Lệnh điều chuyển";
                string docUrl = $"/Doc/Detail/{doc.Id}";

                foreach (var line in doc.Lines)
                {
                    if (!prodDict.TryGetValue(line.ProductId, out var prod)) continue;
                    decimal up = ResolveCost(line.ProductId, whId);
                    decimal valBeforeTax = line.Quantity * up;
                    decimal valTax = 0m; // Điều chuyển kho không tính VAT
                    decimal lineTotal = valBeforeTax;

                    rawItems.Add(new InventoryInDtlItem(
                        seq++,
                        doc.Code,
                        doc.Date,
                        iType,
                        iTypeName,
                        doc.RefNo,
                        refType,
                        whId,
                        whDocName,
                        locCode,
                        doc.FromWarehouse?.Code,
                        supName,
                        prod.Id,
                        prod.Code,
                        prod.Name,
                        prod.Uom,
                        line.Quantity,
                        up,
                        0m,
                        valBeforeTax,
                        valTax,
                        lineTotal,
                        doc.RefNo,
                        doc.Date,
                        doc.CreatedBy,
                        doc.Note,
                        docUrl
                    ));
                }
            }
        }

        // Bổ sung các phiếu InventoryInFG đã Approved nếu chưa link StockDoc
        var unlinkedInFGs = inFGs
            .Where(f => f.Status == InvInFGStatus.Approved && !f.StockDocId.HasValue &&
                        f.Date >= start && f.Date <= end &&
                        (!warehouseId.HasValue || f.WarehouseId == warehouseId.Value))
            .ToList();

        foreach (var fg in unlinkedInFGs)
        {
            var whTitle = fg.Warehouse?.Name ?? "Kho nhận";
            string? locCode = blockDictByWh.TryGetValue(fg.WarehouseId, out var bCode) ? bCode : null;
            foreach (var line in fg.Lines)
            {
                if (!prodDict.TryGetValue(line.ProductId, out var prod)) continue;
                decimal up = line.UnitCost > 0 ? line.UnitCost : ResolveCost(line.ProductId, fg.WarehouseId);
                decimal valBeforeTax = line.ActualQty * up;
                decimal valTax = Math.Round(valBeforeTax * 0.1m, 0);
                decimal lineTotal = valBeforeTax + valTax;

                rawItems.Add(new InventoryInDtlItem(
                    seq++,
                    fg.Code,
                    fg.Date,
                    "IN_FG",
                    "Nhập thành phẩm SX",
                    fg.WorkOrderNo,
                    "Lệnh SX nội bộ",
                    fg.WarehouseId,
                    whTitle,
                    locCode,
                    "WORKSHOP-01",
                    !string.IsNullOrWhiteSpace(fg.WorkshopName) ? fg.WorkshopName : "Xưởng sản xuất",
                    prod.Id,
                    prod.Code,
                    prod.Name,
                    prod.Uom,
                    line.ActualQty,
                    up,
                    10m,
                    valBeforeTax,
                    valTax,
                    lineTotal,
                    fg.WorkOrderNo,
                    fg.Date,
                    fg.CreatedBy ?? "system",
                    fg.Remark,
                    $"/InventoryInFG/Detail/{fg.Id}"
                ));
            }
        }

        // Bổ sung các phiếu CustomerReturn đã Finished nếu chưa link StockDoc
        var unlinkedCusRets = cusReturns
            .Where(c => c.Status == CusReturnStatus.Finished &&
                        !c.StockDocId.HasValue && c.Date >= start && c.Date <= end &&
                        (!warehouseId.HasValue || c.WarehouseId == warehouseId.Value))
            .ToList();

        foreach (var ret in unlinkedCusRets)
        {
            var whTitle = ret.Warehouse?.Name ?? "Kho nhận";
            string? locCode = blockDictByWh.TryGetValue(ret.WarehouseId, out var bCode) ? bCode : null;
            foreach (var line in ret.Lines)
            {
                if (!prodDict.TryGetValue(line.ProductId, out var prod)) continue;
                decimal up = line.UnitPrice > 0 ? line.UnitPrice : ResolveCost(line.ProductId, ret.WarehouseId);
                decimal valBeforeTax = line.Quantity * up;
                decimal valTax = Math.Round(valBeforeTax * 0.1m, 0);
                decimal lineTotal = valBeforeTax + valTax;

                rawItems.Add(new InventoryInDtlItem(
                    seq++,
                    ret.Code,
                    ret.Date,
                    "CUS_RETURN",
                    "Nhập khách trả hàng",
                    ret.RefOrderNo,
                    "Đơn hàng bán gốc",
                    ret.WarehouseId,
                    whTitle,
                    locCode,
                    ret.CustomerCode,
                    ret.CustomerName,
                    prod.Id,
                    prod.Code,
                    prod.Name,
                    prod.Uom,
                    line.Quantity,
                    up,
                    10m,
                    valBeforeTax,
                    valTax,
                    lineTotal,
                    ret.InvoiceNo,
                    ret.Date,
                    ret.CreatedBy,
                    ret.Reason,
                    $"/CustomerReturn/Detail/{ret.Id}"
                ));
            }
        }

        // Lọc theo loại nhập (inType)
        var filtered = rawItems.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(inType))
        {
            filtered = filtered.Where(i => i.InType.Equals(inType.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        // Lọc theo từ khóa (keyword)
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            filtered = filtered.Where(i =>
                i.DocNo.ToLower().Contains(kw) ||
                (i.RefNo != null && i.RefNo.ToLower().Contains(kw)) ||
                (i.InvoiceNo != null && i.InvoiceNo.ToLower().Contains(kw)) ||
                i.ProductCode.ToLower().Contains(kw) ||
                i.ProductName.ToLower().Contains(kw) ||
                i.SupplierName.ToLower().Contains(kw) ||
                (i.SupplierCode != null && i.SupplierCode.ToLower().Contains(kw)) ||
                i.WarehouseName.ToLower().Contains(kw) ||
                i.CreatedBy.ToLower().Contains(kw) ||
                (i.Note != null && i.Note.ToLower().Contains(kw)));
        }

        var finalItems = filtered.OrderByDescending(i => i.DocDate).ThenByDescending(i => i.Id).ToList();

        int totalDocs = finalItems.Select(i => i.DocNo).Distinct().Count();
        int totalQty = finalItems.Sum(i => i.Quantity);
        decimal totalBeforeTax = finalItems.Sum(i => i.ValBeforeTax);
        decimal totalTax = finalItems.Sum(i => i.ValTax);
        decimal totalAmount = finalItems.Sum(i => i.TotalAmount);
        int distinctProds = finalItems.Select(i => i.ProductId).Distinct().Count();
        int distinctSups = finalItems.Select(i => i.SupplierName).Distinct().Count();

        return new InventoryInDtlReport(
            start,
            end.Date,
            warehouseId,
            whName,
            inType,
            keyword,
            finalItems,
            totalDocs,
            totalQty,
            totalBeforeTax,
            totalTax,
            totalAmount,
            distinctProds,
            distinctSups
        );
    }

    /// <summary>Báo cáo Ma trận Tổng hợp Nhập - Xuất & Tồn kho 12 Tháng (port từ Rpt_Summary_In_Out & Rpt_Summary_QtyInvByPeriod Skycic).</summary>
    public async Task<MonthlyMatrixReport> MonthlyMatrixReportAsync(int year, int? warehouseId, string? viewMode, string? keyword)
    {
        if (year < 2000 || year > 2100) year = DateTime.Today.Year;
        viewMode = string.IsNullOrWhiteSpace(viewMode) ? "ALL" : viewMode.ToUpperInvariant();

        string whName = "Toàn bộ hệ thống kho";
        if (warehouseId.HasValue && warehouseId.Value > 0)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var allProducts = await db.Products.OrderBy(p => p.Code).ToListAsync();
        var targetProducts = allProducts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            targetProducts = targetProducts.Where(p => p.Code.ToLowerInvariant().Contains(kw) || p.Name.ToLowerInvariant().Contains(kw));
        }
        var prodsList = targetProducts.ToList();

        var startOfYear = new DateTime(year, 1, 1, 0, 0, 0);
        var endOfYear = new DateTime(year, 12, 31, 23, 59, 59);

        // Lấy tất cả các phiếu đã Posted (kèm Lines) liên quan đến năm này và quá khứ
        var docs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted && d.Date <= endOfYear)
            .Include(d => d.Lines)
            .ToListAsync();

        var items = new List<ProductMonthlyMatrixItem>();
        var qtyPeriodRows = new List<SummaryQtyPeriodRow>();

        int[] monthlyTotalIn = new int[12];
        int[] monthlyTotalOut = new int[12];
        int[] monthlyTotalNet = new int[12];
        int[] monthlyTotalBalance = new int[12];

        foreach (var p in prodsList)
        {
            // 1. Tính tồn đầu năm (Opening Balance)
            int openingYear = 0;
            var pastDocs = docs.Where(d => d.Date < startOfYear);
            foreach (var doc in pastDocs)
            {
                var line = doc.Lines.FirstOrDefault(l => l.ProductId == p.Id);
                if (line == null) continue;

                if (warehouseId.HasValue && warehouseId.Value > 0)
                {
                    int wid = warehouseId.Value;
                    if ((doc.Type == DocType.In && doc.ToWarehouseId == wid) ||
                        (doc.Type == DocType.Transfer && doc.ToWarehouseId == wid))
                    {
                        openingYear += line.Quantity;
                    }
                    else if ((doc.Type == DocType.Out && doc.FromWarehouseId == wid) ||
                             (doc.Type == DocType.Transfer && doc.FromWarehouseId == wid))
                    {
                        openingYear -= line.Quantity;
                    }
                }
                else
                {
                    if (doc.Type == DocType.In) openingYear += line.Quantity;
                    else if (doc.Type == DocType.Out) openingYear -= line.Quantity;
                }
            }

            // 2. Tính số lượng Nhập và Xuất theo 12 tháng của năm được chọn
            int[] inM = new int[12];
            int[] outM = new int[12];
            int[] netM = new int[12];
            int[] balM = new int[12];

            var yearDocs = docs.Where(d => d.Date >= startOfYear && d.Date <= endOfYear);
            foreach (var doc in yearDocs)
            {
                int mIdx = doc.Date.Month - 1; // 0..11
                if (mIdx < 0 || mIdx > 11) continue;

                var line = doc.Lines.FirstOrDefault(l => l.ProductId == p.Id);
                if (line == null) continue;

                if (warehouseId.HasValue && warehouseId.Value > 0)
                {
                    int wid = warehouseId.Value;
                    if ((doc.Type == DocType.In && doc.ToWarehouseId == wid) ||
                        (doc.Type == DocType.Transfer && doc.ToWarehouseId == wid))
                    {
                        inM[mIdx] += line.Quantity;
                    }
                    else if ((doc.Type == DocType.Out && doc.FromWarehouseId == wid) ||
                             (doc.Type == DocType.Transfer && doc.FromWarehouseId == wid))
                    {
                        outM[mIdx] += line.Quantity;
                    }
                }
                else
                {
                    if (doc.Type == DocType.In) inM[mIdx] += line.Quantity;
                    else if (doc.Type == DocType.Out) outM[mIdx] += line.Quantity;
                }
            }

            // 3. Tính tồn lũy kế cuối mỗi tháng và biến động ròng
            int runningBal = openingYear;
            int maxBal = openingYear;
            int minBal = openingYear;

            for (int m = 0; m < 12; m++)
            {
                netM[m] = inM[m] - outM[m];
                runningBal += netM[m];
                balM[m] = runningBal;

                if (m == 0)
                {
                    maxBal = runningBal;
                    minBal = runningBal;
                }
                else
                {
                    if (runningBal > maxBal) maxBal = runningBal;
                    if (runningBal < minBal) minBal = runningBal;
                }

                // Cộng dồn vào tổng toàn kho
                monthlyTotalIn[m] += inM[m];
                monthlyTotalOut[m] += outM[m];
                monthlyTotalNet[m] += netM[m];
                monthlyTotalBalance[m] += runningBal;
            }

            int totalInProd = inM.Sum();
            int totalOutProd = outM.Sum();
            int totalNetProd = totalInProd - totalOutProd;

            // Tìm tháng cao điểm hoạt động của mặt hàng
            int peakMonthProd = 1;
            int peakVolProd = 0;
            for (int m = 0; m < 12; m++)
            {
                int vol = inM[m] + outM[m];
                if (vol > peakVolProd)
                {
                    peakVolProd = vol;
                    peakMonthProd = m + 1;
                }
            }

            var inRow = new MonthlyMatrixRow(
                p.Id, p.Code, p.Name, p.Uom,
                "IN", "Nhập kho", "bg-success text-white",
                inM[0], inM[1], inM[2], inM[3], inM[4], inM[5], inM[6], inM[7], inM[8], inM[9], inM[10], inM[11],
                totalInProd, Math.Round(totalInProd / 12.0, 1), peakMonthProd
            );

            var outRow = new MonthlyMatrixRow(
                p.Id, p.Code, p.Name, p.Uom,
                "OUT", "Xuất kho", "bg-danger text-white",
                outM[0], outM[1], outM[2], outM[3], outM[4], outM[5], outM[6], outM[7], outM[8], outM[9], outM[10], outM[11],
                totalOutProd, Math.Round(totalOutProd / 12.0, 1), peakMonthProd
            );

            var netRow = new MonthlyMatrixRow(
                p.Id, p.Code, p.Name, p.Uom,
                "NET", "Biến động ròng", "bg-info text-dark",
                netM[0], netM[1], netM[2], netM[3], netM[4], netM[5], netM[6], netM[7], netM[8], netM[9], netM[10], netM[11],
                totalNetProd, Math.Round(totalNetProd / 12.0, 1), peakMonthProd
            );

            var balRow = new MonthlyMatrixRow(
                p.Id, p.Code, p.Name, p.Uom,
                "BALANCE", "Tồn cuối kỳ", "bg-primary text-white",
                balM[0], balM[1], balM[2], balM[3], balM[4], balM[5], balM[6], balM[7], balM[8], balM[9], balM[10], balM[11],
                balM[11], Math.Round(balM.Average(), 1), peakMonthProd
            );

            items.Add(new ProductMonthlyMatrixItem
            {
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductName = p.Name,
                Uom = p.Uom,
                OpeningYearQty = openingYear,
                InRow = inRow,
                OutRow = outRow,
                NetRow = netRow,
                BalanceRow = balRow
            });

            qtyPeriodRows.Add(new SummaryQtyPeriodRow(
                p.Id, p.Code, p.Name, p.Uom,
                openingYear,
                balM[0], balM[1], balM[2], balM[3], balM[4], balM[5], balM[6], balM[7], balM[8], balM[9], balM[10], balM[11],
                balM[11], minBal, maxBal, Math.Round(balM.Average(), 1)
            ));
        }

        int totalInYear = monthlyTotalIn.Sum();
        int totalOutYear = monthlyTotalOut.Sum();
        int netMovementYear = totalInYear - totalOutYear;

        // Tìm tháng cao điểm hoạt động toàn kho
        int peakMonth = 1;
        int peakVolume = 0;
        for (int m = 0; m < 12; m++)
        {
            int vol = monthlyTotalIn[m] + monthlyTotalOut[m];
            if (vol > peakVolume)
            {
                peakVolume = vol;
                peakMonth = m + 1;
            }
        }
        string peakMonthName = $"Tháng {peakMonth:D2}/{year}";

        return new MonthlyMatrixReport(
            year,
            warehouseId,
            whName,
            viewMode,
            keyword,
            totalInYear,
            totalOutYear,
            netMovementYear,
            peakMonth,
            peakMonthName,
            peakVolume,
            monthlyTotalIn,
            monthlyTotalOut,
            monthlyTotalNet,
            monthlyTotalBalance,
            items,
            qtyPeriodRows
        );
    }

    public async Task<StockExtendReport> StockExtendReportAsync(int? warehouseId, StockExtendStatus? statusFilter, string? keyword)
    {
        string whName = "Toàn hệ thống";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        var allWarehouses = await db.Warehouses.OrderBy(w => w.Code).ToListAsync();
        var targetWarehouses = warehouseId.HasValue
            ? allWarehouses.Where(w => w.Id == warehouseId.Value).ToList()
            : allWarehouses;

        var allProducts = await db.Products.OrderBy(p => p.Code).ToListAsync();

        // 1. Tồn vật lý thực tế từ các phiếu ĐÃ GHI SỔ (Posted Docs)
        var postedDocs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted)
            .Include(d => d.Lines)
            .ToListAsync();

        var mapPhysical = new Dictionary<(int whId, int prodId), int>();
        void AddPhysical(int wh, int pid, int q)
        {
            mapPhysical.TryGetValue((wh, pid), out var cur);
            mapPhysical[(wh, pid)] = cur + q;
        }

        foreach (var d in postedDocs)
        {
            foreach (var l in d.Lines)
            {
                if (d.Type == DocType.In && d.ToWarehouseId is { } to) AddPhysical(to, l.ProductId, l.Quantity);
                else if (d.Type == DocType.Out && d.FromWarehouseId is { } fr) AddPhysical(fr, l.ProductId, -l.Quantity);
                else if (d.Type == DocType.Transfer)
                {
                    if (d.FromWarehouseId is { } f) AddPhysical(f, l.ProductId, -l.Quantity);
                    if (d.ToWarehouseId is { } t) AddPhysical(t, l.ProductId, l.Quantity);
                }
            }
        }

        // 2. Số lượng hàng bị khóa / giữ chỗ (QtyBlockOK):
        var mapBlock = new Dictionary<(int whId, int prodId), int>();
        void AddBlock(int wh, int pid, int q)
        {
            if (q <= 0) return;
            mapBlock.TryGetValue((wh, pid), out var cur);
            mapBlock[(wh, pid)] = cur + q;
        }

        // Serial bị khóa / lỗi hỏng
        var serials = await db.StockSerials
            .Where(s => s.Status == StockSerialStatus.Locked || s.Status == StockSerialStatus.DamagedNG)
            .ToListAsync();
        foreach (var s in serials)
        {
            AddBlock(s.WarehouseId, s.ProductId, 1);
        }

        // StockDoc Draft Out / Transfer
        var draftOutDocs = await db.Docs
            .Where(d => d.Status == DocStatus.Draft && (d.Type == DocType.Out || d.Type == DocType.Transfer))
            .Include(d => d.Lines)
            .ToListAsync();
        foreach (var d in draftOutDocs)
        {
            if (d.FromWarehouseId is { } fWh)
            {
                foreach (var l in d.Lines) AddBlock(fWh, l.ProductId, l.Quantity);
            }
        }

        // MoveOrder Pending hoặc Approved (kho xuất)
        var pendingMoveOrders = await db.MoveOrders
            .Where(m => m.Status == MoveOrderStatus.Pending || m.Status == MoveOrderStatus.Approved)
            .Include(m => m.Lines)
            .ToListAsync();
        foreach (var m in pendingMoveOrders)
        {
            foreach (var l in m.Lines) AddBlock(m.FromWarehouseId, l.ProductId, l.Quantity);
        }

        // ReturnToSupplier Draft (kho xuất)
        var draftRetSups = await db.ReturnToSuppliers
            .Where(r => r.Status == ReturnSupStatus.Draft)
            .Include(r => r.Lines)
            .ToListAsync();
        foreach (var r in draftRetSups)
        {
            foreach (var l in r.Lines) AddBlock(r.WarehouseId, l.ProductId, l.Quantity);
        }

        // InventoryOutFG Pending (kho xuất)
        var pendingOutFGs = await db.InventoryOutFGs
            .Where(f => f.Status == InvOutFGStatus.Pending)
            .Include(f => f.Lines)
            .ToListAsync();
        foreach (var f in pendingOutFGs)
        {
            foreach (var l in f.Lines) AddBlock(f.WarehouseId, l.ProductId, l.Qty);
        }

        // 3. Số lượng hàng sắp về / đang chờ nhập (QtyBackOrder):
        var mapBackOrder = new Dictionary<(int whId, int prodId), int>();
        void AddBackOrder(int wh, int pid, int q)
        {
            if (q <= 0) return;
            mapBackOrder.TryGetValue((wh, pid), out var cur);
            mapBackOrder[(wh, pid)] = cur + q;
        }

        var draftInDocs = await db.Docs
            .Where(d => d.Status == DocStatus.Draft && d.Type == DocType.In)
            .Include(d => d.Lines)
            .ToListAsync();
        foreach (var d in draftInDocs)
        {
            if (d.ToWarehouseId is { } tWh)
            {
                foreach (var l in d.Lines) AddBackOrder(tWh, l.ProductId, l.Quantity);
            }
        }

        var approvedMoveOrders = await db.MoveOrders
            .Where(m => m.Status == MoveOrderStatus.Approved)
            .Include(m => m.Lines)
            .ToListAsync();
        foreach (var m in approvedMoveOrders)
        {
            foreach (var l in m.Lines) AddBackOrder(m.ToWarehouseId, l.ProductId, l.Quantity);
        }

        var draftCusReturns = await db.CustomerReturns
            .Where(c => c.Status == CusReturnStatus.Draft)
            .Include(c => c.Lines)
            .ToListAsync();
        foreach (var c in draftCusReturns)
        {
            foreach (var l in c.Lines) AddBackOrder(c.WarehouseId, l.ProductId, l.Quantity);
        }

        var pendingInFGs = await db.InventoryInFGs
            .Where(f => f.Status == InvInFGStatus.Pending)
            .Include(f => f.Lines)
            .ToListAsync();
        foreach (var f in pendingInFGs)
        {
            foreach (var l in f.Lines) AddBackOrder(f.WarehouseId, l.ProductId, l.ActualQty > 0 ? l.ActualQty : l.PlanQty);
        }

        // Check mặt hàng có Lô và Serial
        var allLotProdIds = (await db.StockLots.Select(l => l.ProductId).Distinct().ToListAsync()).ToHashSet();
        var allSerialProdIds = (await db.StockSerials.Select(s => s.ProductId).Distinct().ToListAsync()).ToHashSet();

        // 4. Tổng hợp danh sách dòng báo cáo StockExtendRow
        var rows = new List<StockExtendRow>();

        foreach (var wh in targetWarehouses)
        {
            foreach (var prod in allProducts)
            {
                int totalOk = mapPhysical.GetValueOrDefault((wh.Id, prod.Id), 0);
                int blockOk = mapBlock.GetValueOrDefault((wh.Id, prod.Id), 0);
                int backOrder = mapBackOrder.GetValueOrDefault((wh.Id, prod.Id), 0);

                if (totalOk == 0 && blockOk == 0 && backOrder == 0 && prod.MinStock == 0 && prod.MaxStock == 0)
                    continue;

                int availOk = Math.Max(0, totalOk - blockOk);
                double availRate = totalOk > 0 ? Math.Round(availOk * 100.0 / totalOk, 1) : (availOk == 0 && blockOk > 0 ? 0.0 : 100.0);
                int stockExt = availOk + backOrder;
                int minStock = prod.MinStock;
                int maxStock = prod.MaxStock;
                decimal cost = prod.CostPrice;
                decimal totalVal = totalOk * cost;

                // Xác định trạng thái
                StockExtendStatus st;
                string stLabel;
                string badgeClass;

                if (availOk <= 0)
                {
                    st = StockExtendStatus.OutOfStock;
                    stLabel = "Cháy hàng / Hết";
                    badgeClass = "bg-danger text-white";
                }
                else if (minStock > 0 && availOk < minStock)
                {
                    st = StockExtendStatus.UnderMin;
                    stLabel = "Dưới định mức";
                    badgeClass = "bg-warning text-dark";
                }
                else if (maxStock > 0 && availOk > maxStock)
                {
                    st = StockExtendStatus.OverMax;
                    stLabel = "Vượt định mức";
                    badgeClass = "bg-info text-dark";
                }
                else
                {
                    st = StockExtendStatus.Optimal;
                    stLabel = "Đạt chuẩn an toàn";
                    badgeClass = "bg-success text-white";
                }

                int replenishNeeded = Math.Max(0, minStock - stockExt);

                rows.Add(new StockExtendRow(
                    prod.Id,
                    prod.Code,
                    prod.Name,
                    prod.Uom,
                    wh.Id,
                    wh.Name,
                    totalOk,
                    blockOk,
                    availOk,
                    availRate,
                    backOrder,
                    stockExt,
                    minStock,
                    maxStock,
                    cost,
                    totalVal,
                    st,
                    stLabel,
                    badgeClass,
                    replenishNeeded,
                    allLotProdIds.Contains(prod.Id),
                    allSerialProdIds.Contains(prod.Id)
                ));
            }
        }

        // Lọc theo StatusFilter
        if (statusFilter.HasValue && statusFilter.Value != StockExtendStatus.All)
        {
            rows = rows.Where(r => r.Status == statusFilter.Value).ToList();
        }

        // Lọc theo Từ khóa
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLowerInvariant();
            rows = rows.Where(r => r.ProductCode.ToLowerInvariant().Contains(k) ||
                                   r.ProductName.ToLowerInvariant().Contains(k) ||
                                   r.WarehouseName.ToLowerInvariant().Contains(k)).ToList();
        }

        // Sắp xếp
        rows = rows.OrderBy(r => r.WarehouseName).ThenBy(r => r.ProductCode).ToList();

        // Tính toán KPI
        int totalItems = rows.Count;
        int totalQtyTotal = rows.Sum(r => r.QtyTotalOK);
        int totalQtyBlock = rows.Sum(r => r.QtyBlockOK);
        int totalQtyAvail = rows.Sum(r => r.QtyAvailOK);
        int totalQtyBackOrder = rows.Sum(r => r.QtyBackOrder);
        int totalQtyStockExt = rows.Sum(r => r.QtyStockExt);
        decimal totalInvValue = rows.Sum(r => r.TotalValue);

        int outOfStockCount = rows.Count(r => r.Status == StockExtendStatus.OutOfStock);
        int underMinCount = rows.Count(r => r.Status == StockExtendStatus.UnderMin);
        int optimalCount = rows.Count(r => r.Status == StockExtendStatus.Optimal);
        int overMaxCount = rows.Count(r => r.Status == StockExtendStatus.OverMax);
        int urgentReplenishCount = rows.Count(r => r.ReplenishNeeded > 0);
        double avgAvailRate = totalQtyTotal > 0 ? Math.Round(totalQtyAvail * 100.0 / totalQtyTotal, 1) : 0.0;

        return new StockExtendReport(
            warehouseId,
            whName,
            statusFilter,
            keyword,
            totalItems,
            totalQtyTotal,
            totalQtyBlock,
            totalQtyAvail,
            totalQtyBackOrder,
            totalQtyStockExt,
            totalInvValue,
            outOfStockCount,
            underMinCount,
            optimalCount,
            overMaxCount,
            urgentReplenishCount,
            avgAvailRate,
            rows
        );
    }

    /// <summary>Báo cáo Đánh giá giá trị tồn kho & Cơ cấu tài sản kho (port từ Rpt_Inv_InventoryBalance_ByValue & Rpt_Inv_InventoryBalance Skycic).</summary>
    public async Task<InventoryValuationReport> InventoryValuationReportAsync(
        int? warehouseId,
        InventoryValuationAbcClass? abcClass,
        bool onlyHasStock = true,
        string? keyword = null,
        DateTime? asOfDate = null)
    {
        string whName = "Toàn hệ thống";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        DateTime targetDate = asOfDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;
        var allWarehouses = await db.Warehouses.OrderBy(w => w.Code).ToListAsync();
        var targetWarehouses = warehouseId.HasValue
            ? allWarehouses.Where(w => w.Id == warehouseId.Value).ToList()
            : allWarehouses;

        var allProducts = await db.Products.OrderBy(p => p.Code).ToListAsync();

        // 1. Tồn vật lý thực tế từ các phiếu ĐÃ GHI SỔ tính đến mốc thời gian targetDate
        var postedDocs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted && d.Date <= targetDate)
            .Include(d => d.Lines)
            .ToListAsync();

        var mapPhysical = new Dictionary<(int whId, int prodId), int>();
        void AddPhysical(int wh, int pid, int q)
        {
            mapPhysical.TryGetValue((wh, pid), out var cur);
            mapPhysical[(wh, pid)] = cur + q;
        }

        foreach (var d in postedDocs)
        {
            foreach (var l in d.Lines)
            {
                if (d.Type == DocType.In && d.ToWarehouseId is { } to) AddPhysical(to, l.ProductId, l.Quantity);
                else if (d.Type == DocType.Out && d.FromWarehouseId is { } fr) AddPhysical(fr, l.ProductId, -l.Quantity);
                else if (d.Type == DocType.Transfer)
                {
                    if (d.FromWarehouseId is { } f) AddPhysical(f, l.ProductId, -l.Quantity);
                    if (d.ToWarehouseId is { } t) AddPhysical(t, l.ProductId, l.Quantity);
                }
            }
        }

        // 2. Số lượng hàng bị tạm khóa / phong tỏa vốn (QtyBlockOK)
        var mapBlock = new Dictionary<(int whId, int prodId), int>();
        void AddBlock(int wh, int pid, int q)
        {
            if (q <= 0) return;
            mapBlock.TryGetValue((wh, pid), out var cur);
            mapBlock[(wh, pid)] = cur + q;
        }

        var serials = await db.StockSerials
            .Where(s => (s.Status == StockSerialStatus.Locked || s.Status == StockSerialStatus.DamagedNG) && s.InDate <= targetDate)
            .ToListAsync();
        foreach (var s in serials) AddBlock(s.WarehouseId, s.ProductId, 1);

        var draftOutDocs = await db.Docs
            .Where(d => d.Status == DocStatus.Draft && (d.Type == DocType.Out || d.Type == DocType.Transfer) && d.Date <= targetDate)
            .Include(d => d.Lines)
            .ToListAsync();
        foreach (var d in draftOutDocs)
        {
            if (d.FromWarehouseId is { } fWh)
            {
                foreach (var l in d.Lines) AddBlock(fWh, l.ProductId, l.Quantity);
            }
        }

        var pendingMoveOrders = await db.MoveOrders
            .Where(m => (m.Status == MoveOrderStatus.Pending || m.Status == MoveOrderStatus.Approved) && m.Date <= targetDate)
            .Include(m => m.Lines)
            .ToListAsync();
        foreach (var m in pendingMoveOrders)
        {
            foreach (var l in m.Lines) AddBlock(m.FromWarehouseId, l.ProductId, l.Quantity);
        }

        var draftRetSups = await db.ReturnToSuppliers
            .Where(r => r.Status == ReturnSupStatus.Draft && r.Date <= targetDate)
            .Include(r => r.Lines)
            .ToListAsync();
        foreach (var r in draftRetSups)
        {
            foreach (var l in r.Lines) AddBlock(r.WarehouseId, l.ProductId, l.Quantity);
        }

        var pendingOutFGs = await db.InventoryOutFGs
            .Where(f => f.Status == InvOutFGStatus.Pending && f.Date <= targetDate)
            .Include(f => f.Lines)
            .ToListAsync();
        foreach (var f in pendingOutFGs)
        {
            foreach (var l in f.Lines) AddBlock(f.WarehouseId, l.ProductId, l.Qty);
        }

        // Lấy giá vốn kho hiện hành hoặc tại thời điểm targetDate từ CostPriceHist
        var currentCostPrices = await db.CostPriceHists
            .Where(c => c.EffectDate <= targetDate)
            .OrderByDescending(c => c.EffectDate)
            .ThenByDescending(c => c.Id)
            .ToListAsync();

        var costPriceLookup = new Dictionary<(int? whId, int prodId), decimal>();
        foreach (var c in currentCostPrices)
        {
            if (!costPriceLookup.ContainsKey((c.WarehouseId, c.ProductId)))
                costPriceLookup[(c.WarehouseId, c.ProductId)] = c.CostPrice;
        }

        var allLotProdIds = (await db.StockLots.Select(l => l.ProductId).Distinct().ToListAsync()).ToHashSet();
        var allSerialProdIds = (await db.StockSerials.Select(s => s.ProductId).Distinct().ToListAsync()).ToHashSet();

        // 3. Xây dựng danh sách sơ bộ các mặt hàng
        var candidateList = new List<(
            Product Prod,
            Warehouse Wh,
            int TotalOk,
            int BlockOk,
            int AvailOk,
            double AvailRate,
            decimal CostPrice,
            decimal TotalValMixBase,
            decimal TotalValAvail,
            decimal TotalValBlock
        )>();

        foreach (var wh in targetWarehouses)
        {
            foreach (var prod in allProducts)
            {
                int totalOk = mapPhysical.GetValueOrDefault((wh.Id, prod.Id), 0);
                int rawBlockOk = mapBlock.GetValueOrDefault((wh.Id, prod.Id), 0);
                int blockOk = Math.Min(totalOk > 0 ? totalOk : 0, rawBlockOk);
                int availOk = Math.Max(0, totalOk - blockOk);
                double availRate = totalOk > 0 ? Math.Round(availOk * 100.0 / totalOk, 1) : 100.0;

                // Giá vốn ưu tiên: CostPriceHist của kho -> CostPriceHist toàn hệ thống -> Product.CostPrice
                decimal cost = prod.CostPrice;
                if (costPriceLookup.TryGetValue((wh.Id, prod.Id), out var whCost) && whCost > 0)
                    cost = whCost;
                else if (costPriceLookup.TryGetValue((null, prod.Id), out var sysCost) && sysCost > 0)
                    cost = sysCost;

                if (cost <= 0) cost = 100000m; // Fallback giá danh nghĩa

                decimal totalValMixBase = totalOk * cost;
                decimal totalValAvail = availOk * cost;
                decimal totalValBlock = blockOk * cost;

                if (onlyHasStock && totalOk <= 0 && totalValMixBase <= 0)
                    continue;

                candidateList.Add((prod, wh, totalOk, blockOk, availOk, availRate, cost, totalValMixBase, totalValAvail, totalValBlock));
            }
        }

        // 4. Sắp xếp giảm dần theo Tổng giá trị để phân bổ tỷ trọng và phân hạng ABC
        var sortedCandidates = candidateList.OrderByDescending(c => c.TotalValMixBase).ToList();
        decimal grandTotalValMixBase = sortedCandidates.Sum(c => c.TotalValMixBase);

        var rows = new List<InventoryValuationRow>();
        double cumulativeShare = 0.0;

        foreach (var item in sortedCandidates)
        {
            double share = grandTotalValMixBase > 0
                ? Math.Round((double)(item.TotalValMixBase / grandTotalValMixBase * 100m), 2)
                : 0.0;

            cumulativeShare += share;

            InventoryValuationAbcClass itemAbc;
            string abcLabel, abcBadge;

            if (cumulativeShare <= 70.0 || (rows.Count == 0 && share > 0))
            {
                itemAbc = InventoryValuationAbcClass.ClassA;
                abcLabel = "Hạng A (Giá trị cao)";
                abcBadge = "bg-danger text-white";
            }
            else if (cumulativeShare <= 90.0)
            {
                itemAbc = InventoryValuationAbcClass.ClassB;
                abcLabel = "Hạng B (Trung bình)";
                abcBadge = "bg-warning text-dark";
            }
            else
            {
                itemAbc = InventoryValuationAbcClass.ClassC;
                abcLabel = "Hạng C (Giá trị thấp)";
                abcBadge = "bg-secondary text-white";
            }

            string riskStatus, riskBadge;
            if (item.TotalValBlock > 0)
            {
                riskStatus = "Chôn vốn tạm khóa";
                riskBadge = "bg-danger text-white";
            }
            else if (itemAbc == InventoryValuationAbcClass.ClassA && item.TotalOk > 50)
            {
                riskStatus = "Tồn vốn trọng điểm";
                riskBadge = "bg-primary text-white";
            }
            else
            {
                riskStatus = "An toàn luân chuyển";
                riskBadge = "bg-success text-white";
            }

            rows.Add(new InventoryValuationRow(
                item.Prod.Id,
                item.Prod.Code,
                item.Prod.Name,
                item.Prod.Uom,
                item.Wh.Id,
                item.Wh.Name,
                item.TotalOk,
                item.BlockOk,
                item.AvailOk,
                item.AvailRate,
                item.CostPrice,
                item.TotalValMixBase,
                item.TotalValAvail,
                item.TotalValBlock,
                share,
                itemAbc,
                abcLabel,
                abcBadge,
                riskStatus,
                riskBadge,
                allLotProdIds.Contains(item.Prod.Id),
                allSerialProdIds.Contains(item.Prod.Id)
            ));
        }

        // Lọc theo AbcFilter
        if (abcClass.HasValue && abcClass.Value != InventoryValuationAbcClass.All)
        {
            rows = rows.Where(r => r.AbcClass == abcClass.Value).ToList();
        }

        // Lọc theo từ khóa
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLowerInvariant();
            rows = rows.Where(r => r.ProductCode.ToLowerInvariant().Contains(k) ||
                                   r.ProductName.ToLowerInvariant().Contains(k) ||
                                   r.WarehouseName.ToLowerInvariant().Contains(k)).ToList();
        }

        // KPI thống kê
        int totalItems = rows.Count;
        int totalPhysicalQty = rows.Sum(r => r.QtyTotalOK);
        int totalBlockedQty = rows.Sum(r => r.QtyBlockOK);
        int totalAvailableQty = rows.Sum(r => r.QtyAvailOK);
        decimal sumTotalVal = rows.Sum(r => r.TotalValMixBase);
        decimal sumValAvail = rows.Sum(r => r.TotalValAvail);
        decimal sumValBlock = rows.Sum(r => r.TotalValBlock);
        double availRatio = sumTotalVal > 0 ? Math.Round((double)(sumValAvail / sumTotalVal * 100m), 1) : 100.0;

        int classACount = rows.Count(r => r.AbcClass == InventoryValuationAbcClass.ClassA);
        decimal classAVal = rows.Where(r => r.AbcClass == InventoryValuationAbcClass.ClassA).Sum(r => r.TotalValMixBase);
        int classBCount = rows.Count(r => r.AbcClass == InventoryValuationAbcClass.ClassB);
        decimal classBVal = rows.Where(r => r.AbcClass == InventoryValuationAbcClass.ClassB).Sum(r => r.TotalValMixBase);
        int classCCount = rows.Count(r => r.AbcClass == InventoryValuationAbcClass.ClassC);
        decimal classCVal = rows.Where(r => r.AbcClass == InventoryValuationAbcClass.ClassC).Sum(r => r.TotalValMixBase);

        return new InventoryValuationReport(
            warehouseId,
            whName,
            asOfDate ?? DateTime.Today,
            abcClass,
            onlyHasStock,
            keyword,
            totalItems,
            totalPhysicalQty,
            totalBlockedQty,
            totalAvailableQty,
            sumTotalVal,
            sumValAvail,
            sumValBlock,
            availRatio,
            classACount,
            classAVal,
            classBCount,
            classBVal,
            classCCount,
            classCVal,
            rows
        );
    }

    /// <summary>Báo cáo danh mục Loại mặt hàng tổng hợp kèm KPI (port từ Mst_PartType Skycic).</summary>
    public async Task<PartTypeReport> PartTypesReportAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.PartTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(p => p.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(p => p.Code.ToLower().Contains(kw) ||
                                     p.Name.ToLower().Contains(kw) ||
                                     (p.Remark != null && p.Remark.ToLower().Contains(kw)));
        }

        var partTypes = await query.OrderBy(p => p.Code).ToListAsync();
        var allProducts = await db.Products.ToListAsync();
        var balances = await BalancesAsync(null);

        var prodGroup = allProducts
            .Where(p => !string.IsNullOrEmpty(p.PartTypeCode))
            .GroupBy(p => p.PartTypeCode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = partTypes.Select(pt =>
        {
            int pCount = prodGroup.TryGetValue(pt.Code, out var pList) ? pList.Count : 0;
            var pIds = pList?.Select(p => p.Id).ToHashSet() ?? [];
            int totalStock = balances.Where(b => pIds.Contains(b.ProductId)).Sum(b => b.Qty);
            return new PartTypeRow(pt.Id, pt.Code, pt.Name, pt.Remark, pt.IsActive, pt.CreatedAt, pCount, totalStock);
        }).ToList();

        int totalTypes = await db.PartTypes.CountAsync();
        int activeCount = await db.PartTypes.CountAsync(p => p.IsActive);
        int inactiveCount = totalTypes - activeCount;
        int mappedProds = allProducts.Count(p => !string.IsNullOrEmpty(p.PartTypeCode));

        return new PartTypeReport(q, activeOnly, totalTypes, activeCount, inactiveCount, mappedProds, rows);
    }

    public Task<List<PartType>> PartTypesAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.PartTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(p => p.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(p => p.Code.ToLower().Contains(kw) || p.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(p => p.Code).ToListAsync();
    }

    public Task<PartType?> GetPartTypeAsync(int id) =>
        db.PartTypes.FirstOrDefaultAsync(p => p.Id == id);

    public Task<PartType?> GetPartTypeByCodeAsync(string code) =>
        db.PartTypes.FirstOrDefaultAsync(p => p.Code.ToLower() == code.Trim().ToLower());

    public async Task<PartTypeDetailDto?> GetPartTypeDetailAsync(int id)
    {
        var pt = await db.PartTypes.FirstOrDefaultAsync(p => p.Id == id);
        if (pt == null) return null;

        var products = await db.Products
            .Where(p => p.PartTypeCode != null && p.PartTypeCode.ToLower() == pt.Code.ToLower())
            .OrderBy(p => p.Code)
            .ToListAsync();

        var balances = await BalancesAsync(null);
        var pIds = products.Select(p => p.Id).ToHashSet();
        int totalStock = balances.Where(b => pIds.Contains(b.ProductId)).Sum(b => b.Qty);

        return new PartTypeDetailDto(pt, products, products.Count, totalStock);
    }

    public async Task<int> CreatePartTypeAsync(PartType item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Tên loại mặt hàng không được để trống.");

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"PT{await db.PartTypes.CountAsync() + 1:D2}";
        }
        else
        {
            item.Code = item.Code.Trim().ToUpperInvariant();
        }

        bool exists = await db.PartTypes.AnyAsync(p => p.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã loại mặt hàng '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.PartTypes.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdatePartTypeAsync(int id, PartType item)
    {
        var existing = await db.PartTypes.FirstOrDefaultAsync(p => p.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại mặt hàng.");

        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "Tên loại mặt hàng không được để trống.");

        existing.Name = item.Name.Trim();
        existing.Remark = item.Remark?.Trim();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin loại mặt hàng '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> TogglePartTypeStatusAsync(int id)
    {
        var existing = await db.PartTypes.FirstOrDefaultAsync(p => p.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại mặt hàng.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng loại mặt hàng '{existing.Code}'." : $"Đã chuyển loại mặt hàng '{existing.Code}' sang trạng thái Ngừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> DeletePartTypeAsync(int id)
    {
        var existing = await db.PartTypes.FirstOrDefaultAsync(p => p.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại mặt hàng.");

        bool isUsed = await db.Products.AnyAsync(p => p.PartTypeCode != null && p.PartTypeCode.ToLower() == existing.Code.ToLower());
        if (isUsed)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            return (true, $"Loại mặt hàng '{existing.Code}' đang được gán cho sản phẩm trong kho nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.PartTypes.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa loại mặt hàng '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh mục Thương hiệu hàng hóa tổng hợp kèm KPI (port từ Mst_Brand Skycic).</summary>
    public async Task<BrandReport> BrandsReportAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.Brands.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(b => b.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(b => b.Code.ToLower().Contains(kw) ||
                                     b.Name.ToLower().Contains(kw) ||
                                     (b.Origin != null && b.Origin.ToLower().Contains(kw)) ||
                                     (b.Remark != null && b.Remark.ToLower().Contains(kw)));
        }

        var brands = await query.OrderBy(b => b.Code).ToListAsync();
        var allProducts = await db.Products.ToListAsync();
        var balances = await BalancesAsync(null);

        var prodGroup = allProducts
            .Where(p => !string.IsNullOrEmpty(p.BrandCode))
            .GroupBy(p => p.BrandCode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = brands.Select(b =>
        {
            int pCount = prodGroup.TryGetValue(b.Code, out var pList) ? pList.Count : 0;
            var pIds = pList?.Select(p => p.Id).ToHashSet() ?? [];
            int totalStock = balances.Where(bal => pIds.Contains(bal.ProductId)).Sum(bal => bal.Qty);
            return new BrandRow(b.Id, b.Code, b.Name, b.Origin, b.Remark, b.IsActive, b.CreatedAt, pCount, totalStock);
        }).ToList();

        int totalBrands = await db.Brands.CountAsync();
        int activeCount = await db.Brands.CountAsync(b => b.IsActive);
        int inactiveCount = totalBrands - activeCount;
        int mappedProds = allProducts.Count(p => !string.IsNullOrEmpty(p.BrandCode));

        return new BrandReport(q, activeOnly, totalBrands, activeCount, inactiveCount, mappedProds, rows);
    }

    public Task<List<Brand>> BrandsAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.Brands.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(b => b.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(b => b.Code.ToLower().Contains(kw) || b.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(b => b.Code).ToListAsync();
    }

    public Task<Brand?> GetBrandAsync(int id) =>
        db.Brands.FirstOrDefaultAsync(b => b.Id == id);

    public Task<Brand?> GetBrandByCodeAsync(string code) =>
        db.Brands.FirstOrDefaultAsync(b => b.Code.ToLower() == code.Trim().ToLower());

    public async Task<BrandDetailDto?> GetBrandDetailAsync(int id)
    {
        var b = await db.Brands.FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return null;

        var products = await db.Products
            .Where(p => p.BrandCode != null && p.BrandCode.ToLower() == b.Code.ToLower())
            .OrderBy(p => p.Code)
            .ToListAsync();

        var balances = await BalancesAsync(null);
        var pIds = products.Select(p => p.Id).ToHashSet();
        int totalStock = balances.Where(bal => pIds.Contains(bal.ProductId)).Sum(bal => bal.Qty);

        return new BrandDetailDto(b, products, products.Count, totalStock);
    }

    public async Task<int> CreateBrandAsync(Brand item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Tên thương hiệu không được để trống.");

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"BR{await db.Brands.CountAsync() + 1:D2}";
        }
        else
        {
            item.Code = item.Code.Trim().ToUpperInvariant();
        }

        bool exists = await db.Brands.AnyAsync(b => b.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã thương hiệu '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.Brands.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateBrandAsync(int id, Brand item)
    {
        var existing = await db.Brands.FirstOrDefaultAsync(b => b.Id == id);
        if (existing == null) return (false, "Không tìm thấy thương hiệu.");

        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "Tên thương hiệu không được để trống.");

        existing.Name = item.Name.Trim();
        existing.Origin = item.Origin?.Trim();
        existing.Remark = item.Remark?.Trim();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin thương hiệu '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> ToggleBrandStatusAsync(int id)
    {
        var existing = await db.Brands.FirstOrDefaultAsync(b => b.Id == id);
        if (existing == null) return (false, "Không tìm thấy thương hiệu.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng thương hiệu '{existing.Code}'." : $"Đã chuyển thương hiệu '{existing.Code}' sang trạng thái Ngừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> DeleteBrandAsync(int id)
    {
        var existing = await db.Brands.FirstOrDefaultAsync(b => b.Id == id);
        if (existing == null) return (false, "Không tìm thấy thương hiệu.");

        bool isUsed = await db.Products.AnyAsync(p => p.BrandCode != null && p.BrandCode.ToLower() == existing.Code.ToLower());
        if (isUsed)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            return (true, $"Thương hiệu '{existing.Code}' đang được gán cho sản phẩm trong kho nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.Brands.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa thương hiệu '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh mục Đơn vị tính hàng hóa tổng hợp kèm 4 thẻ KPI (port từ Mst_PartUnit Skycic).</summary>
    public async Task<PartUnitReport> PartUnitsReportAsync(string? q = null, bool? activeOnly = null, bool? standardOnly = null)
    {
        var query = db.PartUnits.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(u => u.IsActive == activeOnly.Value);
        if (standardOnly.HasValue) query = query.Where(u => u.IsStandard == standardOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(u => u.Code.ToLower().Contains(kw) ||
                                     u.Name.ToLower().Contains(kw) ||
                                     (u.Remark != null && u.Remark.ToLower().Contains(kw)));
        }

        var units = await query.OrderBy(u => u.Code).ToListAsync();
        var allProducts = await db.Products.ToListAsync();
        var balances = await BalancesAsync(null);

        var prodGroup = allProducts
            .GroupBy(p => p.Uom.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = units.Select(u =>
        {
            var pList = new List<Product>();
            if (prodGroup.TryGetValue(u.Code, out var listByCode)) pList.AddRange(listByCode);
            if (prodGroup.TryGetValue(u.Name, out var listByName))
            {
                foreach (var p in listByName)
                {
                    if (!pList.Any(x => x.Id == p.Id)) pList.Add(p);
                }
            }
            int pCount = pList.Count;
            var pIds = pList.Select(p => p.Id).ToHashSet();
            int totalStock = balances.Where(bal => pIds.Contains(bal.ProductId)).Sum(bal => bal.Qty);
            return new PartUnitRow(u.Id, u.Code, u.Name, u.IsStandard, u.IsActive, u.Remark, u.CreatedAt, pCount, totalStock);
        }).ToList();

        int totalUnits = await db.PartUnits.CountAsync();
        int standardUnitsCount = await db.PartUnits.CountAsync(u => u.IsStandard);
        int activeCount = await db.PartUnits.CountAsync(u => u.IsActive);
        int inactiveCount = totalUnits - activeCount;

        var unitCodesAndNames = (await db.PartUnits.Select(u => new { u.Code, u.Name }).ToListAsync())
            .SelectMany(u => new[] { u.Code.ToLowerInvariant(), u.Name.ToLowerInvariant() }).ToHashSet();
        int mappedProds = allProducts.Count(p => unitCodesAndNames.Contains(p.Uom.Trim().ToLowerInvariant()));

        return new PartUnitReport(q, activeOnly, standardOnly, totalUnits, standardUnitsCount, activeCount, inactiveCount, mappedProds, rows);
    }

    public Task<List<PartUnit>> PartUnitsAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.PartUnits.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(u => u.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(u => u.Code.ToLower().Contains(kw) || u.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(u => u.Code).ToListAsync();
    }

    public Task<PartUnit?> GetPartUnitAsync(int id) =>
        db.PartUnits.FirstOrDefaultAsync(u => u.Id == id);

    public Task<PartUnit?> GetPartUnitByCodeAsync(string code) =>
        db.PartUnits.FirstOrDefaultAsync(u => u.Code.ToLower() == code.Trim().ToLower());

    public async Task<PartUnitDetailDto?> GetPartUnitDetailAsync(int id)
    {
        var u = await db.PartUnits.FirstOrDefaultAsync(x => x.Id == id);
        if (u == null) return null;

        var allProducts = await db.Products.OrderBy(p => p.Code).ToListAsync();
        var products = allProducts
            .Where(p => string.Equals(p.Uom, u.Code, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.Uom, u.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var balances = await BalancesAsync(null);
        var pIds = products.Select(p => p.Id).ToHashSet();
        int totalStock = balances.Where(bal => pIds.Contains(bal.ProductId)).Sum(bal => bal.Qty);

        return new PartUnitDetailDto(u, products, products.Count, totalStock);
    }

    public async Task<int> CreatePartUnitAsync(PartUnit item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Tên đơn vị tính không được để trống.");

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"UNIT{await db.PartUnits.CountAsync() + 1:D2}";
        }
        else
        {
            item.Code = item.Code.Trim().ToUpperInvariant();
        }

        bool exists = await db.PartUnits.AnyAsync(u => u.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã đơn vị tính '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.PartUnits.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdatePartUnitAsync(int id, PartUnit item)
    {
        var existing = await db.PartUnits.FirstOrDefaultAsync(u => u.Id == id);
        if (existing == null) return (false, "Không tìm thấy đơn vị tính.");

        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "Tên đơn vị tính không được để trống.");

        existing.Name = item.Name.Trim();
        existing.IsStandard = item.IsStandard;
        existing.Remark = item.Remark?.Trim();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin đơn vị tính '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> TogglePartUnitStatusAsync(int id)
    {
        var existing = await db.PartUnits.FirstOrDefaultAsync(u => u.Id == id);
        if (existing == null) return (false, "Không tìm thấy đơn vị tính.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng đơn vị tính '{existing.Code}'." : $"Đã chuyển đơn vị tính '{existing.Code}' sang trạng thái Ngừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> DeletePartUnitAsync(int id)
    {
        var existing = await db.PartUnits.FirstOrDefaultAsync(u => u.Id == id);
        if (existing == null) return (false, "Không tìm thấy đơn vị tính.");

        bool isUsed = await db.Products.AnyAsync(p => p.Uom.ToLower() == existing.Code.ToLower() || p.Uom.ToLower() == existing.Name.ToLower());
        if (isUsed)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            return (true, $"Đơn vị tính '{existing.Code}' đang được gán cho sản phẩm trong kho nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.PartUnits.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa đơn vị tính '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh mục Nhóm chất liệu / Vật liệu hàng hóa tổng hợp kèm 4 thẻ KPI (port từ Mst_PartMaterialType Skycic: PMType, PMTypeName, FlagActive, Remark).</summary>
    public async Task<PartMaterialTypeReport> PartMaterialTypesReportAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.PartMaterialTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(m => m.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(m => m.Code.ToLower().Contains(kw) ||
                                     m.Name.ToLower().Contains(kw) ||
                                     (m.Remark != null && m.Remark.ToLower().Contains(kw)));
        }

        var list = await query.OrderBy(m => m.Code).ToListAsync();
        var allProducts = await db.Products.ToListAsync();
        var balances = await BalancesAsync(null);

        var prodGroup = allProducts
            .Where(p => !string.IsNullOrEmpty(p.PMType))
            .GroupBy(p => p.PMType!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = list.Select(m =>
        {
            var pList = new List<Product>();
            if (prodGroup.TryGetValue(m.Code, out var listByCode)) pList.AddRange(listByCode);
            if (prodGroup.TryGetValue(m.Name, out var listByName))
            {
                foreach (var p in listByName)
                {
                    if (!pList.Any(x => x.Id == p.Id)) pList.Add(p);
                }
            }
            int pCount = pList.Count;
            var pIds = pList.Select(p => p.Id).ToHashSet();
            int totalStock = balances.Where(bal => pIds.Contains(bal.ProductId)).Sum(bal => bal.Qty);
            return new PartMaterialTypeRow(m.Id, m.Code, m.Name, m.Remark, m.IsActive, m.CreatedAt, pCount, totalStock);
        }).ToList();

        int totalTypes = await db.PartMaterialTypes.CountAsync();
        int activeCount = await db.PartMaterialTypes.CountAsync(m => m.IsActive);
        int inactiveCount = totalTypes - activeCount;

        var materialCodes = (await db.PartMaterialTypes.Select(m => m.Code).ToListAsync())
            .Select(c => c.ToLowerInvariant()).ToHashSet();
        int mappedProds = allProducts.Count(p => !string.IsNullOrEmpty(p.PMType) && materialCodes.Contains(p.PMType.Trim().ToLowerInvariant()));

        return new PartMaterialTypeReport(q, activeOnly, totalTypes, activeCount, inactiveCount, mappedProds, rows);
    }

    public Task<List<PartMaterialType>> PartMaterialTypesAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.PartMaterialTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(m => m.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(m => m.Code.ToLower().Contains(kw) || m.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(m => m.Code).ToListAsync();
    }

    public Task<PartMaterialType?> GetPartMaterialTypeAsync(int id) =>
        db.PartMaterialTypes.FirstOrDefaultAsync(m => m.Id == id);

    public Task<PartMaterialType?> GetPartMaterialTypeByCodeAsync(string code) =>
        db.PartMaterialTypes.FirstOrDefaultAsync(m => m.Code.ToLower() == code.Trim().ToLower());

    public async Task<PartMaterialTypeDetailDto?> GetPartMaterialTypeDetailAsync(int id)
    {
        var m = await db.PartMaterialTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return null;

        var allProducts = await db.Products.OrderBy(p => p.Code).ToListAsync();
        var products = allProducts
            .Where(p => string.Equals(p.PMType, m.Code, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.PMType, m.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var balances = await BalancesAsync(null);
        var pIds = products.Select(p => p.Id).ToHashSet();
        int totalStock = balances.Where(bal => pIds.Contains(bal.ProductId)).Sum(bal => bal.Qty);

        return new PartMaterialTypeDetailDto(m, products, products.Count, totalStock);
    }

    public async Task<int> CreatePartMaterialTypeAsync(PartMaterialType item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Tên nhóm chất liệu không được để trống.");

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"MAT{await db.PartMaterialTypes.CountAsync() + 1:D2}";
        }
        else
        {
            item.Code = item.Code.Trim().ToUpperInvariant();
        }

        bool exists = await db.PartMaterialTypes.AnyAsync(m => m.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã nhóm chất liệu '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.PartMaterialTypes.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdatePartMaterialTypeAsync(int id, PartMaterialType item)
    {
        var existing = await db.PartMaterialTypes.FirstOrDefaultAsync(m => m.Id == id);
        if (existing == null) return (false, "Không tìm thấy nhóm chất liệu.");

        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "Tên nhóm chất liệu không được để trống.");

        existing.Name = item.Name.Trim();
        existing.Remark = item.Remark?.Trim();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin nhóm chất liệu '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> TogglePartMaterialTypeStatusAsync(int id)
    {
        var existing = await db.PartMaterialTypes.FirstOrDefaultAsync(m => m.Id == id);
        if (existing == null) return (false, "Không tìm thấy nhóm chất liệu.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng nhóm chất liệu '{existing.Code}'." : $"Đã chuyển nhóm chất liệu '{existing.Code}' sang trạng thái Ngừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> DeletePartMaterialTypeAsync(int id)
    {
        var existing = await db.PartMaterialTypes.FirstOrDefaultAsync(m => m.Id == id);
        if (existing == null) return (false, "Không tìm thấy nhóm chất liệu.");

        bool isUsed = await db.Products.AnyAsync(p => p.PMType != null && (p.PMType.ToLower() == existing.Code.ToLower() || p.PMType.ToLower() == existing.Name.ToLower()));
        if (isUsed)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            return (true, $"Nhóm chất liệu '{existing.Code}' đang được gán cho sản phẩm trong kho nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.PartMaterialTypes.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa nhóm chất liệu '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh mục Dòng sản phẩm / Model hàng hóa kho tổng hợp kèm 4 thẻ KPI (port từ Mst_Model / OS_PrdCenter_Mst_Model Skycic: ModelCode, ModelName, BrandCode, OrgModelCode, FlagActive, Remark).</summary>
    public async Task<ProductModelReport> ProductModelsReportAsync(string? q = null, string? brandCode = null, bool? activeOnly = null)
    {
        var query = db.ProductModels.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(m => m.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(brandCode))
        {
            var bc = brandCode.Trim().ToLowerInvariant();
            query = query.Where(m => m.BrandCode != null && m.BrandCode.ToLower() == bc);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(m => m.Code.ToLower().Contains(kw) ||
                                     m.Name.ToLower().Contains(kw) ||
                                     (m.BrandCode != null && m.BrandCode.ToLower().Contains(kw)) ||
                                     (m.OrgModelCode != null && m.OrgModelCode.ToLower().Contains(kw)) ||
                                     (m.Remark != null && m.Remark.ToLower().Contains(kw)));
        }

        var list = await query.OrderBy(m => m.Code).ToListAsync();
        var allProducts = await db.Products.ToListAsync();
        var allBrands = await db.Brands.ToListAsync();
        var brandDict = allBrands.ToDictionary(b => b.Code, b => b.Name, StringComparer.OrdinalIgnoreCase);
        var balances = await BalancesAsync(null);

        var prodGroup = allProducts
            .Where(p => !string.IsNullOrEmpty(p.ModelCode))
            .GroupBy(p => p.ModelCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = list.Select(m =>
        {
            var pList = new List<Product>();
            if (prodGroup.TryGetValue(m.Code, out var listByCode)) pList.AddRange(listByCode);
            if (prodGroup.TryGetValue(m.Name, out var listByName))
            {
                foreach (var p in listByName)
                {
                    if (!pList.Any(x => x.Id == p.Id)) pList.Add(p);
                }
            }
            int pCount = pList.Count;
            var pIds = pList.Select(p => p.Id).ToHashSet();
            int totalStock = balances.Where(bal => pIds.Contains(bal.ProductId)).Sum(bal => bal.Qty);
            string? bName = m.BrandCode != null && brandDict.TryGetValue(m.BrandCode, out var bn) ? bn : m.BrandCode;
            return new ProductModelRow(m.Id, m.Code, m.Name, m.BrandCode, bName, m.OrgModelCode, m.Remark, m.IsActive, m.CreatedAt, pCount, totalStock);
        }).ToList();

        int totalModels = await db.ProductModels.CountAsync();
        int activeCount = await db.ProductModels.CountAsync(m => m.IsActive);
        int inactiveCount = totalModels - activeCount;

        var modelCodes = (await db.ProductModels.Select(m => m.Code).ToListAsync())
            .Select(c => c.ToLowerInvariant()).ToHashSet();
        int mappedProds = allProducts.Count(p => !string.IsNullOrEmpty(p.ModelCode) && modelCodes.Contains(p.ModelCode.Trim().ToLowerInvariant()));

        return new ProductModelReport(q, brandCode, activeOnly, totalModels, activeCount, inactiveCount, mappedProds, rows);
    }

    public Task<List<ProductModel>> ProductModelsAsync(string? q = null, string? brandCode = null, bool? activeOnly = null)
    {
        var query = db.ProductModels.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(m => m.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(brandCode))
        {
            var bc = brandCode.Trim().ToLowerInvariant();
            query = query.Where(m => m.BrandCode != null && m.BrandCode.ToLower() == bc);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(m => m.Code.ToLower().Contains(kw) ||
                                     m.Name.ToLower().Contains(kw) ||
                                     (m.BrandCode != null && m.BrandCode.ToLower().Contains(kw)));
        }
        return query.OrderBy(m => m.Code).ToListAsync();
    }

    public Task<ProductModel?> GetProductModelAsync(int id) =>
        db.ProductModels.FirstOrDefaultAsync(m => m.Id == id);

    public Task<ProductModel?> GetProductModelByCodeAsync(string code) =>
        db.ProductModels.FirstOrDefaultAsync(m => m.Code.ToLower() == code.Trim().ToLower());

    public async Task<ProductModelDetailDto?> GetProductModelDetailAsync(int id)
    {
        var m = await db.ProductModels.FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return null;

        Brand? brand = null;
        if (!string.IsNullOrEmpty(m.BrandCode))
        {
            brand = await db.Brands.FirstOrDefaultAsync(b => b.Code.ToLower() == m.BrandCode.ToLower());
        }

        var allProducts = await db.Products.OrderBy(p => p.Code).ToListAsync();
        var products = allProducts
            .Where(p => string.Equals(p.ModelCode, m.Code, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.ModelCode, m.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var balances = await BalancesAsync(null);
        var pIds = products.Select(p => p.Id).ToHashSet();
        int totalStock = balances.Where(bal => pIds.Contains(bal.ProductId)).Sum(bal => bal.Qty);

        return new ProductModelDetailDto(m, brand, products, products.Count, totalStock);
    }

    public async Task<int> CreateProductModelAsync(ProductModel item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Tên model / dòng sản phẩm không được để trống.");

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"MD{await db.ProductModels.CountAsync() + 1:D3}";
        }
        else
        {
            item.Code = item.Code.Trim().ToUpperInvariant();
        }

        bool exists = await db.ProductModels.AnyAsync(m => m.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã model '{item.Code}' đã tồn tại trong hệ thống.");

        if (!string.IsNullOrWhiteSpace(item.BrandCode))
            item.BrandCode = item.BrandCode.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(item.OrgModelCode))
            item.OrgModelCode = item.OrgModelCode.Trim();

        item.CreatedAt = DateTime.Now;
        db.ProductModels.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateProductModelAsync(int id, ProductModel item)
    {
        var existing = await db.ProductModels.FirstOrDefaultAsync(m => m.Id == id);
        if (existing == null) return (false, "Không tìm thấy model / dòng sản phẩm.");

        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "Tên model / dòng sản phẩm không được để trống.");

        existing.Name = item.Name.Trim();
        existing.BrandCode = string.IsNullOrWhiteSpace(item.BrandCode) ? null : item.BrandCode.Trim().ToUpperInvariant();
        existing.OrgModelCode = string.IsNullOrWhiteSpace(item.OrgModelCode) ? null : item.OrgModelCode.Trim();
        existing.Remark = item.Remark?.Trim();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin model '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> ToggleProductModelStatusAsync(int id)
    {
        var existing = await db.ProductModels.FirstOrDefaultAsync(m => m.Id == id);
        if (existing == null) return (false, "Không tìm thấy model / dòng sản phẩm.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng model '{existing.Code}'." : $"Đã chuyển model '{existing.Code}' sang trạng thái Ngừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> DeleteProductModelAsync(int id)
    {
        var existing = await db.ProductModels.FirstOrDefaultAsync(m => m.Id == id);
        if (existing == null) return (false, "Không tìm thấy model / dòng sản phẩm.");

        bool isUsed = await db.Products.AnyAsync(p => p.ModelCode != null && (p.ModelCode.ToLower() == existing.Code.ToLower() || p.ModelCode.ToLower() == existing.Name.ToLower()));
        if (isUsed)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            return (true, $"Model '{existing.Code}' đang được gán cho sản phẩm trong kho nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.ProductModels.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa model '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh mục Loại kho / Phân loại kho hàng tổng hợp kèm 4 thẻ KPI (port từ Mst_InventoryType Skycic: InvType, InvTypeName, FlagActive, Remark).</summary>
    public async Task<InventoryTypeReport> InventoryTypesReportAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.InventoryTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(t => t.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(t => t.Code.ToLower().Contains(kw) ||
                                     t.Name.ToLower().Contains(kw) ||
                                     (t.Remark != null && t.Remark.ToLower().Contains(kw)));
        }

        var types = await query.OrderBy(t => t.Code).ToListAsync();
        var allWarehouses = await db.Warehouses.ToListAsync();
        var balances = await BalancesAsync(null);

        var whGroup = allWarehouses
            .Where(w => !string.IsNullOrEmpty(w.InvTypeCode))
            .GroupBy(w => w.InvTypeCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = types.Select(t =>
        {
            var wList = new List<Warehouse>();
            if (whGroup.TryGetValue(t.Code, out var listByCode)) wList.AddRange(listByCode);
            if (whGroup.TryGetValue(t.Name, out var listByName))
            {
                foreach (var w in listByName)
                {
                    if (!wList.Any(x => x.Id == w.Id)) wList.Add(w);
                }
            }
            int wCount = wList.Count;
            var wIds = wList.Select(w => w.Id).ToHashSet();
            int totalStock = balances.Where(bal => wIds.Contains(bal.WarehouseId)).Sum(bal => bal.Qty);
            return new InventoryTypeRow(t.Id, t.Code, t.Name, t.Remark, t.IsActive, t.CreatedAt, wCount, totalStock);
        }).ToList();

        int totalTypes = await db.InventoryTypes.CountAsync();
        int activeCount = await db.InventoryTypes.CountAsync(t => t.IsActive);
        int inactiveCount = totalTypes - activeCount;

        var typeCodes = (await db.InventoryTypes.Select(t => t.Code).ToListAsync())
            .Select(c => c.ToLowerInvariant()).ToHashSet();
        int mappedWhs = allWarehouses.Count(w => !string.IsNullOrEmpty(w.InvTypeCode) && typeCodes.Contains(w.InvTypeCode.Trim().ToLowerInvariant()));

        return new InventoryTypeReport(q, activeOnly, totalTypes, activeCount, inactiveCount, mappedWhs, rows);
    }

    public Task<List<InventoryType>> InventoryTypesAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.InventoryTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(t => t.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(t => t.Code.ToLower().Contains(kw) || t.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(t => t.Code).ToListAsync();
    }

    public Task<InventoryType?> GetInventoryTypeAsync(int id) =>
        db.InventoryTypes.FirstOrDefaultAsync(t => t.Id == id);

    public Task<InventoryType?> GetInventoryTypeByCodeAsync(string code) =>
        db.InventoryTypes.FirstOrDefaultAsync(t => t.Code.ToLower() == code.Trim().ToLower());

    public async Task<InventoryTypeDetailDto?> GetInventoryTypeDetailAsync(int id)
    {
        var item = await db.InventoryTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return null;

        var allWarehouses = await db.Warehouses.OrderBy(w => w.Code).ToListAsync();
        var warehouses = allWarehouses
            .Where(w => string.Equals(w.InvTypeCode, item.Code, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(w.InvTypeCode, item.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var balances = await BalancesAsync(null);
        var wIds = warehouses.Select(w => w.Id).ToHashSet();
        int totalStock = balances.Where(bal => wIds.Contains(bal.WarehouseId)).Sum(bal => bal.Qty);

        return new InventoryTypeDetailDto(item, warehouses, warehouses.Count, totalStock);
    }

    public async Task<int> CreateInventoryTypeAsync(InventoryType item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Tên loại kho không được để trống.");

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"KHO_TYPE{await db.InventoryTypes.CountAsync() + 1:D2}";
        }
        else
        {
            item.Code = item.Code.Trim().ToUpperInvariant();
        }

        bool exists = await db.InventoryTypes.AnyAsync(t => t.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã loại kho '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.InventoryTypes.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateInventoryTypeAsync(int id, InventoryType item)
    {
        var existing = await db.InventoryTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại kho.");

        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "Tên loại kho không được để trống.");

        existing.Name = item.Name.Trim();
        existing.Remark = item.Remark?.Trim();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin loại kho '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> ToggleInventoryTypeStatusAsync(int id)
    {
        var existing = await db.InventoryTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại kho.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng loại kho '{existing.Code}'." : $"Đã chuyển loại kho '{existing.Code}' sang trạng thái Ngừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> DeleteInventoryTypeAsync(int id)
    {
        var existing = await db.InventoryTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại kho.");

        bool isUsed = await db.Warehouses.AnyAsync(w => w.InvTypeCode != null && (w.InvTypeCode.ToLower() == existing.Code.ToLower() || w.InvTypeCode.ToLower() == existing.Name.ToLower()));
        if (isUsed)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            return (true, $"Loại kho '{existing.Code}' đang được gán cho kho trong hệ thống nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.InventoryTypes.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa loại kho '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh mục Cấp kho / Phân cấp quản lý kho hàng tổng hợp kèm 4 thẻ KPI (port từ Mst_InventoryLevelType Skycic: InvLevelType, InvLevelTypeName, FlagActive, Remark).</summary>
    public async Task<InventoryLevelTypeReport> InventoryLevelTypesReportAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.InventoryLevelTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(t => t.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(t => t.Code.ToLower().Contains(kw) ||
                                     t.Name.ToLower().Contains(kw) ||
                                     (t.Remark != null && t.Remark.ToLower().Contains(kw)));
        }

        var levels = await query.OrderBy(t => t.Code).ToListAsync();
        var allWarehouses = await db.Warehouses.ToListAsync();
        var balances = await BalancesAsync(null);

        var whGroup = allWarehouses
            .Where(w => !string.IsNullOrEmpty(w.InvLevelTypeCode))
            .GroupBy(w => w.InvLevelTypeCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var rows = levels.Select(t =>
        {
            var wList = new List<Warehouse>();
            if (whGroup.TryGetValue(t.Code, out var listByCode)) wList.AddRange(listByCode);
            if (whGroup.TryGetValue(t.Name, out var listByName))
            {
                foreach (var w in listByName)
                {
                    if (!wList.Any(x => x.Id == w.Id)) wList.Add(w);
                }
            }
            int wCount = wList.Count;
            var wIds = wList.Select(w => w.Id).ToHashSet();
            int totalStock = balances.Where(bal => wIds.Contains(bal.WarehouseId)).Sum(bal => bal.Qty);
            return new InventoryLevelTypeRow(t.Id, t.Code, t.Name, t.Remark, t.IsActive, t.CreatedAt, wCount, totalStock);
        }).ToList();

        int totalLevels = await db.InventoryLevelTypes.CountAsync();
        int activeCount = await db.InventoryLevelTypes.CountAsync(t => t.IsActive);
        int inactiveCount = totalLevels - activeCount;

        var levelCodes = (await db.InventoryLevelTypes.Select(t => t.Code).ToListAsync())
            .Select(c => c.ToLowerInvariant()).ToHashSet();
        int mappedWhs = allWarehouses.Count(w => !string.IsNullOrEmpty(w.InvLevelTypeCode) && levelCodes.Contains(w.InvLevelTypeCode.Trim().ToLowerInvariant()));

        return new InventoryLevelTypeReport(q, activeOnly, totalLevels, activeCount, inactiveCount, mappedWhs, rows);
    }

    public Task<List<InventoryLevelType>> InventoryLevelTypesAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.InventoryLevelTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(t => t.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(t => t.Code.ToLower().Contains(kw) || t.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(t => t.Code).ToListAsync();
    }

    public Task<InventoryLevelType?> GetInventoryLevelTypeAsync(int id) =>
        db.InventoryLevelTypes.FirstOrDefaultAsync(t => t.Id == id);

    public Task<InventoryLevelType?> GetInventoryLevelTypeByCodeAsync(string code) =>
        db.InventoryLevelTypes.FirstOrDefaultAsync(t => t.Code.ToLower() == code.Trim().ToLower());

    public async Task<InventoryLevelTypeDetailDto?> GetInventoryLevelTypeDetailAsync(int id)
    {
        var item = await db.InventoryLevelTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return null;

        var allWarehouses = await db.Warehouses.OrderBy(w => w.Code).ToListAsync();
        var warehouses = allWarehouses
            .Where(w => string.Equals(w.InvLevelTypeCode, item.Code, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(w.InvLevelTypeCode, item.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var balances = await BalancesAsync(null);
        var wIds = warehouses.Select(w => w.Id).ToHashSet();
        int totalStock = balances.Where(bal => wIds.Contains(bal.WarehouseId)).Sum(bal => bal.Qty);

        return new InventoryLevelTypeDetailDto(item, warehouses, warehouses.Count, totalStock);
    }

    public async Task<int> CreateInventoryLevelTypeAsync(InventoryLevelType item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Tên cấp kho không được để trống.");

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"CAP_{await db.InventoryLevelTypes.CountAsync() + 1:D2}";
        }
        else
        {
            item.Code = item.Code.Trim().ToUpperInvariant();
        }

        bool exists = await db.InventoryLevelTypes.AnyAsync(t => t.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã cấp kho '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.InventoryLevelTypes.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateInventoryLevelTypeAsync(int id, InventoryLevelType item)
    {
        var existing = await db.InventoryLevelTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy cấp kho.");

        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "Tên cấp kho không được để trống.");

        existing.Name = item.Name.Trim();
        existing.Remark = item.Remark?.Trim();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin cấp kho '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> ToggleInventoryLevelTypeStatusAsync(int id)
    {
        var existing = await db.InventoryLevelTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy cấp kho.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng cấp kho '{existing.Code}'." : $"Đã chuyển cấp kho '{existing.Code}' sang trạng thái Ngừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> DeleteInventoryLevelTypeAsync(int id)
    {
        var existing = await db.InventoryLevelTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy cấp kho.");

        bool isUsed = await db.Warehouses.AnyAsync(w => w.InvLevelTypeCode != null && (w.InvLevelTypeCode.ToLower() == existing.Code.ToLower() || w.InvLevelTypeCode.ToLower() == existing.Name.ToLower()));
        if (isUsed)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            return (true, $"Cấp kho '{existing.Code}' đang được gán cho kho trong hệ thống nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.InventoryLevelTypes.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa cấp kho '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh mục Loại hình / Lý do Nhập kho tổng hợp kèm 4 thẻ KPI (port từ Mst_InvInType Skycic: InvInType, InvInTypeName, FlagActive, FlagStatistic, Remark).</summary>
    public async Task<InventoryInTypeReport> InventoryInTypesReportAsync(string? q = null, bool? activeOnly = null, bool? statisticOnly = null)
    {
        var query = db.InventoryInTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(t => t.IsActive == activeOnly.Value);
        if (statisticOnly.HasValue) query = query.Where(t => t.FlagStatistic == statisticOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(t => t.Code.ToLower().Contains(kw) ||
                                     t.Name.ToLower().Contains(kw) ||
                                     (t.Remark != null && t.Remark.ToLower().Contains(kw)));
        }

        var types = await query.OrderBy(t => t.Code).ToListAsync();
        var allInDocs = await db.Docs.Include(d => d.Lines).Where(d => d.Type == DocType.In).ToListAsync();

        var rows = types.Select(t =>
        {
            var matchedDocs = allInDocs.Where(d =>
            {
                var codeUpper = t.Code.ToUpperInvariant();
                if (!string.IsNullOrEmpty(d.Note) && d.Note.ToUpperInvariant().Contains(codeUpper)) return true;
                if (!string.IsNullOrEmpty(d.RefNo) && d.RefNo.ToUpperInvariant().Contains(codeUpper)) return true;

                if (codeUpper == "IN_AUDIT" && (d.Note?.Contains("kiểm kê") == true || d.Note?.Contains("cân bằng") == true)) return true;
                if (codeUpper == "IN_RETURN" && (d.Note?.Contains("khách trả") == true || d.Note?.Contains("đổi trả") == true || !string.IsNullOrEmpty(d.CustomerCode))) return true;
                if (codeUpper == "IN_PROD" && (d.Note?.Contains("thành phẩm") == true || d.Note?.Contains("sản xuất") == true)) return true;
                if (codeUpper == "IN_BUY" && (!string.IsNullOrEmpty(d.SupplierCode) || !string.IsNullOrEmpty(d.SupplierName)) &&
                    d.Note?.Contains("kiểm kê") != true && d.Note?.Contains("khách trả") != true && d.Note?.Contains("thành phẩm") != true) return true;
                if (codeUpper == "IN_TRANSFER" && d.Note?.Contains("chuyển kho") == true) return true;

                return false;
            }).ToList();

            int docsCount = matchedDocs.Count;
            int qtyIn = matchedDocs.Sum(d => d.TotalQty);

            return new InventoryInTypeRow(t.Id, t.Code, t.Name, t.FlagStatistic, t.IsActive, t.Remark, t.CreatedAt, docsCount, qtyIn);
        }).ToList();

        int totalTypes = await db.InventoryInTypes.CountAsync();
        int activeCount = await db.InventoryInTypes.CountAsync(t => t.IsActive);
        int statisticCount = await db.InventoryInTypes.CountAsync(t => t.FlagStatistic && t.IsActive);
        int inactiveCount = totalTypes - activeCount;
        int totalInDocsCount = allInDocs.Count;

        return new InventoryInTypeReport(q, activeOnly, statisticOnly, totalTypes, activeCount, statisticCount, inactiveCount, totalInDocsCount, rows);
    }

    public Task<List<InventoryInType>> InventoryInTypesAsync(string? q = null, bool? activeOnly = null, bool? statisticOnly = null)
    {
        var query = db.InventoryInTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(t => t.IsActive == activeOnly.Value);
        if (statisticOnly.HasValue) query = query.Where(t => t.FlagStatistic == statisticOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(t => t.Code.ToLower().Contains(kw) || t.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(t => t.Code).ToListAsync();
    }

    public Task<InventoryInType?> GetInventoryInTypeAsync(int id) =>
        db.InventoryInTypes.FirstOrDefaultAsync(t => t.Id == id);

    public Task<InventoryInType?> GetInventoryInTypeByCodeAsync(string code) =>
        db.InventoryInTypes.FirstOrDefaultAsync(t => t.Code.ToLower() == code.Trim().ToLower());

    public async Task<InventoryInTypeDetailDto?> GetInventoryInTypeDetailAsync(int id)
    {
        var item = await db.InventoryInTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return null;

        var allInDocs = await db.Docs.Include(d => d.Lines).ThenInclude(l => l.Product)
                                     .Include(d => d.ToWarehouse)
                                     .Where(d => d.Type == DocType.In)
                                     .OrderByDescending(d => d.Date)
                                     .ToListAsync();

        var codeUpper = item.Code.ToUpperInvariant();
        var matchedDocs = allInDocs.Where(d =>
        {
            if (!string.IsNullOrEmpty(d.Note) && d.Note.ToUpperInvariant().Contains(codeUpper)) return true;
            if (!string.IsNullOrEmpty(d.RefNo) && d.RefNo.ToUpperInvariant().Contains(codeUpper)) return true;

            if (codeUpper == "IN_AUDIT" && (d.Note?.Contains("kiểm kê") == true || d.Note?.Contains("cân bằng") == true)) return true;
            if (codeUpper == "IN_RETURN" && (d.Note?.Contains("khách trả") == true || d.Note?.Contains("đổi trả") == true || !string.IsNullOrEmpty(d.CustomerCode))) return true;
            if (codeUpper == "IN_PROD" && (d.Note?.Contains("thành phẩm") == true || d.Note?.Contains("sản xuất") == true)) return true;
            if (codeUpper == "IN_BUY" && (!string.IsNullOrEmpty(d.SupplierCode) || !string.IsNullOrEmpty(d.SupplierName)) &&
                d.Note?.Contains("kiểm kê") != true && d.Note?.Contains("khách trả") != true && d.Note?.Contains("thành phẩm") != true) return true;
            if (codeUpper == "IN_TRANSFER" && d.Note?.Contains("chuyển kho") == true) return true;

            return false;
        }).ToList();

        int totalDocs = matchedDocs.Count;
        int totalQtyIn = matchedDocs.Sum(d => d.TotalQty);

        return new InventoryInTypeDetailDto(item, matchedDocs, totalDocs, totalQtyIn);
    }

    public async Task<int> CreateInventoryInTypeAsync(InventoryInType item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Tên loại nhập kho không được để trống.");

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"IN_TYPE{await db.InventoryInTypes.CountAsync() + 1:D2}";
        }
        else
        {
            item.Code = item.Code.Trim().ToUpperInvariant();
        }

        bool exists = await db.InventoryInTypes.AnyAsync(t => t.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã loại nhập kho '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.InventoryInTypes.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateInventoryInTypeAsync(int id, InventoryInType item)
    {
        var existing = await db.InventoryInTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại nhập kho.");

        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "Tên loại nhập kho không được để trống.");

        existing.Name = item.Name.Trim();
        existing.Remark = item.Remark?.Trim();
        existing.FlagStatistic = item.FlagStatistic;
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin loại nhập kho '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> ToggleInventoryInTypeStatusAsync(int id)
    {
        var existing = await db.InventoryInTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại nhập kho.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng loại nhập kho '{existing.Code}'." : $"Đã chuyển loại nhập kho '{existing.Code}' sang trạng thái Ngừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> ToggleInventoryInTypeStatisticAsync(int id)
    {
        var existing = await db.InventoryInTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại nhập kho.");

        existing.FlagStatistic = !existing.FlagStatistic;
        await db.SaveChangesAsync();
        return (true, existing.FlagStatistic ? $"Đã bật cờ tính vào thống kê phân tích mua hàng/sản lượng cho loại '{existing.Code}'." : $"Đã tắt cờ tính vào thống kê mua hàng/sản lượng cho loại '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> DeleteInventoryInTypeAsync(int id)
    {
        var existing = await db.InventoryInTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại nhập kho.");

        bool isUsed = await db.Docs.AnyAsync(d => d.Type == DocType.In && ((d.Note != null && d.Note.Contains(existing.Code)) || (d.RefNo != null && d.RefNo.Contains(existing.Code))));
        if (isUsed)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            return (true, $"Loại nhập kho '{existing.Code}' đã phát sinh giao dịch nhập kho trong hệ thống nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.InventoryInTypes.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa loại nhập kho '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh mục Loại hình / Lý do Xuất kho tổng hợp kèm 4 thẻ KPI (port từ Mst_InvOutType Skycic: InvOutType, InvOutTypeName, FlagActive, FlagStatistic, Remark).</summary>
    public async Task<InventoryOutTypeReport> InventoryOutTypesReportAsync(string? q = null, bool? activeOnly = null, bool? statisticOnly = null)
    {
        var query = db.InventoryOutTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(t => t.IsActive == activeOnly.Value);
        if (statisticOnly.HasValue) query = query.Where(t => t.FlagStatistic == statisticOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(t => t.Code.ToLower().Contains(kw) ||
                                     t.Name.ToLower().Contains(kw) ||
                                     (t.Remark != null && t.Remark.ToLower().Contains(kw)));
        }

        var types = await query.OrderBy(t => t.Code).ToListAsync();
        var allOutDocs = await db.Docs.Include(d => d.Lines).Where(d => d.Type == DocType.Out).ToListAsync();

        var rows = types.Select(t =>
        {
            var matchedDocs = allOutDocs.Where(d =>
            {
                var codeUpper = t.Code.ToUpperInvariant();
                if (!string.IsNullOrEmpty(d.Note) && d.Note.ToUpperInvariant().Contains(codeUpper)) return true;
                if (!string.IsNullOrEmpty(d.RefNo) && d.RefNo.ToUpperInvariant().Contains(codeUpper)) return true;

                if (codeUpper == "OUT_AUDIT" && (d.Note?.Contains("kiểm kê") == true || d.Note?.Contains("cân bằng") == true)) return true;
                if (codeUpper == "OUT_RETURN_SUP" && (d.Note?.Contains("trả ncc") == true || d.Note?.Contains("trả nhà cung cấp") == true || d.Note?.Contains("trả lại") == true || !string.IsNullOrEmpty(d.SupplierCode))) return true;
                if (codeUpper == "OUT_PROD" && (d.Note?.Contains("sản xuất") == true || d.Note?.Contains("cấp phát") == true || d.Note?.Contains("nvl") == true)) return true;
                if (codeUpper == "OUT_DISPOSAL" && (d.Note?.Contains("hủy") == true || d.Note?.Contains("thanh lý") == true || d.Note?.Contains("hỏng") == true)) return true;
                if (codeUpper == "OUT_SAMPLE" && (d.Note?.Contains("hàng mẫu") == true || d.Note?.Contains("triển lãm") == true || d.Note?.Contains("khuyến mại") == true)) return true;
                if (codeUpper == "OUT_TRANSFER" && d.Note?.Contains("chuyển kho") == true) return true;
                if (codeUpper == "OUT_SALE" && (!string.IsNullOrEmpty(d.CustomerCode) || !string.IsNullOrEmpty(d.CustomerName) ||
                    (d.Note?.Contains("kiểm kê") != true && d.Note?.Contains("trả ncc") != true && d.Note?.Contains("sản xuất") != true && d.Note?.Contains("hủy") != true && d.Note?.Contains("hàng mẫu") != true && d.Note?.Contains("chuyển kho") != true))) return true;

                return false;
            }).ToList();

            int docsCount = matchedDocs.Count;
            int qtyOut = matchedDocs.Sum(d => d.TotalQty);

            return new InventoryOutTypeRow(t.Id, t.Code, t.Name, t.FlagStatistic, t.IsActive, t.Remark, t.CreatedAt, docsCount, qtyOut);
        }).ToList();

        int totalTypes = await db.InventoryOutTypes.CountAsync();
        int activeCount = await db.InventoryOutTypes.CountAsync(t => t.IsActive);
        int statisticCount = await db.InventoryOutTypes.CountAsync(t => t.FlagStatistic && t.IsActive);
        int inactiveCount = totalTypes - activeCount;
        int totalOutDocsCount = allOutDocs.Count;

        return new InventoryOutTypeReport(q, activeOnly, statisticOnly, totalTypes, activeCount, statisticCount, inactiveCount, totalOutDocsCount, rows);
    }

    public Task<List<InventoryOutType>> InventoryOutTypesAsync(string? q = null, bool? activeOnly = null, bool? statisticOnly = null)
    {
        var query = db.InventoryOutTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(t => t.IsActive == activeOnly.Value);
        if (statisticOnly.HasValue) query = query.Where(t => t.FlagStatistic == statisticOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(t => t.Code.ToLower().Contains(kw) || t.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(t => t.Code).ToListAsync();
    }

    public Task<InventoryOutType?> GetInventoryOutTypeAsync(int id) =>
        db.InventoryOutTypes.FirstOrDefaultAsync(t => t.Id == id);

    public Task<InventoryOutType?> GetInventoryOutTypeByCodeAsync(string code) =>
        db.InventoryOutTypes.FirstOrDefaultAsync(t => t.Code.ToLower() == code.Trim().ToLower());

    public async Task<InventoryOutTypeDetailDto?> GetInventoryOutTypeDetailAsync(int id)
    {
        var item = await db.InventoryOutTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return null;

        var allOutDocs = await db.Docs.Include(d => d.Lines).ThenInclude(l => l.Product)
                                      .Include(d => d.FromWarehouse)
                                      .Where(d => d.Type == DocType.Out)
                                      .OrderByDescending(d => d.Date)
                                      .ToListAsync();

        var codeUpper = item.Code.ToUpperInvariant();
        var matchedDocs = allOutDocs.Where(d =>
        {
            if (!string.IsNullOrEmpty(d.Note) && d.Note.ToUpperInvariant().Contains(codeUpper)) return true;
            if (!string.IsNullOrEmpty(d.RefNo) && d.RefNo.ToUpperInvariant().Contains(codeUpper)) return true;

            if (codeUpper == "OUT_AUDIT" && (d.Note?.Contains("kiểm kê") == true || d.Note?.Contains("cân bằng") == true)) return true;
            if (codeUpper == "OUT_RETURN_SUP" && (d.Note?.Contains("trả ncc") == true || d.Note?.Contains("trả nhà cung cấp") == true || d.Note?.Contains("trả lại") == true || !string.IsNullOrEmpty(d.SupplierCode))) return true;
            if (codeUpper == "OUT_PROD" && (d.Note?.Contains("sản xuất") == true || d.Note?.Contains("cấp phát") == true || d.Note?.Contains("nvl") == true)) return true;
            if (codeUpper == "OUT_DISPOSAL" && (d.Note?.Contains("hủy") == true || d.Note?.Contains("thanh lý") == true || d.Note?.Contains("hỏng") == true)) return true;
            if (codeUpper == "OUT_SAMPLE" && (d.Note?.Contains("hàng mẫu") == true || d.Note?.Contains("triển lãm") == true || d.Note?.Contains("khuyến mại") == true)) return true;
            if (codeUpper == "OUT_TRANSFER" && d.Note?.Contains("chuyển kho") == true) return true;
            if (codeUpper == "OUT_SALE" && (!string.IsNullOrEmpty(d.CustomerCode) || !string.IsNullOrEmpty(d.CustomerName) ||
                (d.Note?.Contains("kiểm kê") != true && d.Note?.Contains("trả ncc") != true && d.Note?.Contains("sản xuất") != true && d.Note?.Contains("hủy") != true && d.Note?.Contains("hàng mẫu") != true && d.Note?.Contains("chuyển kho") != true))) return true;

            return false;
        }).ToList();

        int totalDocs = matchedDocs.Count;
        int totalQtyOut = matchedDocs.Sum(d => d.TotalQty);

        return new InventoryOutTypeDetailDto(item, matchedDocs, totalDocs, totalQtyOut);
    }

    public async Task<int> CreateInventoryOutTypeAsync(InventoryOutType item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Tên loại xuất kho không được để trống.");

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"OUT_TYPE{await db.InventoryOutTypes.CountAsync() + 1:D2}";
        }
        else
        {
            item.Code = item.Code.Trim().ToUpperInvariant();
        }

        bool exists = await db.InventoryOutTypes.AnyAsync(t => t.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã loại xuất kho '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.InventoryOutTypes.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateInventoryOutTypeAsync(int id, InventoryOutType item)
    {
        var existing = await db.InventoryOutTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại xuất kho.");

        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "Tên loại xuất kho không được để trống.");

        existing.Name = item.Name.Trim();
        existing.Remark = item.Remark?.Trim();
        existing.FlagStatistic = item.FlagStatistic;
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin loại xuất kho '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> ToggleInventoryOutTypeStatusAsync(int id)
    {
        var existing = await db.InventoryOutTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại xuất kho.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng loại xuất kho '{existing.Code}'." : $"Đã chuyển loại xuất kho '{existing.Code}' sang trạng thái Ngừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> ToggleInventoryOutTypeStatisticAsync(int id)
    {
        var existing = await db.InventoryOutTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại xuất kho.");

        existing.FlagStatistic = !existing.FlagStatistic;
        await db.SaveChangesAsync();
        return (true, existing.FlagStatistic ? $"Đã bật cờ tính vào thống kê sản lượng/doanh số xuất cho loại '{existing.Code}'." : $"Đã tắt cờ tính vào thống kê xuất cho loại '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> DeleteInventoryOutTypeAsync(int id)
    {
        var existing = await db.InventoryOutTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại xuất kho.");

        bool isUsed = await db.Docs.AnyAsync(d => d.Type == DocType.Out && ((d.Note != null && d.Note.Contains(existing.Code)) || (d.RefNo != null && d.RefNo.Contains(existing.Code))));
        if (isUsed)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            return (true, $"Loại xuất kho '{existing.Code}' đã phát sinh giao dịch xuất kho trong hệ thống nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.InventoryOutTypes.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa loại xuất kho '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh sách phân quyền người dùng quản lý kho tổng hợp kèm 4 thẻ KPI (port từ Mst_UserMapInventory Skycic: OrgID, UserCode, InvCode, Remark, LogLUBy, LogLUDTimeUTC).</summary>
    public async Task<UserMapInventoryReport> UserMapInventoriesReportAsync(int? warehouseId = null, string? userRole = null, bool? activeOnly = null, string? q = null)
    {
        var query = db.UserMapInventories.Include(m => m.Warehouse).AsQueryable();

        if (warehouseId.HasValue && warehouseId.Value > 0)
            query = query.Where(m => m.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrWhiteSpace(userRole))
            query = query.Where(m => m.UserRole.ToLower() == userRole.Trim().ToLower());

        if (activeOnly.HasValue)
            query = query.Where(m => m.IsActive == activeOnly.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(m => m.UserCode.ToLower().Contains(kw)
                                  || m.UserName.ToLower().Contains(kw)
                                  || m.Warehouse.Code.ToLower().Contains(kw)
                                  || m.Warehouse.Name.ToLower().Contains(kw)
                                  || (m.Email != null && m.Email.ToLower().Contains(kw))
                                  || (m.Phone != null && m.Phone.ToLower().Contains(kw))
                                  || (m.Remark != null && m.Remark.ToLower().Contains(kw)));
        }

        var list = await query.OrderBy(m => m.Warehouse.Code).ThenBy(m => m.UserName).ToListAsync();

        var rows = list.Select(m => new UserMapInventoryRow(
            m.Id,
            m.WarehouseId,
            m.Warehouse.Code,
            m.Warehouse.Name,
            m.Warehouse.InvTypeCode,
            m.Warehouse.InvLevelTypeCode,
            m.UserCode,
            m.UserName,
            m.UserRole,
            m.Email,
            m.Phone,
            m.IsActive,
            m.Remark,
            m.AssignedBy,
            m.AssignedAt
        )).ToList();

        int totalAssignments = await db.UserMapInventories.CountAsync();
        int activeAssignments = await db.UserMapInventories.CountAsync(m => m.IsActive);
        int totalUsersAssigned = await db.UserMapInventories.Where(m => m.IsActive).Select(m => m.UserCode).Distinct().CountAsync();

        var assignedWarehouseIds = await db.UserMapInventories.Where(m => m.IsActive).Select(m => m.WarehouseId).Distinct().ToListAsync();
        int totalWarehouses = await db.Warehouses.CountAsync();
        int unassignedWarehousesCount = Math.Max(0, totalWarehouses - assignedWarehouseIds.Count);

        return new UserMapInventoryReport(warehouseId, userRole, activeOnly, q, totalAssignments, activeAssignments, totalUsersAssigned, unassignedWarehousesCount, rows);
    }

    public Task<List<UserMapInventory>> UserMapInventoriesAsync(int? warehouseId = null, bool? activeOnly = null)
    {
        var query = db.UserMapInventories.Include(m => m.Warehouse).AsQueryable();
        if (warehouseId.HasValue && warehouseId.Value > 0) query = query.Where(m => m.WarehouseId == warehouseId.Value);
        if (activeOnly.HasValue) query = query.Where(m => m.IsActive == activeOnly.Value);
        return query.OrderBy(m => m.Warehouse.Code).ThenBy(m => m.UserName).ToListAsync();
    }

    public Task<UserMapInventory?> GetUserMapInventoryAsync(int id) =>
        db.UserMapInventories.Include(m => m.Warehouse).FirstOrDefaultAsync(m => m.Id == id);

    public Task<List<UserMapInventory>> GetUserMapsByWarehouseAsync(int warehouseId) =>
        db.UserMapInventories.Include(m => m.Warehouse).Where(m => m.WarehouseId == warehouseId).OrderBy(m => m.UserName).ToListAsync();

    public Task<List<UserMapInventory>> GetUserMapsByUserCodeAsync(string userCode) =>
        db.UserMapInventories.Include(m => m.Warehouse).Where(m => m.UserCode.ToLower() == userCode.Trim().ToLower()).OrderBy(m => m.Warehouse.Code).ToListAsync();

    public async Task<List<WarehouseAssignmentSummary>> GetWarehouseAssignmentSummariesAsync()
    {
        var warehouses = await db.Warehouses.OrderBy(w => w.Code).ToListAsync();
        var maps = await db.UserMapInventories.Where(m => m.IsActive).ToListAsync();

        return warehouses.Select(w =>
        {
            var userNames = maps.Where(m => m.WarehouseId == w.Id).Select(m => $"{m.UserName} ({m.UserRole})").ToList();
            return new WarehouseAssignmentSummary(w.Id, w.Code, w.Name, userNames.Count, userNames);
        }).ToList();
    }

    public async Task<int> CreateUserMapInventoryAsync(UserMapInventory item)
    {
        var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == item.WarehouseId);
        if (wh == null) throw new InvalidOperationException($"Không tìm thấy kho có ID {item.WarehouseId}.");

        item.UserCode = item.UserCode.Trim().ToLowerInvariant();
        item.UserName = item.UserName.Trim();
        item.UserRole = string.IsNullOrWhiteSpace(item.UserRole) ? "Thủ kho chính" : item.UserRole.Trim();

        bool exists = await db.UserMapInventories.AnyAsync(m => m.WarehouseId == item.WarehouseId && m.UserCode == item.UserCode);
        if (exists) throw new InvalidOperationException($"Người dùng '{item.UserCode}' đã được phân quyền tại kho '{wh.Name}'.");

        db.UserMapInventories.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateUserMapInventoryAsync(int id, UserMapInventory item)
    {
        var existing = await db.UserMapInventories.Include(m => m.Warehouse).FirstOrDefaultAsync(m => m.Id == id);
        if (existing == null) return (false, "Không tìm thấy bản ghi phân quyền kho.");

        if (string.IsNullOrWhiteSpace(item.UserName)) return (false, "Họ tên người dùng không được để trống.");

        existing.UserName = item.UserName.Trim();
        existing.UserRole = string.IsNullOrWhiteSpace(item.UserRole) ? "Thủ kho chính" : item.UserRole.Trim();
        existing.Email = item.Email?.Trim();
        existing.Phone = item.Phone?.Trim();
        existing.Remark = item.Remark?.Trim();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật phân quyền người dùng '{existing.UserCode}' tại kho '{existing.Warehouse.Name}'.");
    }

    public async Task<(bool ok, string msg)> ToggleUserMapInventoryStatusAsync(int id)
    {
        var existing = await db.UserMapInventories.Include(m => m.Warehouse).FirstOrDefaultAsync(m => m.Id == id);
        if (existing == null) return (false, "Không tìm thấy bản ghi phân quyền kho.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive
            ? $"Đã kích hoạt lại phân quyền quản lý kho '{existing.Warehouse.Name}' cho nhân viên '{existing.UserName}'."
            : $"Đã tạm dừng phân quyền quản lý kho '{existing.Warehouse.Name}' của nhân viên '{existing.UserName}'.");
    }

    public async Task<(bool ok, string msg)> DeleteUserMapInventoryAsync(int id)
    {
        var existing = await db.UserMapInventories.Include(m => m.Warehouse).FirstOrDefaultAsync(m => m.Id == id);
        if (existing == null) return (false, "Không tìm thấy bản ghi phân quyền kho.");

        db.UserMapInventories.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã hủy gán phân quyền thủ kho '{existing.UserName}' ({existing.UserCode}) khỏi kho '{existing.Warehouse.Name}'.");
    }

    public async Task<(bool ok, string msg, int count)> BatchMapUsersToWarehouseAsync(int warehouseId, List<BatchMapUserItemDto> users, string assignedBy)
    {
        var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId);
        if (wh == null) return (false, $"Không tìm thấy kho có ID {warehouseId}.", 0);

        if (users == null || users.Count == 0) return (false, "Không có nhân viên nào được chọn.", 0);

        int addedCount = 0;
        foreach (var u in users)
        {
            var uCode = u.UserCode.Trim().ToLowerInvariant();
            var existing = await db.UserMapInventories.FirstOrDefaultAsync(m => m.WarehouseId == warehouseId && m.UserCode == uCode);
            if (existing != null)
            {
                existing.IsActive = true;
                if (!string.IsNullOrWhiteSpace(u.UserName)) existing.UserName = u.UserName.Trim();
                if (!string.IsNullOrWhiteSpace(u.UserRole)) existing.UserRole = u.UserRole.Trim();
                if (!string.IsNullOrWhiteSpace(u.Email)) existing.Email = u.Email.Trim();
                if (!string.IsNullOrWhiteSpace(u.Phone)) existing.Phone = u.Phone.Trim();
                if (!string.IsNullOrWhiteSpace(u.Remark)) existing.Remark = u.Remark.Trim();
                existing.AssignedBy = assignedBy;
                existing.AssignedAt = DateTime.Now;
                addedCount++;
            }
            else
            {
                db.UserMapInventories.Add(new UserMapInventory
                {
                    WarehouseId = warehouseId,
                    UserCode = uCode,
                    UserName = string.IsNullOrWhiteSpace(u.UserName) ? uCode : u.UserName.Trim(),
                    UserRole = string.IsNullOrWhiteSpace(u.UserRole) ? "Thủ kho chính" : u.UserRole.Trim(),
                    Email = u.Email?.Trim(),
                    Phone = u.Phone?.Trim(),
                    Remark = u.Remark?.Trim() ?? "Phân công quản lý kho hàng loạt",
                    IsActive = true,
                    AssignedBy = assignedBy,
                    AssignedAt = DateTime.Now
                });
                addedCount++;
            }
        }

        await db.SaveChangesAsync();
        return (true, $"Đã phân công thành công {addedCount} nhân sự quản lý cho kho '{wh.Name}'.", addedCount);
    }

    /// <summary>Báo cáo danh mục Nhóm hàng hóa / Phân nhóm sản phẩm kho kèm 4 thẻ KPI (port từ Mst_ProductGroup & Mst_ProductGroupSub Skycic).</summary>
    public async Task<ProductGroupReport> ProductGroupsReportAsync(string? q = null, string? parentCode = null, string? brandCode = null, bool? activeOnly = null)
    {
        var query = db.ProductGroups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parentCode))
        {
            if (parentCode.Equals("ROOT", StringComparison.OrdinalIgnoreCase))
                query = query.Where(g => string.IsNullOrEmpty(g.ParentCode));
            else
                query = query.Where(g => g.ParentCode != null && g.ParentCode.ToLower() == parentCode.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(brandCode))
            query = query.Where(g => g.BrandCode != null && g.BrandCode.ToLower() == brandCode.Trim().ToLower());

        if (activeOnly.HasValue)
            query = query.Where(g => g.IsActive == activeOnly.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(g => g.Code.ToLower().Contains(kw) ||
                                     g.Name.ToLower().Contains(kw) ||
                                     (g.Description != null && g.Description.ToLower().Contains(kw)));
        }

        var allGroups = await db.ProductGroups.ToListAsync();
        var groups = await query.ToListAsync();
        var products = await db.Products.ToListAsync();
        var brands = await db.Brands.ToListAsync();
        var balances = await BalancesAsync(null);

        var groupDict = allGroups.ToDictionary(g => g.Code.ToUpperInvariant(), g => g.Name);
        var brandDict = brands.ToDictionary(b => b.Code.ToUpperInvariant(), b => b.Name);

        var rows = groups.Select(g =>
        {
            var codeUpper = g.Code.ToUpperInvariant();
            var parentUpper = g.ParentCode?.ToUpperInvariant();
            var brandUpper = g.BrandCode?.ToUpperInvariant();

            int level = string.IsNullOrWhiteSpace(g.ParentCode) ? 1 : 2;
            string? parentName = (parentUpper != null && groupDict.TryGetValue(parentUpper, out var pName)) ? pName : null;
            string? brandName = (brandUpper != null && brandDict.TryGetValue(brandUpper, out var bName)) ? bName : null;

            var assignedProducts = products.Where(p => !string.IsNullOrEmpty(p.ProductGrpCode) && p.ProductGrpCode.Equals(g.Code, StringComparison.OrdinalIgnoreCase)).ToList();
            int productCount = assignedProducts.Count;

            var assignedProductIds = assignedProducts.Select(p => p.Id).ToHashSet();
            int totalStockQty = balances.Where(bal => assignedProductIds.Contains(bal.ProductId)).Sum(bal => bal.Qty);

            return new ProductGroupRow(
                g.Id,
                g.Code,
                g.Name,
                g.Description,
                g.ParentCode,
                parentName,
                g.BrandCode,
                brandName,
                g.IsActive,
                g.CreatedAt,
                level,
                productCount,
                totalStockQty
            );
        }).OrderBy(r => string.IsNullOrWhiteSpace(r.ParentCode) ? r.Code : r.ParentCode)
          .ThenBy(r => r.Level)
          .ThenBy(r => r.Code)
          .ToList();

        int totalGroups = allGroups.Count;
        int rootGroupsCount = allGroups.Count(g => string.IsNullOrWhiteSpace(g.ParentCode));
        int subGroupsCount = totalGroups - rootGroupsCount;

        var allAssignedProducts = products.Where(p => !string.IsNullOrEmpty(p.ProductGrpCode)).ToList();
        int totalProductsAssigned = allAssignedProducts.Count;
        var allAssignedProductIds = allAssignedProducts.Select(p => p.Id).ToHashSet();
        int totalStockAssigned = balances.Where(bal => allAssignedProductIds.Contains(bal.ProductId)).Sum(bal => bal.Qty);

        return new ProductGroupReport(
            q,
            parentCode,
            brandCode,
            activeOnly,
            totalGroups,
            rootGroupsCount,
            subGroupsCount,
            totalProductsAssigned,
            totalStockAssigned,
            rows
        );
    }

    public Task<List<ProductGroup>> ProductGroupsAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.ProductGroups.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(g => g.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(g => g.Code.ToLower().Contains(kw) || g.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(g => string.IsNullOrWhiteSpace(g.ParentCode) ? g.Code : g.ParentCode)
                    .ThenBy(g => g.Code)
                    .ToListAsync();
    }

    public Task<ProductGroup?> GetProductGroupAsync(int id) =>
        db.ProductGroups.FirstOrDefaultAsync(g => g.Id == id);

    public Task<ProductGroup?> GetProductGroupByCodeAsync(string code) =>
        db.ProductGroups.FirstOrDefaultAsync(g => g.Code.ToLower() == code.Trim().ToLower());

    public async Task<ProductGroupDetailDto?> GetProductGroupDetailAsync(int id)
    {
        var item = await db.ProductGroups.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return null;

        var parent = !string.IsNullOrWhiteSpace(item.ParentCode)
            ? await db.ProductGroups.FirstOrDefaultAsync(g => g.Code.ToLower() == item.ParentCode.Trim().ToLower())
            : null;

        var subGroups = await db.ProductGroups
            .Where(g => g.ParentCode != null && g.ParentCode.ToLower() == item.Code.Trim().ToLower())
            .OrderBy(g => g.Code)
            .ToListAsync();

        var products = await db.Products
            .Where(p => p.ProductGrpCode != null && p.ProductGrpCode.ToLower() == item.Code.Trim().ToLower())
            .OrderBy(p => p.Code)
            .ToListAsync();

        var balances = await BalancesAsync(null);
        var pIds = products.Select(p => p.Id).ToHashSet();
        int totalStock = balances.Where(bal => pIds.Contains(bal.ProductId)).Sum(bal => bal.Qty);

        return new ProductGroupDetailDto(item, parent, subGroups, products, products.Count, totalStock);
    }

    public async Task<int> CreateProductGroupAsync(ProductGroup item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Tên nhóm hàng không được để trống.");

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"GRP_{await db.ProductGroups.CountAsync() + 1:D2}";
        }
        else
        {
            item.Code = item.Code.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(item.ParentCode))
        {
            item.ParentCode = item.ParentCode.Trim().ToUpperInvariant();
            if (item.ParentCode.Equals(item.Code, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Nhóm hàng cha không thể là chính nhóm hàng này.");
        }

        if (!string.IsNullOrWhiteSpace(item.BrandCode))
        {
            item.BrandCode = item.BrandCode.Trim().ToUpperInvariant();
        }

        bool exists = await db.ProductGroups.AnyAsync(g => g.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã nhóm hàng '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.ProductGroups.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateProductGroupAsync(int id, ProductGroup item)
    {
        var existing = await db.ProductGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (existing == null) return (false, "Không tìm thấy nhóm hàng.");

        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "Tên nhóm hàng không được để trống.");

        if (!string.IsNullOrWhiteSpace(item.ParentCode))
        {
            var pCode = item.ParentCode.Trim().ToUpperInvariant();
            if (pCode.Equals(existing.Code, StringComparison.OrdinalIgnoreCase))
                return (false, "Nhóm hàng cha không thể là chính nhóm hàng này.");
            existing.ParentCode = pCode;
        }
        else
        {
            existing.ParentCode = null;
        }

        existing.Name = item.Name.Trim();
        existing.Description = item.Description?.Trim();
        existing.BrandCode = string.IsNullOrWhiteSpace(item.BrandCode) ? null : item.BrandCode.Trim().ToUpperInvariant();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin nhóm hàng '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> ToggleProductGroupStatusAsync(int id)
    {
        var existing = await db.ProductGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (existing == null) return (false, "Không tìm thấy nhóm hàng.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng nhóm hàng '{existing.Code}'." : $"Đã chuyển nhóm hàng '{existing.Code}' sang trạng thái Tạm dừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> DeleteProductGroupAsync(int id)
    {
        var existing = await db.ProductGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (existing == null) return (false, "Không tìm thấy nhóm hàng.");

        bool hasProducts = await db.Products.AnyAsync(p => p.ProductGrpCode != null && p.ProductGrpCode.ToUpper() == existing.Code.ToUpper());
        bool hasSubGroups = await db.ProductGroups.AnyAsync(g => g.ParentCode != null && g.ParentCode.ToUpper() == existing.Code.ToUpper());

        if (hasProducts || hasSubGroups)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            string reason = hasProducts && hasSubGroups ? "đã có mặt hàng trực thuộc và nhóm hàng con" : hasProducts ? "đã có mặt hàng trực thuộc" : "đã có nhóm hàng con trực thuộc";
            return (true, $"Nhóm hàng '{existing.Code}' {reason} nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.ProductGroups.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa nhóm hàng '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh mục Vùng & Khu vực thị trường kèm 4 thẻ KPI (port từ Mst_Area Skycic).</summary>
    public async Task<AreaReport> AreasReportAsync(string? q = null, string? parentCode = null, bool? activeOnly = null)
    {
        var query = db.Areas.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parentCode))
        {
            if (parentCode.Equals("ROOT", StringComparison.OrdinalIgnoreCase))
                query = query.Where(a => string.IsNullOrEmpty(a.ParentCode));
            else
                query = query.Where(a => a.ParentCode != null && a.ParentCode.ToLower() == parentCode.Trim().ToLower());
        }

        if (activeOnly.HasValue)
            query = query.Where(a => a.IsActive == activeOnly.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(a => a.Code.ToLower().Contains(kw) ||
                                     a.Name.ToLower().Contains(kw) ||
                                     (a.Description != null && a.Description.ToLower().Contains(kw)));
        }

        var allAreas = await db.Areas.ToListAsync();
        var areas = await query.ToListAsync();
        var warehouses = await db.Warehouses.ToListAsync();
        var customers = await db.Customers.ToListAsync();
        var balances = await BalancesAsync(null);

        var areaDict = allAreas.ToDictionary(a => a.Code.ToUpperInvariant(), a => a.Name);

        var rows = areas.Select(a =>
        {
            var codeUpper = a.Code.ToUpperInvariant();
            var parentUpper = a.ParentCode?.ToUpperInvariant();

            int level = string.IsNullOrWhiteSpace(a.ParentCode) ? 1 : 2;
            string? parentName = (parentUpper != null && areaDict.TryGetValue(parentUpper, out var pName)) ? pName : null;

            var assignedWarehouses = warehouses.Where(w => !string.IsNullOrEmpty(w.AreaCode) && w.AreaCode.Equals(a.Code, StringComparison.OrdinalIgnoreCase)).ToList();
            int warehouseCount = assignedWarehouses.Count;

            var childAreaCodes = allAreas.Where(sub => sub.ParentCode != null && sub.ParentCode.Equals(a.Code, StringComparison.OrdinalIgnoreCase)).Select(sub => sub.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var allRelevantWarehouses = warehouses.Where(w => !string.IsNullOrEmpty(w.AreaCode) && (w.AreaCode.Equals(a.Code, StringComparison.OrdinalIgnoreCase) || childAreaCodes.Contains(w.AreaCode))).ToList();
            var allWhIds = allRelevantWarehouses.Select(w => w.Id).ToHashSet();
            int totalStockQty = balances.Where(bal => allWhIds.Contains(bal.WarehouseId)).Sum(bal => bal.Qty);

            var assignedCustomers = customers.Where(c => !string.IsNullOrEmpty(c.AreaCode) && (c.AreaCode.Equals(a.Code, StringComparison.OrdinalIgnoreCase) || childAreaCodes.Contains(c.AreaCode))).ToList();
            int customerCount = assignedCustomers.Count;

            return new AreaRow(
                a.Id,
                a.Code,
                a.Name,
                a.Description,
                a.ParentCode,
                parentName,
                a.IsActive,
                a.CreatedAt,
                level,
                warehouseCount,
                customerCount,
                totalStockQty
            );
        }).OrderBy(r => string.IsNullOrWhiteSpace(r.ParentCode) ? r.Code : r.ParentCode)
          .ThenBy(r => r.Level)
          .ThenBy(r => r.Code)
          .ToList();

        int totalAreas = allAreas.Count;
        int rootAreasCount = allAreas.Count(a => string.IsNullOrWhiteSpace(a.ParentCode));
        int subAreasCount = totalAreas - rootAreasCount;

        var allAssignedWarehouses = warehouses.Where(w => !string.IsNullOrEmpty(w.AreaCode)).ToList();
        int totalWarehousesAssigned = allAssignedWarehouses.Count;

        var allAssignedCustomers = customers.Where(c => !string.IsNullOrEmpty(c.AreaCode)).ToList();
        int totalCustomersAssigned = allAssignedCustomers.Count;

        var assignedWhIds = allAssignedWarehouses.Select(w => w.Id).ToHashSet();
        int grandTotalStock = balances.Where(bal => assignedWhIds.Contains(bal.WarehouseId)).Sum(bal => bal.Qty);

        return new AreaReport(
            q,
            parentCode,
            activeOnly,
            totalAreas,
            rootAreasCount,
            subAreasCount,
            totalWarehousesAssigned,
            totalCustomersAssigned,
            grandTotalStock,
            rows
        );
    }

    public Task<List<Area>> AreasAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.Areas.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(a => a.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(a => a.Code.ToLower().Contains(kw) || a.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(a => string.IsNullOrWhiteSpace(a.ParentCode) ? a.Code : a.ParentCode)
                    .ThenBy(a => a.Code)
                    .ToListAsync();
    }

    public Task<Area?> GetAreaAsync(int id) =>
        db.Areas.FirstOrDefaultAsync(a => a.Id == id);

    public Task<Area?> GetAreaByCodeAsync(string code) =>
        db.Areas.FirstOrDefaultAsync(a => a.Code.ToLower() == code.Trim().ToLower());

    public async Task<AreaDetailDto?> GetAreaDetailAsync(int id)
    {
        var item = await db.Areas.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return null;

        var parent = !string.IsNullOrWhiteSpace(item.ParentCode)
            ? await db.Areas.FirstOrDefaultAsync(a => a.Code.ToLower() == item.ParentCode.Trim().ToLower())
            : null;

        var subAreas = await db.Areas
            .Where(a => a.ParentCode != null && a.ParentCode.ToLower() == item.Code.Trim().ToLower())
            .OrderBy(a => a.Code)
            .ToListAsync();

        var childAreaCodes = subAreas.Select(s => s.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var warehouses = await db.Warehouses
            .Where(w => !string.IsNullOrEmpty(w.AreaCode) && (w.AreaCode.ToLower() == item.Code.Trim().ToLower() || childAreaCodes.Contains(w.AreaCode)))
            .OrderBy(w => w.Code)
            .ToListAsync();

        var customers = await db.Customers
            .Where(c => !string.IsNullOrEmpty(c.AreaCode) && (c.AreaCode.ToLower() == item.Code.Trim().ToLower() || childAreaCodes.Contains(c.AreaCode)))
            .OrderBy(c => c.Code)
            .ToListAsync();

        var balances = await BalancesAsync(null);
        var whIds = warehouses.Select(w => w.Id).ToHashSet();
        int totalStock = balances.Where(bal => whIds.Contains(bal.WarehouseId)).Sum(bal => bal.Qty);

        return new AreaDetailDto(item, parent, subAreas, warehouses, customers, warehouses.Count, customers.Count, totalStock);
    }

    public async Task<int> CreateAreaAsync(Area item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Tên vùng / khu vực không được để trống.");

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"AREA_{await db.Areas.CountAsync() + 1:D2}";
        }
        else
        {
            item.Code = item.Code.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(item.ParentCode))
        {
            item.ParentCode = item.ParentCode.Trim().ToUpperInvariant();
            if (item.ParentCode.Equals(item.Code, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Vùng cha không thể là chính khu vực này.");
        }

        bool exists = await db.Areas.AnyAsync(a => a.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã khu vực '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.Areas.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateAreaAsync(int id, Area item)
    {
        var existing = await db.Areas.FirstOrDefaultAsync(a => a.Id == id);
        if (existing == null) return (false, "Không tìm thấy vùng / khu vực.");

        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "Tên vùng / khu vực không được để trống.");

        if (!string.IsNullOrWhiteSpace(item.ParentCode))
        {
            var pCode = item.ParentCode.Trim().ToUpperInvariant();
            if (pCode.Equals(existing.Code, StringComparison.OrdinalIgnoreCase))
                return (false, "Vùng cha không thể là chính khu vực này.");
            existing.ParentCode = pCode;
        }
        else
        {
            existing.ParentCode = null;
        }

        existing.Name = item.Name.Trim();
        existing.Description = item.Description?.Trim();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin khu vực '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> ToggleAreaStatusAsync(int id)
    {
        var existing = await db.Areas.FirstOrDefaultAsync(a => a.Id == id);
        if (existing == null) return (false, "Không tìm thấy vùng / khu vực.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng khu vực '{existing.Code}'." : $"Đã chuyển khu vực '{existing.Code}' sang trạng thái Tạm dừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> DeleteAreaAsync(int id)
    {
        var existing = await db.Areas.FirstOrDefaultAsync(a => a.Id == id);
        if (existing == null) return (false, "Không tìm thấy vùng / khu vực.");

        bool hasWarehouses = await db.Warehouses.AnyAsync(w => w.AreaCode != null && w.AreaCode.ToUpper() == existing.Code.ToUpper());
        bool hasCustomers = await db.Customers.AnyAsync(c => c.AreaCode != null && c.AreaCode.ToUpper() == existing.Code.ToUpper());
        bool hasSubAreas = await db.Areas.AnyAsync(a => a.ParentCode != null && a.ParentCode.ToUpper() == existing.Code.ToUpper());

        if (hasWarehouses || hasCustomers || hasSubAreas)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            var reasons = new List<string>();
            if (hasWarehouses) reasons.Add("kho hàng trực thuộc");
            if (hasCustomers) reasons.Add("khách hàng/đại lý");
            if (hasSubAreas) reasons.Add("khu vực nhánh trực thuộc");
            return (true, $"Khu vực '{existing.Code}' đang có {string.Join(", ", reasons)} nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.Areas.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa khu vực '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh mục Nhóm khách hàng & Đại lý phân phối kèm 4 thẻ KPI (port từ Mst_CustomerGroup Skycic).</summary>
    public async Task<CustomerGroupReport> CustomerGroupsReportAsync(string? q = null, string? parentCode = null, bool? activeOnly = null)
    {
        var query = db.CustomerGroups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parentCode))
        {
            if (parentCode.Equals("ROOT", StringComparison.OrdinalIgnoreCase))
                query = query.Where(g => string.IsNullOrEmpty(g.ParentCode));
            else
                query = query.Where(g => g.ParentCode != null && g.ParentCode.ToLower() == parentCode.Trim().ToLower());
        }

        if (activeOnly.HasValue)
            query = query.Where(g => g.IsActive == activeOnly.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(g => g.Code.ToLower().Contains(kw) ||
                                     g.Name.ToLower().Contains(kw) ||
                                     (g.Description != null && g.Description.ToLower().Contains(kw)));
        }

        var allGroups = await db.CustomerGroups.ToListAsync();
        var groups = await query.ToListAsync();
        var allCustomers = await db.Customers.ToListAsync();
        var allOutDocs = await db.Docs.Where(d => d.Type == DocType.Out).Include(d => d.Lines).ToListAsync();

        var groupDict = allGroups.ToDictionary(g => g.Code.ToUpperInvariant(), g => g.Name);

        var rows = groups.Select(g =>
        {
            var codeUpper = g.Code.ToUpperInvariant();
            var parentUpper = g.ParentCode?.ToUpperInvariant();

            int level = string.IsNullOrWhiteSpace(g.ParentCode) ? 1 : 2;
            string? parentName = (parentUpper != null && groupDict.TryGetValue(parentUpper, out var pName)) ? pName : null;

            // Lấy mã tất cả phân nhóm con (nếu có)
            var childGroupCodes = allGroups.Where(sub => sub.ParentCode != null && sub.ParentCode.Equals(g.Code, StringComparison.OrdinalIgnoreCase)).Select(sub => sub.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Các khách hàng thuộc nhóm này hoặc các phân nhóm con của nó
            var relevantCusts = allCustomers.Where(c => !string.IsNullOrEmpty(c.CustomerGrpCode) && (c.CustomerGrpCode.Equals(g.Code, StringComparison.OrdinalIgnoreCase) || childGroupCodes.Contains(c.CustomerGrpCode))).ToList();
            int custCount = relevantCusts.Count;

            var custCodes = relevantCusts.Select(c => c.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var custNames = relevantCusts.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Tổng lượng xuất kho phân phối giao cho các khách hàng thuộc nhóm
            int totalDispatched = allOutDocs
                .Where(d => (!string.IsNullOrEmpty(d.CustomerCode) && custCodes.Contains(d.CustomerCode)) ||
                            (!string.IsNullOrEmpty(d.CustomerName) && custNames.Contains(d.CustomerName)))
                .Sum(d => d.Lines.Sum(l => l.Quantity));

            return new CustomerGroupRow(
                g.Id,
                g.Code,
                g.Name,
                g.Description,
                g.ParentCode,
                parentName,
                g.IsActive,
                g.CreatedAt,
                level,
                custCount,
                totalDispatched
            );
        }).OrderBy(r => string.IsNullOrWhiteSpace(r.ParentCode) ? r.Code : r.ParentCode)
          .ThenBy(r => r.Level)
          .ThenBy(r => r.Code)
          .ToList();

        int totalGroups = allGroups.Count;
        int rootGroupsCount = allGroups.Count(g => string.IsNullOrWhiteSpace(g.ParentCode));
        int subGroupsCount = totalGroups - rootGroupsCount;

        var allAssignedCusts = allCustomers.Where(c => !string.IsNullOrEmpty(c.CustomerGrpCode)).ToList();
        int totalCustomersAssigned = allAssignedCusts.Count;

        var allAssignedCustCodes = allAssignedCusts.Select(c => c.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allAssignedCustNames = allAssignedCusts.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        int grandTotalDispatched = allOutDocs
            .Where(d => (!string.IsNullOrEmpty(d.CustomerCode) && allAssignedCustCodes.Contains(d.CustomerCode)) ||
                        (!string.IsNullOrEmpty(d.CustomerName) && allAssignedCustNames.Contains(d.CustomerName)))
            .Sum(d => d.Lines.Sum(l => l.Quantity));

        return new CustomerGroupReport(
            q,
            parentCode,
            activeOnly,
            totalGroups,
            rootGroupsCount,
            subGroupsCount,
            totalCustomersAssigned,
            grandTotalDispatched,
            rows
        );
    }

    public Task<List<CustomerGroup>> CustomerGroupsAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.CustomerGroups.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(g => g.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(g => g.Code.ToLower().Contains(kw) || g.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(g => string.IsNullOrWhiteSpace(g.ParentCode) ? g.Code : g.ParentCode)
                    .ThenBy(g => g.Code)
                    .ToListAsync();
    }

    public Task<CustomerGroup?> GetCustomerGroupAsync(int id) =>
        db.CustomerGroups.FirstOrDefaultAsync(g => g.Id == id);

    public Task<CustomerGroup?> GetCustomerGroupByCodeAsync(string code) =>
        db.CustomerGroups.FirstOrDefaultAsync(g => g.Code.ToLower() == code.Trim().ToLower());

    public async Task<CustomerGroupDetailDto?> GetCustomerGroupDetailAsync(int id)
    {
        var item = await db.CustomerGroups.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return null;

        var parent = !string.IsNullOrWhiteSpace(item.ParentCode)
            ? await db.CustomerGroups.FirstOrDefaultAsync(g => g.Code.ToLower() == item.ParentCode.Trim().ToLower())
            : null;

        var subGroups = await db.CustomerGroups
            .Where(g => g.ParentCode != null && g.ParentCode.ToLower() == item.Code.Trim().ToLower())
            .OrderBy(g => g.Code)
            .ToListAsync();

        var childCodes = subGroups.Select(s => s.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var customers = await db.Customers
            .Where(c => !string.IsNullOrEmpty(c.CustomerGrpCode) && (c.CustomerGrpCode.ToLower() == item.Code.Trim().ToLower() || childCodes.Contains(c.CustomerGrpCode)))
            .OrderBy(c => c.Code)
            .ToListAsync();

        var custCodes = customers.Select(c => c.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var custNames = customers.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var outDocs = await db.Docs
            .Where(d => d.Type == DocType.Out && ((!string.IsNullOrEmpty(d.CustomerCode) && custCodes.Contains(d.CustomerCode)) || (!string.IsNullOrEmpty(d.CustomerName) && custNames.Contains(d.CustomerName))))
            .Include(d => d.FromWarehouse)
            .Include(d => d.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(d => d.Date)
            .ToListAsync();

        int totalDispatched = outDocs.Sum(d => d.Lines.Sum(l => l.Quantity));
        var recentDocs = outDocs.Take(5).ToList();

        return new CustomerGroupDetailDto(item, parent, subGroups, customers, customers.Count, totalDispatched, recentDocs);
    }

    public async Task<int> CreateCustomerGroupAsync(CustomerGroup item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            throw new ArgumentException("Tên nhóm khách hàng không được để trống.");

        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"GRP_{await db.CustomerGroups.CountAsync() + 1:D2}";
        }
        else
        {
            item.Code = item.Code.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(item.ParentCode))
        {
            item.ParentCode = item.ParentCode.Trim().ToUpperInvariant();
            if (item.ParentCode.Equals(item.Code, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Nhóm cha không thể là chính nhóm này.");
        }

        bool exists = await db.CustomerGroups.AnyAsync(g => g.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã nhóm khách hàng '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.CustomerGroups.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateCustomerGroupAsync(int id, CustomerGroup item)
    {
        var existing = await db.CustomerGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (existing == null) return (false, "Không tìm thấy nhóm khách hàng.");

        if (string.IsNullOrWhiteSpace(item.Name))
            return (false, "Tên nhóm khách hàng không được để trống.");

        if (!string.IsNullOrWhiteSpace(item.ParentCode))
        {
            var pCode = item.ParentCode.Trim().ToUpperInvariant();
            if (pCode.Equals(existing.Code, StringComparison.OrdinalIgnoreCase))
                return (false, "Nhóm cha không thể là chính nhóm này.");
            existing.ParentCode = pCode;
        }
        else
        {
            existing.ParentCode = null;
        }

        existing.Name = item.Name.Trim();
        existing.Description = item.Description?.Trim();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin nhóm khách hàng '{existing.Code}'.");
    }

    public async Task<(bool ok, string msg)> ToggleCustomerGroupStatusAsync(int id)
    {
        var existing = await db.CustomerGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (existing == null) return (false, "Không tìm thấy nhóm khách hàng.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng nhóm khách hàng '{existing.Code}'." : $"Đã chuyển nhóm '{existing.Code}' sang trạng thái Tạm dừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> DeleteCustomerGroupAsync(int id)
    {
        var existing = await db.CustomerGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (existing == null) return (false, "Không tìm thấy nhóm khách hàng.");

        bool hasCustomers = await db.Customers.AnyAsync(c => c.CustomerGrpCode != null && c.CustomerGrpCode.ToUpper() == existing.Code.ToUpper());
        bool hasSubGroups = await db.CustomerGroups.AnyAsync(g => g.ParentCode != null && g.ParentCode.ToUpper() == existing.Code.ToUpper());

        if (hasCustomers || hasSubGroups)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            var reasons = new List<string>();
            if (hasCustomers) reasons.Add("khách hàng/đại lý trực thuộc");
            if (hasSubGroups) reasons.Add("phân nhóm nhánh con");
            return (true, $"Nhóm khách hàng '{existing.Code}' đang có {string.Join(", ", reasons)} nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.CustomerGroups.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa nhóm khách hàng '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh mục Bộ phận / Phòng ban kèm 4 thẻ KPI (port từ Mst_Department & Mst_DepartmentExt Skycic).</summary>
    public async Task<DepartmentReport> DepartmentsReportAsync(string? q = null, string? parentCode = null, int? level = null, bool? activeOnly = null)
    {
        var query = db.Departments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(d => d.Code.ToLower().Contains(kw) ||
                                     d.Name.ToLower().Contains(kw) ||
                                     (d.BUCode != null && d.BUCode.ToLower().Contains(kw)) ||
                                     (d.MST != null && d.MST.ToLower().Contains(kw)) ||
                                     (d.Description != null && d.Description.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(parentCode))
        {
            if (parentCode.Equals("ROOT", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => string.IsNullOrWhiteSpace(d.ParentCode) || d.Level == 1);
            }
            else
            {
                query = query.Where(d => d.ParentCode != null && d.ParentCode.ToLower() == parentCode.Trim().ToLower());
            }
        }

        if (level.HasValue && level.Value > 0)
        {
            query = query.Where(d => d.Level == level.Value);
        }

        if (activeOnly.HasValue)
        {
            query = query.Where(d => d.IsActive == activeOnly.Value);
        }

        var allDepts = await db.Departments.ToListAsync();
        var depts = await query.ToListAsync();

        var users = await db.UserMapInventories.ToListAsync();
        var outDocs = await db.Docs
            .Where(d => d.DepartmentCode != null && (d.Type == DocType.Out || d.Status == DocStatus.Posted))
            .Include(d => d.Lines)
            .ToListAsync();

        var rows = depts
            .OrderBy(d => d.Level)
            .ThenBy(d => d.ParentCode ?? "")
            .ThenBy(d => d.Code)
            .Select(d =>
            {
                var parent = !string.IsNullOrWhiteSpace(d.ParentCode)
                    ? allDepts.FirstOrDefault(p => p.Code.Equals(d.ParentCode, StringComparison.OrdinalIgnoreCase))
                    : null;

                var subCount = allDepts.Count(c => c.ParentCode != null && c.ParentCode.Equals(d.Code, StringComparison.OrdinalIgnoreCase));
                var assignedUsersCount = users.Count(u => u.DepartmentCode != null && u.DepartmentCode.Equals(d.Code, StringComparison.OrdinalIgnoreCase));

                var deptDocs = outDocs.Where(doc => doc.DepartmentCode != null && doc.DepartmentCode.Equals(d.Code, StringComparison.OrdinalIgnoreCase)).ToList();
                var dispatchedDocsCount = deptDocs.Count;
                var totalDispatchedQty = deptDocs.Sum(doc => doc.Lines.Sum(l => l.Quantity));

                var levelName = d.Level switch
                {
                    1 => "Khối / Ban điều hành",
                    2 => "Phòng ban chức năng",
                    3 => "Phân xưởng / Tổ / Đội",
                    _ => $"Cấp {d.Level}"
                };

                return new DepartmentRow(
                    d.Id,
                    d.Code,
                    d.Name,
                    d.ParentCode,
                    parent?.Name,
                    d.BUCode,
                    d.Level,
                    levelName,
                    d.MST,
                    d.Description,
                    d.IsActive,
                    d.CreatedAt,
                    subCount,
                    assignedUsersCount,
                    dispatchedDocsCount,
                    totalDispatchedQty
                );
            }).ToList();

        int totalDepartments = allDepts.Count;
        int rootBlocksCount = allDepts.Count(d => d.Level == 1 || string.IsNullOrWhiteSpace(d.ParentCode));
        int subDepartmentsCount = allDepts.Count(d => d.Level > 1);
        int totalAssignedUsers = users.Count(u => !string.IsNullOrWhiteSpace(u.DepartmentCode));
        int totalAllDispatchedQty = outDocs.Sum(doc => doc.Lines.Sum(l => l.Quantity));

        return new DepartmentReport(
            q,
            parentCode,
            level,
            activeOnly,
            totalDepartments,
            rootBlocksCount,
            subDepartmentsCount,
            totalAssignedUsers,
            totalAllDispatchedQty,
            rows
        );
    }

    public Task<List<Department>> DepartmentsAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.Departments.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(d => d.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(d => d.Code.ToLower().Contains(kw) || d.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(d => d.Level).ThenBy(d => d.Code).ToListAsync();
    }

    public Task<Department?> GetDepartmentAsync(int id) =>
        db.Departments.FirstOrDefaultAsync(d => d.Id == id);

    public Task<Department?> GetDepartmentByCodeAsync(string code) =>
        db.Departments.FirstOrDefaultAsync(d => d.Code.ToLower() == code.Trim().ToLower());

    public async Task<DepartmentDetailDto?> GetDepartmentDetailAsync(int id)
    {
        var item = await db.Departments.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return null;

        var parent = !string.IsNullOrWhiteSpace(item.ParentCode)
            ? await db.Departments.FirstOrDefaultAsync(d => d.Code.ToLower() == item.ParentCode.Trim().ToLower())
            : null;

        var subDepartments = await db.Departments
            .Where(d => d.ParentCode != null && d.ParentCode.ToLower() == item.Code.Trim().ToLower())
            .OrderBy(d => d.Code)
            .ToListAsync();

        var assignedUsers = await db.UserMapInventories
            .Where(u => u.DepartmentCode != null && u.DepartmentCode.ToLower() == item.Code.Trim().ToLower())
            .Include(u => u.Warehouse)
            .ToListAsync();

        var recentDocs = await db.Docs
            .Where(d => d.DepartmentCode != null && d.DepartmentCode.ToLower() == item.Code.Trim().ToLower())
            .Include(d => d.Lines)
            .Include(d => d.FromWarehouse)
            .OrderByDescending(d => d.Date)
            .Take(10)
            .ToListAsync();

        int totalDispatched = recentDocs.Sum(d => d.Lines.Sum(l => l.Quantity));

        return new DepartmentDetailDto(item, parent, subDepartments, assignedUsers, subDepartments.Count, assignedUsers.Count, totalDispatched, recentDocs);
    }

    public async Task<int> CreateDepartmentAsync(Department item)
    {
        item.Code = item.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"PB_{await db.Departments.CountAsync() + 1:D2}";
        }
        item.Name = item.Name.Trim();
        item.ParentCode = string.IsNullOrWhiteSpace(item.ParentCode) ? null : item.ParentCode.Trim().ToUpperInvariant();
        item.BUCode = string.IsNullOrWhiteSpace(item.BUCode) ? null : item.BUCode.Trim().ToUpperInvariant();
        item.MST = string.IsNullOrWhiteSpace(item.MST) ? null : item.MST.Trim();
        item.Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();

        if (!string.IsNullOrWhiteSpace(item.ParentCode))
        {
            var parent = await db.Departments.FirstOrDefaultAsync(p => p.Code == item.ParentCode);
            if (parent != null && item.Level <= parent.Level)
            {
                item.Level = parent.Level + 1;
            }
        }
        if (item.Level < 1) item.Level = 1;

        bool exists = await db.Departments.AnyAsync(d => d.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã bộ phận '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.Departments.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateDepartmentAsync(int id, Department item)
    {
        var existing = await db.Departments.FirstOrDefaultAsync(d => d.Id == id);
        if (existing == null) return (false, "Không tìm thấy bộ phận / phòng ban.");

        existing.Name = item.Name.Trim();
        var newParent = string.IsNullOrWhiteSpace(item.ParentCode) ? null : item.ParentCode.Trim().ToUpperInvariant();
        if (newParent != null && newParent.Equals(existing.Code, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Không thể chọn chính bộ phận này làm bộ phận cấp trên.");
        }
        existing.ParentCode = newParent;
        existing.BUCode = string.IsNullOrWhiteSpace(item.BUCode) ? null : item.BUCode.Trim().ToUpperInvariant();
        existing.MST = string.IsNullOrWhiteSpace(item.MST) ? null : item.MST.Trim();
        existing.Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();
        existing.Level = item.Level >= 1 ? item.Level : 1;
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin bộ phận '{existing.Code}' thành công.");
    }

    public async Task<(bool ok, string msg)> ToggleDepartmentStatusAsync(int id)
    {
        var existing = await db.Departments.FirstOrDefaultAsync(d => d.Id == id);
        if (existing == null) return (false, "Không tìm thấy bộ phận / phòng ban.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt áp dụng bộ phận '{existing.Code}'." : $"Đã chuyển bộ phận '{existing.Code}' sang trạng thái Tạm dừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> DeleteDepartmentAsync(int id)
    {
        var existing = await db.Departments.FirstOrDefaultAsync(d => d.Id == id);
        if (existing == null) return (false, "Không tìm thấy bộ phận / phòng ban.");

        bool hasSubDepts = await db.Departments.AnyAsync(d => d.ParentCode != null && d.ParentCode.ToUpper() == existing.Code.ToUpper());
        bool hasAssignedUsers = await db.UserMapInventories.AnyAsync(u => u.DepartmentCode != null && u.DepartmentCode.ToUpper() == existing.Code.ToUpper());
        bool hasDocs = await db.Docs.AnyAsync(doc => doc.DepartmentCode != null && doc.DepartmentCode.ToUpper() == existing.Code.ToUpper());

        if (hasSubDepts || hasAssignedUsers || hasDocs)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            var reasons = new List<string>();
            if (hasSubDepts) reasons.Add("bộ phận cấp dưới trực thuộc");
            if (hasAssignedUsers) reasons.Add("nhân sự quản lý kho");
            if (hasDocs) reasons.Add("phiếu xuất cấp phát vật tư");
            return (true, $"Bộ phận '{existing.Code}' đang có {string.Join(", ", reasons)} nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.Departments.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa bộ phận '{existing.Code}'.");
    }

    /// <summary>Báo cáo danh mục Nguồn khách hàng & Kênh tiếp nhận đối tác kho kèm 4 thẻ KPI (port từ Mst_CustomerSource Skycic).</summary>
    public async Task<CustomerSourceReport> CustomerSourcesReportAsync(string? q = null, string? parentCode = null, bool? activeOnly = null)
    {
        var query = db.CustomerSources.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(s => s.Code.ToLower().Contains(kw) || s.Name.ToLower().Contains(kw) || (s.Description != null && s.Description.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(parentCode))
        {
            if (parentCode == "__ROOT__")
            {
                query = query.Where(s => string.IsNullOrEmpty(s.ParentCode));
            }
            else
            {
                query = query.Where(s => s.ParentCode != null && s.ParentCode.ToLower() == parentCode.Trim().ToLower());
            }
        }

        if (activeOnly.HasValue)
        {
            query = query.Where(s => s.IsActive == activeOnly.Value);
        }

        var allSources = await db.CustomerSources.ToListAsync();
        var sources = await query.ToListAsync();

        var customers = await db.Customers.ToListAsync();
        var outDocs = await db.Docs
            .Where(d => d.Type == DocType.Out && d.CustomerCode != null)
            .Include(d => d.Lines)
            .ThenInclude(l => l.Product)
            .ToListAsync();

        var rows = sources
            .OrderBy(s => string.IsNullOrEmpty(s.ParentCode) ? 0 : 1)
            .ThenBy(s => s.ParentCode ?? "")
            .ThenBy(s => s.Code)
            .Select(s =>
            {
                var parent = !string.IsNullOrWhiteSpace(s.ParentCode)
                    ? allSources.FirstOrDefault(p => p.Code.Equals(s.ParentCode, StringComparison.OrdinalIgnoreCase))
                    : null;

                var subCount = allSources.Count(c => c.ParentCode != null && c.ParentCode.Equals(s.Code, StringComparison.OrdinalIgnoreCase));
                var assignedCusts = customers.Where(c => c.CustomerSourceCode != null && c.CustomerSourceCode.Equals(s.Code, StringComparison.OrdinalIgnoreCase)).ToList();
                var custCount = assignedCusts.Count;
                var custCodes = assignedCusts.Select(c => c.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

                var channelDocs = outDocs.Where(d => d.CustomerCode != null && custCodes.Contains(d.CustomerCode)).ToList();
                var docsCount = channelDocs.Count;
                var totalQty = channelDocs.Sum(d => d.Lines.Sum(l => l.Quantity));
                var totalAmount = channelDocs.Sum(d => d.Lines.Sum(l => l.Quantity * (l.Product?.CostPrice > 0 ? l.Product.CostPrice : 150000m)));

                int level = string.IsNullOrWhiteSpace(s.ParentCode) ? 1 : 2;

                return new CustomerSourceRow(
                    s.Id,
                    s.Code,
                    s.Name,
                    s.Description,
                    s.ParentCode,
                    parent?.Name,
                    s.BUCode,
                    s.IsActive,
                    s.CreatedAt,
                    level,
                    custCount,
                    docsCount,
                    totalQty,
                    totalAmount
                );
            }).ToList();

        int totalSources = allSources.Count;
        int rootSourcesCount = allSources.Count(s => string.IsNullOrWhiteSpace(s.ParentCode));
        int subSourcesCount = allSources.Count(s => !string.IsNullOrWhiteSpace(s.ParentCode));
        int totalCustomersAssigned = customers.Count(c => !string.IsNullOrWhiteSpace(c.CustomerSourceCode));

        var topByVol = rows.OrderByDescending(r => r.TotalShippedQty).FirstOrDefault();
        string topSourceByVolume = topByVol != null && topByVol.TotalShippedQty > 0 ? $"{topByVol.Name} ({topByVol.Code})" : "Chưa có phát sinh xuất";
        int topVolumeQty = topByVol?.TotalShippedQty ?? 0;
        decimal totalAllShippedAmount = rows.Sum(r => r.TotalShippedAmount);

        return new CustomerSourceReport(
            q,
            parentCode,
            activeOnly,
            totalSources,
            rootSourcesCount,
            subSourcesCount,
            totalCustomersAssigned,
            topSourceByVolume,
            topVolumeQty,
            totalAllShippedAmount,
            rows
        );
    }

    public Task<List<CustomerSource>> CustomerSourcesAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.CustomerSources.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(s => s.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(s => s.Code.ToLower().Contains(kw) || s.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(s => string.IsNullOrEmpty(s.ParentCode) ? 0 : 1).ThenBy(s => s.Code).ToListAsync();
    }

    public Task<CustomerSource?> GetCustomerSourceAsync(int id) =>
        db.CustomerSources.FirstOrDefaultAsync(s => s.Id == id);

    public Task<CustomerSource?> GetCustomerSourceByCodeAsync(string code) =>
        db.CustomerSources.FirstOrDefaultAsync(s => s.Code.ToLower() == code.Trim().ToLower());

    public async Task<CustomerSourceDetailDto?> GetCustomerSourceDetailAsync(int id)
    {
        var item = await db.CustomerSources.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return null;

        var parent = !string.IsNullOrWhiteSpace(item.ParentCode)
            ? await db.CustomerSources.FirstOrDefaultAsync(s => s.Code.ToLower() == item.ParentCode.Trim().ToLower())
            : null;

        var subSources = await db.CustomerSources
            .Where(s => s.ParentCode != null && s.ParentCode.ToLower() == item.Code.Trim().ToLower())
            .OrderBy(s => s.Code)
            .ToListAsync();

        var customers = await db.Customers
            .Where(c => c.CustomerSourceCode != null && c.CustomerSourceCode.ToLower() == item.Code.Trim().ToLower())
            .OrderBy(c => c.Code)
            .ToListAsync();

        var custCodes = customers.Select(c => c.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var recentDocs = await db.Docs
            .Where(d => d.Type == DocType.Out && d.CustomerCode != null && custCodes.Contains(d.CustomerCode))
            .Include(d => d.Lines)
            .ThenInclude(l => l.Product)
            .Include(d => d.FromWarehouse)
            .OrderByDescending(d => d.Date)
            .Take(10)
            .ToListAsync();

        int totalShippedQty = recentDocs.Sum(d => d.Lines.Sum(l => l.Quantity));
        decimal totalShippedAmount = recentDocs.Sum(d => d.Lines.Sum(l => l.Quantity * (l.Product?.CostPrice > 0 ? l.Product.CostPrice : 150000m)));

        return new CustomerSourceDetailDto(item, parent, subSources, customers, customers.Count, recentDocs.Count, totalShippedQty, totalShippedAmount, recentDocs);
    }

    public async Task<int> CreateCustomerSourceAsync(CustomerSource item)
    {
        item.Code = item.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"SRC_{await db.CustomerSources.CountAsync() + 1:D2}";
        }
        item.Name = item.Name.Trim();
        item.ParentCode = string.IsNullOrWhiteSpace(item.ParentCode) ? null : item.ParentCode.Trim().ToUpperInvariant();
        item.BUCode = string.IsNullOrWhiteSpace(item.BUCode) ? null : item.BUCode.Trim().ToUpperInvariant();
        item.Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();

        bool exists = await db.CustomerSources.AnyAsync(s => s.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã nguồn khách hàng '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.CustomerSources.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateCustomerSourceAsync(int id, CustomerSource item)
    {
        var existing = await db.CustomerSources.FirstOrDefaultAsync(s => s.Id == id);
        if (existing == null) return (false, "Không tìm thấy nguồn khách hàng.");

        existing.Name = item.Name.Trim();
        var newParent = string.IsNullOrWhiteSpace(item.ParentCode) ? null : item.ParentCode.Trim().ToUpperInvariant();
        if (newParent != null && newParent.Equals(existing.Code, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Không thể chọn chính nguồn này làm nguồn cấp trên.");
        }
        existing.ParentCode = newParent;
        existing.BUCode = string.IsNullOrWhiteSpace(item.BUCode) ? null : item.BUCode.Trim().ToUpperInvariant();
        existing.Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật thông tin nguồn khách hàng '{existing.Code}' thành công.");
    }

    public async Task<(bool ok, string msg)> ToggleCustomerSourceStatusAsync(int id)
    {
        var existing = await db.CustomerSources.FirstOrDefaultAsync(s => s.Id == id);
        if (existing == null) return (false, "Không tìm thấy nguồn khách hàng.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt nguồn khách hàng '{existing.Code}'." : $"Đã chuyển nguồn khách hàng '{existing.Code}' sang trạng thái Tạm dừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> DeleteCustomerSourceAsync(int id)
    {
        var existing = await db.CustomerSources.FirstOrDefaultAsync(s => s.Id == id);
        if (existing == null) return (false, "Không tìm thấy nguồn khách hàng.");

        bool hasCustomers = await db.Customers.AnyAsync(c => c.CustomerSourceCode != null && c.CustomerSourceCode.ToUpper() == existing.Code.ToUpper());
        bool hasSubSources = await db.CustomerSources.AnyAsync(s => s.ParentCode != null && s.ParentCode.ToUpper() == existing.Code.ToUpper());

        if (hasCustomers || hasSubSources)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            var reasons = new List<string>();
            if (hasCustomers) reasons.Add("khách hàng đang liên kết");
            if (hasSubSources) reasons.Add("kênh nhánh trực thuộc");
            return (true, $"Nguồn khách hàng '{existing.Code}' đang có {string.Join(", ", reasons)} nên đã được chuyển sang trạng thái Ngừng áp dụng thay vì xóa hẳn.");
        }

        db.CustomerSources.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa nguồn khách hàng '{existing.Code}'.");
    }

    /// <summary>Báo cáo / Danh sách loại hình điều chuyển kho tổng hợp kèm 4 thẻ KPI (port từ Mst_MoveOrdType Skycic: MoveOrdType, MoveOrdTypeName, FlagActive, LogLUDTimeUTC, LogLUBy).</summary>
    public async Task<MoveOrdTypeReport> MoveOrdTypesReportAsync(string? q = null, bool? activeOnly = null, bool? urgentOnly = null)
    {
        var query = db.MoveOrdTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(t => t.IsActive == activeOnly.Value);
        if (urgentOnly.HasValue) query = query.Where(t => t.IsUrgent == urgentOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(t => t.Code.ToLower().Contains(kw) ||
                                     t.Name.ToLower().Contains(kw) ||
                                     (t.Description != null && t.Description.ToLower().Contains(kw)));
        }

        var types = await query.ToListAsync();
        var allMoveOrders = await db.MoveOrders
            .Include(m => m.Lines)
            .ToListAsync();

        var rows = types.Select(t =>
        {
            var matchedOrders = allMoveOrders
                .Where(m => !string.IsNullOrEmpty(m.MoveOrdTypeCode) && m.MoveOrdTypeCode.Equals(t.Code, StringComparison.OrdinalIgnoreCase))
                .ToList();
            int ordersCount = matchedOrders.Count;
            int qtyMoved = matchedOrders.Sum(m => m.Lines.Sum(l => l.Quantity));

            return new MoveOrdTypeRow(
                t.Id,
                t.Code,
                t.Name,
                t.Description,
                t.IsUrgent,
                t.IsActive,
                t.CreatedAt,
                ordersCount,
                qtyMoved
            );
        }).OrderBy(r => r.IsUrgent ? 0 : 1).ThenBy(r => r.Code).ToList();

        int totalTypes = await db.MoveOrdTypes.CountAsync();
        int activeCount = await db.MoveOrdTypes.CountAsync(t => t.IsActive);
        int urgentCount = await db.MoveOrdTypes.CountAsync(t => t.IsUrgent && t.IsActive);
        int inactiveCount = totalTypes - activeCount;

        int totalMoveOrdersCount = allMoveOrders.Count(m => !string.IsNullOrEmpty(m.MoveOrdTypeCode));
        int totalQtyMoved = allMoveOrders.Where(m => !string.IsNullOrEmpty(m.MoveOrdTypeCode)).Sum(m => m.Lines.Sum(l => l.Quantity));

        return new MoveOrdTypeReport(
            q,
            activeOnly,
            urgentOnly,
            totalTypes,
            activeCount,
            urgentCount,
            inactiveCount,
            totalMoveOrdersCount,
            totalQtyMoved,
            rows
        );
    }

    public Task<List<MoveOrdType>> MoveOrdTypesAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.MoveOrdTypes.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(t => t.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLowerInvariant();
            query = query.Where(t => t.Code.ToLower().Contains(kw) || t.Name.ToLower().Contains(kw));
        }
        return query.OrderBy(t => t.IsUrgent ? 0 : 1).ThenBy(t => t.Code).ToListAsync();
    }

    public Task<MoveOrdType?> GetMoveOrdTypeAsync(int id) =>
        db.MoveOrdTypes.FirstOrDefaultAsync(t => t.Id == id);

    public Task<MoveOrdType?> GetMoveOrdTypeByCodeAsync(string code) =>
        db.MoveOrdTypes.FirstOrDefaultAsync(t => t.Code.ToLower() == code.Trim().ToLower());

    public async Task<MoveOrdTypeDetailDto?> GetMoveOrdTypeDetailAsync(int id)
    {
        var item = await db.MoveOrdTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return null;

        var matchedOrders = await db.MoveOrders
            .Where(m => !string.IsNullOrEmpty(m.MoveOrdTypeCode) && m.MoveOrdTypeCode.ToLower() == item.Code.ToLower())
            .Include(m => m.FromWarehouse)
            .Include(m => m.ToWarehouse)
            .Include(m => m.Lines).ThenInclude(l => l.Product)
            .OrderByDescending(m => m.Date)
            .Take(25)
            .ToListAsync();

        int totalOrders = await db.MoveOrders.CountAsync(m => !string.IsNullOrEmpty(m.MoveOrdTypeCode) && m.MoveOrdTypeCode.ToLower() == item.Code.ToLower());
        int totalQtyMoved = matchedOrders.Sum(m => m.Lines.Sum(l => l.Quantity));

        return new MoveOrdTypeDetailDto(item, matchedOrders, totalOrders, totalQtyMoved);
    }

    public async Task<int> CreateMoveOrdTypeAsync(MoveOrdType item)
    {
        item.Code = item.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"MOVE_TYPE{await db.MoveOrdTypes.CountAsync() + 1:D2}";
        }
        item.Name = item.Name.Trim();
        item.Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();

        bool exists = await db.MoveOrdTypes.AnyAsync(t => t.Code == item.Code);
        if (exists)
            throw new InvalidOperationException($"Mã loại điều chuyển '{item.Code}' đã tồn tại trong hệ thống.");

        item.CreatedAt = DateTime.Now;
        db.MoveOrdTypes.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateMoveOrdTypeAsync(int id, MoveOrdType item)
    {
        var existing = await db.MoveOrdTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại hình điều chuyển.");

        existing.Name = item.Name.Trim();
        existing.Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();
        existing.IsUrgent = item.IsUrgent;
        existing.IsActive = item.IsActive;

        // Cập nhật tên hiển thị ở các lệnh điều chuyển đã lưu
        var ordersToUpdate = await db.MoveOrders.Where(m => m.MoveOrdTypeCode == existing.Code).ToListAsync();
        foreach (var order in ordersToUpdate)
        {
            order.MoveOrdTypeName = existing.Name;
        }

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật loại điều chuyển '{existing.Code}' thành công.");
    }

    public async Task<(bool ok, string msg)> ToggleMoveOrdTypeStatusAsync(int id)
    {
        var existing = await db.MoveOrdTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại hình điều chuyển.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt loại điều chuyển '{existing.Code}'." : $"Đã chuyển loại điều chuyển '{existing.Code}' sang trạng thái Tạm dừng áp dụng.");
    }

    public async Task<(bool ok, string msg)> ToggleMoveOrdTypeUrgentAsync(int id)
    {
        var existing = await db.MoveOrdTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại hình điều chuyển.");

        existing.IsUrgent = !existing.IsUrgent;
        await db.SaveChangesAsync();
        return (true, existing.IsUrgent ? $"Đã đánh dấu loại điều chuyển '{existing.Code}' là Khẩn cấp / Ưu tiên cao." : $"Đã chuyển loại điều chuyển '{existing.Code}' về mức Tiêu chuẩn.");
    }

    public async Task<(bool ok, string msg)> DeleteMoveOrdTypeAsync(int id)
    {
        var existing = await db.MoveOrdTypes.FirstOrDefaultAsync(t => t.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại hình điều chuyển.");

        bool inUse = await db.MoveOrders.AnyAsync(m => m.MoveOrdTypeCode != null && m.MoveOrdTypeCode.ToUpper() == existing.Code.ToUpper());
        if (inUse)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            return (true, $"Loại điều chuyển '{existing.Code}' đã có lệnh điều chuyển sử dụng nên đã được chuyển sang trạng thái Tạm dừng áp dụng thay vì xóa hẳn.");
        }

        db.MoveOrdTypes.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa loại điều chuyển '{existing.Code}'.");
    }

    // ==================== QUẢN LÝ ĐẠI LÝ PHÂN PHỐI & MẠNG LƯỚI ĐIỂM BÁN KHO (Mst_Dealer Skycic) ====================
    public async Task<DealerReport> DealersReportAsync(string? q = null, int? level = null, string? province = null, bool? activeOnly = null)
    {
        var query = db.Dealers.Include(d => d.Warehouse).AsQueryable();
        if (level.HasValue && level > 0) query = query.Where(d => d.Level == level.Value);
        if (!string.IsNullOrWhiteSpace(province)) query = query.Where(d => d.ProvinceCode != null && d.ProvinceCode.ToLower().Contains(province.Trim().ToLower()));
        if (activeOnly.HasValue) query = query.Where(d => d.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(d => d.Code.ToLower().Contains(term) ||
                                     d.Name.ToLower().Contains(term) ||
                                     (d.PresentBy != null && d.PresentBy.ToLower().Contains(term)) ||
                                     (d.Phone != null && d.Phone.ToLower().Contains(term)) ||
                                     (d.ProvinceCode != null && d.ProvinceCode.ToLower().Contains(term)) ||
                                     (d.Address != null && d.Address.ToLower().Contains(term)));
        }

        var allDealers = await db.Dealers.ToListAsync();
        var dealersList = await query.OrderBy(d => d.Level).ThenBy(d => d.Code).ToListAsync();

        var outDocs = await db.Docs
            .Where(d => d.Type == DocType.Out && d.Status == DocStatus.Posted)
            .Include(d => d.Lines)
            .ThenInclude(l => l.Product)
            .ToListAsync();

        var rows = new List<DealerRow>();
        foreach (var d in dealersList)
        {
            var parent = !string.IsNullOrEmpty(d.ParentCode) ? allDealers.FirstOrDefault(p => p.Code.Equals(d.ParentCode, StringComparison.OrdinalIgnoreCase)) : null;
            int subCount = allDealers.Count(s => !string.IsNullOrEmpty(s.ParentCode) && s.ParentCode.Equals(d.Code, StringComparison.OrdinalIgnoreCase));

            var matchedDocs = outDocs.Where(doc =>
                (!string.IsNullOrEmpty(doc.CustomerCode) && doc.CustomerCode.Equals(d.Code, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(doc.CustomerName) && doc.CustomerName.Contains(d.Name, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            int docCount = matchedDocs.Count;
            int shippedQty = matchedDocs.Sum(doc => doc.Lines.Sum(l => l.Quantity));
            decimal shippedAmount = matchedDocs.Sum(doc => doc.Lines.Sum(l => l.Quantity * (l.Product?.CostPrice ?? 0m)));

            string levelLabel = d.Level switch
            {
                1 => "Cấp 1 - Tổng đại lý",
                2 => "Cấp 2 - Đại lý vùng",
                3 => "Cấp 3 - Showroom / Điểm bán",
                _ => $"Cấp {d.Level}"
            };

            string badgeClass = d.Level switch
            {
                1 => "bg-primary text-white",
                2 => "bg-info text-dark",
                3 => "bg-secondary text-white",
                _ => "bg-light text-dark"
            };

            rows.Add(new DealerRow(
                d.Id,
                d.Code,
                d.Name,
                d.ParentCode,
                parent?.Name,
                d.Level,
                levelLabel,
                badgeClass,
                d.DealerType,
                d.BUCode,
                d.ProvinceCode,
                d.Address,
                d.PresentBy,
                d.GovIdNumber,
                d.Phone,
                d.Email,
                d.WarehouseId,
                d.Warehouse?.Name,
                d.IsActive,
                d.Remark,
                d.CreatedAt,
                subCount,
                docCount,
                shippedQty,
                shippedAmount
            ));
        }

        int totalDealers = allDealers.Count;
        int level1Count = allDealers.Count(d => d.Level == 1);
        int level2Count = allDealers.Count(d => d.Level == 2);
        int level3Count = allDealers.Count(d => d.Level >= 3);
        int activeCount = allDealers.Count(d => d.IsActive);
        int inactiveCount = totalDealers - activeCount;

        int totalShippedDocs = rows.Sum(r => r.TotalShippedDocsCount);
        int totalShippedQty = rows.Sum(r => r.TotalShippedQty);
        decimal totalShippedAmount = rows.Sum(r => r.TotalShippedAmount);

        return new DealerReport(
            q,
            level,
            province,
            activeOnly,
            totalDealers,
            level1Count,
            level2Count,
            level3Count,
            activeCount,
            inactiveCount,
            totalShippedDocs,
            totalShippedQty,
            totalShippedAmount,
            rows
        );
    }

    public Task<List<Dealer>> DealersAsync(string? q = null, bool? activeOnly = null)
    {
        var query = db.Dealers.AsQueryable();
        if (activeOnly.HasValue) query = query.Where(d => d.IsActive == activeOnly.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(d => d.Code.ToLower().Contains(term) || d.Name.ToLower().Contains(term));
        }
        return query.OrderBy(d => d.Level).ThenBy(d => d.Name).ToListAsync();
    }

    public Task<Dealer?> GetDealerAsync(int id) =>
        db.Dealers.Include(d => d.Warehouse).FirstOrDefaultAsync(d => d.Id == id);

    public Task<Dealer?> GetDealerByCodeAsync(string code) =>
        db.Dealers.Include(d => d.Warehouse).FirstOrDefaultAsync(d => d.Code.ToLower() == code.Trim().ToLower());

    public async Task<DealerDetailDto?> GetDealerDetailAsync(int id)
    {
        var dealer = await db.Dealers.Include(d => d.Warehouse).FirstOrDefaultAsync(d => d.Id == id);
        if (dealer == null) return null;

        Dealer? parentDealer = null;
        if (!string.IsNullOrEmpty(dealer.ParentCode))
        {
            parentDealer = await db.Dealers.FirstOrDefaultAsync(d => d.Code.ToLower() == dealer.ParentCode.ToLower());
        }

        var subDealers = await db.Dealers
            .Where(d => !string.IsNullOrEmpty(d.ParentCode) && d.ParentCode.ToLower() == dealer.Code.ToLower())
            .OrderBy(d => d.Level).ThenBy(d => d.Name)
            .ToListAsync();

        var outDocs = await db.Docs
            .Where(d => d.Type == DocType.Out && d.Status == DocStatus.Posted)
            .Include(d => d.Lines)
            .ThenInclude(l => l.Product)
            .Where(doc => (!string.IsNullOrEmpty(doc.CustomerCode) && doc.CustomerCode.ToLower() == dealer.Code.ToLower()) ||
                          (!string.IsNullOrEmpty(doc.CustomerName) && doc.CustomerName.ToLower().Contains(dealer.Name.ToLower())))
            .OrderByDescending(d => d.Date)
            .Take(10)
            .ToListAsync();

        int docCount = outDocs.Count;
        int shippedQty = outDocs.Sum(doc => doc.Lines.Sum(l => l.Quantity));
        decimal shippedAmount = outDocs.Sum(doc => doc.Lines.Sum(l => l.Quantity * (l.Product?.CostPrice ?? 0m)));

        return new DealerDetailDto(
            dealer,
            parentDealer,
            subDealers,
            dealer.Warehouse,
            subDealers.Count,
            docCount,
            shippedQty,
            shippedAmount,
            outDocs
        );
    }

    public async Task<int> CreateDealerAsync(Dealer item)
    {
        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"DL_{(item.Level == 1 ? "MB" : item.Level == 2 ? "KV" : "SR")}{await db.Dealers.CountAsync() + 1:D2}";
        }
        item.Code = item.Code.Trim().ToUpperInvariant();
        item.Name = item.Name.Trim();
        if (!string.IsNullOrWhiteSpace(item.ParentCode))
        {
            item.ParentCode = item.ParentCode.Trim().ToUpperInvariant();
        }
        if (!string.IsNullOrWhiteSpace(item.BUCode))
        {
            item.BUCode = item.BUCode.Trim().ToUpperInvariant();
        }

        bool exists = await db.Dealers.AnyAsync(d => d.Code == item.Code);
        if (exists)
        {
            throw new InvalidOperationException($"Mã đại lý '{item.Code}' đã tồn tại trong hệ thống.");
        }

        item.CreatedAt = DateTime.Now;
        db.Dealers.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateDealerAsync(int id, Dealer item)
    {
        var existing = await db.Dealers.FirstOrDefaultAsync(d => d.Id == id);
        if (existing == null) return (false, "Không tìm thấy đại lý phân phối.");

        existing.Name = item.Name.Trim();
        existing.ParentCode = string.IsNullOrWhiteSpace(item.ParentCode) ? null : item.ParentCode.Trim().ToUpperInvariant();
        existing.Level = item.Level > 0 ? item.Level : 1;
        existing.DealerType = string.IsNullOrWhiteSpace(item.DealerType) ? "Đại lý phân phối" : item.DealerType.Trim();
        existing.BUCode = string.IsNullOrWhiteSpace(item.BUCode) ? null : item.BUCode.Trim().ToUpperInvariant();
        existing.ProvinceCode = string.IsNullOrWhiteSpace(item.ProvinceCode) ? null : item.ProvinceCode.Trim();
        existing.Address = string.IsNullOrWhiteSpace(item.Address) ? null : item.Address.Trim();
        existing.PresentBy = string.IsNullOrWhiteSpace(item.PresentBy) ? null : item.PresentBy.Trim();
        existing.GovIdNumber = string.IsNullOrWhiteSpace(item.GovIdNumber) ? null : item.GovIdNumber.Trim();
        existing.Email = string.IsNullOrWhiteSpace(item.Email) ? null : item.Email.Trim();
        existing.Phone = string.IsNullOrWhiteSpace(item.Phone) ? null : item.Phone.Trim();
        existing.WarehouseId = item.WarehouseId;
        existing.IsActive = item.IsActive;
        existing.Remark = string.IsNullOrWhiteSpace(item.Remark) ? null : item.Remark.Trim();

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật đại lý '{existing.Name}' ({existing.Code}) thành công.");
    }

    public async Task<(bool ok, string msg)> ToggleDealerStatusAsync(int id)
    {
        var existing = await db.Dealers.FirstOrDefaultAsync(d => d.Id == id);
        if (existing == null) return (false, "Không tìm thấy đại lý phân phối.");

        existing.IsActive = !existing.IsActive;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt hoạt động đại lý '{existing.Code}'." : $"Đã chuyển đại lý '{existing.Code}' sang trạng thái tạm dừng.");
    }

    public async Task<(bool ok, string msg)> DeleteDealerAsync(int id)
    {
        var existing = await db.Dealers.FirstOrDefaultAsync(d => d.Id == id);
        if (existing == null) return (false, "Không tìm thấy đại lý phân phối.");

        bool hasSubs = await db.Dealers.AnyAsync(d => d.ParentCode != null && d.ParentCode.ToUpper() == existing.Code.ToUpper());
        if (hasSubs)
        {
            return (false, $"Đại lý '{existing.Code}' đang có các đại lý cấp dưới trực thuộc, không thể xóa.");
        }

        bool inUse = await db.Docs.AnyAsync(doc => doc.CustomerCode != null && doc.CustomerCode.ToUpper() == existing.Code.ToUpper());
        if (inUse)
        {
            existing.IsActive = false;
            await db.SaveChangesAsync();
            return (true, $"Đại lý '{existing.Code}' đã có phiếu xuất kho liên kết nên đã được chuyển sang trạng thái Tạm dừng hoạt động thay vì xóa hẳn.");
        }

        db.Dealers.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa đại lý '{existing.Code}'.");
    }

    // ==================== BẢN ĐỒ LỆNH GIAO HÀNG THEO PHIẾU XUẤT KHO (Rpt_MapDeliveryOrder_ByInvFIOut Skycic) ====================
    public async Task<MapDeliveryOrderReport> MapDeliveryOrderReportAsync(
        int? warehouseId,
        string? areaCode,
        string? customerCode,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        string? keyword)
    {
        var today = DateTime.Today;
        var todayStr = today.ToString("yyyy-MM-dd");

        // Dải ngày mặc định: hôm nay - 7 ngày đến hôm nay + 7 ngày
        var dtFrom = (fromDate ?? today.AddDays(-7)).Date;
        var dtTo = (toDate ?? today.AddDays(7)).Date;

        if (dtTo < dtFrom)
        {
            var temp = dtFrom;
            dtFrom = dtTo;
            dtTo = temp;
        }

        // Khống chế tối đa 31 ngày giống quy tắc DateDiff > 31 trong Skycic
        if ((dtTo - dtFrom).TotalDays > 31)
        {
            dtTo = dtFrom.AddDays(31);
        }

        var listDates = Enumerable.Range(0, (int)(dtTo - dtFrom).TotalDays + 1)
            .Select(d => dtFrom.AddDays(d).ToString("yyyy-MM-dd"))
            .ToList();

        // Lấy danh mục Khu vực & Khách hàng để đối chiếu AreaCode/AreaName
        var areas = await db.Areas.ToListAsync();
        var areaDict = areas.ToDictionary(a => a.Code.ToUpper(), a => a.Name);

        var customers = await db.Customers.ToListAsync();
        var cusDict = customers.ToDictionary(c => c.Code.ToUpper(), c => c);

        var warehouses = await db.Warehouses.ToListAsync();
        string whName = warehouseId.HasValue
            ? (warehouses.FirstOrDefault(w => w.Id == warehouseId.Value)?.Name ?? "Kho không xác định")
            : "Toàn bộ kho";

        var allRows = new List<MapDeliveryOrderRow>();

        // 1. Lấy dữ liệu từ StockDoc (Loại Out, không lấy trạng thái Hủy)
        var stockDocsQuery = db.Docs
            .Include(d => d.FromWarehouse)
            .Include(d => d.Lines).ThenInclude(l => l.Product)
            .Where(d => d.Type == DocType.Out && d.Status != DocStatus.Cancelled)
            .Where(d => d.Date >= dtFrom && d.Date < dtTo.AddDays(1))
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            stockDocsQuery = stockDocsQuery.Where(d => d.FromWarehouseId == warehouseId.Value);
        }

        var stockDocs = await stockDocsQuery.ToListAsync();

        foreach (var doc in stockDocs)
        {
            string cCode = doc.CustomerCode?.Trim() ?? "";
            string cName = doc.CustomerName?.Trim() ?? (!string.IsNullOrEmpty(cCode) ? cCode : "Khách vãng lai");

            // Xác định AreaCode: ưu tiên từ Khách hàng, kế đến từ Kho xuất
            string rAreaCode = "AREA_MB";
            if (!string.IsNullOrEmpty(cCode) && cusDict.TryGetValue(cCode.ToUpper(), out var cus) && !string.IsNullOrEmpty(cus.AreaCode))
            {
                rAreaCode = cus.AreaCode;
            }
            else if (!string.IsNullOrEmpty(doc.FromWarehouse?.AreaCode))
            {
                rAreaCode = doc.FromWarehouse.AreaCode;
            }

            string rAreaName = areaDict.TryGetValue(rAreaCode.ToUpper(), out var aName) ? aName : rAreaCode;
            string docDateStr = doc.Date.ToString("yyyy-MM-dd");
            bool isPosted = doc.Status == DocStatus.Posted;
            string docStatus = isPosted ? "DELIVERED" : "PENDING";
            string statusLabel = isPosted ? "Đã giao hàng" : "Chờ giao";
            string badgeClass = isPosted ? "bg-success text-white" : "bg-warning text-dark";

            // Phiếu xuất giao chậm: PENDING mà ngày hẹn <= Hôm nay
            bool isDelayed = (!isPosted) && (doc.Date.Date <= today);

            foreach (var line in doc.Lines)
            {
                var row = new MapDeliveryOrderRow
                {
                    WarehouseId = doc.FromWarehouseId ?? 0,
                    WarehouseCode = doc.FromWarehouse?.Code ?? "",
                    WarehouseName = doc.FromWarehouse?.Name ?? "",
                    AreaCode = rAreaCode,
                    AreaName = rAreaName,
                    CustomerCode = cCode,
                    CustomerName = cName,
                    DeliveryOrderNo = doc.Code,
                    DocTypeLabel = "Xuất bán hàng",
                    OrderDate = doc.Date,
                    ProductId = line.ProductId,
                    ProductCode = line.Product?.Code ?? "",
                    ProductName = line.Product?.Name ?? "",
                    Uom = line.Product?.Uom ?? "cái",
                    TotalQty = line.Quantity,
                    Status = docStatus,
                    StatusLabel = statusLabel,
                    BadgeClass = badgeClass,
                    IsDelayed = isDelayed,
                    Note = doc.Note
                };

                // Điền sản lượng cho các ngày
                foreach (var dStr in listDates)
                {
                    int qtyForDay = (dStr == docDateStr) ? line.Quantity : 0;
                    bool cellIsToday = (dStr == todayStr);
                    bool cellIsDelayed = isDelayed && (dStr == docDateStr);

                    row.DailyQuantities[dStr] = qtyForDay;
                    row.Cells.Add(new MapDeliveryOrderDayCell(dStr, qtyForDay, cellIsToday, cellIsDelayed));
                }

                allRows.Add(row);
            }
        }

        // 2. Lấy dữ liệu từ InventoryOutFG (Phiếu xuất kho thành phẩm)
        var fgDocsQuery = db.InventoryOutFGs
            .Include(f => f.Warehouse)
            .Include(f => f.Lines).ThenInclude(l => l.Product)
            .Where(f => f.Status != InvOutFGStatus.Cancelled)
            .Where(f => f.Date >= dtFrom && f.Date < dtTo.AddDays(1))
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            fgDocsQuery = fgDocsQuery.Where(f => f.WarehouseId == warehouseId.Value);
        }

        var fgDocs = await fgDocsQuery.ToListAsync();

        foreach (var fg in fgDocs)
        {
            string cCode = fg.AgentCode?.Trim() ?? "";
            string cName = fg.CustomerName?.Trim() ?? (!string.IsNullOrEmpty(cCode) ? cCode : "Đại lý nhận hàng");

            string rAreaCode = "AREA_MB";
            if (!string.IsNullOrEmpty(cCode) && cusDict.TryGetValue(cCode.ToUpper(), out var cus) && !string.IsNullOrEmpty(cus.AreaCode))
            {
                rAreaCode = cus.AreaCode;
            }
            else if (!string.IsNullOrEmpty(fg.Warehouse?.AreaCode))
            {
                rAreaCode = fg.Warehouse.AreaCode;
            }

            string rAreaName = areaDict.TryGetValue(rAreaCode.ToUpper(), out var aName) ? aName : rAreaCode;
            string docDateStr = fg.Date.ToString("yyyy-MM-dd");
            bool isApproved = fg.Status == InvOutFGStatus.Approved;
            string docStatus = isApproved ? "DELIVERED" : "PENDING";
            string statusLabel = isApproved ? "Đã giao hàng" : "Chờ giao";
            string badgeClass = isApproved ? "bg-success text-white" : "bg-warning text-dark";

            bool isDelayed = (!isApproved) && (fg.Date.Date <= today);

            string driverInfo = "";
            if (!string.IsNullOrEmpty(fg.DriverName) || !string.IsNullOrEmpty(fg.PlateNo))
            {
                driverInfo = $"{fg.DriverName} ({fg.PlateNo})";
            }

            foreach (var line in fg.Lines)
            {
                var row = new MapDeliveryOrderRow
                {
                    WarehouseId = fg.WarehouseId,
                    WarehouseCode = fg.Warehouse?.Code ?? "",
                    WarehouseName = fg.Warehouse?.Name ?? "",
                    AreaCode = rAreaCode,
                    AreaName = rAreaName,
                    CustomerCode = cCode,
                    CustomerName = cName,
                    DeliveryOrderNo = fg.Code,
                    DocTypeLabel = "Xuất thành phẩm",
                    OrderDate = fg.Date,
                    ProductId = line.ProductId,
                    ProductCode = line.Product?.Code ?? "",
                    ProductName = line.Product?.Name ?? "",
                    Uom = line.Product?.Uom ?? "cái",
                    TotalQty = line.Qty,
                    Status = docStatus,
                    StatusLabel = statusLabel,
                    BadgeClass = badgeClass,
                    IsDelayed = isDelayed,
                    DeliveryAddress = fg.DeliveryAddress,
                    DriverInfo = driverInfo,
                    Note = fg.Remark
                };

                foreach (var dStr in listDates)
                {
                    int qtyForDay = (dStr == docDateStr) ? line.Qty : 0;
                    bool cellIsToday = (dStr == todayStr);
                    bool cellIsDelayed = isDelayed && (dStr == docDateStr);

                    row.DailyQuantities[dStr] = qtyForDay;
                    row.Cells.Add(new MapDeliveryOrderDayCell(dStr, qtyForDay, cellIsToday, cellIsDelayed));
                }

                allRows.Add(row);
            }
        }

        // Áp dụng các bộ lọc bổ sung
        var filteredRows = allRows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(areaCode))
        {
            var aCode = areaCode.Trim().ToUpper();
            filteredRows = filteredRows.Where(r => r.AreaCode.ToUpper() == aCode);
        }

        if (!string.IsNullOrWhiteSpace(customerCode))
        {
            var cTerm = customerCode.Trim().ToLower();
            filteredRows = filteredRows.Where(r => r.CustomerCode.ToLower().Contains(cTerm) || r.CustomerName.ToLower().Contains(cTerm));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToUpper();
            if (s == "DELAYED")
            {
                filteredRows = filteredRows.Where(r => r.IsDelayed);
            }
            else
            {
                filteredRows = filteredRows.Where(r => r.Status.ToUpper() == s);
            }
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim().ToLower();
            filteredRows = filteredRows.Where(r =>
                r.DeliveryOrderNo.ToLower().Contains(k) ||
                r.CustomerName.ToLower().Contains(k) ||
                r.CustomerCode.ToLower().Contains(k) ||
                r.ProductCode.ToLower().Contains(k) ||
                r.ProductName.ToLower().Contains(k) ||
                r.AreaName.ToLower().Contains(k)
            );
        }

        // Sắp xếp theo ngày tăng dần, sau đó theo khu vực và số phiếu xuất
        var resultList = filteredRows
            .OrderBy(r => r.OrderDate)
            .ThenBy(r => r.AreaName)
            .ThenBy(r => r.DeliveryOrderNo)
            .ToList();

        // Gán STT
        for (int i = 0; i < resultList.Count; i++)
        {
            resultList[i].Stt = i + 1;
        }

        // Tính toán các chỉ số KPI
        int totalOrders = resultList.Count;
        int completedOrders = resultList.Count(r => r.Status == "DELIVERED");
        int pendingOrders = resultList.Count(r => r.Status == "PENDING");
        int delayedOrders = resultList.Count(r => r.IsDelayed);
        int totalQty = resultList.Sum(r => r.TotalQty);
        double onTimeRate = totalOrders > 0
            ? Math.Round((double)(totalOrders - delayedOrders) / totalOrders * 100.0, 1)
            : 100.0;

        // Phân bổ thống kê theo từng Khu vực (Area Summaries)
        var areaSummaries = resultList
            .GroupBy(r => new { r.AreaCode, r.AreaName })
            .Select(g =>
            {
                int grpTotal = g.Count();
                int grpCompleted = g.Count(x => x.Status == "DELIVERED");
                int grpPending = g.Count(x => x.Status == "PENDING");
                int grpDelayed = g.Count(x => x.IsDelayed);
                int grpQty = g.Sum(x => x.TotalQty);
                double grpRate = grpTotal > 0
                    ? Math.Round((double)(grpTotal - grpDelayed) / grpTotal * 100.0, 1)
                    : 100.0;

                return new AreaDeliverySummary(
                    g.Key.AreaCode,
                    g.Key.AreaName,
                    grpTotal,
                    grpCompleted,
                    grpPending,
                    grpDelayed,
                    grpQty,
                    grpRate
                );
            })
            .OrderByDescending(a => a.TotalOrders)
            .ToList();

        return new MapDeliveryOrderReport(
            warehouseId,
            whName,
            areaCode,
            customerCode,
            status,
            keyword,
            dtFrom,
            dtTo,
            todayStr,
            listDates,
            totalOrders,
            completedOrders,
            pendingOrders,
            delayedOrders,
            onTimeRate,
            totalQty,
            resultList,
            areaSummaries
        );
    }

    // ==================== QUẢN LÝ BIỂU MẪU IN KHO & THIẾT KẾ TEM NHÃN (InvF_TempPrint & Mst_TempPrintType Skycic) ====================
    public Task<List<TempPrintType>> TempPrintTypesAsync(bool? activeOnly = null)
    {
        var query = db.TempPrintTypes.AsQueryable();
        if (activeOnly == true) query = query.Where(t => t.IsActive);
        return query.OrderBy(t => t.GroupCode).ThenBy(t => t.Code).ToListAsync();
    }

    public Task<List<TempPrint>> TempPrintsAsync(string? typeCode = null, bool? activeOnly = null)
    {
        var query = db.TempPrints.AsQueryable();
        if (!string.IsNullOrWhiteSpace(typeCode))
        {
            var tc = typeCode.Trim().ToUpperInvariant();
            query = query.Where(t => t.TypeCode.ToUpper() == tc);
        }
        if (activeOnly == true) query = query.Where(t => t.IsActive);
        return query.OrderBy(t => t.TypeCode).ThenBy(t => t.Code).ToListAsync();
    }

    public async Task<TempPrintReport> TempPrintsReportAsync(string? q = null, string? typeCode = null, bool? activeOnly = null)
    {
        var types = await db.TempPrintTypes.ToListAsync();
        var typeDict = types.ToDictionary(t => t.Code.ToUpper(), t => t);

        var query = db.TempPrints.AsQueryable();
        if (!string.IsNullOrWhiteSpace(typeCode))
        {
            var tc = typeCode.Trim().ToUpperInvariant();
            query = query.Where(t => t.TypeCode.ToUpper() == tc);
        }
        if (activeOnly.HasValue)
        {
            query = query.Where(t => t.IsActive == activeOnly.Value);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(t => t.Code.ToLower().Contains(term) ||
                                     t.Name.ToLower().Contains(term) ||
                                     t.HeaderTitle.ToLower().Contains(term) ||
                                     t.TypeCode.ToLower().Contains(term) ||
                                     t.UnitName.ToLower().Contains(term));
        }

        var allTemplates = await query.OrderByDescending(t => t.IsDefault).ThenBy(t => t.TypeCode).ThenBy(t => t.Code).ToListAsync();

        var rows = allTemplates.Select(t =>
        {
            typeDict.TryGetValue(t.TypeCode.ToUpper(), out var tType);
            return new TempPrintRow(
                t.Id,
                t.Code,
                t.Name,
                t.TypeCode,
                tType?.Name ?? t.TypeCode,
                tType?.GroupCode ?? "DOC",
                t.PaperSize,
                PaperSizeLabel(t.PaperSize),
                t.UnitName,
                t.HeaderTitle,
                t.IsDefault,
                t.IsActive,
                t.Remark,
                t.CreatedAt,
                t.UpdatedAt
            );
        }).ToList();

        var totalAll = await db.TempPrints.CountAsync();
        var activeAll = await db.TempPrints.CountAsync(t => t.IsActive);
        var inactiveAll = totalAll - activeAll;
        var supportedTypes = await db.TempPrints.Select(t => t.TypeCode).Distinct().CountAsync();
        var defaultCount = await db.TempPrints.CountAsync(t => t.IsDefault);

        return new TempPrintReport(
            typeCode,
            activeOnly,
            q,
            totalAll,
            activeAll,
            inactiveAll,
            supportedTypes,
            defaultCount,
            rows
        );
    }

    public Task<TempPrint?> GetTempPrintAsync(int id) =>
        db.TempPrints.FirstOrDefaultAsync(t => t.Id == id);

    public Task<TempPrint?> GetTempPrintByCodeAsync(string code) =>
        db.TempPrints.FirstOrDefaultAsync(t => t.Code.ToLower() == code.Trim().ToLower());

    public Task<TempPrint?> GetDefaultTempPrintByTypeAsync(string typeCode)
    {
        var tc = typeCode.Trim().ToUpperInvariant();
        return db.TempPrints.FirstOrDefaultAsync(t => t.TypeCode.ToUpper() == tc && t.IsDefault && t.IsActive);
    }

    public async Task<TempPrintPreviewResult?> PreviewTempPrintAsync(int id)
    {
        var t = await db.TempPrints.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return null;

        var tType = await db.TempPrintTypes.FirstOrDefaultAsync(x => x.Code.ToUpper() == t.TypeCode.ToUpper());
        string typeName = tType?.Name ?? t.TypeCode;

        string docNo = t.TypeCode switch
        {
            "IN" => "PN-2026-030089",
            "OUT" => "PX-2026-030145",
            "MOVE" => "PC-2026-030032",
            "AUDIT" => "KK-2026-030012",
            "CARTON" => "CTN-2026-00452",
            "BOX" => "BOX-2026-0128",
            "K80" => "POS-2026-0099",
            _ => "DOC-2026-0088"
        };
        string todayStr = DateTime.Today.ToString("dd/MM/yyyy");
        string todayFull = $"Ngày {DateTime.Today.Day:D2} tháng {DateTime.Today.Month:D2} năm {DateTime.Today.Year}";

        string partnerName = t.TypeCode switch
        {
            "IN" => "Công ty TNHH Apple Computer Việt Nam",
            "OUT" => "Công ty Cổ phần Bán lẻ Kỹ thuật số FPT (FPT Retail)",
            "MOVE" => "Kho TP. Hồ Chí Minh - Chi nhánh Tân Bình",
            "AUDIT" => "Hội đồng kiểm kê kho MiniWMS",
            "CARTON" => "Tổng Đại lý Phân phối Miền Bắc - Phúc Thịnh",
            _ => "Khách hàng thương mại"
        };

        string partnerAddress = t.TypeCode switch
        {
            "IN" => "Tầng 5, Tòa nhà Metropolitan, 235 Đồng Khởi, Q.1, TP.HCM",
            "OUT" => "261 - 263 Khánh Hội, Phường 2, Quận 4, TP.HCM",
            "MOVE" => "KCN Tân Bình, Tây Thạnh, Tân Phú, TP.HCM",
            _ => "Số 188 Nguyễn Trãi, Thanh Xuân, Hà Nội"
        };

        string reason = t.TypeCode switch
        {
            "IN" => "Nhập kho theo Đơn đặt hàng mua PO-2026-881, Hóa đơn VAT 004812",
            "OUT" => "Xuất bán buôn theo Đơn hàng SO-2026-9812, Lệnh giao DO-0388",
            "MOVE" => "Điều chuyển cân đối an toàn định mức tồn kho chi nhánh miền Nam",
            "AUDIT" => "Kiểm kê toàn diện số dư thực tế kỳ chốt tháng 03/2026",
            "CARTON" => "Đóng kiện xuất kho vận chuyển logistics liên tỉnh",
            _ => "Nghiệp vụ lưu chuyển kho"
        };

        string itemsTableHtml = @"
<table style=""width:100%; border-collapse:collapse; margin:15px 0; font-size:13px;"">
    <thead>
        <tr style=""background:#f2f2f2; text-align:center;"">
            <th style=""border:1px solid #000; padding:6px; width:40px;"">STT</th>
            <th style=""border:1px solid #000; padding:6px; width:100px;"">Mã hàng</th>
            <th style=""border:1px solid #000; padding:6px;"">Tên hàng hóa, quy cách</th>
            <th style=""border:1px solid #000; padding:6px; width:60px;"">ĐVT</th>
            <th style=""border:1px solid #000; padding:6px; width:70px;"">Số lượng</th>
            <th style=""border:1px solid #000; padding:6px; width:110px;"">Đơn giá</th>
            <th style=""border:1px solid #000; padding:6px; width:120px;"">Thành tiền (VNĐ)</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td style=""border:1px solid #000; padding:6px; text-align:center;"">1</td>
            <td style=""border:1px solid #000; padding:6px; font-weight:bold;"">IP15-128</td>
            <td style=""border:1px solid #000; padding:6px;"">Điện thoại iPhone 15 128GB Black - Chính hãng VN/A</td>
            <td style=""border:1px solid #000; padding:6px; text-align:center;"">Chiếc</td>
            <td style=""border:1px solid #000; padding:6px; text-align:right; font-weight:bold;"">50</td>
            <td style=""border:1px solid #000; padding:6px; text-align:right;"">19,500,000</td>
            <td style=""border:1px solid #000; padding:6px; text-align:right; font-weight:bold;"">975,000,000</td>
        </tr>
        <tr>
            <td style=""border:1px solid #000; padding:6px; text-align:center;"">2</td>
            <td style=""border:1px solid #000; padding:6px; font-weight:bold;"">MAC-M3-16</td>
            <td style=""border:1px solid #000; padding:6px;"">MacBook Air 13 inch M3 (16GB RAM / 256GB SSD) Space Gray</td>
            <td style=""border:1px solid #000; padding:6px; text-align:center;"">Máy</td>
            <td style=""border:1px solid #000; padding:6px; text-align:right; font-weight:bold;"">15</td>
            <td style=""border:1px solid #000; padding:6px; text-align:right;"">27,000,000</td>
            <td style=""border:1px solid #000; padding:6px; text-align:right; font-weight:bold;"">405,000,000</td>
        </tr>
        <tr>
            <td style=""border:1px solid #000; padding:6px; text-align:center;"">3</td>
            <td style=""border:1px solid #000; padding:6px; font-weight:bold;"">WATCH-S9-41</td>
            <td style=""border:1px solid #000; padding:6px;"">Đồng hồ thông minh Apple Watch Series 9 GPS 41mm</td>
            <td style=""border:1px solid #000; padding:6px; text-align:center;"">Chiếc</td>
            <td style=""border:1px solid #000; padding:6px; text-align:right; font-weight:bold;"">10</td>
            <td style=""border:1px solid #000; padding:6px; text-align:right;"">7,000,000</td>
            <td style=""border:1px solid #000; padding:6px; text-align:right; font-weight:bold;"">70,000,000</td>
        </tr>
        <tr style=""font-weight:bold; background:#fafafa;"">
            <td colspan=""4"" style=""border:1px solid #000; padding:6px; text-align:center;"">CỘNG TỔNG</td>
            <td style=""border:1px solid #000; padding:6px; text-align:right; color:#b02a37;"">75</td>
            <td style=""border:1px solid #000; padding:6px; text-align:center;"">x</td>
            <td style=""border:1px solid #000; padding:6px; text-align:right; color:#b02a37;"">1,450,000,000</td>
        </tr>
    </tbody>
</table>
<div style=""font-size:13px; font-style:italic; margin-bottom:15px;"">
    - Tổng số tiền (viết bằng chữ): <strong>Một tỷ bốn trăm năm mươi triệu đồng chẵn.</strong><br/>
    - Kèm theo: <strong>03</strong> chứng từ gốc (Hóa đơn GTGT, Phiếu xuất xưởng, Giấy bảo hành).
</div>";

        string signaturesHtml = @"
<table style=""width:100%; border-collapse:collapse; margin-top:25px; font-size:13px; text-align:center;"">
    <tr>
        <td style=""width:25%; font-weight:bold;"">NGƯỜI LẬP BIỂU<br/><span style=""font-weight:normal; font-style:italic; font-size:11px;"">(Ký, họ tên)</span><br/><br/><br/><br/><strong>Nguyễn Thị Thu</strong></td>
        <td style=""width:25%; font-weight:bold;"">NGƯỜI GIAO HÀNG<br/><span style=""font-weight:normal; font-style:italic; font-size:11px;"">(Ký, họ tên)</span><br/><br/><br/><br/><strong>Trần Đình Trọng</strong></td>
        <td style=""width:25%; font-weight:bold;"">THỦ KHO<br/><span style=""font-weight:normal; font-style:italic; font-size:11px;"">(Ký, họ tên)</span><br/><br/><br/><br/><strong>Nguyễn Văn Hùng</strong></td>
        <td style=""width:25%; font-weight:bold;"">KẾ TOÁN TRƯỞNG / GIÁM ĐỐC<br/><span style=""font-weight:normal; font-style:italic; font-size:11px;"">(Ký, họ tên, đóng dấu)</span><br/><br/><br/><br/><strong>Lê Hoàng Long</strong></td>
    </tr>
</table>";

        string barcodeSvg = $@"
<div style=""display:inline-block; border:1px solid #000; padding:4px 10px; background:#fff; text-align:center;"">
    <div style=""font-family:monospace; font-size:24px; letter-spacing:3px; font-weight:bold;"">||| | | |||| | ||||| | |||</div>
    <div style=""font-size:11px; font-weight:bold; letter-spacing:1px;"">*{docNo}*</div>
</div>";

        string html = t.BodyTemplateHtml;
        if (string.IsNullOrWhiteSpace(html))
        {
            html = "<p>Mẫu in chưa có nội dung template.</p>";
        }

        html = html
            .Replace("{{DocNo}}", docNo)
            .Replace("{{DocDate}}", todayStr)
            .Replace("{{DocDateFull}}", todayFull)
            .Replace("{{UnitName}}", t.UnitName ?? "")
            .Replace("{{UnitAddress}}", t.UnitAddress ?? "Lô CN-08, KCN Bắc Thăng Long, Đông Anh, TP. Hà Nội")
            .Replace("{{UnitPhone}}", t.UnitPhone ?? "024-3795-8888")
            .Replace("{{UnitEmail}}", t.UnitEmail ?? "contact@miniwms.vn")
            .Replace("{{HeaderTitle}}", t.HeaderTitle ?? "")
            .Replace("{{SubTitle}}", t.SubTitle ?? "")
            .Replace("{{WarehouseName}}", "Kho Hà Nội (KHO-HN)")
            .Replace("{{WarehouseAddress}}", "KCN Bắc Thăng Long, Đông Anh, Hà Nội")
            .Replace("{{PartnerName}}", partnerName)
            .Replace("{{PartnerAddress}}", partnerAddress)
            .Replace("{{Deliverer}}", "Trần Đình Trọng (Bộ phận Vận chuyển)")
            .Replace("{{Receiver}}", "Nguyễn Văn Hùng (Thủ kho tiếp nhận)")
            .Replace("{{Reason}}", reason)
            .Replace("{{Note}}", "Hàng hóa nguyên kiện, đầy đủ chứng chỉ CO/CQ và phiếu bảo hành chính hãng.")
            .Replace("{{NoteFooter}}", t.NoteFooter ?? "")
            .Replace("{{TotalQty}}", "75")
            .Replace("{{TotalAmount}}", "1,450,000,000 đ")
            .Replace("{{TotalAmountWords}}", "Một tỷ bốn trăm năm mươi triệu đồng chẵn")
            .Replace("{{ItemsTable}}", itemsTableHtml)
            .Replace("{{Signatures}}", signaturesHtml)
            .Replace("{{Barcode}}", barcodeSvg);

        return new TempPrintPreviewResult(
            t.Id,
            t.Code,
            t.Name,
            t.TypeCode,
            typeName,
            t.PaperSize,
            PaperSizeCss(t.PaperSize),
            html
        );
    }

    public async Task<int> CreateTempPrintAsync(TempPrint item)
    {
        if (string.IsNullOrWhiteSpace(item.Code))
        {
            item.Code = $"PRT_{item.TypeCode}_{DateTime.Now:yyMMddHHmmss}";
        }
        item.Code = item.Code.Trim().ToUpperInvariant();
        item.Name = item.Name.Trim();
        item.TypeCode = item.TypeCode.Trim().ToUpperInvariant();
        item.CreatedAt = DateTime.Now;

        bool exists = await db.TempPrints.AnyAsync(x => x.Code == item.Code);
        if (exists)
        {
            throw new InvalidOperationException($"Mã mẫu in '{item.Code}' đã tồn tại trong hệ thống.");
        }

        if (item.IsDefault)
        {
            await db.TempPrints
                .Where(x => x.TypeCode == item.TypeCode && x.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, false));
        }

        db.TempPrints.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateTempPrintAsync(int id, TempPrint item)
    {
        var existing = await db.TempPrints.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return (false, "Không tìm thấy mẫu in.");

        existing.Name = item.Name.Trim();
        existing.TypeCode = item.TypeCode.Trim().ToUpperInvariant();
        existing.PaperSize = string.IsNullOrWhiteSpace(item.PaperSize) ? "A4_Portrait" : item.PaperSize.Trim();
        existing.UnitName = item.UnitName?.Trim() ?? "";
        existing.UnitAddress = item.UnitAddress?.Trim();
        existing.UnitPhone = item.UnitPhone?.Trim();
        existing.UnitEmail = item.UnitEmail?.Trim();
        existing.HeaderTitle = item.HeaderTitle?.Trim() ?? "";
        existing.SubTitle = item.SubTitle?.Trim();
        existing.BodyTemplateHtml = item.BodyTemplateHtml ?? "";
        existing.NoteFooter = item.NoteFooter?.Trim();
        existing.IsActive = item.IsActive;
        existing.Remark = item.Remark?.Trim();
        existing.UpdatedAt = DateTime.Now;

        if (item.IsDefault && !existing.IsDefault)
        {
            await db.TempPrints
                .Where(x => x.TypeCode == existing.TypeCode && x.Id != id && x.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, false));
            existing.IsDefault = true;
        }
        else if (!item.IsDefault && existing.IsDefault)
        {
            existing.IsDefault = false;
        }

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật mẫu in '{existing.Name}' ({existing.Code}) thành công.");
    }

    public async Task<(bool ok, string msg)> ToggleTempPrintStatusAsync(int id)
    {
        var existing = await db.TempPrints.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return (false, "Không tìm thấy mẫu in.");

        existing.IsActive = !existing.IsActive;
        existing.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        return (true, existing.IsActive ? $"Đã kích hoạt mẫu in '{existing.Code}'." : $"Đã chuyển mẫu in '{existing.Code}' sang trạng thái tạm dừng.");
    }

    public async Task<(bool ok, string msg)> SetDefaultTempPrintAsync(int id)
    {
        var existing = await db.TempPrints.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return (false, "Không tìm thấy mẫu in.");

        await db.TempPrints
            .Where(x => x.TypeCode == existing.TypeCode && x.Id != id && x.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, false));

        existing.IsDefault = true;
        existing.IsActive = true;
        existing.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        return (true, $"Đã đặt mẫu in '{existing.Name}' ({existing.Code}) làm mẫu in mặc định cho nghiệp vụ {existing.TypeCode}.");
    }

    public async Task<(bool ok, string msg)> DeleteTempPrintAsync(int id)
    {
        var existing = await db.TempPrints.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return (false, "Không tìm thấy mẫu in.");

        if (existing.IsDefault)
        {
            return (false, $"Mẫu in '{existing.Code}' đang là mẫu mặc định cho loại {existing.TypeCode}. Vui lòng chỉ định mẫu khác làm mặc định trước khi xóa.");
        }

        db.TempPrints.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa mẫu in '{existing.Code}'.");
    }

    public async Task<int> CreateTempPrintTypeAsync(TempPrintType item)
    {
        item.Code = item.Code.Trim().ToUpperInvariant();
        item.Name = item.Name.Trim();
        item.GroupCode = string.IsNullOrWhiteSpace(item.GroupCode) ? "DOC" : item.GroupCode.Trim().ToUpperInvariant();
        item.CreatedAt = DateTime.Now;

        bool exists = await db.TempPrintTypes.AnyAsync(x => x.Code == item.Code);
        if (exists)
        {
            throw new InvalidOperationException($"Mã loại mẫu in '{item.Code}' đã tồn tại.");
        }

        db.TempPrintTypes.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task<(bool ok, string msg)> UpdateTempPrintTypeAsync(int id, TempPrintType item)
    {
        var existing = await db.TempPrintTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại mẫu in.");

        existing.Name = item.Name.Trim();
        existing.GroupCode = string.IsNullOrWhiteSpace(item.GroupCode) ? "DOC" : item.GroupCode.Trim().ToUpperInvariant();
        existing.Description = item.Description?.Trim();
        existing.IsActive = item.IsActive;

        await db.SaveChangesAsync();
        return (true, $"Đã cập nhật loại mẫu in '{existing.Name}' ({existing.Code}) thành công.");
    }

    public async Task<(bool ok, string msg)> DeleteTempPrintTypeAsync(int id)
    {
        var existing = await db.TempPrintTypes.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return (false, "Không tìm thấy loại mẫu in.");

        bool inUse = await db.TempPrints.AnyAsync(x => x.TypeCode == existing.Code);
        if (inUse)
        {
            return (false, $"Loại mẫu '{existing.Code}' đang có các mẫu in liên kết, không thể xóa.");
        }

        db.TempPrintTypes.Remove(existing);
        await db.SaveChangesAsync();
        return (true, $"Đã xóa loại mẫu in '{existing.Code}'.");
    }

    private static string PaperSizeLabel(string? size) => size switch
    {
        "A4_Portrait" => "A4 Dọc (210 x 297 mm)",
        "A4_Landscape" => "A4 Ngang (297 x 210 mm)",
        "A5_Landscape" => "A5 Ngang (210 x 148 mm)",
        "A5_Portrait" => "A5 Dọc (148 x 210 mm)",
        "Label_100x150" => "Decal Thùng 100 x 150 mm",
        "Label_100x75" => "Decal Hộp 100 x 75 mm",
        "Thermal_K80" => "In nhiệt POS K80 (80 mm)",
        _ => size ?? "A4 Dọc"
    };

    private static string PaperSizeCss(string? size) => size switch
    {
        "A4_Portrait" => "width: 210mm; min-height: 297mm; padding: 15mm 20mm; margin: 0 auto; background: #fff;",
        "A4_Landscape" => "width: 297mm; min-height: 210mm; padding: 15mm 20mm; margin: 0 auto; background: #fff;",
        "A5_Landscape" => "width: 210mm; min-height: 148mm; padding: 10mm 15mm; margin: 0 auto; background: #fff;",
        "A5_Portrait" => "width: 148mm; min-height: 210mm; padding: 10mm 12mm; margin: 0 auto; background: #fff;",
        "Label_100x150" => "width: 100mm; min-height: 150mm; padding: 8mm; margin: 0 auto; background: #fff; border: 1px dashed #bbb;",
        "Label_100x75" => "width: 100mm; min-height: 75mm; padding: 6mm; margin: 0 auto; background: #fff; border: 1px dashed #bbb;",
        "Thermal_K80" => "width: 80mm; min-height: 160mm; padding: 5mm; margin: 0 auto; background: #fff; font-family: monospace;",
        _ => "width: 210mm; min-height: 297mm; padding: 15mm 20mm; margin: 0 auto; background: #fff;"
    };

    // ==================== BÁO CÁO LỊCH SỬ GIAO DỊCH NHẬP XUẤT THEO ĐỐI TÁC (Rpt_Summary_In_Out_Sup_Pivot Skycic) ====================
    public async Task<SummaryInOutPartnerPivotReport> SummaryInOutPartnerPivotReportAsync(
        int? warehouseId,
        string? partnerCode,
        string? productGrpCode,
        int? productId,
        string? actionType,
        DateTime? fromDate,
        DateTime? toDate,
        string? keyword)
    {
        string whName = "Tất cả kho";
        if (warehouseId.HasValue)
        {
            var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
            if (wh != null) whName = wh.Name;
        }

        DateTime start = fromDate?.Date ?? DateTime.Today.AddDays(-60);
        DateTime end = toDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Today.AddDays(1).AddTicks(-1);

        var warehouses = await db.Warehouses.ToListAsync();
        var whDict = warehouses.ToDictionary(w => w.Id, w => w);

        var products = await db.Products.ToListAsync();
        var prodDict = products.ToDictionary(p => p.Id, p => p);

        var prodGroups = await db.ProductGroups.ToListAsync();
        var grpDict = prodGroups.ToDictionary(g => g.Code, g => g.Name, StringComparer.OrdinalIgnoreCase);

        var customers = await db.Customers.ToListAsync();
        var cusDictByCode = customers.GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var suppliers = await db.Suppliers.ToListAsync();
        var supDictByCode = suppliers.GroupBy(s => s.Code, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var dealers = await db.Dealers.ToListAsync();
        var dealerDictByCode = dealers.GroupBy(d => d.Code, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var areas = await db.Areas.ToListAsync();
        var areaDictByCode = areas.GroupBy(a => a.Code, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);

        (string? AreaName, string? ProvinceName) ResolveLocation(string? pCode, string? pType)
        {
            if (string.IsNullOrWhiteSpace(pCode)) return (null, null);
            if (cusDictByCode.TryGetValue(pCode, out var c))
            {
                string? aName = !string.IsNullOrEmpty(c.AreaCode) && areaDictByCode.TryGetValue(c.AreaCode, out var an) ? an : c.AreaCode;
                return (aName, c.Province);
            }
            if (dealerDictByCode.TryGetValue(pCode, out var d))
            {
                return (null, d.ProvinceCode);
            }
            return (null, null);
        }

        var rawItems = new List<SummaryInOutPartnerPivotItem>();
        int seq = 1;

        // 1. Giao dịch từ StockDoc (nhập mua, xuất bán, trả hàng)
        var docs = await db.Docs
            .Where(d => d.Status == DocStatus.Posted && d.Date >= start && d.Date <= end)
            .Include(d => d.FromWarehouse)
            .Include(d => d.ToWarehouse)
            .Include(d => d.Lines)
            .OrderBy(d => d.Date)
            .ThenBy(d => d.Id)
            .ToListAsync();

        foreach (var doc in docs)
        {
            if (doc.Type == DocType.In)
            {
                int wId = doc.ToWarehouseId ?? 0;
                if (warehouseId.HasValue && wId != warehouseId.Value) continue;
                string wDocName = doc.ToWarehouse?.Name ?? (whDict.TryGetValue(wId, out var wh) ? wh.Name : "Kho nhận");

                string pCode = !string.IsNullOrWhiteSpace(doc.SupplierCode) ? doc.SupplierCode.Trim() : "NCC-KHAC";
                string pName = !string.IsNullOrWhiteSpace(doc.SupplierName) ? doc.SupplierName.Trim() : (pCode == "NCC-KHAC" ? "Nhà cung cấp / Đối tác giao" : pCode);
                string pType = "Nhà cung cấp";
                string inOutType = "Nhập mua NCC";

                if (!string.IsNullOrWhiteSpace(doc.RefNo) && doc.RefNo.StartsWith("THKH", StringComparison.OrdinalIgnoreCase))
                {
                    pType = "Khách hàng / Đại lý";
                    inOutType = "Nhập khách trả hàng";
                    if (!string.IsNullOrWhiteSpace(doc.CustomerCode)) { pCode = doc.CustomerCode; pName = doc.CustomerName ?? pCode; }
                }

                var (areaName, provName) = ResolveLocation(pCode, pType);

                foreach (var line in doc.Lines)
                {
                    if (!prodDict.TryGetValue(line.ProductId, out var prod)) continue;
                    decimal up = prod.CostPrice;
                    decimal amt = line.Quantity * up;
                    string? grpName = !string.IsNullOrEmpty(prod.ProductGrpCode) && grpDict.TryGetValue(prod.ProductGrpCode, out var g) ? g : prod.ProductGrpCode;

                    rawItems.Add(new SummaryInOutPartnerPivotItem(
                        seq++,
                        doc.Code,
                        doc.Date,
                        wId,
                        wDocName,
                        pCode,
                        pName,
                        pType,
                        areaName,
                        provName,
                        prod.Id,
                        prod.Code,
                        prod.Name,
                        prod.ProductGrpCode,
                        grpName,
                        prod.Uom,
                        "IN",
                        "Nhập kho",
                        inOutType,
                        line.Quantity,
                        up,
                        amt,
                        doc.RefNo,
                        doc.CreatedBy,
                        doc.Note,
                        $"/Doc/Detail/{doc.Id}"
                    ));
                }
            }
            else if (doc.Type == DocType.Out)
            {
                int wId = doc.FromWarehouseId ?? 0;
                if (warehouseId.HasValue && wId != warehouseId.Value) continue;
                string wDocName = doc.FromWarehouse?.Name ?? (whDict.TryGetValue(wId, out var wh) ? wh.Name : "Kho xuất");

                string pCode;
                string pName;
                string pType;
                string inOutType;

                if (!string.IsNullOrWhiteSpace(doc.CustomerCode))
                {
                    pCode = doc.CustomerCode.Trim();
                    pName = doc.CustomerName?.Trim() ?? pCode;
                    pType = "Khách hàng / Đại lý";
                    inOutType = "Xuất bán khách hàng";
                }
                else if (!string.IsNullOrWhiteSpace(doc.DepartmentCode))
                {
                    pCode = doc.DepartmentCode.Trim();
                    pName = doc.DepartmentName?.Trim() ?? pCode;
                    pType = "Nội bộ";
                    inOutType = "Xuất cấp phát nội bộ";
                }
                else if (!string.IsNullOrWhiteSpace(doc.SupplierCode) || (!string.IsNullOrWhiteSpace(doc.RefNo) && doc.RefNo.StartsWith("THNCC", StringComparison.OrdinalIgnoreCase)))
                {
                    pCode = doc.SupplierCode?.Trim() ?? "SUP-RET";
                    pName = doc.SupplierName?.Trim() ?? "Nhà cung cấp nhận trả";
                    pType = "Nhà cung cấp";
                    inOutType = "Xuất trả NCC";
                }
                else
                {
                    pCode = "KH-KHAC";
                    pName = "Khách hàng / Đối tác nhận";
                    pType = "Khách hàng / Đại lý";
                    inOutType = "Xuất kho chung";
                }

                var (areaName, provName) = ResolveLocation(pCode, pType);

                foreach (var line in doc.Lines)
                {
                    if (!prodDict.TryGetValue(line.ProductId, out var prod)) continue;
                    decimal up = prod.CostPrice;
                    decimal amt = line.Quantity * up;
                    string? grpName = !string.IsNullOrEmpty(prod.ProductGrpCode) && grpDict.TryGetValue(prod.ProductGrpCode, out var g) ? g : prod.ProductGrpCode;

                    rawItems.Add(new SummaryInOutPartnerPivotItem(
                        seq++,
                        doc.Code,
                        doc.Date,
                        wId,
                        wDocName,
                        pCode,
                        pName,
                        pType,
                        areaName,
                        provName,
                        prod.Id,
                        prod.Code,
                        prod.Name,
                        prod.ProductGrpCode,
                        grpName,
                        prod.Uom,
                        "OUT",
                        "Xuất kho",
                        inOutType,
                        line.Quantity,
                        up,
                        amt,
                        doc.RefNo,
                        doc.CreatedBy,
                        doc.Note,
                        $"/Doc/Detail/{doc.Id}"
                    ));
                }
            }
        }

        // 2. Giao dịch từ InventoryInFG (Nhập thành phẩm sản xuất)
        var inFGs = await db.InventoryInFGs
            .Where(f => f.Status != InvInFGStatus.Cancelled && f.Date >= start && f.Date <= end &&
                        (!warehouseId.HasValue || f.WarehouseId == warehouseId.Value))
            .Include(f => f.Warehouse)
            .Include(f => f.Lines).ThenInclude(l => l.Product)
            .ToListAsync();

        foreach (var fg in inFGs)
        {
            string pCode = "XUONG-SX";
            string pName = !string.IsNullOrWhiteSpace(fg.WorkshopName) ? fg.WorkshopName : "Xưởng sản xuất";
            string pType = "Phân xưởng SX";
            string wName = fg.Warehouse?.Name ?? "Kho thành phẩm";

            foreach (var line in fg.Lines)
            {
                if (line.ActualQty <= 0) continue;
                var prod = line.Product ?? (prodDict.TryGetValue(line.ProductId, out var p) ? p : null);
                if (prod == null) continue;

                decimal up = prod.CostPrice;
                decimal amt = line.ActualQty * up;
                string? grpName = !string.IsNullOrEmpty(prod.ProductGrpCode) && grpDict.TryGetValue(prod.ProductGrpCode, out var g) ? g : prod.ProductGrpCode;

                rawItems.Add(new SummaryInOutPartnerPivotItem(
                    seq++,
                    fg.Code,
                    fg.Date,
                    fg.WarehouseId,
                    wName,
                    pCode,
                    pName,
                    pType,
                    "Nhà máy / Xưởng",
                    null,
                    prod.Id,
                    prod.Code,
                    prod.Name,
                    prod.ProductGrpCode,
                    grpName,
                    prod.Uom,
                    "IN",
                    "Nhập kho",
                    "Nhập thành phẩm SX",
                    line.ActualQty,
                    up,
                    amt,
                    fg.WorkOrderNo,
                    fg.CreatedBy,
                    fg.Remark,
                    $"/InventoryInFG/Detail/{fg.Id}"
                ));
            }
        }

        // 3. Giao dịch từ InventoryOutFG (Xuất thành phẩm giao khách hàng)
        var outFGs = await db.InventoryOutFGs
            .Where(f => f.Status != InvOutFGStatus.Cancelled && f.Date >= start && f.Date <= end &&
                        (!warehouseId.HasValue || f.WarehouseId == warehouseId.Value))
            .Include(f => f.Warehouse)
            .Include(f => f.Lines).ThenInclude(l => l.Product)
            .ToListAsync();

        foreach (var fg in outFGs)
        {
            string pCode = fg.AgentCode ?? "KH-TP";
            string pName = !string.IsNullOrWhiteSpace(fg.CustomerName) ? fg.CustomerName : "Khách hàng dự án";
            string pType = "Khách hàng / Đại lý";
            string wName = fg.Warehouse?.Name ?? "Kho xuất TP";
            var (areaName, provName) = ResolveLocation(pCode, pType);

            foreach (var line in fg.Lines)
            {
                if (line.Qty <= 0) continue;
                var prod = line.Product ?? (prodDict.TryGetValue(line.ProductId, out var p) ? p : null);
                if (prod == null) continue;

                decimal up = prod.CostPrice;
                decimal amt = line.Qty * up;
                string? grpName = !string.IsNullOrEmpty(prod.ProductGrpCode) && grpDict.TryGetValue(prod.ProductGrpCode, out var g) ? g : prod.ProductGrpCode;

                rawItems.Add(new SummaryInOutPartnerPivotItem(
                    seq++,
                    fg.Code,
                    fg.Date,
                    fg.WarehouseId,
                    wName,
                    pCode,
                    pName,
                    pType,
                    areaName,
                    provName,
                    prod.Id,
                    prod.Code,
                    prod.Name,
                    prod.ProductGrpCode,
                    grpName,
                    prod.Uom,
                    "OUT",
                    "Xuất kho",
                    "Xuất thành phẩm",
                    line.Qty,
                    up,
                    amt,
                    fg.OrderNo,
                    fg.CreatedBy,
                    fg.Remark,
                    $"/InventoryOutFG/Detail/{fg.Id}"
                ));
            }
        }

        // 4. Áp dụng các bộ lọc (Filters)
        var filtered = rawItems.AsEnumerable();

        if (warehouseId.HasValue)
            filtered = filtered.Where(x => x.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrWhiteSpace(partnerCode))
        {
            var pc = partnerCode.Trim().ToLowerInvariant();
            filtered = filtered.Where(x => x.PartnerCode.ToLowerInvariant().Contains(pc) || x.PartnerName.ToLowerInvariant().Contains(pc));
        }

        if (!string.IsNullOrWhiteSpace(productGrpCode))
        {
            var gc = productGrpCode.Trim().ToLowerInvariant();
            filtered = filtered.Where(x => x.ProductGrpCode != null && x.ProductGrpCode.ToLowerInvariant() == gc);
        }

        if (productId.HasValue)
            filtered = filtered.Where(x => x.ProductId == productId.Value);

        if (!string.IsNullOrWhiteSpace(actionType) && !actionType.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            var act = actionType.Trim().ToUpperInvariant();
            filtered = filtered.Where(x => x.ActionType == act);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            filtered = filtered.Where(x =>
                x.PartnerCode.ToLowerInvariant().Contains(kw) ||
                x.PartnerName.ToLowerInvariant().Contains(kw) ||
                x.DocNo.ToLowerInvariant().Contains(kw) ||
                x.ProductCode.ToLowerInvariant().Contains(kw) ||
                x.ProductName.ToLowerInvariant().Contains(kw) ||
                (x.RefNo != null && x.RefNo.ToLowerInvariant().Contains(kw)) ||
                (x.WarehouseName != null && x.WarehouseName.ToLowerInvariant().Contains(kw))
            );
        }

        var detailList = filtered.OrderByDescending(x => x.DocDate).ThenByDescending(x => x.Stt).ToList();

        // 5. Xây dựng danh sách Pivot Rows (Partner x Product)
        var pivotRows = detailList
            .GroupBy(x => new { x.PartnerCode, x.PartnerName, x.PartnerType, x.AreaName, x.ProvinceName, x.ProductId, x.ProductCode, x.ProductName, x.ProductGrpCode, x.ProductGrpName, x.Uom })
            .Select(g =>
            {
                int inQ = g.Where(i => i.ActionType == "IN").Sum(i => i.Quantity);
                decimal inA = g.Where(i => i.ActionType == "IN").Sum(i => i.Amount);
                int outQ = g.Where(i => i.ActionType == "OUT").Sum(i => i.Quantity);
                decimal outA = g.Where(i => i.ActionType == "OUT").Sum(i => i.Amount);
                int netQ = inQ - outQ;
                decimal netA = inA - outA;
                int tx = g.Select(i => i.DocNo).Distinct().Count();
                DateTime last = g.Max(i => i.DocDate);

                return new SummaryInOutPartnerPivotRow(
                    g.Key.PartnerCode,
                    g.Key.PartnerName,
                    g.Key.PartnerType,
                    g.Key.AreaName,
                    g.Key.ProvinceName,
                    g.Key.ProductId,
                    g.Key.ProductCode,
                    g.Key.ProductName,
                    g.Key.ProductGrpCode,
                    g.Key.ProductGrpName,
                    g.Key.Uom,
                    inQ,
                    inA,
                    outQ,
                    outA,
                    netQ,
                    netA,
                    tx,
                    last
                );
            })
            .OrderByDescending(r => r.TotalInQty + r.TotalOutQty)
            .ThenBy(r => r.PartnerName)
            .ThenBy(r => r.ProductCode)
            .ToList();

        // 6. Xây dựng nhóm Đối tác (Partner Groups)
        int grandTotalVolume = pivotRows.Sum(r => r.TotalInQty + r.TotalOutQty);
        var partnerGroups = pivotRows
            .GroupBy(r => new { r.PartnerCode, r.PartnerName, r.PartnerType, r.AreaName, r.ProvinceName })
            .Select(g =>
            {
                int grpInQ = g.Sum(x => x.TotalInQty);
                decimal grpInA = g.Sum(x => x.TotalInAmount);
                int grpOutQ = g.Sum(x => x.TotalOutQty);
                decimal grpOutA = g.Sum(x => x.TotalOutAmount);
                int grpNetQ = grpInQ - grpOutQ;
                decimal grpNetA = grpInA - grpOutA;
                int prodCount = g.Select(x => x.ProductId).Distinct().Count();
                int txCount = g.Sum(x => x.TxCount);
                int vol = grpInQ + grpOutQ;
                double share = grandTotalVolume > 0 ? Math.Round((double)vol / grandTotalVolume * 100.0, 2) : 0.0;

                return new SummaryInOutPartnerGroupRow(
                    g.Key.PartnerCode,
                    g.Key.PartnerName,
                    g.Key.PartnerType,
                    g.Key.AreaName,
                    g.Key.ProvinceName,
                    grpInQ,
                    grpInA,
                    grpOutQ,
                    grpOutA,
                    grpNetQ,
                    grpNetA,
                    prodCount,
                    txCount,
                    share,
                    g.OrderByDescending(p => p.TotalInQty + p.TotalOutQty).ToList()
                );
            })
            .OrderByDescending(g => g.TotalInQty + g.TotalOutQty)
            .ThenBy(g => g.PartnerName)
            .ToList();

        // 7. Tính 4 thẻ KPI
        int totalPartners = partnerGroups.Count;
        int totalInQty = detailList.Where(x => x.ActionType == "IN").Sum(x => x.Quantity);
        decimal totalInAmount = detailList.Where(x => x.ActionType == "IN").Sum(x => x.Amount);
        int totalOutQty = detailList.Where(x => x.ActionType == "OUT").Sum(x => x.Quantity);
        decimal totalOutAmount = detailList.Where(x => x.ActionType == "OUT").Sum(x => x.Amount);
        int totalNetQty = totalInQty - totalOutQty;
        decimal totalNetAmount = totalInAmount - totalOutAmount;
        int totalTxCount = detailList.Select(x => x.DocNo).Distinct().Count();

        return new SummaryInOutPartnerPivotReport(
            warehouseId,
            whName,
            partnerCode,
            productGrpCode,
            productId,
            actionType,
            start,
            end.Date,
            keyword,
            totalPartners,
            totalInQty,
            totalInAmount,
            totalOutQty,
            totalOutAmount,
            totalNetQty,
            totalNetAmount,
            totalTxCount,
            partnerGroups,
            pivotRows,
            detailList
        );
    }

    private static string Prefix(DocType t) => t switch { DocType.In => "PN", DocType.Out => "PX", DocType.Transfer => "PC", _ => "PK" };
}

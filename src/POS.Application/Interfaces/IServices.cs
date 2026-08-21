using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using POS.Application.DTOs;
using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken ct = default);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<bool> ResetPasswordAsync(string userId, ResetPasswordRequest request, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<EmployerDashboardDto> GetEmployerDashboardAsync(CancellationToken ct = default);
    Task<WorkerDashboardDto> GetWorkerDashboardAsync(string workerId, CancellationToken ct = default);
}

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(ProductFilterRequest request, CancellationToken ct = default);
    Task<ProductDto> GetProductByIdAsync(string id, CancellationToken ct = default);
    Task<ProductDto?> FindBySkuOrBarcodeAsync(string code, CancellationToken ct = default);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, string userId, string userName, CancellationToken ct = default);
    Task<ProductDto> UpdateProductAsync(string id, UpdateProductRequest request, string userId, string userName, CancellationToken ct = default);
    Task<bool> DeleteProductAsync(string id, string userId, string userName, CancellationToken ct = default);
    Task<List<ProductDto>> GetLowStockProductsAsync(CancellationToken ct = default);
}

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllCategoriesAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<CategoryDto> GetCategoryByIdAsync(string id, CancellationToken ct = default);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, string userId, string userName, CancellationToken ct = default);
    Task<CategoryDto> UpdateCategoryAsync(string id, UpdateCategoryRequest request, string userId, string userName, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(string id, string userId, string userName, CancellationToken ct = default);
}

public interface ISaleService
{
    Task<SaleDto> ProcessSaleAsync(CreateSaleRequest request, string cashierId, string cashierName, decimal workerMaxDiscountPercent, CancellationToken ct = default);
    Task<PagedResult<SaleDto>> GetSalesAsync(SaleFilterRequest request, CancellationToken ct = default);
    Task<SaleDto> GetSaleByIdAsync(string id, CancellationToken ct = default);
    Task<SaleDto> GetSaleByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default);
    Task<bool> CancelSaleAsync(string id, string reason, string userId, string userName, CancellationToken ct = default);
}

public interface IInventoryService
{
    Task<PagedResult<StockTransactionDto>> GetTransactionsAsync(string? productId, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<StockTransactionDto> AdjustStockAsync(StockAdjustmentRequest request, string userId, string userName, CancellationToken ct = default);
}

public interface IPurchaseService
{
    Task<PurchaseDto> CreatePurchaseAsync(CreatePurchaseRequest request, string userId, string userName, CancellationToken ct = default);
    Task<PagedResult<PurchaseDto>> GetPurchasesAsync(PurchaseFilterRequest request, CancellationToken ct = default);
    Task<PurchaseDto> GetPurchaseByIdAsync(string id, CancellationToken ct = default);
}

public interface ISupplierService
{
    Task<List<SupplierDto>> GetAllSuppliersAsync(CancellationToken ct = default);
    Task<SupplierDto> GetSupplierByIdAsync(string id, CancellationToken ct = default);
    Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, string userId, string userName, CancellationToken ct = default);
    Task<SupplierDto> UpdateSupplierAsync(string id, UpdateSupplierRequest request, string userId, string userName, CancellationToken ct = default);
    Task<SupplierPaymentDto> RecordPaymentAsync(SupplierPaymentRequest request, string userId, string userName, CancellationToken ct = default);
    Task<List<SupplierPaymentDto>> GetPaymentHistoryAsync(string supplierId, CancellationToken ct = default);
}

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllCustomersAsync(string? search = null, CancellationToken ct = default);
    Task<CustomerDto> GetCustomerByIdAsync(string id, CancellationToken ct = default);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, string userId, string userName, CancellationToken ct = default);
    Task<CustomerDto> UpdateCustomerAsync(string id, UpdateCustomerRequest request, string userId, string userName, CancellationToken ct = default);
    Task<CustomerPaymentDto> RecordPaymentAsync(CustomerPaymentRequest request, string userId, string userName, CancellationToken ct = default);
    Task<List<CustomerPaymentDto>> GetPaymentHistoryAsync(string customerId, CancellationToken ct = default);
}

public interface IDueService
{
    Task<DueSummaryDto> GetDueSummaryAsync(CancellationToken ct = default);
}

public interface IExpenseService
{
    Task<List<ExpenseDto>> GetExpensesAsync(DateTime? from, DateTime? to, string? categoryId, CancellationToken ct = default);
    Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request, string userId, string userName, CancellationToken ct = default);
    Task<bool> VoidExpenseAsync(string id, string userId, string userName, CancellationToken ct = default);
    Task<List<ExpenseCategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<ExpenseCategoryDto> CreateCategoryAsync(string name, string description, CancellationToken ct = default);
}

public interface IReturnService
{
    Task<ReturnDto> ProcessReturnAsync(CreateReturnRequest request, string cashierId, string cashierName, CancellationToken ct = default);
    Task<PagedResult<ReturnDto>> GetReturnsAsync(int pageNumber, int pageSize, CancellationToken ct = default);
    Task<ReturnDto> GetReturnByIdAsync(string id, CancellationToken ct = default);
}

public interface IDiscountService
{
    Task<List<DiscountRuleDto>> GetAllRulesAsync(CancellationToken ct = default);
    Task<DiscountRuleDto> CreateRuleAsync(CreateDiscountRuleRequest request, string userId, string userName, CancellationToken ct = default);
    Task<DiscountRuleDto> UpdateRuleAsync(string id, CreateDiscountRuleRequest request, string userId, string userName, CancellationToken ct = default);
    Task<bool> DeleteRuleAsync(string id, string userId, string userName, CancellationToken ct = default);
    Task ValidateWorkerDiscountAsync(decimal attemptedPercent, decimal maxAllowedPercent);
}

public interface IWorkerService
{
    Task<List<UserDto>> GetAllWorkersAsync(CancellationToken ct = default);
    Task<UserDto> GetWorkerByIdAsync(string id, CancellationToken ct = default);
    Task<UserDto> CreateWorkerAsync(CreateUserRequest request, string adminUserId, string adminUserName, CancellationToken ct = default);
    Task<UserDto> UpdateWorkerAsync(string id, UpdateUserRequest request, string adminUserId, string adminUserName, CancellationToken ct = default);
    Task<bool> DeleteWorkerAsync(string id, string adminUserId, string adminUserName, CancellationToken ct = default);
    Task<bool> ResetWorkerPasswordAsync(string id, string newPassword, string adminUserId, string adminUserName, CancellationToken ct = default);
    Task<WorkerPerformanceReportDto> GetWorkerPerformanceAsync(string workerId, DateTime? from, DateTime? to, CancellationToken ct = default);
}

public interface IReportService
{
    Task<SalesReportDto> GetSalesReportAsync(ReportFilterRequest filter, CancellationToken ct = default);
    Task<PurchaseReportDto> GetPurchaseReportAsync(ReportFilterRequest filter, CancellationToken ct = default);
    Task<ProfitLossReportDto> GetProfitLossReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<ExpenseReportDto> GetExpenseReportAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<WorkerPerformanceReportDto>> GetAllWorkersPerformanceAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public interface ICashService
{
    Task<CashSessionDto?> GetCurrentSessionAsync(string cashierId, CancellationToken ct = default);
    Task<CashSessionDto> OpenSessionAsync(string cashierId, string cashierName, OpenCashSessionRequest request, CancellationToken ct = default);
    Task<CashSessionDto> CloseSessionAsync(string sessionId, CloseCashSessionRequest request, CancellationToken ct = default);
    Task<CashSessionDto> AdjustCashAsync(string sessionId, CashAdjustmentRequest request, string cashierName, CancellationToken ct = default);
    Task<List<CashSessionDto>> GetSessionHistoryAsync(DateTime? from, DateTime? to, string? cashierId, CancellationToken ct = default);
}

public interface IInvoiceService
{
    Task<string> GenerateReceiptTextAsync(SaleDto sale, BusinessSettingsDto settings);
    Task<string> GenerateA4HtmlAsync(SaleDto sale, BusinessSettingsDto settings);
}

public interface IBarcodeService
{
    string GenerateBarcodeSvg(string code, string format = "CODE128");
}

public interface ISettingsService
{
    Task<BusinessSettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task<BusinessSettingsDto> UpdateSettingsAsync(BusinessSettingsDto request, string userId, string userName, CancellationToken ct = default);
}

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<UserDto> GetUserByIdAsync(string id, CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, string adminUserId, string adminUserName, CancellationToken ct = default);
    Task<UserDto> UpdateUserAsync(string id, UpdateUserRequest request, string adminUserId, string adminUserName, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(string id, string adminUserId, string adminUserName, CancellationToken ct = default);
    Task<bool> ToggleUserStatusAsync(string id, bool isActive, string adminUserId, string adminUserName, CancellationToken ct = default);
}

public interface IActivityLogService
{
    Task LogAsync(string userId, string userName, string action, ActivityModule module, string description, string? ipAddress = null, CancellationToken ct = default);
    Task<PagedResult<ActivityLogDto>> GetLogsAsync(int pageNumber, int pageSize, ActivityModule? module = null, string? searchTerm = null, CancellationToken ct = default);
}

public interface IBackupService
{
    Task<BackupDto> CreateBackupAsync(string targetFolder, string userId, string userName, CancellationToken ct = default);
    Task<bool> RestoreBackupAsync(string backupId, string userId, string userName, CancellationToken ct = default);
    Task<List<BackupDto>> GetBackupsAsync(CancellationToken ct = default);
}

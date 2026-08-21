using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using POS.Application.DTOs;

namespace POS.Desktop.Services;

public interface IApiClient
{
    // Auth
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request);
    Task<ApiResponse<UserDto>> GetCurrentUserAsync();

    // Dashboard
    Task<ApiResponse<EmployerDashboardDto>> GetEmployerDashboardAsync();
    Task<ApiResponse<WorkerDashboardDto>> GetWorkerDashboardAsync();

    // Products & Categories
    Task<ApiResponse<PagedResult<ProductDto>>> GetProductsAsync(ProductFilterRequest request);
    Task<ApiResponse<ProductDto>> GetProductByIdAsync(string id);
    Task<ApiResponse<ProductDto>> FindProductByCodeAsync(string code);
    Task<ApiResponse<List<ProductDto>>> GetLowStockProductsAsync();
    Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductRequest request);
    Task<ApiResponse<ProductDto>> UpdateProductAsync(string id, UpdateProductRequest request);
    Task<ApiResponse<bool>> DeleteProductAsync(string id);

    Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync(bool includeInactive = false);
    Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request);
    Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(string id, UpdateCategoryRequest request);
    Task<ApiResponse<bool>> DeleteCategoryAsync(string id);

    // Sales & POS
    Task<ApiResponse<SaleDto>> ProcessSaleAsync(CreateSaleRequest request);
    Task<ApiResponse<PagedResult<SaleDto>>> GetSalesAsync(SaleFilterRequest request);
    Task<ApiResponse<SaleDto>> GetSaleByIdAsync(string id);
    Task<ApiResponse<SaleDto>> GetSaleByInvoiceAsync(string invoiceNumber);
    Task<ApiResponse<bool>> CancelSaleAsync(string id, string reason);

    // Purchases & Suppliers
    Task<ApiResponse<PurchaseDto>> CreatePurchaseAsync(CreatePurchaseRequest request);
    Task<ApiResponse<PagedResult<PurchaseDto>>> GetPurchasesAsync(PurchaseFilterRequest request);
    Task<ApiResponse<PurchaseDto>> GetPurchaseByIdAsync(string id);

    Task<ApiResponse<List<SupplierDto>>> GetSuppliersAsync();
    Task<ApiResponse<SupplierDto>> CreateSupplierAsync(CreateSupplierRequest request);
    Task<ApiResponse<SupplierDto>> UpdateSupplierAsync(string id, UpdateSupplierRequest request);
    Task<ApiResponse<SupplierPaymentDto>> RecordSupplierPaymentAsync(SupplierPaymentRequest request);
    Task<ApiResponse<List<SupplierPaymentDto>>> GetSupplierPaymentsAsync(string supplierId);

    // Customers & Due
    Task<ApiResponse<List<CustomerDto>>> GetCustomersAsync(string? search = null);
    Task<ApiResponse<CustomerDto>> CreateCustomerAsync(CreateCustomerRequest request);
    Task<ApiResponse<CustomerDto>> UpdateCustomerAsync(string id, UpdateCustomerRequest request);
    Task<ApiResponse<CustomerPaymentDto>> RecordCustomerPaymentAsync(CustomerPaymentRequest request);
    Task<ApiResponse<List<CustomerPaymentDto>>> GetCustomerPaymentsAsync(string customerId);
    Task<ApiResponse<DueSummaryDto>> GetDueSummaryAsync();

    // Expenses & Returns & Discounts
    Task<ApiResponse<List<ExpenseDto>>> GetExpensesAsync(DateTime? from = null, DateTime? to = null, string? categoryId = null);
    Task<ApiResponse<ExpenseDto>> CreateExpenseAsync(CreateExpenseRequest request);
    Task<ApiResponse<bool>> VoidExpenseAsync(string id);
    Task<ApiResponse<List<ExpenseCategoryDto>>> GetExpenseCategoriesAsync();
    Task<ApiResponse<ExpenseCategoryDto>> CreateExpenseCategoryAsync(string name, string description);

    Task<ApiResponse<ReturnDto>> ProcessReturnAsync(CreateReturnRequest request);
    Task<ApiResponse<PagedResult<ReturnDto>>> GetReturnsAsync(int page = 1, int pageSize = 20);

    Task<ApiResponse<List<DiscountRuleDto>>> GetDiscountsAsync();
    Task<ApiResponse<DiscountRuleDto>> CreateDiscountAsync(CreateDiscountRuleRequest request);
    Task<ApiResponse<DiscountRuleDto>> UpdateDiscountAsync(string id, CreateDiscountRuleRequest request);
    Task<ApiResponse<bool>> DeleteDiscountAsync(string id);

    // Cash Management
    Task<ApiResponse<CashSessionDto?>> GetCurrentCashSessionAsync();
    Task<ApiResponse<CashSessionDto>> OpenCashSessionAsync(OpenCashSessionRequest request);
    Task<ApiResponse<CashSessionDto>> CloseCashSessionAsync(string sessionId, CloseCashSessionRequest request);
    Task<ApiResponse<CashSessionDto>> AdjustCashAsync(string sessionId, CashAdjustmentRequest request);
    Task<ApiResponse<List<CashSessionDto>>> GetCashHistoryAsync(DateTime? from = null, DateTime? to = null);

    // Reports
    Task<ApiResponse<SalesReportDto>> GetSalesReportAsync(ReportFilterRequest filter);
    Task<ApiResponse<PurchaseReportDto>> GetPurchaseReportAsync(ReportFilterRequest filter);
    Task<ApiResponse<ProfitLossReportDto>> GetProfitLossReportAsync(DateTime from, DateTime to);
    Task<ApiResponse<ExpenseReportDto>> GetExpenseReportAsync(DateTime from, DateTime to);
    Task<ApiResponse<List<WorkerPerformanceReportDto>>> GetWorkersPerformanceAsync(DateTime from, DateTime to);

    // Invoices & Barcodes & Settings
    Task<ApiResponse<string>> GetReceiptTextAsync(string saleId);
    Task<ApiResponse<string>> GetA4InvoiceHtmlAsync(string saleId);
    Task<ApiResponse<string>> GenerateBarcodeSvgAsync(string code);
    Task<ApiResponse<BusinessSettingsDto>> GetSettingsAsync();
    Task<ApiResponse<BusinessSettingsDto>> UpdateSettingsAsync(BusinessSettingsDto settings);

    // Workers & Users
    Task<ApiResponse<List<UserDto>>> GetWorkersAsync();
    Task<ApiResponse<UserDto>> CreateWorkerAsync(CreateUserRequest request);
    Task<ApiResponse<UserDto>> UpdateWorkerAsync(string id, UpdateUserRequest request);
    Task<ApiResponse<bool>> DeleteWorkerAsync(string id);
    Task<ApiResponse<bool>> ResetWorkerPasswordAsync(string id, string newPassword);
    Task<ApiResponse<WorkerPerformanceReportDto>> GetWorkerPerformanceAsync(string id);

    Task<ApiResponse<List<UserDto>>> GetUsersAsync();
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<ApiResponse<UserDto>> UpdateUserAsync(string id, UpdateUserRequest request);
    Task<ApiResponse<bool>> DeleteUserAsync(string id);
    Task<ApiResponse<bool>> ToggleUserStatusAsync(string id, bool isActive);

    // Activity & Backup
    Task<ApiResponse<PagedResult<ActivityLogDto>>> GetActivityLogsAsync(int page = 1, int pageSize = 50, string? search = null);
    Task<ApiResponse<BackupDto>> CreateBackupAsync(string? targetFolder = null);
    Task<ApiResponse<bool>> RestoreBackupAsync(string backupId);
    Task<ApiResponse<List<BackupDto>>> GetBackupsAsync();
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAuthSession _session;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient, IAuthSession session)
    {
        _httpClient = httpClient;
        _session = session;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private void SetAuthHeader()
    {
        if (!string.IsNullOrEmpty(_session.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    private async Task<ApiResponse<T>> SendAsync<T>(HttpRequestMessage req)
    {
        SetAuthHeader();
        try
        {
            using var response = await _httpClient.SendAsync(req);
            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                return response.IsSuccessStatusCode
                    ? ApiResponse<T>.Ok(default!)
                    : ApiResponse<T>.Fail($"Server returned {response.StatusCode}");
            }

            var apiRes = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
            return apiRes ?? ApiResponse<T>.Fail("Empty response received from server.");
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.Fail($"Connection error: {ex.Message}");
        }
    }

    // Auth
    public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest req) =>
        SendAsync<LoginResponse>(new HttpRequestMessage(HttpMethod.Post, "api/auth/login") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordRequest req) =>
        SendAsync<bool>(new HttpRequestMessage(HttpMethod.Post, "api/auth/change-password") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<UserDto>> GetCurrentUserAsync() =>
        SendAsync<UserDto>(new HttpRequestMessage(HttpMethod.Get, "api/auth/me"));

    // Dashboard
    public Task<ApiResponse<EmployerDashboardDto>> GetEmployerDashboardAsync() =>
        SendAsync<EmployerDashboardDto>(new HttpRequestMessage(HttpMethod.Get, "api/dashboard/employer"));

    public Task<ApiResponse<WorkerDashboardDto>> GetWorkerDashboardAsync() =>
        SendAsync<WorkerDashboardDto>(new HttpRequestMessage(HttpMethod.Get, "api/dashboard/worker"));

    // Products & Categories
    public Task<ApiResponse<PagedResult<ProductDto>>> GetProductsAsync(ProductFilterRequest req)
    {
        var uri = $"api/products?pageNumber={req.PageNumber}&pageSize={req.PageSize}&searchTerm={Uri.EscapeDataString(req.SearchTerm ?? "")}&categoryId={req.CategoryId}&lowStockOnly={req.LowStockOnly}&outOfStockOnly={req.OutOfStockOnly}&activeOnly={req.ActiveOnly}";
        return SendAsync<PagedResult<ProductDto>>(new HttpRequestMessage(HttpMethod.Get, uri));
    }

    public Task<ApiResponse<ProductDto>> GetProductByIdAsync(string id) =>
        SendAsync<ProductDto>(new HttpRequestMessage(HttpMethod.Get, $"api/products/{id}"));

    public Task<ApiResponse<ProductDto>> FindProductByCodeAsync(string code) =>
        SendAsync<ProductDto>(new HttpRequestMessage(HttpMethod.Get, $"api/products/code/{Uri.EscapeDataString(code)}"));

    public Task<ApiResponse<List<ProductDto>>> GetLowStockProductsAsync() =>
        SendAsync<List<ProductDto>>(new HttpRequestMessage(HttpMethod.Get, "api/products/low-stock"));

    public Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductRequest req) =>
        SendAsync<ProductDto>(new HttpRequestMessage(HttpMethod.Post, "api/products") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<ProductDto>> UpdateProductAsync(string id, UpdateProductRequest req) =>
        SendAsync<ProductDto>(new HttpRequestMessage(HttpMethod.Put, $"api/products/{id}") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<bool>> DeleteProductAsync(string id) =>
        SendAsync<bool>(new HttpRequestMessage(HttpMethod.Delete, $"api/products/{id}"));

    public Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync(bool includeInactive = false) =>
        SendAsync<List<CategoryDto>>(new HttpRequestMessage(HttpMethod.Get, $"api/categories?includeInactive={includeInactive}"));

    public Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest req) =>
        SendAsync<CategoryDto>(new HttpRequestMessage(HttpMethod.Post, "api/categories") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(string id, UpdateCategoryRequest req) =>
        SendAsync<CategoryDto>(new HttpRequestMessage(HttpMethod.Put, $"api/categories/{id}") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<bool>> DeleteCategoryAsync(string id) =>
        SendAsync<bool>(new HttpRequestMessage(HttpMethod.Delete, $"api/categories/{id}"));

    // Sales
    public Task<ApiResponse<SaleDto>> ProcessSaleAsync(CreateSaleRequest req) =>
        SendAsync<SaleDto>(new HttpRequestMessage(HttpMethod.Post, "api/sales") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<PagedResult<SaleDto>>> GetSalesAsync(SaleFilterRequest req)
    {
        var uri = $"api/sales?pageNumber={req.PageNumber}&pageSize={req.PageSize}&searchTerm={Uri.EscapeDataString(req.SearchTerm ?? "")}&customerId={req.CustomerId}&cashierId={req.CashierId}&paymentStatus={req.PaymentStatus}&saleStatus={req.SaleStatus}";
        return SendAsync<PagedResult<SaleDto>>(new HttpRequestMessage(HttpMethod.Get, uri));
    }

    public Task<ApiResponse<SaleDto>> GetSaleByIdAsync(string id) =>
        SendAsync<SaleDto>(new HttpRequestMessage(HttpMethod.Get, $"api/sales/{id}"));

    public Task<ApiResponse<SaleDto>> GetSaleByInvoiceAsync(string inv) =>
        SendAsync<SaleDto>(new HttpRequestMessage(HttpMethod.Get, $"api/sales/invoice/{Uri.EscapeDataString(inv)}"));

    public Task<ApiResponse<bool>> CancelSaleAsync(string id, string reason) =>
        SendAsync<bool>(new HttpRequestMessage(HttpMethod.Post, $"api/sales/{id}/cancel?reason={Uri.EscapeDataString(reason)}"));

    // Purchases
    public Task<ApiResponse<PurchaseDto>> CreatePurchaseAsync(CreatePurchaseRequest req) =>
        SendAsync<PurchaseDto>(new HttpRequestMessage(HttpMethod.Post, "api/purchases") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<PagedResult<PurchaseDto>>> GetPurchasesAsync(PurchaseFilterRequest req) =>
        SendAsync<PagedResult<PurchaseDto>>(new HttpRequestMessage(HttpMethod.Get, $"api/purchases?pageNumber={req.PageNumber}&pageSize={req.PageSize}&supplierId={req.SupplierId}"));

    public Task<ApiResponse<PurchaseDto>> GetPurchaseByIdAsync(string id) =>
        SendAsync<PurchaseDto>(new HttpRequestMessage(HttpMethod.Get, $"api/purchases/{id}"));

    // Suppliers & Customers
    public Task<ApiResponse<List<SupplierDto>>> GetSuppliersAsync() =>
        SendAsync<List<SupplierDto>>(new HttpRequestMessage(HttpMethod.Get, "api/suppliers"));

    public Task<ApiResponse<SupplierDto>> CreateSupplierAsync(CreateSupplierRequest req) =>
        SendAsync<SupplierDto>(new HttpRequestMessage(HttpMethod.Post, "api/suppliers") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<SupplierDto>> UpdateSupplierAsync(string id, UpdateSupplierRequest req) =>
        SendAsync<SupplierDto>(new HttpRequestMessage(HttpMethod.Put, $"api/suppliers/{id}") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<SupplierPaymentDto>> RecordSupplierPaymentAsync(SupplierPaymentRequest req) =>
        SendAsync<SupplierPaymentDto>(new HttpRequestMessage(HttpMethod.Post, "api/suppliers/payment") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<List<SupplierPaymentDto>>> GetSupplierPaymentsAsync(string id) =>
        SendAsync<List<SupplierPaymentDto>>(new HttpRequestMessage(HttpMethod.Get, $"api/suppliers/{id}/payments"));

    public Task<ApiResponse<List<CustomerDto>>> GetCustomersAsync(string? search = null) =>
        SendAsync<List<CustomerDto>>(new HttpRequestMessage(HttpMethod.Get, $"api/customers?search={Uri.EscapeDataString(search ?? "")}"));

    public Task<ApiResponse<CustomerDto>> CreateCustomerAsync(CreateCustomerRequest req) =>
        SendAsync<CustomerDto>(new HttpRequestMessage(HttpMethod.Post, "api/customers") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<CustomerDto>> UpdateCustomerAsync(string id, UpdateCustomerRequest req) =>
        SendAsync<CustomerDto>(new HttpRequestMessage(HttpMethod.Put, $"api/customers/{id}") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<CustomerPaymentDto>> RecordCustomerPaymentAsync(CustomerPaymentRequest req) =>
        SendAsync<CustomerPaymentDto>(new HttpRequestMessage(HttpMethod.Post, "api/customers/payment") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<List<CustomerPaymentDto>>> GetCustomerPaymentsAsync(string id) =>
        SendAsync<List<CustomerPaymentDto>>(new HttpRequestMessage(HttpMethod.Get, $"api/customers/{id}/payments"));

    public Task<ApiResponse<DueSummaryDto>> GetDueSummaryAsync() =>
        SendAsync<DueSummaryDto>(new HttpRequestMessage(HttpMethod.Get, "api/due/summary"));

    // Expenses & Returns & Discounts
    public Task<ApiResponse<List<ExpenseDto>>> GetExpensesAsync(DateTime? from = null, DateTime? to = null, string? catId = null) =>
        SendAsync<List<ExpenseDto>>(new HttpRequestMessage(HttpMethod.Get, $"api/expenses?categoryId={catId}"));

    public Task<ApiResponse<ExpenseDto>> CreateExpenseAsync(CreateExpenseRequest req) =>
        SendAsync<ExpenseDto>(new HttpRequestMessage(HttpMethod.Post, "api/expenses") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<bool>> VoidExpenseAsync(string id) =>
        SendAsync<bool>(new HttpRequestMessage(HttpMethod.Post, $"api/expenses/{id}/void"));

    public Task<ApiResponse<List<ExpenseCategoryDto>>> GetExpenseCategoriesAsync() =>
        SendAsync<List<ExpenseCategoryDto>>(new HttpRequestMessage(HttpMethod.Get, "api/expenses/categories"));

    public Task<ApiResponse<ExpenseCategoryDto>> CreateExpenseCategoryAsync(string name, string desc) =>
        SendAsync<ExpenseCategoryDto>(new HttpRequestMessage(HttpMethod.Post, $"api/expenses/categories?name={Uri.EscapeDataString(name)}&description={Uri.EscapeDataString(desc)}"));

    public Task<ApiResponse<ReturnDto>> ProcessReturnAsync(CreateReturnRequest req) =>
        SendAsync<ReturnDto>(new HttpRequestMessage(HttpMethod.Post, "api/returns") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<PagedResult<ReturnDto>>> GetReturnsAsync(int page = 1, int pageSize = 20) =>
        SendAsync<PagedResult<ReturnDto>>(new HttpRequestMessage(HttpMethod.Get, $"api/returns?pageNumber={page}&pageSize={pageSize}"));

    public Task<ApiResponse<List<DiscountRuleDto>>> GetDiscountsAsync() =>
        SendAsync<List<DiscountRuleDto>>(new HttpRequestMessage(HttpMethod.Get, "api/discounts"));

    public Task<ApiResponse<DiscountRuleDto>> CreateDiscountAsync(CreateDiscountRuleRequest req) =>
        SendAsync<DiscountRuleDto>(new HttpRequestMessage(HttpMethod.Post, "api/discounts") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<DiscountRuleDto>> UpdateDiscountAsync(string id, CreateDiscountRuleRequest req) =>
        SendAsync<DiscountRuleDto>(new HttpRequestMessage(HttpMethod.Put, $"api/discounts/{id}") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<bool>> DeleteDiscountAsync(string id) =>
        SendAsync<bool>(new HttpRequestMessage(HttpMethod.Delete, $"api/discounts/{id}"));

    // Cash
    public Task<ApiResponse<CashSessionDto?>> GetCurrentCashSessionAsync() =>
        SendAsync<CashSessionDto?>(new HttpRequestMessage(HttpMethod.Get, "api/cash/current"));

    public Task<ApiResponse<CashSessionDto>> OpenCashSessionAsync(OpenCashSessionRequest req) =>
        SendAsync<CashSessionDto>(new HttpRequestMessage(HttpMethod.Post, "api/cash/open") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<CashSessionDto>> CloseCashSessionAsync(string sessionId, CloseCashSessionRequest req) =>
        SendAsync<CashSessionDto>(new HttpRequestMessage(HttpMethod.Post, $"api/cash/{sessionId}/close") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<CashSessionDto>> AdjustCashAsync(string sessionId, CashAdjustmentRequest req) =>
        SendAsync<CashSessionDto>(new HttpRequestMessage(HttpMethod.Post, $"api/cash/{sessionId}/adjust") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<List<CashSessionDto>>> GetCashHistoryAsync(DateTime? from = null, DateTime? to = null) =>
        SendAsync<List<CashSessionDto>>(new HttpRequestMessage(HttpMethod.Get, "api/cash/history"));

    // Reports
    public Task<ApiResponse<SalesReportDto>> GetSalesReportAsync(ReportFilterRequest f) =>
        SendAsync<SalesReportDto>(new HttpRequestMessage(HttpMethod.Get, $"api/reports/sales?workerId={f.WorkerId}&customerId={f.CustomerId}"));

    public Task<ApiResponse<PurchaseReportDto>> GetPurchaseReportAsync(ReportFilterRequest f) =>
        SendAsync<PurchaseReportDto>(new HttpRequestMessage(HttpMethod.Get, $"api/reports/purchases?supplierId={f.SupplierId}"));

    public Task<ApiResponse<ProfitLossReportDto>> GetProfitLossReportAsync(DateTime from, DateTime to) =>
        SendAsync<ProfitLossReportDto>(new HttpRequestMessage(HttpMethod.Get, $"api/reports/profit-loss?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"));

    public Task<ApiResponse<ExpenseReportDto>> GetExpenseReportAsync(DateTime from, DateTime to) =>
        SendAsync<ExpenseReportDto>(new HttpRequestMessage(HttpMethod.Get, $"api/reports/expenses?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"));

    public Task<ApiResponse<List<WorkerPerformanceReportDto>>> GetWorkersPerformanceAsync(DateTime from, DateTime to) =>
        SendAsync<List<WorkerPerformanceReportDto>>(new HttpRequestMessage(HttpMethod.Get, $"api/reports/workers?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"));

    // Invoices & Barcodes & Settings
    public Task<ApiResponse<string>> GetReceiptTextAsync(string saleId) =>
        SendAsync<string>(new HttpRequestMessage(HttpMethod.Get, $"api/invoices/{saleId}/receipt"));

    public Task<ApiResponse<string>> GetA4InvoiceHtmlAsync(string saleId) =>
        SendAsync<string>(new HttpRequestMessage(HttpMethod.Get, $"api/invoices/{saleId}/a4"));

    public Task<ApiResponse<string>> GenerateBarcodeSvgAsync(string code) =>
        SendAsync<string>(new HttpRequestMessage(HttpMethod.Get, $"api/barcodes/generate?code={Uri.EscapeDataString(code)}"));

    public Task<ApiResponse<BusinessSettingsDto>> GetSettingsAsync() =>
        SendAsync<BusinessSettingsDto>(new HttpRequestMessage(HttpMethod.Get, "api/settings"));

    public Task<ApiResponse<BusinessSettingsDto>> UpdateSettingsAsync(BusinessSettingsDto s) =>
        SendAsync<BusinessSettingsDto>(new HttpRequestMessage(HttpMethod.Put, "api/settings") { Content = JsonContent.Create(s) });

    // Workers & Users
    public Task<ApiResponse<List<UserDto>>> GetWorkersAsync() =>
        SendAsync<List<UserDto>>(new HttpRequestMessage(HttpMethod.Get, "api/workers"));

    public Task<ApiResponse<UserDto>> CreateWorkerAsync(CreateUserRequest req) =>
        SendAsync<UserDto>(new HttpRequestMessage(HttpMethod.Post, "api/workers") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<UserDto>> UpdateWorkerAsync(string id, UpdateUserRequest req) =>
        SendAsync<UserDto>(new HttpRequestMessage(HttpMethod.Put, $"api/workers/{id}") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<bool>> ResetWorkerPasswordAsync(string id, string newPass) =>
        SendAsync<bool>(new HttpRequestMessage(HttpMethod.Post, $"api/workers/{id}/reset-password") { Content = JsonContent.Create(new ResetPasswordRequest { NewPassword = newPass }) });

    public Task<ApiResponse<bool>> DeleteWorkerAsync(string id) =>
        SendAsync<bool>(new HttpRequestMessage(HttpMethod.Delete, $"api/workers/{id}"));

    public Task<ApiResponse<WorkerPerformanceReportDto>> GetWorkerPerformanceAsync(string id) =>
        SendAsync<WorkerPerformanceReportDto>(new HttpRequestMessage(HttpMethod.Get, $"api/workers/{id}/performance"));

    public Task<ApiResponse<List<UserDto>>> GetUsersAsync() =>
        SendAsync<List<UserDto>>(new HttpRequestMessage(HttpMethod.Get, "api/users"));

    public Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest req) =>
        SendAsync<UserDto>(new HttpRequestMessage(HttpMethod.Post, "api/users") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<UserDto>> UpdateUserAsync(string id, UpdateUserRequest req) =>
        SendAsync<UserDto>(new HttpRequestMessage(HttpMethod.Put, $"api/users/{id}") { Content = JsonContent.Create(req) });

    public Task<ApiResponse<bool>> ToggleUserStatusAsync(string id, bool isActive) =>
        SendAsync<bool>(new HttpRequestMessage(HttpMethod.Post, $"api/users/{id}/status?isActive={isActive}"));

    public Task<ApiResponse<bool>> DeleteUserAsync(string id) =>
        SendAsync<bool>(new HttpRequestMessage(HttpMethod.Delete, $"api/users/{id}"));

    // Activity & Backup
    public Task<ApiResponse<PagedResult<ActivityLogDto>>> GetActivityLogsAsync(int page = 1, int pageSize = 50, string? search = null) =>
        SendAsync<PagedResult<ActivityLogDto>>(new HttpRequestMessage(HttpMethod.Get, $"api/activitylogs?pageNumber={page}&pageSize={pageSize}&search={Uri.EscapeDataString(search ?? "")}"));

    public Task<ApiResponse<BackupDto>> CreateBackupAsync(string? targetFolder = null) =>
        SendAsync<BackupDto>(new HttpRequestMessage(HttpMethod.Post, $"api/backup/create?targetFolder={Uri.EscapeDataString(targetFolder ?? "")}"));

    public Task<ApiResponse<bool>> RestoreBackupAsync(string backupId) =>
        SendAsync<bool>(new HttpRequestMessage(HttpMethod.Post, $"api/backup/{backupId}/restore"));

    public Task<ApiResponse<List<BackupDto>>> GetBackupsAsync() =>
        SendAsync<List<BackupDto>>(new HttpRequestMessage(HttpMethod.Get, "api/backup/list"));
}

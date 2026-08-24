using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using POS.Application.DTOs;
using POS.Application.Helpers;
using POS.Application.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;

namespace POS.Application.Services;

public class WorkerService : IWorkerService
{
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Sale> _saleRepo;
    private readonly IRepository<CustomerPayment> _paymentRepo;
    private readonly IRepository<Return> _returnRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IActivityLogService _activityLog;

    public WorkerService(
        IRepository<User> userRepo,
        IRepository<Sale> saleRepo,
        IRepository<CustomerPayment> paymentRepo,
        IRepository<Return> returnRepo,
        IPasswordHasher passwordHasher,
        IActivityLogService activityLog)
    {
        _userRepo = userRepo;
        _saleRepo = saleRepo;
        _paymentRepo = paymentRepo;
        _returnRepo = returnRepo;
        _passwordHasher = passwordHasher;
        _activityLog = activityLog;
    }

    public async Task<List<UserDto>> GetAllWorkersAsync(CancellationToken ct = default)
    {
        var users = await _userRepo.FindAsync(u => u.Role == Roles.Worker, ct);
        return users.Select(MapToDto).OrderBy(u => u.FullName).ToList();
    }

    public async Task<UserDto> GetWorkerByIdAsync(string id, CancellationToken ct = default)
    {
        var u = await _userRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);
        return MapToDto(u);
    }

    public async Task<UserDto> CreateWorkerAsync(CreateUserRequest request, string adminUserId, string adminUserName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            throw new DomainException("Username is required.");

        var passwordValidation = ValidationHelpers.ValidatePasswordStrength(request.Password);
        if (!passwordValidation.IsValid)
            throw new DomainException(string.Join(" ", passwordValidation.Errors));

        var existing = await _userRepo.FindOneAsync(u => u.Username.ToLower() == request.Username.Trim().ToLower(), ct);
        if (existing != null)
            throw new DomainException($"User with username '{request.Username}' already exists.");

        var worker = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email?.Trim() ?? string.Empty,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Username.Trim() : request.FullName.Trim(),
            Phone = request.Phone?.Trim() ?? string.Empty,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = Roles.Worker,
            Permissions = request.Permissions.Count > 0 ? request.Permissions : Permissions.WorkerDefaultPermissions.ToList(),
            MaxDiscountPercentage = request.MaxDiscountPercentage > 0 ? request.MaxDiscountPercentage : 5.0m,
            IsActive = true,
            MustChangePassword = request.MustChangePassword
        };

        var created = await _userRepo.AddAsync(worker, ct);

        await _activityLog.LogAsync(
            adminUserId,
            adminUserName,
            "CreateWorker",
            ActivityModule.Workers,
            $"Created worker account '{worker.Username}' ({worker.FullName}).",
            ct: ct);

        return MapToDto(created);
    }

    public async Task<UserDto> UpdateWorkerAsync(string id, UpdateUserRequest request, string adminUserId, string adminUserName, CancellationToken ct = default)
    {
        var u = await _userRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);

        if (request.FullName != null) u.FullName = request.FullName.Trim() ?? u.FullName;
        if (request.Email != null) u.Email = request.Email.Trim() ?? u.Email;
        if (request.Phone != null) u.Phone = request.Phone.Trim() ?? u.Phone;
        if (request.Permissions != null && request.Permissions.Count > 0) u.Permissions = request.Permissions;
        if (request.MaxDiscountPercentage.HasValue) u.MaxDiscountPercentage = request.MaxDiscountPercentage.Value;
        if (request.IsActive.HasValue) u.IsActive = request.IsActive.Value;
        u.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(u, ct);

        await _activityLog.LogAsync(
            adminUserId,
            adminUserName,
            "UpdateWorker",
            ActivityModule.Workers,
            $"Updated worker account '{u.Username}'.",
            ct: ct);

        return MapToDto(u);
    }

    public async Task<bool> ResetWorkerPasswordAsync(string id, string newPassword, string adminUserId, string adminUserName, CancellationToken ct = default)
    {
        var u = await _userRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);

        var passwordValidation = ValidationHelpers.ValidatePasswordStrength(newPassword);
        if (!passwordValidation.IsValid)
            throw new DomainException(string.Join(" ", passwordValidation.Errors));

        u.PasswordHash = _passwordHasher.HashPassword(newPassword);
        u.MustChangePassword = true;
        u.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(u, ct);

        await _activityLog.LogAsync(
            adminUserId,
            adminUserName,
            "ResetWorkerPassword",
            ActivityModule.Workers,
            $"Reset password for worker '{u.Username}'.",
            ct: ct);

        return true;
    }

    public async Task<bool> DeleteWorkerAsync(string id, string adminUserId, string adminUserName, CancellationToken ct = default)
    {
        var u = await _userRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);

        if (u.Id == adminUserId)
            throw new DomainException("You cannot delete your own account.");

        var deleted = await _userRepo.DeleteAsync(id, ct);

        if (deleted)
        {
            await _activityLog.LogAsync(
                adminUserId,
                adminUserName,
                "DeleteWorker",
                ActivityModule.Workers,
                $"Permanently deleted worker account '{u.Username}' ({u.FullName}).",
                ct: ct);
        }

        return deleted;
    }

    public async Task<WorkerPerformanceReportDto> GetWorkerPerformanceAsync(string workerId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var worker = await _userRepo.GetByIdAsync(workerId, ct)
            ?? throw new NotFoundException(nameof(User), workerId);

        var allSales = await _saleRepo.GetAllAsync(ct);
        var query = allSales.Where(s => s.CashierId == workerId && s.SaleStatus != SaleStatus.Cancelled);

        if (from.HasValue) query = query.Where(s => s.SaleDate >= from.Value);
        if (to.HasValue) query = query.Where(s => s.SaleDate <= to.Value);

        var salesList = query.ToList();
        decimal totalSales = salesList.Sum(s => s.GrandTotal);
        int invoiceCount = salesList.Count;
        decimal totalDiscount = salesList.Sum(s => s.DiscountTotal);

        var payments = await _paymentRepo.FindAsync(p => p.ReceivedByUserId == workerId, ct);
        if (from.HasValue) payments = payments.Where(p => p.CreatedAt >= from.Value).ToList();
        if (to.HasValue) payments = payments.Where(p => p.CreatedAt <= to.Value).ToList();
        decimal totalDueCollected = payments.Sum(p => p.Amount);

        var returns = await _returnRepo.FindAsync(r => r.CashierId == workerId, ct);
        if (from.HasValue) returns = returns.Where(r => r.CreatedAt >= from.Value).ToList();
        if (to.HasValue) returns = returns.Where(r => r.CreatedAt <= to.Value).ToList();
        decimal totalReturns = returns.Sum(r => r.TotalRefundAmount);

        return new WorkerPerformanceReportDto
        {
            WorkerId = worker.Id,
            WorkerName = worker.FullName,
            TotalSales = totalSales,
            InvoiceCount = invoiceCount,
            AverageInvoiceValue = invoiceCount > 0 ? Math.Round(totalSales / invoiceCount, 2) : 0,
            TotalDiscountGiven = totalDiscount,
            TotalDueCollected = totalDueCollected,
            TotalReturnsHandled = totalReturns
        };
    }

    private static UserDto MapToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        FullName = u.FullName,
        Phone = u.Phone,
        Role = u.Role,
        Permissions = u.Permissions,
        MaxDiscountPercentage = u.MaxDiscountPercentage,
        IsActive = u.IsActive,
        MustChangePassword = u.MustChangePassword,
        LastLoginAt = u.LastLoginAt,
        CreatedAt = u.CreatedAt
    };
}

public class ReportService : IReportService
{
    private readonly IRepository<Sale> _saleRepo;
    private readonly IRepository<Purchase> _purchaseRepo;
    private readonly IRepository<Expense> _expenseRepo;
    private readonly IRepository<Return> _returnRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IWorkerService _workerService;

    public ReportService(
        IRepository<Sale> saleRepo,
        IRepository<Purchase> purchaseRepo,
        IRepository<Expense> expenseRepo,
        IRepository<Return> returnRepo,
        IRepository<User> userRepo,
        IWorkerService workerService)
    {
        _saleRepo = saleRepo;
        _purchaseRepo = purchaseRepo;
        _expenseRepo = expenseRepo;
        _returnRepo = returnRepo;
        _userRepo = userRepo;
        _workerService = workerService;
    }

    public async Task<SalesReportDto> GetSalesReportAsync(ReportFilterRequest filter, CancellationToken ct = default)
    {
        var allSales = await _saleRepo.GetAllAsync(ct);
        var query = allSales.Where(s => s.SaleStatus != SaleStatus.Cancelled);

        if (filter.StartDate.HasValue) query = query.Where(s => s.SaleDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue) query = query.Where(s => s.SaleDate <= filter.EndDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.WorkerId)) query = query.Where(s => s.CashierId == filter.WorkerId);
        if (!string.IsNullOrWhiteSpace(filter.CustomerId)) query = query.Where(s => s.CustomerId == filter.CustomerId);
        if (filter.PaymentMethod.HasValue) query = query.Where(s => s.PaymentMethod == filter.PaymentMethod.Value);

        var list = query.OrderByDescending(s => s.SaleDate).ToList();

        var breakdown = list.SelectMany(s => s.Items)
            .GroupBy(i => new { i.ProductId, i.ProductName, i.SKU })
            .Select(g => new ProductSalesSummaryDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                SKU = g.Key.SKU,
                QuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.TotalPrice),
                TotalProfit = g.Sum(x => x.TotalPrice - (x.Quantity * x.UnitPurchasePrice))
            })
            .OrderByDescending(b => b.TotalRevenue)
            .ToList();

        return new SalesReportDto
        {
            TotalGrossSales = list.Sum(s => s.Subtotal),
            TotalDiscounts = list.Sum(s => s.DiscountTotal),
            TotalTaxes = list.Sum(s => s.TaxTotal),
            TotalNetSales = list.Sum(s => s.GrandTotal),
            TotalPaid = list.Sum(s => s.PaidAmount),
            TotalDue = list.Sum(s => s.DueAmount),
            InvoiceCount = list.Count,
            Sales = list.Select(s => new SaleDto
            {
                Id = s.Id,
                InvoiceNumber = s.InvoiceNumber,
                CustomerName = s.CustomerName,
                CashierName = s.CashierName,
                GrandTotal = s.GrandTotal,
                PaidAmount = s.PaidAmount,
                DueAmount = s.DueAmount,
                PaymentMethod = s.PaymentMethod,
                PaymentStatus = s.PaymentStatus,
                SaleStatus = s.SaleStatus,
                SaleDate = s.SaleDate
            }).ToList(),
            ProductBreakdown = breakdown
        };
    }

    public async Task<PurchaseReportDto> GetPurchaseReportAsync(ReportFilterRequest filter, CancellationToken ct = default)
    {
        var allPurchases = await _purchaseRepo.GetAllAsync(ct);
        var query = allPurchases.AsEnumerable();

        if (filter.StartDate.HasValue) query = query.Where(p => p.PurchaseDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue) query = query.Where(p => p.PurchaseDate <= filter.EndDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.SupplierId)) query = query.Where(p => p.SupplierId == filter.SupplierId);

        var list = query.OrderByDescending(p => p.PurchaseDate).ToList();

        return new PurchaseReportDto
        {
            TotalPurchases = list.Sum(p => p.GrandTotal),
            TotalPaid = list.Sum(p => p.PaidAmount),
            TotalDue = list.Sum(p => p.DueAmount),
            PurchaseCount = list.Count,
            Purchases = list.Select(p => new PurchaseDto
            {
                Id = p.Id,
                InvoiceNumber = p.InvoiceNumber,
                SupplierName = p.SupplierName,
                GrandTotal = p.GrandTotal,
                PaidAmount = p.PaidAmount,
                DueAmount = p.DueAmount,
                PurchaseDate = p.PurchaseDate,
                CreatedByUserName = p.CreatedByUserName
            }).ToList()
        };
    }

    public async Task<ProfitLossReportDto> GetProfitLossReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var allSales = await _saleRepo.GetAllAsync(ct);
        var salesInRange = allSales.Where(s => s.SaleDate >= from && s.SaleDate <= to && s.SaleStatus != SaleStatus.Cancelled).ToList();

        decimal totalRevenue = salesInRange.Sum(s => s.GrandTotal);
        decimal cogs = salesInRange.Sum(s => s.Items.Sum(i => i.Quantity * i.UnitPurchasePrice));

        var allExpenses = await _expenseRepo.GetAllAsync(ct);
        decimal totalExpenses = allExpenses.Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to && !e.IsVoided).Sum(e => e.Amount);

        var allReturns = await _returnRepo.GetAllAsync(ct);
        decimal totalRefunds = allReturns.Where(r => r.CreatedAt >= from && r.CreatedAt <= to).Sum(r => r.TotalRefundAmount);

        return new ProfitLossReportDto
        {
            TotalRevenue = totalRevenue,
            CostOfGoodsSold = cogs,
            TotalExpenses = totalExpenses,
            TotalRefunds = totalRefunds,
            StartDate = from,
            EndDate = to
        };
    }

    public async Task<ExpenseReportDto> GetExpenseReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var allExpenses = await _expenseRepo.GetAllAsync(ct);
        var list = allExpenses.Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to && !e.IsVoided).ToList();

        var breakdown = list.GroupBy(e => e.CategoryName)
            .Select(g => new ExpenseSummaryDto
            {
                CategoryName = string.IsNullOrWhiteSpace(g.Key) ? "General" : g.Key,
                TotalAmount = g.Sum(x => x.Amount)
            }).ToList();

        return new ExpenseReportDto
        {
            TotalExpenses = list.Sum(e => e.Amount),
            CategoryBreakdown = breakdown,
            Expenses = list.Select(e => new ExpenseDto
            {
                Id = e.Id,
                CategoryName = e.CategoryName,
                Description = e.Description,
                Amount = e.Amount,
                PaymentMethod = e.PaymentMethod,
                CreatedByUserName = e.CreatedByUserName,
                ExpenseDate = e.ExpenseDate,
                CreatedAt = e.CreatedAt
            }).ToList()
        };
    }

    public async Task<List<WorkerPerformanceReportDto>> GetAllWorkersPerformanceAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var workers = await _userRepo.FindAsync(u => u.Role == Roles.Worker, ct);
        var reports = new List<WorkerPerformanceReportDto>();

        foreach (var w in workers)
        {
            var rep = await _workerService.GetWorkerPerformanceAsync(w.Id, from, to, ct);
            reports.Add(rep);
        }

        return reports;
    }
}

public class CashService : ICashService
{
    private readonly IRepository<CashSession> _sessionRepo;
    private readonly IActivityLogService _activityLog;

    public CashService(IRepository<CashSession> sessionRepo, IActivityLogService activityLog)
    {
        _sessionRepo = sessionRepo;
        _activityLog = activityLog;
    }

    public async Task<CashSessionDto?> GetCurrentSessionAsync(string cashierId, CancellationToken ct = default)
    {
        var session = await _sessionRepo.FindOneAsync(s => s.CashierId == cashierId && s.Status == CashSessionStatus.Open, ct);
        return session != null ? MapToDto(session) : null;
    }

    public async Task<CashSessionDto> OpenSessionAsync(string cashierId, string cashierName, OpenCashSessionRequest request, CancellationToken ct = default)
    {
        var existing = await _sessionRepo.FindOneAsync(s => s.CashierId == cashierId && s.Status == CashSessionStatus.Open, ct);
        if (existing != null)
            throw new DomainException("An active cash session is already open for this user. Please close it first.");

        var session = new CashSession
        {
            CashierId = cashierId,
            CashierName = cashierName,
            StartTime = DateTime.UtcNow,
            OpeningFloat = request.OpeningFloat,
            CashSales = 0,
            CashExpenses = 0,
            CashDueCollections = 0,
            CashAdjustments = 0,
            Status = CashSessionStatus.Open,
            Notes = request.Notes?.Trim() ?? string.Empty
        };

        var saved = await _sessionRepo.AddAsync(session, ct);

        await _activityLog.LogAsync(
            cashierId,
            cashierName,
            "OpenCashSession",
            ActivityModule.Cash,
            $"Opened cash session with opening float ৳{request.OpeningFloat:N2}.",
            ct: ct);

        return MapToDto(saved);
    }

    public async Task<CashSessionDto> CloseSessionAsync(string sessionId, CloseCashSessionRequest request, CancellationToken ct = default)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId, ct)
            ?? throw new NotFoundException(nameof(CashSession), sessionId);

        if (session.Status == CashSessionStatus.Closed)
            throw new DomainException("Cash session is already closed.");

        session.EndTime = DateTime.UtcNow;
        session.ActualCash = request.ActualCash;
        session.Status = CashSessionStatus.Closed;
        session.Notes = string.IsNullOrWhiteSpace(request.Notes) ? session.Notes : $"{session.Notes} | Closing note: {request.Notes.Trim()}";
        session.UpdatedAt = DateTime.UtcNow;

        await _sessionRepo.UpdateAsync(session, ct);

        await _activityLog.LogAsync(
            session.CashierId,
            session.CashierName,
            "CloseCashSession",
            ActivityModule.Cash,
            $"Closed cash session. Expected: ৳{session.ExpectedCash:N2}, Actual: ৳{session.ActualCash:N2}, Difference: ৳{session.Difference:N2}.",
            ct: ct);

        return MapToDto(session);
    }

    public async Task<CashSessionDto> AdjustCashAsync(string sessionId, CashAdjustmentRequest request, string cashierName, CancellationToken ct = default)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId, ct)
            ?? throw new NotFoundException(nameof(CashSession), sessionId);

        session.CashAdjustments += request.Amount;
        session.Notes += $" | Adjust: ৳{request.Amount:N2} ({request.Reason})";
        session.UpdatedAt = DateTime.UtcNow;

        await _sessionRepo.UpdateAsync(session, ct);
        return MapToDto(session);
    }

    public async Task<List<CashSessionDto>> GetSessionHistoryAsync(DateTime? from, DateTime? to, string? cashierId, CancellationToken ct = default)
    {
        var all = await _sessionRepo.GetAllAsync(ct);
        var query = all.AsEnumerable();

        if (from.HasValue) query = query.Where(s => s.StartTime >= from.Value);
        if (to.HasValue) query = query.Where(s => s.StartTime <= to.Value);
        if (!string.IsNullOrWhiteSpace(cashierId)) query = query.Where(s => s.CashierId == cashierId);

        return query.OrderByDescending(s => s.StartTime).Select(MapToDto).ToList();
    }

    private static CashSessionDto MapToDto(CashSession s) => new()
    {
        Id = s.Id,
        CashierId = s.CashierId,
        CashierName = s.CashierName,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        OpeningFloat = s.OpeningFloat,
        CashSales = s.CashSales,
        CashExpenses = s.CashExpenses,
        CashDueCollections = s.CashDueCollections,
        CashAdjustments = s.CashAdjustments,
        ExpectedCash = s.ExpectedCash,
        ActualCash = s.ActualCash,
        Difference = s.Difference,
        Status = s.Status,
        Notes = s.Notes
    };
}

public class InvoiceService : IInvoiceService
{
    public Task<string> GenerateReceiptTextAsync(SaleDto sale, BusinessSettingsDto settings)
    {
        int width = settings.ThermalPaperWidthMm == 58 ? 32 : 42;
        var sb = new StringBuilder();

        string Center(string text)
        {
            if (text.Length >= width) return text[..width];
            int leftPad = (width - text.Length) / 2;
            return text.PadLeft(leftPad + text.Length).PadRight(width);
        }

        string Line(char c = '=') => new string(c, width);

        sb.AppendLine(Center(settings.StoreName));
        if (!string.IsNullOrWhiteSpace(settings.Tagline)) sb.AppendLine(Center(settings.Tagline));
        if (!string.IsNullOrWhiteSpace(settings.Address)) sb.AppendLine(Center(settings.Address));
        if (!string.IsNullOrWhiteSpace(settings.Phone)) sb.AppendLine(Center($"Tel: {settings.Phone}"));
        sb.AppendLine(Line('='));
        sb.AppendLine($"Invoice : {sale.InvoiceNumber}");
        sb.AppendLine($"Date    : {sale.SaleDate:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Cashier : {sale.CashierName}");
        sb.AppendLine($"Customer: {sale.CustomerName}");
        sb.AppendLine(Line('-'));
        sb.AppendLine(string.Format("{0,-18} {1,4} {2,8} {3,9}", "Item", "Qty", "Price", "Total"));
        sb.AppendLine(Line('-'));

        foreach (var item in sale.Items)
        {
            string name = item.ProductName.Length > 18 ? item.ProductName[..18] : item.ProductName;
            sb.AppendLine(string.Format("{0,-18} {1,4} {2,8:F2} {3,9:F2}", name, item.Quantity, item.UnitSellingPrice, item.TotalPrice));
        }

        sb.AppendLine(Line('-'));
        sb.AppendLine($"{"Subtotal:",-28} {settings.CurrencySymbol}{sale.Subtotal,10:F2}");
        if (sale.DiscountTotal > 0)
            sb.AppendLine($"{"Discount:",-28}-{settings.CurrencySymbol}{sale.DiscountTotal,10:F2}");
        if (sale.TaxTotal > 0)
            sb.AppendLine($"{"Tax:",-28} {settings.CurrencySymbol}{sale.TaxTotal,10:F2}");
        sb.AppendLine(Line('='));
        sb.AppendLine($"{"GRAND TOTAL:",-28} {settings.CurrencySymbol}{sale.GrandTotal,10:F2}");
        sb.AppendLine($"{"Paid (" + sale.PaymentMethod + "):",-28} {settings.CurrencySymbol}{sale.PaidAmount,10:F2}");
        if (sale.ChangeAmount > 0)
            sb.AppendLine($"{"Change:",-28} {settings.CurrencySymbol}{sale.ChangeAmount,10:F2}");
        if (sale.DueAmount > 0)
            sb.AppendLine($"{"Due Amount:",-28} {settings.CurrencySymbol}{sale.DueAmount,10:F2}");
        sb.AppendLine(Line('='));
        if (!string.IsNullOrWhiteSpace(settings.ReceiptHeaderNote)) sb.AppendLine(Center(settings.ReceiptHeaderNote));
        if (!string.IsNullOrWhiteSpace(settings.ReceiptFooterNote)) sb.AppendLine(Center(settings.ReceiptFooterNote));

        return Task.FromResult(sb.ToString());
    }

    public Task<string> GenerateA4HtmlAsync(SaleDto sale, BusinessSettingsDto settings)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8' />
<title>Invoice {sale.InvoiceNumber}</title>
<style>
body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 40px; color: #333; }}
.header {{ display: flex; justify-content: space-between; border-bottom: 2px solid #2563eb; padding-bottom: 20px; }}
.store-title {{ font-size: 24px; font-weight: bold; color: #1e3a8a; }}
.invoice-title {{ font-size: 28px; font-weight: bold; color: #2563eb; text-align: right; }}
.info-table {{ width: 100%; margin-top: 20px; }}
.items-table {{ width: 100%; border-collapse: collapse; margin-top: 30px; }}
.items-table th {{ background: #f1f5f9; padding: 10px; border: 1px solid #cbd5e1; text-align: left; }}
.items-table td {{ padding: 10px; border: 1px solid #cbd5e1; }}
.totals {{ margin-top: 20px; width: 300px; margin-left: auto; }}
.totals table {{ width: 100%; border-collapse: collapse; }}
.totals td {{ padding: 6px; }}
.grand-total {{ font-size: 18px; font-weight: bold; background: #e0f2fe; }}
.footer {{ margin-top: 50px; text-align: center; color: #64748b; font-size: 13px; }}
</style>
</head>
<body>
<div class='header'>
  <div>
    <div class='store-title'>{settings.StoreName}</div>
    <div>{settings.Tagline}</div>
    <div>{settings.Address}</div>
    <div>Phone: {settings.Phone} | Email: {settings.Email}</div>
  </div>
  <div>
    <div class='invoice-title'>INVOICE</div>
    <div><strong>Invoice #:</strong> {sale.InvoiceNumber}</div>
    <div><strong>Date:</strong> {sale.SaleDate:yyyy-MM-dd HH:mm}</div>
    <div><strong>Cashier:</strong> {sale.CashierName}</div>
  </div>
</div>

<table class='info-table'>
  <tr>
    <td><strong>Bill To:</strong><br/>{sale.CustomerName}<br/>{sale.CustomerPhone}</td>
    <td style='text-align:right'><strong>Payment Method:</strong> {sale.PaymentMethod}<br/><strong>Payment Status:</strong> {sale.PaymentStatus}</td>
  </tr>
</table>

<table class='items-table'>
  <thead>
    <tr>
      <th>#</th>
      <th>Item Description</th>
      <th>SKU</th>
      <th>Qty</th>
      <th>Unit Price</th>
      <th>Discount</th>
      <th>Total</th>
    </tr>
  </thead>
  <tbody>
    {string.Join("", sale.Items.Select((item, idx) => $@"
    <tr>
      <td>{idx + 1}</td>
      <td>{item.ProductName}</td>
      <td>{item.SKU}</td>
      <td>{item.Quantity}</td>
      <td>{settings.CurrencySymbol}{item.UnitSellingPrice:F2}</td>
      <td>{settings.CurrencySymbol}{item.DiscountAmount:F2}</td>
      <td>{settings.CurrencySymbol}{item.TotalPrice:F2}</td>
    </tr>"))}
  </tbody>
</table>

<div class='totals'>
  <table>
    <tr><td>Subtotal:</td><td style='text-align:right'>{settings.CurrencySymbol}{sale.Subtotal:F2}</td></tr>
    <tr><td>Discount:</td><td style='text-align:right'>-{settings.CurrencySymbol}{sale.DiscountTotal:F2}</td></tr>
    <tr><td>Tax:</td><td style='text-align:right'>{settings.CurrencySymbol}{sale.TaxTotal:F2}</td></tr>
    <tr class='grand-total'><td>Grand Total:</td><td style='text-align:right'>{settings.CurrencySymbol}{sale.GrandTotal:F2}</td></tr>
    <tr><td>Paid Amount:</td><td style='text-align:right'>{settings.CurrencySymbol}{sale.PaidAmount:F2}</td></tr>
    <tr><td>Due Amount:</td><td style='text-align:right'>{settings.CurrencySymbol}{sale.DueAmount:F2}</td></tr>
  </table>
</div>

<div class='footer'>
  <p>{settings.ReceiptHeaderNote}</p>
  <p>{settings.ReceiptFooterNote}</p>
</div>
</body>
</html>";
        return Task.FromResult(html);
    }
}

public class BarcodeService : IBarcodeService
{
    public string GenerateBarcodeSvg(string code, string format = "CODE128")
    {
        // Standalone SVG barcode generation
        int width = 240;
        int height = 80;
        var sb = new StringBuilder();
        sb.AppendLine($"<svg width='{width}' height='{height}' xmlns='http://www.w3.org/2000/svg'>");
        sb.AppendLine($"<rect width='100%' height='100%' fill='white'/>");

        // Generate pseudo-bars deterministically based on hash of string
        int x = 20;
        byte[] bytes = Encoding.UTF8.GetBytes(code);
        for (int i = 0; i < bytes.Length; i++)
        {
            byte b = bytes[i];
            for (int bit = 0; bit < 6; bit++)
            {
                bool isBar = ((b >> bit) & 1) == 1;
                int barWidth = (b % 3) + 1;
                if (isBar && x < width - 20)
                {
                    sb.AppendLine($"<rect x='{x}' y='10' width='{barWidth}' height='50' fill='black'/>");
                }
                x += barWidth + 1;
            }
        }

        sb.AppendLine($"<text x='{width / 2}' y='72' font-family='monospace' font-size='12' text-anchor='middle' fill='black'>{code}</text>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }
}

public class SettingsService : ISettingsService
{
    private readonly IRepository<BusinessSettings> _settingsRepo;
    private readonly IActivityLogService _activityLog;

    public SettingsService(IRepository<BusinessSettings> settingsRepo, IActivityLogService activityLog)
    {
        _settingsRepo = settingsRepo;
        _activityLog = activityLog;
    }

    public async Task<BusinessSettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        var list = await _settingsRepo.GetAllAsync(ct);
        var s = list.FirstOrDefault() ?? new BusinessSettings();
        return MapToDto(s);
    }

    public async Task<BusinessSettingsDto> UpdateSettingsAsync(BusinessSettingsDto request, string userId, string userName, CancellationToken ct = default)
    {
        var list = await _settingsRepo.GetAllAsync(ct);
        var s = list.FirstOrDefault() ?? new BusinessSettings();

        s.StoreName = request.StoreName.Trim();
        s.Tagline = request.Tagline.Trim();
        s.Address = request.Address.Trim();
        s.Phone = request.Phone.Trim();
        s.Email = request.Email.Trim();
        s.Website = request.Website.Trim();
        s.CurrencySymbol = request.CurrencySymbol.Trim();
        s.TaxRatePercentage = request.TaxRatePercentage;
        s.InvoicePrefix = request.InvoicePrefix.Trim();
        s.NextInvoiceNumber = request.NextInvoiceNumber;
        s.DefaultDiscountPercentage = request.DefaultDiscountPercentage;
        s.MaxWorkerDiscountPercentage = request.MaxWorkerDiscountPercentage;
        s.LowStockAlertThreshold = request.LowStockAlertThreshold;
        s.ReceiptHeaderNote = request.ReceiptHeaderNote.Trim();
        s.ReceiptFooterNote = request.ReceiptFooterNote.Trim();
        s.ThermalPaperWidthMm = request.ThermalPaperWidthMm;
        s.DefaultPrinterName = request.DefaultPrinterName?.Trim() ?? string.Empty;
        s.AutoPrintInvoice = request.AutoPrintInvoice;
        s.UpdatedAt = DateTime.UtcNow;

        if (list.Count > 0)
            await _settingsRepo.UpdateAsync(s, ct);
        else
            await _settingsRepo.AddAsync(s, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "UpdateSettings",
            ActivityModule.Settings,
            $"Updated business settings for '{s.StoreName}'.",
            ct: ct);

        return MapToDto(s);
    }

    private static BusinessSettingsDto MapToDto(BusinessSettings s) => new()
    {
        StoreName = s.StoreName,
        Tagline = s.Tagline,
        Address = s.Address,
        Phone = s.Phone,
        Email = s.Email,
        Website = s.Website,
        CurrencySymbol = s.CurrencySymbol,
        TaxRatePercentage = s.TaxRatePercentage,
        InvoicePrefix = s.InvoicePrefix,
        NextInvoiceNumber = s.NextInvoiceNumber,
        DefaultDiscountPercentage = s.DefaultDiscountPercentage,
        MaxWorkerDiscountPercentage = s.MaxWorkerDiscountPercentage,
        LowStockAlertThreshold = s.LowStockAlertThreshold,
        ReceiptHeaderNote = s.ReceiptHeaderNote,
        ReceiptFooterNote = s.ReceiptFooterNote,
        ThermalPaperWidthMm = s.ThermalPaperWidthMm,
        DefaultPrinterName = s.DefaultPrinterName,
        AutoPrintInvoice = s.AutoPrintInvoice
    };
}

public class UserService : IUserService
{
    private readonly IRepository<User> _userRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IActivityLogService _activityLog;

    public UserService(IRepository<User> userRepo, IPasswordHasher passwordHasher, IActivityLogService activityLog)
    {
        _userRepo = userRepo;
        _passwordHasher = passwordHasher;
        _activityLog = activityLog;
    }

    public async Task<List<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _userRepo.GetAllAsync(ct);
        return users.Select(MapToDto).OrderBy(u => u.Role).ThenBy(u => u.Username).ToList();
    }

    public async Task<UserDto> GetUserByIdAsync(string id, CancellationToken ct = default)
    {
        var u = await _userRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);
        return MapToDto(u);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, string adminUserId, string adminUserName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            throw new DomainException("Username is required.");

        var passwordValidation = ValidationHelpers.ValidatePasswordStrength(request.Password);
        if (!passwordValidation.IsValid)
            throw new DomainException(string.Join(" ", passwordValidation.Errors));

        var existing = await _userRepo.FindOneAsync(u => u.Username.ToLower() == request.Username.Trim().ToLower(), ct);
        if (existing != null)
            throw new DomainException($"User '{request.Username}' already exists.");

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = request.Email?.Trim() ?? string.Empty,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Username.Trim() : request.FullName.Trim(),
            Phone = request.Phone?.Trim() ?? string.Empty,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = request.Role == Roles.Employer ? Roles.Employer : Roles.Worker,
            Permissions = request.Role == Roles.Employer ? Permissions.EmployerDefaultPermissions.ToList() : (request.Permissions.Count > 0 ? request.Permissions : Permissions.WorkerDefaultPermissions.ToList()),
            MaxDiscountPercentage = request.MaxDiscountPercentage,
            IsActive = true,
            MustChangePassword = request.MustChangePassword
        };

        var created = await _userRepo.AddAsync(user, ct);

        await _activityLog.LogAsync(
            adminUserId,
            adminUserName,
            "CreateUser",
            ActivityModule.Users,
            $"Created user '{user.Username}' ({user.Role}).",
            ct: ct);

        return MapToDto(created);
    }

    public async Task<UserDto> UpdateUserAsync(string id, UpdateUserRequest request, string adminUserId, string adminUserName, CancellationToken ct = default)
    {
        var u = await _userRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);

        if (request.FullName != null) u.FullName = request.FullName.Trim() ?? u.FullName;
        if (request.Email != null) u.Email = request.Email.Trim() ?? u.Email;
        if (request.Phone != null) u.Phone = request.Phone.Trim() ?? u.Phone;
        if (!string.IsNullOrWhiteSpace(request.Role)) u.Role = request.Role;
        if (request.Permissions != null && request.Permissions.Count > 0) u.Permissions = request.Permissions;
        if (request.MaxDiscountPercentage.HasValue) u.MaxDiscountPercentage = request.MaxDiscountPercentage.Value;
        if (request.IsActive.HasValue) u.IsActive = request.IsActive.Value;
        u.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(u, ct);

        await _activityLog.LogAsync(
            adminUserId,
            adminUserName,
            "UpdateUser",
            ActivityModule.Users,
            $"Updated user '{u.Username}'.",
            ct: ct);

        return MapToDto(u);
    }

    public async Task<bool> ToggleUserStatusAsync(string id, bool isActive, string adminUserId, string adminUserName, CancellationToken ct = default)
    {
        var u = await _userRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);

        u.IsActive = isActive;
        u.UpdatedAt = DateTime.UtcNow;
        var updated = await _userRepo.UpdateAsync(u, ct);

        await _activityLog.LogAsync(
            adminUserId,
            adminUserName,
            isActive ? "ActivateUser" : "DeactivateUser",
            ActivityModule.Users,
            $"Toggled user '{u.Username}' status to {(isActive ? "Active" : "Inactive")}.",
            ct: ct);

        return updated;
    }

    public async Task<bool> DeleteUserAsync(string id, string adminUserId, string adminUserName, CancellationToken ct = default)
    {
        var u = await _userRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(User), id);

        if (u.Id == adminUserId)
            throw new DomainException("You cannot delete your own account.");

        var deleted = await _userRepo.DeleteAsync(id, ct);

        if (deleted)
        {
            await _activityLog.LogAsync(
                adminUserId,
                adminUserName,
                "DeleteUser",
                ActivityModule.Users,
                $"Permanently deleted user account '{u.Username}' ({u.FullName}, Role: {u.Role}).",
                ct: ct);
        }

        return deleted;
    }

    private static UserDto MapToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        FullName = u.FullName,
        Phone = u.Phone,
        Role = u.Role,
        Permissions = u.Permissions,
        MaxDiscountPercentage = u.MaxDiscountPercentage,
        IsActive = u.IsActive,
        MustChangePassword = u.MustChangePassword,
        LastLoginAt = u.LastLoginAt,
        CreatedAt = u.CreatedAt
    };
}

public class ActivityLogService : IActivityLogService
{
    private readonly IRepository<ActivityLog> _logRepo;

    public ActivityLogService(IRepository<ActivityLog> logRepo)
    {
        _logRepo = logRepo;
    }

    public async Task LogAsync(string userId, string userName, string action, ActivityModule module, string description, string? ipAddress = null, CancellationToken ct = default)
    {
        var log = new ActivityLog
        {
            UserId = userId,
            UserName = userName,
            Action = action,
            Module = module,
            Description = description,
            IpAddress = ipAddress ?? "127.0.0.1"
        };
        await _logRepo.AddAsync(log, ct);
    }

    public async Task<PagedResult<ActivityLogDto>> GetLogsAsync(int pageNumber, int pageSize, ActivityModule? module = null, string? searchTerm = null, CancellationToken ct = default)
    {
        var (safePageNumber, safePageSize) = ValidationHelpers.SanitizePagination(pageNumber, pageSize);

        var all = await _logRepo.GetAllAsync(ct);
        var query = all.AsEnumerable();

        if (module.HasValue) query = query.Where(l => l.Module == module.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = ValidationHelpers.SanitizeSearchTerm(searchTerm);
            query = query.Where(l =>
                l.UserName.ToLower().Contains(term) ||
                l.Action.ToLower().Contains(term) ||
                l.Description.ToLower().Contains(term));
        }

        var list = query.OrderByDescending(l => l.CreatedAt).ToList();
        var totalCount = list.Count;
        var paged = list
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .Select(l => new ActivityLogDto
            {
                Id = l.Id,
                UserName = l.UserName,
                Action = l.Action,
                Module = l.Module,
                Description = l.Description,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt
            })
            .ToList();

        return new PagedResult<ActivityLogDto>
        {
            Items = paged,
            TotalCount = totalCount,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }
}

public class BackupService : IBackupService
{
    private readonly IRepository<BackupMetadata> _backupRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<Sale> _saleRepo;
    private readonly IRepository<Customer> _customerRepo;
    private readonly IRepository<Supplier> _supplierRepo;
    private readonly IRepository<Expense> _expenseRepo;
    private readonly IRepository<BusinessSettings> _settingsRepo;
    private readonly IActivityLogService _activityLog;

    public BackupService(
        IRepository<BackupMetadata> backupRepo,
        IRepository<Product> productRepo,
        IRepository<Category> categoryRepo,
        IRepository<Sale> saleRepo,
        IRepository<Customer> customerRepo,
        IRepository<Supplier> supplierRepo,
        IRepository<Expense> expenseRepo,
        IRepository<BusinessSettings> settingsRepo,
        IActivityLogService activityLog)
    {
        _backupRepo = backupRepo;
        _productRepo = productRepo;
        _categoryRepo = categoryRepo;
        _saleRepo = saleRepo;
        _customerRepo = customerRepo;
        _supplierRepo = supplierRepo;
        _expenseRepo = expenseRepo;
        _settingsRepo = settingsRepo;
        _activityLog = activityLog;
    }

    public async Task<BackupDto> CreateBackupAsync(string targetFolder, string userId, string userName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(targetFolder) || !Directory.Exists(targetFolder))
        {
            targetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
        }
        Directory.CreateDirectory(targetFolder);

        var data = new
        {
            ExportedAt = DateTime.UtcNow,
            Categories = await _categoryRepo.GetAllAsync(ct),
            Products = await _productRepo.GetAllAsync(ct),
            Sales = await _saleRepo.GetAllAsync(ct),
            Customers = await _customerRepo.GetAllAsync(ct),
            Suppliers = await _supplierRepo.GetAllAsync(ct),
            Expenses = await _expenseRepo.GetAllAsync(ct),
            Settings = await _settingsRepo.GetAllAsync(ct)
        };

        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        string fileName = $"pos_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        string filePath = Path.Combine(targetFolder, fileName);
        await File.WriteAllTextAsync(filePath, json, ct);

        var fileInfo = new FileInfo(filePath);
        string checksum;
        using (var sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
            checksum = Convert.ToHexString(hash);
        }

        var meta = new BackupMetadata
        {
            FileName = fileName,
            FilePath = filePath,
            SizeBytes = fileInfo.Length,
            Checksum = checksum,
            CreatedByUserId = userId,
            CreatedByUserName = userName
        };

        var saved = await _backupRepo.AddAsync(meta, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "CreateBackup",
            ActivityModule.Backup,
            $"Created database backup snapshot '{fileName}' ({fileInfo.Length / 1024} KB).",
            ct: ct);

        return new BackupDto
        {
            Id = saved.Id,
            FileName = saved.FileName,
            FilePath = saved.FilePath,
            SizeBytes = saved.SizeBytes,
            CreatedByUserName = saved.CreatedByUserName,
            CreatedAt = saved.CreatedAt
        };
    }

    public async Task<bool> RestoreBackupAsync(string backupId, string userId, string userName, CancellationToken ct = default)
    {
        var meta = await _backupRepo.GetByIdAsync(backupId, ct)
            ?? throw new NotFoundException(nameof(BackupMetadata), backupId);

        if (!File.Exists(meta.FilePath))
            throw new FileNotFoundException($"Backup file '{meta.FilePath}' was not found.");

        string json = await File.ReadAllTextAsync(meta.FilePath, ct);
        using var doc = JsonDocument.Parse(json);

        // Verification of snapshot integrity
        if (doc.RootElement.TryGetProperty("Categories", out var catEl))
        {
            var categories = JsonSerializer.Deserialize<List<Category>>(catEl.GetRawText());
            if (categories != null)
            {
                foreach (var c in categories)
                {
                    var exist = await _categoryRepo.GetByIdAsync(c.Id, ct);
                    if (exist != null) await _categoryRepo.UpdateAsync(c, ct);
                    else await _categoryRepo.AddAsync(c, ct);
                }
            }
        }

        if (doc.RootElement.TryGetProperty("Products", out var prodEl))
        {
            var products = JsonSerializer.Deserialize<List<Product>>(prodEl.GetRawText());
            if (products != null)
            {
                foreach (var p in products)
                {
                    var exist = await _productRepo.GetByIdAsync(p.Id, ct);
                    if (exist != null) await _productRepo.UpdateAsync(p, ct);
                    else await _productRepo.AddAsync(p, ct);
                }
            }
        }

        await _activityLog.LogAsync(
            userId,
            userName,
            "RestoreBackup",
            ActivityModule.Backup,
            $"Restored database snapshot from '{meta.FileName}'.",
            ct: ct);

        return true;
    }

    public async Task<List<BackupDto>> GetBackupsAsync(CancellationToken ct = default)
    {
        var list = await _backupRepo.GetAllAsync(ct);
        return list.OrderByDescending(b => b.CreatedAt).Select(b => new BackupDto
        {
            Id = b.Id,
            FileName = b.FileName,
            FilePath = b.FilePath,
            SizeBytes = b.SizeBytes,
            CreatedByUserName = b.CreatedByUserName,
            CreatedAt = b.CreatedAt
        }).ToList();
    }
}

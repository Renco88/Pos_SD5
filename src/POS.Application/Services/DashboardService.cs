using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using POS.Application.DTOs;
using POS.Application.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;

namespace POS.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IRepository<Sale> _saleRepo;
    private readonly IRepository<Purchase> _purchaseRepo;
    private readonly IRepository<Expense> _expenseRepo;
    private readonly IRepository<Customer> _customerRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<CashSession> _cashSessionRepo;
    private readonly IRepository<CustomerPayment> _customerPaymentRepo;

    public DashboardService(
        IRepository<Sale> saleRepo,
        IRepository<Purchase> purchaseRepo,
        IRepository<Expense> expenseRepo,
        IRepository<Customer> customerRepo,
        IRepository<Product> productRepo,
        IRepository<CashSession> cashSessionRepo,
        IRepository<CustomerPayment> customerPaymentRepo)
    {
        _saleRepo = saleRepo;
        _purchaseRepo = purchaseRepo;
        _expenseRepo = expenseRepo;
        _customerRepo = customerRepo;
        _productRepo = productRepo;
        _cashSessionRepo = cashSessionRepo;
        _customerPaymentRepo = customerPaymentRepo;
    }

    public async Task<EmployerDashboardDto> GetEmployerDashboardAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var allSales = await _saleRepo.GetAllAsync(ct);
        var allPurchases = await _purchaseRepo.GetAllAsync(ct);
        var allExpenses = await _expenseRepo.GetAllAsync(ct);
        var allCustomers = await _customerRepo.GetAllAsync(ct);
        var allProducts = await _productRepo.GetAllAsync(ct);

        var todaySales = allSales.Where(s => s.SaleDate >= today && s.SaleDate < tomorrow && s.SaleStatus != SaleStatus.Cancelled).ToList();
        var todayPurchases = allPurchases.Where(p => p.PurchaseDate >= today && p.PurchaseDate < tomorrow).ToList();
        var todayExpenses = allExpenses.Where(e => e.ExpenseDate >= today && e.ExpenseDate < tomorrow && !e.IsVoided).ToList();

        decimal todaySalesTotal = todaySales.Sum(s => s.GrandTotal);
        decimal todayPurchasesTotal = todayPurchases.Sum(p => p.GrandTotal);
        decimal todayExpensesTotal = todayExpenses.Sum(e => e.Amount);

        // Profit calculation: Revenue - COGS - Expenses
        decimal todayCogs = todaySales.Sum(s => s.Items.Sum(i => i.Quantity * i.UnitPurchasePrice));
        decimal todayProfit = todaySalesTotal - todayCogs - todayExpensesTotal;

        decimal totalDue = allCustomers.Sum(c => c.CurrentDue);
        int totalProducts = allProducts.Count;
        int lowStockCount = allProducts.Count(p => p.IsLowStock);
        int outOfStockCount = allProducts.Count(p => p.IsOutOfStock);

        // Recent sales
        var recentSales = allSales
            .OrderByDescending(s => s.SaleDate)
            .Take(10)
            .Select(s => new SaleDto
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
            })
            .ToList();

        // Recent purchases
        var recentPurchases = allPurchases
            .OrderByDescending(p => p.PurchaseDate)
            .Take(10)
            .Select(p => new PurchaseDto
            {
                Id = p.Id,
                InvoiceNumber = p.InvoiceNumber,
                SupplierName = p.SupplierName,
                GrandTotal = p.GrandTotal,
                PaidAmount = p.PaidAmount,
                DueAmount = p.DueAmount,
                PurchaseDate = p.PurchaseDate,
                CreatedByUserName = p.CreatedByUserName
            })
            .ToList();

        // Top selling products
        var topSelling = allSales
            .Where(s => s.SaleStatus != SaleStatus.Cancelled)
            .SelectMany(s => s.Items)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new TopSellingProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                UnitsSold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.UnitsSold)
            .Take(5)
            .ToList();

        // 7-day charts
        var salesChart = new List<ChartDataPointDto>();
        var purchaseChart = new List<ChartDataPointDto>();
        for (int i = 6; i >= 0; i--)
        {
            var dayStart = today.AddDays(-i);
            var dayEnd = dayStart.AddDays(1);
            var label = dayStart.ToString("MMM dd");

            decimal daySales = allSales
                .Where(s => s.SaleDate >= dayStart && s.SaleDate < dayEnd && s.SaleStatus != SaleStatus.Cancelled)
                .Sum(s => s.GrandTotal);

            decimal dayPurchases = allPurchases
                .Where(p => p.PurchaseDate >= dayStart && p.PurchaseDate < dayEnd)
                .Sum(p => p.GrandTotal);

            salesChart.Add(new ChartDataPointDto { Label = label, Value = daySales });
            purchaseChart.Add(new ChartDataPointDto { Label = label, Value = dayPurchases });
        }

        // Expense summary by category
        var expenseSummary = allExpenses
            .Where(e => !e.IsVoided)
            .GroupBy(e => e.CategoryName)
            .Select(g => new ExpenseSummaryDto
            {
                CategoryName = string.IsNullOrEmpty(g.Key) ? "General" : g.Key,
                TotalAmount = g.Sum(x => x.Amount)
            })
            .ToList();

        return new EmployerDashboardDto
        {
            TodaySales = todaySalesTotal,
            TodayPurchases = todayPurchasesTotal,
            TodayExpenses = todayExpensesTotal,
            TodayProfit = todayProfit,
            TotalDue = totalDue,
            TotalCustomers = allCustomers.Count,
            TotalProducts = totalProducts,
            LowStockProductsCount = lowStockCount,
            OutOfStockProductsCount = outOfStockCount,
            RecentSales = recentSales,
            RecentPurchases = recentPurchases,
            TopSellingProducts = topSelling,
            SalesChart = salesChart,
            PurchaseChart = purchaseChart,
            ExpenseSummary = expenseSummary
        };
    }

    public async Task<WorkerDashboardDto> GetWorkerDashboardAsync(string workerId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var workerSales = await _saleRepo.FindAsync(s => s.CashierId == workerId && s.SaleDate >= today && s.SaleDate < tomorrow && s.SaleStatus != SaleStatus.Cancelled, ct);
        var todayCollectedDuePayments = await _customerPaymentRepo.FindAsync(p => p.ReceivedByUserId == workerId && p.CreatedAt >= today && p.CreatedAt < tomorrow, ct);

        decimal todaySalesAmount = workerSales.Sum(s => s.GrandTotal);
        int todayTxCount = workerSales.Count;
        decimal todayCollectedDue = todayCollectedDuePayments.Sum(p => p.Amount);

        // Active cash session
        var activeSession = await _cashSessionRepo.FindOneAsync(c => c.CashierId == workerId && c.Status == CashSessionStatus.Open, ct);
        decimal currentCash = activeSession != null ? activeSession.ExpectedCash : 0;

        var recentSales = workerSales
            .OrderByDescending(s => s.SaleDate)
            .Take(10)
            .Select(s => new SaleDto
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
            })
            .ToList();

        var summary = new WorkerSalesSummaryDto
        {
            TotalSalesAmount = todaySalesAmount,
            TotalInvoices = todayTxCount,
            AverageInvoiceValue = todayTxCount > 0 ? Math.Round(todaySalesAmount / todayTxCount, 2) : 0,
            TotalDiscountGiven = workerSales.Sum(s => s.DiscountTotal)
        };

        return new WorkerDashboardDto
        {
            TodaySales = todaySalesAmount,
            TodayTransactionCount = todayTxCount,
            TodayCollectedDue = todayCollectedDue,
            CurrentCash = currentCash,
            RecentSales = recentSales,
            PersonalSalesSummary = summary
        };
    }
}

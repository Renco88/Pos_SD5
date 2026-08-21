using System;
using System.Collections.Generic;

namespace POS.Application.DTOs;

public class EmployerDashboardDto
{
    public decimal TodaySales { get; set; }
    public decimal TodayPurchases { get; set; }
    public decimal TodayExpenses { get; set; }
    public decimal TodayProfit { get; set; }
    public decimal TotalDue { get; set; }

    public int TotalCustomers { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProductsCount { get; set; }
    public int OutOfStockProductsCount { get; set; }

    public List<SaleDto> RecentSales { get; set; } = [];
    public List<PurchaseDto> RecentPurchases { get; set; } = [];
    public List<TopSellingProductDto> TopSellingProducts { get; set; } = [];
    public List<ChartDataPointDto> SalesChart { get; set; } = [];
    public List<ChartDataPointDto> PurchaseChart { get; set; } = [];
    public List<ExpenseSummaryDto> ExpenseSummary { get; set; } = [];
}

public class WorkerDashboardDto
{
    public decimal TodaySales { get; set; }
    public int TodayTransactionCount { get; set; }
    public decimal TodayCollectedDue { get; set; }
    public decimal CurrentCash { get; set; }
    public List<SaleDto> RecentSales { get; set; } = [];
    public WorkerSalesSummaryDto PersonalSalesSummary { get; set; } = new();
}

public class TopSellingProductDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class ChartDataPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class ExpenseSummaryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

public class WorkerSalesSummaryDto
{
    public decimal TotalSalesAmount { get; set; }
    public int TotalInvoices { get; set; }
    public decimal AverageInvoiceValue { get; set; }
    public decimal TotalDiscountGiven { get; set; }
}

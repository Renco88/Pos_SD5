using System;
using System.Collections.Generic;
using POS.Domain.Enums;

namespace POS.Application.DTOs;

public class CashSessionDto
{
    public string Id { get; set; } = string.Empty;
    public string CashierId { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal OpeningFloat { get; set; }
    public decimal CashSales { get; set; }
    public decimal CashExpenses { get; set; }
    public decimal CashDueCollections { get; set; }
    public decimal CashAdjustments { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal? Difference { get; set; }
    public CashSessionStatus Status { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class OpenCashSessionRequest
{
    public decimal OpeningFloat { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class CloseCashSessionRequest
{
    public decimal ActualCash { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class CashAdjustmentRequest
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ReportFilterRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ProductId { get; set; }
    public string? CategoryId { get; set; }
    public string? WorkerId { get; set; }
    public string? CustomerId { get; set; }
    public string? SupplierId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
}

public class SalesReportDto
{
    public decimal TotalGrossSales { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalTaxes { get; set; }
    public decimal TotalNetSales { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalDue { get; set; }
    public int InvoiceCount { get; set; }
    public List<SaleDto> Sales { get; set; } = [];
    public List<ProductSalesSummaryDto> ProductBreakdown { get; set; } = [];
}

public class ProductSalesSummaryDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalProfit { get; set; }
}

public class PurchaseReportDto
{
    public decimal TotalPurchases { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalDue { get; set; }
    public int PurchaseCount { get; set; }
    public List<PurchaseDto> Purchases { get; set; } = [];
}

public class ProfitLossReportDto
{
    public decimal TotalRevenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit => TotalRevenue - CostOfGoodsSold;
    public decimal TotalExpenses { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal NetProfit => GrossProfit - TotalExpenses - TotalRefunds;
    public decimal NetMarginPercentage => TotalRevenue > 0 ? (NetProfit / TotalRevenue) * 100 : 0;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class ExpenseReportDto
{
    public decimal TotalExpenses { get; set; }
    public List<ExpenseSummaryDto> CategoryBreakdown { get; set; } = [];
    public List<ExpenseDto> Expenses { get; set; } = [];
}

public class WorkerPerformanceReportDto
{
    public string WorkerId { get; set; } = string.Empty;
    public string WorkerName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int InvoiceCount { get; set; }
    public decimal AverageInvoiceValue { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public decimal TotalDueCollected { get; set; }
    public decimal TotalReturnsHandled { get; set; }
    public decimal NetSales => TotalSales - TotalReturnsHandled;
}

public class StockTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public StockTransactionType TransactionType { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string PerformedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class StockAdjustmentRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int QuantityChange { get; set; } // positive or negative
    public StockTransactionType TransactionType { get; set; } = StockTransactionType.Adjustment;
    public string Reason { get; set; } = string.Empty;
}

public class ActivityLogDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public ActivityModule Module { get; set; }
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class BusinessSettingsDto
{
    public string StoreName { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = "৳";
    public decimal TaxRatePercentage { get; set; }
    public string InvoicePrefix { get; set; } = "INV-";
    public long NextInvoiceNumber { get; set; } = 1001;
    public decimal DefaultDiscountPercentage { get; set; }
    public decimal MaxWorkerDiscountPercentage { get; set; } = 5.0m;
    public int LowStockAlertThreshold { get; set; } = 5;
    public string ReceiptHeaderNote { get; set; } = string.Empty;
    public string ReceiptFooterNote { get; set; } = string.Empty;
    public int ThermalPaperWidthMm { get; set; } = 80;
    public string DefaultPrinterName { get; set; } = string.Empty;
    public bool AutoPrintInvoice { get; set; } = true;
}

public class BackupDto
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

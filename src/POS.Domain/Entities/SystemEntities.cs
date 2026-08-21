using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string SaleId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public string CashierName { get; set; } = string.Empty;

    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }

    public string FormattedReceiptText { get; set; } = string.Empty;
    public string TemplateType { get; set; } = "Thermal80mm"; // Thermal58mm, Thermal80mm, A4
}

public class StockTransaction : BaseEntity
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;

    public StockTransactionType TransactionType { get; set; }
    public int QuantityChange { get; set; } // positive or negative
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }

    public string ReferenceId { get; set; } = string.Empty; // SaleId, PurchaseId, ReturnId, etc.
    public string Notes { get; set; } = string.Empty;

    public string PerformedByUserId { get; set; } = string.Empty;
    public string PerformedByUserName { get; set; } = string.Empty;
}

public class ActivityLog : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public ActivityModule Module { get; set; }
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class BusinessSettings : BaseEntity
{
    public string StoreName { get; set; } = "NexPos Store";
    public string Tagline { get; set; } = "Professional Point of Sale System";
    public string Address { get; set; } = "123 Business Avenue, Suite 100";
    public string Phone { get; set; } = "+1 (555) 019-2834";
    public string Email { get; set; } = "support@nexpos.local";
    public string Website { get; set; } = "https://nexpos.local";

    public string CurrencySymbol { get; set; } = "৳";
    public decimal TaxRatePercentage { get; set; } = 0.0m;
    public string InvoicePrefix { get; set; } = "INV-";
    public long NextInvoiceNumber { get; set; } = 1001;

    public decimal DefaultDiscountPercentage { get; set; } = 0.0m;
    public decimal MaxWorkerDiscountPercentage { get; set; } = 5.0m;
    public int LowStockAlertThreshold { get; set; } = 5;

    public string ReceiptHeaderNote { get; set; } = "Thank you for shopping with us!";
    public string ReceiptFooterNote { get; set; } = "Please keep your receipt for returns within 7 days.";
    public int ThermalPaperWidthMm { get; set; } = 80;
    public string DefaultPrinterName { get; set; } = string.Empty;
    public bool AutoPrintInvoice { get; set; } = true;
}

public class BackupMetadata : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
}

using System;
using System.Collections.Generic;
using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class Sale : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public string? CustomerId { get; set; }
    public string CustomerName { get; set; } = "Walk-in Customer";
    public string CustomerPhone { get; set; } = string.Empty;

    public string CashierId { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public string? CashSessionId { get; set; }

    public List<SaleItem> Items { get; set; } = [];

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }

    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public decimal ChangeAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;
    public SaleStatus SaleStatus { get; set; } = SaleStatus.Completed;

    public string Notes { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
}

public class SaleItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal UnitPurchasePrice { get; set; } // Needed for COGS and Net Profit calculation
    public decimal UnitSellingPrice { get; set; }

    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }

    public int ReturnedQuantity { get; set; } = 0;
}

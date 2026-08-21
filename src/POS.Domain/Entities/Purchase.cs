using System;
using System.Collections.Generic;
using POS.Domain.Common;

namespace POS.Domain.Entities;

public class Purchase : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;

    public List<PurchaseItem> Items { get; set; } = [];

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }

    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class PurchaseItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPurchasePrice { get; set; }
    public decimal TotalPrice { get; set; }
}

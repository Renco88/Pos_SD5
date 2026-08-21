using System;
using System.Collections.Generic;
using POS.Domain.Common;

namespace POS.Domain.Entities;

public class Return : BaseEntity
{
    public string ReturnInvoiceNumber { get; set; } = string.Empty;
    public string OriginalSaleId { get; set; } = string.Empty;
    public string OriginalInvoiceNumber { get; set; } = string.Empty;

    public string? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public string CashierId { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;

    public List<ReturnItem> Items { get; set; } = [];

    public decimal TotalRefundAmount { get; set; }
    public string ReturnReason { get; set; } = string.Empty;
    public bool AdjustedFromCustomerDue { get; set; }
}

public class ReturnItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitSellingPrice { get; set; }
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

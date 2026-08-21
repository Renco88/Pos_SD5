using System;
using System.Collections.Generic;

namespace POS.Application.DTOs;

public class PurchaseDto
{
    public string Id { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public List<PurchaseItemDto> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class PurchaseItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPurchasePrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreatePurchaseRequest
{
    public string SupplierId { get; set; } = string.Empty;
    public List<CreatePurchaseItemRequest> Items { get; set; } = [];
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class CreatePurchaseItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPurchasePrice { get; set; }
}

public class PurchaseFilterRequest
{
    public string? SupplierId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

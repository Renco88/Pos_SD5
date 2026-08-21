using System;
using System.Collections.Generic;
using POS.Domain.Enums;

namespace POS.Application.DTOs;

public class SaleDto
{
    public string Id { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;

    public string? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public string CashierId { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;

    public List<SaleItemDto> Items { get; set; } = [];

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }

    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public decimal ChangeAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public SaleStatus SaleStatus { get; set; }

    public string Notes { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
}

public class SaleItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal UnitPurchasePrice { get; set; }
    public decimal UnitSellingPrice { get; set; }

    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalPrice { get; set; }

    public int ReturnedQuantity { get; set; }
}

public class CreateSaleRequest
{
    public string? CustomerId { get; set; }
    public string CustomerName { get; set; } = "Walk-in Customer";
    public string CustomerPhone { get; set; } = string.Empty;

    public List<CreateSaleItemRequest> Items { get; set; } = [];

    public decimal OverallDiscountPercentage { get; set; }
    public decimal OverallDiscountAmount { get; set; }

    public decimal PaidAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? CashSessionId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class CreateSaleItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal DiscountPercentage { get; set; }
}

public class SaleFilterRequest
{
    public string? SearchTerm { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? CustomerId { get; set; }
    public string? CashierId { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public SaleStatus? SaleStatus { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class HoldSaleDto
{
    public string HoldId { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
    public string CustomerName { get; set; } = "Walk-in Customer";
    public DateTime HoldTime { get; set; } = DateTime.UtcNow;
    public List<CreateSaleItemRequest> Items { get; set; } = [];
    public decimal TotalEstimate { get; set; }
}

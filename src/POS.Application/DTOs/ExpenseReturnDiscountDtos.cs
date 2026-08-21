using System;
using System.Collections.Generic;
using POS.Domain.Enums;

namespace POS.Application.DTOs;

public class ExpenseCategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ExpenseDto
{
    public string Id { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public bool IsVoided { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateExpenseRequest
{
    public string CategoryId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? CashSessionId { get; set; }
    public DateTime? ExpenseDate { get; set; }
}

public class ReturnDto
{
    public string Id { get; set; } = string.Empty;
    public string ReturnInvoiceNumber { get; set; } = string.Empty;
    public string OriginalSaleId { get; set; } = string.Empty;
    public string OriginalInvoiceNumber { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public List<ReturnItemDto> Items { get; set; } = [];
    public decimal TotalRefundAmount { get; set; }
    public string ReturnReason { get; set; } = string.Empty;
    public bool AdjustedFromCustomerDue { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReturnItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitSellingPrice { get; set; }
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class CreateReturnRequest
{
    public string OriginalInvoiceNumber { get; set; } = string.Empty;
    public List<CreateReturnItemRequest> Items { get; set; } = [];
    public string ReturnReason { get; set; } = string.Empty;
    public bool AdjustCustomerDue { get; set; } = false;
    public string? CashSessionId { get; set; }
}

public class CreateReturnItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string Reason { get; set; } = string.Empty;
}

public class DiscountRuleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinimumPurchaseAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public string? CategoryId { get; set; }
    public string? ProductId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class CreateDiscountRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
    public decimal DiscountValue { get; set; }
    public decimal? MinimumPurchaseAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public string? CategoryId { get; set; }
    public string? ProductId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

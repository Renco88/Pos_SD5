using System;
using System.Collections.Generic;
using POS.Domain.Enums;

namespace POS.Application.DTOs;

public class CustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal PreviousDue { get; set; }
    public decimal CurrentDue { get; set; }
    public decimal TotalPurchases { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal OpeningDue { get; set; } = 0;
}

public class UpdateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool? IsActive { get; set; }
}

public class CustomerPaymentRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string Note { get; set; } = string.Empty;
    public string? CashSessionId { get; set; }
}

public class CustomerPaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string Note { get; set; } = string.Empty;
    public string ReceivedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SupplierDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal PreviousDue { get; set; }
    public decimal CurrentDue { get; set; }
    public decimal TotalPurchases { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSupplierRequest
{
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal OpeningDue { get; set; } = 0;
}

public class UpdateSupplierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool? IsActive { get; set; }
}

public class SupplierPaymentRequest
{
    public string SupplierId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string Note { get; set; } = string.Empty;
}

public class SupplierPaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string Note { get; set; } = string.Empty;
    public string PaidByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DueSummaryDto
{
    public decimal TotalCustomerDue { get; set; }
    public decimal TotalSupplierDue { get; set; }
    public int DueCustomerCount { get; set; }
    public int DueSupplierCount { get; set; }
    public List<CustomerDto> TopDueCustomers { get; set; } = [];
    public List<SupplierDto> TopDueSuppliers { get; set; } = [];
}

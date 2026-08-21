using System;
using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal PreviousDue { get; set; }
    public decimal CurrentDue { get; set; }
    public decimal TotalPurchases { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SupplierPayment : BaseEntity
{
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string Note { get; set; } = string.Empty;
    public string PaidByUserId { get; set; } = string.Empty;
    public string PaidByUserName { get; set; } = string.Empty;
}

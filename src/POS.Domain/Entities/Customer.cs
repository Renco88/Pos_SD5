using System;
using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal PreviousDue { get; set; }
    public decimal CurrentDue { get; set; }
    public decimal TotalPurchases { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CustomerPayment : BaseEntity
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string Note { get; set; } = string.Empty;
    public string ReceivedByUserId { get; set; } = string.Empty;
    public string ReceivedByUserName { get; set; } = string.Empty;
}

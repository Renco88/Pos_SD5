namespace POS.Domain.Enums;

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    MobileBanking = 3,
    CreditDue = 4,
    SplitPartial = 5,
    BankTransfer = 6
}

public enum PaymentStatus
{
    Paid = 1,
    Partial = 2,
    Due = 3,
    Cancelled = 4,
    Refunded = 5
}

public enum SaleStatus
{
    Completed = 1,
    OnHold = 2,
    Cancelled = 3,
    Returned = 4,
    PartiallyReturned = 5
}

public enum StockTransactionType
{
    Purchase = 1,
    Sale = 2,
    Return = 3,
    Adjustment = 4,
    Damage = 5
}

public enum DiscountType
{
    Percentage = 1,
    FixedAmount = 2
}

public enum CashSessionStatus
{
    Open = 1,
    Closed = 2
}

public enum ActivityModule
{
    Auth = 1,
    Sales = 2,
    Products = 3,
    Categories = 4,
    Purchases = 5,
    Suppliers = 6,
    Customers = 7,
    Due = 8,
    Expenses = 9,
    Returns = 10,
    Discounts = 11,
    Workers = 12,
    Cash = 13,
    Settings = 14,
    Users = 15,
    Backup = 16,
    Reports = 17
}

using System;

namespace POS.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class InsufficientStockException : DomainException
{
    public string ProductId { get; }
    public string ProductName { get; }
    public int AvailableStock { get; }
    public int RequestedStock { get; }

    public InsufficientStockException(string productId, string productName, int available, int requested)
        : base($"Insufficient stock for '{productName}' (SKU/Id: {productId}). Available: {available}, Requested: {requested}")
    {
        ProductId = productId;
        ProductName = productName;
        AvailableStock = available;
        RequestedStock = requested;
    }
}

public class DiscountLimitExceededException : DomainException
{
    public decimal AttemptedDiscount { get; }
    public decimal MaxAllowedDiscount { get; }

    public DiscountLimitExceededException(decimal attempted, decimal maxAllowed)
        : base($"Worker discount limit exceeded. Attempted: {attempted:F2}%, Maximum permitted: {maxAllowed:F2}%.")
    {
        AttemptedDiscount = attempted;
        MaxAllowedDiscount = maxAllowed;
    }
}

public class NotFoundException : DomainException
{
    public string EntityName { get; }
    public string EntityId { get; }

    public NotFoundException(string entityName, string entityId)
        : base($"{entityName} with key '{entityId}' was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}

public class UnauthorizedDomainException : DomainException
{
    public UnauthorizedDomainException(string message) : base(message) { }
}

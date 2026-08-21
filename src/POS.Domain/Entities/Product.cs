using POS.Domain.Common;

namespace POS.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal WholesalePrice { get; set; }

    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; } = 5;
    public string Unit { get; set; } = "pcs";

    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;

    public decimal TaxRate { get; set; } = 0.0m;
    public decimal DiscountRate { get; set; } = 0.0m;
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsLowStock => StockQuantity > 0 && StockQuantity <= MinStockLevel;
    public bool IsOutOfStock => StockQuantity <= 0;
}

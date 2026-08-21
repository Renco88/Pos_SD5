using System;
using System.Collections.Generic;

namespace POS.Application.DTOs;

public class CategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class ProductDto
{
    public string Id { get; set; } = string.Empty;
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
    public int MinStockLevel { get; set; }
    public string Unit { get; set; } = "pcs";
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal TaxRate { get; set; }
    public decimal DiscountRate { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsOutOfStock { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; } = 5;
    public string Unit { get; set; } = "pcs";
    public string SupplierId { get; set; } = string.Empty;
    public decimal TaxRate { get; set; }
    public decimal DiscountRate { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public string? CategoryId { get; set; }
    public string? Brand { get; set; }
    public string? Description { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? WholesalePrice { get; set; }
    public int? StockQuantity { get; set; }
    public int? MinStockLevel { get; set; }
    public string? Unit { get; set; }
    public string? SupplierId { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? DiscountRate { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsActive { get; set; }
}

public class ProductFilterRequest
{
    public string? SearchTerm { get; set; }
    public string? CategoryId { get; set; }
    public bool? LowStockOnly { get; set; }
    public bool? OutOfStockOnly { get; set; }
    public bool? ActiveOnly { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

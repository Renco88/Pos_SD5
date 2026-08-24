using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using POS.Application.DTOs;
using POS.Application.Helpers;
using POS.Application.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;

namespace POS.Application.Services;

public class ProductService : IProductService
{
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<Supplier> _supplierRepo;
    private readonly IRepository<StockTransaction> _stockTxRepo;
    private readonly IActivityLogService _activityLog;

    public ProductService(
        IRepository<Product> productRepo,
        IRepository<Category> categoryRepo,
        IRepository<Supplier> supplierRepo,
        IRepository<StockTransaction> stockTxRepo,
        IActivityLogService activityLog)
    {
        _productRepo = productRepo;
        _categoryRepo = categoryRepo;
        _supplierRepo = supplierRepo;
        _stockTxRepo = stockTxRepo;
        _activityLog = activityLog;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(ProductFilterRequest request, CancellationToken ct = default)
    {
        var (safePageNumber, safePageSize) = ValidationHelpers.SanitizePagination(request.PageNumber, request.PageSize);
        var all = await _productRepo.GetAllAsync(ct);

        var query = all.AsEnumerable();

        if (request.ActiveOnly == true)
            query = query.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.CategoryId))
            query = query.Where(p => p.CategoryId == request.CategoryId);

        if (request.LowStockOnly == true)
            query = query.Where(p => p.IsLowStock);

        if (request.OutOfStockOnly == true)
            query = query.Where(p => p.IsOutOfStock);

        var term = ValidationHelpers.SanitizeSearchTerm(request.SearchTerm);
        if (term.Length > 0)
        {
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.SKU.ToLower().Contains(term) ||
                p.Barcode.ToLower().Contains(term) ||
                p.Brand.ToLower().Contains(term) ||
                p.CategoryName.ToLower().Contains(term));
        }

        var list = query.OrderBy(p => p.Name).ToList();
        var totalCount = list.Count;
        var paged = list
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .Select(MapToDto)
            .ToList();

        return new PagedResult<ProductDto>
        {
            Items = paged,
            TotalCount = totalCount,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<ProductDto> GetProductByIdAsync(string id, CancellationToken ct = default)
    {
        var product = await _productRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);
        return MapToDto(product);
    }

    public async Task<ProductDto?> FindBySkuOrBarcodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var trimmed = code.Trim().ToLower();
        var product = await _productRepo.FindOneAsync(p =>
            p.IsActive && (p.Barcode.ToLower() == trimmed || p.SKU.ToLower() == trimmed), ct);
        return product != null ? MapToDto(product) : null;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, string userId, string userName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Product name is required.");
        if (request.SellingPrice < 0)
            throw new DomainException("Selling price cannot be negative.");
        if (request.PurchasePrice < 0)
            throw new DomainException("Purchase price cannot be negative.");
        if (request.StockQuantity < 0)
            throw new DomainException("Stock quantity cannot be negative.");
        if (request.MinStockLevel < 0)
            throw new DomainException("Minimum stock level cannot be negative.");
        if (request.WholesalePrice < 0)
            throw new DomainException("Wholesale price cannot be negative.");
        if (request.TaxRate < 0 || request.TaxRate > 100)
            throw new DomainException("Tax rate must be between 0 and 100.");
        if (request.DiscountRate < 0 || request.DiscountRate > 100)
            throw new DomainException("Discount rate must be between 0 and 100.");
        if (request.WholesalePrice > 0 && request.WholesalePrice > request.SellingPrice)
            throw new DomainException("Wholesale price cannot be higher than selling price.");
        if (request.PurchasePrice > request.SellingPrice)
            throw new DomainException("Purchase price cannot be higher than selling price (risk of loss).");

        // Check duplicate SKU if SKU is provided
        if (!string.IsNullOrWhiteSpace(request.SKU))
        {
            var existingSku = await _productRepo.FindOneAsync(p => p.SKU.ToLower() == request.SKU.Trim().ToLower(), ct);
            if (existingSku != null)
                throw new DomainException($"Product with SKU '{request.SKU}' already exists.");
        }
        else
        {
            request.SKU = "SKU-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        }

        // Check duplicate Barcode if provided
        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var existingBarcode = await _productRepo.FindOneAsync(p => p.Barcode.ToLower() == request.Barcode.Trim().ToLower(), ct);
            if (existingBarcode != null)
                throw new DomainException($"Product with Barcode '{request.Barcode}' already exists.");
        }
        else
        {
            request.Barcode = request.SKU;
        }

        string categoryName = string.Empty;
        if (!string.IsNullOrWhiteSpace(request.CategoryId))
        {
            var cat = await _categoryRepo.GetByIdAsync(request.CategoryId, ct);
            if (cat != null) categoryName = cat.Name;
        }

        string supplierName = string.Empty;
        if (!string.IsNullOrWhiteSpace(request.SupplierId))
        {
            var sup = await _supplierRepo.GetByIdAsync(request.SupplierId, ct);
            if (sup != null) supplierName = sup.Name;
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            SKU = request.SKU.Trim(),
            Barcode = request.Barcode.Trim(),
            CategoryId = request.CategoryId,
            CategoryName = categoryName,
            Brand = request.Brand?.Trim() ?? string.Empty,
            Description = request.Description?.Trim() ?? string.Empty,
            PurchasePrice = request.PurchasePrice,
            SellingPrice = request.SellingPrice,
            WholesalePrice = request.WholesalePrice > 0 ? request.WholesalePrice : request.SellingPrice,
            StockQuantity = request.StockQuantity,
            MinStockLevel = request.MinStockLevel,
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? "pcs" : request.Unit.Trim(),
            SupplierId = request.SupplierId,
            SupplierName = supplierName,
            TaxRate = request.TaxRate,
            DiscountRate = request.DiscountRate,
            ImageUrl = request.ImageUrl ?? string.Empty,
            IsActive = true
        };

        var created = await _productRepo.AddAsync(product, ct);

        // Record initial stock transaction if initial stock > 0
        if (product.StockQuantity > 0)
        {
            await _stockTxRepo.AddAsync(new StockTransaction
            {
                ProductId = created.Id,
                ProductName = created.Name,
                SKU = created.SKU,
                TransactionType = StockTransactionType.Adjustment,
                QuantityChange = created.StockQuantity,
                QuantityBefore = 0,
                QuantityAfter = created.StockQuantity,
                ReferenceId = created.Id,
                Notes = "Initial product stock upon creation.",
                PerformedByUserId = userId,
                PerformedByUserName = userName
            }, ct);
        }

        await _activityLog.LogAsync(
            userId,
            userName,
            "CreateProduct",
            ActivityModule.Products,
            $"Created product '{product.Name}' (SKU: {product.SKU}) with stock {product.StockQuantity}.",
            ct: ct);

        return MapToDto(created);
    }

    public async Task<ProductDto> UpdateProductAsync(string id, UpdateProductRequest request, string userId, string userName, CancellationToken ct = default)
    {
        var product = await _productRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Product name is required.");
        if (request.SellingPrice.HasValue && request.SellingPrice.Value < 0)
            throw new DomainException("Selling price cannot be negative.");
        if (request.PurchasePrice.HasValue && request.PurchasePrice.Value < 0)
            throw new DomainException("Purchase price cannot be negative.");
        if (request.StockQuantity.HasValue && request.StockQuantity.Value < 0)
            throw new DomainException("Stock quantity cannot be negative.");
        if (request.MinStockLevel.HasValue && request.MinStockLevel.Value < 0)
            throw new DomainException("Minimum stock level cannot be negative.");
        if (request.WholesalePrice.HasValue && request.WholesalePrice.Value < 0)
            throw new DomainException("Wholesale price cannot be negative.");
        if (request.TaxRate.HasValue && (request.TaxRate.Value < 0 || request.TaxRate.Value > 100))
            throw new DomainException("Tax rate must be between 0 and 100.");
        if (request.DiscountRate.HasValue && (request.DiscountRate.Value < 0 || request.DiscountRate.Value > 100))
            throw new DomainException("Discount rate must be between 0 and 100.");

        var finalSellingPrice = request.SellingPrice ?? product.SellingPrice;
        var finalPurchasePrice = request.PurchasePrice ?? product.PurchasePrice;
        var finalWholesalePrice = request.WholesalePrice ?? product.WholesalePrice;

        if (finalWholesalePrice > 0 && finalWholesalePrice > finalSellingPrice)
            throw new DomainException("Wholesale price cannot be higher than selling price.");
        if (finalPurchasePrice > finalSellingPrice)
            throw new DomainException("Purchase price cannot be higher than selling price (risk of loss).");

        // Check duplicate SKU
        if (!string.IsNullOrWhiteSpace(request.SKU) && request.SKU.ToLower() != product.SKU.ToLower())
        {
            var existingSku = await _productRepo.FindOneAsync(p => p.Id != id && p.SKU.ToLower() == request.SKU.Trim().ToLower(), ct);
            if (existingSku != null)
                throw new DomainException($"Product with SKU '{request.SKU}' already exists.");
        }

        // Check duplicate Barcode
        if (!string.IsNullOrWhiteSpace(request.Barcode) && request.Barcode.ToLower() != product.Barcode.ToLower())
        {
            var existingBarcode = await _productRepo.FindOneAsync(p => p.Id != id && p.Barcode.ToLower() == request.Barcode.Trim().ToLower(), ct);
            if (existingBarcode != null)
                throw new DomainException($"Product with Barcode '{request.Barcode}' already exists.");
        }

        string categoryName = product.CategoryName;
        if (!string.IsNullOrWhiteSpace(request.CategoryId) && request.CategoryId != product.CategoryId)
        {
            var cat = await _categoryRepo.GetByIdAsync(request.CategoryId, ct);
            if (cat != null) categoryName = cat.Name;
        }

        string supplierName = product.SupplierName;
        if (!string.IsNullOrWhiteSpace(request.SupplierId) && request.SupplierId != product.SupplierId)
        {
            var sup = await _supplierRepo.GetByIdAsync(request.SupplierId, ct);
            if (sup != null) supplierName = sup.Name;
        }

        product.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.SKU)) product.SKU = request.SKU.Trim();
        if (!string.IsNullOrWhiteSpace(request.Barcode)) product.Barcode = request.Barcode.Trim();
        if (!string.IsNullOrWhiteSpace(request.CategoryId)) product.CategoryId = request.CategoryId;
        if (categoryName != product.CategoryName) product.CategoryName = categoryName;
        if (request.Brand != null) product.Brand = request.Brand.Trim() ?? string.Empty;
        if (request.Description != null) product.Description = request.Description.Trim() ?? string.Empty;
        if (request.PurchasePrice.HasValue) product.PurchasePrice = request.PurchasePrice.Value;
        if (request.SellingPrice.HasValue) product.SellingPrice = request.SellingPrice.Value;
        if (request.WholesalePrice.HasValue) product.WholesalePrice = request.WholesalePrice.Value;
        if (request.MinStockLevel.HasValue) product.MinStockLevel = request.MinStockLevel.Value;
        if (!string.IsNullOrWhiteSpace(request.Unit)) product.Unit = request.Unit.Trim();
        if (!string.IsNullOrWhiteSpace(request.SupplierId)) product.SupplierId = request.SupplierId;
        if (supplierName != product.SupplierName) product.SupplierName = supplierName;
        if (request.TaxRate.HasValue) product.TaxRate = request.TaxRate.Value;
        if (request.DiscountRate.HasValue) product.DiscountRate = request.DiscountRate.Value;
        if (request.ImageUrl != null) product.ImageUrl = request.ImageUrl ?? string.Empty;
        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;

        // Handle StockQuantity change with stock transaction log
        if (request.StockQuantity.HasValue && request.StockQuantity.Value != product.StockQuantity)
        {
            int qtyBefore = product.StockQuantity;
            int qtyAfter = request.StockQuantity.Value;
            product.StockQuantity = qtyAfter;

            await _stockTxRepo.AddAsync(new StockTransaction
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                TransactionType = StockTransactionType.Adjustment,
                QuantityChange = qtyAfter - qtyBefore,
                QuantityBefore = qtyBefore,
                QuantityAfter = qtyAfter,
                ReferenceId = product.Id,
                Notes = "Stock adjusted via product edit.",
                PerformedByUserId = userId,
                PerformedByUserName = userName
            }, ct);
        }

        product.UpdatedAt = DateTime.UtcNow;

        await _productRepo.UpdateAsync(product, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "UpdateProduct",
            ActivityModule.Products,
            $"Updated product '{product.Name}' (SKU: {product.SKU}).",
            ct: ct);

        return MapToDto(product);
    }

    public async Task<bool> DeleteProductAsync(string id, string userId, string userName, CancellationToken ct = default)
    {
        var product = await _productRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        var updated = await _productRepo.UpdateAsync(product, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "DeactivateProduct",
            ActivityModule.Products,
            $"Deactivated product '{product.Name}' (SKU: {product.SKU}).",
            ct: ct);

        return updated;
    }

    public async Task<List<ProductDto>> GetLowStockProductsAsync(CancellationToken ct = default)
    {
        var products = await _productRepo.FindAsync(p => p.IsActive, ct);
        return products.Where(p => p.IsLowStock || p.IsOutOfStock).Select(MapToDto).ToList();
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        SKU = p.SKU,
        Barcode = p.Barcode,
        CategoryId = p.CategoryId,
        CategoryName = p.CategoryName,
        Brand = p.Brand,
        Description = p.Description,
        PurchasePrice = p.PurchasePrice,
        SellingPrice = p.SellingPrice,
        WholesalePrice = p.WholesalePrice,
        StockQuantity = p.StockQuantity,
        MinStockLevel = p.MinStockLevel,
        Unit = p.Unit,
        SupplierId = p.SupplierId,
        SupplierName = p.SupplierName,
        TaxRate = p.TaxRate,
        DiscountRate = p.DiscountRate,
        ImageUrl = p.ImageUrl,
        IsActive = p.IsActive,
        IsLowStock = p.IsLowStock,
        IsOutOfStock = p.IsOutOfStock,
        CreatedAt = p.CreatedAt
    };
}

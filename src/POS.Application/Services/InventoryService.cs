using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using POS.Application.DTOs;
using POS.Application.Interfaces;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Exceptions;

namespace POS.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IRepository<StockTransaction> _stockTxRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IActivityLogService _activityLog;

    public InventoryService(
        IRepository<StockTransaction> stockTxRepo,
        IRepository<Product> productRepo,
        IActivityLogService activityLog)
    {
        _stockTxRepo = stockTxRepo;
        _productRepo = productRepo;
        _activityLog = activityLog;
    }

    public async Task<PagedResult<StockTransactionDto>> GetTransactionsAsync(string? productId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var all = await _stockTxRepo.GetAllAsync(ct);
        var query = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(productId))
            query = query.Where(t => t.ProductId == productId);

        var list = query.OrderByDescending(t => t.CreatedAt).ToList();
        var totalCount = list.Count;
        var paged = list
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new StockTransactionDto
            {
                Id = t.Id,
                ProductId = t.ProductId,
                ProductName = t.ProductName,
                SKU = t.SKU,
                TransactionType = t.TransactionType,
                QuantityChange = t.QuantityChange,
                QuantityBefore = t.QuantityBefore,
                QuantityAfter = t.QuantityAfter,
                ReferenceId = t.ReferenceId,
                Notes = t.Notes,
                PerformedByUserName = t.PerformedByUserName,
                CreatedAt = t.CreatedAt
            })
            .ToList();

        return new PagedResult<StockTransactionDto>
        {
            Items = paged,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<StockTransactionDto> AdjustStockAsync(StockAdjustmentRequest request, string userId, string userName, CancellationToken ct = default)
    {
        var product = await _productRepo.GetByIdAsync(request.ProductId, ct)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        int before = product.StockQuantity;
        int after = before + request.QuantityChange;

        if (after < 0)
        {
            throw new DomainException($"Cannot reduce stock below 0. Current: {before}, Change: {request.QuantityChange}");
        }

        product.StockQuantity = after;
        product.UpdatedAt = DateTime.UtcNow;
        await _productRepo.UpdateAsync(product, ct);

        var tx = new StockTransaction
        {
            ProductId = product.Id,
            ProductName = product.Name,
            SKU = product.SKU,
            TransactionType = request.TransactionType,
            QuantityChange = request.QuantityChange,
            QuantityBefore = before,
            QuantityAfter = after,
            ReferenceId = "MANUAL-ADJUST",
            Notes = request.Reason?.Trim() ?? "Manual stock adjustment",
            PerformedByUserId = userId,
            PerformedByUserName = userName
        };

        var created = await _stockTxRepo.AddAsync(tx, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "AdjustStock",
            ActivityModule.Products,
            $"Adjusted stock for '{product.Name}' (SKU: {product.SKU}) by {request.QuantityChange} (New stock: {after}). Reason: {request.Reason}",
            ct: ct);

        return new StockTransactionDto
        {
            Id = created.Id,
            ProductId = created.ProductId,
            ProductName = created.ProductName,
            SKU = created.SKU,
            TransactionType = created.TransactionType,
            QuantityChange = created.QuantityChange,
            QuantityBefore = created.QuantityBefore,
            QuantityAfter = created.QuantityAfter,
            ReferenceId = created.ReferenceId,
            Notes = created.Notes,
            PerformedByUserName = created.PerformedByUserName,
            CreatedAt = created.CreatedAt
        };
    }
}

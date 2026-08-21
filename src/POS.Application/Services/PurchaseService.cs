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

public class PurchaseService : IPurchaseService
{
    private readonly IRepository<Purchase> _purchaseRepo;
    private readonly IRepository<Supplier> _supplierRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<StockTransaction> _stockTxRepo;
    private readonly IRepository<BusinessSettings> _settingsRepo;
    private readonly IActivityLogService _activityLog;

    public PurchaseService(
        IRepository<Purchase> purchaseRepo,
        IRepository<Supplier> supplierRepo,
        IRepository<Product> productRepo,
        IRepository<StockTransaction> stockTxRepo,
        IRepository<BusinessSettings> settingsRepo,
        IActivityLogService activityLog)
    {
        _purchaseRepo = purchaseRepo;
        _supplierRepo = supplierRepo;
        _productRepo = productRepo;
        _stockTxRepo = stockTxRepo;
        _settingsRepo = settingsRepo;
        _activityLog = activityLog;
    }

    public async Task<PurchaseDto> CreatePurchaseAsync(CreatePurchaseRequest request, string userId, string userName, CancellationToken ct = default)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new DomainException("Purchase must contain at least one item.");

        var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId, ct)
            ?? throw new NotFoundException(nameof(Supplier), request.SupplierId);

        var purchaseItems = new List<PurchaseItem>();
        decimal subtotal = 0;

        var productUpdates = new List<(Product Product, int Quantity, decimal PurchasePrice, int Before, int After)>();

        foreach (var itemReq in request.Items)
        {
            if (itemReq.Quantity <= 0)
                throw new DomainException("Quantity must be greater than zero.");

            var product = await _productRepo.GetByIdAsync(itemReq.ProductId, ct)
                ?? throw new NotFoundException(nameof(Product), itemReq.ProductId);

            decimal lineTotal = itemReq.Quantity * itemReq.UnitPurchasePrice;
            subtotal += lineTotal;

            purchaseItems.Add(new PurchaseItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                Quantity = itemReq.Quantity,
                UnitPurchasePrice = itemReq.UnitPurchasePrice,
                TotalPrice = lineTotal
            });

            productUpdates.Add((product, itemReq.Quantity, itemReq.UnitPurchasePrice, product.StockQuantity, product.StockQuantity + itemReq.Quantity));
        }

        decimal grandTotal = Math.Max(0, subtotal - request.DiscountTotal + request.TaxTotal);
        decimal paidAmount = Math.Max(0, request.PaidAmount);
        decimal dueAmount = Math.Max(0, grandTotal - paidAmount);

        string invoiceNumber = $"PUR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var purchase = new Purchase
        {
            InvoiceNumber = invoiceNumber,
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            Items = purchaseItems,
            Subtotal = subtotal,
            DiscountTotal = request.DiscountTotal,
            TaxTotal = request.TaxTotal,
            GrandTotal = grandTotal,
            PaidAmount = paidAmount,
            DueAmount = dueAmount,
            PurchaseDate = DateTime.UtcNow,
            CreatedByUserId = userId,
            CreatedByUserName = userName,
            Notes = request.Notes?.Trim() ?? string.Empty
        };

        var savedPurchase = await _purchaseRepo.AddAsync(purchase, ct);

        // Update product stock and purchase price
        foreach (var (product, qty, price, before, after) in productUpdates)
        {
            product.StockQuantity = after;
            product.PurchasePrice = price; // update latest purchase price
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepo.UpdateAsync(product, ct);

            await _stockTxRepo.AddAsync(new StockTransaction
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                TransactionType = StockTransactionType.Purchase,
                QuantityChange = qty,
                QuantityBefore = before,
                QuantityAfter = after,
                ReferenceId = savedPurchase.Id,
                Notes = $"Purchase invoice {savedPurchase.InvoiceNumber} from {supplier.Name}",
                PerformedByUserId = userId,
                PerformedByUserName = userName
            }, ct);
        }

        // Update supplier balance
        supplier.CurrentDue += dueAmount;
        supplier.TotalPurchases += grandTotal;
        supplier.UpdatedAt = DateTime.UtcNow;
        await _supplierRepo.UpdateAsync(supplier, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "CreatePurchase",
            ActivityModule.Purchases,
            $"Created purchase {savedPurchase.InvoiceNumber} from {supplier.Name} for ৳{savedPurchase.GrandTotal:N2} (Due: ৳{dueAmount:N2}).",
            ct: ct);

        return MapToDto(savedPurchase);
    }

    public async Task<PagedResult<PurchaseDto>> GetPurchasesAsync(PurchaseFilterRequest request, CancellationToken ct = default)
    {
        var all = await _purchaseRepo.GetAllAsync(ct);
        var query = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.SupplierId))
            query = query.Where(p => p.SupplierId == request.SupplierId);

        if (request.StartDate.HasValue)
            query = query.Where(p => p.PurchaseDate >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(p => p.PurchaseDate <= request.EndDate.Value);

        var list = query.OrderByDescending(p => p.PurchaseDate).ToList();
        var totalCount = list.Count;
        var paged = list
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(MapToDto)
            .ToList();

        return new PagedResult<PurchaseDto>
        {
            Items = paged,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<PurchaseDto> GetPurchaseByIdAsync(string id, CancellationToken ct = default)
    {
        var p = await _purchaseRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Purchase), id);
        return MapToDto(p);
    }

    private static PurchaseDto MapToDto(Purchase p) => new()
    {
        Id = p.Id,
        InvoiceNumber = p.InvoiceNumber,
        SupplierId = p.SupplierId,
        SupplierName = p.SupplierName,
        Items = p.Items.Select(i => new PurchaseItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            SKU = i.SKU,
            Quantity = i.Quantity,
            UnitPurchasePrice = i.UnitPurchasePrice,
            TotalPrice = i.TotalPrice
        }).ToList(),
        Subtotal = p.Subtotal,
        DiscountTotal = p.DiscountTotal,
        TaxTotal = p.TaxTotal,
        GrandTotal = p.GrandTotal,
        PaidAmount = p.PaidAmount,
        DueAmount = p.DueAmount,
        PurchaseDate = p.PurchaseDate,
        CreatedByUserName = p.CreatedByUserName,
        Notes = p.Notes
    };
}

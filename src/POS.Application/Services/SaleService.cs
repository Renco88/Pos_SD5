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

public class SaleService : ISaleService
{
    private readonly IRepository<Sale> _saleRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Customer> _customerRepo;
    private readonly IRepository<StockTransaction> _stockTxRepo;
    private readonly IRepository<CashSession> _cashSessionRepo;
    private readonly IRepository<BusinessSettings> _settingsRepo;
    private readonly IActivityLogService _activityLog;

    public SaleService(
        IRepository<Sale> saleRepo,
        IRepository<Product> productRepo,
        IRepository<Customer> customerRepo,
        IRepository<StockTransaction> stockTxRepo,
        IRepository<CashSession> cashSessionRepo,
        IRepository<BusinessSettings> settingsRepo,
        IActivityLogService activityLog)
    {
        _saleRepo = saleRepo;
        _productRepo = productRepo;
        _customerRepo = customerRepo;
        _stockTxRepo = stockTxRepo;
        _cashSessionRepo = cashSessionRepo;
        _settingsRepo = settingsRepo;
        _activityLog = activityLog;
    }

    public async Task<SaleDto> ProcessSaleAsync(
        CreateSaleRequest request,
        string cashierId,
        string cashierName,
        decimal workerMaxDiscountPercent,
        CancellationToken ct = default)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            throw new DomainException("Cannot process a sale with an empty cart.");
        }

        // 1. Enforce worker discount limits server-side
        if (request.OverallDiscountPercentage > workerMaxDiscountPercent)
        {
            throw new DiscountLimitExceededException(request.OverallDiscountPercentage, workerMaxDiscountPercent);
        }

        foreach (var itemReq in request.Items)
        {
            if (itemReq.DiscountPercentage > workerMaxDiscountPercent)
            {
                throw new DiscountLimitExceededException(itemReq.DiscountPercentage, workerMaxDiscountPercent);
            }
        }

        // 2. Fetch and validate all products & stock
        var saleItems = new List<SaleItem>();
        decimal subtotal = 0;
        decimal totalItemDiscounts = 0;
        decimal totalTax = 0;

        var productUpdates = new List<(Product Product, int QuantityPurchased, int QuantityBefore, int QuantityAfter)>();

        foreach (var itemReq in request.Items)
        {
            if (itemReq.Quantity <= 0)
                throw new DomainException("Item quantity must be greater than zero.");

            var product = await _productRepo.GetByIdAsync(itemReq.ProductId, ct)
                ?? throw new NotFoundException(nameof(Product), itemReq.ProductId);

            if (!product.IsActive)
                throw new DomainException($"Product '{product.Name}' is no longer active.");

            if (product.StockQuantity < itemReq.Quantity)
            {
                throw new InsufficientStockException(product.Id, product.Name, product.StockQuantity, itemReq.Quantity);
            }

            // Server-side calculation of line total
            decimal unitSellingPrice = product.SellingPrice;
            decimal unitPurchasePrice = product.PurchasePrice;
            decimal lineBase = unitSellingPrice * itemReq.Quantity;

            // Line discount
            decimal itemDiscountPercent = Math.Max(0, Math.Min(100, itemReq.DiscountPercentage));
            decimal itemDiscountAmount = Math.Round(lineBase * (itemDiscountPercent / 100m), 2);
            decimal lineAfterDiscount = lineBase - itemDiscountAmount;

            // Line tax
            decimal lineTax = product.TaxRate > 0
                ? Math.Round(lineAfterDiscount * (product.TaxRate / 100m), 2)
                : 0;

            decimal lineTotal = lineAfterDiscount + lineTax;

            subtotal += lineBase;
            totalItemDiscounts += itemDiscountAmount;
            totalTax += lineTax;

            saleItems.Add(new SaleItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                Barcode = product.Barcode,
                Quantity = itemReq.Quantity,
                UnitPurchasePrice = unitPurchasePrice,
                UnitSellingPrice = unitSellingPrice,
                DiscountPercentage = itemDiscountPercent,
                DiscountAmount = itemDiscountAmount,
                TaxAmount = lineTax,
                TotalPrice = lineTotal,
                ReturnedQuantity = 0
            });

            productUpdates.Add((product, itemReq.Quantity, product.StockQuantity, product.StockQuantity - itemReq.Quantity));
        }

        // Overall discount calculation
        decimal overallDiscount = 0;
        if (request.OverallDiscountPercentage > 0)
        {
            overallDiscount = Math.Round(subtotal * (request.OverallDiscountPercentage / 100m), 2);
        }
        else if (request.OverallDiscountAmount > 0)
        {
            overallDiscount = Math.Min(subtotal, request.OverallDiscountAmount);
        }

        decimal grandTotal = Math.Max(0, subtotal - totalItemDiscounts - overallDiscount + totalTax);

        decimal paidAmount = Math.Max(0, request.PaidAmount);
        decimal dueAmount = 0;
        decimal changeAmount = 0;

        if (paidAmount >= grandTotal)
        {
            changeAmount = paidAmount - grandTotal;
            paidAmount = grandTotal; // Net paid towards invoice is grand total
            dueAmount = 0;
        }
        else
        {
            dueAmount = grandTotal - paidAmount;
            changeAmount = 0;
        }

        PaymentStatus paymentStatus = dueAmount == 0 ? PaymentStatus.Paid : (paidAmount > 0 ? PaymentStatus.Partial : PaymentStatus.Due);

        // 3. Generate Invoice Number from Settings
        var settingsList = await _settingsRepo.GetAllAsync(ct);
        var settings = settingsList.FirstOrDefault() ?? new BusinessSettings();
        string invoiceNumber = $"{settings.InvoicePrefix}{settings.NextInvoiceNumber++:D6}";
        settings.UpdatedAt = DateTime.UtcNow;
        if (settingsList.Count > 0)
            await _settingsRepo.UpdateAsync(settings, ct);
        else
            await _settingsRepo.AddAsync(settings, ct);

        // 4. Create Sale entity
        var sale = new Sale
        {
            InvoiceNumber = invoiceNumber,
            CustomerId = request.CustomerId,
            CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? "Walk-in Customer" : request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone?.Trim() ?? string.Empty,
            CashierId = cashierId,
            CashierName = cashierName,
            CashSessionId = request.CashSessionId,
            Items = saleItems,
            Subtotal = subtotal,
            DiscountTotal = totalItemDiscounts + overallDiscount,
            TaxTotal = totalTax,
            GrandTotal = grandTotal,
            PaidAmount = paidAmount,
            DueAmount = dueAmount,
            ChangeAmount = changeAmount,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = paymentStatus,
            SaleStatus = SaleStatus.Completed,
            Notes = request.Notes?.Trim() ?? string.Empty,
            SaleDate = DateTime.UtcNow
        };

        var savedSale = await _saleRepo.AddAsync(sale, ct);

        // 5. Deduct Stock & Record Stock Transactions
        foreach (var (product, qtyPurchased, before, after) in productUpdates)
        {
            product.StockQuantity = after;
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepo.UpdateAsync(product, ct);

            await _stockTxRepo.AddAsync(new StockTransaction
            {
                ProductId = product.Id,
                ProductName = product.Name,
                SKU = product.SKU,
                TransactionType = StockTransactionType.Sale,
                QuantityChange = -qtyPurchased,
                QuantityBefore = before,
                QuantityAfter = after,
                ReferenceId = savedSale.Id,
                Notes = $"Sale invoice {savedSale.InvoiceNumber}",
                PerformedByUserId = cashierId,
                PerformedByUserName = cashierName
            }, ct);
        }

        // 6. Update Customer Due and Total Purchases if customer registered
        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            var customer = await _customerRepo.GetByIdAsync(request.CustomerId, ct);
            if (customer != null)
            {
                customer.CurrentDue += dueAmount;
                customer.TotalPurchases += grandTotal;
                customer.UpdatedAt = DateTime.UtcNow;
                await _customerRepo.UpdateAsync(customer, ct);
            }
        }

        // 7. Update Cash Session if cash transaction
        if (!string.IsNullOrWhiteSpace(request.CashSessionId) &&
            (request.PaymentMethod == PaymentMethod.Cash || request.PaymentMethod == PaymentMethod.SplitPartial))
        {
            var session = await _cashSessionRepo.GetByIdAsync(request.CashSessionId, ct);
            if (session != null && session.Status == CashSessionStatus.Open)
            {
                session.CashSales += paidAmount;
                session.UpdatedAt = DateTime.UtcNow;
                await _cashSessionRepo.UpdateAsync(session, ct);
            }
        }

        // 8. Log Activity
        await _activityLog.LogAsync(
            cashierId,
            cashierName,
            "CreateSale",
            ActivityModule.Sales,
            $"Processed sale {savedSale.InvoiceNumber} for ৳{savedSale.GrandTotal:N2} ({savedSale.PaymentStatus}).",
            ct: ct);

        return MapToDto(savedSale);
    }

    public async Task<PagedResult<SaleDto>> GetSalesAsync(SaleFilterRequest request, CancellationToken ct = default)
    {
        var all = await _saleRepo.GetAllAsync(ct);
        var query = all.AsEnumerable();

        if (request.StartDate.HasValue)
            query = query.Where(s => s.SaleDate >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(s => s.SaleDate <= request.EndDate.Value);

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
            query = query.Where(s => s.CustomerId == request.CustomerId);

        if (!string.IsNullOrWhiteSpace(request.CashierId))
            query = query.Where(s => s.CashierId == request.CashierId);

        if (request.PaymentStatus.HasValue)
            query = query.Where(s => s.PaymentStatus == request.PaymentStatus.Value);

        if (request.SaleStatus.HasValue)
            query = query.Where(s => s.SaleStatus == request.SaleStatus.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(s =>
                s.InvoiceNumber.ToLower().Contains(term) ||
                s.CustomerName.ToLower().Contains(term) ||
                s.CustomerPhone.ToLower().Contains(term) ||
                s.CashierName.ToLower().Contains(term));
        }

        var list = query.OrderByDescending(s => s.SaleDate).ToList();
        var totalCount = list.Count;
        var paged = list
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(MapToDto)
            .ToList();

        return new PagedResult<SaleDto>
        {
            Items = paged,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task<SaleDto> GetSaleByIdAsync(string id, CancellationToken ct = default)
    {
        var sale = await _saleRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Sale), id);
        return MapToDto(sale);
    }

    public async Task<SaleDto> GetSaleByInvoiceNumberAsync(string invoiceNumber, CancellationToken ct = default)
    {
        var sale = await _saleRepo.FindOneAsync(s => s.InvoiceNumber.ToLower() == invoiceNumber.Trim().ToLower(), ct)
            ?? throw new NotFoundException(nameof(Sale), invoiceNumber);
        return MapToDto(sale);
    }

    public async Task<bool> CancelSaleAsync(string id, string reason, string userId, string userName, CancellationToken ct = default)
    {
        var sale = await _saleRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Sale), id);

        if (sale.SaleStatus == SaleStatus.Cancelled)
            throw new DomainException("Sale is already cancelled.");

        // Restore stock
        foreach (var item in sale.Items)
        {
            var product = await _productRepo.GetByIdAsync(item.ProductId, ct);
            if (product != null)
            {
                int before = product.StockQuantity;
                int after = product.StockQuantity + item.Quantity;
                product.StockQuantity = after;
                product.UpdatedAt = DateTime.UtcNow;
                await _productRepo.UpdateAsync(product, ct);

                await _stockTxRepo.AddAsync(new StockTransaction
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    SKU = product.SKU,
                    TransactionType = StockTransactionType.Adjustment,
                    QuantityChange = item.Quantity,
                    QuantityBefore = before,
                    QuantityAfter = after,
                    ReferenceId = sale.Id,
                    Notes = $"Cancelled Sale {sale.InvoiceNumber}: {reason}",
                    PerformedByUserId = userId,
                    PerformedByUserName = userName
                }, ct);
            }
        }

        // Adjust customer due if due was on invoice
        if (!string.IsNullOrWhiteSpace(sale.CustomerId) && sale.DueAmount > 0)
        {
            var customer = await _customerRepo.GetByIdAsync(sale.CustomerId, ct);
            if (customer != null)
            {
                customer.CurrentDue = Math.Max(0, customer.CurrentDue - sale.DueAmount);
                customer.TotalPurchases = Math.Max(0, customer.TotalPurchases - sale.GrandTotal);
                customer.UpdatedAt = DateTime.UtcNow;
                await _customerRepo.UpdateAsync(customer, ct);
            }
        }

        sale.SaleStatus = SaleStatus.Cancelled;
        sale.Notes = string.IsNullOrWhiteSpace(sale.Notes) ? $"Cancelled: {reason}" : $"{sale.Notes} | Cancelled: {reason}";
        sale.UpdatedAt = DateTime.UtcNow;
        var updated = await _saleRepo.UpdateAsync(sale, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "CancelSale",
            ActivityModule.Sales,
            $"Cancelled sale {sale.InvoiceNumber}. Reason: {reason}",
            ct: ct);

        return updated;
    }

    private static SaleDto MapToDto(Sale s) => new()
    {
        Id = s.Id,
        InvoiceNumber = s.InvoiceNumber,
        CustomerId = s.CustomerId,
        CustomerName = s.CustomerName,
        CustomerPhone = s.CustomerPhone,
        CashierId = s.CashierId,
        CashierName = s.CashierName,
        Items = s.Items.Select(i => new SaleItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            SKU = i.SKU,
            Barcode = i.Barcode,
            Quantity = i.Quantity,
            UnitPurchasePrice = i.UnitPurchasePrice,
            UnitSellingPrice = i.UnitSellingPrice,
            DiscountPercentage = i.DiscountPercentage,
            DiscountAmount = i.DiscountAmount,
            TaxAmount = i.TaxAmount,
            TotalPrice = i.TotalPrice,
            ReturnedQuantity = i.ReturnedQuantity
        }).ToList(),
        Subtotal = s.Subtotal,
        DiscountTotal = s.DiscountTotal,
        TaxTotal = s.TaxTotal,
        GrandTotal = s.GrandTotal,
        PaidAmount = s.PaidAmount,
        DueAmount = s.DueAmount,
        ChangeAmount = s.ChangeAmount,
        PaymentMethod = s.PaymentMethod,
        PaymentStatus = s.PaymentStatus,
        SaleStatus = s.SaleStatus,
        Notes = s.Notes,
        SaleDate = s.SaleDate
    };
}

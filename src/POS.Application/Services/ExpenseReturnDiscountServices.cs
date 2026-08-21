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

public class ExpenseService : IExpenseService
{
    private readonly IRepository<Expense> _expenseRepo;
    private readonly IRepository<ExpenseCategory> _categoryRepo;
    private readonly IRepository<CashSession> _cashSessionRepo;
    private readonly IActivityLogService _activityLog;

    public ExpenseService(
        IRepository<Expense> expenseRepo,
        IRepository<ExpenseCategory> categoryRepo,
        IRepository<CashSession> cashSessionRepo,
        IActivityLogService activityLog)
    {
        _expenseRepo = expenseRepo;
        _categoryRepo = categoryRepo;
        _cashSessionRepo = cashSessionRepo;
        _activityLog = activityLog;
    }

    public async Task<List<ExpenseDto>> GetExpensesAsync(DateTime? from, DateTime? to, string? categoryId, CancellationToken ct = default)
    {
        var expenses = await _expenseRepo.GetAllAsync(ct);
        var query = expenses.AsEnumerable();

        if (from.HasValue)
            query = query.Where(e => e.ExpenseDate >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.ExpenseDate <= to.Value);

        if (!string.IsNullOrWhiteSpace(categoryId))
            query = query.Where(e => e.CategoryId == categoryId);

        return query.OrderByDescending(e => e.ExpenseDate).Select(e => new ExpenseDto
        {
            Id = e.Id,
            CategoryId = e.CategoryId,
            CategoryName = e.CategoryName,
            Description = e.Description,
            Amount = e.Amount,
            PaymentMethod = e.PaymentMethod,
            CreatedByUserName = e.CreatedByUserName,
            ExpenseDate = e.ExpenseDate,
            IsVoided = e.IsVoided,
            CreatedAt = e.CreatedAt
        }).ToList();
    }

    public async Task<ExpenseDto> CreateExpenseAsync(CreateExpenseRequest request, string userId, string userName, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new DomainException("Expense amount must be greater than zero.");

        string categoryName = "General";
        if (!string.IsNullOrWhiteSpace(request.CategoryId))
        {
            var cat = await _categoryRepo.GetByIdAsync(request.CategoryId, ct);
            if (cat != null) categoryName = cat.Name;
        }

        var expense = new Expense
        {
            CategoryId = request.CategoryId,
            CategoryName = categoryName,
            Description = request.Description?.Trim() ?? string.Empty,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            CreatedByUserId = userId,
            CreatedByUserName = userName,
            ExpenseDate = request.ExpenseDate ?? DateTime.UtcNow,
            IsVoided = false
        };

        var saved = await _expenseRepo.AddAsync(expense, ct);

        // Update cash session if paid in cash
        if (request.PaymentMethod == PaymentMethod.Cash && !string.IsNullOrWhiteSpace(request.CashSessionId))
        {
            var session = await _cashSessionRepo.GetByIdAsync(request.CashSessionId, ct);
            if (session != null && session.Status == CashSessionStatus.Open)
            {
                session.CashExpenses += request.Amount;
                session.UpdatedAt = DateTime.UtcNow;
                await _cashSessionRepo.UpdateAsync(session, ct);
            }
        }

        await _activityLog.LogAsync(
            userId,
            userName,
            "CreateExpense",
            ActivityModule.Expenses,
            $"Created expense '{expense.Description}' for ৳{expense.Amount:N2} ({categoryName}).",
            ct: ct);

        return new ExpenseDto
        {
            Id = saved.Id,
            CategoryId = saved.CategoryId,
            CategoryName = saved.CategoryName,
            Description = saved.Description,
            Amount = saved.Amount,
            PaymentMethod = saved.PaymentMethod,
            CreatedByUserName = saved.CreatedByUserName,
            ExpenseDate = saved.ExpenseDate,
            IsVoided = saved.IsVoided,
            CreatedAt = saved.CreatedAt
        };
    }

    public async Task<bool> VoidExpenseAsync(string id, string userId, string userName, CancellationToken ct = default)
    {
        var exp = await _expenseRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Expense), id);

        exp.IsVoided = true;
        exp.UpdatedAt = DateTime.UtcNow;
        var updated = await _expenseRepo.UpdateAsync(exp, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "VoidExpense",
            ActivityModule.Expenses,
            $"Voided expense '{exp.Description}' for ৳{exp.Amount:N2}.",
            ct: ct);

        return updated;
    }

    public async Task<List<ExpenseCategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var cats = await _categoryRepo.GetAllAsync(ct);
        return cats.Select(c => new ExpenseCategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description
        }).OrderBy(c => c.Name).ToList();
    }

    public async Task<ExpenseCategoryDto> CreateCategoryAsync(string name, string description, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Expense category name is required.");

        var cat = new ExpenseCategory
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty
        };

        var saved = await _categoryRepo.AddAsync(cat, ct);
        return new ExpenseCategoryDto { Id = saved.Id, Name = saved.Name, Description = saved.Description };
    }
}

public class ReturnService : IReturnService
{
    private readonly IRepository<Return> _returnRepo;
    private readonly IRepository<Sale> _saleRepo;
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Customer> _customerRepo;
    private readonly IRepository<StockTransaction> _stockTxRepo;
    private readonly IRepository<CashSession> _cashSessionRepo;
    private readonly IActivityLogService _activityLog;

    public ReturnService(
        IRepository<Return> returnRepo,
        IRepository<Sale> saleRepo,
        IRepository<Product> productRepo,
        IRepository<Customer> customerRepo,
        IRepository<StockTransaction> stockTxRepo,
        IRepository<CashSession> cashSessionRepo,
        IActivityLogService activityLog)
    {
        _returnRepo = returnRepo;
        _saleRepo = saleRepo;
        _productRepo = productRepo;
        _customerRepo = customerRepo;
        _stockTxRepo = stockTxRepo;
        _cashSessionRepo = cashSessionRepo;
        _activityLog = activityLog;
    }

    public async Task<ReturnDto> ProcessReturnAsync(CreateReturnRequest request, string cashierId, string cashierName, CancellationToken ct = default)
    {
        var sale = await _saleRepo.FindOneAsync(s => s.InvoiceNumber.ToLower() == request.OriginalInvoiceNumber.Trim().ToLower(), ct)
            ?? throw new NotFoundException(nameof(Sale), request.OriginalInvoiceNumber);

        if (sale.SaleStatus == SaleStatus.Cancelled)
            throw new DomainException("Cannot process return for a cancelled sale.");

        if (request.Items == null || request.Items.Count == 0)
            throw new DomainException("Return must contain at least one item.");

        var returnItems = new List<ReturnItem>();
        decimal totalRefund = 0;

        foreach (var itemReq in request.Items)
        {
            var saleItem = sale.Items.FirstOrDefault(i => i.ProductId == itemReq.ProductId)
                ?? throw new DomainException($"Product was not part of original sale '{sale.InvoiceNumber}'.");

            int maxReturnable = saleItem.Quantity - saleItem.ReturnedQuantity;
            if (itemReq.Quantity <= 0 || itemReq.Quantity > maxReturnable)
            {
                throw new DomainException($"Invalid return quantity for '{saleItem.ProductName}'. Max returnable: {maxReturnable}, Requested: {itemReq.Quantity}");
            }

            decimal unitRefundPrice = saleItem.Quantity > 0 ? (saleItem.TotalPrice / saleItem.Quantity) : saleItem.UnitSellingPrice;
            decimal refundAmount = Math.Round(unitRefundPrice * itemReq.Quantity, 2);
            totalRefund += refundAmount;

            saleItem.ReturnedQuantity += itemReq.Quantity;

            returnItems.Add(new ReturnItem
            {
                ProductId = saleItem.ProductId,
                ProductName = saleItem.ProductName,
                SKU = saleItem.SKU,
                Quantity = itemReq.Quantity,
                UnitSellingPrice = saleItem.UnitSellingPrice,
                RefundAmount = refundAmount,
                Reason = itemReq.Reason?.Trim() ?? request.ReturnReason
            });

            // Restore product stock
            var product = await _productRepo.GetByIdAsync(saleItem.ProductId, ct);
            if (product != null)
            {
                int before = product.StockQuantity;
                int after = before + itemReq.Quantity;
                product.StockQuantity = after;
                product.UpdatedAt = DateTime.UtcNow;
                await _productRepo.UpdateAsync(product, ct);

                await _stockTxRepo.AddAsync(new StockTransaction
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    SKU = product.SKU,
                    TransactionType = StockTransactionType.Return,
                    QuantityChange = itemReq.Quantity,
                    QuantityBefore = before,
                    QuantityAfter = after,
                    ReferenceId = sale.Id,
                    Notes = $"Return from sale {sale.InvoiceNumber}: {request.ReturnReason}",
                    PerformedByUserId = cashierId,
                    PerformedByUserName = cashierName
                }, ct);
            }
        }

        // Update Sale status
        bool allReturned = sale.Items.All(i => i.ReturnedQuantity >= i.Quantity);
        sale.SaleStatus = allReturned ? SaleStatus.Returned : SaleStatus.PartiallyReturned;
        sale.UpdatedAt = DateTime.UtcNow;
        await _saleRepo.UpdateAsync(sale, ct);

        // Adjust customer due or cash refund
        if (request.AdjustCustomerDue && !string.IsNullOrWhiteSpace(sale.CustomerId))
        {
            var customer = await _customerRepo.GetByIdAsync(sale.CustomerId, ct);
            if (customer != null)
            {
                customer.CurrentDue = Math.Max(0, customer.CurrentDue - totalRefund);
                customer.UpdatedAt = DateTime.UtcNow;
                await _customerRepo.UpdateAsync(customer, ct);
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.CashSessionId))
        {
            // Cash refund from register
            var session = await _cashSessionRepo.GetByIdAsync(request.CashSessionId, ct);
            if (session != null && session.Status == CashSessionStatus.Open)
            {
                session.CashAdjustments -= totalRefund; // cash paid out as refund
                session.UpdatedAt = DateTime.UtcNow;
                await _cashSessionRepo.UpdateAsync(session, ct);
            }
        }

        string returnInvoiceNumber = $"RET-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        var returnDoc = new Return
        {
            ReturnInvoiceNumber = returnInvoiceNumber,
            OriginalSaleId = sale.Id,
            OriginalInvoiceNumber = sale.InvoiceNumber,
            CustomerId = sale.CustomerId,
            CustomerName = sale.CustomerName,
            CashierId = cashierId,
            CashierName = cashierName,
            Items = returnItems,
            TotalRefundAmount = totalRefund,
            ReturnReason = request.ReturnReason?.Trim() ?? string.Empty,
            AdjustedFromCustomerDue = request.AdjustCustomerDue
        };

        var saved = await _returnRepo.AddAsync(returnDoc, ct);

        await _activityLog.LogAsync(
            cashierId,
            cashierName,
            "ProcessReturn",
            ActivityModule.Returns,
            $"Processed return {saved.ReturnInvoiceNumber} for original sale {sale.InvoiceNumber} (Refund: ৳{totalRefund:N2}).",
            ct: ct);

        return new ReturnDto
        {
            Id = saved.Id,
            ReturnInvoiceNumber = saved.ReturnInvoiceNumber,
            OriginalSaleId = saved.OriginalSaleId,
            OriginalInvoiceNumber = saved.OriginalInvoiceNumber,
            CustomerId = saved.CustomerId,
            CustomerName = saved.CustomerName,
            CashierName = saved.CashierName,
            Items = saved.Items.Select(i => new ReturnItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                SKU = i.SKU,
                Quantity = i.Quantity,
                UnitSellingPrice = i.UnitSellingPrice,
                RefundAmount = i.RefundAmount,
                Reason = i.Reason
            }).ToList(),
            TotalRefundAmount = saved.TotalRefundAmount,
            ReturnReason = saved.ReturnReason,
            AdjustedFromCustomerDue = saved.AdjustedFromCustomerDue,
            CreatedAt = saved.CreatedAt
        };
    }

    public async Task<PagedResult<ReturnDto>> GetReturnsAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var all = await _returnRepo.GetAllAsync(ct);
        var list = all.OrderByDescending(r => r.CreatedAt).ToList();
        var totalCount = list.Count;
        var paged = list
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReturnDto
            {
                Id = r.Id,
                ReturnInvoiceNumber = r.ReturnInvoiceNumber,
                OriginalSaleId = r.OriginalSaleId,
                OriginalInvoiceNumber = r.OriginalInvoiceNumber,
                CustomerId = r.CustomerId,
                CustomerName = r.CustomerName,
                CashierName = r.CashierName,
                Items = r.Items.Select(i => new ReturnItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    SKU = i.SKU,
                    Quantity = i.Quantity,
                    UnitSellingPrice = i.UnitSellingPrice,
                    RefundAmount = i.RefundAmount,
                    Reason = i.Reason
                }).ToList(),
                TotalRefundAmount = r.TotalRefundAmount,
                ReturnReason = r.ReturnReason,
                AdjustedFromCustomerDue = r.AdjustedFromCustomerDue,
                CreatedAt = r.CreatedAt
            })
            .ToList();

        return new PagedResult<ReturnDto>
        {
            Items = paged,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<ReturnDto> GetReturnByIdAsync(string id, CancellationToken ct = default)
    {
        var r = await _returnRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Return), id);

        return new ReturnDto
        {
            Id = r.Id,
            ReturnInvoiceNumber = r.ReturnInvoiceNumber,
            OriginalSaleId = r.OriginalSaleId,
            OriginalInvoiceNumber = r.OriginalInvoiceNumber,
            CustomerId = r.CustomerId,
            CustomerName = r.CustomerName,
            CashierName = r.CashierName,
            Items = r.Items.Select(i => new ReturnItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                SKU = i.SKU,
                Quantity = i.Quantity,
                UnitSellingPrice = i.UnitSellingPrice,
                RefundAmount = i.RefundAmount,
                Reason = i.Reason
            }).ToList(),
            TotalRefundAmount = r.TotalRefundAmount,
            ReturnReason = r.ReturnReason,
            AdjustedFromCustomerDue = r.AdjustedFromCustomerDue,
            CreatedAt = r.CreatedAt
        };
    }
}

public class DiscountService : IDiscountService
{
    private readonly IRepository<DiscountRule> _discountRepo;
    private readonly IActivityLogService _activityLog;

    public DiscountService(IRepository<DiscountRule> discountRepo, IActivityLogService activityLog)
    {
        _discountRepo = discountRepo;
        _activityLog = activityLog;
    }

    public async Task<List<DiscountRuleDto>> GetAllRulesAsync(CancellationToken ct = default)
    {
        var rules = await _discountRepo.GetAllAsync(ct);
        return rules.Select(r => new DiscountRuleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            DiscountType = r.DiscountType,
            DiscountValue = r.DiscountValue,
            MinimumPurchaseAmount = r.MinimumPurchaseAmount,
            MaximumDiscountAmount = r.MaximumDiscountAmount,
            CategoryId = r.CategoryId,
            ProductId = r.ProductId,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            IsActive = r.IsActive
        }).ToList();
    }

    public async Task<DiscountRuleDto> CreateRuleAsync(CreateDiscountRuleRequest request, string userId, string userName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Discount rule name is required.");

        var rule = new DiscountRule
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinimumPurchaseAmount = request.MinimumPurchaseAmount,
            MaximumDiscountAmount = request.MaximumDiscountAmount,
            CategoryId = request.CategoryId,
            ProductId = request.ProductId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true
        };

        var saved = await _discountRepo.AddAsync(rule, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "CreateDiscountRule",
            ActivityModule.Discounts,
            $"Created discount rule '{rule.Name}' ({rule.DiscountValue}{(rule.DiscountType == DiscountType.Percentage ? "%" : "৳")}).",
            ct: ct);

        return new DiscountRuleDto
        {
            Id = saved.Id,
            Name = saved.Name,
            Description = saved.Description,
            DiscountType = saved.DiscountType,
            DiscountValue = saved.DiscountValue,
            MinimumPurchaseAmount = saved.MinimumPurchaseAmount,
            MaximumDiscountAmount = saved.MaximumDiscountAmount,
            CategoryId = saved.CategoryId,
            ProductId = saved.ProductId,
            StartDate = saved.StartDate,
            EndDate = saved.EndDate,
            IsActive = saved.IsActive
        };
    }

    public async Task<DiscountRuleDto> UpdateRuleAsync(string id, CreateDiscountRuleRequest request, string userId, string userName, CancellationToken ct = default)
    {
        var rule = await _discountRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(DiscountRule), id);

        rule.Name = request.Name.Trim();
        rule.Description = request.Description?.Trim() ?? string.Empty;
        rule.DiscountType = request.DiscountType;
        rule.DiscountValue = request.DiscountValue;
        rule.MinimumPurchaseAmount = request.MinimumPurchaseAmount;
        rule.MaximumDiscountAmount = request.MaximumDiscountAmount;
        rule.CategoryId = request.CategoryId;
        rule.ProductId = request.ProductId;
        rule.StartDate = request.StartDate;
        rule.EndDate = request.EndDate;
        rule.UpdatedAt = DateTime.UtcNow;

        await _discountRepo.UpdateAsync(rule, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "UpdateDiscountRule",
            ActivityModule.Discounts,
            $"Updated discount rule '{rule.Name}'.",
            ct: ct);

        return new DiscountRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Description = rule.Description,
            DiscountType = rule.DiscountType,
            DiscountValue = rule.DiscountValue,
            MinimumPurchaseAmount = rule.MinimumPurchaseAmount,
            MaximumDiscountAmount = rule.MaximumDiscountAmount,
            CategoryId = rule.CategoryId,
            ProductId = rule.ProductId,
            StartDate = rule.StartDate,
            EndDate = rule.EndDate,
            IsActive = rule.IsActive
        };
    }

    public async Task<bool> DeleteRuleAsync(string id, string userId, string userName, CancellationToken ct = default)
    {
        var rule = await _discountRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(DiscountRule), id);

        var deleted = await _discountRepo.DeleteAsync(id, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "DeleteDiscountRule",
            ActivityModule.Discounts,
            $"Deleted discount rule '{rule.Name}'.",
            ct: ct);

        return deleted;
    }

    public Task ValidateWorkerDiscountAsync(decimal attemptedPercent, decimal maxAllowedPercent)
    {
        if (attemptedPercent > maxAllowedPercent)
        {
            throw new DiscountLimitExceededException(attemptedPercent, maxAllowedPercent);
        }
        return Task.CompletedTask;
    }
}

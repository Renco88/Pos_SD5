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

public class SupplierService : ISupplierService
{
    private readonly IRepository<Supplier> _supplierRepo;
    private readonly IRepository<SupplierPayment> _paymentRepo;
    private readonly IActivityLogService _activityLog;

    public SupplierService(
        IRepository<Supplier> supplierRepo,
        IRepository<SupplierPayment> paymentRepo,
        IActivityLogService activityLog)
    {
        _supplierRepo = supplierRepo;
        _paymentRepo = paymentRepo;
        _activityLog = activityLog;
    }

    public async Task<List<SupplierDto>> GetAllSuppliersAsync(CancellationToken ct = default)
    {
        var suppliers = await _supplierRepo.GetAllAsync(ct);
        return suppliers.Select(MapToDto).OrderBy(s => s.Name).ToList();
    }

    public async Task<SupplierDto> GetSupplierByIdAsync(string id, CancellationToken ct = default)
    {
        var s = await _supplierRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Supplier), id);
        return MapToDto(s);
    }

    public async Task<SupplierDto> CreateSupplierAsync(CreateSupplierRequest request, string userId, string userName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Supplier name is required.");

        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            Company = request.Company?.Trim() ?? string.Empty,
            Phone = request.Phone?.Trim() ?? string.Empty,
            Email = request.Email?.Trim() ?? string.Empty,
            Address = request.Address?.Trim() ?? string.Empty,
            PreviousDue = request.OpeningDue,
            CurrentDue = request.OpeningDue,
            IsActive = true
        };

        var created = await _supplierRepo.AddAsync(supplier, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "CreateSupplier",
            ActivityModule.Suppliers,
            $"Created supplier '{supplier.Name}'.",
            ct: ct);

        return MapToDto(created);
    }

    public async Task<SupplierDto> UpdateSupplierAsync(string id, UpdateSupplierRequest request, string userId, string userName, CancellationToken ct = default)
    {
        var s = await _supplierRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Supplier), id);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Supplier name is required.");

        s.Name = request.Name.Trim();
        if (request.Company != null) s.Company = request.Company.Trim() ?? string.Empty;
        if (request.Phone != null) s.Phone = request.Phone.Trim() ?? string.Empty;
        if (request.Email != null) s.Email = request.Email.Trim() ?? string.Empty;
        if (request.Address != null) s.Address = request.Address.Trim() ?? string.Empty;
        if (request.IsActive.HasValue) s.IsActive = request.IsActive.Value;
        s.UpdatedAt = DateTime.UtcNow;

        await _supplierRepo.UpdateAsync(s, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "UpdateSupplier",
            ActivityModule.Suppliers,
            $"Updated supplier '{s.Name}'.",
            ct: ct);

        return MapToDto(s);
    }

    public async Task<SupplierPaymentDto> RecordPaymentAsync(SupplierPaymentRequest request, string userId, string userName, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");

        var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId, ct)
            ?? throw new NotFoundException(nameof(Supplier), request.SupplierId);

        supplier.CurrentDue = Math.Max(0, supplier.CurrentDue - request.Amount);
        supplier.UpdatedAt = DateTime.UtcNow;
        await _supplierRepo.UpdateAsync(supplier, ct);

        var payment = new SupplierPayment
        {
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Note = request.Note?.Trim() ?? string.Empty,
            PaidByUserId = userId,
            PaidByUserName = userName
        };

        var saved = await _paymentRepo.AddAsync(payment, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "SupplierPayment",
            ActivityModule.Due,
            $"Paid ৳{request.Amount:N2} to supplier '{supplier.Name}' (Remaining due: ৳{supplier.CurrentDue:N2}).",
            ct: ct);

        return new SupplierPaymentDto
        {
            Id = saved.Id,
            SupplierId = saved.SupplierId,
            SupplierName = saved.SupplierName,
            Amount = saved.Amount,
            PaymentMethod = saved.PaymentMethod,
            Note = saved.Note,
            PaidByUserName = saved.PaidByUserName,
            CreatedAt = saved.CreatedAt
        };
    }

    public async Task<List<SupplierPaymentDto>> GetPaymentHistoryAsync(string supplierId, CancellationToken ct = default)
    {
        var payments = await _paymentRepo.FindAsync(p => p.SupplierId == supplierId, ct);
        return payments.OrderByDescending(p => p.CreatedAt).Select(p => new SupplierPaymentDto
        {
            Id = p.Id,
            SupplierId = p.SupplierId,
            SupplierName = p.SupplierName,
            Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            Note = p.Note,
            PaidByUserName = p.PaidByUserName,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    private static SupplierDto MapToDto(Supplier s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Company = s.Company,
        Phone = s.Phone,
        Email = s.Email,
        Address = s.Address,
        PreviousDue = s.PreviousDue,
        CurrentDue = s.CurrentDue,
        TotalPurchases = s.TotalPurchases,
        IsActive = s.IsActive,
        CreatedAt = s.CreatedAt
    };
}

public class CustomerService : ICustomerService
{
    private readonly IRepository<Customer> _customerRepo;
    private readonly IRepository<CustomerPayment> _paymentRepo;
    private readonly IRepository<CashSession> _cashSessionRepo;
    private readonly IActivityLogService _activityLog;

    public CustomerService(
        IRepository<Customer> customerRepo,
        IRepository<CustomerPayment> paymentRepo,
        IRepository<CashSession> cashSessionRepo,
        IActivityLogService activityLog)
    {
        _customerRepo = customerRepo;
        _paymentRepo = paymentRepo;
        _cashSessionRepo = cashSessionRepo;
        _activityLog = activityLog;
    }

    public async Task<List<CustomerDto>> GetAllCustomersAsync(string? search = null, CancellationToken ct = default)
    {
        var customers = await _customerRepo.GetAllAsync(ct);
        var query = customers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.Phone.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term));
        }

        return query.Select(MapToDto).OrderBy(c => c.Name).ToList();
    }

    public async Task<CustomerDto> GetCustomerByIdAsync(string id, CancellationToken ct = default)
    {
        var c = await _customerRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);
        return MapToDto(c);
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, string userId, string userName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Customer name is required.");

        var customer = new Customer
        {
            Name = request.Name.Trim(),
            Phone = request.Phone?.Trim() ?? string.Empty,
            Email = request.Email?.Trim() ?? string.Empty,
            Address = request.Address?.Trim() ?? string.Empty,
            PreviousDue = request.OpeningDue,
            CurrentDue = request.OpeningDue,
            IsActive = true
        };

        var created = await _customerRepo.AddAsync(customer, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "CreateCustomer",
            ActivityModule.Customers,
            $"Created customer '{customer.Name}' (Phone: {customer.Phone}).",
            ct: ct);

        return MapToDto(created);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(string id, UpdateCustomerRequest request, string userId, string userName, CancellationToken ct = default)
    {
        var c = await _customerRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Customer name is required.");

        c.Name = request.Name.Trim();
        if (request.Phone != null) c.Phone = request.Phone.Trim() ?? string.Empty;
        if (request.Email != null) c.Email = request.Email.Trim() ?? string.Empty;
        if (request.Address != null) c.Address = request.Address.Trim() ?? string.Empty;
        if (request.IsActive.HasValue) c.IsActive = request.IsActive.Value;
        c.UpdatedAt = DateTime.UtcNow;

        await _customerRepo.UpdateAsync(c, ct);

        await _activityLog.LogAsync(
            userId,
            userName,
            "UpdateCustomer",
            ActivityModule.Customers,
            $"Updated customer '{c.Name}'.",
            ct: ct);

        return MapToDto(c);
    }

    public async Task<CustomerPaymentDto> RecordPaymentAsync(CustomerPaymentRequest request, string userId, string userName, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");

        var customer = await _customerRepo.GetByIdAsync(request.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        customer.CurrentDue = Math.Max(0, customer.CurrentDue - request.Amount);
        customer.UpdatedAt = DateTime.UtcNow;
        await _customerRepo.UpdateAsync(customer, ct);

        var payment = new CustomerPayment
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Note = request.Note?.Trim() ?? string.Empty,
            ReceivedByUserId = userId,
            ReceivedByUserName = userName
        };

        var saved = await _paymentRepo.AddAsync(payment, ct);

        // Update active cash session if cash received
        if (request.PaymentMethod == PaymentMethod.Cash && !string.IsNullOrWhiteSpace(request.CashSessionId))
        {
            var session = await _cashSessionRepo.GetByIdAsync(request.CashSessionId, ct);
            if (session != null && session.Status == CashSessionStatus.Open)
            {
                session.CashDueCollections += request.Amount;
                session.UpdatedAt = DateTime.UtcNow;
                await _cashSessionRepo.UpdateAsync(session, ct);
            }
        }

        await _activityLog.LogAsync(
            userId,
            userName,
            "CollectCustomerDue",
            ActivityModule.Due,
            $"Collected ৳{request.Amount:N2} from customer '{customer.Name}' (Remaining due: ৳{customer.CurrentDue:N2}).",
            ct: ct);

        return new CustomerPaymentDto
        {
            Id = saved.Id,
            CustomerId = saved.CustomerId,
            CustomerName = saved.CustomerName,
            Amount = saved.Amount,
            PaymentMethod = saved.PaymentMethod,
            Note = saved.Note,
            ReceivedByUserName = saved.ReceivedByUserName,
            CreatedAt = saved.CreatedAt
        };
    }

    public async Task<List<CustomerPaymentDto>> GetPaymentHistoryAsync(string customerId, CancellationToken ct = default)
    {
        var payments = await _paymentRepo.FindAsync(p => p.CustomerId == customerId, ct);
        return payments.OrderByDescending(p => p.CreatedAt).Select(p => new CustomerPaymentDto
        {
            Id = p.Id,
            CustomerId = p.CustomerId,
            CustomerName = p.CustomerName,
            Amount = p.Amount,
            PaymentMethod = p.PaymentMethod,
            Note = p.Note,
            ReceivedByUserName = p.ReceivedByUserName,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    private static CustomerDto MapToDto(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Phone = c.Phone,
        Email = c.Email,
        Address = c.Address,
        PreviousDue = c.PreviousDue,
        CurrentDue = c.CurrentDue,
        TotalPurchases = c.TotalPurchases,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt
    };
}

public class DueService : IDueService
{
    private readonly IRepository<Customer> _customerRepo;
    private readonly IRepository<Supplier> _supplierRepo;

    public DueService(IRepository<Customer> customerRepo, IRepository<Supplier> supplierRepo)
    {
        _customerRepo = customerRepo;
        _supplierRepo = supplierRepo;
    }

    public async Task<DueSummaryDto> GetDueSummaryAsync(CancellationToken ct = default)
    {
        var customers = await _customerRepo.GetAllAsync(ct);
        var suppliers = await _supplierRepo.GetAllAsync(ct);

        var dueCustomers = customers.Where(c => c.CurrentDue > 0).OrderByDescending(c => c.CurrentDue).ToList();
        var dueSuppliers = suppliers.Where(s => s.CurrentDue > 0).OrderByDescending(s => s.CurrentDue).ToList();

        return new DueSummaryDto
        {
            TotalCustomerDue = dueCustomers.Sum(c => c.CurrentDue),
            TotalSupplierDue = dueSuppliers.Sum(s => s.CurrentDue),
            DueCustomerCount = dueCustomers.Count,
            DueSupplierCount = dueSuppliers.Count,
            TopDueCustomers = dueCustomers.Take(10).Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                Phone = string.IsNullOrWhiteSpace(c.Phone) ? "N/A" : c.Phone,
                Email = string.IsNullOrWhiteSpace(c.Email) ? "N/A" : c.Email,
                Address = c.Address ?? string.Empty,
                PreviousDue = c.PreviousDue,
                CurrentDue = c.CurrentDue,
                TotalPurchases = c.TotalPurchases,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            }).ToList(),
            TopDueSuppliers = dueSuppliers.Take(10).Select(s => new SupplierDto
            {
                Id = s.Id,
                Name = s.Name,
                Company = s.Company ?? string.Empty,
                Phone = string.IsNullOrWhiteSpace(s.Phone) ? "N/A" : s.Phone,
                Email = string.IsNullOrWhiteSpace(s.Email) ? "N/A" : s.Email,
                Address = s.Address ?? string.Empty,
                PreviousDue = s.PreviousDue,
                CurrentDue = s.CurrentDue,
                TotalPurchases = s.TotalPurchases,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            }).ToList()
        };
    }
}

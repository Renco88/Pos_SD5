using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.DTOs;
using POS.Application.Interfaces;
using POS.Domain.Enums;
using POS.Domain.Exceptions;

namespace POS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CustomerDto>>>> GetAll([FromQuery] string? search)
    {
        var list = await _customerService.GetAllCustomersAsync(search, HttpContext.RequestAborted);
        return Ok(ApiResponse<List<CustomerDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetById(string id)
    {
        var c = await _customerService.GetCustomerByIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<CustomerDto>.Ok(c));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Create([FromBody] CreateCustomerRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var created = await _customerService.CreateCustomerAsync(request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<CustomerDto>.Ok(created, "Customer created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Update(string id, [FromBody] UpdateCustomerRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var updated = await _customerService.UpdateCustomerAsync(id, request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<CustomerDto>.Ok(updated, "Customer updated successfully"));
    }

    [HttpPost("payment")]
    public async Task<ActionResult<ApiResponse<CustomerPaymentDto>>> RecordPayment([FromBody] CustomerPaymentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue("FullName") ?? User.FindFirstValue(ClaimTypes.Name)!;
        var payment = await _customerService.RecordPaymentAsync(request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<CustomerPaymentDto>.Ok(payment, "Customer payment recorded successfully"));
    }

    [HttpGet("{id}/payments")]
    public async Task<ActionResult<ApiResponse<List<CustomerPaymentDto>>>> GetPayments(string id)
    {
        var list = await _customerService.GetPaymentHistoryAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<List<CustomerPaymentDto>>.Ok(list));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Employer)]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SupplierDto>>>> GetAll()
    {
        var list = await _supplierService.GetAllSuppliersAsync(HttpContext.RequestAborted);
        return Ok(ApiResponse<List<SupplierDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> GetById(string id)
    {
        var s = await _supplierService.GetSupplierByIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<SupplierDto>.Ok(s));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> Create([FromBody] CreateSupplierRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var created = await _supplierService.CreateSupplierAsync(request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<SupplierDto>.Ok(created, "Supplier created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> Update(string id, [FromBody] UpdateSupplierRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var updated = await _supplierService.UpdateSupplierAsync(id, request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<SupplierDto>.Ok(updated, "Supplier updated successfully"));
    }

    [HttpPost("payment")]
    public async Task<ActionResult<ApiResponse<SupplierPaymentDto>>> RecordPayment([FromBody] SupplierPaymentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue("FullName") ?? User.FindFirstValue(ClaimTypes.Name)!;
        var payment = await _supplierService.RecordPaymentAsync(request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<SupplierPaymentDto>.Ok(payment, "Supplier payment recorded successfully"));
    }

    [HttpGet("{id}/payments")]
    public async Task<ActionResult<ApiResponse<List<SupplierPaymentDto>>>> GetPayments(string id)
    {
        var list = await _supplierService.GetPaymentHistoryAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<List<SupplierPaymentDto>>.Ok(list));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DueController : ControllerBase
{
    private readonly IDueService _dueService;

    public DueController(IDueService dueService)
    {
        _dueService = dueService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DueSummaryDto>>> GetSummary()
    {
        var summary = await _dueService.GetDueSummaryAsync(HttpContext.RequestAborted);
        return Ok(ApiResponse<DueSummaryDto>.Ok(summary));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<List<ExpenseDto>>>> GetExpenses([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? categoryId)
    {
        var list = await _expenseService.GetExpensesAsync(from, to, categoryId, HttpContext.RequestAborted);
        return Ok(ApiResponse<List<ExpenseDto>>.Ok(list));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue("FullName") ?? User.FindFirstValue(ClaimTypes.Name)!;
        var created = await _expenseService.CreateExpenseAsync(request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<ExpenseDto>.Ok(created, "Expense recorded successfully"));
    }

    [HttpPost("{id}/void")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<bool>>> VoidExpense(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var voided = await _expenseService.VoidExpenseAsync(id, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<bool>.Ok(voided, "Expense voided successfully"));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<ExpenseCategoryDto>>>> GetCategories()
    {
        var cats = await _expenseService.GetCategoriesAsync(HttpContext.RequestAborted);
        return Ok(ApiResponse<List<ExpenseCategoryDto>>.Ok(cats));
    }

    [HttpPost("categories")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<ExpenseCategoryDto>>> CreateCategory([FromQuery] string name, [FromQuery] string description)
    {
        var cat = await _expenseService.CreateCategoryAsync(name, description, HttpContext.RequestAborted);
        return Ok(ApiResponse<ExpenseCategoryDto>.Ok(cat, "Expense category created successfully"));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReturnsController : ControllerBase
{
    private readonly IReturnService _returnService;

    public ReturnsController(IReturnService returnService)
    {
        _returnService = returnService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReturnDto>>> ProcessReturn([FromBody] CreateReturnRequest request)
    {
        var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cashierName = User.FindFirstValue("FullName") ?? User.FindFirstValue(ClaimTypes.Name)!;
        var role = User.FindFirstValue(ClaimTypes.Role);

        // Verify if worker has permission to return
        if (role == Roles.Worker)
        {
            var hasPerm = User.FindAll("permission").Any(p => p.Value == Permissions.CanReturnSale);
            if (!hasPerm)
            {
                throw new UnauthorizedDomainException("You do not have permission to process sales returns.");
            }
        }

        var result = await _returnService.ProcessReturnAsync(request, cashierId, cashierName, HttpContext.RequestAborted);
        return Ok(ApiResponse<ReturnDto>.Ok(result, "Return processed successfully"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ReturnDto>>>> GetReturns([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _returnService.GetReturnsAsync(pageNumber, pageSize, HttpContext.RequestAborted);
        return Ok(ApiResponse<PagedResult<ReturnDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ReturnDto>>> GetReturnById(string id)
    {
        var r = await _returnService.GetReturnByIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<ReturnDto>.Ok(r));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountService _discountService;

    public DiscountsController(IDiscountService discountService)
    {
        _discountService = discountService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DiscountRuleDto>>>> GetAllRules()
    {
        var list = await _discountService.GetAllRulesAsync(HttpContext.RequestAborted);
        return Ok(ApiResponse<List<DiscountRuleDto>>.Ok(list));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<DiscountRuleDto>>> CreateRule([FromBody] CreateDiscountRuleRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var created = await _discountService.CreateRuleAsync(request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<DiscountRuleDto>.Ok(created, "Discount rule created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<DiscountRuleDto>>> UpdateRule(string id, [FromBody] CreateDiscountRuleRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var updated = await _discountService.UpdateRuleAsync(id, request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<DiscountRuleDto>.Ok(updated, "Discount rule updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRule(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var deleted = await _discountService.DeleteRuleAsync(id, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<bool>.Ok(deleted, "Discount rule deleted successfully"));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CashController : ControllerBase
{
    private readonly ICashService _cashService;

    public CashController(ICashService cashService)
    {
        _cashService = cashService;
    }

    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<CashSessionDto?>>> GetCurrent()
    {
        var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var session = await _cashService.GetCurrentSessionAsync(cashierId, HttpContext.RequestAborted);
        return Ok(ApiResponse<CashSessionDto?>.Ok(session));
    }

    [HttpPost("open")]
    public async Task<ActionResult<ApiResponse<CashSessionDto>>> Open([FromBody] OpenCashSessionRequest request)
    {
        var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cashierName = User.FindFirstValue("FullName") ?? User.FindFirstValue(ClaimTypes.Name)!;
        var session = await _cashService.OpenSessionAsync(cashierId, cashierName, request, HttpContext.RequestAborted);
        return Ok(ApiResponse<CashSessionDto>.Ok(session, "Cash session opened successfully"));
    }

    [HttpPost("{sessionId}/close")]
    public async Task<ActionResult<ApiResponse<CashSessionDto>>> Close(string sessionId, [FromBody] CloseCashSessionRequest request)
    {
        var session = await _cashService.CloseSessionAsync(sessionId, request, HttpContext.RequestAborted);
        return Ok(ApiResponse<CashSessionDto>.Ok(session, "Cash session closed successfully"));
    }

    [HttpPost("{sessionId}/adjust")]
    public async Task<ActionResult<ApiResponse<CashSessionDto>>> Adjust(string sessionId, [FromBody] CashAdjustmentRequest request)
    {
        var cashierName = User.FindFirstValue("FullName") ?? User.FindFirstValue(ClaimTypes.Name)!;
        var session = await _cashService.AdjustCashAsync(sessionId, request, cashierName, HttpContext.RequestAborted);
        return Ok(ApiResponse<CashSessionDto>.Ok(session, "Cash adjusted successfully"));
    }

    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<List<CashSessionDto>>>> GetHistory([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? cashierId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == Roles.Worker)
        {
            cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        var list = await _cashService.GetSessionHistoryAsync(from, to, cashierId, HttpContext.RequestAborted);
        return Ok(ApiResponse<List<CashSessionDto>>.Ok(list));
    }
}

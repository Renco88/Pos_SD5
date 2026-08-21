using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.DTOs;
using POS.Application.Interfaces;
using POS.Domain.Enums;

namespace POS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SaleDto>>> ProcessSale([FromBody] CreateSaleRequest request)
    {
        var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cashierName = User.FindFirstValue("FullName") ?? User.FindFirstValue(ClaimTypes.Name)!;
        var role = User.FindFirstValue(ClaimTypes.Role);

        decimal maxDiscount = 100.0m;
        if (role == Roles.Worker)
        {
            var discountClaim = User.FindFirstValue("MaxDiscountPercentage");
            if (decimal.TryParse(discountClaim, out var parsedDiscount))
            {
                maxDiscount = parsedDiscount;
            }
            else
            {
                maxDiscount = 5.0m;
            }
        }

        var sale = await _saleService.ProcessSaleAsync(request, cashierId, cashierName, maxDiscount, HttpContext.RequestAborted);
        return Ok(ApiResponse<SaleDto>.Ok(sale, "Sale completed successfully"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<SaleDto>>>> GetSales([FromQuery] SaleFilterRequest request)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == Roles.Worker)
        {
            // Worker can only view their own sales by default unless given permission
            request.CashierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        var result = await _saleService.GetSalesAsync(request, HttpContext.RequestAborted);
        return Ok(ApiResponse<PagedResult<SaleDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SaleDto>>> GetSaleById(string id)
    {
        var sale = await _saleService.GetSaleByIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<SaleDto>.Ok(sale));
    }

    [HttpGet("invoice/{invoiceNumber}")]
    public async Task<ActionResult<ApiResponse<SaleDto>>> GetSaleByInvoice(string invoiceNumber)
    {
        var sale = await _saleService.GetSaleByInvoiceNumberAsync(invoiceNumber, HttpContext.RequestAborted);
        return Ok(ApiResponse<SaleDto>.Ok(sale));
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<bool>>> CancelSale(string id, [FromQuery] string reason)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var cancelled = await _saleService.CancelSaleAsync(id, reason, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<bool>.Ok(cancelled, "Sale cancelled successfully"));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Employer)]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;

    public PurchasesController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseDto>>> CreatePurchase([FromBody] CreatePurchaseRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var created = await _purchaseService.CreatePurchaseAsync(request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<PurchaseDto>.Ok(created, "Purchase recorded successfully"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseDto>>>> GetPurchases([FromQuery] PurchaseFilterRequest request)
    {
        var result = await _purchaseService.GetPurchasesAsync(request, HttpContext.RequestAborted);
        return Ok(ApiResponse<PagedResult<PurchaseDto>>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PurchaseDto>>> GetPurchaseById(string id)
    {
        var p = await _purchaseService.GetPurchaseByIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<PurchaseDto>.Ok(p));
    }
}

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockTransactionDto>>>> GetTransactions(
        [FromQuery] string? productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _inventoryService.GetTransactionsAsync(productId, pageNumber, pageSize, HttpContext.RequestAborted);
        return Ok(ApiResponse<PagedResult<StockTransactionDto>>.Ok(result));
    }

    [HttpPost("adjust")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<StockTransactionDto>>> AdjustStock([FromBody] StockAdjustmentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var result = await _inventoryService.AdjustStockAsync(request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<StockTransactionDto>.Ok(result, "Stock adjusted successfully"));
    }
}

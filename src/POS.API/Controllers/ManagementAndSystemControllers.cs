using System;
using System.Collections.Generic;
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
[Authorize(Roles = Roles.Employer)]
public class WorkersController : ControllerBase
{
    private readonly IWorkerService _workerService;

    public WorkersController(IWorkerService workerService)
    {
        _workerService = workerService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAll()
    {
        var list = await _workerService.GetAllWorkersAsync(HttpContext.RequestAborted);
        return Ok(ApiResponse<List<UserDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(string id)
    {
        var u = await _workerService.GetWorkerByIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<UserDto>.Ok(u));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var adminName = User.FindFirstValue(ClaimTypes.Name)!;
        var created = await _workerService.CreateWorkerAsync(request, adminId, adminName, HttpContext.RequestAborted);
        return Ok(ApiResponse<UserDto>.Ok(created, "Worker created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(string id, [FromBody] UpdateUserRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var adminName = User.FindFirstValue(ClaimTypes.Name)!;
        var updated = await _workerService.UpdateWorkerAsync(id, request, adminId, adminName, HttpContext.RequestAborted);
        return Ok(ApiResponse<UserDto>.Ok(updated, "Worker updated successfully"));
    }

    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var adminName = User.FindFirstValue(ClaimTypes.Name)!;
        var result = await _workerService.ResetWorkerPasswordAsync(id, request.NewPassword, adminId, adminName, HttpContext.RequestAborted);
        return Ok(ApiResponse<bool>.Ok(result, "Password reset successfully. Worker must change on next login."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(string id)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var adminName = User.FindFirstValue(ClaimTypes.Name)!;
        var result = await _workerService.DeleteWorkerAsync(id, adminId, adminName, HttpContext.RequestAborted);
        return Ok(ApiResponse<bool>.Ok(result, "Worker deleted permanently."));
    }

    [HttpGet("{id}/performance")]
    public async Task<ActionResult<ApiResponse<WorkerPerformanceReportDto>>> GetPerformance(string id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var rep = await _workerService.GetWorkerPerformanceAsync(id, from, to, HttpContext.RequestAborted);
        return Ok(ApiResponse<WorkerPerformanceReportDto>.Ok(rep));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("sales")]
    public async Task<ActionResult<ApiResponse<SalesReportDto>>> GetSalesReport([FromQuery] ReportFilterRequest filter)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == Roles.Worker)
        {
            // Worker gets only their own sales
            filter.WorkerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        var report = await _reportService.GetSalesReportAsync(filter, HttpContext.RequestAborted);
        return Ok(ApiResponse<SalesReportDto>.Ok(report));
    }

    [HttpGet("purchases")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<PurchaseReportDto>>> GetPurchaseReport([FromQuery] ReportFilterRequest filter)
    {
        var report = await _reportService.GetPurchaseReportAsync(filter, HttpContext.RequestAborted);
        return Ok(ApiResponse<PurchaseReportDto>.Ok(report));
    }

    [HttpGet("profit-loss")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<ProfitLossReportDto>>> GetProfitLossReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;
        var report = await _reportService.GetProfitLossReportAsync(fromDate, toDate, HttpContext.RequestAborted);
        return Ok(ApiResponse<ProfitLossReportDto>.Ok(report));
    }

    [HttpGet("expenses")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<ExpenseReportDto>>> GetExpenseReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;
        var report = await _reportService.GetExpenseReportAsync(fromDate, toDate, HttpContext.RequestAborted);
        return Ok(ApiResponse<ExpenseReportDto>.Ok(report));
    }

    [HttpGet("workers")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<List<WorkerPerformanceReportDto>>>> GetWorkersPerformance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;
        var reports = await _reportService.GetAllWorkersPerformanceAsync(fromDate, toDate, HttpContext.RequestAborted);
        return Ok(ApiResponse<List<WorkerPerformanceReportDto>>.Ok(reports));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly ISettingsService _settingsService;
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(ISaleService saleService, ISettingsService settingsService, IInvoiceService invoiceService)
    {
        _saleService = saleService;
        _settingsService = settingsService;
        _invoiceService = invoiceService;
    }

    [HttpGet("{saleId}/receipt")]
    public async Task<ActionResult<ApiResponse<string>>> GetReceiptText(string saleId)
    {
        var sale = await _saleService.GetSaleByIdAsync(saleId, HttpContext.RequestAborted);
        var settings = await _settingsService.GetSettingsAsync(HttpContext.RequestAborted);
        var text = await _invoiceService.GenerateReceiptTextAsync(sale, settings);
        return Ok(ApiResponse<string>.Ok(text));
    }

    [HttpGet("{saleId}/a4")]
    public async Task<ActionResult<ApiResponse<string>>> GetA4Html(string saleId)
    {
        var sale = await _saleService.GetSaleByIdAsync(saleId, HttpContext.RequestAborted);
        var settings = await _settingsService.GetSettingsAsync(HttpContext.RequestAborted);
        var html = await _invoiceService.GenerateA4HtmlAsync(sale, settings);
        return Ok(ApiResponse<string>.Ok(html));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BarcodesController : ControllerBase
{
    private readonly IBarcodeService _barcodeService;

    public BarcodesController(IBarcodeService barcodeService)
    {
        _barcodeService = barcodeService;
    }

    [HttpGet("generate")]
    public ActionResult<ApiResponse<string>> GenerateBarcodeSvg([FromQuery] string code, [FromQuery] string format = "CODE128")
    {
        var svg = _barcodeService.GenerateBarcodeSvg(code, format);
        return Ok(ApiResponse<string>.Ok(svg));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<BusinessSettingsDto>>> Get()
    {
        var s = await _settingsService.GetSettingsAsync(HttpContext.RequestAborted);
        return Ok(ApiResponse<BusinessSettingsDto>.Ok(s));
    }

    [HttpPut]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<BusinessSettingsDto>>> Update([FromBody] BusinessSettingsDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var updated = await _settingsService.UpdateSettingsAsync(request, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<BusinessSettingsDto>.Ok(updated, "Business settings updated successfully"));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Employer)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAll()
    {
        var list = await _userService.GetAllUsersAsync(HttpContext.RequestAborted);
        return Ok(ApiResponse<List<UserDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(string id)
    {
        var u = await _userService.GetUserByIdAsync(id, HttpContext.RequestAborted);
        return Ok(ApiResponse<UserDto>.Ok(u));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var adminName = User.FindFirstValue(ClaimTypes.Name)!;
        var created = await _userService.CreateUserAsync(request, adminId, adminName, HttpContext.RequestAborted);
        return Ok(ApiResponse<UserDto>.Ok(created, "User created successfully"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(string id, [FromBody] UpdateUserRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var adminName = User.FindFirstValue(ClaimTypes.Name)!;
        var updated = await _userService.UpdateUserAsync(id, request, adminId, adminName, HttpContext.RequestAborted);
        return Ok(ApiResponse<UserDto>.Ok(updated, "User updated successfully"));
    }

    [HttpPost("{id}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(string id, [FromQuery] bool isActive)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var adminName = User.FindFirstValue(ClaimTypes.Name)!;
        var result = await _userService.ToggleUserStatusAsync(id, isActive, adminId, adminName, HttpContext.RequestAborted);
        return Ok(ApiResponse<bool>.Ok(result, "User status updated successfully"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(string id)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var adminName = User.FindFirstValue(ClaimTypes.Name)!;
        var result = await _userService.DeleteUserAsync(id, adminId, adminName, HttpContext.RequestAborted);
        return Ok(ApiResponse<bool>.Ok(result, "User deleted permanently."));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Employer)]
public class ActivityLogsController : ControllerBase
{
    private readonly IActivityLogService _activityLogService;

    public ActivityLogsController(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ActivityLogDto>>>> GetLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] ActivityModule? module = null,
        [FromQuery] string? search = null)
    {
        var result = await _activityLogService.GetLogsAsync(pageNumber, pageSize, module, search, HttpContext.RequestAborted);
        return Ok(ApiResponse<PagedResult<ActivityLogDto>>.Ok(result));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Employer)]
public class BackupController : ControllerBase
{
    private readonly IBackupService _backupService;

    public BackupController(IBackupService backupService)
    {
        _backupService = backupService;
    }

    [HttpPost("create")]
    public async Task<ActionResult<ApiResponse<BackupDto>>> Create([FromQuery] string? targetFolder)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var backup = await _backupService.CreateBackupAsync(targetFolder ?? "", userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<BackupDto>.Ok(backup, "Backup snapshot created successfully"));
    }

    [HttpPost("{backupId}/restore")]
    public async Task<ActionResult<ApiResponse<bool>>> Restore(string backupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var success = await _backupService.RestoreBackupAsync(backupId, userId, userName, HttpContext.RequestAborted);
        return Ok(ApiResponse<bool>.Ok(success, "Database restored successfully"));
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<List<BackupDto>>>> GetList()
    {
        var list = await _backupService.GetBackupsAsync(HttpContext.RequestAborted);
        return Ok(ApiResponse<List<BackupDto>>.Ok(list));
    }
}

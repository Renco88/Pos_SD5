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
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;

    public AuthController(IAuthService authService, IUserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var response = await _authService.LoginAsync(request, ip, HttpContext.RequestAborted);
        return Ok(ApiResponse<LoginResponse>.Ok(response, "Login successful"));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var success = await _authService.ChangePasswordAsync(userId, request, HttpContext.RequestAborted);
        return Ok(ApiResponse<bool>.Ok(success, "Password changed successfully"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userService.GetUserByIdAsync(userId, HttpContext.RequestAborted);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("employer")]
    [Authorize(Roles = Roles.Employer)]
    public async Task<ActionResult<ApiResponse<EmployerDashboardDto>>> GetEmployerDashboard()
    {
        var data = await _dashboardService.GetEmployerDashboardAsync(HttpContext.RequestAborted);
        return Ok(ApiResponse<EmployerDashboardDto>.Ok(data));
    }

    [HttpGet("worker")]
    public async Task<ActionResult<ApiResponse<WorkerDashboardDto>>> GetWorkerDashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var data = await _dashboardService.GetWorkerDashboardAsync(userId, HttpContext.RequestAborted);
        return Ok(ApiResponse<WorkerDashboardDto>.Ok(data));
    }
}

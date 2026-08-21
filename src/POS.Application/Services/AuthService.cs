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

public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IActivityLogService _activityLog;

    public AuthService(
        IRepository<User> userRepo,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IActivityLogService activityLog)
    {
        _userRepo = userRepo;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _activityLog = activityLog;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken ct = default)
    {
        var user = await _userRepo.FindOneAsync(u => u.Username.ToLower() == request.Username.ToLower() || u.Email.ToLower() == request.Username.ToLower(), ct);
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new DomainException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            throw new DomainException("Your account is deactivated. Please contact an administrator.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);

        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        await _activityLog.LogAsync(
            user.Id,
            user.FullName,
            "Login",
            ActivityModule.Auth,
            $"User '{user.Username}' ({user.Role}) logged in successfully.",
            ipAddress,
            ct);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = MapToDto(user)
        };
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!_passwordHasher.VerifyPassword(request.OldPassword, user.PasswordHash))
        {
            throw new DomainException("Current password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            throw new DomainException("New password must be at least 6 characters long.");
        }

        if (request.NewPassword != request.ConfirmNewPassword)
        {
            throw new DomainException("New password and confirmation do not match.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        var updated = await _userRepo.UpdateAsync(user, ct);

        await _activityLog.LogAsync(
            user.Id,
            user.FullName,
            "ChangePassword",
            ActivityModule.Auth,
            $"User '{user.Username}' changed their password.",
            ct: ct);

        return updated;
    }

    public async Task<bool> ResetPasswordAsync(string userId, ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(nameof(User), userId);

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            throw new DomainException("Password must be at least 6 characters long.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.UtcNow;
        return await _userRepo.UpdateAsync(user, ct);
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        Phone = user.Phone,
        Role = user.Role,
        Permissions = user.Permissions,
        MaxDiscountPercentage = user.MaxDiscountPercentage,
        IsActive = user.IsActive,
        MustChangePassword = user.MustChangePassword,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt
    };
}

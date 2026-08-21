using System;
using System.Collections.Generic;
using POS.Domain.Common;
using POS.Domain.Enums;

namespace POS.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Worker;
    public List<string> Permissions { get; set; } = [];
    public decimal MaxDiscountPercentage { get; set; } = 5.0m;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = false;
    public DateTime? LastLoginAt { get; set; }

    public bool HasPermission(string permission)
    {
        if (Role == Roles.Employer) return true;
        return Permissions.Contains(permission);
    }
}

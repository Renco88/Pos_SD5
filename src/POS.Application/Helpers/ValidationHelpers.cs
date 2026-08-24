using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace POS.Application.Helpers;

public static class ValidationHelpers
{
    public static PasswordValidationResult ValidatePasswordStrength(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password cannot be empty.");
            return new PasswordValidationResult(false, 0, errors);
        }

        int score = 0;

        if (password.Length >= 6) score++;
        else errors.Add("Password must be at least 6 characters long.");

        if (password.Length >= 10) score++;

        if (Regex.IsMatch(password, @"[a-z]")) score++;
        else errors.Add("Password must contain at least one lowercase letter.");

        if (Regex.IsMatch(password, @"[A-Z]")) score++;
        else errors.Add("Password must contain at least one uppercase letter.");

        if (Regex.IsMatch(password, @"[0-9]")) score++;
        else errors.Add("Password must contain at least one digit.");

        if (Regex.IsMatch(password, @"[^\w\s]")) score++;
        else errors.Add("Password must contain at least one special character (e.g., !@#$%^&*).");

        if (!password.Any(char.IsWhiteSpace)) score++;

        var strength = score switch
        {
            <= 2 => PasswordStrength.Weak,
            <= 4 => PasswordStrength.Medium,
            <= 5 => PasswordStrength.Strong,
            _ => PasswordStrength.VeryStrong
        };

        bool isValid = password.Length >= 6 &&
                       Regex.IsMatch(password, @"[a-z]") &&
                       Regex.IsMatch(password, @"[A-Z]") &&
                       Regex.IsMatch(password, @"[0-9]");

        return new PasswordValidationResult(isValid, score, errors, strength);
    }

    public static (int PageNumber, int PageSize) SanitizePagination(int pageNumber, int pageSize, int maxPageSize = 100)
    {
        int safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        int safePageSize = pageSize < 1 ? 20 : (pageSize > maxPageSize ? maxPageSize : pageSize);
        return (safePageNumber, safePageSize);
    }

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            var trimmed = email.Trim();
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));
            return regex.IsMatch(trimmed);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    public static bool IsValidPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length >= 7 && digits.Length <= 15;
    }

    public static string SanitizeSearchTerm(string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return string.Empty;
        return searchTerm.Trim().ToLower();
    }
}

public enum PasswordStrength
{
    Weak,
    Medium,
    Strong,
    VeryStrong
}

public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public int Score { get; set; }
    public PasswordStrength Strength { get; set; }
    public List<string> Errors { get; set; } = [];

    public PasswordValidationResult() { }

    public PasswordValidationResult(bool isValid, int score, List<string> errors, PasswordStrength strength = PasswordStrength.Weak)
    {
        IsValid = isValid;
        Score = score;
        Errors = errors;
        Strength = strength;
    }
}

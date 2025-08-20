using System;
using System.Text.RegularExpressions;

namespace NeoServiceLayer.Utilities.Helpers;

/// <summary>
/// Common validation helper methods.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates an email address format.
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if a string is a valid GUID.
    /// </summary>
    public static bool IsValidGuid(string input)
    {
        return Guid.TryParse(input, out _);
    }
}
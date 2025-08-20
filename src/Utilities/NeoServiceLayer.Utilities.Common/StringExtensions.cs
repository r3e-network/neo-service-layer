using System;

namespace NeoServiceLayer.Utilities.Common;

/// <summary>
/// Common string utility extensions.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts a string to title case.
    /// </summary>
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
            
        return char.ToUpper(input[0]) + input[1..].ToLower();
    }

    /// <summary>
    /// Truncates a string to a specified length.
    /// </summary>
    public static string Truncate(this string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
            
        return input.Length <= maxLength ? input : input[..maxLength];
    }
}
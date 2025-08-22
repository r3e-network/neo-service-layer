using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using NeoServiceLayer.Shared.Constants;


namespace NeoServiceLayer.Shared.Extensions;

/// <summary>
/// Extension methods for string operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if the string is null or empty.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string is null or empty; otherwise, false.</returns>
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Checks if the string is null, empty, or contains only whitespace.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string is null, empty, or whitespace; otherwise, false.</returns>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Converts the string to Base64 encoding.
    /// </summary>
    /// <param name="value">The string to encode.</param>
    /// <param name="encoding">The encoding to use. Defaults to UTF-8.</param>
    /// <returns>The Base64 encoded string.</returns>
    public static string ToBase64(this string value, Encoding? encoding = null)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(value);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Converts the Base64 string to its original form.
    /// </summary>
    /// <param name="value">The Base64 encoded string.</param>
    /// <param name="encoding">The encoding to use. Defaults to UTF-8.</param>
    /// <returns>The decoded string.</returns>
    public static string FromBase64(this string value, Encoding? encoding = null)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        encoding ??= Encoding.UTF8;
        var bytes = Convert.FromBase64String(value);
        return encoding.GetString(bytes);
    }

    /// <summary>
    /// Converts the string to hexadecimal representation.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <param name="encoding">The encoding to use. Defaults to UTF-8.</param>
    /// <returns>The hexadecimal string.</returns>
    public static string ToHex(this string value, Encoding? encoding = null)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(value);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Converts the hexadecimal string to its original form.
    /// </summary>
    /// <param name="value">The hexadecimal string.</param>
    /// <param name="encoding">The encoding to use. Defaults to UTF-8.</param>
    /// <returns>The decoded string.</returns>
    public static string FromHex(this string value, Encoding? encoding = null)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        encoding ??= Encoding.UTF8;
        var bytes = Convert.FromHexString(value);
        return encoding.GetString(bytes);
    }

    /// <summary>
    /// Computes the SHA256 hash of the string.
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <param name="encoding">The encoding to use. Defaults to UTF-8.</param>
    /// <returns>The SHA256 hash as a hexadecimal string.</returns>
    public static string ToSha256(this string value, Encoding? encoding = null)
    {
        if (value == null)
            return string.Empty;

        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Computes the MD5 hash of the string.
    /// WARNING: MD5 is not cryptographically secure. Use only for non-security purposes like checksums.
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <param name="encoding">The encoding to use. Defaults to UTF-8.</param>
    /// <returns>The MD5 hash as a hexadecimal string.</returns>
#pragma warning disable CA5351 // Do not use broken cryptographic algorithms
    public static string ToMd5(this string value, Encoding? encoding = null)
    {
        if (value == null)
            return string.Empty;

        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(value);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
#pragma warning restore CA5351 // Do not use broken cryptographic algorithms

    /// <summary>
    /// Computes the SHA256 hash of the string (cryptographically secure).
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <param name="encoding">The encoding to use. Defaults to UTF-8.</param>
    /// <returns>The SHA256 hash as a hexadecimal string.</returns>
    public static string ToSha256Hash(this string value, Encoding? encoding = null)
    {
        if (value == null)
            return string.Empty;

        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Truncates the string to the specified length.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <param name="suffix">The suffix to append if truncated. Defaults to "...".</param>
    /// <returns>The truncated string.</returns>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (value.IsNullOrEmpty() || value.Length <= maxLength)
            return value ?? string.Empty;

        var truncatedLength = Math.Max(0, maxLength - suffix.Length);
        return value[..truncatedLength] + suffix;
    }

    /// <summary>
    /// Masks the string by replacing characters with a mask character.
    /// </summary>
    /// <param name="value">The string to mask.</param>
    /// <param name="visibleStart">Number of characters to show at the start.</param>
    /// <param name="visibleEnd">Number of characters to show at the end.</param>
    /// <param name="maskChar">The character to use for masking.</param>
    /// <returns>The masked string.</returns>
    public static string Mask(this string value, int visibleStart = 2, int visibleEnd = 2, char maskChar = '*')
    {
        if (value.IsNullOrEmpty() || value.Length <= visibleStart + visibleEnd)
            return new string(maskChar, value?.Length ?? 0);

        var start = value[..visibleStart];
        var end = value[^visibleEnd..];
        var middle = new string(maskChar, value.Length - visibleStart - visibleEnd);

        return start + middle + end;
    }

    /// <summary>
    /// Validates if the string matches the specified pattern.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="pattern">The regular expression pattern.</param>
    /// <param name="options">The regex options.</param>
    /// <returns>True if the string matches the pattern; otherwise, false.</returns>
    public static bool Matches(this string value, string pattern, RegexOptions options = RegexOptions.None)
    {
        if (value.IsNullOrEmpty() || pattern.IsNullOrEmpty())
            return false;

        return Regex.IsMatch(value, pattern, options);
    }

    /// <summary>
    /// Validates if the string is a valid email address.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <returns>True if the string is a valid email; otherwise, false.</returns>
    public static bool IsValidEmail(this string value)
    {
        return value.Matches(Constants.ServiceConstants.Patterns.Email);
    }

    /// <summary>
    /// Validates if the string is a valid URL.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <returns>True if the string is a valid URL; otherwise, false.</returns>
    public static bool IsValidUrl(this string value)
    {
        return value.Matches(Constants.ServiceConstants.Patterns.Url);
    }

    /// <summary>
    /// Validates if the string is a valid hexadecimal string.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <returns>True if the string is valid hex; otherwise, false.</returns>
    public static bool IsValidHex(this string value)
    {
        return value.Matches(Constants.ServiceConstants.Patterns.HexString);
    }

    /// <summary>
    /// Validates if the string is a valid Neo address.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <returns>True if the string is a valid Neo address; otherwise, false.</returns>
    public static bool IsValidNeoAddress(this string value)
    {
        return value.Matches(Constants.ServiceConstants.Patterns.NeoAddress);
    }

    /// <summary>
    /// Validates if the string is a valid Ethereum address.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <returns>True if the string is a valid Ethereum address; otherwise, false.</returns>
    public static bool IsValidEthereumAddress(this string value)
    {
        return value.Matches(Constants.ServiceConstants.Patterns.EthereumAddress);
    }

    /// <summary>
    /// Converts the string to camelCase.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The camelCase string.</returns>
    public static string ToCamelCase(this string value)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        if (value.Length == 1)
            return value.ToLowerInvariant();

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// Converts the string to PascalCase.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The PascalCase string.</returns>
    public static string ToPascalCase(this string value)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        if (value.Length == 1)
            return value.ToUpperInvariant();

        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// Converts the string to kebab-case.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The kebab-case string.</returns>
    public static string ToKebabCase(this string value)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        return Regex.Replace(value, @"([a-z0-9])([A-Z])", "$1-$2").ToLowerInvariant();
    }

    /// <summary>
    /// Converts the string to snake_case.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The snake_case string.</returns>
    public static string ToSnakeCase(this string value)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        return Regex.Replace(value, @"([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
    }

    /// <summary>
    /// Safely deserializes JSON string to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="value">The JSON string.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <returns>The deserialized object or default value if deserialization fails.</returns>
    public static T? FromJson<T>(this string value, JsonSerializerOptions? options = null)
    {
        if (value.IsNullOrWhiteSpace())
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(value, options);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Removes diacritics (accents) from the string.
    /// </summary>
    /// <param name="value">The string to process.</param>
    /// <returns>The string without diacritics.</returns>
    public static string RemoveDiacritics(this string value)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        var normalizedString = value.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Repeats the string the specified number of times.
    /// </summary>
    /// <param name="value">The string to repeat.</param>
    /// <param name="count">The number of times to repeat.</param>
    /// <returns>The repeated string.</returns>
    public static string Repeat(this string value, int count)
    {
        if (value.IsNullOrEmpty() || count <= 0)
            return string.Empty;

        return string.Concat(Enumerable.Repeat(value, count));
    }

    /// <summary>
    /// Splits the string into chunks of the specified size.
    /// </summary>
    /// <param name="value">The string to split.</param>
    /// <param name="chunkSize">The size of each chunk.</param>
    /// <returns>An enumerable of string chunks.</returns>
    public static IEnumerable<string> SplitIntoChunks(this string value, int chunkSize)
    {
        if (value.IsNullOrEmpty() || chunkSize <= 0)
            yield break;

        for (int i = 0; i < value.Length; i += chunkSize)
        {
            yield return value.Substring(i, Math.Min(chunkSize, value.Length - i));
        }
    }

    /// <summary>
    /// Converts the string to title case.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The title case string.</returns>
    public static string ToTitleCase(this string value)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(value.ToLowerInvariant());
    }

    /// <summary>
    /// Capitalizes the first character of the string.
    /// </summary>
    /// <param name="value">The string to capitalize.</param>
    /// <returns>The capitalized string.</returns>
    public static string Capitalize(this string value)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        if (value.Length == 1)
            return value.ToUpperInvariant();

        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// Truncates the string to the specified length and adds ellipsis.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <returns>The truncated string with ellipsis.</returns>
    public static string TruncateWithEllipsis(this string value, int maxLength)
    {
        return value.Truncate(maxLength, "...");
    }

    /// <summary>
    /// Checks if the string contains the specified value ignoring case.
    /// </summary>
    /// <param name="value">The string to search in.</param>
    /// <param name="search">The string to search for.</param>
    /// <returns>True if the string contains the search value ignoring case; otherwise, false.</returns>
    public static bool ContainsIgnoreCase(this string value, string search)
    {
        if (value.IsNullOrEmpty() || search.IsNullOrEmpty())
            return false;

        return value.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the string equals the specified value ignoring case.
    /// </summary>
    /// <param name="value">The string to compare.</param>
    /// <param name="other">The string to compare with.</param>
    /// <returns>True if the strings are equal ignoring case; otherwise, false.</returns>
    public static bool EqualsIgnoreCase(this string value, string other)
    {
        return string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the string starts with the specified value ignoring case.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="prefix">The prefix to check for.</param>
    /// <returns>True if the string starts with the prefix ignoring case; otherwise, false.</returns>
    public static bool StartsWithIgnoreCase(this string value, string prefix)
    {
        if (value.IsNullOrEmpty() || prefix.IsNullOrEmpty())
            return false;

        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the string ends with the specified value ignoring case.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="suffix">The suffix to check for.</param>
    /// <returns>True if the string ends with the suffix ignoring case; otherwise, false.</returns>
    public static bool EndsWithIgnoreCase(this string value, string suffix)
    {
        if (value.IsNullOrEmpty() || suffix.IsNullOrEmpty())
            return false;

        return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the string represents a numeric value.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string is numeric; otherwise, false.</returns>
    public static bool IsNumeric(this string value)
    {
        if (value.IsNullOrWhiteSpace())
            return false;

        return double.TryParse(value, out _);
    }

    /// <summary>
    /// Computes the MD5 hash of the string.
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <returns>The MD5 hash as a hexadecimal string.</returns>
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms - legacy compatibility
    public static string ToMD5Hash(this string value)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
#pragma warning restore CA5351

    /// <summary>
    /// Computes the SHA1 hash of the string.
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <returns>The SHA1 hash as a hexadecimal string.</returns>
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms - legacy compatibility
    public static string ToSHA1Hash(this string value)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        using var sha1 = SHA1.Create();
        var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
#pragma warning restore CA5350

    /// <summary>
    /// Computes the SHA256 hash of the string.
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <returns>The SHA256 hash as a hexadecimal string.</returns>
    public static string ToSHA256Hash(this string value)
    {
        if (value.IsNullOrEmpty())
            return string.Empty;

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Safely trims whitespace from a string, returning empty string if null.
    /// </summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>The trimmed string or empty string if null.</returns>
    public static string TrimSafe(this string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Replaces spaces in the string with the specified replacement.
    /// </summary>
    /// <param name="value">The string to process.</param>
    /// <param name="replacement">The replacement for spaces.</param>
    /// <returns>The string with spaces replaced.</returns>
    public static string ReplaceSpaces(this string value, string replacement)
    {
        if (value.IsNullOrEmpty())
            return value;

        return value.Replace(" ", replacement);
    }

    /// <summary>
    /// Pads the string to the right with spaces or truncates to the specified length.
    /// </summary>
    /// <param name="value">The string to pad or truncate.</param>
    /// <param name="length">The target length.</param>
    /// <returns>The padded or truncated string.</returns>
    public static string PadRightToLength(this string value, int length)
    {
        if (value.IsNullOrEmpty())
            return new string(' ', length);

        return value.Length > length ? value.Substring(0, length) : value.PadRight(length);
    }

    /// <summary>
    /// Pads the string to the left with spaces or truncates to the specified length.
    /// </summary>
    /// <param name="value">The string to pad or truncate.</param>
    /// <param name="length">The target length.</param>
    /// <returns>The padded or truncated string.</returns>
    public static string PadLeftToLength(this string value, int length)
    {
        if (value.IsNullOrEmpty())
            return new string(' ', length);

        return value.Length > length ? value.Substring(0, length) : value.PadLeft(length);
    }

    /// <summary>
    /// Safely splits a string by the specified separators.
    /// </summary>
    /// <param name="value">The string to split.</param>
    /// <param name="separators">The separator characters.</param>
    /// <returns>An array of string segments.</returns>
    public static string[] SafeSplit(this string? value, params char[] separators)
    {
        if (value.IsNullOrEmpty())
            return Array.Empty<string>();

        return value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Determines whether the string is a valid hexadecimal string.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string is a valid hex string, false otherwise.</returns>
    public static bool IsHexString(this string? value)
    {
        if (value == null)
            return false;
        if (value == "")
            return true; // Empty string is considered valid hex

        return System.Text.RegularExpressions.Regex.IsMatch(value, @"^[0-9A-Fa-f]+$");
    }

    /// <summary>
    /// Sanitizes a string by removing control characters and potentially dangerous content.
    /// </summary>
    /// <param name="value">The string to sanitize.</param>
    /// <returns>The sanitized string.</returns>
    public static string Sanitize(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Remove control characters except normal whitespace
        var sanitized = new StringBuilder();
        foreach (char c in value)
        {
            if (!char.IsControl(c) || c == '\t' || c == '\n' || c == '\r')
                sanitized.Append(c);
        }

        return sanitized.ToString();
    }

    /// <summary>
    /// Escapes HTML special characters in the string.
    /// </summary>
    /// <param name="value">The string to escape.</param>
    /// <returns>The HTML-escaped string.</returns>
    public static string EscapeHtml(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    /// <summary>
    /// Determines whether the string is a valid IPv4 address.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string is a valid IPv4 address, false otherwise.</returns>
    public static bool IsValidIPv4(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return System.Net.IPAddress.TryParse(value, out var address) && 
               address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    }
}

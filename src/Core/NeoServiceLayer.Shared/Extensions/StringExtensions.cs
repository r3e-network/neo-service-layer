using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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
    /// </summary>
    /// <param name="value">The string to hash.</param>
    /// <param name="encoding">The encoding to use. Defaults to UTF-8.</param>
    /// <returns>The MD5 hash as a hexadecimal string.</returns>
    public static string ToMd5(this string value, Encoding? encoding = null)
    {
        if (value == null)
            return string.Empty;

        encoding ??= Encoding.UTF8;
        var bytes = encoding.GetBytes(value);
        var hash = MD5.HashData(bytes);
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
} 
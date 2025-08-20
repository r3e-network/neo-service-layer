using System;
using System.Text.RegularExpressions;

namespace NeoServiceLayer.Core.Security;

/// <summary>
/// Provides comprehensive input validation for security-critical operations.
/// </summary>
public static class InputValidation
{
    // Blockchain address patterns for different networks
    private static readonly Regex EthereumAddressPattern = new(@"^0x[a-fA-F0-9]{40}$", RegexOptions.Compiled);
    private static readonly Regex NeoAddressPattern = new(@"^N[a-zA-Z0-9]{33}$", RegexOptions.Compiled);
    private static readonly Regex BitcoinAddressPattern = new(@"^[13][a-km-zA-HJ-NP-Z1-9]{25,34}$|^bc1[a-z0-9]{39,59}$", RegexOptions.Compiled);
    
    // SQL injection prevention patterns
    private static readonly Regex SqlInjectionPattern = new(@"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|UNION|FROM|WHERE|JOIN|ORDER BY|GROUP BY|HAVING)\b)|(-{2})|(/\*.*?\*/)|;", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // XSS prevention patterns
    private static readonly Regex XssPattern = new(@"<script|javascript:|onerror=|onload=|<iframe|<object|<embed|<svg", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Validates a blockchain address based on the specified blockchain type.
    /// </summary>
    public static bool IsValidBlockchainAddress(string address, string blockchainType)
    {
        if (string.IsNullOrWhiteSpace(address))
            return false;

        return blockchainType?.ToUpperInvariant() switch
        {
            "ETHEREUM" or "ETH" => EthereumAddressPattern.IsMatch(address),
            "NEO" => NeoAddressPattern.IsMatch(address),
            "BITCOIN" or "BTC" => BitcoinAddressPattern.IsMatch(address),
            _ => false
        };
    }

    /// <summary>
    /// Validates that input does not contain SQL injection attempts.
    /// </summary>
    public static bool IsSafeSqlInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return true;

        return !SqlInjectionPattern.IsMatch(input);
    }

    /// <summary>
    /// Validates that input does not contain XSS attempts.
    /// </summary>
    public static bool IsSafeHtmlInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return true;

        return !XssPattern.IsMatch(input);
    }

    /// <summary>
    /// Sanitizes input by removing potentially dangerous characters.
    /// </summary>
    public static string SanitizeInput(string input, int maxLength = 1000)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Truncate to max length
        if (input.Length > maxLength)
            input = input[..maxLength];

        // Remove control characters
        input = Regex.Replace(input, @"[\x00-\x1F\x7F]", string.Empty);

        // HTML encode special characters
        input = System.Net.WebUtility.HtmlEncode(input);

        return input;
    }

    /// <summary>
    /// Validates numeric input within specified bounds.
    /// </summary>
    public static bool IsValidNumericRange(decimal value, decimal min, decimal max)
    {
        return value >= min && value <= max;
    }

    /// <summary>
    /// Validates that a URL is safe and from allowed domains.
    /// </summary>
    public static bool IsValidUrl(string url, string[] allowedDomains = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        // Only allow HTTP and HTTPS
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        // Check against allowed domains if specified
        if (allowedDomains?.Length > 0)
        {
            return Array.Exists(allowedDomains, domain => 
                uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
                uri.Host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase));
        }

        return true;
    }

    /// <summary>
    /// Validates email address format.
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates transaction hash format.
    /// </summary>
    public static bool IsValidTransactionHash(string hash, string blockchainType)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return false;

        return blockchainType?.ToUpperInvariant() switch
        {
            "ETHEREUM" or "ETH" => Regex.IsMatch(hash, @"^0x[a-fA-F0-9]{64}$"),
            "NEO" => Regex.IsMatch(hash, @"^0x[a-fA-F0-9]{64}$"),
            "BITCOIN" or "BTC" => Regex.IsMatch(hash, @"^[a-fA-F0-9]{64}$"),
            _ => false
        };
    }
}
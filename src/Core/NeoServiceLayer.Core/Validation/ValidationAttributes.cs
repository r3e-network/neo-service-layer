using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace NeoServiceLayer.Core.Validation;

/// <summary>
/// Validates that a string contains only alphanumeric characters and hyphens.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class AlphanumericAttribute : ValidationAttribute
{
    private readonly bool _allowHyphens;
    private readonly bool _allowUnderscores;

    public AlphanumericAttribute(bool allowHyphens = true, bool allowUnderscores = true)
    {
        _allowHyphens = allowHyphens;
        _allowUnderscores = allowUnderscores;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var pattern = "^[a-zA-Z0-9";
        if (_allowHyphens) pattern += "-";
        if (_allowUnderscores) pattern += "_";
        pattern += "]+$";

        if (!Regex.IsMatch(value.ToString()!, pattern))
        {
            return new ValidationResult($"The {validationContext.DisplayName} field must contain only alphanumeric characters{(_allowHyphens ? ", hyphens" : "")}{(_allowUnderscores ? ", underscores" : "")}.");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates that a string is a valid blockchain address.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class BlockchainAddressAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var address = value.ToString()!;

        // Neo N3 address validation (starts with N and is 34 characters)
        if (address.StartsWith("N") && address.Length == 34)
        {
            return ValidationResult.Success;
        }

        // Ethereum-style address validation (0x followed by 40 hex characters)
        if (Regex.IsMatch(address, "^0x[a-fA-F0-9]{40}$"))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult($"The {validationContext.DisplayName} field must be a valid blockchain address.");
    }
}

/// <summary>
/// Validates that a value is within a specified range.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class PositiveNumberAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (!decimal.TryParse(value.ToString(), out var number) || number <= 0)
        {
            return new ValidationResult($"The {validationContext.DisplayName} field must be a positive number.");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates that a string is a valid hex string.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class HexStringAttribute : ValidationAttribute
{
    private readonly bool _requirePrefix;
    private readonly int? _expectedLength;

    public HexStringAttribute(bool requirePrefix = false, int expectedLength = 0)
    {
        _requirePrefix = requirePrefix;
        _expectedLength = expectedLength > 0 ? expectedLength : null;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var hex = value.ToString()!;

        if (_requirePrefix)
        {
            if (!hex.StartsWith("0x"))
            {
                return new ValidationResult($"The {validationContext.DisplayName} field must start with '0x'.");
            }
            hex = hex.Substring(2);
        }
        else if (hex.StartsWith("0x"))
        {
            hex = hex.Substring(2);
        }

        if (!Regex.IsMatch(hex, "^[a-fA-F0-9]*$"))
        {
            return new ValidationResult($"The {validationContext.DisplayName} field must be a valid hexadecimal string.");
        }

        if (_expectedLength.HasValue && hex.Length != _expectedLength.Value)
        {
            return new ValidationResult($"The {validationContext.DisplayName} field must be exactly {_expectedLength.Value} characters long.");
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates that a string does not contain dangerous characters.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class SafeStringAttribute : ValidationAttribute
{
    private static readonly string[] DangerousPatterns = new[]
    {
        "<script", "</script", "javascript:", "onerror=", "onload=", "onclick=",
        "'; DROP TABLE", "-- ", "/*", "*/", "xp_", "sp_", "exec(", "execute(",
        "insert into", "delete from", "update set", "select * from"
    };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var input = value.ToString()!.ToLowerInvariant();

        foreach (var pattern in DangerousPatterns)
        {
            if (input.Contains(pattern.ToLowerInvariant()))
            {
                return new ValidationResult($"The {validationContext.DisplayName} field contains potentially dangerous content.");
            }
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates file size limits.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class MaxFileSizeAttribute : ValidationAttribute
{
    private readonly long _maxFileSize;

    public MaxFileSizeAttribute(long maxFileSize)
    {
        _maxFileSize = maxFileSize;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is IFormFile file)
        {
            if (file.Length > _maxFileSize)
            {
                return new ValidationResult($"The {validationContext.DisplayName} file size cannot exceed {_maxFileSize / 1024 / 1024}MB.");
            }
        }

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates allowed file extensions.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class AllowedExtensionsAttribute : ValidationAttribute
{
    private readonly string[] _extensions;

    public AllowedExtensionsAttribute(params string[] extensions)
    {
        _extensions = extensions;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName);
            if (!_extensions.Contains(extension.ToLower()))
            {
                return new ValidationResult($"The {validationContext.DisplayName} file type is not allowed. Allowed types: {string.Join(", ", _extensions)}");
            }
        }

        return ValidationResult.Success;
    }
}
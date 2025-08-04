using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using NeoServiceLayer.Core.Validation;

namespace NeoServiceLayer.Core.Models.Requests;

/// <summary>
/// Base request model with common validation properties.
/// </summary>
public abstract class ValidatedRequest
{
    /// <summary>
    /// Gets or sets the request ID for tracking.
    /// </summary>
    [StringLength(100, ErrorMessage = "Request ID cannot exceed 100 characters")]
    [Alphanumeric]
    public string? RequestId { get; set; }
}

/// <summary>
/// Request model for blockchain operations.
/// </summary>
public class BlockchainRequest : ValidatedRequest
{
    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    [Required(ErrorMessage = "Blockchain type is required")]
    [EnumDataType(typeof(BlockchainType), ErrorMessage = "Invalid blockchain type")]
    public string BlockchainType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the from address.
    /// </summary>
    [BlockchainAddress]
    public string? FromAddress { get; set; }

    /// <summary>
    /// Gets or sets the to address.
    /// </summary>
    [BlockchainAddress]
    public string? ToAddress { get; set; }
}

/// <summary>
/// Request model for transaction operations.
/// </summary>
public class TransactionRequest : BlockchainRequest
{
    /// <summary>
    /// Gets or sets the transaction amount.
    /// </summary>
    [PositiveNumber]
    [Range(0.00000001, 1000000000, ErrorMessage = "Amount must be between 0.00000001 and 1,000,000,000")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Gets or sets the gas limit.
    /// </summary>
    [PositiveNumber]
    [Range(21000, 10000000, ErrorMessage = "Gas limit must be between 21,000 and 10,000,000")]
    public long? GasLimit { get; set; }

    /// <summary>
    /// Gets or sets the transaction data.
    /// </summary>
    [HexString]
    [StringLength(10000, ErrorMessage = "Transaction data cannot exceed 10,000 characters")]
    public string? Data { get; set; }
}

/// <summary>
/// Request model for key generation.
/// </summary>
public class KeyGenerationRequest : ValidatedRequest
{
    /// <summary>
    /// Gets or sets the key type.
    /// </summary>
    [Required(ErrorMessage = "Key type is required")]
    [RegularExpression("^(secp256k1|ed25519|rsa)$", ErrorMessage = "Key type must be secp256k1, ed25519, or rsa")]
    public string KeyType { get; set; } = "secp256k1";

    /// <summary>
    /// Gets or sets the key name.
    /// </summary>
    [Required(ErrorMessage = "Key name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Key name must be between 3 and 100 characters")]
    [Alphanumeric]
    [SafeString]
    public string KeyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key description.
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [SafeString]
    public string? Description { get; set; }
}

/// <summary>
/// Request model for storage operations.
/// </summary>
public class StorageRequest : ValidatedRequest
{
    /// <summary>
    /// Gets or sets the storage key.
    /// </summary>
    [Required(ErrorMessage = "Storage key is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Storage key must be between 1 and 255 characters")]
    [Alphanumeric(allowHyphens: true, allowUnderscores: true)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    [StringLength(100, ErrorMessage = "Namespace cannot exceed 100 characters")]
    [Alphanumeric]
    public string? Namespace { get; set; }
}

/// <summary>
/// Request model for file upload operations.
/// </summary>
public class FileUploadRequest : ValidatedRequest
{
    /// <summary>
    /// Gets or sets the uploaded file.
    /// </summary>
    [Required(ErrorMessage = "File is required")]
    [MaxFileSize(10 * 1024 * 1024)] // 10MB
    [AllowedExtensions(".json", ".txt", ".csv", ".xml", ".pdf")]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// Gets or sets the file description.
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [SafeString]
    public string? Description { get; set; }
}

/// <summary>
/// Request model for pagination.
/// </summary>
public class PaginationRequest : ValidatedRequest
{
    private int _page = 1;
    private int _pageSize = 10;

    /// <summary>
    /// Gets or sets the page number.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page
    {
        get => _page;
        set => _page = value > 0 ? value : 1;
    }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 0 && value <= 100 ? value : 10;
    }

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    [StringLength(50, ErrorMessage = "Sort field cannot exceed 50 characters")]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Sort field must contain only alphanumeric characters and underscores")]
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    [RegularExpression("^(asc|desc)$", ErrorMessage = "Sort direction must be 'asc' or 'desc'")]
    public string? SortDirection { get; set; } = "asc";
}

/// <summary>
/// Request model for search operations.
/// </summary>
public class SearchRequest : PaginationRequest
{
    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    [Required(ErrorMessage = "Search query is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Search query must be between 1 and 200 characters")]
    [SafeString]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the search filters.
    /// </summary>
    public Dictionary<string, string>? Filters { get; set; }
}

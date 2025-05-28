using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Oracle;

/// <summary>
/// Represents an oracle request.
/// </summary>
public class OracleRequest
{
    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to fetch data from.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to extract data from the response.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the request headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public string Method { get; set; } = "GET";

    /// <summary>
    /// Gets or sets the request body.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timeout in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the retry count.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the retry delay in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the custom parameters.
    /// </summary>
    public Dictionary<string, string> CustomParameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the request timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents an oracle response.
/// </summary>
public class OracleResponse
{
    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the response timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the cryptographic proof.
    /// </summary>
    public string Proof { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the source URL.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source path.
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the custom metadata.
    /// </summary>
    public Dictionary<string, string> CustomMetadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the block height.
    /// </summary>
    public long BlockHeight { get; set; }

    /// <summary>
    /// Gets or sets the block hash.
    /// </summary>
    public string BlockHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    public string Signature { get; set; } = string.Empty;
}

/// <summary>
/// Represents a batch response from the oracle service.
/// </summary>
public class OracleBatchResponse
{
    /// <summary>
    /// Gets or sets the batch ID.
    /// </summary>
    public string BatchId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the individual responses.
    /// </summary>
    public IEnumerable<OracleResponse> Responses { get; set; } = new List<OracleResponse>();

    /// <summary>
    /// Gets or sets the cryptographic proof for the entire batch.
    /// </summary>
    public string Proof { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the batch timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets the block height.
    /// </summary>
    public long BlockHeight { get; set; }

    /// <summary>
    /// Gets or sets the block hash.
    /// </summary>
    public string BlockHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signature.
    /// </summary>
    public string Signature { get; set; } = string.Empty;
}

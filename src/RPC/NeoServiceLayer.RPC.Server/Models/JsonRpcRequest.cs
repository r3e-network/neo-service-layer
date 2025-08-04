using System.Text.Json.Serialization;

namespace NeoServiceLayer.RPC.Server.Models;

/// <summary>
/// Represents a JSON-RPC 2.0 request.
/// </summary>
public class JsonRpcRequest
{
    /// <summary>
    /// Gets or sets the JSON-RPC version. Must be "2.0".
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets the method name to invoke.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameters for the method.
    /// </summary>
    [JsonPropertyName("params")]
    public object? Params { get; set; }

    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; set; }
}

/// <summary>
/// Represents a JSON-RPC 2.0 response.
/// </summary>
public class JsonRpcResponse
{
    /// <summary>
    /// Gets or sets the JSON-RPC version. Must be "2.0".
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets the result of the method call.
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets the error information if the method call failed.
    /// </summary>
    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }

    /// <summary>
    /// Gets or sets the request ID this response corresponds to.
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; set; }
}

/// <summary>
/// Represents a JSON-RPC 2.0 error.
/// </summary>
public class JsonRpcError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional error data.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// Standard JSON-RPC error codes.
/// </summary>
public static class JsonRpcErrorCodes
{
    /// <summary>
    /// Parse error. Invalid JSON was received by the server.
    /// </summary>
    public const int ParseError = -32700;

    /// <summary>
    /// Invalid Request. The JSON sent is not a valid Request object.
    /// </summary>
    public const int InvalidRequest = -32600;

    /// <summary>
    /// Method not found. The method does not exist / is not available.
    /// </summary>
    public const int MethodNotFound = -32601;

    /// <summary>
    /// Invalid params. Invalid method parameter(s).
    /// </summary>
    public const int InvalidParams = -32602;

    /// <summary>
    /// Internal error. Internal JSON-RPC error.
    /// </summary>
    public const int InternalError = -32603;

    /// <summary>
    /// Server error. Reserved for implementation-defined server-errors.
    /// </summary>
    public const int ServerError = -32000;

    /// <summary>
    /// Authentication required.
    /// </summary>
    public const int AuthenticationRequired = -32001;

    /// <summary>
    /// Insufficient permissions.
    /// </summary>
    public const int InsufficientPermissions = -32002;

    /// <summary>
    /// Rate limit exceeded.
    /// </summary>
    public const int RateLimitExceeded = -32003;

    /// <summary>
    /// Service unavailable.
    /// </summary>
    public const int ServiceUnavailable = -32004;
}
namespace NeoServiceLayer.RPC.Server.Attributes;

/// <summary>
/// Marks a method as available for JSON-RPC calls.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class JsonRpcMethodAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the method name to use in JSON-RPC calls.
    /// If not specified, uses the format "service.method".
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Gets or sets the description of the method.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether authentication is required for this method.
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    /// Gets or sets the required roles for this method.
    /// </summary>
    public string[]? RequiredRoles { get; set; }

    /// <summary>
    /// Gets or sets the rate limit for this method (calls per minute).
    /// </summary>
    public int RateLimit { get; set; } = 60;

    /// <summary>
    /// Initializes a new instance of the JsonRpcMethodAttribute.
    /// </summary>
    public JsonRpcMethodAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the JsonRpcMethodAttribute with a specific method name.
    /// </summary>
    /// <param name="methodName">The method name to use in JSON-RPC calls.</param>
    public JsonRpcMethodAttribute(string methodName)
    {
        MethodName = methodName;
    }
}
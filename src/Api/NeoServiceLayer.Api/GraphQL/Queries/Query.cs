using HotChocolate;

namespace NeoServiceLayer.Api.GraphQL.Queries;

/// <summary>
/// Root query type for GraphQL schema.
/// </summary>
public class Query
{
    /// <summary>
    /// Gets the API version information.
    /// </summary>
    /// <returns>The API version information.</returns>
    [GraphQLDescription("Gets the API version information")]
    public VersionInfo GetVersion() => new VersionInfo
    {
        Version = "1.0.0",
        BuildDate = DateTime.UtcNow,
        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
    };
}

/// <summary>
/// Version information model.
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// Gets or sets the API version.
    /// </summary>
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the build date.
    /// </summary>
    public DateTime BuildDate { get; set; }
    
    /// <summary>
    /// Gets or sets the environment name.
    /// </summary>
    public string Environment { get; set; } = string.Empty;
}
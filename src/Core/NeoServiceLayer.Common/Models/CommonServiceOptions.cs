using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Common.Models;

/// <summary>
/// Common configuration options for all Neo Service Layer microservices
/// </summary>
public class CommonServiceOptions
{
    /// <summary>
    /// Service name for identification
    /// </summary>
    [Required]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Service version for API versioning
    /// </summary>
    [Required]
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Environment name (Development, Staging, Production)
    /// </summary>
    [Required]
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Maximum request timeout in seconds
    /// </summary>
    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable detailed error responses
    /// </summary>
    public bool DetailedErrors { get; set; } = true;

    /// <summary>
    /// Enable performance profiling
    /// </summary>
    public bool EnableProfiling { get; set; } = false;

    /// <summary>
    /// Health check timeout in seconds
    /// </summary>
    [Range(1, 60)]
    public int HealthCheckTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum allowed memory usage in MB
    /// </summary>
    [Range(64, 8192)]
    public int MaxMemoryUsageMB { get; set; } = 512;
}
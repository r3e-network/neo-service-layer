using HotChocolate;
using HotChocolate.Types;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Api.GraphQL.Types;

/// <summary>
/// GraphQL type for ServiceHealth.
/// </summary>
public class ServiceHealthType : EnumType<ServiceHealth>
{
    /// <summary>
    /// Configures the ServiceHealth enum type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    protected override void Configure(IEnumTypeDescriptor<ServiceHealth> descriptor)
    {
        descriptor.Name("ServiceHealth");
        descriptor.Description("Health status of a service");

        descriptor
            .Value(ServiceHealth.Healthy)
            .Description("Service is operating normally");

        descriptor
            .Value(ServiceHealth.Degraded)
            .Description("Service is operational but with reduced performance or functionality");

        descriptor
            .Value(ServiceHealth.Unhealthy)
            .Description("Service is experiencing issues");

        descriptor
            .Value(ServiceHealth.NotRunning)
            .Description("Service is not currently running");
    }
}

/// <summary>
/// GraphQL type for detailed health check result.
/// </summary>
public class HealthCheckResultType : ObjectType<HealthCheckResult>
{
    /// <summary>
    /// Configures the HealthCheckResult type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    protected override void Configure(IObjectTypeDescriptor<HealthCheckResult> descriptor)
    {
        descriptor.Name("HealthCheckResult");
        descriptor.Description("Detailed health check result for a service");

        descriptor
            .Field(f => f.ServiceName)
            .Description("Name of the service");

        descriptor
            .Field(f => f.Status)
            .Description("Current health status");

        descriptor
            .Field(f => f.CheckDuration)
            .Description("How long the health check took");

        descriptor
            .Field(f => f.Message)
            .Description("Additional status message");

        descriptor
            .Field(f => f.LastChecked)
            .Description("When the health check was performed");

        descriptor
            .Field(f => f.Dependencies)
            .Description("Health status of service dependencies");

        descriptor
            .Field(f => f.Metrics)
            .Description("Service-specific health metrics");

        // Add computed field for status color
        descriptor
            .Field("statusColor")
            .Type<NonNullType<StringType>>()
            .Description("Color code for status visualization")
            .Resolve(ctx =>
            {
                var result = ctx.Parent<HealthCheckResult>();
                return result.Status switch
                {
                    ServiceHealth.Healthy => "green",
                    ServiceHealth.Degraded => "yellow",
                    ServiceHealth.Unhealthy => "red",
                    ServiceHealth.NotRunning => "gray",
                    _ => "gray"
                };
            });
    }
}

/// <summary>
/// Health check result model.
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public ServiceHealth Status { get; set; }
    
    /// <summary>
    /// Gets or sets the check duration.
    /// </summary>
    public TimeSpan CheckDuration { get; set; }
    
    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// Gets or sets when the check was performed.
    /// </summary>
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the dependency health statuses.
    /// </summary>
    public Dictionary<string, ServiceHealth>? Dependencies { get; set; }
    
    /// <summary>
    /// Gets or sets the health metrics.
    /// </summary>
    public Dictionary<string, object>? Metrics { get; set; }
}
using HotChocolate;
using HotChocolate.Types;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class HealthQueries
{
    [GraphQLDescription("Gets system health status")]
    public async Task<SystemHealth> GetSystemHealth(
        [Service] IEnumerable<IService> services)
    {
        var healthChecks = new List<HealthCheckResult>();
        
        foreach (var service in services)
        {
            var start = DateTime.UtcNow;
            var health = await service.GetHealthAsync();
            var duration = DateTime.UtcNow - start;
            
            healthChecks.Add(new HealthCheckResult
            {
                ServiceName = service.Name,
                Status = health,
                CheckDuration = duration
            });
        }
        
        return new SystemHealth
        {
            Status = healthChecks.All(h => h.Status == ServiceHealth.Healthy) 
                ? ServiceHealth.Healthy 
                : ServiceHealth.Degraded,
            Services = healthChecks,
            Timestamp = DateTime.UtcNow
        };
    }
}

public class SystemHealth
{
    public ServiceHealth Status { get; set; }
    public List<HealthCheckResult> Services { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

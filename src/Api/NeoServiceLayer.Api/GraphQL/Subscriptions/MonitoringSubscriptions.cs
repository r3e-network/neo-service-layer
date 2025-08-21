using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Api.GraphQL.Subscriptions;

[ExtendObjectType(typeof(Subscription))]
public class MonitoringSubscriptions
{
    [Subscribe]
    [Topic]
    [Authorize(Roles = ["Admin", "Monitor"])]
    [GraphQLDescription("Subscribes to service health updates")]
    public async IAsyncEnumerable<ServiceHealthUpdate> OnServiceHealthChange(
        [EventMessage] ServiceHealthUpdate healthUpdate)
    {
        yield return healthUpdate;
    }
}

public class ServiceHealthUpdate
{
    public string ServiceName { get; set; } = string.Empty;
    public ServiceHealth OldStatus { get; set; }
    public ServiceHealth NewStatus { get; set; }
    public DateTime Timestamp { get; set; }
}

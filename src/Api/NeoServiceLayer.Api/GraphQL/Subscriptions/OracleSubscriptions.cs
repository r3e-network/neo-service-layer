using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;

namespace NeoServiceLayer.Api.GraphQL.Subscriptions;

[ExtendObjectType(typeof(Subscription))]
public class OracleSubscriptions
{
    [Subscribe]
    [Topic]
    [Authorize]
    [GraphQLDescription("Subscribes to price updates")]
    public async IAsyncEnumerable<PriceUpdate> OnPriceUpdate(
        [EventMessage] PriceUpdate priceUpdate,
        string? assetId = null)
    {
        if (assetId == null || priceUpdate.AssetId == assetId)
        {
            yield return priceUpdate;
        }
    }
}

public class PriceUpdate
{
    public string AssetId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}

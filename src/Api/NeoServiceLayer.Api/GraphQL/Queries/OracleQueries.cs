using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NeoServiceLayer.Services.Oracle;

namespace NeoServiceLayer.Api.GraphQL.Queries;

/// <summary>
/// GraphQL queries for oracle operations.
/// </summary>
[ExtendObjectType(typeof(Query))]
public class OracleQueries
{
    /// <summary>
    /// Gets the latest price for an asset.
    /// </summary>
    [Authorize]
    [GraphQLDescription("Gets the latest price for an asset")]
    public async Task<PriceData?> GetLatestPrice(
        string assetId,
        [Service] IOracleService oracleService)
    {
        return await oracleService.GetLatestPriceAsync(assetId);
    }

    /// <summary>
    /// Gets price history for an asset.
    /// </summary>
    [Authorize]
    [GraphQLDescription("Gets price history for an asset")]
    [UsePaging]
    public async Task<IEnumerable<PriceData>> GetPriceHistory(
        string assetId,
        DateTime startDate,
        DateTime endDate,
        [Service] IOracleService oracleService)
    {
        return await oracleService.GetPriceHistoryAsync(assetId, startDate, endDate);
    }
}
using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;

namespace NeoServiceLayer.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class OracleMutations
{
    [Authorize(Roles = ["Admin"])]
    [GraphQLDescription("Updates price data")]
    public async Task<bool> UpdatePrice(string assetId, decimal price)
    {
        // Placeholder
        return true;
    }
}

using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;

namespace NeoServiceLayer.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class ProofOfReserveMutations
{
    [Authorize(Roles = ["Admin"])]
    [GraphQLDescription("Generates new proof")]
    public async Task<bool> GenerateProof(string assetId)
    {
        // Placeholder
        return true;
    }
}

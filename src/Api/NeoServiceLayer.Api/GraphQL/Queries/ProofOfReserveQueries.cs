using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NeoServiceLayer.Services.ProofOfReserve;

namespace NeoServiceLayer.Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class ProofOfReserveQueries
{
    [Authorize]
    [GraphQLDescription("Gets latest proof of reserve")]
    public async Task<ProofOfReserveData> GetLatestProof(
        string assetId,
        [Service] IProofOfReserveService proofService)
    {
        return await proofService.GetLatestProofAsync(assetId);
    }
}

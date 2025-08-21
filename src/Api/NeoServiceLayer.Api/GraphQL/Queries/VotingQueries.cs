using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NeoServiceLayer.Services.Voting;

namespace NeoServiceLayer.Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class VotingQueries
{
    [Authorize]
    [GraphQLDescription("Gets active proposals")]
    [UsePaging]
    public async Task<IEnumerable<VotingProposal>> GetActiveProposals(
        [Service] IVotingService votingService)
    {
        return await votingService.GetActiveProposalsAsync();
    }
}

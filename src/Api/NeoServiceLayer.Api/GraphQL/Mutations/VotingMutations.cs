using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;

namespace NeoServiceLayer.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class VotingMutations
{
    [Authorize]
    [GraphQLDescription("Creates a new proposal")]
    public async Task<bool> CreateProposal(string title, string description)
    {
        // Placeholder
        return true;
    }
    
    [Authorize]
    [GraphQLDescription("Casts a vote")]
    public async Task<bool> Vote(string proposalId, bool support)
    {
        // Placeholder
        return true;
    }
}

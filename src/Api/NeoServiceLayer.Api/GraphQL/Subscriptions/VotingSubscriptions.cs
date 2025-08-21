using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;

namespace NeoServiceLayer.Api.GraphQL.Subscriptions;

[ExtendObjectType(typeof(Subscription))]
public class VotingSubscriptions
{
    [Subscribe]
    [Topic]
    [Authorize]
    [GraphQLDescription("Subscribes to voting updates")]
    public async IAsyncEnumerable<VoteUpdate> OnVoteUpdate(
        [EventMessage] VoteUpdate voteUpdate,
        string? proposalId = null)
    {
        if (proposalId == null || voteUpdate.ProposalId == proposalId)
        {
            yield return voteUpdate;
        }
    }
}

public class VoteUpdate
{
    public string ProposalId { get; set; } = string.Empty;
    public int YesVotes { get; set; }
    public int NoVotes { get; set; }
    public DateTime Timestamp { get; set; }
}

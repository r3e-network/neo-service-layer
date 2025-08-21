using HotChocolate;
using HotChocolate.Types;
using NeoServiceLayer.Services.Voting;

namespace NeoServiceLayer.Api.GraphQL.Types;

/// <summary>
/// GraphQL type for VotingProposal.
/// </summary>
public class VotingProposalType : ObjectType<VotingProposal>
{
    /// <summary>
    /// Configures the VotingProposal type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    protected override void Configure(IObjectTypeDescriptor<VotingProposal> descriptor)
    {
        descriptor.Name("VotingProposal");
        descriptor.Description("Voting proposal information");

        descriptor
            .Field(f => f.ProposalId)
            .Description("Unique identifier for the proposal");

        descriptor
            .Field(f => f.Title)
            .Description("Title of the proposal");

        descriptor
            .Field(f => f.Description)
            .Description("Detailed description of the proposal");

        descriptor
            .Field(f => f.Status)
            .Description("Current status of the proposal");

        descriptor
            .Field(f => f.CreatedBy)
            .Description("Address of the proposal creator");

        descriptor
            .Field(f => f.CreatedAt)
            .Description("When the proposal was created");

        descriptor
            .Field(f => f.StartTime)
            .Description("When voting starts");

        descriptor
            .Field(f => f.EndTime)
            .Description("When voting ends");

        descriptor
            .Field(f => f.YesVotes)
            .Description("Number of yes votes");

        descriptor
            .Field(f => f.NoVotes)
            .Description("Number of no votes");

        descriptor
            .Field(f => f.QuorumRequired)
            .Description("Minimum votes required for validity");

        // Add computed fields
        descriptor
            .Field("isActive")
            .Type<NonNullType<BooleanType>>()
            .Description("Whether voting is currently active")
            .Resolve(ctx =>
            {
                var proposal = ctx.Parent<VotingProposal>();
                var now = DateTime.UtcNow;
                return proposal.Status == "Active" && 
                       now >= proposal.StartTime && 
                       now <= proposal.EndTime;
            });

        descriptor
            .Field("totalVotes")
            .Type<NonNullType<IntType>>()
            .Description("Total number of votes cast")
            .Resolve(ctx =>
            {
                var proposal = ctx.Parent<VotingProposal>();
                return proposal.YesVotes + proposal.NoVotes;
            });
    }
}
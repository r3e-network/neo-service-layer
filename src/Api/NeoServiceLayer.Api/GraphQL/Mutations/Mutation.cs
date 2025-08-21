using HotChocolate;

namespace NeoServiceLayer.Api.GraphQL.Mutations;

/// <summary>
/// Root mutation type for GraphQL schema.
/// </summary>
public class Mutation
{
    /// <summary>
    /// Placeholder mutation to establish the root type.
    /// </summary>
    /// <returns>Success status.</returns>
    [GraphQLDescription("Placeholder mutation for root type")]
    public bool Ping() => true;
}
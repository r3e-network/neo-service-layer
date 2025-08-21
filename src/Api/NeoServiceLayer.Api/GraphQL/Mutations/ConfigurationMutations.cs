using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;

namespace NeoServiceLayer.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class ConfigurationMutations
{
    [Authorize(Roles = ["Admin"])]
    [GraphQLDescription("Updates configuration")]
    public async Task<bool> UpdateConfig(string key, string value)
    {
        // Placeholder
        return true;
    }
}

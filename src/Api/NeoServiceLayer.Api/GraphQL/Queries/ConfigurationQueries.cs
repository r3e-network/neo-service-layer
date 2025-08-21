using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NeoServiceLayer.Services.Configuration;

namespace NeoServiceLayer.Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class ConfigurationQueries
{
    [Authorize(Roles = ["Admin"])]
    [GraphQLDescription("Gets configuration value")]
    public async Task<string?> GetConfigValue(
        string key,
        [Service] IConfigurationService configService)
    {
        return await configService.GetValueAsync(key);
    }
}

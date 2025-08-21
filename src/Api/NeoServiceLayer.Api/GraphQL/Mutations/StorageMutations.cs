using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;

namespace NeoServiceLayer.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class StorageMutations
{
    [Authorize]
    [GraphQLDescription("Uploads an object")]
    public async Task<bool> UploadObject(string bucket, string key, string content)
    {
        // Placeholder
        return true;
    }
}

using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NeoServiceLayer.Services.Storage;

namespace NeoServiceLayer.Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class StorageQueries
{
    [Authorize]
    [GraphQLDescription("Gets object metadata")]
    public async Task<StorageMetadata> GetObjectMetadata(
        string bucket,
        string key,
        [Service] IStorageService storageService)
    {
        return await storageService.GetMetadataAsync(bucket, key);
    }
}

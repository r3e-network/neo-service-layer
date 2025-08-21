using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;

namespace NeoServiceLayer.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class PredictionMutations
{
    [Authorize]
    [GraphQLDescription("Trains prediction model")]
    public async Task<bool> TrainModel(string modelId)
    {
        // Placeholder
        return true;
    }
}

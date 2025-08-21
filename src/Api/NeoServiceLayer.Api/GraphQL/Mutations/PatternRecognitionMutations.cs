using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;

namespace NeoServiceLayer.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class PatternRecognitionMutations
{
    [Authorize]
    [GraphQLDescription("Trains pattern model")]
    public async Task<bool> TrainModel(string modelId)
    {
        // Placeholder
        return true;
    }
}

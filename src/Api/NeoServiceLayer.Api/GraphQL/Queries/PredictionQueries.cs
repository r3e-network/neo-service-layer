using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NeoServiceLayer.AI.Prediction;

namespace NeoServiceLayer.Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class PredictionQueries
{
    [Authorize]
    [GraphQLDescription("Gets a prediction")]
    public async Task<PredictionResult> GetPrediction(
        string modelId,
        Dictionary<string, object> features,
        [Service] IPredictionService predictionService)
    {
        return await predictionService.PredictAsync(modelId, features);
    }
}

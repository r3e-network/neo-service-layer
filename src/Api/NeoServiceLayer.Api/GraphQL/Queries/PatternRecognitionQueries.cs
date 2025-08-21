using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NeoServiceLayer.AI.PatternRecognition;

namespace NeoServiceLayer.Api.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class PatternRecognitionQueries
{
    [Authorize]
    [GraphQLDescription("Analyzes data for patterns")]
    public async Task<PatternResult> AnalyzePattern(
        Dictionary<string, object> data,
        [Service] IPatternRecognitionService patternService)
    {
        return await patternService.AnalyzeAsync(data);
    }
}
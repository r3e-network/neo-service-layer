using HotChocolate;
using HotChocolate.Types;
using NeoServiceLayer.AI.PatternRecognition;

namespace NeoServiceLayer.Api.GraphQL.Types;

/// <summary>
/// GraphQL type for PatternResult.
/// </summary>
public class PatternResultType : ObjectType<PatternResult>
{
    /// <summary>
    /// Configures the PatternResult type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    protected override void Configure(IObjectTypeDescriptor<PatternResult> descriptor)
    {
        descriptor.Name("PatternResult");
        descriptor.Description("Result from pattern recognition analysis");

        descriptor
            .Field(f => f.PatternId)
            .Description("Unique identifier for the detected pattern");

        descriptor
            .Field(f => f.PatternType)
            .Description("Type of pattern detected");

        descriptor
            .Field(f => f.Confidence)
            .Description("Confidence score (0-1)");

        descriptor
            .Field(f => f.MatchedFeatures)
            .Description("Features that matched the pattern");

        descriptor
            .Field(f => f.Anomalies)
            .Description("Any anomalies detected");

        descriptor
            .Field(f => f.Timestamp)
            .Description("When the pattern was detected");
    }
}
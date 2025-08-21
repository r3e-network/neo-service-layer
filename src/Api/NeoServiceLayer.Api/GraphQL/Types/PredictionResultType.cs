using HotChocolate;
using HotChocolate.Types;
using NeoServiceLayer.AI.Prediction;

namespace NeoServiceLayer.Api.GraphQL.Types;

/// <summary>
/// GraphQL type for PredictionResult.
/// </summary>
public class PredictionResultType : ObjectType<PredictionResult>
{
    /// <summary>
    /// Configures the PredictionResult type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    protected override void Configure(IObjectTypeDescriptor<PredictionResult> descriptor)
    {
        descriptor.Name("PredictionResult");
        descriptor.Description("Result from AI prediction service");

        descriptor
            .Field(f => f.PredictionId)
            .Description("Unique identifier for the prediction");

        descriptor
            .Field(f => f.ModelId)
            .Description("ID of the model used for prediction");

        descriptor
            .Field(f => f.Confidence)
            .Description("Confidence score (0-1)");

        descriptor
            .Field(f => f.PredictedValue)
            .Description("The predicted value or class");

        descriptor
            .Field(f => f.Probabilities)
            .Description("Probability distribution for classification tasks");

        descriptor
            .Field(f => f.Features)
            .Description("Input features used for prediction");

        descriptor
            .Field(f => f.Timestamp)
            .Description("When the prediction was made");

        descriptor
            .Field(f => f.ProcessingTime)
            .Description("Time taken to generate prediction");
    }
}
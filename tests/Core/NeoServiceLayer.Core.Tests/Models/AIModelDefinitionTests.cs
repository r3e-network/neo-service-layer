using FluentAssertions;
using NeoServiceLayer.Core.Models;
using Xunit;

namespace NeoServiceLayer.Core.Tests.Models;

/// <summary>
/// Tests for AIModelDefinition class to verify property behavior and validation.
/// </summary>
public class AIModelDefinitionTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var definition = new AIModelDefinition();

        // Assert
        definition.Name.Should().BeEmpty();
        definition.Type.Should().Be(AIModelType.Prediction);
        definition.Version.Should().Be("1.0.0");
        definition.Architecture.Should().BeEmpty();
        definition.InputFeatures.Should().NotBeNull().And.BeEmpty();
        definition.OutputFeatures.Should().NotBeNull().And.BeEmpty();
        definition.Hyperparameters.Should().NotBeNull().And.BeEmpty();
        definition.TrainingConfig.Should().NotBeNull().And.BeEmpty();
        definition.Description.Should().BeEmpty();
        definition.Parameters.Should().NotBeNull().And.BeEmpty();
        definition.TrainingData.Should().NotBeNull().And.BeEmpty();
        definition.IsActive.Should().BeTrue();
        definition.Metadata.Should().NotBeNull().And.BeEmpty();
        definition.Algorithm.Should().BeEmpty();
        definition.TrainingParameters.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var definition = new AIModelDefinition();
        var inputFeatures = new List<string> { "feature1", "feature2" };
        var outputFeatures = new List<string> { "output1" };
        var hyperparameters = new Dictionary<string, object> { ["learning_rate"] = 0.01 };
        var trainingConfig = new Dictionary<string, object> { ["epochs"] = 100 };
        var parameters = new Dictionary<string, object> { ["param1"] = "value1" };
        var trainingData = new Dictionary<string, object> { ["dataset"] = "training_set" };
        var metadata = new Dictionary<string, object> { ["creator"] = "test_user" };
        var trainingParameters = new Dictionary<string, object> { ["batch_size"] = 32 };

        // Act
        definition.Name = "Test Model Definition";
        definition.Type = AIModelType.NeuralNetwork;
        definition.Version = "2.1.0";
        definition.Architecture = "CNN";
        definition.InputFeatures = inputFeatures;
        definition.OutputFeatures = outputFeatures;
        definition.Hyperparameters = hyperparameters;
        definition.TrainingConfig = trainingConfig;
        definition.Description = "Test model for classification";
        definition.Parameters = parameters;
        definition.TrainingData = trainingData;
        definition.IsActive = false;
        definition.Metadata = metadata;
        definition.Algorithm = "Convolutional Neural Network";
        definition.TrainingParameters = trainingParameters;

        // Assert
        definition.Name.Should().Be("Test Model Definition");
        definition.Type.Should().Be(AIModelType.NeuralNetwork);
        definition.Version.Should().Be("2.1.0");
        definition.Architecture.Should().Be("CNN");
        definition.InputFeatures.Should().BeEquivalentTo(inputFeatures);
        definition.OutputFeatures.Should().BeEquivalentTo(outputFeatures);
        definition.Hyperparameters.Should().BeEquivalentTo(hyperparameters);
        definition.TrainingConfig.Should().BeEquivalentTo(trainingConfig);
        definition.Description.Should().Be("Test model for classification");
        definition.Parameters.Should().BeEquivalentTo(parameters);
        definition.TrainingData.Should().BeEquivalentTo(trainingData);
        definition.IsActive.Should().BeFalse();
        definition.Metadata.Should().BeEquivalentTo(metadata);
        definition.Algorithm.Should().Be("Convolutional Neural Network");
        definition.TrainingParameters.Should().BeEquivalentTo(trainingParameters);
    }

    [Theory]
    [InlineData(AIModelType.Prediction)]
    [InlineData(AIModelType.PatternRecognition)]
    [InlineData(AIModelType.Classification)]
    [InlineData(AIModelType.Regression)]
    [InlineData(AIModelType.NeuralNetwork)]
    [InlineData(AIModelType.DecisionTree)]
    [InlineData(AIModelType.Clustering)]
    [InlineData(AIModelType.NLP)]
    [InlineData(AIModelType.ComputerVision)]
    public void Type_ShouldAcceptAllValidEnumValues(AIModelType modelType)
    {
        // Arrange
        var definition = new AIModelDefinition
        {
            // Act
            Type = modelType
        };

        // Assert
        definition.Type.Should().Be(modelType);
    }

    [Fact]
    public void Collections_ShouldBeInitializedAndModifiable()
    {
        // Arrange
        var definition = new AIModelDefinition();

        // Act
        definition.InputFeatures.Add("new_feature");
        definition.OutputFeatures.Add("new_output");
        definition.Hyperparameters["new_param"] = 123;
        definition.TrainingConfig["new_config"] = true;
        definition.Parameters["new_param"] = "new_value";
        definition.TrainingData["new_data"] = new[] { 1, 2, 3 };
        definition.Metadata["new_meta"] = DateTime.UtcNow;
        definition.TrainingParameters["new_training"] = 0.5;

        // Assert
        definition.InputFeatures.Should().Contain("new_feature");
        definition.OutputFeatures.Should().Contain("new_output");
        definition.Hyperparameters.Should().ContainKey("new_param").WhoseValue.Should().Be(123);
        definition.TrainingConfig.Should().ContainKey("new_config").WhoseValue.Should().Be(true);
        definition.Parameters.Should().ContainKey("new_param").WhoseValue.Should().Be("new_value");
        definition.TrainingData.Should().ContainKey("new_data");
        definition.Metadata.Should().ContainKey("new_meta");
        definition.TrainingParameters.Should().ContainKey("new_training").WhoseValue.Should().Be(0.5);
    }
}

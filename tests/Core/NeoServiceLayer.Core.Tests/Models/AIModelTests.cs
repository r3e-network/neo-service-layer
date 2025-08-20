using NeoServiceLayer.Core.Models;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


namespace NeoServiceLayer.Core.Tests.Models;

/// <summary>
/// Tests for AIModel class to verify property behavior and validation.
/// </summary>
public class AIModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var model = new AIModel();

        // Assert
        model.Id.Should().BeEmpty();
        model.Name.Should().BeEmpty();
        model.Type.Should().Be(AIModelType.Prediction);
        model.Version.Should().Be("1.0.0");
        model.Description.Should().BeEmpty();
        model.ModelData.Should().BeEmpty();
        model.Configuration.Should().NotBeNull().And.BeEmpty();
        model.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        model.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        model.IsActive.Should().BeTrue();
        model.LoadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        model.IsLoaded.Should().BeFalse();
        model.Accuracy.Should().Be(0);
        model.Parameters.Should().NotBeNull().And.BeEmpty();
        model.TrainedAt.Should().BeNull();
        model.TrainingMetrics.Should().BeNull();
    }

    [Fact]
    public void ModelId_ShouldBeAliasForId()
    {
        // Arrange
        var model = new AIModel();
        const string testId = "test-model-123";

        // Act
        model.ModelId = testId;

        // Assert
        model.Id.Should().Be(testId);
        model.ModelId.Should().Be(testId);
    }

    [Fact]
    public void Id_ShouldUpdateModelId()
    {
        // Arrange
        var model = new AIModel();
        const string testId = "test-model-456";

        // Act
        model.Id = testId;

        // Assert
        model.ModelId.Should().Be(testId);
        model.Id.Should().Be(testId);
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var model = new AIModel();
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var testConfig = new Dictionary<string, object> { ["key1"] = "value1" };
        var testParams = new Dictionary<string, object> { ["param1"] = 42 };
        var testMetrics = new Dictionary<string, object> { ["accuracy"] = 0.95 };
        var testTime = DateTime.UtcNow.AddDays(-1);

        // Act
        model.Id = "test-id";
        model.Name = "Test Model";
        model.Type = AIModelType.PatternRecognition;
        model.Version = "2.0.0";
        model.Description = "Test Description";
        model.ModelData = testData;
        model.Configuration = testConfig;
        model.CreatedAt = testTime;
        model.UpdatedAt = testTime;
        model.IsActive = false;
        model.LoadedAt = testTime;
        model.IsLoaded = true;
        model.Accuracy = 0.85;
        model.Parameters = testParams;
        model.TrainedAt = testTime;
        model.TrainingMetrics = testMetrics;

        // Assert
        model.Id.Should().Be("test-id");
        model.Name.Should().Be("Test Model");
        model.Type.Should().Be(AIModelType.PatternRecognition);
        model.Version.Should().Be("2.0.0");
        model.Description.Should().Be("Test Description");
        model.ModelData.Should().BeEquivalentTo(testData);
        model.Configuration.Should().BeEquivalentTo(testConfig);
        model.CreatedAt.Should().Be(testTime);
        model.UpdatedAt.Should().Be(testTime);
        model.IsActive.Should().BeFalse();
        model.LoadedAt.Should().Be(testTime);
        model.IsLoaded.Should().BeTrue();
        model.Accuracy.Should().Be(0.85);
        model.Parameters.Should().BeEquivalentTo(testParams);
        model.TrainedAt.Should().Be(testTime);
        model.TrainingMetrics.Should().BeEquivalentTo(testMetrics);
    }
}

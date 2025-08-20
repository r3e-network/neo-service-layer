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
/// Tests for AIInferenceResult class to verify property behavior and aliases.
/// </summary>
public class AIInferenceResultTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var result = new AIInferenceResult();

        // Assert
        result.ModelId.Should().BeEmpty();
        result.Results.Should().NotBeNull().And.BeEmpty();
        result.Confidence.Should().Be(0);
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.ProcessingTimeMs.Should().Be(0);
        result.Metadata.Should().NotBeNull().And.BeEmpty();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Result_ShouldBeAliasForResults()
    {
        // Arrange
        var result = new AIInferenceResult();
        var testResults = new Dictionary<string, object> { ["key1"] = "value1" };

        // Act
        result.Result = testResults;

        // Assert
        result.Results.Should().BeEquivalentTo(testResults);
        result.Result.Should().BeEquivalentTo(testResults);
    }

    [Fact]
    public void Result_WhenSetToNonDictionary_ShouldCreateEmptyDictionary()
    {
        // Arrange
        var result = new AIInferenceResult
        {
            // Act
            Result = "not a dictionary"
        };

        // Assert
        result.Results.Should().NotBeNull().And.BeEmpty();
        ((Dictionary<string, object>)result.Result).Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ExecutedAt_ShouldBeAliasForTimestamp()
    {
        // Arrange
        var result = new AIInferenceResult();
        var testTime = DateTime.UtcNow.AddMinutes(-5);

        // Act
        result.ExecutedAt = testTime;

        // Assert
        result.Timestamp.Should().Be(testTime);
        result.ExecutedAt.Should().Be(testTime);
    }

    [Fact]
    public void ExecutionTimeMs_ShouldBeAliasForProcessingTimeMs()
    {
        // Arrange
        var result = new AIInferenceResult();
        const long testTime = 1500;

        // Act
        result.ExecutionTimeMs = testTime;

        // Assert
        result.ProcessingTimeMs.Should().Be(testTime);
        result.ExecutionTimeMs.Should().Be(testTime);
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var result = new AIInferenceResult();
        var testResults = new Dictionary<string, object> { ["prediction"] = 0.85 };
        var testMetadata = new Dictionary<string, object> { ["version"] = "1.0" };
        var testTime = DateTime.UtcNow.AddMinutes(-10);

        // Act
        result.ModelId = "test-model-123";
        result.Results = testResults;
        result.Confidence = 0.95;
        result.Timestamp = testTime;
        result.ProcessingTimeMs = 2500;
        result.Metadata = testMetadata;
        result.Success = false;

        // Assert
        result.ModelId.Should().Be("test-model-123");
        result.Results.Should().BeEquivalentTo(testResults);
        result.Confidence.Should().Be(0.95);
        result.Timestamp.Should().Be(testTime);
        result.ProcessingTimeMs.Should().Be(2500);
        result.Metadata.Should().BeEquivalentTo(testMetadata);
        result.Success.Should().BeFalse();
    }
}

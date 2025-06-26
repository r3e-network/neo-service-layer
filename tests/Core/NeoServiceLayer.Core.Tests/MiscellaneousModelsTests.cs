using FluentAssertions;
using NeoServiceLayer.Core;
using Xunit;

namespace NeoServiceLayer.Core.Tests;

/// <summary>
/// Tests for Miscellaneous model classes to verify property behavior and default values.
/// </summary>
public class MiscellaneousModelsTests
{
    #region ProofRequest Tests

    [Fact]
    public void ProofRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new ProofRequest();

        // Assert
        request.CircuitId.Should().BeEmpty();
        request.PublicInputs.Should().NotBeNull().And.BeEmpty();
        request.PrivateInputs.Should().NotBeNull().And.BeEmpty();
        request.ProofSystem.Should().Be("groth16");
        request.Parameters.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ProofRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new ProofRequest();
        var publicInputs = new Dictionary<string, object> { ["public1"] = 123 };
        var privateInputs = new Dictionary<string, object> { ["private1"] = "secret" };
        var parameters = new Dictionary<string, object> { ["curve"] = "bn254" };

        // Act
        request.CircuitId = "circuit-misc-123";
        request.PublicInputs = publicInputs;
        request.PrivateInputs = privateInputs;
        request.ProofSystem = "plonk";
        request.Parameters = parameters;

        // Assert
        request.CircuitId.Should().Be("circuit-misc-123");
        request.PublicInputs.Should().BeEquivalentTo(publicInputs);
        request.PrivateInputs.Should().BeEquivalentTo(privateInputs);
        request.ProofSystem.Should().Be("plonk");
        request.Parameters.Should().BeEquivalentTo(parameters);
    }

    #endregion

    #region ProofResult Tests

    [Fact]
    public void ProofResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new ProofResult();

        // Assert
        result.ProofId.Should().NotBeEmpty();
        Guid.TryParse(result.ProofId, out _).Should().BeTrue();
        result.Proof.Should().BeEmpty();
        result.PublicSignals.Should().BeEmpty();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.VerificationKey.Should().BeEmpty();
    }

    [Fact]
    public void ProofResult_Properties_ShouldBeSettable()
    {
        // Arrange
        var result = new ProofResult();
        var publicSignals = new[] { "signal1", "signal2" };
        var generatedAt = DateTime.UtcNow.AddMinutes(-5);

        // Act
        result.ProofId = "proof-misc-456";
        result.Proof = "0xproof123";
        result.PublicSignals = publicSignals;
        result.GeneratedAt = generatedAt;
        result.VerificationKey = "0xverkey456";

        // Assert
        result.ProofId.Should().Be("proof-misc-456");
        result.Proof.Should().Be("0xproof123");
        result.PublicSignals.Should().BeEquivalentTo(publicSignals);
        result.GeneratedAt.Should().Be(generatedAt);
        result.VerificationKey.Should().Be("0xverkey456");
    }

    #endregion

    #region ProofVerification Tests

    [Fact]
    public void ProofVerification_ShouldInitializeWithDefaults()
    {
        // Act
        var verification = new ProofVerification();

        // Assert
        verification.Proof.Should().BeEmpty();
        verification.PublicSignals.Should().BeEmpty();
        verification.VerificationKey.Should().BeEmpty();
        verification.CircuitId.Should().BeEmpty();
    }

    [Fact]
    public void ProofVerification_Properties_ShouldBeSettable()
    {
        // Arrange
        var verification = new ProofVerification();
        var publicSignals = new[] { "verify1", "verify2", "verify3" };

        // Act
        verification.Proof = "0xverifyproof789";
        verification.PublicSignals = publicSignals;
        verification.VerificationKey = "0xverifykey123";
        verification.CircuitId = "circuit-verify-456";

        // Assert
        verification.Proof.Should().Be("0xverifyproof789");
        verification.PublicSignals.Should().BeEquivalentTo(publicSignals);
        verification.VerificationKey.Should().Be("0xverifykey123");
        verification.CircuitId.Should().Be("circuit-verify-456");
    }

    #endregion

    #region ZkComputationRequest Tests

    [Fact]
    public void ZkComputationRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new ZkComputationRequest();

        // Assert
        request.ComputationId.Should().NotBeEmpty();
        Guid.TryParse(request.ComputationId, out _).Should().BeTrue();
        request.CircuitId.Should().BeEmpty();
        request.Inputs.Should().NotBeNull().And.BeEmpty();
        request.PrivateInputs.Should().NotBeNull().And.BeEmpty();
        request.ComputationType.Should().Be("proof-generation");
        request.Parameters.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ZkComputationRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new ZkComputationRequest();
        var inputs = new Dictionary<string, object> { ["input1"] = 42 };
        var privateInputs = new Dictionary<string, object> { ["secret"] = "value" };
        var parameters = new Dictionary<string, object> { ["timeout"] = 30 };

        // Act
        request.ComputationId = "comp-789";
        request.CircuitId = "circuit-comp-123";
        request.Inputs = inputs;
        request.PrivateInputs = privateInputs;
        request.ComputationType = "verification";
        request.Parameters = parameters;

        // Assert
        request.ComputationId.Should().Be("comp-789");
        request.CircuitId.Should().Be("circuit-comp-123");
        request.Inputs.Should().BeEquivalentTo(inputs);
        request.PrivateInputs.Should().BeEquivalentTo(privateInputs);
        request.ComputationType.Should().Be("verification");
        request.Parameters.Should().BeEquivalentTo(parameters);
    }

    #endregion

    #region ZkComputationResult Tests

    [Fact]
    public void ZkComputationResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new ZkComputationResult();

        // Assert
        result.ComputationId.Should().BeEmpty();
        result.CircuitId.Should().BeEmpty();
        result.Results.Should().BeEmpty();
        result.Proof.Should().BeEmpty();
        result.ComputedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ZkComputationResult_Properties_ShouldBeSettable()
    {
        // Arrange
        var result = new ZkComputationResult();
        var results = new object[] { 42, "result", true };
        var metadata = new Dictionary<string, object> { ["execution_time"] = 1500 };
        var computedAt = DateTime.UtcNow.AddMinutes(-2);

        // Act
        result.ComputationId = "comp-result-456";
        result.CircuitId = "circuit-result-789";
        result.Results = results;
        result.Proof = "0xresultproof123";
        result.ComputedAt = computedAt;
        result.IsValid = false;
        result.ErrorMessage = "computation failed";
        result.Metadata = metadata;

        // Assert
        result.ComputationId.Should().Be("comp-result-456");
        result.CircuitId.Should().Be("circuit-result-789");
        result.Results.Should().BeEquivalentTo(results);
        result.Proof.Should().Be("0xresultproof123");
        result.ComputedAt.Should().Be(computedAt);
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("computation failed");
        result.Metadata.Should().BeEquivalentTo(metadata);
    }

    #endregion

    #region CryptoKeyInfo Tests

    [Fact]
    public void CryptoKeyInfo_ShouldInitializeWithDefaults()
    {
        // Act
        var keyInfo = new CryptoKeyInfo();

        // Assert
        keyInfo.KeyId.Should().BeEmpty();
        keyInfo.KeyType.Should().BeEmpty();
        keyInfo.Algorithm.Should().BeEmpty();
        keyInfo.KeySize.Should().Be(0);
        keyInfo.Purpose.Should().BeEmpty();
        keyInfo.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        keyInfo.ExpiresAt.Should().BeNull();
        keyInfo.IsActive.Should().BeTrue();
        keyInfo.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void CryptoKeyInfo_Properties_ShouldBeSettable()
    {
        // Arrange
        var keyInfo = new CryptoKeyInfo();
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var expiresAt = DateTime.UtcNow.AddYears(1);
        var metadata = new Dictionary<string, object> { ["issuer"] = "test-ca" };

        // Act
        keyInfo.KeyId = "key-crypto-123";
        keyInfo.KeyType = "RSA";
        keyInfo.Algorithm = "RSA-OAEP";
        keyInfo.KeySize = 2048;
        keyInfo.Purpose = "encryption";
        keyInfo.CreatedAt = createdAt;
        keyInfo.ExpiresAt = expiresAt;
        keyInfo.IsActive = false;
        keyInfo.Metadata = metadata;

        // Assert
        keyInfo.KeyId.Should().Be("key-crypto-123");
        keyInfo.KeyType.Should().Be("RSA");
        keyInfo.Algorithm.Should().Be("RSA-OAEP");
        keyInfo.KeySize.Should().Be(2048);
        keyInfo.Purpose.Should().Be("encryption");
        keyInfo.CreatedAt.Should().Be(createdAt);
        keyInfo.ExpiresAt.Should().Be(expiresAt);
        keyInfo.IsActive.Should().BeFalse();
        keyInfo.Metadata.Should().BeEquivalentTo(metadata);
    }

    #endregion

    #region FairTransactionRequest Tests

    [Fact]
    public void FairTransactionRequest_ShouldInitializeWithDefaults()
    {
        // Act
        var request = new FairTransactionRequest();

        // Assert
        request.TransactionId.Should().NotBeEmpty();
        Guid.TryParse(request.TransactionId, out _).Should().BeTrue();
        request.From.Should().BeEmpty();
        request.To.Should().BeEmpty();
        request.Value.Should().Be(0);
        request.Data.Should().BeEmpty();
        request.GasLimit.Should().Be(0);
        request.ProtectionLevel.Should().Be("Standard");
        request.MaxSlippage.Should().Be(0);
        request.ExecuteAfter.Should().BeNull();
        request.ExecuteBefore.Should().BeNull();
        request.TransactionData.Should().BeEmpty();
    }

    [Fact]
    public void FairTransactionRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new FairTransactionRequest();
        var executeAfter = DateTime.UtcNow.AddMinutes(5);
        var executeBefore = DateTime.UtcNow.AddMinutes(30);

        // Act
        request.TransactionId = "fair-tx-456";
        request.From = "0xfrom123";
        request.To = "0xto456";
        request.Value = 100.5m;
        request.Data = "0xdata789";
        request.GasLimit = 21000;
        request.ProtectionLevel = "High";
        request.MaxSlippage = 0.05m;
        request.ExecuteAfter = executeAfter;
        request.ExecuteBefore = executeBefore;
        request.TransactionData = "additional data";

        // Assert
        request.TransactionId.Should().Be("fair-tx-456");
        request.From.Should().Be("0xfrom123");
        request.To.Should().Be("0xto456");
        request.Value.Should().Be(100.5m);
        request.Data.Should().Be("0xdata789");
        request.GasLimit.Should().Be(21000);
        request.ProtectionLevel.Should().Be("High");
        request.MaxSlippage.Should().Be(0.05m);
        request.ExecuteAfter.Should().Be(executeAfter);
        request.ExecuteBefore.Should().Be(executeBefore);
        request.TransactionData.Should().Be("additional data");
    }

    #endregion
}

using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Models;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using FluentAssertions;


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
        var request = new NeoServiceLayer.Core.Models.ProofRequest();

        // Assert
        request.CircuitId.Should().BeEmpty();
        request.PublicInputs.Should().NotBeNull().And.BeEmpty();
        request.PrivateInputs.Should().NotBeNull().And.BeEmpty();
        request.ProofType.Should().Be(ProofType.SNARK);
        request.Parameters.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ProofRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new NeoServiceLayer.Core.Models.ProofRequest();
        var publicInputs = new Dictionary<string, object> { ["public1"] = 123 };
        var privateInputs = new Dictionary<string, object> { ["private1"] = "secret" };
        var parameters = new Dictionary<string, object> { ["curve"] = "bn254" };

        // Act
        request.CircuitId = "circuit-misc-123";
        request.PublicInputs = publicInputs;
        request.PrivateInputs = privateInputs;
        request.ProofType = ProofType.STARK;
        request.Parameters = parameters;

        // Assert
        request.CircuitId.Should().Be("circuit-misc-123");
        request.PublicInputs.Should().BeEquivalentTo(publicInputs);
        request.PrivateInputs.Should().BeEquivalentTo(privateInputs);
        request.ProofType.Should().Be(ProofType.STARK);
        request.Parameters.Should().BeEquivalentTo(parameters);
    }

    #endregion

    #region ProofResult Tests

    [Fact]
    public void ProofResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new NeoServiceLayer.Core.Models.ProofResult();

        // Assert
        result.ProofId.Should().BeEmpty();
        result.ProofData.Should().BeEmpty();
        result.PublicOutputs.Should().NotBeNull().And.BeEmpty();
        result.Success.Should().BeFalse();
        result.GenerationTimeMs.Should().Be(0);
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.ErrorMessage.Should().BeNull();
        result.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ProofResult_Properties_ShouldBeSettable()
    {
        // Arrange
        var result = new NeoServiceLayer.Core.Models.ProofResult();
        var proofData = new byte[] { 1, 2, 3 };
        var publicOutputs = new Dictionary<string, object> { ["output1"] = "value1" };
        var metadata = new Dictionary<string, object> { ["version"] = "1.0" };
        var generatedAt = DateTime.UtcNow.AddMinutes(-5);

        // Act
        result.ProofId = "proof-misc-456";
        result.ProofData = proofData;
        result.PublicOutputs = publicOutputs;
        result.Success = true;
        result.GenerationTimeMs = 1500;
        result.GeneratedAt = generatedAt;
        result.ErrorMessage = null;
        result.Metadata = metadata;

        // Assert
        result.ProofId.Should().Be("proof-misc-456");
        result.ProofData.Should().BeEquivalentTo(proofData);
        result.PublicOutputs.Should().BeEquivalentTo(publicOutputs);
        result.Success.Should().BeTrue();
        result.GenerationTimeMs.Should().Be(1500);
        result.GeneratedAt.Should().Be(generatedAt);
        result.ErrorMessage.Should().BeNull();
        result.Metadata.Should().BeEquivalentTo(metadata);
    }

    #endregion

    #region ProofVerification Tests

    [Fact]
    public void ProofVerification_ShouldInitializeWithDefaults()
    {
        // Act
        var verification = new NeoServiceLayer.Core.Models.ProofVerification();

        // Assert
        verification.Proof.Should().BeEmpty();
        verification.PublicSignals.Should().NotBeNull().And.BeEmpty();
        verification.CircuitId.Should().BeEmpty();
    }

    [Fact]
    public void ProofVerification_Properties_ShouldBeSettable()
    {
        // Arrange
        var verification = new NeoServiceLayer.Core.Models.ProofVerification();
        var publicSignals = new[] { "verify1", "verify2", "verify3" };

        // Act
        verification.Proof = "0xverifyproof789";
        verification.PublicSignals = new Dictionary<string, object> { ["signal1"] = "value1" };
        verification.CircuitId = "circuit-verify-456";

        // Assert
        verification.Proof.Should().Be("0xverifyproof789");
        verification.PublicSignals.Should().BeEquivalentTo(new Dictionary<string, object> { ["signal1"] = "value1" });
        verification.CircuitId.Should().Be("circuit-verify-456");
    }

    #endregion


    #region ZkComputationResult Tests

    [Fact]
    public void ZkComputationResult_ShouldInitializeWithDefaults()
    {
        // Act
        var result = new NeoServiceLayer.Core.Models.ZkComputationResult();

        // Assert
        result.ComputationId.Should().BeEmpty();
        result.Success.Should().BeFalse();
        result.Result.Should().NotBeNull().And.BeEmpty();
        result.Proof.Should().BeEmpty();
        result.ExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ZkComputationResult_Properties_ShouldBeSettable()
    {
        // Arrange
        var result = new NeoServiceLayer.Core.Models.ZkComputationResult();
        var resultData = new Dictionary<string, object> { ["output"] = 42, ["status"] = "success" };
        var proof = new byte[] { 1, 2, 3, 4 };
        var executedAt = DateTime.UtcNow.AddMinutes(-2);

        // Act
        result.ComputationId = "comp-result-456";
        result.Success = true;
        result.Result = resultData;
        result.Proof = proof;
        result.ExecutedAt = executedAt;

        // Assert
        result.ComputationId.Should().Be("comp-result-456");
        result.Success.Should().BeTrue();
        result.Result.Should().BeEquivalentTo(resultData);
        result.Proof.Should().BeEquivalentTo(proof);
        result.ExecutedAt.Should().Be(executedAt);
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
        keyInfo.KeyType.Should().Be(default(CryptoKeyType));
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
        keyInfo.KeyType = CryptoKeyType.RSA;
        keyInfo.Algorithm = "RSA-OAEP";
        keyInfo.KeySize = 2048;
        keyInfo.Purpose = "encryption";
        keyInfo.CreatedAt = createdAt;
        keyInfo.ExpiresAt = expiresAt;
        keyInfo.IsActive = false;
        keyInfo.Metadata = metadata;

        // Assert
        keyInfo.KeyId.Should().Be("key-crypto-123");
        keyInfo.KeyType.Should().Be(CryptoKeyType.RSA);
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
        var request = new Core.Models.FairTransactionRequest();

        // Assert
        request.Transaction.Should().NotBeNull();
        request.From.Should().BeEmpty();
        request.To.Should().BeEmpty();
        request.Value.Should().Be(0);
        request.Data.Should().BeEmpty();
        request.FairnessLevel.Should().Be(FairnessLevel.Standard);
        request.MaxWaitTimeMs.Should().Be(30000);
        request.UseMevProtection.Should().BeTrue();
        request.Preferences.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void FairTransactionRequest_Properties_ShouldBeSettable()
    {
        // Arrange
        var request = new Core.Models.FairTransactionRequest();
        var preferences = new Dictionary<string, object> { ["priority"] = "high" };
        var transaction = new PendingTransaction 
        { 
            TransactionId = "fair-tx-456",
            Hash = "0xhash123" 
        };

        // Act
        request.Transaction = transaction;
        request.From = "0xfrom123";
        request.To = "0xto456";
        request.Value = 100.5m;
        request.Data = new byte[] { 0x01, 0x02, 0x03 };
        request.FairnessLevel = FairnessLevel.High;
        request.MaxWaitTimeMs = 60000;
        request.UseMevProtection = false;
        request.Preferences = preferences;

        // Assert
        request.Transaction.Should().Be(transaction);
        request.From.Should().Be("0xfrom123");
        request.To.Should().Be("0xto456");
        request.Value.Should().Be(100.5m);
        request.Data.Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
        request.FairnessLevel.Should().Be(FairnessLevel.High);
        request.MaxWaitTimeMs.Should().Be(60000);
        request.UseMevProtection.Should().BeFalse();
        request.Preferences.Should().BeEquivalentTo(preferences);
    }

    #endregion
}

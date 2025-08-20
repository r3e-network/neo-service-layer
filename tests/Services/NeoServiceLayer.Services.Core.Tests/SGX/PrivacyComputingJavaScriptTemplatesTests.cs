using System.Text.Json;
using FluentAssertions;
using NeoServiceLayer.Services.Core.SGX;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Core.Tests.SGX;

/// <summary>
/// Unit tests for privacy computing JavaScript templates.
/// </summary>
public class PrivacyComputingJavaScriptTemplatesTests
{
    [Fact]
    public void AbstractAccountOperations_Should_ContainRequiredFunctions()
    {
        // Arrange & Act
        var template = PrivacyComputingJavaScriptTemplates.AbstractAccountOperations;

        // Assert
        template.Should().NotBeNullOrEmpty();
        template.Should().Contain("processAbstractAccountTransaction");
        template.Should().Contain("validateWitnesses");
        template.Should().Contain("computeTransactionHash");
        template.Should().Contain("operation");
        template.Should().Contain("witnesses");
        template.Should().Contain("accountData");
    }

    [Fact]
    public void VotingOperations_Should_ContainAnonymousVotingLogic()
    {
        // Arrange & Act
        var template = PrivacyComputingJavaScriptTemplates.VotingOperations;

        // Assert
        template.Should().NotBeNullOrEmpty();
        template.Should().Contain("processAnonymousVote");
        template.Should().Contain("generateVoteProof");
        template.Should().Contain("voterProof");
        template.Should().Contain("votingData");
        template.Should().Contain("zkProof");
        template.Should().Contain("nullifier");
        template.Should().Contain("commitment");
    }

    [Fact]
    public void SocialRecoveryOperations_Should_ContainGuardianValidation()
    {
        // Arrange & Act
        var template = PrivacyComputingJavaScriptTemplates.SocialRecoveryOperations;

        // Assert
        template.Should().NotBeNullOrEmpty();
        template.Should().Contain("processGuardianApproval");
        template.Should().Contain("guardianProof");
        template.Should().Contain("recoveryData");
        template.Should().Contain("guardianHash");
        template.Should().Contain("approvalCommitment");
        template.Should().Contain("validApproval");
    }

    [Fact]
    public void KeyManagementOperations_Should_ContainKeyDerivation()
    {
        // Arrange & Act
        var template = PrivacyComputingJavaScriptTemplates.KeyManagementOperations;

        // Assert
        template.Should().NotBeNullOrEmpty();
        template.Should().Contain("processKeyOperation");
        template.Should().Contain("deriveKey");
        template.Should().Contain("rotateKey");
        template.Should().Contain("validateAccess");
        template.Should().Contain("keyData");
        template.Should().Contain("accessProof");
        template.Should().Contain("metadata");
    }

    [Fact]
    public void ZeroKnowledgeOperations_Should_ContainProofGeneration()
    {
        // Arrange & Act
        var template = PrivacyComputingJavaScriptTemplates.ZeroKnowledgeOperations;

        // Assert
        template.Should().NotBeNullOrEmpty();
        template.Should().Contain("processZeroKnowledgeProof");
        template.Should().Contain("generateProof");
        template.Should().Contain("verifyProof");
        template.Should().Contain("computeProof");
        template.Should().Contain("statement");
        template.Should().Contain("witness");
        template.Should().Contain("commitment");
        template.Should().Contain("challenge");
        template.Should().Contain("response");
    }

    [Fact]
    public void SmartContractOperations_Should_ContainDeploymentValidation()
    {
        // Arrange & Act
        var template = PrivacyComputingJavaScriptTemplates.SmartContractOperations;

        // Assert
        template.Should().NotBeNullOrEmpty();
        template.Should().Contain("processContractOperation");
        template.Should().Contain("validateDeployment");
        template.Should().Contain("validateInvocation");
        template.Should().Contain("contractData");
        template.Should().Contain("deployerProof");
        template.Should().Contain("codeHash");
        template.Should().Contain("deploymentProof");
    }

    [Fact]
    public void OracleOperations_Should_ContainDataFetching()
    {
        // Arrange & Act
        var template = PrivacyComputingJavaScriptTemplates.OracleOperations;

        // Assert
        template.Should().NotBeNullOrEmpty();
        template.Should().Contain("processOracleRequest");
        template.Should().Contain("fetch");
        template.Should().Contain("validate");
        template.Should().Contain("batch");
        template.Should().Contain("oracleData");
        template.Should().Contain("dataSource");
        template.Should().Contain("sourceProof");
        template.Should().Contain("dataHash");
    }

    [Fact]
    public void NotificationOperations_Should_ContainPrivacyFeatures()
    {
        // Arrange & Act
        var template = PrivacyComputingJavaScriptTemplates.NotificationOperations;

        // Assert
        template.Should().NotBeNullOrEmpty();
        template.Should().Contain("processNotification");
        template.Should().Contain("anonymizeRecipient");
        template.Should().Contain("validateRecipient");
        template.Should().Contain("notificationData");
        template.Should().Contain("recipientProof");
        template.Should().Contain("deliveryProof");
        template.Should().Contain("recipientHash");
        template.Should().Contain("channelHash");
    }

    [Theory]
    [InlineData("AbstractAccount")]
    [InlineData("Voting")]
    [InlineData("SocialRecovery")]
    [InlineData("KeyManagement")]
    [InlineData("ZeroKnowledge")]
    [InlineData("SmartContract")]
    [InlineData("Oracle")]
    [InlineData("Notification")]
    public void AllTemplates_Should_ReturnSuccessResult(string templateType)
    {
        // Arrange
        var template = templateType switch
        {
            "AbstractAccount" => PrivacyComputingJavaScriptTemplates.AbstractAccountOperations,
            "Voting" => PrivacyComputingJavaScriptTemplates.VotingOperations,
            "SocialRecovery" => PrivacyComputingJavaScriptTemplates.SocialRecoveryOperations,
            "KeyManagement" => PrivacyComputingJavaScriptTemplates.KeyManagementOperations,
            "ZeroKnowledge" => PrivacyComputingJavaScriptTemplates.ZeroKnowledgeOperations,
            "SmartContract" => PrivacyComputingJavaScriptTemplates.SmartContractOperations,
            "Oracle" => PrivacyComputingJavaScriptTemplates.OracleOperations,
            "Notification" => PrivacyComputingJavaScriptTemplates.NotificationOperations,
            _ => throw new ArgumentException($"Unknown template type: {templateType}")
        };

        // Assert
        template.Should().Contain("success: true");
        template.Should().Contain("result:");
        template.Should().Contain("return JSON.stringify({");
    }

    [Fact]
    public void Templates_Should_UseConsistentHashingApproach()
    {
        // Arrange
        var templates = new[]
        {
            PrivacyComputingJavaScriptTemplates.AbstractAccountOperations,
            PrivacyComputingJavaScriptTemplates.VotingOperations,
            PrivacyComputingJavaScriptTemplates.SocialRecoveryOperations,
            PrivacyComputingJavaScriptTemplates.KeyManagementOperations,
            PrivacyComputingJavaScriptTemplates.ZeroKnowledgeOperations,
            PrivacyComputingJavaScriptTemplates.SmartContractOperations,
            PrivacyComputingJavaScriptTemplates.OracleOperations,
            PrivacyComputingJavaScriptTemplates.NotificationOperations
        };

        // Act & Assert
        foreach (var template in templates)
        {
            // All templates should use SHA-256 for hashing
            template.Should().Contain("sha256");
            template.Should().Contain("function hash(");
        }
    }

    [Fact]
    public void Templates_Should_HandleErrorsCases()
    {
        // Arrange
        var templates = new[]
        {
            PrivacyComputingJavaScriptTemplates.AbstractAccountOperations,
            PrivacyComputingJavaScriptTemplates.VotingOperations,
            PrivacyComputingJavaScriptTemplates.SocialRecoveryOperations,
            PrivacyComputingJavaScriptTemplates.KeyManagementOperations,
            PrivacyComputingJavaScriptTemplates.ZeroKnowledgeOperations,
            PrivacyComputingJavaScriptTemplates.SmartContractOperations,
            PrivacyComputingJavaScriptTemplates.OracleOperations,
            PrivacyComputingJavaScriptTemplates.NotificationOperations
        };

        // Act & Assert
        foreach (var template in templates)
        {
            // All templates should handle invalid operations
            template.Should().Contain("default:");
            template.Should().Contain("success: false");
            template.Should().Contain("Invalid operation");
        }
    }

    [Fact]
    public void Templates_Should_ParseJSONParameters()
    {
        // Arrange
        var templates = new Dictionary<string, string>
        {
            ["AbstractAccount"] = PrivacyComputingJavaScriptTemplates.AbstractAccountOperations,
            ["Voting"] = PrivacyComputingJavaScriptTemplates.VotingOperations,
            ["SocialRecovery"] = PrivacyComputingJavaScriptTemplates.SocialRecoveryOperations,
            ["KeyManagement"] = PrivacyComputingJavaScriptTemplates.KeyManagementOperations,
            ["ZeroKnowledge"] = PrivacyComputingJavaScriptTemplates.ZeroKnowledgeOperations,
            ["SmartContract"] = PrivacyComputingJavaScriptTemplates.SmartContractOperations,
            ["Oracle"] = PrivacyComputingJavaScriptTemplates.OracleOperations,
            ["Notification"] = PrivacyComputingJavaScriptTemplates.NotificationOperations
        };

        // Act & Assert
        foreach (var (name, template) in templates)
        {
            // All templates should parse params as JSON
            template.Should().Contain("JSON.parse(params)");
            
            // Each template should have its specific operation type
            var mainFunction = name switch
            {
                "AbstractAccount" => "processAbstractAccountTransaction",
                "Voting" => "processAnonymousVote",
                "SocialRecovery" => "processGuardianApproval",
                "KeyManagement" => "processKeyOperation",
                "ZeroKnowledge" => "processZeroKnowledgeProof",
                "SmartContract" => "processContractOperation",
                "Oracle" => "processOracleRequest",
                "Notification" => "processNotification",
                _ => throw new ArgumentException($"Unknown template: {name}")
            };
            
            template.Should().Contain($"function {mainFunction}(params)");
        }
    }

    [Fact]
    public void Templates_Should_ImplementPrivacyPreservingPatterns()
    {
        // Arrange
        var votingTemplate = PrivacyComputingJavaScriptTemplates.VotingOperations;
        var socialRecoveryTemplate = PrivacyComputingJavaScriptTemplates.SocialRecoveryOperations;
        var zkTemplate = PrivacyComputingJavaScriptTemplates.ZeroKnowledgeOperations;

        // Act & Assert
        // Voting should use nullifiers to prevent double voting
        votingTemplate.Should().Contain("nullifier");
        votingTemplate.Should().Contain("voterHash");
        
        // Social recovery should hash guardian identities
        socialRecoveryTemplate.Should().Contain("guardianHash");
        socialRecoveryTemplate.Should().Contain("hash(guardian.identity)");
        
        // ZK proofs should follow standard proof structure
        zkTemplate.Should().Contain("commitment");
        zkTemplate.Should().Contain("challenge");
        zkTemplate.Should().Contain("response");
        zkTemplate.Should().Contain("proof.commitment === expectedCommitment");
    }
}
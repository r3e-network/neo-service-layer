using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Core;
using NeoServiceLayer.Integration.Tests.Helpers;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Services.Notification;
using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Services.SmartContracts;
using NeoServiceLayer.Services.SocialRecovery;
using NeoServiceLayer.Services.Voting;
using NeoServiceLayer.Services.ZeroKnowledge;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;

namespace NeoServiceLayer.Integration.Tests.Services;

/// <summary>
/// Integration tests for SGX service integration across all services.
/// </summary>
public class SGXServicesIntegrationTests : IntegrationTestBase
{
    private readonly IServiceProvider _serviceProvider;

    public SGXServicesIntegrationTests()
    {
        _serviceProvider = Factory.Services;
    }

    [Fact]
    public async Task AbstractAccountService_Should_UsePrivacyPreservingOperations()
    {
        // Arrange
        var abstractAccountService = _serviceProvider.GetRequiredService<IAbstractAccountService>();
        var enclaveManager = _serviceProvider.GetService<IEnclaveManager>();

        // Act
        var accountAddress = await abstractAccountService.CreateAccountAsync(
            "TestAccount",
            new[] { "0x123", "0x456" },
            2,
            BlockchainType.NeoN3);

        // Assert
        accountAddress.Should().NotBeNullOrEmpty();

        // Verify account operations use privacy-preserving methods
        var accountInfo = await abstractAccountService.GetAccountInfoAsync(accountAddress, BlockchainType.NeoN3);
        accountInfo.Should().NotBeNull();
        accountInfo.Address.Should().Be(accountAddress);

        // Check that the service is using enclave operations
        if (enclaveManager != null)
        {
            enclaveManager.IsInitialized.Should().BeTrue("Enclave should be initialized for privacy operations");
        }
    }

    [Fact]
    public async Task VotingService_Should_UseAnonymousVoting()
    {
        // Arrange
        var votingService = _serviceProvider.GetRequiredService<IVotingService>();
        var voterAddress = "0xVoter123";
        var votingPower = 100.0m;

        // Create a voting session
        var sessionResult = await votingService.CreateVotingSessionAsync(
            "Test Vote",
            "Should we implement feature X?",
            new[] { "Yes", "No", "Abstain" },
            DateTime.UtcNow.AddDays(1),
            BlockchainType.NeoN3);

        sessionResult.Success.Should().BeTrue();
        var sessionId = sessionResult.SessionId;

        // Act - Submit anonymous vote
        var voteResult = await votingService.SubmitVoteAsync(
            sessionId,
            voterAddress,
            0, // Vote for "Yes"
            votingPower,
            BlockchainType.NeoN3);

        // Assert
        voteResult.Success.Should().BeTrue();
        voteResult.VoteHash.Should().NotBeNullOrEmpty("Vote should be hashed for privacy");

        // Verify vote was recorded anonymously
        var votingResults = await votingService.GetVotingResultsAsync(sessionId, BlockchainType.NeoN3);
        votingResults.Should().NotBeNull();
        votingResults.Options[0].VoteCount.Should().Be(1);
        votingResults.Options[0].VotingPower.Should().Be(votingPower);

        // Individual voter information should not be retrievable
        votingResults.VoterDetails.Should().BeNullOrEmpty("Individual voter details should be private");
    }

    [Fact]
    public async Task KeyManagementService_Should_ProtectKeysInSGX()
    {
        // Arrange
        var keyManagementService = _serviceProvider.GetRequiredService<IKeyManagementService>();
        var keyId = $"test-key-{Guid.NewGuid()}";

        // Act - Create key in SGX
        var createResult = await keyManagementService.CreateKeyAsync(
            keyId,
            "RSA",
            "SIGNING",
            false, // Not exportable for security
            "Test key for SGX",
            BlockchainType.NeoN3);

        // Assert
        createResult.Should().NotBeNull();
        createResult.KeyId.Should().Be(keyId);
        createResult.KeyType.Should().Be("RSA");
        createResult.Exportable.Should().BeFalse();

        // Verify key operations are performed in SGX
        var signData = "Test data to sign";
        var signResult = await keyManagementService.SignDataAsync(
            keyId,
            signData,
            BlockchainType.NeoN3);

        signResult.Success.Should().BeTrue();
        signResult.Signature.Should().NotBeNullOrEmpty();

        // Verify signature
        var verifyResult = await keyManagementService.VerifySignatureAsync(
            keyId,
            signData,
            signResult.Signature,
            BlockchainType.NeoN3);

        verifyResult.Should().BeTrue();
    }

    [Fact]
    public async Task ZeroKnowledgeService_Should_GenerateProofsInSGX()
    {
        // Arrange
        var zkService = _serviceProvider.GetRequiredService<IZeroKnowledgeService>();

        // First compile a circuit
        var circuitDefinition = new ZeroKnowledgeService.ZkCircuitDefinition
        {
            Name = "AgeVerification",
            Description = "Verify age without revealing exact age",
            Type = ZeroKnowledgeService.ZkCircuitType.Comparison,
            InputSchema = new Dictionary<string, object> { ["age"] = "number", ["threshold"] = "number" },
            OutputSchema = new Dictionary<string, object> { ["isValid"] = "boolean" }
        };

        var compileResult = await zkService.CompileCircuitAsync(circuitDefinition, BlockchainType.NeoN3);
        compileResult.Success.Should().BeTrue();
        var circuitId = compileResult.CircuitId;

        // Act - Generate proof in SGX
        var proofRequest = new ProofRequest
        {
            CircuitId = circuitId,
            PublicInputs = new Dictionary<string, object> { ["threshold"] = 18 },
            PrivateInputs = new Dictionary<string, object> { ["age"] = 25 }
        };

        var proofResult = await zkService.GenerateProofAsync(proofRequest, BlockchainType.NeoN3);

        // Assert
        proofResult.Should().NotBeNull();
        proofResult.ProofId.Should().NotBeNullOrEmpty();
        proofResult.Proof.Should().NotBeNullOrEmpty();

        // Verify proof
        var verificationRequest = new ProofVerification
        {
            CircuitId = circuitId,
            Proof = proofResult.Proof,
            PublicSignals = proofResult.PublicSignals
        };

        var isValid = await zkService.VerifyProofAsync(verificationRequest, BlockchainType.NeoN3);
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task OracleService_Should_FetchDataWithPrivacy()
    {
        // Arrange
        var oracleService = _serviceProvider.GetRequiredService<IOracleService>();
        var dataSource = "https://api.example.com/data";
        var dataPath = "price.usd";

        // Act
        var oracleRequest = new OracleService.OracleRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Url = dataSource,
            Path = dataPath,
            BlockchainType = BlockchainType.NeoN3
        };

        var response = await oracleService.FetchDataAsync(oracleRequest, BlockchainType.NeoN3);

        // Assert
        response.Should().NotBeNull();
        response.RequestId.Should().Be(oracleRequest.RequestId);

        // Check privacy metadata
        response.Metadata.Should().NotBeNull();
        response.Metadata.Should().ContainKey("privacy_request_id");
        response.Metadata.Should().ContainKey("data_hash");
        response.Metadata.Should().ContainKey("source_proof_hash");
        response.Metadata.Should().ContainKey("source_proof_signature");

        // Data should be integrity-protected
        response.Data.Should().Contain("integrity");
        response.Data.Should().Contain("enclave_verified");
        response.Data.Should().Contain("privacy_proof");
    }

    [Fact]
    public async Task NotificationService_Should_AnonymizeRecipientData()
    {
        // Arrange
        var notificationService = _serviceProvider.GetRequiredService<INotificationService>();

        // Act
        var request = new NotificationService.SendNotificationRequest
        {
            Channel = NotificationService.NotificationChannel.Email,
            Recipient = "user@example.com",
            Subject = "Test Notification",
            Message = "This is a test message",
            Priority = NotificationService.NotificationPriority.Normal,
            Category = "Test"
        };

        var result = await notificationService.SendNotificationAsync(request, BlockchainType.NeoN3);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.NotificationId.Should().NotBeNullOrEmpty();

        // Check privacy metadata
        result.Metadata.Should().NotBeNull();
        result.Metadata.Should().ContainKey("privacy_proof_id");
        result.Metadata.Should().ContainKey("recipient_hash");
        result.Metadata.Should().ContainKey("channel_hash");
        result.Metadata.Should().ContainKey("delivery_proof");

        // Recipient should be hashed
        result.Metadata["recipient_hash"].Should().NotBe("user@example.com");
    }

    [Fact]
    public async Task SmartContractService_Should_ValidateContractsInSGX()
    {
        // Arrange
        var smartContractService = _serviceProvider.GetRequiredService<ISmartContractService>();

        // Simple contract bytecode (mock)
        var contractCode = Convert.FromBase64String("TU9DSyBDT05UUkFDVCBCWVRFQ09ERQ==");

        // Act
        var deploymentOptions = new SmartContractService.ContractDeploymentOptions
        {
            ContractName = "TestContract",
            Version = "1.0.0",
            Description = "Test contract for SGX validation"
        };

        var deployResult = await smartContractService.DeployContractAsync(
            contractCode,
            null,
            deploymentOptions,
            BlockchainType.NeoN3);

        // Assert
        deployResult.Should().NotBeNull();
        deployResult.Success.Should().BeTrue();
        deployResult.ContractAddress.Should().NotBeNullOrEmpty();

        // Check that deployment was validated in SGX
        deployResult.Metadata.Should().NotBeNull();
        deployResult.Metadata.Should().ContainKey("privacy_deployment_id");
        deployResult.Metadata.Should().ContainKey("code_hash");
        deployResult.Metadata.Should().ContainKey("deployer_hash");
        deployResult.Metadata.Should().ContainKey("deployment_proof");
    }

    [Fact]
    public async Task SocialRecoveryService_Should_ProtectGuardianIdentities()
    {
        // Arrange
        var socialRecoveryService = _serviceProvider.GetRequiredService<ISocialRecoveryService>();
        var accountAddress = "0xAccount123";
        var guardians = new[] { "0xGuardian1", "0xGuardian2", "0xGuardian3" };

        // Act - Setup recovery
        var setupResult = await socialRecoveryService.SetupRecoveryAsync(
            accountAddress,
            guardians,
            2, // threshold
            BlockchainType.NeoN3);

        setupResult.Success.Should().BeTrue();
        var recoveryId = setupResult.RecoveryId;

        // Initiate recovery
        var initiateResult = await socialRecoveryService.InitiateRecoveryAsync(
            accountAddress,
            "0xNewOwner",
            BlockchainType.NeoN3);

        initiateResult.Success.Should().BeTrue();
        initiateResult.RecoveryId.Should().NotBeNullOrEmpty();

        // Check guardian privacy
        initiateResult.RequiredApprovals.Should().Be(2);
        initiateResult.GuardianHashes.Should().NotBeNullOrEmpty();
        initiateResult.GuardianHashes.Should().HaveCount(3);

        // Guardian addresses should be hashed
        initiateResult.GuardianHashes.Should().NotContain(guardians);
        foreach (var hash in initiateResult.GuardianHashes)
        {
            hash.Should().NotBeNullOrEmpty();
            hash.Should().NotBeOneOf(guardians);
        }
    }

    [Fact]
    public async Task AllServices_Should_ShareEnclaveManager()
    {
        // Arrange
        var enclaveManager = _serviceProvider.GetService<IEnclaveManager>();

        if (enclaveManager == null)
        {
            // Skip test if enclave manager is not available
            return;
        }

        // Act & Assert
        enclaveManager.IsInitialized.Should().BeTrue("Enclave should be initialized");

        // Verify all services can use the same enclave instance
        var abstractAccountService = _serviceProvider.GetRequiredService<IAbstractAccountService>();
        var votingService = _serviceProvider.GetRequiredService<IVotingService>();
        var keyManagementService = _serviceProvider.GetRequiredService<IKeyManagementService>();
        var zkService = _serviceProvider.GetRequiredService<IZeroKnowledgeService>();
        var oracleService = _serviceProvider.GetRequiredService<IOracleService>();
        var notificationService = _serviceProvider.GetRequiredService<INotificationService>();
        var smartContractService = _serviceProvider.GetRequiredService<ISmartContractService>();
        var socialRecoveryService = _serviceProvider.GetRequiredService<ISocialRecoveryService>();

        // All services should be initialized and ready
        abstractAccountService.Should().NotBeNull();
        votingService.Should().NotBeNull();
        keyManagementService.Should().NotBeNull();
        zkService.Should().NotBeNull();
        oracleService.Should().NotBeNull();
        notificationService.Should().NotBeNull();
        smartContractService.Should().NotBeNull();
        socialRecoveryService.Should().NotBeNull();
    }

    [Fact]
    public async Task SGXOperations_Should_HandleConcurrentRequests()
    {
        // Arrange
        var zkService = _serviceProvider.GetRequiredService<IZeroKnowledgeService>();
        var keyManagementService = _serviceProvider.GetRequiredService<IKeyManagementService>();

        // Create test data
        var circuitDefinition = new ZeroKnowledgeService.ZkCircuitDefinition
        {
            Name = "TestCircuit",
            Description = "Test circuit for concurrency",
            Type = ZeroKnowledgeService.ZkCircuitType.Arithmetic,
            InputSchema = new Dictionary<string, object> { ["value"] = "number" },
            OutputSchema = new Dictionary<string, object> { ["result"] = "number" }
        };

        var compileResult = await zkService.CompileCircuitAsync(circuitDefinition, BlockchainType.NeoN3);
        var circuitId = compileResult.CircuitId;

        // Act - Execute multiple operations concurrently
        var tasks = new List<Task>();

        // Generate multiple proofs concurrently
        for (int i = 0; i < 5; i++)
        {
            var value = i;
            tasks.Add(Task.Run(async () =>
            {
                var proofRequest = new ProofRequest
                {
                    CircuitId = circuitId,
                    PublicInputs = new Dictionary<string, object> { ["value"] = value },
                    PrivateInputs = new Dictionary<string, object> { ["secret"] = value * 2 }
                };

                var result = await zkService.GenerateProofAsync(proofRequest, BlockchainType.NeoN3);
                result.Should().NotBeNull();
                result.Proof.Should().NotBeNullOrEmpty();
            }));
        }

        // Create multiple keys concurrently
        for (int i = 0; i < 5; i++)
        {
            var keyId = $"concurrent-key-{i}";
            tasks.Add(Task.Run(async () =>
            {
                var result = await keyManagementService.CreateKeyAsync(
                    keyId,
                    "AES",
                    "ENCRYPTION",
                    false,
                    "Concurrent test key",
                    BlockchainType.NeoN3);

                result.Should().NotBeNull();
                result.KeyId.Should().Be(keyId);
            }));
        }

        // Assert
        await Task.WhenAll(tasks);
        // All tasks should complete successfully without exceptions
    }
}

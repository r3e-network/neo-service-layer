using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.ProofOfReserve;
using NeoServiceLayer.Services.Compliance;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Services.ZeroKnowledge;
using NeoServiceLayer.Services.Voting;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Host.Tests;
using NeoServiceLayer.Tee.Enclave;
using System.Text;
using System.Text.Json;
using Xunit;

namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Integration tests for security and compliance scenarios.
/// </summary>
public class SecurityComplianceIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ILogger<SecurityComplianceIntegrationTests> _logger;
    private readonly IProofOfReserveService _proofOfReserveService;
    private readonly IComplianceService _complianceService;
    private readonly IKeyManagementService _keyManagementService;
    private readonly IZeroKnowledgeService _zeroKnowledgeService;
    private readonly IVotingService _votingService;
    private readonly IEnclaveManager _enclaveManager;
    private readonly AttestationService _attestationService;

    public SecurityComplianceIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Add enclave services
        services.AddSingleton<IEnclaveWrapper, TestEnclaveWrapper>();
        services.AddSingleton<IEnclaveManager, EnclaveManager>();
        
        // Add attestation service
        services.AddSingleton<AttestationService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<AttestationService>>();
            var httpClient = new HttpClient();
            return new AttestationService(logger, httpClient, "https://test-attestation.intel.com", "test-api-key");
        });
        
        // Add all services
        services.AddSingleton<IProofOfReserveService, ProofOfReserveService>();
        services.AddSingleton<IComplianceService, ComplianceService>();
        services.AddSingleton<IKeyManagementService, KeyManagementService>();
        services.AddSingleton<IZeroKnowledgeService, ZeroKnowledgeService>();
        services.AddSingleton<IVotingService, VotingService>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get service instances
        _logger = _serviceProvider.GetRequiredService<ILogger<SecurityComplianceIntegrationTests>>();
        _proofOfReserveService = _serviceProvider.GetRequiredService<IProofOfReserveService>();
        _complianceService = _serviceProvider.GetRequiredService<IComplianceService>();
        _keyManagementService = _serviceProvider.GetRequiredService<IKeyManagementService>();
        _zeroKnowledgeService = _serviceProvider.GetRequiredService<IZeroKnowledgeService>();
        _votingService = _serviceProvider.GetRequiredService<IVotingService>();
        _enclaveManager = _serviceProvider.GetRequiredService<IEnclaveManager>();
        _attestationService = _serviceProvider.GetRequiredService<AttestationService>();
        
        // Initialize all services
        InitializeServicesAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeServicesAsync()
    {
        _logger.LogInformation("Initializing all services for security and compliance testing...");
        
        await _enclaveManager.InitializeEnclaveAsync();
        await _proofOfReserveService.InitializeAsync();
        await _complianceService.InitializeAsync();
        await _keyManagementService.InitializeAsync();
        await _zeroKnowledgeService.InitializeAsync();
        await _votingService.InitializeAsync();
        
        _logger.LogInformation("All services initialized successfully");
    }

    [Fact]
    public async Task ComplianceAudit_WithZeroKnowledgeProofs_ShouldMaintainPrivacy()
    {
        _logger.LogInformation("Starting compliance audit with zero-knowledge proofs test...");

        // 1. Create compliance rules
        var complianceRule = new ComplianceRule
        {
            RuleId = "aml-kyc-rule",
            Name = "AML/KYC Compliance",
            Description = "Verify AML/KYC compliance without revealing personal data",
            RuleType = ComplianceRuleType.KYC,
            RequiredProofs = new[] { "age_over_18", "not_on_sanctions_list", "verified_identity" },
            IsActive = true,
            EnforcementLevel = EnforcementLevel.Strict
        };

        var ruleResult = await _complianceService.CreateRuleAsync(complianceRule, BlockchainType.NeoX);
        ruleResult.Success.Should().BeTrue();
        _logger.LogInformation("Created compliance rule {RuleId}", complianceRule.RuleId);

        // 2. Generate ZK proof for age verification
        var ageProofRequest = new ZKProofRequest
        {
            ProofType = "age_verification",
            PrivateInput = JsonSerializer.Serialize(new { birthdate = "1990-01-01", currentDate = DateTime.UtcNow }),
            PublicInput = JsonSerializer.Serialize(new { minimumAge = 18 }),
            CircuitId = "age_verifier_circuit"
        };

        var ageProofResult = await _zeroKnowledgeService.GenerateProofAsync(ageProofRequest, BlockchainType.NeoX);
        ageProofResult.Success.Should().BeTrue();
        ageProofResult.Proof.Should().NotBeNullOrEmpty();
        _logger.LogInformation("Generated ZK proof for age verification");

        // 3. Generate ZK proof for sanctions list check
        var sanctionsProofRequest = new ZKProofRequest
        {
            ProofType = "sanctions_check",
            PrivateInput = JsonSerializer.Serialize(new { userId = "user123", name = "John Doe" }),
            PublicInput = JsonSerializer.Serialize(new { listVersion = "2025-01", result = "not_found" }),
            CircuitId = "sanctions_checker_circuit"
        };

        var sanctionsProofResult = await _zeroKnowledgeService.GenerateProofAsync(sanctionsProofRequest, BlockchainType.NeoX);
        sanctionsProofResult.Success.Should().BeTrue();
        _logger.LogInformation("Generated ZK proof for sanctions list check");

        // 4. Generate secure key for identity verification
        var keyRequest = new KeyGenerationRequest
        {
            KeyType = KeyType.ECDSA,
            KeyUsage = KeyUsage.Signing,
            KeyLength = 256,
            Exportable = false,
            Description = "Identity verification key",
            Metadata = new Dictionary<string, object> { { "purpose", "kyc_verification" } }
        };

        var keyResult = await _keyManagementService.GenerateKeyAsync(keyRequest, BlockchainType.NeoX);
        keyResult.Success.Should().BeTrue();
        var keyId = keyResult.KeyId;
        _logger.LogInformation("Generated secure key {KeyId} for identity verification", keyId);

        // 5. Sign identity attestation
        var identityData = JsonSerializer.SerializeToUtf8Bytes(new
        {
            userId = "user123",
            verificationLevel = "enhanced",
            timestamp = DateTime.UtcNow,
            proofs = new[] { ageProofResult.ProofId, sanctionsProofResult.ProofId }
        });

        var signRequest = new SigningRequest
        {
            KeyId = keyId,
            Data = identityData,
            Algorithm = SigningAlgorithm.ECDSA_SHA256
        };

        var signResult = await _keyManagementService.SignAsync(signRequest, BlockchainType.NeoX);
        signResult.Success.Should().BeTrue();
        _logger.LogInformation("Signed identity attestation");

        // 6. Create compliance check request
        var complianceCheckRequest = new ComplianceCheckRequest
        {
            UserId = "user123",
            RuleIds = new[] { complianceRule.RuleId },
            Proofs = new Dictionary<string, string>
            {
                { "age_over_18", ageProofResult.Proof },
                { "not_on_sanctions_list", sanctionsProofResult.Proof },
                { "verified_identity", Convert.ToBase64String(signResult.Signature) }
            },
            RequestContext = new Dictionary<string, object>
            {
                { "transaction_type", "high_value_transfer" },
                { "amount", 50000 }
            }
        };

        var complianceResult = await _complianceService.CheckComplianceAsync(complianceCheckRequest, BlockchainType.NeoX);
        complianceResult.Success.Should().BeTrue();
        complianceResult.IsCompliant.Should().BeTrue();
        complianceResult.ComplianceScore.Should().BeGreaterThan(0.9);
        _logger.LogInformation("Compliance check passed with score {Score}", complianceResult.ComplianceScore);

        // 7. Verify ZK proofs independently
        var ageVerification = await _zeroKnowledgeService.VerifyProofAsync(
            ageProofResult.Proof, 
            ageProofRequest.PublicInput, 
            "age_verifier_circuit", 
            BlockchainType.NeoX);
        ageVerification.IsValid.Should().BeTrue();

        var sanctionsVerification = await _zeroKnowledgeService.VerifyProofAsync(
            sanctionsProofResult.Proof,
            sanctionsProofRequest.PublicInput,
            "sanctions_checker_circuit",
            BlockchainType.NeoX);
        sanctionsVerification.IsValid.Should().BeTrue();
        _logger.LogInformation("All ZK proofs verified successfully");

        // 8. Store compliance audit trail
        var auditRecord = new ComplianceAuditRecord
        {
            RecordId = Guid.NewGuid().ToString(),
            UserId = "user123",
            RuleId = complianceRule.RuleId,
            CheckResult = complianceResult,
            ProofHashes = new Dictionary<string, string>
            {
                { "age_proof", HashData(ageProofResult.Proof) },
                { "sanctions_proof", HashData(sanctionsProofResult.Proof) },
                { "identity_signature", HashData(Convert.ToBase64String(signResult.Signature)) }
            },
            Timestamp = DateTime.UtcNow,
            Auditor = "system"
        };

        var auditResult = await _complianceService.RecordAuditAsync(auditRecord, BlockchainType.NeoX);
        auditResult.Success.Should().BeTrue();
        _logger.LogInformation("Recorded compliance audit {RecordId}", auditRecord.RecordId);

        // Complete test verification
        ruleResult.RuleId.Should().NotBeNullOrEmpty();
        ageProofResult.ProofId.Should().NotBeNullOrEmpty();
        sanctionsProofResult.ProofId.Should().NotBeNullOrEmpty();
        keyResult.KeyId.Should().NotBeNullOrEmpty();
        signResult.Signature.Should().NotBeEmpty();
        complianceResult.CheckId.Should().NotBeNullOrEmpty();
        auditResult.AuditId.Should().NotBeNullOrEmpty();

        _logger.LogInformation("Compliance audit with zero-knowledge proofs completed successfully!");
    }

    [Fact]
    public async Task SecureVoting_WithEnclaveAttestation_ShouldEnsureIntegrity()
    {
        _logger.LogInformation("Starting secure voting with enclave attestation test...");

        // 1. Generate enclave attestation
        var attestationData = Encoding.UTF8.GetBytes("voting-enclave-v1.0");
        var attestationReport = await _attestationService.GenerateAttestationReportAsync(attestationData);
        attestationReport.Should().NotBeNull();
        attestationReport.IsvEnclaveQuoteStatus.Should().Be("OK");
        _logger.LogInformation("Generated enclave attestation report {ReportId}", attestationReport.Id);

        // 2. Verify attestation
        var verificationResult = await _attestationService.VerifyAttestationAsync(JsonSerializer.Serialize(attestationReport));
        verificationResult.Should().NotBeNull();
        verificationResult.IsValid.Should().BeTrue();
        verificationResult.TcbStatus.Should().Be(TcbStatus.UpToDate);
        _logger.LogInformation("Attestation verified successfully");

        // 3. Create voting proposal within attested enclave
        var proposalRequest = new CreateProposalRequest
        {
            Title = "Protocol Upgrade v2.0",
            Description = "Upgrade the protocol to version 2.0 with enhanced security features",
            ProposalType = ProposalType.ProtocolUpgrade,
            Options = new[]
            {
                new VotingOption { Id = "yes", Name = "Approve Upgrade", Description = "Vote to approve the protocol upgrade" },
                new VotingOption { Id = "no", Name = "Reject Upgrade", Description = "Vote to reject the protocol upgrade" },
                new VotingOption { Id = "abstain", Name = "Abstain", Description = "Abstain from voting" }
            },
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddDays(7),
            QuorumPercentage = 33.33m,
            PassThreshold = 66.67m,
            EnclaveAttestation = verificationResult
        };

        var proposalResult = await _votingService.CreateProposalAsync(proposalRequest, BlockchainType.NeoX);
        proposalResult.Success.Should().BeTrue();
        var proposalId = proposalResult.ProposalId;
        _logger.LogInformation("Created voting proposal {ProposalId} with enclave attestation", proposalId);

        // 4. Generate voter keys within enclave
        var voterKeys = new List<(string voterId, string keyId)>();
        for (int i = 0; i < 5; i++)
        {
            var voterKeyRequest = new KeyGenerationRequest
            {
                KeyType = KeyType.ECDSA,
                KeyUsage = KeyUsage.Signing,
                KeyLength = 256,
                Exportable = false,
                Description = $"Voter {i} signing key",
                Metadata = new Dictionary<string, object> 
                { 
                    { "voter_id", $"voter_{i}" },
                    { "proposal_id", proposalId }
                }
            };

            var voterKeyResult = await _keyManagementService.GenerateKeyAsync(voterKeyRequest, BlockchainType.NeoX);
            voterKeyResult.Success.Should().BeTrue();
            voterKeys.Add(($"voter_{i}", voterKeyResult.KeyId));
        }
        _logger.LogInformation("Generated {Count} voter keys within enclave", voterKeys.Count);

        // 5. Cast votes with zero-knowledge proof of eligibility
        var votes = new List<string>();
        foreach (var (voterId, keyId) in voterKeys)
        {
            // Generate ZK proof of voting eligibility
            var eligibilityProof = new ZKProofRequest
            {
                ProofType = "voting_eligibility",
                PrivateInput = JsonSerializer.Serialize(new { voterId, balance = 1000, stakingDuration = 180 }),
                PublicInput = JsonSerializer.Serialize(new { minimumBalance = 100, minimumStakingDays = 30 }),
                CircuitId = "voting_eligibility_circuit"
            };

            var eligibilityResult = await _zeroKnowledgeService.GenerateProofAsync(eligibilityProof, BlockchainType.NeoX);
            eligibilityResult.Success.Should().BeTrue();

            // Cast vote
            var voteRequest = new CastVoteRequest
            {
                ProposalId = proposalId,
                VoterId = voterId,
                OptionId = i % 3 == 0 ? "yes" : i % 3 == 1 ? "no" : "abstain",
                VotingPower = 1000,
                EligibilityProof = eligibilityResult.Proof,
                SigningKeyId = keyId,
                PrivacyLevel = VotePrivacyLevel.Anonymous
            };

            var voteResult = await _votingService.CastVoteAsync(voteRequest, BlockchainType.NeoX);
            voteResult.Success.Should().BeTrue();
            votes.Add(voteResult.VoteId);
            _logger.LogInformation("Voter {VoterId} cast vote {VoteId}", voterId, voteResult.VoteId);
        }

        // 6. Tally votes within enclave
        var tallyRequest = new TallyVotesRequest
        {
            ProposalId = proposalId,
            IncludeDetails = true,
            VerifyAllProofs = true,
            EnclaveOnly = true
        };

        var tallyResult = await _votingService.TallyVotesAsync(tallyRequest, BlockchainType.NeoX);
        tallyResult.Success.Should().BeTrue();
        tallyResult.TotalVotes.Should().Be(votes.Count);
        tallyResult.QuorumReached.Should().BeTrue();
        _logger.LogInformation("Vote tally completed: Yes={Yes}, No={No}, Abstain={Abstain}", 
            tallyResult.Results["yes"], tallyResult.Results["no"], tallyResult.Results["abstain"]);

        // 7. Generate proof of correct tally
        var tallyProofRequest = new ZKProofRequest
        {
            ProofType = "tally_correctness",
            PrivateInput = JsonSerializer.Serialize(new { votes = votes, tallies = tallyResult.Results }),
            PublicInput = JsonSerializer.Serialize(new { totalVotes = tallyResult.TotalVotes, proposalId }),
            CircuitId = "tally_verifier_circuit"
        };

        var tallyProofResult = await _zeroKnowledgeService.GenerateProofAsync(tallyProofRequest, BlockchainType.NeoX);
        tallyProofResult.Success.Should().BeTrue();
        _logger.LogInformation("Generated proof of correct vote tally");

        // 8. Finalize proposal with attestation
        var finalizeRequest = new FinalizeProposalRequest
        {
            ProposalId = proposalId,
            TallyProof = tallyProofResult.Proof,
            EnclaveAttestation = verificationResult,
            ExecutorAddress = "0xprotocol_executor"
        };

        var finalizeResult = await _votingService.FinalizeProposalAsync(finalizeRequest, BlockchainType.NeoX);
        finalizeResult.Success.Should().BeTrue();
        finalizeResult.ProposalStatus.Should().Be(ProposalStatus.Passed);
        _logger.LogInformation("Proposal {ProposalId} finalized with status {Status}", 
            proposalId, finalizeResult.ProposalStatus);

        // 9. Verify audit trail
        var auditTrail = await _votingService.GetProposalAuditTrailAsync(proposalId, BlockchainType.NeoX);
        auditTrail.Should().NotBeNull();
        auditTrail.Events.Should().Contain(e => e.EventType == "ProposalCreated");
        auditTrail.Events.Should().Contain(e => e.EventType == "VotesCast");
        auditTrail.Events.Should().Contain(e => e.EventType == "TallyCompleted");
        auditTrail.Events.Should().Contain(e => e.EventType == "ProposalFinalized");
        _logger.LogInformation("Audit trail verified with {Count} events", auditTrail.Events.Count);

        // Complete test verification
        attestationReport.Id.Should().NotBeNullOrEmpty();
        verificationResult.IsValid.Should().BeTrue();
        proposalResult.ProposalId.Should().NotBeNullOrEmpty();
        votes.Should().HaveCount(5);
        tallyResult.QuorumReached.Should().BeTrue();
        tallyProofResult.ProofId.Should().NotBeNullOrEmpty();
        finalizeResult.ProposalStatus.Should().Be(ProposalStatus.Passed);

        _logger.LogInformation("Secure voting with enclave attestation completed successfully!");
    }

    [Fact]
    public async Task ProofOfReserve_WithComplianceCheck_ShouldMeetRegulatoryRequirements()
    {
        _logger.LogInformation("Starting proof of reserve with compliance check test...");

        // 1. Create regulatory compliance rule for reserves
        var reserveComplianceRule = new ComplianceRule
        {
            RuleId = "reserve-audit-rule",
            Name = "Reserve Audit Compliance",
            Description = "Ensure reserves meet regulatory requirements",
            RuleType = ComplianceRuleType.Financial,
            RequiredProofs = new[] { "reserve_attestation", "auditor_verification", "blockchain_proof" },
            Thresholds = new Dictionary<string, decimal>
            {
                { "minimum_reserve_ratio", 1.0m },
                { "audit_frequency_days", 30m },
                { "transparency_score", 0.95m }
            },
            IsActive = true,
            EnforcementLevel = EnforcementLevel.Strict
        };

        var ruleResult = await _complianceService.CreateRuleAsync(reserveComplianceRule, BlockchainType.NeoX);
        ruleResult.Success.Should().BeTrue();
        _logger.LogInformation("Created reserve compliance rule");

        // 2. Register stablecoin for proof of reserve
        var assetRequest = new AssetRegistrationRequest
        {
            AssetSymbol = "CUSD",
            AssetName = "Compliant USD",
            ReserveAddresses = new[] 
            { 
                "0xreserve_bank_1", 
                "0xreserve_bank_2", 
                "0xreserve_custodian" 
            },
            MinReserveRatio = 1.05m, // 105% overcollateralized
            TotalSupply = 50000000m,
            UpdateFrequency = TimeSpan.FromHours(6),
            ComplianceRuleIds = new[] { reserveComplianceRule.RuleId }
        };

        var assetResult = await _proofOfReserveService.RegisterAssetAsync(assetRequest, BlockchainType.NeoX);
        assetResult.Success.Should().BeTrue();
        var assetId = assetResult.AssetId;
        _logger.LogInformation("Registered asset {AssetId} for compliant proof of reserve", assetId);

        // 3. Create auditor keys within secure enclave
        var auditorKeyRequest = new KeyGenerationRequest
        {
            KeyType = KeyType.RSA,
            KeyUsage = KeyUsage.Signing,
            KeyLength = 4096,
            Exportable = false,
            Description = "Reserve auditor signing key",
            Metadata = new Dictionary<string, object> 
            { 
                { "auditor", "certified_auditor_firm" },
                { "license", "AUD-2025-001" }
            }
        };

        var auditorKeyResult = await _keyManagementService.GenerateKeyAsync(auditorKeyRequest, BlockchainType.NeoX);
        auditorKeyResult.Success.Should().BeTrue();
        var auditorKeyId = auditorKeyResult.KeyId;
        _logger.LogInformation("Generated auditor key {KeyId} within secure enclave", auditorKeyId);

        // 4. Generate enclave attestation for reserve data
        var reserveData = new
        {
            assetId,
            totalSupply = 50000000m,
            totalReserves = 52500000m,
            reserveRatio = 1.05m,
            auditTimestamp = DateTime.UtcNow,
            auditor = "certified_auditor_firm"
        };

        var attestationData = JsonSerializer.SerializeToUtf8Bytes(reserveData);
        var attestationReport = await _attestationService.GenerateAttestationReportAsync(attestationData);
        attestationReport.Should().NotBeNull();
        _logger.LogInformation("Generated attestation for reserve data");

        // 5. Sign reserve attestation with auditor key
        var attestationSignRequest = new SigningRequest
        {
            KeyId = auditorKeyId,
            Data = attestationData,
            Algorithm = SigningAlgorithm.RSA_SHA256
        };

        var attestationSignResult = await _keyManagementService.SignAsync(attestationSignRequest, BlockchainType.NeoX);
        attestationSignResult.Success.Should().BeTrue();
        _logger.LogInformation("Auditor signed reserve attestation");

        // 6. Generate zero-knowledge proof of reserves
        var reserveProofRequest = new ZKProofRequest
        {
            ProofType = "reserve_proof",
            PrivateInput = JsonSerializer.Serialize(new 
            { 
                bankStatements = new[] { "stmt_001", "stmt_002", "stmt_003" },
                accountBalances = new[] { 20000000m, 22500000m, 10000000m }
            }),
            PublicInput = JsonSerializer.Serialize(new 
            { 
                totalReserves = 52500000m,
                minimumRequired = 50000000m
            }),
            CircuitId = "reserve_verifier_circuit"
        };

        var reserveProofResult = await _zeroKnowledgeService.GenerateProofAsync(reserveProofRequest, BlockchainType.NeoX);
        reserveProofResult.Success.Should().BeTrue();
        _logger.LogInformation("Generated zero-knowledge proof of reserves");

        // 7. Submit proof of reserve with compliance data
        var proofRequest = new ProofGenerationRequest
        {
            AssetId = assetId,
            ProofType = ProofType.MerkleProof,
            IncludeTransactionHistory = true,
            IncludeSignatures = true,
            AdditionalData = new Dictionary<string, object>
            {
                { "auditor_signature", Convert.ToBase64String(attestationSignResult.Signature) },
                { "enclave_attestation", attestationReport },
                { "zk_reserve_proof", reserveProofResult.Proof },
                { "compliance_rule_id", reserveComplianceRule.RuleId }
            }
        };

        var proofResult = await _proofOfReserveService.GenerateProofAsync(proofRequest, BlockchainType.NeoX);
        proofResult.Success.Should().BeTrue();
        _logger.LogInformation("Generated comprehensive proof of reserve");

        // 8. Perform compliance check on the proof
        var complianceCheckRequest = new ComplianceCheckRequest
        {
            EntityId = assetId,
            RuleIds = new[] { reserveComplianceRule.RuleId },
            Proofs = new Dictionary<string, string>
            {
                { "reserve_attestation", Convert.ToBase64String(attestationSignResult.Signature) },
                { "auditor_verification", auditorKeyResult.PublicKey },
                { "blockchain_proof", proofResult.ProofHash }
            },
            RequestContext = new Dictionary<string, object>
            {
                { "asset_type", "stablecoin" },
                { "total_supply", 50000000m },
                { "reserve_ratio", 1.05m }
            }
        };

        var complianceResult = await _complianceService.CheckComplianceAsync(complianceCheckRequest, BlockchainType.NeoX);
        complianceResult.Success.Should().BeTrue();
        complianceResult.IsCompliant.Should().BeTrue();
        complianceResult.ComplianceScore.Should().BeGreaterThan(0.95);
        _logger.LogInformation("Compliance check passed with score {Score}", complianceResult.ComplianceScore);

        // 9. Create compliance report
        var reportRequest = new ComplianceReportRequest
        {
            EntityId = assetId,
            ReportType = ComplianceReportType.ProofOfReserve,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow,
            IncludeDetails = true,
            IncludeRecommendations = true
        };

        var reportResult = await _complianceService.GenerateReportAsync(reportRequest, BlockchainType.NeoX);
        reportResult.Success.Should().BeTrue();
        reportResult.Report.Should().NotBeNull();
        reportResult.Report.ComplianceStatus.Should().Be("Compliant");
        _logger.LogInformation("Generated compliance report {ReportId}", reportResult.ReportId);

        // Complete test verification
        ruleResult.RuleId.Should().NotBeNullOrEmpty();
        assetResult.AssetId.Should().NotBeNullOrEmpty();
        auditorKeyResult.KeyId.Should().NotBeNullOrEmpty();
        attestationReport.Id.Should().NotBeNullOrEmpty();
        attestationSignResult.Signature.Should().NotBeEmpty();
        reserveProofResult.ProofId.Should().NotBeNullOrEmpty();
        proofResult.ProofHash.Should().NotBeNullOrEmpty();
        complianceResult.IsCompliant.Should().BeTrue();
        reportResult.ReportId.Should().NotBeNullOrEmpty();

        _logger.LogInformation("Proof of reserve with compliance check completed successfully!");
    }

    private string HashData(string data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
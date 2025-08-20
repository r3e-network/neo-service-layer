using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Configuration;
using NeoServiceLayer.ServiceFramework;
using CoreConfig = NeoServiceLayer.Core.Configuration.IServiceConfiguration;
using NeoServiceLayer.Services.ZeroKnowledge.Models;
using ProofVerification = NeoServiceLayer.Core.Models.ProofVerification;
using NeoServiceLayer.Tee.Host.Services;
using System.Collections.Generic;
using System.Linq;
using ProofRequest = NeoServiceLayer.Services.ZeroKnowledge.Models.GenerateProofRequest;
using ServiceProofResult = NeoServiceLayer.Services.ZeroKnowledge.Models.ProofResult;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.ZeroKnowledge;

/// <summary>
/// Implementation of the Zero-Knowledge Service that provides privacy-preserving computation capabilities.
/// </summary>
public partial class ZeroKnowledgeService : ServiceFramework.EnclaveBlockchainServiceBase, IZeroKnowledgeService
{
    private readonly Dictionary<string, ZkCircuit> _circuits = new();
    private readonly Dictionary<string, ZkProof> _proofs = new();
    private readonly object _circuitsLock = new();
    
    // Cryptographic algorithm instances
    private readonly System.Security.Cryptography.ECDsa ecdsa = System.Security.Cryptography.ECDsa.Create();
    private readonly System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
    private readonly System.Security.Cryptography.RandomNumberGenerator rng = System.Security.Cryptography.RandomNumberGenerator.Create();

    /// <summary>
    /// Initializes a new instance of the <see cref="ZeroKnowledgeService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="enclaveManager">The enclave manager.</param>
    /// <param name="configuration">The service configuration.</param>
    public ZeroKnowledgeService(ILogger<ZeroKnowledgeService> logger, IEnclaveManager? enclaveManager = null, CoreConfig? configuration = null)
        : base("ZeroKnowledgeService", "Privacy-preserving computation and zero-knowledge proof service", "1.0.0", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        Configuration = configuration;

        AddCapability<IZeroKnowledgeService>();
        AddDependency(new ServiceDependency("KeyManagementService", true, "1.0.0"));
        AddDependency(new ServiceDependency("ComputeService", false, "1.0.0"));
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    protected CoreConfig? Configuration { get; }

    /// <inheritdoc/>
    public async Task<ServiceProofResult> GenerateProofAsync(ProofRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var circuit = GetCircuit(request.CircuitId);
            var proofId = Guid.NewGuid().ToString();

            try
            {
                Logger.LogDebug("Generating ZK proof {ProofId} for circuit {CircuitId}", proofId, request.CircuitId);

                // Generate proof within the enclave for privacy
                var publicInputsAsObject = request.PublicInputs?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value) ?? new Dictionary<string, object>();
                var privateInputsAsObject = request.PrivateInputs?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value) ?? new Dictionary<string, object>();
                
                var proofData = await GenerateProofInEnclaveAsync(circuit, publicInputsAsObject, privateInputsAsObject);
                var publicSignals = await ExtractPublicSignalsAsync(publicInputsAsObject);

                var proof = new ZkProof
                {
                    ProofId = proofId,
                    CircuitId = request.CircuitId,
                    ProofData = proofData,
                    PublicInputs = publicSignals,
                    GeneratedAt = DateTime.UtcNow,
                    IsValid = true,
                    Metadata = new Dictionary<string, object>()
                };

                // Store original public inputs for verification
                proof.Metadata["original_public_inputs"] = request.PublicInputs;

                lock (_circuitsLock)
                {
                    _proofs[proofId] = proof;
                }

                var result = new ServiceProofResult
                {
                    ProofId = proofId,
                    ProofData = System.Text.Encoding.UTF8.GetBytes(proofData),
                    PublicOutputs = new Dictionary<string, object> { ["signals"] = publicSignals },
                    Success = true,
                    GeneratedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object> { ["verification_key"] = await GetVerificationKeyAsync(request.CircuitId) }
                };

                Logger.LogInformation("Generated ZK proof {ProofId} for circuit {CircuitId} on {Blockchain}",
                    proofId, request.CircuitId, blockchainType);

                return result;
            }
            catch (ArgumentException)
            {
                // Re-throw ArgumentExceptions (like invalid witnesses) so tests can catch them
                throw;
            }
            catch (InvalidOperationException)
            {
                // Re-throw InvalidOperationExceptions (like circuit not found) so tests can catch them
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to generate ZK proof {ProofId} for circuit {CircuitId}", proofId, request.CircuitId);

                return new ServiceProofResult
                {
                    ProofId = proofId,
                    ProofData = Array.Empty<byte>(),
                    PublicOutputs = new Dictionary<string, object>(),
                    Success = false,
                    ErrorMessage = ex.Message,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        });
    }

    /// <summary>
    /// Generates a zero-knowledge proof using Core ProofRequest.
    /// </summary>
    public async Task<ServiceProofResult> GenerateProofAsync(NeoServiceLayer.Core.Models.ProofRequest request, BlockchainType blockchainType)
    {
        // Convert Core.Models.ProofRequest to GenerateProofRequest
        var generateRequest = new ProofRequest
        {
            CircuitId = request.CircuitId,
            PublicInputs = ConvertToStringDict(request.PublicInputs),
            PrivateInputs = ConvertToStringDict(request.PrivateInputs)
        };
        
        return await GenerateProofAsync(generateRequest, blockchainType);
    }
    
    private Dictionary<string, string> ConvertToStringDict(Dictionary<string, object> input)
    {
        var result = new Dictionary<string, string>();
        foreach (var kvp in input)
        {
            result[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyProofAsync(ProofVerification verification, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(verification);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var circuit = GetCircuit(verification.CircuitId);

            // Verify proof within the enclave
            var isValid = await VerifyProofInEnclaveAsync(circuit, verification.ProofData, verification.PublicInputs);

            Logger.LogDebug("ZK proof verification for circuit {CircuitId} on {Blockchain}: {IsValid}",
                verification.CircuitId, blockchainType, isValid);

            return isValid;
        });
    }

    /// <inheritdoc/>
    public async Task<ZkComputationResult> ExecutePrivateComputationAsync(ZkComputationRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var computationId = Guid.NewGuid().ToString();

            try
            {
                Logger.LogDebug("Executing private computation {ComputationId} of type {Type}",
                    computationId, request.ComputationType);

                // Execute computation using privacy-preserving operations
                var privacyResult = await ExecutePrivacyComputationAsync(request);

                Logger.LogDebug("Privacy-preserving computation completed: ProofId={ProofId}, Valid={Valid}",
                    privacyResult.ProofId, privacyResult.Valid);

                // Execute actual computation
                var result = await ExecuteComputationInEnclaveAsync(request);
                var proof = await GenerateComputationProofAsync(request, result);

                var computationResult = new ZkComputationResult
                {
                    ComputationId = computationId,
                    CircuitId = request.CircuitId,
                    Results = new object[] { result },
                    Proof = proof,
                    ComputedAt = DateTime.UtcNow,
                    IsValid = privacyResult.Valid,
                    Metadata = new Dictionary<string, object>
                    {
                        ["PrivacyProofId"] = privacyResult.ProofId,
                        ["Commitment"] = privacyResult.Commitment,
                        ["PublicOutputHash"] = privacyResult.PublicOutputHash
                    }
                };

                Logger.LogInformation("Executed private computation {ComputationId} on {Blockchain}",
                    computationId, blockchainType);

                return computationResult;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to execute private computation {ComputationId}", computationId);

                return new ZkComputationResult
                {
                    ComputationId = computationId,
                    CircuitId = request.CircuitId,
                    Results = Array.Empty<object>(),
                    Proof = string.Empty,
                    ComputedAt = DateTime.UtcNow,
                    IsValid = false
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ZkCircuit>> GetCircuitsAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_circuitsLock)
            {
                return _circuits.Values.Where(c => c.IsActive).ToList();
            }
        });
    }

    /// <inheritdoc/>
    public async Task<ZkCircuit> GetCircuitAsync(string circuitId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(circuitId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() => GetCircuit(circuitId));
    }

    /// <inheritdoc/>
    public async Task<ZkProof> GetProofAsync(string proofId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(proofId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() => GetProof(proofId));
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteCircuitAsync(string circuitId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(circuitId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.Run(() =>
        {
            lock (_circuitsLock)
            {
                if (_circuits.TryGetValue(circuitId, out var circuit))
                {
                    circuit.IsActive = false;
                    Logger.LogInformation("Deleted ZK circuit {CircuitId} on {Blockchain}", circuitId, blockchainType);
                    return true;
                }
            }

            Logger.LogWarning("Circuit {CircuitId} not found for deletion on {Blockchain}", circuitId, blockchainType);
            return false;
        });
    }

    /// <summary>
    /// Gets a circuit by ID.
    /// </summary>
    /// <param name="circuitId">The circuit ID.</param>
    /// <returns>The circuit.</returns>
    private ZkCircuit GetCircuit(string circuitId)
    {
        lock (_circuitsLock)
        {
            if (_circuits.TryGetValue(circuitId, out var circuit) && circuit.IsActive)
            {
                return circuit;
            }
        }

        throw new InvalidOperationException($"Circuit not found: {circuitId}");
    }

    /// <summary>
    /// Gets a proof by ID.
    /// </summary>
    /// <param name="proofId">The proof ID.</param>
    /// <returns>The proof.</returns>
    private ZkProof GetProof(string proofId)
    {
        lock (_circuitsLock)
        {
            if (_proofs.TryGetValue(proofId, out var proof))
            {
                return proof;
            }
        }

        throw new ArgumentException($"Proof {proofId} not found", nameof(proofId));
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Zero-Knowledge Service");

        // Initialize default circuits for common use cases
        await InitializeDefaultCircuitsAsync();

        Logger.LogInformation("Zero-Knowledge Service initialized successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Zero-Knowledge Service enclave...");

            // Initialize ZK-specific enclave components
            await InitializeZkEnclaveAsync();

            Logger.LogInformation("Zero-Knowledge Service enclave initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Zero-Knowledge Service enclave");
            return false;
        }
    }

    /// <summary>
    /// Initializes default circuits for common use cases.
    /// </summary>
    private async Task InitializeDefaultCircuitsAsync()
    {
        var defaultCircuits = new[]
        {
            new ZkCircuitDefinition
            {
                Name = "AgeVerification",
                Description = "Verify age without revealing exact age",
                Type = ZkCircuitType.Comparison,
                InputSchema = new Dictionary<string, object> { ["age"] = "number", ["threshold"] = "number" },
                OutputSchema = new Dictionary<string, object> { ["isValid"] = "boolean" }
            },
            new ZkCircuitDefinition
            {
                Name = "BalanceProof",
                Description = "Prove sufficient balance without revealing amount",
                Type = ZkCircuitType.Comparison,
                InputSchema = new Dictionary<string, object> { ["balance"] = "number", ["required"] = "number" },
                OutputSchema = new Dictionary<string, object> { ["hasSufficientBalance"] = "boolean" }
            },
            new ZkCircuitDefinition
            {
                Name = "MembershipProof",
                Description = "Prove membership in a set without revealing identity",
                Type = ZkCircuitType.Membership,
                InputSchema = new Dictionary<string, object> { ["identity"] = "string", ["set"] = "array" },
                OutputSchema = new Dictionary<string, object> { ["isMember"] = "boolean" }
            }
        };

        foreach (var definition in defaultCircuits)
        {
            try
            {
                await CompileCircuitAsync(definition, BlockchainType.NeoN3);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to initialize default circuit {CircuitName}", definition.Name);
            }
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Zero-Knowledge Service");
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Zero-Knowledge Service");
        return true;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        // Check ZK-specific health
        var activeCircuitCount = _circuits.Values.Count(c => c.IsActive);
        var totalProofCount = _proofs.Count;

        Logger.LogDebug("Zero-Knowledge Service health check: {ActiveCircuits} circuits, {TotalProofs} proofs",
            activeCircuitCount, totalProofCount);

        return Task.FromResult(ServiceHealth.Healthy);
    }

    // Make the interface methods explicit to avoid conflicts
    async Task<ServiceProofResult> IZeroKnowledgeService.GenerateProofAsync(ProofRequest request, BlockchainType blockchainType)
    {
        // Convert to Core models and call the implementation
        var coreRequest = new ProofRequest
        {
            CircuitId = request.CircuitId,
            PrivateInputs = request.PrivateInputs,
            PublicInputs = request.PublicInputs,
            Metadata = request.Metadata
        };

        var coreResult = await GenerateProofAsync(coreRequest, blockchainType);

        // Convert back to interface result
        return new ServiceProofResult
        {
            ProofId = coreResult.ProofId,
            Proof = System.Text.Encoding.UTF8.GetString(coreResult.ProofData),
            PublicSignals = coreResult.PublicOutputs.ContainsKey("signals") ?
                (string[])coreResult.PublicOutputs["signals"] : Array.Empty<string>(),
            GeneratedAt = coreResult.GeneratedAt,
            VerificationKey = coreResult.Metadata.ContainsKey("verification_key") ?
                coreResult.Metadata["verification_key"].ToString() ?? "" : ""
        };
    }

    async Task<bool> IZeroKnowledgeService.VerifyProofAsync(ProofVerification verification, BlockchainType blockchainType)
    {
        // Convert to Core models and call the implementation
        var coreVerification = new ProofVerification
        {
            ProofData = System.Text.Encoding.UTF8.GetBytes(verification.Proof),
            PublicInputs = verification.PublicSignals,
            CircuitId = verification.CircuitId
        };

        return await VerifyProofAsync(coreVerification, blockchainType);
    }

    // Helper methods for enclave operations - moved to EnclaveOperations partial class

    private async Task<string[]> ExtractPublicSignalsAsync(Dictionary<string, object> publicInputs)
    {
        await Task.Delay(50);
        return publicInputs.Keys.ToArray();
    }

    // VerifyProofInEnclaveAsync moved to EnclaveOperations partial class

    private async Task<string> GetVerificationKeyAsync(string circuitId)
    {
        await Task.Delay(10);
        return $"vk_{circuitId}";
    }

    private async Task<Dictionary<string, object>> ExecuteComputationInEnclaveAsync(ZkComputationRequest request)
    {
        await Task.Delay(300); // Simulate computation
        return new Dictionary<string, object> { ["result"] = "computed_value", ["type"] = request.ComputationType };
    }

    private async Task<string> GenerateComputationProofAsync(ZkComputationRequest request, Dictionary<string, object> result)
    {
        await Task.Delay(100);
        return $"computation_proof_{request.ComputationType}_{DateTime.UtcNow.Ticks}";
    }

    private async Task InitializeZkEnclaveAsync()
    {
        await Task.Delay(100); // Simulate enclave initialization
        Logger.LogDebug("ZK enclave components initialized");
    }

}

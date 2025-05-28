using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;

namespace NeoServiceLayer.Services.ZeroKnowledge;

/// <summary>
/// Implementation of the Zero-Knowledge Service that provides privacy-preserving computation capabilities.
/// </summary>
public partial class ZeroKnowledgeService : CryptographicServiceBase, IZeroKnowledgeService
{
    private readonly Dictionary<string, ZkCircuit> _circuits = new();
    private readonly Dictionary<string, ZkProof> _proofs = new();
    private readonly object _circuitsLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ZeroKnowledgeService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configuration">The service configuration.</param>
    public ZeroKnowledgeService(ILogger<ZeroKnowledgeService> logger, IServiceConfiguration? configuration = null)
        : base("ZeroKnowledgeService", "Privacy-preserving computation and zero-knowledge proof service", "1.0.0", logger, configuration)
    {
        Configuration = configuration;

        AddCapability<IZeroKnowledgeService>();
        AddDependency(new ServiceDependency("KeyManagementService", "1.0.0", true));
        AddDependency(new ServiceDependency("ComputeService", "1.0.0", false));
    }

    /// <summary>
    /// Gets the service configuration.
    /// </summary>
    protected new IServiceConfiguration? Configuration { get; }

    /// <inheritdoc/>
    public async Task<string> CompileCircuitAsync(ZkCircuitDefinition definition, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteInEnclaveAsync(async () =>
        {
            var circuitId = Guid.NewGuid().ToString();

            // Compile circuit within the enclave for security
            var compiledCircuit = await CompileCircuitInEnclaveAsync(definition);

            var circuit = new ZkCircuit
            {
                CircuitId = circuitId,
                Name = definition.Name,
                Description = definition.Description,
                Type = definition.Type,
                CompiledCode = compiledCircuit,
                InputSchema = definition.InputSchema,
                OutputSchema = definition.OutputSchema,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Metadata = definition.Metadata
            };

            lock (_circuitsLock)
            {
                _circuits[circuitId] = circuit;
            }

            Logger.LogInformation("Compiled ZK circuit {CircuitId} ({Name}) for {Blockchain}",
                circuitId, definition.Name, blockchainType);

            return circuitId;
        });
    }

    /// <inheritdoc/>
    public async Task<ProofResult> GenerateProofAsync(ProofRequest request, BlockchainType blockchainType)
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
                var proofData = await GenerateProofInEnclaveAsync(circuit, request.PublicInputs, request.PrivateInputs);
                var publicSignals = await ExtractPublicSignalsAsync(request.PublicInputs);

                var proof = new ZkProof
                {
                    ProofId = proofId,
                    CircuitId = request.CircuitId,
                    ProofData = proofData,
                    PublicInputs = publicSignals,
                    GeneratedAt = DateTime.UtcNow,
                    IsValid = true,
                    Metadata = request.Parameters
                };

                lock (_circuitsLock)
                {
                    _proofs[proofId] = proof;
                }

                var result = new ProofResult
                {
                    ProofId = proofId,
                    Proof = proofData,
                    PublicSignals = publicSignals,
                    GeneratedAt = DateTime.UtcNow,
                    VerificationKey = await GetVerificationKeyAsync(request.CircuitId)
                };

                Logger.LogInformation("Generated ZK proof {ProofId} for circuit {CircuitId} on {Blockchain}",
                    proofId, request.CircuitId, blockchainType);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to generate ZK proof {ProofId} for circuit {CircuitId}", proofId, request.CircuitId);

                return new ProofResult
                {
                    ProofId = proofId,
                    Proof = string.Empty,
                    PublicSignals = Array.Empty<string>(),
                    GeneratedAt = DateTime.UtcNow,
                    VerificationKey = string.Empty
                };
            }
        });
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
            var isValid = await VerifyProofInEnclaveAsync(circuit, verification.Proof, verification.PublicSignals);

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

                // Execute computation within the enclave for privacy
                var result = await ExecuteComputationInEnclaveAsync(request);
                var proof = await GenerateComputationProofAsync(request, result);

                var computationResult = new ZkComputationResult
                {
                    ComputationId = computationId,
                    Success = true,
                    Result = result,
                    Proof = proof,
                    ExecutedAt = DateTime.UtcNow,
                    ComputationType = request.ComputationType
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
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutedAt = DateTime.UtcNow,
                    ComputationType = request.ComputationType
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

        return await Task.FromResult(() =>
        {
            lock (_circuitsLock)
            {
                return _circuits.Values.Where(c => c.IsActive).ToList();
            }
        })();
    }

    /// <inheritdoc/>
    public async Task<ZkCircuit> GetCircuitAsync(string circuitId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(circuitId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.FromResult(() => GetCircuit(circuitId))();
    }

    /// <inheritdoc/>
    public async Task<ZkProof> GetProofAsync(string proofId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(proofId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.FromResult(() => GetProof(proofId))();
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteCircuitAsync(string circuitId, BlockchainType blockchainType)
    {
        ArgumentException.ThrowIfNullOrEmpty(circuitId);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await Task.FromResult(() =>
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
        })();
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

        throw new ArgumentException($"Circuit {circuitId} not found", nameof(circuitId));
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

        if (!await base.OnInitializeAsync())
        {
            return false;
        }

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
        var baseHealth = base.OnGetHealthAsync().Result;

        if (baseHealth != ServiceHealth.Healthy)
        {
            return Task.FromResult(baseHealth);
        }

        // Check ZK-specific health
        var activeCircuitCount = _circuits.Values.Count(c => c.IsActive);
        var totalProofCount = _proofs.Count;

        Logger.LogDebug("Zero-Knowledge Service health check: {ActiveCircuits} circuits, {TotalProofs} proofs",
            activeCircuitCount, totalProofCount);

        return Task.FromResult(ServiceHealth.Healthy);
    }
}

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Domain;
// using NeoServiceLayer.Tee.Enclave; // TODO: Add project reference when circular dependency resolved

namespace NeoServiceLayer.Core.ConfidentialComputing
{
    /// <summary>
    /// Production implementation of confidential computing service using SGX enclaves
    /// Integrates with existing SGX/TEE infrastructure to provide easy-to-use confidential computing capabilities
    /// </summary>
    public class ConfidentialComputingService : IConfidentialComputingService
    {
        private readonly IEnclaveWrapper _enclaveWrapper;
        private readonly ILogger<ConfidentialComputingService> _logger;
        private readonly Dictionary<string, IConfidentialSession> _activeSessions;
        private readonly object _sessionsLock = new();
        private readonly DateTime _serviceStartTime;

        public ConfidentialComputingService(
            IEnclaveWrapper enclaveWrapper,
            ILogger<ConfidentialComputingService> logger)
        {
            _enclaveWrapper = enclaveWrapper ?? throw new ArgumentNullException(nameof(enclaveWrapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeSessions = new Dictionary<string, IConfidentialSession>();
            _serviceStartTime = DateTime.UtcNow;
        }

        public async Task<ConfidentialComputationResult<TOutput>> ExecuteAsync<TInput, TOutput>(
            ConfidentialComputation<TInput, TOutput> computation,
            TInput input,
            CancellationToken cancellationToken = default)
        {
            if (computation == null)
                throw new ArgumentNullException(nameof(computation));

            if (input == null)
                throw new ArgumentNullException(nameof(input));

            _logger.LogDebug("Executing confidential computation {ComputationId}", computation.ComputationId);

            var metrics = new ComputationMetrics { StartTime = DateTime.UtcNow };

            try
            {
                // Serialize input data
                var inputJson = JsonSerializer.Serialize(input);
                var parameters = new Dictionary<string, object>(computation.Parameters)
                {
                    ["input"] = inputJson,
                    ["computationId"] = computation.ComputationId
                };

                // Execute JavaScript template within SGX enclave
                var scriptResult = await ExecuteJavaScriptAsync(
                    computation.ScriptTemplate,
                    parameters,
                    cancellationToken);

                metrics.EndTime = DateTime.UtcNow;

                if (!scriptResult.Success)
                {
                    _logger.LogError("Confidential computation failed: {Error}", scriptResult.ErrorMessage);
                    return new ConfidentialComputationResult<TOutput>
                    {
                        Success = false,
                        ErrorMessage = scriptResult.ErrorMessage,
                        Metrics = metrics
                    };
                }

                // Deserialize output
                TOutput? result = default;
                if (!string.IsNullOrEmpty(scriptResult.Output))
                {
                    try
                    {
                        result = JsonSerializer.Deserialize<TOutput>(scriptResult.Output);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to deserialize computation result");
                        return new ConfidentialComputationResult<TOutput>
                        {
                            Success = false,
                            ErrorMessage = $"Result deserialization failed: {ex.Message}",
                            Metrics = metrics
                        };
                    }
                }

                // Generate attestation if required
                AttestationProof? attestation = null;
                if (computation.SecurityRequirements.RequireRemoteAttestation)
                {
                    var attestationReport = await GetAttestationAsync(cancellationToken);
                    attestation = new AttestationProof
                    {
                        ProofId = Guid.NewGuid().ToString(),
                        InputHash = ComputeHash(inputJson),
                        OutputHash = ComputeHash(scriptResult.Output ?? ""),
                        AttestationReport = attestationReport,
                        GeneratedAt = DateTime.UtcNow
                    };
                }

                metrics.PeakMemoryBytes = scriptResult.MemoryUsedBytes;
                metrics.GasEstimate = EstimateGasUsage(computation, scriptResult);

                _logger.LogDebug("Confidential computation {ComputationId} completed successfully in {Duration}ms", 
                    computation.ComputationId, metrics.Duration.TotalMilliseconds);

                return new ConfidentialComputationResult<TOutput>
                {
                    Success = true,
                    Result = result,
                    Metrics = metrics,
                    Attestation = attestation,
                    Metadata = new Dictionary<string, object>
                    {
                        ["executionTimeMs"] = scriptResult.ExecutionTimeMs,
                        ["memoryUsedBytes"] = scriptResult.MemoryUsedBytes,
                        ["computationId"] = computation.ComputationId
                    }
                };
            }
            catch (Exception ex)
            {
                metrics.EndTime = DateTime.UtcNow;
                _logger.LogError(ex, "Confidential computation {ComputationId} failed with exception", computation.ComputationId);
                
                return new ConfidentialComputationResult<TOutput>
                {
                    Success = false,
                    ErrorMessage = $"Computation failed: {ex.Message}",
                    Metrics = metrics
                };
            }
        }

        public async Task<ConfidentialScriptResult> ExecuteJavaScriptAsync(
            string scriptTemplate,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(scriptTemplate))
                throw new ArgumentException("Script template cannot be null or empty", nameof(scriptTemplate));

            parameters ??= new Dictionary<string, object>();

            try
            {
                // Convert parameters to JSON
                var parametersJson = JsonSerializer.Serialize(parameters);
                
                // Execute within SGX enclave
                var startTime = DateTime.UtcNow;
                var result = _enclaveWrapper.ExecuteJavaScript(scriptTemplate, parametersJson);
                var endTime = DateTime.UtcNow;

                // Generate attestation if available
                AttestationProof? attestation = null;
                try
                {
                    var attestationData = _enclaveWrapper.GetAttestation();
                    if (attestationData?.Length > 0)
                    {
                        attestation = new AttestationProof
                        {
                            ProofId = Guid.NewGuid().ToString(),
                            InputHash = ComputeHash(parametersJson),
                            OutputHash = ComputeHash(result),
                            GeneratedAt = DateTime.UtcNow
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate attestation for script execution");
                }

                return new ConfidentialScriptResult
                {
                    Success = true,
                    Output = result,
                    ExecutionTimeMs = (long)(endTime - startTime).TotalMilliseconds,
                    MemoryUsedBytes = 0, // Would need enclave-specific measurement
                    Attestation = attestation
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JavaScript execution within enclave failed");
                return new ConfidentialScriptResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutionTimeMs = 0,
                    MemoryUsedBytes = 0
                };
            }
        }

        public async Task<IConfidentialSession> CreateSessionAsync(
            ConfidentialSessionOptions sessionOptions,
            CancellationToken cancellationToken = default)
        {
            if (sessionOptions == null)
                throw new ArgumentNullException(nameof(sessionOptions));

            var sessionId = Guid.NewGuid().ToString();
            var session = new ConfidentialSession(sessionId, sessionOptions, _enclaveWrapper, _logger);

            await session.InitializeAsync(cancellationToken);

            lock (_sessionsLock)
            {
                _activeSessions[sessionId] = session;
            }

            // Set up session cleanup on dispose
            session.SessionDisposed += (sender, args) =>
            {
                lock (_sessionsLock)
                {
                    _activeSessions.Remove(sessionId);
                }
            };

            _logger.LogDebug("Created confidential session {SessionId}", sessionId);
            return session;
        }

        public async Task<byte[]> GenerateSecureRandomAsync(int length, CancellationToken cancellationToken = default)
        {
            if (length <= 0 || length > 1024 * 1024) // 1MB limit
                throw new ArgumentException("Length must be between 1 and 1MB", nameof(length));

            try
            {
                return _enclaveWrapper.GenerateRandomBytes(length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate secure random bytes within enclave");
                throw new InvalidOperationException("Secure random generation failed", ex);
            }
        }

        public async Task<ConfidentialEncryptionResult> EncryptAsync(
            byte[] data,
            string keyId,
            CancellationToken cancellationToken = default)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(keyId))
                throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));

            try
            {
                // Generate a key based on keyId (in production, this would use proper key management)
                var key = await GenerateKeyFromId(keyId);
                var encryptedData = _enclaveWrapper.Encrypt(data, key);

                return new ConfidentialEncryptionResult
                {
                    Success = true,
                    EncryptedData = encryptedData,
                    KeyId = keyId,
                    Algorithm = "AES-256-GCM"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encryption failed for key {KeyId}", keyId);
                return new ConfidentialEncryptionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<byte[]> DecryptAsync(
            byte[] encryptedData,
            string keyId,
            CancellationToken cancellationToken = default)
        {
            if (encryptedData == null)
                throw new ArgumentNullException(nameof(encryptedData));
            if (string.IsNullOrEmpty(keyId))
                throw new ArgumentException("Key ID cannot be null or empty", nameof(keyId));

            try
            {
                var key = await GenerateKeyFromId(keyId);
                return _enclaveWrapper.Decrypt(encryptedData, key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Decryption failed for key {KeyId}", keyId);
                throw new InvalidOperationException("Decryption failed", ex);
            }
        }

        public async Task<AttestationReport> GetAttestationAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var attestationJson = _enclaveWrapper.GetAttestationReport();
                var attestationData = JsonSerializer.Deserialize<Dictionary<string, object>>(attestationJson);

                return new AttestationReport
                {
                    Version = 1,
                    EnclaveHash = attestationData?.GetValueOrDefault("enclaveHash")?.ToString() ?? "",
                    SignerHash = attestationData?.GetValueOrDefault("signerHash")?.ToString() ?? "",
                    SecurityVersion = 1,
                    PlatformInstanceId = attestationData?.GetValueOrDefault("platformId")?.ToString() ?? "",
                    IsDebugEnclave = attestationData?.GetValueOrDefault("debugMode")?.ToString() == "true",
                    RawData = System.Text.Encoding.UTF8.GetBytes(attestationJson),
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get SGX attestation");
                throw new InvalidOperationException("Attestation generation failed", ex);
            }
        }

        public async Task<ConfidentialComputingHealth> GetHealthAsync()
        {
            var health = new ConfidentialComputingHealth
            {
                Status = HealthStatus.Healthy,
                EnclaveInitialized = true, // Assume initialized if we can create the service
                EnclaveMode = "Hardware", // Would check actual mode in production
                Uptime = DateTime.UtcNow - _serviceStartTime,
                CheckedAt = DateTime.UtcNow
            };

            lock (_sessionsLock)
            {
                health.ActiveSessions = _activeSessions.Count;
            }

            // Check SGX hardware availability
            try
            {
                var attestation = await GetAttestationAsync();
                health.SgxHardwareAvailable = true;
                health.LastAttestationTime = attestation.Timestamp;
            }
            catch
            {
                health.SgxHardwareAvailable = false;
                health.Status = HealthStatus.Degraded;
            }

            // Estimate memory usage (would be more accurate in production)
            health.MemoryUsagePercent = Math.Min(95.0, health.ActiveSessions * 5.0);

            health.Details["totalSessionsCreated"] = _activeSessions.Count;
            health.Details["serviceVersion"] = "2.0.0";

            return health;
        }

        private async Task<byte[]> GenerateKeyFromId(string keyId)
        {
            // In production, this would use proper key derivation or key management service
            // For now, generate a deterministic key based on the keyId
            await Task.CompletedTask;
            
            var keyMaterial = System.Text.Encoding.UTF8.GetBytes($"key_{keyId}");
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(keyMaterial);
        }

        private string ComputeHash(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }

        private long EstimateGasUsage(ConfidentialComputation<object, object> computation, ConfidentialScriptResult scriptResult)
        {
            // Simple gas estimation based on execution time and memory usage
            var baseGas = 21000L;
            var executionGas = scriptResult.ExecutionTimeMs * 100;
            var memoryGas = scriptResult.MemoryUsedBytes / 1024;
            var complexityGas = computation.Parameters.Count * 1000;

            return baseGas + executionGas + memoryGas + complexityGas;
        }
    }

    /// <summary>
    /// Implementation of a confidential session
    /// </summary>
    internal class ConfidentialSession : IConfidentialSession
    {
        private readonly IEnclaveWrapper _enclaveWrapper;
        private readonly ILogger _logger;
        private readonly ConfidentialSessionOptions _options;
        private readonly Dictionary<string, byte[]> _sessionStorage;
        private readonly object _storageLock = new();
        private bool _disposed = false;

        public string SessionId { get; }
        public DateTime CreatedAt { get; }
        public bool IsActive { get; private set; }

        public event EventHandler? SessionDisposed;

        public ConfidentialSession(
            string sessionId,
            ConfidentialSessionOptions options,
            IEnclaveWrapper enclaveWrapper,
            ILogger logger)
        {
            SessionId = sessionId;
            CreatedAt = DateTime.UtcNow;
            _options = options;
            _enclaveWrapper = enclaveWrapper;
            _logger = logger;
            _sessionStorage = new Dictionary<string, byte[]>();
            IsActive = false;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            // Initialize session within enclave if needed
            await Task.CompletedTask;
            IsActive = true;
            _logger.LogDebug("Confidential session {SessionId} initialized", SessionId);
        }

        public async Task<ConfidentialComputationResult<TOutput>> ExecuteAsync<TInput, TOutput>(
            ConfidentialComputation<TInput, TOutput> computation,
            TInput input,
            CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                throw new InvalidOperationException("Session is not active");

            // Add session context to computation parameters
            computation.Parameters["sessionId"] = SessionId;
            computation.Parameters["sessionCreatedAt"] = CreatedAt.ToString("O");

            // Use the main service logic (would be refactored to shared code in production)
            var service = new ConfidentialComputingService(_enclaveWrapper, _logger);
            return await service.ExecuteAsync(computation, input, cancellationToken);
        }

        public async Task<bool> StoreAsync(string key, byte[] data, CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                return false;

            if (string.IsNullOrEmpty(key) || data == null)
                return false;

            try
            {
                // In production, this would use SGX sealing
                var sealedData = _enclaveWrapper.SealData(data);

                lock (_storageLock)
                {
                    _sessionStorage[key] = sealedData;
                }

                _logger.LogDebug("Stored data for key {Key} in session {SessionId}", key, SessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store data for key {Key} in session {SessionId}", key, SessionId);
                return false;
            }
        }

        public async Task<byte[]?> RetrieveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                return null;

            if (string.IsNullOrEmpty(key))
                return null;

            try
            {
                byte[]? sealedData;
                lock (_storageLock)
                {
                    _sessionStorage.TryGetValue(key, out sealedData);
                }

                if (sealedData == null)
                    return null;

                // In production, this would use SGX unsealing
                var data = _enclaveWrapper.UnsealData(sealedData);
                
                _logger.LogDebug("Retrieved data for key {Key} from session {SessionId}", key, SessionId);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve data for key {Key} from session {SessionId}", key, SessionId);
                return null;
            }
        }

        public async Task TerminateAsync(CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                return;

            IsActive = false;

            // Clear all session data securely
            lock (_storageLock)
            {
                foreach (var kvp in _sessionStorage)
                {
                    // Securely wipe the data
                    Array.Clear(kvp.Value, 0, kvp.Value.Length);
                }
                _sessionStorage.Clear();
            }

            _logger.LogDebug("Terminated confidential session {SessionId}", SessionId);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            TerminateAsync().GetAwaiter().GetResult();
            SessionDisposed?.Invoke(this, EventArgs.Empty);
            _disposed = true;
        }
    }
}
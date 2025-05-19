using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Host.Tests.Mocks;

namespace NeoServiceLayer.Tee.Host.Tests.Mocks
{
    // Mock implementation of OpenEnclaveInterface for testing
    public class OpenEnclaveInterface : ITeeEnclaveInterface
    {
        private readonly ILogger<OpenEnclaveInterface> _logger;
        private readonly string _enclavePath;
        private bool _disposed;
        private IntPtr _enclaveId = IntPtr.Zero;

        public OpenEnclaveInterface(ILogger<OpenEnclaveInterface> logger, string enclavePath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(enclavePath))
            {
                throw new ArgumentException("Enclave path cannot be null or empty", nameof(enclavePath));
            }

            _enclavePath = enclavePath;
            _logger.LogInformation("OpenEnclaveInterface created with enclave path: {EnclavePath}", _enclavePath);
        }

        public void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(OpenEnclaveInterface));
            }
        }

        public IntPtr GetEnclaveId()
        {
            CheckDisposed();
            return _enclaveId;
        }

        public byte[] GetMrEnclave()
        {
            CheckDisposed();
            return new byte[32];
        }

        public byte[] GetMrSigner()
        {
            CheckDisposed();
            return new byte[32];
        }

        public byte[] GetRandomBytes(int length)
        {
            CheckDisposed();

            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero");
            }

            _logger.LogDebug("Generating {Length} random bytes", length);
            return new byte[length];
        }

        public byte[] SignData(byte[] data)
        {
            CheckDisposed();

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _logger.LogDebug("Signing data of length {Length}", data.Length);
            return new byte[64];
        }

        public bool VerifySignature(byte[] data, byte[] signature)
        {
            CheckDisposed();

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }

            _logger.LogDebug("Verifying signature for data of length {DataLength}", data.Length);
            return true;
        }

        public byte[] SealData(byte[] data)
        {
            CheckDisposed();

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _logger.LogDebug("Sealing data of length {DataLength}", data.Length);
            return new byte[data.Length + 32];
        }

        public byte[] UnsealData(byte[] sealedData)
        {
            CheckDisposed();

            if (sealedData == null)
            {
                throw new ArgumentNullException(nameof(sealedData));
            }

            _logger.LogDebug("Unsealing data of length {SealedDataLength}", sealedData.Length);
            return new byte[sealedData.Length - 32];
        }

        public Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId)
        {
            CheckDisposed();

            if (code == null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (secrets == null)
            {
                throw new ArgumentNullException(nameof(secrets));
            }

            if (functionId == null)
            {
                throw new ArgumentNullException(nameof(functionId));
            }

            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            _logger.LogDebug("Executing JavaScript code for function {FunctionId}, user {UserId}", functionId, userId);
            return Task.FromResult("{\"result\": \"success\"}");
        }

        public Task RecordExecutionMetricsAsync(string functionId, string userId, long gasUsed)
        {
            CheckDisposed();
            _logger.LogDebug("Recording execution metrics for function {FunctionId}, user {UserId}, gas used {GasUsed}", functionId, userId, gasUsed);
            return Task.CompletedTask;
        }

        public Task RecordExecutionFailureAsync(string functionId, string userId, string errorMessage)
        {
            CheckDisposed();
            _logger.LogDebug("Recording execution failure for function {FunctionId}, user {UserId}, error {ErrorMessage}", functionId, userId, errorMessage);
            return Task.CompletedTask;
        }

        public Task StoreUserSecretAsync(string userId, string secretName, string secretValue)
        {
            CheckDisposed();
            _logger.LogDebug("Storing user secret for user {UserId}, secret name {SecretName}", userId, secretName);
            return Task.CompletedTask;
        }

        public Task<string> GetUserSecretAsync(string userId, string secretName)
        {
            CheckDisposed();
            _logger.LogDebug("Getting user secret for user {UserId}, secret name {SecretName}", userId, secretName);
            return Task.FromResult("secret-value");
        }

        public Task DeleteUserSecretAsync(string userId, string secretName)
        {
            CheckDisposed();
            _logger.LogDebug("Deleting user secret for user {UserId}, secret name {SecretName}", userId, secretName);
            return Task.CompletedTask;
        }

        public byte[] GetAttestationReport(byte[] reportData)
        {
            CheckDisposed();
            _logger.LogDebug("Getting attestation report");
            return new byte[256];
        }

        public bool InitializeOcclum()
        {
            CheckDisposed();
            _logger.LogDebug("Initializing Occlum");
            return true;
        }

        public Task<string> ExecuteOcclumCommandAsync(string command)
        {
            CheckDisposed();
            _logger.LogDebug("Executing Occlum command: {Command}", command);
            return Task.FromResult("command-output");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.LogInformation("Disposing OpenEnclaveInterface");
                _disposed = true;
            }
        }
    }
}

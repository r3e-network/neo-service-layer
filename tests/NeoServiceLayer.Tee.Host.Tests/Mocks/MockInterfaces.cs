using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Enclave;

namespace NeoServiceLayer.Tee.Host.Tests.Mocks
{
    // Mock interfaces for testing
    public interface IOpenEnclaveInterface : ITeeEnclaveInterface
    {
        bool InitializeOcclum();
        Task<string> ExecuteOcclumCommandAsync(string command);
    }

    public class MockOpenEnclaveInterface : IOpenEnclaveInterface
    {
        private readonly ILogger<MockOpenEnclaveInterface> _logger;
        private bool _disposed;

        public MockOpenEnclaveInterface(ILogger<MockOpenEnclaveInterface> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IntPtr GetEnclaveId()
        {
            return IntPtr.Zero;
        }

        public byte[] GetMrEnclave()
        {
            return new byte[32];
        }

        public byte[] GetMrSigner()
        {
            return new byte[32];
        }

        public byte[] GetRandomBytes(int length)
        {
            return new byte[length];
        }

        public byte[] SignData(byte[] data)
        {
            return new byte[64];
        }

        public bool VerifySignature(byte[] data, byte[] signature)
        {
            return true;
        }

        public byte[] SealData(byte[] data)
        {
            return new byte[data.Length + 32];
        }

        public byte[] UnsealData(byte[] sealedData)
        {
            return new byte[sealedData.Length - 32];
        }

        public Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId)
        {
            return Task.FromResult("{\"result\": \"success\"}");
        }

        public Task RecordExecutionMetricsAsync(string functionId, string userId, long gasUsed)
        {
            return Task.CompletedTask;
        }

        public Task RecordExecutionFailureAsync(string functionId, string userId, string errorMessage)
        {
            return Task.CompletedTask;
        }

        public Task StoreUserSecretAsync(string userId, string secretName, string secretValue)
        {
            return Task.CompletedTask;
        }

        public Task<string> GetUserSecretAsync(string userId, string secretName)
        {
            return Task.FromResult("secret-value");
        }

        public Task DeleteUserSecretAsync(string userId, string secretName)
        {
            return Task.CompletedTask;
        }

        public byte[] GetAttestationReport(byte[] reportData)
        {
            return new byte[256];
        }

        public bool InitializeOcclum()
        {
            return true;
        }

        public Task<string> ExecuteOcclumCommandAsync(string command)
        {
            return Task.FromResult("command-output");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }

    public class MockSgxEnclaveInterface : ITeeEnclaveInterface
    {
        private readonly ILogger<MockSgxEnclaveInterface> _logger;
        private bool _disposed;

        public MockSgxEnclaveInterface(ILogger<MockSgxEnclaveInterface> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IntPtr GetEnclaveId()
        {
            return IntPtr.Zero;
        }

        public byte[] GetMrEnclave()
        {
            return new byte[32];
        }

        public byte[] GetMrSigner()
        {
            return new byte[32];
        }

        public byte[] GetRandomBytes(int length)
        {
            return new byte[length];
        }

        public byte[] SignData(byte[] data)
        {
            return new byte[64];
        }

        public bool VerifySignature(byte[] data, byte[] signature)
        {
            return true;
        }

        public byte[] SealData(byte[] data)
        {
            return new byte[data.Length + 32];
        }

        public byte[] UnsealData(byte[] sealedData)
        {
            return new byte[sealedData.Length - 32];
        }

        public Task<string> ExecuteJavaScriptAsync(string code, string input, string secrets, string functionId, string userId)
        {
            return Task.FromResult("{\"result\": \"success\"}");
        }

        public Task RecordExecutionMetricsAsync(string functionId, string userId, long gasUsed)
        {
            return Task.CompletedTask;
        }

        public Task RecordExecutionFailureAsync(string functionId, string userId, string errorMessage)
        {
            return Task.CompletedTask;
        }

        public Task StoreUserSecretAsync(string userId, string secretName, string secretValue)
        {
            return Task.CompletedTask;
        }

        public Task<string> GetUserSecretAsync(string userId, string secretName)
        {
            return Task.FromResult("secret-value");
        }

        public Task DeleteUserSecretAsync(string userId, string secretName)
        {
            return Task.CompletedTask;
        }

        public byte[] GetAttestationReport(byte[] reportData)
        {
            return new byte[256];
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}

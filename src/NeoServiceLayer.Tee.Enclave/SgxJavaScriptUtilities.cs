using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NeoServiceLayer.Shared.Models;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Provides JavaScript utilities that interact with the SGX enclave.
    /// </summary>
    public class SgxJavaScriptUtilities
    {
        private readonly ISgxEnclaveInterface _sgxInterface;
        private readonly ILogger _logger;
        private readonly IntPtr _contextId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SgxJavaScriptUtilities"/> class.
        /// </summary>
        /// <param name="sgxInterface">The SGX enclave interface.</param>
        /// <param name="contextId">The context ID.</param>
        public SgxJavaScriptUtilities(ISgxEnclaveInterface sgxInterface, IntPtr contextId)
        {
            _sgxInterface = sgxInterface ?? throw new ArgumentNullException(nameof(sgxInterface));
            _contextId = contextId;
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SgxJavaScriptUtilities>();
        }

        /// <summary>
        /// Gets the context ID.
        /// </summary>
        public IntPtr ContextId => _contextId;

        /// <summary>
        /// Gets the value of a secret from the SGX enclave.
        /// </summary>
        /// <param name="name">The name of the secret.</param>
        /// <returns>The secret value.</returns>
        public string GetSecretValue(string name)
        {
            try
            {
                return _sgxInterface.GetUserSecretAsync("system", name).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting secret {Name}", name);
                throw;
            }
        }

        /// <summary>
        /// Logs a message securely within the SGX enclave.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void SecureLog(string message)
        {
            try
            {
                _logger.LogInformation("JavaScript: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging secure message");
                throw;
            }
        }

        /// <summary>
        /// Verifies a signature using the SGX enclave.
        /// </summary>
        /// <param name="data">The data to verify.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        public bool VerifySignature(string data, string signature)
        {
            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] signatureBytes = Convert.FromBase64String(signature);
                return _sgxInterface.VerifySignature(dataBytes, signatureBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signature");
                throw;
            }
        }

        /// <summary>
        /// Gets random bytes from the SGX enclave.
        /// </summary>
        /// <param name="length">The number of bytes to get.</param>
        /// <returns>The random bytes as a base64 string.</returns>
        public string GetRandomBytes(int length)
        {
            try
            {
                byte[] randomBytes = _sgxInterface.GetRandomBytes(length);
                return Convert.ToBase64String(randomBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random bytes");
                throw;
            }
        }
    }
}

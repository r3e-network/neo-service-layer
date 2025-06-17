using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Host.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Helper utilities for secure enclave operations and data processing.
/// Provides reusable security functions that can be used across all services.
/// </summary>
public static class EnclaveSecurityHelpers
{
    /// <summary>
    /// Executes a secure operation within an enclave with comprehensive error handling.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="enclaveManager">The enclave manager instance.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the secure operation.</returns>
    public static async Task<T> ExecuteSecureOperationAsync<T>(
        IEnclaveManager enclaveManager,
        Func<Task<T>> operation,
        string operationName,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (enclaveManager == null) throw new ArgumentNullException(nameof(enclaveManager));
        if (operation == null) throw new ArgumentNullException(nameof(operation));
        if (string.IsNullOrWhiteSpace(operationName)) throw new ArgumentException("Operation name cannot be null or empty", nameof(operationName));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        if (!enclaveManager.IsInitialized)
        {
            throw new InvalidOperationException($"Enclave must be initialized before executing {operationName}");
        }

        try
        {
            logger.LogDebug("Executing secure operation: {OperationName}", operationName);
            
            var result = await operation();
            
            logger.LogDebug("Secure operation completed successfully: {OperationName}", operationName);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Secure operation cancelled: {OperationName}", operationName);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Secure operation failed: {OperationName}", operationName);
            throw new InvalidOperationException($"Secure operation '{operationName}' failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Securely processes sensitive data within an enclave.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager instance.</param>
    /// <param name="sensitiveData">The sensitive data to process.</param>
    /// <param name="processingScript">The JavaScript processing script to execute in the enclave.</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>The processed data result.</returns>
    public static async Task<string> ProcessSensitiveDataAsync(
        IEnclaveManager enclaveManager,
        string sensitiveData,
        string processingScript,
        ILogger logger)
    {
        if (enclaveManager == null) throw new ArgumentNullException(nameof(enclaveManager));
        if (string.IsNullOrWhiteSpace(sensitiveData)) throw new ArgumentException("Sensitive data cannot be null or empty", nameof(sensitiveData));
        if (string.IsNullOrWhiteSpace(processingScript)) throw new ArgumentException("Processing script cannot be null or empty", nameof(processingScript));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        return await ExecuteSecureOperationAsync(
            enclaveManager,
            async () =>
            {
                // Encode sensitive data as base64 for safe transport to enclave
                var encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(sensitiveData));
                
                // Create a secure processing script with encoded data
                var secureScript = $@"
                    (function() {{
                        try {{
                            const sensitiveData = atob('{encodedData}');
                            {processingScript}
                        }} catch (error) {{
                            throw new Error('Secure data processing failed: ' + error.message);
                        }}
                    }})()";

                var result = await enclaveManager.ExecuteJavaScriptAsync(secureScript);
                return result?.ToString() ?? throw new InvalidOperationException("Secure data processing returned null result");
            },
            "ProcessSensitiveData",
            logger);
    }

    /// <summary>
    /// Generates a secure hash of sensitive data within the enclave.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager instance.</param>
    /// <param name="data">The data to hash.</param>
    /// <param name="algorithm">The hash algorithm to use (SHA256, SHA512, etc.).</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>The secure hash as a hexadecimal string.</returns>
    public static async Task<string> ComputeSecureHashAsync(
        IEnclaveManager enclaveManager,
        string data,
        string algorithm,
        ILogger logger)
    {
        if (enclaveManager == null) throw new ArgumentNullException(nameof(enclaveManager));
        if (string.IsNullOrWhiteSpace(data)) throw new ArgumentException("Data cannot be null or empty", nameof(data));
        if (string.IsNullOrWhiteSpace(algorithm)) throw new ArgumentException("Algorithm cannot be null or empty", nameof(algorithm));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        return await ExecuteSecureOperationAsync(
            enclaveManager,
            async () =>
            {
                var encodedData = Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
                var hashScript = $@"
                    (function() {{
                        const crypto = require('crypto');
                        const data = Buffer.from('{encodedData}', 'base64');
                        const hash = crypto.createHash('{algorithm.ToLowerInvariant()}');
                        hash.update(data);
                        return hash.digest('hex');
                    }})()";

                var result = await enclaveManager.ExecuteJavaScriptAsync(hashScript);
                return result?.ToString() ?? throw new InvalidOperationException("Hash computation failed");
            },
            $"ComputeSecureHash_{algorithm}",
            logger);
    }

    /// <summary>
    /// Generates cryptographically secure random data within the enclave.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager instance.</param>
    /// <param name="length">The length of random data to generate in bytes.</param>
    /// <param name="format">The output format (hex, base64, bytes).</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>The secure random data in the specified format.</returns>
    public static async Task<string> GenerateSecureRandomAsync(
        IEnclaveManager enclaveManager,
        int length,
        string format,
        ILogger logger)
    {
        if (enclaveManager == null) throw new ArgumentNullException(nameof(enclaveManager));
        if (length <= 0) throw new ArgumentException("Length must be positive", nameof(length));
        if (string.IsNullOrWhiteSpace(format)) throw new ArgumentException("Format cannot be null or empty", nameof(format));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        if (length > 4096)
        {
            throw new ArgumentException("Random data length cannot exceed 4096 bytes for security reasons");
        }

        return await ExecuteSecureOperationAsync(
            enclaveManager,
            async () =>
            {
                var randomScript = $@"
                    (function() {{
                        const crypto = require('crypto');
                        const randomBytes = crypto.randomBytes({length});
                        
                        switch ('{format.ToLowerInvariant()}') {{
                            case 'hex':
                                return randomBytes.toString('hex');
                            case 'base64':
                                return randomBytes.toString('base64');
                            case 'bytes':
                                return Array.from(randomBytes);
                            default:
                                throw new Error('Unsupported format: {format}');
                        }}
                    }})()";

                var result = await enclaveManager.ExecuteJavaScriptAsync(randomScript);
                return result?.ToString() ?? throw new InvalidOperationException("Random generation failed");
            },
            $"GenerateSecureRandom_{length}_{format}",
            logger);
    }

    /// <summary>
    /// Validates data integrity using secure checksums within the enclave.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager instance.</param>
    /// <param name="data">The data to validate.</param>
    /// <param name="expectedChecksum">The expected checksum.</param>
    /// <param name="algorithm">The checksum algorithm.</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>True if the data integrity is valid, false otherwise.</returns>
    public static async Task<bool> ValidateDataIntegrityAsync(
        IEnclaveManager enclaveManager,
        string data,
        string expectedChecksum,
        string algorithm,
        ILogger logger)
    {
        if (enclaveManager == null) throw new ArgumentNullException(nameof(enclaveManager));
        if (string.IsNullOrWhiteSpace(data)) throw new ArgumentException("Data cannot be null or empty", nameof(data));
        if (string.IsNullOrWhiteSpace(expectedChecksum)) throw new ArgumentException("Expected checksum cannot be null or empty", nameof(expectedChecksum));
        if (string.IsNullOrWhiteSpace(algorithm)) throw new ArgumentException("Algorithm cannot be null or empty", nameof(algorithm));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        return await ExecuteSecureOperationAsync(
            enclaveManager,
            async () =>
            {
                var computedChecksum = await ComputeSecureHashAsync(enclaveManager, data, algorithm, logger);
                
                var isValid = string.Equals(computedChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
                
                if (!isValid)
                {
                    logger.LogWarning("Data integrity validation failed. Expected: {Expected}, Computed: {Computed}",
                        expectedChecksum, computedChecksum);
                }
                
                return isValid;
            },
            "ValidateDataIntegrity",
            logger);
    }

    /// <summary>
    /// Securely sanitizes input data within the enclave to prevent injection attacks.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager instance.</param>
    /// <param name="input">The input data to sanitize.</param>
    /// <param name="sanitizationType">The type of sanitization to apply.</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>The sanitized input data.</returns>
    public static async Task<string> SanitizeInputAsync(
        IEnclaveManager enclaveManager,
        string input,
        SanitizationType sanitizationType,
        ILogger logger)
    {
        if (enclaveManager == null) throw new ArgumentNullException(nameof(enclaveManager));
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        return await ExecuteSecureOperationAsync(
            enclaveManager,
            async () =>
            {
                var encodedInput = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
                
                var sanitizationScript = sanitizationType switch
                {
                    SanitizationType.HtmlEncode => $@"
                        (function() {{
                            const input = atob('{encodedInput}');
                            return input
                                .replace(/&/g, '&amp;')
                                .replace(/</g, '&lt;')
                                .replace(/>/g, '&gt;')
                                .replace(/'/g, '&#39;')
                                .replace(/\x22/g, '&quot;');
                        }})()",
                    
                    SanitizationType.SqlEscape => $@"
                        (function() {{
                            const input = atob('{encodedInput}');
                            return input.replace(/'/g, """""").replace(/\x00/g, '\\0');
                        }})()",
                    
                    SanitizationType.JsonEscape => $@"
                        (function() {{
                            const input = atob('{encodedInput}');
                            return JSON.stringify(input).slice(1, -1);
                        }})()",
                    
                    SanitizationType.Alphanumeric => $@"
                        (function() {{
                            const input = atob('{encodedInput}');
                            return input.replace(/[^a-zA-Z0-9]/g, '');
                        }})()",
                    
                    _ => throw new ArgumentException($"Unsupported sanitization type: {sanitizationType}")
                };

                var result = await enclaveManager.ExecuteJavaScriptAsync(sanitizationScript);
                return result?.ToString() ?? throw new InvalidOperationException("Input sanitization failed");
            },
            $"SanitizeInput_{sanitizationType}",
            logger);
    }

    /// <summary>
    /// Creates a secure audit trail entry within the enclave.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager instance.</param>
    /// <param name="operation">The operation being audited.</param>
    /// <param name="userId">The user ID performing the operation.</param>
    /// <param name="resourceId">The resource ID being accessed.</param>
    /// <param name="metadata">Additional metadata for the audit entry.</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>The secure audit trail entry.</returns>
    public static async Task<SecureAuditEntry> CreateSecureAuditEntryAsync(
        IEnclaveManager enclaveManager,
        string operation,
        string userId,
        string resourceId,
        Dictionary<string, object>? metadata,
        ILogger logger)
    {
        if (enclaveManager == null) throw new ArgumentNullException(nameof(enclaveManager));
        if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Operation cannot be null or empty", nameof(operation));
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        return await ExecuteSecureOperationAsync(
            enclaveManager,
            async () =>
            {
                var auditEntry = new SecureAuditEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    Operation = operation,
                    UserId = userId,
                    ResourceId = resourceId,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                // Generate a secure signature for the audit entry
                var entryData = JsonSerializer.Serialize(auditEntry);
                var signature = await ComputeSecureHashAsync(enclaveManager, entryData, "SHA256", logger);
                
                auditEntry.Signature = signature;
                auditEntry.Metadata["enclave_processed"] = true;
                auditEntry.Metadata["signature_algorithm"] = "SHA256";

                return auditEntry;
            },
            "CreateSecureAuditEntry",
            logger);
    }

    /// <summary>
    /// Verifies the authenticity of a secure audit entry.
    /// </summary>
    /// <param name="enclaveManager">The enclave manager instance.</param>
    /// <param name="auditEntry">The audit entry to verify.</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>True if the audit entry is authentic, false otherwise.</returns>
    public static async Task<bool> VerifySecureAuditEntryAsync(
        IEnclaveManager enclaveManager,
        SecureAuditEntry auditEntry,
        ILogger logger)
    {
        if (enclaveManager == null) throw new ArgumentNullException(nameof(enclaveManager));
        if (auditEntry == null) throw new ArgumentNullException(nameof(auditEntry));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        return await ExecuteSecureOperationAsync(
            enclaveManager,
            async () =>
            {
                // Create a copy without the signature for verification
                var verificationEntry = new SecureAuditEntry
                {
                    Id = auditEntry.Id,
                    Timestamp = auditEntry.Timestamp,
                    Operation = auditEntry.Operation,
                    UserId = auditEntry.UserId,
                    ResourceId = auditEntry.ResourceId,
                    Metadata = new Dictionary<string, object>(auditEntry.Metadata)
                };

                // Remove signature-related metadata for verification
                verificationEntry.Metadata.Remove("enclave_processed");
                verificationEntry.Metadata.Remove("signature_algorithm");

                var entryData = JsonSerializer.Serialize(verificationEntry);
                var computedSignature = await ComputeSecureHashAsync(enclaveManager, entryData, "SHA256", logger);

                return string.Equals(computedSignature, auditEntry.Signature, StringComparison.OrdinalIgnoreCase);
            },
            "VerifySecureAuditEntry",
            logger);
    }
}

/// <summary>
/// Types of input sanitization supported by the enclave security helpers.
/// </summary>
public enum SanitizationType
{
    /// <summary>HTML encoding to prevent XSS attacks.</summary>
    HtmlEncode,
    
    /// <summary>SQL escaping to prevent SQL injection.</summary>
    SqlEscape,
    
    /// <summary>JSON escaping for safe JSON serialization.</summary>
    JsonEscape,
    
    /// <summary>Allow only alphanumeric characters.</summary>
    Alphanumeric
}

/// <summary>
/// Represents a secure audit trail entry processed within an enclave.
/// </summary>
public class SecureAuditEntry
{
    /// <summary>Gets or sets the unique identifier for the audit entry.</summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the timestamp when the operation occurred.</summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>Gets or sets the operation that was performed.</summary>
    public string Operation { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the user ID who performed the operation.</summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>Gets or sets the resource ID that was accessed.</summary>
    public string ResourceId { get; set; } = string.Empty;
    
    /// <summary>Gets or sets additional metadata for the audit entry.</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>Gets or sets the secure signature of the audit entry.</summary>
    public string Signature { get; set; } = string.Empty;
} 
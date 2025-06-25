using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace NeoServiceLayer.Tests.Common;

/// <summary>
/// Provides centralized test configuration for all test projects.
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// The JWT secret key used for testing. Must be at least 32 characters long.
    /// </summary>
    public const string TestJwtSecretKey = "test-jwt-secret-key-for-integration-tests-only-2024";

    /// <summary>
    /// Sets up environment variables for test execution.
    /// </summary>
    public static void SetupTestEnvironment()
    {
        // Set JWT secret key for authentication tests
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", TestJwtSecretKey);

        // Set SGX mode to simulation for tests
        Environment.SetEnvironmentVariable("SGX_MODE", "SIM");
        Environment.SetEnvironmentVariable("SGX_DEBUG", "1");
        Environment.SetEnvironmentVariable("NEO_ALLOW_SGX_SIMULATION", "true");

        // Set environment to test
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    /// <summary>
    /// Creates a test configuration with default values.
    /// </summary>
    /// <returns>The test configuration.</returns>
    public static IConfiguration CreateTestConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            // JWT Configuration
            ["JwtSettings:SecretKey"] = TestJwtSecretKey,
            ["JwtSettings:Issuer"] = "NeoServiceLayer.Test",
            ["JwtSettings:Audience"] = "NeoServiceLayer.Test",
            ["JwtSettings:ExpirationInMinutes"] = "60",

            // TEE Configuration
            ["Tee:EnclaveType"] = "SGX",
            ["Tee:AttestationServiceUrl"] = "https://test-attestation.example.com",
            ["Tee:EnableRemoteAttestation"] = "false",

            // Enclave Configuration
            ["Enclave:SGXMode"] = "SIM",
            ["Enclave:EnableDebug"] = "true",
            ["Enclave:OcclumVersion"] = "0.29.6",
            ["Enclave:Cryptography:EncryptionAlgorithm"] = "AES-256-GCM",
            ["Enclave:Cryptography:SigningAlgorithm"] = "secp256k1",
            ["Enclave:Cryptography:KeySize"] = "256",
            ["Enclave:Cryptography:EnableHardwareRNG"] = "true",
            ["Enclave:Storage:EnableCompression"] = "true",
            ["Enclave:Storage:EnableIntegrityCheck"] = "true",
            ["Enclave:Network:MaxConnections"] = "100",
            ["Enclave:Network:RequestTimeout"] = "00:00:30",
            ["Enclave:JavaScript:MaxExecutionTime"] = "00:00:05",
            ["Enclave:JavaScript:MaxMemoryUsage"] = "67108864",
            ["Enclave:Performance:EnableMetrics"] = "true",

            // Blockchain Configuration
            ["Blockchain:Neo:RpcUrl"] = "http://localhost:20332",
            ["Blockchain:Neo:Network"] = "TestNet",
            ["Blockchain:NeoX:RpcUrl"] = "http://localhost:8545",
            ["Blockchain:NeoX:ChainId"] = "12227332",

            // Service Configuration
            ["ServiceDefaults:MaxRetries"] = "3",
            ["ServiceDefaults:TimeoutSeconds"] = "30",
            ["ServiceDefaults:EnableCaching"] = "true",

            // Test-specific settings
            ["Testing:EnableDetailedLogs"] = "true",
            ["Testing:MockExternalServices"] = "true"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    /// <summary>
    /// Creates a minimal test configuration for unit tests.
    /// </summary>
    /// <returns>The minimal test configuration.</returns>
    public static IConfiguration CreateMinimalTestConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = TestJwtSecretKey,
            ["Enclave:SGXMode"] = "SIM",
            ["Enclave:EnableDebug"] = "true"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using FluentAssertions;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
using System.Text;

namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Integration tests for enclave services to verify proper registration and functionality.
/// </summary>
[TestFixture]
public class EnclaveServiceIntegrationTests
{
    private ServiceProvider _serviceProvider = null!;
    private IConfiguration _configuration = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Set up test configuration
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tee:EnclaveType"] = "SGX",
                ["Tee:AttestationServiceUrl"] = "https://test-attestation.example.com",
                ["Tee:EnableRemoteAttestation"] = "false",
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
                ["Enclave:Performance:EnableMetrics"] = "true"
            });

        _configuration = configurationBuilder.Build();

        // Set environment variables for SGX simulation mode
        Environment.SetEnvironmentVariable("SGX_MODE", "SIM");
        Environment.SetEnvironmentVariable("SGX_DEBUG", "1");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");

        // Build service collection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Add configuration
        services.AddSingleton(_configuration);
        
        // Add Neo Service Layer (which should include TEE services)
        services.AddNeoServiceLayer(_configuration);

        _serviceProvider = services.BuildServiceProvider();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    public void ServiceProvider_ShouldRegisterIEnclaveWrapper()
    {
        // Act
        var enclaveWrapper = _serviceProvider.GetService<IEnclaveWrapper>();

        // Assert
        enclaveWrapper.Should().NotBeNull("IEnclaveWrapper should be registered");
        enclaveWrapper.Should().BeOfType<ProductionSGXEnclaveWrapper>("Should use ProductionSGXEnclaveWrapper in simulation mode");
    }

    [Test]
    public void ServiceProvider_ShouldRegisterEnclaveManager()
    {
        // Act
        var enclaveManager = _serviceProvider.GetService<EnclaveManager>();

        // Assert
        enclaveManager.Should().NotBeNull("EnclaveManager should be registered");
    }

    [Test]
    public async Task EnclaveWrapper_ShouldInitializeSuccessfully()
    {
        // Arrange
        var enclaveWrapper = _serviceProvider.GetRequiredService<IEnclaveWrapper>();
        var config = new EnclaveConfig
        {
            SGXMode = "SIM",
            EnableDebug = true,
            Cryptography = new CryptographyConfig
            {
                EncryptionAlgorithm = "AES-256-GCM",
                SigningAlgorithm = "secp256k1"
            }
        };

        // Act
        var result = await enclaveWrapper.InitializeAsync(config);

        // Assert
        result.Should().BeTrue("Enclave should initialize successfully in simulation mode");
    }

    [Test]
    public async Task EnclaveWrapper_ShouldSealAndUnsealData()
    {
        // Arrange
        var enclaveWrapper = _serviceProvider.GetRequiredService<IEnclaveWrapper>();
        await InitializeEnclaveAsync(enclaveWrapper);

        var originalData = Encoding.UTF8.GetBytes("This is test data for sealing");
        var keyId = "test-seal-key";

        // Act
        var sealedData = await enclaveWrapper.SealDataAsync(originalData, keyId);
        var unsealedData = await enclaveWrapper.UnsealDataAsync(sealedData, keyId);

        // Assert
        sealedData.Should().NotBeNull("Sealed data should not be null");
        sealedData.EncryptedData.Should().NotBeNullOrEmpty("Encrypted data should not be empty");
        unsealedData.Should().Equal(originalData, "Unsealed data should match original data");
    }

    [Test]
    public async Task EnclaveWrapper_ShouldSignAndVerifyData()
    {
        // Arrange
        var enclaveWrapper = _serviceProvider.GetRequiredService<IEnclaveWrapper>();
        await InitializeEnclaveAsync(enclaveWrapper);

        var dataToSign = Encoding.UTF8.GetBytes("Message to sign for testing");
        var keyId = "test-signing-key";

        // Act
        var signatureResult = await enclaveWrapper.SignDataAsync(dataToSign, keyId);
        var isValid = await enclaveWrapper.VerifySignatureAsync(dataToSign, signatureResult.Signature, signatureResult.PublicKey);

        // Assert
        signatureResult.Should().NotBeNull("Signature result should not be null");
        signatureResult.Signature.Should().NotBeNullOrEmpty("Signature should not be empty");
        signatureResult.PublicKey.Should().NotBeNullOrEmpty("Public key should not be empty");
        isValid.Should().BeTrue("Signature should be valid");
    }

    [Test]
    public async Task EnclaveWrapper_ShouldGenerateAttestation()
    {
        // Arrange
        var enclaveWrapper = _serviceProvider.GetRequiredService<IEnclaveWrapper>();
        await InitializeEnclaveAsync(enclaveWrapper);

        // Act
        var attestation = await enclaveWrapper.GetAttestationAsync();

        // Assert
        attestation.Should().NotBeNull("Attestation should not be null");
        attestation.Quote.Should().NotBeNullOrEmpty("Attestation quote should not be empty");
        attestation.PlatformSecurityVersion.Should().BeGreaterThan(0, "Platform security version should be valid");
    }

    [Test]
    public async Task EnclaveWrapper_ShouldExecuteSecureHttpRequest()
    {
        // Arrange
        var enclaveWrapper = _serviceProvider.GetRequiredService<IEnclaveWrapper>();
        await InitializeEnclaveAsync(enclaveWrapper);

        var request = new SecureHttpRequest
        {
            Url = "https://httpbin.org/get",
            Method = "GET",
            Headers = new Dictionary<string, string>
            {
                ["User-Agent"] = "Neo-Service-Layer-Test"
            },
            DomainValidation = new DomainValidationConfig
            {
                AllowedDomains = new[] { "httpbin.org" },
                RequireHttps = true,
                ValidateCertificate = true
            }
        };

        // Act
        var response = await enclaveWrapper.SecureHttpRequestAsync(request);

        // Assert
        response.Should().NotBeNull("HTTP response should not be null");
        response.Success.Should().BeTrue("HTTP request should succeed");
        response.Data.Should().NotBeNullOrEmpty("Response data should not be empty");
        response.StatusCode.Should().Be(200, "Should return OK status");
    }

    [Test]
    public async Task EnclaveWrapper_ShouldExecuteJavaScript()
    {
        // Arrange
        var enclaveWrapper = _serviceProvider.GetRequiredService<IEnclaveWrapper>();
        await InitializeEnclaveAsync(enclaveWrapper);

        var jsCode = @"
            function calculate(input) {
                return { result: input.value * 2, timestamp: Date.now() };
            }
            calculate(input);
        ";

        var context = new JavaScriptContext
        {
            Input = new { value = 42 },
            Timeout = TimeSpan.FromSeconds(5),
            MemoryLimit = 64 * 1024 * 1024,
            SecurityConstraints = new JavaScriptSecurityConstraints
            {
                DisallowNetworking = true,
                DisallowFileSystem = true,
                DisallowProcessExecution = true
            }
        };

        // Act
        var result = await enclaveWrapper.ExecuteJavaScriptAsync(jsCode, context);

        // Assert
        result.Should().NotBeNull("JavaScript result should not be null");
        result.Success.Should().BeTrue("JavaScript execution should succeed");
        result.Result.Should().NotBeNull("JavaScript result data should not be null");
    }

    [Test]
    public async Task EnclaveWrapper_ShouldProcessAbstractAccount()
    {
        // Arrange
        var enclaveWrapper = _serviceProvider.GetRequiredService<IEnclaveWrapper>();
        await InitializeEnclaveAsync(enclaveWrapper);

        var accountRequest = new AbstractAccountRequest
        {
            AccountId = "test-account-123",
            Operation = AbstractAccountOperation.ExecuteTransaction,
            TransactionData = Encoding.UTF8.GetBytes("test transaction data"),
            GuardianApprovals = new List<string> { "guardian1-signature", "guardian2-signature" },
            SecurityPolicy = new AbstractAccountSecurityPolicy
            {
                RequiredApprovals = 2,
                TimeoutMinutes = 30,
                AllowedOperations = new[] { "transfer", "stake" }
            }
        };

        // Act
        var result = await enclaveWrapper.ProcessAbstractAccountAsync(accountRequest);

        // Assert
        result.Should().NotBeNull("Abstract account result should not be null");
        result.Success.Should().BeTrue("Abstract account processing should succeed");
        result.TransactionHash.Should().NotBeNullOrEmpty("Transaction hash should be generated");
    }

    [Test]
    public async Task EnclaveServices_ShouldWorkTogether()
    {
        // Arrange
        var enclaveWrapper = _serviceProvider.GetRequiredService<IEnclaveWrapper>();
        var enclaveManager = _serviceProvider.GetRequiredService<EnclaveManager>();
        
        await InitializeEnclaveAsync(enclaveWrapper);

        // Act & Assert - Test complete workflow
        
        // 1. Initialize enclave manager
        await enclaveManager.StartAsync(CancellationToken.None);
        
        // 2. Seal some data
        var testData = Encoding.UTF8.GetBytes("Integration test data");
        var sealedData = await enclaveWrapper.SealDataAsync(testData, "integration-test-key");
        
        // 3. Generate a signature
        var signature = await enclaveWrapper.SignDataAsync(testData, "integration-sign-key");
        
        // 4. Verify signature
        var isValid = await enclaveWrapper.VerifySignatureAsync(testData, signature.Signature, signature.PublicKey);
        
        // 5. Unseal data
        var unsealedData = await enclaveWrapper.UnsealDataAsync(sealedData, "integration-test-key");
        
        // 6. Get attestation
        var attestation = await enclaveWrapper.GetAttestationAsync();

        // Assert all operations succeeded
        sealedData.Should().NotBeNull();
        signature.Should().NotBeNull();
        isValid.Should().BeTrue();
        unsealedData.Should().Equal(testData);
        attestation.Should().NotBeNull();
        
        // 7. Stop enclave manager
        await enclaveManager.StopAsync(CancellationToken.None);
    }

    [Test]
    public void ServiceRegistration_ShouldFollowProperLifetimes()
    {
        // Arrange & Act
        var enclaveWrapper1 = _serviceProvider.GetService<IEnclaveWrapper>();
        var enclaveWrapper2 = _serviceProvider.GetService<IEnclaveWrapper>();
        var enclaveManager1 = _serviceProvider.GetService<EnclaveManager>();
        var enclaveManager2 = _serviceProvider.GetService<EnclaveManager>();

        // Assert
        enclaveWrapper1.Should().BeSameAs(enclaveWrapper2, "IEnclaveWrapper should be registered as singleton");
        enclaveManager1.Should().BeSameAs(enclaveManager2, "EnclaveManager should be registered as singleton");
    }

    [Test]
    public async Task EnclaveWrapper_ShouldHandleDisposalProperly()
    {
        // Arrange
        var enclaveWrapper = _serviceProvider.GetRequiredService<IEnclaveWrapper>();
        await InitializeEnclaveAsync(enclaveWrapper);

        // Act & Assert - Should not throw when disposing
        var disposing = () => enclaveWrapper.Dispose();
        disposing.Should().NotThrow("Enclave disposal should be handled gracefully");
    }

    /// <summary>
    /// Helper method to initialize enclave with test configuration.
    /// </summary>
    private static async Task InitializeEnclaveAsync(IEnclaveWrapper enclaveWrapper)
    {
        var config = new EnclaveConfig
        {
            SGXMode = "SIM",
            EnableDebug = true,
            OcclumVersion = "0.29.6",
            Cryptography = new CryptographyConfig
            {
                EncryptionAlgorithm = "AES-256-GCM",
                SigningAlgorithm = "secp256k1",
                KeySize = 256,
                EnableHardwareRNG = true
            },
            Storage = new StorageConfig
            {
                EnableCompression = true,
                EnableIntegrityCheck = true,
                MaxFileSize = 104857600,
                EncryptionEnabled = true
            },
            Network = new NetworkConfig
            {
                MaxConnections = 100,
                RequestTimeout = TimeSpan.FromSeconds(30),
                DomainValidation = new DomainValidationConfig
                {
                    AllowedDomains = new[] { "httpbin.org", "example.com" },
                    RequireHttps = true,
                    ValidateCertificate = true
                }
            },
            JavaScript = new JavaScriptConfig
            {
                MaxExecutionTime = TimeSpan.FromSeconds(5),
                MaxMemoryUsage = 64 * 1024 * 1024,
                SecurityConstraints = new JavaScriptSecurityConstraints
                {
                    DisallowNetworking = true,
                    DisallowFileSystem = true,
                    DisallowProcessExecution = true
                }
            },
            Performance = new PerformanceConfig
            {
                EnableMetrics = true,
                MetricsInterval = TimeSpan.FromMinutes(1),
                EnableBenchmarking = false,
                ExpectedBaselineMs = 100
            }
        };

        var result = await enclaveWrapper.InitializeAsync(config);
        if (!result)
        {
            throw new InvalidOperationException("Failed to initialize enclave for testing");
        }
    }
} 
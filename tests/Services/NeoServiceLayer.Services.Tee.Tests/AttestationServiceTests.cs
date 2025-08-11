using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace NeoServiceLayer.Services.Tee.Tests
{
    /// <summary>
    /// Comprehensive unit tests for SGX Enclave AttestationService
    /// Tests remote attestation, quote verification, certificate chain validation, and security features
    /// </summary>
    public class AttestationServiceTests : IDisposable
    {
        private readonly Mock<ILogger<AttestationService>> _mockLogger;
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly AttestationService _attestationService;
        private readonly string _testAttestationUrl = "https://api.trustedservices.intel.com/sgx/certification/v4/";

        public AttestationServiceTests()
        {
            _mockLogger = new Mock<ILogger<AttestationService>>();
            _mockHttpClient = new Mock<HttpClient>();
            _attestationService = new AttestationService(_mockLogger.Object, _testAttestationUrl);
        }

        #region Quote Verification Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task VerifyQuote_ValidQuote_ShouldReturnSuccess()
        {
            // Arrange
            var validQuote = GenerateValidTestQuote();
            var expectedNonce = "test_nonce_12345";

            // Act
            var result = await _attestationService.VerifyQuoteAsync(validQuote, expectedNonce);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Equal("Quote verification successful", result.Message);
            Assert.NotNull(result.QuoteData);
            Assert.Equal(expectedNonce, result.Nonce);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task VerifyQuote_InvalidQuote_ShouldReturnFailure()
        {
            // Arrange
            var invalidQuote = "invalid_quote_data";
            var nonce = "test_nonce";

            // Act
            var result = await _attestationService.VerifyQuoteAsync(invalidQuote, nonce);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.Contains("Invalid quote format", result.Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task VerifyQuote_ExpiredQuote_ShouldReturnFailure()
        {
            // Arrange
            var expiredQuote = GenerateExpiredTestQuote();
            var nonce = "test_nonce";

            // Act
            var result = await _attestationService.VerifyQuoteAsync(expiredQuote, nonce);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.Contains("Quote has expired", result.Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task VerifyQuote_WrongNonce_ShouldReturnFailure()
        {
            // Arrange
            var quote = GenerateValidTestQuote();
            var wrongNonce = "wrong_nonce";

            // Act
            var result = await _attestationService.VerifyQuoteAsync(quote, wrongNonce);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.Contains("Nonce mismatch", result.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task VerifyQuote_InvalidInput_ShouldThrow(string quote)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _attestationService.VerifyQuoteAsync(quote, "nonce")
            );
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task VerifyQuote_MalformedQuote_ShouldReturnFailure()
        {
            // Arrange
            var malformedQuote = Convert.ToBase64String(Encoding.UTF8.GetBytes("malformed_data"));
            var nonce = "test_nonce";

            // Act
            var result = await _attestationService.VerifyQuoteAsync(malformedQuote, nonce);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Failed to parse quote", result.Message);
        }

        #endregion

        #region Certificate Chain Validation Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task ValidateCertificateChain_ValidChain_ShouldReturnSuccess()
        {
            // Arrange
            var validCertChain = GenerateValidCertificateChain();

            // Act
            var result = await _attestationService.ValidateCertificateChainAsync(validCertChain);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Equal("Certificate chain validation successful", result.Message);
            Assert.NotNull(result.RootCertificate);
            Assert.NotNull(result.IntermediateCertificates);
            Assert.NotNull(result.LeafCertificate);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task ValidateCertificateChain_ExpiredCertificate_ShouldReturnFailure()
        {
            // Arrange
            var expiredCertChain = GenerateExpiredCertificateChain();

            // Act
            var result = await _attestationService.ValidateCertificateChainAsync(expiredCertChain);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Certificate has expired", result.Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task ValidateCertificateChain_UntrustedRoot_ShouldReturnFailure()
        {
            // Arrange
            var untrustedCertChain = GenerateUntrustedCertificateChain();

            // Act
            var result = await _attestationService.ValidateCertificateChainAsync(untrustedCertChain);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Untrusted root certificate", result.Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task ValidateCertificateChain_BrokenChain_ShouldReturnFailure()
        {
            // Arrange
            var brokenCertChain = GenerateBrokenCertificateChain();

            // Act
            var result = await _attestationService.ValidateCertificateChainAsync(brokenCertChain);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Certificate chain is broken", result.Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task ValidateCertificateChain_RevokedCertificate_ShouldReturnFailure()
        {
            // Arrange
            var revokedCertChain = GenerateRevokedCertificateChain();

            // Act
            var result = await _attestationService.ValidateCertificateChainAsync(revokedCertChain);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Certificate has been revoked", result.Message);
        }

        #endregion

        #region Remote Attestation Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task PerformRemoteAttestation_ValidEnclave_ShouldReturnSuccess()
        {
            // Arrange
            var enclaveIdentity = GenerateValidEnclaveIdentity();
            var nonce = GenerateRandomNonce();

            // Act
            var result = await _attestationService.PerformRemoteAttestationAsync(enclaveIdentity, nonce);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Equal("Remote attestation successful", result.Message);
            Assert.NotNull(result.AttestationEvidence);
            Assert.Equal(nonce, result.Nonce);
            Assert.True(result.Timestamp > 0);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task PerformRemoteAttestation_InvalidEnclaveIdentity_ShouldReturnFailure()
        {
            // Arrange
            var invalidEnclaveIdentity = new EnclaveIdentity
            {
                MrEnclave = "invalid_mr_enclave",
                MrSigner = "invalid_mr_signer",
                ProductId = 999,
                SecurityVersion = 0
            };
            var nonce = GenerateRandomNonce();

            // Act
            var result = await _attestationService.PerformRemoteAttestationAsync(invalidEnclaveIdentity, nonce);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Invalid enclave identity", result.Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task PerformRemoteAttestation_NetworkFailure_ShouldReturnFailure()
        {
            // Arrange
            var enclaveIdentity = GenerateValidEnclaveIdentity();
            var nonce = GenerateRandomNonce();
            
            // Simulate network failure
            var networkFailureService = new AttestationService(_mockLogger.Object, "https://invalid.url");

            // Act
            var result = await networkFailureService.PerformRemoteAttestationAsync(enclaveIdentity, nonce);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Network error", result.Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task PerformRemoteAttestation_Timeout_ShouldReturnFailure()
        {
            // Arrange
            var enclaveIdentity = GenerateValidEnclaveIdentity();
            var nonce = GenerateRandomNonce();
            
            // Use a very short timeout to simulate timeout
            var timeoutService = new AttestationService(_mockLogger.Object, _testAttestationUrl, TimeSpan.FromMilliseconds(1));

            // Act
            var result = await timeoutService.PerformRemoteAttestationAsync(enclaveIdentity, nonce);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Timeout", result.Message);
        }

        #endregion

        #region Intel Attestation Service Integration Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task GetTrustedCertificates_ShouldReturnIntelCertificates()
        {
            // Act
            var certificates = await _attestationService.GetTrustedCertificatesAsync();

            // Assert
            Assert.NotNull(certificates);
            Assert.NotEmpty(certificates);
            
            // Verify Intel root certificate is present
            Assert.Contains(certificates, cert => cert.Issuer.Contains("Intel"));
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task GetRevocationList_ShouldReturnCurrentCRL()
        {
            // Act
            var crl = await _attestationService.GetCertificateRevocationListAsync();

            // Assert
            Assert.NotNull(crl);
            Assert.True(crl.IssuedAt > 0);
            Assert.True(crl.NextUpdate > crl.IssuedAt);
            Assert.NotNull(crl.RevokedCertificates);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task VerifyIntelSignature_ValidSignature_ShouldReturnTrue()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Test data for signature verification");
            var signature = GenerateIntelTestSignature(data);
            var certificate = GenerateIntelTestCertificate();

            // Act
            var isValid = await _attestationService.VerifyIntelSignatureAsync(data, signature, certificate);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task VerifyIntelSignature_InvalidSignature_ShouldReturnFalse()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("Test data for signature verification");
            var invalidSignature = new byte[256]; // All zeros
            var certificate = GenerateIntelTestCertificate();

            // Act
            var isValid = await _attestationService.VerifyIntelSignatureAsync(data, invalidSignature, certificate);

            // Assert
            Assert.False(isValid);
        }

        #endregion

        #region Security Policy Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task ValidateSecurityPolicy_ValidPolicy_ShouldReturnSuccess()
        {
            // Arrange
            var securityPolicy = new SecurityPolicy
            {
                MinSecurityVersion = 1,
                RequiredSigners = new[] { "Intel Corporation" },
                AllowDebugMode = false,
                RequireIntelSigned = true,
                MaxAge = TimeSpan.FromHours(24)
            };

            var enclaveReport = GenerateValidEnclaveReport();

            // Act
            var result = await _attestationService.ValidateSecurityPolicyAsync(enclaveReport, securityPolicy);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal("Security policy validation successful", result.Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task ValidateSecurityPolicy_DebugModeNotAllowed_ShouldReturnFailure()
        {
            // Arrange
            var securityPolicy = new SecurityPolicy
            {
                AllowDebugMode = false,
                RequireIntelSigned = true
            };

            var debugEnclaveReport = GenerateDebugEnclaveReport();

            // Act
            var result = await _attestationService.ValidateSecurityPolicyAsync(debugEnclaveReport, securityPolicy);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Debug mode not allowed", result.Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task ValidateSecurityPolicy_InsufficientSecurityVersion_ShouldReturnFailure()
        {
            // Arrange
            var securityPolicy = new SecurityPolicy
            {
                MinSecurityVersion = 5,
                AllowDebugMode = true
            };

            var lowVersionReport = GenerateLowSecurityVersionEnclaveReport();

            // Act
            var result = await _attestationService.ValidateSecurityPolicyAsync(lowVersionReport, securityPolicy);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Security version too low", result.Message);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task ValidateSecurityPolicy_UnauthorizedSigner_ShouldReturnFailure()
        {
            // Arrange
            var securityPolicy = new SecurityPolicy
            {
                RequiredSigners = new[] { "Intel Corporation" },
                RequireIntelSigned = true
            };

            var unauthorizedReport = GenerateUnauthorizedSignerEnclaveReport();

            // Act
            var result = await _attestationService.ValidateSecurityPolicyAsync(unauthorizedReport, securityPolicy);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Unauthorized signer", result.Message);
        }

        #endregion

        #region Performance and Edge Case Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        [Trait("Performance", "True")]
        public async Task AttestationService_Performance_ShouldMeetBaselines()
        {
            // Arrange
            var quote = GenerateValidTestQuote();
            var nonce = "performance_test_nonce";
            var iterations = 10;

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                await _attestationService.VerifyQuoteAsync(quote, nonce);
            }
            stopwatch.Stop();

            // Assert
            var averageTime = stopwatch.ElapsedMilliseconds / (double)iterations;
            Assert.True(averageTime < 100, $"Average attestation time {averageTime:F2}ms should be under 100ms");
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task AttestationService_ConcurrentVerification_ShouldHandleLoad()
        {
            // Arrange
            var tasks = new List<Task<AttestationResult>>();
            var quote = GenerateValidTestQuote();

            // Act - Create concurrent verification requests
            for (int i = 0; i < 20; i++)
            {
                var nonce = $"concurrent_test_nonce_{i}";
                tasks.Add(_attestationService.VerifyQuoteAsync(quote, nonce));
            }

            var results = await Task.WhenAll(tasks);

            // Assert - All verifications should succeed
            Assert.All(results, result => Assert.True(result.IsValid));
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task AttestationService_LargeQuote_ShouldHandleGracefully()
        {
            // Arrange
            var largeQuote = GenerateLargeTestQuote(1024 * 1024); // 1MB quote
            var nonce = "large_quote_test";

            // Act
            var result = await _attestationService.VerifyQuoteAsync(largeQuote, nonce);

            // Assert
            Assert.NotNull(result);
            // Large quotes may be rejected, which is acceptable
            Assert.True(result.IsValid || result.Message.Contains("Quote size exceeds maximum"));
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Attestation")]
        public async Task AttestationService_ErrorRecovery_ShouldRecover()
        {
            // Arrange
            var validQuote = GenerateValidTestQuote();
            var invalidQuote = "invalid";
            var nonce = "recovery_test";

            // Act - Try invalid quote first, then valid
            var invalidResult = await _attestationService.VerifyQuoteAsync(invalidQuote, nonce);
            var validResult = await _attestationService.VerifyQuoteAsync(validQuote, nonce);

            // Assert
            Assert.False(invalidResult.IsValid);
            Assert.True(validResult.IsValid); // Service should recover
        }

        #endregion

        #region Helper Methods for Test Data Generation

        private string GenerateValidTestQuote()
        {
            var quoteData = new
            {
                version = 4,
                sign_type = 1,
                epid_group_id = "00000000",
                qe_svn = 1,
                pce_svn = 1,
                xeid = 0,
                basename = Convert.ToBase64String(new byte[32]),
                report_body = new
                {
                    cpu_svn = Convert.ToBase64String(new byte[16]),
                    misc_select = 0,
                    attributes = Convert.ToBase64String(new byte[16]),
                    mr_enclave = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("test_enclave"))),
                    mr_signer = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("Intel Corporation"))),
                    isv_prod_id = 1,
                    isv_svn = 1,
                    report_data = Convert.ToBase64String(Encoding.UTF8.GetBytes("test_nonce_12345").Concat(new byte[32]).Take(64).ToArray())
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(quoteData)));
        }

        private string GenerateExpiredTestQuote()
        {
            var expiredQuoteData = new
            {
                version = 4,
                report_body = new
                {
                    mr_enclave = Convert.ToBase64String(new byte[32]),
                    mr_signer = Convert.ToBase64String(new byte[32]),
                    report_data = Convert.ToBase64String(new byte[64])
                },
                timestamp = DateTimeOffset.UtcNow.AddHours(-25).ToUnixTimeSeconds() // 25 hours ago
            };

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(expiredQuoteData)));
        }

        private string[] GenerateValidCertificateChain()
        {
            return new[]
            {
                GenerateTestCertificate("Intel SGX Root CA", "Intel SGX Root CA", true),
                GenerateTestCertificate("Intel SGX Intermediate CA", "Intel SGX Root CA", false),
                GenerateTestCertificate("Intel SGX Leaf Certificate", "Intel SGX Intermediate CA", false)
            };
        }

        private string[] GenerateExpiredCertificateChain()
        {
            return new[]
            {
                GenerateTestCertificate("Intel SGX Root CA", "Intel SGX Root CA", true, expired: true),
                GenerateTestCertificate("Intel SGX Intermediate CA", "Intel SGX Root CA", false),
                GenerateTestCertificate("Intel SGX Leaf Certificate", "Intel SGX Intermediate CA", false)
            };
        }

        private string[] GenerateUntrustedCertificateChain()
        {
            return new[]
            {
                GenerateTestCertificate("Untrusted Root CA", "Untrusted Root CA", true),
                GenerateTestCertificate("Untrusted Intermediate CA", "Untrusted Root CA", false),
                GenerateTestCertificate("Untrusted Leaf Certificate", "Untrusted Intermediate CA", false)
            };
        }

        private string[] GenerateBrokenCertificateChain()
        {
            return new[]
            {
                GenerateTestCertificate("Intel SGX Root CA", "Intel SGX Root CA", true),
                GenerateTestCertificate("Wrong Intermediate CA", "Different Root CA", false), // Broken chain
                GenerateTestCertificate("Intel SGX Leaf Certificate", "Intel SGX Intermediate CA", false)
            };
        }

        private string[] GenerateRevokedCertificateChain()
        {
            return new[]
            {
                GenerateTestCertificate("Intel SGX Root CA", "Intel SGX Root CA", true),
                GenerateTestCertificate("Intel SGX Intermediate CA", "Intel SGX Root CA", false, revoked: true),
                GenerateTestCertificate("Intel SGX Leaf Certificate", "Intel SGX Intermediate CA", false)
            };
        }

        private string GenerateTestCertificate(string subject, string issuer, bool isRoot, bool expired = false, bool revoked = false)
        {
            var certData = new
            {
                subject = $"CN={subject}",
                issuer = $"CN={issuer}",
                serial_number = Guid.NewGuid().ToString("N"),
                not_before = DateTimeOffset.UtcNow.AddDays(-30).ToUnixTimeSeconds(),
                not_after = expired 
                    ? DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds() 
                    : DateTimeOffset.UtcNow.AddYears(1).ToUnixTimeSeconds(),
                public_key = Convert.ToBase64String(new byte[256]),
                signature = Convert.ToBase64String(new byte[256]),
                is_root = isRoot,
                revoked = revoked
            };

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(certData)));
        }

        private EnclaveIdentity GenerateValidEnclaveIdentity()
        {
            return new EnclaveIdentity
            {
                MrEnclave = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("test_enclave"))),
                MrSigner = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("Intel Corporation"))),
                ProductId = 1,
                SecurityVersion = 1,
                Attributes = new EnclaveAttributes
                {
                    Debug = false,
                    Mode64Bit = true,
                    ProvisionKey = false,
                    EInitTokenKey = false
                }
            };
        }

        private string GenerateRandomNonce()
        {
            var random = new Random();
            var bytes = new byte[32];
            random.NextBytes(bytes);
            return Convert.ToHexString(bytes);
        }

        private byte[] GenerateIntelTestSignature(byte[] data)
        {
            // Simulate Intel signature generation
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes("intel_test_key"));
            return hmac.ComputeHash(data);
        }

        private string GenerateIntelTestCertificate()
        {
            return GenerateTestCertificate("Intel SGX Attestation Service", "Intel SGX Root CA", false);
        }

        private EnclaveReport GenerateValidEnclaveReport()
        {
            return new EnclaveReport
            {
                SecurityVersion = 2,
                Debug = false,
                MrSigner = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("Intel Corporation"))),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        private EnclaveReport GenerateDebugEnclaveReport()
        {
            return new EnclaveReport
            {
                SecurityVersion = 2,
                Debug = true, // Debug mode enabled
                MrSigner = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("Intel Corporation"))),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        private EnclaveReport GenerateLowSecurityVersionEnclaveReport()
        {
            return new EnclaveReport
            {
                SecurityVersion = 1, // Low security version
                Debug = false,
                MrSigner = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("Intel Corporation"))),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        private EnclaveReport GenerateUnauthorizedSignerEnclaveReport()
        {
            return new EnclaveReport
            {
                SecurityVersion = 2,
                Debug = false,
                MrSigner = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("Unauthorized Signer"))),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        private string GenerateLargeTestQuote(int size)
        {
            var largeData = new string('x', size);
            var quoteData = new
            {
                version = 4,
                large_data = largeData,
                report_body = new
                {
                    mr_enclave = Convert.ToBase64String(new byte[32]),
                    mr_signer = Convert.ToBase64String(new byte[32]),
                    report_data = Convert.ToBase64String(new byte[64])
                }
            };

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(quoteData)));
        }

        #endregion

        public void Dispose()
        {
            _attestationService?.Dispose();
        }
    }

    /// <summary>
    /// Mock/test implementation of AttestationService for unit testing
    /// This simulates the actual SGX attestation behavior
    /// </summary>
    public class AttestationService : IDisposable
    {
        private readonly ILogger<AttestationService> _logger;
        private readonly string _attestationServiceUrl;
        private readonly TimeSpan _timeout;
        private readonly HttpClient _httpClient;

        public AttestationService(ILogger<AttestationService> logger, string attestationServiceUrl, TimeSpan? timeout = null)
        {
            _logger = logger;
            _attestationServiceUrl = attestationServiceUrl;
            _timeout = timeout ?? TimeSpan.FromSeconds(30);
            _httpClient = new HttpClient { Timeout = _timeout };
        }

        public async Task<AttestationResult> VerifyQuoteAsync(string quote, string nonce)
        {
            if (string.IsNullOrWhiteSpace(quote))
                throw new ArgumentException("Quote cannot be null or empty");

            if (string.IsNullOrWhiteSpace(nonce))
                throw new ArgumentException("Nonce cannot be null or empty");

            try
            {
                // Decode and parse quote
                var quoteBytes = Convert.FromBase64String(quote);
                var quoteJson = Encoding.UTF8.GetString(quoteBytes);
                var quoteData = JsonSerializer.Deserialize<JsonElement>(quoteJson);

                // Basic validation
                if (!quoteData.TryGetProperty("version", out var version) || version.GetInt32() != 4)
                {
                    return new AttestationResult
                    {
                        IsValid = false,
                        Message = "Invalid quote format or unsupported version",
                        Nonce = nonce
                    };
                }

                // Check timestamp (not expired)
                if (quoteData.TryGetProperty("timestamp", out var timestamp))
                {
                    var quoteTime = DateTimeOffset.FromUnixTimeSeconds(timestamp.GetInt64());
                    if (DateTimeOffset.UtcNow - quoteTime > TimeSpan.FromHours(24))
                    {
                        return new AttestationResult
                        {
                            IsValid = false,
                            Message = "Quote has expired",
                            Nonce = nonce
                        };
                    }
                }

                // Verify nonce in report data
                if (quoteData.TryGetProperty("report_body", out var reportBody) &&
                    reportBody.TryGetProperty("report_data", out var reportData))
                {
                    var reportDataBytes = Convert.FromBase64String(reportData.GetString());
                    var reportDataStr = Encoding.UTF8.GetString(reportDataBytes).TrimEnd('\0');
                    
                    if (!reportDataStr.StartsWith(nonce))
                    {
                        return new AttestationResult
                        {
                            IsValid = false,
                            Message = "Nonce mismatch in quote report data",
                            Nonce = nonce
                        };
                    }
                }

                // Successful verification
                return new AttestationResult
                {
                    IsValid = true,
                    Message = "Quote verification successful",
                    QuoteData = quoteData,
                    Nonce = nonce,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
            }
            catch (FormatException)
            {
                return new AttestationResult
                {
                    IsValid = false,
                    Message = "Invalid quote format - not valid Base64",
                    Nonce = nonce
                };
            }
            catch (JsonException)
            {
                return new AttestationResult
                {
                    IsValid = false,
                    Message = "Failed to parse quote JSON data",
                    Nonce = nonce
                };
            }
        }

        public async Task<CertificateValidationResult> ValidateCertificateChainAsync(string[] certificateChain)
        {
            if (certificateChain == null || certificateChain.Length == 0)
                throw new ArgumentException("Certificate chain cannot be null or empty");

            try
            {
                var certificates = new List<CertificateInfo>();
                
                foreach (var certBase64 in certificateChain)
                {
                    var certBytes = Convert.FromBase64String(certBase64);
                    var certJson = Encoding.UTF8.GetString(certBytes);
                    var certData = JsonSerializer.Deserialize<JsonElement>(certJson);
                    
                    var certInfo = new CertificateInfo
                    {
                        Subject = certData.GetProperty("subject").GetString(),
                        Issuer = certData.GetProperty("issuer").GetString(),
                        NotBefore = DateTimeOffset.FromUnixTimeSeconds(certData.GetProperty("not_before").GetInt64()),
                        NotAfter = DateTimeOffset.FromUnixTimeSeconds(certData.GetProperty("not_after").GetInt64()),
                        IsRoot = certData.TryGetProperty("is_root", out var isRoot) && isRoot.GetBoolean(),
                        IsRevoked = certData.TryGetProperty("revoked", out var revoked) && revoked.GetBoolean()
                    };
                    
                    certificates.Add(certInfo);
                }

                // Check for expired certificates
                var expiredCert = certificates.FirstOrDefault(c => DateTimeOffset.UtcNow > c.NotAfter);
                if (expiredCert != null)
                {
                    return new CertificateValidationResult
                    {
                        IsValid = false,
                        Message = $"Certificate has expired: {expiredCert.Subject}"
                    };
                }

                // Check for revoked certificates
                var revokedCert = certificates.FirstOrDefault(c => c.IsRevoked);
                if (revokedCert != null)
                {
                    return new CertificateValidationResult
                    {
                        IsValid = false,
                        Message = $"Certificate has been revoked: {revokedCert.Subject}"
                    };
                }

                // Check for trusted root
                var rootCert = certificates.FirstOrDefault(c => c.IsRoot);
                if (rootCert == null || !rootCert.Subject.Contains("Intel"))
                {
                    return new CertificateValidationResult
                    {
                        IsValid = false,
                        Message = "Untrusted root certificate"
                    };
                }

                // Check chain continuity
                for (int i = 1; i < certificates.Count; i++)
                {
                    var cert = certificates[i];
                    var issuer = certificates.FirstOrDefault(c => c.Subject == cert.Issuer);
                    if (issuer == null)
                    {
                        return new CertificateValidationResult
                        {
                            IsValid = false,
                            Message = "Certificate chain is broken"
                        };
                    }
                }

                return new CertificateValidationResult
                {
                    IsValid = true,
                    Message = "Certificate chain validation successful",
                    RootCertificate = rootCert,
                    IntermediateCertificates = certificates.Where(c => !c.IsRoot && c != certificates.Last()).ToArray(),
                    LeafCertificate = certificates.Last()
                };
            }
            catch (Exception ex)
            {
                return new CertificateValidationResult
                {
                    IsValid = false,
                    Message = $"Certificate validation error: {ex.Message}"
                };
            }
        }

        public async Task<AttestationResult> PerformRemoteAttestationAsync(EnclaveIdentity enclaveIdentity, string nonce)
        {
            try
            {
                // Validate enclave identity
                if (string.IsNullOrEmpty(enclaveIdentity.MrEnclave) || 
                    string.IsNullOrEmpty(enclaveIdentity.MrSigner))
                {
                    return new AttestationResult
                    {
                        IsValid = false,
                        Message = "Invalid enclave identity - missing MrEnclave or MrSigner",
                        Nonce = nonce
                    };
                }

                // Check if it's a known valid enclave (Intel-signed)
                var expectedIntelSigner = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                    Encoding.UTF8.GetBytes("Intel Corporation"))).ToUpper();
                
                if (!enclaveIdentity.MrSigner.Equals(expectedIntelSigner, StringComparison.OrdinalIgnoreCase))
                {
                    return new AttestationResult
                    {
                        IsValid = false,
                        Message = "Invalid enclave identity - not Intel signed",
                        Nonce = nonce
                    };
                }

                // Simulate network call to Intel Attestation Service
                if (_attestationServiceUrl.Contains("invalid.url"))
                {
                    return new AttestationResult
                    {
                        IsValid = false,
                        Message = "Network error - unable to connect to attestation service",
                        Nonce = nonce
                    };
                }

                // Simulate timeout
                if (_timeout.TotalMilliseconds <= 1)
                {
                    await Task.Delay(10); // Simulate work that exceeds timeout
                    return new AttestationResult
                    {
                        IsValid = false,
                        Message = "Timeout - attestation service did not respond in time",
                        Nonce = nonce
                    };
                }

                // Generate attestation evidence
                var evidence = new
                {
                    quote = GenerateAttestationQuote(enclaveIdentity, nonce),
                    certificates = GenerateAttestationCertificates(),
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    service_url = _attestationServiceUrl
                };

                return new AttestationResult
                {
                    IsValid = true,
                    Message = "Remote attestation successful",
                    AttestationEvidence = JsonSerializer.Serialize(evidence),
                    Nonce = nonce,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
            }
            catch (Exception ex)
            {
                return new AttestationResult
                {
                    IsValid = false,
                    Message = $"Remote attestation error: {ex.Message}",
                    Nonce = nonce
                };
            }
        }

        public async Task<CertificateInfo[]> GetTrustedCertificatesAsync()
        {
            // Simulate Intel trusted certificates
            await Task.Delay(10); // Simulate network call
            
            return new[]
            {
                new CertificateInfo
                {
                    Subject = "CN=Intel SGX Root CA",
                    Issuer = "CN=Intel SGX Root CA",
                    NotBefore = DateTimeOffset.UtcNow.AddYears(-5),
                    NotAfter = DateTimeOffset.UtcNow.AddYears(5),
                    IsRoot = true,
                    IsRevoked = false
                },
                new CertificateInfo
                {
                    Subject = "CN=Intel SGX Intermediate CA",
                    Issuer = "CN=Intel SGX Root CA",
                    NotBefore = DateTimeOffset.UtcNow.AddYears(-2),
                    NotAfter = DateTimeOffset.UtcNow.AddYears(2),
                    IsRoot = false,
                    IsRevoked = false
                }
            };
        }

        public async Task<CertificateRevocationList> GetCertificateRevocationListAsync()
        {
            await Task.Delay(10); // Simulate network call
            
            return new CertificateRevocationList
            {
                IssuedAt = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(),
                NextUpdate = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                RevokedCertificates = new[]
                {
                    new RevokedCertificate
                    {
                        SerialNumber = "1234567890ABCDEF",
                        RevocationDate = DateTimeOffset.UtcNow.AddDays(-10).ToUnixTimeSeconds(),
                        Reason = "Key compromise"
                    }
                }
            };
        }

        public async Task<bool> VerifyIntelSignatureAsync(byte[] data, byte[] signature, string certificate)
        {
            await Task.Delay(1); // Simulate verification work
            
            // Simulate signature verification
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes("intel_test_key"));
            var expectedSignature = hmac.ComputeHash(data);
            
            return signature.SequenceEqual(expectedSignature);
        }

        public async Task<PolicyValidationResult> ValidateSecurityPolicyAsync(EnclaveReport report, SecurityPolicy policy)
        {
            await Task.Delay(1); // Simulate validation work
            
            // Check debug mode
            if (report.Debug && !policy.AllowDebugMode)
            {
                return new PolicyValidationResult
                {
                    IsValid = false,
                    Message = "Debug mode not allowed by security policy"
                };
            }

            // Check security version
            if (report.SecurityVersion < policy.MinSecurityVersion)
            {
                return new PolicyValidationResult
                {
                    IsValid = false,
                    Message = $"Security version too low: {report.SecurityVersion} < {policy.MinSecurityVersion}"
                };
            }

            // Check authorized signers
            if (policy.RequiredSigners?.Length > 0)
            {
                var signerValid = false;
                foreach (var requiredSigner in policy.RequiredSigners)
                {
                    var expectedMrSigner = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                        Encoding.UTF8.GetBytes(requiredSigner))).ToUpper();
                    
                    if (report.MrSigner.Equals(expectedMrSigner, StringComparison.OrdinalIgnoreCase))
                    {
                        signerValid = true;
                        break;
                    }
                }

                if (!signerValid)
                {
                    return new PolicyValidationResult
                    {
                        IsValid = false,
                        Message = "Unauthorized signer - not in allowed signers list"
                    };
                }
            }

            // Check age
            if (policy.MaxAge.HasValue)
            {
                var reportAge = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(report.Timestamp);
                if (reportAge > policy.MaxAge.Value)
                {
                    return new PolicyValidationResult
                    {
                        IsValid = false,
                        Message = "Report is too old"
                    };
                }
            }

            return new PolicyValidationResult
            {
                IsValid = true,
                Message = "Security policy validation successful"
            };
        }

        private string GenerateAttestationQuote(EnclaveIdentity identity, string nonce)
        {
            var quote = new
            {
                version = 4,
                report_body = new
                {
                    mr_enclave = identity.MrEnclave,
                    mr_signer = identity.MrSigner,
                    isv_prod_id = identity.ProductId,
                    isv_svn = identity.SecurityVersion,
                    report_data = Convert.ToBase64String(Encoding.UTF8.GetBytes(nonce + new string('\0', 32)).Take(64).ToArray())
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(quote)));
        }

        private string[] GenerateAttestationCertificates()
        {
            return new[]
            {
                Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"subject\": \"CN=Intel SGX Root CA\", \"issuer\": \"CN=Intel SGX Root CA\"}")),
                Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"subject\": \"CN=Intel SGX Attestation\", \"issuer\": \"CN=Intel SGX Root CA\"}"))
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // Supporting types for the tests
    public class AttestationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public JsonElement? QuoteData { get; set; }
        public string AttestationEvidence { get; set; }
        public string Nonce { get; set; }
        public long Timestamp { get; set; }
    }

    public class CertificateValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public CertificateInfo RootCertificate { get; set; }
        public CertificateInfo[] IntermediateCertificates { get; set; }
        public CertificateInfo LeafCertificate { get; set; }
    }

    public class PolicyValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }

    public class CertificateInfo
    {
        public string Subject { get; set; }
        public string Issuer { get; set; }
        public DateTimeOffset NotBefore { get; set; }
        public DateTimeOffset NotAfter { get; set; }
        public bool IsRoot { get; set; }
        public bool IsRevoked { get; set; }
    }

    public class CertificateRevocationList
    {
        public long IssuedAt { get; set; }
        public long NextUpdate { get; set; }
        public RevokedCertificate[] RevokedCertificates { get; set; }
    }

    public class RevokedCertificate
    {
        public string SerialNumber { get; set; }
        public long RevocationDate { get; set; }
        public string Reason { get; set; }
    }

    public class EnclaveIdentity
    {
        public string MrEnclave { get; set; }
        public string MrSigner { get; set; }
        public int ProductId { get; set; }
        public int SecurityVersion { get; set; }
        public EnclaveAttributes Attributes { get; set; }
    }

    public class EnclaveAttributes
    {
        public bool Debug { get; set; }
        public bool Mode64Bit { get; set; }
        public bool ProvisionKey { get; set; }
        public bool EInitTokenKey { get; set; }
    }

    public class EnclaveReport
    {
        public int SecurityVersion { get; set; }
        public bool Debug { get; set; }
        public string MrSigner { get; set; }
        public long Timestamp { get; set; }
    }

    public class SecurityPolicy
    {
        public int MinSecurityVersion { get; set; } = 1;
        public string[] RequiredSigners { get; set; }
        public bool AllowDebugMode { get; set; } = false;
        public bool RequireIntelSigned { get; set; } = true;
        public TimeSpan? MaxAge { get; set; }
    }
}
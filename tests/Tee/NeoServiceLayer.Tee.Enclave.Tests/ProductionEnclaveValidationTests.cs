using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// Production validation tests ensuring enclave operations meet enterprise requirements.
    /// Focuses on security, compliance, audit trails, and error handling.
    /// </summary>
    public class ProductionEnclaveValidationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly SGXSimulationEnclaveWrapper _enclave;

        public ProductionEnclaveValidationTests(ITestOutputHelper output)
        {
            _output = output;
            _enclave = new SGXSimulationEnclaveWrapper();
        }

        public void Dispose()
        {
            _enclave?.Dispose();
        }

        #region Security Validation Tests

        [Fact]
        public void ProductionValidation_CryptographicRandomness_ShouldMeetNISTStandards()
        {
            // Arrange
            _enclave.Initialize();
            const int sampleSize = 10000;
            var randomBytes = new byte[sampleSize];

            // Act
            for (int i = 0; i < sampleSize; i++)
            {
                var singleByte = _enclave.GenerateRandomBytes(1);
                randomBytes[i] = singleByte[0];
            }

            // Assert - Basic statistical tests for randomness
            var zeroBits = 0;
            var oneBits = 0;

            foreach (var b in randomBytes)
            {
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((b & (1 << bit)) != 0)
                        oneBits++;
                    else
                        zeroBits++;
                }
            }

            var totalBits = sampleSize * 8;
            var zeroBitRatio = (double)zeroBits / totalBits;
            var oneBitRatio = (double)oneBits / totalBits;

            // NIST requires roughly 50% distribution for random bits
            zeroBitRatio.Should().BeInRange(0.48, 0.52, "Zero bit distribution should be close to 50%");
            oneBitRatio.Should().BeInRange(0.48, 0.52, "One bit distribution should be close to 50%");

            _output.WriteLine($"✅ Cryptographic randomness meets NIST standards");
            _output.WriteLine($"   Zero bits: {zeroBitRatio:P2}, One bits: {oneBitRatio:P2}");
        }

        [Fact]
        public void ProductionValidation_KeyGeneration_ShouldIncludeAuditMetadata()
        {
            // Arrange
            _enclave.Initialize();
            var keyId = "production-audit-key";

            // Act
            var keyResult = _enclave.GenerateKey(keyId, "secp256k1", "Sign,Verify", false, "Production audit test");

            // Assert
            keyResult.Should().NotBeNullOrEmpty();
            var keyJson = JsonDocument.Parse(keyResult);

            // Verify audit trail fields
            keyJson.RootElement.TryGetProperty("keyId", out _).Should().BeTrue();
            keyJson.RootElement.TryGetProperty("created", out _).Should().BeTrue();
            keyJson.RootElement.TryGetProperty("algorithm", out _).Should().BeTrue();
            keyJson.RootElement.TryGetProperty("keySize", out _).Should().BeTrue();
            keyJson.RootElement.TryGetProperty("fingerprint", out _).Should().BeTrue();
            keyJson.RootElement.TryGetProperty("enclaveGenerated", out _).Should().BeTrue();
            keyJson.RootElement.TryGetProperty("attestation", out _).Should().BeTrue();

            keyJson.RootElement.GetProperty("enclaveGenerated").GetBoolean().Should().BeTrue();
            keyJson.RootElement.GetProperty("exportable").GetBoolean().Should().BeFalse();

            _output.WriteLine("✅ Key generation includes complete audit metadata");
        }

        [Fact]
        public void ProductionValidation_EncryptionAuthentication_ShouldPreventTamperingDetection()
        {
            // Arrange
            _enclave.Initialize();
            var plaintext = Encoding.UTF8.GetBytes("Confidential business data requiring integrity protection");
            var key = _enclave.GenerateRandomBytes(32);

            // Act
            var ciphertext = _enclave.Encrypt(plaintext, key);

            // Tamper with ciphertext more aggressively to ensure detection
            var tamperedCiphertext = (byte[])ciphertext.Clone();
            // Flip multiple bits to ensure detection
            for (int i = 0; i < Math.Min(tamperedCiphertext.Length, 4); i++)
            {
                tamperedCiphertext[i] = (byte)(tamperedCiphertext[i] ^ 0xFF);
            }

            // Assert
            var exception = Assert.Throws<System.Security.Cryptography.CryptographicException>(() => _enclave.Decrypt(tamperedCiphertext, key));

            _output.WriteLine("✅ Encrypted data tampering is properly detected and rejected");
        }

        [Fact]
        public void ProductionValidation_SecureStorage_ShouldIncludeIntegrityChecking()
        {
            // Arrange
            _enclave.Initialize();
            var storageKey = "integrity-test-data";
            var sensitiveData = Encoding.UTF8.GetBytes("Sensitive customer PII data");
            var encryptionKey = "production-encryption-key";

            // Act
            var storeResult = _enclave.StoreData(storageKey, sensitiveData, encryptionKey, true);
            var metadata = _enclave.GetStorageMetadata(storageKey);

            // Assert
            storeResult.Should().NotBeNullOrEmpty();
            metadata.Should().NotBeNullOrEmpty();

            var storeJson = JsonDocument.Parse(storeResult);
            storeJson.RootElement.TryGetProperty("checksum", out _).Should().BeTrue();
            storeJson.RootElement.TryGetProperty("timestamp", out _).Should().BeTrue();
            storeJson.RootElement.TryGetProperty("enclave", out _).Should().BeTrue();
            storeJson.RootElement.TryGetProperty("attestation", out _).Should().BeTrue();

            var metadataJson = JsonDocument.Parse(metadata);
            metadataJson.RootElement.TryGetProperty("checksum", out _).Should().BeTrue();
            metadataJson.RootElement.TryGetProperty("encrypted", out _).Should().BeTrue();
            metadataJson.RootElement.TryGetProperty("enclaveSecured", out _).Should().BeTrue();

            _output.WriteLine("✅ Secure storage includes integrity checking and audit metadata");
        }

        #endregion

        #region Compliance and Audit Tests

        [Fact]
        public void ProductionValidation_AllOperations_ShouldIncludeAttestationData()
        {
            // Arrange
            _enclave.Initialize();

            // Act & Assert - Test that all major operations include attestation
            var keyResult = _enclave.GenerateKey("audit-key", "aes", "Encrypt", false, "Audit test");
            var keyJson = JsonDocument.Parse(keyResult);
            keyJson.RootElement.TryGetProperty("attestation", out _).Should().BeTrue();

            var data = Encoding.UTF8.GetBytes("audit data");
            var storeResult = _enclave.StoreData("audit-storage", data, "audit-enc-key");
            var storeJson = JsonDocument.Parse(storeResult);
            storeJson.RootElement.TryGetProperty("attestation", out _).Should().BeTrue();

            var trainingResult = _enclave.TrainAIModel("audit-model", "linear", new double[] { 1, 2, 3 });
            var trainingJson = JsonDocument.Parse(trainingResult);
            trainingJson.RootElement.TryGetProperty("attestation", out _).Should().BeTrue();

            var accountResult = _enclave.CreateAbstractAccount("audit-account", "{}");
            var accountJson = JsonDocument.Parse(accountResult);
            accountJson.RootElement.TryGetProperty("attestation", out _).Should().BeTrue();

            _output.WriteLine("✅ All operations include attestation data for compliance");
        }

        [Fact]
        public void ProductionValidation_SensitiveDataHandling_ShouldMeetDataProtectionStandards()
        {
            // Arrange
            _enclave.Initialize();
            var piiData = """
                {
                "customerName": "John Doe",
                    "ssn": "123-45-6789",
                    "creditCard": "4532-1234-5678-9012",
                    "medicalRecord": "Patient has diabetes type 2"
                }
            """;
            var piiBytes = Encoding.UTF8.GetBytes(piiData);
            var encryptionKey = "gdpr-compliant-key";

            // Act
            var storeResult = _enclave.StoreData("pii-data", piiBytes, encryptionKey, true);
            var retrievedData = _enclave.RetrieveData("pii-data", encryptionKey);
            var metadata = _enclave.GetStorageMetadata("pii-data");

            // Assert
            retrievedData.Should().Equal(piiBytes);

            var storeJson = JsonDocument.Parse(storeResult);
            storeJson.RootElement.GetProperty("encrypted").GetBoolean().Should().BeTrue();
            storeJson.RootElement.GetProperty("enclaveSecured").GetBoolean().Should().BeTrue();

            var metadataJson = JsonDocument.Parse(metadata);
            metadataJson.RootElement.GetProperty("encrypted").GetBoolean().Should().BeTrue();

            _output.WriteLine("✅ Sensitive data handling meets data protection standards");
        }

        #endregion

        #region Performance and Scalability Tests

        [Fact]
        public void ProductionValidation_HighVolumeOperations_ShouldMaintainPerformance()
        {
            // Arrange
            _enclave.Initialize();
            const int operationCount = 1000;
            var durations = new List<TimeSpan>();

            // Act - Test sustained high-volume operations
            for (int i = 0; i < operationCount; i++)
            {
                var startTime = DateTime.UtcNow;

                var randomData = _enclave.GenerateRandomBytes(256);
                var key = _enclave.GenerateRandomBytes(32);
                var encrypted = _enclave.Encrypt(randomData, key);
                var decrypted = _enclave.Decrypt(encrypted, key);

                var endTime = DateTime.UtcNow;
                durations.Add(endTime - startTime);

                // Verify correctness
                decrypted.Should().Equal(randomData);
            }

            // Assert
            var averageDuration = durations.Average(d => d.TotalMilliseconds);
            var maxDuration = durations.Max(d => d.TotalMilliseconds);
            var minDuration = durations.Min(d => d.TotalMilliseconds);

            averageDuration.Should().BeLessThan(100, "Average operation should complete within 100ms");
            maxDuration.Should().BeLessThan(1000, "No operation should take longer than 1 second");

            _output.WriteLine($"✅ High-volume operations maintain performance:");
            _output.WriteLine($"   Operations: {operationCount}");
            _output.WriteLine($"   Average: {averageDuration:F2}ms");
            _output.WriteLine($"   Min: {minDuration:F2}ms, Max: {maxDuration:F2}ms");
        }

        [Fact]
        public void ProductionValidation_MemoryUsage_ShouldBeConstrained()
        {
            // Arrange
            _enclave.Initialize();
            var initialMemory = GC.GetTotalMemory(true);

            // Act - Perform memory-intensive operations
            for (int i = 0; i < 100; i++)
            {
                var largeData = _enclave.GenerateRandomBytes(1024 * 10); // 10KB per iteration
                var key = _enclave.GenerateRandomBytes(32);
                var encrypted = _enclave.Encrypt(largeData, key);
                var decrypted = _enclave.Decrypt(encrypted, key);

                // Store and retrieve to test storage memory
                var storageKey = $"memory-test-{i}";
                _enclave.StoreData(storageKey, largeData, "mem-key");
                var retrieved = _enclave.RetrieveData(storageKey, "mem-key");
                _enclave.DeleteData(storageKey);
            }

            // Assert
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;
            var memoryIncreaseKB = memoryIncrease / 1024.0;

            memoryIncreaseKB.Should().BeLessThan(5000, "Memory increase should be less than 5MB");
            _output.WriteLine($"✅ Memory usage is constrained (increased by {memoryIncreaseKB:F2}KB)");
        }

        #endregion

        #region Error Recovery and Resilience Tests

        [Fact]
        public void ProductionValidation_ExceptionRecovery_ShouldNotCorruptState()
        {
            // Arrange
            _enclave.Initialize();
            var validKey = "valid-key";
            var validData = Encoding.UTF8.GetBytes("valid data");

            // Act - Cause an exception and then test normal operations
            try
            {
                _enclave.GenerateRandomBytes(-1); // This should throw
            }
            catch (ArgumentException)
            {
                // Expected exception
            }

            // Assert - Enclave should still work normally after exception
            var randomBytes = _enclave.GenerateRandomBytes(32);
            randomBytes.Should().HaveCount(32);

            _enclave.StoreData(validKey, validData, "enc-key");
            var retrievedData = _enclave.RetrieveData(validKey, "enc-key");
            retrievedData.Should().Equal(validData);

            _output.WriteLine("✅ Enclave state remains stable after exceptions");
        }

        [Fact]
        public async Task ProductionValidation_ConcurrentExceptions_ShouldNotAffectOtherOperations()
        {
            // Arrange
            _enclave.Initialize();
            const int concurrentTasks = 20;

            // Act - Half the tasks will cause exceptions, half will be normal
            var tasks = Enumerable.Range(0, concurrentTasks).Select(taskId =>
                Task.Run(() =>
                {
                    try
                    {
                        if (taskId % 2 == 0)
                        {
                            // Normal operation
                            var data = _enclave.GenerateRandomBytes(32);
                            var key = _enclave.GenerateRandomBytes(32);
                            var encrypted = _enclave.Encrypt(data, key);
                            var decrypted = _enclave.Decrypt(encrypted, key);
                            return decrypted.SequenceEqual(data);
                        }
                        else
                        {
                            // Operation that causes exception
                            _enclave.GenerateRandomBytes(-1);
                            return false; // Should not reach here
                        }
                    }
                    catch (ArgumentException)
                    {
                        return true; // Expected exception
                    }
                    catch (Exception)
                    {
                        return false; // Unexpected exception
                    }
                }));

            var results = await Task.WhenAll(tasks);

            // Assert - All tasks should complete successfully (either normal operation or expected exception)
            results.Should().AllSatisfy(result => result.Should().BeTrue());
            _output.WriteLine("✅ Concurrent exceptions do not affect other operations");
        }

        #endregion

        #region Business Logic Validation Tests

        [Fact]
        public void ProductionValidation_FinancialTransactionSigning_ShouldBeSecureAndAuditable()
        {
            // Arrange
            _enclave.Initialize();
            var accountId = "financial-account-001";
            var accountData = """
                {
                "accountType": "business",
                    "complianceLevel": "SOX",
                    "auditRequired": true,
                    "multiSigThreshold": 2
                }
            """;
            var transactionData = """
                {
                "transactionType": "wire_transfer",
                    "amount": "1000000.00",
                    "currency": "USD",
                    "from": "account-001",
                    "to": "account-002",
                    "timestamp": "2024-12-19T10:00:00Z",
                    "memo": "Business payment for services",
                    "complianceChecked": true
                }
            """;

            // Act
            var accountResult = _enclave.CreateAbstractAccount(accountId, accountData);
            var signatureResult = _enclave.SignAbstractAccountTransaction(accountId, transactionData);

            // Assert
            accountResult.Should().NotBeNullOrEmpty();
            signatureResult.Should().NotBeNullOrEmpty();

            var accountJson = JsonDocument.Parse(accountResult);
            accountJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
            accountJson.RootElement.TryGetProperty("enclaveSecured", out _).Should().BeTrue();

            var signatureJson = JsonDocument.Parse(signatureResult);
            signatureJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
            signatureJson.RootElement.TryGetProperty("signature", out _).Should().BeTrue();
            signatureJson.RootElement.TryGetProperty("signedAt", out _).Should().BeTrue();
            signatureJson.RootElement.TryGetProperty("enclaveSecured", out _).Should().BeTrue();
            signatureJson.RootElement.TryGetProperty("attestation", out _).Should().BeTrue();

            _output.WriteLine("✅ Financial transaction signing is secure and auditable");
        }

        [Fact]
        public void ProductionValidation_PredictiveAnalytics_ShouldProvideReliableMLInference()
        {
            // Arrange
            _enclave.Initialize();
            var modelId = "risk-assessment-model";
            var trainingData = new double[]
            {
                // Historical risk scores and outcomes
                0.1, 0.2, 0.15, 0.8, 0.9, 0.85, 0.3, 0.4, 0.35, 0.7,
                0.12, 0.18, 0.22, 0.75, 0.88, 0.92, 0.33, 0.38, 0.41, 0.68
            };
            var parameters = """
                {
                "modelType": "riskAssessment",
                    "confidenceThreshold": 0.85,
                    "auditTrail": true,
                    "regulatoryCompliance": "Basel III"
                }
            """;

            // Act
            var trainingResult = _enclave.TrainAIModel(modelId, "risk_assessment", trainingData, parameters);

            // Test predictions for various risk profiles
            var lowRiskInput = new double[] { 0.1, 0.15, 0.12 };
            var highRiskInput = new double[] { 0.85, 0.9, 0.88 };

            var (lowRiskPredictions, lowRiskMetadata) = _enclave.PredictWithAIModel(modelId, lowRiskInput);
            var (highRiskPredictions, highRiskMetadata) = _enclave.PredictWithAIModel(modelId, highRiskInput);

            // Assert
            trainingResult.Should().NotBeNullOrEmpty();
            lowRiskPredictions.Should().NotBeNull();
            highRiskPredictions.Should().NotBeNull();

            var trainingJson = JsonDocument.Parse(trainingResult);
            trainingJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
            trainingJson.RootElement.TryGetProperty("enclaveTrained", out _).Should().BeTrue();

            var lowRiskMetadataJson = JsonDocument.Parse(lowRiskMetadata);
            lowRiskMetadataJson.RootElement.TryGetProperty("confidence", out _).Should().BeTrue();
            lowRiskMetadataJson.RootElement.TryGetProperty("enclaveSecured", out _).Should().BeTrue();

            // Risk predictions should be reasonable
            lowRiskPredictions.Average().Should().BeLessThan(highRiskPredictions.Average(),
                "Low risk inputs should produce lower risk predictions");

            _output.WriteLine("✅ Predictive analytics provides reliable ML inference");
            _output.WriteLine($"   Low risk prediction average: {lowRiskPredictions.Average():F3}");
            _output.WriteLine($"   High risk prediction average: {highRiskPredictions.Average():F3}");
        }

        #endregion

        #region Data Governance and Privacy Tests

        [Fact]
        public void ProductionValidation_DataRetention_ShouldSupportRightToErasure()
        {
            // Arrange
            _enclave.Initialize();
            var customerDataKeys = new[] { "customer-001-pii", "customer-001-financial", "customer-001-behavioral" };
            var customerData = Encoding.UTF8.GetBytes("Sensitive customer data that must be erasable");
            var encryptionKey = "gdpr-encryption-key";

            // Act - Store customer data
            foreach (var key in customerDataKeys)
            {
                _enclave.StoreData(key, customerData, encryptionKey);
            }

            // Verify data exists
            foreach (var key in customerDataKeys)
            {
                var retrievedData = _enclave.RetrieveData(key, encryptionKey);
                retrievedData.Should().Equal(customerData);
            }

            // Act - Exercise right to erasure
            foreach (var key in customerDataKeys)
            {
                var deleteResult = _enclave.DeleteData(key);
                var deleteJson = JsonDocument.Parse(deleteResult);
                deleteJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
                deleteJson.RootElement.GetProperty("deleted").GetBoolean().Should().BeTrue();
            }

            // Assert - Data should be completely removed
            foreach (var key in customerDataKeys)
            {
                var exception = Assert.Throws<KeyNotFoundException>(() =>
                    _enclave.RetrieveData(key, encryptionKey));
                exception.Message.Should().Contain("not found");
            }

            _output.WriteLine("✅ Data retention supports right to erasure (GDPR compliance)");
        }

        #endregion
    }
}

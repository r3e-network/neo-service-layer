using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.TestUtilities
{
    /// <summary>
    /// Comprehensive test data factory for generating consistent test data across all test scenarios.
    /// </summary>
    public class TestDataFactory
    {
        private readonly ILogger<TestDataFactory> _logger;
        private readonly Random _random;
        private static readonly string[] SampleNames = 
        {
            "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry", "Ivy", "Jack"
        };
        
        private static readonly string[] SampleEmails = 
        {
            "test.user@example.com", "demo.account@test.org", "sample.data@domain.net"
        };

        public TestDataFactory(ILogger<TestDataFactory> logger)
        {
            _logger = logger;
            _random = new Random(42); // Fixed seed for reproducible tests
        }

        #region User and Authentication Data

        /// <summary>
        /// Creates test user data with optional customization.
        /// </summary>
        public TestUserData CreateTestUser(string? username = null, string? email = null)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            
            return new TestUserData
            {
                UserId = Guid.NewGuid().ToString(),
                Username = username ?? $"testuser_{timestamp}_{_random.Next(1000, 9999)}",
                Email = email ?? $"test_{timestamp}@example.com",
                FirstName = SampleNames[_random.Next(SampleNames.Length)],
                LastName = $"TestUser{_random.Next(1000, 9999)}",
                Password = "SecureTestPassword123!",
                PasswordHash = GeneratePasswordHash("SecureTestPassword123!"),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Roles = new List<string> { "User" },
                Permissions = new List<string> { "Read", "Write" },
                Profile = new Dictionary<string, object>
                {
                    ["department"] = "Testing",
                    ["location"] = "Virtual",
                    ["preferences"] = new { theme = "dark", language = "en" }
                }
            };
        }

        /// <summary>
        /// Creates test authentication tokens.
        /// </summary>
        public TestAuthToken CreateAuthToken(string? userId = null, TimeSpan? expiryDuration = null)
        {
            var expiry = expiryDuration ?? TimeSpan.FromHours(24);
            
            return new TestAuthToken
            {
                TokenId = Guid.NewGuid().ToString(),
                UserId = userId ?? Guid.NewGuid().ToString(),
                AccessToken = GenerateSecureToken(32),
                RefreshToken = GenerateSecureToken(64),
                TokenType = "Bearer",
                ExpiresAt = DateTime.UtcNow.Add(expiry),
                IssuedAt = DateTime.UtcNow,
                Scope = "read write admin",
                Claims = new Dictionary<string, object>
                {
                    ["sub"] = userId ?? Guid.NewGuid().ToString(),
                    ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ["exp"] = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds(),
                    ["role"] = "TestUser"
                }
            };
        }

        #endregion

        #region Cryptographic and Key Management Data

        /// <summary>
        /// Creates test cryptographic key pair data.
        /// </summary>
        public TestKeyPair CreateKeyPair(string keyType = "secp256r1", string? userId = null)
        {
            var privateKey = Convert.ToBase64String(ecdsa.ExportECPrivateKey());
            var publicKey = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());

            return new TestKeyPair
            {
                KeyId = Guid.NewGuid().ToString(),
                UserId = userId ?? Guid.NewGuid().ToString(),
                KeyType = keyType,
                PrivateKey = privateKey,
                PublicKey = publicKey,
                Algorithm = "ECDSA",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                Purpose = "Signing",
                Metadata = new Dictionary<string, object>
                {
                    ["curve"] = keyType,
                    ["usage"] = new[] { "sign", "verify" },
                    ["environment"] = "test"
                }
            };
        }

        /// <summary>
        /// Creates test signature data.
        /// </summary>
        public TestSignature CreateSignature(string data, TestKeyPair? keyPair = null)
        {
            var keys = keyPair ?? CreateKeyPair();
            var signature = GenerateSignature(data, keys.PrivateKey);

            return new TestSignature
            {
                SignatureId = Guid.NewGuid().ToString(),
                Data = data,
                Signature = signature,
                PublicKey = keys.PublicKey,
                Algorithm = "ECDSA",
                HashAlgorithm = "SHA256",
                CreatedAt = DateTime.UtcNow,
                IsValid = true,
                Metadata = new Dictionary<string, object>
                {
                    ["keyId"] = keys.KeyId,
                    ["purpose"] = "test_verification"
                }
            };
        }

        #endregion

        #region Blockchain and Smart Contract Data

        /// <summary>
        /// Creates test blockchain account data.
        /// </summary>
        public TestAccountData CreateBlockchainAccount(string? address = null, decimal balance = 1000.0m)
        {
            return new TestAccountData
            {
                AccountId = Guid.NewGuid().ToString(),
                Address = address ?? GenerateBlockchainAddress(),
                Balance = balance,
                Nonce = _random.Next(0, 1000),
                AccountType = "Standard",
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                IsActive = true,
                Assets = new Dictionary<string, decimal>
                {
                    ["NEO"] = balance * 0.1m,
                    ["GAS"] = balance * 0.9m
                },
                Contracts = new List<string>(),
                Metadata = new Dictionary<string, object>
                {
                    ["version"] = "1.0",
                    ["network"] = "testnet"
                }
            };
        }

        /// <summary>
        /// Creates test smart contract data.
        /// </summary>
        public TestSmartContract CreateSmartContract(string? hash = null, string? author = null)
        {
            return new TestSmartContract
            {
                ContractHash = hash ?? GenerateContractHash(),
                Name = $"TestContract_{_random.Next(1000, 9999)}",
                Author = author ?? $"test_author_{_random.Next(100, 999)}",
                Version = "1.0.0",
                Script = GenerateTestContractScript(),
                Manifest = CreateContractManifest(),
                DeployedAt = DateTime.UtcNow,
                Gas = _random.Next(100, 10000),
                IsActive = true,
                Methods = new List<string> { "initialize", "transfer", "balanceOf", "totalSupply" },
                Events = new List<string> { "Transfer", "Approval" },
                Storage = new Dictionary<string, object>
                {
                    ["totalSupply"] = 1000000,
                    ["decimals"] = 8,
                    ["symbol"] = "TEST"
                }
            };
        }

        /// <summary>
        /// Creates test transaction data.
        /// </summary>
        public TestTransaction CreateTransaction(string? from = null, string? to = null, decimal amount = 100.0m)
        {
            return new TestTransaction
            {
                TransactionId = Guid.NewGuid().ToString(),
                Hash = GenerateTransactionHash(),
                FromAddress = from ?? GenerateBlockchainAddress(),
                ToAddress = to ?? GenerateBlockchainAddress(),
                Amount = amount,
                Fee = amount * 0.001m,
                Nonce = _random.Next(1, 1000000),
                Timestamp = DateTime.UtcNow,
                BlockHeight = _random.Next(1000000, 2000000),
                Status = "Confirmed",
                Confirmations = _random.Next(1, 100),
                Data = JsonSerializer.Serialize(new { purpose = "test_transfer", environment = "test" }),
                Witnesses = new List<TestWitness>
                {
                    new()
                    {
                        InvocationScript = GenerateSecureToken(32),
                        VerificationScript = GenerateSecureToken(16)
                    }
                }
            };
        }

        #endregion

        #region Service Integration Data

        /// <summary>
        /// Creates test service configuration data.
        /// </summary>
        public TestServiceConfig CreateServiceConfig(string serviceName, Dictionary<string, object>? customSettings = null)
        {
            var defaultSettings = new Dictionary<string, object>
            {
                ["maxRetries"] = 3,
                ["timeoutMs"] = 30000,
                ["enableCaching"] = true,
                ["logLevel"] = "Information",
                ["environment"] = "test"
            };

            if (customSettings != null)
            {
                foreach (var setting in customSettings)
                {
                    defaultSettings[setting.Key] = setting.Value;
                }
            }

            return new TestServiceConfig
            {
                ServiceName = serviceName,
                ServiceId = Guid.NewGuid().ToString(),
                Version = "1.0.0",
                BaseUrl = $"https://test-{serviceName.ToLower()}.example.com",
                Settings = defaultSettings,
                Dependencies = new List<string>(),
                HealthCheckUrl = $"https://test-{serviceName.ToLower()}.example.com/health",
                CreatedAt = DateTime.UtcNow,
                IsEnabled = true
            };
        }

        /// <summary>
        /// Creates test workflow definition.
        /// </summary>
        public TestWorkflowDefinition CreateWorkflowDefinition(string workflowName, List<string> services)
        {
            return new TestWorkflowDefinition
            {
                WorkflowId = Guid.NewGuid().ToString(),
                Name = workflowName,
                Version = "1.0.0",
                Description = $"Test workflow for {workflowName}",
                RequiredServices = services,
                Steps = services.Select((service, index) => new TestWorkflowStep
                {
                    StepId = Guid.NewGuid().ToString(),
                    Name = $"Step_{index + 1}_{service}",
                    ServiceName = service,
                    Order = index + 1,
                    IsRequired = true,
                    TimeoutMs = 30000,
                    RetryCount = 3,
                    Parameters = new Dictionary<string, object>
                    {
                        ["testData"] = $"test_value_{index}",
                        ["stepType"] = "integration_test"
                    }
                }).ToList(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test_system",
                IsActive = true
            };
        }

        #endregion

        #region Performance and Monitoring Data

        /// <summary>
        /// Creates test performance metrics.
        /// </summary>
        public TestPerformanceMetrics CreatePerformanceMetrics(string operation, TimeSpan? duration = null)
        {
            var operationDuration = duration ?? TimeSpan.FromMilliseconds(_random.Next(10, 1000));
            
            return new TestPerformanceMetrics
            {
                MetricId = Guid.NewGuid().ToString(),
                Operation = operation,
                Timestamp = DateTime.UtcNow,
                Duration = operationDuration,
                MemoryUsageMB = _random.NextDouble() * 100,
                CpuUsagePercent = _random.NextDouble() * 50,
                ThroughputPerSecond = _random.Next(100, 10000),
                ErrorCount = _random.Next(0, 5),
                SuccessCount = _random.Next(95, 100),
                CustomMetrics = new Dictionary<string, double>
                {
                    ["cacheHitRatio"] = _random.NextDouble() * 100,
                    ["dbConnectionPoolUsage"] = _random.NextDouble() * 80,
                    ["queueLength"] = _random.Next(0, 50)
                }
            };
        }

        /// <summary>
        /// Creates test monitoring alert data.
        /// </summary>
        public TestMonitoringAlert CreateMonitoringAlert(string alertType = "Performance", string severity = "Warning")
        {
            return new TestMonitoringAlert
            {
                AlertId = Guid.NewGuid().ToString(),
                AlertType = alertType,
                Severity = severity,
                Message = $"Test {alertType.ToLower()} alert - {severity}",
                ServiceName = $"TestService_{_random.Next(1, 10)}",
                Timestamp = DateTime.UtcNow,
                IsActive = true,
                Threshold = _random.NextDouble() * 100,
                CurrentValue = _random.NextDouble() * 150,
                Tags = new Dictionary<string, string>
                {
                    ["environment"] = "test",
                    ["component"] = alertType.ToLower(),
                    ["automated"] = "true"
                },
                Actions = new List<string> { "log", "notify", "escalate" }
            };
        }

        #endregion

        #region Security and Enclave Data

        /// <summary>
        /// Creates test enclave data.
        /// </summary>
        public TestEnclaveData CreateEnclaveData(string? enclaveId = null)
        {
            return new TestEnclaveData
            {
                EnclaveId = enclaveId ?? Guid.NewGuid().ToString(),
                AttestationReport = GenerateAttestationReport(),
                Quote = GenerateSecureToken(128),
                MrEnclave = GenerateSecureToken(32),
                MrSigner = GenerateSecureToken(32),
                IsvProdId = _random.Next(1, 1000),
                IsvSvn = _random.Next(1, 100),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Status = "Active",
                SecurityLevel = "High",
                Capabilities = new List<string> { "DataSealing", "RemoteAttestation", "SecureStorage" },
                Metadata = new Dictionary<string, object>
                {
                    ["platform"] = "SGX",
                    ["version"] = "2.0",
                    ["debug"] = false
                }
            };
        }

        /// <summary>
        /// Creates test security audit log entry.
        /// </summary>
        public TestSecurityAuditLog CreateSecurityAuditLog(string action, string? userId = null)
        {
            return new TestSecurityAuditLog
            {
                LogId = Guid.NewGuid().ToString(),
                UserId = userId ?? Guid.NewGuid().ToString(),
                Action = action,
                Resource = $"test_resource_{_random.Next(1, 100)}",
                Timestamp = DateTime.UtcNow,
                SourceIp = $"192.168.1.{_random.Next(1, 255)}",
                UserAgent = "TestAgent/1.0",
                Success = _random.NextDouble() > 0.1, // 90% success rate
                RiskScore = _random.Next(0, 100),
                Details = new Dictionary<string, object>
                {
                    ["sessionId"] = Guid.NewGuid().ToString(),
                    ["requestId"] = Guid.NewGuid().ToString(),
                    ["method"] = "POST",
                    ["endpoint"] = $"/api/test/{action.ToLower()}"
                }
            };
        }

        #endregion

        #region Helper Methods

        private string GeneratePasswordHash(string password)
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "test_salt"));
            return Convert.ToBase64String(hashedBytes);
        }

        private string GenerateSecureToken(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        private string GenerateBlockchainAddress()
        {
            return $"N{GenerateSecureToken(33)}";
        }

        private string GenerateContractHash()
        {
            return $"0x{GenerateSecureToken(40).ToLower()}";
        }

        private string GenerateTransactionHash()
        {
            return $"0x{GenerateSecureToken(64).ToLower()}";
        }

        private string GenerateSignature(string data, string privateKey)
        {
            // Simplified signature generation for testing
            var dataHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data + privateKey));
            return Convert.ToBase64String(dataHash);
        }

        private string GenerateTestContractScript()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"test_contract_script_{_random.Next(1000, 9999)}"));
        }

        private Dictionary<string, object> CreateContractManifest()
        {
            return new Dictionary<string, object>
            {
                ["name"] = $"TestContract_{_random.Next(1000, 9999)}",
                ["groups"] = new object[] { },
                ["supportedstandards"] = new[] { "NEP-17" },
                ["abi"] = new
                {
                    methods = new[]
                    {
                        new { name = "balanceOf", parameters = new[] { new { name = "account", type = "Hash160" } } },
                        new { name = "transfer", parameters = new[] { 
                            new { name = "from", type = "Hash160" },
                            new { name = "to", type = "Hash160" },
                            new { name = "amount", type = "Integer" }
                        }}
                    },
                    events = new[]
                    {
                        new { name = "Transfer", parameters = new[] {
                            new { name = "from", type = "Hash160" },
                            new { name = "to", type = "Hash160" },
                            new { name = "amount", type = "Integer" }
                        }}
                    }
                },
                ["permissions"] = new[]
                {
                    new { contract = "*", methods = new[] { "onNEP17Payment" } }
                },
                ["trusts"] = new object[] { },
                ["extra"] = new { author = "TestFramework", email = "test@example.com" }
            };
        }

        private string GenerateAttestationReport()
        {
            var report = new
            {
                version = 4,
                sign_type = 1,
                epid_group_id = GenerateSecureToken(8),
                qe_svn = _random.Next(1, 100),
                pce_svn = _random.Next(1, 100),
                xeid = _random.Next(1, 1000),
                basename = GenerateSecureToken(32),
                report_body = new
                {
                    cpu_svn = GenerateSecureToken(32),
                    misc_select = _random.Next(1, 100),
                    attributes = GenerateSecureToken(32),
                    mr_enclave = GenerateSecureToken(64),
                    mr_signer = GenerateSecureToken(64),
                    isv_prod_id = _random.Next(1, 1000),
                    isv_svn = _random.Next(1, 100),
                    report_data = GenerateSecureToken(128)
                }
            };

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(report)));
        }

        #endregion

        #region Bulk Data Generation

        /// <summary>
        /// Creates multiple test users for bulk testing.
        /// </summary>
        public List<TestUserData> CreateMultipleUsers(int count, string? baseUsername = null)
        {
            var users = new List<TestUserData>();
            for (int i = 0; i < count; i++)
            {
                var username = baseUsername != null ? $"{baseUsername}_{i}" : null;
                users.Add(CreateTestUser(username));
            }
            return users;
        }

        /// <summary>
        /// Creates multiple test transactions for performance testing.
        /// </summary>
        public List<TestTransaction> CreateMultipleTransactions(int count, string? fromAddress = null, string? toAddress = null)
        {
            var transactions = new List<TestTransaction>();
            for (int i = 0; i < count; i++)
            {
                transactions.Add(CreateTransaction(fromAddress, toAddress, _random.Next(1, 1000)));
            }
            return transactions;
        }

        /// <summary>
        /// Creates test data set for complex integration scenarios.
        /// </summary>
        public CompleteTestDataSet CreateCompleteDataSet(string scenarioName)
        {
            var user = CreateTestUser();
            var keyPair = CreateKeyPair(userId: user.UserId);
            var account = CreateBlockchainAccount();
            var contract = CreateSmartContract(author: user.Username);
            var transaction = CreateTransaction(account.Address, GenerateBlockchainAddress());

            return new CompleteTestDataSet
            {
                ScenarioName = scenarioName,
                User = user,
                AuthToken = CreateAuthToken(user.UserId),
                KeyPair = keyPair,
                Account = account,
                Contract = contract,
                Transaction = transaction,
                ServiceConfig = CreateServiceConfig("TestService"),
                PerformanceMetrics = CreatePerformanceMetrics($"{scenarioName}_operation"),
                EnclaveData = CreateEnclaveData(),
                CreatedAt = DateTime.UtcNow
            };
        }

        #endregion
    }
}
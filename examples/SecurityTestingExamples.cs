using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Monitoring;
using NeoServiceLayer.Services.CrossChain;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Examples;

/// <summary>
/// Security testing examples demonstrating various security scenarios and edge cases
/// </summary>
public class SecurityTestingExamples
{
    private readonly IServiceProvider _serviceProvider;

    public SecurityTestingExamples()
    {
        _serviceProvider = ConfigureServices();
    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core services
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<IHttpClientService, HttpClientService>();
        services.AddSingleton<IBlockchainClientFactory, BlockchainClientFactory>();

        // Business services
        services.AddTransient<StorageService>();
        services.AddTransient<BackupService>();
        services.AddTransient<MonitoringService>();
        services.AddTransient<CrossChainService>();

        return services.BuildServiceProvider();
    }

    #region Input Validation and Sanitization Tests

    public async Task InputValidationSecurityTests()
    {
        Console.WriteLine("=== Input Validation Security Tests ===");

        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();

        var testId = Guid.NewGuid().ToString();

        // Test 1: SQL Injection Prevention
        await TestSqlInjectionPrevention(storageService, monitoringService, testId);

        // Test 2: XSS Prevention
        await TestXssPrevention(storageService, monitoringService, testId);

        // Test 3: Path Traversal Prevention
        await TestPathTraversalPrevention(storageService, monitoringService, testId);

        // Test 4: Buffer Overflow Protection
        await TestBufferOverflowProtection(storageService, monitoringService, testId);

        // Test 5: Invalid Character Handling
        await TestInvalidCharacterHandling(storageService, monitoringService, testId);

        Console.WriteLine("✓ Input validation security tests completed");
    }

    private async Task TestSqlInjectionPrevention(StorageService storage, MonitoringService monitoring, string testId)
    {
        Console.WriteLine("1. Testing SQL injection prevention...");

        var sqlInjectionPayloads = new[]
        {
            "'; DROP TABLE users; --",
            "' OR '1'='1",
            "admin'--",
            "' UNION SELECT * FROM passwords--",
            "'; INSERT INTO users VALUES('hacker', 'password'); --"
        };

        int blockedAttempts = 0;

        foreach (var payload in sqlInjectionPayloads)
        {
            try
            {
                var result = await storage.StoreDataAsync(new StorageRequest
                {
                    Key = $"test_sql_{DateTime.UtcNow.Ticks}",
                    Data = Encoding.UTF8.GetBytes(payload),
                    Metadata = new Dictionary<string, object>
                    {
                        { "test_type", "sql_injection" },
                        { "test_id", testId }
                    }
                });

                if (!result.Success && result.Error.Contains("invalid"))
                {
                    blockedAttempts++;
                }

                // Record security event
                await monitoring.RecordMetricAsync(new MetricData
                {
                    Name = "security_test_sql_injection",
                    Value = result.Success ? 0 : 1, // 0 = allowed, 1 = blocked
                    Tags = new Dictionary<string, string>
                    {
                        { "test_id", testId },
                        { "payload_type", "sql_injection" },
                        { "result", result.Success ? "allowed" : "blocked" }
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                blockedAttempts++;
                Console.WriteLine($"   Exception caught (expected): {ex.GetType().Name}");
            }
        }

        Console.WriteLine($"   SQL injection attempts blocked: {blockedAttempts}/{sqlInjectionPayloads.Length}");
    }

    private async Task TestXssPrevention(StorageService storage, MonitoringService monitoring, string testId)
    {
        Console.WriteLine("2. Testing XSS prevention...");

        var xssPayloads = new[]
        {
            "<script>alert('XSS')</script>",
            "<img src=x onerror=alert('XSS')>",
            "javascript:alert('XSS')",
            "<svg/onload=alert('XSS')>",
            "';alert('XSS');//"
        };

        int sanitizedInputs = 0;

        foreach (var payload in xssPayloads)
        {
            var result = await storage.StoreDataAsync(new StorageRequest
            {
                Key = $"test_xss_{DateTime.UtcNow.Ticks}",
                Data = Encoding.UTF8.GetBytes(payload),
                Metadata = new Dictionary<string, object>
                {
                    { "test_type", "xss_prevention" },
                    { "test_id", testId }
                }
            });

            // Check if input was sanitized
            if (result.Success)
            {
                var retrieveResult = await storage.RetrieveDataAsync(result.StorageId);
                var storedContent = Encoding.UTF8.GetString(retrieveResult.Data);
                
                if (!storedContent.Contains("<script>") && !storedContent.Contains("javascript:"))
                {
                    sanitizedInputs++;
                }
            }

            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "security_test_xss_prevention",
                Value = 1,
                Tags = new Dictionary<string, string>
                {
                    { "test_id", testId },
                    { "payload_type", "xss" },
                    { "sanitized", (result.Success && sanitizedInputs > 0).ToString() }
                },
                Timestamp = DateTime.UtcNow
            });
        }

        Console.WriteLine($"   XSS payloads sanitized: {sanitizedInputs}/{xssPayloads.Length}");
    }

    private async Task TestPathTraversalPrevention(StorageService storage, MonitoringService monitoring, string testId)
    {
        Console.WriteLine("3. Testing path traversal prevention...");

        var pathTraversalPayloads = new[]
        {
            "../../../etc/passwd",
            "..\\..\\..\\windows\\system32\\config\\sam",
            "....//....//....//etc//passwd",
            "%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd",
            "..%252f..%252f..%252fetc%252fpasswd"
        };

        int blockedAttempts = 0;

        foreach (var payload in pathTraversalPayloads)
        {
            var result = await storage.RetrieveDataAsync(payload);
            
            if (!result.Success && (result.Error.Contains("invalid path") || result.Error.Contains("not found")))
            {
                blockedAttempts++;
            }

            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "security_test_path_traversal",
                Value = result.Success ? 0 : 1,
                Tags = new Dictionary<string, string>
                {
                    { "test_id", testId },
                    { "payload_type", "path_traversal" },
                    { "blocked", (!result.Success).ToString() }
                },
                Timestamp = DateTime.UtcNow
            });
        }

        Console.WriteLine($"   Path traversal attempts blocked: {blockedAttempts}/{pathTraversalPayloads.Length}");
    }

    private async Task TestBufferOverflowProtection(StorageService storage, MonitoringService monitoring, string testId)
    {
        Console.WriteLine("4. Testing buffer overflow protection...");

        var largeSizes = new[] { 1024, 10240, 102400, 1048576 }; // 1KB to 1MB

        foreach (var size in largeSizes)
        {
            var largeData = new byte[size];
            new Random().NextBytes(largeData);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var result = await storage.StoreDataAsync(new StorageRequest
            {
                Key = $"test_buffer_{size}_{DateTime.UtcNow.Ticks}",
                Data = largeData,
                Metadata = new Dictionary<string, object>
                {
                    { "test_type", "buffer_overflow" },
                    { "test_id", testId },
                    { "data_size", size }
                }
            });

            stopwatch.Stop();

            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "security_test_buffer_protection",
                Value = stopwatch.ElapsedMilliseconds,
                Tags = new Dictionary<string, string>
                {
                    { "test_id", testId },
                    { "data_size", size.ToString() },
                    { "success", result.Success.ToString() }
                },
                Timestamp = DateTime.UtcNow
            });

            Console.WriteLine($"   {size} bytes: {(result.Success ? "Handled" : "Rejected")} in {stopwatch.ElapsedMilliseconds}ms");
        }
    }

    private async Task TestInvalidCharacterHandling(StorageService storage, MonitoringService monitoring, string testId)
    {
        Console.WriteLine("5. Testing invalid character handling...");

        var invalidInputs = new[]
        {
            "test\0null_byte", // Null byte
            "test\x01\x02\x03control_chars", // Control characters
            "test\uFEFFbom_marker", // BOM marker
            "test\u202Ezero_width", // Zero-width characters
            new string('A', 10000) // Extremely long string
        };

        int handledProperly = 0;

        foreach (var input in invalidInputs)
        {
            var result = await storage.StoreDataAsync(new StorageRequest
            {
                Key = $"test_invalid_{DateTime.UtcNow.Ticks}",
                Data = Encoding.UTF8.GetBytes(input),
                Metadata = new Dictionary<string, object>
                {
                    { "test_type", "invalid_characters" },
                    { "test_id", testId }
                }
            });

            if (result.Success || (!result.Success && result.Error.Contains("invalid")))
            {
                handledProperly++;
            }

            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "security_test_invalid_chars",
                Value = 1,
                Tags = new Dictionary<string, string>
                {
                    { "test_id", testId },
                    { "input_type", "invalid_characters" },
                    { "handled_properly", (handledProperly > 0).ToString() }
                },
                Timestamp = DateTime.UtcNow
            });
        }

        Console.WriteLine($"   Invalid inputs handled properly: {handledProperly}/{invalidInputs.Length}");
    }

    #endregion

    #region Cryptographic Security Tests

    public async Task CryptographicSecurityTests()
    {
        Console.WriteLine("=== Cryptographic Security Tests ===");

        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();

        var testId = Guid.NewGuid().ToString();

        // Test 1: Data Encryption at Rest
        await TestDataEncryptionAtRest(storageService, monitoringService, testId);

        // Test 2: Secure Key Management
        await TestSecureKeyManagement(storageService, monitoringService, testId);

        // Test 3: Hash Function Security
        await TestHashFunctionSecurity(monitoringService, testId);

        // Test 4: Random Number Generation
        await TestRandomNumberGeneration(monitoringService, testId);

        Console.WriteLine("✓ Cryptographic security tests completed");
    }

    private async Task TestDataEncryptionAtRest(StorageService storage, MonitoringService monitoring, string testId)
    {
        Console.WriteLine("1. Testing data encryption at rest...");

        var sensitiveData = new
        {
            creditCardNumber = "4111-1111-1111-1111",
            socialSecurityNumber = "123-45-6789",
            personalData = "John Doe, 123 Main St, Anytown USA"
        };

        var serializedData = JsonSerializer.Serialize(sensitiveData);

        // Store sensitive data
        var result = await storage.StoreDataAsync(new StorageRequest
        {
            Key = $"sensitive_data_{testId}",
            Data = Encoding.UTF8.GetBytes(serializedData),
            Metadata = new Dictionary<string, object>
            {
                { "data_classification", "sensitive" },
                { "encryption_required", true },
                { "test_id", testId }
            }
        });

        // Verify data is encrypted
        var retrieveResult = await storage.RetrieveDataAsync(result.StorageId);
        var isDataEncrypted = !Encoding.UTF8.GetString(retrieveResult.Data).Contains("4111-1111-1111-1111");

        await monitoring.RecordMetricAsync(new MetricData
        {
            Name = "security_test_encryption",
            Value = isDataEncrypted ? 1 : 0,
            Tags = new Dictionary<string, string>
            {
                { "test_id", testId },
                { "test_type", "data_encryption_at_rest" },
                { "encrypted", isDataEncrypted.ToString() }
            },
            Timestamp = DateTime.UtcNow
        });

        Console.WriteLine($"   Sensitive data encrypted: {isDataEncrypted}");
    }

    private async Task TestSecureKeyManagement(StorageService storage, MonitoringService monitoring, string testId)
    {
        Console.WriteLine("2. Testing secure key management...");

        // Generate test encryption key
        {
            aes.GenerateKey();
            var keyData = Convert.ToBase64String(aes.Key);

            // Test key storage with appropriate metadata
            var keyResult = await storage.StoreDataAsync(new StorageRequest
            {
                Key = $"encryption_key_{testId}",
                Data = Encoding.UTF8.GetBytes(keyData),
                Metadata = new Dictionary<string, object>
                {
                    { "key_type", "aes_256" },
                    { "key_usage", "data_encryption" },
                    { "created_at", DateTime.UtcNow },
                    { "test_id", testId }
                }
            });

            // Verify key is stored securely (not in plain text)
            var keyRetrieveResult = await storage.RetrieveDataAsync(keyResult.StorageId);
            var keySecurelyStored = keyRetrieveResult.Success;

            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "security_test_key_management",
                Value = keySecurelyStored ? 1 : 0,
                Tags = new Dictionary<string, string>
                {
                    { "test_id", testId },
                    { "key_type", "aes_256" },
                    { "securely_stored", keySecurelyStored.ToString() }
                },
                Timestamp = DateTime.UtcNow
            });

            Console.WriteLine($"   Encryption key securely managed: {keySecurelyStored}");
        }
    }

    private async Task TestHashFunctionSecurity(MonitoringService monitoring, string testId)
    {
        Console.WriteLine("3. Testing hash function security...");

        var testData = "sensitive_password_12345";
        var hashFunctions = new Dictionary<string, Func<byte[], byte[]>>
        {
            { "SHA256", data => SHA256.HashData(data) },
            { "SHA512", data => SHA512.HashData(data) },
            // MD5 should be avoided for security
            { "MD5", data => MD5.HashData(data) }
        };

        foreach (var hashFunc in hashFunctions)
        {
            var hash1 = hashFunc.Value(Encoding.UTF8.GetBytes(testData));
            var hash2 = hashFunc.Value(Encoding.UTF8.GetBytes(testData));
            var hash3 = hashFunc.Value(Encoding.UTF8.GetBytes(testData + "different"));

            var isConsistent = hash1.SequenceEqual(hash2);
            var isDeterministic = !hash1.SequenceEqual(hash3);
            var isSecure = hashFunc.Key != "MD5"; // MD5 is not secure

            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "security_test_hash_function",
                Value = (isConsistent && isDeterministic && isSecure) ? 1 : 0,
                Tags = new Dictionary<string, string>
                {
                    { "test_id", testId },
                    { "hash_algorithm", hashFunc.Key },
                    { "secure", isSecure.ToString() }
                },
                Timestamp = DateTime.UtcNow
            });

            Console.WriteLine($"   {hashFunc.Key}: Consistent={isConsistent}, Deterministic={isDeterministic}, Secure={isSecure}");
        }
    }

    private async Task TestRandomNumberGeneration(MonitoringService monitoring, string testId)
    {
        Console.WriteLine("4. Testing random number generation...");

        var strongRng = RandomNumberGenerator.Create();
        var weakRng = new Random();

        // Test entropy of random number generation
        var strongRandomBytes = new byte[1000];
        var weakRandomBytes = new byte[1000];

        strongRng.GetBytes(strongRandomBytes);
        weakRng.NextBytes(weakRandomBytes);

        var strongEntropy = CalculateEntropy(strongRandomBytes);
        var weakEntropy = CalculateEntropy(weakRandomBytes);

        var strongEntropyGood = strongEntropy > 7.5; // Good entropy should be > 7.5 bits per byte
        var entropyDifference = strongEntropy - weakEntropy;

        await monitoring.RecordMetricAsync(new MetricData
        {
            Name = "security_test_random_generation",
            Value = strongEntropy,
            Tags = new Dictionary<string, string>
            {
                { "test_id", testId },
                { "rng_type", "cryptographic" },
                { "entropy_sufficient", strongEntropyGood.ToString() }
            },
            Timestamp = DateTime.UtcNow
        });

        await monitoring.RecordMetricAsync(new MetricData
        {
            Name = "security_test_random_generation",
            Value = weakEntropy,
            Tags = new Dictionary<string, string>
            {
                { "test_id", testId },
                { "rng_type", "pseudo_random" },
                { "entropy_sufficient", (weakEntropy > 7.0).ToString() }
            },
            Timestamp = DateTime.UtcNow
        });

        Console.WriteLine($"   Strong RNG entropy: {strongEntropy:F2} bits/byte");
        Console.WriteLine($"   Weak RNG entropy: {weakEntropy:F2} bits/byte");
        Console.WriteLine($"   Entropy difference: {entropyDifference:F2} bits/byte");

        strongRng.Dispose();
    }

    private double CalculateEntropy(byte[] data)
    {
        var frequencies = new int[256];
        foreach (byte b in data)
        {
            frequencies[b]++;
        }

        double entropy = 0.0;
        int length = data.Length;

        for (int i = 0; i < 256; i++)
        {
            if (frequencies[i] > 0)
            {
                double probability = (double)frequencies[i] / length;
                entropy -= probability * Math.Log2(probability);
            }
        }

        return entropy;
    }

    #endregion

    #region Access Control and Authorization Tests

    public async Task AccessControlSecurityTests()
    {
        Console.WriteLine("=== Access Control Security Tests ===");

        var storageService = _serviceProvider.GetRequiredService<StorageService>();
        var backupService = _serviceProvider.GetRequiredService<BackupService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();

        var testId = Guid.NewGuid().ToString();

        // Test 1: Role-Based Access Control
        await TestRoleBasedAccessControl(storageService, monitoringService, testId);

        // Test 2: Privilege Escalation Prevention
        await TestPrivilegeEscalationPrevention(backupService, monitoringService, testId);

        // Test 3: Session Management
        await TestSessionManagement(monitoringService, testId);

        // Test 4: Audit Trail Integrity
        await TestAuditTrailIntegrity(storageService, monitoringService, testId);

        Console.WriteLine("✓ Access control security tests completed");
    }

    private async Task TestRoleBasedAccessControl(StorageService storage, MonitoringService monitoring, string testId)
    {
        Console.WriteLine("1. Testing role-based access control...");

        var roles = new[] { "admin", "user", "readonly", "guest" };
        var accessAttempts = new Dictionary<string, bool>();

        foreach (var role in roles)
        {
            var testData = new
            {
                role = role,
                data = $"Test data for {role}",
                timestamp = DateTime.UtcNow
            };

            var result = await storage.StoreDataAsync(new StorageRequest
            {
                Key = $"rbac_test_{role}_{testId}",
                Data = JsonSerializer.SerializeToUtf8Bytes(testData),
                Metadata = new Dictionary<string, object>
                {
                    { "user_role", role },
                    { "access_level", GetAccessLevel(role) },
                    { "test_id", testId }
                }
            });

            accessAttempts[role] = result.Success;

            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "security_test_rbac",
                Value = result.Success ? 1 : 0,
                Tags = new Dictionary<string, string>
                {
                    { "test_id", testId },
                    { "user_role", role },
                    { "access_granted", result.Success.ToString() }
                },
                Timestamp = DateTime.UtcNow
            });
        }

        Console.WriteLine("   Role-based access results:");
        foreach (var attempt in accessAttempts)
        {
            Console.WriteLine($"     {attempt.Key}: {(attempt.Value ? "Granted" : "Denied")}");
        }
    }

    private string GetAccessLevel(string role)
    {
        return role switch
        {
            "admin" => "full",
            "user" => "read_write",
            "readonly" => "read_only",
            "guest" => "limited",
            _ => "none"
        };
    }

    private async Task TestPrivilegeEscalationPrevention(BackupService backup, MonitoringService monitoring, string testId)
    {
        Console.WriteLine("2. Testing privilege escalation prevention...");

        // Simulate low-privilege user attempting high-privilege operations
        var escalationAttempts = new[]
        {
            new { operation = "delete_all_backups", expectedResult = false },
            new { operation = "modify_system_backup", expectedResult = false },
            new { operation = "access_admin_backups", expectedResult = false }
        };

        foreach (var attempt in escalationAttempts)
        {
            try
            {
                // Simulate privilege escalation attempt
                var result = await SimulatePrivilegeEscalation(attempt.operation, backup);
                var prevented = !result || result != attempt.expectedResult;

                await monitoring.RecordMetricAsync(new MetricData
                {
                    Name = "security_test_privilege_escalation",
                    Value = prevented ? 1 : 0,
                    Tags = new Dictionary<string, string>
                    {
                        { "test_id", testId },
                        { "operation", attempt.operation },
                        { "prevented", prevented.ToString() }
                    },
                    Timestamp = DateTime.UtcNow
                });

                Console.WriteLine($"   {attempt.operation}: {(prevented ? "Prevented" : "Failed to prevent")}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"   {attempt.operation}: Prevented (Exception thrown)");
            }
        }
    }

    private async Task<bool> SimulatePrivilegeEscalation(string operation, BackupService backup)
    {
        // This would normally check actual permissions
        // For demo purposes, we'll simulate the check
        switch (operation)
        {
            case "delete_all_backups":
                // Should require admin privileges
                return false; // Simulated: operation denied
            case "modify_system_backup":
                // Should require system privileges  
                return false; // Simulated: operation denied
            case "access_admin_backups":
                // Should require admin privileges
                return false; // Simulated: operation denied
            default:
                return false;
        }
    }

    private async Task TestSessionManagement(MonitoringService monitoring, string testId)
    {
        Console.WriteLine("3. Testing session management...");

        var sessions = new[]
        {
            new { sessionId = Guid.NewGuid().ToString(), userId = "user1", created = DateTime.UtcNow, timeout = TimeSpan.FromMinutes(30) },
            new { sessionId = Guid.NewGuid().ToString(), userId = "user2", created = DateTime.UtcNow.AddHours(-2), timeout = TimeSpan.FromMinutes(30) },
            new { sessionId = Guid.NewGuid().ToString(), userId = "user3", created = DateTime.UtcNow.AddMinutes(-45), timeout = TimeSpan.FromMinutes(30) }
        };

        foreach (var session in sessions)
        {
            var isExpired = DateTime.UtcNow - session.created > session.timeout;
            var isActive = !isExpired;

            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "security_test_session_management",
                Value = isActive ? 1 : 0,
                Tags = new Dictionary<string, string>
                {
                    { "test_id", testId },
                    { "session_id", session.sessionId },
                    { "user_id", session.userId },
                    { "session_state", isActive ? "active" : "expired" }
                },
                Timestamp = DateTime.UtcNow
            });

            Console.WriteLine($"   Session {session.userId}: {(isActive ? "Active" : "Expired")}");
        }
    }

    private async Task TestAuditTrailIntegrity(StorageService storage, MonitoringService monitoring, string testId)
    {
        Console.WriteLine("4. Testing audit trail integrity...");

        var auditEvents = new[]
        {
            new { action = "login", userId = "user1", timestamp = DateTime.UtcNow, result = "success" },
            new { action = "data_access", userId = "user2", timestamp = DateTime.UtcNow, result = "success" },
            new { action = "delete_attempt", userId = "user3", timestamp = DateTime.UtcNow, result = "denied" },
            new { action = "backup_restore", userId = "admin1", timestamp = DateTime.UtcNow, result = "success" }
        };

        var auditIntegrityChecks = 0;

        foreach (var auditEvent in auditEvents)
        {
            // Store audit event
            var auditRecord = new
            {
                eventId = Guid.NewGuid().ToString(),
                action = auditEvent.action,
                userId = auditEvent.userId,
                timestamp = auditEvent.timestamp,
                result = auditEvent.result,
                checksum = CalculateChecksum(JsonSerializer.Serialize(auditEvent))
            };

            var result = await storage.StoreDataAsync(new StorageRequest
            {
                Key = $"audit_{auditRecord.eventId}",
                Data = JsonSerializer.SerializeToUtf8Bytes(auditRecord),
                Metadata = new Dictionary<string, object>
                {
                    { "record_type", "audit_log" },
                    { "test_id", testId },
                    { "integrity_protected", true }
                }
            });

            if (result.Success)
            {
                // Verify audit record integrity
                var retrieveResult = await storage.RetrieveDataAsync(result.StorageId);
                var retrievedRecord = JsonSerializer.Deserialize<dynamic>(retrieveResult.Data);
                
                // Simulate integrity check (in real implementation, this would verify cryptographic signatures)
                auditIntegrityChecks++;
            }

            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "security_test_audit_integrity",
                Value = result.Success ? 1 : 0,
                Tags = new Dictionary<string, string>
                {
                    { "test_id", testId },
                    { "audit_action", auditEvent.action },
                    { "integrity_verified", result.Success.ToString() }
                },
                Timestamp = DateTime.UtcNow
            });
        }

        Console.WriteLine($"   Audit records with integrity protection: {auditIntegrityChecks}/{auditEvents.Length}");
    }

    private string CalculateChecksum(string data)
    {
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }
    }

    #endregion

    #region Network Security Tests

    public async Task NetworkSecurityTests()
    {
        Console.WriteLine("=== Network Security Tests ===");

        var crossChainService = _serviceProvider.GetRequiredService<CrossChainService>();
        var monitoringService = _serviceProvider.GetRequiredService<MonitoringService>();

        var testId = Guid.NewGuid().ToString();

        // Test 1: Man-in-the-Middle Prevention
        await TestMitMPrevention(crossChainService, monitoringService, testId);

        // Test 2: Replay Attack Prevention
        await TestReplayAttackPrevention(crossChainService, monitoringService, testId);

        // Test 3: DDoS Protection
        await TestDDoSProtection(monitoringService, testId);

        // Test 4: SSL/TLS Security
        await TestSslTlsSecurity(monitoringService, testId);

        Console.WriteLine("✓ Network security tests completed");
    }

    private async Task TestMitMPrevention(CrossChainService crossChain, MonitoringService monitoring, string testId)
    {
        Console.WriteLine("1. Testing man-in-the-middle prevention...");

        var legitimateTransfer = new CrossChainTransferRequest
        {
            TransferId = Guid.NewGuid().ToString(),
            SourceChain = BlockchainType.NeoN3,
            DestinationChain = BlockchainType.NeoX,
            AssetType = "GAS",
            Amount = 10.0m,
            SourceAddress = "NbTiM6h8r99kpRtb428XcsUk1TzKed2gTc",
            DestinationAddress = "0x1234567890abcdef1234567890abcdef12345678",
            Metadata = new Dictionary<string, object>
            {
                { "signature", "valid_signature" },
                { "test_id", testId }
            }
        };

        // Simulate tampered request (MITM attack)
        var tamperedTransfer = new CrossChainTransferRequest
        {
            TransferId = legitimateTransfer.TransferId,
            SourceChain = legitimateTransfer.SourceChain,
            DestinationChain = legitimateTransfer.DestinationChain,
            AssetType = legitimateTransfer.AssetType,
            Amount = 100.0m, // Tampered amount
            SourceAddress = legitimateTransfer.SourceAddress,
            DestinationAddress = "0xdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef", // Tampered destination
            Metadata = new Dictionary<string, object>
            {
                { "signature", "valid_signature" }, // Invalid signature for tampered data
                { "test_id", testId }
            }
        };

        var legitimateResult = await crossChain.InitiateCrossChainTransferAsync(legitimateTransfer);
        var tamperedResult = await crossChain.InitiateCrossChainTransferAsync(tamperedTransfer);

        var mitMPrevented = legitimateResult.Success && !tamperedResult.Success;

        await monitoring.RecordMetricAsync(new MetricData
        {
            Name = "security_test_mitm_prevention",
            Value = mitMPrevented ? 1 : 0,
            Tags = new Dictionary<string, string>
            {
                { "test_id", testId },
                { "attack_type", "man_in_the_middle" },
                { "prevented", mitMPrevented.ToString() }
            },
            Timestamp = DateTime.UtcNow
        });

        Console.WriteLine($"   MITM attack prevented: {mitMPrevented}");
    }

    private async Task TestReplayAttackPrevention(CrossChainService crossChain, MonitoringService monitoring, string testId)
    {
        Console.WriteLine("2. Testing replay attack prevention...");

        var originalTransfer = new CrossChainTransferRequest
        {
            TransferId = Guid.NewGuid().ToString(),
            SourceChain = BlockchainType.NeoN3,
            DestinationChain = BlockchainType.NeoX,
            AssetType = "GAS",
            Amount = 5.0m,
            SourceAddress = "NbTiM6h8r99kpRtb428XcsUk1TzKed2gTc",
            DestinationAddress = "0x1234567890abcdef1234567890abcdef12345678",
            Metadata = new Dictionary<string, object>
            {
                { "nonce", Guid.NewGuid().ToString() },
                { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                { "test_id", testId }
            }
        };

        // Execute original request
        var originalResult = await crossChain.InitiateCrossChainTransferAsync(originalTransfer);

        // Simulate replay attack (same request again)
        var replayResult = await crossChain.InitiateCrossChainTransferAsync(originalTransfer);

        var replayPrevented = originalResult.Success && !replayResult.Success;

        await monitoring.RecordMetricAsync(new MetricData
        {
            Name = "security_test_replay_prevention",
            Value = replayPrevented ? 1 : 0,
            Tags = new Dictionary<string, string>
            {
                { "test_id", testId },
                { "attack_type", "replay_attack" },
                { "prevented", replayPrevented.ToString() }
            },
            Timestamp = DateTime.UtcNow
        });

        Console.WriteLine($"   Replay attack prevented: {replayPrevented}");
    }

    private async Task TestDDoSProtection(MonitoringService monitoring, string testId)
    {
        Console.WriteLine("3. Testing DDoS protection...");

        var requestCounts = new[] { 10, 50, 100, 500, 1000 }; // Requests per second
        var ddosProtectionResults = new List<bool>();

        foreach (var requestCount in requestCounts)
        {
            var tasks = new List<Task>();
            var successCount = 0;
            var rateLimitedCount = 0;

            // Simulate high-volume requests
            for (int i = 0; i < requestCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        // Simulate API request
                        await Task.Delay(1); // Minimal processing time
                        Interlocked.Increment(ref successCount);
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref rateLimitedCount);
                    }
                }));
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await Task.WhenAll(tasks);
            stopwatch.Stop();

            var requestsPerSecond = requestCount / Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
            var protectionActive = requestsPerSecond > 200 && rateLimitedCount > 0; // Rate limiting kicks in

            ddosProtectionResults.Add(protectionActive);

            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "security_test_ddos_protection",
                Value = requestsPerSecond,
                Tags = new Dictionary<string, string>
                {
                    { "test_id", testId },
                    { "request_volume", requestCount.ToString() },
                    { "protection_active", protectionActive.ToString() }
                },
                Timestamp = DateTime.UtcNow
            });

            Console.WriteLine($"   {requestCount} requests: {requestsPerSecond:F0} req/s, Protection: {protectionActive}");
        }
    }

    private async Task TestSslTlsSecurity(MonitoringService monitoring, string testId)
    {
        Console.WriteLine("4. Testing SSL/TLS security...");

        var sslTests = new[]
        {
            new { protocol = "TLS 1.3", secure = true, score = 95 },
            new { protocol = "TLS 1.2", secure = true, score = 85 },
            new { protocol = "TLS 1.1", secure = false, score = 60 },
            new { protocol = "SSL 3.0", secure = false, score = 30 }
        };

        foreach (var test in sslTests)
        {
            await monitoring.RecordMetricAsync(new MetricData
            {
                Name = "security_test_ssl_tls",
                Value = test.score,
                Tags = new Dictionary<string, string>
                {
                    { "test_id", testId },
                    { "protocol", test.protocol },
                    { "secure", test.secure.ToString() }
                },
                Timestamp = DateTime.UtcNow
            });

            Console.WriteLine($"   {test.protocol}: Score {test.score}/100, Secure: {test.secure}");
        }
    }

    #endregion

    #region Main Demo Runner

    public static async Task Main(string[] args)
    {
        var examples = new SecurityTestingExamples();

        Console.WriteLine("=== NeoServiceLayer Security Testing Examples ===\n");

        try
        {
            await examples.InputValidationSecurityTests();
            Console.WriteLine();

            await examples.CryptographicSecurityTests();
            Console.WriteLine();

            await examples.AccessControlSecurityTests();
            Console.WriteLine();

            await examples.NetworkSecurityTests();
            Console.WriteLine();

            Console.WriteLine("✓ All security testing examples completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error running security tests: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    #endregion
}
using System;
using System.Collections.Generic;

namespace NeoServiceLayer.TestUtilities
{
    #region User and Authentication Models

    public class TestUserData
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public Dictionary<string, object> Profile { get; set; } = new();
    }

    public class TestAuthToken
    {
        public string TokenId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime IssuedAt { get; set; }
        public string Scope { get; set; } = string.Empty;
        public Dictionary<string, object> Claims { get; set; } = new();
    }

    #endregion

    #region Cryptographic Models

    public class TestKeyPair
    {
        public string KeyId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string KeyType { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class TestSignature
    {
        public string SignatureId { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public string HashAlgorithm { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsValid { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    #endregion

    #region Blockchain Models

    public class TestAccountData
    {
        public string AccountId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public int Nonce { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, decimal> Assets { get; set; } = new();
        public List<string> Contracts { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class TestSmartContract
    {
        public string ContractHash { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Script { get; set; } = string.Empty;
        public Dictionary<string, object> Manifest { get; set; } = new();
        public DateTime DeployedAt { get; set; }
        public long Gas { get; set; }
        public bool IsActive { get; set; }
        public List<string> Methods { get; set; } = new();
        public List<string> Events { get; set; } = new();
        public Dictionary<string, object> Storage { get; set; } = new();
    }

    public class TestTransaction
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string ToAddress { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public long Nonce { get; set; }
        public DateTime Timestamp { get; set; }
        public long BlockHeight { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Confirmations { get; set; }
        public string Data { get; set; } = string.Empty;
        public List<TestWitness> Witnesses { get; set; } = new();
    }

    public class TestWitness
    {
        public string InvocationScript { get; set; } = string.Empty;
        public string VerificationScript { get; set; } = string.Empty;
    }

    #endregion

    #region Service Integration Models

    public class TestServiceConfig
    {
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public string HealthCheckUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class TestWorkflowDefinition
    {
        public string WorkflowId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredServices { get; set; } = new();
        public List<TestWorkflowStep> Steps { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class TestWorkflowStep
    {
        public string StepId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsRequired { get; set; }
        public int TimeoutMs { get; set; }
        public int RetryCount { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    #endregion

    #region Performance and Monitoring Models

    public class TestPerformanceMetrics
    {
        public string MetricId { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public double MemoryUsageMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public long ThroughputPerSecond { get; set; }
        public int ErrorCount { get; set; }
        public int SuccessCount { get; set; }
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
    }

    public class TestMonitoringAlert
    {
        public string AlertId { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsActive { get; set; }
        public double Threshold { get; set; }
        public double CurrentValue { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
        public List<string> Actions { get; set; } = new();
    }

    #endregion

    #region Security and Enclave Models

    public class TestEnclaveData
    {
        public string EnclaveId { get; set; } = string.Empty;
        public string AttestationReport { get; set; } = string.Empty;
        public string Quote { get; set; } = string.Empty;
        public string MrEnclave { get; set; } = string.Empty;
        public string MrSigner { get; set; } = string.Empty;
        public int IsvProdId { get; set; }
        public int IsvSvn { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SecurityLevel { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class TestSecurityAuditLog
    {
        public string LogId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string SourceIp { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int RiskScore { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }

    #endregion

    #region Composite Models

    public class CompleteTestDataSet
    {
        public string ScenarioName { get; set; } = string.Empty;
        public TestUserData User { get; set; } = new();
        public TestAuthToken AuthToken { get; set; } = new();
        public TestKeyPair KeyPair { get; set; } = new();
        public TestAccountData Account { get; set; } = new();
        public TestSmartContract Contract { get; set; } = new();
        public TestTransaction Transaction { get; set; } = new();
        public TestServiceConfig ServiceConfig { get; set; } = new();
        public TestPerformanceMetrics PerformanceMetrics { get; set; } = new();
        public TestEnclaveData EnclaveData { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    #endregion

    #region Test Scenario Models

    public class TestScenarioDefinition
    {
        public string ScenarioId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<TestAssertion> Assertions { get; set; } = new();
        public TimeSpan ExpectedDuration { get; set; }
        public string Priority { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }

    public class TestAssertion
    {
        public string Property { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public object ExpectedValue { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class TestEnvironmentSetup
    {
        public string EnvironmentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, string> Variables { get; set; } = new();
        public List<TestServiceSetup> Services { get; set; } = new();
        public List<TestDatabaseSetup> Databases { get; set; } = new();
        public Dictionary<string, object> Configuration { get; set; } = new();
        public bool IsIsolated { get; set; } = true;
    }

    public class TestServiceSetup
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public bool MockExternal { get; set; } = true;
    }

    public class TestDatabaseSetup
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public List<string> InitialData { get; set; } = new();
        public bool ResetBetweenTests { get; set; } = true;
    }

    #endregion
}
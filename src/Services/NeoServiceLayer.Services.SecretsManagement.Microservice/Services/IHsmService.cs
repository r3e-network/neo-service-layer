namespace Neo.SecretsManagement.Service.Services;

public interface IHsmService
{
    Task<string> GenerateKeyAsync(string keyId, string algorithm, int keySize);
    Task<string> EncryptAsync(string plaintext, string keyId);
    Task<string> DecryptAsync(string ciphertext, string keyId);
    Task<string> SignAsync(string data, string keyId);
    Task<bool> VerifyAsync(string data, string signature, string keyId);
    Task<bool> RevokeKeyAsync(string keyId);
    Task<bool> ValidateKeyAsync(string keyId);
    Task<HsmStatus> GetStatusAsync();
    Task<List<string>> ListKeysAsync();
    Task<HsmKeyInfo?> GetKeyInfoAsync(string keyId);
    Task<bool> BackupKeyAsync(string keyId, string backupLocation);
    Task<bool> RestoreKeyAsync(string keyId, string backupLocation);
    Task<HsmPerformanceMetrics> GetPerformanceMetricsAsync();
}

public class HsmStatus
{
    public bool IsAvailable { get; set; }
    public string Version { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public int ActiveSlots { get; set; }
    public int TotalSlots { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public List<string> Alarms { get; set; } = new();
}

public class HsmKeyInfo
{
    public string KeyId { get; set; } = string.Empty;
    public string SlotId { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public int KeySize { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int UsageCount { get; set; }
}

public class HsmPerformanceMetrics
{
    public int OperationsPerSecond { get; set; }
    public double AverageResponseTime { get; set; }
    public int ActiveConnections { get; set; }
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public int ErrorRate { get; set; }
    public DateTime MeasuredAt { get; set; }
}
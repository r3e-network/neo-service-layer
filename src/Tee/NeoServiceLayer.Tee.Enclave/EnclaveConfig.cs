namespace NeoServiceLayer.Tee.Enclave;

/// <summary>
/// Configuration for SGX enclave settings.
/// </summary>
public class EnclaveConfig
{
    /// <summary>
    /// Gets or sets the SGX mode (SIM or HW).
    /// Default is HW for production, but can be overridden by environment variable.
    /// </summary>
    public string SgxMode { get; set; } = Environment.GetEnvironmentVariable("SGX_MODE") ?? "HW";

    /// <summary>
    /// Gets or sets whether debug mode is enabled.
    /// Should be false in production for security.
    /// </summary>
    public bool DebugMode { get; set; } = Environment.GetEnvironmentVariable("SGX_DEBUG") == "1";

    /// <summary>
    /// Gets or sets the enclave library path.
    /// </summary>
    public string? EnclavePath { get; set; }

    /// <summary>
    /// Gets or sets the SGX SDK path.
    /// </summary>
    public string? SgxSdkPath { get; set; }

    /// <summary>
    /// Gets or sets whether Occlum LibOS is enabled.
    /// </summary>
    public bool EnableOcclum { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of threads.
    /// </summary>
    public int MaxThreads { get; set; } = 10;

    /// <summary>
    /// Gets or sets the memory limit in MB.
    /// </summary>
    public long MemoryLimitMb { get; set; } = 512;
} 
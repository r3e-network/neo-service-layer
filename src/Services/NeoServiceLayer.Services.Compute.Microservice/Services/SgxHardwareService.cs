using Neo.Compute.Service.Services;
using System.Runtime.InteropServices;

namespace Neo.Compute.Service.Services;

public class SgxHardwareService : ISgxHardwareService
{
    private readonly ILogger<SgxHardwareService> _logger;
    private bool _sgxAvailable;
    private bool _initialized;
    private readonly Dictionary<string, object> _hardwareInfo = new();

    public SgxHardwareService(ILogger<SgxHardwareService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            _logger.LogInformation("Initializing SGX hardware detection...");
            
            // Detect SGX availability
            _sgxAvailable = await DetectSgxHardwareAsync();
            
            if (_sgxAvailable)
            {
                await CollectHardwareInfoAsync();
                _logger.LogInformation("SGX hardware detected and initialized successfully");
            }
            else
            {
                _logger.LogWarning("SGX hardware not available or not enabled");
            }
            
            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SGX hardware service");
            _sgxAvailable = false;
            _initialized = true;
        }
    }

    public async Task<bool> IsSgxAvailableAsync()
    {
        if (!_initialized)
            await InitializeAsync();
            
        return _sgxAvailable;
    }

    public async Task<bool> IsSgxEnabledInBiosAsync()
    {
        try
        {
            // Check SGX BIOS settings
            // This would typically involve reading CPU features
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return await CheckLinuxSgxSupportAsync();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await CheckWindowsSgxSupportAsync();
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check SGX BIOS settings");
            return false;
        }
    }

    public async Task<string> GetSgxVersionAsync()
    {
        if (!await IsSgxAvailableAsync())
            return "Not Available";
            
        return _hardwareInfo.TryGetValue("sgx_version", out var version) 
            ? version.ToString()! 
            : "Unknown";
    }

    public async Task<Dictionary<string, object>> GetHardwareInfoAsync()
    {
        if (!_initialized)
            await InitializeAsync();
            
        return new Dictionary<string, object>(_hardwareInfo);
    }

    public async Task<bool> CanCreateEnclaveAsync()
    {
        if (!await IsSgxAvailableAsync())
            return false;
            
        // Check if we have available resources to create a new enclave
        var maxEnclaves = await GetMaxEnclavesAsync();
        var availableMemory = await GetAvailableEpcMemoryAsync();
        
        return maxEnclaves > 0 && availableMemory > 0;
    }

    public async Task<int> GetMaxEnclavesAsync()
    {
        if (!await IsSgxAvailableAsync())
            return 0;
            
        return _hardwareInfo.TryGetValue("max_enclaves", out var max) 
            ? Convert.ToInt32(max) 
            : 0;
    }

    public async Task<long> GetAvailableEpcMemoryAsync()
    {
        if (!await IsSgxAvailableAsync())
            return 0;
            
        return _hardwareInfo.TryGetValue("available_epc_memory", out var memory) 
            ? Convert.ToInt64(memory) 
            : 0;
    }

    private async Task<bool> DetectSgxHardwareAsync()
    {
        try
        {
            // Check for SGX support in CPU features
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return await CheckLinuxSgxSupportAsync();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await CheckWindowsSgxSupportAsync();
            }
            
            // For other platforms or Docker environments, check environment variables
            var sgxEnabled = Environment.GetEnvironmentVariable("SGX_ENABLED");
            return !string.IsNullOrEmpty(sgxEnabled) && 
                   (sgxEnabled.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                    sgxEnabled.Equals("1"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect SGX hardware");
            return false;
        }
    }

    private async Task<bool> CheckLinuxSgxSupportAsync()
    {
        try
        {
            // Check /proc/cpuinfo for SGX flags
            if (File.Exists("/proc/cpuinfo"))
            {
                var cpuInfo = await File.ReadAllTextAsync("/proc/cpuinfo");
                if (cpuInfo.Contains("sgx"))
                {
                    _logger.LogDebug("SGX flag found in /proc/cpuinfo");
                    return true;
                }
            }

            // Check for SGX device nodes
            if (Directory.Exists("/dev") && 
                (File.Exists("/dev/sgx_enclave") || File.Exists("/dev/sgx/enclave")))
            {
                _logger.LogDebug("SGX device nodes found");
                return true;
            }

            // Check for SGX kernel module
            if (File.Exists("/proc/modules"))
            {
                var modules = await File.ReadAllTextAsync("/proc/modules");
                if (modules.Contains("intel_sgx"))
                {
                    _logger.LogDebug("Intel SGX kernel module loaded");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Linux SGX support");
            return false;
        }
    }

    private async Task<bool> CheckWindowsSgxSupportAsync()
    {
        try
        {
            // On Windows, SGX support would typically be checked through WMI or registry
            // For now, return false as this would require platform-specific implementation
            await Task.Delay(1); // Prevent compiler warning
            _logger.LogDebug("Windows SGX detection not implemented");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Windows SGX support");
            return false;
        }
    }

    private async Task CollectHardwareInfoAsync()
    {
        try
        {
            _hardwareInfo.Clear();
            
            // Collect basic hardware information
            _hardwareInfo["platform"] = RuntimeInformation.OSDescription;
            _hardwareInfo["architecture"] = RuntimeInformation.ProcessArchitecture.ToString();
            _hardwareInfo["sgx_version"] = "2.0"; // Default SGX version
            _hardwareInfo["max_enclaves"] = 32; // Default max enclaves
            _hardwareInfo["available_epc_memory"] = 128 * 1024 * 1024; // Default 128MB EPC
            _hardwareInfo["sgx_enabled"] = true;
            _hardwareInfo["last_updated"] = DateTime.UtcNow;

            // In a real implementation, this would query actual hardware
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                await CollectLinuxHardwareInfoAsync();
            }

            _logger.LogDebug("Hardware information collected: {HardwareInfo}", 
                string.Join(", ", _hardwareInfo.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect hardware information");
        }
    }

    private async Task CollectLinuxHardwareInfoAsync()
    {
        try
        {
            // Check SGX capabilities from sysfs or proc filesystem
            if (Directory.Exists("/sys/firmware/efi"))
            {
                _hardwareInfo["firmware_type"] = "UEFI";
            }
            else
            {
                _hardwareInfo["firmware_type"] = "BIOS";
            }

            // Read CPU information
            if (File.Exists("/proc/cpuinfo"))
            {
                var cpuInfo = await File.ReadAllTextAsync("/proc/cpuinfo");
                var lines = cpuInfo.Split('\n');
                
                foreach (var line in lines)
                {
                    if (line.StartsWith("model name"))
                    {
                        var modelName = line.Split(':')[1].Trim();
                        _hardwareInfo["cpu_model"] = modelName;
                        break;
                    }
                }
            }

            _logger.LogDebug("Linux hardware information collected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect Linux hardware information");
        }
    }
}
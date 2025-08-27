namespace Neo.Compute.Service.Services;

public interface ISgxHardwareService
{
    Task InitializeAsync();
    Task<bool> IsSgxAvailableAsync();
    Task<bool> IsSgxEnabledInBiosAsync();
    Task<string> GetSgxVersionAsync();
    Task<Dictionary<string, object>> GetHardwareInfoAsync();
    Task<bool> CanCreateEnclaveAsync();
    Task<int> GetMaxEnclavesAsync();
    Task<long> GetAvailableEpcMemoryAsync();
}
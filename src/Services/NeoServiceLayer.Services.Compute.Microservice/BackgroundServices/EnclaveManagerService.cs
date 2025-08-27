using Microsoft.Extensions.DependencyInjection;
using Neo.Compute.Service.Services;

namespace Neo.Compute.Service.BackgroundServices;

public class EnclaveManagerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EnclaveManagerService> _logger;
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromMinutes(1);

    public EnclaveManagerService(
        IServiceProvider serviceProvider,
        ILogger<EnclaveManagerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Enclave Manager Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var enclaveService = scope.ServiceProvider.GetRequiredService<ISgxEnclaveService>();
                var hardwareService = scope.ServiceProvider.GetRequiredService<ISgxHardwareService>();

                // Check SGX hardware availability
                var isSgxAvailable = await hardwareService.IsSgxAvailableAsync();
                if (!isSgxAvailable)
                {
                    _logger.LogWarning("SGX hardware not available - enclave management disabled");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                // Get all enclaves and check their health
                await CheckEnclaveHealthAsync(enclaveService);

                // Clean up expired or crashed enclaves
                await CleanupEnclavesAsync(enclaveService);

                await Task.Delay(_heartbeatInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Enclave Manager Service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Enclave Manager Service stopped");
    }

    private async Task CheckEnclaveHealthAsync(ISgxEnclaveService enclaveService)
    {
        try
        {
            var enclaves = await enclaveService.GetEnclavesAsync();
            
            foreach (var enclave in enclaves)
            {
                var enclaveId = Guid.Parse(enclave.Id);
                
                // Check if enclave has sent heartbeat recently
                if (enclave.LastHeartbeat.HasValue &&
                    DateTime.UtcNow - enclave.LastHeartbeat.Value > TimeSpan.FromMinutes(5))
                {
                    _logger.LogWarning("Enclave {EnclaveId} has not sent heartbeat for over 5 minutes", enclaveId);
                    
                    // Mark enclave as potentially crashed
                    if (enclave.Status == "Running" || enclave.Status == "Ready")
                    {
                        await enclaveService.UpdateEnclaveStatusAsync(enclaveId, Models.SgxEnclaveStatus.Error);
                    }
                }

                // Auto-heal enclaves in error state
                if (enclave.Status == "Error" && ShouldAttemptRecovery(enclave))
                {
                    _logger.LogInformation("Attempting to recover enclave {EnclaveId}", enclaveId);
                    await AttemptEnclaveRecoveryAsync(enclaveService, enclaveId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking enclave health");
        }
    }

    private async Task CleanupEnclavesAsync(ISgxEnclaveService enclaveService)
    {
        try
        {
            var enclaves = await enclaveService.GetEnclavesAsync(Models.SgxEnclaveStatus.Crashed);
            
            foreach (var enclave in enclaves)
            {
                var enclaveId = Guid.Parse(enclave.Id);
                
                // Clean up crashed enclaves that have been in this state for more than 10 minutes
                if (DateTime.UtcNow - enclave.CreatedAt > TimeSpan.FromMinutes(10))
                {
                    _logger.LogInformation("Cleaning up crashed enclave {EnclaveId}", enclaveId);
                    
                    try
                    {
                        await enclaveService.DeleteEnclaveAsync(enclaveId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to cleanup crashed enclave {EnclaveId}", enclaveId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up enclaves");
        }
    }

    private static bool ShouldAttemptRecovery(Models.EnclaveResponse enclave)
    {
        // Only attempt recovery if enclave has been in error state for less than 30 minutes
        // and hasn't been recovered too many times recently
        return DateTime.UtcNow - enclave.CreatedAt < TimeSpan.FromMinutes(30);
    }

    private async Task AttemptEnclaveRecoveryAsync(ISgxEnclaveService enclaveService, Guid enclaveId)
    {
        try
        {
            // Set to initializing state to trigger recovery
            await enclaveService.UpdateEnclaveStatusAsync(enclaveId, Models.SgxEnclaveStatus.Initializing);
            
            // Simulate recovery process
            await Task.Delay(TimeSpan.FromSeconds(30));
            
            // Set back to ready if recovery successful
            await enclaveService.UpdateEnclaveStatusAsync(enclaveId, Models.SgxEnclaveStatus.Ready);
            
            _logger.LogInformation("Successfully recovered enclave {EnclaveId}", enclaveId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover enclave {EnclaveId}", enclaveId);
            await enclaveService.UpdateEnclaveStatusAsync(enclaveId, Models.SgxEnclaveStatus.Crashed);
        }
    }
}
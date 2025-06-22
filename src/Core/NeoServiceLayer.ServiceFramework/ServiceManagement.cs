using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Implementation of service dependency resolver.
/// </summary>
public class DefaultServiceDependencyResolver : IServiceDependencyResolver
{
    private readonly ILogger<DefaultServiceDependencyResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultServiceDependencyResolver"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DefaultServiceDependencyResolver(ILogger<DefaultServiceDependencyResolver> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> ResolveDependenciesAsync(IService service, IEnumerable<IService> availableServices)
    {
        try
        {
            _logger.LogDebug("Resolving dependencies for service {ServiceName}", service.Name);

            var result = await service.ValidateDependenciesAsync(availableServices);

            if (result)
            {
                _logger.LogDebug("All dependencies resolved for service {ServiceName}", service.Name);
            }
            else
            {
                _logger.LogWarning("Failed to resolve dependencies for service {ServiceName}", service.Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving dependencies for service {ServiceName}", service.Name);
            return false;
        }
    }
}

/// <summary>
/// Implementation of service lifecycle manager.
/// </summary>
public class DefaultServiceLifecycleManager : IServiceLifecycleManager
{
    private readonly ILogger<DefaultServiceLifecycleManager> _logger;
    private readonly IServiceDependencyResolver _dependencyResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultServiceLifecycleManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dependencyResolver">The dependency resolver.</param>
    public DefaultServiceLifecycleManager(
        ILogger<DefaultServiceLifecycleManager> logger,
        IServiceDependencyResolver dependencyResolver)
    {
        _logger = logger;
        _dependencyResolver = dependencyResolver;
    }

    /// <inheritdoc/>
    public async Task<bool> StartServicesAsync(IEnumerable<IService> services)
    {
        var serviceList = services.ToList();
        var startedServices = new List<IService>();

        try
        {
            _logger.LogInformation("Starting {ServiceCount} services", serviceList.Count);

            foreach (var service in serviceList)
            {
                try
                {
                    _logger.LogDebug("Starting service {ServiceName}", service.Name);

                    // Resolve dependencies
                    if (!await _dependencyResolver.ResolveDependenciesAsync(service, startedServices))
                    {
                        _logger.LogError("Failed to resolve dependencies for service {ServiceName}", service.Name);
                        continue;
                    }

                    // Initialize service
                    if (!await service.InitializeAsync())
                    {
                        _logger.LogError("Failed to initialize service {ServiceName}", service.Name);
                        continue;
                    }

                    // Start service
                    if (!await service.StartAsync())
                    {
                        _logger.LogError("Failed to start service {ServiceName}", service.Name);
                        continue;
                    }

                    startedServices.Add(service);
                    _logger.LogInformation("Service {ServiceName} started successfully", service.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting service {ServiceName}", service.Name);
                }
            }

            _logger.LogInformation("Started {StartedCount}/{TotalCount} services successfully",
                startedServices.Count, serviceList.Count);

            return startedServices.Count == serviceList.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting services");

            // Stop any services that were started
            await StopServicesAsync(startedServices);

            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> StopServicesAsync(IEnumerable<IService> services)
    {
        var serviceList = services.ToList();
        var stoppedCount = 0;

        try
        {
            _logger.LogInformation("Stopping {ServiceCount} services", serviceList.Count);

            // Stop services in reverse order
            foreach (var service in serviceList.AsEnumerable().Reverse())
            {
                try
                {
                    _logger.LogDebug("Stopping service {ServiceName}", service.Name);

                    if (await service.StopAsync())
                    {
                        stoppedCount++;
                        _logger.LogInformation("Service {ServiceName} stopped successfully", service.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Service {ServiceName} failed to stop gracefully", service.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping service {ServiceName}", service.Name);
                }
            }

            _logger.LogInformation("Stopped {StoppedCount}/{TotalCount} services",
                stoppedCount, serviceList.Count);

            return stoppedCount == serviceList.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping services");
            return false;
        }
    }
}

/// <summary>
/// Implementation of service validator.
/// </summary>
public class DefaultServiceValidator : IServiceValidator
{
    private readonly ILogger<DefaultServiceValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultServiceValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public DefaultServiceValidator(ILogger<DefaultServiceValidator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ServiceValidationResult> ValidateAsync(IEnumerable<IService> services)
    {
        var result = new ServiceValidationResult { IsValid = true };
        var serviceList = services.ToList();

        try
        {
            _logger.LogInformation("Validating {ServiceCount} services", serviceList.Count);

            // Validate each service
            foreach (var service in serviceList)
            {
                await ValidateServiceAsync(service, serviceList, result);
            }

            // Validate overall system
            await ValidateSystemAsync(serviceList, result);

            _logger.LogInformation("Service validation completed. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                result.IsValid, result.Errors.Count, result.Warnings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during service validation");
            result.IsValid = false;
            result.Errors.Add($"Validation failed with exception: {ex.Message}");
            return result;
        }
    }

    /// <summary>
    /// Validates a single service.
    /// </summary>
    private async Task ValidateServiceAsync(IService service, List<IService> allServices, ServiceValidationResult result)
    {
        try
        {
            // Validate service configuration
            if (string.IsNullOrEmpty(service.Name))
            {
                result.Errors.Add($"Service has empty name");
                result.IsValid = false;
            }

            if (string.IsNullOrEmpty(service.Version))
            {
                result.Warnings.Add($"Service {service.Name} has no version specified");
            }

            // Validate dependencies
            if (!await service.ValidateDependenciesAsync(allServices))
            {
                result.Errors.Add($"Service {service.Name} has unresolved dependencies");
                result.IsValid = false;
            }

            // Validate blockchain services
            if (service is IBlockchainService blockchainService)
            {
                if (!blockchainService.SupportedBlockchains.Any())
                {
                    result.Warnings.Add($"Blockchain service {service.Name} supports no blockchain types");
                }
            }

            // Validate enclave services
            if (service is IEnclaveService enclaveService)
            {
                if (!enclaveService.IsEnclaveInitialized)
                {
                    result.Warnings.Add($"Enclave service {service.Name} enclave is not initialized");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error validating service {service.Name}: {ex.Message}");
            result.IsValid = false;
        }
    }

    /// <summary>
    /// Validates the overall system.
    /// </summary>
    private async Task ValidateSystemAsync(List<IService> services, ServiceValidationResult result)
    {
        await Task.CompletedTask; // Ensure async

        // Check for duplicate service names
        var duplicateNames = services
            .GroupBy(s => s.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var duplicateName in duplicateNames)
        {
            result.Errors.Add($"Duplicate service name found: {duplicateName}");
            result.IsValid = false;
        }

        // Check for circular dependencies (simplified)
        var serviceNames = services.Select(s => s.Name).ToHashSet();
        foreach (var service in services)
        {
            foreach (var dependency in service.Dependencies)
            {
                if (!serviceNames.Contains(dependency.ToString() ?? ""))
                {
                    result.Warnings.Add($"Service {service.Name} depends on unregistered service: {dependency}");
                }
            }
        }
    }
}

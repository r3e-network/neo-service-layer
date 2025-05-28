using System.Text.RegularExpressions;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Interface for the service registry.
/// </summary>
public interface IServiceRegistry
{
    /// <summary>
    /// Registers a service with the registry.
    /// </summary>
    /// <param name="service">The service to register.</param>
    void RegisterService(IService service);

    /// <summary>
    /// Registers a service with the registry asynchronously.
    /// </summary>
    /// <param name="service">The service to register.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RegisterServiceAsync(IService service);

    /// <summary>
    /// Unregisters a service from the registry.
    /// </summary>
    /// <param name="serviceName">The name of the service to unregister.</param>
    /// <returns>True if the service was unregistered, false if it was not found.</returns>
    bool UnregisterService(string serviceName);

    /// <summary>
    /// Gets a service by name.
    /// </summary>
    /// <param name="serviceName">The name of the service to get.</param>
    /// <returns>The service, or null if it was not found.</returns>
    IService? GetService(string serviceName);

    /// <summary>
    /// Gets a service by name and type.
    /// </summary>
    /// <typeparam name="T">The type of service to get.</typeparam>
    /// <param name="serviceName">The name of the service to get.</param>
    /// <returns>The service, or null if it was not found or is not of the specified type.</returns>
    T? GetService<T>(string serviceName) where T : class, IService;

    /// <summary>
    /// Gets all registered services.
    /// </summary>
    /// <returns>All registered services.</returns>
    IEnumerable<IService> GetAllServices();

    /// <summary>
    /// Gets all registered services asynchronously.
    /// </summary>
    /// <returns>All registered services.</returns>
    Task<IEnumerable<IService>> GetAllServicesAsync();

    /// <summary>
    /// Gets all registered services of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of services to get.</typeparam>
    /// <returns>All registered services of the specified type.</returns>
    IEnumerable<T> GetAllServices<T>() where T : class, IService;

    /// <summary>
    /// Finds services by name pattern.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to match service names.</param>
    /// <returns>All services with names matching the pattern.</returns>
    IEnumerable<IService> FindServicesByNamePattern(string pattern);

    /// <summary>
    /// Finds services by capability.
    /// </summary>
    /// <typeparam name="T">The capability interface type.</typeparam>
    /// <returns>All services implementing the specified capability interface.</returns>
    IEnumerable<T> FindServicesByCapability<T>() where T : class;

    /// <summary>
    /// Finds services by blockchain type.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>All services supporting the specified blockchain type.</returns>
    IEnumerable<IBlockchainService> FindServicesByBlockchainType(BlockchainType blockchainType);

    /// <summary>
    /// Checks if a service exists.
    /// </summary>
    /// <param name="serviceName">The name of the service to check.</param>
    /// <returns>True if the service exists, false otherwise.</returns>
    bool ServiceExists(string serviceName);

    /// <summary>
    /// Gets the number of registered services.
    /// </summary>
    /// <returns>The number of registered services.</returns>
    int GetServiceCount();

    /// <summary>
    /// Initializes all registered services.
    /// </summary>
    /// <returns>True if all services were initialized successfully, false otherwise.</returns>
    Task<bool> InitializeAllServicesAsync();

    /// <summary>
    /// Initializes services matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to match service names.</param>
    /// <returns>True if all matching services were initialized successfully, false otherwise.</returns>
    Task<bool> InitializeServicesByPatternAsync(string pattern);

    /// <summary>
    /// Starts all registered services.
    /// </summary>
    /// <returns>True if all services were started successfully, false otherwise.</returns>
    Task<bool> StartAllServicesAsync();

    /// <summary>
    /// Starts services matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to match service names.</param>
    /// <returns>True if all matching services were started successfully, false otherwise.</returns>
    Task<bool> StartServicesByPatternAsync(string pattern);

    /// <summary>
    /// Stops all registered services.
    /// </summary>
    /// <returns>True if all services were stopped successfully, false otherwise.</returns>
    Task<bool> StopAllServicesAsync();

    /// <summary>
    /// Stops services matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to match service names.</param>
    /// <returns>True if all matching services were stopped successfully, false otherwise.</returns>
    Task<bool> StopServicesByPatternAsync(string pattern);

    /// <summary>
    /// Gets the health status of all registered services.
    /// </summary>
    /// <returns>A dictionary mapping service names to their health status.</returns>
    Task<IDictionary<string, ServiceHealth>> GetAllServicesHealthAsync();

    /// <summary>
    /// Gets the health status of services matching the specified pattern.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to match service names.</param>
    /// <returns>A dictionary mapping service names to their health status.</returns>
    Task<IDictionary<string, ServiceHealth>> GetServicesHealthByPatternAsync(string pattern);

    /// <summary>
    /// Event raised when a service is registered.
    /// </summary>
    event EventHandler<ServiceEventArgs>? ServiceRegistered;

    /// <summary>
    /// Event raised when a service is unregistered.
    /// </summary>
    event EventHandler<ServiceEventArgs>? ServiceUnregistered;

    /// <summary>
    /// Event raised when a service's health status changes.
    /// </summary>
    event EventHandler<ServiceHealthChangedEventArgs>? ServiceHealthChanged;
}

/// <summary>
/// Event arguments for service events.
/// </summary>
public class ServiceEventArgs : EventArgs
{
    /// <summary>
    /// Gets the service.
    /// </summary>
    public IService Service { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceEventArgs"/> class.
    /// </summary>
    /// <param name="service">The service.</param>
    public ServiceEventArgs(IService service)
    {
        Service = service;
    }
}

/// <summary>
/// Event arguments for service health changed events.
/// </summary>
public class ServiceHealthChangedEventArgs : ServiceEventArgs
{
    /// <summary>
    /// Gets the previous health status.
    /// </summary>
    public ServiceHealth PreviousHealth { get; }

    /// <summary>
    /// Gets the current health status.
    /// </summary>
    public ServiceHealth CurrentHealth { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceHealthChangedEventArgs"/> class.
    /// </summary>
    /// <param name="service">The service.</param>
    /// <param name="previousHealth">The previous health status.</param>
    /// <param name="currentHealth">The current health status.</param>
    public ServiceHealthChangedEventArgs(IService service, ServiceHealth previousHealth, ServiceHealth currentHealth)
        : base(service)
    {
        PreviousHealth = previousHealth;
        CurrentHealth = currentHealth;
    }
}

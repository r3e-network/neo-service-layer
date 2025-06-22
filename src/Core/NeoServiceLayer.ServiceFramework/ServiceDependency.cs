using NeoServiceLayer.Core;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Represents a service dependency.
/// </summary>
public class ServiceDependency
{
    /// <summary>
    /// Gets the name of the required service.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// Gets a value indicating whether the dependency is required.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Gets the minimum version of the required service.
    /// </summary>
    public string? MinimumVersion { get; }

    /// <summary>
    /// Gets the maximum version of the required service.
    /// </summary>
    public string? MaximumVersion { get; }

    /// <summary>
    /// Gets the type of the required service.
    /// </summary>
    public Type? ServiceType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceDependency"/> class.
    /// </summary>
    /// <param name="serviceName">The name of the required service.</param>
    /// <param name="isRequired">A value indicating whether the dependency is required.</param>
    /// <param name="minimumVersion">The minimum version of the required service.</param>
    /// <param name="maximumVersion">The maximum version of the required service.</param>
    /// <param name="serviceType">The type of the required service.</param>
    public ServiceDependency(string serviceName, bool isRequired = true, string? minimumVersion = null, string? maximumVersion = null, Type? serviceType = null)
    {
        ServiceName = serviceName;
        IsRequired = isRequired;
        MinimumVersion = minimumVersion;
        MaximumVersion = maximumVersion;
        ServiceType = serviceType;
    }

    /// <summary>
    /// Creates a required service dependency.
    /// </summary>
    /// <param name="serviceName">The name of the required service.</param>
    /// <param name="minimumVersion">The minimum version of the required service.</param>
    /// <param name="maximumVersion">The maximum version of the required service.</param>
    /// <returns>A new service dependency.</returns>
    public static ServiceDependency Required(string serviceName, string? minimumVersion = null, string? maximumVersion = null)
    {
        return new ServiceDependency(serviceName, true, minimumVersion, maximumVersion);
    }

    /// <summary>
    /// Creates a required service dependency with a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the required service.</typeparam>
    /// <param name="serviceName">The name of the required service.</param>
    /// <param name="minimumVersion">The minimum version of the required service.</param>
    /// <param name="maximumVersion">The maximum version of the required service.</param>
    /// <returns>A new service dependency.</returns>
    public static ServiceDependency Required<T>(string serviceName, string? minimumVersion = null, string? maximumVersion = null) where T : IService
    {
        return new ServiceDependency(serviceName, true, minimumVersion, maximumVersion, typeof(T));
    }

    /// <summary>
    /// Creates an optional service dependency.
    /// </summary>
    /// <param name="serviceName">The name of the required service.</param>
    /// <param name="minimumVersion">The minimum version of the required service.</param>
    /// <param name="maximumVersion">The maximum version of the required service.</param>
    /// <returns>A new service dependency.</returns>
    public static ServiceDependency Optional(string serviceName, string? minimumVersion = null, string? maximumVersion = null)
    {
        return new ServiceDependency(serviceName, false, minimumVersion, maximumVersion);
    }

    /// <summary>
    /// Creates an optional service dependency with a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the required service.</typeparam>
    /// <param name="serviceName">The name of the required service.</param>
    /// <param name="minimumVersion">The minimum version of the required service.</param>
    /// <param name="maximumVersion">The maximum version of the required service.</param>
    /// <returns>A new service dependency.</returns>
    public static ServiceDependency Optional<T>(string serviceName, string? minimumVersion = null, string? maximumVersion = null) where T : IService
    {
        return new ServiceDependency(serviceName, false, minimumVersion, maximumVersion, typeof(T));
    }

    /// <summary>
    /// Validates the dependency against a service.
    /// </summary>
    /// <param name="service">The service to validate.</param>
    /// <returns>True if the service satisfies the dependency, false otherwise.</returns>
    public bool Validate(IService service)
    {
        if (service.Name != ServiceName)
        {
            return false;
        }

        if (ServiceType != null && !ServiceType.IsInstanceOfType(service))
        {
            return false;
        }

        if (MinimumVersion != null && !IsVersionGreaterOrEqual(service.Version, MinimumVersion))
        {
            return false;
        }

        if (MaximumVersion != null && !IsVersionLessOrEqual(service.Version, MaximumVersion))
        {
            return false;
        }

        return true;
    }

    private static bool IsVersionGreaterOrEqual(string version1, string version2)
    {
        if (!Version.TryParse(version1, out var v1) || !Version.TryParse(version2, out var v2))
        {
            return string.Compare(version1, version2, StringComparison.Ordinal) >= 0;
        }

        return v1 >= v2;
    }

    private static bool IsVersionLessOrEqual(string version1, string version2)
    {
        if (!Version.TryParse(version1, out var v1) || !Version.TryParse(version2, out var v2))
        {
            return string.Compare(version1, version2, StringComparison.Ordinal) <= 0;
        }

        return v1 <= v2;
    }
}

using System;

namespace NeoServiceLayer.ServiceFramework.Permissions;

/// <summary>
/// Attribute to specify permission requirements for service methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : Attribute
{
    /// <summary>
    /// Gets the required resource pattern.
    /// </summary>
    public string Resource { get; }

    /// <summary>
    /// Gets the required action.
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Gets or sets the permission scope.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Gets or sets whether to allow service-to-service access.
    /// </summary>
    public bool AllowServiceAccess { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to allow admin override.
    /// </summary>
    public bool AllowAdminOverride { get; set; } = true;

    /// <summary>
    /// Gets or sets custom denial message.
    /// </summary>
    public string? DenialMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
    /// </summary>
    /// <param name="resource">The required resource pattern.</param>
    /// <param name="action">The required action.</param>
    public RequirePermissionAttribute(string resource, string action)
    {
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }
}

/// <summary>
/// Attribute to specify that a service method allows anonymous access.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AllowAnonymousAccessAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the reason for allowing anonymous access.
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Attribute to specify service-level permission requirements.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ServicePermissionsAttribute : Attribute
{
    /// <summary>
    /// Gets the service resource prefix.
    /// </summary>
    public string ResourcePrefix { get; }

    /// <summary>
    /// Gets or sets the default required role.
    /// </summary>
    public string? DefaultRole { get; set; }

    /// <summary>
    /// Gets or sets whether to auto-register service permissions.
    /// </summary>
    public bool AutoRegister { get; set; } = true;

    /// <summary>
    /// Gets or sets the service description for permission registration.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServicePermissionsAttribute"/> class.
    /// </summary>
    /// <param name="resourcePrefix">The service resource prefix (e.g., "voting", "storage").</param>
    public ServicePermissionsAttribute(string resourcePrefix)
    {
        ResourcePrefix = resourcePrefix ?? throw new ArgumentNullException(nameof(resourcePrefix));
    }
}
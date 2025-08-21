using System;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Migration class to help transition from old ServiceBase to UnifiedServiceBase.
/// This class will be deprecated once all services are migrated.
/// </summary>
[Obsolete("Use UnifiedServiceBase instead. This class is provided for backward compatibility only.")]
public abstract class ServiceBase : UnifiedServiceBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBase"/> class.
    /// </summary>
    protected ServiceBase(string name, string description, string version, ILogger logger)
        : base(name, description, version, logger)
    {
        // This constructor is provided for backward compatibility
        // All functionality is now in UnifiedServiceBase
    }
}

/// <summary>
/// Extension methods to help with service migration.
/// </summary>
public static class ServiceBaseMigrationExtensions
{
    /// <summary>
    /// Checks if a service is using the legacy ServiceBase implementation.
    /// </summary>
    public static bool IsLegacyService(this IService service)
    {
        var type = service.GetType();
        
        // Check if it inherits from the old ServiceBase in Core namespace
        if (type.BaseType?.FullName == "NeoServiceLayer.Core.ServiceBase")
            return true;
            
        // Check if it inherits from the deprecated ServiceBase
        if (type.BaseType == typeof(ServiceBase))
            return true;
            
        return false;
    }
    
    /// <summary>
    /// Gets migration recommendations for a service.
    /// </summary>
    public static string GetMigrationRecommendations(this IService service)
    {
        if (!service.IsLegacyService())
            return "Service is already using the unified implementation.";
            
        return @"To migrate this service to UnifiedServiceBase:
1. Change the base class from ServiceBase to UnifiedServiceBase
2. Update the namespace import to use NeoServiceLayer.ServiceFramework
3. Review any custom Dispose implementations (UnifiedServiceBase handles async disposal)
4. Test the service thoroughly after migration
5. Remove any duplicate capability or metadata management code";
    }
}
using System;
using Microsoft.Extensions.DependencyInjection;

namespace NeoServiceLayer.Core.Attributes
{
    /// <summary>
    /// Specifies the service lifetime for dependency injection registration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ServiceLifetimeAttribute : Attribute
    {
        /// <summary>
        /// Gets the service lifetime
        /// </summary>
        public ServiceLifetime Lifetime { get; }

        /// <summary>
        /// Initializes a new instance of the ServiceLifetimeAttribute class
        /// </summary>
        /// <param name="lifetime">The service lifetime</param>
        public ServiceLifetimeAttribute(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
        }
    }

    /// <summary>
    /// Marks a service as having Scoped lifetime
    /// </summary>
    public class ScopedServiceAttribute : ServiceLifetimeAttribute
    {
        public ScopedServiceAttribute() : base(ServiceLifetime.Scoped) { }
    }

    /// <summary>
    /// Marks a service as having Transient lifetime
    /// </summary>
    public class TransientServiceAttribute : ServiceLifetimeAttribute
    {
        public TransientServiceAttribute() : base(ServiceLifetime.Transient) { }
    }

    /// <summary>
    /// Marks a service as having Singleton lifetime
    /// </summary>
    public class SingletonServiceAttribute : ServiceLifetimeAttribute
    {
        public SingletonServiceAttribute() : base(ServiceLifetime.Singleton) { }
    }
}
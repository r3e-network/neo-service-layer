using System;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Tee.Shared.Events;

namespace NeoServiceLayer.Tee.Host.Events
{
    /// <summary>
    /// Extension methods for registering event services.
    /// </summary>
    public static class EventServiceExtensions
    {
        /// <summary>
        /// Adds event services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddEventServices(this IServiceCollection services)
        {
            // Register event trigger manager
            services.AddSingleton<IEventTriggerManager, EventTriggerManager>();

            // Register event system
            services.AddSingleton<IEventSystem, EventSystem>();

            // Register background services
            services.AddHostedService<ScheduledEventProcessor>();
            services.AddHostedService<PendingEventProcessor>();

            return services;
        }
    }
}

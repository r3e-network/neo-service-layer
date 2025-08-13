using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NeoServiceLayer.Core.Events;

namespace NeoServiceLayer.Infrastructure.EventSourcing
{
    /// <summary>
    /// Service collection extensions for event sourcing infrastructure
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds event sourcing infrastructure to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEventSourcing(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure options
            services.Configure&lt;EventStoreConfiguration&gt;(
                configuration.GetSection(EventStoreConfiguration.SectionName));
            
            services.Configure&lt;EventBusConfiguration&gt;(
                configuration.GetSection(EventBusConfiguration.SectionName));
            
            services.Configure&lt;EventProcessingConfiguration&gt;(
                configuration.GetSection(EventProcessingConfiguration.SectionName));

            // Validate configuration
            services.AddSingleton&lt;IValidateOptions&lt;EventStoreConfiguration&gt;, EventStoreConfigurationValidator&gt;();
            services.AddSingleton&lt;IValidateOptions&lt;EventBusConfiguration&gt;, EventBusConfigurationValidator&gt;();
            services.AddSingleton&lt;IValidateOptions&lt;EventProcessingConfiguration&gt;, EventProcessingConfigurationValidator&gt;();

            // Register core services
            services.AddSingleton&lt;IEventStore, PostgreSqlEventStore&gt;();
            services.AddSingleton&lt;IEventBus, RabbitMqEventBus&gt;();
            services.AddHostedService&lt;EventProcessingEngine&gt;();

            // Register event sourcing initializer
            services.AddHostedService&lt;EventSourcingInitializer&gt;();

            return services;
        }

        /// <summary>
        /// Adds an event handler to the service collection
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <typeparam name="THandler">Handler type</typeparam>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEventHandler&lt;TEvent, THandler&gt;(this IServiceCollection services)
            where TEvent : class, IDomainEvent
            where THandler : class, IEventHandler&lt;TEvent&gt;
        {
            services.AddTransient&lt;IEventHandler&lt;TEvent&gt;, THandler&gt;();
            services.AddTransient&lt;THandler&gt;();

            return services;
        }

        /// <summary>
        /// Adds multiple event handlers from an assembly
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="assembly">Assembly to scan for event handlers</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEventHandlersFromAssembly(
            this IServiceCollection services,
            System.Reflection.Assembly assembly)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t =&gt; t.IsClass && !t.IsAbstract)
                .Where(t =&gt; t.GetInterfaces()
                    .Any(i =&gt; i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler&lt;&gt;)))
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                var interfaceTypes = handlerType.GetInterfaces()
                    .Where(i =&gt; i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler&lt;&gt;));

                foreach (var interfaceType in interfaceTypes)
                {
                    services.AddTransient(interfaceType, handlerType);
                    services.AddTransient(handlerType);
                }
            }

            return services;
        }
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NeoServiceLayer.Core.Events;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


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
            services.Configure<EventStoreConfiguration>(
                configuration.GetSection(EventStoreConfiguration.SectionName));

            services.Configure<EventBusConfiguration>(
                configuration.GetSection(EventBusConfiguration.SectionName));

            services.Configure<EventProcessingConfiguration>(
                configuration.GetSection(EventProcessingConfiguration.SectionName));

            // Validate configuration
            services.AddSingleton<IValidateOptions<EventStoreConfiguration>, EventStoreConfigurationValidator>();
            services.AddSingleton<IValidateOptions<EventBusConfiguration>, EventBusConfigurationValidator>();
            services.AddSingleton<IValidateOptions<EventProcessingConfiguration>, EventProcessingConfigurationValidator>();

            // Register core services
            services.AddSingleton<IEventStore, PostgreSqlEventStore>();
            services.AddSingleton<IEventBus, RabbitMqEventBus>();
            services.AddHostedService<EventProcessingEngine>();

            // Register event sourcing initializer
            services.AddHostedService<EventSourcingInitializer>();

            return services;
        }

        /// <summary>
        /// Adds an event handler to the service collection
        /// </summary>
        /// <typeparam name="TEvent">Event type</typeparam>
        /// <typeparam name="THandler">Handler type</typeparam>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEventHandler<TEvent, THandler>(this IServiceCollection services)
            where TEvent : class, IDomainEvent
            where THandler : class, IEventHandler<TEvent>
        {
            services.AddTransient<IEventHandler<TEvent>, THandler>();
            services.AddTransient<THandler>();

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
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>)))
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                var interfaceTypes = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

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
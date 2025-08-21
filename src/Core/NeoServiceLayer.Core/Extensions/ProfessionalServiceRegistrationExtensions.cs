using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Core.Attributes;

namespace NeoServiceLayer.Core.Extensions
{
    /// <summary>
    /// Extension methods for professional service registration with lifetime validation
    /// </summary>
    public static class ProfessionalServiceRegistrationExtensions
    {
        /// <summary>
        /// Adds services with automatic lifetime detection based on attributes
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="assemblies">Assemblies to scan for services</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddServicesWithLifetimeValidation(
            this IServiceCollection services, 
            params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var serviceTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract)
                    .Where(t => t.GetCustomAttribute<ServiceLifetimeAttribute>() != null)
                    .ToList();

                foreach (var serviceType in serviceTypes)
                {
                    var lifetimeAttribute = serviceType.GetCustomAttribute<ServiceLifetimeAttribute>();
                    var interfaces = serviceType.GetInterfaces();

                    if (interfaces.Any())
                    {
                        foreach (var interfaceType in interfaces)
                        {
                            services.Add(new ServiceDescriptor(
                                interfaceType,
                                serviceType,
                                lifetimeAttribute!.Lifetime));
                        }
                    }
                    else
                    {
                        services.Add(new ServiceDescriptor(
                            serviceType,
                            serviceType,
                            lifetimeAttribute!.Lifetime));
                    }
                }
            }

            return services;
        }

        /// <summary>
        /// Validates that all services are registered with correct lifetimes
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        /// <exception cref="InvalidOperationException">Thrown when lifetime validation fails</exception>
        public static IServiceCollection ValidateServiceLifetimes(this IServiceCollection services)
        {
            var errors = new List<string>();

            foreach (var service in services)
            {
                if (service.ImplementationType?.GetCustomAttribute<ServiceLifetimeAttribute>() is { } attribute)
                {
                    if (service.Lifetime != attribute.Lifetime)
                    {
                        errors.Add(
                            $"Service {service.ImplementationType.Name} is registered with " +
                            $"{service.Lifetime} but marked with {attribute.Lifetime}");
                    }
                }
            }

            if (errors.Any())
            {
                throw new InvalidOperationException(
                    "Service lifetime validation failed:\n" + string.Join("\n", errors));
            }

            return services;
        }

        /// <summary>
        /// Adds domain services with proper lifetime management
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            // Domain services should be scoped to maintain consistency within a request
            services.AddScoped<IPasswordPolicy, EnterprisePasswordPolicy>();
            
            return services;
        }

        /// <summary>
        /// Adds CQRS infrastructure with proper lifetime management
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddCQRS(this IServiceCollection services)
        {
            // Command and query dispatchers should be scoped
            services.AddScoped<ICommandDispatcher, CommandDispatcher>();
            services.AddScoped<IQueryDispatcher, QueryDispatcher>();
            
            return services;
        }

        /// <summary>
        /// Adds event publishing infrastructure
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddDomainEvents(this IServiceCollection services)
        {
            // Event publisher should be scoped to ensure proper event handling within requests
            services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
            
            // Register domain event handlers
            services.AddScoped<IDomainEventHandler<UserCreatedEvent>, UserCreatedEventHandler>();
            services.AddScoped<IDomainEventHandler<AuthenticationSucceededEvent>, AuthenticationSucceededEventHandler>();
            services.AddScoped<IDomainEventHandler<AuthenticationFailedEvent>, AuthenticationFailedEventHandler>();
            services.AddScoped<IDomainEventHandler<AccountLockedEvent>, AccountLockedEventHandler>();
            services.AddScoped<IDomainEventHandler<PasswordChangedEvent>, PasswordChangedEventHandler>();
            
            return services;
        }

        /// <summary>
        /// Registers repositories with proper lifetime management
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // Repositories should be scoped to maintain consistency within a request
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, EntityFrameworkUnitOfWork>();
            services.AddScoped<IUnitOfWorkWithEvents, EntityFrameworkUnitOfWorkWithEvents>();
            
            return services;
        }

        /// <summary>
        /// Fixes common service lifetime issues in existing registrations
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        public static IServiceCollection FixServiceLifetimeIssues(this IServiceCollection services)
        {
            // Find and fix common lifetime issues
            var problematicServices = services
                .Where(s => s.Lifetime == ServiceLifetime.Singleton && 
                           s.ServiceType.Name.EndsWith("Service") &&
                           !IsStatelessService(s.ServiceType))
                .ToList();

            foreach (var service in problematicServices)
            {
                // Remove the problematic registration
                services.Remove(service);
                
                // Re-register with correct lifetime
                if (service.ImplementationType != null)
                {
                    services.AddScoped(service.ServiceType, service.ImplementationType);
                }
                else if (service.ImplementationFactory != null)
                {
                    services.AddScoped(service.ServiceType, service.ImplementationFactory);
                }
            }

            return services;
        }

        /// <summary>
        /// Determines if a service is stateless and can safely be a singleton
        /// </summary>
        /// <param name="serviceType">The service type</param>
        /// <returns>True if the service is stateless</returns>
        private static bool IsStatelessService(Type serviceType)
        {
            // Services that are safe as singletons
            var statelessServiceNames = new[]
            {
                "IPasswordPolicy",
                "IEmailValidator",
                "ILogger",
                "IConfiguration",
                "IMetricsCollector"
            };

            return statelessServiceNames.Any(name => 
                serviceType.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Validates service dependency graphs for circular dependencies
    /// </summary>
    public static class ServiceDependencyValidator
    {
        /// <summary>
        /// Validates the service dependency graph for circular dependencies
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection</returns>
        /// <exception cref="InvalidOperationException">Thrown when circular dependencies are detected</exception>
        public static IServiceCollection ValidateDependencyGraph(this IServiceCollection services)
        {
            var dependencyGraph = BuildDependencyGraph(services);
            var circularDependencies = DetectCircularDependencies(dependencyGraph);

            if (circularDependencies.Any())
            {
                var errorMessage = "Circular dependencies detected:\n" + 
                                 string.Join("\n", circularDependencies.Select(cycle => 
                                     string.Join(" -> ", cycle)));
                throw new InvalidOperationException(errorMessage);
            }

            return services;
        }

        private static Dictionary<Type, List<Type>> BuildDependencyGraph(IServiceCollection services)
        {
            var graph = new Dictionary<Type, List<Type>>();

            foreach (var service in services)
            {
                if (service.ImplementationType == null) continue;

                var dependencies = GetDependencies(service.ImplementationType);
                graph[service.ServiceType] = dependencies.ToList();
            }

            return graph;
        }

        private static IEnumerable<Type> GetDependencies(Type implementationType)
        {
            var constructors = implementationType.GetConstructors();
            var primaryConstructor = constructors
                .OrderByDescending(c => c.GetParameters().Length)
                .First();

            return primaryConstructor.GetParameters()
                .Select(p => p.ParameterType)
                .Where(t => !t.IsPrimitive && t != typeof(string));
        }

        private static List<List<Type>> DetectCircularDependencies(Dictionary<Type, List<Type>> graph)
        {
            var circularDependencies = new List<List<Type>>();
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();

            foreach (var node in graph.Keys)
            {
                if (!visited.Contains(node))
                {
                    var path = new List<Type>();
                    DetectCycle(node, graph, visited, recursionStack, path, circularDependencies);
                }
            }

            return circularDependencies;
        }

        private static bool DetectCycle(
            Type node,
            Dictionary<Type, List<Type>> graph,
            HashSet<Type> visited,
            HashSet<Type> recursionStack,
            List<Type> path,
            List<List<Type>> circularDependencies)
        {
            visited.Add(node);
            recursionStack.Add(node);
            path.Add(node);

            if (graph.TryGetValue(node, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (!visited.Contains(dependency))
                    {
                        if (DetectCycle(dependency, graph, visited, recursionStack, path, circularDependencies))
                            return true;
                    }
                    else if (recursionStack.Contains(dependency))
                    {
                        // Found a cycle
                        var cycleStartIndex = path.IndexOf(dependency);
                        var cycle = path.Skip(cycleStartIndex).Concat(new[] { dependency }).ToList();
                        circularDependencies.Add(cycle);
                        return true;
                    }
                }
            }

            recursionStack.Remove(node);
            path.RemoveAt(path.Count - 1);
            return false;
        }
    }
}
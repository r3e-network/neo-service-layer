using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Core.Domain;
using NeoServiceLayer.Core.Attributes;

namespace NeoServiceLayer.Core.Architecture
{
    /// <summary>
    /// Architectural fitness functions for validating design principles
    /// </summary>
    public static class ArchitecturalTests
    {
        /// <summary>
        /// Validates that all aggregate roots properly inherit from AggregateRoot
        /// </summary>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Validation result</returns>
        public static ArchitecturalValidationResult ValidateAggregateRoots(params Assembly[] assemblies)
        {
            var errors = new List<string>();
            
            foreach (var assembly in assemblies)
            {
                var aggregateTypes = assembly.GetTypes()
                    .Where(t => t.Name.EndsWith("Aggregate") || 
                               (t.IsClass && HasDomainEvents(t)))
                    .Where(t => !IsAggregateRoot(t))
                    .ToList();

                errors.AddRange(aggregateTypes.Select(type => 
                    $"Type {type.FullName} appears to be an aggregate but doesn't inherit from AggregateRoot"));
            }

            return new ArchitecturalValidationResult(errors.Count == 0, errors);
        }

        /// <summary>
        /// Validates that domain events are immutable and properly structured
        /// </summary>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Validation result</returns>
        public static ArchitecturalValidationResult ValidateDomainEvents(params Assembly[] assemblies)
        {
            var errors = new List<string>();

            foreach (var assembly in assemblies)
            {
                var eventTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && typeof(IDomainEvent).IsAssignableFrom(t))
                    .ToList();

                foreach (var eventType in eventTypes)
                {
                    // Check immutability - properties should only have getters
                    var mutableProperties = eventType.GetProperties()
                        .Where(p => p.CanWrite && p.SetMethod?.IsPublic == true)
                        .ToList();

                    if (mutableProperties.Any())
                    {
                        errors.Add($"Domain event {eventType.FullName} has mutable properties: {string.Join(", ", mutableProperties.Select(p => p.Name))}");
                    }

                    // Check for proper event naming
                    if (!eventType.Name.EndsWith("Event"))
                    {
                        errors.Add($"Domain event {eventType.FullName} should end with 'Event'");
                    }
                }
            }

            return new ArchitecturalValidationResult(errors.Count == 0, errors);
        }

        /// <summary>
        /// Validates that value objects are properly immutable
        /// </summary>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Validation result</returns>
        public static ArchitecturalValidationResult ValidateValueObjects(params Assembly[] assemblies)
        {
            var errors = new List<string>();

            foreach (var assembly in assemblies)
            {
                var valueObjectTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && typeof(ValueObject).IsAssignableFrom(t))
                    .ToList();

                foreach (var valueObjectType in valueObjectTypes)
                {
                    // Check immutability
                    var mutableProperties = valueObjectType.GetProperties()
                        .Where(p => p.CanWrite && p.SetMethod?.IsPublic == true)
                        .Where(p => p.DeclaringType == valueObjectType) // Only check properties declared in this type
                        .ToList();

                    if (mutableProperties.Any())
                    {
                        errors.Add($"Value object {valueObjectType.FullName} has mutable properties: {string.Join(", ", mutableProperties.Select(p => p.Name))}");
                    }

                    // Check for public constructor
                    var publicConstructors = valueObjectType.GetConstructors()
                        .Where(c => c.IsPublic)
                        .ToList();

                    if (publicConstructors.Any())
                    {
                        errors.Add($"Value object {valueObjectType.FullName} has public constructors. Use factory methods instead.");
                    }
                }
            }

            return new ArchitecturalValidationResult(errors.Count == 0, errors);
        }

        /// <summary>
        /// Validates service lifetime attributes are correctly applied
        /// </summary>
        /// <param name="serviceCollection">The service collection to validate</param>
        /// <returns>Validation result</returns>
        public static ArchitecturalValidationResult ValidateServiceLifetimes(IServiceCollection serviceCollection)
        {
            var errors = new List<string>();

            foreach (var service in serviceCollection)
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

                // Check for common lifetime issues
                if (service.Lifetime == ServiceLifetime.Singleton && 
                    service.ServiceType.Name.EndsWith("Repository"))
                {
                    errors.Add($"Repository {service.ServiceType.Name} should not be registered as Singleton");
                }

                if (service.Lifetime == ServiceLifetime.Transient && 
                    service.ServiceType.Name.EndsWith("Context"))
                {
                    errors.Add($"DbContext {service.ServiceType.Name} should not be registered as Transient");
                }
            }

            return new ArchitecturalValidationResult(errors.Count == 0, errors);
        }

        /// <summary>
        /// Validates dependency directions follow clean architecture principles
        /// </summary>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Validation result</returns>
        public static ArchitecturalValidationResult ValidateDependencyDirection(params Assembly[] assemblies)
        {
            var errors = new List<string>();
            var coreAssemblies = assemblies.Where(a => a.FullName?.Contains("Core") == true).ToList();
            var infrastructureAssemblies = assemblies.Where(a => a.FullName?.Contains("Infrastructure") == true).ToList();

            // Core should not depend on Infrastructure
            foreach (var coreAssembly in coreAssemblies)
            {
                var referencedAssemblies = coreAssembly.GetReferencedAssemblies();
                var infrastructureDependencies = referencedAssemblies
                    .Where(a => a.Name?.Contains("Infrastructure") == true)
                    .ToList();

                if (infrastructureDependencies.Any())
                {
                    errors.Add($"Core assembly {coreAssembly.GetName().Name} depends on infrastructure assemblies: {string.Join(", ", infrastructureDependencies.Select(a => a.Name))}");
                }
            }

            return new ArchitecturalValidationResult(errors.Count == 0, errors);
        }

        /// <summary>
        /// Validates that entities follow domain-driven design principles
        /// </summary>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Validation result</returns>
        public static ArchitecturalValidationResult ValidateEntityDesign(params Assembly[] assemblies)
        {
            var errors = new List<string>();

            foreach (var assembly in assemblies)
            {
                var entityTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && typeof(Entity<>).IsAssignableFrom(t.BaseType))
                    .ToList();

                foreach (var entityType in entityTypes)
                {
                    // Check for anemic domain model (entities should have behavior, not just properties)
                    var methods = entityType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .Where(m => !m.IsSpecialName) // Exclude property getters/setters
                        .Where(m => m.DeclaringType == entityType)
                        .ToList();

                    if (!methods.Any())
                    {
                        errors.Add($"Entity {entityType.FullName} appears to be anemic (no business methods). Consider adding business behavior.");
                    }

                    // Check for public setters on important properties
                    var publicSetters = entityType.GetProperties()
                        .Where(p => p.SetMethod?.IsPublic == true)
                        .Where(p => p.Name != "Id") // ID can have public setter for ORM
                        .ToList();

                    if (publicSetters.Count > 3) // Allow some flexibility
                    {
                        errors.Add($"Entity {entityType.FullName} has many public setters. Consider encapsulating state changes in methods.");
                    }
                }
            }

            return new ArchitecturalValidationResult(errors.Count == 0, errors);
        }

        /// <summary>
        /// Runs all architectural validation tests
        /// </summary>
        /// <param name="serviceCollection">The service collection</param>
        /// <param name="assemblies">Assemblies to scan</param>
        /// <returns>Combined validation result</returns>
        public static ArchitecturalValidationResult ValidateAll(IServiceCollection serviceCollection, params Assembly[] assemblies)
        {
            var allErrors = new List<string>();

            var tests = new[]
            {
                ValidateAggregateRoots(assemblies),
                ValidateDomainEvents(assemblies),
                ValidateValueObjects(assemblies),
                ValidateServiceLifetimes(serviceCollection),
                ValidateDependencyDirection(assemblies),
                ValidateEntityDesign(assemblies)
            };

            foreach (var test in tests)
            {
                allErrors.AddRange(test.Errors);
            }

            return new ArchitecturalValidationResult(allErrors.Count == 0, allErrors);
        }

        private static bool IsAggregateRoot(Type type)
        {
            var current = type.BaseType;
            while (current != null)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
                {
                    return true;
                }
                current = current.BaseType;
            }
            return false;
        }

        private static bool HasDomainEvents(Type type)
        {
            return type.GetProperties()
                .Any(p => p.PropertyType == typeof(IReadOnlyList<IDomainEvent>) ||
                         p.PropertyType == typeof(List<IDomainEvent>));
        }
    }

    /// <summary>
    /// Result of architectural validation
    /// </summary>
    public class ArchitecturalValidationResult
    {
        /// <summary>
        /// Gets whether the validation passed
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the validation errors
        /// </summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Initializes a new instance of ArchitecturalValidationResult
        /// </summary>
        /// <param name="isValid">Whether validation passed</param>
        /// <param name="errors">Validation errors</param>
        public ArchitecturalValidationResult(bool isValid, IEnumerable<string> errors)
        {
            IsValid = isValid;
            Errors = errors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
        }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        /// <returns>Successful result</returns>
        public static ArchitecturalValidationResult Success() => 
            new ArchitecturalValidationResult(true, new List<string>());

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        /// <param name="errors">Validation errors</param>
        /// <returns>Failed result</returns>
        public static ArchitecturalValidationResult Failure(params string[] errors) => 
            new ArchitecturalValidationResult(false, errors);
    }
}
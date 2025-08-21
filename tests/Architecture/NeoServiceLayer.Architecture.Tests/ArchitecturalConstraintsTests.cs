using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Core.Architecture;
using NeoServiceLayer.Core.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Architecture.Tests
{
    /// <summary>
    /// Tests that validate architectural constraints and fitness functions
    /// </summary>
    public class ArchitecturalConstraintsTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Assembly[] _assemblies;

        public ArchitecturalConstraintsTests(ITestOutputHelper output)
        {
            _output = output;
            _assemblies = new[]
            {
                Assembly.Load("NeoServiceLayer.Core"),
                Assembly.Load("NeoServiceLayer.Infrastructure"),
                Assembly.Load("NeoServiceLayer.Api")
            };
        }

        [Fact]
        public void AggregateRoots_ShouldInheritFromAggregateRootBaseClass()
        {
            // Arrange & Act
            var result = ArchitecturalTests.ValidateAggregateRoots(_assemblies);

            // Assert
            if (!result.IsValid)
            {
                _output.WriteLine("Aggregate root validation errors:");
                foreach (var error in result.Errors)
                {
                    _output.WriteLine($"- {error}");
                }
            }

            Assert.True(result.IsValid, $"Aggregate root validation failed: {string.Join(", ", result.Errors)}");
        }

        [Fact]
        public void DomainEvents_ShouldBeImmutable()
        {
            // Arrange & Act
            var result = ArchitecturalTests.ValidateDomainEvents(_assemblies);

            // Assert
            if (!result.IsValid)
            {
                _output.WriteLine("Domain event validation errors:");
                foreach (var error in result.Errors)
                {
                    _output.WriteLine($"- {error}");
                }
            }

            Assert.True(result.IsValid, $"Domain event validation failed: {string.Join(", ", result.Errors)}");
        }

        [Fact]
        public void ValueObjects_ShouldBeImmutable()
        {
            // Arrange & Act
            var result = ArchitecturalTests.ValidateValueObjects(_assemblies);

            // Assert
            if (!result.IsValid)
            {
                _output.WriteLine("Value object validation errors:");
                foreach (var error in result.Errors)
                {
                    _output.WriteLine($"- {error}");
                }
            }

            Assert.True(result.IsValid, $"Value object validation failed: {string.Join(", ", result.Errors)}");
        }

        [Fact]
        public void ServiceLifetimes_ShouldBeCorrectlyConfigured()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddServicesWithLifetimeValidation(_assemblies);

            // Act
            var result = ArchitecturalTests.ValidateServiceLifetimes(services);

            // Assert
            if (!result.IsValid)
            {
                _output.WriteLine("Service lifetime validation errors:");
                foreach (var error in result.Errors)
                {
                    _output.WriteLine($"- {error}");
                }
            }

            Assert.True(result.IsValid, $"Service lifetime validation failed: {string.Join(", ", result.Errors)}");
        }

        [Fact]
        public void DependencyDirection_ShouldFollowCleanArchitecture()
        {
            // Arrange & Act
            var result = ArchitecturalTests.ValidateDependencyDirection(_assemblies);

            // Assert
            if (!result.IsValid)
            {
                _output.WriteLine("Dependency direction validation errors:");
                foreach (var error in result.Errors)
                {
                    _output.WriteLine($"- {error}");
                }
            }

            Assert.True(result.IsValid, $"Dependency direction validation failed: {string.Join(", ", result.Errors)}");
        }

        [Fact]
        public void EntityDesign_ShouldFollowDomainDrivenDesignPrinciples()
        {
            // Arrange & Act
            var result = ArchitecturalTests.ValidateEntityDesign(_assemblies);

            // Assert
            if (!result.IsValid)
            {
                _output.WriteLine("Entity design validation errors:");
                foreach (var error in result.Errors)
                {
                    _output.WriteLine($"- {error}");
                }
            }

            // Note: This test allows warnings but fails on errors
            Assert.True(result.IsValid, $"Entity design validation failed: {string.Join(", ", result.Errors)}");
        }

        [Fact]
        public void OverallArchitecture_ShouldMeetAllConstraints()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddServicesWithLifetimeValidation(_assemblies);

            // Act
            var result = ArchitecturalTests.ValidateAll(services, _assemblies);

            // Assert
            if (!result.IsValid)
            {
                _output.WriteLine("Overall architectural validation errors:");
                foreach (var error in result.Errors)
                {
                    _output.WriteLine($"- {error}");
                }
            }

            Assert.True(result.IsValid, $"Overall architectural validation failed: {string.Join(", ", result.Errors)}");
        }
    }
}
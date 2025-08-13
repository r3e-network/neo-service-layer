using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Exporters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace NeoServiceLayer.Performance.Tests.Infrastructure
{
    /// <summary>
    /// Minimal base class for performance benchmarks with simplified configuration.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob]
    [MarkdownExporter]
    public abstract class MinimalPerformanceBenchmarkBase
    {
        protected IServiceProvider? ServiceProvider { get; private set; }
        protected IConfiguration? Configuration { get; private set; }

        /// <summary>
        /// Global setup for the benchmark.
        /// </summary>
        [GlobalSetup]
        public virtual void Setup()
        {
            // Build configuration
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.benchmark.json", optional: true);

            Configuration = configurationBuilder.Build();

            // Configure services
            var services = new ServiceCollection();
            
            // Add logging with console output
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Global cleanup for the benchmark.
        /// </summary>
        [GlobalCleanup]
        public virtual void Cleanup()
        {
            if (ServiceProvider is IDisposable disposable)
                disposable.Dispose();
        }

        /// <summary>
        /// Configure services for the benchmark.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        protected abstract void ConfigureServices(IServiceCollection services);

        /// <summary>
        /// Creates a custom benchmark configuration.
        /// </summary>
        /// <returns>The benchmark configuration.</returns>
        protected virtual IConfig CreateConfig()
        {
            return DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddLogger(ConsoleLogger.Default)
                .AddExporter(MarkdownExporter.Default);
        }
    }
}
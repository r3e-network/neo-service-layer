using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NeoServiceLayer.Tee.Enclave
{
    /// <summary>
    /// Extension methods for setting up Neo Service Layer Occlum services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Neo Service Layer Occlum services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configuration">The configuration instance.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddNeoServiceLayerOcclum(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Add options
            services.Configure<OcclumOptions>(configuration.GetSection("Occlum"));
            services.Configure<GasAccountingOptions>(configuration.GetSection("GasAccounting"));
            services.Configure<PersistentStorageOptions>(configuration.GetSection("PersistentStorage"));

            // Register the services
            services.AddSingleton<IPersistentStorageService, OcclumFileStorageProvider>();
            services.AddSingleton<IOcclumInterface, OcclumInterface>();
            services.AddSingleton<UserSecretManager>();
            services.AddSingleton<GasAccountingManager>();
            services.AddTransient<JavaScriptEngine>();
            services.AddTransient<JavaScriptExecutionService>();

            return services;
        }

        /// <summary>
        /// Adds the Neo Service Layer Occlum services to the specified <see cref="IServiceCollection"/> with custom options.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureOcclum">The action to configure the Occlum options.</param>
        /// <param name="configureGasAccounting">The action to configure the gas accounting options.</param>
        /// <param name="configurePersistentStorage">The action to configure the persistent storage options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddNeoServiceLayerOcclum(
            this IServiceCollection services,
            Action<OcclumOptions> configureOcclum = null,
            Action<GasAccountingOptions> configureGasAccounting = null,
            Action<PersistentStorageOptions> configurePersistentStorage = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Add options
            if (configureOcclum != null)
            {
                services.Configure(configureOcclum);
            }
            else
            {
                services.Configure<OcclumOptions>(options =>
                {
                    options.InstanceDir = "/occlum_instance";
                    options.LogLevel = "info";
                    options.NodeJsPath = "/bin/node";
                    options.TempDir = "/tmp";
                    options.EnableDebugMode = false;
                    options.MaxMemoryMB = 512;
                    options.MaxThreads = 32;
                    options.MaxProcesses = 16;
                    options.MaxExecutionTimeSeconds = 60;
                });
            }

            if (configureGasAccounting != null)
            {
                services.Configure(configureGasAccounting);
            }
            else
            {
                services.Configure<GasAccountingOptions>(options =>
                {
                    options.MaxGasLimit = 1_000_000;
                    options.EnableTimeBasedGas = true;
                    options.GasPerMillisecond = 1.0;
                    options.BasicOperationGas = 1;
                    options.MemoryOperationGasPerByte = 0.1;
                    options.StorageOperationGasPerByte = 1.0;
                    options.CryptographicOperationGas = 50;
                });
            }

            if (configurePersistentStorage != null)
            {
                services.Configure(configurePersistentStorage);
            }
            else
            {
                services.Configure<PersistentStorageOptions>(options =>
                {
                    options.StoragePath = "/data/storage";
                    options.EnableEncryption = true;
                    options.EnableCompression = true;
                    options.CompressionLevel = 6;
                    options.CreateIfNotExists = true;
                    options.EnableCaching = true;
                    options.CacheSizeBytes = 50 * 1024 * 1024; // 50MB
                    options.EnableAutoFlush = true;
                    options.AutoFlushIntervalMs = 5000; // 5 seconds
                });
            }

            // Register services
            services.AddSingleton<IPersistentStorageService, OcclumFileStorageProvider>();
            services.AddSingleton<IOcclumInterface>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OcclumInterface>>();
                var options = sp.GetRequiredService<IOptions<OcclumOptions>>();
                return new OcclumInterface(
                    logger,
                    options,
                    () => sp.GetRequiredService<IPersistentStorageService>());
            });
            services.AddSingleton<UserSecretManager>();
            services.AddSingleton<GasAccountingManager>();
            services.AddTransient<JavaScriptEngine>();
            services.AddTransient<JavaScriptExecutionService>();

            return services;
        }

        /// <summary>
        /// Initializes the Neo Service Layer Occlum services.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async System.Threading.Tasks.Task InitializeNeoServiceLayerOcclumAsync(this IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OcclumInterface>>();
            logger.LogInformation("Initializing Neo Service Layer Occlum services...");

            // Initialize Occlum interface
            var occlumInterface = serviceProvider.GetRequiredService<IOcclumInterface>();
            bool occlumInitialized = await occlumInterface.InitializeAsync();
            
            if (!occlumInitialized)
            {
                throw new InvalidOperationException("Failed to initialize Occlum interface");
            }

            // Initialize persistent storage
            var storageService = serviceProvider.GetRequiredService<IPersistentStorageService>();
            var storageOptions = serviceProvider.GetRequiredService<IOptions<PersistentStorageOptions>>().Value;
            await storageService.InitializeAsync(storageOptions);

            logger.LogInformation("Neo Service Layer Occlum services initialized successfully");
        }
    }
} 
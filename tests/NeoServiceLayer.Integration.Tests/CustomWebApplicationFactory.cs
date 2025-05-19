using System;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Infrastructure.Data;
using NeoServiceLayer.Infrastructure.Services;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Integration.Tests.Mocks;

namespace NeoServiceLayer.Integration.Tests
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        private bool _databaseInitialized = false;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove all DbContext registrations to avoid conflicts
                var descriptors = services.Where(
                    d => d.ServiceType == typeof(DbContextOptions<NeoServiceLayerDbContext>) ||
                         d.ServiceType == typeof(DbContextOptions) ||
                         d.ServiceType == typeof(NeoServiceLayerDbContext) ||
                         (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Set environment variable to use in-memory database
                Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");

                // Create a new service provider for the in-memory database
                var serviceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                // Add in-memory database with a unique name for each test run
                services.AddDbContext<NeoServiceLayerDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"InMemoryDbForTesting-{Guid.NewGuid()}");
                    options.UseInternalServiceProvider(serviceProvider);
                });

                // Configure to use the real SGX enclave bridge in simulation mode
                var teeHostServiceDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ITeeHostService));

                if (teeHostServiceDescriptor != null)
                {
                    services.Remove(teeHostServiceDescriptor);
                }

                // Set environment variables for SGX simulation mode
                Environment.SetEnvironmentVariable("SGX_MODE", "SIM");
                Environment.SetEnvironmentVariable("SGX_SIMULATION", "1");

                // Register the real SGX enclave bridge in simulation mode
                services.AddSingleton<ISgxEnclaveBridge>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<SgxEnclaveBridge>>();
                    var enclaveBridge = new SgxEnclaveBridge(logger);

                    try
                    {
                        // Set the path to the enclave library for testing
                        var enclavePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "neoservicelayer_enclave.so");
                        if (System.IO.File.Exists(enclavePath))
                        {
                            logger.LogInformation("Found enclave library at {Path}", enclavePath);
                        }
                        else
                        {
                            logger.LogWarning("Enclave library not found at {Path}", enclavePath);
                            // Try to find the enclave library in other locations
                            var possiblePaths = new[]
                            {
                                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib", "neoservicelayer_enclave.so"),
                                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "lib", "neoservicelayer_enclave.so"),
                                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "lib", "neoservicelayer_enclave.so"),
                                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "lib", "neoservicelayer_enclave.so")
                            };

                            foreach (var path in possiblePaths)
                            {
                                if (System.IO.File.Exists(path))
                                {
                                    logger.LogInformation("Found enclave library at {Path}", path);
                                    enclavePath = path;
                                    break;
                                }
                            }
                        }

                        // Initialize in simulation mode
                        var result = enclaveBridge.Initialize(true);
                        if (!result)
                        {
                            logger.LogWarning("Failed to initialize SGX enclave bridge in simulation mode.");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to initialize SGX enclave bridge in simulation mode.");
                    }

                    return enclaveBridge;
                });

                // Register the TeeHostService with the SGX enclave bridge
                services.AddSingleton<ITeeHostService>(provider =>
                {
                    var configuration = provider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                    var logger = provider.GetRequiredService<ILogger<TeeHostService>>();
                    var enclaveBridge = provider.GetRequiredService<ISgxEnclaveBridge>();

                    // Configure TeeHostService to use simulation mode
                    configuration["Tee:SimulationMode"] = "true";

                    // Set the enclave library path if it's not already set
                    if (string.IsNullOrEmpty(configuration["Tee:EnclaveLibraryPath"]))
                    {
                        var enclavePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "neoservicelayer_enclave.so");
                        if (System.IO.File.Exists(enclavePath))
                        {
                            configuration["Tee:EnclaveLibraryPath"] = enclavePath;
                            logger.LogInformation("Setting enclave library path to {Path}", enclavePath);
                        }
                    }

                    var teeHostService = new TeeHostService(configuration, logger, enclaveBridge);

                    // Log the status of the TEE host service
                    try
                    {
                        var status = teeHostService.GetStatus();
                        logger.LogInformation("TEE host service status: {Status}, EnclaveId: {EnclaveId}",
                            status.Status, status.EnclaveId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to get TEE host service status");
                    }

                    return teeHostService;
                });

                // Set environment variables for testing
                Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Test");
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
                Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");

                // Use real implementations where possible, with mocks only for external dependencies

                // Register attestation service with real implementation using SGX simulation
                services.AddScoped<IAttestationService, Infrastructure.Services.AttestationService>();

                // Register task service with real implementation
                services.AddScoped<ITaskService, Infrastructure.Services.TaskService>();

                // Register key service with mock implementation
                services.AddScoped<IKeyService, Mocks.MockKeyService>();

                // Register key management service with mock implementation
                services.AddScoped<IKeyManagementServiceExtended, Mocks.MockKeyManagementService>();
                services.AddScoped<IKeyManagementService>(provider =>
                    provider.GetRequiredService<IKeyManagementServiceExtended>());

                // Register randomness service with mock implementation
                services.AddScoped<IRandomnessService, Mocks.MockRandomnessService>();

                // Register compliance service with mock implementation
                services.AddScoped<IComplianceService, Mocks.MockComplianceService>();

                // Register event service with mock implementation
                services.AddSingleton<IEventService, Mocks.MockEventService>();

                // Register Neo N3 blockchain service mock (external dependency)
                services.AddSingleton<Mocks.MockNeoN3BlockchainService>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<Mocks.MockNeoN3BlockchainService>>();
                    return new Mocks.MockNeoN3BlockchainService(logger);
                });
                services.AddSingleton<INeoN3BlockchainService>(provider => provider.GetRequiredService<Mocks.MockNeoN3BlockchainService>());

                // We're already using the MockNeoN3BlockchainService, no need to register the real one

                // Register Neo N3 event listener service mock
                services.AddSingleton<NeoServiceLayer.Infrastructure.Services.NeoN3EventListenerService, Mocks.MockNeoN3EventListenerService>();

                // Add health checks service
                services.AddHealthChecks()
                    .AddCheck<Mocks.MockHealthCheck>("mock_health_check");

                // Add controllers with mock data
                services.AddControllers()
                    .AddApplicationPart(typeof(NeoServiceLayer.Api.Controllers.AttestationController).Assembly);

                // We'll seed the database in a separate method that will be called after the application is built
            });
        }

        protected override void ConfigureClient(HttpClient client)
        {
            base.ConfigureClient(client);

            // Initialize the database if it hasn't been initialized yet
            if (!_databaseInitialized)
            {
                try
                {
                    using (var scope = Services.CreateScope())
                    {
                        var scopedServices = scope.ServiceProvider;
                        var db = scopedServices.GetRequiredService<NeoServiceLayerDbContext>();
                        var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory<TStartup>>>();

                        try
                        {
                            // Ensure the database is created
                            db.Database.EnsureCreated();

                            // Seed the database with test data
                            SeedDatabase(db);

                            _databaseInitialized = true;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "An error occurred initializing the database. Error: {Message}", ex.Message);
                            // Don't set _databaseInitialized to true if there was an error
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to initialize database: {ex.Message}");
                    // Continue without failing the test setup
                    _databaseInitialized = true; // Prevent further initialization attempts
                }
            }
        }

        private void SeedDatabase(NeoServiceLayerDbContext context)
        {
            try
            {
                // Add verification results
                if (!context.VerificationResults.Any(v => v.Id == "test-verification-id"))
                {
                    context.VerificationResults.Add(new Infrastructure.Data.Entities.VerificationResultEntity
                    {
                        Id = "test-verification-id",
                        Status = "completed",
                        VerificationType = "KYC",
                        IdentityData = "{\"name\":\"John Doe\"}",
                        Verified = true,
                        Score = 0.95,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                        ProcessedAt = DateTime.UtcNow.AddMinutes(-5),
                        MetadataJson = "{\"source\":\"test\"}",
                        Reason = "Test verification"
                    });
                    context.SaveChanges();
                }

                // Add tasks
                if (!context.Tasks.Any(t => t.Id == "test-task-id"))
                {
                    context.Tasks.Add(new Infrastructure.Data.Entities.TaskEntity
                    {
                        Id = "test-task-id",
                        UserId = "user123",
                        Type = NeoServiceLayer.Core.Models.TaskType.SmartContractExecution,
                        Status = NeoServiceLayer.Core.Models.TaskStatus.Completed,
                        DataJson = "{\"contract\":\"0x1234567890abcdef\",\"method\":\"transfer\",\"params\":[\"address1\",\"address2\",\"100\"]}",
                        ResultJson = "{\"result\":\"success\",\"output\":\"contract_executed\",\"gas_used\":1000}",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                        StartedAt = DateTime.UtcNow.AddMinutes(-9),
                        CompletedAt = DateTime.UtcNow.AddMinutes(-8)
                    });
                    context.SaveChanges();
                }

                // Add accounts
                if (!context.TeeAccounts.Any(a => a.Id == "test-account-id"))
                {
                    context.TeeAccounts.Add(new Infrastructure.Data.Entities.TeeAccountEntity
                    {
                        Id = "test-account-id",
                        UserId = "user123",
                        Type = NeoServiceLayer.Core.Models.AccountType.Wallet,
                        Name = "Test Neo Account",
                        PublicKey = "03b209fd4f53a7170ea4444e0cb0a6bb6a53c2bd016926989cf85f9b0fba17a70c",
                        Address = "NZHf1NJvz1tvELGLWZjhpb3NqZJFFUYpxT",
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        IsExportable = true,
                        MetadataJson = "{\"purpose\":\"testing\"}",
                        AttestationProof = "test-attestation-proof"
                    });
                    context.SaveChanges();
                }

                // Note: Keys are managed by the KeyManagementService, not directly in the database

                // Add randomness requests
                if (context.GetType().GetProperty("RandomnessRequests") != null)
                {
                    var randomnessRequestsProperty = context.GetType().GetProperty("RandomnessRequests");
                    if (randomnessRequestsProperty != null)
                    {
                        var randomnessRequests = randomnessRequestsProperty.GetValue(context);
                        var anyMethod = randomnessRequests.GetType().GetMethod("Any");

                        if (anyMethod != null)
                        {
                            var hasTestRequest = (bool)anyMethod.Invoke(randomnessRequests, new object[] { });

                            if (!hasTestRequest)
                            {
                                // Add randomness request using reflection to avoid compilation errors if the table doesn't exist
                                var addMethod = randomnessRequests.GetType().GetMethod("Add");
                                if (addMethod != null)
                                {
                                    var randomnessRequestType = Type.GetType("NeoServiceLayer.Infrastructure.Data.Entities.RandomnessRequestEntity, NeoServiceLayer.Infrastructure");
                                    if (randomnessRequestType != null)
                                    {
                                        var randomnessRequest = Activator.CreateInstance(randomnessRequestType);

                                        // Set properties using reflection
                                        randomnessRequestType.GetProperty("Id").SetValue(randomnessRequest, "test-randomness-id");
                                        randomnessRequestType.GetProperty("UserId").SetValue(randomnessRequest, "user123");
                                        randomnessRequestType.GetProperty("Status").SetValue(randomnessRequest, "Completed");
                                        randomnessRequestType.GetProperty("RandomnessHex").SetValue(randomnessRequest, "0123456789abcdef");
                                        randomnessRequestType.GetProperty("Proof").SetValue(randomnessRequest, "test-proof");
                                        randomnessRequestType.GetProperty("CreatedAt").SetValue(randomnessRequest, DateTime.UtcNow.AddMinutes(-10));
                                        randomnessRequestType.GetProperty("CompletedAt").SetValue(randomnessRequest, DateTime.UtcNow.AddMinutes(-9));

                                        addMethod.Invoke(randomnessRequests, new[] { randomnessRequest });
                                        context.SaveChanges();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding database: {ex.Message}");
                // Continue without failing the test setup
            }
        }
    }
}

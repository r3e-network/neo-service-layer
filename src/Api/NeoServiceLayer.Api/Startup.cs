using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using MongoDB.Driver;
using NeoServiceLayer.Api.HealthChecks;
using NeoServiceLayer.Api.Middleware;
using NeoServiceLayer.Api.Extensions;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Events;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Infrastructure.EventBus;
using NeoServiceLayer.Infrastructure.CQRS;
using NeoServiceLayer.Infrastructure.Caching;
using NeoServiceLayer.Services.Authentication.Implementation;
using NeoServiceLayer.Services.Compute.Implementation;
using NeoServiceLayer.Services.Storage.Implementation;
using NeoServiceLayer.Services.Oracle.Implementation;
using NeoServiceLayer.Services.UserManagement;
using NeoServiceLayer.Api.GraphQL;
using Prometheus;
using Serilog;

namespace NeoServiceLayer.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Configure enhanced HTTPS and security
            services.AddHttpsSecurity(_configuration);

            // Add API versioning
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });

            // Add controllers with JSON options
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.WriteIndented = _environment.IsDevelopment();
                });

            // Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins", builder =>
                {
                    builder.WithOrigins(_configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" })
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            // Configure JWT Authentication
            ConfigureAuthentication(services);

            // Configure databases
            ConfigureDatabases(services);

            // Configure caching
            ConfigureCaching(services);

            // Configure messaging
            ConfigureMessaging(services);

            // Register core services
            RegisterCoreServices(services);

            // Register business services
            RegisterBusinessServices(services);

            // Configure health checks
            ConfigureHealthChecks(services);

            // Configure Swagger
            ConfigureSwagger(services);

            // Add rate limiting
            services.AddSingleton<IRateLimitCounterStore, RedisRateLimitCounterStore>();

            // Add HTTP clients
            services.AddHttpClient();

            // Add metrics
            services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();

            // Configure GraphQL
            services.AddGraphQLServices();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Service Layer API V1");
                    c.RoutePrefix = "swagger";
                });
                
                // GraphQL IDE for development
                app.UseGraphQLVoyager("/graphql-voyager", "/graphql");
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseHsts();
            }

            // Use Serilog request logging
            app.UseSerilogRequestLogging();

            // Use enhanced security headers middleware
            app.UseEnhancedSecurity(env);

            // Use HTTPS redirection
            app.UseHttpsRedirection();

            // Use CORS
            app.UseCors("AllowSpecificOrigins");

            // Custom middleware
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<PerformanceMonitoringMiddleware>();
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseMiddleware<RateLimitingMiddleware>();

            // Use routing
            app.UseRouting();

            // Use authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Prometheus metrics
            app.UseHttpMetrics();

            // Map endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                
                // GraphQL endpoints
                endpoints.MapGraphQL("/graphql");
                
                // Health check endpoints
                endpoints.MapHealthChecks("/health");
                endpoints.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready")
                });
                endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("live")
                });
                
                endpoints.MapMetrics(); // Prometheus metrics endpoint
            });

            // Initialize services
            InitializeServices(app);
        }

        private void ConfigureAuthentication(IServiceCollection services)
        {
            var jwtSecret = _configuration["Jwt:Secret"];
            var key = Encoding.ASCII.GetBytes(jwtSecret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !_environment.IsDevelopment();
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
                options.AddPolicy("RequireUser", policy => policy.RequireRole("User", "Admin"));
                options.AddPolicy("RequireEnclave", policy => policy.RequireClaim("enclave_access", "true"));
            });
        }

        private void ConfigureDatabases(IServiceCollection services)
        {
            // PostgreSQL
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? 
                                 _configuration.GetConnectionString("PostgreSQL");
            
            services.AddDbContext<NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.NeoServiceLayerDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Add PostgreSQL repositories
            services.AddScoped<NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories.IOracleDataFeedRepository, 
                              NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories.OracleDataFeedRepository>();
            services.AddScoped<NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories.IVotingRepository, 
                              NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories.VotingRepository>();
            services.AddScoped<NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories.ICrossChainRepository, 
                              NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories.CrossChainRepository>();
            services.AddScoped<NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories.ISealedDataRepository, 
                              NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories.SealedDataRepository>();
            services.AddScoped<NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories.IUserRepository, 
                              NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories.UserRepository>();

            // Redis
            var redisConnection = _configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnection))
            {
                services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));
            }

            // MongoDB (optional)
            var mongoConnection = _configuration.GetConnectionString("MongoDB");
            if (!string.IsNullOrEmpty(mongoConnection))
            {
                services.AddSingleton<IMongoClient>(new MongoClient(mongoConnection));
                services.AddScoped(provider =>
                {
                    var client = provider.GetService<IMongoClient>();
                    return client?.GetDatabase("neo_service_layer");
                });
            }

            // Event Store
            services.AddSingleton<IEventStore, PostgreSqlEventStore>();
        }

        private void ConfigureCaching(IServiceCollection services)
        {
            // Distributed cache (Redis)
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = _configuration.GetConnectionString("Redis");
                options.InstanceName = "NeoServiceLayer";
            });

            // Memory cache
            services.AddMemoryCache();

            // Cache service abstraction
            services.AddSingleton<ICacheService, DistributedCacheService>();
        }

        private void ConfigureMessaging(IServiceCollection services)
        {
            // Event bus
            services.AddSingleton<IEventHandlerRegistry, EventHandlerRegistry>();
            services.AddSingleton<IEventBus, RabbitMqEventBus>();

            // Command bus
            services.AddScoped<ICommandBus, CommandBus>();
            
            // Query bus
            services.AddScoped<IQueryBus, QueryBus>();
        }

        private void RegisterCoreServices(IServiceCollection services)
        {
            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IJobRepository, JobRepository>();
            services.AddScoped<IFileRepository, FileRepository>();
            services.AddScoped<IOracleDataRepository, OracleDataRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();

            // Core services
            services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
            services.AddSingleton<ITokenBlacklistService, RedisTokenBlacklistService>();
            services.AddSingleton<IHttpClientFactory, HttpClientFactory>();
            
            // Blockchain
            services.AddScoped<IBlockchainClient, NeoBlockchainClient>();
            services.AddScoped<IBlockchainClientFactory, BlockchainClientFactory>();

            // Enclave
            services.AddSingleton<IEnclaveManager, SgxEnclaveManager>();
            
            // Metrics
            services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();
        }

        private void RegisterBusinessServices(IServiceCollection services)
        {
            // Authentication
            services.AddScoped<IAuthenticationService, JwtAuthenticationService>();
            
            // User Management
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IPermissionService, PermissionService>();
            
            // Compute
            services.AddSingleton<IComputeService, SecureComputeService>();
            
            // Storage
            services.AddScoped<IStorageService, EncryptedStorageService>();
            services.AddScoped<IEncryptionService, AesEncryptionService>();
            services.AddScoped<IObjectStorageProvider, S3StorageProvider>();
            
            // Oracle
            services.AddSingleton<IOracleService, BlockchainOracleService>();
            
            // Command Handlers
            services.AddScoped<ICommandHandler<CreateUserCommand>, CreateUserCommandHandler>();
            services.AddScoped<ICommandHandler<UpdateUserCommand>, UpdateUserCommandHandler>();
            services.AddScoped<ICommandHandler<ChangePasswordCommand>, ChangePasswordCommandHandler>();
            services.AddScoped<ICommandHandler<AssignRoleCommand>, AssignRoleCommandHandler>();
            services.AddScoped<ICommandHandler<LockUserCommand>, LockUserCommandHandler>();
            
            // Query Handlers
            services.AddScoped<IQueryHandler<GetUserByIdQuery, UserReadModel>, GetUserByIdQueryHandler>();
            services.AddScoped<IQueryHandler<GetUsersByTenantQuery, PagedResult<UserReadModel>>, GetUsersByTenantQueryHandler>();
            
            // Event Handlers
            services.AddScoped<IEventHandler<UserCreatedEvent>, UserCreatedEventHandler>();
            services.AddScoped<IEventHandler<UserUpdatedEvent>, UserUpdatedEventHandler>();
        }

        private void ConfigureHealthChecks(IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<PostgreSqlHealthCheck>("postgresql", tags: new[] { "db", "ready" })
                .AddCheck<RedisHealthCheck>("redis", tags: new[] { "cache", "ready" })
                .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: new[] { "messaging", "ready" })
                .AddCheck<MongoDbHealthCheck>("mongodb", tags: new[] { "db", "ready" })
                .AddCheck<EnclaveHealthCheck>("enclave", tags: new[] { "security", "ready" })
                .AddCheck<BlockchainHealthCheck>("blockchain", tags: new[] { "blockchain", "ready" })
                .AddCheck<DiskSpaceHealthCheck>("disk", tags: new[] { "infrastructure", "live" })
                .AddCheck<MemoryHealthCheck>("memory", tags: new[] { "infrastructure", "live" })
                .AddCheck<SystemHealthCheck>("system", tags: new[] { "ready" });
        }

        private void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Neo Service Layer API",
                    Version = "v1",
                    Description = "Enterprise blockchain service platform with SGX enclave support",
                    Contact = new OpenApiContact
                    {
                        Name = "Neo Service Layer Team",
                        Email = "support@neoservicelayer.io"
                    }
                });

                // Add JWT authentication
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Include XML comments
                var xmlFiles = System.IO.Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
                foreach (var xmlFile in xmlFiles)
                {
                    c.IncludeXmlComments(xmlFile);
                }
            });
        }

        private void InitializeServices(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                // Run database migrations
                var dbContext = services.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();

                // Initialize enclave
                var enclaveManager = services.GetRequiredService<IEnclaveManager>();
                enclaveManager.InitializeAsync().GetAwaiter().GetResult();

                // Register event handlers
                var eventBus = services.GetRequiredService<IEventBus>();
                var registry = services.GetRequiredService<IEventHandlerRegistry>();
                
                // Register all event handlers
                registry.RegisterHandler<UserCreatedEvent, UserCreatedEventHandler>();
                registry.RegisterHandler<UserUpdatedEvent, UserUpdatedEventHandler>();
                registry.RegisterHandler<PasswordChangedEvent, PasswordChangedEventHandler>();
                registry.RegisterHandler<RoleAssignedEvent, RoleAssignedEventHandler>();
                registry.RegisterHandler<UserLockedEvent, UserLockedEventHandler>();

                Log.Information("All services initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An error occurred while initializing services");
                throw;
            }
        }
    }

    // Helper classes for dependency injection
    public class PrometheusMetricsCollector : IMetricsCollector
    {
        private readonly Dictionary<string, Counter> _counters = new();
        private readonly Dictionary<string, Gauge> _gauges = new();
        private readonly Dictionary<string, Histogram> _histograms = new();

        public void IncrementCounter(string name, (string, string)[] labels = null)
        {
            if (!_counters.ContainsKey(name))
            {
                _counters[name] = Metrics.CreateCounter(name, name);
            }
            _counters[name].Inc();
        }

        public void RecordValue(string name, double value, (string, string)[] labels = null)
        {
            if (!_gauges.ContainsKey(name))
            {
                _gauges[name] = Metrics.CreateGauge(name, name);
            }
            _gauges[name].Set(value);
        }

        public void RecordLatency(string name, double milliseconds, (string, string)[] labels = null)
        {
            if (!_histograms.ContainsKey(name))
            {
                _histograms[name] = Metrics.CreateHistogram(name, name);
            }
            _histograms[name].Observe(milliseconds);
        }
    }
}
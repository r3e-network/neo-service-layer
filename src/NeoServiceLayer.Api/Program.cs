using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NeoServiceLayer.Api.Middleware;
using NeoServiceLayer.Core.Interfaces;
using NeoServiceLayer.Core.Services;
using NeoServiceLayer.Core.Storage;
using NeoServiceLayer.Infrastructure.Data;
using NeoServiceLayer.Infrastructure.Data.Repositories;
using NeoServiceLayer.Infrastructure.Monitoring;
using NeoServiceLayer.Infrastructure.Services;
using NeoServiceLayer.Tee.Host;
using NeoServiceLayer.Tee.Host.Occlum;
using NeoServiceLayer.Tee.Host.Services;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NeoServiceLayer.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // Add API versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-API-Version"),
                new QueryStringApiVersionReader("api-version"));
        });

        // Add API version explorer
        builder.Services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            // Define a Swagger document for each API version
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Neo Service Layer API",
                Version = "v1",
                Description = "Neo Service Layer API for confidential computing services",
                Contact = new OpenApiContact
                {
                    Name = "Neo Service Layer Team",
                    Email = "support@neoservices.io",
                    Url = new Uri("https://neoservices.io/contact")
                },
                License = new OpenApiLicense
                {
                    Name = "Neo Service Layer License",
                    Url = new Uri("https://neoservices.io/license")
                }
            });

            c.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = "Neo Service Layer API",
                Version = "v2",
                Description = "Neo Service Layer API v2 with enhanced features",
                Contact = new OpenApiContact
                {
                    Name = "Neo Service Layer Team",
                    Email = "support@neoservices.io",
                    Url = new Uri("https://neoservices.io/contact")
                },
                License = new OpenApiLicense
                {
                    Name = "Neo Service Layer License",
                    Url = new Uri("https://neoservices.io/license")
                }
            });

            // Add JWT Authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
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

            // Use fully qualified type names to avoid conflicts
            c.CustomSchemaIds(type => type.FullName);

            // Include XML comments if available
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        // Add authentication
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
            };
        })
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", options =>
        {
            options.Realm = "Neo Service Layer API";
        });

        // Add rate limiting
        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        QueueLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });
        });

        // Add CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigins",
                corsBuilder =>
                {
                    corsBuilder.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "https://neoservices.io" })
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
        });

        // Add health checks
        builder.Services.AddHealthChecks()
            .AddCheck<NeoServiceLayerHealthCheck>("neo_service_layer_health_check");

        // Add metrics service
        builder.Services.AddSingleton<IMetricsService, MetricsService>();

        // Add telemetry
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.AddSource("NeoServiceLayer.Api");
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter("NeoServiceLayer.Api");
            });

        // Add database context
        var useInMemoryDatabase = Environment.GetEnvironmentVariable("UseInMemoryDatabase")?.ToLower() == "true";
        if (useInMemoryDatabase)
        {
            builder.Services.AddDbContext<NeoServiceLayerDbContext>(options =>
                options.UseInMemoryDatabase("InMemoryDb"));

            Console.WriteLine("Using in-memory database");
        }
        else
        {
            builder.Services.AddDbContext<NeoServiceLayerDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            Console.WriteLine($"Using SQL Server database: {builder.Configuration.GetConnectionString("DefaultConnection")}");
        }

        // Register repositories
        builder.Services.AddScoped<IAttestationProofRepository, AttestationProofRepository>();
        builder.Services.AddScoped<ITeeAccountRepository, TeeAccountRepository>();
        builder.Services.AddScoped<ITaskRepository, TaskRepository>();
        builder.Services.AddScoped<IVerificationResultRepository, VerificationResultRepository>();

        // Register services
        builder.Services.AddScoped<ITaskService, TaskService>();
        builder.Services.AddScoped<IAttestationService, AttestationService>();
        builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
        builder.Services.AddScoped<IRandomnessService, RandomnessService>();
        builder.Services.AddScoped<IComplianceService, ComplianceService>();
        builder.Services.AddScoped<IEventService, EventService>();
        builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

        // Register persistent storage services
        builder.Services.AddSingleton<IPersistentStorageProvider, OcclumFileStorageProvider>();
        builder.Services.AddSingleton<IPersistentStorageService, PersistentStorageService>();

        // Register Neo N3 blockchain service with real implementation
        builder.Services.AddSingleton<INeoN3BlockchainService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<NeoN3BlockchainService>>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            var rpcUrl = configuration["Neo:RpcUrl"];
            var walletPath = configuration["Neo:WalletPath"];
            var walletPassword = configuration["Neo:WalletPassword"];

            return new NeoN3BlockchainService(logger, rpcUrl, walletPath, walletPassword);
        });

        // Register TEE services
        builder.Services.Configure<TeeEnclaveSettings>(builder.Configuration.GetSection("Tee"));

        // Register the TEE interface factory
        TeeInterfaceFactory.RegisterServices(builder.Services, builder.Configuration);

        builder.Services.AddSingleton<NeoServiceLayer.Core.Interfaces.ITeeHostService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<TeeHostService>>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            var teeInterfaceFactory = provider.GetRequiredService<ITeeInterfaceFactory>();

            var teeHostService = new TeeHostService(configuration, logger, teeInterfaceFactory);

            // Initialize the TEE asynchronously
            Task.Run(async () =>
            {
                try
                {
                    var teeSettings = configuration.GetSection("Tee").Get<TeeEnclaveSettings>();
                    var simulationMode = teeSettings?.SimulationMode ?? true;
                    var occlumSupport = teeSettings?.OcclumSupport ?? false;

                    await teeHostService.InitializeTeeAsync();

                    logger.LogInformation(
                        "TEE initialized successfully (Type: {TeeType}, Simulation Mode: {SimulationMode}, Occlum Support: {OcclumSupport})",
                        teeSettings?.Type ?? "OpenEnclave",
                        simulationMode,
                        occlumSupport);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to initialize TEE");
                }
            });

            return (NeoServiceLayer.Core.Interfaces.ITeeHostService)teeHostService;
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Service Layer API v1");
                options.SwaggerEndpoint("/swagger/v2/swagger.json", "Neo Service Layer API v2");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Neo Service Layer API Documentation";
                options.DefaultModelsExpandDepth(0); // Hide schemas section by default
                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();
                options.EnableTryItOutByDefault();
            });
        }

        // Initialize persistent storage
        var storageService = app.Services.GetRequiredService<IPersistentStorageService>();
        var storageOptions = new PersistentStorageOptions
        {
            StoragePath = app.Configuration["Tee:Storage:StoragePath"] ?? app.Configuration["Tee:Occlum:TempDir"] ?? "/tmp",
            EnableEncryption = app.Configuration.GetValue<bool>("Tee:Storage:EnableEncryption", true),
            EncryptionKey = Encoding.UTF8.GetBytes(app.Configuration["Tee:Storage:EncryptionKey"] ?? "NeoServiceLayerEncryptionKey12345678901"),
            EnableCompression = app.Configuration.GetValue<bool>("Tee:Storage:EnableCompression", true),
            CompressionLevel = app.Configuration.GetValue<int>("Tee:Storage:CompressionLevel", 6),
            MaxChunkSize = app.Configuration.GetValue<int>("Tee:Storage:MaxChunkSize", 4 * 1024 * 1024),
            EnableCaching = app.Configuration.GetValue<bool>("Tee:Storage:EnableCaching", true),
            CacheSizeBytes = app.Configuration.GetValue<long>("Tee:Storage:CacheSizeBytes", 50 * 1024 * 1024),
            EnableAutoFlush = app.Configuration.GetValue<bool>("Tee:Storage:EnableAutoFlush", true),
            AutoFlushIntervalMs = app.Configuration.GetValue<int>("Tee:Storage:AutoFlushIntervalMs", 5000),
            CreateIfNotExists = true,
            EnableLogging = true,
            LogLevel = app.Configuration["Tee:Storage:LogLevel"] ?? "Information"
        };

        try
        {
            storageService.InitializeAsync(storageOptions).GetAwaiter().GetResult();
            app.Logger.LogInformation("Persistent storage initialized successfully");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Failed to initialize persistent storage");
        }
        else
        {
            // In production, only expose Swagger with API key
            app.UseSwagger(options =>
            {
                options.PreSerializeFilters.Add((swagger, httpReq) =>
                {
                    swagger.Servers = new List<OpenApiServer>
                    {
                        new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }
                    };
                });
            });

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Service Layer API v1");
                options.SwaggerEndpoint("/swagger/v2/swagger.json", "Neo Service Layer API v2");
                options.RoutePrefix = "api-docs";
                options.DocumentTitle = "Neo Service Layer API Documentation";
                options.DefaultModelsExpandDepth(0);
                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();

                // Require API key in production
                options.ConfigObject.AdditionalItems.Add("apiKeyName", "X-API-Key");
                options.ConfigObject.AdditionalItems.Add("apiKeyValue", app.Configuration["Swagger:ApiKey"]);
            });
        }

        app.UseHttpsRedirection();

        app.UseCors("AllowSpecificOrigins");

        app.UseRateLimiter();

        // Enable authentication and authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Add custom middleware
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<MetricsMiddleware>();

        app.MapControllers();

        // Add health checks endpoint
        app.MapHealthChecks("/health");

        app.Run();
    }
}

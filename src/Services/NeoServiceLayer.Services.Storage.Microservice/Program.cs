using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Neo.Storage.Service.Data;
using Neo.Storage.Service.Services;
using Neo.Storage.Service.BackgroundServices;
using Neo.Shared.Infrastructure.Extensions;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.Seq(context.Configuration.GetConnectionString("Seq") ?? "http://seq:5341")
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ServiceName", "neo-storage-service")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);
});

// Database configuration
builder.Services.AddDbContext<StorageDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Service registrations
builder.Services.AddScoped<IStorageObjectService, StorageObjectService>();
builder.Services.AddScoped<IBucketService, BucketService>();
builder.Services.AddScoped<IReplicationService, ReplicationService>();
builder.Services.AddScoped<IStorageNodeService, StorageNodeService>();
builder.Services.AddScoped<IStorageTransactionService, StorageTransactionService>();
builder.Services.AddScoped<IDistributedHashService, DistributedHashService>();
builder.Services.AddSingleton<IShardingService, ConsistentHashShardingService>();

// Background services
builder.Services.AddHostedService<ReplicationManagerService>();
builder.Services.AddHostedService<NodeHealthMonitorService>();
builder.Services.AddHostedService<StorageMaintenanceService>();
builder.Services.AddHostedService<TransactionProcessorService>();

// HTTP clients for inter-node communication
builder.Services.AddHttpClient<IStorageNodeService, StorageNodeService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "Neo-Storage-Service/1.0");
});

// Redis for distributed caching and coordination
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "NeoStorage";
});

// Infrastructure services
builder.Services.AddNeoInfrastructure(builder.Configuration);

// Authentication & Authorization
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.Audience = "neo-service-layer";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StorageRead", policy =>
        policy.RequireClaim("scope", "storage:read"));
    options.AddPolicy("StorageWrite", policy =>
        policy.RequireClaim("scope", "storage:write"));
    options.AddPolicy("StorageAdmin", policy =>
        policy.RequireClaim("role", "storage-admin"));
    options.AddPolicy("NodeOperator", policy =>
        policy.RequireClaim("role", "storage-node-operator"));
});

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Neo Storage Service API", 
        Version = "v1",
        Description = "Neo Storage Service - Distributed object storage with replication and sharding",
        Contact = new OpenApiContact
        {
            Name = "Neo Service Layer Team",
            Email = "support@neo-service-layer.com"
        }
    });
    
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    
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
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
    .AddDbContextCheck<StorageDbContext>()
    .AddCheck<StorageNodeHealthCheck>("storage-nodes")
    .AddCheck<ReplicationHealthCheck>("replication-health");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("neo-storage-service")
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("neo-storage-service", "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["service.namespace"] = "neo-services",
                    ["k8s.cluster.name"] = "neo-cluster",
                    ["service.type"] = "storage",
                    ["storage.distributed"] = true,
                    ["storage.replication.enabled"] = true
                }))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddRedisInstrumentation()
            .AddJaegerExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["Jaeger:Endpoint"] ?? "http://jaeger-collector:14268/api/traces");
            });
    });

// Rate limiting for storage operations
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 200, // Higher limit for storage operations
                QueueLimit = 50,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// CORS for web applications
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Storage Service API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Database migration and storage initialization
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StorageDbContext>();
    context.Database.Migrate();
    
    // Initialize storage nodes and sharding
    var nodeService = scope.ServiceProvider.GetRequiredService<IStorageNodeService>();
    var shardingService = scope.ServiceProvider.GetRequiredService<IShardingService>();
    
    await nodeService.InitializeStorageNodesAsync();
    await shardingService.InitializeAsync();
    
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Storage service initialized with distributed capabilities");
}

app.Run();
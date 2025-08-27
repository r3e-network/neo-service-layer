using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Neo.Compute.Service.Data;
using Neo.Compute.Service.Services;
using Neo.Compute.Service.BackgroundServices;
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
        .Enrich.WithProperty("ServiceName", "neo-compute-service")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);
});

// Database configuration
builder.Services.AddDbContext<ComputeDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Service registrations
builder.Services.AddScoped<IComputeJobService, ComputeJobService>();
builder.Services.AddScoped<ISgxEnclaveService, SgxEnclaveService>();
builder.Services.AddScoped<IAttestationService, AttestationService>();
builder.Services.AddScoped<ISecureComputationService, SecureComputationService>();
builder.Services.AddScoped<IResourceAllocationService, ResourceAllocationService>();
builder.Services.AddSingleton<ISgxHardwareService, SgxHardwareService>();

// Background services
builder.Services.AddHostedService<EnclaveManagerService>();
builder.Services.AddHostedService<AttestationValidationService>();
builder.Services.AddHostedService<ComputeJobProcessorService>();
builder.Services.AddHostedService<ResourceMonitoringService>();

// HTTP clients
builder.Services.AddHttpClient<IAttestationService, AttestationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Add("User-Agent", "Neo-Compute-Service/1.0");
});

// Redis for job queuing and caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "NeoCompute";
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
    options.AddPolicy("ComputeRead", policy =>
        policy.RequireClaim("scope", "compute:read"));
    options.AddPolicy("ComputeWrite", policy =>
        policy.RequireClaim("scope", "compute:write"));
    options.AddPolicy("ComputeAdmin", policy =>
        policy.RequireClaim("role", "compute-admin"));
    options.AddPolicy("SgxOperator", policy =>
        policy.RequireClaim("role", "sgx-operator"));
});

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Neo Compute Service API", 
        Version = "v1",
        Description = "Neo Compute Service - SGX-enabled secure computation platform",
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
    .AddDbContextCheck<ComputeDbContext>()
    .AddCheck<SgxHardwareHealthCheck>("sgx-hardware")
    .AddCheck<EnclaveHealthCheck>("enclaves");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("neo-compute-service")
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("neo-compute-service", "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["service.namespace"] = "neo-services",
                    ["k8s.cluster.name"] = "neo-cluster",
                    ["service.type"] = "compute",
                    ["sgx.enabled"] = Environment.GetEnvironmentVariable("SGX_ENABLED") == "true"
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

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100, // Lower limit for compute resources
                QueueLimit = 20,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                Window = TimeSpan.FromMinutes(1)
            }));
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Compute Service API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Database migration and SGX initialization
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ComputeDbContext>();
    context.Database.Migrate();
    
    // Initialize SGX hardware detection
    var sgxService = scope.ServiceProvider.GetRequiredService<ISgxHardwareService>();
    await sgxService.InitializeAsync();
    
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var sgxEnabled = await sgxService.IsSgxAvailableAsync();
    logger.LogInformation("SGX Hardware Detection: {SgxStatus}", sgxEnabled ? "Available" : "Not Available");
}

app.Run();
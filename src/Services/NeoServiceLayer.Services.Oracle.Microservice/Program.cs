using Neo.Oracle.Service.Data;
using Neo.Oracle.Service.Services;
using Neo.Oracle.Service.BackgroundServices;
using NeoServiceLayer.Common.Extensions;
using Hangfire;
using Hangfire.PostgreSql;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add common Neo Service Layer components
builder.Services.AddNeoServiceCommon(builder.Configuration, Assembly.GetExecutingAssembly());
builder.Services.AddNeoServiceDatabase<OracleDbContext>(builder.Configuration);
builder.Services.AddNeoServiceAuthentication(builder.Configuration);
builder.Services.AddNeoServiceTelemetry(builder.Configuration, "neo-oracle-service");
builder.Services.AddNeoServiceCors(builder.Configuration);

// Hangfire for background jobs
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// Service registrations
builder.Services.AddScoped<IOracleDataService, OracleDataService>();
builder.Services.AddScoped<IPriceFeedService, PriceFeedService>();
builder.Services.AddScoped<IConsensusService, ConsensusService>();
builder.Services.AddScoped<IDataValidationService, DataValidationService>();
builder.Services.AddScoped<IExternalApiService, ExternalApiService>();
builder.Services.AddSingleton<IOracleConfigurationService, OracleConfigurationService>();

// Background services
builder.Services.AddHostedService<PriceFeedCollectorService>();
builder.Services.AddHostedService<ConsensusEngineService>();
builder.Services.AddHostedService<DataValidationService>();

// HTTP clients for external APIs
builder.Services.AddHttpClient<IExternalApiService, ExternalApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "Neo-Oracle-Service/1.0");
});

// Redis for caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "NeoOracle";
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
    options.AddPolicy("OracleRead", policy =>
        policy.RequireClaim("scope", "oracle:read"));
    options.AddPolicy("OracleWrite", policy =>
        policy.RequireClaim("scope", "oracle:write"));
    options.AddPolicy("OracleAdmin", policy =>
        policy.RequireClaim("role", "oracle-admin"));
});

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Neo Oracle Service API", 
        Version = "v1",
        Description = "Neo Oracle Service - Decentralized data feeds and consensus mechanisms",
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
    .AddDbContextCheck<OracleDbContext>()
    .AddCheck<ExternalApiHealthCheck>("external-apis");

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("neo-oracle-service")
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("neo-oracle-service", "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["service.namespace"] = "neo-services",
                    ["k8s.cluster.name"] = "neo-cluster",
                    ["service.type"] = "oracle"
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
                PermitLimit = 1000,
                QueueLimit = 100,
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Neo Oracle Service API v1");
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

// Hangfire dashboard (only in development)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// Database migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OracleDbContext>();
    context.Database.Migrate();
    
    // Initialize oracle configurations
    var configService = scope.ServiceProvider.GetRequiredService<IOracleConfigurationService>();
    await configService.InitializeDefaultConfigurationsAsync();
}

app.Run();
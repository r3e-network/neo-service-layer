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

// Oracle-specific authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OracleRead", policy =>
        policy.RequireRole("SystemAdmin", "ServiceUser", "ServiceAdmin"));
    options.AddPolicy("OracleWrite", policy =>
        policy.RequireRole("SystemAdmin", "ServiceAdmin"));
    options.AddPolicy("OracleAdmin", policy =>
        policy.RequireRole("SystemAdmin"));
});

// Additional health checks specific to Oracle service
builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
    .AddDbContextCheck<OracleDbContext>()
    .AddCheck<ExternalApiHealthCheck>("external-apis");

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Configure pipeline with common middleware
app.UseMiddleware<NeoServiceLayer.Common.Middleware.CorrelationIdMiddleware>();
app.UseMiddleware<NeoServiceLayer.Common.Middleware.RequestResponseLoggingMiddleware>();
app.UseMiddleware<NeoServiceLayer.Common.Middleware.GlobalExceptionHandlingMiddleware>();

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
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/v1/monitoring/health");
app.MapHealthChecks("/api/v1/monitoring/ready");
app.MapHealthChecks("/api/v1/monitoring/live");

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
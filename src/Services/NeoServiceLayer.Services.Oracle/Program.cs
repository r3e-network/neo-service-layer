using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Framework;
using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.ServiceFramework.Extensions;
using NeoServiceLayer.Tee.Host.Extensions;
using NeoServiceLayer.Infrastructure.Blockchain.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add core framework services
builder.Services.AddCoreFrameworkServices(builder.Configuration);

// Add TEE and enclave services
builder.Services.AddTeeServices(builder.Configuration);

// Add blockchain infrastructure
builder.Services.AddBlockchainInfrastructure(builder.Configuration);

// Add service framework
builder.Services.AddServiceFramework(builder.Configuration);

// Register Oracle service
builder.Services.AddScoped<IOracleService, OracleService>();

// Add hosted service for Oracle service lifecycle
builder.Services.AddHostedService<OracleServiceWorker>();

// Build and run the host
var host = builder.Build();

// Log startup information
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Oracle Service with SGX confidential computing support");

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Oracle Service crashed during startup");
    throw;
}
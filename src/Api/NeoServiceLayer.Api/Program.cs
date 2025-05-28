using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Services.Randomness;
using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Services.Compute;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.Compliance;
using NeoServiceLayer.Services.EventSubscription;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.Services.Automation;
using NeoServiceLayer.Services.CrossChain;
using NeoServiceLayer.Services.ProofOfReserve;
using NeoServiceLayer.Services.ZeroKnowledge;
using NeoServiceLayer.AI.Prediction;
using NeoServiceLayer.AI.PatternRecognition;
using NeoServiceLayer.Advanced.FairOrdering;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Neo Service Layer framework
builder.Services.AddNeoServiceFramework();

// Add enclave services
builder.Services.AddSingleton<IEnclaveManager, EnclaveManager>();
builder.Services.AddHostedService<EnclaveHostService>();

// Add blockchain clients
builder.Services.AddBlockchainClients(new Dictionary<BlockchainType, string>
{
    { BlockchainType.NeoN3, "http://localhost:10332" },
    { BlockchainType.NeoX, "http://localhost:8545" }
});

// Add Neo Service Layer services

// Core Infrastructure Services (11 total - 10 implemented, 1 planned)
builder.Services.AddNeoService<IRandomnessService, RandomnessService>();
builder.Services.AddNeoService<IOracleService, OracleService>();
builder.Services.AddNeoService<IKeyManagementService, KeyManagementService>();
builder.Services.AddNeoService<IComputeService, ComputeService>();
builder.Services.AddNeoService<IStorageService, StorageService>();
builder.Services.AddNeoService<IComplianceService, ComplianceService>();
builder.Services.AddNeoService<IEventSubscriptionService, EventSubscriptionService>();
builder.Services.AddNeoService<IAbstractAccountService, AbstractAccountService>();
builder.Services.AddNeoService<IAutomationService, AutomationService>();
builder.Services.AddNeoService<ICrossChainService, CrossChainService>();
builder.Services.AddNeoService<IProofOfReserveService, ProofOfReserveService>();
builder.Services.AddNeoService<IZeroKnowledgeService, ZeroKnowledgeService>();

// Specialized AI Services (2 total - 2 implemented)
builder.Services.AddNeoService<IPredictionService, PredictionService>();
builder.Services.AddNeoService<IPatternRecognitionService, PatternRecognitionService>();

// Advanced Infrastructure Services (2 total - 1 implemented, 1 planned)
builder.Services.AddNeoService<IFairOrderingService, FairOrderingService>();
// TODO: Add Future Service when ecosystem requirements are defined

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Register all Neo Service Layer services
app.Services.RegisterAllNeoServices();

// Define API endpoints
app.MapGet("/services", (IServiceRegistry registry) =>
{
    var services = registry.GetAllServices();
    return Results.Ok(services.Select(s => new
    {
        s.Name,
        s.Description,
        s.Version,
        Health = s.GetHealthAsync().Result.ToString(),
        Capabilities = s.GetCapabilities().Select(c => c.Name).ToArray(),
        Metadata = s.GetMetadata()
    }));
})
.WithName("GetServices")
.WithOpenApi();

app.Run();

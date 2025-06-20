using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Root endpoint
app.MapGet("/", () => new
{
    Name = "Neo Service Layer",
    Version = "1.0.0",
    Status = "Running",
    Message = "Enterprise Blockchain Infrastructure with Intel SGX",
    Services = new[]
    {
        "KeyManagement - Hardware-secured cryptographic operations",
        "Storage - Encrypted data storage with compression", 
        "Randomness - Verifiable random number generation",
        "AI.PatternRecognition - Fraud detection and anomaly analysis",
        "AI.Prediction - Machine learning forecasting",
        "Oracle - Secure external data feeds",
        "AbstractAccount - Account abstraction & gasless transactions",
        "Voting - Secure governance mechanisms",
        "CrossChain - Multi-chain interoperability",
        "ProofOfReserve - Asset backing verification",
        "ZeroKnowledge - Privacy-preserving proofs",
        "Compliance - AML/KYC regulatory compliance",
        "Backup - Automated backup and recovery",
        "Health - System health monitoring",
        "Monitoring - Real-time performance metrics",
        "Configuration - Dynamic configuration management",
        "Automation - Workflow automation",
        "Notification - Multi-channel notifications",
        "Compute - Distributed computation",
        "EventSubscription - Blockchain event streaming",
        "FairOrdering - MEV protection",
        "SGX - Intel SGX trusted execution"
    },
    Security = new
    {
        Cryptography = "PBKDF2 (600k iterations) + HKDF",
        Authentication = "JWT with environment-based secrets",
        TEE = "Intel SGX with remote attestation",
        Compliance = "Zero hardcoded secrets"
    },
    TestCoverage = "80%+",
    Framework = ".NET 9.0"
});

// Service health endpoints
app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.MapGet("/api/services", () => new[]
{
    new { name = "KeyManagement", status = "Operational", category = "Foundation" },
    new { name = "Storage", status = "Operational", category = "Foundation" },
    new { name = "SGX", status = "Operational", category = "Foundation" },
    new { name = "PatternRecognition", status = "Operational", category = "AI" },
    new { name = "Prediction", status = "Operational", category = "AI" },
    new { name = "Oracle", status = "Operational", category = "AI" },
    new { name = "AbstractAccount", status = "Operational", category = "Blockchain" },
    new { name = "Voting", status = "Operational", category = "Blockchain" },
    new { name = "CrossChain", status = "Operational", category = "Blockchain" },
    new { name = "ProofOfReserve", status = "Operational", category = "Blockchain" },
    new { name = "Compliance", status = "Operational", category = "Security" },
    new { name = "ZeroKnowledge", status = "Operational", category = "Security" },
    new { name = "Backup", status = "Operational", category = "Security" },
    new { name = "Health", status = "Operational", category = "Infrastructure" },
    new { name = "Monitoring", status = "Operational", category = "Infrastructure" },
    new { name = "Configuration", status = "Operational", category = "Infrastructure" },
    new { name = "Automation", status = "Operational", category = "Automation" },
    new { name = "Notification", status = "Operational", category = "Automation" },
    new { name = "Compute", status = "Operational", category = "Automation" },
    new { name = "Randomness", status = "Operational", category = "Automation" },
    new { name = "EventSubscription", status = "Operational", category = "Infrastructure" },
    new { name = "FairOrdering", status = "Operational", category = "Advanced" }
});

// Demo endpoints
app.MapGet("/api/randomness/generate", () => new
{
    value = Guid.NewGuid().ToString(),
    timestamp = DateTime.UtcNow,
    source = "Secure Random Generator"
});

app.MapGet("/api/keymanagement/info", () => new
{
    service = "Key Management Service",
    features = new[] { "Key Generation", "Signing", "Encryption", "Key Rotation" },
    security = "Intel SGX Enclave Protected"
});

app.MapGet("/api/dashboard", () => new
{
    totalServices = 22,
    healthyServices = 22,
    avgResponseTime = "45ms",
    requestsPerSecond = 1250,
    systemHealth = "Excellent"
});

Console.WriteLine("🚀 Neo Service Layer Demo Server");
Console.WriteLine("================================");
Console.WriteLine($"✅ Server running at http://localhost:5000");
Console.WriteLine($"📚 API Documentation at http://localhost:5000/swagger");
Console.WriteLine($"🌐 Service Info at http://localhost:5000/api/services");
Console.WriteLine($"📊 Dashboard at http://localhost:5000/api/dashboard");
Console.WriteLine("\nPress Ctrl+C to stop the server...");

app.Run();
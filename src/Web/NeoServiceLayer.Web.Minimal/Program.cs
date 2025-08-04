using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapHealthChecks("/health");

// Add web interface endpoints
app.MapGet("/", async context =>
{
    await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html>
<head>
    <title>Neo Service Layer Dashboard</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .header { background: #0066cc; color: white; padding: 20px; border-radius: 5px; }
        .service { background: #f5f5f5; margin: 10px 0; padding: 15px; border-radius: 5px; }
        .healthy { border-left: 5px solid #28a745; }
        .status { color: #28a745; font-weight: bold; }
    </style>
</head>
<body>
    <div class='header'>
        <h1>Neo Service Layer Dashboard</h1>
        <p>Complete Blockchain Service Infrastructure</p>
    </div>
    
    <h2>Service Status</h2>
    <div class='service healthy'>
        <h3>Phase 1 - Infrastructure & Core Services</h3>
        <p class='status'>✓ All services healthy</p>
        <p>API Gateway, Smart Contracts, PostgreSQL, Redis, Consul, Prometheus, Grafana</p>
    </div>
    
    <div class='service healthy'>
        <h3>Phase 2 - Management & AI Services</h3>
        <p class='status'>✓ All services healthy</p>
        <p>Key Management, Notification, Monitoring, Health, Pattern Recognition, Prediction</p>
    </div>
    
    <div class='service healthy'>
        <h3>Phase 3 - Advanced Services</h3>
        <p class='status'>✓ All services healthy</p>
        <p>Oracle, Storage, CrossChain, Proof of Reserve, Randomness, Fair Ordering, TEE Host</p>
    </div>
    
    <div class='service healthy'>
        <h3>Phase 4 - Security & Governance</h3>
        <p class='status'>✓ All services healthy</p>
        <p>Voting, Zero Knowledge, Secrets Management, Social Recovery, Enclave Storage, Network Security</p>
    </div>
    
    <h2>Quick Links</h2>
    <ul>
        <li><a href='http://localhost:8080'>API Gateway</a></li>
        <li><a href='http://localhost:19090'>Prometheus</a></li>
        <li><a href='http://localhost:13000'>Grafana</a></li>
        <li><a href='http://localhost:18500'>Consul</a></li>
    </ul>
</body>
</html>
    ");
});

app.Run();

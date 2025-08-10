#!/bin/bash
# Neo Service Layer - Create New Service Script
# Generates a complete service structure using the framework

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Function to display usage
usage() {
    echo -e "${GREEN}Neo Service Layer - Service Generator${NC}"
    echo "====================================="
    echo ""
    echo "Usage: $0 <ServiceName> [options]"
    echo ""
    echo "Arguments:"
    echo "  ServiceName       Name of the service (e.g., 'Messaging', 'Analytics')"
    echo ""
    echo "Options:"
    echo "  --enclave        Include enclave support (Intel SGX)"
    echo "  --blockchain     Include blockchain support (Neo N3/X)"
    echo "  --ai             Use AI service base"
    echo "  --crypto         Use cryptographic service base"
    echo "  --data           Use data service base"
    echo "  --minimal        Create minimal version (without full implementation)"
    echo "  --no-tests       Skip test project creation"
    echo "  --no-docker      Skip Dockerfile creation"
    echo ""
    echo "Examples:"
    echo "  $0 Messaging"
    echo "  $0 SecureCompute --enclave --blockchain"
    echo "  $0 Analytics --data --minimal"
    exit 1
}

# Check arguments
if [ $# -lt 1 ]; then
    usage
fi

SERVICE_NAME="$1"
shift

# Default options
INCLUDE_ENCLAVE=false
INCLUDE_BLOCKCHAIN=false
USE_AI_BASE=false
USE_CRYPTO_BASE=false
USE_DATA_BASE=false
CREATE_MINIMAL=false
CREATE_TESTS=true
CREATE_DOCKER=true

# Parse options
while [[ $# -gt 0 ]]; do
    case $1 in
        --enclave)
            INCLUDE_ENCLAVE=true
            shift
            ;;
        --blockchain)
            INCLUDE_BLOCKCHAIN=true
            shift
            ;;
        --ai)
            USE_AI_BASE=true
            shift
            ;;
        --crypto)
            USE_CRYPTO_BASE=true
            shift
            ;;
        --data)
            USE_DATA_BASE=true
            shift
            ;;
        --minimal)
            CREATE_MINIMAL=true
            shift
            ;;
        --no-tests)
            CREATE_TESTS=false
            shift
            ;;
        --no-docker)
            CREATE_DOCKER=false
            shift
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            usage
            ;;
    esac
done

# Validate service name
if [[ ! "$SERVICE_NAME" =~ ^[A-Z][a-zA-Z0-9]+$ ]]; then
    echo -e "${RED}Error: Service name must start with uppercase and contain only alphanumeric characters${NC}"
    exit 1
fi

# Set paths
BASE_DIR="$(pwd)"
SERVICE_DIR="src/Services/NeoServiceLayer.Services.$SERVICE_NAME"
TEST_DIR="tests/Services/NeoServiceLayer.Services.$SERVICE_NAME.Tests"
NAMESPACE="NeoServiceLayer.Services.$SERVICE_NAME"

echo -e "${GREEN}Creating new service: $SERVICE_NAME${NC}"
echo "=================================="
echo -e "${BLUE}Options:${NC}"
echo "  Include Enclave: $INCLUDE_ENCLAVE"
echo "  Include Blockchain: $INCLUDE_BLOCKCHAIN"
echo "  AI Base: $USE_AI_BASE"
echo "  Crypto Base: $USE_CRYPTO_BASE"
echo "  Data Base: $USE_DATA_BASE"
echo "  Minimal: $CREATE_MINIMAL"
echo "  Tests: $CREATE_TESTS"
echo "  Docker: $CREATE_DOCKER"
echo ""

# Create directories
echo -e "${YELLOW}Creating directory structure...${NC}"
mkdir -p "$SERVICE_DIR"
mkdir -p "$SERVICE_DIR/Models"
mkdir -p "$SERVICE_DIR/Configuration"

if [ "$CREATE_TESTS" = true ]; then
    mkdir -p "$TEST_DIR"
fi

# Determine base class
BASE_CLASS="ServiceBase"
if [ "$INCLUDE_ENCLAVE" = true ] && [ "$INCLUDE_BLOCKCHAIN" = true ]; then
    BASE_CLASS="EnclaveBlockchainServiceBase"
elif [ "$INCLUDE_ENCLAVE" = true ]; then
    BASE_CLASS="EnclaveServiceBase"
elif [ "$INCLUDE_BLOCKCHAIN" = true ]; then
    BASE_CLASS="BlockchainServiceBase"
elif [ "$USE_AI_BASE" = true ]; then
    BASE_CLASS="AIServiceBase"
elif [ "$USE_CRYPTO_BASE" = true ]; then
    BASE_CLASS="CryptographicServiceBase"
elif [ "$USE_DATA_BASE" = true ]; then
    BASE_CLASS="DataServiceBase"
fi

# Create service interface
echo -e "${YELLOW}Creating service interface...${NC}"
cat > "$SERVICE_DIR/I${SERVICE_NAME}Service.cs" << EOF
using NeoServiceLayer.Core;

namespace $NAMESPACE;

/// <summary>
/// Interface for the $SERVICE_NAME service.
/// </summary>
public interface I${SERVICE_NAME}Service : IService
{
    /// <summary>
    /// Example method for the $SERVICE_NAME service.
    /// </summary>
    /// <param name="request">The request object.</param>
    /// <returns>The response object.</returns>
    Task<${SERVICE_NAME}Response> Process${SERVICE_NAME}Async(${SERVICE_NAME}Request request);

    /// <summary>
    /// Gets the current status of the service.
    /// </summary>
    /// <returns>The service status.</returns>
    Task<${SERVICE_NAME}Status> GetStatusAsync();
}
EOF

# Create models
echo -e "${YELLOW}Creating models...${NC}"
cat > "$SERVICE_DIR/Models/${SERVICE_NAME}Request.cs" << EOF
namespace $NAMESPACE.Models;

/// <summary>
/// Request model for $SERVICE_NAME operations.
/// </summary>
public class ${SERVICE_NAME}Request
{
    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
EOF

cat > "$SERVICE_DIR/Models/${SERVICE_NAME}Response.cs" << EOF
namespace $NAMESPACE.Models;

/// <summary>
/// Response model for $SERVICE_NAME operations.
/// </summary>
public class ${SERVICE_NAME}Response
{
    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the result.
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if any.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the processing time in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}
EOF

cat > "$SERVICE_DIR/Models/${SERVICE_NAME}Status.cs" << EOF
namespace $NAMESPACE.Models;

/// <summary>
/// Status model for the $SERVICE_NAME service.
/// </summary>
public class ${SERVICE_NAME}Status
{
    /// <summary>
    /// Gets or sets the total requests processed.
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets the successful requests.
    /// </summary>
    public long SuccessfulRequests { get; set; }

    /// <summary>
    /// Gets or sets the failed requests.
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// Gets or sets the average processing time in milliseconds.
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the last request time.
    /// </summary>
    public DateTime? LastRequestTime { get; set; }

    /// <summary>
    /// Gets or sets additional status information.
    /// </summary>
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}
EOF

# Create service implementation
echo -e "${YELLOW}Creating service implementation...${NC}"
cat > "$SERVICE_DIR/${SERVICE_NAME}Service.cs" << EOF
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using $NAMESPACE.Models;
EOF

if [ "$INCLUDE_ENCLAVE" = true ]; then
    echo "using NeoServiceLayer.Tee.Host.Services;" >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs"
fi

if [ "$INCLUDE_BLOCKCHAIN" = true ]; then
    echo "using NeoServiceLayer.Infrastructure;" >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs"
fi

cat >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs" << EOF

namespace $NAMESPACE;

/// <summary>
/// Implementation of the $SERVICE_NAME service.
/// </summary>
public class ${SERVICE_NAME}Service : $BASE_CLASS, I${SERVICE_NAME}Service
{
EOF

if [ "$INCLUDE_ENCLAVE" = true ]; then
    echo "    private readonly IEnclaveManager _enclaveManager;" >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs"
fi

if [ "$INCLUDE_BLOCKCHAIN" = true ]; then
    echo "    private readonly IBlockchainClientFactory _blockchainClientFactory;" >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs"
fi

cat >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs" << EOF
    private readonly IServiceConfiguration _configuration;
    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private readonly List<long> _processingTimes = new();
    private DateTime? _lastRequestTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="${SERVICE_NAME}Service"/> class.
    /// </summary>
EOF

# Add constructor parameters documentation
if [ "$INCLUDE_ENCLAVE" = true ]; then
    echo "    /// <param name=\"enclaveManager\">The enclave manager.</param>" >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs"
fi

if [ "$INCLUDE_BLOCKCHAIN" = true ]; then
    echo "    /// <param name=\"blockchainClientFactory\">The blockchain client factory.</param>" >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs"
fi

cat >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs" << EOF
    /// <param name="configuration">The service configuration.</param>
    /// <param name="logger">The logger.</param>
    public ${SERVICE_NAME}Service(
EOF

# Add constructor parameters
CTOR_PARAMS=""
if [ "$INCLUDE_ENCLAVE" = true ]; then
    CTOR_PARAMS="IEnclaveManager enclaveManager,\n        "
fi

if [ "$INCLUDE_BLOCKCHAIN" = true ]; then
    CTOR_PARAMS="${CTOR_PARAMS}IBlockchainClientFactory blockchainClientFactory,\n        "
fi

CTOR_PARAMS="${CTOR_PARAMS}IServiceConfiguration configuration,\n        ILogger<${SERVICE_NAME}Service> logger)"

echo -e "        $CTOR_PARAMS" >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs"

# Add base constructor call
if [ "$INCLUDE_BLOCKCHAIN" = true ]; then
    echo "        : base(\"$SERVICE_NAME\", \"$SERVICE_NAME Service\", \"1.0.0\", logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX })" >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs"
else
    echo "        : base(\"$SERVICE_NAME\", \"$SERVICE_NAME Service\", \"1.0.0\", logger)" >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs"
fi

cat >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs" << EOF
    {
EOF

if [ "$INCLUDE_ENCLAVE" = true ]; then
    echo "        _enclaveManager = enclaveManager;" >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs"
fi

if [ "$INCLUDE_BLOCKCHAIN" = true ]; then
    echo "        _blockchainClientFactory = blockchainClientFactory;" >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs"
fi

cat >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs" << EOF
        _configuration = configuration;

        // Add capabilities
        AddCapability<I${SERVICE_NAME}Service>();

        // Add metadata
        SetMetadata("CreatedAt", DateTime.UtcNow.ToString("o"));
        SetMetadata("ServiceType", "$SERVICE_NAME");
        
        // Add dependencies (customize as needed)
        AddOptionalDependency("Storage", "1.0.0");
        AddOptionalDependency("KeyManagement", "1.0.0");
    }

    /// <inheritdoc/>
    public async Task<${SERVICE_NAME}Response> Process${SERVICE_NAME}Async(${SERVICE_NAME}Request request)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var stopwatch = Stopwatch.StartNew();
        _totalRequests++;
        _lastRequestTime = DateTime.UtcNow;

        try
        {
            Logger.LogInformation("Processing ${SERVICE_NAME} request: {RequestId}", request.RequestId);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Data))
            {
                throw new ArgumentException("Request data cannot be empty", nameof(request));
            }

            // Process the request (implement your logic here)
            var result = await ProcessRequestInternalAsync(request);

            stopwatch.Stop();
            _processingTimes.Add(stopwatch.ElapsedMilliseconds);
            _successfulRequests++;

            Logger.LogInformation("Successfully processed ${SERVICE_NAME} request: {RequestId} in {ElapsedMs}ms", 
                request.RequestId, stopwatch.ElapsedMilliseconds);

            return new ${SERVICE_NAME}Response
            {
                RequestId = request.RequestId,
                Result = result,
                Success = true,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _failedRequests++;

            Logger.LogError(ex, "Error processing ${SERVICE_NAME} request: {RequestId}", request.RequestId);

            return new ${SERVICE_NAME}Response
            {
                RequestId = request.RequestId,
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <inheritdoc/>
    public async Task<${SERVICE_NAME}Status> GetStatusAsync()
    {
        await Task.CompletedTask;

        var status = new ${SERVICE_NAME}Status
        {
            TotalRequests = _totalRequests,
            SuccessfulRequests = _successfulRequests,
            FailedRequests = _failedRequests,
            LastRequestTime = _lastRequestTime
        };

        if (_processingTimes.Count > 0)
        {
            status.AverageProcessingTimeMs = _processingTimes.Average();
        }

        // Add service-specific status information
        status.AdditionalInfo["ServiceVersion"] = Version;
        status.AdditionalInfo["IsRunning"] = IsRunning;
EOF

if [ "$INCLUDE_ENCLAVE" = true ]; then
    echo "        status.AdditionalInfo[\"EnclaveInitialized\"] = IsEnclaveInitialized;" >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs"
fi

cat >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs" << EOF

        return status;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing $SERVICE_NAME Service...");

            // Validate configuration
            if (!ValidateConfiguration())
            {
                Logger.LogError("Configuration validation failed");
                return false;
            }

            // Initialize service-specific components
            await InitializeComponentsAsync();

            Logger.LogInformation("$SERVICE_NAME Service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing $SERVICE_NAME Service");
            return false;
        }
    }
EOF

if [ "$INCLUDE_ENCLAVE" = true ]; then
    cat >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs" << EOF

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeEnclaveAsync()
    {
        try
        {
            Logger.LogInformation("Initializing $SERVICE_NAME Service enclave...");
            
            var result = await _enclaveManager.InitializeAsync();
            if (!result)
            {
                Logger.LogError("Failed to initialize enclave");
                return false;
            }

            Logger.LogInformation("$SERVICE_NAME Service enclave initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing $SERVICE_NAME Service enclave");
            return false;
        }
    }
EOF
fi

cat >> "$SERVICE_DIR/${SERVICE_NAME}Service.cs" << EOF

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        try
        {
            Logger.LogInformation("Starting $SERVICE_NAME Service...");

            // Start any background tasks or timers
            await StartBackgroundTasksAsync();

            Logger.LogInformation("$SERVICE_NAME Service started successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting $SERVICE_NAME Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        try
        {
            Logger.LogInformation("Stopping $SERVICE_NAME Service...");

            // Stop background tasks gracefully
            await StopBackgroundTasksAsync();

            Logger.LogInformation("$SERVICE_NAME Service stopped successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping $SERVICE_NAME Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        try
        {
            // Check service health
            var isHealthy = await CheckServiceHealthAsync();
            
            if (!isHealthy)
            {
                Logger.LogWarning("$SERVICE_NAME Service health check failed");
                return ServiceHealth.Unhealthy;
            }

            // Check if service is degraded
            var failureRate = _totalRequests > 0 ? (double)_failedRequests / _totalRequests : 0;
            if (failureRate > 0.1) // More than 10% failure rate
            {
                Logger.LogWarning("$SERVICE_NAME Service is degraded - failure rate: {FailureRate:P}", failureRate);
                return ServiceHealth.Degraded;
            }

            return ServiceHealth.Healthy;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during health check");
            return ServiceHealth.Unhealthy;
        }
    }

    /// <inheritdoc/>
    protected override async Task OnUpdateMetricsAsync()
    {
        await Task.CompletedTask;

        // Update standard metrics
        UpdateMetric("TotalRequests", _totalRequests);
        UpdateMetric("SuccessfulRequests", _successfulRequests);
        UpdateMetric("FailedRequests", _failedRequests);
        
        if (_processingTimes.Count > 0)
        {
            UpdateMetric("AverageProcessingTimeMs", _processingTimes.Average());
            UpdateMetric("MinProcessingTimeMs", _processingTimes.Min());
            UpdateMetric("MaxProcessingTimeMs", _processingTimes.Max());
        }

        if (_lastRequestTime.HasValue)
        {
            UpdateMetric("SecondsSinceLastRequest", (DateTime.UtcNow - _lastRequestTime.Value).TotalSeconds);
        }

        // Update resource metrics
        var process = Process.GetCurrentProcess();
        UpdateMetric("MemoryUsageMB", process.WorkingSet64 / (1024 * 1024));
        UpdateMetric("ThreadCount", process.Threads.Count);
    }

    #region Private Methods

    private async Task<string> ProcessRequestInternalAsync(${SERVICE_NAME}Request request)
    {
        // TODO: Implement your business logic here
        await Task.Delay(100); // Simulate processing
        
        return \$"Processed: {request.Data} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
    }

    private bool ValidateConfiguration()
    {
        // TODO: Implement configuration validation
        return _configuration != null;
    }

    private async Task InitializeComponentsAsync()
    {
        // TODO: Initialize service-specific components
        await Task.CompletedTask;
    }

    private async Task StartBackgroundTasksAsync()
    {
        // TODO: Start any background tasks
        await Task.CompletedTask;
    }

    private async Task StopBackgroundTasksAsync()
    {
        // TODO: Stop background tasks gracefully
        await Task.CompletedTask;
    }

    private async Task<bool> CheckServiceHealthAsync()
    {
        // TODO: Implement service-specific health checks
        await Task.CompletedTask;
        return true;
    }

    #endregion
}
EOF

# Create controller
echo -e "${YELLOW}Creating controller...${NC}"
cat > "$SERVICE_DIR/${SERVICE_NAME}Controller.cs" << EOF
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using $NAMESPACE.Models;

namespace $NAMESPACE;

/// <summary>
/// API controller for the $SERVICE_NAME service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ${SERVICE_NAME}Controller : ControllerBase
{
    private readonly I${SERVICE_NAME}Service _service;
    private readonly ILogger<${SERVICE_NAME}Controller> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="${SERVICE_NAME}Controller"/> class.
    /// </summary>
    /// <param name="service">The $SERVICE_NAME service.</param>
    /// <param name="logger">The logger.</param>
    public ${SERVICE_NAME}Controller(I${SERVICE_NAME}Service service, ILogger<${SERVICE_NAME}Controller> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Processes a $SERVICE_NAME request.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <returns>The processing result.</returns>
    [HttpPost("process")]
    [ProducesResponseType(typeof(${SERVICE_NAME}Response), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<IActionResult> ProcessAsync([FromBody] ${SERVICE_NAME}Request request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _service.Process${SERVICE_NAME}Async(request);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            return BadRequest(new ProblemDetails
            {
                Title = "Processing Failed",
                Detail = response.ErrorMessage,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An error occurred while processing the request",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Gets the service status.
    /// </summary>
    /// <returns>The service status.</returns>
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(${SERVICE_NAME}Status), 200)]
    public async Task<IActionResult> GetStatusAsync()
    {
        var status = await _service.GetStatusAsync();
        return Ok(status);
    }

    /// <summary>
    /// Gets the service health.
    /// </summary>
    /// <returns>The health status.</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetHealthAsync()
    {
        var health = await _service.GetHealthAsync();
        return Ok(new
        {
            status = health.ToString(),
            timestamp = DateTime.UtcNow
        });
    }
}
EOF

# Create Program.cs
echo -e "${YELLOW}Creating Program.cs...${NC}"
if [ "$CREATE_MINIMAL" = true ]; then
    # Minimal Program.cs
    cat > "$SERVICE_DIR/Program.cs" << EOF
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add service implementation
builder.Services.AddSingleton<NeoServiceLayer.Services.$SERVICE_NAME.I${SERVICE_NAME}Service, NeoServiceLayer.Services.$SERVICE_NAME.${SERVICE_NAME}Service>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
EOF
else
    # Full Program.cs with MicroserviceHost
    cat > "$SERVICE_DIR/Program.cs" << EOF
using NeoServiceLayer.ServiceFramework.ServiceHost;
using NeoServiceLayer.Services.$SERVICE_NAME;

namespace NeoServiceLayer.Services.$SERVICE_NAME;

/// <summary>
/// Entry point for the $SERVICE_NAME service.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        var host = new ${SERVICE_NAME}Host(args);
        return await host.RunAsync();
    }
}

/// <summary>
/// Host configuration for the $SERVICE_NAME service.
/// </summary>
public class ${SERVICE_NAME}Host : MicroserviceHost<${SERVICE_NAME}Service>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="${SERVICE_NAME}Host"/> class.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public ${SERVICE_NAME}Host(string[] args) : base(args)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
    {
        // Add service-specific dependencies
        services.AddSingleton<I${SERVICE_NAME}Service, ${SERVICE_NAME}Service>();
        
        // Add controllers
        services.AddControllers();
        
        // Add authorization
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = context.Configuration["Auth:Authority"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false
                };
            });
        
        services.AddAuthorization();
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<${SERVICE_NAME}HealthCheck>("${SERVICE_NAME.ToLower()}_health");
            
        // Add resilience policies
        services.AddHttpClient("default")
            .AddStandardResilienceHandler();
    }

    /// <inheritdoc/>
    protected override void MapServiceEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Map additional service-specific endpoints if needed
        endpoints.MapGet("/", async context =>
        {
            await context.Response.WriteAsync("$SERVICE_NAME Service is running!");
        });
    }
}

/// <summary>
/// Health check implementation for the $SERVICE_NAME service.
/// </summary>
public class ${SERVICE_NAME}HealthCheck : IHealthCheck
{
    private readonly I${SERVICE_NAME}Service _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="${SERVICE_NAME}HealthCheck"/> class.
    /// </summary>
    /// <param name="service">The service instance.</param>
    public ${SERVICE_NAME}HealthCheck(I${SERVICE_NAME}Service service)
    {
        _service = service;
    }

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await _service.GetHealthAsync();
            
            return health switch
            {
                ServiceHealth.Healthy => HealthCheckResult.Healthy("Service is healthy"),
                ServiceHealth.Degraded => HealthCheckResult.Degraded("Service is degraded"),
                _ => HealthCheckResult.Unhealthy("Service is unhealthy")
            };
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}
EOF
fi

# Create project file
echo -e "${YELLOW}Creating project file...${NC}"
cat > "$SERVICE_DIR/NeoServiceLayer.Services.$SERVICE_NAME.csproj" << EOF
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Core\NeoServiceLayer.Core\NeoServiceLayer.Core.csproj" />
    <ProjectReference Include="..\..\Core\NeoServiceLayer.ServiceFramework\NeoServiceLayer.ServiceFramework.csproj" />
    <ProjectReference Include="..\..\Infrastructure\NeoServiceLayer.Infrastructure\NeoServiceLayer.Infrastructure.csproj" />
EOF

if [ "$INCLUDE_ENCLAVE" = true ]; then
    echo "    <ProjectReference Include=\"..\..\Tee\NeoServiceLayer.Tee.Host\NeoServiceLayer.Tee.Host.csproj\" />" >> "$SERVICE_DIR/NeoServiceLayer.Services.$SERVICE_NAME.csproj"
fi

cat >> "$SERVICE_DIR/NeoServiceLayer.Services.$SERVICE_NAME.csproj" << EOF
  </ItemGroup>

</Project>
EOF

# Create appsettings.json
echo -e "${YELLOW}Creating configuration files...${NC}"
cat > "$SERVICE_DIR/appsettings.json" << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ServicePort": 80,
  "$SERVICE_NAME": {
    "MaxConcurrentRequests": 100,
    "RequestTimeoutSeconds": 30,
    "EnableDetailedLogging": false
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=${SERVICE_NAME,,}db;Username=neo_user;Password=\${DB_PASSWORD}"
  },
  "Redis": {
    "ConnectionString": "redis:6379"
  },
  "Auth": {
    "Authority": "https://login.microsoftonline.com/your-tenant-id",
    "Audience": "api://neo-service-layer"
  }
}
EOF

cat > "$SERVICE_DIR/appsettings.Development.json" << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  },
  "$SERVICE_NAME": {
    "EnableDetailedLogging": true
  }
}
EOF

cat > "$SERVICE_DIR/appsettings.Production.json" << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "$SERVICE_NAME": {
    "EnableDetailedLogging": false
  }
}
EOF

# Create Dockerfile if requested
if [ "$CREATE_DOCKER" = true ]; then
    echo -e "${YELLOW}Creating Dockerfile...${NC}"
    cat > "$SERVICE_DIR/Dockerfile" << EOF
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Services/NeoServiceLayer.Services.$SERVICE_NAME/NeoServiceLayer.Services.$SERVICE_NAME.csproj", "src/Services/NeoServiceLayer.Services.$SERVICE_NAME/"]
COPY ["src/Core/NeoServiceLayer.Core/NeoServiceLayer.Core.csproj", "src/Core/NeoServiceLayer.Core/"]
COPY ["src/Core/NeoServiceLayer.ServiceFramework/NeoServiceLayer.ServiceFramework.csproj", "src/Core/NeoServiceLayer.ServiceFramework/"]
COPY ["src/Infrastructure/NeoServiceLayer.Infrastructure/NeoServiceLayer.Infrastructure.csproj", "src/Infrastructure/NeoServiceLayer.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.$SERVICE_NAME/NeoServiceLayer.Services.$SERVICE_NAME.csproj"

# Copy source code
COPY . .
WORKDIR "/src/src/Services/NeoServiceLayer.Services.$SERVICE_NAME"

# Build
RUN dotnet build "NeoServiceLayer.Services.$SERVICE_NAME.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "NeoServiceLayer.Services.$SERVICE_NAME.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.$SERVICE_NAME.dll"]
EOF
fi

# Create README
echo -e "${YELLOW}Creating README...${NC}"
cat > "$SERVICE_DIR/README.md" << EOF
# $SERVICE_NAME Service

## Overview

The $SERVICE_NAME Service is a microservice in the Neo Service Layer that provides [describe functionality].

## Features

- Feature 1
- Feature 2
- Feature 3

## Architecture

The service is built using:
- **Base Class**: $BASE_CLASS
- **Framework**: NeoServiceLayer.ServiceFramework
- **Dependencies**: [List key dependencies]

## API Endpoints

### Process $SERVICE_NAME Request
\`\`\`
POST /api/${SERVICE_NAME,,}/process
Content-Type: application/json

{
  "data": "string"
}
\`\`\`

### Get Status
\`\`\`
GET /api/${SERVICE_NAME,,}/status
\`\`\`

### Health Check
\`\`\`
GET /health
\`\`\`

## Configuration

Key configuration options in \`appsettings.json\`:

\`\`\`json
{
  "$SERVICE_NAME": {
    "MaxConcurrentRequests": 100,
    "RequestTimeoutSeconds": 30,
    "EnableDetailedLogging": false
  }
}
\`\`\`

## Development

### Prerequisites
- .NET 9.0 SDK
- Docker (optional)
EOF

if [ "$INCLUDE_ENCLAVE" = true ]; then
    echo "- Intel SGX SDK and drivers" >> "$SERVICE_DIR/README.md"
fi

cat >> "$SERVICE_DIR/README.md" << EOF

### Building
\`\`\`bash
dotnet build
\`\`\`

### Running
\`\`\`bash
dotnet run
\`\`\`

### Testing
\`\`\`bash
dotnet test
\`\`\`

## Deployment

### Docker
\`\`\`bash
docker build -t neo-${SERVICE_NAME,,}-service .
docker run -p 8080:80 neo-${SERVICE_NAME,,}-service
\`\`\`

### Kubernetes
\`\`\`bash
kubectl apply -f k8s/services/${SERVICE_NAME,,}-service.yaml
\`\`\`

## Monitoring

The service exposes the following metrics:
- Total requests processed
- Success/failure rates
- Average processing time
- Resource usage

Access metrics at: \`/metrics\`

## Security Considerations

EOF

if [ "$INCLUDE_ENCLAVE" = true ]; then
    cat >> "$SERVICE_DIR/README.md" << EOF
- All sensitive operations are performed within Intel SGX enclaves
- Data is encrypted at rest and in transit
- Attestation is required for enclave initialization
EOF
fi

cat >> "$SERVICE_DIR/README.md" << EOF
- JWT authentication is required for all endpoints except health/status
- HTTPS is enforced in production
- Rate limiting is applied to prevent abuse

## License

[Your License]
EOF

# Create test project if requested
if [ "$CREATE_TESTS" = true ]; then
    echo -e "${YELLOW}Creating test project...${NC}"
    
    cat > "$TEST_DIR/NeoServiceLayer.Services.$SERVICE_NAME.Tests.csproj" << EOF
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\src\Services\NeoServiceLayer.Services.$SERVICE_NAME\NeoServiceLayer.Services.$SERVICE_NAME.csproj" />
  </ItemGroup>

</Project>
EOF

    cat > "$TEST_DIR/${SERVICE_NAME}ServiceTests.cs" << EOF
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.$SERVICE_NAME;
using NeoServiceLayer.Services.$SERVICE_NAME.Models;

namespace NeoServiceLayer.Services.$SERVICE_NAME.Tests;

public class ${SERVICE_NAME}ServiceTests
{
    private readonly Mock<ILogger<${SERVICE_NAME}Service>> _loggerMock;
    private readonly Mock<IServiceConfiguration> _configurationMock;
    private readonly ${SERVICE_NAME}Service _service;

    public ${SERVICE_NAME}ServiceTests()
    {
        _loggerMock = new Mock<ILogger<${SERVICE_NAME}Service>>();
        _configurationMock = new Mock<IServiceConfiguration>();
        
        _service = new ${SERVICE_NAME}Service(
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_ShouldReturnTrue()
    {
        // Act
        var result = await _service.InitializeAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task Process${SERVICE_NAME}Async_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        var request = new ${SERVICE_NAME}Request
        {
            Data = "test data"
        };

        // Act
        var response = await _service.Process${SERVICE_NAME}Async(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal(request.RequestId, response.RequestId);
        Assert.NotEmpty(response.Result);
    }

    [Fact]
    public async Task Process${SERVICE_NAME}Async_EmptyData_ShouldReturnError()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        
        var request = new ${SERVICE_NAME}Request
        {
            Data = ""
        };

        // Act
        var response = await _service.Process${SERVICE_NAME}Async(request);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.NotNull(response.ErrorMessage);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnCurrentStatus()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Process some requests
        await _service.Process${SERVICE_NAME}Async(new ${SERVICE_NAME}Request { Data = "test1" });
        await _service.Process${SERVICE_NAME}Async(new ${SERVICE_NAME}Request { Data = "test2" });

        // Act
        var status = await _service.GetStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.Equal(2, status.TotalRequests);
        Assert.True(status.LastRequestTime.HasValue);
    }

    [Fact]
    public async Task GetHealthAsync_WhenHealthy_ShouldReturnHealthy()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var health = await _service.GetHealthAsync();

        // Assert
        Assert.Equal(ServiceHealth.Healthy, health);
    }
}
EOF

    cat > "$TEST_DIR/${SERVICE_NAME}ControllerTests.cs" << EOF
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.$SERVICE_NAME;
using NeoServiceLayer.Services.$SERVICE_NAME.Models;

namespace NeoServiceLayer.Services.$SERVICE_NAME.Tests;

public class ${SERVICE_NAME}ControllerTests
{
    private readonly Mock<I${SERVICE_NAME}Service> _serviceMock;
    private readonly Mock<ILogger<${SERVICE_NAME}Controller>> _loggerMock;
    private readonly ${SERVICE_NAME}Controller _controller;

    public ${SERVICE_NAME}ControllerTests()
    {
        _serviceMock = new Mock<I${SERVICE_NAME}Service>();
        _loggerMock = new Mock<ILogger<${SERVICE_NAME}Controller>>();
        _controller = new ${SERVICE_NAME}Controller(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessAsync_ValidRequest_ShouldReturnOk()
    {
        // Arrange
        var request = new ${SERVICE_NAME}Request { Data = "test" };
        var response = new ${SERVICE_NAME}Response 
        { 
            RequestId = request.RequestId,
            Success = true,
            Result = "processed"
        };
        
        _serviceMock.Setup(x => x.Process${SERVICE_NAME}Async(It.IsAny<${SERVICE_NAME}Request>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.ProcessAsync(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<${SERVICE_NAME}Response>(okResult.Value);
        Assert.Equal(response.RequestId, returnedResponse.RequestId);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnStatus()
    {
        // Arrange
        var status = new ${SERVICE_NAME}Status
        {
            TotalRequests = 10,
            SuccessfulRequests = 9,
            FailedRequests = 1
        };
        
        _serviceMock.Setup(x => x.GetStatusAsync()).ReturnsAsync(status);

        // Act
        var result = await _controller.GetStatusAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedStatus = Assert.IsType<${SERVICE_NAME}Status>(okResult.Value);
        Assert.Equal(10, returnedStatus.TotalRequests);
    }

    [Fact]
    public async Task GetHealthAsync_ShouldReturnHealth()
    {
        // Arrange
        _serviceMock.Setup(x => x.GetHealthAsync()).ReturnsAsync(ServiceHealth.Healthy);

        // Act
        var result = await _controller.GetHealthAsync();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic health = okResult.Value!;
        Assert.Equal("Healthy", health.status);
    }
}
EOF
fi

# Create Kubernetes manifest
echo -e "${YELLOW}Creating Kubernetes manifest...${NC}"
mkdir -p "k8s/services"
cat > "k8s/services/${SERVICE_NAME,,}-service.yaml" << EOF
apiVersion: v1
kind: Service
metadata:
  name: ${SERVICE_NAME,,}-service
  namespace: neo-service-layer
spec:
  selector:
    app: ${SERVICE_NAME,,}-service
  ports:
  - port: 80
    targetPort: 80
    name: http
  - port: 443
    targetPort: 443
    name: https
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ${SERVICE_NAME,,}-service
  namespace: neo-service-layer
spec:
  replicas: 2
  selector:
    matchLabels:
      app: ${SERVICE_NAME,,}-service
  template:
    metadata:
      labels:
        app: ${SERVICE_NAME,,}-service
    spec:
      containers:
      - name: ${SERVICE_NAME,,}-service
        image: neo-${SERVICE_NAME,,}-service:latest
        ports:
        - containerPort: 80
        - containerPort: 443
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: neo-secrets
              key: db-password
        - name: JWT_SECRET
          valueFrom:
            secretKeyRef:
              name: neo-secrets
              key: jwt-secret
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
EOF

# Add to solution
echo -e "${YELLOW}Adding to solution...${NC}"
dotnet sln add "$SERVICE_DIR/NeoServiceLayer.Services.$SERVICE_NAME.csproj" 2>/dev/null || echo "Note: Could not add to solution automatically"

if [ "$CREATE_TESTS" = true ]; then
    dotnet sln add "$TEST_DIR/NeoServiceLayer.Services.$SERVICE_NAME.Tests.csproj" 2>/dev/null || echo "Note: Could not add test project to solution automatically"
fi

# Summary
echo ""
echo -e "${GREEN}✅ Service '$SERVICE_NAME' created successfully!${NC}"
echo ""
echo "Created files:"
echo "  - $SERVICE_DIR/I${SERVICE_NAME}Service.cs"
echo "  - $SERVICE_DIR/${SERVICE_NAME}Service.cs"
echo "  - $SERVICE_DIR/${SERVICE_NAME}Controller.cs"
echo "  - $SERVICE_DIR/Program.cs"
echo "  - $SERVICE_DIR/Models/*.cs"
echo "  - $SERVICE_DIR/appsettings.json"
echo "  - $SERVICE_DIR/NeoServiceLayer.Services.$SERVICE_NAME.csproj"
echo "  - $SERVICE_DIR/README.md"

if [ "$CREATE_DOCKER" = true ]; then
    echo "  - $SERVICE_DIR/Dockerfile"
fi

if [ "$CREATE_TESTS" = true ]; then
    echo "  - $TEST_DIR/*.cs"
    echo "  - $TEST_DIR/NeoServiceLayer.Services.$SERVICE_NAME.Tests.csproj"
fi

echo "  - k8s/services/${SERVICE_NAME,,}-service.yaml"
echo ""
echo -e "${BLUE}Next steps:${NC}"
echo "1. Implement your business logic in ${SERVICE_NAME}Service.cs"
echo "2. Update the models in Models/ directory"
echo "3. Add specific configuration in appsettings.json"
echo "4. Update the README.md with service-specific documentation"
echo "5. Build and test: dotnet build && dotnet test"
echo "6. Deploy: docker build -t neo-${SERVICE_NAME,,}-service . && kubectl apply -f k8s/services/${SERVICE_NAME,,}-service.yaml"
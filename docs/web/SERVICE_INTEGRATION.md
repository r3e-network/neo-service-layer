# Service Integration Guide

## Overview

This document explains how all Neo Service Layer services are integrated with the web application, providing a comprehensive reference for developers working with the system.

## 🏗️ Integration Architecture

### **Service Registration Pattern**

All services follow a consistent registration pattern in `Program.cs`:

```csharp
// Core Services
builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
builder.Services.AddScoped<IRandomnessService, RandomnessService>();
builder.Services.AddScoped<IOracleService, OracleService>();
builder.Services.AddScoped<IVotingService, VotingService>();

// Storage & Data Services
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Security Services
builder.Services.AddScoped<IZeroKnowledgeService, ZeroKnowledgeService>();
builder.Services.AddScoped<IAbstractAccountService, AbstractAccountService>();
builder.Services.AddScoped<IComplianceService, ComplianceService>();
builder.Services.AddScoped<IProofOfReserveService, ProofOfReserveService>();

// Operations Services
builder.Services.AddScoped<IAutomationService, AutomationService>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Infrastructure Services
builder.Services.AddScoped<ICrossChainService, CrossChainService>();
builder.Services.AddScoped<IComputeService, ComputeService>();
builder.Services.AddScoped<IEventSubscriptionService, EventSubscriptionService>();

// AI Services
builder.Services.AddScoped<IPatternRecognitionService, PatternRecognitionService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();

// TEE Services
builder.Services.AddScoped<IEnclaveWrapper, OcclumEnclaveWrapper>();
builder.Services.AddScoped<IEnclaveManager, EnclaveManager>();
```

## 📋 Service Catalog

### **Core Services**

#### **Key Management Service**
- **Controller**: `KeyManagementController.cs`
- **Endpoints**: 
  - `POST /api/keymanagement/generate/{blockchainType}`
  - `GET /api/keymanagement/list/{blockchainType}`
  - `GET /api/keymanagement/health`
- **Web Demo**: Generate demo keys, list existing keys
- **Features**: ECDSA/Ed25519 key generation, SGX-secured storage

#### **Randomness Service**
- **Controller**: `RandomnessController.cs`
- **Endpoints**:
  - `POST /api/randomness/generate`
  - `GET /api/randomness/health`
  - `GET /api/randomness/generate-simple`
- **Web Demo**: Generate random numbers and bytes
- **Features**: Cryptographically secure RNG, multiple output formats

#### **Oracle Service**
- **Controller**: `OracleController.cs`
- **Endpoints**:
  - `POST /api/oracle/feeds`
  - `GET /api/oracle/feeds`
  - `GET /api/oracle/feeds/{feedId}`
- **Web Demo**: Create data feeds, list feeds
- **Features**: External data integration, cryptographic proofs

#### **Voting Service**
- **Controller**: `VotingController.cs`
- **Endpoints**:
  - `POST /api/voting/proposals`
  - `GET /api/voting/proposals`
  - `POST /api/voting/proposals/{proposalId}/vote`
- **Web Demo**: Create proposals, list proposals
- **Features**: Decentralized voting, proposal management

### **Storage & Data Services**

#### **Storage Service**
- **Controller**: `StorageController.cs`
- **Endpoints**:
  - `POST /api/storage/store`
  - `GET /api/storage/list`
  - `GET /api/storage/retrieve/{dataId}`
- **Web Demo**: Store data, retrieve data list
- **Features**: Encrypted storage, metadata management

#### **Backup Service**
- **Controller**: `BackupController.cs`
- **Endpoints**:
  - `POST /api/backup/backups`
  - `GET /api/backup/backups`
  - `POST /api/backup/restore/{backupId}`
- **Web Demo**: Create backups, list backups
- **Features**: Automated backups, restore functionality

#### **Configuration Service**
- **Controller**: `ConfigurationController.cs`
- **Endpoints**:
  - `GET /api/configuration/settings`
  - `PUT /api/configuration/settings`
  - `GET /api/configuration/history`
- **Web Demo**: Get configuration, update settings
- **Features**: Dynamic configuration, change tracking

### **Security Services**

#### **Zero Knowledge Service**
- **Controller**: `ZeroKnowledgeController.cs`
- **Endpoints**:
  - `POST /api/zeroknowledge/proofs`
  - `POST /api/zeroknowledge/verify`
  - `GET /api/zeroknowledge/circuits`
- **Web Demo**: Create proofs, verify proofs
- **Features**: zk-SNARKs, privacy-preserving computations

#### **Abstract Account Service**
- **Controller**: `AbstractAccountController.cs`
- **Endpoints**:
  - `POST /api/abstractaccount/accounts`
  - `GET /api/abstractaccount/accounts`
  - `POST /api/abstractaccount/execute/{accountId}`
- **Web Demo**: Create accounts, list accounts
- **Features**: Smart contract accounts, gasless transactions

#### **Compliance Service**
- **Controller**: `ComplianceController.cs`
- **Endpoints**:
  - `POST /api/compliance/check`
  - `GET /api/compliance/reports`
  - `GET /api/compliance/rules`
- **Web Demo**: Run compliance checks, get reports
- **Features**: AML/KYC checks, regulatory compliance

#### **Proof of Reserve Service**
- **Controller**: `ProofOfReserveController.cs`
- **Endpoints**:
  - `POST /api/proofofreserve/generate`
  - `POST /api/proofofreserve/verify`
  - `GET /api/proofofreserve/reports`
- **Web Demo**: Generate proofs, verify reserves
- **Features**: Asset backing verification, cryptographic proofs

### **Operations Services**

#### **Automation Service**
- **Controller**: `AutomationController.cs`
- **Endpoints**:
  - `POST /api/automation/jobs`
  - `GET /api/automation/jobs`
  - `PUT /api/automation/jobs/{jobId}/toggle`
- **Web Demo**: Create jobs, list jobs
- **Features**: Workflow automation, scheduled tasks

#### **Monitoring Service**
- **Controller**: `MonitoringController.cs`
- **Endpoints**:
  - `GET /api/monitoring/metrics`
  - `GET /api/monitoring/alerts`
  - `GET /api/monitoring/performance`
- **Web Demo**: Get metrics, view alerts
- **Features**: System monitoring, performance analytics

#### **Health Service**
- **Controller**: `HealthController.cs`
- **Endpoints**:
  - `GET /api/health/check`
  - `GET /api/health/diagnostics`
  - `GET /api/health/nodes`
- **Web Demo**: Health checks, diagnostics
- **Features**: System health monitoring, diagnostics

#### **Notification Service**
- **Controller**: `NotificationController.cs`
- **Endpoints**:
  - `POST /api/notification/send`
  - `GET /api/notification/notifications`
  - `POST /api/notification/subscribe`
- **Web Demo**: Send notifications, get notifications
- **Features**: Multi-channel notifications, subscriptions

### **Infrastructure Services**

#### **Cross-Chain Service**
- **Controller**: `CrossChainController.cs`
- **Endpoints**:
  - `POST /api/crosschain/bridge`
  - `GET /api/crosschain/bridge/status`
  - `GET /api/crosschain/chains`
- **Web Demo**: Initiate bridge, check status
- **Features**: Multi-blockchain operations, asset bridging

#### **Compute Service**
- **Controller**: `ComputeController.cs`
- **Endpoints**:
  - `POST /api/compute/execute`
  - `GET /api/compute/results`
  - `GET /api/compute/jobs`
- **Web Demo**: Execute computations, get results
- **Features**: Secure TEE computations, job management

#### **Event Subscription Service**
- **Controller**: `EventSubscriptionController.cs`
- **Endpoints**:
  - `POST /api/eventsubscription/subscriptions`
  - `GET /api/eventsubscription/subscriptions`
  - `DELETE /api/eventsubscription/subscriptions/{subscriptionId}`
- **Web Demo**: Create subscriptions, list subscriptions
- **Features**: Blockchain event monitoring, webhooks

## 🔄 Integration Patterns

### **Controller Pattern**

All service controllers follow a consistent pattern:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExampleController : BaseApiController
{
    private readonly IExampleService _exampleService;
    
    public ExampleController(IExampleService exampleService, ILogger<ExampleController> logger) 
        : base(logger)
    {
        _exampleService = exampleService;
    }
    
    [HttpPost("operation")]
    public async Task<IActionResult> Operation([FromBody] OperationRequest request)
    {
        try
        {
            var result = await _exampleService.OperationAsync(request);
            return Ok(CreateResponse(result, "Operation completed successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, "Operation");
        }
    }
}
```

### **BaseApiController Features**

The `BaseApiController` provides common functionality:

```csharp
public abstract class BaseApiController : ControllerBase
{
    protected ILogger Logger { get; }
    
    protected BaseApiController(ILogger logger)
    {
        Logger = logger;
    }
    
    // Standard response creation
    protected ApiResponse<T> CreateResponse<T>(T data, string message = "Success")
    
    // Exception handling
    protected IActionResult HandleException(Exception ex, string operation)
    
    // User context
    protected string GetCurrentUserId()
    
    // Validation helpers
    protected bool ValidateRequest<T>(T request, out IActionResult? errorResult)
}
```

### **Web Integration Pattern**

Each service is integrated into the web application through:

1. **Service Card**: UI representation in ServiceDemo.cshtml
2. **JavaScript Functions**: Client-side service interaction
3. **API Endpoints**: RESTful service operations
4. **Response Display**: Formatted JSON response viewers

Example service card structure:

```html
<!-- Service Demo Card -->
<div class="col-md-6 mb-4">
    <div class="card service-card demo-section">
        <div class="card-header">
            <h5 class="mb-0">
                <i class="fas fa-icon me-2"></i>Service Name
                <span class="status-dot status-success" id="service-status"></span>
            </h5>
        </div>
        <div class="card-body">
            <p class="card-text">Service description</p>
            <div class="d-grid gap-2">
                <button class="btn btn-primary" onclick="demoOperation1()">
                    <i class="fas fa-action me-2"></i>Operation 1
                </button>
                <button class="btn btn-outline-primary" onclick="demoOperation2()">
                    <i class="fas fa-action me-2"></i>Operation 2
                </button>
            </div>
            <div id="service-result" class="mt-3" style="display: none;">
                <h6>Response:</h6>
                <pre class="response-json p-3"><code id="service-output"></code></pre>
            </div>
        </div>
    </div>
</div>
```

## 🔧 Development Guidelines

### **Adding a New Service**

1. **Create Service Interface and Implementation**
   ```csharp
   public interface INewService
   {
       Task<Result> OperationAsync(Request request);
   }
   
   public class NewService : INewService
   {
       // Implementation
   }
   ```

2. **Register Service in Program.cs**
   ```csharp
   builder.Services.AddScoped<INewService, NewService>();
   ```

3. **Create API Controller**
   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class NewServiceController : BaseApiController
   {
       // Controller implementation
   }
   ```

4. **Add Web Integration**
   - Add service card to ServiceDemo.cshtml
   - Implement JavaScript functions
   - Update service count in status overview

### **Testing Integration**

1. **Unit Tests**: Test individual service methods
2. **Controller Tests**: Test API endpoint behavior
3. **Integration Tests**: Test end-to-end service flows
4. **Web Tests**: Test UI interactions and JavaScript functions

### **Error Handling**

All services implement consistent error handling:

```csharp
try
{
    var result = await _service.OperationAsync(request);
    return Ok(CreateResponse(result, "Success message"));
}
catch (ValidationException ex)
{
    return BadRequest(CreateErrorResponse(ex.Message));
}
catch (NotFoundException ex)
{
    return NotFound(CreateErrorResponse(ex.Message));
}
catch (Exception ex)
{
    Logger.LogError(ex, "Operation failed");
    return StatusCode(500, CreateErrorResponse("Internal server error"));
}
```

## 📊 Service Status Monitoring

### **Health Check Integration**

Each service implements health checks:

```csharp
[HttpGet("health")]
public async Task<IActionResult> GetHealth()
{
    try
    {
        var health = await _service.GetHealthAsync();
        return Ok(health);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { status = "unhealthy", error = ex.Message });
    }
}
```

### **Status Indicators**

The web application displays real-time service status:
- **Green Dot**: Service operational
- **Yellow Dot**: Service warning
- **Red Dot**: Service error

## 🔒 Security Integration

### **Authentication Flow**

1. **Token Generation**: `/api/auth/demo-token` generates JWT tokens
2. **Token Validation**: All service endpoints validate JWT tokens
3. **Role Authorization**: Different operations require specific roles
4. **Secure Communication**: HTTPS encryption for all API calls

### **Role-based Access**

- **Admin**: Full access to all services
- **KeyManager**: Key management and related operations
- **ServiceUser**: General service access
- **Guest**: Read-only access (limited endpoints)

## 📚 Related Documentation

- [Web Application Guide](WEB_APPLICATION_GUIDE.md) - Main web app documentation
- [Authentication & Security](AUTHENTICATION.md) - Security implementation
- [API Reference](API_REFERENCE.md) - Complete API documentation

---

This integration approach ensures consistency, maintainability, and scalability across all Neo Service Layer services.
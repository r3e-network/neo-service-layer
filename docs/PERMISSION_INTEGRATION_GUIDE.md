# Permission Integration Guide

This guide explains how to integrate the comprehensive permission management system into new and existing services in the Neo Service Layer.

## 🏗️ Architecture Overview

The permission system provides three levels of integration:

1. **Automatic Integration**: Services inherit from `PermissionAwareServiceBase` for built-in permission checking
2. **Attribute-Based**: Use `[RequirePermission]` and related attributes for declarative permissions  
3. **Manual Integration**: Call permission methods directly for fine-grained control

## 🚀 Quick Start for New Services

### 1. Inherit from PermissionAwareServiceBase

```csharp
[ServicePermissions("myservice", Description = "My service description")]
public class MyService : PermissionAwareServiceBase, IMyService
{
    public MyService(
        ILogger<MyService> logger,
        IEnclaveManager enclaveManager,
        IServiceProvider serviceProvider)
        : base("MyService", "Description", "1.0.0",
               logger, new[] { BlockchainType.NeoN3 }, enclaveManager, serviceProvider)
    {
        AddCapability<IMyService>();
    }
}
```

### 2. Add Permission Attributes to Methods

```csharp
[RequirePermission("myservice:data", "read")]
public async Task<MyData> GetDataAsync(string id)
{
    if (!await EnsurePermissionAsync())
    {
        throw new UnauthorizedAccessException("Insufficient permissions");
    }
    
    // Your implementation here
    return await LoadDataAsync(id);
}

[RequirePermission("myservice:data", "write")]
public async Task<bool> SaveDataAsync(MyData data)
{
    return await ExecuteWithPermissionAsync(
        () => SaveDataInternalAsync(data),
        $"myservice:data:{data.Id}",
        "write");
}

[AllowAnonymousAccess(Reason = "Public health endpoint")]
public async Task<HealthStatus> GetHealthAsync()
{
    return HealthStatus.Healthy;
}
```

### 3. Auto-Registration

The system automatically:
- Registers service permissions based on attributes
- Creates default roles (`myservice-admin`, `myservice-user`, `myservice-readonly`)
- Sets up basic access policies
- Integrates with SGX enclave storage

## 🔧 Configuration Options

### ServicePermissions Attribute

```csharp
[ServicePermissions(
    "myservice",                    // Resource prefix
    Description = "Service desc",   // Description for registration
    DefaultRole = "user",           // Default role for access
    AutoRegister = true)]           // Auto-register permissions
```

### RequirePermission Attribute

```csharp
[RequirePermission(
    "myservice:admin:*",            // Resource pattern
    "execute",                      // Required action
    Scope = "Service",              // Permission scope
    AllowServiceAccess = true,      // Allow service-to-service access
    AllowAdminOverride = true,      // Allow admin override
    DenialMessage = "Custom message")] // Custom denial message
```

## 📝 Permission Patterns

### Resource Patterns

- `myservice:*` - All service resources
- `myservice:data:*` - All data resources
- `myservice:data:user:{userId}` - User-specific data
- `myservice:admin:*` - Admin operations

### Action Types

- `read` / `get` / `list` / `query` - Read operations
- `write` / `create` / `update` / `set` - Write operations  
- `delete` / `remove` - Delete operations
- `execute` / `run` / `invoke` - Execution operations
- `*` / `full` - All operations

## 🛡️ Advanced Permission Checking

### Context-Aware Permissions

```csharp
public async Task<UserData> GetUserDataAsync(string userId)
{
    var context = new Dictionary<string, object>
    {
        ["userId"] = userId,
        ["dataType"] = "sensitive"
    };
    
    if (!await EnsurePermissionAsync(additionalContext: context))
    {
        throw new UnauthorizedAccessException();
    }
    
    // Implementation
}
```

### Custom Permission Logic

```csharp
public async Task<bool> CustomOperationAsync(string resourceId)
{
    // Check multiple permissions
    var hasRead = await CheckPermissionAsync($"myservice:data:{resourceId}", "read");
    var hasWrite = await CheckPermissionAsync($"myservice:data:{resourceId}", "write");
    
    if (!hasRead || !hasWrite)
    {
        return false;
    }
    
    // Proceed with operation
    return true;
}
```

## 🔄 Integration with Existing Services

### Step 1: Update Base Class

```csharp
// Change from:
public class ExistingService : EnclaveBlockchainServiceBase, IExistingService

// To:
[ServicePermissions("existing")]
public class ExistingService : PermissionAwareServiceBase, IExistingService
{
    // Update constructor to include IServiceProvider
    public ExistingService(
        ILogger<ExistingService> logger,
        IEnclaveManager enclaveManager,
        IServiceProvider serviceProvider) // Add this parameter
        : base("ExistingService", "Description", "1.0.0",
               logger, supportedBlockchains, enclaveManager, serviceProvider) // Add serviceProvider
    {
        // Existing initialization
    }
}
```

### Step 2: Add Permission Attributes

```csharp
[RequirePermission("existing:operation", "execute")]
public async Task<Result> ExistingMethodAsync(string param)
{
    // Add permission check at the beginning
    if (!await EnsurePermissionAsync())
    {
        throw new UnauthorizedAccessException("Insufficient permissions");
    }
    
    // Existing implementation unchanged
    return await DoOperationAsync(param);
}
```

### Step 3: Update Service Registration

The permission system automatically integrates when services inherit from `PermissionAwareServiceBase`. No additional DI registration is needed.

## 🎛️ Custom Permission Setup

### Define Custom Permissions

```csharp
var customPermissions = new List<ServicePermission>
{
    new ServicePermission
    {
        ResourcePattern = "myservice:special:*",
        AllowedAccess = new List<AccessType> { AccessType.Execute },
        CanDelegate = false
    }
};

await PermissionHelper.SetupServicePermissionsAsync(
    serviceProvider,
    "MyService",
    "myservice",
    customPermissions);
```

### Create Custom Roles

```csharp
var customRole = new Role
{
    RoleId = "myservice-poweruser",
    Name = "MyService Power User",
    Description = "Advanced access to MyService",
    Permissions = new List<Permission>
    {
        new Permission
        {
            Resource = "myservice:advanced:*",
            Action = "*",
            Scope = PermissionScope.Service
        }
    }
};

await permissionService.CreateRoleAsync(customRole);
```

### Define Access Policies

```csharp
var policy = PermissionHelper.CreateServicePolicy(
    "MyService",
    "myservice:sensitive:*",
    new List<string> { "read", "write" },
    new List<Principal>
    {
        new Principal 
        { 
            PrincipalId = "admin-role", 
            Type = PrincipalType.Role 
        }
    });

// Add time restrictions
policy.Conditions = PermissionHelper.CreateTimeRestrictions(
    businessHoursOnly: true,
    allowedDays: new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Friday });

await permissionService.CreateDataAccessPolicyAsync(policy);
```

## 🔒 Security Best Practices

### 1. Principle of Least Privilege
- Grant minimal permissions needed for functionality
- Use specific resource patterns, avoid wildcards when possible
- Implement time-based and context-based restrictions

### 2. Defense in Depth
```csharp
[RequirePermission("service:sensitive", "read")]
public async Task<SensitiveData> GetSensitiveDataAsync(string id)
{
    // Layer 1: Attribute-based permission
    if (!await EnsurePermissionAsync())
        throw new UnauthorizedAccessException();
    
    // Layer 2: Business logic validation
    if (!await ValidateBusinessRulesAsync(id))
        throw new InvalidOperationException();
    
    // Layer 3: Data-level access control
    var data = await GetDataWithAccessCheckAsync(id);
    
    return data;
}
```

### 3. Audit Everything
```csharp
// All permission checks are automatically audited
// Additional custom auditing:
Logger.LogInformation("Sensitive operation performed by {User} on {Resource}", 
    currentUserId, resourceId);
```

## 📊 Monitoring & Debugging

### Enable Debug Logging

```json
{
  "Logging": {
    "LogLevel": {
      "NeoServiceLayer.ServiceFramework.Permissions": "Debug",
      "NeoServiceLayer.Services.Permissions": "Debug"
    }
  }
}
```

### Check Permission Service Health

```csharp
// Via API
GET /api/permission/health

// Via service
var health = await permissionService.GetHealthAsync();
```

### View Audit Logs

```csharp
var auditLogs = await permissionService.GetAuditLogsAsync(new AuditLogFilter
{
    PrincipalId = "user123",
    StartDate = DateTime.UtcNow.AddDays(-7),
    OnlyDenied = true // Show only denied access attempts
});
```

## 🧪 Testing Permissions

### Unit Tests

```csharp
[Test]
public async Task GetDataAsync_WithoutPermission_ThrowsUnauthorizedException()
{
    // Arrange
    var mockPermissionService = new Mock<IPermissionService>();
    mockPermissionService
        .Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(false);

    var service = new MyService(logger, enclaveManager, CreateServiceProvider(mockPermissionService.Object));

    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(
        () => service.GetDataAsync("test-id"));
}

[Test]
public async Task GetDataAsync_WithPermission_ReturnsData()
{
    // Arrange
    var mockPermissionService = new Mock<IPermissionService>();
    mockPermissionService
        .Setup(x => x.CheckPermissionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(true);

    var service = new MyService(logger, enclaveManager, CreateServiceProvider(mockPermissionService.Object));

    // Act
    var result = await service.GetDataAsync("test-id");

    // Assert
    Assert.IsNotNull(result);
}
```

### Integration Tests

```csharp
[Test]
public async Task E2E_PermissionFlow()
{
    // Create user and assign role
    await permissionService.AssignRoleAsync("testuser", "myservice-user");
    
    // Test API call with proper authentication
    var response = await httpClient.GetAsync("/api/myservice/data/123");
    
    // Should succeed
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
}
```

## 🔧 Troubleshooting

### Common Issues

1. **Service Not Found**: Ensure service is registered in DI container
2. **Permission Denied**: Check user roles and permissions
3. **Missing Attributes**: Verify `[ServicePermissions]` and `[RequirePermission]` attributes
4. **Configuration Error**: Check `IServiceProvider` is passed to base constructor

### Debug Checklist

- [ ] Service inherits from `PermissionAwareServiceBase`
- [ ] `[ServicePermissions]` attribute is present with correct resource prefix
- [ ] `[RequirePermission]` attributes are on protected methods
- [ ] `IServiceProvider` is injected and passed to base constructor
- [ ] Permission service is registered in DI container
- [ ] User/service has required roles and permissions
- [ ] Audit logs show permission checks are happening

## 📚 Examples

See the complete `VotingService` implementation in `/src/Services/NeoServiceLayer.Services.Voting/VotingService.cs` for a full example of permission integration.

## 🤝 Contributing

When adding new services:

1. Always inherit from `PermissionAwareServiceBase`
2. Add appropriate permission attributes
3. Follow resource naming conventions: `{service}:{category}:{resource}`
4. Include unit tests for permission scenarios
5. Update this documentation with service-specific patterns
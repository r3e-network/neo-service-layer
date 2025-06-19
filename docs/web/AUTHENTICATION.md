# Authentication & Security Guide

## Overview

The Neo Service Layer Web Application implements a comprehensive security model using JWT (JSON Web Tokens) authentication with role-based authorization. This document covers the complete security architecture and implementation details.

## 🔐 Authentication Architecture

### **JWT Token-Based Authentication**

The application uses JWT tokens for stateless authentication:

```json
{
  "JwtSettings": {
    "SecretKey": "YourSecretKeyThatIsAtLeast32CharactersLong",
    "Issuer": "NeoServiceLayer",
    "Audience": "NeoServiceLayerUsers",
    "ExpirationHours": 24
  }
}
```

### **Token Structure**

JWT tokens contain the following claims:

```json
{
  "sub": "user-id",
  "name": "username",
  "role": ["Admin", "KeyManager", "ServiceUser"],
  "iss": "NeoServiceLayer",
  "aud": "NeoServiceLayerUsers",
  "exp": 1640995200,
  "iat": 1640908800
}
```

## 🔑 Authentication Flow

### **Development Environment**

For development and testing, the application provides automatic token generation:

1. **Demo Token Endpoint**: `POST /api/auth/demo-token`
2. **Automatic Generation**: JavaScript automatically requests tokens
3. **Full Permissions**: Demo tokens include all roles

```csharp
app.MapPost("/api/auth/demo-token", () =>
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(secretKey);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "demo-user"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "KeyManager"),
            new Claim(ClaimTypes.Role, "ServiceUser")
        }),
        Expires = DateTime.UtcNow.AddHours(24),
        Issuer = issuer,
        Audience = audience,
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key), 
            SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);

    return new { token = tokenString, expires = tokenDescriptor.Expires };
}).AllowAnonymous();
```

### **Production Environment**

In production, implement proper authentication:

1. **User Registration/Login**: Implement user management
2. **Identity Provider Integration**: Azure AD, Auth0, etc.
3. **Multi-factor Authentication**: Enhanced security
4. **Token Refresh**: Automatic token renewal

## 🛡️ Authorization Model

### **Role Definitions**

#### **Admin Role**
- **Permissions**: Full access to all services and operations
- **Use Cases**: System administration, configuration management
- **Endpoints**: All API endpoints

#### **KeyManager Role**
- **Permissions**: Key management operations, cryptographic services
- **Use Cases**: Cryptographic operations, key lifecycle management
- **Endpoints**: Key Management, Randomness, Zero Knowledge services

#### **ServiceUser Role**
- **Permissions**: General service access, data operations
- **Use Cases**: Application integration, service consumption
- **Endpoints**: Most services except administrative operations

#### **Guest Role** (Future)
- **Permissions**: Read-only access to public endpoints
- **Use Cases**: Public data access, health checks
- **Endpoints**: Health checks, public information

### **Authorization Implementation**

Controllers use attribute-based authorization:

```csharp
[Authorize(Roles = "Admin,KeyManager")]
public async Task<IActionResult> GenerateKey([FromBody] KeyRequest request)
{
    // Implementation
}

[Authorize(Roles = "Admin,ServiceUser")]
public async Task<IActionResult> GetData([FromQuery] string dataId)
{
    // Implementation
}

[Authorize(Roles = "Admin")]
public async Task<IActionResult> DeleteService([FromRoute] string serviceId)
{
    // Implementation
}
```

### **Authorization Policies**

Additional policies provide fine-grained control:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("KeyManagerOrAdmin", policy => 
        policy.RequireRole("Admin", "KeyManager"));
    
    options.AddPolicy("ServiceUser", policy => 
        policy.RequireRole("Admin", "KeyManager", "KeyUser", "ServiceUser"));
});
```

## 🔒 Security Configuration

### **JWT Configuration**

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });
```

### **CORS Configuration**

Secure cross-origin requests:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000", 
                "https://localhost:3001",
                "http://localhost:5000", 
                "https://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

### **HTTPS Configuration**

Production deployments should enforce HTTPS:

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

## 🌐 Client-Side Authentication

### **Token Management**

JavaScript handles token management automatically:

```javascript
let authToken = null;

async function getAuthToken() {
    try {
        const response = await fetch('/api/auth/demo-token', { method: 'POST' });
        const data = await response.json();
        authToken = data.token;
        
        // Store token for persistence (development only)
        localStorage.setItem('neo-service-token', authToken);
    } catch (error) {
        console.error('Error getting auth token:', error);
        throw error;
    }
}

// Use token in service calls
async function callService(endpoint, options = {}) {
    if (!authToken) await getAuthToken();
    
    const headers = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${authToken}`,
        ...options.headers
    };
    
    return fetch(endpoint, { ...options, headers });
}
```

### **Automatic Token Refresh**

Handle token expiration gracefully:

```javascript
async function makeAuthenticatedRequest(url, options) {
    let response = await fetch(url, {
        ...options,
        headers: {
            'Authorization': `Bearer ${authToken}`,
            ...options.headers
        }
    });
    
    // Handle token expiration
    if (response.status === 401) {
        await getAuthToken();
        response = await fetch(url, {
            ...options,
            headers: {
                'Authorization': `Bearer ${authToken}`,
                ...options.headers
            }
        });
    }
    
    return response;
}
```

## 🛡️ Security Best Practices

### **Environment-Specific Security**

#### **Development Environment**
- Demo token generation for testing
- Relaxed CORS policies
- Detailed error messages
- Swagger UI enabled

#### **Production Environment**
- Real authentication required
- Strict CORS policies
- Generic error messages
- Swagger UI disabled
- HTTPS enforcement
- Rate limiting enabled

### **Secret Management**

#### **Development**
```json
{
  "JwtSettings": {
    "SecretKey": "YourDevelopmentSecretKey"
  }
}
```

#### **Production**
Use environment variables or secure secret management:

```bash
export JWT_SECRET_KEY="your-production-secret-key"
export JWT_ISSUER="NeoServiceLayer"
export JWT_AUDIENCE="NeoServiceLayerUsers"
```

```csharp
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
    ?? builder.Configuration["JwtSettings:SecretKey"];

if (builder.Environment.IsProduction() && 
    (string.IsNullOrEmpty(secretKey) || secretKey.Contains("YourSuperSecretKey")))
{
    throw new InvalidOperationException(
        "JWT secret key must be configured via environment variables in production");
}
```

### **Security Headers**

Add security headers for production:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    
    await next();
});
```

### **Input Validation**

All controllers validate input data:

```csharp
[HttpPost("operation")]
public async Task<IActionResult> Operation([FromBody] OperationRequest request)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }
    
    // Additional validation
    if (string.IsNullOrWhiteSpace(request.RequiredField))
    {
        return BadRequest("RequiredField is mandatory");
    }
    
    // Process request
}
```

## 🔍 Security Monitoring

### **Authentication Logging**

Log authentication events:

```csharp
public class AuthenticationLoggingMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<AuthenticationLoggingMiddleware>>();
        
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            logger.LogInformation("Authenticated request to {Path} by {User}",
                context.Request.Path, context.User.Identity?.Name ?? "Unknown");
        }
        
        await next(context);
    }
}
```

### **Failed Authentication Tracking**

Monitor failed authentication attempts:

```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                    
                logger.LogWarning("Authentication failed for {Path}: {Error}",
                    context.Request.Path, context.Exception.Message);
                
                return Task.CompletedTask;
            }
        };
    });
```

## 🚨 Security Incident Response

### **Common Security Issues**

1. **Invalid Token**: Return 401 Unauthorized
2. **Insufficient Permissions**: Return 403 Forbidden
3. **Expired Token**: Return 401 with refresh instruction
4. **Malformed Request**: Return 400 Bad Request

### **Error Handling**

Secure error responses:

```csharp
protected IActionResult HandleException(Exception ex, string operation)
{
    Logger.LogError(ex, "Error in {Operation}: {Message}", operation, ex.Message);
    
    return ex switch
    {
        UnauthorizedAccessException => Unauthorized("Access denied"),
        ArgumentException => BadRequest("Invalid request"),
        NotFoundException => NotFound("Resource not found"),
        _ => StatusCode(500, "An error occurred processing your request")
    };
}
```

## 📋 Security Checklist

### **Deployment Security**

- [ ] JWT secret key configured via environment variables
- [ ] HTTPS enabled with valid certificates
- [ ] CORS policies restricted to known origins
- [ ] Rate limiting implemented
- [ ] Security headers configured
- [ ] Error messages sanitized
- [ ] Logging configured for security events
- [ ] Authentication and authorization tested
- [ ] Input validation implemented
- [ ] SQL injection protection verified

### **Monitoring & Alerting**

- [ ] Failed authentication attempts logged
- [ ] Unusual access patterns monitored
- [ ] Token expiration tracking
- [ ] Security incident response plan
- [ ] Regular security audits scheduled

## 🔗 Related Documentation

- [Web Application Guide](WEB_APPLICATION_GUIDE.md) - Main web app documentation
- [Service Integration](SERVICE_INTEGRATION.md) - Service integration patterns
- [API Reference](API_REFERENCE.md) - Complete API documentation

---

Security is paramount in the Neo Service Layer ecosystem. This authentication system provides a solid foundation while remaining flexible for different deployment scenarios.
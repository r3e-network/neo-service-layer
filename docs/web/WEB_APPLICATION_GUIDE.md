# Neo Service Layer Web Application Guide

## Overview

The Neo Service Layer Web Application provides a comprehensive, interactive interface for accessing and testing all services in the Neo Service Layer ecosystem. Built with ASP.NET Core and Razor Pages, it offers real-time service interaction, professional UI design, and complete functionality coverage.

## < Features

### **Interactive Service Demonstrations**
- **20+ Service Cards**: Each service has a dedicated UI card with specific operations
- **Real-time Testing**: All demonstrations call actual service endpoints
- **Live Responses**: View real JSON responses from service layer
- **Error Handling**: Comprehensive error display and user notifications

### **Professional User Interface**
- **Modern Design**: Bootstrap 5-based responsive design
- **Service-specific Icons**: FontAwesome icons for visual service identification
- **Color-coded Categories**: Different color schemes for service types
- **Mobile-responsive**: Works seamlessly on all device sizes

### **Security & Authentication**
- **JWT Authentication**: Secure API access with role-based permissions
- **Demo Tokens**: Automatic token generation for testing purposes
- **Protected Endpoints**: All service calls require authentication
- **Role-based Access**: Admin, KeyManager, ServiceUser roles supported

## <� Architecture

### **Application Structure**
```
src/Web/NeoServiceLayer.Web/
   Controllers/                 # 20+ API Controllers for all services
   Pages/                       # Razor Pages for web interface
   Models/                      # Request/response models
   wwwroot/                     # Static assets
   Program.cs                   # Application configuration
```

### **Service Registration**
All services are registered in `Program.cs` using dependency injection:

```csharp
// Register all Neo Service Layer services
builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
builder.Services.AddScoped<IRandomnessService, RandomnessService>();
builder.Services.AddScoped<IOracleService, OracleService>();
// ... all other services (20+ total)
```

## =� Getting Started

### **Prerequisites**
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code
- Git for source control

### **Running the Application**

1. **Clone and Build:**
   ```bash
   git clone https://github.com/neo-project/neo-service-layer.git
   cd neo-service-layer
   dotnet build src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj
   ```

2. **Start the Application:**
   ```bash
   dotnet run --project src/Web/NeoServiceLayer.Web
   ```

3. **Access the Web Interface:**
   - Main Interface: `http://localhost:5000`
   - Service Demo: `http://localhost:5000/servicepages/servicedemo`
   - API Documentation: `http://localhost:5000/swagger`

## <� Service Demonstrations

### **Available Services (20+)**

The Service Demo page provides interactive demonstrations for:

#### **Core Services (4)**
- **Key Management**: Generate/list cryptographic keys
- **Randomness**: Generate secure random numbers and bytes
- **Oracle**: Create/manage data feeds
- **Voting**: Create proposals and cast votes

#### **Storage & Data (3)**
- **Storage**: Store and retrieve encrypted data
- **Backup**: Create and manage backups
- **Configuration**: System settings management

#### **Security Services (4)**
- **Zero Knowledge**: Generate/verify ZK proofs
- **Abstract Account**: Smart contract account management
- **Compliance**: Regulatory compliance checks
- **Proof of Reserve**: Asset reserve verification

#### **Operations (4)**
- **Automation**: Workflow automation
- **Monitoring**: System metrics and alerts
- **Health**: System health diagnostics
- **Notification**: Multi-channel notifications

#### **Infrastructure (3)**
- **Cross-Chain**: Multi-blockchain operations
- **Compute**: Secure TEE computations
- **Event Subscription**: Blockchain event monitoring

#### **AI Services (2)**
- **Pattern Recognition**: AI-powered analysis
- **Prediction**: Machine learning forecasting

### **How Service Demonstrations Work**

1. **Authentication**: Automatic JWT token generation for demo access
2. **Service Calls**: JavaScript functions call real API endpoints
3. **Response Display**: JSON responses shown in formatted code blocks
4. **Error Handling**: Comprehensive error display with user notifications
5. **Real-time Updates**: Live service status indicators

### **Example Service Interaction**

```javascript
// Key Management Demo
async function demoKeyGeneration() {
    const response = await fetch('/api/keymanagement/generate/NeoN3', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${authToken}`
        },
        body: JSON.stringify({
            keyId: `demo_key_${Date.now()}`,
            keyType: 'ECDSA',
            keyUsage: 'Signing',
            exportable: false
        })
    });
    
    const result = await response.json();
    displayResult('keymanagement', result);
}
```

## = Security Features

### **Authentication & Authorization**
- **JWT Tokens**: Secure API access with configurable expiration
- **Role-based Access**: Admin, KeyManager, ServiceUser roles
- **Demo Tokens**: Automatic generation for testing (development only)

### **API Security**
- **HTTPS Enforcement**: TLS encryption for all communications
- **CORS Protection**: Configured allowed origins
- **Input Validation**: Comprehensive request validation
- **Error Handling**: Secure error responses without sensitive data

## =� Monitoring & Health

### **Service Status Monitoring**
The web application provides real-time monitoring:

- **Service Count**: Total number of registered services (20+)
- **Active Services**: Currently operational services
- **API Calls**: Daily API call statistics
- **System Uptime**: Service availability metrics

### **Health Checks**
- `/health` - Basic application health
- `/api/health/check` - Detailed health diagnostics
- Service-specific health endpoints

## <� User Interface

### **Design Principles**
- **Modern & Professional**: Bootstrap 5-based responsive design
- **Service-specific Branding**: Color-coded service categories
- **Intuitive Navigation**: Clear layout and logical flow
- **Accessibility**: WCAG-compliant design patterns

### **Component Structure**
- **Service Cards**: Interactive service demonstration cards
- **Response Display**: Formatted JSON response viewers
- **Status Indicators**: Real-time service health dots
- **Notification System**: Toast notifications for user feedback

## =� Deployment

### **Development**
```bash
dotnet run --project src/Web/NeoServiceLayer.Web
```

### **Production**
```bash
dotnet publish -c Release
```

### **Docker**
```bash
docker build -t neo-service-layer-web .
docker run -p 5000:5000 neo-service-layer-web
```

## =' Development

### **Adding New Services**

1. **Create Controller**: Implement API controller for the service
2. **Update Program.cs**: Register service in dependency injection
3. **Add UI Card**: Create service demonstration card in ServiceDemo.cshtml
4. **Implement JavaScript**: Add service interaction functions
5. **Update Documentation**: Document the new service

### **Project Structure**
- Controllers follow RESTful patterns
- All services use BaseApiController for consistency
- JWT authentication required for all service endpoints
- Comprehensive error handling and logging

## =� Related Documentation

- [Service Integration Guide](SERVICE_INTEGRATION.md) - How services integrate with the web app
- [Authentication & Security](AUTHENTICATION.md) - Security implementation details
- [API Reference](API_REFERENCE.md) - Complete API documentation

## <� Troubleshooting

### **Common Issues**
1. **Port Conflicts**: Application runs on port 5000 by default
2. **Authentication Errors**: Ensure JWT configuration is correct
3. **Service Registration**: Verify all services are properly registered
4. **CORS Issues**: Check allowed origins configuration

### **Debug Mode**
```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/Web/NeoServiceLayer.Web
```

## =� Support

For support and questions:
- **Documentation**: Check the `/docs` directory
- **Issues**: Create GitHub issues for bugs
- **Discussions**: Use GitHub Discussions for questions

---

Built with d for the Neo blockchain ecosystem.
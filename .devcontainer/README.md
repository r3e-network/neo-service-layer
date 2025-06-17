# Neo Service Layer Development Container

This development container provides a complete Ubuntu 24.04 environment with all necessary tools for Neo Service Layer development, including SGX SDK, Occlum LibOS, .NET 9.0, and Rust.

## 🚀 Quick Start

1. **Open in DevContainer**:
   - In VS Code, press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
   - Type "Dev Containers: Reopen in Container"
   - Wait for the container to build and initialize (this may take 10-15 minutes on first run)

2. **Start the Web Application**:
   ```bash
   ./start-dev.sh
   ```

3. **Access the Application**:
   - Main Application: http://localhost:5000
   - API Documentation: http://localhost:5000/swagger
   - Health Check: http://localhost:5000/health
   - JWT Token: POST http://localhost:5000/api/auth/demo-token

## 🛠️ What's Included

### Core Development Tools
- **Ubuntu 24.04 LTS** - Latest stable Linux environment
- **.NET 9.0 SDK** - Complete .NET development stack
- **Rust 1.75+** - With cargo, rustfmt, clippy
- **Node.js & npm** - For web asset management
- **Git, GitHub CLI** - Version control and GitHub integration

### Blockchain & Security
- **Intel SGX SDK 2.23** - Trusted execution environment
- **Occlum LibOS 0.29.6** - LibOS for confidential computing
- **Fortanix EDP Tools** - Additional SGX utilities
- **Protocol Buffers** - Cross-platform serialization

### Development Features
- **VS Code Extensions** - C#, Rust, Docker, and more pre-installed
- **Debugging Support** - Full .NET and Rust debugging capabilities
- **IntelliSense** - Complete code completion and analysis
- **Integrated Terminal** - Zsh with Oh My Zsh configuration

## 🏗️ Project Structure

The devcontainer automatically sets up:

```
/workspace/
├── .devcontainer/          # Container configuration
├── .vscode/               # VS Code settings and tasks
├── src/                   # Source code
│   ├── Web/              # Web application (simplified for startup)
│   ├── Services/         # Microservices (gradually enable)
│   ├── Tee/             # SGX enclave code
│   └── ...
├── start-dev.sh          # Quick start script
└── test-all.sh           # Test runner script
```

## 🔧 Development Workflow

### 1. Initial Development (Current State)
The devcontainer starts with a minimal web application to avoid dependency conflicts:
- Basic web interface with JWT authentication
- Health monitoring and API documentation
- Simplified service registration

### 2. Gradual Service Integration
To add services back:

1. **Uncomment service registrations** in `src/Web/NeoServiceLayer.Web/Program.cs`
2. **Restore controller files** by renaming `.disabled` files back to `.cs`
3. **Fix any dependency injection issues** that arise
4. **Test incrementally** to ensure stability

### 3. SGX Development
```bash
# Source SGX environment
source /opt/intel/sgxsdk/environment

# Build SGX enclave
cd src/Tee/NeoServiceLayer.Tee.Enclave
cargo build

# Run in simulation mode
export SGX_MODE=SIM
```

### 4. Testing
```bash
# Run all tests
./test-all.sh

# Run specific tests
dotnet test src/Services/NeoServiceLayer.Services.KeyManagement.Tests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🐛 Troubleshooting

### Common Issues and Solutions

1. **Build Errors on Startup**
   - The post-create script automatically handles most dependency issues
   - If errors persist, check `/workspace/.devcontainer/post-create.sh` logs

2. **SGX-related Errors**
   - Ensure `SGX_MODE=SIM` is set for simulation mode
   - Source the SGX environment: `source /opt/intel/sgxsdk/environment`

3. **Port Already in Use**
   - Check running processes: `lsof -i :5000`
   - Kill existing processes: `pkill -f dotnet`

4. **Dependency Injection Errors**
   - The container starts with minimal services to avoid conflicts
   - Gradually enable services one by one in `Program.cs`

### Service Restoration Process

To restore full functionality:

1. **Enable Core Services First**:
   ```csharp
   // In Program.cs, uncomment these one by one:
   builder.Services.AddScoped<IKeyManagementService, KeyManagementService>();
   builder.Services.AddScoped<IStorageService, StorageService>();
   ```

2. **Test After Each Service**:
   ```bash
   dotnet build
   ./start-dev.sh
   ```

3. **Fix Interface Mismatches**:
   - Check controller method signatures
   - Verify service interface implementations
   - Update dependency injection registrations

## 📦 Package Management

### .NET Packages
```bash
# Add package to specific project
dotnet add src/Web/NeoServiceLayer.Web/ package PackageName

# Restore all packages
dotnet restore

# Update packages
dotnet list package --outdated
```

### Rust Crates
```bash
# Add crate to Cargo.toml
cargo add serde

# Update dependencies
cargo update

# Audit for security issues
cargo audit
```

## 🔐 Security Features

### SGX Simulation Mode
- All SGX operations run in simulation mode by default
- No hardware SGX requirements for development
- Full API compatibility with hardware mode

### JWT Authentication
- Development tokens with 24-hour expiration
- Admin role assigned to demo tokens
- Configurable through `appsettings.Development.json`

### Secure Development Practices
- All dependencies scanned for vulnerabilities
- Code analysis tools integrated
- Security-focused extensions pre-installed

## 🚀 Deployment

### Container Build
```bash
# Build production container
docker build -f .devcontainer/Dockerfile -t neo-service-layer:dev .

# Run container
docker run -p 5000:5000 neo-service-layer:dev
```

### Local Testing
```bash
# Run with production settings
export ASPNETCORE_ENVIRONMENT=Production
dotnet run --project src/Web/NeoServiceLayer.Web/
```

## 📝 Contributing

1. **Make changes** in the devcontainer environment
2. **Test thoroughly** using `./test-all.sh`
3. **Ensure builds pass** with `dotnet build`
4. **Document any new dependencies** in this README
5. **Submit pull request** with detailed description

## 🆘 Support

### Getting Help
- **Documentation**: Check `/docs` directory for detailed guides
- **Issues**: Review existing GitHub issues
- **Logs**: Check application logs in `logs/` directory

### Debugging
- **VS Code Debugging**: Use F5 to start debugging session
- **Console Logs**: Check terminal output during startup
- **SGX Debugging**: Use `sgx-gdb` for enclave debugging

### Performance
- **Monitoring**: Access `/health` endpoint for status
- **Metrics**: Review application metrics in logs
- **Profiling**: Use dotnet diagnostic tools

---

## 🎯 Next Steps

Once the devcontainer is running:

1. ✅ **Basic Web App** - Already working
2. 🔄 **Enable Services** - Gradually restore functionality  
3. 🔧 **Fix DI Issues** - Resolve dependency injection conflicts
4. 🧪 **Add Tests** - Expand test coverage
5. 🔐 **SGX Integration** - Enable full enclave functionality
6. 🚀 **Production Deploy** - Create production containers

Welcome to Neo Service Layer development! 🎉 
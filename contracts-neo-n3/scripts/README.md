# Neo Service Layer Scripts

This directory contains all the necessary scripts for building, testing, and deploying the Neo Service Layer smart contracts.

## 📋 **Script Overview**

### **🔧 Setup & Environment**
- **`setup.sh`** - Install prerequisites and setup development environment
- **`make-executable.sh`** - Make all scripts executable

### **🏗️ Build & Compilation**
- **`build.sh`** - Complete build pipeline for all 22 contracts
- **`compile.sh`** - Compile smart contracts using Neo C# Compiler
- **`clean.sh`** - Clean build artifacts and temporary files

### **🧪 Testing & Verification**
- **`test.sh`** - Comprehensive testing suite for all contracts
- **`verify.sh`** - Verify compilation, deployment, and functionality

### **🚀 Deployment**
- **`deploy.sh`** - Deploy all contracts to Neo blockchain

## 🚀 **Quick Start**

### **Initial Setup**
```bash
# Make scripts executable
chmod +x scripts/*.sh

# Or use the helper script
./scripts/make-executable.sh

# Setup development environment
./scripts/setup.sh
```

### **Build All Contracts**
```bash
# Complete build pipeline
./scripts/build.sh

# Or step by step
./scripts/clean.sh          # Clean previous builds
./scripts/compile.sh        # Compile contracts
./scripts/test.sh           # Run tests
./scripts/verify.sh         # Verify build
```

### **Deploy to Blockchain**
```bash
# Set environment variables
export WALLET_PATH="./wallet.json"
export NETWORK="testnet"

# Deploy all contracts
./scripts/deploy.sh deploy
```

## 📚 **Detailed Script Documentation**

### **setup.sh**
Installs all prerequisites and sets up the development environment.

**Usage:**
```bash
./scripts/setup.sh              # Full setup
./scripts/setup.sh dotnet       # Install .NET only
./scripts/setup.sh compiler     # Install Neo compiler only
./scripts/setup.sh project      # Setup project only
```

**Features:**
- Detects operating system (Linux, macOS, Windows)
- Installs .NET 8.0 SDK
- Installs Neo C# Compiler (nccs)
- Restores NuGet packages
- Creates necessary directories

### **build.sh**
Complete build pipeline for all 22 smart contracts.

**Usage:**
```bash
./scripts/build.sh              # Full build pipeline
./scripts/build.sh clean        # Clean only
./scripts/build.sh restore      # Restore packages only
./scripts/build.sh compile      # Compile only
./scripts/build.sh test         # Build and test
./scripts/build.sh fast         # Skip tests and verification
```

**Features:**
- Clean previous builds
- Restore NuGet dependencies
- Build .NET project
- Compile smart contracts
- Run tests (optional)
- Verify build (optional)
- Generate build report
- Package artifacts

**Environment Variables:**
- `BUILD_CONFIGURATION` - Build configuration (default: Release)
- `SKIP_TESTS` - Skip testing (default: false)
- `SKIP_VERIFICATION` - Skip verification (default: false)

### **compile.sh**
Compiles smart contracts using the official Neo C# Compiler.

**Usage:**
```bash
./scripts/compile.sh            # Compile all contracts
./scripts/compile.sh clean      # Clean build artifacts
./scripts/compile.sh restore    # Restore packages
./scripts/compile.sh verify     # Verify compiled contracts
```

**Features:**
- Uses official Neo C# Compiler v3.6.3
- Compiles all 22 contracts individually
- Generates NEF and manifest files
- Supports multiple compilation methods
- Comprehensive error handling
- Generates compilation report

**Output:**
- NEF files: `./bin/contracts/*.nef`
- Manifest files: `./manifests/*.manifest.json`
- Compilation report: `compilation-report-*.json`

### **test.sh**
Comprehensive testing suite for all smart contracts.

**Usage:**
```bash
./scripts/test.sh               # Run all tests
./scripts/test.sh unit          # Unit tests only
./scripts/test.sh integration   # Integration tests only
./scripts/test.sh performance   # Performance tests only
./scripts/test.sh security      # Security tests only
./scripts/test.sh financial     # Financial contracts only
./scripts/test.sh industry      # Industry contracts only
./scripts/test.sh coverage      # Generate coverage report
```

**Features:**
- Unit, integration, performance, and security tests
- Contract-specific test suites
- Code coverage reporting
- Test result aggregation
- Comprehensive test reporting

**Output:**
- Test results: `./test-results/*.trx`
- Coverage report: `./coverage/index.html`
- Test report: `test-report-*.json`

### **verify.sh**
Verifies contract compilation, deployment, and functionality.

**Usage:**
```bash
./scripts/verify.sh             # Full verification
./scripts/verify.sh compilation # Verify compilation only
./scripts/verify.sh structure   # Verify project structure
./scripts/verify.sh dependencies # Verify dependencies
./scripts/verify.sh contracts   # Verify all contracts
./scripts/verify.sh sizes       # Verify contract sizes
```

**Features:**
- Compilation verification
- Project structure validation
- Dependency checking
- Individual contract verification
- Contract size analysis
- Verification reporting

**Output:**
- Verification report: `verification-report-*.json`

### **deploy.sh**
Deploys all contracts to the Neo blockchain.

**Usage:**
```bash
./scripts/deploy.sh build       # Build contracts only
./scripts/deploy.sh deploy      # Build and deploy
./scripts/deploy.sh verify      # Verify deployment
```

**Features:**
- Builds contracts before deployment
- Deploys all 22 contracts in correct order
- Registers services with ServiceRegistry
- Generates deployment report
- Supports multiple networks

**Environment Variables:**
- `WALLET_PATH` - Path to wallet file
- `WALLET_PASSWORD` - Wallet password
- `NETWORK` - Target network (testnet/mainnet)
- `GAS_LIMIT` - Gas limit for deployments

**Output:**
- Deployment report: `deployment-report-*.json`

### **clean.sh**
Cleans build artifacts and temporary files.

**Usage:**
```bash
./scripts/clean.sh              # Standard clean
./scripts/clean.sh build        # Clean build artifacts only
./scripts/clean.sh temp         # Clean temporary files only
./scripts/clean.sh test         # Clean test outputs only
./scripts/clean.sh logs         # Clean log files only
./scripts/clean.sh all          # Deep clean (including IDE files)
./scripts/clean.sh reset        # Reset to clean state
```

**Features:**
- Removes build artifacts
- Cleans temporary files
- Removes test outputs
- Cleans log files
- Removes generated reports
- Clears NuGet cache
- Docker cleanup (if available)
- IDE file cleanup

## 🔧 **Environment Variables**

### **Global Configuration**
```bash
# .NET and Compiler
export DOTNET_VERSION="8.0.0"
export NEO_COMPILER_VERSION="3.6.3"

# Build Configuration
export BUILD_CONFIGURATION="Release"
export SKIP_TESTS="false"
export SKIP_VERIFICATION="false"

# Deployment Configuration
export WALLET_PATH="./wallet.json"
export WALLET_PASSWORD="your_password"
export NETWORK="testnet"
export GAS_LIMIT="20000000"
export NEO_CLI_PATH="neo-cli"
```

## 📊 **Output Structure**

```
contracts-neo-n3/
├── bin/contracts/              # Compiled NEF files
│   ├── ServiceRegistry.nef
│   ├── PaymentProcessingContract.nef
│   └── ... (20 more contracts)
├── manifests/                  # Contract manifests
│   ├── ServiceRegistry.manifest.json
│   └── ... (21 more manifests)
├── test-results/              # Test outputs
│   ├── unit-tests.trx
│   ├── integration-tests.trx
│   └── coverage.cobertura.xml
├── coverage/                  # Coverage reports
│   └── index.html
├── logs/                      # Log files
└── *.json                     # Generated reports
```

## 🚀 **Complete Workflow Example**

```bash
# 1. Initial setup
./scripts/setup.sh

# 2. Build all contracts
./scripts/build.sh

# 3. Run comprehensive tests
./scripts/test.sh

# 4. Verify everything
./scripts/verify.sh

# 5. Deploy to testnet
export NETWORK="testnet"
export WALLET_PATH="./testnet-wallet.json"
./scripts/deploy.sh deploy

# 6. Verify deployment
./scripts/deploy.sh verify
```

## 🔍 **Troubleshooting**

### **Common Issues**

1. **Permission Denied**
   ```bash
   chmod +x scripts/*.sh
   # or
   ./scripts/make-executable.sh
   ```

2. **Missing Dependencies**
   ```bash
   ./scripts/setup.sh
   ```

3. **Build Failures**
   ```bash
   ./scripts/clean.sh
   ./scripts/build.sh
   ```

4. **Test Failures**
   ```bash
   ./scripts/test.sh unit
   # Check test-results/ for details
   ```

### **Getting Help**

Each script supports `--help` or can be run without arguments to see usage information:

```bash
./scripts/build.sh --help
./scripts/test.sh
./scripts/deploy.sh
```

## 📈 **Performance Tips**

1. **Fast Builds**
   ```bash
   SKIP_TESTS=true ./scripts/build.sh
   ```

2. **Parallel Testing**
   ```bash
   ./scripts/test.sh unit &
   ./scripts/test.sh integration &
   wait
   ```

3. **Incremental Builds**
   ```bash
   ./scripts/compile.sh  # Only compile changed contracts
   ```

## 🏆 **Script Features**

- ✅ **Cross-Platform**: Works on Linux, macOS, and Windows
- ✅ **Error Handling**: Comprehensive error detection and reporting
- ✅ **Logging**: Colored output with detailed progress information
- ✅ **Modular**: Each script can be run independently
- ✅ **Configurable**: Environment variable support
- ✅ **Reporting**: Detailed JSON reports for all operations
- ✅ **Verification**: Built-in verification and validation
- ✅ **Documentation**: Comprehensive help and usage information

---

**The Neo Service Layer scripts provide a complete, production-ready build and deployment system for the world's most comprehensive blockchain enterprise platform.**
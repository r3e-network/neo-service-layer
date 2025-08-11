# Neo Service Layer - Makefile Guide

## Overview

The Neo Service Layer Makefile has been significantly enhanced to provide a comprehensive build system for the entire project. This guide covers all available targets and common usage patterns.

## Quick Start

```bash
# Display help
make help

# Quick build and test
make quick

# Full CI pipeline
make ci

# Start development environment
make dev
```

## Target Categories

### 🏗️ Build Targets

| Target | Description | Example |
|--------|-------------|---------|
| `all` | Clean, restore, build, and test everything | `make all` |
| `build` | Build the solution | `make build` |
| `build-minimal` | Fast build without restore | `make build-minimal` |
| `build-debug` | Build in Debug configuration | `make build-debug` |
| `build-release` | Build in Release configuration | `make build-release` |
| `build-contracts` | Build Neo smart contracts | `make build-contracts` |
| `publish` | Publish the application | `make publish` |
| `publish-linux` | Publish for Linux x64 | `make publish-linux` |
| `publish-windows` | Publish for Windows x64 | `make publish-windows` |
| `publish-macos` | Publish for macOS x64 | `make publish-macos` |

### 🧪 Testing Targets

| Target | Description | Example |
|--------|-------------|---------|
| `test` | Run all tests | `make test` |
| `test-all` | Run all tests including performance | `make test-all` |
| `test-minimal` | Run minimal test suite (fast) | `make test-minimal` |
| `test-unit` | Run unit tests only | `make test-unit` |
| `test-integration` | Run integration tests | `make test-integration` |
| `test-e2e` | Run end-to-end tests | `make test-e2e` |
| `test-performance` | Run performance tests | `make test-performance` |
| `test-contracts` | Run smart contract tests | `make test-contracts` |
| `test-coverage` | Run tests with coverage report | `make test-coverage` |
| `test-service` | Test specific service | `make test-service SERVICE=Notification` |
| `test-watch` | Run tests in watch mode | `make test-watch` |
| `test-failed` | Re-run only failed tests | `make test-failed` |

### 📊 Benchmarking Targets

| Target | Description | Example |
|--------|-------------|---------|
| `bench` | Run all benchmarks | `make bench` |
| `bench-quick` | Run quick benchmarks | `make bench-quick` |
| `bench-specific` | Run specific benchmark | `make bench-specific BENCH_FILTER=EnclaveBenchmarks` |
| `bench-memory` | Run memory-focused benchmarks | `make bench-memory` |
| `bench-compare` | Compare benchmark results | `make bench-compare BASELINE=old.json CURRENT=new.json` |
| `load-test` | Run load tests using NBomber | `make load-test` |
| `stress-test` | Run stress tests | `make stress-test` |
| `perf-profile` | Profile application performance | `make perf-profile` |

### ⚙️ Configuration Management

| Target | Description | Example |
|--------|-------------|---------|
| `config-list` | List all configuration files | `make config-list` |
| `config-validate` | Validate configuration files | `make config-validate` |
| `config-env` | Generate environment template | `make config-env` |
| `config-check-env` | Check environment variables | `make config-check-env` |
| `config-setup` | Interactive configuration setup | `make config-setup` |
| `config-diff` | Show config differences | `make config-diff` |
| `config-secrets` | Manage user secrets | `make config-secrets` |
| `config-export` | Export configuration | `make config-export` |

### 🐳 Docker Operations

| Target | Description | Example |
|--------|-------------|---------|
| `docker-build` | Build Docker image | `make docker-build` |
| `docker-run` | Build and run Docker container | `make docker-run` |
| `docker-compose-up` | Start services with docker-compose | `make docker-compose-up` |
| `docker-compose-down` | Stop services | `make docker-compose-down` |
| `docker-logs` | Show Docker logs | `make docker-logs` |

### 🧹 Maintenance

| Target | Description | Example |
|--------|-------------|---------|
| `clean` | Clean build artifacts | `make clean` |
| `clean-all` | Deep clean including caches | `make clean-all` |
| `clean-minimal` | Minimal clean (bin/obj only) | `make clean-minimal` |
| `clean-logs` | Clean log files | `make clean-logs` |
| `update-packages` | Update NuGet packages | `make update-packages` |
| `deps-check` | Check for vulnerabilities | `make deps-check` |

### 🛠️ Development Tools

| Target | Description | Example |
|--------|-------------|---------|
| `dev` | Start development environment | `make dev` |
| `dev-api` | Start API development server | `make dev-api` |
| `tools-install` | Install required dev tools | `make tools-install` |
| `check-tools` | Check tools installation | `make check-tools` |
| `analyze` | Run code analysis | `make analyze` |
| `format` | Format code | `make format` |
| `format-check` | Check code formatting | `make format-check` |

### 🚀 Quick Commands

| Target | Description | Example |
|--------|-------------|---------|
| `quick` | Quick build and test | `make quick` |
| `quick-test` | Quick smoke test | `make quick-test` |
| `run` | Run web application | `make run` |
| `run-api` | Run API service | `make run-api` |

### 📋 Utility Commands

| Target | Description | Example |
|--------|-------------|---------|
| `version` | Show version information | `make version` |
| `status` | Show project status | `make status` |
| `project-stats` | Show project statistics | `make project-stats` |
| `scaffold-service` | Create new service structure | `make scaffold-service SERVICE_NAME=MyNewService` |
| `backup` | Create project backup | `make backup` |
| `docs` | Open documentation | `make docs` |
| `changelog` | Show recent changes | `make changelog` |
| `todo` | Show TODO items in code | `make todo` |

## Configuration Variables

The Makefile supports several configuration variables:

```bash
# Build configuration (Debug/Release)
make build CONFIGURATION=Debug

# Output verbosity (quiet/minimal/normal/detailed)
make test VERBOSITY=detailed

# Parallel builds (true/false)
make build PARALLEL=false

# Target runtime
make publish RUNTIME=linux-x64

# Target framework
make build TARGET_FRAMEWORK=net9.0
```

## Common Workflows

### Development Workflow
```bash
# Start development with hot reload
make dev

# Run quick smoke tests
make quick-test

# Format code
make format

# Check for issues
make lint
```

### Testing Workflow
```bash
# Run all tests with coverage
make test-coverage

# Run only unit tests
make test-unit

# Run specific test category
make test-integration

# Watch mode for TDD
make test-watch
```

### Benchmarking Workflow
```bash
# Run all benchmarks
make bench

# Run specific benchmark
make bench-specific BENCH_FILTER=EnclaveBenchmarks

# View results
ls BenchmarkResults/
```

### CI/CD Workflow
```bash
# Run full CI pipeline locally
make ci

# Create release package
make package

# Generate release notes
make release-notes
```

### Docker Workflow
```bash
# Build and run in Docker
make docker-run

# Use docker-compose
make docker-compose-up

# View logs
make docker-logs

# Stop services
make docker-compose-down
```

## Advanced Usage

### Cross-Platform Builds
```bash
# Build for specific runtime
make publish RUNTIME=linux-x64

# Self-contained deployment
make publish RUNTIME=win-x64
```

### Performance Profiling
```bash
# Run performance tests
make test-performance

# Run benchmarks with detailed output
make bench VERBOSITY=detailed
```

### Dependency Management
```bash
# Check for outdated packages
make update-packages

# Security vulnerability scan
make deps-check

# Install development tools
make tools-install
```

### Configuration Management
```bash
# List all config files
make config-list

# Validate JSON configs
make config-validate

# Generate env template
make config-env
```

## Target Categories

### General
- `help` - Display help message
- `version` - Show version information
- `all` - Clean, restore, build, and test

### Build
- `restore` - Restore NuGet packages
- `build` - Build the solution
- `rebuild` - Clean and rebuild
- `publish` - Publish applications

### Testing
- `test` - Run all tests
- `test-unit` - Run unit tests only
- `test-integration` - Run integration tests
- `test-performance` - Run performance tests
- `test-coverage` - Generate coverage report
- `test-watch` - Run tests in watch mode

### Benchmarking
- `bench` - Run all benchmarks
- `bench-run` - Run benchmarks without build
- `bench-specific` - Run filtered benchmarks

### Code Quality
- `lint` - Run code analysis
- `format` - Format code
- `format-check` - Check formatting

### Configuration
- `config-list` - List config files
- `config-validate` - Validate JSON files
- `config-env` - Generate env template

### Docker
- `docker-build` - Build Docker image
- `docker-run` - Run Docker container
- `docker-compose-up` - Start with compose
- `docker-compose-down` - Stop services
- `docker-logs` - View container logs

### Maintenance
- `clean` - Clean build artifacts
- `clean-all` - Deep clean with caches
- `update-packages` - Check for updates
- `deps-check` - Security scan

### Development
- `dev` - Start dev server
- `dev-api` - Start API dev server
- `tools-install` - Install dev tools

### Quick Commands
- `quick-test` - Run smoke test
- `run` - Run web application
- `run-api` - Run API service

## Tips and Tricks

1. **Parallel Builds**: Enable with `PARALLEL=true` (default) for faster builds
2. **Verbose Output**: Use `VERBOSITY=detailed` for debugging build issues
3. **Watch Mode**: Use `make dev` or `make test-watch` for TDD workflow
4. **Coverage Reports**: View HTML reports in `CoverageReport/index.html`
5. **Benchmark Results**: Find detailed reports in `BenchmarkResults/`

## Troubleshooting

### Build Failures
```bash
# Clean and rebuild
make rebuild

# Deep clean if issues persist
make clean-all
make build
```

### Test Failures
```bash
# Run with detailed output
make test VERBOSITY=detailed

# Run specific test project
dotnet test tests/Services/YourService.Tests
```

### Docker Issues
```bash
# Check Docker status
docker info

# Clean Docker resources
docker system prune -a

# Rebuild without cache
make docker-build --no-cache
```

## Integration with CI/CD

The Makefile is designed to work seamlessly with CI/CD pipelines:

```yaml
# Example GitHub Actions workflow
- name: Build and Test
  run: |
    make ci
    make package
```

## Contributing

When adding new targets:
1. Use descriptive names
2. Add help documentation with `##`
3. Include progress messages
4. Handle errors gracefully
5. Support configuration variables

Example:
```makefile
new-target: ## Description of what this does
	@echo -e "$(BLUE)🎯 Starting new target...$(NC)"
	# Your commands here
	@echo -e "$(GREEN)✅ New target completed$(NC)"
```
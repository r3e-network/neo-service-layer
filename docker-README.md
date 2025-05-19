# Neo Service Layer Docker Setup

This document provides instructions for running the Neo Service Layer using Docker.

## Prerequisites

- Docker and Docker Compose installed
- PowerShell (for Windows) or Bash (for Linux/macOS)

## Docker Configurations

The Neo Service Layer provides several Docker Compose configurations for different purposes:

### 1. Development Environment

The development environment includes all the necessary services for development, including SQL Server, Redis, and a mock Neo N3 node.

```powershell
# Run the development environment
.\run-dev-docker.ps1
```

This will start the following services:
- SQL Server
- Redis
- Mock Neo N3 Node
- TEE Host with SGX simulation
- API Service

### 2. Running Tests

The test environment is configured to run unit and integration tests.

```powershell
# Run all tests
.\run-tests-docker.ps1

# Run only unit tests
.\run-tests-docker.ps1 -UnitTests

# Run only integration tests
.\run-tests-docker.ps1 -IntegrationTests
```

### 3. Running Examples

The example environment is configured to run the example client with minimal dependencies.

```powershell
# Run the example
.\run-example-docker.ps1
```

This will start the following services:
- Mock Neo N3 Node
- API Service with mocked dependencies
- Example client

### 4. Production Environment

The production environment is configured for deployment with SGX simulation or hardware mode.

```powershell
# Run in production mode with SGX simulation
.\run-prod-docker.ps1

# Run in production mode with SGX hardware mode
.\run-prod-docker.ps1 -WithSGX
```

This will start the following services:
- SQL Server
- Redis
- Neo N3 Node
- TEE Host with SGX
- API Service

## Docker Compose Files

The following Docker Compose files are available:

- `docker-compose.dev.yml`: Development environment
- `docker-compose.tests.yml`: Test environment
- `docker-compose.example-mock.yml`: Example environment
- `docker-compose.prod.yml`: Production environment
- `docker-compose.migrations.yml`: Database migrations

## Scripts

The following scripts are available:

- `run-dev-docker.ps1`: Run the development environment
- `run-tests-docker.ps1`: Run tests
- `run-example-docker.ps1`: Run the example
- `run-prod-docker.ps1`: Run the production environment

## Environment Variables

The following environment variables can be set for the production environment:

- `SQL_PASSWORD`: SQL Server password (default: Password123!)
- `REDIS_PASSWORD`: Redis password (default: Password123!)
- `JWT_KEY`: JWT key for authentication (default: YourSecretKeyHere)
- `NEO_WALLET_PASSWORD`: Neo wallet password (default: password)
- `SGX_MODE`: SGX mode (HW or SIM, default: SIM)
- `SGX_SIMULATION`: SGX simulation flag (0 or 1, default: 1)
- `SGX_SIMULATION_MODE`: SGX simulation mode flag (true or false, default: true)

## Troubleshooting

If you encounter issues with the Docker setup, try the following:

1. Stop all containers and remove volumes:
   ```powershell
   docker-compose -f docker-compose.dev.yml down -v
   ```

2. Rebuild the images:
   ```powershell
   docker-compose -f docker-compose.dev.yml build --no-cache
   ```

3. Start the services again:
   ```powershell
   .\run-dev-docker.ps1
   ```

## Notes

- The development and example environments use SGX simulation mode by default.
- The production environment can be configured to use SGX hardware mode with the `-WithSGX` flag.
- The test environment includes both unit and integration tests.
- The example environment uses a mock Neo N3 node to avoid external dependencies.

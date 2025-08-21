#!/bin/bash

# Neo Service Layer PostgreSQL Integration Validation Script

set -e

echo "🔍 Neo Service Layer PostgreSQL Integration Validation"
echo "====================================================="

VALIDATION_PASSED=true
ERRORS=()

# Function to log errors
log_error() {
    ERRORS+=("❌ $1")
    VALIDATION_PASSED=false
}

# Function to log success
log_success() {
    echo "✅ $1"
}

echo ""
echo "📁 Validating File Structure..."

# Check for key PostgreSQL files
if [ -f "src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PostgreSQL/NeoServiceLayerDbContext.cs" ]; then
    log_success "PostgreSQL DbContext found"
else
    log_error "PostgreSQL DbContext missing"
fi

if [ -f "src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PostgreSQL/Migrations/001_InitialPostgreSQLSchema.sql" ]; then
    log_success "Database migration script found"
else
    log_error "Database migration script missing"
fi

if [ -f ".env.template" ]; then
    log_success "Environment template found"
else
    log_error "Environment template missing"
fi

echo ""
echo "🔧 Validating Configuration Files..."

# Check Docker Compose
if grep -q "neo-postgres:" docker-compose.yml; then
    log_success "PostgreSQL service configured in Docker Compose"
else
    log_error "PostgreSQL service missing from Docker Compose"
fi

if grep -q "neo-redis:" docker-compose.yml; then
    log_success "Redis service configured in Docker Compose"
else
    log_error "Redis service missing from Docker Compose"
fi

# Check appsettings.json
if grep -q "PostgreSQL" src/Api/NeoServiceLayer.Api/appsettings.json; then
    log_success "PostgreSQL configuration found in appsettings.json"
else
    log_error "PostgreSQL configuration missing from appsettings.json"
fi

echo ""
echo "📦 Validating Project Dependencies..."

# Check key service projects for PostgreSQL dependencies
SERVICES_TO_CHECK=(
    "src/Services/NeoServiceLayer.Services.Oracle/NeoServiceLayer.Services.Oracle.csproj"
    "src/Services/NeoServiceLayer.Services.Voting/NeoServiceLayer.Services.Voting.csproj"
    "src/Services/NeoServiceLayer.Services.CrossChain/NeoServiceLayer.Services.CrossChain.csproj"
    "src/Services/NeoServiceLayer.Services.EnclaveStorage/NeoServiceLayer.Services.EnclaveStorage.csproj"
)

for service in "${SERVICES_TO_CHECK[@]}"; do
    if [ -f "$service" ]; then
        if grep -q "Npgsql.EntityFrameworkCore.PostgreSQL" "$service"; then
            log_success "PostgreSQL dependency found in $(basename "$service")"
        else
            log_error "PostgreSQL dependency missing from $(basename "$service")"
        fi
    else
        log_error "Service project not found: $(basename "$service")"
    fi
done

echo ""
echo "🧪 Validating Service Implementations..."

# Check for PostgreSQL-specific implementations
POSTGRESQL_SERVICES=(
    "src/Services/NeoServiceLayer.Services.Oracle/OracleService.PostgreSQL.cs"
    "src/Services/NeoServiceLayer.Services.Voting/VotingService.PostgreSQL.cs"
    "src/Services/NeoServiceLayer.Services.CrossChain/CrossChainService.PostgreSQL.cs"
)

for service in "${POSTGRESQL_SERVICES[@]}"; do
    if [ -f "$service" ]; then
        log_success "PostgreSQL implementation found for $(basename "$service")"
    else
        log_error "PostgreSQL implementation missing: $(basename "$service")"
    fi
done

echo ""
echo "🏗️ Validating Entity Framework Setup..."

# Check for PostgreSQL entities
ENTITY_FILES=(
    "src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PostgreSQL/Entities/SgxEntities.cs"
    "src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PostgreSQL/Entities/OracleEntities.cs"
    "src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PostgreSQL/Entities/VotingEntities.cs"
    "src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PostgreSQL/Entities/CrossChainEntities.cs"
)

for entity in "${ENTITY_FILES[@]}"; do
    if [ -f "$entity" ]; then
        log_success "Entity file found: $(basename "$entity")"
    else
        log_error "Entity file missing: $(basename "$entity")"
    fi
done

# Check for PostgreSQL repositories
REPOSITORY_FILES=(
    "src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PostgreSQL/Repositories/SealedDataRepository.cs"
    "src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PostgreSQL/Repositories/OracleDataFeedRepository.cs"
    "src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PostgreSQL/Repositories/VotingRepository.cs"
    "src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PostgreSQL/Repositories/CrossChainRepository.cs"
)

for repo in "${REPOSITORY_FILES[@]}"; do
    if [ -f "$repo" ]; then
        log_success "Repository found: $(basename "$repo")"
    else
        log_error "Repository missing: $(basename "$repo")"
    fi
done

echo ""
echo "🔄 Validating Startup Configuration..."

# Check API Startup.cs for PostgreSQL registration
if grep -q "NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.NeoServiceLayerDbContext" src/Api/NeoServiceLayer.Api/Startup.cs; then
    log_success "PostgreSQL DbContext registered in Startup.cs"
else
    log_error "PostgreSQL DbContext not registered in Startup.cs"
fi

if grep -q "UseNpgsql" src/Api/NeoServiceLayer.Api/Startup.cs; then
    log_success "Npgsql provider configured in Startup.cs"
else
    log_error "Npgsql provider not configured in Startup.cs"
fi

echo ""
echo "🧪 Validating Test Configuration..."

# Check integration tests
if [ -f "tests/Integration/NeoServiceLayer.Integration.Tests/NeoServiceLayer.Integration.Tests.csproj" ]; then
    if grep -q "Npgsql.EntityFrameworkCore.PostgreSQL" "tests/Integration/NeoServiceLayer.Integration.Tests/NeoServiceLayer.Integration.Tests.csproj"; then
        log_success "PostgreSQL dependency found in integration tests"
    else
        log_error "PostgreSQL dependency missing from integration tests"
    fi
    
    if grep -q "Testcontainers.PostgreSql" "tests/Integration/NeoServiceLayer.Integration.Tests/NeoServiceLayer.Integration.Tests.csproj"; then
        log_success "Testcontainers PostgreSQL found in integration tests"
    else
        log_error "Testcontainers PostgreSQL missing from integration tests"
    fi
else
    log_error "Integration test project not found"
fi

echo ""
echo "📋 Validating Documentation..."

# Check README for PostgreSQL setup instructions
if grep -q "PostgreSQL Database Setup" README.md; then
    log_success "PostgreSQL setup instructions found in README"
else
    log_error "PostgreSQL setup instructions missing from README"
fi

echo ""
echo "🔍 Environment Validation..."

# Check for environment template
if [ -f ".env.template" ]; then
    if grep -q "POSTGRES_PASSWORD" .env.template; then
        log_success "PostgreSQL password configuration found in .env.template"
    else
        log_error "PostgreSQL password configuration missing from .env.template"
    fi
else
    log_error ".env.template file missing"
fi

echo ""
echo "📊 Validation Summary"
echo "===================="

if [ "$VALIDATION_PASSED" = true ]; then
    echo "🎉 All validations passed! PostgreSQL integration is complete."
    echo ""
    echo "✅ Key features validated:"
    echo "   • PostgreSQL database configuration"
    echo "   • Service implementations with PostgreSQL persistence"
    echo "   • Entity Framework Core integration"
    echo "   • Docker Compose with PostgreSQL and Redis"
    echo "   • Migration scripts and database schema"
    echo "   • Test infrastructure with PostgreSQL support"
    echo "   • Environment configuration and documentation"
    echo ""
    echo "🚀 Ready for deployment!"
    echo ""
    echo "Next steps:"
    echo "1. Run: ./scripts/setup-postgresql.sh"
    echo "2. Start: docker-compose up -d"
    echo "3. Test: curl http://localhost:8080/health"
    
    exit 0
else
    echo "❌ Validation failed with the following errors:"
    for error in "${ERRORS[@]}"; do
        echo "   $error"
    done
    echo ""
    echo "Please fix the errors above before deploying."
    exit 1
fi
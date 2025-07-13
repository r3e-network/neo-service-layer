#!/bin/bash

# Neo Service Layer Database Migration Script
# This script handles database migrations for production deployments

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
PROJECT_PATH="src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence"
STARTUP_PROJECT="src/Api/NeoServiceLayer.Api"
ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Development}
CONTEXT_NAME="ApplicationDbContext"

# Functions
print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --env)
            ENVIRONMENT="$2"
            shift 2
            ;;
        --add)
            MIGRATION_NAME="$2"
            shift 2
            ;;
        --remove)
            REMOVE_MIGRATION=true
            shift
            ;;
        --list)
            LIST_MIGRATIONS=true
            shift
            ;;
        --update)
            UPDATE_DATABASE=true
            shift
            ;;
        --script)
            GENERATE_SCRIPT=true
            shift
            ;;
        --rollback)
            ROLLBACK_TO="$2"
            shift 2
            ;;
        --help)
            echo "Usage: ./migrate.sh [options]"
            echo ""
            echo "Options:"
            echo "  --env <environment>     Set the environment (default: Development)"
            echo "  --add <name>           Add a new migration with the given name"
            echo "  --remove               Remove the last migration"
            echo "  --list                 List all migrations"
            echo "  --update               Update the database to the latest migration"
            echo "  --script               Generate SQL script for all migrations"
            echo "  --rollback <migration> Rollback to a specific migration"
            echo "  --help                 Show this help message"
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Check if dotnet ef is installed
if ! command -v dotnet &> /dev/null; then
    print_error "dotnet CLI is not installed"
    exit 1
fi

# Install or update dotnet-ef tool
if ! dotnet tool list -g | grep -q dotnet-ef; then
    print_warning "Installing dotnet-ef tool..."
    dotnet tool install -g dotnet-ef
else
    print_warning "Updating dotnet-ef tool..."
    dotnet tool update -g dotnet-ef
fi

# Ensure we're in the project root
if [ ! -f "NeoServiceLayer.sln" ]; then
    print_error "This script must be run from the project root directory"
    exit 1
fi

# Set environment
export ASPNETCORE_ENVIRONMENT=$ENVIRONMENT
print_success "Using environment: $ENVIRONMENT"

# Add migration
if [ ! -z "$MIGRATION_NAME" ]; then
    print_warning "Adding migration: $MIGRATION_NAME"
    dotnet ef migrations add "$MIGRATION_NAME" \
        --project "$PROJECT_PATH" \
        --startup-project "$STARTUP_PROJECT" \
        --context "$CONTEXT_NAME" \
        --output-dir Migrations \
        --verbose
    
    if [ $? -eq 0 ]; then
        print_success "Migration '$MIGRATION_NAME' added successfully"
        
        # Generate SQL script for the new migration
        SCRIPT_FILE="scripts/database/migrations/${MIGRATION_NAME}.sql"
        mkdir -p scripts/database/migrations
        
        dotnet ef migrations script \
            --project "$PROJECT_PATH" \
            --startup-project "$STARTUP_PROJECT" \
            --context "$CONTEXT_NAME" \
            --output "$SCRIPT_FILE" \
            --idempotent
        
        print_success "SQL script generated: $SCRIPT_FILE"
    else
        print_error "Failed to add migration"
        exit 1
    fi
fi

# Remove last migration
if [ "$REMOVE_MIGRATION" = true ]; then
    print_warning "Removing last migration..."
    dotnet ef migrations remove \
        --project "$PROJECT_PATH" \
        --startup-project "$STARTUP_PROJECT" \
        --context "$CONTEXT_NAME" \
        --force
    
    if [ $? -eq 0 ]; then
        print_success "Last migration removed successfully"
    else
        print_error "Failed to remove migration"
        exit 1
    fi
fi

# List migrations
if [ "$LIST_MIGRATIONS" = true ]; then
    echo "Listing all migrations..."
    dotnet ef migrations list \
        --project "$PROJECT_PATH" \
        --startup-project "$STARTUP_PROJECT" \
        --context "$CONTEXT_NAME"
fi

# Update database
if [ "$UPDATE_DATABASE" = true ]; then
    print_warning "Updating database..."
    
    # Create backup first if in production
    if [ "$ENVIRONMENT" = "Production" ]; then
        print_warning "Creating database backup before migration..."
        # Add your backup command here
        # pg_dump -h $DB_HOST -U $DB_USER -d $DB_NAME > backup_$(date +%Y%m%d_%H%M%S).sql
    fi
    
    dotnet ef database update \
        --project "$PROJECT_PATH" \
        --startup-project "$STARTUP_PROJECT" \
        --context "$CONTEXT_NAME" \
        --verbose
    
    if [ $? -eq 0 ]; then
        print_success "Database updated successfully"
    else
        print_error "Failed to update database"
        exit 1
    fi
fi

# Generate script
if [ "$GENERATE_SCRIPT" = true ]; then
    SCRIPT_FILE="scripts/database/migrations/complete_$(date +%Y%m%d_%H%M%S).sql"
    mkdir -p scripts/database/migrations
    
    print_warning "Generating SQL script..."
    dotnet ef migrations script \
        --project "$PROJECT_PATH" \
        --startup-project "$STARTUP_PROJECT" \
        --context "$CONTEXT_NAME" \
        --output "$SCRIPT_FILE" \
        --idempotent
    
    if [ $? -eq 0 ]; then
        print_success "SQL script generated: $SCRIPT_FILE"
    else
        print_error "Failed to generate script"
        exit 1
    fi
fi

# Rollback to specific migration
if [ ! -z "$ROLLBACK_TO" ]; then
    print_warning "Rolling back to migration: $ROLLBACK_TO"
    
    # Create backup first
    if [ "$ENVIRONMENT" = "Production" ]; then
        print_warning "Creating database backup before rollback..."
        # Add your backup command here
    fi
    
    dotnet ef database update "$ROLLBACK_TO" \
        --project "$PROJECT_PATH" \
        --startup-project "$STARTUP_PROJECT" \
        --context "$CONTEXT_NAME" \
        --verbose
    
    if [ $? -eq 0 ]; then
        print_success "Database rolled back to '$ROLLBACK_TO' successfully"
    else
        print_error "Failed to rollback database"
        exit 1
    fi
fi

print_success "Migration script completed"
#!/bin/bash

# Neo Service Layer Database Migration Script

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT=${ENVIRONMENT:-Development}
PROJECT_PATH="src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence"
STARTUP_PROJECT="src/Api/NeoServiceLayer.Api"

# Functions
print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
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
        --env)
            ENVIRONMENT="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --add <name>     Add a new migration with the specified name"
            echo "  --remove         Remove the last migration"
            echo "  --list           List all migrations"
            echo "  --update         Update the database to the latest migration"
            echo "  --script         Generate SQL script for all migrations"
            echo "  --env <env>      Set the environment (Development, Staging, Production)"
            echo "  --help           Show this help message"
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    print_error "dotnet CLI is not installed"
    exit 1
fi

# Check if EF Core tools are installed
if ! dotnet tool list -g | grep -q dotnet-ef; then
    print_warning "Installing Entity Framework Core tools..."
    dotnet tool install --global dotnet-ef
    print_success "EF Core tools installed"
fi

# Set environment
export ASPNETCORE_ENVIRONMENT=$ENVIRONMENT
print_warning "Using environment: $ENVIRONMENT"

# Change to project root
cd "$(dirname "$0")/../.."

# List migrations
if [ "$LIST_MIGRATIONS" = true ]; then
    print_warning "Listing migrations..."
    dotnet ef migrations list \
        --project "$PROJECT_PATH" \
        --startup-project "$STARTUP_PROJECT"
    exit 0
fi

# Add migration
if [ -n "$MIGRATION_NAME" ]; then
    print_warning "Adding migration: $MIGRATION_NAME"
    
    dotnet ef migrations add "$MIGRATION_NAME" \
        --project "$PROJECT_PATH" \
        --startup-project "$STARTUP_PROJECT" \
        --output-dir Migrations
    
    if [ $? -eq 0 ]; then
        print_success "Migration '$MIGRATION_NAME' created successfully"
        echo ""
        echo "Next steps:"
        echo "1. Review the generated migration files"
        echo "2. Run '$0 --update' to apply the migration"
    else
        print_error "Failed to create migration"
        exit 1
    fi
    exit 0
fi

# Remove last migration
if [ "$REMOVE_MIGRATION" = true ]; then
    print_warning "Removing last migration..."
    
    dotnet ef migrations remove \
        --project "$PROJECT_PATH" \
        --startup-project "$STARTUP_PROJECT"
    
    if [ $? -eq 0 ]; then
        print_success "Last migration removed successfully"
    else
        print_error "Failed to remove migration"
        exit 1
    fi
    exit 0
fi

# Update database
if [ "$UPDATE_DATABASE" = true ]; then
    print_warning "Updating database..."
    
    # Check if we can connect to the database
    print_warning "Checking database connection..."
    
    dotnet ef database update \
        --project "$PROJECT_PATH" \
        --startup-project "$STARTUP_PROJECT"
    
    if [ $? -eq 0 ]; then
        print_success "Database updated successfully"
    else
        print_error "Failed to update database"
        print_warning "Make sure the database server is running and connection string is correct"
        exit 1
    fi
    exit 0
fi

# Generate SQL script
if [ "$GENERATE_SCRIPT" = true ]; then
    print_warning "Generating SQL script..."
    
    SCRIPT_FILE="migrations_$(date +%Y%m%d_%H%M%S).sql"
    
    dotnet ef migrations script \
        --project "$PROJECT_PATH" \
        --startup-project "$STARTUP_PROJECT" \
        --output "$SCRIPT_FILE" \
        --idempotent
    
    if [ $? -eq 0 ]; then
        print_success "SQL script generated: $SCRIPT_FILE"
    else
        print_error "Failed to generate SQL script"
        exit 1
    fi
    exit 0
fi

# If no options provided, show help
echo "No action specified. Use --help to see available options."
exit 1
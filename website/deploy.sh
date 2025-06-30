#!/bin/bash

# Neo Service Layer Website Deployment Script
# This script builds and deploys the website

set -e

echo "🚀 Neo Service Layer Website Deployment"
echo "======================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if we're in the website directory
if [ ! -f "package.json" ]; then
    print_error "This script must be run from the website directory"
    exit 1
fi

# Check for required environment variables
print_status "Checking environment variables..."

if [ -z "$NEXTAUTH_SECRET" ]; then
    print_warning "NEXTAUTH_SECRET not set. Generating a random secret..."
    export NEXTAUTH_SECRET=$(openssl rand -base64 32)
fi

if [ -z "$DATABASE_URL" ]; then
    print_error "DATABASE_URL environment variable is required"
    exit 1
fi

print_success "Environment variables checked"

# Install dependencies
print_status "Installing dependencies..."
npm ci

print_success "Dependencies installed"

# Generate Prisma client
print_status "Generating Prisma client..."
npx prisma generate

print_success "Prisma client generated"

# Run database migrations
print_status "Running database migrations..."
npx prisma db push

print_success "Database migrations completed"

# Type checking
print_status "Running type checks..."
npm run type-check

print_success "Type checks passed"

# Linting
print_status "Running linter..."
npm run lint

print_success "Linting passed"

# Build the application
print_status "Building application..."
npm run build

print_success "Application built successfully"

# Check if we should start the server
if [ "$1" = "--start" ]; then
    print_status "Starting production server..."
    npm start
else
    print_success "Deployment preparation complete!"
    echo ""
    echo "To start the production server, run:"
    echo "  npm start"
    echo ""
    echo "Or deploy to your hosting platform with the built files."
fi

print_success "Neo Service Layer website deployment completed! 🎉"
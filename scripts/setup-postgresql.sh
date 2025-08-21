#!/bin/bash

# Neo Service Layer PostgreSQL Setup Script

set -e

echo "🚀 Neo Service Layer PostgreSQL Setup"
echo "====================================="

# Check if .env file exists
if [ ! -f .env ]; then
    echo "📋 Creating .env file from template..."
    cp .env.template .env
    
    # Generate JWT secret
    JWT_SECRET=$(openssl rand -base64 32)
    sed -i "s/^JWT_SECRET_KEY=$/JWT_SECRET_KEY=${JWT_SECRET}/" .env
    
    # Generate PostgreSQL password
    POSTGRES_PASSWORD=$(openssl rand -base64 16 | tr -d "=+/")
    sed -i "s/^POSTGRES_PASSWORD=$/POSTGRES_PASSWORD=${POSTGRES_PASSWORD}/" .env
    
    echo "✅ Environment file created with generated secrets"
    echo "📝 Please review .env file and adjust settings as needed"
else
    echo "ℹ️  Using existing .env file"
fi

# Load environment variables
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
fi

# Verify required environment variables
if [ -z "$JWT_SECRET_KEY" ]; then
    echo "❌ JWT_SECRET_KEY is not set in .env file"
    exit 1
fi

if [ -z "$POSTGRES_PASSWORD" ]; then
    echo "❌ POSTGRES_PASSWORD is not set in .env file"
    exit 1
fi

echo "🐳 Starting PostgreSQL with Docker Compose..."
docker-compose up -d neo-postgres neo-redis

echo "⏳ Waiting for PostgreSQL to be ready..."
timeout 60 bash -c 'until docker-compose exec neo-postgres pg_isready -U neo_user -d neo_service_layer; do sleep 2; done'

echo "🏗️  Running database migrations..."
# Use the migration runner or manual SQL execution
docker-compose exec neo-postgres psql -U neo_user -d neo_service_layer -f /docker-entrypoint-initdb.d/001_InitialPostgreSQLSchema.sql || echo "Migration may have already been applied"

echo "🔍 Verifying database setup..."
docker-compose exec neo-postgres psql -U neo_user -d neo_service_layer -c "SELECT schemaname FROM pg_tables WHERE schemaname IN ('core', 'auth', 'sgx', 'oracle', 'voting', 'crosschain') LIMIT 5;"

echo "✅ PostgreSQL setup completed successfully!"
echo ""
echo "📊 Database Information:"
echo "  - Host: localhost"
echo "  - Port: 5432"
echo "  - Database: neo_service_layer"
echo "  - Username: neo_user"
echo "  - Password: (stored in .env file)"
echo ""
echo "🏃 To start the full application:"
echo "  docker-compose up -d"
echo ""
echo "🔍 To check application health:"
echo "  curl http://localhost:8080/health"
echo ""
echo "📚 To view API documentation:"
echo "  http://localhost:8080/swagger"
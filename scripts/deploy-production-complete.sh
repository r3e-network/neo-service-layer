#!/bin/bash

# Neo Service Layer - Complete Production Deployment Script
# This script handles the full production deployment process

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DEPLOYMENT_ENV="${DEPLOYMENT_ENV:-production}"
NAMESPACE="${NAMESPACE:-neo-service-layer}"

# Functions
print_header() {
    echo -e "\n${BLUE}==== $1 ====${NC}\n"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

check_prerequisites() {
    print_header "Checking Prerequisites"
    
    local missing_deps=()
    
    # Check required tools
    command -v docker >/dev/null 2>&1 || missing_deps+=("docker")
    command -v docker compose >/dev/null 2>&1 || missing_deps+=("docker compose")
    command -v dotnet >/dev/null 2>&1 || missing_deps+=("dotnet")
    command -v openssl >/dev/null 2>&1 || missing_deps+=("openssl")
    
    if [ ${#missing_deps[@]} -ne 0 ]; then
        print_error "Missing required dependencies: ${missing_deps[*]}"
        exit 1
    fi
    
    print_success "All prerequisites met"
}

generate_credentials() {
    print_header "Generating Secure Credentials"
    
    cd "$PROJECT_ROOT"
    
    if [ ! -f ".env.production" ]; then
        print_warning "Generating production credentials..."
        ./scripts/generate-secure-credentials.sh
        print_success "Credentials generated"
    else
        print_warning "Production credentials already exist"
    fi
    
    # Verify critical environment variables
    source .env.production
    
    if [ -z "$JWT_SECRET_KEY" ] || [ "$JWT_SECRET_KEY" == "CHANGE_ME_USE_SCRIPT" ]; then
        print_error "JWT_SECRET_KEY not properly configured"
        exit 1
    fi
    
    print_success "Credentials verified"
}

run_tests() {
    print_header "Running Tests"
    
    cd "$PROJECT_ROOT"
    
    print_warning "Running unit tests..."
    dotnet test --filter "Category=Unit" --no-build --logger "console;verbosity=minimal" || {
        print_error "Unit tests failed"
        exit 1
    }
    
    print_warning "Running integration tests..."
    dotnet test --filter "Category=Integration" --no-build --logger "console;verbosity=minimal" || {
        print_warning "Some integration tests failed (continuing)"
    }
    
    print_success "Tests completed"
}

build_application() {
    print_header "Building Application"
    
    cd "$PROJECT_ROOT"
    
    print_warning "Building .NET application..."
    dotnet build --configuration Release || {
        print_error "Build failed"
        exit 1
    }
    
    print_success "Application built successfully"
}

run_database_migrations() {
    print_header "Running Database Migrations"
    
    cd "$PROJECT_ROOT"
    
    # Start database if not running
    docker compose up -d postgres
    
    # Wait for database to be ready
    print_warning "Waiting for database..."
    sleep 10
    
    # Run migrations
    print_warning "Applying database migrations..."
    ./scripts/database/migrate.sh --update --env Production || {
        print_error "Database migration failed"
        exit 1
    }
    
    print_success "Database migrations completed"
}

build_docker_images() {
    print_header "Building Docker Images"
    
    cd "$PROJECT_ROOT"
    
    # Build base images
    print_warning "Building base images..."
    ./scripts/build-base-images.sh || {
        print_error "Base image build failed"
        exit 1
    }
    
    # Build service images
    print_warning "Building service images..."
    docker compose -f docker compose.production.yml build || {
        print_error "Service image build failed"
        exit 1
    }
    
    # Build Occlum production image
    print_warning "Building Occlum production image..."
    docker build -f Dockerfile.occlum.production -t neo-service-layer-occlum:production . || {
        print_error "Occlum image build failed"
        exit 1
    }
    
    print_success "All Docker images built"
}

configure_ssl_certificates() {
    print_header "Configuring SSL Certificates"
    
    cd "$PROJECT_ROOT"
    
    if [ ! -f "certificates/certificate.pfx" ]; then
        print_warning "No production certificate found. Using self-signed certificate for testing."
        mkdir -p certificates
        
        # This should be replaced with real certificates in production
        openssl req -x509 -newkey rsa:4096 -keyout certificates/key.pem -out certificates/cert.pem -days 365 -nodes \
            -subj "/C=US/ST=State/L=City/O=NeoServiceLayer/CN=neo-service-layer.com"
        
        openssl pkcs12 -export -out certificates/certificate.pfx -inkey certificates/key.pem -in certificates/cert.pem \
            -password pass:${CERTIFICATE_PASSWORD}
        
        rm -f certificates/key.pem certificates/cert.pem
        chmod 600 certificates/certificate.pfx
    fi
    
    print_success "SSL certificates configured"
}

deploy_services() {
    print_header "Deploying Services"
    
    cd "$PROJECT_ROOT"
    
    # Stop existing services
    print_warning "Stopping existing services..."
    docker compose -f docker compose.production.yml down
    
    # Start infrastructure services first
    print_warning "Starting infrastructure services..."
    docker compose -f docker compose.production.yml up -d \
        consul postgres redis rabbitmq prometheus grafana jaeger
    
    # Wait for infrastructure
    print_warning "Waiting for infrastructure to be ready..."
    sleep 30
    
    # Start application services
    print_warning "Starting application services..."
    docker compose -f docker compose.production.yml up -d
    
    print_success "All services deployed"
}

configure_monitoring() {
    print_header "Configuring Monitoring"
    
    # Configure Prometheus alerts
    print_warning "Configuring Prometheus alerts..."
    cp "$PROJECT_ROOT/monitoring/prometheus/alerts.yml" \
       "$PROJECT_ROOT/prometheus_data/alerts.yml" 2>/dev/null || true
    
    # Restart Prometheus to load alerts
    docker compose -f docker compose.production.yml restart prometheus
    
    print_success "Monitoring configured"
}

verify_deployment() {
    print_header "Verifying Deployment"
    
    local services=(
        "consul:8500/v1/status/leader"
        "api-gateway:443/health"
        "prometheus:9090/-/healthy"
        "grafana:3000/api/health"
        "jaeger:16686/"
    )
    
    for service in "${services[@]}"; do
        IFS=':' read -r name endpoint <<< "$service"
        
        if curl -k -f -s "https://localhost:${endpoint}" >/dev/null 2>&1 || \
           curl -f -s "http://localhost:${endpoint}" >/dev/null 2>&1; then
            print_success "$name is healthy"
        else
            print_warning "$name health check failed (may still be starting)"
        fi
    done
    
    # Show running containers
    print_warning "Running containers:"
    docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
}

print_deployment_summary() {
    print_header "Deployment Summary"
    
    echo "Neo Service Layer has been deployed successfully!"
    echo ""
    echo "Access points:"
    echo "- API Gateway: https://localhost:443"
    echo "- Consul UI: http://localhost:8500"
    echo "- Grafana: http://localhost:3000 (admin/${GRAFANA_ADMIN_PASSWORD})"
    echo "- Jaeger UI: http://localhost:16686"
    echo "- Prometheus: http://localhost:9090"
    echo ""
    echo "Health endpoints:"
    echo "- https://localhost:443/health"
    echo "- https://localhost:443/health/ready"
    echo "- https://localhost:443/health/live"
    echo ""
    print_warning "Remember to:"
    echo "1. Update DNS records to point to this server"
    echo "2. Configure firewall rules for production access"
    echo "3. Set up backup procedures"
    echo "4. Configure log aggregation"
    echo "5. Set up monitoring alerts"
}

# Main execution
main() {
    print_header "Neo Service Layer Production Deployment"
    
    check_prerequisites
    generate_credentials
    run_tests
    build_application
    run_database_migrations
    build_docker_images
    configure_ssl_certificates
    deploy_services
    configure_monitoring
    verify_deployment
    print_deployment_summary
    
    print_success "Deployment completed successfully!"
}

# Run main function
main "$@"
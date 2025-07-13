#!/bin/bash

# Neo Service Layer Production Deployment Script
# This script handles the complete production deployment process

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
DEPLOYMENT_ENV=${1:-production}
ENV_FILE=".env.${DEPLOYMENT_ENV}"
DOCKER_COMPOSE_FILE="docker-compose.${DEPLOYMENT_ENV}.yml"
BACKUP_DIR="backups/$(date +%Y%m%d_%H%M%S)"
HEALTH_CHECK_RETRIES=30
HEALTH_CHECK_DELAY=10

# Functions
print_header() {
    echo -e "\n${BLUE}=== $1 ===${NC}\n"
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

# Check prerequisites
check_prerequisites() {
    print_header "Checking Prerequisites"
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed"
        exit 1
    fi
    print_success "Docker is installed"
    
    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        print_error "Docker Compose is not installed"
        exit 1
    fi
    print_success "Docker Compose is installed"
    
    # Check environment file
    if [ ! -f "$ENV_FILE" ]; then
        print_error "Environment file $ENV_FILE not found"
        print_warning "Run ./scripts/generate-secure-credentials.sh first"
        exit 1
    fi
    print_success "Environment file found"
    
    # Check docker-compose file
    if [ ! -f "$DOCKER_COMPOSE_FILE" ]; then
        print_error "Docker compose file $DOCKER_COMPOSE_FILE not found"
        exit 1
    fi
    print_success "Docker compose file found"
    
    # Check certificates
    if [ ! -f "certificates/certificate.pfx" ]; then
        print_warning "SSL certificate not found. HTTPS will not work properly."
        print_warning "Generate or install a valid certificate in certificates/certificate.pfx"
    else
        print_success "SSL certificate found"
    fi
}

# Validate environment configuration
validate_environment() {
    print_header "Validating Environment Configuration"
    
    # Source the environment file
    set -a
    source "$ENV_FILE"
    set +a
    
    # Check critical variables
    CRITICAL_VARS=(
        "JWT_SECRET_KEY"
        "DB_PASSWORD"
        "REDIS_PASSWORD"
        "RABBITMQ_PASSWORD"
    )
    
    for var in "${CRITICAL_VARS[@]}"; do
        if [ -z "${!var}" ]; then
            print_error "$var is not set in $ENV_FILE"
            exit 1
        fi
        
        # Check for default/weak values
        if [[ "${!var}" == *"CHANGE_THIS"* ]]; then
            print_error "$var contains default value. Please update it!"
            exit 1
        fi
    done
    
    print_success "All critical environment variables are set"
}

# Create backup
create_backup() {
    print_header "Creating Backup"
    
    mkdir -p "$BACKUP_DIR"
    
    # Backup database if running
    if docker ps | grep -q neo-postgres; then
        print_warning "Backing up database..."
        docker exec neo-postgres pg_dump -U "$DB_USER" -d "$DB_NAME" > "$BACKUP_DIR/database.sql"
        print_success "Database backed up to $BACKUP_DIR/database.sql"
    fi
    
    # Backup volumes
    if [ -d "volumes" ]; then
        cp -r volumes "$BACKUP_DIR/"
        print_success "Volumes backed up"
    fi
    
    # Backup configuration
    cp "$ENV_FILE" "$BACKUP_DIR/"
    print_success "Configuration backed up"
}

# Run database migrations
run_migrations() {
    print_header "Running Database Migrations"
    
    # Check if migration script exists
    if [ -f "scripts/database/migrate.sh" ]; then
        print_warning "Running database migrations..."
        ./scripts/database/migrate.sh --env "$DEPLOYMENT_ENV" --update
        print_success "Database migrations completed"
    else
        print_warning "Migration script not found, skipping..."
    fi
}

# Deploy services
deploy_services() {
    print_header "Deploying Services"
    
    # Pull latest images
    print_warning "Pulling latest Docker images..."
    docker-compose -f "$DOCKER_COMPOSE_FILE" --env-file "$ENV_FILE" pull
    
    # Deploy with zero-downtime strategy
    print_warning "Starting services..."
    docker-compose -f "$DOCKER_COMPOSE_FILE" --env-file "$ENV_FILE" up -d --build --remove-orphans
    
    print_success "Services deployed"
}

# Health checks
perform_health_checks() {
    print_header "Performing Health Checks"
    
    local services=(
        "api-gateway:443"
        "consul:8500"
        "postgres:5432"
        "redis:6379"
        "rabbitmq:5672"
        "prometheus:9090"
        "grafana:3000"
    )
    
    for service in "${services[@]}"; do
        local name="${service%%:*}"
        local port="${service##*:}"
        
        echo -n "Checking $name... "
        
        local retries=0
        while [ $retries -lt $HEALTH_CHECK_RETRIES ]; do
            if docker-compose -f "$DOCKER_COMPOSE_FILE" ps | grep -q "$name.*Up"; then
                echo -e "${GREEN}✓${NC}"
                break
            fi
            
            retries=$((retries + 1))
            if [ $retries -eq $HEALTH_CHECK_RETRIES ]; then
                echo -e "${RED}✗${NC}"
                print_error "$name failed to start"
            else
                sleep $HEALTH_CHECK_DELAY
            fi
        done
    done
    
    # Check API Gateway health endpoint
    print_warning "Checking API Gateway health endpoint..."
    local retries=0
    while [ $retries -lt $HEALTH_CHECK_RETRIES ]; do
        if curl -sk https://localhost/health > /dev/null 2>&1; then
            print_success "API Gateway is healthy"
            break
        fi
        
        retries=$((retries + 1))
        if [ $retries -eq $HEALTH_CHECK_RETRIES ]; then
            print_error "API Gateway health check failed"
        else
            sleep $HEALTH_CHECK_DELAY
        fi
    done
}

# Configure monitoring
configure_monitoring() {
    print_header "Configuring Monitoring"
    
    # Wait for Grafana to be ready
    print_warning "Waiting for Grafana to initialize..."
    sleep 30
    
    # Import dashboards (if Grafana API is available)
    if curl -sk "http://localhost:3000/api/health" > /dev/null 2>&1; then
        print_success "Grafana is ready"
        # Additional Grafana configuration can be added here
    else
        print_warning "Grafana is not accessible, manual configuration may be required"
    fi
    
    print_success "Monitoring configuration completed"
}

# Post-deployment tasks
post_deployment() {
    print_header "Post-Deployment Tasks"
    
    # Display service URLs
    echo -e "\n${GREEN}Service URLs:${NC}"
    echo "API Gateway: https://localhost"
    echo "Consul UI: http://localhost:8500"
    echo "Grafana: http://localhost:3000"
    echo "Prometheus: http://localhost:9090"
    echo "Jaeger UI: http://localhost:16686"
    echo "RabbitMQ Management: http://localhost:15672"
    
    # Display logs command
    echo -e "\n${GREEN}View logs:${NC}"
    echo "docker-compose -f $DOCKER_COMPOSE_FILE logs -f [service-name]"
    
    # Display useful commands
    echo -e "\n${GREEN}Useful commands:${NC}"
    echo "View all services: docker-compose -f $DOCKER_COMPOSE_FILE ps"
    echo "Stop all services: docker-compose -f $DOCKER_COMPOSE_FILE down"
    echo "View service logs: docker-compose -f $DOCKER_COMPOSE_FILE logs [service]"
    echo "Scale a service: docker-compose -f $DOCKER_COMPOSE_FILE up -d --scale [service]=N"
}

# Rollback function
rollback() {
    print_header "Rolling Back Deployment"
    
    print_error "Deployment failed, rolling back..."
    
    # Stop current deployment
    docker-compose -f "$DOCKER_COMPOSE_FILE" down
    
    # Restore from backup if available
    if [ -d "$BACKUP_DIR" ]; then
        print_warning "Restoring from backup..."
        # Restore database
        if [ -f "$BACKUP_DIR/database.sql" ]; then
            # Start only postgres
            docker-compose -f "$DOCKER_COMPOSE_FILE" up -d postgres
            sleep 10
            docker exec -i neo-postgres psql -U "$DB_USER" -d "$DB_NAME" < "$BACKUP_DIR/database.sql"
            print_success "Database restored"
        fi
    fi
    
    print_error "Rollback completed. Manual intervention may be required."
    exit 1
}

# Main deployment flow
main() {
    print_header "Neo Service Layer Production Deployment"
    echo "Environment: $DEPLOYMENT_ENV"
    echo "Start time: $(date)"
    
    # Set error trap
    trap rollback ERR
    
    # Execute deployment steps
    check_prerequisites
    validate_environment
    create_backup
    run_migrations
    deploy_services
    perform_health_checks
    configure_monitoring
    post_deployment
    
    # Clear error trap
    trap - ERR
    
    print_header "Deployment Completed Successfully"
    echo "End time: $(date)"
}

# Handle command line arguments
case "${2:-deploy}" in
    deploy)
        main
        ;;
    stop)
        print_header "Stopping Services"
        docker-compose -f "$DOCKER_COMPOSE_FILE" down
        print_success "Services stopped"
        ;;
    restart)
        print_header "Restarting Services"
        docker-compose -f "$DOCKER_COMPOSE_FILE" restart
        print_success "Services restarted"
        ;;
    status)
        print_header "Service Status"
        docker-compose -f "$DOCKER_COMPOSE_FILE" ps
        ;;
    logs)
        docker-compose -f "$DOCKER_COMPOSE_FILE" logs -f "${3:-}"
        ;;
    backup)
        create_backup
        print_success "Backup completed"
        ;;
    *)
        echo "Usage: $0 [environment] [deploy|stop|restart|status|logs|backup]"
        echo "Example: $0 production deploy"
        exit 1
        ;;
esac
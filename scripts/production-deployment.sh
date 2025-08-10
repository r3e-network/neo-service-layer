#!/bin/bash
# Neo Service Layer - Production Deployment Script
# Comprehensive deployment script with validation and rollback capabilities

set -euo pipefail

# Configuration
NAMESPACE="neo-service-layer"
DEPLOYMENT_ENV="${DEPLOYMENT_ENV:-production}"
DRY_RUN="${DRY_RUN:-false}"
ROLLBACK_ON_ERROR="${ROLLBACK_ON_ERROR:-true}"
DEPLOYMENT_TIMEOUT="600s"
VERSION_TAG="${VERSION_TAG:-latest}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Logging functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

# Error handling
cleanup() {
    if [ $? -ne 0 ] && [ "$ROLLBACK_ON_ERROR" = "true" ]; then
        log_error "Deployment failed. Initiating rollback..."
        rollback_deployment
    fi
}
trap cleanup EXIT

# Function to check prerequisites
check_prerequisites() {
    log_step "Checking prerequisites..."
    
    # Check kubectl
    if ! command -v kubectl &> /dev/null; then
        log_error "kubectl is required but not installed"
        exit 1
    fi
    
    # Check helm
    if ! command -v helm &> /dev/null; then
        log_error "helm is required but not installed"
        exit 1
    fi
    
    # Check kubernetes connection
    if ! kubectl cluster-info &> /dev/null; then
        log_error "Cannot connect to Kubernetes cluster"
        exit 1
    fi
    
    # Check namespace exists
    if ! kubectl get namespace $NAMESPACE &> /dev/null; then
        log_warn "Namespace $NAMESPACE doesn't exist. Creating..."
        kubectl create namespace $NAMESPACE
    fi
    
    log_info "Prerequisites check passed"
}

# Function to validate configurations
validate_configurations() {
    log_step "Validating configurations..."
    
    # Validate YAML files
    for file in k8s/base/*.yaml k8s/services/*.yaml; do
        if [ -f "$file" ]; then
            kubectl apply --dry-run=client -f "$file" &> /dev/null || {
                log_error "Invalid YAML in $file"
                exit 1
            }
        fi
    done
    
    # Check for required secrets
    if ! kubectl get secret neo-secrets -n $NAMESPACE &> /dev/null; then
        log_error "Required secret 'neo-secrets' not found. Run generate-secrets.sh first"
        exit 1
    fi
    
    log_info "Configuration validation passed"
}

# Function to backup current state
backup_current_state() {
    log_step "Backing up current deployment state..."
    
    BACKUP_DIR="backups/$(date +%Y%m%d_%H%M%S)"
    mkdir -p "$BACKUP_DIR"
    
    # Backup deployments
    kubectl get deployments -n $NAMESPACE -o yaml > "$BACKUP_DIR/deployments.yaml"
    
    # Backup services
    kubectl get services -n $NAMESPACE -o yaml > "$BACKUP_DIR/services.yaml"
    
    # Backup configmaps
    kubectl get configmaps -n $NAMESPACE -o yaml > "$BACKUP_DIR/configmaps.yaml"
    
    # Backup current images
    kubectl get deployments -n $NAMESPACE -o jsonpath='{range .items[*]}{.metadata.name}{"\t"}{.spec.template.spec.containers[0].image}{"\n"}{end}' > "$BACKUP_DIR/images.txt"
    
    echo "$BACKUP_DIR" > .last_backup
    log_info "Backup saved to $BACKUP_DIR"
}

# Function to deploy infrastructure components
deploy_infrastructure() {
    log_step "Deploying infrastructure components..."
    
    # Apply resource quotas and limits
    kubectl apply -f k8s/base/resource-quotas.yaml
    
    # Apply pod security standards
    kubectl apply -f k8s/base/pod-security-standards.yaml
    
    # Apply network policies
    kubectl apply -f k8s/base/network-policies.yaml
    
    # Deploy Redis
    if ! kubectl get deployment redis -n $NAMESPACE &> /dev/null; then
        log_info "Deploying Redis..."
        kubectl apply -f k8s/base/redis.yaml
        kubectl wait --for=condition=available --timeout=$DEPLOYMENT_TIMEOUT deployment/redis -n $NAMESPACE
    fi
    
    # Deploy PostgreSQL
    if ! kubectl get deployment postgres -n $NAMESPACE &> /dev/null; then
        log_info "Deploying PostgreSQL..."
        kubectl apply -f k8s/base/postgres.yaml
        kubectl wait --for=condition=available --timeout=$DEPLOYMENT_TIMEOUT deployment/postgres -n $NAMESPACE
    fi
    
    # Deploy RabbitMQ
    if ! kubectl get deployment rabbitmq -n $NAMESPACE &> /dev/null; then
        log_info "Deploying RabbitMQ..."
        kubectl apply -f k8s/base/rabbitmq.yaml
        kubectl wait --for=condition=available --timeout=$DEPLOYMENT_TIMEOUT deployment/rabbitmq -n $NAMESPACE
    fi
    
    # Deploy Consul
    kubectl apply -f k8s/base/consul.yaml
    kubectl wait --for=condition=ready --timeout=$DEPLOYMENT_TIMEOUT statefulset/consul -n $NAMESPACE
    
    log_info "Infrastructure components deployed"
}

# Function to deploy monitoring stack
deploy_monitoring() {
    log_step "Deploying monitoring stack..."
    
    # Deploy Prometheus
    kubectl apply -f k8s/base/monitoring.yaml
    
    # Deploy Grafana (if exists)
    if [ -f "k8s/base/grafana.yaml" ]; then
        kubectl apply -f k8s/base/grafana.yaml
    fi
    
    log_info "Monitoring stack deployed"
}

# Function to deploy services
deploy_services() {
    log_step "Deploying Neo services..."
    
    # Core services deployment order
    CORE_SERVICES=(
        "storage-service"
        "key-management-service"
        "notification-service"
        "health-service"
        "monitoring-service"
    )
    
    # Deploy core services first
    for service in "${CORE_SERVICES[@]}"; do
        if [ -f "k8s/services/$service.yaml" ]; then
            log_info "Deploying $service..."
            kubectl apply -f "k8s/services/$service.yaml"
        fi
    done
    
    # Wait for core services
    for service in "${CORE_SERVICES[@]}"; do
        if kubectl get deployment "$service" -n $NAMESPACE &> /dev/null; then
            kubectl wait --for=condition=available --timeout=$DEPLOYMENT_TIMEOUT deployment/"$service" -n $NAMESPACE
        fi
    done
    
    # Deploy remaining services
    for file in k8s/services/*.yaml; do
        service_name=$(basename "$file" .yaml)
        if [[ ! " ${CORE_SERVICES[@]} " =~ " ${service_name} " ]]; then
            log_info "Deploying $service_name..."
            kubectl apply -f "$file"
        fi
    done
    
    # Apply HPA configurations
    kubectl apply -f k8s/base/hpa.yaml
    
    log_info "All services deployed"
}

# Function to run health checks
run_health_checks() {
    log_step "Running health checks..."
    
    FAILED_CHECKS=0
    
    # Get all deployments
    DEPLOYMENTS=$(kubectl get deployments -n $NAMESPACE -o jsonpath='{.items[*].metadata.name}')
    
    for deployment in $DEPLOYMENTS; do
        # Check if deployment is ready
        READY=$(kubectl get deployment "$deployment" -n $NAMESPACE -o jsonpath='{.status.conditions[?(@.type=="Available")].status}')
        if [ "$READY" != "True" ]; then
            log_error "Deployment $deployment is not ready"
            ((FAILED_CHECKS++))
        else
            log_info "✓ $deployment is healthy"
        fi
        
        # Check service endpoint if exists
        if kubectl get service "$deployment" -n $NAMESPACE &> /dev/null; then
            # Try to hit health endpoint
            SERVICE_IP=$(kubectl get service "$deployment" -n $NAMESPACE -o jsonpath='{.spec.clusterIP}')
            if [ ! -z "$SERVICE_IP" ]; then
                # Note: This would need to be run from within the cluster or with port-forwarding
                log_info "  Service endpoint: $SERVICE_IP"
            fi
        fi
    done
    
    if [ $FAILED_CHECKS -gt 0 ]; then
        log_error "$FAILED_CHECKS health checks failed"
        return 1
    fi
    
    log_info "All health checks passed"
}

# Function to rollback deployment
rollback_deployment() {
    log_step "Rolling back deployment..."
    
    if [ -f ".last_backup" ]; then
        BACKUP_DIR=$(cat .last_backup)
        if [ -d "$BACKUP_DIR" ]; then
            log_info "Restoring from backup: $BACKUP_DIR"
            kubectl apply -f "$BACKUP_DIR/deployments.yaml"
            kubectl apply -f "$BACKUP_DIR/services.yaml"
            kubectl apply -f "$BACKUP_DIR/configmaps.yaml"
            log_info "Rollback completed"
        else
            log_error "Backup directory not found: $BACKUP_DIR"
        fi
    else
        log_error "No backup information found"
    fi
}

# Function to display deployment summary
display_summary() {
    log_step "Deployment Summary"
    echo "===================="
    echo "Environment: $DEPLOYMENT_ENV"
    echo "Namespace: $NAMESPACE"
    echo "Version: $VERSION_TAG"
    echo ""
    
    # List deployed services
    echo "Deployed Services:"
    kubectl get deployments -n $NAMESPACE --no-headers | awk '{print "  - " $1 " (" $2 "/" $3 " ready)"}'
    echo ""
    
    # Show resource usage
    echo "Resource Usage:"
    kubectl top nodes || log_warn "Metrics server not available"
    echo ""
    
    # Show endpoints
    echo "Service Endpoints:"
    kubectl get services -n $NAMESPACE --no-headers | grep -v ClusterIP | awk '{print "  - " $1 ": " $4}'
}

# Main deployment flow
main() {
    log_info "Starting Neo Service Layer production deployment"
    log_info "Environment: $DEPLOYMENT_ENV"
    
    if [ "$DRY_RUN" = "true" ]; then
        log_warn "Running in DRY RUN mode - no changes will be applied"
    fi
    
    # Pre-deployment steps
    check_prerequisites
    validate_configurations
    
    if [ "$DRY_RUN" != "true" ]; then
        backup_current_state
        
        # Deploy components
        deploy_infrastructure
        deploy_monitoring
        deploy_services
        
        # Post-deployment validation
        log_info "Waiting for deployments to stabilize..."
        sleep 30
        
        run_health_checks
        
        # Display summary
        display_summary
        
        log_info "Deployment completed successfully!"
        
        # Save deployment info
        cat > .last_deployment <<EOF
DATE: $(date)
ENVIRONMENT: $DEPLOYMENT_ENV
VERSION: $VERSION_TAG
NAMESPACE: $NAMESPACE
EOF
    else
        log_info "Dry run completed. No changes were made."
    fi
}

# Run main function
main "$@"
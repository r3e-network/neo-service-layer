#!/bin/bash

# Neo Service Layer - Deployment Migration Script
# Phase 3: Deploy microservices and migrate traffic
# This script handles deployment automation and traffic migration

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
LOG_FILE="${PROJECT_ROOT}/logs/deployment-$(date +%Y%m%d_%H%M%S).log"

# Kubernetes configuration
KUBE_CONFIG="${KUBE_CONFIG:-$HOME/.kube/config}"
NAMESPACE_SERVICES="neo-services"
NAMESPACE_DATABASES="neo-databases"
NAMESPACE_MONITORING="neo-monitoring"
NAMESPACE_INFRASTRUCTURE="neo-infrastructure"

# Service deployment order (dependencies first)
DEPLOYMENT_ORDER=(
    "infrastructure"
    "databases" 
    "monitoring"
    "auth"
    "oracle"
    "compute"
    "storage"
    "secrets"
    "voting"
    "crosschain"
    "health"
)

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Logging functions
log() {
    echo -e "${1}" | tee -a "${LOG_FILE}"
}

log_info() {
    log "${BLUE}[INFO]${NC} $1"
}

log_success() {
    log "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    log "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    log "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking deployment prerequisites..."
    
    # Check kubectl
    if ! command -v kubectl &> /dev/null; then
        log_error "kubectl is not installed. Please install kubectl."
        exit 1
    fi
    
    # Check cluster connection
    if ! kubectl cluster-info &> /dev/null; then
        log_error "Cannot connect to Kubernetes cluster. Check your kubeconfig."
        exit 1
    fi
    
    # Check cluster nodes
    local node_count
    node_count=$(kubectl get nodes --no-headers | wc -l)
    log_info "Connected to cluster with ${node_count} nodes"
    
    # Check if Istio is installed
    if ! kubectl get ns istio-system &> /dev/null; then
        log_warning "Istio system namespace not found. Service mesh features may not work."
    fi
    
    # Create log directory
    mkdir -p "$(dirname "${LOG_FILE}")"
    
    log_success "Prerequisites check completed"
}

# Deploy infrastructure components
deploy_infrastructure() {
    log_info "Deploying infrastructure components..."
    
    # Apply namespaces first
    kubectl apply -f "${PROJECT_ROOT}/k8s/namespaces/" 2>&1 | tee -a "${LOG_FILE}"
    
    # Wait for namespaces to be ready
    for ns in ${NAMESPACE_SERVICES} ${NAMESPACE_DATABASES} ${NAMESPACE_MONITORING} ${NAMESPACE_INFRASTRUCTURE}; do
        kubectl wait --for=condition=Active --timeout=60s namespace/${ns} || log_warning "Namespace ${ns} not ready"
    done
    
    # Deploy infrastructure
    log_info "Applying infrastructure manifests..."
    kubectl apply -f "${PROJECT_ROOT}/k8s/infrastructure/" 2>&1 | tee -a "${LOG_FILE}"
    
    # Wait for infrastructure to be ready
    log_info "Waiting for infrastructure components..."
    kubectl wait --for=condition=available --timeout=300s deployment -n ${NAMESPACE_DATABASES} --all 2>&1 | tee -a "${LOG_FILE}" || true
    kubectl wait --for=condition=available --timeout=300s deployment -n ${NAMESPACE_INFRASTRUCTURE} --all 2>&1 | tee -a "${LOG_FILE}" || true
    
    log_success "Infrastructure deployment completed"
}

# Deploy monitoring stack
deploy_monitoring() {
    log_info "Deploying monitoring stack..."
    
    # Apply monitoring manifests
    kubectl apply -f "${PROJECT_ROOT}/k8s/monitoring/" 2>&1 | tee -a "${LOG_FILE}"
    
    # Wait for monitoring components
    log_info "Waiting for monitoring components..."
    kubectl wait --for=condition=available --timeout=300s deployment -n ${NAMESPACE_MONITORING} --all 2>&1 | tee -a "${LOG_FILE}" || true
    
    # Check Prometheus
    if kubectl get deployment prometheus -n ${NAMESPACE_MONITORING} &> /dev/null; then
        log_success "Prometheus deployed successfully"
    else
        log_warning "Prometheus deployment not found"
    fi
    
    # Check Grafana
    if kubectl get deployment grafana -n ${NAMESPACE_MONITORING} &> /dev/null; then
        log_success "Grafana deployed successfully"
    else
        log_warning "Grafana deployment not found"
    fi
    
    log_success "Monitoring stack deployment completed"
}

# Deploy Istio service mesh
deploy_service_mesh() {
    log_info "Deploying Istio service mesh configuration..."
    
    # Check if Istio is ready
    if kubectl get crd gateways.networking.istio.io &> /dev/null; then
        # Apply Istio configurations
        kubectl apply -f "${PROJECT_ROOT}/k8s/istio/" 2>&1 | tee -a "${LOG_FILE}"
        
        # Wait a bit for configurations to be processed
        sleep 10
        
        # Verify gateway
        if kubectl get gateway neo-service-gateway -n ${NAMESPACE_INFRASTRUCTURE} &> /dev/null; then
            log_success "Istio gateway configured"
        else
            log_warning "Istio gateway not found"
        fi
        
        log_success "Service mesh configuration completed"
    else
        log_warning "Istio CRDs not found. Service mesh configuration skipped."
    fi
}

# Deploy individual service
deploy_service() {
    local service_name=$1
    
    log_info "Deploying ${service_name} service..."
    
    # Check if service directory exists
    local service_manifest_dir="${PROJECT_ROOT}/k8s/services/${service_name}"
    if [ ! -d "${service_manifest_dir}" ]; then
        # Try alternative location
        service_manifest_dir="${PROJECT_ROOT}/extracted_services/${service_name}/k8s"
        if [ ! -d "${service_manifest_dir}" ]; then
            log_warning "Service manifests not found for ${service_name}, skipping..."
            return 0
        fi
    fi
    
    # Apply service manifests
    kubectl apply -f "${service_manifest_dir}/" 2>&1 | tee -a "${LOG_FILE}" || log_warning "Failed to apply ${service_name} manifests"
    
    # Wait for deployment
    if kubectl get deployment "neo-${service_name}-service" -n ${NAMESPACE_SERVICES} &> /dev/null; then
        log_info "Waiting for ${service_name} service deployment..."
        kubectl wait --for=condition=available --timeout=300s deployment "neo-${service_name}-service" -n ${NAMESPACE_SERVICES} 2>&1 | tee -a "${LOG_FILE}" || log_warning "${service_name} deployment timeout"
        
        # Check pod status
        local ready_pods
        ready_pods=$(kubectl get pods -n ${NAMESPACE_SERVICES} -l app=neo-${service_name}-service --field-selector=status.phase=Running --no-headers | wc -l)
        log_info "${service_name} service: ${ready_pods} pods running"
        
        if [ "${ready_pods}" -gt 0 ]; then
            log_success "${service_name} service deployed successfully"
        else
            log_error "${service_name} service deployment failed - no running pods"
            return 1
        fi
    else
        log_warning "Deployment for ${service_name} service not found"
        return 1
    fi
}

# Deploy all services in order
deploy_services() {
    log_info "Deploying microservices in dependency order..."
    
    local failed_services=()
    
    for service in "${DEPLOYMENT_ORDER[@]}"; do
        case ${service} in
            "infrastructure")
                deploy_infrastructure
                ;;
            "databases")
                # Already handled in infrastructure
                log_info "Database components deployed with infrastructure"
                ;;
            "monitoring")
                deploy_monitoring
                deploy_service_mesh
                ;;
            *)
                if ! deploy_service "${service}"; then
                    failed_services+=("${service}")
                fi
                ;;
        esac
        
        # Brief pause between deployments
        sleep 5
    done
    
    if [ ${#failed_services[@]} -gt 0 ]; then
        log_error "Failed to deploy services: ${failed_services[*]}"
        return 1
    fi
    
    log_success "All services deployed successfully"
}

# Verify deployment health
verify_deployment() {
    log_info "Verifying deployment health..."
    
    # Check all namespaces
    log_info "Namespace status:"
    kubectl get ns | grep neo- | tee -a "${LOG_FILE}"
    
    # Check all pods
    log_info "Pod status across all Neo namespaces:"
    kubectl get pods --all-namespaces | grep -E "(neo-|istio-)" | tee -a "${LOG_FILE}"
    
    # Check services
    log_info "Service status:"
    kubectl get svc -n ${NAMESPACE_SERVICES} | tee -a "${LOG_FILE}"
    
    # Check ingress/gateway
    if kubectl get gateway neo-service-gateway -n ${NAMESPACE_INFRASTRUCTURE} &> /dev/null; then
        log_info "Gateway configuration:"
        kubectl get gateway -n ${NAMESPACE_INFRASTRUCTURE} | tee -a "${LOG_FILE}"
        kubectl get virtualservice -n ${NAMESPACE_SERVICES} | tee -a "${LOG_FILE}"
    fi
    
    # Health check endpoints
    log_info "Testing health endpoints..."
    local failed_health_checks=()
    
    # Get service IPs for health checks
    for service in auth oracle compute storage; do
        if kubectl get svc "neo-${service}-service" -n ${NAMESPACE_SERVICES} &> /dev/null; then
            local service_ip
            service_ip=$(kubectl get svc "neo-${service}-service" -n ${NAMESPACE_SERVICES} -o jsonpath='{.spec.clusterIP}')
            
            if [ -n "${service_ip}" ] && [ "${service_ip}" != "None" ]; then
                # Test health endpoint (from within cluster)
                if kubectl run health-test-${service} --image=curlimages/curl:latest --rm -i --restart=Never -- \
                    curl -f "http://${service_ip}/health" --max-time 10 &> /dev/null; then
                    log_success "${service} service health check passed"
                else
                    failed_health_checks+=("${service}")
                    log_warning "${service} service health check failed"
                fi
            fi
        fi
    done
    
    if [ ${#failed_health_checks[@]} -gt 0 ]; then
        log_warning "Failed health checks for: ${failed_health_checks[*]}"
    fi
    
    log_success "Deployment verification completed"
}

# Configure traffic routing
configure_traffic_routing() {
    log_info "Configuring traffic routing for gradual migration..."
    
    # Check if we have the original monolith service
    if kubectl get svc neo-service-layer -n ${NAMESPACE_SERVICES} &> /dev/null; then
        log_info "Monolith service found - configuring gradual traffic migration"
        
        # Create traffic splitting configuration
        cat > /tmp/traffic-split.yaml << EOF
apiVersion: networking.istio.io/v1beta1
kind: VirtualService
metadata:
  name: neo-traffic-split
  namespace: ${NAMESPACE_SERVICES}
spec:
  hosts:
  - api.neo-service-layer.com
  gateways:
  - ${NAMESPACE_INFRASTRUCTURE}/neo-service-gateway
  http:
  # Auth service - 100% to microservice
  - match:
    - uri:
        prefix: "/api/auth"
    route:
    - destination:
        host: neo-auth-service
      weight: 100
  
  # Oracle service - 50% split for testing
  - match:
    - uri:
        prefix: "/api/oracle"
    route:
    - destination:
        host: neo-oracle-service
      weight: 50
    - destination:
        host: neo-service-layer
      weight: 50
  
  # Default route - monolith for now
  - route:
    - destination:
        host: neo-service-layer
      weight: 100
EOF
        
        kubectl apply -f /tmp/traffic-split.yaml 2>&1 | tee -a "${LOG_FILE}"
        rm -f /tmp/traffic-split.yaml
        
        log_success "Traffic splitting configured"
    else
        log_info "No monolith service found - routing directly to microservices"
    fi
}

# Create deployment summary
create_deployment_summary() {
    log_info "Creating deployment summary..."
    
    local summary_file="${PROJECT_ROOT}/deployment-summary-$(date +%Y%m%d_%H%M%S).md"
    
    cat > "${summary_file}" << EOF
# Neo Service Layer - Deployment Summary

**Deployment Date:** $(date)
**Cluster:** $(kubectl config current-context)

## Deployment Status

### Infrastructure Components
EOF
    
    # Check infrastructure components
    kubectl get all -n ${NAMESPACE_INFRASTRUCTURE} >> "${summary_file}" 2>/dev/null || echo "No infrastructure components found" >> "${summary_file}"
    
    cat >> "${summary_file}" << EOF

### Database Components
EOF
    
    kubectl get all -n ${NAMESPACE_DATABASES} >> "${summary_file}" 2>/dev/null || echo "No database components found" >> "${summary_file}"
    
    cat >> "${summary_file}" << EOF

### Monitoring Components
EOF
    
    kubectl get all -n ${NAMESPACE_MONITORING} >> "${summary_file}" 2>/dev/null || echo "No monitoring components found" >> "${summary_file}"
    
    cat >> "${summary_file}" << EOF

### Microservices
EOF
    
    kubectl get all -n ${NAMESPACE_SERVICES} >> "${summary_file}" 2>/dev/null || echo "No services found" >> "${summary_file}"
    
    cat >> "${summary_file}" << EOF

### Service Mesh Configuration
EOF
    
    if kubectl get gateway -n ${NAMESPACE_INFRASTRUCTURE} &> /dev/null; then
        kubectl get gateway,virtualservice,destinationrule --all-namespaces | grep neo >> "${summary_file}"
    else
        echo "No Istio configuration found" >> "${summary_file}"
    fi
    
    cat >> "${summary_file}" << EOF

## Access Information

### Internal Services
- Auth Service: http://neo-auth-service.${NAMESPACE_SERVICES}.svc.cluster.local
- Oracle Service: http://neo-oracle-service.${NAMESPACE_SERVICES}.svc.cluster.local
- Compute Service: http://neo-compute-service.${NAMESPACE_SERVICES}.svc.cluster.local

### Monitoring
- Prometheus: http://neo-prometheus.${NAMESPACE_MONITORING}.svc.cluster.local:9090
- Grafana: http://grafana.${NAMESPACE_MONITORING}.svc.cluster.local:3000
- Jaeger: http://jaeger-query.${NAMESPACE_MONITORING}.svc.cluster.local:16686

### External Access
- API Gateway: https://api.neo-service-layer.com
- Auth Service: https://auth.neo-service-layer.com
- Monitoring: https://monitoring.neo-service-layer.com

## Next Steps

1. **Verify service functionality** through health checks and API testing
2. **Monitor service performance** using Grafana dashboards
3. **Gradually increase traffic** to microservices using traffic splitting
4. **Implement comprehensive testing** for all service interactions
5. **Scale services** based on load and performance metrics
6. **Update client applications** to use new service endpoints
7. **Decommission monolith** after successful migration validation

## Troubleshooting

### Common Issues
- Pod startup failures: Check resource limits and database connectivity
- Service discovery issues: Verify DNS and service mesh configuration
- Authentication failures: Check JWT token configuration and service certificates
- Performance issues: Monitor resource usage and adjust scaling policies

### Useful Commands
\`\`\`bash
# Check service status
kubectl get pods -n ${NAMESPACE_SERVICES}

# View service logs
kubectl logs -f deployment/neo-auth-service -n ${NAMESPACE_SERVICES}

# Check service mesh status
kubectl get gateway,virtualservice,destinationrule --all-namespaces

# Test service connectivity
kubectl exec -it <pod-name> -n ${NAMESPACE_SERVICES} -- curl http://neo-auth-service/health
\`\`\`

## Files and Logs
- Deployment Log: ${LOG_FILE}
- Summary Report: ${summary_file}
EOF
    
    log_success "Deployment summary created: ${summary_file}"
}

# Rollback function
rollback_deployment() {
    log_error "Performing emergency rollback..."
    
    # Delete problematic services
    kubectl delete -f "${PROJECT_ROOT}/k8s/services/" --ignore-not-found=true 2>&1 | tee -a "${LOG_FILE}"
    
    # Keep infrastructure and monitoring
    log_info "Infrastructure and monitoring components preserved"
    
    # Restore monolith routing if needed
    if kubectl get svc neo-service-layer -n ${NAMESPACE_SERVICES} &> /dev/null; then
        cat > /tmp/rollback-routing.yaml << EOF
apiVersion: networking.istio.io/v1beta1
kind: VirtualService
metadata:
  name: neo-rollback-routing
  namespace: ${NAMESPACE_SERVICES}
spec:
  hosts:
  - api.neo-service-layer.com
  gateways:
  - ${NAMESPACE_INFRASTRUCTURE}/neo-service-gateway
  http:
  - route:
    - destination:
        host: neo-service-layer
      weight: 100
EOF
        
        kubectl apply -f /tmp/rollback-routing.yaml
        rm -f /tmp/rollback-routing.yaml
        
        log_info "Traffic routing restored to monolith"
    fi
    
    log_success "Rollback completed"
}

# Main execution
main() {
    log_info "Starting Neo Service Layer Deployment Migration"
    log_info "============================================="
    
    # Set error handling for rollback
    trap 'rollback_deployment; exit 1' ERR
    
    check_prerequisites
    deploy_services
    configure_traffic_routing
    verify_deployment
    create_deployment_summary
    
    log_success "Deployment migration completed successfully!"
    log_info "Check the deployment summary for access information and next steps"
    log_info "Log file: ${LOG_FILE}"
}

# Handle script interruption
trap 'log_error "Deployment interrupted by user"; rollback_deployment; exit 130' INT

# Execute main function if script is run directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
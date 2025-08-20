#!/bin/bash

# Operational Runbooks for Neo Service Layer
# Automated operational procedures and incident response

set -e

# Configuration
NAMESPACE="${NAMESPACE:-neo-service-layer}"
MONITORING_NAMESPACE="${MONITORING_NAMESPACE:-monitoring}"
ALERT_WEBHOOK="${ALERT_WEBHOOK:-}"
LOG_LEVEL="${LOG_LEVEL:-INFO}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging function
log() {
    local level=$1
    shift
    local message="$@"
    local timestamp=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    
    case $level in
        ERROR)   echo -e "${RED}[${timestamp}] ERROR: ${message}${NC}" ;;
        WARN)    echo -e "${YELLOW}[${timestamp}] WARN: ${message}${NC}" ;;
        INFO)    echo -e "${GREEN}[${timestamp}] INFO: ${message}${NC}" ;;
        DEBUG)   echo -e "${BLUE}[${timestamp}] DEBUG: ${message}${NC}" ;;
    esac
    
    # Send to external logging if configured
    if [[ -n "$ALERT_WEBHOOK" ]] && [[ "$level" == "ERROR" ]]; then
        curl -s -X POST "$ALERT_WEBHOOK" \
            -H "Content-Type: application/json" \
            -d "{\"level\":\"$level\",\"message\":\"$message\",\"timestamp\":\"$timestamp\",\"service\":\"neo-service-layer\"}" \
            >/dev/null 2>&1 || true
    fi
}

# Function to check system health
health_check() {
    log INFO "🏥 Performing comprehensive health check"
    
    local issues=0
    
    # Check namespace exists
    if ! kubectl get namespace $NAMESPACE >/dev/null 2>&1; then
        log ERROR "Namespace $NAMESPACE does not exist"
        ((issues++))
    fi
    
    # Check pod status
    local unhealthy_pods=$(kubectl get pods -n $NAMESPACE --field-selector=status.phase!=Running -o name 2>/dev/null | wc -l)
    if [[ $unhealthy_pods -gt 0 ]]; then
        log WARN "$unhealthy_pods pods are not running"
        kubectl get pods -n $NAMESPACE --field-selector=status.phase!=Running
        ((issues++))
    fi
    
    # Check service endpoints
    local services=("api-gateway" "oracle-service" "crosschain-service")
    for service in "${services[@]}"; do
        local endpoints=$(kubectl get endpoints $service -n $NAMESPACE -o jsonpath='{.subsets[*].addresses[*].ip}' 2>/dev/null | wc -w)
        if [[ $endpoints -eq 0 ]]; then
            log ERROR "Service $service has no healthy endpoints"
            ((issues++))
        else
            log INFO "Service $service has $endpoints healthy endpoints"
        fi
    done
    
    # Check resource usage
    local high_cpu_pods=$(kubectl top pods -n $NAMESPACE --no-headers 2>/dev/null | awk '$3 > 80 {print $1}' | wc -l)
    if [[ $high_cpu_pods -gt 0 ]]; then
        log WARN "$high_cpu_pods pods have high CPU usage (>80%)"
        kubectl top pods -n $NAMESPACE --no-headers | awk '$3 > 80'
    fi
    
    local high_memory_pods=$(kubectl top pods -n $NAMESPACE --no-headers 2>/dev/null | awk '$4 > 1000 {print $1}' | wc -l)
    if [[ $high_memory_pods -gt 0 ]]; then
        log WARN "$high_memory_pods pods have high memory usage (>1000Mi)"
        kubectl top pods -n $NAMESPACE --no-headers | awk '$4 > 1000'
    fi
    
    # Check persistent volume claims
    local pvc_issues=$(kubectl get pvc -n $NAMESPACE 2>/dev/null | grep -v Bound | grep -v NAME | wc -l)
    if [[ $pvc_issues -gt 0 ]]; then
        log ERROR "$pvc_issues PVCs are not bound"
        kubectl get pvc -n $NAMESPACE | grep -v Bound
        ((issues++))
    fi
    
    # Summary
    if [[ $issues -eq 0 ]]; then
        log INFO "✅ All health checks passed"
        return 0
    else
        log ERROR "❌ Health check failed with $issues issues"
        return 1
    fi
}

# Function to restart unhealthy pods
restart_unhealthy_pods() {
    log INFO "🔄 Restarting unhealthy pods"
    
    # Get pods that are not running or ready
    local unhealthy_pods=$(kubectl get pods -n $NAMESPACE --field-selector=status.phase!=Running -o name 2>/dev/null)
    
    if [[ -z "$unhealthy_pods" ]]; then
        # Check for pods with restart counts > 5
        unhealthy_pods=$(kubectl get pods -n $NAMESPACE -o json | jq -r '.items[] | select(.status.containerStatuses[]?.restartCount > 5) | .metadata.name' 2>/dev/null)
    fi
    
    if [[ -n "$unhealthy_pods" ]]; then
        for pod in $unhealthy_pods; do
            log INFO "Restarting pod: $pod"
            kubectl delete pod "$pod" -n $NAMESPACE --force --grace-period=0
        done
        
        # Wait for pods to be recreated
        log INFO "Waiting for pods to be recreated..."
        sleep 30
        kubectl wait --for=condition=ready pod -l app=api-gateway -n $NAMESPACE --timeout=300s
    else
        log INFO "No unhealthy pods found"
    fi
}

# Function to scale services
scale_service() {
    local service=$1
    local replicas=$2
    
    log INFO "📈 Scaling service $service to $replicas replicas"
    
    if kubectl get deployment $service -n $NAMESPACE >/dev/null 2>&1; then
        kubectl scale deployment $service --replicas=$replicas -n $NAMESPACE
        kubectl rollout status deployment/$service -n $NAMESPACE --timeout=300s
        log INFO "✅ Service $service scaled successfully"
    elif kubectl get rollout $service-rollout -n $NAMESPACE >/dev/null 2>&1; then
        kubectl argo rollouts set replica-count $service-rollout $replicas -n $NAMESPACE
        kubectl argo rollouts wait $service-rollout -n $NAMESPACE --timeout=300s
        log INFO "✅ Rollout $service-rollout scaled successfully"
    else
        log ERROR "Service $service not found"
        return 1
    fi
}

# Function to handle high traffic
handle_high_traffic() {
    log INFO "🚀 Handling high traffic scenario"
    
    # Auto-scale API Gateway
    scale_service "api-gateway" 10
    
    # Auto-scale backend services
    scale_service "oracle-service" 6
    scale_service "crosschain-service" 4
    
    # Enable request throttling
    log INFO "Enabling request throttling"
    kubectl patch envoyfilter rate-limit-filter -n $NAMESPACE --type='merge' -p='{
        "spec": {
            "configPatches": [{
                "applyTo": "HTTP_FILTER",
                "match": {
                    "context": "SIDECAR_INBOUND",
                    "listener": {
                        "filterChain": {
                            "filter": {
                                "name": "envoy.filters.network.http_connection_manager"
                            }
                        }
                    }
                },
                "patch": {
                    "operation": "INSERT_BEFORE",
                    "value": {
                        "name": "envoy.filters.http.local_ratelimit",
                        "typed_config": {
                            "@type": "type.googleapis.com/udpa.type.v1.TypedStruct",
                            "type_url": "type.googleapis.com/envoy.extensions.filters.http.local_ratelimit.v3.LocalRateLimit",
                            "value": {
                                "stat_prefix": "neo_service_rate_limiter_emergency",
                                "token_bucket": {
                                    "max_tokens": 200,
                                    "tokens_per_fill": 200,
                                    "fill_interval": "60s"
                                }
                            }
                        }
                    }
                }
            }]
        }
    }'
    
    log INFO "✅ High traffic mitigation applied"
}

# Function to handle database issues
handle_database_issues() {
    log INFO "🗄️  Handling database connectivity issues"
    
    # Check database connectivity
    local db_pod=$(kubectl get pods -n database -l app=postgresql -o name | head -1)
    if [[ -z "$db_pod" ]]; then
        log ERROR "Database pod not found"
        return 1
    fi
    
    # Test database connection
    if ! kubectl exec $db_pod -n database -- pg_isready; then
        log ERROR "Database is not accepting connections"
        
        # Restart database pod
        kubectl delete $db_pod -n database --force --grace-period=0
        
        # Wait for database to be ready
        kubectl wait --for=condition=ready pod -l app=postgresql -n database --timeout=300s
    fi
    
    # Clear connection pools in application
    log INFO "Clearing application connection pools"
    kubectl exec -n $NAMESPACE $(kubectl get pods -n $NAMESPACE -l app=api-gateway -o name | head -1) -- \
        curl -X POST http://localhost:8080/admin/clear-connection-pools || true
    
    log INFO "✅ Database issue mitigation completed"
}

# Function to collect diagnostic information
collect_diagnostics() {
    log INFO "🔍 Collecting diagnostic information"
    
    local diag_dir="diagnostics-$(date +%Y%m%d-%H%M%S)"
    mkdir -p $diag_dir
    
    # System information
    log INFO "Collecting system information"
    kubectl cluster-info > $diag_dir/cluster-info.txt
    kubectl get nodes -o wide > $diag_dir/nodes.txt
    kubectl top nodes > $diag_dir/node-usage.txt 2>/dev/null || echo "metrics-server not available" > $diag_dir/node-usage.txt
    
    # Namespace information
    log INFO "Collecting namespace information"
    kubectl get all -n $NAMESPACE -o wide > $diag_dir/namespace-resources.txt
    kubectl describe pods -n $NAMESPACE > $diag_dir/pod-descriptions.txt
    kubectl top pods -n $NAMESPACE > $diag_dir/pod-usage.txt 2>/dev/null || echo "metrics-server not available" > $diag_dir/pod-usage.txt
    
    # Logs
    log INFO "Collecting application logs"
    for pod in $(kubectl get pods -n $NAMESPACE -o name); do
        pod_name=$(basename $pod)
        kubectl logs $pod -n $NAMESPACE --tail=1000 > $diag_dir/logs-$pod_name.txt 2>/dev/null
        kubectl logs $pod -n $NAMESPACE --previous --tail=1000 > $diag_dir/logs-$pod_name-previous.txt 2>/dev/null || true
    done
    
    # Events
    log INFO "Collecting events"
    kubectl get events -n $NAMESPACE --sort-by='.lastTimestamp' > $diag_dir/events.txt
    
    # ConfigMaps and Secrets (metadata only)
    kubectl get configmaps -n $NAMESPACE -o yaml > $diag_dir/configmaps.yaml
    kubectl get secrets -n $NAMESPACE -o custom-columns=NAME:.metadata.name,TYPE:.type,DATA:.data > $diag_dir/secrets-metadata.txt
    
    # Network policies
    kubectl get networkpolicies -n $NAMESPACE -o yaml > $diag_dir/network-policies.yaml
    
    # Istio configuration
    kubectl get destinationrules,virtualservices,gateways -n $NAMESPACE -o yaml > $diag_dir/istio-config.yaml 2>/dev/null || echo "Istio not available" > $diag_dir/istio-config.yaml
    
    # Create tarball
    tar -czf $diag_dir.tar.gz $diag_dir
    rm -rf $diag_dir
    
    log INFO "✅ Diagnostics collected: $diag_dir.tar.gz"
}

# Function to perform emergency shutdown
emergency_shutdown() {
    log WARN "🚨 Performing emergency shutdown"
    
    # Scale down all services
    for deployment in $(kubectl get deployments -n $NAMESPACE -o name); do
        kubectl scale $deployment --replicas=0 -n $NAMESPACE
    done
    
    # Scale down rollouts
    for rollout in $(kubectl get rollouts -n $NAMESPACE -o name 2>/dev/null || true); do
        kubectl argo rollouts set replica-count $rollout 0 -n $NAMESPACE
    done
    
    # Delete all pods forcefully
    kubectl delete pods --all -n $NAMESPACE --force --grace-period=0
    
    log WARN "⛔ Emergency shutdown completed"
}

# Function to perform recovery
recovery_procedure() {
    log INFO "🔧 Starting recovery procedure"
    
    # Step 1: Verify infrastructure
    log INFO "Step 1: Verifying infrastructure"
    if ! health_check; then
        log ERROR "Infrastructure verification failed"
        return 1
    fi
    
    # Step 2: Start core services
    log INFO "Step 2: Starting core services"
    scale_service "api-gateway" 3
    sleep 30
    
    # Step 3: Start supporting services
    log INFO "Step 3: Starting supporting services"
    scale_service "oracle-service" 2
    scale_service "crosschain-service" 2
    sleep 30
    
    # Step 4: Verify services are healthy
    log INFO "Step 4: Verifying service health"
    if ! health_check; then
        log ERROR "Recovery verification failed"
        return 1
    fi
    
    # Step 5: Run smoke tests
    log INFO "Step 5: Running smoke tests"
    run_smoke_tests
    
    log INFO "✅ Recovery procedure completed"
}

# Function to run smoke tests
run_smoke_tests() {
    log INFO "💨 Running smoke tests"
    
    # Get API Gateway endpoint
    local api_endpoint=$(kubectl get svc api-gateway-active -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')
    
    if [[ -z "$api_endpoint" ]]; then
        # Use port-forward for testing
        kubectl port-forward svc/api-gateway-active 8080:80 -n $NAMESPACE &
        local port_forward_pid=$!
        sleep 5
        api_endpoint="localhost:8080"
    fi
    
    # Test health endpoint
    log INFO "Testing health endpoint"
    if curl -sf "http://$api_endpoint/health/live" >/dev/null; then
        log INFO "✅ Health endpoint responsive"
    else
        log ERROR "❌ Health endpoint failed"
        kill $port_forward_pid 2>/dev/null || true
        return 1
    fi
    
    # Test ready endpoint
    log INFO "Testing ready endpoint"
    if curl -sf "http://$api_endpoint/health/ready" >/dev/null; then
        log INFO "✅ Ready endpoint responsive"
    else
        log ERROR "❌ Ready endpoint failed"
        kill $port_forward_pid 2>/dev/null || true
        return 1
    fi
    
    # Test API endpoints
    log INFO "Testing API endpoints"
    if curl -sf "http://$api_endpoint/api/v1/system/info" >/dev/null; then
        log INFO "✅ System info endpoint responsive"
    else
        log WARN "⚠️  System info endpoint may not be available"
    fi
    
    # Cleanup
    kill $port_forward_pid 2>/dev/null || true
    
    log INFO "✅ Smoke tests completed"
}

# Function to monitor and alert
monitoring_alert() {
    local alert_type=$1
    local message=$2
    
    log INFO "📢 Sending alert: $alert_type - $message"
    
    # Send to webhook if configured
    if [[ -n "$ALERT_WEBHOOK" ]]; then
        curl -X POST "$ALERT_WEBHOOK" \
            -H "Content-Type: application/json" \
            -d "{
                \"alert_type\":\"$alert_type\",
                \"message\":\"$message\",
                \"timestamp\":\"$(date -u +"%Y-%m-%dT%H:%M:%SZ")\",
                \"service\":\"neo-service-layer\",
                \"namespace\":\"$NAMESPACE\"
            }"
    fi
    
    # Log to kubectl events
    kubectl create event --action="Alert" --reason="$alert_type" --message="$message" --namespace="$NAMESPACE" || true
}

# Main operational menu
main() {
    case "${1:-help}" in
        health-check)
            health_check
            ;;
        restart-pods)
            restart_unhealthy_pods
            ;;
        scale)
            if [[ $# -lt 3 ]]; then
                log ERROR "Usage: $0 scale <service> <replicas>"
                exit 1
            fi
            scale_service "$2" "$3"
            ;;
        high-traffic)
            handle_high_traffic
            ;;
        database-issues)
            handle_database_issues
            ;;
        diagnostics)
            collect_diagnostics
            ;;
        emergency-shutdown)
            emergency_shutdown
            ;;
        recovery)
            recovery_procedure
            ;;
        smoke-tests)
            run_smoke_tests
            ;;
        monitor)
            # Continuous monitoring mode
            log INFO "Starting continuous monitoring mode"
            while true; do
                if ! health_check; then
                    monitoring_alert "HealthCheckFailed" "Health check failed in namespace $NAMESPACE"
                    
                    # Attempt automatic recovery
                    log INFO "Attempting automatic recovery"
                    restart_unhealthy_pods
                    sleep 60
                    
                    if ! health_check; then
                        monitoring_alert "AutoRecoveryFailed" "Automatic recovery failed, manual intervention required"
                    fi
                fi
                sleep 300  # Check every 5 minutes
            done
            ;;
        help|*)
            echo "Neo Service Layer Operational Runbooks"
            echo ""
            echo "Usage: $0 <command> [options]"
            echo ""
            echo "Commands:"
            echo "  health-check          Perform comprehensive health check"
            echo "  restart-pods          Restart unhealthy pods"
            echo "  scale <service> <n>   Scale service to n replicas"
            echo "  high-traffic          Handle high traffic scenario"
            echo "  database-issues       Handle database connectivity issues"
            echo "  diagnostics           Collect diagnostic information"
            echo "  emergency-shutdown    Perform emergency shutdown"
            echo "  recovery              Perform recovery procedure"
            echo "  smoke-tests           Run smoke tests"
            echo "  monitor               Start continuous monitoring"
            echo "  help                  Show this help"
            echo ""
            echo "Environment Variables:"
            echo "  NAMESPACE            Kubernetes namespace (default: neo-service-layer)"
            echo "  ALERT_WEBHOOK        Webhook URL for alerts"
            echo "  LOG_LEVEL            Log level: DEBUG|INFO|WARN|ERROR (default: INFO)"
            ;;
    esac
}

# Execute main function with all arguments
main "$@"
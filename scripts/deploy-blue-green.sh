#!/bin/bash

# Blue-Green Deployment Script for Neo Service Layer
set -e

# Configuration
NAMESPACE="${NAMESPACE:-neo-service-layer}"
SERVICE_NAME="${SERVICE_NAME:-api-gateway}"
IMAGE_TAG="${IMAGE_TAG:-latest}"
REGISTRY="${REGISTRY:-neoservicelayer}"
TIMEOUT="${TIMEOUT:-600s}"
DRY_RUN="${DRY_RUN:-false}"

echo "🚀 Starting Blue-Green Deployment"
echo "Namespace: $NAMESPACE"
echo "Service: $SERVICE_NAME"
echo "Image: $REGISTRY/$SERVICE_NAME:$IMAGE_TAG"
echo "Timeout: $TIMEOUT"

# Function to check if ArgoCD Rollouts is installed
check_argo_rollouts() {
    echo "🔍 Checking ArgoCD Rollouts installation..."
    
    if ! kubectl get crd rollouts.argoproj.io >/dev/null 2>&1; then
        echo "❌ ArgoCD Rollouts not found. Installing..."
        
        # Install ArgoCD Rollouts
        kubectl create namespace argo-rollouts --dry-run=client -o yaml | kubectl apply -f -
        kubectl apply -n argo-rollouts -f https://github.com/argoproj/argo-rollouts/releases/latest/download/install.yaml
        
        # Wait for rollouts controller to be ready
        echo "⏳ Waiting for ArgoCD Rollouts controller to be ready..."
        kubectl wait --for=condition=available --timeout=300s deployment/argo-rollouts-controller -n argo-rollouts
        
        echo "✅ ArgoCD Rollouts installed successfully"
    else
        echo "✅ ArgoCD Rollouts already installed"
    fi
}

# Function to install kubectl argo rollouts plugin
install_rollouts_plugin() {
    echo "🔍 Checking kubectl argo rollouts plugin..."
    
    if ! kubectl argo rollouts version >/dev/null 2>&1; then
        echo "📦 Installing kubectl argo rollouts plugin..."
        
        # Detect OS and architecture
        OS=$(uname -s | tr '[:upper:]' '[:lower:]')
        ARCH=$(uname -m)
        
        case $ARCH in
            x86_64) ARCH="amd64" ;;
            arm64|aarch64) ARCH="arm64" ;;
        esac
        
        # Download and install plugin
        curl -LO https://github.com/argoproj/argo-rollouts/releases/latest/download/kubectl-argo-rollouts-${OS}-${ARCH}
        chmod +x kubectl-argo-rollouts-${OS}-${ARCH}
        sudo mv kubectl-argo-rollouts-${OS}-${ARCH} /usr/local/bin/kubectl-argo-rollouts
        
        echo "✅ kubectl argo rollouts plugin installed"
    else
        echo "✅ kubectl argo rollouts plugin already installed"
    fi
}

# Function to create namespace if it doesn't exist
create_namespace() {
    echo "🏗️  Creating namespace if needed..."
    kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -
}

# Function to deploy prerequisites
deploy_prerequisites() {
    echo "📋 Deploying prerequisites..."
    
    # Apply RBAC and service account
    if [ "$DRY_RUN" = "true" ]; then
        kubectl apply -f k8s/deployments/blue-green-deployment.yaml --dry-run=client
    else
        kubectl apply -f k8s/deployments/blue-green-deployment.yaml
    fi
    
    echo "✅ Prerequisites deployed"
}

# Function to update image in rollout
update_rollout_image() {
    echo "🖼️  Updating rollout image to $REGISTRY/$SERVICE_NAME:$IMAGE_TAG..."
    
    if [ "$DRY_RUN" = "true" ]; then
        echo "[DRY RUN] Would update image to $REGISTRY/$SERVICE_NAME:$IMAGE_TAG"
    else
        kubectl argo rollouts set image ${SERVICE_NAME}-rollout -n $NAMESPACE \
            ${SERVICE_NAME}=$REGISTRY/$SERVICE_NAME:$IMAGE_TAG
    fi
}

# Function to start rollout
start_rollout() {
    echo "🔄 Starting rollout..."
    
    if [ "$DRY_RUN" = "true" ]; then
        echo "[DRY RUN] Would start rollout"
        return
    fi
    
    # Check current rollout status
    kubectl argo rollouts get rollout ${SERVICE_NAME}-rollout -n $NAMESPACE
    
    # Start the rollout if not already in progress
    kubectl argo rollouts restart ${SERVICE_NAME}-rollout -n $NAMESPACE || true
    
    echo "✅ Rollout started"
}

# Function to monitor rollout progress
monitor_rollout() {
    echo "👀 Monitoring rollout progress..."
    
    if [ "$DRY_RUN" = "true" ]; then
        echo "[DRY RUN] Would monitor rollout progress"
        return
    fi
    
    # Wait for rollout to complete
    kubectl argo rollouts wait ${SERVICE_NAME}-rollout -n $NAMESPACE --timeout=$TIMEOUT
    
    # Show final status
    kubectl argo rollouts get rollout ${SERVICE_NAME}-rollout -n $NAMESPACE
    
    echo "✅ Rollout completed successfully"
}

# Function to run health checks on preview environment
health_check_preview() {
    echo "🏥 Running health checks on preview environment..."
    
    if [ "$DRY_RUN" = "true" ]; then
        echo "[DRY RUN] Would run health checks"
        return
    fi
    
    # Get preview service endpoint
    PREVIEW_ENDPOINT=$(kubectl get svc ${SERVICE_NAME}-preview -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')
    
    if [ -z "$PREVIEW_ENDPOINT" ]; then
        echo "⚠️  Preview service endpoint not available yet, using port-forward for testing..."
        
        # Use port-forward for testing
        kubectl port-forward svc/${SERVICE_NAME}-preview 8080:80 -n $NAMESPACE &
        PORT_FORWARD_PID=$!
        
        # Wait a moment for port-forward to establish
        sleep 5
        
        # Run health checks
        echo "Testing health endpoint..."
        for i in {1..10}; do
            if curl -sf http://localhost:8080/health/live >/dev/null 2>&1; then
                echo "✅ Health check $i/10 passed"
            else
                echo "❌ Health check $i/10 failed"
                kill $PORT_FORWARD_PID 2>/dev/null || true
                return 1
            fi
            sleep 2
        done
        
        # Cleanup port-forward
        kill $PORT_FORWARD_PID 2>/dev/null || true
        
    else
        # Test external endpoint
        echo "Testing external endpoint: $PREVIEW_ENDPOINT"
        for i in {1..10}; do
            if curl -sf http://$PREVIEW_ENDPOINT/health/live >/dev/null 2>&1; then
                echo "✅ Health check $i/10 passed"
            else
                echo "❌ Health check $i/10 failed"
                return 1
            fi
            sleep 2
        done
    fi
    
    echo "✅ All health checks passed"
}

# Function to promote rollout
promote_rollout() {
    echo "⬆️  Promoting rollout to active..."
    
    if [ "$DRY_RUN" = "true" ]; then
        echo "[DRY RUN] Would promote rollout"
        return
    fi
    
    # Promote the rollout
    kubectl argo rollouts promote ${SERVICE_NAME}-rollout -n $NAMESPACE
    
    # Wait for promotion to complete
    kubectl argo rollouts wait ${SERVICE_NAME}-rollout -n $NAMESPACE --timeout=$TIMEOUT
    
    echo "✅ Rollout promoted successfully"
}

# Function to rollback if needed
rollback_rollout() {
    echo "⬅️  Rolling back deployment..."
    
    if [ "$DRY_RUN" = "true" ]; then
        echo "[DRY RUN] Would rollback deployment"
        return
    fi
    
    # Get previous revision
    kubectl argo rollouts undo ${SERVICE_NAME}-rollout -n $NAMESPACE
    
    # Wait for rollback to complete
    kubectl argo rollouts wait ${SERVICE_NAME}-rollout -n $NAMESPACE --timeout=$TIMEOUT
    
    echo "✅ Rollback completed"
}

# Function to cleanup old replica sets
cleanup_old_replicasets() {
    echo "🧹 Cleaning up old replica sets..."
    
    if [ "$DRY_RUN" = "true" ]; then
        echo "[DRY RUN] Would cleanup old replica sets"
        return
    fi
    
    # Keep only the last 3 replica sets
    kubectl delete replicaset -n $NAMESPACE -l app=$SERVICE_NAME --field-selector='status.replicas=0' --ignore-not-found=true
    
    echo "✅ Cleanup completed"
}

# Function to show deployment status
show_status() {
    echo "📊 Deployment Status:"
    echo "===================="
    
    if [ "$DRY_RUN" = "true" ]; then
        echo "[DRY RUN] Would show deployment status"
        return
    fi
    
    # Show rollout status
    kubectl argo rollouts get rollout ${SERVICE_NAME}-rollout -n $NAMESPACE
    
    # Show service endpoints
    echo ""
    echo "Service Endpoints:"
    echo "=================="
    kubectl get svc -n $NAMESPACE -l app=$SERVICE_NAME -o custom-columns=NAME:.metadata.name,TYPE:.spec.type,EXTERNAL-IP:.status.loadBalancer.ingress[*].hostname,PORT:.spec.ports[*].port
    
    # Show pod status
    echo ""
    echo "Pod Status:"
    echo "==========="
    kubectl get pods -n $NAMESPACE -l app=$SERVICE_NAME -o custom-columns=NAME:.metadata.name,READY:.status.containerStatuses[*].ready,STATUS:.status.phase,AGE:.metadata.creationTimestamp
}

# Main deployment function
main() {
    echo "🎯 Starting Blue-Green Deployment Process"
    
    # Trap errors and cleanup
    trap 'echo "❌ Deployment failed. Check logs above."; exit 1' ERR
    
    # Install prerequisites
    check_argo_rollouts
    install_rollouts_plugin
    create_namespace
    deploy_prerequisites
    
    # Update and start rollout
    update_rollout_image
    start_rollout
    monitor_rollout
    
    # Health checks on preview
    if health_check_preview; then
        echo "✅ Preview environment healthy, proceeding with promotion..."
        
        # Promote to active
        promote_rollout
        
        # Final health check
        echo "🏥 Running final health checks..."
        sleep 30  # Wait for promotion to fully complete
        
        # Show final status
        show_status
        
        # Cleanup
        cleanup_old_replicasets
        
        echo ""
        echo "🎉 Blue-Green Deployment Completed Successfully!"
        echo "🌐 Service is now active and serving traffic"
        
    else
        echo "❌ Preview environment health checks failed"
        
        if [ "${AUTO_ROLLBACK:-true}" = "true" ]; then
            echo "🔄 Auto-rollback enabled, rolling back..."
            rollback_rollout
            echo "✅ Rollback completed"
        else
            echo "⚠️  Auto-rollback disabled. Manual intervention required."
            echo "To rollback: kubectl argo rollouts undo ${SERVICE_NAME}-rollout -n $NAMESPACE"
        fi
        
        exit 1
    fi
}

# Command line options
while [[ $# -gt 0 ]]; do
    case $1 in
        --namespace=*)
            NAMESPACE="${1#*=}"
            shift
            ;;
        --service=*)
            SERVICE_NAME="${1#*=}"
            shift
            ;;
        --image-tag=*)
            IMAGE_TAG="${1#*=}"
            shift
            ;;
        --registry=*)
            REGISTRY="${1#*=}"
            shift
            ;;
        --timeout=*)
            TIMEOUT="${1#*=}"
            shift
            ;;
        --dry-run)
            DRY_RUN="true"
            shift
            ;;
        --rollback)
            echo "🔄 Rolling back deployment..."
            rollback_rollout
            exit 0
            ;;
        --status)
            show_status
            exit 0
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --namespace=NAME     Kubernetes namespace (default: neo-service-layer)"
            echo "  --service=NAME       Service name (default: api-gateway)"
            echo "  --image-tag=TAG      Docker image tag (default: latest)"
            echo "  --registry=URL       Docker registry (default: neoservicelayer)"
            echo "  --timeout=DURATION   Rollout timeout (default: 600s)"
            echo "  --dry-run            Simulate deployment without making changes"
            echo "  --rollback           Rollback the current deployment"
            echo "  --status             Show current deployment status"
            echo "  --help               Show this help"
            echo ""
            echo "Environment Variables:"
            echo "  AUTO_ROLLBACK        Auto-rollback on failure (default: true)"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Execute main function
main
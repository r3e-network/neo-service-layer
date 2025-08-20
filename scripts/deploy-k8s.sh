#!/bin/bash

# Kubernetes Deployment Script for Neo Service Layer
set -e

NAMESPACE="neo-service-layer"
ENVIRONMENT="${1:-production}"

echo "🚀 Starting Neo Service Layer Kubernetes Deployment"
echo "Environment: $ENVIRONMENT"

# Function to check if kubectl is installed
check_kubectl() {
    if ! command -v kubectl &> /dev/null; then
        echo "❌ kubectl is not installed. Please install kubectl first."
        exit 1
    fi
    echo "✅ kubectl is installed"
}

# Function to check cluster connection
check_cluster() {
    if ! kubectl cluster-info &> /dev/null; then
        echo "❌ Cannot connect to Kubernetes cluster. Please check your kubeconfig."
        exit 1
    fi
    echo "✅ Connected to Kubernetes cluster"
}

# Function to create namespace
create_namespace() {
    echo "📦 Creating namespace..."
    kubectl apply -f k8s/namespace.yaml
    kubectl wait --for=condition=Active namespace/$NAMESPACE --timeout=30s
    echo "✅ Namespace created"
}

# Function to apply configurations
apply_configs() {
    echo "⚙️ Applying ConfigMaps and Secrets..."
    kubectl apply -f k8s/configmap.yaml
    
    # Check if secrets already exist
    if kubectl get secret neo-service-secrets -n $NAMESPACE &> /dev/null; then
        echo "⚠️ Secrets already exist. Skipping secret creation."
        echo "   To update secrets, delete the existing secret first:"
        echo "   kubectl delete secret neo-service-secrets -n $NAMESPACE"
    else
        kubectl apply -f k8s/secret.yaml
        echo "✅ Secrets created. Remember to update them with actual values!"
    fi
}

# Function to deploy infrastructure services
deploy_infrastructure() {
    echo "🗄️ Deploying infrastructure services..."
    
    # Deploy PostgreSQL
    echo "  - Deploying PostgreSQL..."
    kubectl apply -f k8s/postgres-deployment.yaml
    
    # Deploy Redis
    echo "  - Deploying Redis..."
    kubectl apply -f k8s/redis-deployment.yaml
    
    # Deploy RabbitMQ
    echo "  - Deploying RabbitMQ..."
    kubectl apply -f k8s/rabbitmq-deployment.yaml
    
    echo "⏳ Waiting for infrastructure services to be ready..."
    kubectl wait --for=condition=available --timeout=300s \
        deployment/postgres deployment/redis deployment/rabbitmq \
        -n $NAMESPACE
    
    echo "✅ Infrastructure services deployed"
}

# Function to deploy application services
deploy_services() {
    echo "🎯 Deploying application services..."
    
    # Deploy API Gateway
    echo "  - Deploying API Gateway..."
    kubectl apply -f k8s/api-gateway-deployment.yaml
    
    # Deploy Oracle Service
    echo "  - Deploying Oracle Service..."
    kubectl apply -f k8s/oracle-service-deployment.yaml
    
    # Deploy CrossChain Service
    echo "  - Deploying CrossChain Service..."
    kubectl apply -f k8s/crosschain-service-deployment.yaml
    
    echo "⏳ Waiting for application services to be ready..."
    kubectl wait --for=condition=available --timeout=300s \
        deployment/api-gateway deployment/oracle-service deployment/crosschain-service \
        -n $NAMESPACE
    
    echo "✅ Application services deployed"
}

# Function to apply network policies
apply_network_policies() {
    echo "🔒 Applying network policies..."
    kubectl apply -f k8s/network-policy.yaml
    echo "✅ Network policies applied"
}

# Function to setup ingress
setup_ingress() {
    echo "🌐 Setting up ingress..."
    kubectl apply -f k8s/ingress.yaml
    echo "✅ Ingress configured"
}

# Function to check deployment status
check_status() {
    echo ""
    echo "📊 Deployment Status:"
    echo "===================="
    kubectl get all -n $NAMESPACE
    echo ""
    echo "🔗 Services:"
    kubectl get svc -n $NAMESPACE
    echo ""
    echo "🌐 Ingress:"
    kubectl get ingress -n $NAMESPACE
    echo ""
    echo "📈 HPA Status:"
    kubectl get hpa -n $NAMESPACE
}

# Function to get access information
get_access_info() {
    echo ""
    echo "🎉 Deployment Complete!"
    echo "======================"
    
    # Get LoadBalancer IP
    LB_IP=$(kubectl get svc api-gateway-service -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null)
    if [ -z "$LB_IP" ]; then
        LB_IP="<pending>"
    fi
    
    echo "API Gateway LoadBalancer IP: $LB_IP"
    echo ""
    echo "Access your services at:"
    echo "  - API Gateway: http://$LB_IP"
    echo "  - Health Check: http://$LB_IP/health"
    echo ""
    echo "For production with domain:"
    echo "  - https://api.neo-service.io"
    echo "  - https://gateway.neo-service.io"
    echo ""
    echo "📝 Next Steps:"
    echo "  1. Update secrets with production values:"
    echo "     kubectl edit secret neo-service-secrets -n $NAMESPACE"
    echo "  2. Configure DNS to point to LoadBalancer IP"
    echo "  3. Install cert-manager for TLS certificates"
    echo "  4. Monitor services:"
    echo "     kubectl logs -f deployment/api-gateway -n $NAMESPACE"
}

# Main deployment flow
main() {
    check_kubectl
    check_cluster
    create_namespace
    apply_configs
    deploy_infrastructure
    deploy_services
    apply_network_policies
    setup_ingress
    check_status
    get_access_info
}

# Run main function
main
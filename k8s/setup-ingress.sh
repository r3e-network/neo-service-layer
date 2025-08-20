#!/bin/bash
# Setup script for Kubernetes Ingress with TLS

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Check if kubectl is installed
if ! command -v kubectl &> /dev/null; then
    print_error "kubectl is not installed. Please install kubectl first."
    exit 1
fi

# Check if connected to a cluster
if ! kubectl cluster-info &> /dev/null; then
    print_error "Not connected to a Kubernetes cluster. Please configure kubectl."
    exit 1
fi

print_status "Starting Kubernetes Ingress setup for Neo Service Layer..."

# Step 1: Install NGINX Ingress Controller
print_status "Installing NGINX Ingress Controller..."
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.2/deploy/static/provider/cloud/deploy.yaml

# Wait for NGINX to be ready
print_status "Waiting for NGINX Ingress Controller to be ready..."
kubectl wait --namespace ingress-nginx \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller \
  --timeout=300s

# Step 2: Install cert-manager
print_status "Installing cert-manager..."
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.2/cert-manager.yaml

# Wait for cert-manager to be ready
print_status "Waiting for cert-manager to be ready..."
kubectl wait --namespace cert-manager \
  --for=condition=ready pod \
  --selector=app.kubernetes.io/instance=cert-manager \
  --timeout=300s

# Step 3: Create Neo Service Layer namespace if it doesn't exist
print_status "Creating Neo Service Layer namespace..."
kubectl create namespace neo-service-layer --dry-run=client -o yaml | kubectl apply -f -

# Step 4: Apply NGINX configuration
print_status "Applying NGINX configuration..."
kubectl apply -f k8s/nginx/nginx-config.yaml

# Step 5: Apply certificate issuers
print_status "Setting up certificate issuers..."
kubectl apply -f k8s/cert-manager/issuer-production.yaml

# Wait for issuers to be ready
sleep 10

# Step 6: Generate basic auth for monitoring (if not exists)
if ! kubectl get secret monitoring-basic-auth -n neo-service-layer &> /dev/null; then
    print_status "Generating basic auth for monitoring endpoints..."
    
    # Generate random passwords
    ADMIN_PASSWORD=$(openssl rand -base64 32)
    MONITORING_PASSWORD=$(openssl rand -base64 32)
    
    # Generate htpasswd entries
    ADMIN_HASH=$(htpasswd -nbB admin "$ADMIN_PASSWORD" | sed -e s/\\$/\\$\\$/g)
    MONITORING_HASH=$(htpasswd -nbB monitoring "$MONITORING_PASSWORD" | sed -e s/\\$/\\$\\$/g)
    
    # Create secret
    kubectl create secret generic monitoring-basic-auth \
        --from-literal=auth="$ADMIN_HASH
$MONITORING_HASH" \
        -n neo-service-layer
    
    print_status "Basic auth credentials created:"
    echo "  Admin user: admin"
    echo "  Admin password: $ADMIN_PASSWORD"
    echo "  Monitoring user: monitoring"
    echo "  Monitoring password: $MONITORING_PASSWORD"
    echo ""
    print_warning "Please save these credentials securely!"
fi

# Step 7: Apply Ingress resources
print_status "Applying Ingress resources..."
kubectl apply -f k8s/ingress/neo-service-ingress.yaml

# Step 8: Verify setup
print_status "Verifying Ingress setup..."

# Check NGINX controller
if kubectl get pods -n ingress-nginx | grep -q "Running"; then
    print_status "✓ NGINX Ingress Controller is running"
else
    print_error "✗ NGINX Ingress Controller is not running"
fi

# Check cert-manager
if kubectl get pods -n cert-manager | grep -q "Running"; then
    print_status "✓ cert-manager is running"
else
    print_error "✗ cert-manager is not running"
fi

# Check certificate issuers
if kubectl get clusterissuer letsencrypt-prod &> /dev/null; then
    print_status "✓ Let's Encrypt production issuer is configured"
else
    print_error "✗ Let's Encrypt production issuer is not configured"
fi

# Check Ingress resources
if kubectl get ingress -n neo-service-layer &> /dev/null; then
    print_status "✓ Ingress resources are created"
    kubectl get ingress -n neo-service-layer
else
    print_error "✗ Ingress resources are not created"
fi

# Step 9: Get Load Balancer IP
print_status "Getting Load Balancer IP address..."
LB_IP=$(kubectl get svc -n ingress-nginx ingress-nginx-controller -o jsonpath='{.status.loadBalancer.ingress[0].ip}')

if [ -z "$LB_IP" ]; then
    print_warning "Load Balancer IP not yet assigned. Waiting..."
    kubectl get svc -n ingress-nginx ingress-nginx-controller
    print_warning "Run 'kubectl get svc -n ingress-nginx ingress-nginx-controller' to check the external IP"
else
    print_status "Load Balancer IP: $LB_IP"
    echo ""
    print_status "Next steps:"
    echo "1. Configure your DNS to point the following domains to $LB_IP:"
    echo "   - api.neoservicelayer.io"
    echo "   - api-v1.neoservicelayer.io"
    echo "   - api-v2.neoservicelayer.io"
    echo "   - grafana.neoservicelayer.io"
    echo "   - prometheus.neoservicelayer.io"
    echo "   - monitoring.neoservicelayer.io"
    echo "   - ws.neoservicelayer.io"
    echo "   - events.neoservicelayer.io"
    echo ""
    echo "2. Once DNS is configured, certificates will be automatically issued"
    echo "3. Monitor certificate status with:"
    echo "   kubectl get certificates -n neo-service-layer"
fi

# Step 10: Create a test endpoint script
print_status "Creating test script..."
cat > test-ingress.sh << 'EOF'
#!/bin/bash
# Test script for Neo Service Layer Ingress

API_URL="${1:-https://api.neoservicelayer.io}"

echo "Testing Neo Service Layer Ingress at $API_URL"
echo ""

# Test health endpoint
echo "Testing health endpoint..."
curl -k -s -o /dev/null -w "HTTP Status: %{http_code}\n" "$API_URL/health"

# Test with headers
echo ""
echo "Testing security headers..."
curl -k -s -I "$API_URL/health" | grep -E "X-Content-Type-Options|X-Frame-Options|Strict-Transport-Security"

# Test rate limiting
echo ""
echo "Testing rate limiting (making 10 rapid requests)..."
for i in {1..10}; do
    STATUS=$(curl -k -s -o /dev/null -w "%{http_code}" "$API_URL/health")
    echo "Request $i: HTTP $STATUS"
    if [ "$STATUS" = "429" ]; then
        echo "Rate limit triggered successfully!"
        break
    fi
done

# Test SSL/TLS
echo ""
echo "Testing SSL/TLS configuration..."
echo | openssl s_client -connect "${API_URL#https://}:443" -servername "${API_URL#https://}" 2>/dev/null | grep -E "Protocol|Cipher"

echo ""
echo "Ingress test complete!"
EOF

chmod +x test-ingress.sh

print_status "Setup complete!"
print_status "Created test script: ./test-ingress.sh"
print_status "Run './test-ingress.sh' after DNS is configured to test the Ingress"

# Display summary
echo ""
echo "========================================="
echo "     Kubernetes Ingress Setup Summary     "
echo "========================================="
echo "✓ NGINX Ingress Controller installed"
echo "✓ cert-manager installed"
echo "✓ Certificate issuers configured"
echo "✓ Ingress resources created"
echo "✓ Monitoring authentication configured"
echo ""
echo "Ingress endpoints configured:"
echo "- Main API: https://api.neoservicelayer.io"
echo "- API v1: https://api-v1.neoservicelayer.io"
echo "- API v2: https://api-v2.neoservicelayer.io"
echo "- Grafana: https://grafana.neoservicelayer.io"
echo "- Prometheus: https://prometheus.neoservicelayer.io"
echo "- Monitoring: https://monitoring.neoservicelayer.io"
echo "- WebSocket: https://ws.neoservicelayer.io"
echo "- Events: https://events.neoservicelayer.io"
echo "========================================="
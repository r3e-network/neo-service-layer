#!/bin/bash

# Prerequisites verification script for Neo Service Layer deployment

# Note: Don't use 'set -e' here as some checks might return non-zero exit codes
# but still provide useful information

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Counters
PASSED=0
FAILED=0

check_pass() {
    echo -e "${GREEN}✓ $1${NC}"
    ((PASSED++))
}

check_fail() {
    echo -e "${RED}✗ $1${NC}"
    echo "  $2"
    ((FAILED++))
}

check_warn() {
    echo -e "${YELLOW}⚠ $1${NC}"
    echo "  $2"
}

echo "Neo Service Layer - Prerequisites Verification"
echo "============================================="
echo ""

# 1. Check Docker
echo "Checking Docker..."
if command -v docker &> /dev/null; then
    DOCKER_VERSION=$(docker --version | cut -d' ' -f3 | cut -d',' -f1)
    if docker ps &> /dev/null; then
        check_pass "Docker installed and running (version $DOCKER_VERSION)"
    else
        check_fail "Docker not running" "Start Docker daemon: sudo systemctl start docker"
    fi
else
    check_fail "Docker not installed" "Install Docker: https://docs.docker.com/install/"
fi

# 2. Check Docker Compose
echo "Checking Docker Compose..."
if command -v docker compose &> /dev/null; then
    COMPOSE_VERSION=$(docker compose --version | cut -d' ' -f3 | cut -d',' -f1)
    check_pass "Docker Compose installed (version $COMPOSE_VERSION)"
else
    check_fail "Docker Compose not installed" "Install Docker Compose: https://docs.docker.com/compose/install/"
fi

# 3. Check Git
echo "Checking Git..."
if command -v git &> /dev/null; then
    GIT_VERSION=$(git --version | cut -d' ' -f3)
    check_pass "Git installed (version $GIT_VERSION)"
else
    check_fail "Git not installed" "Install Git: sudo apt-get install git"
fi

# 4. Check .NET SDK
echo "Checking .NET SDK..."
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    if [[ "$DOTNET_VERSION" == 8.* ]]; then
        check_pass ".NET 8 SDK installed (version $DOTNET_VERSION)"
    else
        check_warn ".NET version $DOTNET_VERSION" "Recommended: .NET 8.0+ for best compatibility"
    fi
else
    check_fail ".NET SDK not installed" "Install .NET 8: https://dotnet.microsoft.com/download"
fi

# 5. Check OpenSSL
echo "Checking OpenSSL..."
if command -v openssl &> /dev/null; then
    OPENSSL_VERSION=$(openssl version | cut -d' ' -f2)
    check_pass "OpenSSL installed (version $OPENSSL_VERSION)"
else
    check_fail "OpenSSL not installed" "Install OpenSSL: sudo apt-get install openssl"
fi

# 6. Check curl
echo "Checking curl..."
if command -v curl &> /dev/null; then
    check_pass "curl installed"
else
    check_fail "curl not installed" "Install curl: sudo apt-get install curl"
fi

# 7. Check jq
echo "Checking jq..."
if command -v jq &> /dev/null; then
    check_pass "jq installed"
else
    check_warn "jq not installed" "Install jq for JSON parsing: sudo apt-get install jq"
fi

# 8. Check available ports
echo "Checking port availability..."
REQUIRED_PORTS=(80 443 3000 5432 6379 8500 9090)
PORTS_AVAILABLE=true

for port in "${REQUIRED_PORTS[@]}"; do
    if lsof -i :$port &> /dev/null; then
        check_warn "Port $port is in use" "May conflict with Neo Service Layer services"
        PORTS_AVAILABLE=false
    fi
done

if $PORTS_AVAILABLE; then
    check_pass "All required ports available"
fi

# 9. Check disk space
echo "Checking disk space..."
AVAILABLE_SPACE=$(df . | tail -1 | awk '{print $4}')
REQUIRED_SPACE=10485760  # 10GB in KB

if [ "$AVAILABLE_SPACE" -gt "$REQUIRED_SPACE" ]; then
    SPACE_GB=$((AVAILABLE_SPACE / 1024 / 1024))
    check_pass "Sufficient disk space available (${SPACE_GB}GB)"
else
    SPACE_GB=$((AVAILABLE_SPACE / 1024 / 1024))
    check_fail "Insufficient disk space" "Available: ${SPACE_GB}GB, Required: 10GB"
fi

# 10. Check memory
echo "Checking memory..."
AVAILABLE_MEMORY=$(free -m | awk 'NR==2{print $7}')
REQUIRED_MEMORY=4096  # 4GB in MB

if [ "$AVAILABLE_MEMORY" -gt "$REQUIRED_MEMORY" ]; then
    check_pass "Sufficient memory available (${AVAILABLE_MEMORY}MB)"
else
    check_warn "Low available memory" "Available: ${AVAILABLE_MEMORY}MB, Recommended: 4GB+"
fi

# 11. Check SGX support (optional)
echo "Checking Intel SGX support..."
if [ -c /dev/sgx_enclave ] || [ -c /dev/isgx ]; then
    check_pass "Intel SGX device detected"
else
    check_warn "Intel SGX device not detected" "SGX enclaves will use simulation mode"
fi

# 12. Check project files
echo "Checking project structure..."
REQUIRED_FILES=(
    "docker-compose.production.yml"
    "src/Api/NeoServiceLayer.Api/Dockerfile"
    ".env.production.template"
)

PROJECT_COMPLETE=true
for file in "${REQUIRED_FILES[@]}"; do
    if [ -f "$file" ]; then
        check_pass "Found $file"
    else
        check_fail "Missing $file" "Ensure you're in the Neo Service Layer root directory"
        PROJECT_COMPLETE=false
    fi
done

# 13. Check Docker resources
echo "Checking Docker resources..."
if docker system df &> /dev/null; then
    DOCKER_SPACE=$(docker system df --format "table {{.Type}}\t{{.TotalCount}}\t{{.Size}}" | grep "Images" | awk '{print $3}' | sed 's/[^0-9.]*//g')
    check_pass "Docker system accessible"
else
    check_fail "Cannot access Docker system" "Check Docker permissions"
fi

# 14. Check network connectivity
echo "Checking network connectivity..."
if curl -s --max-time 5 https://google.com &> /dev/null; then
    check_pass "Internet connectivity available"
else
    check_fail "No internet connectivity" "Required for downloading Docker images"
fi

# 15. Check DNS resolution
echo "Checking DNS resolution..."
if nslookup docker.io &> /dev/null; then
    check_pass "DNS resolution working"
else
    check_fail "DNS resolution failed" "Check DNS configuration"
fi

# Summary
echo ""
echo "============================================="
echo "Prerequisites Verification Summary"
echo "============================================="
echo -e "Passed: ${GREEN}$PASSED${NC}"
echo -e "Failed: ${RED}$FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ All prerequisites satisfied!${NC}"
    echo "You can proceed with deployment."
    exit 0
else
    echo -e "${RED}✗ $FAILED prerequisites failed${NC}"
    echo "Please fix the issues above before proceeding with deployment."
    echo ""
    echo "Quick fixes:"
    echo "  Ubuntu/Debian: sudo apt-get update && sudo apt-get install docker.io docker compose git curl jq"
    echo "  .NET 8: wget https://dot.net/v1/dotnet-install.sh && chmod +x dotnet-install.sh && ./dotnet-install.sh --channel 8.0"
    echo ""
    exit 1
fi
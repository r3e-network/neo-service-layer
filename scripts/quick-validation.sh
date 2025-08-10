#\!/bin/bash
# Quick validation summary

echo "=== NEO SERVICE LAYER VALIDATION SUMMARY ==="
echo ""

# Project Structure
echo "1. PROJECT STRUCTURE:"
echo "   - Solution projects: $(dotnet sln list | grep -c ".csproj" || echo "0")"
echo "   - Service directories: $(ls -d src/Services/NeoServiceLayer.Services.* 2>/dev/null | wc -l)"
echo "   - AI services: $(ls -d src/AI/NeoServiceLayer.AI.* 2>/dev/null | wc -l)"
echo ""

# Smart Contracts
echo "2. SMART CONTRACTS:"
echo "   - Contract files: $(find contracts-neo-n3/src -name "*.cs" -type f 2>/dev/null | wc -l)"
echo "   - Core contracts: $(find contracts-neo-n3/src/Core -name "*.cs" 2>/dev/null | wc -l)"
echo "   - Production contracts: $(find contracts-neo-n3/src/ProductionReady -name "*.cs" 2>/dev/null | wc -l)"
echo "   - Service contracts: $(find contracts-neo-n3/src/Services -name "*.cs" 2>/dev/null | wc -l)"
echo ""

# Kubernetes
echo "3. KUBERNETES CONFIGURATION:"
echo "   - K8s YAML files: $(find k8s -name "*.yaml" 2>/dev/null | wc -l)"
echo "   - Service configs: $(find k8s/services -name "*.yaml" 2>/dev/null | wc -l)"
echo "   - Base configs: $(find k8s/base -name "*.yaml" 2>/dev/null | wc -l)"
echo ""

# Documentation
echo "4. DOCUMENTATION:"
echo "   - README files: $(find . -name "README.md" -not -path "./node_modules/*" 2>/dev/null | wc -l)"
echo "   - Docs directory: $(find docs -name "*.md" 2>/dev/null | wc -l)"
echo ""

# Scripts
echo "5. AUTOMATION SCRIPTS:"
echo "   - Shell scripts: $(find scripts -name "*.sh" 2>/dev/null | wc -l)"
echo "   - Executable scripts: $(find scripts -name "*.sh" -executable 2>/dev/null | wc -l)"
echo ""

# Docker
echo "6. DOCKER CONFIGURATION:"
for file in docker-compose.yml docker-compose.dev.yml docker-compose.test.yml; do
    if [ -f "$file" ]; then
        echo "   ✓ $file exists"
    else
        echo "   ✗ $file missing"
    fi
done
echo ""

# Critical Files
echo "7. CRITICAL FILES:"
critical_files=(
    "Directory.Packages.props"
    "NeoServiceLayer.sln"
    "README.md"
    ".github/workflows/ci-cd-production.yml"
    "monitoring/prometheus.yml"
)
for file in "${critical_files[@]}"; do
    if [ -f "$file" ]; then
        echo "   ✓ $file"
    else
        echo "   ✗ $file missing"
    fi
done
echo ""

# Build Test
echo "8. BUILD STATUS:"
if dotnet build --no-restore --verbosity quiet > /dev/null 2>&1; then
    echo "   ✓ Solution builds successfully"
else
    echo "   ✗ Build failed"
fi

echo ""
echo "=== END OF VALIDATION SUMMARY ==="

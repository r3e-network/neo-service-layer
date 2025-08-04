#!/bin/bash

# Comprehensive security scanning script for Neo Service Layer
set -e

# Configuration
SCAN_DIR="${1:-.}"
REPORT_DIR="./security-reports/$(date +%Y%m%d-%H%M%S)"
DOCKER_BENCH_VERSION="latest"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Create report directory
mkdir -p "$REPORT_DIR"

echo -e "${YELLOW}Neo Service Layer - Security Scan${NC}"
echo "Scan directory: $SCAN_DIR"
echo "Report directory: $REPORT_DIR"
echo ""

# Helper functions
run_scan() {
    local name=$1
    local command=$2
    
    echo -e "${YELLOW}Running $name...${NC}"
    eval "$command" > "$REPORT_DIR/$name.txt" 2>&1 || echo -e "${RED}$name failed${NC}"
    echo -e "${GREEN}✓ $name completed${NC}"
}

# 1. Dependency vulnerability scan
echo -e "${YELLOW}=== Dependency Vulnerability Scan ===${NC}"

# .NET dependencies
if command -v dotnet &> /dev/null; then
    run_scan "dotnet-audit" "dotnet list package --vulnerable --include-transitive"
fi

# NPM dependencies (if any)
if [ -f "package.json" ] && command -v npm &> /dev/null; then
    run_scan "npm-audit" "npm audit --json"
fi

# 2. SAST (Static Application Security Testing)
echo -e "${YELLOW}=== Static Code Analysis ===${NC}"

# Semgrep scan
if command -v semgrep &> /dev/null; then
    run_scan "semgrep" "semgrep --config=auto --json --output=$REPORT_DIR/semgrep.json $SCAN_DIR"
else
    echo "Installing Semgrep..."
    pip3 install semgrep
    run_scan "semgrep" "semgrep --config=auto --json --output=$REPORT_DIR/semgrep.json $SCAN_DIR"
fi

# Security Code Scan for .NET
if command -v security-scan &> /dev/null; then
    run_scan "security-code-scan" "security-scan $SCAN_DIR"
fi

# 3. Secret scanning
echo -e "${YELLOW}=== Secret Detection ===${NC}"

# GitLeaks
if command -v gitleaks &> /dev/null; then
    run_scan "gitleaks" "gitleaks detect --source $SCAN_DIR --report-path $REPORT_DIR/gitleaks.json"
else
    echo "Installing GitLeaks..."
    wget https://github.com/zricethezav/gitleaks/releases/latest/download/gitleaks_linux_amd64 -O /tmp/gitleaks
    chmod +x /tmp/gitleaks
    run_scan "gitleaks" "/tmp/gitleaks detect --source $SCAN_DIR --report-path $REPORT_DIR/gitleaks.json"
fi

# 4. Container security
echo -e "${YELLOW}=== Container Security Scan ===${NC}"

# Trivy for container images
if command -v trivy &> /dev/null; then
    # Scan Dockerfiles
    find . -name "Dockerfile*" -type f | while read dockerfile; do
        echo "Scanning $dockerfile..."
        dir=$(dirname "$dockerfile")
        trivy config "$dockerfile" --format json --output "$REPORT_DIR/trivy-dockerfile-$(basename $dockerfile).json"
    done
    
    # Scan running containers
    docker ps --format "table {{.Names}}" | tail -n +2 | while read container; do
        echo "Scanning container: $container"
        trivy image --format json --output "$REPORT_DIR/trivy-container-$container.json" $(docker inspect --format='{{.Config.Image}}' $container)
    done
else
    echo "Installing Trivy..."
    wget -qO - https://aquasecurity.github.io/trivy-repo/deb/public.key | sudo apt-key add -
    echo "deb https://aquasecurity.github.io/trivy-repo/deb $(lsb_release -sc) main" | sudo tee -a /etc/apt/sources.list.d/trivy.list
    sudo apt-get update && sudo apt-get install trivy
fi

# Docker Bench Security
echo "Running Docker Bench Security..."
docker run --rm --net host --pid host --userns host --cap-add audit_control \
    -e DOCKER_CONTENT_TRUST=$DOCKER_CONTENT_TRUST \
    -v /etc:/etc:ro \
    -v /usr/bin/containerd:/usr/bin/containerd:ro \
    -v /usr/bin/runc:/usr/bin/runc:ro \
    -v /usr/lib/systemd:/usr/lib/systemd:ro \
    -v /var/lib:/var/lib:ro \
    -v /var/run/docker.sock:/var/run/docker.sock:ro \
    --label docker_bench_security \
    docker/docker-bench-security > "$REPORT_DIR/docker-bench.txt"

# 5. Infrastructure security
echo -e "${YELLOW}=== Infrastructure Security ===${NC}"

# Check SSL/TLS configuration
if [ -f "certificates/certificate.pfx" ]; then
    echo "Checking SSL/TLS configuration..."
    openssl pkcs12 -in certificates/certificate.pfx -nokeys -out /tmp/cert.pem -passin pass:$(grep CERTIFICATE_PASSWORD .env.production | cut -d= -f2) 2>/dev/null
    openssl x509 -in /tmp/cert.pem -text -noout > "$REPORT_DIR/ssl-certificate.txt"
    rm -f /tmp/cert.pem
fi

# Check file permissions
echo "Checking file permissions..."
find . -type f -perm /077 -not -path "./.git/*" > "$REPORT_DIR/world-writable-files.txt"
find . -type f -name "*.key" -o -name "*.pem" -o -name "*.pfx" | xargs ls -la > "$REPORT_DIR/key-file-permissions.txt"

# 6. OWASP checks
echo -e "${YELLOW}=== OWASP Security Checks ===${NC}"

# Check for common vulnerabilities
cat > "$REPORT_DIR/owasp-checklist.md" << 'EOF'
# OWASP Top 10 Checklist

## A01:2021 – Broken Access Control
- [ ] Authorization checks on all endpoints
- [ ] Rate limiting implemented
- [ ] CORS properly configured
- [ ] JWT validation in place

## A02:2021 – Cryptographic Failures
- [ ] Strong encryption algorithms used
- [ ] Secure key management (Intel SGX)
- [ ] TLS 1.2+ enforced
- [ ] No hardcoded secrets

## A03:2021 – Injection
- [ ] Parameterized queries used
- [ ] Input validation implemented
- [ ] Command injection prevention

## A04:2021 – Insecure Design
- [ ] Threat modeling completed
- [ ] Security requirements defined
- [ ] Secure design patterns used

## A05:2021 – Security Misconfiguration
- [ ] Default credentials changed
- [ ] Error messages sanitized
- [ ] Security headers configured
- [ ] Unnecessary features disabled

## A06:2021 – Vulnerable Components
- [ ] Dependencies regularly updated
- [ ] Vulnerability scanning automated
- [ ] Component inventory maintained

## A07:2021 – Authentication Failures
- [ ] Strong password policy
- [ ] Account lockout mechanism
- [ ] Multi-factor authentication available
- [ ] Session management secure

## A08:2021 – Software and Data Integrity
- [ ] Code signing implemented
- [ ] CI/CD security checks
- [ ] Dependency integrity verification

## A09:2021 – Security Logging & Monitoring
- [ ] Comprehensive logging
- [ ] Log monitoring and alerting
- [ ] Incident response plan

## A10:2021 – Server-Side Request Forgery
- [ ] URL validation implemented
- [ ] Network segmentation in place
- [ ] Outbound request restrictions
EOF

# 7. Smart contract security
echo -e "${YELLOW}=== Smart Contract Security ===${NC}"

# Basic contract analysis
if [ -d "contracts-neo-n3" ]; then
    echo "Analyzing smart contracts..."
    find contracts-neo-n3 -name "*.cs" -type f | while read contract; do
        echo "Analyzing: $contract" >> "$REPORT_DIR/smart-contract-analysis.txt"
        # Check for common vulnerabilities
        grep -n "Transfer\|OnTransfer\|Runtime.CheckWitness\|Storage.Put\|Storage.Get" "$contract" >> "$REPORT_DIR/smart-contract-analysis.txt" 2>/dev/null || true
    done
fi

# 8. Generate summary report
echo -e "${YELLOW}=== Generating Summary Report ===${NC}"

cat > "$REPORT_DIR/SECURITY_SCAN_SUMMARY.md" << EOF
# Security Scan Summary

**Date**: $(date)
**Scan Directory**: $SCAN_DIR

## Scan Results

### Dependency Vulnerabilities
$(if [ -f "$REPORT_DIR/dotnet-audit.txt" ]; then grep -c "vulnerable" "$REPORT_DIR/dotnet-audit.txt" 2>/dev/null || echo "0"; fi) vulnerabilities found

### Static Code Analysis
$(if [ -f "$REPORT_DIR/semgrep.json" ]; then jq '.results | length' "$REPORT_DIR/semgrep.json" 2>/dev/null || echo "0"; fi) potential issues found

### Secret Detection
$(if [ -f "$REPORT_DIR/gitleaks.json" ]; then jq '.Issues | length' "$REPORT_DIR/gitleaks.json" 2>/dev/null || echo "0"; fi) potential secrets found

### Container Security
- Dockerfile scans: $(find "$REPORT_DIR" -name "trivy-dockerfile-*.json" | wc -l)
- Container scans: $(find "$REPORT_DIR" -name "trivy-container-*.json" | wc -l)

### File Permissions
- World-writable files: $(wc -l < "$REPORT_DIR/world-writable-files.txt" 2>/dev/null || echo "0")
- Key files checked: $(wc -l < "$REPORT_DIR/key-file-permissions.txt" 2>/dev/null || echo "0")

## Recommendations

1. Review all findings in detail
2. Prioritize high and critical vulnerabilities
3. Update dependencies with known vulnerabilities
4. Fix any hardcoded secrets or credentials
5. Review and fix file permissions
6. Implement missing OWASP controls

## Next Steps

1. Review detailed reports in: $REPORT_DIR
2. Create tickets for findings
3. Run scan again after fixes
4. Schedule regular security scans

---
Generated by Neo Service Layer Security Scanner
EOF

# Display summary
echo ""
echo -e "${GREEN}=== Security Scan Complete ===${NC}"
echo ""
cat "$REPORT_DIR/SECURITY_SCAN_SUMMARY.md"
echo ""
echo "Detailed reports saved to: $REPORT_DIR"

# Return non-zero if critical issues found
if grep -q "CRITICAL\|HIGH" "$REPORT_DIR"/*.txt 2>/dev/null; then
    echo -e "${RED}Critical or high severity issues found!${NC}"
    exit 1
fi

exit 0
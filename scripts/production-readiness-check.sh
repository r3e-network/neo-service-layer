#!/bin/bash

# Comprehensive production readiness validation script
set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

# Counters
PASSED=0
FAILED=0
WARNINGS=0

# Results file
RESULTS_FILE="production-readiness-report-$(date +%Y%m%d-%H%M%S).md"

echo -e "${BLUE}═══════════════════════════════════════════════════${NC}"
echo -e "${BLUE}    Neo Service Layer Production Readiness Check    ${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════${NC}"
echo ""

# Helper functions
check_pass() {
    echo -e "${GREEN}✓ PASS: $1${NC}"
    echo "✓ **PASS**: $1" >> "$RESULTS_FILE"
    ((PASSED++))
}

check_fail() {
    echo -e "${RED}✗ FAIL: $1${NC}"
    echo "  ${2}"
    echo "✗ **FAIL**: $1" >> "$RESULTS_FILE"
    echo "  - ${2}" >> "$RESULTS_FILE"
    ((FAILED++))
}

check_warn() {
    echo -e "${YELLOW}⚠ WARN: $1${NC}"
    echo "  ${2}"
    echo "⚠ **WARN**: $1" >> "$RESULTS_FILE"
    echo "  - ${2}" >> "$RESULTS_FILE"
    ((WARNINGS++))
}

section() {
    echo ""
    echo -e "${BLUE}=== $1 ===${NC}"
    echo "" >> "$RESULTS_FILE"
    echo "## $1" >> "$RESULTS_FILE"
    echo "" >> "$RESULTS_FILE"
}

# Initialize report
cat > "$RESULTS_FILE" << EOF
# Production Readiness Report

**Date**: $(date)
**System**: $(hostname)
**Version**: $(git describe --tags --always 2>/dev/null || echo "unknown")

EOF

# 1. Environment Configuration
section "Environment Configuration"

if [ -f ".env.production" ]; then
    check_pass "Production environment file exists"
    
    # Check for placeholders
    if grep -q "CHANGE_ME\|YOUR_\|INSERT_\|TODO\|FIXME" .env.production; then
        check_fail "Environment contains placeholders" "Found placeholder values in .env.production"
    else
        check_pass "No placeholder values in environment"
    fi
    
    # Check file permissions
    perms=$(stat -c %a .env.production)
    if [ "$perms" == "600" ]; then
        check_pass "Environment file has secure permissions (600)"
    else
        check_fail "Environment file has insecure permissions" "Current: $perms, Expected: 600"
    fi
else
    check_fail "Production environment file missing" "Run: ./scripts/generate-secure-credentials.sh"
fi

# 2. SSL/TLS Configuration
section "SSL/TLS Configuration"

if [ -f "certificates/certificate.pfx" ]; then
    check_pass "SSL certificate exists"
    
    # Check certificate validity (if possible)
    if command -v openssl &> /dev/null && [ -f ".env.production" ]; then
        CERT_PASSWORD=$(grep CERTIFICATE_PASSWORD .env.production | cut -d= -f2)
        if [ -n "$CERT_PASSWORD" ]; then
            # Extract certificate info
            openssl pkcs12 -in certificates/certificate.pfx -nokeys -out /tmp/cert.pem -passin "pass:$CERT_PASSWORD" 2>/dev/null
            if [ -f "/tmp/cert.pem" ]; then
                EXPIRY=$(openssl x509 -in /tmp/cert.pem -noout -enddate | cut -d= -f2)
                DAYS_LEFT=$(( ($(date -d "$EXPIRY" +%s) - $(date +%s)) / 86400 ))
                rm -f /tmp/cert.pem
                
                if [ $DAYS_LEFT -gt 30 ]; then
                    check_pass "SSL certificate valid for $DAYS_LEFT days"
                elif [ $DAYS_LEFT -gt 0 ]; then
                    check_warn "SSL certificate expires soon" "Only $DAYS_LEFT days remaining"
                else
                    check_fail "SSL certificate expired" "Expired $((DAYS_LEFT * -1)) days ago"
                fi
            fi
        fi
    fi
else
    check_fail "SSL certificate missing" "Run: ./scripts/generate-production-cert.sh"
fi

# 3. Database Configuration
section "Database Configuration"

if command -v pg_isready &> /dev/null; then
    if [ -f ".env.production" ]; then
        source .env.production
        if pg_isready -h "$DB_HOST" -p "$DB_PORT" &> /dev/null; then
            check_pass "Database connection successful"
        else
            check_fail "Database connection failed" "Check database configuration and connectivity"
        fi
    fi
else
    check_warn "PostgreSQL client not installed" "Cannot verify database connectivity"
fi

# 4. Blockchain Configuration
section "Blockchain Configuration"

if [ -f ".env.production" ]; then
    source .env.production
    
    # Check Neo N3 endpoint
    if [[ "$NEO_N3_RPC_URL" == *"localhost"* ]] || [[ "$NEO_N3_RPC_URL" == *"127.0.0.1"* ]]; then
        check_fail "Neo N3 using localhost endpoint" "Update to production endpoint"
    else
        check_pass "Neo N3 using production endpoint"
    fi
    
    # Check Neo X endpoint
    if [[ "$NEO_X_RPC_URL" == *"localhost"* ]] || [[ "$NEO_X_RPC_URL" == *"127.0.0.1"* ]]; then
        check_fail "Neo X using localhost endpoint" "Update to production endpoint"
    else
        check_pass "Neo X using production endpoint"
    fi
fi

# 5. Smart Contracts
section "Smart Contracts"

TODO_COUNT=$(find contracts-neo-n3 -name "*.cs" -type f -exec grep -l "TODO\|FIXME\|HACK\|XXX" {} \; 2>/dev/null | wc -l)
if [ $TODO_COUNT -eq 0 ]; then
    check_pass "No TODOs in smart contracts"
else
    check_warn "Smart contracts contain TODOs" "$TODO_COUNT files with TODO comments"
fi

# Check for placeholder contract hashes
if grep -q "0x0000000000000000000000000000000000000000" src/Api/NeoServiceLayer.Api/appsettings.json 2>/dev/null; then
    check_fail "Placeholder contract hashes found" "Deploy contracts and update hashes"
else
    check_pass "Contract hashes configured"
fi

# 6. Docker Configuration
section "Docker Configuration"

if [ -f "docker compose.production.yml" ]; then
    check_pass "Production Docker Compose file exists"
    
    # Check for resource limits
    if grep -q "limits:" docker compose.production.yml; then
        check_pass "Docker resource limits configured"
    else
        check_warn "Docker resource limits not configured" "Add memory and CPU limits"
    fi
    
    # Check for health checks
    HEALTHCHECK_COUNT=$(grep -c "healthcheck:" docker compose.production.yml)
    if [ $HEALTHCHECK_COUNT -gt 5 ]; then
        check_pass "Health checks configured ($HEALTHCHECK_COUNT services)"
    else
        check_warn "Limited health checks" "Only $HEALTHCHECK_COUNT services have health checks"
    fi
else
    check_fail "Production Docker Compose missing" "docker compose.production.yml not found"
fi

# 7. Monitoring Configuration
section "Monitoring Configuration"

if [ -f "monitoring/prometheus/prometheus.yml" ]; then
    check_pass "Prometheus configuration exists"
else
    check_fail "Prometheus configuration missing" "monitoring/prometheus/prometheus.yml not found"
fi

if [ -f "monitoring/prometheus/alerts.yml" ] || [ -f "monitoring/alerting/alerts.yaml" ]; then
    check_pass "Alert rules configured"
else
    check_fail "Alert rules missing" "No alert configuration found"
fi

if [ -d "monitoring/grafana/dashboards" ]; then
    DASHBOARD_COUNT=$(find monitoring/grafana/dashboards -name "*.json" | wc -l)
    if [ $DASHBOARD_COUNT -gt 0 ]; then
        check_pass "Grafana dashboards configured ($DASHBOARD_COUNT dashboards)"
    else
        check_fail "No Grafana dashboards" "Add monitoring dashboards"
    fi
else
    check_fail "Grafana dashboards directory missing" "monitoring/grafana/dashboards not found"
fi

# 8. Backup Configuration
section "Backup Configuration"

if [ -f "scripts/backup-automation.sh" ]; then
    check_pass "Backup automation script exists"
    
    if [ -x "scripts/backup-automation.sh" ]; then
        check_pass "Backup script is executable"
    else
        check_fail "Backup script not executable" "Run: chmod +x scripts/backup-automation.sh"
    fi
else
    check_fail "Backup automation missing" "scripts/backup-automation.sh not found"
fi

# 9. Security Configuration
section "Security Configuration"

# Check JWT configuration
if [ -f ".env.production" ]; then
    JWT_KEY=$(grep "^JWT_SECRET_KEY=" .env.production | cut -d= -f2)
    if [ -n "$JWT_KEY" ] && [ ${#JWT_KEY} -ge 32 ]; then
        check_pass "JWT secret key configured (${#JWT_KEY} characters)"
    else
        check_fail "JWT secret key invalid" "Key must be at least 32 characters"
    fi
fi

# Check for security scanning
if [ -f ".github/workflows/security-scan.yml" ]; then
    check_pass "Security scanning workflow configured"
else
    check_warn "Security scanning not automated" "Add .github/workflows/security-scan.yml"
fi

# 10. Documentation
section "Documentation"

REQUIRED_DOCS=(
    "README.md"
    "PRODUCTION_DEPLOYMENT_CHECKLIST.md"
    "docs/DISASTER_RECOVERY_PLAN.md"
)

for doc in "${REQUIRED_DOCS[@]}"; do
    if [ -f "$doc" ]; then
        check_pass "Documentation exists: $doc"
    else
        check_fail "Documentation missing: $doc" "Create required documentation"
    fi
done

# 11. CI/CD Configuration
section "CI/CD Configuration"

if [ -d ".github/workflows" ]; then
    WORKFLOW_COUNT=$(find .github/workflows -name "*.yml" -o -name "*.yaml" | wc -l)
    if [ $WORKFLOW_COUNT -gt 0 ]; then
        check_pass "GitHub Actions workflows configured ($WORKFLOW_COUNT workflows)"
    else
        check_fail "No CI/CD workflows" "Add workflow files to .github/workflows"
    fi
else
    check_fail "GitHub Actions not configured" ".github/workflows directory missing"
fi

# 12. External Services
section "External Services"

if [ -f ".env.production" ]; then
    # Check email configuration
    if grep -q "^SMTP_HOST=smtp.your-provider.com" .env.production; then
        check_fail "Email service not configured" "Update SMTP settings"
    else
        check_pass "Email service configured"
    fi
    
    # Check Intel SGX
    if grep -q "^IAS_API_KEY=YOUR_IAS_API_KEY_HERE" .env.production; then
        check_fail "Intel SGX API key not configured" "Add IAS API key"
    else
        check_pass "Intel SGX API key configured"
    fi
fi

# Summary
echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════${NC}"
echo -e "${BLUE}                    SUMMARY                         ${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════${NC}"

# Add summary to report
cat >> "$RESULTS_FILE" << EOF

## Summary

- **Passed**: $PASSED checks
- **Failed**: $FAILED checks
- **Warnings**: $WARNINGS checks
- **Total**: $((PASSED + FAILED + WARNINGS)) checks

### Production Readiness Score: $(( (PASSED * 100) / (PASSED + FAILED + WARNINGS) ))%

EOF

# Display summary
echo ""
echo -e "Passed:   ${GREEN}$PASSED${NC}"
echo -e "Failed:   ${RED}$FAILED${NC}"
echo -e "Warnings: ${YELLOW}$WARNINGS${NC}"
echo ""

SCORE=$(( (PASSED * 100) / (PASSED + FAILED + WARNINGS) ))
if [ $SCORE -ge 95 ]; then
    echo -e "${GREEN}Production Readiness Score: ${SCORE}% - READY FOR PRODUCTION${NC}"
    echo "### Status: READY FOR PRODUCTION" >> "$RESULTS_FILE"
elif [ $SCORE -ge 80 ]; then
    echo -e "${YELLOW}Production Readiness Score: ${SCORE}% - ALMOST READY${NC}"
    echo "### Status: ALMOST READY (Address failures)" >> "$RESULTS_FILE"
else
    echo -e "${RED}Production Readiness Score: ${SCORE}% - NOT READY${NC}"
    echo "### Status: NOT READY FOR PRODUCTION" >> "$RESULTS_FILE"
fi

echo ""
echo "Detailed report saved to: $RESULTS_FILE"

# Exit with error if critical failures
if [ $FAILED -gt 0 ]; then
    exit 1
fi

exit 0
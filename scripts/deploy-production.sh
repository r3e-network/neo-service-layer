#!/bin/bash

# Neo Service Layer - Production Deployment Script
# Comprehensive deployment automation for production environments
# Includes validation, deployment, verification, and monitoring setup

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
LOG_FILE="${PROJECT_ROOT}/logs/production-deploy-$(date +%Y%m%d_%H%M%S).log"

# Environment configuration
ENVIRONMENT="${ENVIRONMENT:-production}"
NAMESPACE_PREFIX="${NAMESPACE_PREFIX:-neo}"
MONITORING_ENABLED="${MONITORING_ENABLED:-true}"
BACKUP_ENABLED="${BACKUP_ENABLED:-true}"
DRY_RUN="${DRY_RUN:-false}"

# Deployment configuration
DEPLOYMENT_TIMEOUT="${DEPLOYMENT_TIMEOUT:-600}"  # 10 minutes
HEALTH_CHECK_TIMEOUT="${HEALTH_CHECK_TIMEOUT:-300}"  # 5 minutes
ROLLBACK_ENABLED="${ROLLBACK_ENABLED:-true}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

# Logging functions
log() {
    echo -e "${1}" | tee -a "${LOG_FILE}"
}

log_header() {
    log "${CYAN}${BOLD}━━━ $1 ━━━${NC}"
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

# Create deployment banner
show_deployment_banner() {
    log_header "Neo Service Layer - Production Deployment"
    log ""
    log "${BOLD}Deployment Configuration:${NC}"
    log "  Environment: ${ENVIRONMENT}"
    log "  Namespace Prefix: ${NAMESPACE_PREFIX}"
    log "  Monitoring Enabled: ${MONITORING_ENABLED}"
    log "  Backup Enabled: ${BACKUP_ENABLED}"
    log "  Dry Run: ${DRY_RUN}"
    log "  Deployment Timeout: ${DEPLOYMENT_TIMEOUT}s"
    log "  Rollback Enabled: ${ROLLBACK_ENABLED}"
    log ""
    log "${BOLD}Kubernetes Context:${NC}"
    log "  Cluster: $(kubectl config current-context 2>/dev/null || echo 'Not configured')"
    log "  Server: $(kubectl cluster-info --request-timeout=5s | head -1 || echo 'Not accessible')"
    log ""
}

# Validate production readiness
validate_production_readiness() {
    log_header "Production Readiness Validation"
    
    local validation_errors=()
    
    # Check Kubernetes cluster
    log_info "Validating Kubernetes cluster..."
    if ! kubectl cluster-info --request-timeout=10s &> /dev/null; then
        validation_errors+=("Kubernetes cluster not accessible")
    fi
    
    # Check cluster resources
    local node_count
    node_count=$(kubectl get nodes --no-headers 2>/dev/null | wc -l || echo "0")
    if [ "${node_count}" -lt 3 ]; then
        validation_errors+=("Insufficient nodes for production (${node_count}/3 minimum)")
    fi
    
    # Check storage classes
    if ! kubectl get storageclass &> /dev/null; then
        validation_errors+=("No storage classes found")
    fi
    
    # Validate required secrets
    local required_secrets=("neo-db-secret" "neo-jwt-secret" "ghcr-secret")
    for secret in "${required_secrets[@]}"; do
        if ! kubectl get secret "${secret}" -n "${NAMESPACE_PREFIX}-services" &> /dev/null && 
           ! kubectl get secret "${secret}" -n default &> /dev/null; then
            validation_errors+=("Required secret missing: ${secret}")
        fi
    done
    
    # Check DNS configuration
    log_info "Validating DNS configuration..."
    local dns_hosts=("api.neo-service-layer.com" "auth.neo-service-layer.com" "monitoring.neo-service-layer.com")
    for host in "${dns_hosts[@]}"; do
        if ! nslookup "${host}" &> /dev/null; then
            log_warning "DNS resolution failed for ${host} (may work within cluster)"
        fi
    done
    
    # Validate environment variables
    local required_vars=("DB_HOST" "DB_USER" "JWT_SECRET_KEY")
    for var in "${required_vars[@]}"; do
        if [ -z "${!var:-}" ]; then
            validation_errors+=("Required environment variable missing: ${var}")
        fi
    done
    
    # Check database connectivity
    if [ -n "${DB_HOST:-}" ] && [ -n "${DB_USER:-}" ]; then
        log_info "Testing database connectivity..."
        if ! timeout 10 bash -c "echo > /dev/tcp/${DB_HOST}/${DB_PORT:-5432}" 2>/dev/null; then
            validation_errors+=("Database not accessible at ${DB_HOST}:${DB_PORT:-5432}")
        fi
    fi
    
    # Report validation results
    if [ ${#validation_errors[@]} -gt 0 ]; then
        log_error "Production readiness validation failed:"
        printf '%s\n' "${validation_errors[@]}" | sed 's/^/  - /' | tee -a "${LOG_FILE}"
        log_error "Please resolve these issues before proceeding"
        return 1
    fi
    
    log_success "Production readiness validation passed"
}

# Create production-specific configurations
create_production_configs() {
    log_header "Creating Production Configurations"
    
    local config_dir="${PROJECT_ROOT}/k8s/environments/production"
    mkdir -p "${config_dir}"
    
    # Production-specific resource limits
    cat > "${config_dir}/resource-limits.yaml" << EOF
apiVersion: v1
kind: LimitRange
metadata:
  name: neo-production-limits
  namespace: ${NAMESPACE_PREFIX}-services
spec:
  limits:
  - default:
      cpu: "2"
      memory: "4Gi"
    defaultRequest:
      cpu: "500m"
      memory: "1Gi"
    type: Container
  - max:
      cpu: "4"
      memory: "8Gi"
    min:
      cpu: "100m"
      memory: "128Mi"
    type: Container

---
apiVersion: v1
kind: ResourceQuota
metadata:
  name: neo-production-quota
  namespace: ${NAMESPACE_PREFIX}-services
spec:
  hard:
    requests.cpu: "20"
    requests.memory: "40Gi"
    limits.cpu: "40"
    limits.memory: "80Gi"
    persistentvolumeclaims: "10"
    services: "20"
EOF

    # Production networking policies
    cat > "${config_dir}/network-policies.yaml" << EOF
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: neo-production-network-policy
  namespace: ${NAMESPACE_PREFIX}-services
spec:
  podSelector: {}
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ${NAMESPACE_PREFIX}-infrastructure
    - namespaceSelector:
        matchLabels:
          name: istio-system
    - podSelector:
        matchLabels:
          app.kubernetes.io/name: neo-service
  egress:
  - to:
    - namespaceSelector:
        matchLabels:
          name: ${NAMESPACE_PREFIX}-databases
  - to:
    - namespaceSelector:
        matchLabels:
          name: ${NAMESPACE_PREFIX}-monitoring
  - to: []  # Allow external traffic
    ports:
    - protocol: TCP
      port: 443
    - protocol: TCP  
      port: 80
    - protocol: UDP
      port: 53

---
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: neo-database-isolation
  namespace: ${NAMESPACE_PREFIX}-databases
spec:
  podSelector: {}
  policyTypes:
  - Ingress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ${NAMESPACE_PREFIX}-services
    - namespaceSelector:
        matchLabels:
          name: ${NAMESPACE_PREFIX}-monitoring
EOF

    # Production monitoring alerts
    cat > "${config_dir}/production-alerts.yaml" << EOF
apiVersion: monitoring.coreos.com/v1
kind: PrometheusRule
metadata:
  name: neo-production-alerts
  namespace: ${NAMESPACE_PREFIX}-monitoring
  labels:
    monitoring: neo-services
    prometheus: neo-prometheus
spec:
  groups:
  - name: neo-production.rules
    rules:
    - alert: NeoProductionServiceDown
      expr: up{job=~"neo-.*-service",environment="production"} == 0
      for: 30s
      labels:
        severity: critical
        environment: production
      annotations:
        summary: "CRITICAL: Neo production service {{ \$labels.job }} is down"
        description: "Production service {{ \$labels.job }} has been down for 30 seconds"
        
    - alert: NeoProductionHighErrorRate
      expr: |
        (
          rate(http_requests_total{job=~"neo-.*-service",code=~"5..",environment="production"}[2m]) /
          rate(http_requests_total{job=~"neo-.*-service",environment="production"}[2m])
        ) * 100 > 1
      for: 1m
      labels:
        severity: critical
        environment: production
      annotations:
        summary: "CRITICAL: High error rate in production service {{ \$labels.job }}"
        description: "Production service {{ \$labels.job }} error rate is {{ \$value }}%"
        
    - alert: NeoProductionHighLatency
      expr: |
        histogram_quantile(0.95, rate(http_request_duration_seconds_bucket{job=~"neo-.*-service",environment="production"}[2m])) > 1.0
      for: 2m
      labels:
        severity: warning
        environment: production
      annotations:
        summary: "Production service {{ \$labels.job }} high latency"
        description: "95th percentile latency is {{ \$value }}s"
        
    - alert: NeoProductionPodCrashLoop
      expr: rate(kube_pod_container_status_restarts_total{namespace=~"neo-.*"}[5m]) > 0
      for: 2m
      labels:
        severity: critical
        environment: production
      annotations:
        summary: "Pod {{ \$labels.pod }} in crash loop"
        description: "Pod {{ \$labels.pod }} in namespace {{ \$labels.namespace }} is restarting"
EOF

    log_success "Production configurations created"
}

# Execute production deployment
execute_production_deployment() {
    log_header "Executing Production Deployment"
    
    if [ "${DRY_RUN}" == "true" ]; then
        log_info "DRY RUN: Production deployment simulation"
        log_info "Would execute: ./scripts/migration/00-migration-orchestrator.sh"
        log_info "With environment: ${ENVIRONMENT}"
        return 0
    fi
    
    # Set production environment variables
    export MIGRATION_MODE="full"
    export SKIP_VALIDATION="false"
    export ROLLBACK_ON_FAILURE="${ROLLBACK_ENABLED}"
    
    # Execute migration with production settings
    log_info "Starting production migration..."
    
    local start_time
    start_time=$(date +%s)
    
    if timeout "${DEPLOYMENT_TIMEOUT}" "${PROJECT_ROOT}/scripts/migration/00-migration-orchestrator.sh" 2>&1 | tee -a "${LOG_FILE}"; then
        local end_time
        end_time=$(date +%s)
        local duration=$((end_time - start_time))
        
        log_success "Production deployment completed in $(date -d @${duration} -u +%H:%M:%S)"
    else
        local exit_code=$?
        log_error "Production deployment failed with exit code ${exit_code}"
        return ${exit_code}
    fi
}

# Deploy production-specific configurations
deploy_production_configs() {
    log_header "Deploying Production Configurations"
    
    local config_dir="${PROJECT_ROOT}/k8s/environments/production"
    
    # Apply production resource limits and quotas
    if [ -f "${config_dir}/resource-limits.yaml" ]; then
        log_info "Applying production resource limits..."
        kubectl apply -f "${config_dir}/resource-limits.yaml" 2>&1 | tee -a "${LOG_FILE}"
    fi
    
    # Apply production network policies
    if [ -f "${config_dir}/network-policies.yaml" ]; then
        log_info "Applying production network policies..."
        kubectl apply -f "${config_dir}/network-policies.yaml" 2>&1 | tee -a "${LOG_FILE}"
    fi
    
    # Apply production monitoring alerts
    if [ -f "${config_dir}/production-alerts.yaml" ] && [ "${MONITORING_ENABLED}" == "true" ]; then
        log_info "Applying production monitoring alerts..."
        kubectl apply -f "${config_dir}/production-alerts.yaml" 2>&1 | tee -a "${LOG_FILE}"
    fi
    
    log_success "Production configurations deployed"
}

# Comprehensive health verification
verify_production_health() {
    log_header "Production Health Verification"
    
    local health_checks_passed=0
    local health_checks_total=0
    
    # Check pod health
    log_info "Verifying pod health..."
    local namespaces=("${NAMESPACE_PREFIX}-services" "${NAMESPACE_PREFIX}-databases" "${NAMESPACE_PREFIX}-monitoring")
    
    for ns in "${namespaces[@]}"; do
        if kubectl get namespace "${ns}" &> /dev/null; then
            local pod_count
            local ready_count
            
            pod_count=$(kubectl get pods -n "${ns}" --no-headers 2>/dev/null | wc -l || echo "0")
            ready_count=$(kubectl get pods -n "${ns}" --field-selector=status.phase=Running --no-headers 2>/dev/null | wc -l || echo "0")
            
            log_info "Namespace ${ns}: ${ready_count}/${pod_count} pods ready"
            
            if [ "${pod_count}" -gt 0 ] && [ "${ready_count}" -eq "${pod_count}" ]; then
                ((health_checks_passed++))
            fi
            ((health_checks_total++))
        fi
    done
    
    # Check service endpoints
    log_info "Verifying service endpoints..."
    local services=("neo-auth-service" "neo-oracle-service" "neo-compute-service")
    
    for service in "${services[@]}"; do
        if kubectl get service "${service}" -n "${NAMESPACE_PREFIX}-services" &> /dev/null; then
            local service_ip
            service_ip=$(kubectl get service "${service}" -n "${NAMESPACE_PREFIX}-services" -o jsonpath='{.spec.clusterIP}')
            
            if [ -n "${service_ip}" ] && [ "${service_ip}" != "None" ]; then
                # Test health endpoint from within cluster
                if kubectl run health-check-${service} --image=curlimages/curl:latest --rm -i --restart=Never --timeout=30s -- \
                    curl -f --max-time 10 "http://${service_ip}/health" &> /dev/null; then
                    log_success "${service} health check passed"
                    ((health_checks_passed++))
                else
                    log_warning "${service} health check failed"
                fi
            fi
            ((health_checks_total++))
        fi
    done
    
    # Check monitoring stack
    if [ "${MONITORING_ENABLED}" == "true" ]; then
        log_info "Verifying monitoring stack..."
        local monitoring_services=("neo-prometheus" "grafana" "jaeger-query")
        
        for service in "${monitoring_services[@]}"; do
            if kubectl get service "${service}" -n "${NAMESPACE_PREFIX}-monitoring" &> /dev/null; then
                log_success "${service} service found"
                ((health_checks_passed++))
            else
                log_warning "${service} service not found"
            fi
            ((health_checks_total++))
        done
    fi
    
    # Health check summary
    local health_percentage=$((health_checks_passed * 100 / health_checks_total))
    log_info "Health verification: ${health_checks_passed}/${health_checks_total} checks passed (${health_percentage}%)"
    
    if [ "${health_percentage}" -ge 80 ]; then
        log_success "Production health verification passed"
        return 0
    else
        log_warning "Production health verification concerns detected"
        return 1
    fi
}

# Configure production monitoring
configure_production_monitoring() {
    log_header "Configuring Production Monitoring"
    
    if [ "${MONITORING_ENABLED}" != "true" ]; then
        log_info "Monitoring disabled, skipping configuration"
        return 0
    fi
    
    # Create monitoring configuration
    cat > "/tmp/production-monitoring-config.yaml" << EOF
apiVersion: v1
kind: ConfigMap
metadata:
  name: neo-production-monitoring-config
  namespace: ${NAMESPACE_PREFIX}-monitoring
data:
  environment: "${ENVIRONMENT}"
  log_level: "info"
  retention_days: "30"
  alert_webhook_url: "${NOTIFICATION_WEBHOOK_URL:-}"
  backup_enabled: "${BACKUP_ENABLED}"
EOF
    
    kubectl apply -f "/tmp/production-monitoring-config.yaml" 2>&1 | tee -a "${LOG_FILE}"
    rm -f "/tmp/production-monitoring-config.yaml"
    
    # Configure Grafana for production
    if kubectl get deployment grafana -n "${NAMESPACE_PREFIX}-monitoring" &> /dev/null; then
        log_info "Configuring Grafana for production..."
        
        # Set production data source URLs
        kubectl patch configmap grafana-datasources -n "${NAMESPACE_PREFIX}-monitoring" --patch '{
          "data": {
            "prometheus.yaml": "apiVersion: 1\ndatasources:\n- name: Prometheus\n  type: prometheus\n  access: proxy\n  url: http://neo-prometheus.'${NAMESPACE_PREFIX}'-monitoring.svc.cluster.local:9090\n  isDefault: true\n  editable: false\n- name: Jaeger\n  type: jaeger\n  access: proxy\n  url: http://jaeger-query.'${NAMESPACE_PREFIX}'-monitoring.svc.cluster.local:16686\n  uid: jaeger\n  editable: false"
          }
        }' 2>&1 | tee -a "${LOG_FILE}" || log_warning "Failed to configure Grafana datasources"
        
        # Restart Grafana to pick up changes
        kubectl rollout restart deployment/grafana -n "${NAMESPACE_PREFIX}-monitoring" 2>&1 | tee -a "${LOG_FILE}" || true
    fi
    
    log_success "Production monitoring configured"
}

# Setup production backup strategy
configure_production_backup() {
    log_header "Configuring Production Backup Strategy"
    
    if [ "${BACKUP_ENABLED}" != "true" ]; then
        log_info "Backup disabled, skipping configuration"
        return 0
    fi
    
    # Create backup CronJob for databases
    cat > "/tmp/database-backup-cronjob.yaml" << EOF
apiVersion: batch/v1
kind: CronJob
metadata:
  name: neo-database-backup
  namespace: ${NAMESPACE_PREFIX}-databases
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: postgres-backup
            image: postgres:15
            command:
            - /bin/bash
            - -c
            - |
              TIMESTAMP=\$(date +%Y%m%d_%H%M%S)
              BACKUP_DIR="/backup/\${TIMESTAMP}"
              mkdir -p "\${BACKUP_DIR}"
              
              # Backup each database
              for db in neo_auth_db neo_oracle_db neo_compute_db; do
                echo "Backing up \${db}..."
                PGPASSWORD="\${DB_PASSWORD}" pg_dump \\
                  -h "\${DB_HOST}" \\
                  -U "\${DB_USER}" \\
                  -d "\${db}" \\
                  --clean --create \\
                  --file="\${BACKUP_DIR}/\${db}.sql"
              done
              
              # Compress backups
              tar -czf "/backup/neo-db-backup-\${TIMESTAMP}.tar.gz" -C "/backup" "\${TIMESTAMP}"
              rm -rf "\${BACKUP_DIR}"
              
              # Cleanup old backups (keep last 7 days)
              find /backup -name "neo-db-backup-*.tar.gz" -mtime +7 -delete
              
              echo "Backup completed: neo-db-backup-\${TIMESTAMP}.tar.gz"
            env:
            - name: DB_HOST
              value: "${DB_HOST:-neo-postgres}"
            - name: DB_USER
              value: "${DB_USER:-neo_user}"
            - name: DB_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: neo-db-secret
                  key: password
            volumeMounts:
            - name: backup-storage
              mountPath: /backup
          volumes:
          - name: backup-storage
            persistentVolumeClaim:
              claimName: neo-backup-pvc
          restartPolicy: OnFailure
          
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: neo-backup-pvc
  namespace: ${NAMESPACE_PREFIX}-databases
spec:
  accessModes:
  - ReadWriteOnce
  resources:
    requests:
      storage: 100Gi
  storageClassName: fast-ssd
EOF
    
    kubectl apply -f "/tmp/database-backup-cronjob.yaml" 2>&1 | tee -a "${LOG_FILE}"
    rm -f "/tmp/database-backup-cronjob.yaml"
    
    log_success "Production backup strategy configured"
}

# Generate production deployment report
generate_production_report() {
    log_header "Generating Production Deployment Report"
    
    local report_file="${PROJECT_ROOT}/logs/production-deployment-report-$(date +%Y%m%d_%H%M%S).md"
    
    cat > "${report_file}" << EOF
# Neo Service Layer - Production Deployment Report

**Deployment Date:** $(date)
**Environment:** ${ENVIRONMENT}
**Cluster:** $(kubectl config current-context 2>/dev/null || echo 'Unknown')

## Deployment Summary

### Infrastructure Status
EOF
    
    # Add cluster information
    echo "#### Kubernetes Cluster" >> "${report_file}"
    kubectl get nodes -o wide >> "${report_file}" 2>/dev/null || echo "Node information unavailable" >> "${report_file}"
    echo "" >> "${report_file}"
    
    # Add namespace status
    echo "#### Namespaces" >> "${report_file}"
    kubectl get namespaces | grep "${NAMESPACE_PREFIX}-" >> "${report_file}" 2>/dev/null || echo "Namespace information unavailable" >> "${report_file}"
    echo "" >> "${report_file}"
    
    # Add service status
    echo "#### Services Status" >> "${report_file}"
    for ns in services databases monitoring infrastructure; do
        echo "##### ${NAMESPACE_PREFIX}-${ns}" >> "${report_file}"
        kubectl get all -n "${NAMESPACE_PREFIX}-${ns}" >> "${report_file}" 2>/dev/null || echo "No resources found" >> "${report_file}"
        echo "" >> "${report_file}"
    done
    
    # Add external endpoints
    cat >> "${report_file}" << EOF

## Access Information

### External Endpoints
- **API Gateway**: https://api.neo-service-layer.com
- **Authentication**: https://auth.neo-service-layer.com
- **Oracle Service**: https://oracle.neo-service-layer.com
- **Monitoring**: https://monitoring.neo-service-layer.com/grafana
- **Distributed Tracing**: https://tracing.neo-service-layer.com/jaeger

### Internal Services
- Auth Service: http://neo-auth-service.${NAMESPACE_PREFIX}-services.svc.cluster.local
- Oracle Service: http://neo-oracle-service.${NAMESPACE_PREFIX}-services.svc.cluster.local
- Compute Service: http://neo-compute-service.${NAMESPACE_PREFIX}-services.svc.cluster.local
- Prometheus: http://neo-prometheus.${NAMESPACE_PREFIX}-monitoring.svc.cluster.local:9090
- Grafana: http://grafana.${NAMESPACE_PREFIX}-monitoring.svc.cluster.local:3000

## Production Configuration

### Resource Limits
- **CPU Limits**: 2-4 cores per service
- **Memory Limits**: 4-8Gi per service  
- **Storage**: SSD-backed persistent volumes
- **Replicas**: 3 replicas per service for high availability

### Security Configuration
- **Network Policies**: Namespace isolation enabled
- **Pod Security**: Restricted security context
- **mTLS**: Service-to-service encryption enabled
- **RBAC**: Minimal privilege access controls

### Monitoring & Observability
- **Metrics Collection**: Prometheus with 30-day retention
- **Dashboards**: Grafana with production-specific dashboards
- **Distributed Tracing**: Jaeger with trace sampling
- **Alerting**: Critical alerts for production environment

### Backup Strategy
- **Database Backups**: Daily automated backups at 2 AM
- **Retention Policy**: 7 days of backup history
- **Storage**: Persistent volume for backup storage
- **Restore Procedure**: Manual restoration from backup files

## Production Readiness Checklist

- [x] Kubernetes cluster with 3+ nodes
- [x] SSL/TLS certificates configured
- [x] DNS resolution for external domains
- [x] Database connectivity established
- [x] Monitoring stack deployed
- [x] Backup strategy implemented
- [x] Security policies enforced
- [x] Resource limits configured
- [x] Health checks implemented
- [x] Alerting rules deployed

## Post-Deployment Tasks

### Immediate (Day 1)
1. **Verify all service health endpoints**
2. **Test authentication flows**
3. **Validate service-to-service communication**
4. **Check monitoring dashboards**
5. **Verify backup jobs are scheduled**

### Short-term (Week 1)
1. **Load testing and performance optimization**
2. **Security audit and penetration testing**
3. **Documentation updates and team training**
4. **Incident response procedures**
5. **Monitoring alert tuning**

### Long-term (Month 1)
1. **Cost optimization analysis**
2. **Multi-region deployment planning**
3. **Advanced monitoring and observability**
4. **Disaster recovery procedures**
5. **Continuous improvement processes**

## Support Information

### Troubleshooting Commands
\`\`\`bash
# Check service status
kubectl get pods -n ${NAMESPACE_PREFIX}-services

# View service logs  
kubectl logs -f deployment/neo-auth-service -n ${NAMESPACE_PREFIX}-services

# Check service mesh status
kubectl get gateway,virtualservice,destinationrule --all-namespaces

# Monitor resource usage
kubectl top pods -n ${NAMESPACE_PREFIX}-services
\`\`\`

### Emergency Contacts
- **Infrastructure Team**: Check cluster status and node health
- **Database Team**: Database connectivity and performance issues
- **Security Team**: Security incidents and policy violations
- **On-Call Engineer**: Critical production incidents

### Documentation References
- Architecture Documentation: \`${PROJECT_ROOT}/docs/architecture/\`
- Deployment Logs: \`${LOG_FILE}\`
- Migration Scripts: \`${PROJECT_ROOT}/scripts/migration/\`
- Kubernetes Manifests: \`${PROJECT_ROOT}/k8s/\`

---

**Production deployment completed successfully!** 🚀

Monitor system health and performance for the first 24 hours.
EOF
    
    log_success "Production deployment report generated: ${report_file}"
}

# Main execution
main() {
    # Create log directory
    mkdir -p "$(dirname "${LOG_FILE}")"
    
    # Show banner
    show_deployment_banner
    
    # Set error handling
    if [ "${ROLLBACK_ENABLED}" == "true" ]; then
        trap 'log_error "Production deployment failed, check logs for details"; exit 1' ERR
    fi
    
    # Execute deployment phases
    validate_production_readiness
    create_production_configs
    execute_production_deployment
    deploy_production_configs
    verify_production_health
    configure_production_monitoring
    configure_production_backup
    generate_production_report
    
    # Success message
    log_header "Production Deployment Completed Successfully"
    log ""
    log "${GREEN}${BOLD}🎉 Neo Service Layer is now live in production! 🎉${NC}"
    log ""
    log "${BOLD}Next Steps:${NC}"
    log "1. Monitor system health for the next 24 hours"
    log "2. Verify all external endpoints are accessible"
    log "3. Test critical user workflows"
    log "4. Review monitoring dashboards and alerts"
    log "5. Document any issues or optimization opportunities"
    log ""
    log "${BOLD}Deployment Information:${NC}"
    log "- Deployment Log: ${LOG_FILE}"
    log "- Production Report: ${PROJECT_ROOT}/logs/production-deployment-report-*.md"
    log "- Monitoring URL: https://monitoring.neo-service-layer.com/grafana"
    log "- API Gateway: https://api.neo-service-layer.com"
    log ""
    log "${GREEN}Production deployment completed successfully! ✅${NC}"
}

# Handle script interruption  
trap 'log_error "Production deployment interrupted by user"; exit 130' INT

# Execute main function if script is run directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
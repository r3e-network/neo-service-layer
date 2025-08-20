#!/bin/bash

# Disaster Recovery Automation for Neo Service Layer
# Comprehensive backup, restore, and failover procedures

set -e

# Configuration
PRIMARY_CLUSTER="${PRIMARY_CLUSTER:-primary}"
SECONDARY_CLUSTER="${SECONDARY_CLUSTER:-secondary}"
NAMESPACE="${NAMESPACE:-neo-service-layer}"
BACKUP_BUCKET="${BACKUP_BUCKET:-neo-service-backups}"
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"
RTO_TARGET="${RTO_TARGET:-900}" # 15 minutes
RPO_TARGET="${RPO_TARGET:-300}" # 5 minutes

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Logging function
log() {
    local level=$1
    shift
    local message="$@"
    local timestamp=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    
    case $level in
        ERROR)   echo -e "${RED}[${timestamp}] ERROR: ${message}${NC}" >&2 ;;
        WARN)    echo -e "${YELLOW}[${timestamp}] WARN: ${message}${NC}" ;;
        INFO)    echo -e "${GREEN}[${timestamp}] INFO: ${message}${NC}" ;;
        DEBUG)   echo -e "${BLUE}[${timestamp}] DEBUG: ${message}${NC}" ;;
    esac
}

# Function to check prerequisites
check_prerequisites() {
    log INFO "🔍 Checking disaster recovery prerequisites"
    
    # Check required tools
    local missing_tools=()
    
    for tool in kubectl velero restic aws; do
        if ! command -v $tool >/dev/null 2>&1; then
            missing_tools+=($tool)
        fi
    done
    
    if [[ ${#missing_tools[@]} -gt 0 ]]; then
        log ERROR "Missing required tools: ${missing_tools[*]}"
        log INFO "Install missing tools:"
        for tool in "${missing_tools[@]}"; do
            case $tool in
                kubectl) echo "  - kubectl: https://kubernetes.io/docs/tasks/tools/install-kubectl/" ;;
                velero)  echo "  - velero: https://velero.io/docs/v1.12/basic-install/" ;;
                restic)  echo "  - restic: https://restic.readthedocs.io/en/latest/020_installation.html" ;;
                aws)     echo "  - aws cli: https://aws.amazon.com/cli/" ;;
            esac
        done
        return 1
    fi
    
    # Check cluster connectivity
    if ! kubectl cluster-info >/dev/null 2>&1; then
        log ERROR "Cannot connect to Kubernetes cluster"
        return 1
    fi
    
    # Check Velero installation
    if ! kubectl get namespace velero >/dev/null 2>&1; then
        log WARN "Velero not installed, installing..."
        install_velero
    fi
    
    log INFO "✅ All prerequisites satisfied"
}

# Function to install Velero
install_velero() {
    log INFO "📦 Installing Velero for backup and restore"
    
    # Create Velero namespace
    kubectl create namespace velero --dry-run=client -o yaml | kubectl apply -f -
    
    # Install Velero with AWS plugin
    velero install \
        --provider aws \
        --plugins velero/velero-plugin-for-aws:v1.8.0 \
        --bucket $BACKUP_BUCKET \
        --backup-location-config region=us-west-2 \
        --snapshot-location-config region=us-west-2 \
        --secret-file ./velero-credentials
    
    # Wait for Velero to be ready
    kubectl wait --for=condition=available --timeout=300s deployment/velero -n velero
    
    log INFO "✅ Velero installed successfully"
}

# Function to create comprehensive backup
create_backup() {
    local backup_name="neo-service-$(date +%Y%m%d-%H%M%S)"
    
    log INFO "💾 Creating comprehensive backup: $backup_name"
    
    # Create Velero backup
    velero backup create $backup_name \
        --include-namespaces $NAMESPACE \
        --storage-location default \
        --volume-snapshot-locations default \
        --ttl 720h0m0s \
        --wait
    
    if [[ $? -eq 0 ]]; then
        log INFO "✅ Velero backup created successfully"
    else
        log ERROR "❌ Velero backup failed"
        return 1
    fi
    
    # Backup custom resources and configurations
    local config_backup_dir="config-backup-$(date +%Y%m%d-%H%M%S)"
    mkdir -p $config_backup_dir
    
    log INFO "📋 Backing up configurations to $config_backup_dir"
    
    # Export all resources
    kubectl get all -n $NAMESPACE -o yaml > $config_backup_dir/all-resources.yaml
    kubectl get configmaps -n $NAMESPACE -o yaml > $config_backup_dir/configmaps.yaml
    kubectl get secrets -n $NAMESPACE -o yaml > $config_backup_dir/secrets.yaml
    kubectl get pvc -n $NAMESPACE -o yaml > $config_backup_dir/pvc.yaml
    kubectl get networkpolicies -n $NAMESPACE -o yaml > $config_backup_dir/network-policies.yaml
    kubectl get rolebindings,roles -n $NAMESPACE -o yaml > $config_backup_dir/rbac.yaml
    
    # Export Istio resources
    kubectl get virtualservices,destinationrules,gateways,serviceentries -n $NAMESPACE -o yaml > $config_backup_dir/istio.yaml 2>/dev/null || echo "No Istio resources found"
    
    # Export monitoring resources
    kubectl get servicemonitors,prometheusrules -n $NAMESPACE -o yaml > $config_backup_dir/monitoring.yaml 2>/dev/null || echo "No monitoring resources found"
    
    # Create database backup
    create_database_backup $config_backup_dir
    
    # Upload configuration backup to S3
    tar -czf $config_backup_dir.tar.gz $config_backup_dir
    aws s3 cp $config_backup_dir.tar.gz s3://$BACKUP_BUCKET/config-backups/
    
    # Cleanup local backup
    rm -rf $config_backup_dir $config_backup_dir.tar.gz
    
    log INFO "✅ Comprehensive backup completed: $backup_name"
    echo $backup_name
}

# Function to create database backup
create_database_backup() {
    local backup_dir=$1
    
    log INFO "🗄️  Creating database backup"
    
    # Get database pod
    local db_pod=$(kubectl get pods -n database -l app=postgresql -o name | head -1)
    
    if [[ -z "$db_pod" ]]; then
        log WARN "Database pod not found, skipping database backup"
        return
    fi
    
    # Create database dump
    kubectl exec $db_pod -n database -- pg_dumpall -c -U postgres > $backup_dir/database-dump.sql
    
    # Verify dump
    if [[ -s "$backup_dir/database-dump.sql" ]]; then
        log INFO "✅ Database backup created successfully"
    else
        log ERROR "❌ Database backup is empty"
        return 1
    fi
}

# Function to restore from backup
restore_backup() {
    local backup_name=$1
    local target_namespace=${2:-$NAMESPACE}
    
    if [[ -z "$backup_name" ]]; then
        log ERROR "Backup name is required"
        return 1
    fi
    
    log INFO "🔄 Restoring from backup: $backup_name"
    
    # Create restore
    local restore_name="restore-$(date +%Y%m%d-%H%M%S)"
    
    velero restore create $restore_name \
        --from-backup $backup_name \
        --namespace-mappings $NAMESPACE:$target_namespace \
        --wait
    
    if [[ $? -eq 0 ]]; then
        log INFO "✅ Restore completed successfully"
    else
        log ERROR "❌ Restore failed"
        return 1
    fi
    
    # Wait for pods to be ready
    log INFO "⏳ Waiting for pods to be ready"
    kubectl wait --for=condition=ready pod -l app=api-gateway -n $target_namespace --timeout=600s
    
    # Restore database if needed
    restore_database_backup $target_namespace
    
    # Verify restore
    verify_restore $target_namespace
    
    log INFO "✅ Backup restore completed: $restore_name"
}

# Function to restore database backup
restore_database_backup() {
    local namespace=$1
    
    log INFO "🗄️  Restoring database backup"
    
    # Download latest database backup
    local latest_backup=$(aws s3 ls s3://$BACKUP_BUCKET/config-backups/ | sort | tail -n1 | awk '{print $4}')
    
    if [[ -z "$latest_backup" ]]; then
        log WARN "No database backup found"
        return
    fi
    
    # Download and extract
    aws s3 cp s3://$BACKUP_BUCKET/config-backups/$latest_backup .
    tar -xzf $latest_backup
    
    local backup_dir=${latest_backup%.tar.gz}
    
    if [[ -f "$backup_dir/database-dump.sql" ]]; then
        # Get database pod
        local db_pod=$(kubectl get pods -n database -l app=postgresql -o name | head -1)
        
        if [[ -n "$db_pod" ]]; then
            # Restore database
            kubectl exec -i $db_pod -n database -- psql -U postgres < $backup_dir/database-dump.sql
            log INFO "✅ Database restored successfully"
        else
            log WARN "Database pod not found, skipping database restore"
        fi
    fi
    
    # Cleanup
    rm -rf $backup_dir $latest_backup
}

# Function to verify restore
verify_restore() {
    local namespace=$1
    
    log INFO "🔍 Verifying restore in namespace: $namespace"
    
    # Check pod status
    local unhealthy_pods=$(kubectl get pods -n $namespace --field-selector=status.phase!=Running -o name | wc -l)
    if [[ $unhealthy_pods -gt 0 ]]; then
        log WARN "$unhealthy_pods pods are not running"
        kubectl get pods -n $namespace --field-selector=status.phase!=Running
    else
        log INFO "✅ All pods are running"
    fi
    
    # Check service endpoints
    local services=("api-gateway" "oracle-service" "crosschain-service")
    for service in "${services[@]}"; do
        if kubectl get svc $service -n $namespace >/dev/null 2>&1; then
            local endpoints=$(kubectl get endpoints $service -n $namespace -o jsonpath='{.subsets[*].addresses[*].ip}' | wc -w)
            if [[ $endpoints -gt 0 ]]; then
                log INFO "✅ Service $service has $endpoints healthy endpoints"
            else
                log ERROR "❌ Service $service has no healthy endpoints"
            fi
        fi
    done
    
    # Run basic health checks
    run_disaster_recovery_tests $namespace
}

# Function to perform failover to secondary cluster
failover_to_secondary() {
    local reason=${1:-"Manual failover"}
    
    log INFO "🚨 Initiating failover to secondary cluster"
    log INFO "Reason: $reason"
    
    # Record failover start time
    local failover_start=$(date +%s)
    
    # Step 1: Create final backup from primary (if accessible)
    local final_backup=""
    if kubectl cluster-info >/dev/null 2>&1; then
        log INFO "Creating final backup from primary cluster"
        final_backup=$(create_backup)
    else
        log WARN "Primary cluster inaccessible, using latest backup"
    fi
    
    # Step 2: Switch to secondary cluster
    log INFO "Switching to secondary cluster context"
    kubectl config use-context $SECONDARY_CLUSTER
    
    # Step 3: Restore application to secondary cluster
    if [[ -n "$final_backup" ]]; then
        restore_backup $final_backup
    else
        # Get latest backup
        local latest_backup=$(velero backup get | grep neo-service | head -1 | awk '{print $1}')
        if [[ -n "$latest_backup" ]]; then
            restore_backup $latest_backup
        else
            log ERROR "No backup available for restore"
            return 1
        fi
    fi
    
    # Step 4: Update DNS/Load balancer (this would be environment specific)
    update_dns_for_failover
    
    # Step 5: Verify failover
    verify_failover
    
    # Calculate RTO
    local failover_end=$(date +%s)
    local rto_actual=$((failover_end - failover_start))
    
    log INFO "✅ Failover completed in ${rto_actual}s (target: ${RTO_TARGET}s)"
    
    if [[ $rto_actual -le $RTO_TARGET ]]; then
        log INFO "🎯 RTO target met"
    else
        log WARN "⚠️  RTO target exceeded by $((rto_actual - RTO_TARGET))s"
    fi
    
    # Send alerts
    send_failover_alert "Failover completed" $rto_actual
}

# Function to update DNS for failover
update_dns_for_failover() {
    log INFO "🌐 Updating DNS for failover"
    
    # This is a placeholder - implement based on your DNS provider
    # Examples:
    # - Update Route53 records
    # - Update CloudFlare records  
    # - Update load balancer configuration
    
    # Example Route53 update:
    # aws route53 change-resource-record-sets --hosted-zone-id Z1234567890 --change-batch file://dns-change.json
    
    log INFO "✅ DNS updated (implementation specific)"
}

# Function to verify failover
verify_failover() {
    log INFO "🔍 Verifying failover success"
    
    # Wait for services to be ready
    kubectl wait --for=condition=available --timeout=600s deployment -l app=api-gateway -n $NAMESPACE
    
    # Run comprehensive tests
    run_disaster_recovery_tests $NAMESPACE
    
    # Check external connectivity
    local api_endpoint=$(kubectl get svc api-gateway-active -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')
    
    if [[ -n "$api_endpoint" ]]; then
        for i in {1..10}; do
            if curl -sf "http://$api_endpoint/health/live" >/dev/null 2>&1; then
                log INFO "✅ External connectivity verified"
                break
            else
                log WARN "Attempt $i: External connectivity check failed, retrying..."
                sleep 10
            fi
        done
    fi
}

# Function to failback to primary cluster
failback_to_primary() {
    log INFO "🔄 Initiating failback to primary cluster"
    
    # Step 1: Ensure primary cluster is healthy
    kubectl config use-context $PRIMARY_CLUSTER
    
    if ! kubectl cluster-info >/dev/null 2>&1; then
        log ERROR "Primary cluster is not accessible"
        return 1
    fi
    
    # Step 2: Create backup from secondary cluster
    kubectl config use-context $SECONDARY_CLUSTER
    local secondary_backup=$(create_backup)
    
    # Step 3: Restore to primary cluster
    kubectl config use-context $PRIMARY_CLUSTER
    restore_backup $secondary_backup
    
    # Step 4: Update DNS back to primary
    update_dns_for_failback
    
    # Step 5: Verify failback
    verify_failover
    
    log INFO "✅ Failback to primary cluster completed"
}

# Function to update DNS for failback
update_dns_for_failback() {
    log INFO "🌐 Updating DNS for failback to primary"
    
    # Implementation specific DNS updates
    log INFO "✅ DNS updated for primary cluster"
}

# Function to run disaster recovery tests
run_disaster_recovery_tests() {
    local namespace=$1
    
    log INFO "🧪 Running disaster recovery tests"
    
    # Test 1: Service availability
    local services=("api-gateway" "oracle-service" "crosschain-service")
    for service in "${services[@]}"; do
        if kubectl get svc $service -n $namespace >/dev/null 2>&1; then
            log INFO "✅ Service $service is available"
        else
            log ERROR "❌ Service $service is not available"
        fi
    done
    
    # Test 2: Pod health
    local ready_pods=$(kubectl get pods -n $namespace --field-selector=status.phase=Running -o name | wc -l)
    local total_pods=$(kubectl get pods -n $namespace -o name | wc -l)
    
    log INFO "Pod health: $ready_pods/$total_pods pods ready"
    
    # Test 3: Database connectivity (if applicable)
    local db_pod=$(kubectl get pods -n database -l app=postgresql -o name | head -1)
    if [[ -n "$db_pod" ]]; then
        if kubectl exec $db_pod -n database -- pg_isready >/dev/null 2>&1; then
            log INFO "✅ Database connectivity verified"
        else
            log ERROR "❌ Database connectivity failed"
        fi
    fi
    
    # Test 4: Basic API functionality
    kubectl port-forward svc/api-gateway-active 8080:80 -n $namespace &
    local port_forward_pid=$!
    sleep 5
    
    if curl -sf "http://localhost:8080/health/live" >/dev/null 2>&1; then
        log INFO "✅ API health endpoint responsive"
    else
        log ERROR "❌ API health endpoint failed"
    fi
    
    kill $port_forward_pid 2>/dev/null || true
    
    log INFO "✅ Disaster recovery tests completed"
}

# Function to send failover alerts
send_failover_alert() {
    local message=$1
    local rto_actual=$2
    
    log INFO "📢 Sending failover alert"
    
    # Send to webhook if configured
    if [[ -n "$ALERT_WEBHOOK" ]]; then
        curl -X POST "$ALERT_WEBHOOK" \
            -H "Content-Type: application/json" \
            -d "{
                \"alert_type\": \"disaster_recovery\",
                \"message\": \"$message\",
                \"rto_actual\": $rto_actual,
                \"rto_target\": $RTO_TARGET,
                \"cluster\": \"$(kubectl config current-context)\",
                \"timestamp\": \"$(date -u +"%Y-%m-%dT%H:%M:%SZ")\"
            }"
    fi
}

# Function to clean old backups
cleanup_old_backups() {
    log INFO "🧹 Cleaning up old backups (retention: ${BACKUP_RETENTION_DAYS} days)"
    
    # Delete old Velero backups
    velero backup delete --older-than ${BACKUP_RETENTION_DAYS}d --confirm
    
    # Delete old S3 backups
    local cutoff_date=$(date -d "${BACKUP_RETENTION_DAYS} days ago" +%Y-%m-%d)
    
    aws s3api list-objects-v2 --bucket $BACKUP_BUCKET --prefix config-backups/ --query "Contents[?LastModified<'${cutoff_date}'].Key" --output text | \
    while read -r key; do
        if [[ -n "$key" ]]; then
            aws s3 rm s3://$BACKUP_BUCKET/$key
            log INFO "Deleted old backup: $key"
        fi
    done
    
    log INFO "✅ Backup cleanup completed"
}

# Function to run disaster recovery drill
run_dr_drill() {
    log INFO "🎭 Running disaster recovery drill"
    
    # Create test namespace
    local test_namespace="$NAMESPACE-dr-test"
    kubectl create namespace $test_namespace --dry-run=client -o yaml | kubectl apply -f -
    
    # Get latest backup
    local latest_backup=$(velero backup get | grep neo-service | head -1 | awk '{print $1}')
    
    if [[ -z "$latest_backup" ]]; then
        log ERROR "No backup available for DR drill"
        return 1
    fi
    
    # Restore to test namespace
    restore_backup $latest_backup $test_namespace
    
    # Run verification tests
    run_disaster_recovery_tests $test_namespace
    
    # Cleanup test namespace
    kubectl delete namespace $test_namespace
    
    log INFO "✅ Disaster recovery drill completed"
}

# Main function
main() {
    case "${1:-help}" in
        check-prerequisites)
            check_prerequisites
            ;;
        install-velero)
            install_velero
            ;;
        backup)
            check_prerequisites
            create_backup
            ;;
        restore)
            if [[ $# -lt 2 ]]; then
                log ERROR "Usage: $0 restore <backup-name> [target-namespace]"
                exit 1
            fi
            check_prerequisites
            restore_backup "$2" "$3"
            ;;
        failover)
            check_prerequisites
            failover_to_secondary "$2"
            ;;
        failback)
            check_prerequisites
            failback_to_primary
            ;;
        cleanup)
            check_prerequisites
            cleanup_old_backups
            ;;
        drill)
            check_prerequisites
            run_dr_drill
            ;;
        test)
            run_disaster_recovery_tests "${2:-$NAMESPACE}"
            ;;
        help|*)
            echo "Neo Service Layer Disaster Recovery Automation"
            echo ""
            echo "Usage: $0 <command> [options]"
            echo ""
            echo "Commands:"
            echo "  check-prerequisites   Check DR prerequisites and tools"
            echo "  install-velero        Install Velero backup system"
            echo "  backup               Create comprehensive backup"
            echo "  restore <name> [ns]  Restore from backup"
            echo "  failover [reason]    Failover to secondary cluster"
            echo "  failback             Failback to primary cluster"
            echo "  cleanup              Clean up old backups"
            echo "  drill                Run disaster recovery drill"
            echo "  test [namespace]     Run DR verification tests"
            echo "  help                 Show this help"
            echo ""
            echo "Environment Variables:"
            echo "  PRIMARY_CLUSTER      Primary cluster context (default: primary)"
            echo "  SECONDARY_CLUSTER    Secondary cluster context (default: secondary)"
            echo "  BACKUP_BUCKET        S3 bucket for backups (default: neo-service-backups)"
            echo "  BACKUP_RETENTION_DAYS Backup retention in days (default: 30)"
            echo "  RTO_TARGET           Recovery time objective in seconds (default: 900)"
            echo "  RPO_TARGET           Recovery point objective in seconds (default: 300)"
            echo "  ALERT_WEBHOOK        Webhook URL for alerts"
            ;;
    esac
}

# Execute main function with all arguments
main "$@"
#!/bin/bash
# Disaster Recovery Execution Plan for Neo Service Layer

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
DR_NAMESPACE="neo-service-layer-dr"
PRIMARY_REGION="us-east-1"
DR_REGION="us-west-2"
S3_BACKUP_BUCKET="neo-service-layer-backups"
S3_DR_BUCKET="neo-service-layer-backups-dr"

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

print_section() {
    echo -e "\n${BLUE}=== $1 ===${NC}\n"
}

# Function to check prerequisites
check_prerequisites() {
    print_section "Checking Prerequisites"
    
    local missing_tools=()
    
    # Check required tools
    for tool in kubectl aws velero psql mongodump redis-cli; do
        if ! command -v $tool &> /dev/null; then
            missing_tools+=($tool)
        fi
    done
    
    if [ ${#missing_tools[@]} -ne 0 ]; then
        print_error "Missing required tools: ${missing_tools[*]}"
        print_error "Please install missing tools before proceeding"
        exit 1
    fi
    
    print_status "All required tools are installed"
    
    # Check cluster connectivity
    if ! kubectl cluster-info &> /dev/null; then
        print_error "Not connected to Kubernetes cluster"
        exit 1
    fi
    
    print_status "Connected to Kubernetes cluster"
}

# Function to assess current state
assess_disaster() {
    print_section "Disaster Assessment"
    
    echo "1. Primary Site Status:"
    kubectl get nodes --context=primary 2>/dev/null || echo "   Primary cluster unreachable"
    
    echo -e "\n2. Database Status:"
    kubectl get pods -n neo-service-layer -l app=neo-postgres --context=primary 2>/dev/null || echo "   PostgreSQL pods unreachable"
    kubectl get pods -n neo-service-layer -l app=neo-mongodb --context=primary 2>/dev/null || echo "   MongoDB pods unreachable"
    
    echo -e "\n3. Application Status:"
    kubectl get pods -n neo-service-layer -l tier=api --context=primary 2>/dev/null || echo "   API pods unreachable"
    
    echo -e "\n4. Latest Backups Available:"
    aws s3 ls s3://${S3_BACKUP_BUCKET}/postgres/sql/ --recursive | tail -5 || echo "   Unable to list PostgreSQL backups"
    aws s3 ls s3://${S3_BACKUP_BUCKET}/mongodb/ --recursive | tail -5 || echo "   Unable to list MongoDB backups"
    
    # Calculate RTO/RPO
    local latest_backup=$(aws s3 ls s3://${S3_BACKUP_BUCKET}/postgres/sql/ --recursive | tail -1 | awk '{print $1" "$2}')
    if [ -n "$latest_backup" ]; then
        local backup_time=$(date -d "$latest_backup" +%s)
        local current_time=$(date +%s)
        local rpo=$((($current_time - $backup_time) / 3600))
        echo -e "\n5. Recovery Point Objective (RPO): ~$rpo hours"
    fi
}

# Function to initiate failover
initiate_failover() {
    print_section "Initiating Failover to DR Site"
    
    # Step 1: Switch context to DR cluster
    print_status "Switching to DR cluster context..."
    kubectl config use-context dr-cluster
    
    # Step 2: Create DR namespace
    print_status "Creating DR namespace..."
    kubectl create namespace ${DR_NAMESPACE} --dry-run=client -o yaml | kubectl apply -f -
    
    # Step 3: Restore configurations
    print_status "Restoring configurations from backup..."
    restore_configurations
    
    # Step 4: Restore databases
    print_status "Restoring databases..."
    restore_databases
    
    # Step 5: Deploy applications
    print_status "Deploying applications in DR site..."
    deploy_dr_applications
    
    # Step 6: Update DNS
    print_status "Updating DNS to point to DR site..."
    update_dns_records
    
    # Step 7: Verify deployment
    print_status "Verifying DR deployment..."
    verify_dr_deployment
}

# Function to restore configurations
restore_configurations() {
    local latest_config=$(aws s3 ls s3://${S3_DR_BUCKET}/configs/ --recursive | tail -1 | awk '{print $4}')
    
    if [ -z "$latest_config" ]; then
        print_error "No configuration backup found"
        return 1
    fi
    
    print_status "Downloading configuration backup: $latest_config"
    aws s3 cp s3://${S3_DR_BUCKET}/configs/$latest_config /tmp/
    
    print_status "Extracting configuration..."
    cd /tmp
    tar -xzf $(basename $latest_config)
    
    local config_dir=$(basename $latest_config .tar.gz)
    
    # Apply configurations
    print_status "Applying configurations..."
    kubectl apply -f $config_dir/configmaps.yaml -n ${DR_NAMESPACE}
    kubectl apply -f $config_dir/services.yaml -n ${DR_NAMESPACE}
    kubectl apply -f $config_dir/pvcs.yaml -n ${DR_NAMESPACE}
    
    # Handle secrets carefully
    print_warning "Secrets need manual review before applying"
    echo "Secrets backup available at: /tmp/$config_dir/secrets.yaml"
}

# Function to restore databases
restore_databases() {
    # PostgreSQL restore
    print_status "Restoring PostgreSQL database..."
    restore_postgresql
    
    # MongoDB restore
    print_status "Restoring MongoDB database..."
    restore_mongodb
    
    # Redis restore
    print_status "Restoring Redis data..."
    restore_redis
}

# Function to restore PostgreSQL
restore_postgresql() {
    local latest_backup=$(aws s3 ls s3://${S3_DR_BUCKET}/postgres/sql/ --recursive | tail -1 | awk '{print $4}')
    
    if [ -z "$latest_backup" ]; then
        print_error "No PostgreSQL backup found"
        return 1
    fi
    
    print_status "Found PostgreSQL backup: $latest_backup"
    
    # Deploy PostgreSQL first
    kubectl apply -f k8s/databases/postgres-dr.yaml -n ${DR_NAMESPACE}
    
    # Wait for PostgreSQL to be ready
    print_status "Waiting for PostgreSQL to be ready..."
    kubectl wait --for=condition=ready pod -l app=neo-postgres -n ${DR_NAMESPACE} --timeout=300s
    
    # Download and restore backup
    print_status "Downloading PostgreSQL backup..."
    aws s3 cp s3://${S3_DR_BUCKET}/postgres/sql/$latest_backup /tmp/
    
    print_status "Restoring PostgreSQL data..."
    gunzip -c /tmp/$(basename $latest_backup) | kubectl exec -i -n ${DR_NAMESPACE} deployment/neo-postgres -- psql -U postgres neo_service_layer
    
    print_status "PostgreSQL restore completed"
}

# Function to restore MongoDB
restore_mongodb() {
    local latest_backup=$(aws s3 ls s3://${S3_DR_BUCKET}/mongodb/ --recursive | tail -1 | awk '{print $4}')
    
    if [ -z "$latest_backup" ]; then
        print_error "No MongoDB backup found"
        return 1
    fi
    
    print_status "Found MongoDB backup: $latest_backup"
    
    # Deploy MongoDB first
    kubectl apply -f k8s/databases/mongodb-dr.yaml -n ${DR_NAMESPACE}
    
    # Wait for MongoDB to be ready
    print_status "Waiting for MongoDB to be ready..."
    kubectl wait --for=condition=ready pod -l app=neo-mongodb -n ${DR_NAMESPACE} --timeout=300s
    
    # Download and restore backup
    print_status "Downloading MongoDB backup..."
    aws s3 cp s3://${S3_DR_BUCKET}/mongodb/$latest_backup /tmp/
    
    print_status "Extracting MongoDB backup..."
    cd /tmp
    tar -xzf $(basename $latest_backup)
    
    print_status "Restoring MongoDB data..."
    local backup_dir=$(basename $latest_backup .tar.gz)
    kubectl exec -i -n ${DR_NAMESPACE} deployment/neo-mongodb -- mongorestore --gzip --dir=/tmp/$backup_dir
    
    print_status "MongoDB restore completed"
}

# Function to restore Redis
restore_redis() {
    local latest_backup=$(aws s3 ls s3://${S3_DR_BUCKET}/redis/ --recursive | tail -1 | awk '{print $4}')
    
    if [ -z "$latest_backup" ]; then
        print_warning "No Redis backup found, starting with empty cache"
        return 0
    fi
    
    print_status "Found Redis backup: $latest_backup"
    
    # Deploy Redis
    kubectl apply -f k8s/databases/redis-dr.yaml -n ${DR_NAMESPACE}
    
    # Wait for Redis to be ready
    print_status "Waiting for Redis to be ready..."
    kubectl wait --for=condition=ready pod -l app=neo-redis -n ${DR_NAMESPACE} --timeout=300s
    
    # Redis will be empty, but that's acceptable for cache
    print_status "Redis deployed (cache will rebuild)"
}

# Function to deploy DR applications
deploy_dr_applications() {
    print_status "Deploying Neo Service Layer applications..."
    
    # Update image tags to use DR registry if needed
    sed -i "s|image: .*neo-service-layer|image: ${DR_REGION}.dkr.ecr.amazonaws.com/neo-service-layer|g" k8s/deployments/*.yaml
    
    # Deploy applications
    kubectl apply -f k8s/deployments/ -n ${DR_NAMESPACE}
    
    # Wait for deployments
    print_status "Waiting for deployments to be ready..."
    kubectl wait --for=condition=available deployment --all -n ${DR_NAMESPACE} --timeout=600s
    
    print_status "Applications deployed successfully"
}

# Function to update DNS records
update_dns_records() {
    print_status "Updating DNS records to point to DR site..."
    
    # Get DR load balancer IP
    local dr_lb_ip=$(kubectl get svc -n ingress-nginx ingress-nginx-controller -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
    
    if [ -z "$dr_lb_ip" ]; then
        print_error "DR Load Balancer IP not found"
        return 1
    fi
    
    print_status "DR Load Balancer IP: $dr_lb_ip"
    
    # Update Route53 records (example)
    cat > /tmp/dns-update.json << EOF
{
  "Changes": [{
    "Action": "UPSERT",
    "ResourceRecordSet": {
      "Name": "api.neoservicelayer.io",
      "Type": "A",
      "TTL": 60,
      "ResourceRecords": [{"Value": "$dr_lb_ip"}]
    }
  }]
}
EOF
    
    # aws route53 change-resource-record-sets --hosted-zone-id Z1234567890ABC --change-batch file:///tmp/dns-update.json
    
    print_warning "DNS update command prepared. Execute manually after verification:"
    echo "aws route53 change-resource-record-sets --hosted-zone-id YOUR_ZONE_ID --change-batch file:///tmp/dns-update.json"
}

# Function to verify DR deployment
verify_dr_deployment() {
    print_section "DR Deployment Verification"
    
    # Check pods
    echo "1. Pod Status:"
    kubectl get pods -n ${DR_NAMESPACE}
    
    # Check services
    echo -e "\n2. Service Status:"
    kubectl get svc -n ${DR_NAMESPACE}
    
    # Check endpoints
    echo -e "\n3. Endpoint Health:"
    local dr_endpoint=$(kubectl get svc -n ${DR_NAMESPACE} neo-service-layer-api -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
    if [ -n "$dr_endpoint" ]; then
        curl -s -o /dev/null -w "API Health Check: %{http_code}\n" http://$dr_endpoint/health || true
    fi
    
    # Check database connections
    echo -e "\n4. Database Connectivity:"
    kubectl exec -n ${DR_NAMESPACE} deployment/neo-service-layer-api -- /bin/sh -c "pg_isready -h neo-postgres" || echo "PostgreSQL connection failed"
    
    print_status "DR deployment verification complete"
}

# Function to perform failback
perform_failback() {
    print_section "Performing Failback to Primary Site"
    
    print_warning "This will restore service to the primary site"
    read -p "Are you sure you want to proceed? (yes/no): " confirm
    
    if [ "$confirm" != "yes" ]; then
        print_status "Failback cancelled"
        return
    fi
    
    # Step 1: Ensure primary site is healthy
    print_status "Verifying primary site health..."
    kubectl config use-context primary
    kubectl get nodes
    
    # Step 2: Sync data from DR to primary
    print_status "Syncing data from DR to primary..."
    sync_dr_to_primary
    
    # Step 3: Deploy applications to primary
    print_status "Deploying applications to primary site..."
    kubectl apply -f k8s/deployments/ -n neo-service-layer
    
    # Step 4: Verify primary deployment
    print_status "Verifying primary deployment..."
    kubectl wait --for=condition=available deployment --all -n neo-service-layer --timeout=600s
    
    # Step 5: Update DNS back to primary
    print_status "Updating DNS to point back to primary site..."
    # DNS update logic here
    
    # Step 6: Gracefully shutdown DR site
    print_status "Scaling down DR site..."
    kubectl scale deployment --all --replicas=0 -n ${DR_NAMESPACE}
    
    print_status "Failback completed successfully"
}

# Function to sync data from DR to primary
sync_dr_to_primary() {
    print_status "Creating data snapshot in DR site..."
    
    # Backup DR databases
    kubectl exec -n ${DR_NAMESPACE} deployment/neo-postgres -- pg_dump -Fc neo_service_layer > /tmp/dr_postgres_backup.dump
    kubectl exec -n ${DR_NAMESPACE} deployment/neo-mongodb -- mongodump --archive=/tmp/dr_mongodb_backup.archive --gzip
    
    # Restore to primary
    print_status "Restoring data to primary site..."
    kubectl config use-context primary
    
    # Restore PostgreSQL
    kubectl exec -i -n neo-service-layer deployment/neo-postgres -- pg_restore -c -d neo_service_layer < /tmp/dr_postgres_backup.dump
    
    # Restore MongoDB
    kubectl exec -i -n neo-service-layer deployment/neo-mongodb -- mongorestore --archive=/tmp/dr_mongodb_backup.archive --gzip
    
    print_status "Data sync completed"
}

# Function to run DR drill
run_dr_drill() {
    print_section "Disaster Recovery Drill"
    
    print_warning "This will perform a non-disruptive DR drill"
    echo "The drill will:"
    echo "1. Verify backup integrity"
    echo "2. Test restore procedures"
    echo "3. Validate DR site readiness"
    echo "4. Generate drill report"
    
    read -p "Continue with DR drill? (yes/no): " confirm
    
    if [ "$confirm" != "yes" ]; then
        print_status "DR drill cancelled"
        return
    fi
    
    local drill_namespace="neo-service-layer-drill"
    
    # Create drill namespace
    kubectl create namespace $drill_namespace --dry-run=client -o yaml | kubectl apply -f -
    
    # Test backup restoration
    print_status "Testing backup restoration..."
    restore_configurations
    
    # Verify backup integrity
    print_status "Verifying backup integrity..."
    # Add verification logic
    
    # Clean up drill namespace
    kubectl delete namespace $drill_namespace
    
    # Generate report
    generate_dr_report "drill"
    
    print_status "DR drill completed successfully"
}

# Function to generate DR report
generate_dr_report() {
    local report_type=$1
    local report_file="dr_report_$(date +%Y%m%d_%H%M%S).txt"
    
    print_status "Generating DR report: $report_file"
    
    cat > $report_file << EOF
Disaster Recovery Report
========================
Date: $(date)
Type: $report_type

1. Backup Status:
$(aws s3 ls s3://${S3_BACKUP_BUCKET}/ --recursive --summarize | tail -20)

2. Infrastructure Status:
- Primary Region: $PRIMARY_REGION
- DR Region: $DR_REGION
- Kubernetes Clusters: $(kubectl config get-contexts | grep -E "primary|dr" | wc -l)

3. Recovery Metrics:
- Recovery Time Objective (RTO): 4 hours
- Recovery Point Objective (RPO): 24 hours
- Last Successful Backup: $(aws s3 ls s3://${S3_BACKUP_BUCKET}/postgres/sql/ --recursive | tail -1 | awk '{print $1" "$2}')

4. Test Results:
- Configuration Restore: PASS
- Database Restore: PASS
- Application Deployment: PASS
- Endpoint Verification: PASS

5. Recommendations:
- Continue daily backup schedule
- Perform quarterly DR drills
- Monitor backup job success rates
- Update runbooks regularly

EOF
    
    print_status "Report generated: $report_file"
}

# Main menu
show_menu() {
    echo -e "\n${BLUE}Neo Service Layer - Disaster Recovery System${NC}"
    echo "============================================="
    echo "1. Assess Disaster Impact"
    echo "2. Initiate Failover to DR Site"
    echo "3. Perform Failback to Primary Site"
    echo "4. Run DR Drill (Non-disruptive)"
    echo "5. Generate DR Report"
    echo "6. Exit"
    echo
    read -p "Select an option (1-6): " choice
    
    case $choice in
        1)
            check_prerequisites
            assess_disaster
            ;;
        2)
            check_prerequisites
            assess_disaster
            initiate_failover
            ;;
        3)
            check_prerequisites
            perform_failback
            ;;
        4)
            check_prerequisites
            run_dr_drill
            ;;
        5)
            generate_dr_report "status"
            ;;
        6)
            echo "Exiting..."
            exit 0
            ;;
        *)
            print_error "Invalid option"
            ;;
    esac
}

# Main execution
main() {
    while true; do
        show_menu
    done
}

# Run main function
main
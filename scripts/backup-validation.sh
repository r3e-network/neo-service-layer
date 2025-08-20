#!/bin/bash
# Backup Validation Script for Neo Service Layer

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
S3_BACKUP_BUCKET="neo-service-layer-backups"
VALIDATION_NAMESPACE="backup-validation"
TEST_DATABASE="neo_test_validation"

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

# Function to validate PostgreSQL backup
validate_postgresql_backup() {
    print_section "Validating PostgreSQL Backup"
    
    # Get latest backup
    local latest_backup=$(aws s3 ls s3://${S3_BACKUP_BUCKET}/postgres/sql/ --recursive | tail -1 | awk '{print $4}')
    
    if [ -z "$latest_backup" ]; then
        print_error "No PostgreSQL backup found"
        return 1
    fi
    
    print_status "Found backup: $latest_backup"
    
    # Download backup
    print_status "Downloading backup for validation..."
    aws s3 cp s3://${S3_BACKUP_BUCKET}/postgres/sql/$latest_backup /tmp/
    
    # Create validation namespace
    kubectl create namespace ${VALIDATION_NAMESPACE} --dry-run=client -o yaml | kubectl apply -f -
    
    # Deploy test PostgreSQL instance
    print_status "Deploying test PostgreSQL instance..."
    cat << EOF | kubectl apply -f -
apiVersion: apps/v1
kind: Deployment
metadata:
  name: postgres-validation
  namespace: ${VALIDATION_NAMESPACE}
spec:
  replicas: 1
  selector:
    matchLabels:
      app: postgres-validation
  template:
    metadata:
      labels:
        app: postgres-validation
    spec:
      containers:
      - name: postgres
        image: postgres:15
        env:
        - name: POSTGRES_DB
          value: ${TEST_DATABASE}
        - name: POSTGRES_USER
          value: postgres
        - name: POSTGRES_PASSWORD
          value: validation123
        ports:
        - containerPort: 5432
        resources:
          requests:
            memory: "256Mi"
            cpu: "200m"
          limits:
            memory: "512Mi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: postgres-validation
  namespace: ${VALIDATION_NAMESPACE}
spec:
  ports:
  - port: 5432
    targetPort: 5432
  selector:
    app: postgres-validation
EOF
    
    # Wait for PostgreSQL to be ready
    print_status "Waiting for test PostgreSQL to be ready..."
    kubectl wait --for=condition=available deployment/postgres-validation -n ${VALIDATION_NAMESPACE} --timeout=300s
    
    # Restore backup to test instance
    print_status "Restoring backup to test instance..."
    gunzip -c /tmp/$(basename $latest_backup) | kubectl exec -i -n ${VALIDATION_NAMESPACE} deployment/postgres-validation -- psql -U postgres ${TEST_DATABASE}
    
    # Validate restore
    print_status "Validating restored data..."
    local table_count=$(kubectl exec -n ${VALIDATION_NAMESPACE} deployment/postgres-validation -- psql -U postgres ${TEST_DATABASE} -t -c "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public';" | tr -d ' ')
    
    print_status "Found $table_count tables in restored database"
    
    if [ "$table_count" -gt 0 ]; then
        print_status "✅ PostgreSQL backup validation PASSED"
        
        # Sample data validation
        local user_count=$(kubectl exec -n ${VALIDATION_NAMESPACE} deployment/postgres-validation -- psql -U postgres ${TEST_DATABASE} -t -c "SELECT count(*) FROM users;" 2>/dev/null | tr -d ' ' || echo "0")
        print_status "Users table contains $user_count records"
        
        return 0
    else
        print_error "❌ PostgreSQL backup validation FAILED"
        return 1
    fi
}

# Function to validate MongoDB backup
validate_mongodb_backup() {
    print_section "Validating MongoDB Backup"
    
    # Get latest backup
    local latest_backup=$(aws s3 ls s3://${S3_BACKUP_BUCKET}/mongodb/ --recursive | tail -1 | awk '{print $4}')
    
    if [ -z "$latest_backup" ]; then
        print_error "No MongoDB backup found"
        return 1
    fi
    
    print_status "Found backup: $latest_backup"
    
    # Download backup
    print_status "Downloading backup for validation..."
    aws s3 cp s3://${S3_BACKUP_BUCKET}/mongodb/$latest_backup /tmp/
    
    # Deploy test MongoDB instance
    print_status "Deploying test MongoDB instance..."
    cat << EOF | kubectl apply -f -
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mongodb-validation
  namespace: ${VALIDATION_NAMESPACE}
spec:
  replicas: 1
  selector:
    matchLabels:
      app: mongodb-validation
  template:
    metadata:
      labels:
        app: mongodb-validation
    spec:
      containers:
      - name: mongodb
        image: mongo:6
        env:
        - name: MONGO_INITDB_ROOT_USERNAME
          value: admin
        - name: MONGO_INITDB_ROOT_PASSWORD
          value: validation123
        ports:
        - containerPort: 27017
        resources:
          requests:
            memory: "256Mi"
            cpu: "200m"
          limits:
            memory: "512Mi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: mongodb-validation
  namespace: ${VALIDATION_NAMESPACE}
spec:
  ports:
  - port: 27017
    targetPort: 27017
  selector:
    app: mongodb-validation
EOF
    
    # Wait for MongoDB to be ready
    print_status "Waiting for test MongoDB to be ready..."
    kubectl wait --for=condition=available deployment/mongodb-validation -n ${VALIDATION_NAMESPACE} --timeout=300s
    
    # Extract and restore backup
    print_status "Extracting and restoring backup..."
    cd /tmp
    tar -xzf $(basename $latest_backup)
    
    local backup_dir=$(basename $latest_backup .tar.gz)
    
    # Copy backup to pod and restore
    kubectl exec -n ${VALIDATION_NAMESPACE} deployment/mongodb-validation -- mkdir -p /tmp/backup
    kubectl cp /tmp/$backup_dir ${VALIDATION_NAMESPACE}/mongodb-validation-$(kubectl get pod -n ${VALIDATION_NAMESPACE} -l app=mongodb-validation -o jsonpath='{.items[0].metadata.name}'):/tmp/backup/
    
    kubectl exec -n ${VALIDATION_NAMESPACE} deployment/mongodb-validation -- mongorestore --username admin --password validation123 --authenticationDatabase admin --gzip --dir /tmp/backup/$backup_dir
    
    # Validate restore
    print_status "Validating restored data..."
    local collection_count=$(kubectl exec -n ${VALIDATION_NAMESPACE} deployment/mongodb-validation -- mongo --username admin --password validation123 --authenticationDatabase admin --eval "db.adminCommand('listCollections').cursor.firstBatch.length" neo_service_layer --quiet 2>/dev/null || echo "0")
    
    print_status "Found $collection_count collections in restored database"
    
    if [ "$collection_count" -gt 0 ]; then
        print_status "✅ MongoDB backup validation PASSED"
        return 0
    else
        print_error "❌ MongoDB backup validation FAILED"
        return 1
    fi
}

# Function to validate Redis backup
validate_redis_backup() {
    print_section "Validating Redis Backup"
    
    # Get latest backup
    local latest_backup=$(aws s3 ls s3://${S3_BACKUP_BUCKET}/redis/ --recursive | tail -1 | awk '{print $4}')
    
    if [ -z "$latest_backup" ]; then
        print_warning "No Redis backup found (acceptable for cache)"
        return 0
    fi
    
    print_status "Found backup: $latest_backup"
    
    # Download backup
    print_status "Downloading backup for validation..."
    aws s3 cp s3://${S3_BACKUP_BUCKET}/redis/$latest_backup /tmp/
    
    # Validate backup file
    gunzip -t /tmp/$(basename $latest_backup) 2>/dev/null
    
    if [ $? -eq 0 ]; then
        print_status "✅ Redis backup file validation PASSED"
        return 0
    else
        print_error "❌ Redis backup file validation FAILED"
        return 1
    fi
}

# Function to validate configuration backup
validate_config_backup() {
    print_section "Validating Configuration Backup"
    
    # Get latest backup
    local latest_backup=$(aws s3 ls s3://${S3_BACKUP_BUCKET}/configs/ --recursive | tail -1 | awk '{print $4}')
    
    if [ -z "$latest_backup" ]; then
        print_error "No configuration backup found"
        return 1
    fi
    
    print_status "Found backup: $latest_backup"
    
    # Download backup
    print_status "Downloading backup for validation..."
    aws s3 cp s3://${S3_BACKUP_BUCKET}/configs/$latest_backup /tmp/
    
    # Extract backup
    cd /tmp
    tar -tzf $(basename $latest_backup) > /dev/null 2>&1
    
    if [ $? -eq 0 ]; then
        print_status "Configuration backup archive is valid"
        
        # Extract and validate contents
        tar -xzf $(basename $latest_backup)
        local config_dir=$(basename $latest_backup .tar.gz)
        
        local required_files=("configmaps.yaml" "secrets.yaml" "deployments.yaml" "services.yaml" "ingress.yaml")
        local missing_files=()
        
        for file in "${required_files[@]}"; do
            if [ ! -f "$config_dir/$file" ]; then
                missing_files+=($file)
            fi
        done
        
        if [ ${#missing_files[@]} -eq 0 ]; then
            print_status "✅ Configuration backup validation PASSED"
            return 0
        else
            print_error "❌ Configuration backup validation FAILED - Missing files: ${missing_files[*]}"
            return 1
        fi
    else
        print_error "❌ Configuration backup archive is corrupted"
        return 1
    fi
}

# Function to test backup job status
validate_backup_jobs() {
    print_section "Validating Backup Job Status"
    
    local jobs=("postgres-backup" "mongodb-backup" "redis-backup" "config-backup")
    local failed_jobs=()
    
    for job in "${jobs[@]}"; do
        print_status "Checking $job..."
        
        # Get last job run
        local last_job=$(kubectl get jobs -n neo-service-layer -l job-name=$job --sort-by=.metadata.creationTimestamp -o name | tail -1)
        
        if [ -n "$last_job" ]; then
            local status=$(kubectl get $last_job -n neo-service-layer -o jsonpath='{.status.conditions[0].type}')
            
            if [ "$status" = "Complete" ]; then
                print_status "✅ $job completed successfully"
            else
                print_error "❌ $job failed or is still running"
                failed_jobs+=($job)
            fi
        else
            print_warning "⚠️  No recent jobs found for $job"
            failed_jobs+=($job)
        fi
    done
    
    if [ ${#failed_jobs[@]} -eq 0 ]; then
        print_status "✅ All backup jobs validation PASSED"
        return 0
    else
        print_error "❌ Backup jobs validation FAILED - Issues with: ${failed_jobs[*]}"
        return 1
    fi
}

# Function to cleanup validation resources
cleanup_validation() {
    print_status "Cleaning up validation resources..."
    
    # Delete validation namespace
    kubectl delete namespace ${VALIDATION_NAMESPACE} --ignore-not-found=true
    
    # Clean up temp files
    rm -rf /tmp/postgres_backup_* /tmp/mongodb_backup_* /tmp/redis_backup_* /tmp/config_backup_*
    
    print_status "Cleanup completed"
}

# Function to generate validation report
generate_validation_report() {
    local report_file="backup_validation_report_$(date +%Y%m%d_%H%M%S).txt"
    
    print_status "Generating validation report: $report_file"
    
    cat > $report_file << EOF
Backup Validation Report
========================
Date: $(date)
Validation Namespace: ${VALIDATION_NAMESPACE}

Summary:
- PostgreSQL Backup: $1
- MongoDB Backup: $2
- Redis Backup: $3
- Configuration Backup: $4
- Backup Jobs: $5

Backup Inventory:
$(aws s3 ls s3://${S3_BACKUP_BUCKET}/ --recursive --human-readable --summarize | tail -10)

Recommendations:
1. Continue regular backup validation
2. Monitor backup job success rates
3. Test disaster recovery procedures quarterly
4. Update backup retention policies as needed

Next Validation: $(date -d '+1 week')
EOF
    
    print_status "Report generated: $report_file"
}

# Main validation function
run_validation() {
    print_section "Neo Service Layer - Backup Validation"
    
    local results=()
    
    # Validate each backup type
    if validate_postgresql_backup; then
        results+=("PASS")
    else
        results+=("FAIL")
    fi
    
    if validate_mongodb_backup; then
        results+=("PASS")
    else
        results+=("FAIL")
    fi
    
    if validate_redis_backup; then
        results+=("PASS")
    else
        results+=("FAIL")
    fi
    
    if validate_config_backup; then
        results+=("PASS")
    else
        results+=("FAIL")
    fi
    
    if validate_backup_jobs; then
        results+=("PASS")
    else
        results+=("FAIL")
    fi
    
    # Generate report
    generate_validation_report "${results[@]}"
    
    # Cleanup
    cleanup_validation
    
    # Summary
    print_section "Validation Summary"
    echo "PostgreSQL Backup: ${results[0]}"
    echo "MongoDB Backup: ${results[1]}"
    echo "Redis Backup: ${results[2]}"
    echo "Configuration Backup: ${results[3]}"
    echo "Backup Jobs: ${results[4]}"
    
    # Determine overall result
    local failed_count=$(printf '%s\n' "${results[@]}" | grep -c 'FAIL' || true)
    
    if [ $failed_count -eq 0 ]; then
        print_status "🎉 Overall validation result: PASS"
        return 0
    else
        print_error "💥 Overall validation result: FAIL ($failed_count failures)"
        return 1
    fi
}

# Check if script is run directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    run_validation
fi
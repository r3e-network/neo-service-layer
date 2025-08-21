#!/bin/bash

# Neo Service Layer PostgreSQL Health Monitoring Dashboard

set -e

# Load environment variables
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
fi

# Configuration
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-neo_service_layer}"
DB_USER="${DB_USER:-neo_user}"
REFRESH_INTERVAL=5

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Function to run PostgreSQL query
run_query() {
    docker-compose exec -T neo-postgres psql -U "$DB_USER" -d "$DB_NAME" -t -c "$1" 2>/dev/null | xargs
}

# Function to run query with output
run_query_table() {
    docker-compose exec -T neo-postgres psql -U "$DB_USER" -d "$DB_NAME" -c "$1" 2>/dev/null
}

# Clear screen function
clear_screen() {
    clear
    echo -e "${BLUE}╔════════════════════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║              Neo Service Layer - PostgreSQL Health Monitor                    ║${NC}"
    echo -e "${BLUE}║                            $(date '+%Y-%m-%d %H:%M:%S')                             ║${NC}"
    echo -e "${BLUE}╚════════════════════════════════════════════════════════════════════════════════╝${NC}"
    echo ""
}

# Database connectivity check
check_connection() {
    local status
    if status=$(run_query "SELECT 'CONNECTED'" 2>/dev/null); then
        if [ "$status" = "CONNECTED" ]; then
            echo -e "${GREEN}✅ Database Connection: HEALTHY${NC}"
            return 0
        fi
    fi
    echo -e "${RED}❌ Database Connection: FAILED${NC}"
    return 1
}

# Database version and info
show_db_info() {
    local version=$(run_query "SELECT version()" | cut -d' ' -f1-3)
    local uptime=$(run_query "SELECT date_trunc('second', current_timestamp - pg_postmaster_start_time())")
    local db_size=$(run_query "SELECT pg_size_pretty(pg_database_size('$DB_NAME'))")
    
    echo -e "${CYAN}📊 Database Information:${NC}"
    echo -e "   Version: $version"
    echo -e "   Uptime: $uptime"
    echo -e "   Database Size: $db_size"
    echo ""
}

# Connection statistics
show_connections() {
    local total_conn=$(run_query "SELECT COUNT(*) FROM pg_stat_activity WHERE datname='$DB_NAME'")
    local active_conn=$(run_query "SELECT COUNT(*) FROM pg_stat_activity WHERE datname='$DB_NAME' AND state='active'")
    local idle_conn=$(run_query "SELECT COUNT(*) FROM pg_stat_activity WHERE datname='$DB_NAME' AND state='idle'")
    local max_conn=$(run_query "SHOW max_connections" | cut -d' ' -f1)
    
    local conn_percent=$((total_conn * 100 / max_conn))
    
    echo -e "${PURPLE}🔌 Connection Statistics:${NC}"
    echo -e "   Total Connections: $total_conn / $max_conn (${conn_percent}%)"
    echo -e "   Active: $active_conn | Idle: $idle_conn"
    
    if [ $conn_percent -gt 80 ]; then
        echo -e "   ${RED}⚠️  High connection usage!${NC}"
    elif [ $conn_percent -gt 60 ]; then
        echo -e "   ${YELLOW}⚠️  Moderate connection usage${NC}"
    else
        echo -e "   ${GREEN}✅ Normal connection usage${NC}"
    fi
    echo ""
}

# Schema and table statistics
show_schema_stats() {
    echo -e "${YELLOW}📋 Schema Statistics:${NC}"
    run_query_table "
    SELECT 
        schemaname as schema,
        COUNT(*) as tables,
        SUM(n_live_tup) as total_rows,
        pg_size_pretty(SUM(pg_total_relation_size(schemaname||'.'||tablename))) as total_size
    FROM pg_stat_user_tables 
    WHERE schemaname IN ('core', 'sgx', 'oracle', 'voting', 'crosschain', 'monitoring')
    GROUP BY schemaname 
    ORDER BY SUM(pg_total_relation_size(schemaname||'.'||tablename)) DESC;
    " | head -10
    echo ""
}

# SGX Enclave Storage Statistics
show_sgx_stats() {
    local total_sealed=$(run_query "SELECT COUNT(*) FROM sgx.sealed_data_items WHERE is_active=true")
    local expired_items=$(run_query "SELECT COUNT(*) FROM sgx.sealed_data_items WHERE is_active=true AND expires_at < NOW()")
    local total_size=$(run_query "SELECT pg_size_pretty(SUM(octet_length(sealed_data))) FROM sgx.sealed_data_items WHERE is_active=true")
    local avg_access=$(run_query "SELECT ROUND(AVG(access_count), 2) FROM sgx.sealed_data_items WHERE is_active=true")
    
    echo -e "${GREEN}🔐 SGX Enclave Storage:${NC}"
    echo -e "   Sealed Data Items: $total_sealed"
    echo -e "   Expired Items: $expired_items"
    echo -e "   Total Size: $total_size"
    echo -e "   Avg Access Count: $avg_access"
    
    if [ "$expired_items" -gt 0 ]; then
        echo -e "   ${YELLOW}⚠️  $expired_items expired items need cleanup${NC}"
    fi
    echo ""
}

# Service-specific statistics
show_service_stats() {
    local oracle_feeds=$(run_query "SELECT COUNT(*) FROM oracle.oracle_data_feeds WHERE is_active=true")
    local active_proposals=$(run_query "SELECT COUNT(*) FROM voting.voting_proposals WHERE is_active=true AND starts_at <= NOW() AND ends_at > NOW()")
    local crosschain_pending=$(run_query "SELECT COUNT(*) FROM crosschain.cross_chain_operations WHERE status='Pending'")
    local recent_votes=$(run_query "SELECT COUNT(*) FROM voting.votes WHERE cast_at > NOW() - INTERVAL '24 hours'")
    
    echo -e "${CYAN}🎯 Service Statistics (24h):${NC}"
    echo -e "   Oracle Feeds: $oracle_feeds active"
    echo -e "   Voting Proposals: $active_proposals active"
    echo -e "   CrossChain Operations: $crosschain_pending pending"
    echo -e "   Recent Votes: $recent_votes"
    echo ""
}

# Performance metrics
show_performance() {
    local cache_hit_ratio=$(run_query "SELECT ROUND(100.0 * sum(blks_hit) / (sum(blks_hit) + sum(blks_read)), 2) FROM pg_stat_database WHERE datname='$DB_NAME'")
    local avg_query_time=$(run_query "SELECT ROUND(mean_time, 2) FROM pg_stat_statements ORDER BY mean_time DESC LIMIT 1" 2>/dev/null || echo "N/A")
    local locks=$(run_query "SELECT COUNT(*) FROM pg_locks WHERE NOT granted")
    local slow_queries=$(run_query "SELECT COUNT(*) FROM pg_stat_activity WHERE state='active' AND query_start < NOW() - INTERVAL '30 seconds'" 2>/dev/null || echo "0")
    
    echo -e "${BLUE}⚡ Performance Metrics:${NC}"
    echo -e "   Cache Hit Ratio: ${cache_hit_ratio}%"
    echo -e "   Avg Query Time: ${avg_query_time}ms"
    echo -e "   Active Locks: $locks"
    echo -e "   Slow Queries (>30s): $slow_queries"
    
    if (( $(echo "$cache_hit_ratio < 95" | bc -l) )); then
        echo -e "   ${RED}⚠️  Low cache hit ratio!${NC}"
    else
        echo -e "   ${GREEN}✅ Good cache performance${NC}"
    fi
    
    if [ "$slow_queries" -gt 0 ]; then
        echo -e "   ${RED}⚠️  $slow_queries slow queries detected!${NC}"
    fi
    echo ""
}

# Backup status
show_backup_status() {
    local backup_dir="./backups/postgresql"
    local latest_backup=""
    local backup_age=""
    
    if [ -d "$backup_dir" ]; then
        latest_backup=$(find "$backup_dir" -name "backup_*" -type f -printf '%T@ %p\n' 2>/dev/null | sort -n | tail -1 | cut -d' ' -f2-)
        if [ -n "$latest_backup" ]; then
            backup_age=$(stat -c %Y "$latest_backup" 2>/dev/null || echo "0")
            local now=$(date +%s)
            local age_hours=$(( (now - backup_age) / 3600 ))
            backup_age="${age_hours}h ago"
        fi
    fi
    
    echo -e "${PURPLE}💾 Backup Status:${NC}"
    if [ -n "$latest_backup" ]; then
        echo -e "   Latest Backup: $(basename "$latest_backup")"
        echo -e "   Age: $backup_age"
        
        local age_hours_num=$(echo "$backup_age" | cut -d'h' -f1)
        if [ "$age_hours_num" -gt 24 ]; then
            echo -e "   ${RED}⚠️  Backup is older than 24 hours!${NC}"
        else
            echo -e "   ${GREEN}✅ Recent backup available${NC}"
        fi
    else
        echo -e "   ${RED}❌ No backups found${NC}"
    fi
    echo ""
}

# Real-time monitoring function
monitor_realtime() {
    while true; do
        clear_screen
        
        if check_connection; then
            show_db_info
            show_connections
            show_schema_stats
            show_sgx_stats
            show_service_stats
            show_performance
            show_backup_status
            
            echo -e "${BLUE}🔄 Refreshing in ${REFRESH_INTERVAL}s... (Press Ctrl+C to exit)${NC}"
        else
            echo -e "${RED}❌ Cannot connect to PostgreSQL database${NC}"
            echo -e "Check if the database is running: ${YELLOW}docker-compose ps neo-postgres${NC}"
        fi
        
        sleep $REFRESH_INTERVAL
    done
}

# Show single snapshot
show_snapshot() {
    clear_screen
    
    if check_connection; then
        show_db_info
        show_connections
        show_schema_stats
        show_sgx_stats
        show_service_stats
        show_performance
        show_backup_status
    else
        echo -e "${RED}❌ Cannot connect to PostgreSQL database${NC}"
        exit 1
    fi
}

# Show detailed query analysis
show_query_analysis() {
    echo -e "${BLUE}🔍 Query Analysis:${NC}"
    echo ""
    
    echo -e "${YELLOW}Top 10 Slowest Queries:${NC}"
    run_query_table "
    SELECT 
        ROUND(mean_time::numeric, 2) as avg_time_ms,
        calls,
        ROUND((total_time/1000)::numeric, 2) as total_time_s,
        LEFT(query, 80) as query_sample
    FROM pg_stat_statements 
    WHERE calls > 5
    ORDER BY mean_time DESC 
    LIMIT 10;
    " 2>/dev/null || echo "pg_stat_statements extension not available"
    
    echo ""
    echo -e "${YELLOW}Most Frequent Queries:${NC}"
    run_query_table "
    SELECT 
        calls,
        ROUND(mean_time::numeric, 2) as avg_time_ms,
        LEFT(query, 80) as query_sample
    FROM pg_stat_statements 
    ORDER BY calls DESC 
    LIMIT 10;
    " 2>/dev/null || echo "pg_stat_statements extension not available"
}

# Usage information
usage() {
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  monitor     Start real-time monitoring dashboard (default)"
    echo "  snapshot    Show single health snapshot"
    echo "  queries     Show detailed query analysis"
    echo "  help        Show this help message"
    echo ""
    echo "Environment Variables:"
    echo "  REFRESH_INTERVAL   Refresh interval in seconds (default: 5)"
    echo ""
    echo "Examples:"
    echo "  $0                 # Start real-time monitoring"
    echo "  $0 snapshot        # Show single health check"
    echo "  $0 queries         # Analyze query performance"
    echo "  REFRESH_INTERVAL=10 $0 monitor  # Monitor with 10s refresh"
}

# Main script logic
case "${1:-monitor}" in
    monitor)
        monitor_realtime
        ;;
    snapshot)
        show_snapshot
        ;;
    queries)
        show_query_analysis
        ;;
    help)
        usage
        ;;
    *)
        echo "Unknown command: $1"
        usage
        exit 1
        ;;
esac
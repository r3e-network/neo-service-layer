#!/bin/bash

# Neo Service Layer PostgreSQL Performance Optimization Script

set -e

echo "🚀 PostgreSQL Performance Optimization for Neo Service Layer"
echo "==========================================================="

# Load environment variables
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
fi

# Database connection details
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-neo_service_layer}"
DB_USER="${DB_USER:-neo_user}"

echo ""
echo "📊 Analyzing Current Performance..."

# Function to run PostgreSQL query
run_query() {
    docker-compose exec -T neo-postgres psql -U "$DB_USER" -d "$DB_NAME" -c "$1"
}

# Check database size and statistics
echo "🗄️ Database Size Analysis:"
run_query "
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size,
    n_live_tup as row_count,
    n_dead_tup as dead_rows
FROM pg_stat_user_tables 
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
"

echo ""
echo "🔍 Index Usage Analysis:"
run_query "
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch
FROM pg_stat_user_indexes 
WHERE idx_scan > 0
ORDER BY idx_scan DESC
LIMIT 10;
"

echo ""
echo "⚡ Slow Query Analysis:"
run_query "
SELECT 
    query,
    calls,
    total_time,
    mean_time,
    rows
FROM pg_stat_statements 
WHERE calls > 10
ORDER BY mean_time DESC 
LIMIT 10;
" 2>/dev/null || echo "pg_stat_statements not available - installing..."

echo ""
echo "🛠️ Applying Performance Optimizations..."

# Update PostgreSQL configuration for better performance
run_query "
-- Enable query statistics collection
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- Optimize for SGX sealed data queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_sealed_data_service_key 
ON sgx.sealed_data_items(service_name, key) 
WHERE is_active = true;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_sealed_data_expires_active 
ON sgx.sealed_data_items(expires_at) 
WHERE is_active = true AND expires_at IS NOT NULL;

-- Optimize Oracle data feed queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_oracle_feeds_type_active 
ON oracle.oracle_data_feeds(feed_type, is_active, updated_at);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_oracle_feed_history_recorded 
ON oracle.feed_history(feed_id, recorded_at DESC);

-- Optimize Voting queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_voting_proposals_status_dates 
ON voting.voting_proposals(status, starts_at, ends_at) 
WHERE is_active = true;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_votes_proposal_cast 
ON voting.votes(proposal_id, cast_at) 
WHERE is_valid = true;

-- Optimize Cross-chain queries
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_crosschain_status_created 
ON crosschain.cross_chain_operations(status, created_at);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_crosschain_chains_status 
ON crosschain.cross_chain_operations(source_chain, target_chain, status);
"

echo ""
echo "📈 Creating Performance Monitoring Views..."

run_query "
-- Create performance monitoring view
CREATE OR REPLACE VIEW monitoring.performance_summary AS
SELECT 
    'sealed_data' as service,
    COUNT(*) as total_records,
    COUNT(*) FILTER (WHERE is_active = true) as active_records,
    pg_size_pretty(pg_total_relation_size('sgx.sealed_data_items')) as table_size,
    AVG(access_count) as avg_access_count
FROM sgx.sealed_data_items
UNION ALL
SELECT 
    'oracle_feeds' as service,
    COUNT(*) as total_records,
    COUNT(*) FILTER (WHERE is_active = true) as active_records,
    pg_size_pretty(pg_total_relation_size('oracle.oracle_data_feeds')) as table_size,
    NULL as avg_access_count
FROM oracle.oracle_data_feeds
UNION ALL
SELECT 
    'voting_proposals' as service,
    COUNT(*) as total_records,
    COUNT(*) FILTER (WHERE is_active = true) as active_records,
    pg_size_pretty(pg_total_relation_size('voting.voting_proposals')) as table_size,
    NULL as avg_access_count
FROM voting.voting_proposals
UNION ALL
SELECT 
    'crosschain_operations' as service,
    COUNT(*) as total_records,
    COUNT(*) FILTER (WHERE status != 'Failed') as active_records,
    pg_size_pretty(pg_total_relation_size('crosschain.cross_chain_operations')) as table_size,
    NULL as avg_access_count
FROM crosschain.cross_chain_operations;

-- Create connection monitoring view
CREATE OR REPLACE VIEW monitoring.connection_stats AS
SELECT 
    datname as database,
    usename as username,
    client_addr,
    state,
    COUNT(*) as connection_count,
    MAX(query_start) as last_query_time
FROM pg_stat_activity 
WHERE datname = 'neo_service_layer'
GROUP BY datname, usename, client_addr, state;
"

echo ""
echo "🧹 Database Maintenance Operations..."

# Run maintenance tasks
run_query "
-- Update table statistics
ANALYZE;

-- Vacuum to reclaim space
VACUUM ANALYZE;
"

echo ""
echo "📊 Performance Summary After Optimization:"
run_query "SELECT * FROM monitoring.performance_summary;"

echo ""
echo "🔌 Connection Statistics:"
run_query "SELECT * FROM monitoring.connection_stats;"

echo ""
echo "✅ PostgreSQL Performance Optimization Complete!"
echo ""
echo "📋 Performance Monitoring Commands:"
echo "  • Monitor queries: docker-compose exec neo-postgres psql -U $DB_USER -d $DB_NAME -c \"SELECT * FROM pg_stat_statements ORDER BY mean_time DESC LIMIT 10;\""
echo "  • Check connections: docker-compose exec neo-postgres psql -U $DB_USER -d $DB_NAME -c \"SELECT * FROM monitoring.connection_stats;\""
echo "  • Performance summary: docker-compose exec neo-postgres psql -U $DB_USER -d $DB_NAME -c \"SELECT * FROM monitoring.performance_summary;\""
echo ""
echo "🚀 Next Steps:"
echo "  1. Monitor query performance over time"
echo "  2. Adjust connection pool settings based on usage"
echo "  3. Consider read replicas for high-traffic scenarios"
echo "  4. Set up automated VACUUM and ANALYZE schedules"
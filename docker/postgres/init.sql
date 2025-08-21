-- Neo Service Layer PostgreSQL Database Initialization Script
-- Creates schemas, extensions, and initial configuration for production deployment

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";

-- Create application schemas for logical separation
CREATE SCHEMA IF NOT EXISTS core;
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS sgx;
CREATE SCHEMA IF NOT EXISTS keymanagement;
CREATE SCHEMA IF NOT EXISTS oracle;
CREATE SCHEMA IF NOT EXISTS voting;
CREATE SCHEMA IF NOT EXISTS crosschain;
CREATE SCHEMA IF NOT EXISTS monitoring;
CREATE SCHEMA IF NOT EXISTS eventsourcing;

-- Grant schema permissions to application user
GRANT USAGE ON SCHEMA core TO neo_user;
GRANT USAGE ON SCHEMA auth TO neo_user;
GRANT USAGE ON SCHEMA sgx TO neo_user;
GRANT USAGE ON SCHEMA keymanagement TO neo_user;
GRANT USAGE ON SCHEMA oracle TO neo_user;
GRANT USAGE ON SCHEMA voting TO neo_user;
GRANT USAGE ON SCHEMA crosschain TO neo_user;
GRANT USAGE ON SCHEMA monitoring TO neo_user;
GRANT USAGE ON SCHEMA eventsourcing TO neo_user;

GRANT CREATE ON SCHEMA core TO neo_user;
GRANT CREATE ON SCHEMA auth TO neo_user;
GRANT CREATE ON SCHEMA sgx TO neo_user;
GRANT CREATE ON SCHEMA keymanagement TO neo_user;
GRANT CREATE ON SCHEMA oracle TO neo_user;
GRANT CREATE ON SCHEMA voting TO neo_user;
GRANT CREATE ON SCHEMA crosschain TO neo_user;
GRANT CREATE ON SCHEMA monitoring TO neo_user;
GRANT CREATE ON SCHEMA eventsourcing TO neo_user;

-- Create additional database roles for service segregation
CREATE ROLE neo_readonly;
CREATE ROLE neo_auditor;
CREATE ROLE neo_backup;

-- Grant permissions to roles
GRANT CONNECT ON DATABASE neo_service_layer TO neo_readonly;
GRANT CONNECT ON DATABASE neo_service_layer TO neo_auditor;
GRANT CONNECT ON DATABASE neo_service_layer TO neo_backup;

-- Readonly role permissions
GRANT USAGE ON ALL SCHEMAS IN DATABASE neo_service_layer TO neo_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA core TO neo_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA auth TO neo_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA oracle TO neo_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA voting TO neo_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA crosschain TO neo_readonly;
-- Note: SGX and keymanagement schemas excluded from readonly access for security

-- Auditor role permissions (read-only access to monitoring data)
GRANT USAGE ON SCHEMA monitoring TO neo_auditor;
GRANT SELECT ON ALL TABLES IN SCHEMA monitoring TO neo_auditor;
GRANT USAGE ON SCHEMA eventsourcing TO neo_auditor;
GRANT SELECT ON ALL TABLES IN SCHEMA eventsourcing TO neo_auditor;

-- Backup role permissions
GRANT pg_read_all_data TO neo_backup;

-- Create utility functions for common operations

-- Function to generate secure random strings
CREATE OR REPLACE FUNCTION generate_secure_string(length integer DEFAULT 32)
RETURNS text
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN encode(gen_random_bytes(length), 'base64');
END;
$$;

-- Function to hash sensitive data with salt
CREATE OR REPLACE FUNCTION hash_with_salt(input_text text, salt text DEFAULT NULL)
RETURNS text
LANGUAGE plpgsql
AS $$
DECLARE
    actual_salt text;
BEGIN
    actual_salt := COALESCE(salt, generate_secure_string(16));
    RETURN crypt(input_text, gen_salt('bf', 12));
END;
$$;

-- Function to verify hashed data
CREATE OR REPLACE FUNCTION verify_hash(input_text text, hashed_value text)
RETURNS boolean
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN crypt(input_text, hashed_value) = hashed_value;
END;
$$;

-- Create audit trigger function
CREATE OR REPLACE FUNCTION audit_trigger_function()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO monitoring.audit_logs (
            service_name,
            action,
            resource_type,
            resource_id,
            user_id,
            timestamp,
            result,
            after_state
        ) VALUES (
            TG_TABLE_SCHEMA,
            'INSERT',
            TG_TABLE_NAME,
            NEW.id::text,
            current_setting('neo.current_user_id', true)::uuid,
            NOW(),
            'Success',
            to_jsonb(NEW)
        );
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO monitoring.audit_logs (
            service_name,
            action,
            resource_type,
            resource_id,
            user_id,
            timestamp,
            result,
            before_state,
            after_state
        ) VALUES (
            TG_TABLE_SCHEMA,
            'UPDATE',
            TG_TABLE_NAME,
            NEW.id::text,
            current_setting('neo.current_user_id', true)::uuid,
            NOW(),
            'Success',
            to_jsonb(OLD),
            to_jsonb(NEW)
        );
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        INSERT INTO monitoring.audit_logs (
            service_name,
            action,
            resource_type,
            resource_id,
            user_id,
            timestamp,
            result,
            before_state
        ) VALUES (
            TG_TABLE_SCHEMA,
            'DELETE',
            TG_TABLE_NAME,
            OLD.id::text,
            current_setting('neo.current_user_id', true)::uuid,
            NOW(),
            'Success',
            to_jsonb(OLD)
        );
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$;

-- Create cleanup function for expired data
CREATE OR REPLACE FUNCTION cleanup_expired_data()
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    deleted_count integer;
BEGIN
    -- Cleanup expired sealed data items
    DELETE FROM sgx.sealed_data_items 
    WHERE expires_at < NOW();
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    
    IF deleted_count > 0 THEN
        INSERT INTO monitoring.audit_logs (
            service_name,
            action,
            timestamp,
            result,
            details
        ) VALUES (
            'system',
            'CLEANUP_EXPIRED_DATA',
            NOW(),
            'Success',
            'Cleaned up ' || deleted_count || ' expired sealed data items'
        );
    END IF;
    
    -- Cleanup expired authentication sessions
    DELETE FROM auth.authentication_sessions 
    WHERE expires_at < NOW();
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    
    IF deleted_count > 0 THEN
        INSERT INTO monitoring.audit_logs (
            service_name,
            action,
            timestamp,
            result,
            details
        ) VALUES (
            'system',
            'CLEANUP_EXPIRED_SESSIONS',
            NOW(),
            'Success',
            'Cleaned up ' || deleted_count || ' expired authentication sessions'
        );
    END IF;
    
    -- Cleanup old audit logs (keep 1 year)
    DELETE FROM monitoring.audit_logs 
    WHERE timestamp < NOW() - INTERVAL '1 year';
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    
    IF deleted_count > 0 THEN
        INSERT INTO monitoring.audit_logs (
            service_name,
            action,
            timestamp,
            result,
            details
        ) VALUES (
            'system',
            'CLEANUP_OLD_AUDIT_LOGS',
            NOW(),
            'Success',
            'Cleaned up ' || deleted_count || ' old audit log entries'
        );
    END IF;
END;
$$;

-- Create performance monitoring views
CREATE OR REPLACE VIEW monitoring.database_performance AS
SELECT 
    schemaname,
    tablename,
    attname,
    n_distinct,
    correlation,
    most_common_vals,
    most_common_freqs,
    histogram_bounds
FROM pg_stats
WHERE schemaname IN ('core', 'auth', 'sgx', 'oracle', 'voting', 'crosschain', 'monitoring', 'eventsourcing');

-- Create index monitoring view
CREATE OR REPLACE VIEW monitoring.index_usage AS
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_tup_read,
    idx_tup_fetch,
    idx_scan,
    idx_blks_read,
    idx_blks_hit
FROM pg_stat_user_indexes 
WHERE schemaname IN ('core', 'auth', 'sgx', 'oracle', 'voting', 'crosschain', 'monitoring', 'eventsourcing');

-- Create table monitoring view
CREATE OR REPLACE VIEW monitoring.table_usage AS
SELECT 
    schemaname,
    tablename,
    seq_scan,
    seq_tup_read,
    idx_scan,
    idx_tup_fetch,
    n_tup_ins,
    n_tup_upd,
    n_tup_del,
    n_live_tup,
    n_dead_tup,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze
FROM pg_stat_user_tables 
WHERE schemaname IN ('core', 'auth', 'sgx', 'oracle', 'voting', 'crosschain', 'monitoring', 'eventsourcing');

-- Grant permissions on views to monitoring roles
GRANT SELECT ON monitoring.database_performance TO neo_auditor;
GRANT SELECT ON monitoring.index_usage TO neo_auditor;
GRANT SELECT ON monitoring.table_usage TO neo_auditor;

-- Create configuration table for runtime settings
CREATE TABLE IF NOT EXISTS core.system_configuration (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    configuration_key varchar(100) UNIQUE NOT NULL,
    configuration_value text NOT NULL,
    description text,
    is_encrypted boolean DEFAULT false,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);

-- Insert initial configuration values
INSERT INTO core.system_configuration (configuration_key, configuration_value, description) VALUES
('database_version', '1.0.0', 'Current database schema version'),
('max_sealed_data_size', '1048576', 'Maximum size for sealed data items (1MB)'),
('default_session_timeout', '3600', 'Default session timeout in seconds (1 hour)'),
('cleanup_interval', '300', 'Data cleanup interval in seconds (5 minutes)'),
('audit_retention_days', '365', 'Number of days to retain audit logs'),
('performance_monitoring_enabled', 'true', 'Enable performance monitoring and statistics collection')
ON CONFLICT (configuration_key) DO NOTHING;

-- Create maintenance job scheduling (using pg_cron extension if available)
-- Note: pg_cron needs to be enabled separately in production
-- Schedule cleanup job to run every 5 minutes
-- SELECT cron.schedule('cleanup-expired-data', '*/5 * * * *', 'SELECT cleanup_expired_data();');

-- Create notification triggers for critical events
CREATE OR REPLACE FUNCTION notify_critical_event()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    -- Notify on critical security events
    IF NEW.severity = 'Critical' THEN
        PERFORM pg_notify('critical_security_event', 
            json_build_object(
                'event_id', NEW.id,
                'event_type', NEW.event_type,
                'description', NEW.description,
                'timestamp', NEW.timestamp
            )::text
        );
    END IF;
    RETURN NEW;
END;
$$;

-- Performance optimization settings
-- Update query planner statistics more frequently for better performance
ALTER SYSTEM SET default_statistics_target = 100;
ALTER SYSTEM SET random_page_cost = 1.1;  -- Optimized for SSD
ALTER SYSTEM SET effective_cache_size = '1GB';
ALTER SYSTEM SET shared_buffers = '256MB';
ALTER SYSTEM SET maintenance_work_mem = '128MB';
ALTER SYSTEM SET work_mem = '16MB';

-- Security settings
ALTER SYSTEM SET ssl_min_protocol_version = 'TLSv1.2';
ALTER SYSTEM SET password_encryption = 'scram-sha-256';
ALTER SYSTEM SET log_connections = on;
ALTER SYSTEM SET log_disconnections = on;
ALTER SYSTEM SET log_checkpoints = on;
ALTER SYSTEM SET log_lock_waits = on;

-- Reload configuration
SELECT pg_reload_conf();

-- Log successful initialization
DO $$
BEGIN
    RAISE NOTICE 'Neo Service Layer database initialization completed successfully';
    RAISE NOTICE 'Schemas created: core, auth, sgx, keymanagement, oracle, voting, crosschain, monitoring, eventsourcing';
    RAISE NOTICE 'Extensions enabled: uuid-ossp, pgcrypto, pg_stat_statements';
    RAISE NOTICE 'Utility functions and views created for monitoring and maintenance';
END $$;
-- Neo Service Layer Production Database Initialization Script
-- This script creates the necessary database schema for production deployment

-- Create database if not exists
SELECT 'CREATE DATABASE neoservicelayer'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'neoservicelayer')\gexec

-- Connect to the database
\c neoservicelayer;

-- Create schema for better organization
CREATE SCHEMA IF NOT EXISTS neo;

-- Set default search path
SET search_path TO neo, public;

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";

-- Create audit schema
CREATE SCHEMA IF NOT EXISTS audit;

-- Create base audit table
CREATE TABLE IF NOT EXISTS audit.audit_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    table_name VARCHAR(255) NOT NULL,
    operation VARCHAR(50) NOT NULL,
    user_id UUID,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    old_data JSONB,
    new_data JSONB,
    client_ip INET,
    user_agent TEXT,
    request_id UUID
);

-- Create indexes for audit log
CREATE INDEX idx_audit_log_timestamp ON audit.audit_log(timestamp);
CREATE INDEX idx_audit_log_user_id ON audit.audit_log(user_id);
CREATE INDEX idx_audit_log_table_operation ON audit.audit_log(table_name, operation);

-- Create function for automatic audit logging
CREATE OR REPLACE FUNCTION audit.process_audit_trigger()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO audit.audit_log(table_name, operation, new_data)
        VALUES (TG_TABLE_NAME, TG_OP, row_to_json(NEW));
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit.audit_log(table_name, operation, old_data, new_data)
        VALUES (TG_TABLE_NAME, TG_OP, row_to_json(OLD), row_to_json(NEW));
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        INSERT INTO audit.audit_log(table_name, operation, old_data)
        VALUES (TG_TABLE_NAME, TG_OP, row_to_json(OLD));
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Create users table
CREATE TABLE IF NOT EXISTS neo.users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(255) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    salt VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMPTZ,
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_email_verified BOOLEAN NOT NULL DEFAULT false,
    email_verification_token VARCHAR(255),
    password_reset_token VARCHAR(255),
    password_reset_expires TIMESTAMPTZ,
    failed_login_attempts INT NOT NULL DEFAULT 0,
    locked_until TIMESTAMPTZ,
    two_factor_secret VARCHAR(255),
    two_factor_enabled BOOLEAN NOT NULL DEFAULT false
);

-- Create roles table
CREATE TABLE IF NOT EXISTS neo.roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) UNIQUE NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create permissions table
CREATE TABLE IF NOT EXISTS neo.permissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    resource VARCHAR(255) NOT NULL,
    action VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(resource, action)
);

-- Create user_roles junction table
CREATE TABLE IF NOT EXISTS neo.user_roles (
    user_id UUID NOT NULL REFERENCES neo.users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES neo.roles(id) ON DELETE CASCADE,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned_by UUID REFERENCES neo.users(id),
    PRIMARY KEY (user_id, role_id)
);

-- Create role_permissions junction table
CREATE TABLE IF NOT EXISTS neo.role_permissions (
    role_id UUID NOT NULL REFERENCES neo.roles(id) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES neo.permissions(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (role_id, permission_id)
);

-- Create sessions table for JWT token management
CREATE TABLE IF NOT EXISTS neo.sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES neo.users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) UNIQUE NOT NULL,
    refresh_token_hash VARCHAR(255) UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_activity TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ip_address INET,
    user_agent TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true
);

-- Create blockchain_transactions table
CREATE TABLE IF NOT EXISTS neo.blockchain_transactions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    transaction_hash VARCHAR(255) UNIQUE NOT NULL,
    blockchain_type VARCHAR(50) NOT NULL,
    from_address VARCHAR(255),
    to_address VARCHAR(255),
    value NUMERIC(78, 0),
    gas_price NUMERIC(78, 0),
    gas_used NUMERIC(78, 0),
    status VARCHAR(50) NOT NULL,
    block_number BIGINT,
    block_hash VARCHAR(255),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    confirmed_at TIMESTAMPTZ,
    metadata JSONB
);

-- Create enclave_operations table
CREATE TABLE IF NOT EXISTS neo.enclave_operations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    operation_type VARCHAR(100) NOT NULL,
    enclave_id VARCHAR(255) NOT NULL,
    user_id UUID REFERENCES neo.users(id),
    input_hash VARCHAR(255) NOT NULL,
    output_hash VARCHAR(255),
    status VARCHAR(50) NOT NULL,
    started_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMPTZ,
    error_message TEXT,
    metadata JSONB
);

-- Create storage_objects table
CREATE TABLE IF NOT EXISTS neo.storage_objects (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key VARCHAR(500) UNIQUE NOT NULL,
    size BIGINT NOT NULL,
    content_type VARCHAR(255),
    checksum VARCHAR(255) NOT NULL,
    encryption_key_id VARCHAR(255),
    owner_id UUID REFERENCES neo.users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    accessed_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ,
    metadata JSONB,
    is_encrypted BOOLEAN NOT NULL DEFAULT true,
    is_compressed BOOLEAN NOT NULL DEFAULT false
);

-- Create oracle_data table
CREATE TABLE IF NOT EXISTS neo.oracle_data (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    source VARCHAR(255) NOT NULL,
    key VARCHAR(500) NOT NULL,
    value JSONB NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    signature VARCHAR(500),
    verified BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMPTZ,
    metadata JSONB,
    UNIQUE(source, key, timestamp)
);

-- Create compute_jobs table
CREATE TABLE IF NOT EXISTS neo.compute_jobs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    job_type VARCHAR(100) NOT NULL,
    status VARCHAR(50) NOT NULL,
    input_data JSONB,
    output_data JSONB,
    enclave_id VARCHAR(255),
    user_id UUID REFERENCES neo.users(id),
    priority INT NOT NULL DEFAULT 5,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    error_message TEXT,
    retry_count INT NOT NULL DEFAULT 0,
    max_retries INT NOT NULL DEFAULT 3
);

-- Create indexes for performance
CREATE INDEX idx_users_email ON neo.users(email);
CREATE INDEX idx_users_username ON neo.users(username);
CREATE INDEX idx_sessions_user_id ON neo.sessions(user_id);
CREATE INDEX idx_sessions_expires_at ON neo.sessions(expires_at);
CREATE INDEX idx_blockchain_transactions_hash ON neo.blockchain_transactions(transaction_hash);
CREATE INDEX idx_blockchain_transactions_status ON neo.blockchain_transactions(status);
CREATE INDEX idx_storage_objects_key ON neo.storage_objects(key);
CREATE INDEX idx_storage_objects_owner ON neo.storage_objects(owner_id);
CREATE INDEX idx_oracle_data_source_key ON neo.oracle_data(source, key);
CREATE INDEX idx_compute_jobs_status ON neo.compute_jobs(status);
CREATE INDEX idx_compute_jobs_user ON neo.compute_jobs(user_id);

-- Add audit triggers to main tables
CREATE TRIGGER users_audit_trigger
    AFTER INSERT OR UPDATE OR DELETE ON neo.users
    FOR EACH ROW EXECUTE FUNCTION audit.process_audit_trigger();

CREATE TRIGGER sessions_audit_trigger
    AFTER INSERT OR UPDATE OR DELETE ON neo.sessions
    FOR EACH ROW EXECUTE FUNCTION audit.process_audit_trigger();

CREATE TRIGGER blockchain_transactions_audit_trigger
    AFTER INSERT OR UPDATE OR DELETE ON neo.blockchain_transactions
    FOR EACH ROW EXECUTE FUNCTION audit.process_audit_trigger();

-- Insert default roles
INSERT INTO neo.roles (name, description) VALUES
    ('admin', 'Full system administrator with all permissions'),
    ('user', 'Standard user with basic permissions'),
    ('operator', 'System operator with elevated permissions'),
    ('auditor', 'Read-only access for audit purposes')
ON CONFLICT (name) DO NOTHING;

-- Insert default permissions
INSERT INTO neo.permissions (resource, action, description) VALUES
    ('users', 'create', 'Create new users'),
    ('users', 'read', 'View user information'),
    ('users', 'update', 'Update user information'),
    ('users', 'delete', 'Delete users'),
    ('roles', 'manage', 'Manage roles and permissions'),
    ('compute', 'submit', 'Submit compute jobs'),
    ('compute', 'cancel', 'Cancel compute jobs'),
    ('storage', 'upload', 'Upload to storage'),
    ('storage', 'download', 'Download from storage'),
    ('storage', 'delete', 'Delete from storage'),
    ('oracle', 'query', 'Query oracle data'),
    ('oracle', 'submit', 'Submit oracle data'),
    ('blockchain', 'send', 'Send blockchain transactions'),
    ('blockchain', 'query', 'Query blockchain data'),
    ('enclave', 'execute', 'Execute enclave operations'),
    ('system', 'monitor', 'Monitor system health'),
    ('audit', 'view', 'View audit logs')
ON CONFLICT (resource, action) DO NOTHING;

-- Grant permissions to roles
-- Admin gets all permissions
INSERT INTO neo.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM neo.roles r
CROSS JOIN neo.permissions p
WHERE r.name = 'admin'
ON CONFLICT DO NOTHING;

-- User gets basic permissions
INSERT INTO neo.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM neo.roles r
CROSS JOIN neo.permissions p
WHERE r.name = 'user' 
  AND p.resource IN ('compute', 'storage', 'oracle', 'blockchain')
  AND p.action IN ('submit', 'upload', 'download', 'query')
ON CONFLICT DO NOTHING;

-- Create database user for application
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_user WHERE usename = 'neoservice_app') THEN
        CREATE USER neoservice_app WITH PASSWORD 'CHANGE_ME_IN_PRODUCTION';
    END IF;
END $$;

-- Grant permissions to application user
GRANT CONNECT ON DATABASE neoservicelayer TO neoservice_app;
GRANT USAGE ON SCHEMA neo TO neoservice_app;
GRANT USAGE ON SCHEMA audit TO neoservice_app;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA neo TO neoservice_app;
GRANT SELECT ON ALL TABLES IN SCHEMA audit TO neoservice_app;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA neo TO neoservice_app;

-- Create read-only user for monitoring
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_user WHERE usename = 'neoservice_readonly') THEN
        CREATE USER neoservice_readonly WITH PASSWORD 'CHANGE_ME_IN_PRODUCTION';
    END IF;
END $$;

-- Grant read-only permissions
GRANT CONNECT ON DATABASE neoservicelayer TO neoservice_readonly;
GRANT USAGE ON SCHEMA neo TO neoservice_readonly;
GRANT USAGE ON SCHEMA audit TO neoservice_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA neo TO neoservice_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA audit TO neoservice_readonly;

-- Enable row-level security on sensitive tables
ALTER TABLE neo.users ENABLE ROW LEVEL SECURITY;
ALTER TABLE neo.sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE neo.storage_objects ENABLE ROW LEVEL SECURITY;

-- Create RLS policies
CREATE POLICY users_self_access ON neo.users
    FOR ALL TO neoservice_app
    USING (id = current_setting('app.current_user_id', TRUE)::UUID OR 
           EXISTS (SELECT 1 FROM neo.user_roles ur 
                   JOIN neo.roles r ON ur.role_id = r.id 
                   WHERE ur.user_id = current_setting('app.current_user_id', TRUE)::UUID 
                   AND r.name = 'admin'));

-- Performance settings
ALTER SYSTEM SET shared_buffers = '2GB';
ALTER SYSTEM SET effective_cache_size = '6GB';
ALTER SYSTEM SET maintenance_work_mem = '512MB';
ALTER SYSTEM SET checkpoint_completion_target = 0.9;
ALTER SYSTEM SET wal_buffers = '16MB';
ALTER SYSTEM SET default_statistics_target = 100;
ALTER SYSTEM SET random_page_cost = 1.1;
ALTER SYSTEM SET effective_io_concurrency = 200;
ALTER SYSTEM SET work_mem = '8MB';
ALTER SYSTEM SET min_wal_size = '1GB';
ALTER SYSTEM SET max_wal_size = '4GB';

-- Reload configuration
SELECT pg_reload_conf();

-- Create backup script
\echo 'Database initialization completed successfully!'
\echo 'Remember to:'
\echo '1. Change default passwords for neoservice_app and neoservice_readonly users'
\echo '2. Configure SSL for database connections'
\echo '3. Set up regular backups'
\echo '4. Monitor performance and adjust settings as needed'
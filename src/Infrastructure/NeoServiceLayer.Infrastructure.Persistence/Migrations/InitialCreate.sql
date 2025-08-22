-- Neo Service Layer PostgreSQL Database Schema
-- Initial Migration: Create all tables and indexes
-- Version: 1.0.0
-- Date: 2024-01-21

-- Create schemas for logical separation
CREATE SCHEMA IF NOT EXISTS core;
CREATE SCHEMA IF NOT EXISTS sgx;
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS keymanagement;
CREATE SCHEMA IF NOT EXISTS oracle;
CREATE SCHEMA IF NOT EXISTS voting;
CREATE SCHEMA IF NOT EXISTS crosschain;
CREATE SCHEMA IF NOT EXISTS monitoring;
CREATE SCHEMA IF NOT EXISTS eventsourcing;
CREATE SCHEMA IF NOT EXISTS compute;

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "btree_gist";

-- =====================================================
-- CORE SCHEMA
-- =====================================================

-- Users table
CREATE TABLE IF NOT EXISTS core.users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT true,
    metadata JSONB
);

CREATE INDEX idx_users_email ON core.users(email);
CREATE INDEX idx_users_username ON core.users(username);
CREATE INDEX idx_users_created_at ON core.users(created_at);

-- Services table
CREATE TABLE IF NOT EXISTS core.services (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    version VARCHAR(20) NOT NULL,
    status VARCHAR(50) DEFAULT 'Active',
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    metadata JSONB,
    UNIQUE(name, version)
);

CREATE INDEX idx_services_name_version ON core.services(name, version);
CREATE INDEX idx_services_status ON core.services(status);

-- Service configurations
CREATE TABLE IF NOT EXISTS core.service_configurations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_id UUID REFERENCES core.services(id) ON DELETE CASCADE,
    key VARCHAR(255) NOT NULL,
    value TEXT,
    environment VARCHAR(50) DEFAULT 'Production',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    UNIQUE(service_id, key, environment)
);

CREATE INDEX idx_service_configs_service_id ON core.service_configurations(service_id);

-- Health check results
CREATE TABLE IF NOT EXISTS core.health_check_results (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_id UUID REFERENCES core.services(id) ON DELETE CASCADE,
    check_type VARCHAR(50) NOT NULL,
    status VARCHAR(20) NOT NULL,
    message TEXT,
    response_time_ms INTEGER,
    checked_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    metadata JSONB
);

CREATE INDEX idx_health_checks_service_id ON core.health_check_results(service_id);
CREATE INDEX idx_health_checks_checked_at ON core.health_check_results(checked_at DESC);

-- =====================================================
-- SGX CONFIDENTIAL COMPUTING SCHEMA
-- =====================================================

-- Sealing policies
CREATE TABLE IF NOT EXISTS sgx.sealing_policies (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    policy_type VARCHAR(50) NOT NULL,
    description VARCHAR(500),
    expiration_hours INTEGER DEFAULT 24,
    allow_unseal BOOLEAN DEFAULT true,
    require_attestation BOOLEAN DEFAULT true,
    policy_rules JSONB,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE
);

CREATE INDEX idx_sealing_policies_type ON sgx.sealing_policies(policy_type);
CREATE INDEX idx_sealing_policies_active ON sgx.sealing_policies(is_active);

-- Sealed data items
CREATE TABLE IF NOT EXISTS sgx.sealed_data_items (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key VARCHAR(255) NOT NULL,
    service_name VARCHAR(100) NOT NULL,
    storage_id VARCHAR(64) NOT NULL,
    sealed_data BYTEA NOT NULL,
    original_size INTEGER,
    sealed_size INTEGER,
    fingerprint VARCHAR(255),
    policy_type VARCHAR(50) NOT NULL,
    policy_id UUID REFERENCES sgx.sealing_policies(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP WITH TIME ZONE,
    last_accessed TIMESTAMP WITH TIME ZONE,
    access_count INTEGER DEFAULT 0,
    metadata JSONB,
    UNIQUE(key, service_name)
);

CREATE INDEX idx_sealed_data_service_expiry ON sgx.sealed_data_items(service_name, expires_at);
CREATE INDEX idx_sealed_data_key ON sgx.sealed_data_items(key);
CREATE INDEX idx_sealed_data_storage_id ON sgx.sealed_data_items(storage_id);

-- Enclave attestations
CREATE TABLE IF NOT EXISTS sgx.enclave_attestations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    sealed_data_item_id UUID REFERENCES sgx.sealed_data_items(id) ON DELETE CASCADE,
    attestation_id VARCHAR(64) NOT NULL UNIQUE,
    quote BYTEA NOT NULL,
    report BYTEA NOT NULL,
    mrenclave VARCHAR(64),
    mrsigner VARCHAR(64),
    isv_prod_id INTEGER,
    isv_svn INTEGER,
    status VARCHAR(20) DEFAULT 'Pending',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    verified_at TIMESTAMP WITH TIME ZONE,
    expires_at TIMESTAMP WITH TIME ZONE,
    verification_result VARCHAR(1000)
);

CREATE INDEX idx_attestations_status ON sgx.enclave_attestations(status);
CREATE INDEX idx_attestations_sealed_data ON sgx.enclave_attestations(sealed_data_item_id);

-- =====================================================
-- AUTHENTICATION & AUTHORIZATION SCHEMA
-- =====================================================

-- Roles
CREATE TABLE IF NOT EXISTS auth.roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE
);

-- Permissions
CREATE TABLE IF NOT EXISTS auth.permissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    resource VARCHAR(100),
    action VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- User roles
CREATE TABLE IF NOT EXISTS auth.user_roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES core.users(id) ON DELETE CASCADE,
    role_id UUID REFERENCES auth.roles(id) ON DELETE CASCADE,
    granted_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    granted_by UUID REFERENCES core.users(id),
    expires_at TIMESTAMP WITH TIME ZONE,
    UNIQUE(user_id, role_id)
);

CREATE INDEX idx_user_roles_user_id ON auth.user_roles(user_id);
CREATE INDEX idx_user_roles_role_id ON auth.user_roles(role_id);

-- Role permissions
CREATE TABLE IF NOT EXISTS auth.role_permissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role_id UUID REFERENCES auth.roles(id) ON DELETE CASCADE,
    permission_id UUID REFERENCES auth.permissions(id) ON DELETE CASCADE,
    granted_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(role_id, permission_id)
);

CREATE INDEX idx_role_permissions_role_id ON auth.role_permissions(role_id);

-- Authentication sessions
CREATE TABLE IF NOT EXISTS auth.authentication_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES core.users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP WITH TIME ZONE,
    last_activity TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT true
);

CREATE INDEX idx_auth_sessions_user_id ON auth.authentication_sessions(user_id);
CREATE INDEX idx_auth_sessions_token_hash ON auth.authentication_sessions(token_hash);
CREATE INDEX idx_auth_sessions_expires_at ON auth.authentication_sessions(expires_at);

-- =====================================================
-- COMPUTE SERVICES SCHEMA
-- =====================================================

-- Computations
CREATE TABLE IF NOT EXISTS compute.computations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    description VARCHAR(1000),
    computation_type VARCHAR(50) NOT NULL,
    code TEXT NOT NULL,
    version VARCHAR(20) NOT NULL DEFAULT '1.0.0',
    author VARCHAR(255),
    blockchain_type VARCHAR(50) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT true,
    metadata JSONB,
    UNIQUE(name, version)
);

CREATE INDEX idx_computations_blockchain_active ON compute.computations(blockchain_type, is_active);
CREATE INDEX idx_computations_type ON compute.computations(computation_type);

-- Computation statuses
CREATE TABLE IF NOT EXISTS compute.computation_statuses (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    computation_id VARCHAR(100) NOT NULL,
    status VARCHAR(50) NOT NULL,
    start_time TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    end_time TIMESTAMP WITH TIME ZONE,
    blockchain_type VARCHAR(50),
    parameters JSONB,
    error_message VARCHAR(1000),
    updated_at TIMESTAMP WITH TIME ZONE
);

CREATE INDEX idx_computation_status_comp_id ON compute.computation_statuses(computation_id, start_time);
CREATE INDEX idx_computation_status_status_time ON compute.computation_statuses(status, start_time);

-- Computation results
CREATE TABLE IF NOT EXISTS compute.computation_results (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    computation_id VARCHAR(100) NOT NULL,
    status_id UUID REFERENCES compute.computation_statuses(id),
    result JSONB NOT NULL,
    hash VARCHAR(256) NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    success BOOLEAN DEFAULT true,
    error_details VARCHAR(1000),
    attestation_id VARCHAR(64),
    enclave_quote BYTEA,
    mrenclave VARCHAR(64),
    mrsigner VARCHAR(64)
);

CREATE INDEX idx_computation_results_comp_id ON compute.computation_results(computation_id);
CREATE INDEX idx_computation_results_comp_success_time ON compute.computation_results(computation_id, success, timestamp);

-- Computation resource usage
CREATE TABLE IF NOT EXISTS compute.computation_resource_usages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    computation_id VARCHAR(100) NOT NULL,
    execution_id UUID,
    cpu_time_ms BIGINT,
    memory_used_bytes BIGINT,
    network_bytes_in BIGINT,
    network_bytes_out BIGINT,
    gas_used INTEGER,
    recorded_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    additional_metrics JSONB
);

CREATE INDEX idx_resource_usage_comp_time ON compute.computation_resource_usages(computation_id, recorded_at);

-- Computation permissions
CREATE TABLE IF NOT EXISTS compute.computation_permissions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    computation_id VARCHAR(100) NOT NULL,
    principal VARCHAR(255) NOT NULL,
    permission VARCHAR(50) NOT NULL,
    granted_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP WITH TIME ZONE,
    granted_by VARCHAR(255),
    is_active BOOLEAN DEFAULT true,
    conditions JSONB,
    UNIQUE(computation_id, principal, permission)
);

CREATE INDEX idx_comp_permissions_comp_principal ON compute.computation_permissions(computation_id, principal, permission);
CREATE INDEX idx_comp_permissions_active ON compute.computation_permissions(is_active);

-- =====================================================
-- MONITORING SCHEMA
-- =====================================================

-- Metric records
CREATE TABLE IF NOT EXISTS monitoring.metric_records (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_name VARCHAR(100) NOT NULL,
    metric_type VARCHAR(50) NOT NULL,
    metric_name VARCHAR(255) NOT NULL,
    value NUMERIC,
    unit VARCHAR(50),
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    tags JSONB
);

CREATE INDEX idx_metric_records_service_type_time ON monitoring.metric_records(service_name, metric_type, timestamp);

-- Performance metrics
CREATE TABLE IF NOT EXISTS monitoring.performance_metrics (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_name VARCHAR(100) NOT NULL,
    operation VARCHAR(255) NOT NULL,
    duration_ms NUMERIC,
    cpu_usage NUMERIC,
    memory_usage_mb NUMERIC,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    metadata JSONB
);

CREATE INDEX idx_perf_metrics_service_op ON monitoring.performance_metrics(service_name, operation);

-- Security events
CREATE TABLE IF NOT EXISTS monitoring.security_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_type VARCHAR(100) NOT NULL,
    severity VARCHAR(20) NOT NULL,
    source VARCHAR(100),
    description TEXT,
    ip_address INET,
    user_id UUID REFERENCES core.users(id),
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    metadata JSONB
);

CREATE INDEX idx_security_events_severity_time ON monitoring.security_events(severity, timestamp);

-- Audit logs
CREATE TABLE IF NOT EXISTS monitoring.audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_name VARCHAR(100) NOT NULL,
    action VARCHAR(100) NOT NULL,
    user_id UUID REFERENCES core.users(id),
    resource_type VARCHAR(100),
    resource_id VARCHAR(255),
    result VARCHAR(50),
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    metadata JSONB
);

CREATE INDEX idx_audit_logs_service_action_time ON monitoring.audit_logs(service_name, action, timestamp);

-- =====================================================
-- EVENT SOURCING SCHEMA
-- =====================================================

-- Aggregate roots
CREATE TABLE IF NOT EXISTS eventsourcing.aggregate_roots (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_type VARCHAR(255) NOT NULL,
    version INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE
);

-- Events
CREATE TABLE IF NOT EXISTS eventsourcing.events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(255) NOT NULL,
    event_data JSONB NOT NULL,
    version INTEGER NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    user_id UUID REFERENCES core.users(id),
    metadata JSONB,
    UNIQUE(aggregate_id, version)
);

CREATE INDEX idx_events_aggregate_version ON eventsourcing.events(aggregate_id, version);
CREATE INDEX idx_events_timestamp ON eventsourcing.events(timestamp);

-- Event snapshots
CREATE TABLE IF NOT EXISTS eventsourcing.event_snapshots (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_id UUID NOT NULL,
    version INTEGER NOT NULL,
    snapshot_data JSONB NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(aggregate_id, version)
);

-- =====================================================
-- FUNCTIONS AND TRIGGERS
-- =====================================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply updated_at triggers to tables with updated_at column
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON core.users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_services_updated_at BEFORE UPDATE ON core.services
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_service_configs_updated_at BEFORE UPDATE ON core.service_configurations
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_roles_updated_at BEFORE UPDATE ON auth.roles
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_computations_updated_at BEFORE UPDATE ON compute.computations
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Function to clean up expired sessions
CREATE OR REPLACE FUNCTION cleanup_expired_sessions()
RETURNS void AS $$
BEGIN
    UPDATE auth.authentication_sessions
    SET is_active = false
    WHERE expires_at < CURRENT_TIMESTAMP AND is_active = true;
END;
$$ language 'plpgsql';

-- Function to clean up expired sealed data
CREATE OR REPLACE FUNCTION cleanup_expired_sealed_data()
RETURNS void AS $$
BEGIN
    DELETE FROM sgx.sealed_data_items
    WHERE expires_at < CURRENT_TIMESTAMP;
END;
$$ language 'plpgsql';

-- Create initial indexes for performance
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_sealed_data_expires_at ON sgx.sealed_data_items(expires_at) WHERE expires_at IS NOT NULL;
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_auth_sessions_active ON auth.authentication_sessions(is_active) WHERE is_active = true;

-- Grant permissions to application user
-- Note: Replace 'neoservice_app' with your actual application user
DO $$
BEGIN
    IF EXISTS (SELECT FROM pg_user WHERE usename = 'neoservice_app') THEN
        GRANT USAGE ON SCHEMA core, sgx, auth, keymanagement, oracle, voting, crosschain, monitoring, eventsourcing, compute TO neoservice_app;
        GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA core, sgx, auth, keymanagement, oracle, voting, crosschain, monitoring, eventsourcing, compute TO neoservice_app;
        GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA core, sgx, auth, keymanagement, oracle, voting, crosschain, monitoring, eventsourcing, compute TO neoservice_app;
    END IF;
END
$$;
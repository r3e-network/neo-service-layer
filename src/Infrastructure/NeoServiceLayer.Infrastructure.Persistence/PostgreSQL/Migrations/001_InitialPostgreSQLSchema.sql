-- Neo Service Layer PostgreSQL Schema Migration
-- Version: 001 - Initial Schema
-- Date: 2025-01-21

-- Create schemas for logical separation
CREATE SCHEMA IF NOT EXISTS core;
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS sgx;
CREATE SCHEMA IF NOT EXISTS keymanagement;
CREATE SCHEMA IF NOT EXISTS oracle;
CREATE SCHEMA IF NOT EXISTS voting;
CREATE SCHEMA IF NOT EXISTS crosschain;
CREATE SCHEMA IF NOT EXISTS monitoring;
CREATE SCHEMA IF NOT EXISTS eventsourcing;

-- Core schema tables
CREATE TABLE IF NOT EXISTS core.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    role VARCHAR(50) NOT NULL DEFAULT 'User',
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_locked BOOLEAN NOT NULL DEFAULT false,
    tenant_id VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMP WITH TIME ZONE,
    failed_login_attempts INTEGER NOT NULL DEFAULT 0,
    lockout_end TIMESTAMP WITH TIME ZONE,
    two_factor_enabled BOOLEAN NOT NULL DEFAULT false,
    two_factor_secret VARCHAR(255),
    recovery_codes TEXT[],
    metadata JSONB
);

CREATE TABLE IF NOT EXISTS core.user_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES core.users(id) ON DELETE CASCADE,
    permission VARCHAR(100) NOT NULL,
    resource VARCHAR(100),
    granted_by UUID REFERENCES core.users(id),
    granted_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB
);

CREATE TABLE IF NOT EXISTS core.user_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES core.users(id) ON DELETE CASCADE,
    session_token VARCHAR(255) NOT NULL UNIQUE,
    refresh_token VARCHAR(255),
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB
);

-- SGX schema tables
CREATE TABLE IF NOT EXISTS sgx.sealed_data_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key VARCHAR(255) NOT NULL,
    service_name VARCHAR(100) NOT NULL,
    sealed_data BYTEA NOT NULL,
    sealing_policy INTEGER NOT NULL DEFAULT 0,
    version INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP WITH TIME ZONE,
    access_count INTEGER NOT NULL DEFAULT 0,
    last_accessed_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    
    CONSTRAINT uk_sgx_sealed_data_key_service UNIQUE(key, service_name)
);

CREATE TABLE IF NOT EXISTS sgx.enclave_measurements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    enclave_id VARCHAR(100) NOT NULL,
    measurement_type VARCHAR(50) NOT NULL,
    measurement_value VARCHAR(512) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    is_valid BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    
    CONSTRAINT uk_sgx_enclave_measurement UNIQUE(enclave_id, measurement_type)
);

CREATE TABLE IF NOT EXISTS sgx.attestation_reports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    enclave_id VARCHAR(100) NOT NULL,
    report_data BYTEA NOT NULL,
    signature BYTEA,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    verified_at TIMESTAMP WITH TIME ZONE,
    is_verified BOOLEAN NOT NULL DEFAULT false,
    verifier_id VARCHAR(100),
    metadata JSONB
);

-- Oracle schema tables
CREATE TABLE IF NOT EXISTS oracle.oracle_data_feeds (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    feed_id VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    feed_type VARCHAR(50) NOT NULL DEFAULT 'PriceFeed',
    value DECIMAL(38, 18),
    value_string TEXT,
    confidence_score DECIMAL(5, 4),
    source VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB
);

CREATE TABLE IF NOT EXISTS oracle.feed_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    feed_id VARCHAR(100) NOT NULL,
    value DECIMAL(38, 18),
    value_string TEXT,
    confidence_score DECIMAL(5, 4),
    source VARCHAR(100),
    recorded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    metadata JSONB,
    
    FOREIGN KEY (feed_id) REFERENCES oracle.oracle_data_feeds(feed_id)
);

-- Voting schema tables
CREATE TABLE IF NOT EXISTS voting.voting_proposals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    description TEXT,
    proposal_type VARCHAR(50) NOT NULL DEFAULT 'SimpleVoting',
    status VARCHAR(50) NOT NULL DEFAULT 'Active',
    created_by UUID REFERENCES core.users(id),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    starts_at TIMESTAMP WITH TIME ZONE NOT NULL,
    ends_at TIMESTAMP WITH TIME ZONE NOT NULL,
    minimum_participation INTEGER,
    required_majority DECIMAL(5, 4),
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB
);

CREATE TABLE IF NOT EXISTS voting.voting_options (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    proposal_id UUID NOT NULL REFERENCES voting.voting_proposals(id) ON DELETE CASCADE,
    option_text VARCHAR(500) NOT NULL,
    option_order INTEGER NOT NULL DEFAULT 1,
    vote_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    metadata JSONB
);

CREATE TABLE IF NOT EXISTS voting.votes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    proposal_id UUID NOT NULL REFERENCES voting.voting_proposals(id) ON DELETE CASCADE,
    option_id UUID NOT NULL REFERENCES voting.voting_options(id) ON DELETE CASCADE,
    voter_id UUID REFERENCES core.users(id),
    voter_identifier VARCHAR(255), -- For anonymous voting
    vote_weight DECIMAL(18, 8) NOT NULL DEFAULT 1.0,
    cast_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    is_valid BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    
    CONSTRAINT uk_votes_proposal_voter UNIQUE(proposal_id, voter_id)
);

CREATE TABLE IF NOT EXISTS voting.voting_results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    proposal_id UUID NOT NULL REFERENCES voting.voting_proposals(id) ON DELETE CASCADE,
    total_votes INTEGER NOT NULL DEFAULT 0,
    total_weight DECIMAL(18, 8) NOT NULL DEFAULT 0,
    participation_rate DECIMAL(5, 4),
    winning_option_id UUID REFERENCES voting.voting_options(id),
    calculated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    is_final BOOLEAN NOT NULL DEFAULT false,
    metadata JSONB
);

-- Cross-chain schema tables
CREATE TABLE IF NOT EXISTS crosschain.cross_chain_operations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    operation_id VARCHAR(255) NOT NULL UNIQUE,
    operation_type VARCHAR(100) NOT NULL,
    source_chain VARCHAR(50) NOT NULL,
    target_chain VARCHAR(50) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    transaction_hash VARCHAR(255),
    block_number BIGINT,
    gas_used BIGINT,
    gas_price BIGINT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMP WITH TIME ZONE,
    retry_count INTEGER NOT NULL DEFAULT 0,
    max_retries INTEGER NOT NULL DEFAULT 3,
    metadata JSONB
);

CREATE TABLE IF NOT EXISTS crosschain.token_transfers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    operation_id UUID NOT NULL REFERENCES crosschain.cross_chain_operations(id) ON DELETE CASCADE,
    from_address VARCHAR(255) NOT NULL,
    to_address VARCHAR(255) NOT NULL,
    token_contract VARCHAR(255),
    amount DECIMAL(38, 18) NOT NULL,
    decimals INTEGER NOT NULL DEFAULT 18,
    source_tx_hash VARCHAR(255),
    target_tx_hash VARCHAR(255),
    bridge_fee DECIMAL(38, 18),
    exchange_rate DECIMAL(38, 18),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    metadata JSONB
);

-- Monitoring schema tables
CREATE TABLE IF NOT EXISTS monitoring.system_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    metric_name VARCHAR(100) NOT NULL,
    metric_value DECIMAL(20, 8) NOT NULL,
    metric_unit VARCHAR(20),
    service_name VARCHAR(100),
    instance_id VARCHAR(100),
    recorded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    metadata JSONB
);

CREATE TABLE IF NOT EXISTS monitoring.health_check_results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    check_name VARCHAR(100) NOT NULL,
    status VARCHAR(20) NOT NULL,
    response_time_ms INTEGER,
    service_name VARCHAR(100),
    instance_id VARCHAR(100),
    error_message TEXT,
    checked_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    metadata JSONB
);

-- Event sourcing schema tables
CREATE TABLE IF NOT EXISTS eventsourcing.events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    aggregate_id UUID NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    event_data JSONB NOT NULL,
    event_version INTEGER NOT NULL DEFAULT 1,
    occurred_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    correlation_id UUID,
    causation_id UUID,
    metadata JSONB
);

CREATE TABLE IF NOT EXISTS eventsourcing.snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    aggregate_id UUID NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL,
    snapshot_data JSONB NOT NULL,
    version INTEGER NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT uk_snapshots_aggregate UNIQUE(aggregate_id, aggregate_type)
);

-- Indexes for performance optimization
-- User indexes
CREATE INDEX IF NOT EXISTS idx_users_username ON core.users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON core.users(email);
CREATE INDEX IF NOT EXISTS idx_users_tenant_id ON core.users(tenant_id);
CREATE INDEX IF NOT EXISTS idx_users_is_active ON core.users(is_active);
CREATE INDEX IF NOT EXISTS idx_users_created_at ON core.users(created_at);

-- SGX indexes
CREATE INDEX IF NOT EXISTS idx_sealed_data_key ON sgx.sealed_data_items(key);
CREATE INDEX IF NOT EXISTS idx_sealed_data_service_name ON sgx.sealed_data_items(service_name);
CREATE INDEX IF NOT EXISTS idx_sealed_data_created_at ON sgx.sealed_data_items(created_at);
CREATE INDEX IF NOT EXISTS idx_sealed_data_expires_at ON sgx.sealed_data_items(expires_at);

-- Oracle indexes
CREATE INDEX IF NOT EXISTS idx_oracle_feeds_feed_id ON oracle.oracle_data_feeds(feed_id);
CREATE INDEX IF NOT EXISTS idx_oracle_feeds_type ON oracle.oracle_data_feeds(feed_type);
CREATE INDEX IF NOT EXISTS idx_oracle_feeds_updated_at ON oracle.oracle_data_feeds(updated_at);
CREATE INDEX IF NOT EXISTS idx_feed_history_feed_id ON oracle.feed_history(feed_id);
CREATE INDEX IF NOT EXISTS idx_feed_history_recorded_at ON oracle.feed_history(recorded_at);

-- Voting indexes
CREATE INDEX IF NOT EXISTS idx_voting_proposals_status ON voting.voting_proposals(status);
CREATE INDEX IF NOT EXISTS idx_voting_proposals_created_by ON voting.voting_proposals(created_by);
CREATE INDEX IF NOT EXISTS idx_voting_proposals_dates ON voting.voting_proposals(starts_at, ends_at);
CREATE INDEX IF NOT EXISTS idx_votes_proposal_id ON voting.votes(proposal_id);
CREATE INDEX IF NOT EXISTS idx_votes_voter_id ON voting.votes(voter_id);
CREATE INDEX IF NOT EXISTS idx_votes_cast_at ON voting.votes(cast_at);

-- Cross-chain indexes
CREATE INDEX IF NOT EXISTS idx_crosschain_operation_id ON crosschain.cross_chain_operations(operation_id);
CREATE INDEX IF NOT EXISTS idx_crosschain_status ON crosschain.cross_chain_operations(status);
CREATE INDEX IF NOT EXISTS idx_crosschain_chains ON crosschain.cross_chain_operations(source_chain, target_chain);
CREATE INDEX IF NOT EXISTS idx_crosschain_created_at ON crosschain.cross_chain_operations(created_at);
CREATE INDEX IF NOT EXISTS idx_token_transfers_operation_id ON crosschain.token_transfers(operation_id);
CREATE INDEX IF NOT EXISTS idx_token_transfers_addresses ON crosschain.token_transfers(from_address, to_address);

-- Monitoring indexes
CREATE INDEX IF NOT EXISTS idx_system_metrics_name ON monitoring.system_metrics(metric_name);
CREATE INDEX IF NOT EXISTS idx_system_metrics_service ON monitoring.system_metrics(service_name);
CREATE INDEX IF NOT EXISTS idx_system_metrics_recorded_at ON monitoring.system_metrics(recorded_at);
CREATE INDEX IF NOT EXISTS idx_health_check_name ON monitoring.health_check_results(check_name);
CREATE INDEX IF NOT EXISTS idx_health_check_service ON monitoring.health_check_results(service_name);
CREATE INDEX IF NOT EXISTS idx_health_check_checked_at ON monitoring.health_check_results(checked_at);

-- Event sourcing indexes
CREATE INDEX IF NOT EXISTS idx_events_aggregate_id ON eventsourcing.events(aggregate_id);
CREATE INDEX IF NOT EXISTS idx_events_aggregate_type ON eventsourcing.events(aggregate_type);
CREATE INDEX IF NOT EXISTS idx_events_event_type ON eventsourcing.events(event_type);
CREATE INDEX IF NOT EXISTS idx_events_occurred_at ON eventsourcing.events(occurred_at);
CREATE INDEX IF NOT EXISTS idx_events_correlation_id ON eventsourcing.events(correlation_id);

-- Functions and triggers for automatic timestamp updates
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply update triggers
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON core.users FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_sealed_data_updated_at BEFORE UPDATE ON sgx.sealed_data_items FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_oracle_feeds_updated_at BEFORE UPDATE ON oracle.oracle_data_feeds FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_crosschain_operations_updated_at BEFORE UPDATE ON crosschain.cross_chain_operations FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Initial admin user (password: Admin@123!)
INSERT INTO core.users (username, email, password_hash, first_name, last_name, role, is_active)
VALUES (
    'admin',
    'admin@neoservicelayer.io',
    '$2a$11$rX8xQCiKzpxTgLqVX8oUPO.GzCgOzHPzgqjFb.K7zN8jK9wGqZKxq', -- Admin@123!
    'System',
    'Administrator',
    'Admin',
    true
) ON CONFLICT (username) DO NOTHING;

-- Grant necessary permissions
GRANT USAGE ON SCHEMA core TO PUBLIC;
GRANT USAGE ON SCHEMA auth TO PUBLIC;
GRANT USAGE ON SCHEMA sgx TO PUBLIC;
GRANT USAGE ON SCHEMA keymanagement TO PUBLIC;
GRANT USAGE ON SCHEMA oracle TO PUBLIC;
GRANT USAGE ON SCHEMA voting TO PUBLIC;
GRANT USAGE ON SCHEMA crosschain TO PUBLIC;
GRANT USAGE ON SCHEMA monitoring TO PUBLIC;
GRANT USAGE ON SCHEMA eventsourcing TO PUBLIC;

-- Grant table permissions (adjust as needed for security)
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA core TO PUBLIC;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA auth TO PUBLIC;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA sgx TO PUBLIC;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA keymanagement TO PUBLIC;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA oracle TO PUBLIC;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA voting TO PUBLIC;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA crosschain TO PUBLIC;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA monitoring TO PUBLIC;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA eventsourcing TO PUBLIC;

-- Grant sequence permissions
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA core TO PUBLIC;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA auth TO PUBLIC;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA sgx TO PUBLIC;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA keymanagement TO PUBLIC;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA oracle TO PUBLIC;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA voting TO PUBLIC;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA crosschain TO PUBLIC;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA monitoring TO PUBLIC;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA eventsourcing TO PUBLIC;

-- Set default schema search path
ALTER DATABASE neo_service_layer SET search_path = core, auth, sgx, keymanagement, oracle, voting, crosschain, monitoring, eventsourcing, public;

-- Migration complete
SELECT 'Neo Service Layer PostgreSQL schema migration completed successfully' AS result;
# Neo Service Layer - Database Design Specification

## Overview

This document outlines the comprehensive database design for the Neo Service Layer, including schema definitions, relationships, indexes, partitioning strategies, and migration patterns.

## Database Architecture

### 1. Multi-Database Strategy

#### 1.1 Primary Database (PostgreSQL)
- **Purpose**: Transactional data, user management, configuration
- **Features**: ACID compliance, complex queries, JSON support
- **Version**: PostgreSQL 16+
- **Extensions**: pgcrypto, uuid-ossp, pg_stat_statements

#### 1.2 Cache Layer (Redis)
- **Purpose**: Session storage, API caching, real-time data
- **Features**: In-memory performance, pub/sub, data structures
- **Version**: Redis 7.0+
- **Modules**: RedisJSON, RedisTimeSeries, RedisBloom

#### 1.3 Time Series Database (TimescaleDB)
- **Purpose**: Metrics, logs, blockchain events
- **Features**: Time-series optimization, continuous aggregates
- **Version**: TimescaleDB 2.14+
- **Integration**: PostgreSQL extension

#### 1.4 Search Engine (Elasticsearch)
- **Purpose**: Full-text search, log analysis, audit trails
- **Features**: Distributed search, analytics, real-time indexing
- **Version**: Elasticsearch 8.0+
- **Plugins**: Security, alerting, monitoring

## Schema Design

### 2. Core Schemas

#### 2.1 Authentication Schema
```sql
-- Schema for user authentication and authorization
CREATE SCHEMA authentication;

-- Users table with secure design
CREATE TABLE authentication.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(100) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    email_verified BOOLEAN DEFAULT FALSE,
    password_hash VARCHAR(255) NOT NULL,
    salt VARCHAR(255) NOT NULL,
    mfa_enabled BOOLEAN DEFAULT FALSE,
    mfa_secret VARCHAR(255),
    failed_login_attempts INTEGER DEFAULT 0,
    locked_until TIMESTAMPTZ,
    last_login_at TIMESTAMPTZ,
    password_changed_at TIMESTAMPTZ DEFAULT NOW(),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    version INTEGER DEFAULT 0,
    
    -- Constraints
    CONSTRAINT email_format CHECK (email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}$'),
    CONSTRAINT username_length CHECK (char_length(username) >= 3 AND char_length(username) <= 100)
);

-- User profiles with extended information
CREATE TABLE authentication.user_profiles (
    user_id UUID PRIMARY KEY REFERENCES authentication.users(id) ON DELETE CASCADE,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    display_name VARCHAR(200),
    avatar_url TEXT,
    preferred_language VARCHAR(10) DEFAULT 'en',
    timezone VARCHAR(50) DEFAULT 'UTC',
    date_format VARCHAR(20) DEFAULT 'YYYY-MM-DD',
    time_format VARCHAR(20) DEFAULT '24h',
    theme VARCHAR(20) DEFAULT 'light',
    notifications_enabled BOOLEAN DEFAULT TRUE,
    marketing_emails BOOLEAN DEFAULT FALSE,
    two_factor_backup_codes JSONB,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Role-based access control
CREATE TABLE authentication.roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) UNIQUE NOT NULL,
    description TEXT,
    permissions JSONB NOT NULL DEFAULT '[]',
    is_system_role BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- User role assignments
CREATE TABLE authentication.user_roles (
    user_id UUID REFERENCES authentication.users(id) ON DELETE CASCADE,
    role_id UUID REFERENCES authentication.roles(id) ON DELETE CASCADE,
    granted_by UUID REFERENCES authentication.users(id),
    granted_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    PRIMARY KEY (user_id, role_id)
);

-- Session management
CREATE TABLE authentication.user_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES authentication.users(id) ON DELETE CASCADE,
    session_token VARCHAR(255) UNIQUE NOT NULL,
    refresh_token VARCHAR(255) UNIQUE NOT NULL,
    device_info JSONB,
    ip_address INET,
    user_agent TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    last_accessed_at TIMESTAMPTZ DEFAULT NOW()
);

-- Login history for security auditing
CREATE TABLE authentication.login_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES authentication.users(id) ON DELETE CASCADE,
    login_method VARCHAR(50) NOT NULL, -- password, oauth, mfa
    success BOOLEAN NOT NULL,
    ip_address INET,
    user_agent TEXT,
    failure_reason VARCHAR(100),
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### 2.2 Cryptographic Schema
```sql
-- Schema for key management and cryptographic operations
CREATE SCHEMA cryptography;

-- Key management with hardware security module support
CREATE TABLE cryptography.keys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES authentication.users(id) ON DELETE CASCADE,
    key_name VARCHAR(100) NOT NULL,
    key_type VARCHAR(20) NOT NULL CHECK (key_type IN ('secp256k1', 'ed25519', 'rsa', 'aes')),
    key_purpose VARCHAR(50) NOT NULL CHECK (key_purpose IN ('signing', 'encryption', 'authentication', 'derivation')),
    key_curve VARCHAR(20),
    public_key TEXT NOT NULL,
    encrypted_private_key TEXT NOT NULL,
    key_derivation_path VARCHAR(100),
    parent_key_id UUID REFERENCES cryptography.keys(id),
    hsm_key_id VARCHAR(100), -- Hardware Security Module reference
    key_status VARCHAR(20) DEFAULT 'active' CHECK (key_status IN ('active', 'inactive', 'expired', 'compromised', 'revoked')),
    encryption_algorithm VARCHAR(50) DEFAULT 'AES-256-GCM',
    key_strength INTEGER,
    usage_counter BIGINT DEFAULT 0,
    max_usage_count BIGINT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    activated_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ,
    compromised_at TIMESTAMPTZ,
    revoked_at TIMESTAMPTZ,
    last_used_at TIMESTAMPTZ,
    metadata JSONB DEFAULT '{}',
    
    -- Unique constraint on user and key name
    UNIQUE (user_id, key_name),
    
    -- Ensure key relationships are valid
    CONSTRAINT key_hierarchy_check CHECK (
        (parent_key_id IS NULL) OR (parent_key_id != id)
    )
);

-- Key rotation history
CREATE TABLE cryptography.key_rotations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    old_key_id UUID NOT NULL REFERENCES cryptography.keys(id),
    new_key_id UUID NOT NULL REFERENCES cryptography.keys(id),
    rotation_reason VARCHAR(100) NOT NULL,
    initiated_by UUID REFERENCES authentication.users(id),
    completed_at TIMESTAMPTZ,
    migration_status VARCHAR(20) DEFAULT 'pending' CHECK (migration_status IN ('pending', 'in_progress', 'completed', 'failed')),
    affected_records_count BIGINT DEFAULT 0,
    error_message TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Certificate management
CREATE TABLE cryptography.certificates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key_id UUID NOT NULL REFERENCES cryptography.keys(id),
    certificate_type VARCHAR(50) NOT NULL CHECK (certificate_type IN ('x509', 'pem', 'der')),
    certificate_data TEXT NOT NULL,
    issuer VARCHAR(255),
    subject VARCHAR(255),
    serial_number VARCHAR(100),
    not_before TIMESTAMPTZ,
    not_after TIMESTAMPTZ,
    fingerprint VARCHAR(128),
    is_ca_certificate BOOLEAN DEFAULT FALSE,
    revocation_status VARCHAR(20) DEFAULT 'valid' CHECK (revocation_status IN ('valid', 'revoked', 'expired')),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    revoked_at TIMESTAMPTZ
);
```

#### 2.3 Blockchain Schema
```sql
-- Schema for blockchain-related data
CREATE SCHEMA blockchain;

-- Supported blockchain networks
CREATE TABLE blockchain.networks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) UNIQUE NOT NULL,
    network_type VARCHAR(20) NOT NULL CHECK (network_type IN ('mainnet', 'testnet', 'private')),
    chain_id BIGINT,
    network_magic INTEGER,
    rpc_endpoints JSONB NOT NULL DEFAULT '[]',
    websocket_endpoints JSONB DEFAULT '[]',
    explorer_urls JSONB DEFAULT '[]',
    native_currency JSONB NOT NULL, -- {symbol, name, decimals}
    block_time_seconds INTEGER,
    confirmation_blocks INTEGER DEFAULT 6,
    is_active BOOLEAN DEFAULT TRUE,
    configuration JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- User addresses for different networks
CREATE TABLE blockchain.addresses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES authentication.users(id) ON DELETE CASCADE,
    network_id UUID NOT NULL REFERENCES blockchain.networks(id),
    address VARCHAR(42) NOT NULL,
    address_type VARCHAR(20) DEFAULT 'standard' CHECK (address_type IN ('standard', 'multisig', 'contract')),
    key_id UUID REFERENCES cryptography.keys(id),
    derivation_path VARCHAR(100),
    label VARCHAR(100),
    is_primary BOOLEAN DEFAULT FALSE,
    is_watch_only BOOLEAN DEFAULT FALSE,
    balance_cache JSONB DEFAULT '{}', -- Cached balances by asset
    last_balance_update TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Ensure unique addresses per network
    UNIQUE (network_id, address),
    -- Only one primary address per user per network
    EXCLUDE USING btree (user_id, network_id WITH =) WHERE (is_primary = TRUE)
);

-- Transaction records
CREATE TABLE blockchain.transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    network_id UUID NOT NULL REFERENCES blockchain.networks(id),
    tx_hash VARCHAR(66) NOT NULL,
    tx_index INTEGER,
    block_number BIGINT,
    block_hash VARCHAR(66),
    block_timestamp TIMESTAMPTZ,
    from_address VARCHAR(42),
    to_address VARCHAR(42),
    value DECIMAL(78,0), -- Support very large numbers
    gas_limit BIGINT,
    gas_used BIGINT,
    gas_price DECIMAL(78,0),
    nonce BIGINT,
    transaction_type VARCHAR(20) DEFAULT 'transfer',
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'confirmed', 'failed', 'dropped')),
    confirmation_count INTEGER DEFAULT 0,
    raw_transaction JSONB,
    receipt JSONB,
    logs JSONB DEFAULT '[]',
    error_message TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    confirmed_at TIMESTAMPTZ,
    
    -- Unique constraint on network and transaction hash
    UNIQUE (network_id, tx_hash)
);

-- Smart contracts
CREATE TABLE blockchain.smart_contracts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    network_id UUID NOT NULL REFERENCES blockchain.networks(id),
    contract_address VARCHAR(42) NOT NULL,
    contract_name VARCHAR(100),
    contract_symbol VARCHAR(20),
    decimals INTEGER,
    total_supply DECIMAL(78,0),
    contract_type VARCHAR(50), -- ERC20, ERC721, ERC1155, custom
    abi JSONB,
    bytecode TEXT,
    source_code TEXT,
    compiler_version VARCHAR(50),
    deployment_tx_hash VARCHAR(66),
    deployer_address VARCHAR(42),
    is_verified BOOLEAN DEFAULT FALSE,
    is_proxy BOOLEAN DEFAULT FALSE,
    implementation_address VARCHAR(42),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    deployed_at TIMESTAMPTZ,
    
    -- Unique constraint on network and contract address
    UNIQUE (network_id, contract_address)
);

-- Contract interactions/invocations
CREATE TABLE blockchain.contract_invocations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id UUID NOT NULL REFERENCES blockchain.smart_contracts(id),
    transaction_id UUID NOT NULL REFERENCES blockchain.transactions(id),
    method_name VARCHAR(100),
    parameters JSONB DEFAULT '[]',
    return_value JSONB,
    execution_status VARCHAR(20) DEFAULT 'success' CHECK (execution_status IN ('success', 'failed', 'reverted')),
    gas_consumed BIGINT,
    events_emitted JSONB DEFAULT '[]',
    error_details TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### 2.4 Configuration Schema
```sql
-- Schema for system configuration and settings
CREATE SCHEMA configuration;

-- Service configurations with versioning
CREATE TABLE configuration.service_configs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    service_name VARCHAR(100) NOT NULL,
    config_key VARCHAR(200) NOT NULL,
    config_value JSONB NOT NULL,
    value_type VARCHAR(50) DEFAULT 'json',
    is_encrypted BOOLEAN DEFAULT FALSE,
    is_sensitive BOOLEAN DEFAULT FALSE,
    environment VARCHAR(20) DEFAULT 'development',
    version INTEGER DEFAULT 1,
    is_active BOOLEAN DEFAULT TRUE,
    schema_validation JSONB,
    description TEXT,
    created_by UUID REFERENCES authentication.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Unique constraint on service, key, and environment
    UNIQUE (service_name, config_key, environment),
    
    -- Ensure version consistency
    CONSTRAINT config_version_positive CHECK (version > 0)
);

-- Configuration change history
CREATE TABLE configuration.config_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    config_id UUID NOT NULL REFERENCES configuration.service_configs(id),
    change_type VARCHAR(20) NOT NULL CHECK (change_type IN ('created', 'updated', 'deleted', 'activated', 'deactivated')),
    old_value JSONB,
    new_value JSONB,
    changed_by UUID REFERENCES authentication.users(id),
    change_reason TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Environment-specific settings
CREATE TABLE configuration.environments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) UNIQUE NOT NULL,
    display_name VARCHAR(100),
    description TEXT,
    is_production BOOLEAN DEFAULT FALSE,
    deployment_config JSONB DEFAULT '{}',
    health_check_urls JSONB DEFAULT '[]',
    monitoring_config JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### 2.5 Audit Schema
```sql
-- Schema for audit trails and event sourcing
CREATE SCHEMA audit;

-- Domain events for event sourcing
CREATE TABLE audit.domain_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    aggregate_id UUID NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL,
    aggregate_version BIGINT NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    event_data JSONB NOT NULL,
    event_version INTEGER DEFAULT 1,
    correlation_id UUID,
    causation_id UUID,
    initiated_by VARCHAR(255),
    occurred_at TIMESTAMPTZ DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    processing_status VARCHAR(20) DEFAULT 'pending' CHECK (processing_status IN ('pending', 'processed', 'failed')),
    retry_count INTEGER DEFAULT 0,
    error_message TEXT,
    
    -- Ensure event ordering per aggregate
    UNIQUE (aggregate_id, aggregate_version)
);

-- Aggregate snapshots for performance optimization
CREATE TABLE audit.aggregate_snapshots (
    aggregate_id UUID PRIMARY KEY,
    aggregate_type VARCHAR(100) NOT NULL,
    aggregate_version BIGINT NOT NULL,
    snapshot_data JSONB NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Ensure snapshot version consistency
    CONSTRAINT snapshot_version_positive CHECK (aggregate_version > 0)
);

-- Security audit log
CREATE TABLE audit.security_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_type VARCHAR(100) NOT NULL,
    severity VARCHAR(20) NOT NULL CHECK (severity IN ('low', 'medium', 'high', 'critical')),
    user_id UUID REFERENCES authentication.users(id),
    session_id UUID REFERENCES authentication.user_sessions(id),
    source_ip INET,
    user_agent TEXT,
    resource_type VARCHAR(100),
    resource_id UUID,
    action VARCHAR(100),
    outcome VARCHAR(20) CHECK (outcome IN ('success', 'failure', 'blocked')),
    risk_score INTEGER CHECK (risk_score BETWEEN 0 AND 100),
    event_details JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Index for security monitoring
    INDEX idx_security_events_severity_time ON audit.security_events (severity, created_at),
    INDEX idx_security_events_user_time ON audit.security_events (user_id, created_at)
);

-- Data access audit trail
CREATE TABLE audit.data_access_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES authentication.users(id),
    table_name VARCHAR(100) NOT NULL,
    operation VARCHAR(20) NOT NULL CHECK (operation IN ('SELECT', 'INSERT', 'UPDATE', 'DELETE')),
    record_id UUID,
    affected_columns JSONB DEFAULT '[]',
    old_values JSONB,
    new_values JSONB,
    query_hash VARCHAR(64),
    execution_time_ms INTEGER,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

### 3. Indexes and Performance Optimization

#### 3.1 Primary Indexes
```sql
-- Authentication schema indexes
CREATE INDEX idx_users_email ON authentication.users (email) WHERE deleted_at IS NULL;
CREATE INDEX idx_users_username ON authentication.users (username) WHERE deleted_at IS NULL;
CREATE INDEX idx_user_sessions_token ON authentication.user_sessions (session_token);
CREATE INDEX idx_user_sessions_user_active ON authentication.user_sessions (user_id) WHERE is_active = TRUE;
CREATE INDEX idx_login_history_user_time ON authentication.login_history (user_id, created_at DESC);

-- Cryptography schema indexes
CREATE INDEX idx_keys_user_status ON cryptography.keys (user_id, key_status);
CREATE INDEX idx_keys_purpose_status ON cryptography.keys (key_purpose, key_status);
CREATE INDEX idx_keys_expires_at ON cryptography.keys (expires_at) WHERE expires_at IS NOT NULL;
CREATE INDEX idx_key_rotations_status ON cryptography.key_rotations (migration_status);

-- Blockchain schema indexes
CREATE INDEX idx_addresses_user_network ON blockchain.addresses (user_id, network_id);
CREATE INDEX idx_addresses_network_address ON blockchain.addresses (network_id, address);
CREATE INDEX idx_transactions_network_hash ON blockchain.transactions (network_id, tx_hash);
CREATE INDEX idx_transactions_block ON blockchain.transactions (block_number DESC, tx_index);
CREATE INDEX idx_transactions_address_time ON blockchain.transactions (from_address, created_at DESC);
CREATE INDEX idx_transactions_to_address_time ON blockchain.transactions (to_address, created_at DESC);
CREATE INDEX idx_transactions_status ON blockchain.transactions (status, created_at);

-- Configuration schema indexes
CREATE INDEX idx_service_configs_name_env ON configuration.service_configs (service_name, environment);
CREATE INDEX idx_service_configs_active ON configuration.service_configs (is_active, service_name);

-- Audit schema indexes
CREATE INDEX idx_domain_events_aggregate ON audit.domain_events (aggregate_id, aggregate_version);
CREATE INDEX idx_domain_events_type_time ON audit.domain_events (event_type, occurred_at DESC);
CREATE INDEX idx_domain_events_processing ON audit.domain_events (processing_status, created_at) WHERE processing_status != 'processed';
CREATE INDEX idx_security_events_user_time ON audit.security_events (user_id, created_at DESC);
CREATE INDEX idx_security_events_severity ON audit.security_events (severity, created_at DESC);
```

#### 3.2 Composite Indexes for Complex Queries
```sql
-- Multi-column indexes for common query patterns
CREATE INDEX idx_transactions_user_network_status_time ON blockchain.transactions 
    (user_id, network_id, status, created_at DESC) 
    WHERE user_id IS NOT NULL;

CREATE INDEX idx_keys_user_purpose_status_time ON cryptography.keys 
    (user_id, key_purpose, key_status, created_at DESC);

CREATE INDEX idx_domain_events_aggregate_processed ON audit.domain_events 
    (aggregate_id, processing_status, occurred_at DESC);

-- Partial indexes for improved performance
CREATE INDEX idx_active_user_sessions ON authentication.user_sessions 
    (user_id, expires_at DESC) 
    WHERE is_active = TRUE;

CREATE INDEX idx_pending_transactions ON blockchain.transactions 
    (network_id, created_at DESC) 
    WHERE status = 'pending';

CREATE INDEX idx_failed_events ON audit.domain_events 
    (retry_count, occurred_at) 
    WHERE processing_status = 'failed';
```

### 4. Partitioning Strategy

#### 4.1 Time-Based Partitioning
```sql
-- Partition large audit tables by time
CREATE TABLE audit.domain_events_2025 PARTITION OF audit.domain_events
    FOR VALUES FROM ('2025-01-01') TO ('2026-01-01');

CREATE TABLE audit.domain_events_2024 PARTITION OF audit.domain_events
    FOR VALUES FROM ('2024-01-01') TO ('2025-01-01');

-- Partition transaction data by network and time
CREATE TABLE blockchain.transactions_neo_n3 PARTITION OF blockchain.transactions
    FOR VALUES IN (SELECT id FROM blockchain.networks WHERE name = 'neo-n3');

CREATE TABLE blockchain.transactions_neo_x PARTITION OF blockchain.transactions
    FOR VALUES IN (SELECT id FROM blockchain.networks WHERE name = 'neo-x');
```

#### 4.2 Hash Partitioning for User Data
```sql
-- Partition user sessions by user_id hash
CREATE TABLE authentication.user_sessions_0 PARTITION OF authentication.user_sessions
    FOR VALUES WITH (MODULUS 4, REMAINDER 0);

CREATE TABLE authentication.user_sessions_1 PARTITION OF authentication.user_sessions
    FOR VALUES WITH (MODULUS 4, REMAINDER 1);

CREATE TABLE authentication.user_sessions_2 PARTITION OF authentication.user_sessions
    FOR VALUES WITH (MODULUS 4, REMAINDER 2);

CREATE TABLE authentication.user_sessions_3 PARTITION OF authentication.user_sessions
    FOR VALUES WITH (MODULUS 4, REMAINDER 3);
```

### 5. Data Constraints and Validation

#### 5.1 Business Rule Constraints
```sql
-- Ensure password change tracking
ALTER TABLE authentication.users 
ADD CONSTRAINT password_changed_required 
CHECK (password_changed_at IS NOT NULL);

-- Validate key expiration logic
ALTER TABLE cryptography.keys 
ADD CONSTRAINT key_expiration_logic 
CHECK (
    (expires_at IS NULL) OR 
    (expires_at > created_at)
);

-- Ensure transaction amounts are non-negative
ALTER TABLE blockchain.transactions 
ADD CONSTRAINT transaction_value_positive 
CHECK (value IS NULL OR value >= 0);

-- Validate configuration environments
ALTER TABLE configuration.service_configs 
ADD CONSTRAINT valid_environment 
CHECK (environment IN ('development', 'testing', 'staging', 'production'));

-- Ensure audit event ordering
ALTER TABLE audit.domain_events 
ADD CONSTRAINT event_version_positive 
CHECK (event_version > 0 AND aggregate_version > 0);
```

#### 5.2 Data Integrity Functions
```sql
-- Function to update timestamps automatically
CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply to relevant tables
CREATE TRIGGER tr_users_updated_at 
    BEFORE UPDATE ON authentication.users 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER tr_user_profiles_updated_at 
    BEFORE UPDATE ON authentication.user_profiles 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

CREATE TRIGGER tr_service_configs_updated_at 
    BEFORE UPDATE ON configuration.service_configs 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();
```

### 6. Security and Encryption

#### 6.1 Row Level Security (RLS)
```sql
-- Enable RLS on sensitive tables
ALTER TABLE authentication.users ENABLE ROW LEVEL SECURITY;
ALTER TABLE authentication.user_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE cryptography.keys ENABLE ROW LEVEL SECURITY;

-- Users can only access their own data
CREATE POLICY users_own_data ON authentication.users
    FOR ALL TO authenticated_users
    USING (id = current_user_id());

CREATE POLICY user_profiles_own_data ON authentication.user_profiles
    FOR ALL TO authenticated_users
    USING (user_id = current_user_id());

CREATE POLICY keys_own_data ON cryptography.keys
    FOR ALL TO authenticated_users
    USING (user_id = current_user_id());

-- Admin users can access all data
CREATE POLICY admin_full_access ON authentication.users
    FOR ALL TO admin_users
    USING (true);
```

#### 6.2 Encryption at Rest
```sql
-- Enable transparent data encryption for sensitive columns
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Function to encrypt sensitive data
CREATE OR REPLACE FUNCTION encrypt_sensitive_data(data TEXT, key TEXT)
RETURNS TEXT AS $$
BEGIN
    RETURN encode(
        pgp_sym_encrypt(data, key),
        'base64'
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to decrypt sensitive data
CREATE OR REPLACE FUNCTION decrypt_sensitive_data(encrypted_data TEXT, key TEXT)
RETURNS TEXT AS $$
BEGIN
    RETURN pgp_sym_decrypt(
        decode(encrypted_data, 'base64'),
        key
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
```

### 7. Performance Monitoring

#### 7.1 Query Performance Views
```sql
-- View for slow queries monitoring
CREATE VIEW monitoring.slow_queries AS
SELECT 
    query,
    calls,
    total_time,
    mean_time,
    max_time,
    stddev_time,
    rows,
    100.0 * shared_blks_hit / nullif(shared_blks_hit + shared_blks_read, 0) AS hit_percent
FROM pg_stat_statements
WHERE mean_time > 100  -- Queries taking more than 100ms on average
ORDER BY total_time DESC;

-- View for table statistics
CREATE VIEW monitoring.table_stats AS
SELECT 
    schemaname,
    tablename,
    n_tup_ins AS inserts,
    n_tup_upd AS updates,
    n_tup_del AS deletes,
    n_tup_hot_upd AS hot_updates,
    n_live_tup AS live_tuples,
    n_dead_tup AS dead_tuples,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze
FROM pg_stat_user_tables
ORDER BY n_live_tup DESC;
```

#### 7.2 Index Usage Monitoring
```sql
-- View for unused indexes
CREATE VIEW monitoring.unused_indexes AS
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_tup_read,
    idx_tup_fetch,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size
FROM pg_stat_user_indexes
WHERE idx_tup_read = 0 AND idx_tup_fetch = 0
ORDER BY pg_relation_size(indexrelid) DESC;

-- View for index effectiveness
CREATE VIEW monitoring.index_effectiveness AS
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_tup_read,
    idx_tup_fetch,
    idx_tup_read::FLOAT / GREATEST(idx_tup_fetch, 1) AS selectivity,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size
FROM pg_stat_user_indexes
WHERE idx_tup_read > 0
ORDER BY selectivity DESC;
```

### 8. Backup and Recovery Strategy

#### 8.1 Backup Configuration
```sql
-- Create backup role with limited permissions
CREATE ROLE backup_user WITH LOGIN;
GRANT CONNECT ON DATABASE neo_service_layer TO backup_user;
GRANT USAGE ON ALL SCHEMAS IN DATABASE neo_service_layer TO backup_user;
GRANT SELECT ON ALL TABLES IN DATABASE neo_service_layer TO backup_user;

-- Backup script (to be run externally)
-- pg_dump -h localhost -U backup_user -d neo_service_layer --schema-only > schema_backup.sql
-- pg_dump -h localhost -U backup_user -d neo_service_layer --data-only > data_backup.sql
```

#### 8.2 Point-in-Time Recovery Setup
```sql
-- Configure WAL archiving (in postgresql.conf)
-- wal_level = replica
-- archive_mode = on
-- archive_command = 'cp %p /var/lib/postgresql/wal_archive/%f'
-- max_wal_senders = 3
-- wal_keep_segments = 64

-- Create replication user
CREATE ROLE replication_user WITH LOGIN REPLICATION;
```

### 9. Migration Strategy

#### 9.1 Schema Versioning
```sql
-- Schema version tracking
CREATE TABLE public.schema_migrations (
    version VARCHAR(20) PRIMARY KEY,
    applied_at TIMESTAMPTZ DEFAULT NOW(),
    description TEXT,
    script_name VARCHAR(255),
    checksum VARCHAR(64)
);

-- Migration template
INSERT INTO public.schema_migrations (version, description, script_name)
VALUES ('2025.01.23.001', 'Initial authentication schema', 'V2025.01.23.001__create_authentication_schema.sql');
```

#### 9.2 Data Migration Utilities
```sql
-- Function for safe column addition
CREATE OR REPLACE FUNCTION safe_add_column(
    table_name TEXT,
    column_name TEXT,
    column_type TEXT,
    default_value TEXT DEFAULT NULL
) RETURNS VOID AS $$
BEGIN
    -- Check if column exists
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' 
        AND table_name = table_name
        AND column_name = column_name
    ) THEN
        EXECUTE format('ALTER TABLE %I ADD COLUMN %I %s %s',
            table_name, column_name, column_type, 
            COALESCE('DEFAULT ' || default_value, ''));
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Function for safe index creation
CREATE OR REPLACE FUNCTION safe_create_index(
    index_name TEXT,
    table_name TEXT,
    columns TEXT,
    unique_index BOOLEAN DEFAULT FALSE
) RETURNS VOID AS $$
BEGIN
    -- Check if index exists
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes
        WHERE schemaname = 'public'
        AND indexname = index_name
    ) THEN
        EXECUTE format('CREATE %s INDEX %I ON %I (%s)',
            CASE WHEN unique_index THEN 'UNIQUE' ELSE '' END,
            index_name, table_name, columns);
    END IF;
END;
$$ LANGUAGE plpgsql;
```

This comprehensive database design provides a solid foundation for the Neo Service Layer platform with proper security, performance optimization, and scalability considerations.
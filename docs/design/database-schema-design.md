# Database Schema Design for Secure Data Storage

## Overview

This document provides a comprehensive database schema design for the Neo Service Layer platform, implementing enterprise-grade security controls including encryption at rest, row-level security, data classification, and audit trails. The schema supports PostgreSQL with advanced security features and compliance requirements.

## Security Architecture Principles

### 🔒 **Data Classification Framework**
- **Public**: No encryption required, basic access controls
- **Internal**: Standard encryption, role-based access
- **Confidential**: Enhanced encryption, field-level protection
- **Secret**: SGX sealing + encryption, maximum protection

### 🛡️ **Defense in Depth**
- Database-level encryption (TDE)
- Column-level encryption for sensitive data
- Row-level security policies
- Comprehensive audit logging
- Data anonymization and pseudonymization

### 🔑 **Key Management**
- Hierarchical key structure (KEK → DEK)
- Automated key rotation (24-hour cycle)
- Hardware security module (HSM) integration
- Secure key escrow and recovery

## Core Schema Architecture

### Database Structure Overview

```sql
-- ============================================================
-- Neo Service Layer - Secure Database Schema
-- PostgreSQL with Advanced Security Features
-- ============================================================

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";

-- Create dedicated schemas for different security levels
CREATE SCHEMA IF NOT EXISTS security_public;     -- Public data
CREATE SCHEMA IF NOT EXISTS security_internal;   -- Internal data
CREATE SCHEMA IF NOT EXISTS security_confidential; -- Confidential data
CREATE SCHEMA IF NOT EXISTS security_secret;     -- Secret data (SGX sealed)
CREATE SCHEMA IF NOT EXISTS audit;               -- Audit and logging
CREATE SCHEMA IF NOT EXISTS config;              -- System configuration
```

## 1. User Management and Authentication

### Users and Authentication Schema

```sql
-- ============================================================
-- USER MANAGEMENT AND AUTHENTICATION
-- ============================================================

-- User accounts with enhanced security
CREATE TABLE security_confidential.users (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    username varchar(64) UNIQUE NOT NULL,
    email varchar(256) UNIQUE NOT NULL,
    
    -- Encrypted password hash (PBKDF2 100K iterations)
    password_hash text NOT NULL,
    password_salt text NOT NULL,
    password_iterations integer DEFAULT 100000,
    password_last_changed timestamp with time zone DEFAULT NOW(),
    
    -- Account security
    account_status varchar(20) DEFAULT 'active' 
        CHECK (account_status IN ('active', 'disabled', 'locked', 'suspended')),
    failed_login_attempts integer DEFAULT 0,
    lockout_until timestamp with time zone,
    last_login timestamp with time zone,
    last_login_ip inet,
    
    -- Multi-factor authentication
    mfa_enabled boolean DEFAULT false,
    mfa_secret_encrypted text, -- AES-256-GCM encrypted
    backup_codes_encrypted text[], -- Encrypted backup codes
    
    -- Personal information (encrypted)
    first_name_encrypted text,
    last_name_encrypted text,
    phone_encrypted text,
    
    -- Audit fields
    created_at timestamp with time zone DEFAULT NOW(),
    updated_at timestamp with time zone DEFAULT NOW(),
    created_by uuid,
    updated_by uuid,
    version integer DEFAULT 1
);

-- User roles and permissions
CREATE TABLE security_internal.roles (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    name varchar(64) UNIQUE NOT NULL,
    description text,
    permissions text[], -- JSON array of permissions
    is_system_role boolean DEFAULT false,
    created_at timestamp with time zone DEFAULT NOW(),
    updated_at timestamp with time zone DEFAULT NOW()
);

-- User role assignments
CREATE TABLE security_internal.user_roles (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id uuid NOT NULL REFERENCES security_confidential.users(id) ON DELETE CASCADE,
    role_id uuid NOT NULL REFERENCES security_internal.roles(id) ON DELETE CASCADE,
    granted_by uuid REFERENCES security_confidential.users(id),
    granted_at timestamp with time zone DEFAULT NOW(),
    expires_at timestamp with time zone,
    is_active boolean DEFAULT true,
    UNIQUE(user_id, role_id)
);

-- JWT token management and blacklist
CREATE TABLE security_internal.jwt_tokens (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id uuid NOT NULL REFERENCES security_confidential.users(id) ON DELETE CASCADE,
    token_hash text NOT NULL, -- SHA-256 hash of token
    token_type varchar(20) DEFAULT 'access' CHECK (token_type IN ('access', 'refresh')),
    issued_at timestamp with time zone NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    revoked_at timestamp with time zone,
    device_fingerprint text,
    ip_address inet,
    user_agent text,
    is_active boolean DEFAULT true
);

-- User sessions
CREATE TABLE security_internal.user_sessions (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id uuid NOT NULL REFERENCES security_confidential.users(id) ON DELETE CASCADE,
    session_token_hash text NOT NULL,
    device_info jsonb,
    ip_address inet,
    user_agent text,
    location_data jsonb, -- Geo-location information
    created_at timestamp with time zone DEFAULT NOW(),
    last_activity timestamp with time zone DEFAULT NOW(),
    expires_at timestamp with time zone NOT NULL,
    is_active boolean DEFAULT true
);
```

### Row-Level Security Policies

```sql
-- Enable row-level security
ALTER TABLE security_confidential.users ENABLE ROW LEVEL SECURITY;

-- Policy: Users can only see their own data
CREATE POLICY user_self_access ON security_confidential.users
    FOR ALL TO authenticated_users
    USING (id = current_user_id());

-- Policy: Administrators can see all users
CREATE POLICY admin_full_access ON security_confidential.users
    FOR ALL TO admin_role
    USING (true);

-- Function to get current user ID from JWT context
CREATE OR REPLACE FUNCTION current_user_id()
RETURNS uuid
LANGUAGE sql
STABLE
AS $$
    SELECT COALESCE(
        (current_setting('app.current_user_id', true))::uuid,
        '00000000-0000-0000-0000-000000000000'::uuid
    );
$$;
```

## 2. Security Service Schema

### Security Policies and Configuration

```sql
-- ============================================================
-- SECURITY SERVICE SCHEMA
-- ============================================================

-- Security policies
CREATE TABLE security_internal.security_policies (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    resource_type varchar(64) NOT NULL,
    policy_name varchar(128) NOT NULL,
    
    -- Policy configuration
    requires_authentication boolean DEFAULT true,
    requires_encryption boolean DEFAULT true,
    max_input_size integer DEFAULT 1048576,
    rate_limit_requests integer DEFAULT 100,
    rate_limit_window_seconds integer DEFAULT 60,
    
    -- Validation settings
    validate_input boolean DEFAULT true,
    check_sql_injection boolean DEFAULT true,
    check_xss boolean DEFAULT true,
    check_code_injection boolean DEFAULT true,
    check_path_traversal boolean DEFAULT true,
    
    -- Audit settings
    log_security_events boolean DEFAULT true,
    log_data_access boolean DEFAULT false,
    retention_days integer DEFAULT 90,
    
    -- Metadata
    created_at timestamp with time zone DEFAULT NOW(),
    updated_at timestamp with time zone DEFAULT NOW(),
    created_by uuid REFERENCES security_confidential.users(id),
    is_active boolean DEFAULT true,
    version integer DEFAULT 1,
    
    UNIQUE(resource_type, policy_name)
);

-- Encryption key metadata
CREATE TABLE security_secret.encryption_keys (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    key_id varchar(128) UNIQUE NOT NULL,
    key_type varchar(32) NOT NULL CHECK (key_type IN ('DEK', 'KEK', 'SGX_SEAL')),
    algorithm varchar(32) DEFAULT 'AES-256-GCM',
    
    -- Key management
    created_at timestamp with time zone DEFAULT NOW(),
    rotated_at timestamp with time zone,
    expires_at timestamp with time zone,
    rotation_interval_hours integer DEFAULT 24,
    
    -- Status and usage
    status varchar(20) DEFAULT 'active' 
        CHECK (status IN ('active', 'rotating', 'revoked', 'archived')),
    usage_count integer DEFAULT 0,
    last_used_at timestamp with time zone,
    
    -- Security metadata (key itself stored in HSM)
    hsm_key_id varchar(256), -- Reference to HSM key
    key_escrow_id varchar(256), -- Escrow reference
    
    -- Audit
    created_by uuid REFERENCES security_confidential.users(id)
);

-- Rate limiting state
CREATE TABLE security_internal.rate_limit_state (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    identifier varchar(256) NOT NULL, -- IP, user ID, API key hash
    identifier_type varchar(32) NOT NULL CHECK (identifier_type IN ('ip', 'user', 'api_key')),
    
    -- Rate limiting data
    request_count integer DEFAULT 0,
    window_start timestamp with time zone DEFAULT NOW(),
    window_size_seconds integer DEFAULT 60,
    max_requests integer DEFAULT 100,
    
    -- Status
    is_blocked boolean DEFAULT false,
    blocked_until timestamp with time zone,
    
    -- Metadata
    first_seen timestamp with time zone DEFAULT NOW(),
    last_request timestamp with time zone DEFAULT NOW(),
    
    UNIQUE(identifier, identifier_type)
);

-- Security threats and incidents
CREATE TABLE security_internal.security_threats (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Threat identification
    threat_type varchar(64) NOT NULL 
        CHECK (threat_type IN ('sql_injection', 'xss', 'code_injection', 'path_traversal', 
                               'brute_force', 'dos', 'suspicious_pattern', 'unknown')),
    severity varchar(20) DEFAULT 'medium'
        CHECK (severity IN ('low', 'medium', 'high', 'critical')),
    risk_score numeric(3,2) CHECK (risk_score >= 0.0 AND risk_score <= 1.0),
    
    -- Source information
    source_ip inet,
    user_id uuid REFERENCES security_confidential.users(id),
    user_agent text,
    request_uri text,
    request_method varchar(10),
    
    -- Threat details
    threat_payload text, -- The detected malicious input
    detection_method varchar(64), -- How it was detected
    blocked boolean DEFAULT true,
    
    -- Context
    session_id uuid,
    correlation_id uuid,
    additional_context jsonb,
    
    -- Timestamps
    detected_at timestamp with time zone DEFAULT NOW(),
    resolved_at timestamp with time zone,
    
    -- Response actions
    response_action varchar(64), -- Action taken
    analyst_notes text
);
```

## 3. SGX Enclave Data Schema

### SGX Operations and Data Sealing

```sql
-- ============================================================
-- SGX ENCLAVE DATA SCHEMA
-- ============================================================

-- SGX enclave instances and attestation
CREATE TABLE security_secret.sgx_enclaves (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    enclave_id varchar(128) UNIQUE NOT NULL,
    
    -- Enclave measurements
    mr_enclave char(64) NOT NULL, -- SHA-256 hex
    mr_signer char(64) NOT NULL,  -- SHA-256 hex
    isv_prod_id integer NOT NULL,
    isv_svn integer NOT NULL,
    
    -- Attestation information
    attestation_status varchar(32) DEFAULT 'pending'
        CHECK (attestation_status IN ('pending', 'valid', 'invalid', 'expired', 'revoked')),
    quote_data text, -- Base64 encoded attestation quote
    signature_data text, -- Attestation signature
    platform_info jsonb, -- Platform configuration
    
    -- Timestamps
    created_at timestamp with time zone DEFAULT NOW(),
    last_attestation timestamp with time zone,
    attestation_expires_at timestamp with time zone,
    
    -- Status
    is_active boolean DEFAULT true,
    health_status varchar(20) DEFAULT 'healthy'
        CHECK (health_status IN ('healthy', 'degraded', 'unhealthy'))
);

-- SGX code execution records
CREATE TABLE security_internal.sgx_executions (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    execution_id varchar(128) UNIQUE NOT NULL,
    
    -- Execution context
    enclave_id uuid NOT NULL REFERENCES security_secret.sgx_enclaves(id),
    user_id uuid REFERENCES security_confidential.users(id),
    session_id uuid,
    
    -- Execution details
    script_hash char(64) NOT NULL, -- SHA-256 of executed script
    input_data_hash char(64), -- SHA-256 of input data
    output_data_hash char(64), -- SHA-256 of output data
    
    -- Performance metrics
    execution_time_ms integer,
    memory_usage_bytes integer,
    cpu_time_ms integer,
    
    -- Status and results
    execution_status varchar(20) DEFAULT 'pending'
        CHECK (execution_status IN ('pending', 'running', 'completed', 'failed', 'timeout')),
    error_message text,
    
    -- Security validation
    input_validated boolean DEFAULT false,
    output_validated boolean DEFAULT false,
    threats_detected text[], -- Array of threat types
    risk_score numeric(3,2),
    
    -- Timestamps
    started_at timestamp with time zone DEFAULT NOW(),
    completed_at timestamp with time zone
);

-- SGX sealed data storage
CREATE TABLE security_secret.sgx_sealed_data (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    data_id varchar(128) UNIQUE NOT NULL,
    
    -- Sealing information
    enclave_id uuid NOT NULL REFERENCES security_secret.sgx_enclaves(id),
    sealing_policy varchar(32) DEFAULT 'MRENCLAVE'
        CHECK (sealing_policy IN ('MRENCLAVE', 'MRSIGNER', 'HYBRID')),
    
    -- Data information
    sealed_blob bytea NOT NULL, -- The actual sealed data
    original_size integer,
    sealed_size integer,
    compression_ratio numeric(5,2),
    
    -- Metadata
    description text,
    tags text[],
    content_type varchar(64),
    
    -- Security
    integrity_hash char(64) NOT NULL, -- SHA-256 of sealed blob
    additional_data text, -- Additional authentication data used in sealing
    
    -- Lifecycle
    created_at timestamp with time zone DEFAULT NOW(),
    last_accessed timestamp with time zone,
    access_count integer DEFAULT 0,
    expires_at timestamp with time zone,
    
    -- Ownership
    created_by uuid REFERENCES security_confidential.users(id),
    access_policy jsonb -- Who can unseal this data
);
```

## 4. Service Data Schema

### Core Service Storage

```sql
-- ============================================================
-- CORE SERVICES DATA SCHEMA
-- ============================================================

-- Service registry and metadata
CREATE TABLE security_internal.services (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_name varchar(128) UNIQUE NOT NULL,
    service_type varchar(64) NOT NULL,
    version varchar(32) NOT NULL,
    
    -- Service configuration
    description text,
    capabilities text[], -- Array of service capabilities
    dependencies text[], -- Array of dependent services
    
    -- Health and status
    status varchar(20) DEFAULT 'active'
        CHECK (status IN ('active', 'inactive', 'maintenance', 'deprecated')),
    health_status varchar(20) DEFAULT 'unknown'
        CHECK (health_status IN ('healthy', 'degraded', 'unhealthy', 'unknown')),
    last_health_check timestamp with time zone,
    
    -- Performance metrics
    average_response_time_ms numeric(10,2),
    success_rate numeric(5,2),
    error_rate numeric(5,2),
    
    -- Configuration
    configuration_encrypted text, -- AES-256-GCM encrypted JSON
    secrets_vault_path varchar(256), -- Path to secrets in vault
    
    -- Lifecycle
    created_at timestamp with time zone DEFAULT NOW(),
    updated_at timestamp with time zone DEFAULT NOW(),
    deployed_at timestamp with time zone,
    
    -- Ownership
    owner_team varchar(64),
    maintainer_email varchar(256)
);

-- Service configurations with encryption
CREATE TABLE security_confidential.service_configurations (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_id uuid NOT NULL REFERENCES security_internal.services(id) ON DELETE CASCADE,
    
    -- Configuration data
    config_key varchar(128) NOT NULL,
    config_value_encrypted text, -- Encrypted configuration value
    config_type varchar(32) DEFAULT 'string'
        CHECK (config_type IN ('string', 'number', 'boolean', 'json', 'secret')),
    
    -- Security classification
    classification varchar(32) DEFAULT 'internal'
        CHECK (classification IN ('public', 'internal', 'confidential', 'secret')),
    
    -- Encryption metadata
    encryption_key_id varchar(128) REFERENCES security_secret.encryption_keys(key_id),
    encrypted_at timestamp with time zone DEFAULT NOW(),
    
    -- Lifecycle
    created_at timestamp with time zone DEFAULT NOW(),
    updated_at timestamp with time zone DEFAULT NOW(),
    version integer DEFAULT 1,
    is_active boolean DEFAULT true,
    
    UNIQUE(service_id, config_key)
);

-- API endpoints and security policies
CREATE TABLE security_internal.api_endpoints (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_id uuid NOT NULL REFERENCES security_internal.services(id),
    
    -- Endpoint information
    path varchar(512) NOT NULL,
    http_method varchar(10) NOT NULL,
    endpoint_name varchar(128),
    description text,
    
    -- Security requirements
    requires_authentication boolean DEFAULT true,
    requires_authorization boolean DEFAULT true,
    allowed_roles text[], -- Array of role names
    
    -- Rate limiting
    rate_limit_requests integer DEFAULT 100,
    rate_limit_window_seconds integer DEFAULT 60,
    
    -- Input validation
    validate_input boolean DEFAULT true,
    max_request_size integer DEFAULT 1048576,
    allowed_content_types text[] DEFAULT '{"application/json"}',
    
    -- Monitoring
    enable_metrics boolean DEFAULT true,
    enable_tracing boolean DEFAULT true,
    enable_logging boolean DEFAULT true,
    
    -- Status
    is_active boolean DEFAULT true,
    deprecated_at timestamp with time zone,
    
    -- Lifecycle
    created_at timestamp with time zone DEFAULT NOW(),
    updated_at timestamp with time zone DEFAULT NOW(),
    
    UNIQUE(service_id, path, http_method)
);
```

## 5. Business Data Schema

### Blockchain and Transaction Data

```sql
-- ============================================================
-- BUSINESS DATA SCHEMA
-- ============================================================

-- Blockchain networks configuration
CREATE TABLE security_internal.blockchain_networks (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    network_name varchar(64) UNIQUE NOT NULL,
    network_type varchar(32) NOT NULL 
        CHECK (network_type IN ('neo_n3', 'neo_x', 'ethereum', 'bitcoin', 'custom')),
    
    -- Network configuration
    rpc_endpoints text[] NOT NULL, -- Array of RPC endpoints
    chain_id integer,
    block_time_seconds integer,
    
    -- Security settings
    connection_encrypted boolean DEFAULT true,
    certificate_pinning boolean DEFAULT true,
    api_key_encrypted text, -- Encrypted API keys for endpoints
    
    -- Status and health
    status varchar(20) DEFAULT 'active'
        CHECK (status IN ('active', 'inactive', 'maintenance')),
    last_block_height bigint,
    last_sync timestamp with time zone,
    
    -- Metadata
    created_at timestamp with time zone DEFAULT NOW(),
    updated_at timestamp with time zone DEFAULT NOW()
);

-- Smart contracts registry
CREATE TABLE security_confidential.smart_contracts (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    contract_address varchar(128) NOT NULL,
    network_id uuid NOT NULL REFERENCES security_internal.blockchain_networks(id),
    
    -- Contract information
    contract_name varchar(128),
    contract_version varchar(32),
    abi_hash char(64), -- SHA-256 of contract ABI
    bytecode_hash char(64), -- SHA-256 of contract bytecode
    
    -- Security analysis
    security_audit_status varchar(32) DEFAULT 'pending'
        CHECK (security_audit_status IN ('pending', 'in_progress', 'passed', 'failed', 'expired')),
    audit_report_encrypted text, -- Encrypted audit report
    vulnerability_count integer DEFAULT 0,
    risk_score numeric(3,2),
    
    -- Deployment information
    deployed_by uuid REFERENCES security_confidential.users(id),
    deployed_at timestamp with time zone,
    deployment_transaction_hash varchar(128),
    
    -- Access control
    owner_address varchar(128),
    access_control_list jsonb, -- Addresses and permissions
    
    -- Status
    is_active boolean DEFAULT true,
    is_verified boolean DEFAULT false,
    
    -- Lifecycle
    created_at timestamp with time zone DEFAULT NOW(),
    updated_at timestamp with time zone DEFAULT NOW(),
    
    UNIQUE(contract_address, network_id)
);

-- Transaction history with privacy protection
CREATE TABLE security_confidential.transactions (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    transaction_hash varchar(128) UNIQUE NOT NULL,
    network_id uuid NOT NULL REFERENCES security_internal.blockchain_networks(id),
    
    -- Transaction details (encrypted)
    from_address_encrypted text,
    to_address_encrypted text,
    value_encrypted text, -- Encrypted amount
    gas_used bigint,
    gas_price bigint,
    
    -- Transaction metadata
    block_height bigint,
    block_hash varchar(128),
    transaction_index integer,
    status varchar(20) DEFAULT 'pending'
        CHECK (status IN ('pending', 'confirmed', 'failed', 'replaced')),
    
    -- Privacy and anonymization
    anonymized boolean DEFAULT false,
    pseudonym_from varchar(64), -- Pseudonymized address
    pseudonym_to varchar(64), -- Pseudonymized address
    
    -- Smart contract interaction
    contract_id uuid REFERENCES security_confidential.smart_contracts(id),
    function_signature varchar(128),
    input_data_hash char(64),
    
    -- Timestamps
    created_at timestamp with time zone DEFAULT NOW(),
    confirmed_at timestamp with time zone,
    
    -- User association (optional, for user's own transactions)
    user_id uuid REFERENCES security_confidential.users(id)
);
```

## 6. Audit and Compliance Schema

### Comprehensive Audit Trail

```sql
-- ============================================================
-- AUDIT AND COMPLIANCE SCHEMA
-- ============================================================

-- Comprehensive audit log
CREATE TABLE audit.audit_log (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Event identification
    event_type varchar(64) NOT NULL,
    event_category varchar(32) NOT NULL
        CHECK (event_category IN ('authentication', 'authorization', 'data_access', 
                                  'data_modification', 'system', 'security', 'admin')),
    event_action varchar(64) NOT NULL, -- CREATE, READ, UPDATE, DELETE, LOGIN, etc.
    
    -- Actor information
    user_id uuid REFERENCES security_confidential.users(id),
    user_email varchar(256),
    user_roles text[],
    session_id uuid,
    
    -- Request context
    ip_address inet,
    user_agent text,
    request_id uuid,
    correlation_id uuid,
    
    -- Resource information
    resource_type varchar(64),
    resource_id varchar(128),
    resource_path varchar(512),
    
    -- Data changes (for data modification events)
    old_values jsonb, -- Previous values (encrypted if sensitive)
    new_values jsonb, -- New values (encrypted if sensitive)
    
    -- Security context
    security_classification varchar(32) DEFAULT 'internal',
    data_sensitivity varchar(32) DEFAULT 'normal'
        CHECK (data_sensitivity IN ('normal', 'sensitive', 'highly_sensitive')),
    
    -- Result information
    result varchar(32) DEFAULT 'success'
        CHECK (result IN ('success', 'failure', 'error', 'partial')),
    error_message text,
    
    -- Metadata
    event_timestamp timestamp with time zone DEFAULT NOW(),
    server_name varchar(64),
    service_name varchar(64),
    version varchar(32),
    
    -- Additional context
    additional_data jsonb,
    
    -- Integrity protection
    event_hash char(64), -- SHA-256 hash of event data
    previous_event_hash char(64) -- Chain of audit events
);

-- Data access log (separate for performance)
CREATE TABLE audit.data_access_log (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Access information
    user_id uuid REFERENCES security_confidential.users(id),
    table_name varchar(128) NOT NULL,
    column_names text[], -- Which columns were accessed
    row_identifier varchar(256), -- Primary key or identifier
    
    -- Access context
    access_type varchar(32) NOT NULL
        CHECK (access_type IN ('SELECT', 'INSERT', 'UPDATE', 'DELETE')),
    query_hash char(64), -- SHA-256 of executed query
    
    -- Security context
    data_classification varchar(32),
    access_granted boolean DEFAULT true,
    access_reason varchar(256), -- Business justification
    
    -- Request context
    session_id uuid,
    request_id uuid,
    ip_address inet,
    
    -- Performance data
    execution_time_ms integer,
    rows_affected integer,
    
    -- Timestamp
    accessed_at timestamp with time zone DEFAULT NOW(),
    
    -- Compliance
    retention_category varchar(32) DEFAULT 'standard'
        CHECK (retention_category IN ('standard', 'extended', 'permanent'))
);

-- Security events log
CREATE TABLE audit.security_events_log (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Event classification
    event_type varchar(64) NOT NULL,
    severity varchar(20) NOT NULL
        CHECK (severity IN ('info', 'low', 'medium', 'high', 'critical')),
    category varchar(32) NOT NULL
        CHECK (category IN ('threat_detection', 'access_violation', 'policy_violation', 
                           'anomaly', 'incident', 'vulnerability')),
    
    -- Source information
    source_ip inet,
    source_user_id uuid REFERENCES security_confidential.users(id),
    source_service varchar(64),
    source_component varchar(64),
    
    -- Event details
    event_description text NOT NULL,
    threat_indicators text[], -- Array of IOCs
    attack_vectors text[], -- Identified attack methods
    
    -- Response information
    automated_response boolean DEFAULT false,
    response_actions text[], -- Actions taken
    blocked boolean DEFAULT false,
    
    -- Investigation
    investigated boolean DEFAULT false,
    false_positive boolean,
    analyst_notes text,
    incident_id uuid,
    
    -- Context
    additional_context jsonb,
    correlation_id uuid,
    
    -- Timestamps
    event_timestamp timestamp with time zone DEFAULT NOW(),
    investigation_started timestamp with time zone,
    resolved_at timestamp with time zone,
    
    -- Compliance and retention
    retention_days integer DEFAULT 2555, -- 7 years for security events
    archived boolean DEFAULT false
);

-- Compliance report data
CREATE TABLE audit.compliance_reports (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Report information
    report_type varchar(64) NOT NULL,
    compliance_framework varchar(64) NOT NULL, -- GDPR, SOX, HIPAA, etc.
    report_period_start date NOT NULL,
    report_period_end date NOT NULL,
    
    -- Report data
    report_data_encrypted text NOT NULL, -- Encrypted JSON report
    report_hash char(64) NOT NULL, -- Integrity hash
    
    -- Generation information
    generated_by uuid REFERENCES security_confidential.users(id),
    generated_at timestamp with time zone DEFAULT NOW(),
    generation_duration_ms integer,
    
    -- Review and approval
    reviewed_by uuid REFERENCES security_confidential.users(id),
    reviewed_at timestamp with time zone,
    approved_by uuid REFERENCES security_confidential.users(id),
    approved_at timestamp with time zone,
    
    -- Status
    status varchar(32) DEFAULT 'draft'
        CHECK (status IN ('draft', 'pending_review', 'reviewed', 'approved', 'archived')),
    
    -- Distribution
    recipients text[], -- Who received the report
    distributed_at timestamp with time zone
);
```

## 7. Performance and Monitoring Schema

### System Performance Data

```sql
-- ============================================================
-- PERFORMANCE AND MONITORING SCHEMA
-- ============================================================

-- Service performance metrics
CREATE TABLE security_internal.performance_metrics (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Metric identification
    metric_name varchar(128) NOT NULL,
    metric_type varchar(32) NOT NULL
        CHECK (metric_type IN ('counter', 'gauge', 'histogram', 'summary')),
    service_name varchar(128),
    endpoint_path varchar(512),
    
    -- Metric data
    value numeric(15,4) NOT NULL,
    unit varchar(32), -- ms, bytes, requests/sec, etc.
    
    -- Labels and dimensions
    labels jsonb, -- Key-value pairs for metric dimensions
    
    -- Timestamps
    timestamp timestamp with time zone DEFAULT NOW(),
    collection_interval_seconds integer DEFAULT 60,
    
    -- Aggregation
    aggregation_period varchar(32) DEFAULT 'minute'
        CHECK (aggregation_period IN ('minute', 'hour', 'day'))
);

-- System health status
CREATE TABLE security_internal.system_health (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Component identification
    component_type varchar(64) NOT NULL, -- database, sgx_enclave, api, etc.
    component_name varchar(128) NOT NULL,
    instance_id varchar(128),
    
    -- Health status
    status varchar(20) NOT NULL
        CHECK (status IN ('healthy', 'degraded', 'unhealthy', 'unknown')),
    status_reason text,
    
    -- Performance indicators
    response_time_ms numeric(10,2),
    error_rate numeric(5,2),
    availability_percentage numeric(5,2),
    
    -- Resource utilization
    cpu_usage_percentage numeric(5,2),
    memory_usage_percentage numeric(5,2),
    disk_usage_percentage numeric(5,2),
    
    -- Health check details
    health_check_timestamp timestamp with time zone DEFAULT NOW(),
    health_check_duration_ms integer,
    
    -- Metadata
    version varchar(32),
    environment varchar(32) DEFAULT 'production',
    additional_metrics jsonb
);

-- Distributed tracing data
CREATE TABLE security_internal.distributed_traces (
    id uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    
    -- Trace identification
    trace_id varchar(64) NOT NULL,
    span_id varchar(32) NOT NULL,
    parent_span_id varchar(32),
    
    -- Span information
    operation_name varchar(256) NOT NULL,
    service_name varchar(128) NOT NULL,
    
    -- Timing
    start_time timestamp with time zone NOT NULL,
    end_time timestamp with time zone,
    duration_microseconds bigint,
    
    -- Status
    status_code varchar(32), -- OK, ERROR, TIMEOUT
    error_message text,
    
    -- Tags and metadata
    tags jsonb, -- Span tags
    logs jsonb, -- Span logs
    
    -- Security context
    user_id uuid REFERENCES security_confidential.users(id),
    session_id uuid,
    
    -- Performance
    cpu_time_microseconds bigint,
    memory_usage_bytes bigint,
    
    UNIQUE(trace_id, span_id)
);
```

## 8. Indexes and Performance Optimization

### Strategic Indexes for Security and Performance

```sql
-- ============================================================
-- INDEXES AND PERFORMANCE OPTIMIZATION
-- ============================================================

-- User authentication indexes
CREATE INDEX CONCURRENTLY idx_users_username ON security_confidential.users(username);
CREATE INDEX CONCURRENTLY idx_users_email ON security_confidential.users(email);
CREATE INDEX CONCURRENTLY idx_users_status ON security_confidential.users(account_status);
CREATE INDEX CONCURRENTLY idx_users_last_login ON security_confidential.users(last_login);

-- JWT token indexes
CREATE INDEX CONCURRENTLY idx_jwt_tokens_user_id ON security_internal.jwt_tokens(user_id);
CREATE INDEX CONCURRENTLY idx_jwt_tokens_hash ON security_internal.jwt_tokens(token_hash);
CREATE INDEX CONCURRENTLY idx_jwt_tokens_expires ON security_internal.jwt_tokens(expires_at);
CREATE INDEX CONCURRENTLY idx_jwt_tokens_active ON security_internal.jwt_tokens(is_active, expires_at);

-- Security threat indexes
CREATE INDEX CONCURRENTLY idx_security_threats_type ON security_internal.security_threats(threat_type);
CREATE INDEX CONCURRENTLY idx_security_threats_severity ON security_internal.security_threats(severity);
CREATE INDEX CONCURRENTLY idx_security_threats_detected_at ON security_internal.security_threats(detected_at);
CREATE INDEX CONCURRENTLY idx_security_threats_source_ip ON security_internal.security_threats(source_ip);
CREATE INDEX CONCURRENTLY idx_security_threats_user_id ON security_internal.security_threats(user_id);

-- Rate limiting indexes
CREATE UNIQUE INDEX CONCURRENTLY idx_rate_limit_identifier ON security_internal.rate_limit_state(identifier, identifier_type);
CREATE INDEX CONCURRENTLY idx_rate_limit_window_start ON security_internal.rate_limit_state(window_start);

-- SGX execution indexes
CREATE INDEX CONCURRENTLY idx_sgx_executions_enclave_id ON security_internal.sgx_executions(enclave_id);
CREATE INDEX CONCURRENTLY idx_sgx_executions_user_id ON security_internal.sgx_executions(user_id);
CREATE INDEX CONCURRENTLY idx_sgx_executions_status ON security_internal.sgx_executions(execution_status);
CREATE INDEX CONCURRENTLY idx_sgx_executions_started_at ON security_internal.sgx_executions(started_at);

-- Audit log indexes
CREATE INDEX CONCURRENTLY idx_audit_log_event_type ON audit.audit_log(event_type);
CREATE INDEX CONCURRENTLY idx_audit_log_user_id ON audit.audit_log(user_id);
CREATE INDEX CONCURRENTLY idx_audit_log_timestamp ON audit.audit_log(event_timestamp);
CREATE INDEX CONCURRENTLY idx_audit_log_resource_type ON audit.audit_log(resource_type);
CREATE INDEX CONCURRENTLY idx_audit_log_correlation_id ON audit.audit_log(correlation_id);

-- Security events indexes
CREATE INDEX CONCURRENTLY idx_security_events_type ON audit.security_events_log(event_type);
CREATE INDEX CONCURRENTLY idx_security_events_severity ON audit.security_events_log(severity);
CREATE INDEX CONCURRENTLY idx_security_events_timestamp ON audit.security_events_log(event_timestamp);
CREATE INDEX CONCURRENTLY idx_security_events_source_ip ON audit.security_events_log(source_ip);

-- Performance metrics indexes
CREATE INDEX CONCURRENTLY idx_performance_metrics_name_timestamp ON security_internal.performance_metrics(metric_name, timestamp);
CREATE INDEX CONCURRENTLY idx_performance_metrics_service_timestamp ON security_internal.performance_metrics(service_name, timestamp);
CREATE INDEX CONCURRENTLY idx_performance_metrics_timestamp ON security_internal.performance_metrics(timestamp);

-- GIN indexes for JSONB columns
CREATE INDEX CONCURRENTLY idx_audit_log_additional_data_gin ON audit.audit_log USING gin(additional_data);
CREATE INDEX CONCURRENTLY idx_security_events_context_gin ON audit.security_events_log USING gin(additional_context);
CREATE INDEX CONCURRENTLY idx_performance_metrics_labels_gin ON security_internal.performance_metrics USING gin(labels);

-- Full-text search indexes
CREATE INDEX CONCURRENTLY idx_audit_log_event_description_fts ON audit.audit_log USING gin(to_tsvector('english', event_action));
CREATE INDEX CONCURRENTLY idx_security_events_description_fts ON audit.security_events_log USING gin(to_tsvector('english', event_description));
```

## 9. Database Security Configuration

### Advanced Security Settings

```sql
-- ============================================================
-- DATABASE SECURITY CONFIGURATION
-- ============================================================

-- Create roles for different access levels
CREATE ROLE app_readonly_role;
CREATE ROLE app_readwrite_role;
CREATE ROLE app_admin_role;
CREATE ROLE audit_role;
CREATE ROLE security_analyst_role;

-- Grant schema usage permissions
GRANT USAGE ON SCHEMA security_public TO app_readonly_role, app_readwrite_role, app_admin_role;
GRANT USAGE ON SCHEMA security_internal TO app_readwrite_role, app_admin_role;
GRANT USAGE ON SCHEMA security_confidential TO app_admin_role;
GRANT USAGE ON SCHEMA security_secret TO app_admin_role;
GRANT USAGE ON SCHEMA audit TO audit_role, security_analyst_role, app_admin_role;

-- Configure table permissions
GRANT SELECT ON ALL TABLES IN SCHEMA security_public TO app_readonly_role;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA security_internal TO app_readwrite_role;
GRANT ALL ON ALL TABLES IN SCHEMA security_confidential TO app_admin_role;
GRANT ALL ON ALL TABLES IN SCHEMA security_secret TO app_admin_role;
GRANT SELECT ON ALL TABLES IN SCHEMA audit TO audit_role;
GRANT ALL ON ALL TABLES IN SCHEMA audit TO security_analyst_role;

-- Enable audit logging
ALTER SYSTEM SET log_statement = 'all';
ALTER SYSTEM SET log_min_duration_statement = 100; -- Log slow queries
ALTER SYSTEM SET log_checkpoints = on;
ALTER SYSTEM SET log_connections = on;
ALTER SYSTEM SET log_disconnections = on;
ALTER SYSTEM SET log_lock_waits = on;

-- Security settings
ALTER SYSTEM SET ssl = on;
ALTER SYSTEM SET ssl_ciphers = 'HIGH:MEDIUM:+3DES:!aNULL';
ALTER SYSTEM SET ssl_prefer_server_ciphers = on;
ALTER SYSTEM SET password_encryption = 'scram-sha-256';
ALTER SYSTEM SET row_security = on;

-- Performance settings for encryption
ALTER SYSTEM SET shared_preload_libraries = 'pg_stat_statements';
ALTER SYSTEM SET max_connections = 200;
ALTER SYSTEM SET shared_buffers = '256MB';
ALTER SYSTEM SET effective_cache_size = '1GB';
ALTER SYSTEM SET maintenance_work_mem = '64MB';

-- Reload configuration
SELECT pg_reload_conf();
```

## 10. Data Retention and Archival

### Automated Data Lifecycle Management

```sql
-- ============================================================
-- DATA RETENTION AND ARCHIVAL
-- ============================================================

-- Create partitioned tables for high-volume audit data
CREATE TABLE audit.audit_log_2024 PARTITION OF audit.audit_log
FOR VALUES FROM ('2024-01-01') TO ('2025-01-01');

CREATE TABLE audit.audit_log_2025 PARTITION OF audit.audit_log
FOR VALUES FROM ('2025-01-01') TO ('2026-01-01');

-- Create automated data archival function
CREATE OR REPLACE FUNCTION archive_old_audit_data()
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    cutoff_date timestamp with time zone;
BEGIN
    -- Archive audit logs older than 2 years
    cutoff_date := NOW() - INTERVAL '2 years';
    
    -- Move to archive table (implement as needed)
    INSERT INTO audit.audit_log_archive
    SELECT * FROM audit.audit_log
    WHERE event_timestamp < cutoff_date;
    
    -- Delete archived data
    DELETE FROM audit.audit_log
    WHERE event_timestamp < cutoff_date;
    
    -- Archive security events older than 7 years
    cutoff_date := NOW() - INTERVAL '7 years';
    
    UPDATE audit.security_events_log
    SET archived = true
    WHERE event_timestamp < cutoff_date
    AND archived = false;
    
    RAISE NOTICE 'Data archival completed at %', NOW();
END;
$$;

-- Create scheduled job for data archival (requires pg_cron extension)
-- SELECT cron.schedule('archive-audit-data', '0 2 1 * *', 'SELECT archive_old_audit_data();');
```

## Summary

This comprehensive database schema design provides:

### 🔒 **Enterprise Security**
- Multi-level data classification (Public → Secret)
- Row-level security policies
- Column-level encryption for sensitive data
- Comprehensive audit trails with integrity protection

### 🛡️ **Defense in Depth**
- Database-level encryption (TDE)
- Application-level encryption (AES-256-GCM)
- SGX hardware-based data sealing
- Hierarchical key management with HSM integration

### 📊 **Comprehensive Monitoring**
- Real-time security threat detection
- Performance metrics collection
- Distributed tracing support
- Automated compliance reporting

### 🔧 **Operational Excellence**
- Automated data archival and retention
- Performance-optimized indexes
- Horizontal scaling support through partitioning
- Comprehensive backup and recovery procedures

### 📋 **Compliance Ready**
- GDPR, SOX, HIPAA compliance support
- Automated data retention policies
- Comprehensive audit trails
- Data anonymization and pseudonymization

This schema design ensures data security, performance, and compliance while supporting the full range of Neo Service Layer platform operations with enterprise-grade reliability and security controls.
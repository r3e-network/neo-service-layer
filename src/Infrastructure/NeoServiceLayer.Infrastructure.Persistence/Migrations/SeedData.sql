-- Neo Service Layer PostgreSQL Database
-- Seed Data Script
-- Version: 1.0.0
-- Date: 2024-01-21

-- =====================================================
-- CORE SERVICES
-- =====================================================

-- Insert initial services
INSERT INTO core.services (id, name, version, status, description) VALUES
    ('00000000-0000-0000-0000-000000000001', 'Authentication', '1.0.0', 'Active', 'User authentication and authorization service'),
    ('00000000-0000-0000-0000-000000000002', 'EnclaveStorage', '2.0.0', 'Active', 'SGX-based confidential storage service'),
    ('00000000-0000-0000-0000-000000000003', 'Oracle', '1.0.0', 'Active', 'Decentralized oracle service for external data'),
    ('00000000-0000-0000-0000-000000000004', 'Voting', '1.0.0', 'Active', 'Secure voting and governance service'),
    ('00000000-0000-0000-0000-000000000005', 'CrossChain', '1.0.0', 'Active', 'Cross-chain bridge and interoperability service'),
    ('00000000-0000-0000-0000-000000000006', 'Monitoring', '1.0.0', 'Active', 'System monitoring and alerting service'),
    ('00000000-0000-0000-0000-000000000007', 'Compute', '1.0.0', 'Active', 'Secure computation service'),
    ('00000000-0000-0000-0000-000000000008', 'ZeroKnowledge', '1.0.0', 'Active', 'Zero-knowledge proof service'),
    ('00000000-0000-0000-0000-000000000009', 'SecretsManagement', '1.0.0', 'Active', 'Secure secrets and key management service'),
    ('00000000-0000-0000-0000-000000000010', 'ProofOfReserve', '1.0.0', 'Active', 'Proof of reserve verification service')
ON CONFLICT (name, version) DO NOTHING;

-- Insert default service configurations
INSERT INTO core.service_configurations (service_id, key, value, environment) VALUES
    ('00000000-0000-0000-0000-000000000001', 'jwt.expiration', '3600', 'Production'),
    ('00000000-0000-0000-0000-000000000001', 'max.login.attempts', '5', 'Production'),
    ('00000000-0000-0000-0000-000000000002', 'enclave.mode', 'hardware', 'Production'),
    ('00000000-0000-0000-0000-000000000002', 'storage.encryption', 'AES256', 'Production'),
    ('00000000-0000-0000-0000-000000000003', 'oracle.timeout', '30000', 'Production'),
    ('00000000-0000-0000-0000-000000000004', 'voting.quorum', '51', 'Production'),
    ('00000000-0000-0000-0000-000000000006', 'metrics.interval', '60', 'Production'),
    ('00000000-0000-0000-0000-000000000007', 'compute.max.concurrent', '10', 'Production')
ON CONFLICT (service_id, key, environment) DO NOTHING;

-- =====================================================
-- AUTHENTICATION & AUTHORIZATION
-- =====================================================

-- Insert default roles
INSERT INTO auth.roles (id, name, description) VALUES
    ('10000000-0000-0000-0000-000000000001', 'Administrator', 'Full system access'),
    ('10000000-0000-0000-0000-000000000002', 'User', 'Standard user access'),
    ('10000000-0000-0000-0000-000000000003', 'Developer', 'Developer access for API and services'),
    ('10000000-0000-0000-0000-000000000004', 'Auditor', 'Read-only access for auditing'),
    ('10000000-0000-0000-0000-000000000005', 'Operator', 'Service operation and monitoring')
ON CONFLICT (name) DO NOTHING;

-- Insert default permissions
INSERT INTO auth.permissions (id, name, description, resource, action) VALUES
    ('20000000-0000-0000-0000-000000000001', 'storage:read', 'Read from confidential storage', 'storage', 'read'),
    ('20000000-0000-0000-0000-000000000002', 'storage:write', 'Write to confidential storage', 'storage', 'write'),
    ('20000000-0000-0000-0000-000000000003', 'storage:delete', 'Delete from confidential storage', 'storage', 'delete'),
    ('20000000-0000-0000-0000-000000000004', 'oracle:read', 'Read oracle data', 'oracle', 'read'),
    ('20000000-0000-0000-0000-000000000005', 'oracle:submit', 'Submit oracle data', 'oracle', 'submit'),
    ('20000000-0000-0000-0000-000000000006', 'voting:participate', 'Participate in voting', 'voting', 'participate'),
    ('20000000-0000-0000-0000-000000000007', 'voting:create', 'Create voting proposals', 'voting', 'create'),
    ('20000000-0000-0000-0000-000000000008', 'compute:execute', 'Execute computations', 'compute', 'execute'),
    ('20000000-0000-0000-0000-000000000009', 'compute:register', 'Register new computations', 'compute', 'register'),
    ('20000000-0000-0000-0000-000000000010', 'monitoring:view', 'View monitoring data', 'monitoring', 'view'),
    ('20000000-0000-0000-0000-000000000011', 'monitoring:alert', 'Manage monitoring alerts', 'monitoring', 'alert'),
    ('20000000-0000-0000-0000-000000000012', 'admin:users', 'Manage users', 'admin', 'users'),
    ('20000000-0000-0000-0000-000000000013', 'admin:roles', 'Manage roles', 'admin', 'roles'),
    ('20000000-0000-0000-0000-000000000014', 'admin:services', 'Manage services', 'admin', 'services')
ON CONFLICT (name) DO NOTHING;

-- Assign permissions to roles
-- Administrator gets all permissions
INSERT INTO auth.role_permissions (role_id, permission_id)
SELECT '10000000-0000-0000-0000-000000000001', id FROM auth.permissions
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- User role permissions
INSERT INTO auth.role_permissions (role_id, permission_id) VALUES
    ('10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001'), -- storage:read
    ('10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000002'), -- storage:write
    ('10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000004'), -- oracle:read
    ('10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000006'), -- voting:participate
    ('10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000008'), -- compute:execute
    ('10000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000010')  -- monitoring:view
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Developer role permissions
INSERT INTO auth.role_permissions (role_id, permission_id) VALUES
    ('10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000001'), -- storage:read
    ('10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000002'), -- storage:write
    ('10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000003'), -- storage:delete
    ('10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000004'), -- oracle:read
    ('10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000005'), -- oracle:submit
    ('10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000008'), -- compute:execute
    ('10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000009'), -- compute:register
    ('10000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000010')  -- monitoring:view
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Auditor role permissions (read-only)
INSERT INTO auth.role_permissions (role_id, permission_id) VALUES
    ('10000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000001'), -- storage:read
    ('10000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000004'), -- oracle:read
    ('10000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000010')  -- monitoring:view
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- Operator role permissions
INSERT INTO auth.role_permissions (role_id, permission_id) VALUES
    ('10000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000010'), -- monitoring:view
    ('10000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000011'), -- monitoring:alert
    ('10000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000014')  -- admin:services
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- =====================================================
-- SGX SEALING POLICIES
-- =====================================================

INSERT INTO sgx.sealing_policies (id, name, policy_type, description, expiration_hours, allow_unseal, require_attestation, policy_rules) VALUES
    ('30000000-0000-0000-0000-000000000001', 'Default_MRSIGNER', 'MRSIGNER', 'Default MRSIGNER policy - binds to enclave signer', 24, true, false, 
     '{"policyType":"MRSIGNER","expirationHours":24,"requireAttestation":false}'),
    ('30000000-0000-0000-0000-000000000002', 'Default_MRENCLAVE', 'MRENCLAVE', 'Default MRENCLAVE policy - binds to specific enclave measurement', 12, true, true,
     '{"policyType":"MRENCLAVE","expirationHours":12,"requireAttestation":true}'),
    ('30000000-0000-0000-0000-000000000003', 'HighSecurity', 'CUSTOM', 'High security policy with strict attestation requirements', 6, true, true,
     '{"policyType":"CUSTOM","expirationHours":6,"requireAttestation":true,"minISVSvn":1}'),
    ('30000000-0000-0000-0000-000000000004', 'LongTerm', 'MRSIGNER', 'Long-term storage policy', 168, true, false,
     '{"policyType":"MRSIGNER","expirationHours":168,"requireAttestation":false}'),
    ('30000000-0000-0000-0000-000000000005', 'Ephemeral', 'CUSTOM', 'Short-lived ephemeral data policy', 1, true, false,
     '{"policyType":"CUSTOM","expirationHours":1,"requireAttestation":false}')
ON CONFLICT (name) DO NOTHING;

-- =====================================================
-- COMPUTE SERVICE SAMPLE DATA
-- =====================================================

-- Insert sample computations
INSERT INTO compute.computations (id, name, description, computation_type, code, version, author, blockchain_type) VALUES
    ('40000000-0000-0000-0000-000000000001', 'SimpleCalculator', 'Basic arithmetic calculator', 'JavaScript',
     'function calculate(a, b, op) { switch(op) { case "+": return a+b; case "-": return a-b; case "*": return a*b; case "/": return a/b; default: return 0; } }',
     '1.0.0', 'System', 'NeoN3'),
    ('40000000-0000-0000-0000-000000000002', 'HashGenerator', 'Generate SHA256 hash', 'JavaScript',
     'function generateHash(input) { const crypto = require("crypto"); return crypto.createHash("sha256").update(input).digest("hex"); }',
     '1.0.0', 'System', 'NeoX'),
    ('40000000-0000-0000-0000-000000000003', 'DataValidator', 'Validate data format', 'JavaScript',
     'function validate(data, schema) { /* validation logic */ return true; }',
     '1.0.0', 'System', 'NeoN3')
ON CONFLICT (name, version) DO NOTHING;

-- =====================================================
-- DEFAULT USERS (For Development/Testing Only)
-- =====================================================
-- Note: In production, users should be created through the application

-- Insert test users (passwords should be properly hashed in production)
-- Password for all test users: "TestPassword123!"
INSERT INTO core.users (id, username, email, password_hash) VALUES
    ('50000000-0000-0000-0000-000000000001', 'admin', 'admin@neoservice.local', 
     '$2a$10$K.0gVBuPGU5YmVKqFwLZeOYljY1nn6QHNCoL3j.cGQbBB1BBHghGi'),
    ('50000000-0000-0000-0000-000000000002', 'developer', 'developer@neoservice.local',
     '$2a$10$K.0gVBuPGU5YmVKqFwLZeOYljY1nn6QHNCoL3j.cGQbBB1BBHghGi'),
    ('50000000-0000-0000-0000-000000000003', 'auditor', 'auditor@neoservice.local',
     '$2a$10$K.0gVBuPGU5YmVKqFwLZeOYljY1nn6QHNCoL3j.cGQbBB1BBHghGi')
ON CONFLICT (username) DO NOTHING;

-- Assign roles to test users
INSERT INTO auth.user_roles (user_id, role_id) VALUES
    ('50000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001'), -- admin -> Administrator
    ('50000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000003'), -- developer -> Developer
    ('50000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000004')  -- auditor -> Auditor
ON CONFLICT (user_id, role_id) DO NOTHING;

-- =====================================================
-- MONITORING INITIAL DATA
-- =====================================================

-- Insert initial health check results for services
INSERT INTO core.health_check_results (service_id, check_type, status, message, response_time_ms) VALUES
    ('00000000-0000-0000-0000-000000000001', 'Startup', 'Healthy', 'Service started successfully', 150),
    ('00000000-0000-0000-0000-000000000002', 'Startup', 'Healthy', 'Service started successfully', 200),
    ('00000000-0000-0000-0000-000000000003', 'Startup', 'Healthy', 'Service started successfully', 175),
    ('00000000-0000-0000-0000-000000000004', 'Startup', 'Healthy', 'Service started successfully', 180),
    ('00000000-0000-0000-0000-000000000005', 'Startup', 'Healthy', 'Service started successfully', 210),
    ('00000000-0000-0000-0000-000000000006', 'Startup', 'Healthy', 'Service started successfully', 120),
    ('00000000-0000-0000-0000-000000000007', 'Startup', 'Healthy', 'Service started successfully', 190),
    ('00000000-0000-0000-0000-000000000008', 'Startup', 'Healthy', 'Service started successfully', 220),
    ('00000000-0000-0000-0000-000000000009', 'Startup', 'Healthy', 'Service started successfully', 160),
    ('00000000-0000-0000-0000-000000000010', 'Startup', 'Healthy', 'Service started successfully', 170);

-- =====================================================
-- VALIDATION
-- =====================================================

-- Validate seed data was inserted correctly
DO $$
DECLARE
    service_count INTEGER;
    role_count INTEGER;
    permission_count INTEGER;
    policy_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO service_count FROM core.services;
    SELECT COUNT(*) INTO role_count FROM auth.roles;
    SELECT COUNT(*) INTO permission_count FROM auth.permissions;
    SELECT COUNT(*) INTO policy_count FROM sgx.sealing_policies;
    
    RAISE NOTICE 'Seed data validation:';
    RAISE NOTICE '  Services: %', service_count;
    RAISE NOTICE '  Roles: %', role_count;
    RAISE NOTICE '  Permissions: %', permission_count;
    RAISE NOTICE '  SGX Policies: %', policy_count;
    
    IF service_count < 10 THEN
        RAISE WARNING 'Expected at least 10 services, found %', service_count;
    END IF;
    
    IF role_count < 5 THEN
        RAISE WARNING 'Expected at least 5 roles, found %', role_count;
    END IF;
    
    IF permission_count < 14 THEN
        RAISE WARNING 'Expected at least 14 permissions, found %', permission_count;
    END IF;
    
    IF policy_count < 5 THEN
        RAISE WARNING 'Expected at least 5 SGX policies, found %', policy_count;
    END IF;
END;
$$;
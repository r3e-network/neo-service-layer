# Compliance Service Workflow

```mermaid
sequenceDiagram
    participant App as Application
    participant Host as TeeEnclaveHost
    participant Interface as OpenEnclaveInterface
    participant Enclave as OpenEnclaveEnclave
    participant Compliance as ComplianceService
    
    Note over App,Compliance: Code Compliance Verification
    
    App->>Host: VerifyComplianceAsync(code, userId, functionId, complianceRules)
    Host->>Interface: VerifyComplianceAsync
    Interface->>Enclave: verify_compliance
    Enclave->>Compliance: verify_compliance
    
    Compliance->>Compliance: parse_compliance_rules(complianceRules)
    
    Compliance->>Compliance: check_code_for_prohibited_apis(code, prohibitedApis)
    Compliance->>Compliance: check_code_for_data_access(code, prohibitedData)
    Compliance->>Compliance: check_code_for_network_access(code, allowNetworkAccess)
    Compliance->>Compliance: check_code_for_resource_usage(code, maxGas)
    
    Compliance->>Compliance: create_compliance_result(functionId, userId, isCompliant, violations)
    Compliance->>Compliance: store_compliance_status(functionId, jurisdiction, result)
    
    Compliance-->>Enclave: result
    Enclave-->>Interface: result
    Interface-->>Host: result
    Host-->>App: result
    
    Note over App,Compliance: Compliance Rules Management
    
    App->>Host: GetComplianceRulesAsync(jurisdiction)
    Host->>Interface: GetComplianceRulesAsync
    Interface->>Enclave: get_compliance_rules
    Enclave->>Compliance: get_compliance_rules
    Compliance-->>Enclave: rules
    Enclave-->>Interface: rules
    Interface-->>Host: rules
    Host-->>App: rules
    
    App->>Host: SetComplianceRulesAsync(jurisdiction, rules)
    Host->>Interface: SetComplianceRulesAsync
    Interface->>Enclave: set_compliance_rules
    Enclave->>Compliance: set_compliance_rules
    Compliance->>Compliance: validate_rules(rules)
    Compliance->>Compliance: store_rules(jurisdiction, rules)
    Compliance-->>Enclave: success
    Enclave-->>Interface: success
    Interface-->>Host: success
    Host-->>App: success
    
    Note over App,Compliance: Identity Verification
    
    App->>Host: VerifyIdentityAsync(userId, identityData, jurisdiction)
    Host->>Interface: VerifyIdentityAsync
    Interface->>Enclave: verify_identity
    Enclave->>Compliance: verify_identity
    
    Compliance->>Compliance: get_identity_rules(jurisdiction)
    Compliance->>Compliance: parse_identity_data(identityData)
    Compliance->>Compliance: check_required_fields(identityData, requiredFields)
    
    Compliance->>Compliance: create_identity_result(userId, jurisdiction, isVerified, violations)
    Compliance->>Compliance: store_identity_status(userId, jurisdiction, result)
    
    Compliance-->>Enclave: result
    Enclave-->>Interface: result
    Interface-->>Host: result
    Host-->>App: result
```

## Workflow Description

### Code Compliance Verification

1. The application calls VerifyComplianceAsync with the JavaScript code, user ID, function ID, and compliance rules.
2. The TeeEnclaveHost forwards the call to the OpenEnclaveInterface.
3. The OpenEnclaveInterface calls the verify_compliance method of the OpenEnclaveEnclave.
4. The OpenEnclaveEnclave calls the verify_compliance method of the ComplianceService.
5. The ComplianceService parses the compliance rules.
6. The ComplianceService performs various checks on the code:
   - Checks for prohibited APIs
   - Checks for prohibited data access
   - Checks for network access
   - Checks for resource usage
7. The ComplianceService creates a compliance result containing the function ID, user ID, compliance status, and any violations.
8. The ComplianceService stores the compliance status for the function.
9. The compliance result is returned to the application.

### Compliance Rules Management

1. **Getting Compliance Rules**:
   - The application calls GetComplianceRulesAsync with the jurisdiction.
   - The TeeEnclaveHost forwards the call to the OpenEnclaveInterface.
   - The OpenEnclaveInterface calls the get_compliance_rules method of the OpenEnclaveEnclave.
   - The OpenEnclaveEnclave calls the get_compliance_rules method of the ComplianceService.
   - The compliance rules for the jurisdiction are returned to the application.

2. **Setting Compliance Rules**:
   - The application calls SetComplianceRulesAsync with the jurisdiction and rules.
   - The TeeEnclaveHost forwards the call to the OpenEnclaveInterface.
   - The OpenEnclaveInterface calls the set_compliance_rules method of the OpenEnclaveEnclave.
   - The OpenEnclaveEnclave calls the set_compliance_rules method of the ComplianceService.
   - The ComplianceService validates the rules.
   - The ComplianceService stores the rules for the jurisdiction.
   - The success status is returned to the application.

### Identity Verification

1. The application calls VerifyIdentityAsync with the user ID, identity data, and jurisdiction.
2. The TeeEnclaveHost forwards the call to the OpenEnclaveInterface.
3. The OpenEnclaveInterface calls the verify_identity method of the OpenEnclaveEnclave.
4. The OpenEnclaveEnclave calls the verify_identity method of the ComplianceService.
5. The ComplianceService gets the identity rules for the jurisdiction.
6. The ComplianceService parses the identity data.
7. The ComplianceService checks if the identity data contains all required fields.
8. The ComplianceService creates an identity result containing the user ID, jurisdiction, verification status, and any violations.
9. The ComplianceService stores the identity status for the user.
10. The identity result is returned to the application.

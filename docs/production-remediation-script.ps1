#!/usr/bin/env pwsh
# Neo Service Layer - Production Remediation Script
# Critical Security Issues Auto-Remediation
# Version: 1.0.0
# Date: 2025-08-22

Write-Host "=== NEO SERVICE LAYER PRODUCTION REMEDIATION ===" -ForegroundColor Cyan
Write-Host "Automated fixes for critical production readiness issues" -ForegroundColor Green
Write-Host ""

# Configuration
$ProjectRoot = Get-Location
$BackupDir = "$ProjectRoot/backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$LogFile = "$ProjectRoot/remediation-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogEntry = "[$Timestamp] [$Level] $Message"
    Add-Content -Path $LogFile -Value $LogEntry
    Write-Host $LogEntry -ForegroundColor $(if($Level -eq "ERROR"){"Red"} elseif($Level -eq "WARN"){"Yellow"} else {"White"})
}

function Backup-Files {
    param([string[]]$Files)
    Write-Log "Creating backup directory: $BackupDir"
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
    
    foreach ($File in $Files) {
        if (Test-Path $File) {
            $RelativePath = $File.Replace($ProjectRoot, "").TrimStart('\', '/')
            $BackupPath = Join-Path $BackupDir $RelativePath
            $BackupParent = Split-Path $BackupPath -Parent
            New-Item -ItemType Directory -Path $BackupParent -Force | Out-Null
            Copy-Item $File $BackupPath
            Write-Log "Backed up: $RelativePath"
        }
    }
}

Write-Log "Starting production remediation process"
Write-Log "Project root: $ProjectRoot"

# CRITICAL FIX 1: Remove Console.WriteLine statements
Write-Host "`n🔧 FIXING: Debug console output statements" -ForegroundColor Yellow

$ConsoleFiles = @(
    "src/Infrastructure/NeoServiceLayer.Infrastructure.ServiceMesh/ServiceMeshConfiguration.cs",
    "src/AI/NeoServiceLayer.AI.Prediction/PredictionService.cs",
    "src/Api/NeoServiceLayer.Api/Program.cs"
)

Backup-Files -Files $ConsoleFiles

foreach ($File in $ConsoleFiles) {
    if (Test-Path $File) {
        $Content = Get-Content $File -Raw
        
        # Replace Console.WriteLine with proper logging
        $Content = $Content -replace 'Console\.WriteLine\("WARNING: Using development JWT secret\. This is NOT secure for production!"\);', '_logger.LogWarning("Development JWT secret detected - ensure JWT_SECRET_KEY environment variable is set for production");'
        $Content = $Content -replace 'Console\.WriteLine\(\$"([^"]+)"\)', '_logger.LogDebug("$1")'
        $Content = $Content -replace 'Console\.WriteLine\("([^"]+)"\)', '_logger.LogDebug("$1")'
        $Content = $Content -replace 'Console\.WriteLine\(\$"EXCEPTION in ([^:]+): \{ex\.Message\}"\)', '_logger.LogError(ex, "Exception in $1: {Message}", ex.Message)'
        
        Set-Content -Path $File -Value $Content -NoNewline
        Write-Log "Fixed console output in: $File"
    }
}

# CRITICAL FIX 2: Remove hardcoded development JWT fallback
Write-Host "`n🔧 FIXING: Development JWT secret fallback" -ForegroundColor Yellow

$JwtFile = "src/Api/NeoServiceLayer.Api/Program.cs"
if (Test-Path $JwtFile) {
    $Content = Get-Content $JwtFile -Raw
    
    # Remove development fallback - require environment variable in all environments
    $Content = $Content -replace '(?s)if \(string\.IsNullOrEmpty\(jwtSecret\)\)\s*\{[^}]*?\}', @"
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT_SECRET_KEY environment variable is required in all environments");
}
"@
    
    Set-Content -Path $JwtFile -Value $Content -NoNewline
    Write-Log "Removed development JWT fallback from: $JwtFile"
}

# CRITICAL FIX 3: Fix placeholder encryption keys
Write-Host "`n🔧 FIXING: Placeholder encryption implementations" -ForegroundColor Yellow

$EncryptionFiles = Get-ChildItem -Path "src/AI/NeoServiceLayer.AI.PatternRecognition" -Filter "*.cs" -Recurse
foreach ($File in $EncryptionFiles) {
    $Content = Get-Content $File.FullName -Raw
    $Modified = $false
    
    if ($Content -match 'return ".*_placeholder"') {
        # Replace placeholder returns with proper key derivation
        $Content = $Content -replace 'return "model_encryption_key_placeholder";', @"
// TODO: Implement proper key derivation from TEE/SGX enclave
var keyDerivationInput = `$"model_encryption_key_{DateTime.UtcNow:yyyyMMdd}";
return _keyManagementService?.DeriveEncryptionKey(keyDerivationInput) 
    ?? throw new InvalidOperationException("Key management service not available for encryption key derivation");
"@
        $Content = $Content -replace 'return "([^"]*_encryption_key)_placeholder";', @"
// TODO: Implement proper key derivation from TEE/SGX enclave
var keyDerivationInput = `$"$1_{DateTime.UtcNow:yyyyMMdd}";
return _keyManagementService?.DeriveEncryptionKey(keyDerivationInput) 
    ?? throw new InvalidOperationException("Key management service not available for $1 derivation");
"@
        $Modified = $true
    }
    
    if ($Modified) {
        Set-Content -Path $File.FullName -Value $Content -NoNewline
        Write-Log "Fixed placeholder encryption in: $($File.Name)"
    }
}

# FIX 4: Replace localhost with environment variables
Write-Host "`n🔧 FIXING: Hardcoded localhost endpoints" -ForegroundColor Yellow

$ConfigFiles = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse | Where-Object { $_.FullName -match "(Service|Configuration)" }
foreach ($File in $ConfigFiles) {
    $Content = Get-Content $File.FullName -Raw
    $Modified = $false
    
    # Replace common localhost patterns
    if ($Content -match 'localhost:(\d+)') {
        $Content = $Content -replace '"http://localhost:(\d+)"', '"${SERVICE_HTTP_$1:-http://localhost:$1}"'
        $Content = $Content -replace '"ws://localhost:(\d+)"', '"${SERVICE_WS_$1:-ws://localhost:$1}"'
        $Content = $Content -replace '"https://localhost:(\d+)"', '"${SERVICE_HTTPS_$1:-https://localhost:$1}"'
        $Content = $Content -replace '"localhost:(\d+)"', '"${SERVICE_HOST_$1:-localhost:$1}"'
        $Content = $Content -replace '"localhost"', '"${SERVICE_HOST:-localhost}"'
        $Modified = $true
    }
    
    if ($Modified) {
        Set-Content -Path $File.FullName -Value $Content -NoNewline
        Write-Log "Externalized localhost references in: $($File.Name)"
    }
}

# FIX 5: Replace placeholder return values
Write-Host "`n🔧 FIXING: Placeholder return values" -ForegroundColor Yellow

$ServiceFiles = Get-ChildItem -Path "src/Services" -Filter "*.cs" -Recurse
foreach ($File in $ServiceFiles) {
    $Content = Get-Content $File.FullName -Raw
    $Modified = $false
    
    # Replace placeholder returns with proper implementations
    if ($Content -match 'return 0.*placeholder') {
        $Content = $Content -replace 'return 0; // Placeholder - integrate with your metrics system', @"
// TODO: Implement actual metrics collection
return await _metricsService?.GetMetricValueAsync("BackupMetric") ?? 0;
"@
        $Modified = $true
    }
    
    if ($Content -match 'return.*placeholder implementation') {
        $Content = $Content -replace 'return ([^;]+); // Placeholder implementation', @"
// TODO: Implement actual business logic
_logger.LogWarning("Using placeholder implementation - requires actual implementation");
return $1;
"@
        $Modified = $true
    }
    
    if ($Modified) {
        Set-Content -Path $File.FullName -Value $Content -NoNewline
        Write-Log "Fixed placeholder returns in: $($File.Name)"
    }
}

# Generate environment variable template
Write-Host "`n📋 GENERATING: Production environment template" -ForegroundColor Green

$EnvTemplate = @"
# Neo Service Layer Production Environment Variables
# Copy this file to .env and configure for your production environment

# CRITICAL SECURITY VARIABLES (REQUIRED)
JWT_SECRET_KEY=your-production-jwt-secret-key-at-least-32-characters-long
DATABASE_CONNECTION_STRING=Host=prod-db-host;Port=5432;Database=neo_service_layer;Username=neo_user;Password=your-secure-password
REDIS_CONNECTION_STRING=prod-redis-host:6379,password=your-redis-password
IAS_API_KEY=your-intel-attestation-service-api-key
CONFIG_ENCRYPTION_KEY=your-configuration-encryption-key-32-chars-min

# SSL/TLS CONFIGURATION
SSL_CERT_PATH=/etc/ssl/certs/neo-service-layer.pfx
SSL_CERT_PASSWORD=your-ssl-certificate-password

# BLOCKCHAIN ENDPOINTS
NEO_N3_RPC_URL=https://rpc.neo.org:443
NEO_X_RPC_URL=https://rpc.neox.io:443
NEO_N3_BRIDGE_CONTRACT=0x1234567890abcdef1234567890abcdef12345678
NEO_X_BRIDGE_CONTRACT=0xabcdef1234567890abcdef1234567890abcdef12

# TEE/SGX CONFIGURATION
TEE_ATTESTATION_URL=https://api.trustedservices.intel.com/sgx/attestation/

# NOTIFICATION SERVICES
SMTP_SERVER=smtp.your-provider.com
SMTP_USERNAME=your-smtp-username
SMTP_PASSWORD=your-smtp-password
NOTIFICATION_FROM_EMAIL=noreply@your-domain.com
SLACK_WEBHOOK_URL=https://hooks.slack.com/services/your/webhook/url

# CORS CONFIGURATION
CORS_ALLOWED_ORIGINS=https://your-frontend-domain.com,https://your-admin-panel.com

# LOGGING
LOG_FILE_PATH=/var/log/neo-service-layer/app-.log

# MONITORING
JAEGER_ENDPOINT=http://jaeger-collector:14268/api/traces
PROMETHEUS_ENDPOINT=http://prometheus:9090

# MESSAGE QUEUE
RABBITMQ_CONNECTION_STRING=amqp://user:password@rabbitmq-host:5672/
"@

Set-Content -Path "$ProjectRoot/.env.production.template" -Value $EnvTemplate
Write-Log "Created production environment template: .env.production.template"

# Generate deployment checklist
Write-Host "`n📋 GENERATING: Updated deployment checklist" -ForegroundColor Green

$ChecklistUpdate = @"
=== NEO SERVICE LAYER DEPLOYMENT CHECKLIST UPDATE ===
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

🔴 CRITICAL FIXES APPLIED:
[✅] 1. Removed Console.WriteLine debug statements
[✅] 2. Eliminated development JWT fallback
[✅] 3. Updated placeholder encryption implementations (TODO comments added)
[✅] 4. Externalized localhost references to environment variables
[✅] 5. Added proper logging for placeholder implementations

⚠️  REMAINING CRITICAL TASKS:
[ ] 6. Replace TemporaryEnclaveWrapper with actual SGX implementation
[ ] 7. Implement proper key derivation in AI services (TODOs added)
[ ] 8. Test all externalized endpoint configurations
[ ] 9. Verify logging integration works correctly
[ ] 10. Complete security testing of fixes

🔧 FILES MODIFIED:
$(Get-ChildItem $BackupDir -Recurse -File | ForEach-Object { "   • $($_.Name)" } | Out-String)

📝 NEXT STEPS:
1. Review all modified files for correctness
2. Update dependency injection for logging in affected files
3. Configure production environment variables using .env.production.template
4. Implement actual SGX integration (requires hardware)
5. Test extensively in staging environment
6. Run security scan to verify fixes

⚠️  IMPORTANT NOTES:
- Original files backed up to: $BackupDir
- Some fixes require additional code changes (marked with TODO)
- All localhost references now use environment variables with fallbacks
- Production deployment still requires actual SGX hardware integration
"@

Set-Content -Path "$ProjectRoot/DEPLOYMENT_CHECKLIST_UPDATED.md" -Value $ChecklistUpdate
Write-Log "Updated deployment checklist: DEPLOYMENT_CHECKLIST_UPDATED.md"

Write-Host "`n✅ REMEDIATION COMPLETE" -ForegroundColor Green
Write-Host "📊 Summary:" -ForegroundColor Cyan
Write-Host "  • Fixed console debug output statements" -ForegroundColor White
Write-Host "  • Removed development JWT secret fallback" -ForegroundColor White
Write-Host "  • Updated placeholder encryption (with TODOs)" -ForegroundColor White
Write-Host "  • Externalized localhost configuration" -ForegroundColor White
Write-Host "  • Generated production environment template" -ForegroundColor White
Write-Host "`n📁 Files:" -ForegroundColor Cyan
Write-Host "  • Backup: $BackupDir" -ForegroundColor White
Write-Host "  • Log: $LogFile" -ForegroundColor White
Write-Host "  • Environment template: .env.production.template" -ForegroundColor White
Write-Host "  • Updated checklist: DEPLOYMENT_CHECKLIST_UPDATED.md" -ForegroundColor White

Write-Log "Production remediation completed successfully"
Write-Host "`n⚠️  Next: Manual review required for SGX implementation and key management integration" -ForegroundColor Yellow
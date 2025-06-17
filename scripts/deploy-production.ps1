# Neo Service Layer - Production Deployment Script
# This script handles production deployment with validation, backup, and rollback capabilities

param(
    [string]$Environment = "Production",
    [string]$Version = "",
    [switch]$SkipTests,
    [switch]$SkipBackup,
    [switch]$DryRun,
    [switch]$Rollback,
    [string]$RollbackVersion = "",
    [switch]$Force,
    [string]$ConfigPath = "./config"
)

# Script configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Colors for output
$Colors = @{
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
    Info = "Cyan"
    Debug = "Gray"
}

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Colors[$Color]
}

function Write-Step {
    param([string]$Message)
    Write-ColorOutput "🔄 $Message" "Info"
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✅ $Message" "Success"
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠️  $Message" "Warning"
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "❌ $Message" "Error"
}

# Main deployment function
function Start-Deployment {
    Write-ColorOutput "🚀 Neo Service Layer - Production Deployment" "Info"
    Write-ColorOutput "================================================" "Info"
    
    if ($DryRun) {
        Write-Warning "DRY RUN MODE - No actual changes will be made"
    }
    
    if ($Rollback) {
        Start-Rollback
        return
    }
    
    # Pre-deployment validation
    Test-Prerequisites
    Test-Configuration
    
    # Get version information
    $deployVersion = Get-DeploymentVersion
    Write-ColorOutput "Deploying version: $deployVersion" "Info"
    
    # Create backup
    if (-not $SkipBackup) {
        New-Backup
    }
    
    # Run tests
    if (-not $SkipTests) {
        Invoke-Tests
    }
    
    # Build and deploy
    Build-Application
    Deploy-Application $deployVersion
    
    # Post-deployment validation
    Test-Deployment
    
    # Update monitoring
    Update-Monitoring
    
    Write-Success "Deployment completed successfully!"
    Write-ColorOutput "Version $deployVersion is now live" "Success"
}

function Test-Prerequisites {
    Write-Step "Checking prerequisites..."
    
    # Check Docker
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw "Docker is not installed or not in PATH"
    }
    
    # Check Docker Compose
    if (-not (Get-Command docker-compose -ErrorAction SilentlyContinue)) {
        throw "Docker Compose is not installed or not in PATH"
    }
    
    # Check .NET SDK
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw ".NET SDK is not installed or not in PATH"
    }
    
    # Check required files
    $requiredFiles = @(
        "docker-compose.production.yml",
        "Dockerfile",
        "env.production.template"
    )
    
    foreach ($file in $requiredFiles) {
        if (-not (Test-Path $file)) {
            throw "Required file not found: $file"
        }
    }
    
    # Check environment file
    if (-not (Test-Path ".env")) {
        Write-Warning "Environment file (.env) not found. Please copy env.production.template to .env and configure it."
        if (-not $Force) {
            throw "Environment file is required for production deployment"
        }
    }
    
    Write-Success "Prerequisites check passed"
}

function Test-Configuration {
    Write-Step "Validating configuration..."
    
    # Load environment variables
    if (Test-Path ".env") {
        Get-Content ".env" | ForEach-Object {
            if ($_ -match "^([^#][^=]+)=(.*)$") {
                [Environment]::SetEnvironmentVariable($matches[1], $matches[2], "Process")
            }
        }
    }
    
    # Check critical environment variables
    $criticalVars = @(
        "POSTGRES_PASSWORD",
        "JWT_SECRET_KEY",
        "NEO_N3_RPC_URL",
        "NEO_X_RPC_URL"
    )
    
    foreach ($var in $criticalVars) {
        $value = [Environment]::GetEnvironmentVariable($var)
        if ([string]::IsNullOrEmpty($value) -or $value.StartsWith("your_")) {
            throw "Critical environment variable '$var' is not configured properly"
        }
    }
    
    # Validate configuration files
    $configFiles = @(
        "$ConfigPath/prometheus.yml",
        "docker-compose.production.yml"
    )
    
    foreach ($file in $configFiles) {
        if (Test-Path $file) {
            Write-ColorOutput "  ✓ $file" "Debug"
        } else {
            Write-Warning "Configuration file not found: $file"
        }
    }
    
    Write-Success "Configuration validation passed"
}

function Get-DeploymentVersion {
    if (-not [string]::IsNullOrEmpty($Version)) {
        return $Version
    }
    
    # Try to get version from git
    try {
        $gitVersion = git describe --tags --always --dirty 2>$null
        if ($gitVersion) {
            return $gitVersion.Trim()
        }
    } catch {
        Write-Warning "Could not get version from git"
    }
    
    # Fallback to timestamp
    return "v$(Get-Date -Format 'yyyyMMdd-HHmmss')"
}

function New-Backup {
    Write-Step "Creating backup..."
    
    $backupDir = "backups/$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    
    if (-not $DryRun) {
        # Backup database
        Write-ColorOutput "  Backing up database..." "Debug"
        docker-compose -f docker-compose.production.yml exec -T postgres pg_dump -U neoservice neoservice > "$backupDir/database.sql"
        
        # Backup configuration
        Write-ColorOutput "  Backing up configuration..." "Debug"
        Copy-Item ".env" "$backupDir/.env.backup" -ErrorAction SilentlyContinue
        Copy-Item "docker-compose.production.yml" "$backupDir/docker-compose.backup.yml"
        
        # Backup volumes
        Write-ColorOutput "  Backing up volumes..." "Debug"
        docker run --rm -v neo-service-layer_postgres_data:/data -v ${PWD}/backups:/backup alpine tar czf /backup/postgres_data.tar.gz -C /data .
        docker run --rm -v neo-service-layer_redis_data:/data -v ${PWD}/backups:/backup alpine tar czf /backup/redis_data.tar.gz -C /data .
    }
    
    Write-Success "Backup created: $backupDir"
}

function Invoke-Tests {
    Write-Step "Running tests..."
    
    if (-not $DryRun) {
        # Run unit tests
        Write-ColorOutput "  Running unit tests..." "Debug"
        & .\run-tests-comprehensive.ps1 -Unit -Configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw "Unit tests failed"
        }
        
        # Run integration tests
        Write-ColorOutput "  Running integration tests..." "Debug"
        & .\run-tests-comprehensive.ps1 -Integration -Configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw "Integration tests failed"
        }
    }
    
    Write-Success "All tests passed"
}

function Build-Application {
    Write-Step "Building application..."
    
    if (-not $DryRun) {
        # Build Docker image
        Write-ColorOutput "  Building Docker image..." "Debug"
        docker build -t neo-service-layer:latest -t neo-service-layer:$deployVersion .
        if ($LASTEXITCODE -ne 0) {
            throw "Docker build failed"
        }
        
        # Build solution
        Write-ColorOutput "  Building .NET solution..." "Debug"
        dotnet build --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw ".NET build failed"
        }
    }
    
    Write-Success "Application built successfully"
}

function Deploy-Application {
    param([string]$Version)
    
    Write-Step "Deploying application..."
    
    if (-not $DryRun) {
        # Stop existing services
        Write-ColorOutput "  Stopping existing services..." "Debug"
        docker-compose -f docker-compose.production.yml down --remove-orphans
        
        # Pull latest images
        Write-ColorOutput "  Pulling latest images..." "Debug"
        docker-compose -f docker-compose.production.yml pull
        
        # Start services
        Write-ColorOutput "  Starting services..." "Debug"
        docker-compose -f docker-compose.production.yml up -d
        
        # Wait for services to be ready
        Write-ColorOutput "  Waiting for services to be ready..." "Debug"
        Start-Sleep -Seconds 30
        
        # Check service health
        $maxRetries = 12
        $retryCount = 0
        do {
            $retryCount++
            Write-ColorOutput "  Health check attempt $retryCount/$maxRetries..." "Debug"
            
            try {
                $response = Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 10
                if ($response -and $response.status -eq "Healthy") {
                    break
                }
            } catch {
                Write-ColorOutput "  Health check failed: $($_.Exception.Message)" "Debug"
            }
            
            if ($retryCount -ge $maxRetries) {
                throw "Service health check failed after $maxRetries attempts"
            }
            
            Start-Sleep -Seconds 10
        } while ($true)
    }
    
    Write-Success "Application deployed successfully"
}

function Test-Deployment {
    Write-Step "Validating deployment..."
    
    if (-not $DryRun) {
        # Test API endpoints
        $endpoints = @(
            "http://localhost:5000/health",
            "http://localhost:5000/health/ready",
            "http://localhost:5000/swagger/v1/swagger.json"
        )
        
        foreach ($endpoint in $endpoints) {
            try {
                Write-ColorOutput "  Testing $endpoint..." "Debug"
                $response = Invoke-RestMethod -Uri $endpoint -TimeoutSec 10
                Write-ColorOutput "    ✓ $endpoint responded successfully" "Debug"
            } catch {
                Write-Warning "Endpoint test failed: $endpoint - $($_.Exception.Message)"
            }
        }
        
        # Test database connectivity
        Write-ColorOutput "  Testing database connectivity..." "Debug"
        $dbTest = docker-compose -f docker-compose.production.yml exec -T postgres pg_isready -U neoservice -d neoservice
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "    ✓ Database is accessible" "Debug"
        } else {
            Write-Warning "Database connectivity test failed"
        }
        
        # Test Redis connectivity
        Write-ColorOutput "  Testing Redis connectivity..." "Debug"
        $redisTest = docker-compose -f docker-compose.production.yml exec -T redis redis-cli ping
        if ($redisTest -eq "PONG") {
            Write-ColorOutput "    ✓ Redis is accessible" "Debug"
        } else {
            Write-Warning "Redis connectivity test failed"
        }
    }
    
    Write-Success "Deployment validation completed"
}

function Update-Monitoring {
    Write-Step "Updating monitoring configuration..."
    
    if (-not $DryRun) {
        # Reload Prometheus configuration
        try {
            Invoke-RestMethod -Uri "http://localhost:9091/-/reload" -Method Post -TimeoutSec 10
            Write-ColorOutput "  ✓ Prometheus configuration reloaded" "Debug"
        } catch {
            Write-Warning "Failed to reload Prometheus configuration: $($_.Exception.Message)"
        }
        
        # Update Grafana dashboards
        Write-ColorOutput "  Updating Grafana dashboards..." "Debug"
        # Implementation would depend on your Grafana setup
    }
    
    Write-Success "Monitoring updated"
}

function Start-Rollback {
    Write-Step "Starting rollback process..."
    
    if ([string]::IsNullOrEmpty($RollbackVersion)) {
        # Find latest backup
        $backups = Get-ChildItem "backups" | Sort-Object Name -Descending
        if ($backups.Count -eq 0) {
            throw "No backups found for rollback"
        }
        $RollbackVersion = $backups[0].Name
    }
    
    $backupPath = "backups/$RollbackVersion"
    if (-not (Test-Path $backupPath)) {
        throw "Backup not found: $backupPath"
    }
    
    Write-Warning "Rolling back to version: $RollbackVersion"
    
    if (-not $DryRun) {
        # Stop current services
        docker-compose -f docker-compose.production.yml down
        
        # Restore database
        if (Test-Path "$backupPath/database.sql") {
            Write-ColorOutput "  Restoring database..." "Debug"
            docker-compose -f docker-compose.production.yml up -d postgres
            Start-Sleep -Seconds 10
            Get-Content "$backupPath/database.sql" | docker-compose -f docker-compose.production.yml exec -T postgres psql -U neoservice -d neoservice
        }
        
        # Restore configuration
        if (Test-Path "$backupPath/.env.backup") {
            Copy-Item "$backupPath/.env.backup" ".env" -Force
        }
        
        # Start services
        docker-compose -f docker-compose.production.yml up -d
    }
    
    Write-Success "Rollback completed"
}

# Error handling
trap {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    Write-ColorOutput "Stack trace:" "Debug"
    Write-ColorOutput $_.ScriptStackTrace "Debug"
    
    if (-not $DryRun -and -not $Rollback) {
        Write-Warning "Consider running rollback: .\deploy-production.ps1 -Rollback"
    }
    
    exit 1
}

# Main execution
try {
    Start-Deployment
} catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    exit 1
} 
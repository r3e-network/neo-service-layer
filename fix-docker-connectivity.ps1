# Docker Connectivity Fix Script for Neo Service Layer
# This script diagnoses and attempts to fix Docker connectivity issues

param(
    [switch]$Test,
    [switch]$Fix,
    [switch]$Force
)

Write-Host "Neo Service Layer Docker Connectivity Diagnostic Tool" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# Check if Docker is running
function Test-DockerRunning {
    Write-Host "`nChecking if Docker is running..." -ForegroundColor Yellow
    
    try {
        $result = docker version --format "{{.Server.Version}}" 2>$null
        if ($result) {
            Write-Host "✓ Docker is running (Server version: $result)" -ForegroundColor Green
            return $true
        }
    } catch {
        Write-Host "✗ Docker is not running or not accessible" -ForegroundColor Red
        return $false
    }
}

# Test Docker Hub connectivity
function Test-DockerHubConnectivity {
    Write-Host "`nTesting Docker Hub connectivity..." -ForegroundColor Yellow
    
    # Test basic connectivity
    try {
        $result = Test-NetConnection -ComputerName "docker.io" -Port 443 -InformationLevel Quiet
        if ($result) {
            Write-Host "✓ Can connect to docker.io:443" -ForegroundColor Green
        } else {
            Write-Host "✗ Cannot connect to docker.io:443" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "✗ Network connectivity test failed" -ForegroundColor Red
        return $false
    }
    
    # Test Docker registry API
    try {
        $response = Invoke-WebRequest -Uri "https://registry-1.docker.io/v2/" -TimeoutSec 10 -UseBasicParsing
        if ($response.StatusCode -eq 401) {  # 401 is expected for unauthenticated access
            Write-Host "✓ Docker registry API is accessible" -ForegroundColor Green
            return $true
        } else {
            Write-Host "? Unexpected response from Docker registry: $($response.StatusCode)" -ForegroundColor Yellow
            return $false
        }
    } catch {
        Write-Host "✗ Cannot access Docker registry API: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Test Docker daemon configuration
function Test-DockerConfig {
    Write-Host "`nChecking Docker daemon configuration..." -ForegroundColor Yellow
    
    $dockerConfigPath = "$env:USERPROFILE\.docker\daemon.json"
    
    if (Test-Path $dockerConfigPath) {
        Write-Host "✓ Found Docker daemon configuration at $dockerConfigPath" -ForegroundColor Green
        
        try {
            $config = Get-Content $dockerConfigPath | ConvertFrom-Json
            Write-Host "Current configuration:" -ForegroundColor Cyan
            $config | ConvertTo-Json -Depth 3 | Write-Host
            return $config
        } catch {
            Write-Host "✗ Invalid JSON in Docker daemon configuration" -ForegroundColor Red
            return $null
        }
    } else {
        Write-Host "? No Docker daemon configuration found" -ForegroundColor Yellow
        return $null
    }
}

# Create recommended Docker configuration
function Set-DockerConfig {
    param([bool]$Force = $false)
    
    Write-Host "`nCreating recommended Docker configuration..." -ForegroundColor Yellow
    
    $dockerConfigPath = "$env:USERPROFILE\.docker\daemon.json"
    $dockerDir = Split-Path $dockerConfigPath -Parent
    
    # Ensure .docker directory exists
    if (!(Test-Path $dockerDir)) {
        New-Item -ItemType Directory -Path $dockerDir -Force | Out-Null
    }
    
    # Backup existing config
    if ((Test-Path $dockerConfigPath) -and !$Force) {
        $backupPath = "$dockerConfigPath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        Copy-Item $dockerConfigPath $backupPath
        Write-Host "✓ Backed up existing configuration to $backupPath" -ForegroundColor Green
    }
    
    # Create optimized configuration
    $config = @{
        "ipv6" = $false
        "fixed-cidr-v6" = ""
        "experimental" = $false
        "ip-forward" = $true
        "dns" = @("8.8.8.8", "8.8.4.4", "1.1.1.1")
        "registry-mirrors" = @("https://mirror.gcr.io")
        "max-concurrent-downloads" = 3
        "max-concurrent-uploads" = 5
    }
    
    try {
        $config | ConvertTo-Json -Depth 3 | Set-Content $dockerConfigPath -Encoding UTF8
        Write-Host "✓ Created optimized Docker configuration" -ForegroundColor Green
        Write-Host "Configuration saved to: $dockerConfigPath" -ForegroundColor Cyan
        return $true
    } catch {
        Write-Host "✗ Failed to create Docker configuration: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Test Docker hello-world
function Test-DockerHelloWorld {
    Write-Host "`nTesting Docker with hello-world image..." -ForegroundColor Yellow
    
    try {
        $result = docker run --rm hello-world 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Docker hello-world test passed" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ Docker hello-world test failed:" -ForegroundColor Red
            Write-Host $result -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "✗ Failed to run Docker hello-world test: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Pre-pull required images
function Get-RequiredImages {
    Write-Host "`nPre-pulling required images for Neo Service Layer..." -ForegroundColor Yellow
    
    $images = @(
        "mcr.microsoft.com/dotnet/sdk:8.0-jammy",
        "mcr.microsoft.com/dotnet/aspnet:8.0",
        "ubuntu:22.04",
        "ubuntu:24.04"
    )
    
    $success = $true
    foreach ($image in $images) {
        Write-Host "Pulling $image..." -ForegroundColor Cyan
        try {
            docker pull $image
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Successfully pulled $image" -ForegroundColor Green
            } else {
                Write-Host "✗ Failed to pull $image" -ForegroundColor Red
                $success = $false
            }
        } catch {
            Write-Host "✗ Error pulling $image: $($_.Exception.Message)" -ForegroundColor Red
            $success = $false
        }
    }
    
    return $success
}

# Restart Docker Desktop
function Restart-DockerDesktop {
    Write-Host "`nRestarting Docker Desktop..." -ForegroundColor Yellow
    
    # Stop Docker Desktop
    Get-Process "Docker Desktop" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 5
    
    # Start Docker Desktop
    $dockerPath = "${env:ProgramFiles}\Docker\Docker\Docker Desktop.exe"
    if (Test-Path $dockerPath) {
        Start-Process $dockerPath
        Write-Host "✓ Docker Desktop restart initiated" -ForegroundColor Green
        Write-Host "Please wait 30-60 seconds for Docker to fully start..." -ForegroundColor Yellow
        return $true
    } else {
        Write-Host "✗ Could not find Docker Desktop executable" -ForegroundColor Red
        return $false
    }
}

# Main diagnostic function
function Start-Diagnosis {
    Write-Host "`n=== DIAGNOSIS PHASE ===" -ForegroundColor Magenta
    
    $issues = @()
    
    # Check Docker running
    if (!(Test-DockerRunning)) {
        $issues += "Docker is not running"
    }
    
    # Check connectivity
    if (!(Test-DockerHubConnectivity)) {
        $issues += "Docker Hub connectivity issues"
    }
    
    # Check configuration
    $config = Test-DockerConfig
    
    # Test hello-world
    if (!(Test-DockerHelloWorld)) {
        $issues += "Docker functionality test failed"
    }
    
    return $issues
}

# Main fix function
function Start-Fix {
    Write-Host "`n=== FIX PHASE ===" -ForegroundColor Magenta
    
    # Apply Docker configuration fix
    $configFixed = Set-DockerConfig -Force:$Force
    
    if ($configFixed) {
        # Restart Docker Desktop
        Write-Host "Docker configuration updated. Restart required." -ForegroundColor Yellow
        $restart = Read-Host "Restart Docker Desktop now? (y/N)"
        
        if ($restart -eq 'y' -or $restart -eq 'Y') {
            Restart-DockerDesktop
            
            Write-Host "Waiting for Docker to restart..." -ForegroundColor Yellow
            Start-Sleep -Seconds 30
            
            # Re-test after restart
            Write-Host "`n=== POST-FIX VERIFICATION ===" -ForegroundColor Magenta
            if (Test-DockerRunning) {
                if (Test-DockerHelloWorld) {
                    Write-Host "✓ Docker connectivity issue resolved!" -ForegroundColor Green
                    
                    # Try to pre-pull images
                    $pullSuccess = Get-RequiredImages
                    if ($pullSuccess) {
                        Write-Host "✓ All required images pulled successfully" -ForegroundColor Green
                        Write-Host "`nYou can now try running your devcontainer again." -ForegroundColor Cyan
                    } else {
                        Write-Host "? Some images failed to pull, but basic connectivity is working" -ForegroundColor Yellow
                    }
                } else {
                    Write-Host "? Docker is running but connectivity issues persist" -ForegroundColor Yellow
                    Write-Host "You may need to manually configure proxy settings" -ForegroundColor Yellow
                }
            } else {
                Write-Host "✗ Docker failed to start properly after restart" -ForegroundColor Red
            }
        } else {
            Write-Host "Please restart Docker Desktop manually and run this script again." -ForegroundColor Yellow
        }
    }
}

# Main execution
Write-Host "`nStarting Docker connectivity diagnosis..." -ForegroundColor White

if ($Test) {
    $issues = Start-Diagnosis
    
    if ($issues.Count -eq 0) {
        Write-Host "`n✓ No issues detected! Docker connectivity appears to be working." -ForegroundColor Green
    } else {
        Write-Host "`n✗ Issues detected:" -ForegroundColor Red
        $issues | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        Write-Host "`nRun with -Fix parameter to attempt automatic resolution." -ForegroundColor Yellow
    }
} elseif ($Fix) {
    $issues = Start-Diagnosis
    
    if ($issues.Count -eq 0) {
        Write-Host "`n✓ No issues detected!" -ForegroundColor Green
    } else {
        Write-Host "`nAttempting to fix detected issues..." -ForegroundColor Yellow
        Start-Fix
    }
} else {
    Write-Host "`nUsage:" -ForegroundColor White
    Write-Host "  .\fix-docker-connectivity.ps1 -Test    # Run diagnostic tests only" -ForegroundColor Gray
    Write-Host "  .\fix-docker-connectivity.ps1 -Fix     # Run diagnosis and attempt fixes" -ForegroundColor Gray
    Write-Host "  .\fix-docker-connectivity.ps1 -Fix -Force  # Force overwrite existing config" -ForegroundColor Gray
    Write-Host ""
    Write-Host "This script will help resolve the Docker connectivity issues you're experiencing." -ForegroundColor Cyan
    Write-Host "Run with -Test first to see what issues are detected." -ForegroundColor Cyan
}

Write-Host "`nFor more detailed troubleshooting, see DOCKER_CONNECTIVITY_TROUBLESHOOTING.md" -ForegroundColor Gray
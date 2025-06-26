# Neo Service Layer - Docker Network Troubleshooting Script for Windows
# This script diagnoses and fixes common Docker connectivity issues

Write-Host "🔧 Neo Service Layer - Docker Network Troubleshooting" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

# Check if Docker Desktop is running
Write-Host "`n📋 Checking Docker Desktop status..." -ForegroundColor Yellow
try {
    $dockerInfo = docker info 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Docker Desktop is running" -ForegroundColor Green
    } else {
        Write-Host "❌ Docker Desktop is not running or not responding" -ForegroundColor Red
        Write-Host "Please start Docker Desktop and try again." -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "❌ Docker command not found. Please install Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check network connectivity to Docker Hub
Write-Host "`n🌐 Testing network connectivity..." -ForegroundColor Yellow

$testUrls = @(
    "registry-1.docker.io",
    "auth.docker.io",
    "production.cloudflare.docker.com"
)

foreach ($url in $testUrls) {
    try {
        $result = Test-NetConnection -ComputerName $url -Port 443 -InformationLevel Quiet
        if ($result) {
            Write-Host "✅ Can reach $url" -ForegroundColor Green
        } else {
            Write-Host "❌ Cannot reach $url" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Error testing $url : $_" -ForegroundColor Red
    }
}

# Check DNS resolution
Write-Host "`n🔍 Testing DNS resolution..." -ForegroundColor Yellow
try {
    $dnsResult = Resolve-DnsName registry-1.docker.io -ErrorAction Stop
    Write-Host "✅ DNS resolution working for Docker Hub" -ForegroundColor Green
    Write-Host "   Resolved to: $($dnsResult.IPAddress -join ', ')" -ForegroundColor Gray
} catch {
    Write-Host "❌ DNS resolution failed for Docker Hub" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
}

# Solution 1: Reset Docker Desktop network settings
Write-Host "`n🔄 Solution 1: Reset Docker Desktop..." -ForegroundColor Yellow
$resetDocker = Read-Host "Do you want to reset Docker Desktop network settings? (y/n)"
if ($resetDocker -eq 'y' -or $resetDocker -eq 'Y') {
    Write-Host "Stopping Docker Desktop..." -ForegroundColor Yellow
    Stop-Process -Name "Docker Desktop" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 5
    
    Write-Host "Starting Docker Desktop..." -ForegroundColor Yellow
    Start-Process "Docker Desktop" -ErrorAction SilentlyContinue
    Write-Host "⏳ Please wait for Docker Desktop to fully start (30-60 seconds)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 30
}

# Solution 2: Configure Docker to use different DNS
Write-Host "`n🌐 Solution 2: Configure Docker DNS..." -ForegroundColor Yellow
$configureDns = Read-Host "Do you want to configure Docker to use Google DNS? (y/n)"
if ($configureDns -eq 'y' -or $configureDns -eq 'Y') {
    $dockerConfig = @{
        "dns" = @("8.8.8.8", "8.8.4.4")
        "registry-mirrors" = @("https://mirror.gcr.io")
    }
    
    $configPath = "$env:APPDATA\Docker\settings.json"
    Write-Host "Updating Docker configuration at: $configPath" -ForegroundColor Yellow
    
    if (Test-Path $configPath) {
        $existingConfig = Get-Content $configPath | ConvertFrom-Json
        $existingConfig.dns = $dockerConfig.dns
        $existingConfig | ConvertTo-Json -Depth 10 | Set-Content $configPath
        Write-Host "✅ Docker DNS configuration updated" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Docker config file not found. Please configure DNS manually in Docker Desktop settings." -ForegroundColor Yellow
    }
}

# Solution 3: Use alternative base images
Write-Host "`n🐳 Solution 3: Test alternative approach..." -ForegroundColor Yellow
$useSimple = Read-Host "Do you want to use the simplified devcontainer? (y/n)"
if ($useSimple -eq 'y' -or $useSimple -eq 'Y') {
    Write-Host "Copying simplified devcontainer configuration..." -ForegroundColor Yellow
    
    if (Test-Path ".devcontainer\devcontainer.simple.json") {
        Copy-Item ".devcontainer\devcontainer.simple.json" ".devcontainer\devcontainer.json" -Force
        Write-Host "✅ Switched to simplified devcontainer configuration" -ForegroundColor Green
        Write-Host "   You can now try building the devcontainer again" -ForegroundColor Green
    } else {
        Write-Host "❌ Simplified devcontainer config not found" -ForegroundColor Red
    }
}

# Solution 4: Manual image pull
Write-Host "`n📥 Solution 4: Pre-pull required images..." -ForegroundColor Yellow
$prePull = Read-Host "Do you want to pre-pull required Docker images? (y/n)"
if ($prePull -eq 'y' -or $prePull -eq 'Y') {
    $images = @(
        "mcr.microsoft.com/dotnet/sdk:9.0-ubuntu-24.04",
        "ubuntu:24.04"
    )
    
    foreach ($image in $images) {
        Write-Host "Pulling $image..." -ForegroundColor Yellow
        try {
            docker pull $image
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Successfully pulled $image" -ForegroundColor Green
            } else {
                Write-Host "❌ Failed to pull $image" -ForegroundColor Red
            }
        } catch {
            Write-Host "❌ Error pulling $image : $_" -ForegroundColor Red
        }
    }
}

# Final recommendations
Write-Host "`n📋 Additional Recommendations:" -ForegroundColor Cyan
Write-Host "1. Check if your antivirus/firewall is blocking Docker" -ForegroundColor White
Write-Host "2. Try connecting to a different network (mobile hotspot)" -ForegroundColor White
Write-Host "3. Check if your company has a proxy or firewall blocking Docker Hub" -ForegroundColor White
Write-Host "4. Try running Docker Desktop as Administrator" -ForegroundColor White
Write-Host "5. Update Docker Desktop to the latest version" -ForegroundColor White

Write-Host "`n🚀 Next Steps:" -ForegroundColor Cyan
Write-Host "1. If issues persist, use the simplified devcontainer: '.devcontainer\devcontainer.simple.json'" -ForegroundColor White
Write-Host "2. Alternatively, develop locally without containers using: './start-dev.ps1'" -ForegroundColor White
Write-Host "3. For corporate networks, contact IT for Docker Hub access" -ForegroundColor White

Write-Host "`nTroubleshooting complete! 🎉" -ForegroundColor Green 
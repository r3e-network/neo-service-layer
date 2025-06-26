# 🚀 Neo Service Layer - Development Startup Script (PowerShell)
# For Windows environments

param(
    [switch]$Force,
    [string]$Port = "5000"
)

Write-Host "🚀 Starting Neo Service Layer Web Application..." -ForegroundColor Green

# Function to check if port is available
function Test-Port {
    param([int]$Port)
    $tcpObject = New-Object System.Net.NetworkInformation.TcpListener($Port)
    try {
        $tcpObject.Start()
        $tcpObject.Stop()
        return $true
    }
    catch {
        return $false
    }
}

# Function to stop processes on a specific port
function Stop-ProcessOnPort {
    param([int]$Port)
    $processes = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "⚠️  Port $Port is in use. Stopping conflicting processes..." -ForegroundColor Yellow
        foreach ($process in $processes) {
            try {
                Stop-Process -Id $process.OwningProcess -Force -ErrorAction SilentlyContinue
                Write-Host "✅ Stopped process $($process.OwningProcess)" -ForegroundColor Green
            }
            catch {
                Write-Warning "Could not stop process $($process.OwningProcess)"
            }
        }
        Start-Sleep -Seconds 2
    }
}

# Check and free ports
$ports = @(5000, 5001)
foreach ($port in $ports) {
    if (!(Test-Port $port) -or $Force) {
        Stop-ProcessOnPort $port
    }
}

# Check if .NET is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "📦 .NET Version: $dotnetVersion" -ForegroundColor Cyan
}
catch {
    Write-Host "❌ .NET SDK not found! Please install .NET 9.0 SDK" -ForegroundColor Red
    exit 1
}

# Restore packages if needed
if (!(Test-Path "src\Web\NeoServiceLayer.Web\bin") -or !(Test-Path "src\Web\NeoServiceLayer.Web\obj")) {
    Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Cyan
    dotnet restore src\Web\NeoServiceLayer.Web\NeoServiceLayer.Web.csproj
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Package restore failed!" -ForegroundColor Red
        exit 1
    }
}

# Build the application
Write-Host "🔨 Building application..." -ForegroundColor Cyan
dotnet build src\Web\NeoServiceLayer.Web\NeoServiceLayer.Web.csproj --configuration Debug --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed! Please check the errors above." -ForegroundColor Red
    exit 1
}

# Create logs directory if it doesn't exist
if (!(Test-Path "logs")) {
    New-Item -ItemType Directory -Path "logs" | Out-Null
}

# Set environment variables
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5000;https://localhost:5001"

# Display startup information
Write-Host ""
Write-Host "🌐 Starting web application at:" -ForegroundColor Green
Write-Host "   HTTP:  http://localhost:5000" -ForegroundColor Cyan
Write-Host "   HTTPS: https://localhost:5001" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Available endpoints:" -ForegroundColor Yellow
Write-Host "   🏠 Home:           http://localhost:5000" -ForegroundColor White
Write-Host "   🔧 Services:       http://localhost:5000/Services" -ForegroundColor White
Write-Host "   📊 Dashboard:      http://localhost:5000/Dashboard" -ForegroundColor White
Write-Host "   🧪 Demo:           http://localhost:5000/Demo" -ForegroundColor White
Write-Host "   🔍 Health Check:   http://localhost:5000/health" -ForegroundColor White
Write-Host "   📚 API Docs:       http://localhost:5000/swagger" -ForegroundColor White
Write-Host "   🔑 Demo Token:     http://localhost:5000/api/auth/demo-token" -ForegroundColor White
Write-Host "   ℹ️  Info:          http://localhost:5000/api/info" -ForegroundColor White
Write-Host ""
Write-Host "⌨️  Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

# Start the application
try {
    dotnet run --project src\Web\NeoServiceLayer.Web\NeoServiceLayer.Web.csproj `
        --configuration Debug `
        --no-build `
        --environment Development
}
catch {
    Write-Host "❌ Failed to start the application!" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
} 
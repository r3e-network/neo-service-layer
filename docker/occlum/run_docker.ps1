# PowerShell script to run the Docker setup for Occlum
# This script should be run from the root of the project

# Stop on first error
$ErrorActionPreference = "Stop"

Write-Host "Running Docker setup for Occlum..." -ForegroundColor Green

# Check if Docker is installed
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Docker not found. Please install Docker." -ForegroundColor Red
    exit 1
}

# Check if Docker Compose is installed
if (-not (Get-Command docker-compose -ErrorAction SilentlyContinue)) {
    Write-Host "Docker Compose not found. Please install Docker Compose." -ForegroundColor Red
    exit 1
}

# Check if we're in simulation mode
$simulationMode = $env:OCCLUM_SIMULATION -eq "1"
if ($simulationMode) {
    Write-Host "Running in simulation mode." -ForegroundColor Yellow
    $env:OCCLUM_SIMULATION = "1"
}

# Run Docker Compose
Write-Host "Starting Docker Compose..." -ForegroundColor Cyan
Push-Location docker/occlum
docker-compose up -d
Pop-Location

Write-Host "Docker setup for Occlum is running." -ForegroundColor Green
Write-Host "To stop the Docker setup, run: docker-compose -f docker/occlum/docker-compose.yml down" -ForegroundColor Cyan

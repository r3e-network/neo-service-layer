# Run integration tests with Docker Compose
param (
    [switch]$Simple,
    [switch]$All,
    [switch]$Build,
    [switch]$Clean,
    [switch]$KeepRunning,
    [string]$Filter,
    [switch]$Verbose
)

Write-Host "Running Neo Service Layer integration tests..." -ForegroundColor Green

# Clean up if requested
if ($Clean) {
    Write-Host "Cleaning up previous containers..." -ForegroundColor Yellow
    docker-compose -f docker-compose.tests.yml down
}

# Build if requested
if ($Build) {
    Write-Host "Building test containers..." -ForegroundColor Yellow
    docker-compose -f docker-compose.tests.yml build
}

# Prepare the command
$verbosityLevel = if ($Verbose) { "diagnostic" } else { "detailed" }
$testCommand = "dotnet test tests/NeoServiceLayer.Integration.Tests/NeoServiceLayer.Integration.Tests.csproj --logger ""console;verbosity=$verbosityLevel"""

# Add filter if specified
if ($Filter) {
    $testCommand += " --filter ""$Filter"""
}

# Run the simple test if requested
if ($Simple) {
    Write-Host "Running simple test..." -ForegroundColor Cyan
    docker-compose -f docker-compose.tests.yml run simple-test
}
# Run all tests if requested
elseif ($All) {
    Write-Host "Running all integration tests..." -ForegroundColor Cyan

    if ($KeepRunning) {
        Write-Host "Starting services and keeping them running..." -ForegroundColor Yellow
        docker-compose -f docker-compose.tests.yml up -d redis tee-host api
        docker-compose -f docker-compose.tests.yml run --rm integration-tests $testCommand
    } else {
        docker-compose -f docker-compose.tests.yml run --rm integration-tests $testCommand
    }
}
# If no specific tests were selected, run the simple test
else {
    Write-Host "Running simple test (default)..." -ForegroundColor Cyan
    docker-compose -f docker-compose.tests.yml run simple-test
}

# Clean up
if (-not $KeepRunning) {
    Write-Host "Cleaning up containers..." -ForegroundColor Yellow
    docker-compose -f docker-compose.tests.yml down
} else {
    Write-Host "Services are still running. Use 'docker-compose -f docker-compose.tests.yml down' to stop them." -ForegroundColor Yellow
}

Write-Host "Tests completed!" -ForegroundColor Green

# Run tests in Docker
param (
    [switch]$UnitTests,
    [switch]$IntegrationTests,
    [switch]$All,
    [switch]$Legacy
)

Write-Host "Running Neo Service Layer tests in Docker..." -ForegroundColor Green

if ($Legacy) {
    # Use the legacy approach with Dockerfile.test
    Write-Host "Using legacy approach with Dockerfile.test..." -ForegroundColor Yellow

    # Build the test image
    docker build -t neoservicelayer-tests -f Dockerfile.test .

    # Run the tests
    docker run --rm neoservicelayer-tests
} else {
    # Use the new approach with docker-compose.tests.yml
    if ($UnitTests -or $All) {
        Write-Host "Running unit tests..." -ForegroundColor Cyan
        docker-compose -f docker-compose.tests.yml run unit-tests
    }

    if ($IntegrationTests -or $All) {
        Write-Host "Running integration tests..." -ForegroundColor Cyan
        docker-compose -f docker-compose.tests.yml run integration-tests
    }

    # If no specific tests were selected, run all tests
    if (-not $UnitTests -and -not $IntegrationTests -and -not $All) {
        Write-Host "Running all tests..." -ForegroundColor Cyan
        docker-compose -f docker-compose.tests.yml run unit-tests
        docker-compose -f docker-compose.tests.yml run integration-tests
    }

    # Clean up
    Write-Host "Cleaning up..." -ForegroundColor Yellow
    docker-compose -f docker-compose.tests.yml down
}

Write-Host "Tests completed!" -ForegroundColor Green

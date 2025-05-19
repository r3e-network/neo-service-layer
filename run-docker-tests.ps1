# Run Neo Service Layer Tests in Docker

param (
    [switch]$Build,
    [switch]$Clean
)

Write-Host "Running Neo Service Layer Tests in Docker..." -ForegroundColor Green

# Clean up if requested
if ($Clean) {
    Write-Host "Cleaning up previous containers..." -ForegroundColor Yellow
    docker-compose -f docker-compose.simple-tests.yml down
}

# Build if requested
if ($Build) {
    Write-Host "Building test containers..." -ForegroundColor Yellow
    docker-compose -f docker-compose.simple-tests.yml build
}

# Run the tests
Write-Host "Running tests..." -ForegroundColor Cyan
docker-compose -f docker-compose.simple-tests.yml up

# Clean up
Write-Host "Cleaning up containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.simple-tests.yml down

Write-Host "Tests completed!" -ForegroundColor Green

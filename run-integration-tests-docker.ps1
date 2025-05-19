# Run integration tests using Docker Compose
Write-Host "Running Neo Service Layer integration tests in Docker..." -ForegroundColor Green

# Build and run the tests
docker-compose -f docker-compose.test.yml up --build --abort-on-container-exit

# Clean up
docker-compose -f docker-compose.test.yml down

Write-Host "Integration tests completed!" -ForegroundColor Green

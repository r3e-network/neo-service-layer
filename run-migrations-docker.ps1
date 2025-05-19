# Run database migrations in Docker
Write-Host "Running Neo Service Layer database migrations in Docker..." -ForegroundColor Green

# Build and run the migrations
docker-compose -f docker-compose.migrations.yml up --build migrations

# Clean up
docker-compose -f docker-compose.migrations.yml down

Write-Host "Database migrations completed!" -ForegroundColor Green

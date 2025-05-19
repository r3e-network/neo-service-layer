# Run the development environment using Docker Compose
Write-Host "Starting Neo Service Layer development environment..." -ForegroundColor Green

# Build and start the services
docker-compose -f docker-compose.dev.yml up -d --build

# Wait for the services to start
Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Show service status
docker-compose -f docker-compose.dev.yml ps

Write-Host "Neo Service Layer development environment is running!" -ForegroundColor Green
Write-Host "API: http://localhost:5000" -ForegroundColor Cyan
Write-Host "Swagger UI: http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor Cyan
Write-Host "Jaeger UI: http://localhost:16686" -ForegroundColor Cyan

Write-Host "`nTo run the example, use:" -ForegroundColor Yellow
Write-Host "docker-compose -f docker-compose.dev.yml run example" -ForegroundColor White

Write-Host "`nTo stop the services, use:" -ForegroundColor Yellow
Write-Host "docker-compose -f docker-compose.dev.yml down" -ForegroundColor White

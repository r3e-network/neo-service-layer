# Run the example using Docker Compose
Write-Host "Running Neo Service Layer example in Docker..." -ForegroundColor Green

# Create mock Neo N3 node directory if it doesn't exist
if (-not (Test-Path -Path "mock-neo-node")) {
    Write-Host "Creating mock Neo N3 node directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path "mock-neo-node" -Force | Out-Null
}

# Create mock Neo N3 node response file if it doesn't exist
if (-not (Test-Path -Path "mock-neo-node/index.html")) {
    Write-Host "Creating mock Neo N3 node response file..." -ForegroundColor Yellow
    $mockResponse = @"
{
    "jsonrpc": "2.0",
    "id": 1,
    "result": {
        "version": {
            "tcpport": 10333,
            "wsport": 10334,
            "nonce": 1234567890,
            "useragent": "/NEO-GO:0.99.0/"
        },
        "peers": [
            {
                "address": "127.0.0.1:10333",
                "height": 12345678
            }
        ],
        "height": 12345678,
        "lastblockhash": "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"
    }
}
"@
    Set-Content -Path "mock-neo-node/index.html" -Value $mockResponse
}

# Build and start the services
docker-compose -f docker-compose.example-mock.yml up -d --build

# Wait for the services to start
Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Show service status
docker-compose -f docker-compose.example-mock.yml ps

Write-Host "Neo Service Layer example is running!" -ForegroundColor Green
Write-Host "API: http://localhost:5000" -ForegroundColor Cyan

Write-Host "`nTo see the example output, use:" -ForegroundColor Yellow
Write-Host "docker-compose -f docker-compose.example-mock.yml logs -f example" -ForegroundColor White

Write-Host "`nTo stop the services, use:" -ForegroundColor Yellow
Write-Host "docker-compose -f docker-compose.example-mock.yml down" -ForegroundColor White

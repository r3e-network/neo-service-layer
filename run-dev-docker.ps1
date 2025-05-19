# Run the Neo Service Layer in development mode
Write-Host "Running Neo Service Layer in development mode..." -ForegroundColor Green

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

# Create data directory if it doesn't exist
if (-not (Test-Path -Path "data")) {
    Write-Host "Creating data directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path "data" -Force | Out-Null
}

# Create a sample Neo wallet file if it doesn't exist
if (-not (Test-Path -Path "data/neo-wallet.json")) {
    Write-Host "Creating sample Neo wallet file..." -ForegroundColor Yellow
    $neoWallet = @"
{
    "name": "neo-wallet",
    "version": "1.0",
    "scrypt": {
        "n": 16384,
        "r": 8,
        "p": 8
    },
    "accounts": [
        {
            "address": "NZNos2WqTbu5oCgyfss9kUJgBXJqhuYAaj",
            "label": "Default",
            "isDefault": true,
            "lock": false,
            "key": "6PYLHmDf6AjF4AsVtosmxHuPYeuyJL3SLuw7J1U8i7HxKAnYNsp61HYRfF",
            "contract": {
                "script": "DCECztQJHFBukPP8CCyiNdQQ6jH5Ddr5JgOi0EgDYfQUu0FBVuezJw==",
                "parameters": [
                    {
                        "name": "signature",
                        "type": "Signature"
                    }
                ],
                "deployed": false
            },
            "extra": null
        }
    ],
    "extra": null
}
"@
    Set-Content -Path "data/neo-wallet.json" -Value $neoWallet
}

# Run database migrations first
Write-Host "Running database migrations..." -ForegroundColor Yellow
docker-compose -f docker-compose.migrations.yml up --build migrations

# Build and start the services
Write-Host "Building and starting services..." -ForegroundColor Green
docker-compose -f docker-compose.dev.yml up -d --build

# Wait for the services to start
Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Show service status
docker-compose -f docker-compose.dev.yml ps

Write-Host "Neo Service Layer is running in development mode!" -ForegroundColor Green
Write-Host "API: http://localhost:5000" -ForegroundColor Cyan
Write-Host "TEE Host: http://localhost:5100" -ForegroundColor Cyan

Write-Host "`nTo run the example, use:" -ForegroundColor Yellow
Write-Host "docker-compose -f docker-compose.dev.yml run example" -ForegroundColor White

Write-Host "`nTo stop the services, use:" -ForegroundColor Yellow
Write-Host "docker-compose -f docker-compose.dev.yml down" -ForegroundColor White

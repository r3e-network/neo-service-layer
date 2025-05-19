# Run the Neo Service Layer in production mode
param (
    [switch]$WithSGX
)

Write-Host "Running Neo Service Layer in production mode..." -ForegroundColor Green

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

# Set environment variables
$env:SQL_PASSWORD = "StrongPassword123!"
$env:REDIS_PASSWORD = "StrongRedisPassword123!"
$env:JWT_KEY = "StrongJwtKey123!StrongJwtKey123!StrongJwtKey123!"
$env:NEO_WALLET_PASSWORD = "StrongWalletPassword123!"

if ($WithSGX) {
    Write-Host "Running with SGX hardware mode..." -ForegroundColor Yellow
    $env:SGX_MODE = "HW"
    $env:SGX_SIMULATION = "0"
    $env:SGX_SIMULATION_MODE = "false"
} else {
    Write-Host "Running with SGX simulation mode..." -ForegroundColor Yellow
    $env:SGX_MODE = "SIM"
    $env:SGX_SIMULATION = "1"
    $env:SGX_SIMULATION_MODE = "true"
}

# Run database migrations first
Write-Host "Running database migrations..." -ForegroundColor Yellow
docker-compose -f docker-compose.migrations.yml up --build migrations

# Build and start the services
Write-Host "Building and starting services..." -ForegroundColor Green
docker-compose -f docker-compose.prod.yml up -d --build

# Wait for the services to start
Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

# Show service status
docker-compose -f docker-compose.prod.yml ps

Write-Host "Neo Service Layer is running in production mode!" -ForegroundColor Green
Write-Host "API: http://localhost:5000" -ForegroundColor Cyan
Write-Host "API (HTTPS): https://localhost:5001" -ForegroundColor Cyan
Write-Host "TEE Host: http://localhost:5100" -ForegroundColor Cyan

Write-Host "`nTo stop the services, use:" -ForegroundColor Yellow
Write-Host "docker-compose -f docker-compose.prod.yml down" -ForegroundColor White

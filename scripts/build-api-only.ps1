# Neo Service Layer - API Only Build Script
# This script builds only the essential API components for deployment

param(
    [string]$Configuration = "Release",
    [switch]$Clean,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "Neo Service Layer - API Only Build" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan

# Set verbosity
$verbosity = if ($Verbose) { "normal" } else { "minimal" }

try {
    # Clean if requested
    if ($Clean) {
        Write-Host "Cleaning solution..." -ForegroundColor Yellow
        dotnet clean NeoServiceLayer.sln --configuration $Configuration --verbosity $verbosity
    }

    # Build core components first
    Write-Host "Building core components..." -ForegroundColor Green
    
    $coreProjects = @(
        "src/Core/NeoServiceLayer.Core/NeoServiceLayer.Core.csproj",
        "src/Core/NeoServiceLayer.ServiceFramework/NeoServiceLayer.ServiceFramework.csproj",
        "src/Core/NeoServiceLayer.Infrastructure/NeoServiceLayer.Infrastructure.csproj"
    )

    foreach ($project in $coreProjects) {
        Write-Host "  Building $project..." -ForegroundColor Gray
        dotnet build $project --configuration $Configuration --verbosity $verbosity
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to build $project"
        }
    }

    # Build blockchain components
    Write-Host "Building blockchain components..." -ForegroundColor Green
    
    $blockchainProjects = @(
        "src/Blockchain/NeoServiceLayer.Neo.N3/NeoServiceLayer.Neo.N3.csproj",
        "src/Blockchain/NeoServiceLayer.Neo.X/NeoServiceLayer.Neo.X.csproj"
    )

    foreach ($project in $blockchainProjects) {
        Write-Host "  Building $project..." -ForegroundColor Gray
        dotnet build $project --configuration $Configuration --verbosity $verbosity
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to build $project - continuing with other components"
        }
    }

    # Build working services only
    Write-Host "Building working services..." -ForegroundColor Green
    
    $workingServices = @(
        "src/Services/NeoServiceLayer.Services.KeyManagement/NeoServiceLayer.Services.KeyManagement.csproj",
        "src/Services/NeoServiceLayer.Services.Storage/NeoServiceLayer.Services.Storage.csproj",
        "src/Services/NeoServiceLayer.Services.Oracle/NeoServiceLayer.Services.Oracle.csproj",
        "src/Services/NeoServiceLayer.Services.Compute/NeoServiceLayer.Services.Compute.csproj",
        "src/Services/NeoServiceLayer.Services.EventSubscription/NeoServiceLayer.Services.EventSubscription.csproj",
        "src/Services/NeoServiceLayer.Services.Compliance/NeoServiceLayer.Services.Compliance.csproj",
        "src/Services/NeoServiceLayer.Services.CrossChain/NeoServiceLayer.Services.CrossChain.csproj",
        "src/Services/NeoServiceLayer.Services.Automation/NeoServiceLayer.Services.Automation.csproj",
        "src/Services/NeoServiceLayer.Services.ProofOfReserve/NeoServiceLayer.Services.ProofOfReserve.csproj",
        "src/Services/NeoServiceLayer.Services.Randomness/NeoServiceLayer.Services.Randomness.csproj"
    )

    foreach ($project in $workingServices) {
        Write-Host "  Building $project..." -ForegroundColor Gray
        dotnet build $project --configuration $Configuration --verbosity $verbosity
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to build $project - continuing with other components"
        }
    }

    # Build AI components (if they compile)
    Write-Host "Building AI components..." -ForegroundColor Green
    
    $aiProjects = @(
        "src/AI/NeoServiceLayer.AI.PatternRecognition/NeoServiceLayer.AI.PatternRecognition.csproj",
        "src/AI/NeoServiceLayer.AI.Prediction/NeoServiceLayer.AI.Prediction.csproj"
    )

    foreach ($project in $aiProjects) {
        Write-Host "  Building $project..." -ForegroundColor Gray
        dotnet build $project --configuration $Configuration --verbosity $verbosity
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to build $project - AI features may not be available"
        }
    }

    # Finally build the API
    Write-Host "Building API..." -ForegroundColor Green
    dotnet build "src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj" --configuration $Configuration --verbosity $verbosity
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "API build completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Build artifacts:" -ForegroundColor Cyan
        Write-Host "  API: src/Api/NeoServiceLayer.Api/bin/$Configuration/net9.0/" -ForegroundColor Gray
        Write-Host ""
        Write-Host "To run the API:" -ForegroundColor Cyan
        Write-Host "  cd src/Api/NeoServiceLayer.Api" -ForegroundColor Gray
        Write-Host "  dotnet run --configuration $Configuration" -ForegroundColor Gray
    } else {
        throw "Failed to build API"
    }

} catch {
    Write-Host "Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Build process completed!" -ForegroundColor Green 
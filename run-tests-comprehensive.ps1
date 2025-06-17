# Neo Service Layer - Comprehensive Test Runner
# This script runs all tests with coverage reporting and detailed analysis

param(
    [string]$Configuration = "Debug",
    [string]$Framework = "net9.0",
    [switch]$Coverage,
    [switch]$Verbose,
    [switch]$Integration,
    [switch]$Unit,
    [string]$Filter = "",
    [string]$Output = "TestResults"
)

Write-Host "🧪 Neo Service Layer - Comprehensive Test Runner" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# Create output directory
if (!(Test-Path $Output)) {
    New-Item -ItemType Directory -Path $Output -Force | Out-Null
}

# Test categories
$testProjects = @()

if ($Unit -or (!$Integration -and !$Unit)) {
    $testProjects += @(
        "tests/Core/NeoServiceLayer.ServiceFramework.Tests",
        "tests/Services/NeoServiceLayer.Services.KeyManagement.Tests",
        "tests/Services/NeoServiceLayer.Services.Compliance.Tests",
        "tests/Services/NeoServiceLayer.Services.Compute.Tests",
        "tests/Services/NeoServiceLayer.Services.EventSubscription.Tests",
        "tests/Services/NeoServiceLayer.Services.KeyManagement.Tests",
        "tests/Services/NeoServiceLayer.Services.Oracle.Tests",
        "tests/Services/NeoServiceLayer.Services.Randomness.Tests",
        "tests/Services/NeoServiceLayer.Services.Storage.Tests",
        "tests/Services/NeoServiceLayer.Services.ZeroKnowledge.Tests",
        "tests/AI/NeoServiceLayer.AI.PatternRecognition.Tests",
        "tests/AI/NeoServiceLayer.AI.Prediction.Tests",
        "tests/Blockchain/NeoServiceLayer.Neo.N3.Tests",
        "tests/Tee/NeoServiceLayer.Tee.Enclave.Tests",
        "tests/Tee/NeoServiceLayer.Tee.Host.Tests"
    )
}

if ($Integration -or (!$Integration -and !$Unit)) {
    $testProjects += @(
        "tests/Api/NeoServiceLayer.Api.Tests",
        "tests/Integration/NeoServiceLayer.Integration.Tests"
    )
}

# Filter existing projects
$existingProjects = $testProjects | Where-Object { Test-Path "$_.csproj" }

Write-Host "📋 Test Projects Found: $($existingProjects.Count)" -ForegroundColor Green
foreach ($project in $existingProjects) {
    Write-Host "  ✓ $project" -ForegroundColor Gray
}

if ($existingProjects.Count -eq 0) {
    Write-Host "❌ No test projects found!" -ForegroundColor Red
    exit 1
}

# Build test projects first
Write-Host "`n🔨 Building test projects..." -ForegroundColor Yellow
foreach ($project in $existingProjects) {
    Write-Host "Building $project..." -ForegroundColor Gray
    $buildResult = dotnet build $project --configuration $Configuration --framework $Framework --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed for $project" -ForegroundColor Red
        exit 1
    }
}

Write-Host "✅ All test projects built successfully" -ForegroundColor Green

# Prepare test arguments
$testArgs = @(
    "test"
    "--configuration", $Configuration
    "--framework", $Framework
    "--logger", "trx;LogFileName=TestResults.trx"
    "--logger", "console;verbosity=normal"
    "--results-directory", $Output
    "--no-build"
)

if ($Coverage) {
    $testArgs += @(
        "--collect", "XPlat Code Coverage"
        "--settings", "coverlet.runsettings"
    )
}

if ($Verbose) {
    $testArgs += @("--verbosity", "detailed")
}

if ($Filter) {
    $testArgs += @("--filter", $Filter)
}

# Run tests for each project
$totalTests = 0
$passedTests = 0
$failedTests = 0
$skippedTests = 0

Write-Host "`n🧪 Running tests..." -ForegroundColor Yellow

foreach ($project in $existingProjects) {
    Write-Host "`nTesting $project..." -ForegroundColor Cyan
    
    $projectArgs = $testArgs + @($project)
    
    $testOutput = & dotnet @projectArgs 2>&1
    $exitCode = $LASTEXITCODE
    
    if ($Verbose) {
        Write-Host $testOutput -ForegroundColor Gray
    }
    
    # Parse test results
    $testSummary = $testOutput | Select-String "Test Run Successful|Test Run Failed|Total tests:|Passed:|Failed:|Skipped:"
    
    if ($testSummary) {
        foreach ($line in $testSummary) {
            if ($line -match "Total tests: (\d+)") {
                $totalTests += [int]$matches[1]
            }
            if ($line -match "Passed: (\d+)") {
                $passedTests += [int]$matches[1]
            }
            if ($line -match "Failed: (\d+)") {
                $failedTests += [int]$matches[1]
            }
            if ($line -match "Skipped: (\d+)") {
                $skippedTests += [int]$matches[1]
            }
        }
    }
    
    if ($exitCode -eq 0) {
        Write-Host "  ✅ Tests passed" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Tests failed" -ForegroundColor Red
        if (!$Verbose) {
            Write-Host $testOutput -ForegroundColor Red
        }
    }
}

# Generate summary report
Write-Host "`n📊 Test Summary" -ForegroundColor Cyan
Write-Host "===============" -ForegroundColor Cyan
Write-Host "Total Tests:  $totalTests" -ForegroundColor White
Write-Host "Passed:       $passedTests" -ForegroundColor Green
Write-Host "Failed:       $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
Write-Host "Skipped:      $skippedTests" -ForegroundColor Yellow

$successRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 }
Write-Host "Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 90) { "Green" } elseif ($successRate -ge 70) { "Yellow" } else { "Red" })

# Coverage report
if ($Coverage) {
    Write-Host "`n📈 Generating coverage report..." -ForegroundColor Yellow
    
    $coverageFiles = Get-ChildItem -Path $Output -Recurse -Filter "coverage.cobertura.xml"
    
    if ($coverageFiles.Count -gt 0) {
        Write-Host "Coverage files found: $($coverageFiles.Count)" -ForegroundColor Green
        
        # Install reportgenerator if not available
        $reportGenerator = Get-Command "reportgenerator" -ErrorAction SilentlyContinue
        if (!$reportGenerator) {
            Write-Host "Installing ReportGenerator..." -ForegroundColor Yellow
            dotnet tool install -g dotnet-reportgenerator-globaltool
        }
        
        # Generate HTML coverage report
        $coverageArgs = @(
            "-reports:$($coverageFiles -join ';')"
            "-targetdir:$Output/CoverageReport"
            "-reporttypes:Html;Badges"
        )
        
        & reportgenerator @coverageArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Coverage report generated: $Output/CoverageReport/index.html" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to generate coverage report" -ForegroundColor Red
        }
    } else {
        Write-Host "⚠️  No coverage files found" -ForegroundColor Yellow
    }
}

# Test results location
Write-Host "`n📁 Test Results Location: $Output" -ForegroundColor Cyan

# Final status
if ($failedTests -eq 0) {
    Write-Host "`n🎉 All tests completed successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n💥 Some tests failed. Check the results above." -ForegroundColor Red
    exit 1
} 
# Neo Service Layer - Comprehensive Test Runner
# This script runs all unit tests with coverage analysis and generates detailed reports

param(
    [string]$Configuration = "Debug",
    [string]$Filter = "",
    [switch]$Coverage = $false,
    [switch]$Parallel = $true,
    [switch]$Verbose = $false
)

Write-Host "🧪 Neo Service Layer - Comprehensive Test Suite" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan

# Test configuration
$testProjects = @(
    "tests/Blockchain/NeoServiceLayer.Neo.N3.Tests",
    "tests/Blockchain/NeoServiceLayer.Neo.X.Tests", 
    "tests/AI/NeoServiceLayer.AI.Prediction.Tests",
    "tests/AI/NeoServiceLayer.AI.PatternRecognition.Tests",
    "tests/Services/NeoServiceLayer.Services.ZeroKnowledge.Tests",
    "tests/Services/NeoServiceLayer.Services.Oracle.Tests",
    "tests/Services/NeoServiceLayer.Services.Randomness.Tests",
    "tests/Services/NeoServiceLayer.Services.KeyManagement.Tests",
    "tests/Services/NeoServiceLayer.Services.Compute.Tests",
    "tests/Services/NeoServiceLayer.Services.Storage.Tests",
    "tests/Services/NeoServiceLayer.Services.Compliance.Tests",
    "tests/Services/NeoServiceLayer.Services.EventSubscription.Tests",
    "tests/Core/NeoServiceLayer.ServiceFramework.Tests",
    "tests/Tee/NeoServiceLayer.Tee.Host.Tests",
    "tests/Tee/NeoServiceLayer.Tee.Enclave.Tests"
)

$totalTests = 0
$passedTests = 0
$failedTests = 0
$skippedTests = 0
$testResults = @()

# Build solution first
Write-Host "🔨 Building solution..." -ForegroundColor Yellow
dotnet build NeoServiceLayer.sln --configuration $Configuration --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Exiting." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Build successful" -ForegroundColor Green

# Create test results directory
$resultsDir = "TestResults"
if (Test-Path $resultsDir) {
    Remove-Item $resultsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $resultsDir | Out-Null

# Function to run tests for a project
function Run-ProjectTests {
    param(
        [string]$ProjectPath,
        [string]$ProjectName
    )
    
    Write-Host "🧪 Testing: $ProjectName" -ForegroundColor Cyan
    
    if (-not (Test-Path "$ProjectPath/$ProjectName.csproj")) {
        Write-Host "⚠️  Project file not found: $ProjectPath/$ProjectName.csproj" -ForegroundColor Yellow
        return @{ Passed = 0; Failed = 0; Skipped = 0; Total = 0; Status = "Skipped" }
    }
    
    $testArgs = @(
        "test",
        "$ProjectPath/$ProjectName.csproj",
        "--configuration", $Configuration,
        "--logger", "trx;LogFileName=$ProjectName.trx",
        "--results-directory", $resultsDir,
        "--verbosity", $(if ($Verbose) { "detailed" } else { "minimal" })
    )
    
    if ($Filter) {
        $testArgs += "--filter", $Filter
    }
    
    if ($Coverage) {
        $testArgs += @(
            "--collect", "XPlat Code Coverage",
            "--settings", "coverlet.runsettings"
        )
    }
    
    if (-not $Parallel) {
        $testArgs += "--parallel", "false"
    }
    
    $output = & dotnet @testArgs 2>&1
    $exitCode = $LASTEXITCODE
    
    # Parse test results
    $passed = 0
    $failed = 0
    $skipped = 0
    $total = 0
    
    foreach ($line in $output) {
        if ($line -match "Passed:\s*(\d+)") {
            $passed = [int]$matches[1]
        }
        if ($line -match "Failed:\s*(\d+)") {
            $failed = [int]$matches[1]
        }
        if ($line -match "Skipped:\s*(\d+)") {
            $skipped = [int]$matches[1]
        }
        if ($line -match "Total:\s*(\d+)") {
            $total = [int]$matches[1]
        }
    }
    
    $status = if ($exitCode -eq 0) { "Passed" } else { "Failed" }
    $statusColor = if ($exitCode -eq 0) { "Green" } else { "Red" }
    
    Write-Host "   Results: $passed passed, $failed failed, $skipped skipped" -ForegroundColor $statusColor
    
    return @{
        Passed = $passed
        Failed = $failed
        Skipped = $skipped
        Total = $total
        Status = $status
        Output = $output
    }
}

# Run tests for each project
Write-Host "`n🚀 Running test suite..." -ForegroundColor Yellow

foreach ($projectPath in $testProjects) {
    $projectName = Split-Path $projectPath -Leaf
    $result = Run-ProjectTests -ProjectPath $projectPath -ProjectName $projectName
    
    $testResults += [PSCustomObject]@{
        Project = $projectName
        Passed = $result.Passed
        Failed = $result.Failed
        Skipped = $result.Skipped
        Total = $result.Total
        Status = $result.Status
    }
    
    $totalTests += $result.Total
    $passedTests += $result.Passed
    $failedTests += $result.Failed
    $skippedTests += $result.Skipped
}

# Generate coverage report if requested
if ($Coverage) {
    Write-Host "`n📊 Generating coverage report..." -ForegroundColor Yellow
    
    # Install report generator if not present
    $reportGenerator = Get-Command reportgenerator -ErrorAction SilentlyContinue
    if (-not $reportGenerator) {
        Write-Host "Installing ReportGenerator..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-reportgenerator-globaltool
    }
    
    # Generate HTML coverage report
    $coverageFiles = Get-ChildItem -Path $resultsDir -Filter "coverage.cobertura.xml" -Recurse
    if ($coverageFiles.Count -gt 0) {
        $coverageArgs = @(
            "-reports:$($coverageFiles.FullName -join ';')",
            "-targetdir:$resultsDir/coverage",
            "-reporttypes:Html;Badges;TextSummary"
        )
        
        & reportgenerator @coverageArgs
        Write-Host "✅ Coverage report generated: $resultsDir/coverage/index.html" -ForegroundColor Green
    }
}

# Display summary
Write-Host "`n📋 Test Summary" -ForegroundColor Cyan
Write-Host "===============" -ForegroundColor Cyan

$testResults | Format-Table -AutoSize

$successRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 }

Write-Host "`n📊 Overall Results:" -ForegroundColor Cyan
Write-Host "   Total Tests: $totalTests" -ForegroundColor White
Write-Host "   Passed: $passedTests" -ForegroundColor Green
Write-Host "   Failed: $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
Write-Host "   Skipped: $skippedTests" -ForegroundColor Yellow
Write-Host "   Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 90) { "Green" } elseif ($successRate -ge 70) { "Yellow" } else { "Red" })

# Quality gates
Write-Host "`n🎯 Quality Gates:" -ForegroundColor Cyan
$qualityGates = @(
    @{ Name = "All Tests Pass"; Condition = $failedTests -eq 0; Status = $failedTests -eq 0 },
    @{ Name = "Success Rate >= 95%"; Condition = $successRate -ge 95; Status = $successRate -ge 95 },
    @{ Name = "No Skipped Tests"; Condition = $skippedTests -eq 0; Status = $skippedTests -eq 0 }
)

foreach ($gate in $qualityGates) {
    $status = if ($gate.Status) { "✅ PASS" } else { "❌ FAIL" }
    $color = if ($gate.Status) { "Green" } else { "Red" }
    Write-Host "   $($gate.Name): $status" -ForegroundColor $color
}

# Exit with appropriate code
$allQualityGatesPassed = ($qualityGates | Where-Object { -not $_.Status }).Count -eq 0

if ($allQualityGatesPassed) {
    Write-Host "`n🎉 All quality gates passed! Test suite completed successfully." -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n❌ Some quality gates failed. Please review test results." -ForegroundColor Red
    exit 1
}

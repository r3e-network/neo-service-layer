# Neo Service Layer Integration Test Runner
# This script runs comprehensive integration tests for the Neo Service Layer

param(
    [string]$Configuration = "Debug",
    [string]$TestFilter = "",
    [switch]$Coverage = $false,
    [switch]$Verbose = $false,
    [switch]$Parallel = $true
)

Write-Host "🧪 Neo Service Layer Integration Test Runner" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

# Test project path
$TestProject = Join-Path $RootDir "tests\Integration\NeoServiceLayer.Integration.Tests\NeoServiceLayer.Integration.Tests.csproj"

# Verify test project exists
if (-not (Test-Path $TestProject)) {
    Write-Error "❌ Integration test project not found at: $TestProject"
    exit 1
}

Write-Host "📋 Test Configuration:" -ForegroundColor Yellow
Write-Host "  • Configuration: $Configuration" -ForegroundColor Gray
Write-Host "  • Test Project: $TestProject" -ForegroundColor Gray
Write-Host "  • Coverage: $Coverage" -ForegroundColor Gray
Write-Host "  • Verbose: $Verbose" -ForegroundColor Gray
Write-Host "  • Parallel: $Parallel" -ForegroundColor Gray
if ($TestFilter) {
    Write-Host "  • Filter: $TestFilter" -ForegroundColor Gray
}
Write-Host ""

# Build the test project
Write-Host "🔨 Building integration test project..." -ForegroundColor Yellow
try {
    dotnet build $TestProject --configuration $Configuration --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host "✅ Build completed successfully" -ForegroundColor Green
} catch {
    Write-Error "❌ Build failed: $_"
    exit 1
}

# Prepare test arguments
$TestArgs = @(
    "test"
    $TestProject
    "--configuration"
    $Configuration
    "--no-build"
    "--logger"
    "console;verbosity=normal"
)

if ($TestFilter) {
    $TestArgs += "--filter"
    $TestArgs += $TestFilter
}

if ($Verbose) {
    $TestArgs += "--verbosity"
    $TestArgs += "detailed"
}

if (-not $Parallel) {
    $TestArgs += "--parallel"
    $TestArgs += "false"
}

# Add coverage if requested
if ($Coverage) {
    Write-Host "📊 Enabling code coverage..." -ForegroundColor Yellow
    $TestArgs += "--collect"
    $TestArgs += "XPlat Code Coverage"
    $TestArgs += "--results-directory"
    $TestArgs += Join-Path $RootDir "TestResults"
}

# Run the tests
Write-Host "🚀 Running integration tests..." -ForegroundColor Yellow
Write-Host "Command: dotnet $($TestArgs -join ' ')" -ForegroundColor Gray
Write-Host ""

$StartTime = Get-Date

try {
    & dotnet @TestArgs
    $TestExitCode = $LASTEXITCODE
} catch {
    Write-Error "❌ Test execution failed: $_"
    exit 1
}

$EndTime = Get-Date
$Duration = $EndTime - $StartTime

Write-Host ""
Write-Host "⏱️ Test execution completed in $($Duration.TotalSeconds.ToString('F2')) seconds" -ForegroundColor Cyan

# Process test results
if ($TestExitCode -eq 0) {
    Write-Host "✅ All integration tests passed!" -ForegroundColor Green
    
    # Generate coverage report if requested
    if ($Coverage) {
        Write-Host ""
        Write-Host "📊 Generating coverage report..." -ForegroundColor Yellow
        
        $CoverageFiles = Get-ChildItem -Path (Join-Path $RootDir "TestResults") -Filter "coverage.cobertura.xml" -Recurse
        if ($CoverageFiles.Count -gt 0) {
            $LatestCoverage = $CoverageFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            Write-Host "📄 Coverage file: $($LatestCoverage.FullName)" -ForegroundColor Gray
            
            # Try to generate HTML report if reportgenerator is available
            try {
                $ReportDir = Join-Path $RootDir "TestResults\CoverageReport"
                dotnet tool run reportgenerator -reports:$($LatestCoverage.FullName) -targetdir:$ReportDir -reporttypes:Html 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "📊 HTML coverage report generated: $ReportDir\index.html" -ForegroundColor Green
                }
            } catch {
                Write-Host "⚠️ Could not generate HTML coverage report (reportgenerator not available)" -ForegroundColor Yellow
            }
        } else {
            Write-Host "⚠️ No coverage files found" -ForegroundColor Yellow
        }
    }
    
} else {
    Write-Host "❌ Some integration tests failed (exit code: $TestExitCode)" -ForegroundColor Red
    
    # Show test summary
    Write-Host ""
    Write-Host "📋 Test Summary:" -ForegroundColor Yellow
    Write-Host "  • Check the test output above for detailed failure information" -ForegroundColor Gray
    Write-Host "  • Run with -Verbose for more detailed output" -ForegroundColor Gray
    Write-Host "  • Use -TestFilter to run specific tests" -ForegroundColor Gray
    
    exit $TestExitCode
}

# Show test categories summary
Write-Host ""
Write-Host "📋 Integration Test Categories Completed:" -ForegroundColor Cyan
Write-Host "  ✅ Cross-Service Integration Tests" -ForegroundColor Green
Write-Host "  ✅ Smart Contract Integration Tests" -ForegroundColor Green
Write-Host "  ✅ Performance Integration Tests" -ForegroundColor Green
Write-Host "  ✅ End-to-End Scenario Tests" -ForegroundColor Green

Write-Host ""
Write-Host "🎉 Neo Service Layer integration testing completed successfully!" -ForegroundColor Green
Write-Host ""

# Show next steps
Write-Host "🔗 Next Steps:" -ForegroundColor Cyan
Write-Host "  • Review test results and performance metrics" -ForegroundColor Gray
Write-Host "  • Run smart contract tests: npm test (in contracts/ directory)" -ForegroundColor Gray
Write-Host "  • Deploy to testnet for end-to-end validation" -ForegroundColor Gray
Write-Host "  • Monitor system performance under real load" -ForegroundColor Gray

exit 0

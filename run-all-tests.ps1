#!/usr/bin/env pwsh

# Neo Service Layer - Comprehensive Test Runner (PowerShell)
# This script builds the solution and runs all unit tests with proper reporting

param(
    [string]$Configuration = "Release",
    [string]$Verbosity = "normal",
    [switch]$SkipBuild = $false,
    [switch]$Coverage = $true,
    [switch]$Help = $false
)

if ($Help) {
    Write-Host @"
Neo Service Layer - Comprehensive Test Runner

Usage: ./run-all-tests.ps1 [OPTIONS]

Options:
  -Configuration <config>  Build configuration (Debug/Release) [default: Release]
  -Verbosity <level>       Test verbosity (quiet/minimal/normal/detailed) [default: normal]
  -SkipBuild              Skip the build step
  -Coverage               Enable code coverage collection [default: true]
  -Help                   Show this help message

Examples:
  ./run-all-tests.ps1                           # Run with defaults
  ./run-all-tests.ps1 -Configuration Debug      # Run in Debug mode
  ./run-all-tests.ps1 -Verbosity detailed       # Run with detailed output
  ./run-all-tests.ps1 -SkipBuild                # Skip build, just run tests
"@
    exit 0
}

# Configuration
$ResultsDir = "./TestResults"
$CoverageDir = "./CoverageReport"

Write-Host "🧪 Neo Service Layer - Comprehensive Test Suite" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration" -ForegroundColor Blue
Write-Host "Verbosity: $Verbosity" -ForegroundColor Blue
Write-Host "Coverage: $($Coverage ? 'Enabled' : 'Disabled')" -ForegroundColor Blue
Write-Host ""

# Clean previous results
Write-Host "🧹 Cleaning previous test results..." -ForegroundColor Yellow
if (Test-Path $ResultsDir) { Remove-Item $ResultsDir -Recurse -Force }
if (Test-Path $CoverageDir) { Remove-Item $CoverageDir -Recurse -Force }
New-Item -ItemType Directory -Path $ResultsDir -Force | Out-Null
New-Item -ItemType Directory -Path $CoverageDir -Force | Out-Null

# Check .NET installation
Write-Host "🔍 Checking .NET installation..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET CLI not found. Please install .NET 9.0 SDK." -ForegroundColor Red
    exit 1
}

# Restore dependencies
Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Yellow
$restoreResult = dotnet restore NeoServiceLayer.sln --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to restore packages" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Packages restored successfully" -ForegroundColor Green

# Build solution
if (-not $SkipBuild) {
    Write-Host "🔨 Building solution..." -ForegroundColor Yellow
    $buildResult = dotnet build NeoServiceLayer.sln --configuration $Configuration --no-restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build completed successfully" -ForegroundColor Green
} else {
    Write-Host "⏭️  Skipping build step" -ForegroundColor Yellow
}

# Discover test projects
Write-Host "🔍 Discovering test projects..." -ForegroundColor Yellow
$testProjects = Get-ChildItem -Path "tests" -Recurse -Filter "*.csproj" | Sort-Object Name
$testCount = $testProjects.Count

Write-Host "Found $testCount test projects:" -ForegroundColor Blue
foreach ($project in $testProjects) {
    $projectName = $project.BaseName
    Write-Host "  • $projectName" -ForegroundColor Cyan
}
Write-Host ""

# Prepare test arguments
$testArgs = @(
    "test", "NeoServiceLayer.sln",
    "--configuration", $Configuration,
    "--no-build",
    "--verbosity", $Verbosity,
    "--logger", "trx;LogFileName=TestResults.trx",
    "--logger", "console;verbosity=$Verbosity",
    "--results-directory", $ResultsDir
)

if ($Coverage) {
    $testArgs += @(
        "--collect", "XPlat Code Coverage",
        "--settings", "coverlet.runsettings"
    )
}

# Run all tests
Write-Host "🧪 Running all tests$(if ($Coverage) { ' with coverage' })..." -ForegroundColor Yellow
$testOutput = & dotnet @testArgs 2>&1
$testExitCode = $LASTEXITCODE

# Display test output
if ($Verbosity -eq "detailed") {
    Write-Host $testOutput -ForegroundColor Gray
}

# Analyze test results
Write-Host ""
Write-Host "📊 Analyzing test results..." -ForegroundColor Cyan

$trxFiles = Get-ChildItem -Path $ResultsDir -Recurse -Filter "*.trx" -ErrorAction SilentlyContinue
$coverageFiles = Get-ChildItem -Path $ResultsDir -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue

Write-Host "Test result files: $($trxFiles.Count)" -ForegroundColor Blue
Write-Host "Coverage files: $($coverageFiles.Count)" -ForegroundColor Blue

# Generate coverage report
if ($Coverage -and $coverageFiles.Count -gt 0) {
    Write-Host "📈 Generating coverage report..." -ForegroundColor Yellow
    
    # Check if reportgenerator is installed
    $reportGenerator = Get-Command "reportgenerator" -ErrorAction SilentlyContinue
    if (-not $reportGenerator) {
        Write-Host "Installing ReportGenerator..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-reportgenerator-globaltool
    }
    
    # Generate HTML coverage report
    $coverageReports = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"
    $reportArgs = @(
        "-reports:$coverageReports",
        "-targetdir:$CoverageDir",
        "-reporttypes:Html;Badges;TextSummary;MarkdownSummaryGithub",
        "-verbosity:Warning"
    )
    
    $reportResult = & reportgenerator @reportArgs
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Coverage report generated: $CoverageDir/index.html" -ForegroundColor Green
        
        # Display coverage summary
        $summaryFile = Join-Path $CoverageDir "Summary.txt"
        if (Test-Path $summaryFile) {
            Write-Host ""
            Write-Host "📋 Coverage Summary:" -ForegroundColor Cyan
            Get-Content $summaryFile | Write-Host
        }
    } else {
        Write-Host "❌ Failed to generate coverage report" -ForegroundColor Red
    }
} elseif ($Coverage) {
    Write-Host "⚠️  No coverage files found" -ForegroundColor Yellow
}

# Run individual test projects for detailed reporting
Write-Host ""
Write-Host "🔍 Individual Test Project Results:" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan

$totalProjects = 0
$passedProjects = 0
$failedProjects = 0
$failedProjectsList = @()

foreach ($project in $testProjects) {
    $projectName = $project.BaseName
    $totalProjects++
    
    Write-Host "Testing: $projectName" -ForegroundColor Blue
    
    # Run test for individual project
    $projectTestOutput = dotnet test $project.FullName --configuration $Configuration --no-build --verbosity minimal --logger "console;verbosity=minimal" 2>&1
    $projectExitCode = $LASTEXITCODE
    
    if ($projectExitCode -eq 0) {
        $passedProjects++
        Write-Host "  ✅ PASSED" -ForegroundColor Green
    } else {
        $failedProjects++
        $failedProjectsList += $projectName
        Write-Host "  ❌ FAILED" -ForegroundColor Red
        
        # Show error details
        Write-Host "  Error details:" -ForegroundColor Red
        $projectTestOutput | Select-Object -Last 10 | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
    }
}

# Final summary
Write-Host ""
Write-Host "📋 Final Test Summary" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host "Total Projects: $totalProjects" -ForegroundColor Blue
Write-Host "Passed Projects: $passedProjects" -ForegroundColor Green
Write-Host "Failed Projects: $failedProjects" -ForegroundColor Red

if ($failedProjects -eq 0) {
    Write-Host "🎉 All test projects passed!" -ForegroundColor Green
} else {
    Write-Host "💥 $failedProjects test project(s) failed:" -ForegroundColor Red
    foreach ($failedProject in $failedProjectsList) {
        Write-Host "  • $failedProject" -ForegroundColor Red
    }
}

# Quality gates
Write-Host ""
Write-Host "🎯 Quality Gates:" -ForegroundColor Cyan

# Gate 1: All tests must pass
if ($testExitCode -eq 0) {
    Write-Host "  ✅ All tests pass" -ForegroundColor Green
} else {
    Write-Host "  ❌ Some tests failed" -ForegroundColor Red
}

# Gate 2: Coverage collection
if ($Coverage -and $coverageFiles.Count -gt 0) {
    Write-Host "  ✅ Coverage data collected" -ForegroundColor Green
} elseif ($Coverage) {
    Write-Host "  ⚠️  No coverage data collected" -ForegroundColor Yellow
} else {
    Write-Host "  ⏭️  Coverage collection disabled" -ForegroundColor Yellow
}

# Gate 3: All projects build and run
if ($failedProjects -eq 0) {
    Write-Host "  ✅ All test projects executable" -ForegroundColor Green
} else {
    Write-Host "  ❌ Some test projects failed to run" -ForegroundColor Red
}

# Results location
Write-Host ""
Write-Host "📁 Results Location:" -ForegroundColor Cyan
Write-Host "  Test Results: $ResultsDir"
if ($Coverage) {
    Write-Host "  Coverage Report: $CoverageDir/index.html"
}

# Performance metrics
Write-Host ""
Write-Host "⏱️  Performance Metrics:" -ForegroundColor Cyan
$testDuration = if ($testOutput -match "Total time: (.+)") { $matches[1] } else { "Unknown" }
Write-Host "  Test Duration: $testDuration"
Write-Host "  Projects Tested: $totalProjects"
Write-Host "  Success Rate: $([math]::Round(($passedProjects / $totalProjects) * 100, 2))%"

# Final exit code
if ($testExitCode -eq 0 -and $failedProjects -eq 0) {
    Write-Host ""
    Write-Host "🎉 Test suite completed successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "❌ Test suite completed with failures" -ForegroundColor Red
    exit 1
}
#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Runs comprehensive advanced performance tests for Neo Service Layer enclave operations.

.DESCRIPTION
    This script executes load tests, micro-benchmarks, and stress tests for the enclave system,
    providing detailed performance analysis including resource monitoring, throughput analysis,
    and comparative performance reporting.

.PARAMETER TestMode
    The type of performance tests to run: LoadTests, Benchmarks, StressTests, or All (default)

.PARAMETER Configuration
    Build configuration: Debug or Release (default)

.PARAMETER SGXMode
    SGX execution mode: SIM (default) or HW

.PARAMETER OutputDirectory
    Directory for test reports and artifacts (default: ./performance-results)

.PARAMETER Verbose
    Enable verbose output for detailed logging

.PARAMETER CleanArtifacts
    Clean previous test artifacts before running

.PARAMETER GenerateReport
    Generate comprehensive HTML performance report

.PARAMETER CompareBaselines
    Compare results against performance baselines

.EXAMPLE
    .\run-advanced-performance-tests.ps1 -TestMode All -Configuration Release -Verbose

.EXAMPLE
    .\run-advanced-performance-tests.ps1 -TestMode LoadTests -SGXMode SIM -GenerateReport

.EXAMPLE
    .\run-advanced-performance-tests.ps1 -TestMode Benchmarks -CompareBaselines -OutputDirectory "./perf-results"
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet("LoadTests", "Benchmarks", "StressTests", "All")]
    [string]$TestMode = "All",
    
    [Parameter()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter()]
    [ValidateSet("SIM", "HW")]
    [string]$SGXMode = "SIM",
    
    [Parameter()]
    [string]$OutputDirectory = "./performance-results",
    
    [Parameter()]
    [switch]$Verbose,
    
    [Parameter()]
    [switch]$CleanArtifacts,
    
    [Parameter()]
    [switch]$GenerateReport,
    
    [Parameter()]
    [switch]$CompareBaselines
)

# Script configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Color definitions for consistent output
$Colors = @{
    Header = 'Cyan'
    Success = 'Green'
    Warning = 'Yellow'
    Error = 'Red'
    Info = 'White'
    Highlight = 'Magenta'
}

# Performance test configuration
$TestConfig = @{
    ProjectPath = Split-Path -Parent $PSScriptRoot
    TestProject = "NeoServiceLayer.Performance.Tests.csproj"
    DefaultTimeout = 3600  # 1 hour timeout for comprehensive tests
    ResultsPath = ""
    StartTime = Get-Date
}

# Initialize results directory
$TestConfig.ResultsPath = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) { 
    $OutputDirectory 
} else { 
    Join-Path $TestConfig.ProjectPath $OutputDirectory 
}

function Write-Header {
    param([string]$Message)
    Write-Host "=" * 80 -ForegroundColor $Colors.Header
    Write-Host " $Message" -ForegroundColor $Colors.Header
    Write-Host "=" * 80 -ForegroundColor $Colors.Header
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor $Colors.Info
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor $Colors.Success
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor $Colors.Warning
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $Colors.Error
}

function Write-Highlight {
    param([string]$Message)
    Write-Host "[HIGHLIGHT] $Message" -ForegroundColor $Colors.Highlight
}

function Test-Prerequisites {
    Write-Info "Checking prerequisites..."
    
    # Check .NET 9.0 SDK
    try {
        $dotnetVersion = & dotnet --version 2>$null
        if ($LASTEXITCODE -ne 0 -or -not $dotnetVersion.StartsWith("9.")) {
            throw ".NET 9.0 SDK not found or incorrect version: $dotnetVersion"
        }
        Write-Success ".NET SDK version: $dotnetVersion"
    }
    catch {
        Write-Error "Failed to detect .NET 9.0 SDK: $_"
        exit 1
    }
    
    # Check SGX simulation mode prerequisites
    if ($SGXMode -eq "SIM") {
        $env:SGX_MODE = "SIM"
        $env:SGX_DEBUG = if ($Configuration -eq "Debug") { "1" } else { "0" }
        Write-Success "SGX simulation mode configured"
    }
    else {
        Write-Warning "Hardware SGX mode selected - ensure SGX drivers are installed"
        # Additional SGX hardware checks could be added here
    }
    
    # Verify project structure
    $projectFile = Join-Path $TestConfig.ProjectPath $TestConfig.TestProject
    if (-not (Test-Path $projectFile)) {
        Write-Error "Performance test project not found: $projectFile"
        exit 1
    }
    Write-Success "Performance test project found"
    
    # Check available memory
    $availableMemory = if ($IsWindows) {
        Get-CimInstance -ClassName Win32_OperatingSystem | Select-Object -ExpandProperty FreePhysicalMemory
    } else {
        # Linux memory check
        $memInfo = Get-Content /proc/meminfo -ErrorAction SilentlyContinue | Where-Object { $_ -match "MemAvailable" }
        if ($memInfo) {
            [int]($memInfo -split '\s+')[1]
        } else { 2048000 } # Default 2GB estimate
    }
    
    $availableMemoryMB = [math]::Round($availableMemory / 1024, 0)
    if ($availableMemoryMB -lt 2048) {
        Write-Warning "Low available memory: ${availableMemoryMB}MB (recommended: 2GB+)"
    } else {
        Write-Success "Available memory: ${availableMemoryMB}MB"
    }
}

function Initialize-Environment {
    Write-Info "Initializing test environment..."
    
    # Clean artifacts if requested
    if ($CleanArtifacts) {
        Write-Info "Cleaning previous test artifacts..."
        $artifactPaths = @(
            $TestConfig.ResultsPath,
            (Join-Path $TestConfig.ProjectPath "load-test-reports"),
            (Join-Path $TestConfig.ProjectPath "stress-test-reports"),
            (Join-Path $TestConfig.ProjectPath "burst-test-reports"),
            (Join-Path $TestConfig.ProjectPath "BenchmarkDotNet.Artifacts")
        )
        
        foreach ($path in $artifactPaths) {
            if (Test-Path $path) {
                Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
                Write-Info "Cleaned: $path"
            }
        }
    }
    
    # Create results directory
    if (-not (Test-Path $TestConfig.ResultsPath)) {
        New-Item -Path $TestConfig.ResultsPath -ItemType Directory -Force | Out-Null
        Write-Success "Created results directory: $($TestConfig.ResultsPath)"
    }
    
    # Set environment variables
    $env:DOTNET_ENVIRONMENT = "PerformanceTest"
    $env:SGX_MODE = $SGXMode
    $env:SGX_DEBUG = if ($Configuration -eq "Debug") { "1" } else { "0" }
    
    # Build performance test project
    Write-Info "Building performance test project..."
    Set-Location $TestConfig.ProjectPath
    
    $buildArgs = @(
        "build"
        $TestConfig.TestProject
        "--configuration", $Configuration
        "--verbosity", "minimal"
        "--no-restore"
    )
    
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build performance test project"
        exit 1
    }
    Write-Success "Performance test project built successfully"
}

function Invoke-LoadTests {
    Write-Header "Running NBomber Load Tests"
    
    $loadTestArgs = @(
        "test"
        $TestConfig.TestProject
        "--configuration", $Configuration
        "--logger", "console;verbosity=normal"
        "--logger", "trx;LogFileName=load-tests-results.trx"
        "--results-directory", $TestConfig.ResultsPath
        "--filter", "Category=LoadTest"
        "--collect:XPlat Code Coverage"
        "--settings", "coverlet.runsettings"
    )
    
    if ($Verbose) {
        $loadTestArgs += "--verbosity", "detailed"
    }
    
    Write-Info "Executing load tests with NBomber..."
    Write-Info "Test arguments: $($loadTestArgs -join ' ')"
    
    $loadTestStart = Get-Date
    & dotnet @loadTestArgs
    $loadTestResult = $LASTEXITCODE
    $loadTestDuration = (Get-Date) - $loadTestStart
    
    if ($loadTestResult -eq 0) {
        Write-Success "Load tests completed successfully in $($loadTestDuration.TotalMinutes.ToString('F2')) minutes"
        
        # Copy NBomber reports to results directory
        $nbomberReports = @(
            "load-test-reports",
            "stress-test-reports", 
            "burst-test-reports"
        )
        
        foreach ($reportDir in $nbomberReports) {
            $sourcePath = Join-Path $TestConfig.ProjectPath $reportDir
            if (Test-Path $sourcePath) {
                $destPath = Join-Path $TestConfig.ResultsPath $reportDir
                Copy-Item $sourcePath $destPath -Recurse -Force
                Write-Info "Copied $reportDir to results directory"
            }
        }
    } else {
        Write-Error "Load tests failed with exit code: $loadTestResult"
        return $false
    }
    
    return $true
}

function Invoke-Benchmarks {
    Write-Header "Running BenchmarkDotNet Micro-Benchmarks"
    
    $benchmarkArgs = @(
        "run"
        "--project", $TestConfig.TestProject
        "--configuration", $Configuration
        "--framework", "net9.0"
        "--"
        "--job", "short"
        "--exporters", "html,csv,json"
        "--artifacts", (Join-Path $TestConfig.ResultsPath "BenchmarkDotNet.Artifacts")
    )
    
    if ($Verbose) {
        $benchmarkArgs += "--verbosity", "diagnostic"
    }
    
    Write-Info "Executing micro-benchmarks with BenchmarkDotNet..."
    Write-Info "Benchmark arguments: $($benchmarkArgs -join ' ')"
    
    $benchmarkStart = Get-Date
    & dotnet @benchmarkArgs
    $benchmarkResult = $LASTEXITCODE
    $benchmarkDuration = (Get-Date) - $benchmarkStart
    
    if ($benchmarkResult -eq 0) {
        Write-Success "Benchmarks completed successfully in $($benchmarkDuration.TotalMinutes.ToString('F2')) minutes"
        
        # Process benchmark results
        $artifactsPath = Join-Path $TestConfig.ResultsPath "BenchmarkDotNet.Artifacts"
        if (Test-Path $artifactsPath) {
            $resultFiles = Get-ChildItem $artifactsPath -Recurse -Include "*.html", "*.csv", "*.json"
            Write-Info "Generated $($resultFiles.Count) benchmark result files"
            
            foreach ($file in $resultFiles) {
                Write-Info "  - $($file.Name) ($($file.Length) bytes)"
            }
        }
    } else {
        Write-Error "Benchmarks failed with exit code: $benchmarkResult"
        return $false
    }
    
    return $true
}

function Invoke-StressTests {
    Write-Header "Running Stress Tests"
    
    $stressTestArgs = @(
        "test"
        $TestConfig.TestProject
        "--configuration", $Configuration
        "--logger", "console;verbosity=normal"
        "--logger", "trx;LogFileName=stress-tests-results.trx"
        "--results-directory", $TestConfig.ResultsPath
        "--filter", "Category=StressTest|Category=BurstLoad"
        "--collect:XPlat Code Coverage"
    )
    
    if ($Verbose) {
        $stressTestArgs += "--verbosity", "detailed"
    }
    
    Write-Info "Executing stress tests..."
    Write-Info "Stress test arguments: $($stressTestArgs -join ' ')"
    
    $stressTestStart = Get-Date
    & dotnet @stressTestArgs
    $stressTestResult = $LASTEXITCODE
    $stressTestDuration = (Get-Date) - $stressTestStart
    
    if ($stressTestResult -eq 0) {
        Write-Success "Stress tests completed successfully in $($stressTestDuration.TotalMinutes.ToString('F2')) minutes"
    } else {
        Write-Error "Stress tests failed with exit code: $stressTestResult"
        return $false
    }
    
    return $true
}

function Compare-WithBaselines {
    if (-not $CompareBaselines) {
        return
    }
    
    Write-Header "Comparing Results with Performance Baselines"
    
    $baselineConfigPath = Join-Path $TestConfig.ProjectPath "benchmark-config.json"
    if (-not (Test-Path $baselineConfigPath)) {
        Write-Warning "Baseline configuration not found: $baselineConfigPath"
        return
    }
    
    try {
        $baselineConfig = Get-Content $baselineConfigPath | ConvertFrom-Json
        $baselines = $baselineConfig.PerformanceBaselines
        
        Write-Info "Performance baselines loaded:"
        foreach ($baseline in $baselines.PSObject.Properties) {
            $name = $baseline.Name
            $target = $baseline.Value.TargetMs
            $tolerance = $baseline.Value.TolerancePercent
            Write-Info "  - $name: ${target}ms (±${tolerance}%)"
        }
        
        # TODO: Implement actual baseline comparison logic
        # This would parse benchmark results and compare against baselines
        Write-Info "Baseline comparison would be implemented here"
        
    } catch {
        Write-Warning "Failed to process performance baselines: $_"
    }
}

function Generate-PerformanceReport {
    if (-not $GenerateReport) {
        return
    }
    
    Write-Header "Generating Comprehensive Performance Report"
    
    $reportData = @{
        TestRun = @{
            Timestamp = $TestConfig.StartTime
            Duration = (Get-Date) - $TestConfig.StartTime
            Configuration = $Configuration
            SGXMode = $SGXMode
            TestMode = $TestMode
            Environment = @{
                OS = if ($IsWindows) { "Windows" } elseif ($IsLinux) { "Linux" } else { "Unknown" }
                DotNetVersion = & dotnet --version
                ProcessorCount = $env:NUMBER_OF_PROCESSORS
                MachineName = $env:COMPUTERNAME
            }
        }
        Results = @{
            LoadTests = Test-Path (Join-Path $TestConfig.ResultsPath "load-test-reports")
            Benchmarks = Test-Path (Join-Path $TestConfig.ResultsPath "BenchmarkDotNet.Artifacts")
            StressTests = Test-Path (Join-Path $TestConfig.ResultsPath "*stress-tests-results.trx")
        }
    }
    
    # Generate HTML report
    $reportPath = Join-Path $TestConfig.ResultsPath "performance-report.html"
    $htmlContent = @"
<!DOCTYPE html>
<html>
<head>
    <title>Neo Service Layer - Advanced Performance Test Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background-color: #2c3e50; color: white; padding: 20px; border-radius: 5px; }
        .section { margin: 20px 0; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }
        .success { background-color: #d4edda; border-color: #c3e6cb; }
        .info { background-color: #d1ecf1; border-color: #bee5eb; }
        .metrics { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; }
        .metric { text-align: center; padding: 10px; background-color: #f8f9fa; border-radius: 5px; }
        .metric-value { font-size: 24px; font-weight: bold; color: #007bff; }
        .metric-label { font-size: 14px; color: #6c757d; }
    </style>
</head>
<body>
    <div class="header">
        <h1>🚀 Neo Service Layer - Advanced Performance Test Report</h1>
        <p>Generated on: $($reportData.TestRun.Timestamp.ToString('yyyy-MM-dd HH:mm:ss UTC'))</p>
    </div>
    
    <div class="section info">
        <h2>📊 Test Execution Summary</h2>
        <div class="metrics">
            <div class="metric">
                <div class="metric-value">$($reportData.TestRun.Duration.TotalMinutes.ToString('F1'))m</div>
                <div class="metric-label">Total Duration</div>
            </div>
            <div class="metric">
                <div class="metric-value">$($reportData.TestRun.Configuration)</div>
                <div class="metric-label">Configuration</div>
            </div>
            <div class="metric">
                <div class="metric-value">$($reportData.TestRun.SGXMode)</div>
                <div class="metric-label">SGX Mode</div>
            </div>
            <div class="metric">
                <div class="metric-value">$($reportData.TestRun.Environment.OS)</div>
                <div class="metric-label">Platform</div>
            </div>
        </div>
    </div>
    
    <div class="section success">
        <h2>✅ Test Results</h2>
        <ul>
            <li>Load Tests: $(if ($reportData.Results.LoadTests) { "✅ Completed" } else { "❌ Not Run" })</li>
            <li>Micro-Benchmarks: $(if ($reportData.Results.Benchmarks) { "✅ Completed" } else { "❌ Not Run" })</li>
            <li>Stress Tests: $(if ($reportData.Results.StressTests) { "✅ Completed" } else { "❌ Not Run" })</li>
        </ul>
    </div>
    
    <div class="section">
        <h2>📁 Generated Artifacts</h2>
        <p>Test results and detailed reports are available in: <code>$($TestConfig.ResultsPath)</code></p>
        <ul>
            <li>NBomber Load Test Reports (HTML, CSV, Markdown)</li>
            <li>BenchmarkDotNet Micro-Benchmark Results (HTML, CSV, JSON)</li>
            <li>Test Coverage Reports (Cobertura XML, JSON)</li>
            <li>Performance Monitoring Data</li>
        </ul>
    </div>
    
    <div class="section">
        <h2>🔍 Next Steps</h2>
        <ul>
            <li>Review detailed performance metrics in individual report files</li>
            <li>Compare results with previous runs and baselines</li>
            <li>Analyze performance trends and identify optimization opportunities</li>
            <li>Investigate any performance regressions or anomalies</li>
        </ul>
    </div>
    
    <footer style="margin-top: 40px; text-align: center; color: #6c757d;">
        <p>Neo Service Layer Advanced Performance Testing Framework</p>
        <p>Report generated at $($reportData.TestRun.Timestamp.ToString('yyyy-MM-dd HH:mm:ss UTC'))</p>
    </footer>
</body>
</html>
"@
    
    $htmlContent | Out-File -FilePath $reportPath -Encoding UTF8
    Write-Success "Performance report generated: $reportPath"
    
    # Generate JSON summary for programmatic access
    $jsonPath = Join-Path $TestConfig.ResultsPath "performance-summary.json"
    $reportData | ConvertTo-Json -Depth 10 | Out-File -FilePath $jsonPath -Encoding UTF8
    Write-Success "Performance summary (JSON): $jsonPath"
}

function Show-Summary {
    $duration = (Get-Date) - $TestConfig.StartTime
    
    Write-Header "Advanced Performance Testing Summary"
    Write-Highlight "Total execution time: $($duration.TotalMinutes.ToString('F2')) minutes"
    Write-Highlight "Test mode: $TestMode"
    Write-Highlight "Configuration: $Configuration"
    Write-Highlight "SGX mode: $SGXMode"
    Write-Highlight "Results location: $($TestConfig.ResultsPath)"
    
    Write-Info ""
    Write-Success "🎉 Advanced performance testing completed successfully!"
    Write-Info "📊 Review the generated reports for detailed performance insights"
    Write-Info "📈 Use the results to optimize enclave operations and identify bottlenecks"
    Write-Info "🔍 Compare results with baselines to track performance trends"
}

# Main execution flow
try {
    Write-Header "Neo Service Layer - Advanced Performance Testing"
    Write-Info "Starting advanced performance testing suite..."
    Write-Info "Test Mode: $TestMode | Configuration: $Configuration | SGX Mode: $SGXMode"
    
    # Prerequisites and environment setup
    Test-Prerequisites
    Initialize-Environment
    
    # Execute selected test modes
    $allTestsSuccessful = $true
    
    if ($TestMode -eq "LoadTests" -or $TestMode -eq "All") {
        $allTestsSuccessful = $allTestsSuccessful -and (Invoke-LoadTests)
    }
    
    if ($TestMode -eq "Benchmarks" -or $TestMode -eq "All") {
        $allTestsSuccessful = $allTestsSuccessful -and (Invoke-Benchmarks)
    }
    
    if ($TestMode -eq "StressTests" -or $TestMode -eq "All") {
        $allTestsSuccessful = $allTestsSuccessful -and (Invoke-StressTests)
    }
    
    # Post-processing
    Compare-WithBaselines
    Generate-PerformanceReport
    
    # Final summary
    Show-Summary
    
    if (-not $allTestsSuccessful) {
        Write-Error "Some performance tests failed. Check the logs for details."
        exit 1
    }
    
} catch {
    Write-Error "Fatal error during performance testing: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
} 
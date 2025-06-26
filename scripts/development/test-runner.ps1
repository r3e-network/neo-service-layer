#!/usr/bin/env pwsh

# Neo Service Layer Test Runner
# Validates core functionality and reports test coverage

Write-Host "🚀 Neo Service Layer Test Runner" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

$ErrorActionPreference = "Continue"
$testResults = @()

function Test-Project {
    param(
        [string]$ProjectPath,
        [string]$ProjectName
    )
    
    Write-Host "`n📋 Testing: $ProjectName" -ForegroundColor Yellow
    Write-Host "Path: $ProjectPath" -ForegroundColor Gray
    
    if (-not (Test-Path $ProjectPath)) {
        Write-Host "❌ Project not found: $ProjectPath" -ForegroundColor Red
        return @{
            Project = $ProjectName
            Status = "Not Found"
            Tests = 0
            Passed = 0
            Failed = 0
            Duration = "0s"
        }
    }
    
    try {
        $output = dotnet test $ProjectPath --verbosity minimal --logger "console;verbosity=minimal" 2>&1
        $exitCode = $LASTEXITCODE
        
        # Parse test results
        $testLine = $output | Where-Object { $_ -match "Test summary:" } | Select-Object -Last 1
        $durationLine = $output | Where-Object { $_ -match "duration:" } | Select-Object -Last 1
        
        if ($testLine -match "total: (\d+), failed: (\d+), succeeded: (\d+)") {
            $total = [int]$matches[1]
            $failed = [int]$matches[2]
            $passed = [int]$matches[3]
        } else {
            $total = 0
            $failed = 0
            $passed = 0
        }
        
        if ($durationLine -match "duration: (.+s)") {
            $duration = $matches[1]
        } else {
            $duration = "Unknown"
        }
        
        $status = if ($exitCode -eq 0) { "✅ Passed" } else { "❌ Failed" }
        
        Write-Host "Status: $status" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })
        Write-Host "Tests: $total | Passed: $passed | Failed: $failed | Duration: $duration" -ForegroundColor White
        
        return @{
            Project = $ProjectName
            Status = if ($exitCode -eq 0) { "Passed" } else { "Failed" }
            Tests = $total
            Passed = $passed
            Failed = $failed
            Duration = $duration
            ExitCode = $exitCode
        }
    }
    catch {
        Write-Host "❌ Error testing project: $($_.Exception.Message)" -ForegroundColor Red
        return @{
            Project = $ProjectName
            Status = "Error"
            Tests = 0
            Passed = 0
            Failed = 0
            Duration = "0s"
            Error = $_.Exception.Message
        }
    }
}

# Test projects in order of complexity
$testProjects = @(
    @{ Path = "tests/Core/NeoServiceLayer.Shared.Tests/"; Name = "Shared Utilities" },
    @{ Path = "tests/Tee/NeoServiceLayer.Tee.Host.Tests/"; Name = "TEE Host" },
    @{ Path = "tests/Advanced/NeoServiceLayer.Advanced.FairOrdering.Tests/"; Name = "Fair Ordering" }
)

Write-Host "`n🧪 Running Test Suite..." -ForegroundColor Cyan

foreach ($project in $testProjects) {
    $result = Test-Project -ProjectPath $project.Path -ProjectName $project.Name
    $testResults += $result
}

# Summary Report
Write-Host "`n📊 Test Summary Report" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor Cyan

$totalTests = ($testResults | Measure-Object -Property Tests -Sum).Sum
$totalPassed = ($testResults | Measure-Object -Property Passed -Sum).Sum
$totalFailed = ($testResults | Measure-Object -Property Failed -Sum).Sum
$passedProjects = ($testResults | Where-Object { $_.Status -eq "Passed" }).Count
$totalProjects = $testResults.Count

Write-Host "`nOverall Statistics:" -ForegroundColor White
Write-Host "Projects: $passedProjects/$totalProjects passed" -ForegroundColor $(if ($passedProjects -eq $totalProjects) { "Green" } else { "Yellow" })
Write-Host "Tests: $totalTests total | $totalPassed passed | $totalFailed failed" -ForegroundColor White

if ($totalTests -gt 0) {
    $successRate = [math]::Round(($totalPassed / $totalTests) * 100, 2)
    Write-Host "Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 80) { "Green" } elseif ($successRate -ge 60) { "Yellow" } else { "Red" })
}

Write-Host "`nProject Details:" -ForegroundColor White
foreach ($result in $testResults) {
    $statusColor = switch ($result.Status) {
        "Passed" { "Green" }
        "Failed" { "Red" }
        default { "Yellow" }
    }
    
    Write-Host "  $($result.Project): $($result.Status)" -ForegroundColor $statusColor
    if ($result.Tests -gt 0) {
        Write-Host "    Tests: $($result.Tests) | Passed: $($result.Passed) | Failed: $($result.Failed) | Duration: $($result.Duration)" -ForegroundColor Gray
    }
    if ($result.Error) {
        Write-Host "    Error: $($result.Error)" -ForegroundColor Red
    }
}

# Recommendations
Write-Host "`n💡 Recommendations:" -ForegroundColor Cyan
if ($totalFailed -gt 0) {
    Write-Host "  • Fix failing tests to improve reliability" -ForegroundColor Yellow
}
if ($passedProjects -lt $totalProjects) {
    Write-Host "  • Address compilation issues in failing projects" -ForegroundColor Yellow
}
if ($totalTests -eq 0) {
    Write-Host "  • Ensure test projects are properly configured" -ForegroundColor Yellow
}

Write-Host "`n🎯 Test Coverage Goals:" -ForegroundColor Cyan
Write-Host "  • Shared Utilities: 95% (Core functionality)" -ForegroundColor White
Write-Host "  • Core Services: 90% (Business logic)" -ForegroundColor White
Write-Host "  • AI Services: 85% (ML algorithms)" -ForegroundColor White
Write-Host "  • Advanced Services: 80% (Complex features)" -ForegroundColor White

$overallSuccess = $totalFailed -eq 0 -and $passedProjects -eq $totalProjects
Write-Host "`n$(if ($overallSuccess) { '🎉 All tests passed!' } else { '⚠️  Some tests need attention' })" -ForegroundColor $(if ($overallSuccess) { "Green" } else { "Yellow" })

exit $(if ($overallSuccess) { 0 } else { 1 }) 
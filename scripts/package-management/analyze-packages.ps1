#!/usr/bin/env pwsh

param(
    [string]$OutputPath = "PackageReferences.md"
)

# Initialize collections
$packageReferences = @{}
$projectFiles = Get-ChildItem -Path . -Filter "*.csproj" -Recurse

Write-Host "Found $($projectFiles.Count) .csproj files"

# Extract package references from each project
foreach ($projectFile in $projectFiles) {
    Write-Host "Processing: $($projectFile.FullName)"
    
    $content = [xml](Get-Content $projectFile.FullName)
    $projectName = $projectFile.Name
    
    # Find all PackageReference elements
    $packages = $content.Project.ItemGroup.PackageReference
    
    if ($packages) {
        foreach ($package in $packages) {
            $packageName = $package.Include
            $version = $package.Version
            
            if ($packageName -and $version) {
                if (-not $packageReferences.ContainsKey($packageName)) {
                    $packageReferences[$packageName] = @{
                        Versions = @{}
                        Projects = @()
                        Category = ""
                    }
                }
                
                # Track version usage
                if (-not $packageReferences[$packageName].Versions.ContainsKey($version)) {
                    $packageReferences[$packageName].Versions[$version] = @()
                }
                
                $packageReferences[$packageName].Versions[$version] += $projectName
                $packageReferences[$packageName].Projects += $projectName
            }
        }
    }
}

# Categorize packages
foreach ($packageName in $packageReferences.Keys) {
    $category = switch -Wildcard ($packageName) {
        "Microsoft.Extensions.*" { "Microsoft Extensions" }
        "Microsoft.AspNetCore.*" { "ASP.NET Core" }
        "Microsoft.EntityFrameworkCore.*" { "Entity Framework Core" }
        "Microsoft.ML.*" { "Machine Learning" }
        "Microsoft.NET.Test.*" { "Testing" }
        "xunit*" { "Testing" }
        "Moq*" { "Testing" }
        "FluentAssertions" { "Testing" }
        "AutoFixture" { "Testing" }
        "coverlet.*" { "Testing" }
        "Serilog*" { "Logging" }
        "Swashbuckle.*" { "API Documentation" }
        "Neo*" { "NEO Blockchain" }
        "Nethereum.*" { "Ethereum Integration" }
        "StackExchange.*" { "Caching/Redis" }
        "Npgsql*" { "Database" }
        "System.*" { "System Libraries" }
        "Asp.Versioning.*" { "API Versioning" }
        "AspNetCore.HealthChecks.*" { "Health Checks" }
        "Newtonsoft.Json" { "JSON Processing" }
        "Bogus" { "Testing" }
        default { "Other" }
    }
    
    $packageReferences[$packageName].Category = $category
}

# Generate output
$output = @()
$output += "# Package References Analysis"
$output += ""
$output += "Generated on: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$output += ""
$output += "## Summary"
$output += ""
$output += "- Total Projects: $($projectFiles.Count)"
$output += "- Total Unique Packages: $($packageReferences.Count)"
$output += ""

# Group by category
$categories = $packageReferences.Values.Category | Select-Object -Unique | Sort-Object

foreach ($category in $categories) {
    $categoryPackages = $packageReferences.GetEnumerator() | 
        Where-Object { $_.Value.Category -eq $category } | 
        Sort-Object Key
    
    if ($categoryPackages.Count -gt 0) {
        $output += "## $category"
        $output += ""
        $output += "| Package | Versions | Project Count | Version Details |"
        $output += "|---------|----------|---------------|-----------------|"
        
        foreach ($package in $categoryPackages) {
            $packageName = $package.Key
            $packageInfo = $package.Value
            $versionCount = $packageInfo.Versions.Count
            $projectCount = ($packageInfo.Projects | Select-Object -Unique).Count
            
            # Format version details
            $versionDetails = @()
            foreach ($version in $packageInfo.Versions.GetEnumerator() | Sort-Object Key -Descending) {
                $projects = $version.Value | Select-Object -Unique | Sort-Object
                $versionDetails += "**$($version.Key)** ($($projects.Count) projects)"
            }
            
            $versionDetailsStr = $versionDetails -join "<br/>"
            
            $output += "| $packageName | $versionCount | $projectCount | $versionDetailsStr |"
        }
        
        $output += ""
    }
}

# Add version inconsistencies section
$output += "## Version Inconsistencies"
$output += ""
$output += "Packages with multiple versions across projects:"
$output += ""

$inconsistencies = $packageReferences.GetEnumerator() | 
    Where-Object { $_.Value.Versions.Count -gt 1 } | 
    Sort-Object Key

if ($inconsistencies.Count -gt 0) {
    $output += "| Package | Versions | Projects |"
    $output += "|---------|----------|----------|"
    
    foreach ($inconsistency in $inconsistencies) {
        $packageName = $inconsistency.Key
        $versions = @()
        
        foreach ($version in $inconsistency.Value.Versions.GetEnumerator() | Sort-Object Key -Descending) {
            $projects = $version.Value | Select-Object -Unique | Sort-Object
            $versions += "**$($version.Key)**: $($projects -join ', ')"
        }
        
        $versionsStr = $versions -join "<br/>"
        $allVersions = ($inconsistency.Value.Versions.Keys | Sort-Object -Descending) -join ", "
        
        $output += "| $packageName | $allVersions | $versionsStr |"
    }
} else {
    $output += "No version inconsistencies found!"
}

$output += ""

# Add recommendations section
$output += "## Recommendations"
$output += ""
$output += "### Version Alignment"
$output += ""

# Find latest versions for common packages
$commonPackages = @{
    "Microsoft.Extensions.*" = "9.0.5"
    "Microsoft.AspNetCore.*" = "9.0.0"
    "Microsoft.ML.*" = "5.0.0-preview.1.25127.4"
    "System.Text.Json" = "9.0.6"
}

$output += "Recommended versions for consistency:"
$output += ""
$output += "| Package Pattern | Recommended Version |"
$output += "|-----------------|---------------------|"

foreach ($pattern in $commonPackages.GetEnumerator() | Sort-Object Key) {
    $output += "| $($pattern.Key) | $($pattern.Value) |"
}

# Write output
$output | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host ""
Write-Host "Analysis complete! Output written to: $OutputPath" -ForegroundColor Green
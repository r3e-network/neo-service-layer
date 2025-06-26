#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Migrates all .csproj files to use centralized package management via Directory.Packages.props

.DESCRIPTION
    This script converts PackageReference elements in all .csproj files to remove version attributes,
    enabling centralized package version management through Directory.Packages.props.

.PARAMETER DryRun
    If specified, shows what changes would be made without actually modifying files.

.EXAMPLE
    ./migrate-to-central-packages.ps1 -DryRun
    ./migrate-to-central-packages.ps1
#>

param(
    [switch]$DryRun
)

# Find all .csproj files
$projectFiles = Get-ChildItem -Path . -Recurse -Filter "*.csproj" | Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }

Write-Host "Found $($projectFiles.Count) project files to process..." -ForegroundColor Green

$totalChanges = 0

foreach ($projectFile in $projectFiles) {
    Write-Host "`nProcessing: $($projectFile.FullName)" -ForegroundColor Yellow
    
    # Read the project file content
    $content = Get-Content $projectFile.FullName -Raw
    $originalContent = $content
    
    # Use regex to find and remove Version attributes from PackageReference elements
    $pattern = '(<PackageReference\s+Include="[^"]+")(\s+Version="[^"]+")(\s*/?)'
    $replacement = '$1$3'
    
    $content = $content -replace $pattern, $replacement
    
    # Count changes made
    $changesMade = ($originalContent -split "`n").Count - ($content -split "`n").Count
    if ($originalContent -ne $content) {
        $changesMade = ([regex]::Matches($originalContent, $pattern)).Count
        $totalChanges += $changesMade
        
        Write-Host "  - Found $changesMade PackageReference elements with versions to update" -ForegroundColor Cyan
        
        if (-not $DryRun) {
            # Create backup
            $backupPath = "$($projectFile.FullName).backup"
            Copy-Item $projectFile.FullName $backupPath
            
            # Write updated content
            Set-Content -Path $projectFile.FullName -Value $content -NoNewline
            Write-Host "  - Updated and created backup at: $backupPath" -ForegroundColor Green
        } else {
            Write-Host "  - [DRY RUN] Would remove version attributes from $changesMade PackageReference elements" -ForegroundColor Magenta
        }
    } else {
        Write-Host "  - No changes needed" -ForegroundColor Gray
    }
}

Write-Host "`n" -NoNewline
Write-Host "Migration Summary:" -ForegroundColor Green
Write-Host "=================" -ForegroundColor Green
Write-Host "Total project files processed: $($projectFiles.Count)"
Write-Host "Total PackageReference elements updated: $totalChanges"

if ($DryRun) {
    Write-Host "`n[DRY RUN MODE] No files were actually modified." -ForegroundColor Magenta
    Write-Host "Run without -DryRun parameter to apply changes." -ForegroundColor Magenta
} else {
    Write-Host "`nMigration completed! All projects now use centralized package management." -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Run 'dotnet restore' to verify the migration" -ForegroundColor White
    Write-Host "2. Run 'dotnet build' to ensure everything compiles" -ForegroundColor White
    Write-Host "3. Commit the changes if everything works correctly" -ForegroundColor White
    Write-Host "4. Remove .backup files once you're satisfied with the results" -ForegroundColor White
}
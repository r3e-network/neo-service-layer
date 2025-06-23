#!/bin/bash

# Script to migrate all .csproj files to use centralized package management

echo "Starting migration to centralized package management..."

# Find all .csproj files, excluding bin and obj directories
csproj_files=$(find . -name "*.csproj" -type f | grep -v "/bin/" | grep -v "/obj/")

total_files=0
total_changes=0

for file in $csproj_files; do
    echo "Processing: $file"
    total_files=$((total_files + 1))
    
    # Create backup
    cp "$file" "$file.backup"
    
    # Count changes before modification
    changes=$(grep -c 'PackageReference.*Version=' "$file" || echo 0)
    
    if [ $changes -gt 0 ]; then
        # Remove Version attributes from PackageReference elements
        sed -i 's/<PackageReference Include="\([^"]*\)" Version="[^"]*"/<PackageReference Include="\1"/g' "$file"
        
        # Also handle PackageReference elements that might have Version on a separate line
        sed -i '/<PackageReference Include=/,/>/ { s/ Version="[^"]*"//g }' "$file"
        
        total_changes=$((total_changes + changes))
        echo "  - Updated $changes PackageReference elements"
    else
        echo "  - No changes needed"
    fi
done

echo ""
echo "Migration Summary:"
echo "=================="
echo "Total project files processed: $total_files"
echo "Total PackageReference elements updated: $total_changes"
echo ""
echo "Migration completed! All projects now use centralized package management."
echo ""
echo "Next steps:"
echo "1. Run 'dotnet restore' to verify the migration"
echo "2. Run 'dotnet build' to ensure everything compiles"
echo "3. Commit the changes if everything works correctly"
echo "4. Remove .backup files once you're satisfied with the results"
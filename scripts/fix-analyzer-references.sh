#!/bin/bash

# Find all test project files that have Microsoft.CodeAnalysis.NetAnalyzers with version
echo "Removing Microsoft.CodeAnalysis.NetAnalyzers version attributes from test projects..."

# Find files with the analyzer reference and version
test_files=$(find . -name "*.csproj" -path "*/tests/*" -exec grep -l "Microsoft.CodeAnalysis.NetAnalyzers.*Version=" {} \;)

for file in $test_files; do
    echo "Processing: $file"
    # Remove the entire PackageReference line for Microsoft.CodeAnalysis.NetAnalyzers
    sed -i '/<PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers".*\/>/d' "$file"
    echo "  - Removed Microsoft.CodeAnalysis.NetAnalyzers reference"
done

echo "Done!"
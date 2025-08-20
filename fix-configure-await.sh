#!/bin/bash

# Script to add ConfigureAwait(false) to async method calls
# This improves performance by avoiding unnecessary context switching

echo "🔧 Fixing ConfigureAwait(false) in async methods..."

# Define directories to process
DIRS=(
    "src/Services"
    "src/Core"
    "src/Infrastructure"
    "src/Api"
)

# Counter for fixed occurrences
FIXED_COUNT=0

# Function to process C# files
process_csharp_files() {
    local dir=$1
    echo "Processing directory: $dir"
    
    # Find all C# files
    find "$dir" -name "*.cs" -type f | while read -r file; do
        # Skip generated files and test files
        if [[ "$file" == *".g.cs" ]] || [[ "$file" == *".Designer.cs" ]] || [[ "$file" == *"Test.cs" ]]; then
            continue
        fi
        
        # Create a temporary file
        temp_file="${file}.tmp"
        
        # Process the file
        # Pattern 1: await SomeMethod() -> await SomeMethod().ConfigureAwait(false)
        # Pattern 2: await SomeMethod(...) -> await SomeMethod(...).ConfigureAwait(false)
        # But skip if ConfigureAwait is already present
        
        perl -pe '
            # Skip if ConfigureAwait already exists on the line
            next if /ConfigureAwait/;
            
            # Match await patterns and add ConfigureAwait(false)
            s/(\bawait\s+[\w\.]+\([^)]*\))(?!\.ConfigureAwait)/$1.ConfigureAwait(false)/g;
            
            # Match await patterns with generic types
            s/(\bawait\s+[\w\.]+<[^>]+>\([^)]*\))(?!\.ConfigureAwait)/$1.ConfigureAwait(false)/g;
            
            # Match await patterns with property access
            s/(\bawait\s+[\w\.]+\.\w+)(?=;|\s*\)|,)(?!\.ConfigureAwait)/$1.ConfigureAwait(false)/g;
        ' "$file" > "$temp_file"
        
        # Check if file was modified
        if ! cmp -s "$file" "$temp_file"; then
            mv "$temp_file" "$file"
            echo "  ✓ Fixed: $file"
            ((FIXED_COUNT++))
        else
            rm "$temp_file"
        fi
    done
}

# Process each directory
for dir in "${DIRS[@]}"; do
    if [ -d "$dir" ]; then
        process_csharp_files "$dir"
    else
        echo "Directory not found: $dir"
    fi
done

echo ""
echo "✅ ConfigureAwait(false) fixes completed!"
echo "📊 Total files modified: $FIXED_COUNT"
echo ""
echo "⚠️  Please review the changes and run tests to ensure everything works correctly."
echo "💡 Tip: Use 'git diff' to review the changes before committing."
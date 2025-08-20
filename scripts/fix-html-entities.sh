#!/bin/bash

# Fix HTML entity encoding issues in C# files
echo "Fixing HTML entity encoding issues..."

# Find all C# files with HTML entities and fix them
find src/Core -name "*.cs" -type f | while read file; do
    if grep -q "&lt;\|&gt;\|&amp;\|= >" "$file"; then
        echo "Fixing: $file"
        # Create a backup
        cp "$file" "$file.bak"
        
        # Fix HTML entities and spacing issues in operators
        sed -i 's/&lt;/</g' "$file"
        sed -i 's/&gt;/>/g' "$file"
        sed -i 's/&amp;/\&/g' "$file"
        sed -i 's/= >/=>/g' "$file"
        sed -i 's/ = >/=>/g' "$file"
        sed -i 's/= > /=> /g' "$file"
        
        # Remove backup if successful
        if [ $? -eq 0 ]; then
            rm "$file.bak"
        else
            echo "Error fixing $file, restoring backup"
            mv "$file.bak" "$file"
        fi
    fi
done

echo "HTML entity fixes complete!"
#!/bin/bash
# Automated exception handler fixes for Neo Service Layer
# This script applies targeted exception type replacements based on context

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Backup directory
BACKUP_DIR="exception-fixes-backup-$(date +%Y%m%d_%H%M%S)"
mkdir -p "$BACKUP_DIR"

echo -e "${GREEN}Neo Service Layer - Automated Exception Handler Fixes${NC}"
echo "===================================================="
echo -e "${YELLOW}Creating backup in: $BACKUP_DIR${NC}"

# Function to create file backup
backup_file() {
    local file=$1
    local rel_path=${file#src/}
    local backup_path="$BACKUP_DIR/$rel_path"
    mkdir -p "$(dirname "$backup_path")"
    cp "$file" "$backup_path"
}

# Function to apply context-aware exception replacements
apply_exception_fixes() {
    local file=$1
    local changes_made=0
    
    # Create backup
    backup_file "$file"
    
    # Temporary file for changes
    local temp_file="${file}.tmp"
    cp "$file" "$temp_file"
    
    # Apply fixes based on context patterns
    
    # 1. File/IO operations
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "File\|Stream\|Directory\|Path\|Read\|Write"; then
        perl -i -pe 's/catch\s*\(\s*Exception\s+(\w+)\s*\)/catch (IOException $1)/g' "$temp_file"
        ((changes_made++)) || true
    fi
    
    # 2. Database operations
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "DbContext\|Database\|Sql\|Query\|Connection\|Transaction"; then
        perl -i -pe 's/catch\s*\(\s*Exception\s+(\w+)\s*\)(?=.*(?:DbContext|Database|Sql))/catch (DbException $1)/g' "$temp_file"
        ((changes_made++)) || true
    fi
    
    # 3. HTTP/Network operations
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "Http\|Client\|Request\|Response\|Api\|WebSocket"; then
        perl -i -pe 's/catch\s*\(\s*Exception\s+(\w+)\s*\)(?=.*(?:Http|Client|Request))/catch (HttpRequestException $1)/g' "$temp_file"
        ((changes_made++)) || true
    fi
    
    # 4. Parsing/Conversion operations
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "Parse\|Convert\|Format\|Deserialize\|Serialize"; then
        perl -i -pe 's/catch\s*\(\s*Exception\s+(\w+)\s*\)(?=.*(?:Parse|Convert))/catch (FormatException $1)/g' "$temp_file"
        ((changes_made++)) || true
    fi
    
    # 5. Task/Async operations
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "Task\|Async\|Await\|Cancel\|Timeout"; then
        perl -i -pe 's/catch\s*\(\s*Exception\s+(\w+)\s*\)(?=.*(?:Task|Cancel))/catch (OperationCanceledException $1)/g' "$temp_file"
        ((changes_made++)) || true
    fi
    
    # 6. Argument validation
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "Argument\|Parameter\|Validate\|null\|Invalid"; then
        perl -i -pe 's/catch\s*\(\s*Exception\s+(\w+)\s*\)(?=.*(?:Argument|null))/catch (ArgumentException $1)/g' "$temp_file"
        ((changes_made++)) || true
    fi
    
    # 7. Configuration operations
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "Configuration\|Settings\|Options\|Config"; then
        perl -i -pe 's/catch\s*\(\s*Exception\s+(\w+)\s*\)(?=.*(?:Configuration|Settings))/catch (ConfigurationException $1)/g' "$temp_file"
        ((changes_made++)) || true
    fi
    
    # 8. Security/Authentication operations
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "Auth\|Security\|Token\|Credential\|Permission"; then
        perl -i -pe 's/catch\s*\(\s*Exception\s+(\w+)\s*\)(?=.*(?:Auth|Security))/catch (SecurityException $1)/g' "$temp_file"
        ((changes_made++)) || true
    fi
    
    # If changes were made, update the file
    if [ $changes_made -gt 0 ]; then
        mv "$temp_file" "$file"
        return 0
    else
        rm "$temp_file"
        return 1
    fi
}

# Function to add necessary using statements
add_using_statements() {
    local file=$1
    
    # Check which exception types are used and add corresponding using statements
    local using_statements=""
    
    if grep -q "IOException" "$file"; then
        using_statements="${using_statements}using System.IO;\n"
    fi
    
    if grep -q "HttpRequestException" "$file"; then
        using_statements="${using_statements}using System.Net.Http;\n"
    fi
    
    if grep -q "DbException" "$file"; then
        using_statements="${using_statements}using System.Data.Common;\n"
    fi
    
    if grep -q "ConfigurationException" "$file"; then
        using_statements="${using_statements}using System.Configuration;\n"
    fi
    
    if grep -q "SecurityException" "$file"; then
        using_statements="${using_statements}using System.Security;\n"
    fi
    
    if [ -n "$using_statements" ]; then
        # Add using statements after the last existing using statement
        perl -i -pe "s/(using System.*?;)(?!.*using System)/$1\n$using_statements/s" "$file"
    fi
}

# Main processing
echo -e "${BLUE}Processing files...${NC}"

TOTAL_FILES=0
FIXED_FILES=0

# Find all C# files with broad exception handlers
for file in $(find src -name "*.cs" -type f -exec grep -l "catch\s*(Exception\s*\w*)" {} \; 2>/dev/null); do
    ((TOTAL_FILES++))
    echo -ne "\r${BLUE}Processing: $TOTAL_FILES files${NC}"
    
    if apply_exception_fixes "$file"; then
        add_using_statements "$file"
        ((FIXED_FILES++))
    fi
done

echo -e "\n${GREEN}Processing complete!${NC}"
echo -e "${YELLOW}Summary:${NC}"
echo "  Total files processed: $TOTAL_FILES"
echo "  Files fixed: $FIXED_FILES"
echo "  Backup location: $BACKUP_DIR"

# Generate detailed fix report
REPORT_FILE="exception-fixes-report-$(date +%Y%m%d_%H%M%S).md"
cat > "$REPORT_FILE" << EOF
# Exception Handler Fixes Report

Generated: $(date)

## Summary
- Total files processed: $TOTAL_FILES
- Files fixed: $FIXED_FILES
- Backup location: $BACKUP_DIR

## Applied Fixes

The following context-aware replacements were applied:

1. **File/IO Operations**: Exception → IOException
2. **Database Operations**: Exception → DbException
3. **HTTP/Network Operations**: Exception → HttpRequestException
4. **Parsing/Conversion**: Exception → FormatException
5. **Task/Async Operations**: Exception → OperationCanceledException
6. **Argument Validation**: Exception → ArgumentException
7. **Configuration Operations**: Exception → ConfigurationException
8. **Security/Authentication**: Exception → SecurityException

## Files Modified

EOF

# List modified files
if [ $FIXED_FILES -gt 0 ]; then
    echo "### Modified Files:" >> "$REPORT_FILE"
    for file in $(find src -name "*.cs" -newer "$BACKUP_DIR" -type f 2>/dev/null | head -20); do
        echo "- $file" >> "$REPORT_FILE"
    done
fi

echo -e "\n${GREEN}✓ Exception handler fixes applied!${NC}"
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Review the changes in modified files"
echo "2. Run the test suite: dotnet test"
echo "3. Check for compilation errors: dotnet build"
echo "4. Review the report: $REPORT_FILE"
echo ""
echo -e "${BLUE}To revert changes:${NC}"
echo "  cp -r $BACKUP_DIR/* src/"
#!/bin/bash
# Script to identify and help fix overly broad exception handlers
# This script generates a report of all broad catch blocks for manual review

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Output directory
REPORT_DIR="exception-handler-fixes"
mkdir -p "$REPORT_DIR"

echo -e "${GREEN}Neo Service Layer - Exception Handler Analysis${NC}"
echo "============================================="

# Find all C# files with broad exception handlers
echo -e "${BLUE}Scanning for broad exception handlers...${NC}"

# Create detailed report
REPORT_FILE="$REPORT_DIR/exception-handler-report.md"
cat > "$REPORT_FILE" << 'EOF'
# Exception Handler Analysis Report

## Overview
This report identifies all instances of overly broad exception handlers (`catch (Exception ex)`) in the codebase.

## Issues Found

EOF

# Find all files with broad exception handlers
FILES_WITH_ISSUES=$(find src -name "*.cs" -type f -exec grep -l "catch\s*(Exception\s*\w*)" {} \; 2>/dev/null | sort)

TOTAL_FILES=$(echo "$FILES_WITH_ISSUES" | wc -l)
echo -e "${YELLOW}Found $TOTAL_FILES files with broad exception handlers${NC}"

# Analyze each file
FILE_COUNT=0
for file in $FILES_WITH_ISSUES; do
    ((FILE_COUNT++))
    echo -e "\r${BLUE}Analyzing file $FILE_COUNT/$TOTAL_FILES${NC}\c"
    
    # Get file name relative to project root
    REL_FILE=${file#src/}
    
    # Count occurrences in this file
    OCCURRENCES=$(grep -n "catch\s*(Exception\s*\w*)" "$file" | wc -l)
    
    # Write to report
    echo -e "\n### $REL_FILE ($OCCURRENCES occurrences)" >> "$REPORT_FILE"
    echo '```csharp' >> "$REPORT_FILE"
    
    # Extract context around each catch block
    grep -n -B2 -A5 "catch\s*(Exception\s*\w*)" "$file" | sed 's/^/Line /' >> "$REPORT_FILE" 2>/dev/null || true
    
    echo '```' >> "$REPORT_FILE"
    
    # Determine likely exception type based on context
    echo -e "\n**Suggested fixes:**" >> "$REPORT_FILE"
    
    # Analyze the code context to suggest specific exceptions
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "File\|Stream\|Read\|Write"; then
        echo "- Consider using `IOException` for file operations" >> "$REPORT_FILE"
    fi
    
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "Parse\|Convert\|Format"; then
        echo "- Consider using `FormatException` or `ArgumentException` for parsing operations" >> "$REPORT_FILE"
    fi
    
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "Http\|Client\|Request\|Response"; then
        echo "- Consider using `HttpRequestException` for HTTP operations" >> "$REPORT_FILE"
    fi
    
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "Database\|Sql\|Query\|Connection"; then
        echo "- Consider using `DbException` or specific database exceptions" >> "$REPORT_FILE"
    fi
    
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "Task\|Async\|Await"; then
        echo "- Consider using `TaskCanceledException` or `OperationCanceledException`" >> "$REPORT_FILE"
    fi
    
    if grep -B5 "catch\s*(Exception" "$file" | grep -q "Argument\|Parameter\|Validate"; then
        echo "- Consider using `ArgumentException`, `ArgumentNullException`, or `ArgumentOutOfRangeException`" >> "$REPORT_FILE"
    fi
    
    echo "" >> "$REPORT_FILE"
done

echo -e "\n${GREEN}Analysis complete!${NC}"

# Generate summary
echo -e "\n## Summary Statistics" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# Count by namespace/service
echo "### Distribution by Service" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

for service_dir in src/Services/*; do
    if [ -d "$service_dir" ]; then
        SERVICE_NAME=$(basename "$service_dir")
        COUNT=$(find "$service_dir" -name "*.cs" -exec grep -l "catch\s*(Exception\s*\w*)" {} \; 2>/dev/null | wc -l)
        if [ $COUNT -gt 0 ]; then
            echo "- **$SERVICE_NAME**: $COUNT files" >> "$REPORT_FILE"
        fi
    fi
done

# Generate automated fix script template
FIX_SCRIPT="$REPORT_DIR/apply-exception-fixes.sh"
cat > "$FIX_SCRIPT" << 'SCRIPT_EOF'
#!/bin/bash
# Automated exception handler fixes
# Review each change carefully before applying

set -euo pipefail

echo "Applying exception handler fixes..."

# Example fixes - customize based on your analysis

# Fix 1: Replace generic exceptions in file operations
find src -name "*.cs" -type f -exec sed -i.bak \
    '/File\|Stream\|Read\|Write/{
        N
        s/catch\s*(Exception/catch (IOException/g
    }' {} \;

# Fix 2: Replace generic exceptions in HTTP operations
find src -name "*.cs" -type f -exec sed -i.bak \
    '/Http\|Client\|Request/{
        N
        s/catch\s*(Exception/catch (HttpRequestException/g
    }' {} \;

# Fix 3: Replace generic exceptions in parsing operations
find src -name "*.cs" -type f -exec sed -i.bak \
    '/Parse\|Convert\|TryParse/{
        N
        s/catch\s*(Exception/catch (FormatException/g
    }' {} \;

echo "Fixes applied. Please review changes and run tests."
SCRIPT_EOF

chmod +x "$FIX_SCRIPT"

# Generate specific fix recommendations
RECOMMENDATIONS="$REPORT_DIR/recommendations.md"
cat > "$RECOMMENDATIONS" << 'EOF'
# Exception Handler Fix Recommendations

## Priority Fixes

### 1. Security-Critical Services
These services handle sensitive operations and need specific exception handling:
- KeyManagementService
- SecretsManagementService
- EncryptionService
- AuthenticationService

### 2. Data Operations
Services that perform data operations should use specific database exceptions:
- StorageService
- BackupService
- DataService

### 3. Network Operations
Services making external calls need proper network exception handling:
- OracleService
- CrossChainService
- NotificationService

## Best Practices

1. **Use specific exception types**:
   ```csharp
   // Bad
   catch (Exception ex) { }
   
   // Good
   catch (IOException ioEx) { }
   catch (ArgumentNullException argEx) { }
   ```

2. **Log with appropriate detail**:
   ```csharp
   catch (SpecificException ex)
   {
       _logger.LogError(ex, "Operation failed: {Operation}", operationName);
       throw; // or handle appropriately
   }
   ```

3. **Consider exception filters**:
   ```csharp
   catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
   {
       // Handle 404 specifically
   }
   ```

4. **Use finally blocks for cleanup**:
   ```csharp
   try
   {
       // Operation
   }
   catch (SpecificException ex)
   {
       // Handle
   }
   finally
   {
       // Cleanup
   }
   ```

## Testing Exception Handlers

After fixing exception handlers:
1. Run unit tests
2. Run integration tests
3. Test error scenarios specifically
4. Verify logging output
5. Check error responses
EOF

# Summary output
echo -e "\n${GREEN}✓ Exception handler analysis complete!${NC}"
echo -e "${YELLOW}Files generated:${NC}"
echo "  - $REPORT_FILE (detailed analysis)"
echo "  - $FIX_SCRIPT (automated fix template)"
echo "  - $RECOMMENDATIONS (best practices)"
echo ""
echo -e "${BLUE}Next steps:${NC}"
echo "1. Review the detailed report: $REPORT_FILE"
echo "2. Apply automated fixes: ./$FIX_SCRIPT"
echo "3. Manually review and test each change"
echo "4. Run the full test suite"

# Generate quick stats
TOTAL_OCCURRENCES=$(grep -r "catch\s*(Exception\s*\w*)" src --include="*.cs" | wc -l)
echo -e "\n${YELLOW}Summary:${NC}"
echo "  Total files with issues: $TOTAL_FILES"
echo "  Total occurrences: $TOTAL_OCCURRENCES"
echo "  Average per file: $((TOTAL_OCCURRENCES / TOTAL_FILES))"
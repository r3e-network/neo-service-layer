#!/bin/bash

# Script to check and fix missing ConfigureAwait(false) in async methods
# This script analyzes C# files and adds ConfigureAwait(false) where missing

echo "=================================================="
echo "ConfigureAwait(false) Checker and Fixer"
echo "=================================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Counters
TOTAL_FILES=0
FILES_WITH_ISSUES=0
TOTAL_MISSING=0
TOTAL_FIXED=0

# Function to check a single file
check_file() {
    local file="$1"
    local fix_mode="$2"
    local file_issues=0
    
    # Skip generated files and test files
    if [[ "$file" == *"/obj/"* ]] || [[ "$file" == *"/bin/"* ]] || [[ "$file" == *".Designer.cs" ]]; then
        return 0
    fi
    
    # Check for async method calls without ConfigureAwait
    local missing=$(grep -n "await.*\.$" "$file" 2>/dev/null | grep -v "ConfigureAwait" | wc -l)
    
    if [ "$missing" -gt 0 ]; then
        FILES_WITH_ISSUES=$((FILES_WITH_ISSUES + 1))
        TOTAL_MISSING=$((TOTAL_MISSING + missing))
        
        echo -e "${YELLOW}File: ${file}${NC}"
        echo "  Missing ConfigureAwait(false): $missing occurrences"
        
        if [ "$fix_mode" == "fix" ]; then
            # Create backup
            cp "$file" "${file}.bak"
            
            # Fix missing ConfigureAwait(false)
            # This is a simplified fix - in production, use a proper C# parser
            sed -i.tmp -E 's/(await[[:space:]]+[^;]+)([[:space:]]*;)/\1.ConfigureAwait(false)\2/g' "$file"
            
            # Remove lines that now have double ConfigureAwait
            sed -i.tmp 's/\.ConfigureAwait(false)\.ConfigureAwait(false)/.ConfigureAwait(false)/g' "$file"
            
            # Clean up temp files
            rm -f "${file}.tmp"
            
            # Count fixes
            local fixed=$(grep -n "ConfigureAwait(false)" "$file" 2>/dev/null | wc -l)
            TOTAL_FIXED=$((TOTAL_FIXED + fixed))
            
            echo -e "  ${GREEN}Fixed: Added ConfigureAwait(false) to async calls${NC}"
        else
            # Show specific lines that need fixing
            echo "  Lines needing ConfigureAwait(false):"
            grep -n "await.*\.$" "$file" 2>/dev/null | grep -v "ConfigureAwait" | head -5 | while read -r line; do
                echo "    $line"
            done
        fi
        
        echo ""
    fi
}

# Function to analyze project statistics
analyze_stats() {
    echo "=================================================="
    echo "Async/Await Statistics"
    echo "=================================================="
    
    local total_async=$(find . -name "*.cs" -type f ! -path "*/bin/*" ! -path "*/obj/*" | xargs grep -h "async " | wc -l)
    local total_await=$(find . -name "*.cs" -type f ! -path "*/bin/*" ! -path "*/obj/*" | xargs grep -h "await " | wc -l)
    local total_configured=$(find . -name "*.cs" -type f ! -path "*/bin/*" ! -path "*/obj/*" | xargs grep -h "ConfigureAwait" | wc -l)
    
    echo "Total async methods: $total_async"
    echo "Total await calls: $total_await"
    echo "Total ConfigureAwait calls: $total_configured"
    
    if [ "$total_await" -gt 0 ]; then
        local percentage=$((total_configured * 100 / total_await))
        echo "ConfigureAwait coverage: ${percentage}%"
    fi
    
    echo ""
}

# Function to generate report
generate_report() {
    local report_file="configure-await-report.md"
    
    cat > "$report_file" << EOF
# ConfigureAwait(false) Analysis Report

Generated: $(date)

## Summary

- **Total Files Analyzed**: $TOTAL_FILES
- **Files with Missing ConfigureAwait**: $FILES_WITH_ISSUES
- **Total Missing Occurrences**: $TOTAL_MISSING
- **Total Fixed**: $TOTAL_FIXED

## Recommendations

1. Always use \`ConfigureAwait(false)\` in library code
2. Consider using \`ConfigureAwait(true)\` only when necessary in UI code
3. Use analyzer rules to enforce this pattern:
   - CA2007: Do not directly await a Task
   - CA2008: Do not create tasks without passing a TaskScheduler

## Files Requiring Attention

EOF
    
    # List files with most issues
    find . -name "*.cs" -type f ! -path "*/bin/*" ! -path "*/obj/*" | while read -r file; do
        local missing=$(grep -n "await.*\.$" "$file" 2>/dev/null | grep -v "ConfigureAwait" | wc -l)
        if [ "$missing" -gt 0 ]; then
            echo "- $file: $missing missing" >> "$report_file"
        fi
    done | sort -t: -k2 -nr | head -20
    
    echo ""
    echo -e "${GREEN}Report generated: $report_file${NC}"
}

# Main execution
MODE="${1:-check}"

if [ "$MODE" == "--help" ] || [ "$MODE" == "-h" ]; then
    echo "Usage: $0 [check|fix|stats|report]"
    echo ""
    echo "Modes:"
    echo "  check  - Check for missing ConfigureAwait(false) (default)"
    echo "  fix    - Automatically add ConfigureAwait(false) where missing"
    echo "  stats  - Show async/await statistics"
    echo "  report - Generate detailed report"
    echo ""
    exit 0
fi

echo "Mode: $MODE"
echo ""

# Change to project root
cd "$(dirname "$0")/.." || exit 1

if [ "$MODE" == "stats" ]; then
    analyze_stats
    exit 0
fi

if [ "$MODE" == "report" ]; then
    # First collect data
    find . -name "*.cs" -type f ! -path "*/bin/*" ! -path "*/obj/*" | while read -r file; do
        TOTAL_FILES=$((TOTAL_FILES + 1))
        check_file "$file" "check" > /dev/null 2>&1
    done
    
    generate_report
    exit 0
fi

# Process all C# files
echo "Analyzing C# files..."
echo ""

find . -name "*.cs" -type f ! -path "*/bin/*" ! -path "*/obj/*" | while read -r file; do
    TOTAL_FILES=$((TOTAL_FILES + 1))
    check_file "$file" "$MODE"
done

# Summary
echo "=================================================="
echo "Summary"
echo "=================================================="
echo "Total files analyzed: $TOTAL_FILES"
echo "Files with missing ConfigureAwait: $FILES_WITH_ISSUES"
echo "Total missing ConfigureAwait calls: $TOTAL_MISSING"

if [ "$MODE" == "fix" ]; then
    echo -e "${GREEN}Total fixed: $TOTAL_FIXED${NC}"
    echo ""
    echo "Backup files created with .bak extension"
    echo "Review changes and remove backups when satisfied"
else
    echo ""
    echo "Run with 'fix' mode to automatically add ConfigureAwait(false)"
    echo "Example: $0 fix"
fi

# Create .editorconfig rule if it doesn't exist
if [ ! -f ".editorconfig" ] || ! grep -q "dotnet_code_quality.CA2007.ConfigureAwait" ".editorconfig"; then
    echo ""
    echo "Adding analyzer rule to .editorconfig..."
    cat >> .editorconfig << EOF

# Async/Await Rules
dotnet_code_quality.CA2007.ConfigureAwait = true
dotnet_code_quality.CA2007.severity = warning
dotnet_code_quality.CA2008.severity = warning
EOF
    echo -e "${GREEN}Analyzer rules added to .editorconfig${NC}"
fi
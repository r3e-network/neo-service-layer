#!/bin/bash
# Neo Service Layer - Project Cleanup Script
# Removes build artifacts, temporary files, and outdated content

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${GREEN}Neo Service Layer - Project Cleanup${NC}"
echo "===================================="
echo ""

# Dry run mode by default
DRY_RUN="${DRY_RUN:-true}"

if [ "$DRY_RUN" = "true" ]; then
    echo -e "${YELLOW}Running in DRY RUN mode. No files will be deleted.${NC}"
    echo "To actually delete files, run: DRY_RUN=false $0"
    echo ""
fi

# Function to remove files/directories
remove_item() {
    local item=$1
    local type=$2
    
    if [ "$DRY_RUN" = "true" ]; then
        echo -e "${BLUE}[DRY RUN] Would remove $type: $item${NC}"
    else
        if [ -e "$item" ]; then
            rm -rf "$item"
            echo -e "${GREEN}✓ Removed $type: $item${NC}"
        fi
    fi
}

# Track statistics
TOTAL_ITEMS=0
DIRS_TO_REMOVE=0
FILES_TO_REMOVE=0

echo -e "${YELLOW}1. Cleaning build artifacts...${NC}"
# Remove obj and bin directories
while IFS= read -r dir; do
    if [ -d "$dir" ]; then
        ((DIRS_TO_REMOVE++))
        ((TOTAL_ITEMS++))
        remove_item "$dir" "build directory"
    fi
done < <(find . -type d \( -name "obj" -o -name "bin" \) -not -path "./.nuget/*" -not -path "./node_modules/*" 2>/dev/null)

echo ""
echo -e "${YELLOW}2. Cleaning Visual Studio artifacts...${NC}"
# Remove .vs and .vscode directories
for dir in $(find . -type d \( -name ".vs" -o -name ".vscode" \) 2>/dev/null); do
    ((DIRS_TO_REMOVE++))
    ((TOTAL_ITEMS++))
    remove_item "$dir" "VS directory"
done

echo ""
echo -e "${YELLOW}3. Cleaning temporary and backup files...${NC}"
# Remove various temporary files
while IFS= read -r file; do
    if [ -f "$file" ]; then
        ((FILES_TO_REMOVE++))
        ((TOTAL_ITEMS++))
        remove_item "$file" "temporary file"
    fi
done < <(find . \( -name "*.tmp" -o -name "*.temp" -o -name "*.backup" -o -name "*.bak" -o -name "*.old" -o -name "*~" -o -name "*.swp" -o -name ".DS_Store" -o -name "Thumbs.db" \) -type f 2>/dev/null)

echo ""
echo -e "${YELLOW}4. Cleaning log files...${NC}"
# Remove log files (except important ones)
while IFS= read -r file; do
    # Skip validation and deployment logs we might want to keep
    if [[ ! "$file" =~ validation-report.*\.log$ ]] && [[ ! "$file" =~ deployment-report.*\.log$ ]]; then
        ((FILES_TO_REMOVE++))
        ((TOTAL_ITEMS++))
        remove_item "$file" "log file"
    fi
done < <(find . -name "*.log" -type f 2>/dev/null)

echo ""
echo -e "${YELLOW}5. Cleaning test artifacts...${NC}"
# Remove test results and coverage files
for pattern in "TestResults" "*.trx" "coverage.cobertura.xml" "coverage.json" ".coverage"; do
    while IFS= read -r item; do
        ((TOTAL_ITEMS++))
        if [ -d "$item" ]; then
            ((DIRS_TO_REMOVE++))
            remove_item "$item" "test directory"
        else
            ((FILES_TO_REMOVE++))
            remove_item "$item" "test file"
        fi
    done < <(find . -name "$pattern" 2>/dev/null)
done

echo ""
echo -e "${YELLOW}6. Cleaning Python artifacts...${NC}"
# Remove Python cache directories
while IFS= read -r dir; do
    ((DIRS_TO_REMOVE++))
    ((TOTAL_ITEMS++))
    remove_item "$dir" "Python cache"
done < <(find . -type d \( -name "__pycache__" -o -name ".pytest_cache" -o -name "*.egg-info" \) 2>/dev/null)

echo ""
echo -e "${YELLOW}7. Cleaning package lock files and caches...${NC}"
# Remove package-lock.json if package.json doesn't exist
while IFS= read -r lockfile; do
    dir=$(dirname "$lockfile")
    if [ ! -f "$dir/package.json" ]; then
        ((FILES_TO_REMOVE++))
        ((TOTAL_ITEMS++))
        remove_item "$lockfile" "orphaned lock file"
    fi
done < <(find . -name "package-lock.json" -type f 2>/dev/null)

echo ""
echo -e "${YELLOW}8. Cleaning duplicate and outdated files...${NC}"
# Remove specific known duplicates and outdated files
OUTDATED_FILES=(
    "src/Api/NeoServiceLayer.Api/Program.Minimal.cs.backup"
    "src/Api/NeoServiceLayer.Api/Program.Production.cs.backup"
    "GITHUB_ACTIONS_FIXES.md"
    "fix-all-todos.sh"
    "fix-program-files.sh"
    "neo-express-workflow-test.sh"
    "deployment-phase1-*.log"
)

for file_pattern in "${OUTDATED_FILES[@]}"; do
    for file in $file_pattern; do
        if [ -e "$file" ]; then
            ((FILES_TO_REMOVE++))
            ((TOTAL_ITEMS++))
            remove_item "$file" "outdated file"
        fi
    done
done

echo ""
echo -e "${YELLOW}9. Cleaning Neo Express artifacts...${NC}"
# Remove .neo-express directory and checkpoint files
for item in ".neo-express" "*.neo-express" "*.checkpoint"; do
    while IFS= read -r found; do
        ((TOTAL_ITEMS++))
        if [ -d "$found" ]; then
            ((DIRS_TO_REMOVE++))
            remove_item "$found" "Neo Express directory"
        else
            ((FILES_TO_REMOVE++))
            remove_item "$found" "Neo Express file"
        fi
    done < <(find . -name "$item" 2>/dev/null)
done

echo ""
echo -e "${YELLOW}10. Cleaning empty directories...${NC}"
# Remove empty directories (only in actual run mode)
if [ "$DRY_RUN" = "false" ]; then
    find . -type d -empty -not -path "./.git/*" -delete 2>/dev/null || true
    echo -e "${GREEN}✓ Removed empty directories${NC}"
else
    EMPTY_DIRS=$(find . -type d -empty -not -path "./.git/*" 2>/dev/null | wc -l)
    echo -e "${BLUE}[DRY RUN] Would remove $EMPTY_DIRS empty directories${NC}"
fi

echo ""
echo -e "${YELLOW}11. Cleaning obsolete directories...${NC}"
# Clean up specific obsolete directories
OBSOLETE_DIRS=(
    ".hive-mind"
    ".swarm"
    ".claude-flow"
    "contracts-neo-n3/.claude-flow"
    "contracts-neo-n3/.swarm"
    "contracts-neo-n3/src/ProductionReady/.claude-flow"
    "contracts-neo-n3/src/ProductionReady/.swarm"
)

for dir in "${OBSOLETE_DIRS[@]}"; do
    if [ -d "$dir" ]; then
        ((DIRS_TO_REMOVE++))
        ((TOTAL_ITEMS++))
        remove_item "$dir" "obsolete directory"
    fi
done

echo ""
echo -e "${YELLOW}12. Organizing documentation...${NC}"
# Create organized docs structure
if [ "$DRY_RUN" = "false" ]; then
    mkdir -p docs/{architecture,guides,api,deployment,security}
    
    # Move reports to appropriate folders
    [ -f "docs/PRODUCTION_READINESS_SUMMARY.md" ] && mv -f "docs/PRODUCTION_READINESS_SUMMARY.md" "docs/deployment/" 2>/dev/null || true
    [ -f "docs/PRODUCTION_READINESS_UPDATE.md" ] && mv -f "docs/PRODUCTION_READINESS_UPDATE.md" "docs/deployment/" 2>/dev/null || true
    [ -f "docs/FINAL_PRODUCTION_READINESS_REPORT.md" ] && mv -f "docs/FINAL_PRODUCTION_READINESS_REPORT.md" "docs/deployment/" 2>/dev/null || true
    [ -f "docs/database-sharding-strategy.md" ] && mv -f "docs/database-sharding-strategy.md" "docs/architecture/" 2>/dev/null || true
    [ -f "docs/resilience-integration-guide.md" ] && mv -f "docs/resilience-integration-guide.md" "docs/guides/" 2>/dev/null || true
    [ -f "docs/SERVICE_FRAMEWORK_GUIDE.md" ] && mv -f "docs/SERVICE_FRAMEWORK_GUIDE.md" "docs/guides/" 2>/dev/null || true
    
    echo -e "${GREEN}✓ Organized documentation structure${NC}"
else
    echo -e "${BLUE}[DRY RUN] Would organize documentation into subdirectories${NC}"
fi

echo ""
echo -e "${YELLOW}===== CLEANUP SUMMARY =====${NC}"
echo "Total items to clean: $TOTAL_ITEMS"
echo "Directories to remove: $DIRS_TO_REMOVE"
echo "Files to remove: $FILES_TO_REMOVE"

if [ "$DRY_RUN" = "true" ]; then
    echo ""
    echo -e "${YELLOW}This was a DRY RUN. No files were actually deleted.${NC}"
    echo "To perform the actual cleanup, run:"
    echo -e "${GREEN}DRY_RUN=false $0${NC}"
    
    # Estimate space savings
    if command -v du >/dev/null 2>&1; then
        echo ""
        echo -e "${BLUE}Estimated space savings:${NC}"
        
        # Calculate space for obj/bin directories
        OBJ_BIN_SIZE=$(find . -type d \( -name "obj" -o -name "bin" \) -not -path "./.nuget/*" -exec du -sb {} \; 2>/dev/null | awk '{sum+=$1} END {print sum}' || echo "0")
        if [ "$OBJ_BIN_SIZE" -gt 0 ]; then
            echo "  Build artifacts: $(numfmt --to=iec-i --suffix=B $OBJ_BIN_SIZE)"
        fi
        
        # Calculate total size that would be freed
        TOTAL_SIZE=$(find . \( -name "*.tmp" -o -name "*.log" -o -name "*.backup" -o -name "*.bak" -o -type d -name "obj" -o -type d -name "bin" -o -type d -name "__pycache__" \) -not -path "./.nuget/*" -exec du -sb {} \; 2>/dev/null | awk '{sum+=$1} END {print sum}' || echo "0")
        if [ "$TOTAL_SIZE" -gt 0 ]; then
            echo -e "${GREEN}  Total: $(numfmt --to=iec-i --suffix=B $TOTAL_SIZE)${NC}"
        fi
    fi
else
    echo ""
    echo -e "${GREEN}✅ Cleanup completed successfully!${NC}"
    
    # Run git status to show what changed
    echo ""
    echo -e "${BLUE}Git status after cleanup:${NC}"
    git status --short || true
fi

echo ""
echo -e "${YELLOW}Recommendations:${NC}"
echo "1. Review the changes with 'git status' and 'git diff'"
echo "2. Add important generated files to .gitignore"
echo "3. Commit the cleanup: git add -A && git commit -m 'chore: Clean up project artifacts and organize structure'"
echo "4. Consider running 'dotnet build' to ensure everything still works"

# Create/update .gitignore if needed
if [ "$DRY_RUN" = "false" ]; then
    echo ""
    echo -e "${YELLOW}Updating .gitignore...${NC}"
    
    # Add common patterns if not already present
    GITIGNORE_ADDITIONS=(
        "# Build artifacts"
        "**/bin/"
        "**/obj/"
        "*.user"
        "*.suo"
        ".vs/"
        ".vscode/"
        ""
        "# Test artifacts"
        "TestResults/"
        "*.trx"
        "coverage.cobertura.xml"
        "coverage.json"
        ".coverage"
        ""
        "# Temporary files"
        "*.tmp"
        "*.temp"
        "*.backup"
        "*.bak"
        "*.log"
        "*.swp"
        "*~"
        ""
        "# OS files"
        ".DS_Store"
        "Thumbs.db"
        ""
        "# Python"
        "__pycache__/"
        "*.py[cod]"
        ".pytest_cache/"
        "*.egg-info/"
        ""
        "# Neo Express"
        ".neo-express/"
        "*.neo-express"
        "*.checkpoint"
        ""
        "# Node"
        "node_modules/"
        "npm-debug.log"
        ""
        "# IDE"
        "*.DotSettings.user"
        ".idea/"
    )
    
    for pattern in "${GITIGNORE_ADDITIONS[@]}"; do
        if ! grep -qF "$pattern" .gitignore 2>/dev/null; then
            echo "$pattern" >> .gitignore
        fi
    done
    
    echo -e "${GREEN}✓ Updated .gitignore${NC}"
fi
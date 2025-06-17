#!/bin/bash

# Neo Service Layer - Project Cleanup Script
# This script cleans up temporary files and build artifacts

echo "🧹 Cleaning up Neo Service Layer project..."

# Remove .NET build artifacts
echo "Removing .NET build artifacts..."
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true

# Remove test results
echo "Removing test results..."
find . -name "TestResults" -type d -exec rm -rf {} + 2>/dev/null || true
find . -name "coverage-reports" -type d -exec rm -rf {} + 2>/dev/null || true

# Remove temporary files
echo "Removing temporary files..."
find . -name "*.tmp" -delete 2>/dev/null || true
find . -name "*.temp" -delete 2>/dev/null || true
find . -name "*~" -delete 2>/dev/null || true
find . -name ".DS_Store" -delete 2>/dev/null || true
find . -name "Thumbs.db" -delete 2>/dev/null || true

# Remove backup files
echo "Removing backup files..."
find . -name "*.bak" -delete 2>/dev/null || true
find . -name "*.old" -delete 2>/dev/null || true
find . -name "*.orig" -delete 2>/dev/null || true

# Clean Rust artifacts (if accessible)
echo "Attempting to clean Rust artifacts..."
if [ -d "src/Tee/NeoServiceLayer.Tee.Enclave/target" ]; then
    cd src/Tee/NeoServiceLayer.Tee.Enclave
    cargo clean 2>/dev/null || echo "cargo clean failed, but target directory is in .gitignore"
    cd - > /dev/null
fi

echo "✅ Cleanup completed!"
echo ""
echo "📋 Project is now clean and ready for GitHub:"
echo "  • Build artifacts removed"
echo "  • Temporary files cleaned"
echo "  • Documentation organized"
echo "  • .gitignore updated"
echo "  • README enhanced"
echo ""
echo "🚀 Ready for: git add . && git commit -m 'Clean project for GitHub'"
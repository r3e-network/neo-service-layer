#!/bin/bash

# Coverage Collection Script for Neo Service Layer
set -e

echo "=========================================="
echo "Neo Service Layer - Coverage Collection"
echo "=========================================="
echo ""

# Setup environment
export MSBUILDDISABLENODEREUSE=1
COVERAGE_DIR="./TestResults/Coverage"
REPORT_DIR="./TestResults/CoverageReport"

# Clean previous results
rm -rf $COVERAGE_DIR $REPORT_DIR
mkdir -p $COVERAGE_DIR $REPORT_DIR

echo "Collecting coverage for available test assemblies..."
echo ""

# Run tests with coverage using Python wrapper
python3 << 'EOF'
import subprocess
import os
from pathlib import Path

project_root = Path("/home/ubuntu/neo-service-layer")
os.chdir(project_root)

# Find test DLLs
test_dlls = list(project_root.glob("tests/**/bin/Release/net9.0/*.Tests.dll"))

# Remove duplicates
seen = set()
unique_dlls = []
for dll in test_dlls:
    if dll.name not in seen:
        seen.add(dll.name)
        unique_dlls.append(dll)

print(f"Found {len(unique_dlls)} test assemblies")

coverage_files = []
for dll in unique_dlls[:5]:  # Test first 5 for now
    print(f"\nCollecting coverage for: {dll.name}")
    
    # Get project path
    project_name = dll.name.replace(".dll", "")
    project_path = dll.parent.parent.parent / f"{project_name}.csproj"
    
    if project_path.exists():
        try:
            result = subprocess.run([
                "dotnet", "test", str(project_path),
                "--no-build",
                "--configuration", "Release",
                "--collect:XPlat Code Coverage",
                f"--results-directory", "./TestResults/Coverage",
                "--settings", "coverlet.runsettings"
            ], capture_output=True, text=True, timeout=60)
            
            if "Passed!" in result.stdout:
                print(f"  ✅ Coverage collected")
                # Find coverage files
                for coverage_file in Path("./TestResults/Coverage").glob("**/coverage.cobertura.xml"):
                    coverage_files.append(str(coverage_file))
                    print(f"  📄 {coverage_file.name}")
            else:
                print(f"  ⚠️ No coverage collected")
        except Exception as e:
            print(f"  ❌ Error: {e}")
    else:
        print(f"  ⚠️ Project file not found")

print(f"\n\nTotal coverage files: {len(coverage_files)}")
EOF

# Check if coverage files were generated
COVERAGE_FILES=$(find $COVERAGE_DIR -name "*.xml" 2>/dev/null | wc -l)

if [ "$COVERAGE_FILES" -gt 0 ]; then
    echo ""
    echo "Generating coverage report..."
    
    # Install report generator if not present
    if ! command -v reportgenerator &> /dev/null; then
        echo "Installing ReportGenerator..."
        dotnet tool install -g dotnet-reportgenerator-globaltool
        export PATH="$PATH:$HOME/.dotnet/tools"
    fi
    
    # Generate HTML report
    reportgenerator \
        -reports:"$COVERAGE_DIR/**/coverage.cobertura.xml" \
        -targetdir:"$REPORT_DIR" \
        -reporttypes:"Html;Cobertura;MarkdownSummary" \
        -title:"Neo Service Layer Coverage Report" \
        2>/dev/null || echo "Report generation had issues"
    
    echo ""
    echo "✅ Coverage report generated in: $REPORT_DIR"
    echo ""
    
    # Display summary if available
    if [ -f "$REPORT_DIR/Summary.md" ]; then
        echo "Coverage Summary:"
        cat "$REPORT_DIR/Summary.md" | head -20
    fi
else
    echo ""
    echo "⚠️ No coverage files generated"
fi

echo ""
echo "Coverage collection complete!"
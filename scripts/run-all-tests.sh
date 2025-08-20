#\!/bin/bash

# Neo Service Layer - Complete Test Execution Script
set -e

echo "=========================================="
echo "Neo Service Layer - Complete Test Suite"
echo "=========================================="

export MSBUILDDISABLENODEREUSE=1

# Clean and run tests
rm -rf TestResults
mkdir -p TestResults

# Use Python runner
python3 scripts/full-test-execution.py

echo "Test execution complete\!"

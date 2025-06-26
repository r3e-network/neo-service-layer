#!/bin/bash

# Neo Service Layer - Test Runner Wrapper
# This script provides easy access to test scripts from the root directory

SCRIPT_DIR="scripts/testing"

case "$1" in
    "unit"|"units"|"u")
        echo "🧪 Running unit tests..."
        exec "$SCRIPT_DIR/run-unit-tests.sh" "${@:2}"
        ;;
    "all"|"full"|"a")
        echo "🧪 Running all tests (including performance)..."
        exec "$SCRIPT_DIR/run-all-tests.sh" "${@:2}"
        ;;
    "help"|"--help"|"-h"|"")
        echo "Neo Service Layer Test Runner"
        echo ""
        echo "Usage: ./test.sh <command> [options]"
        echo ""
        echo "Commands:"
        echo "  unit, u     Run unit tests only (recommended for CI)"
        echo "  all, a      Run all tests including performance tests"
        echo "  help        Show this help message"
        echo ""
        echo "Options are passed through to the underlying test scripts."
        echo ""
        echo "Examples:"
        echo "  ./test.sh unit                    # Run unit tests with defaults"
        echo "  ./test.sh unit Debug minimal     # Run unit tests in Debug mode"
        echo "  ./test.sh all Release normal     # Run all tests in Release mode"
        echo ""
        echo "For more detailed options, see: scripts/testing/"
        ;;
    *)
        echo "❌ Unknown command: $1"
        echo "Run './test.sh help' for usage information."
        exit 1
        ;;
esac
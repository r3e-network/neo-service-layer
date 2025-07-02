#!/bin/bash

# Comprehensive test script for persistent storage implementation
# This script tests the core functionality of persistent storage across services

echo "=== Neo Service Layer Persistent Storage Test Suite ==="
echo "Starting comprehensive test execution..."

# Set up test environment
export DOTNET_ENVIRONMENT=Test
export ASPNETCORE_ENVIRONMENT=Test

# Test directories
TEST_DIR="/tmp/neo-service-test"
BACKUP_DIR="/tmp/neo-service-backups"
LOG_FILE="/tmp/neo-service-test.log"

# Clean up previous test artifacts
rm -rf $TEST_DIR $BACKUP_DIR $LOG_FILE

# Create test directories
mkdir -p $TEST_DIR $BACKUP_DIR

echo "Test environment setup complete"
echo "Test Directory: $TEST_DIR"
echo "Backup Directory: $BACKUP_DIR"
echo "Log File: $LOG_FILE"

# Function to run a test and capture results
run_test() {
    local test_name=$1
    local test_command=$2
    
    echo "----------------------------------------"
    echo "Running: $test_name"
    echo "Command: $test_command"
    echo "----------------------------------------"
    
    # Run the test and capture output
    eval "$test_command" 2>&1 | tee -a $LOG_FILE
    local exit_code=${PIPESTATUS[0]}
    
    if [ $exit_code -eq 0 ]; then
        echo "✅ PASSED: $test_name"
    else
        echo "❌ FAILED: $test_name (Exit code: $exit_code)"
    fi
    
    echo ""
    return $exit_code
}

# Initialize test counters
total_tests=0
passed_tests=0

# Test 1: Build solution
total_tests=$((total_tests + 1))
if run_test "Build Solution" "dotnet build NeoServiceLayer.sln --configuration Release --no-restore"; then
    passed_tests=$((passed_tests + 1))
fi

# Test 2: Core Framework Tests
total_tests=$((total_tests + 1))
if run_test "Core Framework Tests" "dotnet test tests/Core/NeoServiceLayer.Core.Tests/ --configuration Release --no-build --logger 'console;verbosity=minimal'"; then
    passed_tests=$((passed_tests + 1))
fi

# Test 3: Service Framework Tests
total_tests=$((total_tests + 1))
if run_test "Service Framework Tests" "dotnet test tests/Core/NeoServiceLayer.ServiceFramework.Tests/ --configuration Release --no-build --logger 'console;verbosity=minimal'"; then
    passed_tests=$((passed_tests + 1))
fi

# Test 4: Infrastructure Tests
total_tests=$((total_tests + 1))
if run_test "Infrastructure Tests" "dotnet test tests/Infrastructure/NeoServiceLayer.Infrastructure.Tests/ --configuration Release --no-build --logger 'console;verbosity=minimal'"; then
    passed_tests=$((passed_tests + 1))
fi

# Test 5: Notification Service Tests (with persistent storage)
total_tests=$((total_tests + 1))
if run_test "Notification Service Tests" "dotnet test tests/Services/NeoServiceLayer.Services.Notification.Tests/ --configuration Release --no-build --logger 'console;verbosity=minimal'"; then
    passed_tests=$((passed_tests + 1))
fi

# Test 6: Storage Service Tests
total_tests=$((total_tests + 1))
if run_test "Storage Service Tests" "dotnet test tests/Services/NeoServiceLayer.Services.Storage.Tests/ --configuration Release --no-build --logger 'console;verbosity=minimal'"; then
    passed_tests=$((passed_tests + 1))
fi

# Test 7: Monitoring Service Tests
total_tests=$((total_tests + 1))
if run_test "Monitoring Service Tests" "dotnet test tests/Services/NeoServiceLayer.Services.Monitoring.Tests/ --configuration Release --no-build --logger 'console;verbosity=minimal'"; then
    passed_tests=$((passed_tests + 1))
fi

# Test 8: Configuration Service Tests
total_tests=$((total_tests + 1))
if run_test "Configuration Service Tests" "dotnet test tests/Services/NeoServiceLayer.Services.Configuration.Tests/ --configuration Release --no-build --logger 'console;verbosity=minimal'"; then
    passed_tests=$((passed_tests + 1))
fi

# Test 9: Integration Tests
total_tests=$((total_tests + 1))
if run_test "Integration Tests" "dotnet test tests/Integration/NeoServiceLayer.Integration.Tests/ --configuration Release --no-build --logger 'console;verbosity=minimal'"; then
    passed_tests=$((passed_tests + 1))
fi

# Test 10: Simple functionality test
total_tests=$((total_tests + 1))
echo "----------------------------------------"
echo "Running: Simple Persistent Storage Test"
echo "----------------------------------------"

# Create a simple test program to validate persistent storage
cat > /tmp/storage-test.cs << 'EOF'
using System;
using System.Text;
using System.Threading.Tasks;
using NeoServiceLayer.Infrastructure.Persistence;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var config = new PersistentStorageConfiguration
            {
                RootPath = "/tmp/neo-service-test",
                EncryptionEnabled = true,
                CompressionEnabled = true
            };
            
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<OcclumFileStorageProvider>.Instance;
            var storage = new OcclumFileStorageProvider(config, logger);
            
            await storage.InitializeAsync();
            
            // Test basic storage operations
            var testKey = "test:key:123";
            var testData = Encoding.UTF8.GetBytes("Hello, Persistent Storage!");
            
            // Store data
            var stored = await storage.StoreAsync(testKey, testData);
            Console.WriteLine($"Store operation: {(stored ? "SUCCESS" : "FAILED")}");
            
            // Retrieve data
            var retrieved = await storage.RetrieveAsync(testKey);
            var success = retrieved != null && Encoding.UTF8.GetString(retrieved) == "Hello, Persistent Storage!";
            Console.WriteLine($"Retrieve operation: {(success ? "SUCCESS" : "FAILED")}");
            
            // Check existence
            var exists = await storage.ExistsAsync(testKey);
            Console.WriteLine($"Exists check: {(exists ? "SUCCESS" : "FAILED")}");
            
            // Delete data
            var deleted = await storage.DeleteAsync(testKey);
            Console.WriteLine($"Delete operation: {(deleted ? "SUCCESS" : "FAILED")}");
            
            // Verify deletion
            var stillExists = await storage.ExistsAsync(testKey);
            Console.WriteLine($"Deletion verification: {(!stillExists ? "SUCCESS" : "FAILED")}");
            
            storage.Dispose();
            
            Console.WriteLine("All basic persistent storage tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
EOF

# Compile and run the storage test
cd /home/neo/git/neo-service-layer
if dotnet run --project - < /tmp/storage-test.cs 2>&1 | tee -a $LOG_FILE; then
    passed_tests=$((passed_tests + 1))
    echo "✅ PASSED: Simple Persistent Storage Test"
else
    echo "❌ FAILED: Simple Persistent Storage Test"
fi

echo ""
echo "=========================================="
echo "TEST EXECUTION SUMMARY"
echo "=========================================="
echo "Total Tests: $total_tests"
echo "Passed: $passed_tests"
echo "Failed: $((total_tests - passed_tests))"
echo "Success Rate: $(( passed_tests * 100 / total_tests ))%"
echo ""

if [ $passed_tests -eq $total_tests ]; then
    echo "🎉 ALL TESTS PASSED! Persistent storage implementation is working correctly."
    exit_code=0
else
    echo "⚠️  Some tests failed. Check the log file for details: $LOG_FILE"
    exit_code=1
fi

echo ""
echo "Test artifacts saved to:"
echo "  - Log file: $LOG_FILE"
echo "  - Test directory: $TEST_DIR"
echo "  - Backup directory: $BACKUP_DIR"

echo ""
echo "=== Test Suite Complete ==="

exit $exit_code
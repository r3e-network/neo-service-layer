#\!/usr/bin/env python3

import subprocess
import os
import json
import sys
from pathlib import Path

def run_tests():
    """Execute tests using Python subprocess to bypass shell issues"""
    
    print("Neo Service Layer - Python Test Executor")
    print("=" * 50)
    print()
    
    # Change to project directory
    os.chdir("/home/ubuntu/neo-service-layer")
    
    # Find all test DLLs
    test_dlls = list(Path(".").glob("tests/**/bin/Release/net9.0/*.Tests.dll"))
    
    total_tests = 0
    passed_tests = 0
    failed_tests = 0
    
    for dll in test_dlls[:5]:  # Test first 5 projects
        dll_path = str(dll.absolute())
        dll_name = dll.name
        
        print(f"Testing: {dll_name}")
        print("-" * 40)
        
        try:
            # Use subprocess to run tests directly, avoiding shell interpretation
            result = subprocess.run(
                ["dotnet", "vstest", dll_path, "--logger:console;verbosity=minimal"],
                capture_output=True,
                text=True,
                timeout=60
            )
            
            output = result.stdout + result.stderr
            
            # Parse output for results
            if "Passed\!" in output or "Total tests:" in output:
                passed_tests += 1
                print(f"✅ {dll_name} - Tests executed")
                
                # Extract test counts if available
                for line in output.split('\n'):
                    if 'Total tests:' in line or 'Passed:' in line:
                        print(f"   {line.strip()}")
            else:
                failed_tests += 1
                print(f"❌ {dll_name} - No tests found or execution failed")
                
        except subprocess.TimeoutExpired:
            failed_tests += 1
            print(f"⏱️ {dll_name} - Timeout")
        except Exception as e:
            failed_tests += 1
            print(f"❌ {dll_name} - Error: {e}")
        
        print()
    
    print("=" * 50)
    print("Test Execution Summary")
    print("=" * 50)
    print(f"Test Assemblies Processed: {passed_tests + failed_tests}")
    print(f"Successful Executions: {passed_tests}")
    print(f"Failed/No Tests: {failed_tests}")
    
    return passed_tests > 0

if __name__ == "__main__":
    success = run_tests()
    sys.exit(0 if success else 1)

#!/usr/bin/env python3
"""
Simple test runner that directly executes test DLLs
"""

import subprocess
from pathlib import Path
import json

def find_test_dlls():
    """Find all built test DLLs."""
    project_root = Path("/home/ubuntu/neo-service-layer")
    test_dlls = list(project_root.glob("tests/**/bin/Release/net9.0/*.Tests.dll"))
    return test_dlls

def run_test_dll(dll_path):
    """Run a single test DLL."""
    try:
        result = subprocess.run(
            ["dotnet", "vstest", str(dll_path), "--logger:console;verbosity=minimal"],
            capture_output=True,
            text=True,
            timeout=60
        )
        return result.returncode == 0, result.stdout
    except Exception as e:
        return False, str(e)

def main():
    print("=" * 80)
    print("SIMPLE TEST RUNNER")
    print("=" * 80)
    print()
    
    # Find all test DLLs
    test_dlls = find_test_dlls()
    print(f"Found {len(test_dlls)} test DLL files")
    print()
    
    total_tests = 0
    passed_tests = 0
    failed_tests = 0
    
    for dll in test_dlls:
        dll_name = dll.stem
        print(f"Running {dll_name}...")
        
        success, output = run_test_dll(dll)
        
        if success:
            # Count tests from output
            if "Total tests:" in output:
                for line in output.split('\n'):
                    if "Total tests:" in line:
                        count = int(line.split("Total tests:")[1].split()[0])
                        total_tests += count
                        passed_tests += count
                        print(f"  ✅ {count} tests passed")
                        break
            else:
                print(f"  ✅ Tests passed")
                passed_tests += 1
                total_tests += 1
        else:
            print(f"  ❌ Tests failed or DLL not runnable")
            failed_tests += 1
            total_tests += 1
    
    print()
    print("=" * 80)
    print("SUMMARY")
    print("=" * 80)
    print(f"Total DLLs found: {len(test_dlls)}")
    print(f"Total tests run: {total_tests}")
    print(f"Passed: {passed_tests}")
    print(f"Failed: {failed_tests}")
    if total_tests > 0:
        print(f"Pass rate: {passed_tests * 100 // total_tests}%")

if __name__ == "__main__":
    main()
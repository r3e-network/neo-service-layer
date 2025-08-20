#!/usr/bin/env python3
"""
Final comprehensive test enabler - uses working test execution method
"""

import subprocess
import os
from pathlib import Path
import json
import shutil

class FinalTestEnabler:
    def __init__(self):
        self.project_root = Path("/home/ubuntu/neo-service-layer")
        os.chdir(self.project_root)
        self.working_dlls = []
        self.failed_projects = []
        
    def find_existing_test_dlls(self):
        """Find all test DLLs that already exist"""
        dlls = []
        for dll in self.project_root.glob("tests/**/bin/Release/net9.0/*.Tests.dll"):
            if "/obj/" not in str(dll):
                dlls.append(dll)
        return sorted(set(dlls))
    
    def copy_working_dlls(self):
        """Copy working test DLLs from various locations"""
        # Known working DLLs from our previous runs
        working_patterns = [
            "tests/**/bin/Release/net9.0/NeoServiceLayer.*.Tests.dll",
            "tests/**/bin/Debug/net9.0/NeoServiceLayer.*.Tests.dll"
        ]
        
        found_dlls = set()
        for pattern in working_patterns:
            for dll in self.project_root.glob(pattern):
                if "/obj/" not in str(dll):
                    found_dlls.add(dll)
        
        return sorted(found_dlls)
    
    def test_dll(self, dll_path):
        """Test a DLL using the working vstest method"""
        try:
            result = subprocess.run(
                ["dotnet", "vstest", str(dll_path), "--logger:console;verbosity=quiet"],
                capture_output=True,
                text=True,
                timeout=30
            )
            
            if "Passed!" in result.stdout:
                import re
                match = re.search(r"Total:\s*(\d+)", result.stdout)
                if match:
                    return True, int(match.group(1)), None
                return True, 0, None
            elif "Failed!" in result.stdout:
                import re
                failed_match = re.search(r"Failed:\s*(\d+)", result.stdout)
                total_match = re.search(r"Total:\s*(\d+)", result.stdout)
                if failed_match and total_match:
                    return False, int(total_match.group(1)), f"{failed_match.group(1)} failures"
            
            return False, 0, "No tests found"
        except Exception as e:
            return False, 0, str(e)
    
    def run_comprehensive_test(self):
        """Run comprehensive test of all available DLLs"""
        print("=" * 80)
        print("COMPREHENSIVE TEST EXECUTION - ALL AVAILABLE DLLS")
        print("=" * 80)
        print()
        
        # Find all test DLLs
        all_dlls = self.copy_working_dlls()
        print(f"Found {len(all_dlls)} test DLLs")
        print()
        
        # Remove duplicates by name
        unique_dlls = {}
        for dll in all_dlls:
            if dll.name not in unique_dlls:
                unique_dlls[dll.name] = dll
        
        print(f"Testing {len(unique_dlls)} unique test assemblies")
        print()
        
        total_tests = 0
        passed_assemblies = 0
        failed_assemblies = 0
        
        results = []
        
        for i, (dll_name, dll_path) in enumerate(sorted(unique_dlls.items()), 1):
            print(f"[{i:2d}/{len(unique_dlls)}] {dll_name}")
            
            success, test_count, error = self.test_dll(dll_path)
            
            if success:
                print(f"  ✅ PASSED - {test_count} tests")
                total_tests += test_count
                passed_assemblies += 1
                results.append({
                    "name": dll_name,
                    "path": str(dll_path),
                    "tests": test_count,
                    "status": "passed"
                })
            elif test_count > 0:
                print(f"  ❌ FAILED - {test_count} tests, {error}")
                total_tests += test_count
                failed_assemblies += 1
                results.append({
                    "name": dll_name,
                    "path": str(dll_path),
                    "tests": test_count,
                    "status": "failed",
                    "error": error
                })
            else:
                print(f"  ⚠️ NO TESTS or ERROR - {error}")
                results.append({
                    "name": dll_name,
                    "path": str(dll_path),
                    "tests": 0,
                    "status": "error",
                    "error": error
                })
            
            print()
        
        # Print summary
        print("=" * 80)
        print("COMPREHENSIVE TEST SUMMARY")
        print("=" * 80)
        print(f"Total Assemblies Tested: {len(unique_dlls)}")
        print(f"Passed Assemblies: {passed_assemblies}")
        print(f"Failed Assemblies: {failed_assemblies}")
        print(f"Total Tests Executed: {total_tests}")
        
        if passed_assemblies > 0:
            print(f"Overall Pass Rate: {passed_assemblies/len(unique_dlls)*100:.1f}%")
        
        print()
        
        # Save results
        self.save_results(results)
        
        return results
    
    def save_results(self, results):
        """Save test results to file"""
        summary = {
            "timestamp": str(Path.cwd()),
            "total_assemblies": len(results),
            "passed_assemblies": len([r for r in results if r["status"] == "passed"]),
            "failed_assemblies": len([r for r in results if r["status"] == "failed"]),
            "error_assemblies": len([r for r in results if r["status"] == "error"]),
            "total_tests": sum(r["tests"] for r in results),
            "assemblies": results
        }
        
        with open(self.project_root / "comprehensive-test-results.json", "w") as f:
            json.dump(summary, f, indent=2)
        
        print(f"📄 Results saved to comprehensive-test-results.json")

if __name__ == "__main__":
    enabler = FinalTestEnabler()
    enabler.run_comprehensive_test()
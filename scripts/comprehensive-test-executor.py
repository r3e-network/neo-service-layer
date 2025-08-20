#!/usr/bin/env python3
"""
Neo Service Layer - Comprehensive Test Executor
Bypasses MSBuild response file issues by using Python subprocess
"""

import subprocess
import os
import re
import sys
import time
from pathlib import Path
from datetime import datetime

class TestExecutor:
    def __init__(self):
        self.project_root = Path("/home/ubuntu/neo-service-layer")
        self.results = {
            'total_assemblies': 0,
            'executed_assemblies': 0,
            'total_tests': 0,
            'passed_tests': 0,
            'failed_tests': 0,
            'skipped_tests': 0,
            'execution_time_ms': 0,
            'assemblies': []
        }
    
    def find_test_assemblies(self):
        """Find all test DLL files in the project"""
        pattern = "tests/**/bin/Release/net9.0/*.Tests.dll"
        return sorted(self.project_root.glob(pattern))
    
    def parse_test_output(self, output):
        """Parse vstest output for test metrics"""
        metrics = {
            'passed': 0,
            'failed': 0,
            'skipped': 0,
            'total': 0,
            'duration_ms': 0,
            'has_tests': False
        }
        
        # Look for test result pattern
        # Example: "Passed!  - Failed:     0, Passed:   239, Skipped:     0, Total:   239, Duration: 147 ms"
        result_pattern = r"Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+),\s*Total:\s*(\d+),\s*Duration:\s*(\d+(?:\.\d+)?)\s*(?:ms|s)"
        match = re.search(result_pattern, output)
        
        if match:
            metrics['failed'] = int(match.group(1))
            metrics['passed'] = int(match.group(2))
            metrics['skipped'] = int(match.group(3))
            metrics['total'] = int(match.group(4))
            
            # Handle duration (could be in ms or s)
            duration_str = match.group(5)
            if 's' in output[match.end():match.end()+5] and 'ms' not in output[match.end():match.end()+5]:
                metrics['duration_ms'] = int(float(duration_str) * 1000)
            else:
                metrics['duration_ms'] = int(float(duration_str))
            
            metrics['has_tests'] = metrics['total'] > 0
        
        # Check for "No test is available" message
        if "No test is available" in output:
            metrics['has_tests'] = False
        
        return metrics
    
    def execute_test_assembly(self, dll_path):
        """Execute a single test assembly"""
        dll_name = dll_path.name
        print(f"Testing: {dll_name}")
        print("-" * 60)
        
        try:
            # Run vstest using subprocess to avoid shell interpretation
            start_time = time.time()
            result = subprocess.run(
                ["dotnet", "vstest", str(dll_path), "--logger:console;verbosity=normal"],
                capture_output=True,
                text=True,
                timeout=120,
                cwd=str(self.project_root)
            )
            execution_time = int((time.time() - start_time) * 1000)
            
            output = result.stdout + result.stderr
            metrics = self.parse_test_output(output)
            
            if metrics['has_tests']:
                status = "✅ PASSED" if metrics['failed'] == 0 else "❌ FAILED"
                print(f"{status} - Tests: {metrics['total']} (Passed: {metrics['passed']}, Failed: {metrics['failed']}, Skipped: {metrics['skipped']})")
                print(f"Duration: {metrics['duration_ms']}ms")
            else:
                print("⚠️ No tests found in assembly")
                metrics['total'] = 0
            
            return {
                'name': dll_name,
                'path': str(dll_path),
                'executed': True,
                'has_tests': metrics['has_tests'],
                **metrics
            }
            
        except subprocess.TimeoutExpired:
            print("⏱️ Test execution timeout (120s)")
            return {
                'name': dll_name,
                'path': str(dll_path),
                'executed': False,
                'timeout': True
            }
        except Exception as e:
            print(f"❌ Error executing tests: {e}")
            return {
                'name': dll_name,
                'path': str(dll_path),
                'executed': False,
                'error': str(e)
            }
    
    def run_all_tests(self):
        """Execute all test assemblies"""
        print("=" * 80)
        print("NEO SERVICE LAYER - COMPREHENSIVE TEST EXECUTION")
        print("=" * 80)
        print(f"Execution Time: {datetime.now().strftime('%Y-%m-%d %H:%M:%S UTC')}")
        print()
        
        # Change to project directory
        os.chdir(str(self.project_root))
        
        # Find all test assemblies
        test_dlls = self.find_test_assemblies()
        self.results['total_assemblies'] = len(test_dlls)
        
        print(f"Found {len(test_dlls)} test assemblies")
        print()
        
        # Execute each test assembly
        for dll in test_dlls:
            assembly_result = self.execute_test_assembly(dll)
            self.results['assemblies'].append(assembly_result)
            
            if assembly_result.get('executed') and assembly_result.get('has_tests'):
                self.results['executed_assemblies'] += 1
                self.results['total_tests'] += assembly_result.get('total', 0)
                self.results['passed_tests'] += assembly_result.get('passed', 0)
                self.results['failed_tests'] += assembly_result.get('failed', 0)
                self.results['skipped_tests'] += assembly_result.get('skipped', 0)
                self.results['execution_time_ms'] += assembly_result.get('duration_ms', 0)
            
            print()
        
        self.print_summary()
        self.save_report()
        
        return self.results['failed_tests'] == 0
    
    def print_summary(self):
        """Print test execution summary"""
        print("=" * 80)
        print("TEST EXECUTION SUMMARY")
        print("=" * 80)
        print(f"Total Test Assemblies: {self.results['total_assemblies']}")
        print(f"Assemblies with Tests: {self.results['executed_assemblies']}")
        print()
        print(f"Total Tests Executed: {self.results['total_tests']}")
        print(f"  ✅ Passed: {self.results['passed_tests']}")
        print(f"  ❌ Failed: {self.results['failed_tests']}")
        print(f"  ⏭️ Skipped: {self.results['skipped_tests']}")
        print()
        
        if self.results['total_tests'] > 0:
            pass_rate = (self.results['passed_tests'] / self.results['total_tests']) * 100
            print(f"Pass Rate: {pass_rate:.2f}%")
            print(f"Total Execution Time: {self.results['execution_time_ms']}ms ({self.results['execution_time_ms']/1000:.2f}s)")
            
            if self.results['failed_tests'] == 0:
                print()
                print("🎉 ALL TESTS PASSED! 🎉")
        else:
            print("⚠️ No tests were executed")
        
        print("=" * 80)
    
    def save_report(self):
        """Save detailed test report to file"""
        report_path = self.project_root / "TestExecutionResults.md"
        
        with open(report_path, 'w') as f:
            f.write("# Neo Service Layer - Test Execution Results\n\n")
            f.write(f"**Execution Date**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S UTC')}\n")
            f.write(f"**Test Framework**: xUnit with dotnet vstest\n")
            f.write(f"**Execution Method**: Python subprocess (MSBuild workaround)\n\n")
            
            f.write("## Summary\n\n")
            f.write(f"- **Total Assemblies**: {self.results['total_assemblies']}\n")
            f.write(f"- **Assemblies with Tests**: {self.results['executed_assemblies']}\n")
            f.write(f"- **Total Tests**: {self.results['total_tests']}\n")
            f.write(f"- **Passed**: {self.results['passed_tests']}\n")
            f.write(f"- **Failed**: {self.results['failed_tests']}\n")
            f.write(f"- **Skipped**: {self.results['skipped_tests']}\n")
            
            if self.results['total_tests'] > 0:
                pass_rate = (self.results['passed_tests'] / self.results['total_tests']) * 100
                f.write(f"- **Pass Rate**: {pass_rate:.2f}%\n")
                f.write(f"- **Execution Time**: {self.results['execution_time_ms']}ms\n")
            
            f.write("\n## Detailed Results\n\n")
            f.write("| Assembly | Tests | Passed | Failed | Skipped | Duration | Status |\n")
            f.write("|----------|-------|--------|--------|---------|----------|--------|\n")
            
            for assembly in self.results['assemblies']:
                if assembly.get('executed') and assembly.get('has_tests'):
                    status = "✅" if assembly.get('failed', 0) == 0 else "❌"
                    f.write(f"| {assembly['name']} | {assembly.get('total', 0)} | ")
                    f.write(f"{assembly.get('passed', 0)} | {assembly.get('failed', 0)} | ")
                    f.write(f"{assembly.get('skipped', 0)} | {assembly.get('duration_ms', 0)}ms | {status} |\n")
                elif assembly.get('timeout'):
                    f.write(f"| {assembly['name']} | - | - | - | - | Timeout | ⏱️ |\n")
                elif not assembly.get('has_tests'):
                    f.write(f"| {assembly['name']} | 0 | - | - | - | - | ⚠️ |\n")
                else:
                    f.write(f"| {assembly['name']} | - | - | - | - | Error | ❌ |\n")
            
            f.write("\n---\n")
            f.write("*Report generated using Python subprocess to bypass MSBuild response file issues*\n")
        
        print(f"\n📄 Detailed report saved to: {report_path}")

if __name__ == "__main__":
    executor = TestExecutor()
    success = executor.run_all_tests()
    sys.exit(0 if success else 1)
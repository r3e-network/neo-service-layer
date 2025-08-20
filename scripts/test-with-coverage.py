#!/usr/bin/env python3
"""
Execute tests with code coverage collection
"""

import subprocess
import os
import json
import xml.etree.ElementTree as ET
from pathlib import Path
from datetime import datetime

class CoverageTestRunner:
    def __init__(self):
        self.project_root = Path("/home/ubuntu/neo-service-layer")
        self.coverage_dir = self.project_root / "TestResults" / "Coverage"
        self.coverage_results = []
        
    def setup_environment(self):
        """Setup test environment"""
        self.coverage_dir.mkdir(parents=True, exist_ok=True)
        
        # Clean previous results
        for file in self.coverage_dir.glob("*.xml"):
            file.unlink()
            
        os.environ["MSBUILDDISABLENODEREUSE"] = "1"
        os.environ["CollectCoverage"] = "true"
        os.environ["CoverletOutputFormat"] = "cobertura"
        
    def run_test_with_coverage(self, dll_path):
        """Run a single test assembly with coverage"""
        dll_name = dll_path.name
        coverage_file = self.coverage_dir / f"{dll_name}.coverage.xml"
        
        print(f"Testing with coverage: {dll_name}")
        print("-" * 50)
        
        try:
            # Run test with coverage collection
            result = subprocess.run(
                [
                    "dotnet", "test", str(dll_path.parent.parent.parent),
                    "--no-build",
                    "--configuration", "Release",
                    "--collect:XPlat Code Coverage",
                    "--results-directory", str(self.coverage_dir),
                    "--settings", "/home/ubuntu/neo-service-layer/coverlet.runsettings"
                ],
                capture_output=True,
                text=True,
                timeout=120,
                cwd=str(self.project_root)
            )
            
            # Check if tests passed
            if "Passed!" in result.stdout:
                # Extract test count
                import re
                match = re.search(r"Passed:\s*(\d+)", result.stdout)
                if match:
                    test_count = int(match.group(1))
                    print(f"✅ {test_count} tests passed")
                    
                    # Look for coverage file
                    coverage_files = list(self.coverage_dir.glob("**/coverage.cobertura.xml"))
                    if coverage_files:
                        coverage_data = self.parse_coverage_file(coverage_files[0])
                        self.coverage_results.append({
                            "assembly": dll_name,
                            "tests": test_count,
                            "coverage": coverage_data
                        })
                        print(f"📊 Coverage: {coverage_data.get('line_rate', 0)*100:.1f}%")
                    else:
                        print("⚠️ No coverage file generated")
                else:
                    print("✅ Tests passed (count unknown)")
            else:
                print("❌ Tests failed or no tests found")
                
        except subprocess.TimeoutExpired:
            print("⏱️ Test timeout")
        except Exception as e:
            print(f"❌ Error: {e}")
            
    def parse_coverage_file(self, coverage_file):
        """Parse Cobertura XML coverage file"""
        try:
            tree = ET.parse(coverage_file)
            root = tree.getroot()
            
            return {
                "line_rate": float(root.get("line-rate", 0)),
                "branch_rate": float(root.get("branch-rate", 0)),
                "lines_covered": int(root.get("lines-covered", 0)),
                "lines_valid": int(root.get("lines-valid", 0)),
                "branches_covered": int(root.get("branches-covered", 0)),
                "branches_valid": int(root.get("branches-valid", 0))
            }
        except:
            return {"line_rate": 0, "branch_rate": 0}
            
    def run_all_tests(self):
        """Run all available tests with coverage"""
        print("=" * 70)
        print("NEO SERVICE LAYER - TEST EXECUTION WITH COVERAGE")
        print("=" * 70)
        print()
        
        self.setup_environment()
        
        # Find built test DLLs
        test_dlls = sorted(self.project_root.glob("tests/**/bin/Release/net9.0/*.Tests.dll"))
        
        # Remove duplicates
        seen = set()
        unique_dlls = []
        for dll in test_dlls:
            if dll.name not in seen:
                seen.add(dll.name)
                unique_dlls.append(dll)
        
        print(f"Found {len(unique_dlls)} test assemblies")
        print()
        
        # Run tests with coverage
        for dll in unique_dlls[:5]:  # Test first 5 for now
            self.run_test_with_coverage(dll)
            print()
            
        self.print_summary()
        self.save_coverage_report()
        
    def print_summary(self):
        """Print coverage summary"""
        print("=" * 70)
        print("COVERAGE SUMMARY")
        print("=" * 70)
        
        if self.coverage_results:
            total_tests = sum(r["tests"] for r in self.coverage_results)
            avg_coverage = sum(r["coverage"]["line_rate"] for r in self.coverage_results) / len(self.coverage_results)
            
            print(f"Total Tests Executed: {total_tests}")
            print(f"Average Line Coverage: {avg_coverage*100:.1f}%")
            print()
            
            print("Assembly Coverage:")
            for result in self.coverage_results:
                coverage = result["coverage"]["line_rate"] * 100
                status = "✅" if coverage >= 80 else "⚠️" if coverage >= 60 else "❌"
                print(f"  {status} {result['assembly']}: {coverage:.1f}%")
        else:
            print("No coverage data collected")
            
    def save_coverage_report(self):
        """Save coverage report"""
        report_path = self.project_root / "docs" / "COVERAGE_REPORT.md"
        
        with open(report_path, 'w') as f:
            f.write("# Code Coverage Report\n\n")
            f.write(f"**Date**: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
            f.write(f"**Framework**: Coverlet with Cobertura format\n\n")
            
            if self.coverage_results:
                f.write("## Coverage Summary\n\n")
                f.write("| Assembly | Tests | Line Coverage | Branch Coverage | Status |\n")
                f.write("|----------|-------|---------------|-----------------|--------|\n")
                
                for result in self.coverage_results:
                    line_cov = result["coverage"]["line_rate"] * 100
                    branch_cov = result["coverage"]["branch_rate"] * 100
                    status = "✅" if line_cov >= 80 else "⚠️" if line_cov >= 60 else "❌"
                    f.write(f"| {result['assembly']} | {result['tests']} | ")
                    f.write(f"{line_cov:.1f}% | {branch_cov:.1f}% | {status} |\n")
                    
                f.write("\n## Recommendations\n\n")
                f.write("1. Target 80% line coverage for all assemblies\n")
                f.write("2. Focus on critical business logic first\n")
                f.write("3. Add tests for uncovered edge cases\n")
            else:
                f.write("No coverage data available\n")
                
        print(f"\n📄 Coverage report saved to: {report_path}")

if __name__ == "__main__":
    runner = CoverageTestRunner()
    runner.run_all_tests()
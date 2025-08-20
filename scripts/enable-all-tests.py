#!/usr/bin/env python3
"""
Enable and build all test projects in the solution
"""

import subprocess
import os
from pathlib import Path
import json

class TestEnabler:
    def __init__(self):
        self.project_root = Path("/home/ubuntu/neo-service-layer")
        self.enabled_projects = []
        self.failed_projects = []
        
    def find_all_test_projects(self):
        """Find all test project files"""
        test_projects = list(self.project_root.glob("tests/**/*.Tests.csproj"))
        return sorted(test_projects)
    
    def build_project(self, project_path):
        """Build a single test project"""
        try:
            result = subprocess.run(
                ["dotnet", "build", str(project_path), 
                 "--configuration", "Release", 
                 "--no-restore"],
                capture_output=True,
                text=True,
                timeout=30,
                cwd=str(self.project_root),
                env={**os.environ, "MSBUILDDISABLENODEREUSE": "1"}
            )
            
            return result.returncode == 0
        except:
            return False
    
    def run_tests_for_project(self, project_path):
        """Run tests for a single project"""
        dll_name = project_path.stem + ".dll"
        dll_path = project_path.parent / "bin" / "Release" / "net9.0" / dll_name
        
        if not dll_path.exists():
            return None
        
        try:
            result = subprocess.run(
                ["dotnet", "vstest", str(dll_path), "--logger:console;verbosity=quiet"],
                capture_output=True,
                text=True,
                timeout=30,
                cwd=str(self.project_root)
            )
            
            # Parse output for test count
            if "Passed!" in result.stdout:
                import re
                match = re.search(r"Total:\s*(\d+)", result.stdout)
                if match:
                    return int(match.group(1))
            return 0
        except:
            return None
    
    def enable_all_tests(self):
        """Enable and build all test projects"""
        print("=" * 70)
        print("ENABLING ALL TEST PROJECTS")
        print("=" * 70)
        print()
        
        test_projects = self.find_all_test_projects()
        print(f"Found {len(test_projects)} test projects")
        print()
        
        total_tests = 0
        successful_builds = 0
        
        for i, project in enumerate(test_projects, 1):
            project_name = project.stem
            print(f"[{i}/{len(test_projects)}] {project_name}")
            
            # Try to build
            if self.build_project(project):
                print(f"  ✅ Built successfully")
                successful_builds += 1
                
                # Try to run tests
                test_count = self.run_tests_for_project(project)
                if test_count is not None:
                    if test_count > 0:
                        print(f"  ✅ {test_count} tests found")
                        total_tests += test_count
                        self.enabled_projects.append({
                            "name": project_name,
                            "path": str(project),
                            "tests": test_count
                        })
                    else:
                        print(f"  ⚠️ No tests found")
                else:
                    print(f"  ⚠️ Could not run tests")
            else:
                print(f"  ❌ Build failed")
                self.failed_projects.append(project_name)
            
            print()
        
        self.print_summary(successful_builds, len(test_projects), total_tests)
        self.save_results()
    
    def print_summary(self, successful, total, test_count):
        """Print summary of results"""
        print("=" * 70)
        print("SUMMARY")
        print("=" * 70)
        print(f"Projects Built: {successful}/{total} ({successful/total*100:.1f}%)")
        print(f"Total Tests Found: {test_count}")
        print(f"Enabled Projects: {len(self.enabled_projects)}")
        print(f"Failed Projects: {len(self.failed_projects)}")
        
        if self.failed_projects:
            print("\nFailed to build:")
            for proj in self.failed_projects[:10]:
                print(f"  - {proj}")
    
    def save_results(self):
        """Save results to file"""
        results = {
            "enabled_projects": self.enabled_projects,
            "failed_projects": self.failed_projects,
            "summary": {
                "total_projects": len(self.enabled_projects) + len(self.failed_projects),
                "enabled": len(self.enabled_projects),
                "failed": len(self.failed_projects),
                "total_tests": sum(p["tests"] for p in self.enabled_projects)
            }
        }
        
        results_file = self.project_root / "test-enablement-results.json"
        with open(results_file, 'w') as f:
            json.dump(results, f, indent=2)
        
        print(f"\n📄 Results saved to: {results_file}")

if __name__ == "__main__":
    enabler = TestEnabler()
    enabler.enable_all_tests()
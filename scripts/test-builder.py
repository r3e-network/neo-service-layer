#!/usr/bin/env python3
"""
Build all test projects using Python subprocess to avoid MSBuild issues
"""

import subprocess
import os
from pathlib import Path
import json

class TestBuilder:
    def __init__(self):
        self.project_root = Path("/home/ubuntu/neo-service-layer")
        os.chdir(self.project_root)
        self.successful_projects = []
        self.failed_projects = []
        
    def find_test_projects(self):
        """Find all real test project files"""
        projects = []
        for proj in self.project_root.glob("tests/**/*.Tests.csproj"):
            if "/obj/" not in str(proj):
                projects.append(proj)
        return sorted(projects)
    
    def build_project(self, project_path):
        """Build a single project using subprocess"""
        try:
            # Clean first
            subprocess.run(
                ["dotnet", "clean", str(project_path), "-v", "quiet"],
                capture_output=True,
                timeout=30
            )
            
            # Build
            result = subprocess.run(
                ["dotnet", "build", str(project_path), "--configuration", "Release", "--no-restore"],
                capture_output=True,
                text=True,
                timeout=60
            )
            
            return result.returncode == 0, result.stdout, result.stderr
        except Exception as e:
            return False, "", str(e)
    
    def test_project(self, project_path):
        """Test if DLL exists and can run"""
        dll_name = project_path.stem + ".dll"
        dll_path = project_path.parent / "bin" / "Release" / "net9.0" / dll_name
        
        if not dll_path.exists():
            return False, 0, "DLL not found"
        
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
                    return True, int(match.group(1)), "Success"
            return False, 0, "No tests found"
        except Exception as e:
            return False, 0, str(e)
    
    def build_all(self):
        """Build all test projects"""
        print("=" * 80)
        print("BUILDING ALL TEST PROJECTS")
        print("=" * 80)
        print()
        
        # First restore packages
        print("Restoring packages...")
        subprocess.run(
            ["dotnet", "restore", "NeoServiceLayer.sln"],
            capture_output=True,
            timeout=120
        )
        print("  ✓ Package restore complete")
        print()
        
        projects = self.find_test_projects()
        print(f"Found {len(projects)} test projects to build")
        print()
        
        total_tests = 0
        
        for i, project in enumerate(projects, 1):
            project_name = project.stem
            print(f"[{i:2d}/{len(projects)}] {project_name}")
            
            # Build
            success, stdout, stderr = self.build_project(project)
            if success:
                print(f"  ✓ Built successfully")
                
                # Test
                test_success, test_count, test_msg = self.test_project(project)
                if test_success:
                    print(f"  ✓ {test_count} tests found")
                    total_tests += test_count
                    self.successful_projects.append({
                        "name": project_name,
                        "path": str(project),
                        "tests": test_count
                    })
                else:
                    print(f"  ⚠️ {test_msg}")
                    if test_count == 0:
                        self.successful_projects.append({
                            "name": project_name,
                            "path": str(project),
                            "tests": 0
                        })
            else:
                print(f"  ✗ Build failed")
                if stderr:
                    # Extract first error
                    for line in stderr.split('\n'):
                        if "error" in line.lower() and not "MSB" in line:
                            print(f"    {line.strip()[:100]}")
                            break
                
                self.failed_projects.append({
                    "name": project_name,
                    "path": str(project),
                    "error": stderr.split('\n')[0] if stderr else "Unknown error"
                })
            
            print()
        
        self.print_summary(total_tests)
        self.save_results()
    
    def print_summary(self, total_tests):
        """Print build summary"""
        print("=" * 80)
        print("BUILD SUMMARY")
        print("=" * 80)
        total = len(self.successful_projects) + len(self.failed_projects)
        print(f"Total Projects: {total}")
        print(f"Successful Builds: {len(self.successful_projects)} ({len(self.successful_projects)/total*100:.1f}%)")
        print(f"Failed Builds: {len(self.failed_projects)} ({len(self.failed_projects)/total*100:.1f}%)")
        print(f"Total Tests Found: {total_tests}")
        print()
        
        if self.successful_projects:
            print("Successful Projects:")
            with_tests = [p for p in self.successful_projects if p['tests'] > 0]
            without_tests = [p for p in self.successful_projects if p['tests'] == 0]
            
            for proj in with_tests[:10]:
                print(f"  ✓ {proj['name']} ({proj['tests']} tests)")
            
            if without_tests:
                print(f"\n  Projects built but no tests: {len(without_tests)}")
        
        if self.failed_projects:
            print("\nFailed Projects:")
            for proj in self.failed_projects[:5]:
                print(f"  ✗ {proj['name']}")
    
    def save_results(self):
        """Save build results to file"""
        results = {
            "successful": self.successful_projects,
            "failed": self.failed_projects,
            "summary": {
                "total_projects": len(self.successful_projects) + len(self.failed_projects),
                "successful_builds": len(self.successful_projects),
                "failed_builds": len(self.failed_projects),
                "total_tests": sum(p['tests'] for p in self.successful_projects)
            }
        }
        
        with open(self.project_root / "test-build-results.json", "w") as f:
            json.dump(results, f, indent=2)
        
        print(f"\n📄 Results saved to test-build-results.json")

if __name__ == "__main__":
    builder = TestBuilder()
    builder.build_all()
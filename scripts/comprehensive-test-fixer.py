#!/usr/bin/env python3
"""
Comprehensive test project fixer - ensures all projects build and run
"""

from pathlib import Path
import subprocess
import os
import shutil

class ComprehensiveTestFixer:
    def __init__(self):
        self.project_root = Path("/home/ubuntu/neo-service-layer")
        self.fixed_projects = []
        self.failed_projects = []
        
    def clean_coverage_directories(self):
        """Remove all coverage-related directories"""
        print("Cleaning up coverage directories...")
        
        # Find and remove coverage directories
        patterns = [
            "**/obj/**/.msCoverageSourceRootsMapping_*",
            "**/obj/**/CoverletSourceRootsMapping_*",
            "**/.msCoverageSourceRootsMapping_*", 
            "**/CoverletSourceRootsMapping_*"
        ]
        
        removed_count = 0
        for pattern in patterns:
            for path in self.project_root.glob(pattern):
                if path.is_dir():
                    try:
                        shutil.rmtree(path)
                        removed_count += 1
                    except:
                        pass
        
        print(f"  Removed {removed_count} coverage directories")
        return removed_count
    
    def find_real_test_projects(self):
        """Find only real test project directories"""
        test_dirs = []
        
        # Look for directories ending in .Tests that are not in obj folders
        for test_dir in self.project_root.glob("tests/**/*.Tests"):
            # Skip if it's in obj directory or is a coverage mapping
            if "/obj/" not in str(test_dir) and \
               not test_dir.name.startswith(".msCoverage") and \
               not test_dir.name.startswith("CoverletSource"):
                test_dirs.append(test_dir)
        
        return sorted(set(test_dirs))
    
    def restore_packages(self):
        """Restore all NuGet packages"""
        print("Restoring NuGet packages...")
        try:
            result = subprocess.run(
                ["dotnet", "restore"],
                capture_output=True,
                text=True,
                timeout=60,
                cwd=str(self.project_root),
                env={**os.environ, "MSBUILDDISABLENODEREUSE": "1"}
            )
            
            if result.returncode == 0:
                print("  ✓ Packages restored successfully")
                return True
            else:
                print("  ✗ Package restore failed")
                return False
        except Exception as e:
            print(f"  ✗ Error: {e}")
            return False
    
    def build_project(self, project_path):
        """Build a single test project"""
        try:
            # Clean first
            subprocess.run(
                ["dotnet", "clean", str(project_path), "-v", "quiet"],
                capture_output=True,
                timeout=30,
                cwd=str(self.project_root),
                env={**os.environ, "MSBUILDDISABLENODEREUSE": "1"}
            )
            
            # Then build
            result = subprocess.run(
                ["dotnet", "build", str(project_path), 
                 "--configuration", "Release"],
                capture_output=True,
                text=True,
                timeout=60,
                cwd=str(self.project_root),
                env={**os.environ, "MSBUILDDISABLENODEREUSE": "1"}
            )
            
            return result.returncode == 0, result.stderr
        except Exception as e:
            return False, str(e)
    
    def test_project(self, project_path):
        """Run tests for a project"""
        dll_name = project_path.stem + ".dll"
        dll_path = project_path.parent / "bin" / "Release" / "net9.0" / dll_name
        
        if not dll_path.exists():
            return None, "DLL not found"
        
        try:
            result = subprocess.run(
                ["dotnet", "vstest", str(dll_path), "--logger:console;verbosity=quiet"],
                capture_output=True,
                text=True,
                timeout=30,
                cwd=str(self.project_root)
            )
            
            if "Passed!" in result.stdout:
                # Extract test count
                import re
                match = re.search(r"Total:\s*(\d+)", result.stdout)
                if match:
                    return int(match.group(1)), None
            return 0, "No tests found"
        except Exception as e:
            return None, str(e)
    
    def fix_all_projects(self):
        """Fix all test projects"""
        print("=" * 80)
        print("COMPREHENSIVE TEST PROJECT FIX")
        print("=" * 80)
        print()
        
        # Step 1: Clean coverage directories
        self.clean_coverage_directories()
        print()
        
        # Step 2: Find real test projects
        test_projects = self.find_real_test_projects()
        print(f"Found {len(test_projects)} real test projects")
        print()
        
        # Step 3: Restore packages
        self.restore_packages()
        print()
        
        # Step 4: Build and test each project
        total_tests = 0
        successful_builds = 0
        successful_tests = 0
        
        for i, test_dir in enumerate(test_projects, 1):
            project_name = test_dir.name
            csproj_path = test_dir / f"{project_name}.csproj"
            
            print(f"[{i:2d}/{len(test_projects)}] {project_name}")
            
            if not csproj_path.exists():
                print(f"  ✗ Missing .csproj file")
                self.failed_projects.append({
                    "name": project_name,
                    "reason": "Missing .csproj"
                })
                print()
                continue
            
            # Try to build
            success, error = self.build_project(csproj_path)
            if success:
                print(f"  ✓ Built successfully")
                successful_builds += 1
                
                # Try to run tests
                test_count, test_error = self.test_project(csproj_path)
                if test_count is not None and test_count > 0:
                    print(f"  ✓ {test_count} tests found and passing")
                    total_tests += test_count
                    successful_tests += 1
                    self.fixed_projects.append({
                        "name": project_name,
                        "tests": test_count,
                        "path": str(csproj_path)
                    })
                elif test_count == 0:
                    print(f"  ⚠️ No tests found")
                else:
                    print(f"  ✗ Test execution failed: {test_error}")
                    self.failed_projects.append({
                        "name": project_name,
                        "reason": f"Test failed: {test_error}"
                    })
            else:
                print(f"  ✗ Build failed")
                # Show first error
                if error:
                    lines = error.split('\n')
                    for line in lines[:2]:
                        if line.strip() and not line.startswith("MSBUILD"):
                            print(f"    Error: {line.strip()[:100]}")
                
                self.failed_projects.append({
                    "name": project_name,
                    "reason": "Build failed"
                })
            
            print()
        
        # Print summary
        self.print_summary(successful_builds, successful_tests, len(test_projects), total_tests)
    
    def print_summary(self, builds, tests, total, test_count):
        """Print summary of results"""
        print("=" * 80)
        print("SUMMARY")
        print("=" * 80)
        print(f"Total Projects: {total}")
        print(f"Successful Builds: {builds} ({builds/total*100:.1f}%)")
        print(f"Projects with Tests: {tests} ({tests/total*100:.1f}%)")
        print(f"Total Tests: {test_count}")
        print()
        
        if self.fixed_projects:
            print("Successfully Fixed Projects:")
            for proj in self.fixed_projects[:10]:
                print(f"  ✓ {proj['name']} ({proj['tests']} tests)")
            if len(self.fixed_projects) > 10:
                print(f"  ... and {len(self.fixed_projects) - 10} more")
        
        if self.failed_projects:
            print("\nFailed Projects:")
            for proj in self.failed_projects[:10]:
                print(f"  ✗ {proj['name']}: {proj['reason']}")
            if len(self.failed_projects) > 10:
                print(f"  ... and {len(self.failed_projects) - 10} more")

if __name__ == "__main__":
    fixer = ComprehensiveTestFixer()
    fixer.fix_all_projects()
#!/usr/bin/env python3
"""
Comprehensive test project fix script
"""

import subprocess
import json
from pathlib import Path
import sys
import time

def run_dotnet_command(args, cwd=None, timeout=60):
    """Run a dotnet command using direct subprocess call."""
    try:
        result = subprocess.run(
            ["/usr/bin/dotnet"] + args,
            capture_output=True,
            text=True,
            cwd=cwd,
            timeout=timeout
        )
        return result
    except subprocess.TimeoutExpired:
        return type('obj', (object,), {'returncode': 1, 'stdout': '', 'stderr': 'Timeout'})()
    except Exception as e:
        return type('obj', (object,), {'returncode': 1, 'stdout': '', 'stderr': str(e)})()

def build_project(project_path):
    """Build a single project."""
    result = run_dotnet_command(
        ["build", str(project_path), "--configuration", "Release", "--no-restore", "-v", "quiet"]
    )
    return result.returncode == 0

def get_test_count(dll_path):
    """Get the number of tests in a DLL."""
    if not dll_path.exists():
        return 0
    
    result = run_dotnet_command(
        ["vstest", str(dll_path), "--ListTests"],
        timeout=30
    )
    
    if result.returncode == 0:
        # Count test lines (they usually contain namespace.class.method format)
        test_lines = [line for line in result.stdout.split('\n') if '.' in line and not line.startswith(' ')]
        return len(test_lines)
    return 0

def main():
    project_root = Path("/home/ubuntu/neo-service-layer")
    
    # First ensure all source projects are built
    print("=" * 80)
    print("BUILDING SOURCE PROJECTS")
    print("=" * 80)
    
    source_projects = list(project_root.glob("src/**/*.csproj"))
    for i, proj in enumerate(source_projects, 1):
        print(f"[{i}/{len(source_projects)}] Building {proj.name}...", end=" ")
        if build_project(proj):
            print("✅")
        else:
            print("❌")
    
    print()
    print("=" * 80)
    print("BUILDING TEST PROJECTS")
    print("=" * 80)
    
    # Find all test projects
    test_projects = list(project_root.glob("tests/**/*.csproj"))
    
    successful_builds = []
    failed_builds = []
    total_tests = 0
    
    for i, project_path in enumerate(test_projects, 1):
        project_name = project_path.stem
        print(f"[{i}/{len(test_projects)}] {project_name}")
        
        # Try to build
        if build_project(project_path):
            print(f"  ✅ Built successfully")
            
            # Count tests
            dll_path = project_path.parent / "bin" / "Release" / "net9.0" / f"{project_name}.dll"
            test_count = get_test_count(dll_path)
            
            if test_count > 0:
                print(f"  ✅ {test_count} tests found")
                total_tests += test_count
                successful_builds.append({
                    "name": project_name,
                    "path": str(project_path),
                    "tests": test_count,
                    "passing": True
                })
            else:
                print(f"  ⚠️ No tests found")
                successful_builds.append({
                    "name": project_name,
                    "path": str(project_path),
                    "tests": 0,
                    "passing": False
                })
        else:
            print(f"  ❌ Build failed")
            failed_builds.append({
                "name": project_name,
                "path": str(project_path),
                "error": "Build failed"
            })
        
        print()
    
    # Summary
    print("=" * 80)
    print("BUILD SUMMARY")
    print("=" * 80)
    print(f"Total Projects: {len(test_projects)}")
    print(f"Successfully Built: {len(successful_builds)} ({len(successful_builds)*100//len(test_projects)}%)")
    print(f"Failed Builds: {len(failed_builds)} ({len(failed_builds)*100//len(test_projects)}%)")
    print(f"Total Tests: {total_tests}")
    print()
    
    if failed_builds:
        print("Failed projects:")
        for proj in failed_builds[:10]:
            print(f"  ❌ {proj['name']}")
        if len(failed_builds) > 10:
            print(f"  ... and {len(failed_builds) - 10} more")
    
    # Save results
    results = {
        "timestamp": str(project_root),
        "summary": {
            "total_projects": len(test_projects),
            "successful_builds": len(successful_builds),
            "failed_builds": len(failed_builds),
            "total_tests": total_tests
        },
        "successful": successful_builds,
        "failed": failed_builds
    }
    
    output_file = project_root / "test-build-results-comprehensive.json"
    with open(output_file, 'w') as f:
        json.dump(results, f, indent=2)
    
    print(f"\n📄 Results saved to {output_file.name}")
    
    # Now run the tests that built successfully
    if successful_builds:
        print()
        print("=" * 80)
        print("RUNNING TESTS")
        print("=" * 80)
        
        passed_tests = 0
        failed_tests = 0
        
        for proj in successful_builds:
            if proj["tests"] > 0:
                dll_path = Path(proj["path"]).parent / "bin" / "Release" / "net9.0" / f"{proj['name']}.dll"
                
                if dll_path.exists():
                    print(f"\nRunning {proj['name']} ({proj['tests']} tests)...")
                    
                    result = run_dotnet_command(
                        ["vstest", str(dll_path), "--logger:console;verbosity=minimal"],
                        timeout=120
                    )
                    
                    if result.returncode == 0:
                        print(f"  ✅ All tests passed")
                        passed_tests += proj["tests"]
                    else:
                        print(f"  ❌ Some tests failed")
                        failed_tests += proj["tests"]
        
        print()
        print("=" * 80)
        print("TEST EXECUTION SUMMARY")
        print("=" * 80)
        print(f"Total Tests Run: {passed_tests + failed_tests}")
        print(f"Passed: {passed_tests}")
        print(f"Failed: {failed_tests}")
        if passed_tests + failed_tests > 0:
            print(f"Pass Rate: {passed_tests * 100 // (passed_tests + failed_tests)}%")

if __name__ == "__main__":
    main()
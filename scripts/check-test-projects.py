#!/usr/bin/env python3
"""
Check status of all test projects
"""

from pathlib import Path
import subprocess
import os

def check_test_projects():
    project_root = Path("/home/ubuntu/neo-service-layer")
    test_dirs = sorted(project_root.glob("tests/**/*.Tests"))
    
    results = {
        "has_csproj": [],
        "missing_csproj": [],
        "builds": [],
        "build_fails": [],
        "has_tests": [],
        "no_tests": []
    }
    
    print("=" * 80)
    print("TEST PROJECT STATUS CHECK")
    print("=" * 80)
    print()
    
    for i, test_dir in enumerate(test_dirs, 1):
        project_name = test_dir.name
        csproj_path = test_dir / f"{project_name}.csproj"
        
        print(f"[{i:2d}/45] {project_name}")
        
        # Check for .csproj
        if csproj_path.exists():
            print(f"  ✓ Has .csproj")
            results["has_csproj"].append(project_name)
            
            # Try to build
            try:
                result = subprocess.run(
                    ["dotnet", "build", str(csproj_path), 
                     "--configuration", "Release", 
                     "--no-restore", "-v", "quiet"],
                    capture_output=True,
                    text=True,
                    timeout=30,
                    cwd=str(project_root),
                    env={**os.environ, "MSBUILDDISABLENODEREUSE": "1"}
                )
                
                if result.returncode == 0:
                    print(f"  ✓ Builds successfully")
                    results["builds"].append(project_name)
                    
                    # Check for test DLL
                    dll_path = test_dir / "bin" / "Release" / "net9.0" / f"{project_name}.dll"
                    if dll_path.exists():
                        print(f"  ✓ Has test DLL")
                        results["has_tests"].append(project_name)
                    else:
                        print(f"  ✗ No test DLL found")
                        results["no_tests"].append(project_name)
                else:
                    print(f"  ✗ Build failed")
                    results["build_fails"].append(project_name)
                    # Show error
                    if result.stderr:
                        errors = result.stderr.split('\n')[:3]
                        for err in errors:
                            if err.strip():
                                print(f"    Error: {err.strip()[:100]}")
            except Exception as e:
                print(f"  ✗ Build error: {e}")
                results["build_fails"].append(project_name)
        else:
            print(f"  ✗ Missing .csproj")
            results["missing_csproj"].append(project_name)
            
            # Check if there are any .cs files
            cs_files = list(test_dir.glob("*.cs"))
            if cs_files:
                print(f"    Found {len(cs_files)} .cs files - needs project file")
            else:
                print(f"    No .cs files found")
        
        print()
    
    # Print summary
    print("=" * 80)
    print("SUMMARY")
    print("=" * 80)
    print(f"Total test projects: 45")
    print(f"Has .csproj: {len(results['has_csproj'])}")
    print(f"Missing .csproj: {len(results['missing_csproj'])}")
    print(f"Builds successfully: {len(results['builds'])}")
    print(f"Build failures: {len(results['build_fails'])}")
    print(f"Has test DLL: {len(results['has_tests'])}")
    print()
    
    if results['missing_csproj']:
        print("Projects missing .csproj files:")
        for proj in results['missing_csproj'][:10]:
            print(f"  - {proj}")
        if len(results['missing_csproj']) > 10:
            print(f"  ... and {len(results['missing_csproj']) - 10} more")
    
    return results

if __name__ == "__main__":
    results = check_test_projects()
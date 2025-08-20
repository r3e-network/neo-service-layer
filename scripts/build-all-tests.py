#!/usr/bin/env python3
"""
Build all test projects in the Neo Service Layer
Bypasses MSBuild response file issues
"""

import subprocess
import os
from pathlib import Path
import sys

def build_test_projects():
    """Build all test projects individually"""
    project_root = Path("/home/ubuntu/neo-service-layer")
    os.chdir(project_root)
    
    # Find all test project files
    test_projects = list(project_root.glob("tests/**/*.Tests.csproj"))
    
    print(f"Found {len(test_projects)} test projects to build")
    print("=" * 60)
    
    success_count = 0
    failed_projects = []
    
    for i, project in enumerate(test_projects, 1):
        project_name = project.stem
        print(f"\n[{i}/{len(test_projects)}] Building: {project_name}")
        print("-" * 40)
        
        try:
            # Build using subprocess to avoid shell issues
            result = subprocess.run(
                ["dotnet", "build", str(project), 
                 "--configuration", "Release", 
                 "--no-restore"],
                capture_output=True,
                text=True,
                timeout=60,
                env={**os.environ, "MSBUILDDISABLENODEREUSE": "1"}
            )
            
            if result.returncode == 0:
                print(f"✅ {project_name} built successfully")
                success_count += 1
            else:
                print(f"❌ {project_name} build failed")
                failed_projects.append(project_name)
                # Show first few lines of error
                error_lines = result.stderr.split('\n')[:3]
                for line in error_lines:
                    if line.strip():
                        print(f"   Error: {line.strip()}")
                        
        except subprocess.TimeoutExpired:
            print(f"⏱️ {project_name} build timeout")
            failed_projects.append(project_name)
        except Exception as e:
            print(f"❌ {project_name} error: {e}")
            failed_projects.append(project_name)
    
    print("\n" + "=" * 60)
    print("BUILD SUMMARY")
    print("=" * 60)
    print(f"Total Projects: {len(test_projects)}")
    print(f"Successfully Built: {success_count}")
    print(f"Failed: {len(failed_projects)}")
    
    if failed_projects:
        print("\nFailed Projects:")
        for proj in failed_projects:
            print(f"  - {proj}")
    
    return success_count == len(test_projects)

if __name__ == "__main__":
    success = build_test_projects()
    sys.exit(0 if success else 1)
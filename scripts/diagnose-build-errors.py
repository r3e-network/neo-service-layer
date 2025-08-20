#!/usr/bin/env python3
import subprocess
import os
from pathlib import Path
from collections import defaultdict

def build_project(project_path):
    """Build a project and return error details"""
    cmd = ['dotnet', 'build', str(project_path), '--no-restore', '-v', 'quiet']
    result = subprocess.run(cmd, capture_output=True, text=True, shell=False)
    
    errors = []
    for line in result.stdout.split('\n'):
        if 'error CS' in line or 'error MSB' in line:
            errors.append(line)
    
    return result.returncode == 0, errors

def main():
    os.chdir('/home/ubuntu/neo-service-layer')
    
    # Test a few key projects
    test_projects = [
        'src/Core/NeoServiceLayer.Shared/NeoServiceLayer.Shared.csproj',
        'src/Core/NeoServiceLayer.Core/NeoServiceLayer.Core.csproj',
        'src/Core/NeoServiceLayer.ServiceFramework/NeoServiceLayer.ServiceFramework.csproj',
        'src/Infrastructure/NeoServiceLayer.Infrastructure.Security/NeoServiceLayer.Infrastructure.Security.csproj',
        'src/Services/NeoServiceLayer.Services.Authentication/NeoServiceLayer.Services.Authentication.csproj',
    ]
    
    error_patterns = defaultdict(list)
    
    for proj in test_projects:
        print(f"\n{'='*60}")
        print(f"Testing: {proj}")
        print('='*60)
        
        success, errors = build_project(proj)
        
        if success:
            print("✅ BUILD SUCCEEDED!")
        else:
            print("❌ BUILD FAILED")
            if errors:
                print("\nErrors found:")
                for error in errors[:5]:  # Show first 5 errors
                    print(f"  {error}")
                    # Extract error code
                    if 'error CS' in error:
                        error_code = error.split('error CS')[1].split(':')[0].strip()
                        error_patterns[f"CS{error_code}"].append(proj)
                    elif 'error MSB' in error:
                        error_code = error.split('error MSB')[1].split(':')[0].strip()
                        error_patterns[f"MSB{error_code}"].append(proj)
    
    # Summary of error patterns
    print(f"\n{'='*60}")
    print("ERROR PATTERN SUMMARY")
    print('='*60)
    for error_code, projects in sorted(error_patterns.items()):
        print(f"{error_code}: {len(projects)} projects")
        for proj in projects[:3]:
            print(f"  - {Path(proj).stem}")

if __name__ == "__main__":
    main()
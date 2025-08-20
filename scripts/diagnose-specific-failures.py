#!/usr/bin/env python3
"""
Diagnose specific build failures for key projects.
"""
import subprocess
import os
from pathlib import Path

def get_build_errors(project_path):
    """Get detailed build errors for a project"""
    cmd = ['dotnet', 'build', str(project_path), '--no-restore', '-v', 'normal']
    result = subprocess.run(cmd, capture_output=True, text=True, shell=False)
    
    errors = []
    for line in result.stdout.split('\n') + result.stderr.split('\n'):
        if 'error CS' in line or 'error MSB' in line or 'error NU' in line:
            errors.append(line.strip())
    
    return result.returncode == 0, errors

def main():
    os.chdir('/home/ubuntu/neo-service-layer')
    
    # Key projects that should build
    key_projects = [
        'src/Core/NeoServiceLayer.ServiceFramework/NeoServiceLayer.ServiceFramework.csproj',
        'src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/NeoServiceLayer.Infrastructure.Persistence.csproj',
        'src/Infrastructure/NeoServiceLayer.Infrastructure.Security/NeoServiceLayer.Infrastructure.Security.csproj',
        'src/Infrastructure/NeoServiceLayer.Infrastructure.Observability/NeoServiceLayer.Infrastructure.Observability.csproj',
        'src/Infrastructure/NeoServiceLayer.Infrastructure.Blockchain/NeoServiceLayer.Infrastructure.Blockchain.csproj',
    ]
    
    for proj_path in key_projects:
        if not Path(proj_path).exists():
            print(f"❌ {proj_path} - NOT FOUND")
            continue
            
        print(f"\n{'='*70}")
        print(f"DIAGNOSING: {proj_path}")
        print('='*70)
        
        success, errors = get_build_errors(proj_path)
        
        if success:
            print("✅ BUILD SUCCESSFUL!")
        else:
            print("❌ BUILD FAILED")
            if errors:
                print("\nErrors found:")
                # Group similar errors
                error_types = {}
                for error in errors:
                    if 'CS' in error:
                        error_code = error.split('error CS')[1].split(':')[0].strip()
                        key = f"CS{error_code}"
                    elif 'MSB' in error:
                        error_code = error.split('error MSB')[1].split(':')[0].strip()
                        key = f"MSB{error_code}"
                    else:
                        key = "OTHER"
                    
                    if key not in error_types:
                        error_types[key] = []
                    error_types[key].append(error)
                
                # Show first instance of each error type
                for error_type, instances in error_types.items():
                    print(f"\n{error_type} ({len(instances)} instances):")
                    print(f"  {instances[0][:200]}")

if __name__ == "__main__":
    main()
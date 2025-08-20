#!/usr/bin/env python3
import subprocess
import os
from pathlib import Path
import sys

def safe_run(cmd):
    """Run command safely without shell interpretation"""
    try:
        # Use subprocess without shell to avoid the "2>&1" issue
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            shell=False,
            cwd='/home/ubuntu/neo-service-layer'
        )
        return result.returncode, result.stdout, result.stderr
    except Exception as e:
        return 1, "", str(e)

def build_project(project_path):
    """Build a single project"""
    cmd = ['dotnet', 'build', str(project_path), '--no-restore', '-v', 'quiet']
    return safe_run(cmd)

def build_all_core_projects():
    """Build core projects first"""
    core_projects = [
        'src/Core/NeoServiceLayer.Shared/NeoServiceLayer.Shared.csproj',
        'src/Core/NeoServiceLayer.Core/NeoServiceLayer.Core.csproj',
        'src/Core/NeoServiceLayer.ServiceFramework/NeoServiceLayer.ServiceFramework.csproj',
    ]
    
    results = {}
    for proj in core_projects:
        print(f"Building {proj}...")
        returncode, stdout, stderr = build_project(proj)
        if returncode == 0:
            results[proj] = 'SUCCESS'
            print(f"  ✅ Success")
        else:
            results[proj] = 'FAILED'
            print(f"  ❌ Failed")
            # Print first error
            errors = [line for line in stderr.split('\n') if 'error' in line.lower()]
            if errors:
                print(f"    Error: {errors[0]}")
    
    return results

def build_infrastructure():
    """Build infrastructure projects"""
    infra_projects = list(Path('src/Infrastructure').glob('*/*.csproj'))
    
    results = {}
    for proj in sorted(infra_projects):
        print(f"Building {proj}...")
        returncode, stdout, stderr = build_project(proj)
        if returncode == 0:
            results[str(proj)] = 'SUCCESS'
            print(f"  ✅ Success")
        else:
            results[str(proj)] = 'FAILED'
            print(f"  ❌ Failed")
    
    return results

def build_services():
    """Build service projects"""
    service_projects = list(Path('src/Services').glob('*/*.csproj'))
    
    results = {}
    success_count = 0
    for proj in sorted(service_projects):
        print(f"Building {proj}...")
        returncode, stdout, stderr = build_project(proj)
        if returncode == 0:
            results[str(proj)] = 'SUCCESS'
            success_count += 1
            print(f"  ✅ Success ({success_count}/{len(service_projects)})")
        else:
            results[str(proj)] = 'FAILED'
            print(f"  ❌ Failed")
    
    return results

def build_tests():
    """Build test projects"""
    test_projects = list(Path('tests').glob('**/*.csproj'))
    # Exclude any regression test subprojects
    test_projects = [p for p in test_projects if 'RegressionTests/RegressionTests.csproj' not in str(p)]
    
    results = {}
    success_count = 0
    for proj in sorted(test_projects):
        print(f"Building {proj}...")
        returncode, stdout, stderr = build_project(proj)
        if returncode == 0:
            results[str(proj)] = 'SUCCESS'
            success_count += 1
            print(f"  ✅ Success ({success_count}/{len(test_projects)})")
        else:
            results[str(proj)] = 'FAILED'
            print(f"  ❌ Failed")
    
    return results

def main():
    os.chdir('/home/ubuntu/neo-service-layer')
    
    print("=" * 60)
    print("Safe Build Script - Avoiding MSBuild Issues")
    print("=" * 60)
    
    # Build core projects first
    print("\n🔨 Building Core Projects...")
    core_results = build_all_core_projects()
    
    # Build infrastructure
    print("\n🔨 Building Infrastructure Projects...")
    infra_results = build_infrastructure()
    
    # Build services
    print("\n🔨 Building Service Projects...")
    service_results = build_services()
    
    # Build tests
    print("\n🔨 Building Test Projects...")
    test_results = build_tests()
    
    # Summary
    all_results = {**core_results, **infra_results, **service_results, **test_results}
    success_count = sum(1 for v in all_results.values() if v == 'SUCCESS')
    total_count = len(all_results)
    
    print("\n" + "=" * 60)
    print("📊 Build Summary")
    print("=" * 60)
    print(f"Total projects: {total_count}")
    print(f"✅ Succeeded: {success_count}")
    print(f"❌ Failed: {total_count - success_count}")
    print(f"Success rate: {success_count / total_count * 100:.1f}%")
    
    # List successes
    successes = [k for k, v in all_results.items() if v == 'SUCCESS']
    if successes:
        print("\n✅ Successfully built:")
        for proj in successes[:5]:
            print(f"  - {proj}")
        if len(successes) > 5:
            print(f"  ... and {len(successes) - 5} more")

if __name__ == "__main__":
    main()
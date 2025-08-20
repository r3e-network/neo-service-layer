#!/usr/bin/env python3
import subprocess
import json
import os
from pathlib import Path

def run_command(cmd):
    """Run a command and return output"""
    try:
        result = subprocess.run(cmd, shell=False, capture_output=True, text=True)
        return result.stdout + result.stderr
    except Exception as e:
        return str(e)

def build_solution():
    """Build the solution and return status"""
    print("Building solution...")
    cmd = ['dotnet', 'build', 'NeoServiceLayer.sln', '--no-restore', '-v', 'minimal']
    output = run_command(cmd)
    return output

def count_projects():
    """Count total projects in solution"""
    project_files = list(Path('.').glob('**/*.csproj'))
    test_projects = [p for p in project_files if 'Tests' in str(p) or 'Test' in str(p)]
    src_projects = [p for p in project_files if p not in test_projects]
    
    return {
        'total': len(project_files),
        'test': len(test_projects),
        'src': len(src_projects)
    }

def check_individual_projects():
    """Try to build each project individually"""
    results = {'succeeded': [], 'failed': []}
    
    for proj_file in Path('.').glob('**/*.csproj'):
        if 'bin' in str(proj_file) or 'obj' in str(proj_file):
            continue
            
        print(f"Checking {proj_file}...")
        cmd = ['dotnet', 'build', str(proj_file), '--no-restore', '-v', 'quiet']
        output = run_command(cmd)
        
        if 'Build succeeded' in output or not any(err in output for err in ['error CS', 'error MSB', 'Build FAILED']):
            results['succeeded'].append(str(proj_file))
        else:
            results['failed'].append(str(proj_file))
            
    return results

def main():
    os.chdir('/home/ubuntu/neo-service-layer')
    
    # Count projects
    counts = count_projects()
    print(f"\n📊 Project Count:")
    print(f"  Total: {counts['total']}")
    print(f"  Source: {counts['src']}")
    print(f"  Tests: {counts['test']}")
    
    # Try to build solution
    print("\n🔨 Building solution...")
    build_output = build_solution()
    
    # Check if build succeeded
    if 'Build succeeded' in build_output:
        print("✅ Solution build succeeded!")
    else:
        print("❌ Solution build failed. Checking individual projects...")
        
    # Check individual projects
    print("\n🔍 Checking individual projects...")
    results = check_individual_projects()
    
    print(f"\n📈 Build Results:")
    print(f"  ✅ Succeeded: {len(results['succeeded'])} projects")
    print(f"  ❌ Failed: {len(results['failed'])} projects")
    print(f"  Success Rate: {len(results['succeeded']) / (len(results['succeeded']) + len(results['failed'])) * 100:.1f}%")
    
    if len(results['succeeded']) > 0:
        print("\n✅ Successfully building projects:")
        for proj in sorted(results['succeeded'])[:10]:
            print(f"  - {proj}")
        if len(results['succeeded']) > 10:
            print(f"  ... and {len(results['succeeded']) - 10} more")
    
    if len(results['failed']) > 0:
        print("\n❌ Failed projects:")
        for proj in sorted(results['failed'])[:10]:
            print(f"  - {proj}")
        if len(results['failed']) > 10:
            print(f"  ... and {len(results['failed']) - 10} more")

if __name__ == "__main__":
    main()
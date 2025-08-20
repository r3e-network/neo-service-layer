#!/usr/bin/env python3
"""
Analyze build failures for test projects and identify common issues
"""

import subprocess
from pathlib import Path
import json
from collections import defaultdict

def get_build_errors(project_path):
    """Get detailed build errors for a project."""
    try:
        result = subprocess.run(
            ["dotnet", "build", str(project_path), "--configuration", "Release", "--no-restore", "-v", "diagnostic"],
            capture_output=True,
            text=True,
            timeout=30
        )
        
        errors = []
        for line in result.stdout.split('\n'):
            if 'error CS' in line or 'error NU' in line:
                # Extract error code and message
                if 'CS0246' in line:
                    errors.append("CS0246: Missing type or namespace")
                elif 'CS0234' in line:
                    errors.append("CS0234: Missing namespace")
                elif 'CS0103' in line:
                    errors.append("CS0103: Name does not exist")
                elif 'CS1061' in line:
                    errors.append("CS1061: Missing method or property")
                elif 'NU1101' in line:
                    errors.append("NU1101: Package not found")
                elif 'error' in line.lower():
                    errors.append(line.strip()[:100])
        
        return errors
    except Exception as e:
        return [f"Exception: {str(e)}"]

def analyze_project_references(project_path):
    """Check if referenced projects exist and build."""
    if not project_path.exists():
        return ["Project file not found"]
    
    content = project_path.read_text()
    issues = []
    
    # Check for project references
    if '<ProjectReference Include=' in content:
        for line in content.split('\n'):
            if '<ProjectReference Include=' in line:
                ref_path = line.split('"')[1]
                # Convert relative path to absolute
                abs_ref_path = (project_path.parent / ref_path).resolve()
                
                if not abs_ref_path.exists():
                    issues.append(f"Missing project: {ref_path}")
                else:
                    # Check if referenced project builds
                    result = subprocess.run(
                        ["dotnet", "build", str(abs_ref_path), "--configuration", "Release", "--no-restore", "-v", "quiet"],
                        capture_output=True,
                        text=True,
                        timeout=30
                    )
                    if result.returncode != 0:
                        issues.append(f"Referenced project fails to build: {abs_ref_path.name}")
    
    return issues

def main():
    project_root = Path("/home/ubuntu/neo-service-layer")
    
    # Load the test results to get failed projects
    results_file = project_root / "test-build-results-comprehensive.json"
    if results_file.exists():
        with open(results_file) as f:
            results = json.load(f)
    else:
        print("No test results file found")
        return
    
    print("=" * 80)
    print("ANALYZING BUILD FAILURES")
    print("=" * 80)
    print()
    
    failed_projects = results.get("failed", [])
    
    # Group errors by type
    error_types = defaultdict(list)
    
    # Analyze first 10 failed projects in detail
    for i, project in enumerate(failed_projects[:10], 1):
        project_path = Path(project["path"])
        print(f"[{i}/10] Analyzing {project['name']}...")
        
        # Get build errors
        errors = get_build_errors(project_path)
        
        # Check project references
        ref_issues = analyze_project_references(project_path)
        
        if errors or ref_issues:
            print(f"  Issues found:")
            for error in errors[:3]:  # Show first 3 errors
                print(f"    - {error}")
                error_types[error].append(project['name'])
            
            for issue in ref_issues[:3]:
                print(f"    - {issue}")
                error_types[issue].append(project['name'])
        else:
            print(f"  No specific errors captured")
        
        print()
    
    # Summary of error types
    print("=" * 80)
    print("ERROR SUMMARY")
    print("=" * 80)
    
    for error_type, projects in sorted(error_types.items(), key=lambda x: len(x[1]), reverse=True):
        print(f"\n{error_type}:")
        print(f"  Affects {len(projects)} projects")
        print(f"  Examples: {', '.join(projects[:3])}")
    
    # Recommendations
    print()
    print("=" * 80)
    print("RECOMMENDATIONS")
    print("=" * 80)
    
    if "CS0246: Missing type or namespace" in error_types:
        print("1. Add missing type definitions or fix namespace references")
    
    if "CS0234: Missing namespace" in error_types:
        print("2. Ensure all namespaces are properly defined")
    
    if "Referenced project fails to build" in [k for k in error_types.keys() if "Referenced" in k]:
        print("3. Fix referenced projects first before test projects")
    
    if "NU1101: Package not found" in error_types:
        print("4. Restore NuGet packages or update package references")
    
    print("\nPriority: Fix service project builds first, then test projects")

if __name__ == "__main__":
    main()
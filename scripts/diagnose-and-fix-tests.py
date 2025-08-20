#!/usr/bin/env python3
"""
Diagnose and fix all failing test projects
"""

import subprocess
import json
from pathlib import Path
import re

def get_build_errors(project_path):
    """Get detailed build errors for a project."""
    try:
        result = subprocess.run(
            ["dotnet", "build", str(project_path), "--configuration", "Release", "--no-restore", "-v", "normal"],
            capture_output=True,
            text=True,
            timeout=30
        )
        
        if result.returncode != 0:
            # Extract error messages
            errors = []
            for line in result.stdout.split('\n'):
                if 'error' in line.lower() and 'MSB1008' not in line:
                    errors.append(line.strip())
            
            # Also check stderr
            for line in result.stderr.split('\n'):
                if 'error' in line.lower():
                    errors.append(line.strip())
            
            return errors
        return []
    except Exception as e:
        return [str(e)]

def fix_common_issues(project_path):
    """Fix common compilation issues in test projects."""
    project_dir = project_path.parent
    fixed_issues = []
    
    # Find all .cs files in the project
    cs_files = list(project_dir.glob("**/*.cs"))
    
    for cs_file in cs_files:
        if cs_file.is_file():
            content = cs_file.read_text()
            original_content = content
            
            # Fix missing xUnit references
            if any(attr in content for attr in ["[Fact]", "[Theory]", "[InlineData"]):
                if "using Xunit;" not in content:
                    lines = content.split('\n')
                    last_using = -1
                    for i, line in enumerate(lines):
                        if line.startswith("using "):
                            last_using = i
                    
                    if last_using >= 0:
                        lines.insert(last_using + 1, "using Xunit;")
                        content = '\n'.join(lines)
                        fixed_issues.append(f"Added xUnit reference to {cs_file.name}")
            
            # Fix missing FluentAssertions references
            if ".Should()" in content and "using FluentAssertions;" not in content:
                lines = content.split('\n')
                last_using = -1
                for i, line in enumerate(lines):
                    if line.startswith("using "):
                        last_using = i
                
                if last_using >= 0:
                    lines.insert(last_using + 1, "using FluentAssertions;")
                    content = '\n'.join(lines)
                    fixed_issues.append(f"Added FluentAssertions reference to {cs_file.name}")
            
            # Fix missing Moq references
            if "Mock<" in content and "using Moq;" not in content:
                lines = content.split('\n')
                last_using = -1
                for i, line in enumerate(lines):
                    if line.startswith("using "):
                        last_using = i
                
                if last_using >= 0:
                    lines.insert(last_using + 1, "using Moq;")
                    content = '\n'.join(lines)
                    fixed_issues.append(f"Added Moq reference to {cs_file.name}")
            
            # Save if modified
            if content != original_content:
                cs_file.write_text(content)
    
    return fixed_issues

def main():
    # Load the test results
    results_file = Path("/home/ubuntu/neo-service-layer/test-build-results-fixed.json")
    with open(results_file) as f:
        results = json.load(f)
    
    print("=" * 80)
    print("DIAGNOSING AND FIXING FAILED TEST PROJECTS")
    print("=" * 80)
    print()
    
    failed_projects = results.get("failed", [])
    fixed_count = 0
    
    for i, project in enumerate(failed_projects, 1):
        project_path = Path(project["path"])
        print(f"[{i}/{len(failed_projects)}] {project['name']}")
        
        # First, try to fix common issues
        fixes = fix_common_issues(project_path)
        if fixes:
            print(f"  Applied fixes:")
            for fix in fixes:
                print(f"    - {fix}")
        
        # Now try to build again
        print("  Attempting build...")
        result = subprocess.run(
            ["dotnet", "build", str(project_path), "--configuration", "Release", "--no-restore", "-v", "quiet"],
            capture_output=True,
            text=True,
            timeout=60
        )
        
        if result.returncode == 0:
            print(f"  ✅ Build successful after fixes!")
            fixed_count += 1
        else:
            # Get detailed errors
            errors = get_build_errors(project_path)
            if errors:
                print(f"  ❌ Build still failing:")
                for error in errors[:3]:  # Show first 3 errors
                    print(f"     {error}")
            else:
                print(f"  ❌ Build failed (unknown error)")
        
        print()
    
    print("=" * 80)
    print(f"Fixed {fixed_count} out of {len(failed_projects)} failed projects")
    print("=" * 80)

if __name__ == "__main__":
    main()
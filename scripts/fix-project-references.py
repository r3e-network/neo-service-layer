#!/usr/bin/env python3
"""
Fix project reference paths in .csproj files
"""

from pathlib import Path
import re

def fix_project_references(project_file):
    """Fix Windows-style paths to Unix-style in project references."""
    if not project_file.exists():
        return False
    
    content = project_file.read_text()
    original_content = content
    
    # Fix backslashes in project references
    content = re.sub(r'\\', '/', content)
    
    # Fix specific common path issues
    replacements = [
        # Fix double dots with forward slashes
        (r'\.\.\\\.\.\\', '../../'),
        (r'\.\./\.\./\.\./', '../../'),
        (r'\.\.\\', '../'),
    ]
    
    for pattern, replacement in replacements:
        content = re.sub(pattern, replacement, content)
    
    if content != original_content:
        project_file.write_text(content)
        return True
    
    return False

def ensure_project_exists(project_file):
    """Check if all referenced projects exist and fix paths if needed."""
    if not project_file.exists():
        return []
    
    content = project_file.read_text()
    fixes = []
    
    # Find all project references
    project_refs = re.findall(r'<ProjectReference Include="([^"]+)"', content)
    
    for ref in project_refs:
        # Convert to absolute path
        ref_path = (project_file.parent / ref).resolve()
        
        if not ref_path.exists():
            # Try to find the project in the repo
            project_name = Path(ref).name
            search_results = list(Path("/home/ubuntu/neo-service-layer").glob(f"**/{project_name}"))
            
            if search_results:
                # Calculate relative path from current project to found project
                try:
                    relative_path = search_results[0].relative_to(project_file.parent)
                    # Replace the reference
                    content = content.replace(f'Include="{ref}"', f'Include="{relative_path}"')
                    fixes.append(f"Fixed reference: {ref} -> {relative_path}")
                except ValueError:
                    # If relative path fails, use absolute path
                    content = content.replace(f'Include="{ref}"', f'Include="{search_results[0]}"')
                    fixes.append(f"Fixed reference: {ref} -> {search_results[0]}")
    
    if fixes:
        project_file.write_text(content)
    
    return fixes

def main():
    project_root = Path("/home/ubuntu/neo-service-layer")
    
    print("=" * 80)
    print("FIXING PROJECT REFERENCES")
    print("=" * 80)
    print()
    
    # Find all .csproj files
    csproj_files = list(project_root.glob("**/*.csproj"))
    
    fixed_count = 0
    reference_fixes = []
    
    for csproj in csproj_files:
        # Fix path separators
        if fix_project_references(csproj):
            fixed_count += 1
            print(f"Fixed path separators in: {csproj.relative_to(project_root)}")
        
        # Fix missing references
        fixes = ensure_project_exists(csproj)
        if fixes:
            reference_fixes.extend(fixes)
            print(f"Fixed references in: {csproj.relative_to(project_root)}")
            for fix in fixes:
                print(f"  - {fix}")
    
    print()
    print("=" * 80)
    print("SUMMARY")
    print("=" * 80)
    print(f"Fixed path separators in {fixed_count} projects")
    print(f"Fixed {len(reference_fixes)} project references")
    
    # Now try to build the TestInfrastructure project as it's a common dependency
    print()
    print("Building TestInfrastructure project...")
    
    test_infra_proj = project_root / "tests/TestInfrastructure/NeoServiceLayer.TestInfrastructure.csproj"
    if test_infra_proj.exists():
        import subprocess
        result = subprocess.run(
            ["dotnet", "build", str(test_infra_proj), "--configuration", "Release", "--no-restore", "-v", "quiet"],
            capture_output=True,
            text=True,
            timeout=60
        )
        
        if result.returncode == 0:
            print("✅ TestInfrastructure project now builds successfully!")
        else:
            print("❌ TestInfrastructure still has build issues")
            # Show first few errors
            for line in result.stdout.split('\n'):
                if 'error' in line.lower():
                    print(f"  {line.strip()[:100]}")

if __name__ == "__main__":
    main()
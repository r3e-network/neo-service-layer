#!/usr/bin/env python3
"""
Fix namespace declaration issues in C# files
"""

from pathlib import Path
import re

def fix_namespace_order(file_path):
    """Fix file-scoped namespace declaration order issues."""
    if not file_path.exists():
        return False
    
    content = file_path.read_text()
    lines = content.split('\n')
    
    # Find using statements and namespace declarations
    using_statements = []
    namespace_line = None
    namespace_content = []
    other_lines = []
    
    in_namespace = False
    namespace_indent = 0
    
    for i, line in enumerate(lines):
        # Check for misplaced using statements
        if line.strip().startswith('using ') and line.strip().endswith(';'):
            if not in_namespace:
                using_statements.append(line)
            else:
                # Move using statement before namespace
                using_statements.append(line.strip())
                continue
        elif line.strip().startswith('namespace '):
            namespace_line = line
            in_namespace = True
            # Determine if it's file-scoped (ends with ;) or block-scoped (ends with {)
            if line.strip().endswith(';'):
                namespace_indent = 0  # File-scoped
            else:
                namespace_indent = len(line) - len(line.lstrip())
        elif in_namespace:
            namespace_content.append(line)
        else:
            # Skip empty lines at the beginning
            if line.strip() or using_statements or namespace_line:
                other_lines.append(line)
    
    # Reconstruct the file
    new_lines = []
    
    # Add using statements first
    for using in using_statements:
        if using.strip():
            new_lines.append(using.rstrip())
    
    # Add blank line after usings if present
    if using_statements:
        new_lines.append('')
    
    # Add namespace declaration
    if namespace_line:
        new_lines.append(namespace_line.rstrip())
    
    # Add namespace content
    for line in namespace_content:
        new_lines.append(line.rstrip())
    
    # Add other content if no namespace was found
    if not namespace_line:
        for line in other_lines:
            new_lines.append(line.rstrip())
    
    new_content = '\n'.join(new_lines)
    
    if new_content != content:
        file_path.write_text(new_content)
        return True
    
    return False

def fix_specific_files():
    """Fix specific files with known issues."""
    files_to_fix = [
        "src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/OcclumFileStorageProvider.cs",
        "src/Blockchain/NeoServiceLayer.Neo.N3/NeoN3Client.cs", 
        "src/Tee/NeoServiceLayer.Tee.Enclave/AttestationService.cs",
        "src/Tee/NeoServiceLayer.Tee.Enclave/OcclumEnclaveWrapper.cs",
        "src/Tee/NeoServiceLayer.Tee.Enclave/EnclaveWrapperCrypto.cs"
    ]
    
    project_root = Path("/home/ubuntu/neo-service-layer")
    fixed_count = 0
    
    for file_path in files_to_fix:
        full_path = project_root / file_path
        if full_path.exists():
            # Read the file
            content = full_path.read_text()
            lines = content.split('\n')
            
            # Special handling for OcclumEnclaveWrapper.cs - remove top-level statements
            if "OcclumEnclaveWrapper.cs" in file_path:
                # Remove any top-level statements (lines outside of namespace/class)
                new_lines = []
                for line in lines:
                    # Skip lines that look like top-level statements
                    if line.strip() and not line.strip().startswith('//') and not line.strip().startswith('using') and not line.strip().startswith('namespace'):
                        # Check if it's before the namespace declaration
                        if 'namespace' not in '\n'.join(new_lines):
                            continue  # Skip top-level statements
                    new_lines.append(line)
                
                content = '\n'.join(new_lines)
                full_path.write_text(content)
            
            # Fix namespace order for all files
            if fix_namespace_order(full_path):
                fixed_count += 1
                print(f"Fixed: {file_path}")
    
    return fixed_count

def main():
    project_root = Path("/home/ubuntu/neo-service-layer")
    
    print("=" * 80)
    print("FIXING NAMESPACE ISSUES")
    print("=" * 80)
    print()
    
    # Fix specific files with known issues
    print("Fixing specific files with namespace issues...")
    fixed_specific = fix_specific_files()
    print(f"  Fixed {fixed_specific} specific files")
    
    # Fix all C# files in the project
    print("\nScanning all C# files for namespace issues...")
    cs_files = list(project_root.glob("src/**/*.cs"))
    fixed_count = 0
    
    for cs_file in cs_files:
        if fix_namespace_order(cs_file):
            fixed_count += 1
            print(f"  Fixed: {cs_file.relative_to(project_root)}")
    
    print()
    print("=" * 80)
    print("SUMMARY")
    print("=" * 80)
    print(f"Fixed {fixed_specific} specific files")
    print(f"Fixed {fixed_count} additional files")
    print(f"Total fixes: {fixed_specific + fixed_count}")

if __name__ == "__main__":
    main()
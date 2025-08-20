#!/usr/bin/env python3
"""
Fix all files with misplaced code lines that look like using statements.
"""
import os
import re
from pathlib import Path

def fix_file(file_path):
    """Fix a file with misplaced using statements"""
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    proper_usings = []
    code_lines = []
    namespace_found = False
    misplaced_code = []
    
    for line in lines:
        stripped = line.strip()
        
        # Check if it's a using statement for a disposable (contains 'var' or '=')
        if stripped.startswith('using ') and ('var ' in stripped or '=' in stripped):
            misplaced_code.append(stripped)
            continue  # Skip these - they belong inside methods
        
        # Collect proper using directives
        if stripped.startswith('using ') and stripped.endswith(';') and not namespace_found:
            proper_usings.append(line)
        elif stripped.startswith('namespace '):
            namespace_found = True
            code_lines.append(line)
        elif namespace_found or not stripped or stripped.startswith('//'):
            code_lines.append(line)
    
    # Build proper file content
    new_content = []
    new_content.extend(proper_usings)
    if proper_usings:
        new_content.append('\n')
    new_content.extend(code_lines)
    
    # Write back
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(new_content)
    
    return len(misplaced_code) > 0, misplaced_code

def main():
    os.chdir('/home/ubuntu/neo-service-layer')
    
    # Files known to have issues
    problem_files = [
        'src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PersistentStorageExtensions.cs',
        'src/Infrastructure/NeoServiceLayer.Infrastructure.Observability/Logging/StructuredLogger.cs',
    ]
    
    # Also scan all C# files for this pattern
    all_files = list(Path('.').glob('**/*.cs'))
    all_files = [f for f in all_files if f.is_file() and 'bin' not in str(f) and 'obj' not in str(f) and '.nuget' not in str(f)]
    
    fixed_count = 0
    for file_path in all_files:
        try:
            fixed, misplaced = fix_file(file_path)
            if fixed:
                fixed_count += 1
                print(f"Fixed: {file_path}")
                for code in misplaced[:3]:
                    print(f"  Removed misplaced: {code[:60]}...")
        except Exception as e:
            print(f"Error processing {file_path}: {e}")
    
    print(f"\n✅ Fixed {fixed_count} files with misplaced code")

if __name__ == "__main__":
    main()
#!/usr/bin/env python3
"""
Final comprehensive fix for all remaining compilation issues.
"""
import os
import re
from pathlib import Path

def fix_using_directives(file_path):
    """Fix missing using directives"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Check for specific missing types and add corresponding usings
    usings_to_add = set()
    
    if 'BigInteger' in content:
        if 'using System.Numerics;' not in content:
            usings_to_add.add('using System.Numerics;')
    
    if '[Required]' in content or 'RequiredAttribute' in content:
        if 'using System.ComponentModel.DataAnnotations;' not in content:
            usings_to_add.add('using System.ComponentModel.DataAnnotations;')
    
    if 'SecureString' in content:
        if 'using System.Security;' not in content:
            usings_to_add.add('using System.Security;')
    
    if not usings_to_add:
        return False
    
    # Find namespace declaration and add usings before it
    namespace_match = re.search(r'^namespace [\w\.]+', content, re.MULTILINE)
    if namespace_match:
        insertion_point = namespace_match.start()
        
        # Find last using statement before namespace
        last_using = content.rfind('using ', 0, insertion_point)
        if last_using > 0:
            # Find the end of the line after the last using
            insert_after = content.find('\n', last_using) + 1
            # Add new usings
            new_usings = '\n'.join(sorted(usings_to_add)) + '\n'
            content = content[:insert_after] + new_usings + content[insert_after:]
        else:
            # No existing usings, add at the beginning
            new_usings = '\n'.join(sorted(usings_to_add)) + '\n\n'
            content = new_usings + content
        
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        return True
    
    return False

def fix_misplaced_usings(file_path):
    """Fix using statements that are placed after namespace declarations"""
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # Collect all using statements
    using_statements = []
    namespace_line = -1
    other_lines = []
    
    in_namespace = False
    for i, line in enumerate(lines):
        if line.strip().startswith('namespace '):
            namespace_line = i
            in_namespace = True
            other_lines.append(line)
        elif line.strip().startswith('using ') and line.strip().endswith(';'):
            using_statements.append(line)
        else:
            other_lines.append(line)
    
    if not using_statements or namespace_line == -1:
        return False
    
    # Rebuild file with usings at the top
    new_content = using_statements + ['\n'] + other_lines
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(new_content)
    
    return True

def main():
    os.chdir('/home/ubuntu/neo-service-layer')
    
    # Fix specific known problem files
    problem_files = [
        'src/Core/NeoServiceLayer.Core/SmartContracts/ISmartContractManager.cs',
        'src/Core/NeoServiceLayer.Core/Models/PatternRecognitionModels.cs',
        'src/Core/NeoServiceLayer.Core/Models/ServiceModels.cs',
        'src/Infrastructure/NeoServiceLayer.Infrastructure.Persistence/PersistentStorageExtensions.cs',
    ]
    
    for file_path in problem_files:
        if Path(file_path).exists():
            print(f"Fixing {file_path}...")
            if fix_using_directives(file_path):
                print(f"  ✅ Added missing using directives")
            if fix_misplaced_usings(file_path):
                print(f"  ✅ Fixed misplaced using statements")
    
    # Also scan all C# files for these issues
    print("\nScanning all C# files for remaining issues...")
    cs_files = list(Path('.').glob('**/*.cs'))
    cs_files = [f for f in cs_files if f.is_file() and 'bin' not in str(f) and 'obj' not in str(f) and '.nuget' not in str(f)]
    
    fixed_count = 0
    for file_path in cs_files:
        try:
            if fix_using_directives(file_path):
                fixed_count += 1
                print(f"Fixed: {file_path}")
        except Exception as e:
            print(f"Error processing {file_path}: {e}")
    
    print(f"\n✅ Fixed {fixed_count} additional files")

if __name__ == "__main__":
    main()
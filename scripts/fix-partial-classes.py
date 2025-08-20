#!/usr/bin/env python3
import os
import re
import glob

def fix_partial_class_file(filepath):
    """Fix partial class files to use correct base class references"""
    
    with open(filepath, 'r') as f:
        content = f.read()
    
    original_content = content
    filename = os.path.basename(filepath)
    
    # Skip if it's not a service file
    if 'Service' not in filename:
        return False
    
    # Add ServiceFramework using if missing and file has EnclaveBlockchainServiceBase
    if 'EnclaveBlockchainServiceBase' in content and 'using NeoServiceLayer.ServiceFramework;' not in content:
        # Find the last using statement
        last_using = content.rfind('using ')
        if last_using != -1:
            end_of_line = content.find('\n', last_using)
            if end_of_line != -1:
                content = content[:end_of_line] + '\nusing NeoServiceLayer.ServiceFramework;' + content[end_of_line:]
    
    # Fix ambiguous EnclaveBlockchainServiceBase references in partial classes
    content = re.sub(
        r'public\s+partial\s+class\s+(\w+Service)\s*:\s+EnclaveBlockchainServiceBase',
        r'public partial class \1 : ServiceFramework.EnclaveBlockchainServiceBase',
        content
    )
    
    # Fix ambiguous BlockchainServiceBase references in partial classes
    content = re.sub(
        r'public\s+partial\s+class\s+(\w+Service)\s*:\s+BlockchainServiceBase',
        r'public partial class \1 : ServiceFramework.BlockchainServiceBase',
        content
    )
    
    # Fix ambiguous ServiceBase references in partial classes
    content = re.sub(
        r'public\s+partial\s+class\s+(\w+Service)\s*:\s+ServiceBase',
        r'public partial class \1 : ServiceFramework.ServiceBase',
        content
    )
    
    # Fix non-partial classes too
    content = re.sub(
        r'public\s+class\s+(\w+Service)\s*:\s+EnclaveBlockchainServiceBase',
        r'public class \1 : ServiceFramework.EnclaveBlockchainServiceBase',
        content
    )
    
    content = re.sub(
        r'public\s+class\s+(\w+Service)\s*:\s+BlockchainServiceBase',
        r'public class \1 : ServiceFramework.BlockchainServiceBase',
        content
    )
    
    # Only write if changes were made
    if content != original_content:
        with open(filepath, 'w') as f:
            f.write(content)
        print(f"Fixed: {filename}")
        return True
    
    return False

def main():
    print("=== Fixing Partial Class Base Class References ===\n")
    
    # Find all service files
    service_files = []
    patterns = [
        'src/Services/**/*.cs',
        'src/Advanced/**/*.cs',
        'src/AI/**/*.cs'
    ]
    
    for pattern in patterns:
        service_files.extend(glob.glob(f'/home/ubuntu/neo-service-layer/{pattern}', recursive=True))
    
    fixed_count = 0
    for filepath in service_files:
        if os.path.exists(filepath):
            if fix_partial_class_file(filepath):
                fixed_count += 1
    
    print(f"\n=== Summary ===")
    print(f"Total files fixed: {fixed_count}")
    print(f"Total files scanned: {len(service_files)}")
    print("\nRun 'dotnet build' to verify fixes")

if __name__ == "__main__":
    main()
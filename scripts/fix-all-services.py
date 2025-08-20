#!/usr/bin/env python3
import os
import re
import glob

def fix_service_file(filepath):
    """Fix a service file to properly inherit from base classes"""
    
    with open(filepath, 'r') as f:
        content = f.read()
    
    original_content = content
    filename = os.path.basename(filepath)
    
    # Skip if it's a partial class file or interface
    if 'partial class' in content or 'interface I' in content:
        return False
    
    # Add ServiceFramework using if missing
    if 'using NeoServiceLayer.ServiceFramework;' not in content and 'class' in content:
        # Find the last using statement
        last_using = content.rfind('using ')
        if last_using != -1:
            end_of_line = content.find('\n', last_using)
            if end_of_line != -1:
                content = content[:end_of_line] + '\nusing NeoServiceLayer.ServiceFramework;' + content[end_of_line:]
    
    # Fix ambiguous EnclaveBlockchainServiceBase references
    content = re.sub(
        r':\s+EnclaveBlockchainServiceBase\s*,',
        ': ServiceFramework.EnclaveBlockchainServiceBase,',
        content
    )
    
    # Fix ambiguous BlockchainServiceBase references
    content = re.sub(
        r':\s+BlockchainServiceBase\s*,',
        ': ServiceFramework.BlockchainServiceBase,',
        content
    )
    
    # Fix services that don't inherit from any base class
    patterns_to_fix = [
        (r'public\s+class\s+(\w+Service)\s*:\s*I(\w+Service)', 
         r'public class \1 : ServiceBase, I\2'),
        (r'public\s+class\s+(\w+Service)\s*:\s*I(\w+),\s*IBlockchainService',
         r'public class \1 : BlockchainServiceBase, I\2, IBlockchainService'),
    ]
    
    for pattern, replacement in patterns_to_fix:
        content = re.sub(pattern, replacement, content)
    
    # Fix base constructor calls for services with logger-only constructor
    if ': base(' not in content and 'ILogger<' in content:
        # Find constructor patterns
        ctor_patterns = [
            r'public\s+(\w+Service)\s*\(\s*ILogger<\1>\s+logger\s*\)',
            r'public\s+(\w+Service)\s*\(\s*ILogger<\1>\s+logger,',
        ]
        
        for pattern in ctor_patterns:
            match = re.search(pattern, content)
            if match:
                service_name = match.group(1)
                # Check if it needs a base constructor call
                if f'class {service_name} : ServiceBase' in content or f'class {service_name} : ServiceFramework.ServiceBase' in content:
                    # Find the constructor and add base call
                    ctor_start = match.start()
                    brace_pos = content.find('{', match.end())
                    if brace_pos != -1:
                        # Insert base constructor call
                        base_call = f'\n        : base("{service_name}", "1.0.0", "{service_name} service", logger)'
                        content = content[:brace_pos] + base_call + '\n    ' + content[brace_pos:]
                elif f'class {service_name} : BlockchainServiceBase' in content or 'BlockchainServiceBase,' in content:
                    # Find the constructor and add base call for blockchain service
                    ctor_start = match.start()
                    brace_pos = content.find('{', match.end())
                    if brace_pos != -1:
                        base_call = f'\n        : base("{service_name}", "1.0.0", "{service_name} service", logger, new[] {{ BlockchainType.NeoN3, BlockchainType.NeoX }})'
                        content = content[:brace_pos] + base_call + '\n    ' + content[brace_pos:]
    
    # Replace _logger with Logger
    content = re.sub(r'\b_logger\b', 'Logger', content)
    
    # Only write if changes were made
    if content != original_content:
        with open(filepath, 'w') as f:
            f.write(content)
        print(f"Fixed: {filename}")
        return True
    
    return False

def main():
    print("=== Comprehensive Service Fixes ===\n")
    
    # Find all service files
    service_files = []
    for pattern in ['src/Services/**/*.cs', 'src/Advanced/**/*.cs', 'src/AI/**/*.cs']:
        service_files.extend(glob.glob(f'/home/ubuntu/neo-service-layer/{pattern}', recursive=True))
    
    fixed_count = 0
    for filepath in service_files:
        # Skip test files, interfaces, and certain files
        if any(x in filepath for x in ['Test', 'Interface', '.Tests/', 'Models/', 'Dto/', 'EnclaveOperations']):
            continue
        
        if 'Service' in filepath and filepath.endswith('.cs'):
            if fix_service_file(filepath):
                fixed_count += 1
    
    print(f"\n=== Summary ===")
    print(f"Total files fixed: {fixed_count}")
    print(f"Total files scanned: {len(service_files)}")
    print("\nRun 'dotnet build NeoServiceLayer.sln' to verify fixes")

if __name__ == "__main__":
    main()
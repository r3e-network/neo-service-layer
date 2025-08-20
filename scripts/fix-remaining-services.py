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
    
    # Add ServiceFramework using if missing
    if 'using NeoServiceLayer.ServiceFramework;' not in content and 'EnclaveBlockchainServiceBase' in content:
        # Find the last using statement
        last_using = content.rfind('using ')
        if last_using != -1:
            end_of_line = content.find('\n', last_using)
            if end_of_line != -1:
                content = content[:end_of_line] + '\nusing NeoServiceLayer.ServiceFramework;' + content[end_of_line:]
    
    # Fix ambiguous EnclaveBlockchainServiceBase references
    content = re.sub(
        r':\s+EnclaveBlockchainServiceBase\b',
        ': ServiceFramework.EnclaveBlockchainServiceBase',
        content
    )
    
    # Fix ambiguous BlockchainServiceBase references
    content = re.sub(
        r':\s+BlockchainServiceBase\b',
        ': ServiceFramework.BlockchainServiceBase',
        content
    )
    
    # Fix partial class definitions that use ambiguous base classes
    content = re.sub(
        r'public\s+partial\s+class\s+(\w+Service)\s*:\s*EnclaveBlockchainServiceBase',
        r'public partial class \1 : ServiceFramework.EnclaveBlockchainServiceBase',
        content
    )
    
    content = re.sub(
        r'public\s+partial\s+class\s+(\w+Service)\s*:\s*BlockchainServiceBase',
        r'public partial class \1 : ServiceFramework.BlockchainServiceBase',
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
    print("=== Fixing Remaining Service Implementation Issues ===\n")
    
    # Specific files that need fixing based on build output
    files_to_fix = [
        '/home/ubuntu/neo-service-layer/src/Services/NeoServiceLayer.Services.Oracle/OracleService.Core.cs',
        '/home/ubuntu/neo-service-layer/src/Services/NeoServiceLayer.Services.Oracle/OracleService.cs',
        '/home/ubuntu/neo-service-layer/src/Services/NeoServiceLayer.Services.ProofOfReserve/ProofOfReserveService.cs',
        '/home/ubuntu/neo-service-layer/src/Services/NeoServiceLayer.Services.Randomness/RandomnessService.cs',
        '/home/ubuntu/neo-service-layer/src/Services/NeoServiceLayer.Services.SecretsManagement/SecretsManagementService.cs',
        '/home/ubuntu/neo-service-layer/src/Services/NeoServiceLayer.Services.SmartContracts/SmartContractsService.cs',
        '/home/ubuntu/neo-service-layer/src/Services/NeoServiceLayer.Services.Voting/VotingService.cs',
        '/home/ubuntu/neo-service-layer/src/Services/NeoServiceLayer.Services.ZeroKnowledge/ZeroKnowledgeService.cs',
    ]
    
    # Also search for all service files that might have issues
    service_patterns = [
        'src/Services/**/*Service.cs',
        'src/Services/**/*Service.*.cs',
    ]
    
    all_files = set(files_to_fix)
    for pattern in service_patterns:
        for file in glob.glob(f'/home/ubuntu/neo-service-layer/{pattern}', recursive=True):
            if not any(x in file for x in ['Test', 'Interface', '.Tests/', 'Models/', 'Dto/']):
                all_files.add(file)
    
    fixed_count = 0
    for filepath in all_files:
        if os.path.exists(filepath):
            if fix_service_file(filepath):
                fixed_count += 1
    
    print(f"\n=== Summary ===")
    print(f"Total files fixed: {fixed_count}")
    print(f"Total files scanned: {len(all_files)}")
    print("\nRun 'dotnet build NeoServiceLayer.sln' to verify fixes")

if __name__ == "__main__":
    main()
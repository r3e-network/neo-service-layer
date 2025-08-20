#!/usr/bin/env python3
import os
import re
import sys

# Dictionary of services that need fixing with their base class
services_to_fix = {
    'ComplianceService': 'BlockchainServiceBase',
    'ZeroKnowledgeService': 'BlockchainServiceBase',
    'FairOrderingService': 'BlockchainServiceBase',
    'MonitoringService': 'ServiceBase',
    'BackupService': 'ServiceBase',
    'ConfigurationService': 'ServiceBase',
    'EventSubscriptionService': 'ServiceBase',
    'HealthService': 'ServiceBase',
    'AutomationService': 'ServiceBase'
}

def fix_service_file(filepath, service_name, base_class):
    """Fix a service file to inherit from the correct base class"""
    
    with open(filepath, 'r') as f:
        content = f.read()
    
    # Check if ServiceFramework using is present
    if 'using NeoServiceLayer.ServiceFramework;' not in content:
        # Add it after the last using statement
        last_using_index = content.rfind('using ')
        if last_using_index != -1:
            # Find the end of that line
            end_of_line = content.find('\n', last_using_index)
            if end_of_line != -1:
                content = content[:end_of_line] + '\nusing NeoServiceLayer.ServiceFramework;' + content[end_of_line:]
    
    # Fix the class declaration
    # Pattern to match: public [partial] class ServiceName : IServiceInterface
    pattern = rf'public\s+(partial\s+)?class\s+{service_name}\s*:\s*I'
    replacement = rf'public \1class {service_name} : {base_class}, I'
    content = re.sub(pattern, replacement, content)
    
    # Add constructor if needed (this is simplified - real implementation would need to be more careful)
    if f'{service_name}(ILogger<{service_name}> logger)' in content:
        # Check if we already have base constructor call
        if ': base(' not in content:
            # Find constructor and add base call
            ctor_pattern = rf'public\s+{service_name}\s*\(\s*ILogger<{service_name}>\s+logger\s*\)'
            ctor_match = re.search(ctor_pattern, content)
            if ctor_match:
                # Insert base constructor call
                if base_class == 'ServiceBase':
                    base_call = f'\n            : base("{service_name}", "1.0.0", "{service_name} service", logger)'
                else:  # BlockchainServiceBase
                    base_call = f'\n            : base("{service_name}", "1.0.0", "{service_name} service", logger, new[] {{ BlockchainType.NeoN3, BlockchainType.NeoX }})'
                
                # Find where to insert
                ctor_end = ctor_match.end()
                # Look for the opening brace
                brace_index = content.find('{', ctor_end)
                if brace_index != -1:
                    content = content[:brace_index] + base_call + '\n        ' + content[brace_index:]
    
    # Replace _logger with Logger throughout
    content = re.sub(r'\b_logger\b', 'Logger', content)
    
    with open(filepath, 'w') as f:
        f.write(content)
    
    print(f"Fixed {filepath}")

def find_service_files():
    """Find all service files that need fixing"""
    
    service_files = {}
    
    # Search for service files
    for root, dirs, files in os.walk('/home/ubuntu/neo-service-layer/src'):
        for file in files:
            if file.endswith('.cs'):
                for service_name in services_to_fix:
                    if service_name in file:
                        filepath = os.path.join(root, file)
                        # Skip interface files and test files
                        if 'I' + service_name not in file and 'Test' not in file:
                            service_files[service_name] = filepath
    
    return service_files

def main():
    print("Starting comprehensive service fixes...")
    
    service_files = find_service_files()
    
    for service_name, filepath in service_files.items():
        if os.path.exists(filepath):
            base_class = services_to_fix[service_name]
            fix_service_file(filepath, service_name, base_class)
        else:
            print(f"Warning: File not found for {service_name}")
    
    print("\nFixes completed!")
    print("Run 'dotnet build NeoServiceLayer.sln' to verify")

if __name__ == "__main__":
    main()
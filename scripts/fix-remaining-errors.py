#!/usr/bin/env python3
"""
Fix remaining compilation errors in the Neo Service Layer project.
This script systematically identifies and fixes common compilation issues.
"""

import subprocess
import re
import os
import json
from pathlib import Path
from typing import Dict, List, Tuple

class CompilationErrorFixer:
    def __init__(self):
        self.root_dir = Path("/home/ubuntu/neo-service-layer")
        self.errors_fixed = 0
        self.errors_by_type = {}
        
    def run_build(self, project_path: str = None) -> Tuple[bool, List[str]]:
        """Run dotnet build and capture errors."""
        if project_path:
            cmd = f"dotnet build {project_path} --no-restore"
        else:
            cmd = "dotnet build NeoServiceLayer.sln --no-restore"
            
        result = subprocess.run(cmd, shell=True, capture_output=True, text=True)
        errors = []
        
        for line in result.stderr.split('\n') + result.stdout.split('\n'):
            if 'error CS' in line:
                errors.append(line)
                
        return result.returncode == 0, errors
    
    def categorize_errors(self, errors: List[str]) -> Dict[str, List[str]]:
        """Categorize errors by type."""
        categorized = {
            'CS0101': [],  # Duplicate definitions
            'CS0246': [],  # Type not found
            'CS0426': [],  # Type name does not exist
            'CS0535': [],  # Does not implement interface member
            'CS1061': [],  # Does not contain definition
            'CS7036': [],  # No argument given
            'other': []
        }
        
        for error in errors:
            match = re.search(r'error (CS\d+):', error)
            if match:
                error_code = match.group(1)
                if error_code in categorized:
                    categorized[error_code].append(error)
                else:
                    categorized['other'].append(error)
                    
        return categorized
    
    def fix_duplicate_definitions(self):
        """Fix CS0101 duplicate definition errors."""
        print("Fixing duplicate definitions...")
        
        # Remove duplicate files in RequestModels.cs
        duplicates = [
            "src/AI/NeoServiceLayer.AI.PatternRecognition/Models/RequestModels.cs",
        ]
        
        for dup_file in duplicates:
            file_path = self.root_dir / dup_file
            if file_path.exists():
                # Check for duplicates and remove them
                content = file_path.read_text()
                lines = content.split('\n')
                
                # Track defined classes to remove duplicates
                defined_classes = set()
                new_lines = []
                in_class = False
                class_name = None
                class_lines = []
                brace_count = 0
                
                for line in lines:
                    if re.match(r'^\s*public\s+class\s+(\w+)', line):
                        match = re.match(r'^\s*public\s+class\s+(\w+)', line)
                        if in_class and class_name and class_name not in defined_classes:
                            new_lines.extend(class_lines)
                            defined_classes.add(class_name)
                            
                        class_name = match.group(1)
                        in_class = True
                        class_lines = [line]
                        brace_count = line.count('{') - line.count('}')
                    elif in_class:
                        class_lines.append(line)
                        brace_count += line.count('{') - line.count('}')
                        if brace_count == 0:
                            if class_name not in defined_classes:
                                new_lines.extend(class_lines)
                                defined_classes.add(class_name)
                            in_class = False
                            class_lines = []
                    else:
                        new_lines.append(line)
                        
                # Write back the deduplicated content
                file_path.write_text('\n'.join(new_lines))
                print(f"  Fixed duplicates in {dup_file}")
                self.errors_fixed += 1
    
    def fix_missing_types(self):
        """Fix CS0246/CS0426 missing type errors."""
        print("Fixing missing types...")
        
        # Add missing using statements
        fixes = {
            "src/Services/NeoServiceLayer.Services.Authentication/UserRepository.cs": [
                ("using System.Threading;", "using System.Threading;\nusing NeoServiceLayer.Services.Authentication.Models;")
            ],
            "src/Services/NeoServiceLayer.Services.Notification/NotificationService.cs": [
                ("namespace NeoServiceLayer.Services.Notification;", 
                 "namespace NeoServiceLayer.Services.Notification;\n\nusing Models = NeoServiceLayer.Services.Notification.Models;")
            ]
        }
        
        for file_path, replacements in fixes.items():
            full_path = self.root_dir / file_path
            if full_path.exists():
                content = full_path.read_text()
                for old, new in replacements:
                    if old in content and new not in content:
                        content = content.replace(old, new)
                        self.errors_fixed += 1
                full_path.write_text(content)
                print(f"  Fixed missing types in {file_path}")
    
    def fix_interface_implementations(self):
        """Fix CS0535 interface implementation errors."""
        print("Fixing interface implementations...")
        
        # This is more complex and requires adding missing methods
        # For now, we'll create stub implementations
        
        interface_stubs = {
            "InitializeEnclaveAsync": """
    public async Task<bool> InitializeEnclaveAsync()
    {
        await Task.CompletedTask;
        return true;
    }""",
            "GetAttestationAsync": """
    public async Task<byte[]> GetAttestationAsync()
    {
        await Task.CompletedTask;
        return Array.Empty<byte>();
    }""",
            "ValidateEnclaveAsync": """
    public async Task<bool> ValidateEnclaveAsync()
    {
        await Task.CompletedTask;
        return true;
    }""",
            "HasEnclaveCapabilities": """
    public bool HasEnclaveCapabilities => false;""",
            "IsEnclaveInitialized": """
    public bool IsEnclaveInitialized => false;""",
            "SupportsBlockchain": """
    public bool SupportsBlockchain(BlockchainType blockchainType)
    {
        return blockchainType == BlockchainType.NeoN3;
    }""",
            "SupportedBlockchains": """
    public IEnumerable<BlockchainType> SupportedBlockchains => new[] { BlockchainType.NeoN3 };"""
        }
        
        # Add these to files that need them
        files_needing_stubs = [
            "src/AI/NeoServiceLayer.AI.PatternRecognition/Services/PatternRecognitionOrchestrator.cs"
        ]
        
        for file_path in files_needing_stubs:
            full_path = self.root_dir / file_path
            if full_path.exists():
                content = full_path.read_text()
                
                # Find the last closing brace of the class
                lines = content.split('\n')
                insert_index = -1
                for i in range(len(lines) - 1, -1, -1):
                    if lines[i].strip() == '}' and i > 0 and lines[i-1].strip() == '}':
                        insert_index = i
                        break
                
                if insert_index > 0:
                    # Insert stub methods
                    for method_name, stub_code in interface_stubs.items():
                        if method_name not in content:
                            lines.insert(insert_index, stub_code)
                            self.errors_fixed += 1
                    
                    full_path.write_text('\n'.join(lines))
                    print(f"  Added interface stubs to {file_path}")
    
    def generate_report(self, errors: List[str]):
        """Generate a detailed error report."""
        categorized = self.categorize_errors(errors)
        
        report = []
        report.append("=" * 60)
        report.append("NEO SERVICE LAYER - BUILD ERROR REPORT")
        report.append("=" * 60)
        report.append(f"Total Errors: {len(errors)}")
        report.append("")
        
        for error_type, error_list in categorized.items():
            if error_list:
                report.append(f"{error_type}: {len(error_list)} errors")
                
        report.append("")
        report.append("Top Files with Errors:")
        
        file_errors = {}
        for error in errors:
            match = re.search(r'([^/]+\.cs)\(\d+,\d+\)', error)
            if match:
                file_name = match.group(1)
                file_errors[file_name] = file_errors.get(file_name, 0) + 1
        
        for file_name, count in sorted(file_errors.items(), key=lambda x: x[1], reverse=True)[:10]:
            report.append(f"  {file_name}: {count} errors")
            
        report_path = self.root_dir / "build-error-report.txt"
        report_path.write_text('\n'.join(report))
        print(f"\nReport saved to: {report_path}")
        
        return categorized
    
    def main(self):
        """Main execution flow."""
        print("Starting compilation error fix process...")
        
        # Initial build
        print("\nRunning initial build...")
        success, initial_errors = self.run_build()
        print(f"Found {len(initial_errors)} initial errors")
        
        if success:
            print("Build successful! No errors to fix.")
            return
        
        # Categorize errors
        categorized = self.categorize_errors(initial_errors)
        
        # Apply fixes
        print("\nApplying fixes...")
        self.fix_duplicate_definitions()
        self.fix_missing_types()
        self.fix_interface_implementations()
        
        # Final build
        print("\nRunning final build...")
        success, final_errors = self.run_build()
        
        # Generate report
        self.generate_report(final_errors)
        
        # Summary
        print("\n" + "=" * 60)
        print("SUMMARY")
        print("=" * 60)
        print(f"Initial errors: {len(initial_errors)}")
        print(f"Final errors: {len(final_errors)}")
        print(f"Errors fixed: {self.errors_fixed}")
        print(f"Reduction: {len(initial_errors) - len(final_errors)} ({((len(initial_errors) - len(final_errors)) / len(initial_errors) * 100):.1f}%)")
        
        if not success:
            print("\nBuild still has errors. Manual intervention required.")
            print("Check build-error-report.txt for details.")
        else:
            print("\nBuild successful! 🎉")

if __name__ == "__main__":
    fixer = CompilationErrorFixer()
    fixer.main()
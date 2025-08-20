#!/usr/bin/env python3
"""
Analyze and fix build errors in the Neo Service Layer project
"""

import subprocess
import os
from pathlib import Path
import re
import json

class BuildAnalyzer:
    def __init__(self):
        self.project_root = Path("/home/ubuntu/neo-service-layer")
        os.chdir(self.project_root)
        self.errors = []
        self.warnings = []
        
    def build_and_analyze(self):
        """Build the solution and analyze errors"""
        print("=" * 80)
        print("NEO SERVICE LAYER - BUILD ANALYSIS")
        print("=" * 80)
        print()
        
        # First, restore packages
        print("Restoring NuGet packages...")
        restore_result = subprocess.run(
            ["dotnet", "restore", "NeoServiceLayer.sln"],
            capture_output=True,
            text=True,
            timeout=120
        )
        
        if restore_result.returncode == 0:
            print("✅ Package restore successful")
        else:
            print("⚠️ Package restore had issues")
        print()
        
        # Build and capture detailed output
        print("Building solution...")
        build_result = subprocess.run(
            ["dotnet", "build", "NeoServiceLayer.sln", 
             "--configuration", "Release", 
             "--no-restore",
             "-v", "normal"],
            capture_output=True,
            text=True,
            timeout=180
        )
        
        # Parse build output
        self.parse_build_output(build_result.stdout + build_result.stderr)
        
        # Analyze results
        print(f"\n📊 Build Analysis Results:")
        print(f"  Errors: {len(self.errors)}")
        print(f"  Warnings: {len(self.warnings)}")
        
        if self.errors:
            print("\n❌ Build Errors:")
            self.display_errors()
        
        if self.warnings:
            print(f"\n⚠️ Warnings: {len(self.warnings)} (showing first 10)")
            for warning in self.warnings[:10]:
                print(f"  - {warning[:150]}")
        
        return build_result.returncode == 0
    
    def parse_build_output(self, output):
        """Parse build output for errors and warnings"""
        lines = output.split('\n')
        
        for line in lines:
            if ': error ' in line:
                self.errors.append(line.strip())
            elif ': warning ' in line:
                self.warnings.append(line.strip())
    
    def display_errors(self):
        """Display categorized errors"""
        error_categories = {}
        
        for error in self.errors:
            # Extract error code
            match = re.search(r'error (\w+\d+):', error)
            if match:
                error_code = match.group(1)
                if error_code not in error_categories:
                    error_categories[error_code] = []
                error_categories[error_code].append(error)
            else:
                if 'Other' not in error_categories:
                    error_categories['Other'] = []
                error_categories['Other'].append(error)
        
        for category, errors in sorted(error_categories.items()):
            print(f"\n  {category}: {len(errors)} errors")
            for error in errors[:3]:
                # Extract file and message
                if '.cs(' in error:
                    parts = error.split('.cs(')
                    if len(parts) > 1:
                        file_part = parts[0].split('/')[-1] + '.cs'
                        msg_part = parts[1].split(':', 2)[-1] if ':' in parts[1] else parts[1]
                        print(f"    {file_part}: {msg_part[:100]}")
                else:
                    print(f"    {error[:150]}")
    
    def find_source_files(self):
        """Find all C# source files"""
        return list(self.project_root.glob("src/**/*.cs"))
    
    def check_missing_implementations(self):
        """Check for missing interface implementations"""
        print("\n🔍 Checking for missing implementations...")
        
        source_files = self.find_source_files()
        issues = []
        
        for file in source_files:
            if file.is_file():
                content = file.read_text()
                
                # Check for interfaces without implementations
                if 'interface I' in content and 'throw new NotImplementedException()' in content:
                    issues.append(f"NotImplementedException in {file.relative_to(self.project_root)}")
                
                # Check for partial classes without all parts
                if 'partial class' in content:
                    class_match = re.search(r'partial class (\w+)', content)
                    if class_match:
                        class_name = class_match.group(1)
                        # Check if there are other parts
                        other_parts = list(file.parent.glob(f"*{class_name}*.cs"))
                        if len(other_parts) == 1:
                            issues.append(f"Partial class {class_name} may be missing parts")
        
        if issues:
            print(f"  Found {len(issues)} potential issues:")
            for issue in issues[:10]:
                print(f"    - {issue}")
        else:
            print("  ✅ No obvious implementation issues found")
        
        return issues

    def analyze_project_structure(self):
        """Analyze overall project structure"""
        print("\n📁 Project Structure Analysis:")
        
        # Count project files
        csproj_files = list(self.project_root.glob("**/*.csproj"))
        src_projects = [p for p in csproj_files if '/src/' in str(p)]
        test_projects = [p for p in csproj_files if '/tests/' in str(p)]
        
        print(f"  Total projects: {len(csproj_files)}")
        print(f"  Source projects: {len(src_projects)}")
        print(f"  Test projects: {len(test_projects)}")
        
        # Check for key components
        key_components = {
            "API": self.project_root / "src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj",
            "Core": self.project_root / "src/Core/NeoServiceLayer.Core/NeoServiceLayer.Core.csproj",
            "Infrastructure": any(self.project_root.glob("src/Infrastructure/**/*.csproj")),
            "Services": any(self.project_root.glob("src/Services/**/*.csproj"))
        }
        
        print("\n  Key Components:")
        for component, exists in key_components.items():
            status = "✅" if exists else "❌"
            print(f"    {status} {component}")
        
        return len(csproj_files)

if __name__ == "__main__":
    analyzer = BuildAnalyzer()
    
    # Analyze project structure first
    analyzer.analyze_project_structure()
    
    # Build and analyze errors
    success = analyzer.build_and_analyze()
    
    # Check for missing implementations
    analyzer.check_missing_implementations()
    
    if success:
        print("\n✅ Build successful!")
    else:
        print("\n❌ Build failed - fixes needed")
#!/usr/bin/env python3
"""
Fix MSBuild issue and build remaining test projects
"""

import subprocess
import os
from pathlib import Path
import json
import xml.etree.ElementTree as ET

class MSBuildFixer:
    def __init__(self):
        self.project_root = Path("/home/ubuntu/neo-service-layer")
        os.chdir(self.project_root)
        self.fixed_projects = []
        self.failed_projects = []
        
    def find_unbuild_projects(self):
        """Find test projects that don't have built DLLs"""
        all_projects = list(self.project_root.glob("tests/**/*.Tests.csproj"))
        built_dlls = set(dll.stem for dll in self.project_root.glob("tests/**/bin/Release/net9.0/*.Tests.dll"))
        
        unbuilt = []
        for proj in all_projects:
            if proj.stem not in built_dlls:
                unbuilt.append(proj)
        
        return sorted(unbuilt)
    
    def build_with_msbuild_directly(self, project_path):
        """Build using MSBuild directly with proper arguments"""
        try:
            # Use MSBuild directly with explicit arguments
            result = subprocess.run(
                ["/usr/lib/dotnet/sdk/9.0.107/MSBuild.dll", 
                 str(project_path),
                 "-p:Configuration=Release",
                 "-v:quiet",
                 "-nologo"],
                capture_output=True,
                text=True,
                timeout=60,
                cwd=str(self.project_root)
            )
            
            return result.returncode == 0, result.stderr
        except Exception as e:
            return False, str(e)
    
    def build_with_dotnet_publish(self, project_path):
        """Try building with dotnet publish instead"""
        try:
            result = subprocess.run(
                ["dotnet", "publish", 
                 str(project_path),
                 "-c", "Release",
                 "-o", str(project_path.parent / "bin" / "Release" / "net9.0"),
                 "--no-restore"],
                capture_output=True,
                text=True,
                timeout=60
            )
            
            return result.returncode == 0, result.stderr
        except Exception as e:
            return False, str(e)
    
    def create_build_script(self, project_path):
        """Create a shell script to build the project"""
        script_content = f"""#!/bin/bash
cd {self.project_root}
export MSBUILDDISABLENODEREUSE=1
dotnet build "{project_path}" -c Release --no-restore
"""
        script_path = self.project_root / "temp_build.sh"
        with open(script_path, "w") as f:
            f.write(script_content)
        
        os.chmod(script_path, 0o755)
        
        try:
            result = subprocess.run(
                ["bash", str(script_path)],
                capture_output=True,
                text=True,
                timeout=60
            )
            return result.returncode == 0, result.stderr
        finally:
            script_path.unlink(missing_ok=True)
    
    def fix_and_build_all(self):
        """Try multiple methods to build all projects"""
        print("=" * 80)
        print("FIXING MSBUILD AND BUILDING REMAINING PROJECTS")
        print("=" * 80)
        print()
        
        unbuilt = self.find_unbuild_projects()
        print(f"Found {len(unbuilt)} projects without built DLLs")
        print()
        
        for i, project in enumerate(unbuilt, 1):
            project_name = project.stem
            print(f"[{i:2d}/{len(unbuilt)}] {project_name}")
            
            # Try method 1: dotnet publish
            success, error = self.build_with_dotnet_publish(project)
            if success:
                print(f"  ✅ Built with dotnet publish")
                self.fixed_projects.append(project_name)
            else:
                # Try method 2: shell script
                success, error = self.create_build_script(project)
                if success:
                    print(f"  ✅ Built with shell script")
                    self.fixed_projects.append(project_name)
                else:
                    print(f"  ❌ Build failed")
                    self.failed_projects.append({
                        "name": project_name,
                        "error": error.split('\n')[0] if error else "Unknown"
                    })
            
            print()
        
        self.print_summary()
    
    def print_summary(self):
        """Print build summary"""
        print("=" * 80)
        print("BUILD SUMMARY")
        print("=" * 80)
        print(f"Successfully built: {len(self.fixed_projects)}")
        print(f"Failed to build: {len(self.failed_projects)}")
        
        if self.fixed_projects:
            print("\nNewly built projects:")
            for proj in self.fixed_projects[:10]:
                print(f"  ✅ {proj}")
        
        if self.failed_projects:
            print("\nStill failing:")
            for proj in self.failed_projects[:10]:
                print(f"  ❌ {proj['name']}")

if __name__ == "__main__":
    fixer = MSBuildFixer()
    fixer.fix_and_build_all()
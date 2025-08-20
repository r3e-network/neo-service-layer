#!/usr/bin/env python3
"""
Create missing .csproj files for test projects that have test code but no project file
"""

import os
from pathlib import Path

def create_test_project_file(project_dir, project_name):
    """Create a .csproj file for a test project"""
    
    csproj_content = f"""<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="6.12.2" />
    <PackageReference Include="Moq" Version="4.20.72" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference the project being tested -->
    <ProjectReference Include="..\\..\\..\\src\\Services\\{project_name.replace('.Tests', '')}\\{project_name.replace('.Tests', '')}.csproj" />
  </ItemGroup>

</Project>"""
    
    csproj_path = project_dir / f"{project_name}.csproj"
    
    if not csproj_path.exists():
        with open(csproj_path, 'w') as f:
            f.write(csproj_content)
        return True, f"Created: {csproj_path}"
    else:
        return False, f"Already exists: {csproj_path}"

def main():
    project_root = Path("/home/ubuntu/neo-service-layer")
    tests_dir = project_root / "tests"
    
    print("Creating missing test project files...")
    print("=" * 60)
    
    created_count = 0
    skipped_count = 0
    errors = []
    
    # Find all test directories
    for test_dir in tests_dir.rglob("*.Tests"):
        if test_dir.is_dir():
            project_name = test_dir.name
            
            # Check if .cs files exist but no .csproj
            cs_files = list(test_dir.glob("*.cs"))
            csproj_files = list(test_dir.glob("*.csproj"))
            
            if cs_files and not csproj_files:
                print(f"\nProcessing: {project_name}")
                print(f"  Found {len(cs_files)} .cs files")
                
                try:
                    created, message = create_test_project_file(test_dir, project_name)
                    if created:
                        print(f"  ✅ {message}")
                        created_count += 1
                    else:
                        print(f"  ⏭️ {message}")
                        skipped_count += 1
                except Exception as e:
                    print(f"  ❌ Error: {e}")
                    errors.append(f"{project_name}: {e}")
    
    # Also check for specific service test projects
    service_test_projects = [
        "NeoServiceLayer.Services.Authentication.Tests",
        "NeoServiceLayer.Services.Backup.Tests",
        "NeoServiceLayer.Services.Storage.Tests",
        "NeoServiceLayer.Services.Monitoring.Tests",
        "NeoServiceLayer.Services.Oracle.Tests",
        "NeoServiceLayer.Services.KeyManagement.Tests",
        "NeoServiceLayer.Services.Voting.Tests",
        "NeoServiceLayer.Services.CrossChain.Tests",
        "NeoServiceLayer.Services.ZeroKnowledge.Tests",
        "NeoServiceLayer.Services.Randomness.Tests",
    ]
    
    for project_name in service_test_projects:
        test_dir = tests_dir / "Services" / project_name
        if test_dir.exists():
            cs_files = list(test_dir.glob("*.cs"))
            csproj_files = list(test_dir.glob("*.csproj"))
            
            if cs_files and not csproj_files:
                print(f"\nProcessing service test: {project_name}")
                try:
                    created, message = create_test_project_file(test_dir, project_name)
                    if created:
                        print(f"  ✅ {message}")
                        created_count += 1
                    else:
                        print(f"  ⏭️ {message}")
                        skipped_count += 1
                except Exception as e:
                    print(f"  ❌ Error: {e}")
                    errors.append(f"{project_name}: {e}")
    
    print("\n" + "=" * 60)
    print("SUMMARY")
    print("=" * 60)
    print(f"Created: {created_count} project files")
    print(f"Skipped: {skipped_count} (already exist)")
    print(f"Errors: {len(errors)}")
    
    if errors:
        print("\nErrors encountered:")
        for error in errors:
            print(f"  - {error}")
    
    return created_count > 0

if __name__ == "__main__":
    success = main()
    exit(0 if success else 1)
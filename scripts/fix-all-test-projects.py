#!/usr/bin/env python3
"""
Create .csproj files for all test directories with .cs files but missing project files
"""

import os
from pathlib import Path

# Template for test project files
TEST_PROJECT_TEMPLATE = """<Project Sdk="Microsoft.NET.Sdk">

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
    {extra_packages}
  </ItemGroup>

  <ItemGroup>
    {project_references}
  </ItemGroup>

</Project>"""

def determine_project_references(test_name):
    """Determine which projects to reference based on test name"""
    references = []
    
    # Remove .Tests suffix to get base name
    base_name = test_name.replace(".Tests", "")
    
    # Map test projects to their dependencies
    if "Authentication" in test_name:
        references.append(f"..\\..\\..\\src\\Services\\NeoServiceLayer.Services.Authentication\\NeoServiceLayer.Services.Authentication.csproj")
    elif "Backup" in test_name:
        references.append(f"..\\..\\..\\src\\Services\\NeoServiceLayer.Services.Backup\\NeoServiceLayer.Services.Backup.csproj")
    elif "Storage" in test_name:
        references.append(f"..\\..\\..\\src\\Services\\NeoServiceLayer.Services.Storage\\NeoServiceLayer.Services.Storage.csproj")
    elif "Monitoring" in test_name:
        references.append(f"..\\..\\..\\src\\Services\\NeoServiceLayer.Services.Monitoring\\NeoServiceLayer.Services.Monitoring.csproj")
    elif "Oracle" in test_name:
        references.append(f"..\\..\\..\\src\\Services\\NeoServiceLayer.Services.Oracle\\NeoServiceLayer.Services.Oracle.csproj")
    elif "KeyManagement" in test_name:
        references.append(f"..\\..\\..\\src\\Services\\NeoServiceLayer.Services.KeyManagement\\NeoServiceLayer.Services.KeyManagement.csproj")
    elif "Voting" in test_name:
        references.append(f"..\\..\\..\\src\\Services\\NeoServiceLayer.Services.Voting\\NeoServiceLayer.Services.Voting.csproj")
    elif "CrossChain" in test_name:
        references.append(f"..\\..\\..\\src\\Services\\NeoServiceLayer.Services.CrossChain\\NeoServiceLayer.Services.CrossChain.csproj")
    elif "Api.Controllers" in test_name:
        references.append(f"..\\..\\..\\src\\Api\\NeoServiceLayer.Api\\NeoServiceLayer.Api.csproj")
    elif "Integration" in test_name:
        # Integration tests might need multiple references
        references.append(f"..\\..\\..\\src\\Core\\NeoServiceLayer.Core\\NeoServiceLayer.Core.csproj")
    else:
        # Default to Core reference
        references.append(f"..\\..\\..\\src\\Core\\NeoServiceLayer.Core\\NeoServiceLayer.Core.csproj")
    
    # Format as XML
    return "\n    ".join([f'<ProjectReference Include="{ref}" />' for ref in references])

def determine_extra_packages(test_name):
    """Determine extra packages needed based on test type"""
    extra = []
    
    if "Performance" in test_name:
        extra.append('<PackageReference Include="BenchmarkDotNet" Version="0.14.0" />')
    if "Integration" in test_name:
        extra.append('<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />')
    if "Api" in test_name or "Controllers" in test_name:
        extra.append('<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />')
    
    return "\n    ".join(extra) if extra else ""

def create_project_file(test_dir):
    """Create a .csproj file for a test directory"""
    project_name = test_dir.name
    csproj_path = test_dir / f"{project_name}.csproj"
    
    if csproj_path.exists():
        return False, "Already exists"
    
    # Generate project content
    project_refs = determine_project_references(project_name)
    extra_packages = determine_extra_packages(project_name)
    
    content = TEST_PROJECT_TEMPLATE.format(
        project_references=project_refs,
        extra_packages=extra_packages
    )
    
    # Write project file
    with open(csproj_path, 'w') as f:
        f.write(content)
    
    return True, "Created"

def main():
    project_root = Path("/home/ubuntu/neo-service-layer")
    tests_dir = project_root / "tests"
    
    print("Fixing all test projects...")
    print("=" * 60)
    
    created = 0
    skipped = 0
    
    # Find all directories with test .cs files
    for test_dir in tests_dir.rglob("*Tests"):
        if test_dir.is_dir():
            cs_files = list(test_dir.glob("*.cs"))
            if cs_files:
                print(f"\nChecking: {test_dir.name}")
                print(f"  C# files: {len(cs_files)}")
                
                success, message = create_project_file(test_dir)
                if success:
                    print(f"  ✅ {message}")
                    created += 1
                else:
                    print(f"  ⏭️ {message}")
                    skipped += 1
    
    print("\n" + "=" * 60)
    print(f"Created: {created} project files")
    print(f"Skipped: {skipped} project files")
    print("=" * 60)
    
    if created > 0:
        print("\nNow building all test projects...")
        # Try to restore packages
        import subprocess
        result = subprocess.run(
            ["dotnet", "restore"],
            capture_output=True,
            text=True,
            cwd=str(project_root)
        )
        if result.returncode == 0:
            print("✅ Package restore successful")
        else:
            print("⚠️ Package restore had issues")

if __name__ == "__main__":
    main()
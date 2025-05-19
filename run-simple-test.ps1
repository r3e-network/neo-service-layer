# Run a simple test for the Neo Service Layer

Write-Host "Running simple test for Neo Service Layer..." -ForegroundColor Green

# Create a simple test file
$testDir = "tests/SimpleTest"
$testFile = "$testDir/SimpleTest.cs"

# Create directory if it doesn't exist
if (-not (Test-Path $testDir)) {
    New-Item -ItemType Directory -Path $testDir -Force | Out-Null
}

# Create a simple test file
$testContent = @"
using System;
using Xunit;

namespace SimpleTest
{
    public class SimpleTests
    {
        [Fact]
        public void SimpleTest_AlwaysPasses()
        {
            // This test always passes
            Assert.True(true);
        }

        [Fact]
        public void SimpleTest_StringComparison()
        {
            // Simple string comparison
            string expected = "Neo Service Layer";
            string actual = "Neo Service Layer";
            Assert.Equal(expected, actual);
        }
    }
}
"@

# Write the test file
Set-Content -Path $testFile -Value $testContent

# Create a simple project file
$projectFile = "$testDir/SimpleTest.csproj"
$projectContent = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageReference Include="xunit" Version="2.7.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.7">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

</Project>
"@

# Write the project file
Set-Content -Path $projectFile -Value $projectContent

# Run the test
Write-Host "Running test..." -ForegroundColor Cyan
dotnet test $projectFile --logger "console;verbosity=detailed"

Write-Host "Test completed!" -ForegroundColor Green

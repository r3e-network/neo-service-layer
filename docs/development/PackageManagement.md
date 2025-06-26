# Package Management Guide

## Overview

This document describes the centralized package management approach for the Neo Service Layer project. We use .NET's Central Package Management feature to ensure consistent package versions across all projects.

## Central Package Management

All package versions are defined in the `Directory.Build.props` file at the solution root. This provides:

- **Consistency**: All projects use the same version of each package
- **Maintainability**: Update package versions in one place
- **Clarity**: Easy to see all dependencies at a glance
- **Conflict Resolution**: Prevents version conflicts between projects

## Package Categories

### Microsoft Extensions (19 packages)
Core infrastructure packages for dependency injection, configuration, logging, and hosting.

**Key Packages:**
- `Microsoft.Extensions.DependencyInjection` (9.0.0)
- `Microsoft.Extensions.Configuration` (9.0.1)
- `Microsoft.Extensions.Logging` (9.0.0)
- `Microsoft.Extensions.Hosting` (9.0.0)

### ASP.NET Core (5 packages)
Web framework packages for building APIs and web applications.

**Key Packages:**
- `Microsoft.AspNetCore.Authentication.JwtBearer` (9.0.0)
- `Microsoft.AspNetCore.OpenApi` (9.0.0)
- `Swashbuckle.AspNetCore` (9.0.1)

### Testing Framework (10 packages)
Comprehensive testing tools including unit testing, mocking, and assertions.

**Key Packages:**
- `xunit` (2.9.3)
- `Moq` (4.20.72)
- `FluentAssertions` (7.0.0)
- `Microsoft.NET.Test.Sdk` (17.13.0)

### Blockchain Integration
- **NEO** (6 packages): Neo blockchain integration
- **Ethereum** (9 packages): Nethereum for Ethereum/EVM compatibility

### Machine Learning (5 packages)
Microsoft ML.NET packages for AI/ML capabilities.

**Version**: 5.0.0-preview.1.25127.4 (preview version for latest features)

### Other Infrastructure
- **Logging**: Serilog (4.2.0)
- **Database**: Npgsql (9.0.2)
- **Caching**: StackExchange.Redis (2.8.16)
- **JSON**: System.Text.Json (9.0.6), Newtonsoft.Json (13.0.3)

## Version Strategy

### .NET 9.0 Alignment
Most Microsoft packages are aligned with .NET 9.0:
- Microsoft.Extensions.*: 9.0.0 or 9.0.1
- Microsoft.AspNetCore.*: 9.0.0
- System.Text.Json: 9.0.6

### Exceptions
- `Microsoft.Extensions.DependencyInjection.Abstractions`: 9.0.5 (newer patch)
- `Microsoft.Extensions.Logging.Abstractions`: 9.0.5 (newer patch)
- `Microsoft.ML.*`: Preview versions for latest features
- `Microsoft.IdentityModel.*`: 8.12.1 (latest stable)

## Migration Guide

### Converting to Central Package Management

1. **Backup your project files** (automated by migration script)

2. **Run the migration script**:
   ```bash
   python3 migrate_to_central_packages.py
   ```

3. **Verify the migration**:
   ```bash
   dotnet restore
   dotnet build
   ```

### Adding New Packages

1. Add the package version to `Directory.Build.props`:
   ```xml
   <PackageVersion Include="PackageName" Version="1.2.3" />
   ```

2. Reference the package in your `.csproj` without version:
   ```xml
   <PackageReference Include="PackageName" />
   ```

### Updating Package Versions

1. Update the version in `Directory.Build.props`
2. Run `dotnet restore` to update all projects

## Version Inconsistencies Found

The following packages had multiple versions before centralization:

| Package | Versions | Resolution |
|---------|----------|------------|
| FluentAssertions | 7.0.0, 6.12.2 | → 7.0.0 |
| Microsoft.EntityFrameworkCore.InMemory | 9.0.0, 8.0.0 | → 9.0.0 |
| Microsoft.Extensions.Configuration | 9.0.3, 9.0.1, 9.0.0 | → 9.0.1 |
| Microsoft.Extensions.DependencyInjection | 9.0.1, 9.0.0 | → 9.0.0 |
| Microsoft.ML.* | 5.0.0-preview, 4.0.1 | → 5.0.0-preview |
| Swashbuckle.AspNetCore | 9.0.1, 7.2.0 | → 9.0.1 |

## Best Practices

1. **Regular Updates**: Review and update package versions quarterly
2. **Security Patches**: Apply security updates immediately
3. **Preview Packages**: Use preview packages only when necessary (e.g., ML.NET)
4. **Version Alignment**: Keep related packages at the same version
5. **Testing**: Always test thoroughly after updating packages

## Troubleshooting

### Package Version Conflicts
If you encounter version conflicts:
1. Check `Directory.Build.props` for the centralized version
2. Ensure no `.csproj` files have explicit versions
3. Run `dotnet restore --force`

### Missing Packages
If a package is not found:
1. Verify it's listed in `Directory.Build.props`
2. Check the package name spelling
3. Ensure the package source is configured

## Package Statistics

- **Total Projects**: 78
- **Total Unique Packages**: 87
- **Most Used Package**: Microsoft.NET.Test.Sdk (36 projects)
- **Largest Category**: Microsoft Extensions (19 packages)

## Maintenance

### Monthly Tasks
- Review for new package updates
- Check for security advisories
- Update preview packages to stable versions when available

### Quarterly Tasks
- Major version updates
- Remove unused packages
- Consolidate similar packages

## References

- [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [Directory.Build.props](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build)
- [Package Reference Format](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files)
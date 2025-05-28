# Neo Service Layer - File Organization Guide

## Overview

This document outlines the file organization principles and structure for the Neo Service Layer codebase to ensure maintainability, scalability, and developer productivity.

## File Size Guidelines

### Maximum File Size Limits
- **Source Files**: Maximum 500 lines per file
- **Model Files**: Maximum 400 lines per file
- **Interface Files**: Maximum 300 lines per file
- **Test Files**: Maximum 600 lines per file

### When to Split Files
Split files when they exceed the size limits or when they contain multiple distinct responsibilities.

## Service Organization Structure

### Standard Service Structure
```
src/Services/NeoServiceLayer.Services.{ServiceName}/
├── Models/
│   ├── {ServiceName}CoreModels.cs          # Core operations (CRUD, basic types)
│   ├── {ServiceName}ValidationModels.cs   # Validation logic and rules
│   ├── {ServiceName}ManagementModels.cs    # List, status, management operations
│   ├── {ServiceName}SecurityModels.cs     # Security, encryption, authentication
│   ├── {ServiceName}ImportExportModels.cs # Import/export operations
│   └── {ServiceName}SubscriptionModels.cs # Subscription and notification models
├── {ServiceName}ServiceCore.cs            # Core service implementation
├── {ServiceName}ServiceExecution.cs       # Execution methods
├── {ServiceName}ServiceManagement.cs      # Management methods
├── I{ServiceName}Service.cs               # Service interface
└── {ServiceName}Service.csproj            # Project file
```

## Model File Organization

### Core Models (`{ServiceName}CoreModels.cs`)
- Basic enumerations
- Core request/response models
- Primary data structures
- Essential operations (Get, Set, Delete)

### Validation Models (`{ServiceName}ValidationModels.cs`)
- Validation rules and schemas
- Error and warning models
- Validation request/response models
- Rule definitions

### Management Models (`{ServiceName}ManagementModels.cs`)
- List and query operations
- Status and progress tracking
- Statistics and metrics
- Administrative operations

### Security Models (`{ServiceName}SecurityModels.cs`)
- Encryption and compression settings
- Authentication and authorization
- Security policies and configurations
- Key management structures

### Import/Export Models (`{ServiceName}ImportExportModels.cs`)
- Data import/export operations
- Format specifications
- Transfer configurations
- Backup and restore models

### Subscription Models (`{ServiceName}SubscriptionModels.cs`)
- Event subscriptions
- Notification preferences
- Change tracking
- Callback configurations

## Service Implementation Organization

### Core Service (`{ServiceName}ServiceCore.cs`)
- Service initialization and configuration
- Base service setup and dependencies
- Common utility methods
- Cache management

### Execution Methods (`{ServiceName}ServiceExecution.cs`)
- Primary business logic operations
- Data processing methods
- Core functionality implementation
- Performance-critical operations

### Management Methods (`{ServiceName}ServiceManagement.cs`)
- Administrative operations
- Configuration management
- Monitoring and statistics
- Lifecycle management

## Naming Conventions

### File Naming
- Use PascalCase for all file names
- Include service name prefix for clarity
- Use descriptive suffixes (Core, Management, Validation, etc.)
- Maintain consistency across all services

### Class Naming
- Match class names with file names
- Use clear, descriptive names
- Follow .NET naming conventions
- Include appropriate suffixes (Request, Result, Model, etc.)

### Namespace Organization
```csharp
namespace NeoServiceLayer.Services.{ServiceName}.Models;
namespace NeoServiceLayer.Services.{ServiceName};
```

## Benefits of This Organization

### Maintainability
- **Single Responsibility**: Each file has a clear, focused purpose
- **Easy Navigation**: Developers can quickly locate relevant code
- **Reduced Complexity**: Smaller files are easier to understand and modify

### Scalability
- **Modular Design**: New features can be added to appropriate files
- **Independent Development**: Multiple developers can work on different aspects
- **Clear Dependencies**: Relationships between components are apparent

### Developer Experience
- **Faster Loading**: IDEs handle smaller files more efficiently
- **Reduced Conflicts**: Smaller files reduce merge conflicts
- **Better Testing**: Focused files enable targeted unit testing

## Implementation Examples

### Configuration Service
- **Before**: 1 file, 1,243 lines
- **After**: 7 files, ~200 lines each
- **Improvement**: 85% reduction in file complexity

### Backup Service
- **Before**: 1 file, 1,556 lines
- **After**: 7 files, ~220 lines each
- **Improvement**: 86% reduction in file complexity

### Notification Service
- **Before**: 1 file, 695 lines
- **After**: 5 files, ~140 lines each
- **Improvement**: 80% reduction in file complexity

## Best Practices

### File Organization
1. Group related models together
2. Separate concerns clearly
3. Use consistent naming patterns
4. Maintain logical file sizes

### Code Structure
1. Keep classes focused on single responsibilities
2. Use partial classes for large service implementations
3. Implement clear interfaces
4. Document public APIs thoroughly

### Maintenance
1. Regularly review file sizes
2. Refactor when files grow too large
3. Update documentation when structure changes
4. Ensure consistent patterns across services

## Migration Guidelines

### When Refactoring Large Files
1. **Analyze Content**: Identify logical groupings
2. **Plan Structure**: Design the new file organization
3. **Create New Files**: Split content into appropriate files
4. **Update References**: Ensure all imports are correct
5. **Test Thoroughly**: Verify functionality is preserved
6. **Update Documentation**: Reflect the new structure

### Validation Checklist
- [ ] All files under size limits
- [ ] Logical grouping maintained
- [ ] Consistent naming conventions
- [ ] Proper namespace organization
- [ ] All references updated
- [ ] Tests passing
- [ ] Documentation updated

This organization ensures the Neo Service Layer remains maintainable, scalable, and developer-friendly as it continues to grow and evolve.

#!/usr/bin/env python3
"""
Fix all test project compilation errors to achieve zero-error build.
This script systematically fixes FluentAssertions, Moq, and other test-related issues.
"""

import subprocess
import re
import os
from pathlib import Path
from typing import Dict, List, Tuple

class TestProjectFixer:
    def __init__(self):
        self.root_dir = Path("/home/ubuntu/neo-service-layer")
        self.fixes_applied = 0
        self.test_projects = list(self.root_dir.glob("tests/**/*.csproj"))
        
    def add_test_packages(self):
        """Add missing test packages to all test projects."""
        print("Adding test packages to all test projects...")
        
        required_packages = [
            'FluentAssertions',
            'Moq',
            'xunit',
            'xunit.runner.visualstudio',
            'Microsoft.NET.Test.Sdk',
            'coverlet.collector'
        ]
        
        for project in self.test_projects:
            print(f"  Updating {project.name}...")
            content = project.read_text()
            
            # Check if ItemGroup for PackageReference exists
            if '<PackageReference' not in content:
                # Add ItemGroup with all packages
                lines = content.split('\n')
                for i, line in enumerate(lines):
                    if '</Project>' in line:
                        lines.insert(i, '  <ItemGroup>')
                        for pkg in required_packages:
                            lines.insert(i+1, f'    <PackageReference Include="{pkg}" />')
                        lines.insert(i+len(required_packages)+1, '  </ItemGroup>')
                        lines.insert(i+len(required_packages)+2, '')
                        self.fixes_applied += len(required_packages)
                        break
                content = '\n'.join(lines)
            else:
                # Add missing packages
                for pkg in required_packages:
                    if f'Include="{pkg}"' not in content:
                        # Find last PackageReference and add after it
                        lines = content.split('\n')
                        for i in range(len(lines)-1, -1, -1):
                            if '<PackageReference' in lines[i]:
                                lines.insert(i+1, f'    <PackageReference Include="{pkg}" />')
                                self.fixes_applied += 1
                                break
                        content = '\n'.join(lines)
            
            project.write_text(content)
    
    def fix_fluent_assertions_usings(self):
        """Add FluentAssertions using statements to test files."""
        print("Fixing FluentAssertions using statements...")
        
        test_files = list(self.root_dir.glob("tests/**/*.cs"))
        
        for test_file in test_files:
            content = test_file.read_text()
            
            # Check if file uses Should() but doesn't have FluentAssertions
            if '.Should()' in content or '.Should(' in content:
                if 'using FluentAssertions;' not in content:
                    # Add using statement after other usings
                    lines = content.split('\n')
                    for i, line in enumerate(lines):
                        if line.startswith('using ') and i < len(lines) - 1:
                            if not lines[i+1].startswith('using '):
                                lines.insert(i+1, 'using FluentAssertions;')
                                self.fixes_applied += 1
                                break
                    content = '\n'.join(lines)
                    test_file.write_text(content)
                    print(f"  Fixed {test_file.name}")
    
    def fix_moq_setup_issues(self):
        """Fix Moq ReturnsAsync and setup issues."""
        print("Fixing Moq setup issues...")
        
        test_files = list(self.root_dir.glob("tests/**/*.cs"))
        
        for test_file in test_files:
            content = test_file.read_text()
            
            # Fix ReturnsAsync issues
            if 'ReturnsAsync' in content and 'error CS1929' in str(subprocess.run(f"dotnet build {test_file.parent.parent}/*.csproj --no-restore 2>&1", shell=True, capture_output=True, text=True).stdout):
                # Add Moq using if missing
                if 'using Moq;' not in content:
                    lines = content.split('\n')
                    for i, line in enumerate(lines):
                        if line.startswith('using '):
                            lines.insert(i+1, 'using Moq;')
                            self.fixes_applied += 1
                            break
                    content = '\n'.join(lines)
                    test_file.write_text(content)
                    print(f"  Fixed Moq in {test_file.name}")
    
    def fix_missing_test_models(self):
        """Fix missing test model definitions."""
        print("Fixing missing test models...")
        
        # Create CacheStatistics model if needed
        cache_stats_file = self.root_dir / "tests/Services/NeoServiceLayer.Services.Storage.Tests/TestModels.cs"
        if not cache_stats_file.exists():
            cache_stats_content = """using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.Storage.Tests
{
    public class CacheStatistics
    {
        public long Hits { get; set; }
        public long Misses { get; set; }
        public long TotalRequests { get; set; }
        public double HitRate => TotalRequests > 0 ? (double)Hits / TotalRequests : 0;
        public DateTime LastReset { get; set; } = DateTime.UtcNow;
        public Dictionary<string, long> DetailedStats { get; set; } = new();
    }
    
    public class StorageMetrics
    {
        public long TotalSize { get; set; }
        public long UsedSize { get; set; }
        public long AvailableSize { get; set; }
        public int FileCount { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
"""
            cache_stats_file.write_text(cache_stats_content)
            self.fixes_applied += 1
            print(f"  Created TestModels.cs with CacheStatistics")
    
    def fix_test_base_classes(self):
        """Create base test classes for common functionality."""
        print("Creating base test classes...")
        
        base_test_file = self.root_dir / "tests/NeoServiceLayer.Tests.Common/TestBase.cs"
        if not base_test_file.exists():
            base_test_file.parent.mkdir(parents=True, exist_ok=True)
            base_test_content = """using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NeoServiceLayer.Tests.Common
{
    public abstract class TestBase : IDisposable
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly Mock<ILogger> LoggerMock;
        
        protected TestBase()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            LoggerMock = new Mock<ILogger>();
        }
        
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Override in derived classes to add specific services
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (ServiceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
    
    public abstract class AsyncTestBase : TestBase, IAsyncLifetime
    {
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
        
        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
"""
            base_test_file.write_text(base_test_content)
            self.fixes_applied += 1
            print(f"  Created TestBase.cs")
    
    def run_build_and_analyze(self) -> Tuple[bool, int, List[str]]:
        """Run build and analyze remaining errors."""
        print("\nRunning build analysis...")
        
        cmd = "dotnet build NeoServiceLayer.sln --no-restore 2>&1"
        result = subprocess.run(cmd, shell=True, capture_output=True, text=True)
        
        errors = []
        for line in result.stdout.split('\n') + result.stderr.split('\n'):
            if 'error CS' in line:
                errors.append(line)
        
        error_count = len(errors)
        build_success = "Build succeeded" in result.stdout
        
        return build_success, error_count, errors
    
    def generate_phase2_plan(self):
        """Generate Phase 2 Service Implementation plan."""
        print("Generating Phase 2 implementation plan...")
        
        plan_content = """# Neo Service Layer - Phase 2: Service Implementation Plan

## 🎯 Objective
Complete all service implementations with production-ready business logic, error handling, and logging.

## 📋 Week 1 Tasks

### Day 1-2: Core Service Implementations
- [ ] Replace all stub methods with real implementations
- [ ] Add comprehensive error handling
- [ ] Implement retry logic for transient failures
- [ ] Add structured logging

### Day 3-4: Database Integration
- [ ] Implement Entity Framework Core repositories
- [ ] Add database migrations
- [ ] Configure connection strings
- [ ] Implement unit of work pattern

### Day 5: Caching Layer
- [ ] Implement Redis caching
- [ ] Add cache invalidation logic
- [ ] Configure cache policies
- [ ] Add distributed cache support

### Day 6-7: Testing & Validation
- [ ] Create unit tests for all services
- [ ] Add integration tests
- [ ] Implement contract tests
- [ ] Validate all endpoints

## 📊 Service Priority Matrix

### Critical Services (Priority 1)
1. **Authentication Service**
   - JWT token generation
   - User validation
   - Role management
   - Session handling

2. **KeyManagement Service**
   - Encryption/decryption
   - Key rotation
   - Secure storage
   - HSM integration

3. **ProofOfReserve Service**
   - Reserve calculations
   - Audit trail
   - Alert mechanisms
   - Compliance reporting

### Important Services (Priority 2)
1. **Voting Service**
   - Proposal management
   - Vote tallying
   - Delegate voting
   - Result calculation

2. **Oracle Service**
   - Data feed integration
   - Price aggregation
   - Source validation
   - Update scheduling

3. **Monitoring Service**
   - Metrics collection
   - Alert generation
   - Dashboard data
   - Performance tracking

### Standard Services (Priority 3)
- Notification Service
- Storage Service
- Compliance Service
- Automation Service

## 🔧 Implementation Standards

### Error Handling Pattern
```csharp
try
{
    // Business logic
    return Result.Success(data);
}
catch (ValidationException ex)
{
    _logger.LogWarning(ex, "Validation failed");
    return Result.Failure(ex.Message);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return Result.Failure("An error occurred");
}
```

### Logging Standards
- Use structured logging
- Include correlation IDs
- Log at appropriate levels
- Include performance metrics

### Testing Requirements
- Minimum 80% code coverage
- All public methods tested
- Integration tests for critical paths
- Performance benchmarks

## 📈 Success Metrics
- All services compile without errors
- 80% unit test coverage
- All integration tests pass
- Sub-200ms response time for APIs
- Zero critical security issues

## 🚀 Deliverables
1. Fully implemented service layer
2. Comprehensive test suite
3. API documentation
4. Performance benchmarks
5. Deployment guide

---
Generated: $(date)
"""
        
        plan_file = self.root_dir / "PHASE2_IMPLEMENTATION_PLAN.md"
        plan_file.write_text(plan_content)
        print(f"  Created PHASE2_IMPLEMENTATION_PLAN.md")
    
    def main(self):
        """Main execution flow."""
        print("=" * 60)
        print("TEST PROJECT FIX & PHASE 2 PREPARATION")
        print("=" * 60)
        
        # Get initial state
        initial_success, initial_errors, _ = self.run_build_and_analyze()
        print(f"Initial state: {initial_errors} errors")
        
        # Apply fixes
        print("\nApplying comprehensive test fixes...")
        self.add_test_packages()
        self.fix_fluent_assertions_usings()
        self.fix_moq_setup_issues()
        self.fix_missing_test_models()
        self.fix_test_base_classes()
        
        # Restore packages after adding references
        print("\nRestoring NuGet packages...")
        subprocess.run("dotnet restore NeoServiceLayer.sln", shell=True)
        
        # Get final state
        print("\nRunning final build...")
        final_success, final_errors, remaining_errors = self.run_build_and_analyze()
        
        # Generate report
        print("\n" + "=" * 60)
        print("RESULTS")
        print("=" * 60)
        print(f"Initial errors: {initial_errors}")
        print(f"Final errors: {final_errors}")
        print(f"Errors fixed: {initial_errors - final_errors}")
        print(f"Total fixes applied: {self.fixes_applied}")
        
        if final_success:
            print("\n" + "🎉" * 20)
            print("BUILD SUCCESSFUL! ZERO ERRORS!")
            print("🎉" * 20)
            print("\nPHASE 1 COMPLETE! Ready for Phase 2.")
            
            # Generate Phase 2 plan
            self.generate_phase2_plan()
            print("\nPhase 2 plan generated. Check PHASE2_IMPLEMENTATION_PLAN.md")
            
        elif final_errors < 100:
            print(f"\n✅ Excellent progress! Only {final_errors} errors remaining.")
            print("Review remaining errors:")
            for error in remaining_errors[:10]:
                print(f"  - {error[:100]}...")
        else:
            print(f"\n⚠️ Still {final_errors} errors to fix.")
            print("Manual intervention may be required.")

if __name__ == "__main__":
    fixer = TestProjectFixer()
    fixer.main()
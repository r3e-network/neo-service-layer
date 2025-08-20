#!/usr/bin/env python3
"""
Final comprehensive fix for all remaining compilation errors.
This script addresses TEE test issues, Compliance models, and other missing types.
"""

import subprocess
import re
import os
from pathlib import Path
from typing import Dict, List, Tuple

class FinalErrorFixer:
    def __init__(self):
        self.root_dir = Path("/home/ubuntu/neo-service-layer")
        self.fixes_applied = 0
        
    def fix_tee_test_models(self):
        """Fix missing TEE test models and implementations."""
        print("Fixing TEE test models...")
        
        # Create missing TEE test models
        tee_models_file = self.root_dir / "tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/TestModels.cs"
        tee_models_content = """using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    public class TrainingRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;
        public double[] TrainingData { get; set; } = Array.Empty<double>();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
    
    public class PredictionRequest
    {
        public string ModelId { get; set; } = string.Empty;
        public double[] InputData { get; set; } = Array.Empty<double>();
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
    
    public class PredictionResult
    {
        public double[] Predictions { get; set; } = Array.Empty<double>();
        public double Confidence { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
    
    public class AbstractAccountRequest
    {
        public string AccountId { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}
"""
        tee_models_file.write_text(tee_models_content)
        self.fixes_applied += 1
        print(f"  Created TEE test models")
        
        # Fix TestEnclaveWrapper implementation
        wrapper_file = self.root_dir / "tests/Tee/NeoServiceLayer.Tee.Enclave.Tests/TestEnclaveWrapper.cs"
        if wrapper_file.exists():
            content = wrapper_file.read_text()
            
            # Add missing method implementations
            if "ExecuteJavaScript" not in content:
                impl_methods = """
    public string ExecuteJavaScript(string script, string input)
    {
        return $"Test execution of {script}";
    }
    
    public int GenerateRandom(int min, int max)
    {
        return (min + max) / 2; // Test implementation
    }
    
    public byte[] GenerateRandomBytes(int length)
    {
        return new byte[length];
    }
    
    public byte[] Encrypt(byte[] data, byte[] key)
    {
        return data; // Test implementation
    }
    
    public byte[] Decrypt(byte[] data, byte[] key)
    {
        return data; // Test implementation
    }
    
    public byte[] Sign(byte[] data, byte[] privateKey)
    {
        return new byte[64]; // Test signature
    }
    
    public bool Verify(byte[] data, byte[] signature, byte[] publicKey)
    {
        return true; // Test implementation
    }
    
    public string GenerateKey(string keyType, string algorithm, string usage, bool exportable, string metadata)
    {
        return $"test-key-{keyType}";
    }
    
    public string FetchOracleData(string source, string query, string format, string options)
    {
        return $"{{\"source\":\"{source}\",\"data\":\"test\"}}";
    }
    
    public string ExecuteComputation(string computationType, string input, string options)
    {
        return $"{{\"result\":\"computed\"}}";
    }
    
    public bool StoreData(string key, byte[] data, string metadata, bool encrypted)
    {
        return true;
    }
    
    public byte[] RetrieveData(string key, string options)
    {
        return new byte[0];
    }
    
    public bool DeleteData(string key)
    {
        return true;
    }
    
    public string GetStorageMetadata(string key)
    {
        return "{}";
    }
    
    public bool TrainAIModel(string modelId, string modelType, double[] trainingData, string options)
    {
        return true;
    }
    
    public double[] PredictWithAIModel(string modelId, double[] inputData, out string metadata)
    {
        metadata = "{}";
        return new double[0];
    }
    
    public string CreateAbstractAccount(string config, string metadata)
    {
        return "test-account-id";
    }
    
    public string SignAbstractAccountTransaction(string accountId, string transaction)
    {
        return "signed-transaction";
    }
    
    public bool AddAbstractAccountGuardian(string accountId, string guardianInfo)
    {
        return true;
    }
    
    public string GetAttestationReport()
    {
        return "test-attestation-report";
    }
"""
                # Insert before the last closing brace
                lines = content.split('\n')
                for i in range(len(lines) - 1, -1, -1):
                    if '}' in lines[i] and 'namespace' not in lines[i]:
                        lines.insert(i, impl_methods)
                        break
                content = '\n'.join(lines)
                wrapper_file.write_text(content)
                self.fixes_applied += 1
                print(f"  Fixed TestEnclaveWrapper implementation")
    
    def fix_compliance_models(self):
        """Fix missing Compliance service models."""
        print("Fixing Compliance service models...")
        
        # Add missing types to ComplianceSupportingTypes.cs
        supporting_types_file = self.root_dir / "src/Services/NeoServiceLayer.Services.Compliance/Models/ComplianceSupportingTypes.cs"
        if supporting_types_file.exists():
            content = supporting_types_file.read_text()
            
            # Add missing enums and classes
            if "RemediationStatus" not in content:
                additional_types = """
// Additional Compliance Types

public enum RemediationStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Verified
}

public class RemediationStep
{
    public string StepId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RemediationStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ResponsibleParty { get; set; } = string.Empty;
}

public enum KycStatus
{
    NotStarted,
    Pending,
    InReview,
    Approved,
    Rejected,
    Expired
}

public enum KycLevel
{
    None,
    Basic,
    Enhanced,
    Full
}

public class ComplianceMetrics
{
    public int TotalChecks { get; set; }
    public int PassedChecks { get; set; }
    public int FailedChecks { get; set; }
    public double ComplianceScore { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class AmlAlert
{
    public string AlertId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
}
"""
                # Insert before the last closing brace of the namespace
                lines = content.split('\n')
                for i in range(len(lines) - 1, -1, -1):
                    if '}' in lines[i]:
                        lines.insert(i, additional_types)
                        break
                content = '\n'.join(lines)
                supporting_types_file.write_text(content)
                self.fixes_applied += 1
                print(f"  Added missing Compliance types")
    
    def fix_storage_test_models(self):
        """Fix missing Storage test models."""
        print("Fixing Storage test models...")
        
        storage_test_file = self.root_dir / "tests/Services/NeoServiceLayer.Services.Storage.Tests/StorageServiceTests.cs"
        if storage_test_file.exists():
            content = storage_test_file.read_text()
            
            # Add missing CacheStatistics if not exists
            if "class CacheStatistics" not in content:
                # Create separate model file
                models_file = self.root_dir / "tests/Services/NeoServiceLayer.Services.Storage.Tests/TestModels.cs"
                models_content = """using System;
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
    
    public class StorageTestData
    {
        public string Id { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public Dictionary<string, string> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
"""
                models_file.write_text(models_content)
                self.fixes_applied += 1
                print(f"  Created Storage test models")
    
    def fix_moq_and_fluent_issues(self):
        """Fix remaining Moq and FluentAssertions issues."""
        print("Fixing Moq and FluentAssertions issues...")
        
        # Find all test files
        test_files = list(self.root_dir.glob("tests/**/*.cs"))
        
        for test_file in test_files:
            content = test_file.read_text()
            modified = False
            
            # Fix FluentAssertions namespace
            if ".Should()" in content and "using FluentAssertions;" not in content:
                lines = content.split('\n')
                # Find the last using statement
                last_using_idx = -1
                for i, line in enumerate(lines):
                    if line.strip().startswith("using "):
                        last_using_idx = i
                
                if last_using_idx >= 0:
                    lines.insert(last_using_idx + 1, "using FluentAssertions;")
                    modified = True
            
            # Fix Moq namespace
            if "Mock<" in content and "using Moq;" not in content:
                lines = content.split('\n') if not modified else lines
                # Find the last using statement
                last_using_idx = -1
                for i, line in enumerate(lines):
                    if line.strip().startswith("using "):
                        last_using_idx = i
                
                if last_using_idx >= 0:
                    lines.insert(last_using_idx + 1, "using Moq;")
                    modified = True
            
            # Fix xUnit namespace
            if "[Fact]" in content and "using Xunit;" not in content:
                lines = content.split('\n') if not modified else lines
                # Find the last using statement
                last_using_idx = -1
                for i, line in enumerate(lines):
                    if line.strip().startswith("using "):
                        last_using_idx = i
                
                if last_using_idx >= 0:
                    lines.insert(last_using_idx + 1, "using Xunit;")
                    modified = True
            
            if modified:
                content = '\n'.join(lines)
                test_file.write_text(content)
                self.fixes_applied += 1
    
    def add_missing_test_references(self):
        """Add missing project references to test projects."""
        print("Adding missing project references...")
        
        test_projects = list(self.root_dir.glob("tests/**/*.csproj"))
        
        for project_file in test_projects:
            content = project_file.read_text()
            
            # Check if it's missing key references
            if "ProjectReference" not in content:
                # Determine what project this is testing
                project_name = project_file.stem
                if ".Tests" in project_name:
                    base_name = project_name.replace(".Tests", "")
                    
                    # Find the corresponding source project
                    source_project = None
                    for src_proj in self.root_dir.glob(f"src/**/{base_name}.csproj"):
                        source_project = src_proj
                        break
                    
                    if source_project:
                        # Add project reference
                        relative_path = os.path.relpath(source_project, project_file.parent)
                        
                        lines = content.split('\n')
                        for i, line in enumerate(lines):
                            if '</Project>' in line:
                                lines.insert(i, f'  <ItemGroup>')
                                lines.insert(i+1, f'    <ProjectReference Include="{relative_path}" />')
                                lines.insert(i+2, f'  </ItemGroup>')
                                lines.insert(i+3, '')
                                break
                        
                        content = '\n'.join(lines)
                        project_file.write_text(content)
                        self.fixes_applied += 1
                        print(f"  Added reference to {base_name} in {project_name}")
    
    def run_build_and_analyze(self) -> Tuple[bool, int, List[str]]:
        """Run build and analyze remaining errors."""
        print("\nRunning build analysis...")
        
        cmd = "dotnet build NeoServiceLayer.sln --no-restore 2>&1"
        result = subprocess.run(cmd, shell=True, capture_output=True, text=True, cwd=self.root_dir)
        
        errors = []
        for line in result.stdout.split('\n') + result.stderr.split('\n'):
            if 'error CS' in line:
                errors.append(line)
        
        error_count = len(errors)
        build_success = "Build succeeded" in result.stdout
        
        return build_success, error_count, errors
    
    def main(self):
        """Main execution flow."""
        print("=" * 60)
        print("FINAL COMPREHENSIVE ERROR FIX")
        print("=" * 60)
        
        # Get initial state
        initial_success, initial_errors, _ = self.run_build_and_analyze()
        print(f"Initial state: {initial_errors} errors")
        
        # Apply all fixes
        print("\nApplying final comprehensive fixes...")
        self.fix_tee_test_models()
        self.fix_compliance_models()
        self.fix_storage_test_models()
        self.fix_moq_and_fluent_issues()
        self.add_missing_test_references()
        
        # Restore packages after changes
        print("\nRestoring NuGet packages...")
        subprocess.run("dotnet restore NeoServiceLayer.sln", shell=True, cwd=self.root_dir)
        
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
        elif final_errors < 100:
            print(f"\n✅ Excellent progress! Only {final_errors} errors remaining.")
            print("Top remaining errors:")
            for error in remaining_errors[:5]:
                print(f"  - {error[:150]}...")
        else:
            print(f"\n⚠️ Progress made. {final_errors} errors remaining.")
            print("Top error categories:")
            # Analyze error patterns
            error_types = {}
            for error in remaining_errors:
                if "CS" in error:
                    cs_code = error.split("error CS")[1][:4]
                    error_types[cs_code] = error_types.get(cs_code, 0) + 1
            
            for cs_code, count in sorted(error_types.items(), key=lambda x: x[1], reverse=True)[:5]:
                print(f"  CS{cs_code}: {count} occurrences")

if __name__ == "__main__":
    fixer = FinalErrorFixer()
    fixer.main()
using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Tests.Common
{
    // Common test types used across multiple test projects
    
    public class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
    
    public class TestConfigurationData
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Settings { get; set; } = new();
    }
    
    public class TestMetrics
    {
        public long ElapsedMilliseconds { get; set; }
        public int ItemsProcessed { get; set; }
        public double SuccessRate { get; set; }
    }
    
    public class TestEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    public static class TestConstants
    {
        public const string DefaultConnectionString = "Server=localhost;Database=TestDb;";
        public const int DefaultTimeout = 5000;
        public const string TestEnvironment = "Test";
    }
}

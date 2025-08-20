using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.Monitoring.Tests
{
    /// <summary>
    /// Test helper class for creating test data
    /// </summary>
    public static class TestDataHelper
    {
        public static Dictionary<string, string> CreateTestTags(string environment = "test", string version = "1.0")
        {
            return new Dictionary<string, string>
            {
                { "environment", environment },
                { "version", version }
            };
        }

        public static Dictionary<string, object> CreateTestMetadata(string source = "test")
        {
            return new Dictionary<string, object>
            {
                { "source", source },
                { "created_at", DateTime.UtcNow }
            };
        }
    }
}
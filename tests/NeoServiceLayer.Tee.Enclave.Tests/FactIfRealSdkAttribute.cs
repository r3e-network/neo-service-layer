using System;
using Xunit;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// Attribute for facts that should only run if the real OpenEnclave SDK is available.
    /// </summary>
    /// <remarks>
    /// This attribute is used to mark tests that should only run if the real OpenEnclave SDK is available.
    /// If the real SDK is not available, the test will be skipped.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FactIfRealSdkAttribute : FactAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FactIfRealSdkAttribute"/> class.
        /// </summary>
        public FactIfRealSdkAttribute()
        {
            // Set the Skip property to a default value
            // This will be overridden at runtime if the real SDK is available
            Skip = "This test requires the real OpenEnclave SDK";
        }
    }
}

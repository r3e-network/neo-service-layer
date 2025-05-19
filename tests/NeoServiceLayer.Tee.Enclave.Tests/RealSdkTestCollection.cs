using System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace NeoServiceLayer.Tee.Enclave.Tests
{

    /// <summary>
    /// Collection fixture that updates the Skip property of the FactIfRealSdk attribute at runtime.
    /// </summary>
    public class RealSdkTestCollectionFixture : IDisposable
    {
        private readonly ILogger<RealSdkTestCollectionFixture> _logger;
        private readonly SimulationModeFixture _fixture;

        /// <summary>
        /// Initializes a new instance of the <see cref="RealSdkTestCollectionFixture"/> class.
        /// </summary>
        /// <param name="fixture">The simulation mode fixture.</param>
        public RealSdkTestCollectionFixture(SimulationModeFixture fixture)
        {
            _fixture = fixture;
            _logger = fixture.LoggerFactory.CreateLogger<RealSdkTestCollectionFixture>();

            // Update the Skip property of the FactIfRealSdk attribute at runtime
            UpdateFactIfRealSdkAttribute();
        }

        /// <summary>
        /// Updates the Skip property of the FactIfRealSdk attribute at runtime.
        /// </summary>
        private void UpdateFactIfRealSdkAttribute()
        {
            try
            {
                // Get all test methods with the FactIfRealSdk attribute
                var testAssembly = typeof(RealSdkTestCollectionFixture).Assembly;
                var testMethods = testAssembly.GetTypes()
                    .SelectMany(t => t.GetMethods())
                    .Where(m => m.GetCustomAttributes(typeof(FactIfRealSdkAttribute), false).Length > 0)
                    .ToList();

                _logger.LogInformation("Found {Count} test methods with the FactIfRealSdk attribute", testMethods.Count);

                // Update the Skip property of the FactIfRealSdk attribute
                foreach (var method in testMethods)
                {
                    var attribute = method.GetCustomAttribute<FactIfRealSdkAttribute>();
                    if (attribute != null)
                    {
                        // Get the skip reason
                        string skipReason = RealSdkTestHelper.GetRealSdkSkipReason(_fixture);

                        // Update the Skip property
                        var skipProperty = typeof(FactAttribute).GetProperty("Skip");
                        if (skipProperty != null)
                        {
                            skipProperty.SetValue(attribute, skipReason);
                            _logger.LogInformation("Updated Skip property of FactIfRealSdk attribute for method {Method} to {SkipReason}",
                                method.Name, skipReason ?? "(null)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating FactIfRealSdk attribute");
            }
        }

        /// <summary>
        /// Disposes the resources used by the <see cref="RealSdkTestCollectionFixture"/> class.
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}

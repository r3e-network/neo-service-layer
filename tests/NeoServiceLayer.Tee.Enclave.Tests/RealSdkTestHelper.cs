using System;
using Microsoft.Extensions.Logging;
using Xunit;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// Helper class for running tests with the real OpenEnclave SDK.
    /// </summary>
    public static class RealSdkTestHelper
    {
        /// <summary>
        /// Runs a test with the real OpenEnclave SDK if available, otherwise skips the test.
        /// </summary>
        /// <param name="fixture">The simulation mode fixture.</param>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="testAction">The test action to run.</param>
        public static void RunWithRealSdkOrSkip(SimulationModeFixture fixture, ILogger logger, Action testAction)
        {
            // Skip the test if we're not using the real SDK
            if (!fixture.UsingRealSdk)
            {
                logger.LogInformation("Skipping test because the real OpenEnclave SDK is not available");
                Skip.Always("This test requires the real OpenEnclave SDK");
                return; // This line will never be reached, but it's here for clarity
            }

            try
            {
                // Run the test
                logger.LogInformation("Running test with real OpenEnclave SDK");
                testAction();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running test with real OpenEnclave SDK");
                throw;
            }
        }

        /// <summary>
        /// Runs a test with the real OpenEnclave SDK if available, otherwise runs it with the mock implementation.
        /// </summary>
        /// <param name="fixture">The simulation mode fixture.</param>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="realSdkAction">The action to run if the real SDK is available.</param>
        /// <param name="mockAction">The action to run if the real SDK is not available.</param>
        public static void RunWithRealSdkOrMock(SimulationModeFixture fixture, ILogger logger, Action realSdkAction, Action mockAction)
        {
            try
            {
                if (fixture.UsingRealSdk)
                {
                    logger.LogInformation("Running test with real OpenEnclave SDK");
                    realSdkAction();
                }
                else
                {
                    logger.LogInformation("Running test with mock implementation");
                    mockAction();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running test");
                throw;
            }
        }

        /// <summary>
        /// Forces the use of the mock implementation for all tests.
        /// </summary>
        public static void ForceMockImplementation()
        {
            SimulationModeFixture.ForceMockImplementation = true;
        }

        /// <summary>
        /// Allows the use of the real OpenEnclave SDK for tests if available.
        /// </summary>
        public static void AllowRealSdk()
        {
            SimulationModeFixture.ForceMockImplementation = false;
        }

        /// <summary>
        /// Gets the skip reason for a test that requires the real OpenEnclave SDK.
        /// </summary>
        /// <param name="fixture">The simulation mode fixture.</param>
        /// <returns>The skip reason, or null if the test should not be skipped.</returns>
        public static string GetRealSdkSkipReason(SimulationModeFixture fixture)
        {
            return fixture.UsingRealSdk ? null : "This test requires the real OpenEnclave SDK";
        }
    }
}

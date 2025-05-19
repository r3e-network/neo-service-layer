using System;
using Xunit;

namespace NeoServiceLayer.TestHelpers
{
    /// <summary>
    /// Utility class for skipping tests.
    /// </summary>
    public static class Skip
    {
        /// <summary>
        /// Skips the test if the condition is true.
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="reason">The reason for skipping the test.</param>
        public static void If(bool condition, string reason)
        {
            if (condition)
            {
                throw new SkipException(reason);
            }
        }

        /// <summary>
        /// Skips the test unconditionally.
        /// </summary>
        /// <param name="reason">The reason for skipping the test.</param>
        public static void Always(string reason)
        {
            throw new SkipException(reason);
        }
    }
}

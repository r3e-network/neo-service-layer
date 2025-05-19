using System;
using Xunit;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// Attribute for facts that can be skipped.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkippableFactAttribute : FactAttribute
    {
    }

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
        /// Skips the test if the condition is false.
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="reason">The reason for skipping the test.</param>
        public static void IfNot(bool condition, string reason)
        {
            If(!condition, reason);
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

    /// <summary>
    /// Exception thrown when a test should be skipped.
    /// </summary>
    [Serializable]
    public class SkipException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SkipException"/> class.
        /// </summary>
        public SkipException() : base("Test skipped")
        {
            // Skip.If(true, "Test skipped");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkipException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SkipException(string message) : base(message)
        {
            // Skip.If(true, message);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkipException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public SkipException(string message, Exception innerException) : base(message, innerException)
        {
            // Skip.If(true, $"{message} Inner exception: {innerException.Message}");
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Core.Events.SGXEvents
{
    /// <summary>
    /// Event raised when SGX secure execution is completed
    /// </summary>
    public class SGXExecutionCompletedEvent : DomainEventBase
    {
        public SGXExecutionCompletedEvent(
            string aggregateId,
            long aggregateVersion,
            string initiatedBy,
            string executionId,
            bool wasSuccessful,
            long executionTimeMs,
            string codeHash,
            int inputSize,
            int outputSize,
            string attestationStatus,
            Guid? causationId = null,
            Guid? correlationId = null)
            : base(aggregateId, "SGXService", aggregateVersion, initiatedBy, causationId, correlationId)
        {
            ExecutionId = executionId ?? throw new ArgumentNullException(nameof(executionId));
            WasSuccessful = wasSuccessful;
            ExecutionTimeMs = executionTimeMs;
            CodeHash = codeHash ?? throw new ArgumentNullException(nameof(codeHash));
            InputSize = inputSize;
            OutputSize = outputSize;
            AttestationStatus = attestationStatus ?? throw new ArgumentNullException(nameof(attestationStatus));

            AddMetadata("performance_category", GetPerformanceCategory(executionTimeMs));
            AddMetadata("execution_efficiency", CalculateEfficiency(inputSize, outputSize, executionTimeMs));
            AddMetadata("security_level", GetSecurityLevel(attestationStatus));
        }

        /// <summary>
        /// Unique identifier for this execution
        /// </summary>
        public string ExecutionId { get; }

        /// <summary>
        /// Whether the execution completed successfully
        /// </summary>
        public bool WasSuccessful { get; }

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; }

        /// <summary>
        /// SHA-256 hash of the executed code
        /// </summary>
        public string CodeHash { get; }

        /// <summary>
        /// Size of input data in bytes
        /// </summary>
        public int InputSize { get; }

        /// <summary>
        /// Size of output data in bytes
        /// </summary>
        public int OutputSize { get; }

        /// <summary>
        /// Status of SGX attestation
        /// </summary>
        public string AttestationStatus { get; }

        private static string GetPerformanceCategory(long executionTimeMs)
        {
            return executionTimeMs switch
            {
                < 100 => "fast",
                < 1000 => "normal",
                < 5000 => "slow",
                _ => "very_slow"
            };
        }

        private static double CalculateEfficiency(int inputSize, int outputSize, long executionTimeMs)
        {
            if (executionTimeMs == 0) return 0;
            return (double)(inputSize + outputSize) / executionTimeMs;
        }

        private static string GetSecurityLevel(string attestationStatus)
        {
            return attestationStatus.ToLowerInvariant() switch
            {
                "valid" => "high",
                "expired" => "medium",
                "invalid" => "low",
                _ => "unknown"
            };
        }
    }
}

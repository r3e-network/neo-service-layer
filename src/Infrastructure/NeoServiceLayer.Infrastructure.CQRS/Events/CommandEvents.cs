using System;
using NeoServiceLayer.Core.Events;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Infrastructure.CQRS.Events
{
    /// <summary>
    /// Event raised when a command is executed successfully
    /// </summary>
    public class CommandExecutedEvent : DomainEventBase
    {
        public CommandExecutedEvent(
            Guid commandId,
            string commandType,
            string initiatedBy,
            long executionTimeMs,
            Guid? correlationId = null)
            : base(
                commandId.ToString(),
                "CommandBus",
                1,
                initiatedBy,
                commandId,
                correlationId)
        {
            CommandId = commandId;
            CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
            ExecutionTimeMs = executionTimeMs;

            AddMetadata("performance_category", GetPerformanceCategory(executionTimeMs));
            AddMetadata("command_namespace", GetCommandNamespace(commandType));
        }

        /// <summary>
        /// ID of the executed command
        /// </summary>
        public Guid CommandId { get; }

        /// <summary>
        /// Type name of the executed command
        /// </summary>
        public string CommandType { get; }

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; }

        private static string GetPerformanceCategory(long executionTimeMs)
        {
            return executionTimeMs switch
            {
                < 50 => "fast",
                < 200 => "normal",
                < 1000 => "slow",
                _ => "very_slow"
            };
        }

        private static string GetCommandNamespace(string commandType)
        {
            var parts = commandType.Split('.');
            return parts.Length > 1 ? parts[^2] : "Unknown";
        }
    }

    /// <summary>
    /// Event raised when a command execution fails
    /// </summary>
    public class CommandFailedEvent : DomainEventBase
    {
        public CommandFailedEvent(
            Guid commandId,
            string commandType,
            string initiatedBy,
            string errorMessage,
            string errorType,
            long executionTimeMs,
            Guid? correlationId = null)
            : base(
                commandId.ToString(),
                "CommandBus",
                1,
                initiatedBy,
                commandId,
                correlationId)
        {
            CommandId = commandId;
            CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            ErrorType = errorType ?? throw new ArgumentNullException(nameof(errorType));
            ExecutionTimeMs = executionTimeMs;

            AddMetadata("error_category", GetErrorCategory(errorType));
            AddMetadata("requires_investigation", IsInvestigationRequired(errorType));
        }

        /// <summary>
        /// ID of the failed command
        /// </summary>
        public Guid CommandId { get; }

        /// <summary>
        /// Type name of the failed command
        /// </summary>
        public string CommandType { get; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Type of the error/exception
        /// </summary>
        public string ErrorType { get; }

        /// <summary>
        /// Execution time before failure in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; }

        private static string GetErrorCategory(string errorType)
        {
            return errorType switch
            {
                var t when t.Contains("Validation") => "validation",
                var t when t.Contains("Timeout") => "timeout",
                var t when t.Contains("NotFound") => "not_found",
                var t when t.Contains("Conflict") => "conflict",
                var t when t.Contains("Circuit") => "circuit_breaker",
                _ => "system_error"
            };
        }

        private static bool IsInvestigationRequired(string errorType)
        {
            var criticalErrors = new[] { "Circuit", "System", "Fatal", "Critical" };
            return criticalErrors.Any(critical => errorType.Contains(critical, StringComparison.OrdinalIgnoreCase));
        }
    }
}
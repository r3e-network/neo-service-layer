using System.Threading;
using System.Threading.Tasks;
using NeoServiceLayer.Core.CQRS;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System;


namespace NeoServiceLayer.Infrastructure.CQRS
{
    /// <summary>
    /// Interface for command validation
    /// </summary>
    /// <typeparam name="TCommand">Type of command to validate</typeparam>
    public interface ICommandValidator<in TCommand> where TCommand : ICommand
    {
        /// <summary>
        /// Validates the specified command
        /// </summary>
        /// <param name="command">Command to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateAsync(TCommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for query validation
    /// </summary>
    /// <typeparam name="TQuery">Type of query to validate</typeparam>
    public interface IQueryValidator<in TQuery> where TQuery : class
    {
        /// <summary>
        /// Validates the specified query
        /// </summary>
        /// <param name="query">Query to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateAsync(TQuery query, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public ValidationResult()
        {
            Errors = new List<ValidationError>();
        }

        public bool IsValid => !Errors.Any();
        public List<ValidationError> Errors { get; }

        public void AddError(string propertyName, string errorMessage)
        {
            Errors.Add(new ValidationError(propertyName, errorMessage));
        }
    }

    /// <summary>
    /// Validation error
    /// </summary>
    public class ValidationError
    {
        public ValidationError(string propertyName, string errorMessage)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        }

        public string PropertyName { get; }
        public string ErrorMessage { get; }
    }

    /// <summary>
    /// Exception thrown when validation fails
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(List<ValidationError> errors)
            : base(FormatMessage(errors))
        {
            Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        public List<ValidationError> Errors { get; }

        private static string FormatMessage(List<ValidationError> errors)
        {
            if (errors == null || !errors.Any())
                return "Validation failed";

            var errorMessages = errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
            return $"Validation failed: {string.Join(", ", errorMessages)}";
        }
    }
}
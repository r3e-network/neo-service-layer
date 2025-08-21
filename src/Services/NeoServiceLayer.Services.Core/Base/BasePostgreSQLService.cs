using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence.PostgreSQL.Repositories;

namespace NeoServiceLayer.Services.Core.Base
{
    /// <summary>
    /// Base class for all services using PostgreSQL persistence
    /// </summary>
    public abstract class BasePostgreSQLService<TService>
    {
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly ILogger<TService> _logger;

        protected BasePostgreSQLService(
            IUnitOfWork unitOfWork,
            ILogger<TService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes an operation within a database transaction
        /// </summary>
        protected async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<Task<TResult>> operation,
            string operationName)
        {
            try
            {
                _logger.LogDebug("Starting transaction for operation: {OperationName}", operationName);
                
                await _unitOfWork.BeginTransactionAsync();
                
                var result = await operation();
                
                await _unitOfWork.CommitAsync();
                
                _logger.LogDebug("Transaction committed successfully for operation: {OperationName}", operationName);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transaction for operation: {OperationName}. Rolling back...", operationName);
                
                await _unitOfWork.RollbackAsync();
                
                throw;
            }
        }

        /// <summary>
        /// Executes an operation within a database transaction without return value
        /// </summary>
        protected async Task ExecuteInTransactionAsync(
            Func<Task> operation,
            string operationName)
        {
            try
            {
                _logger.LogDebug("Starting transaction for operation: {OperationName}", operationName);
                
                await _unitOfWork.BeginTransactionAsync();
                
                await operation();
                
                await _unitOfWork.CommitAsync();
                
                _logger.LogDebug("Transaction committed successfully for operation: {OperationName}", operationName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in transaction for operation: {OperationName}. Rolling back...", operationName);
                
                await _unitOfWork.RollbackAsync();
                
                throw;
            }
        }

        /// <summary>
        /// Logs and rethrows exceptions with context
        /// </summary>
        protected void LogAndThrowException(Exception ex, string context, params object[] args)
        {
            var message = $"Error in {typeof(TService).Name}: {context}";
            _logger.LogError(ex, message, args);
            throw new InvalidOperationException(string.Format(message, args), ex);
        }

        /// <summary>
        /// Validates input parameters
        /// </summary>
        protected void ValidateInput<T>(T input, string parameterName) where T : class
        {
            if (input == null)
            {
                var message = $"{parameterName} cannot be null";
                _logger.LogError(message);
                throw new ArgumentNullException(parameterName, message);
            }
        }

        /// <summary>
        /// Validates string input parameters
        /// </summary>
        protected void ValidateStringInput(string input, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                var message = $"{parameterName} cannot be null or empty";
                _logger.LogError(message);
                throw new ArgumentException(message, parameterName);
            }
        }

        /// <summary>
        /// Validates GUID input parameters
        /// </summary>
        protected void ValidateGuidInput(Guid input, string parameterName)
        {
            if (input == Guid.Empty)
            {
                var message = $"{parameterName} cannot be empty GUID";
                _logger.LogError(message);
                throw new ArgumentException(message, parameterName);
            }
        }
    }
}
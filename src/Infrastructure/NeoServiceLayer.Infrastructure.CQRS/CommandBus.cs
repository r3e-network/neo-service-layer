using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Core.Events;
using NeoServiceLayer.Infrastructure.CQRS.Events;
using Polly;
using System.ComponentModel.DataAnnotations;
using System.Linq;


namespace NeoServiceLayer.Infrastructure.CQRS
{
    /// <summary>
    /// Command bus implementation for routing and executing commands
    /// </summary>
    public class CommandBus : ICommandBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CommandBus> _logger;
        private readonly IEventBus _eventBus;
        private readonly IAsyncPolicy _resiliencePolicy;
        private readonly CommandBusMetrics _metrics;
        private readonly Dictionary<Type, Type> _handlerRegistry;

        public CommandBus(
            IServiceProvider serviceProvider,
            ILogger<CommandBus> logger,
            IEventBus eventBus,
            CommandBusMetrics metrics)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _handlerRegistry = new Dictionary<Type, Type>();

            // Configure resilience policy with retry (Polly v8 compatible)
            _resiliencePolicy = Policy.Handle<Exception>(ex => !(ex is ValidationException))
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {RetryCount} after {Delay}ms for command execution",
                            retryCount, timespan.TotalMilliseconds);
                        _metrics.RecordRetry();
                    });
        }

        public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var stopwatch = Stopwatch.StartNew();
            var commandType = command.GetType();

            try
            {
                _logger.LogInformation(
                    "Executing command {CommandType} with ID {CommandId}",
                    commandType.Name, command.CommandId);

                // Get handler type
                var handlerType = GetHandlerType(commandType);
                if (handlerType == null)
                {
                    throw new HandlerNotFoundException(
                        $"No handler registered for command type {commandType.Name}");
                }

                // Execute with resilience policy
                using var scope = _serviceProvider.CreateScope();
                await _resiliencePolicy.ExecuteAsync(async (ct) =>
                {
                    var handler = scope.ServiceProvider.GetService(handlerType);

                    if (handler == null)
                    {
                        throw new HandlerNotFoundException(
                            $"Handler {handlerType.Name} could not be resolved from DI container");
                    }

                    // Validate command if validator exists
                    await ValidateCommandAsync(command, scope.ServiceProvider, ct);

                    // Execute handler
                    var handleMethod = handlerType.GetMethod("HandleAsync");
                    if (handleMethod == null)
                    {
                        throw new InvalidOperationException(
                            $"Handler {handlerType.Name} does not have HandleAsync method");
                    }

                    var task = (Task?)handleMethod.Invoke(handler, new object[] { command, ct });
                    if (task != null)
                    {
                        await task;
                    }

                    // Publish command executed event
                    await PublishCommandExecutedEventAsync(command, stopwatch.ElapsedMilliseconds, ct);

                }, cancellationToken);

                _metrics.RecordCommandSuccess(commandType.Name, stopwatch.ElapsedMilliseconds);

                _logger.LogInformation(
                    "Command {CommandType} with ID {CommandId} executed successfully in {ElapsedMs}ms",
                    commandType.Name, command.CommandId, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _metrics.RecordCommandFailure(commandType.Name, stopwatch.ElapsedMilliseconds);

                _logger.LogError(ex,
                    "Failed to execute command {CommandType} with ID {CommandId}",
                    commandType.Name, command.CommandId);

                await PublishCommandFailedEventAsync(command, ex, stopwatch.ElapsedMilliseconds, cancellationToken);

                throw;
            }
        }

        public async Task<TResult> SendAsync<TResult>(
            ICommand<TResult> command,
            CancellationToken cancellationToken = default)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            var stopwatch = Stopwatch.StartNew();
            var commandType = command.GetType();

            try
            {
                _logger.LogInformation(
                    "Executing command {CommandType} with ID {CommandId}",
                    commandType.Name, command.CommandId);

                // Get handler type
                var handlerType = GetHandlerType(commandType);
                if (handlerType == null)
                {
                    throw new HandlerNotFoundException(
                        $"No handler registered for command type {commandType.Name}");
                }

                // Execute with resilience policy
                using var scope = _serviceProvider.CreateScope();
                var result = await _resiliencePolicy.ExecuteAsync(async (ct) =>
                {
                    var handler = scope.ServiceProvider.GetService(handlerType);

                    if (handler == null)
                    {
                        throw new HandlerNotFoundException(
                            $"Handler {handlerType.Name} could not be resolved from DI container");
                    }

                    // Validate command if validator exists
                    await ValidateCommandAsync(command, scope.ServiceProvider, ct);

                    // Execute handler
                    var handleMethod = handlerType.GetMethod("HandleAsync");
                    if (handleMethod == null)
                    {
                        throw new InvalidOperationException(
                            $"Handler {handlerType.Name} does not have HandleAsync method");
                    }

                    var task = handleMethod.Invoke(handler, new object[] { command, ct });
                    if (task == null)
                    {
                        throw new InvalidOperationException("Handler did not return a task");
                    }

                    // Get result from task
                    var taskType = task.GetType();
                    var resultProperty = taskType.GetProperty("Result");

                    // Await the task
                    await (Task)task;

                    var taskResult = resultProperty?.GetValue(task);

                    // Publish command executed event
                    await PublishCommandExecutedEventAsync(command, stopwatch.ElapsedMilliseconds, ct);

                    return (TResult)taskResult!;

                }, cancellationToken);

                _metrics.RecordCommandSuccess(commandType.Name, stopwatch.ElapsedMilliseconds);

                _logger.LogInformation(
                    "Command {CommandType} with ID {CommandId} executed successfully in {ElapsedMs}ms",
                    commandType.Name, command.CommandId, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _metrics.RecordCommandFailure(commandType.Name, stopwatch.ElapsedMilliseconds);

                _logger.LogError(ex,
                    "Failed to execute command {CommandType} with ID {CommandId}",
                    commandType.Name, command.CommandId);

                await PublishCommandFailedEventAsync(command, ex, stopwatch.ElapsedMilliseconds, cancellationToken);

                throw;
            }
        }

        public void RegisterHandler(Type commandType, Type handlerType)
        {
            if (!typeof(ICommand).IsAssignableFrom(commandType))
            {
                throw new ArgumentException(
                    $"Type {commandType.Name} does not implement ICommand", nameof(commandType));
            }

            _handlerRegistry[commandType] = handlerType;

            _logger.LogDebug(
                "Registered handler {HandlerType} for command {CommandType}",
                handlerType.Name, commandType.Name);
        }

        private Type? GetHandlerType(Type commandType)
        {
            if (_handlerRegistry.TryGetValue(commandType, out var handlerType))
            {
                return handlerType;
            }

            // Try to find handler through DI container
            var genericHandlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
            return genericHandlerType;
        }

        private async Task ValidateCommandAsync(
            ICommand command,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            var validatorType = typeof(ICommandValidator<>).MakeGenericType(command.GetType());
            var validator = serviceProvider.GetService(validatorType);

            if (validator != null)
            {
                var validateMethod = validatorType.GetMethod("ValidateAsync");
                if (validateMethod != null)
                {
                    var validationTask = (Task<ValidationResult>?)validateMethod.Invoke(
                        validator, new object[] { command, cancellationToken });

                    if (validationTask != null)
                    {
                        var validationResult = await validationTask;
                        if (!validationResult.IsValid)
                        {
                            throw new ValidationException(validationResult.Errors);
                        }
                    }
                }
            }
        }

        private async Task PublishCommandExecutedEventAsync(
            ICommand command,
            long executionTimeMs,
            CancellationToken cancellationToken)
        {
            var commandExecutedEvent = new CommandExecutedEvent(
                command.CommandId,
                command.GetType().Name,
                command.InitiatedBy,
                executionTimeMs,
                command.CorrelationId);

            await _eventBus.PublishAsync(commandExecutedEvent, cancellationToken);
        }

        private async Task PublishCommandFailedEventAsync(
            ICommand command,
            Exception exception,
            long executionTimeMs,
            CancellationToken cancellationToken)
        {
            var commandFailedEvent = new CommandFailedEvent(
                command.CommandId,
                command.GetType().Name,
                command.InitiatedBy,
                exception.Message,
                exception.GetType().Name,
                executionTimeMs,
                command.CorrelationId);

            await _eventBus.PublishAsync(commandFailedEvent, cancellationToken);
        }
    }

    /// <summary>
    /// Interface for command bus
    /// </summary>
    public interface ICommandBus
    {
        Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
        Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
        void RegisterHandler(Type commandType, Type handlerType);
    }

    /// <summary>
    /// Exception thrown when a handler is not found
    /// </summary>
    public class HandlerNotFoundException : Exception
    {
        public HandlerNotFoundException(string message) : base(message) { }
        public HandlerNotFoundException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
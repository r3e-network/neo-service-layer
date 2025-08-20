using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.Saga;

/// <summary>
/// Orchestrator for managing distributed transactions using the SAGA pattern.
/// </summary>
public class SagaOrchestrator<TState> : ISagaOrchestrator<TState> where TState : class, new()
{
    private readonly ILogger<SagaOrchestrator<TState>> _logger;
    private readonly List<ISagaStep<TState>> _steps = new();
    private readonly ISagaStateStore<TState> _stateStore;

    public SagaOrchestrator(
        ILogger<SagaOrchestrator<TState>> logger,
        ISagaStateStore<TState> stateStore)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    /// <inheritdoc/>
    public ISagaOrchestrator<TState> AddStep(ISagaStep<TState> step)
    {
        _steps.Add(step);
        return this;
    }

    /// <inheritdoc/>
    public ISagaOrchestrator<TState> AddStep(
        string name,
        Func<TState, CancellationToken, Task<StepResult>> action,
        Func<TState, CancellationToken, Task<StepResult>> compensate)
    {
        _steps.Add(new SagaStep<TState>(name, action, compensate));
        return this;
    }

    /// <inheritdoc/>
    public async Task<SagaResult<TState>> ExecuteAsync(
        TState initialState = null,
        CancellationToken cancellationToken = default)
    {
        var sagaId = Guid.NewGuid().ToString();
        var state = initialState ?? new TState();
        var executedSteps = new Stack<ISagaStep<TState>>();
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Starting SAGA {SagaId} with {StepCount} steps", sagaId, _steps.Count);

        try
        {
            // Save initial state
            await _stateStore.SaveStateAsync(sagaId, state, SagaStatus.Running, cancellationToken)
                .ConfigureAwait(false);

            // Execute steps forward
            foreach (var step in _steps)
            {
                _logger.LogDebug("Executing step {StepName} in SAGA {SagaId}", step.Name, sagaId);

                try
                {
                    var result = await step.ExecuteAsync(state, cancellationToken).ConfigureAwait(false);

                    if (!result.Success)
                    {
                        _logger.LogWarning("Step {StepName} failed in SAGA {SagaId}: {Error}", 
                            step.Name, sagaId, result.Error);

                        // Start compensation
                        await CompensateAsync(sagaId, state, executedSteps, cancellationToken)
                            .ConfigureAwait(false);

                        return new SagaResult<TState>
                        {
                            SagaId = sagaId,
                            Success = false,
                            State = state,
                            Error = $"Step {step.Name} failed: {result.Error}",
                            Duration = DateTime.UtcNow - startTime,
                            CompensatedSteps = executedSteps.Select(s => s.Name).ToList()
                        };
                    }

                    executedSteps.Push(step);

                    // Update state after each successful step
                    await _stateStore.UpdateStepStatusAsync(sagaId, step.Name, StepStatus.Completed, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in step {StepName} of SAGA {SagaId}", step.Name, sagaId);

                    // Start compensation
                    await CompensateAsync(sagaId, state, executedSteps, cancellationToken)
                        .ConfigureAwait(false);

                    return new SagaResult<TState>
                    {
                        SagaId = sagaId,
                        Success = false,
                        State = state,
                        Error = $"Step {step.Name} threw exception: {ex.Message}",
                        Duration = DateTime.UtcNow - startTime,
                        CompensatedSteps = executedSteps.Select(s => s.Name).ToList()
                    };
                }
            }

            // All steps completed successfully
            await _stateStore.SaveStateAsync(sagaId, state, SagaStatus.Completed, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("SAGA {SagaId} completed successfully", sagaId);

            return new SagaResult<TState>
            {
                SagaId = sagaId,
                Success = true,
                State = state,
                Duration = DateTime.UtcNow - startTime,
                ExecutedSteps = _steps.Select(s => s.Name).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SAGA {SagaId}", sagaId);

            await _stateStore.SaveStateAsync(sagaId, state, SagaStatus.Failed, cancellationToken)
                .ConfigureAwait(false);

            return new SagaResult<TState>
            {
                SagaId = sagaId,
                Success = false,
                State = state,
                Error = $"Unexpected error: {ex.Message}",
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    private async Task CompensateAsync(
        string sagaId,
        TState state,
        Stack<ISagaStep<TState>> executedSteps,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting compensation for SAGA {SagaId} with {StepCount} steps to compensate", 
            sagaId, executedSteps.Count);

        await _stateStore.SaveStateAsync(sagaId, state, SagaStatus.Compensating, cancellationToken)
            .ConfigureAwait(false);

        while (executedSteps.Count > 0)
        {
            var step = executedSteps.Pop();

            try
            {
                _logger.LogDebug("Compensating step {StepName} in SAGA {SagaId}", step.Name, sagaId);

                var result = await step.CompensateAsync(state, cancellationToken).ConfigureAwait(false);

                if (!result.Success)
                {
                    _logger.LogError("Failed to compensate step {StepName} in SAGA {SagaId}: {Error}", 
                        step.Name, sagaId, result.Error);
                }

                await _stateStore.UpdateStepStatusAsync(sagaId, step.Name, StepStatus.Compensated, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception compensating step {StepName} in SAGA {SagaId}", step.Name, sagaId);
                // Continue compensating other steps even if one fails
            }
        }

        await _stateStore.SaveStateAsync(sagaId, state, SagaStatus.Compensated, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Compensation completed for SAGA {SagaId}", sagaId);
    }
}

/// <summary>
/// Interface for SAGA orchestrator.
/// </summary>
public interface ISagaOrchestrator<TState> where TState : class
{
    ISagaOrchestrator<TState> AddStep(ISagaStep<TState> step);
    ISagaOrchestrator<TState> AddStep(
        string name,
        Func<TState, CancellationToken, Task<StepResult>> action,
        Func<TState, CancellationToken, Task<StepResult>> compensate);
    Task<SagaResult<TState>> ExecuteAsync(TState initialState = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for a SAGA step.
/// </summary>
public interface ISagaStep<TState> where TState : class
{
    string Name { get; }
    Task<StepResult> ExecuteAsync(TState state, CancellationToken cancellationToken);
    Task<StepResult> CompensateAsync(TState state, CancellationToken cancellationToken);
}

/// <summary>
/// Implementation of a SAGA step.
/// </summary>
public class SagaStep<TState> : ISagaStep<TState> where TState : class
{
    private readonly Func<TState, CancellationToken, Task<StepResult>> _executeAction;
    private readonly Func<TState, CancellationToken, Task<StepResult>> _compensateAction;

    public string Name { get; }

    public SagaStep(
        string name,
        Func<TState, CancellationToken, Task<StepResult>> executeAction,
        Func<TState, CancellationToken, Task<StepResult>> compensateAction)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _executeAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
        _compensateAction = compensateAction ?? throw new ArgumentNullException(nameof(compensateAction));
    }

    public Task<StepResult> ExecuteAsync(TState state, CancellationToken cancellationToken)
    {
        return _executeAction(state, cancellationToken);
    }

    public Task<StepResult> CompensateAsync(TState state, CancellationToken cancellationToken)
    {
        return _compensateAction(state, cancellationToken);
    }
}

/// <summary>
/// Result of a SAGA step execution.
/// </summary>
public class StepResult
{
    public bool Success { get; set; }
    public string Error { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();

    public static StepResult Ok() => new() { Success = true };
    public static StepResult Fail(string error) => new() { Success = false, Error = error };
}

/// <summary>
/// Result of a SAGA execution.
/// </summary>
public class SagaResult<TState> where TState : class
{
    public string SagaId { get; set; }
    public bool Success { get; set; }
    public TState State { get; set; }
    public string Error { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> ExecutedSteps { get; set; } = new();
    public List<string> CompensatedSteps { get; set; } = new();
}

/// <summary>
/// Interface for SAGA state storage.
/// </summary>
public interface ISagaStateStore<TState> where TState : class
{
    Task SaveStateAsync(string sagaId, TState state, SagaStatus status, CancellationToken cancellationToken);
    Task<TState> GetStateAsync(string sagaId, CancellationToken cancellationToken);
    Task UpdateStepStatusAsync(string sagaId, string stepName, StepStatus status, CancellationToken cancellationToken);
}

/// <summary>
/// SAGA status enumeration.
/// </summary>
public enum SagaStatus
{
    Created,
    Running,
    Completed,
    Failed,
    Compensating,
    Compensated
}

/// <summary>
/// Step status enumeration.
/// </summary>
public enum StepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Compensated
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Services.Automation.Services
{
    /// <summary>
    /// Service for evaluating automation conditions with extensible handlers.
    /// </summary>
    public class ConditionEvaluationService : IConditionEvaluationService
    {
        private readonly ILogger<ConditionEvaluationService> _logger;
        private readonly Dictionary<AutomationConditionType, IConditionHandler> _handlers;

        public ConditionEvaluationService(ILogger<ConditionEvaluationService> logger)
        {
            _logger = logger;
            _handlers = new Dictionary<AutomationConditionType, IConditionHandler>();
        }

        public async Task<bool> CheckConditionsAsync(AutomationCondition[] conditions)
        {
            if (conditions == null || conditions.Length == 0)
                return true;

            _logger.LogDebug("Evaluating {Count} conditions", conditions.Length);

            foreach (var condition in conditions)
            {
                var result = await CheckSingleConditionAsync(condition).ConfigureAwait(false);
                if (!result)
                {
                    _logger.LogDebug("Condition {Type} failed: {Field} {Operator} {Value}",
                        condition.Type, condition.Field, condition.Operator, condition.Value);
                    return false;
                }
            }

            _logger.LogDebug("All conditions passed");
            return true;
        }

        public async Task<bool> CheckSingleConditionAsync(AutomationCondition condition)
        {
            ArgumentNullException.ThrowIfNull(condition);

            // Check for registered handler
            if (_handlers.TryGetValue(condition.Type, out var handler))
            {
                try
                {
                    return await handler.EvaluateAsync(condition).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating condition {Type}", condition.Type);
                    return false;
                }
            }

            // Fallback to default evaluation
            _logger.LogWarning("No handler registered for condition type {Type}, using default evaluation",
                condition.Type);

            return await DefaultConditionEvaluationAsync(condition).ConfigureAwait(false);
        }

        public void RegisterHandler(AutomationConditionType type, IConditionHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            _handlers[type] = handler;
            _logger.LogInformation("Registered handler for condition type {Type}", type);
        }

        public IConditionHandler? GetHandler(AutomationConditionType type)
        {
            return _handlers.GetValueOrDefault(type);
        }

        private async Task<bool> DefaultConditionEvaluationAsync(AutomationCondition condition)
        {
            // Basic string comparison for default evaluation
            var actualValue = condition.Field;
            var expectedValue = condition.Value;

            var result = condition.Operator.ToLowerInvariant() switch
            {
                "equals" or "==" => actualValue == expectedValue,
                "notequals" or "!=" => actualValue != expectedValue,
                "contains" => actualValue?.Contains(expectedValue ?? "") ?? false,
                "startswith" => actualValue?.StartsWith(expectedValue ?? "") ?? false,
                "endswith" => actualValue?.EndsWith(expectedValue ?? "") ?? false,
                _ => false
            };

            await Task.CompletedTask.ConfigureAwait(false);
            return result;
        }
    }

    /// <summary>
    /// Base implementation for condition handlers.
    /// </summary>
    public abstract class ConditionHandlerBase : IConditionHandler
    {
        protected readonly ILogger _logger;

        protected ConditionHandlerBase(ILogger logger)
        {
            _logger = logger;
        }

        public abstract AutomationConditionType SupportedType { get; }

        public abstract Task<bool> EvaluateAsync(AutomationCondition condition);

        protected bool CompareValues(string actualValue, string expectedValue, string operatorType)
        {
            return operatorType.ToLowerInvariant() switch
            {
                "equals" or "==" => actualValue == expectedValue,
                "notequals" or "!=" => actualValue != expectedValue,
                "greater" or ">" => CompareNumeric(actualValue, expectedValue) > 0,
                "less" or "<" => CompareNumeric(actualValue, expectedValue) < 0,
                "greaterequal" or ">=" => CompareNumeric(actualValue, expectedValue) >= 0,
                "lessequal" or "<=" => CompareNumeric(actualValue, expectedValue) <= 0,
                "contains" => actualValue?.Contains(expectedValue ?? "") ?? false,
                "startswith" => actualValue?.StartsWith(expectedValue ?? "") ?? false,
                "endswith" => actualValue?.EndsWith(expectedValue ?? "") ?? false,
                _ => false
            };
        }

        private int CompareNumeric(string actual, string expected)
        {
            if (decimal.TryParse(actual, out var actualNum) &&
                decimal.TryParse(expected, out var expectedNum))
            {
                return actualNum.CompareTo(expectedNum);
            }

            return string.Compare(actual, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}

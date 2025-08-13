using System.Threading.Tasks;

namespace NeoServiceLayer.Services.Automation.Services
{
    /// <summary>
    /// Interface for evaluating automation conditions.
    /// </summary>
    public interface IConditionEvaluationService
    {
        /// <summary>
        /// Checks if all conditions are met.
        /// </summary>
        Task<bool> CheckConditionsAsync(AutomationCondition[] conditions);

        /// <summary>
        /// Evaluates a single condition.
        /// </summary>
        Task<bool> CheckSingleConditionAsync(AutomationCondition condition);

        /// <summary>
        /// Registers a condition handler for a specific type.
        /// </summary>
        void RegisterHandler(AutomationConditionType type, IConditionHandler handler);

        /// <summary>
        /// Gets a registered handler for a condition type.
        /// </summary>
        IConditionHandler? GetHandler(AutomationConditionType type);
    }

    /// <summary>
    /// Interface for condition handlers.
    /// </summary>
    public interface IConditionHandler
    {
        /// <summary>
        /// Evaluates a condition.
        /// </summary>
        Task<bool> EvaluateAsync(AutomationCondition condition);

        /// <summary>
        /// Gets the condition type this handler supports.
        /// </summary>
        AutomationConditionType SupportedType { get; }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NeoServiceLayer.Api.Controllers;

namespace NeoServiceLayer.Api.Filters;

/// <summary>
/// Global validation filter that automatically validates model state and returns standardized error responses.
/// </summary>
public class ValidationFilter : IActionFilter
{
    private readonly ILogger<ValidationFilter> _logger;

    public ValidationFilter(ILogger<ValidationFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed",
                Timestamp = DateTime.UtcNow,
                Errors = errors
            };

            _logger.LogWarning("Validation failed for {Controller}.{Action}: {Errors}",
                context.Controller.GetType().Name,
                context.ActionDescriptor.DisplayName,
                string.Join(", ", errors.SelectMany(e => e.Value)));

            context.Result = new BadRequestObjectResult(response);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action needed after execution
    }
}

/// <summary>
/// Attribute to bypass validation for specific actions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class BypassValidationAttribute : Attribute
{
}

/// <summary>
/// Filter to check for bypass validation attribute.
/// </summary>
public class ConditionalValidationFilter : IActionFilter
{
    private readonly ValidationFilter _validationFilter;

    public ConditionalValidationFilter(ILogger<ValidationFilter> logger)
    {
        _validationFilter = new ValidationFilter(logger);
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var bypassValidation = context.ActionDescriptor.EndpointMetadata
            .Any(m => m is BypassValidationAttribute);

        if (!bypassValidation)
        {
            _validationFilter.OnActionExecuting(context);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        _validationFilter.OnActionExecuted(context);
    }
}

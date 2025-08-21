using HotChocolate;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Api.GraphQL;

/// <summary>
/// GraphQL error filter for consistent error handling.
/// </summary>
public class GraphQLErrorFilter : IErrorFilter
{
    private readonly ILogger<GraphQLErrorFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLErrorFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles GraphQL errors.
    /// </summary>
    /// <param name="error">The error to handle.</param>
    /// <returns>The processed error.</returns>
    public IError OnError(IError error)
    {
        _logger.LogError(error.Exception, "GraphQL error: {Message}", error.Message);

        if (error.Exception is UnauthorizedAccessException)
        {
            return ErrorBuilder.New()
                .SetMessage("Unauthorized access")
                .SetCode("UNAUTHORIZED")
                .AddLocation(error.Locations?.FirstOrDefault())
                .SetPath(error.Path)
                .Build();
        }

        if (error.Exception is ArgumentException || error.Exception is ValidationException)
        {
            return ErrorBuilder.New()
                .SetMessage(error.Exception.Message)
                .SetCode("VALIDATION_ERROR")
                .AddLocation(error.Locations?.FirstOrDefault())
                .SetPath(error.Path)
                .Build();
        }

        if (error.Exception is InvalidOperationException)
        {
            return ErrorBuilder.New()
                .SetMessage(error.Exception.Message)
                .SetCode("INVALID_OPERATION")
                .AddLocation(error.Locations?.FirstOrDefault())
                .SetPath(error.Path)
                .Build();
        }

        // For all other errors, return a generic message in production
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        
        return ErrorBuilder.New()
            .SetMessage(isDevelopment ? error.Message : "An error occurred while processing your request")
            .SetCode("INTERNAL_ERROR")
            .AddLocation(error.Locations?.FirstOrDefault())
            .SetPath(error.Path)
            .Build();
    }
}
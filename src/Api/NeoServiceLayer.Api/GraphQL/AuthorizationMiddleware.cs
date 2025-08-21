using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Authorization;

namespace NeoServiceLayer.Api.GraphQL;

/// <summary>
/// Authorization middleware for GraphQL field execution.
/// </summary>
public class AuthorizationMiddleware : IFieldMiddleware
{
    private readonly FieldDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next field delegate.</param>
    public AuthorizationMiddleware(FieldDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The middleware context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(IMiddlewareContext context)
    {
        var authorizeDirective = context.Field.Directives
            .FirstOrDefault(d => d.Name.Value == "authorize");

        if (authorizeDirective != null)
        {
            var httpContext = context.GetHttpContext();
            var authService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();
            var user = httpContext.User;

            // Extract roles from directive
            var roles = authorizeDirective.Arguments
                .FirstOrDefault(a => a.Name.Value == "roles")
                ?.Value?.Value?.ToString()?.Split(',') ?? Array.Empty<string>();

            // Check if user is authenticated
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.ReportError(
                    ErrorBuilder.New()
                        .SetMessage("Authentication required")
                        .SetCode("UNAUTHENTICATED")
                        .Build());
                return;
            }

            // Check roles if specified
            if (roles.Any() && !roles.Any(role => user.IsInRole(role.Trim())))
            {
                context.ReportError(
                    ErrorBuilder.New()
                        .SetMessage("Insufficient permissions")
                        .SetCode("FORBIDDEN")
                        .Build());
                return;
            }
        }

        await _next(context);
    }
}
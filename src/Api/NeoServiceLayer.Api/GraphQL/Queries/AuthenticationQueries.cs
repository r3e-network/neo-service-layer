using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NeoServiceLayer.Services.Authentication;

namespace NeoServiceLayer.Api.GraphQL.Queries;

/// <summary>
/// GraphQL queries for authentication operations.
/// </summary>
[ExtendObjectType(typeof(Query))]
public class AuthenticationQueries
{
    /// <summary>
    /// Gets the current user information.
    /// </summary>
    [Authorize]
    [GraphQLDescription("Gets the current authenticated user's information")]
    public async Task<User?> GetCurrentUser(
        [Service] IAuthenticationService authService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var username = httpContextAccessor.HttpContext?.User?.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return null;
            
        return await authService.GetUserByUsernameAsync(username);
    }

    /// <summary>
    /// Gets all users (admin only).
    /// </summary>
    [Authorize(Roles = ["Admin"])]
    [GraphQLDescription("Gets all users in the system")]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public async Task<IEnumerable<User>> GetUsers(
        [Service] IAuthenticationService authService)
    {
        return await authService.GetAllUsersAsync();
    }
}
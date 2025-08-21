using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Types;
using NeoServiceLayer.Services.Authentication;

namespace NeoServiceLayer.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class AuthenticationMutations
{
    [GraphQLDescription("Authenticates a user")]
    public async Task<AuthenticationResult> Login(
        string username,
        string password,
        [Service] IAuthenticationService authService)
    {
        return await authService.AuthenticateAsync(username, password);
    }
    
    [Authorize]
    [GraphQLDescription("Logs out the current user")]
    public async Task<bool> Logout(
        [Service] IAuthenticationService authService,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        await authService.LogoutAsync(httpContextAccessor.HttpContext\!);
        return true;
    }
}

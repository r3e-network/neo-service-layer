using HotChocolate;
using HotChocolate.Types;
using NeoServiceLayer.Services.Authentication;

namespace NeoServiceLayer.Api.GraphQL.Types;

/// <summary>
/// GraphQL type for User.
/// </summary>
public class UserType : ObjectType<User>
{
    /// <summary>
    /// Configures the User type.
    /// </summary>
    /// <param name="descriptor">The type descriptor.</param>
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Name("User");
        descriptor.Description("User account information");

        descriptor
            .Field(f => f.Id)
            .Description("Unique identifier for the user");

        descriptor
            .Field(f => f.Username)
            .Description("Username for login");

        descriptor
            .Field(f => f.Email)
            .Description("Email address");

        descriptor
            .Field(f => f.Roles)
            .Description("Roles assigned to the user");

        descriptor
            .Field(f => f.CreatedAt)
            .Description("When the user account was created");

        descriptor
            .Field(f => f.LastLoginAt)
            .Description("When the user last logged in");

        descriptor
            .Field(f => f.IsActive)
            .Description("Whether the user account is active");

        descriptor
            .Field(f => f.IsTwoFactorEnabled)
            .Description("Whether two-factor authentication is enabled");

        // Don't expose password hash
        descriptor.Ignore(f => f.PasswordHash);

        // Add computed fields
        descriptor
            .Field("isOnline")
            .Type<NonNullType<BooleanType>>()
            .Description("Whether the user is currently online")
            .Resolve(ctx =>
            {
                var user = ctx.Parent<User>();
                return user.LastLoginAt.HasValue && 
                       DateTime.UtcNow - user.LastLoginAt.Value < TimeSpan.FromMinutes(15);
            });

        descriptor
            .Field("accountAge")
            .Type<NonNullType<StringType>>()
            .Description("Age of the account in human-readable format")
            .Resolve(ctx =>
            {
                var user = ctx.Parent<User>();
                var age = DateTime.UtcNow - user.CreatedAt;
                
                if (age.TotalDays >= 365)
                    return $"{(int)(age.TotalDays / 365)} years";
                else if (age.TotalDays >= 30)
                    return $"{(int)(age.TotalDays / 30)} months";
                else if (age.TotalDays >= 1)
                    return $"{(int)age.TotalDays} days";
                else
                    return "New user";
            });
    }
}
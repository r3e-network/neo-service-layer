using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Core.CQRS;
using NeoServiceLayer.Services.Authentication.Commands;
using NeoServiceLayer.Services.Authentication.Infrastructure;
using NeoServiceLayer.Services.Authentication.Projections;
using NeoServiceLayer.Services.Authentication.Queries;
using NeoServiceLayer.Services.Authentication.Services;
using NeoServiceLayer.ServiceFramework;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Authentication
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddAuthenticationService(this IServiceCollection services)
        {
            // Infrastructure
            services.AddSingleton<IUserReadModelStore, InMemoryUserReadModelStore>();
            services.AddSingleton<ISessionReadModelStore, InMemorySessionReadModelStore>();
            services.AddSingleton<ITokenReadModelStore, InMemoryTokenReadModelStore>();

            // Services
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ITwoFactorService, TwoFactorService>();
            services.AddScoped<IEmailService, EmailService>();

            // Command Handlers
            services.AddScoped<ICommandHandler<CreateUserCommand, Guid>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<DeleteUserCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<SuspendUserCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<ReactivateUserCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<LoginCommand, LoginResult>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<LogoutCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<RefreshTokenCommand, TokenResult>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<RevokeRefreshTokenCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<VerifyEmailCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<ResendVerificationEmailCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<ChangePasswordCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<InitiatePasswordResetCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<CompletePasswordResetCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<EnableTwoFactorCommand, TwoFactorSetupResult>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<DisableTwoFactorCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<VerifyTwoFactorCommand, bool>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<AssignRoleCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<RemoveRoleCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<GrantPermissionCommand>, AuthenticationCommandHandlers>();
            services.AddScoped<ICommandHandler<RevokePermissionCommand>, AuthenticationCommandHandlers>();

            // Query Handlers
            services.AddScoped<IQueryHandler<GetUserByIdQuery, UserDto?>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetUserByUsernameQuery, UserDto?>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetUserByEmailQuery, UserDto?>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<SearchUsersQuery, PagedResult<UserDto>>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetUserSessionsQuery, List<SessionDto>>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetSessionByIdQuery, SessionDto?>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetLoginHistoryQuery, List<LoginAttemptDto>>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetFailedLoginAttemptsQuery, List<LoginAttemptDto>>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetUserRolesQuery, List<string>>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetUserPermissionsQuery, List<string>>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetUsersWithRoleQuery, List<UserDto>>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetUsersWithPermissionQuery, List<UserDto>>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetActiveRefreshTokensQuery, List<RefreshTokenDto>>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<ValidateRefreshTokenQuery, RefreshTokenValidationResult>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetUserStatisticsQuery, UserStatistics>, AuthenticationQueryHandlers>();
            services.AddScoped<IQueryHandler<GetLoginStatisticsQuery, LoginStatistics>, AuthenticationQueryHandlers>();

            // Projections
            services.AddScoped<AuthenticationProjection>();

            return services;
        }
    }
}
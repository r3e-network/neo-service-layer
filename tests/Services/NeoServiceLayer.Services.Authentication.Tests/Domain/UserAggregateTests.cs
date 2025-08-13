using System;
using System.Linq;
using FluentAssertions;
using NeoServiceLayer.Services.Authentication.Domain.Aggregates;
using NeoServiceLayer.Services.Authentication.Domain.Events;
using Xunit;

namespace NeoServiceLayer.Services.Authentication.Tests.Domain
{
    public class UserAggregateTests
    {
        [Fact]
        public void Create_Should_RaiseUserCreatedEvent()
        {
            // Arrange
            var username = "testuser";
            var email = "test@example.com";
            var passwordHash = "hashedpassword";
            var roles = new[] { "User", "Admin" };

            // Act
            var user = User.Create(username, email, passwordHash, roles);

            // Assert
            user.Should().NotBeNull();
            user.Id.Should().NotBeEmpty();
            user.Username.Should().Be(username);
            user.Email.Should().Be(email);
            user.PasswordHash.Should().Be(passwordHash);
            user.Status.Should().Be(UserStatus.Active);
            user.Roles.Should().BeEquivalentTo(roles);

            var events = user.GetUncommittedEvents().ToList();
            events.Should().HaveCount(1);
            events[0].Should().BeOfType<UserCreatedEvent>();
        }

        [Fact]
        public void VerifyEmail_Should_RaiseEmailVerifiedEvent()
        {
            // Arrange
            var user = CreateTestUser();
            var verificationToken = user.EmailVerificationToken!;

            // Act
            user.VerifyEmail(verificationToken);

            // Assert
            user.EmailVerifiedAt.Should().NotBeNull();
            
            var events = user.GetUncommittedEvents().ToList();
            events.Should().HaveCount(2); // UserCreatedEvent + EmailVerifiedEvent
            events[1].Should().BeOfType<EmailVerifiedEvent>();
        }

        [Fact]
        public void VerifyEmail_WithInvalidToken_Should_ThrowException()
        {
            // Arrange
            var user = CreateTestUser();

            // Act & Assert
            var act = () => user.VerifyEmail("invalid-token");
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Invalid verification token");
        }

        [Fact]
        public void Login_Should_RaiseUserLoggedInEvent()
        {
            // Arrange
            var user = CreateTestUser();
            var ipAddress = "192.168.1.1";
            var userAgent = "Mozilla/5.0";
            var deviceId = "device123";

            // Act
            user.Login(ipAddress, userAgent, deviceId);

            // Assert
            user.LastLoginAt.Should().NotBeNull();
            user.Sessions.Should().HaveCount(1);
            user.Sessions[0].IpAddress.Should().Be(ipAddress);
            
            var events = user.GetUncommittedEvents().ToList();
            events.Should().HaveCount(2);
            events[1].Should().BeOfType<UserLoggedInEvent>();
        }

        [Fact]
        public void Login_WhenSuspended_Should_ThrowException()
        {
            // Arrange
            var user = CreateTestUser();
            user.Suspend("Test suspension");

            // Act & Assert
            var act = () => user.Login("192.168.1.1", "Mozilla", null);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot login - user status is Suspended");
        }

        [Fact]
        public void RecordFailedLogin_AfterFiveAttempts_Should_LockAccount()
        {
            // Arrange
            var user = CreateTestUser();
            var ipAddress = "192.168.1.1";

            // Act
            for (int i = 0; i < 5; i++)
            {
                user.RecordFailedLogin(ipAddress, "Invalid password");
            }

            // Assert
            user.FailedLoginAttempts.Should().Be(5);
            user.LockedUntil.Should().NotBeNull();
            
            var events = user.GetUncommittedEvents().ToList();
            events.Should().HaveCount(11); // 1 UserCreatedEvent + 5 LoginFailedEvents + 5 AccountLockedEvents
            events.Last().Should().BeOfType<AccountLockedEvent>();
        }

        [Fact]
        public void ChangePassword_Should_RevokeAllRefreshTokens()
        {
            // Arrange
            var user = CreateTestUser();
            var token1 = user.IssueRefreshToken("token1", DateTime.UtcNow.AddDays(30));
            var token2 = user.IssueRefreshToken("token2", DateTime.UtcNow.AddDays(30));
            var newPasswordHash = "newhashedpassword";

            // Act
            user.ChangePassword(newPasswordHash);

            // Assert
            user.PasswordHash.Should().Be(newPasswordHash);
            user.LastPasswordChangeAt.Should().NotBeNull();
            
            var events = user.GetUncommittedEvents().ToList();
            var passwordChangedEvent = events.OfType<PasswordChangedEvent>().FirstOrDefault();
            passwordChangedEvent.Should().NotBeNull();
            
            var revokedTokenEvents = events.OfType<RefreshTokenRevokedEvent>().ToList();
            revokedTokenEvents.Should().HaveCount(2);
        }

        [Fact]
        public void EnableTwoFactorAuthentication_Should_RaiseTwoFactorEnabledEvent()
        {
            // Arrange
            var user = CreateTestUser();
            var totpSecret = "JBSWY3DPEHPK3PXP";

            // Act
            user.EnableTwoFactorAuthentication(totpSecret);

            // Assert
            user.IsTwoFactorEnabled.Should().BeTrue();
            user.TotpSecret.Should().Be(totpSecret);
            
            var events = user.GetUncommittedEvents().ToList();
            events.Should().HaveCount(2);
            events[1].Should().BeOfType<TwoFactorEnabledEvent>();
        }

        [Fact]
        public void AssignRole_Should_RaiseRoleAssignedEvent()
        {
            // Arrange
            var user = CreateTestUser();
            var role = "Admin";

            // Act
            user.AssignRole(role);

            // Assert
            user.Roles.Should().Contain(role);
            
            var events = user.GetUncommittedEvents().ToList();
            events.Should().HaveCount(2);
            events[1].Should().BeOfType<RoleAssignedEvent>();
        }

        [Fact]
        public void AssignRole_WhenAlreadyAssigned_Should_ThrowException()
        {
            // Arrange
            var user = CreateTestUser();
            var role = "Admin";
            user.AssignRole(role);

            // Act & Assert
            var act = () => user.AssignRole(role);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"Role {role} already assigned");
        }

        [Fact]
        public void CompletePasswordReset_Should_LogoutAllSessionsAndRevokeTokens()
        {
            // Arrange
            var user = CreateTestUser();
            user.Login("192.168.1.1", "Mozilla", "device1");
            user.Login("192.168.1.2", "Chrome", "device2");
            user.IssueRefreshToken("token1", DateTime.UtcNow.AddDays(30));
            user.IssueRefreshToken("token2", DateTime.UtcNow.AddDays(30));
            
            var resetToken = "reset-token-123";
            user.InitiatePasswordReset(resetToken, DateTime.UtcNow.AddHours(1));
            var newPasswordHash = "newhashedpassword";

            // Act
            user.CompletePasswordReset(resetToken, newPasswordHash);

            // Assert
            user.PasswordHash.Should().Be(newPasswordHash);
            user.PasswordResetToken.Should().BeNull();
            
            var events = user.GetUncommittedEvents().ToList();
            var logoutEvents = events.OfType<UserLoggedOutEvent>().ToList();
            logoutEvents.Should().HaveCount(2);
            
            var revokedTokenEvents = events.OfType<RefreshTokenRevokedEvent>().ToList();
            revokedTokenEvents.Should().HaveCount(2);
        }

        [Fact]
        public void Delete_Should_RaiseUserDeletedEvent()
        {
            // Arrange
            var user = CreateTestUser();

            // Act
            user.Delete();

            // Assert
            user.Status.Should().Be(UserStatus.Deleted);
            
            var events = user.GetUncommittedEvents().ToList();
            events.Should().HaveCount(2);
            events[1].Should().BeOfType<UserDeletedEvent>();
        }

        private User CreateTestUser()
        {
            return User.Create(
                "testuser",
                "test@example.com",
                "hashedpassword",
                new[] { "User" });
        }
    }
}
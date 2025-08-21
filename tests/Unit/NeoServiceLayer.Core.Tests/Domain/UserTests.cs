using System;
using System.Linq;
using NeoServiceLayer.Core.Domain;
using Xunit;

namespace NeoServiceLayer.Core.Tests.Domain
{
    /// <summary>
    /// Unit tests for the User domain aggregate
    /// </summary>
    public class UserTests
    {
        [Fact]
        public void CreateUser_WithValidData_ShouldSucceed()
        {
            // Arrange
            var userId = UserId.Create(Guid.NewGuid());
            var username = Username.Create("testuser");
            var email = EmailAddress.Create("test@example.com");
            var password = Password.Create("SecurePassword123!");
            var policy = new EnterprisePasswordPolicy();

            // Act
            var user = User.Create(userId, username, email, password);

            // Assert
            Assert.Equal(userId, user.Id);
            Assert.Equal(username, user.Username);
            Assert.Equal(email, user.Email);
            Assert.Equal(password, user.Password);
            Assert.False(user.IsLocked);
            Assert.Single(user.DomainEvents);
            Assert.IsType<UserCreatedEvent>(user.DomainEvents.First());
        }

        [Fact]
        public void CreateUser_WithNullUserId_ShouldThrowArgumentNullException()
        {
            // Arrange
            var username = Username.Create("testuser");
            var email = EmailAddress.Create("test@example.com");
            var password = Password.Create("SecurePassword123!");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                User.Create(null, username, email, password));
        }

        [Fact]
        public void Authenticate_WithCorrectPassword_ShouldSucceed()
        {
            // Arrange
            var user = CreateTestUser();
            var policy = new EnterprisePasswordPolicy();
            var plainPassword = "SecurePassword123!";

            // Act
            var result = user.Authenticate(plainPassword, policy);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains(user.DomainEvents, e => e is AuthenticationSucceededEvent);
        }

        [Fact]
        public void Authenticate_WithIncorrectPassword_ShouldFail()
        {
            // Arrange
            var user = CreateTestUser();
            var policy = new EnterprisePasswordPolicy();
            var wrongPassword = "WrongPassword";

            // Act
            var result = user.Authenticate(wrongPassword, policy);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid password", result.FailureReason);
        }

        [Fact]
        public void Authenticate_WhenAccountLocked_ShouldFail()
        {
            // Arrange
            var user = CreateTestUser();
            var policy = new EnterprisePasswordPolicy();
            user.LockAccount("Test lock");

            // Act
            var result = user.Authenticate("SecurePassword123!", policy);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Test lock", result.FailureReason);
        }

        [Fact]
        public void LockAccount_ShouldSetLockedStateAndRaiseEvent()
        {
            // Arrange
            var user = CreateTestUser();
            var lockReason = "Too many failed attempts";

            // Act
            user.LockAccount(lockReason);

            // Assert
            Assert.True(user.IsLocked);
            Assert.Equal(lockReason, user.LockReason);
            Assert.Contains(user.DomainEvents, e => e is AccountLockedEvent);
        }

        [Fact]
        public void UnlockAccount_ShouldClearLockedStateAndRaiseEvent()
        {
            // Arrange
            var user = CreateTestUser();
            user.LockAccount("Test lock");

            // Act
            user.UnlockAccount();

            // Assert
            Assert.False(user.IsLocked);
            Assert.Null(user.LockReason);
            Assert.Contains(user.DomainEvents, e => e is AccountUnlockedEvent);
        }

        [Fact]
        public void ChangePassword_WithValidPassword_ShouldSucceed()
        {
            // Arrange
            var user = CreateTestUser();
            var newPassword = Password.Create("NewSecurePassword456!");

            // Act
            user.ChangePassword(newPassword);

            // Assert
            Assert.Equal(newPassword, user.Password);
            Assert.Contains(user.DomainEvents, e => e is PasswordChangedEvent);
        }

        [Fact]
        public void AddRole_WithValidRole_ShouldSucceed()
        {
            // Arrange
            var user = CreateTestUser();
            var role = Role.Admin;

            // Act
            user.AddRole(role);

            // Assert
            Assert.Contains(role, user.Roles);
            Assert.Contains(user.DomainEvents, e => e is RoleAddedEvent);
        }

        [Fact]
        public void RemoveRole_WithExistingRole_ShouldSucceed()
        {
            // Arrange
            var user = CreateTestUser();
            var role = Role.Admin;
            user.AddRole(role);

            // Act
            user.RemoveRole(role);

            // Assert
            Assert.DoesNotContain(role, user.Roles);
            Assert.Contains(user.DomainEvents, e => e is RoleRemovedEvent);
        }

        [Fact]
        public void HasRole_WithExistingRole_ShouldReturnTrue()
        {
            // Arrange
            var user = CreateTestUser();
            var role = Role.User;
            user.AddRole(role);

            // Act
            var hasRole = user.HasRole(role);

            // Assert
            Assert.True(hasRole);
        }

        [Fact]
        public void HasRole_WithNonExistingRole_ShouldReturnFalse()
        {
            // Arrange
            var user = CreateTestUser();
            var role = Role.Admin;

            // Act
            var hasRole = user.HasRole(role);

            // Assert
            Assert.False(hasRole);
        }

        private static User CreateTestUser()
        {
            var userId = UserId.Create(Guid.NewGuid());
            var username = Username.Create("testuser");
            var email = EmailAddress.Create("test@example.com");
            var password = Password.Create("SecurePassword123!");

            var user = User.Create(userId, username, email, password);
            user.ClearDomainEvents(); // Clear creation event for cleaner test assertions
            
            return user;
        }
    }
}
using System;
using NeoServiceLayer.Core.Domain;
using NeoServiceLayer.Core.Domain.ValueObjects;
using NeoServiceLayer.Core.Domain.Policies;
using NeoServiceLayer.Core.Domain.Events;
using Xunit;

namespace NeoServiceLayer.Architecture.Validation
{
    /// <summary>
    /// Architectural validation tests for professional Neo Service Layer improvements
    /// </summary>
    public class CoreArchitectureValidationTests
    {
        [Fact]
        public void PasswordPolicy_ValidatesPasswordCorrectly()
        {
            // Arrange
            var policy = new EnterprisePasswordPolicy();
            var validPassword = "SecurePassword123!";
            var invalidPassword = "weak";

            // Act
            var validResult = policy.ValidatePassword(validPassword);
            var invalidResult = policy.ValidatePassword(invalidPassword);

            // Assert
            Assert.True(validResult.IsValid, "Enterprise password policy should validate strong passwords");
            Assert.False(invalidResult.IsValid, "Enterprise password policy should reject weak passwords");
            Assert.NotEmpty(invalidResult.Errors);
        }

        [Fact]
        public void ValueObjects_ImplementEqualityCorrectly()
        {
            // Test Username equality
            var username1 = Username.Create("testuser");
            var username2 = Username.Create("testuser");
            var username3 = Username.Create("different");

            Assert.Equal(username1, username2);
            Assert.NotEqual(username1, username3);
            Assert.Equal(username1.GetHashCode(), username2.GetHashCode());

            // Test EmailAddress equality
            var email1 = EmailAddress.Create("test@example.com");
            var email2 = EmailAddress.Create("test@example.com");
            var email3 = EmailAddress.Create("other@example.com");

            Assert.Equal(email1, email2);
            Assert.NotEqual(email1, email3);
        }

        [Fact]
        public void Password_HashingAndVerificationWorks()
        {
            // Arrange
            var plainPassword = "SecurePassword123!";

            // Act
            var password = Password.Create(plainPassword);
            var validVerification = password.Verify(plainPassword);
            var invalidVerification = password.Verify("wrongpassword");

            // Assert
            Assert.NotNull(password);
            Assert.True(validVerification, "Password should verify with correct input");
            Assert.False(invalidVerification, "Password should fail with incorrect input");
        }

        [Fact]
        public void User_RichDomainModel_HasBusinessBehavior()
        {
            // Arrange
            var policy = new EnterprisePasswordPolicy();
            var userId = UserId.Create(Guid.NewGuid());
            var username = Username.Create("testuser");
            var email = EmailAddress.Create("test@example.com");
            var password = Password.Create("SecurePassword123!");

            // Act
            var user = User.Create(userId, username, email, password);
            var authResult = user.Authenticate("SecurePassword123!", policy);

            // Assert
            Assert.NotNull(user);
            Assert.True(authResult.IsSuccess, "User should authenticate with correct password");
            Assert.True(user.DomainEvents.Count > 0, "User creation should generate domain events");
            Assert.Contains(user.DomainEvents, e => e is UserCreatedEvent);
        }

        [Fact]
        public void Entity_IdentityEqualityWorks()
        {
            // Arrange
            var userId1 = UserId.Create(Guid.NewGuid());
            var userId2 = UserId.Create(userId1.Value); // Same ID
            var userId3 = UserId.Create(Guid.NewGuid()); // Different ID

            var username = Username.Create("testuser");
            var email = EmailAddress.Create("test@example.com");
            var password = Password.Create("SecurePassword123!");

            // Act
            var user1 = User.Create(userId1, username, email, password);
            var user2 = User.Create(userId2, username, email, password);
            var user3 = User.Create(userId3, username, email, password);

            // Assert
            Assert.Equal(user1, user2); // Same ID = equal entities
            Assert.NotEqual(user1, user3); // Different ID = different entities
        }

        [Fact]
        public void Role_ValueObject_WorksCorrectly()
        {
            // Act
            var role1 = Role.Create("Admin", "Administrator role");
            var role2 = Role.Create("Admin", "Administrator role");
            var role3 = Role.Create("User", "Regular user role");

            // Assert
            Assert.NotNull(role1);
            Assert.Equal("Admin", role1.Name);
            Assert.Equal("Administrator role", role1.Description);
            Assert.Equal(role1, role2); // Same values = equal
            Assert.NotEqual(role1, role3); // Different values = not equal
        }

        [Fact]
        public void DomainEvents_AreGeneratedCorrectly()
        {
            // Arrange
            var userId = UserId.Create(Guid.NewGuid());
            var username = Username.Create("testuser");
            var email = EmailAddress.Create("test@example.com");
            var password = Password.Create("SecurePassword123!");

            // Act
            var user = User.Create(userId, username, email, password);

            // Assert - Domain events should be generated
            Assert.NotEmpty(user.DomainEvents);
            
            // Should have UserCreatedEvent
            var userCreatedEvent = user.DomainEvents
                .FirstOrDefault(e => e is UserCreatedEvent) as UserCreatedEvent;
            
            Assert.NotNull(userCreatedEvent);
            Assert.Equal(userId.Value, userCreatedEvent.UserId);
            Assert.Equal(username.Value, userCreatedEvent.Username);
        }

        [Fact]
        public void User_FailedAuthentication_GeneratesEvents()
        {
            // Arrange
            var policy = new EnterprisePasswordPolicy();
            var userId = UserId.Create(Guid.NewGuid());
            var username = Username.Create("testuser");
            var email = EmailAddress.Create("test@example.com");
            var password = Password.Create("SecurePassword123!");
            var user = User.Create(userId, username, email, password);
            
            // Clear creation events to focus on authentication
            user.ClearDomainEvents();

            // Act - Authenticate with wrong password
            var failedAuth = user.Authenticate("wrongpassword", policy);

            // Assert
            Assert.False(failedAuth.IsSuccess, "Authentication should fail with wrong password");
            Assert.NotEmpty(user.DomainEvents);
            
            // Should generate AuthenticationFailedEvent
            var authFailedEvent = user.DomainEvents
                .FirstOrDefault(e => e is AuthenticationFailedEvent);
            
            Assert.NotNull(authFailedEvent);
        }
    }
}
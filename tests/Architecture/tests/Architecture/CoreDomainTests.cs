using System;
using NeoServiceLayer.Core.Domain;
using NeoServiceLayer.Core.Domain.ValueObjects;
using NeoServiceLayer.Core.Domain.Policies;
using NeoServiceLayer.Core.Domain.Events;
using Xunit;

namespace NeoServiceLayer.Architecture.Tests
{
    /// <summary>
    /// Tests to verify our core domain architectural improvements work correctly
    /// </summary>
    public class CoreDomainTests
    {
        [Fact]
        public void PasswordPolicy_ShouldValidatePassword_Successfully()
        {
            // Arrange
            var policy = new EnterprisePasswordPolicy();
            var validPassword = "SecurePassword123!";
            var invalidPassword = "weak";

            // Act
            var validResult = policy.ValidatePassword(validPassword);
            var invalidResult = policy.ValidatePassword(invalidPassword);

            // Assert
            Assert.True(validResult.IsValid, "Valid password should pass validation");
            Assert.False(invalidResult.IsValid, "Invalid password should fail validation");
            Assert.NotEmpty(invalidResult.Errors);
            Assert.True(invalidResult.Errors.Count > 0, "Invalid password should have error messages");
        }

        [Fact]
        public void Username_ShouldCreateAndValidate_Successfully()
        {
            // Arrange
            var validName = "testuser";

            // Act
            var username = Username.Create(validName);

            // Assert
            Assert.NotNull(username);
            Assert.Equal(validName, username.Value);
        }

        [Fact]
        public void EmailAddress_ShouldCreateAndValidate_Successfully()
        {
            // Arrange
            var validEmail = "test@example.com";

            // Act
            var email = EmailAddress.Create(validEmail);

            // Assert
            Assert.NotNull(email);
            Assert.Equal(validEmail, email.Value);
        }

        [Fact]
        public void Password_ShouldCreateAndVerify_Successfully()
        {
            // Arrange
            var plainPassword = "SecurePassword123!";

            // Act
            var password = Password.Create(plainPassword);
            var isValid = password.Verify(plainPassword);
            var isInvalid = password.Verify("wrongpassword");

            // Assert
            Assert.NotNull(password);
            Assert.True(isValid, "Correct password should verify successfully");
            Assert.False(isInvalid, "Wrong password should fail verification");
        }

        [Fact]
        public void Role_ShouldCreateWithProperties_Successfully()
        {
            // Arrange
            var roleName = "Admin";
            var roleDescription = "Administrator role";

            // Act
            var role = Role.Create(roleName, roleDescription);

            // Assert
            Assert.NotNull(role);
            Assert.Equal(roleName, role.Name);
            Assert.Equal(roleDescription, role.Description);
        }

        [Fact]
        public void User_ShouldCreateWithDomainEvents_Successfully()
        {
            // Arrange
            var userId = UserId.Create(Guid.NewGuid());
            var username = Username.Create("testuser");
            var email = EmailAddress.Create("test@example.com");
            var password = Password.Create("SecurePassword123!");

            // Act
            var user = User.Create(userId, username, email, password);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(userId, user.Id);
            Assert.Equal(username, user.Username);
            Assert.Equal(email, user.Email);
            Assert.Equal(password, user.Password);
            Assert.False(user.IsLocked);
            
            // Verify domain events are created
            Assert.True(user.DomainEvents.Count > 0, "User creation should generate domain events");
            Assert.Contains(user.DomainEvents, e => e is UserCreatedEvent);
        }

        [Fact]
        public void ValueObjects_ShouldBeImmutableAndEqualityWork_Successfully()
        {
            // Arrange & Act
            var username1 = Username.Create("testuser");
            var username2 = Username.Create("testuser");
            var username3 = Username.Create("differentuser");

            var email1 = EmailAddress.Create("test@example.com");
            var email2 = EmailAddress.Create("test@example.com");
            var email3 = EmailAddress.Create("other@example.com");

            // Assert - Value objects should have proper equality
            Assert.Equal(username1, username2);
            Assert.NotEqual(username1, username3);
            
            Assert.Equal(email1, email2);
            Assert.NotEqual(email1, email3);
            
            Assert.Equal(username1.GetHashCode(), username2.GetHashCode());
            Assert.Equal(email1.GetHashCode(), email2.GetHashCode());
        }

        [Fact]
        public void User_Authentication_ShouldWorkWithBusinessLogic_Successfully()
        {
            // Arrange
            var policy = new EnterprisePasswordPolicy();
            var userId = UserId.Create(Guid.NewGuid());
            var username = Username.Create("testuser");
            var email = EmailAddress.Create("test@example.com");
            var password = Password.Create("SecurePassword123!");
            var user = User.Create(userId, username, email, password);

            var correctPassword = "SecurePassword123!";
            var incorrectPassword = "wrongpassword";

            // Act
            var successResult = user.Authenticate(correctPassword, policy);
            var failResult = user.Authenticate(incorrectPassword, policy);

            // Assert
            Assert.True(successResult.IsSuccess, "Correct password should authenticate");
            Assert.False(failResult.IsSuccess, "Incorrect password should fail authentication");
        }

        [Fact]
        public void Entity_Equality_ShouldWorkCorrectly()
        {
            // Arrange
            var userId1 = UserId.Create(Guid.NewGuid());
            var userId2 = UserId.Create(userId1.Value); // Same ID
            var userId3 = UserId.Create(Guid.NewGuid()); // Different ID

            var username = Username.Create("testuser");
            var email = EmailAddress.Create("test@example.com");
            var password = Password.Create("SecurePassword123!");

            var user1 = User.Create(userId1, username, email, password);
            var user2 = User.Create(userId2, username, email, password);
            var user3 = User.Create(userId3, username, email, password);

            // Act & Assert - Entities with same ID should be equal
            Assert.Equal(user1, user2);
            Assert.NotEqual(user1, user3);
            Assert.Equal(user1.GetHashCode(), user2.GetHashCode());
        }
    }
}
using System;
using NeoServiceLayer.Core.Domain;
using Xunit;

namespace NeoServiceLayer.Core.Tests.Domain.ValueObjects
{
    /// <summary>
    /// Unit tests for the Password value object
    /// </summary>
    public class PasswordTests
    {
        [Fact]
        public void Create_WithValidPassword_ShouldSucceed()
        {
            // Arrange
            var plainPassword = "SecurePassword123!";

            // Act
            var password = Password.Create(plainPassword);

            // Assert
            Assert.NotNull(password);
            Assert.NotEqual(plainPassword, password.HashedValue); // Should be hashed
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_WithInvalidPassword_ShouldThrowDomainValidationException(string invalidPassword)
        {
            // Act & Assert
            Assert.Throws<DomainValidationException>(() => Password.Create(invalidPassword));
        }

        [Fact]
        public void Verify_WithCorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            var plainPassword = "SecurePassword123!";
            var password = Password.Create(plainPassword);

            // Act
            var isValid = password.Verify(plainPassword);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void Verify_WithIncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            var plainPassword = "SecurePassword123!";
            var wrongPassword = "WrongPassword";
            var password = Password.Create(plainPassword);

            // Act
            var isValid = password.Verify(wrongPassword);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void Create_SamePasswordTwice_ShouldProduceDifferentHashes()
        {
            // Arrange
            var plainPassword = "SecurePassword123!";

            // Act
            var password1 = Password.Create(plainPassword);
            var password2 = Password.Create(plainPassword);

            // Assert
            Assert.NotEqual(password1.HashedValue, password2.HashedValue); // Salts should be different
            Assert.True(password1.Verify(plainPassword));
            Assert.True(password2.Verify(plainPassword));
        }

        [Fact]
        public void Equals_WithSameHashedValue_ShouldReturnTrue()
        {
            // Arrange
            var plainPassword = "SecurePassword123!";
            var password1 = Password.Create(plainPassword);
            var password2 = Password.FromHash(password1.HashedValue);

            // Act & Assert
            Assert.Equal(password1, password2);
        }

        [Fact]
        public void Equals_WithDifferentHashedValue_ShouldReturnFalse()
        {
            // Arrange
            var password1 = Password.Create("Password1");
            var password2 = Password.Create("Password2");

            // Act & Assert
            Assert.NotEqual(password1, password2);
        }

        [Fact]
        public void ToString_ShouldReturnMaskedValue()
        {
            // Arrange
            var password = Password.Create("SecurePassword123!");

            // Act
            var stringValue = password.ToString();

            // Assert
            Assert.Equal("[PROTECTED]", stringValue);
        }
    }
}
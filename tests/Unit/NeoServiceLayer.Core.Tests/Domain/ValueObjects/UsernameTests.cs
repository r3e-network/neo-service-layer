using System;
using NeoServiceLayer.Core.Domain;
using Xunit;

namespace NeoServiceLayer.Core.Tests.Domain.ValueObjects
{
    /// <summary>
    /// Unit tests for the Username value object
    /// </summary>
    public class UsernameTests
    {
        [Fact]
        public void Create_WithValidUsername_ShouldSucceed()
        {
            // Arrange
            var validUsername = "testuser";

            // Act
            var username = Username.Create(validUsername);

            // Assert
            Assert.NotNull(username);
            Assert.Equal(validUsername, username.Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_WithInvalidUsername_ShouldThrowDomainValidationException(string invalidUsername)
        {
            // Act & Assert
            Assert.Throws<DomainValidationException>(() => Username.Create(invalidUsername));
        }

        [Fact]
        public void Create_WithTooLongUsername_ShouldThrowDomainValidationException()
        {
            // Arrange
            var tooLongUsername = new string('a', 101); // 101 characters

            // Act & Assert
            Assert.Throws<DomainValidationException>(() => Username.Create(tooLongUsername));
        }

        [Fact]
        public void Equals_WithSameValue_ShouldReturnTrue()
        {
            // Arrange
            var username1 = Username.Create("testuser");
            var username2 = Username.Create("testuser");

            // Act & Assert
            Assert.Equal(username1, username2);
        }

        [Fact]
        public void Equals_WithDifferentValue_ShouldReturnFalse()
        {
            // Arrange
            var username1 = Username.Create("testuser1");
            var username2 = Username.Create("testuser2");

            // Act & Assert
            Assert.NotEqual(username1, username2);
        }

        [Fact]
        public void ToString_ShouldReturnValue()
        {
            // Arrange
            var usernameValue = "testuser";
            var username = Username.Create(usernameValue);

            // Act
            var stringValue = username.ToString();

            // Assert
            Assert.Equal(usernameValue, stringValue);
        }
    }
}
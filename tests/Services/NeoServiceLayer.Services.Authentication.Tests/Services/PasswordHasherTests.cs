using FluentAssertions;
using NeoServiceLayer.Services.Authentication.Services;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Authentication.Tests.Services
{
    public class PasswordHasherTests
    {
        private readonly IPasswordHasher _passwordHasher;

        public PasswordHasherTests()
        {
            _passwordHasher = new PasswordHasher();
        }

        [Fact]
        public void HashPassword_Should_GenerateDifferentHashesForSamePassword()
        {
            // Arrange
            var password = "MySecurePassword123!";

            // Act
            var hash1 = _passwordHasher.HashPassword(password);
            var hash2 = _passwordHasher.HashPassword(password);

            // Assert
            hash1.Should().NotBeNullOrEmpty();
            hash2.Should().NotBeNullOrEmpty();
            hash1.Should().NotBe(hash2); // Different salts should produce different hashes
        }

        [Fact]
        public void VerifyPassword_WithCorrectPassword_Should_ReturnTrue()
        {
            // Arrange
            var password = "MySecurePassword123!";
            var hash = _passwordHasher.HashPassword(password);

            // Act
            var result = _passwordHasher.VerifyPassword(password, hash);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_WithIncorrectPassword_Should_ReturnFalse()
        {
            // Arrange
            var password = "MySecurePassword123!";
            var wrongPassword = "WrongPassword456!";
            var hash = _passwordHasher.HashPassword(password);

            // Act
            var result = _passwordHasher.VerifyPassword(wrongPassword, hash);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyPassword_WithSlightlyDifferentPassword_Should_ReturnFalse()
        {
            // Arrange
            var password = "MySecurePassword123!";
            var wrongPassword = "MySecurePassword123"; // Missing '!'
            var hash = _passwordHasher.HashPassword(password);

            // Act
            var result = _passwordHasher.VerifyPassword(wrongPassword, hash);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("")]
        [InlineData("a")]
        [InlineData("short")]
        [InlineData("ThisIsAVeryLongPasswordThatShouldStillWorkCorrectly123!@#")]
        public void HashAndVerify_Should_WorkWithVariousPasswordLengths(string password)
        {
            // Act
            var hash = _passwordHasher.HashPassword(password);
            var result = _passwordHasher.VerifyPassword(password, hash);

            // Assert
            hash.Should().NotBeNullOrEmpty();
            result.Should().BeTrue();
        }

        [Fact]
        public void HashPassword_Should_ProduceConsistentLengthHashes()
        {
            // Arrange
            var passwords = new[] { "short", "medium123", "ThisIsAVeryLongPassword123!@#" };

            // Act
            var hashes = passwords.Select(p => _passwordHasher.HashPassword(p)).ToList();

            // Assert
            var firstLength = hashes[0].Length;
            hashes.Should().AllSatisfy(h => h.Length.Should().Be(firstLength));
        }
    }
}
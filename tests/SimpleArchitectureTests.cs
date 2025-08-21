using System;
using NeoServiceLayer.Core.Domain;
using Xunit;

/// <summary>
/// Simple tests to verify our architectural improvements work
/// </summary>
public class SimpleArchitectureTests
{
    [Fact]
    public void PasswordPolicy_ShouldValidatePassword()
    {
        // Arrange
        var policy = new EnterprisePasswordPolicy();
        var validPassword = "SecurePassword123!";
        var invalidPassword = "weak";

        // Act
        var validResult = policy.ValidatePassword(validPassword);
        var invalidResult = policy.ValidatePassword(invalidPassword);

        // Assert
        Assert.True(validResult.IsValid);
        Assert.False(invalidResult.IsValid);
        Assert.NotEmpty(invalidResult.Errors);
    }

    [Fact]
    public void Username_ShouldCreateAndValidate()
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
    public void EmailAddress_ShouldCreateAndValidate()
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
    public void Password_ShouldCreateAndVerify()
    {
        // Arrange
        var plainPassword = "SecurePassword123!";

        // Act
        var password = Password.Create(plainPassword);
        var isValid = password.Verify(plainPassword);
        var isInvalid = password.Verify("wrongpassword");

        // Assert
        Assert.NotNull(password);
        Assert.True(isValid);
        Assert.False(isInvalid);
    }

    [Fact]
    public void Role_ShouldCreateWithProperties()
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
}
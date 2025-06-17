using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tests.Infrastructure;

/// <summary>
/// Base class for all tests providing common testing utilities.
/// </summary>
public abstract class TestBase
{
    protected readonly ITestOutputHelper Output;
    protected readonly Mock<ILogger> LoggerMock;

    protected TestBase(ITestOutputHelper output)
    {
        Output = output;
        LoggerMock = new Mock<ILogger>();
    }

    /// <summary>
    /// Creates a test user with specified roles.
    /// </summary>
    protected ClaimsPrincipal CreateTestUser(string userId = "test-user", params string[] roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, $"Test User {userId}")
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    /// <summary>
    /// Creates test data with specified properties.
    /// </summary>
    protected T CreateTestData<T>() where T : new()
    {
        return new T();
    }

    /// <summary>
    /// Logs test output for debugging.
    /// </summary>
    protected void LogTestOutput(string message, params object[] args)
    {
        Output.WriteLine(string.Format(message, args));
    }
} 
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Compliance.Models;
using ComplianceResponseModels = NeoServiceLayer.Services.Compliance.Models;
using NeoServiceLayer.Tee.Host.Services;
using System.Text.Json;
using Xunit;

namespace NeoServiceLayer.Services.Compliance.Tests;

public class ComplianceServiceTests
{
    private readonly Mock<IEnclaveManager> _enclaveManagerMock;
    private readonly Mock<IServiceConfiguration> _configurationMock;
    private readonly Mock<ILogger<ComplianceService>> _loggerMock;
    private readonly ComplianceService _service;

    public ComplianceServiceTests()
    {
        _enclaveManagerMock = new Mock<IEnclaveManager>();
        _configurationMock = new Mock<IServiceConfiguration>();
        _loggerMock = new Mock<ILogger<ComplianceService>>();

        _configurationMock
            .Setup(c => c.GetValue(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string key, string defaultValue) => defaultValue);

        _enclaveManagerMock
            .Setup(e => e.InitializeEnclaveAsync())
            .ReturnsAsync(true);
        _enclaveManagerMock
            .Setup(e => e.InitializeAsync(null, default))
            .Returns(Task.CompletedTask);

        _enclaveManagerMock
            .Setup(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string script, CancellationToken token) =>
            {
                if (script.Contains("verifyTransaction"))
                {
                    return JsonSerializer.Serialize(new VerificationResult
                    {
                        Passed = true,
                        RiskScore = 0,
                        Violations = new List<RuleViolation>()
                    });
                }
                else if (script.Contains("verifyAddress"))
                {
                    return JsonSerializer.Serialize(new VerificationResult
                    {
                        Passed = true,
                        RiskScore = 0,
                        Violations = new List<RuleViolation>()
                    });
                }
                else if (script.Contains("verifyContract"))
                {
                    return JsonSerializer.Serialize(new VerificationResult
                    {
                        Passed = true,
                        RiskScore = 0,
                        Violations = new List<RuleViolation>()
                    });
                }
                else if (script.Contains("getComplianceRules"))
                {
                    return JsonSerializer.Serialize(new List<ComplianceRule>
                    {
                        new ComplianceRule
                        {
                            RuleId = "rule-1",
                            RuleName = "Blacklist Check",
                            RuleDescription = "Check if an address is on the blacklist",
                            RuleType = "Address",
                            Parameters = new Dictionary<string, string>
                            {
                                { "blacklist", "address1,address2,address3" }
                            },
                            Severity = 100,
                            Enabled = true,
                            CreatedAt = DateTime.UtcNow,
                            LastModifiedAt = DateTime.UtcNow
                        }
                    });
                }
                else if (script.Contains("addComplianceRule"))
                {
                    return "true";
                }
                else if (script.Contains("removeComplianceRule"))
                {
                    return "true";
                }
                else if (script.Contains("updateComplianceRule"))
                {
                    return "true";
                }

                return string.Empty;
            });

        _service = new ComplianceService(_enclaveManagerMock.Object, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_ShouldInitializeEnclave()
    {
        // Act
        await _service.InitializeAsync();

        // Assert
        _enclaveManagerMock.Verify(e => e.InitializeAsync(null, default), Times.Once);
        Assert.True(_service.IsEnclaveInitialized);
    }

    [Fact]
    public async Task StartAsync_ShouldStartService()
    {
        // Arrange
        await _service.InitializeAsync();

        // Act
        await _service.StartAsync();

        // Assert
        Assert.True(_service.IsRunning);
    }

    [Fact]
    public async Task StopAsync_ShouldStopService()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        Assert.False(_service.IsRunning);
    }

    [Fact]
    public async Task VerifyTransactionAsync_ShouldVerifyTransaction()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.VerifyTransactionAsync(
            "transaction-data",
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result.Passed);
        Assert.Equal(0, result.RiskScore);
        Assert.Empty(result.Violations);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyAddressAsync_ShouldVerifyAddress()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.VerifyAddressAsync(
            "address",
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result.Passed);
        Assert.Equal(0, result.RiskScore);
        Assert.Empty(result.Violations);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyContractAsync_ShouldVerifyContract()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.VerifyContractAsync(
            "contract-data",
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result.Passed);
        Assert.Equal(0, result.RiskScore);
        Assert.Empty(result.Violations);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetComplianceRulesAsync_ShouldGetComplianceRules()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();

        // Act
        var result = await _service.GetComplianceRulesAsync(
            BlockchainType.NeoN3);

        // Assert
        Assert.Single(result);
        Assert.Equal("rule-1", result.First().RuleId);
        Assert.Equal("Blacklist Check", result.First().RuleName);
        Assert.Equal("Check if an address is on the blacklist", result.First().RuleDescription);
        Assert.Equal("Address", result.First().RuleType);
        Assert.Equal(100, result.First().Severity);
        Assert.True(result.First().Enabled);
        Assert.Equal("address1,address2,address3", result.First().Parameters["blacklist"]);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddComplianceRuleAsync_ShouldAddComplianceRule()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var rule = new ComplianceRule
        {
            RuleId = "rule-2",
            RuleName = "Whitelist Check",
            RuleDescription = "Check if an address is on the whitelist",
            RuleType = "Address",
            Parameters = new Dictionary<string, string>
            {
                { "whitelist", "address4,address5,address6" }
            },
            Severity = 50,
            Enabled = true
        };

        // Act
        var result = await _service.AddComplianceRuleAsync(
            rule,
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveComplianceRuleAsync_ShouldRemoveComplianceRule()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var rule = new ComplianceRule
        {
            RuleId = "rule-1",
            RuleName = "Blacklist Check",
            RuleDescription = "Check if an address is on the blacklist",
            RuleType = "Address",
            Parameters = new Dictionary<string, string>
            {
                { "blacklist", "address1,address2,address3" }
            },
            Severity = 100,
            Enabled = true
        };
        await _service.AddComplianceRuleAsync(rule, BlockchainType.NeoN3);

        // Act
        var result = await _service.RemoveComplianceRuleAsync(
            "rule-1",
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateComplianceRuleAsync_ShouldUpdateComplianceRule()
    {
        // Arrange
        await _service.InitializeAsync();
        await _service.StartAsync();
        var rule = new ComplianceRule
        {
            RuleId = "rule-1",
            RuleName = "Blacklist Check",
            RuleDescription = "Check if an address is on the blacklist",
            RuleType = "Address",
            Parameters = new Dictionary<string, string>
            {
                { "blacklist", "address1,address2,address3" }
            },
            Severity = 100,
            Enabled = true
        };
        await _service.AddComplianceRuleAsync(rule, BlockchainType.NeoN3);

        rule.RuleName = "Updated Blacklist Check";
        rule.Severity = 75;

        // Act
        var result = await _service.UpdateComplianceRuleAsync(
            rule,
            BlockchainType.NeoN3);

        // Assert
        Assert.True(result);
        _enclaveManagerMock.Verify(e => e.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}

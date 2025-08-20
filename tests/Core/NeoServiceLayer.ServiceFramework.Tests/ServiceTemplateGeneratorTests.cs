using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Xunit;


namespace NeoServiceLayer.ServiceFramework.Tests;

/// <summary>
/// Tests for the ServiceTemplateGenerator class.
/// </summary>
public class ServiceTemplateGeneratorTests
{
    private readonly ServiceTemplateGenerator _templateGenerator;

    public ServiceTemplateGeneratorTests()
    {
        _templateGenerator = new ServiceTemplateGenerator();
    }

    [Fact]
    public void GenerateServiceImplementation_ShouldReturnValidTemplate_ForBasicService()
    {
        // Arrange
        string serviceName = "Test";
        string namespaceName = "NeoServiceLayer.Services";
        bool includeEnclave = false;
        bool includeBlockchain = false;

        // Act
        string template = _templateGenerator.GenerateServiceImplementation(
            serviceName,
            namespaceName,
            includeEnclave,
            includeBlockchain);

        // Assert
        Assert.NotNull(template);
        Assert.NotEmpty(template);
        Assert.Contains($"public class {serviceName}Service : ServiceBase, I{serviceName}Service", template);
        Assert.Contains($"public {serviceName}Service(", template);
        Assert.Contains($"ILogger<{serviceName}Service> logger", template);
        Assert.Contains($": base(\"{serviceName}\", \"{serviceName} Service\", \"1.0.0\", logger)", template);
        Assert.Contains("protected override async Task<bool> OnInitializeAsync()", template);
        Assert.Contains("protected override async Task<bool> OnStartAsync()", template);
        Assert.Contains("protected override async Task<bool> OnStopAsync()", template);
        Assert.Contains("protected override async Task<ServiceHealth> OnGetHealthAsync()", template);
        Assert.Contains("protected override async Task OnUpdateMetricsAsync()", template);
    }

    [Fact]
    public void GenerateServiceImplementation_ShouldReturnValidTemplate_ForEnclaveService()
    {
        // Arrange
        string serviceName = "TestEnclave";
        string namespaceName = "NeoServiceLayer.Services";
        bool includeEnclave = true;
        bool includeBlockchain = false;

        // Act
        string template = _templateGenerator.GenerateServiceImplementation(
            serviceName,
            namespaceName,
            includeEnclave,
            includeBlockchain);

        // Assert
        Assert.NotNull(template);
        Assert.NotEmpty(template);
        Assert.Contains($"public class {serviceName}Service : EnclaveServiceBase, I{serviceName}Service", template);
        Assert.Contains($"public {serviceName}Service(", template);
        Assert.Contains("IEnclaveManager enclaveManager", template);
        Assert.Contains($"ILogger<{serviceName}Service> logger", template);
        Assert.Contains($": base(\"{serviceName}\", \"{serviceName} Service\", \"1.0.0\", logger)", template);
        Assert.Contains("protected override async Task<bool> OnInitializeAsync()", template);
        Assert.Contains("protected override async Task<bool> OnInitializeEnclaveAsync()", template);
        Assert.Contains("protected override async Task<bool> OnStartAsync()", template);
        Assert.Contains("protected override async Task<bool> OnStopAsync()", template);
        Assert.Contains("protected override async Task<ServiceHealth> OnGetHealthAsync()", template);
        Assert.Contains("protected override async Task OnUpdateMetricsAsync()", template);
    }

    [Fact]
    public void GenerateServiceImplementation_ShouldReturnValidTemplate_ForBlockchainService()
    {
        // Arrange
        string serviceName = "TestBlockchain";
        string namespaceName = "NeoServiceLayer.Services";
        bool includeEnclave = false;
        bool includeBlockchain = true;

        // Act
        string template = _templateGenerator.GenerateServiceImplementation(
            serviceName,
            namespaceName,
            includeEnclave,
            includeBlockchain);

        // Assert
        Assert.NotNull(template);
        Assert.NotEmpty(template);
        Assert.Contains($"public class {serviceName}Service : BlockchainServiceBase, I{serviceName}Service", template);
        Assert.Contains($"public {serviceName}Service(", template);
        Assert.Contains("IBlockchainClientFactory blockchainClientFactory", template);
        Assert.Contains($"ILogger<{serviceName}Service> logger", template);
        Assert.Contains($": base(\"{serviceName}\", \"{serviceName} Service\", \"1.0.0\", logger, new[] {{ BlockchainType.NeoN3, BlockchainType.NeoX }})", template);
        Assert.Contains("protected override async Task<bool> OnInitializeAsync()", template);
        Assert.Contains("protected override async Task<bool> OnStartAsync()", template);
        Assert.Contains("protected override async Task<bool> OnStopAsync()", template);
        Assert.Contains("protected override async Task<ServiceHealth> OnGetHealthAsync()", template);
        Assert.Contains("protected override async Task OnUpdateMetricsAsync()", template);
        Assert.Contains("public async Task<string> DoSomethingWithBlockchainAsync(string input, BlockchainType blockchainType)", template);
    }

    [Fact]
    public void GenerateServiceImplementation_ShouldReturnValidTemplate_ForEnclaveBlockchainService()
    {
        // Arrange
        string serviceName = "TestEnclaveBlockchain";
        string namespaceName = "NeoServiceLayer.Services";
        bool includeEnclave = true;
        bool includeBlockchain = true;

        // Act
        string template = _templateGenerator.GenerateServiceImplementation(
            serviceName,
            namespaceName,
            includeEnclave,
            includeBlockchain);

        // Assert
        Assert.NotNull(template);
        Assert.NotEmpty(template);
        Assert.Contains($"public class {serviceName}Service : EnclaveBlockchainServiceBase, I{serviceName}Service", template);
        Assert.Contains($"public {serviceName}Service(", template);
        Assert.Contains("IEnclaveManager enclaveManager", template);
        Assert.Contains("IBlockchainClientFactory blockchainClientFactory", template);
        Assert.Contains($"ILogger<{serviceName}Service> logger", template);
        Assert.Contains($": base(\"{serviceName}\", \"{serviceName} Service\", \"1.0.0\", logger, new[] {{ BlockchainType.NeoN3, BlockchainType.NeoX }})", template);
        Assert.Contains("protected override async Task<bool> OnInitializeAsync()", template);
        Assert.Contains("protected override async Task<bool> OnInitializeEnclaveAsync()", template);
        Assert.Contains("protected override async Task<bool> OnStartAsync()", template);
        Assert.Contains("protected override async Task<bool> OnStopAsync()", template);
        Assert.Contains("protected override async Task<ServiceHealth> OnGetHealthAsync()", template);
        Assert.Contains("protected override async Task OnUpdateMetricsAsync()", template);
        Assert.Contains("public async Task<string> DoSomethingWithBlockchainAsync(string input, BlockchainType blockchainType)", template);
    }

    [Fact]
    public void GenerateServiceImplementation_ShouldIncludeUsings()
    {
        // Arrange
        string serviceName = "Test";
        string namespaceName = "NeoServiceLayer.Services";
        bool includeEnclave = false;
        bool includeBlockchain = false;

        // Act
        string template = _templateGenerator.GenerateServiceImplementation(
            serviceName,
            namespaceName,
            includeEnclave,
            includeBlockchain);

        // Assert
        Assert.Contains("using Microsoft.Extensions.Logging;", template);
        Assert.Contains("using NeoServiceLayer.Core;", template);
        Assert.Contains("using NeoServiceLayer.ServiceFramework;", template);
    }

    [Fact]
    public void GenerateServiceImplementation_ShouldIncludeNamespace()
    {
        // Arrange
        string serviceName = "Test";
        string namespaceName = "NeoServiceLayer.Services";
        bool includeEnclave = false;
        bool includeBlockchain = false;

        // Act
        string template = _templateGenerator.GenerateServiceImplementation(
            serviceName,
            namespaceName,
            includeEnclave,
            includeBlockchain);

        // Assert
        Assert.Contains($"namespace {namespaceName};", template);
    }

    [Fact]
    public void GenerateServiceInterface_ShouldReturnValidTemplate()
    {
        // Arrange
        string serviceName = "Test";
        string namespaceName = "NeoServiceLayer.Services";
        bool includeEnclave = false;
        bool includeBlockchain = false;

        // Act
        string template = _templateGenerator.GenerateServiceInterface(
            serviceName,
            namespaceName,
            includeEnclave,
            includeBlockchain);

        // Assert
        Assert.NotNull(template);
        Assert.NotEmpty(template);
        Assert.Contains($"public interface I{serviceName}Service : IService", template);
        Assert.Contains("Task<string> DoSomethingAsync(string input);", template);
    }

    [Fact]
    public void GenerateServiceTest_ShouldReturnValidTemplate()
    {
        // Arrange
        string serviceName = "Test";
        string namespaceName = "NeoServiceLayer.Services";
        bool includeEnclave = false;
        bool includeBlockchain = false;

        // Act
        string template = _templateGenerator.GenerateServiceTest(
            serviceName,
            namespaceName,
            includeEnclave,
            includeBlockchain);

        // Assert
        Assert.NotNull(template);
        Assert.NotEmpty(template);
        Assert.Contains($"public class {serviceName}ServiceTests", template);
        Assert.Contains($"private readonly {serviceName}Service _service;", template);
        Assert.Contains("[Fact]", template);
        Assert.Contains("public async Task InitializeAsync_ShouldReturnTrue()", template);
    }

    [Fact]
    public void GenerateServiceDocumentation_ShouldReturnValidTemplate()
    {
        // Arrange
        string serviceName = "Test";

        // Act
        string template = _templateGenerator.GenerateServiceDocumentation(serviceName);

        // Assert
        Assert.NotNull(template);
        Assert.NotEmpty(template);
        Assert.Contains($"# Neo Service Layer - {serviceName} Service", template);
        Assert.Contains("## Overview", template);
        Assert.Contains("## API Reference", template);
    }
}

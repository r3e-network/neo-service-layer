using System.Text;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Interface for service template generators.
/// </summary>
public interface IServiceTemplateGenerator
{
    /// <summary>
    /// Generates a service interface template.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="includeEnclave">Whether to include enclave support.</param>
    /// <param name="includeBlockchain">Whether to include blockchain support.</param>
    /// <returns>The service interface template.</returns>
    string GenerateServiceInterface(string serviceName, string @namespace, bool includeEnclave = false, bool includeBlockchain = false);

    /// <summary>
    /// Generates a service implementation template.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="includeEnclave">Whether to include enclave support.</param>
    /// <param name="includeBlockchain">Whether to include blockchain support.</param>
    /// <returns>The service implementation template.</returns>
    string GenerateServiceImplementation(string serviceName, string @namespace, bool includeEnclave = false, bool includeBlockchain = false);

    /// <summary>
    /// Generates a service test template.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="includeEnclave">Whether to include enclave support.</param>
    /// <param name="includeBlockchain">Whether to include blockchain support.</param>
    /// <returns>The service test template.</returns>
    string GenerateServiceTest(string serviceName, string @namespace, bool includeEnclave = false, bool includeBlockchain = false);

    /// <summary>
    /// Generates a service documentation template.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The service documentation template.</returns>
    string GenerateServiceDocumentation(string serviceName);
}

/// <summary>
/// Implementation of the service template generator.
/// </summary>
public class ServiceTemplateGenerator : IServiceTemplateGenerator
{
    /// <inheritdoc/>
    public string GenerateServiceInterface(string serviceName, string @namespace, bool includeEnclave = false, bool includeBlockchain = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using NeoServiceLayer.Core;");
        sb.AppendLine();
        sb.AppendLine($"namespace {@namespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Interface for the {serviceName} service.");
        sb.AppendLine("/// </summary>");

        var interfaces = new List<string> { "IService" };
        if (includeEnclave)
        {
            interfaces.Add("IEnclaveService");
        }
        if (includeBlockchain)
        {
            interfaces.Add("IBlockchainService");
        }

        sb.AppendLine($"public interface I{serviceName}Service : {string.Join(", ", interfaces)}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Example method for the {serviceName} service.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"input\">The input parameter.</param>");
        sb.AppendLine("    /// <returns>The result of the operation.</returns>");
        sb.AppendLine("    Task<string> DoSomethingAsync(string input);");

        if (includeBlockchain)
        {
            sb.AppendLine();
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Example method for the {serviceName} service with blockchain support.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <param name=\"input\">The input parameter.</param>");
            sb.AppendLine("    /// <param name=\"blockchainType\">The blockchain type.</param>");
            sb.AppendLine("    /// <returns>The result of the operation.</returns>");
            sb.AppendLine("    Task<string> DoSomethingWithBlockchainAsync(string input, BlockchainType blockchainType);");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <inheritdoc/>
    public string GenerateServiceImplementation(string serviceName, string @namespace, bool includeEnclave = false, bool includeBlockchain = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine("using NeoServiceLayer.Core;");
        sb.AppendLine("using NeoServiceLayer.ServiceFramework;");

        if (includeEnclave)
        {
            sb.AppendLine("using NeoServiceLayer.Tee.Host.Services;");
        }

        if (includeBlockchain)
        {
            sb.AppendLine("using NeoServiceLayer.Infrastructure;");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {@namespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Implementation of the {serviceName} service.");
        sb.AppendLine("/// </summary>");

        string baseClass;
        if (includeEnclave && includeBlockchain)
        {
            baseClass = "EnclaveBlockchainServiceBase";
        }
        else if (includeEnclave)
        {
            baseClass = "EnclaveServiceBase";
        }
        else if (includeBlockchain)
        {
            baseClass = "BlockchainServiceBase";
        }
        else
        {
            baseClass = "ServiceBase";
        }

        sb.AppendLine($"public class {serviceName}Service : {baseClass}, I{serviceName}Service");
        sb.AppendLine("{");

        // Fields
        if (includeEnclave)
        {
            sb.AppendLine("    private readonly IEnclaveManager _enclaveManager;");
        }

        if (includeBlockchain)
        {
            sb.AppendLine("    private readonly IBlockchainClientFactory _blockchainClientFactory;");
        }

        sb.AppendLine("    private readonly IServiceConfiguration _configuration;");
        sb.AppendLine();

        // Constructor
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Initializes a new instance of the <see cref=\"{serviceName}Service\"/> class.");
        sb.AppendLine("    /// </summary>");

        if (includeEnclave)
        {
            sb.AppendLine("    /// <param name=\"enclaveManager\">The enclave manager.</param>");
        }

        if (includeBlockchain)
        {
            sb.AppendLine("    /// <param name=\"blockchainClientFactory\">The blockchain client factory.</param>");
        }

        sb.AppendLine("    /// <param name=\"configuration\">The service configuration.</param>");
        sb.AppendLine("    /// <param name=\"logger\">The logger.</param>");
        sb.Append($"    public {serviceName}Service(");

        var ctorParams = new List<string>();
        if (includeEnclave)
        {
            ctorParams.Add("IEnclaveManager enclaveManager");
        }

        if (includeBlockchain)
        {
            ctorParams.Add("IBlockchainClientFactory blockchainClientFactory");
        }

        ctorParams.Add("IServiceConfiguration configuration");
        ctorParams.Add($"ILogger<{serviceName}Service> logger");

        sb.Append(string.Join(", ", ctorParams));
        sb.AppendLine(")");

        if (includeEnclave && includeBlockchain)
        {
            sb.AppendLine($"        : base(\"{serviceName}\", \"{serviceName} Service\", \"1.0.0\", logger, new[] {{ BlockchainType.NeoN3, BlockchainType.NeoX }})");
        }
        else if (includeBlockchain)
        {
            sb.AppendLine($"        : base(\"{serviceName}\", \"{serviceName} Service\", \"1.0.0\", logger, new[] {{ BlockchainType.NeoN3, BlockchainType.NeoX }})");
        }
        else
        {
            sb.AppendLine($"        : base(\"{serviceName}\", \"{serviceName} Service\", \"1.0.0\", logger)");
        }

        sb.AppendLine("    {");

        if (includeEnclave)
        {
            sb.AppendLine("        _enclaveManager = enclaveManager;");
        }

        if (includeBlockchain)
        {
            sb.AppendLine("        _blockchainClientFactory = blockchainClientFactory;");
        }

        sb.AppendLine("        _configuration = configuration;");

        // Add capabilities
        sb.AppendLine();
        sb.AppendLine($"        // Add capabilities");
        sb.AppendLine($"        AddCapability<I{serviceName}Service>();");

        if (includeEnclave)
        {
            sb.AppendLine("        AddCapability<IEnclaveService>();");
        }

        if (includeBlockchain)
        {
            sb.AppendLine("        AddCapability<IBlockchainService>();");
        }

        // Add metadata
        sb.AppendLine();
        sb.AppendLine("        // Add metadata");
        sb.AppendLine("        SetMetadata(\"CreatedAt\", DateTime.UtcNow.ToString(\"o\"));");

        sb.AppendLine("    }");
        sb.AppendLine();

        // Methods
        sb.AppendLine("    /// <inheritdoc/>");
        sb.AppendLine("    public async Task<string> DoSomethingAsync(string input)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Implementation goes here");
        sb.AppendLine("        await Task.Delay(100); // Simulate some work");
        sb.AppendLine("        return $\"Processed: {input}\";");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (includeBlockchain)
        {
            sb.AppendLine("    /// <inheritdoc/>");
            sb.AppendLine("    public async Task<string> DoSomethingWithBlockchainAsync(string input, BlockchainType blockchainType)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (!SupportsBlockchain(blockchainType))");
            sb.AppendLine("        {");
            sb.AppendLine("            throw new NotSupportedException($\"Blockchain type {blockchainType} is not supported.\");");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        var client = _blockchainClientFactory.CreateClient(blockchainType);");
            sb.AppendLine("        var blockHeight = await client.GetBlockHeightAsync();");
            sb.AppendLine();
            sb.AppendLine("        // Implementation goes here");
            sb.AppendLine("        await Task.Delay(100); // Simulate some work");
            sb.AppendLine("        return $\"Processed: {input} on {blockchainType} at block {blockHeight}\";");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Override methods
        sb.AppendLine("    /// <inheritdoc/>");
        sb.AppendLine("    protected override async Task<bool> OnInitializeAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Initialize the service");
        sb.AppendLine("        await Task.Delay(100); // Simulate some work");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (includeEnclave)
        {
            sb.AppendLine("    /// <inheritdoc/>");
            sb.AppendLine("    protected override async Task<bool> OnInitializeEnclaveAsync()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Initialize the enclave");
            sb.AppendLine("        return await _enclaveManager.InitializeEnclaveAsync();");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("    /// <inheritdoc/>");
        sb.AppendLine("    protected override async Task<bool> OnStartAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Start the service");
        sb.AppendLine("        await Task.Delay(100); // Simulate some work");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    /// <inheritdoc/>");
        sb.AppendLine("    protected override async Task<bool> OnStopAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Stop the service");
        sb.AppendLine("        await Task.Delay(100); // Simulate some work");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    /// <inheritdoc/>");
        sb.AppendLine("    protected override async Task<ServiceHealth> OnGetHealthAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Check the health of the service");
        sb.AppendLine("        await Task.Delay(100); // Simulate some work");
        sb.AppendLine("        return ServiceHealth.Healthy;");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    /// <inheritdoc/>");
        sb.AppendLine("    protected override async Task OnUpdateMetricsAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Update service metrics");
        sb.AppendLine("        UpdateMetric(\"LastUpdated\", DateTime.UtcNow);");
        sb.AppendLine("        UpdateMetric(\"RequestCount\", 0); // Replace with actual metric");
        sb.AppendLine("        await Task.CompletedTask;");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <inheritdoc/>
    public string GenerateServiceTest(string serviceName, string @namespace, bool includeEnclave = false, bool includeBlockchain = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine("using Moq;");
        sb.AppendLine("using NeoServiceLayer.Core;");
        sb.AppendLine("using NeoServiceLayer.ServiceFramework;");

        if (includeEnclave)
        {
            sb.AppendLine("using NeoServiceLayer.Tee.Host.Services;");
        }

        if (includeBlockchain)
        {
            sb.AppendLine("using NeoServiceLayer.Infrastructure;");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {@namespace}.Tests;");
        sb.AppendLine();
        sb.AppendLine($"public class {serviceName}ServiceTests");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly Mock<ILogger<{serviceName}Service>> _loggerMock;");

        if (includeEnclave)
        {
            sb.AppendLine("    private readonly Mock<IEnclaveManager> _enclaveManagerMock;");
        }

        if (includeBlockchain)
        {
            sb.AppendLine("    private readonly Mock<IBlockchainClientFactory> _blockchainClientFactoryMock;");
            sb.AppendLine("    private readonly Mock<IBlockchainClient> _blockchainClientMock;");
        }

        sb.AppendLine("    private readonly Mock<IServiceConfiguration> _configurationMock;");
        sb.AppendLine($"    private readonly {serviceName}Service _service;");
        sb.AppendLine();
        sb.AppendLine($"    public {serviceName}ServiceTests()");
        sb.AppendLine("    {");
        sb.AppendLine($"        _loggerMock = new Mock<ILogger<{serviceName}Service>>();");

        if (includeEnclave)
        {
            sb.AppendLine("        _enclaveManagerMock = new Mock<IEnclaveManager>();");
            sb.AppendLine("        _enclaveManagerMock.Setup(e => e.InitializeEnclaveAsync()).ReturnsAsync(true);");
        }

        if (includeBlockchain)
        {
            sb.AppendLine("        _blockchainClientFactoryMock = new Mock<IBlockchainClientFactory>();");
            sb.AppendLine("        _blockchainClientMock = new Mock<IBlockchainClient>();");
            sb.AppendLine("        _blockchainClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<BlockchainType>())).Returns(_blockchainClientMock.Object);");
            sb.AppendLine("        _blockchainClientMock.Setup(c => c.GetBlockHeightAsync()).ReturnsAsync(1000L);");
        }

        sb.AppendLine("        _configurationMock = new Mock<IServiceConfiguration>();");
        sb.AppendLine();
        sb.Append($"        _service = new {serviceName}Service(");

        var ctorParams = new List<string>();
        if (includeEnclave)
        {
            ctorParams.Add("_enclaveManagerMock.Object");
        }

        if (includeBlockchain)
        {
            ctorParams.Add("_blockchainClientFactoryMock.Object");
        }

        ctorParams.Add("_configurationMock.Object");
        ctorParams.Add("_loggerMock.Object");

        sb.Append(string.Join(", ", ctorParams));
        sb.AppendLine(");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Fact]");
        sb.AppendLine("    public async Task InitializeAsync_ShouldReturnTrue()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Act");
        sb.AppendLine("        var result = await _service.InitializeAsync();");
        sb.AppendLine();
        sb.AppendLine("        // Assert");
        sb.AppendLine("        Assert.True(result);");

        if (includeEnclave)
        {
            sb.AppendLine("        Assert.True(_service.IsEnclaveInitialized);");
            sb.AppendLine("        _enclaveManagerMock.Verify(e => e.InitializeEnclaveAsync(), Times.Once);");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Fact]");
        sb.AppendLine("    public async Task StartAsync_ShouldReturnTrue()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Arrange");
        sb.AppendLine("        await _service.InitializeAsync();");
        sb.AppendLine();
        sb.AppendLine("        // Act");
        sb.AppendLine("        var result = await _service.StartAsync();");
        sb.AppendLine();
        sb.AppendLine("        // Assert");
        sb.AppendLine("        Assert.True(result);");
        sb.AppendLine("        Assert.True(_service.IsRunning);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Fact]");
        sb.AppendLine("    public async Task StopAsync_ShouldReturnTrue()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Arrange");
        sb.AppendLine("        await _service.InitializeAsync();");
        sb.AppendLine("        await _service.StartAsync();");
        sb.AppendLine();
        sb.AppendLine("        // Act");
        sb.AppendLine("        var result = await _service.StopAsync();");
        sb.AppendLine();
        sb.AppendLine("        // Assert");
        sb.AppendLine("        Assert.True(result);");
        sb.AppendLine("        Assert.False(_service.IsRunning);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Fact]");
        sb.AppendLine("    public async Task GetHealthAsync_ShouldReturnHealthy()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Arrange");
        sb.AppendLine("        await _service.InitializeAsync();");
        sb.AppendLine("        await _service.StartAsync();");
        sb.AppendLine();
        sb.AppendLine("        // Act");
        sb.AppendLine("        var result = await _service.GetHealthAsync();");
        sb.AppendLine();
        sb.AppendLine("        // Assert");
        sb.AppendLine("        Assert.Equal(ServiceHealth.Healthy, result);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Fact]");
        sb.AppendLine("    public async Task DoSomethingAsync_ShouldReturnProcessedInput()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Arrange");
        sb.AppendLine("        await _service.InitializeAsync();");
        sb.AppendLine("        await _service.StartAsync();");
        sb.AppendLine();
        sb.AppendLine("        // Act");
        sb.AppendLine("        var result = await _service.DoSomethingAsync(\"test\");");
        sb.AppendLine();
        sb.AppendLine("        // Assert");
        sb.AppendLine("        Assert.Equal(\"Processed: test\", result);");
        sb.AppendLine("    }");

        if (includeBlockchain)
        {
            sb.AppendLine();
            sb.AppendLine("    [Fact]");
            sb.AppendLine("    public async Task DoSomethingWithBlockchainAsync_ShouldReturnProcessedInput()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Arrange");
            sb.AppendLine("        await _service.InitializeAsync();");
            sb.AppendLine("        await _service.StartAsync();");
            sb.AppendLine();
            sb.AppendLine("        // Act");
            sb.AppendLine("        var result = await _service.DoSomethingWithBlockchainAsync(\"test\", BlockchainType.NeoN3);");
            sb.AppendLine();
            sb.AppendLine("        // Assert");
            sb.AppendLine("        Assert.Contains(\"Processed: test on NeoN3 at block 1000\", result);");
            sb.AppendLine("        _blockchainClientFactoryMock.Verify(f => f.CreateClient(BlockchainType.NeoN3), Times.Once);");
            sb.AppendLine("        _blockchainClientMock.Verify(c => c.GetBlockHeightAsync(), Times.Once);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    [Fact]");
            sb.AppendLine("    public async Task DoSomethingWithBlockchainAsync_ShouldThrowNotSupportedException_WhenBlockchainTypeIsNotSupported()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Arrange");
            sb.AppendLine("        await _service.InitializeAsync();");
            sb.AppendLine("        await _service.StartAsync();");
            sb.AppendLine();
            sb.AppendLine("        // Act & Assert");
            sb.AppendLine("        await Assert.ThrowsAsync<NotSupportedException>(() => _service.DoSomethingWithBlockchainAsync(\"test\", (BlockchainType)999));");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <inheritdoc/>
    public string GenerateServiceDocumentation(string serviceName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Neo Service Layer - {serviceName} Service");
        sb.AppendLine();
        sb.AppendLine("## Overview");
        sb.AppendLine();
        sb.AppendLine($"The {serviceName} Service provides [description of the service].");
        sb.AppendLine();
        sb.AppendLine("## Features");
        sb.AppendLine();
        sb.AppendLine("- Feature 1");
        sb.AppendLine("- Feature 2");
        sb.AppendLine("- Feature 3");
        sb.AppendLine();
        sb.AppendLine("## Architecture");
        sb.AppendLine();
        sb.AppendLine("The service consists of the following components:");
        sb.AppendLine();
        sb.AppendLine("### Service Layer");
        sb.AppendLine();
        sb.AppendLine($"- **I{serviceName}Service**: Interface defining the {serviceName} service operations.");
        sb.AppendLine($"- **{serviceName}Service**: Implementation of the {serviceName} service.");
        sb.AppendLine();
        sb.AppendLine("### Enclave Layer");
        sb.AppendLine();
        sb.AppendLine("- **Enclave Implementation**: C++ code running within Occlum LibOS enclaves.");
        sb.AppendLine("- **Secure Communication**: Encrypted communication between the service layer and the enclave.");
        sb.AppendLine();
        sb.AppendLine("### Blockchain Integration");
        sb.AppendLine();
        sb.AppendLine("- **Neo N3 Integration**: Integration with the Neo N3 blockchain.");
        sb.AppendLine("- **NeoX Integration**: Integration with the NeoX blockchain (EVM-compatible).");
        sb.AppendLine();
        sb.AppendLine("## API Reference");
        sb.AppendLine();
        sb.AppendLine($"### I{serviceName}Service Interface");
        sb.AppendLine();
        sb.AppendLine("```csharp");
        sb.AppendLine($"public interface I{serviceName}Service : IService");
        sb.AppendLine("{");
        sb.AppendLine("    Task<string> DoSomethingAsync(string input);");
        sb.AppendLine("}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("#### Methods");
        sb.AppendLine();
        sb.AppendLine("- **DoSomethingAsync**: [Description of the method]");
        sb.AppendLine("  - Parameters:");
        sb.AppendLine("    - `input`: [Description of the parameter]");
        sb.AppendLine("  - Returns: [Description of the return value]");
        sb.AppendLine();
        sb.AppendLine("## Usage Examples");
        sb.AppendLine();
        sb.AppendLine("```csharp");
        sb.AppendLine($"// Get the {serviceName} service");
        sb.AppendLine($"var {serviceName.ToLowerInvariant()}Service = serviceProvider.GetRequiredService<I{serviceName}Service>();");
        sb.AppendLine();
        sb.AppendLine("// Use the service");
        sb.AppendLine($"var result = await {serviceName.ToLowerInvariant()}Service.DoSomethingAsync(\"input\");");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Security Considerations");
        sb.AppendLine();
        sb.AppendLine("- [Security consideration 1]");
        sb.AppendLine("- [Security consideration 2]");
        sb.AppendLine("- [Security consideration 3]");
        sb.AppendLine();
        sb.AppendLine("## Conclusion");
        sb.AppendLine();
        sb.AppendLine($"The {serviceName} Service provides [summary of the service].");

        return sb.ToString();
    }
}

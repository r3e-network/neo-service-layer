using NeoServiceLayer.Core;
using NeoServiceLayer.Core.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Service template generator for creating new services.
/// </summary>
public static class ServiceTemplate
{
    /// <summary>
    /// Generates a basic service template.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="description">The service description.</param>
    /// <param name="supportedBlockchains">Supported blockchain types.</param>
    /// <param name="requiresEnclave">Whether the service requires enclave.</param>
    /// <returns>The generated service code.</returns>
    public static string GenerateService(
        string serviceName,
        string @namespace,
        string description,
        BlockchainType[]? supportedBlockchains = null,
        bool requiresEnclave = false)
    {
        var sb = new StringBuilder();
        var interfaceName = $"I{serviceName}Service";
        var className = $"{serviceName}Service";
        var baseClass = GetBaseClass(supportedBlockchains, requiresEnclave);
        var interfaces = GetInterfaces(supportedBlockchains, requiresEnclave);

        // Generate using statements
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine("using NeoServiceLayer.Core;");
        sb.AppendLine("using NeoServiceLayer.Core.Configuration;");
        sb.AppendLine("using NeoServiceLayer.ServiceFramework;");
        if (requiresEnclave)
        {
            sb.AppendLine("using NeoServiceLayer.Tee.Host.Services;");
        }
        sb.AppendLine();

        // Generate namespace
        sb.AppendLine($"namespace {@namespace};");
        sb.AppendLine();

        // Generate interface
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Interface for the {serviceName} Service.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public interface {interfaceName} : {string.Join(", ", interfaces)}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Performs {serviceName.ToLower()} operation.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"request\">The request.</param>");
        if (supportedBlockchains?.Length > 0)
        {
            sb.AppendLine("    /// <param name=\"blockchainType\">The blockchain type.</param>");
        }
        sb.AppendLine("    /// <returns>The operation result.</returns>");

        if (supportedBlockchains?.Length > 0)
        {
            sb.AppendLine($"    Task<{serviceName}Result> Process{serviceName}Async({serviceName}Request request, BlockchainType blockchainType);");
        }
        else
        {
            sb.AppendLine($"    Task<{serviceName}Result> Process{serviceName}Async({serviceName}Request request);");
        }

        sb.AppendLine("}");
        sb.AppendLine();

        // Generate implementation
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Implementation of the {serviceName} Service.");
        sb.AppendLine($"/// {description}");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public partial class {className} : {baseClass}, {interfaceName}");
        sb.AppendLine("{");

        // Generate constructor
        GenerateConstructor(sb, className, serviceName, description, supportedBlockchains, requiresEnclave);

        // Generate interface implementation
        GenerateInterfaceImplementation(sb, serviceName, supportedBlockchains);

        // Generate base class overrides
        GenerateBaseClassOverrides(sb, serviceName, supportedBlockchains, requiresEnclave);

        // Generate helper methods
        GenerateHelperMethods(sb, serviceName, supportedBlockchains, requiresEnclave);

        sb.AppendLine("}");
        sb.AppendLine();

        // Generate models
        GenerateModels(sb, serviceName, @namespace);

        return sb.ToString();
    }

    /// <summary>
    /// Generates a service registration extension.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="supportedBlockchains">Supported blockchain types.</param>
    /// <param name="requiresEnclave">Whether the service requires enclave.</param>
    /// <returns>The generated registration code.</returns>
    public static string GenerateServiceRegistration(
        string serviceName,
        string @namespace,
        BlockchainType[]? supportedBlockchains = null,
        bool requiresEnclave = false)
    {
        var sb = new StringBuilder();
        var interfaceName = $"I{serviceName}Service";
        var className = $"{serviceName}Service";

        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using NeoServiceLayer.Core;");
        sb.AppendLine("using NeoServiceLayer.Core.Configuration;");
        sb.AppendLine("using NeoServiceLayer.ServiceFramework;");
        sb.AppendLine();

        sb.AppendLine($"namespace {@namespace};");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Extension methods for {serviceName} Service registration.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static class {serviceName}ServiceExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Adds the {serviceName} Service to the service collection.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"services\">The service collection.</param>");
        sb.AppendLine("    /// <returns>The service collection for chaining.</returns>");
        sb.AppendLine($"    public static IServiceCollection Add{serviceName}Service(this IServiceCollection services)");
        sb.AppendLine("    {");

        if (supportedBlockchains?.Length > 0 && requiresEnclave)
        {
            sb.AppendLine($"        return services.AddEnclaveService<{interfaceName}, {className}>();");
        }
        else if (supportedBlockchains?.Length > 0)
        {
            sb.AppendLine($"        var blockchains = new[] {{ {string.Join(", ", supportedBlockchains.Select(b => $"BlockchainType.{b}"))} }};");
            sb.AppendLine($"        return services.AddBlockchainService<{interfaceName}, {className}>(blockchains);");
        }
        else
        {
            sb.AppendLine($"        return services.AddNeoService<{interfaceName}, {className}>();");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GetBaseClass(BlockchainType[]? supportedBlockchains, bool requiresEnclave)
    {
        if (requiresEnclave && supportedBlockchains?.Length > 0)
            return "EnclaveBlockchainServiceBase";
        if (requiresEnclave)
            return "EnclaveServiceBase";
        if (supportedBlockchains?.Length > 0)
            return "BlockchainServiceBase";
        return "ServiceBase";
    }

    private static string[] GetInterfaces(BlockchainType[]? supportedBlockchains, bool requiresEnclave)
    {
        var interfaces = new List<string> { "IService" };

        if (supportedBlockchains?.Length > 0)
            interfaces.Add("IBlockchainService");

        if (requiresEnclave)
            interfaces.Add("IEnclaveService");

        return interfaces.ToArray();
    }

    private static void GenerateConstructor(
        StringBuilder sb,
        string className,
        string serviceName,
        string description,
        BlockchainType[]? supportedBlockchains,
        bool requiresEnclave)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Initializes a new instance of the <see cref=\"{className}\"/> class.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"logger\">The logger.</param>");

        if (requiresEnclave)
        {
            sb.AppendLine("    /// <param name=\"enclaveManager\">The enclave manager.</param>");
        }

        sb.AppendLine("    /// <param name=\"configuration\">The service configuration.</param>");
        sb.AppendLine($"    public {className}(");
        sb.AppendLine($"        ILogger<{className}> logger,");

        if (requiresEnclave)
        {
            sb.AppendLine("        IEnclaveManager enclaveManager,");
        }

        sb.AppendLine("        IServiceConfiguration? configuration = null)");

        var baseParams = new List<string>
        {
            $"\"{serviceName}\"",
            $"\"{description}\"",
            "\"1.0.0\"",
            "logger"
        };

        if (supportedBlockchains?.Length > 0)
        {
            var blockchainArray = $"new[] {{ {string.Join(", ", supportedBlockchains.Select(b => $"BlockchainType.{b}"))} }}";
            baseParams.Add(blockchainArray);
        }

        if (requiresEnclave)
        {
            baseParams.Add("enclaveManager");
        }

        sb.AppendLine($"        : base({string.Join(", ", baseParams)})");
        sb.AppendLine("    {");
        sb.AppendLine("        Configuration = configuration;");
        sb.AppendLine();
        sb.AppendLine("        // Add capabilities");
        sb.AppendLine($"        AddCapability<I{serviceName}Service>();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets the service configuration.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    protected IServiceConfiguration? Configuration { get; }");
        sb.AppendLine();
    }

    private static void GenerateInterfaceImplementation(StringBuilder sb, string serviceName, BlockchainType[]? supportedBlockchains)
    {
        sb.AppendLine("    /// <inheritdoc/>");

        if (supportedBlockchains?.Length > 0)
        {
            sb.AppendLine($"    public async Task<{serviceName}Result> Process{serviceName}Async({serviceName}Request request, BlockchainType blockchainType)");
            sb.AppendLine("    {");
            sb.AppendLine("        ArgumentNullException.ThrowIfNull(request);");
            sb.AppendLine();
            sb.AppendLine("        if (!SupportsBlockchain(blockchainType))");
            sb.AppendLine("        {");
            sb.AppendLine("            throw new NotSupportedException($\"Blockchain {blockchainType} is not supported\");");
            sb.AppendLine("        }");
        }
        else
        {
            sb.AppendLine($"    public async Task<{serviceName}Result> Process{serviceName}Async({serviceName}Request request)");
            sb.AppendLine("    {");
            sb.AppendLine("        ArgumentNullException.ThrowIfNull(request);");
        }

        sb.AppendLine();
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine($"            Logger.LogInformation(\"Processing {serviceName.ToLower()} request {{RequestId}}\", request.RequestId);");
        sb.AppendLine();
        sb.AppendLine($"            // Validate request");
        sb.AppendLine("            if (string.IsNullOrWhiteSpace(request.RequestId))");
        sb.AppendLine("            {");
        sb.AppendLine("                throw new ArgumentException(\"RequestId cannot be null or empty\", nameof(request));");
        sb.AppendLine("            }");
        sb.AppendLine();
        if (supportedBlockchains?.Length > 0)
        {
            sb.AppendLine("            // Ensure blockchain client is available");
            sb.AppendLine("            var client = await GetBlockchainClientAsync(blockchainType);");
            sb.AppendLine("            if (client == null)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw new InvalidOperationException($\"Failed to get blockchain client for {blockchainType}\");");
            sb.AppendLine("            }");
            sb.AppendLine();
        }
        // Note: requiresEnclave parameter is not available in this scope, but it's not needed for the core logic
        sb.AppendLine("            // Process the request with validation and error handling");
        sb.AppendLine("            var processedData = await ProcessRequestDataAsync(request);");
        sb.AppendLine("            if (processedData == null)");
        sb.AppendLine("            {");
        sb.AppendLine("                throw new InvalidOperationException(\"Failed to process request data\");");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine($"            var result = new {serviceName}Result");
        sb.AppendLine("            {");
        sb.AppendLine("                RequestId = request.RequestId,");
        sb.AppendLine("                Success = true,");
        sb.AppendLine("                ProcessedAt = DateTime.UtcNow,");
        sb.AppendLine("                Data = processedData");
        sb.AppendLine("            };");
        sb.AppendLine();
        sb.AppendLine("            // Add processing metadata");
        sb.AppendLine("            result.Metadata[\"ProcessingTimeMs\"] = (DateTime.UtcNow - DateTime.UtcNow).TotalMilliseconds;");
        if (supportedBlockchains?.Length > 0)
        {
            sb.AppendLine("            result.Metadata[\"BlockchainType\"] = blockchainType.ToString();");
        }
        sb.AppendLine();
        sb.AppendLine($"            Logger.LogInformation(\"{serviceName} request {{RequestId}} processed successfully\", request.RequestId);");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception ex)");
        sb.AppendLine("        {");
        sb.AppendLine($"            Logger.LogError(ex, \"Error processing {serviceName.ToLower()} request {{RequestId}}\", request.RequestId);");
        sb.AppendLine($"            return new {serviceName}Result");
        sb.AppendLine("            {");
        sb.AppendLine("                RequestId = request.RequestId,");
        sb.AppendLine("                Success = false,");
        sb.AppendLine("                ErrorMessage = ex.Message,");
        sb.AppendLine("                ProcessedAt = DateTime.UtcNow");
        sb.AppendLine("            };");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateBaseClassOverrides(StringBuilder sb, string serviceName, BlockchainType[]? supportedBlockchains, bool requiresEnclave)
    {
        // OnInitializeAsync
        sb.AppendLine("    /// <inheritdoc/>");
        sb.AppendLine("    protected override async Task<bool> OnInitializeAsync()");
        sb.AppendLine("    {");
        sb.AppendLine($"        Logger.LogInformation(\"Initializing {serviceName} Service\");");
        sb.AppendLine();
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            // Initialize configuration");
        sb.AppendLine("            if (Configuration != null)");
        sb.AppendLine("            {");
        sb.AppendLine("                var isValid = await ValidateConfigurationAsync();");
        sb.AppendLine("                if (!isValid)");
        sb.AppendLine("                {");
        sb.AppendLine("                    Logger.LogError(\"Service configuration validation failed\");");
        sb.AppendLine("                    return false;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // Initialize dependencies");
        sb.AppendLine("            var dependenciesInitialized = await InitializeDependenciesAsync();");
        sb.AppendLine("            if (!dependenciesInitialized)");
        sb.AppendLine("            {");
        sb.AppendLine("                Logger.LogError(\"Failed to initialize service dependencies\");");
        sb.AppendLine("                return false;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // Perform service-specific initialization");
        sb.AppendLine("            var serviceInitialized = await InitializeServiceSpecificAsync();");
        sb.AppendLine("            if (!serviceInitialized)");
        sb.AppendLine("            {");
        sb.AppendLine("                Logger.LogError(\"Service-specific initialization failed\");");
        sb.AppendLine("                return false;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine($"            Logger.LogInformation(\"{serviceName} Service initialized successfully\");");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception ex)");
        sb.AppendLine("        {");
        sb.AppendLine($"            Logger.LogError(ex, \"Error initializing {serviceName} Service\");");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (requiresEnclave)
        {
            // OnInitializeEnclaveAsync
            sb.AppendLine("    /// <inheritdoc/>");
            sb.AppendLine("    protected override async Task<bool> OnInitializeEnclaveAsync()");
            sb.AppendLine("    {");
            sb.AppendLine($"        Logger.LogInformation(\"Initializing {serviceName} Service enclave\");");
            sb.AppendLine();
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine("            // Validate enclave availability");
            sb.AppendLine("            if (!IsEnclaveAvailable())");
            sb.AppendLine("            {");
            sb.AppendLine("                Logger.LogError(\"SGX enclave is not available on this system\");");
            sb.AppendLine("                return false;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            // Initialize enclave with required parameters");
            sb.AppendLine("            var enclaveConfig = GetEnclaveConfiguration();");
            sb.AppendLine("            var initSuccess = await InitializeEnclaveWithConfigAsync(enclaveConfig);");
            sb.AppendLine("            if (!initSuccess)");
            sb.AppendLine("            {");
            sb.AppendLine("                Logger.LogError(\"Failed to initialize enclave with configuration\");");
            sb.AppendLine("                return false;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            // Verify enclave attestation");
            sb.AppendLine("            var attestationValid = await VerifyEnclaveAttestationAsync();");
            sb.AppendLine("            if (!attestationValid)");
            sb.AppendLine("            {");
            sb.AppendLine("                Logger.LogError(\"Enclave attestation verification failed\");");
            sb.AppendLine("                return false;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            // Load service-specific enclave code");
            sb.AppendLine("            var codeLoaded = await LoadEnclaveServiceCodeAsync();");
            sb.AppendLine("            if (!codeLoaded)");
            sb.AppendLine("            {");
            sb.AppendLine("                Logger.LogError(\"Failed to load service-specific enclave code\");");
            sb.AppendLine("                return false;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine($"            Logger.LogInformation(\"{serviceName} Service enclave initialized successfully\");");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (Exception ex)");
            sb.AppendLine("        {");
            sb.AppendLine($"            Logger.LogError(ex, \"Error initializing {serviceName} Service enclave\");");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // OnStartAsync
        sb.AppendLine("    /// <inheritdoc/>");
        sb.AppendLine("    protected override async Task<bool> OnStartAsync()");
        sb.AppendLine("    {");
        sb.AppendLine($"        Logger.LogInformation(\"Starting {serviceName} Service\");");
        sb.AppendLine("        await Task.CompletedTask;");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // OnStopAsync
        sb.AppendLine("    /// <inheritdoc/>");
        sb.AppendLine("    protected override async Task<bool> OnStopAsync()");
        sb.AppendLine("    {");
        sb.AppendLine($"        Logger.LogInformation(\"Stopping {serviceName} Service\");");
        sb.AppendLine("        await Task.CompletedTask;");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // OnGetHealthAsync
        sb.AppendLine("    /// <inheritdoc/>");
        sb.AppendLine("    protected override Task<ServiceHealth> OnGetHealthAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            var health = ServiceHealth.Healthy;");
        sb.AppendLine("            var checks = new List<string>();");
        sb.AppendLine();
        sb.AppendLine("            // Check service configuration");
        sb.AppendLine("            if (Configuration == null)");
        sb.AppendLine("            {");
        sb.AppendLine("                health = ServiceHealth.Degraded;");
        sb.AppendLine("                checks.Add(\"Configuration is missing\");");
        sb.AppendLine("            }");
        sb.AppendLine();
        if (requiresEnclave)
        {
            sb.AppendLine("            // Check enclave status");
            sb.AppendLine("            if (!IsEnclaveInitialized)");
            sb.AppendLine("            {");
            sb.AppendLine("                health = ServiceHealth.Unhealthy;");
            sb.AppendLine("                checks.Add(\"Enclave is not initialized\");");
            sb.AppendLine("            }");
            sb.AppendLine("            else if (!IsEnclaveHealthy())");
            sb.AppendLine("            {");
            sb.AppendLine("                health = ServiceHealth.Degraded;");
            sb.AppendLine("                checks.Add(\"Enclave health check failed\");");
            sb.AppendLine("            }");
            sb.AppendLine();
        }
        if (supportedBlockchains?.Length > 0)
        {
            sb.AppendLine("            // Check blockchain connectivity");
            sb.AppendLine("            var blockchainHealthy = CheckBlockchainConnectivity();");
            sb.AppendLine("            if (!blockchainHealthy)");
            sb.AppendLine("            {");
            sb.AppendLine("                health = ServiceHealth.Degraded;");
            sb.AppendLine("                checks.Add(\"Blockchain connectivity issues\");");
            sb.AppendLine("            }");
            sb.AppendLine();
        }
        sb.AppendLine("            // Check resource usage");
        sb.AppendLine("            var resourceHealth = CheckResourceUsage();");
        sb.AppendLine("            if (resourceHealth != ServiceHealth.Healthy)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (health == ServiceHealth.Healthy) health = resourceHealth;");
        sb.AppendLine("                checks.Add($\"Resource usage: {resourceHealth}\");");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // Log health status if not healthy");
        sb.AppendLine("            if (health != ServiceHealth.Healthy)");
        sb.AppendLine("            {");
        sb.AppendLine($"                Logger.LogWarning(\"{serviceName} Service health check: {{Health}}, Issues: {{Issues}}\", health, string.Join(\", \", checks));");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return Task.FromResult(health);");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception ex)");
        sb.AppendLine("        {");
        sb.AppendLine($"            Logger.LogError(ex, \"Error during {serviceName} Service health check\");");
        sb.AppendLine("            return Task.FromResult(ServiceHealth.Unhealthy);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private static void GenerateHelperMethods(StringBuilder sb, string serviceName, BlockchainType[]? supportedBlockchains, bool requiresEnclave)
    {
        // ProcessRequestDataAsync method
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Processes the request data with validation and business logic.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    /// <param name=\"request\">The {serviceName.ToLower()} request.</param>");
        sb.AppendLine("    /// <returns>The processed data.</returns>");
        sb.AppendLine($"    protected virtual async Task<object?> ProcessRequestDataAsync({serviceName}Request request)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Override this method in derived classes to implement specific processing logic");
        sb.AppendLine("        await Task.CompletedTask;");
        sb.AppendLine("        return new { ProcessedAt = DateTime.UtcNow, RequestId = request.RequestId };");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (requiresEnclave)
        {
            // ExecuteInEnclaveAsync method
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Executes secure computation in the enclave.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    /// <param name=\"request\">The {serviceName.ToLower()} request.</param>");
            sb.AppendLine("    /// <returns>The enclave execution result.</returns>");
            sb.AppendLine($"    protected virtual async Task<EnclaveResult> ExecuteInEnclaveAsync({serviceName}Request request)");
            sb.AppendLine("    {");
            sb.AppendLine("        // Override this method to implement enclave-specific logic");
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine("            var enclaveData = await PrepareEnclaveDataAsync(request);");
            sb.AppendLine("            var result = await CallEnclaveAsync(enclaveData);");
            sb.AppendLine("            return new EnclaveResult { Success = true, Data = result };");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (Exception ex)");
            sb.AppendLine("        {");
            sb.AppendLine("            Logger.LogError(ex, \"Error executing in enclave\");");
            sb.AppendLine("            return new EnclaveResult { Success = false, ErrorMessage = ex.Message };");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Helper methods for enclave
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Prepares data for enclave processing.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    /// <param name=\"request\">The {serviceName.ToLower()} request.</param>");
            sb.AppendLine("    /// <returns>The prepared enclave data.</returns>");
            sb.AppendLine($"    protected virtual async Task<byte[]> PrepareEnclaveDataAsync({serviceName}Request request)");
            sb.AppendLine("    {");
            sb.AppendLine("        // Serialize request data for enclave");
            sb.AppendLine("        await Task.CompletedTask;");
            sb.AppendLine("        return System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(request));");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Calls the enclave with prepared data.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <param name=\"data\">The data to process in enclave.</param>");
            sb.AppendLine("    /// <returns>The enclave processing result.</returns>");
            sb.AppendLine("    protected virtual async Task<byte[]> CallEnclaveAsync(byte[] data)");
            sb.AppendLine("    {");
            sb.AppendLine("        // Override this method to implement actual enclave calls");
            sb.AppendLine("        await Task.Delay(10); // Simulate enclave processing");
            sb.AppendLine("        return System.Text.Encoding.UTF8.GetBytes(\"{\\\"status\\\":\\\"processed\\\"}\");");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        if (supportedBlockchains?.Length > 0)
        {
            // GetBlockchainClientAsync method
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Gets a blockchain client for the specified type.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <param name=\"blockchainType\">The blockchain type.</param>");
            sb.AppendLine("    /// <returns>The blockchain client.</returns>");
            sb.AppendLine("    protected virtual async Task<IBlockchainClient?> GetBlockchainClientAsync(BlockchainType blockchainType)");
            sb.AppendLine("    {");
            sb.AppendLine("        // Override this method to implement blockchain client retrieval");
            sb.AppendLine("        await Task.CompletedTask;");
            sb.AppendLine("        return null; // Should be replaced with actual blockchain client factory call");
            sb.AppendLine("    }");
            sb.AppendLine();

            // CheckBlockchainConnectivity method
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Checks connectivity to supported blockchains.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <returns>True if all supported blockchains are accessible.</returns>");
            sb.AppendLine("    protected virtual bool CheckBlockchainConnectivity()");
            sb.AppendLine("    {");
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine($"            var supportedTypes = new[] {{ {string.Join(", ", supportedBlockchains.Select(b => $"BlockchainType.{b}"))} }};");
            sb.AppendLine("            foreach (var type in supportedTypes)");
            sb.AppendLine("            {");
            sb.AppendLine("                // Check if blockchain client can be created and is responsive");
            sb.AppendLine("                // This should be implemented based on actual blockchain client factory");
            sb.AppendLine("            }");
            sb.AppendLine("            return true; // Placeholder - implement actual connectivity check");
            sb.AppendLine("        }");
            sb.AppendLine("        catch");
            sb.AppendLine("        {");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Common helper methods
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Validates the service configuration.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <returns>True if configuration is valid.</returns>");
        sb.AppendLine("    protected virtual async Task<bool> ValidateConfigurationAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Override this method to implement configuration validation");
        sb.AppendLine("        await Task.CompletedTask;");
        sb.AppendLine("        return Configuration != null;");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Initializes service dependencies.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <returns>True if dependencies are initialized successfully.</returns>");
        sb.AppendLine("    protected virtual async Task<bool> InitializeDependenciesAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Override this method to implement dependency initialization");
        sb.AppendLine("        await Task.CompletedTask;");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Performs service-specific initialization logic.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <returns>True if service-specific initialization is successful.</returns>");
        sb.AppendLine("    protected virtual async Task<bool> InitializeServiceSpecificAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Override this method to implement service-specific initialization");
        sb.AppendLine("        await Task.CompletedTask;");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (requiresEnclave)
        {
            // Enclave helper methods
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Checks if the enclave is available on the system.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <returns>True if enclave is available.</returns>");
            sb.AppendLine("    protected virtual bool IsEnclaveAvailable()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Override this method to implement enclave availability check");
            sb.AppendLine("        return true; // Placeholder - implement actual SGX availability check");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Gets the enclave configuration.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <returns>The enclave configuration.</returns>");
            sb.AppendLine("    protected virtual object GetEnclaveConfiguration()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Override this method to provide enclave-specific configuration");
            sb.AppendLine("        return new { EnclaveLibraryPath = \"./enclave/service.so\", DebugMode = false };");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Initializes the enclave with configuration.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <param name=\"config\">The enclave configuration.</param>");
            sb.AppendLine("    /// <returns>True if initialization is successful.</returns>");
            sb.AppendLine("    protected virtual async Task<bool> InitializeEnclaveWithConfigAsync(object config)");
            sb.AppendLine("    {");
            sb.AppendLine("        // Override this method to implement enclave initialization");
            sb.AppendLine("        await Task.Delay(10); // Simulate initialization");
            sb.AppendLine("        return true;");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Verifies the enclave attestation.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <returns>True if attestation is valid.</returns>");
            sb.AppendLine("    protected virtual async Task<bool> VerifyEnclaveAttestationAsync()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Override this method to implement attestation verification");
            sb.AppendLine("        await Task.Delay(10); // Simulate verification");
            sb.AppendLine("        return true;");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Loads service-specific enclave code.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <returns>True if code is loaded successfully.</returns>");
            sb.AppendLine("    protected virtual async Task<bool> LoadEnclaveServiceCodeAsync()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Override this method to load service-specific enclave code");
            sb.AppendLine("        await Task.Delay(10); // Simulate code loading");
            sb.AppendLine("        return true;");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Checks if the enclave is healthy.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    /// <returns>True if enclave is healthy.</returns>");
            sb.AppendLine("    protected virtual bool IsEnclaveHealthy()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Override this method to implement enclave health check");
            sb.AppendLine("        return IsEnclaveInitialized; // Basic check");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Resource usage check
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Checks resource usage for health monitoring.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <returns>The resource health status.</returns>");
        sb.AppendLine("    protected virtual ServiceHealth CheckResourceUsage()");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            // Check memory usage");
        sb.AppendLine("            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();");
        sb.AppendLine("            var memoryUsage = currentProcess.WorkingSet64;");
        sb.AppendLine("            var memoryThresholdBytes = 1_000_000_000; // 1GB threshold");
        sb.AppendLine();
        sb.AppendLine("            if (memoryUsage > memoryThresholdBytes)");
        sb.AppendLine("            {");
        sb.AppendLine("                return ServiceHealth.Degraded;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            return ServiceHealth.Healthy;");
        sb.AppendLine("        }");
        sb.AppendLine("        catch");
        sb.AppendLine("        {");
        sb.AppendLine("            return ServiceHealth.Degraded;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (requiresEnclave)
        {
            // EnclaveResult class
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Result of enclave execution.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    protected class EnclaveResult");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets whether the operation was successful.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public bool Success { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the error message if the operation failed.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public string? ErrorMessage { get; set; }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets or sets the result data.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public byte[]? Data { get; set; }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    private static void GenerateModels(StringBuilder sb, string serviceName, string @namespace)
    {
        // Generate request model
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Request model for {serviceName} operations.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class {serviceName}Request");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets or sets the request ID.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public string RequestId { get; set; } = Guid.NewGuid().ToString();");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets or sets additional metadata.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public Dictionary<string, object> Metadata { get; set; } = new();");
        sb.AppendLine("}");
        sb.AppendLine();

        // Generate result model
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Result model for {serviceName} operations.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class {serviceName}Result");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets or sets the request ID.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public string RequestId { get; set; } = string.Empty;");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets or sets whether the operation was successful.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public bool Success { get; set; }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets or sets the error message if the operation failed.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public string? ErrorMessage { get; set; }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets or sets when the operation was processed.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public DateTime ProcessedAt { get; set; }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets or sets the processed data.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public object? Data { get; set; }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets or sets additional metadata.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public Dictionary<string, object> Metadata { get; set; } = new();");
        sb.AppendLine("}");
    }
}

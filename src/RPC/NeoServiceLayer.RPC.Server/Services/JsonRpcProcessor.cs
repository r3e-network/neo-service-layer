using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.RPC.Server.Models;
using NeoServiceLayer.RPC.Server.Attributes;

namespace NeoServiceLayer.RPC.Server.Services;

/// <summary>
/// Processes JSON-RPC requests and routes them to appropriate service methods.
/// </summary>
public class JsonRpcProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JsonRpcProcessor> _logger;
    private readonly Dictionary<string, MethodInfo> _methodRegistry = new();
    private readonly Dictionary<string, Type> _serviceRegistry = new();

    public JsonRpcProcessor(IServiceProvider serviceProvider, ILogger<JsonRpcProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        RegisterMethods();
    }

    /// <summary>
    /// Processes a JSON-RPC request and returns the response.
    /// </summary>
    public async Task<JsonRpcResponse> ProcessRequestAsync(JsonRpcRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrEmpty(request.Method))
            {
                return CreateErrorResponse(request.Id, JsonRpcErrorCodes.InvalidRequest, "Method name is required");
            }

            // Check if method exists
            if (!_methodRegistry.TryGetValue(request.Method, out var methodInfo))
            {
                return CreateErrorResponse(request.Id, JsonRpcErrorCodes.MethodNotFound, $"Method '{request.Method}' not found");
            }

            // Get service instance
            var serviceType = _serviceRegistry[request.Method];
            var service = _serviceProvider.GetService(serviceType);
            if (service == null)
            {
                return CreateErrorResponse(request.Id, JsonRpcErrorCodes.InternalError, $"Service '{serviceType.Name}' not available");
            }

            // Prepare parameters
            var parameters = PrepareParameters(methodInfo, request.Params);
            if (parameters == null)
            {
                return CreateErrorResponse(request.Id, JsonRpcErrorCodes.InvalidParams, "Invalid parameters");
            }

            // Invoke method
            var result = methodInfo.Invoke(service, parameters);

            // Handle async methods
            if (result is Task task)
            {
                await task;
                
                // Get result from Task<T>
                if (task.GetType().IsGenericType)
                {
                    var property = task.GetType().GetProperty("Result");
                    result = property?.GetValue(task);
                }
                else
                {
                    result = null; // Task without return value
                }
            }

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (TargetParameterCountException)
        {
            return CreateErrorResponse(request.Id, JsonRpcErrorCodes.InvalidParams, "Parameter count mismatch");
        }
        catch (ArgumentException ex)
        {
            return CreateErrorResponse(request.Id, JsonRpcErrorCodes.InvalidParams, ex.Message);
        }
        catch (TargetInvocationException ex)
        {
            _logger.LogError(ex.InnerException, "Error invoking method {Method}", request.Method);
            return CreateErrorResponse(request.Id, JsonRpcErrorCodes.InternalError, "Internal server error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing request for method {Method}", request.Method);
            return CreateErrorResponse(request.Id, JsonRpcErrorCodes.InternalError, "Unexpected server error");
        }
    }

    /// <summary>
    /// Registers all available RPC methods by scanning services.
    /// </summary>
    private void RegisterMethods()
    {
        var services = new[]
        {
            typeof(NeoServiceLayer.Services.KeyManagement.IKeyManagementService),
            typeof(NeoServiceLayer.Services.Storage.IStorageService),
            typeof(NeoServiceLayer.Services.Oracle.IOracleService),
            typeof(NeoServiceLayer.Services.Voting.IVotingService),
            typeof(NeoServiceLayer.Services.Randomness.IRandomnessService),
            typeof(NeoServiceLayer.Services.SmartContracts.ISmartContractService)
        };

        foreach (var serviceType in services)
        {
            RegisterServiceMethods(serviceType);
        }

        _logger.LogInformation("Registered {Count} RPC methods", _methodRegistry.Count);
    }

    /// <summary>
    /// Registers methods from a specific service type.
    /// </summary>
    private void RegisterServiceMethods(Type serviceType)
    {
        var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var method in methods)
        {
            var rpcAttribute = method.GetCustomAttribute<JsonRpcMethodAttribute>();
            if (rpcAttribute != null)
            {
                var methodName = rpcAttribute.MethodName ?? $"{serviceType.Name.Replace("Service", "").Replace("I", "").ToLower()}.{method.Name.ToLower()}";
                
                _methodRegistry[methodName] = method;
                _serviceRegistry[methodName] = serviceType;
                
                _logger.LogDebug("Registered RPC method: {MethodName} -> {ServiceType}.{MethodName}", 
                    methodName, serviceType.Name, method.Name);
            }
        }
    }

    /// <summary>
    /// Prepares method parameters from the JSON-RPC request.
    /// </summary>
    private object[]? PrepareParameters(MethodInfo methodInfo, object? requestParams)
    {
        var parameters = methodInfo.GetParameters();
        
        if (parameters.Length == 0)
        {
            return Array.Empty<object>();
        }

        if (requestParams == null)
        {
            // Check if all parameters are optional
            if (parameters.All(p => p.HasDefaultValue))
            {
                return parameters.Select(p => p.DefaultValue).ToArray();
            }
            return null;
        }

        try
        {
            // Handle array parameters
            if (requestParams is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                var paramArray = JsonSerializer.Deserialize<object[]>(jsonElement.GetRawText());
                if (paramArray?.Length != parameters.Length)
                {
                    return null;
                }

                var result = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    result[i] = ConvertParameter(paramArray[i], parameters[i].ParameterType);
                }
                return result;
            }

            // Handle object parameters (named parameters)
            if (requestParams is JsonElement objectElement && objectElement.ValueKind == JsonValueKind.Object)
            {
                var result = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramName = parameters[i].Name!;
                    if (objectElement.TryGetProperty(paramName, out var propElement))
                    {
                        result[i] = ConvertParameter(propElement, parameters[i].ParameterType);
                    }
                    else if (parameters[i].HasDefaultValue)
                    {
                        result[i] = parameters[i].DefaultValue!;
                    }
                    else
                    {
                        return null; // Required parameter missing
                    }
                }
                return result;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to prepare parameters for method {Method}", methodInfo.Name);
            return null;
        }
    }

    /// <summary>
    /// Converts a parameter value to the target type.
    /// </summary>
    private object ConvertParameter(object value, Type targetType)
    {
        if (value is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize(jsonElement.GetRawText(), targetType)!;
        }

        if (targetType.IsAssignableFrom(value.GetType()))
        {
            return value;
        }

        return Convert.ChangeType(value, targetType);
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    private static JsonRpcResponse CreateErrorResponse(object? id, int code, string message, object? data = null)
    {
        return new JsonRpcResponse
        {
            Id = id,
            Error = new JsonRpcError
            {
                Code = code,
                Message = message,
                Data = data
            }
        };
    }

    /// <summary>
    /// Gets all registered methods for introspection.
    /// </summary>
    public IReadOnlyDictionary<string, string> GetRegisteredMethods()
    {
        return _methodRegistry.ToDictionary(
            kvp => kvp.Key,
            kvp => $"{_serviceRegistry[kvp.Key].Name}.{kvp.Value.Name}"
        );
    }
}
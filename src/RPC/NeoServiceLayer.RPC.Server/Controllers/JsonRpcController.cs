using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using NeoServiceLayer.RPC.Server.Models;
using NeoServiceLayer.RPC.Server.Services;

namespace NeoServiceLayer.RPC.Server.Controllers;

/// <summary>
/// JSON-RPC endpoint controller.
/// </summary>
[ApiController]
[Route("rpc")]
[Route("")]
public class JsonRpcController : ControllerBase
{
    private readonly JsonRpcProcessor _processor;
    private readonly ILogger<JsonRpcController> _logger;

    public JsonRpcController(JsonRpcProcessor processor, ILogger<JsonRpcController> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    /// <summary>
    /// Handles JSON-RPC requests via POST.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> HandleRequest([FromBody] JsonElement requestElement)
    {
        try
        {
            // Handle batch requests
            if (requestElement.ValueKind == JsonValueKind.Array)
            {
                var requests = JsonSerializer.Deserialize<JsonRpcRequest[]>(requestElement.GetRawText());
                if (requests == null || requests.Length == 0)
                {
                    return BadRequest(CreateErrorResponse(null, JsonRpcErrorCodes.InvalidRequest, "Empty batch request"));
                }

                var responses = new List<JsonRpcResponse>();
                foreach (var request in requests)
                {
                    var response = await _processor.ProcessRequestAsync(request);
                    responses.Add(response);
                }

                return Ok(responses);
            }
            
            // Handle single request
            var singleRequest = JsonSerializer.Deserialize<JsonRpcRequest>(requestElement.GetRawText());
            if (singleRequest == null)
            {
                return BadRequest(CreateErrorResponse(null, JsonRpcErrorCodes.InvalidRequest, "Invalid request format"));
            }

            var singleResponse = await _processor.ProcessRequestAsync(singleRequest);
            return Ok(singleResponse);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON-RPC request");
            return BadRequest(CreateErrorResponse(null, JsonRpcErrorCodes.ParseError, "Invalid JSON"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error handling JSON-RPC request");
            return StatusCode(500, CreateErrorResponse(null, JsonRpcErrorCodes.InternalError, "Internal server error"));
        }
    }

    /// <summary>
    /// Gets information about available RPC methods.
    /// </summary>
    [HttpGet("methods")]
    [AllowAnonymous]
    public IActionResult GetMethods()
    {
        var methods = _processor.GetRegisteredMethods();
        return Ok(new
        {
            version = "2.0",
            methods = methods,
            server = "Neo Service Layer RPC Server",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Health check endpoint for the RPC server.
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            server = "Neo Service Layer RPC Server",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            methods_count = _processor.GetRegisteredMethods().Count
        });
    }

    /// <summary>
    /// Server information endpoint.
    /// </summary>
    [HttpGet("info")]
    [AllowAnonymous]
    public IActionResult Info()
    {
        return Ok(new
        {
            name = "Neo Service Layer RPC Server",
            version = "1.0.0",
            protocol = "JSON-RPC 2.0",
            description = "Enterprise-grade JSON-RPC server for Neo Service Layer",
            features = new[]
            {
                "Batch requests",
                "Method introspection",
                "Rate limiting", 
                "Authentication",
                "Real-time notifications",
                "Health monitoring"
            },
            endpoints = new
            {
                rpc = "/rpc",
                methods = "/rpc/methods", 
                health = "/rpc/health",
                info = "/rpc/info",
                websocket = "/ws"
            }
        });
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
}
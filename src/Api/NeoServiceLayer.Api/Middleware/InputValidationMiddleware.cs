using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Api.Middleware;

public class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputValidationMiddleware> _logger;
    private readonly InputValidationOptions _options;

    public InputValidationMiddleware(
        RequestDelegate next,
        ILogger<InputValidationMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _options = configuration.GetSection("InputValidation").Get<InputValidationOptions>()
                   ?? new InputValidationOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Validate request size
            if (context.Request.ContentLength > _options.MaxRequestBodySize)
            {
                await WriteErrorResponse(context, 413, "Request body too large");
                return;
            }

            // Validate Content-Type
            if (!IsValidContentType(context))
            {
                await WriteErrorResponse(context, 415, "Unsupported media type");
                return;
            }

            // Validate headers
            if (!ValidateHeaders(context))
            {
                await WriteErrorResponse(context, 400, "Invalid request headers");
                return;
            }

            // Validate query parameters
            if (!ValidateQueryParameters(context))
            {
                await WriteErrorResponse(context, 400, "Invalid query parameters");
                return;
            }

            // For POST/PUT/PATCH requests, validate body
            if (context.Request.Method == HttpMethod.Post.Method ||
                context.Request.Method == HttpMethod.Put.Method ||
                context.Request.Method == HttpMethod.Patch.Method)
            {
                if (!await ValidateRequestBodyAsync(context))
                {
                    return; // Error response already written
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in input validation middleware");
            await WriteErrorResponse(context, 500, "Internal server error during validation");
        }
    }

    private bool IsValidContentType(HttpContext context)
    {
        if (context.Request.ContentLength == 0)
            return true;

        var contentType = context.Request.ContentType?.ToLower() ?? "";

        return _options.AllowedContentTypes.Any(allowed =>
            contentType.StartsWith(allowed.ToLower()));
    }

    private bool ValidateHeaders(HttpContext context)
    {
        foreach (var header in context.Request.Headers)
        {
            // Check header name
            if (!IsValidHeaderName(header.Key))
            {
                _logger.LogWarning("Invalid header name detected: {HeaderName}", header.Key);
                return false;
            }

            // Check header value
            foreach (var value in header.Value)
            {
                if (!IsValidHeaderValue(value))
                {
                    _logger.LogWarning("Invalid header value detected for header {HeaderName}", header.Key);
                    return false;
                }

                // Check for common injection patterns
                if (ContainsInjectionPattern(value))
                {
                    _logger.LogWarning("Potential injection in header {HeaderName}", header.Key);
                    return false;
                }
            }
        }

        return true;
    }

    private bool ValidateQueryParameters(HttpContext context)
    {
        foreach (var param in context.Request.Query)
        {
            // Validate parameter name
            if (!IsValidParameterName(param.Key))
            {
                _logger.LogWarning("Invalid query parameter name: {ParamName}", param.Key);
                return false;
            }

            // Validate parameter values
            foreach (var value in param.Value)
            {
                if (string.IsNullOrEmpty(value))
                    continue;

                // Check length
                if (value.Length > _options.MaxParameterValueLength)
                {
                    _logger.LogWarning("Query parameter {ParamName} value too long", param.Key);
                    return false;
                }

                // Check for injection patterns
                if (ContainsInjectionPattern(value))
                {
                    _logger.LogWarning("Potential injection in query parameter {ParamName}", param.Key);
                    return false;
                }

                // Validate specific parameter formats
                if (!ValidateParameterFormat(param.Key, value))
                {
                    _logger.LogWarning("Invalid format for query parameter {ParamName}", param.Key);
                    return false;
                }
            }
        }

        return true;
    }

    private async Task<bool> ValidateRequestBodyAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        try
        {
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (string.IsNullOrEmpty(body))
                return true;

            // Check body size
            if (body.Length > _options.MaxRequestBodySize)
            {
                await WriteErrorResponse(context, 413, "Request body too large");
                return false;
            }

            // Validate based on content type
            var contentType = context.Request.ContentType?.ToLower() ?? "";

            if (contentType.StartsWith("application/json"))
            {
                return await ValidateJsonBodyAsync(context, body);
            }
            else if (contentType.StartsWith("application/xml"))
            {
                return await ValidateXmlBodyAsync(context, body);
            }
            else if (contentType.StartsWith("application/x-www-form-urlencoded"))
            {
                return ValidateFormBody(context, body);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating request body");
            await WriteErrorResponse(context, 400, "Invalid request body");
            return false;
        }
    }

    private async Task<bool> ValidateJsonBodyAsync(HttpContext context, string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            // Validate JSON structure depth
            if (GetJsonDepth(root) > _options.MaxJsonDepth)
            {
                await WriteErrorResponse(context, 400, "JSON structure too deep");
                return false;
            }

            // Validate JSON properties
            if (!ValidateJsonElement(root))
            {
                await WriteErrorResponse(context, 400, "Invalid JSON content");
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON in request body");
            await WriteErrorResponse(context, 400, "Invalid JSON format");
            return false;
        }
    }

    private bool ValidateJsonElement(JsonElement element, int currentDepth = 0)
    {
        if (currentDepth > _options.MaxJsonDepth)
            return false;

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    // Validate property name
                    if (!IsValidPropertyName(property.Name))
                    {
                        _logger.LogWarning("Invalid JSON property name: {PropertyName}", property.Name);
                        return false;
                    }

                    // Recursively validate property value
                    if (!ValidateJsonElement(property.Value, currentDepth + 1))
                        return false;
                }
                break;

            case JsonValueKind.Array:
                if (element.GetArrayLength() > _options.MaxArrayLength)
                {
                    _logger.LogWarning("JSON array too large");
                    return false;
                }

                foreach (var item in element.EnumerateArray())
                {
                    if (!ValidateJsonElement(item, currentDepth + 1))
                        return false;
                }
                break;

            case JsonValueKind.String:
                var stringValue = element.GetString();
                if (stringValue != null)
                {
                    if (stringValue.Length > _options.MaxStringLength)
                    {
                        _logger.LogWarning("JSON string value too long");
                        return false;
                    }

                    if (ContainsInjectionPattern(stringValue))
                    {
                        _logger.LogWarning("Potential injection in JSON string value");
                        return false;
                    }
                }
                break;
        }

        return true;
    }

    private async Task<bool> ValidateXmlBodyAsync(HttpContext context, string body)
    {
        // Basic XML validation - check for common XXE patterns
        if (body.Contains("<!DOCTYPE") || body.Contains("<!ENTITY"))
        {
            _logger.LogWarning("Potential XXE attack detected in XML body");
            await WriteErrorResponse(context, 400, "XML external entities not allowed");
            return false;
        }

        return true;
    }

    private bool ValidateFormBody(HttpContext context, string body)
    {
        var formData = System.Web.HttpUtility.ParseQueryString(body);

        foreach (string key in formData.AllKeys)
        {
            if (key == null) continue;

            if (!IsValidParameterName(key))
                return false;

            var value = formData[key];
            if (value != null && ContainsInjectionPattern(value))
                return false;
        }

        return true;
    }

    private bool IsValidHeaderName(string name)
    {
        return Regex.IsMatch(name, @"^[a-zA-Z0-9\-_]+$");
    }

    private bool IsValidHeaderValue(string value)
    {
        // Check for control characters
        return !value.Any(c => char.IsControl(c) && c != '\t');
    }

    private bool IsValidParameterName(string name)
    {
        return Regex.IsMatch(name, @"^[a-zA-Z0-9\-_\.\[\]]+$");
    }

    private bool IsValidPropertyName(string name)
    {
        return Regex.IsMatch(name, @"^[a-zA-Z0-9\-_\.]+$");
    }

    private bool ContainsInjectionPattern(string value)
    {
        // SQL Injection patterns
        if (Regex.IsMatch(value, @"(\b(union|select|insert|update|delete|drop|exec|execute|xp_|sp_)\b)|(-{2})|(/\*.*\*/)", RegexOptions.IgnoreCase))
            return true;

        // XSS patterns
        if (Regex.IsMatch(value, @"<\s*(script|iframe|object|embed|form)", RegexOptions.IgnoreCase))
            return true;

        // Command injection patterns
        if (Regex.IsMatch(value, @"[;&|`$]|\$\(|\b(wget|curl|bash|sh|cmd|powershell)\b", RegexOptions.IgnoreCase))
            return true;

        // Path traversal patterns
        if (value.Contains("../") || value.Contains("..\\"))
            return true;

        return false;
    }

    private bool ValidateParameterFormat(string paramName, string value)
    {
        // Add specific validation for known parameters
        switch (paramName.ToLower())
        {
            case "email":
                return Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

            case "id":
            case "userid":
            case "keyid":
                return Regex.IsMatch(value, @"^[a-zA-Z0-9\-_]+$");

            case "limit":
            case "offset":
            case "page":
                return int.TryParse(value, out var num) && num >= 0 && num <= 10000;

            case "sort":
            case "orderby":
                return Regex.IsMatch(value, @"^[a-zA-Z0-9_]+(\s+(asc|desc))?$", RegexOptions.IgnoreCase);

            default:
                return true;
        }
    }

    private int GetJsonDepth(JsonElement element, int currentDepth = 0)
    {
        if (currentDepth > _options.MaxJsonDepth)
            return currentDepth;

        int maxDepth = currentDepth;

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var depth = GetJsonDepth(property.Value, currentDepth + 1);
                    maxDepth = Math.Max(maxDepth, depth);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var depth = GetJsonDepth(item, currentDepth + 1);
                    maxDepth = Math.Max(maxDepth, depth);
                }
                break;
        }

        return maxDepth;
    }

    private async Task WriteErrorResponse(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = message,
            timestamp = DateTime.UtcNow,
            path = context.Request.Path.Value
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

public class InputValidationOptions
{
    public long MaxRequestBodySize { get; set; } = 10 * 1024 * 1024; // 10MB
    public int MaxParameterValueLength { get; set; } = 1000;
    public int MaxStringLength { get; set; } = 10000;
    public int MaxArrayLength { get; set; } = 1000;
    public int MaxJsonDepth { get; set; } = 10;
    public string[] AllowedContentTypes { get; set; } = new[]
    {
        "application/json",
        "application/xml",
        "application/x-www-form-urlencoded",
        "multipart/form-data",
        "text/plain"
    };
}

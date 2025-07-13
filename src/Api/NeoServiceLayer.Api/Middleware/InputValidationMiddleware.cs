using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        // Skip validation for certain paths
        if (ShouldSkipValidation(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            // Validate request size
            if (!ValidateRequestSize(context))
            {
                await WriteErrorResponse(context, "Request size exceeds maximum allowed", 413);
                return;
            }

            // Validate content type
            if (!ValidateContentType(context))
            {
                await WriteErrorResponse(context, "Unsupported content type", 415);
                return;
            }

            // Enable request body buffering for multiple reads
            context.Request.EnableBuffering();

            // Read and validate request body
            if (context.Request.ContentLength > 0)
            {
                var bodyText = await ReadRequestBodyAsync(context.Request);
                
                if (!string.IsNullOrEmpty(bodyText))
                {
                    // Check for SQL injection patterns
                    if (ContainsSqlInjectionPatterns(bodyText))
                    {
                        _logger.LogWarning("Potential SQL injection detected in request from {IP}", 
                            context.Connection.RemoteIpAddress);
                        await WriteErrorResponse(context, "Invalid request content", 400);
                        return;
                    }

                    // Check for XSS patterns
                    if (ContainsXssPatterns(bodyText))
                    {
                        _logger.LogWarning("Potential XSS attack detected in request from {IP}", 
                            context.Connection.RemoteIpAddress);
                        await WriteErrorResponse(context, "Invalid request content", 400);
                        return;
                    }

                    // Check for path traversal patterns
                    if (ContainsPathTraversalPatterns(bodyText))
                    {
                        _logger.LogWarning("Potential path traversal detected in request from {IP}", 
                            context.Connection.RemoteIpAddress);
                        await WriteErrorResponse(context, "Invalid request content", 400);
                        return;
                    }
                }

                // Reset the request body stream position
                context.Request.Body.Position = 0;
            }

            // Validate headers
            if (!ValidateHeaders(context))
            {
                await WriteErrorResponse(context, "Invalid request headers", 400);
                return;
            }

            // Validate query parameters
            if (!ValidateQueryParameters(context))
            {
                await WriteErrorResponse(context, "Invalid query parameters", 400);
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in input validation middleware");
            await WriteErrorResponse(context, "Internal server error", 500);
        }
    }

    private bool ShouldSkipValidation(PathString path)
    {
        var skipPaths = new[] { "/health", "/metrics", "/swagger", "/.well-known" };
        return skipPaths.Any(sp => path.StartsWithSegments(sp));
    }

    private bool ValidateRequestSize(HttpContext context)
    {
        var contentLength = context.Request.ContentLength;
        return !contentLength.HasValue || contentLength.Value <= _options.MaxRequestBodySize;
    }

    private bool ValidateContentType(HttpContext context)
    {
        if (context.Request.Method == "GET" || context.Request.Method == "DELETE")
            return true;

        var contentType = context.Request.ContentType?.ToLower();
        if (string.IsNullOrEmpty(contentType))
            return false;

        return _options.AllowedContentTypes.Any(act => contentType.Contains(act));
    }

    private bool ValidateHeaders(HttpContext context)
    {
        foreach (var header in context.Request.Headers)
        {
            // Check header name
            if (!IsValidHeaderName(header.Key))
                return false;

            // Check header value
            foreach (var value in header.Value)
            {
                if (!IsValidHeaderValue(value))
                    return false;

                // Check for header injection
                if (ContainsHeaderInjection(value))
                {
                    _logger.LogWarning("Header injection detected in header {Header}", header.Key);
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
            // Check parameter name
            if (!IsValidParameterName(param.Key))
                return false;

            // Check parameter values
            foreach (var value in param.Value)
            {
                if (!IsValidParameterValue(value))
                    return false;
            }
        }

        return true;
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(
            request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private bool ContainsSqlInjectionPatterns(string input)
    {
        var sqlPatterns = new[]
        {
            @"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?)\b)",
            @"(--|\*|;|'|""|=|<|>|\|\||&&)",
            @"(@@\w+|WAITFOR\s+DELAY|BENCHMARK\s*\(|SLEEP\s*\()",
            @"(xp_cmdshell|sp_executesql|OPENROWSET|OPENDATASOURCE)"
        };

        var upperInput = input.ToUpper();
        return sqlPatterns.Any(pattern => Regex.IsMatch(upperInput, pattern, RegexOptions.IgnoreCase));
    }

    private bool ContainsXssPatterns(string input)
    {
        var xssPatterns = new[]
        {
            @"<script[^>]*>[\s\S]*?</script>",
            @"javascript\s*:",
            @"on\w+\s*=",
            @"<iframe[^>]*>",
            @"<object[^>]*>",
            @"<embed[^>]*>",
            @"<link[^>]*>",
            @"eval\s*\(",
            @"expression\s*\(",
            @"vbscript\s*:",
            @"<img[^>]+src[\\s]*=[\\s]*[""']javascript:"
        };

        return xssPatterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }

    private bool ContainsPathTraversalPatterns(string input)
    {
        var pathPatterns = new[]
        {
            @"\.\./",
            @"\.\.\\",
            @"%2e%2e[/\\]",
            @"%252e%252e[/\\]",
            @"\.\.%",
            @"/etc/passwd",
            @"C:\\Windows",
            @"..%c0%af",
            @"..%c1%9c"
        };

        return pathPatterns.Any(pattern => input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool ContainsHeaderInjection(string value)
    {
        return value.Contains('\r') || value.Contains('\n');
    }

    private bool IsValidHeaderName(string name)
    {
        return !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[a-zA-Z0-9\-_]+$");
    }

    private bool IsValidHeaderValue(string value)
    {
        return !string.IsNullOrEmpty(value) && value.Length <= _options.MaxHeaderValueLength;
    }

    private bool IsValidParameterName(string name)
    {
        return !string.IsNullOrEmpty(name) && 
               name.Length <= _options.MaxParameterNameLength &&
               Regex.IsMatch(name, @"^[a-zA-Z0-9\-_\[\]\.]+$");
    }

    private bool IsValidParameterValue(string value)
    {
        return value == null || value.Length <= _options.MaxParameterValueLength;
    }

    private async Task WriteErrorResponse(HttpContext context, string message, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = new
            {
                message,
                timestamp = DateTime.UtcNow,
                path = context.Request.Path.ToString()
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

public class InputValidationOptions
{
    public long MaxRequestBodySize { get; set; } = 33554432; // 32 MB
    public int MaxHeaderValueLength { get; set; } = 8192;
    public int MaxParameterNameLength { get; set; } = 256;
    public int MaxParameterValueLength { get; set; } = 8192;
    public string[] AllowedContentTypes { get; set; } = new[]
    {
        "application/json",
        "application/xml",
        "text/xml",
        "application/x-www-form-urlencoded",
        "multipart/form-data"
    };
}

// Attribute for model validation
public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            var response = new
            {
                error = new
                {
                    message = "Validation failed",
                    details = errors,
                    timestamp = DateTime.UtcNow
                }
            };

            context.Result = new BadRequestObjectResult(response);
        }
    }
}

// Custom validation attributes
public class NoSqlInjectionAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        var stringValue = value.ToString();
        var sqlPatterns = new[]
        {
            @"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?)\b)",
            @"(--|\*|;|'|""|=|<|>)"
        };

        if (sqlPatterns.Any(pattern => Regex.IsMatch(stringValue, pattern, RegexOptions.IgnoreCase)))
        {
            return new ValidationResult("Input contains potentially dangerous SQL patterns");
        }

        return ValidationResult.Success;
    }
}

public class NoXssAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        var stringValue = value.ToString();
        var xssPatterns = new[]
        {
            "<script", "javascript:", "onerror=", "onclick=", "<iframe", "<object"
        };

        if (xssPatterns.Any(pattern => stringValue.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return new ValidationResult("Input contains potentially dangerous script content");
        }

        return ValidationResult.Success;
    }
}

public class SecureFileNameAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        var fileName = value.ToString();
        
        // Check for path traversal
        if (fileName.Contains("..") || fileName.Contains("~"))
        {
            return new ValidationResult("File name contains invalid characters");
        }

        // Check for valid file name characters
        if (!Regex.IsMatch(fileName, @"^[a-zA-Z0-9\-_\.]+$"))
        {
            return new ValidationResult("File name contains invalid characters");
        }

        // Check file extension
        var allowedExtensions = new[] { ".txt", ".pdf", ".jpg", ".png", ".doc", ".docx" };
        var extension = Path.GetExtension(fileName).ToLower();
        
        if (!allowedExtensions.Contains(extension))
        {
            return new ValidationResult($"File type '{extension}' is not allowed");
        }

        return ValidationResult.Success;
    }
}
# Neo Service Layer API Error Handling

## Overview

This document describes how errors are handled in the Neo Service Layer API and how to handle them in your applications.

## Error Response Format

When an error occurs, the Neo Service Layer API returns a response with a non-200 HTTP status code and an error object in the response body:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "error-code",
    "message": "Error message",
    "details": {
      // Error details
    }
  },
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

The error object contains the following fields:

- **code**: A string code that identifies the error.
- **message**: A human-readable error message.
- **details**: An object containing additional details about the error.

## HTTP Status Codes

The Neo Service Layer API uses the following HTTP status codes:

- **400 Bad Request**: The request was invalid or cannot be served.
- **401 Unauthorized**: Authentication is required or has failed.
- **403 Forbidden**: The request is not allowed.
- **404 Not Found**: The requested resource does not exist.
- **409 Conflict**: The request conflicts with the current state of the server.
- **429 Too Many Requests**: The user has sent too many requests in a given amount of time.
- **500 Internal Server Error**: An error occurred on the server.
- **503 Service Unavailable**: The server is currently unavailable.

## Error Codes

The Neo Service Layer API uses the following error codes:

### Authentication Errors

- **auth_required**: Authentication is required.
- **invalid_api_key**: The API key is invalid.
- **expired_api_key**: The API key has expired.
- **invalid_jwt**: The JWT token is invalid.
- **expired_jwt**: The JWT token has expired.
- **invalid_oauth_token**: The OAuth 2.0 token is invalid.
- **expired_oauth_token**: The OAuth 2.0 token has expired.

### Validation Errors

- **invalid_request**: The request is invalid.
- **missing_parameter**: A required parameter is missing.
- **invalid_parameter**: A parameter is invalid.
- **invalid_blockchain**: The blockchain type is invalid.
- **unsupported_blockchain**: The blockchain type is not supported.

### Resource Errors

- **resource_not_found**: The requested resource does not exist.
- **resource_already_exists**: The resource already exists.
- **resource_conflict**: The request conflicts with the current state of the resource.

### Service Errors

- **service_unavailable**: The service is currently unavailable.
- **service_error**: An error occurred in the service.
- **enclave_error**: An error occurred in the enclave.
- **blockchain_error**: An error occurred in the blockchain.

### Rate Limiting Errors

- **rate_limit_exceeded**: The rate limit has been exceeded.

## Handling Errors

When handling errors in your applications, you should check the HTTP status code and the error code to determine the appropriate action.

### Example: JavaScript

```javascript
fetch('https://api.neoservicelayer.org/api/v1/randomness/generate', {
  headers: {
    'X-API-Key': 'your-api-key'
  }
})
.then(response => {
  if (!response.ok) {
    return response.json().then(errorData => {
      throw new Error(`${response.status} ${errorData.error.code}: ${errorData.error.message}`);
    });
  }
  return response.json();
})
.then(data => {
  console.log(data);
})
.catch(error => {
  console.error('Error:', error.message);
});
```

### Example: Python

```python
import requests

url = "https://api.neoservicelayer.org/api/v1/randomness/generate"
headers = {
    "X-API-Key": "your-api-key"
}

response = requests.get(url, headers=headers)
if response.status_code != 200:
    error_data = response.json()
    raise Exception(f"{response.status_code} {error_data['error']['code']}: {error_data['error']['message']}")

data = response.json()
print(data)
```

### Example: C#

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

public class ApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(string apiKey)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    public async Task<T> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync($"https://api.neoservicelayer.org/api/v1/{endpoint}");
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorData = JsonSerializer.Deserialize<ErrorResponse>(content);
            throw new Exception($"{(int)response.StatusCode} {errorData.Error.Code}: {errorData.Error.Message}");
        }

        var data = JsonSerializer.Deserialize<SuccessResponse<T>>(content);
        return data.Data;
    }
}

public class SuccessResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public object Error { get; set; }
    public object Meta { get; set; }
}

public class ErrorResponse
{
    public bool Success { get; set; }
    public object Data { get; set; }
    public ErrorData Error { get; set; }
    public object Meta { get; set; }
}

public class ErrorData
{
    public string Code { get; set; }
    public string Message { get; set; }
    public object Details { get; set; }
}
```

## Retry Strategy

When handling transient errors (e.g., 429 Too Many Requests, 503 Service Unavailable), you should implement a retry strategy with exponential backoff:

```javascript
async function fetchWithRetry(url, options, maxRetries = 3, initialDelay = 1000) {
  let retries = 0;
  let delay = initialDelay;

  while (retries < maxRetries) {
    try {
      const response = await fetch(url, options);
      
      if (response.status === 429 || response.status === 503) {
        // Get retry-after header if available
        const retryAfter = response.headers.get('Retry-After');
        const retryAfterMs = retryAfter ? parseInt(retryAfter) * 1000 : delay;
        
        console.log(`Rate limited. Retrying after ${retryAfterMs}ms...`);
        await new Promise(resolve => setTimeout(resolve, retryAfterMs));
        
        retries++;
        delay *= 2; // Exponential backoff
        continue;
      }
      
      if (!response.ok) {
        return response.json().then(errorData => {
          throw new Error(`${response.status} ${errorData.error.code}: ${errorData.error.message}`);
        });
      }
      
      return response.json();
    } catch (error) {
      if (retries === maxRetries - 1) {
        throw error;
      }
      
      console.log(`Error: ${error.message}. Retrying...`);
      await new Promise(resolve => setTimeout(resolve, delay));
      
      retries++;
      delay *= 2; // Exponential backoff
    }
  }
}
```

## Error Logging

When handling errors in your applications, you should log them for debugging purposes:

```javascript
fetch('https://api.neoservicelayer.org/api/v1/randomness/generate', {
  headers: {
    'X-API-Key': 'your-api-key'
  }
})
.then(response => {
  if (!response.ok) {
    return response.json().then(errorData => {
      console.error('API Error:', {
        status: response.status,
        code: errorData.error.code,
        message: errorData.error.message,
        details: errorData.error.details,
        requestId: errorData.meta.requestId,
        timestamp: errorData.meta.timestamp
      });
      throw new Error(`${response.status} ${errorData.error.code}: ${errorData.error.message}`);
    });
  }
  return response.json();
})
.then(data => {
  console.log(data);
})
.catch(error => {
  console.error('Error:', error.message);
});
```

## References

- [Neo Service Layer API](README.md)
- [Neo Service Layer API Endpoints](endpoints.md)
- [Neo Service Layer API Authentication](authentication.md)

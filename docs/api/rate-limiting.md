# Neo Service Layer API Rate Limiting

## Overview

The Neo Service Layer API implements rate limiting to prevent abuse and ensure fair usage of the API. This document describes how rate limiting works and how to handle rate limit errors in your applications.

## Rate Limits

Rate limits are applied on a per-API-key basis. The rate limits are as follows:

- **Requests per second**: 10
- **Requests per minute**: 100
- **Requests per hour**: 1,000
- **Requests per day**: 10,000

Different endpoints may have different rate limits. For example, endpoints that perform resource-intensive operations may have lower rate limits than endpoints that perform simple operations.

## Rate Limit Headers

Rate limit information is included in the response headers:

- **X-RateLimit-Limit**: The maximum number of requests allowed in the current time window.
- **X-RateLimit-Remaining**: The number of requests remaining in the current time window.
- **X-RateLimit-Reset**: The time at which the current rate limit window resets, in UTC epoch seconds.

Example:

```http
HTTP/1.1 200 OK
Content-Type: application/json
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 99
X-RateLimit-Reset: 1609459200
```

## Rate Limit Exceeded

If you exceed the rate limit, you will receive a 429 Too Many Requests response with a Retry-After header indicating how many seconds to wait before making another request:

```http
HTTP/1.1 429 Too Many Requests
Content-Type: application/json
Retry-After: 60
```

The response body will contain an error message:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "rate_limit_exceeded",
    "message": "Rate limit exceeded. Please try again later.",
    "details": {
      "retryAfter": 60
    }
  },
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

## Handling Rate Limits

When handling rate limits in your applications, you should implement a retry strategy with exponential backoff:

### Example: JavaScript

```javascript
async function fetchWithRetry(url, options, maxRetries = 3) {
  let retries = 0;

  while (retries < maxRetries) {
    try {
      const response = await fetch(url, options);
      
      if (response.status === 429) {
        // Get retry-after header
        const retryAfter = response.headers.get('Retry-After');
        const retryAfterMs = retryAfter ? parseInt(retryAfter) * 1000 : 60000;
        
        console.log(`Rate limited. Retrying after ${retryAfterMs}ms...`);
        await new Promise(resolve => setTimeout(resolve, retryAfterMs));
        
        retries++;
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
      await new Promise(resolve => setTimeout(resolve, 1000 * Math.pow(2, retries)));
      
      retries++;
    }
  }
}
```

### Example: Python

```python
import requests
import time

def fetch_with_retry(url, headers, max_retries=3):
    retries = 0
    
    while retries < max_retries:
        try:
            response = requests.get(url, headers=headers)
            
            if response.status_code == 429:
                # Get retry-after header
                retry_after = response.headers.get('Retry-After')
                retry_after_seconds = int(retry_after) if retry_after else 60
                
                print(f"Rate limited. Retrying after {retry_after_seconds} seconds...")
                time.sleep(retry_after_seconds)
                
                retries += 1
                continue
            
            response.raise_for_status()
            return response.json()
        except requests.exceptions.RequestException as e:
            if retries == max_retries - 1:
                raise e
            
            print(f"Error: {str(e)}. Retrying...")
            time.sleep(2 ** retries)
            
            retries += 1
```

### Example: C#

```csharp
using System;
using System.Net.Http;
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

    public async Task<T> GetWithRetryAsync<T>(string endpoint, int maxRetries = 3)
    {
        int retries = 0;

        while (retries < maxRetries)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://api.neoservicelayer.org/api/v1/{endpoint}");
                var content = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode == 429)
                {
                    // Get retry-after header
                    if (response.Headers.TryGetValues("Retry-After", out var values) && int.TryParse(values.FirstOrDefault(), out var retryAfter))
                    {
                        Console.WriteLine($"Rate limited. Retrying after {retryAfter} seconds...");
                        await Task.Delay(retryAfter * 1000);
                    }
                    else
                    {
                        Console.WriteLine("Rate limited. Retrying after 60 seconds...");
                        await Task.Delay(60000);
                    }

                    retries++;
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorData = JsonSerializer.Deserialize<ErrorResponse>(content);
                    throw new Exception($"{(int)response.StatusCode} {errorData.Error.Code}: {errorData.Error.Message}");
                }

                var data = JsonSerializer.Deserialize<SuccessResponse<T>>(content);
                return data.Data;
            }
            catch (Exception ex)
            {
                if (retries == maxRetries - 1)
                {
                    throw;
                }

                Console.WriteLine($"Error: {ex.Message}. Retrying...");
                await Task.Delay(1000 * (int)Math.Pow(2, retries));

                retries++;
            }
        }

        throw new Exception("Max retries exceeded.");
    }
}
```

## Best Practices

To avoid hitting rate limits, follow these best practices:

1. **Cache responses**: Cache responses to avoid making unnecessary requests.
2. **Use bulk operations**: Use bulk operations to reduce the number of requests.
3. **Implement retry with exponential backoff**: Implement a retry strategy with exponential backoff to handle rate limit errors.
4. **Monitor rate limit headers**: Monitor the rate limit headers to avoid hitting rate limits.
5. **Distribute requests**: Distribute requests evenly over time to avoid bursts of requests.

## Rate Limit Increase

If you need higher rate limits, you can request a rate limit increase by contacting support:

1. Go to the [Neo Service Layer Portal](https://portal.neoservicelayer.org).
2. Log in to your account.
3. Navigate to the Support section.
4. Click on "Request Rate Limit Increase".
5. Fill in the required information, including the reason for the rate limit increase.
6. Click on "Submit" to submit the request.

Rate limit increases are granted on a case-by-case basis and may require additional verification.

## References

- [Neo Service Layer API](README.md)
- [Neo Service Layer API Endpoints](endpoints.md)
- [Neo Service Layer API Error Handling](error-handling.md)

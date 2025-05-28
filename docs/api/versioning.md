# Neo Service Layer API Versioning

## Overview

The Neo Service Layer API uses versioning to ensure backward compatibility while allowing for new features and improvements. This document describes how API versioning works and how to use different API versions in your applications.

## Versioning Strategy

The Neo Service Layer API uses a semantic versioning strategy with the following format:

```
v{major}
```

For example:

- `v1`: The first major version of the API.
- `v2`: The second major version of the API.

Major version changes indicate breaking changes that are not backward compatible. Minor and patch version changes are backward compatible and do not require a version change in the API URL.

## Specifying API Version

You can specify the API version in the following ways:

### URL Path

The most common way to specify the API version is through the URL path:

```
https://api.neoservicelayer.org/api/v1/randomness/generate
```

In this example, `v1` indicates that you are using version 1 of the API.

### Accept Header

You can also specify the API version through the `Accept` header:

```http
GET /api/randomness/generate HTTP/1.1
Host: api.neoservicelayer.org
Accept: application/json;version=1
X-API-Key: your-api-key
```

### Query Parameter

Alternatively, you can specify the API version through a query parameter:

```
https://api.neoservicelayer.org/api/randomness/generate?version=1
```

## Version Lifecycle

Each API version goes through the following lifecycle:

1. **Preview**: The version is available for testing but may change without notice.
2. **Stable**: The version is stable and will not change in a backward-incompatible way.
3. **Deprecated**: The version is deprecated and will be removed in the future.
4. **Retired**: The version is no longer available.

When a version is deprecated, a deprecation notice will be included in the response headers:

```http
HTTP/1.1 200 OK
Content-Type: application/json
Deprecation: true
Sunset: Sat, 31 Dec 2023 23:59:59 GMT
Link: <https://api.neoservicelayer.org/api/v2/randomness/generate>; rel="successor-version"
```

The `Deprecation` header indicates that the version is deprecated. The `Sunset` header indicates when the version will be retired. The `Link` header provides a link to the successor version.

## Version Compatibility

The Neo Service Layer API follows these compatibility guidelines:

- **Major version changes**: Breaking changes that are not backward compatible.
- **Minor version changes**: New features that are backward compatible.
- **Patch version changes**: Bug fixes that are backward compatible.

When a new major version is released, the previous version will continue to be supported for a period of time to allow for a smooth transition.

## Using Multiple Versions

You may need to use multiple API versions in your applications during a transition period. Here's how to handle this:

### Example: JavaScript

```javascript
class ApiClient {
  constructor(apiKey, version = 1) {
    this.apiKey = apiKey;
    this.version = version;
    this.baseUrl = `https://api.neoservicelayer.org/api/v${version}`;
  }

  async get(endpoint) {
    const response = await fetch(`${this.baseUrl}/${endpoint}`, {
      headers: {
        'X-API-Key': this.apiKey
      }
    });

    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(`${response.status} ${errorData.error.code}: ${errorData.error.message}`);
    }

    return response.json();
  }

  async post(endpoint, data) {
    const response = await fetch(`${this.baseUrl}/${endpoint}`, {
      method: 'POST',
      headers: {
        'X-API-Key': this.apiKey,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(data)
    });

    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(`${response.status} ${errorData.error.code}: ${errorData.error.message}`);
    }

    return response.json();
  }
}

// Use version 1
const apiClientV1 = new ApiClient('your-api-key', 1);
apiClientV1.get('randomness/generate')
  .then(data => console.log(data));

// Use version 2
const apiClientV2 = new ApiClient('your-api-key', 2);
apiClientV2.get('randomness/generate')
  .then(data => console.log(data));
```

### Example: Python

```python
import requests

class ApiClient:
    def __init__(self, api_key, version=1):
        self.api_key = api_key
        self.version = version
        self.base_url = f"https://api.neoservicelayer.org/api/v{version}"

    def get(self, endpoint):
        response = requests.get(f"{self.base_url}/{endpoint}", headers={
            'X-API-Key': self.api_key
        })
        response.raise_for_status()
        return response.json()

    def post(self, endpoint, data):
        response = requests.post(f"{self.base_url}/{endpoint}", headers={
            'X-API-Key': self.api_key,
            'Content-Type': 'application/json'
        }, json=data)
        response.raise_for_status()
        return response.json()

# Use version 1
api_client_v1 = ApiClient('your-api-key', 1)
data = api_client_v1.get('randomness/generate')
print(data)

# Use version 2
api_client_v2 = ApiClient('your-api-key', 2)
data = api_client_v2.get('randomness/generate')
print(data)
```

## Version Migration

When migrating from one API version to another, follow these steps:

1. **Review the changes**: Review the changes between versions to understand what needs to be updated.
2. **Update your code**: Update your code to use the new version.
3. **Test your code**: Test your code to ensure it works with the new version.
4. **Deploy your code**: Deploy your code to production.
5. **Monitor your code**: Monitor your code to ensure it works as expected.

## Version Differences

### v1 to v2

The following changes were made in the transition from v1 to v2:

- **Randomness Service**: Added support for additional random number generation algorithms.
- **Oracle Service**: Added support for additional data sources and transformation options.
- **Key Management Service**: Added support for additional key types and algorithms.
- **Compute Service**: Added support for additional computation types and languages.
- **Storage Service**: Added support for additional storage options and access control mechanisms.
- **Compliance Service**: Added support for additional compliance rules and regulations.
- **Event Subscription Service**: Added support for additional event types and filtering options.

For detailed information about the changes, see the [API Changelog](changelog.md).

## References

- [Neo Service Layer API](README.md)
- [Neo Service Layer API Endpoints](endpoints.md)
- [Neo Service Layer API Changelog](changelog.md)

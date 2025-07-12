# Neo Service Layer API Authentication

## Overview

The Neo Service Layer API requires authentication for all requests. This document describes the authentication methods supported by the API and how to use them.

## Authentication Methods

The Neo Service Layer API supports the following authentication methods:

- **API Key**: A simple API key that is included in the request header.
- **JWT**: JSON Web Tokens for more secure authentication.
- **OAuth 2.0**: OAuth 2.0 for delegated authentication.

## Environment Configuration

Before using the API, ensure your environment is properly configured:

1. **Copy the environment template**:
   ```bash
   cp .env.example .env
   ```

2. **Generate secure credentials** (for production):
   ```bash
   ./scripts/generate-secure-credentials.sh
   ```

3. **Configure JWT settings**:
   ```bash
   JWT_SECRET_KEY=your-secure-key-here
   JWT_ISSUER=neo-service-layer
   JWT_AUDIENCE=neo-service-layer-clients
   JWT_EXPIRATION_MINUTES=60
   ```

## API Key Authentication

API key authentication is the simplest authentication method. It involves including an API key in the request header.

### Obtaining an API Key

To obtain an API key, you need to register for an account on the Neo Service Layer portal:

1. Go to the [Neo Service Layer Portal](https://portal.neoservicelayer.org).
2. Sign up for an account or log in to your existing account.
3. Navigate to the API Keys section.
4. Click on "Create API Key".
5. Give your API key a name and select the permissions you want to grant.
6. Click on "Create" to generate the API key.
7. Copy the API key and store it securely. The API key will only be shown once.

### Using an API Key

To use an API key, include it in the `X-API-Key` header of your request:

```http
GET /api/v1/randomness/generate HTTP/1.1
Host: api.neoservicelayer.org
X-API-Key: your-api-key
```

Example using cURL:

```bash
curl -X GET "https://api.neoservicelayer.org/api/v1/randomness/generate" \
     -H "X-API-Key: your-api-key"
```

Example using JavaScript:

```javascript
fetch('https://api.neoservicelayer.org/api/v1/randomness/generate', {
  headers: {
    'X-API-Key': 'your-api-key'
  }
})
.then(response => response.json())
.then(data => console.log(data));
```

Example using Python:

```python
import requests

url = "https://api.neoservicelayer.org/api/v1/randomness/generate"
headers = {
    "X-API-Key": "your-api-key"
}

response = requests.get(url, headers=headers)
data = response.json()
print(data)
```

### API Key Security

API keys should be kept secure and not shared with others. If your API key is compromised, you should revoke it and create a new one.

To revoke an API key:

1. Go to the [Neo Service Layer Portal](https://portal.neoservicelayer.org).
2. Log in to your account.
3. Navigate to the API Keys section.
4. Find the API key you want to revoke.
5. Click on "Revoke" to revoke the API key.

## JWT Authentication

JWT (JSON Web Token) authentication provides a more secure authentication method than API keys. It involves including a JWT token in the request header.

### JWT Configuration

The Neo Service Layer uses standardized JWT configuration across all environments:

- **Issuer**: `neo-service-layer` (configurable via `JWT_ISSUER`)
- **Audience**: `neo-service-layer-clients` (configurable via `JWT_AUDIENCE`)
- **Expiration**: 60 minutes by default (configurable via `JWT_EXPIRATION_MINUTES`)
- **Refresh Token Expiration**: 7 days (10080 minutes)
- **Clock Skew**: 5 minutes tolerance
- **Validation**: All validation options enabled (issuer, audience, lifetime, signature)

### Obtaining a JWT Token

To obtain a JWT token, you need to authenticate with the Neo Service Layer authentication service:

```http
POST /api/v1/auth/login HTTP/1.1
Host: api.neoservicelayer.org
Content-Type: application/json

{
  "username": "your-username",
  "password": "your-password"
}
```

The response will include a JWT token:

```json
{
  "success": true,
  "data": {
    "token": "your-jwt-token",
    "refreshToken": "your-refresh-token",
    "expiresIn": 3600,
    "tokenType": "Bearer"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

### Using a JWT Token

To use a JWT token, include it in the `Authorization` header of your request:

```http
GET /api/v1/randomness/generate HTTP/1.1
Host: api.neoservicelayer.org
Authorization: Bearer your-jwt-token
```

Example using cURL:

```bash
curl -X GET "https://api.neoservicelayer.org/api/v1/randomness/generate" \
     -H "Authorization: Bearer your-jwt-token"
```

Example using JavaScript:

```javascript
fetch('https://api.neoservicelayer.org/api/v1/randomness/generate', {
  headers: {
    'Authorization': 'Bearer your-jwt-token'
  }
})
.then(response => response.json())
.then(data => console.log(data));
```

Example using Python:

```python
import requests

url = "https://api.neoservicelayer.org/api/v1/randomness/generate"
headers = {
    "Authorization": "Bearer your-jwt-token"
}

response = requests.get(url, headers=headers)
data = response.json()
print(data)
```

### JWT Token Security

JWT tokens have an expiration time, after which they are no longer valid. If your JWT token expires, you need to obtain a new one.

**Security Best Practices:**
- Store tokens securely (never in local storage for web apps)
- Use HTTPS for all API communications
- Implement token refresh before expiration
- Revoke tokens on logout
- Monitor for suspicious token usage

**Token Validation:**
All JWT tokens are validated for:
- Valid signature
- Correct issuer
- Correct audience
- Not expired
- Required claims present

To refresh a JWT token:

```http
POST /api/v1/auth/refresh HTTP/1.1
Host: api.neoservicelayer.org
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

The response will include a new JWT token:

```json
{
  "success": true,
  "data": {
    "token": "your-new-jwt-token",
    "refreshToken": "your-new-refresh-token",
    "expiresIn": 3600,
    "tokenType": "Bearer"
  },
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

## OAuth 2.0 Authentication

OAuth 2.0 authentication provides a way for users to grant third-party applications access to their resources without sharing their credentials. It involves obtaining an access token from an OAuth 2.0 server and including it in the request header.

### Registering an OAuth 2.0 Client

To use OAuth 2.0 authentication, you need to register your application as an OAuth 2.0 client:

1. Go to the [Neo Service Layer Portal](https://portal.neoservicelayer.org).
2. Log in to your account.
3. Navigate to the OAuth 2.0 Clients section.
4. Click on "Register Client".
5. Fill in the required information, including the client name, redirect URI, and allowed scopes.
6. Click on "Register" to register the client.
7. Copy the client ID and client secret and store them securely. The client secret will only be shown once.

### Obtaining an OAuth 2.0 Access Token

To obtain an OAuth 2.0 access token, you need to redirect the user to the Neo Service Layer authorization server:

```
https://auth.neoservicelayer.org/oauth2/authorize?
  response_type=code&
  client_id=your-client-id&
  redirect_uri=your-redirect-uri&
  scope=read+write&
  state=random-state
```

The user will be prompted to log in and grant access to your application. If the user grants access, they will be redirected to your redirect URI with an authorization code:

```
https://your-redirect-uri?code=authorization-code&state=random-state
```

You can then exchange the authorization code for an access token:

```http
POST /oauth2/token HTTP/1.1
Host: auth.neoservicelayer.org
Content-Type: application/x-www-form-urlencoded
Authorization: Basic base64(your-client-id:your-client-secret)

grant_type=authorization_code&
code=authorization-code&
redirect_uri=your-redirect-uri
```

The response will include an access token:

```json
{
  "access_token": "your-access-token",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "your-refresh-token",
  "scope": "read write"
}
```

### Using an OAuth 2.0 Access Token

To use an OAuth 2.0 access token, include it in the `Authorization` header of your request:

```http
GET /api/v1/randomness/generate HTTP/1.1
Host: api.neoservicelayer.org
Authorization: Bearer your-access-token
```

Example using cURL:

```bash
curl -X GET "https://api.neoservicelayer.org/api/v1/randomness/generate" \
     -H "Authorization: Bearer your-access-token"
```

Example using JavaScript:

```javascript
fetch('https://api.neoservicelayer.org/api/v1/randomness/generate', {
  headers: {
    'Authorization': 'Bearer your-access-token'
  }
})
.then(response => response.json())
.then(data => console.log(data));
```

Example using Python:

```python
import requests

url = "https://api.neoservicelayer.org/api/v1/randomness/generate"
headers = {
    "Authorization": "Bearer your-access-token"
}

response = requests.get(url, headers=headers)
data = response.json()
print(data)
```

### OAuth 2.0 Access Token Security

OAuth 2.0 access tokens have an expiration time, after which they are no longer valid. If your access token expires, you can use the refresh token to obtain a new access token:

```http
POST /oauth2/token HTTP/1.1
Host: auth.neoservicelayer.org
Content-Type: application/x-www-form-urlencoded
Authorization: Basic base64(your-client-id:your-client-secret)

grant_type=refresh_token&
refresh_token=your-refresh-token
```

The response will include a new access token:

```json
{
  "access_token": "your-new-access-token",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "your-new-refresh-token",
  "scope": "read write"
}
```

## References

- [Neo Service Layer API](README.md)
- [Neo Service Layer API Endpoints](endpoints.md)
- [OAuth 2.0 Specification](https://oauth.net/2/)

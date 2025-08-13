# Neo Service Layer - Authentication API Documentation

## Overview

The Neo Service Layer provides a comprehensive authentication and authorization system with support for JWT tokens, refresh tokens, multi-factor authentication (MFA), rate limiting, and extensive security features.

## Table of Contents

1. [Authentication Overview](#authentication-overview)
2. [API Endpoints](#api-endpoints)
3. [Security Features](#security-features)
4. [Token Management](#token-management)
5. [Rate Limiting](#rate-limiting)
6. [Monitoring & Health Checks](#monitoring--health-checks)
7. [Error Handling](#error-handling)

## Authentication Overview

### Authentication Flow

1. **User Registration**: Create a new user account
2. **Email Verification**: Verify email address via token
3. **Login**: Authenticate with username/password
4. **MFA Verification**: Complete two-factor authentication (if enabled)
5. **Token Usage**: Use access token for API requests
6. **Token Refresh**: Refresh expired access tokens
7. **Logout**: Revoke tokens and end session

### Token Types

- **Access Token**: Short-lived JWT (15 minutes default)
- **Refresh Token**: Long-lived token (30 days default)
- **Verification Token**: Email verification (24 hours)
- **Password Reset Token**: Password recovery (1 hour)
- **MFA Token**: Two-factor code (5 minutes)

## API Endpoints

### Authentication Controller

#### Register User
```http
POST /api/v1/authentication/register
Content-Type: application/json

{
  "username": "string",
  "email": "user@example.com",
  "password": "string",
  "firstName": "string",
  "lastName": "string",
  "phoneNumber": "string"
}

Response: 201 Created
{
  "success": true,
  "data": {
    "userId": "string",
    "username": "string",
    "email": "string",
    "requiresEmailVerification": true
  },
  "message": "Registration successful"
}
```

#### Login
```http
POST /api/v1/authentication/login
Content-Type: application/json

{
  "username": "string",
  "password": "string",
  "rememberMe": false
}

Response: 200 OK
{
  "success": true,
  "data": {
    "accessToken": "string",
    "refreshToken": "string",
    "expiresIn": 900,
    "tokenType": "Bearer",
    "requiresMfa": false
  }
}
```

#### Refresh Token
```http
POST /api/v1/authentication/refresh
Content-Type: application/json

{
  "refreshToken": "string"
}

Response: 200 OK
{
  "success": true,
  "data": {
    "accessToken": "string",
    "refreshToken": "string",
    "expiresIn": 900
  }
}
```

#### Logout
```http
POST /api/v1/authentication/logout
Authorization: Bearer {access_token}

Response: 200 OK
{
  "success": true,
  "message": "Logout successful"
}
```

#### Verify Email
```http
GET /api/v1/authentication/verify-email?token={verification_token}

Response: 200 OK
{
  "success": true,
  "message": "Email verified successfully"
}
```

#### Request Password Reset
```http
POST /api/v1/authentication/password/request-reset
Content-Type: application/json

{
  "email": "user@example.com"
}

Response: 200 OK
{
  "success": true,
  "message": "Password reset instructions sent to email"
}
```

#### Reset Password
```http
POST /api/v1/authentication/password/reset
Content-Type: application/json

{
  "token": "string",
  "newPassword": "string"
}

Response: 200 OK
{
  "success": true,
  "message": "Password reset successful"
}
```

#### Change Password
```http
POST /api/v1/authentication/password/change
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "currentPassword": "string",
  "newPassword": "string"
}

Response: 200 OK
{
  "success": true,
  "message": "Password changed successfully"
}
```

### MFA Endpoints

#### Enable MFA
```http
POST /api/v1/authentication/mfa/enable
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "method": "totp|email|sms"
}

Response: 200 OK
{
  "success": true,
  "data": {
    "secret": "string",
    "qrCode": "string",
    "backupCodes": ["string"]
  }
}
```

#### Verify MFA
```http
POST /api/v1/authentication/mfa/verify
Content-Type: application/json

{
  "sessionId": "string",
  "code": "string",
  "method": "totp|email|sms"
}

Response: 200 OK
{
  "success": true,
  "data": {
    "accessToken": "string",
    "refreshToken": "string"
  }
}
```

### User Management (Admin Only)

#### List Users
```http
GET /api/v1/users?page=1&pageSize=20&search=john&isActive=true&role=user
Authorization: Bearer {admin_token}

Response: 200 OK
{
  "success": true,
  "data": [
    {
      "id": "string",
      "username": "string",
      "email": "string",
      "firstName": "string",
      "lastName": "string",
      "isActive": true,
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 100,
  "totalPages": 5
}
```

#### Get User Details
```http
GET /api/v1/users/{userId}
Authorization: Bearer {admin_token}

Response: 200 OK
{
  "success": true,
  "data": {
    "id": "string",
    "username": "string",
    "email": "string",
    "firstName": "string",
    "lastName": "string",
    "phoneNumber": "string",
    "emailVerified": true,
    "mfaEnabled": false,
    "isActive": true,
    "isLocked": false,
    "roles": ["user"],
    "lastLoginAt": "2024-01-01T00:00:00Z",
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

#### Create User
```http
POST /api/v1/users
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "username": "string",
  "email": "string",
  "password": "string",
  "firstName": "string",
  "lastName": "string",
  "phoneNumber": "string",
  "roles": ["user"],
  "sendVerificationEmail": true,
  "requirePasswordChange": false
}

Response: 201 Created
```

#### Update User
```http
PUT /api/v1/users/{userId}
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "email": "string",
  "firstName": "string",
  "lastName": "string",
  "phoneNumber": "string",
  "isActive": true,
  "requiresPasswordChange": false
}

Response: 200 OK
```

#### Delete User
```http
DELETE /api/v1/users/{userId}
Authorization: Bearer {admin_token}

Response: 204 No Content
```

#### Lock User Account
```http
POST /api/v1/users/{userId}/lock
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "reason": "string",
  "lockUntil": "2024-12-31T23:59:59Z"
}

Response: 200 OK
```

#### Unlock User Account
```http
POST /api/v1/users/{userId}/unlock
Authorization: Bearer {admin_token}

Response: 200 OK
```

#### Admin Password Reset
```http
POST /api/v1/users/{userId}/reset-password
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "newPassword": "string",
  "generatePassword": false
}

Response: 200 OK
{
  "success": true,
  "data": {
    "temporaryPassword": "string",
    "requiresPasswordChange": true
  }
}
```

### Role Management

#### Get User Roles
```http
GET /api/v1/users/{userId}/roles
Authorization: Bearer {admin_token}

Response: 200 OK
{
  "success": true,
  "data": ["user", "admin"]
}
```

#### Add User to Role
```http
POST /api/v1/users/{userId}/roles
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "role": "admin"
}

Response: 200 OK
```

#### Remove User from Role
```http
DELETE /api/v1/users/{userId}/roles/{role}
Authorization: Bearer {admin_token}

Response: 204 No Content
```

### Session Management

#### Get User Sessions
```http
GET /api/v1/users/{userId}/sessions
Authorization: Bearer {admin_token}

Response: 200 OK
{
  "success": true,
  "data": [
    {
      "sessionId": "string",
      "createdAt": "2024-01-01T00:00:00Z",
      "lastActivityAt": "2024-01-01T00:00:00Z",
      "ipAddress": "192.168.1.1",
      "userAgent": "string",
      "isActive": true
    }
  ]
}
```

#### Revoke User Sessions
```http
POST /api/v1/users/{userId}/revoke-sessions
Authorization: Bearer {admin_token}

Response: 200 OK
{
  "success": true,
  "message": "All user sessions have been revoked"
}
```

## Security Features

### JWT Token Security
- **Signing Algorithm**: HMAC SHA256
- **Token Expiry**: 15 minutes (access), 30 days (refresh)
- **Token Blacklisting**: Immediate revocation capability
- **Refresh Token Rotation**: One-time use tokens
- **Session Tracking**: Unique session IDs

### Password Security
- **Hashing Algorithm**: PBKDF2 with 100,000 iterations
- **Salt**: Random 128-bit salt per password
- **Minimum Requirements**: 8 characters minimum
- **Password History**: Prevent reuse (configurable)
- **Force Change**: Admin-initiated password changes

### Multi-Factor Authentication
- **TOTP**: Time-based one-time passwords
- **Email**: Code sent to verified email
- **SMS**: Code sent to verified phone
- **Backup Codes**: Recovery codes for lost devices

### Account Security
- **Email Verification**: Required for new accounts
- **Account Lockout**: After failed attempts
- **IP Tracking**: Login location monitoring
- **Device Fingerprinting**: Detect new devices
- **Security Alerts**: Email notifications for security events

## Rate Limiting

### Default Limits
- **General API**: 60 requests/minute, 1000 requests/hour
- **Login**: 5 requests/minute, 20 requests/hour
- **Registration**: 2 requests/minute, 10 requests/hour
- **Password Reset**: 2 requests/minute, 5 requests/hour

### Rate Limit Headers
```http
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1704067200
Retry-After: 30
```

### Rate Limit Response
```http
429 Too Many Requests
{
  "success": false,
  "message": "Rate limit exceeded",
  "data": {
    "limit": 60,
    "remaining": 0,
    "resetAt": "2024-01-01T00:00:00Z",
    "retryAfter": 30
  }
}
```

## Monitoring & Health Checks

### Health Check Endpoint
```http
GET /api/v1/monitoring/authentication/health

Response: 200 OK (Healthy) | 503 Service Unavailable (Unhealthy)
{
  "status": "Healthy|Degraded|Unhealthy",
  "totalDuration": "00:00:00.123",
  "entries": [
    {
      "name": "JwtConfiguration",
      "status": "Healthy",
      "description": "JWT configuration is valid",
      "duration": "00:00:00.001"
    }
  ]
}
```

### Metrics Endpoints

#### Authentication Metrics
```http
GET /api/v1/monitoring/authentication/metrics
Authorization: Bearer {admin_token}

Response: 200 OK
{
  "loginMetrics": {
    "totalAttempts": 1000,
    "successfulLogins": 850,
    "failedLogins": 150,
    "averageLoginDuration": 123.45
  },
  "tokenMetrics": {
    "accessTokensGenerated": 850,
    "refreshTokensGenerated": 850,
    "tokenValidations": 5000,
    "tokenValidationFailures": 50
  },
  "securityMetrics": {
    "accountLockouts": 10,
    "rateLimitHits": 25,
    "passwordResets": 15,
    "emailVerifications": 100
  }
}
```

#### Dashboard Data
```http
GET /api/v1/monitoring/authentication/dashboard
Authorization: Bearer {admin_token}

Response: 200 OK
{
  "timestamp": "2024-01-01T00:00:00Z",
  "healthStatus": "Healthy",
  "activeUsers": 150,
  "activeSessions": 200,
  "loginActivity": {
    "last5Minutes": 10,
    "last15Minutes": 25,
    "lastHour": 85,
    "failureRate": 0.15
  },
  "securityAlerts": {
    "critical": 0,
    "high": 2,
    "medium": 5,
    "low": 10
  }
}
```

#### Prometheus Metrics Export
```http
GET /api/v1/monitoring/authentication/metrics/export

Response: 200 OK
Content-Type: text/plain

# HELP auth_login_total Total number of login attempts
# TYPE auth_login_total counter
auth_login_total{status="success"} 850
auth_login_total{status="failure"} 150

# HELP auth_token_generated_total Total number of tokens generated
# TYPE auth_token_generated_total counter
auth_token_generated_total{type="access"} 850
auth_token_generated_total{type="refresh"} 850
```

## Error Handling

### Standard Error Response
```json
{
  "success": false,
  "message": "Error description",
  "data": {
    "field": ["Validation error message"]
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### Common Error Codes

| HTTP Status | Error Type | Description |
|------------|------------|-------------|
| 400 | Bad Request | Invalid input or malformed request |
| 401 | Unauthorized | Invalid or missing authentication |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource not found |
| 409 | Conflict | Resource already exists |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |
| 503 | Service Unavailable | Service temporarily unavailable |

### Authentication-Specific Errors

| Error Code | Message | Description |
|------------|---------|-------------|
| AUTH001 | Invalid credentials | Username or password incorrect |
| AUTH002 | Account locked | Account temporarily locked |
| AUTH003 | Email not verified | Email verification required |
| AUTH004 | MFA required | Two-factor authentication required |
| AUTH005 | Invalid token | Token expired or invalid |
| AUTH006 | Session expired | Session has expired |
| AUTH007 | Password expired | Password change required |
| AUTH008 | Invalid MFA code | MFA verification failed |
| AUTH009 | Rate limit exceeded | Too many requests |
| AUTH010 | Account disabled | Account has been disabled |

## Configuration

### Environment Variables

```bash
# JWT Configuration
Authentication__JwtSecret=your-secret-key-min-32-chars
Authentication__Issuer=NeoServiceLayer
Authentication__Audience=NeoServiceLayer
Authentication__AccessTokenExpiryMinutes=15
Authentication__RefreshTokenExpiryDays=30

# Email Configuration
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__Username=your-email@gmail.com
Email__Smtp__Password=your-app-password
Email__Smtp__EnableSsl=true
Email__From__Address=noreply@neoservicelayer.com
Email__From__Name=Neo Service Layer

# Rate Limiting
RateLimit__Enabled=true
RateLimit__DefaultRequestsPerMinute=60
RateLimit__DefaultRequestsPerHour=1000
RateLimit__DefaultBurstSize=10

# Security
Authentication__RequireEmailVerification=true
Authentication__EnableMfa=true
Authentication__MaxFailedLoginAttempts=5
Authentication__LockoutDurationMinutes=30
Authentication__PasswordExpiryDays=90
Authentication__RequirePasswordHistory=true
Authentication__PasswordHistoryCount=5
```

### appsettings.json

```json
{
  "Authentication": {
    "JwtSecret": "your-secret-key",
    "Issuer": "NeoServiceLayer",
    "Audience": "NeoServiceLayer",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 30,
    "EmailVerificationExpiryHours": 24,
    "PasswordResetExpiryMinutes": 60,
    "MfaTokenExpiryMinutes": 5
  },
  "RateLimit": {
    "Enabled": true,
    "DefaultRequestsPerMinute": 60,
    "DefaultRequestsPerHour": 1000,
    "DefaultBurstSize": 10
  }
}
```

## Integration Examples

### JavaScript/TypeScript

```typescript
// Login
async function login(username: string, password: string): Promise<AuthResponse> {
  const response = await fetch('https://api.neoservicelayer.com/api/v1/authentication/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ username, password }),
  });
  
  if (!response.ok) {
    throw new Error('Login failed');
  }
  
  const data = await response.json();
  
  // Store tokens securely
  localStorage.setItem('accessToken', data.data.accessToken);
  localStorage.setItem('refreshToken', data.data.refreshToken);
  
  return data.data;
}

// Authenticated Request
async function getProfile(): Promise<UserProfile> {
  const token = localStorage.getItem('accessToken');
  
  const response = await fetch('https://api.neoservicelayer.com/api/v1/profile', {
    headers: {
      'Authorization': `Bearer ${token}`,
    },
  });
  
  if (response.status === 401) {
    // Token expired, try to refresh
    await refreshToken();
    return getProfile();
  }
  
  return response.json();
}

// Refresh Token
async function refreshToken(): Promise<void> {
  const refreshToken = localStorage.getItem('refreshToken');
  
  const response = await fetch('https://api.neoservicelayer.com/api/v1/authentication/refresh', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ refreshToken }),
  });
  
  if (!response.ok) {
    // Refresh failed, redirect to login
    window.location.href = '/login';
    return;
  }
  
  const data = await response.json();
  localStorage.setItem('accessToken', data.data.accessToken);
  localStorage.setItem('refreshToken', data.data.refreshToken);
}
```

### C#/.NET

```csharp
public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenStorage _tokenStorage;

    public async Task<AuthResponse> LoginAsync(string username, string password)
    {
        var request = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var response = await _httpClient.PostAsJsonAsync(
            "api/v1/authentication/login", 
            request);

        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        
        // Store tokens
        await _tokenStorage.StoreTokensAsync(
            authResponse.Data.AccessToken,
            authResponse.Data.RefreshToken);

        return authResponse.Data;
    }

    public async Task<T> AuthenticatedRequestAsync<T>(string endpoint)
    {
        var token = await _tokenStorage.GetAccessTokenAsync();
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.GetAsync(endpoint);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await RefreshTokenAsync();
            return await AuthenticatedRequestAsync<T>(endpoint);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }
}
```

### Python

```python
import requests
from datetime import datetime, timedelta

class AuthenticationClient:
    def __init__(self, base_url):
        self.base_url = base_url
        self.access_token = None
        self.refresh_token = None
        self.token_expiry = None
    
    def login(self, username, password):
        response = requests.post(
            f"{self.base_url}/api/v1/authentication/login",
            json={"username": username, "password": password}
        )
        response.raise_for_status()
        
        data = response.json()["data"]
        self.access_token = data["accessToken"]
        self.refresh_token = data["refreshToken"]
        self.token_expiry = datetime.now() + timedelta(seconds=data["expiresIn"])
        
        return data
    
    def authenticated_request(self, method, endpoint, **kwargs):
        if self.token_expiry and datetime.now() >= self.token_expiry:
            self.refresh_tokens()
        
        headers = kwargs.get("headers", {})
        headers["Authorization"] = f"Bearer {self.access_token}"
        kwargs["headers"] = headers
        
        response = requests.request(method, f"{self.base_url}{endpoint}", **kwargs)
        
        if response.status_code == 401:
            self.refresh_tokens()
            return self.authenticated_request(method, endpoint, **kwargs)
        
        return response
    
    def refresh_tokens(self):
        response = requests.post(
            f"{self.base_url}/api/v1/authentication/refresh",
            json={"refreshToken": self.refresh_token}
        )
        response.raise_for_status()
        
        data = response.json()["data"]
        self.access_token = data["accessToken"]
        self.refresh_token = data["refreshToken"]
        self.token_expiry = datetime.now() + timedelta(seconds=data["expiresIn"])
```

## Best Practices

### Security Best Practices

1. **Always use HTTPS** in production
2. **Store tokens securely** (HttpOnly cookies or secure storage)
3. **Implement token refresh** before expiry
4. **Enable MFA** for sensitive accounts
5. **Monitor failed login attempts**
6. **Implement rate limiting** on all endpoints
7. **Log security events** for audit trails
8. **Regular security audits** and penetration testing

### Implementation Guidelines

1. **Token Storage**:
   - Web: HttpOnly, Secure, SameSite cookies
   - Mobile: Secure keychain/keystore
   - Desktop: OS credential manager

2. **Error Handling**:
   - Don't expose sensitive information
   - Log security events
   - Implement retry logic with exponential backoff

3. **Session Management**:
   - Implement idle timeout
   - Allow users to view active sessions
   - Provide session revocation

4. **Password Policy**:
   - Minimum 8 characters
   - Require complexity
   - Prevent common passwords
   - Implement password history

## Support

For additional support or questions:
- Documentation: https://docs.neoservicelayer.com
- API Status: https://status.neoservicelayer.com
- Support: support@neoservicelayer.com
# Authentication API Documentation

## Overview

The Neo Service Layer Authentication API provides comprehensive user authentication and authorization services including user registration, login, multi-factor authentication (MFA), password management, and session management.

## Base URL

```
https://api.neoservicelayer.com/api/auth
```

## Authentication

Most endpoints require authentication via JWT Bearer token in the Authorization header:

```
Authorization: Bearer <access_token>
```

## Endpoints

### 1. User Registration

**POST** `/register`

Register a new user account.

#### Request Body

```json
{
  "username": "string",
  "email": "string",
  "password": "string",
  "confirmPassword": "string",
  "firstName": "string",
  "lastName": "string",
  "phoneNumber": "string",
  "acceptTerms": true
}
```

#### Response

**201 Created**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "string",
  "email": "string",
  "message": "Registration successful. Please check your email to verify your account.",
  "emailVerificationRequired": true
}
```

**400 Bad Request**
```json
{
  "error": "registration_failed",
  "message": "Username or email already exists"
}
```

### 2. User Login

**POST** `/login`

Authenticate user and receive JWT tokens.

#### Request Body

```json
{
  "username": "string",
  "password": "string",
  "twoFactorCode": "string" // Optional, required if MFA is enabled
}
```

#### Response

**200 OK**
```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "tokenType": "Bearer",
  "expiresIn": 900,
  "username": "string",
  "email": "string",
  "roles": ["User"]
}
```

**202 Accepted** (MFA Required)
```json
{
  "requiresTwoFactor": true,
  "message": "Two-factor authentication required"
}
```

**401 Unauthorized**
```json
{
  "error": "invalid_credentials",
  "message": "Invalid username or password"
}
```

**403 Forbidden**
```json
{
  "error": "account_locked",
  "message": "Account is locked"
}
```

**429 Too Many Requests**
```json
{
  "error": "rate_limit_exceeded",
  "message": "Too many login attempts. Please try again later."
}
```

### 3. Email Verification

**GET** `/verify-email?token={token}`

Verify email address with verification token.

#### Query Parameters

- `token` (required): Email verification token

#### Response

**200 OK**
```json
{
  "message": "Email successfully verified. You can now log in."
}
```

**400 Bad Request**
```json
{
  "error": "verification_failed",
  "message": "Invalid or expired token"
}
```

### 4. Password Reset Request

**POST** `/forgot-password`

Request a password reset email.

#### Request Body

```json
{
  "email": "string"
}
```

#### Response

**200 OK**
```json
{
  "message": "If an account exists with this email, a password reset link has been sent."
}
```

> Note: Always returns 200 OK to prevent email enumeration attacks.

### 5. Complete Password Reset

**POST** `/reset-password`

Reset password using reset token.

#### Request Body

```json
{
  "token": "string",
  "newPassword": "string",
  "confirmPassword": "string"
}
```

#### Response

**200 OK**
```json
{
  "message": "Password successfully reset. You can now log in with your new password."
}
```

**400 Bad Request**
```json
{
  "error": "reset_failed",
  "message": "Invalid or expired token"
}
```

### 6. Refresh Access Token

**POST** `/refresh`

Get new access token using refresh token.

#### Request Body

```json
{
  "refreshToken": "string"
}
```

#### Response

**200 OK**
```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "tokenType": "Bearer",
  "expiresIn": 900
}
```

**401 Unauthorized**
```json
{
  "error": "invalid_refresh_token",
  "message": "Invalid or expired refresh token"
}
```

### 7. Logout

**POST** `/logout`

Logout and revoke current session.

🔒 **Requires Authentication**

#### Response

**200 OK**
```json
{
  "message": "Successfully logged out"
}
```

### 8. Get User Profile

**GET** `/user/profile`

Get current user profile information.

🔒 **Requires Authentication**

#### Response

**200 OK**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "string",
  "email": "string",
  "emailVerified": true,
  "twoFactorEnabled": false,
  "roles": ["User"],
  "createdAt": "2024-01-01T00:00:00Z",
  "lastLoginAt": "2024-01-15T10:30:00Z"
}
```

## Multi-Factor Authentication (MFA)

### 9. Enable MFA

**POST** `/mfa/enable`

Enable two-factor authentication.

🔒 **Requires Authentication**

#### Request Body

```json
{
  "type": "totp" // Options: "totp", "sms", "email"
}
```

#### Response

**200 OK**
```json
{
  "type": "totp",
  "secret": "JBSWY3DPEHPK3PXP",
  "qrCodeUrl": "otpauth://totp/NeoServiceLayer:username?secret=JBSWY3DPEHPK3PXP&issuer=NeoServiceLayer",
  "backupCodes": [
    "ABCD1234",
    "EFGH5678",
    "IJKL9012",
    "MNOP3456",
    "QRST7890",
    "UVWX1234",
    "YZAB5678",
    "CDEF9012"
  ],
  "message": "MFA has been enabled. Please save your backup codes in a safe place."
}
```

### 10. Verify MFA Setup

**POST** `/mfa/verify`

Verify MFA setup with authentication code.

🔒 **Requires Authentication**

#### Request Body

```json
{
  "code": "123456"
}
```

#### Response

**200 OK**
```json
{
  "message": "MFA successfully verified and activated"
}
```

**400 Bad Request**
```json
{
  "error": "invalid_code",
  "message": "Invalid verification code"
}
```

### 11. Disable MFA

**POST** `/mfa/disable`

Disable two-factor authentication.

🔒 **Requires Authentication**

#### Request Body

```json
{
  "password": "string"
}
```

#### Response

**200 OK**
```json
{
  "message": "MFA has been disabled"
}
```

### 12. Generate Backup Codes

**POST** `/mfa/backup-codes`

Generate new backup codes for MFA.

🔒 **Requires Authentication**

#### Response

**200 OK**
```json
{
  "backupCodes": [
    "NEWC1234",
    "CODE5678",
    "BACK9012",
    "UPCO3456",
    "DESE7890",
    "CURE1234",
    "AGEN5678",
    "ERAT9012"
  ],
  "message": "New backup codes generated. Please save them in a safe place. Old codes are now invalid."
}
```

## Password Requirements

Passwords must meet the following requirements:
- Minimum 12 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character

## Rate Limiting

The API implements rate limiting to prevent abuse:
- Login attempts: 5 per 15 minutes per IP/username
- Registration: 3 per hour per IP
- Password reset: 3 per hour per email

## Security Features

1. **JWT Token Security**
   - Access tokens expire in 15 minutes
   - Refresh tokens expire in 30 days
   - Tokens can be revoked/blacklisted

2. **Account Protection**
   - Account lockout after 5 failed login attempts
   - 30-minute lockout duration
   - Email verification required for new accounts

3. **Session Management**
   - Device tracking for sessions
   - Ability to revoke individual or all sessions
   - Session expiry management

4. **Multi-Factor Authentication**
   - TOTP (Time-based One-Time Password)
   - SMS-based codes
   - Email-based codes
   - Backup codes for recovery

## Error Codes

| Code | Description |
|------|-------------|
| `invalid_credentials` | Username or password incorrect |
| `account_locked` | Account locked due to failed attempts |
| `email_not_verified` | Email verification required |
| `rate_limit_exceeded` | Too many requests |
| `mfa_required` | Two-factor authentication required |
| `invalid_token` | Token is invalid or expired |
| `registration_failed` | Registration failed |
| `verification_failed` | Email verification failed |
| `reset_failed` | Password reset failed |
| `invalid_refresh_token` | Refresh token invalid |
| `invalid_code` | MFA code invalid |
| `mfa_setup_failed` | MFA setup failed |
| `mfa_disable_failed` | MFA disable failed |
| `internal_error` | Server error occurred |

## HTTP Status Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Success |
| 201 | Created |
| 202 | Accepted (MFA required) |
| 400 | Bad Request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 429 | Too Many Requests |
| 500 | Internal Server Error |

## Examples

### Example: Complete Registration and Login Flow

1. **Register new user**
```bash
curl -X POST https://api.neoservicelayer.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "email": "john@example.com",
    "password": "SecurePassword123!",
    "confirmPassword": "SecurePassword123!",
    "firstName": "John",
    "lastName": "Doe",
    "acceptTerms": true
  }'
```

2. **Verify email** (user clicks link in email)
```bash
curl -X GET "https://api.neoservicelayer.com/api/auth/verify-email?token=verification_token_here"
```

3. **Login**
```bash
curl -X POST https://api.neoservicelayer.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "password": "SecurePassword123!"
  }'
```

4. **Use access token for authenticated requests**
```bash
curl -X GET https://api.neoservicelayer.com/api/auth/user/profile \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Example: Enable MFA

1. **Enable TOTP MFA**
```bash
curl -X POST https://api.neoservicelayer.com/api/auth/mfa/enable \
  -H "Authorization: Bearer <access_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "type": "totp"
  }'
```

2. **Scan QR code with authenticator app and verify**
```bash
curl -X POST https://api.neoservicelayer.com/api/auth/mfa/verify \
  -H "Authorization: Bearer <access_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "123456"
  }'
```

3. **Login with MFA**
```bash
curl -X POST https://api.neoservicelayer.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "password": "SecurePassword123!",
    "twoFactorCode": "123456"
  }'
```

## SDK Examples

### JavaScript/TypeScript

```typescript
import { AuthenticationClient } from '@neoservicelayer/auth-sdk';

const authClient = new AuthenticationClient({
  baseUrl: 'https://api.neoservicelayer.com'
});

// Register
const registration = await authClient.register({
  username: 'johndoe',
  email: 'john@example.com',
  password: 'SecurePassword123!',
  confirmPassword: 'SecurePassword123!'
});

// Login
const loginResult = await authClient.login({
  username: 'johndoe',
  password: 'SecurePassword123!'
});

// Use access token
authClient.setAccessToken(loginResult.accessToken);

// Get profile
const profile = await authClient.getProfile();
```

### C#/.NET

```csharp
using NeoServiceLayer.SDK.Authentication;

var authClient = new AuthenticationClient("https://api.neoservicelayer.com");

// Register
var registration = await authClient.RegisterAsync(new RegisterRequest
{
    Username = "johndoe",
    Email = "john@example.com",
    Password = "SecurePassword123!",
    ConfirmPassword = "SecurePassword123!"
});

// Login
var loginResult = await authClient.LoginAsync(new LoginRequest
{
    Username = "johndoe",
    Password = "SecurePassword123!"
});

// Set access token
authClient.SetBearerToken(loginResult.AccessToken);

// Get profile
var profile = await authClient.GetProfileAsync();
```

## Support

For support, please contact:
- Email: support@neoservicelayer.com
- Documentation: https://docs.neoservicelayer.com
- GitHub Issues: https://github.com/neoservicelayer/api/issues
# Security Policy

## Supported Versions

Currently supported versions for security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

The Neo Service Layer team takes security vulnerabilities seriously. We appreciate your efforts to responsibly disclose your findings.

### How to Report

Please **DO NOT** open a public issue to report security vulnerabilities. Instead, please report them using one of the following methods:

1. **Email**: Send details to security@neo.org
2. **Security Advisory**: Use GitHub's [Security Advisory](https://github.com/neo-project/neo-service-layer/security/advisories/new) feature

### What to Include

When reporting a vulnerability, please include:

- A clear description of the vulnerability
- Steps to reproduce the issue
- Potential impact of the vulnerability
- Any suggested fixes or mitigations
- Your contact information (optional)

### Response Timeline

- **Initial Response**: Within 48 hours
- **Status Update**: Within 7 days
- **Resolution Target**: Within 30 days for critical issues

### What to Expect

1. **Acknowledgment**: We'll acknowledge receipt of your report
2. **Assessment**: Our security team will assess the vulnerability
3. **Communication**: We'll keep you informed of our progress
4. **Resolution**: We'll work on a fix and coordinate disclosure
5. **Recognition**: With your permission, we'll acknowledge your contribution

## Security Best Practices

When using Neo Service Layer:

### Environment Variables

- Never commit `.env` files to version control
- Use the provided `.env.example` as a template
- Generate secure credentials using the provided script:
  ```bash
  ./scripts/generate-secure-credentials.sh
  ```

### JWT Configuration

- Always use strong, randomly generated JWT secret keys
- Configure appropriate token expiration times
- Enable all validation options in production

### SGX/TEE Security

- Use hardware mode (HW) in production environments
- Enable remote attestation for critical operations
- Regularly update SGX drivers and SDK

### Database Security

- Use strong, unique passwords for all database instances
- Enable SSL/TLS for database connections in production
- Implement proper access controls and network isolation

### API Security

- Enable HTTPS in production
- Configure CORS appropriately
- Implement rate limiting
- Use API authentication for sensitive endpoints

## Security Features

Neo Service Layer includes several security features:

- **Intel SGX Integration**: Hardware-based security for sensitive operations
- **End-to-End Encryption**: Data encryption at rest and in transit
- **Authentication & Authorization**: JWT-based authentication with role-based access
- **Audit Logging**: Comprehensive audit trails for security events
- **Input Validation**: Strict validation of all user inputs
- **Rate Limiting**: Protection against abuse and DoS attacks
- **Security Headers**: Proper security headers for web responses

## Compliance

Neo Service Layer is designed with compliance in mind:

- GDPR compliance features
- Audit log retention policies
- Data encryption standards
- Access control mechanisms

## Security Updates

Stay informed about security updates:

- Watch the repository for security advisories
- Subscribe to our security mailing list
- Check the [CHANGELOG](CHANGELOG.md) for security-related updates

## Responsible Disclosure

We support responsible disclosure and will:

- Work with security researchers to verify and fix issues
- Publicly acknowledge researchers (with permission)
- Not pursue legal action for good-faith security research

Thank you for helping keep Neo Service Layer secure!
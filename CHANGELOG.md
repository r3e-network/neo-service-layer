# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Security
- Fixed critical security vulnerabilities with hardcoded database credentials
- Removed hardcoded secrets from environment files
- Added secure credential generation script (`scripts/generate-secure-credentials.sh`)
- Updated all docker-compose files to use environment variables for sensitive data
- Added SECURITY.md with vulnerability reporting guidelines

### Changed
- Standardized JWT configuration across all environments
  - Unified configuration structure under `Jwt` section
  - Consistent property names and validation settings
  - Added refresh token support with configurable expiration
- Fixed audit log retention period from 2555 to 365 days
- Updated .env.example with secure defaults and instructions
- Enhanced documentation with security configuration guidelines

### Added
- `.env.example` template file with comprehensive configuration options
- Secure credential generation script for production use
- SECURITY.md file for responsible vulnerability disclosure
- JWT standardization template (`src/Common/jwt-settings-standard.json`)
- Environment-based configuration for all sensitive values

### Fixed
- Missing `using System` statement in TestConfiguration.cs
- Build warnings related to async methods without await
- Nullable reference type warnings

### Documentation
- Updated README.md with security configuration section
- Enhanced Docker documentation with new environment variables
- Updated API authentication documentation with JWT configuration details
- Added environment setup instructions to all deployment guides

## [1.0.0] - 2024-01-XX

### Added
- Initial production-ready release
- Microservices architecture implementation
- Intel SGX integration with Occlum LibOS
- Comprehensive service suite:
  - Storage Service
  - Key Management Service (with 4 crypto services)
  - AI Services (Pattern Recognition, Prediction)
  - Cross-Chain Bridge Service
  - Notification Service
  - Monitoring Service
  - Compliance Service
  - Oracle Service
  - And 20+ additional services
- Service discovery with Consul
- API Gateway with rate limiting
- Distributed tracing with Jaeger
- Metrics collection with Prometheus
- Monitoring dashboards with Grafana
- Docker and Kubernetes support
- Comprehensive testing infrastructure
- Multi-blockchain support (Neo N3 and NeoX)

### Infrastructure
- Clean architecture implementation
- Central package version management
- Consistent coding standards
- Performance optimizations
- Security hardening
- Observability stack

### Documentation
- Comprehensive API documentation
- Architecture guides
- Deployment instructions
- Service documentation
- Security guidelines
- Contributing guidelines
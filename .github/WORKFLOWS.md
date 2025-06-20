# 🚀 Neo Service Layer - GitHub Workflows Guide

[![CI/CD Pipeline](https://github.com/r3e-network/neo-service-layer/workflows/🚀%20Neo%20Service%20Layer%20CI/CD%20Pipeline/badge.svg)](https://github.com/r3e-network/neo-service-layer/actions/workflows/ci-cd.yml)
[![Code Quality](https://github.com/r3e-network/neo-service-layer/workflows/🔍%20Code%20Quality%20Gate/badge.svg)](https://github.com/r3e-network/neo-service-layer/actions/workflows/code-quality.yml)
[![Security Scan](https://github.com/r3e-network/neo-service-layer/workflows/🛡️%20Dependency%20Security%20Check/badge.svg)](https://github.com/r3e-network/neo-service-layer/actions/workflows/dependency-check.yml)
[![Release](https://github.com/r3e-network/neo-service-layer/workflows/🚀%20Release%20Pipeline/badge.svg)](https://github.com/r3e-network/neo-service-layer/actions/workflows/release.yml)

## 📋 Workflow Overview

The Neo Service Layer uses a comprehensive GitHub Actions setup with 4 main workflows designed for enterprise-grade automation:

### 🏗️ **CI/CD Pipeline** (`ci-cd.yml`)
**Triggers:** Push to main/master/develop, Pull Requests, Manual dispatch
- **Purpose:** Continuous Integration and Deployment
- **Duration:** ~15-25 minutes
- **Features:**
  - 🔄 Matrix builds (Debug/Release)
  - 🧪 Comprehensive testing (Unit, Integration, Performance)
  - 🐳 Docker multi-arch builds with security scanning
  - 🚀 Automated deployments to staging/production
  - 📊 Performance benchmarking
  - 🔍 Security scanning (dependency and container scanning)

### 🔍 **Code Quality Gate** (`code-quality.yml`)
**Triggers:** Pull Requests, Push to main branches
- **Purpose:** Enforce code quality standards
- **Duration:** ~8-12 minutes
- **Features:**
  - 📈 Code coverage analysis (75% line, 70% branch minimum)
  - 🛡️ Security vulnerability scanning
  - 🎨 Code style and formatting validation
  - 💬 Automated PR comments with quality metrics
  - ❌ Fails builds that don't meet quality gates

### 🚀 **Release Pipeline** (`release.yml`)
**Triggers:** Version tags (`v*`)
- **Purpose:** Automated release management
- **Duration:** ~20-30 minutes
- **Features:**
  - 📦 Multi-platform binary builds (Linux, Windows, macOS, ARM64)
  - 📚 NuGet package publishing
  - 🐳 Multi-arch Docker images with SBOM
  - 📝 Automated changelog generation
  - 🏷️ GitHub release creation
  - 📢 Slack notifications

### 🛡️ **Dependency Security Check** (`dependency-check.yml`)
**Triggers:** Daily schedule, Pull Requests with dependency changes, Manual dispatch
- **Purpose:** Continuous security monitoring
- **Duration:** ~10-15 minutes
- **Features:**
  - 🔍 OWASP dependency check across all ecosystems (.NET, Node.js, Rust)
  - 🚨 Automated security issue creation
  - 📧 Slack security alerts
  - 🔒 PR blocking for critical vulnerabilities
  - 📊 Comprehensive vulnerability reporting with SARIF

## 🔧 Workflow Configuration

### Environment Variables

Configure these secrets in your GitHub repository settings:

#### Required Secrets
```bash
# NuGet Publishing
NUGET_API_KEY                 # NuGet.org API key

# Docker Registry
DOCKER_USERNAME               # Docker Hub username
DOCKER_PASSWORD               # Docker Hub password

# Slack Notifications (Optional)
SLACK_WEBHOOK_URL            # Slack webhook for notifications

# Deployment (Production)
PRODUCTION_SSH_KEY           # SSH key for production deployment
PRODUCTION_HOST              # Production server hostname
STAGING_SSH_KEY              # SSH key for staging deployment
STAGING_HOST                 # Staging server hostname

# Security Scanning (Optional)
SNYK_TOKEN                   # Snyk security scanning token
```

#### Environment Variables
```bash
# Coverage Thresholds
COVERAGE_THRESHOLD=75        # Minimum line coverage percentage
BRANCH_COVERAGE_THRESHOLD=70 # Minimum branch coverage percentage

# Build Configuration
DOTNET_VERSION=9.0.x        # .NET version
NODE_VERSION=20             # Node.js version
RUST_VERSION=stable        # Rust version
```

### Quality Gate Configuration

The workflows enforce the following quality standards:

#### Code Coverage
- **Line Coverage:** 75% minimum
- **Branch Coverage:** 70% minimum
- **Exemptions:** Test projects, generated code

#### Security Standards
- **Zero tolerance** for Critical and High severity vulnerabilities
- **Automated blocking** of PRs with security issues
- **Daily scans** for new vulnerabilities

#### Code Quality
- **Automated formatting** enforcement
- **Style guide** compliance
- **Complexity analysis** with configurable thresholds

## 🚀 Usage Guide

### Creating a Release

1. **Prepare Release:**
   ```bash
   # Update version in global.json
   git add global.json
   git commit -m "chore: bump version to 1.2.0"
   git push origin master
   ```

2. **Create Release Tag:**
   ```bash
   git tag -a v1.2.0 -m "Release version 1.2.0"
   git push origin v1.2.0
   ```

3. **Monitor Release:**
   - Watch the Release Pipeline workflow
   - Check GitHub Releases page for generated release
   - Verify NuGet packages are published
   - Confirm Docker images are available

### Manual Workflow Triggers

#### CI/CD Pipeline
```bash
# Skip tests (for hotfixes)
gh workflow run ci-cd.yml -f skip_tests=true

# Skip Docker build (for documentation changes)
gh workflow run ci-cd.yml -f skip_docker=true
```

#### Dependency Security Check
```bash
# Run security scan manually
gh workflow run dependency-check.yml
```

### Quality Gate Overrides

For exceptional cases, you can override quality gates:

```bash
# In commit message or PR description
[skip coverage]     # Skip coverage requirements
[skip security]     # Skip security scanning (emergency only)
[skip quality]      # Skip all quality gates (emergency only)
```

## 📊 Monitoring and Maintenance

### Performance Metrics
- **Build Times:** Monitored and optimized through caching
- **Test Execution:** Parallel execution with matrix strategies
- **Docker Builds:** Multi-stage builds with layer caching

### Caching Strategy
- **.NET Dependencies:** Restored packages cached by lock file hash
- **Node.js Dependencies:** npm cache with package-lock.json hash
- **Rust Dependencies:** Cargo cache with Cargo.lock hash
- **Docker Layers:** BuildKit cache for faster image builds

### Troubleshooting

#### Common Issues

1. **Build Failures:**
   ```bash
   # Check workflow logs
   gh run list --workflow=ci-cd.yml
   gh run view [RUN_ID] --log
   ```

2. **Coverage Failures:**
   ```bash
   # Run coverage locally
   dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
   ```

3. **Security Scan Issues:**
   ```bash
   # Run OWASP dependency check locally
   dotnet list package --vulnerable
   ```

#### Workflow Maintenance

- **Weekly:** Review workflow performance metrics
- **Monthly:** Update action versions and dependencies
- **Quarterly:** Review and optimize caching strategies
- **Annually:** Security audit of workflow permissions

## 🔐 Security Considerations

### Workflow Permissions
All workflows use minimal required permissions:
- `contents: read` - Read repository contents
- `security-events: write` - Write security scan results
- `packages: write` - Publish packages (release only)

### Secret Management
- **Never commit secrets** to the repository
- **Use GitHub Secrets** for sensitive configuration
- **Rotate secrets regularly** (quarterly recommended)
- **Audit secret usage** in workflow runs

### Supply Chain Security
- **Pinned action versions** with SHA hashes
- **SBOM generation** for container images
- **Provenance tracking** for all artifacts
- **Signed releases** with checksums

## 📞 Support and Maintenance

### Workflow Updates
To update workflows:
1. Create a feature branch
2. Test changes in the branch
3. Create PR with workflow changes
4. Test thoroughly before merging

### Getting Help
- **Workflow Issues:** Check GitHub Actions tab
- **Security Alerts:** Review Security tab
- **Performance Issues:** Review workflow timing metrics

### Best Practices
- **Keep workflows DRY** - Use reusable actions
- **Monitor costs** - GitHub Actions minutes usage
- **Regular maintenance** - Update dependencies and actions
- **Document changes** - Update this guide when workflows change

---

## 🏆 Workflow Excellence

These workflows represent enterprise-grade automation following industry best practices:

- ✅ **Security First:** Comprehensive scanning and compliance
- ✅ **Quality Enforced:** Automated quality gates with no exceptions
- ✅ **Performance Optimized:** Intelligent caching and parallel execution
- ✅ **Developer Friendly:** Clear feedback and automated processes
- ✅ **Production Ready:** Automated releases with rollback capabilities

The Neo Service Layer workflows ensure every code change meets enterprise standards while maintaining developer velocity and system reliability.
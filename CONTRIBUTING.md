# Contributing to Neo Service Layer

Thank you for your interest in contributing to the Neo Service Layer! This document provides guidelines and instructions for contributing to the project.

## 🌟 Ways to Contribute

- **Bug Reports**: Report issues and bugs
- **Feature Requests**: Suggest new features or improvements
- **Code Contributions**: Submit pull requests with bug fixes or new features
- **Documentation**: Improve documentation and examples
- **Testing**: Help improve test coverage and quality
- **Security**: Report security vulnerabilities responsibly

## 🚀 Getting Started

### Prerequisites

- **.NET 9.0 SDK** or later
- **Git** for version control
- **Visual Studio 2022/2025** or **VS Code**
- **Docker** (for container testing)
- **Intel SGX SDK** (for enclave development)

### Development Setup

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR_USERNAME/neo-service-layer.git
   cd neo-service-layer
   ```

3. **Add upstream remote**:
   ```bash
   git remote add upstream https://github.com/neo-project/neo-service-layer.git
   ```

4. **Build the solution**:
   ```bash
   dotnet build
   ```

5. **Run tests** to ensure everything works:
   ```bash
   dotnet test
   ```

## 📝 Development Workflow

### Creating a Feature Branch

```bash
# Update your fork
git checkout main
git pull upstream main
git push origin main

# Create a feature branch
git checkout -b feature/your-feature-name
```

### Making Changes

1. **Follow coding standards** outlined in [CODING_STANDARDS.md](docs/development/CODING_STANDARDS.md)
2. **Write tests** for new functionality
3. **Update documentation** as needed
4. **Ensure code quality** by running:
   ```bash
   # Build and test
   dotnet build
   dotnet test
   
   # Check for formatting issues
   dotnet format --verify-no-changes
   ```

### Committing Changes

We follow conventional commit messages:

```bash
# Format: type(scope): description
git commit -m "feat(oracle): add new data source validation"
git commit -m "fix(storage): resolve encryption key rotation issue"
git commit -m "docs(api): update authentication examples"
```

**Commit Types:**
- `feat`: New features
- `fix`: Bug fixes
- `docs`: Documentation updates
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

### Submitting a Pull Request

1. **Push your branch**:
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a pull request** on GitHub with:
   - Clear title and description
   - Reference any related issues
   - Include testing instructions
   - Add screenshots for UI changes

3. **Address review feedback** promptly

## 🧪 Testing Guidelines

### Test Requirements

- **Unit tests** for all new functionality
- **Integration tests** for service interactions
- **Maintain or improve** test coverage (currently 80%+)
- **Security tests** for cryptographic operations

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
dotnet test --filter Category=Performance

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Structure

```
tests/
├── Unit/                   # Unit tests
├── Integration/           # Integration tests
├── Performance/           # Performance tests
└── TestInfrastructure/    # Shared test utilities
```

## 📋 Coding Standards

### General Guidelines

- **Follow C# conventions** and .NET best practices
- **Use meaningful names** for variables, methods, and classes
- **Add XML documentation** for public APIs
- **Keep methods focused** and single-purpose
- **Handle exceptions** appropriately
- **Use async/await** for I/O operations

### Code Formatting

```bash
# Format code automatically
dotnet format

# Check formatting
dotnet format --verify-no-changes
```

### Documentation Requirements

- **XML comments** for all public members
- **README files** for new services or components
- **API documentation** updates for new endpoints
- **Code comments** for complex logic

## 🔒 Security Considerations

### Security Requirements

- **Never commit secrets** or credentials
- **Use secure coding practices** for cryptographic operations
- **Follow SGX enclave** development guidelines
- **Validate all inputs** and sanitize outputs
- **Report security issues** privately

### Reporting Security Issues

**DO NOT** create public issues for security vulnerabilities. Instead:

1. Email security@neo.org with details
2. Include steps to reproduce
3. Provide impact assessment
4. Allow time for responsible disclosure

## 🎯 Service Development Guidelines

### Adding New Services

1. **Follow service framework** patterns
2. **Implement required interfaces** (IService)
3. **Add comprehensive tests** (unit and integration)
4. **Create service documentation**
5. **Update API controllers** if needed

### Service Structure

```
src/Services/NeoServiceLayer.Services.YourService/
├── IYourService.cs                    # Service interface
├── YourService.cs                     # Main service implementation
├── YourService.Operations.cs          # Additional operations
├── Models/                            # Service-specific models
└── README.md                          # Service documentation
```

### Required Components

- **Service interface** implementing `IService`
- **Service implementation** inheriting from appropriate base class
- **Unit tests** with 80%+ coverage
- **Integration tests** for external dependencies
- **API controller** for web endpoints
- **Documentation** and examples

## 📊 Pull Request Guidelines

### Before Submitting

- [ ] Code builds successfully
- [ ] All tests pass
- [ ] Test coverage is maintained or improved
- [ ] Documentation is updated
- [ ] Security considerations are addressed
- [ ] Code follows style guidelines

### PR Template

Use this template for pull requests:

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] No new warnings introduced
```

## 📚 Documentation Standards

### Documentation Types

- **Code Documentation**: XML comments for APIs
- **User Documentation**: Usage guides and examples
- **Developer Documentation**: Architecture and implementation details
- **API Documentation**: Swagger/OpenAPI specifications

### Writing Guidelines

- **Use clear, concise language**
- **Include code examples** where appropriate
- **Keep documentation up-to-date** with code changes
- **Use consistent formatting** and style

## 🤝 Community Guidelines

### Code of Conduct

- **Be respectful** and inclusive
- **Focus on constructive feedback**
- **Help others learn** and grow
- **Collaborate effectively**
- **Report inappropriate behavior**

### Getting Help

- **GitHub Discussions**: For questions and discussions
- **GitHub Issues**: For bug reports and feature requests
- **Discord/Slack**: For real-time community chat
- **Email**: For private or sensitive matters

## 🏆 Recognition

Contributors are recognized through:

- **Git commit history** and contributions
- **Release notes** acknowledgments
- **Community highlights** and features
- **Contributor badges** and recognition

## 📞 Contact

- **General Questions**: GitHub Discussions
- **Bug Reports**: GitHub Issues
- **Security Issues**: security@neo.org
- **Maintainers**: maintainers@neo.org

Thank you for contributing to the Neo Service Layer! 🚀
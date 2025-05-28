# Contributing to Neo Service Layer

Thank you for your interest in contributing to the Neo Service Layer! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

Please read and follow our [Code of Conduct](CODE_OF_CONDUCT.md).

## How to Contribute

### Reporting Bugs

If you find a bug, please report it by creating an issue in the GitHub repository. Please include:

- A clear and descriptive title
- A detailed description of the bug
- Steps to reproduce the bug
- Expected behavior
- Actual behavior
- Screenshots (if applicable)
- Environment information (OS, .NET version, etc.)

### Suggesting Enhancements

If you have an idea for an enhancement, please create an issue in the GitHub repository. Please include:

- A clear and descriptive title
- A detailed description of the enhancement
- Why the enhancement would be useful
- Any relevant examples or mockups

### Pull Requests

1. Fork the repository
2. Create a new branch for your changes
3. Make your changes
4. Run tests to ensure your changes don't break existing functionality
5. Submit a pull request

Please ensure your pull request:

- Has a clear and descriptive title
- Includes a detailed description of the changes
- References any related issues
- Follows the coding style and conventions of the project
- Includes tests for new functionality
- Updates documentation as necessary

## Development Environment

### Prerequisites

- .NET 9.0 SDK
- Visual Studio 2025 or later (optional)
- Git
- Occlum LibOS (for enclave development)
- SGX SDK (for enclave development)

### Building the Project

1. Clone the repository:

```bash
git clone https://github.com/neo-project/neo-service-layer.git
cd neo-service-layer
```

2. Build the solution:

```bash
dotnet build
```

3. Run the tests:

```bash
dotnet test
```

## Coding Style

Please follow the coding style and conventions used in the project. We use the following guidelines:

- Use 4 spaces for indentation
- Use camelCase for private fields with an underscore prefix
- Use PascalCase for public members
- Use meaningful names for variables, methods, and classes
- Write clear and concise comments
- Write unit tests for new functionality

## Documentation

Please update documentation as necessary when making changes. This includes:

- Code comments
- README files
- API documentation
- Architecture documentation

## License

By contributing to the Neo Service Layer, you agree that your contributions will be licensed under the project's MIT License.

## Questions

If you have any questions, please feel free to create an issue in the GitHub repository or contact the project maintainers.

Thank you for your contributions!

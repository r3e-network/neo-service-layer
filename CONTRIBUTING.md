# Contributing to Neo Service Layer

Thank you for your interest in contributing to the Neo Service Layer! We welcome contributions from the community to help make this project better.

## Code of Conduct

Please read and follow our [Code of Conduct](CODE_OF_CONDUCT.md) to maintain a respectful and inclusive environment for everyone.

## How to Contribute

There are many ways to contribute to the Neo Service Layer:

1. **Reporting Bugs**: If you find a bug, please report it by creating an issue in our GitHub repository.
2. **Suggesting Enhancements**: If you have an idea for a new feature or an improvement to an existing feature, please create an issue in our GitHub repository.
3. **Contributing Code**: If you want to contribute code, please follow the guidelines below.
4. **Improving Documentation**: If you find errors or omissions in the documentation, please submit a pull request with your improvements.
5. **Reviewing Pull Requests**: Help review pull requests from other contributors.

## Development Environment Setup

Please refer to the [Development Guide](docs/Development.md) for detailed instructions on setting up your development environment.

## Pull Request Process

1. **Fork the Repository**: Fork the Neo Service Layer repository to your GitHub account.
2. **Create a Branch**: Create a branch for your changes from the `main` branch.
3. **Make Changes**: Make your changes to the codebase.
4. **Write Tests**: Write tests for your changes to ensure they work as expected.
5. **Run Tests**: Run all tests to ensure your changes don't break existing functionality.
6. **Update Documentation**: Update the documentation to reflect your changes.
7. **Submit a Pull Request**: Submit a pull request from your branch to the `main` branch of the Neo Service Layer repository.

## Pull Request Guidelines

- Follow the coding standards and style guidelines.
- Keep pull requests focused on a single change or feature.
- Write clear and concise commit messages.
- Include tests for your changes.
- Update documentation as needed.
- Ensure all tests pass before submitting your pull request.
- Squash your commits before merging.

## Coding Standards

Please follow the coding standards and style guidelines for the Neo Service Layer:

- **C# Code**: Follow the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).
- **C++ Code**: Follow the [Google C++ Style Guide](https://google.github.io/styleguide/cppguide.html).
- **Documentation**: Write clear and concise documentation.
- **Tests**: Write comprehensive tests for your code.

## Commit Message Guidelines

We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification for commit messages:

```
<type>(<scope>): <subject>

<body>

<footer>
```

- **Type**: The type of change (e.g., feat, fix, docs, style, refactor, test, chore).
- **Scope**: The scope of the change (e.g., api, tee-host, enclave).
- **Subject**: A short description of the change.
- **Body**: A more detailed description of the change.
- **Footer**: Any breaking changes or references to issues.

Examples:

```
feat(api): add new endpoint for attestation verification

Add a new endpoint to verify attestation proofs from the TEE.

Closes #123
```

```
fix(enclave): fix memory leak in key management

Fix a memory leak in the key management component of the enclave.

Closes #456
```

## License

By contributing to the Neo Service Layer, you agree that your contributions will be licensed under the project's [MIT License](LICENSE).

# Neo Service Layer - Frequently Asked Questions

## General Questions

### What is the Neo Service Layer?

The Neo Service Layer is a comprehensive framework for building secure, privacy-preserving services on the Neo blockchain ecosystem. It leverages Trusted Execution Environments (TEEs), specifically Intel SGX with Occlum LibOS, to ensure the confidentiality and integrity of sensitive operations.

### What blockchains does the Neo Service Layer support?

The Neo Service Layer currently supports the following blockchains:

- **Neo N3**: The third generation of the Neo blockchain, featuring improved performance, enhanced security, and new features.
- **NeoX**: An EVM-compatible blockchain that uses Neo GAS as the transaction fee, providing compatibility with Ethereum-based applications.

### What services does the Neo Service Layer provide?

The Neo Service Layer provides the following core services:

- **Randomness Service**: Provides verifiable random numbers for use in smart contracts and applications.
- **Oracle Service**: Fetches data from external sources and delivers it to smart contracts with cryptographic proofs.
- **Key Management Service**: Securely manages cryptographic keys for various operations.
- **Compute Service**: Executes computations in a secure enclave, ensuring that the code and data are protected.
- **Storage Service**: Stores and retrieves data with encryption, compression, and access control.
- **Compliance Service**: Verifies compliance with regulatory requirements for transactions, addresses, and contracts.
- **Event Subscription Service**: Subscribes to and receives events from the blockchain, enabling automated contract interactions.

### How does the Neo Service Layer ensure security?

The Neo Service Layer ensures security through multiple layers of protection:

1. **Enclave Security**: Trusted Execution Environments (TEEs) using Intel SGX with Occlum LibOS protect code and data from unauthorized access.
2. **API Security**: Authentication, authorization, and rate limiting for API access.
3. **Data Security**: Encryption, access control, and secure storage for sensitive data.
4. **Network Security**: TLS, firewalls, and network segmentation.
5. **Operational Security**: Secure deployment, monitoring, and incident response.

### How can I get started with the Neo Service Layer?

To get started with the Neo Service Layer, follow these steps:

1. **Set Up Development Environment**: Install the required prerequisites, including .NET 9.0, Intel SGX SDK, and Occlum LibOS.
2. **Clone the Repository**: Clone the Neo Service Layer repository from GitHub.
3. **Build the Solution**: Build the solution using .NET CLI or Visual Studio.
4. **Run the Services**: Run the services locally for development and testing.
5. **Explore the API**: Use the API documentation to explore the available endpoints.
6. **Develop Your Application**: Develop your application using the Neo Service Layer services.

For detailed instructions, see the [Development Guide](development/README.md).

## Technical Questions

### What is Intel SGX?

Intel Software Guard Extensions (SGX) is a set of security-related instruction codes built into Intel CPUs. It allows user-level code to allocate private regions of memory, called enclaves, which are protected from processes running at higher privilege levels, including the operating system and hypervisor.

### What is Occlum LibOS?

Occlum is a memory-safe, multi-process library operating system (LibOS) for Intel SGX. It enables legacy applications to run securely in SGX enclaves without modification. Occlum provides a complete LibOS that supports most POSIX-compatible applications.

### How does the Neo Service Layer use Intel SGX and Occlum LibOS?

The Neo Service Layer uses Intel SGX and Occlum LibOS to create Trusted Execution Environments (TEEs) that protect code and data from unauthorized access. The sensitive operations, such as random number generation, key management, and computation, are performed within these TEEs, ensuring their confidentiality and integrity.

### Can I run the Neo Service Layer without Intel SGX?

Yes, the Neo Service Layer can run in simulation mode without Intel SGX hardware. However, in simulation mode, the security guarantees provided by SGX are not available. Simulation mode is useful for development and testing but should not be used in production environments where security is critical.

### How does the Neo Service Layer integrate with Neo N3 and NeoX?

The Neo Service Layer integrates with Neo N3 and NeoX through blockchain clients that communicate with the respective blockchain networks. These clients provide functionality for interacting with the blockchain, such as submitting transactions, querying the blockchain state, and monitoring events.

### What programming languages are supported for developing with the Neo Service Layer?

The Neo Service Layer is primarily developed in C# for the service layer and C++ for the enclave layer. It supports executing JavaScript code within the enclave for custom computations. Client applications can be developed in any language that can make HTTP requests to the API.

### How does the Neo Service Layer handle user secrets?

The Neo Service Layer securely stores user secrets within the enclave, ensuring that they are protected from unauthorized access. Secrets are encrypted before storage and can only be accessed by authorized users through the API with proper authentication and authorization.

### How does the Neo Service Layer ensure the integrity of external data?

The Oracle Service fetches data from external sources and generates cryptographic proofs that can be verified by smart contracts. These proofs ensure that the data has not been tampered with and comes from the expected source.

### How does the Neo Service Layer handle high availability and scalability?

The Neo Service Layer can be deployed in a clustered configuration for high availability and scalability. Multiple instances of the services can be deployed across multiple nodes, with load balancing to distribute the load. The services are designed to be stateless, allowing for horizontal scaling.

## API Questions

### How do I authenticate with the API?

The API supports the following authentication methods:

1. **API Key**: Include the API key in the `X-API-Key` header.
2. **JWT**: Include the JWT token in the `Authorization` header with the `Bearer` scheme.
3. **OAuth 2.0**: Obtain an access token from the OAuth 2.0 server and include it in the `Authorization` header with the `Bearer` scheme.

### What are the rate limits for the API?

The API implements rate limiting to prevent abuse, with the following default limits:

- **Requests per second**: 10
- **Requests per minute**: 100
- **Requests per hour**: 1,000
- **Requests per day**: 10,000

Rate limits are applied on a per-API-key basis, and rate limit information is included in the response headers.

### How do I handle API errors?

API errors are returned with appropriate HTTP status codes and error messages in the response body. The response body includes an error object with a code, message, and details.

Example error response:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "invalid_parameter",
    "message": "The parameter 'min' must be less than 'max'.",
    "details": {
      "min": 100,
      "max": 1
    }
  },
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z"
  }
}
```

To handle API errors, check the HTTP status code and the error object in the response body.

### How do I use pagination with the API?

For endpoints that return multiple items, the API supports pagination through the following query parameters:

- **page**: The page number to retrieve (default: 1).
- **per_page**: The number of items per page (default: 10, max: 100).

Pagination information is included in the response metadata:

```json
{
  "success": true,
  "data": [
    // Response data
  ],
  "error": null,
  "meta": {
    "requestId": "request-id",
    "timestamp": "2023-01-01T00:00:00Z",
    "pagination": {
      "page": 1,
      "per_page": 10,
      "total_pages": 5,
      "total_items": 42
    }
  }
}
```

### How do I use the API with different blockchain types?

To use the API with different blockchain types, include the `blockchain` parameter in the request body or query string. The supported values are `neo-n3` and `neo-x`.

Example:

```json
{
  "blockchain": "neo-n3",
  "min": 1,
  "max": 100
}
```

## Deployment Questions

### What are the hardware requirements for running the Neo Service Layer?

The hardware requirements for running the Neo Service Layer are:

- **CPU**: Intel CPU with SGX support (for enclave operations)
- **RAM**: Minimum 16 GB (32 GB recommended)
- **Storage**: Minimum 100 GB SSD
- **Network**: 1 Gbps Ethernet

### What are the software requirements for running the Neo Service Layer?

The software requirements for running the Neo Service Layer are:

- **Operating System**: Ubuntu 20.04 LTS or later
- **.NET SDK**: .NET 9.0 or later
- **Docker**: Docker 20.10 or later (optional, for containerized deployment)
- **Kubernetes**: Kubernetes 1.25 or later (optional, for clustered deployment)
- **Intel SGX Driver**: SGX driver compatible with your CPU
- **Occlum LibOS**: Occlum 0.30.0 or later

### How do I deploy the Neo Service Layer in a production environment?

To deploy the Neo Service Layer in a production environment, follow these steps:

1. **Set Up Infrastructure**: Set up the required infrastructure, including servers, networking, and storage.
2. **Install Prerequisites**: Install the required prerequisites, including .NET 9.0, Intel SGX SDK, and Occlum LibOS.
3. **Configure Services**: Configure the services for your environment.
4. **Deploy Services**: Deploy the services using your preferred deployment method (e.g., Docker, Kubernetes).
5. **Set Up Monitoring**: Set up monitoring and alerting for the services.
6. **Set Up Backup**: Set up backup and recovery procedures.

For detailed instructions, see the [Deployment Guide](deployment/README.md).

### Can I deploy the Neo Service Layer in a cloud environment?

Yes, the Neo Service Layer can be deployed in a cloud environment that supports Intel SGX, such as Azure Confidential Computing or AWS Nitro Enclaves. However, the availability of SGX in cloud environments may be limited, and you should check with your cloud provider for support.

## Support Questions

### How do I get support for the Neo Service Layer?

You can get support for the Neo Service Layer from the following sources:

- **Documentation**: Check the [Neo Service Layer Documentation](https://docs.neoservicelayer.org).
- **GitHub Issues**: Check the [GitHub Issues](https://github.com/neo-project/neo-service-layer/issues) for known issues and solutions.
- **Discord**: Join the [Neo Discord](https://discord.gg/neo) and ask for help in the #neo-service-layer channel.
- **Email Support**: Contact support@neoservicelayer.org for assistance.

### How do I report a security vulnerability?

To report a security vulnerability, contact security@neoservicelayer.org with the details of the vulnerability. Do not disclose the vulnerability publicly until it has been addressed.

### How do I contribute to the Neo Service Layer?

To contribute to the Neo Service Layer, follow these steps:

1. **Fork the Repository**: Fork the repository on GitHub.
2. **Create a Branch**: Create a branch for your changes.
3. **Make Changes**: Make your changes following the coding standards.
4. **Write Tests**: Write tests for your changes.
5. **Run Tests**: Run tests to ensure they pass.
6. **Submit Pull Request**: Submit a pull request with your changes.

For detailed instructions, see the [Contributing Guide](CONTRIBUTING.md).

## References

- [Neo Service Layer Architecture](architecture/README.md)
- [Neo Service Layer API](api/README.md)
- [Neo Service Layer Services](services/README.md)
- [Neo Service Layer Deployment Guide](deployment/README.md)
- [Neo Service Layer Development Guide](development/README.md)
- [Neo Service Layer Security Guide](security/README.md)
- [Neo Service Layer Troubleshooting Guide](troubleshooting/README.md)

# Neo Service Layer - Troubleshooting Guide

## Overview

This guide provides troubleshooting information for common issues that may occur when using the Neo Service Layer. It covers API issues, service issues, enclave issues, blockchain integration issues, and deployment issues.

## Diagnosing Issues

When troubleshooting issues with the Neo Service Layer, follow these general steps:

1. **Identify the Issue**: Determine what is not working as expected.
2. **Check Logs**: Check the logs for error messages.
3. **Check Health Status**: Check the health status of the services.
4. **Check Metrics**: Check the metrics for anomalies.
5. **Isolate the Issue**: Determine which component is causing the issue.
6. **Apply Fixes**: Apply the appropriate fixes.
7. **Verify the Fix**: Verify that the issue is resolved.

## API Issues

### API Connection Failed

**Symptoms**:
- Unable to connect to the API
- Connection timeout
- Connection refused

**Possible Causes**:
- API service is not running
- Network connectivity issues
- Firewall blocking the connection

**Troubleshooting Steps**:
1. Verify that the API service is running:
   ```bash
   curl http://localhost:5000/api/v1/health
   ```
2. Check the API service logs:
   ```bash
   cat logs/api.log
   ```
3. Check network connectivity:
   ```bash
   ping api.neoservicelayer.org
   ```
4. Check firewall rules:
   ```bash
   sudo iptables -L
   ```

**Resolution**:
- Start the API service if it is not running:
  ```bash
  dotnet run --project src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj
  ```
- Fix network connectivity issues
- Update firewall rules to allow the connection

### API Authentication Failed

**Symptoms**:
- 401 Unauthorized response
- 403 Forbidden response

**Possible Causes**:
- Invalid API key
- Expired JWT token
- Insufficient permissions

**Troubleshooting Steps**:
1. Check the API key or JWT token:
   ```bash
   curl -H "X-API-Key: your-api-key" http://localhost:5000/api/v1/health
   ```
2. Check the API service logs:
   ```bash
   cat logs/api.log
   ```
3. Check the user's permissions:
   ```bash
   curl -H "X-API-Key: your-api-key" http://localhost:5000/api/v1/auth/me
   ```

**Resolution**:
- Use a valid API key
- Refresh the JWT token
- Grant the necessary permissions to the user

### API Rate Limit Exceeded

**Symptoms**:
- 429 Too Many Requests response
- Rate limit headers in the response

**Possible Causes**:
- Too many requests in a short period
- Rate limit set too low

**Troubleshooting Steps**:
1. Check the rate limit headers in the response:
   ```bash
   curl -v -H "X-API-Key: your-api-key" http://localhost:5000/api/v1/health
   ```
2. Check the API service logs:
   ```bash
   cat logs/api.log
   ```
3. Check the rate limit configuration:
   ```bash
   cat config/appsettings.json
   ```

**Resolution**:
- Reduce the request rate
- Implement request batching
- Request a rate limit increase

## Service Issues

### Service Initialization Failed

**Symptoms**:
- Service health check fails
- Service logs show initialization errors

**Possible Causes**:
- Missing or invalid configuration
- Dependency initialization failed
- Enclave initialization failed

**Troubleshooting Steps**:
1. Check the service logs:
   ```bash
   cat logs/{service-name}.log
   ```
2. Check the service configuration:
   ```bash
   cat config/appsettings.json
   ```
3. Check the dependency health:
   ```bash
   curl http://localhost:5000/api/v1/health
   ```
4. Check the enclave logs:
   ```bash
   cat logs/enclave.log
   ```

**Resolution**:
- Fix the service configuration
- Fix the dependency issues
- Fix the enclave issues

### Service Operation Failed

**Symptoms**:
- Service operation returns an error
- Service logs show operation errors

**Possible Causes**:
- Invalid input parameters
- Internal service error
- Dependency failure

**Troubleshooting Steps**:
1. Check the service logs:
   ```bash
   cat logs/{service-name}.log
   ```
2. Check the input parameters:
   ```bash
   curl -v -H "X-API-Key: your-api-key" -H "Content-Type: application/json" -d '{"param": "value"}' http://localhost:5000/api/v1/{service}/{operation}
   ```
3. Check the dependency health:
   ```bash
   curl http://localhost:5000/api/v1/health
   ```

**Resolution**:
- Fix the input parameters
- Fix the internal service error
- Fix the dependency issues

## Enclave Issues

### Enclave Initialization Failed

**Symptoms**:
- Enclave health check fails
- Enclave logs show initialization errors

**Possible Causes**:
- SGX driver not installed or not working
- Occlum LibOS not installed or not working
- Enclave image not found or corrupted

**Troubleshooting Steps**:
1. Check the enclave logs:
   ```bash
   cat logs/enclave.log
   ```
2. Check the SGX driver status:
   ```bash
   ls /dev/sgx*
   ```
3. Check the Occlum LibOS installation:
   ```bash
   occlum --version
   ```
4. Check the enclave image:
   ```bash
   ls -l enclave/enclave.signed.so
   ```

**Resolution**:
- Install or fix the SGX driver
- Install or fix the Occlum LibOS
- Rebuild the enclave image

### Enclave Operation Failed

**Symptoms**:
- Enclave operation returns an error
- Enclave logs show operation errors

**Possible Causes**:
- Invalid input parameters
- Internal enclave error
- Memory or resource constraints

**Troubleshooting Steps**:
1. Check the enclave logs:
   ```bash
   cat logs/enclave.log
   ```
2. Check the input parameters:
   ```bash
   curl -v -H "X-API-Key: your-api-key" -H "Content-Type: application/json" -d '{"param": "value"}' http://localhost:5000/api/v1/{service}/{operation}
   ```
3. Check the enclave memory usage:
   ```bash
   ps -o pid,rss,command -p $(pgrep -f enclave)
   ```

**Resolution**:
- Fix the input parameters
- Fix the internal enclave error
- Increase the enclave memory allocation

## Blockchain Integration Issues

### Blockchain Connection Failed

**Symptoms**:
- Blockchain operations fail
- Blockchain logs show connection errors

**Possible Causes**:
- Blockchain node not running
- Network connectivity issues
- Invalid blockchain configuration

**Troubleshooting Steps**:
1. Check the blockchain logs:
   ```bash
   cat logs/blockchain.log
   ```
2. Check the blockchain node status:
   ```bash
   curl -X POST -H "Content-Type: application/json" -d '{"jsonrpc":"2.0","method":"getblockcount","params":[],"id":1}' http://localhost:10332
   ```
3. Check network connectivity:
   ```bash
   ping localhost:10332
   ```
4. Check the blockchain configuration:
   ```bash
   cat config/appsettings.json
   ```

**Resolution**:
- Start the blockchain node
- Fix network connectivity issues
- Fix the blockchain configuration

### Blockchain Transaction Failed

**Symptoms**:
- Blockchain transaction fails
- Blockchain logs show transaction errors

**Possible Causes**:
- Insufficient funds
- Invalid transaction parameters
- Network congestion

**Troubleshooting Steps**:
1. Check the blockchain logs:
   ```bash
   cat logs/blockchain.log
   ```
2. Check the account balance:
   ```bash
   curl -X POST -H "Content-Type: application/json" -d '{"jsonrpc":"2.0","method":"getbalance","params":["GAS"],"id":1}' http://localhost:10332
   ```
3. Check the transaction parameters:
   ```bash
   curl -v -H "X-API-Key: your-api-key" -H "Content-Type: application/json" -d '{"param": "value"}' http://localhost:5000/api/v1/{service}/{operation}
   ```
4. Check the network status:
   ```bash
   curl -X POST -H "Content-Type: application/json" -d '{"jsonrpc":"2.0","method":"getblockcount","params":[],"id":1}' http://localhost:10332
   ```

**Resolution**:
- Add funds to the account
- Fix the transaction parameters
- Wait for network congestion to decrease

## Deployment Issues

### Deployment Failed

**Symptoms**:
- Deployment process fails
- Deployment logs show errors

**Possible Causes**:
- Missing prerequisites
- Invalid configuration
- Resource constraints

**Troubleshooting Steps**:
1. Check the deployment logs:
   ```bash
   cat logs/deployment.log
   ```
2. Check the prerequisites:
   ```bash
   dotnet --version
   docker --version
   kubectl version
   ```
3. Check the configuration:
   ```bash
   cat config/appsettings.json
   ```
4. Check the resource usage:
   ```bash
   df -h
   free -h
   ```

**Resolution**:
- Install missing prerequisites
- Fix the configuration
- Increase resource allocation

### Service Not Starting After Deployment

**Symptoms**:
- Service does not start after deployment
- Service logs show startup errors

**Possible Causes**:
- Missing dependencies
- Invalid configuration
- Port conflicts

**Troubleshooting Steps**:
1. Check the service logs:
   ```bash
   cat logs/{service-name}.log
   ```
2. Check the dependencies:
   ```bash
   dotnet restore
   ```
3. Check the configuration:
   ```bash
   cat config/appsettings.json
   ```
4. Check for port conflicts:
   ```bash
   netstat -tuln | grep 5000
   ```

**Resolution**:
- Install missing dependencies
- Fix the configuration
- Resolve port conflicts

## Common Error Messages

### API Errors

- **401 Unauthorized**: Authentication is required or has failed.
- **403 Forbidden**: The request is not allowed.
- **404 Not Found**: The requested resource does not exist.
- **429 Too Many Requests**: The user has sent too many requests in a given amount of time.
- **500 Internal Server Error**: An error occurred on the server.
- **503 Service Unavailable**: The server is currently unavailable.

### Service Errors

- **Service initialization failed**: The service failed to initialize.
- **Service operation failed**: The service operation failed.
- **Service dependency not available**: A service dependency is not available.
- **Service configuration invalid**: The service configuration is invalid.

### Enclave Errors

- **Enclave initialization failed**: The enclave failed to initialize.
- **Enclave operation failed**: The enclave operation failed.
- **Enclave attestation failed**: The enclave attestation failed.
- **Enclave memory allocation failed**: The enclave memory allocation failed.

### Blockchain Errors

- **Blockchain connection failed**: The blockchain connection failed.
- **Blockchain transaction failed**: The blockchain transaction failed.
- **Blockchain account not found**: The blockchain account was not found.
- **Blockchain insufficient funds**: The blockchain account has insufficient funds.

## Getting Help

If you are unable to resolve an issue using this troubleshooting guide, you can get help from the following sources:

- **Documentation**: Check the [Neo Service Layer Documentation](https://docs.neoservicelayer.org).
- **GitHub Issues**: Check the [GitHub Issues](https://github.com/neo-project/neo-service-layer/issues) for known issues and solutions.
- **Discord**: Join the [Neo Discord](https://discord.gg/neo) and ask for help in the #neo-service-layer channel.
- **Email Support**: Contact support@neoservicelayer.org for assistance.

## References

- [Neo Service Layer Architecture](../architecture/README.md)
- [Neo Service Layer API](../api/README.md)
- [Neo Service Layer Services](../services/README.md)
- [Neo Service Layer Deployment Guide](../deployment/README.md)
- [Neo Service Layer Security Guide](../security/README.md)

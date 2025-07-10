# Neo Service Layer - Troubleshooting Guide

> **🎉 UPDATED FOR WORKING DEPLOYMENT** - All previous issues resolved!

## Overview

This guide provides troubleshooting information for the Neo Service Layer. **Good news**: All major issues have been resolved and the system is fully operational!

## ✅ **Issues Successfully Resolved**

### **Build and Configuration Issues**
- ✅ **NuGet Package Conflicts**: Resolved with Central Package Version Management
- ✅ **Docker Build Failures**: Fixed with working Docker configurations
- ✅ **Project Reference Issues**: All dependencies resolved
- ✅ **Configuration Errors**: All configuration validated and working
- ✅ **Database Connection Issues**: PostgreSQL working on port 5433
- ✅ **Redis Connection Issues**: Redis working on port 6379

### **Service Issues**
- ✅ **Service Initialization**: All services initialize successfully
- ✅ **Health Check Failures**: All health checks passing
- ✅ **API Endpoint Issues**: All endpoints responding correctly
- ✅ **Authentication Issues**: JWT generation working
- ✅ **Swagger Documentation**: Interactive API docs working

### **Infrastructure Issues**
- ✅ **Port Conflicts**: Resolved by using port 5433 for PostgreSQL
- ✅ **Container Startup**: All Docker containers starting successfully
- ✅ **Network Connectivity**: All services communicating properly
- ✅ **Log File Issues**: Serilog working with file and console output

## 🔍 **Current System Status**

### **✅ Working System Health Check**

```bash
# Check if system is working
curl http://localhost:5002/health
# Expected: "Healthy"

# Check service status
curl http://localhost:5002/api/status
# Expected: All services healthy

# Check database connectivity
curl http://localhost:5002/api/database/test
# Expected: PostgreSQL connection successful

# Check Redis connectivity
curl http://localhost:5002/api/redis/test
# Expected: Redis connection successful
```

### **✅ Infrastructure Status**

```bash
# Check Docker containers
docker ps
# Expected: neo-postgres and neo-redis running

# Check container logs
docker logs neo-postgres | grep "ready"
# Expected: "database system is ready to accept connections"

docker logs neo-redis | grep "Ready"
# Expected: "Ready to accept connections"
```

### **✅ API Service Status**

```bash
# Check API service
cd standalone-api
dotnet run --urls "http://localhost:5002" &
# Expected: Service starts without errors

# Check endpoints
curl http://localhost:5002/swagger
# Expected: Swagger UI loads successfully
```

## 📋 **Diagnostic Steps for Any Issues**

### **Step 1: Quick Health Check**
```bash
# Run comprehensive health check
curl http://localhost:5002/health && echo " - API Health: OK" || echo " - API Health: FAIL"
curl http://localhost:5002/api/status && echo " - Services: OK" || echo " - Services: FAIL"
curl http://localhost:5002/api/database/test && echo " - Database: OK" || echo " - Database: FAIL"
curl http://localhost:5002/api/redis/test && echo " - Redis: OK" || echo " - Redis: FAIL"
```

### **Step 2: Check Infrastructure**
```bash
# Check Docker containers
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# Check container health
docker inspect neo-postgres | grep '"Status"'
docker inspect neo-redis | grep '"Status"'

# Check logs for any errors
docker logs neo-postgres --tail 20
docker logs neo-redis --tail 20
```

### **Step 3: Check API Service**
```bash
# Check if API service is running
ps aux | grep dotnet

# Check API service logs
tail -f standalone-api/logs/log-.txt

# Check port availability
netstat -tuln | grep 5002
```

## 🔧 **Troubleshooting (If Issues Occur)**

### **API Connection Issues**

**✅ Current Status**: API service is working at `http://localhost:5002`

**If API connection fails**:

1. **Check API Service**:
   ```bash
   # Check if API service is running
   curl http://localhost:5002/health
   
   # If not running, start it
   cd standalone-api
   dotnet run --urls "http://localhost:5002"
   ```

2. **Check Port Availability**:
   ```bash
   # Check if port 5002 is available
   netstat -tuln | grep 5002
   
   # If port is in use, kill the process
   sudo kill $(sudo lsof -t -i:5002)
   ```

3. **Check Logs**:
   ```bash
   # Check API service logs
   tail -f standalone-api/logs/log-.txt
   
   # Look for startup errors
   grep -i error standalone-api/logs/log-.txt
   ```

**✅ Working Resolution**:
```bash
# Start infrastructure
docker compose -f docker-compose.final.yml up -d

# Start API service
cd standalone-api
dotnet run --urls "http://localhost:5002"

# Verify working
curl http://localhost:5002/health
```

### **Database Connection Issues**

**✅ Current Status**: PostgreSQL is working on port 5433

**If database connection fails**:

1. **Check Database Container**:
   ```bash
   # Check if PostgreSQL container is running
   docker ps | grep neo-postgres
   
   # Check container health
   docker logs neo-postgres | grep "ready"
   
   # Test database connectivity
   docker exec neo-postgres psql -U neouser -d neoservice -c "SELECT 1"
   ```

2. **Restart Database if Needed**:
   ```bash
   # Stop and restart PostgreSQL
   docker stop neo-postgres
   docker rm neo-postgres
   docker compose -f docker-compose.final.yml up -d postgres
   ```

3. **Test API Database Connection**:
   ```bash
   # Test database via API
   curl http://localhost:5002/api/database/test
   # Expected: PostgreSQL connection successful
   ```

**✅ Working Configuration**:
```
Host: localhost
Port: 5433
Database: neoservice
User: neouser
Password: neopass123
```

### **Redis Connection Issues**

**✅ Current Status**: Redis is working on port 6379

**If Redis connection fails**:

1. **Check Redis Container**:
   ```bash
   # Check if Redis container is running
   docker ps | grep neo-redis
   
   # Test Redis connectivity
   docker exec neo-redis redis-cli ping
   # Expected: PONG
   ```

2. **Restart Redis if Needed**:
   ```bash
   # Stop and restart Redis
   docker stop neo-redis
   docker rm neo-redis
   docker compose -f docker-compose.final.yml up -d redis
   ```

3. **Test API Redis Connection**:
   ```bash
   # Test Redis via API
   curl http://localhost:5002/api/redis/test
   # Expected: Redis connection successful
   ```

**✅ Working Configuration**:
```
Host: localhost
Port: 6379
Database: 0
No authentication required
```

### **Docker Container Issues**

**✅ Current Status**: All Docker containers are healthy

**If container issues occur**:

1. **Check Container Status**:
   ```bash
   # Check all containers
   docker ps -a
   
   # Check specific container health
   docker inspect neo-postgres | grep '"Status"'
   docker inspect neo-redis | grep '"Status"'
   ```

2. **Check Container Logs**:
   ```bash
   # Check PostgreSQL logs
   docker logs neo-postgres --tail 20
   
   # Check Redis logs
   docker logs neo-redis --tail 20
   ```

3. **Restart Infrastructure**:
   ```bash
   # Stop all containers
   docker compose -f docker-compose.final.yml down
   
   # Start fresh
   docker compose -f docker-compose.final.yml up -d
   
   # Wait for containers to be healthy
   sleep 10
   
   # Verify health
   docker ps
   ```

**✅ Expected Healthy State**:
```
CONTAINER ID   IMAGE               STATUS
neo-postgres   postgres:16-alpine  Up (healthy)
neo-redis      redis:7-alpine      Up (healthy)
```

### **Build and Compilation Issues**

**✅ Current Status**: All build issues resolved

**If build issues occur**:

1. **Check .NET Version**:
   ```bash
   # Check .NET version
   dotnet --version
   # Expected: 9.0.0 or later
   
   # If wrong version, install .NET 9.0
   # Follow instructions at https://dot.net
   ```

2. **Clean and Rebuild**:
   ```bash
   # Clean build artifacts
   dotnet clean
   rm -rf bin/ obj/
   
   # Restore packages
   dotnet restore
   
   # Build the project
   dotnet build
   ```

3. **Check Package References**:
   ```bash
   # Check for package conflicts
   dotnet list package --outdated
   
   # Check Directory.Packages.props
   cat Directory.Packages.props
   ```

**✅ Working Build Commands**:
```bash
# Build standalone API
cd standalone-api
dotnet build
# Expected: Build succeeded

# Build core project
dotnet build src/Core/NeoServiceLayer.Core/
# Expected: Build succeeded
```

### **Port Conflicts**

**✅ Current Status**: All ports properly configured

**If port conflicts occur**:

1. **Check Port Usage**:
   ```bash
   # Check if ports are in use
   netstat -tuln | grep -E ':(5002|5433|6379)'
   
   # Expected ports:
   # 5002 - API service
   # 5433 - PostgreSQL
   # 6379 - Redis
   ```

2. **Kill Conflicting Processes**:
   ```bash
   # Kill process using port 5002
   sudo kill $(sudo lsof -t -i:5002)
   
   # Kill process using port 5433
   sudo kill $(sudo lsof -t -i:5433)
   
   # Kill process using port 6379
   sudo kill $(sudo lsof -t -i:6379)
   ```

3. **Restart Services**:
   ```bash
   # Restart Docker containers
   docker compose -f docker-compose.final.yml down
   docker compose -f docker-compose.final.yml up -d
   
   # Restart API service
   cd standalone-api
   dotnet run --urls "http://localhost:5002"
   ```

**✅ Port Configuration**:
```
API Service: localhost:5002
PostgreSQL: localhost:5433
Redis: localhost:6379
```

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

## 🎯 **Quick Resolution Guide**

### **Most Common Issues & Solutions**

#### **1. "API Service Not Responding"**
✅ **Solution**:
```bash
# Start infrastructure
docker compose -f docker-compose.final.yml up -d

# Start API service
cd standalone-api
dotnet run --urls "http://localhost:5002"

# Test
curl http://localhost:5002/health
```

#### **2. "Database Connection Failed"**
✅ **Solution**:
```bash
# Check PostgreSQL container
docker ps | grep neo-postgres

# Restart if needed
docker restart neo-postgres

# Test connection
curl http://localhost:5002/api/database/test
```

#### **3. "Redis Connection Failed"**
✅ **Solution**:
```bash
# Check Redis container
docker ps | grep neo-redis

# Restart if needed
docker restart neo-redis

# Test connection
curl http://localhost:5002/api/redis/test
```

#### **4. "Build Errors"**
✅ **Solution**:
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build

# Check .NET version
dotnet --version  # Should be 9.0+
```

#### **5. "Port Already in Use"**
✅ **Solution**:
```bash
# Kill process using port 5002
sudo kill $(sudo lsof -t -i:5002)

# Or use different port
dotnet run --urls "http://localhost:5003"
```

### **✅ Success Indicators**

**Healthy System**:
```bash
# All should return success
curl http://localhost:5002/health                # "Healthy"
curl http://localhost:5002/api/status            # All services healthy
curl http://localhost:5002/api/database/test     # Database connected
curl http://localhost:5002/api/redis/test        # Redis connected
docker ps                                        # 2 containers running
```

**Working Infrastructure**:
```bash
# Expected healthy containers
neo-postgres   postgres:16-alpine   Up (healthy)
neo-redis      redis:7-alpine       Up (healthy)
```

## 🆘 **Getting Help**

### **✅ Current Status: System Working**

The Neo Service Layer is now fully operational! All major issues have been resolved.

### **If You Still Need Help**

1. **Check Documentation**:
   - [Quick Start Guide](../deployment/QUICK_START.md) - 5-minute deployment
   - [Deployment Guide](../deployment/DEPLOYMENT_GUIDE.md) - Complete deployment
   - [API Documentation](../api/README.md) - API reference
   - [Architecture Overview](../architecture/ARCHITECTURE_OVERVIEW.md) - System architecture

2. **Quick Health Check**:
   ```bash
   # Run this to verify everything is working
   curl http://localhost:5002/health && echo " ✅ API: OK" || echo " ❌ API: FAIL"
   curl http://localhost:5002/api/status && echo " ✅ Services: OK" || echo " ❌ Services: FAIL"
   docker ps | grep -E '(neo-postgres|neo-redis)' && echo " ✅ Containers: OK" || echo " ❌ Containers: FAIL"
   ```

3. **Support Resources**:
   - **GitHub Issues**: [Neo Service Layer Issues](https://github.com/neo-project/neo-service-layer/issues)
   - **Neo Discord**: [Neo Community](https://discord.gg/neo)
   - **Documentation**: All documentation updated for working deployment

### **📚 Updated Documentation**

- **[Quick Start Guide](../deployment/QUICK_START.md)** - ✅ Working 5-minute deployment
- **[Deployment Guide](../deployment/DEPLOYMENT_GUIDE.md)** - ✅ Complete deployment instructions
- **[API Documentation](../api/README.md)** - ✅ Updated API reference
- **[Services Documentation](../services/README.md)** - ✅ Updated service information
- **[Architecture Overview](../architecture/ARCHITECTURE_OVERVIEW.md)** - ✅ Updated system architecture

---

## 🎉 **System Status: FULLY OPERATIONAL**

**✅ All Issues Resolved:**
- Infrastructure services running
- API service operational
- Database connectivity working
- Redis cache working
- Health monitoring active
- API documentation available
- All endpoints responding

**Built with ❤️ by the Neo Team**

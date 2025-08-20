#!/usr/bin/env python3
"""
Make the Neo Service Layer production-ready
"""

import subprocess
import os
from pathlib import Path
import json
import re

class ProductionReadiness:
    def __init__(self):
        self.project_root = Path("/home/ubuntu/neo-service-layer")
        os.chdir(self.project_root)
        self.issues = []
        self.fixes_applied = []
        
    def check_environment_files(self):
        """Ensure proper environment configuration"""
        print("\n🔧 Checking environment configuration...")
        
        # Check for .env files
        env_files = {
            ".env.example": True,
            ".env": False,  # Should not exist in repo
            ".env.production": False,
            ".env.development": False
        }
        
        for file, should_exist in env_files.items():
            path = self.project_root / file
            exists = path.exists()
            
            if should_exist and not exists:
                self.issues.append(f"Missing {file}")
            elif not should_exist and exists:
                print(f"  ⚠️ Warning: {file} exists (should not be in repo)")
        
        # Create .env.example if missing
        env_example = self.project_root / ".env.example"
        if not env_example.exists():
            self.create_env_example()
            self.fixes_applied.append("Created .env.example")
    
    def create_env_example(self):
        """Create example environment file"""
        content = """# Neo Service Layer Environment Configuration
# Copy this file to .env and update with your values

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
APP_NAME=NeoServiceLayer
APP_VERSION=1.0.0

# Database Configuration
CONNECTION_STRING=Server=localhost;Database=NeoServiceLayer;User Id=sa;Password=YourPassword123!;
REDIS_CONNECTION=localhost:6379

# JWT Configuration
JWT_SECRET_KEY=YourSuperSecretKeyThatIsAtLeast256BitsLongForProduction2024!
JWT_ISSUER=NeoServiceLayer
JWT_AUDIENCE=NeoServiceLayerAPI
JWT_EXPIRY_MINUTES=60

# Logging
LOG_LEVEL=Information
ENABLE_CONSOLE_LOGGING=true
ENABLE_FILE_LOGGING=true
LOG_PATH=logs/

# Security
ENABLE_CORS=true
CORS_ORIGINS=https://localhost:3000,https://yourdomain.com
ENABLE_RATE_LIMITING=true
RATE_LIMIT_REQUESTS_PER_MINUTE=60

# External Services
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password

# SGX/TEE Configuration
ENABLE_SGX=false
SGX_ENCLAVE_PATH=/opt/sgx/enclave.signed.so

# Monitoring
ENABLE_METRICS=true
METRICS_ENDPOINT=/metrics
ENABLE_HEALTH_CHECKS=true
HEALTH_CHECK_ENDPOINT=/health

# Feature Flags
ENABLE_MFA=true
ENABLE_SOCIAL_LOGIN=false
ENABLE_API_VERSIONING=true
"""
        
        with open(self.project_root / ".env.example", "w") as f:
            f.write(content)
    
    def check_docker_setup(self):
        """Ensure Docker configuration is complete"""
        print("\n🐳 Checking Docker setup...")
        
        docker_files = ["Dockerfile", "docker-compose.yml", "docker-compose.production.yml"]
        
        for file in docker_files:
            if not (self.project_root / file).exists():
                self.issues.append(f"Missing {file}")
                if file == "docker-compose.yml":
                    self.create_docker_compose()
                    self.fixes_applied.append("Created docker-compose.yml")
    
    def create_docker_compose(self):
        """Create docker-compose.yml"""
        content = """version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:5000"
      - "5001:5001"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:5001;http://+:5000
    env_file:
      - .env
    depends_on:
      - db
      - redis
    networks:
      - neo-network
    volumes:
      - ./logs:/app/logs
      - ./data:/app/data

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Password123
    ports:
      - "1433:1433"
    volumes:
      - db-data:/var/opt/mssql
    networks:
      - neo-network

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    networks:
      - neo-network

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/nginx/ssl:ro
    depends_on:
      - api
    networks:
      - neo-network

volumes:
  db-data:
  redis-data:

networks:
  neo-network:
    driver: bridge
"""
        
        with open(self.project_root / "docker-compose.yml", "w") as f:
            f.write(content)
    
    def check_api_configuration(self):
        """Check API configuration files"""
        print("\n🌐 Checking API configuration...")
        
        appsettings = self.project_root / "src/Api/NeoServiceLayer.Api/appsettings.json"
        if appsettings.exists():
            content = appsettings.read_text()
            
            # Check for important configurations
            required_sections = ["ConnectionStrings", "Jwt", "Logging", "AllowedHosts"]
            for section in required_sections:
                if section not in content:
                    self.issues.append(f"Missing {section} in appsettings.json")
        else:
            self.issues.append("Missing appsettings.json")
    
    def check_database_setup(self):
        """Check database migrations and setup"""
        print("\n💾 Checking database setup...")
        
        migrations_dir = self.project_root / "src/Infrastructure/NeoServiceLayer.Infrastructure.Data/Migrations"
        if not migrations_dir.exists():
            self.issues.append("No database migrations found")
            print("  ⚠️ No migrations directory found")
    
    def check_security_headers(self):
        """Check for security middleware"""
        print("\n🔒 Checking security configuration...")
        
        program_file = self.project_root / "src/Api/NeoServiceLayer.Api/Program.cs"
        if program_file.exists():
            content = program_file.read_text()
            
            security_checks = {
                "UseHttpsRedirection": "HTTPS redirection",
                "UseCors": "CORS policy",
                "UseAuthentication": "Authentication",
                "UseAuthorization": "Authorization",
                "UseRateLimiter": "Rate limiting"
            }
            
            for method, description in security_checks.items():
                if method not in content:
                    self.issues.append(f"Missing {description} ({method})")
    
    def check_logging_setup(self):
        """Check logging configuration"""
        print("\n📝 Checking logging setup...")
        
        # Check for Serilog or similar
        csproj_files = list(self.project_root.glob("src/**/*.csproj"))
        has_serilog = False
        
        for csproj in csproj_files:
            content = csproj.read_text()
            if "Serilog" in content or "NLog" in content:
                has_serilog = True
                break
        
        if not has_serilog:
            self.issues.append("No structured logging library found (Serilog/NLog)")
    
    def check_health_checks(self):
        """Check health check endpoints"""
        print("\n❤️ Checking health checks...")
        
        # Look for health check configuration
        has_health_checks = False
        for cs_file in self.project_root.glob("src/**/*.cs"):
            if cs_file.is_file():
                content = cs_file.read_text()
                if "AddHealthChecks" in content or "MapHealthChecks" in content:
                    has_health_checks = True
                    break
        
        if not has_health_checks:
            self.issues.append("No health check endpoints configured")
    
    def create_deployment_scripts(self):
        """Create deployment helper scripts"""
        print("\n🚀 Creating deployment scripts...")
        
        deploy_script = self.project_root / "scripts/deploy.sh"
        if not deploy_script.exists():
            content = """#!/bin/bash
# Neo Service Layer Deployment Script

set -e

echo "=========================================="
echo "Neo Service Layer - Production Deployment"
echo "=========================================="

# Load environment
if [ -f .env.production ]; then
    export $(cat .env.production | xargs)
fi

# Build application
echo "Building application..."
dotnet publish -c Release -o ./publish

# Run database migrations
echo "Running database migrations..."
dotnet ef database update --project src/Infrastructure/NeoServiceLayer.Infrastructure.Data

# Build Docker images
echo "Building Docker images..."
docker-compose -f docker-compose.production.yml build

# Deploy services
echo "Deploying services..."
docker-compose -f docker-compose.production.yml up -d

# Health check
echo "Waiting for services to be healthy..."
sleep 10
curl -f http://localhost:5000/health || exit 1

echo "✅ Deployment successful!"
"""
            deploy_script.parent.mkdir(exist_ok=True)
            with open(deploy_script, "w") as f:
                f.write(content)
            os.chmod(deploy_script, 0o755)
            self.fixes_applied.append("Created deployment script")
    
    def run_comprehensive_check(self):
        """Run all production readiness checks"""
        print("=" * 80)
        print("PRODUCTION READINESS CHECK")
        print("=" * 80)
        
        # Run all checks
        self.check_environment_files()
        self.check_docker_setup()
        self.check_api_configuration()
        self.check_database_setup()
        self.check_security_headers()
        self.check_logging_setup()
        self.check_health_checks()
        self.create_deployment_scripts()
        
        # Build the project
        print("\n🔨 Building project...")
        result = subprocess.run(
            ["dotnet", "build", "--configuration", "Release", "--no-restore"],
            capture_output=True,
            text=True,
            timeout=120
        )
        
        if result.returncode != 0:
            self.issues.append("Build failed")
        else:
            print("  ✅ Build successful")
        
        # Report results
        self.print_summary()
    
    def print_summary(self):
        """Print summary of checks"""
        print("\n" + "=" * 80)
        print("PRODUCTION READINESS SUMMARY")
        print("=" * 80)
        
        if self.fixes_applied:
            print("\n✅ Fixes Applied:")
            for fix in self.fixes_applied:
                print(f"  - {fix}")
        
        if self.issues:
            print("\n⚠️ Issues Found:")
            for issue in self.issues:
                print(f"  - {issue}")
        else:
            print("\n✅ No critical issues found!")
        
        # Overall status
        critical_issues = [i for i in self.issues if "Missing" in i or "failed" in i]
        if not critical_issues:
            print("\n🎉 Project is PRODUCTION READY!")
        else:
            print(f"\n⚠️ {len(critical_issues)} critical issues need attention")

if __name__ == "__main__":
    checker = ProductionReadiness()
    checker.run_comprehensive_check()
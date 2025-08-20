#!/bin/bash

# Script to update all vulnerable and outdated NuGet packages
echo "📦 Updating NuGet packages to latest secure versions..."

# Update Microsoft.Extensions packages to 9.0.8
PACKAGES=(
    "Microsoft.Extensions.Configuration"
    "Microsoft.Extensions.Configuration.Abstractions"
    "Microsoft.Extensions.Configuration.EnvironmentVariables"
    "Microsoft.Extensions.Configuration.Json"
    "Microsoft.Extensions.DependencyInjection.Abstractions"
    "Microsoft.Extensions.Diagnostics.HealthChecks"
    "Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions"
    "Microsoft.Extensions.Hosting.Abstractions"
    "Microsoft.Extensions.Http"
    "Microsoft.Extensions.Http.Polly"
    "Microsoft.Extensions.Logging.Abstractions"
    "Microsoft.Extensions.Caching.Memory"
    "Microsoft.Extensions.Caching.Distributed"
    "Microsoft.Extensions.Options"
)

echo "🔄 Updating Microsoft.Extensions packages to version 9.0.8..."
for package in "${PACKAGES[@]}"; do
    echo "  Updating $package..."
    dotnet add package "$package" --version 9.0.8 2>/dev/null || true
done

# Update System.Text.Json to latest
echo "🔄 Updating System.Text.Json to version 9.0.8..."
dotnet add package System.Text.Json --version 9.0.8

# Update Polly for resilience patterns
echo "🔄 Updating Polly packages..."
dotnet add package Polly --version 8.5.2
dotnet add package Microsoft.Extensions.Http.Polly --version 9.0.8

# Update security-related packages
echo "🔄 Updating security packages..."
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.8
dotnet add package Microsoft.IdentityModel.Tokens --version 8.3.2

# Restore packages
echo "📥 Restoring packages..."
dotnet restore NeoServiceLayer.sln

echo "✅ Package updates completed!"
echo ""
echo "📊 Checking for remaining vulnerabilities..."
dotnet list NeoServiceLayer.sln package --vulnerable --include-transitive

echo ""
echo "📋 Summary of outdated packages:"
dotnet list NeoServiceLayer.sln package --outdated | head -20
#!/bin/bash

echo "=== Comprehensive Package Update Script ==="
echo "This will update ALL packages to their latest versions"
echo ""

# Function to update package version in all csproj files
update_package() {
    local package_name=$1
    local new_version=$2
    echo "Updating $package_name to $new_version..."
    find . -name "*.csproj" -type f -exec sed -i "s/\"$package_name\" Version=\"[^\"]*\"/\"$package_name\" Version=\"$new_version\"/g" {} \;
}

# Update all Microsoft.Extensions packages to 9.0.0
echo "=== Updating Microsoft.Extensions packages to 9.0.0 ==="
update_package "Microsoft.Extensions.Logging" "9.0.0"
update_package "Microsoft.Extensions.Logging.Abstractions" "9.0.0"
update_package "Microsoft.Extensions.Logging.Console" "9.0.0"
update_package "Microsoft.Extensions.DependencyInjection" "9.0.0"
update_package "Microsoft.Extensions.DependencyInjection.Abstractions" "9.0.0"
update_package "Microsoft.Extensions.Configuration" "9.0.0"
update_package "Microsoft.Extensions.Configuration.Abstractions" "9.0.0"
update_package "Microsoft.Extensions.Configuration.Json" "9.0.0"
update_package "Microsoft.Extensions.Configuration.Binder" "9.0.0"
update_package "Microsoft.Extensions.Options" "9.0.0"
update_package "Microsoft.Extensions.Options.ConfigurationExtensions" "9.0.0"
update_package "Microsoft.Extensions.Hosting" "9.0.0"
update_package "Microsoft.Extensions.Hosting.Abstractions" "9.0.0"
update_package "Microsoft.Extensions.Http" "9.0.0"
update_package "Microsoft.Extensions.Diagnostics.HealthChecks" "9.0.0"
update_package "Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" "9.0.0"
update_package "Microsoft.Extensions.Caching.Memory" "9.0.0"

# Update Microsoft.AspNetCore packages to 9.0.0
echo ""
echo "=== Updating Microsoft.AspNetCore packages to 9.0.0 ==="
update_package "Microsoft.AspNetCore.OpenApi" "9.0.0"
update_package "Microsoft.AspNetCore.Authentication.JwtBearer" "9.0.0"
update_package "Microsoft.AspNetCore.Authorization" "9.0.0"
update_package "Microsoft.AspNetCore.Mvc.Testing" "9.0.0"
update_package "Microsoft.AspNetCore.TestHost" "9.0.0"

# Update ASP.NET Core MVC Versioning - these have changed namespace
echo ""
echo "=== Updating ASP.NET Core MVC Versioning ==="
find . -name "*.csproj" -type f -exec sed -i 's/"Microsoft\.AspNetCore\.Mvc\.Versioning" Version="[^"]*"/"Asp.Versioning.Mvc" Version="8.1.0"/g' {} \;
find . -name "*.csproj" -type f -exec sed -i 's/"Microsoft\.AspNetCore\.Mvc\.Versioning\.ApiExplorer" Version="[^"]*"/"Asp.Versioning.Mvc.ApiExplorer" Version="8.1.0"/g' {} \;

# Update System packages
echo ""
echo "=== Updating System packages ==="
update_package "System.Text.Json" "9.0.0"
update_package "System.ComponentModel.Annotations" "9.0.0"
update_package "System.Diagnostics.DiagnosticSource" "9.0.0"
update_package "System.Diagnostics.PerformanceCounter" "9.0.0"
update_package "System.IdentityModel.Tokens.Jwt" "8.2.1"

# Update test frameworks
echo ""
echo "=== Updating test frameworks ==="
update_package "Microsoft.NET.Test.Sdk" "17.13.0"
update_package "xunit" "2.9.3"
update_package "xunit.runner.visualstudio" "3.1.0"
update_package "NUnit" "4.3.0"
update_package "NUnit3TestAdapter" "4.6.0"
update_package "Moq" "4.20.72"
update_package "FluentAssertions" "7.0.0"  # Staying on 7.x to avoid license issues
update_package "coverlet.collector" "6.0.2"

# Update Serilog
echo ""
echo "=== Updating Serilog packages ==="
update_package "Serilog" "4.2.0"
update_package "Serilog.AspNetCore" "9.0.0"
update_package "Serilog.Sinks.Console" "6.0.0"
update_package "Serilog.Sinks.File" "6.0.0"

# Update other packages
echo ""
echo "=== Updating other packages ==="
update_package "Swashbuckle.AspNetCore" "7.2.0"
update_package "Npgsql" "9.0.2"
update_package "StackExchange.Redis" "2.9.0"
update_package "AutoFixture" "5.0.0"
update_package "BenchmarkDotNet" "0.14.0"
update_package "NBomber" "6.0.0"

# Update Nethereum to v5
echo ""
echo "=== Updating Nethereum packages to v5 ==="
update_package "Nethereum.Web3" "5.0.0"
update_package "Nethereum.Contracts" "5.0.0"
update_package "Nethereum.RPC" "5.0.0"
update_package "Nethereum.JsonRpc.Client" "5.0.0"

# Update Neo packages to latest
echo ""
echo "=== Checking Neo packages ==="
update_package "Neo" "3.9.2"
update_package "Neo.Json" "3.9.2"
update_package "Neo.VM" "3.9.2"

# Update ML packages
echo ""
echo "=== Updating ML packages ==="
update_package "Microsoft.ML" "4.0.0"
update_package "Microsoft.ML.TimeSeries" "4.0.0"
update_package "Microsoft.ML.AutoML" "0.22.0"
update_package "Microsoft.ML.FastTree" "4.0.0"
update_package "Microsoft.ML.LightGbm" "4.0.0"

# Update HealthChecks packages
echo ""
echo "=== Updating HealthChecks packages ==="
update_package "AspNetCore.HealthChecks.UI" "9.0.0"
update_package "AspNetCore.HealthChecks.UI.InMemory.Storage" "9.0.0"

# Force update any remaining vulnerable packages
echo ""
echo "=== Force updating known vulnerable packages ==="
# Remove any old System.Net.Http references (included in SDK)
find . -name "*.csproj" -type f -exec sed -i '/"System\.Net\.Http" Version="4\./d' {} \;
# Remove any old System.Text.RegularExpressions references (included in SDK)
find . -name "*.csproj" -type f -exec sed -i '/"System\.Text\.RegularExpressions" Version="4\./d' {} \;

echo ""
echo "=== Package updates complete! ==="
echo ""
echo "Next steps:"
echo "1. Run 'dotnet restore' to restore packages"
echo "2. Run 'dotnet build' to check for compilation errors"
echo "3. Run 'dotnet test' to ensure tests pass"
echo "4. Run 'dotnet list package --vulnerable --include-transitive' to check for remaining vulnerabilities"
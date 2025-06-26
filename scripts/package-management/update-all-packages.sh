#!/bin/bash

echo "Updating all packages to latest versions..."

# Update all Microsoft.Extensions packages from 8.0.0 to 9.0.6
find . -name "*.csproj" -type f -exec sed -i 's/Microsoft\.Extensions\.[^"]*" Version="8\.0\.[0-9]"/&" Version="9.0.6"/g' {} \;

# Update all Microsoft.AspNetCore packages from 8.0.0 to 9.0.6
find . -name "*.csproj" -type f -exec sed -i 's/Microsoft\.AspNetCore\.[^"]*" Version="8\.0\.[0-9]"/&" Version="9.0.6"/g' {} \;

# Update System.Text.Json to latest
find . -name "*.csproj" -type f -exec sed -i 's/System\.Text\.Json" Version="8\.0\.[0-9]"/System.Text.Json" Version="9.0.1"/g' {} \;

# Update xUnit packages
find . -name "*.csproj" -type f -exec sed -i 's/xunit" Version="2\.[0-9]\.[0-9]"/xunit" Version="2.9.3"/g' {} \;
find . -name "*.csproj" -type f -exec sed -i 's/xunit\.runner\.visualstudio" Version="2\.[0-9]\.[0-9]"/xunit.runner.visualstudio" Version="3.1.1"/g' {} \;

# Update Microsoft.NET.Test.Sdk
find . -name "*.csproj" -type f -exec sed -i 's/Microsoft\.NET\.Test\.Sdk" Version="17\.[0-9]\.[0-9]"/Microsoft.NET.Test.Sdk" Version="17.13.0"/g' {} \;

# Update Moq
find . -name "*.csproj" -type f -exec sed -i 's/Moq" Version="4\.20\.[0-9][0-9]"/Moq" Version="4.20.72"/g' {} \;

# Update FluentAssertions (staying on 7.0.0 to avoid license issues)
find . -name "*.csproj" -type f -exec sed -i 's/FluentAssertions" Version="6\.12\.[0-9]"/FluentAssertions" Version="7.0.0"/g' {} \;

# Update Serilog
find . -name "*.csproj" -type f -exec sed -i 's/Serilog" Version="[0-9]\.[0-9]\.[0-9]"/Serilog" Version="4.2.0"/g' {} \;
find . -name "*.csproj" -type f -exec sed -i 's/Serilog\.AspNetCore" Version="8\.[0-9]\.[0-9]"/Serilog.AspNetCore" Version="9.0.0"/g' {} \;

# Update Swashbuckle
find . -name "*.csproj" -type f -exec sed -i 's/Swashbuckle\.AspNetCore" Version="[0-9]\.[0-9]\.[0-9]"/Swashbuckle.AspNetCore" Version="7.2.0"/g' {} \;

# Update System.ComponentModel.Annotations
find . -name "*.csproj" -type f -exec sed -i 's/System\.ComponentModel\.Annotations" Version="5\.0\.0"/System.ComponentModel.Annotations" Version="9.0.0"/g' {} \;

# Update Nethereum to latest v5
find . -name "*.csproj" -type f -exec sed -i 's/Nethereum\.[^"]*" Version="4\.[0-9][0-9]\.[0-9]"/&" Version="5.0.0"/g' {} \;

# Update ASP.NET Core MVC Versioning (these need special handling)
find . -name "*.csproj" -type f -exec sed -i 's/Microsoft\.AspNetCore\.Mvc\.Versioning" Version="5\.1\.0"/Asp.Versioning.Mvc" Version="8.1.0"/g' {} \;
find . -name "*.csproj" -type f -exec sed -i 's/Microsoft\.AspNetCore\.Mvc\.Versioning\.ApiExplorer" Version="5\.1\.0"/Asp.Versioning.Mvc.ApiExplorer" Version="8.1.0"/g' {} \;

echo "Direct package updates complete!"
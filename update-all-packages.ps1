# PowerShell script to update all packages to latest versions
# This will update everything to the absolute latest without caring about compatibility

Write-Host "Updating all packages to latest versions..." -ForegroundColor Green

# Update all Microsoft.Extensions packages to latest (9.x)
$extensions = @(
    "Microsoft.Extensions.Logging",
    "Microsoft.Extensions.Logging.Abstractions",
    "Microsoft.Extensions.Logging.Console",
    "Microsoft.Extensions.DependencyInjection",
    "Microsoft.Extensions.DependencyInjection.Abstractions",
    "Microsoft.Extensions.Configuration",
    "Microsoft.Extensions.Configuration.Abstractions",
    "Microsoft.Extensions.Configuration.Json",
    "Microsoft.Extensions.Options",
    "Microsoft.Extensions.Options.ConfigurationExtensions",
    "Microsoft.Extensions.Hosting",
    "Microsoft.Extensions.Hosting.Abstractions",
    "Microsoft.Extensions.Http",
    "Microsoft.Extensions.Diagnostics.HealthChecks",
    "Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore",
    "Microsoft.Extensions.Caching.Memory"
)

foreach ($package in $extensions) {
    Write-Host "Updating $package..." -ForegroundColor Yellow
    dotnet add package $package --version 9.* 2>$null
}

# Update ASP.NET Core packages
$aspnetcore = @(
    "Microsoft.AspNetCore.OpenApi",
    "Microsoft.AspNetCore.Authentication.JwtBearer",
    "Microsoft.AspNetCore.Authorization",
    "Microsoft.AspNetCore.Mvc.Testing",
    "Microsoft.AspNetCore.TestHost"
)

foreach ($package in $aspnetcore) {
    Write-Host "Updating $package..." -ForegroundColor Yellow
    dotnet add package $package --version 9.* 2>$null
}

# Update test frameworks
Write-Host "Updating test frameworks..." -ForegroundColor Yellow
dotnet add package xunit --version 2.9.3 2>$null
dotnet add package xunit.runner.visualstudio --version 3.1.1 2>$null
dotnet add package Microsoft.NET.Test.Sdk --version 17.* 2>$null
dotnet add package Moq --version 4.20.72 2>$null
dotnet add package FluentAssertions --version 7.0.0 2>$null  # Staying on 7.x to avoid license issues
dotnet add package NUnit --version 4.* 2>$null
dotnet add package NUnit3TestAdapter --version 4.* 2>$null

# Update other packages
Write-Host "Updating other packages..." -ForegroundColor Yellow
dotnet add package Swashbuckle.AspNetCore --version 7.* 2>$null
dotnet add package Serilog.AspNetCore --version 9.* 2>$null
dotnet add package Serilog --version 4.* 2>$null
dotnet add package System.IdentityModel.Tokens.Jwt --version 8.* 2>$null
dotnet add package Npgsql --version 8.* 2>$null
dotnet add package StackExchange.Redis --version 2.* 2>$null
dotnet add package System.ComponentModel.Annotations --version 9.* 2>$null

# Update blockchain packages
Write-Host "Updating blockchain packages..." -ForegroundColor Yellow
dotnet add package Nethereum.Web3 --version 5.* 2>$null
dotnet add package Nethereum.Contracts --version 5.* 2>$null
dotnet add package Nethereum.RPC --version 5.* 2>$null
dotnet add package Nethereum.JsonRpc.Client --version 5.* 2>$null

Write-Host "Package updates complete!" -ForegroundColor Green
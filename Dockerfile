# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["NeoServiceLayer.sln", "./"]
COPY ["src/", "src/"]
COPY ["tests/", "tests/"]

# Restore dependencies
RUN dotnet restore "src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj" || true

# Build
RUN dotnet build "src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj" -c Release --no-restore || true

# Publish
RUN dotnet publish "src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj" -c Release -o /app/publish --no-restore || true

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Create directories for logs and data
RUN mkdir -p /app/logs /app/data

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
ENV ASPNETCORE_ENVIRONMENT=Development

# Entry point
ENTRYPOINT ["dotnet", "NeoServiceLayer.Api.dll"]
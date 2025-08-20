# Multi-stage build for Neo Service Layer
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY src/**/*.csproj ./src/
COPY tests/**/*.csproj ./tests/

# Restore project structure
RUN for file in $(find . -name "*.csproj"); do \
    dir=$(dirname $file); \
    mkdir -p /src/$dir && \
    mv $file /src/$dir/; \
    done

# Copy source code
COPY . .

# Restore dependencies
RUN dotnet restore NeoServiceLayer.sln

# Build the solution
RUN dotnet build NeoServiceLayer.sln -c Release --no-restore

# Publish the API project
RUN dotnet publish src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install Intel SGX dependencies (if available)
RUN apt-get update && apt-get install -y \
    libsgx-launch \
    libsgx-epid \
    libsgx-quote-ex \
    libsgx-urts \
    && rm -rf /var/lib/apt/lists/* || true

# Copy published application
COPY --from=build /app/publish .

# Configure environment
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

# Expose ports
EXPOSE 5000

# Run the application
ENTRYPOINT ["dotnet", "NeoServiceLayer.Api.dll"]
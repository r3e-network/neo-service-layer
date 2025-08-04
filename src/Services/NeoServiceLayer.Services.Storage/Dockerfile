# Multi-stage build for NeoServiceLayer.Services.Storage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY *.sln .
COPY Directory.*.props .
COPY Directory.*.targets .
COPY NuGet.Config .

# Copy all project files for proper restore
COPY src/ src/
COPY tests/ tests/

# Restore packages
RUN dotnet restore "src/Services/NeoServiceLayer.Services.Storage/NeoServiceLayer.Services.Storage.csproj"

# Build the service
RUN dotnet build "src/Services/NeoServiceLayer.Services.Storage/NeoServiceLayer.Services.Storage.csproj" -c Release --no-restore

# Publish the service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.Storage/NeoServiceLayer.Services.Storage.csproj" -c Release --no-build -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Create non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8021/health || exit 1

# Expose port
EXPOSE 8021

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8021
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.Storage.dll"]

# Multi-stage build for NeoServiceLayer.AI.Prediction
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
RUN dotnet restore "src/AI/NeoServiceLayer.AI.Prediction/NeoServiceLayer.AI.Prediction.csproj"

# Build the service
RUN dotnet build "src/AI/NeoServiceLayer.AI.Prediction/NeoServiceLayer.AI.Prediction.csproj" -c Release --no-restore

# Publish the service
RUN dotnet publish "src/AI/NeoServiceLayer.AI.Prediction/NeoServiceLayer.AI.Prediction.csproj" -c Release --no-build -o /app/publish

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
    CMD curl -f http://localhost:8025/health || exit 1

# Expose port
EXPOSE 8025

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8025
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "NeoServiceLayer.AI.Prediction.dll"]

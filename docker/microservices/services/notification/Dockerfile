# Dockerfile for Notification Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.Notification/NeoServiceLayer.Services.Notification.csproj", "src/Services/NeoServiceLayer.Services.Notification/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.Notification/NeoServiceLayer.Services.Notification.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.Notification/", "src/Services/NeoServiceLayer.Services.Notification/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.Notification/NeoServiceLayer.Services.Notification.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.Notification/NeoServiceLayer.Services.Notification.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage
FROM ${RUNTIME_BASE_IMAGE} AS final
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Service-specific environment variables
ENV SERVICE_NAME=NotificationService
ENV SERVICE_TYPE=Notification
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - Notification Service"
LABEL service.type="notification"
LABEL service.version="1.0.0"

# Create service host program
RUN echo '#!/bin/sh' > /app/run-service.sh && \
    echo 'exec dotnet NeoServiceLayer.Services.Notification.dll' >> /app/run-service.sh && \
    chmod +x /app/run-service.sh

ENTRYPOINT ["/app/run-service.sh"]
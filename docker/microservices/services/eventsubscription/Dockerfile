# Dockerfile for EventSubscription Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.EventSubscription/NeoServiceLayer.Services.EventSubscription.csproj", "src/Services/NeoServiceLayer.Services.EventSubscription/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.EventSubscription/NeoServiceLayer.Services.EventSubscription.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.EventSubscription/", "src/Services/NeoServiceLayer.Services.EventSubscription/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.EventSubscription/NeoServiceLayer.Services.EventSubscription.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.EventSubscription/NeoServiceLayer.Services.EventSubscription.csproj" \
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
ENV SERVICE_NAME=EventSubscriptionService
ENV SERVICE_TYPE=EventSubscription
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - EventSubscription Service"
LABEL service.type="eventsubscription"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/EventSubscriptionServiceHost.cs
RUN sed -i "s/{ServiceName}/EventSubscription/g" /app/EventSubscriptionServiceHost.cs && \
    sed -i "s/{servicename}/eventsubscription/g" /app/EventSubscriptionServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.EventSubscription.dll"]

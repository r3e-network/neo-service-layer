# Dockerfile for Voting Service
ARG BUILD_BASE_IMAGE=neoservicelayer/build-base:latest
ARG RUNTIME_BASE_IMAGE=neoservicelayer/runtime-base:latest

# Build stage
FROM ${BUILD_BASE_IMAGE} AS build
WORKDIR /src

# Copy service-specific project files
COPY ["src/Services/NeoServiceLayer.Services.Voting/NeoServiceLayer.Services.Voting.csproj", "src/Services/NeoServiceLayer.Services.Voting/"]

# Restore service dependencies
RUN dotnet restore "src/Services/NeoServiceLayer.Services.Voting/NeoServiceLayer.Services.Voting.csproj"

# Copy service source files
COPY ["src/Services/NeoServiceLayer.Services.Voting/", "src/Services/NeoServiceLayer.Services.Voting/"]

# Build service
RUN dotnet build "src/Services/NeoServiceLayer.Services.Voting/NeoServiceLayer.Services.Voting.csproj" -c Release --no-restore

# Publish service
RUN dotnet publish "src/Services/NeoServiceLayer.Services.Voting/NeoServiceLayer.Services.Voting.csproj" \
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
ENV SERVICE_NAME=VotingService
ENV SERVICE_TYPE=Voting
ENV SERVICE_PORT=80

# Service-specific labels
LABEL service.name="Neo Service Layer - Voting Service"
LABEL service.type="voting"
LABEL service.version="1.0.0"

# Create service host program
COPY --from=build /src/src/Services/ServiceHostTemplate/Program.cs /app/VotingServiceHost.cs
RUN sed -i "s/{ServiceName}/Voting/g" /app/VotingServiceHost.cs && \
    sed -i "s/{servicename}/voting/g" /app/VotingServiceHost.cs

ENTRYPOINT ["dotnet", "NeoServiceLayer.Services.Voting.dll"]

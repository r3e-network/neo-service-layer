# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj", "src/NeoServiceLayer.Api/"]
COPY ["src/NeoServiceLayer.Core/NeoServiceLayer.Core.csproj", "src/NeoServiceLayer.Core/"]
COPY ["src/NeoServiceLayer.Infrastructure/NeoServiceLayer.Infrastructure.csproj", "src/NeoServiceLayer.Infrastructure/"]
COPY ["src/NeoServiceLayer.Tee.Host/NeoServiceLayer.Tee.Host.csproj", "src/NeoServiceLayer.Tee.Host/"]
COPY ["src/NeoServiceLayer.Tee.Enclave/NeoServiceLayer.Tee.Enclave.csproj", "src/NeoServiceLayer.Tee.Enclave/"]
COPY ["src/NeoServiceLayer.Shared/NeoServiceLayer.Shared.csproj", "src/NeoServiceLayer.Shared/"]
RUN dotnet restore "src/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj"

# Copy the rest of the code
COPY . .

# Build the application
RUN dotnet build "src/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "src/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install Occlum and SGX dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        wget \
        gnupg \
        apt-transport-https \
        ca-certificates \
        build-essential \
        cmake \
        libssl-dev \
        && \
    echo "deb [arch=amd64] https://download.01.org/intel-sgx/sgx_repo/ubuntu focal main" | tee /etc/apt/sources.list.d/intel-sgx.list && \
    wget -qO - https://download.01.org/intel-sgx/sgx_repo/ubuntu/intel-sgx-deb.key | apt-key add - && \
    apt-get update && \
    apt-get install -y --no-install-recommends \
        libsgx-enclave-common \
        libsgx-quote-ex \
        libsgx-dcap-ql \
        libsgx-dcap-ql-dev \
        az-dcap-client \
        && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Install Node.js
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        curl \
        && \
    curl -fsSL https://deb.nodesource.com/setup_16.x | bash - && \
    apt-get install -y --no-install-recommends \
        nodejs \
        && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=publish /app/publish .

# Create a non-root user to run the application
RUN useradd -m neoserviceuser
USER neoserviceuser

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV Tee__SimulationMode=true
ENV Tee__Type=Occlum
ENV OCCLUM_SIMULATION=1

# Expose ports
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "NeoServiceLayer.Api.dll"]

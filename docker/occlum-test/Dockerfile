# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the solution file and restore dependencies
COPY ["NeoServiceLayer.sln", "./"]
COPY ["src/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj", "src/NeoServiceLayer.Api/"]
COPY ["src/NeoServiceLayer.Core/NeoServiceLayer.Core.csproj", "src/NeoServiceLayer.Core/"]
COPY ["src/NeoServiceLayer.Infrastructure/NeoServiceLayer.Infrastructure.csproj", "src/NeoServiceLayer.Infrastructure/"]
COPY ["src/NeoServiceLayer.Tee.Host/NeoServiceLayer.Tee.Host.csproj", "src/NeoServiceLayer.Tee.Host/"]
COPY ["src/NeoServiceLayer.Tee.Enclave/NeoServiceLayer.Tee.Enclave.csproj", "src/NeoServiceLayer.Tee.Enclave/"]
COPY ["src/NeoServiceLayer.Shared/NeoServiceLayer.Shared.csproj", "src/NeoServiceLayer.Shared/"]

RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "src/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "src/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj" -c Release -o /app/publish

# Stage 3: Build the Occlum image
FROM occlum/occlum:0.29.5-ubuntu20.04 AS occlum-builder

# Install Node.js
RUN apt-get update && apt-get install -y curl
RUN curl -fsSL https://deb.nodesource.com/setup_16.x | bash -
RUN apt-get install -y nodejs

# Create an Occlum instance
WORKDIR /occlum_instance
RUN occlum init
RUN mkdir -p image/bin image/lib image/etc image/node_modules image/tmp

# Copy Node.js and dependencies to the Occlum instance
RUN cp $(which node) image/bin/
RUN cp -r /usr/lib/x86_64-linux-gnu/libnode* image/lib/
RUN cp /etc/hosts image/etc/
RUN cp /etc/resolv.conf image/etc/

# Install required Node.js modules
WORKDIR /tmp
RUN npm init -y
RUN npm install fs-extra crypto-js uuid axios
RUN cp -r node_modules/* /occlum_instance/image/node_modules/

# Configure Occlum instance
WORKDIR /occlum_instance
RUN echo '{"resource_limits": {"user_space_size": "1GB", "kernel_space_heap_size": "64MB", "kernel_space_stack_size": "1MB", "max_num_of_threads": 32}, "process": {"default_stack_size": "4MB", "default_heap_size": "32MB", "default_mmap_size": "500MB"}, "entry_points": ["/bin/node"], "env": {"LD_LIBRARY_PATH": "/lib:/usr/lib:/usr/local/lib", "PATH": "/bin:/usr/bin", "OCCLUM": "yes"}}' > Occlum.json

# Build the Occlum instance
RUN occlum build

# Stage 4: Final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Install dependencies for OpenEnclave and Occlum
RUN apt-get update && apt-get install -y \
    build-essential \
    cmake \
    libssl-dev \
    libcurl4-openssl-dev \
    pkg-config \
    && rm -rf /var/lib/apt/lists/*

# Copy the published application
WORKDIR /app
COPY --from=publish /app/publish .

# Copy the Occlum instance
COPY --from=occlum-builder /occlum_instance /occlum_instance

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV Tee__SimulationMode=true
ENV Tee__OcclumSupport=true
ENV Tee__OcclumInstanceDir=/occlum_instance
ENV Tee__OcclumLogLevel=info

# Expose the API port
EXPOSE 5000

# Set the entry point
ENTRYPOINT ["dotnet", "NeoServiceLayer.Api.dll"]

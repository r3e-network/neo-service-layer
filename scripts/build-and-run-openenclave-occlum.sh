#!/bin/bash

# Build and run the Neo Service Layer with Open Enclave and Occlum

# Set environment variables
export OE_SIMULATION=1
export OCCLUM_RELEASE_ENCLAVE=1

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "Docker is not installed. Please install Docker and try again."
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo "Docker Compose is not installed. Please install Docker Compose and try again."
    exit 1
fi

# Build the Docker image
echo "Building the Docker image..."
docker-compose -f docker/openenclave-occlum/docker-compose.yml build

# Run the Docker container
echo "Running the Docker container..."
docker-compose -f docker/openenclave-occlum/docker-compose.yml up -d

# Wait for the container to start
echo "Waiting for the container to start..."
sleep 5

# Check if the container is running
containerId=$(docker ps -q -f "name=neo-service-layer")
if [ -z "$containerId" ]; then
    echo "Failed to start the container. Please check the logs."
    docker-compose -f docker/openenclave-occlum/docker-compose.yml logs
    exit 1
fi

# Print the container logs
echo "Container logs:"
docker logs $containerId

# Print the URL
echo "Neo Service Layer is running at http://localhost:5000"

# Run the tests
echo "Running the tests..."
dotnet test tests/NeoServiceLayer.Tee.Enclave.Tests --filter "Category=OpenEnclave"
dotnet test tests/NeoServiceLayer.Integration.Tests --filter "Category=OpenEnclave"

echo "Done!"

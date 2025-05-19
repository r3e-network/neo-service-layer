# Build and run the Neo Service Layer with Open Enclave and Occlum

# Set environment variables
$env:OE_SIMULATION = "1"
$env:OCCLUM_RELEASE_ENCLAVE = "1"

# Check if Docker is installed
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker is not installed. Please install Docker and try again."
    exit 1
}

# Check if Docker Compose is installed
if (-not (Get-Command docker-compose -ErrorAction SilentlyContinue)) {
    Write-Error "Docker Compose is not installed. Please install Docker Compose and try again."
    exit 1
}

# Build the Docker image
Write-Host "Building the Docker image..."
docker-compose -f docker/openenclave-occlum/docker-compose.yml build

# Run the Docker container
Write-Host "Running the Docker container..."
docker-compose -f docker/openenclave-occlum/docker-compose.yml up -d

# Wait for the container to start
Write-Host "Waiting for the container to start..."
Start-Sleep -Seconds 5

# Check if the container is running
$containerId = docker ps -q -f "name=neo-service-layer"
if (-not $containerId) {
    Write-Error "Failed to start the container. Please check the logs."
    docker-compose -f docker/openenclave-occlum/docker-compose.yml logs
    exit 1
}

# Print the container logs
Write-Host "Container logs:"
docker logs $containerId

# Print the URL
Write-Host "Neo Service Layer is running at http://localhost:5000"

# Run the tests
Write-Host "Running the tests..."
dotnet test tests/NeoServiceLayer.Tee.Enclave.Tests --filter "Category=OpenEnclave"
dotnet test tests/NeoServiceLayer.Integration.Tests --filter "Category=OpenEnclave"

Write-Host "Done!"

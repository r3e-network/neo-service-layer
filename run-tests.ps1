# Build the Docker image for testing
docker build -t neoservicelayer-test -f Dockerfile.test .

# Run the tests in the Docker container
docker run --rm neoservicelayer-test

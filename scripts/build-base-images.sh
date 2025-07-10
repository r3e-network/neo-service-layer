#!/bin/bash

# Script to build base Docker images for Neo Service Layer microservices

set -e

echo "Building Neo Service Layer base images..."

# Build the build base image
echo "Building build-base image..."
docker build -t neoservicelayer/build-base:latest -f docker/base/build-base.Dockerfile .

# Build the runtime base image
echo "Building runtime-base image..."
docker build -t neoservicelayer/runtime-base:latest -f docker/base/runtime-base.Dockerfile .

echo "Base images built successfully!"
echo "Images created:"
echo "  - neoservicelayer/build-base:latest"
echo "  - neoservicelayer/runtime-base:latest"

# Optionally tag with version
if [ -n "$1" ]; then
    echo "Tagging with version: $1"
    docker tag neoservicelayer/build-base:latest neoservicelayer/build-base:$1
    docker tag neoservicelayer/runtime-base:latest neoservicelayer/runtime-base:$1
fi

echo "Done!"
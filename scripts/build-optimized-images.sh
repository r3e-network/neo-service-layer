#!/bin/bash

# Optimized Docker Image Build Script for Neo Service Layer
set -e

# Configuration
REGISTRY="${DOCKER_REGISTRY:-neoservicelayer}"
TAG="${BUILD_TAG:-latest}"
PLATFORM="${BUILD_PLATFORM:-linux/amd64,linux/arm64}"
BUILD_CONTEXT="${BUILD_CONTEXT:-.}"

echo "🚀 Building Optimized Neo Service Layer Docker Images"
echo "Registry: $REGISTRY"
echo "Tag: $TAG"
echo "Platform: $PLATFORM"

# Function to build multi-arch images with BuildKit
build_image() {
    local service=$1
    local dockerfile=$2
    local context=$3
    local target=${4:-""}
    
    echo "📦 Building $service image..."
    
    # Create builder if it doesn't exist
    if ! docker buildx inspect neo-builder >/dev/null 2>&1; then
        echo "Creating buildx builder..."
        docker buildx create --name neo-builder --driver docker-container --bootstrap
    fi
    
    # Use the builder
    docker buildx use neo-builder
    
    # Build arguments
    local build_args=(
        --platform "$PLATFORM"
        --tag "$REGISTRY/$service:$TAG"
        --tag "$REGISTRY/$service:latest"
        --file "$dockerfile"
        --context "$context"
        --build-arg "BUILD_DATE=$(date -u +'%Y-%m-%dT%H:%M:%SZ')"
        --build-arg "VCS_REF=$(git rev-parse --short HEAD)"
        --build-arg "VERSION=$TAG"
    )
    
    # Add target if specified
    if [ -n "$target" ]; then
        build_args+=(--target "$target")
    fi
    
    # Add cache arguments
    build_args+=(
        --cache-from "type=registry,ref=$REGISTRY/$service:buildcache"
        --cache-to "type=registry,ref=$REGISTRY/$service:buildcache,mode=max"
    )
    
    # Push if not in local mode
    if [ "${PUSH_IMAGES:-true}" = "true" ]; then
        build_args+=(--push)
    else
        build_args+=(--load)
    fi
    
    # Execute build
    docker buildx build "${build_args[@]}"
    
    echo "✅ $service image built successfully"
}

# Function to optimize image with dive
optimize_image() {
    local image=$1
    
    echo "🔍 Analyzing image efficiency: $image"
    
    if command -v dive >/dev/null 2>&1; then
        dive "$image" --ci --lowestEfficiency=0.95 --highestWastedBytes=100MB
    else
        echo "ℹ️  Install 'dive' for detailed image analysis: https://github.com/wagoodman/dive"
    fi
}

# Function to scan image for vulnerabilities
scan_image() {
    local image=$1
    
    echo "🔒 Scanning $image for vulnerabilities..."
    
    if command -v trivy >/dev/null 2>&1; then
        trivy image --exit-code 1 --severity HIGH,CRITICAL "$image"
    elif command -v docker >/dev/null 2>&1; then
        # Use Docker Scout if available
        if docker scout version >/dev/null 2>&1; then
            docker scout cves "$image"
        else
            echo "ℹ️  Install 'trivy' or 'docker scout' for vulnerability scanning"
        fi
    fi
}

# Function to generate SBOM
generate_sbom() {
    local image=$1
    local service=$2
    
    echo "📋 Generating SBOM for $service..."
    
    if command -v syft >/dev/null 2>&1; then
        syft "$image" -o spdx-json --file "sbom-$service.spdx.json"
        echo "✅ SBOM generated: sbom-$service.spdx.json"
    else
        echo "ℹ️  Install 'syft' for SBOM generation: https://github.com/anchore/syft"
    fi
}

# Main build process
main() {
    echo "🔧 Preparing build environment..."
    
    # Enable Docker BuildKit
    export DOCKER_BUILDKIT=1
    export DOCKER_CLI_EXPERIMENTAL=enabled
    
    # Clean up any existing builders
    docker buildx rm neo-builder >/dev/null 2>&1 || true
    
    # Build API Gateway service
    build_image "api-gateway" "Dockerfile.optimized" "." "runtime"
    
    # Build Oracle service
    build_image "oracle-service" "Dockerfile.optimized" "." "runtime"
    
    # Build CrossChain service
    build_image "crosschain-service" "Dockerfile.optimized" "." "runtime"
    
    # Build specialized images if needed
    if [ "${BUILD_SPECIALIZED:-false}" = "true" ]; then
        # Build development image (with debugging tools)
        build_image "api-gateway-debug" "Dockerfile.optimized" "." "build-env"
        
        # Build minimal image (stripped down for edge/IoT)
        build_image "api-gateway-minimal" "Dockerfile.minimal" "."
    fi
    
    echo "🎉 All images built successfully!"
    
    # Post-build analysis and security scanning
    if [ "${ANALYZE_IMAGES:-true}" = "true" ]; then
        echo "📊 Running post-build analysis..."
        
        local images=(
            "$REGISTRY/api-gateway:$TAG"
            "$REGISTRY/oracle-service:$TAG"
            "$REGISTRY/crosschain-service:$TAG"
        )
        
        for image in "${images[@]}"; do
            service=$(echo "$image" | cut -d'/' -f2 | cut -d':' -f1)
            
            # Optimize image analysis
            optimize_image "$image"
            
            # Security scanning
            if [ "${SECURITY_SCAN:-true}" = "true" ]; then
                scan_image "$image"
            fi
            
            # SBOM generation
            if [ "${GENERATE_SBOM:-true}" = "true" ]; then
                generate_sbom "$image" "$service"
            fi
            
            # Image size analysis
            echo "📐 Image size for $service:"
            docker images "$image" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}"
        done
    fi
    
    # Tag images for different environments
    if [ "${TAG_ENVIRONMENTS:-true}" = "true" ]; then
        echo "🏷️  Tagging images for different environments..."
        
        local services=("api-gateway" "oracle-service" "crosschain-service")
        local environments=("dev" "staging" "prod")
        
        for service in "${services[@]}"; do
            for env in "${environments[@]}"; do
                local env_tag="$TAG-$env"
                docker tag "$REGISTRY/$service:$TAG" "$REGISTRY/$service:$env_tag"
                
                if [ "${PUSH_IMAGES:-true}" = "true" ]; then
                    docker push "$REGISTRY/$service:$env_tag"
                fi
            done
        done
    fi
    
    # Clean up builder
    docker buildx rm neo-builder >/dev/null 2>&1 || true
    
    echo ""
    echo "🎯 Build Summary:"
    echo "=================="
    docker images "$REGISTRY/*:$TAG" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"
    
    echo ""
    echo "🚀 Next Steps:"
    echo "- Deploy to staging: kubectl apply -k k8s/overlays/staging"
    echo "- Run security tests: ./scripts/security-test.sh"
    echo "- Performance testing: ./scripts/load-test.sh"
    echo "- Deploy to production: ./scripts/deploy-production.sh"
}

# Command line options
while [[ $# -gt 0 ]]; do
    case $1 in
        --registry=*)
            REGISTRY="${1#*=}"
            shift
            ;;
        --tag=*)
            TAG="${1#*=}"
            shift
            ;;
        --platform=*)
            PLATFORM="${1#*=}"
            shift
            ;;
        --no-push)
            PUSH_IMAGES="false"
            shift
            ;;
        --no-analyze)
            ANALYZE_IMAGES="false"
            shift
            ;;
        --no-scan)
            SECURITY_SCAN="false"
            shift
            ;;
        --specialized)
            BUILD_SPECIALIZED="true"
            shift
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --registry=NAME     Docker registry (default: neoservicelayer)"
            echo "  --tag=TAG           Image tag (default: latest)"
            echo "  --platform=PLATFORMS Target platforms (default: linux/amd64,linux/arm64)"
            echo "  --no-push           Don't push images to registry"
            echo "  --no-analyze        Skip image analysis"
            echo "  --no-scan           Skip security scanning"
            echo "  --specialized       Build specialized variants"
            echo "  --help              Show this help"
            echo ""
            echo "Environment Variables:"
            echo "  DOCKER_REGISTRY     Docker registry URL"
            echo "  BUILD_TAG           Build tag"
            echo "  BUILD_PLATFORM      Target platforms"
            echo "  PUSH_IMAGES         Push to registry (true/false)"
            echo "  ANALYZE_IMAGES      Run analysis (true/false)"
            echo "  SECURITY_SCAN       Run security scan (true/false)"
            echo "  GENERATE_SBOM       Generate SBOM (true/false)"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Execute main function
main
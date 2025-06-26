#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Neo Service Layer - Ubuntu 24 Docker Build Script${NC}"
echo -e "${BLUE}====================================================${NC}"
echo ""

# Function to print colored output
print_step() {
    echo -e "${GREEN}[STEP]${NC} $1"
}

print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check if Docker is running
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        print_error "Docker is not running. Please start Docker and try again."
        exit 1
    fi
    print_info "Docker is running ✓"
}

# Function to check if docker-compose is available
check_docker_compose() {
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        print_error "Docker Compose is not available. Please install Docker Compose."
        exit 1
    fi
    print_info "Docker Compose is available ✓"
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [COMMAND] [OPTIONS]"
    echo ""
    echo "Commands:"
    echo "  build          Build the Docker image"
    echo "  run            Build and run the application"
    echo "  test           Build and run comprehensive tests"
    echo "  dev            Start development environment"
    echo "  clean          Clean up Docker containers and images"
    echo "  rebuild        Clean build (remove existing images and rebuild)"
    echo "  shell          Start interactive shell in container"
    echo "  logs           Show application logs"
    echo "  status         Show container status"
    echo "  stop           Stop all services"
    echo "  restart        Restart all services"
    echo ""
    echo "Options:"
    echo "  --no-cache     Build without using cache"
    echo "  --pull         Pull latest base images"
    echo "  --verbose      Verbose output"
    echo "  --profile      Use specific docker-compose profile"
    echo ""
    echo "Examples:"
    echo "  $0 build --no-cache"
    echo "  $0 run"
    echo "  $0 test"
    echo "  $0 dev"
    echo "  $0 shell"
}

# Parse command line arguments
COMMAND=""
NO_CACHE=""
PULL=""
VERBOSE=""
PROFILE=""

while [[ $# -gt 0 ]]; do
    case $1 in
        build|run|test|dev|clean|rebuild|shell|logs|status|stop|restart)
            COMMAND="$1"
            shift
            ;;
        --no-cache)
            NO_CACHE="--no-cache"
            shift
            ;;
        --pull)
            PULL="--pull"
            shift
            ;;
        --verbose)
            VERBOSE="--verbose"
            shift
            ;;
        --profile)
            PROFILE="--profile $2"
            shift 2
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Default command
if [ -z "$COMMAND" ]; then
    COMMAND="build"
fi

# Set compose command
if command -v docker-compose &> /dev/null; then
    COMPOSE_CMD="docker-compose"
else
    COMPOSE_CMD="docker compose"
fi

# Main execution
main() {
    check_docker
    check_docker_compose
    
    case $COMMAND in
        "build")
            print_step "Building Neo Service Layer Docker image..."
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml build $NO_CACHE $PULL $VERBOSE
            print_info "Build completed successfully! ✓"
            ;;
            
        "run")
            print_step "Building and starting Neo Service Layer..."
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml build $NO_CACHE $PULL
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml up -d neo-service-layer
            print_info "Neo Service Layer is starting..."
            print_info "Web interface will be available at: http://localhost:5000"
            print_info "API documentation at: http://localhost:5000/swagger"
            echo ""
            print_info "To view logs: $0 logs"
            print_info "To check status: $0 status"
            ;;
            
        "test")
            print_step "Building and running comprehensive tests..."
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml build $NO_CACHE $PULL
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml --profile test run --rm neo-test
            print_info "Tests completed! Check ./test-results for detailed results."
            ;;
            
        "dev")
            print_step "Starting development environment..."
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml build $NO_CACHE $PULL
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml run --rm neo-dev
            ;;
            
        "clean")
            print_step "Cleaning up Docker containers and images..."
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml down -v --remove-orphans
            docker system prune -f
            print_info "Cleanup completed! ✓"
            ;;
            
        "rebuild")
            print_step "Performing clean rebuild..."
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml down -v --remove-orphans
            docker rmi neo-service-layer_neo-service-layer 2>/dev/null || true
            docker rmi neo-service-layer_neo-dev 2>/dev/null || true
            docker rmi neo-service-layer_neo-test 2>/dev/null || true
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml build --no-cache $PULL
            print_info "Rebuild completed! ✓"
            ;;
            
        "shell")
            print_step "Starting interactive shell..."
            if ! docker ps | grep -q neo-service-layer-ubuntu24; then
                print_info "Starting container first..."
                $COMPOSE_CMD -f docker-compose.ubuntu24.yml up -d neo-service-layer
                sleep 5
            fi
            docker exec -it neo-service-layer-ubuntu24 /neo-service-layer/dev-tools.sh shell
            ;;
            
        "logs")
            print_step "Showing application logs..."
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml logs -f neo-service-layer
            ;;
            
        "status")
            print_step "Checking container status..."
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml ps
            echo ""
            if docker ps | grep -q neo-service-layer-ubuntu24; then
                print_info "Application health:"
                docker exec neo-service-layer-ubuntu24 curl -s http://localhost:5000/api/health 2>/dev/null || print_warning "Health check failed"
            fi
            ;;
            
        "stop")
            print_step "Stopping all services..."
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml down
            print_info "All services stopped! ✓"
            ;;
            
        "restart")
            print_step "Restarting all services..."
            $COMPOSE_CMD -f docker-compose.ubuntu24.yml restart
            print_info "All services restarted! ✓"
            ;;
            
        *)
            print_error "Unknown command: $COMMAND"
            show_usage
            exit 1
            ;;
    esac
}

# Trap to handle interrupts
trap 'echo -e "\n${RED}Build interrupted!${NC}"; exit 1' INT

# Run main function
main

print_info "Script completed successfully! 🎉" 
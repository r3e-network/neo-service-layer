#!/bin/bash

# Set colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Parse arguments
SIMPLE=false
ALL=false
BUILD=false
CLEAN=false

for arg in "$@"
do
    case $arg in
        --simple)
        SIMPLE=true
        shift
        ;;
        --all)
        ALL=true
        shift
        ;;
        --build)
        BUILD=true
        shift
        ;;
        --clean)
        CLEAN=true
        shift
        ;;
    esac
done

echo -e "${GREEN}Running Neo Service Layer integration tests...${NC}"

# Clean up if requested
if [ "$CLEAN" = true ]; then
    echo -e "${YELLOW}Cleaning up previous containers...${NC}"
    docker-compose -f docker-compose.tests.yml down
fi

# Build if requested
if [ "$BUILD" = true ]; then
    echo -e "${YELLOW}Building test containers...${NC}"
    docker-compose -f docker-compose.tests.yml build
fi

# Run the simple test if requested
if [ "$SIMPLE" = true ]; then
    echo -e "${CYAN}Running simple test...${NC}"
    docker-compose -f docker-compose.tests.yml run simple-test
# Run all tests if requested
elif [ "$ALL" = true ]; then
    echo -e "${CYAN}Running all integration tests...${NC}"
    docker-compose -f docker-compose.tests.yml run integration-tests
# If no specific tests were selected, run the simple test
else
    echo -e "${CYAN}Running simple test (default)...${NC}"
    docker-compose -f docker-compose.tests.yml run simple-test
fi

# Clean up
echo -e "${GREEN}Tests completed!${NC}"

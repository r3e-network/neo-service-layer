#!/bin/bash

# Run integration tests with Docker Compose

# Default values
SIMPLE=false
ALL=false
BUILD=false
CLEAN=false
KEEP_RUNNING=false
FILTER=""
VERBOSE=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
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
    --keep-running)
      KEEP_RUNNING=true
      shift
      ;;
    --filter)
      FILTER="$2"
      shift
      shift
      ;;
    --verbose)
      VERBOSE=true
      shift
      ;;
    *)
      echo "Unknown option: $1"
      echo "Usage: $0 [--simple] [--all] [--build] [--clean] [--keep-running] [--filter \"FilterExpression\"] [--verbose]"
      exit 1
      ;;
  esac
done

echo -e "\e[32mRunning Neo Service Layer integration tests...\e[0m"

# Clean up if requested
if [ "$CLEAN" = true ]; then
  echo -e "\e[33mCleaning up previous containers...\e[0m"
  docker-compose -f docker-compose.tests.yml down
fi

# Build if requested
if [ "$BUILD" = true ]; then
  echo -e "\e[33mBuilding test containers...\e[0m"
  docker-compose -f docker-compose.tests.yml build
fi

# Prepare the command
VERBOSITY_LEVEL="detailed"
if [ "$VERBOSE" = true ]; then
  VERBOSITY_LEVEL="diagnostic"
fi

TEST_COMMAND="dotnet test tests/NeoServiceLayer.Integration.Tests/NeoServiceLayer.Integration.Tests.csproj --logger \"console;verbosity=$VERBOSITY_LEVEL\""

# Add filter if specified
if [ -n "$FILTER" ]; then
  TEST_COMMAND="$TEST_COMMAND --filter \"$FILTER\""
fi

# Run the simple test if requested
if [ "$SIMPLE" = true ]; then
  echo -e "\e[36mRunning simple test...\e[0m"
  docker-compose -f docker-compose.tests.yml run simple-test
# Run all tests if requested
elif [ "$ALL" = true ]; then
  echo -e "\e[36mRunning all integration tests...\e[0m"
  
  if [ "$KEEP_RUNNING" = true ]; then
    echo -e "\e[33mStarting services and keeping them running...\e[0m"
    docker-compose -f docker-compose.tests.yml up -d redis tee-host api
    docker-compose -f docker-compose.tests.yml run --rm integration-tests $TEST_COMMAND
  else
    docker-compose -f docker-compose.tests.yml run --rm integration-tests $TEST_COMMAND
  fi
# If no specific tests were selected, run the simple test
else
  echo -e "\e[36mRunning simple test (default)...\e[0m"
  docker-compose -f docker-compose.tests.yml run simple-test
fi

# Clean up
if [ "$KEEP_RUNNING" = false ]; then
  echo -e "\e[33mCleaning up containers...\e[0m"
  docker-compose -f docker-compose.tests.yml down
else
  echo -e "\e[33mServices are still running. Use 'docker-compose -f docker-compose.tests.yml down' to stop them.\e[0m"
fi

echo -e "\e[32mTests completed!\e[0m"

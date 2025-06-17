# Neo Service Layer - Ubuntu 24 Docker Environment
# Makefile for easy management

.PHONY: help build run test dev clean rebuild shell logs status stop restart info

# Default target
.DEFAULT_GOAL := help

# Color codes
BLUE=\033[0;34m
GREEN=\033[0;32m
YELLOW=\033[1;33m
RED=\033[0;31m
NC=\033[0m

# Docker compose file
COMPOSE_FILE=docker-compose.ubuntu24.yml
BUILD_SCRIPT=./build-docker-ubuntu24.sh

help: ## Show this help message
	@echo -e "$(BLUE)🚀 Neo Service Layer - Ubuntu 24 Docker Environment$(NC)"
	@echo -e "$(BLUE)====================================================$(NC)"
	@echo ""
	@echo "Available targets:"
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  $(GREEN)%-15s$(NC) %s\n", $$1, $$2}' $(MAKEFILE_LIST)
	@echo ""
	@echo "Examples:"
	@echo "  make run           # Build and run the application"
	@echo "  make test          # Run comprehensive tests"
	@echo "  make dev           # Start development environment"
	@echo "  make shell         # Access running container shell"

setup: ## Setup the build script permissions
	@echo -e "$(YELLOW)Setting up build script permissions...$(NC)"
	@chmod +x $(BUILD_SCRIPT)
	@echo -e "$(GREEN)✓ Setup completed$(NC)"

build: setup ## Build the Docker image
	@echo -e "$(BLUE)Building Neo Service Layer Docker image...$(NC)"
	@$(BUILD_SCRIPT) build

build-no-cache: setup ## Build without cache
	@echo -e "$(BLUE)Building without cache...$(NC)"
	@$(BUILD_SCRIPT) build --no-cache

run: setup ## Build and run the application
	@echo -e "$(BLUE)Starting Neo Service Layer...$(NC)"
	@$(BUILD_SCRIPT) run
	@echo ""
	@echo -e "$(GREEN)🌐 Application URLs:$(NC)"
	@echo -e "  Web Interface: http://localhost:5000"
	@echo -e "  API Docs:      http://localhost:5000/swagger"
	@echo -e "  Services:      http://localhost:5000/services"

test: setup ## Run comprehensive tests
	@echo -e "$(BLUE)Running comprehensive test suite...$(NC)"
	@$(BUILD_SCRIPT) test
	@echo -e "$(GREEN)✓ Tests completed! Check ./test-results for details$(NC)"

dev: setup ## Start development environment with shell
	@echo -e "$(BLUE)Starting development environment...$(NC)"
	@$(BUILD_SCRIPT) dev

shell: setup ## Access shell in running container
	@echo -e "$(BLUE)Accessing container shell...$(NC)"
	@$(BUILD_SCRIPT) shell

clean: setup ## Clean up containers and images
	@echo -e "$(YELLOW)Cleaning up Docker resources...$(NC)"
	@$(BUILD_SCRIPT) clean
	@echo -e "$(GREEN)✓ Cleanup completed$(NC)"

rebuild: setup ## Clean rebuild (removes existing images)
	@echo -e "$(YELLOW)Performing clean rebuild...$(NC)"
	@$(BUILD_SCRIPT) rebuild
	@echo -e "$(GREEN)✓ Rebuild completed$(NC)"

logs: setup ## Show application logs
	@echo -e "$(BLUE)Showing application logs...$(NC)"
	@$(BUILD_SCRIPT) logs

status: setup ## Check container status and health
	@echo -e "$(BLUE)Checking system status...$(NC)"
	@$(BUILD_SCRIPT) status

stop: setup ## Stop all services
	@echo -e "$(YELLOW)Stopping all services...$(NC)"
	@$(BUILD_SCRIPT) stop
	@echo -e "$(GREEN)✓ All services stopped$(NC)"

restart: setup ## Restart all services
	@echo -e "$(BLUE)Restarting all services...$(NC)"
	@$(BUILD_SCRIPT) restart
	@echo -e "$(GREEN)✓ All services restarted$(NC)"

# Advanced targets
monitoring: setup ## Start with monitoring stack (Prometheus + Grafana)
	@echo -e "$(BLUE)Starting with monitoring stack...$(NC)"
	@docker-compose -f $(COMPOSE_FILE) --profile monitoring up -d
	@echo ""
	@echo -e "$(GREEN)📊 Monitoring URLs:$(NC)"
	@echo -e "  Prometheus: http://localhost:9090"
	@echo -e "  Grafana:    http://localhost:3000 (admin/admin)"

database: setup ## Start with database (PostgreSQL)
	@echo -e "$(BLUE)Starting with database...$(NC)"
	@docker-compose -f $(COMPOSE_FILE) --profile database up -d
	@echo -e "$(GREEN)✓ Database started on port 5432$(NC)"

cache: setup ## Start with caching (Redis)
	@echo -e "$(BLUE)Starting with caching...$(NC)"
	@docker-compose -f $(COMPOSE_FILE) --profile cache up -d
	@echo -e "$(GREEN)✓ Redis cache started on port 6379$(NC)"

full-stack: setup ## Start full stack (app + database + cache + monitoring)
	@echo -e "$(BLUE)Starting full stack...$(NC)"
	@docker-compose -f $(COMPOSE_FILE) --profile database --profile cache --profile monitoring up -d
	@echo ""
	@echo -e "$(GREEN)🚀 Full stack is running:$(NC)"
	@echo -e "  Application: http://localhost:5000"
	@echo -e "  Prometheus:  http://localhost:9090"
	@echo -e "  Grafana:     http://localhost:3000"
	@echo -e "  Database:    localhost:5432"
	@echo -e "  Redis:       localhost:6379"

info: setup ## Show system information
	@echo -e "$(BLUE)System Information:$(NC)"
	@echo -e "  Docker:       $$(docker --version 2>/dev/null || echo 'Not available')"
	@echo -e "  Compose:      $$(docker-compose --version 2>/dev/null || docker compose version 2>/dev/null || echo 'Not available')"
	@echo -e "  Free space:   $$(df -h . | tail -1 | awk '{print $$4}')"
	@echo -e "  Memory:       $$(free -h | grep Mem | awk '{print $$7}') available"
	@echo ""
	@echo -e "$(BLUE)Container Status:$(NC)"
	@docker ps --filter "name=neo-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null || echo "No Neo containers running"

validate: setup ## Validate the environment
	@echo -e "$(BLUE)Validating environment...$(NC)"
	@echo -e "  ✓ Checking Docker..."
	@docker info > /dev/null 2>&1 || (echo -e "$(RED)✗ Docker not running$(NC)" && exit 1)
	@echo -e "  ✓ Checking Docker Compose..."
	@command -v docker-compose > /dev/null 2>&1 || docker compose version > /dev/null 2>&1 || (echo -e "$(RED)✗ Docker Compose not available$(NC)" && exit 1)
	@echo -e "  ✓ Checking build script..."
	@test -x $(BUILD_SCRIPT) || (echo -e "$(RED)✗ Build script not executable$(NC)" && exit 1)
	@echo -e "  ✓ Checking compose file..."
	@test -f $(COMPOSE_FILE) || (echo -e "$(RED)✗ Compose file not found$(NC)" && exit 1)
	@echo -e "$(GREEN)✓ Environment validation passed$(NC)"

quick-test: setup ## Quick smoke test
	@echo -e "$(BLUE)Running quick smoke test...$(NC)"
	@$(BUILD_SCRIPT) build
	@timeout 120 $(BUILD_SCRIPT) run &
	@sleep 30
	@curl -f http://localhost:5000/api/health > /dev/null 2>&1 && echo -e "$(GREEN)✓ Health check passed$(NC)" || echo -e "$(RED)✗ Health check failed$(NC)"
	@$(BUILD_SCRIPT) stop
	@echo -e "$(GREEN)✓ Quick test completed$(NC)"

# Utility targets
ps: ## Show running containers
	@docker ps --filter "name=neo-"

images: ## Show Neo Service Layer images
	@docker images --filter "reference=*neo*"

volumes: ## Show Docker volumes
	@docker volume ls --filter "name=neo"

network: ## Show Docker networks
	@docker network ls --filter "name=neo"

# Development helpers
format: ## Format code (placeholder for future use)
	@echo -e "$(BLUE)Code formatting not implemented yet$(NC)"

lint: ## Lint code (placeholder for future use)
	@echo -e "$(BLUE)Code linting not implemented yet$(NC)"

docs: ## Open documentation
	@echo -e "$(BLUE)Opening documentation...$(NC)"
	@cat README-Docker-Ubuntu24.md

# Cleanup targets
prune: ## Prune unused Docker resources
	@echo -e "$(YELLOW)Pruning unused Docker resources...$(NC)"
	@docker system prune -f
	@echo -e "$(GREEN)✓ Pruning completed$(NC)"

deep-clean: clean prune ## Deep clean (remove everything)
	@echo -e "$(RED)Performing deep clean...$(NC)"
	@docker volume prune -f
	@docker network prune -f
	@echo -e "$(GREEN)✓ Deep clean completed$(NC)" 
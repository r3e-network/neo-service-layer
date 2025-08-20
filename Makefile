# Neo Service Layer - Comprehensive Build System
# Makefile for building, testing, benchmarking, and managing the Neo Service Layer
# Optimized for .NET 9.0 and centralized package management

.PHONY: help all build test bench clean docker-build docker-run

# Default target
.DEFAULT_GOAL := help

# Configuration
SOLUTION = NeoServiceLayer.sln
CONFIGURATION ?= Release
VERBOSITY ?= minimal
PARALLEL ?= true
RUNTIME ?= 
TARGET_FRAMEWORK ?= net9.0

# Directories
RESULTS_DIR = TestResults
COVERAGE_DIR = CoverageReport
BENCHMARK_DIR = BenchmarkResults
ARTIFACTS_DIR = artifacts
SRC_DIR = src
TEST_DIR = tests
SCRIPTS_DIR = scripts
CONFIG_DIR = config
LOGS_DIR = logs

# Docker Configuration
DOCKER_IMAGE = neo-service-layer
DOCKER_TAG ?= latest
COMPOSE_FILE = docker-compose.yml
COMPOSE_FILE_UBUNTU = docker-compose.ubuntu24.yml
COMPOSE_FILE_DEV = docker-compose.dev.yml
COMPOSE_FILE_OCCLUM = docker-compose.occlum.yml

# Colors for output
BLUE := \033[0;34m
GREEN := \033[0;32m
YELLOW := \033[1;33m
RED := \033[0;31m
CYAN := \033[0;36m
MAGENTA := \033[0;35m
NC := \033[0m

# .NET CLI commands
DOTNET = dotnet
ifeq ($(PARALLEL),true)
	BUILD_FLAGS = -m -p:ParallelBuildEnable=true
	TEST_FLAGS = 
else
	BUILD_FLAGS =
	TEST_FLAGS =
endif

# Environment detection
UNAME_S := $(shell uname -s)
ifeq ($(UNAME_S),Linux)
	OS_TYPE = linux
	TIME_CMD = /usr/bin/time -v
endif
ifeq ($(UNAME_S),Darwin)
	OS_TYPE = macos
	TIME_CMD = gtime -v
endif

# Version information
GIT_TAG := $(shell git describe --tags --abbrev=0 2>/dev/null || echo "")
GIT_COMMIT := $(shell git rev-parse --short HEAD 2>/dev/null || echo "unknown")
GIT_BRANCH := $(shell git rev-parse --abbrev-ref HEAD 2>/dev/null || echo "unknown")
ifeq ($(GIT_TAG),)
	VERSION := 1.0.0
else
	VERSION := $(GIT_TAG)
endif
COMMIT := $(GIT_COMMIT)
BUILD_DATE := $(shell date -u +"%Y-%m-%dT%H:%M:%SZ")

##@ General

help: ## Display this help message
	@echo "$(BLUE)🚀 Neo Service Layer - Build System v$(VERSION)$(NC)"
	@echo "$(BLUE)===========================================$(NC)"
	@echo ""
	@awk 'BEGIN {FS = ":.*##"; printf "Usage:\n  make $(CYAN)<target>$(NC)\n"} /^[a-zA-Z_0-9-]+:.*?##/ { printf "  $(CYAN)%-25s$(NC) %s\n", $$1, $$2 } /^##@/ { printf "\n$(YELLOW)%s$(NC)\n", substr($$0, 5) } ' $(MAKEFILE_LIST)
	@echo ""
	@echo "$(MAGENTA)Configuration Variables:$(NC)"
	@echo "  $(GREEN)CONFIGURATION$(NC)     Build configuration (Debug/Release) [current: $(CONFIGURATION)]"
	@echo "  $(GREEN)VERBOSITY$(NC)         Output verbosity (quiet/minimal/normal/detailed) [current: $(VERBOSITY)]"
	@echo "  $(GREEN)PARALLEL$(NC)          Enable parallel builds (true/false) [current: $(PARALLEL)]"
	@echo "  $(GREEN)RUNTIME$(NC)           Target runtime (e.g., linux-x64, win-x64) [current: $(RUNTIME)]"
	@echo "  $(GREEN)TARGET_FRAMEWORK$(NC)  Target framework [current: $(TARGET_FRAMEWORK)]"
	@echo ""
	@echo "$(MAGENTA)Quick Start:$(NC)"
	@echo "  make quick            # Clean, build, and run quick tests"
	@echo "  make dev              # Start development environment"
	@echo "  make ci               # Run full CI pipeline locally"
	@echo ""
	@echo "$(MAGENTA)Examples:$(NC)"
	@echo "  make build                         # Build in Release mode"
	@echo "  make test CONFIGURATION=Debug      # Run tests in Debug mode"
	@echo "  make bench VERBOSITY=detailed      # Run benchmarks with detailed output"
	@echo "  make docker-compose-up             # Start all services with docker-compose"

version: ## Show version and environment information
	@echo "$(CYAN)📋 Version Information:$(NC)"
	@echo "  Version:          $(VERSION)"
	@echo "  Commit:           $(COMMIT)"
	@echo "  Branch:           $(GIT_BRANCH)"
	@echo "  Build Date:       $(BUILD_DATE)"
	@echo ""
	@echo "$(CYAN)🔧 Environment:$(NC)"
	@echo "  .NET SDK:         $(shell $(DOTNET) --version)"
	@echo "  Target Framework: $(TARGET_FRAMEWORK)"
	@echo "  OS Type:          $(OS_TYPE)"
	@echo "  CPU Cores:        $(shell nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo "unknown")"

status: ## Show project status and health
	@echo "$(CYAN)📊 Project Status:$(NC)"
	@echo "  Git Status:"
	@git status --short || echo "    Not a git repository"
	@echo ""
	@echo "  NuGet Packages:"
	@$(DOTNET) list package --outdated 2>/dev/null | head -10 || echo "    Run 'make update-packages' to check"
	@echo ""
	@echo "  Test Coverage:"
	@if [ -f "$(COVERAGE_DIR)/Summary.txt" ]; then \
		tail -5 "$(COVERAGE_DIR)/Summary.txt"; \
	else \
		echo "    No coverage report available. Run 'make test-coverage'"; \
	fi

##@ Build

all: clean restore build build-sgx test ## Clean, restore, build (including SGX), and test everything

quick: clean-minimal build-minimal test-minimal ## Quick build and test (minimal clean, no restore)

restore: ## Restore NuGet packages
	@echo "$(BLUE)📦 Restoring NuGet packages...$(NC)"
	@mkdir -p $(LOGS_DIR)
	$(DOTNET) restore $(SOLUTION) --verbosity $(VERBOSITY) | tee $(LOGS_DIR)/restore.log
	@echo "$(GREEN)✅ Package restore completed$(NC)"

restore-locked: ## Restore packages using lock file
	@echo "$(BLUE)📦 Restoring packages with lock file...$(NC)"
	$(DOTNET) restore $(SOLUTION) --locked-mode --verbosity $(VERBOSITY)
	@echo "$(GREEN)✅ Locked restore completed$(NC)"

build: restore ## Build the solution (C# projects)
	@echo "$(BLUE)🔨 Building solution ($(CONFIGURATION))...$(NC)"
	@mkdir -p $(LOGS_DIR)
	$(DOTNET) build $(SOLUTION) \
		--configuration $(CONFIGURATION) \
		--no-restore \
		--verbosity $(VERBOSITY) \
		$(BUILD_FLAGS) \
		-p:Version=$(VERSION) \
		-p:InformationalVersion=$(VERSION)-$(COMMIT) \
		-p:TreatWarningsAsErrors=false \
		-p:WarningLevel=4 | tee $(LOGS_DIR)/build.log
	@echo "$(GREEN)✅ .NET build completed successfully$(NC)"

build-minimal: ## Build without restore (fast rebuild)
	@echo "$(BLUE)⚡ Fast build (no restore)...$(NC)"
	$(DOTNET) build $(SOLUTION) \
		--configuration $(CONFIGURATION) \
		--no-restore \
		--verbosity quiet \
		$(BUILD_FLAGS)
	@echo "$(GREEN)✅ Fast build completed$(NC)"

build-debug: ## Build in Debug configuration
	@$(MAKE) build CONFIGURATION=Debug

build-release: ## Build in Release configuration  
	@$(MAKE) build CONFIGURATION=Release

rebuild: clean build ## Clean and rebuild the solution

publish: build ## Publish the application
	@echo "$(BLUE)📤 Publishing application...$(NC)"
	@mkdir -p $(ARTIFACTS_DIR)
	$(DOTNET) publish src/Web/NeoServiceLayer.Web/NeoServiceLayer.Web.csproj \
		--configuration $(CONFIGURATION) \
		--no-build \
		--output $(ARTIFACTS_DIR)/web \
		$(if $(RUNTIME),--runtime $(RUNTIME) --self-contained,)
	$(DOTNET) publish src/Api/NeoServiceLayer.Api/NeoServiceLayer.Api.csproj \
		--configuration $(CONFIGURATION) \
		--no-build \
		--output $(ARTIFACTS_DIR)/api \
		$(if $(RUNTIME),--runtime $(RUNTIME) --self-contained,)
	@echo "$(GREEN)✅ Publishing completed$(NC)"

publish-linux: ## Publish for Linux x64
	@$(MAKE) publish RUNTIME=linux-x64

publish-windows: ## Publish for Windows x64
	@$(MAKE) publish RUNTIME=win-x64

publish-macos: ## Publish for macOS x64
	@$(MAKE) publish RUNTIME=osx-x64

build-contracts: ## Build Neo smart contracts
	@echo "$(BLUE)📜 Building Neo smart contracts...$(NC)"
	@cd contracts-neo-n3 && \
		if [ -x scripts/build.sh ]; then \
			./scripts/build.sh; \
		else \
			echo "$(YELLOW)⚠️  Contract build script not found$(NC)"; \
		fi
	@echo "$(GREEN)✅ Contract build completed$(NC)"

##@ SGX and Rust Build Targets

# Rust and SGX configuration
CARGO := cargo
RUSTC := rustc
SGX_SDK := /opt/intel/sgxsdk
SGX_MODE ?= SIM
ENCLAVE_DIR := src/Tee/NeoServiceLayer.Tee.Enclave
ENCLAVE_NAME := neo-service-enclave
RUST_INSTALLED := $(shell command -v cargo 2> /dev/null)

build-sgx: check-rust build-rust-enclave build-tee-components ## Build all SGX and Rust components
	@echo "$(GREEN)✅ SGX components built successfully$(NC)"

check-rust: ## Check if Rust is installed
ifndef RUST_INSTALLED
	@echo "$(RED)❌ Rust is not installed. Please install Rust from https://rustup.rs/$(NC)"
	@echo "  Run: curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh"
	@exit 1
else
	@echo "$(GREEN)✅ Rust is installed: $(shell cargo --version)$(NC)"
endif

build-rust-enclave: check-rust ## Build Rust SGX enclave
	@echo "$(BLUE)🦀 Building Rust SGX enclave...$(NC)"
	@mkdir -p $(LOGS_DIR)
	@cd $(ENCLAVE_DIR) && \
	if [ -f "Cargo.toml" ]; then \
		echo "  Building $(ENCLAVE_NAME) (this may take several minutes on first build)..."; \
		$(CARGO) build --release 2>&1 | tee ../../$(LOGS_DIR)/rust-build.log; \
		if [ -f "target/release/libneo_service_enclave.so" ]; then \
			echo "$(GREEN)  ✅ Rust enclave built successfully$(NC)"; \
		else \
			echo "$(RED)  ❌ Rust enclave build failed$(NC)"; \
			exit 1; \
		fi \
	else \
		echo "$(YELLOW)  ⚠️  No Cargo.toml found in $(ENCLAVE_DIR)$(NC)"; \
	fi

build-tee-components: ## Build TEE host and enclave C# wrappers
	@echo "$(BLUE)🔐 Building TEE components...$(NC)"
	@echo "  SGX Mode: $(SGX_MODE)"
	@echo "  Building TEE Host..."
	@$(DOTNET) build src/Tee/NeoServiceLayer.Tee.Host/NeoServiceLayer.Tee.Host.csproj \
		--configuration $(CONFIGURATION) \
		--verbosity quiet || echo "$(YELLOW)  ⚠️  TEE Host build warnings$(NC)"
	@echo "  Building TEE Enclave wrapper..."
	@$(DOTNET) build src/Tee/NeoServiceLayer.Tee.Enclave/NeoServiceLayer.Tee.Enclave.csproj \
		--configuration $(CONFIGURATION) \
		--verbosity quiet || echo "$(YELLOW)  ⚠️  TEE Enclave wrapper build warnings$(NC)"

build-all: build build-sgx build-contracts ## Build everything including SGX and contracts
	@echo "$(GREEN)✅ Complete build finished (C#, Rust SGX, Contracts)$(NC)"

clean-sgx: ## Clean SGX and Rust artifacts
	@echo "$(YELLOW)🧹 Cleaning SGX artifacts...$(NC)"
	@if [ -d "$(ENCLAVE_DIR)/target" ]; then \
		rm -rf $(ENCLAVE_DIR)/target; \
		echo "  Cleaned Rust build artifacts"; \
	fi
	@if [ -d "src/Tee/NeoServiceLayer.Tee.Host/bin" ]; then \
		rm -rf src/Tee/NeoServiceLayer.Tee.Host/bin src/Tee/NeoServiceLayer.Tee.Host/obj; \
		echo "  Cleaned TEE Host artifacts"; \
	fi
	@if [ -d "src/Tee/NeoServiceLayer.Tee.Enclave/bin" ]; then \
		rm -rf src/Tee/NeoServiceLayer.Tee.Enclave/bin src/Tee/NeoServiceLayer.Tee.Enclave/obj; \
		echo "  Cleaned TEE Enclave artifacts"; \
	fi
	@echo "$(GREEN)✅ SGX cleanup completed$(NC)"

test-sgx: build-sgx ## Test SGX components
	@echo "$(BLUE)🧪 Testing SGX components...$(NC)"
	@if [ -f "$(ENCLAVE_DIR)/Cargo.toml" ]; then \
		echo "  Running Rust enclave tests..."; \
		cd $(ENCLAVE_DIR) && $(CARGO) test --release 2>&1 | tee ../../$(LOGS_DIR)/rust-test.log || true; \
	fi
	@if [ -f "scripts/sgx/run-sgx-tests-docker.sh" ]; then \
		echo "  Running SGX simulation tests..."; \
		bash scripts/sgx/run-sgx-tests-docker.sh 2>&1 | tee $(LOGS_DIR)/sgx-test.log || true; \
	fi
	@echo "$(GREEN)✅ SGX tests completed$(NC)"

info-sgx: ## Show SGX and Rust configuration
	@echo "$(BLUE)ℹ️  SGX Build Information$(NC)"
	@echo "  SGX Mode: $(SGX_MODE)"
	@echo "  SGX SDK: $(SGX_SDK)"
	@echo "  Enclave Directory: $(ENCLAVE_DIR)"
	@echo "  Rust Version: $(shell cargo --version 2>/dev/null || echo 'Not installed')"
	@echo "  Target Architecture: $(shell uname -m)"
	@echo ""
	@echo "  Rust Projects:"
	@find . -name "Cargo.toml" -type f | while read -r cargo; do \
		dir=$$(dirname "$$cargo"); \
		name=$$(grep "^name" "$$cargo" | cut -d'"' -f2 | head -1); \
		echo "    - $$name ($$dir)"; \
	done

analyze: build ## Run code analysis
	@echo "$(BLUE)🔍 Running code analysis...$(NC)"
	$(DOTNET) build $(SOLUTION) \
		--configuration $(CONFIGURATION) \
		--no-restore \
		-p:RunAnalyzers=true \
		-p:AnalysisLevel=latest-recommended \
		-p:TreatWarningsAsErrors=false
	@echo "$(GREEN)✅ Code analysis completed$(NC)"

##@ Testing

test: build test-unit test-integration ## Run all tests

test-all: test test-performance test-contracts ## Run all tests including performance and contracts

test-minimal: ## Run minimal test suite (fast)
	@echo "$(BLUE)⚡ Running minimal test suite...$(NC)"
	$(DOTNET) test $(SOLUTION) \
		--configuration $(CONFIGURATION) \
		--no-build \
		--verbosity minimal \
		--filter "Category=Fast|Priority=Critical"
	@echo "$(GREEN)✅ Minimal tests completed$(NC)"

test-unit: ## Run unit tests
	@echo "$(BLUE)🧪 Running unit tests...$(NC)"
	@mkdir -p $(RESULTS_DIR)
	$(DOTNET) test $(SOLUTION) \
		--configuration $(CONFIGURATION) \
		--no-build \
		--verbosity $(VERBOSITY) \
		--filter "Category!=Integration&Category!=Performance&Category!=EndToEnd" \
		--logger "trx;LogFileName=unit-tests.trx" \
		--logger "console;verbosity=$(VERBOSITY)" \
		--results-directory $(RESULTS_DIR) \
		$(TEST_FLAGS)
	@echo "$(GREEN)✅ Unit tests completed$(NC)"

test-integration: ## Run integration tests
	@echo "$(BLUE)🔗 Running integration tests...$(NC)"
	@mkdir -p $(RESULTS_DIR)
	$(DOTNET) test $(SOLUTION) \
		--configuration $(CONFIGURATION) \
		--no-build \
		--verbosity $(VERBOSITY) \
		--filter "Category=Integration" \
		--logger "trx;LogFileName=integration-tests.trx" \
		--logger "console;verbosity=$(VERBOSITY)" \
		--results-directory $(RESULTS_DIR)
	@echo "$(GREEN)✅ Integration tests completed$(NC)"

test-e2e: ## Run end-to-end tests
	@echo "$(BLUE)🌐 Running end-to-end tests...$(NC)"
	@mkdir -p $(RESULTS_DIR)
	$(DOTNET) test $(SOLUTION) \
		--configuration $(CONFIGURATION) \
		--no-build \
		--verbosity $(VERBOSITY) \
		--filter "Category=EndToEnd" \
		--logger "trx;LogFileName=e2e-tests.trx" \
		--results-directory $(RESULTS_DIR)
	@echo "$(GREEN)✅ End-to-end tests completed$(NC)"

test-performance: build ## Run performance tests
	@echo "$(BLUE)⚡ Running performance tests...$(NC)"
	@mkdir -p $(RESULTS_DIR)
	$(DOTNET) test tests/Performance/NeoServiceLayer.Performance.Tests/NeoServiceLayer.Performance.Tests.csproj \
		--configuration $(CONFIGURATION) \
		--no-build \
		--verbosity $(VERBOSITY) \
		--filter "Category=Performance" \
		--logger "trx;LogFileName=performance-tests.trx" \
		--results-directory $(RESULTS_DIR)
	@echo "$(GREEN)✅ Performance tests completed$(NC)"

test-contracts: ## Run smart contract tests
	@echo "$(BLUE)📜 Running contract tests...$(NC)"
	@cd neo-express-test && \
		if [ -f test-integration.sh ]; then \
			./test-integration.sh; \
		else \
			echo "$(YELLOW)⚠️  Contract test script not found$(NC)"; \
		fi
	@echo "$(GREEN)✅ Contract tests completed$(NC)"

test-coverage: build ## Run tests with code coverage
	@echo "$(BLUE)📊 Running tests with coverage...$(NC)"
	@mkdir -p $(RESULTS_DIR) $(COVERAGE_DIR)
	$(DOTNET) test $(SOLUTION) \
		--configuration $(CONFIGURATION) \
		--no-build \
		--verbosity $(VERBOSITY) \
		--collect:"XPlat Code Coverage" \
		--settings config/coverlet.runsettings \
		--results-directory $(RESULTS_DIR) \
		$(TEST_FLAGS)
	@echo "$(YELLOW)📈 Generating coverage report...$(NC)"
	@if command -v reportgenerator &> /dev/null; then \
		reportgenerator \
			-reports:"$(RESULTS_DIR)/**/coverage.cobertura.xml" \
			-targetdir:"$(COVERAGE_DIR)" \
			-reporttypes:"Html;Badges;TextSummary;MarkdownSummaryGithub;Cobertura" \
			-verbosity:Warning; \
		echo -e "$(GREEN)✅ Coverage report generated: $(COVERAGE_DIR)/index.html$(NC)"; \
		if [ -f "$(COVERAGE_DIR)/Summary.txt" ]; then \
			echo ""; \
			cat "$(COVERAGE_DIR)/Summary.txt"; \
		fi; \
	else \
		echo -e "$(YELLOW)⚠️  ReportGenerator not installed. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool$(NC)"; \
	fi

test-service: ## Test specific service (use SERVICE=ServiceName)
	@if [ -z "$(SERVICE)" ]; then \
		echo "$(RED)❌ Please specify SERVICE=ServiceName$(NC)"; \
		exit 1; \
	fi
	@echo "$(BLUE)🧪 Testing $(SERVICE)...$(NC)"
	$(DOTNET) test tests/Services/NeoServiceLayer.Services.$(SERVICE).Tests \
		--configuration $(CONFIGURATION) \
		--verbosity $(VERBOSITY)

test-watch: ## Run tests in watch mode
	@echo "$(BLUE)👁️  Running tests in watch mode...$(NC)"
	$(DOTNET) watch --project $(SOLUTION) test

test-failed: ## Re-run only failed tests
	@echo "$(BLUE)🔄 Re-running failed tests...$(NC)"
	@if [ -f "$(RESULTS_DIR)/failed-tests.txt" ]; then \
		$(DOTNET) test $(SOLUTION) \
			--configuration $(CONFIGURATION) \
			--no-build \
			--filter "$$(cat $(RESULTS_DIR)/failed-tests.txt)"; \
	else \
		echo "$(YELLOW)No failed tests found$(NC)"; \
	fi

##@ Benchmarking

bench: build bench-run ## Build and run all benchmarks

bench-quick: build ## Run quick benchmarks only
	@echo "$(BLUE)⚡ Running quick benchmarks...$(NC)"
	@mkdir -p $(BENCHMARK_DIR)
	$(DOTNET) run --project tests/Performance/NeoServiceLayer.Performance.Tests/NeoServiceLayer.Performance.Tests.csproj \
		--configuration $(CONFIGURATION) \
		--no-build \
		-- \
		--job Short \
		--runtimes net9.0 \
		--filter * \
		--artifactsPath $(BENCHMARK_DIR)
	@echo "$(GREEN)✅ Quick benchmarks completed$(NC)"

bench-run: ## Run full benchmark suite
	@echo "$(BLUE)🏃 Running full benchmark suite...$(NC)"
	@mkdir -p $(BENCHMARK_DIR)
	$(DOTNET) run --project tests/Performance/NeoServiceLayer.Performance.Tests/NeoServiceLayer.Performance.Tests.csproj \
		--configuration $(CONFIGURATION) \
		--no-build \
		-- \
		--filter * \
		--artifactsPath $(BENCHMARK_DIR) \
		--exporters json html csv markdown \
		--memorydisplayer columns
	@echo "$(GREEN)✅ Benchmarks completed. Results in: $(BENCHMARK_DIR)$(NC)"
	@echo "$(CYAN)📊 Benchmark Summary:$(NC)"
	@find $(BENCHMARK_DIR) -name "*-report.html" -exec echo "  HTML Report: {}" \;
	@find $(BENCHMARK_DIR) -name "*.md" -exec echo "  Markdown Report: {}" \;

bench-specific: build ## Run specific benchmark (use BENCH_FILTER=YourBenchmarkClass)
	@if [ -z "$(BENCH_FILTER)" ]; then \
		echo "$(RED)❌ Please specify BENCH_FILTER=YourBenchmarkClass$(NC)"; \
		exit 1; \
	fi
	@echo "$(BLUE)🏃 Running benchmark: $(BENCH_FILTER)...$(NC)"
	@mkdir -p $(BENCHMARK_DIR)
	$(DOTNET) run --project tests/Performance/NeoServiceLayer.Performance.Tests/NeoServiceLayer.Performance.Tests.csproj \
		--configuration $(CONFIGURATION) \
		--no-build \
		-- \
		--filter *$(BENCH_FILTER)* \
		--artifactsPath $(BENCHMARK_DIR)

bench-memory: build ## Run memory-focused benchmarks
	@echo "$(BLUE)💾 Running memory benchmarks...$(NC)"
	@mkdir -p $(BENCHMARK_DIR)
	$(DOTNET) run --project tests/Performance/NeoServiceLayer.Performance.Tests/NeoServiceLayer.Performance.Tests.csproj \
		--configuration $(CONFIGURATION) \
		--no-build \
		-- \
		--filter * \
		--artifactsPath $(BENCHMARK_DIR) \
		--memorydiagnoser \
		--disassemblydisagnoser
	@echo "$(GREEN)✅ Memory benchmarks completed$(NC)"

bench-compare: ## Compare benchmark results (use BASELINE=path/to/baseline.json CURRENT=path/to/current.json)
	@if [ -z "$(BASELINE)" ] || [ -z "$(CURRENT)" ]; then \
		echo "$(RED)❌ Please specify BASELINE and CURRENT json files$(NC)"; \
		exit 1; \
	fi
	@echo "$(BLUE)📊 Comparing benchmark results...$(NC)"
	@if command -v benchmarkdotnet &> /dev/null; then \
		benchmarkdotnet results comparer $(BASELINE) $(CURRENT); \
	else \
		echo "$(YELLOW)⚠️  BenchmarkDotNet CLI not installed$(NC)"; \
	fi

load-test: ## Run load tests using NBomber
	@echo "$(BLUE)🔥 Running load tests...$(NC)"
	@mkdir -p $(BENCHMARK_DIR)
	$(DOTNET) test tests/Performance/NeoServiceLayer.Performance.Tests \
		--configuration $(CONFIGURATION) \
		--filter "ClassName~NBomberLoadTests" \
		--logger "console;verbosity=normal"
	@echo "$(GREEN)✅ Load tests completed$(NC)"

stress-test: ## Run stress tests
	@echo "$(BLUE)💪 Running stress tests...$(NC)"
	@mkdir -p $(BENCHMARK_DIR)
	$(DOTNET) test tests/Performance/NeoServiceLayer.Performance.Tests \
		--configuration $(CONFIGURATION) \
		--filter "Category=Stress" \
		--logger "console;verbosity=detailed"
	@echo "$(GREEN)✅ Stress tests completed$(NC)"

perf-profile: ## Profile application performance
	@echo "$(BLUE)📈 Profiling application performance...$(NC)"
	@if command -v dotnet-trace &> /dev/null; then \
		dotnet-trace collect --process-name NeoServiceLayer.Web --providers Microsoft-DotNETCore-SampleProfiler; \
	else \
		echo "$(YELLOW)⚠️  dotnet-trace not installed. Install with: dotnet tool install -g dotnet-trace$(NC)"; \
	fi

##@ Code Quality

lint: ## Run code analysis
	@echo "$(BLUE)🔍 Running code analysis...$(NC)"
	$(DOTNET) build $(SOLUTION) \
		--configuration $(CONFIGURATION) \
		--no-restore \
		-p:RunAnalyzers=true \
		-p:AnalysisLevel=latest-recommended
	@echo "$(GREEN)✅ Code analysis completed$(NC)"

format: ## Format code using dotnet format
	@echo "$(BLUE)✨ Formatting code...$(NC)"
	$(DOTNET) format $(SOLUTION) --verbosity $(VERBOSITY)
	@echo "$(GREEN)✅ Code formatting completed$(NC)"

format-check: ## Check code formatting without making changes
	@echo "$(BLUE)🔍 Checking code format...$(NC)"
	$(DOTNET) format $(SOLUTION) --verify-no-changes --verbosity $(VERBOSITY)

##@ Configuration Management

config-list: ## List all configuration files
	@echo "$(CYAN)📋 Configuration Files:$(NC)"
	@echo ""
	@echo "$(YELLOW)Application Settings:$(NC)"
	@find . -name "appsettings*.json" | grep -v obj | grep -v bin | sort | sed 's/^/  /'
	@echo ""
	@echo "$(YELLOW)Test Configuration:$(NC)"
	@find . -name "*.runsettings" -o -name "test*.json" | grep -v obj | grep -v bin | sort | sed 's/^/  /'
	@echo ""
	@echo "$(YELLOW)Docker Configuration:$(NC)"
	@find . -name "docker-compose*.yml" -o -name "Dockerfile*" | sort | sed 's/^/  /'
	@echo ""
	@echo "$(YELLOW)Build Configuration:$(NC)"
	@find . -name "*.props" -o -name "*.targets" | grep -v obj | grep -v bin | sort | sed 's/^/  /'

config-validate: ## Validate all configuration files
	@echo "$(BLUE)✔️  Validating configuration files...$(NC)"
	@VALID=0; \
	INVALID=0; \
	echo ""; \
	echo "$(YELLOW)JSON Files:$(NC)"; \
	for file in $$(find . -name "*.json" | grep -v obj | grep -v bin | grep -v node_modules); do \
		printf "  %-60s " "$$file"; \
		if jq . $$file > /dev/null 2>&1; then \
			echo -e "$(GREEN)✅ Valid$(NC)"; \
			VALID=$$((VALID+1)); \
		else \
			echo -e "$(RED)❌ Invalid$(NC)"; \
			INVALID=$$((INVALID+1)); \
		fi; \
	done; \
	echo ""; \
	echo "$(YELLOW)XML Files:$(NC)"; \
	for file in $$(find . -name "*.xml" -o -name "*.props" -o -name "*.targets" -o -name "*.csproj" | grep -v obj | grep -v bin); do \
		printf "  %-60s " "$$file"; \
		if xmllint --noout $$file 2>/dev/null; then \
			echo -e "$(GREEN)✅ Valid$(NC)"; \
			VALID=$$((VALID+1)); \
		else \
			echo -e "$(RED)❌ Invalid$(NC)"; \
			INVALID=$$((INVALID+1)); \
		fi; \
	done; \
	echo ""; \
	echo "$(CYAN)Summary: $(GREEN)$$VALID valid$(NC), $(RED)$$INVALID invalid$(NC)"

config-env: ## Generate environment template
	@echo "$(BLUE)📝 Generating environment template...$(NC)"
	@echo "# Neo Service Layer Environment Configuration" > .env.template
	@echo "# Generated on $(BUILD_DATE)" >> .env.template
	@echo "# Version: $(VERSION)" >> .env.template
	@echo "" >> .env.template
	@echo "# SGX Configuration" >> .env.template
	@echo "ENABLE_SGX=true" >> .env.template
	@echo "SGX_MODE=HW" >> .env.template
	@echo "" >> .env.template
	@echo "# Service Configuration" >> .env.template
	@echo "ASPNETCORE_ENVIRONMENT=Development" >> .env.template
	@echo "$(GREEN)✅ Created .env.template$(NC)"

config-check-env: ## Check environment variables
	@echo "$(BLUE)🔍 Checking environment configuration...$(NC)"
	@echo ""
	@echo "$(YELLOW)Required Environment Variables:$(NC)"
	@for var in ASPNETCORE_ENVIRONMENT DOTNET_ENVIRONMENT DATABASE_CONNECTION_STRING JWT_SECRET_KEY; do \
		if [ -z "$${!var}" ]; then \
			echo "  $(RED)❌ $$var - Not set$(NC)"; \
		else \
			echo "  $(GREEN)✅ $$var - Set$(NC)"; \
		fi; \
	done

config-setup: ## Interactive configuration setup
	@echo "$(BLUE)🔧 Interactive Configuration Setup$(NC)"
	@echo ""
	@if [ ! -f .env ]; then \
		echo "Creating .env file from template..."; \
		cp config/env.production.template .env; \
		echo "$(GREEN)✅ Created .env file$(NC)"; \
		echo "$(YELLOW)⚠️  Please edit .env file with your values$(NC)"; \
	else \
		echo "$(GREEN)✅ .env file already exists$(NC)"; \
	fi

config-diff: ## Show configuration differences between environments
	@echo "$(BLUE)📊 Configuration Differences$(NC)"
	@echo ""
	@echo "$(YELLOW)Development vs Production:$(NC)"
	@diff -u src/Web/NeoServiceLayer.Web/appsettings.Development.json \
		src/Web/NeoServiceLayer.Web/appsettings.Production.json || true

config-secrets: ## Manage user secrets
	@echo "$(BLUE)🔐 Managing User Secrets$(NC)"
	@echo ""
	@echo "Web Project Secrets:"
	@cd src/Web/NeoServiceLayer.Web && $(DOTNET) user-secrets list
	@echo ""
	@echo "API Project Secrets:"
	@cd src/Api/NeoServiceLayer.Api && $(DOTNET) user-secrets list

config-export: ## Export configuration for deployment
	@echo "$(BLUE)📤 Exporting configuration...$(NC)"
	@mkdir -p $(ARTIFACTS_DIR)/config
	@cp -r config/* $(ARTIFACTS_DIR)/config/
	@find . -name "appsettings.Production.json" -exec cp {} $(ARTIFACTS_DIR)/config/ \;
	@echo "$(GREEN)✅ Configuration exported to $(ARTIFACTS_DIR)/config$(NC)"

##@ Docker Operations

docker-build: ## Build Docker image
	@echo "$(BLUE)🐳 Building Docker image...$(NC)"
	docker build -t $(DOCKER_IMAGE):$(DOCKER_TAG) \
		--build-arg VERSION=$(VERSION) \
		--build-arg COMMIT=$(COMMIT) \
		--build-arg BUILD_DATE=$(BUILD_DATE) \
		-f docker/Dockerfile .
	@echo "$(GREEN)✅ Docker image built: $(DOCKER_IMAGE):$(DOCKER_TAG)$(NC)"

docker-run: docker-build ## Build and run Docker container
	@echo "$(BLUE)🚀 Running Docker container...$(NC)"
	docker run -d \
		--name neo-service-layer \
		-p 5000:5000 \
		-p 5001:5001 \
		$(DOCKER_IMAGE):$(DOCKER_TAG)
	@echo "$(GREEN)✅ Container started$(NC)"
	@echo "$(CYAN)Web Interface: http://localhost:5000$(NC)"
	@echo "$(CYAN)API Endpoint:  http://localhost:5001$(NC)"

docker-compose-up: ## Start services using docker-compose
	@echo "$(BLUE)🐳 Starting services with docker-compose...$(NC)"
	docker-compose -f $(COMPOSE_FILE) up -d
	@echo "$(GREEN)✅ Services started$(NC)"

docker-compose-down: ## Stop services using docker-compose
	@echo "$(YELLOW)🛑 Stopping services...$(NC)"
	docker-compose -f $(COMPOSE_FILE) down
	@echo "$(GREEN)✅ Services stopped$(NC)"

docker-logs: ## Show Docker container logs
	docker logs -f neo-service-layer

##@ Neo Express Operations

neo-express-setup: ## Setup Neo Express for local blockchain testing
	@echo "$(BLUE)🔧 Setting up Neo Express...$(NC)"
	@if ! command -v neoxp &> /dev/null; then \
		echo -e "$(YELLOW)Installing Neo Express...$(NC)"; \
		$(DOTNET) tool install -g Neo.Express; \
	fi
	@cd neo-express-test && neoxp create -f
	@echo "$(GREEN)✅ Neo Express setup completed$(NC)"

neo-express-start: ## Start Neo Express blockchain
	@echo "$(BLUE)▶️  Starting Neo Express...$(NC)"
	@cd neo-express-test && neoxp run

##@ Maintenance

clean: clean-sgx ## Clean build artifacts including SGX
	@echo "$(YELLOW)🧹 Cleaning build artifacts...$(NC)"
	$(DOTNET) clean $(SOLUTION) --configuration $(CONFIGURATION) --verbosity quiet
	@rm -rf $(RESULTS_DIR) $(COVERAGE_DIR) $(BENCHMARK_DIR) $(ARTIFACTS_DIR)
	@find . -type d -name "bin" -o -name "obj" | grep -v "node_modules" | xargs rm -rf
	@echo "$(GREEN)✅ Clean completed$(NC)"

clean-all: clean ## Deep clean including packages, caches, and Rust artifacts
	@echo "$(RED)🧹 Performing deep clean...$(NC)"
	@rm -rf ~/.nuget/packages/neoservicelayer.*
	@$(DOTNET) nuget locals all --clear
	@if [ -d "$(ENCLAVE_DIR)/target" ]; then \
		echo "  Cleaning Rust target directory..."; \
		rm -rf $(ENCLAVE_DIR)/target; \
	fi
	@echo "$(GREEN)✅ Deep clean completed$(NC)"

update-packages: ## Update NuGet packages
	@echo "$(BLUE)📦 Updating NuGet packages...$(NC)"
	@for proj in $$(find . -name "*.csproj" | grep -v obj); do \
		echo -e "  Updating $$proj..."; \
		$(DOTNET) list $$proj package --outdated; \
	done

deps-check: ## Check for security vulnerabilities in dependencies
	@echo "$(BLUE)🔒 Checking dependencies for vulnerabilities...$(NC)"
	@if command -v dotnet-outdated &> /dev/null; then \
		dotnet-outdated -u:Auto; \
	else \
		echo -e "$(YELLOW)⚠️  dotnet-outdated not installed. Install with: dotnet tool install -g dotnet-outdated-tool$(NC)"; \
	fi

##@ Development Tools

dev: ## Start development environment
	@echo "$(BLUE)🚀 Starting development environment...$(NC)"
	$(DOTNET) watch --project src/Web/NeoServiceLayer.Web run --urls "http://localhost:5000;https://localhost:5001"

dev-api: ## Start API development server
	@echo "$(BLUE)🚀 Starting API development server...$(NC)"
	$(DOTNET) watch --project src/Api/NeoServiceLayer.Api run --urls "http://localhost:5010;https://localhost:5011"

tools-install: ## Install required development tools
	@echo "$(BLUE)🔧 Installing development tools...$(NC)"
	$(DOTNET) tool install -g dotnet-reportgenerator-globaltool
	$(DOTNET) tool install -g dotnet-outdated-tool
	$(DOTNET) tool install -g Neo.Express
	@echo "$(GREEN)✅ Tools installed$(NC)"

##@ CI/CD Helpers

ci: clean restore build lint test-coverage ## Run CI pipeline locally
	@echo "$(GREEN)✅ CI pipeline completed successfully$(NC)"

package: publish ## Create deployment packages
	@echo "$(BLUE)📦 Creating deployment packages...$(NC)"
	@cd $(ARTIFACTS_DIR) && tar -czf neo-service-layer-$(VERSION).tar.gz *
	@echo "$(GREEN)✅ Package created: $(ARTIFACTS_DIR)/neo-service-layer-$(VERSION).tar.gz$(NC)"

release-notes: ## Generate release notes
	@echo "$(BLUE)📝 Generating release notes...$(NC)"
	@echo "# Release Notes - v$(VERSION)" > RELEASE_NOTES.md
	@echo "" >> RELEASE_NOTES.md
	@echo "## Changes since last release:" >> RELEASE_NOTES.md
	@git log --pretty=format:"- %s (%h)" --no-merges HEAD...$(shell git describe --tags --abbrev=0 2>/dev/null || echo HEAD~10) >> RELEASE_NOTES.md
	@echo "$(GREEN)✅ Release notes generated: RELEASE_NOTES.md$(NC)"

##@ Monitoring & Diagnostics

health-check: ## Check service health
	@echo "$(BLUE)🏥 Checking service health...$(NC)"
	@curl -f http://localhost:5000/api/health || echo -e "$(RED)❌ Service not responding$(NC)"

metrics: ## Display service metrics
	@echo "$(BLUE)📊 Service metrics:$(NC)"
	@curl -s http://localhost:5000/api/metrics | jq . || echo -e "$(YELLOW)⚠️  Metrics endpoint not available$(NC)"

logs: ## Tail application logs
	@echo "$(BLUE)📜 Tailing logs...$(NC)"
	@tail -f logs/*.log 2>/dev/null || echo -e "$(YELLOW)⚠️  No log files found$(NC)"

##@ Quick Commands

quick-test: restore ## Quick smoke test
	@echo "$(BLUE)🚀 Running quick smoke test...$(NC)"
	$(DOTNET) test tests/Core/NeoServiceLayer.Core.Tests \
		--configuration $(CONFIGURATION) \
		--no-restore \
		--verbosity minimal
	@echo "$(GREEN)✅ Quick test completed$(NC)"

run: build ## Run the web application
	@echo "$(BLUE)▶️  Starting Neo Service Layer...$(NC)"
	$(DOTNET) run --project src/Web/NeoServiceLayer.Web \
		--configuration $(CONFIGURATION) \
		--no-build \
		--urls "http://localhost:5000;https://localhost:5001"

run-api: build ## Run the API service
	@echo "$(BLUE)▶️  Starting API service...$(NC)"
	$(DOTNET) run --project src/Api/NeoServiceLayer.Api \
		--configuration $(CONFIGURATION) \
		--no-build \
		--urls "http://localhost:5010;https://localhost:5011"

##@ Utility Commands

clean-minimal: ## Minimal clean (bin/obj only)
	@echo "$(YELLOW)🧹 Minimal clean...$(NC)"
	@find . -type d -name "bin" -o -name "obj" | grep -v "node_modules" | xargs rm -rf
	@echo "$(GREEN)✅ Minimal clean completed$(NC)"

clean-logs: ## Clean log files
	@echo "$(YELLOW)🧹 Cleaning log files...$(NC)"
	@rm -rf $(LOGS_DIR)/*.log
	@find . -name "*.log" -type f -delete
	@echo "$(GREEN)✅ Log files cleaned$(NC)"

check-tools: ## Check required tools installation
	@echo "$(CYAN)🔧 Checking required tools...$(NC)"
	@echo ""
	@TOOLS_OK=true; \
	for tool in dotnet git docker docker-compose jq xmllint; do \
		printf "  %-20s " "$$tool"; \
		if command -v $$tool &> /dev/null; then \
			echo -e "$(GREEN)✅ Installed$(NC)"; \
		else \
			echo -e "$(RED)❌ Not installed$(NC)"; \
			TOOLS_OK=false; \
		fi; \
	done; \
	echo ""; \
	if [ "$$TOOLS_OK" = true ]; then \
		echo "$(GREEN)✅ All required tools are installed$(NC)"; \
	else \
		echo "$(RED)❌ Some tools are missing. Please install them.$(NC)"; \
	fi

project-stats: ## Show project statistics
	@echo "$(CYAN)📊 Project Statistics:$(NC)"
	@echo ""
	@echo "  Lines of C# Code:    $$(find . -name "*.cs" -type f | grep -v obj | grep -v bin | xargs wc -l | tail -1 | awk '{print $$1}')"
	@echo "  C# Files:            $$(find . -name "*.cs" -type f | grep -v obj | grep -v bin | wc -l)"
	@echo "  Test Files:          $$(find tests -name "*.cs" -type f | grep -v obj | grep -v bin | wc -l)"
	@echo "  Project Files:       $$(find . -name "*.csproj" -type f | wc -l)"
	@echo "  Services:            $$(find src/Services -type d -maxdepth 1 | tail -n +2 | wc -l)"
	@echo "  Docker Images:       $$(find . -name "Dockerfile*" -type f | wc -l)"
	@echo ""

scaffold-service: ## Scaffold a new service (use SERVICE_NAME=YourServiceName)
	@if [ -z "$(SERVICE_NAME)" ]; then \
		echo "$(RED)❌ Please specify SERVICE_NAME=YourServiceName$(NC)"; \
		exit 1; \
	fi
	@echo "$(BLUE)🏗️  Scaffolding new service: $(SERVICE_NAME)...$(NC)"
	@mkdir -p src/Services/NeoServiceLayer.Services.$(SERVICE_NAME)
	@mkdir -p tests/Services/NeoServiceLayer.Services.$(SERVICE_NAME).Tests
	@echo "$(GREEN)✅ Service structure created$(NC)"
	@echo "$(YELLOW)📝 TODO: Add service implementation and tests$(NC)"

backup: ## Create project backup
	@echo "$(BLUE)💾 Creating project backup...$(NC)"
	@BACKUP_NAME="neo-service-layer-backup-$$(date +%Y%m%d-%H%M%S).tar.gz"; \
	tar --exclude='./bin' --exclude='./obj' --exclude='./TestResults' --exclude='./CoverageReport' \
		--exclude='./BenchmarkResults' --exclude='./artifacts' --exclude='./.git' \
		-czf "../$$BACKUP_NAME" .; \
	echo "$(GREEN)✅ Backup created: ../$$BACKUP_NAME$(NC)"

##@ Help & Documentation

docs: ## Open project documentation
	@echo "$(BLUE)📚 Opening documentation...$(NC)"
	@if [ -f "docs/index.md" ]; then \
		if command -v xdg-open &> /dev/null; then \
			xdg-open docs/index.md; \
		elif command -v open &> /dev/null; then \
			open docs/index.md; \
		else \
			echo "$(YELLOW)Documentation is available at: docs/index.md$(NC)"; \
		fi; \
	else \
		echo "$(RED)❌ Documentation not found$(NC)"; \
	fi

changelog: ## Show recent changes
	@echo "$(CYAN)📝 Recent Changes:$(NC)"
	@git log --oneline --graph --decorate -10

todo: ## Show TODO items in code
	@echo "$(CYAN)📋 TODO Items:$(NC)"
	@grep -r "TODO\|FIXME\|HACK\|BUG" --include="*.cs" --include="*.md" . | grep -v Binary | head -20

# Include OS-specific targets if they exist
-include Makefile.$(shell uname -s)

# Special targets
.SILENT: version help status
.NOTPARALLEL: clean clean-all
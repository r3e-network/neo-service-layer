# JavaScript Engine Integration Plan for SGX Enclave

## Executive Summary

Building on the existing sophisticated computation service with security analysis and resource monitoring, this plan details the integration of a production JavaScript engine (V8/Deno) within the SGX enclave environment, replacing the current simulation-based execution with real JavaScript runtime capabilities.

## Current Foundation Analysis

### Existing Computation Service Strengths
- **Advanced Security Analysis**: Comprehensive pattern detection for dangerous code constructs
- **Resource Monitoring**: Production-grade memory usage estimation and performance tracking  
- **Code Complexity Analysis**: McCabe complexity calculation with recursion detection
- **Execution Context Management**: Sophisticated timeout and resource limit enforcement
- **Performance Profiling**: Real-time metrics collection with system resource tracking

### Integration Requirements
- **V8 Isolate Integration**: Secure JavaScript execution with memory constraints
- **Deno Core Runtime**: Modern JavaScript/TypeScript with built-in security
- **WebAssembly Support**: High-performance computation capabilities
- **SGX Compatibility**: Hardware enclave constraints and memory management
- **API Surface Control**: Restricted global object access and custom APIs

## JavaScript Engine Architecture

### 1. Dual Engine Strategy

```rust
/// JavaScript engine abstraction supporting multiple runtimes
pub trait JavaScriptEngine: Send + Sync {
    /// Execute JavaScript code with security constraints
    async fn execute(
        &mut self,
        code: &str,
        context: &ExecutionContext,
        input_data: &[u8],
    ) -> Result<ExecutionResult>;
    
    /// Compile JavaScript to bytecode for performance
    fn compile(&mut self, code: &str) -> Result<CompiledScript>;
    
    /// Execute pre-compiled script
    async fn execute_compiled(
        &mut self,
        script: &CompiledScript,
        context: &ExecutionContext,
        input_data: &[u8],
    ) -> Result<ExecutionResult>;
    
    /// Get engine capabilities and limits
    fn capabilities(&self) -> EngineCapabilities;
    
    /// Reset engine state for security
    fn reset(&mut self) -> Result<()>;
}

/// Multi-engine JavaScript runtime with automatic engine selection
pub struct MultiEngineJavaScriptRuntime {
    /// V8 engine for high-performance execution
    v8_engine: Option<V8JavaScriptEngine>,
    /// Deno engine for modern JavaScript with built-in security
    deno_engine: Option<DenoJavaScriptEngine>,
    /// WebAssembly engine for compiled modules
    wasm_engine: WasmJavaScriptEngine,
    /// Engine selection strategy
    selection_strategy: EngineSelectionStrategy,
    /// Performance monitor for engine comparison
    performance_monitor: EnginePerformanceMonitor,
}

impl MultiEngineJavaScriptRuntime {
    /// Select optimal engine based on code characteristics
    pub fn select_engine(&self, code: &str, context: &ExecutionContext) -> EngineType {
        match self.selection_strategy {
            EngineSelectionStrategy::Performance => {
                if self.is_compute_intensive(code) {
                    if self.can_compile_to_wasm(code) {
                        EngineType::WebAssembly
                    } else {
                        EngineType::V8
                    }
                } else {
                    EngineType::Deno
                }
            }
            EngineSelectionStrategy::Security => {
                // Deno has better built-in security model
                EngineType::Deno
            }
            EngineSelectionStrategy::Compatibility => {
                // V8 has widest compatibility
                EngineType::V8
            }
            EngineSelectionStrategy::Adaptive => {
                self.adaptive_engine_selection(code, context)
            }
        }
    }
}
```

### 2. V8 Engine Integration

```rust
/// Production V8 JavaScript engine with SGX optimization
pub struct V8JavaScriptEngine {
    /// V8 isolate with security constraints
    isolate: v8::OwnedIsolate,
    /// Security policy enforcement
    security_enforcer: V8SecurityEnforcer,
    /// Resource management
    resource_manager: V8ResourceManager,
    /// Custom API registry
    api_registry: CustomAPIRegistry,
    /// Performance profiler
    profiler: V8Profiler,
    /// Memory allocator for SGX
    sgx_allocator: SGXMemoryAllocator,
}

impl V8JavaScriptEngine {
    /// Create V8 engine optimized for SGX environment
    pub fn new(config: &JavaScriptEngineConfig) -> Result<Self> {
        // 1. Initialize V8 platform with SGX considerations
        let platform = v8::new_default_platform(
            config.max_threads.min(4), // Limit threads in SGX
            false, // Disable idle task support for determinism
        ).make_shared();
        v8::V8::initialize_platform(platform);
        v8::V8::initialize();
        
        // 2. Create isolate with memory constraints
        let create_params = v8::Isolate::create_params()
            .max_old_space_size(config.max_memory_mb)
            .max_young_space_size(config.max_heap_mb)
            .array_buffer_allocator(Some(
                SGXArrayBufferAllocator::new(config.max_memory_mb * 1024 * 1024)
            ));
        
        let isolate = v8::Isolate::new(create_params);
        
        // 3. Setup security constraints
        let security_enforcer = V8SecurityEnforcer::new(&config.security_policy)?;
        
        // 4. Initialize resource management
        let resource_manager = V8ResourceManager::new(config)?;
        
        Ok(Self {
            isolate,
            security_enforcer,
            resource_manager,
            api_registry: CustomAPIRegistry::new(),
            profiler: V8Profiler::new(),
            sgx_allocator: SGXMemoryAllocator::new(),
        })
    }
    
    /// Execute JavaScript with comprehensive monitoring
    pub async fn execute_secure(
        &mut self,
        code: &str,
        context: &ExecutionContext,
        input_data: &[u8],
    ) -> Result<ExecutionResult> {
        // 1. Pre-execution security validation
        self.security_enforcer.validate_code(code)?;
        
        // 2. Setup execution environment
        let handle_scope = &mut v8::HandleScope::new(&mut self.isolate);
        
        // 3. Create secure context
        let global_template = self.create_secure_global_template(handle_scope, context)?;
        let context_local = v8::Context::new_from_template(handle_scope, global_template);
        let context_scope = &mut v8::ContextScope::new(handle_scope, context_local);
        
        // 4. Setup custom APIs
        self.setup_custom_apis(context_scope, input_data)?;
        
        // 5. Compile script with validation
        let source = v8::String::new(context_scope, code)
            .ok_or_else(|| anyhow!("Failed to create V8 string"))?;
        
        let script = v8::Script::compile(context_scope, source, None)
            .ok_or_else(|| anyhow!("Failed to compile JavaScript"))?;
        
        // 6. Execute with resource monitoring
        self.resource_manager.start_monitoring()?;
        
        let result = script.run(context_scope)
            .ok_or_else(|| anyhow!("JavaScript execution failed"))?;
        
        let metrics = self.resource_manager.stop_monitoring()?;
        
        // 7. Convert result to safe format
        let output = self.convert_v8_value_to_safe_output(context_scope, result)?;
        
        Ok(ExecutionResult {
            output,
            metrics,
            console_output: self.get_console_output(),
            execution_time_ms: metrics.execution_time_ms,
            memory_used_bytes: metrics.peak_memory_bytes,
        })
    }
    
    /// Create secure global template with restricted APIs
    fn create_secure_global_template(
        &self,
        scope: &mut v8::HandleScope,
        context: &ExecutionContext,
    ) -> Result<v8::Local<v8::ObjectTemplate>> {
        let global = v8::ObjectTemplate::new(scope);
        
        // Add allowed JavaScript built-ins
        if context.allowed_apis.contains(&"Math".to_string()) {
            self.add_math_api(scope, &global)?;
        }
        
        if context.allowed_apis.contains(&"JSON".to_string()) {
            self.add_json_api(scope, &global)?;
        }
        
        if context.allowed_apis.contains(&"Date".to_string()) {
            self.add_date_api(scope, &global)?;
        }
        
        // Add custom secure APIs
        self.add_console_api(scope, &global)?;
        self.add_crypto_api(scope, &global)?;
        
        // Explicitly disable dangerous globals
        self.disable_dangerous_globals(scope, &global)?;
        
        Ok(global)
    }
    
    /// Disable dangerous global objects and functions
    fn disable_dangerous_globals(
        &self,
        scope: &mut v8::HandleScope,
        global: &v8::Local<v8::ObjectTemplate>,
    ) -> Result<()> {
        let undefined = v8::undefined(scope);
        
        // Disable network access
        global.set(v8::String::new(scope, "fetch").unwrap().into(), undefined.into());
        global.set(v8::String::new(scope, "XMLHttpRequest").unwrap().into(), undefined.into());
        global.set(v8::String::new(scope, "WebSocket").unwrap().into(), undefined.into());
        
        // Disable file system access
        global.set(v8::String::new(scope, "require").unwrap().into(), undefined.into());
        global.set(v8::String::new(scope, "import").unwrap().into(), undefined.into());
        
        // Disable dynamic code execution
        global.set(v8::String::new(scope, "eval").unwrap().into(), undefined.into());
        global.set(v8::String::new(scope, "Function").unwrap().into(), undefined.into());
        
        // Disable DOM access (not applicable in enclave but for safety)
        global.set(v8::String::new(scope, "document").unwrap().into(), undefined.into());
        global.set(v8::String::new(scope, "window").unwrap().into(), undefined.into());
        
        Ok(())
    }
}

/// V8 resource manager with SGX-specific optimizations
pub struct V8ResourceManager {
    /// Memory usage tracker
    memory_tracker: MemoryTracker,
    /// CPU time limiter
    cpu_limiter: CPULimiter,
    /// GC pressure monitor
    gc_monitor: GCPressureMonitor,
    /// Performance metrics collector
    metrics_collector: V8MetricsCollector,
}

impl V8ResourceManager {
    /// Start resource monitoring for execution
    pub fn start_monitoring(&mut self) -> Result<()> {
        self.memory_tracker.start_tracking()?;
        self.cpu_limiter.start_timing()?;
        self.gc_monitor.start_monitoring()?;
        Ok(())
    }
    
    /// Stop monitoring and collect metrics
    pub fn stop_monitoring(&mut self) -> Result<ExecutionMetrics> {
        let memory_metrics = self.memory_tracker.stop_tracking()?;
        let cpu_metrics = self.cpu_limiter.stop_timing()?;
        let gc_metrics = self.gc_monitor.stop_monitoring()?;
        
        Ok(ExecutionMetrics {
            execution_time_ms: cpu_metrics.execution_time_ms,
            peak_memory_bytes: memory_metrics.peak_memory_bytes,
            gc_collections: gc_metrics.collections,
            gc_time_ms: gc_metrics.total_time_ms,
            compiled_size_bytes: memory_metrics.compiled_size_bytes,
        })
    }
}
```

### 3. Deno Core Integration

```rust
/// Deno Core JavaScript engine with built-in security model
pub struct DenoJavaScriptEngine {
    /// Deno runtime with security restrictions
    runtime: deno_core::JsRuntime,
    /// Extension manager for custom APIs
    extension_manager: DenoExtensionManager,
    /// Permission system
    permission_system: DenoPermissionSystem,
    /// Module resolver with security constraints
    module_resolver: SecureModuleResolver,
}

impl DenoJavaScriptEngine {
    /// Create Deno engine with security-first configuration
    pub fn new(config: &JavaScriptEngineConfig) -> Result<Self> {
        // 1. Create secure runtime options
        let options = deno_core::RuntimeOptions {
            extensions: vec![
                // Core extension with minimal permissions
                create_secure_core_extension(),
                // Custom crypto extension
                create_crypto_extension(),
                // Limited console extension
                create_console_extension(),
            ],
            module_loader: Some(Rc::new(SecureModuleLoader::new())),
            startup_snapshot: None, // Build custom snapshot for SGX
            will_snapshot: false,
            create_params: Some(
                v8::CreateParams::default()
                    .max_old_space_size(config.max_memory_mb)
                    .max_young_space_size(config.max_heap_mb)
            ),
            v8_platform: None, // Use shared platform
            shared_array_buffer_store: None,
            compiled_wasm_module_store: None,
            inspector: false, // Disable inspector in enclave
            is_main: true,
            feature_checker: None,
        };
        
        let runtime = deno_core::JsRuntime::new(options);
        
        Ok(Self {
            runtime,
            extension_manager: DenoExtensionManager::new(),
            permission_system: DenoPermissionSystem::new(&config.security_policy),
            module_resolver: SecureModuleResolver::new(),
        })
    }
    
    /// Execute JavaScript with Deno's security model
    pub async fn execute_secure(
        &mut self,
        code: &str,
        context: &ExecutionContext,
        input_data: &[u8],
    ) -> Result<ExecutionResult> {
        // 1. Validate permissions
        self.permission_system.validate_code_permissions(code)?;
        
        // 2. Setup execution context
        self.setup_execution_context(context, input_data)?;
        
        // 3. Execute with timeout
        let result = tokio::time::timeout(
            Duration::from_millis(context.timeout_ms),
            self.execute_code_internal(code),
        ).await??;
        
        Ok(result)
    }
    
    /// Internal code execution with monitoring
    async fn execute_code_internal(&mut self, code: &str) -> Result<ExecutionResult> {
        // 1. Create module source
        let module_source = deno_core::ModuleSource {
            code: code.into(),
            module_type: deno_core::ModuleType::JavaScript,
            module_url_specified: "enclave://script.js".to_string(),
            module_url_found: "enclave://script.js".to_string(),
        };
        
        // 2. Load and evaluate module
        let module_id = self.runtime
            .load_side_module(&module_source, None)
            .await?;
        
        let receiver = self.runtime.mod_evaluate(module_id);
        
        // 3. Run event loop until completion
        self.runtime.run_event_loop(false).await?;
        
        // 4. Get result
        let result = receiver.await?;
        
        // 5. Convert to safe format
        let execution_result = self.convert_deno_result_to_safe_output(result)?;
        
        Ok(execution_result)
    }
}

/// Secure module resolver for Deno with whitelist-based resolution
pub struct SecureModuleResolver {
    /// Allowed module whitelist
    allowed_modules: HashSet<String>,
    /// Built-in module implementations
    builtin_modules: HashMap<String, ModuleImplementation>,
}

impl SecureModuleResolver {
    /// Resolve module with security validation
    pub fn resolve_module(
        &self,
        specifier: &str,
        referrer: &str,
    ) -> Result<ModuleResolution> {
        // Only allow whitelisted modules
        if !self.allowed_modules.contains(specifier) {
            return Err(anyhow!("Module '{}' is not allowed", specifier));
        }
        
        // Provide built-in implementations for security
        if let Some(implementation) = self.builtin_modules.get(specifier) {
            return Ok(ModuleResolution::Builtin(implementation.clone()));
        }
        
        Err(anyhow!("Module '{}' not found", specifier))
    }
}
```

### 4. WebAssembly Integration

```rust
/// WebAssembly engine for high-performance computation
pub struct WasmJavaScriptEngine {
    /// Wasmtime runtime with security configuration
    engine: wasmtime::Engine,
    /// Module store for compiled WASM
    store: wasmtime::Store<WasmState>,
    /// Linker for host functions
    linker: wasmtime::Linker<WasmState>,
    /// Memory limiter for SGX constraints
    memory_limiter: WasmMemoryLimiter,
}

impl WasmJavaScriptEngine {
    /// Create WASM engine optimized for enclave
    pub fn new(config: &JavaScriptEngineConfig) -> Result<Self> {
        // 1. Configure engine with security settings
        let engine_config = wasmtime::Config::new()
            .wasm_backtrace_details(wasmtime::WasmBacktraceDetails::Enable)
            .wasm_reference_types(false) // Disable for simplicity
            .wasm_bulk_memory(true)
            .wasm_threads(false) // Single-threaded in SGX
            .cranelift_opt_level(wasmtime::OptLevel::Speed);
        
        let engine = wasmtime::Engine::new(&engine_config)?;
        
        // 2. Create store with memory limits
        let wasm_state = WasmState::new(config.max_memory_mb * 1024 * 1024);
        let mut store = wasmtime::Store::new(&engine, wasm_state);
        
        store.limiter(|state| &mut state.limiter);
        
        // 3. Setup linker with secure host functions
        let mut linker = wasmtime::Linker::new(&engine);
        Self::setup_secure_host_functions(&mut linker)?;
        
        Ok(Self {
            engine,
            store,
            linker,
            memory_limiter: WasmMemoryLimiter::new(config.max_memory_mb),
        })
    }
    
    /// Execute WASM module with security constraints
    pub async fn execute_wasm(
        &mut self,
        wasm_bytes: &[u8],
        input_data: &[u8],
    ) -> Result<ExecutionResult> {
        // 1. Validate WASM module
        self.validate_wasm_module(wasm_bytes)?;
        
        // 2. Compile module
        let module = wasmtime::Module::new(&self.engine, wasm_bytes)?;
        
        // 3. Instantiate with security constraints
        let instance = self.linker.instantiate(&mut self.store, &module)?;
        
        // 4. Setup input data
        self.setup_wasm_input_data(&instance, input_data)?;
        
        // 5. Call main function with monitoring
        let start_time = std::time::Instant::now();
        
        let main_func = instance
            .get_typed_func::<(), i32>(&mut self.store, "main")?;
        
        let result = main_func.call(&mut self.store, ())?;
        
        let execution_time = start_time.elapsed();
        
        // 6. Extract output data
        let output_data = self.extract_wasm_output_data(&instance)?;
        
        Ok(ExecutionResult {
            output: output_data,
            metrics: ExecutionMetrics {
                execution_time_ms: execution_time.as_millis() as u64,
                peak_memory_bytes: self.memory_limiter.get_peak_usage(),
                ..Default::default()
            },
            console_output: vec![], // WASM doesn't have console by default
            execution_time_ms: execution_time.as_millis() as u64,
            memory_used_bytes: self.memory_limiter.get_current_usage(),
        })
    }
}
```

### 5. Integration with Existing Computation Service

```rust
/// Enhanced computation service with real JavaScript engine integration
pub struct EnhancedComputationService {
    /// Original computation service functionality
    base_service: ComputationService,
    /// Multi-engine JavaScript runtime
    js_runtime: Arc<Mutex<MultiEngineJavaScriptRuntime>>,
    /// Code analysis and security validation
    code_analyzer: CodeAnalyzer,
    /// Performance benchmarking
    performance_tracker: PerformanceTracker,
    /// Engine configuration
    engine_config: JavaScriptEngineConfig,
}

impl EnhancedComputationService {
    /// Execute JavaScript with real engine (replacing simulation)
    pub async fn execute_javascript_real(
        &self,
        code: &str,
        args: &str,
        execution_context: &ExecutionContext,
    ) -> Result<String> {
        // 1. Perform existing security analysis
        let security_issues = analyze_code_security(code);
        if !security_issues.is_empty() {
            return Err(anyhow!("Security violations detected: {:?}", security_issues));
        }
        
        // 2. Analyze code complexity for engine selection
        let complexity = analyze_code_complexity(code);
        
        // 3. Select optimal JavaScript engine
        let mut runtime = self.js_runtime.lock().await;
        let engine_type = runtime.select_engine(code, execution_context);
        
        // 4. Execute with selected engine
        let result = match engine_type {
            EngineType::V8 => {
                if let Some(engine) = &mut runtime.v8_engine {
                    engine.execute_secure(code, execution_context, args.as_bytes()).await?
                } else {
                    return Err(anyhow!("V8 engine not available"));
                }
            }
            EngineType::Deno => {
                if let Some(engine) = &mut runtime.deno_engine {
                    engine.execute_secure(code, execution_context, args.as_bytes()).await?
                } else {
                    return Err(anyhow!("Deno engine not available"));
                }
            }
            EngineType::WebAssembly => {
                // Compile JavaScript to WASM if possible, otherwise fallback
                if let Ok(wasm_bytes) = self.compile_js_to_wasm(code).await {
                    runtime.wasm_engine.execute_wasm(&wasm_bytes, args.as_bytes()).await?
                } else {
                    // Fallback to V8
                    if let Some(engine) = &mut runtime.v8_engine {
                        engine.execute_secure(code, execution_context, args.as_bytes()).await?
                    } else {
                        return Err(anyhow!("No suitable engine available"));
                    }
                }
            }
        };
        
        // 5. Track performance metrics
        self.performance_tracker.record_execution(&result, &complexity, engine_type);
        
        // 6. Format response with enhanced metadata
        let enhanced_response = serde_json::json!({
            "result": result.output,
            "execution_time_ms": result.execution_time_ms,
            "memory_used_bytes": result.memory_used_bytes,
            "engine_used": format!("{:?}", engine_type),
            "complexity_score": complexity.cyclomatic_complexity,
            "security_analysis": "passed",
            "performance_tier": self.classify_performance_tier(&result),
            "timestamp": SystemTime::now()
                .duration_since(SystemTime::UNIX_EPOCH)
                .unwrap_or_default()
                .as_secs(),
        });
        
        Ok(enhanced_response.to_string())
    }
}
```

## SGX-Specific Considerations

### 1. Memory Management in SGX

```rust
/// SGX-optimized memory allocator for JavaScript engines
pub struct SGXMemoryAllocator {
    /// Memory pool for deterministic allocation
    memory_pool: MemoryPool,
    /// Allocation tracking
    allocation_tracker: AllocationTracker,
    /// Memory pressure monitor
    pressure_monitor: MemoryPressureMonitor,
}

impl SGXMemoryAllocator {
    /// Allocate memory with SGX constraints
    pub fn allocate(&mut self, size: usize, alignment: usize) -> Result<*mut u8> {
        // Check available memory
        if !self.memory_pool.can_allocate(size) {
            // Try garbage collection first
            self.trigger_gc()?;
            
            if !self.memory_pool.can_allocate(size) {
                return Err(anyhow!("Insufficient memory in SGX enclave"));
            }
        }
        
        let ptr = self.memory_pool.allocate(size, alignment)?;
        self.allocation_tracker.track_allocation(ptr, size);
        
        Ok(ptr)
    }
    
    /// Deallocate memory and clear sensitive data
    pub fn deallocate(&mut self, ptr: *mut u8, size: usize) {
        // Clear memory for security
        unsafe {
            std::ptr::write_bytes(ptr, 0, size);
        }
        
        self.memory_pool.deallocate(ptr, size);
        self.allocation_tracker.untrack_allocation(ptr);
    }
}
```

### 2. Performance Optimization Strategy

```rust
/// Performance optimization configuration for SGX environment
pub struct SGXPerformanceConfig {
    /// JIT compilation settings
    pub jit_enabled: bool,
    pub max_compilation_time_ms: u64,
    
    /// Memory optimization
    pub gc_strategy: GCStrategy,
    pub memory_compaction_threshold: f32,
    
    /// Code caching
    pub enable_bytecode_cache: bool,
    pub cache_size_limit_mb: usize,
    
    /// Threading constraints
    pub max_worker_threads: usize,
    pub enable_parallel_compilation: bool,
}

/// Garbage collection strategy for SGX
#[derive(Debug, Clone)]
pub enum GCStrategy {
    /// Conservative GC with predictable timing
    Conservative { max_pause_ms: u64 },
    /// Incremental GC with smaller pauses
    Incremental { target_pause_ms: u64 },
    /// Generational GC optimized for typical JS workloads
    Generational { young_gen_size_mb: usize },
}
```

## Implementation Phases

### Phase 1: V8 Integration (Week 1)
1. **Basic V8 Setup**: Initialize V8 isolate with SGX constraints
2. **Security Hardening**: Implement API restrictions and global object control
3. **Resource Management**: Memory limits and CPU timeout enforcement
4. **Basic Execution**: Replace simulation with real V8 execution

### Phase 2: Deno Integration (Week 2)  
1. **Deno Core Setup**: Configure Deno runtime with security extensions
2. **Module System**: Implement secure module resolver with whitelist
3. **Permission System**: Integrate Deno's permission model
4. **TypeScript Support**: Enable TypeScript compilation in enclave

### Phase 3: WebAssembly Integration (Week 3)
1. **Wasmtime Setup**: Configure WASM runtime with memory limits
2. **Host Functions**: Implement secure host function interface
3. **JS-to-WASM Compilation**: Optional compilation for performance
4. **Performance Optimization**: Benchmark and optimize all engines

### Phase 4: Production Integration (Week 4)
1. **Engine Selection**: Implement adaptive engine selection strategy
2. **Caching System**: Bytecode caching for improved performance
3. **Monitoring Integration**: Connect with existing performance tracking
4. **Testing and Validation**: Comprehensive security and performance testing

## Security Guarantees

### JavaScript Engine Security
1. **API Surface Control**: Whitelist-based global object access
2. **Code Validation**: Pre-execution security analysis and pattern detection
3. **Resource Limits**: Strict memory, CPU, and timeout enforcement
4. **Memory Isolation**: SGX hardware-backed memory protection

### Performance Targets
1. **Execution Latency**: <50ms for typical JavaScript operations  
2. **Memory Overhead**: <32MB additional memory per engine instance
3. **Startup Time**: <100ms for engine initialization
4. **Compilation Time**: <200ms for bytecode compilation

This JavaScript engine integration plan provides a comprehensive roadmap for replacing the simulation-based execution with production-ready JavaScript engines while maintaining the excellent security and performance characteristics of the existing computation service.
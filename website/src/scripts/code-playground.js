/**
 * Neo Service Layer Documentation Code Playground
 * Interactive code editor and executor for SDK examples
 */

class CodePlayground {
    constructor() {
        this.editors = new Map();
        this.consoleOutputs = new Map();
        this.isInitialized = false;
        
        // Mock SDK for playground execution
        this.mockSDK = null;
        
        this.init();
    }
    
    async init() {
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.setupPlaygrounds());
        } else {
            this.setupPlaygrounds();
        }
    }
    
    setupPlaygrounds() {
        // Find all code playgrounds
        const playgrounds = document.querySelectorAll('.code-playground');
        
        playgrounds.forEach((playground, index) => {
            this.initializePlayground(playground, `playground-${index}`);
        });
        
        // Initialize mock SDK
        this.initializeMockSDK();
        
        this.isInitialized = true;
    }
    
    initializePlayground(container, id) {
        const codeBlock = container.querySelector('code');
        const initialCode = codeBlock ? codeBlock.textContent.trim() : '';
        
        // Create playground structure
        container.innerHTML = `
            <div class="playground-header">
                <div class="playground-title">
                    <i class="ti ti-code"></i>
                    Interactive Example
                </div>
                <div class="playground-actions">
                    <button class="playground-button playground-run" data-id="${id}">
                        <i class="ti ti-player-play"></i>
                        Run Code
                    </button>
                    <button class="playground-button playground-reset" data-id="${id}">
                        <i class="ti ti-refresh"></i>
                        Reset
                    </button>
                    <button class="playground-button playground-copy" data-id="${id}">
                        <i class="ti ti-copy"></i>
                        Copy
                    </button>
                </div>
            </div>
            <div class="playground-content">
                <div class="playground-editor-container">
                    <div class="playground-editor" id="editor-${id}"></div>
                </div>
                <div class="playground-output-container">
                    <div class="playground-output-header">
                        <i class="ti ti-terminal-2"></i>
                        Console Output
                        <button class="playground-clear-output" data-id="${id}">
                            <i class="ti ti-trash"></i>
                        </button>
                    </div>
                    <div class="playground-output" id="output-${id}"></div>
                </div>
            </div>
        `;
        
        // Initialize code editor (using simple textarea for now, could integrate Monaco Editor)
        this.initializeEditor(id, initialCode);
        
        // Setup event listeners
        this.setupEventListeners(id);
    }
    
    initializeEditor(id, initialCode) {
        const editorContainer = document.getElementById(`editor-${id}`);
        
        // Create textarea editor (in production, you'd use Monaco Editor or CodeMirror)
        const textarea = document.createElement('textarea');
        textarea.className = 'playground-textarea';
        textarea.value = initialCode;
        textarea.spellcheck = false;
        
        // Add syntax highlighting and line numbers
        const editorWrapper = document.createElement('div');
        editorWrapper.className = 'editor-wrapper';
        
        const lineNumbers = document.createElement('div');
        lineNumbers.className = 'line-numbers';
        
        editorWrapper.appendChild(lineNumbers);
        editorWrapper.appendChild(textarea);
        editorContainer.appendChild(editorWrapper);
        
        // Store editor reference
        this.editors.set(id, {
            textarea,
            lineNumbers,
            initialCode
        });
        
        // Update line numbers
        this.updateLineNumbers(id);
        
        // Setup textarea events
        textarea.addEventListener('input', () => {
            this.updateLineNumbers(id);
        });
        
        textarea.addEventListener('scroll', () => {
            lineNumbers.scrollTop = textarea.scrollTop;
        });
    }
    
    updateLineNumbers(id) {
        const editor = this.editors.get(id);
        if (!editor) return;
        
        const lines = editor.textarea.value.split('\\n');
        const lineNumbersHTML = lines.map((_, index) => 
            `<div class="line-number">${index + 1}</div>`
        ).join('');
        
        editor.lineNumbers.innerHTML = lineNumbersHTML;
    }
    
    setupEventListeners(id) {
        // Run button
        const runButton = document.querySelector(`[data-id="${id}"].playground-run`);
        runButton.addEventListener('click', () => this.runCode(id));
        
        // Reset button
        const resetButton = document.querySelector(`[data-id="${id}"].playground-reset`);
        resetButton.addEventListener('click', () => this.resetCode(id));
        
        // Copy button
        const copyButton = document.querySelector(`[data-id="${id}"].playground-copy`);
        copyButton.addEventListener('click', () => this.copyCode(id));
        
        // Clear output button
        const clearButton = document.querySelector(`[data-id="${id}"].playground-clear-output`);
        clearButton.addEventListener('click', () => this.clearOutput(id));
        
        // Keyboard shortcuts
        const editor = this.editors.get(id);
        if (editor) {
            editor.textarea.addEventListener('keydown', (e) => {
                // Ctrl/Cmd + Enter to run
                if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
                    e.preventDefault();
                    this.runCode(id);
                }
                
                // Tab for indentation
                if (e.key === 'Tab') {
                    e.preventDefault();
                    const start = e.target.selectionStart;
                    const end = e.target.selectionEnd;
                    const value = e.target.value;
                    
                    e.target.value = value.substring(0, start) + '  ' + value.substring(end);
                    e.target.selectionStart = e.target.selectionEnd = start + 2;
                    
                    this.updateLineNumbers(id);
                }
            });
        }
    }
    
    async runCode(id) {
        const editor = this.editors.get(id);
        if (!editor) return;
        
        const code = editor.textarea.value;
        const output = document.getElementById(`output-${id}`);
        
        // Update button state
        const runButton = document.querySelector(`[data-id="${id}"].playground-run`);
        const originalHTML = runButton.innerHTML;
        runButton.innerHTML = '<i class="ti ti-loader-2 ti-spin"></i> Running...';
        runButton.disabled = true;
        
        try {
            // Clear previous output
            this.clearOutput(id);
            
            // Capture console output
            this.captureConsoleOutput(id);
            
            // Execute code in sandboxed environment
            await this.executeCode(code, id);
            
        } catch (error) {
            this.logToOutput(id, `Error: ${error.message}`, 'error');
        } finally {
            // Restore button state
            runButton.innerHTML = originalHTML;
            runButton.disabled = false;
            
            // Restore console
            this.restoreConsole();
        }
    }
    
    async executeCode(code, playgroundId) {
        // Create a sandboxed execution environment
        const sandbox = {
            console: {
                log: (...args) => this.logToOutput(playgroundId, args.join(' '), 'log'),
                error: (...args) => this.logToOutput(playgroundId, args.join(' '), 'error'),
                warn: (...args) => this.logToOutput(playgroundId, args.join(' '), 'warn'),
                info: (...args) => this.logToOutput(playgroundId, args.join(' '), 'info')
            },
            sdk: this.mockSDK,
            // Add other safe globals as needed
            setTimeout: (fn, delay) => setTimeout(fn, Math.min(delay, 5000)), // Max 5s delay
            clearTimeout,
            Promise,
            JSON,
            Math,
            Date
        };
        
        // Wrap code in async function for await support
        const wrappedCode = `
            (async function() {
                ${code}
            })();
        `;
        
        try {
            // Create function with sandbox context
            const func = new Function(
                ...Object.keys(sandbox),
                `return ${wrappedCode}`
            );
            
            // Execute with sandbox values
            const result = await func(...Object.values(sandbox));
            
            if (result !== undefined) {
                this.logToOutput(playgroundId, `Result: ${JSON.stringify(result, null, 2)}`, 'result');
            }
            
        } catch (error) {
            throw error;
        }
    }
    
    captureConsoleOutput(id) {
        // Store original console methods
        this.originalConsole = {
            log: console.log,
            error: console.error,
            warn: console.warn,
            info: console.info
        };
        
        // Override console methods
        console.log = (...args) => {
            this.originalConsole.log(...args);
            this.logToOutput(id, args.join(' '), 'log');
        };
        
        console.error = (...args) => {
            this.originalConsole.error(...args);
            this.logToOutput(id, args.join(' '), 'error');
        };
        
        console.warn = (...args) => {
            this.originalConsole.warn(...args);
            this.logToOutput(id, args.join(' '), 'warn');
        };
        
        console.info = (...args) => {
            this.originalConsole.info(...args);
            this.logToOutput(id, args.join(' '), 'info');
        };
    }
    
    restoreConsole() {
        if (this.originalConsole) {
            console.log = this.originalConsole.log;
            console.error = this.originalConsole.error;
            console.warn = this.originalConsole.warn;
            console.info = this.originalConsole.info;
        }
    }
    
    logToOutput(id, message, type = 'log') {
        const output = document.getElementById(`output-${id}`);
        if (!output) return;
        
        const logElement = document.createElement('div');
        logElement.className = `output-line output-${type}`;
        
        // Add timestamp
        const timestamp = new Date().toLocaleTimeString();
        logElement.innerHTML = `
            <span class="output-timestamp">[${timestamp}]</span>
            <span class="output-content">${this.escapeHtml(message)}</span>
        `;
        
        output.appendChild(logElement);
        output.scrollTop = output.scrollHeight;
    }
    
    resetCode(id) {
        const editor = this.editors.get(id);
        if (!editor) return;
        
        editor.textarea.value = editor.initialCode;
        this.updateLineNumbers(id);
        this.clearOutput(id);
    }
    
    copyCode(id) {
        const editor = this.editors.get(id);
        if (!editor) return;
        
        const code = editor.textarea.value;
        
        navigator.clipboard.writeText(code).then(() => {
            const button = document.querySelector(`[data-id="${id}"].playground-copy`);
            const originalHTML = button.innerHTML;
            
            button.innerHTML = '<i class="ti ti-check"></i> Copied!';
            button.style.color = 'var(--docs-accent-green)';
            
            setTimeout(() => {
                button.innerHTML = originalHTML;
                button.style.color = '';
            }, 2000);
        });
    }
    
    clearOutput(id) {
        const output = document.getElementById(`output-${id}`);
        if (output) {
            output.innerHTML = '';
        }
    }
    
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
    
    initializeMockSDK() {
        // Create a mock SDK for playground demonstrations
        this.mockSDK = {
            storage: {
                store: async (data) => {
                    await this.simulateDelay(500);
                    return {
                        transactionId: 'tx_' + Math.random().toString(36).substr(2, 9),
                        key: data.key,
                        success: true,
                        timestamp: new Date().toISOString()
                    };
                },
                
                get: async (key) => {
                    await this.simulateDelay(300);
                    return {
                        key,
                        value: `{"message": "Hello from Neo Service Layer!", "timestamp": "${new Date().toISOString()}"}`,
                        metadata: {
                            created: new Date().toISOString(),
                            size: 128
                        }
                    };
                },
                
                delete: async (key) => {
                    await this.simulateDelay(200);
                    return {
                        key,
                        success: true,
                        transactionId: 'tx_' + Math.random().toString(36).substr(2, 9)
                    };
                }
            },
            
            oracle: {
                requestData: async (params) => {
                    await this.simulateDelay(800);
                    return {
                        requestId: 'req_' + Math.random().toString(36).substr(2, 9),
                        data: {
                            price: (Math.random() * 100).toFixed(2),
                            timestamp: new Date().toISOString(),
                            source: 'mock_oracle'
                        },
                        confidence: 0.95
                    };
                },
                
                subscribe: async (feed) => {
                    console.log(`Subscribed to ${feed} - mock data will be streamed`);
                    return {
                        subscriptionId: 'sub_' + Math.random().toString(36).substr(2, 9),
                        feed,
                        active: true
                    };
                }
            },
            
            identity: {
                createDID: async () => {
                    await this.simulateDelay(1000);
                    return {
                        did: 'did:neo:' + Math.random().toString(36).substr(2, 16),
                        publicKey: '03' + Array(64).fill(0).map(() => Math.floor(Math.random() * 16).toString(16)).join(''),
                        created: new Date().toISOString()
                    };
                },
                
                verifyCredential: async (credential) => {
                    await this.simulateDelay(600);
                    return {
                        valid: true,
                        issuer: 'did:neo:mock_issuer',
                        verificationTime: new Date().toISOString()
                    };
                }
            }
        };
    }
    
    async simulateDelay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}

// Initialize playground when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.codePlayground = new CodePlayground();
});

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CodePlayground;
}
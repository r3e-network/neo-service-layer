/**
 * Neo Service Layer Documentation Search
 * Advanced search functionality with fuzzy matching, categorization, and keyboard navigation
 */

class DocsSearch {
    constructor() {
        this.searchIndex = [];
        this.isInitialized = false;
        this.searchInput = null;
        this.searchResults = null;
        this.currentFocus = -1;
        this.searchTimeout = null;
        this.isSearchOpen = false;
        
        // Search configuration
        this.config = {
            threshold: 0.6,
            minQueryLength: 2,
            maxResults: 10,
            debounceDelay: 300
        };
        
        this.init();
    }
    
    async init() {
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.setupSearch());
        } else {
            this.setupSearch();
        }
    }
    
    async setupSearch() {
        this.searchInput = document.getElementById('docs-search');
        if (!this.searchInput) return;
        
        // Create search results container
        this.createSearchUI();
        
        // Load search index
        await this.loadSearchIndex();
        
        // Setup event listeners
        this.setupEventListeners();
        
        this.isInitialized = true;
    }
    
    createSearchUI() {
        // Create search results container
        const searchContainer = this.searchInput.closest('.docs-search-container');
        
        this.searchResults = document.createElement('div');
        this.searchResults.className = 'docs-search-results';
        this.searchResults.innerHTML = `
            <div class="search-results-header">
                <span class="search-results-count"></span>
                <button class="search-results-close" aria-label="Close search">
                    <i class="ti ti-x"></i>
                </button>
            </div>
            <div class="search-results-content">
                <div class="search-results-list"></div>
                <div class="search-results-footer">
                    <div class="search-shortcuts">
                        <kbd>↑</kbd><kbd>↓</kbd> Navigate
                        <kbd>↵</kbd> Select
                        <kbd>Esc</kbd> Close
                    </div>
                </div>
            </div>
        `;
        
        searchContainer.appendChild(this.searchResults);
        
        // Create search overlay
        this.searchOverlay = document.createElement('div');
        this.searchOverlay.className = 'docs-search-overlay';
        document.body.appendChild(this.searchOverlay);
    }
    
    async loadSearchIndex() {
        try {
            // Load search index from JSON file
            const response = await fetch('../src/data/search-index.json');
            const data = await response.json();
            this.searchIndex = data.searchIndex;
        } catch (error) {
            console.warn('Failed to load search index, using fallback:', error);
            // Fallback to static index
            this.searchIndex = [
            // Getting Started
            {
                title: 'Introduction',
                content: 'Neo Service Layer is an enterprise-grade blockchain infrastructure platform',
                url: '/docs/index-professional.html#introduction',
                category: 'Getting Started',
                tags: ['introduction', 'overview', 'enterprise', 'blockchain'],
                type: 'page'
            },
            {
                title: 'Quick Start',
                content: 'Get started with Neo Service Layer in under 5 minutes',
                url: '/docs/index-professional.html#quick-start',
                category: 'Getting Started',
                tags: ['quickstart', 'setup', 'tutorial'],
                type: 'section'
            },
            
            // JavaScript SDK
            {
                title: 'JavaScript SDK Installation',
                content: 'Install the Neo Service Layer JavaScript SDK using npm, yarn, or pnpm',
                url: '/docs/javascript-sdk-professional.html#installation',
                category: 'SDK',
                tags: ['javascript', 'sdk', 'installation', 'npm', 'yarn'],
                type: 'section'
            },
            {
                title: 'SDK Configuration',
                content: 'Configure the SDK with network settings, authentication, and advanced options',
                url: '/docs/javascript-sdk-professional.html#configuration',
                category: 'SDK',
                tags: ['configuration', 'setup', 'network', 'authentication'],
                type: 'section'
            },
            {
                title: 'Storage Service',
                content: 'Store and retrieve data with encryption and access control',
                url: '/docs/javascript-sdk-professional.html#storage-service',
                category: 'SDK',
                tags: ['storage', 'data', 'encryption', 'access control'],
                type: 'service'
            },
            
            // API Reference
            {
                title: 'API Authentication',
                content: 'Authenticate API requests using JWT tokens or API keys',
                url: '/docs/api-reference-professional.html#authentication',
                category: 'API',
                tags: ['authentication', 'jwt', 'api key', 'security'],
                type: 'section'
            },
            {
                title: 'POST /storage/store',
                content: 'Store data with optional encryption and access control',
                url: '/docs/api-reference-professional.html#post-storage-store',
                category: 'API',
                tags: ['storage', 'post', 'endpoint', 'encryption'],
                type: 'endpoint'
            },
            {
                title: 'Rate Limiting',
                content: 'API requests are rate limited to ensure fair usage',
                url: '/docs/api-reference-professional.html#rate-limiting',
                category: 'API',
                tags: ['rate limiting', 'limits', 'throttling'],
                type: 'section'
            },
            
            // Deployment
            {
                title: 'Docker Quick Start',
                content: 'Get Neo Service Layer running locally with Docker Compose',
                url: '/docs/deployment-guide-professional.html#docker-quickstart',
                category: 'Deployment',
                tags: ['docker', 'quickstart', 'local', 'development'],
                type: 'section'
            },
            {
                title: 'Kubernetes Deployment',
                content: 'Deploy to Kubernetes for production workloads with high availability',
                url: '/docs/deployment-guide-professional.html#kubernetes-deployment',
                category: 'Deployment',
                tags: ['kubernetes', 'production', 'helm', 'scaling'],
                type: 'section'
            },
            {
                title: 'Intel SGX Setup',
                content: 'Configure Intel SGX for hardware-based security',
                url: '/docs/deployment-guide-professional.html#intel-sgx-setup',
                category: 'Deployment',
                tags: ['intel sgx', 'security', 'hardware', 'enclave'],
                type: 'section'
            },
            
            // Error Codes
            {
                title: 'SDK Error Codes',
                content: 'Complete list of SDK error codes and troubleshooting',
                url: '/docs/error-codes.html',
                category: 'Reference',
                tags: ['errors', 'troubleshooting', 'debugging'],
                type: 'page'
            },
            {
                title: 'Migration Guide',
                content: 'Step-by-step guide for upgrading from SDK v1 to v2',
                url: '/docs/migration-guide.html',
                category: 'Reference',
                tags: ['migration', 'upgrade', 'v2', 'breaking changes'],
                type: 'page'
            }
        ];
        }
        
        // Preprocess for search
        this.preprocessIndex();
    }
    
    preprocessIndex() {
        this.searchIndex = this.searchIndex.map(item => ({
            ...item,
            searchText: `${item.title} ${item.content} ${item.tags.join(' ')}`.toLowerCase(),
            words: this.tokenize(`${item.title} ${item.content} ${item.tags.join(' ')}`)
        }));
    }
    
    tokenize(text) {
        return text.toLowerCase()
            .replace(/[^\w\s]/g, ' ')
            .split(/\s+/)
            .filter(word => word.length > 1);
    }
    
    setupEventListeners() {
        // Search input events
        this.searchInput.addEventListener('input', (e) => {
            this.handleSearchInput(e.target.value);
        });
        
        this.searchInput.addEventListener('focus', () => {
            if (this.searchInput.value.trim()) {
                this.showSearchResults();
            }
        });
        
        this.searchInput.addEventListener('keydown', (e) => {
            this.handleKeyNavigation(e);
        });
        
        // Close search results
        const closeButton = this.searchResults.querySelector('.search-results-close');
        closeButton.addEventListener('click', () => {
            this.hideSearchResults();
        });
        
        this.searchOverlay.addEventListener('click', () => {
            this.hideSearchResults();
        });
        
        // Global keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            // Cmd/Ctrl + K to focus search
            if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
                e.preventDefault();
                this.focusSearch();
            }
            
            // Escape to close search
            if (e.key === 'Escape' && this.isSearchOpen) {
                this.hideSearchResults();
            }
        });
    }
    
    handleSearchInput(query) {
        // Clear existing timeout
        if (this.searchTimeout) {
            clearTimeout(this.searchTimeout);
        }
        
        // Debounce search
        this.searchTimeout = setTimeout(() => {
            this.performSearch(query);
        }, this.config.debounceDelay);
    }
    
    performSearch(query) {
        const trimmedQuery = query.trim();
        
        if (trimmedQuery.length < this.config.minQueryLength) {
            this.hideSearchResults();
            return;
        }
        
        const results = this.search(trimmedQuery);
        this.displayResults(results, trimmedQuery);
        this.showSearchResults();
    }
    
    search(query) {
        const queryWords = this.tokenize(query);
        const results = [];
        
        for (const item of this.searchIndex) {
            const score = this.calculateScore(item, query, queryWords);
            if (score > this.config.threshold) {
                results.push({
                    ...item,
                    score,
                    highlights: this.getHighlights(item, query)
                });
            }
        }
        
        // Sort by score (highest first) and limit results
        return results
            .sort((a, b) => b.score - a.score)
            .slice(0, this.config.maxResults);
    }
    
    calculateScore(item, query, queryWords) {
        let score = 0;
        const titleWords = this.tokenize(item.title);
        const contentWords = this.tokenize(item.content);
        
        // Exact title match gets highest score
        if (item.title.toLowerCase().includes(query.toLowerCase())) {
            score += 100;
        }
        
        // Title word matches
        for (const queryWord of queryWords) {
            for (const titleWord of titleWords) {
                if (titleWord.includes(queryWord)) {
                    score += 50;
                }
                if (titleWord === queryWord) {
                    score += 75;
                }
            }
        }
        
        // Content matches
        for (const queryWord of queryWords) {
            if (item.searchText.includes(queryWord)) {
                score += 25;
            }
        }
        
        // Tag matches
        for (const tag of item.tags) {
            if (tag.toLowerCase().includes(query.toLowerCase())) {
                score += 40;
            }
        }
        
        // Category boost
        if (item.category.toLowerCase().includes(query.toLowerCase())) {
            score += 30;
        }
        
        // Type boost for exact matches
        if (item.type === query.toLowerCase()) {
            score += 20;
        }
        
        return Math.min(score, 300); // Cap the score
    }
    
    getHighlights(item, query) {
        const regex = new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
        
        return {
            title: item.title.replace(regex, '<mark>$1</mark>'),
            content: item.content.replace(regex, '<mark>$1</mark>')
        };
    }
    
    displayResults(results, query) {
        const resultsList = this.searchResults.querySelector('.search-results-list');
        const resultsCount = this.searchResults.querySelector('.search-results-count');
        
        // Update count
        resultsCount.textContent = `${results.length} result${results.length !== 1 ? 's' : ''} for "${query}"`;
        
        if (results.length === 0) {
            resultsList.innerHTML = `
                <div class="search-no-results">
                    <div class="search-no-results-icon">
                        <i class="ti ti-search-off"></i>
                    </div>
                    <h4>No results found</h4>
                    <p>Try adjusting your search terms or browse the documentation categories.</p>
                </div>
            `;
            return;
        }
        
        // Group results by category
        const groupedResults = this.groupResultsByCategory(results);
        
        let html = '';
        for (const [category, categoryResults] of Object.entries(groupedResults)) {
            html += `
                <div class="search-category">
                    <h4 class="search-category-title">${category}</h4>
                    <div class="search-category-results">
            `;
            
            for (const result of categoryResults) {
                html += this.renderSearchResult(result);
            }
            
            html += `
                    </div>
                </div>
            `;
        }
        
        resultsList.innerHTML = html;
        this.currentFocus = -1;
    }
    
    groupResultsByCategory(results) {
        const grouped = {};
        
        for (const result of results) {
            if (!grouped[result.category]) {
                grouped[result.category] = [];
            }
            grouped[result.category].push(result);
        }
        
        return grouped;
    }
    
    renderSearchResult(result) {
        const typeIcon = this.getTypeIcon(result.type);
        
        return `
            <a href="${result.url}" class="search-result" data-url="${result.url}">
                <div class="search-result-icon">
                    <i class="${typeIcon}"></i>
                </div>
                <div class="search-result-content">
                    <h5 class="search-result-title">${result.highlights.title}</h5>
                    <p class="search-result-description">${result.highlights.content}</p>
                    <div class="search-result-meta">
                        <span class="search-result-type">${result.type}</span>
                        <span class="search-result-category">${result.category}</span>
                    </div>
                </div>
                <div class="search-result-arrow">
                    <i class="ti ti-arrow-right"></i>
                </div>
            </a>
        `;
    }
    
    getTypeIcon(type) {
        const icons = {
            'page': 'ti ti-file-text',
            'section': 'ti ti-hash',
            'endpoint': 'ti ti-api',
            'service': 'ti ti-server',
            'method': 'ti ti-function'
        };
        
        return icons[type] || 'ti ti-file';
    }
    
    handleKeyNavigation(e) {
        if (!this.isSearchOpen) return;
        
        const results = this.searchResults.querySelectorAll('.search-result');
        
        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                this.currentFocus = Math.min(this.currentFocus + 1, results.length - 1);
                this.updateFocus(results);
                break;
                
            case 'ArrowUp':
                e.preventDefault();
                this.currentFocus = Math.max(this.currentFocus - 1, -1);
                this.updateFocus(results);
                break;
                
            case 'Enter':
                e.preventDefault();
                if (this.currentFocus >= 0 && results[this.currentFocus]) {
                    window.location.href = results[this.currentFocus].href;
                }
                break;
                
            case 'Escape':
                this.hideSearchResults();
                break;
        }
    }
    
    updateFocus(results) {
        // Remove previous focus
        results.forEach(result => result.classList.remove('focused'));
        
        // Add focus to current item
        if (this.currentFocus >= 0 && results[this.currentFocus]) {
            results[this.currentFocus].classList.add('focused');
            results[this.currentFocus].scrollIntoView({
                block: 'nearest'
            });
        }
    }
    
    showSearchResults() {
        this.searchResults.classList.add('active');
        this.searchOverlay.classList.add('active');
        this.isSearchOpen = true;
        document.body.classList.add('search-open');
    }
    
    hideSearchResults() {
        this.searchResults.classList.remove('active');
        this.searchOverlay.classList.remove('active');
        this.isSearchOpen = false;
        document.body.classList.remove('search-open');
        this.currentFocus = -1;
    }
    
    focusSearch() {
        this.searchInput.focus();
        this.searchInput.select();
    }
}

// Initialize search when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.docsSearch = new DocsSearch();
});

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DocsSearch;
}
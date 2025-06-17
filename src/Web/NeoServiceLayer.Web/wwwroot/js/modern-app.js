// Neo Service Layer - Modern Web App JavaScript

class NeoServiceApp {
    constructor() {
        this.initializeApp();
        this.setupEventListeners();
        this.setupScrollAnimations();
        this.setupTheme();
    }

    initializeApp() {
        console.log('🚀 Neo Service Layer Web App Initialized');
        this.showLoadingComplete();
    }

    showLoadingComplete() {
        setTimeout(() => {
            document.body.classList.add('loaded');
        }, 500);
    }

    setupEventListeners() {
        // Mobile menu toggle
        const mobileToggle = document.querySelector('.mobile-toggle');
        const navMenu = document.querySelector('.nav-menu');
        
        if (mobileToggle && navMenu) {
            mobileToggle.addEventListener('click', () => {
                navMenu.classList.toggle('active');
                mobileToggle.classList.toggle('active');
            });
        }

        // Smooth scrolling for anchor links
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', (e) => {
                e.preventDefault();
                const target = document.querySelector(anchor.getAttribute('href'));
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });

        // Copy to clipboard functionality
        document.querySelectorAll('.copy-button').forEach(button => {
            button.addEventListener('click', async () => {
                const text = button.dataset.copy;
                try {
                    await navigator.clipboard.writeText(text);
                    this.showToast('Copied to clipboard!', 'success');
                } catch (err) {
                    this.showToast('Failed to copy', 'error');
                }
            });
        });

        // Service card interactions
        document.querySelectorAll('.service-card-modern').forEach(card => {
            card.addEventListener('mouseenter', () => {
                this.animateServiceCard(card, 'enter');
            });
            
            card.addEventListener('mouseleave', () => {
                this.animateServiceCard(card, 'leave');
            });
        });
    }

    setupScrollAnimations() {
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('revealed');
                }
            });
        }, observerOptions);

        // Observe all elements with scroll-reveal class
        document.querySelectorAll('.scroll-reveal').forEach(el => {
            observer.observe(el);
        });

        // Parallax effect for hero section
        window.addEventListener('scroll', () => {
            const scrolled = window.pageYOffset;
            const parallax = document.querySelector('.hero-parallax');
            if (parallax) {
                parallax.style.transform = `translateY(${scrolled * 0.5}px)`;
            }
        });
    }

    setupTheme() {
        // Theme switcher
        const themeToggle = document.querySelector('.theme-toggle');
        const currentTheme = localStorage.getItem('neo-theme') || 'light';
        
        document.documentElement.setAttribute('data-theme', currentTheme);
        
        if (themeToggle) {
            themeToggle.addEventListener('click', () => {
                const currentTheme = document.documentElement.getAttribute('data-theme');
                const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
                
                document.documentElement.setAttribute('data-theme', newTheme);
                localStorage.setItem('neo-theme', newTheme);
                
                this.showToast(`Switched to ${newTheme} theme`, 'info');
            });
        }
    }

    animateServiceCard(card, direction) {
        const icon = card.querySelector('.service-icon');
        const title = card.querySelector('.card-title');
        
        if (direction === 'enter') {
            if (icon) {
                icon.style.transform = 'scale(1.1) rotate(5deg)';
            }
            if (title) {
                title.style.color = 'var(--neo-primary)';
            }
        } else {
            if (icon) {
                icon.style.transform = 'scale(1) rotate(0deg)';
            }
            if (title) {
                title.style.color = '';
            }
        }
    }

    showToast(message, type = 'info') {
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.textContent = message;
        
        toast.style.cssText = `
            position: fixed;
            top: 2rem;
            right: 2rem;
            background: var(--gradient-primary);
            color: white;
            padding: 1rem 1.5rem;
            border-radius: var(--radius-md);
            box-shadow: var(--shadow-lg);
            z-index: 10000;
            transform: translateX(100%);
            transition: transform 0.3s ease-out;
            font-weight: 500;
        `;
        
        document.body.appendChild(toast);
        
        // Animate in
        setTimeout(() => {
            toast.style.transform = 'translateX(0)';
        }, 100);
        
        // Animate out and remove
        setTimeout(() => {
            toast.style.transform = 'translateX(100%)';
            setTimeout(() => {
                document.body.removeChild(toast);
            }, 300);
        }, 3000);
    }

    // Service API interactions
    async callService(serviceName, endpoint, data = null) {
        const loadingElement = document.querySelector(`#${serviceName}-loading`);
        const resultElement = document.querySelector(`#${serviceName}-result`);
        
        if (loadingElement) loadingElement.style.display = 'block';
        if (resultElement) resultElement.innerHTML = '';
        
        try {
            const options = {
                method: data ? 'POST' : 'GET',
                headers: {
                    'Content-Type': 'application/json',
                },
            };
            
            if (data) {
                options.body = JSON.stringify(data);
            }
            
            const response = await fetch(endpoint, options);
            const result = await response.json();
            
            if (resultElement) {
                resultElement.innerHTML = `
                    <div class="code-block">
                        <pre><code>${JSON.stringify(result, null, 2)}</code></pre>
                    </div>
                `;
            }
            
            this.showToast('Service call completed', 'success');
            
        } catch (error) {
            console.error('Service call error:', error);
            
            if (resultElement) {
                resultElement.innerHTML = `
                    <div class="alert alert-danger">
                        <strong>Error:</strong> ${error.message}
                    </div>
                `;
            }
            
            this.showToast('Service call failed', 'error');
            
        } finally {
            if (loadingElement) loadingElement.style.display = 'none';
        }
    }

    // Utility functions
    formatBytes(bytes, decimals = 2) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const dm = decimals < 0 ? 0 : decimals;
        const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
    }

    formatNumber(num) {
        return new Intl.NumberFormat().format(num);
    }

    generateUUID() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
            const r = Math.random() * 16 | 0;
            const v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    // Real-time system monitoring
    startSystemMonitoring() {
        this.updateSystemInfo();
        setInterval(() => {
            this.updateSystemInfo();
        }, 5000);
    }

    async updateSystemInfo() {
        try {
            const response = await fetch('/api/info');
            const info = await response.json();
            
            const systemInfoElement = document.querySelector('#system-info');
            if (systemInfoElement) {
                systemInfoElement.innerHTML = `
                    <div class="row g-3">
                        <div class="col-md-3 text-center">
                            <i class="fas fa-server text-primary"></i>
                            <h6>Environment</h6>
                            <span class="badge bg-primary">${info.Environment}</span>
                        </div>
                        <div class="col-md-3 text-center">
                            <i class="fas fa-code-branch text-success"></i>
                            <h6>Version</h6>
                            <span class="badge bg-success">${info.Version}</span>
                        </div>
                        <div class="col-md-3 text-center">
                            <i class="fas fa-cogs text-info"></i>
                            <h6>Services</h6>
                            <span class="badge bg-info">${info.Features.length}</span>
                        </div>
                        <div class="col-md-3 text-center">
                            <i class="fas fa-clock text-warning"></i>
                            <h6>Uptime</h6>
                            <span class="badge bg-warning">${this.getRelativeTime(info.Timestamp)}</span>
                        </div>
                    </div>
                `;
            }
        } catch (error) {
            console.error('Failed to update system info:', error);
        }
    }

    getRelativeTime(timestamp) {
        const now = new Date();
        const time = new Date(timestamp);
        const diffInSeconds = Math.floor((now - time) / 1000);
        
        if (diffInSeconds < 60) return `${diffInSeconds}s`;
        if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m`;
        if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h`;
        return `${Math.floor(diffInSeconds / 86400)}d`;
    }
}

// Initialize the app when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.neoApp = new NeoServiceApp();
    
    // Start system monitoring if on dashboard
    if (document.querySelector('#system-info')) {
        window.neoApp.startSystemMonitoring();
    }
});

// Export for global access
window.NeoServiceApp = NeoServiceApp;
// Main JavaScript for Neo Service Layer Website

// DOM Content Loaded
document.addEventListener('DOMContentLoaded', function() {
    initializeNavigation();
    initializeTabs();
    initializeCodeCopy();
    initializeScrollAnimations();
    initializeSmoothScrolling();
});

// Navigation functionality
function initializeNavigation() {
    const navToggle = document.querySelector('.nav-toggle');
    const navMenu = document.querySelector('.nav-menu');
    
    if (navToggle && navMenu) {
        navToggle.addEventListener('click', function() {
            navMenu.classList.toggle('active');
            
            // Animate hamburger menu
            const spans = navToggle.querySelectorAll('span');
            spans.forEach((span, index) => {
                if (navMenu.classList.contains('active')) {
                    if (index === 0) span.style.transform = 'rotate(45deg) translate(5px, 5px)';
                    if (index === 1) span.style.opacity = '0';
                    if (index === 2) span.style.transform = 'rotate(-45deg) translate(7px, -6px)';
                } else {
                    span.style.transform = '';
                    span.style.opacity = '';
                }
            });
        });
    }
    
    // Close mobile menu when clicking on links
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        link.addEventListener('click', () => {
            if (navMenu && navMenu.classList.contains('active')) {
                navMenu.classList.remove('active');
                const spans = navToggle.querySelectorAll('span');
                spans.forEach(span => {
                    span.style.transform = '';
                    span.style.opacity = '';
                });
            }
        });
    });
    
    // Navbar scroll effect
    window.addEventListener('scroll', function() {
        const navbar = document.querySelector('.navbar');
        if (navbar) {
            if (window.scrollY > 100) {
                navbar.style.background = 'rgba(10, 14, 39, 0.98)';
                navbar.style.boxShadow = '0 2px 20px rgba(0, 0, 0, 0.1)';
            } else {
                navbar.style.background = 'rgba(10, 14, 39, 0.95)';
                navbar.style.boxShadow = 'none';
            }
        }
    });
}

// Tab functionality for services and examples
function initializeTabs() {
    // Service tabs
    const serviceTabButtons = document.querySelectorAll('.tab-button');
    const serviceTabPanes = document.querySelectorAll('.tab-pane');
    
    serviceTabButtons.forEach(button => {
        button.addEventListener('click', function() {
            const targetTab = this.getAttribute('data-tab');
            
            // Remove active class from all buttons and panes
            serviceTabButtons.forEach(btn => btn.classList.remove('active'));
            serviceTabPanes.forEach(pane => pane.classList.remove('active'));
            
            // Add active class to clicked button and corresponding pane
            this.classList.add('active');
            const targetPane = document.getElementById(targetTab);
            if (targetPane) {
                targetPane.classList.add('active');
            }
        });
    });
    
    // Example tabs
    const exampleTabButtons = document.querySelectorAll('.example-tab');
    const exampleTabPanes = document.querySelectorAll('.example-pane');
    
    exampleTabButtons.forEach(button => {
        button.addEventListener('click', function() {
            const targetExample = this.getAttribute('data-example');
            
            // Remove active class from all buttons and panes
            exampleTabButtons.forEach(btn => btn.classList.remove('active'));
            exampleTabPanes.forEach(pane => pane.classList.remove('active'));
            
            // Add active class to clicked button and corresponding pane
            this.classList.add('active');
            const targetPane = document.getElementById(targetExample + '-example');
            if (targetPane) {
                targetPane.classList.add('active');
            }
        });
    });
}

// Code copy functionality
function initializeCodeCopy() {
    const copyButtons = document.querySelectorAll('.copy-btn');
    
    copyButtons.forEach(button => {
        button.addEventListener('click', function() {
            const codeBlock = this.closest('.code-block');
            const code = codeBlock.querySelector('code');
            
            if (code) {
                const text = code.textContent;
                
                // Copy to clipboard
                navigator.clipboard.writeText(text).then(() => {
                    // Show success feedback
                    const originalText = this.innerHTML;
                    this.innerHTML = '<i class="fas fa-check"></i> Copied!';
                    this.style.color = 'var(--primary-color)';
                    
                    setTimeout(() => {
                        this.innerHTML = originalText;
                        this.style.color = '';
                    }, 2000);
                }).catch(err => {
                    // Failed to copy text - silently handle error
                    
                    // Fallback for older browsers
                    const textArea = document.createElement('textarea');
                    textArea.value = text;
                    document.body.appendChild(textArea);
                    textArea.select();
                    document.execCommand('copy');
                    document.body.removeChild(textArea);
                    
                    // Show success feedback
                    const originalText = this.innerHTML;
                    this.innerHTML = '<i class="fas fa-check"></i> Copied!';
                    this.style.color = 'var(--primary-color)';
                    
                    setTimeout(() => {
                        this.innerHTML = originalText;
                        this.style.color = '';
                    }, 2000);
                });
            }
        });
    });
}

// Scroll animations
function initializeScrollAnimations() {
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };
    
    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('revealed');
            }
        });
    }, observerOptions);
    
    // Observe all elements with scroll-reveal class
    const revealElements = document.querySelectorAll('.scroll-reveal');
    revealElements.forEach(element => {
        observer.observe(element);
    });
    
    // Add scroll-reveal class to animated elements
    const animatedElements = document.querySelectorAll('.feature-card, .service-card, .doc-card, .step');
    animatedElements.forEach(element => {
        element.classList.add('scroll-reveal');
        observer.observe(element);
    });
}

// Smooth scrolling for anchor links
function initializeSmoothScrolling() {
    const links = document.querySelectorAll('a[href^="#"]');
    
    links.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            const targetId = this.getAttribute('href');
            const targetElement = document.querySelector(targetId);
            
            if (targetElement) {
                const offsetTop = targetElement.offsetTop - 80; // Account for fixed navbar
                
                window.scrollTo({
                    top: offsetTop,
                    behavior: 'smooth'
                });
            }
        });
    });
}

// Utility functions
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function throttle(func, limit) {
    let inThrottle;
    return function() {
        const args = arguments;
        const context = this;
        if (!inThrottle) {
            func.apply(context, args);
            inThrottle = true;
            setTimeout(() => inThrottle = false, limit);
        }
    }
}

// Loading state management
function showLoading(element) {
    if (element) {
        element.classList.add('loading');
    }
}

function hideLoading(element) {
    if (element) {
        element.classList.remove('loading');
    }
}

// Form validation (for future contact forms)
function validateEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

function validateForm(formData) {
    const errors = [];
    
    if (!formData.name || formData.name.trim().length < 2) {
        errors.push('Name must be at least 2 characters long');
    }
    
    if (!formData.email || !validateEmail(formData.email)) {
        errors.push('Please enter a valid email address');
    }
    
    if (!formData.message || formData.message.trim().length < 10) {
        errors.push('Message must be at least 10 characters long');
    }
    
    return errors;
}

// Analytics tracking (placeholder for future implementation)
function trackEvent(eventName, properties = {}) {
    // Placeholder for analytics tracking
    // Event tracking will be implemented with proper analytics service
}

// Performance monitoring
function measurePerformance() {
    if ('performance' in window) {
        window.addEventListener('load', function() {
            setTimeout(function() {
                const perfData = performance.getEntriesByType('navigation')[0];
                const loadTime = perfData.loadEventEnd - perfData.loadEventStart;
                
                // Page load time measured for potential performance tracking
                
                // Track performance metrics
                trackEvent('page_load_time', {
                    load_time: loadTime,
                    dom_content_loaded: perfData.domContentLoadedEventEnd - perfData.domContentLoadedEventStart
                });
            }, 0);
        });
    }
}

// Initialize performance monitoring
measurePerformance();

// Export functions for potential module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        initializeNavigation,
        initializeTabs,
        initializeCodeCopy,
        initializeScrollAnimations,
        initializeSmoothScrolling,
        validateEmail,
        validateForm,
        trackEvent,
        debounce,
        throttle
    };
}
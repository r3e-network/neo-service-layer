// Performance Optimization Script

// Lazy load images
function lazyLoadImages() {
    const images = document.querySelectorAll('img[data-src]');
    const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.removeAttribute('data-src');
                observer.unobserve(img);
            }
        });
    }, {
        rootMargin: '50px 0px',
        threshold: 0.01
    });

    images.forEach(img => imageObserver.observe(img));
}

// Preload critical resources
function preloadCriticalResources() {
    const criticalResources = [
        { href: '/assets/fonts/inter-var.woff2', as: 'font', type: 'font/woff2' },
        { href: '/assets/css/variables.css', as: 'style' },
        { href: '/assets/images/hero-bg.jpg', as: 'image' }
    ];

    criticalResources.forEach(resource => {
        const link = document.createElement('link');
        link.rel = 'preload';
        link.href = resource.href;
        link.as = resource.as;
        if (resource.type) link.type = resource.type;
        if (resource.as === 'font') link.crossOrigin = 'anonymous';
        document.head.appendChild(link);
    });
}

// Optimize scroll performance
function optimizeScroll() {
    let ticking = false;
    
    function updateScrollProgress() {
        const scrollProgress = document.querySelector('.scroll-progress');
        if (scrollProgress) {
            const windowHeight = window.innerHeight;
            const documentHeight = document.documentElement.scrollHeight - windowHeight;
            const scrolled = window.scrollY;
            const progress = (scrolled / documentHeight) * 100;
            scrollProgress.style.width = progress + '%';
        }
        ticking = false;
    }

    window.addEventListener('scroll', () => {
        if (!ticking) {
            requestAnimationFrame(updateScrollProgress);
            ticking = true;
        }
    });
}

// Debounce function for resize events
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

// Resource hints for faster navigation
function addResourceHints() {
    const links = document.querySelectorAll('a[href^="http"]');
    const domains = new Set();

    links.forEach(link => {
        const url = new URL(link.href);
        domains.add(url.origin);
    });

    domains.forEach(domain => {
        const link = document.createElement('link');
        link.rel = 'dns-prefetch';
        link.href = domain;
        document.head.appendChild(link);
    });
}

// Optimize font loading
function optimizeFonts() {
    if ('fonts' in document) {
        Promise.all([
            document.fonts.load('400 1em Inter'),
            document.fonts.load('700 1em Inter')
        ]).then(() => {
            document.documentElement.classList.add('fonts-loaded');
        });
    }
}

// Monitor performance metrics
function monitorPerformance() {
    if ('PerformanceObserver' in window) {
        // Monitor Largest Contentful Paint
        const lcpObserver = new PerformanceObserver(list => {
            const entries = list.getEntries();
            const lastEntry = entries[entries.length - 1];
            console.log('LCP:', lastEntry.renderTime || lastEntry.loadTime);
        });
        lcpObserver.observe({ entryTypes: ['largest-contentful-paint'] });

        // Monitor First Input Delay
        const fidObserver = new PerformanceObserver(list => {
            const entries = list.getEntries();
            entries.forEach(entry => {
                console.log('FID:', entry.processingStart - entry.startTime);
            });
        });
        fidObserver.observe({ entryTypes: ['first-input'] });

        // Monitor Cumulative Layout Shift
        const clsObserver = new PerformanceObserver(list => {
            let clsScore = 0;
            list.getEntries().forEach(entry => {
                if (!entry.hadRecentInput) {
                    clsScore += entry.value;
                }
            });
            console.log('CLS:', clsScore);
        });
        clsObserver.observe({ entryTypes: ['layout-shift'] });
    }
}

// Memory optimization
function optimizeMemory() {
    // Clean up event listeners on hidden elements
    const hiddenElements = document.querySelectorAll('[style*="display: none"]');
    hiddenElements.forEach(element => {
        // Remove non-essential event listeners
        const clone = element.cloneNode(true);
        element.parentNode.replaceChild(clone, element);
    });

    // Clear unused variables
    if (window.performance && window.performance.memory) {
        const memoryUsage = window.performance.memory;
        console.log('Memory usage:', {
            used: (memoryUsage.usedJSHeapSize / 1048576).toFixed(2) + ' MB',
            total: (memoryUsage.totalJSHeapSize / 1048576).toFixed(2) + ' MB',
            limit: (memoryUsage.jsHeapSizeLimit / 1048576).toFixed(2) + ' MB'
        });
    }
}

// Initialize performance optimizations
document.addEventListener('DOMContentLoaded', () => {
    lazyLoadImages();
    preloadCriticalResources();
    optimizeScroll();
    addResourceHints();
    optimizeFonts();
    
    // Monitor performance in development
    if (window.location.hostname === 'localhost') {
        monitorPerformance();
    }
    
    // Optimize memory every 30 seconds
    setInterval(optimizeMemory, 30000);
});

// Export functions
window.PerformanceOptimizer = {
    lazyLoadImages,
    preloadCriticalResources,
    optimizeScroll,
    debounce,
    addResourceHints,
    optimizeFonts,
    monitorPerformance,
    optimizeMemory
};
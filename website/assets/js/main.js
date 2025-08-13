// Main JavaScript for Neo Service Layer

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initPreloader();
    initNavigation();
    initThemeToggle();
    initScrollEffects();
    initCounters();
    initServiceCarousel();
    initDocSearch();
    initTooltips();
    initModals();
    initTabs();
    initAccordions();
    initFormValidation();
    initCopyCode();
    initBackToTop();
    initBlogFilters();
});

// Preloader
function initPreloader() {
    const preloader = document.querySelector('.preloader');
    if (preloader) {
        window.addEventListener('load', () => {
            setTimeout(() => {
                preloader.classList.add('fade-out');
                setTimeout(() => {
                    preloader.style.display = 'none';
                }, 500);
            }, 500);
        });
    }
}

// Navigation
function initNavigation() {
    const navbar = document.querySelector('.navbar');
    const navToggle = document.querySelector('.navbar-toggle');
    const navMenu = document.querySelector('.navbar-menu');
    const navLinks = document.querySelectorAll('.navbar-link');
    
    // Scroll effect
    if (navbar) {
        let lastScroll = 0;
        window.addEventListener('scroll', () => {
            const currentScroll = window.pageYOffset;
            
            if (currentScroll > 50) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }
            
            // Hide/show on scroll
            if (currentScroll > lastScroll && currentScroll > 100) {
                navbar.style.transform = 'translateY(-100%)';
            } else {
                navbar.style.transform = 'translateY(0)';
            }
            
            lastScroll = currentScroll;
        });
    }
    
    // Mobile menu toggle
    if (navToggle && navMenu) {
        navToggle.addEventListener('click', () => {
            navToggle.classList.toggle('active');
            navMenu.classList.toggle('active');
            document.body.classList.toggle('menu-open');
        });
        
        // Close menu on link click
        navLinks.forEach(link => {
            link.addEventListener('click', () => {
                navToggle.classList.remove('active');
                navMenu.classList.remove('active');
                document.body.classList.remove('menu-open');
            });
        });
    }
    
    // Smooth scrolling
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                const offset = navbar ? navbar.offsetHeight : 0;
                const targetPosition = target.getBoundingClientRect().top + window.pageYOffset - offset;
                
                window.scrollTo({
                    top: targetPosition,
                    behavior: 'smooth'
                });
            }
        });
    });
}

// Theme Toggle
function initThemeToggle() {
    const themeToggle = document.querySelector('.theme-toggle');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)');
    
    // Get stored theme or default to system preference
    const currentTheme = localStorage.getItem('theme') || 
                        (prefersDark.matches ? 'dark' : 'light');
    
    // Apply theme
    document.documentElement.setAttribute('data-theme', currentTheme);
    updateThemeIcon(currentTheme);
    
    if (themeToggle) {
        themeToggle.addEventListener('click', () => {
            const theme = document.documentElement.getAttribute('data-theme');
            const newTheme = theme === 'light' ? 'dark' : 'light';
            
            document.documentElement.setAttribute('data-theme', newTheme);
            localStorage.setItem('theme', newTheme);
            updateThemeIcon(newTheme);
            
            // Animate theme change
            document.documentElement.style.transition = 'all 0.3s ease';
            setTimeout(() => {
                document.documentElement.style.transition = '';
            }, 300);
        });
    }
    
    // Listen for system theme changes
    prefersDark.addEventListener('change', (e) => {
        if (!localStorage.getItem('theme')) {
            const theme = e.matches ? 'dark' : 'light';
            document.documentElement.setAttribute('data-theme', theme);
            updateThemeIcon(theme);
        }
    });
}

function updateThemeIcon(theme) {
    const themeToggle = document.querySelector('.theme-toggle');
    if (themeToggle) {
        const icon = themeToggle.querySelector('i');
        if (icon) {
            icon.className = theme === 'light' ? 'fas fa-moon' : 'fas fa-sun';
        }
    }
}

// Scroll Effects
function initScrollEffects() {
    // Parallax elements
    const parallaxElements = document.querySelectorAll('.parallax');
    
    if (parallaxElements.length > 0) {
        window.addEventListener('scroll', () => {
            const scrolled = window.pageYOffset;
            
            parallaxElements.forEach(element => {
                const speed = element.dataset.speed || 0.5;
                const offset = element.dataset.offset || 0;
                const yPos = -(scrolled * speed) + Number(offset);
                
                element.style.transform = `translateY(${yPos}px)`;
            });
        });
    }
    
    // Reveal on scroll
    const revealElements = document.querySelectorAll('[data-reveal]');
    
    if (revealElements.length > 0) {
        const revealOnScroll = () => {
            revealElements.forEach(element => {
                const elementTop = element.getBoundingClientRect().top;
                const elementBottom = element.getBoundingClientRect().bottom;
                const elementHeight = element.offsetHeight;
                const windowHeight = window.innerHeight;
                const revealPoint = 100;
                
                if (elementTop < windowHeight - revealPoint && elementBottom > 0) {
                    element.classList.add('revealed');
                }
            });
        };
        
        window.addEventListener('scroll', revealOnScroll);
        revealOnScroll(); // Check on load
    }
}

// Counter Animation
function initCounters() {
    const counters = document.querySelectorAll('[data-counter]');
    
    if (counters.length > 0) {
        const animateCounter = (counter) => {
            const target = parseInt(counter.getAttribute('data-counter'));
            const duration = parseInt(counter.getAttribute('data-duration')) || 2000;
            const increment = target / (duration / 16);
            let current = 0;
            
            const updateCounter = () => {
                current += increment;
                if (current < target) {
                    counter.textContent = Math.floor(current).toLocaleString();
                    requestAnimationFrame(updateCounter);
                } else {
                    counter.textContent = target.toLocaleString();
                    
                    // Add suffix if exists
                    const suffix = counter.getAttribute('data-suffix');
                    if (suffix) {
                        counter.textContent += suffix;
                    }
                }
            };
            
            updateCounter();
        };
        
        // Intersection Observer for counters
        const counterObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting && !entry.target.classList.contains('counted')) {
                    entry.target.classList.add('counted');
                    animateCounter(entry.target);
                }
            });
        }, { threshold: 0.5 });
        
        counters.forEach(counter => {
            counterObserver.observe(counter);
        });
    }
}

// Service Carousel
function initServiceCarousel() {
    const carousel = document.querySelector('.services-carousel');
    const prevBtn = document.querySelector('.service-nav-btn.prev');
    const nextBtn = document.querySelector('.service-nav-btn.next');
    const dots = document.querySelectorAll('.service-dots .dot');
    
    if (carousel && prevBtn && nextBtn) {
        let currentIndex = 0;
        const cards = carousel.querySelectorAll('.service-card');
        const totalCards = cards.length;
        
        const updateCarousel = () => {
            cards.forEach((card, index) => {
                card.classList.toggle('active', index === currentIndex);
            });
            
            dots.forEach((dot, index) => {
                dot.classList.toggle('active', index === currentIndex);
            });
            
            // Update navigation buttons
            prevBtn.disabled = currentIndex === 0;
            nextBtn.disabled = currentIndex === totalCards - 1;
        };
        
        prevBtn.addEventListener('click', () => {
            if (currentIndex > 0) {
                currentIndex--;
                updateCarousel();
            }
        });
        
        nextBtn.addEventListener('click', () => {
            if (currentIndex < totalCards - 1) {
                currentIndex++;
                updateCarousel();
            }
        });
        
        dots.forEach((dot, index) => {
            dot.addEventListener('click', () => {
                currentIndex = index;
                updateCarousel();
            });
        });
        
        // Touch support
        let touchStartX = 0;
        let touchEndX = 0;
        
        carousel.addEventListener('touchstart', (e) => {
            touchStartX = e.changedTouches[0].screenX;
        });
        
        carousel.addEventListener('touchend', (e) => {
            touchEndX = e.changedTouches[0].screenX;
            handleSwipe();
        });
        
        const handleSwipe = () => {
            if (touchEndX < touchStartX - 50 && currentIndex < totalCards - 1) {
                currentIndex++;
                updateCarousel();
            }
            
            if (touchEndX > touchStartX + 50 && currentIndex > 0) {
                currentIndex--;
                updateCarousel();
            }
        };
        
        // Auto-play
        const autoPlay = carousel.getAttribute('data-autoplay') === 'true';
        if (autoPlay) {
            setInterval(() => {
                currentIndex = (currentIndex + 1) % totalCards;
                updateCarousel();
            }, 5000);
        }
        
        updateCarousel();
    }
}

// Documentation Search
function initDocSearch() {
    const searchInput = document.querySelector('.doc-search input');
    const searchResults = document.querySelector('.search-results');
    
    if (searchInput) {
        let searchTimeout;
        
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchTimeout);
            const query = e.target.value.trim();
            
            if (query.length > 2) {
                searchTimeout = setTimeout(() => {
                    performSearch(query);
                }, 300);
            } else if (searchResults) {
                searchResults.classList.remove('active');
            }
        });
        
        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                searchInput.focus();
            }
            
            if (e.key === 'Escape' && searchResults) {
                searchResults.classList.remove('active');
                searchInput.blur();
            }
        });
    }
}

function performSearch(query) {
    // Simulate search results
    const results = [
        { title: 'Getting Started', category: 'Guides', url: '#getting-started' },
        { title: 'API Reference', category: 'API', url: '#api-reference' },
        { title: 'SGX Integration', category: 'Guides', url: '#sgx-integration' },
        { title: 'Smart Contracts', category: 'Tutorials', url: '#smart-contracts' }
    ].filter(item => 
        item.title.toLowerCase().includes(query.toLowerCase()) ||
        item.category.toLowerCase().includes(query.toLowerCase())
    );
    
    displaySearchResults(results);
}

function displaySearchResults(results) {
    const searchResults = document.querySelector('.search-results');
    if (!searchResults) return;
    
    searchResults.innerHTML = results.length > 0 ? results.map(result => `
        <a href="${result.url}" class="search-result">
            <div class="result-category">${result.category}</div>
            <div class="result-title">${result.title}</div>
        </a>
    `).join('') : '<div class="no-results">No results found</div>';
    
    searchResults.classList.add('active');
}

// Tooltips
function initTooltips() {
    const tooltips = document.querySelectorAll('[data-tooltip]');
    
    tooltips.forEach(element => {
        element.addEventListener('mouseenter', (e) => {
            const tooltip = document.createElement('div');
            tooltip.className = 'tooltip-content';
            tooltip.textContent = element.getAttribute('data-tooltip');
            document.body.appendChild(tooltip);
            
            const rect = element.getBoundingClientRect();
            const tooltipRect = tooltip.getBoundingClientRect();
            
            tooltip.style.position = 'fixed';
            tooltip.style.top = `${rect.top - tooltipRect.height - 10}px`;
            tooltip.style.left = `${rect.left + (rect.width - tooltipRect.width) / 2}px`;
            tooltip.style.opacity = '1';
            
            element._tooltip = tooltip;
        });
        
        element.addEventListener('mouseleave', () => {
            if (element._tooltip) {
                element._tooltip.remove();
                delete element._tooltip;
            }
        });
    });
}

// Modals
function initModals() {
    const modalTriggers = document.querySelectorAll('[data-modal]');
    const modalCloses = document.querySelectorAll('.modal-close, .modal-backdrop');
    
    modalTriggers.forEach(trigger => {
        trigger.addEventListener('click', (e) => {
            e.preventDefault();
            const modalId = trigger.getAttribute('data-modal');
            const modal = document.getElementById(modalId);
            
            if (modal) {
                modal.classList.add('active');
                document.body.style.overflow = 'hidden';
            }
        });
    });
    
    modalCloses.forEach(close => {
        close.addEventListener('click', () => {
            const modal = close.closest('.modal');
            if (modal) {
                modal.classList.remove('active');
                document.body.style.overflow = '';
            }
        });
    });
    
    // Close on Escape
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            const activeModal = document.querySelector('.modal.active');
            if (activeModal) {
                activeModal.classList.remove('active');
                document.body.style.overflow = '';
            }
        }
    });
}

// Tabs
function initTabs() {
    const tabContainers = document.querySelectorAll('.tabs');
    
    tabContainers.forEach(container => {
        const tabButtons = container.querySelectorAll('.tab-button');
        const tabPanels = container.querySelectorAll('.tab-panel');
        
        tabButtons.forEach((button, index) => {
            button.addEventListener('click', () => {
                // Remove active class from all
                tabButtons.forEach(btn => btn.classList.remove('active'));
                tabPanels.forEach(panel => panel.classList.remove('active'));
                
                // Add active class to clicked
                button.classList.add('active');
                if (tabPanels[index]) {
                    tabPanels[index].classList.add('active');
                }
            });
        });
    });
}

// Accordions
function initAccordions() {
    const accordionHeaders = document.querySelectorAll('.accordion-header');
    
    accordionHeaders.forEach(header => {
        header.addEventListener('click', () => {
            const item = header.parentElement;
            const content = item.querySelector('.accordion-content');
            const isActive = item.classList.contains('active');
            
            // Close all other accordions in the same container
            const accordion = item.parentElement;
            accordion.querySelectorAll('.accordion-item').forEach(accordionItem => {
                if (accordionItem !== item) {
                    accordionItem.classList.remove('active');
                }
            });
            
            // Toggle current accordion
            item.classList.toggle('active');
            
            // Animate height
            if (!isActive && content) {
                content.style.maxHeight = content.scrollHeight + 'px';
            } else if (content) {
                content.style.maxHeight = '0';
            }
        });
    });
}

// Form Validation
function initFormValidation() {
    const forms = document.querySelectorAll('form[data-validate]');
    
    forms.forEach(form => {
        form.addEventListener('submit', (e) => {
            e.preventDefault();
            
            let isValid = true;
            const inputs = form.querySelectorAll('input[required], textarea[required]');
            
            inputs.forEach(input => {
                const value = input.value.trim();
                const errorElement = input.nextElementSibling;
                
                if (!value) {
                    isValid = false;
                    input.classList.add('error');
                    if (errorElement && errorElement.classList.contains('error-message')) {
                        errorElement.style.display = 'block';
                    }
                } else {
                    input.classList.remove('error');
                    if (errorElement && errorElement.classList.contains('error-message')) {
                        errorElement.style.display = 'none';
                    }
                }
                
                // Email validation
                if (input.type === 'email' && value) {
                    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
                    if (!emailRegex.test(value)) {
                        isValid = false;
                        input.classList.add('error');
                    }
                }
            });
            
            if (isValid) {
                // Show success message
                showNotification('Form submitted successfully!', 'success');
                form.reset();
            }
        });
    });
}

// Copy Code
function initCopyCode() {
    const codeBlocks = document.querySelectorAll('pre');
    
    codeBlocks.forEach(block => {
        const copyButton = document.createElement('button');
        copyButton.className = 'copy-code-btn';
        copyButton.innerHTML = '<i class="fas fa-copy"></i>';
        copyButton.title = 'Copy code';
        
        block.style.position = 'relative';
        block.appendChild(copyButton);
        
        copyButton.addEventListener('click', async () => {
            const code = block.querySelector('code').textContent;
            
            try {
                await navigator.clipboard.writeText(code);
                copyButton.innerHTML = '<i class="fas fa-check"></i>';
                copyButton.classList.add('copied');
                
                setTimeout(() => {
                    copyButton.innerHTML = '<i class="fas fa-copy"></i>';
                    copyButton.classList.remove('copied');
                }, 2000);
            } catch (err) {
                console.error('Failed to copy:', err);
            }
        });
    });
}

// Back to Top
function initBackToTop() {
    const backToTop = document.createElement('button');
    backToTop.className = 'back-to-top';
    backToTop.innerHTML = '<i class="fas fa-arrow-up"></i>';
    document.body.appendChild(backToTop);
    
    window.addEventListener('scroll', () => {
        if (window.pageYOffset > 300) {
            backToTop.classList.add('visible');
        } else {
            backToTop.classList.remove('visible');
        }
    });
    
    backToTop.addEventListener('click', () => {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
}

// Notification System
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <i class="fas fa-${type === 'success' ? 'check-circle' : 
                         type === 'error' ? 'exclamation-circle' : 
                         type === 'warning' ? 'exclamation-triangle' : 
                         'info-circle'}"></i>
        <span>${message}</span>
    `;
    
    document.body.appendChild(notification);
    
    // Animate in
    setTimeout(() => {
        notification.classList.add('show');
    }, 10);
    
    // Remove after 3 seconds
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => {
            notification.remove();
        }, 300);
    }, 3000);
}

// Blog Filters
function initBlogFilters() {
    const filterButtons = document.querySelectorAll('.filter-btn');
    const blogPosts = document.querySelectorAll('.blog-post');
    
    if (filterButtons.length === 0 || blogPosts.length === 0) return;
    
    filterButtons.forEach(button => {
        button.addEventListener('click', () => {
            const filter = button.getAttribute('data-filter');
            
            // Update active button
            filterButtons.forEach(btn => btn.classList.remove('active'));
            button.classList.add('active');
            
            // Filter posts
            blogPosts.forEach(post => {
                const category = post.getAttribute('data-category');
                
                if (filter === 'all' || category === filter) {
                    post.style.display = '';
                    // Animate in
                    post.style.opacity = '0';
                    post.style.transform = 'translateY(20px)';
                    
                    setTimeout(() => {
                        post.style.transition = 'all 0.3s ease';
                        post.style.opacity = '1';
                        post.style.transform = 'translateY(0)';
                    }, 50);
                } else {
                    post.style.opacity = '0';
                    post.style.transform = 'translateY(20px)';
                    
                    setTimeout(() => {
                        post.style.display = 'none';
                    }, 300);
                }
            });
            
            // Check if no posts visible
            const visiblePosts = Array.from(blogPosts).filter(post => {
                const category = post.getAttribute('data-category');
                return filter === 'all' || category === filter;
            });
            
            if (visiblePosts.length === 0) {
                // Show no results message
                const blogGrid = document.querySelector('.blog-grid');
                if (blogGrid && !document.querySelector('.no-posts-message')) {
                    const message = document.createElement('div');
                    message.className = 'no-posts-message';
                    message.innerHTML = `
                        <i class="fas fa-inbox"></i>
                        <p>No posts found in this category</p>
                    `;
                    blogGrid.appendChild(message);
                }
            } else {
                // Remove no results message if exists
                const message = document.querySelector('.no-posts-message');
                if (message) {
                    message.remove();
                }
            }
        });
    });
    
    // Blog pagination
    const paginationButtons = document.querySelectorAll('.pagination-btn:not(.next)');
    const nextButton = document.querySelector('.pagination-btn.next');
    
    paginationButtons.forEach((button, index) => {
        button.addEventListener('click', () => {
            // Update active button
            paginationButtons.forEach(btn => btn.classList.remove('active'));
            button.classList.add('active');
            
            // Scroll to top of blog section
            const blogSection = document.querySelector('.blog-section');
            if (blogSection) {
                const offset = document.querySelector('.navbar').offsetHeight || 0;
                const targetPosition = blogSection.getBoundingClientRect().top + window.pageYOffset - offset;
                
                window.scrollTo({
                    top: targetPosition,
                    behavior: 'smooth'
                });
            }
            
            // Here you would typically load new posts via AJAX
            showNotification(`Loading page ${button.textContent}...`, 'info');
        });
    });
    
    if (nextButton) {
        nextButton.addEventListener('click', () => {
            const activeButton = document.querySelector('.pagination-btn.active');
            const nextIndex = Array.from(paginationButtons).indexOf(activeButton) + 1;
            
            if (nextIndex < paginationButtons.length) {
                paginationButtons[nextIndex].click();
            }
        });
    }
}

// Export functions for external use
window.NeoServiceLayer = {
    showNotification,
    performSearch,
    updateThemeIcon
};
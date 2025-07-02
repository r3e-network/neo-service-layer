/**
 * Neo Service Layer - Navigation Component
 * Handles responsive navigation, active states, and smooth scrolling
 */

class NeoNavigation {
    constructor() {
        this.navbar = document.getElementById('navbar');
        this.navToggle = document.getElementById('nav-toggle');
        this.navMenu = document.getElementById('nav-menu');
        this.navLinks = document.querySelectorAll('.nav-link');
        this.scrollThreshold = 50;
        this.currentPath = window.location.pathname;
        
        this.init();
    }
    
    init() {
        // Mobile menu toggle
        if (this.navToggle) {
            this.navToggle.addEventListener('click', () => this.toggleMenu());
        }
        
        // Close mobile menu on link click
        this.navLinks.forEach(link => {
            link.addEventListener('click', () => this.closeMenu());
        });
        
        // Scroll behavior
        window.addEventListener('scroll', () => this.handleScroll());
        
        // Set active link
        this.setActiveLink();
        
        // Smooth scrolling for anchor links
        this.initSmoothScroll();
        
        // Close menu on outside click
        document.addEventListener('click', (e) => this.handleOutsideClick(e));
        
        // Handle resize
        window.addEventListener('resize', () => this.handleResize());
    }
    
    toggleMenu() {
        this.navToggle.classList.toggle('active');
        this.navMenu.classList.toggle('active');
        document.body.classList.toggle('nav-open');
    }
    
    closeMenu() {
        this.navToggle.classList.remove('active');
        this.navMenu.classList.remove('active');
        document.body.classList.remove('nav-open');
    }
    
    handleScroll() {
        if (window.scrollY > this.scrollThreshold) {
            this.navbar.classList.add('scrolled');
        } else {
            this.navbar.classList.remove('scrolled');
        }
        
        // Update active section for single-page navigation
        if (this.currentPath === '/' || this.currentPath === '/index.html') {
            this.updateActiveSection();
        }
    }
    
    setActiveLink() {
        this.navLinks.forEach(link => {
            const href = link.getAttribute('href');
            
            // Handle exact matches and directory matches
            if (href === this.currentPath || 
                (href.endsWith('/') && this.currentPath.startsWith(href)) ||
                (this.currentPath.includes(href) && href !== '/')) {
                link.classList.add('active');
            } else {
                link.classList.remove('active');
            }
        });
    }
    
    updateActiveSection() {
        const sections = document.querySelectorAll('section[id]');
        const scrollY = window.pageYOffset;
        
        sections.forEach(section => {
            const sectionHeight = section.offsetHeight;
            const sectionTop = section.offsetTop - 100;
            const sectionId = section.getAttribute('id');
            
            if (scrollY > sectionTop && scrollY <= sectionTop + sectionHeight) {
                this.navLinks.forEach(link => {
                    if (link.getAttribute('href') === `#${sectionId}`) {
                        link.classList.add('active');
                    } else if (link.getAttribute('href').startsWith('#')) {
                        link.classList.remove('active');
                    }
                });
            }
        });
    }
    
    initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', (e) => {
                const href = anchor.getAttribute('href');
                if (href === '#') return;
                
                const target = document.querySelector(href);
                if (target) {
                    e.preventDefault();
                    const offset = this.navbar.offsetHeight + 20;
                    const targetPosition = target.offsetTop - offset;
                    
                    window.scrollTo({
                        top: targetPosition,
                        behavior: 'smooth'
                    });
                    
                    // Update URL without jumping
                    history.pushState(null, null, href);
                }
            });
        });
    }
    
    handleOutsideClick(e) {
        if (this.navMenu.classList.contains('active') && 
            !this.navbar.contains(e.target)) {
            this.closeMenu();
        }
    }
    
    handleResize() {
        if (window.innerWidth > 768) {
            this.closeMenu();
        }
    }
}

// Initialize navigation when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    new NeoNavigation();
});

// Export for use in other scripts
window.NeoNavigation = NeoNavigation;
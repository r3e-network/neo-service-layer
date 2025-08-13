// Advanced Animations and Effects

// Initialize animations when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    initAOS();
    initParticles();
    initTextAnimations();
    initMorphingShapes();
    initGlowEffects();
    initWaveEffects();
    initMatrixRain();
    initFloatingElements();
    initScrollProgress();
    initMouseEffects();
});

// Initialize AOS (Animate On Scroll)
function initAOS() {
    if (typeof AOS !== 'undefined') {
        AOS.init({
            duration: 800,
            easing: 'ease-out-cubic',
            once: true,
            offset: 100,
            delay: 0,
            anchorPlacement: 'top-bottom'
        });
    }
}

// Particle System
function initParticles() {
    const particleContainers = document.querySelectorAll('.particle-container');
    
    particleContainers.forEach(container => {
        const particleCount = parseInt(container.getAttribute('data-particles')) || 50;
        const particleColor = container.getAttribute('data-particle-color') || '#00ff88';
        
        for (let i = 0; i < particleCount; i++) {
            createParticle(container, particleColor);
        }
    });
}

function createParticle(container, color) {
    const particle = document.createElement('div');
    particle.className = 'particle';
    particle.style.cssText = `
        position: absolute;
        width: ${Math.random() * 4 + 2}px;
        height: ${Math.random() * 4 + 2}px;
        background: ${color};
        border-radius: 50%;
        opacity: ${Math.random() * 0.5 + 0.3};
        left: ${Math.random() * 100}%;
        top: ${Math.random() * 100}%;
        animation: floatParticle ${Math.random() * 10 + 10}s linear infinite;
        animation-delay: ${Math.random() * 5}s;
    `;
    
    container.appendChild(particle);
    
    // Remove particle after animation
    particle.addEventListener('animationiteration', () => {
        particle.style.left = Math.random() * 100 + '%';
        particle.style.top = Math.random() * 100 + '%';
    });
}

// Text Animations
function initTextAnimations() {
    // Typewriter effect
    const typewriterElements = document.querySelectorAll('.typewriter');
    
    typewriterElements.forEach(element => {
        const text = element.getAttribute('data-text') || element.textContent;
        const speed = parseInt(element.getAttribute('data-speed')) || 50;
        element.textContent = '';
        
        typeWriter(element, text, 0, speed);
    });
    
    // Text reveal animation
    const revealTexts = document.querySelectorAll('.text-reveal');
    
    const textObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('revealed');
                
                // Animate each word
                const words = entry.target.textContent.split(' ');
                entry.target.innerHTML = words.map((word, index) => 
                    `<span class="word" style="animation-delay: ${index * 0.1}s">${word}</span>`
                ).join(' ');
            }
        });
    }, { threshold: 0.5 });
    
    revealTexts.forEach(text => textObserver.observe(text));
    
    // Glitch effect
    const glitchTexts = document.querySelectorAll('.glitch');
    
    glitchTexts.forEach(text => {
        const originalText = text.textContent;
        text.setAttribute('data-text', originalText);
        
        setInterval(() => {
            if (Math.random() < 0.1) {
                glitchText(text, originalText);
            }
        }, 3000);
    });
}

function typeWriter(element, text, index, speed) {
    if (index < text.length) {
        element.textContent += text.charAt(index);
        setTimeout(() => typeWriter(element, text, index + 1, speed), speed);
    } else {
        // Add blinking cursor
        const cursor = document.createElement('span');
        cursor.className = 'cursor';
        cursor.textContent = '|';
        element.appendChild(cursor);
    }
}

function glitchText(element, originalText) {
    const glitchChars = '!@#$%^&*()_+-=[]{}|;:,.<>?';
    let glitchedText = '';
    
    for (let i = 0; i < originalText.length; i++) {
        if (Math.random() < 0.1) {
            glitchedText += glitchChars[Math.floor(Math.random() * glitchChars.length)];
        } else {
            glitchedText += originalText[i];
        }
    }
    
    element.textContent = glitchedText;
    
    setTimeout(() => {
        element.textContent = originalText;
    }, 100);
}

// Morphing Shapes
function initMorphingShapes() {
    const morphShapes = document.querySelectorAll('.morph-shape');
    
    morphShapes.forEach(shape => {
        const svg = shape.querySelector('svg');
        if (!svg) return;
        
        const paths = svg.querySelectorAll('path');
        const morphTargets = shape.getAttribute('data-morph-targets');
        
        if (morphTargets && paths.length > 0) {
            const targets = JSON.parse(morphTargets);
            let currentTarget = 0;
            
            setInterval(() => {
                morphPath(paths[0], targets[currentTarget]);
                currentTarget = (currentTarget + 1) % targets.length;
            }, 3000);
        }
    });
}

function morphPath(path, targetD) {
    // Simple morph animation (for production, use a library like anime.js or GSAP)
    path.style.transition = 'all 1s ease-in-out';
    path.setAttribute('d', targetD);
}

// Glow Effects
function initGlowEffects() {
    const glowElements = document.querySelectorAll('.glow-effect');
    
    glowElements.forEach(element => {
        element.addEventListener('mouseenter', () => {
            createGlowRipple(element);
        });
    });
}

function createGlowRipple(element) {
    const ripple = document.createElement('div');
    ripple.className = 'glow-ripple';
    
    const rect = element.getBoundingClientRect();
    ripple.style.width = ripple.style.height = Math.max(rect.width, rect.height) * 2 + 'px';
    
    element.appendChild(ripple);
    
    setTimeout(() => {
        ripple.classList.add('active');
    }, 10);
    
    setTimeout(() => {
        ripple.remove();
    }, 1000);
}

// Wave Effects
function initWaveEffects() {
    const waveContainers = document.querySelectorAll('.wave-container');
    
    waveContainers.forEach(container => {
        const canvas = document.createElement('canvas');
        canvas.className = 'wave-canvas';
        container.appendChild(canvas);
        
        const ctx = canvas.getContext('2d');
        let animationId;
        
        function resizeCanvas() {
            canvas.width = container.offsetWidth;
            canvas.height = container.offsetHeight;
        }
        
        function drawWave(timestamp) {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            
            const waveHeight = 50;
            const waveLength = 0.01;
            const waveSpeed = 0.001;
            
            ctx.beginPath();
            ctx.moveTo(0, canvas.height / 2);
            
            for (let x = 0; x < canvas.width; x++) {
                const y = canvas.height / 2 + 
                         Math.sin(x * waveLength + timestamp * waveSpeed) * waveHeight;
                ctx.lineTo(x, y);
            }
            
            ctx.strokeStyle = 'rgba(0, 255, 136, 0.3)';
            ctx.lineWidth = 2;
            ctx.stroke();
            
            animationId = requestAnimationFrame(drawWave);
        }
        
        resizeCanvas();
        window.addEventListener('resize', resizeCanvas);
        drawWave(0);
        
        // Clean up on removal
        const observer = new MutationObserver(() => {
            if (!document.contains(container)) {
                cancelAnimationFrame(animationId);
                observer.disconnect();
            }
        });
        
        observer.observe(container.parentNode, { childList: true });
    });
}

// Matrix Rain Effect
function initMatrixRain() {
    const matrixContainers = document.querySelectorAll('.matrix-rain');
    
    matrixContainers.forEach(container => {
        const canvas = document.createElement('canvas');
        canvas.className = 'matrix-canvas';
        container.appendChild(canvas);
        
        const ctx = canvas.getContext('2d');
        
        canvas.width = container.offsetWidth;
        canvas.height = container.offsetHeight;
        
        const matrix = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789@#$%^&*()*&^%+-/~{[|`]}';
        const matrixArray = matrix.split('');
        
        const fontSize = 10;
        const columns = canvas.width / fontSize;
        
        const drops = [];
        for (let x = 0; x < columns; x++) {
            drops[x] = 1;
        }
        
        function drawMatrix() {
            ctx.fillStyle = 'rgba(0, 0, 0, 0.04)';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            
            ctx.fillStyle = '#00ff88';
            ctx.font = fontSize + 'px monospace';
            
            for (let i = 0; i < drops.length; i++) {
                const text = matrixArray[Math.floor(Math.random() * matrixArray.length)];
                ctx.fillText(text, i * fontSize, drops[i] * fontSize);
                
                if (drops[i] * fontSize > canvas.height && Math.random() > 0.975) {
                    drops[i] = 0;
                }
                
                drops[i]++;
            }
        }
        
        const matrixInterval = setInterval(drawMatrix, 35);
        
        // Clean up
        const observer = new MutationObserver(() => {
            if (!document.contains(container)) {
                clearInterval(matrixInterval);
                observer.disconnect();
            }
        });
        
        observer.observe(container.parentNode, { childList: true });
    });
}

// Floating Elements
function initFloatingElements() {
    const floatingElements = document.querySelectorAll('.floating');
    
    floatingElements.forEach((element, index) => {
        const duration = 3 + Math.random() * 2;
        const delay = index * 0.2;
        const distance = 10 + Math.random() * 20;
        
        element.style.animation = `float ${duration}s ease-in-out ${delay}s infinite`;
        element.style.setProperty('--float-distance', `${distance}px`);
    });
}

// Scroll Progress Indicator
function initScrollProgress() {
    const progressBar = document.createElement('div');
    progressBar.className = 'scroll-progress';
    document.body.appendChild(progressBar);
    
    window.addEventListener('scroll', () => {
        const windowHeight = window.innerHeight;
        const documentHeight = document.documentElement.scrollHeight - windowHeight;
        const scrolled = window.scrollY;
        const progress = (scrolled / documentHeight) * 100;
        
        progressBar.style.width = progress + '%';
    });
}

// Mouse Effects
function initMouseEffects() {
    // Custom cursor
    const cursor = document.createElement('div');
    cursor.className = 'custom-cursor';
    document.body.appendChild(cursor);
    
    const cursorDot = document.createElement('div');
    cursorDot.className = 'cursor-dot';
    document.body.appendChild(cursorDot);
    
    let mouseX = 0;
    let mouseY = 0;
    let cursorX = 0;
    let cursorY = 0;
    
    document.addEventListener('mousemove', (e) => {
        mouseX = e.clientX;
        mouseY = e.clientY;
    });
    
    function animateCursor() {
        const distX = mouseX - cursorX;
        const distY = mouseY - cursorY;
        
        cursorX += distX * 0.1;
        cursorY += distY * 0.1;
        
        cursor.style.left = cursorX + 'px';
        cursor.style.top = cursorY + 'px';
        
        cursorDot.style.left = mouseX + 'px';
        cursorDot.style.top = mouseY + 'px';
        
        requestAnimationFrame(animateCursor);
    }
    
    animateCursor();
    
    // Hover effects
    const hoverElements = document.querySelectorAll('a, button, .interactive');
    
    hoverElements.forEach(element => {
        element.addEventListener('mouseenter', () => {
            cursor.classList.add('hover');
            cursorDot.classList.add('hover');
        });
        
        element.addEventListener('mouseleave', () => {
            cursor.classList.remove('hover');
            cursorDot.classList.remove('hover');
        });
    });
    
    // Magnetic effect
    const magneticElements = document.querySelectorAll('.magnetic');
    
    magneticElements.forEach(element => {
        element.addEventListener('mousemove', (e) => {
            const rect = element.getBoundingClientRect();
            const x = e.clientX - rect.left - rect.width / 2;
            const y = e.clientY - rect.top - rect.height / 2;
            
            element.style.transform = `translate(${x * 0.3}px, ${y * 0.3}px)`;
        });
        
        element.addEventListener('mouseleave', () => {
            element.style.transform = 'translate(0, 0)';
        });
    });
}

// CSS for dynamic animations
const style = document.createElement('style');
style.textContent = `
    @keyframes floatParticle {
        from {
            transform: translateY(100vh) rotate(0deg);
            opacity: 0;
        }
        10% {
            opacity: 1;
        }
        90% {
            opacity: 1;
        }
        to {
            transform: translateY(-100vh) rotate(360deg);
            opacity: 0;
        }
    }
    
    .cursor {
        animation: blink 1s ease-in-out infinite;
    }
    
    @keyframes blink {
        0%, 50% { opacity: 1; }
        51%, 100% { opacity: 0; }
    }
    
    .word {
        display: inline-block;
        opacity: 0;
        transform: translateY(20px);
        animation: revealWord 0.6s ease forwards;
    }
    
    @keyframes revealWord {
        to {
            opacity: 1;
            transform: translateY(0);
        }
    }
    
    .glow-ripple {
        position: absolute;
        border-radius: 50%;
        background: radial-gradient(circle, rgba(0, 255, 136, 0.3) 0%, transparent 70%);
        transform: translate(-50%, -50%) scale(0);
        pointer-events: none;
        transition: transform 1s ease-out, opacity 1s ease-out;
    }
    
    .glow-ripple.active {
        transform: translate(-50%, -50%) scale(1);
        opacity: 0;
    }
    
    .scroll-progress {
        position: fixed;
        top: 0;
        left: 0;
        height: 3px;
        background: var(--gradient-primary);
        z-index: 9999;
        transition: width 0.1s ease;
    }
    
    .custom-cursor {
        position: fixed;
        width: 20px;
        height: 20px;
        border: 2px solid var(--primary);
        border-radius: 50%;
        pointer-events: none;
        z-index: 9998;
        transition: transform 0.2s ease, border-color 0.2s ease;
        transform: translate(-50%, -50%);
    }
    
    .cursor-dot {
        position: fixed;
        width: 4px;
        height: 4px;
        background: var(--primary);
        border-radius: 50%;
        pointer-events: none;
        z-index: 9999;
        transform: translate(-50%, -50%);
    }
    
    .custom-cursor.hover {
        transform: translate(-50%, -50%) scale(2);
        border-color: var(--secondary);
    }
    
    .cursor-dot.hover {
        background: var(--secondary);
    }
    
    @media (hover: none) {
        .custom-cursor,
        .cursor-dot {
            display: none;
        }
    }
`;

document.head.appendChild(style);

// Export animation functions for external use
window.NeoAnimations = {
    createParticle,
    typeWriter,
    glitchText,
    createGlowRipple,
    morphPath
};
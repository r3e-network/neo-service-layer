/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './pages/**/*.{js,ts,jsx,tsx,mdx}',
    './components/**/*.{js,ts,jsx,tsx,mdx}',
    './app/**/*.{js,ts,jsx,tsx,mdx}',
    './src/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // Neo Brand Colors
        neo: {
          50: '#f0fdf4',
          100: '#dcfce7',
          200: '#bbf7d0',
          300: '#86efac',
          400: '#4ade80',
          500: '#00d4aa', // Primary Neo Green
          600: '#00b894',
          700: '#009876',
          800: '#007a5e',
          900: '#00654d',
          950: '#003d2e',
        },
        // Custom Dark Theme
        dark: {
          50: '#f8fafc',
          100: '#f1f5f9',
          200: '#e2e8f0',
          300: '#cbd5e1',
          400: '#94a3b8',
          500: '#64748b',
          600: '#475569',
          700: '#334155',
          800: '#1e293b',
          900: '#0f172a',
          950: '#020617',
        },
        // Background gradients
        background: {
          primary: '#0a0e27',
          secondary: '#1a1f3a',
          tertiary: '#2a2f4a',
        },
        // Service category colors
        services: {
          core: '#00d4aa',
          business: '#6c5ce7',
          technical: '#fd79a8',
          specialized: '#fdcb6e',
          advanced: '#e17055',
        },
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'Fira Code', 'monospace'],
        display: ['Inter', 'system-ui', 'sans-serif'],
      },
      fontSize: {
        '2xs': ['0.625rem', { lineHeight: '0.75rem' }],
        '3xl': ['1.875rem', { lineHeight: '2.25rem' }],
        '4xl': ['2.25rem', { lineHeight: '2.5rem' }],
        '5xl': ['3rem', { lineHeight: '1' }],
        '6xl': ['3.75rem', { lineHeight: '1' }],
        '7xl': ['4.5rem', { lineHeight: '1' }],
        '8xl': ['6rem', { lineHeight: '1' }],
        '9xl': ['8rem', { lineHeight: '1' }],
      },
      spacing: {
        '18': '4.5rem',
        '88': '22rem',
        '128': '32rem',
      },
      maxWidth: {
        '8xl': '88rem',
        '9xl': '96rem',
      },
      animation: {
        'fade-in': 'fadeIn 0.5s ease-in-out',
        'fade-in-up': 'fadeInUp 0.5s ease-out',
        'fade-in-down': 'fadeInDown 0.5s ease-out',
        'slide-in-left': 'slideInLeft 0.5s ease-out',
        'slide-in-right': 'slideInRight 0.5s ease-out',
        'scale-in': 'scaleIn 0.3s ease-out',
        'pulse-glow': 'pulseGlow 2s infinite',
        'gradient-shift': 'gradientShift 3s ease-in-out infinite',
        'float': 'float 3s ease-in-out infinite',
        'spin-slow': 'spin 3s linear infinite',
        'bounce-gentle': 'bounceGentle 2s ease-in-out infinite',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        fadeInUp: {
          '0%': { opacity: '0', transform: 'translateY(20px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        fadeInDown: {
          '0%': { opacity: '0', transform: 'translateY(-20px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        slideInLeft: {
          '0%': { opacity: '0', transform: 'translateX(-20px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
        slideInRight: {
          '0%': { opacity: '0', transform: 'translateX(20px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
        scaleIn: {
          '0%': { opacity: '0', transform: 'scale(0.9)' },
          '100%': { opacity: '1', transform: 'scale(1)' },
        },
        pulseGlow: {
          '0%, 100%': { boxShadow: '0 0 20px rgba(0, 212, 170, 0.5)' },
          '50%': { boxShadow: '0 0 40px rgba(0, 212, 170, 0.8)' },
        },
        gradientShift: {
          '0%, 100%': { backgroundPosition: '0% 50%' },
          '50%': { backgroundPosition: '100% 50%' },
        },
        float: {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(-10px)' },
        },
        bounceGentle: {
          '0%, 100%': { transform: 'translateY(0)' },
          '50%': { transform: 'translateY(-5px)' },
        },
      },
      backgroundImage: {
        'gradient-radial': 'radial-gradient(var(--tw-gradient-stops))',
        'gradient-conic': 'conic-gradient(from 180deg at 50% 50%, var(--tw-gradient-stops))',
        'neo-gradient': 'linear-gradient(135deg, #00d4aa 0%, #6c5ce7 50%, #fd79a8 100%)',
        'dark-gradient': 'linear-gradient(135deg, #0a0e27 0%, #1a1f3a 50%, #2a2f4a 100%)',
      },
      boxShadow: {
        'neo': '0 0 20px rgba(0, 212, 170, 0.3)',
        'neo-lg': '0 0 40px rgba(0, 212, 170, 0.4)',
        'glow': '0 0 20px rgba(255, 255, 255, 0.1)',
        'glow-lg': '0 0 40px rgba(255, 255, 255, 0.15)',
        'inner-glow': 'inset 0 0 20px rgba(0, 212, 170, 0.1)',
      },
      borderColor: {
        'neo': '#00d4aa',
        'neo-light': 'rgba(0, 212, 170, 0.3)',
      },
      backdropBlur: {
        xs: '2px',
      },
      screens: {
        'xs': '475px',
        '3xl': '1680px',
      },
    },
  },
  plugins: [
    require('@tailwindcss/typography'),
    require('@tailwindcss/forms'),
    // Custom utilities
    function({ addUtilities }) {
      addUtilities({
        '.text-gradient': {
          'background': 'linear-gradient(135deg, #00d4aa 0%, #6c5ce7 100%)',
          '-webkit-background-clip': 'text',
          '-webkit-text-fill-color': 'transparent',
          'background-clip': 'text',
        },
        '.neo-border': {
          'border': '1px solid rgba(0, 212, 170, 0.3)',
        },
        '.neo-border-glow': {
          'border': '1px solid #00d4aa',
          'box-shadow': '0 0 10px rgba(0, 212, 170, 0.3)',
        },
        '.glass': {
          'background': 'rgba(255, 255, 255, 0.05)',
          'backdrop-filter': 'blur(10px)',
          'border': '1px solid rgba(255, 255, 255, 0.1)',
        },
        '.glass-dark': {
          'background': 'rgba(0, 0, 0, 0.3)',
          'backdrop-filter': 'blur(10px)',
          'border': '1px solid rgba(255, 255, 255, 0.1)',
        },
      });
    },
  ],
};
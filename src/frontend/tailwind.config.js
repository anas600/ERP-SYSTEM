/** @type {import('tailwindcss').Config} */
// Sprint 39 (DEC-125): Design system overhaul — extended theme with custom colors,
// spacing, animations, shadows, and typography. All UI components now use these
// design tokens instead of hard-coded values.
module.exports = {
  content: [
    './app/**/*.{js,ts,jsx,tsx}',
    './lib/**/*.{js,ts,jsx,tsx}',
    './components/**/*.{js,ts,jsx,tsx}',
  ],
  theme: {
    extend: {
      // Brand colors — used for primary actions, focus rings, active states
      colors: {
        brand: {
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6',  // primary
          600: '#2563eb',  // primary-hover
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a8a',
          950: '#172554',
        },
        // Semantic colors
        success: {
          50: '#f0fdf4',
          100: '#dcfce7',
          500: '#22c55e',
          600: '#16a34a',
          700: '#15803d',
        },
        warning: {
          50: '#fffbeb',
          100: '#fef3c7',
          500: '#f59e0b',
          600: '#d97706',
          700: '#b45309',
        },
        danger: {
          50: '#fef2f2',
          100: '#fee2e2',
          500: '#ef4444',
          600: '#dc2626',
          700: '#b91c1c',
        },
        // Neutral grays (slightly cooler than default Tailwind)
        ink: {
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
        },
      },
      // Custom font family — IBM Plex Sans Arabic for body, IBM Plex Sans for numbers
      fontFamily: {
        sans: ['var(--font-arabic)', 'system-ui', '-apple-system', 'sans-serif'],
        mono: ['"JetBrains Mono"', '"Fira Code"', 'monospace'],
        display: ['var(--font-arabic)', 'system-ui', 'sans-serif'],
      },
      // Spacing scale — extends Tailwind default with 18 (4.5rem) and 88 (22rem)
      spacing: {
        '18': '4.5rem',
        '88': '22rem',
        '112': '28rem',
        '128': '32rem',
      },
      // Shadows — softer, more modern
      boxShadow: {
        'soft-sm': '0 1px 2px 0 rgb(0 0 0 / 0.04)',
        'soft': '0 1px 3px 0 rgb(0 0 0 / 0.06), 0 1px 2px -1px rgb(0 0 0 / 0.04)',
        'soft-md': '0 4px 6px -1px rgb(0 0 0 / 0.06), 0 2px 4px -2px rgb(0 0 0 / 0.04)',
        'soft-lg': '0 10px 15px -3px rgb(0 0 0 / 0.06), 0 4px 6px -4px rgb(0 0 0 / 0.04)',
        'soft-xl': '0 20px 25px -5px rgb(0 0 0 / 0.06), 0 8px 10px -6px rgb(0 0 0 / 0.04)',
        'inner-soft': 'inset 0 1px 2px 0 rgb(0 0 0 / 0.04)',
        'focus-brand': '0 0 0 3px rgb(59 130 246 / 0.18)',
      },
      // Border radius — consistent rounded corners
      borderRadius: {
        'xs': '0.25rem',
        'sm': '0.375rem',
        DEFAULT: '0.5rem',
        'md': '0.5rem',
        'lg': '0.75rem',
        'xl': '1rem',
        '2xl': '1.25rem',
        '3xl': '1.5rem',
      },
      // Animations
      animation: {
        'fade-in': 'fadeIn 200ms ease-out',
        'slide-up': 'slideUp 250ms cubic-bezier(0.16, 1, 0.3, 1)',
        'slide-down': 'slideDown 250ms cubic-bezier(0.16, 1, 0.3, 1)',
        'slide-in-from-top': 'slideInFromTop 250ms cubic-bezier(0.16, 1, 0.3, 1)',
        'slide-in-from-right': 'slideInFromRight 250ms cubic-bezier(0.16, 1, 0.3, 1)',
        'scale-in': 'scaleIn 200ms ease-out',
        'pulse-soft': 'pulseSoft 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'shimmer': 'shimmer 2s linear infinite',
        'spin-slow': 'spin 3s linear infinite',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideUp: {
          '0%': { opacity: '0', transform: 'translateY(8px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        slideDown: {
          '0%': { opacity: '0', transform: 'translateY(-8px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        slideInFromTop: {
          '0%': { opacity: '0', transform: 'translateY(-12px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        slideInFromRight: {
          '0%': { opacity: '0', transform: 'translateX(12px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
        scaleIn: {
          '0%': { opacity: '0', transform: 'scale(0.96)' },
          '100%': { opacity: '1', transform: 'scale(1)' },
        },
        pulseSoft: {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.7' },
        },
        shimmer: {
          '0%': { backgroundPosition: '-200% 0' },
          '100%': { backgroundPosition: '200% 0' },
        },
      },
      // Backdrop blur for overlays
      backdropBlur: {
        xs: '2px',
        '4xl': '72px',
      },
      // Transition timing functions
      transitionTimingFunction: {
        'ease-in-out-quart': 'cubic-bezier(0.76, 0, 0.24, 1)',
        'ease-out-quart': 'cubic-bezier(0.16, 1, 0.3, 1)',
      },
    },
  },
  plugins: [],
};

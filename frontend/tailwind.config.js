/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: [
    './index.html',
    './src/**/*.{ts,tsx}',
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          25:  '#f2f7ff',
          50:  '#ecf3ff',
          100: '#dde9ff',
          200: '#c2d6ff',
          300: '#9cb9ff',
          400: '#7592ff',
          500: '#465fff',
          600: '#3641f5',
          700: '#2a31d8',
          800: '#252dae',
          900: '#262e89',
          950: '#161950',
        },
      },
      fontFamily: {
        outfit: ['Outfit', 'sans-serif'],
      },
      // TailAdmin custom text sizes
      fontSize: {
        'theme-xs':  ['12px', { lineHeight: '18px' }],
        'theme-sm':  ['14px', { lineHeight: '20px' }],
        'theme-xl':  ['20px', { lineHeight: '30px' }],
        'title-sm':  ['30px', { lineHeight: '38px' }],
        'title-md':  ['36px', { lineHeight: '44px' }],
        'title-lg':  ['48px', { lineHeight: '60px' }],
        'title-xl':  ['60px', { lineHeight: '72px' }],
        'title-2xl': ['72px', { lineHeight: '90px' }],
      },
      boxShadow: {
        'theme-xs': '0px 1px 2px 0px rgba(16,24,40,0.05)',
        'theme-sm': '0px 1px 3px 0px rgba(16,24,40,0.1), 0px 1px 2px 0px rgba(16,24,40,0.06)',
        'theme-md': '0px 4px 8px -2px rgba(16,24,40,0.1), 0px 2px 4px -2px rgba(16,24,40,0.06)',
        'theme-lg': '0px 12px 16px -4px rgba(16,24,40,0.08), 0px 4px 6px -2px rgba(16,24,40,0.03)',
        'theme-xl': '0px 20px 24px -4px rgba(16,24,40,0.08), 0px 8px 8px -4px rgba(16,24,40,0.03)',
      },
      zIndex: {
        '99999': '99999',
        '999999': '999999',
      },
    },
  },
  plugins: [],
}


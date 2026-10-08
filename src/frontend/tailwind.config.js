/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        // "Survey sheet" palette: limestone ground, slate ink, malachite (copper ore) for action.
        // Risk colours appear only when someone is at risk; a normal state stays in ink.
        limestone: '#E8ECE6',
        sheet: '#F6F7F3',
        rule: '#C9D0C7',
        rock: '#56645F',
        ink: { DEFAULT: '#1D2A2E', soft: '#33433F' },
        malachite: { DEFAULT: '#1E6A5A', deep: '#154B40', wash: '#DCE9E3' },
        ochre: { DEFAULT: '#9A5E0C', bright: '#C98A1E', wash: '#F5E7CF' },
        signal: { DEFAULT: '#BF3018', wash: '#F7DCD3' },
      },
      fontFamily: {
        sans: ['"Archivo Variable"', 'Archivo', 'system-ui', 'sans-serif'],
      },
      fontSize: {
        // 1.25 ratio from a 15px body.
        xs: ['0.75rem', { lineHeight: '1.1rem' }],
        sm: ['0.8125rem', { lineHeight: '1.25rem' }],
        base: ['0.9375rem', { lineHeight: '1.5rem' }],
        lg: ['1.1875rem', { lineHeight: '1.6rem' }],
        xl: ['1.5rem', { lineHeight: '1.9rem' }],
        '2xl': ['1.875rem', { lineHeight: '2.2rem' }],
        '3xl': ['2.375rem', { lineHeight: '2.6rem' }],
        '4xl': ['3rem', { lineHeight: '3.1rem' }],
      },
      borderRadius: {
        DEFAULT: '3px',
        md: '5px',
      },
    },
  },
  plugins: [],
}

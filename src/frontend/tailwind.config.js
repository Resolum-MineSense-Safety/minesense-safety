/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,jsx}'],
  theme: {
    extend: {
      colors: {
        mineral: '#F3F5F4', // page background, cool rock dust
        line: '#DDE3E1', // hairlines and borders
        basalt: { DEFAULT: '#1F2A2E', soft: '#3B4A50', mute: '#6B7A80' },
        haul: { DEFAULT: '#F2B705', deep: '#B98A00', wash: '#FDF3CC' }, // haul-truck safety yellow
        ore: { DEFAULT: '#1F7A8C', wash: '#E3F1F4' }, // copper-oxide teal for info and links
        risk: {
          normal: '#2E9E6B',
          normalWash: '#E2F3EA',
          warning: '#E58A00',
          warningWash: '#FDEFD9',
          critical: '#D6362F',
          criticalWash: '#FBE3E1',
        },
      },
      fontFamily: {
        sans: ['Barlow', 'system-ui', 'sans-serif'],
        cond: ['"Barlow Condensed"', 'Barlow', 'sans-serif'],
      },
      borderRadius: { tile: '14px' },
      keyframes: {
        beacon: { '0%, 100%': { opacity: '1' }, '50%': { opacity: '0.35' } },
        slidein: { from: { transform: 'translateX(24px)', opacity: '0' }, to: { transform: 'none', opacity: '1' } },
      },
      animation: {
        beacon: 'beacon 1.6s ease-in-out infinite',
        slidein: 'slidein 180ms ease-out',
      },
    },
  },
  plugins: [],
}

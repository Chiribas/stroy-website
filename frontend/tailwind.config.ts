import type { Config } from 'tailwindcss'

export default <Partial<Config>>{
  content: [],
  theme: {
    extend: {
      colors: {
        brand: { DEFAULT: '#d97706', dark: '#b45309', light: '#fbbf24' },
        ink: '#1f2937',
        muted: '#6b7280',
      },
      fontFamily: { sans: ['Inter', 'system-ui', 'sans-serif'] },
      borderRadius: { card: '0.75rem' },
    },
  },
}

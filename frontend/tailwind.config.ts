import type { Config } from 'tailwindcss'
import typography from '@tailwindcss/typography'

const withOpacity = (v: string) => `rgb(var(${v}) / <alpha-value>)`

export default <Partial<Config>>{
  darkMode: 'class',
  content: [],
  theme: {
    extend: {
      colors: {
        surface: withOpacity('--surface'),
        'surface-2': withOpacity('--surface-2'),
        base: withOpacity('--base'),
        ink: withOpacity('--ink'),
        muted: withOpacity('--muted'),
        brand: {
          DEFAULT: withOpacity('--brand'),
          contrast: withOpacity('--brand-contrast'),
        },
      },
      fontFamily: { sans: ['Inter', 'system-ui', 'sans-serif'] },
      borderRadius: { card: '0.75rem' },
    },
  },
  plugins: [typography],
}

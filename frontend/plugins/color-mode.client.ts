export default defineNuxtPlugin(() => {
  const apply = (dark: boolean) => document.documentElement.classList.toggle('dark', dark)
  const stored = localStorage.getItem('theme')
  const system = window.matchMedia('(prefers-color-scheme: dark)').matches
  apply(stored ? stored === 'dark' : system)
})

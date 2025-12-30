import { defineStore } from 'pinia'
import type { ThemeName } from '../plugins/vuetify'

const STORAGE_KEY = 'photo-manager-theme'

export const useUiStore = defineStore('ui', {
  state: () => ({
    currentTheme: 'lightTheme' as ThemeName,
    prefersSchemeWatcher: null as MediaQueryList | null,
  }),
  actions: {
    bootstrap() {
      if (typeof window === 'undefined') return
      const stored = window.localStorage.getItem(STORAGE_KEY) as ThemeName | null
      if (stored) {
        this.currentTheme = stored
      } else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
        this.currentTheme = 'darkTheme'
      }
      this.applyTheme()
      this.prefersSchemeWatcher = window.matchMedia('(prefers-color-scheme: dark)')
      this.prefersSchemeWatcher.addEventListener('change', this.handleSchemeChange)
    },
    teardown() {
      this.prefersSchemeWatcher?.removeEventListener('change', this.handleSchemeChange)
    },
    toggleTheme() {
      this.setTheme(this.currentTheme === 'lightTheme' ? 'darkTheme' : 'lightTheme')
    },
    setTheme(theme: ThemeName) {
      this.currentTheme = theme
      if (typeof window !== 'undefined') {
        window.localStorage.setItem(STORAGE_KEY, theme)
      }
      this.applyTheme()
    },
    handleSchemeChange(event: MediaQueryListEvent) {
      if (typeof window === 'undefined') return
      const stored = window.localStorage.getItem(STORAGE_KEY)
      if (stored) return
      this.setTheme(event.matches ? 'darkTheme' : 'lightTheme')
    },
    applyTheme() {
      if (typeof document === 'undefined') return
      document.documentElement.dataset.theme = this.currentTheme
    },
  },
})

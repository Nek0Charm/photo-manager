import { createVuetify, type ThemeDefinition } from 'vuetify'
import { md3 } from 'vuetify/blueprints'
import { aliases, mdi } from 'vuetify/iconsets/mdi'

const lightTheme: ThemeDefinition = {
  dark: false,
  colors: {
    background: '#f6f8fa',
    surface: '#ffffff',
    primary: '#0969da',
    secondary: '#57606a',
    'surface-bright': '#fefefe',
    'surface-variant': '#d0d7de',
    'on-surface-variant': '#24292f',
    'surface-hover': '#f3f4f6',
    'border-subtle': '#d8dee4',
    'border-strong': '#6e7781',
    success: '#1a7f37',
    info: '#0969da',
    warning: '#bf8700',
    error: '#cf222e',
  },
}

const darkTheme: ThemeDefinition = {
  dark: true,
  colors: {
    background: '#0d1117',
    surface: '#161b22',
    primary: '#2f81f7',
    secondary: '#8b949e',
    'surface-bright': '#1f242d',
    'surface-variant': '#30363d',
    'on-surface-variant': '#e6edf3',
    'surface-hover': '#21262d',
    'border-subtle': '#30363d',
    'border-strong': '#8b949e',
    success: '#3fb950',
    info: '#2f81f7',
    warning: '#d29922',
    error: '#ff7b72',
  },
}

export type ThemeName = 'lightTheme' | 'darkTheme'
export { lightTheme, darkTheme }

const vuetify = createVuetify({
  blueprint: md3,
  theme: {
    defaultTheme: 'lightTheme',
    themes: {
      lightTheme,
      darkTheme,
    },
  },
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: {
      mdi,
    },
  },
})

export default vuetify

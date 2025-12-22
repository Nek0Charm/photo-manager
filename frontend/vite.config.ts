import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vuetify from 'vite-plugin-vuetify'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vuetify({
      autoImport: true,
    }),
  ],
  server: {
    host: '0.0.0.0',
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5151',
        changeOrigin: true,
        secure: false,
      },
      '/uploads': {
        target: 'http://localhost:5151',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})

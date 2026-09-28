import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// The dev server forwards /api requests to the .NET API, so the browser never makes cross-origin calls.
// API_PROXY_TARGET has no VITE_ prefix on purpose: it is only read here and never shipped to the browser.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', '')
  const apiTarget = env.API_PROXY_TARGET || 'http://localhost:5151'

  return {
    plugins: [react(), tailwindcss()],
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: apiTarget,
          changeOrigin: true,
        },
      },
    },
  }
})

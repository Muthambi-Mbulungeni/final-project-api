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
          // When the API is not running, answer 502 Bad Gateway, as a production gateway would,
          // so the portal can say "could not be reached" instead of "something went wrong".
          configure: (proxy) => {
            proxy.on('error', (_error, _request, response) => {
              if (response && typeof response.writeHead === 'function' && !response.headersSent) {
                response.writeHead(502, { 'Content-Type': 'application/json' })
                response.end(JSON.stringify({ message: 'The API is not reachable.' }))
              }
            })
          },
        },
      },
    },
  }
})

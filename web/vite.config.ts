import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    // Proxy opcional para desarrollar contra una API remota (staging/producción) sin
    // pelear con CORS: `VITE_DEV_API_PROXY=https://… pnpm dev` y dejar VITE_API_URL en
    // `/api/v1`. Replica lo que hace Caddy en producción, donde el front y la API
    // comparten origen. Sin la variable, el server queda exactamente como antes.
    proxy: process.env.VITE_DEV_API_PROXY
      ? { '/api': { target: process.env.VITE_DEV_API_PROXY, changeOrigin: true, secure: true } }
      : undefined,
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/vitest.setup.ts'],
  },
})

/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      // '@' -> frontend/src, so tests located outside this folder can import
      // app code with a stable path (e.g. '@/components/TradeEntryForm.vue').
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    // Allow importing test files that live in the repo-level tests/ folder
    // (one directory above this project root).
    fs: { allow: ['..'] },
    // Proxy API calls to the .NET backend during dev so the frontend can use
    // same-origin relative URLs (/trades, /positions) with no CORS friction.
    proxy: {
      '/trades': 'http://localhost:5000',
      '/positions': 'http://localhost:5000',
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    // Frontend unit/component tests live under the repo-level tests/ folder.
    include: ['../tests/frontend/unit/**/*.spec.ts'],
  },
})

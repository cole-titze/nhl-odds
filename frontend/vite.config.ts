import { reactRouter } from '@react-router/dev/vite'
import { defineConfig } from 'vite'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [tailwindcss(), reactRouter()],
  environments: {
    client: {
      build: {
        rollupOptions: {
          output: {
            // Vite 8 (Rolldown) takes only the function form of manualChunks
            manualChunks: (id) => {
              if (/node_modules\/(react|react-dom|react-router|scheduler)\//.test(id)) return 'vendor';
              if (/node_modules\/recharts\//.test(id)) return 'recharts';
            },
          },
        },
      },
    },
  },
  server: {
    host: true,
    proxy: {
      '/api': {
        target: 'http://localhost:32616',
        changeOrigin: true,
      },
    },
  },
})

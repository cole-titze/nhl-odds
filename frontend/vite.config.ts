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
            // A group also takes the modules it imports, so React goes in
            // vendor first (higher priority) or recharts would pull it in, and
            // every page would then load the chart library
            codeSplitting: {
              groups: [
                { name: 'vendor', test: /node_modules\/(react|react-dom|react-router|scheduler)\//, priority: 2 },
                { name: 'recharts', test: /node_modules\/recharts\//, priority: 1 },
              ],
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

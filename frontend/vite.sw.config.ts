import { resolve } from 'path';
import { defineConfig } from 'vite';

// The service worker is its own entry with a fixed name (/sw.js), built after
// the app into the same client folder
export default defineConfig({
  publicDir: false,
  build: {
    outDir: 'build/client',
    emptyOutDir: false,
    copyPublicDir: false,
    rollupOptions: {
      input: resolve(import.meta.dirname, 'src/sw.ts'),
      output: { entryFileNames: 'sw.js' },
    },
  },
});

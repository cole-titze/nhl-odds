import { execSync } from 'node:child_process'
import { reactRouter } from '@react-router/dev/vite'
import { defineConfig, type Plugin } from 'vite'
import tailwindcss from '@tailwindcss/vite'

// The build's version: the commit's date and short SHA (2026.11.03-abc1234), so the same code
// always gets the same version. CI passes GIT_SHA and BUILD_DATE (the Docker build has no .git);
// locally they come from git.
function git(args: string): string {
  try {
    return execSync(`git ${args}`, { stdio: ['ignore', 'pipe', 'ignore'] }).toString().trim()
  } catch {
    return ''
  }
}
const sha = process.env.GIT_SHA || git('rev-parse HEAD')
const date =
  process.env.BUILD_DATE ||
  git('log -1 --format=%cd --date=format:%Y.%m.%d') ||
  new Date().toISOString().slice(0, 10).replaceAll('-', '.')
const version = `${date}-${sha ? sha.slice(0, 7) : 'dev'}`

// /version.json, for server.mjs (it tags forwarded error reports) and for checking a deploy
const versionFile: Plugin = {
  name: 'build-version',
  applyToEnvironment: (env) => env.name === 'client',
  generateBundle() {
    this.emitFile({ type: 'asset', fileName: 'version.json', source: JSON.stringify({ version, sha }) })
  },
}

export default defineConfig({
  plugins: [tailwindcss(), reactRouter(), versionFile],
  define: {
    __BUILD_VERSION__: JSON.stringify(version),
    __BUILD_SHA__: JSON.stringify(sha),
  },
  environments: {
    client: {
      build: {
        // Maps without the sourceMappingURL comment; scripts/move-sourcemaps.mjs moves them out
        // of the served files, and server.mjs uses them to map error reports' stack traces
        sourcemap: 'hidden',
        // Fonts stay files: the CSP's font-src doesn't allow data: URLs
        assetsInlineLimit: (file) => (/\.woff2?$/.test(file) ? false : undefined),
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

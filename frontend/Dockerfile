# CI builds arm64 natively. If the image is ever built for another platform,
# this stage still runs natively (emulated npm runs are slow and can hang): the
# output is plain JavaScript and the pruned runtime packages have no native modules.
FROM --platform=$BUILDPLATFORM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build && npm prune --omit=dev

# Node renders the pages (server.mjs), serves the assets and proxies /api and /mcp
FROM node:24-alpine
# The runtime only runs node. npm, corepack and yarn ship their own bundled packages that
# Trivy flags (undici, brace-expansion, ...), so drop them.
RUN apk upgrade --no-cache \
    && rm -rf /usr/local/lib/node_modules/npm /usr/local/lib/node_modules/corepack /opt/yarn-* \
       /usr/local/bin/npm /usr/local/bin/npx /usr/local/bin/corepack /usr/local/bin/yarn /usr/local/bin/yarnpkg
WORKDIR /app
# The server renders game times in Central time until the browser takes over
ENV NODE_ENV=production PORT=8080 TZ=America/Chicago
COPY --from=build /app/package.json ./
COPY --from=build /app/node_modules ./node_modules
COPY --from=build /app/build ./build
COPY server.mjs ./
# Numeric (the node user) so Kubernetes runAsNonRoot can verify it
USER 1000
EXPOSE 8080
CMD ["node", "server.mjs"]

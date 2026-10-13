# Built on the runner's own architecture: QEMU-emulated npm runs are slow and
# can hang. The output is plain JavaScript and the pruned runtime packages have
# no native modules, so the same files work on amd64 and arm64.
FROM --platform=$BUILDPLATFORM node:24-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build && npm prune --omit=dev

# Node renders the pages (server.mjs), serves the assets and proxies /api and /mcp
FROM node:24-alpine
RUN apk upgrade --no-cache
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

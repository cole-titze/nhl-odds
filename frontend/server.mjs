// Production server: renders pages with React Router, serves the built
// assets, and proxies /api and /mcp to the web API (nginx did all three before).
import compression from 'compression';
import express from 'express';
import { createRequestHandler } from '@react-router/express';
import { createProxyMiddleware } from 'http-proxy-middleware';

const API_URL = process.env.API_URL || 'http://webapi:8080';
const PORT = Number(process.env.PORT) || 8080;

const app = express();
app.disable('x-powered-by');

const setRealIp = (proxyReq, req) => proxyReq.setHeader('X-Real-IP', req.socket.remoteAddress ?? '');

// MCP streams its responses, so it goes before compression (which buffers) and has no timeout
app.use(
  createProxyMiddleware({
    target: API_URL,
    pathFilter: '/mcp',
    proxyTimeout: 0,
    on: { proxyReq: setRealIp },
  }),
);

app.use(compression());

app.use(createProxyMiddleware({ target: API_URL, pathFilter: '/api', on: { proxyReq: setRealIp } }));

// Hashed file names, so they can be cached forever
app.use('/assets', express.static('build/client/assets', { immutable: true, maxAge: '1y' }));
app.use(express.static('build/client', { maxAge: '1h', index: false }));

app.use(createRequestHandler({ build: await import('./build/server/index.js') }));

app.listen(PORT, () => console.log(`Listening on http://localhost:${PORT}`));

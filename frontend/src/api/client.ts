// In the browser the API is same-origin (/api is proxied). On the server it's
// called directly: API_URL is the webapi service, the Vite proxy target in dev.
const BASE_URL = import.meta.env.SSR
  ? process.env.API_URL || 'http://localhost:32616'
  : import.meta.env.VITE_API_URL || '';

export class ApiError extends Error {
  readonly status: number;
  constructor(status: number, statusText: string) {
    super(`API error: ${status} ${statusText}`);
    this.status = status;
  }
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, init);
  if (!res.ok) {
    throw new ApiError(res.status, res.statusText);
  }
  return res.json();
}

/**
 * Passes the visitor's IP on to the API from a route loader, so its per-IP
 * rate limit counts the visitor rather than this server.
 */
export function forwardClientIp(request: Request): RequestInit | undefined {
  const ip = request.headers.get('CF-Connecting-IP') ?? request.headers.get('X-Real-IP');
  return ip ? { headers: { 'CF-Connecting-IP': ip } } : undefined;
}

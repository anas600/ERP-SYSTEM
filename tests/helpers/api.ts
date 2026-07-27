/**
 * API helpers — used by all specs to hit the backend with proper auth.
 * Re-exports the access token + X-Company-Id automatically.
 */
import { APIRequestContext, request } from '@playwright/test';

export const API = process.env.API_BASE_URL || 'http://localhost:5000';
export const HOLDING_ID = '00000000-0000-0000-0000-000000000001';
export const ADMIN_EMAIL = 'admin@alfajr.local';
export const ADMIN_PASSWORD = 'Demo1234';

let _cachedToken: string | null = null;
let _cachedExpiresAt = 0;
const TOKEN_TTL_MS = 30 * 60 * 1000; // 30 min

export async function getApiContext(): Promise<APIRequestContext> {
  return await request.newContext({ baseURL: API });
}

export async function login(): Promise<string> {
  const now = Date.now();
  if (_cachedToken && now < _cachedExpiresAt) return _cachedToken;

  const ctx = await getApiContext();
  const res = await ctx.post('/api/auth/login', {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
  });
  if (!res.ok()) throw new Error(`Login failed: ${res.status()}`);
  const json = await res.json();
  if (!json.accessToken) throw new Error('No accessToken in login response');
  _cachedToken = json.accessToken;
  _cachedExpiresAt = now + TOKEN_TTL_MS;
  await ctx.dispose();
  return _cachedToken!;
}

export function authHeaders(token: string, companyId: string = HOLDING_ID) {
  return {
    Authorization: `Bearer ${token}`,
    'X-Company-Id': companyId,
    'Content-Type': 'application/json',
  };
}

export async function apiGet<T = any>(path: string, params?: Record<string, any>): Promise<{ status: number; data: T | null; raw: string }> {
  const token = await login();
  const ctx = await getApiContext();
  const res = await ctx.get(path, { headers: authHeaders(token), params });
  const raw = await res.text();
  let data: T | null = null;
  try { data = raw ? JSON.parse(raw) : null; } catch { /* not JSON */ }
  await ctx.dispose();
  return { status: res.status(), data, raw };
}

export async function apiPost<T = any>(path: string, body: any): Promise<{ status: number; data: T | null; raw: string }> {
  const token = await login();
  const ctx = await getApiContext();
  const res = await ctx.post(path, { headers: authHeaders(token), data: body });
  const raw = await res.text();
  let data: T | null = null;
  try { data = raw ? JSON.parse(raw) : null; } catch { /* not JSON */ }
  await ctx.dispose();
  return { status: res.status(), data, raw };
}

export async function apiPut<T = any>(path: string, body: any): Promise<{ status: number; data: T | null; raw: string }> {
  const token = await login();
  const ctx = await getApiContext();
  const res = await ctx.put(path, { headers: authHeaders(token), data: body });
  const raw = await res.text();
  let data: T | null = null;
  try { data = raw ? JSON.parse(raw) : null; } catch { /* not JSON */ }
  await ctx.dispose();
  return { status: res.status(), data, raw };
}

export async function apiDelete(path: string): Promise<{ status: number; data: any; raw: string }> {
  const token = await login();
  const ctx = await getApiContext();
  const res = await ctx.delete(path, { headers: authHeaders(token) });
  const raw = await res.text();
  let data: any = null;
  try { data = raw ? JSON.parse(raw) : null; } catch { /* not JSON */ }
  await ctx.dispose();
  return { status: res.status(), data, raw };
}

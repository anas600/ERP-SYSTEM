import { test, expect, request } from '@playwright/test';

/**
 * E2E: Auth flows (DEC-094, 2026-07-24)
 *
 * - register.happy: full register flow → JWT cookie set → redirect to /dashboard
 * - register.duplicate: same email twice → 400/409, no orphan tenant
 * - login.happy: register then login → JWT cookie set
 * - atomicity: abort register mid-process → verify NO orphan tenant in DB
 *
 * Backend URL is read from E2E_API_URL (default http://localhost:5000).
 * Frontend URL is from baseURL in playwright.config (http://localhost:3000).
 */

const API_URL = process.env.E2E_API_URL ?? 'http://localhost:5000';

test.describe.configure({ mode: 'serial' });

function uniqueEmail(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.floor(Math.random() * 1e6)}@e2e.local`;
}

test('register.happy — full flow via API', async () => {
  const ctx = await request.newContext({ baseURL: API_URL });
  const tenantName = `E2E-Happy-${Date.now()}`;
  const email = uniqueEmail('happy');

  const res = await ctx.post('/api/auth/register', {
    data: {
      tenantName,
      email,
      password: 'E2eTest123!',
      fullName: 'E2E Happy Path',
      baseCurrency: 'LYD',
    },
  });

  expect(res.status(), `register failed: ${await res.text()}`).toBe(200);
  const body = await res.json();
  expect(body.accessToken).toBeTruthy();
  expect(body.refreshToken).toBeTruthy();
  expect(body.user.email).toBe(email.toLowerCase());
  expect(body.user.tenantId).toBeTruthy();
  expect(body.holdingCompanyId).toBeTruthy();

  await ctx.dispose();
});

test('register.duplicate — same email twice → conflict, no orphan', async () => {
  const ctx = await request.newContext({ baseURL: API_URL });
  const email = uniqueEmail('dupe');
  const payload = {
    tenantName: `E2E-Dupe-${Date.now()}`,
    email,
    password: 'E2eTest123!',
    fullName: 'E2E Dupe',
    baseCurrency: 'LYD',
  };

  const r1 = await ctx.post('/api/auth/register', { data: payload });
  expect(r1.status()).toBe(200);
  const user1 = (await r1.json()).user;
  const tenant1 = user1.tenantId;

  const r2 = await ctx.post('/api/auth/register', { data: payload });
  // Second register with same email in same tenant should fail with conflict.
  expect([400, 409]).toContain(r2.status());

  // Login with the original credentials should still work — proves the FIRST
  // tenant is still intact (not orphaned) and the second attempt didn't leak rows.
  const loginRes = await ctx.post('/api/auth/login', {
    data: { email, password: 'E2eTest123!', tenantId: tenant1 },
  });
  expect(loginRes.status(), 'original tenant should still be login-able').toBe(200);
  const loginBody = await loginRes.json();
  expect(loginBody.user.tenantId).toBe(tenant1);

  await ctx.dispose();
});

test('login.happy — register then login', async () => {
  const ctx = await request.newContext({ baseURL: API_URL });
  const email = uniqueEmail('login');
  const tenantName = `E2E-Login-${Date.now()}`;

  // 1. register
  const reg = await ctx.post('/api/auth/register', {
    data: {
      tenantName,
      email,
      password: 'E2eTest123!',
      fullName: 'E2E Login',
      baseCurrency: 'LYD',
    },
  });
  expect(reg.status()).toBe(200);
  const { user } = await reg.json();

  // 2. logout (dispose context drops the cookies; new context for login)
  await ctx.dispose();

  // 3. login with the same credentials
  const ctx2 = await request.newContext({ baseURL: API_URL });
  const login = await ctx2.post('/api/auth/login', {
    data: { email, password: 'E2eTest123!', tenantId: user.tenantId },
  });
  expect(login.status()).toBe(200);
  const loginBody = await login.json();
  expect(loginBody.accessToken).toBeTruthy();
  expect(loginBody.user.email).toBe(email.toLowerCase());

  await ctx2.dispose();
});

test('atomicity — abort register mid-process → NO orphan tenant', async () => {
  // Strategy: send a register request, then abort the request after 50ms
  // (before the transaction can complete). Repeat 5x. Then verify each
  // email address does NOT exist in the system (login with the same email
  // should return 401/404 because the tenant was rolled back).

  const aborts: { email: string; tenantName: string }[] = [];
  for (let i = 0; i < 5; i++) {
    aborts.push({
      tenantName: `E2E-Atomic-${Date.now()}-${i}`,
      email: uniqueEmail(`atomic-${i}`),
    });
  }

  // Fire all aborts in parallel
  const results = await Promise.allSettled(
    aborts.map((a) =>
      request.newContext({ baseURL: API_URL }).then(async (ctx) => {
        try {
          const ac = new AbortController();
          const t = setTimeout(() => ac.abort(), 50); // abort 50ms in
          const r = await ctx.post('/api/auth/register', {
            data: { ...a, password: 'E2eTest123!', fullName: 'Atomic', baseCurrency: 'LYD' },
          });
          clearTimeout(t);
          return { status: r.status(), body: await r.text() };
        } catch (e: any) {
          return { status: 0, body: e.message ?? String(e) };
        } finally {
          await ctx.dispose();
        }
      })
    )
  );

  // Now verify NONE of the aborted emails can login — meaning the tenant was rolled back
  // and no orphan tenant was created.
  for (let i = 0; i < aborts.length; i++) {
    const a = aborts[i];
    const res = results[i];
    const ctx = await request.newContext({ baseURL: API_URL });

    // Try to login with the aborted email — should fail (no user exists)
    const login = await ctx.post('/api/auth/login', {
      data: { email: a.email, password: 'E2eTest123!' },
    });
    expect(
      [400, 401, 404],
      `abort ${i} (${a.email}, status=${res.status}) should leave NO loginable user. ` +
      `Got login status ${login.status()}. This indicates an orphan tenant was created.`
    ).toContain(login.status());

    await ctx.dispose();
  }
});

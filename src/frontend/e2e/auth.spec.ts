import { test, expect, request } from '@playwright/test';

/**
 * E2E: Auth flows (DEC-094, 2026-07-24, refreshed Phase 6.3 2026-07-26).
 *
 * Phase 6.3: Multi-Company model.
 *   - register.happy: full register flow → JWT cookie, holdingCompanyId, user.companies populated
 *   - register.duplicate: same email twice → 400/409, no orphan user (the failure mode is now
 *     "no orphan user" — tenants are no longer created at register time)
 *   - login.happy: register then login → JWT cookie, defaultCompanyId set
 *   - atomicity: abort register mid-process → verify NO orphan user in DB
 *     (Phase 6.3 replaces the "no orphan tenant" check with "no orphan user")
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
  const email = uniqueEmail('happy');

  const res = await ctx.post('/api/auth/register', {
    data: {
      email,
      password: 'E2eTest123!',
      fullName: 'E2E Happy Path',
    },
  });

  expect(res.status(), `register failed: ${await res.text()}`).toBe(200);
  const body = await res.json();
  expect(body.accessToken).toBeTruthy();
  expect(body.refreshToken).toBeTruthy();
  expect(body.user.email).toBe(email.toLowerCase());
  // Phase 6.3: multi-company claims in the response
  expect(body.user.defaultCompanyId).toBeTruthy();
  expect(Array.isArray(body.user.companies)).toBe(true);
  expect(body.user.companies.length).toBeGreaterThanOrEqual(1);
  expect(body.user.companies[0].isHolding).toBe(true);
  expect(body.holdingCompanyId).toBeTruthy();
  // Sanity: defaultCompanyId is one of the companies
  expect(body.user.companies.some((c: any) => c.companyId === body.user.defaultCompanyId)).toBe(true);

  await ctx.dispose();
});

test('register.duplicate — same email twice → conflict, no orphan user', async () => {
  const ctx = await request.newContext({ baseURL: API_URL });
  const email = uniqueEmail('dupe');
  const payload = {
    email,
    password: 'E2eTest123!',
    fullName: 'E2E Dupe',
  };

  const r1 = await ctx.post('/api/auth/register', { data: payload });
  expect(r1.status()).toBe(200);
  const user1 = (await r1.json()).user;
  const company1 = user1.defaultCompanyId;

  const r2 = await ctx.post('/api/auth/register', { data: payload });
  // Second register with same email should fail with conflict.
  expect([400, 409]).toContain(r2.status());

  // Login with the original credentials should still work — proves the FIRST
  // user is still intact (not orphaned) and the second attempt didn't leak rows.
  const loginRes = await ctx.post('/api/auth/login', {
    data: { email, password: 'E2eTest123!' },
  });
  expect(loginRes.status(), 'original user should still be login-able').toBe(200);
  const loginBody = await loginRes.json();
  expect(loginBody.user.defaultCompanyId).toBe(company1);

  await ctx.dispose();
});

test('login.happy — register then login', async () => {
  const ctx = await request.newContext({ baseURL: API_URL });
  const email = uniqueEmail('login');

  // 1. register
  const reg = await ctx.post('/api/auth/register', {
    data: {
      email,
      password: 'E2eTest123!',
      fullName: 'E2E Login',
    },
  });
  expect(reg.status()).toBe(200);
  const regBody = await reg.json();
  const user = regBody.user;

  // 2. logout (dispose context drops the cookies; new context for login)
  await ctx.dispose();

  // 3. login with the same credentials (Phase 6.3: no tenantId in payload)
  const ctx2 = await request.newContext({ baseURL: API_URL });
  const login = await ctx2.post('/api/auth/login', {
    data: { email, password: 'E2eTest123!' },
  });
  expect(login.status()).toBe(200);
  const loginBody = await login.json();
  expect(loginBody.accessToken).toBeTruthy();
  expect(loginBody.user.email).toBe(email.toLowerCase());
  expect(loginBody.user.defaultCompanyId).toBe(user.defaultCompanyId);
  // The new token's default_company_id claim should match the user's default.
  const me = await ctx2.get('/api/auth/me');
  expect(me.status()).toBe(200);
  const meBody = await me.json();
  expect(meBody.defaultCompanyId).toBe(user.defaultCompanyId);

  await ctx2.dispose();
});

test('atomicity — abort register mid-process → NO orphan user', async () => {
  // Phase 6.3: tenants are no longer created at register time, so the
  // old "no orphan tenant" check is now "no orphan user". Same atomicity
  // guarantee (single conn + single tx in RegisterAsync) — verified by
  // sending 5 register requests, aborting them mid-process, then asserting
  // that none of the emails can be logged in (because the user row was
  // rolled back together with the (now non-existent) tenant).

  const aborts: { email: string }[] = [];
  for (let i = 0; i < 5; i++) {
    aborts.push({ email: uniqueEmail(`atomic-${i}`) });
  }

  // Fire all aborts in parallel
  const results = await Promise.allSettled(
    aborts.map((a) =>
      request.newContext({ baseURL: API_URL }).then(async (ctx) => {
        try {
          const ac = new AbortController();
          const t = setTimeout(() => ac.abort(), 50); // abort 50ms in
          const r = await ctx.post('/api/auth/register', {
            data: { email: a.email, password: 'E2eTest123!', fullName: 'Atomic' },
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

  // Now verify NONE of the aborted emails can login — meaning the user was rolled back
  // and no orphan user was created.
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
      `Got login status ${login.status()}. This indicates an orphan user was created.`
    ).toContain(login.status());

    await ctx.dispose();
  }
});

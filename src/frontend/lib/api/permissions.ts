// Sprint 63 (DEC-218) — FE client for GET /api/me/permissions.
//
// Returns the list of permission codes the current user holds (used by the
// PermissionGate component to show/hide create/edit/delete buttons).
//
// L19 / DEC-095: this client sends NO userId. The BE reads the userId from
// the JWT (the api.ts interceptor already attaches the Bearer token).

import { api } from '../api';
import type { MyPermissionsResponse } from '../api-types';

const ENDPOINT = '/api/me/permissions';

/** Wildcard permission that grants every check. Set on the Admin role. */
export const ADMIN_ALL_PERMISSION = 'admin.all';

/**
 * Fetch the list of permission codes the current user holds.
 *
 * BE returns `{ permissions: ["projects.view", "projects.create", ...] }`.
 * The hook layer (`usePermissions`) wraps this in a Set and exposes a
 * `hasPermission(code)` helper.
 */
export async function fetchMyPermissions(): Promise<string[]> {
  const { data } = await api.get<MyPermissionsResponse>(ENDPOINT);

  // Defensive: BE contract is { permissions: string[] } but be tolerant.
  const list = (data && Array.isArray(data.permissions)) ? data.permissions : [];
  return list;
}

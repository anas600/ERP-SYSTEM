// Sprint 63 (DEC-217) — FE client for GET /api/me/visible-modules.
//
// Returns the list of module names the current user can see (used by the
// SmartSidebar to hide items the user has no access to).
//
// L19 / DEC-095: this client sends NO userId. The BE reads the userId from
// the JWT (the api.ts interceptor already attaches the Bearer token).

import { api } from '../api';
import type { ModuleCode, VisibleModulesResponse } from '../api-types';

const ENDPOINT = '/api/me/visible-modules';

/**
 * Fetch the list of module names the current user can see.
 *
 * BE returns `{ modules: ["Dashboard", "HR", ...] }`. The hook layer
 * (`useVisibleModules`) handles the unwrap; this function is the typed
 * transport.
 */
export async function fetchVisibleModules(): Promise<ModuleCode[]> {
  const { data } = await api.get<VisibleModulesResponse>(ENDPOINT);

  // Defensive: BE contract is { modules: string[] } but be tolerant.
  const list = (data && Array.isArray(data.modules)) ? data.modules : [];
  return list as ModuleCode[];
}

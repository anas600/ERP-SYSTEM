// 📡 lib/api/dashboard.ts — Sprint 65 / Wave 2A (DEC-234 + DEC-236)
//
// Cross-module dashboard API client. Sits next to the existing `dashboardApi`
// in `lib/api.ts` (Sprint 1 / Sprint 5 summary + charts) but is split into its
// own file because the new endpoints are a different domain (cross-module
// AR ↔ AP ↔ Project profitability, not just journal-entry counts).
//
// The 2 functions correspond 1:1 to the C# controller endpoints:
//   - fetchDashboardCrossModule()     → GET /api/dashboard/cross-module
//   - fetchProjectProfitability()     → GET /api/dashboard/project-profitability
//
// Contract notes:
//   - The X-Company-Id header is set by the axios interceptor in `lib/api.ts`,
//     so callers don't need to pass the companyId explicitly.
//   - On 401 the global response interceptor in `lib/api.ts` redirects to
//     /login (no per-call handling needed).
//   - The /project-profitability endpoint may return either a bare JSON array
//     or a `{$values: [...]}` wrapper depending on the ASP.NET serializer. The
//     helper normalises both shapes so the FE doesn't care.

import { api } from '../api';
import type {
  DashboardCrossModuleResponse,
  ProjectProfitabilityResponse,
} from '../api-types';

export async function fetchDashboardCrossModule(): Promise<DashboardCrossModuleResponse> {
  const { data } = await api.get<DashboardCrossModuleResponse>(
    '/api/dashboard/cross-module',
  );
  return data;
}

export async function fetchProjectProfitability(): Promise<ProjectProfitabilityResponse[]> {
  const { data } = await api.get<
    | ProjectProfitabilityResponse[]
    | { $values: ProjectProfitabilityResponse[] }
  >('/api/dashboard/project-profitability');
  return Array.isArray(data) ? data : (data.$values ?? []);
}

// 📡 lib/api/reconciliation.ts — Sprint 65 / Wave 3A (DEC-235 + DEC-237)
//
// Bank reconciliation API client. Sits next to the existing `lib/api/dashboard.ts`
// (Wave 2A) because it is a new finance domain. The 3 functions correspond 1:1
// to the C# controller endpoints in
// `src/backend/Host/Controllers/BankReconciliationsController.cs`:
//
//   - suggestMatches(receiptId, max)      → GET /api/receipts/{id}/suggest-matches?max=5
//   - confirmMatch(receiptId, subPayId)   → POST /api/receipts/{id}/confirm-match/{subPaymentId}
//   - fetchReconciliationQueue(skip, take) → GET /api/reconciliation/queue?skip=0&take=50
//
// Contract notes:
//   - The X-Company-Id header is set by the axios interceptor in `lib/api.ts`,
//     so callers don't need to pass the companyId explicitly.
//   - On 401 the global response interceptor in `lib/api.ts` redirects to
//     /login (no per-call handling needed).
//   - Some endpoints may return either a bare JSON array or a `{$values: [...]}`
//     wrapper depending on the ASP.NET serializer. The helpers normalise both
//     shapes so the FE doesn't care.

import { api } from '../api';
import type { SubPaymentMatch, UnmatchedReceipt } from '../api-types';

export async function suggestMatches(
  receiptId: string,
  max = 5,
): Promise<SubPaymentMatch[]> {
  const { data } = await api.get<
    SubPaymentMatch[] | { $values: SubPaymentMatch[] }
  >(`/api/receipts/${receiptId}/suggest-matches?max=${max}`);
  return Array.isArray(data) ? data : (data.$values ?? []);
}

export async function confirmMatch(
  receiptId: string,
  subPaymentId: string,
): Promise<SubPaymentMatch> {
  const { data } = await api.post<SubPaymentMatch>(
    `/api/receipts/${receiptId}/confirm-match/${subPaymentId}`,
  );
  return data;
}

export async function fetchReconciliationQueue(
  skip = 0,
  take = 50,
): Promise<UnmatchedReceipt[]> {
  const { data } = await api.get<
    UnmatchedReceipt[] | { $values: UnmatchedReceipt[] }
  >(`/api/reconciliation/queue?skip=${skip}&take=${take}`);
  return Array.isArray(data) ? data : (data.$values ?? []);
}

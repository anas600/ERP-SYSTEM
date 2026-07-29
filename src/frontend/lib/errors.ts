// lib/errors.ts
// Bilingual (AR + EN) error message catalog for the ERP frontend.
//
// Why this exists:
// - Every error toast / inline error / fallback should speak both Arabic and
//   English so support + demos don't need a translator on standby.
// - Centralizing the messages keeps the wording consistent across pages
//   (instead of "فشل التحميل" / "تعذّر التحميل" / "خطأ في الجلب" drift).
// - `formatApiError` wraps axios errors and picks a sensible fallback based
//   on the HTTP status (401 → session expired, 403 → forbidden, 5xx → server
//   down, etc.) using the same bilingual catalog.
//
// Usage:
//   toast.error(formatApiError(e, ErrorKey.LOAD_FAILED));
//   setError(getApiErrorMessage(e, ErrorKey.SAVE_FAILED));
//
// Convention: each key has a stable { ar, en } pair. New keys: append to
// `ErrorMessages`, never delete or rename a key (would break CHANGELOG audits).

export type ErrorKey =
  | 'LOAD_FAILED'
  | 'SAVE_FAILED'
  | 'DELETE_FAILED'
  | 'NETWORK'
  | 'UNAUTHORIZED'
  | 'FORBIDDEN'
  | 'NOT_FOUND'
  | 'SERVER'
  | 'VALIDATION'
  | 'TIMEOUT'
  | 'POSTING_FAILED'
  | 'CANCEL_FAILED'
  | 'MARK_READ_FAILED'
  | 'PRINT_FAILED'
  | 'EMPTY_RESULTS'
  | 'GENERIC';

export interface Bilingual {
  ar: string;
  en: string;
}

export const ErrorMessages: Record<ErrorKey, Bilingual> = {
  LOAD_FAILED:        { ar: 'فشل تحميل البيانات.',           en: 'Failed to load data.' },
  SAVE_FAILED:        { ar: 'فشل الحفظ. حاول مرة أخرى.',     en: 'Save failed. Please try again.' },
  DELETE_FAILED:      { ar: 'فشل الحذف.',                     en: 'Delete failed.' },
  NETWORK:            { ar: 'تعذّر الاتصال بالخادم.',         en: 'Cannot reach the server.' },
  UNAUTHORIZED:       { ar: 'انتهت الجلسة. سجّل دخولك مجدداً.', en: 'Session expired. Please sign in again.' },
  FORBIDDEN:          { ar: 'ليست لديك صلاحية لهذا الإجراء.', en: 'You do not have permission for this action.' },
  NOT_FOUND:          { ar: 'العنصر غير موجود.',              en: 'Item not found.' },
  SERVER:             { ar: 'خطأ في الخادم. حاول لاحقاً.',    en: 'Server error. Please try again later.' },
  VALIDATION:         { ar: 'البيانات المدخلة غير صحيحة.',    en: 'The provided data is invalid.' },
  TIMEOUT:            { ar: 'انتهت مهلة الطلب.',              en: 'Request timed out.' },
  POSTING_FAILED:     { ar: 'فشل ترحيل المستند.',             en: 'Document posting failed.' },
  CANCEL_FAILED:      { ar: 'فشل الإلغاء.',                   en: 'Cancellation failed.' },
  MARK_READ_FAILED:   { ar: 'فشل التعليم كمقروء.',            en: 'Failed to mark as read.' },
  PRINT_FAILED:       { ar: 'فشل الطباعة.',                   en: 'Print failed.' },
  EMPTY_RESULTS:      { ar: 'لا توجد بيانات لعرضها.',         en: 'No data to display.' },
  GENERIC:            { ar: 'حدث خطأ غير متوقع.',            en: 'An unexpected error occurred.' },
};

/**
 * Pick a key based on an HTTP status code (used by `formatApiError`).
 */
export function keyFromStatus(status?: number): ErrorKey {
  if (status === undefined || status === null) return 'NETWORK';
  if (status === 401) return 'UNAUTHORIZED';
  if (status === 403) return 'FORBIDDEN';
  if (status === 404) return 'NOT_FOUND';
  if (status === 408) return 'TIMEOUT';
  if (status === 422) return 'VALIDATION';
  if (status >= 500) return 'SERVER';
  return 'GENERIC';
}

/**
 * Format a bilingual error message from an unknown error (axios, fetch, etc.).
 *
 * @param e      - the thrown error
 * @param fallback - key to use when no specific status-driven message applies
 * @param locale  - 'ar' or 'en' (default 'ar' — primary locale per AGENTS.md)
 * @returns localized string ready for `toast.error()` or `setError()`
 */
export function formatApiError(
  e: unknown,
  fallback: ErrorKey = 'GENERIC',
  locale: 'ar' | 'en' = 'ar',
): string {
  // Try to extract a status code
  const status = extractStatus(e);
  const key = status !== undefined ? keyFromStatus(status) : fallback;
  return ErrorMessages[key][locale];
}

/**
 * Same as formatApiError but returns BOTH languages joined by ' | '.
 * Useful for error boundaries / logs that need both.
 */
export function formatApiErrorBilingual(
  e: unknown,
  fallback: ErrorKey = 'GENERIC',
): string {
  const status = extractStatus(e);
  const key = status !== undefined ? keyFromStatus(status) : fallback;
  const m = ErrorMessages[key];
  return `${m.ar} | ${m.en}`;
}

function extractStatus(e: unknown): number | undefined {
  if (!e || typeof e !== 'object') return undefined;
  const obj = e as Record<string, unknown>;
  // Axios shape: e.response.status
  const resp = obj.response as { status?: number } | undefined;
  if (resp?.status) return resp.status;
  // Fetch shape: e.status
  if (typeof obj.status === 'number') return obj.status;
  return undefined;
}

/**
 * Convenience: get a bilingual message by key without an error object.
 * Useful in static UI (empty states, etc.) where the cause is known.
 */
export function t(key: ErrorKey, locale: 'ar' | 'en' = 'ar'): string {
  return ErrorMessages[key][locale];
}

export function tBilingual(key: ErrorKey): string {
  const m = ErrorMessages[key];
  return `${m.ar} | ${m.en}`;
}

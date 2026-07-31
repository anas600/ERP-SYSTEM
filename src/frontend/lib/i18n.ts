'use client';

// i18n helper — bilingual (Arabic primary + English fallback) string lookup.
// Lightweight foundation: the rest of the codebase currently uses hardcoded
// Arabic strings (per the existing convention). This helper is the canonical
// place to start migrating user-facing copy to AR + EN. Adopting it is
// optional and incremental — existing components keep their hardcoded strings.
//
// Usage:
//   import { useTranslation } from '@/lib/i18n';
//   const t = useTranslation();
//   <h1>{t('error.unexpected')}</h1>
//
// Or the imperative variant for non-React contexts:
//   import { t } from '@/lib/i18n';
//   const msg = t('error.network', 'en');

import { useMemo } from 'react';

export type Locale = 'ar' | 'en';

export const DEFAULT_LOCALE: Locale = 'ar';

/**
 * Translation dictionary — single source of truth.
 * Arabic is the primary locale (per Constitution / frontend AGENTS.md).
 * Keys are dot-namespaced (`error.*`, `loading.*`, `common.*`, ...).
 *
 * To add a new key: add it under BOTH `ar` and `en`. The type system enforces
 * this loosely; a missing key falls back to the key string itself.
 */
export const translations: Record<Locale, Record<string, string>> = {
  ar: {
    // Error messages (Sprint 9 T3 — FE Jimi 3)
    'error.unexpected': 'حدث خطأ غير متوقع',
    'error.network': 'خطأ في الاتصال بالشبكة',
    'error.unauthorized': 'غير مصرح بالوصول',
    'error.forbidden': 'الوصول مرفوض',
    // Loading messages (Sprint 9 T3 — FE Jimi 3)
    'loading.companies': 'جاري تحميل الشركات...',
    'loading.dashboard': 'جاري تحميل لوحة المعلومات...',
    'loading.holding': 'جاري تحميل بيانات القابضة...',
  },
  en: {
    'error.unexpected': 'An unexpected error occurred',
    'error.network': 'Network error',
    'error.unauthorized': 'Unauthorized',
    'error.forbidden': 'Forbidden',
    'loading.companies': 'Loading companies...',
    'loading.dashboard': 'Loading dashboard...',
    'loading.holding': 'Loading holding data...',
  },
};

/**
 * Imperative translation helper. For non-React contexts (error boundary,
 * class components, server-side). Falls back to the key when missing.
 */
export function t(key: string, locale: Locale = DEFAULT_LOCALE): string {
  const dict = translations[locale] ?? translations[DEFAULT_LOCALE];
  const value = dict[key];
  if (value !== undefined) return value;
  // Last-resort fallback: English, then the raw key.
  const en = translations.en[key];
  return en ?? key;
}

/**
 * Hook variant — returns a stable `t(key)` function bound to the current
 * locale. Currently the locale is fixed to `DEFAULT_LOCALE` (Arabic); the
 * hook is here so the call sites are ready for a future locale switcher.
 */
export function useTranslation(locale: Locale = DEFAULT_LOCALE) {
  return useMemo(() => {
    const dict = translations[locale] ?? translations[DEFAULT_LOCALE];
    const en = translations.en;
    return (key: string): string => {
      const value = dict[key];
      if (value !== undefined) return value;
      const enValue = en[key];
      return enValue ?? key;
    };
  }, [locale]);
}
